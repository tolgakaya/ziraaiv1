using Business.Services.Sponsorship;
using Business.Services.Subscription;
using Business.Hubs;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IHubContext<PlantAnalysisHub> _hubContext;
        private readonly ITierFeatureService _tierFeatureService;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;

        public AnalysisMessagingService(
            IAnalysisMessageRepository messageRepository,
            ISponsorProfileRepository sponsorProfileRepository,
            IUserRepository userRepository,
            ISponsorAnalysisAccessRepository analysisAccessRepository,
            IPlantAnalysisRepository plantAnalysisRepository,
            IFarmerSponsorBlockRepository blockRepository,
            IMessageRateLimitService rateLimitService,
            IHubContext<PlantAnalysisHub> hubContext,
            ITierFeatureService tierFeatureService,
            IUserSubscriptionRepository userSubscriptionRepository)
        {
            _messageRepository = messageRepository;
            _sponsorProfileRepository = sponsorProfileRepository;
            _hubContext = hubContext;
            _userRepository = userRepository;
            _analysisAccessRepository = analysisAccessRepository;
            _plantAnalysisRepository = plantAnalysisRepository;
            _blockRepository = blockRepository;
            _rateLimitService = rateLimitService;
            _tierFeatureService = tierFeatureService;
            _userSubscriptionRepository = userSubscriptionRepository;
        }

        /// <summary>
        /// Checks if user can send messages for a specific analysis (tier-based permission)
        /// CRITICAL: Tier comes from ANALYSIS, not from user's purchases
        /// </summary>
        public async Task<bool> CanSendMessageAsync(int userId, int plantAnalysisId)
        {
            // Get the analysis to determine its tier
            var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == plantAnalysisId);
            if (analysis == null)
            {
                return false;
            }

            // Check if analysis has sponsorship (ActiveSponsorshipId)
            if (!analysis.ActiveSponsorshipId.HasValue || analysis.ActiveSponsorshipId.Value == 0)
            {
                // Analysis not sponsored - no tier, no messaging
                return false;
            }

            // Get the UserSubscription (sponsorship package) that this analysis uses
            var userSubscription = await _userSubscriptionRepository.GetAsync(us => us.Id == analysis.ActiveSponsorshipId.Value);
            if (userSubscription == null)
            {
                return false;
            }

            // Check if ANALYSIS tier (from UserSubscription, not user tier) has messaging feature
            // This is the correct approach: analysis.ActiveSponsorshipId -> UserSubscription -> SubscriptionTierId -> TierFeature
            var hasMessaging = await _tierFeatureService.HasFeatureAccessAsync(userSubscription.SubscriptionTierId, "messaging");
            return hasMessaging;
        }

        /// <summary>
        /// Checks if sponsor has access to message farmer for a specific analysis
        /// Validates: 1) Tier permission, 2) Analysis ownership, 3) Rate limit, 4) Not blocked
        /// </summary>
        public async Task<(bool canSend, string errorMessage)> CanSendMessageForAnalysisAsync(int sponsorId, int farmerId, int plantAnalysisId)
        {
            // 1. Check tier permission based on ANALYSIS tier (not user tier)
            if (!await CanSendMessageAsync(sponsorId, plantAnalysisId))
            {
                return (false, "Messaging is not available for this analysis tier. Upgrade to M tier or higher to enable messaging");
            }

            // 2. Check analysis ownership - sponsor must own this analysis OR be the dealer who distributed the code
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

            // Verify analysis attribution - user must be involved as sponsor OR dealer
            // - As Sponsor: SponsorUserId = sponsorId (codes purchased by sponsor)
            // - As Dealer: DealerId = sponsorId (codes distributed by dealer on behalf of sponsor)
            // - Hybrid: Users who are both sponsor and dealer can access analyses from both roles
            bool hasAttribution = (analysis.SponsorUserId == sponsorId) || 
                                  (analysis.DealerId == sponsorId);

            if (!hasAttribution)
            {
                return (false, "You can only message farmers for analyses done using sponsorship codes you purchased or distributed");
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
        /// Validates if farmer can reply to sponsor's message
        /// Farmers can only reply if sponsor has sent them a message first
        /// </summary>
        public async Task<(bool canReply, string errorMessage)> CanFarmerReplyAsync(int farmerId, int sponsorId, int plantAnalysisId)
        {
            // 1. Check if analysis exists
            var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == plantAnalysisId);
            if (analysis == null)
            {
                return (false, "Analysis not found");
            }

            // 2. Verify farmer owns this analysis
            var analysisFarmerId = analysis.UserId ?? 0;
            if (analysisFarmerId != farmerId)
            {
                return (false, "You can only reply to messages for your own analyses");
            }

            // 3. Verify analysis was sponsored
            if (!analysis.SponsorUserId.HasValue && !analysis.DealerId.HasValue)
            {
                return (false, "This analysis was not sponsored");
            }

            // 4. Verify sponsor/dealer matches - accept messages from either sponsor OR dealer
            // sponsorId parameter actually represents whoever sent the message (could be sponsor or dealer)
            bool isValidMessageSender = (analysis.SponsorUserId.HasValue && analysis.SponsorUserId.Value == sponsorId) ||
                                        (analysis.DealerId.HasValue && analysis.DealerId.Value == sponsorId);

            if (!isValidMessageSender)
            {
                return (false, "Invalid sponsor/dealer for this analysis");
            }

            // 5. Check if sponsor/dealer has sent at least one message to farmer
            var sponsorMessage = await _messageRepository.GetAsync(
                m => m.PlantAnalysisId == plantAnalysisId &&
                     m.FromUserId == sponsorId &&
                     m.ToUserId == farmerId);

            if (sponsorMessage == null)
            {
                return (false, "You can only reply after the sponsor/dealer sends you a message first");
            }

            // 6. Check if farmer has blocked this sponsor
            var isBlocked = await _blockRepository.IsBlockedAsync(farmerId, sponsorId);
            if (isBlocked)
            {
                return (false, "You have blocked this sponsor");
            }

            return (true, string.Empty);
        }

        public async Task<Entities.Concrete.PlantAnalysis> GetPlantAnalysisAsync(int plantAnalysisId)
        {
            return await _plantAnalysisRepository.GetAsync(a => a.Id == plantAnalysisId);
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

                // 🔔 Send real-time SignalR notification to recipient
                try
                {
                    await _hubContext.Clients.User(toUserId.ToString()).SendAsync("NewMessage", new
                    {
                        messageId = newMessage.Id,
                        plantAnalysisId = newMessage.PlantAnalysisId,
                        fromUserId = newMessage.FromUserId,
                        fromUserName = newMessage.SenderName,
                        fromUserCompany = newMessage.SenderCompany,
                        senderRole = newMessage.SenderRole,
                        senderAvatarUrl = fromUser.AvatarUrl,
                        senderAvatarThumbnailUrl = fromUser.AvatarThumbnailUrl,
                        message = newMessage.Message,
                        messageType = newMessage.MessageType,
                        sentDate = newMessage.SentDate,
                        isApproved = newMessage.IsApproved,
                        requiresApproval = isFirstMessage,
                        hasAttachments = false,
                        attachmentCount = 0,
                        attachmentUrls = (string[])null,
                        attachmentThumbnails = (string[])null,
                        isVoiceMessage = false,
                        voiceMessageUrl = (string)null,
                        voiceMessageDuration = (int?)null,
                        voiceMessageWaveform = (string)null
                    });
                }
                catch (Exception ex)
                {
                    // Log but don't fail if SignalR notification fails
                    // Message is still saved, notification is best-effort
                    Console.WriteLine($"[AnalysisMessagingService] Warning: Failed to send SignalR notification: {ex.Message}");
                }

                return newMessage;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SendMessageNotificationAsync(
            AnalysisMessage message, 
            string senderRole,
            string[] attachmentUrls = null,
            string[] attachmentThumbnails = null,
            string voiceMessageUrl = null,
            string voiceMessageWaveform = null)
        {
            try
            {
                var sender = await _userRepository.GetAsync(u => u.UserId == message.FromUserId);
                var isFirstMessage = !message.IsApproved;

                await _hubContext.Clients.User(message.ToUserId.ToString()).SendAsync("NewMessage", new
                {
                    messageId = message.Id,
                    plantAnalysisId = message.PlantAnalysisId,
                    fromUserId = message.FromUserId,
                    fromUserName = sender?.FullName ?? string.Empty,
                    fromUserCompany = string.Empty,
                    senderRole = senderRole,
                    senderAvatarUrl = sender?.AvatarUrl,
                    senderAvatarThumbnailUrl = sender?.AvatarThumbnailUrl,
                    message = message.Message,
                    messageType = message.MessageType,
                    sentDate = message.SentDate,
                    isApproved = message.IsApproved,
                    requiresApproval = isFirstMessage,
                    hasAttachments = message.HasAttachments,
                    attachmentCount = message.AttachmentCount,
                    attachmentUrls = attachmentUrls,
                    attachmentThumbnails = attachmentThumbnails,
                    isVoiceMessage = !string.IsNullOrEmpty(message.VoiceMessageUrl),
                    voiceMessageUrl = voiceMessageUrl,
                    voiceMessageDuration = message.VoiceMessageDuration,
                    voiceMessageWaveform = voiceMessageWaveform
                });
            }
            catch (Exception ex)
            {
                // Log but don't fail if SignalR notification fails
                Console.WriteLine($"[AnalysisMessagingService] Warning: Failed to send SignalR notification: {ex.Message}");
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

            // Get plantAnalysisId from message
            var plantAnalysisId = message.PlantAnalysisId;

            // Sponsor ise mesajlaşma yetkisi var mı? (Check ANALYSIS tier, not user tier)
            var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(userId);
            if (sponsorProfile != null)
                return await CanSendMessageAsync(userId, plantAnalysisId);

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

        public async Task<bool> HasMessagingPermissionAsync(int sponsorId, int plantAnalysisId)
        {
            return await CanSendMessageAsync(sponsorId, plantAnalysisId);
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