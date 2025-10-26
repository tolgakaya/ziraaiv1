using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;

using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Get comprehensive ROI analytics for sponsor
    /// Includes cost/value calculations, ROI per tier, and efficiency metrics
    /// Cache TTL: 12 hours for relatively stable financial data
    /// Authorization: Sponsor, Admin roles only
    /// </summary>
    public class GetSponsorROIAnalyticsQuery : IRequest<IDataResult<SponsorROIAnalyticsDto>>
    {
        public int SponsorId { get; set; }

        public class GetSponsorROIAnalyticsQueryHandler : IRequestHandler<GetSponsorROIAnalyticsQuery, IDataResult<SponsorROIAnalyticsDto>>
        {
            private readonly ISponsorshipPurchaseRepository _purchaseRepository;
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly IPlantAnalysisRepository _analysisRepository;
            private readonly ISubscriptionTierRepository _tierRepository;
            private readonly ICacheManager _cacheManager;
            private readonly ILogger<GetSponsorROIAnalyticsQueryHandler> _logger;

            private const string CacheKeyPrefix = "SponsorROIAnalytics";
            private const int CacheDurationMinutes = 720; // 12 hours
            
            /// <summary>
            /// Standard value (in TL) generated per plant analysis
            /// This represents the market value of one analysis service
            /// Used to calculate total value and ROI across all sponsors
            /// </summary>
            private const decimal AnalysisUnitValue = 50.00m;

            public GetSponsorROIAnalyticsQueryHandler(
                ISponsorshipPurchaseRepository purchaseRepository,
                ISponsorshipCodeRepository codeRepository,
                IPlantAnalysisRepository analysisRepository,
                ISubscriptionTierRepository tierRepository,
                ICacheManager cacheManager,
                ILogger<GetSponsorROIAnalyticsQueryHandler> logger)
            {
                _purchaseRepository = purchaseRepository;
                _codeRepository = codeRepository;
                _analysisRepository = analysisRepository;
                _tierRepository = tierRepository;
                _cacheManager = cacheManager;
                _logger = logger;
            }

            private string GetCacheKey(int sponsorId)
            {
                return $"{CacheKeyPrefix}:{sponsorId}";
            }

            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<SponsorROIAnalyticsDto>> Handle(
                GetSponsorROIAnalyticsQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    // Check cache first
                    var cacheKey = GetCacheKey(request.SponsorId);
                    var cachedData = _cacheManager.Get<SponsorROIAnalyticsDto>(cacheKey);

                    if (cachedData != null)
                    {
                        _logger.LogInformation("[ROIAnalytics] Cache HIT for sponsor {SponsorId}", request.SponsorId);
                        return new SuccessDataResult<SponsorROIAnalyticsDto>(
                            cachedData,
                            "ROI analytics retrieved from cache");
                    }

                    _logger.LogInformation("[ROIAnalytics] Cache MISS for sponsor {SponsorId} - calculating ROI", request.SponsorId);

                    // Get all purchases for this sponsor
                    var purchases = await _purchaseRepository.GetListAsync(p => p.SponsorId == request.SponsorId);
                    var purchasesList = purchases.ToList();

                    if (!purchasesList.Any())
                    {
                        return new SuccessDataResult<SponsorROIAnalyticsDto>(
                            new SponsorROIAnalyticsDto
                            {
                                AnalysisUnitValue = AnalysisUnitValue
                            },
                            "No purchases found");
                    }

                    // Get all codes for this sponsor
                    var codes = await _codeRepository.GetListAsync(c => c.SponsorId == request.SponsorId);
                    var codesList = codes.ToList();

                    // Get all analyses sponsored by this sponsor
                    var analyses = await _analysisRepository.GetListAsync(a => 
                        a.SponsorCompanyId.HasValue && 
                        a.SponsorCompanyId.Value == request.SponsorId);
                    var analysesList = analyses.ToList();

                    // Calculate cost breakdown
                    var totalInvestment = purchasesList.Sum(p => p.TotalAmount);
                    var totalCodesPurchased = purchasesList.Sum(p => p.Quantity);
                    var totalCodesRedeemed = codesList.Count(c => c.IsUsed);
                    var totalAnalyses = analysesList.Count;
                    var uniqueFarmers = analysesList
                        .Where(a => a.UserId.HasValue)
                        .Select(a => a.UserId.Value)
                        .Distinct()
                        .Count();

                    var costPerCode = totalCodesPurchased > 0 ? totalInvestment / totalCodesPurchased : 0;
                    var costPerRedemption = totalCodesRedeemed > 0 ? totalInvestment / totalCodesRedeemed : 0;
                    var costPerAnalysis = totalAnalyses > 0 ? totalInvestment / totalAnalyses : 0;
                    var costPerFarmer = uniqueFarmers > 0 ? totalInvestment / uniqueFarmers : 0;

                    // Calculate value analysis
                    var totalAnalysesValue = totalAnalyses * AnalysisUnitValue;
                    
                    var avgLifetimeValuePerFarmer = uniqueFarmers > 0 
                        ? totalAnalysesValue / uniqueFarmers 
                        : 0;
                    
                    var avgValuePerCode = totalCodesRedeemed > 0 
                        ? totalAnalysesValue / totalCodesRedeemed 
                        : 0;

                    // Calculate overall ROI
                    var overallROI = totalInvestment > 0 
                        ? ((totalAnalysesValue - totalInvestment) / totalInvestment) * 100 
                        : 0;

                    var roiStatus = overallROI > 5 ? "Positive" : (overallROI < -5 ? "Negative" : "Breakeven");

                    // Calculate efficiency metrics
                    var utilizationRate = totalCodesPurchased > 0 
                        ? ((decimal)totalCodesRedeemed / totalCodesPurchased) * 100 
                        : 0;

                    var expiredCodes = codesList.Count(c => 
                        !c.IsUsed && 
                        c.ExpiryDate < DateTime.Now);

                    var wasteRate = totalCodesPurchased > 0 
                        ? ((decimal)expiredCodes / totalCodesPurchased) * 100 
                        : 0;

                    var breakevenAnalysisCount = AnalysisUnitValue > 0 
                        ? (int)Math.Ceiling(totalInvestment / AnalysisUnitValue) 
                        : 0;

                    var analysesUntilBreakeven = breakevenAnalysisCount - totalAnalyses;
                    if (analysesUntilBreakeven < 0) analysesUntilBreakeven = 0;

                    // Calculate payback period (estimated days to breakeven)
                    int? estimatedPaybackDays = null;
                    if (totalAnalyses > 0 && analysesUntilBreakeven > 0)
                    {
                        var firstAnalysisDate = analysesList.Min(a => a.AnalysisDate);
                        var daysSinceFirst = (DateTime.Now - firstAnalysisDate).Days;
                        if (daysSinceFirst > 0)
                        {
                            var analysesPerDay = (decimal)totalAnalyses / daysSinceFirst;
                            if (analysesPerDay > 0)
                            {
                                var daysToBreakeven = (int)Math.Ceiling(analysesUntilBreakeven / analysesPerDay);
                                estimatedPaybackDays = daysToBreakeven;
                            }
                        }
                    }

                    // Calculate ROI by tier
                    var roiByTier = new List<TierROI>();
                    var tierGroups = purchasesList.GroupBy(p => p.SubscriptionTierId);

                    foreach (var tierGroup in tierGroups)
                    {
                        var tierId = tierGroup.Key;
                        var tier = await _tierRepository.GetAsync(t => t.Id == tierId);
                        if (tier == null) continue;

                        var tierInvestment = tierGroup.Sum(p => p.TotalAmount);
                        var tierCodesPurchased = tierGroup.Sum(p => p.Quantity);
                        
                        var tierCodes = codesList.Where(c => c.SubscriptionTierId == tierId).ToList();
                        var tierCodesRedeemed = tierCodes.Count(c => c.IsUsed);
                        
                        // Get analyses for this tier via SponsorshipCodeId
                        var tierAnalysesCount = 0;
                        foreach (var analysis in analysesList)
                        {
                            if (!analysis.SponsorshipCodeId.HasValue) continue;
                            
                            var code = codesList.FirstOrDefault(c => c.Id == analysis.SponsorshipCodeId.Value);
                            if (code != null && code.SubscriptionTierId == tierId)
                            {
                                tierAnalysesCount++;
                            }
                        }

                        var tierTotalValue = tierAnalysesCount * AnalysisUnitValue;
                        var tierROI = tierInvestment > 0 
                            ? ((tierTotalValue - tierInvestment) / tierInvestment) * 100 
                            : 0;
                        
                        var tierUtilization = tierCodesPurchased > 0 
                            ? ((decimal)tierCodesRedeemed / tierCodesPurchased) * 100 
                            : 0;

                        roiByTier.Add(new TierROI
                        {
                            TierName = tier.TierName,
                            Investment = tierInvestment,
                            CodesPurchased = tierCodesPurchased,
                            CodesRedeemed = tierCodesRedeemed,
                            AnalysesGenerated = tierAnalysesCount,
                            TotalValue = tierTotalValue,
                            ROI = Math.Round(tierROI, 2),
                            UtilizationRate = Math.Round(tierUtilization, 2)
                        });
                    }

                    // Build final DTO
                    var analyticsDto = new SponsorROIAnalyticsDto
                    {
                        // Cost Breakdown
                        TotalInvestment = totalInvestment,
                        CostPerCode = Math.Round(costPerCode, 2),
                        CostPerRedemption = Math.Round(costPerRedemption, 2),
                        CostPerAnalysis = Math.Round(costPerAnalysis, 2),
                        CostPerFarmer = Math.Round(costPerFarmer, 2),

                        // Value Analysis
                        TotalAnalysesValue = totalAnalysesValue,
                        AverageLifetimeValuePerFarmer = Math.Round(avgLifetimeValuePerFarmer, 2),
                        AverageValuePerCode = Math.Round(avgValuePerCode, 2),

                        // ROI Metrics
                        OverallROI = Math.Round(overallROI, 2),
                        ROIStatus = roiStatus,
                        ROIByTier = roiByTier.OrderByDescending(r => r.ROI).ToList(),

                        // Efficiency Metrics
                        UtilizationRate = Math.Round(utilizationRate, 2),
                        WasteRate = Math.Round(wasteRate, 2),
                        BreakevenAnalysisCount = breakevenAnalysisCount,
                        AnalysesUntilBreakeven = analysesUntilBreakeven,
                        EstimatedPaybackDays = estimatedPaybackDays,

                        // Supporting Data
                        TotalCodesPurchased = totalCodesPurchased,
                        TotalCodesRedeemed = totalCodesRedeemed,
                        TotalAnalysesGenerated = totalAnalyses,
                        UniqueFarmersReached = uniqueFarmers,
                        AnalysisUnitValue = AnalysisUnitValue
                    };

                    // Cache for 12 hours
                    _cacheManager.Add(cacheKey, analyticsDto, CacheDurationMinutes);
                    _logger.LogInformation(
                        "[ROIAnalytics] Cached data for sponsor {SponsorId} (TTL: 12h)", 
                        request.SponsorId);

                    return new SuccessDataResult<SponsorROIAnalyticsDto>(
                        analyticsDto,
                        "ROI analytics retrieved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "[ROIAnalytics] Error fetching analytics for sponsor {SponsorId}", 
                        request.SponsorId);
                    return new ErrorDataResult<SponsorROIAnalyticsDto>(
                        $"Error retrieving ROI analytics: {ex.Message}");
                }
            }
        }
    }
}
