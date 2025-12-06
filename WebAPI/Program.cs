using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using DotNetEnv;
using Serilog;
using Serilog.Events;

namespace WebAPI
{
    /// <summary>
    ///
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Detects if running in a cloud environment (Railway, Azure, AWS, etc.)
        /// </summary>
        private static bool IsCloudEnvironment()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT")) ||
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")) || // Azure
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")) || // AWS
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")); // Container environments
        }

        /// <summary>
        /// Configures cloud environment variables before configuration is built
        /// </summary>
        private static void ConfigureCloudEnvironmentVariables()
        {
            try
            {
                var cloudProvider = DetectCloudProvider();
                var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

                // Check if we have DATABASE_CONNECTION_STRING but not ConnectionStrings__DArchPgContext
                var databaseConnectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
                var connectionStringFromConfig = Environment.GetEnvironmentVariable("ConnectionStrings__DArchPgContext");

                if (!string.IsNullOrEmpty(databaseConnectionString) && string.IsNullOrEmpty(connectionStringFromConfig))
                {
                    Environment.SetEnvironmentVariable("ConnectionStrings__DArchPgContext", databaseConnectionString);
                    if (isDevelopment)
                    {
                        Console.WriteLine($"[{cloudProvider}] Set ConnectionStrings__DArchPgContext from DATABASE_CONNECTION_STRING");
                    }
                }

                // Only log in development
                if (isDevelopment && !string.IsNullOrEmpty(connectionStringFromConfig))
                {
                    Console.WriteLine($"[{cloudProvider}] Using existing ConnectionStrings__DArchPgContext");
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
        private static string DetectCloudProvider()
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // CRITICAL FIX: Set PostgreSQL timezone compatibility globally
            System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            System.AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    // CLOUD FIX: Set environment variables BEFORE configuration is built
                    // This ensures connection strings are available when the configuration is loaded
                    if (IsCloudEnvironment())
                    {
                        ConfigureCloudEnvironmentVariables();
                    }

                    // Load environment-specific .env file (for local development only)
                    // Cloud platforms provide environment variables directly
                    var isDevelopment = env.EnvironmentName == "Development";
                    var envFile = $"../.env.{env.EnvironmentName.ToLower()}";

                    if (File.Exists(envFile))
                    {
                        // Local development: Load from .env file
                        Env.Load(envFile);
                        if (isDevelopment)
                        {
                            Console.WriteLine($"Loaded environment variables from {envFile}");
                        }
                    }
                    else if (File.Exists("../.env"))
                    {
                        // Fallback to generic .env file
                        Env.Load("../.env");
                        if (isDevelopment)
                        {
                            Console.WriteLine("Loaded environment variables from .env");
                        }
                    }

                    // CRITICAL FIX: Load JSON files FIRST, then environment variables LAST
                    // This ensures environment variables override JSON configuration values
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                    // Add environment variables LAST to override JSON configuration
                    config.AddEnvironmentVariables();
                })
                .UseSerilog((context, configuration) =>
                {
                    // Configure SeriLog from appsettings.json
                    var fileLogConfig = context.Configuration.GetSection("SeriLogConfigurations:FileLogConfiguration");
                    var isProduction = context.HostingEnvironment.IsProduction();
                    var isStaging = context.HostingEnvironment.EnvironmentName == "Staging";

                    // Environment-based minimum level
                    var minimumLevel = isProduction || isStaging ? LogEventLevel.Warning : LogEventLevel.Debug;
                    var microsoftLevel = isProduction || isStaging ? LogEventLevel.Error : LogEventLevel.Information;
                    var businessLevel = isProduction || isStaging ? LogEventLevel.Information : LogEventLevel.Debug;

                    // Base configuration with console output
                    configuration
                        .MinimumLevel.Is(minimumLevel)
                        .MinimumLevel.Override("Microsoft", microsoftLevel)
                        .MinimumLevel.Override("System", microsoftLevel)
                        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", microsoftLevel)
                        .MinimumLevel.Override("Business", businessLevel)
                        .MinimumLevel.Override("WebAPI", businessLevel)
                        .MinimumLevel.Override("PlantAnalysisWorkerService", businessLevel)
                        .Enrich.FromLogContext()
                        .WriteTo.Console(outputTemplate: isProduction || isStaging
                            ? "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                            : "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}");

                    // Add file logging if configured
                    if (fileLogConfig.Exists())
                    {
                        var folderPath = fileLogConfig["FolderPath"];
                        var outputTemplate = fileLogConfig["OutputTemplate"];

                        if (!string.IsNullOrEmpty(folderPath))
                        {
                            try
                            {
                                // Ensure logs directory exists
                                var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), folderPath);
                                Directory.CreateDirectory(logDirectory);

                                configuration.WriteTo.File(
                                    path: Path.Combine(logDirectory, "log-.txt"),
                                    outputTemplate: outputTemplate ?? "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                                    rollingInterval: RollingInterval.Hour,
                                    retainedFileCountLimit: 24,
                                    fileSizeLimitBytes: 10485760,
                                    restrictedToMinimumLevel: minimumLevel);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[SERILOG] File logging configuration failed: {ex.Message}");
                            }
                        }
                    }
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}