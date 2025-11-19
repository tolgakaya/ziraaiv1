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

namespace Business.Handlers.AdminSms.Queries
{
    /// <summary>
    /// Query to get current SMS provider information and status
    /// </summary>
    public class GetSmsProviderInfoQuery : IRequest<IDataResult<SmsProviderInfoResponse>>
    {
        public class GetSmsProviderInfoQueryHandler : IRequestHandler<GetSmsProviderInfoQuery, IDataResult<SmsProviderInfoResponse>>
        {
            private readonly IMessagingServiceFactory _messagingFactory;
            private readonly IConfiguration _configuration;
            private readonly ILogger<GetSmsProviderInfoQueryHandler> _logger;

            public GetSmsProviderInfoQueryHandler(
                IMessagingServiceFactory messagingFactory,
                IConfiguration configuration,
                ILogger<GetSmsProviderInfoQueryHandler> logger)
            {
                _messagingFactory = messagingFactory;
                _configuration = configuration;
                _logger = logger;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SmsProviderInfoResponse>> Handle(GetSmsProviderInfoQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    // Get current provider from configuration
                    var provider = _configuration["SmsService:Provider"] ?? "Mock";

                    _logger.LogInformation("Getting SMS provider info. Provider: {Provider}", provider);

                    // Get SMS service from factory
                    var smsService = _messagingFactory.GetSmsService();

                    // Get sender info
                    var senderInfo = await smsService.GetSenderInfoAsync();

                    var response = new SmsProviderInfoResponse
                    {
                        Provider = provider,
                        IsConfigured = IsProviderConfigured(provider),
                        SenderId = senderInfo.Success ? senderInfo.Data?.SenderId : null,
                        Balance = senderInfo.Success ? senderInfo.Data?.Balance : null,
                        Currency = senderInfo.Success ? senderInfo.Data?.Currency : null,
                        MonthlyQuota = senderInfo.Success ? senderInfo.Data?.MonthlyQuota : null,
                        UsedQuota = senderInfo.Success ? senderInfo.Data?.UsedQuota : null,
                        IsActive = senderInfo.Success && (senderInfo.Data?.IsActive ?? false),
                        StatusMessage = senderInfo.Success
                            ? "Provider aktif ve çalışıyor"
                            : senderInfo.Message,
                        RetrievedAt = DateTime.Now
                    };

                    return new SuccessDataResult<SmsProviderInfoResponse>(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting SMS provider info");
                    return new ErrorDataResult<SmsProviderInfoResponse>($"Provider bilgisi alınamadı: {ex.Message}");
                }
            }

            private bool IsProviderConfigured(string provider)
            {
                return provider.ToLower() switch
                {
                    "mock" => true, // Mock is always configured
                    "netgsm" => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NETGSM_USERCODE"))
                        || !string.IsNullOrEmpty(_configuration["SmsProvider:Netgsm:UserCode"]),
                    "turkcell" => !string.IsNullOrEmpty(_configuration["SmsProvider:Turkcell:Username"]),
                    "twilio" => !string.IsNullOrEmpty(_configuration["SmsService:TwilioSettings:AccountSid"]),
                    _ => false
                };
            }
        }
    }

    /// <summary>
    /// Response model for SMS provider info
    /// </summary>
    public class SmsProviderInfoResponse
    {
        /// <summary>
        /// Current provider name (Mock, Netgsm, Turkcell)
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Whether provider credentials are configured
        /// </summary>
        public bool IsConfigured { get; set; }

        /// <summary>
        /// Sender ID / Message header
        /// </summary>
        public string SenderId { get; set; }

        /// <summary>
        /// Account balance
        /// </summary>
        public decimal? Balance { get; set; }

        /// <summary>
        /// Currency (TL, USD, etc.)
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Monthly SMS quota
        /// </summary>
        public int? MonthlyQuota { get; set; }

        /// <summary>
        /// Used quota this month
        /// </summary>
        public int? UsedQuota { get; set; }

        /// <summary>
        /// Whether provider is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Status message or error
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Timestamp of info retrieval
        /// </summary>
        public DateTime RetrievedAt { get; set; }
    }
}
