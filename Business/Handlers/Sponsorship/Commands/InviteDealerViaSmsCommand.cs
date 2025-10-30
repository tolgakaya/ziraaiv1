using Business.Services.Messaging;
using Business.Services.DealerInvitation;
using Business.Services.Messaging.Factories;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Commands
{
    public class InviteDealerViaSmsCommand : IRequest<IDataResult<DealerInvitationResponseDto>>
    {
        public int SponsorId { get; set; } // Authenticated sponsor ID
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DealerName { get; set; }
        public int CodeCount { get; set; }
        
        /// <summary>
        /// Optional tier filter for code selection: S, M, L, XL.
        /// If not specified, codes from any tier will be selected automatically.
        /// System will intelligently select codes based on expiry date (FIFO).
        /// </summary>
        public string PackageTier { get; set; }

        public class InviteDealerViaSmsCommandHandler : IRequestHandler<InviteDealerViaSmsCommand, IDataResult<DealerInvitationResponseDto>>
        {
            private readonly IDealerInvitationRepository _invitationRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly ISubscriptionTierRepository _tierRepository;
            private readonly IMessagingServiceFactory _messagingFactory;
            private readonly IDealerInvitationConfigurationService _configService;
            private readonly IConfiguration _configuration;
            private readonly ILogger<InviteDealerViaSmsCommandHandler> _logger;

            public InviteDealerViaSmsCommandHandler(
                IDealerInvitationRepository invitationRepository,
                ISponsorshipCodeRepository codeRepository,
                ISponsorProfileRepository sponsorProfileRepository,
                ISubscriptionTierRepository tierRepository,
                IMessagingServiceFactory messagingFactory,
                IDealerInvitationConfigurationService configService,
                IConfiguration configuration,
                ILogger<InviteDealerViaSmsCommandHandler> logger)
            {
                _invitationRepository = invitationRepository;
                _codeRepository = codeRepository;
                _sponsorProfileRepository = sponsorProfileRepository;
                _tierRepository = tierRepository;
                _messagingFactory = messagingFactory;
                _configService = configService;
                _configuration = configuration;
                _logger = logger;
            }

            [CacheRemoveAspect("Get")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<DealerInvitationResponseDto>> Handle(InviteDealerViaSmsCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("📨 Sponsor {SponsorId} sending dealer invitation via SMS to {Phone} (Tier: {Tier})",
                        request.SponsorId, request.Phone, request.PackageTier ?? "Any");

                    // 1. Validate tier if specified
                    if (!string.IsNullOrEmpty(request.PackageTier))
                    {
                        var validTiers = new[] { "S", "M", "L", "XL" };
                        if (!validTiers.Contains(request.PackageTier.ToUpper()))
                        {
                            _logger.LogWarning("❌ Invalid tier: {Tier}", request.PackageTier);
                            return new ErrorDataResult<DealerInvitationResponseDto>(
                                "Geçersiz paket tier. Geçerli değerler: S, M, L, XL");
                        }
                    }

                    // 2. Get available codes using intelligent selection
                    var codesToReserve = await GetCodesToTransferAsync(
                        request.SponsorId,
                        request.CodeCount,
                        request.PackageTier);

                    if (codesToReserve.Count < request.CodeCount)
                    {
                        var tierMessage = !string.IsNullOrEmpty(request.PackageTier)
                            ? $" ({request.PackageTier} tier)"
                            : "";
                        
                        _logger.LogWarning("❌ Insufficient codes{TierMsg}. Available: {Available}, Requested: {Requested}",
                            tierMessage, codesToReserve.Count, request.CodeCount);
                        
                        return new ErrorDataResult<DealerInvitationResponseDto>(
                            $"Yetersiz kod{tierMessage}. Mevcut: {codesToReserve.Count}, İstenen: {request.CodeCount}");
                    }

                    // 3. Get sponsor profile for SMS template
                    var sponsorProfile = await _sponsorProfileRepository.GetAsync(sp => sp.SponsorId == request.SponsorId);
                    var sponsorCompanyName = sponsorProfile?.CompanyName ?? "ZiraAI Sponsor";

                    // 4. Create dealer invitation
                    var invitation = new DealerInvitation
                    {
                        SponsorId = request.SponsorId,
                        Email = request.Email,
                        Phone = FormatPhoneNumber(request.Phone),
                        DealerName = request.DealerName,
                        PackageTier = request.PackageTier?.ToUpper(), // Store tier filter
                        CodeCount = request.CodeCount,
                        InvitationType = "Invite",
                        InvitationToken = Guid.NewGuid().ToString("N"), // 32-character hex
                        Status = "Pending",
                        CreatedDate = DateTime.Now,
                        ExpiryDate = DateTime.Now.AddDays(await _configService.GetTokenExpiryDaysAsync())
                    };

                    _invitationRepository.Add(invitation);
                    await _invitationRepository.SaveChangesAsync();

                    _logger.LogInformation("✅ Created invitation {InvitationId} with token {Token}",
                        invitation.Id, invitation.InvitationToken);

                    // 5. Reserve codes for this invitation
                    foreach (var code in codesToReserve)
                    {
                        code.ReservedForInvitationId = invitation.Id;
                        code.ReservedAt = DateTime.Now;
                        _codeRepository.Update(code);
                    }
                    await _codeRepository.SaveChangesAsync();

                    _logger.LogInformation("✅ Reserved {Count} codes for invitation {InvitationId}",
                        codesToReserve.Count, invitation.Id);

                    // 6. Generate deep link using configuration service
                    var baseUrl = await _configService.GetDeepLinkBaseUrlAsync();
                    var deepLink = $"{baseUrl.TrimEnd('/')}/DEALER-{invitation.InvitationToken}";

                    // 7. Get Play Store link
                    var playStorePackageName = _configuration["MobileApp:PlayStorePackageName"] ?? "com.ziraai.app";
                    var playStoreLink = $"https://play.google.com/store/apps/details?id={playStorePackageName}";

                    // 8. Build SMS message using configuration service
                    var smsTemplate = await _configService.GetSmsTemplateAsync();
                    var smsMessage = smsTemplate
                        .Replace("{sponsorName}", sponsorCompanyName)
                        .Replace("{token}", invitation.InvitationToken)
                        .Replace("{deepLink}", deepLink)
                        .Replace("{playStoreLink}", playStoreLink);

                    // 9. Send SMS
                    var smsService = _messagingFactory.GetSmsService();
                    var sendResult = await smsService.SendSmsAsync(invitation.Phone, smsMessage);

                    if (!sendResult.Success)
                    {
                        _logger.LogWarning("⚠️ SMS send failed for invitation {InvitationId}: {Error}",
                            invitation.Id, sendResult.Message);

                        // Update invitation with failed status but don't fail the whole operation
                        invitation.LinkSentDate = DateTime.Now;
                        invitation.LinkSentVia = "SMS";
                        invitation.LinkDelivered = false;
                        await _invitationRepository.SaveChangesAsync();
                    }
                    else
                    {
                        _logger.LogInformation("✅ SMS sent successfully for invitation {InvitationId}", invitation.Id);

                        invitation.LinkSentDate = DateTime.Now;
                        invitation.LinkSentVia = "SMS";
                        invitation.LinkDelivered = true;
                        await _invitationRepository.SaveChangesAsync();
                    }

                    // 10. Build response
                    var response = new DealerInvitationResponseDto
                    {
                        InvitationId = invitation.Id,
                        InvitationToken = invitation.InvitationToken,
                        InvitationLink = deepLink,
                        Email = invitation.Email,
                        Phone = invitation.Phone,
                        DealerName = invitation.DealerName,
                        CodeCount = invitation.CodeCount,
                        Status = invitation.Status,
                        InvitationType = invitation.InvitationType,
                        CreatedAt = invitation.CreatedDate,
                        ExpiryDate = invitation.ExpiryDate,
                        SmsSent = sendResult.Success,
                        SmsDeliveryStatus = sendResult.Success ? "Sent" : "Failed"
                    };

                    var successMessage = sendResult.Success
                        ? $"📱 Bayilik daveti {request.Phone} numarasına SMS ile gönderildi"
                        : $"⚠️ Davetiye oluşturuldu ancak SMS gönderilemedi. Linki manuel olarak iletebilirsiniz: {deepLink}";

                    return new SuccessDataResult<DealerInvitationResponseDto>(response, successMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error creating dealer invitation for sponsor {SponsorId}", request.SponsorId);
                    return new ErrorDataResult<DealerInvitationResponseDto>("Bayilik daveti oluşturulurken hata oluştu");
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

            /// <summary>
            /// Intelligent code selection algorithm.
            /// Priority: 1) Tier filter (if specified) 2) Expiry date (FIFO) 3) Creation date (oldest first)
            /// Supports multi-purchase automatic selection.
            /// </summary>
            private async Task<List<SponsorshipCode>> GetCodesToTransferAsync(
                int sponsorId,
                int codeCount,
                string packageTier)
            {
                // Start with base query - available codes for sponsor
                var availableCodes = await _codeRepository.GetListAsync(c =>
                    c.SponsorId == sponsorId &&
                    !c.IsUsed &&
                    c.DealerId == null &&  // Not already transferred
                    c.ReservedForInvitationId == null &&  // Not reserved
                    c.ExpiryDate > DateTime.Now);  // Not expired

                var codesList = availableCodes.ToList();

                // Apply tier filter if specified
                if (!string.IsNullOrEmpty(packageTier))
                {
                    // Get tier ID for the specified tier string (S, M, L, XL)
                    var tier = await _tierRepository.GetAsync(t => t.TierName == packageTier.ToUpper());
                    
                    if (tier != null)
                    {
                        // Filter codes by tier
                        codesList = codesList
                            .Where(c => c.SubscriptionTierId == tier.Id)
                            .ToList();
                    }
                }

                // Intelligent ordering:
                // 1. Codes expiring soonest first (prevent waste)
                // 2. Oldest codes first (FIFO for same expiry date)
                var selectedCodes = codesList
                    .OrderBy(c => c.ExpiryDate)
                    .ThenBy(c => c.CreatedDate)
                    .Take(codeCount)
                    .ToList();

                return selectedCodes;
            }
        }
    }
}
