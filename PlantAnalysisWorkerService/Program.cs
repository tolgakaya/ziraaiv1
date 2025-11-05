using Business.DependencyResolvers;
using Core.Configuration;
using Core.DependencyResolvers;
using Core.Extensions;
using Core.Utilities.IoC;
using Hangfire;
using Hangfire.PostgreSql;
using PlantAnalysisWorkerService.Jobs;
using PlantAnalysisWorkerService.Services;
using System;
using System.IO;
using DotNetEnv;

// CRITICAL FIX: Set PostgreSQL timezone compatibility globally
System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
System.AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

/// <summary>
/// Detects if running in a cloud environment (Railway, Azure, AWS, etc.)
/// </summary>
static bool IsCloudEnvironment()
{
    return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT")) ||
           !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")) || // Azure
           !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")) || // AWS
           !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")); // Container environments
}

/// <summary>
/// Configures cloud environment variables before configuration is built
/// </summary>
static void ConfigureCloudEnvironmentVariables()
{
    try
    {
        var cloudProvider = DetectCloudProvider();
        
        // Use Railway configuration helper to get proper connection string
        var databaseConnectionString = Core.Utilities.Helpers.RailwayConfigurationHelper.GetDatabaseConnectionString();
        var connectionStringFromConfig = Environment.GetEnvironmentVariable("ConnectionStrings__DArchPgContext");
        
        // Debug: Log Railway environment variables
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        var pgHost = Environment.GetEnvironmentVariable("PGHOST");
        var railwayEnv = Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT");
        
        Console.WriteLine($"[{cloudProvider}] Debug - DATABASE_URL exists: {!string.IsNullOrEmpty(databaseUrl)}");
        Console.WriteLine($"[{cloudProvider}] Debug - PGHOST exists: {!string.IsNullOrEmpty(pgHost)}");
        Console.WriteLine($"[{cloudProvider}] Debug - RAILWAY_ENVIRONMENT: {railwayEnv ?? "not set"}");
        Console.WriteLine($"[{cloudProvider}] Debug - ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "not set"}");
        
        if (!string.IsNullOrEmpty(databaseConnectionString))
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DArchPgContext", databaseConnectionString);
            Console.WriteLine($"[{cloudProvider}] Set ConnectionStrings__DArchPgContext from Railway helper");
            
            // Also set TaskScheduler connection string
            Environment.SetEnvironmentVariable("TaskSchedulerOptions__ConnectionString", databaseConnectionString);
            Console.WriteLine($"[{cloudProvider}] Set TaskSchedulerOptions__ConnectionString from Railway helper");
        }
        else if (!string.IsNullOrEmpty(connectionStringFromConfig))
        {
            Console.WriteLine($"[{cloudProvider}] Using existing ConnectionStrings__DArchPgContext from environment");
        }
        else
        {
            Console.WriteLine($"[{cloudProvider}] No database connection string available - will use appsettings.json");
        }
        
        // Log connection string for debugging (safely)
        var finalConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DArchPgContext");
        if (!string.IsNullOrEmpty(finalConnectionString))
        {
            // Remove password for logging
            var safeConnectionString = finalConnectionString;
            if (safeConnectionString.Contains("Password="))
            {
                var parts = safeConnectionString.Split(';');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith("Password="))
                    {
                        parts[i] = "Password=***";
                        break;
                    }
                }
                safeConnectionString = string.Join(";", parts);
            }
            Console.WriteLine($"[{cloudProvider}] Final connection string: {safeConnectionString}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CLOUD] Error configuring environment: {ex.Message}");
    }
}

/// <summary>
/// Detects which cloud provider we're running on
/// </summary>
static string DetectCloudProvider()
{
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT")))
        return "RAILWAY";
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
        return "AZURE";
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")))
        return "AWS";
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")))
        return "CONTAINER";
    
    return "LOCAL";
}

// FIX: Use ASPNETCORE_ENVIRONMENT instead of DOTNET_ENVIRONMENT (to match WebAPI behavior)
// Host.CreateApplicationBuilder uses DOTNET_ENVIRONMENT by default
// but we want to use ASPNETCORE_ENVIRONMENT for consistency with WebAPI
var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (!string.IsNullOrEmpty(aspnetEnv))
{
    Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", aspnetEnv);
    Console.WriteLine($"[WORKER] Using ASPNETCORE_ENVIRONMENT: {aspnetEnv}");
}

