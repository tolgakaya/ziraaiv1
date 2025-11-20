using System;
using Core.Entities;
using Core.Entities.Concrete;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for updating farmer profile
    /// UserId is not included - will be taken from JWT token
    /// </summary>
    public class UpdateFarmerProfileDto : IDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string MobilePhones { get; set; }
        public DateTime? BirthDate { get; set; }
        public int? Gender { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
    }
}
