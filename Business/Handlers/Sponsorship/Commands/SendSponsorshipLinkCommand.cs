using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Constants;
using Business.Services.Redemption;
using Business.Services.Notification;
using Business.Services.Notification.Models;
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

        public class LinkRecipient
        {
            public string Code { get; set; }
            public string Phone { get; set; }
            public string Name { get; set; }
        }

        public class SendSponsorshipLinkCommandHandler : IRequestHandler<SendSponsorshipLinkCommand, IDataResult<BulkSendResult>>
        {
            private readonly IRedemptionService _redemptionService;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly INotificationService _notificationService;
            private readonly IConfiguration _configuration;
            private readonly ILogger<SendSponsorshipLinkCommandHandler> _logger;
            private readonly ICacheManager _cacheManager;

            public SendSponsorshipLinkCommandHandler(
                IRedemptionService redemptionService,
                ISponsorshipCodeRepository codeRepository,
                INotificationService notificationService,
                IConfiguration configuration,
                ILogger<SendSponsorshipLinkCommandHandler> logger,
                ICacheManager cacheManager)
            {
                _redemptionService = redemptionService;
                _codeRepository = codeRepository;
                _notificationService = notificationService;
                _configuration = configuration;
                _logger = logger;
                _cacheManager = cacheManager;
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
                    var validCodes = await _codeRepository.GetListAsync(c => 
                        codes.Contains(c.Code) && 
                        c.SponsorId == request.SponsorId && 
                        !c.IsUsed && 
                        c.ExpiryDate > DateTime.Now);

                    var validCodesList = validCodes.ToList();
                    _logger.LogInformation("📋 Validated {ValidCount}/{TotalCount} codes", 
                        validCodesList.Count, codes.Count);

                    // Prepare bulk notification recipients
                    var recipients = new List<BulkNotificationRecipientDto>();
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

                        // Generate redemption link using environment-specific base URL
                        var baseUrl = _configuration["WebAPI:BaseUrl"] 
                            ?? _configuration["Referral:FallbackDeepLinkBaseUrl"]?.TrimEnd('/').Replace("/ref", "")
                            ?? throw new InvalidOperationException("WebAPI:BaseUrl must be configured");
                        var redemptionLink = $"{baseUrl.TrimEnd('/')}/redeem/{recipient.Code}";

                        recipients.Add(new BulkNotificationRecipientDto
                        {
                            UserId = 0, // No user ID for prospects
                            PhoneNumber = FormatPhoneNumber(recipient.Phone),
                            Name = recipient.Name,
                            Parameters = new Dictionary<string, object>
                            {
                                { "farmer_name", recipient.Name },
                                { "sponsor_code", recipient.Code },
                                { "redemption_link", redemptionLink },
                                { "tier_name", "Premium" }, // Will be updated with proper tier lookup
                                { "custom_message", request.CustomMessage ?? "" }
                            }
                        });
                    }

                    // Send notifications based on channel
                    IDataResult<List<NotificationResultDto>> notificationResult;
                    
                    if (request.Channel.ToLower() == "whatsapp")
                    {
                        notificationResult = await _notificationService.SendBulkTemplateNotificationsAsync(
                            recipients, 
                            "sponsorship_invitation",
                            NotificationChannel.WhatsApp);
                    }
                    else
                    {
                        // Default to SMS for now
                        notificationResult = await _notificationService.SendBulkTemplateNotificationsAsync(
                            recipients, 
                            "sponsorship_invitation_sms",
                            NotificationChannel.SMS);
                    }

                    // Process notification results and update database
                    if (notificationResult.Success && notificationResult.Data != null)
                    {
                        for (int i = 0; i < recipients.Count; i++)
                        {
                            var recipient = recipients[i];
                            var originalRecipient = request.Recipients.FirstOrDefault(r =>
                                FormatPhoneNumber(r.Phone) == recipient.PhoneNumber);

                            if (originalRecipient != null)
                            {
                                var notificationSuccess = i < notificationResult.Data.Count &&
                                                        notificationResult.Data[i].Success;

                                // Update database for successful sends
                                if (notificationSuccess)
                                {
                                    var codeEntity = validCodesList.FirstOrDefault(c => c.Code == originalRecipient.Code);
                                    if (codeEntity != null)
                                    {
                                        // Generate redemption link (same logic as before)
                                        var baseUrl = _configuration["WebAPI:BaseUrl"]
                                            ?? _configuration["Referral:FallbackDeepLinkBaseUrl"]?.TrimEnd('/').Replace("/ref", "")
                                            ?? "https://ziraai.com";
                                        var redemptionLink = $"{baseUrl.TrimEnd('/')}/redeem/{originalRecipient.Code}";

                                        // Update code entity
                                        codeEntity.RedemptionLink = redemptionLink;
                                        codeEntity.RecipientPhone = recipient.PhoneNumber;
                                        codeEntity.RecipientName = originalRecipient.Name;
                                        codeEntity.LinkSentDate = DateTime.Now;
                                        codeEntity.LinkSentVia = request.Channel;
                                        codeEntity.LinkDelivered = true;
                                        codeEntity.DistributionChannel = request.Channel;
                                        codeEntity.DistributionDate = DateTime.Now;  // ✅ KEY FIELD!
                                        codeEntity.DistributedTo = $"{originalRecipient.Name} ({recipient.PhoneNumber})";

                                        _codeRepository.Update(codeEntity);

                                        _logger.LogInformation("✅ Updated code {Code} with distribution info", originalRecipient.Code);
                                    }
                                }

                                results.Add(new SendResult
                                {
                                    Code = originalRecipient.Code,
                                    Phone = recipient.PhoneNumber,
                                    Success = notificationSuccess,
                                    ErrorMessage = notificationSuccess ? null :
                                        (i < notificationResult.Data.Count ?
                                         notificationResult.Data[i].ErrorDetails :
                                         "Bildirim gönderimi başarısız"),
                                    DeliveryStatus = notificationSuccess ? "Sent" : "Failed"
                                });
                            }
                        }

                        // Save all changes to database
                        await _codeRepository.SaveChangesAsync();
                        _logger.LogInformation("💾 Saved {Count} code updates to database", results.Count(r => r.Success));
                    }
                    else
                    {
                        // All failed
                        foreach (var recipient in recipients)
                        {
                            var originalRecipient = request.Recipients.FirstOrDefault(r => 
                                FormatPhoneNumber(r.Phone) == recipient.PhoneNumber);

                            if (originalRecipient != null)
                            {
                                results.Add(new SendResult
                                {
                                    Code = originalRecipient.Code,
                                    Phone = recipient.PhoneNumber,
                                    Success = false,
                                    ErrorMessage = notificationResult.Message ?? "Bildirim servisi hatası",
                                    DeliveryStatus = "Failed"
                                });
                            }
                        }
                    }

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