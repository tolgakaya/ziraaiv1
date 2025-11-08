using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Handlers.PlantAnalyses.Queries;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;

namespace Business.Handlers.AdminSponsorship.Queries
{
    /// <summary>
    /// Admin query to view sponsor's analyses (admin impersonating sponsor perspective)
    /// </summary>
    public class GetSponsorAnalysesAsAdminQuery : IRequest<IDataResult<SponsoredAnalysesListResponseDto>>
    {
        public int SponsorId { get; set; }

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Sorting
        public string SortBy { get; set; } = "date";
        public string SortOrder { get; set; } = "desc";

        // Filters
        public string FilterByTier { get; set; }
        public string FilterByCropType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? DealerId { get; set; }
        public string FilterByMessageStatus { get; set; }
        public bool? HasUnreadMessages { get; set; }
        public bool? HasUnreadForCurrentUser { get; set; }
        public int? UnreadMessagesMin { get; set; }

        public class GetSponsorAnalysesAsAdminQueryHandler : IRequestHandler<GetSponsorAnalysesAsAdminQuery, IDataResult<SponsoredAnalysesListResponseDto>>
        {
            private readonly IMediator _mediator;

            public GetSponsorAnalysesAsAdminQueryHandler(IMediator mediator)
            {
                _mediator = mediator;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsoredAnalysesListResponseDto>> Handle(GetSponsorAnalysesAsAdminQuery request, CancellationToken cancellationToken)
            {
                // Reuse existing GetSponsoredAnalysesListQuery logic
                // Admin can view any sponsor's data by specifying SponsorId
                var sponsorQuery = new GetSponsoredAnalysesListQuery
                {
                    SponsorId = request.SponsorId,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    SortBy = request.SortBy,
                    SortOrder = request.SortOrder,
                    FilterByTier = request.FilterByTier,
                    FilterByCropType = request.FilterByCropType,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    DealerId = request.DealerId,
                    FilterByMessageStatus = request.FilterByMessageStatus,
                    HasUnreadMessages = request.HasUnreadMessages,
                    HasUnreadForCurrentUser = request.HasUnreadForCurrentUser,
                    UnreadMessagesMin = request.UnreadMessagesMin
                };

                var result = await _mediator.Send(sponsorQuery, cancellationToken);

                if (!result.Success)
                {
                    return new ErrorDataResult<SponsoredAnalysesListResponseDto>(result.Message);
                }

                return new SuccessDataResult<SponsoredAnalysesListResponseDto>(
                    result.Data,
                    $"Admin retrieved {result.Data.Items.Length} analyses for sponsor {request.SponsorId}"
                );
            }
        }
    }
}
