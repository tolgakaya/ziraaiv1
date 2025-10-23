using Business.Handlers.AdminSubscriptions.Commands;
using Business.Handlers.AdminSubscriptions.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Admin controller for subscription management operations
    /// Provides endpoints for viewing, assigning, extending, and canceling subscriptions
    /// </summary>
    [Route("api/admin/subscriptions")]
    public class AdminSubscriptionsController : AdminBaseController
    {
        /// <summary>
        /// Get all subscriptions with pagination and filtering
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <param name="status">Filter by status (optional)</param>
        /// <param name="isActive">Filter by active status (optional)</param>
        /// <param name="isSponsoredSubscription">Filter by sponsored status (optional)</param>
        [HttpGet]
        public async Task<IActionResult> GetAllSubscriptions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string status = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? isSponsoredSubscription = null)
        {
            var query = new GetAllSubscriptionsQuery
            {
                Page = page,
                PageSize = pageSize,
                Status = status,
                IsActive = isActive,
                IsSponsoredSubscription = isSponsoredSubscription
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Get subscription by ID
        /// </summary>
        /// <param name="subscriptionId">Subscription ID</param>
        [HttpGet("{subscriptionId}")]
        public async Task<IActionResult> GetSubscriptionById(int subscriptionId)
        {
            var query = new GetSubscriptionByIdQuery { SubscriptionId = subscriptionId };
            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Assign a subscription to a user
        /// </summary>
        /// <param name="request">Assignment request details</param>
        [HttpPost("assign")]
        public async Task<IActionResult> AssignSubscription([FromBody] AssignSubscriptionRequest request)
        {
            var command = new AssignSubscriptionCommand
            {
                UserId = request.UserId,
                SubscriptionTierId = request.SubscriptionTierId,
                DurationMonths = request.DurationMonths,
                IsSponsoredSubscription = request.IsSponsoredSubscription,
                SponsorId = request.SponsorId,
                Notes = request.Notes,
                AdminUserId = AdminUserId,
                IpAddress = ClientIpAddress,
                UserAgent = UserAgent,
                RequestPath = RequestPath
            };

            var result = await Mediator.Send(command);
            return GetResponseOnlyResult(result);
        }

        /// <summary>
        /// Extend an existing subscription
        /// </summary>
        /// <param name="subscriptionId">Subscription ID to extend</param>
        /// <param name="request">Extension request details</param>
        [HttpPost("{subscriptionId}/extend")]
        public async Task<IActionResult> ExtendSubscription(int subscriptionId, [FromBody] ExtendSubscriptionRequest request)
        {
            var command = new ExtendSubscriptionCommand
            {
                SubscriptionId = subscriptionId,
                ExtensionMonths = request.ExtensionMonths,
                Notes = request.Notes,
                AdminUserId = AdminUserId,
                IpAddress = ClientIpAddress,
                UserAgent = UserAgent,
                RequestPath = RequestPath
            };

            var result = await Mediator.Send(command);
            return GetResponseOnlyResult(result);
        }

        /// <summary>
        /// Cancel an active subscription
        /// </summary>
        /// <param name="subscriptionId">Subscription ID to cancel</param>
        /// <param name="request">Cancellation request details</param>
        [HttpPost("{subscriptionId}/cancel")]
        public async Task<IActionResult> CancelSubscription(int subscriptionId, [FromBody] CancelSubscriptionRequest request)
        {
            var command = new CancelSubscriptionCommand
            {
                SubscriptionId = subscriptionId,
                CancellationReason = request.CancellationReason,
                AdminUserId = AdminUserId,
                IpAddress = ClientIpAddress,
                UserAgent = UserAgent,
                RequestPath = RequestPath
            };

            var result = await Mediator.Send(command);
            return GetResponseOnlyResult(result);
        }
    }

    /// <summary>
    /// Request model for assigning a subscription
    /// </summary>
    public class AssignSubscriptionRequest
    {
        /// <summary>
        /// User ID to assign subscription to
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Subscription tier ID
        /// </summary>
        public int SubscriptionTierId { get; set; }

        /// <summary>
        /// Duration in months
        /// </summary>
        public int DurationMonths { get; set; }

        /// <summary>
        /// Whether this is a sponsored subscription
        /// </summary>
        public bool IsSponsoredSubscription { get; set; }

        /// <summary>
        /// Sponsor user ID (if sponsored)
        /// </summary>
        public int? SponsorId { get; set; }

        /// <summary>
        /// Notes about the assignment
        /// </summary>
        public string Notes { get; set; }
    }

    /// <summary>
    /// Request model for extending a subscription
    /// </summary>
    public class ExtendSubscriptionRequest
    {
        /// <summary>
        /// Number of months to extend
        /// </summary>
        public int ExtensionMonths { get; set; }

        /// <summary>
        /// Notes about the extension
        /// </summary>
        public string Notes { get; set; }
    }

    /// <summary>
    /// Request model for canceling a subscription
    /// </summary>
    public class CancelSubscriptionRequest
    {
        /// <summary>
        /// Reason for cancellation
        /// </summary>
        public string CancellationReason { get; set; }
    }
}
