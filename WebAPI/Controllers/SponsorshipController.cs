using Business.Handlers.Sponsorship.Commands;
using Business.Handlers.Sponsorship.Queries;
using Core.Entities.Concrete;
using Core.Extensions;
using Core.Utilities.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Sponsorship management for bulk subscription purchases and code distribution
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SponsorshipController : BaseApiController
    {
        /// <summary>
        /// Purchase bulk subscriptions for distribution to farmers
        /// </summary>
        /// <param name="command">Purchase details</param>
        /// <returns>Purchase record with generated codes</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("purchase-bulk")]
        public async Task<IActionResult> PurchaseBulkSubscriptions([FromBody] PurchaseBulkSponsorshipCommand command)
        {
            try
            {
                Console.WriteLine("[SponsorshipController] Purchase bulk request received");
                
                // Set sponsor ID from current user
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    Console.WriteLine("[SponsorshipController] User ID not found in claims");
                    return Unauthorized();
                }
                
                Console.WriteLine($"[SponsorshipController] User ID: {userId.Value}");
                command.SponsorId = userId.Value;
                
                Console.WriteLine($"[SponsorshipController] Sending command to mediator: TierId={command.SubscriptionTierId}, Quantity={command.Quantity}");
                var result = await Mediator.Send(command);
                
                Console.WriteLine($"[SponsorshipController] Mediator result: Success={result.Success}");
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SponsorshipController] Exception in PurchaseBulkSubscriptions: {ex.Message}");
                Console.WriteLine($"[SponsorshipController] Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message, details = ex.StackTrace });
            }
        }

        /// <summary>
        /// Redeem a sponsorship code to get a free subscription
        /// </summary>
        /// <param name="command">Code redemption details</param>
        /// <returns>Created subscription</returns>
        [Authorize(Roles = "Farmer,Admin")]
        [HttpPost("redeem")]
        public async Task<IActionResult> RedeemSponsorshipCode([FromBody] RedeemSponsorshipCodeCommand command)
        {
            // Set user ID from current user
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            command.UserId = userId.Value;
            command.UserEmail = GetUserEmail();
            command.UserFullName = GetUserFullName();
            
            var result = await Mediator.Send(command);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        /// <summary>
        /// Get sponsorship codes for current sponsor
        /// </summary>
        /// <param name="onlyUnused">Return only unused codes</param>
        /// <returns>List of sponsorship codes</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("codes")]
        public async Task<IActionResult> GetSponsorshipCodes([FromQuery] bool onlyUnused = false)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            var query = new GetSponsorshipCodesQuery
            {
                SponsorId = userId.Value,
                OnlyUnused = onlyUnused
            };
            
            var result = await Mediator.Send(query);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        /// <summary>
        /// Get purchase history for current sponsor
        /// </summary>
        /// <returns>List of sponsorship purchases</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("purchases")]
        public async Task<IActionResult> GetSponsorshipPurchases()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            var query = new GetSponsorshipPurchasesQuery
            {
                SponsorId = userId.Value
            };
            
            var result = await Mediator.Send(query);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        /// <summary>
        /// Get farmers sponsored by current sponsor
        /// </summary>
        /// <returns>List of sponsored farmers</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("farmers")]
        public async Task<IActionResult> GetSponsoredFarmers()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            var query = new GetSponsoredFarmersQuery
            {
                SponsorId = userId.Value
            };
            
            var result = await Mediator.Send(query);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        /// <summary>
        /// Get sponsorship statistics for current sponsor
        /// </summary>
        /// <returns>Sponsorship usage statistics</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("statistics")]
        public async Task<IActionResult> GetSponsorshipStatistics()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            var query = new GetSponsorshipStatisticsQuery
            {
                SponsorId = userId.Value
            };
            
            var result = await Mediator.Send(query);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        /// <summary>
        /// Validate a sponsorship code without redeeming it
        /// </summary>
        /// <param name="code">Sponsorship code to validate</param>
        /// <returns>Code validation result</returns>
        [Authorize(Roles = "Farmer,Sponsor,Admin")]
        [HttpGet("validate/{code}")]
        public async Task<IActionResult> ValidateSponsorshipCode(string code)
        {
            var query = new ValidateSponsorshipCodeQuery
            {
                Code = code
            };
            
            var result = await Mediator.Send(query);
            
            if (result.Success)
            {
                return Ok(new 
                {
                    Success = true,
                    Message = "Code is valid",
                    Data = new
                    {
                        Code = result.Data.Code,
                        SubscriptionTier = result.Data.SubscriptionTier?.DisplayName,
                        ExpiryDate = result.Data.ExpiryDate,
                        IsValid = true
                    }
                });
            }
            
            return Ok(new 
            {
                Success = false,
                Message = result.Message,
                Data = new
                {
                    Code = code,
                    IsValid = false
                }
            });
        }

        /// <summary>
        /// Get current user's sponsor information (for farmers)
        /// </summary>
        /// <returns>Sponsor details if user has sponsored subscription</returns>
        [Authorize(Roles = "Farmer,Admin")]
        [HttpGet("my-sponsor")]
        public async Task<IActionResult> GetMySponsor()
        {
            // This would need a new query to get user's sponsor info
            // For now, return not implemented
            return Ok(new SuccessResult("Feature coming soon"));
        }

        private int? GetUserId()
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (int.TryParse(userIdClaim, out var userId))
                return userId;
            
            return null;
        }

        private string GetUserEmail()
        {
            return User?.FindFirst(ClaimTypes.Email)?.Value;
        }

        private string GetUserFullName()
        {
            return User?.FindFirst(ClaimTypes.Name)?.Value;
        }
    }
}