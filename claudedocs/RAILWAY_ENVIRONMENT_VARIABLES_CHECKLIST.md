# Railway Environment Variables Checklist

## Production Environment Variables

### 📋 Complete List for Both WebAPI and WorkerService

#### Database (PostgreSQL)
```bash
DATABASE_URL=postgresql://user:password@host:port/database
```

#### Redis (Cache & SignalR)
```bash
REDIS_HOST=redis.railway.internal
REDIS_PORT=6379
REDIS_PASSWORD=your-redis-password
# Note: CacheOptions__Ssl is set to true in appsettings.Production.json
```

#### RabbitMQ (Message Queue)
```bash
RABBITMQ_URL=amqp://user:password@host:port/vhost
```

#### WebAPI Configuration
```bash
ZIRAAI_WEBAPI_URL=https://api.ziraai.com
ZIRAAI_INTERNAL_SECRET=your-internal-secret-key
RAILWAY_PUBLIC_DOMAIN=https://api.ziraai.com
```

#### N8N Webhook
```bash
N8N_WEBHOOK_URL=https://n8n.ziraai.com/webhook/api/plant-analysis
```

#### File Storage (Optional - if using external providers)
```bash
# FreeImageHost
FREEIMAGEHOST_API_KEY=your-api-key

# ImgBB (if using)
IMGBB_API_KEY=your-api-key

# AWS S3 (if using)
S3_BUCKET_NAME=ziraai-production-images
AWS_REGION=us-east-1
AWS_ACCESS_KEY_ID=your-access-key
AWS_SECRET_ACCESS_KEY=your-secret-key
CLOUDFRONT_DOMAIN=cdn.ziraai.com
```

#### Hangfire (WorkerService only)
```bash
HANGFIRE_USERNAME=admin
HANGFIRE_PASSWORD=your-secure-password
```

#### Sponsorship Features
```bash
SPONSOR_REQUEST_SECRET=your-sponsor-request-secret
```

#### Environment
```bash
ASPNETCORE_ENVIRONMENT=Production
```

---

## SignalR-Specific Variables

### WebAPI Service
SignalR Redis backplane kullanıyor, bu yüzden aşağıdaki variables **ZORUNLU**:

```bash
# Redis Connection (SignalR için gerekli)
REDIS_HOST=redis.railway.internal
REDIS_PORT=6379
REDIS_PASSWORD=your-redis-password

# SignalR Configuration (appsettings.Production.json'da tanımlı)
# CacheOptions__Ssl=true (hardcoded)
# UseRedis=true (hardcoded)
```

### WorkerService
WorkerService SignalR notification göndermek için WebAPI'yi çağırıyor:

```bash
# WebAPI Internal Communication (Notification için gerekli)
ZIRAAI_WEBAPI_URL=https://api.ziraai.com
ZIRAAI_INTERNAL_SECRET=your-internal-secret-key
```

---

## Railway Service Configuration

### WebAPI Service
✅ **Gerekli Variables:**
- DATABASE_URL
- REDIS_HOST, REDIS_PORT, REDIS_PASSWORD
- RABBITMQ_URL
- ZIRAAI_WEBAPI_URL, ZIRAAI_INTERNAL_SECRET
- RAILWAY_PUBLIC_DOMAIN
- N8N_WEBHOOK_URL
- SPONSOR_REQUEST_SECRET
- FREEIMAGEHOST_API_KEY

### WorkerService
✅ **Gerekli Variables:**
- DATABASE_URL
- REDIS_HOST, REDIS_PORT, REDIS_PASSWORD (CacheOptions için)
- RABBITMQ_URL
- ZIRAAI_WEBAPI_URL, ZIRAAI_INTERNAL_SECRET (Internal notification için)
- RAILWAY_PUBLIC_DOMAIN
- N8N_WEBHOOK_URL
- FREEIMAGEHOST_API_KEY
- HANGFIRE_USERNAME, HANGFIRE_PASSWORD

