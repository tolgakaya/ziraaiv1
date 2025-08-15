using System;
using System.Reflection;
using Core.Entities.Concrete;
using Entities.Concrete;
using SubscriptionTier = Entities.Concrete.SubscriptionTier;
using UserSubscription = Entities.Concrete.UserSubscription;
using SubscriptionUsageLog = Entities.Concrete.SubscriptionUsageLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Concrete.EntityFramework.Contexts
{
    /// <summary>
    /// Because this context is followed by migration for more than one provider
    /// works on PostGreSql db by default. If you want to pass sql
    /// When adding AddDbContext, use MsDbContext derived from it.
    /// </summary>
    public class ProjectDbContext : DbContext
    {
        /// <summary>
        /// in constructor we get IConfiguration, parallel to more than one db
        /// we can create migration.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="configuration"></param>
        public ProjectDbContext(DbContextOptions<ProjectDbContext> options, IConfiguration configuration)
            : base(options)
        {
            Configuration = configuration;
            // CRITICAL FIX: Enable legacy timestamp behavior for PostgreSQL timezone compatibility
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
        }

        /// <summary>
        /// Let's also implement the general version.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="configuration"></param>
        protected ProjectDbContext(DbContextOptions options, IConfiguration configuration)
            : base(options)
        {
            Configuration = configuration;
            // CRITICAL FIX: Enable legacy timestamp behavior for PostgreSQL timezone compatibility
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
        }

        public DbSet<OperationClaim> OperationClaims { get; set; }
        public DbSet<UserClaim> UserClaims { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<GroupClaim> GroupClaims { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<MobileLogin> MobileLogins { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Translate> Translates { get; set; }
        public DbSet<PlantAnalysis> PlantAnalyses { get; set; }
        public DbSet<Configuration> Configurations { get; set; }
        
        // Subscription System
        public DbSet<SubscriptionTier> SubscriptionTiers { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<SubscriptionUsageLog> SubscriptionUsageLogs { get; set; }
        
        // Sponsorship System
        public DbSet<SponsorshipCode> SponsorshipCodes { get; set; }
        public DbSet<SponsorshipPurchase> SponsorshipPurchases { get; set; }
        
        // Sponsor Tier-Based Benefits System
        public DbSet<SponsorProfile> SponsorProfiles { get; set; }
        public DbSet<SponsorAnalysisAccess> SponsorAnalysisAccess { get; set; }
        public DbSet<AnalysisMessage> AnalysisMessages { get; set; }
        public DbSet<SmartLink> SmartLinks { get; set; }

        protected IConfiguration Configuration { get; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            if (!optionsBuilder.IsConfigured)
            {
                base.OnConfiguring(optionsBuilder.UseNpgsql(Configuration.GetConnectionString("DArchPgContext"))
                    .EnableSensitiveDataLogging()
                    .ConfigureWarnings(warnings => 
                    {
                        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
                    }));
            }
        }
    }
}