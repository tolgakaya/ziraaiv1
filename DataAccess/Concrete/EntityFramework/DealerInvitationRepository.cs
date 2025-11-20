using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework
{
    /// <summary>
    /// Entity Framework implementation of IDealerInvitationRepository
    /// Provides data access for dealer invitations using PostgreSQL database
    /// </summary>
    public class DealerInvitationRepository : EfEntityRepositoryBase<DealerInvitation, ProjectDbContext>, IDealerInvitationRepository
    {
        public DealerInvitationRepository(ProjectDbContext context) : base(context)
        {
        }
    }
}
