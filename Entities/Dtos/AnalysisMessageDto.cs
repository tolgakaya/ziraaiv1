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
        public bool IsRead { get; set; }
        public DateTime SentDate { get; set; }
        public DateTime? ReadDate { get; set; }
        public string SenderRole { get; set; }
        public string SenderName { get; set; }
        public string SenderCompany { get; set; }
        public string Priority { get; set; }
        public string Category { get; set; }
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