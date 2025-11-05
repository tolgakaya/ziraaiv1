using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework
{
    /// <summary>
    /// Entity Framework implementation of IBulkCodeDistributionJobRepository
    /// Provides data access for bulk code distribution jobs using PostgreSQL database
    /// </summary>
    public class BulkCodeDistributionJobRepository : EfEntityRepositoryBase<BulkCodeDistributionJob, ProjectDbContext>, IBulkCodeDistributionJobRepository
    {
        public BulkCodeDistributionJobRepository(ProjectDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Atomically increments progress counters for a bulk job
        /// Uses raw SQL to prevent race conditions from concurrent worker processing
        /// </summary>
        /// <param name="bulkJobId">The bulk job ID</param>
        /// <param name="success">True for successful distribution, false for failed</param>
        /// <param name="codesDistributed">Number of codes distributed in this operation</param>
        /// <param name="smsSent">Whether SMS was sent (for SMS count increment)</param>
        /// <returns>Updated job entity</returns>
        public async Task<BulkCodeDistributionJob> IncrementProgressAsync(
            int bulkJobId,
            bool success,
            int codesDistributed = 0,
            bool smsSent = false)
        {
            // Use raw SQL for atomic increment to prevent race conditions
            // This ensures concurrent Hangfire jobs don't overwrite each other's updates
            var successField = success ? "\"SuccessfulDistributions\"" : "\"FailedDistributions\"";
            var smsIncrement = smsSent ? ", \"TotalSmsSent\" = \"TotalSmsSent\" + 1" : "";

            var sql = $@"
                UPDATE ""BulkCodeDistributionJobs""
                SET
                    ""ProcessedFarmers"" = ""ProcessedFarmers"" + 1,
                    {successField} = {successField} + 1,
                    ""TotalCodesDistributed"" = ""TotalCodesDistributed"" + {{0}}
                    {smsIncrement}
                WHERE ""Id"" = {{1}}";

            // Execute UPDATE without RETURNING (prevents EF Core non-composable SQL error)
            await Context.Database.ExecuteSqlRawAsync(sql, codesDistributed, bulkJobId);

            // Fetch the updated entity in a separate query
            var updatedJob = await Context.BulkCodeDistributionJobs
                .FirstOrDefaultAsync(j => j.Id == bulkJobId);

            return updatedJob;
        }

        /// <summary>
        /// Checks if all farmers have been processed and marks job as complete if so
        /// Determines final status based on success/failure ratio
        /// </summary>
        /// <param name="bulkJobId">The bulk job ID</param>
        /// <returns>True if job is complete, false if still in progress</returns>
        public async Task<bool> CheckAndMarkCompleteAsync(int bulkJobId)
        {
            // Fetch latest state after atomic increment
            var job = await Context.BulkCodeDistributionJobs
                .FirstOrDefaultAsync(j => j.Id == bulkJobId);

            if (job == null)
                return false;

            // Check if all farmers have been processed
            if (job.ProcessedFarmers >= job.TotalFarmers)
            {
                // Determine final status based on success/failure ratio
                job.Status = job.FailedDistributions == 0
                    ? "Completed"
                    : job.SuccessfulDistributions > 0
                        ? "PartialSuccess"
                        : "Failed";

                job.CompletedDate = DateTime.Now;

                Context.BulkCodeDistributionJobs.Update(job);
                await Context.SaveChangesAsync();

                return true; // Job is complete
            }

            return false; // Job still in progress
        }
    }
}
