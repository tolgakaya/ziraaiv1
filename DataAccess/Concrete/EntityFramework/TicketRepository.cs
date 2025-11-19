using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework
{
    public class TicketRepository : EfEntityRepositoryBase<Ticket, ProjectDbContext>, ITicketRepository
    {
        public TicketRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<List<Ticket>> GetUserTicketsAsync(int userId, string status = null, string category = null)
        {
            var query = Context.Tickets
                .Include(t => t.Messages)
                .Include(t => t.AssignedToUser)
                .Where(t => t.UserId == userId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(t => t.Category == category);
            }

            return await query
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }

        public async Task<Ticket> GetTicketWithMessagesAsync(int ticketId)
        {
            return await Context.Tickets
                .Include(t => t.User)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Messages)
                    .ThenInclude(m => m.FromUser)
                .FirstOrDefaultAsync(t => t.Id == ticketId);
        }

        public async Task<List<Ticket>> GetAllTicketsForAdminAsync(string status = null, string category = null, string priority = null)
        {
            var query = Context.Tickets
                .Include(t => t.User)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Messages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(t => t.Category == category);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(t => t.Priority == priority);
            }

            return await query
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }

        public async Task<int> GetTicketCountByStatusAsync(string status)
        {
            return await Context.Tickets
                .CountAsync(t => t.Status == status);
        }
    }
}
