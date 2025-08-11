using Business.DependencyResolvers;
using Core.Configuration;
using Core.DependencyResolvers;
using Core.Extensions;
using Core.Utilities.IoC;
using Hangfire;
using Hangfire.PostgreSql;
using PlantAnalysisWorkerService.Jobs;
using PlantAnalysisWorkerService.Services;

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

// Manual dependency injection for Worker Service
// Add necessary services manually instead of full business module
builder.Services.AddSingleton<Business.Services.Configuration.IConfigurationService, Business.Services.Configuration.ConfigurationService>();
builder.Services.AddScoped<DataAccess.Abstract.IPlantAnalysisRepository, DataAccess.Concrete.EntityFramework.PlantAnalysisRepository>();
builder.Services.AddDbContext<DataAccess.Concrete.EntityFramework.Contexts.ProjectDbContext>();

// Add worker services
builder.Services.AddHostedService<RabbitMQConsumerWorker>();
builder.Services.AddScoped<IPlantAnalysisJobService, PlantAnalysisJobService>();

var host = builder.Build();

// Set ServiceTool for aspects
ServiceTool.ServiceProvider = host.Services;

host.Run();
