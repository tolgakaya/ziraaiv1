using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Entities.Dtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework
{
    public class AnalysisMessageRepository : EfEntityRepositoryBase<AnalysisMessage, ProjectDbContext>, IAnalysisMessageRepository
    {
        public AnalysisMessageRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<List<AnalysisMessage>> GetByAnalysisIdAsync(int plantAnalysisId)
        {
            return await Context.AnalysisMessages
                .Include(x => x.FromUser)
                .Include(x => x.ToUser)
                .Include(x => x.ParentMessage)
                .Where(x => x.PlantAnalysisId == plantAnalysisId && !x.IsDeleted)
                .OrderBy(x => x.SentDate)
                .ToListAsync();
        }

        public async Task<List<AnalysisMessage>> GetConversationAsync(int fromUserId, int toUserId, int plantAnalysisId)
        {
            // ✅ FIX: Get analysis to check if dealer exists (three-way conversation support)
            var analysis = await Context.PlantAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == plantAnalysisId);
            
            if (analysis == null)
                return new List<AnalysisMessage>();
            
            var query = Context.AnalysisMessages
                .Include(x => x.FromUser)
                .Include(x => x.ToUser)
                .Where(x => x.PlantAnalysisId == plantAnalysisId && !x.IsDeleted);
            
            // ✅ FIX: If dealer exists, show ALL messages for this analysis
            // This enables three-way conversation: farmer ↔ sponsor + farmer ↔ dealer
            // All parties (farmer, sponsor, dealer) can see all messages for transparency
            if (analysis.DealerId.HasValue)
            {
                // Show all messages for this analysis
                // Authorization already checked in controller (farmer, sponsor, or dealer)
                // ⚠️ REVERSE ORDER: Latest messages first (DESC) for mobile chat UI
                return await query.OrderByDescending(x => x.SentDate).ToListAsync();
            }

            // Keep existing 1-to-1 behavior when no dealer (backward compatibility)
            // ⚠️ REVERSE ORDER: Latest messages first (DESC) for mobile chat UI
            return await query
                .Where(x => (x.FromUserId == fromUserId && x.ToUserId == toUserId) ||
                            (x.FromUserId == toUserId && x.ToUserId == fromUserId))
                .OrderByDescending(x => x.SentDate)
                .ToListAsync();
        }

        public async Task<List<AnalysisMessage>> GetUnreadMessagesAsync(int userId)
        {
            return await Context.AnalysisMessages
                .Include(x => x.FromUser)
                .Include(x => x.PlantAnalysis)
                .Where(x => x.ToUserId == userId && !x.IsRead && !x.IsDeleted)
                .OrderByDescending(x => x.SentDate)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await Context.AnalysisMessages
                .CountAsync(x => x.ToUserId == userId && !x.IsRead && !x.IsDeleted);
        }

        public async Task MarkAsReadAsync(int messageId)
        {
            var message = await GetAsync(m => m.Id == messageId);
            if (message != null)
            {
                message.IsRead = true;
                message.ReadDate = DateTime.Now;
                message.UpdatedDate = DateTime.Now;
                
                Context.AnalysisMessages.Update(message);
                await Context.SaveChangesAsync();
            }
        }

        public async Task MarkConversationAsReadAsync(int userId, int plantAnalysisId)
        {
            var unreadMessages = await Context.AnalysisMessages
                .Where(x => x.ToUserId == userId && 
                           x.PlantAnalysisId == plantAnalysisId && 
                           !x.IsRead && 
                           !x.IsDeleted)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadDate = DateTime.Now;
                message.UpdatedDate = DateTime.Now;
            }

            if (unreadMessages.Any())
            {
                Context.AnalysisMessages.UpdateRange(unreadMessages);
                await Context.SaveChangesAsync();
            }
        }

        public async Task<List<AnalysisMessage>> GetMessageThreadAsync(int parentMessageId)
        {
            return await Context.AnalysisMessages
                .Include(x => x.FromUser)
                .Include(x => x.ToUser)
                .Where(x => x.ParentMessageId == parentMessageId && !x.IsDeleted)
                .OrderBy(x => x.SentDate)
                .ToListAsync();
        }

        public async Task<List<AnalysisMessage>> GetRecentMessagesAsync(int userId, int count = 10)
        {
            return await Context.AnalysisMessages
                .Include(x => x.FromUser)
                .Include(x => x.ToUser)
                .Include(x => x.PlantAnalysis)
                .Where(x => (x.FromUserId == userId || x.ToUserId == userId) && !x.IsDeleted)
                .OrderByDescending(x => x.SentDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<AnalysisMessage>> GetMessagesByPriorityAsync(int userId, string priority)
        {
            return await Context.AnalysisMessages
                .Include(x => x.FromUser)
                .Include(x => x.ToUser)
                .Include(x => x.PlantAnalysis)
                .Where(x => x.ToUserId == userId && 
                           x.Priority == priority && 
                           !x.IsDeleted &&
                           !x.IsRead)
                .OrderByDescending(x => x.SentDate)
                .ToListAsync();
        }

        public async Task<List<AnalysisMessage>> GetPendingApprovalAsync()
        {
            return await Context.AnalysisMessages
                .Include(x => x.FromUser)
                .Include(x => x.ToUser)
                .Include(x => x.PlantAnalysis)
                .Where(x => !x.IsApproved && !x.IsDeleted)
                .OrderBy(x => x.SentDate)
                .ToListAsync();
        }

        public async Task ApproveMessageAsync(int messageId, int approvedByUserId)
        {
            var message = await GetAsync(m => m.Id == messageId);
            if (message != null)
            {
                message.IsApproved = true;
                message.ApprovedDate = DateTime.Now;
                message.ApprovedByUserId = approvedByUserId;
                message.UpdatedDate = DateTime.Now;
                
                Context.AnalysisMessages.Update(message);
                await Context.SaveChangesAsync();
            }
        }




        public async Task<bool> HasSponsorMessagedAnalysisAsync(int plantAnalysisId, int sponsorUserId)
        {
            return await Context.AnalysisMessages
                .AnyAsync(m =>
                    m.PlantAnalysisId == plantAnalysisId &&
                    m.FromUserId == sponsorUserId &&
                    !m.IsDeleted);
        }

        public async Task<Dictionary<int, MessagingStatusDto>> GetMessagingStatusForAnalysesAsync(
            int sponsorId,
            int[] analysisIds)
        {
            // Single efficient query with grouping
            var result = await Context.AnalysisMessages
                .Where(m => analysisIds.Contains(m.PlantAnalysisId) && !m.IsDeleted)
                .GroupBy(m => m.PlantAnalysisId)
                .Select(g => new
                {
                    AnalysisId = g.Key,
                    TotalMessageCount = g.Count(),
                    UnreadCount = g.Count(m => !m.IsRead && m.ToUserId == sponsorId),
                    LastMessageDate = g.Max(m => m.SentDate),
                    LastMessage = g.OrderByDescending(m => m.SentDate).FirstOrDefault(),
                    HasFarmerResponse = g.Any(m => m.ToUserId == sponsorId),
                    LastFarmerResponseDate = g.Where(m => m.ToUserId == sponsorId)
                        .Max(m => (DateTime?)m.SentDate)
                })
                .ToDictionaryAsync(
                    x => x.AnalysisId,
                    x => new MessagingStatusDto
                    {
                        HasMessages = true,
                        TotalMessageCount = x.TotalMessageCount,
                        UnreadCount = x.UnreadCount,
                        LastMessageDate = x.LastMessageDate,
                        LastMessagePreview = x.LastMessage != null && !string.IsNullOrEmpty(x.LastMessage.Message)
                            ? (x.LastMessage.Message.Length > 50
                                ? x.LastMessage.Message.Substring(0, 50) + "..."
                                : x.LastMessage.Message)
                            : null,
                        LastMessageBy = x.LastMessage != null
                            ? (x.LastMessage.FromUserId == sponsorId ? "sponsor" : "farmer")
                            : null,
                        HasFarmerResponse = x.HasFarmerResponse,
                        LastFarmerResponseDate = x.LastFarmerResponseDate,
                        ConversationStatus = CalculateConversationStatus(
                            x.TotalMessageCount,
                            x.HasFarmerResponse,
                            x.LastMessageDate)
                    });

            // Add default status for analyses with no messages
            foreach (var analysisId in analysisIds.Where(id => !result.ContainsKey(id)))
            {
                result[analysisId] = new MessagingStatusDto
                {
                    HasMessages = false,
                    TotalMessageCount = 0,
                    UnreadCount = 0,
                    LastMessageDate = null,
                    LastMessagePreview = null,
                    LastMessageBy = null,
                    HasFarmerResponse = false,
                    LastFarmerResponseDate = null,
                    ConversationStatus = ConversationStatus.NoContact
                };
            }

            return result;
        }


        public async Task<Dictionary<int, MessagingStatusDto>> GetMessagingStatusForFarmerAsync(
            int farmerUserId,
            List<Entities.Concrete.PlantAnalysis> analyses)
        {
            var analysisIds = analyses.Select(a => a.Id).ToArray();
            
            // Build sponsor ID map: analysisId -> sponsorUserId
            var sponsorMap = analyses
                .Where(a => a.SponsorUserId.HasValue)
                .ToDictionary(a => a.Id, a => a.SponsorUserId.Value);

            // Single efficient query with grouping
            var result = await Context.AnalysisMessages
                .Where(m => analysisIds.Contains(m.PlantAnalysisId) && !m.IsDeleted)
                .GroupBy(m => m.PlantAnalysisId)
                .Select(g => new
                {
                    AnalysisId = g.Key,
                    TotalMessageCount = g.Count(),
                    UnreadCount = g.Count(m => !m.IsRead && m.ToUserId == farmerUserId),
                    LastMessageDate = g.Max(m => m.SentDate),
                    LastMessage = g.OrderByDescending(m => m.SentDate).FirstOrDefault(),
                    HasFarmerResponse = g.Any(m => m.FromUserId == farmerUserId),
                    LastFarmerResponseDate = g.Where(m => m.FromUserId == farmerUserId)
                        .Max(m => (DateTime?)m.SentDate)
                })
                .ToDictionaryAsync(
                    x => x.AnalysisId,
                    x => new MessagingStatusDto
                    {
                        HasMessages = true,
                        TotalMessageCount = x.TotalMessageCount,
                        UnreadCount = x.UnreadCount,
                        LastMessageDate = x.LastMessageDate,
                        LastMessagePreview = x.LastMessage != null && !string.IsNullOrEmpty(x.LastMessage.Message)
                            ? (x.LastMessage.Message.Length > 50
                                ? x.LastMessage.Message.Substring(0, 50) + "..."
                                : x.LastMessage.Message)
                            : null,
                        // ✅ CORRECT: Compare against actual sponsor ID for this analysis
                        LastMessageBy = x.LastMessage != null && sponsorMap.ContainsKey(x.AnalysisId)
                            ? (x.LastMessage.FromUserId == sponsorMap[x.AnalysisId] ? "sponsor" : "farmer")
                            : null,
                        HasFarmerResponse = x.HasFarmerResponse,
                        LastFarmerResponseDate = x.LastFarmerResponseDate,
                        ConversationStatus = CalculateConversationStatus(
                            x.TotalMessageCount,
                            x.HasFarmerResponse,
                            x.LastMessageDate)
                    });

            // Add default status for analyses with no messages
            foreach (var analysisId in analysisIds.Where(id => !result.ContainsKey(id)))
            {
                result[analysisId] = new MessagingStatusDto
                {
                    HasMessages = false,
                    TotalMessageCount = 0,
                    UnreadCount = 0,
                    LastMessageDate = null,
                    LastMessagePreview = null,
                    LastMessageBy = null,
                    HasFarmerResponse = false,
                    LastFarmerResponseDate = null,
                    ConversationStatus = ConversationStatus.NoContact
                };
            }

            return result;
        }

        /// <summary>
        /// Calculate conversation status based on message counts and dates
        /// </summary>
        private static ConversationStatus CalculateConversationStatus(
            int totalCount,
            bool hasResponse,
            DateTime lastMessageDate)
        {
            if (totalCount == 0)
                return ConversationStatus.NoContact;

            if (!hasResponse)
                return ConversationStatus.Pending;

            var daysSinceLastMessage = (DateTime.Now - lastMessageDate).Days;
            return daysSinceLastMessage < 7
                ? ConversationStatus.Active
                : ConversationStatus.Idle;
        }
    }
}