using Business.Handlers.Sponsorship.Commands;
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
using Business.Handlers.MessagingFeatures.Commands;
using Business.Handlers.MessagingFeatures.Queries;
using Business.Handlers.FarmerSponsorBlock.Queries;
using Business.Services.Sponsorship;
using Core.Entities.Concrete;
using Core.Extensions;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using WebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        public SponsorshipController(
            ILogger<SponsorshipController> logger,
            ISponsorshipTierMappingService tierMappingService,
            ISubscriptionTierRepository subscriptionTierRepository,
            IConfiguration configuration)
        {
            _logger = logger;
            _tierMappingService = tierMappingService;
            _subscriptionTierRepository = subscriptionTierRepository;
            _configuration = configuration;
        }
        /// <summary>
        /// Get subscription tiers for sponsor package purchase selection
        /// Returns tier-specific sponsorship features (data access, logo visibility, messaging, smart links)
        /// </summary>
        /// <returns>List of available tiers with sponsorship features</returns>
        [AllowAnonymous] // Public endpoint for purchase preview
        [HttpGet("tiers-for-purchase")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SuccessDataResult<List<SponsorshipTierComparisonDto>>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResult))]
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<SponsorProfileDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Core.Utilities.Results.IResult))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
                    BusinessModel = dto.BusinessModel,
                    Password = dto.Password // Pass password to enable email+password login
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

        // NOTE: Deep link handling moved to RedemptionController.cs
        // GET /redeem/{code} is handled by RedemptionController.RedeemSponsorshipCode
        // which provides complete redemption flow with account creation and auto-login

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
        /// Get comprehensive messaging analytics for current sponsor
        /// Includes message volumes, response metrics, conversation metrics, content types, and satisfaction ratings
        /// Optional date range filtering for custom analytics periods
        /// Cache TTL: 15 minutes for real-time insights
        /// </summary>
        /// <param name="startDate">Start date for analytics (optional)</param>
        /// <param name="endDate">End date for analytics (optional)</param>
        /// <returns>Messaging analytics with top 10 most active conversations</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("messaging-analytics")]
        public async Task<IActionResult> GetMessagingAnalytics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    _logger.LogWarning("[MessagingAnalytics] User ID not found in claims");
                    return Unauthorized();
                }

                _logger.LogInformation("[MessagingAnalytics] Fetching analytics for sponsor {SponsorId}", userId.Value);

                var query = new GetSponsorMessagingAnalyticsQuery
                {
                    SponsorId = userId.Value,
                    StartDate = startDate,
                    EndDate = endDate
                };

                var result = await Mediator.Send(query);

                if (result.Success)
                {
                    _logger.LogInformation("[MessagingAnalytics] Successfully retrieved analytics for sponsor {SponsorId}", userId.Value);
                    return Ok(result);
                }

                _logger.LogWarning("[MessagingAnalytics] Failed to retrieve analytics for sponsor {SponsorId}: {Message}",
                    userId.Value, result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MessagingAnalytics] Error retrieving analytics for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Messaging analytics retrieval failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get comprehensive impact analytics for current sponsor
        /// Includes farmer reach, agricultural impact, geographic coverage, and severity distribution
        /// Cache TTL: 6 hours for relatively stable impact data
        /// </summary>
        /// <returns>Impact analytics with farmer, agricultural, and geographic metrics</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("impact-analytics")]
        public async Task<IActionResult> GetImpactAnalytics()
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    _logger.LogWarning("[ImpactAnalytics] User ID not found in claims");
                    return Unauthorized();
                }

                _logger.LogInformation("[ImpactAnalytics] Fetching analytics for sponsor {SponsorId}", userId.Value);

                var query = new GetSponsorImpactAnalyticsQuery
                {
                    SponsorId = userId.Value
                };

                var result = await Mediator.Send(query);

                if (result.Success)
                {
                    _logger.LogInformation("[ImpactAnalytics] Successfully retrieved analytics for sponsor {SponsorId}", userId.Value);
                    return Ok(result);
                }

                _logger.LogWarning("[ImpactAnalytics] Failed to retrieve analytics for sponsor {SponsorId}: {Message}",
                    userId.Value, result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ImpactAnalytics] Error retrieving analytics for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Impact analytics retrieval failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get comprehensive temporal analytics for current sponsor
        /// Includes time-series data with trends, growth metrics, and peak performance
        /// Supports Day, Week, or Month grouping for flexible analysis periods
        /// Cache TTL: 1 hour for relatively fresh temporal data
        /// </summary>
        /// <param name="startDate">Start date for analytics (optional, defaults to 30 days ago)</param>
        /// <param name="endDate">End date for analytics (optional, defaults to today)</param>
        /// <param name="groupBy">Grouping period: Day, Week, or Month (default: Day)</param>
        /// <returns>Temporal analytics with time-series data, trends, and peak metrics</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("temporal-analytics")]
        public async Task<IActionResult> GetTemporalAnalytics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string groupBy = "Day")
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    _logger.LogWarning("[TemporalAnalytics] User ID not found in claims");
                    return Unauthorized();
                }

                // Validate groupBy parameter
                var validGroupings = new[] { "Day", "Week", "Month" };
                if (!validGroupings.Contains(groupBy, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new ErrorResult("Invalid groupBy parameter. Use Day, Week, or Month."));
                }

                _logger.LogInformation("[TemporalAnalytics] Fetching analytics for sponsor {SponsorId} with grouping {GroupBy}", 
                    userId.Value, groupBy);

                var query = new GetSponsorTemporalAnalyticsQuery
                {
                    SponsorId = userId.Value,
                    StartDate = startDate,
                    EndDate = endDate,
                    GroupBy = groupBy
                };

                var result = await Mediator.Send(query);

                if (result.Success)
                {
                    _logger.LogInformation("[TemporalAnalytics] Successfully retrieved analytics for sponsor {SponsorId}", userId.Value);
                    return Ok(result);
                }

                _logger.LogWarning("[TemporalAnalytics] Failed to retrieve analytics for sponsor {SponsorId}: {Message}",
                    userId.Value, result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TemporalAnalytics] Error retrieving analytics for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Temporal analytics retrieval failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get comprehensive ROI (Return on Investment) analytics for current sponsor
        /// Includes cost/value breakdown, ROI metrics per tier, and efficiency statistics
        /// Uses database configuration for AnalysisUnitValue (Sponsorship:AnalysisUnitValue key)
        /// Cache TTL: 12 hours for relatively stable financial data
        /// </summary>
        /// <returns>ROI analytics with cost, value, ROI, and efficiency metrics</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("roi-analytics")]
        public async Task<IActionResult> GetROIAnalytics()
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                {
                    _logger.LogWarning("[ROIAnalytics] User ID not found in claims");
                    return Unauthorized();
                }

                _logger.LogInformation("[ROIAnalytics] Fetching analytics for sponsor {SponsorId}", userId.Value);

                var query = new GetSponsorROIAnalyticsQuery
                {
                    SponsorId = userId.Value
                };

                var result = await Mediator.Send(query);

                if (result.Success)
                {
                    _logger.LogInformation("[ROIAnalytics] Successfully retrieved analytics for sponsor {SponsorId}", userId.Value);
                    return Ok(result);
                }

                _logger.LogWarning("[ROIAnalytics] Failed to retrieve analytics for sponsor {SponsorId}: {Message}",
                    userId.Value, result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ROIAnalytics] Error retrieving analytics for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"ROI analytics retrieval failed: {ex.Message}"));
            }
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
        /// Get paginated list of sponsored analyses with tier-based filtering
        /// Returns summary information for each analysis based on sponsor's tier level
        /// Includes logo display permissions and messaging capabilities per analysis
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
        /// <param name="sortBy">Sort field: date, healthScore, cropType (default: date)</param>
        /// <param name="sortOrder">Sort order: asc, desc (default: desc)</param>
        /// <param name="filterByTier">Filter by tier: S, M, L, XL (optional)</param>
        /// <param name="filterByCropType">Filter by crop type (optional)</param>
        /// <param name="startDate">Filter by start date (optional)</param>
        /// <param name="endDate">Filter by end date (optional)</param>
        /// <returns>Paginated list of analysis summaries with tier-based field visibility</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("analyses")]
        public async Task<IActionResult> GetSponsoredAnalysesList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "date",
            [FromQuery] string sortOrder = "desc",
            [FromQuery] string filterByTier = null,
            [FromQuery] string filterByCropType = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? dealerId = null, // NEW: Filter by dealer (for dealer view)
            // Message Status Filters
            [FromQuery] string filterByMessageStatus = null,
            [FromQuery] bool? hasUnreadMessages = null,
            [FromQuery] bool? hasUnreadForCurrentUser = null,
            [FromQuery] int? unreadMessagesMin = null)
        {
            try
            {
                // Validate pagination
                if (page < 1)
                    return BadRequest(new ErrorResult("Page must be greater than 0"));

                if (pageSize < 1 || pageSize > 100)
                    return BadRequest(new ErrorResult("Page size must be between 1 and 100"));

                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                // Query will automatically include analyses where user is SponsorUserId OR DealerId
                // Optional dealerId parameter allows filtering to specific dealer (for admin/monitoring)
                var query = new GetSponsoredAnalysesListQuery
                {
                    SponsorId = userId.Value,
                    DealerId = dealerId, // Optional: filter to specific dealer (null = show all user's analyses)
                    Page = page,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortOrder = sortOrder,
                    FilterByTier = filterByTier,
                    FilterByCropType = filterByCropType,
                    StartDate = startDate,
                    EndDate = endDate,
                    // Pass messaging filters
                    FilterByMessageStatus = filterByMessageStatus,
                    HasUnreadMessages = hasUnreadMessages,
                    HasUnreadForCurrentUser = hasUnreadForCurrentUser,
                    UnreadMessagesMin = unreadMessagesMin
                };

                var result = await Mediator.Send(query);

                if (result.Success)
                {
                    _logger.LogInformation("Retrieved {Count} sponsored analyses for sponsor {SponsorId} (page {Page}/{TotalPages})",
                        result.Data.Items.Length, userId.Value, result.Data.Page, result.Data.TotalPages);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sponsored analyses list for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Analyses list retrieval failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Send message to farmer (M, L and XL tiers only)
        /// </summary>
        /// <param name="command">Message details</param>
        /// <returns>Sent message information</returns>
        [Authorize(Roles = "Sponsor,Farmer,Admin")]
        [HttpPost("messages")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<AnalysisMessageDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IDataResult<AnalysisMessageDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
        /// Get conversation between current user and another user
        /// </summary>
        /// <param name="otherUserId">The other participant's user ID (can be sponsor or farmer)</param>
        /// <param name="plantAnalysisId">Analysis ID for context</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of messages per page (default: 20, max: 100)</param>
        /// <returns>Paginated message conversation</returns>
        [Authorize(Roles = "Sponsor,Farmer,Admin")]
        [HttpGet("messages/conversation")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResult<List<AnalysisMessageDto>>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(PaginatedResult<List<AnalysisMessageDto>>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetConversation(
            int otherUserId,
            int plantAnalysisId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            // AUTHORIZATION CHECK: Verify user has access to this analysis
            // Use service to get full analysis entity (not DTO) for authorization
            var analysisMessagingService = HttpContext.RequestServices.GetService(typeof(Business.Services.Sponsorship.IAnalysisMessagingService)) as Business.Services.Sponsorship.IAnalysisMessagingService;
            var analysis = await analysisMessagingService.GetPlantAnalysisAsync(plantAnalysisId);

            if (analysis == null)
            {
                return NotFound(new { success = false, message = "Analysis not found" });
            }

            // Check if user has permission to view this conversation
            // Permissions:
            // - Farmer: UserId matches analysis.UserId
            // - Sponsor: SponsorUserId matches OR DealerId matches (hybrid support)
            // - Admin: Always allowed (role check at attribute level)
            bool hasAccess = (analysis.UserId == userId.Value) ||  // Farmer
                             (analysis.SponsorUserId == userId.Value) ||  // Main Sponsor
                             (analysis.DealerId == userId.Value);  // Dealer

            if (!hasAccess)
            {
                return Forbid();  // 403 Forbidden
            }

            // Validate and limit page size
            if (pageSize > 100) pageSize = 100;
            if (pageSize < 1) pageSize = 20;
            if (page < 1) page = 1;

            var query = new GetConversationQuery
            {
                FromUserId = userId.Value,
                ToUserId = otherUserId,
                PlantAnalysisId = plantAnalysisId,
                Page = page,
                PageSize = pageSize
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

        private string GetUserPhone()
        {
            return User?.FindFirst(ClaimTypes.MobilePhone)?.Value;
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

        // ====== FARMER BLOCK/UNBLOCK SPONSOR ENDPOINTS ======

        /// <summary>
        /// Farmer blocks a sponsor from sending messages
        /// </summary>
        /// <param name="command">Block details (sponsorId, reason)</param>
        /// <returns>Block confirmation</returns>
        [Authorize(Roles = "Farmer,Admin")]
        [HttpPost("messages/block")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Core.Utilities.Results.IResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Core.Utilities.Results.IResult))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> BlockSponsor([FromBody] Business.Handlers.FarmerSponsorBlock.Commands.BlockSponsorCommand command)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                command.FarmerId = userId.Value;
                var result = await Mediator.Send(command);

                if (result.Success)
                {
                    _logger.LogInformation("Farmer {FarmerId} blocked sponsor {SponsorId}", userId.Value, command.SponsorId);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking sponsor for farmer {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Block operation failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Farmer unblocks a sponsor
        /// </summary>
        /// <param name="sponsorId">Sponsor user ID to unblock</param>
        /// <returns>Unblock confirmation</returns>
        [Authorize(Roles = "Farmer,Admin")]
        [HttpDelete("messages/block/{sponsorId}")]
        public async Task<IActionResult> UnblockSponsor(int sponsorId)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var command = new Business.Handlers.FarmerSponsorBlock.Commands.UnblockSponsorCommand
                {
                    FarmerId = userId.Value,
                    SponsorId = sponsorId
                };

                var result = await Mediator.Send(command);

                if (result.Success)
                {
                    _logger.LogInformation("Farmer {FarmerId} unblocked sponsor {SponsorId}", userId.Value, sponsorId);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking sponsor for farmer {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Unblock operation failed: {ex.Message}"));
            }
        }

        #region Messaging Features

        /// <summary>
        /// Get messaging features configuration for a specific analysis
        /// Returns feature availability based on analysis tier and admin toggles
        /// </summary>
        /// <param name="plantAnalysisId">The plant analysis ID to check features for</param>
        /// <returns>Feature configuration with availability flags</returns>
        [Authorize]
        [HttpGet("messaging/features")]
        public async Task<IActionResult> GetMessagingFeatures([FromQuery] int plantAnalysisId)
        {
            try
            {
                if (plantAnalysisId <= 0)
                    return BadRequest(new ErrorResult("Plant analysis ID is required"));

                var query = new Business.Handlers.MessagingFeatures.Queries.GetMessagingFeaturesQuery
                {
                    PlantAnalysisId = plantAnalysisId
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
                _logger.LogError(ex, "Error getting messaging features for user {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Failed to retrieve messaging features: {ex.Message}"));
            }
        }

        /// <summary>
        /// Update messaging feature toggle (Admin only)
        /// </summary>
        /// <param name="featureId">Feature ID to update</param>
        /// <param name="request">Update request with IsEnabled flag</param>
        /// <returns>Success or error result</returns>
        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/messaging/features/{featureId}")]
        public async Task<IActionResult> UpdateMessagingFeature(int featureId, [FromBody] UpdateMessagingFeatureRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var command = new Business.Handlers.MessagingFeatures.Commands.UpdateMessagingFeatureCommand
                {
                    FeatureId = featureId,
                    IsEnabled = request.IsEnabled,
                    AdminUserId = userId.Value
                };

                var result = await Mediator.Send(command);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating messaging feature {FeatureId} by admin {UserId}", featureId, GetUserId());
                return StatusCode(500, new ErrorResult($"Failed to update feature: {ex.Message}"));
            }
        }

        /// <summary>
        /// Mark a single message as read
        /// </summary>
        [Authorize]
        [HttpPatch("messages/{messageId}/read")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkMessageAsRead(int messageId)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var command = new Business.Handlers.AnalysisMessages.Commands.MarkMessageAsReadCommand
                {
                    MessageId = messageId,
                    UserId = userId.Value
                };

                var result = await Mediator.Send(command);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message {MessageId} as read for user {UserId}", messageId, GetUserId());
                return StatusCode(500, new ErrorResult($"Failed to mark message as read: {ex.Message}"));
            }
        }

        /// <summary>
        /// Mark multiple messages as read (bulk operation for conversation view)
        /// </summary>
        [Authorize]
        [HttpPatch("messages/read")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkMessagesAsRead([FromBody] List<int> messageIds)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var command = new Business.Handlers.AnalysisMessages.Commands.MarkMessagesAsReadCommand
                {
                    MessageIds = messageIds,
                    UserId = userId.Value
                };

                var result = await Mediator.Send(command);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read for user {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Failed to mark messages as read: {ex.Message}"));
            }
        }

        /// <summary>
        /// Edit message (M tier+, 1 hour limit) - Phase 4A
        /// </summary>
        [Authorize]
        [HttpPut("messages/{messageId}")]
        [Produces("application/json")]
        public async Task<IActionResult> EditMessage(int messageId, [FromBody] string newMessage)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var command = new Business.Handlers.AnalysisMessages.Commands.EditMessageCommand
                {
                    MessageId = messageId,
                    UserId = userId.Value,
                    NewMessage = newMessage
                };

                var result = await Mediator.Send(command);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing message {MessageId}", messageId);
                return StatusCode(500, new ErrorResult($"Failed to edit message: {ex.Message}"));
            }
        }

        /// <summary>
        /// Delete message (All tiers, 24 hour limit) - Phase 4A
        /// </summary>
        [Authorize]
        [HttpDelete("messages/{messageId}")]
        [Produces("application/json")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var command = new Business.Handlers.AnalysisMessages.Commands.DeleteMessageCommand
                {
                    MessageId = messageId,
                    UserId = userId.Value
                };

                var result = await Mediator.Send(command);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
                return StatusCode(500, new ErrorResult($"Failed to delete message: {ex.Message}"));
            }
        }

        /// <summary>
        /// Forward message (M tier+) - Phase 4B
        /// </summary>
        [Authorize]
        [HttpPost("messages/{messageId}/forward")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Core.Utilities.Results.IResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Core.Utilities.Results.IResult))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ForwardMessage(
            int messageId,
            [FromBody] ForwardMessageRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var command = new Business.Handlers.AnalysisMessages.Commands.ForwardMessageCommand
                {
                    MessageId = messageId,
                    FromUserId = userId.Value,
                    ToUserId = request.ToUserId,
                    PlantAnalysisId = request.PlantAnalysisId
                };

                var result = await Mediator.Send(command);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding message {MessageId}", messageId);
                return StatusCode(500, new ErrorResult($"Failed to forward message: {ex.Message}"));
            }
        }

        /// <summary>
        /// Send voice message (XL tier only) - Phase 2B
        /// </summary>
        [Authorize]
        [HttpPost("messages/voice")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendVoiceMessage(SendVoiceMessageDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var command = new Business.Handlers.AnalysisMessages.Commands.SendVoiceMessageCommand
                {
                    FromUserId = userId.Value,
                    ToUserId = dto.ToUserId,
                    PlantAnalysisId = dto.PlantAnalysisId,
                    Duration = dto.Duration,
                    Waveform = dto.Waveform,
                    VoiceFile = dto.VoiceFile
                };

                var result = await Mediator.Send(command);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending voice message for user {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Failed to send voice message: {ex.Message}"));
            }
        }

        /// <summary>
        /// Send message with attachments (images/files) - Phase 2A
        /// </summary>
        [Authorize]
        [HttpPost("messages/attachments")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SendMessageWithAttachments(SendMessageWithAttachmentsDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var command = new Business.Handlers.AnalysisMessages.Commands.SendMessageWithAttachmentCommand
                {
                    FromUserId = userId.Value,
                    ToUserId = dto.ToUserId,
                    PlantAnalysisId = dto.PlantAnalysisId,
                    Message = dto.Message,
                    MessageType = dto.MessageType ?? "Information",
                    Attachments = dto.Attachments
                };

                var result = await Mediator.Send(command);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message with attachments for user {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Failed to send message with attachments: {ex.Message}"));
            }
        }

        #endregion

        // ====== DEALER CODE DISTRIBUTION ENDPOINTS ======

        /// <summary>
        /// Transfer sponsorship codes to a dealer (sub-sponsor)
        /// Main sponsor can distribute codes to dealers who will distribute them to farmers
        /// </summary>
        /// <param name="command">Transfer details (dealerId, purchaseId, codeCount)</param>
        /// <returns>Transfer result with transferred code IDs</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("dealer/transfer-codes")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<DealerCodeTransferResponseDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IDataResult<DealerCodeTransferResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> TransferCodesToDealer([FromBody] TransferCodesToDealerCommand command)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                command.UserId = userId.Value;
                var result = await Mediator.Send(command);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully transferred {Count} codes to dealer {DealerId}", 
                        result.Data.TransferredCount, result.Data.DealerId);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring codes to dealer for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Code transfer failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Create dealer invitation (Invite or AutoCreate types)
        /// Invite: Sends invitation link to existing sponsor
        /// AutoCreate: Creates new sponsor account with auto-generated password
        /// </summary>
        /// <param name="command">Invitation details (email, phone, dealerName, invitationType, purchaseId, codeCount)</param>
        /// <returns>Invitation details with token/link or auto-created credentials</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("dealer/invite")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<DealerInvitationResponseDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IDataResult<DealerInvitationResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateDealerInvitation([FromBody] CreateDealerInvitationCommand command)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                command.SponsorId = userId.Value;
                var result = await Mediator.Send(command);

                if (result.Success)
                {
                    _logger.LogInformation("Dealer invitation created for sponsor {SponsorId}, type: {Type}", 
                        userId.Value, command.InvitationType);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dealer invitation for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Invitation creation failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Send dealer invitation via SMS with deep link
        /// Creates invitation and sends SMS with token for easy mobile acceptance
        /// </summary>
        /// <param name="command">Invitation details (email, phone, dealerName, purchaseId, codeCount)</param>
        /// <returns>Invitation details with SMS delivery status</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("dealer/invite-via-sms")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<DealerInvitationResponseDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IDataResult<DealerInvitationResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> InviteDealerViaSms([FromBody] InviteDealerViaSmsCommand command)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                command.SponsorId = userId.Value;
                var result = await Mediator.Send(command);

                if (result.Success)
                {
                    _logger.LogInformation("Dealer SMS invitation sent for sponsor {SponsorId} to {Phone}", 
                        userId.Value, command.Phone);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending dealer SMS invitation for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"SMS invitation failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Accept dealer invitation (mobile endpoint)
        /// Validates token, assigns Sponsor role if needed, and transfers codes
        /// </summary>
        /// <param name="command">Invitation token</param>
        /// <returns>Acceptance result with transferred code count</returns>
        [Authorize]
        [HttpPost("dealer/accept-invitation")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<DealerInvitationAcceptResponseDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IDataResult<DealerInvitationAcceptResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AcceptDealerInvitation([FromBody] AcceptDealerInvitationCommand command)
        {
            try
            {
                var userId = GetUserId();
                var userEmail = GetUserEmail();
                var userPhone = GetUserPhone();

                if (!userId.HasValue)
                    return Unauthorized();

                command.CurrentUserId = userId.Value;
                command.CurrentUserEmail = userEmail;
                command.CurrentUserPhone = userPhone;

                var result = await Mediator.Send(command);

                if (result.Success)
                {
                    _logger.LogInformation("Dealer invitation {InvitationToken} accepted by user {UserId}",
                        command.InvitationToken, userId.Value);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting dealer invitation for user {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Invitation acceptance failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get dealer invitation details by token (PUBLIC - no auth required)
        /// Used by mobile app to display invitation details before login/acceptance
        /// </summary>
        /// <param name="token">Invitation token</param>
        /// <returns>Invitation details (sponsor name, code count, expiry, etc.)</returns>
        [AllowAnonymous]
        [HttpGet("dealer/invitation-details")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<DealerInvitationDetailsDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IDataResult<DealerInvitationDetailsDto>))]
        public async Task<IActionResult> GetDealerInvitationDetails([FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return BadRequest(new ErrorDataResult<DealerInvitationDetailsDto>("Token is required"));

                var query = new GetDealerInvitationDetailsQuery
                {
                    InvitationToken = token
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
                _logger.LogError(ex, "Error getting invitation details for token {Token}", token);
                return StatusCode(500, new ErrorResult($"Invitation details retrieval failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Reclaim unused codes from a dealer back to main sponsor
        /// Allows main sponsor to reclaim codes that were transferred but not yet distributed
        /// </summary>
        /// <param name="command">Reclaim details (dealerId, optional codeIds)</param>
        /// <returns>Reclaim result with reclaimed code IDs and count</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpPost("dealer/reclaim-codes")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<ReclaimCodesResponseDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IDataResult<ReclaimCodesResponseDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ReclaimDealerCodes([FromBody] ReclaimDealerCodesCommand command)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                command.UserId = userId.Value;
                var result = await Mediator.Send(command);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully reclaimed {Count} codes from dealer {DealerId}", 
                        result.Data.ReclaimedCount, result.Data.DealerId);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reclaiming codes from dealer for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Code reclaim failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get performance analytics for a specific dealer
        /// Shows codes received, sent, used, available, reclaimed, and unique farmers reached
        /// </summary>
        /// <param name="dealerId">Dealer user ID</param>
        /// <returns>Detailed dealer performance metrics</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("dealer/analytics/{dealerId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<DealerPerformanceDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IDataResult<DealerPerformanceDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetDealerPerformance(int dealerId)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var query = new GetDealerPerformanceQuery
                {
                    UserId = userId.Value,
                    DealerId = dealerId
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
                _logger.LogError(ex, "Error getting dealer performance for sponsor {UserId}, dealer {DealerId}", 
                    GetUserId(), dealerId);
                return StatusCode(500, new ErrorResult($"Dealer performance retrieval failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get summary of all dealers for current sponsor
        /// Aggregated statistics across all dealers
        /// </summary>
        /// <returns>Dealer summary with aggregated metrics and individual dealer list</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("dealer/summary")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<DealerSummaryDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetDealerSummary()
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var query = new GetDealerSummaryQuery
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dealer summary for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Dealer summary retrieval failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get list of dealer invitations for current sponsor
        /// Optional status filter (Pending, Accepted, Expired, Cancelled)
        /// </summary>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of dealer invitations</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("dealer/invitations")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<List<DealerInvitationListDto>>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetDealerInvitations([FromQuery] string status = null)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var query = new GetDealerInvitationsQuery
                {
                    SponsorId = userId.Value,
                    Status = status
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
                _logger.LogError(ex, "Error getting dealer invitations for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Invitations retrieval failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Search for existing sponsor/dealer by email (Method A: Manual search)
        /// Returns user details and sponsor role status
        /// </summary>
        /// <param name="email">Email address to search</param>
        /// <returns>User details with sponsor role flag</returns>
        [Authorize(Roles = "Sponsor,Admin")]
        [HttpGet("dealer/search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<DealerSearchResultDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(IDataResult<DealerSearchResultDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SearchDealerByEmail([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(new ErrorDataResult<DealerSearchResultDto>("Email is required"));

                var query = new SearchDealerByEmailQuery
                {
                    Email = email
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
                _logger.LogError(ex, "Error searching dealer by email for sponsor {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Dealer search failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get list of blocked sponsors for current farmer
        /// </summary>
        /// <returns>List of blocked sponsors</returns>
        [Authorize(Roles = "Farmer,Admin")]
        [HttpGet("messages/blocked")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<List<BlockedSponsorDto>>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetBlockedSponsors()
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var query = new Business.Handlers.FarmerSponsorBlock.Queries.GetBlockedSponsorsQuery
                {
                    FarmerId = userId.Value
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
                _logger.LogError(ex, "Error getting blocked sponsors for farmer {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Retrieval failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get authenticated user's pending dealer invitations
        /// </summary>
        /// <returns>List of pending invitations for the current user</returns>
        [HttpGet("dealer/invitations/my-pending")]
        [Authorize(Roles = "Dealer,Farmer,Sponsor")]
        public async Task<IActionResult> GetMyPendingInvitations()
        {
            try
            {
                var userEmail = GetUserEmail();
                var userPhone = GetUserPhone();

                if (string.IsNullOrEmpty(userEmail) && string.IsNullOrEmpty(userPhone))
                {
                    _logger.LogWarning("⚠️ User has no email or phone in JWT claims");
                    return BadRequest(new ErrorDataResult<object>("Email veya telefon bilgisi bulunamadı"));
                }

                var query = new GetMyPendingInvitationsQuery
                {
                    UserEmail = userEmail,
                    UserPhone = userPhone
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
                _logger.LogError(ex, "❌ Error getting pending invitations for user");
                return StatusCode(500, new ErrorDataResult<object>("Bekleyen davetiyeler alınırken hata oluştu"));
            }
        }

        /// <summary>
        /// Get codes transferred to current dealer
        /// Returns codes that have been transferred from sponsors to this dealer
        /// Use onlyUnsent=true to get codes not yet distributed to farmers (available for distribution)
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50, max: 200)</param>
        /// <param name="onlyUnsent">Only show codes not sent to farmers yet (default: false)</param>
        /// <returns>Paginated list of dealer's codes</returns>
        [Authorize(Roles = "Dealer,Sponsor")]
        [HttpGet("dealer/my-codes")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<SponsorshipCodesPaginatedDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMyDealerCodes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] bool onlyUnsent = false)
        {
            try
            {
                // Validate pagination
                if (page < 1)
                    return BadRequest(new ErrorResult("Page must be greater than 0"));

                if (pageSize < 1 || pageSize > 200)
                    return BadRequest(new ErrorResult("Page size must be between 1 and 200"));

                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var query = new GetDealerCodesQuery
                {
                    DealerId = userId.Value,
                    Page = page,
                    PageSize = pageSize,
                    OnlyUnsent = onlyUnsent
                };

                var result = await Mediator.Send(query);

                if (result.Success)
                {
                    _logger.LogInformation("Dealer codes retrieved for dealer {DealerId}, OnlyUnsent: {OnlyUnsent}, TotalCount: {Count}",
                        userId.Value, onlyUnsent, result.Data?.TotalCount ?? 0);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dealer codes for user {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Dealer kodları alınırken hata oluştu: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get dashboard summary for current dealer
        /// Returns quick statistics: total received, available, sent, used codes
        /// Optimized for fast loading with minimal queries
        /// </summary>
        /// <returns>Dashboard summary with code statistics</returns>
        [Authorize(Roles = "Dealer,Sponsor")]
        [HttpGet("dealer/my-dashboard")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<DealerDashboardSummaryDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyDealerDashboard()
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                var userEmail = GetUserEmail();
                var userPhone = GetUserPhone();

                var query = new GetDealerDashboardSummaryQuery
                {
                    DealerId = userId.Value,
                    UserEmail = userEmail,
                    UserPhone = userPhone
                };

                var result = await Mediator.Send(query);

                if (result.Success)
                {
                    _logger.LogInformation("Dealer dashboard summary retrieved for dealer {DealerId}", userId.Value);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dealer dashboard summary for user {UserId}", GetUserId());
                return StatusCode(500, new ErrorResult($"Dashboard özeti alınırken hata oluştu: {ex.Message}"));
            }
        }
    }
}