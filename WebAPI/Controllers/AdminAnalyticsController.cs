using Business.Handlers.AdminAnalytics.Commands;
using Business.Handlers.AdminAnalytics.Queries;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Admin controller for analytics and reporting
    /// Provides endpoints for dashboard metrics and statistics
    /// </summary>
    [Route("api/admin/analytics")]
    public class AdminAnalyticsController : AdminBaseController
    {
        /// <summary>
        /// Get user statistics and metrics
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        /// <summary>
        /// Get user statistics and metrics
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("user-statistics")]
        public async Task<IActionResult> GetUserStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = new GetUserStatisticsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Get subscription statistics and metrics
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        /// <summary>
        /// Get subscription statistics and metrics
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("subscription-statistics")]
        public async Task<IActionResult> GetSubscriptionStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = new GetSubscriptionStatisticsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Get sponsorship statistics and metrics
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("sponsorship")]
        public async Task<IActionResult> GetSponsorshipStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = new GetSponsorshipStatisticsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Get dashboard overview with all key metrics
        /// </summary>
        /// <summary>
        /// Get dashboard overview with all key metrics
        /// </summary>
        [HttpGet("dashboard-overview")]
        public async Task<IActionResult> GetDashboardOverview()
        {
            // Get all statistics in parallel for dashboard
            var userStatsTask = Mediator.Send(new GetUserStatisticsQuery());
            var subscriptionStatsTask = Mediator.Send(new GetSubscriptionStatisticsQuery());
            var sponsorshipStatsTask = Mediator.Send(new GetSponsorshipStatisticsQuery());

            await Task.WhenAll(userStatsTask, subscriptionStatsTask, sponsorshipStatsTask);

            var dashboardData = new
            {
                UserStatistics = userStatsTask.Result.Data,
                SubscriptionStatistics = subscriptionStatsTask.Result.Data,
                SponsorshipStatistics = sponsorshipStatsTask.Result.Data,
                GeneratedAt = DateTime.Now
            };

            return Ok(new { Data = dashboardData, Success = true, Message = "Dashboard data retrieved successfully" });
        }

        /// <summary>
        /// Get system activity logs with filtering and pagination
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <param name="userId">Filter by user ID (admin or target user)</param>
        /// <param name="actionType">Filter by action type</param>
        /// <param name="startDate">Start date for filtering</param>
        /// <param name="endDate">End date for filtering</param>
        [HttpGet("activity-logs")]
        public async Task<IActionResult> GetActivityLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? userId = null,
            [FromQuery] string actionType = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = new GetActivityLogsQuery
            {
                Page = page,
                PageSize = pageSize,
                UserId = userId,
                ActionType = actionType,
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Export statistics as CSV file
        /// </summary>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("export")]
        public async Task<IActionResult> ExportStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = new ExportStatisticsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await Mediator.Send(query);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            var fileName = $"ziraai-statistics-{DateTime.Now:yyyy-MM-dd-HHmmss}.csv";
            return File(result.Data, "text/csv", fileName);
        }

        /// <summary>
        /// Rebuild all admin statistics caches
        /// Used for manual cache warming or after system maintenance
        /// </summary>
        [HttpPost("rebuild-cache")]
        public async Task<IActionResult> RebuildAdminCache()
        {
            var command = new RebuildAdminCacheCommand();
            var result = await Mediator.Send(command);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
    }
}
