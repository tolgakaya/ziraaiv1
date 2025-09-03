using Business.Services.Sponsorship;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Entities.Concrete;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.PlantAnalyses.Queries
{
    public class GetFilteredAnalysisForSponsorQuery : IRequest<IDataResult<PlantAnalysis>>
    {
        public int SponsorId { get; set; }
        public int PlantAnalysisId { get; set; }

        public class GetFilteredAnalysisForSponsorQueryHandler : IRequestHandler<GetFilteredAnalysisForSponsorQuery, IDataResult<PlantAnalysis>>
        {
            private readonly ISponsorDataAccessService _dataAccessService;

            public GetFilteredAnalysisForSponsorQueryHandler(ISponsorDataAccessService dataAccessService)
            {
                _dataAccessService = dataAccessService;
            }

            [CacheAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<PlantAnalysis>> Handle(GetFilteredAnalysisForSponsorQuery request, CancellationToken cancellationToken)
            {
                // Check if sponsor has access to this analysis
                if (!await _dataAccessService.HasAccessToAnalysisAsync(request.SponsorId, request.PlantAnalysisId))
                {
                    return new ErrorDataResult<PlantAnalysis>("Access denied to this analysis");
                }

                var filteredAnalysis = await _dataAccessService.GetFilteredAnalysisDataAsync(request.SponsorId, request.PlantAnalysisId);
                
                if (filteredAnalysis == null)
                    return new ErrorDataResult<PlantAnalysis>("Analysis not found or access denied");

                return new SuccessDataResult<PlantAnalysis>(filteredAnalysis);
            }
        }
    }
}