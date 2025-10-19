using Core.Entities;
using System;

namespace Entities.Dtos
{
    public class AnalysisMessageDto : IDto
    {
        public int Id { get; set; }
        public int PlantAnalysisId { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public string Message { get; set; }
        public string MessageType { get; set; }
        public string Subject { get; set; }
        
        // Message Status
        public string MessageStatus { get; set; } // Sent, Delivered, Read
        public bool IsRead { get; set; }
        public DateTime SentDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? ReadDate { get; set; }
        
        // Sender Information
        public string SenderRole { get; set; }
        public string SenderName { get; set; }
        public string SenderCompany { get; set; }

        // Sender Avatar (Phase 1A)
        public string SenderAvatarUrl { get; set; }
        public string SenderAvatarThumbnailUrl { get; set; }

        // Receiver Information
        public string ReceiverRole { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverCompany { get; set; }

        // Receiver Avatar
        public string ReceiverAvatarUrl { get; set; }
        public string ReceiverAvatarThumbnailUrl { get; set; }
        
        // Message Classification
        public string Priority { get; set; }
        public string Category { get; set; }
        
        // Attachment Support (Phase 2A)
        public bool HasAttachments { get; set; }
        public int AttachmentCount { get; set; }
        public string[] AttachmentUrls { get; set; }
        public string[] AttachmentTypes { get; set; }
        public long[] AttachmentSizes { get; set; }
        public string[] AttachmentNames { get; set; }
        
        // Voice Message Support (Phase 2B)
        public bool IsVoiceMessage { get; set; }
        public string VoiceMessageUrl { get; set; }
        public int? VoiceMessageDuration { get; set; }
        public string VoiceMessageWaveform { get; set; }
        
        // Edit/Delete/Forward Support (Phase 4)
        public bool IsEdited { get; set; }
        public DateTime? EditedDate { get; set; }
        public bool IsForwarded { get; set; }
        public int? ForwardedFromMessageId { get; set; }
        public bool IsActive { get; set; }
    }

    public class SendMessageDto : IDto
    {
        public int PlantAnalysisId { get; set; }
        public int ToUserId { get; set; }
        public string Message { get; set; }
        public string MessageType { get; set; } = "Information";
        public string Subject { get; set; }
        public string Priority { get; set; } = "Normal";
        public string Category { get; set; } = "General";
    }

    public class MessageThreadDto : IDto
    {
        public int PlantAnalysisId { get; set; }
        public string PlantCropType { get; set; }
        public string PlantStatus { get; set; }
        public int TotalMessages { get; set; }
        public int UnreadCount { get; set; }
        public DateTime LastMessageDate { get; set; }
        public string OtherParticipantName { get; set; }
        public string OtherParticipantCompany { get; set; }
    }
}