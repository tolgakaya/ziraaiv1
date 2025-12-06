using Entities.Dtos;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    /// <summary>
    /// Cache service interface for dealer dashboard data
    /// Implements cache-first pattern with configurable TTL
    /// </summary>
    public interface IDealerDashboardCacheService
    {
        /// <summary>
        /// Gets dealer dashboard summary with cache-first pattern
        /// </summary>
        /// <param name="dealerId">Dealer ID</param>
        /// <returns>Cached or fresh dealer dashboard summary</returns>
        Task<DealerDashboardSummaryDto> GetDashboardSummaryAsync(int dealerId);

        /// <summary>
        /// Invalidates all cached data for specified dealer
        /// </summary>
        /// <param name="dealerId">Dealer ID</param>
        Task InvalidateDashboardAsync(int dealerId);

        /// <summary>
        /// Pre-populates cache for specified dealer (warm cache)
        /// </summary>
        /// <param name="dealerId">Dealer ID</param>
        Task WarmCacheAsync(int dealerId);
    }
}
