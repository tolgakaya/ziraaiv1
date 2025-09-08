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