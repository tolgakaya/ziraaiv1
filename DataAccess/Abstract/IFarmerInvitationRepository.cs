using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract
{
    /// <summary>
    /// Repository interface for FarmerInvitation entity
    /// Provides data access operations for farmer invitation management
    /// </summary>
    public interface IFarmerInvitationRepository : IEntityRepository<FarmerInvitation>
    {
        // IEntityRepository<FarmerInvitation> already provides:
        // - Get(Expression<Func<FarmerInvitation, bool>> filter)
        // - GetList(Expression<Func<FarmerInvitation, bool>> filter = null)
        // - Add(FarmerInvitation entity)
        // - Update(FarmerInvitation entity)
        // - Delete(FarmerInvitation entity)
        // - GetAsync(Expression<Func<FarmerInvitation, bool>> filter)
        // - GetListAsync(Expression<Func<FarmerInvitation, bool>> filter = null)
        // - AddAsync(FarmerInvitation entity)
        // - UpdateAsync(FarmerInvitation entity)
        // - DeleteAsync(FarmerInvitation entity)
    }
}
