using System;

namespace Entities.Dtos
{
    /// <summary>
    /// Filter DTO for querying admin operation logs
    /// Supports filtering by admin, target user, action, entity, date range, etc.
    /// </summary>
    public class AdminOperationLogFilterDto
    {
        /// <summary>
        /// Filter by admin user ID
        /// </summary>
        public int? AdminUserId { get; set; }
        
        /// <summary>
        /// Filter by target user ID
        /// </summary>
        public int? TargetUserId { get; set; }
        
        /// <summary>
        /// Filter by action type (exact match)
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// Filter by entity type
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// Filter by entity ID
        /// </summary>
        public int? EntityId { get; set; }
        
        /// <summary>
        /// Filter by on-behalf-of operations only
        /// </summary>
        public bool? IsOnBehalfOf { get; set; }
        
        /// <summary>
        /// Filter by IP address
        /// </summary>
        public string IpAddress { get; set; }
        
        /// <summary>
        /// Date range filters
        /// </summary>
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        /// <summary>
        /// Search term for reason field (partial match)
        /// </summary>
        public string ReasonSearch { get; set; }
        
        /// <summary>
        /// Pagination
        /// </summary>
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        
        /// <summary>
        /// Sort order (default: newest first)
        /// </summary>
        public string SortBy { get; set; } = "Timestamp";
        public bool SortDescending { get; set; } = true;
    }
}
