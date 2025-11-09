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
    /// Admin query to view detailed information about a non-sponsored farmer
    /// </summary>
    public class GetNonSponsoredFarmerDetailQuery : IRequest<IDataResult<NonSponsoredFarmerDetailDto>>
    {
        public int UserId { get; set; }

        public class GetNonSponsoredFarmerDetailQueryHandler : IRequestHandler<GetNonSponsoredFarmerDetailQuery, IDataResult<NonSponsoredFarmerDetailDto>>
        {
            private readonly IUserRepository _userRepository;
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;

            public GetNonSponsoredFarmerDetailQueryHandler(
                IUserRepository userRepository,
                IPlantAnalysisRepository plantAnalysisRepository)
            {
                _userRepository = userRepository;
                _plantAnalysisRepository = plantAnalysisRepository;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<NonSponsoredFarmerDetailDto>> Handle(GetNonSponsoredFarmerDetailQuery request, CancellationToken cancellationToken)
            {
                // Get user information
                var user = await _userRepository.GetAsync(u => u.UserId == request.UserId);

                if (user == null)
                {
                    return new ErrorDataResult<NonSponsoredFarmerDetailDto>("User not found");
                }

                // Get all non-sponsored analyses for this user
                var allAnalyses = await _plantAnalysisRepository.GetListAsync(p =>
                    p.UserId == request.UserId &&
                    p.Status &&
                    string.IsNullOrEmpty(p.SponsorId) &&
                    p.SponsorshipCodeId == null &&
                    p.SponsorUserId == null);

                var analysesList = allAnalyses.ToList();

                // Calculate statistics
                var totalAnalyses = analysesList.Count;
                var completedAnalyses = analysesList.Count(a => a.AnalysisStatus == "completed");
                var pendingAnalyses = analysesList.Count(a => a.AnalysisStatus == "pending");
                var failedAnalyses = analysesList.Count(a => a.AnalysisStatus == "failed");

                // Get date range
                var firstAnalysisDate = analysesList.Any() ? analysesList.Min(a => a.AnalysisDate) : (DateTime?)null;
                var lastAnalysisDate = analysesList.Any() ? analysesList.Max(a => a.AnalysisDate) : (DateTime?)null;

                // Get crop types
                var cropTypes = analysesList
                    .Where(a => !string.IsNullOrEmpty(a.CropType))
                    .Select(a => a.CropType)
                    .Distinct()
                    .ToList();

                // Get average health score
                var avgHealthScore = analysesList.Any()
                    ? analysesList.Average(a => a.OverallHealthScore)
                    : 0;

                // Get most common concerns
                var commonConcerns = analysesList
                    .Where(a => !string.IsNullOrEmpty(a.PrimaryConcern))
                    .GroupBy(a => a.PrimaryConcern)
                    .Select(g => new { Concern = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .Select(x => x.Concern)
                    .ToList();

                // Get recent analyses (last 5)
                var recentAnalyses = analysesList
                    .OrderByDescending(a => a.AnalysisDate)
                    .Take(5)
                    .Select(a => new NonSponsoredAnalysisDto
                    {
                        PlantAnalysisId = a.Id,
                        AnalysisDate = a.AnalysisDate,
                        AnalysisStatus = a.AnalysisStatus,
                        CropType = a.CropType,
                        Location = a.Location,
                        UserId = a.UserId,
                        UserFullName = user.FullName,
                        UserEmail = user.Email,
                        UserPhone = user.MobilePhones,
                        ImageUrl = a.ImageUrl,
                        OverallHealthScore = a.OverallHealthScore,
                        PrimaryConcern = a.PrimaryConcern,
                        IsOnBehalfOf = a.IsOnBehalfOf,
                        CreatedByAdminId = a.CreatedByAdminId
                    })
                    .ToList();

                var detail = new NonSponsoredFarmerDetailDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    MobilePhone = user.MobilePhones,
                    Status = user.Status,
                    RecordDate = user.RecordDate,

                    // Analysis statistics
                    TotalAnalyses = totalAnalyses,
                    CompletedAnalyses = completedAnalyses,
                    PendingAnalyses = pendingAnalyses,
                    FailedAnalyses = failedAnalyses,
                    FirstAnalysisDate = firstAnalysisDate,
                    LastAnalysisDate = lastAnalysisDate,
                    AverageHealthScore = (int)Math.Round(avgHealthScore),

                    // Agricultural profile
                    CropTypes = cropTypes,
                    CommonConcerns = commonConcerns,

                    // Recent activity
                    RecentAnalyses = recentAnalyses
                };

                return new SuccessDataResult<NonSponsoredFarmerDetailDto>(
                    detail,
                    $"Retrieved detail for non-sponsored farmer {user.FullName} ({totalAnalyses} analyses)"
                );
            }
        }
    }
}
