using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework
{
    public class PlantAnalysisRepository : EfEntityRepositoryBase<PlantAnalysis, ProjectDbContext>, IPlantAnalysisRepository
    {
        public PlantAnalysisRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<List<PlantAnalysis>> GetListByUserIdAsync(int userId)
        {
            return await Context.Set<PlantAnalysis>()
                .Where(p => p.UserId == userId && p.Status)
                .OrderByDescending(p => p.AnalysisDate)
                .ToListAsync();
        }

        public async Task<PlantAnalysis> GetLatestAnalysisByUserIdAsync(int userId)
        {
            return await Context.Set<PlantAnalysis>()
                .Where(p => p.UserId == userId && p.Status)
                .OrderByDescending(p => p.AnalysisDate)
                .FirstOrDefaultAsync();
        }
    }
}