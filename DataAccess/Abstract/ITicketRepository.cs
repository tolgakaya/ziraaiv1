using Core.DataAccess;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface ITicketRepository : IEntityRepository<Ticket>
    {
        Task<List<Ticket>> GetUserTicketsAsync(int userId, string status = null, string category = null);
        Task<Ticket> GetTicketWithMessagesAsync(int ticketId);
        Task<List<Ticket>> GetAllTicketsForAdminAsync(string status = null, string category = null, string priority = null);
        Task<int> GetTicketCountByStatusAsync(string status);
    }
}
