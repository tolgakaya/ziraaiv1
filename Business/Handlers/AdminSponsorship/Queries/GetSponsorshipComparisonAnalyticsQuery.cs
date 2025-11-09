using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;

namespace Business.Handlers.AdminSponsorship.Queries
{
    /// <summary>
    /// Admin query to compare sponsored vs non-sponsored analysis metrics
    /// </summary>
    public class GetSponsorshipComparisonAnalyticsQuery : IRequest<IDataResult<SponsorshipComparisonAnalyticsDto>>
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetSponsorshipComparisonAnalyticsQueryHandler : IRequestHandler<GetSponsorshipComparisonAnalyticsQuery, IDataResult<SponsorshipComparisonAnalyticsDto>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;
            private readonly IUserRepository _userRepository;

            public GetSponsorshipComparisonAnalyticsQueryHandler(
                IPlantAnalysisRepository plantAnalysisRepository,
                IUserRepository userRepository)
            {
                _plantAnalysisRepository = plantAnalysisRepository;
                _userRepository = userRepository;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsorshipComparisonAnalyticsDto>> Handle(GetSponsorshipComparisonAnalyticsQuery request, CancellationToken cancellationToken)
            {
                // Get all active analyses
                var allAnalyses = await _plantAnalysisRepository.GetListAsync(p => p.Status);
                var analysesList = allAnalyses.ToList();

                // Apply date filters if provided
                if (request.StartDate.HasValue)
                {
                    analysesList = analysesList.Where(a => a.AnalysisDate >= request.StartDate.Value).ToList();
                }

                if (request.EndDate.HasValue)
                {
                    analysesList = analysesList.Where(a => a.AnalysisDate <= request.EndDate.Value).ToList();
                }

                // Separate sponsored and non-sponsored analyses
                var sponsoredAnalyses = analysesList.Where(a =>
                    !string.IsNullOrEmpty(a.SponsorId) ||
                    a.SponsorshipCodeId != null ||
                    a.SponsorUserId != null).ToList();

                var nonSponsoredAnalyses = analysesList.Where(a =>
                    string.IsNullOrEmpty(a.SponsorId) &&
                    a.SponsorshipCodeId == null &&
                    a.SponsorUserId == null).ToList();

                // Calculate statistics for sponsored analyses
                var sponsoredStats = new AnalyticsSegmentDto
                {
                    TotalAnalyses = sponsoredAnalyses.Count,
                    CompletedAnalyses = sponsoredAnalyses.Count(a => a.AnalysisStatus == "completed"),
                    PendingAnalyses = sponsoredAnalyses.Count(a => a.AnalysisStatus == "pending"),
                    FailedAnalyses = sponsoredAnalyses.Count(a => a.AnalysisStatus == "failed"),
                    AverageHealthScore = sponsoredAnalyses.Any()
                        ? (int)Math.Round(sponsoredAnalyses.Average(a => a.OverallHealthScore))
                        : 0,
                    UniqueUsers = sponsoredAnalyses
                        .Where(a => a.UserId.HasValue)
                        .Select(a => a.UserId.Value)
                        .Distinct()
                        .Count(),
                    TopCropTypes = sponsoredAnalyses
                        .Where(a => !string.IsNullOrEmpty(a.CropType))
                        .GroupBy(a => a.CropType)
                        .Select(g => new { CropType = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(5)
                        .ToDictionary(x => x.CropType, x => x.Count)
                };

                // Calculate statistics for non-sponsored analyses
                var nonSponsoredStats = new AnalyticsSegmentDto
                {
                    TotalAnalyses = nonSponsoredAnalyses.Count,
                    CompletedAnalyses = nonSponsoredAnalyses.Count(a => a.AnalysisStatus == "completed"),
                    PendingAnalyses = nonSponsoredAnalyses.Count(a => a.AnalysisStatus == "pending"),
                    FailedAnalyses = nonSponsoredAnalyses.Count(a => a.AnalysisStatus == "failed"),
                    AverageHealthScore = nonSponsoredAnalyses.Any()
                        ? (int)Math.Round(nonSponsoredAnalyses.Average(a => a.OverallHealthScore))
                        : 0,
                    UniqueUsers = nonSponsoredAnalyses
                        .Where(a => a.UserId.HasValue)
                        .Select(a => a.UserId.Value)
                        .Distinct()
                        .Count(),
                    TopCropTypes = nonSponsoredAnalyses
                        .Where(a => !string.IsNullOrEmpty(a.CropType))
                        .GroupBy(a => a.CropType)
                        .Select(g => new { CropType = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(5)
                        .ToDictionary(x => x.CropType, x => x.Count)
                };

                // Calculate comparison metrics
                var totalAnalyses = analysesList.Count;
                var sponsorshipRate = totalAnalyses > 0
                    ? Math.Round((double)sponsoredAnalyses.Count / totalAnalyses * 100, 2)
                    : 0;

                var avgHealthDifference = sponsoredStats.AverageHealthScore - nonSponsoredStats.AverageHealthScore;

                var completionRateSponsored = sponsoredAnalyses.Count > 0
                    ? Math.Round((double)sponsoredStats.CompletedAnalyses / sponsoredAnalyses.Count * 100, 2)
                    : 0;

                var completionRateNonSponsored = nonSponsoredAnalyses.Count > 0
                    ? Math.Round((double)nonSponsoredStats.CompletedAnalyses / nonSponsoredAnalyses.Count * 100, 2)
                    : 0;

                var result = new SponsorshipComparisonAnalyticsDto
                {
                    DateRange = new DateRangeDto
                    {
                        StartDate = request.StartDate,
                        EndDate = request.EndDate
                    },
                    TotalAnalyses = totalAnalyses,
                    SponsorshipRate = sponsorshipRate,
                    SponsoredAnalytics = sponsoredStats,
                    NonSponsoredAnalytics = nonSponsoredStats,
                    ComparisonMetrics = new ComparisonMetricsDto
                    {
                        AverageHealthScoreDifference = avgHealthDifference,
                        CompletionRateSponsored = completionRateSponsored,
                        CompletionRateNonSponsored = completionRateNonSponsored,
                        CompletionRateDifference = completionRateSponsored - completionRateNonSponsored,
                        UserEngagementRatio = nonSponsoredStats.UniqueUsers > 0
                            ? Math.Round((double)sponsoredStats.UniqueUsers / nonSponsoredStats.UniqueUsers, 2)
                            : 0
                    }
                };

                return new SuccessDataResult<SponsorshipComparisonAnalyticsDto>(
                    result,
                    $"Comparison analytics: {sponsorshipRate}% sponsorship rate ({sponsoredAnalyses.Count}/{totalAnalyses})"
                );
            }
        }
    }
}
