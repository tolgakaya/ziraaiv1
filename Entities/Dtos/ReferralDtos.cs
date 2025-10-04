using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    #region Request DTOs

    /// <summary>
    /// Request to generate and send referral links
    /// </summary>
    public class GenerateReferralLinkRequest
    {
        /// <summary>
        /// Delivery method: 1=SMS, 2=WhatsApp, 3=Both (Hybrid)
        /// </summary>
        public int DeliveryMethod { get; set; }

        /// <summary>
        /// List of recipient phone numbers (Turkish format: 05321234567)
        /// </summary>
        public List<string> PhoneNumbers { get; set; }

        /// <summary>
        /// Optional custom message (will use default template if null)
        /// </summary>
        public string CustomMessage { get; set; }
    }

    /// <summary>
    /// Request to validate a referral code
    /// </summary>
    public class ValidateReferralCodeRequest
    {
        public string Code { get; set; }
    }

    /// <summary>
    /// Request to track a referral click
    /// </summary>
    public class TrackReferralClickRequest
    {
        public string Code { get; set; }
        public string IpAddress { get; set; }
        public string DeviceId { get; set; }
    }

    #endregion

    #region Response DTOs

    /// <summary>
    /// Response after generating referral link
    /// </summary>
    public class ReferralLinkResponse
    {
        public string ReferralCode { get; set; }
        public string DeepLink { get; set; }
        public string PlayStoreLink { get; set; }
        public DateTime ExpiresAt { get; set; }
        public List<DeliveryStatusDto> DeliveryStatuses { get; set; }
    }

    /// <summary>
    /// Delivery status for a phone number
    /// </summary>
    public class DeliveryStatusDto
    {
        public string PhoneNumber { get; set; }
        public string Method { get; set; } // "SMS" or "WhatsApp"
        public string Status { get; set; } // "Sent", "Failed", "Pending"
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Referral statistics for a user
    /// </summary>
    public class ReferralStatsResponse
    {
        public int TotalReferrals { get; set; }
        public int SuccessfulReferrals { get; set; }
        public int PendingReferrals { get; set; }
        public int TotalCreditsEarned { get; set; }
        public ReferralBreakdownDto ReferralBreakdown { get; set; }
    }

    /// <summary>
    /// Breakdown of referral stages
    /// </summary>
    public class ReferralBreakdownDto
    {
        public int Clicked { get; set; }
        public int Registered { get; set; }
        public int Validated { get; set; }
        public int Rewarded { get; set; }
    }

    /// <summary>
    /// Referral code information
    /// </summary>
    public class ReferralCodeDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; } // "Active", "Expired", "Disabled"
        public int UsageCount { get; set; }
    }

    /// <summary>
    /// Referral credit breakdown
    /// </summary>
    public class ReferralCreditBreakdownDto
    {
        public int TotalEarned { get; set; }
        public int TotalUsed { get; set; }
        public int CurrentBalance { get; set; }
    }

    /// <summary>
    /// Referral reward information
    /// </summary>
    public class ReferralRewardDto
    {
        public int Id { get; set; }
        public string RefereeUserName { get; set; }
        public int CreditAmount { get; set; }
        public DateTime AwardedAt { get; set; }
    }

    #endregion
}
