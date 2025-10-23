using System.ComponentModel.DataAnnotations;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for deactivating a user account
    /// Admin operation requiring mandatory reason
    /// </summary>
    public class DeactivateUserDto
    {
        /// <summary>
        /// User ID to deactivate
        /// </summary>
        [Required]
        public int UserId { get; set; }
        
        /// <summary>
        /// Mandatory reason for deactivation (audit trail)
        /// </summary>
        [Required(ErrorMessage = "Deactivation reason is required")]
        [MinLength(10, ErrorMessage = "Reason must be at least 10 characters")]
        [MaxLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")]
        public string Reason { get; set; }
        
        /// <summary>
        /// Optional: Send notification to user about deactivation
        /// </summary>
        public bool SendNotification { get; set; } = false;
    }
}
