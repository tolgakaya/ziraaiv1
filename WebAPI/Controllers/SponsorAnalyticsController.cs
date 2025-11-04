using Business.Services.Analytics;
using Core.Utilities.Results;
using Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Sponsor analytics endpoints for dealer performance tracking
    /// Uses Redis cache for high-performance analytics (5-15ms response time)
    /// </summary>
    [Route("api/v1/sponsorship/analytics")]
    [ApiController]
    [Authorize(Roles = "Sponsor")]
    public class SponsorAnalyticsController : ControllerBase
    {
        private readonly ISponsorDealerAnalyticsCacheService _analyticsService;
        private readonly ILogger<SponsorAnalyticsController> _logger;

        public SponsorAnalyticsController(
            ISponsorDealerAnalyticsCacheService analyticsService,
            ILogger<SponsorAnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        /// <summary>
        /// Get dealer performance analytics for current sponsor
        /// </summary>
        /// <param name="dealerId">Optional dealer ID to filter results</param>
        /// <returns>Analytics data with summary and per-dealer breakdown</returns>
        /// <response code="200">Returns analytics data</response>
        /// <response code="401">Unauthorized - Sponsor role required</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("dealer-performance")]
        [ProducesResponseType(typeof(IDataResult<DealerSummaryDto>), 200)]
        public async Task<IActionResult> GetDealerPerformance([FromQuery] int? dealerId = null)
        {
            try
            {
                // Get current sponsor ID from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int sponsorId))
                {
                    _logger.LogWarning("[ANALYTICS] Invalid user ID in JWT claims");
                    return Unauthorized(new ErrorResult("Invalid authentication token"));
                }

                _logger.LogInformation(
                    "[ANALYTICS] Getting dealer performance - SponsorId: {SponsorId}, DealerId: {DealerId}",
                    sponsorId, dealerId);

                var analytics = await _analyticsService.GetDealerPerformanceAsync(sponsorId, dealerId);

                return Ok(new SuccessDataResult<DealerSummaryDto>(
                    analytics,
                    "Analytics retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ANALYTICS] Failed to get dealer performance");
                return StatusCode(500, new ErrorResult("Failed to retrieve analytics"));
            }
        }

        /// <summary>
        /// Rebuild analytics cache from database for current sponsor
        /// Use this endpoint if cache data seems stale or incorrect
        /// </summary>
        /// <returns>Success message</returns>
        /// <response code="200">Cache rebuilt successfully</response>
        /// <response code="401">Unauthorized - Sponsor role required</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("rebuild-cache")]
        [ProducesResponseType(typeof(IResult), 200)]
        public async Task<IActionResult> RebuildCache()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int sponsorId))
                {
                    _logger.LogWarning("[ANALYTICS] Invalid user ID in JWT claims");
                    return Unauthorized(new ErrorResult("Invalid authentication token"));
                }

                _logger.LogInformation("[ANALYTICS] Rebuilding cache - SponsorId: {SponsorId}", sponsorId);

                await _analyticsService.RebuildCacheAsync(sponsorId);

                return Ok(new SuccessResult("Analytics cache rebuilt successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ANALYTICS] Failed to rebuild cache");
                return StatusCode(500, new ErrorResult("Failed to rebuild analytics cache"));
            }
        }
    }
}
