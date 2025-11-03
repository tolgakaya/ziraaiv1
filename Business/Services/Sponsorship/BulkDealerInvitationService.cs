using Business.Services.MessageQueue;
using Core.Configuration;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using IFormFile = Microsoft.AspNetCore.Http.IFormFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public interface IBulkDealerInvitationService
    {
        Task<IDataResult<BulkInvitationJobDto>> QueueBulkInvitationsAsync(
            IFormFile excelFile,
            int sponsorId,
            string invitationType,
            bool sendSms);
    }

    public class BulkDealerInvitationService : IBulkDealerInvitationService
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IBulkInvitationJobRepository _bulkJobRepository;
        private readonly ISponsorshipCodeRepository _codeRepository;
        private readonly ISubscriptionTierRepository _tierRepository;
        private readonly IUserRepository _userRepository;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly ILogger<BulkDealerInvitationService> _logger;

        private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private const int MaxRowCount = 2000;

        public BulkDealerInvitationService(
            IMessageQueueService messageQueueService,
            IBulkInvitationJobRepository bulkJobRepository,
            ISponsorshipCodeRepository codeRepository,
            ISubscriptionTierRepository tierRepository,
            IUserRepository userRepository,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            ILogger<BulkDealerInvitationService> logger)
        {
            _messageQueueService = messageQueueService;
            _bulkJobRepository = bulkJobRepository;
            _codeRepository = codeRepository;
            _tierRepository = tierRepository;
            _userRepository = userRepository;
            _rabbitMQOptions = rabbitMQOptions.Value;
            _logger = logger;
        }

        public async Task<IDataResult<BulkInvitationJobDto>> QueueBulkInvitationsAsync(
            IFormFile excelFile,
            int sponsorId,
            string invitationType,
            bool sendSms)
        {
            try
            {
                _logger.LogInformation(
                    "📤 Starting bulk invitation - SponsorId: {SponsorId}, Type: {Type}",
                    sponsorId, invitationType);

                // 1. Validate file
                var fileValidation = ValidateFile(excelFile);
                if (!fileValidation.Success)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>(fileValidation.Message);
                }

                // 2. Parse Excel (header-based)
                var rows = await ParseExcelAsync(excelFile);

                if (rows.Count == 0)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>("Excel dosyasında geçerli satır bulunamadı.");
                }

                if (rows.Count > MaxRowCount)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>(
                        $"Maksimum {MaxRowCount} dealer kaydı yüklenebilir. Dosyanızda {rows.Count} kayıt var.");
                }

                // 3. Validate rows
                var validationResult = await ValidateRowsAsync(rows, sponsorId);
                if (!validationResult.Success)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>(validationResult.Message);
                }

                // 4. Check code availability (per tier)
                var codeCheckResult = await CheckCodeAvailabilityAsync(rows, sponsorId);
                if (!codeCheckResult.Success)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>(codeCheckResult.Message);
                }

                // 5. Create BulkInvitationJob entity
                var bulkJob = new BulkInvitationJob
                {
                    SponsorId = sponsorId,
                    InvitationType = invitationType,
                    DefaultTier = null,  // Not used anymore
                    DefaultCodeCount = 0,  // Not used anymore
                    SendSms = sendSms,
                    TotalDealers = rows.Count,
                    ProcessedDealers = 0,
                    SuccessfulInvitations = 0,
                    FailedInvitations = 0,
                    Status = "Pending",
                    CreatedDate = DateTime.Now,
                    OriginalFileName = excelFile.FileName,
                    FileSize = (int)excelFile.Length
                };

                _bulkJobRepository.Add(bulkJob);
                await _bulkJobRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ BulkInvitationJob created - JobId: {JobId}, TotalDealers: {TotalDealers}",
                    bulkJob.Id, bulkJob.TotalDealers);

                // 6. Publish messages to RabbitMQ (one per dealer)
                var queueName = _rabbitMQOptions.Queues.DealerInvitationRequest;
                var publishedCount = 0;

                foreach (var row in rows)
                {
                    var queueMessage = new DealerInvitationQueueMessage
                    {
                        CorrelationId = bulkJob.Id.ToString(),
                        RowNumber = row.RowNumber,
                        BulkJobId = bulkJob.Id,
                        SponsorId = sponsorId,
                        Email = row.Email,
                        Phone = row.Phone,
                        DealerName = row.DealerName,
                        InvitationType = invitationType,
                        PackageTier = row.PackageTier,  // Optional: null for auto-allocation
                        CodeCount = row.CodeCount.Value,  // Required from Excel
                        SendSms = sendSms,
                        QueuedAt = DateTime.Now
                    };

                    var published = await _messageQueueService.PublishAsync(
                        queueName,
                        queueMessage,
                        bulkJob.Id.ToString());

                    if (published)
                    {
                        publishedCount++;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "⚠️ Failed to publish message - Row: {RowNumber}, Email: {Email}",
                            row.RowNumber, row.Email);
                    }
                }

                if (publishedCount == 0)
                {
                    bulkJob.Status = "Failed";
                    _bulkJobRepository.Update(bulkJob);
                    await _bulkJobRepository.SaveChangesAsync();

                    return new ErrorDataResult<BulkInvitationJobDto>(
                        "Hiçbir mesaj kuyruğa gönderilemedi. Lütfen tekrar deneyin.");
                }

                // Update job status to Processing
                bulkJob.Status = "Processing";
                bulkJob.StartedDate = DateTime.Now;
                _bulkJobRepository.Update(bulkJob);
                await _bulkJobRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ Published {PublishedCount}/{TotalCount} messages to RabbitMQ - JobId: {JobId}",
                    publishedCount, rows.Count, bulkJob.Id);

                // 7. Return response
                var response = new BulkInvitationJobDto
                {
                    JobId = bulkJob.Id,
                    TotalDealers = bulkJob.TotalDealers,
                    Status = bulkJob.Status,
                    CreatedDate = bulkJob.CreatedDate,
                    StatusCheckUrl = $"/api/v1/sponsorship/dealer/bulk-status/{bulkJob.Id}"
                };

                return new SuccessDataResult<BulkInvitationJobDto>(
                    response,
                    $"Toplu davet işlemi başlatıldı. {publishedCount} dealer kuyruğa eklendi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in QueueBulkInvitationsAsync - SponsorId: {SponsorId}", sponsorId);
                return new ErrorDataResult<BulkInvitationJobDto>("Toplu davet işlemi başlatılamadı.");
            }
        }

        private IResult ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return new ErrorResult("Dosya yüklenmedi.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return new ErrorResult($"Dosya boyutu çok büyük. Maksimum: {MaxFileSizeBytes / (1024 * 1024)} MB");
            }

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                return new ErrorResult("Geçersiz dosya formatı. Sadece .xlsx ve .xls desteklenir.");
            }

            return new SuccessResult();
        }

        private async Task<List<DealerInvitationRow>> ParseExcelAsync(IFormFile file)
        {
            var rows = new List<DealerInvitationRow>();

            // EPPlus license is set globally in Startup.cs
            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension?.End.Row ?? 0;
            var colCount = worksheet.Dimension?.End.Column ?? 0;

            // 🔥 HEADER-BASED PARSING: Map column names to column indices
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int col = 1; col <= colCount; col++)
            {
                var headerName = worksheet.Cells[1, col].Text?.Trim();
                if (!string.IsNullOrWhiteSpace(headerName))
                {
                    headers[headerName] = col;
                }
            }

            // Validate required columns
            if (!headers.ContainsKey("Email"))
            {
                throw new Exception("Excel'de 'Email' sütunu zorunludur");
            }

            if (!headers.ContainsKey("Phone"))
            {
                throw new Exception("Excel'de 'Phone' sütunu zorunludur");
            }

            // PackageTier is OPTIONAL - if not provided, auto-allocation will be used
            // (same behavior as single dealer invitations)

            if (!headers.ContainsKey("CodeCount"))
            {
                throw new Exception("Excel'de 'CodeCount' sütunu zorunludur");
            }

            _logger.LogInformation(
                "📊 Excel headers found: {Headers}",
                string.Join(", ", headers.Keys));

            // Row 1 is header, start from row 2
            for (int row = 2; row <= rowCount; row++)
            {
                var email = worksheet.Cells[row, headers["Email"]].Text?.Trim();
                var phone = worksheet.Cells[row, headers["Phone"]].Text?.Trim();
                
                // PackageTier is optional - if not provided, auto-allocation will be used
                var tier = headers.ContainsKey("PackageTier")
                    ? worksheet.Cells[row, headers["PackageTier"]].Text?.Trim()
                    : null;
                
                var codeCountText = worksheet.Cells[row, headers["CodeCount"]].Text?.Trim();
                
                // DealerName is optional
                var dealerName = headers.ContainsKey("DealerName")
                    ? worksheet.Cells[row, headers["DealerName"]].Text?.Trim()
                    : null;

                // Skip empty rows
                if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
                {
                    continue;
                }

                // Parse CodeCount
                if (!int.TryParse(codeCountText, out var codeCount) || codeCount <= 0)
                {
                    throw new Exception($"Satır {row}: CodeCount geçersiz veya boş - '{codeCountText}'");
                }

                // Validate PackageTier if provided
                string normalizedTier = null;
                if (!string.IsNullOrWhiteSpace(tier))
                {
                    normalizedTier = tier.ToUpper();
                    if (!new[] { "S", "M", "L", "XL" }.Contains(normalizedTier))
                    {
                        throw new Exception($"Satır {row}: PackageTier geçersiz - '{tier}'. S, M, L veya XL olmalı.");
                    }
                }

                var invitationRow = new DealerInvitationRow
                {
                    RowNumber = row,
                    Email = email,
                    Phone = phone,
                    DealerName = dealerName,
                    CodeCount = codeCount,
                    PackageTier = normalizedTier
                };

                rows.Add(invitationRow);
            }

            return rows;
        }

        private async Task<IResult> ValidateRowsAsync(List<DealerInvitationRow> rows, int sponsorId)
        {
            var errors = new List<string>();
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var phones = new HashSet<string>();

            foreach (var row in rows)
            {
                // Email validation
                if (string.IsNullOrWhiteSpace(row.Email) || !IsValidEmail(row.Email))
                {
                    errors.Add($"Satır {row.RowNumber}: Geçersiz email - {row.Email}");
                    continue;
                }

                // Phone validation
                if (string.IsNullOrWhiteSpace(row.Phone) || !IsValidPhone(row.Phone))
                {
                    errors.Add($"Satır {row.RowNumber}: Geçersiz telefon - {row.Phone}");
                    continue;
                }

                // DealerName validation
                if (string.IsNullOrWhiteSpace(row.DealerName) || row.DealerName.Length > 200)
                {
                    errors.Add($"Satır {row.RowNumber}: Geçersiz dealer ismi");
                    continue;
                }

                // Duplicate check (in file)
                if (emails.Contains(row.Email))
                {
                    errors.Add($"Satır {row.RowNumber}: Duplicate email - {row.Email}");
                    continue;
                }

                if (phones.Contains(row.Phone))
                {
                    errors.Add($"Satır {row.RowNumber}: Duplicate telefon - {row.Phone}");
                    continue;
                }

                emails.Add(row.Email);
                phones.Add(row.Phone);
            }

            if (errors.Any())
            {
                return new ErrorResult("Geçersiz satırlar:\n" + string.Join("\n", errors));
            }

            return new SuccessResult();
        }


        private async Task<IResult> CheckCodeAvailabilityAsync(
            List<DealerInvitationRow> rows,
            int sponsorId)
        {
            // Check if using auto-allocation mode (no tier specified for any row)
            var hasAnyTierSpecified = rows.Any(r => !string.IsNullOrWhiteSpace(r.PackageTier));

            if (!hasAnyTierSpecified)
            {
                // AUTO-ALLOCATION MODE: Check total available codes across all tiers
                // (same behavior as single dealer invitations)
                var totalRequired = rows.Sum(r => r.CodeCount.Value);

                var availableCodes = await _codeRepository.GetListAsync(c =>
                    c.SponsorId == sponsorId &&
                    !c.IsUsed &&
                    c.DealerId == null &&
                    c.ReservedForInvitationId == null &&
                    c.ExpiryDate > DateTime.Now);

                var availableCount = availableCodes.Count();

                _logger.LogInformation(
                    "🔄 Auto-allocation mode: {Required} kod gerekli, {Available} kod mevcut (tüm tier'lar)",
                    totalRequired, availableCount);

                if (availableCount < totalRequired)
                {
                    return new ErrorResult(
                        $"Yetersiz kod. Gerekli: {totalRequired}, Mevcut: {availableCount} (tüm tier'lar)");
                }

                return new SuccessResult();
            }
            else
            {
                // PER-TIER MODE: Check availability for each specified tier
                var allTiers = await _tierRepository.GetListAsync(t => t.IsActive);
                var tierNameToId = allTiers.ToDictionary(t => t.TierName.ToUpper(), t => t.Id);

                // Group rows by PackageTier and calculate required codes per tier
                var tierRequirements = rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.PackageTier))  // Only rows with tier specified
                    .GroupBy(r => r.PackageTier)
                    .Select(g => new
                    {
                        TierName = g.Key,
                        TierId = tierNameToId.ContainsKey(g.Key) ? tierNameToId[g.Key] : (int?)null,
                        RequiredCount = g.Sum(r => r.CodeCount.Value)
                    })
                    .ToList();

                // Check if there are rows without tier (mixed mode - not supported)
                var rowsWithoutTier = rows.Count(r => string.IsNullOrWhiteSpace(r.PackageTier));
                if (rowsWithoutTier > 0)
                {
                    return new ErrorResult(
                        $"Karma mod desteklenmiyor. Tüm satırlar tier belirtmeli veya hiçbiri belirtmemeli. " +
                        $"{rowsWithoutTier} satırda tier eksik.");
                }

                _logger.LogInformation(
                    "📊 Tier requirements: {Requirements}",
                    string.Join(", ", tierRequirements.Select(t => $"{t.TierName}={t.RequiredCount}")));

                // Check availability for each tier
                var errors = new List<string>();

                foreach (var requirement in tierRequirements)
                {
                    if (!requirement.TierId.HasValue)
                    {
                        errors.Add($"{requirement.TierName} tier: Geçersiz tier adı");
                        continue;
                    }

                    // Get available codes for this tier and sponsor
                    var availableCodes = await _codeRepository.GetListAsync(c =>
                        c.SponsorId == sponsorId &&
                        c.SubscriptionTierId == requirement.TierId.Value &&
                        !c.IsUsed &&
                        c.DealerId == null &&
                        c.ReservedForInvitationId == null &&
                        c.ExpiryDate > DateTime.Now);

                    var availableCount = availableCodes.Count();

                    if (availableCount < requirement.RequiredCount)
                    {
                        errors.Add(
                            $"{requirement.TierName} tier: {availableCount} kod mevcut, " +
                            $"{requirement.RequiredCount} kod gerekli (Eksik: {requirement.RequiredCount - availableCount})");
                    }
                    else
                    {
                        _logger.LogInformation(
                            "✅ {Tier} tier: {Available} kod mevcut, {Required} kod gerekli",
                            requirement.TierName, availableCount, requirement.RequiredCount);
                    }
                }

                if (errors.Any())
                {
                    return new ErrorResult("Yetersiz kod:\n" + string.Join("\n", errors));
                }

                return new SuccessResult();
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            var normalized = phone.Replace(" ", "").Replace("-", "")
                                  .Replace("(", "").Replace(")", "");

            // Turkish formats: +905xx, 905xx, 05xx
            if (normalized.StartsWith("+90") && normalized.Length == 13) return true;
            if (normalized.StartsWith("90") && normalized.Length == 12) return true;
            if (normalized.StartsWith("0") && normalized.Length == 11) return true;

            return false;
        }
    }
}
