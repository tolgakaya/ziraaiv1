using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Public query (no authentication required) - validates by token only
    /// Used by mobile app to display invitation details before login/accept
    /// </summary>
    public class GetDealerInvitationDetailsQuery : IRequest<IDataResult<DealerInvitationDetailsDto>>
    {
        public string InvitationToken { get; set; }

        public class GetDealerInvitationDetailsQueryHandler : IRequestHandler<GetDealerInvitationDetailsQuery, IDataResult<DealerInvitationDetailsDto>>
        {
            private readonly IDealerInvitationRepository _invitationRepository;
            private readonly ISponsorProfileRepository _sponsorProfileRepository;
            private readonly ISubscriptionTierRepository _tierRepository;
            private readonly ISponsorshipPurchaseRepository _purchaseRepository;
            private readonly ILogger<GetDealerInvitationDetailsQueryHandler> _logger;

            public GetDealerInvitationDetailsQueryHandler(
                IDealerInvitationRepository invitationRepository,
                ISponsorProfileRepository sponsorProfileRepository,
                ISubscriptionTierRepository tierRepository,
                ISponsorshipPurchaseRepository purchaseRepository,
                ILogger<GetDealerInvitationDetailsQueryHandler> logger)
            {
                _invitationRepository = invitationRepository;
                _sponsorProfileRepository = sponsorProfileRepository;
                _tierRepository = tierRepository;
                _purchaseRepository = purchaseRepository;
                _logger = logger;
            }

            public async Task<IDataResult<DealerInvitationDetailsDto>> Handle(GetDealerInvitationDetailsQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("🔍 Fetching dealer invitation details for token: {Token}", request.InvitationToken);

                    // Find invitation by token
                    var invitation = await _invitationRepository.GetAsync(i =>
                        i.InvitationToken == request.InvitationToken);

                    if (invitation == null)
                    {
                        _logger.LogWarning("❌ Invitation not found for token: {Token}", request.InvitationToken);
                        return new ErrorDataResult<DealerInvitationDetailsDto>("Davetiye bulunamadı");
                    }

                    // Check if already accepted/rejected
                    if (invitation.Status != "Pending")
                    {
                        _logger.LogInformation("ℹ️ Invitation {InvitationId} status: {Status}", invitation.Id, invitation.Status);

                        if (invitation.Status == "Accepted")
                        {
                            return new ErrorDataResult<DealerInvitationDetailsDto>(
                                "Bu davetiye daha önce kabul edilmiş");
                        }
                        if (invitation.Status == "Rejected")
                        {
                            return new ErrorDataResult<DealerInvitationDetailsDto>(
                                "Bu davetiye reddedilmiş");
                        }
                        if (invitation.Status == "Expired")
                        {
                            return new ErrorDataResult<DealerInvitationDetailsDto>(
                                "Bu davetiyenin süresi dolmuş");
                        }
                    }

                    // Check expiry
                    if (invitation.ExpiryDate < DateTime.Now)
                    {
                        _logger.LogWarning("❌ Invitation {InvitationId} expired. Expiry: {Expiry}",
                            invitation.Id, invitation.ExpiryDate);

                        // Auto-update status to Expired
                        invitation.Status = "Expired";
                        await _invitationRepository.SaveChangesAsync();

                        return new ErrorDataResult<DealerInvitationDetailsDto>(
                            "Davetiyenin süresi dolmuş. Lütfen sponsor ile iletişime geçin");
                    }

                    // Get sponsor profile
                    var sponsorProfile = await _sponsorProfileRepository.GetAsync(sp =>
                        sp.SponsorId == invitation.SponsorId);

                    var sponsorCompanyName = sponsorProfile?.CompanyName ?? "ZiraAI Sponsor";

                    // Get package tier information (v2.0 - handle nullable PurchaseId)
                    string packageTier = "Unknown";

                    // Priority 1: Use PackageTier directly if specified (v2.0 feature)
                    if (!string.IsNullOrEmpty(invitation.PackageTier))
                    {
                        packageTier = invitation.PackageTier;
                        _logger.LogInformation("📦 Using PackageTier from invitation: {Tier}", packageTier);
                    }
                    // Priority 2: Fallback to PurchaseId lookup (backward compatibility)
                    else if (invitation.PurchaseId.HasValue)
                    {
                        var purchase = await _purchaseRepository.GetAsync(p => p.Id == invitation.PurchaseId.Value);
                        if (purchase != null)
                        {
                            var tier = await _tierRepository.GetAsync(t => t.Id == purchase.SubscriptionTierId);
                            packageTier = tier?.TierName ?? "Unknown";
                            _logger.LogInformation("📦 Using PackageTier from PurchaseId {PurchaseId}: {Tier}",
                                invitation.PurchaseId.Value, packageTier);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ No PackageTier or PurchaseId found for invitation {InvitationId}",
                            invitation.Id);
                    }

                    // Calculate remaining time
                    var remainingDays = (invitation.ExpiryDate - DateTime.Now).Days;

                    // Build response
                    var details = new DealerInvitationDetailsDto
                    {
                        InvitationId = invitation.Id,
                        SponsorCompanyName = sponsorCompanyName,
                        CodeCount = invitation.CodeCount,
                        PackageTier = packageTier,
                        ExpiresAt = invitation.ExpiryDate,
                        RemainingDays = remainingDays,
                        Status = invitation.Status,
                        InvitationMessage = $"🎉 {sponsorCompanyName} sizi bayilik ağına katılmaya davet ediyor!",
                        DealerEmail = invitation.Email,
                        DealerPhone = invitation.Phone,
                        CreatedAt = invitation.CreatedDate
                    };

                    _logger.LogInformation("✅ Invitation details fetched successfully. Invitation: {InvitationId}, Sponsor: {Sponsor}",
                        invitation.Id, sponsorCompanyName);

                    return new SuccessDataResult<DealerInvitationDetailsDto>(details, "Davetiye bilgileri başarıyla alındı");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error fetching invitation details for token: {Token}", request.InvitationToken);
                    return new ErrorDataResult<DealerInvitationDetailsDto>("Davetiye bilgileri alınırken hata oluştu");
                }
            }
        }
    }
}
