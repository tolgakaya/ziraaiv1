using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;

namespace Business.Handlers.AdminPlantAnalysis.Queries
{
    /// <summary>
    /// Admin query to get all analyses for a specific user
    /// </summary>
    public class GetUserAnalysesQuery : IRequest<IDataResult<IEnumerable<PlantAnalysis>>>
    {
        public int UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string Status { get; set; }
        public bool? IsOnBehalfOf { get; set; }

        public class GetUserAnalysesQueryHandler : IRequestHandler<GetUserAnalysesQuery, IDataResult<IEnumerable<PlantAnalysis>>>
        {
            private readonly IPlantAnalysisRepository _analysisRepository;

            public GetUserAnalysesQueryHandler(IPlantAnalysisRepository analysisRepository)
            {
                _analysisRepository = analysisRepository;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<IEnumerable<PlantAnalysis>>> Handle(GetUserAnalysesQuery request, CancellationToken cancellationToken)
            {
                var query = _analysisRepository.Query()
                    .Where(a => a.UserId == request.UserId)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(a => a.AnalysisStatus == request.Status);
                }

                if (request.IsOnBehalfOf.HasValue)
                {
                    query = query.Where(a => a.IsOnBehalfOf == request.IsOnBehalfOf.Value);
                }

                var analyses = query
                    .OrderByDescending(a => a.CreatedDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new SuccessDataResult<IEnumerable<PlantAnalysis>>(analyses, $"Found {analyses.Count} analyses for user");
            }
        }
    }
}
