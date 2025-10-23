using Entities.Concrete;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Business.Services.AdminAudit
{
    /// <summary>
    /// Service interface for admin audit trail logging
    /// Provides methods to log all admin operations with full context
    /// </summary>
    public interface IAdminAuditService
    {
        /// <summary>
        /// Log an admin operation with full context
        /// </summary>
        /// <param name="adminUserId">Admin user performing the operation</param>
        /// <param name="action">Action type (e.g., "CREATE_USER", "UPDATE_SUBSCRIPTION")</param>
        /// <param name="httpContext">HTTP context for request metadata</param>
        /// <param name="targetUserId">Target user affected (optional)</param>
        /// <param name="entityType">Entity type affected (e.g., "User", "Subscription")</param>
        /// <param name="entityId">Entity ID affected</param>
        /// <param name="isOnBehalfOf">Is this an on-behalf-of operation</param>
        /// <param name="reason">Admin-provided reason</param>
        /// <param name="beforeState">State before operation (JSON)</param>
        /// <param name="afterState">State after operation (JSON)</param>
        /// <param name="requestPayload">Request payload (JSON)</param>
        /// <param name="responseStatus">HTTP response status</param>
        /// <param name="duration">Operation duration in milliseconds</param>
        /// <returns>Created audit log entry</returns>
        Task<AdminOperationLog> LogOperationAsync(
            int adminUserId,
            string action,
            HttpContext httpContext,
            int? targetUserId = null,
            string entityType = null,
            int? entityId = null,
            bool isOnBehalfOf = false,
            string reason = null,
            string beforeState = null,
            string afterState = null,
            string requestPayload = null,
            int? responseStatus = null,
            int? duration = null);

        /// <summary>
        /// Log a simple admin action without detailed state tracking
        /// </summary>
        Task<AdminOperationLog> LogSimpleActionAsync(
            int adminUserId,
            string action,
            HttpContext httpContext,
            string reason = null);

        /// <summary>
        /// Log an on-behalf-of operation
        /// </summary>
        Task<AdminOperationLog> LogOnBehalfOfOperationAsync(
            int adminUserId,
            int targetUserId,
            string action,
            HttpContext httpContext,
            string reason,
            string entityType = null,
            int? entityId = null);

        /// <summary>
        /// Log a user management operation (create, update, deactivate, etc.)
        /// </summary>
        Task<AdminOperationLog> LogUserManagementAsync(
            int adminUserId,
            string action,
            HttpContext httpContext,
            int targetUserId,
            string reason,
            object beforeState = null,
            object afterState = null);

        /// <summary>
        /// Log a subscription management operation
        /// </summary>
        Task<AdminOperationLog> LogSubscriptionManagementAsync(
            int adminUserId,
            string action,
            HttpContext httpContext,
            int targetUserId,
            int subscriptionId,
            string reason,
            object beforeState = null,
            object afterState = null);

        /// <summary>
        /// Log a sponsorship management operation
        /// </summary>
        Task<AdminOperationLog> LogSponsorshipManagementAsync(
            int adminUserId,
            string action,
            HttpContext httpContext,
            int? targetUserId,
            int entityId,
            string reason,
            object beforeState = null,
            object afterState = null);
    }
}
