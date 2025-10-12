using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Get package-level distribution statistics: purchased vs distributed vs redeemed
    /// </summary>
    public class GetPackageDistributionStatisticsQuery : IRequest<IDataResult<PackageDistributionStatisticsDto>>
    {
        public int SponsorId { get; set; }

        public class GetPackageDistributionStatisticsQueryHandler : IRequestHandler<GetPackageDistributionStatisticsQuery, IDataResult<PackageDistributionStatisticsDto>>
        {
            private readonly ISponsorshipPurchaseRepository _purchaseRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly ISubscriptionTierRepository _tierRepository;
            private readonly ILogger<GetPackageDistributionStatisticsQueryHandler> _logger;

            public GetPackageDistributionStatisticsQueryHandler(
                ISponsorshipPurchaseRepository purchaseRepository,
                ISponsorshipCodeRepository codeRepository,
                ISubscriptionTierRepository tierRepository,
                ILogger<GetPackageDistributionStatisticsQueryHandler> logger)
            {
                _purchaseRepository = purchaseRepository;
                _codeRepository = codeRepository;
                _tierRepository = tierRepository;
                _logger = logger;
            }

            [SecuredOperation(Priority = 1)]
            [CacheAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<PackageDistributionStatisticsDto>> Handle(
                GetPackageDistributionStatisticsQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("Getting package distribution statistics for sponsor {SponsorId}", request.SponsorId);

                    // Get all purchases for this sponsor
                    var purchases = await _purchaseRepository.GetListAsync(p => p.SponsorId == request.SponsorId);
                    var purchasesList = purchases.ToList();

                    if (!purchasesList.Any())
                    {
                        return new SuccessDataResult<PackageDistributionStatisticsDto>(
                            new PackageDistributionStatisticsDto
                            {
                                TotalCodesPurchased = 0,
                                TotalCodesDistributed = 0,
                                TotalCodesRedeemed = 0,
                                CodesNotDistributed = 0,
                                CodesDistributedNotRedeemed = 0,
                                DistributionRate = 0,
                                RedemptionRate = 0,
                                OverallSuccessRate = 0,
                                PackageBreakdowns = new List<PackageBreakdown>(),
                                TierBreakdowns = new List<TierBreakdown>(),
                                ChannelBreakdowns = new List<ChannelBreakdown>()
                            },
                            "No purchases found");
                    }

                    // Get all codes for this sponsor
                    var codes = await _codeRepository.GetListAsync(c => c.SponsorId == request.SponsorId);
                    var codesList = codes.ToList();

                    // Calculate overall statistics
                    var totalCodesPurchased = codesList.Count;
                    var totalCodesDistributed = codesList.Count(c => c.LinkSentDate.HasValue);
                    var totalCodesRedeemed = codesList.Count(c => c.IsUsed);
                    var codesNotDistributed = totalCodesPurchased - totalCodesDistributed;
                    var codesDistributedNotRedeemed = totalCodesDistributed - totalCodesRedeemed;

                    var distributionRate = totalCodesPurchased > 0
                        ? (decimal)totalCodesDistributed / totalCodesPurchased * 100
                        : 0;
                    var redemptionRate = totalCodesDistributed > 0
                        ? (decimal)totalCodesRedeemed / totalCodesDistributed * 100
                        : 0;
                    var overallSuccessRate = totalCodesPurchased > 0
                        ? (decimal)totalCodesRedeemed / totalCodesPurchased * 100
                        : 0;

                    // Build package breakdowns
                    var packageBreakdowns = new List<PackageBreakdown>();
                    foreach (var purchase in purchasesList)
                    {
                        var purchaseCodes = codesList.Where(c => c.SponsorshipPurchaseId == purchase.Id).ToList();
                        var purchaseCodesDistributed = purchaseCodes.Count(c => c.LinkSentDate.HasValue);
                        var purchaseCodesRedeemed = purchaseCodes.Count(c => c.IsUsed);

                        var tier = await _tierRepository.GetAsync(t => t.Id == purchase.SubscriptionTierId);

                        packageBreakdowns.Add(new PackageBreakdown
                        {
                            PurchaseId = purchase.Id,
                            PurchaseDate = purchase.PurchaseDate,
                            TierName = tier?.TierName ?? "Unknown",
                            CodesPurchased = purchaseCodes.Count,
                            CodesDistributed = purchaseCodesDistributed,
                            CodesRedeemed = purchaseCodesRedeemed,
                            CodesNotDistributed = purchaseCodes.Count - purchaseCodesDistributed,
                            CodesDistributedNotRedeemed = purchaseCodesDistributed - purchaseCodesRedeemed,
                            DistributionRate = purchaseCodes.Count > 0
                                ? (decimal)purchaseCodesDistributed / purchaseCodes.Count * 100
                                : 0,
                            RedemptionRate = purchaseCodesDistributed > 0
                                ? (decimal)purchaseCodesRedeemed / purchaseCodesDistributed * 100
                                : 0,
                            TotalAmount = purchase.TotalAmount,
                            Currency = purchase.Currency
                        });
                    }

                    // Build tier breakdowns
                    var tierBreakdowns = new List<TierBreakdown>();
                    var tierGroups = codesList.GroupBy(c => c.SubscriptionTierId);
                    foreach (var tierGroup in tierGroups)
                    {
                        var tier = await _tierRepository.GetAsync(t => t.Id == tierGroup.Key);
                        var tierCodes = tierGroup.ToList();
                        var tierCodesDistributed = tierCodes.Count(c => c.LinkSentDate.HasValue);
                        var tierCodesRedeemed = tierCodes.Count(c => c.IsUsed);

                        tierBreakdowns.Add(new TierBreakdown
                        {
                            TierName = tier?.TierName ?? "Unknown",
                            TierDisplayName = tier?.DisplayName ?? "Unknown",
                            CodesPurchased = tierCodes.Count,
                            CodesDistributed = tierCodesDistributed,
                            CodesRedeemed = tierCodesRedeemed,
                            DistributionRate = tierCodes.Count > 0
                                ? (decimal)tierCodesDistributed / tierCodes.Count * 100
                                : 0,
                            RedemptionRate = tierCodesDistributed > 0
                                ? (decimal)tierCodesRedeemed / tierCodesDistributed * 100
                                : 0
                        });
                    }

                    // Build channel breakdowns
                    var channelBreakdowns = new List<ChannelBreakdown>();
                    var distributedCodes = codesList.Where(c => c.LinkSentDate.HasValue).ToList();
                    var channelGroups = distributedCodes.GroupBy(c => c.LinkSentVia ?? "Manual");

                    foreach (var channelGroup in channelGroups)
                    {
                        var channelCodes = channelGroup.ToList();
                        var channelCodesDelivered = channelCodes.Count(c => c.LinkDelivered);
                        var channelCodesRedeemed = channelCodes.Count(c => c.IsUsed);

                        channelBreakdowns.Add(new ChannelBreakdown
                        {
                            Channel = channelGroup.Key,
                            CodesDistributed = channelCodes.Count,
                            CodesDelivered = channelCodesDelivered,
                            CodesRedeemed = channelCodesRedeemed,
                            DeliveryRate = channelCodes.Count > 0
                                ? (decimal)channelCodesDelivered / channelCodes.Count * 100
                                : 0,
                            RedemptionRate = channelCodes.Count > 0
                                ? (decimal)channelCodesRedeemed / channelCodes.Count * 100
                                : 0
                        });
                    }

                    // Add "Not Distributed" channel for codes that were never sent
                    var notDistributedCount = codesList.Count(c => !c.LinkSentDate.HasValue);
                    if (notDistributedCount > 0)
                    {
                        channelBreakdowns.Add(new ChannelBreakdown
                        {
                            Channel = "Not Distributed",
                            CodesDistributed = 0,
                            CodesDelivered = 0,
                            CodesRedeemed = 0,
                            DeliveryRate = 0,
                            RedemptionRate = 0
                        });
                    }

                    var statistics = new PackageDistributionStatisticsDto
                    {
                        TotalCodesPurchased = totalCodesPurchased,
                        TotalCodesDistributed = totalCodesDistributed,
                        TotalCodesRedeemed = totalCodesRedeemed,
                        CodesNotDistributed = codesNotDistributed,
                        CodesDistributedNotRedeemed = codesDistributedNotRedeemed,
                        DistributionRate = distributionRate,
                        RedemptionRate = redemptionRate,
                        OverallSuccessRate = overallSuccessRate,
                        PackageBreakdowns = packageBreakdowns.OrderByDescending(p => p.PurchaseDate).ToList(),
                        TierBreakdowns = tierBreakdowns,
                        ChannelBreakdowns = channelBreakdowns.OrderByDescending(c => c.CodesDistributed).ToList()
                    };

                    _logger.LogInformation(
                        "Package distribution statistics: Purchased={Purchased}, Distributed={Distributed}, Redeemed={Redeemed}",
                        totalCodesPurchased, totalCodesDistributed, totalCodesRedeemed);

                    return new SuccessDataResult<PackageDistributionStatisticsDto>(
                        statistics,
                        "Package distribution statistics retrieved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting package distribution statistics for sponsor {SponsorId}", request.SponsorId);
                    return new ErrorDataResult<PackageDistributionStatisticsDto>(
                        "Error retrieving package distribution statistics");
                }
            }
        }
    }
}
