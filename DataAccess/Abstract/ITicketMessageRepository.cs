using Core.DataAccess;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface ITicketMessageRepository : IEntityRepository<TicketMessage>
    {
        Task<List<TicketMessage>> GetTicketMessagesAsync(int ticketId, bool includeInternal = false);
        Task MarkMessagesAsReadAsync(int ticketId, int userId);
        Task<int> GetUnreadCountForUserAsync(int ticketId, int userId);
    }
}
