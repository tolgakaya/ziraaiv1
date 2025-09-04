namespace Business.Constants
{
    public static partial class Messages
    {
        // Sponsor Profile Messages
        public static string SponsorProfileAlreadyExists => "Sponsor profile already exists";
        public static string SponsorProfileNotFound => "Sponsor profile not found";
        public static string SponsorProfileCreated => "Sponsor profile created successfully";
        public static string SponsorProfileUpdated => "Sponsor profile updated successfully";
        public static string SubscriptionTierNotFound => "Subscription tier not found";

        // Messaging Messages
        public static string MessagingNotAllowed => "Messaging is not allowed for your subscription tier";
        public static string MessageSent => "Message sent successfully";
        public static string MessageSendFailed => "Failed to send message";
        public static string MessageNotFound => "Message not found";
        public static string MessageMarkedAsRead => "Message marked as read";
        public static string ConversationMarkedAsRead => "Conversation marked as read";
        public static string MessageDeleted => "Message deleted successfully";

        // Smart Link Messages
        public static string SmartLinkNotAllowed => "Smart links are not allowed for your subscription tier";
        public static string SmartLinkQuotaExceeded => "Smart link quota exceeded";
        public static string SmartLinkCreated => "Smart link created successfully";
        public static string SmartLinkCreationFailed => "Failed to create smart link";
        public static string SmartLinkUpdated => "Smart link updated successfully";
        public static string SmartLinkDeleted => "Smart link deleted successfully";
        public static string SmartLinkNotFound => "Smart link not found";
        public static string SmartLinkApproved => "Smart link approved successfully";

        // Data Access Messages
        public static string AccessDenied => "Access denied to this analysis data";
        public static string DataAccessRecorded => "Data access recorded successfully";
        public static string InsufficientPermissions => "Insufficient permissions for this operation";

        // General Sponsorship Messages
        public static string SponsorNotVerified => "Sponsor account not verified";
        public static string SponsorAccountInactive => "Sponsor account is inactive";
        public static string InvalidSponsorTier => "Invalid sponsor tier";
    }
}