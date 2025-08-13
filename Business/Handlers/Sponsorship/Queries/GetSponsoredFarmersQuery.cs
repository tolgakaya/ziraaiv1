using Business.Services.Sponsorship;
using Core.Utilities.Results;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    public class GetSponsoredFarmersQuery : IRequest<IDataResult<List<object>>>
    {
        public int SponsorId { get; set; }

        public class GetSponsoredFarmersQueryHandler : IRequestHandler<GetSponsoredFarmersQuery, IDataResult<List<object>>>
        {
            private readonly ISponsorshipService _sponsorshipService;

            public GetSponsoredFarmersQueryHandler(ISponsorshipService sponsorshipService)
            {
                _sponsorshipService = sponsorshipService;
            }

            public async Task<IDataResult<List<object>>> Handle(GetSponsoredFarmersQuery request, CancellationToken cancellationToken)
            {
                return await _sponsorshipService.GetSponsoredFarmersAsync(request.SponsorId);
            }
        }
    }
}