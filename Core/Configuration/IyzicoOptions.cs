using System.ComponentModel.DataAnnotations;

namespace Core.Configuration
{
    /// <summary>
    /// Configuration options for iyzico payment gateway integration
    /// </summary>
    public class IyzicoOptions
    {
        public const string SectionName = "Iyzico";

        /// <summary>
        /// iyzico API base URL (sandbox or production)
        /// </summary>
        [Required]
        public string BaseUrl { get; set; } = "https://sandbox-api.iyzipay.com";

        /// <summary>
        /// iyzico API Key from merchant dashboard
        /// </summary>
        [Required]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// iyzico Secret Key from merchant dashboard
        /// </summary>
        [Required]
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Default currency for payment transactions (TRY, USD, EUR, GBP)
        /// </summary>
        public string Currency { get; set; } = "TRY";

        /// <summary>
        /// Payment channel for mobile applications (MOBILE, MOBILE_IOS, MOBILE_ANDROID)
        /// </summary>
        public string PaymentChannel { get; set; } = "MOBILE";

        /// <summary>
        /// Payment group for transactions (PRODUCT, LISTING, SUBSCRIPTION)
        /// </summary>
        public string PaymentGroup { get; set; } = "SUBSCRIPTION";

        /// <summary>
        /// Payment token expiration time in minutes (default: 30 minutes)
        /// </summary>
        public int TokenExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// Callback configuration for payment completion
        /// </summary>
        public IyzicoCallbackSettings Callback { get; set; } = new();

        /// <summary>
        /// Timeout settings for iyzico API calls
        /// </summary>
        public IyzicoTimeoutSettings Timeout { get; set; } = new();

        /// <summary>
        /// Retry settings for failed API calls
        /// </summary>
        public IyzicoRetrySettings Retry { get; set; } = new();
    }

    public class IyzicoCallbackSettings
    {
        /// <summary>
        /// Deep link scheme for mobile app callbacks (e.g., ziraai://payment-callback)
        /// </summary>
        [Required]
        public string DeepLinkScheme { get; set; } = "ziraai://payment-callback";

        /// <summary>
        /// Fallback URL if deep link fails
        /// </summary>
        public string FallbackUrl { get; set; } = string.Empty;
    }

    public class IyzicoTimeoutSettings
    {
        /// <summary>
        /// Timeout for Initialize API call in seconds
        /// </summary>
        public int InitializeTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Timeout for Verify API call in seconds
        /// </summary>
        public int VerifyTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Timeout for Webhook processing in seconds
        /// </summary>
        public int WebhookTimeoutSeconds { get; set; } = 15;
    }

    public class IyzicoRetrySettings
    {
        /// <summary>
        /// Maximum number of retry attempts for failed API calls
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in milliseconds
        /// </summary>
        public int RetryDelayMilliseconds { get; set; } = 1000;

        /// <summary>
        /// Enable exponential backoff for retries
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;
    }
}
