using System;

namespace Core.Utilities.Helpers
{
    /// <summary>
    /// Helper class for handling Railway-specific configuration
    /// </summary>
    public static class RailwayConfigurationHelper
    {
        /// <summary>
        /// Cleans connection string by removing line breaks and extra whitespace
        /// </summary>
        /// <param name="connectionString">Raw connection string that might contain line breaks</param>
        /// <returns>Cleaned connection string</returns>
        private static string CleanConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return connectionString;
            
            // Remove line breaks, carriage returns, and normalize whitespace
            return connectionString
                .Replace("\r\n", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("  ", " ") // Replace multiple spaces with single space
                .Trim();
        }

        /// <summary>
        /// Converts Railway DATABASE_URL to .NET connection string format
        /// </summary>
        /// <param name="databaseUrl">Railway DATABASE_URL in format: postgresql://user:pass@host:port/database</param>
        /// <returns>.NET formatted connection string</returns>
        public static string ConvertRailwayDatabaseUrl(string databaseUrl)
        {
            if (string.IsNullOrEmpty(databaseUrl))
                return null;

            try
            {
                var uri = new System.Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');
                var username = userInfo[0];
                var password = userInfo.Length > 1 ? userInfo[1] : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');

                // Build .NET connection string
                var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
                
                return connectionString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting DATABASE_URL: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets database connection string with Railway support
        /// </summary>
        /// <returns>Database connection string</returns>
        public static string GetDatabaseConnectionString()
        {
            // Priority order:
            // 1. DATABASE_CONNECTION_STRING (direct .NET format)
            // 2. DATABASE_URL (Railway format - needs conversion)
            // 3. Individual components (PGHOST, PGUSER, etc.)
            
            var directConnectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
            if (!string.IsNullOrEmpty(directConnectionString))
            {
                // Clean up any line breaks or extra whitespace that might come from Railway environment variables
                return CleanConnectionString(directConnectionString);
            }

            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrEmpty(databaseUrl))
                return ConvertRailwayDatabaseUrl(databaseUrl);

            // Build from individual Railway PostgreSQL variables
            var pgHost = Environment.GetEnvironmentVariable("PGHOST");
            var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
            var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
            var pgUser = Environment.GetEnvironmentVariable("PGUSER");
            var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");

            if (!string.IsNullOrEmpty(pgHost) && !string.IsNullOrEmpty(pgDatabase))
            {
                return $"Host={pgHost};Port={pgPort};Database={pgDatabase};Username={pgUser};Password={pgPassword};SSL Mode=Require;Trust Server Certificate=true";
            }

            return null;
        }

        /// <summary>
        /// Gets Redis connection string with Railway support
        /// </summary>
        /// <returns>Redis connection string</returns>
        public static string GetRedisConnectionString()
        {
            // Priority order:
            // 1. REDIS_CONNECTION (direct connection string)
            // 2. REDIS_URL (Railway format)
            // 3. Individual components (REDIS_HOST, REDIS_PORT, REDIS_PASSWORD)
            
            var directConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
            if (!string.IsNullOrEmpty(directConnection))
                return directConnection;

            var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
            if (!string.IsNullOrEmpty(redisUrl))
            {
                // Parse redis://user:password@host:port format
                var uri = new System.Uri(redisUrl);
                var redisPassword = uri.UserInfo.Contains(":") ? uri.UserInfo.Split(':')[1] : "";
                return $"{uri.Host}:{uri.Port},password={redisPassword}";
            }

            var host = Environment.GetEnvironmentVariable("REDIS_HOST");
            var port = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
            var password = Environment.GetEnvironmentVariable("REDIS_PASSWORD");

            if (!string.IsNullOrEmpty(host))
            {
                var connStr = $"{host}:{port}";
                if (!string.IsNullOrEmpty(password))
                    connStr += $",password={password}";
                return connStr;
            }

            return null;
        }

        /// <summary>
        /// Checks if running in Railway environment
        /// </summary>
        /// <returns>True if running in Railway</returns>
        public static bool IsRailwayEnvironment()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT")) ||
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RAILWAY_PROJECT_ID"));
        }

        /// <summary>
        /// Gets the current environment name with Railway detection
        /// </summary>
        /// <returns>Environment name</returns>
        public static string GetEnvironmentName()
        {
            // Check ASPNETCORE_ENVIRONMENT first
            var aspNetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!string.IsNullOrEmpty(aspNetEnv))
                return aspNetEnv;

            // Check Railway environment
            var railwayEnv = Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT");
            if (!string.IsNullOrEmpty(railwayEnv))
            {
                // Map Railway environments to ASP.NET Core environments
                return railwayEnv.ToLower() switch
                {
                    "production" => "Production",
                    "staging" => "Staging",
                    "development" => "Development",
                    _ => "Production"
                };
            }

            return "Development"; // Default
        }
    }
}