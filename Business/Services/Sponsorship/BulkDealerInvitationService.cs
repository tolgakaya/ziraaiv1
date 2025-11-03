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
            string defaultTier,
            int defaultCodeCount,
            bool sendSms,
            bool useRowSpecificCounts);
    }

    public class BulkDealerInvitationService : IBulkDealerInvitationService
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IBulkInvitationJobRepository _bulkJobRepository;
        private readonly ISponsorshipCodeRepository _codeRepository;
        private readonly IUserRepository _userRepository;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly ILogger<BulkDealerInvitationService> _logger;

        private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private const int MaxRowCount = 2000;

        public BulkDealerInvitationService(
            IMessageQueueService messageQueueService,
            IBulkInvitationJobRepository bulkJobRepository,
            ISponsorshipCodeRepository codeRepository,
            IUserRepository userRepository,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            ILogger<BulkDealerInvitationService> logger)
        {
            _messageQueueService = messageQueueService;
            _bulkJobRepository = bulkJobRepository;
            _codeRepository = codeRepository;
            _userRepository = userRepository;
            _rabbitMQOptions = rabbitMQOptions.Value;
            _logger = logger;
        }

        public async Task<IDataResult<BulkInvitationJobDto>> QueueBulkInvitationsAsync(
            IFormFile excelFile,
            int sponsorId,
            string invitationType,
            string defaultTier,
            int defaultCodeCount,
            bool sendSms,
            bool useRowSpecificCounts)
        {
            try
            {
                _logger.LogInformation(
                    "📤 Starting bulk invitation - SponsorId: {SponsorId}, Type: {Type}, Tier: {Tier}, CodeCount: {CodeCount}",
                    sponsorId, invitationType, defaultTier ?? "Any", defaultCodeCount);

                // 1. Validate file
                var fileValidation = ValidateFile(excelFile);
                if (!fileValidation.Success)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>(fileValidation.Message);
                }

                // 2. Parse Excel
                var rows = await ParseExcelAsync(excelFile, useRowSpecificCounts, defaultCodeCount, defaultTier);

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

                // 4. Check code availability
                var codeCheckResult = await CheckCodeAvailabilityAsync(rows, sponsorId, defaultTier, defaultCodeCount);
                if (!codeCheckResult.Success)
                {
                    return new ErrorDataResult<BulkInvitationJobDto>(codeCheckResult.Message);
                }

                // 5. Create BulkInvitationJob entity
                var bulkJob = new BulkInvitationJob
                {
                    SponsorId = sponsorId,
                    InvitationType = invitationType,
                    DefaultTier = defaultTier,
                    DefaultCodeCount = defaultCodeCount,
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
                        PackageTier = row.PackageTier ?? defaultTier,
                        CodeCount = row.CodeCount ?? defaultCodeCount,
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

        private async Task<List<DealerInvitationRow>> ParseExcelAsync(
            IFormFile file,
            bool useRowSpecificCounts,
            int defaultCodeCount,
            string defaultTier)
        {
            var rows = new List<DealerInvitationRow>();

            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension?.End.Row ?? 0;

            // Row 1 is header, start from row 2
            for (int row = 2; row <= rowCount; row++)
            {
                var email = worksheet.Cells[row, 1].Text?.Trim();
                var phone = worksheet.Cells[row, 2].Text?.Trim();
                var dealerName = worksheet.Cells[row, 3].Text?.Trim();
                var codeCountText = worksheet.Cells[row, 4].Text?.Trim();
                var tier = worksheet.Cells[row, 5].Text?.Trim();

                // Skip empty rows
                if (string.IsNullOrWhiteSpace(email) &&
                    string.IsNullOrWhiteSpace(phone) &&
                    string.IsNullOrWhiteSpace(dealerName))
                {
                    continue;
                }

                var invitationRow = new DealerInvitationRow
                {
                    RowNumber = row,
                    Email = email,
                    Phone = phone,
                    DealerName = dealerName,
                    CodeCount = useRowSpecificCounts && int.TryParse(codeCountText, out var count)
                        ? count
                        : (int?)null,
                    PackageTier = !string.IsNullOrWhiteSpace(tier) ? tier.ToUpper() : null
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
                return new ErrorResult(string.Join("\n", errors.Take(10)) +
                    (errors.Count > 10 ? $"\n... ve {errors.Count - 10} hata daha" : ""));
            }

            // Check existing dealers in database
            var existingDealers = await _userRepository.GetListAsync(u =>
                emails.Contains(u.Email));

            if (existingDealers.Any())
            {
                var existingEmails = string.Join(", ", existingDealers.Select(u => u.Email).Take(5));
                return new ErrorResult($"Bu email adresleri zaten kullanılıyor: {existingEmails}");
            }

            return new SuccessResult();
        }

        private async Task<IResult> CheckCodeAvailabilityAsync(
            List<DealerInvitationRow> rows,
            int sponsorId,
            string defaultTier,
            int defaultCodeCount)
        {
            // Calculate total required codes
            var totalRequired = 0;
            foreach (var row in rows)
            {
                totalRequired += row.CodeCount ?? defaultCodeCount;
            }

            // Get available codes for this sponsor
            System.Linq.Expressions.Expression<Func<SponsorshipCode, bool>> predicate = c =>
                c.SponsorId == sponsorId &&
                !c.IsUsed &&
                c.DealerId == null &&
                c.ReservedForInvitationId == null &&
                c.ExpiryDate > DateTime.Now;
            var availableCodes = await _codeRepository.GetListAsync(predicate);

            var availableCount = availableCodes.Count();

            if (availableCount < totalRequired)
            {
                return new ErrorResult(
                    $"Yetersiz kod. Gerekli: {totalRequired}, Mevcut: {availableCount}");
            }

            return new SuccessResult();
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
