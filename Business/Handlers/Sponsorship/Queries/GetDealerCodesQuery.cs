using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Get codes transferred to current dealer
    /// Optimized for performance with focused filtering
    /// </summary>
    public class GetDealerCodesQuery : IRequest<IDataResult<SponsorshipCodesPaginatedDto>>
    {
        public int DealerId { get; set; } // Authenticated dealer ID
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public bool OnlyUnsent { get; set; } = false; // Not sent to farmers yet

        public class GetDealerCodesQueryHandler : IRequestHandler<GetDealerCodesQuery, IDataResult<SponsorshipCodesPaginatedDto>>
        {
            private readonly ISponsorshipCodeRepository _codeRepository;
            private readonly ISubscriptionTierRepository _tierRepository;
            private readonly ILogger<GetDealerCodesQueryHandler> _logger;

            public GetDealerCodesQueryHandler(
                ISponsorshipCodeRepository codeRepository,
                ISubscriptionTierRepository tierRepository,
                ILogger<GetDealerCodesQueryHandler> logger)
            {
                _codeRepository = codeRepository;
                _tierRepository = tierRepository;
                _logger = logger;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SponsorshipCodesPaginatedDto>> Handle(
                GetDealerCodesQuery request,
                CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("🔍 Fetching codes for dealer {DealerId}, OnlyUnsent: {OnlyUnsent}, Page: {Page}",
                        request.DealerId, request.OnlyUnsent, request.Page);

                    // Base query: codes transferred to this dealer and not reclaimed
                    // Performance: Uses index on DealerId + ReclaimedAt
                    var query = _codeRepository.Query()
                        .Where(c => c.DealerId == request.DealerId && c.ReclaimedAt == null);

                    // Apply unsent filter if requested
                    if (request.OnlyUnsent)
                    {
                        // Unsent = not distributed to farmers yet
                        // Performance: Uses index on DistributionDate
                        query = query.Where(c => c.DistributionDate == null &&
                                                 !c.IsUsed &&
                                                 c.ExpiryDate > DateTime.Now &&
                                                 c.IsActive);
                    }

                    // Get total count (optimized - no data loaded)
                    var totalCount = await query.CountAsync(cancellationToken);

                    _logger.LogInformation("📊 Found {TotalCount} codes for dealer {DealerId}",
                        totalCount, request.DealerId);

                    // Paginate and load only required data
                    // Performance: OrderBy uses index on TransferredAt
                    var codes = await query
                        .OrderByDescending(c => c.TransferredAt)
                        .Skip((request.Page - 1) * request.PageSize)
                        .Take(request.PageSize)
                        .Select(c => new
                        {
                            c.Id,
                            c.Code,
                            c.SubscriptionTierId,
                            c.IsUsed,
                            c.IsActive,
                            c.ExpiryDate,
                            c.CreatedDate,
                            c.TransferredAt,
                            c.DistributionDate,
                            c.UsedDate,
                            c.RecipientPhone,
                            c.RecipientName,
                            c.DistributedTo
                        })
                        .ToListAsync(cancellationToken);

                    // Get tier names (cached lookup)
                    var tierIds = codes.Select(c => c.SubscriptionTierId).Distinct().ToList();
                    var tiers = await _tierRepository.GetListAsync(t => tierIds.Contains(t.Id));
                    var tierMap = tiers.ToDictionary(t => t.Id, t => t.TierName);

                    // Map to DTOs
                    var codeDtos = codes.Select(c => new SponsorshipCodeDto
                    {
                        Id = c.Id,
                        Code = c.Code,
                        SubscriptionTier = tierMap.ContainsKey(c.SubscriptionTierId)
                            ? tierMap[c.SubscriptionTierId]
                            : "Unknown",
                        IsUsed = c.IsUsed,
                        IsActive = c.IsActive,
                        ExpiryDate = c.ExpiryDate,
                        CreatedDate = c.CreatedDate,
                        TransferredAt = c.TransferredAt,
                        DistributionDate = c.DistributionDate,
                        UsedDate = c.UsedDate,
                        RecipientPhone = c.RecipientPhone,
                        RecipientName = c.RecipientName,
                        DistributedTo = c.DistributedTo
                    }).ToList();

                    var result = new SponsorshipCodesPaginatedDto
                    {
                        Codes = codeDtos,
                        TotalCount = totalCount,
                        Page = request.Page,
                        PageSize = request.PageSize,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
                    };

                    _logger.LogInformation("✅ Returning {Count} codes (page {Page} of {TotalPages})",
                        codeDtos.Count, result.Page, result.TotalPages);

                    return new SuccessDataResult<SponsorshipCodesPaginatedDto>(
                        result,
                        $"Dealer codes retrieved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error retrieving dealer codes for {DealerId}", request.DealerId);
                    return new ErrorDataResult<SponsorshipCodesPaginatedDto>(
                        "Dealer kodları alınırken hata oluştu");
                }
            }
        }
    }
}
