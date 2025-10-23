using System;
using Core.Entities;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for AdminOperationLog entity
    /// Used to return audit trail information for admin operations
    /// </summary>
    public class AdminOperationLogDto : IDto
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Admin user who performed the operation
        /// </summary>
        public int AdminUserId { get; set; }
        public string AdminUserName { get; set; }
        public string AdminUserEmail { get; set; }
        
        /// <summary>
        /// Target user affected by the operation (nullable)
        /// </summary>
        public int? TargetUserId { get; set; }
        public string TargetUserName { get; set; }
        public string TargetUserEmail { get; set; }
        
        /// <summary>
        /// Operation performed (e.g., "CREATE_USER", "UPDATE_SUBSCRIPTION", "DEACTIVATE_USER")
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// Type of entity affected (e.g., "User", "Subscription", "PlantAnalysis")
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// ID of the specific entity affected
        /// </summary>
        public int? EntityId { get; set; }
        
        /// <summary>
        /// Was this an on-behalf-of operation
        /// </summary>
        public bool IsOnBehalfOf { get; set; }
        
        /// <summary>
        /// Request metadata
        /// </summary>
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }
        
        /// <summary>
        /// Request/Response data (may be truncated for large payloads)
        /// </summary>
        public string RequestPayload { get; set; }
        public int? ResponseStatus { get; set; }
        
        /// <summary>
        /// Operation duration in milliseconds
        /// </summary>
        public int? Duration { get; set; }
        
        /// <summary>
        /// When the operation occurred
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Admin-provided reason for the operation
        /// </summary>
        public string Reason { get; set; }
        
        /// <summary>
        /// State snapshots (JSON) - may be truncated
        /// </summary>
        public string BeforeState { get; set; }
        public string AfterState { get; set; }
    }
}
