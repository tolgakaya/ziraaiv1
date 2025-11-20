using Business.Services.Sponsor;
using Core.Utilities.Results;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using IResult = Core.Utilities.Results.IResult;

namespace Business.Handlers.SponsorProfiles.Commands
{
    public class DeleteSponsorLogoCommand : IRequest<IResult>
    {
        public int SponsorId { get; set; }

        public class DeleteSponsorLogoCommandHandler : IRequestHandler<DeleteSponsorLogoCommand, IResult>
        {
            private readonly ISponsorLogoService _sponsorLogoService;

            public DeleteSponsorLogoCommandHandler(ISponsorLogoService sponsorLogoService)
            {
                _sponsorLogoService = sponsorLogoService;
            }

            public async Task<IResult> Handle(DeleteSponsorLogoCommand request, CancellationToken cancellationToken)
            {
                return await _sponsorLogoService.DeleteLogoAsync(request.SponsorId);
            }
        }
    }
}
