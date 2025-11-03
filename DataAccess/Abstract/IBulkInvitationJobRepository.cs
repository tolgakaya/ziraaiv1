using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract
{
    /// <summary>
    /// Repository interface for BulkInvitationJob entity
    /// Provides data access operations for bulk dealer invitation job tracking
    /// </summary>
    public interface IBulkInvitationJobRepository : IEntityRepository<BulkInvitationJob>
    {
        // IEntityRepository<BulkInvitationJob> already provides:
        // - Get(Expression<Func<BulkInvitationJob, bool>> filter)
        // - GetList(Expression<Func<BulkInvitationJob, bool>> filter = null)
        // - Add(BulkInvitationJob entity)
        // - Update(BulkInvitationJob entity)
        // - Delete(BulkInvitationJob entity)
        // - GetAsync(Expression<Func<BulkInvitationJob, bool>> filter)
        // - GetListAsync(Expression<Func<BulkInvitationJob, bool>> filter = null)
        // - AddAsync(BulkInvitationJob entity)
        // - UpdateAsync(BulkInvitationJob entity)
        // - DeleteAsync(BulkInvitationJob entity)
        // - SaveChangesAsync()
    }
}
