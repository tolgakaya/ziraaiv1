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
    /// Admin query to get all plant analyses created on behalf of users
    /// </summary>
    public class GetAllOBOAnalysesQuery : IRequest<IDataResult<OBOAnalysesDto>>
    {
        public int? AdminUserId { get; set; }
        public int? TargetUserId { get; set; }
        public string Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;

        public class GetAllOBOAnalysesQueryHandler : IRequestHandler<GetAllOBOAnalysesQuery, IDataResult<OBOAnalysesDto>>
        {
            private readonly IPlantAnalysisRepository _analysisRepository;

            public GetAllOBOAnalysesQueryHandler(IPlantAnalysisRepository analysisRepository)
            {
                _analysisRepository = analysisRepository;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<OBOAnalysesDto>> Handle(GetAllOBOAnalysesQuery request, CancellationToken cancellationToken)
            {
                var query = _analysisRepository.Query()
                    .Where(a => a.IsOnBehalfOf == true)
                    .AsQueryable();

                // Apply filters
                if (request.AdminUserId.HasValue)
                {
                    query = query.Where(a => a.CreatedByAdminId == request.AdminUserId.Value);
                }

                if (request.TargetUserId.HasValue)
                {
                    query = query.Where(a => a.UserId == request.TargetUserId.Value);
                }

                if (!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(a => a.AnalysisStatus == request.Status);
                }

                var totalCount = query.Count();
                var analyses = query
                    .OrderByDescending(a => a.CreatedDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var result = new OBOAnalysesDto
                {
                    Analyses = analyses,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalCount = totalCount
                };

                return new SuccessDataResult<OBOAnalysesDto>(result, "OBO analyses retrieved successfully");
            }
        }
    }

    public class OBOAnalysesDto
    {
        public List<PlantAnalysis> Analyses { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
