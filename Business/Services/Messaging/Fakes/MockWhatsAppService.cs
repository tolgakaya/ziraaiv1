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
    /// Mock WhatsApp Service for Development/Staging environments.
    /// Implements IWhatsAppService interface with comprehensive logging.
    /// Does not send real WhatsApp messages, only logs to console for testing.
    /// </summary>
    public class MockWhatsAppService : IWhatsAppService
    {
        private readonly ILogger<MockWhatsAppService> _logger;
        private readonly IConfiguration _configuration;
        private readonly bool _logToConsole;
        private readonly Dictionary<string, WhatsAppDeliveryStatus> _deliveryStatusCache;

        public MockWhatsAppService(
            ILogger<MockWhatsAppService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // Read mock configuration
            _logToConsole = bool.Parse(_configuration["WhatsAppService:MockSettings:LogToConsole"] ?? "true");
            _deliveryStatusCache = new Dictionary<string, WhatsAppDeliveryStatus>();
        }

        public async Task<IResult> SendMessageAsync(string phoneNumber, string message)
        {
            // Simulate network delay (WhatsApp typically slower than SMS)
            await Task.Delay(150);

            var messageId = GenerateMessageId();
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);

            if (_logToConsole)
            {
                Console.WriteLine($"💬 MOCK WhatsApp");
                Console.WriteLine($"   To: {normalizedPhone}");
                Console.WriteLine($"   Message: {message}");
                Console.WriteLine($"   MessageId: {messageId}");
                Console.WriteLine();
            }

            _logger.LogInformation(
                "💬 MOCK WhatsApp sent. To={Phone}, MessageId={MessageId}",
                normalizedPhone, messageId);

            // Store delivery status for later queries
            _deliveryStatusCache[messageId] = new WhatsAppDeliveryStatus
            {
                MessageId = messageId,
                PhoneNumber = normalizedPhone,
                Status = "read",
                SentDate = DateTime.Now,
                DeliveredDate = DateTime.Now.AddSeconds(3),
                ReadDate = DateTime.Now.AddSeconds(5),
                Provider = "Mock"
            };

            return new SuccessResult($"WhatsApp message sent. MessageId: {messageId}");
        }

        public async Task<IResult> SendTemplateMessageAsync(string phoneNumber, string templateName, object templateParameters)
        {
            // Simulate network delay
            await Task.Delay(150);

            var messageId = GenerateMessageId();
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);

            if (_logToConsole)
            {
                Console.WriteLine($"💬 MOCK WhatsApp Template");
                Console.WriteLine($"   To: {normalizedPhone}");
                Console.WriteLine($"   Template: {templateName}");
                Console.WriteLine($"   Parameters: {templateParameters}");
                Console.WriteLine($"   MessageId: {messageId}");
                Console.WriteLine();
            }

            _logger.LogInformation(
                "💬 MOCK WhatsApp template sent. To={Phone}, Template={Template}, MessageId={MessageId}",
                normalizedPhone, templateName, messageId);

            // Store delivery status
            _deliveryStatusCache[messageId] = new WhatsAppDeliveryStatus
            {
                MessageId = messageId,
                PhoneNumber = normalizedPhone,
                Status = "read",
                SentDate = DateTime.Now,
                DeliveredDate = DateTime.Now.AddSeconds(3),
                ReadDate = DateTime.Now.AddSeconds(6),
                Provider = "Mock"
            };

            return new SuccessResult($"WhatsApp template sent. MessageId: {messageId}");
        }

        public async Task<IResult> SendBulkMessageAsync(BulkWhatsAppRequest request)
        {
            _logger.LogInformation("💬 MOCK Bulk WhatsApp to {Count} recipients", request.Recipients.Length);

            var successCount = 0;
            var failedCount = 0;

            if (_logToConsole)
            {
                Console.WriteLine($"💬 MOCK Bulk WhatsApp");
                Console.WriteLine($"   Recipients: {request.Recipients.Length}");
                Console.WriteLine($"   UseTemplate: {request.UseTemplate}");
                if (request.UseTemplate)
                {
                    Console.WriteLine($"   Template: {request.TemplateName}");
                }
                Console.WriteLine();
            }

            foreach (var recipient in request.Recipients)
            {
                try
                {
                    IResult result;

                    if (request.UseTemplate && !string.IsNullOrEmpty(request.TemplateName))
                    {
                        result = await SendTemplateMessageAsync(
                            recipient.PhoneNumber,
                            request.TemplateName,
                            recipient.TemplateParameters);
                    }
                    else
                    {
                        var message = !string.IsNullOrEmpty(recipient.PersonalizedMessage)
                            ? recipient.PersonalizedMessage
                            : request.Message?.Replace("{name}", recipient.Name ?? "Valued User");

                        result = await SendMessageAsync(recipient.PhoneNumber, message);
                    }

                    if (result.Success)
                        successCount++;
                    else
                        failedCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogError(ex, "Error sending WhatsApp to {Phone}", recipient.PhoneNumber);
                }

                // Simulate processing time
                await Task.Delay(100);
            }

            if (_logToConsole)
            {
                Console.WriteLine($"💬 MOCK Bulk WhatsApp Complete");
                Console.WriteLine($"   Success: {successCount}");
                Console.WriteLine($"   Failed: {failedCount}");
                Console.WriteLine();
            }

            return new SuccessResult(
                $"Bulk WhatsApp sent. Success: {successCount}, Failed: {failedCount}");
        }

        public async Task<IDataResult<WhatsAppDeliveryStatus>> GetDeliveryStatusAsync(string messageId)
        {
            await Task.Delay(50);

            if (_deliveryStatusCache.TryGetValue(messageId, out var status))
            {
                _logger.LogInformation("Delivery status found for {MessageId}: {Status}", messageId, status.Status);
                return new SuccessDataResult<WhatsAppDeliveryStatus>(status);
            }

            _logger.LogWarning("Delivery status not found for {MessageId}", messageId);
            return new ErrorDataResult<WhatsAppDeliveryStatus>("Message not found in delivery status cache");
        }

        public async Task<IDataResult<WhatsAppAccountInfo>> GetAccountInfoAsync()
        {
            await Task.Delay(50);

            var info = new WhatsAppAccountInfo
            {
                BusinessPhoneNumber = "+905321234567",
                BusinessName = "ZiraAI Mock",
                AccountStatus = "VERIFIED",
                MonthlyMessageQuota = 50000,
                UsedMessages = _deliveryStatusCache.Count,
                Currency = "USD",
                IsVerified = true
            };

            if (_logToConsole)
            {
                Console.WriteLine($"💬 MOCK WhatsApp Account Info");
                Console.WriteLine($"   Phone: {info.BusinessPhoneNumber}");
                Console.WriteLine($"   Name: {info.BusinessName}");
                Console.WriteLine($"   Status: {info.AccountStatus}");
                Console.WriteLine($"   Quota: {info.UsedMessages}/{info.MonthlyMessageQuota}");
                Console.WriteLine($"   Verified: {info.IsVerified}");
                Console.WriteLine();
            }

            return new SuccessDataResult<WhatsAppAccountInfo>(info);
        }

        #region Helper Methods

        private string GenerateMessageId()
        {
            return $"MOCK-WA-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        }

        private string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return phone;

            // Remove all non-digit characters
            var digitsOnly = Regex.Replace(phone, @"\D", string.Empty);

            // Turkish format for WhatsApp (international format required)
            // +905321234567
            if (digitsOnly.StartsWith("90") && digitsOnly.Length == 12)
            {
                return "+" + digitsOnly;
            }

            // 05321234567 → +905321234567
            if (digitsOnly.StartsWith("0") && digitsOnly.Length == 11)
            {
                return "+9" + digitsOnly;
            }

            // 5321234567 → +905321234567
            if (!digitsOnly.StartsWith("0") && digitsOnly.Length == 10)
            {
                return "+90" + digitsOnly;
            }

            // If already has +, return as-is
            return phone.StartsWith("+") ? phone : "+" + digitsOnly;
        }

        #endregion
    }
}
