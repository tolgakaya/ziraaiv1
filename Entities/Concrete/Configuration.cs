using Core.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace Entities.Concrete
{
    public class Configuration : IEntity
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Key { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Value { get; set; }
        
        [MaxLength(1000)]
        public string Description { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Category { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string ValueType { get; set; } // int, decimal, bool, string
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedDate { get; set; }
        
        public int? CreatedBy { get; set; }
        
        public int? UpdatedBy { get; set; }
    }
}