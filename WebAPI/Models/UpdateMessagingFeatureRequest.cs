namespace WebAPI.Models
{
    /// <summary>
    /// Request model for updating messaging feature toggle
    /// </summary>
    public class UpdateMessagingFeatureRequest
    {
        /// <summary>
        /// Feature enabled/disabled state
        /// </summary>
        public bool IsEnabled { get; set; }
    }
}
