using Core.CrossCuttingConcerns.Caching;
using DataAccess.Abstract;
using Entities.Dtos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business.Services.Analytics
{
    public class SponsorDealerAnalyticsCacheService : ISponsorDealerAnalyticsCacheService
    {
        private readonly ICacheManager _cache;
        private readonly ISponsorshipCodeRepository _codeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPlantAnalysisRepository _plantAnalysisRepository;
        private readonly ILogger<SponsorDealerAnalyticsCacheService> _logger;

        private const string CACHE_KEY_PREFIX = "sponsor_dealer_analytics";
        private const int CACHE_DURATION_MINUTES = 1440;

        public SponsorDealerAnalyticsCacheService(
            ICacheManager cache,
            ISponsorshipCodeRepository codeRepository,
            IUserRepository userRepository,
            IPlantAnalysisRepository plantAnalysisRepository,
            ILogger<SponsorDealerAnalyticsCacheService> logger)
        {
            _cache = cache;
            _codeRepository = codeRepository;
            _userRepository = userRepository;
            _plantAnalysisRepository = plantAnalysisRepository;
            _logger = logger;
        }

        public async Task OnCodeTransferredAsync(int sponsorId, int dealerId, int codeCount)
        {
            try
            {
                var cacheKey = GetCacheKey(sponsorId);
                var summary = await GetOrCreateSummaryAsync(sponsorId);
                var dealerStats = summary.Dealers.FirstOrDefault(d => d.DealerId == dealerId);
                if (dealerStats == null)
                {
                    var dealer = await _userRepository.GetAsync(u => u.UserId == dealerId);
                    dealerStats = new DealerPerformanceDto
                    {
                        DealerId = dealerId,
                        DealerName = dealer?.FullName ?? "Unknown",
                        DealerEmail = dealer?.Email ?? "",
                        TotalCodesReceived = 0,
                        CodesSent = 0,
                        CodesUsed = 0,
                        CodesAvailable = 0,
                        UsageRate = 0
                    };
                    summary.Dealers.Add(dealerStats);
                }
                dealerStats.TotalCodesReceived += codeCount;
                dealerStats.CodesAvailable += codeCount;
                summary.TotalCodesDistributed += codeCount;
                summary.TotalCodesAvailable += codeCount;
                _cache.Add(cacheKey, JsonSerializer.Serialize(summary), CACHE_DURATION_MINUTES);
                _logger.LogInformation("[ANALYTICS_CACHE] Transfer - Sponsor:{0}, Dealer:{1}, Count:{2}", sponsorId, dealerId, codeCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ANALYTICS_CACHE] Failed on code transfer");
            }
        }

        public async Task OnCodeDistributedAsync(int sponsorId, int dealerId)
        {
            try
            {
                var cacheKey = GetCacheKey(sponsorId);
                var summary = await GetOrCreateSummaryAsync(sponsorId);
                var dealerStats = summary.Dealers.FirstOrDefault(d => d.DealerId == dealerId);
                if (dealerStats != null)
                {
                    dealerStats.CodesSent++;
                    dealerStats.CodesAvailable--;
                    dealerStats.UsageRate = dealerStats.CodesSent > 0 ? Math.Round((decimal)dealerStats.CodesUsed / dealerStats.CodesSent * 100, 2) : 0;
                    summary.TotalCodesAvailable--;
                    _cache.Add(cacheKey, JsonSerializer.Serialize(summary), CACHE_DURATION_MINUTES);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ANALYTICS_CACHE] Failed on code distributed");
            }
        }

        public async Task OnCodeRedeemedAsync(int sponsorId, int dealerId)
        {
            try
            {
                var cacheKey = GetCacheKey(sponsorId);
                var summary = await GetOrCreateSummaryAsync(sponsorId);
                var dealerStats = summary.Dealers.FirstOrDefault(d => d.DealerId == dealerId);
                if (dealerStats != null)
                {
                    dealerStats.CodesUsed++;
                    dealerStats.UsageRate = dealerStats.CodesSent > 0 ? Math.Round((decimal)dealerStats.CodesUsed / dealerStats.CodesSent * 100, 2) : 0;
                    summary.TotalCodesUsed++;
                    var totalSent = summary.Dealers.Sum(d => d.CodesSent);
                    summary.OverallUsageRate = totalSent > 0 ? Math.Round((decimal)summary.TotalCodesUsed / totalSent * 100, 2) : 0;
                    
                    // Note: uniqueFarmersReached and totalAnalyses are NOT updated incrementally
                    // These metrics require rebuild to ensure accuracy due to async analysis processing
                    _logger.LogInformation("[ANALYTICS_CACHE] Code redeemed - Note: Rebuild required for accurate farmer/analysis counts");
                    
                    _cache.Add(cacheKey, JsonSerializer.Serialize(summary), CACHE_DURATION_MINUTES);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ANALYTICS_CACHE] Failed on code redeemed");
            }
        }

        public async Task OnInvitationSentAsync(int sponsorId, int dealerId)
        {
            try
            {
                var cacheKey = GetCacheKey(sponsorId);
                var summary = await GetOrCreateSummaryAsync(sponsorId);
                summary.TotalDealers = summary.Dealers.Select(d => d.DealerId).Distinct().Count();
                _cache.Add(cacheKey, JsonSerializer.Serialize(summary), CACHE_DURATION_MINUTES);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ANALYTICS_CACHE] Failed on invitation");
            }
        }

        public async Task<DealerSummaryDto> GetDealerPerformanceAsync(int sponsorId, int? dealerId = null)
        {
            try
            {
                var cacheKey = GetCacheKey(sponsorId);
                var cachedData = _cache.Get<string>(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var summary = JsonSerializer.Deserialize<DealerSummaryDto>(cachedData);
                    if (dealerId.HasValue && summary != null)
                    {
                        summary.Dealers = summary.Dealers.Where(d => d.DealerId == dealerId.Value).ToList();
                    }
                    return summary;
                }
                await RebuildCacheAsync(sponsorId);
                cachedData = _cache.Get<string>(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var summary = JsonSerializer.Deserialize<DealerSummaryDto>(cachedData);
                    if (dealerId.HasValue && summary != null)
                    {
                        summary.Dealers = summary.Dealers.Where(d => d.DealerId == dealerId.Value).ToList();
                    }
                    return summary;
                }
                return new DealerSummaryDto
                {
                    TotalDealers = 0,
                    TotalCodesDistributed = 0,
                    TotalCodesUsed = 0,
                    TotalCodesAvailable = 0,
                    OverallUsageRate = 0,
                    Dealers = new List<DealerPerformanceDto>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ANALYTICS_CACHE] Failed to get performance");
                throw;
            }
        }

        public async Task RebuildCacheAsync(int sponsorId)
        {
            try
            {
                var codes = await _codeRepository.GetListAsync(c => c.SponsorId == sponsorId);
                var dealerGroups = codes.GroupBy(c => c.DealerId ?? 0).Where(g => g.Key > 0).ToList();
                var dealerStats = new List<DealerPerformanceDto>();

                foreach (var dealerGroup in dealerGroups)
                {
                    var dealerId = dealerGroup.Key;
                    var dealerCodes = dealerGroup.ToList();
                    var dealer = await _userRepository.GetAsync(u => u.UserId == dealerId);
                    var totalReceived = dealerCodes.Count;
                    var codesSent = dealerCodes.Count(c => c.DistributionDate.HasValue);
                    var codesUsed = dealerCodes.Count(c => c.IsUsed);
                    var codesAvailable = totalReceived - codesSent;

                    // Get code IDs for this dealer
                    var dealerCodeIds = dealerCodes.Select(c => c.Id).ToList();

                    // Calculate unique farmers reached and total analyses using PlantAnalysis table
                    // This counts ALL analyses created with these codes, regardless of sponsor viewing
                    var analyses = await _plantAnalysisRepository.GetListAsync(
                        a => dealerCodeIds.Contains(a.SponsorshipCodeId ?? 0));

                    var uniqueFarmers = analyses
                        .Select(a => a.UserId ?? 0)
                        .Where(userId => userId > 0)
                        .Distinct()
                        .Count();

                    var totalAnalyses = analyses.Count();

                    // Get first and last transfer dates
                    var transferDates = dealerCodes
                        .Where(c => c.TransferredAt.HasValue)
                        .Select(c => c.TransferredAt.Value)
                        .OrderBy(d => d)
                        .ToList();

                    var firstTransferDate = transferDates.FirstOrDefault();
                    var lastTransferDate = transferDates.LastOrDefault();

                    dealerStats.Add(new DealerPerformanceDto
                    {
                        DealerId = dealerId,
                        DealerName = dealer?.FullName ?? "Unknown",
                        DealerEmail = dealer?.Email ?? "",
                        TotalCodesReceived = totalReceived,
                        CodesSent = codesSent,
                        CodesUsed = codesUsed,
                        CodesAvailable = Math.Max(0, codesAvailable),
                        UsageRate = codesSent > 0 ? Math.Round((decimal)codesUsed / codesSent * 100, 2) : 0,
                        UniqueFarmersReached = uniqueFarmers,
                        TotalAnalyses = totalAnalyses,
                        FirstTransferDate = firstTransferDate != default ? firstTransferDate : (DateTime?)null,
                        LastTransferDate = lastTransferDate != default ? lastTransferDate : (DateTime?)null
                    });
                }

                var totalSent = dealerStats.Sum(d => d.CodesSent);
                var summary = new DealerSummaryDto
                {
                    TotalDealers = dealerStats.Count,
                    TotalCodesDistributed = dealerStats.Sum(d => d.TotalCodesReceived),
                    TotalCodesUsed = dealerStats.Sum(d => d.CodesUsed),
                    TotalCodesAvailable = dealerStats.Sum(d => d.CodesAvailable),
                    OverallUsageRate = totalSent > 0 ? Math.Round((decimal)dealerStats.Sum(d => d.CodesUsed) / totalSent * 100, 2) : 0,
                    Dealers = dealerStats
                };

                var cacheKey = GetCacheKey(sponsorId);
                _cache.Add(cacheKey, JsonSerializer.Serialize(summary), CACHE_DURATION_MINUTES);
                _logger.LogInformation("[ANALYTICS_CACHE] Rebuilt - Sponsor:{0}, Dealers:{1}", sponsorId, dealerStats.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ANALYTICS_CACHE] Failed to rebuild");
                throw;
            }
        }

        private async Task<DealerSummaryDto> GetOrCreateSummaryAsync(int sponsorId)
        {
            var cacheKey = GetCacheKey(sponsorId);
            var cachedData = _cache.Get<string>(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<DealerSummaryDto>(cachedData);
            }
            await RebuildCacheAsync(sponsorId);
            cachedData = _cache.Get<string>(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<DealerSummaryDto>(cachedData);
            }
            return new DealerSummaryDto
            {
                TotalDealers = 0,
                TotalCodesDistributed = 0,
                TotalCodesUsed = 0,
                TotalCodesAvailable = 0,
                    OverallUsageRate = 0,
                Dealers = new List<DealerPerformanceDto>()
            };
        }

        private string GetCacheKey(int sponsorId)
        {
            return CACHE_KEY_PREFIX + ":" + sponsorId;
        }
    }
}
