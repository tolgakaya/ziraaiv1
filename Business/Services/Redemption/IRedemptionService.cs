using System.Threading.Tasks;
using Core.Utilities.Results;
using Entities.Concrete;
using UserEntity = Core.Entities.Concrete.User;

namespace Business.Services.Redemption
{
    public interface IRedemptionService
    {
        /// <summary>
        /// Track when a redemption link is clicked
        /// </summary>
        Task TrackLinkClickAsync(string code, string ipAddress);

        /// <summary>
        /// Validate if a sponsorship code is valid and can be redeemed
        /// </summary>
        Task<IResult> ValidateCodeAsync(string code);

        /// <summary>
        /// Validate if a sponsorship code is valid and can be redeemed, with user context check
        /// </summary>
        Task<IResult> ValidateCodeWithUserAsync(string code, Microsoft.AspNetCore.Http.HttpContext httpContext);

        /// <summary>
        /// Find existing user by sponsorship code (using phone number)
        /// </summary>
        Task<UserEntity> FindUserByCodeAsync(string code);

        /// <summary>
        /// Create a new user account from sponsorship code information
        /// </summary>
        Task<IDataResult<UserEntity>> CreateAccountFromCodeAsync(string code);

        /// <summary>
        /// Activate subscription for a user using sponsorship code
        /// </summary>
        Task<IDataResult<UserSubscription>> ActivateSubscriptionAsync(string code, int userId);

        /// <summary>
        /// Generate JWT token for auto-login after successful redemption
        /// </summary>
        Task<string> GenerateAutoLoginTokenAsync(int userId);

        /// <summary>
        /// Generate redemption link for a sponsorship code
        /// </summary>
        Task<string> GenerateRedemptionLinkAsync(string code);

        /// <summary>
        /// Send sponsorship link via SMS or WhatsApp
        /// </summary>
        Task<IResult> SendSponsorshipLinkAsync(
            string code,
            string recipientPhone,
            string recipientName,
            string channel = "SMS");

        /// <summary>
        /// Send multiple sponsorship links in bulk
        /// </summary>
        Task<IDataResult<BulkSendResult>> SendBulkSponsorshipLinksAsync(
            BulkSendRequest request);
    }

    public class BulkSendRequest
    {
        public int SponsorId { get; set; }
        public RecipientInfo[] Recipients { get; set; }
        public string Channel { get; set; } = "SMS"; // SMS or WhatsApp
        public string CustomMessage { get; set; }
    }

    public class RecipientInfo
    {
        public string Code { get; set; }
        public string Phone { get; set; }
        public string Name { get; set; }
    }

    public class BulkSendResult
    {
        public int TotalSent { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public SendResult[] Results { get; set; }
    }

    public class SendResult
    {
        public string Code { get; set; }
        public string Phone { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string DeliveryStatus { get; set; }
    }
}