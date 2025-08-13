using Business.Services.Sponsorship;
using Core.Utilities.Results;
using Entities.Concrete;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    public class GetSponsorshipCodesQuery : IRequest<IDataResult<List<SponsorshipCode>>>
    {
        public int SponsorId { get; set; }
        public bool OnlyUnused { get; set; } = false;

        public class GetSponsorshipCodesQueryHandler : IRequestHandler<GetSponsorshipCodesQuery, IDataResult<List<SponsorshipCode>>>
        {
            private readonly ISponsorshipService _sponsorshipService;

            public GetSponsorshipCodesQueryHandler(ISponsorshipService sponsorshipService)
            {
                _sponsorshipService = sponsorshipService;
            }

            public async Task<IDataResult<List<SponsorshipCode>>> Handle(GetSponsorshipCodesQuery request, CancellationToken cancellationToken)
            {
                if (request.OnlyUnused)
                {
                    return await _sponsorshipService.GetUnusedSponsorCodesAsync(request.SponsorId);
                }
                else
                {
                    return await _sponsorshipService.GetSponsorCodesAsync(request.SponsorId);
                }
            }
        }
    }
}