using System.Collections.Generic;
using Entities.Concrete;

namespace Entities.Dtos
{
    /// <summary>
    /// Paginated result for sponsorship codes listing
    /// </summary>
    public class SponsorshipCodesPaginatedDto
    {
        /// <summary>
        /// List of sponsorship codes for the current page
        /// </summary>
        public List<SponsorshipCode> Items { get; set; }

        /// <summary>
        /// List of sponsorship code DTOs (for dealer endpoints)
        /// </summary>
        public List<SponsorshipCodeDto> Codes { get; set; }

        /// <summary>
        /// Total number of codes across all pages
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Indicates if there is a previous page
        /// </summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// Indicates if there is a next page
        /// </summary>
        public bool HasNextPage => Page < TotalPages;

        public SponsorshipCodesPaginatedDto()
        {
            Items = new List<SponsorshipCode>();
        }
    }
}
