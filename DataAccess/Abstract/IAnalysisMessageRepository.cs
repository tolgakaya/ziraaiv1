using Core.DataAccess;
using Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
using Entities.Dtos;
using System.Collections.Generic;

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

        /// <summary>
        /// Get messaging status summary for multiple analyses efficiently
        /// Uses a single query with grouping for optimal performance
        /// </summary>
        /// <param name="sponsorId">ID of the sponsor</param>
        /// <param name="analysisIds">Array of analysis IDs to get status for</param>
        /// <returns>Dictionary mapping analysis ID to messaging status</returns>

        /// <summary>
        /// Check if a sponsor has sent any message to a specific analysis
        /// Used to determine if farmer can reply (sponsor must initiate conversation)
        /// </summary>
        /// <param name="plantAnalysisId">Analysis ID to check</param>
        /// <param name="sponsorUserId">Sponsor's user ID</param>
        /// <returns>True if sponsor has sent at least one non-deleted message</returns>
        Task<bool> HasSponsorMessagedAnalysisAsync(int plantAnalysisId, int sponsorUserId);

        Task<Dictionary<int, MessagingStatusDto>> GetMessagingStatusForAnalysesAsync(
            int sponsorId,
            int[] analysisIds);
    }
}