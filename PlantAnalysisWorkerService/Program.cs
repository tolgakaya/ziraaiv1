using Business.DependencyResolvers;
using Core.Configuration;
using Core.DependencyResolvers;
using Core.Extensions;
using Core.Utilities.IoC;
using Hangfire;
using Hangfire.PostgreSql;
using PlantAnalysisWorkerService.Jobs;
using PlantAnalysisWorkerService.Services;

// CRITICAL FIX: Set PostgreSQL timezone compatibility globally
System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
System.AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

var builder = Host.CreateApplicationBuilder(args);

// Configuration Options
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection(RabbitMQOptions.SectionName));

// Hangfire Configuration
var connectionString = builder.Configuration.GetConnectionString("DArchPgContext");
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(connectionString));

builder.Services.AddHangfireServer();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Business.DependencyResolvers.AutofacBusinessModule).Assembly);

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add DbContext
builder.Services.AddDbContext<DataAccess.Concrete.EntityFramework.Contexts.ProjectDbContext>();

// Add HttpClient for FreeImageHostStorageService
builder.Services.AddHttpClient();

// Manual dependency injection for Worker Service
// Add necessary services manually instead of full business module
builder.Services.AddScoped<DataAccess.Abstract.IConfigurationRepository, DataAccess.Concrete.EntityFramework.ConfigurationRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IPlantAnalysisRepository, DataAccess.Concrete.EntityFramework.PlantAnalysisRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IUserSubscriptionRepository, DataAccess.Concrete.EntityFramework.UserSubscriptionRepository>();
builder.Services.AddScoped<DataAccess.Abstract.ISponsorshipCodeRepository, DataAccess.Concrete.EntityFramework.SponsorshipCodeRepository>();
builder.Services.AddScoped<Business.Services.Configuration.IConfigurationService, Business.Services.Configuration.ConfigurationService>();
builder.Services.AddScoped<Business.Services.FileStorage.IFileStorageService, Business.Services.FileStorage.FreeImageHostStorageService>();
builder.Services.AddScoped<Business.Services.PlantAnalysis.IPlantAnalysisService, Business.Services.PlantAnalysis.PlantAnalysisService>();

// Add HttpContextAccessor for URL generation
builder.Services.AddHttpContextAccessor();

// Add worker services
builder.Services.AddHostedService<RabbitMQConsumerWorker>();
builder.Services.AddScoped<IPlantAnalysisJobService, PlantAnalysisJobService>();

var host = builder.Build();

// Set ServiceTool for aspects
ServiceTool.ServiceProvider = host.Services;

host.Run();
