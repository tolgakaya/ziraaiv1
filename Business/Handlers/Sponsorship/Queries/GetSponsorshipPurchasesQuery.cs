using Business.Services.Sponsorship;
using Core.Utilities.Results;
using Entities.Concrete;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    public class GetSponsorshipPurchasesQuery : IRequest<IDataResult<List<SponsorshipPurchase>>>
    {
        public int SponsorId { get; set; }

        public class GetSponsorshipPurchasesQueryHandler : IRequestHandler<GetSponsorshipPurchasesQuery, IDataResult<List<SponsorshipPurchase>>>
        {
            private readonly ISponsorshipService _sponsorshipService;

            public GetSponsorshipPurchasesQueryHandler(ISponsorshipService sponsorshipService)
            {
                _sponsorshipService = sponsorshipService;
            }

            public async Task<IDataResult<List<SponsorshipPurchase>>> Handle(GetSponsorshipPurchasesQuery request, CancellationToken cancellationToken)
            {
                return await _sponsorshipService.GetSponsorPurchasesAsync(request.SponsorId);
            }
        }
    }
}