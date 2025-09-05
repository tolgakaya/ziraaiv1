using Core.Entities;
using Core.Entities.Concrete;
using System;

namespace Entities.Concrete
{
    public class SponsorContact : IEntity
    {
        public int Id { get; set; }
        public int SponsorId { get; set; }
        public string ContactName { get; set; }
        public string PhoneNumber { get; set; }
        public string Source { get; set; }         // Manual, WhatsAppAPI
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        
        // Navigation properties
        public virtual User Sponsor { get; set; }
    }
}