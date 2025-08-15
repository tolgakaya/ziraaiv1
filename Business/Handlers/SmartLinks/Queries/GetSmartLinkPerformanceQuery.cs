using Business.Services.Sponsorship;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.SmartLinks.Queries
{
    public class GetSmartLinkPerformanceQuery : IRequest<IDataResult<List<SmartLinkPerformanceDto>>>
    {
        public int SponsorId { get; set; }

        public class GetSmartLinkPerformanceQueryHandler : IRequestHandler<GetSmartLinkPerformanceQuery, IDataResult<List<SmartLinkPerformanceDto>>>
        {
            private readonly ISmartLinkService _smartLinkService;

            public GetSmartLinkPerformanceQueryHandler(ISmartLinkService smartLinkService)
            {
                _smartLinkService = smartLinkService;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<List<SmartLinkPerformanceDto>>> Handle(GetSmartLinkPerformanceQuery request, CancellationToken cancellationToken)
            {
                var performanceLinks = await _smartLinkService.GetTopPerformingLinksAsync(request.SponsorId, 20);

                var performanceDtos = performanceLinks.Select(l => new SmartLinkPerformanceDto
                {
                    Id = l.Id,
                    LinkText = l.LinkText,
                    ProductName = l.ProductName,
                    ClickCount = l.ClickCount,
                    DisplayCount = l.DisplayCount,
                    ClickThroughRate = l.ClickThroughRate,
                    ConversionCount = l.ConversionCount,
                    ConversionRate = l.ConversionRate,
                    LastClickDate = l.LastClickDate ?? System.DateTime.MinValue
                }).ToList();

                return new SuccessDataResult<List<SmartLinkPerformanceDto>>(performanceDtos);
            }
        }
    }
}