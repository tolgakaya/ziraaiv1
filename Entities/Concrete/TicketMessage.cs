using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.Text.Json.Serialization;

namespace Entities.Concrete
{
    public class TicketMessage : IEntity
    {
        public int Id { get; set; }

        // Message Context
        public int TicketId { get; set; }
        public int FromUserId { get; set; }

        // Message Content
        public string Message { get; set; }
        public bool IsAdminResponse { get; set; }
        public bool IsInternal { get; set; }  // Admin internal note (not visible to user)

        // Message Status
        public bool IsRead { get; set; }
        public DateTime? ReadDate { get; set; }

        // Timestamps
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        [JsonIgnore]
        public virtual Ticket Ticket { get; set; }

        [JsonIgnore]
        public virtual User FromUser { get; set; }
    }
}
