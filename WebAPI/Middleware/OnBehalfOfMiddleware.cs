using Business.Services.AdminAudit;
using Core.Entities.Concrete;
using DataAccess.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebAPI.Middleware
{
    /// <summary>
    /// Middleware to handle On-Behalf-Of (OBO) operations for admin users
    /// Intercepts requests with X-On-Behalf-Of-User header and validates admin permissions
    /// Must be registered AFTER authentication middleware in Program.cs
    /// </summary>
    public class OnBehalfOfMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<OnBehalfOfMiddleware> _logger;

        // Header name for OBO operations
        private const string OBO_HEADER = "X-On-Behalf-Of-User";
        
        // Claim type for OBO context (stored in HttpContext.Items)
        public const string OBO_CONTEXT_KEY = "OnBehalfOfContext";

        public OnBehalfOfMiddleware(RequestDelegate next, ILogger<OnBehalfOfMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context, 
            IUserRepository userRepository,
            IAdminAuditService adminAuditService)
        {
            // Check if OBO header is present
            if (context.Request.Headers.TryGetValue(OBO_HEADER, out var targetUserIdValue))
            {
                // Get current user from JWT claims
                var currentUserIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserRoles = context.User?.FindAll(ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                // Validate user is authenticated
                if (string.IsNullOrEmpty(currentUserIdClaim))
                {
                    _logger.LogWarning("OBO attempt without authentication. IP: {IP}", 
                        context.Connection.RemoteIpAddress);
                    
                    await WriteErrorResponse(context, 401, "Unauthorized: Authentication required for OBO operations");
                    return;
                }

                // Validate user has Admin role
                if (currentUserRoles == null || !currentUserRoles.Contains("Admin"))
                {
                    _logger.LogWarning(
                        "OBO attempt without Admin role. User: {UserId}, Roles: {Roles}, IP: {IP}",
                        currentUserIdClaim,
                        string.Join(",", currentUserRoles ?? new System.Collections.Generic.List<string>()),
                        context.Connection.RemoteIpAddress);
                    
                    await WriteErrorResponse(context, 403, "Forbidden: Admin role required for OBO operations");
                    return;
                }

                // Parse and validate target user ID
                if (!int.TryParse(targetUserIdValue.ToString(), out var targetUserId))
                {
                    _logger.LogWarning(
                        "Invalid target user ID in OBO header. Admin: {AdminId}, TargetUserId: {TargetUserId}",
                        currentUserIdClaim,
                        targetUserIdValue);
                    
                    await WriteErrorResponse(context, 400, "Bad Request: Invalid target user ID");
                    return;
                }

                // Verify target user exists and is active
                var targetUser = await userRepository.GetAsync(u => u.UserId == targetUserId);
                if (targetUser == null)
                {
                    _logger.LogWarning(
                        "OBO attempt for non-existent user. Admin: {AdminId}, TargetUserId: {TargetUserId}",
                        currentUserIdClaim,
                        targetUserId);
                    
                    await WriteErrorResponse(context, 404, "Not Found: Target user does not exist");
                    return;
                }

                if (!targetUser.IsActive)
                {
                    _logger.LogWarning(
                        "OBO attempt for deactivated user. Admin: {AdminId}, TargetUserId: {TargetUserId}",
                        currentUserIdClaim,
                        targetUserId);
                    
                    await WriteErrorResponse(context, 403, "Forbidden: Target user is deactivated");
                    return;
                }

                // Get target user roles to prevent OBO for other admins
                var targetUserRoles = await userRepository.GetUserGroupsAsync(targetUserId);
                if (targetUserRoles.Contains("Admin"))
                {
                    _logger.LogWarning(
                        "OBO attempt for another admin user. Admin: {AdminId}, TargetUserId: {TargetUserId}",
                        currentUserIdClaim,
                        targetUserId);
                    
                    await WriteErrorResponse(context, 403, "Forbidden: Cannot perform OBO operations for admin users");
                    return;
                }

                // Parse admin user ID
                if (!int.TryParse(currentUserIdClaim, out var adminUserId))
                {
                    _logger.LogError(
                        "Failed to parse admin user ID from claims. ClaimValue: {ClaimValue}",
                        currentUserIdClaim);
                    
                    await WriteErrorResponse(context, 500, "Internal Server Error: Invalid user claim");
                    return;
                }

                // Create OBO context
                var oboContext = new OnBehalfOfContext
                {
                    AdminUserId = adminUserId,
                    TargetUserId = targetUserId,
                    TargetUserName = targetUser.FullName,
                    TargetUserEmail = targetUser.Email,
                    RequestPath = context.Request.Path.Value,
                    Timestamp = DateTime.Now
                };

                // Store OBO context in HttpContext.Items for downstream use
                context.Items[OBO_CONTEXT_KEY] = oboContext;

                _logger.LogInformation(
                    "OBO operation initiated. Admin: {AdminId}, Target: {TargetUserId} ({TargetUserName}), Path: {Path}",
                    adminUserId,
                    targetUserId,
                    targetUser.FullName,
                    context.Request.Path);

                // Log the OBO operation initiation
                try
                {
                    await adminAuditService.LogOnBehalfOfOperationAsync(
                        adminUserId: adminUserId,
                        targetUserId: targetUserId,
                        action: $"OBO_{context.Request.Method}",
                        httpContext: context,
                        reason: $"On-behalf-of operation: {context.Request.Method} {context.Request.Path}",
                        entityType: null,
                        entityId: null);
                }
                catch (Exception ex)
                {
                    // Don't fail the request if audit logging fails, just log the error
                    _logger.LogError(ex, "Failed to log OBO operation initiation");
                }
            }

            // Continue to next middleware
            await _next(context);
        }

        /// <summary>
        /// Helper method to write JSON error responses
        /// </summary>
        private async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = message,
                data = (object)null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    /// <summary>
    /// Context object for On-Behalf-Of operations
    /// Stored in HttpContext.Items for downstream access
    /// </summary>
    public class OnBehalfOfContext
    {
        public int AdminUserId { get; set; }
        public int TargetUserId { get; set; }
        public string TargetUserName { get; set; }
        public string TargetUserEmail { get; set; }
        public string RequestPath { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
