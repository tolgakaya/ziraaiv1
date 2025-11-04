using Entities.Dtos;
using System.Threading.Tasks;

namespace Business.Services.Analytics
{
    /// <summary>
    /// Redis-based analytics cache service for sponsor-dealer performance tracking
    /// Provides real-time analytics with 5-15ms response time
    /// </summary>
    public interface ISponsorDealerAnalyticsCacheService
    {
        /// <summary>
        /// Update cache when codes are transferred from sponsor to dealer
        /// </summary>
        Task OnCodeTransferredAsync(int sponsorId, int dealerId, int codeCount);

        /// <summary>
        /// Update cache when dealer distributes code to farmer
        /// </summary>
        Task OnCodeDistributedAsync(int sponsorId, int dealerId);

        /// <summary>
        /// Update cache when farmer redeems code
        /// </summary>
        Task OnCodeRedeemedAsync(int sponsorId, int dealerId);

        /// <summary>
        /// Update cache when invitation is sent to dealer
        /// </summary>
        Task OnInvitationSentAsync(int sponsorId, int dealerId);

        /// <summary>
        /// Get cached dealer performance statistics
        /// Returns summary and optionally filtered by dealer ID
        /// </summary>
        Task<DealerSummaryDto> GetDealerPerformanceAsync(int sponsorId, int? dealerId = null);

        /// <summary>
        /// Rebuild cache from database (for cache warming or recovery)
        /// </summary>
        Task RebuildCacheAsync(int sponsorId);
    }
}
