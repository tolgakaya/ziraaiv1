using System.Linq;

namespace Core.Utilities.Helpers
{
    /// <summary>
    /// Phone number normalization utility for consistent Turkish phone number formatting
    /// CRITICAL: This format must be used consistently across ALL farmer invitation operations
    /// </summary>
    public static class PhoneNumberHelper
    {
        /// <summary>
        /// Normalize Turkish phone number to +90XXXXXXXXXX format (E.164 standard)
        ///
        /// Examples:
        /// - "05421396386" → "+905421396386"
        /// - "+905421396386" → "+905421396386"
        /// - "905421396386" → "+905421396386"
        /// - "5421396386" → "+905421396386"
        /// - "+90 542 139 6386" → "+905421396386"
        ///
        /// This is the CANONICAL format for the application.
        /// All phone numbers in FarmerInvitation and related tables MUST use this format.
        /// </summary>
        public static string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            // Remove all non-numeric characters (spaces, dashes, parentheses)
            var cleaned = new string(phone.Where(char.IsDigit).ToArray());

            // Handle different input formats and normalize to +90XXXXXXXXXX
            if (cleaned.StartsWith("90") && cleaned.Length == 12)
            {
                // Already has country code: "905421396386" → "+905421396386"
                return "+" + cleaned;
            }
            else if (cleaned.StartsWith("0") && cleaned.Length == 11)
            {
                // Turkish format with leading 0: "05421396386" → "+905421396386"
                return "+90" + cleaned.Substring(1);
            }
            else if (cleaned.Length == 10)
            {
                // Mobile number without prefix: "5421396386" → "+905421396386"
                return "+90" + cleaned;
            }

            // If already in correct format or unrecognized format, return as-is with + prefix
            if (!cleaned.StartsWith("+"))
            {
                return "+" + cleaned;
            }

            return cleaned;
        }

        /// <summary>
        /// Format phone number for display purposes (Turkish format)
        /// "+905421396386" → "0542 139 6386"
        /// </summary>
        public static string FormatForDisplay(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;

            var normalized = NormalizePhoneNumber(phone);
            if (normalized.StartsWith("+90") && normalized.Length == 13)
            {
                // +905421396386 → 0542 139 6386
                var without90 = "0" + normalized.Substring(3);
                return $"{without90.Substring(0, 4)} {without90.Substring(4, 3)} {without90.Substring(7)}";
            }

            return phone;
        }

        /// <summary>
        /// Check if two phone numbers are equivalent (after normalization)
        /// </summary>
        public static bool AreEquivalent(string phone1, string phone2)
        {
            if (string.IsNullOrWhiteSpace(phone1) || string.IsNullOrWhiteSpace(phone2))
                return false;

            return NormalizePhoneNumber(phone1) == NormalizePhoneNumber(phone2);
        }
    }
}
