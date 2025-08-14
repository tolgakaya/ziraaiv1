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
                    _logger.LogInformation("📤 MOCK: Sponsor {SponsorId} sending {Count} sponsorship links via {Channel}",
                        request.SponsorId, request.Recipients.Count, request.Channel);

                    // MOCK IMPLEMENTATION - Skip database validation for now
                    _logger.LogInformation("📋 MOCK: Skipping database validation for codes: {Codes}", 
                        string.Join(", ", request.Recipients.Select(r => r.Code)));

                    // Mock successful bulk send result
                    var mockResult = new BulkSendResult
                    {
                        TotalSent = request.Recipients.Count,
                        SuccessCount = request.Recipients.Count,
                        FailureCount = 0,
                        Results = request.Recipients.Select(r => new SendResult
                        {
                            Code = r.Code,
                            Phone = FormatPhoneNumber(r.Phone),
                            Success = true,
                            ErrorMessage = null,
                            DeliveryStatus = "Mock Delivered"
                        }).ToArray()
                    };

                    _logger.LogInformation("📧 MOCK bulk send completed. Success: {Success}, Failed: {Failed}",
                        mockResult.SuccessCount, mockResult.FailureCount);

                    // Simulate network delay
                    await Task.Delay(300);

                    return new SuccessDataResult<BulkSendResult>(mockResult, 
                        $"📱 MOCK: {mockResult.SuccessCount} link başarıyla gönderildi via {request.Channel}");
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