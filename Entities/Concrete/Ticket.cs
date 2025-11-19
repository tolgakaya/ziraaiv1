using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Entities.Concrete
{
    public class Ticket : IEntity
    {
        public int Id { get; set; }

        // Ticket Owner
        public int UserId { get; set; }
        public string UserRole { get; set; }  // "Farmer" or "Sponsor"

        // Ticket Content
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }  // Technical, Billing, Account, General
        public string Priority { get; set; }  // Low, Normal, High

        // Ticket Status
        public string Status { get; set; }  // Open, InProgress, Resolved, Closed
        public int? AssignedToUserId { get; set; }

        // Resolution
        public DateTime? ResolvedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string ResolutionNotes { get; set; }

        // Satisfaction
        public int? SatisfactionRating { get; set; }  // 1-5 stars
        public string SatisfactionFeedback { get; set; }

        // Timestamps
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime? LastResponseDate { get; set; }

        // Navigation properties
        [JsonIgnore]
        public virtual User User { get; set; }

        [JsonIgnore]
        public virtual User AssignedToUser { get; set; }

        [JsonIgnore]
        public virtual ICollection<TicketMessage> Messages { get; set; }
    }
}
