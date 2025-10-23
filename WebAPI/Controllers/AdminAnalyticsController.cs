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
        [HttpGet("users")]
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
        [HttpGet("subscriptions")]
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
        [HttpGet("dashboard")]
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
    }
}
