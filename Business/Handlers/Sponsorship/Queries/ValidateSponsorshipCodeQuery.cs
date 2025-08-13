using Business.Services.Sponsorship;
using Core.Utilities.Results;
using Entities.Concrete;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    public class ValidateSponsorshipCodeQuery : IRequest<IDataResult<SponsorshipCode>>
    {
        public string Code { get; set; }

        public class ValidateSponsorshipCodeQueryHandler : IRequestHandler<ValidateSponsorshipCodeQuery, IDataResult<SponsorshipCode>>
        {
            private readonly ISponsorshipService _sponsorshipService;

            public ValidateSponsorshipCodeQueryHandler(ISponsorshipService sponsorshipService)
            {
                _sponsorshipService = sponsorshipService;
            }

            public async Task<IDataResult<SponsorshipCode>> Handle(ValidateSponsorshipCodeQuery request, CancellationToken cancellationToken)
            {
                return await _sponsorshipService.ValidateCodeAsync(request.Code);
            }
        }
    }
}