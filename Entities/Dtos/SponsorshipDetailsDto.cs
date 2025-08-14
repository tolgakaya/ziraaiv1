using Core.Entities;

namespace Entities.Dtos
{
    public class SponsorshipDetailsDto : IDto
    {
        public string SponsorId { get; set; }           // String format: S001, S002, etc.
        public int? SponsorUserId { get; set; }          // Actual sponsor user ID
        public int SponsorshipCodeId { get; set; }      // SponsorshipCode table ID
        public string SponsorshipCode { get; set; }     // The actual code used
        public bool HasSponsor { get; set; }            // Quick check if user has sponsor
    }
}