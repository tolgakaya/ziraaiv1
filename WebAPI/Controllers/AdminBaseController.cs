using Business.Services.AdminAudit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebAPI.Middleware;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Base controller for admin-specific operations
    /// Provides helper methods for OBO context, audit logging, and admin authorization
    /// All admin controllers should inherit from this
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("api/v1/admin/[controller]")]
    [ApiController]
    public class AdminBaseController : BaseApiController
    {
        protected readonly IAdminAuditService _adminAuditService;

        public AdminBaseController(IAdminAuditService adminAuditService)
        {
            _adminAuditService = adminAuditService;
        }

        #region User Context Methods

        /// <summary>
        /// Get the current admin user ID from JWT claims
        /// </summary>
        /// <returns>Admin user ID</returns>
        [NonAction]
        protected int GetAdminUserId()
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing user ID claim");
            }
            return userId;
        }

        /// <summary>
        /// Get the current admin user's full name from JWT claims
        /// </summary>
        [NonAction]
        protected string GetAdminUserName()
        {
            return User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown Admin";
        }

        /// <summary>
        /// Get the current admin user's email from JWT claims
        /// </summary>
        [NonAction]
        protected string GetAdminUserEmail()
        {
            return User?.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@ziraai.com";
        }

        /// <summary>
        /// Check if current user has Admin role
        /// </summary>
        [NonAction]
        protected bool IsAdmin()
        {
            return User?.IsInRole("Admin") ?? false;
        }

        /// <summary>
        /// Get all roles for the current user
        /// </summary>
        [NonAction]
        protected string[] GetUserRoles()
        {
            return User?.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToArray() ?? Array.Empty<string>();
        }

        #endregion

        #region On-Behalf-Of Context Methods

        /// <summary>
        /// Check if the current request is an on-behalf-of operation
        /// </summary>
        [NonAction]
        protected bool IsOnBehalfOfOperation()
        {
            return HttpContext.Items.ContainsKey(OnBehalfOfMiddleware.OBO_CONTEXT_KEY);
        }

        /// <summary>
        /// Get the on-behalf-of context for the current request
        /// Returns null if not an OBO operation
        /// </summary>
        [NonAction]
        protected OnBehalfOfContext GetOnBehalfOfContext()
        {
            if (HttpContext.Items.TryGetValue(OnBehalfOfMiddleware.OBO_CONTEXT_KEY, out var context))
            {
                return context as OnBehalfOfContext;
            }
            return null;
        }

        /// <summary>
        /// Get the target user ID for OBO operations
        /// Returns null if not an OBO operation
        /// </summary>
        [NonAction]
        protected int? GetTargetUserId()
        {
            var oboContext = GetOnBehalfOfContext();
            return oboContext?.TargetUserId;
        }

        #endregion

        #region Audit Logging Helper Methods

        /// <summary>
        /// Log a simple admin action
        /// </summary>
        [NonAction]
        protected async Task LogAdminActionAsync(string action, string reason = null)
        {
            try
            {
                var adminUserId = GetAdminUserId();
                await _adminAuditService.LogSimpleActionAsync(
                    adminUserId: adminUserId,
                    action: action,
                    httpContext: HttpContext,
                    reason: reason);
            }
            catch (Exception ex)
            {
                // Log but don't fail the request
                Console.WriteLine($"Failed to log admin action: {ex.Message}");
            }
        }

        /// <summary>
        /// Log a user management operation
        /// </summary>
        [NonAction]
        protected async Task LogUserManagementAsync(
            string action, 
            int targetUserId, 
            string reason,
            object beforeState = null,
            object afterState = null)
        {
            try
            {
                var adminUserId = GetAdminUserId();
                await _adminAuditService.LogUserManagementAsync(
                    adminUserId: adminUserId,
                    action: action,
                    httpContext: HttpContext,
                    targetUserId: targetUserId,
                    reason: reason,
                    beforeState: beforeState,
                    afterState: afterState);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log user management action: {ex.Message}");
            }
        }

        /// <summary>
        /// Log a subscription management operation
        /// </summary>
        [NonAction]
        protected async Task LogSubscriptionManagementAsync(
            string action,
            int targetUserId,
            int subscriptionId,
            string reason,
            object beforeState = null,
            object afterState = null)
        {
            try
            {
                var adminUserId = GetAdminUserId();
                await _adminAuditService.LogSubscriptionManagementAsync(
                    adminUserId: adminUserId,
                    action: action,
                    httpContext: HttpContext,
                    targetUserId: targetUserId,
                    subscriptionId: subscriptionId,
                    reason: reason,
                    beforeState: beforeState,
                    afterState: afterState);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log subscription management action: {ex.Message}");
            }
        }

        /// <summary>
        /// Log a sponsorship management operation
        /// </summary>
        [NonAction]
        protected async Task LogSponsorshipManagementAsync(
            string action,
            int? targetUserId,
            int entityId,
            string reason,
            object beforeState = null,
            object afterState = null)
        {
            try
            {
                var adminUserId = GetAdminUserId();
                await _adminAuditService.LogSponsorshipManagementAsync(
                    adminUserId: adminUserId,
                    action: action,
                    httpContext: HttpContext,
                    targetUserId: targetUserId,
                    entityId: entityId,
                    reason: reason,
                    beforeState: beforeState,
                    afterState: afterState);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log sponsorship management action: {ex.Message}");
            }
        }

        #endregion

        #region Validation Helper Methods

        /// <summary>
        /// Validate that a reason is provided and has minimum length
        /// Used for operations that require mandatory reasons
        /// </summary>
        [NonAction]
        protected IActionResult ValidateReason(string reason, int minLength = 10)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest<object>(
                    "Validation failed",
                    "Reason is required for this operation",
                    null);
            }

            if (reason.Length < minLength)
            {
                return BadRequest<object>(
                    "Validation failed",
                    $"Reason must be at least {minLength} characters",
                    null);
            }

            return null;
        }

        /// <summary>
        /// Validate that target user ID is provided for OBO operations
        /// </summary>
        [NonAction]
        protected IActionResult ValidateTargetUser(int? targetUserId)
        {
            if (!targetUserId.HasValue || targetUserId.Value <= 0)
            {
                return BadRequest<object>(
                    "Validation failed",
                    "Valid target user ID is required",
                    null);
            }

            return null;
        }

        #endregion

        #region IP Address Helper

        /// <summary>
        /// Get the client IP address from HTTP context
        /// Handles X-Forwarded-For for proxies/load balancers
        /// </summary>
        [NonAction]
        protected string GetClientIpAddress()
        {
            // Check X-Forwarded-For header (for proxies/load balancers)
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                if (ips.Length > 0)
                    return ips[0].Trim();
            }

            // Fallback to remote IP address
            return HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        #endregion
    }
}
