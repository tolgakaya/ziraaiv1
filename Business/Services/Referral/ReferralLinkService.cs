using Business.Services.Messaging;
using Business.Services.Messaging.Factories;
using Core.Utilities.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.Referral
{
    public class ReferralLinkService : IReferralLinkService
    {
        private readonly IReferralCodeService _codeService;
        private readonly IReferralConfigurationService _configService;
        private readonly IMessagingServiceFactory _messagingFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReferralLinkService> _logger;

        public ReferralLinkService(
            IReferralCodeService codeService,
            IReferralConfigurationService configService,
            IMessagingServiceFactory messagingFactory,
            IConfiguration configuration,
            ILogger<ReferralLinkService> logger)
        {
            _codeService = codeService;
            _configService = configService;
            _messagingFactory = messagingFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IDataResult<ReferralLinkResponse>> GenerateAndSendLinksAsync(
            int userId,
            List<string> phoneNumbers,
            DeliveryMethod deliveryMethod,
            string customMessage = null)
        {
            try
            {
                // Validate phone numbers
                if (phoneNumbers == null || !phoneNumbers.Any())
                    return new ErrorDataResult<ReferralLinkResponse>("Phone numbers are required");

                // Generate or get existing active code for user
                var activeCodesResult = await _codeService.GetUserActiveCodesAsync(userId);
                string referralCode;

                if (activeCodesResult.Success && activeCodesResult.Data.Any())
                {
                    // Use existing active code
                    referralCode = activeCodesResult.Data.First().Code;
                    _logger.LogInformation("Using existing referral code {Code} for user {UserId}",
                        referralCode, userId);
                }
                else
                {
                    // Generate new code
                    var generateResult = await _codeService.GenerateCodeAsync(userId);
                    if (!generateResult.Success)
                        return new ErrorDataResult<ReferralLinkResponse>(generateResult.Message);

                    referralCode = generateResult.Data.Code;
                }

                // Build links
                var webDeepLink = await BuildWebDeepLinkAsync(referralCode);
                var playStoreLink = await BuildPlayStoreLinkAsync(referralCode);

                // Get expiry date
                var expiryDays = await _configService.GetLinkExpiryDaysAsync();
                var expiresAt = DateTime.Now.AddDays(expiryDays);

                // Send messages
                var deliveryStatuses = await SendMessagesAsync(
                    phoneNumbers,
                    referralCode,
                    webDeepLink,
                    playStoreLink,
                    deliveryMethod,
                    customMessage);

                var response = new ReferralLinkResponse
                {
                    ReferralCode = referralCode,
                    DeepLink = webDeepLink,
                    PlayStoreLink = playStoreLink,
                    ExpiresAt = expiresAt,
                    DeliveryStatuses = deliveryStatuses
                };

                return new SuccessDataResult<ReferralLinkResponse>(response, "Referral links sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating and sending referral links for user {UserId}", userId);
                return new ErrorDataResult<ReferralLinkResponse>("Failed to generate and send referral links");
            }
        }

        public async Task<string> BuildPlayStoreLinkAsync(string referralCode)
        {
            // Play Store link with referrer parameter
            // When user installs from this link, the app can capture the referrer parameter
            var packageName = _configuration["MobileApp:PlayStorePackageName"]
                ?? throw new InvalidOperationException("MobileApp:PlayStorePackageName must be configured");

            var playStoreLink = $"https://play.google.com/store/apps/details?id={packageName}&referrer={referralCode}";

            return await Task.FromResult(playStoreLink);
        }

        public async Task<string> BuildWebDeepLinkAsync(string referralCode)
        {
            var baseUrl = await _configService.GetDeepLinkBaseUrlAsync();
            return $"{baseUrl}{referralCode}";
        }

        #region Private Helper Methods

        private async Task<List<DeliveryStatus>> SendMessagesAsync(
            List<string> phoneNumbers,
            string referralCode,
            string webDeepLink,
            string playStoreLink,
            DeliveryMethod deliveryMethod,
            string customMessage)
        {
            var deliveryStatuses = new List<DeliveryStatus>();

            // Check configuration for enabled methods
            var smsEnabled = await _configService.IsSmsEnabledAsync();
            var whatsAppEnabled = await _configService.IsWhatsAppEnabledAsync();

            foreach (var phone in phoneNumbers)
            {
                // Send SMS if required
                if ((deliveryMethod == DeliveryMethod.SMS || deliveryMethod == DeliveryMethod.Both) && smsEnabled)
                {
                    var smsStatus = await SendSmsAsync(phone, referralCode, playStoreLink, customMessage);
                    deliveryStatuses.Add(smsStatus);
                }

                // Send WhatsApp if required
                if ((deliveryMethod == DeliveryMethod.WhatsApp || deliveryMethod == DeliveryMethod.Both) && whatsAppEnabled)
                {
                    var whatsappStatus = await SendWhatsAppAsync(phone, referralCode, playStoreLink, customMessage);
                    deliveryStatuses.Add(whatsappStatus);
                }
            }

            return deliveryStatuses;
        }

        private async Task<DeliveryStatus> SendSmsAsync(
            string phoneNumber,
            string referralCode,
            string playStoreLink,
            string customMessage)
        {
            try
            {
                var smsService = _messagingFactory.GetSmsService();

                var message = customMessage ?? BuildSmsMessage(referralCode, playStoreLink);

                var smsResult = await smsService.SendSmsAsync(phoneNumber, message);
                var result = smsResult.Success;

                return new DeliveryStatus
                {
                    PhoneNumber = phoneNumber,
                    Method = "SMS",
                    Status = result ? "Sent" : "Failed",
                    ErrorMessage = result ? null : "SMS sending failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", phoneNumber);
                return new DeliveryStatus
                {
                    PhoneNumber = phoneNumber,
                    Method = "SMS",
                    Status = "Failed",
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<DeliveryStatus> SendWhatsAppAsync(
            string phoneNumber,
            string referralCode,
            string playStoreLink,
            string customMessage)
        {
            try
            {
                var whatsAppService = _messagingFactory.GetWhatsAppService();

                var message = customMessage ?? BuildWhatsAppMessage(referralCode, playStoreLink);

                var waResult = await whatsAppService.SendMessageAsync(phoneNumber, message);
                var result = waResult.Success;

                return new DeliveryStatus
                {
                    PhoneNumber = phoneNumber,
                    Method = "WhatsApp",
                    Status = result ? "Sent" : "Failed",
                    ErrorMessage = result ? null : "WhatsApp sending failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp to {PhoneNumber}", phoneNumber);
                return new DeliveryStatus
                {
                    PhoneNumber = phoneNumber,
                    Method = "WhatsApp",
                    Status = "Failed",
                    ErrorMessage = ex.Message
                };
            }
        }

        private string BuildSmsMessage(string referralCode, string playStoreLink)
        {
            var expiryDays = 30; // Default, could be fetched from config
            var expiryDate = DateTime.Now.AddDays(expiryDays).ToString("dd.MM.yyyy");

            return $@"🌱 ZiraAI'ye hoş geldin!

Bitki analizi için uygulamayı indir:
{playStoreLink}

Kod: {referralCode}
Son kullanma: {expiryDate}";
        }

        private string BuildWhatsAppMessage(string referralCode, string playStoreLink)
        {
            var expiryDays = 30; // Default, could be fetched from config
            var expiryDate = DateTime.Now.AddDays(expiryDays).ToString("dd.MM.yyyy");

            return $@"🌱 *ZiraAI - Bitki Analizi*

Merhaba! Yapay zeka ile bitkilerini ücretsiz analiz et.

📱 Uygulamayı İndir:
{playStoreLink}

🎁 Referans Kodu: *{referralCode}*
⏰ Son Kullanma: {expiryDate}

_Kayıt olurken bu kodu kullan!_";
        }

        #endregion
    }
}
