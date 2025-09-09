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
        /// Configures Railway environment variables before configuration is built
        /// </summary>
        private static void ConfigureRailwayEnvironmentVariables()
        {
            try
            {
                // Check if we have DATABASE_CONNECTION_STRING but not ConnectionStrings__DArchPgContext
                var databaseConnectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
                var connectionStringFromConfig = Environment.GetEnvironmentVariable("ConnectionStrings__DArchPgContext");
                
                if (!string.IsNullOrEmpty(databaseConnectionString) && string.IsNullOrEmpty(connectionStringFromConfig))
                {
                    Environment.SetEnvironmentVariable("ConnectionStrings__DArchPgContext", databaseConnectionString);
                    Console.WriteLine($"[RAILWAY] Set ConnectionStrings__DArchPgContext from DATABASE_CONNECTION_STRING");
                }
                
                // If ConnectionStrings__DArchPgContext is already set, use it
                if (!string.IsNullOrEmpty(connectionStringFromConfig))
                {
                    Console.WriteLine($"[RAILWAY] Using existing ConnectionStrings__DArchPgContext");
                }
                
                // Log for debugging
                var finalConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DArchPgContext");
                if (!string.IsNullOrEmpty(finalConnectionString))
                {
                    var truncated = finalConnectionString.Length > 50 
                        ? finalConnectionString.Substring(0, 50) + "..." 
                        : finalConnectionString;
                    Console.WriteLine($"[RAILWAY] Final connection string: {truncated}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RAILWAY] Error configuring environment: {ex.Message}");
            }
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
                    
                    // RAILWAY FIX: Set environment variables BEFORE configuration is built
                    // This ensures connection strings are available when the configuration is loaded
                    ConfigureRailwayEnvironmentVariables();
                    
                    // Load environment-specific .env file (for local development only)
                    // Railway uses its own environment variable system
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
                        // Production: Railway provides environment variables directly
                        Console.WriteLine($"Using system environment variables ({env.EnvironmentName} mode)");
                    }
                    
                    // Add environment variables to configuration
                    config.AddEnvironmentVariables();
                    
                    // Replace placeholders in configuration with environment variables
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .ConfigureLogging(logging =>
                {
                    // RAILWAY FIX: Don't clear providers, keep console logging
                    // logging.ClearProviders(); 
                    logging.SetMinimumLevel(LogLevel.Information);
                    logging.AddConsole();
                });
    }
}