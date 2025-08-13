using Business.Services.Sponsorship;
using Core.Utilities.Results;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    public class GetSponsorshipStatisticsQuery : IRequest<IDataResult<object>>
    {
        public int SponsorId { get; set; }

        public class GetSponsorshipStatisticsQueryHandler : IRequestHandler<GetSponsorshipStatisticsQuery, IDataResult<object>>
        {
            private readonly ISponsorshipService _sponsorshipService;

            public GetSponsorshipStatisticsQueryHandler(ISponsorshipService sponsorshipService)
            {
                _sponsorshipService = sponsorshipService;
            }

            public async Task<IDataResult<object>> Handle(GetSponsorshipStatisticsQuery request, CancellationToken cancellationToken)
            {
                return await _sponsorshipService.GetSponsorshipStatisticsAsync(request.SponsorId);
            }
        }
    }
}