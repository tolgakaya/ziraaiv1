using Business.Services.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Roles = "Sponsor,Admin")]
    public class AnalyticsController : BaseApiController
    {
        private readonly ISponsorshipAnalyticsService _analyticsService;

        public AnalyticsController(ISponsorshipAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Get comprehensive sponsorship dashboard with all key metrics
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDashboard()
        {
            var sponsorId = GetCurrentUserId();
            if (!sponsorId.HasValue)
            {
                return Forbid("Sponsor ID bulunamadı");
            }

            var result = await _analyticsService.GetSponsorDashboardAsync(sponsorId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get detailed link performance metrics with filtering options
        /// </summary>
        [HttpGet("link-performance")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLinkPerformance([FromQuery] string timeRange = "30d")
        {
            var sponsorId = GetCurrentUserId();
            if (!sponsorId.HasValue)
            {
                return Forbid("Sponsor ID bulunamadı");
            }

            var result = await _analyticsService.GetLinkPerformanceAsync(sponsorId.Value, timeRange);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get redemption analytics with success rates and failure analysis
        /// </summary>
        [HttpGet("redemption-analytics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRedemptionAnalytics([FromQuery] string timeRange = "30d")
        {
            var sponsorId = GetCurrentUserId();
            if (!sponsorId.HasValue)
            {
                return Forbid("Sponsor ID bulunamadı");
            }

            var result = await _analyticsService.GetRedemptionAnalyticsAsync(sponsorId.Value, timeRange);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get geographic distribution of clicks and redemptions
        /// </summary>
        [HttpGet("geographic-distribution")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGeographicDistribution()
        {
            var sponsorId = GetCurrentUserId();
            if (!sponsorId.HasValue)
            {
                return Forbid("Sponsor ID bulunamadı");
            }

            var result = await _analyticsService.GetGeographicAnalyticsAsync(sponsorId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get message performance analytics including template effectiveness
        /// </summary>
        [HttpGet("message-performance")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMessagePerformance([FromQuery] string timeRange = "30d")
        {
            var sponsorId = GetCurrentUserId();
            if (!sponsorId.HasValue)
            {
                return Forbid("Sponsor ID bulunamadı");
            }

            var result = await _analyticsService.GetMessagePerformanceAsync(sponsorId.Value, timeRange);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get conversion funnel analysis with optimization suggestions
        /// </summary>
        [HttpGet("conversion-funnel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetConversionFunnel([FromQuery] string timeRange = "30d")
        {
            var sponsorId = GetCurrentUserId();
            if (!sponsorId.HasValue)
            {
                return Forbid("Sponsor ID bulunamadı");
            }

            var result = await _analyticsService.GetConversionFunnelAsync(sponsorId.Value, timeRange);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get competitive analytics and industry benchmarking
        /// </summary>
        [HttpGet("competitive-analysis")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCompetitiveAnalysis()
        {
            var sponsorId = GetCurrentUserId();
            if (!sponsorId.HasValue)
            {
                return Forbid("Sponsor ID bulunamadı");
            }

            var result = await _analyticsService.GetCompetitiveAnalyticsAsync(sponsorId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get analytics summary for mobile dashboard (lightweight version)
        /// </summary>
        [HttpGet("mobile-summary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMobileSummary()
        {
            var sponsorId = GetCurrentUserId();
            if (!sponsorId.HasValue)
            {
                return Forbid("Sponsor ID bulunamadı");
            }

            // Get lightweight dashboard data optimized for mobile
            var dashboard = await _analyticsService.GetSponsorDashboardAsync(sponsorId.Value);
            
            if (!dashboard.Success)
                return BadRequest(dashboard);

            // Return only essential metrics for mobile
            var mobileSummary = new
            {
                overview = new
                {
                    dashboard.Data.Overview.RedemptionRate,
                    dashboard.Data.Overview.TotalCodesRedeemed,
                    dashboard.Data.Overview.TotalSpent,
                    dashboard.Data.Overview.CurrentTier
                },
                quickStats = new
                {
                    dashboard.Data.QuickStats.ActiveCodes,
                    dashboard.Data.QuickStats.TodaysRedemptions,
                    dashboard.Data.QuickStats.ConversionRate,
                    dashboard.Data.QuickStats.UniqueFarmersReached
                },
                topPlatform = dashboard.Data.PlatformBreakdown?.FirstOrDefault()?.Platform ?? "Unknown",
                recentActivity = dashboard.Data.RecentActivity?.Activities?.Take(3)?.ToList() ?? new List<ActivityItem>(),
                chartData = dashboard.Data.RedemptionTrendChart?.TakeLast(7)?.ToList() ?? new List<ChartData>()
            };

            return Ok(new { success = true, data = mobileSummary });
        }

        /// <summary>
        /// Export analytics data as CSV (Admin only)
        /// </summary>
        [HttpGet("export")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ExportAnalytics(
            [FromQuery] int sponsorId, 
            [FromQuery] string format = "csv",
            [FromQuery] string timeRange = "30d")
        {
            try
            {
                // Get comprehensive analytics data
                var dashboard = await _analyticsService.GetSponsorDashboardAsync(sponsorId);
                var linkPerformance = await _analyticsService.GetLinkPerformanceAsync(sponsorId, timeRange);
                var redemptionAnalytics = await _analyticsService.GetRedemptionAnalyticsAsync(sponsorId, timeRange);

                if (!dashboard.Success || !linkPerformance.Success || !redemptionAnalytics.Success)
                {
                    return BadRequest("Analytics verileri alınamadı");
                }

                // Generate CSV content
                var csvContent = GenerateAnalyticsCsv(dashboard.Data, linkPerformance.Data, redemptionAnalytics.Data);

                var fileName = $"sponsorship-analytics-{sponsorId}-{DateTime.Now:yyyyMMdd}.csv";
                return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Export işlemi başarısız: {ex.Message}");
            }
        }

        #region Private Helper Methods

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim?.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        private string GenerateAnalyticsCsv(SponsorshipDashboard dashboard, LinkPerformanceMetrics linkPerformance, RedemptionAnalytics redemptionAnalytics)
        {
            var csv = new System.Text.StringBuilder();
            
            // Header
            csv.AppendLine("Metric,Value,Description");
            
            // Overview metrics
            csv.AppendLine($"Sponsor Name,{dashboard.Overview.SponsorName},Company name");
            csv.AppendLine($"Total Purchases,{dashboard.Overview.TotalPurchases},Number of package purchases");
            csv.AppendLine($"Total Spent,{dashboard.Overview.TotalSpent:C},Total amount spent");
            csv.AppendLine($"Codes Generated,{dashboard.Overview.TotalCodesGenerated},Total codes generated");
            csv.AppendLine($"Codes Redeemed,{dashboard.Overview.TotalCodesRedeemed},Total codes redeemed");
            csv.AppendLine($"Redemption Rate,{dashboard.Overview.RedemptionRate:F2}%,Percentage of codes redeemed");
            
            // Performance metrics
            csv.AppendLine($"Total Links Sent,{linkPerformance.TotalLinksSent},Links sent via all channels");
            csv.AppendLine($"Total Clicks,{linkPerformance.TotalClicks},Total link clicks");
            csv.AppendLine($"Click-Through Rate,{linkPerformance.OverallCTR:F2}%,Overall CTR");
            csv.AppendLine($"Successful Redemptions,{redemptionAnalytics.SuccessfulRedemptions},Successful redemptions");
            csv.AppendLine($"Success Rate,{redemptionAnalytics.SuccessRate:F2}%,Redemption success rate");
            csv.AppendLine($"Total Value Redeemed,{redemptionAnalytics.TotalValueRedeemed:C},Total value of redemptions");

            return csv.ToString();
        }

        #endregion
    }
}