var builder = Host.CreateApplicationBuilder(args);

// CLOUD FIX: Set environment variables BEFORE configuration is built
// This ensures connection strings are available when the configuration is loaded
if (IsCloudEnvironment())
{
    ConfigureCloudEnvironmentVariables();
}

// Load environment-specific .env file (for local development only)
// Cloud platforms provide environment variables directly
var env = builder.Environment;
var envFile = $"../.env.{env.EnvironmentName.ToLower()}";
if (File.Exists(envFile))
{
    // Local development: Load from .env file
    Env.Load(envFile);
    Console.WriteLine($"[WORKER] Loaded environment variables from {envFile} (Development mode)");
}
else if (!IsCloudEnvironment())
{
    // Try loading generic .env file for local development
    var genericEnvFile = "../.env";
    if (File.Exists(genericEnvFile))
    {
        Env.Load(genericEnvFile);
        Console.WriteLine($"[WORKER] Loaded environment variables from {genericEnvFile} (Local development)");
    }
    else
    {
        Console.WriteLine($"[WORKER] No .env file found for local development");
    }
}
else
{
    Console.WriteLine($"[WORKER] Cloud environment detected - using platform environment variables");
}

// Configuration Options
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection(RabbitMQOptions.SectionName));

// Hangfire Configuration
var connectionString = builder.Configuration.GetConnectionString("DArchPgContext");
Console.WriteLine($"[WORKER] Using connection string: {connectionString?.Substring(0, Math.Min(50, connectionString?.Length ?? 0))}...");

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

