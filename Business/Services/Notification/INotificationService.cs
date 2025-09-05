using Business.Services.Notification.Models;
using Core.Utilities.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Notification
{
    /// <summary>
    /// Multi-channel notification service interface supporting WhatsApp, SMS, Email, and Push notifications
    /// </summary>
    public interface INotificationService
    {
        #region Template-based Notifications

        /// <summary>
        /// Send template-based notification using user's preferred channel
        /// </summary>
        /// <param name="request">Notification request with template details</param>
        /// <returns>Notification delivery result</returns>
        Task<IDataResult<NotificationResultDto>> SendTemplateNotificationAsync(NotificationRequestDto request);

        /// <summary>
        /// Send template-based notification via specific channel
        /// </summary>
        /// <param name="userId">Target user ID</param>
        /// <param name="phoneNumber">Recipient phone number</param>
        /// <param name="templateName">Template name</param>
        /// <param name="templateParameters">Template parameters</param>
        /// <param name="channel">Notification channel</param>
        /// <returns>Notification delivery result</returns>
        Task<IDataResult<NotificationResultDto>> SendTemplateNotificationAsync(
            int userId,
            string phoneNumber,
            string templateName,
            Dictionary<string, object> templateParameters,
            NotificationChannel channel);

        /// <summary>
        /// Send bulk template notifications to multiple recipients
        /// </summary>
        /// <param name="recipients">List of recipients</param>
        /// <param name="templateName">Template name</param>
        /// <param name="channel">Notification channel</param>
        /// <returns>List of delivery results</returns>
        Task<IDataResult<List<NotificationResultDto>>> SendBulkTemplateNotificationsAsync(
            List<BulkNotificationRecipientDto> recipients,
            string templateName,
            NotificationChannel channel);

        #endregion

        #region Plant Analysis Notifications

        /// <summary>
        /// Send plant analysis completion notification
        /// </summary>
        /// <param name="userId">Farmer user ID</param>
        /// <param name="phoneNumber">Farmer phone number</param>
        /// <param name="analysisId">Plant analysis ID</param>
        /// <param name="cropType">Crop type analyzed</param>
        /// <param name="healthScore">Overall health score</param>
        /// <param name="primaryConcern">Primary health concern</param>
        /// <param name="dashboardUrl">Link to view full results</param>
        /// <returns>Notification delivery result</returns>
        Task<IDataResult<NotificationResultDto>> SendAnalysisCompletedNotificationAsync(
            int userId,
            string phoneNumber,
            int analysisId,
            string cropType,
            int healthScore,
            string primaryConcern,
            string dashboardUrl);

        /// <summary>
        /// Send urgent plant health alert notification
        /// </summary>
        /// <param name="userId">Farmer user ID</param>
        /// <param name="phoneNumber">Farmer phone number</param>
        /// <param name="cropType">Affected crop type</param>
        /// <param name="urgentIssue">Description of urgent issue</param>
        /// <param name="recommendedAction">Immediate recommended action</param>
        /// <returns>Notification delivery result</returns>
        Task<IDataResult<NotificationResultDto>> SendUrgentHealthAlertNotificationAsync(
            int userId,
            string phoneNumber,
            string cropType,
            string urgentIssue,
            string recommendedAction);

        #endregion

        #region Subscription Notifications

        /// <summary>
        /// Send subscription usage alert notification
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="phoneNumber">User phone number</param>
        /// <param name="usagePercentage">Current usage percentage</param>
        /// <param name="limitType">Limit type (daily/monthly)</param>
        /// <param name="resetDate">Next reset date</param>
        /// <returns>Notification delivery result</returns>
        Task<IDataResult<NotificationResultDto>> SendUsageAlertNotificationAsync(
            int userId,
            string phoneNumber,
            int usagePercentage,
            string limitType,
            string resetDate);

        /// <summary>
        /// Send subscription expiry warning notification
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="phoneNumber">User phone number</param>
        /// <param name="subscriptionTier">Current subscription tier</param>
        /// <param name="expiryDate">Expiry date</param>
        /// <param name="daysRemaining">Days until expiry</param>
        /// <returns>Notification delivery result</returns>
        Task<IDataResult<NotificationResultDto>> SendSubscriptionExpiryWarningAsync(
            int userId,
            string phoneNumber,
            string subscriptionTier,
            string expiryDate,
            int daysRemaining);

        /// <summary>
        /// Send payment failure notification
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="phoneNumber">User phone number</param>
        /// <param name="failureReason">Payment failure reason</param>
        /// <param name="retryUrl">URL to retry payment</param>
        /// <returns>Notification delivery result</returns>
        Task<IDataResult<NotificationResultDto>> SendPaymentFailureNotificationAsync(
            int userId,
            string phoneNumber,
            string failureReason,
            string retryUrl);

        #endregion

        #region Sponsorship Notifications

        /// <summary>
        /// Send sponsorship link to farmer
        /// </summary>
        /// <param name="phoneNumber">Farmer phone number</param>
        /// <param name="farmerName">Farmer name</param>
        /// <param name="sponsorCompany">Sponsor company name</param>
        /// <param name="subscriptionTier">Subscription tier gifted</param>
        /// <param name="redemptionLink">Redemption link</param>
        /// <param name="expiryDate">Link expiry date</param>
        /// <param name="channel">Notification channel</param>
        /// <returns>Notification delivery result</returns>
        Task<IDataResult<NotificationResultDto>> SendSponsorshipLinkNotificationAsync(
            string phoneNumber,
            string farmerName,
            string sponsorCompany,
            string subscriptionTier,
            string redemptionLink,
            string expiryDate,
            NotificationChannel channel);

        /// <summary>
        /// Send sponsorship redemption confirmation
        /// </summary>
        /// <param name="phoneNumber">Farmer phone number</param>
        /// <param name="farmerName">Farmer name</param>
        /// <param name="sponsorCompany">Sponsor company</param>
        /// <param name="subscriptionTier">Activated subscription tier</param>
        /// <param name="amount">Sponsorship amount</param>
        /// <returns>Notification delivery result</returns>
        Task<IDataResult<NotificationResultDto>> SendRedemptionConfirmationNotificationAsync(
            string phoneNumber,
            string farmerName,
            string sponsorCompany,
            string subscriptionTier,
            decimal amount);

        #endregion

        #region System Notifications

        /// <summary>
        /// Send system maintenance notification
        /// </summary>
        /// <param name="userIds">List of user IDs to notify</param>
        /// <param name="maintenanceStart">Maintenance start time</param>
        /// <param name="estimatedDuration">Estimated maintenance duration</param>
        /// <returns>List of delivery results</returns>
        Task<IDataResult<List<NotificationResultDto>>> SendMaintenanceNotificationAsync(
            List<int> userIds,
            string maintenanceStart,
            string estimatedDuration);

        /// <summary>
        /// Send welcome notification to new users
        /// </summary>
        /// <param name="userId">New user ID</param>
        /// <param name="phoneNumber">User phone number</param>
        /// <param name="userName">User name</param>
        /// <param name="welcomeGuideUrl">Welcome guide URL</param>
        /// <returns>Notification delivery result</returns>
        Task<IDataResult<NotificationResultDto>> SendWelcomeNotificationAsync(
            int userId,
            string phoneNumber,
            string userName,
            string welcomeGuideUrl);

        #endregion

        #region Preferences and Management

        /// <summary>
        /// Get user notification preferences
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User notification preferences</returns>
        Task<IDataResult<NotificationPreferencesDto>> GetUserNotificationPreferencesAsync(int userId);

        /// <summary>
        /// Update user notification preferences
        /// </summary>
        /// <param name="preferences">Updated preferences</param>
        /// <returns>Update result</returns>
        Task<IResult> UpdateUserNotificationPreferencesAsync(NotificationPreferencesDto preferences);

        /// <summary>
        /// Get notification delivery statistics
        /// </summary>
        /// <param name="userId">User ID (optional, for user-specific stats)</param>
        /// <param name="fromDate">Start date for statistics</param>
        /// <param name="toDate">End date for statistics</param>
        /// <returns>Notification statistics</returns>
        Task<IDataResult<NotificationStatisticsDto>> GetNotificationStatisticsAsync(
            int? userId,
            System.DateTime fromDate,
            System.DateTime toDate);

        /// <summary>
        /// Test notification delivery for specific channel
        /// </summary>
        /// <param name="phoneNumber">Test phone number</param>
        /// <param name="channel">Channel to test</param>
        /// <param name="testMessage">Test message content</param>
        /// <returns>Test result</returns>
        Task<IDataResult<NotificationResultDto>> SendTestNotificationAsync(
            string phoneNumber,
            NotificationChannel channel,
            string testMessage);

        #endregion

        #region Health and Monitoring

        /// <summary>
        /// Check health of all notification channels
        /// </summary>
        /// <returns>Health status of all channels</returns>
        Task<IDataResult<Dictionary<NotificationChannel, bool>>> CheckChannelHealthAsync();

        /// <summary>
        /// Get failed notifications for retry
        /// </summary>
        /// <param name="hoursBack">Hours to look back for failed notifications</param>
        /// <returns>List of failed notifications</returns>
        Task<IDataResult<List<FailedNotificationDto>>> GetFailedNotificationsAsync(int hoursBack = 24);

        /// <summary>
        /// Retry failed notification
        /// </summary>
        /// <param name="notificationId">Failed notification ID</param>
        /// <returns>Retry result</returns>
        Task<IDataResult<NotificationResultDto>> RetryFailedNotificationAsync(int notificationId);

        #endregion
    }

    /// <summary>
    /// Bulk notification recipient information
    /// </summary>
    public class BulkNotificationRecipientDto
    {
        public int UserId { get; set; }
        public string PhoneNumber { get; set; }
        public string Name { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Notification delivery statistics
    /// </summary>
    public class NotificationStatisticsDto
    {
        public int TotalSent { get; set; }
        public int TotalDelivered { get; set; }
        public int TotalFailed { get; set; }
        public int TotalRead { get; set; }
        public Dictionary<NotificationChannel, int> ChannelBreakdown { get; set; } = new();
        public Dictionary<string, int> TemplateUsage { get; set; } = new();
        public double AverageDeliveryTime { get; set; }
        public decimal TotalCost { get; set; }
    }

    /// <summary>
    /// Failed notification information for retry
    /// </summary>
    public class FailedNotificationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string PhoneNumber { get; set; }
        public NotificationChannel Channel { get; set; }
        public string TemplateName { get; set; }
        public string ErrorMessage { get; set; }
        public System.DateTime FailedAt { get; set; }
        public int RetryCount { get; set; }
        public Dictionary<string, object> OriginalParameters { get; set; } = new();
    }
}