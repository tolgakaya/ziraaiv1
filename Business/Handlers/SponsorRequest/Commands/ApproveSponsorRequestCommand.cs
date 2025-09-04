using Business.Services.SponsorRequest;
using Core.Utilities.Results;
using IResult = Core.Utilities.Results.IResult;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Microsoft.AspNetCore.Http;

namespace Business.Handlers.SponsorRequest.Commands
{
    public class ApproveSponsorRequestCommand : IRequest<IResult>
    {
        public List<int> RequestIds { get; set; }
        public int SubscriptionTierId { get; set; }
        public string ApprovalNotes { get; set; }

        public class ApproveSponsorRequestCommandHandler : IRequestHandler<ApproveSponsorRequestCommand, IResult>
        {
            private readonly ISponsorRequestService _sponsorRequestService;
            private readonly IHttpContextAccessor _httpContextAccessor;

            public ApproveSponsorRequestCommandHandler(
                ISponsorRequestService sponsorRequestService,
                IHttpContextAccessor httpContextAccessor)
            {
                _sponsorRequestService = sponsorRequestService;
                _httpContextAccessor = httpContextAccessor;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(ApproveSponsorRequestCommand request, CancellationToken cancellationToken)
            {
                // Get current user ID from JWT token (sponsor)
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int sponsorId))
                {
                    return new ErrorResult("User not authenticated");
                }

                return await _sponsorRequestService.ApproveRequestsAsync(
                    request.RequestIds, 
                    sponsorId, 
                    request.SubscriptionTierId, 
                    request.ApprovalNotes);
            }
        }
    }
}