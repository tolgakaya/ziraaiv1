using Business.Constants;
using Business.Services.Sponsorship;
using Business.Handlers.SmartLinks.ValidationRules;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos;
using MediatR;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.SmartLinks.Commands
{
    public class CreateSmartLinkCommand : IRequest<IDataResult<SmartLinkDto>>
    {
        public int SponsorId { get; set; }
        public string LinkUrl { get; set; }
        public string LinkText { get; set; }
        public string LinkDescription { get; set; }
        public string LinkType { get; set; } = "Product";
        public string[] Keywords { get; set; }
        public string ProductCategory { get; set; }
        public string[] TargetCropTypes { get; set; }
        public string[] TargetDiseases { get; set; }
        public string[] TargetPests { get; set; }
        public int Priority { get; set; } = 50;
        public string DisplayPosition { get; set; } = "Inline";
        public string DisplayStyle { get; set; } = "Button";
        public string ProductName { get; set; }
        public decimal? ProductPrice { get; set; }
        public string ProductCurrency { get; set; } = "TRY";
        public bool IsPromotional { get; set; }
        public decimal? DiscountPercentage { get; set; }

        public class CreateSmartLinkCommandHandler : IRequestHandler<CreateSmartLinkCommand, IDataResult<SmartLinkDto>>
        {
            private readonly ISmartLinkService _smartLinkService;

            public CreateSmartLinkCommandHandler(ISmartLinkService smartLinkService)
            {
                _smartLinkService = smartLinkService;
            }

            [ValidationAspect(typeof(CreateSmartLinkValidator), Priority = 1)]
            [CacheRemoveAspect("Get")]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SmartLinkDto>> Handle(CreateSmartLinkCommand request, CancellationToken cancellationToken)
            {
                // Check if sponsor can create smart links
                if (!await _smartLinkService.CanCreateSmartLinksAsync(request.SponsorId))
                {
                    return new ErrorDataResult<SmartLinkDto>(Messages.SmartLinkNotAllowed);
                }

                // Check quota
                var maxLinks = await _smartLinkService.GetMaxSmartLinksAsync(request.SponsorId);
                var activeCount = await _smartLinkService.GetActiveSmartLinksCountAsync(request.SponsorId);
                
                if (activeCount >= maxLinks)
                {
                    return new ErrorDataResult<SmartLinkDto>(Messages.SmartLinkQuotaExceeded);
                }

                var smartLink = new SmartLink
                {
                    SponsorId = request.SponsorId,
                    LinkUrl = request.LinkUrl,
                    LinkText = request.LinkText,
                    LinkDescription = request.LinkDescription,
                    LinkType = request.LinkType,
                    Keywords = JsonSerializer.Serialize(request.Keywords ?? new string[0]),
                    ProductCategory = request.ProductCategory,
                    TargetCropTypes = JsonSerializer.Serialize(request.TargetCropTypes ?? new string[0]),
                    TargetDiseases = JsonSerializer.Serialize(request.TargetDiseases ?? new string[0]),
                    TargetPests = JsonSerializer.Serialize(request.TargetPests ?? new string[0]),
                    Priority = request.Priority,
                    DisplayPosition = request.DisplayPosition,
                    DisplayStyle = request.DisplayStyle,
                    ProductName = request.ProductName,
                    ProductPrice = request.ProductPrice,
                    ProductCurrency = request.ProductCurrency,
                    IsPromotional = request.IsPromotional,
                    DiscountPercentage = request.DiscountPercentage
                };

                var createdLink = await _smartLinkService.CreateSmartLinkAsync(smartLink);
                if (createdLink == null)
                    return new ErrorDataResult<SmartLinkDto>(Messages.SmartLinkCreationFailed);

                var linkDto = new SmartLinkDto
                {
                    Id = createdLink.Id,
                    SponsorId = createdLink.SponsorId,
                    SponsorName = createdLink.SponsorName,
                    LinkUrl = createdLink.LinkUrl,
                    LinkText = createdLink.LinkText,
                    LinkDescription = createdLink.LinkDescription,
                    LinkType = createdLink.LinkType,
                    Keywords = JsonSerializer.Deserialize<string[]>(createdLink.Keywords ?? "[]"),
                    ProductCategory = createdLink.ProductCategory,
                    TargetCropTypes = JsonSerializer.Deserialize<string[]>(createdLink.TargetCropTypes ?? "[]"),
                    TargetDiseases = JsonSerializer.Deserialize<string[]>(createdLink.TargetDiseases ?? "[]"),
                    Priority = createdLink.Priority,
                    DisplayPosition = createdLink.DisplayPosition,
                    DisplayStyle = createdLink.DisplayStyle,
                    ProductName = createdLink.ProductName,
                    ProductPrice = createdLink.ProductPrice,
                    ProductCurrency = createdLink.ProductCurrency,
                    IsPromotional = createdLink.IsPromotional,
                    DiscountPercentage = createdLink.DiscountPercentage,
                    ClickCount = createdLink.ClickCount,
                    ClickThroughRate = createdLink.ClickThroughRate,
                    IsActive = createdLink.IsActive,
                    IsApproved = createdLink.IsApproved,
                    CreatedDate = createdLink.CreatedDate
                };

                return new SuccessDataResult<SmartLinkDto>(linkDto, Messages.SmartLinkCreated);
            }
        }
    }
}