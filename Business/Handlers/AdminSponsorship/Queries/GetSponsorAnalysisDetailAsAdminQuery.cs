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
    /// Admin query to view specific analysis detail from sponsor's perspective
    /// </summary>
    public class GetSponsorAnalysisDetailAsAdminQuery : IRequest<IDataResult<SponsoredAnalysisDetailDto>>
    {
        public int SponsorId { get; set; }
        public int PlantAnalysisId { get; set; }

        public class GetSponsorAnalysisDetailAsAdminQueryHandler : IRequestHandler<GetSponsorAnalysisDetailAsAdminQuery, IDataResult<SponsoredAnalysisDetailDto>>
        {
            private readonly IMediator _mediator;

            public GetSponsorAnalysisDetailAsAdminQueryHandler(IMediator mediator)
            {
                _mediator = mediator;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsoredAnalysisDetailDto>> Handle(GetSponsorAnalysisDetailAsAdminQuery request, CancellationToken cancellationToken)
            {
                // Reuse existing GetFilteredAnalysisForSponsorQuery logic
                // Admin can view any sponsor's analysis detail by specifying SponsorId and PlantAnalysisId
                var sponsorQuery = new GetFilteredAnalysisForSponsorQuery
                {
                    SponsorId = request.SponsorId,
                    PlantAnalysisId = request.PlantAnalysisId
                };

                var result = await _mediator.Send(sponsorQuery, cancellationToken);

                if (!result.Success)
                {
                    return new ErrorDataResult<SponsoredAnalysisDetailDto>(result.Message);
                }

                return new SuccessDataResult<SponsoredAnalysisDetailDto>(
                    result.Data,
                    $"Admin retrieved analysis {request.PlantAnalysisId} detail for sponsor {request.SponsorId}"
                );
            }
        }
    }
}
