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

// Manual dependency injection for Worker Service
// Add necessary services manually instead of full business module
builder.Services.AddScoped<DataAccess.Abstract.IConfigurationRepository, DataAccess.Concrete.EntityFramework.ConfigurationRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IPlantAnalysisRepository, DataAccess.Concrete.EntityFramework.PlantAnalysisRepository>();
builder.Services.AddScoped<DataAccess.Abstract.IUserSubscriptionRepository, DataAccess.Concrete.EntityFramework.UserSubscriptionRepository>();
builder.Services.AddScoped<DataAccess.Abstract.ISponsorshipCodeRepository, DataAccess.Concrete.EntityFramework.SponsorshipCodeRepository>();
builder.Services.AddScoped<Business.Services.Configuration.IConfigurationService, Business.Services.Configuration.ConfigurationService>();
builder.Services.AddScoped<Business.Services.FileStorage.IFileStorageService, Business.Services.FileStorage.FreeImageHostStorageService>();
builder.Services.AddScoped<Business.Services.ImageProcessing.IImageProcessingService, Business.Services.ImageProcessing.ImageProcessingService>();
builder.Services.AddScoped<Business.Services.PlantAnalysis.IPlantAnalysisService, Business.Services.PlantAnalysis.PlantAnalysisService>();

// Add HttpContextAccessor for URL generation
builder.Services.AddHttpContextAccessor();

// 🆕 Add SignalR for real-time notifications
// Worker Service needs SignalR Hub Context to send notifications
builder.Services.AddSignalR();

// 🆕 Add Plant Analysis Notification Service
builder.Services.AddScoped<Business.Services.Notification.IPlantAnalysisNotificationService, Business.Services.Notification.PlantAnalysisNotificationService>();

// Add worker services
builder.Services.AddHostedService<RabbitMQConsumerWorker>();
builder.Services.AddScoped<IPlantAnalysisJobService, PlantAnalysisJobService>();

var host = builder.Build();

// Set ServiceTool for aspects
ServiceTool.ServiceProvider = host.Services;

Console.WriteLine($"[WORKER] PlantAnalysisWorkerService starting in {env.EnvironmentName} environment");
Console.WriteLine($"[WORKER] Cloud environment: {IsCloudEnvironment()}");
Console.WriteLine($"[WORKER] Cloud provider: {DetectCloudProvider()}");

host.Run();