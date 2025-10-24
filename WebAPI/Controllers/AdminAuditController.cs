using Business.Handlers.AdminAudit.Queries;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Admin controller for audit log management
    /// Provides endpoints for viewing and tracking admin operations
    /// </summary>
    [Route("api/admin/audit")]
    public class AdminAuditController : AdminBaseController
    {
        /// <summary>
        /// Get all audit logs with pagination and filtering
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <param name="action">Filter by action (optional)</param>
        /// <param name="entityType">Filter by entity type (optional)</param>
        /// <param name="isOnBehalfOf">Filter by on-behalf-of status (optional)</param>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet]
        public async Task<IActionResult> GetAllAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string action = null,
            [FromQuery] string entityType = null,
            [FromQuery] bool? isOnBehalfOf = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = new GetAllAuditLogsQuery
            {
                Page = page,
                PageSize = pageSize,
                Action = action,
                EntityType = entityType,
                IsOnBehalfOf = isOnBehalfOf,
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Get audit logs by admin user
        /// </summary>
        /// <param name="adminUserId">Admin user ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("admin/{adminUserId}")]
        public async Task<IActionResult> GetAuditLogsByAdmin(
            int adminUserId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = new GetAuditLogsByAdminQuery
            {
                AdminUserId = adminUserId,
                Page = page,
                PageSize = pageSize,
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Get audit logs by target user
        /// </summary>
        /// <param name="targetUserId">Target user ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("target/{targetUserId}")]
        public async Task<IActionResult> GetAuditLogsByTarget(
            int targetUserId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = new GetAuditLogsByTargetQuery
            {
                TargetUserId = targetUserId,
                Page = page,
                PageSize = pageSize,
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }

        /// <summary>
        /// Get on-behalf-of operation logs
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <param name="startDate">Start date for filtering (optional)</param>
        /// <param name="endDate">End date for filtering (optional)</param>
        [HttpGet("on-behalf-of")]
        public async Task<IActionResult> GetOnBehalfOfLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = new GetAllAuditLogsQuery
            {
                Page = page,
                PageSize = pageSize,
                IsOnBehalfOf = true,
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await Mediator.Send(query);
            return GetResponse(result);
        }
    }
}