// Configure named HttpClient for WebAPI communication (notifications, SignalR callbacks)
builder.Services.AddHttpClient("WebAPI", client =>
{
    var webApiBaseUrl = builder.Configuration.GetValue<string>("WebAPI:BaseUrl")
                       ?? "https://localhost:5001";

    client.BaseAddress = new Uri(webApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Configure connection pool settings for concurrent requests
    MaxConnectionsPerServer = 10
});

// Manual dependency injection for Worker Service
// Add necessary services manually instead of full business module
builder.Services.AddScoped<DataAccess.Abstract.IConfigurationRepository, DataAccess.Concrete.EntityFramework.ConfigurationRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IPlantAnalysisRepository, DataAccess.Concrete.EntityFramework.PlantAnalysisRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IUserSubscriptionRepository, DataAccess.Concrete.EntityFramework.UserSubscriptionRepository>();
builder.Services.AddScoped<DataAccess.Abstract.ISponsorshipCodeRepository, DataAccess.Concrete.EntityFramework.SponsorshipCodeRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IReferralCodeRepository, DataAccess.Concrete.EntityFramework.ReferralCodeRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IReferralTrackingRepository, DataAccess.Concrete.EntityFramework.ReferralTrackingRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IReferralRewardRepository, DataAccess.Concrete.EntityFramework.ReferralRewardRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IReferralConfigurationRepository, DataAccess.Concrete.EntityFramework.ReferralConfigurationRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IDealerInvitationRepository, DataAccess.Concrete.EntityFramework.DealerInvitationRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IBulkInvitationJobRepository, DataAccess.Concrete.EntityFramework.BulkInvitationJobRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IBulkCodeDistributionJobRepository, DataAccess.Concrete.EntityFramework.BulkCodeDistributionJobRepository>();
builder.Services.AddScoped<DataAccess.Abstract.ISmsLogRepository, DataAccess.Concrete.EntityFramework.SmsLogRepository>();
// 🆕 Add missing repositories required by CreateDealerInvitationCommandHandler
builder.Services.AddScoped<DataAccess.Abstract.IUserRepository, DataAccess.Concrete.EntityFramework.UserRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IGroupRepository, DataAccess.Concrete.EntityFramework.GroupRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IUserGroupRepository, DataAccess.Concrete.EntityFramework.UserGroupRepository>();
builder.Services.AddScoped<DataAccess.Abstract.ISubscriptionTierRepository, DataAccess.Concrete.EntityFramework.SubscriptionTierRepository>();
builder.Services.AddScoped<DataAccess.Abstract.ISponsorProfileRepository, DataAccess.Concrete.EntityFramework.SponsorProfileRepository>();
builder.Services.AddScoped<Business.Services.Configuration.IConfigurationService, Business.Services.Configuration.ConfigurationService>();
// Use RedisCacheManager to match API's cache provider for cross-service cache invalidation
builder.Services.AddSingleton<Core.CrossCuttingConcerns.Caching.ICacheManager, Core.CrossCuttingConcerns.Caching.Redis.RedisCacheManager>();
builder.Services.AddScoped<Business.Services.FileStorage.IFileStorageService, Business.Services.FileStorage.FreeImageHostStorageService>();
builder.Services.AddScoped<Business.Services.ImageProcessing.IImageProcessingService, Business.Services.ImageProcessing.ImageProcessingService>();
builder.Services.AddScoped<Business.Services.PlantAnalysis.IPlantAnalysisService, Business.Services.PlantAnalysis.PlantAnalysisService>();
builder.Services.AddScoped<Business.Services.Referral.IReferralTrackingService, Business.Services.Referral.ReferralTrackingService>();
builder.Services.AddScoped<Business.Services.Referral.IReferralRewardService, Business.Services.Referral.ReferralRewardService>();
builder.Services.AddScoped<Business.Services.Referral.IReferralConfigurationService, Business.Services.Referral.ReferralConfigurationService>();

// Add HttpContextAccessor for URL generation
builder.Services.AddHttpContextAccessor();

// 🆕 Add SignalR for real-time notifications
// Worker Service needs SignalR Hub Context to send notifications
builder.Services.AddSignalR();

// 🆕 Add Plant Analysis Notification Service
builder.Services.AddScoped<Business.Services.Notification.IPlantAnalysisNotificationService, Business.Services.Notification.PlantAnalysisNotificationService>();

// 🆕 Add Bulk Invitation Notification Service
builder.Services.AddScoped<Business.Services.Notification.IBulkInvitationNotificationService, Business.Services.Notification.BulkInvitationNotificationService>();
builder.Services.AddScoped<Business.Services.Notification.IBulkCodeDistributionNotificationService, Business.Services.Notification.BulkCodeDistributionNotificationService>();

// 🆕 Add SMS and WhatsApp Services via Factory Pattern (matches WebAPI approach)
builder.Services.AddScoped<Business.Services.Messaging.ISmsService, Business.Services.Messaging.Fakes.MockSmsService>();
builder.Services.AddScoped<Business.Services.Messaging.TurkcellSmsService>();
builder.Services.AddScoped<Business.Services.Messaging.IWhatsAppService, Business.Services.Messaging.Fakes.MockWhatsAppService>();

// 🆕 Add SMS Logging Service (config-controlled debugging feature)
builder.Services.AddScoped<Business.Services.Logging.ISmsLoggingService, Business.Services.Logging.SmsLoggingService>();
builder.Services.AddScoped<Business.Services.Messaging.WhatsAppBusinessService>();
builder.Services.AddScoped<Business.Services.Messaging.Factories.IMessagingServiceFactory, Business.Services.Messaging.Factories.MessagingServiceFactory>();

// 🆕 Add MediatR for CQRS (required by DealerInvitationJobService)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Business.DependencyResolvers.AutofacBusinessModule).Assembly));

// Add worker services
builder.Services.AddHostedService<RabbitMQConsumerWorker>();
builder.Services.AddScoped<IPlantAnalysisJobService, PlantAnalysisJobService>();

// 🆕 Add Dealer Invitation Worker and Job Service
builder.Services.AddHostedService<DealerInvitationConsumerWorker>();
builder.Services.AddScoped<IDealerInvitationJobService, DealerInvitationJobService>();

// 🆕 Add Farmer Code Distribution Worker and Job Service
builder.Services.AddHostedService<FarmerCodeDistributionConsumerWorker>();
builder.Services.AddScoped<IFarmerCodeDistributionJobService, FarmerCodeDistributionJobService>();

var host = builder.Build();

// Set ServiceTool for aspects
ServiceTool.ServiceProvider = host.Services;

Console.WriteLine($"[WORKER] PlantAnalysisWorkerService starting in {env.EnvironmentName} environment");
Console.WriteLine($"[WORKER] Cloud environment: {IsCloudEnvironment()}");
Console.WriteLine($"[WORKER] Cloud provider: {DetectCloudProvider()}");

host.Run();