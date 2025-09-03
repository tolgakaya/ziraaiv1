using Business.Services.Sponsorship;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.SmartLinks.Queries
{
    public class GetSponsorSmartLinksQuery : IRequest<IDataResult<List<SmartLinkDto>>>
    {
        public int SponsorId { get; set; }

        public class GetSponsorSmartLinksQueryHandler : IRequestHandler<GetSponsorSmartLinksQuery, IDataResult<List<SmartLinkDto>>>
        {
            private readonly ISmartLinkService _smartLinkService;

            public GetSponsorSmartLinksQueryHandler(ISmartLinkService smartLinkService)
            {
                _smartLinkService = smartLinkService;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<List<SmartLinkDto>>> Handle(GetSponsorSmartLinksQuery request, CancellationToken cancellationToken)
            {
                var links = await _smartLinkService.GetSponsorLinksAsync(request.SponsorId);

                var linkDtos = links.Select(l => new SmartLinkDto
                {
                    Id = l.Id,
                    SponsorId = l.SponsorId,
                    SponsorName = l.SponsorName,
                    LinkUrl = l.LinkUrl,
                    LinkText = l.LinkText,
                    LinkDescription = l.LinkDescription,
                    LinkType = l.LinkType,
                    Keywords = JsonSerializer.Deserialize<string[]>(l.Keywords ?? "[]"),
                    ProductCategory = l.ProductCategory,
                    TargetCropTypes = JsonSerializer.Deserialize<string[]>(l.TargetCropTypes ?? "[]"),
                    TargetDiseases = JsonSerializer.Deserialize<string[]>(l.TargetDiseases ?? "[]"),
                    Priority = l.Priority,
                    DisplayPosition = l.DisplayPosition,
                    DisplayStyle = l.DisplayStyle,
                    ProductName = l.ProductName,
                    ProductPrice = l.ProductPrice,
                    ProductCurrency = l.ProductCurrency,
                    IsPromotional = l.IsPromotional,
                    DiscountPercentage = l.DiscountPercentage,
                    ClickCount = l.ClickCount,
                    ClickThroughRate = l.ClickThroughRate,
                    RelevanceScore = l.RelevanceScore,
                    IsActive = l.IsActive,
                    IsApproved = l.IsApproved,
                    CreatedDate = l.CreatedDate
                }).ToList();

                return new SuccessDataResult<List<SmartLinkDto>>(linkDtos);
            }
        }
    }
}