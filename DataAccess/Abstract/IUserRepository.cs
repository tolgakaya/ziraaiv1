using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
        new Task<User> GetAsync(Expression<Func<User, bool>> expression);
        new User Update(User entity);
    }
}