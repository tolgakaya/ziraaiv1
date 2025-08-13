using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DataAccess;
using Core.Entities.Concrete;

namespace DataAccess.Abstract
{
    public interface IUserRepository : IEntityRepository<User>
    {
        List<OperationClaim> GetClaims(int userId);
        List<string> GetUserGroups(int userId);
        Task<User> GetByRefreshToken(string refreshToken);
    }
}