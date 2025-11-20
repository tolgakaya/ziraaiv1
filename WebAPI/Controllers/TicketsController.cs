using System.Security.Claims;
using System.Threading.Tasks;
using Business.Handlers.Tickets.Commands;
using Business.Handlers.Tickets.Queries;
using Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Ticket management endpoints for Farmers and Sponsors
    /// Secure implementation using JWT-based user identification
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class TicketsController : BaseApiController
    {
        /// <summary>
        /// Create a new support ticket
        /// </summary>
        /// <param name="dto">Ticket details</param>
        /// <returns>Created ticket ID</returns>
        [Authorize(Roles = "Farmer,Sponsor")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı token'ı." });
            }

            // Get user role from claims
            var userRole = User.IsInRole("Farmer") ? "Farmer" : "Sponsor";

            var command = new CreateTicketCommand
            {
                UserId = userId,
                UserRole = userRole,
                Subject = dto.Subject,
                Description = dto.Description,
                Category = dto.Category,
                Priority = dto.Priority
            };

            var result = await Mediator.Send(command);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Get user's tickets with optional filters
        /// </summary>
        /// <param name="status">Optional status filter (Open, InProgress, Resolved, Closed)</param>
        /// <param name="category">Optional category filter (Technical, Billing, Account, General)</param>
        /// <returns>List of user's tickets</returns>
        [Authorize(Roles = "Farmer,Sponsor")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TicketListResponseDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyTickets([FromQuery] string status = null, [FromQuery] string category = null)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı token'ı." });
            }

            var query = new GetMyTicketsQuery
            {
                UserId = userId,
                Status = status,
                Category = category
            };

            var result = await Mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get ticket detail with messages
        /// </summary>
        /// <param name="ticketId">Ticket ID</param>
        /// <returns>Ticket detail with messages</returns>
        [Authorize(Roles = "Farmer,Sponsor")]
        [HttpGet("{ticketId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TicketDetailDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTicketDetail(int ticketId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı token'ı." });
            }

            var query = new GetTicketDetailQuery
            {
                UserId = userId,
                TicketId = ticketId
            };

            var result = await Mediator.Send(query);
            return result.Success ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Add a message to user's own ticket
        /// </summary>
        /// <param name="ticketId">Ticket ID</param>
        /// <param name="dto">Message content</param>
        /// <returns>Success or error result</returns>
        [Authorize(Roles = "Farmer,Sponsor")]
        [HttpPost("{ticketId}/messages")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddMessage(int ticketId, [FromBody] AddTicketMessageDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı token'ı." });
            }

            var command = new AddTicketMessageCommand
            {
                UserId = userId,
                TicketId = ticketId,
                Message = dto.Message
            };

            var result = await Mediator.Send(command);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Close user's own ticket
        /// </summary>
        /// <param name="ticketId">Ticket ID</param>
        /// <returns>Success or error result</returns>
        [Authorize(Roles = "Farmer,Sponsor")]
        [HttpPost("{ticketId}/close")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CloseTicket(int ticketId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı token'ı." });
            }

            var command = new CloseTicketCommand
            {
                UserId = userId,
                TicketId = ticketId
            };

            var result = await Mediator.Send(command);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Rate ticket resolution (1-5 stars)
        /// </summary>
        /// <param name="ticketId">Ticket ID</param>
        /// <param name="dto">Rating details</param>
        /// <returns>Success or error result</returns>
        [Authorize(Roles = "Farmer,Sponsor")]
        [HttpPost("{ticketId}/rate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RateResolution(int ticketId, [FromBody] RateTicketResolutionDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı token'ı." });
            }

            var command = new RateTicketResolutionCommand
            {
                UserId = userId,
                TicketId = ticketId,
                Rating = dto.Rating,
                Feedback = dto.Feedback
            };

            var result = await Mediator.Send(command);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
