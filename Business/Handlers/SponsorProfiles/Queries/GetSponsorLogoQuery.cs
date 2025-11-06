using Business.Services.Sponsor;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.SponsorProfiles.Queries
{
    public class GetSponsorLogoQuery : IRequest<IDataResult<SponsorLogoDto>>
    {
        public int SponsorId { get; set; }

        public class GetSponsorLogoQueryHandler : IRequestHandler<GetSponsorLogoQuery, IDataResult<SponsorLogoDto>>
        {
            private readonly ISponsorLogoService _sponsorLogoService;

            public GetSponsorLogoQueryHandler(ISponsorLogoService sponsorLogoService)
            {
                _sponsorLogoService = sponsorLogoService;
            }

            public async Task<IDataResult<SponsorLogoDto>> Handle(GetSponsorLogoQuery request, CancellationToken cancellationToken)
            {
                return await _sponsorLogoService.GetLogoUrlAsync(request.SponsorId);
            }
        }
    }
}
