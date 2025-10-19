using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete.EntityFramework
{
    public class MessagingFeatureRepository : EfRepositoryBase<MessagingFeature, ProjectDbContext>, IMessagingFeatureRepository
    {
        public MessagingFeatureRepository(ProjectDbContext context) : base(context)
        {
        }
    }
}
