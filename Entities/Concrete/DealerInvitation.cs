using Core.Entities;
using System;
using System.Text.Json.Serialization;

namespace Entities.Concrete
{
    /// <summary>
    /// Tracks dealer invitations and auto-created dealer profiles for code distribution
    /// Supports two onboarding methods: Invite (email invitation) and AutoCreate (automatic account creation)
    /// </summary>
    public class DealerInvitation : IEntity
    {
        public int Id { get; set; }
        
        // Sponsor Information
        public int SponsorId { get; set; } // Ana sponsor who is inviting/creating the dealer
        
        // Dealer Information
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DealerName { get; set; } // Dealer company/person name
        
        // Invitation Status and Type
        /// <summary>
        /// Pending: Waiting for acceptance
        /// Accepted: Dealer linked and codes transferred
        /// Expired: Token/invitation expired
        /// Cancelled: Sponsor cancelled the invitation
        /// </summary>
        public string Status { get; set; } = "Pending";
        
        /// <summary>
        /// Invite: Email/SMS invitation with registration link
        /// AutoCreate: Automatic sponsor account creation with generated password
        /// </summary>
        public string InvitationType { get; set; }
        
        public string InvitationToken { get; set; } // Unique token for invitation link
        
        // Package and Code Information
        /// <summary>
        /// [DEPRECATED] Purchase ID - nullable for backward compatibility.
        /// New invitations should use PackageTier instead for intelligent code selection.
        /// </summary>
        public int? PurchaseId { get; set; }
        
        /// <summary>
        /// Optional tier filter for code selection: S, M, L, XL.
        /// If null, codes from any tier can be selected automatically.
        /// System will intelligently select codes based on expiry date (FIFO).
        /// </summary>
        public string PackageTier { get; set; }
        
        public int CodeCount { get; set; } // Number of codes to transfer

        // Dealer Creation Tracking
        public int? CreatedDealerId { get; set; } // Set when dealer account is created/linked
        public DateTime? AcceptedDate { get; set; } // When dealer accepted the invitation
        public string AutoCreatedPassword { get; set; } // Encrypted password for auto-created accounts
        
        // SMS/Link Tracking
        public DateTime? LinkSentDate { get; set; } // When the SMS/link was sent
        public string LinkSentVia { get; set; } // SMS, WhatsApp, Email, etc.
        public bool LinkDelivered { get; set; } = false; // Whether the message was successfully delivered

        // Audit Fields
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ExpiryDate { get; set; } // Invitation expiry (default: 7 days)
        public DateTime? CancelledDate { get; set; }
        public int? CancelledByUserId { get; set; }
        public string Notes { get; set; }
        
        // Navigation properties removed to prevent compilation issues
        // Use foreign key IDs directly instead of navigation properties
    }
}
