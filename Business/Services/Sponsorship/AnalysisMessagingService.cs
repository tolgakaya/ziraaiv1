using Business.Services.Sponsorship;
using DataAccess.Abstract;
using Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public class AnalysisMessagingService : IAnalysisMessagingService
    {
        private readonly IAnalysisMessageRepository _messageRepository;
        private readonly ISponsorProfileRepository _sponsorProfileRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISponsorAnalysisAccessRepository _analysisAccessRepository;
        private readonly IPlantAnalysisRepository _plantAnalysisRepository;
        private readonly IFarmerSponsorBlockRepository _blockRepository;
        private readonly IMessageRateLimitService _rateLimitService;

        public AnalysisMessagingService(
            IAnalysisMessageRepository messageRepository,
            ISponsorProfileRepository sponsorProfileRepository,
            IUserRepository userRepository,
            ISponsorAnalysisAccessRepository analysisAccessRepository,
            IPlantAnalysisRepository plantAnalysisRepository,
            IFarmerSponsorBlockRepository blockRepository,
            IMessageRateLimitService rateLimitService)
        {
            _messageRepository = messageRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
            _userRepository = userRepository;
            _analysisAccessRepository = analysisAccessRepository;
            _plantAnalysisRepository = plantAnalysisRepository;
            _blockRepository = blockRepository;
            _rateLimitService = rateLimitService;
        }

        /// <summary>
        /// Checks if sponsor can send messages (tier-based permission)
        /// </summary>
        public async Task<bool> CanSendMessageAsync(int sponsorId)
        {
            var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);

            // If not a sponsor, allow messaging (farmers can always send messages)
            if (profile == null)
            {
                return true;
            }

            // For sponsors, check if they have active profile (verification not required for messaging)
            if (!profile.IsActive)
            {
                return false;
            }

            // Sponsor'un M, L veya XL paketi satın almış olması gerekiyor (mesajlaşma için)
            if (profile.SponsorshipPurchases != null && profile.SponsorshipPurchases.Any())
            {
                foreach (var purchase in profile.SponsorshipPurchases)
                {
                    // Sadece L, XL tier'larında mesajlaşma var (L=3, XL=4)
                    // M tier'da mesajlaşma yok çünkü çiftçi profili anonim
                    if (purchase.SubscriptionTierId >= 3) // L=3, XL=4
                    {
                        return true;
                    }
                }
                return false;
            }

            return false;
        }

        /// <summary>
        /// Checks if sponsor has access to message farmer for a specific analysis
        /// Validates: 1) Tier permission, 2) Analysis ownership, 3) Rate limit, 4) Not blocked
        /// </summary>
        public async Task<(bool canSend, string errorMessage)> CanSendMessageForAnalysisAsync(int sponsorId, int farmerId, int plantAnalysisId)
        {
            // 1. Check tier permission (L/XL only)
            if (!await CanSendMessageAsync(sponsorId))
            {
                return (false, "Messaging is only available for L and XL tier sponsors");
            }

            // 2. Check analysis ownership - sponsor must own this analysis
            var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == plantAnalysisId);
            if (analysis == null)
            {
                return (false, "Analysis not found");
            }

            // Get sponsor profile
            var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
            if (sponsorProfile == null)
            {
                return (false, "Sponsor profile not found");
            }

            // Verify analysis was sponsored by this sponsor (check SponsorUserId match)
            if (analysis.SponsorUserId != sponsorId)
            {
                return (false, "You can only message farmers for analyses done using your sponsorship codes");
            }

            // Verify sponsor has access record for this analysis
            var hasAccess = await _analysisAccessRepository.GetAsync(
                a => a.SponsorId == sponsorId &&
                     a.PlantAnalysisId == plantAnalysisId);

            if (hasAccess == null)
            {
                return (false, "No access record found for this analysis");
            }

            // 3. Check if farmer has blocked this sponsor
            var isBlocked = await _blockRepository.IsBlockedAsync(farmerId, sponsorId);
            if (isBlocked)
            {
                return (false, "This farmer has blocked messages from you");
            }

            // 4. Check rate limit (10 messages per day per farmer)
            var canSendByRate = await _rateLimitService.CanSendMessageToFarmerAsync(sponsorId, farmerId);
            if (!canSendByRate)
            {
                var remaining = await _rateLimitService.GetRemainingMessagesAsync(sponsorId, farmerId);
                return (false, $"Daily message limit reached (10 messages per day per farmer). Remaining: {remaining}");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Checks if this is the first message between sponsor and farmer
        /// First messages require admin approval
        /// </summary>
        private async Task<bool> IsFirstMessageAsync(int sponsorId, int farmerId, int plantAnalysisId)
        {
            var existingMessages = await _messageRepository.GetListAsync(m =>
                ((m.FromUserId == sponsorId && m.ToUserId == farmerId) ||
                 (m.FromUserId == farmerId && m.ToUserId == sponsorId)) &&
                m.PlantAnalysisId == plantAnalysisId);

            return existingMessages == null || !existingMessages.Any();
        }

        public async Task<AnalysisMessage> SendMessageAsync(int fromUserId, int toUserId, int plantAnalysisId, string message, string messageType = "Information")
        {
            try
            {
                // Gönderen kullanıcının mesajlaşma yetkisi var mı kontrol et
                var fromUser = await _userRepository.GetAsync(u => u.UserId == fromUserId);
                var toUser = await _userRepository.GetAsync(u => u.UserId == toUserId);

                if (fromUser == null || toUser == null)
                {
                    return null;
                }

                // Sponsor ise mesajlaşma yetkisi kontrolü (comprehensive validation)
                var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(fromUserId);

                if (sponsorProfile != null)
                {
                    // Use comprehensive validation for sponsors
                    var (canSend, errorMessage) = await CanSendMessageForAnalysisAsync(fromUserId, toUserId, plantAnalysisId);
                    if (!canSend)
                    {
                        return null; // Validation failed
                    }
                }

                // Check if this is the first message (requires admin approval)
                var isFirstMessage = await IsFirstMessageAsync(fromUserId, toUserId, plantAnalysisId);

                var newMessage = new AnalysisMessage
                {
                    PlantAnalysisId = plantAnalysisId,
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    Message = message ?? string.Empty,
                    MessageType = messageType ?? "Information",
                    SentDate = DateTime.Now,
                    IsRead = false,
                    IsApproved = !isFirstMessage, // First messages require approval, others auto-approved
                    ApprovedDate = !isFirstMessage ? DateTime.Now : (DateTime?)null,
                    SenderRole = sponsorProfile != null ? "Sponsor" : "Farmer",
                    SenderName = fromUser.FullName ?? string.Empty,
                    SenderCompany = sponsorProfile?.CompanyName ?? string.Empty,
                    Priority = "Normal",
                    Category = "General",
                    CreatedDate = DateTime.Now
                };

                _messageRepository.Add(newMessage);
                await _messageRepository.SaveChangesAsync();

                return newMessage;
            }
            catch (Exception)
            {
                throw;
            }
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

            _messageRepository.Add(replyMessage);
            await _messageRepository.SaveChangesAsync();
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
                
                _messageRepository.Update(message);
                await _messageRepository.SaveChangesAsync();
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
                
                _messageRepository.Update(message);
                await _messageRepository.SaveChangesAsync();
            }
        }

        public async Task<List<AnalysisMessage>> GetMessagesByPriorityAsync(int userId, string priority)
        {
            return await _messageRepository.GetMessagesByPriorityAsync(userId, priority);
        }
    }
}