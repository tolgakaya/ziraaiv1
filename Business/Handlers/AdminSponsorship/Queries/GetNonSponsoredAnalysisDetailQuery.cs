using System;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;

namespace Business.Handlers.AdminSponsorship.Queries
{
    /// <summary>
    /// Admin query to view detailed non-sponsored analysis (same view as farmer sees)
    /// </summary>
    public class GetNonSponsoredAnalysisDetailQuery : IRequest<IDataResult<PlantAnalysisDetailDto>>
    {
        public int PlantAnalysisId { get; set; }

        public class GetNonSponsoredAnalysisDetailQueryHandler : IRequestHandler<GetNonSponsoredAnalysisDetailQuery, IDataResult<PlantAnalysisDetailDto>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;
            private readonly IMediator _mediator;

            public GetNonSponsoredAnalysisDetailQueryHandler(
                IPlantAnalysisRepository plantAnalysisRepository,
                IMediator mediator)
            {
                _plantAnalysisRepository = plantAnalysisRepository;
                _mediator = mediator;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<PlantAnalysisDetailDto>> Handle(GetNonSponsoredAnalysisDetailQuery request, CancellationToken cancellationToken)
            {
                // Verify analysis exists and is non-sponsored
                var analysis = await _plantAnalysisRepository.GetAsync(p =>
                    p.Id == request.PlantAnalysisId &&
                    p.Status &&
                    string.IsNullOrEmpty(p.SponsorId) &&
                    p.SponsorshipCodeId == null &&
                    p.SponsorUserId == null);

                if (analysis == null)
                {
                    return new ErrorDataResult<PlantAnalysisDetailDto>("Non-sponsored analysis not found");
                }

                // Use the existing farmer-facing query to get the exact same view
                var farmerQuery = new PlantAnalyses.Queries.GetPlantAnalysisDetailQuery
                {
                    Id = request.PlantAnalysisId
                };

                var result = await _mediator.Send(farmerQuery, cancellationToken);

                return result;
            }
        }
    }
}
