using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.Referral;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Referrals.Commands
{
    /// <summary>
    /// Disable a referral code
    /// </summary>
    public class DisableReferralCodeCommand : IRequest<IResult>
    {
        public int UserId { get; set; }
        public string Code { get; set; }

        public class DisableReferralCodeCommandHandler : IRequestHandler<DisableReferralCodeCommand, IResult>
        {
            private readonly IReferralCodeService _codeService;
            private readonly ILogger<DisableReferralCodeCommandHandler> _logger;

            public DisableReferralCodeCommandHandler(
                IReferralCodeService codeService,
                ILogger<DisableReferralCodeCommandHandler> logger)
            {
                _codeService = codeService;
                _logger = logger;
            }

            [SecuredOperation(Priority = 1)]
            [CacheRemoveAspect("Get")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(DisableReferralCodeCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("Disabling referral code {Code} by user {UserId}",
                        request.Code, request.UserId);

                    var result = await _codeService.DisableCodeAsync(request.Code, request.UserId);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disabling referral code {Code}", request.Code);
                    return new ErrorResult("Failed to disable referral code");
                }
            }
        }
    }
}
