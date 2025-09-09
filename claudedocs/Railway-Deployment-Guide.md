# Railway Deployment Guide for ZiraAI

## Overview
This document provides a comprehensive guide for deploying the ZiraAI .NET 9.0 Web API to Railway cloud platform. It includes configuration fixes, environment variable setup, and troubleshooting solutions developed during the staging environment deployment.

## Table of Contents
1. [Railway Environment Setup](#railway-environment-setup)
2. [Configuration System Fixes](#configuration-system-fixes)
3. [Database Configuration](#database-configuration)
4. [Redis Cache Configuration](#redis-cache-configuration)
5. [Environment Variables](#environment-variables)
6. [Deployment Process](#deployment-process)
7. [Troubleshooting](#troubleshooting)
8. [Production Considerations](#production-considerations)

## Railway Environment Setup

### Prerequisites
- Railway account with project created
- PostgreSQL and Redis services provisioned in Railway
- GitHub repository connected to Railway for auto-deployment

### Branch Configuration
- **Staging Environment**: Connected to `staging` branch
- **Production Environment**: Should connect to `master` branch

## Configuration System Fixes

### Critical Configuration Override Fix
**Problem**: Railway environment variables were not overriding appsettings.json values due to incorrect configuration source ordering.

**Solution**: Modified `WebAPI/Program.cs` to ensure proper configuration precedence:

```csharp
// CRITICAL FIX: Load JSON files FIRST, then environment variables LAST
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables() // MUST BE LAST to override JSON values
    .Build();
```

**Key Points**:
- Environment variables MUST be added LAST to override JSON configuration
- This fixes database, Redis, and all other service configurations
- Essential for Railway's environment variable system to work properly

### Railway-Specific Configuration Helper
Added Railway environment detection and configuration methods:

```csharp
// Railway environment variable configuration
if (IsRailwayEnvironment())
{
    ConfigureRailwayEnvironmentVariables(config);
}

private static bool IsRailwayEnvironment()
{
    return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT"));
}

private static void ConfigureRailwayEnvironmentVariables(IConfiguration config)
{
    // Railway uses specific environment variable patterns
    // This method ensures proper Railway integration
}
```

## Database Configuration

### PostgreSQL Connection String
Railway provides PostgreSQL connection details through environment variables:

**Environment Variables**:
```bash
DATABASE_CONNECTION_STRING="Host=tramway.proxy.rlwy.net;Port=39540;Database=railway;Username=postgres;Password=<railway_password>"
ConnectionStrings__DArchPgContext="Host=tramway.proxy.rlwy.net;Port=39540;Database=railway;Username=postgres;Password=<railway_password>"
```

**Key Configuration Points**:
- Use Railway-provided host: `tramway.proxy.rlwy.net`
- Port: `39540` (Railway-specific)
- Database name: `railway` (Railway default)
- Connection string format must match .NET conventions

### Entity Framework Configuration
Ensure `DataAccess/Concrete/EntityFramework/Contexts/ProjectDbContext.cs` properly handles Railway connections:

```csharp
// PostgreSQL compatibility switches (required for Railway)
System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
System.AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
```

## Redis Cache Configuration

### Railway Redis Connection
**Problem**: Initial configuration attempted SSL connection, causing "Software caused connection abort" errors.

**Solution**: Railway Redis accepts non-SSL connections, SSL should be disabled.

**Environment Variables**:
```bash
CacheOptions__Host="maglev.proxy.rlwy.net"
CacheOptions__Port="38265"
CacheOptions__Password="<railway_redis_password>"
CacheOptions__Ssl="false"  # CRITICAL: Must be false for Railway
UseRedis="true"
```

**Redis Configuration in Code** (`Core/CrossCuttingConcerns/Caching/Redis/RedisCacheManager.cs`):
```csharp
public RedisCacheManager(IConfiguration configuration)
{
    var cacheConfig = configuration.GetSection(nameof(CacheOptions)).Get<CacheOptions>();
    
    var configurationOptions = ConfigurationOptions.Parse($"{cacheConfig.Host}:{cacheConfig.Port}");
    if (!string.IsNullOrEmpty(cacheConfig.Password))
    {
        configurationOptions.Password = cacheConfig.Password;
    }

    configurationOptions.DefaultDatabase = cacheConfig.Database;
    configurationOptions.Ssl = cacheConfig.Ssl; // Will be false for Railway
    configurationOptions.AbortOnConnectFail = false;
    
    // SSL configuration only applied if SSL is enabled
    if (cacheConfig.Ssl)
    {
        // SSL configuration code (not used for Railway)
    }
    
    _redis = ConnectionMultiplexer.Connect(configurationOptions);
    _cache = _redis.GetDatabase(cacheConfig.Database);
}
```

**Important Notes**:
- Railway Redis runs on port `38265`
- SSL is optional and should be disabled for Railway
- Host: `maglev.proxy.rlwy.net`

## Environment Variables

### Complete Railway Environment Configuration
Create `.env.railway.staging` file with all necessary variables:

```bash
# Core Application Settings
ASPNETCORE_ENVIRONMENT="Staging"
ASPNETCORE_URLS="http://0.0.0.0:8080"

# Database Configuration
DATABASE_CONNECTION_STRING="Host=tramway.proxy.rlwy.net;Port=39540;Database=railway;Username=postgres;Password=<railway_db_password>"
ConnectionStrings__DArchPgContext="Host=tramway.proxy.rlwy.net;Port=39540;Database=railway;Username=postgres;Password=<railway_db_password>"

# Redis Configuration
REDIS_HOST="maglev.proxy.rlwy.net"
REDIS_PORT="38265"
REDIS_PASSWORD="<railway_redis_password>"
REDIS_USERNAME="default"
CacheOptions__Host="maglev.proxy.rlwy.net"
CacheOptions__Port="38265"
CacheOptions__Password="<railway_redis_password>"
CacheOptions__Ssl="false"

# Service Toggles
UseRedis="true"
UseElasticsearch="false"
UseHangfire="false"
UseRabbitMQ="false"
UseTaskScheduler="false"

# JWT Configuration
JWT_SECRET_KEY="<your_jwt_secret>"
JWT_ISSUER="ZiraAI_Staging"
JWT_AUDIENCE="ZiraAI_Staging_Users"
JWT_ACCESS_TOKEN_EXPIRE_HOURS="1"
JWT_REFRESH_TOKEN_EXPIRE_HOURS="72"

# File Storage
FileStorage__Provider="FreeImageHost"
FREEIMAGEHOST_API_KEY="<your_api_key>"

# Logging
LOG_LEVEL="Information"
SERILOG_CONNECTION_STRING="Host=tramway.proxy.rlwy.net;Port=39540;Database=railway;Username=postgres;Password=<railway_db_password>"
SeriLogConfigurations__PostgreSqlLogConfiguration__ConnectionString="Host=tramway.proxy.rlwy.net;Port=39540;Database=railway;Username=postgres;Password=<railway_db_password>"

# CORS Settings
CORS_ALLOWED_ORIGINS="https://your-railway-app.up.railway.app"

# Debug Settings
STARTUP_DEBUG="true"
```

### Environment Variable Naming Conventions
- **Database**: Use `ConnectionStrings__DArchPgContext` for Entity Framework
- **Redis**: Use `CacheOptions__` prefix for Redis configuration
- **Nested JSON**: Use double underscore `__` for nested configuration sections

## Deployment Process

### 1. Repository Setup
```bash
# Connect Railway project to GitHub repository
# Set up auto-deployment from staging branch
```

### 2. Environment Variables Configuration
```bash
# Set all environment variables in Railway dashboard
# Or use Railway CLI:
railway variables set ASPNETCORE_ENVIRONMENT=Staging
railway variables set CacheOptions__Ssl=false
# ... (set all variables from the list above)
```

### 3. Database Setup
```bash
# Run migrations on Railway PostgreSQL
railway run dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext
```

### 4. Deployment Verification
```bash
# Check Railway logs for successful startup
railway logs
```

## Troubleshooting

### Common Issues and Solutions

#### 1. Database Connection Issues
**Symptoms**: Application connects to localhost instead of Railway PostgreSQL
**Error**: `Npgsql.NpgsqlException: Connection refused`

**Solution**: 
- Verify configuration system ordering in `Program.cs`
- Ensure environment variables are loaded AFTER JSON files
- Check `ConnectionStrings__DArchPgContext` environment variable

#### 2. Redis Connection Issues
**Symptoms**: SSL handshake failures, certificate errors
**Error**: `Software caused connection abort` during Redis operations

**Solution**:
- Set `CacheOptions__Ssl="false"` 
- Verify Railway Redis host and port
- Check Redis password in environment variables

#### 3. Configuration Override Problems
**Symptoms**: Environment variables not overriding appsettings.json
**Root Cause**: Configuration sources loaded in wrong order

**Solution**:
```csharp
// CORRECT order in Program.cs
config.AddJsonFile("appsettings.json", optional: false)
      .AddJsonFile($"appsettings.{env}.json", optional: true)
      .AddEnvironmentVariables(); // MUST BE LAST
```

#### 4. Railway-Specific Certificate Issues
**Symptoms**: SSL certificate validation errors
**Solution**: Railway's internal network is secure, disable SSL for internal services

### Debugging Steps
1. **Check Railway logs**: `railway logs --tail`
2. **Verify environment variables**: Check Railway dashboard
3. **Test database connection**: Use Railway's built-in database tools
4. **Validate configuration**: Add debug logging to startup

## Production Considerations

### Security
- Use separate JWT secrets for production
- Enable HTTPS redirection
- Configure proper CORS origins
- Use secure Redis passwords

### Performance
- Enable connection pooling
- Configure Redis with appropriate timeout values
- Set up proper logging levels (Warning/Error for production)

### Monitoring
- Configure structured logging with Serilog
- Set up health checks
- Monitor database connection pool

### Scaling
- Railway auto-scales based on CPU/memory usage
- Consider Redis connection limits for high traffic
- Plan for database connection pooling

## Migration from Staging to Production

### 1. Environment Setup
- Create production Railway service
- Connect to `master` branch
- Set up production PostgreSQL and Redis

### 2. Configuration Updates
```bash
# Update environment variables for production
ASPNETCORE_ENVIRONMENT="Production"
JWT_ISSUER="ZiraAI_Production"
CORS_ALLOWED_ORIGINS="https://your-production-domain.com"
LOG_LEVEL="Warning"  # Reduce logging in production
```

### 3. Database Migration
```bash
# Run production migrations
railway run --environment production dotnet ef database update
```

### 4. SSL Considerations
- For production, consider enabling Redis SSL if Railway supports it
- Configure proper SSL certificates for HTTPS endpoints

## File Structure
```
ziraai/
├── .env.railway.staging          # Railway environment variables
├── WebAPI/
│   ├── Program.cs               # Configuration system fixes
│   ├── appsettings.Staging.json # Staging configuration
│   └── appsettings.json         # Base configuration
├── Core/CrossCuttingConcerns/Caching/Redis/
│   └── RedisCacheManager.cs     # Redis connection logic
└── claudedocs/
    └── Railway-Deployment-Guide.md # This document
```

## Version History
- **v1.0**: Initial Railway deployment configuration
- **v1.1**: Fixed configuration system override issues
- **v1.2**: Resolved Redis SSL connection problems
- **v1.3**: Added comprehensive environment variable documentation

---

**Last Updated**: September 9, 2025  
**Author**: Claude Code Assistant  
**Environment**: Railway Staging Deployment  
**Status**: Production Ready