# Railway Redis Setup Guide for SignalR Backplane

## Overview
This guide explains how to configure Redis for SignalR horizontal scaling on Railway for both staging and production environments.

## Prerequisites
- Railway account with staging and production environments
- ZiraAI WebAPI deployed to Railway
- SignalR real-time notifications implemented

## Redis Configuration in Application

### CacheOptions Structure
The application uses the existing `CacheOptions` pattern for Redis configuration:

```json
{
  "CacheOptions": {
    "Host": "${REDIS_HOST}",
    "Port": "${REDIS_PORT}",
    "Password": "${REDIS_PASSWORD}",
    "Database": 0,
    "Ssl": true
  },
  "UseRedis": true
}
```

### Railway Environment Variables
Redis connection is configured via Railway environment variables using double underscore notation:

```bash
CacheOptions__Host=monorail.proxy.rlwy.net
CacheOptions__Port=38265
CacheOptions__Password=your-redis-password
CacheOptions__Ssl=true
UseRedis=true
```

## Setting Up Redis on Railway

### Step 1: Add Redis Service
1. Open your Railway project
2. Click "New Service" → "Database" → "Add Redis"
3. Railway will provision a Redis instance with:
   - Internal hostname (for inter-service communication)
   - Public hostname (for external access)
   - Automatically generated password

### Step 2: Configure Environment Variables

#### Option A: Using Railway Private Networking (Recommended)
If using Railway's private networking between services:

```bash
# Railway provides these automatically as service variables
CacheOptions__Host=${{Redis.RAILWAY_PRIVATE_DOMAIN}}
CacheOptions__Port=6379
CacheOptions__Password=${{Redis.REDIS_PASSWORD}}
CacheOptions__Ssl=false
UseRedis=true
```

#### Option B: Using Public Redis URL
For public access or if private networking isn't available:

```bash
# Extract from REDIS_URL provided by Railway
REDIS_URL=redis://:password@host:port

# Set manually:
CacheOptions__Host=monorail.proxy.rlwy.net
CacheOptions__Port=38265
CacheOptions__Password=your-redis-password
CacheOptions__Ssl=true
UseRedis=true
```

### Step 3: Configure SSL Certificate Validation
The application includes Railway SSL certificate validation bypass (already implemented in Startup.cs):

```csharp
// Railway SSL certificate fix
if (cacheConfig.Ssl)
{
    configOptions.CertificateValidation += (sender, certificate, chain, errors) => true;
}
```

⚠️ **Note**: This bypasses certificate validation. For production, consider proper certificate management.

## Environment-Specific Configuration

### Staging Environment
```bash
# Railway Staging Environment Variables
ASPNETCORE_ENVIRONMENT=Staging
CacheOptions__Host=staging-redis.railway.internal
CacheOptions__Port=6379
CacheOptions__Password=staging-redis-password
CacheOptions__Ssl=false
UseRedis=true
```

### Production Environment
```bash
# Railway Production Environment Variables
ASPNETCORE_ENVIRONMENT=Production
CacheOptions__Host=monorail.proxy.rlwy.net
CacheOptions__Port=38265
CacheOptions__Password=production-redis-password
CacheOptions__Ssl=true
UseRedis=true
```

## SignalR Redis Channel Configuration
The application uses Redis with the following settings:

```csharp
signalRBuilder.AddStackExchangeRedis(configOptions.ToString(), options =>
{
    options.Configuration.ChannelPrefix = RedisChannel.Literal("ZiraAI:SignalR:");
});
```

**Channel Prefix**: `ZiraAI:SignalR:` - All SignalR messages are prefixed with this to avoid conflicts with other Redis usage.

## Verification

### 1. Check Application Logs
Look for these console messages during startup:

**Success:**
```
🔴 Configuring Redis backplane for SignalR using existing CacheOptions
✅ SignalR Redis backplane configured - Host: monorail.proxy.rlwy.net, SSL: true
```

**Fallback to In-Memory:**
```
⚠️ UseRedis=true but CacheOptions not configured - falling back to in-memory
```

**In-Memory Mode:**
```
📦 Using in-memory SignalR (single instance only)
```

### 2. Test Redis Connection
From Railway CLI or service logs:

```bash
# Railway CLI
railway run redis-cli -h $REDIS_HOST -p $REDIS_PORT -a $REDIS_PASSWORD

# Inside container
redis-cli -h $CacheOptions__Host -p $CacheOptions__Port -a $CacheOptions__Password

# Test commands
PING
# Expected: PONG

# Check SignalR channels
PUBSUB CHANNELS ZiraAI:SignalR:*
```

### 3. Test SignalR Horizontal Scaling
1. Scale WebAPI to 2+ replicas in Railway
2. Connect client to SignalR hub
3. Trigger notification from different replica
4. Verify client receives notification

## Troubleshooting

### Issue: Connection Timeout
**Symptoms:** SignalR falls back to in-memory, logs show Redis connection errors

**Solutions:**
1. Check Redis service is running in Railway
2. Verify environment variables are correctly set
3. Check if SSL is required (Railway public endpoints require SSL)
4. Verify Redis password is correct

### Issue: SSL Certificate Errors
**Symptoms:** `System.Security.Authentication.AuthenticationException`

**Solutions:**
1. Ensure `CacheOptions__Ssl=true` for public endpoints
2. Verify SSL certificate validation callback is enabled
3. Check Railway Redis is accessible via public endpoint

### Issue: Messages Not Propagating Across Instances
**Symptoms:** Notifications only work on same instance

**Solutions:**
1. Verify Redis backplane is configured (check startup logs)
2. Check all instances are connected to same Redis instance
3. Verify channel prefix matches: `ZiraAI:SignalR:`
4. Check Redis memory usage (Railway may evict data if full)

### Issue: High Redis Memory Usage
**Symptoms:** Redis memory approaching limit

**Solutions:**
1. Monitor SignalR channel usage: `MEMORY USAGE key`
2. Configure Redis maxmemory policy in Railway
3. Review SignalR message size and frequency
4. Consider upgrading Redis instance size

## Cost Considerations

### Railway Redis Pricing
- **Shared Redis**: Included in Hobby plan ($5/month)
- **Dedicated Redis**: Usage-based pricing
- **Network Egress**: Charged separately

### Optimization Tips
1. Use private networking between services (no egress fees)
2. Monitor Redis memory usage
3. Set appropriate SignalR KeepAliveInterval (currently 15s)
4. Use Redis for SignalR only, not general caching (separate instances)

## Development vs Production

### Development (Local)
```json
{
  "UseRedis": false,  // In-memory SignalR
  "CacheOptions": {
    "Host": "localhost",
    "Port": "6379",
    "Ssl": false
  }
}
```

### Staging (Railway)
```bash
UseRedis=true
CacheOptions__Ssl=false  # If using private networking
```

### Production (Railway)
```bash
UseRedis=true
CacheOptions__Ssl=true  # Required for public endpoints
```

## Security Best Practices

1. **Never commit credentials** - Use Railway environment variables
2. **Use private networking** - Reduces attack surface
3. **Strong passwords** - Railway generates secure passwords by default
4. **Network policies** - Restrict Redis access to WebAPI service only
5. **Monitor access** - Use Railway logs to track Redis connections

## References
- [Railway Redis Documentation](https://docs.railway.app/databases/redis)
- [SignalR Redis Backplane](https://docs.microsoft.com/en-us/aspnet/core/signalr/redis-backplane)
- [StackExchange.Redis Configuration](https://stackexchange.github.io/StackExchange.Redis/Configuration)
