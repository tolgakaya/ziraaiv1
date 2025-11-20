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

                    // Get configuration details for debugging
                    var configDetails = GetProviderConfiguration(provider);

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
                        RetrievedAt = DateTime.Now,
                        // Configuration details (for debugging - remove in production)
                        Configuration = configDetails
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

            private ProviderConfigurationInfo GetProviderConfiguration(string provider)
            {
                var config = new ProviderConfigurationInfo();

                switch (provider.ToLower())
                {
                    case "netgsm":
                        // Check environment variables first, then appsettings
                        var userCode = Environment.GetEnvironmentVariable("NETGSM_USERCODE")
                            ?? _configuration["SmsProvider:Netgsm:UserCode"];
                        var password = Environment.GetEnvironmentVariable("NETGSM_PASSWORD")
                            ?? _configuration["SmsProvider:Netgsm:Password"];
                        var msgHeader = Environment.GetEnvironmentVariable("NETGSM_MSGHEADER")
                            ?? _configuration["SmsProvider:Netgsm:MsgHeader"];
                        var apiUrl = _configuration["SmsProvider:Netgsm:ApiUrl"];

                        config.ApiUrl = apiUrl;
                        config.UserCode = userCode;
                        config.Password = password;
                        config.MsgHeader = msgHeader;
                        config.UserCodeSource = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NETGSM_USERCODE"))
                            ? "Environment Variable" : "appsettings";
                        config.PasswordSource = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NETGSM_PASSWORD"))
                            ? "Environment Variable" : "appsettings";
                        break;

                    case "turkcell":
                        config.ApiUrl = _configuration["SmsProvider:Turkcell:ApiUrl"];
                        config.UserCode = _configuration["SmsProvider:Turkcell:Username"];
                        config.Password = _configuration["SmsProvider:Turkcell:Password"];
                        config.MsgHeader = _configuration["SmsProvider:Turkcell:SenderId"];
                        config.UserCodeSource = "appsettings";
                        config.PasswordSource = "appsettings";
                        break;

                    case "mock":
                        config.ApiUrl = "N/A (Mock)";
                        config.UserCode = "N/A";
                        config.Password = "N/A";
                        config.MsgHeader = _configuration["SmsService:MockSettings:FixedCode"] ?? "123456";
                        config.UserCodeSource = "N/A";
                        config.PasswordSource = "N/A";
                        break;

                    default:
                        config.ApiUrl = "Unknown provider";
                        break;
                }

                return config;
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

        /// <summary>
        /// Provider configuration details (for debugging - remove in production)
        /// </summary>
        public ProviderConfigurationInfo Configuration { get; set; }
    }

    /// <summary>
    /// Provider configuration details for debugging
    /// WARNING: Contains sensitive data - remove this endpoint in production
    /// </summary>
    public class ProviderConfigurationInfo
    {
        /// <summary>
        /// API URL
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// User code / Username
        /// </summary>
        public string UserCode { get; set; }

        /// <summary>
        /// Password (sensitive!)
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Message header / Sender ID
        /// </summary>
        public string MsgHeader { get; set; }

        /// <summary>
        /// Source of UserCode (Environment Variable or appsettings)
        /// </summary>
        public string UserCodeSource { get; set; }

        /// <summary>
        /// Source of Password (Environment Variable or appsettings)
        /// </summary>
        public string PasswordSource { get; set; }
    }
}
