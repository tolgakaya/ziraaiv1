namespace Entities.Dtos
{
    /// <summary>
    /// Analysis of message effectiveness by time of day
    /// </summary>
    public class TimeSlotAnalysisDto
    {
        /// <summary>
        /// Number of messages sent in this time slot
        /// </summary>
        public int MessagesSent { get; set; }

        /// <summary>
        /// Response rate for this time slot (0-1)
        /// </summary>
        public decimal ResponseRate { get; set; }

        /// <summary>
        /// Recommendation for this time slot
        /// </summary>
        public string BestFor { get; set; }
    }
}
