using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

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
    }
}
