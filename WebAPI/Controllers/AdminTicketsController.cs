using System.Threading.Tasks;
using Business.Handlers.Tickets.Commands;
using Business.Handlers.Tickets.Queries;
using Entities.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Admin ticket management endpoints
    /// Extends AdminBaseController for admin-specific features
    /// </summary>
    public class AdminTicketsController : AdminBaseController
    {
        /// <summary>
        /// Get all tickets with optional filters
        /// </summary>
        /// <param name="status">Optional status filter (Open, InProgress, Resolved, Closed)</param>
        /// <param name="category">Optional category filter (Technical, Billing, Account, General)</param>
        /// <param name="priority">Optional priority filter (Low, Normal, High)</param>
        /// <returns>List of all tickets</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AdminTicketListResponseDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllTickets(
            [FromQuery] string status = null,
            [FromQuery] string category = null,
            [FromQuery] string priority = null)
        {
            var query = new GetAllTicketsAsAdminQuery
            {
                Status = status,
                Category = category,
                Priority = priority
            };

            var result = await Mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get ticket statistics
        /// </summary>
        /// <returns>Ticket count by status</returns>
        [HttpGet("stats")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TicketStatsDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTicketStats()
        {
            var result = await Mediator.Send(new GetTicketStatsQuery());
            return Ok(result);
        }

        /// <summary>
        /// Get ticket detail with all messages (including internal notes)
        /// </summary>
        /// <param name="ticketId">Ticket ID</param>
        /// <returns>Ticket detail with all messages</returns>
        [HttpGet("{ticketId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AdminTicketDetailDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTicketDetail(int ticketId)
        {
            var query = new GetTicketDetailAsAdminQuery
            {
                TicketId = ticketId
            };

            var result = await Mediator.Send(query);
            return result.Success ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Assign ticket to admin user
        /// </summary>
        /// <param name="ticketId">Ticket ID</param>
        /// <param name="assignedToUserId">Admin user ID to assign (null to unassign)</param>
        /// <returns>Success or error result</returns>
        [HttpPost("{ticketId}/assign")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignTicket(int ticketId, [FromQuery] int? assignedToUserId = null)
        {
            var command = new AssignTicketCommand
            {
                TicketId = ticketId,
                AssignedToUserId = assignedToUserId
            };

            var result = await Mediator.Send(command);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Respond to a ticket
        /// </summary>
        /// <param name="ticketId">Ticket ID</param>
        /// <param name="dto">Response details</param>
        /// <returns>Success or error result</returns>
        [HttpPost("{ticketId}/respond")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RespondToTicket(int ticketId, [FromBody] AdminRespondTicketDto dto)
        {
            var command = new AdminRespondTicketCommand
            {
                AdminUserId = AdminUserId,
                TicketId = ticketId,
                Message = dto.Message,
                IsInternal = dto.IsInternal
            };

            var result = await Mediator.Send(command);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Update ticket status
        /// </summary>
        /// <param name="ticketId">Ticket ID</param>
        /// <param name="dto">Status update details</param>
        /// <returns>Success or error result</returns>
        [HttpPut("{ticketId}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTicketStatus(int ticketId, [FromBody] UpdateTicketStatusDto dto)
        {
            var command = new UpdateTicketStatusCommand
            {
                TicketId = ticketId,
                Status = dto.Status,
                ResolutionNotes = dto.ResolutionNotes
            };

            var result = await Mediator.Send(command);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
