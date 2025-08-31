using Core.DataAccess;
using Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface IAnalysisMessageRepository : IEntityRepository<AnalysisMessage>
    {
        Task<List<AnalysisMessage>> GetByAnalysisIdAsync(int plantAnalysisId);
        Task<List<AnalysisMessage>> GetConversationAsync(int fromUserId, int toUserId, int plantAnalysisId);
        Task<List<AnalysisMessage>> GetUnreadMessagesAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAsReadAsync(int messageId);
        Task MarkConversationAsReadAsync(int userId, int plantAnalysisId);
        Task<List<AnalysisMessage>> GetMessageThreadAsync(int parentMessageId);
        Task<List<AnalysisMessage>> GetRecentMessagesAsync(int userId, int count = 10);
        Task<List<AnalysisMessage>> GetMessagesByPriorityAsync(int userId, string priority);
        Task<List<AnalysisMessage>> GetPendingApprovalAsync();
        Task ApproveMessageAsync(int messageId, int approvedByUserId);
    }
}