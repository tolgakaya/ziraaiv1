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
        public bool OnlyUnsent { get; set; } = false;
        public int? SentDaysAgo { get; set; } = null;

        public class GetSponsorshipCodesQueryHandler : IRequestHandler<GetSponsorshipCodesQuery, IDataResult<List<SponsorshipCode>>>
        {
            private readonly ISponsorshipService _sponsorshipService;

            public GetSponsorshipCodesQueryHandler(ISponsorshipService sponsorshipService)
            {
                _sponsorshipService = sponsorshipService;
            }

            public async Task<IDataResult<List<SponsorshipCode>>> Handle(GetSponsorshipCodesQuery request, CancellationToken cancellationToken)
            {
                // Priority 1: OnlyUnsent - codes never sent to farmers (DistributionDate IS NULL)
                if (request.OnlyUnsent)
                {
                    return await _sponsorshipService.GetUnsentSponsorCodesAsync(request.SponsorId);
                }

                // Priority 2: SentDaysAgo - codes sent X days ago but still unused
                if (request.SentDaysAgo.HasValue)
                {
                    return await _sponsorshipService.GetSentButUnusedSponsorCodesAsync(
                        request.SponsorId, request.SentDaysAgo.Value);
                }

                // Priority 3: OnlyUnused - codes not redeemed (includes both sent and unsent)
                if (request.OnlyUnused)
                {
                    return await _sponsorshipService.GetUnusedSponsorCodesAsync(request.SponsorId);
                }

                // Default: All codes
                return await _sponsorshipService.GetSponsorCodesAsync(request.SponsorId);
            }
        }
    }
}