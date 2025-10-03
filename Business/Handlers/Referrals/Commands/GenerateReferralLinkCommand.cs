using System;
using System.Collections.Generic;
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

namespace Business.Handlers.Referrals.Commands
{
    /// <summary>
    /// Generate and send referral links via SMS/WhatsApp (Hybrid supported)
    /// </summary>
    public class GenerateReferralLinkCommand : IRequest<IDataResult<Entities.Dtos.ReferralLinkResponse>>
    {
        public int UserId { get; set; }
        public int DeliveryMethod { get; set; } // 1=SMS, 2=WhatsApp, 3=Both
        public List<string> PhoneNumbers { get; set; }
        public string CustomMessage { get; set; }

        public class GenerateReferralLinkCommandHandler : IRequestHandler<GenerateReferralLinkCommand, IDataResult<Entities.Dtos.ReferralLinkResponse>>
        {
            private readonly IReferralLinkService _linkService;
            private readonly ILogger<GenerateReferralLinkCommandHandler> _logger;

            public GenerateReferralLinkCommandHandler(
                IReferralLinkService linkService,
                ILogger<GenerateReferralLinkCommandHandler> logger)
            {
                _linkService = linkService;
                _logger = logger;
            }

            [CacheRemoveAspect("Get")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<Entities.Dtos.ReferralLinkResponse>> Handle(GenerateReferralLinkCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("Generating referral link for user {UserId}, method: {DeliveryMethod}, recipients: {Count}",
                        request.UserId, request.DeliveryMethod, request.PhoneNumbers?.Count ?? 0);

                    var deliveryMethod = (DeliveryMethod)request.DeliveryMethod;

                    var result = await _linkService.GenerateAndSendLinksAsync(
                        request.UserId,
                        request.PhoneNumbers,
                        deliveryMethod,
                        request.CustomMessage);

                    if (!result.Success)
                        return new ErrorDataResult<Entities.Dtos.ReferralLinkResponse>(result.Message);

                    // Map to DTO
                    var responseDto = new Entities.Dtos.ReferralLinkResponse
                    {
                        ReferralCode = result.Data.ReferralCode,
                        DeepLink = result.Data.DeepLink,
                        PlayStoreLink = result.Data.PlayStoreLink,
                        ExpiresAt = result.Data.ExpiresAt,
                        DeliveryStatuses = result.Data.DeliveryStatuses.ConvertAll(ds => new DeliveryStatusDto
                        {
                            PhoneNumber = ds.PhoneNumber,
                            Method = ds.Method,
                            Status = ds.Status,
                            ErrorMessage = ds.ErrorMessage
                        })
                    };

                    return new SuccessDataResult<Entities.Dtos.ReferralLinkResponse>(responseDto, result.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating referral link for user {UserId}", request.UserId);
                    return new ErrorDataResult<Entities.Dtos.ReferralLinkResponse>("Failed to generate referral link");
                }
            }
        }
    }
}
