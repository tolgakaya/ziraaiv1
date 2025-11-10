using Core.DataAccess;
using Entities.Concrete;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    /// <summary>
    /// Repository interface for bulk subscription assignment job data access
    /// </summary>
    public interface IBulkSubscriptionAssignmentJobRepository : IEntityRepository<BulkSubscriptionAssignmentJob>
    {
        /// <summary>
        /// Atomically increments progress counters for a bulk subscription assignment job
        /// </summary>
        Task<BulkSubscriptionAssignmentJob> IncrementProgressAsync(
            int bulkJobId,
            bool success,
            int subscriptionsCreated = 0,
            bool notificationSent = false);

        /// <summary>
        /// Checks if all farmers have been processed and marks job as complete if so
        /// </summary>
        Task<bool> CheckAndMarkCompleteAsync(int bulkJobId);
    }
}
