using Business.Services.Messaging.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Business.Services.Messaging.Factories
{
    /// <summary>
    /// Factory for creating messaging service instances based on configuration.
    /// Supports multiple providers: Mock, Twilio, Netgsm, Turkcell
    /// </summary>
    public interface IMessagingServiceFactory
    {
        ISmsService GetSmsService();
        IWhatsAppService GetWhatsAppService();
    }

    /// <summary>
    /// Implementation of messaging service factory.
    /// Provider selection is driven by appsettings configuration.
    /// </summary>
    public class MessagingServiceFactory : IMessagingServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MessagingServiceFactory> _logger;

        public MessagingServiceFactory(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<MessagingServiceFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Gets SMS service based on configuration: SmsService:Provider
        /// </summary>
        public ISmsService GetSmsService()
        {
            var provider = _configuration["SmsService:Provider"] ?? "Mock";

            _logger.LogDebug("Creating SMS service with provider: {Provider}", provider);

            return provider.ToLower() switch
            {
                "mock" => (ISmsService)_serviceProvider.GetService(typeof(ISmsService)),
                "twilio" => throw new NotImplementedException("Twilio SMS provider not yet implemented. Use Mock for development."),
                "netgsm" => throw new NotImplementedException("Netgsm SMS provider not yet implemented. Use Mock for development."),
                "turkcell" => (ISmsService)_serviceProvider.GetService(typeof(TurkcellSmsService)),
                _ => throw new InvalidOperationException($"Unknown SMS provider: {provider}. Supported: Mock, Twilio, Netgsm, Turkcell")
            };
        }

        /// <summary>
        /// Gets WhatsApp service based on configuration: WhatsAppService:Provider
        /// </summary>
        public IWhatsAppService GetWhatsAppService()
        {
            var provider = _configuration["WhatsAppService:Provider"] ?? "Mock";

            _logger.LogDebug("Creating WhatsApp service with provider: {Provider}", provider);

            return provider.ToLower() switch
            {
                "mock" => (IWhatsAppService)_serviceProvider.GetService(typeof(IWhatsAppService)),
                "twilio" => throw new NotImplementedException("Twilio WhatsApp provider not yet implemented. Use Mock for development."),
                "whatsappbusiness" or "business" => (IWhatsAppService)_serviceProvider.GetService(typeof(WhatsAppBusinessService)),
                "turkcell" => throw new NotImplementedException("Turkcell WhatsApp provider not yet implemented. Use Mock for development."),
                _ => throw new InvalidOperationException($"Unknown WhatsApp provider: {provider}. Supported: Mock, Twilio, WhatsAppBusiness, Turkcell")
            };
        }
    }
}
