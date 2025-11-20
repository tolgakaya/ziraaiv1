using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    // Request DTOs
    public class CreateTicketDto
    {
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }  // Technical, Billing, Account, General
        public string Priority { get; set; }  // Low, Normal, High (default: Normal)
    }

    public class AddTicketMessageDto
    {
        public int TicketId { get; set; }
        public string Message { get; set; }
    }

    public class AdminRespondTicketDto
    {
        public int TicketId { get; set; }
        public string Message { get; set; }
        public bool IsInternal { get; set; }  // Internal note flag
    }

    public class UpdateTicketStatusDto
    {
        public int TicketId { get; set; }
        public string Status { get; set; }
        public string ResolutionNotes { get; set; }
    }

    public class RateTicketResolutionDto
    {
        public int TicketId { get; set; }
        public int Rating { get; set; }  // 1-5
        public string Feedback { get; set; }
    }

    // Response DTOs
    public class TicketListDto
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastResponseDate { get; set; }
        public bool HasUnreadMessages { get; set; }
    }

    public class TicketDetailDto
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string ResolutionNotes { get; set; }
        public int? SatisfactionRating { get; set; }
        public string SatisfactionFeedback { get; set; }
        public List<TicketMessageDto> Messages { get; set; }
    }

    public class TicketMessageDto
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string FromUserName { get; set; }
        public bool IsAdminResponse { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class AdminTicketListDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserRole { get; set; }
        public string Subject { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public int? AssignedToUserId { get; set; }
        public string AssignedToUserName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastResponseDate { get; set; }
        public int MessageCount { get; set; }
    }

    public class AdminTicketDetailDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserRole { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public int? AssignedToUserId { get; set; }
        public string AssignedToUserName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string ResolutionNotes { get; set; }
        public int? SatisfactionRating { get; set; }
        public string SatisfactionFeedback { get; set; }
        public List<AdminTicketMessageDto> Messages { get; set; }
    }

    public class AdminTicketMessageDto
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public int FromUserId { get; set; }
        public string FromUserName { get; set; }
        public bool IsAdminResponse { get; set; }
        public bool IsInternal { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class TicketStatsDto
    {
        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ClosedCount { get; set; }
        public int TotalCount { get; set; }
    }

    public class TicketListResponseDto
    {
        public List<TicketListDto> Tickets { get; set; }
        public int TotalCount { get; set; }
    }

    public class AdminTicketListResponseDto
    {
        public List<AdminTicketListDto> Tickets { get; set; }
        public int TotalCount { get; set; }
    }
}
