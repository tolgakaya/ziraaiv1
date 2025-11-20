using Business.Services.Sponsor;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.SponsorProfiles.Commands
{
    public class UploadSponsorLogoCommand : IRequest<IDataResult<SponsorLogoUploadResult>>
    {
        public int SponsorId { get; set; }
        public IFormFile File { get; set; }

        public class UploadSponsorLogoCommandHandler : IRequestHandler<UploadSponsorLogoCommand, IDataResult<SponsorLogoUploadResult>>
        {
            private readonly ISponsorLogoService _sponsorLogoService;

            public UploadSponsorLogoCommandHandler(ISponsorLogoService sponsorLogoService)
            {
                _sponsorLogoService = sponsorLogoService;
            }

            public async Task<IDataResult<SponsorLogoUploadResult>> Handle(UploadSponsorLogoCommand request, CancellationToken cancellationToken)
            {
                return await _sponsorLogoService.UploadLogoAsync(request.SponsorId, request.File);
            }
        }
    }
}
