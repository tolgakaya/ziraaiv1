using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Services.Sponsorship
{
    public interface IAnalysisMessagingService
    {
        Task<bool> CanSendMessageAsync(int sponsorId);
        Task<(bool canSend, string errorMessage)> CanSendMessageForAnalysisAsync(int sponsorId, int farmerId, int plantAnalysisId);
        Task<AnalysisMessage> SendMessageAsync(int fromUserId, int toUserId, int plantAnalysisId, string message, string messageType = "Information");
        Task<List<AnalysisMessage>> GetConversationAsync(int fromUserId, int toUserId, int plantAnalysisId);
        Task<List<AnalysisMessage>> GetUnreadMessagesAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAsReadAsync(int messageId);
        Task MarkConversationAsReadAsync(int userId, int plantAnalysisId);
        Task<bool> CanReplyToMessageAsync(int userId, int messageId);
        Task<AnalysisMessage> ReplyToMessageAsync(int userId, int parentMessageId, string message);
        Task<List<AnalysisMessage>> GetRecentMessagesAsync(int userId, int count = 10);
        Task<bool> HasMessagingPermissionAsync(int sponsorId);
        Task DeleteMessageAsync(int messageId, int userId);
        Task FlagMessageAsync(int messageId, int flaggedByUserId, string reason);
        Task<List<AnalysisMessage>> GetMessagesByPriorityAsync(int userId, string priority);
    }
}