using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
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
                    
                    // Load environment-specific .env file
                    var envFile = $".env.{env.EnvironmentName.ToLower()}";
                    if (File.Exists(envFile))
                    {
                        Env.Load(envFile);
                    }
                    else if (File.Exists(".env"))
                    {
                        Env.Load(".env");
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