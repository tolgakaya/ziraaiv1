namespace Entities.Dtos
{
    /// <summary>
    /// Breakdown of messages by category/type
    /// </summary>
    public class MessageBreakdownDto
    {
        /// <summary>
        /// Product recommendation messages
        /// </summary>
        public MessageCategoryStatsDto ProductRecommendations { get; set; }

        /// <summary>
        /// General query messages
        /// </summary>
        public MessageCategoryStatsDto GeneralQueries { get; set; }

        /// <summary>
        /// Follow-up messages
        /// </summary>
        public MessageCategoryStatsDto FollowUps { get; set; }

        public MessageBreakdownDto()
        {
            ProductRecommendations = new MessageCategoryStatsDto();
            GeneralQueries = new MessageCategoryStatsDto();
            FollowUps = new MessageCategoryStatsDto();
        }
    }
}
