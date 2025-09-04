using Core.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Concrete
{
    [Table("SecurityEvents")]
    public class SecurityEvent : IEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string EventType { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [StringLength(20)]
        public string Severity { get; set; } // "Low", "Medium", "High", "Critical"

        [StringLength(50)]
        public string UserId { get; set; }

        [StringLength(45)]
        public string IpAddress { get; set; }

        [StringLength(500)]
        public string UserAgent { get; set; }

        [Column(TypeName = "jsonb")]
        public string EventData { get; set; } // JSON serialized data

        public DateTime OccurredAt { get; set; } = DateTime.Now;

        public bool IsProcessed { get; set; } = false;

        [StringLength(50)]
        public string ProcessedBy { get; set; }

        public DateTime? ProcessedAt { get; set; }

        [StringLength(500)]
        public string ProcessingNotes { get; set; }
    }
}