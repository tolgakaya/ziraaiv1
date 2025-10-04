using Core.Entities;
using System;

namespace Entities.Concrete
{
    /// <summary>
    /// System-wide configurable settings for the referral program.
    /// Allows runtime configuration without code changes.
    /// Examples: Credit amount per referral, link expiry days, validation rules, etc.
    /// </summary>
    public class ReferralConfiguration : IEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// Configuration key (unique identifier)
        /// Examples: "Referral.CreditPerReferral", "Referral.LinkExpiryDays"
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Configuration value (stored as text, parsed by DataType)
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Human-readable description of this configuration
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Data type for parsing: "int", "bool", "string", "decimal"
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// When this configuration was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// User who last updated this configuration (admin user ID)
        /// Nullable for system-generated defaults
        /// </summary>
        public int? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Standard referral configuration keys (constants)
    /// </summary>
    public static class ReferralConfigurationKeys
    {
        /// <summary>
        /// Number of analysis credits per successful referral (default: 10)
        /// </summary>
        public const string CreditPerReferral = "Referral.CreditPerReferral";

        /// <summary>
        /// Number of days before referral link expires (default: 30)
        /// </summary>
        public const string LinkExpiryDays = "Referral.LinkExpiryDays";

        /// <summary>
        /// Minimum analyses required for validation (default: 1)
        /// </summary>
        public const string MinAnalysisForValidation = "Referral.MinAnalysisForValidation";

        /// <summary>
        /// Maximum referrals per user (default: 0 = unlimited)
        /// </summary>
        public const string MaxReferralsPerUser = "Referral.MaxReferralsPerUser";

        /// <summary>
        /// Enable WhatsApp link sharing (default: true)
        /// </summary>
        public const string EnableWhatsApp = "Referral.EnableWhatsApp";

        /// <summary>
        /// Enable SMS link sharing (default: true)
        /// </summary>
        public const string EnableSMS = "Referral.EnableSMS";

        /// <summary>
        /// Referral code prefix (default: "ZIRA")
        /// </summary>
        public const string CodePrefix = "Referral.CodePrefix";

        /// <summary>
        /// Base URL for deep links (default: "https://ziraai.com/ref/")
        /// </summary>
        public const string DeepLinkBaseUrl = "Referral.DeepLinkBaseUrl";
    }
}
