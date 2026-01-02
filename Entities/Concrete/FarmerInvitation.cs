using Core.Entities;
using System;

namespace Entities.Concrete
{
    /// <summary>
    /// Tracks farmer invitations for sponsorship code distribution via token-based acceptance flow.
    /// Similar to DealerInvitation but for end-user farmers.
    /// Supports unregistered users (phone-based matching) and registered users.
    /// </summary>
    public class FarmerInvitation : IEntity
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int Id { get; set; }

        // =====================================================
        // SPONSOR INFORMATION
        // =====================================================

        /// <summary>
        /// Sponsor who is sending the invitation and distributing codes
        /// </summary>
        public int SponsorId { get; set; }

        // =====================================================
        // FARMER INFORMATION
        // =====================================================

        /// <summary>
        /// Farmer's phone number (required, normalized format: +90XXXXXXXXXX)
        /// Used for security verification during acceptance
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Farmer's name (optional, for personalization)
        /// </summary>
        public string FarmerName { get; set; }

        /// <summary>
        /// Farmer's email address (optional)
        /// </summary>
        public string Email { get; set; }

        // =====================================================
        // INVITATION DETAILS
        // =====================================================

        /// <summary>
        /// Unique invitation token (32-character hex string)
        /// Used in deep link: ziraai://farmer-invite/{token}
        /// Format: FARMER-{Guid.NewGuid().ToString("N")}
        /// </summary>
        public string InvitationToken { get; set; }

        /// <summary>
        /// Invitation status lifecycle
        /// Values: Pending (default), Accepted, Expired, Cancelled
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Type of invitation (for consistency with DealerInvitation)
        /// Default: "Invite" (SMS/WhatsApp invitation with deep link)
        /// </summary>
        public string InvitationType { get; set; } = "Invite";

        // =====================================================
        // CODE INFORMATION
        // =====================================================

        /// <summary>
        /// Number of sponsorship codes to assign to farmer upon acceptance
        /// </summary>
        public int CodeCount { get; set; }

        /// <summary>
        /// Optional tier filter for intelligent code selection: S, M, L, XL
        /// If null, codes from any tier can be selected (FIFO based on expiry)
        /// </summary>
        public string PackageTier { get; set; }

        // =====================================================
        // ACCEPTANCE TRACKING
        // =====================================================

        /// <summary>
        /// User ID of farmer who accepted the invitation
        /// Set when invitation is accepted (via AcceptFarmerInvitationCommand)
        /// </summary>
        public int? AcceptedByUserId { get; set; }

        /// <summary>
        /// Timestamp when invitation was accepted
        /// </summary>
        public DateTime? AcceptedDate { get; set; }

        // =====================================================
        // SMS/MESSAGING TRACKING
        // =====================================================

        /// <summary>
        /// Timestamp when SMS/WhatsApp message with invitation link was sent
        /// </summary>
        public DateTime? LinkSentDate { get; set; }

        /// <summary>
        /// Channel used to send invitation: SMS, WhatsApp, Email
        /// </summary>
        public string LinkSentVia { get; set; }

        /// <summary>
        /// Whether the message was successfully delivered to farmer
        /// </summary>
        public bool LinkDelivered { get; set; } = false;

        // =====================================================
        // LIFECYCLE
        // =====================================================

        /// <summary>
        /// Timestamp when invitation was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Expiration timestamp for invitation (default: 7 days from creation)
        /// Expired invitations cannot be accepted
        /// </summary>
        public DateTime ExpiryDate { get; set; }

        /// <summary>
        /// Timestamp when invitation was cancelled (if applicable)
        /// </summary>
        public DateTime? CancelledDate { get; set; }

        /// <summary>
        /// Additional notes or metadata about the invitation
        /// </summary>
        public string Notes { get; set; }

        // Navigation properties removed to prevent compilation issues
        // Use foreign key IDs directly and fetch related entities when needed
    }
}
