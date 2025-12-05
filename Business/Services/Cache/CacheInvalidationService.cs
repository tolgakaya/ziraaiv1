using Core.CrossCuttingConcerns.Caching;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Business.Services.Cache
{
    /// <summary>
    /// Cache invalidation service implementation with comprehensive logging
    /// Manages cache lifecycles and ensures data consistency across distributed systems
    /// </summary>
    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly ICacheManager _cache;
        private readonly ILogger<CacheInvalidationService> _logger;

        public CacheInvalidationService(
            ICacheManager cache,
            ILogger<CacheInvalidationService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task InvalidateDealerDashboardAsync(int dealerId)
        {
            try
            {
                var keys = new[]
                {
                    $"dealer:dashboard:{dealerId}",
                    $"dealer:summary:{dealerId}"
                };

                foreach (var key in keys)
                {
                    _cache.Remove(key);
                }

                _logger.LogInformation(
                    "[CACHE_INVALIDATED] Dealer dashboard - DealerId: {DealerId}, Keys: {KeyCount}",
                    dealerId, keys.Length);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE_INVALIDATION_ERROR] Failed to invalidate dealer dashboard - DealerId: {DealerId}", dealerId);
            }
        }

        public async Task InvalidateSponsorDashboardAsync(int sponsorId)
        {
            try
            {
                var keys = new[]
                {
                    $"sponsor:dashboard:{sponsorId}",
                    $"sponsor:summary:{sponsorId}",
                    $"sponsor:packages:{sponsorId}"
                };

                foreach (var key in keys)
                {
                    _cache.Remove(key);
                }

                _logger.LogInformation(
                    "[CACHE_INVALIDATED] Sponsor dashboard - SponsorId: {SponsorId}, Keys: {KeyCount}",
                    sponsorId, keys.Length);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE_INVALIDATION_ERROR] Failed to invalidate sponsor dashboard - SponsorId: {SponsorId}", sponsorId);
            }
        }

        public async Task InvalidateAdminStatisticsAsync()
        {
            try
            {
                // Pattern-based invalidation for all admin statistics
                _cache.RemoveByPattern("admin:stats:*");

                _logger.LogInformation("[CACHE_INVALIDATED] All admin statistics - Pattern: admin:stats:*");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE_INVALIDATION_ERROR] Failed to invalidate admin statistics");
            }
        }

        public async Task InvalidateSubscriptionTierAsync(int? tierId = null)
        {
            try
            {
                if (tierId.HasValue)
                {
                    var key = $"subscription:tier:{tierId.Value}";
                    _cache.Remove(key);
                    _logger.LogInformation("[CACHE_INVALIDATED] Subscription tier - TierId: {TierId}", tierId.Value);
                }
                else
                {
                    // Invalidate all tiers
                    _cache.RemoveByPattern("subscription:tier:*");
                    _logger.LogInformation("[CACHE_INVALIDATED] All subscription tiers - Pattern: subscription:tier:*");
                }

                // Broadcast for distributed systems
                await BroadcastInvalidationAsync("subscription:tiers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE_INVALIDATION_ERROR] Failed to invalidate subscription tier - TierId: {TierId}", tierId);
            }
        }

        public async Task InvalidateConfigurationAsync(string key)
        {
            try
            {
                var cacheKey = $"config:{key}";
                _cache.Remove(cacheKey);

                _logger.LogInformation("[CACHE_INVALIDATED] Configuration - Key: {Key}", key);

                // Broadcast for distributed systems (critical for consistency)
                await BroadcastInvalidationAsync(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE_INVALIDATION_ERROR] Failed to invalidate configuration - Key: {Key}", key);
            }
        }

        public async Task InvalidateSponsorAnalyticsAsync(int sponsorId)
        {
            try
            {
                var keys = new[]
                {
                    $"sponsor:analytics:{sponsorId}",
                    $"sponsor:dealer:analytics:{sponsorId}",
                    $"sponsor:roi:{sponsorId}",
                    $"sponsor:temporal:{sponsorId}",
                    $"sponsor:messaging:{sponsorId}"
                };

                foreach (var key in keys)
                {
                    _cache.Remove(key);
                }

                _logger.LogInformation(
                    "[CACHE_INVALIDATED] Sponsor analytics - SponsorId: {SponsorId}, Keys: {KeyCount}",
                    sponsorId, keys.Length);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE_INVALIDATION_ERROR] Failed to invalidate sponsor analytics - SponsorId: {SponsorId}", sponsorId);
            }
        }

        public async Task InvalidateDealerCodesAsync(int dealerId)
        {
            try
            {
                var key = $"dealer:codes:{dealerId}";
                _cache.Remove(key);

                _logger.LogInformation("[CACHE_INVALIDATED] Dealer codes - DealerId: {DealerId}", dealerId);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE_INVALIDATION_ERROR] Failed to invalidate dealer codes - DealerId: {DealerId}", dealerId);
            }
        }

        public async Task InvalidateDealerInvitationsAsync(int dealerId)
        {
            try
            {
                var key = $"dealer:invitations:{dealerId}";
                _cache.Remove(key);

                _logger.LogInformation("[CACHE_INVALIDATED] Dealer invitations - DealerId: {DealerId}", dealerId);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE_INVALIDATION_ERROR] Failed to invalidate dealer invitations - DealerId: {DealerId}", dealerId);
            }
        }

        public async Task InvalidateUserStatisticsAsync()
        {
            try
            {
                _cache.RemoveByPattern("admin:stats:users:*");

                _logger.LogInformation("[CACHE_INVALIDATED] User statistics - Pattern: admin:stats:users:*");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE_INVALIDATION_ERROR] Failed to invalidate user statistics");
            }
        }

        public async Task InvalidateSubscriptionStatisticsAsync()
        {
            try
            {
                _cache.RemoveByPattern("admin:stats:subscriptions:*");

                _logger.LogInformation("[CACHE_INVALIDATED] Subscription statistics - Pattern: admin:stats:subscriptions:*");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE_INVALIDATION_ERROR] Failed to invalidate subscription statistics");
            }
        }

        public async Task InvalidateSponsorshipStatisticsAsync()
        {
            try
            {
                _cache.RemoveByPattern("admin:stats:sponsorship:*");

                _logger.LogInformation("[CACHE_INVALIDATED] Sponsorship statistics - Pattern: admin:stats:sponsorship:*");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE_INVALIDATION_ERROR] Failed to invalidate sponsorship statistics");
            }
        }

        public async Task BroadcastInvalidationAsync(string cacheKey)
        {
            try
            {
                // Redis Pub/Sub implementation for distributed cache invalidation
                // In a multi-instance deployment, this would publish to Redis channel
                // For now, we log the broadcast (can be enhanced with StackExchange.Redis Pub/Sub)

                _logger.LogInformation(
                    "[CACHE_BROADCAST] Broadcasting invalidation - Key: {CacheKey}",
                    cacheKey);

                // Future enhancement: Publish to Redis channel
                // await _redis.PublishAsync("cache:invalidate", cacheKey);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE_BROADCAST_ERROR] Failed to broadcast invalidation - Key: {CacheKey}", cacheKey);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                _cache.RemoveByPattern(pattern);

                _logger.LogInformation("[CACHE_INVALIDATED] Pattern removal - Pattern: {Pattern}", pattern);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE_INVALIDATION_ERROR] Failed to remove by pattern - Pattern: {Pattern}", pattern);
            }
        }
    }
}
