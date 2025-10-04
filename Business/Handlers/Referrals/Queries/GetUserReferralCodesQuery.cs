using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Services.Referral;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Referrals.Queries
{
    /// <summary>
    /// Get all referral codes for a user
    /// </summary>
    public class GetUserReferralCodesQuery : IRequest<IDataResult<List<ReferralCodeDto>>>
    {
        public int UserId { get; set; }

        public class GetUserReferralCodesQueryHandler : IRequestHandler<GetUserReferralCodesQuery, IDataResult<List<ReferralCodeDto>>>
        {
            private readonly IReferralCodeService _codeService;
            private readonly ILogger<GetUserReferralCodesQueryHandler> _logger;

            public GetUserReferralCodesQueryHandler(
                IReferralCodeService codeService,
                ILogger<GetUserReferralCodesQueryHandler> logger)
            {
                _codeService = codeService;
                _logger = logger;
            }

            // Cache removed - referral data must be real-time
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<List<ReferralCodeDto>>> Handle(GetUserReferralCodesQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("Getting referral codes for user {UserId}", request.UserId);

                    var codesResult = await _codeService.GetUserCodesAsync(request.UserId);
                    if (!codesResult.Success)
                        return new ErrorDataResult<List<ReferralCodeDto>>(codesResult.Message);

                    var codes = codesResult.Data;

                    var codeDtos = codes.Select(c => new ReferralCodeDto
                    {
                        Id = c.Id,
                        Code = c.Code,
                        IsActive = c.IsActive,
                        CreatedAt = c.CreatedAt,
                        ExpiresAt = c.ExpiresAt,
                        Status = c.Status,
                        StatusText = GetStatusText(c.Status, c.ExpiresAt),
                        UsageCount = 0 // Will be populated from tracking service if needed
                    }).ToList();

                    return new SuccessDataResult<List<ReferralCodeDto>>(codeDtos);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting referral codes for user {UserId}", request.UserId);
                    return new ErrorDataResult<List<ReferralCodeDto>>("Failed to retrieve referral codes");
                }
            }

            private string GetStatusText(int status, DateTime expiresAt)
            {
                if (status == 2) return "Disabled";
                if (status == 1 || DateTime.Now > expiresAt) return "Expired";
                return "Active";
            }
        }
    }
}
