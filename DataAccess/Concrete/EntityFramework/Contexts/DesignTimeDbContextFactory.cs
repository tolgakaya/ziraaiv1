using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace DataAccess.Concrete.EntityFramework.Contexts
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ProjectDbContext>
    {
        public ProjectDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../WebAPI"))
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var builder = new DbContextOptionsBuilder<ProjectDbContext>();
            var connectionString = configuration.GetConnectionString("DArchPgContext");
            builder.UseNpgsql(connectionString);

            return new ProjectDbContext(builder.Options, configuration);
        }
    }
}