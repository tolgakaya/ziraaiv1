using System;
using System.Collections.Generic;

namespace Entities.Dtos
{
    public class SponsorRequestDto
    {
        public int Id { get; set; }
        public int FarmerId { get; set; }
        public int SponsorId { get; set; }
        public string FarmerPhone { get; set; }
        public string SponsorPhone { get; set; }
        public string FarmerName { get; set; }
        public string RequestMessage { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public int? ApprovedSubscriptionTierId { get; set; }
        public string ApprovalNotes { get; set; }
        public string GeneratedSponsorshipCode { get; set; }
    }

    public class CreateSponsorRequestDto
    {
        public string SponsorPhone { get; set; }
        public string RequestMessage { get; set; }
        public int RequestedTierId { get; set; }
    }

    public class ApproveSponsorRequestDto
    {
        public List<int> RequestIds { get; set; }
        public int SubscriptionTierId { get; set; }
        public string ApprovalNotes { get; set; }
    }

    public class ProcessDeeplinkDto
    {
        public string HashedToken { get; set; }
    }
}