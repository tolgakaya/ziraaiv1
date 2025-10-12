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
using Business.Services.Sponsorship;
using Core.Entities.Concrete;
using Core.Extensions;
using Core.Utilities.Results;
using DataAccess.Abstract;
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
        private readonly ISponsorshipTierMappingService _tierMappingService;
        private readonly ISubscriptionTierRepository _subscriptionTierRepository;

        public SponsorshipController(
            ILogger<SponsorshipController> logger,
            ISponsorshipTierMappingService tierMappingService,
            ISubscriptionTierRepository subscriptionTierRepository)
        {
            _logger = logger;
            _tierMappingService = tierMappingService;
            _subscriptionTierRepository = subscriptionTierRepository;
        }
        /// <summary>
        /// Get subscription tiers for sponsor package purchase selection
        /// Returns tier-specific sponsorship features (data access, logo visibility, messaging, smart links)
        /// </summary>
        /// <returns>List of available tiers with sponsorship features</returns>
        [AllowAnonymous] // Public endpoint for purchase preview
        [HttpGet("tiers-for-purchase")]
        public async Task<IActionResult> GetTiersForPurchase()
        {
            try
            {
                _logger.LogInformation("📊 Fetching subscription tiers for purchase selection");

                // Get active tiers
                var tiers = await _subscriptionTierRepository.GetActiveTiersAsync();

                // Exclude Trial tier - only show purchasable tiers (S, M, L, XL)
                var purchasableTiers = tiers.Where(t => t.TierName != "Trial").ToList();

                // Map to sponsorship comparison DTOs
                var comparisonDtos = _tierMappingService.MapToComparisonDtos(purchasableTiers);

                _logger.LogInformation("✅ Retrieved {Count} purchasable tier options (excluded Trial)", comparisonDtos.Count);

                return Ok(new SuccessDataResult<List<SponsorshipTierComparisonDto>>(
                    comparisonDtos,
                    "Sponsorship tiers retrieved successfully"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving tiers for purchase: {Message}", ex.Message);
                return StatusCode(500, new ErrorResult($"Tier retrieval failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Create sponsor company profile (one-time setup)
        /// </summary>
        /// <param name="dto">Company profile information</param>
        /// <returns>Created sponsor profile</returns>
        [Authorize] // Allow any authenticated user (Farmer can become Sponsor)
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
        /// Get sponsorship codes for current sponsor with advanced filtering and pagination
        /// </summary>
        /// <param name="onlyUnused">Return only unused codes (includes both sent and unsent)</param>
        /// <param name="onlyUnsent">Return only codes never sent to farmers (DistributionDate IS NULL) - RECOMMENDED for distribution</param>
        /// <param name="sentDaysAgo">Return codes sent X days ago but still unused (e.g., 7 for codes sent 1 week ago)</param>
        /// <param name="onlySentExpired">Return only codes sent to farmers but expired without being used - OPTIMIZED for millions of rows</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 50, max: 200)</param>
        /// <returns>Paginated list of sponsorship codes with total count and navigation info</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("codes")]
        public async Task<IActionResult> GetSponsorshipCodes(
            [FromQuery] bool onlyUnused = false,
            [FromQuery] bool onlyUnsent = false,
            [FromQuery] int? sentDaysAgo = null,
            [FromQuery] bool onlySentExpired = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            // Validate pagination parameters
            if (page < 1)
                return BadRequest(new ErrorResult("Page must be greater than 0"));
            
            if (pageSize < 1 || pageSize > 200)
                return BadRequest(new ErrorResult("Page size must be between 1 and 200"));

            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var query = new GetSponsorshipCodesQuery
            {
                SponsorId = userId.Value,
                OnlyUnused = onlyUnused,
                OnlyUnsent = onlyUnsent,
                SentDaysAgo = sentDaysAgo,
                OnlySentExpired = onlySentExpired,
                Page = page,
                PageSize = pageSize
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
        /// Get comprehensive dashboard summary for mobile app home screen
        /// Includes sent codes count, total analyses, purchases, and tier-based package breakdowns
        /// Optimized single endpoint for sponsor dashboard UI
        /// </summary>
        /// <returns>Dashboard summary with all key metrics</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("dashboard-summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    _logger.LogWarning("[Dashboard] User ID not found in claims");
                    return Unauthorized();
                }

                _logger.LogInformation("[Dashboard] Fetching dashboard summary for sponsor {SponsorId}", userId.Value);

                var query = new GetSponsorDashboardSummaryQuery
                {
                    SponsorId = userId.Value
                };

                var result = await Mediator.Send(query);

                if (result.Success)
                {
                    _logger.LogInformation("[Dashboard] Successfully retrieved dashboard summary for sponsor {SponsorId}", userId.Value);
                    return Ok(result);
                }

                _logger.LogWarning("[Dashboard] Failed to retrieve dashboard summary for sponsor {SponsorId}: {Message}",
                    userId.Value, result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Dashboard] Error retrieving dashboard summary for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Dashboard summary retrieval failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get package distribution statistics: purchased vs distributed vs redeemed breakdown
        /// </summary>
        /// <returns>Detailed package-level distribution statistics</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("package-statistics")]
        public async Task<IActionResult> GetPackageDistributionStatistics()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var query = new GetPackageDistributionStatisticsQuery
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
        /// Get code-level analysis statistics: which codes generated how many analyses
        /// </summary>
        /// <param name="includeAnalysisDetails">Include full analysis list per code (default: true)</param>
        /// <param name="topCodesCount">Number of top performing codes to show (default: 10)</param>
        /// <returns>Detailed code-level analysis statistics with drill-down capability</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("code-analysis-statistics")]
        public async Task<IActionResult> GetCodeAnalysisStatistics(
            [FromQuery] bool includeAnalysisDetails = true,
            [FromQuery] int topCodesCount = 10)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var query = new GetCodeAnalysisStatisticsQuery
            {
                SponsorId = userId.Value,
                IncludeAnalysisDetails = includeAnalysisDetails,
                TopCodesCount = topCodesCount
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
                        SubscriptionTier = "Premium", // Navigation property removed - fetch separately if needed
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
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered analysis for sponsor {UserId}, analysis {PlantAnalysisId}", GetUserId(), plantAnalysisId);
                return StatusCode(500, new ErrorResult($"Analysis retrieval failed: {ex.Message}"));
            }
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
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();
                    
                command.SponsorId = userId.Value;
                _logger.LogInformation("Creating smart link for sponsor {SponsorId}", userId.Value);
                
                var result = await Mediator.Send(command);
                
                if (result.Success)
                {
                    _logger.LogInformation("Smart link created successfully for sponsor {SponsorId}", userId.Value);
                    return Ok(result);
                }
                
                _logger.LogWarning("Smart link creation failed for sponsor {SponsorId}: {Message}", userId.Value, result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating smart link for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Smart link creation failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get sponsor's smart links
        /// </summary>
        /// <returns>List of sponsor's smart links</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("smart-links")]
        public async Task<IActionResult> GetSmartLinks()
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();
                    
                _logger.LogInformation("Getting smart links for sponsor {SponsorId}", userId.Value);
                
                var query = new GetSponsorSmartLinksQuery { SponsorId = userId.Value };
                var result = await Mediator.Send(query);
                
                if (result.Success)
                {
                    _logger.LogInformation("Retrieved {Count} smart links for sponsor {SponsorId}", result.Data?.Count ?? 0, userId.Value);
                    return Ok(result);
                }
                
                _logger.LogWarning("Failed to get smart links for sponsor {SponsorId}: {Message}", userId.Value, result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting smart links for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Smart links retrieval failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get smart link performance analytics
        /// </summary>
        /// <returns>Smart link performance data</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("smart-links/performance")]
        public async Task<IActionResult> GetSmartLinkPerformance()
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();
                    
                _logger.LogInformation("Getting smart link performance for sponsor {SponsorId}", userId.Value);
                
                var query = new GetSmartLinkPerformanceQuery { SponsorId = userId.Value };
                var result = await Mediator.Send(query);
                
                if (result.Success)
                {
                    _logger.LogInformation("Retrieved smart link performance for sponsor {SponsorId}", userId.Value);
                    return Ok(result);
                }
                
                _logger.LogWarning("Failed to get smart link performance for sponsor {SponsorId}: {Message}", userId.Value, result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting smart link performance for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Smart link performance retrieval failed: {ex.Message}"));
            }
        }

        // ====== NEW CORRECTED ARCHITECTURE ENDPOINTS ======

        /// <summary>
        /// Get sponsor logo permissions for a specific analysis
        /// </summary>
        /// <param name="plantAnalysisId">Plant analysis ID</param>
        /// <param name="screen">Screen type (start, result, analysis, profile)</param>
        /// <returns>Logo display permissions based on redeemed code tier</returns>
        [Authorize]
        [HttpGet("logo-permissions/analysis/{plantAnalysisId}")]
        public async Task<IActionResult> GetLogoPermissionsForAnalysis(int plantAnalysisId, [FromQuery] string screen = "result")
        {
            try
            {
                var query = new GetLogoPermissionsForAnalysisQuery
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