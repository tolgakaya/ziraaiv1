using System;
using System.Linq;
using System.Threading.Tasks;
using Business.Services.Configuration;
using DataAccess.Abstract;
using Entities.Constants;

namespace Business.Services.Sponsorship
{
    /// <summary>
    /// Implementation of message rate limiting service
    /// Enforces configurable daily message limit per farmer for L/XL tier sponsors
    /// Default: 10 messages per day (configurable via database)
    /// </summary>
    public class MessageRateLimitService : IMessageRateLimitService
    {
        private readonly IAnalysisMessageRepository _messageRepository;
        private readonly IConfigurationService _configurationService;

        public MessageRateLimitService(
            IAnalysisMessageRepository messageRepository,
            IConfigurationService configurationService)
        {
            _messageRepository = messageRepository;
            _configurationService = configurationService;
        }

        public async Task<bool> CanSendMessageToFarmerAsync(int sponsorId, int farmerId)
        {
            var dailyLimit = await _configurationService.GetIntValueAsync(
                ConfigurationKeys.Messaging.DailyMessageLimitPerFarmer, 10);
            
            var todayCount = await GetTodayMessageCountAsync(sponsorId, farmerId);
            return todayCount < dailyLimit;
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
            var dailyLimit = await _configurationService.GetIntValueAsync(
                ConfigurationKeys.Messaging.DailyMessageLimitPerFarmer, 10);
                
            var todayCount = await GetTodayMessageCountAsync(sponsorId, farmerId);
            var remaining = dailyLimit - todayCount;
            return remaining > 0 ? remaining : 0;
        }
    }
}
