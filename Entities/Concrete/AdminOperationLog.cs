using System;
using Core.Entities;
using Core.Entities.Concrete;

namespace Entities.Concrete
{
    /// <summary>
    /// Audit trail entity for all admin operations including on-behalf-of actions
    /// Tracks who did what, when, and why for compliance and debugging
    /// </summary>
    public class AdminOperationLog : IEntity
    {
        public int Id { get; set; }
        
        /// <summary>
        /// ID of the admin user who performed the action
        /// </summary>
        public int AdminUserId { get; set; }
        
        /// <summary>
        /// ID of the user affected by the action (null for system-wide operations)
        /// Used for on-behalf-of operations and user management actions
        /// </summary>
        public int? TargetUserId { get; set; }
        
        /// <summary>
        /// Action performed (e.g., "DeactivateUser", "CreateAnalysis", "AssignSubscription")
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// Type of entity affected (e.g., "User", "PlantAnalysis", "Subscription")
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// ID of the affected entity
        /// </summary>
        public int? EntityId { get; set; }
        
        /// <summary>
        /// True when admin is acting on behalf of another user (farmer/sponsor)
        /// </summary>
        public bool IsOnBehalfOf { get; set; }
        
        /// <summary>
        /// Client IP address (IPv4 or IPv6)
        /// </summary>
        public string IpAddress { get; set; }
        
        /// <summary>
        /// Browser/client user agent
        /// </summary>
        public string UserAgent { get; set; }
        
        /// <summary>
        /// API endpoint called (e.g., "/api/v1/admin/users/456")
        /// </summary>
        public string RequestPath { get; set; }
        
        /// <summary>
        /// JSON serialized request body (for complex operations)
        /// </summary>
        public string RequestPayload { get; set; }
        
        /// <summary>
        /// HTTP status code (200, 400, 500, etc.)
        /// </summary>
        public int? ResponseStatus { get; set; }
        
        /// <summary>
        /// Request processing time in milliseconds
        /// </summary>
        public int? Duration { get; set; }
        
        /// <summary>
        /// When the action was performed
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Admin's reason for the action (especially important for OBO operations)
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
        
        // Navigation properties
        
        /// <summary>
        /// Admin user who performed the action
        /// </summary>
        public virtual User AdminUser { get; set; }
        
        /// <summary>
        /// Target user affected by the action (if any)
        /// </summary>
        public virtual User TargetUser { get; set; }
    }
}
