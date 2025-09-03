using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
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
            return await Context.AnalysisMessages
                .Include(x => x.FromUser)
                .Include(x => x.ToUser)
                .Where(x => x.PlantAnalysisId == plantAnalysisId && 
                           !x.IsDeleted &&
                           ((x.FromUserId == fromUserId && x.ToUserId == toUserId) ||
                            (x.FromUserId == toUserId && x.ToUserId == fromUserId)))
                .OrderBy(x => x.SentDate)
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
    }
}