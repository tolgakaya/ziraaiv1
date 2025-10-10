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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Get analysis statistics for each sponsorship code: which codes generated how many analyses
    /// </summary>
    public class GetCodeAnalysisStatisticsQuery : IRequest<IDataResult<CodeAnalysisStatisticsDto>>
    {
        public int SponsorId { get; set; }
        public bool IncludeAnalysisDetails { get; set; } = true; // Default: include full analysis list
        public int TopCodesCount { get; set; } = 10; // Show top N performing codes

        public class GetCodeAnalysisStatisticsQueryHandler : IRequestHandler<GetCodeAnalysisStatisticsQuery, IDataResult<CodeAnalysisStatisticsDto>>
        {
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly IUserSubscriptionRepository _subscriptionRepository;
            private readonly IPlantAnalysisRepository _analysisRepository;
            private readonly IUserRepository _userRepository;
            private readonly ISubscriptionTierRepository _tierRepository;
            private readonly IConfiguration _configuration;
            private readonly ILogger<GetCodeAnalysisStatisticsQueryHandler> _logger;

            public GetCodeAnalysisStatisticsQueryHandler(
                ISponsorshipCodeRepository codeRepository,
                IUserSubscriptionRepository subscriptionRepository,
                IPlantAnalysisRepository analysisRepository,
                IUserRepository userRepository,
                ISubscriptionTierRepository tierRepository,
                IConfiguration configuration,
                ILogger<GetCodeAnalysisStatisticsQueryHandler> logger)
            {
                _codeRepository = codeRepository;
                _subscriptionRepository = subscriptionRepository;
                _analysisRepository = analysisRepository;
                _userRepository = userRepository;
                _tierRepository = tierRepository;
                _configuration = configuration;
                _logger = logger;
            }

            [SecuredOperation(Priority = 1)]
            [CacheAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<CodeAnalysisStatisticsDto>> Handle(
                GetCodeAnalysisStatisticsQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("Getting code analysis statistics for sponsor {SponsorId}", request.SponsorId);

                    // Get all redeemed codes for this sponsor
                    var redeemedCodes = await _codeRepository.GetListAsync(c =>
                        c.SponsorId == request.SponsorId &&
                        c.IsUsed);

                    var redeemedCodesList = redeemedCodes.ToList();

                    if (!redeemedCodesList.Any())
                    {
                        return new SuccessDataResult<CodeAnalysisStatisticsDto>(
                            new CodeAnalysisStatisticsDto
                            {
                                TotalRedeemedCodes = 0,
                                TotalAnalysesPerformed = 0,
                                AverageAnalysesPerCode = 0,
                                TotalActiveFarmers = 0,
                                CodeBreakdowns = new List<CodeAnalysisBreakdown>(),
                                TopPerformingCodes = new List<CodeAnalysisBreakdown>(),
                                CropTypeDistribution = new List<CropTypeStatistic>(),
                                DiseaseDistribution = new List<DiseaseStatistic>()
                            },
                            "No redeemed codes found");
                    }

                    // Get base URL for analysis details links
                    var baseUrl = _configuration["WebAPI:BaseUrl"] ?? "https://ziraai.com";

                    // Build code breakdowns
                    var codeBreakdowns = new List<CodeAnalysisBreakdown>();
                    var allAnalyses = new List<SponsoredAnalysisSummary>();

                    foreach (var code in redeemedCodesList)
                    {
                        if (!code.UsedByUserId.HasValue || !code.CreatedSubscriptionId.HasValue)
                            continue;

                        // Get farmer details
                        var farmer = await _userRepository.GetAsync(u => u.UserId == code.UsedByUserId.Value);
                        if (farmer == null)
                            continue;

                        // Get subscription details
                        var subscription = await _subscriptionRepository.GetAsync(s => s.Id == code.CreatedSubscriptionId.Value);
                        if (subscription == null)
                            continue;

                        // Get tier details
                        var tier = await _tierRepository.GetAsync(t => t.Id == code.SubscriptionTierId);

                        // Get all analyses by this farmer during sponsored subscription
                        var farmerAnalyses = await _analysisRepository.GetListAsync(a =>
                            a.UserId == farmer.UserId &&
                            a.AnalysisDate >= subscription.StartDate &&
                            (subscription.EndDate == null || a.AnalysisDate <= subscription.EndDate));

                        var farmerAnalysesList = farmerAnalyses.ToList();

                        // Build analysis summaries
                        var analysisSummaries = new List<SponsoredAnalysisSummary>();
                        if (request.IncludeAnalysisDetails)
                        {
                            foreach (var analysis in farmerAnalysesList)
                            {
                                var summary = new SponsoredAnalysisSummary
                                {
                                    AnalysisId = analysis.Id,
                                    AnalysisDate = analysis.AnalysisDate,
                                    CropType = analysis.CropType ?? "Unknown",
                                    Disease = analysis.PrimaryIssue ?? analysis.PrimaryConcern ?? "Unknown",
                                    DiseaseCategory = analysis.HealthSeverity ?? "Unknown",
                                    Severity = analysis.HealthSeverity ?? "Unknown",
                                    Location = analysis.Location ?? "Unknown",
                                    Status = analysis.AnalysisStatus ?? "Completed",
                                    SponsorLogoDisplayed = analysis.ActiveSponsorshipId.HasValue,
                                    AnalysisDetailsUrl = $"{baseUrl.TrimEnd('/')}/api/v1/sponsorship/analysis/{analysis.Id}"
                                };
                                analysisSummaries.Add(summary);
                                allAnalyses.Add(summary);
                            }
                        }

                        // Determine data visibility based on tier
                        var tierName = tier?.TierName ?? "Unknown";
                        var farmerName = "Anonymous";
                        var farmerEmail = "";
                        var farmerPhone = "";
                        var location = farmer.Address ?? "Unknown";

                        // Data visibility rules (from sponsorship documentation)
                        if (tierName == "L" || tierName == "XL")
                        {
                            // 100% data visibility
                            farmerName = farmer.FullName ?? "Unknown";
                            farmerEmail = farmer.Email ?? "";
                            farmerPhone = farmer.MobilePhones ?? "";
                            location = farmer.Address ?? "Unknown";
                        }
                        else if (tierName == "M")
                        {
                            // 60% data visibility (no personal info)
                            farmerName = "Anonymous";
                            location = $"{farmer.Address?.Split(',').FirstOrDefault() ?? "Unknown"}";
                        }
                        else if (tierName == "S")
                        {
                            // 30% data visibility (minimal info)
                            farmerName = "Anonymous";
                            location = "Limited";
                        }

                        var lastAnalysis = farmerAnalysesList.OrderByDescending(a => a.AnalysisDate).FirstOrDefault();
                        var daysSinceLastAnalysis = lastAnalysis != null
                            ? (int)(DateTime.Now - lastAnalysis.AnalysisDate).TotalDays
                            : (int?)null;

                        var breakdown = new CodeAnalysisBreakdown
                        {
                            Code = code.Code,
                            TierName = tierName,
                            FarmerId = farmer.UserId,
                            FarmerName = farmerName,
                            FarmerEmail = farmerEmail,
                            FarmerPhone = farmerPhone,
                            Location = location,
                            RedeemedDate = code.UsedDate ?? code.CreatedDate,
                            SubscriptionStatus = subscription.IsActive ? "Active" : "Expired",
                            SubscriptionEndDate = subscription.EndDate,
                            TotalAnalyses = farmerAnalysesList.Count,
                            Analyses = analysisSummaries.OrderByDescending(a => a.AnalysisDate).ToList(),
                            LastAnalysisDate = lastAnalysis?.AnalysisDate,
                            DaysSinceLastAnalysis = daysSinceLastAnalysis
                        };

                        codeBreakdowns.Add(breakdown);
                    }

                    // Calculate overall statistics
                    var totalAnalyses = codeBreakdowns.Sum(c => c.TotalAnalyses);
                    var averageAnalysesPerCode = redeemedCodesList.Count > 0
                        ? (decimal)totalAnalyses / redeemedCodesList.Count
                        : 0;

                    // Get top performing codes
                    var topPerformingCodes = codeBreakdowns
                        .OrderByDescending(c => c.TotalAnalyses)
                        .Take(request.TopCodesCount)
                        .ToList();

                    // Calculate crop type distribution
                    var cropTypeDistribution = allAnalyses
                        .GroupBy(a => a.CropType)
                        .Select(g => new CropTypeStatistic
                        {
                            CropType = g.Key,
                            AnalysisCount = g.Count(),
                            Percentage = totalAnalyses > 0 ? (decimal)g.Count() / totalAnalyses * 100 : 0,
                            UniqueFarmers = codeBreakdowns.Count(c => c.Analyses.Any(a => a.CropType == g.Key))
                        })
                        .OrderByDescending(c => c.AnalysisCount)
                        .ToList();

                    // Calculate disease distribution
                    var diseaseDistribution = allAnalyses
                        .Where(a => !string.IsNullOrEmpty(a.Disease) && a.Disease != "Unknown")
                        .GroupBy(a => new { a.Disease, a.DiseaseCategory })
                        .Select(g => new DiseaseStatistic
                        {
                            Disease = g.Key.Disease,
                            Category = g.Key.DiseaseCategory,
                            OccurrenceCount = g.Count(),
                            Percentage = totalAnalyses > 0 ? (decimal)g.Count() / totalAnalyses * 100 : 0,
                            AffectedCrops = g.Select(a => a.CropType).Distinct().ToList(),
                            GeographicDistribution = g.Select(a => a.Location.Split(',').FirstOrDefault() ?? "Unknown")
                                .Distinct().ToList()
                        })
                        .OrderByDescending(d => d.OccurrenceCount)
                        .Take(20) // Top 20 diseases
                        .ToList();

                    var statistics = new CodeAnalysisStatisticsDto
                    {
                        TotalRedeemedCodes = redeemedCodesList.Count,
                        TotalAnalysesPerformed = totalAnalyses,
                        AverageAnalysesPerCode = averageAnalysesPerCode,
                        TotalActiveFarmers = codeBreakdowns.Count(c => c.SubscriptionStatus == "Active"),
                        CodeBreakdowns = codeBreakdowns.OrderByDescending(c => c.TotalAnalyses).ToList(),
                        TopPerformingCodes = topPerformingCodes,
                        CropTypeDistribution = cropTypeDistribution,
                        DiseaseDistribution = diseaseDistribution
                    };

                    _logger.LogInformation(
                        "Code analysis statistics: {RedeemedCodes} codes, {TotalAnalyses} analyses",
                        redeemedCodesList.Count, totalAnalyses);

                    return new SuccessDataResult<CodeAnalysisStatisticsDto>(
                        statistics,
                        "Code analysis statistics retrieved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting code analysis statistics for sponsor {SponsorId}", request.SponsorId);
                    return new ErrorDataResult<CodeAnalysisStatisticsDto>(
                        "Error retrieving code analysis statistics");
                }
            }
        }
    }
}
