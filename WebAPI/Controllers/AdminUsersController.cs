using Business.Handlers.AdminUsers.Commands;
using Business.Handlers.AdminUsers.Queries;
using Business.Services.AdminAudit;
using Entities.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Admin-only controller for user management operations
    /// Provides comprehensive user CRUD, search, deactivation, and password reset
    /// All operations are logged to audit trail
    /// </summary>
    public class AdminUsersController : AdminBaseController
    {
        public AdminUsersController(IAdminAuditService adminAuditService) 
            : base(adminAuditService)
        {
        }

        /// <summary>
        /// Get all users with optional pagination and filtering
        /// GET /api/v1/admin/adminusers
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50, max: 100)</param>
        /// <param name="isActive">Filter by active status (optional)</param>
        /// <param name="role">Filter by role (optional)</param>
        /// <param name="searchTerm">Search in name, email, phone (optional)</param>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] bool? isActive = null,
            [FromQuery] string role = null,
            [FromQuery] string searchTerm = null)
        {
            // Validate pagination
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100;

            var result = await Mediator.Send(new GetAllUsersQuery
            {
                Page = page,
                PageSize = pageSize,
                IsActive = isActive,
                Role = role,
                SearchTerm = searchTerm
            });

            await LogAdminActionAsync("GET_ALL_USERS", $"Retrieved users list (page: {page}, pageSize: {pageSize})");

            return GetResponse(result);
        }

        /// <summary>
        /// Get detailed user information by ID
        /// GET /api/v1/admin/adminusers/{id}
        /// </summary>
        /// <param name="id">User ID</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await Mediator.Send(new GetUserByIdQuery { UserId = id });

            if (result.Success)
            {
                await LogAdminActionAsync("GET_USER_DETAILS", $"Retrieved details for user ID: {id}");
            }

            return GetResponse(result);
        }

        /// <summary>
        /// Advanced search for users with multiple filter criteria
        /// POST /api/v1/admin/adminusers/search
        /// </summary>
        /// <param name="searchQuery">Search criteria</param>
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchUsersQuery searchQuery)
        {
            var result = await Mediator.Send(searchQuery);

            await LogAdminActionAsync("SEARCH_USERS", $"Search performed with criteria: {searchQuery.SearchTerm}");

            return GetResponse(result);
        }

        /// <summary>
        /// Deactivate a user account
        /// POST /api/v1/admin/adminusers/{id}/deactivate
        /// </summary>
        /// <param name="id">User ID to deactivate</param>
        /// <param name="dto">Deactivation details (mandatory reason)</param>
        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(int id, [FromBody] DeactivateUserDto dto)
        {
            // Validate reason
            var reasonValidation = ValidateReason(dto.Reason);
            if (reasonValidation != null)
                return reasonValidation;

            var adminUserId = GetAdminUserId();

            var result = await Mediator.Send(new DeactivateUserCommand
            {
                UserId = id,
                AdminUserId = adminUserId,
                Reason = dto.Reason,
                HttpContext = HttpContext
            });

            return GetResponseOnlyResult(result);
        }

        /// <summary>
        /// Reactivate a deactivated user account
        /// POST /api/v1/admin/adminusers/{id}/reactivate
        /// </summary>
        /// <param name="id">User ID to reactivate</param>
        /// <param name="dto">Reactivation details (reason required)</param>
        [HttpPost("{id}/reactivate")]
        public async Task<IActionResult> Reactivate(int id, [FromBody] ReactivateUserRequest dto)
        {
            // Validate reason
            var reasonValidation = ValidateReason(dto.Reason);
            if (reasonValidation != null)
                return reasonValidation;

            var adminUserId = GetAdminUserId();

            var result = await Mediator.Send(new ReactivateUserCommand
            {
                UserId = id,
                AdminUserId = adminUserId,
                Reason = dto.Reason,
                HttpContext = HttpContext
            });

            return GetResponseOnlyResult(result);
        }

        /// <summary>
        /// Reset a user's password to a temporary password
        /// POST /api/v1/admin/adminusers/{id}/reset-password
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="dto">Password reset details (reason required)</param>
        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest dto)
        {
            // Validate reason
            var reasonValidation = ValidateReason(dto.Reason);
            if (reasonValidation != null)
                return reasonValidation;

            var adminUserId = GetAdminUserId();

            var result = await Mediator.Send(new ResetUserPasswordCommand
            {
                UserId = id,
                AdminUserId = adminUserId,
                Reason = dto.Reason,
                NewPassword = dto.NewPassword,
                HttpContext = HttpContext
            });

            return GetResponse(result);
        }

        /// <summary>
        /// Get user's activity history (analyses, subscriptions, etc.)
        /// GET /api/v1/admin/adminusers/{id}/activity
        /// </summary>
        /// <param name="id">User ID</param>
        [HttpGet("{id}/activity")]
        public async Task<IActionResult> GetUserActivity(int id)
        {
            // TODO: Implement in Phase 2.3 (Analytics & Reporting)
            // This will retrieve:
            // - Recent plant analyses
            // - Subscription history
            // - Login history
            // - API usage statistics

            await LogAdminActionAsync("GET_USER_ACTIVITY", $"Retrieved activity for user ID: {id}");

            return Success(
                "User activity endpoint",
                "This endpoint will be implemented in Phase 2.3",
                new { userId = id, note = "Coming in Phase 2.3 - Analytics & Reporting" });
        }

        /// <summary>
        /// Get audit trail for a specific user (all admin operations)
        /// GET /api/v1/admin/adminusers/{id}/audit-trail
        /// </summary>
        /// <param name="id">User ID</param>
        [HttpGet("{id}/audit-trail")]
        public async Task<IActionResult> GetUserAuditTrail(int id)
        {
            // TODO: Implement using AdminOperationLogRepository
            // This will retrieve all admin operations performed on this user

            await LogAdminActionAsync("GET_USER_AUDIT_TRAIL", $"Retrieved audit trail for user ID: {id}");

            return Success(
                "User audit trail endpoint",
                "This endpoint will be implemented in Phase 2.3",
                new { userId = id, note = "Coming in Phase 2.3 - Analytics & Reporting" });
        }
    }

    #region Request DTOs

    /// <summary>
    /// Request DTO for user reactivation
    /// </summary>
    public class ReactivateUserRequest
    {
        public string Reason { get; set; }
    }

    /// <summary>
    /// Request DTO for password reset
    /// </summary>
    public class ResetPasswordRequest
    {
        public string Reason { get; set; }
        public string NewPassword { get; set; }  // Optional: If not provided, generates random password
    }

    #endregion
}
