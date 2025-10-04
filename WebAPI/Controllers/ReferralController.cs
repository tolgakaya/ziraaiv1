using Business.Handlers.Referrals.Commands;
using Business.Handlers.Referrals.Queries;
using Core.Utilities.Results;
using Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Referral system endpoints for generating links, tracking clicks, and managing rewards
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class ReferralController : BaseApiController
    {
        /// <summary>
        /// Generate and send referral links via SMS/WhatsApp (hybrid supported)
        /// </summary>
        /// <param name="request">Delivery method, phone numbers, and optional custom message</param>
        /// <returns>Referral link details and delivery statuses</returns>
        [Authorize]
        [HttpPost("generate")]
        [ProducesResponseType(typeof(IDataResult<ReferralLinkResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IDataResult<ReferralLinkResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateReferralLink([FromBody] GenerateReferralLinkRequest request)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return BadRequest(new ErrorDataResult<ReferralLinkResponse>("User ID not found in token"));

            var command = new GenerateReferralLinkCommand
            {
                UserId = userId.Value,
                DeliveryMethod = request.DeliveryMethod,
                PhoneNumbers = request.PhoneNumbers,
                CustomMessage = request.CustomMessage
            };

            var result = await Mediator.Send(command);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Track a referral link click (public endpoint - no authentication required)
        /// </summary>
        /// <param name="request">Referral code, IP address, and device ID</param>
        /// <returns>Success/error result</returns>
        [AllowAnonymous]
        [HttpPost("track-click")]
        [ProducesResponseType(typeof(Core.Utilities.Results.IResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Core.Utilities.Results.IResult), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TrackReferralClick([FromBody] TrackReferralClickRequest request)
        {
            var ipAddress = GetClientIpAddress();

            var command = new TrackReferralClickCommand
            {
                Code = request.Code,
                IpAddress = ipAddress ?? request.IpAddress,
                DeviceId = request.DeviceId
            };

            var result = await Mediator.Send(command);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Validate if a referral code is valid and active (public endpoint - no authentication required)
        /// </summary>
        /// <param name="request">Referral code to validate</param>
        /// <returns>Validation result</returns>
        [AllowAnonymous]
        [HttpPost("validate")]
        [ProducesResponseType(typeof(Core.Utilities.Results.IResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Core.Utilities.Results.IResult), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ValidateReferralCode([FromBody] ValidateReferralCodeRequest request)
        {
            var query = new ValidateReferralCodeQuery
            {
                Code = request.Code
            };

            var result = await Mediator.Send(query);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Get comprehensive referral statistics for the authenticated user
        /// </summary>
        /// <returns>Referral statistics including counts and credits</returns>
        [Authorize]
        [HttpGet("stats")]
        [ProducesResponseType(typeof(IDataResult<ReferralStatsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IDataResult<ReferralStatsResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetReferralStats()
        {
            var userId = GetUserId();
            
            // 🔍 DEBUG: Log token UserId for troubleshooting
            Console.WriteLine($"[REFERRAL_STATS] Token UserId: {userId?.ToString() ?? "NULL"}");
            
            if (!userId.HasValue)
                return BadRequest(new ErrorDataResult<ReferralStatsResponse>("User ID not found in token"));

            var query = new GetReferralStatsQuery
            {
                UserId = userId.Value
            };

            var result = await Mediator.Send(query);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Get all referral codes for the authenticated user
        /// </summary>
        /// <returns>List of user's referral codes</returns>
        [Authorize]
        [HttpGet("codes")]
        [ProducesResponseType(typeof(IDataResult<List<ReferralCodeDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IDataResult<List<ReferralCodeDto>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUserReferralCodes()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return BadRequest(new ErrorDataResult<List<ReferralCodeDto>>("User ID not found in token"));

            var query = new GetUserReferralCodesQuery
            {
                UserId = userId.Value
            };

            var result = await Mediator.Send(query);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Get referral credit breakdown (earned, used, balance)
        /// </summary>
        /// <returns>Credit breakdown details</returns>
        [Authorize]
        [HttpGet("credits")]
        [ProducesResponseType(typeof(IDataResult<ReferralCreditBreakdownDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IDataResult<ReferralCreditBreakdownDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetReferralCreditBreakdown()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return BadRequest(new ErrorDataResult<ReferralCreditBreakdownDto>("User ID not found in token"));

            var query = new GetReferralCreditBreakdownQuery
            {
                UserId = userId.Value
            };

            var result = await Mediator.Send(query);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Get reward history for the authenticated user
        /// </summary>
        /// <returns>List of referral rewards</returns>
        [Authorize]
        [HttpGet("rewards")]
        [ProducesResponseType(typeof(IDataResult<List<ReferralRewardDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IDataResult<List<ReferralRewardDto>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetReferralRewards()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return BadRequest(new ErrorDataResult<List<ReferralRewardDto>>("User ID not found in token"));

            var query = new GetReferralRewardsQuery
            {
                UserId = userId.Value
            };

            var result = await Mediator.Send(query);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Disable a referral code
        /// </summary>
        /// <param name="code">Referral code to disable</param>
        /// <returns>Success/error result</returns>
        [Authorize]
        [HttpDelete("disable/{code}")]
        [ProducesResponseType(typeof(Core.Utilities.Results.IResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Core.Utilities.Results.IResult), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DisableReferralCode([FromRoute] string code)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return BadRequest(new ErrorResult("User ID not found in token"));

            var command = new DisableReferralCodeCommand
            {
                UserId = userId.Value,
                Code = code
            };

            var result = await Mediator.Send(command);

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Helper method to get authenticated user ID from JWT claims
        /// </summary>
        private int? GetUserId()
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
                return userId;
            return null;
        }

        /// <summary>
        /// Helper method to get client IP address
        /// </summary>
        private string GetClientIpAddress()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            // Check for X-Forwarded-For header (for proxies/load balancers)
            if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            }
            else if (HttpContext.Request.Headers.ContainsKey("X-Real-IP"))
            {
                ipAddress = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            }

            return ipAddress;
        }
    }
}
