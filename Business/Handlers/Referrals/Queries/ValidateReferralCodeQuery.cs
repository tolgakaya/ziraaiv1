using System;
using System.Threading;
using System.Threading.Tasks;
using Business.Services.Referral;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Referrals.Queries
{
    /// <summary>
    /// Validate if a referral code is valid and active (public endpoint - no auth required)
    /// </summary>
    public class ValidateReferralCodeQuery : IRequest<IResult>
    {
        public string Code { get; set; }

        public class ValidateReferralCodeQueryHandler : IRequestHandler<ValidateReferralCodeQuery, IResult>
        {
            private readonly IReferralCodeService _codeService;
            private readonly ILogger<ValidateReferralCodeQueryHandler> _logger;

            public ValidateReferralCodeQueryHandler(
                IReferralCodeService codeService,
                ILogger<ValidateReferralCodeQueryHandler> logger)
            {
                _codeService = codeService;
                _logger = logger;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(ValidateReferralCodeQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("Validating referral code: {Code}", request.Code);

                    var result = await _codeService.ValidateCodeAsync(request.Code);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating referral code {Code}", request.Code);
                    return new ErrorResult("Failed to validate referral code");
                }
            }
        }
    }
}
