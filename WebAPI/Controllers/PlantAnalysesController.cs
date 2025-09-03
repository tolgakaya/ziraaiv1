using Business.Handlers.PlantAnalyses.Commands;
using Business.Handlers.PlantAnalyses.Queries;
using Business.Services.PlantAnalysis;
using Business.Services.Subscription;
using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class PlantAnalysesController : BaseApiController
    {
        private readonly IMediator _mediator;
        private readonly IPlantAnalysisAsyncService _asyncAnalysisService;
        private readonly ISubscriptionValidationService _subscriptionValidationService;

        public PlantAnalysesController(
            IMediator mediator, 
            IPlantAnalysisAsyncService asyncAnalysisService,
            ISubscriptionValidationService subscriptionValidationService)
        {
            _mediator = mediator;
            _asyncAnalysisService = asyncAnalysisService;
            _subscriptionValidationService = subscriptionValidationService;
        }

        /// <summary>
        /// Create a new plant analysis
        /// </summary>
        /// <param name="request">Plant analysis request with image base64</param>
        /// <returns>Plant analysis result</returns>
        [HttpPost("analyze")]
        [Authorize(Roles = "Farmer,Admin")] // Require authentication + role
        
        public async Task<IActionResult> Analyze([FromBody] PlantAnalysisRequestDto request)
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(new { 
                    success = false, 
                    message = "Validation failed", 
                    errors = errors 
                });
            }

            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            // Check subscription and quota limits
            var quotaValidation = await _subscriptionValidationService.ValidateAndLogUsageAsync(
                userId.Value, 
                HttpContext.Request.Path.Value ?? "/api/plantanalyses/analyze", 
                HttpContext.Request.Method);
                
            if (!quotaValidation.Success)
            {
                // Get detailed status for better error message
                var statusResult = await _subscriptionValidationService.CheckSubscriptionStatusAsync(userId.Value);
                
                return StatusCode(403, new
                {
                    success = false,
                    message = quotaValidation.Message,
                    subscriptionStatus = statusResult.Data,
                    upgradeMessage = statusResult.Data?.TierName == "Trial" 
                        ? "Upgrade to Small plan for 5 daily analyses at ₺99.99/month!"
                        : "Please upgrade your subscription plan."
                });
            }
            
            // Automatically determine farmer ID and sponsor details based on authenticated user
            string farmerId = null;
            string sponsorId = null;
            int? sponsorUserId = null;
            int? sponsorshipCodeId = null;
            
            var userRoles = HttpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value);
            if (userRoles.Contains("Farmer"))
            {
                // Farmer can only analyze for themselves - use their user ID as farmer ID
                farmerId = $"F{userId.Value:D3}"; // Format: F001, F002, etc.
                
                // Get detailed sponsorship information
                var sponsorshipDetails = await _subscriptionValidationService.GetSponsorshipDetailsAsync(userId.Value);
                if (sponsorshipDetails.Success && sponsorshipDetails.Data.HasSponsor)
                {
                    sponsorId = sponsorshipDetails.Data.SponsorId;              // S001, S002, etc.
                    sponsorUserId = sponsorshipDetails.Data.SponsorUserId;      // Actual sponsor user ID
                    sponsorshipCodeId = sponsorshipDetails.Data.SponsorshipCodeId; // SponsorshipCode table ID
                }
            }
            else if (userRoles.Contains("Admin"))
            {
                // Admin users: FarmerId can be provided via query parameter or default to their ID
                // For now, default to admin's formatted ID
                farmerId = $"F{userId.Value:D3}";
                // Admin users typically don't have sponsors
                sponsorId = null;
                sponsorUserId = null;
                sponsorshipCodeId = null;
            }
            
            // Map request to command
            var command = new CreatePlantAnalysisCommand
            {
                Image = request.Image,
                UserId = userId, // Always use authenticated user's ID
                FarmerId = farmerId, // Automatically determined
                SponsorId = sponsorId, // Automatically determined
                SponsorUserId = sponsorUserId, // Actual sponsor user ID
                SponsorshipCodeId = sponsorshipCodeId, // SponsorshipCode table ID
                FieldId = request.FieldId,
                CropType = request.CropType,
                Location = request.Location,
                GpsCoordinates = request.GpsCoordinates,
                Altitude = request.Altitude,
                PlantingDate = request.PlantingDate,
                ExpectedHarvestDate = request.ExpectedHarvestDate,
                LastFertilization = request.LastFertilization,
                LastIrrigation = request.LastIrrigation,
                PreviousTreatments = request.PreviousTreatments,
                SoilType = request.SoilType,
                Temperature = request.Temperature,
                Humidity = request.Humidity,
                WeatherConditions = request.WeatherConditions,
                UrgencyLevel = request.UrgencyLevel,
                Notes = request.Notes,
                ContactInfo = request.ContactInfo,
                AdditionalInfo = request.AdditionalInfo
            };

            var result = await _mediator.Send(command);
            
            if (result.Success)
            {
                // Increment usage counter after successful analysis
                await _subscriptionValidationService.IncrementUsageAsync(userId.Value, result.Data?.Id);
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        /// <summary>
        /// Queue a new plant analysis for async processing
        /// </summary>
        /// <param name="request">Plant analysis request with image base64</param>
        /// <returns>Analysis ID for tracking</returns>
        [HttpPost("analyze-async")]
        [Authorize(Roles = "Farmer,Admin")] // Require authentication + role
        
        public async Task<IActionResult> AnalyzeAsync([FromBody] PlantAnalysisRequestDto request)
        {
            try
            {
                // Validate model
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(new { 
                        success = false, 
                        message = "Validation failed", 
                        errors = errors 
                    });
                }

                // Check if queue is healthy
                var isQueueHealthy = await _asyncAnalysisService.IsQueueHealthyAsync();
                if (!isQueueHealthy)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                    {
                        success = false,
                        message = "Message queue service is currently unavailable. Please try again later."
                    });
                }

                // Get authenticated user ID and add to request
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized();

                // Check subscription and quota limits
                var quotaValidation = await _subscriptionValidationService.ValidateAndLogUsageAsync(
                    userId.Value, 
                    HttpContext.Request.Path.Value ?? "/api/plantanalyses/analyze-async", 
                    HttpContext.Request.Method);
                    
                if (!quotaValidation.Success)
                {
                    // Get detailed status for better error message
                    var statusResult = await _subscriptionValidationService.CheckSubscriptionStatusAsync(userId.Value);
                    
                    return StatusCode(403, new
                    {
                        success = false,
                        message = quotaValidation.Message,
                        subscriptionStatus = statusResult.Data,
                        upgradeMessage = statusResult.Data?.TierName == "Trial" 
                            ? "Upgrade to Small plan for 5 daily analyses at ₺99.99/month!"
                            : "Please upgrade your subscription plan."
                    });
                }
                
                // Automatically determine farmer ID and sponsor details based on authenticated user
                string farmerId = null;
                string sponsorId = null;
                int? sponsorUserId = null;
                int? sponsorshipCodeId = null;
                
                var userRoles = HttpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value);
                if (userRoles.Contains("Farmer"))
                {
                    // Farmer can only analyze for themselves - use their user ID as farmer ID
                    farmerId = $"F{userId.Value:D3}"; // Format: F001, F002, etc.
                    
                    // Get detailed sponsorship information
                    var sponsorshipDetails = await _subscriptionValidationService.GetSponsorshipDetailsAsync(userId.Value);
                    if (sponsorshipDetails.Success && sponsorshipDetails.Data.HasSponsor)
                    {
                        sponsorId = sponsorshipDetails.Data.SponsorId;              // S001, S002, etc.
                        sponsorUserId = sponsorshipDetails.Data.SponsorUserId;      // Actual sponsor user ID
                        sponsorshipCodeId = sponsorshipDetails.Data.SponsorshipCodeId; // SponsorshipCode table ID
                    }
                }
                else if (userRoles.Contains("Admin"))
                {
                    // Admin users: default to their formatted ID
                    farmerId = $"F{userId.Value:D3}";
                    // Admin users typically don't have sponsors
                    sponsorId = null;
                    sponsorUserId = null;
                    sponsorshipCodeId = null;
                }
                
                // Set automatically determined values
                request.UserId = userId; // Set authenticated user's ID
                request.FarmerId = farmerId; // Automatically determined
                request.SponsorId = sponsorId; // Automatically determined
                request.SponsorUserId = sponsorUserId; // Actual sponsor user ID
                request.SponsorshipCodeId = sponsorshipCodeId; // SponsorshipCode table ID

                // Queue the analysis
                var analysisId = await _asyncAnalysisService.QueuePlantAnalysisAsync(request);

                // Increment usage counter after successful queueing
                await _subscriptionValidationService.IncrementUsageAsync(userId.Value);

                return Accepted(new
                {
                    success = true,
                    message = "Plant analysis has been queued for processing",
                    analysis_id = analysisId,
                    estimated_processing_time = "2-5 minutes",
                    status_check_endpoint = $"/api/plantanalyses/status/{analysisId}",
                    notification_info = "You will receive a notification when analysis is complete"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Failed to queue plant analysis: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get plant analysis by ID
        /// </summary>
        /// <param name="id">Analysis ID</param>
        /// <returns>Plant analysis details</returns>
        [HttpGet("{id}")]
        [Authorize] // Require authentication
        public async Task<IActionResult> GetById(int id)
        {
            var query = new GetPlantAnalysisQuery { Id = id };
            var result = await _mediator.Send(query);
            
            if (!result.Success)
                return NotFound(result);

            // Check authorization: Users can only see their own analyses, unless they're admin
            var userId = GetUserId();
            var isAdmin = User.IsInRole("Admin");
            var isSponsor = User.IsInRole("Sponsor");
            
            // Admins can see all analyses
            if (isAdmin)
                return Ok(result);
            
            // Sponsors can see analyses they sponsor
            if (isSponsor && result.Data.SponsorId == User.FindFirst("SponsorId")?.Value)
                return Ok(result);
            
            // Farmers can only see their own analyses
            if (result.Data.UserId == userId)
                return Ok(result);
            
            return Forbid("You don't have permission to view this analysis");
        }

        /// <summary>
        /// Get all plant analyses for current user (Farmers) - Full Detail
        /// </summary>
        /// <returns>List of plant analyses with full details</returns>
        [HttpGet("my-analyses")]
        [Authorize]
        [ProducesResponseType(typeof(IDataResult<List<PlantAnalysisResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyAnalyses()
        {
            var userId = GetUserId();
            
            var query = new GetPlantAnalysesQuery { UserId = userId };
            var result = await _mediator.Send(query);
            
            return Ok(result);
        }

        /// <summary>
        /// Get paginated plant analysis history for mobile app (Farmers)
        /// Lightweight response optimized for mobile listing with filtering and pagination
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 20, max: 50)</param>
        /// <param name="status">Filter by status: Completed, Processing, Failed</param>
        /// <param name="fromDate">Filter from date (YYYY-MM-DD)</param>
        /// <param name="toDate">Filter to date (YYYY-MM-DD)</param>
        /// <param name="cropType">Filter by crop type</param>
        /// <returns>Paginated list of plant analyses</returns>
        [HttpGet("list")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(IDataResult<PlantAnalysisListResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAnalysesList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string cropType = null)
        {
            var userId = GetUserId();
            
            // Validate and limit page size for performance
            if (pageSize > 50) pageSize = 50;
            if (pageSize < 1) pageSize = 20;
            if (page < 1) page = 1;

            var query = new GetPlantAnalysesForFarmerQuery 
            { 
                UserId = userId.Value,
                Page = page,
                PageSize = pageSize,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                CropType = cropType
            };
            
            var result = await _mediator.Send(query);
            
            return Ok(result);
        }

        /// <summary>
        /// Get all plant analyses sponsored by current sponsor
        /// </summary>
        /// <returns>List of sponsored plant analyses</returns>
        [HttpGet("sponsored-analyses")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IDataResult<List<PlantAnalysisResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSponsoredAnalyses()
        {
            // Get sponsor ID from claims or user profile
            var sponsorId = User.FindFirst("SponsorId")?.Value ?? User.Identity?.Name;
            
            var query = new GetPlantAnalysesQuery { SponsorId = sponsorId };
            var result = await _mediator.Send(query);
            
            return Ok(result);
        }

        /// <summary>
        /// Get all plant analyses (Admin only)
        /// </summary>
        /// <returns>List of all plant analyses</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IDataResult<List<PlantAnalysisResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var query = new GetPlantAnalysesQuery();
            var result = await _mediator.Send(query);
            
            return Ok(result);
        }

        /// <summary>
        /// Get plant analysis image by analysis ID
        /// </summary>
        /// <param name="id">Analysis ID</param>
        /// <returns>Image file</returns>
        [HttpGet("{id}/image")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetImage(int id)
        {
            // Get analysis to check if exists and get image path
            var query = new GetPlantAnalysisQuery { Id = id };
            var analysisResult = await _mediator.Send(query);
            
            if (!analysisResult.Success)
                return NotFound("Analysis not found");
            
            var imagePath = analysisResult.Data.ImagePath;
            if (string.IsNullOrEmpty(imagePath))
                return NotFound("Image not found");
            
            // Convert relative path to absolute path
            var fullPath = Path.Combine("wwwroot", imagePath.TrimStart('/'));
            
            if (!System.IO.File.Exists(fullPath))
                return NotFound("Image file not found");
            
            // Detect MIME type from file extension
            var mimeType = GetMimeTypeFromExtension(Path.GetExtension(fullPath));
            
            var imageBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(imageBytes, mimeType);
        }

        private string GetMimeTypeFromExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                ".tiff" or ".tif" => "image/tiff",
                _ => "image/jpeg" // default
            };
        }

        private int? GetUserId()
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (int.TryParse(userIdClaim, out var userId))
                return userId;
            
            return null;
        }
    }
}