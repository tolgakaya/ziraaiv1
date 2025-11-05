using Core.DataAccess;
using Entities.Concrete;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    /// <summary>
    /// Repository interface for bulk code distribution job data access
    /// </summary>
    public interface IBulkCodeDistributionJobRepository : IEntityRepository<BulkCodeDistributionJob>
    {
        /// <summary>
        /// Atomically increments progress counters for a bulk job
        /// </summary>
        Task<BulkCodeDistributionJob> IncrementProgressAsync(
            int bulkJobId,
            bool success,
            int codesDistributed = 0,
            bool smsSent = false);

        /// <summary>
        /// Checks if all farmers have been processed and marks job as complete if so
        /// </summary>
        Task<bool> CheckAndMarkCompleteAsync(int bulkJobId);
    }
}
