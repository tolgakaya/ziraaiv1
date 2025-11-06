using System.ComponentModel.DataAnnotations;

namespace Entities.Dtos
{
    /// <summary>
    /// Request DTO for reclaiming unused codes from a dealer
    /// Only contains user-provided fields (UserId comes from token)
    /// </summary>
    public class ReclaimDealerCodesRequestDto
    {
        [Required(ErrorMessage = "Dealer ID is required")]
        public int DealerId { get; set; }
        
        [Required(ErrorMessage = "Reclaim reason is required")]
        [MaxLength(500, ErrorMessage = "Reclaim reason cannot exceed 500 characters")]
        public string ReclaimReason { get; set; }
    }
}
