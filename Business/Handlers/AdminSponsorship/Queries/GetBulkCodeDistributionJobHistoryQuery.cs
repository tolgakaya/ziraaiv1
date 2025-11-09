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
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers.AdminSponsorship.Queries
{
    /// <summary>
    /// Admin query to get bulk code distribution job history with pagination and filtering
    /// </summary>
    public class GetBulkCodeDistributionJobHistoryQuery : IRequest<IDataResult<BulkCodeDistributionJobHistoryResponseDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string Status { get; set; }
        public int? SponsorId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetBulkCodeDistributionJobHistoryQueryHandler : IRequestHandler<GetBulkCodeDistributionJobHistoryQuery, IDataResult<BulkCodeDistributionJobHistoryResponseDto>>
        {
            private readonly IBulkCodeDistributionJobRepository _jobRepository;
            private readonly IUserRepository _userRepository;

            public GetBulkCodeDistributionJobHistoryQueryHandler(
                IBulkCodeDistributionJobRepository jobRepository,
                IUserRepository userRepository)
            {
                _jobRepository = jobRepository;
                _userRepository = userRepository;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<BulkCodeDistributionJobHistoryResponseDto>> Handle(GetBulkCodeDistributionJobHistoryQuery request, CancellationToken cancellationToken)
            {
                var query = _jobRepository.Query().AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(j => j.Status == request.Status);
                }

                if (request.SponsorId.HasValue)
                {
                    query = query.Where(j => j.SponsorId == request.SponsorId.Value);
                }

                if (request.StartDate.HasValue)
                {
                    query = query.Where(j => j.CreatedDate >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(j => j.CreatedDate <= request.EndDate.Value);
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync(cancellationToken);
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                // Get paginated jobs
                var jobs = await query
                    .OrderByDescending(j => j.CreatedDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                // Get sponsor information
                var sponsorIds = jobs.Select(j => j.SponsorId).Distinct().ToList();
                var sponsors = await _userRepository.Query()
                    .Where(u => sponsorIds.Contains(u.UserId))
                    .ToListAsync(cancellationToken);

                var sponsorDict = sponsors.ToDictionary(s => s.UserId, s => s);

                // Map to DTOs
                var jobDtos = jobs.Select(j => new BulkCodeDistributionJobHistoryDto
                {
                    JobId = j.Id,
                    SponsorId = j.SponsorId,
                    SponsorName = sponsorDict.ContainsKey(j.SponsorId) ? sponsorDict[j.SponsorId].FullName : "Unknown",
                    SponsorEmail = sponsorDict.ContainsKey(j.SponsorId) ? sponsorDict[j.SponsorId].Email : "Unknown",
                    PurchaseId = j.PurchaseId,
                    DeliveryMethod = j.DeliveryMethod,
                    TotalFarmers = j.TotalFarmers,
                    ProcessedFarmers = j.ProcessedFarmers,
                    SuccessfulDistributions = j.SuccessfulDistributions,
                    FailedDistributions = j.FailedDistributions,
                    Status = j.Status,
                    CreatedDate = j.CreatedDate,
                    StartedDate = j.StartedDate,
                    CompletedDate = j.CompletedDate,
                    OriginalFileName = j.OriginalFileName,
                    FileSize = j.FileSize,
                    ResultFileUrl = j.ResultFileUrl,
                    TotalCodesDistributed = j.TotalCodesDistributed,
                    TotalSmsSent = j.TotalSmsSent
                }).ToList();

                var response = new BulkCodeDistributionJobHistoryResponseDto
                {
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    Jobs = jobDtos
                };

                return new SuccessDataResult<BulkCodeDistributionJobHistoryResponseDto>(
                    response,
                    $"Retrieved {jobDtos.Count} jobs (Page {request.Page}/{totalPages}, Total: {totalCount})"
                );
            }
        }
    }
}
