using System.Collections.Generic;

namespace Entities.Dtos
{
    /// <summary>
    /// Response DTO for messaging features configuration
    /// Sent to mobile app on startup to configure available features
    /// </summary>
    public class MessagingFeaturesDto
    {
        public MessagingFeatureDto VoiceMessages { get; set; }
        public MessagingFeatureDto ImageAttachments { get; set; }
        public MessagingFeatureDto VideoAttachments { get; set; }
        public MessagingFeatureDto FileAttachments { get; set; }
        public MessagingFeatureDto MessageEdit { get; set; }
        public MessagingFeatureDto MessageDelete { get; set; }
        public MessagingFeatureDto MessageForward { get; set; }
        public MessagingFeatureDto TypingIndicator { get; set; }
        public MessagingFeatureDto LinkPreview { get; set; }
    }

    /// <summary>
    /// Individual feature configuration with availability check
    /// </summary>
    public class MessagingFeatureDto
    {
        /// <summary>
        /// Admin toggle - is this feature enabled globally?
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Is this feature available to the current user based on their tier?
        /// </summary>
        public bool Available { get; set; }

        /// <summary>
        /// Minimum tier required (S, M, L, XL, or None)
        /// </summary>
        public string RequiredTier { get; set; }

        /// <summary>
        /// Maximum file size in bytes
        /// </summary>
        public long? MaxFileSize { get; set; }

        /// <summary>
        /// Maximum duration in seconds
        /// </summary>
        public int? MaxDuration { get; set; }

        /// <summary>
        /// Allowed MIME types
        /// </summary>
        public List<string> AllowedTypes { get; set; }

        /// <summary>
        /// Time limit for action in seconds (edit/delete)
        /// </summary>
        public int? TimeLimit { get; set; }

        /// <summary>
        /// User-friendly reason why feature is unavailable
        /// </summary>
        public string UnavailableReason { get; set; }
    }
}
