using System;
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
using Entities.Dtos;
using MediatR;
using Newtonsoft.Json;

namespace Business.Handlers.AdminSponsorship.Queries
{
    /// <summary>
    /// Admin query to view non-sponsored analyses with filtering and pagination
    /// </summary>
    public class GetNonSponsoredAnalysesQuery : IRequest<PaginatedResult<List<NonSponsoredAnalysisDto>>>
    {
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Sorting
        public string SortBy { get; set; } = "date";
        public string SortOrder { get; set; } = "desc";

        // Filtering
        public string FilterByCropType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string FilterByStatus { get; set; }
        public int? UserId { get; set; }

        public class GetNonSponsoredAnalysesQueryHandler : IRequestHandler<GetNonSponsoredAnalysesQuery, PaginatedResult<List<NonSponsoredAnalysisDto>>>
        {
            private readonly IPlantAnalysisRepository _plantAnalysisRepository;
            private readonly IUserRepository _userRepository;

            public GetNonSponsoredAnalysesQueryHandler(
                IPlantAnalysisRepository plantAnalysisRepository,
                IUserRepository userRepository)
            {
                _plantAnalysisRepository = plantAnalysisRepository;
                _userRepository = userRepository;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<PaginatedResult<List<NonSponsoredAnalysisDto>>> Handle(GetNonSponsoredAnalysesQuery request, CancellationToken cancellationToken)
            {
                // Query non-sponsored analyses (SponsorId is null)
                var query = await _plantAnalysisRepository.GetListAsync(p =>
                    p.Status &&
                    string.IsNullOrEmpty(p.SponsorId) &&
                    p.SponsorshipCodeId == null &&
                    p.SponsorUserId == null);

                var analyses = query.ToList();

                // Apply filters
                if (request.UserId.HasValue)
                {
                    analyses = analyses.Where(p => p.UserId == request.UserId.Value).ToList();
                }

                if (!string.IsNullOrEmpty(request.FilterByCropType))
                {
                    analyses = analyses.Where(p =>
                        !string.IsNullOrEmpty(p.CropType) &&
                        p.CropType.Contains(request.FilterByCropType, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (request.StartDate.HasValue)
                {
                    analyses = analyses.Where(p => p.AnalysisDate >= request.StartDate.Value).ToList();
                }

                if (request.EndDate.HasValue)
                {
                    analyses = analyses.Where(p => p.AnalysisDate <= request.EndDate.Value).ToList();
                }

                if (!string.IsNullOrEmpty(request.FilterByStatus))
                {
                    analyses = analyses.Where(p =>
                        !string.IsNullOrEmpty(p.AnalysisStatus) &&
                        p.AnalysisStatus.Equals(request.FilterByStatus, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Apply sorting
                analyses = request.SortBy?.ToLower() switch
                {
                    "date" => request.SortOrder?.ToLower() == "asc"
                        ? analyses.OrderBy(p => p.AnalysisDate).ToList()
                        : analyses.OrderByDescending(p => p.AnalysisDate).ToList(),
                    "croptype" => request.SortOrder?.ToLower() == "asc"
                        ? analyses.OrderBy(p => p.CropType).ToList()
                        : analyses.OrderByDescending(p => p.CropType).ToList(),
                    "status" => request.SortOrder?.ToLower() == "asc"
                        ? analyses.OrderBy(p => p.AnalysisStatus).ToList()
                        : analyses.OrderByDescending(p => p.AnalysisStatus).ToList(),
                    _ => analyses.OrderByDescending(p => p.AnalysisDate).ToList()
                };

                // Get total count before pagination
                var totalCount = analyses.Count;

                // Apply pagination
                var paginatedAnalyses = analyses
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // Get user information for analyses
                var userIds = paginatedAnalyses
                    .Where(p => p.UserId.HasValue)
                    .Select(p => p.UserId.Value)
                    .Distinct()
                    .ToList();

                var users = await _userRepository.GetListAsync(u => userIds.Contains(u.UserId));
                var userDict = users.ToDictionary(u => u.UserId, u => u);

                // Map to DTOs
                var items = paginatedAnalyses.Select(analysis => new NonSponsoredAnalysisDto
                {
                    PlantAnalysisId = analysis.Id,
                    AnalysisDate = analysis.AnalysisDate,
                    AnalysisStatus = analysis.AnalysisStatus,
                    CropType = analysis.CropType,
                    Location = analysis.Location,
                    UserId = analysis.UserId,
                    UserFullName = analysis.UserId.HasValue && userDict.ContainsKey(analysis.UserId.Value)
                        ? userDict[analysis.UserId.Value].FullName
                        : null,
                    UserEmail = analysis.UserId.HasValue && userDict.ContainsKey(analysis.UserId.Value)
                        ? userDict[analysis.UserId.Value].Email
                        : null,
                    UserPhone = analysis.UserId.HasValue && userDict.ContainsKey(analysis.UserId.Value)
                        ? userDict[analysis.UserId.Value].MobilePhones
                        : null,
                    ImageUrl = analysis.ImageUrl,
                    OverallHealthScore = analysis.OverallHealthScore,
                    PrimaryConcern = analysis.PrimaryConcern,
                    IsOnBehalfOf = analysis.IsOnBehalfOf,
                    CreatedByAdminId = analysis.CreatedByAdminId
                }).ToList();

                var result = new PaginatedResult<List<NonSponsoredAnalysisDto>>(items, request.Page, request.PageSize)
                {
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                    TotalRecords = totalCount
                };

                return result;
            }
        }
    }
}
