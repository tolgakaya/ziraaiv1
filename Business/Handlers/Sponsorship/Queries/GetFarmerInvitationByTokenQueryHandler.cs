using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Handler for getting farmer invitation details by token
    /// Authorization: AllowAnonymous (public endpoint for unregistered users)
    /// Used by mobile app to show invitation details before acceptance
    /// </summary>
    public class GetFarmerInvitationByTokenQueryHandler : IRequestHandler<GetFarmerInvitationByTokenQuery, IDataResult<FarmerInvitationDetailDto>>
    {
        private readonly IFarmerInvitationRepository _farmerInvitationRepository;
        private readonly ISponsorProfileRepository _sponsorProfileRepository;

        public GetFarmerInvitationByTokenQueryHandler(
            IFarmerInvitationRepository farmerInvitationRepository,
            ISponsorProfileRepository sponsorProfileRepository)
        {
            _farmerInvitationRepository = farmerInvitationRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
        }

        // AllowAnonymous - No SecuredOperation attribute needed
        public async Task<IDataResult<FarmerInvitationDetailDto>> Handle(GetFarmerInvitationByTokenQuery request, CancellationToken cancellationToken)
        {
            // Find invitation by token
            var invitation = await _farmerInvitationRepository.GetAsync(i => i.InvitationToken == request.InvitationToken);

            if (invitation == null)
            {
                return new ErrorDataResult<FarmerInvitationDetailDto>("Davetiye bulunamadı");
            }

            // Get sponsor profile for company name
            var sponsorProfile = await _sponsorProfileRepository.GetAsync(sp => sp.SponsorId == invitation.SponsorId);

            // Check if invitation can be accepted
            var now = DateTime.Now;
            var isExpired = invitation.ExpiryDate < now;
            var canAccept = invitation.Status == "Pending" && !isExpired;

            // Map to DTO (without revealing sensitive information like actual codes)
            var detailDto = new FarmerInvitationDetailDto
            {
                InvitationId = invitation.Id,
                SponsorName = sponsorProfile?.CompanyName ?? "ZiraAI Sponsor",
                CodeCount = invitation.CodeCount,
                PackageTier = invitation.PackageTier,
                ExpiresAt = invitation.ExpiryDate,
                RemainingDays = Math.Max(0, (int)(invitation.ExpiryDate - now).TotalDays),
                Status = isExpired && invitation.Status == "Pending" ? "Expired" : invitation.Status,
                CanAccept = canAccept,
                Message = canAccept
                    ? $"✅ {invitation.CodeCount} adet sponsorluk kodu {sponsorProfile?.CompanyName ?? "sponsor"} tarafından size gönderildi."
                    : isExpired
                        ? "❌ Bu davetiyenin süresi dolmuş. Lütfen sponsor ile iletişime geçin."
                        : "⚠️ Bu davetiye zaten kabul edilmiş veya iptal edilmiş.",
                CreatedAt = invitation.CreatedDate
            };

            return new SuccessDataResult<FarmerInvitationDetailDto>(detailDto);
        }
    }
}
