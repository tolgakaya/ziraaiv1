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
    public interface IBulkCodeDistributionService
    {
        Task<IDataResult<BulkCodeDistributionJobDto>> QueueBulkCodeDistributionAsync(
            IFormFile excelFile,
            int sponsorId,
            bool sendSms);
    }

    public class BulkCodeDistributionService : IBulkCodeDistributionService
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IBulkCodeDistributionJobRepository _bulkJobRepository;
        private readonly ISponsorshipCodeRepository _codeRepository;
        private readonly ISponsorshipPurchaseRepository _purchaseRepository;
        private readonly IUserRepository _userRepository;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly ILogger<BulkCodeDistributionService> _logger;

        private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private const int MaxRowCount = 2000;

        public BulkCodeDistributionService(
            IMessageQueueService messageQueueService,
            IBulkCodeDistributionJobRepository bulkJobRepository,
            ISponsorshipCodeRepository codeRepository,
            ISponsorshipPurchaseRepository purchaseRepository,
            IUserRepository userRepository,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            ILogger<BulkCodeDistributionService> logger)
        {
            _messageQueueService = messageQueueService;
            _bulkJobRepository = bulkJobRepository;
            _codeRepository = codeRepository;
            _purchaseRepository = purchaseRepository;
            _userRepository = userRepository;
            _rabbitMQOptions = rabbitMQOptions.Value;
            _logger = logger;
        }

        public async Task<IDataResult<BulkCodeDistributionJobDto>> QueueBulkCodeDistributionAsync(
            IFormFile excelFile,
            int sponsorId,
            bool sendSms)
        {
            try
            {
                _logger.LogInformation(
                    "📤 Starting bulk farmer code distribution - SponsorId: {SponsorId}, SendSms: {SendSms}",
                    sponsorId, sendSms);

                // 1. Validate file
                var fileValidation = ValidateFile(excelFile);
                if (!fileValidation.Success)
                {
                    return new ErrorDataResult<BulkCodeDistributionJobDto>(fileValidation.Message);
                }

                // 2. Find latest purchase with available codes
                var purchaseResult = await FindLatestPurchaseWithAvailableCodesAsync(sponsorId);
                if (!purchaseResult.Success)
                {
                    return new ErrorDataResult<BulkCodeDistributionJobDto>(purchaseResult.Message);
                }

                var purchase = purchaseResult.Data;
                var purchaseId = purchase.Id;

                // 3. Parse Excel (header-based)
                var rows = await ParseExcelAsync(excelFile);

                if (rows.Count == 0)
                {
                    return new ErrorDataResult<BulkCodeDistributionJobDto>("Excel dosyasında geçerli satır bulunamadı.");
                }

                if (rows.Count > MaxRowCount)
                {
                    return new ErrorDataResult<BulkCodeDistributionJobDto>(
                        $"Maksimum {MaxRowCount} farmer kaydı yüklenebilir. Dosyanızda {rows.Count} kayıt var.");
                }

                // 4. Validate rows (email/phone format, duplicates)
                var rowValidation = await ValidateRowsAsync(rows, sponsorId);
                if (!rowValidation.Success)
                {
                    return new ErrorDataResult<BulkCodeDistributionJobDto>(rowValidation.Message);
                }

                // 5. Calculate total codes needed and check availability
                var totalCodesNeeded = CalculateTotalCodesNeeded(rows);
                var codeCheckResult = await CheckCodeAvailabilityAsync(purchaseId, sponsorId, totalCodesNeeded);
                if (!codeCheckResult.Success)
                {
                    return new ErrorDataResult<BulkCodeDistributionJobDto>(codeCheckResult.Message);
                }

                var availableCodesCount = codeCheckResult.Data;

                // 6. Create BulkCodeDistributionJob entity
                var bulkJob = new BulkCodeDistributionJob
                {
                    SponsorId = sponsorId,
                    PurchaseId = purchaseId,
                    SendSms = sendSms,
                    DeliveryMethod = sendSms ? "Both" : "Direct",
                    TotalFarmers = rows.Count,
                    ProcessedFarmers = 0,
                    SuccessfulDistributions = 0,
                    FailedDistributions = 0,
                    Status = "Pending",
                    CreatedDate = DateTime.Now,
                    OriginalFileName = excelFile.FileName,
                    FileSize = (int)excelFile.Length,
                    TotalCodesDistributed = 0,
                    TotalSmsSent = 0
                };

                _bulkJobRepository.Add(bulkJob);
                await _bulkJobRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ BulkCodeDistributionJob created - JobId: {JobId}, TotalFarmers: {TotalFarmers}, TotalCodes: {TotalCodes}",
                    bulkJob.Id, bulkJob.TotalFarmers, totalCodesNeeded);

                // 7. Publish messages to RabbitMQ (one per farmer)
                var queueName = _rabbitMQOptions.Queues.FarmerCodeDistributionRequest; // Will need to add this to config
                var publishedCount = 0;

                foreach (var row in rows)
                {
                    var queueMessage = new FarmerCodeDistributionQueueMessage
                    {
                        CorrelationId = bulkJob.Id.ToString(),
                        RowNumber = row.RowNumber,
                        BulkJobId = bulkJob.Id,
                        SponsorId = sponsorId,
                        PurchaseId = purchaseId,
                        Email = row.Email,
                        Phone = row.Phone,
                        FarmerName = row.FarmerName,
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
                    bulkJob.ErrorSummary = "Hiçbir mesaj kuyruğa gönderilemedi";
                    _bulkJobRepository.Update(bulkJob);
                    await _bulkJobRepository.SaveChangesAsync();

                    return new ErrorDataResult<BulkCodeDistributionJobDto>(
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

                // 8. Return response
                var response = new BulkCodeDistributionJobDto
                {
                    JobId = bulkJob.Id,
                    TotalFarmers = bulkJob.TotalFarmers,
                    TotalCodesRequired = totalCodesNeeded,
                    AvailableCodes = availableCodesCount,
                    Status = bulkJob.Status,
                    CreatedDate = bulkJob.CreatedDate,
                    EstimatedCompletionTime = DateTime.Now.AddMinutes(bulkJob.TotalFarmers * 0.5), // ~30s per farmer
                    StatusCheckUrl = $"/api/v1/sponsorship/bulk-code-distribution/status/{bulkJob.Id}"
                };

                return new SuccessDataResult<BulkCodeDistributionJobDto>(
                    response,
                    $"Toplu kod dağıtım işlemi başlatıldı. {publishedCount} farmer kuyruğa eklendi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in QueueBulkCodeDistributionAsync - SponsorId: {SponsorId}", sponsorId);
                return new ErrorDataResult<BulkCodeDistributionJobDto>("Toplu kod dağıtım işlemi başlatılamadı.");
            }
        }

        #region Validation Methods

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

        private async Task<IDataResult<SponsorshipPurchase>> FindLatestPurchaseWithAvailableCodesAsync(int sponsorId)
        {
            // Get all completed purchases for this sponsor
            var completedPurchases = await _purchaseRepository.GetListAsync(p =>
                p.SponsorId == sponsorId &&
                p.PaymentStatus == "Completed");

            if (!completedPurchases.Any())
            {
                return new ErrorDataResult<SponsorshipPurchase>(
                    "Ödeme tamamlanmış satın alma bulunamadı. Lütfen önce sponsorluk paketi satın alın.");
            }

            // For each purchase, check if it has available codes
            foreach (var purchase in completedPurchases.OrderByDescending(p => p.PurchaseDate))
            {
                var availableCodes = await _codeRepository.GetListAsync(c =>
                    c.SponsorId == sponsorId &&
                    c.SponsorshipPurchaseId == purchase.Id &&
                    !c.IsUsed &&
                    c.UsedByUserId == null &&
                    c.DealerId == null &&
                    c.DistributionDate == null &&
                    c.ExpiryDate > DateTime.Now);

                if (availableCodes.Any())
                {
                    _logger.LogInformation(
                        "✅ Auto-selected purchase {PurchaseId} with {AvailableCount} available codes",
                        purchase.Id, availableCodes.Count());
                    return new SuccessDataResult<SponsorshipPurchase>(purchase);
                }
            }

            return new ErrorDataResult<SponsorshipPurchase>(
                "Kullanılabilir kodu olan satın alma bulunamadı. Lütfen yeni sponsorluk paketi satın alın veya mevcut kodları kontrol edin.");
        }

        private async Task<IDataResult<SponsorshipPurchase>> ValidatePurchaseAsync(int purchaseId, int sponsorId)
        {
            var purchase = await _purchaseRepository.GetAsync(p => p.Id == purchaseId);

            if (purchase == null)
            {
                return new ErrorDataResult<SponsorshipPurchase>("Satın alma kaydı bulunamadı.");
            }

            if (purchase.SponsorId != sponsorId)
            {
                return new ErrorDataResult<SponsorshipPurchase>("Bu satın alma kaydına erişim yetkiniz yok.");
            }

            if (purchase.PaymentStatus != "Completed")
            {
                return new ErrorDataResult<SponsorshipPurchase>(
                    "Sadece ödeme tamamlanmış satın almalardan kod dağıtımı yapılabilir.");
            }

            return new SuccessDataResult<SponsorshipPurchase>(purchase);
        }

        private async Task<List<FarmerCodeDistributionRow>> ParseExcelAsync(IFormFile file)
        {
            var rows = new List<FarmerCodeDistributionRow>();

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

            _logger.LogInformation(
                "📊 Excel headers found: {Headers}",
                string.Join(", ", headers.Keys));

            // Row 1 is header, start from row 2
            for (int row = 2; row <= rowCount; row++)
            {
                var email = worksheet.Cells[row, headers["Email"]].Text?.Trim();
                var phone = worksheet.Cells[row, headers["Phone"]].Text?.Trim();

                // FarmerName is optional
                var farmerName = headers.ContainsKey("FarmerName")
                    ? worksheet.Cells[row, headers["FarmerName"]].Text?.Trim()
                    : null;

                // Skip empty rows
                if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
                {
                    continue;
                }

                var distributionRow = new FarmerCodeDistributionRow
                {
                    RowNumber = row,
                    Email = email,
                    Phone = NormalizePhone(phone),
                    FarmerName = farmerName
                };

                rows.Add(distributionRow);
            }

            return rows;
        }

        private async Task<IResult> ValidateRowsAsync(List<FarmerCodeDistributionRow> rows, int sponsorId)
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

                // FarmerName validation (if provided)
                if (!string.IsNullOrWhiteSpace(row.FarmerName) && row.FarmerName.Length > 200)
                {
                    errors.Add($"Satır {row.RowNumber}: Farmer ismi çok uzun (max 200 karakter)");
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
                return new ErrorResult("Geçersiz satırlar:\n" + string.Join("\n", errors.Take(10)));
            }

            return new SuccessResult();
        }

        private int CalculateTotalCodesNeeded(List<FarmerCodeDistributionRow> rows)
        {
            // Each farmer gets exactly 1 code
            return rows.Count;
        }

        private async Task<IDataResult<int>> CheckCodeAvailabilityAsync(
            int purchaseId,
            int sponsorId,
            int totalCodesNeeded)
        {
            // Get available codes from this specific purchase
            var availableCodes = await _codeRepository.GetListAsync(c =>
                c.SponsorId == sponsorId &&
                c.SponsorshipPurchaseId == purchaseId &&
                !c.IsUsed &&
                c.UsedByUserId == null &&
                c.DealerId == null &&
                c.DistributionDate == null &&
                c.ExpiryDate > DateTime.Now);

            var availableCount = availableCodes.Count();

            _logger.LogInformation(
                "📊 Code availability check: {Required} kod gerekli, {Available} kod mevcut (PurchaseId: {PurchaseId})",
                totalCodesNeeded, availableCount, purchaseId);

            if (availableCount < totalCodesNeeded)
            {
                return new ErrorDataResult<int>(
                    $"Yetersiz kod. Gerekli: {totalCodesNeeded}, Mevcut: {availableCount}");
            }

            return new SuccessDataResult<int>(availableCount);
        }

        #endregion

        #region Helper Methods

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

            var cleaned = phone.Replace(" ", "").Replace("-", "")
                               .Replace("(", "").Replace(")", "")
                               .Replace(".", "");

            if (cleaned.StartsWith("+"))
                cleaned = cleaned.Substring(1);

            if (cleaned.Length == 12 && cleaned.StartsWith("90"))
            {
                return cleaned.Substring(2, 1) == "5";
            }

            if (cleaned.Length == 11 && cleaned.StartsWith("0"))
            {
                return cleaned.Substring(1, 1) == "5";
            }

            if (cleaned.Length == 10 && cleaned.StartsWith("5"))
            {
                return true;
            }

            return false;
        }

        private string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            var cleaned = phone.Replace(" ", "").Replace("-", "")
                               .Replace("(", "").Replace(")", "")
                               .Replace(".", "");

            if (cleaned.StartsWith("+"))
                cleaned = cleaned.Substring(1);

            // Turkish format normalization to 05XXXXXXXXX (local format)
            // +905321234567 → 05321234567
            if (cleaned.Length == 12 && cleaned.StartsWith("90"))
            {
                return "0" + cleaned.Substring(2);
            }

            // 05321234567 → 05321234567 (already correct)
            if (cleaned.Length == 11 && cleaned.StartsWith("0"))
            {
                return cleaned;
            }

            // 5321234567 → 05321234567
            if (cleaned.Length == 10 && cleaned.StartsWith("5"))
            {
                return "0" + cleaned;
            }

            return cleaned;
        }

        #endregion
    }

    #region Helper Classes

    public class FarmerCodeDistributionRow
    {
        public int RowNumber { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FarmerName { get; set; }
    }

    public class FarmerCodeDistributionQueueMessage
    {
        public string CorrelationId { get; set; }
        public int RowNumber { get; set; }
        public int BulkJobId { get; set; }
        public int SponsorId { get; set; }
        public int PurchaseId { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FarmerName { get; set; }
        public bool SendSms { get; set; }
        public DateTime QueuedAt { get; set; }
    }

    #endregion
}
