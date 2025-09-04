using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.Text.Json.Serialization;

namespace Entities.Concrete
{
    public class AnalysisMessage : IEntity
    {
        public int Id { get; set; }
        
        // Message Context
        public int PlantAnalysisId { get; set; } // Related analysis
        public int FromUserId { get; set; } // Sender (sponsor or farmer)
        public int ToUserId { get; set; } // Recipient (farmer or sponsor)
        
        // Message Content
        public string Message { get; set; }
        public string MessageType { get; set; } // Question, Answer, Recommendation, Information
        public string Subject { get; set; } // Optional subject line
        public int? ParentMessageId { get; set; } // For threaded conversations
        
        // Message Status
        public bool IsRead { get; set; }
        public DateTime SentDate { get; set; }
        public DateTime? ReadDate { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? ArchivedDate { get; set; }
        
        // Sender Information
        public string SenderRole { get; set; } // Farmer, Sponsor, Admin
        public string SenderName { get; set; } // Cached for display
        public string SenderCompany { get; set; } // For sponsors
        
        // Attachments and Rich Content
        public string AttachmentUrls { get; set; } // JSON array of attachment URLs
        public bool HasAttachments { get; set; }
        public string LinkedProducts { get; set; } // JSON array of product IDs/links
        public string RecommendedActions { get; set; } // JSON array of action items
        
        // Priority and Classification
        public string Priority { get; set; } // Low, Normal, High, Urgent
        public string Category { get; set; } // Disease, Pest, Nutrient, General, Product
        public bool RequiresResponse { get; set; }
        public DateTime? ResponseDeadline { get; set; }
        
        // Tracking
        public bool IsImportant { get; set; }
        public bool IsFlagged { get; set; }
        public string FlagReason { get; set; }
        public int? Rating { get; set; } // 1-5 star rating for message quality
        public string RatingFeedback { get; set; }
        
        // Moderation
        public bool IsApproved { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int? ApprovedByUserId { get; set; }
        public string ModerationNotes { get; set; }
        
        // Notification Settings
        public bool EmailNotificationSent { get; set; }
        public DateTime? EmailSentDate { get; set; }
        public bool SmsNotificationSent { get; set; }
        public DateTime? SmsSentDate { get; set; }
        public bool PushNotificationSent { get; set; }
        public DateTime? PushSentDate { get; set; }
        
        // Audit
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public virtual PlantAnalysis PlantAnalysis { get; set; }
        
        [JsonIgnore]
        public virtual User FromUser { get; set; }
        
        [JsonIgnore]
        public virtual User ToUser { get; set; }
        
        [JsonIgnore]
        public virtual AnalysisMessage ParentMessage { get; set; }
        
        [JsonIgnore]
        public virtual User ApprovedByUser { get; set; }
    }
}