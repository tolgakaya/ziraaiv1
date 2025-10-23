using Business.Handlers.AdminUsers.Commands;
using Business.Handlers.AdminUsers.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Admin controller for user management operations
    /// Provides endpoints for viewing, searching, and managing user accounts
    /// </summary>
    [Route("api/admin/users")]
    public class AdminUsersController : AdminBaseController
    {
        /// <summary>
        /// Get all users with pagination and filtering
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <param name="isActive">Filter by active status (optional)</param>
        /// <param name="status">Filter by status (optional)</param>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] bool? isActive = null,
            [FromQuery] string status = null)
        {
            var query = new GetAllUsersQuery
            {
                Page = page,
                PageSize = pageSize,
                IsActive = isActive,
                Status = status
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="userId">User ID</param>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            var query = new GetUserByIdQuery { UserId = userId };
            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Search users by email, name, or mobile phone
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers(
            [FromQuery] string searchTerm,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = new SearchUsersQuery
            {
                SearchTerm = searchTerm,
                Page = page,
                PageSize = pageSize
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Deactivate a user account
        /// </summary>
        /// <param name="userId">User ID to deactivate</param>
        /// <param name="reason">Reason for deactivation</param>
        [HttpPost("{userId}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int userId, [FromBody] DeactivateUserRequest request)
        {
            var command = new DeactivateUserCommand
            {
                UserId = userId,
                AdminUserId = AdminUserId,
                Reason = request?.Reason,
                IpAddress = ClientIpAddress,
                UserAgent = UserAgent,
                RequestPath = RequestPath
            };

            var result = await Mediator.Send(command);
            return GetResponseOnlyResult(result);
        }

        /// <summary>
        /// Reactivate a deactivated user account
        /// </summary>
        /// <param name="userId">User ID to reactivate</param>
        /// <param name="reason">Reason for reactivation</param>
        [HttpPost("{userId}/reactivate")]
        public async Task<IActionResult> ReactivateUser(int userId, [FromBody] ReactivateUserRequest request)
        {
            var command = new ReactivateUserCommand
            {
                UserId = userId,
                AdminUserId = AdminUserId,
                Reason = request?.Reason,
                IpAddress = ClientIpAddress,
                UserAgent = UserAgent,
                RequestPath = RequestPath
            };

            var result = await Mediator.Send(command);
            return GetResponseOnlyResult(result);
        }
    }

    /// <summary>
    /// Request model for deactivating a user
    /// </summary>
    public class DeactivateUserRequest
    {
        /// <summary>
        /// Reason for deactivation
        /// </summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// Request model for reactivating a user
    /// </summary>
    public class ReactivateUserRequest
    {
        /// <summary>
        /// Reason for reactivation
        /// </summary>
        public string Reason { get; set; }
    }
}
