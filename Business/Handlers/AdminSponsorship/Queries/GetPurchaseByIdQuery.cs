using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.AdminSponsorship.Queries
{
    /// <summary>
    /// Admin query to get sponsorship purchase by ID with full details
    /// </summary>
    public class GetPurchaseByIdQuery : IRequest<IDataResult<SponsorshipPurchase>>
    {
        public int PurchaseId { get; set; }

        public class GetPurchaseByIdQueryHandler : IRequestHandler<GetPurchaseByIdQuery, IDataResult<SponsorshipPurchase>>
        {
            private readonly ISponsorshipPurchaseRepository _purchaseRepository;

            public GetPurchaseByIdQueryHandler(ISponsorshipPurchaseRepository purchaseRepository)
            {
                _purchaseRepository = purchaseRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsorshipPurchase>> Handle(GetPurchaseByIdQuery request, CancellationToken cancellationToken)
            {
                var purchase = await _purchaseRepository.Query()
                    .Include(p => p.SubscriptionTier)
                    .Include(p => p.Sponsor)
                    .Include(p => p.SponsorshipCodes)
                    .FirstOrDefaultAsync(p => p.Id == request.PurchaseId, cancellationToken);

                if (purchase == null)
                {
                    return new ErrorDataResult<SponsorshipPurchase>("Purchase not found");
                }

                return new SuccessDataResult<SponsorshipPurchase>(purchase, "Purchase retrieved successfully");
            }
        }
    }
}
