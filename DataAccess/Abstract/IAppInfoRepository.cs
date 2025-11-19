using Core.DataAccess;
using Entities.Concrete;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface IAppInfoRepository : IEntityRepository<AppInfo>
    {
        Task<AppInfo> GetActiveAppInfoAsync();
    }
}
