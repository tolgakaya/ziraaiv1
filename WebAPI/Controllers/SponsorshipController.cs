using Business.Handlers.Sponsorship.Commands;
using Business.Handlers.Sponsorship.Queries;
using Business.Handlers.Sponsorships.Queries;
using Business.Handlers.SponsorProfiles.Commands;
using Business.Handlers.SponsorProfiles.Queries;
using Business.Handlers.AnalysisMessages.Commands;
using Business.Handlers.AnalysisMessages.Queries;
using Business.Handlers.SmartLinks.Commands;
using Business.Handlers.SmartLinks.Queries;
using Business.Handlers.PlantAnalyses.Queries;
using Core.Entities.Concrete;
using Core.Extensions;
using Core.Utilities.Results;
using Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Sponsorship management for bulk subscription purchases and code distribution
    /// </summary>
    [Route("api/v{version:apiVersion}/sponsorship")]
    [ApiController]
    public class SponsorshipController : BaseApiController
    {
        private readonly ILogger<SponsorshipController> _logger;

        public SponsorshipController(ILogger<SponsorshipController> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Create sponsor company profile (one-time setup)
        /// </summary>
        /// <param name="dto">Company profile information</param>
        /// <returns>Created sponsor profile</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("create-profile")]
        public async Task<IActionResult> CreateSponsorProfile([FromBody] CreateSponsorProfileDto dto)
        {
            try
            {
                // Set sponsor ID from current user
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }
                
                // Map DTO to Command and set SponsorId from authenticated user
                var command = new CreateSponsorProfileCommand
                {
                    SponsorId = userId.Value,
                    CompanyName = dto.CompanyName,
                    CompanyDescription = dto.CompanyDescription,
                    SponsorLogoUrl = dto.SponsorLogoUrl,
                    WebsiteUrl = dto.WebsiteUrl,
                    ContactEmail = dto.ContactEmail,
                    ContactPhone = dto.ContactPhone,
                    ContactPerson = dto.ContactPerson,
                    CompanyType = dto.CompanyType,
                    BusinessModel = dto.BusinessModel
                };
                
                var result = await Mediator.Send(command);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sponsor profile for user {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Profile creation failed: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Purchase subscription packages for distribution to farmers
        /// </summary>
        /// <param name="command">Package purchase details</param>
        /// <returns>Purchase record with generated codes</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("purchase-package")]
        public async Task<IActionResult> PurchasePackage([FromBody] PurchaseBulkSponsorshipCommand command)
        {
            try
            {
                // Set sponsor ID from current user
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }
                
                command.SponsorId = userId.Value;
                var result = await Mediator.Send(command);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error purchasing bulk subscriptions for sponsor {UserId}", GetUserId());
                return StatusCode(500, new { error = ex.Message });
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
        /// Create individual sponsorship code
        /// </summary>
        /// <param name="command">Code creation details</param>
        /// <returns>Created sponsorship code</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("codes")]
        public async Task<IActionResult> CreateSponsorshipCode([FromBody] CreateSponsorshipCodeCommand command)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                command.SponsorId = userId.Value;
                var result = await Mediator.Send(command);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sponsorship code");
                return StatusCode(500, "Internal server error");
            }
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
        /// Get link distribution statistics for current sponsor
        /// </summary>
        /// <param name="startDate">Start date for statistics (optional)</param>
        /// <param name="endDate">End date for statistics (optional)</param>
        /// <returns>Link usage and performance statistics</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("link-statistics")]
        public async Task<IActionResult> GetLinkStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            var query = new GetLinkStatisticsQuery
            {
                SponsorId = userId.Value,
                StartDate = startDate,
                EndDate = endDate
            };
            
            var result = await Mediator.Send(query);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        /// <summary>
        /// Send sponsorship links via SMS or WhatsApp to farmers
        /// </summary>
        /// <param name="command">Link sending details with recipients</param>
        /// <returns>Bulk send results</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("send-link")]
        public async Task<IActionResult> SendSponsorshipLink([FromBody] SendSponsorshipLinkCommand command)
        {
            try
            {
                _logger.LogInformation("Send sponsorship link request received");
                
                // Set sponsor ID from current user
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    _logger.LogWarning("User ID not found in claims");
                    return Unauthorized();
                }
                
                command.SponsorId = userId.Value;
                _logger.LogInformation("Sending {Count} links for sponsor {SponsorId} via {Channel}", 
                    command.Recipients?.Count ?? 0, command.SponsorId, command.Channel);
                
                var result = await Mediator.Send(command);
                
                if (result.Success)
                {
                    _logger.LogInformation("Links sent successfully. Success: {Success}, Failed: {Failed}", 
                        result.Data?.SuccessCount ?? 0, result.Data?.FailureCount ?? 0);
                    return Ok(result);
                }
                
                _logger.LogWarning("Failed to send links: {Message}", result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending sponsorship links for sponsor {UserId}", GetUserId());
                return StatusCode(500, new { error = "Bağlantılar gönderilirken hata oluştu." });
            }
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

        // ====== SPONSOR TIER-BASED BENEFITS ENDPOINTS ======

        /// <summary>
        /// Get sponsor profile information
        /// </summary>
        /// <returns>Sponsor profile details</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("profile")]
        public async Task<IActionResult> GetSponsorProfile()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            var query = new GetSponsorProfileQuery { SponsorId = userId.Value };
            var result = await Mediator.Send(query);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Create or update sponsor profile
        /// </summary>
        /// <param name="command">Sponsor profile information</param>
        /// <returns>Created/updated profile</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("profile")]
        public async Task<IActionResult> CreateOrUpdateSponsorProfile([FromBody] CreateSponsorProfileCommand command)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            command.SponsorId = userId.Value;
            var result = await Mediator.Send(command);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Get filtered plant analysis data based on sponsor tier
        /// </summary>
        /// <param name="plantAnalysisId">Analysis ID to view</param>
        /// <returns>Filtered analysis data</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("analysis/{plantAnalysisId}")]
        public async Task<IActionResult> GetAnalysisForSponsor(int plantAnalysisId)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            var query = new GetFilteredAnalysisForSponsorQuery 
            { 
                SponsorId = userId.Value, 
                PlantAnalysisId = plantAnalysisId 
            };
            
            var result = await Mediator.Send(query);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Send message to farmer (M, L and XL tiers only)
        /// </summary>
        /// <param name="command">Message details</param>
        /// <returns>Sent message information</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("messages")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageCommand command)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();
                    
                command.FromUserId = userId.Value;
                
                _logger.LogInformation($"[SendMessage] User {userId} sending message. Command: {System.Text.Json.JsonSerializer.Serialize(command)}");
                
                var result = await Mediator.Send(command);
                
                _logger.LogInformation($"[SendMessage] Result: Success={result.Success}, Message={result.Message}");
                
                if (result.Success)
                    return Ok(result);
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[SendMessage] Exception occurred: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message, innerException = ex.InnerException?.Message });
            }
        }

        /// <summary>
        /// Get conversation with farmer
        /// </summary>
        /// <param name="farmerId">Farmer user ID</param>
        /// <param name="plantAnalysisId">Analysis ID for context</param>
        /// <returns>Message conversation</returns>
        [Authorize(Roles = "Sponsor,Farmer,Admin")]
        [HttpGet("messages/conversation")]
        public async Task<IActionResult> GetConversation(int farmerId, int plantAnalysisId)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            var query = new GetConversationQuery
            {
                FromUserId = userId.Value,
                ToUserId = farmerId,
                PlantAnalysisId = plantAnalysisId
            };
            
            var result = await Mediator.Send(query);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Create smart link (XL tier only)
        /// </summary>
        /// <param name="command">Smart link details</param>
        /// <returns>Created smart link</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("smart-links")]
        public async Task<IActionResult> CreateSmartLink([FromBody] CreateSmartLinkCommand command)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            command.SponsorId = userId.Value;
            var result = await Mediator.Send(command);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Get sponsor's smart links
        /// </summary>
        /// <returns>List of sponsor's smart links</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("smart-links")]
        public async Task<IActionResult> GetSmartLinks()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            var query = new GetSponsorSmartLinksQuery { SponsorId = userId.Value };
            var result = await Mediator.Send(query);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Get smart link performance analytics
        /// </summary>
        /// <returns>Smart link performance data</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("smart-links/performance")]
        public async Task<IActionResult> GetSmartLinkPerformance()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
                
            var query = new GetSmartLinkPerformanceQuery { SponsorId = userId.Value };
            var result = await Mediator.Send(query);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        // ====== NEW CORRECTED ARCHITECTURE ENDPOINTS ======

        /// <summary>
        /// Get sponsor logo permissions for a specific analysis (NEW LOGIC)
        /// </summary>
        /// <param name="plantAnalysisId">Plant analysis ID</param>
        /// <returns>Logo display permissions based on redeemed code tier</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("logo-permissions/analysis/{plantAnalysisId}")]
        public async Task<IActionResult> GetLogoPermissionsForAnalysis(int plantAnalysisId)
        {
            try
            {
                // This would use the new SponsorVisibilityService methods
                // TODO: Create GetLogoPermissionsForAnalysisQuery
                
                return Ok(new
                {
                    Success = true,
                    Message = "Feature being implemented with new architecture",
                    PlantAnalysisId = plantAnalysisId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logo permissions for analysis {PlantAnalysisId}", plantAnalysisId);
                return StatusCode(500, new ErrorResult($"Logo permissions check failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get sponsor display information for a specific analysis screen (NEW LOGIC)
        /// </summary>
        /// <param name="plantAnalysisId">Plant analysis ID</param>
        /// <param name="screen">Screen type (start, result, analysis, profile)</param>
        /// <returns>Sponsor information if logo can be displayed</returns>
        [Authorize]
        [HttpGet("display-info/analysis/{plantAnalysisId}")]
        public async Task<IActionResult> GetDisplayInfoForAnalysis(int plantAnalysisId, [FromQuery] string screen = "result")
        {
            try
            {
                var query = new GetSponsorDisplayInfoForAnalysisQuery
                {
                    PlantAnalysisId = plantAnalysisId,
                    Screen = screen
                };
                
                var result = await Mediator.Send(query);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting display info for analysis {PlantAnalysisId}, screen {Screen}", plantAnalysisId, screen);
                return StatusCode(500, new ErrorResult($"Display info retrieval failed: {ex.Message}"));
            }
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

        /// <summary>
        /// Debug endpoint to check user's roles and claims
        /// </summary>
        /// <returns>User's roles and claims</returns>
        [Authorize]
        [HttpGet("debug/user-info")]
        public IActionResult GetUserInfo()
        {
            try
            {
                var userId = GetUserId();
                var userRoles = User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList() ?? new List<string>();
                var allClaims = User?.Claims?.Select(c => new { Type = c.Type, Value = c.Value }).ToList();

                return Ok(new
                {
                    Success = true,
                    Data = new
                    {
                        UserId = userId,
                        Roles = userRoles,
                        HasSponsorRole = userRoles.Contains("Sponsor"),
                        HasAdminRole = userRoles.Contains("Admin"),
                        AllClaims = allClaims,
                        IsAuthenticated = User?.Identity?.IsAuthenticated ?? false,
                        UserName = User?.Identity?.Name
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Success = false,
                    Message = $"Debug failed: {ex.Message}",
                    Exception = ex.ToString()
                });
            }
        }
    }
}