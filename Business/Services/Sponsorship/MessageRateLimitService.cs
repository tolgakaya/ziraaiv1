using System;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Abstract;

namespace Business.Services.Sponsorship
{
    /// <summary>
    /// Implementation of message rate limiting service
    /// Enforces 10 messages per day per farmer for L/XL tier sponsors
    /// </summary>
    public class MessageRateLimitService : IMessageRateLimitService
    {
        private readonly IAnalysisMessageRepository _messageRepository;
        private const int DAILY_MESSAGE_LIMIT = 10;

        public MessageRateLimitService(IAnalysisMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task<bool> CanSendMessageToFarmerAsync(int sponsorId, int farmerId)
        {
            var todayCount = await GetTodayMessageCountAsync(sponsorId, farmerId);
            return todayCount < DAILY_MESSAGE_LIMIT;
        }

        public async Task<int> GetTodayMessageCountAsync(int sponsorId, int farmerId)
        {
            var today = DateTime.Now.Date;
            var tomorrow = today.AddDays(1);

            // Get all messages sent from sponsor to farmer today
            var messages = await _messageRepository.GetListAsync(m =>
                m.FromUserId == sponsorId &&
                m.ToUserId == farmerId &&
                m.SentDate >= today &&
                m.SentDate < tomorrow &&
                m.SenderRole == "Sponsor"
            );

            return messages?.Count() ?? 0;
        }

        public async Task<int> GetRemainingMessagesAsync(int sponsorId, int farmerId)
        {
            var todayCount = await GetTodayMessageCountAsync(sponsorId, farmerId);
            var remaining = DAILY_MESSAGE_LIMIT - todayCount;
            return remaining > 0 ? remaining : 0;
        }
    }
}
