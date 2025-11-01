using System;

namespace Entities.Dtos
{
    public class DealerInvitationSummaryDto
    {
        public int InvitationId { get; set; }
        public string Token { get; set; }
        public string SponsorCompanyName { get; set; }
        public int CodeCount { get; set; }
        public string PackageTier { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int RemainingDays { get; set; }
        public string Status { get; set; }
        public string DealerEmail { get; set; }
        public string DealerPhone { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PendingInvitationsResponseDto
    {
        public System.Collections.Generic.List<DealerInvitationSummaryDto> Invitations { get; set; }
        public int TotalCount { get; set; }
    }
}
