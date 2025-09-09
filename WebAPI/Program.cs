using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using DotNetEnv;

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
                
                // Check if we have DATABASE_CONNECTION_STRING but not ConnectionStrings__DArchPgContext
                var databaseConnectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
                var connectionStringFromConfig = Environment.GetEnvironmentVariable("ConnectionStrings__DArchPgContext");
                
                if (!string.IsNullOrEmpty(databaseConnectionString) && string.IsNullOrEmpty(connectionStringFromConfig))
                {
                    Environment.SetEnvironmentVariable("ConnectionStrings__DArchPgContext", databaseConnectionString);
                    Console.WriteLine($"[{cloudProvider}] Set ConnectionStrings__DArchPgContext from DATABASE_CONNECTION_STRING");
                }
                
                // If ConnectionStrings__DArchPgContext is already set, use it
                if (!string.IsNullOrEmpty(connectionStringFromConfig))
                {
                    Console.WriteLine($"[{cloudProvider}] Using existing ConnectionStrings__DArchPgContext");
                }
                
                // Log for debugging
                var finalConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DArchPgContext");
                if (!string.IsNullOrEmpty(finalConnectionString))
                {
                    var truncated = finalConnectionString.Length > 50 
                        ? finalConnectionString.Substring(0, 50) + "..." 
                        : finalConnectionString;
                    Console.WriteLine($"[{cloudProvider}] Final connection string: {truncated}");
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
            
            // Log environment variables for debugging
            Console.WriteLine($"[DEBUG] DATABASE_CONNECTION_STRING: {Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")?.Substring(0, Math.Min(30, Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")?.Length ?? 0))}...");
            Console.WriteLine($"[DEBUG] ConnectionStrings__DArchPgContext: {Environment.GetEnvironmentVariable("ConnectionStrings__DArchPgContext")?.Substring(0, Math.Min(30, Environment.GetEnvironmentVariable("ConnectionStrings__DArchPgContext")?.Length ?? 0))}...");
            
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
                    var envFile = $"../.env.{env.EnvironmentName.ToLower()}";
                    if (File.Exists(envFile))
                    {
                        // Local development: Load from .env file
                        Env.Load(envFile);
                        Console.WriteLine($"Loaded environment variables from {envFile} (Development mode)");
                    }
                    else if (File.Exists("../.env"))
                    {
                        // Fallback to generic .env file
                        Env.Load("../.env");
                        Console.WriteLine("Loaded environment variables from .env (Development mode)");
                    }
                    else
                    {
                        // Cloud/Production: Platform provides environment variables directly
                        var provider = IsCloudEnvironment() ? DetectCloudProvider() : "LOCAL";
                        Console.WriteLine($"Using system environment variables ({env.EnvironmentName} mode - {provider})");
                    }
                    
                    // CRITICAL FIX: Load JSON files FIRST, then environment variables LAST
                    // This ensures environment variables override JSON configuration values
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    
                    // Add environment variables LAST to override JSON configuration
                    config.AddEnvironmentVariables();
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .ConfigureLogging(logging =>
                {
                    // CLOUD FIX: Don't clear providers, keep console logging for cloud environments
                    // logging.ClearProviders(); 
                    logging.SetMinimumLevel(LogLevel.Information);
                    logging.AddConsole();
                });
    }
}