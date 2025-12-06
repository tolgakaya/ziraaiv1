# Railway Staging Environment Implementation Updates

**Tarih:** 30 Kasım 2025
**Konu:** Production Readiness Implementation Plan - Railway Staging Güncellemesi

## Kritik Değişiklik

Tüm geliştirme, test ve implementasyon süreçleri **Railway Staging ortamında** yapılacaktır.

## Ortam Stratejisi

### Railway Staging (ziraai-staging)
- **Kullanım:** Tüm yeni özellik geliştirme ve test
- **Services:** WebAPI, PlantAnalysisWorker, TypeScript Workers, Dispatcher, Admin Panel
- **Infrastructure:**
  - PostgreSQL (Railway Postgres)
  - Redis (Railway Redis)
  - RabbitMQ (CloudAMQP via Railway)
  - Cloudflare R2 bucket: `ziraai-messages-staging`
- **Deployment:** Railway CLI + Git push
- **Environment Variables:** Railway dashboard
- **Logs:** Railway logs (real-time)

### Production (ziraai-production)
- **Kullanım:** Sadece Staging'de onaylanmış değişiklikler
- **Deployment:** Manuel approval sonrası Railway deploy

### Local Development
- **Kullanım:** Minimal - sadece IDE-based kod yazımı
- **Not:** Docker Compose kullanmıyoruz

## Temel Değişiklikler

### 1. Docker Compose → Railway Deployment

**ESKİ:**
```bash
docker-compose -f docker-compose.local.yml up --build openai-worker
```

**YENİ:**
```bash
cd workers/analysis-worker
railway login
railway link # ziraai-staging projesini seç
railway up   # Deploy
```

### 2. Localhost → Railway Staging URL

**ESKİ:**
```
http://localhost:5001/api/v1/plant-analyses
http://localhost:15672  # RabbitMQ Management
```

**YENİ:**
```
https://ziraai-api-staging.up.railway.app/api/v1/plant-analyses
https://cloudamqp-dashboard-url  # Railway RabbitMQ Management
```

### 3. Environment Variables

**ESKİ:** `appsettings.Development.json` içinde hardcoded
```json
{
  "RabbitMQ": {
    "ConnectionString": "amqp://guest:guest@localhost:5672/"
  }
}
```

**YENİ:** Railway environment variables
```bash
# Railway dashboard üzerinden:
RABBITMQ_URL=${{RABBITMQ_URL}}  # Otomatik inject
REDIS_URL=${{REDIS_URL}}
DATABASE_URL=${{DATABASE_URL}}
FEATURE_USE_NEW_WORKER_SYSTEM=true
```

### 4. appsettings Configuration

**ESKİ:** Development.json
```json
{
  "Features": {
    "UseNewWorkerSystem": true
  }
}
```

