namespace Entities.Dtos
{
    /// <summary>
    /// Performance metrics for message templates
    /// </summary>
    public class MessageTemplatePerformanceDto
    {
        /// <summary>
        /// Type/category of message
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Message template or pattern
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// Response rate for this template (0-1)
        /// </summary>
        public decimal ResponseRate { get; set; }

        /// <summary>
        /// Average response time in hours
        /// </summary>
        public decimal AvgResponseTime { get; set; }

        /// <summary>
        /// Conversion rate for this template (0-1)
        /// </summary>
        public decimal ConversionRate { get; set; }

        /// <summary>
        /// Number of times this template was used
        /// </summary>
        public int UsageCount { get; set; }
    }
}
