using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.Messaging.Factories;
using Business.Services.FarmerInvitation;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Caching;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Helpers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Sponsorship.Commands
{
    /// <summary>
    /// Bulk create farmer invitations - same pattern as SendSponsorshipLinkCommand
    /// Allows sponsors to upload Excel and send multiple invitations at once
    /// </summary>
    public class BulkCreateFarmerInvitationsCommand : IRequest<IDataResult<BulkFarmerInvitationResult>>
    {
        public int SponsorId { get; set; }
        public List<FarmerInvitationRecipient> Recipients { get; set; } = new();
        public string Channel { get; set; } = "SMS"; // SMS or WhatsApp
        public string CustomMessage { get; set; } // Optional custom message

        public class FarmerInvitationRecipient
        {
            public string Phone { get; set; }
            public string FarmerName { get; set; }
            public string Email { get; set; }
            public string PackageTier { get; set; } // S, M, L, XL or null
            public string Notes { get; set; }

            // CodeCount removed - always defaults to 1 per farmer invitation
        }
    }

    public class BulkCreateFarmerInvitationsCommandHandler : IRequestHandler<BulkCreateFarmerInvitationsCommand, IDataResult<BulkFarmerInvitationResult>>
    {
        private readonly IFarmerInvitationRepository _invitationRepository;
        private readonly ISponsorshipCodeRepository _codeRepository;
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly ISubscriptionTierRepository _tierRepository;
        private readonly IMessagingServiceFactory _messagingFactory;
        private readonly IFarmerInvitationConfigurationService _configService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BulkCreateFarmerInvitationsCommandHandler> _logger;
        private readonly Business.Services.Notification.IFarmerInvitationNotificationService _notificationService;
        private readonly ICacheManager _cacheManager;

        public BulkCreateFarmerInvitationsCommandHandler(
            IFarmerInvitationRepository invitationRepository,
            ISponsorshipCodeRepository codeRepository,
            ISponsorProfileRepository sponsorProfileRepository,
            ISubscriptionTierRepository tierRepository,
            IMessagingServiceFactory messagingFactory,
            IFarmerInvitationConfigurationService configService,
            IConfiguration configuration,
            ILogger<BulkCreateFarmerInvitationsCommandHandler> logger,
            Business.Services.Notification.IFarmerInvitationNotificationService notificationService,
            ICacheManager cacheManager)
        {
            _invitationRepository = invitationRepository;
            _codeRepository = codeRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
            _tierRepository = tierRepository;
            _messagingFactory = messagingFactory;
            _configService = configService;
            _configuration = configuration;
            _logger = logger;
            _notificationService = notificationService;
            _cacheManager = cacheManager;
        }

        [SecuredOperation(Priority = 1)]
        [CacheRemoveAspect("Get")]
        [LogAspect(typeof(FileLogger))]
        public async Task<IDataResult<BulkFarmerInvitationResult>> Handle(BulkCreateFarmerInvitationsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("📤 Sponsor {SponsorId} creating {Count} farmer invitations via {Channel}",
                    request.SponsorId, request.Recipients.Count, request.Channel);

                // Validate recipients
                if (request.Recipients == null || !request.Recipients.Any())
                {
                    return new ErrorDataResult<BulkFarmerInvitationResult>("Hiç alıcı belirtilmedi");
                }

                // Get sponsor profile for SMS template
                var sponsorProfile = await _sponsorProfileRepository.GetAsync(sp => sp.SponsorId == request.SponsorId);
                var sponsorCompanyName = sponsorProfile?.CompanyName ?? "ZiraAI Sponsor";

                // Get configuration values
                var expiryDays = await _configService.GetTokenExpiryDaysAsync();
                var baseUrl = await _configService.GetDeepLinkBaseUrlAsync();
                var smsTemplate = await _configService.GetSmsTemplateAsync();
                var playStorePackageName = _configuration["MobileApp:PlayStorePackageName"] ?? "com.ziraai.app";
                var playStoreLink = $"https://play.google.com/store/apps/details?id={playStorePackageName}";

                // Process each recipient
                var results = new List<FarmerInvitationSendResult>();

                foreach (var recipient in request.Recipients)
                {
                    const int codeCount = 1; // Always 1 code per farmer invitation

                    var sendResult = new FarmerInvitationSendResult
                    {
                        Phone = recipient.Phone,
                        FarmerName = recipient.FarmerName,
                        CodeCount = codeCount,
                        PackageTier = recipient.PackageTier
                    };

                    try
                    {
                        // 1. Validate tier if specified
                        if (!string.IsNullOrEmpty(recipient.PackageTier))
                        {
                            var validTiers = new[] { "S", "M", "L", "XL" };
                            if (!validTiers.Contains(recipient.PackageTier.ToUpper()))
                            {
                                sendResult.Success = false;
                                sendResult.ErrorMessage = $"Geçersiz tier: {recipient.PackageTier}";
                                sendResult.DeliveryStatus = "Failed - Invalid Tier";
                                results.Add(sendResult);
                                continue;
                            }
                        }

                        // 2. Get available codes (always 1 code)
                        var codesToReserve = await GetCodesToReserveAsync(
                            request.SponsorId,
                            codeCount,
                            recipient.PackageTier);

                        if (codesToReserve.Count < codeCount)
                        {
                            var tierMessage = !string.IsNullOrEmpty(recipient.PackageTier)
                                ? $" ({recipient.PackageTier} tier)"
                                : "";

                            sendResult.Success = false;
                            sendResult.ErrorMessage = $"Yetersiz kod{tierMessage}. Mevcut: {codesToReserve.Count}, İstenen: {codeCount}";
                            sendResult.DeliveryStatus = "Failed - Insufficient Codes";
                            results.Add(sendResult);
                            continue;
                        }

                        // 3. Create invitation
                        var invitation = new FarmerInvitation
                        {
                            SponsorId = request.SponsorId,
                            Phone = PhoneNumberHelper.NormalizePhoneNumber(recipient.Phone),
                            FarmerName = recipient.FarmerName,
                            Email = recipient.Email,
                            PackageTier = recipient.PackageTier?.ToUpper(),
                            CodeCount = codeCount, // Always 1
                            Notes = recipient.Notes,
                            InvitationType = "Invite",
                            InvitationToken = Guid.NewGuid().ToString("N"),
                            Status = "Pending",
                            CreatedDate = DateTime.Now,
                            ExpiryDate = DateTime.Now.AddDays(expiryDays)
                        };

                        _invitationRepository.Add(invitation);
                        await _invitationRepository.SaveChangesAsync();

                        _logger.LogInformation("✅ Created invitation {InvitationId} for {Phone}",
                            invitation.Id, invitation.Phone);

                        // Send SignalR notification to farmer
                        try
                        {
                            await _notificationService.NotifyNewInvitationAsync(invitation);
                            _logger.LogInformation("📣 SignalR notification sent for farmer invitation {InvitationId}", invitation.Id);
                        }
                        catch (Exception notificationEx)
                        {
                            // Log but don't fail - notification is optional
                            _logger.LogWarning(notificationEx, "⚠️ Failed to send SignalR notification for farmer invitation {InvitationId}", invitation.Id);
                        }

                        // 4. Reserve codes AND set distribution tracking
                        // Distribution tracking is set NOW (at send time) to make codes appear as "sent" in dashboard
                        // Link tracking fields will be updated after SMS is sent
                        var codeReservationTime = DateTime.Now;

                        _logger.LogInformation("📦 [BULK] Reserving {Count} codes for invitation {InvitationId}. Codes: {Codes}",
                            codesToReserve.Count, invitation.Id, string.Join(", ", codesToReserve.Select(c => c.Code)));

                        foreach (var code in codesToReserve)
                        {
                            _logger.LogInformation("🔧 [BULK] Updating code {Code} for recipient {Phone}. Current values: Reserved={Reserved}, DistDate={DistDate}",
                                code.Code, invitation.Phone, code.ReservedForFarmerInvitationId, code.DistributionDate);

                            // Reservation tracking
                            code.ReservedForFarmerInvitationId = invitation.Id;
                            code.ReservedForFarmerAt = codeReservationTime;

                            // Distribution tracking (for dashboard statistics)
                            // Dashboard counts codes as "sent" based on DistributionDate.HasValue
                            code.RecipientPhone = invitation.Phone;
                            code.RecipientName = invitation.FarmerName;
                            code.DistributionChannel = "FarmerInvitation";
                            code.DistributionDate = codeReservationTime;  // ← CRITICAL: Makes code count as "sent" in dashboard
                            code.DistributedTo = string.IsNullOrEmpty(invitation.FarmerName)
                                ? invitation.Phone
                                : $"{invitation.FarmerName} ({invitation.Phone})";

                            // Link tracking fields (will be updated after SMS sent)
                            code.LinkSentVia = null;
                            code.LinkSentDate = null;
                            code.LinkDelivered = false;

                            _codeRepository.Update(code);
                        }
                        await _codeRepository.SaveChangesAsync();

                        _logger.LogInformation("✅ [BULK] Saved code reservations for invitation {InvitationId}", invitation.Id);

                        // 5. Generate deep link
                        var deepLink = $"{baseUrl.TrimEnd('/')}/{invitation.InvitationToken}";

                        // 6. Build message using template from appsettings (unless custom message provided)
                        var message = request.CustomMessage ?? smsTemplate
                            .Replace("{sponsorName}", sponsorCompanyName)
                            .Replace("{playStoreLink}", playStoreLink)
                            .Replace("{expiryDays}", expiryDays.ToString());

                        _logger.LogInformation("📱 [BULK DEBUG] Template: {Template}", smsTemplate);
                        _logger.LogInformation("📱 [BULK DEBUG] SponsorName: {SponsorName}, PlayStoreLink: {PlayStoreLink}, ExpiryDays: {ExpiryDays}",
                            sponsorCompanyName, playStoreLink, expiryDays);
                        _logger.LogInformation("📱 [BULK DEBUG] Final message to {Phone}: {Message}",
                            invitation.Phone, message);

                        // 7. Send SMS or WhatsApp
                        IResult messageSendResult;
                        if (request.Channel.ToLower() == "whatsapp")
                        {
                            var whatsAppService = _messagingFactory.GetWhatsAppService();
                            messageSendResult = await whatsAppService.SendMessageAsync(invitation.Phone, message);
                        }
                        else
                        {
                            var smsService = _messagingFactory.GetSmsService();
                            messageSendResult = await smsService.SendSmsAsync(invitation.Phone, message);
                        }

                        // 8. Update link tracking for both invitation AND reserved codes
                        var linkSentTime = DateTime.Now;
                        invitation.LinkSentDate = linkSentTime;
                        invitation.LinkSentVia = request.Channel;
                        invitation.LinkDelivered = messageSendResult.Success;
                        await _invitationRepository.SaveChangesAsync();

                        // Update codes with link tracking
                        foreach (var code in codesToReserve)
                        {
                            code.LinkSentDate = linkSentTime;
                            code.LinkSentVia = request.Channel;
                            code.LinkDelivered = messageSendResult.Success;
                            _codeRepository.Update(code);
                        }
                        await _codeRepository.SaveChangesAsync();

                        // 9. Build result
                        if (messageSendResult.Success)
                        {
                            sendResult.Success = true;
                            sendResult.InvitationId = invitation.Id;
                            sendResult.InvitationToken = invitation.InvitationToken;
                            sendResult.InvitationLink = deepLink;
                            sendResult.DeliveryStatus = "Sent";

                            _logger.LogInformation("✅ Sent invitation {InvitationId} to {Phone}",
                                invitation.Id, invitation.Phone);
                        }
                        else
                        {
                            sendResult.Success = false;
                            sendResult.InvitationId = invitation.Id;
                            sendResult.InvitationToken = invitation.InvitationToken;
                            sendResult.InvitationLink = deepLink;
                            sendResult.ErrorMessage = messageSendResult.Message;
                            sendResult.DeliveryStatus = "Failed - SMS Error";

                            _logger.LogWarning("⚠️ SMS failed for invitation {InvitationId}: {Error}",
                                invitation.Id, messageSendResult.Message);
                        }

                        // Small delay to respect rate limits
                        await Task.Delay(50, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing recipient {Phone}", recipient.Phone);
                        sendResult.Success = false;
                        sendResult.ErrorMessage = ex.Message;
                        sendResult.DeliveryStatus = "Failed - Exception";
                    }

                    results.Add(sendResult);
                }

                var bulkResult = new BulkFarmerInvitationResult
                {
                    TotalRequested = request.Recipients.Count,
                    SuccessCount = results.Count(r => r.Success),
                    FailedCount = results.Count(r => !r.Success),
                    Results = results.ToArray()
                };

                _logger.LogInformation("📧 Bulk farmer invitations completed. Success: {Success}, Failed: {Failed}",
                    bulkResult.SuccessCount, bulkResult.FailedCount);

                // Invalidate sponsor dashboard cache after successful invitations
                if (bulkResult.SuccessCount > 0)
                {
                    var cacheKey = $"SponsorDashboard:{request.SponsorId}";
                    _cacheManager.Remove(cacheKey);
                    _logger.LogInformation("[DashboardCache] 🗑️ Invalidated cache for sponsor {SponsorId} after {Count} farmer invitations",
                        request.SponsorId, bulkResult.SuccessCount);
                }

                return new SuccessDataResult<BulkFarmerInvitationResult>(bulkResult,
                    $"📱 {bulkResult.SuccessCount} davet başarıyla gönderildi via {request.Channel}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk farmer invitations for sponsor {SponsorId}", request.SponsorId);
                return new ErrorDataResult<BulkFarmerInvitationResult>("Toplu davet gönderimi sırasında hata oluştu");
            }
        }

        // Removed - now using PhoneNumberHelper.NormalizePhoneNumber() for consistency

        private async Task<List<SponsorshipCode>> GetCodesToReserveAsync(
            int sponsorId,
            int codeCount,
            string packageTier)
        {
            // Get available codes for sponsor
            var availableCodes = await _codeRepository.GetListAsync(c =>
                c.SponsorId == sponsorId &&
                !c.IsUsed &&
                c.FarmerInvitationId == null &&
                c.DealerId == null &&
                c.ReservedForInvitationId == null &&
                c.ReservedForFarmerInvitationId == null &&
                c.ExpiryDate > DateTime.Now);

            var codesList = availableCodes.ToList();

            // Apply tier filter if specified
            if (!string.IsNullOrEmpty(packageTier))
            {
                var tier = await _tierRepository.GetAsync(t => t.TierName == packageTier.ToUpper());
                if (tier != null)
                {
                    codesList = codesList
                        .Where(c => c.SubscriptionTierId == tier.Id)
                        .ToList();
                }
            }

            // Intelligent ordering: expiry date first (FIFO)
            var selectedCodes = codesList
                .OrderBy(c => c.ExpiryDate)
                .ThenBy(c => c.CreatedDate)
                .Take(codeCount)
                .ToList();

            return selectedCodes;
        }
    }

    /// <summary>
    /// Result for bulk farmer invitation operation
    /// Same structure as BulkSendResult for consistency
    /// </summary>
    public class BulkFarmerInvitationResult
    {
        public int TotalRequested { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public FarmerInvitationSendResult[] Results { get; set; }
    }

    /// <summary>
    /// Individual invitation send result
    /// Similar to SendResult but with farmer invitation specific fields
    /// </summary>
    public class FarmerInvitationSendResult
    {
        public string Phone { get; set; }
        public string FarmerName { get; set; }
        public int CodeCount { get; set; }
        public string PackageTier { get; set; }
        public bool Success { get; set; }
        public int? InvitationId { get; set; }
        public string InvitationToken { get; set; }
        public string InvitationLink { get; set; }
        public string ErrorMessage { get; set; }
        public string DeliveryStatus { get; set; }
    }
}
