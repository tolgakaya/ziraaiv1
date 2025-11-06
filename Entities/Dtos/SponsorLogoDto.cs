using System;

namespace Entities.Dtos
{
    public class SponsorLogoDto
    {
        public int SponsorId { get; set; }
        public string LogoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
