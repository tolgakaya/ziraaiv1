using Core.Utilities.Results;
using Entities.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Business.Handlers.Sponsorship.Queries
{
    /// <summary>
    /// Query for authenticated farmer's own sponsorship inbox
    /// Uses UserId from JWT token to fetch user's codes
    /// SECURITY: Farmer can only see their own codes
    /// </summary>
    public class GetFarmerSponsorshipInboxQuery : IRequest<IDataResult<List<FarmerSponsorshipInboxDto>>>
    {
        /// <summary>
        /// User ID from JWT token (farmer's ID)
        /// Handler will lookup user's phone number and fetch codes
        /// </summary>
        public int UserId { get; set; }

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
