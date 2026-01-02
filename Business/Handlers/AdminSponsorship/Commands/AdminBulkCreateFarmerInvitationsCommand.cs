using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Handlers.Sponsorship.Commands;
using Business.Services.AdminAudit;
using Business.Services.Messaging.Factories;
using Business.Services.FarmerInvitation;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.AdminSponsorship.Commands
{
    /// <summary>
    /// Admin command to bulk create farmer invitations on behalf of sponsor
    /// Same pattern as BulkSendCodesCommand
    /// </summary>
    public class AdminBulkCreateFarmerInvitationsCommand : IRequest<IDataResult<BulkFarmerInvitationResult>>
    {
        public int SponsorId { get; set; }
        public List<AdminFarmerInvitationRecipient> Recipients { get; set; } = new();
        public string Channel { get; set; } = "SMS"; // SMS or WhatsApp
        public string CustomMessage { get; set; }

        // Admin context
        public int AdminUserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }

        public class AdminFarmerInvitationRecipient
        {
            public string Phone { get; set; }
            public string FarmerName { get; set; }
            public string Email { get; set; }
            public int CodeCount { get; set; }
            public string PackageTier { get; set; }
            public string Notes { get; set; }
        }
    }

    public class AdminBulkCreateFarmerInvitationsCommandHandler : IRequestHandler<AdminBulkCreateFarmerInvitationsCommand, IDataResult<BulkFarmerInvitationResult>>
    {
        private readonly IFarmerInvitationRepository _invitationRepository;
        private readonly ISponsorshipCodeRepository _codeRepository;
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly ISubscriptionTierRepository _tierRepository;
        private readonly IMessagingServiceFactory _messagingFactory;
        private readonly IFarmerInvitationConfigurationService _configService;
        private readonly IAdminAuditService _auditService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminBulkCreateFarmerInvitationsCommandHandler> _logger;

        public AdminBulkCreateFarmerInvitationsCommandHandler(
            IFarmerInvitationRepository invitationRepository,
            ISponsorshipCodeRepository codeRepository,
            ISponsorProfileRepository sponsorProfileRepository,
            ISubscriptionTierRepository tierRepository,
            IMessagingServiceFactory messagingFactory,
            IFarmerInvitationConfigurationService configService,
            IAdminAuditService auditService,
            IConfiguration configuration,
            ILogger<AdminBulkCreateFarmerInvitationsCommandHandler> logger)
        {
            _invitationRepository = invitationRepository;
            _codeRepository = codeRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
            _tierRepository = tierRepository;
            _messagingFactory = messagingFactory;
            _configService = configService;
            _auditService = auditService;
            _configuration = configuration;
            _logger = logger;
        }

        [SecuredOperation(Priority = 1)]
        [LogAspect(typeof(FileLogger))]
        public async Task<IDataResult<BulkFarmerInvitationResult>> Handle(AdminBulkCreateFarmerInvitationsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("📤 ADMIN {AdminId} creating {Count} farmer invitations on behalf of sponsor {SponsorId} via {Channel}",
                    request.AdminUserId, request.Recipients.Count, request.SponsorId, request.Channel);

                // Validate recipients
                if (request.Recipients == null || !request.Recipients.Any())
                {
                    return new ErrorDataResult<BulkFarmerInvitationResult>("Hiç alıcı belirtilmedi");
                }

                // Get sponsor profile
                var sponsorProfile = await _sponsorProfileRepository.GetAsync(sp => sp.SponsorId == request.SponsorId);
                var sponsorCompanyName = sponsorProfile?.CompanyName ?? "ZiraAI Sponsor";

                // Get configuration values
                var tokenExpiryDays = await _configService.GetTokenExpiryDaysAsync();
                var baseUrl = await _configService.GetDeepLinkBaseUrlAsync();
                var smsTemplate = await _configService.GetSmsTemplateAsync();
                var playStorePackageName = _configuration["MobileApp:PlayStorePackageName"] ?? "com.ziraai.app";
                var playStoreLink = $"https://play.google.com/store/apps/details?id={playStorePackageName}";

                // Process each recipient
                var results = new List<FarmerInvitationSendResult>();

                foreach (var recipient in request.Recipients)
                {
                    var sendResult = new FarmerInvitationSendResult
                    {
                        Phone = recipient.Phone,
                        FarmerName = recipient.FarmerName,
                        CodeCount = recipient.CodeCount,
                        PackageTier = recipient.PackageTier
                    };

                    try
                    {
                        // 1. Validate tier
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

                        // 2. Get available codes
                        var codesToReserve = await GetCodesToReserveAsync(
                            request.SponsorId,
                            recipient.CodeCount,
                            recipient.PackageTier);

                        if (codesToReserve.Count < recipient.CodeCount)
                        {
                            var tierMessage = !string.IsNullOrEmpty(recipient.PackageTier)
                                ? $" ({recipient.PackageTier} tier)"
                                : "";

                            sendResult.Success = false;
                            sendResult.ErrorMessage = $"Yetersiz kod{tierMessage}. Mevcut: {codesToReserve.Count}, İstenen: {recipient.CodeCount}";
                            sendResult.DeliveryStatus = "Failed - Insufficient Codes";
                            results.Add(sendResult);
                            continue;
                        }

                        // 3. Create invitation
                        var invitation = new FarmerInvitation
                        {
                            SponsorId = request.SponsorId,
                            Phone = FormatPhoneNumber(recipient.Phone),
                            FarmerName = recipient.FarmerName,
                            Email = recipient.Email,
                            PackageTier = recipient.PackageTier?.ToUpper(),
                            CodeCount = recipient.CodeCount,
                            Notes = recipient.Notes,
                            InvitationType = "Invite",
                            InvitationToken = Guid.NewGuid().ToString("N"),
                            Status = "Pending",
                            CreatedDate = DateTime.Now,
                            ExpiryDate = DateTime.Now.AddDays(tokenExpiryDays)
                        };

                        _invitationRepository.Add(invitation);
                        await _invitationRepository.SaveChangesAsync();

                        _logger.LogInformation("✅ ADMIN created invitation {InvitationId} for {Phone}",
                            invitation.Id, invitation.Phone);

                        // 4. Reserve codes
                        foreach (var code in codesToReserve)
                        {
                            code.ReservedForFarmerInvitationId = invitation.Id;
                            code.ReservedForFarmerAt = DateTime.Now;
                            _codeRepository.Update(code);
                        }
                        await _codeRepository.SaveChangesAsync();

                        // 5. Generate deep link
                        var deepLink = $"{baseUrl.TrimEnd('/')}/{invitation.InvitationToken}";

                        // 6. Build message
                        var message = request.CustomMessage ?? smsTemplate
                            .Replace("{sponsorName}", sponsorCompanyName)
                            .Replace("{farmerName}", recipient.FarmerName ?? "Değerli Çiftçimiz")
                            .Replace("{codeCount}", recipient.CodeCount.ToString())
                            .Replace("{deepLink}", deepLink)
                            .Replace("{playStoreLink}", playStoreLink);

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

                        // 8. Update invitation
                        invitation.LinkSentDate = DateTime.Now;
                        invitation.LinkSentVia = request.Channel;
                        invitation.LinkDelivered = messageSendResult.Success;
                        await _invitationRepository.SaveChangesAsync();

                        // 9. Build result
                        if (messageSendResult.Success)
                        {
                            sendResult.Success = true;
                            sendResult.InvitationId = invitation.Id;
                            sendResult.InvitationToken = invitation.InvitationToken;
                            sendResult.InvitationLink = deepLink;
                            sendResult.DeliveryStatus = "Sent";

                            _logger.LogInformation("✅ ADMIN sent invitation {InvitationId} to {Phone}",
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

                            _logger.LogWarning("⚠️ ADMIN SMS failed for invitation {InvitationId}: {Error}",
                                invitation.Id, messageSendResult.Message);
                        }

                        // Small delay
                        await Task.Delay(50, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ADMIN error processing recipient {Phone}", recipient.Phone);
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

                // Audit log
                await _auditService.LogAsync(
                    action: "AdminBulkCreateFarmerInvitations",
                    adminUserId: request.AdminUserId,
                    targetUserId: request.SponsorId,
                    entityType: "FarmerInvitation",
                    entityId: request.SponsorId,
                    isOnBehalfOf: true,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    requestPath: request.RequestPath,
                    reason: $"Admin bulk created {bulkResult.SuccessCount} farmer invitations via {request.Channel}",
                    afterState: new
                    {
                        SponsorId = request.SponsorId,
                        TotalRequested = bulkResult.TotalRequested,
                        SuccessCount = bulkResult.SuccessCount,
                        FailedCount = bulkResult.FailedCount,
                        Channel = request.Channel
                    }
                );

                _logger.LogInformation("📧 ADMIN bulk farmer invitations completed. Success: {Success}, Failed: {Failed}",
                    bulkResult.SuccessCount, bulkResult.FailedCount);

                return new SuccessDataResult<BulkFarmerInvitationResult>(bulkResult,
                    $"📱 {bulkResult.SuccessCount} davet başarıyla gönderildi via {request.Channel}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ADMIN error in bulk farmer invitations for sponsor {SponsorId}", request.SponsorId);
                return new ErrorDataResult<BulkFarmerInvitationResult>("Toplu davet gönderimi sırasında hata oluştu");
            }
        }

        private string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            var cleaned = new string(phone.Where(char.IsDigit).ToArray());

            if (cleaned.StartsWith("+90"))
            {
                cleaned = "0" + cleaned.Substring(3);
            }
            else if (cleaned.StartsWith("90") && cleaned.Length == 12)
            {
                cleaned = "0" + cleaned.Substring(2);
            }
            else if (!cleaned.StartsWith("0") && cleaned.Length == 10)
            {
                cleaned = "0" + cleaned;
            }

            return cleaned;
        }

        private async Task<List<SponsorshipCode>> GetCodesToReserveAsync(
            int sponsorId,
            int codeCount,
            string packageTier)
        {
            var availableCodes = await _codeRepository.GetListAsync(c =>
                c.SponsorId == sponsorId &&
                !c.IsUsed &&
                c.FarmerInvitationId == null &&
                c.DealerId == null &&
                c.ReservedForInvitationId == null &&
                c.ReservedForFarmerInvitationId == null &&
                c.ExpiryDate > DateTime.Now);

            var codesList = availableCodes.ToList();

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

            var selectedCodes = codesList
                .OrderBy(c => c.ExpiryDate)
                .ThenBy(c => c.CreatedDate)
                .Take(codeCount)
                .ToList();

            return selectedCodes;
        }
    }
}
