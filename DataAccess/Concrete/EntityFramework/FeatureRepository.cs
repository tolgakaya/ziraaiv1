using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework
{
    public class FeatureRepository : EfEntityRepositoryBase<Feature, ProjectDbContext>, IFeatureRepository
    {
        public FeatureRepository(ProjectDbContext context) : base(context)
        {
        }
    }
}
