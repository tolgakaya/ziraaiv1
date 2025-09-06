using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Core.DataAccess.EntityFramework;
using Core.Entities.Concrete;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Concrete.EntityFramework
{
    public class UserRepository : EfEntityRepositoryBase<User, ProjectDbContext>, IUserRepository
    {
        public UserRepository(ProjectDbContext context)
            : base(context)
        {
        }

        public new async Task<User> GetAsync(Expression<Func<User, bool>> filter)
        {
            var user = await base.GetAsync(filter);
            if (user != null)
            {
                FixInfinityValues(user);
            }
            return user;
        }

        public new User Update(User entity)
        {
            FixInfinityValues(entity);
            return base.Update(entity);
        }

        private void FixInfinityValues(User user)
        {
            // Handle infinity values that might come from database
            if (user.BirthDate.HasValue && user.BirthDate.Value == DateTime.MaxValue)
            {
                user.BirthDate = null;
            }
            if (user.UpdateContactDate == DateTime.MaxValue)
            {
                user.UpdateContactDate = DateTime.Now;
            }
            if (user.RecordDate == DateTime.MaxValue)
            {
                user.RecordDate = DateTime.Now;
            }
        }

        public async Task<List<OperationClaim>> GetClaimsAsync(int userId)
        {
            var result = (from user in Context.Users
                join userGroup in Context.UserGroups on user.UserId equals userGroup.UserId
                join groupClaim in Context.GroupClaims on userGroup.GroupId equals groupClaim.GroupId
                join operationClaim in Context.OperationClaims on groupClaim.ClaimId equals operationClaim.Id
                where user.UserId == userId
                select new
                {
                    operationClaim.Name
                }).Union(from user in Context.Users
                join userClaim in Context.UserClaims on user.UserId equals userClaim.UserId
                join operationClaim in Context.OperationClaims on userClaim.ClaimId equals operationClaim.Id
                where user.UserId == userId
                select new
                {
                    operationClaim.Name
                });

            var list = await result.Select(x => new OperationClaim { Name = x.Name }).Distinct()
                .ToListAsync();
            return list;
        }

        public async Task<List<string>> GetUserGroupsAsync(int userId)
        {
            var result = from user in Context.Users
                         join userGroup in Context.UserGroups on user.UserId equals userGroup.UserId
                         join grp in Context.Groups on userGroup.GroupId equals grp.Id
                         where user.UserId == userId
                         select grp.GroupName;

            return await result.Distinct().ToListAsync();
        }

        public async Task<User> GetByRefreshToken(string refreshToken)
        {
            return await Context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.Status);
        }
    }
}