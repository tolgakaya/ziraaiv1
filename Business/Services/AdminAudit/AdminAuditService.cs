using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Business.Services.AdminAudit
{
    /// <summary>
    /// Service implementation for admin audit trail logging
    /// Captures comprehensive metadata for all admin operations
    /// </summary>
    public class AdminAuditService : IAdminAuditService
    {
        private readonly IAdminOperationLogRepository _adminOperationLogRepository;

        public AdminAuditService(IAdminOperationLogRepository adminOperationLogRepository)
        {
            _adminOperationLogRepository = adminOperationLogRepository;
        }

        /// <summary>
        /// Log an admin operation with full context
        /// </summary>
        public async Task<AdminOperationLog> LogOperationAsync(
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
            int? duration = null)
        {
            var log = new AdminOperationLog
            {
                AdminUserId = adminUserId,
                TargetUserId = targetUserId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                IsOnBehalfOf = isOnBehalfOf,
                IpAddress = GetIpAddress(httpContext),
                UserAgent = GetUserAgent(httpContext),
                RequestPath = httpContext?.Request?.Path.Value,
                RequestPayload = requestPayload,
                ResponseStatus = responseStatus,
                Duration = duration,
                Timestamp = DateTime.Now,
                Reason = reason,
                BeforeState = beforeState,
                AfterState = afterState
            };

            return await _adminOperationLogRepository.AddAsync(log);
        }

        /// <summary>
        /// Log a simple admin action without detailed state tracking
        /// </summary>
        public async Task<AdminOperationLog> LogSimpleActionAsync(
            int adminUserId,
            string action,
            HttpContext httpContext,
            string reason = null)
        {
            return await LogOperationAsync(
                adminUserId: adminUserId,
                action: action,
                httpContext: httpContext,
                reason: reason);
        }

        /// <summary>
        /// Log an on-behalf-of operation
        /// </summary>
        public async Task<AdminOperationLog> LogOnBehalfOfOperationAsync(
            int adminUserId,
            int targetUserId,
            string action,
            HttpContext httpContext,
            string reason,
            string entityType = null,
            int? entityId = null)
        {
            return await LogOperationAsync(
                adminUserId: adminUserId,
                action: action,
                httpContext: httpContext,
                targetUserId: targetUserId,
                entityType: entityType,
                entityId: entityId,
                isOnBehalfOf: true,
                reason: reason);
        }

        /// <summary>
        /// Log a user management operation (create, update, deactivate, etc.)
        /// </summary>
        public async Task<AdminOperationLog> LogUserManagementAsync(
            int adminUserId,
            string action,
            HttpContext httpContext,
            int targetUserId,
            string reason,
            object beforeState = null,
            object afterState = null)
        {
            return await LogOperationAsync(
                adminUserId: adminUserId,
                action: action,
                httpContext: httpContext,
                targetUserId: targetUserId,
                entityType: "User",
                entityId: targetUserId,
                reason: reason,
                beforeState: SerializeState(beforeState),
                afterState: SerializeState(afterState));
        }

        /// <summary>
        /// Log a subscription management operation
        /// </summary>
        public async Task<AdminOperationLog> LogSubscriptionManagementAsync(
            int adminUserId,
            string action,
            HttpContext httpContext,
            int targetUserId,
            int subscriptionId,
            string reason,
            object beforeState = null,
            object afterState = null)
        {
            return await LogOperationAsync(
                adminUserId: adminUserId,
                action: action,
                httpContext: httpContext,
                targetUserId: targetUserId,
                entityType: "UserSubscription",
                entityId: subscriptionId,
                reason: reason,
                beforeState: SerializeState(beforeState),
                afterState: SerializeState(afterState));
        }

        /// <summary>
        /// Log a sponsorship management operation
        /// </summary>
        public async Task<AdminOperationLog> LogSponsorshipManagementAsync(
            int adminUserId,
            string action,
            HttpContext httpContext,
            int? targetUserId,
            int entityId,
            string reason,
            object beforeState = null,
            object afterState = null)
        {
            return await LogOperationAsync(
                adminUserId: adminUserId,
                action: action,
                httpContext: httpContext,
                targetUserId: targetUserId,
                entityType: "SponsorshipCode",
                entityId: entityId,
                reason: reason,
                beforeState: SerializeState(beforeState),
                afterState: SerializeState(afterState));
        }

        #region Helper Methods

        /// <summary>
        /// Extract IP address from HTTP context
        /// Handles X-Forwarded-For for proxies/load balancers
        /// </summary>
        private string GetIpAddress(HttpContext httpContext)
        {
            if (httpContext == null) return null;

            // Check X-Forwarded-For header (for proxies/load balancers)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Take the first IP if multiple are present
                var ips = forwardedFor.Split(',');
                if (ips.Length > 0)
                    return ips[0].Trim();
            }

            // Fallback to remote IP address
            return httpContext.Connection?.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// Extract User-Agent from HTTP context
        /// </summary>
        private string GetUserAgent(HttpContext httpContext)
        {
            if (httpContext == null) return null;
            return httpContext.Request.Headers["User-Agent"].ToString();
        }

        /// <summary>
        /// Serialize object to JSON for state tracking
        /// Handles null values and circular references
        /// </summary>
        private string SerializeState(object state)
        {
            if (state == null) return null;

            try
            {
                // If already a string, return as-is
                if (state is string str)
                    return str;

                // Serialize to JSON with circular reference handling
                return JsonConvert.SerializeObject(state, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.None
                });
            }
            catch (Exception)
            {
                // If serialization fails, return safe fallback
                return "{\"error\":\"serialization_failed\"}";
            }
        }

        #endregion
    }
}
