using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.Text.Json.Serialization;

namespace Entities.Concrete
{
    /// <summary>
    /// Messaging system feature flags for admin-controlled on/off switches
    /// Enables dynamic feature management without requiring app updates
    /// </summary>
    public class MessagingFeature : IEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// Unique feature identifier (e.g., "VoiceMessages", "ImageAttachments")
        /// </summary>
        public string FeatureName { get; set; }

        /// <summary>
        /// Display name for admin panel
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Admin-controlled feature toggle
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Minimum subscription tier required (None, S, M, L, XL)
        /// </summary>
        public string RequiredTier { get; set; }

        /// <summary>
        /// Maximum file size in bytes (for attachments/voice)
        /// </summary>
        public long? MaxFileSize { get; set; }

        /// <summary>
        /// Maximum duration in seconds (for voice/video messages)
        /// </summary>
        public int? MaxDuration { get; set; }

        /// <summary>
        /// Comma-separated allowed MIME types (e.g., "image/jpeg,image/png")
        /// </summary>
        public string AllowedMimeTypes { get; set; }

        /// <summary>
        /// Time limit in seconds for actions (e.g., edit within 1 hour, delete within 24 hours)
        /// </summary>
        public int? TimeLimit { get; set; }

        /// <summary>
        /// Feature description for admin reference
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Additional JSON configuration for future extensibility
        /// </summary>
        public string ConfigurationJson { get; set; }

        // Audit fields
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? UpdatedByUserId { get; set; }

        // Navigation properties
        [JsonIgnore]
        public virtual User CreatedByUser { get; set; }

        [JsonIgnore]
        public virtual User UpdatedByUser { get; set; }
    }
}
