using Business.Handlers.PlantAnalyses.Commands;
using Business.Handlers.PlantAnalyses.Queries;
using Business.Services.PlantAnalysis;
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
    [Route("api/[controller]")]
    [ApiController]
    public class PlantAnalysesController : BaseApiController
    {
        private readonly IMediator _mediator;
        private readonly IPlantAnalysisAsyncService _asyncAnalysisService;

        public PlantAnalysesController(IMediator mediator, IPlantAnalysisAsyncService asyncAnalysisService)
        {
            _mediator = mediator;
            _asyncAnalysisService = asyncAnalysisService;
        }

        /// <summary>
        /// Create a new plant analysis
        /// </summary>
        /// <param name="request">Plant analysis request with image base64</param>
        /// <returns>Plant analysis result</returns>
        [HttpPost("analyze")]
        [ProducesResponseType(typeof(IDataResult<PlantAnalysisResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
            
            // Map request to command
            var command = new CreatePlantAnalysisCommand
            {
                Image = request.Image,
                UserId = request.UserId ?? userId,
                FarmerId = request.FarmerId,
                SponsorId = request.SponsorId,
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
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Queue a new plant analysis for async processing
        /// </summary>
        /// <param name="request">Plant analysis request with image base64</param>
        /// <returns>Analysis ID for tracking</returns>
        [HttpPost("analyze-async")]
        [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
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

                // Queue the analysis
                var analysisId = await _asyncAnalysisService.QueuePlantAnalysisAsync(request);

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
        [ProducesResponseType(typeof(IDataResult<PlantAnalysisResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var query = new GetPlantAnalysisQuery { Id = id };
            var result = await _mediator.Send(query);
            
            if (result.Success)
                return Ok(result);
            
            return NotFound(result);
        }

        /// <summary>
        /// Get all plant analyses for current user
        /// </summary>
        /// <returns>List of plant analyses</returns>
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