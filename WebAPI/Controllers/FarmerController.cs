using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Business.Handlers.Farmers.Commands;
using Business.Handlers.Farmers.Queries;
using Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Farmer-specific profile management endpoints
    /// Secure implementation using JWT-based user identification
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class FarmerController : BaseApiController
    {
        /// <summary>
        /// Get current farmer's profile
        /// Uses JWT token to identify user - secure by design
        /// </summary>
        /// <returns>Farmer profile data without sensitive information</returns>
        [Authorize(Roles = "Farmer,Admin")]
        [HttpGet("profile")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FarmerProfileDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            // Get userId from JWT token - cannot be manipulated by user
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı token'ı." });
            }

            var result = await Mediator.Send(new GetFarmerProfileQuery { UserId = userId });

            return result.Success 
                ? Ok(result) 
                : NotFound(result);
        }

        /// <summary>
        /// Update current farmer's profile
        /// Uses JWT token to identify user - prevents unauthorized profile updates
        /// </summary>
        /// <param name="dto">Profile update data (userId comes from JWT, not from request)</param>
        /// <returns>Success or error result</returns>
        [Authorize(Roles = "Farmer,Admin")]
        [HttpPut("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateFarmerProfileDto dto)
        {
            // Get userId from JWT token - secure by design
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı token'ı." });
            }

            // Map DTO to command with JWT userId
            var command = new UpdateFarmerProfileCommand
            {
                UserId = userId, // From JWT token, not user input
                FullName = dto.FullName,
                Email = dto.Email,
                MobilePhones = dto.MobilePhones,
                BirthDate = dto.BirthDate,
                Gender = dto.Gender,
                Address = dto.Address,
                Notes = dto.Notes
            };

            var result = await Mediator.Send(command);

            return result.Success 
                ? Ok(result) 
                : BadRequest(result);
        }
    }
}
