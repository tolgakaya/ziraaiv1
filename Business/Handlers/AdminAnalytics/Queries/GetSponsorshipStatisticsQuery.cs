using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.AdminAnalytics.Queries
{
    /// <summary>
    /// Admin query to get sponsorship statistics and metrics
    /// </summary>
    public class GetSponsorshipStatisticsQuery : IRequest<IDataResult<SponsorshipStatisticsDto>>
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetSponsorshipStatisticsQueryHandler : IRequestHandler<GetSponsorshipStatisticsQuery, IDataResult<SponsorshipStatisticsDto>>
        {
            private readonly ISponsorshipPurchaseRepository _purchaseRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;

            public GetSponsorshipStatisticsQueryHandler(
                ISponsorshipPurchaseRepository purchaseRepository,
                ISponsorshipCodeRepository codeRepository)
            {
                _purchaseRepository = purchaseRepository;
                _codeRepository = codeRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsorshipStatisticsDto>> Handle(GetSponsorshipStatisticsQuery request, CancellationToken cancellationToken)
            {
                var allPurchases = _purchaseRepository.Query()
                    .Include(p => p.SubscriptionTier);

                var allCodes = _codeRepository.Query();

                // Apply date filters if provided
                var purchasesQuery = allPurchases.AsQueryable();
                var codesQuery = allCodes.AsQueryable();
                
                if (request.StartDate.HasValue)
                {
                    purchasesQuery = purchasesQuery.Where(p => p.PurchaseDate >= request.StartDate.Value);
                    codesQuery = codesQuery.Where(c => c.CreatedDate >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    purchasesQuery = purchasesQuery.Where(p => p.PurchaseDate <= request.EndDate.Value);
                    codesQuery = codesQuery.Where(c => c.CreatedDate <= request.EndDate.Value);
                }

                var purchasesList = await purchasesQuery.ToListAsync(cancellationToken);
                var codesList = await codesQuery.ToListAsync(cancellationToken);

                var stats = new SponsorshipStatisticsDto
                {
                    TotalPurchases = purchasesList.Count,
                    CompletedPurchases = purchasesList.Count(p => p.PaymentStatus == "Completed"),
                    PendingPurchases = purchasesList.Count(p => p.PaymentStatus == "Pending"),
                    RefundedPurchases = purchasesList.Count(p => p.PaymentStatus == "Refunded"),
                    TotalRevenue = purchasesList
                        .Where(p => p.PaymentStatus == "Completed")
                        .Sum(p => p.TotalAmount),
                    TotalCodesGenerated = codesList.Count,
                    TotalCodesUsed = codesList.Count(c => c.IsUsed),
                    TotalCodesActive = codesList.Count(c => c.IsActive && !c.IsUsed),
                    TotalCodesExpired = codesList.Count(c => c.ExpiryDate < DateTime.Now && !c.IsUsed),
                    CodeRedemptionRate = codesList.Any() 
                        ? (double)codesList.Count(c => c.IsUsed) / codesList.Count * 100 
                        : 0,
                    AveragePurchaseAmount = purchasesList.Any() 
                        ? purchasesList.Average(p => p.TotalAmount) 
                        : 0,
                    TotalQuantityPurchased = purchasesList.Sum(p => p.Quantity),
                    UniqueSponsorCount = purchasesList.Select(p => p.SponsorId).Distinct().Count(),
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    GeneratedAt = DateTime.Now
                };

                return new SuccessDataResult<SponsorshipStatisticsDto>(stats, "Sponsorship statistics retrieved successfully");
            }
        }
    }

    public class SponsorshipStatisticsDto
    {
        public int TotalPurchases { get; set; }
        public int CompletedPurchases { get; set; }
        public int PendingPurchases { get; set; }
        public int RefundedPurchases { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalCodesGenerated { get; set; }
        public int TotalCodesUsed { get; set; }
        public int TotalCodesActive { get; set; }
        public int TotalCodesExpired { get; set; }
        public double CodeRedemptionRate { get; set; }
        public decimal AveragePurchaseAmount { get; set; }
        public int TotalQuantityPurchased { get; set; }
        public int UniqueSponsorCount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
