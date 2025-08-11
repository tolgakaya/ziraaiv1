using Core.DataAccess;
using Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface IPlantAnalysisRepository : IEntityRepository<PlantAnalysis>
    {
        Task<List<PlantAnalysis>> GetListByUserIdAsync(int userId);
        Task<PlantAnalysis> GetLatestAnalysisByUserIdAsync(int userId);
    }
}