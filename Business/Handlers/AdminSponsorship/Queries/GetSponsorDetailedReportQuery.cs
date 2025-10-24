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

namespace Business.Handlers.AdminSponsorship.Queries
{
    /// <summary>
    /// Admin query to get detailed report for a specific sponsor
    /// Includes all purchases, codes, and usage statistics
    /// </summary>
    public class GetSponsorDetailedReportQuery : IRequest<IDataResult<SponsorDetailedReportDto>>
    {
        public int SponsorId { get; set; }

        public class GetSponsorDetailedReportQueryHandler : IRequestHandler<GetSponsorDetailedReportQuery, IDataResult<SponsorDetailedReportDto>>
        {
            private readonly ISponsorshipPurchaseRepository _purchaseRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly IUserRepository _userRepository;

            public GetSponsorDetailedReportQueryHandler(
                ISponsorshipPurchaseRepository purchaseRepository,
                ISponsorshipCodeRepository codeRepository,
                IUserRepository userRepository)
            {
                _purchaseRepository = purchaseRepository;
                _codeRepository = codeRepository;
                _userRepository = userRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsorDetailedReportDto>> Handle(GetSponsorDetailedReportQuery request, CancellationToken cancellationToken)
            {
                // Get sponsor info
                var sponsor = await _userRepository.GetAsync(u => u.UserId == request.SponsorId);
                if (sponsor == null)
                {
                    return new ErrorDataResult<SponsorDetailedReportDto>("Sponsor not found");
                }

                // Get all purchases
                var purchases = await _purchaseRepository.Query()
                    .Where(p => p.SponsorId == request.SponsorId)
                    .Include(p => p.SubscriptionTier)
                    .ToListAsync(cancellationToken);

                // Get all codes
                var codes = _codeRepository.GetList(c => c.SponsorId == request.SponsorId);

                var report = new SponsorDetailedReportDto
                {
                    SponsorId = request.SponsorId,
                    SponsorName = sponsor.FullName,
                    SponsorEmail = sponsor.Email,

                    // Purchase Statistics
                    TotalPurchases = purchases.Count,
                    ActivePurchases = purchases.Count(p => p.Status == "Active"),
                    PendingPurchases = purchases.Count(p => p.Status == "Pending"),
                    CancelledPurchases = purchases.Count(p => p.Status == "Cancelled"),
                    CompletedPurchases = purchases.Count(p => p.PaymentStatus == "Completed"),
                    TotalSpent = purchases.Where(p => p.PaymentStatus == "Completed").Sum(p => p.TotalAmount),

                    // Code Statistics
                    TotalCodesGenerated = codes.Count(),
                    TotalCodesSent = codes.Count(c => c.LinkSentDate.HasValue),
                    TotalCodesUsed = codes.Count(c => c.IsUsed),
                    TotalCodesActive = codes.Count(c => c.IsActive && !c.IsUsed),
                    TotalCodesExpired = codes.Count(c => c.ExpiryDate < System.DateTime.Now && !c.IsUsed),
                    CodeRedemptionRate = codes.Any() 
                        ? (double)codes.Count(c => c.IsUsed) / codes.Count() * 100 
                        : 0,

                    // Detailed Purchases
                    Purchases = purchases.Select(p => new PurchaseSummaryDto
                    {
                        Id = p.Id,
                        TierName = p.SubscriptionTier?.TierName,
                        Quantity = p.Quantity,
                        TotalAmount = p.TotalAmount,
                        Currency = p.Currency,
                        Status = p.Status,
                        PaymentStatus = p.PaymentStatus,
                        PurchaseDate = p.PurchaseDate,
                        CodesGenerated = p.CodesGenerated,
                        CodesUsed = p.CodesUsed,
                        CodesSent = codes.Count(c => c.SponsorshipPurchaseId == p.Id && c.LinkSentDate.HasValue)
                    }).ToList(),

                    // Code Distribution by Status
                    CodeDistribution = new CodeDistributionDto
                    {
                        Unused = codes.Count(c => !c.IsUsed && c.IsActive),
                        Used = codes.Count(c => c.IsUsed),
                        Expired = codes.Count(c => c.ExpiryDate < System.DateTime.Now && !c.IsUsed),
                        Deactivated = codes.Count(c => !c.IsActive),
                        Sent = codes.Count(c => c.LinkSentDate.HasValue),
                        NotSent = codes.Count(c => !c.LinkSentDate.HasValue)
                    }
                };

                return new SuccessDataResult<SponsorDetailedReportDto>(report, $"Detailed report for {sponsor.FullName} retrieved successfully");
            }
        }
    }

    public class SponsorDetailedReportDto
    {
        // Sponsor Info
        public int SponsorId { get; set; }
        public string SponsorName { get; set; }
        public string SponsorEmail { get; set; }

        // Purchase Statistics
        public int TotalPurchases { get; set; }
        public int ActivePurchases { get; set; }
        public int PendingPurchases { get; set; }
        public int CancelledPurchases { get; set; }
        public int CompletedPurchases { get; set; }
        public decimal TotalSpent { get; set; }

        // Code Statistics
        public int TotalCodesGenerated { get; set; }
        public int TotalCodesSent { get; set; }
        public int TotalCodesUsed { get; set; }
        public int TotalCodesActive { get; set; }
        public int TotalCodesExpired { get; set; }
        public double CodeRedemptionRate { get; set; }

        // Detailed Data
        public System.Collections.Generic.List<PurchaseSummaryDto> Purchases { get; set; }
        public CodeDistributionDto CodeDistribution { get; set; }
    }

    public class PurchaseSummaryDto
    {
        public int Id { get; set; }
        public string TierName { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public System.DateTime PurchaseDate { get; set; }
        public int CodesGenerated { get; set; }
        public int CodesUsed { get; set; }
        public int CodesSent { get; set; }
    }

    public class CodeDistributionDto
    {
        public int Unused { get; set; }
        public int Used { get; set; }
        public int Expired { get; set; }
        public int Deactivated { get; set; }
        public int Sent { get; set; }
        public int NotSent { get; set; }
    }
}
