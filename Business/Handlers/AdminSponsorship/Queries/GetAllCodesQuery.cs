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

namespace Business.Handlers.AdminSponsorship.Queries
{
    /// <summary>
    /// Admin query to get all sponsorship codes with pagination and filtering
    /// </summary>
    public class GetAllCodesQuery : IRequest<IDataResult<IEnumerable<SponsorshipCode>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public bool? IsUsed { get; set; }
        public bool? IsActive { get; set; }
        public int? SponsorId { get; set; }
        public int? PurchaseId { get; set; }

        public class GetAllCodesQueryHandler : IRequestHandler<GetAllCodesQuery, IDataResult<IEnumerable<SponsorshipCode>>>
        {
            private readonly ISponsorshipCodeRepository _codeRepository;

            public GetAllCodesQueryHandler(ISponsorshipCodeRepository codeRepository)
            {
                _codeRepository = codeRepository;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<IEnumerable<SponsorshipCode>>> Handle(GetAllCodesQuery request, CancellationToken cancellationToken)
            {
                var query = _codeRepository.Query().AsQueryable();

                // Apply filters
                if (request.IsUsed.HasValue)
                {
                    query = query.Where(c => c.IsUsed == request.IsUsed.Value);
                }

                if (request.IsActive.HasValue)
                {
                    query = query.Where(c => c.IsActive == request.IsActive.Value);
                }

                if (request.SponsorId.HasValue)
                {
                    query = query.Where(c => c.SponsorId == request.SponsorId.Value);
                }

                if (request.PurchaseId.HasValue)
                {
                    query = query.Where(c => c.SponsorshipPurchaseId == request.PurchaseId.Value);
                }

                var codes = query
                    .OrderByDescending(c => c.CreatedDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new SuccessDataResult<IEnumerable<SponsorshipCode>>(codes, "Codes retrieved successfully");
            }
        }
    }
}
