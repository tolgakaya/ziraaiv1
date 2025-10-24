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
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.AdminSponsorship.Queries
{
    /// <summary>
    /// Admin query to get all sponsorship purchases with pagination and filtering
    /// </summary>
    public class GetAllPurchasesQuery : IRequest<IDataResult<IEnumerable<SponsorshipPurchase>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public int? SponsorId { get; set; }

        public class GetAllPurchasesQueryHandler : IRequestHandler<GetAllPurchasesQuery, IDataResult<IEnumerable<SponsorshipPurchase>>>
        {
            private readonly ISponsorshipPurchaseRepository _purchaseRepository;

            public GetAllPurchasesQueryHandler(ISponsorshipPurchaseRepository purchaseRepository)
            {
                _purchaseRepository = purchaseRepository;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<IEnumerable<SponsorshipPurchase>>> Handle(GetAllPurchasesQuery request, CancellationToken cancellationToken)
            {
                var query = _purchaseRepository.Query()
                    .Include(p => p.SubscriptionTier)
                    .Include(p => p.Sponsor)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(p => p.Status == request.Status);
                }

                if (!string.IsNullOrEmpty(request.PaymentStatus))
                {
                    query = query.Where(p => p.PaymentStatus == request.PaymentStatus);
                }

                if (request.SponsorId.HasValue)
                {
                    query = query.Where(p => p.SponsorId == request.SponsorId.Value);
                }

                var purchases = await query
                    .OrderByDescending(p => p.PurchaseDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                return new SuccessDataResult<IEnumerable<SponsorshipPurchase>>(purchases, "Purchases retrieved successfully");
            }
        }
    }
}
