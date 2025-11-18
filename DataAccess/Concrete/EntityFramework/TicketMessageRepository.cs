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
    public class TicketMessageRepository : EfEntityRepositoryBase<TicketMessage, ProjectDbContext>, ITicketMessageRepository
    {
        public TicketMessageRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<List<TicketMessage>> GetTicketMessagesAsync(int ticketId, bool includeInternal = false)
        {
            var query = Context.TicketMessages
                .Include(m => m.FromUser)
                .Where(m => m.TicketId == ticketId);

            if (!includeInternal)
            {
                query = query.Where(m => !m.IsInternal);
            }

            return await query
                .OrderBy(m => m.CreatedDate)
                .ToListAsync();
        }

        public async Task MarkMessagesAsReadAsync(int ticketId, int userId)
        {
            var unreadMessages = await Context.TicketMessages
                .Where(m => m.TicketId == ticketId &&
                            m.FromUserId != userId &&
                            !m.IsRead &&
                            !m.IsInternal)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadDate = DateTime.Now;
            }

            if (unreadMessages.Any())
            {
                Context.TicketMessages.UpdateRange(unreadMessages);
                await Context.SaveChangesAsync();
            }
        }

        public async Task<int> GetUnreadCountForUserAsync(int ticketId, int userId)
        {
            return await Context.TicketMessages
                .CountAsync(m => m.TicketId == ticketId &&
                                 m.FromUserId != userId &&
                                 !m.IsRead &&
                                 !m.IsInternal);
        }
    }
}
