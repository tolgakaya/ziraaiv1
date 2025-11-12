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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Business.Services.Admin
{
    public interface IBulkSubscriptionAssignmentService
    {
        Task<IDataResult<BulkSubscriptionAssignmentJobDto>> QueueBulkSubscriptionAssignmentAsync(
            IFormFile excelFile,
            int adminId,
            int? defaultTierId,
            int? defaultDurationDays,
            bool sendNotification,
            string notificationMethod,
            bool autoActivate);
    }

    public class BulkSubscriptionAssignmentService : IBulkSubscriptionAssignmentService
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IBulkSubscriptionAssignmentJobRepository _bulkJobRepository;
        private readonly ISubscriptionTierRepository _tierRepository;
        private readonly IUserRepository _userRepository;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly ILogger<BulkSubscriptionAssignmentService> _logger;

        private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private const int MaxRowCount = 2000;

        public BulkSubscriptionAssignmentService(
            IMessageQueueService messageQueueService,
            IBulkSubscriptionAssignmentJobRepository bulkJobRepository,
            ISubscriptionTierRepository tierRepository,
            IUserRepository userRepository,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            ILogger<BulkSubscriptionAssignmentService> logger)
        {
            _messageQueueService = messageQueueService;
            _bulkJobRepository = bulkJobRepository;
            _tierRepository = tierRepository;
            _userRepository = userRepository;
            _rabbitMQOptions = rabbitMQOptions.Value;
            _logger = logger;
        }

        public async Task<IDataResult<BulkSubscriptionAssignmentJobDto>> QueueBulkSubscriptionAssignmentAsync(
            IFormFile excelFile,
            int adminId,
            int? defaultTierId,
            int? defaultDurationDays,
            bool sendNotification,
            string notificationMethod,
            bool autoActivate)
        {
            try
            {
                _logger.LogInformation(
                    "📤 Starting bulk subscription assignment - AdminId: {AdminId}, SendNotification: {SendNotification}",
                    adminId, sendNotification);

                // 1. Validate file
                var fileValidation = ValidateFile(excelFile);
                if (!fileValidation.Success)
                {
                    return new ErrorDataResult<BulkSubscriptionAssignmentJobDto>(fileValidation.Message);
                }

                // 2. Validate default tier if provided
                if (defaultTierId.HasValue)
                {
                    var tierValidation = await ValidateSubscriptionTierAsync(defaultTierId.Value);
                    if (!tierValidation.Success)
                    {
                        return new ErrorDataResult<BulkSubscriptionAssignmentJobDto>(tierValidation.Message);
                    }
                }

                // 3. Parse Excel (header-based)
                var rows = await ParseExcelAsync(excelFile, defaultTierId, defaultDurationDays);

                if (rows.Count == 0)
                {
                    return new ErrorDataResult<BulkSubscriptionAssignmentJobDto>("Excel dosyasında geçerli satır bulunamadı.");
                }

                if (rows.Count > MaxRowCount)
                {
                    return new ErrorDataResult<BulkSubscriptionAssignmentJobDto>(
                        $"Maksimum {MaxRowCount} farmer kaydı yüklenebilir. Dosyanızda {rows.Count} kayıt var.");
                }

                // 4. Validate rows (email/phone format, tier IDs, duplicates)
                var rowValidation = await ValidateRowsAsync(rows);
                if (!rowValidation.Success)
                {
                    return new ErrorDataResult<BulkSubscriptionAssignmentJobDto>(rowValidation.Message);
                }

                // 5. Create BulkSubscriptionAssignmentJob entity
                var bulkJob = new BulkSubscriptionAssignmentJob
                {
                    AdminId = adminId,
                    DefaultTierId = defaultTierId,
                    DefaultDurationDays = defaultDurationDays,
                    SendNotification = sendNotification,
                    NotificationMethod = notificationMethod ?? "Email",
                    AutoActivate = autoActivate,
                    TotalFarmers = rows.Count,
                    ProcessedFarmers = 0,
                    SuccessfulAssignments = 0,
                    FailedAssignments = 0,
                    Status = "Pending",
                    CreatedDate = DateTime.Now,
                    OriginalFileName = excelFile.FileName,
                    FileSize = (int)excelFile.Length,
                    NewSubscriptionsCreated = 0,
                    ExistingSubscriptionsUpdated = 0,
                    TotalNotificationsSent = 0
                };

                _bulkJobRepository.Add(bulkJob);
                await _bulkJobRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "✅ BulkSubscriptionAssignmentJob created - JobId: {JobId}, TotalFarmers: {TotalFarmers}",
                    bulkJob.Id, bulkJob.TotalFarmers);

                // 6. Publish messages to RabbitMQ (one per farmer)
                var queueName = _rabbitMQOptions.Queues.FarmerSubscriptionAssignmentRequest;
                var publishedCount = 0;

                foreach (var row in rows)
                {
                    var queueMessage = new FarmerSubscriptionAssignmentQueueMessage
                    {
                        CorrelationId = bulkJob.Id.ToString(),
                        RowNumber = row.RowNumber,
                        BulkJobId = bulkJob.Id,
                        AdminId = adminId,
                        Email = row.Email,
                        Phone = row.Phone,
                        FirstName = row.FirstName,
                        LastName = row.LastName,
                        SubscriptionTierId = row.SubscriptionTierId,
                        DurationDays = row.DurationDays,
                        SendNotification = sendNotification,
                        NotificationMethod = notificationMethod ?? "Email",
                        AutoActivate = autoActivate,
                        Notes = row.Notes,
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

                    return new ErrorDataResult<BulkSubscriptionAssignmentJobDto>(
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
                var response = new BulkSubscriptionAssignmentJobDto
                {
                    JobId = bulkJob.Id,
                    TotalFarmers = bulkJob.TotalFarmers,
                    Status = bulkJob.Status,
                    CreatedDate = bulkJob.CreatedDate,
                    EstimatedCompletionTime = DateTime.Now.AddMinutes(bulkJob.TotalFarmers * 0.5), // ~30s per farmer
                    StatusCheckUrl = $"/api/v1/admin/subscriptions/bulk-assignment/status/{bulkJob.Id}"
                };

                return new SuccessDataResult<BulkSubscriptionAssignmentJobDto>(
                    response,
                    $"Toplu subscription atama işlemi başlatıldı. {publishedCount} farmer kuyruğa eklendi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in QueueBulkSubscriptionAssignmentAsync - AdminId: {AdminId}", adminId);
                return new ErrorDataResult<BulkSubscriptionAssignmentJobDto>("Toplu subscription atama işlemi başlatılamadı.");
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

        private async Task<IResult> ValidateSubscriptionTierAsync(int tierId)
        {
            var tier = await _tierRepository.GetAsync(t => t.Id == tierId && t.IsActive);

            if (tier == null)
            {
                return new ErrorResult($"Geçersiz veya pasif subscription tier ID: {tierId}");
            }

            return new SuccessResult();
        }

        private async Task<List<FarmerSubscriptionAssignmentRow>> ParseExcelAsync(
            IFormFile file,
            int? defaultTierId,
            int? defaultDurationDays)
        {
            var rows = new List<FarmerSubscriptionAssignmentRow>();

            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension?.End.Row ?? 0;
            var colCount = worksheet.Dimension?.End.Column ?? 0;

            // HEADER-BASED PARSING: Map column names to column indices
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
            if (!headers.ContainsKey("Email") && !headers.ContainsKey("Phone"))
            {
                throw new Exception("Excel'de 'Email' veya 'Phone' sütunlarından en az biri zorunludur");
            }

            if (!headers.ContainsKey("TierName") && !defaultTierId.HasValue)
            {
                throw new Exception("Excel'de 'TierName' sütunu veya default tier ID belirtilmelidir");
            }

            if (!headers.ContainsKey("DurationDays") && !defaultDurationDays.HasValue)
            {
                throw new Exception("Excel'de 'DurationDays' sütunu veya default duration belirtilmelidir");
            }

            _logger.LogInformation(
                "📊 Excel headers found: {Headers}",
                string.Join(", ", headers.Keys));

            // Row 1 is header, start from row 2
            for (int row = 2; row <= rowCount; row++)
            {
                var email = headers.ContainsKey("Email")
                    ? worksheet.Cells[row, headers["Email"]].Text?.Trim()
                    : null;

                var phone = headers.ContainsKey("Phone")
                    ? worksheet.Cells[row, headers["Phone"]].Text?.Trim()
                    : null;

                // Skip empty rows
                if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
                {
                    continue;
                }

                // Optional fields
                var firstName = headers.ContainsKey("FirstName")
                    ? worksheet.Cells[row, headers["FirstName"]].Text?.Trim()
                    : null;

                var lastName = headers.ContainsKey("LastName")
                    ? worksheet.Cells[row, headers["LastName"]].Text?.Trim()
                    : null;

                var tierName = headers.ContainsKey("TierName")
                    ? worksheet.Cells[row, headers["TierName"]].Text?.Trim()
                    : null;

                var durationDaysStr = headers.ContainsKey("DurationDays")
                    ? worksheet.Cells[row, headers["DurationDays"]].Text?.Trim()
                    : null;

                var notes = headers.ContainsKey("Notes")
                    ? worksheet.Cells[row, headers["Notes"]].Text?.Trim()
                    : null;

                // Parse tier ID from tier name or use default
                int? tierId = null;
                if (!string.IsNullOrWhiteSpace(tierName))
                {
                    tierId = await GetTierIdByNameAsync(tierName);
                }
                else if (defaultTierId.HasValue)
                {
                    tierId = defaultTierId.Value;
                }

                // Parse duration or use default
                int durationDays;
                if (!string.IsNullOrWhiteSpace(durationDaysStr) && int.TryParse(durationDaysStr, out var parsedDuration))
                {
                    durationDays = parsedDuration;
                }
                else if (defaultDurationDays.HasValue)
                {
                    durationDays = defaultDurationDays.Value;
                }
                else
                {
                    // Skip row if no duration specified
                    _logger.LogWarning("⚠️ Row {RowNumber}: No duration specified, skipping", row);
                    continue;
                }

                var assignmentRow = new FarmerSubscriptionAssignmentRow
                {
                    RowNumber = row,
                    Email = email,
                    Phone = NormalizePhone(phone),
                    FirstName = firstName,
                    LastName = lastName,
                    SubscriptionTierId = tierId ?? 0,
                    DurationDays = durationDays,
                    Notes = notes
                };

                rows.Add(assignmentRow);
            }

            return rows;
        }

        private async Task<int?> GetTierIdByNameAsync(string tierName)
        {
            var tier = await _tierRepository.GetAsync(t =>
                t.TierName.ToLower() == tierName.ToLower() ||
                t.DisplayName.ToLower() == tierName.ToLower());

            return tier?.Id;
        }

        private async Task<IResult> ValidateRowsAsync(List<FarmerSubscriptionAssignmentRow> rows)
        {
            var errors = new List<string>();
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var phones = new HashSet<string>();

            foreach (var row in rows)
            {
                // Email or phone required
                if (string.IsNullOrWhiteSpace(row.Email) && string.IsNullOrWhiteSpace(row.Phone))
                {
                    errors.Add($"Satır {row.RowNumber}: Email veya Phone gerekli");
                    continue;
                }

                // Email validation (if provided)
                if (!string.IsNullOrWhiteSpace(row.Email) && !IsValidEmail(row.Email))
                {
                    errors.Add($"Satır {row.RowNumber}: Geçersiz email - {row.Email}");
                    continue;
                }

                // Phone validation (if provided)
                if (!string.IsNullOrWhiteSpace(row.Phone) && !IsValidPhone(row.Phone))
                {
                    errors.Add($"Satır {row.RowNumber}: Geçersiz telefon - {row.Phone}");
                    continue;
                }

                // Tier validation
                if (row.SubscriptionTierId <= 0)
                {
                    errors.Add($"Satır {row.RowNumber}: Geçersiz subscription tier");
                    continue;
                }

                // Duration validation
                if (row.DurationDays <= 0)
                {
                    errors.Add($"Satır {row.RowNumber}: Duration 0'dan büyük olmalı");
                    continue;
                }

                // Duplicate check (email)
                if (!string.IsNullOrWhiteSpace(row.Email))
                {
                    if (emails.Contains(row.Email))
                    {
                        errors.Add($"Satır {row.RowNumber}: Duplicate email - {row.Email}");
                        continue;
                    }
                    emails.Add(row.Email);
                }

                // Duplicate check (phone)
                if (!string.IsNullOrWhiteSpace(row.Phone))
                {
                    if (phones.Contains(row.Phone))
                    {
                        errors.Add($"Satır {row.RowNumber}: Duplicate phone - {row.Phone}");
                        continue;
                    }
                    phones.Add(row.Phone);
                }
            }

            if (errors.Any())
            {
                var errorMessage = string.Join("\n", errors.Take(10));
                if (errors.Count > 10)
                {
                    errorMessage += $"\n... ve {errors.Count - 10} hata daha";
                }
                return new ErrorResult($"Excel validasyon hataları:\n{errorMessage}");
            }

            return new SuccessResult();
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
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

            var normalized = NormalizePhone(phone);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            // Normalized format should be 11 digits starting with 05
            // Example: 05321234567
            return normalized.Length == 11 && normalized.StartsWith("0");
        }

        private string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            // Remove all non-digit characters (spaces, dashes, parentheses, dots, plus)
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

            // 905321234567 → 05321234567
            if (cleaned.Length == 11 && cleaned.StartsWith("90"))
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

            // Return as-is if format not recognized (will fail validation)
            return cleaned;
        }

        #endregion
    }

    // Internal row class for Excel parsing
    internal class FarmerSubscriptionAssignmentRow
    {
        public int RowNumber { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int SubscriptionTierId { get; set; }
        public int DurationDays { get; set; }
        public string Notes { get; set; }
    }
}
