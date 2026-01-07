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
    public class BulkFarmerInvitationService : IBulkFarmerInvitationService
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IBulkInvitationJobRepository _bulkJobRepository;
        private readonly ISponsorshipCodeRepository _codeRepository;
        private readonly ISubscriptionTierRepository _tierRepository;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly ILogger<BulkFarmerInvitationService> _logger;

        private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private const int MaxRowCount = 2000;

        public BulkFarmerInvitationService(
            IMessageQueueService messageQueueService,
            IBulkInvitationJobRepository bulkJobRepository,
            ISponsorshipCodeRepository codeRepository,
            ISubscriptionTierRepository tierRepository,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            ILogger<BulkFarmerInvitationService> logger)
        {
            _messageQueueService = messageQueueService;
            _bulkJobRepository = bulkJobRepository;
            _codeRepository = codeRepository;
            _tierRepository = tierRepository;
            _rabbitMQOptions = rabbitMQOptions.Value;
            _logger = logger;
        }

        public async Task<IDataResult<BulkInvitationJobDto>> QueueBulkInvitationsAsync(
            IFormFile excelFile,
            int sponsorId,
            string channel,
            string customMessage)
        {
            try
            {
                _logger.LogInformation(
                    "📤 [FARMER_BULK] Starting bulk farmer invitation - SponsorId: {SponsorId}, Channel: {Channel}",
                    sponsorId, channel);

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
                        $"Maksimum {MaxRowCount} çiftçi kaydı yüklenebilir. Dosyanızda {rows.Count} kayıt var.");
                }

                // 3. Validate rows
                var validationResult = await ValidateRowsAsync(rows, sponsorId);
                if (!validationResult.Success)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>(validationResult.Message);
                }

                // 4. Check code availability (1 code per farmer)
                var codeCheckResult = await CheckCodeAvailabilityAsync(rows, sponsorId);
                if (!codeCheckResult.Success)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>(codeCheckResult.Message);
                }

                // 5. Create BulkInvitationJob entity (same table as dealer)
                var bulkJob = new BulkInvitationJob
                {
                    SponsorId = sponsorId,
                    InvitationType = "FarmerInvite",  // Distinguish from dealer
                    DefaultTier = null,  // Not used
                    DefaultCodeCount = 1,  // Always 1 for farmers
                    SendSms = channel.ToLower() != "whatsapp",  // SMS if not WhatsApp
                    TotalDealers = rows.Count,  // Reusing dealer field name
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
                    "✅ [FARMER_BULK] BulkInvitationJob created - JobId: {JobId}, TotalFarmers: {TotalFarmers}",
                    bulkJob.Id, bulkJob.TotalDealers);

                // 6. Publish messages to RabbitMQ (one per farmer)
                var queueName = _rabbitMQOptions.Queues.FarmerInvitationRequest;
                var publishedCount = 0;

                foreach (var row in rows)
                {
                    var queueMessage = new FarmerInvitationQueueMessage
                    {
                        CorrelationId = bulkJob.Id.ToString(),
                        RowNumber = row.RowNumber,
                        BulkJobId = bulkJob.Id,
                        SponsorId = sponsorId,
                        Phone = row.Phone,
                        FarmerName = row.FarmerName,
                        Email = row.Email,
                        PackageTier = row.PackageTier,  // Optional: null for auto-allocation
                        Notes = row.Notes,
                        Channel = channel,
                        CustomMessage = customMessage,
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
                            "⚠️ [FARMER_BULK] Failed to publish message - Row: {RowNumber}, Phone: {Phone}",
                            row.RowNumber, row.Phone);
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
                    "✅ [FARMER_BULK] Published {PublishedCount}/{TotalCount} messages to RabbitMQ - JobId: {JobId}",
                    publishedCount, rows.Count, bulkJob.Id);

                // 7. Return response
                var response = new BulkInvitationJobDto
                {
                    JobId = bulkJob.Id,
                    TotalDealers = bulkJob.TotalDealers,  // Reusing field
                    Status = bulkJob.Status,
                    CreatedDate = bulkJob.CreatedDate,
                    StatusCheckUrl = $"/api/v1/sponsorship/farmer/bulk-status/{bulkJob.Id}"
                };

                return new SuccessDataResult<BulkInvitationJobDto>(
                    response,
                    $"Toplu davet işlemi başlatıldı. {publishedCount} çiftçi kuyruğa eklendi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [FARMER_BULK] Error in QueueBulkInvitationsAsync - SponsorId: {SponsorId}", sponsorId);
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

        private async Task<List<FarmerInvitationRow>> ParseExcelAsync(IFormFile file)
        {
            var rows = new List<FarmerInvitationRow>();

            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension?.End.Row ?? 0;
            var colCount = worksheet.Dimension?.End.Column ?? 0;

            // Header-based parsing: Map column names to indices
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
            if (!headers.ContainsKey("Phone"))
            {
                throw new Exception("Excel'de 'Phone' sütunu zorunludur");
            }

            // FarmerName, Email, PackageTier, Notes are all OPTIONAL

            _logger.LogInformation(
                "📊 [FARMER_BULK] Excel headers found: {Headers}",
                string.Join(", ", headers.Keys));

            // Row 1 is header, start from row 2
            for (int row = 2; row <= rowCount; row++)
            {
                var phone = worksheet.Cells[row, headers["Phone"]].Text?.Trim();

                // Optional fields
                var farmerName = headers.ContainsKey("FarmerName")
                    ? worksheet.Cells[row, headers["FarmerName"]].Text?.Trim()
                    : null;

                var email = headers.ContainsKey("Email")
                    ? worksheet.Cells[row, headers["Email"]].Text?.Trim()
                    : null;

                var tier = headers.ContainsKey("PackageTier")
                    ? worksheet.Cells[row, headers["PackageTier"]].Text?.Trim()
                    : null;

                var notes = headers.ContainsKey("Notes")
                    ? worksheet.Cells[row, headers["Notes"]].Text?.Trim()
                    : null;

                // Skip empty rows
                if (string.IsNullOrWhiteSpace(phone))
                {
                    continue;
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

                var invitationRow = new FarmerInvitationRow
                {
                    RowNumber = row,
                    Phone = NormalizePhone(phone),  // Normalize to 0XXXXXXXXXX format (11 digits)
                    FarmerName = farmerName,
                    Email = email,
                    PackageTier = normalizedTier,
                    Notes = notes
                };

                rows.Add(invitationRow);
            }

            return rows;
        }

        private async Task<IResult> ValidateRowsAsync(List<FarmerInvitationRow> rows, int sponsorId)
        {
            var errors = new List<string>();
            var phones = new HashSet<string>();

            foreach (var row in rows)
            {
                // Phone validation
                if (string.IsNullOrWhiteSpace(row.Phone) || !IsValidPhone(row.Phone))
                {
                    errors.Add($"Satır {row.RowNumber}: Geçersiz telefon - {row.Phone}");
                    continue;
                }

                // Email validation (optional)
                if (!string.IsNullOrWhiteSpace(row.Email) && !IsValidEmail(row.Email))
                {
                    errors.Add($"Satır {row.RowNumber}: Geçersiz email - {row.Email}");
                    continue;
                }

                // Duplicate check (in file)
                if (phones.Contains(row.Phone))
                {
                    errors.Add($"Satır {row.RowNumber}: Duplicate telefon - {row.Phone}");
                    continue;
                }

                phones.Add(row.Phone);
            }

            if (errors.Any())
            {
                return new ErrorResult("Geçersiz satırlar:\n" + string.Join("\n", errors));
            }

            return new SuccessResult();
        }

        private async Task<IResult> CheckCodeAvailabilityAsync(
            List<FarmerInvitationRow> rows,
            int sponsorId)
        {
            _logger.LogInformation("🔍 [DEBUG] CheckCodeAvailabilityAsync called with sponsorId={SponsorId}", sponsorId);
            
            // Each farmer gets exactly 1 code
            var hasAnyTierSpecified = rows.Any(r => !string.IsNullOrWhiteSpace(r.PackageTier));

            if (!hasAnyTierSpecified)
            {
                // AUTO-ALLOCATION MODE: Check total available codes across all tiers
                var totalRequired = rows.Count;  // 1 code per farmer

                var availableCodes = await _codeRepository.GetListAsync(c =>
                    c.SponsorId == sponsorId &&
                    !c.IsUsed &&
                    c.FarmerInvitationId == null &&
                    c.DealerId == null &&
                    c.ReservedForInvitationId == null &&
                    c.ReservedForFarmerInvitationId == null &&
                    c.ExpiryDate > DateTime.Now);

                var availableCount = availableCodes.Count();

                _logger.LogInformation(
                    "🔄 [FARMER_BULK] Auto-allocation mode: {Required} kod gerekli, {Available} kod mevcut (tüm tier'lar)",
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

                // Group rows by PackageTier and count
                var tierRequirements = rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.PackageTier))
                    .GroupBy(r => r.PackageTier)
                    .Select(g => new
                    {
                        TierName = g.Key,
                        TierId = tierNameToId.ContainsKey(g.Key) ? tierNameToId[g.Key] : (int?)null,
                        RequiredCount = g.Count()  // 1 code per farmer in this tier
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
                    "📊 [FARMER_BULK] Tier requirements: {Requirements}",
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
                        c.FarmerInvitationId == null &&
                        c.DealerId == null &&
                        c.ReservedForInvitationId == null &&
                        c.ReservedForFarmerInvitationId == null &&
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
                            "✅ [FARMER_BULK] {Tier} tier: {Available} kod mevcut, {Required} kod gerekli",
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
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Remove all formatting
            var cleaned = phone.Replace(" ", "").Replace("-", "")
                               .Replace("(", "").Replace(")", "")
                               .Replace(".", "");

            // Remove leading + if present
            if (cleaned.StartsWith("+"))
                cleaned = cleaned.Substring(1);

            // Valid Turkish phone formats:
            // 905xxxxxxxxx (12 digits) - will be converted to 0xxxxxxxxxx
            // 0xxxxxxxxxx (11 digits) - target format
            // 5xxxxxxxxx (10 digits) - will be converted to 0xxxxxxxxxx

            if (cleaned.Length == 12 && cleaned.StartsWith("90"))
            {
                return cleaned.Substring(2, 1) == "5"; // Must be mobile (5xx)
            }

            if (cleaned.Length == 11 && cleaned.StartsWith("0"))
            {
                return cleaned.Substring(1, 1) == "5"; // Must be mobile (5xx)
            }

            if (cleaned.Length == 10 && cleaned.StartsWith("5"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Normalizes Turkish phone number to 0XXXXXXXXXX format (11 digits)
        /// Accepts formats: +90 506 946 86 93, 0506 946 86 93, 506 946 86 93, 5069468693
        /// Returns: 05069468693
        /// </summary>
        private string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            // Remove all formatting
            var cleaned = phone.Replace(" ", "").Replace("-", "")
                               .Replace("(", "").Replace(")", "")
                               .Replace(".", "");

            // Remove leading + if present
            if (cleaned.StartsWith("+"))
                cleaned = cleaned.Substring(1);

            // Normalize to 0XXXXXXXXXX format (11 digits)
            if (cleaned.Length == 12 && cleaned.StartsWith("90"))
            {
                // 905xxxxxxxxx → 0xxxxxxxxxx
                return "0" + cleaned.Substring(2);
            }

            if (cleaned.Length == 11 && cleaned.StartsWith("0"))
            {
                // Already in 0xxxxxxxxxx format
                return cleaned;
            }

            if (cleaned.Length == 10 && cleaned.StartsWith("5"))
            {
                // 5xxxxxxxxx → 0xxxxxxxxxx
                return "0" + cleaned;
            }

            // Return as-is if format not recognized (will fail validation)
            return cleaned;
        }
    }

    /// <summary>
    /// Farmer invitation row from Excel
    /// CodeCount is always 1 (not in row, hardcoded in handler)
    /// </summary>
    public class FarmerInvitationRow
    {
        public int RowNumber { get; set; }
        public string Phone { get; set; }
        public string FarmerName { get; set; }
        public string Email { get; set; }
        public string PackageTier { get; set; }  // S, M, L, XL or null
        public string Notes { get; set; }
        // CodeCount is always 1 - not stored in row
    }
}
