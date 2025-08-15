using Business.Services.Sponsorship;
using DataAccess.Abstract;
using Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public class AnalysisMessagingService : IAnalysisMessagingService
    {
        private readonly IAnalysisMessageRepository _messageRepository;
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly IUserRepository _userRepository;

        public AnalysisMessagingService(
            IAnalysisMessageRepository messageRepository,
            ISponsorProfileRepository sponsorProfileRepository,
            IUserRepository userRepository)
        {
            _messageRepository = messageRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
            _userRepository = userRepository;
        }

        public async Task<bool> CanSendMessageAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (profile == null || !profile.IsActive || !profile.IsVerified)
                return false;

            // Sadece L ve XL paketleri mesajlaşma hakkına sahip
            return profile.HasMessaging;
        }

        public async Task<AnalysisMessage> SendMessageAsync(int fromUserId, int toUserId, int plantAnalysisId, string message, string messageType = "Information")
        {
            // Gönderen kullanıcının mesajlaşma yetkisi var mı kontrol et
            var fromUser = await _userRepository.GetAsync(u => u.UserId == fromUserId);
            var toUser = await _userRepository.GetAsync(u => u.UserId == toUserId);
            
            if (fromUser == null || toUser == null)
                return null;

            // Sponsor ise mesajlaşma yetkisi kontrolü
            var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(fromUserId);
            if (sponsorProfile != null && !await CanSendMessageAsync(fromUserId))
                return null;

            var newMessage = new AnalysisMessage
            {
                PlantAnalysisId = plantAnalysisId,
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Message = message,
                MessageType = messageType,
                SentDate = DateTime.Now,
                IsRead = false,
                IsApproved = true, // Auto-approve for verified sponsors and farmers
                SenderRole = sponsorProfile != null ? "Sponsor" : "Farmer",
                SenderName = fromUser.FullName,
                SenderCompany = sponsorProfile?.CompanyName,
                Priority = "Normal",
                Category = "General",
                CreatedDate = DateTime.Now
            };

            await _messageRepository.AddAsync(newMessage);
            return newMessage;
        }

        public async Task<List<AnalysisMessage>> GetConversationAsync(int fromUserId, int toUserId, int plantAnalysisId)
        {
            return await _messageRepository.GetConversationAsync(fromUserId, toUserId, plantAnalysisId);
        }

        public async Task<List<AnalysisMessage>> GetUnreadMessagesAsync(int userId)
        {
            return await _messageRepository.GetUnreadMessagesAsync(userId);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _messageRepository.GetUnreadCountAsync(userId);
        }

        public async Task MarkAsReadAsync(int messageId)
        {
            await _messageRepository.MarkAsReadAsync(messageId);
        }

        public async Task MarkConversationAsReadAsync(int userId, int plantAnalysisId)
        {
            await _messageRepository.MarkConversationAsReadAsync(userId, plantAnalysisId);
        }

        public async Task<bool> CanReplyToMessageAsync(int userId, int messageId)
        {
            var message = await _messageRepository.GetAsync(m => m.Id == messageId);
            if (message == null)
                return false;

            // Kullanıcı mesajın alıcısı mı?
            if (message.ToUserId != userId)
                return false;

            // Sponsor ise mesajlaşma yetkisi var mı?
            var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(userId);
            if (sponsorProfile != null)
                return await CanSendMessageAsync(userId);

            return true; // Farmers can always reply
        }

        public async Task<AnalysisMessage> ReplyToMessageAsync(int userId, int parentMessageId, string message)
        {
            var parentMessage = await _messageRepository.GetAsync(m => m.Id == parentMessageId);
            if (parentMessage == null || !await CanReplyToMessageAsync(userId, parentMessageId))
                return null;

            var replyMessage = new AnalysisMessage
            {
                PlantAnalysisId = parentMessage.PlantAnalysisId,
                FromUserId = userId,
                ToUserId = parentMessage.FromUserId, // Reply to the sender
                ParentMessageId = parentMessageId,
                Message = message,
                MessageType = "Answer",
                Subject = $"Re: {parentMessage.Subject}",
                SentDate = DateTime.Now,
                IsRead = false,
                IsApproved = true,
                Priority = "Normal",
                Category = parentMessage.Category,
                CreatedDate = DateTime.Now
            };

            var user = await _userRepository.GetAsync(u => u.UserId == userId);
            var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(userId);
            
            replyMessage.SenderRole = sponsorProfile != null ? "Sponsor" : "Farmer";
            replyMessage.SenderName = user?.FullName;
            replyMessage.SenderCompany = sponsorProfile?.CompanyName;

            await _messageRepository.AddAsync(replyMessage);
            return replyMessage;
        }

        public async Task<List<AnalysisMessage>> GetRecentMessagesAsync(int userId, int count = 10)
        {
            return await _messageRepository.GetRecentMessagesAsync(userId, count);
        }

        public async Task<bool> HasMessagingPermissionAsync(int sponsorId)
        {
            return await CanSendMessageAsync(sponsorId);
        }

        public async Task DeleteMessageAsync(int messageId, int userId)
        {
            var message = await _messageRepository.GetAsync(m => m.Id == messageId);
            if (message != null && (message.FromUserId == userId || message.ToUserId == userId))
            {
                message.IsDeleted = true;
                message.DeletedDate = DateTime.Now;
                message.UpdatedDate = DateTime.Now;
                
                await _messageRepository.UpdateAsync(message);
            }
        }

        public async Task FlagMessageAsync(int messageId, int flaggedByUserId, string reason)
        {
            var message = await _messageRepository.GetAsync(m => m.Id == messageId);
            if (message != null)
            {
                message.IsFlagged = true;
                message.FlagReason = reason;
                message.UpdatedDate = DateTime.Now;
                
                await _messageRepository.UpdateAsync(message);
            }
        }

        public async Task<List<AnalysisMessage>> GetMessagesByPriorityAsync(int userId, string priority)
        {
            return await _messageRepository.GetMessagesByPriorityAsync(userId, priority);
        }
    }
}