using Core.Entities;
using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for creating dealer invitation
    /// Used in POST /api/Sponsorship/dealer/invite
    /// </summary>
    public class CreateDealerInvitationDto : IDto
    {
        /// <summary>
        /// Dealer's email address (required for Invite type)
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Dealer's phone number (optional)
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Dealer company/person name
        /// </summary>
        public string DealerName { get; set; }

        /// <summary>
        /// Purchase ID to transfer codes from
        /// </summary>
        public int PurchaseId { get; set; }

        /// <summary>
        /// Number of codes to allocate to this dealer
        /// </summary>
        public int CodeCount { get; set; }

        /// <summary>
        /// Invitation type: "Invite" or "AutoCreate"
        /// </summary>
        public string InvitationType { get; set; } = "Invite";
    }

    /// <summary>
    /// Response DTO for dealer invitation creation
    /// </summary>
    public class DealerInvitationResponseDto : IDto
    {
        public int InvitationId { get; set; }
        public string InvitationToken { get; set; }
        public string InvitationLink { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DealerName { get; set; }
        public int CodeCount { get; set; }
        public string Status { get; set; }
        public string InvitationType { get; set; }
        
        /// <summary>
        /// Only populated for AutoCreate type
        /// </summary>
        public string AutoCreatedPassword { get; set; }
        
        /// <summary>
        /// Only populated for AutoCreate type
        /// </summary>
        public int? CreatedDealerId { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for listing dealer invitations
    /// </summary>
    public class DealerInvitationListDto : IDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DealerName { get; set; }
        public string Status { get; set; }
        public string InvitationType { get; set; }
        public int CodeCount { get; set; }
        public int? CreatedDealerId { get; set; }
        public DateTime? AcceptedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
