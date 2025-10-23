using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Base controller for all admin endpoints
    /// Provides admin-specific helpers and on-behalf-of support
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("api/admin/[controller]")]
    [ApiController]
    public class AdminBaseController : BaseApiController
    {
        /// <summary>
        /// Get the current admin user ID from claims
        /// </summary>
        protected int AdminUserId
        {
            get
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.Parse(userIdClaim ?? "0");
            }
        }

        /// <summary>
        /// Get the current admin user email from claims
        /// </summary>
        protected string AdminUserEmail => User.FindFirst(ClaimTypes.Email)?.Value;

        /// <summary>
        /// Get the current admin user name from claims
        /// </summary>
        protected string AdminUserName => User.FindFirst(ClaimTypes.Name)?.Value;

        /// <summary>
        /// Check if current request is an on-behalf-of operation
        /// </summary>
        protected bool IsOnBehalfOfOperation => HttpContext.Items.ContainsKey("OnBehalfOf.IsActive")
            && (bool)HttpContext.Items["OnBehalfOf.IsActive"];

        /// <summary>
        /// Get target user ID for on-behalf-of operations
        /// Returns null if not an OBO operation
        /// </summary>
        protected int? OnBehalfOfTargetUserId
        {
            get
            {
                if (!IsOnBehalfOfOperation) return null;
                return HttpContext.Items.TryGetValue("OnBehalfOf.TargetUserId", out var userId)
                    ? (int?)userId
                    : null;
            }
        }

        /// <summary>
        /// Get reason for on-behalf-of operation
        /// Returns null if not an OBO operation or no reason provided
        /// </summary>
        protected string OnBehalfOfReason
        {
            get
            {
                if (!IsOnBehalfOfOperation) return null;
                return HttpContext.Items.TryGetValue("OnBehalfOf.Reason", out var reason)
                    ? reason?.ToString()
                    : null;
            }
        }

        /// <summary>
        /// Get client IP address for audit logging
        /// </summary>
        protected string ClientIpAddress
        {
            get
            {
                // Check for X-Forwarded-For header (common in load balancers/proxies)
                var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    // X-Forwarded-For can contain multiple IPs, take the first one
                    return forwardedFor.Split(',')[0].Trim();
                }

                // Fallback to direct connection IP
                return HttpContext.Connection.RemoteIpAddress?.ToString();
            }
        }

        /// <summary>
        /// Get user agent string for audit logging
        /// </summary>
        protected string UserAgent => HttpContext.Request.Headers["User-Agent"].FirstOrDefault();

        /// <summary>
        /// Get current request path for audit logging
        /// </summary>
        protected string RequestPath => HttpContext.Request.Path.Value;

        /// <summary>
        /// Helper to verify admin has specific role/claim
        /// </summary>
        protected bool HasRole(string role)
        {
            return User.IsInRole(role);
        }

        /// <summary>
        /// Helper to verify admin has specific operation claim
        /// </summary>
        protected bool HasClaim(string claimType, string claimValue)
        {
            return User.HasClaim(claimType, claimValue);
        }
    }
}
