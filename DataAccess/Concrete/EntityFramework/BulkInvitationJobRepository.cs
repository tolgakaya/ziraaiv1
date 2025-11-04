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
    /// Entity Framework implementation of IBulkInvitationJobRepository
    /// Provides data access for bulk invitation jobs using PostgreSQL database
    /// </summary>
    public class BulkInvitationJobRepository : EfEntityRepositoryBase<BulkInvitationJob, ProjectDbContext>, IBulkInvitationJobRepository
    {
        public BulkInvitationJobRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<BulkInvitationJob> IncrementProgressAsync(int bulkJobId, bool success)
        {
            // Use raw SQL for atomic increment to prevent race conditions
            // This ensures concurrent Hangfire jobs don't overwrite each other's updates
            var incrementField = success ? "\"SuccessfulInvitations\"" : "\"FailedInvitations\"";

            var sql = $@"
                UPDATE ""BulkInvitationJobs""
                SET
                    ""ProcessedDealers"" = ""ProcessedDealers"" + 1,
                    {incrementField} = {incrementField} + 1
                WHERE ""Id"" = {{0}}
                RETURNING *";

            var updatedJob = await Context.BulkInvitationJobs
                .FromSqlRaw(sql, bulkJobId)
                .FirstOrDefaultAsync();

            return updatedJob;
        }

        public async Task<bool> CheckAndMarkCompleteAsync(int bulkJobId)
        {
            // Fetch latest state after atomic increment
            var job = await Context.BulkInvitationJobs
                .FirstOrDefaultAsync(j => j.Id == bulkJobId);

            if (job == null)
                return false;

            // Check if all dealers have been processed
            if (job.ProcessedDealers >= job.TotalDealers)
            {
                // Determine final status based on success/failure ratio
                job.Status = job.FailedInvitations == 0
                    ? "Completed"
                    : job.SuccessfulInvitations > 0
                        ? "PartialSuccess"
                        : "Failed";

                job.CompletedDate = DateTime.Now;

                Context.BulkInvitationJobs.Update(job);
                await Context.SaveChangesAsync();

                return true; // Job is complete
            }

            return false; // Job still in progress
        }
    }
}
