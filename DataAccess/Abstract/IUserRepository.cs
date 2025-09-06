using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Entities.Concrete;

namespace DataAccess.Abstract
{
    public interface IUserRepository : IEntityRepository<User>
    {
        Task<List<OperationClaim>> GetClaimsAsync(int userId);
        Task<List<string>> GetUserGroupsAsync(int userId);
        Task<User> GetByRefreshToken(string refreshToken);
    }
}