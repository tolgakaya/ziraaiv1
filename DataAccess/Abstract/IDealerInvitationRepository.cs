using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract
{
    /// <summary>
    /// Repository interface for DealerInvitation entity
    /// Provides data access operations for dealer invitation management
    /// </summary>
    public interface IDealerInvitationRepository : IEntityRepository<DealerInvitation>
    {
        // IRepository<DealerInvitation> already provides:
        // - Get(Expression<Func<DealerInvitation, bool>> filter)
        // - GetList(Expression<Func<DealerInvitation, bool>> filter = null)
        // - Add(DealerInvitation entity)
        // - Update(DealerInvitation entity)
        // - Delete(DealerInvitation entity)
        // - GetAsync(Expression<Func<DealerInvitation, bool>> filter)
        // - GetListAsync(Expression<Func<DealerInvitation, bool>> filter = null)
        // - AddAsync(DealerInvitation entity)
        // - UpdateAsync(DealerInvitation entity)
        // - DeleteAsync(DealerInvitation entity)
    }
}
