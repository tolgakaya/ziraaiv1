using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    /// <summary>
    /// Service for rate limiting sponsor-farmer messaging
    /// Enforces 10 messages per day per farmer limit for L/XL tier sponsors
    /// </summary>
    public interface IMessageRateLimitService
    {
        /// <summary>
        /// Checks if sponsor can send a message to farmer based on daily rate limit (10 messages/day)
        /// </summary>
        /// <param name="sponsorId">Sponsor user ID</param>
        /// <param name="farmerId">Farmer user ID</param>
        /// <returns>True if under rate limit, false if limit exceeded</returns>
        Task<bool> CanSendMessageToFarmerAsync(int sponsorId, int farmerId);

        /// <summary>
        /// Gets the number of messages sent today from sponsor to farmer
        /// </summary>
        /// <param name="sponsorId">Sponsor user ID</param>
        /// <param name="farmerId">Farmer user ID</param>
        /// <returns>Count of messages sent today</returns>
        Task<int> GetTodayMessageCountAsync(int sponsorId, int farmerId);

        /// <summary>
        /// Gets remaining messages sponsor can send to farmer today
        /// </summary>
        /// <param name="sponsorId">Sponsor user ID</param>
        /// <param name="farmerId">Farmer user ID</param>
        /// <returns>Remaining message count (0-10)</returns>
        Task<int> GetRemainingMessagesAsync(int sponsorId, int farmerId);
    }
}
