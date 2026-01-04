using Core.Entities;
using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for creating farmer invitation
    /// Used in POST /api/v1/sponsorship/farmer-invitations
    /// </summary>
    public class CreateFarmerInvitationDto : IDto
    {
        /// <summary>
        /// Farmer's phone number (required, used for matching during acceptance)
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Farmer's name (optional)
        /// </summary>
        public string FarmerName { get; set; }

        /// <summary>
        /// Farmer's email address (optional)
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Number of codes to allocate to this farmer
        /// </summary>
        public int CodeCount { get; set; }

        /// <summary>
        /// Package tier (S, M, L, XL) - determines which codes to send
        /// </summary>
        public string PackageTier { get; set; }

        /// <summary>
        /// Notes about this invitation (optional)
        /// </summary>
        public string Notes { get; set; }
    }

    /// <summary>
    /// Response DTO for farmer invitation creation
    /// </summary>
    public class FarmerInvitationResponseDto : IDto
    {
        public int InvitationId { get; set; }
        public string InvitationToken { get; set; }
        public string InvitationLink { get; set; }
        public string Phone { get; set; }
        public string FarmerName { get; set; }
        public string Email { get; set; }
        public int CodeCount { get; set; }
        public string PackageTier { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiryDate { get; set; }

        /// <summary>
        /// SMS delivery status
        /// </summary>
        public bool SmsSent { get; set; }
        public string SmsDeliveryStatus { get; set; }
        public DateTime? LinkSentDate { get; set; }
        public string LinkSentVia { get; set; }
    }

    /// <summary>
    /// Response DTO for farmer invitation acceptance
    /// Contains the actual sponsorship codes assigned to the farmer
    /// </summary>
    public class FarmerInvitationAcceptResponseDto : IDto
    {
        public int InvitationId { get; set; }
        public string Status { get; set; }
        public DateTime AcceptedDate { get; set; }
        public int TotalCodesAssigned { get; set; }

        /// <summary>
        /// The actual sponsorship codes assigned to the farmer
        /// </summary>
        public List<string> SponsorshipCodes { get; set; }

        /// <summary>
        /// Summary by package tier
        /// </summary>
        public Dictionary<string, int> CodesByTier { get; set; }

        public string Message { get; set; }
    }

    /// <summary>
    /// DTO for farmer invitation details (public, no auth required for unregistered users)
    /// Used by mobile app to show invitation details before acceptance
    /// </summary>
    public class FarmerInvitationDetailDto : IDto
    {
        public int InvitationId { get; set; }
        public string SponsorName { get; set; }
        public int CodeCount { get; set; }
        public string PackageTier { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int RemainingDays { get; set; }
        public string Status { get; set; }
        public bool CanAccept { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for listing farmer invitations (for sponsor view)
    /// </summary>
    public class FarmerInvitationListDto : IDto
    {
        public int Id { get; set; }
        public string Phone { get; set; }
        public string FarmerName { get; set; }
        public string Email { get; set; }
        public string InvitationToken { get; set; }
        public string Status { get; set; }
        public int CodeCount { get; set; }
        public string PackageTier { get; set; }
        public int? AcceptedByUserId { get; set; }
        public DateTime? AcceptedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool LinkDelivered { get; set; }
        public DateTime? LinkSentDate { get; set; }
    }

    /// <summary>
    /// Paginated response for farmer invitations list
    /// </summary>
    public class FarmerInvitationsPaginatedDto : IDto
    {
        public List<FarmerInvitationListDto> Invitations { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
