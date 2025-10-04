using Core.Utilities.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Referral
{
    /// <summary>
    /// Service for generating and sending referral links via SMS and WhatsApp.
    /// Supports hybrid delivery (both SMS and WhatsApp).
    /// </summary>
    public interface IReferralLinkService
    {
        /// <summary>
        /// Generate referral link and send via specified method(s)
        /// </summary>
        Task<IDataResult<ReferralLinkResponse>> GenerateAndSendLinksAsync(
            int userId,
            List<string> phoneNumbers,
            DeliveryMethod deliveryMethod,
            string customMessage = null);

        /// <summary>
        /// Build Play Store deep link with referral code
        /// </summary>
        Task<string> BuildPlayStoreLinkAsync(string referralCode);

        /// <summary>
        /// Build web deep link for referral tracking
        /// </summary>
        Task<string> BuildWebDeepLinkAsync(string referralCode);
    }

    /// <summary>
    /// Delivery method for referral links
    /// </summary>
    public enum DeliveryMethod
    {
        SMS = 1,
        WhatsApp = 2,
        Both = 3  // Hybrid: SMS + WhatsApp (user requirement!)
    }

    /// <summary>
    /// Response from link generation and sending
    /// </summary>
    public class ReferralLinkResponse
    {
        public string ReferralCode { get; set; }
        public string DeepLink { get; set; }
        public string PlayStoreLink { get; set; }
        public System.DateTime ExpiresAt { get; set; }
        public List<DeliveryStatus> DeliveryStatuses { get; set; }
    }

    /// <summary>
    /// Status of delivery for a specific phone number and method
    /// </summary>
    public class DeliveryStatus
    {
        public string PhoneNumber { get; set; }
        public string Method { get; set; } // "SMS" or "WhatsApp"
        public string Status { get; set; } // "Sent", "Failed", "Pending"
        public string ErrorMessage { get; set; }
    }
}
