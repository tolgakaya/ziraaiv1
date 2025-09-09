# Railway PostgreSQL Timeout Fix

## Problem
Registration endpoint failing with timeout error after 30 seconds:
- Error: `Timeout during reading attempt`
- Inner Exception: `An exception has been raised that is likely due to a transient failure`
- Database operation timing out while saving user data

## Root Cause
Railway PostgreSQL connection experiencing network latency or connection pool exhaustion due to:
1. No explicit timeout configuration in connection string
2. Default connection pool settings may be insufficient
3. Railway's network routing may add latency

## Solution
Updated PostgreSQL connection strings with comprehensive timeout and pooling parameters:

### Connection String Parameters Added:
```
Timeout=30                    # Connection timeout (seconds)
Command Timeout=30            # Command execution timeout (seconds)  
Max Pool Size=50             # Maximum connections in pool
Min Pool Size=5              # Minimum connections to maintain
Connection Idle Lifetime=300  # Max idle time before pruning (seconds)
Connection Lifetime=0        # Max connection lifetime (0=infinite)
Pooling=true                 # Enable connection pooling
Connection Pruning Interval=10 # How often to prune idle connections
```

### Updated Environment Variables:
```bash
DATABASE_CONNECTION_STRING="Host=tramway.proxy.rlwy.net;Port=39540;Database=railway;Username=postgres;Password=cEAvVsWsZIHDUaUKUMiSTRaTGmuswdEd;Timeout=30;Command Timeout=30;Max Pool Size=50;Min Pool Size=5;Connection Idle Lifetime=300;Connection Lifetime=0;Pooling=true;Connection Pruning Interval=10"

ConnectionStrings__DArchPgContext="Host=tramway.proxy.rlwy.net;Port=39540;Database=railway;Username=postgres;Password=cEAvVsWsZIHDUaUKUMiSTRaTGmuswdEd;Timeout=30;Command Timeout=30;Max Pool Size=50;Min Pool Size=5;Connection Idle Lifetime=300;Connection Lifetime=0;Pooling=true;Connection Pruning Interval=10"
```

## Implementation Steps
1. Copy the updated connection strings from `.env.railway.staging.all`
2. Go to Railway Dashboard > Variables
3. Update both `DATABASE_CONNECTION_STRING` and `ConnectionStrings__DArchPgContext`
4. Save changes - Railway will automatically redeploy

## Additional Optimizations (if timeout persists)
If the problem continues after applying these settings:

### Option 1: Increase Timeouts
```
Timeout=60
Command Timeout=60
```

### Option 2: Optimize Entity Framework
Add to `Program.cs`:
```csharp
services.AddDbContext<ProjectDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(60);
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
});
```

### Option 3: Add Resilience with Polly
```csharp
services.AddDbContext<ProjectDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure();
    });
});
```

## Monitoring
After applying the fix, monitor for:
- Registration endpoint response times
- Database connection pool metrics
- Any recurring timeout errors

## Success Indicators
- Registration completing in < 5 seconds
- No timeout errors in logs
- Stable database connections