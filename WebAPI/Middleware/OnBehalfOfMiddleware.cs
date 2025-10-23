using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebAPI.Middleware
{
    /// <summary>
    /// Middleware to handle "on-behalf-of" operations
    /// Allows admins to perform actions on behalf of other users
    /// </summary>
    public class OnBehalfOfMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<OnBehalfOfMiddleware> _logger;
        private const string OnBehalfOfHeader = "X-On-Behalf-Of-User";
        private const string OnBehalfOfReasonHeader = "X-On-Behalf-Of-Reason";

        public OnBehalfOfMiddleware(RequestDelegate next, ILogger<OnBehalfOfMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if on-behalf-of header is present
            if (context.Request.Headers.TryGetValue(OnBehalfOfHeader, out var targetUserIdHeader))
            {
                // Verify the current user is an admin
                var currentUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRoles = context.User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                if (!userRoles.Contains("Admin"))
                {
                    _logger.LogWarning(
                        "Non-admin user {UserId} attempted on-behalf-of operation",
                        currentUserId);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Only administrators can perform on-behalf-of operations");
                    return;
                }

                // Parse target user ID
                if (int.TryParse(targetUserIdHeader, out var targetUserId))
                {
                    // Get reason (optional but recommended)
                    context.Request.Headers.TryGetValue(OnBehalfOfReasonHeader, out var reason);

                    // Store on-behalf-of context in HttpContext items
                    context.Items["OnBehalfOf.TargetUserId"] = targetUserId;
                    context.Items["OnBehalfOf.AdminUserId"] = int.Parse(currentUserId);
                    context.Items["OnBehalfOf.Reason"] = reason.ToString();
                    context.Items["OnBehalfOf.IsActive"] = true;

                    _logger.LogInformation(
                        "Admin {AdminId} is performing operation on behalf of user {TargetUserId}. Reason: {Reason}",
                        currentUserId, targetUserId, reason);
                }
                else
                {
                    _logger.LogWarning(
                        "Invalid target user ID in on-behalf-of header: {TargetUserId}",
                        targetUserIdHeader);

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Invalid target user ID in X-On-Behalf-Of-User header");
                    return;
                }
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Extension method to easily add OnBehalfOf middleware to the pipeline
    /// </summary>
    public static class OnBehalfOfMiddlewareExtensions
    {
        public static IApplicationBuilder UseOnBehalfOf(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OnBehalfOfMiddleware>();
        }
    }
}
