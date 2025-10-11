using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Get comprehensive dashboard summary for sponsor mobile app
    /// Includes sent codes, analyses count, purchases, and tier-based package breakdowns
    /// </summary>
    public class GetSponsorDashboardSummaryQuery : IRequest<IDataResult<SponsorDashboardSummaryDto>>
    {
        public int SponsorId { get; set; }

        public class GetSponsorDashboardSummaryQueryHandler : IRequestHandler<GetSponsorDashboardSummaryQuery, IDataResult<SponsorDashboardSummaryDto>>
        {
            private readonly ISponsorshipCodeRepository _sponsorshipCodeRepository;
            private readonly ISponsorshipPurchaseRepository _sponsorshipPurchaseRepository;
            private readonly ISubscriptionTierRepository _subscriptionTierRepository;
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;
            private readonly IUserSubscriptionRepository _userSubscriptionRepository;

            public GetSponsorDashboardSummaryQueryHandler(
                ISponsorshipCodeRepository sponsorshipCodeRepository,
                ISponsorshipPurchaseRepository sponsorshipPurchaseRepository,
                ISubscriptionTierRepository subscriptionTierRepository,
                IPlantAnalysisRepository plantAnalysisRepository,
                IUserSubscriptionRepository userSubscriptionRepository)
            {
                _sponsorshipCodeRepository = sponsorshipCodeRepository;
                _sponsorshipPurchaseRepository = sponsorshipPurchaseRepository;
                _subscriptionTierRepository = subscriptionTierRepository;
                _plantAnalysisRepository = plantAnalysisRepository;
                _userSubscriptionRepository = userSubscriptionRepository;
            }

            public async Task<IDataResult<SponsorDashboardSummaryDto>> Handle(
                GetSponsorDashboardSummaryQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    // Get all sponsor's codes
                    var allCodes = await _sponsorshipCodeRepository.GetBySponsorIdAsync(request.SponsorId);

                    // Get all sponsor's purchases
                    var allPurchases = await _sponsorshipPurchaseRepository.GetBySponsorIdAsync(request.SponsorId);

                    // Get all tiers
                    var allTiers = await _subscriptionTierRepository.GetListAsync();

                    // Calculate top-level metrics
                    var totalCodes = allCodes.Count();
                    var sentCodes = allCodes.Count(c => c.DistributionDate.HasValue);
                    var sentCodesPercentage = totalCodes > 0 ? (decimal)sentCodes / totalCodes * 100 : 0;

                    // Calculate total analyses from sponsored subscriptions
                    var sponsoredSubscriptionIds = allCodes
                        .Where(c => c.CreatedSubscriptionId.HasValue)
                        .Select(c => c.CreatedSubscriptionId.Value)
                        .Distinct()
                        .ToList();

                    var totalAnalyses = 0;
                    if (sponsoredSubscriptionIds.Any())
                    {
                        // Count all analyses where ActiveSponsorshipId matches any of our subscription IDs
                        var analyses = await _plantAnalysisRepository.GetListAsync(
                            pa => pa.ActiveSponsorshipId.HasValue &&
                                  sponsoredSubscriptionIds.Contains(pa.ActiveSponsorshipId.Value));
                        totalAnalyses = analyses.Count();
                    }

                    // Purchases count
                    var purchasesCount = allPurchases.Count();

                    // Total spent
                    var totalSpent = allPurchases.Sum(p => p.TotalAmount);
                    var currency = allPurchases.FirstOrDefault()?.Currency ?? "TRY";

                    // Group codes by tier for active packages
                    var tierGroups = allCodes.GroupBy(c => c.SubscriptionTierId).ToList();
                    var activePackages = new List<ActivePackageSummary>();

                    foreach (var tierGroup in tierGroups)
                    {
                        var tier = allTiers.FirstOrDefault(t => t.Id == tierGroup.Key);
                        if (tier == null) continue;

                        var tierCodes = tierGroup.ToList();
                        var tierTotalCodes = tierCodes.Count;
                        var tierSentCodes = tierCodes.Count(c => c.DistributionDate.HasValue);
                        var tierUnsentCodes = tierCodes.Count(c => !c.DistributionDate.HasValue);
                        var tierUsedCodes = tierCodes.Count(c => c.IsUsed);
                        var tierUnusedSentCodes = tierCodes.Count(c => c.DistributionDate.HasValue && !c.IsUsed);

                        var tierUsagePercentage = tierSentCodes > 0
                            ? (decimal)tierUsedCodes / tierSentCodes * 100
                            : 0;

                        var tierDistributionPercentage = tierTotalCodes > 0
                            ? (decimal)tierSentCodes / tierTotalCodes * 100
                            : 0;

                        // Count unique farmers for this tier
                        var uniqueFarmers = tierCodes
                            .Where(c => c.UsedByUserId.HasValue)
                            .Select(c => c.UsedByUserId.Value)
                            .Distinct()
                            .Count();

                        // Count analyses for this tier's subscriptions
                        var tierSubscriptionIds = tierCodes
                            .Where(c => c.CreatedSubscriptionId.HasValue)
                            .Select(c => c.CreatedSubscriptionId.Value)
                            .ToList();

                        var tierAnalysesCount = 0;
                        if (tierSubscriptionIds.Any())
                        {
                            var tierAnalyses = await _plantAnalysisRepository.GetListAsync(
                                pa => pa.ActiveSponsorshipId.HasValue &&
                                      tierSubscriptionIds.Contains(pa.ActiveSponsorshipId.Value));
                            tierAnalysesCount = tierAnalyses.Count();
                        }

                        activePackages.Add(new ActivePackageSummary
                        {
                            TierName = tier.TierName,
                            TierDisplayName = tier.DisplayName,
                            TotalCodes = tierTotalCodes,
                            SentCodes = tierSentCodes,
                            UnsentCodes = tierUnsentCodes,
                            UsedCodes = tierUsedCodes,
                            UnusedSentCodes = tierUnusedSentCodes,
                            RemainingCodes = tierUnsentCodes,
                            UsagePercentage = Math.Round(tierUsagePercentage, 2),
                            DistributionPercentage = Math.Round(tierDistributionPercentage, 2),
                            UniqueFarmers = uniqueFarmers,
                            AnalysesCount = tierAnalysesCount
                        });
                    }

                    // Sort by tier order (S, M, L, XL)
                    var tierOrder = new Dictionary<string, int>
                    {
                        { "S", 1 },
                        { "M", 2 },
                        { "L", 3 },
                        { "XL", 4 }
                    };
                    activePackages = activePackages
                        .OrderBy(p => tierOrder.ContainsKey(p.TierName) ? tierOrder[p.TierName] : 99)
                        .ToList();

                    // Calculate overall statistics
                    var smsDistributions = allCodes.Count(c => c.DistributionChannel == "SMS");
                    var whatsAppDistributions = allCodes.Count(c => c.DistributionChannel == "WhatsApp");
                    var totalUsed = allCodes.Count(c => c.IsUsed);
                    var overallRedemptionRate = sentCodes > 0
                        ? (decimal)totalUsed / sentCodes * 100
                        : 0;

                    // Calculate average redemption time
                    var redemptionTimes = allCodes
                        .Where(c => c.DistributionDate.HasValue && c.UsedDate.HasValue)
                        .Select(c => (c.UsedDate.Value - c.DistributionDate.Value).TotalDays)
                        .ToList();

                    var avgRedemptionTime = redemptionTimes.Any()
                        ? (decimal)redemptionTimes.Average()
                        : 0;

                    // Total unique farmers
                    var totalUniqueFarmers = allCodes
                        .Where(c => c.UsedByUserId.HasValue)
                        .Select(c => c.UsedByUserId.Value)
                        .Distinct()
                        .Count();

                    // Last purchase and distribution dates
                    var lastPurchaseDate = allPurchases.Any()
                        ? allPurchases.Max(p => p.PurchaseDate)
                        : (DateTime?)null;

                    var lastDistributionDate = allCodes.Any(c => c.DistributionDate.HasValue)
                        ? allCodes.Where(c => c.DistributionDate.HasValue).Max(c => c.DistributionDate.Value)
                        : (DateTime?)null;

                    // Build response DTO
                    var summary = new SponsorDashboardSummaryDto
                    {
                        TotalCodesCount = totalCodes,
                        SentCodesCount = sentCodes,
                        SentCodesPercentage = Math.Round(sentCodesPercentage, 2),
                        TotalAnalysesCount = totalAnalyses,
                        PurchasesCount = purchasesCount,
                        TotalSpent = totalSpent,
                        Currency = currency,
                        ActivePackages = activePackages,
                        OverallStats = new OverallStatistics
                        {
                            SmsDistributions = smsDistributions,
                            WhatsAppDistributions = whatsAppDistributions,
                            OverallRedemptionRate = Math.Round(overallRedemptionRate, 2),
                            AverageRedemptionTime = Math.Round(avgRedemptionTime, 1),
                            TotalUniqueFarmers = totalUniqueFarmers,
                            LastPurchaseDate = lastPurchaseDate,
                            LastDistributionDate = lastDistributionDate
                        }
                    };

                    return new SuccessDataResult<SponsorDashboardSummaryDto>(
                        summary,
                        "Dashboard summary retrieved successfully");
                }
                catch (Exception ex)
                {
                    return new ErrorDataResult<SponsorDashboardSummaryDto>(
                        $"Error fetching dashboard summary: {ex.Message}");
                }
            }
        }
    }
}
