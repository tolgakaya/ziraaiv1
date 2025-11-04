using System;

namespace Entities.Dtos
{
    /// <summary>
    /// Response DTO for bulk invitation job creation
    /// Returned to client after Excel upload
    /// </summary>
    public class BulkInvitationJobDto
    {
        public int JobId { get; set; }
        public int TotalDealers { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string StatusCheckUrl { get; set; }
    }
}
