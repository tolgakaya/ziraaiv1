using Business.Services.SponsorRequest;
using Core.Utilities.Results;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Microsoft.AspNetCore.Http;

namespace Business.Handlers.SponsorRequest.Commands
{
    public class CreateSponsorRequestCommand : IRequest<IDataResult<string>>
    {
        public string SponsorPhone { get; set; }
        public string RequestMessage { get; set; }
        public int RequestedTierId { get; set; }

        public class CreateSponsorRequestCommandHandler : IRequestHandler<CreateSponsorRequestCommand, IDataResult<string>>
        {
            private readonly ISponsorRequestService _sponsorRequestService;
            private readonly IHttpContextAccessor _httpContextAccessor;

            public CreateSponsorRequestCommandHandler(
                ISponsorRequestService sponsorRequestService,
                IHttpContextAccessor httpContextAccessor)
            {
                _sponsorRequestService = sponsorRequestService;
                _httpContextAccessor = httpContextAccessor;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<string>> Handle(CreateSponsorRequestCommand request, CancellationToken cancellationToken)
            {
                // Get current user ID from JWT token
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int farmerId))
                {
                    return new ErrorDataResult<string>("User not authenticated");
                }

                return await _sponsorRequestService.CreateRequestAsync(
                    farmerId, 
                    request.SponsorPhone, 
                    request.RequestMessage, 
                    request.RequestedTierId);
            }
        }
    }
}