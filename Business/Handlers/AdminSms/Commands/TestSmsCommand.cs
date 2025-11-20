using Business.BusinessAspects;
using Business.Services.Messaging.Factories;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.AdminSms.Commands
{
    /// <summary>
    /// Test SMS sending command for admin verification
    /// Used to verify SMS provider configuration (Mock/NetGSM/Turkcell)
    /// </summary>
    public class TestSmsCommand : IRequest<IDataResult<TestSmsResponse>>
    {
        /// <summary>
        /// Phone number to send test SMS (Turkish format: 05xx or 905xx)
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Custom message to send (optional - defaults to test message)
        /// </summary>
        public string Message { get; set; }

        public class TestSmsCommandHandler : IRequestHandler<TestSmsCommand, IDataResult<TestSmsResponse>>
        {
            private readonly IMessagingServiceFactory _messagingFactory;
            private readonly IConfiguration _configuration;
            private readonly ILogger<TestSmsCommandHandler> _logger;

            public TestSmsCommandHandler(
                IMessagingServiceFactory messagingFactory,
                IConfiguration configuration,
                ILogger<TestSmsCommandHandler> logger)
            {
                _messagingFactory = messagingFactory;
                _configuration = configuration;
                _logger = logger;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<TestSmsResponse>> Handle(TestSmsCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    // Validate phone number
                    if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                    {
                        return new ErrorDataResult<TestSmsResponse>("Telefon numarası zorunludur.");
                    }

                    // Get current provider from configuration
                    var provider = _configuration["SmsService:Provider"] ?? "Mock";

                    // Build test message if not provided
                    var message = string.IsNullOrWhiteSpace(request.Message)
                        ? $"ZiraAI SMS Test - {DateTime.Now:yyyy-MM-dd HH:mm:ss} - Provider: {provider}"
                        : request.Message;

                    _logger.LogInformation(
                        "Admin SMS test initiated. Phone: {Phone}, Provider: {Provider}",
                        request.PhoneNumber, provider);

                    // Get SMS service from factory
                    var smsService = _messagingFactory.GetSmsService();

                    // Send test SMS
                    var sendResult = await smsService.SendSmsAsync(request.PhoneNumber, message);

                    if (!sendResult.Success)
                    {
                        _logger.LogWarning(
                            "SMS test failed. Phone: {Phone}, Provider: {Provider}, Error: {Error}",
                            request.PhoneNumber, provider, sendResult.Message);

                        return new ErrorDataResult<TestSmsResponse>(
                            new TestSmsResponse
                            {
                                Success = false,
                                Provider = provider,
                                PhoneNumber = request.PhoneNumber,
                                Message = message,
                                ErrorMessage = sendResult.Message,
                                SentAt = DateTime.Now
                            },
                            sendResult.Message);
                    }

                    // Extract message ID from success message
                    var messageId = ExtractMessageId(sendResult.Message);

                    // Get sender info for additional context
                    var senderInfo = await smsService.GetSenderInfoAsync();

                    var response = new TestSmsResponse
                    {
                        Success = true,
                        MessageId = messageId,
                        Provider = provider,
                        PhoneNumber = request.PhoneNumber,
                        Message = message,
                        SentAt = DateTime.Now,
                        Balance = senderInfo.Success ? senderInfo.Data?.Balance : null,
                        Currency = senderInfo.Success ? senderInfo.Data?.Currency : null
                    };

                    _logger.LogInformation(
                        "SMS test successful. Phone: {Phone}, Provider: {Provider}, MessageId: {MessageId}",
                        request.PhoneNumber, provider, messageId);

                    return new SuccessDataResult<TestSmsResponse>(response, "Test SMS başarıyla gönderildi.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SMS test exception. Phone: {Phone}", request.PhoneNumber);
                    return new ErrorDataResult<TestSmsResponse>($"SMS test hatası: {ex.Message}");
                }
            }

            private string ExtractMessageId(string message)
            {
                // Try to extract message ID from various formats
                if (string.IsNullOrEmpty(message)) return "N/A";

                // Pattern: "Mesaj ID: xxxxx" or "MessageId: xxxxx"
                var patterns = new[]
                {
                    @"Mesaj ID:\s*(\S+)",
                    @"MessageId:\s*(\S+)",
                    @"ID:\s*(\S+)"
                };

                foreach (var pattern in patterns)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(message, pattern);
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }

                return "N/A";
            }
        }
    }

    /// <summary>
    /// Response model for SMS test operation
    /// </summary>
    public class TestSmsResponse
    {
        /// <summary>
        /// Whether SMS was sent successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message ID from provider (for tracking)
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// SMS provider used (Mock, Netgsm, Turkcell)
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Target phone number
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Message content sent
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Timestamp when SMS was sent
        /// </summary>
        public DateTime SentAt { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Account balance (if available from provider)
        /// </summary>
        public decimal? Balance { get; set; }

        /// <summary>
        /// Currency for balance (TL, USD, etc.)
        /// </summary>
        public string Currency { get; set; }
    }
}
