using Core.Utilities.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Business.Services.Messaging.Fakes
{
    /// <summary>
    /// Mock SMS Service for Development/Staging environments.
    /// Implements modern ISmsService interface with comprehensive logging.
    /// Does not send real SMS, only logs to console for testing.
    /// </summary>
    public class MockSmsService : ISmsService
    {
        private readonly ILogger<MockSmsService> _logger;
        private readonly IConfiguration _configuration;
        private readonly SmsRetrieverHelper _smsRetrieverHelper;
        private readonly bool _useFixedCode;
        private readonly string _fixedCode;
        private readonly bool _logToConsole;
        private readonly Dictionary<string, SmsDeliveryStatus> _deliveryStatusCache;

        public MockSmsService(
            ILogger<MockSmsService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _smsRetrieverHelper = new SmsRetrieverHelper(configuration);

            // Read mock configuration
            _useFixedCode = bool.Parse(_configuration["SmsService:MockSettings:UseFixedCode"] ?? "true");
            _fixedCode = _configuration["SmsService:MockSettings:FixedCode"] ?? "123456";
            _logToConsole = bool.Parse(_configuration["SmsService:MockSettings:LogToConsole"] ?? "true");
            _deliveryStatusCache = new Dictionary<string, SmsDeliveryStatus>();
        }

        public async Task<IResult> SendSmsAsync(string phoneNumber, string message)
        {
            // Simulate network delay
            await Task.Delay(100);

            var messageId = GenerateMessageId();
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);

            // Extract OTP if present
            var otpCode = ExtractOtpCode(message);
            var displayMessage = _useFixedCode && !string.IsNullOrEmpty(otpCode)
                ? $"Fixed OTP: {_fixedCode} (Original: {otpCode})"
                : message;

            if (_logToConsole)
            {
                Console.WriteLine($"📱 MOCK SMS");
                Console.WriteLine($"   To: {normalizedPhone}");
                Console.WriteLine($"   Message: {displayMessage}");
                Console.WriteLine($"   MessageId: {messageId}");
                Console.WriteLine();
            }

            _logger.LogInformation(
                "📱 MOCK SMS sent. To={Phone}, MessageId={MessageId}",
                normalizedPhone, messageId);

            // Store delivery status for later queries
            _deliveryStatusCache[messageId] = new SmsDeliveryStatus
            {
                MessageId = messageId,
                PhoneNumber = normalizedPhone,
                Status = "Delivered",
                SentDate = DateTime.Now,
                DeliveredDate = DateTime.Now.AddSeconds(2),
                Cost = 0.05m,
                Provider = "Mock"
            };

            return new SuccessResult($"SMS sent successfully. MessageId: {messageId}");
        }

        public async Task<IResult> SendOtpAsync(string phoneNumber, string otpCode)
        {
            // Simulate network delay
            await Task.Delay(100);

            var messageId = GenerateMessageId();
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);
            var environment = _smsRetrieverHelper.GetCurrentEnvironment();

            // Use fixed code for testing if configured
            var displayCode = _useFixedCode ? _fixedCode : otpCode;

            // Build OTP message with Google SMS Retriever API app signature hash
            // This enables automatic OTP detection and auto-fill on Android devices
            var otpMessage = _smsRetrieverHelper.BuildOtpSmsMessage(displayCode);
            var appHash = _smsRetrieverHelper.GetAppSignatureHash();

            if (_logToConsole)
            {
                Console.WriteLine($"📱 MOCK OTP SMS (Google SMS Retriever API)");
                Console.WriteLine($"   To: {normalizedPhone}");
                Console.WriteLine($"   Environment: {environment}");
                Console.WriteLine($"   OTP Code: {displayCode}");
                if (_useFixedCode && displayCode != otpCode)
                {
                    Console.WriteLine($"   (Original: {otpCode})");
                }
                Console.WriteLine($"   App Hash: {appHash}");
                Console.WriteLine($"   Message Length: {otpMessage.Length}/140 chars");
                Console.WriteLine($"   Full Message:");
                Console.WriteLine($"   {otpMessage}");
                Console.WriteLine($"   MessageId: {messageId}");
                Console.WriteLine();
            }

            _logger.LogInformation(
                "📱 MOCK OTP sent. To={Phone}, Code={Code}, Environment={Environment}, AppHash={AppHash}, MessageId={MessageId}",
                normalizedPhone, displayCode, environment, appHash, messageId);

            // Store delivery status for later queries
            _deliveryStatusCache[messageId] = new SmsDeliveryStatus
            {
                MessageId = messageId,
                PhoneNumber = normalizedPhone,
                Status = "Delivered",
                SentDate = DateTime.Now,
                DeliveredDate = DateTime.Now.AddSeconds(1), // OTP is faster
                Cost = 0.05m,
                Provider = "Mock-OTP"
            };

            return new SuccessResult($"OTP sent successfully. MessageId: {messageId}");
        }

        public async Task<IResult> SendBulkSmsAsync(BulkSmsRequest request)
        {
            _logger.LogInformation("📱 MOCK Bulk SMS to {Count} recipients", request.Recipients.Length);

            var successCount = 0;
            var failedCount = 0;
            var totalCost = 0.0m;

            if (_logToConsole)
            {
                Console.WriteLine($"📱 MOCK Bulk SMS");
                Console.WriteLine($"   Recipients: {request.Recipients.Length}");
                Console.WriteLine($"   Sender ID: {request.SenderId}");
                if (request.ScheduledSend && request.ScheduledDate.HasValue)
                {
                    Console.WriteLine($"   Scheduled: {request.ScheduledDate.Value}");
                }
                Console.WriteLine();
            }

            foreach (var recipient in request.Recipients)
            {
                try
                {
                    // Personalize message
                    var message = !string.IsNullOrEmpty(recipient.PersonalizedMessage)
                        ? recipient.PersonalizedMessage
                        : request.Message.Replace("{name}", recipient.Name ?? "Valued User");

                    var result = await SendSmsAsync(recipient.PhoneNumber, message);

                    if (result.Success)
                    {
                        successCount++;
                        totalCost += 0.05m;
                    }
                    else
                    {
                        failedCount++;
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogError(ex, "Error sending SMS to {Phone}", recipient.PhoneNumber);
                }

                // Simulate processing time
                await Task.Delay(50);
            }

            if (_logToConsole)
            {
                Console.WriteLine($"📱 MOCK Bulk SMS Complete");
                Console.WriteLine($"   Success: {successCount}");
                Console.WriteLine($"   Failed: {failedCount}");
                Console.WriteLine($"   Total Cost: {totalCost:F2} TL");
                Console.WriteLine();
            }

            return new SuccessResult(
                $"Bulk SMS sent. Success: {successCount}, Failed: {failedCount}, Total Cost: {totalCost:F2} TL");
        }

        public async Task<IDataResult<SmsDeliveryStatus>> GetDeliveryStatusAsync(string messageId)
        {
            await Task.Delay(50);

            if (_deliveryStatusCache.TryGetValue(messageId, out var status))
            {
                _logger.LogInformation("Delivery status found for {MessageId}: {Status}", messageId, status.Status);
                return new SuccessDataResult<SmsDeliveryStatus>(status);
            }

            _logger.LogWarning("Delivery status not found for {MessageId}", messageId);
            return new ErrorDataResult<SmsDeliveryStatus>("Message not found in delivery status cache");
        }

        public async Task<IDataResult<SmsSenderInfo>> GetSenderInfoAsync()
        {
            await Task.Delay(50);

            var info = new SmsSenderInfo
            {
                SenderId = "MOCK-ZIRAAI",
                Balance = 999.99m,
                Currency = "TL",
                MonthlyQuota = 100000,
                UsedQuota = _deliveryStatusCache.Count,
                Provider = "Mock",
                IsActive = true
            };

            if (_logToConsole)
            {
                Console.WriteLine($"📱 MOCK SMS Sender Info");
                Console.WriteLine($"   Sender ID: {info.SenderId}");
                Console.WriteLine($"   Balance: {info.Balance:F2} {info.Currency}");
                Console.WriteLine($"   Quota: {info.UsedQuota}/{info.MonthlyQuota}");
                Console.WriteLine();
            }

            return new SuccessDataResult<SmsSenderInfo>(info);
        }

        #region Helper Methods

        private string GenerateMessageId()
        {
            return $"MOCK-SMS-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        }

        private string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return phone;

            // Remove all non-digit characters
            var digitsOnly = Regex.Replace(phone, @"\D", string.Empty);

            // Turkish format normalization
            // +905321234567 → 05321234567
            if (digitsOnly.StartsWith("90") && digitsOnly.Length == 12)
            {
                return "0" + digitsOnly.Substring(2);
            }

            // 5321234567 → 05321234567
            if (!digitsOnly.StartsWith("0") && digitsOnly.Length == 10)
            {
                return "0" + digitsOnly;
            }

            return digitsOnly;
        }

        private string ExtractOtpCode(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            // Try multiple OTP patterns
            var patterns = new[]
            {
                @"PAROLANIZ\s*:?\s*(\d{4,6})",  // Turkish: PAROLANIZ : 123456
                @"CODE\s*:?\s*(\d{4,6})",        // English: CODE: 123456
                @"OTP\s*:?\s*(\d{4,6})",         // OTP: 123456
                @"KOD\s*:?\s*(\d{4,6})",         // Turkish: KOD: 123456
                @"\b(\d{4,6})\b"                 // Any 4-6 digit number
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);

                if (match.Success)
                    return match.Groups[1].Value;
            }

            return null;
        }

        #endregion
    }
}
