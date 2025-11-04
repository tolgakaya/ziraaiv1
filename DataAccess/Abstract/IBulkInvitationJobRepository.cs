using Core.DataAccess;
using Entities.Concrete;
using System.Threading.Tasks;

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

        /// <summary>
        /// Atomically increments progress counters for a bulk invitation job
        /// Thread-safe operation that prevents race conditions in concurrent processing
        /// </summary>
        /// <param name="bulkJobId">ID of the bulk invitation job</param>
        /// <param name="success">Whether the invitation was successful</param>
        /// <returns>Updated bulk job with latest counters, or null if not found</returns>
        Task<BulkInvitationJob> IncrementProgressAsync(int bulkJobId, bool success);

        /// <summary>
        /// Checks if all invitations have been processed and updates status accordingly
        /// Must be called after IncrementProgressAsync to ensure completion detection
        /// </summary>
        /// <param name="bulkJobId">ID of the bulk invitation job</param>
        /// <returns>True if job is complete, false otherwise</returns>
        Task<bool> CheckAndMarkCompleteAsync(int bulkJobId);
    }
}
