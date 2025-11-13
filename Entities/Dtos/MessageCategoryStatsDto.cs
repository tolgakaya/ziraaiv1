namespace Entities.Dtos
{
    /// <summary>
    /// Statistics for a specific message category
    /// </summary>
    public class MessageCategoryStatsDto
    {
        /// <summary>
        /// Number of messages sent in this category
        /// </summary>
        public int Sent { get; set; }

        /// <summary>
        /// Number of messages that received responses
        /// </summary>
        public int Responded { get; set; }

        /// <summary>
        /// Response/conversion rate percentage (0-100)
        /// </summary>
        public decimal ConversionRate { get; set; }
    }
}
