using Core.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace Entities.Dtos
{
    public class ConfigurationDto : IDto
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
        public string ValueType { get; set; }
        
        public bool IsActive { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public DateTime? UpdatedDate { get; set; }
    }

    public class CreateConfigurationDto : IDto
    {
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
        public string ValueType { get; set; }
    }

    public class UpdateConfigurationDto : IDto
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Value { get; set; }
        
        [MaxLength(1000)]
        public string Description { get; set; }
        
        public bool IsActive { get; set; }
    }
}