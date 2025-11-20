using Business.Services.Sponsorship;
using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    public class GetSponsorshipCodesQuery : IRequest<IDataResult<SponsorshipCodesPaginatedDto>>
    {
        public int SponsorId { get; set; }
        public bool OnlyUnused { get; set; } = false;
        public bool OnlyUnsent { get; set; } = false;
        public int? SentDaysAgo { get; set; } = null;
        public bool OnlySentExpired { get; set; } = false;
        public bool ExcludeDealerTransferred { get; set; } = false;
        public bool ExcludeReserved { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;

        public class GetSponsorshipCodesQueryHandler : IRequestHandler<GetSponsorshipCodesQuery, IDataResult<SponsorshipCodesPaginatedDto>>
        {
            private readonly ISponsorshipService _sponsorshipService;

            public GetSponsorshipCodesQueryHandler(ISponsorshipService sponsorshipService)
            {
                _sponsorshipService = sponsorshipService;
            }

            public async Task<IDataResult<SponsorshipCodesPaginatedDto>> Handle(GetSponsorshipCodesQuery request, CancellationToken cancellationToken)
            {
                // Priority 1: OnlySentExpired - codes sent to farmers but expired without being used
                if (request.OnlySentExpired)
                {
                    return await _sponsorshipService.GetSentExpiredCodesAsync(
                        request.SponsorId,
                        request.Page,
                        request.PageSize,
                        request.ExcludeDealerTransferred,
                        request.ExcludeReserved);
                }

                // Priority 2: OnlyUnsent - codes never sent to farmers (DistributionDate IS NULL)
                if (request.OnlyUnsent)
                {
                    return await _sponsorshipService.GetUnsentSponsorCodesAsync(
                        request.SponsorId,
                        request.Page,
                        request.PageSize,
                        request.ExcludeDealerTransferred,
                        request.ExcludeReserved);
                }

                // Priority 3: SentDaysAgo - codes sent X days ago but still unused
                if (request.SentDaysAgo.HasValue)
                {
                    return await _sponsorshipService.GetSentButUnusedSponsorCodesAsync(
                        request.SponsorId,
                        request.SentDaysAgo.Value,
                        request.Page,
                        request.PageSize,
                        request.ExcludeDealerTransferred,
                        request.ExcludeReserved);
                }

                // Priority 4: OnlyUnused - codes not redeemed (includes both sent and unsent)
                if (request.OnlyUnused)
                {
                    return await _sponsorshipService.GetUnusedSponsorCodesAsync(
                        request.SponsorId,
                        request.Page,
                        request.PageSize,
                        request.ExcludeDealerTransferred,
                        request.ExcludeReserved);
                }

                // Default: All codes (paginated)
                return await _sponsorshipService.GetSponsorCodesAsync(
                    request.SponsorId,
                    request.Page,
                    request.PageSize,
                    request.ExcludeDealerTransferred,
                    request.ExcludeReserved);
            }
        }
    }
}
