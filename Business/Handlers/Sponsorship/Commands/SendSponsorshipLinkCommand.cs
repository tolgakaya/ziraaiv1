using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Constants;
using Business.Services.Redemption;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Sponsorship.Commands
{
    public class SendSponsorshipLinkCommand : IRequest<IDataResult<BulkSendResult>>
    {
        public int SponsorId { get; set; }
        public List<LinkRecipient> Recipients { get; set; } = new();
        public string Channel { get; set; } = "SMS"; // SMS or WhatsApp
        public string CustomMessage { get; set; } // Optional custom message

        public class LinkRecipient
        {
            public string Code { get; set; }
            public string Phone { get; set; }
            public string Name { get; set; }
        }

        public class SendSponsorshipLinkCommandHandler : IRequestHandler<SendSponsorshipLinkCommand, IDataResult<BulkSendResult>>
        {
            private readonly IRedemptionService _redemptionService;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly ILogger<SendSponsorshipLinkCommandHandler> _logger;

            public SendSponsorshipLinkCommandHandler(
                IRedemptionService redemptionService,
                ISponsorshipCodeRepository codeRepository,
                ILogger<SendSponsorshipLinkCommandHandler> logger)
            {
                _redemptionService = redemptionService;
                _codeRepository = codeRepository;
                _logger = logger;
            }

            [SecuredOperation(Priority = 1)]
            [CacheRemoveAspect("Get")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<BulkSendResult>> Handle(SendSponsorshipLinkCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("Sponsor {SponsorId} sending {Count} sponsorship links via {Channel}",
                        request.SponsorId, request.Recipients.Count, request.Channel);

                    // Validate that all codes belong to the sponsor
                    var codes = request.Recipients.Select(r => r.Code).ToList();
                    var sponsorCodes = await _codeRepository.GetListAsync(c => 
                        codes.Contains(c.Code) && 
                        c.SponsorId == request.SponsorId &&
                        !c.IsUsed &&
                        c.IsActive);

                    if (sponsorCodes.Count() != codes.Count)
                    {
                        var invalidCodes = codes.Except(sponsorCodes.Select(sc => sc.Code));
                        return new ErrorDataResult<BulkSendResult>(
                            $"Bazı kodlar geçersiz veya size ait değil: {string.Join(", ", invalidCodes)}");
                    }

                    // Prepare bulk send request
                    var bulkRequest = new BulkSendRequest
                    {
                        SponsorId = request.SponsorId,
                        Channel = request.Channel,
                        CustomMessage = request.CustomMessage,
                        Recipients = request.Recipients.Select(r => new RecipientInfo
                        {
                            Code = r.Code,
                            Phone = FormatPhoneNumber(r.Phone),
                            Name = r.Name
                        }).ToArray()
                    };

                    // Send links in bulk
                    var result = await _redemptionService.SendBulkSponsorshipLinksAsync(bulkRequest);

                    _logger.LogInformation("Bulk send completed. Success: {Success}, Failed: {Failed}",
                        result.Data.SuccessCount, result.Data.FailureCount);

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending sponsorship links for sponsor {SponsorId}", request.SponsorId);
                    return new ErrorDataResult<BulkSendResult>("Link gönderimi sırasında hata oluştu");
                }
            }

            private string FormatPhoneNumber(string phone)
            {
                // Remove all non-numeric characters
                var cleaned = new string(phone.Where(char.IsDigit).ToArray());

                // Add Turkey country code if not present
                if (!cleaned.StartsWith("90") && cleaned.Length == 10)
                {
                    cleaned = "90" + cleaned;
                }

                // Add + prefix
                if (!cleaned.StartsWith("+"))
                {
                    cleaned = "+" + cleaned;
                }

                return cleaned;
            }
        }
    }
}