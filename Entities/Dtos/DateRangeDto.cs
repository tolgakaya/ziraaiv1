using System;

namespace Entities.Dtos
{
    /// <summary>
    /// DTO for date range filters in analytics queries
    /// </summary>
    public class DateRangeDto
    {
        /// <summary>
        /// Start date of the range (inclusive)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date of the range (inclusive)
        /// </summary>
        public DateTime? EndDate { get; set; }
    }
}
