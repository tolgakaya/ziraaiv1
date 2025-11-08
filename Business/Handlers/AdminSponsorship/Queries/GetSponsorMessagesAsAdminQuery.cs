using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Handlers.AnalysisMessages.Queries;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;

namespace Business.Handlers.AdminSponsorship.Queries
{
    /// <summary>
    /// Admin query to view message conversation for a specific analysis from sponsor's perspective
    /// </summary>
    public class GetSponsorMessagesAsAdminQuery : IRequest<PaginatedResult<List<AnalysisMessageDto>>>
    {
        public int SponsorId { get; set; }
        public int FarmerUserId { get; set; }
        public int PlantAnalysisId { get; set; }

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public class GetSponsorMessagesAsAdminQueryHandler : IRequestHandler<GetSponsorMessagesAsAdminQuery, PaginatedResult<List<AnalysisMessageDto>>>
        {
            private readonly IMediator _mediator;

            public GetSponsorMessagesAsAdminQueryHandler(IMediator mediator)
            {
                _mediator = mediator;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<PaginatedResult<List<AnalysisMessageDto>>> Handle(GetSponsorMessagesAsAdminQuery request, CancellationToken cancellationToken)
            {
                // Reuse existing GetConversationQuery logic
                // Admin can view conversation between sponsor and farmer for a specific analysis
                var conversationQuery = new GetConversationQuery
                {
                    FromUserId = request.SponsorId,
                    ToUserId = request.FarmerUserId,
                    PlantAnalysisId = request.PlantAnalysisId,
                    Page = request.Page,
                    PageSize = request.PageSize
                };

                var result = await _mediator.Send(conversationQuery, cancellationToken);

                return result;
            }
        }
    }
}
