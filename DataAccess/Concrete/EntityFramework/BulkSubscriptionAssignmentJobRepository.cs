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
    /// Entity Framework implementation of IBulkSubscriptionAssignmentJobRepository
    /// Provides data access for bulk subscription assignment jobs using PostgreSQL database
    /// </summary>
    public class BulkSubscriptionAssignmentJobRepository : EfEntityRepositoryBase<BulkSubscriptionAssignmentJob, ProjectDbContext>, IBulkSubscriptionAssignmentJobRepository
    {
        public BulkSubscriptionAssignmentJobRepository(ProjectDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Atomically increments progress counters for a bulk subscription assignment job
        /// Uses raw SQL to prevent race conditions from concurrent worker processing
        /// </summary>
        /// <param name="bulkJobId">The bulk job ID</param>
        /// <param name="success">True for successful assignment, false for failed</param>
        /// <param name="subscriptionsCreated">Number of subscriptions created in this operation</param>
        /// <param name="notificationSent">Whether notification was sent (for notification count increment)</param>
        /// <returns>Updated job entity</returns>
        public async Task<BulkSubscriptionAssignmentJob> IncrementProgressAsync(
            int bulkJobId,
            bool success,
            int subscriptionsCreated = 0,
            bool notificationSent = false)
        {
            // Use raw SQL for atomic increment to prevent race conditions
            // This ensures concurrent Hangfire jobs don't overwrite each other's updates
            var successField = success ? "\"SuccessfulAssignments\"" : "\"FailedAssignments\"";
            var notificationIncrement = notificationSent ? ", \"TotalNotificationsSent\" = \"TotalNotificationsSent\" + 1" : "";

            var sql = $@"
                UPDATE ""BulkSubscriptionAssignmentJobs""
                SET
                    ""ProcessedFarmers"" = ""ProcessedFarmers"" + 1,
                    {successField} = {successField} + 1,
                    ""NewSubscriptionsCreated"" = ""NewSubscriptionsCreated"" + {{0}}
                    {notificationIncrement}
                WHERE ""Id"" = {{1}}";

            // Execute UPDATE without RETURNING (prevents EF Core non-composable SQL error)
            await Context.Database.ExecuteSqlRawAsync(sql, subscriptionsCreated, bulkJobId);

            // Fetch the updated entity in a separate query
            var updatedJob = await Context.BulkSubscriptionAssignmentJobs
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
            var job = await Context.BulkSubscriptionAssignmentJobs
                .FirstOrDefaultAsync(j => j.Id == bulkJobId);

            if (job == null)
                return false;

            // Check if all farmers have been processed
            if (job.ProcessedFarmers >= job.TotalFarmers)
            {
                // Determine final status based on success/failure ratio
                job.Status = job.FailedAssignments == 0
                    ? "Completed"
                    : job.SuccessfulAssignments > 0
                        ? "PartialSuccess"
                        : "Failed";

                job.CompletedDate = DateTime.Now;

                Context.BulkSubscriptionAssignmentJobs.Update(job);
                await Context.SaveChangesAsync();

                return true; // Job is complete
            }

            return false; // Job still in progress
        }
    }
}
