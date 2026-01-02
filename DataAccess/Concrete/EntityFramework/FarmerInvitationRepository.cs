using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework
{
    /// <summary>
    /// Entity Framework implementation of IFarmerInvitationRepository
    /// Provides data access for farmer invitations using PostgreSQL database
    /// </summary>
    public class FarmerInvitationRepository : EfEntityRepositoryBase<FarmerInvitation, ProjectDbContext>, IFarmerInvitationRepository
    {
        public FarmerInvitationRepository(ProjectDbContext context) : base(context)
        {
        }
    }
}
