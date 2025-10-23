using Business.Handlers.AdminSubscriptions.Commands;
using Business.Handlers.AdminSubscriptions.Queries;
using Core.Utilities.Results;
using Entities.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Handlers.AdminSubscriptions
{
    /// <summary>
    /// Admin controller for subscription management operations
    /// </summary>
    [Route("api/v1/admin/subscriptions")]
    [ApiController]
    public class AdminSubscriptionsController : AdminBaseController
    {
        /// <summary>
        /// Get all subscriptions with filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllSubscriptionsQuery query)
        {
            var result = await Mediator.Send(query);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Get subscription details by ID with usage history
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await Mediator.Send(new GetSubscriptionByIdQuery { SubscriptionId = id });
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Get all subscriptions for a specific user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserSubscriptions(int userId, [FromQuery] bool includeInactive = false)
        {
            var result = await Mediator.Send(new GetUserSubscriptionsQuery 
            { 
                UserId = userId,
                IncludeInactive = includeInactive
            });
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Create a new subscription for a user
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubscriptionCommand command)
        {
            var result = await Mediator.Send(command);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Extend subscription end date
        /// </summary>
        [HttpPost("{id}/extend")]
        public async Task<IActionResult> Extend(int id, [FromBody] ExtendSubscriptionRequest request)
        {
            var command = new ExtendSubscriptionCommand
            {
                SubscriptionId = id,
                NewEndDate = request.NewEndDate,
                Reason = request.Reason
            };
            var result = await Mediator.Send(command);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Cancel subscription (immediate or at end of period)
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelSubscriptionRequest request)
        {
            var command = new CancelSubscriptionCommand
            {
                SubscriptionId = id,
                ImmediateCancel = request.ImmediateCancel,
                Reason = request.Reason
            };
            var result = await Mediator.Send(command);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Update subscription usage limits
        /// </summary>
        [HttpPut("{id}/limits")]
        public async Task<IActionResult> UpdateLimits(int id, [FromBody] UpdateLimitsRequest request)
        {
            var command = new UpdateSubscriptionLimitsCommand
            {
                SubscriptionId = id,
                NewDailyLimit = request.NewDailyLimit,
                NewMonthlyLimit = request.NewMonthlyLimit,
                Reason = request.Reason
            };
            var result = await Mediator.Send(command);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Reset subscription usage counters
        /// </summary>
        [HttpPost("{id}/reset-usage")]
        public async Task<IActionResult> ResetUsage(int id, [FromBody] ResetUsageRequest request)
        {
            var command = new ResetSubscriptionUsageCommand
            {
                SubscriptionId = id,
                ResetDaily = request.ResetDaily,
                ResetMonthly = request.ResetMonthly,
                Reason = request.Reason
            };
            var result = await Mediator.Send(command);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }

    #region Request Models

    public class ExtendSubscriptionRequest
    {
        public DateTime NewEndDate { get; set; }
        public string Reason { get; set; }
    }

    public class CancelSubscriptionRequest
    {
        public bool ImmediateCancel { get; set; }
        public string Reason { get; set; }
    }

    public class UpdateLimitsRequest
    {
        public int? NewDailyLimit { get; set; }
        public int? NewMonthlyLimit { get; set; }
        public string Reason { get; set; }
    }

    public class ResetUsageRequest
    {
        public bool ResetDaily { get; set; }
        public bool ResetMonthly { get; set; }
        public string Reason { get; set; }
    }

    #endregion
}
