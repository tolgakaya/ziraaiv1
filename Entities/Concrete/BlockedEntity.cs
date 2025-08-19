using Core.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Concrete
{
    [Table("BlockedEntities")]
    public class BlockedEntity : IEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } // "IP", "User", "Device", "Phone"

        [Required]
        [StringLength(200)]
        public string EntityValue { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; }

        [Required]
        [StringLength(20)]
        public string Severity { get; set; } // "Low", "Medium", "High", "Critical"

        public DateTime BlockedAt { get; set; } = DateTime.Now;

        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        public string BlockedBy { get; set; }

        [Column(TypeName = "jsonb")]
        public string Metadata { get; set; } // JSON serialized metadata

        [StringLength(500)]
        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}