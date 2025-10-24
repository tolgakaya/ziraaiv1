using Core.Entities;
using Core.Entities.Concrete;
using System;

namespace Entities.Concrete
{
    /// <summary>
    /// Audit trail entity for all admin operations including on-behalf-of actions
    /// Tracks who did what, when, why, and what changed
    /// </summary>
    public class AdminOperationLog : IEntity
    {
        public int Id { get; set; }

        // Actor Information
        /// <summary>
        /// Admin user ID who performed the action
        /// </summary>
        public int AdminUserId { get; set; }

        /// <summary>
        /// User affected by the action (for on-behalf-of operations)
        /// </summary>
        public int? TargetUserId { get; set; }

        // Action Details
        /// <summary>
        /// Action performed (e.g., "DeactivateUser", "CreateAnalysis", "AssignSubscription")
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Entity type affected (e.g., "User", "PlantAnalysis", "Subscription")
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// ID of the affected entity
        /// </summary>
        public int? EntityId { get; set; }

        /// <summary>
        /// True if admin acted on behalf of another user
        /// </summary>
        public bool IsOnBehalfOf { get; set; }

        // Request Context
        /// <summary>
        /// IP address of the admin who performed the action (IPv4 or IPv6)
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Browser/client user agent string
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// API endpoint path that was called
        /// </summary>
        public string RequestPath { get; set; }

        /// <summary>
        /// JSON serialized request body (for complex operations)
        /// </summary>
        public string RequestPayload { get; set; }

        // Response Information
        /// <summary>
        /// HTTP status code of the response (200, 400, 500, etc.)
        /// </summary>
        public int? ResponseStatus { get; set; }

        /// <summary>
        /// Request processing time in milliseconds
        /// </summary>
        public int? Duration { get; set; }

        // Audit Information
        /// <summary>
        /// Timestamp when the action was performed
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Admin's reason for the action (especially important for on-behalf-of operations)
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// JSON snapshot of entity state before the change (for critical operations)
        /// </summary>
        public string BeforeState { get; set; }

        /// <summary>
        /// JSON snapshot of entity state after the change (for critical operations)
        /// </summary>
        public string AfterState { get; set; }

        // Navigation Properties
        /// <summary>
        /// Admin user who performed the action
        /// </summary>
        public virtual User AdminUser { get; set; }

        /// <summary>
        /// Target user affected by the action
        /// </summary>
        public virtual User TargetUser { get; set; }
    }
}
