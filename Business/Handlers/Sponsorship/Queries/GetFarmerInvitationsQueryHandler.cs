using Business.BusinessAspects;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Handler for getting list of farmer invitations for a sponsor
    /// Authorization: Sponsor role only
    /// </summary>
    public class GetFarmerInvitationsQueryHandler : IRequestHandler<GetFarmerInvitationsQuery, IDataResult<List<FarmerInvitationListDto>>>
    {
        private readonly IFarmerInvitationRepository _farmerInvitationRepository;

        public GetFarmerInvitationsQueryHandler(IFarmerInvitationRepository farmerInvitationRepository)
        {
            _farmerInvitationRepository = farmerInvitationRepository;
        }

        [SecuredOperation(Priority = 1)]
        public async Task<IDataResult<List<FarmerInvitationListDto>>> Handle(GetFarmerInvitationsQuery request, CancellationToken cancellationToken)
        {
            // Get all invitations for this sponsor
            var invitations = await _farmerInvitationRepository.GetListAsync(i => i.SponsorId == request.SponsorId);

            // Filter by status if provided
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                invitations = invitations.Where(i => i.Status == request.Status).ToList();
            }

            // Map to DTO
            var invitationDtos = invitations.Select(i => new FarmerInvitationListDto
            {
                Id = i.Id,
                Phone = i.Phone,
                FarmerName = i.FarmerName,
                Email = i.Email,
                Status = i.Status,
                CodeCount = i.CodeCount,
                PackageTier = i.PackageTier,
                AcceptedByUserId = i.AcceptedByUserId,
                AcceptedDate = i.AcceptedDate,
                CreatedDate = i.CreatedDate,
                ExpiryDate = i.ExpiryDate,
                LinkDelivered = i.LinkDelivered,
                LinkSentDate = i.LinkSentDate
            })
            .OrderByDescending(i => i.CreatedDate)
            .ToList();

            return new SuccessDataResult<List<FarmerInvitationListDto>>(invitationDtos);
        }
    }
}
