namespace Business.Services.Notification.Models
{
    /// <summary>
    /// Notification delivery channels supported by the system
    /// </summary>
    public enum NotificationChannel
    {
        /// <summary>
        /// SMS text messaging
        /// </summary>
        SMS = 1,

        /// <summary>
        /// WhatsApp messaging via WhatsApp Business API
        /// </summary>
        WhatsApp = 2,

        /// <summary>
        /// Email notifications
        /// </summary>
        Email = 3,

        /// <summary>
        /// Push notifications for mobile apps
        /// </summary>
        Push = 4,

        /// <summary>
        /// In-app notifications
        /// </summary>
        InApp = 5
    }
}