---

## Verification Commands

### Check if Redis is working
```bash
# Railway CLI
railway run --service redis redis-cli PING
# Expected: PONG
```

### Check WebAPI Redis connection
```bash
# Check logs for this message
✅ SignalR Redis backplane configured - Host: redis.railway.internal, SSL: true
```

### Check WorkerService can reach WebAPI
```bash
# Check WorkerService logs for successful notification sends
[INF] Successfully sent notification to WebAPI
```

---

## Private Networking vs Public Endpoints

### Private Network (Recommended)
```bash
# Use Railway internal hostnames
REDIS_HOST=redis.railway.internal
DATABASE_HOST=postgres.railway.internal
# SSL: false for private network
```

### Public Endpoints
```bash
# Use public Railway URLs
REDIS_HOST=monorail.proxy.rlwy.net
REDIS_PORT=38265
# SSL: true for public endpoints (already set in appsettings.Production.json)
```

**Note:** Production appsettings.json artık `Ssl: true` olarak hard-coded, bu yüzden Railway public endpoints kullanıyorsanız hiçbir şey değiştirmeniz gerekmez.

---

## Railway Project Setup Steps

1. **Add Redis Service**
   - Go to Railway project
   - Add "Redis" database
   - Railway otomatik REDIS_HOST, REDIS_PORT, REDIS_PASSWORD oluşturacak

2. **Add PostgreSQL Service** (if not already added)
   - Add "PostgreSQL" database
   - Railway otomatik DATABASE_URL oluşturacak

3. **Add RabbitMQ Service** (if not already added)
   - Add "RabbitMQ" service from templates
   - Railway otomatik RABBITMQ_URL oluşturacak

4. **Configure Environment Variables**
   - WebAPI service → Add all WebAPI variables
   - WorkerService → Add all WorkerService variables

5. **Enable Private Networking** (Optional but recommended)
   - Railway Settings → Enable Private Networking
   - Use `.railway.internal` hostnames

---

## Testing After Deployment

### WebAPI
```bash
# 1. Check startup logs
railway logs --service webapi | grep "SignalR Redis"
# Should see: ✅ SignalR Redis backplane configured

# 2. Test SignalR connection
curl https://api.ziraai.com/hubs/plantanalysis/negotiate
# Should return connection info

# 3. Test notification endpoint
curl -X POST https://api.ziraai.com/api/internal/notification \
  -H "X-Internal-Secret: your-secret" \
  -H "Content-Type: application/json" \
  -d '{"userId":1,"analysisId":1,"message":"Test"}'
```

### WorkerService
```bash
# 1. Check startup logs
railway logs --service workerservice | grep "Redis"
# Should see Redis connection logs

# 2. Check if worker can send notifications
railway logs --service workerservice | grep "notification"
# Should see successful notification sends
```

---

## Common Issues

### Issue: Redis SSL Certificate Errors
**Solution:** `Ssl: true` zaten appsettings.Production.json'da tanımlı ve SSL certificate validation bypass yapılıyor.

### Issue: WorkerService can't reach WebAPI
**Solution:**
1. Check ZIRAAI_WEBAPI_URL is correct
2. Check ZIRAAI_INTERNAL_SECRET matches
3. Use private networking if available

### Issue: SignalR falls back to in-memory
**Solution:**
1. Check REDIS_HOST, REDIS_PORT, REDIS_PASSWORD are set
2. Check Redis service is running in Railway
3. Check startup logs for error messages

---

## Security Notes

- ✅ Never commit credentials to git
- ✅ Use Railway environment variables for all secrets
- ✅ Rotate secrets periodically
- ✅ Use strong passwords for Hangfire dashboard
- ✅ Use private networking when possible
- ✅ Monitor access logs regularly

## Updated: 2025-10-01
Railway environment variables ve SignalR Redis backplane için gerekli tüm konfigürasyonlar tamamlandı.
