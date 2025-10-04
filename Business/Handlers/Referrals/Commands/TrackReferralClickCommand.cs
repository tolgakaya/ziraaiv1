using System;
using System.Threading;
using System.Threading.Tasks;
using Business.Services.Referral;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Referrals.Commands
{
    /// <summary>
    /// Track a referral link click (public endpoint - no auth required)
    /// </summary>
    public class TrackReferralClickCommand : IRequest<IResult>
    {
        public string Code { get; set; }
        public string IpAddress { get; set; }
        public string DeviceId { get; set; }

        public class TrackReferralClickCommandHandler : IRequestHandler<TrackReferralClickCommand, IResult>
        {
            private readonly IReferralTrackingService _trackingService;
            private readonly ILogger<TrackReferralClickCommandHandler> _logger;

            public TrackReferralClickCommandHandler(
                IReferralTrackingService trackingService,
                ILogger<TrackReferralClickCommandHandler> logger)
            {
                _trackingService = trackingService;
                _logger = logger;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(TrackReferralClickCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("Tracking referral click: Code {Code}, Device {DeviceId}, IP {IpAddress}",
                        request.Code, request.DeviceId, request.IpAddress);

                    var result = await _trackingService.TrackClickAsync(
                        request.Code,
                        request.IpAddress,
                        request.DeviceId);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error tracking referral click for code {Code}", request.Code);
                    return new ErrorResult("Failed to track click");
                }
            }
        }
    }
}
