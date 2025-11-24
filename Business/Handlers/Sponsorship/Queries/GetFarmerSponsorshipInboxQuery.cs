using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Query to get all sponsorship codes sent to a farmer's phone number
    /// Pattern: Similar to GetDealerInvitationsQuery
    /// No authentication required - public endpoint using phone as identifier
    /// </summary>
    public class GetFarmerSponsorshipInboxQuery : IRequest<IDataResult<List<FarmerSponsorshipInboxDto>>>
    {
        /// <summary>
        /// Farmer's phone number (will be normalized to +905551234567 format)
        /// Supports formats: 05551234567, +905551234567, 555 123 45 67
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Include already redeemed codes in results
        /// Default: false (show only active/unused codes)
        /// </summary>
        public bool IncludeUsed { get; set; } = false;

        /// <summary>
        /// Include expired codes in results
        /// Default: false (show only non-expired codes)
        /// </summary>
        public bool IncludeExpired { get; set; } = false;
    }
}
