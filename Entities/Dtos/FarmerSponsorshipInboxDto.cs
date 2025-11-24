using System;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for farmer's sponsorship code inbox
    /// Shows codes sent to farmer's phone number via SMS
    /// Pattern: Similar to dealer invitation inbox
    /// </summary>
    public class FarmerSponsorshipInboxDto
    {
        /// <summary>
        /// Sponsorship code (e.g., AGRI-2025-X3K9)
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Sponsor company name
        /// </summary>
        public string SponsorName { get; set; }

        /// <summary>
        /// Subscription tier (S, M, L, XL)
        /// </summary>
        public string TierName { get; set; }

        /// <summary>
        /// When the SMS was sent
        /// </summary>
        public DateTime SentDate { get; set; }

        /// <summary>
        /// How it was sent (SMS, WhatsApp)
        /// </summary>
        public string SentVia { get; set; }

        /// <summary>
        /// Whether code has been redeemed
        /// </summary>
        public bool IsUsed { get; set; }

        /// <summary>
        /// When code was redeemed (null if not used)
        /// </summary>
        public DateTime? UsedDate { get; set; }

        /// <summary>
        /// Code expiry date
        /// </summary>
        public DateTime ExpiryDate { get; set; }

        /// <summary>
        /// Deep link for redemption
        /// </summary>
        public string RedemptionLink { get; set; }

        /// <summary>
        /// Farmer's name (from SMS recipient)
        /// </summary>
        public string RecipientName { get; set; }

        /// <summary>
        /// Computed: Is code expired?
        /// </summary>
        public bool IsExpired => ExpiryDate < DateTime.Now;

        /// <summary>
        /// Computed: Days until expiry (negative if expired)
        /// </summary>
        public int DaysUntilExpiry => (ExpiryDate - DateTime.Now).Days;

        /// <summary>
        /// Computed: User-friendly status
        /// </summary>
        public string Status => IsUsed ? "Kullanıldı"
                              : IsExpired ? "Süresi Doldu"
                              : "Aktif";
    }
}
