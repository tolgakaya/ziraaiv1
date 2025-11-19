using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework
{
    public class AppInfoRepository : EfEntityRepositoryBase<AppInfo, ProjectDbContext>, IAppInfoRepository
    {
        public AppInfoRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<AppInfo> GetActiveAppInfoAsync()
        {
            return await Context.AppInfos.FirstOrDefaultAsync(a => a.IsActive);
        }
    }
}
