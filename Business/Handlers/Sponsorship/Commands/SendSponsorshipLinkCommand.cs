using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Constants;
using Business.Services.Analytics;
using Business.Services.Messaging;
using Business.Services.Messaging.Factories;
using Business.Services.Redemption;
using Business.Services.Sponsorship;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Caching;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Sponsorship.Commands
{
    public class SendSponsorshipLinkCommand : IRequest<IDataResult<BulkSendResult>>
    {
        public int SponsorId { get; set; }
        public List<LinkRecipient> Recipients { get; set; } = new();
        public string Channel { get; set; } = "SMS"; // SMS or WhatsApp
        public string CustomMessage { get; set; } // Optional custom message
        public bool AllowResendExpired { get; set; } = false; // Allow resending expired codes with renewed expiry date

        public class LinkRecipient
        {
            public string Code { get; set; }
            public string Phone { get; set; }
            public string Name { get; set; }
        }

        public class SendSponsorshipLinkCommandHandler : IRequestHandler<SendSponsorshipLinkCommand, IDataResult<BulkSendResult>>
        {

            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly IMessagingServiceFactory _messagingFactory;
            private readonly IConfiguration _configuration;
            private readonly ILogger<SendSponsorshipLinkCommandHandler> _logger;
            private readonly ICacheManager _cacheManager;
            private readonly ISponsorDealerAnalyticsCacheService _analyticsCache;
            private readonly IDealerDashboardCacheService _dealerDashboardCache;

            public SendSponsorshipLinkCommandHandler(
                ISponsorshipCodeRepository codeRepository,
                ISponsorProfileRepository sponsorProfileRepository,
                IMessagingServiceFactory messagingFactory,
                IConfiguration configuration,
                ILogger<SendSponsorshipLinkCommandHandler> logger,
                ICacheManager cacheManager,
                ISponsorDealerAnalyticsCacheService analyticsCache,
                IDealerDashboardCacheService dealerDashboardCache)
            {
                _codeRepository = codeRepository;
                _sponsorProfileRepository = sponsorProfileRepository;
                _messagingFactory = messagingFactory;
                _configuration = configuration;
                _logger = logger;
                _cacheManager = cacheManager;
                _analyticsCache = analyticsCache;
                _dealerDashboardCache = dealerDashboardCache;
            }

            [SecuredOperation(Priority = 1)]
            [CacheRemoveAspect("Get")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<BulkSendResult>> Handle(SendSponsorshipLinkCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("📤 Sponsor {SponsorId} sending {Count} sponsorship links via {Channel}",
                        request.SponsorId, request.Recipients.Count, request.Channel);

                    // Validate codes exist and are available
                    var codes = request.Recipients.Select(r => r.Code).ToList();
                    
                    // ✅ FIX: Fetch codes based on AllowResendExpired flag
                    // Support both sponsor (original owner) and dealer (transferred to)
                    var validCodes = request.AllowResendExpired
                        ? await _codeRepository.GetListAsync(c => 
                            codes.Contains(c.Code) && 
                            (c.SponsorId == request.SponsorId || c.DealerId == request.SponsorId) &&  // ✅ Check both!
                            !c.IsUsed)  // Allow expired codes if AllowResendExpired=true
                        : await _codeRepository.GetListAsync(c => 
                            codes.Contains(c.Code) && 
                            (c.SponsorId == request.SponsorId || c.DealerId == request.SponsorId) &&  // ✅ Check both!
                            !c.IsUsed && 
                            c.ExpiryDate > DateTime.Now);  // Only non-expired codes

                    var validCodesList = validCodes.ToList();
                    _logger.LogInformation("📋 Validated {ValidCount}/{TotalCount} codes (AllowResendExpired: {AllowResend})", 
                        validCodesList.Count, codes.Count, request.AllowResendExpired);

                    // Renew expiry date for expired codes if AllowResendExpired=true
                    if (request.AllowResendExpired)
                    {
                        var expiredCodes = validCodesList.Where(c => c.ExpiryDate < DateTime.Now).ToList();
                        if (expiredCodes.Any())
                        {
                            _logger.LogInformation("🔄 Renewing expiry date for {Count} expired codes", expiredCodes.Count);
                            foreach (var expiredCode in expiredCodes)
                            {
                                expiredCode.ExpiryDate = DateTime.Now.AddDays(30); // Renew for 30 days
                                _codeRepository.Update(expiredCode);
                            }
                            await _codeRepository.SaveChangesAsync();
                            _logger.LogInformation("✅ Renewed expiry dates successfully");
                        }
                    }

                    // Get sponsor profile information for SMS template
                    var sponsorProfile = await _sponsorProfileRepository.GetAsync(sp => sp.SponsorId == request.SponsorId);
                    var sponsorCompanyName = sponsorProfile?.CompanyName ?? "ZiraAI Sponsor";
                    
                    // Get Play Store package name from configuration
                    var playStorePackageName = _configuration["MobileApp:PlayStorePackageName"] ?? "com.ziraai.app";
                    var playStoreLink = $"https://play.google.com/store/apps/details?id={playStorePackageName}";

                    _logger.LogInformation("📱 Using sponsor company: {CompanyName}, Play Store: {PackageName}", 
                        sponsorCompanyName, playStorePackageName);

                    // Send messages and track results
                    var results = new List<SendResult>();

                    foreach (var recipient in request.Recipients)
                    {
                        var codeEntity = validCodesList.FirstOrDefault(c => c.Code == recipient.Code);
                        if (codeEntity == null)
                        {
                            results.Add(new SendResult
                            {
                                Code = recipient.Code,
                                Phone = FormatPhoneNumber(recipient.Phone),
                                Success = false,
                                ErrorMessage = "Kod bulunamadı veya kullanılamaz durumda",
                                DeliveryStatus = "Failed - Invalid Code"
                            });
                            continue;
                        }

                        try
                        {
                            var formattedPhone = FormatPhoneNumber(recipient.Phone);

                            // Generate redemption deep link (for SMS)
                            var baseUrl = _configuration["WebAPI:BaseUrl"]
                                ?? _configuration["Referral:FallbackDeepLinkBaseUrl"]?.TrimEnd('/').Replace("/ref", "")
                                ?? "https://ziraai.com";
                            var deepLink = $"{baseUrl.TrimEnd('/')}/redeem/{recipient.Code}";

                            // Build SMS message with sponsor info, code, and deep link
                            var message = request.CustomMessage
                                ?? BuildSmsMessage(recipient.Name, sponsorCompanyName, recipient.Code, playStoreLink, deepLink);

                            // Send SMS or WhatsApp
                            IResult sendResult;
                            if (request.Channel.ToLower() == "whatsapp")
                            {
                                var whatsAppService = _messagingFactory.GetWhatsAppService();
                                sendResult = await whatsAppService.SendMessageAsync(formattedPhone, message);
                            }
                            else
                            {
                                var smsService = _messagingFactory.GetSmsService();
                                sendResult = await smsService.SendSmsAsync(formattedPhone, message);
                            }

                            if (sendResult.Success)
                            {
                                // Use the deep link we already generated
                                var redemptionLink = deepLink;

                                // Update code entity
                                codeEntity.RedemptionLink = redemptionLink;
                                codeEntity.RecipientPhone = formattedPhone;
                                codeEntity.RecipientName = recipient.Name;
                                codeEntity.LinkSentDate = DateTime.Now;
                                codeEntity.LinkSentVia = request.Channel;
                                codeEntity.LinkDelivered = true;
                                codeEntity.DistributionChannel = request.Channel;
                                codeEntity.DistributionDate = DateTime.Now;
                                codeEntity.DistributedTo = $"{recipient.Name} ({formattedPhone})";

                                _codeRepository.Update(codeEntity);

                                // Update analytics cache - code distributed by dealer
                if (codeEntity.SponsorId > 0 && codeEntity.DealerId.HasValue)
                {
                    await _analyticsCache.OnCodeDistributedAsync(codeEntity.SponsorId, codeEntity.DealerId.Value);
                }

                                results.Add(new SendResult
                                {
                                    Code = recipient.Code,
                                    Phone = formattedPhone,
                                    Success = true,
                                    DeliveryStatus = "Sent"
                                });

                                _logger.LogInformation("✅ Sent sponsorship code {Code} to {Phone}", recipient.Code, formattedPhone);
                            }
                            else
                            {
                                results.Add(new SendResult
                                {
                                    Code = recipient.Code,
                                    Phone = formattedPhone,
                                    Success = false,
                                    ErrorMessage = sendResult.Message,
                                    DeliveryStatus = "Failed"
                                });

                                _logger.LogWarning("❌ Failed to send code {Code} to {Phone}: {Error}",
                                    recipient.Code, formattedPhone, sendResult.Message);
                            }

                            // Small delay to respect rate limits
                            await Task.Delay(50);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending to {Phone}", recipient.Phone);
                            results.Add(new SendResult
                            {
                                Code = recipient.Code,
                                Phone = FormatPhoneNumber(recipient.Phone),
                                Success = false,
                                ErrorMessage = ex.Message,
                                DeliveryStatus = "Failed - Exception"
                            });
                        }
                    }

                    // Save all changes to database
                    await _codeRepository.SaveChangesAsync();
                    _logger.LogInformation("💾 Saved {Count} code updates to database", results.Count(r => r.Success));

                    var bulkResult = new BulkSendResult
                    {
                        TotalSent = results.Count,
                        SuccessCount = results.Count(r => r.Success),
                        FailureCount = results.Count(r => !r.Success),
                        Results = results.ToArray()
                    };

                    _logger.LogInformation("📧 Bulk send completed. Success: {Success}, Failed: {Failed}",
                        bulkResult.SuccessCount, bulkResult.FailureCount);

                    // Invalidate sponsor dashboard cache after successful sends
                    if (bulkResult.SuccessCount > 0)
                    {
                        var cacheKey = $"SponsorDashboard:{request.SponsorId}";
                        _cacheManager.Remove(cacheKey);
                        _logger.LogInformation("[DashboardCache] 🗑️ Invalidated cache for sponsor {SponsorId} after sending {Count} links",
                            request.SponsorId, bulkResult.SuccessCount);

                        // Invalidate dealer dashboard cache (SponsorId is actually DealerId for dealers)
                        await _dealerDashboardCache.InvalidateDashboardAsync(request.SponsorId);
                    }

                    return new SuccessDataResult<BulkSendResult>(bulkResult,
                        $"📱 {bulkResult.SuccessCount} link başarıyla gönderildi via {request.Channel}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending sponsorship links for sponsor {SponsorId}", request.SponsorId);
                    return new ErrorDataResult<BulkSendResult>("Link gönderimi sırasında hata oluştu");
                }
            }

            private string BuildSmsMessage(string farmerName, string sponsorCompany, string sponsorCode, string playStoreLink, string deepLink)
            {
                // SMS-based deferred deep linking: Mobile app will read SMS and auto-extract AGRI-XXXXX code
                // Deep link allows users to tap and open app directly with code pre-filled

                // Try to get template from configuration (without emoji, Turkish chars normalized)
                var template = _configuration["Sponsorship:SmsTemplate"];

                if (!string.IsNullOrEmpty(template))
                {
                    return template
                        .Replace("{sponsorName}", sponsorCompany)
                        .Replace("{farmerName}", farmerName)
                        .Replace("{sponsorCode}", sponsorCode)
                        .Replace("{deepLink}", deepLink)
                        .Replace("{playStoreLink}", playStoreLink)
                        .Replace("\\n", "\n");
                }

                // Fallback to default template (without emoji, Turkish chars normalized for SMS compatibility)
                return $@"{sponsorCompany} size sponsorluk paketi hediye etti!

Sponsorluk Kodunuz: {sponsorCode}

Hemen kullanmak icin tiklayin:
{deepLink}

Veya uygulamayi indirin:
{playStoreLink}";
            }

            private string FormatPhoneNumber(string phone)
            {
                // Remove all non-numeric characters
                var cleaned = new string(phone.Where(char.IsDigit).ToArray());

                // Add Turkey country code if not present
                if (!cleaned.StartsWith("90") && cleaned.Length == 10)
                {
                    cleaned = "90" + cleaned;
                }

                // Add + prefix
                if (!cleaned.StartsWith("+"))
                {
                    cleaned = "+" + cleaned;
                }

                return cleaned;
            }
        }
    }
}