**YENİ:** Staging.json (environment variable'dan okuma)
```json
{
  "Features": {
    "UseNewWorkerSystem": "${FEATURE_USE_NEW_WORKER_SYSTEM:false}",
    "TrafficPercentage": "${FEATURE_TRAFFIC_PERCENTAGE:0}"
  }
}
```

### 5. Test ve Deployment Workflow

**ESKİ Workflow:**
```
Code → Local Docker Compose → Test → Commit → Railway Deploy
```

**YENİ Workflow:**
```
Code → Git Commit → Railway Staging Auto-Deploy → Test → Approve → Production Deploy
```

## Phase-by-Phase Değişiklikler

### Phase 1: Temel Altyapı

**Deployment:**
- Railway Staging'de 5x OpenAI worker container
- Railway CLI ile service creation
- Environment variables Railway dashboard'dan yönetim

**Test:**
- Railway Staging API endpoint'e Postman request
- Railway logs ile real-time monitoring
- CloudAMQP dashboard ile queue monitoring

### Phase 2: Multi-Provider

**Deployment:**
- Railway Staging'de Dispatcher + 15 worker (5x3 provider)
- Her worker ayrı Railway service
- Parallel deployment with Railway CLI

**Test:**
- Railway Staging'de load distribution test
- Provider failover test
- Circuit breaker test

### Phase 3: Admin Panel

**Deployment:**
- Next.js admin panel Railway'de ayrı service
- Railway public domain ile erişim
- WebAPI ile same network communication

**Features:**
- Railway metrics API integration
- Railway CLI ile scale control
- Real-time Railway logs monitoring

### Phase 4: Production Hardening

**Load Testing:**
- k6 ile Railway Staging API'ye test
- Target: 694 req/min sustained
- Railway metrics ile performance monitoring

**Gradual Rollout:**
- Railway Staging'de canary test (10% → 50% → 100%)
- Feature flags via Railway environment variables
- Railway Production'a manuel deployment

## Railway Service Yapısı

```
Railway Project: ziraai-staging
│
├── Services (18 total):
│   ├── webapi (existing)
│   ├── plant-analysis-worker (existing)
│   ├── dispatcher (new)
│   ├── openai-worker-001 ... 005 (new, 5 services)
│   ├── gemini-worker-001 ... 005 (new, 5 services)
│   ├── anthropic-worker-001 ... 005 (new, 5 services)
│   └── admin-panel (new)
│
├── Shared Resources:
│   ├── PostgreSQL (Railway Postgres)
│   ├── Redis (Railway Redis)
│   └── RabbitMQ (CloudAMQP plugin)
│
└── Domains:
    ├── webapi: ziraai-api-staging.up.railway.app
    └── admin-panel: ziraai-admin-staging.up.railway.app
```

## Railway CLI Komutları

### Service Creation
```bash
railway service create \
  --name openai-worker-001 \
  --source ./workers/analysis-worker

railway variables --set PROVIDER=openai
railway variables --set CONCURRENCY=60
railway variables --set QUEUE_NAME=openai-analysis-queue
```

### Deployment
```bash
railway up --service openai-worker-001
railway up --service dispatcher
railway up --service admin-panel
```

### Monitoring
```bash
railway logs --service openai-worker-001
railway status --service openai-worker-001
railway service list
```

### Scaling
```bash
# Manual scale via Railway dashboard
# Or via API script using Railway API
```

## Environment Variables (Railway Dashboard)

### Shared (All Services)
```
RABBITMQ_URL=${{RABBITMQ_URL}}
REDIS_URL=${{REDIS_URL}}
DATABASE_URL=${{DATABASE_URL}}
NODE_ENV=staging
CLOUDFLARE_R2_BUCKET=ziraai-messages-staging
```

### WebAPI Specific
```
FEATURE_USE_NEW_WORKER_SYSTEM=true
FEATURE_TRAFFIC_PERCENTAGE=100
JWT_SECRET_KEY=<staging-secret>
OPENAI_API_KEY=<staging-openai-key>
```

### Worker Specific (per worker)
```
PROVIDER=openai|gemini|anthropic
CONCURRENCY=60
RATE_LIMIT=350
QUEUE_NAME=openai-analysis-queue
RESULT_QUEUE=analysis-results
DLQ_QUEUE=analysis-dlq
OPENAI_API_KEY=<key>
```

## Test Workflow

### 1. Code Change
```bash
git add .
git commit -m "feat: add circuit breaker"
git push origin feature/circuit-breaker
```

### 2. Railway Auto-Deploy
Railway GitHub integration automatically deploys to staging

### 3. Verify Deployment
```bash
railway logs --tail 100
railway status
```

### 4. Test API
```bash
curl -X POST https://ziraai-api-staging.up.railway.app/api/v1/plant-analyses/analyze-async \
  -H "Authorization: Bearer $STAGING_JWT" \
  -H "Content-Type: application/json" \
  -d '{...}'
```

### 5. Monitor
- Railway dashboard: Metrics, logs, resource usage
- CloudAMQP dashboard: Queue depths, message rates
- Admin panel: Real-time system status

## Rollback Procedure

### Railway Rollback
```bash
# Via Railway dashboard:
# Services → Select service → Deployments → Rollback to previous

# Via CLI:
railway rollback --service openai-worker-001
```

### Feature Flag Rollback
```bash
railway variables --set FEATURE_USE_NEW_WORKER_SYSTEM=false
railway variables --set FEATURE_TRAFFIC_PERCENTAGE=0
```

## Cost Tracking

### Railway Staging Costs
- WebAPI: $5/month
- PlantAnalysisWorker: $5/month
- Dispatcher: $5/month
- Workers (15x): $10/month each = $150/month
- Admin Panel: $5/month
- **Total Infrastructure:** ~$170/month

### AI API Costs (Staging Testing)
- Limited volume testing: ~$100/month
- Full load testing (1 day): ~$500

## Monitoring ve Alerting

### Railway Native
- Service health checks
- CPU/Memory metrics
- Log aggregation
- Crash detection & auto-restart

### Custom (Admin Panel)
- Real-time throughput
- Provider health
- Queue depths
- Cost tracking

## Success Criteria Updates

### Phase 1 Completion (Railway Staging)
- [ ] 5 OpenAI workers deployed on Railway ✅
- [ ] End-to-end flow working via Railway Staging ✅
- [ ] Railway logs showing successful processing ✅
- [ ] CloudAMQP queues functioning ✅
- [ ] < 90s response time via Railway network ✅

### Phase 2 Completion (Railway Staging)
- [ ] Dispatcher + 15 workers deployed ✅
- [ ] Multi-provider distribution working ✅
- [ ] Circuit breaker tested on Railway ✅
- [ ] Failover working ✅

### Phase 3 Completion (Railway Staging)
- [ ] Admin panel deployed on Railway ✅
- [ ] Railway metrics integration working ✅
- [ ] Scale control via Railway CLI ✅

### Phase 4 Completion
- [ ] Load test against Railway Staging: 694 req/min ✅
- [ ] Canary deployment Railway Staging → Production ✅
- [ ] Production monitoring active ✅

## Sonuç

Tüm geliştirme süreci Railway Staging ortamında gerçekleşecek:
- **No Docker Compose** kullanılmayacak
- **No localhost testing** - tüm testler Railway Staging'de
- **Railway CLI** primary deployment tool
- **Environment variables** Railway dashboard üzerinden
- **Real production-like environment** early testing için
- **Seamless Staging → Production** pipeline

Bu yaklaşım sayesinde:
1. Production ortamına %100 benzer test environment
2. Deployment pipeline early testing
3. Infrastructure issues early detection
4. Zero surprises during production deployment
