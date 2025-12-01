# Railway Staging Deployment Guide

**Phase 1, Day 3-4 Implementation**
**Date**: 30 Kasım 2025
**Status**: Ready for Deployment

## Executive Summary

This guide provides comprehensive Railway staging deployment configuration for the multi-provider AI analysis worker system. The architecture supports horizontal scaling with independent worker instances for OpenAI, Gemini, and Anthropic providers.

---

## 📋 Architecture Overview

### Service Components

```
┌─────────────────────────────────────────────────────────────┐
│                    Railway Staging Environment               │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐   │
│  │  RabbitMQ    │   │    Redis     │   │  PostgreSQL  │   │
│  │  (Shared)    │   │  (Shared)    │   │   (Shared)   │   │
│  └──────┬───────┘   └──────┬───────┘   └──────┬───────┘   │
│         │                  │                   │            │
│  ┌──────▼──────────────────▼───────────────────▼────────┐  │
│  │           Analysis Worker Instances (3-15)            │  │
│  │                                                        │  │
│  │  ┌──────────┐  ┌──────────┐  ┌───────────┐          │  │
│  │  │ OpenAI   │  │  Gemini  │  │ Anthropic │  ...     │  │
│  │  │ Workers  │  │ Workers  │  │ Workers   │          │  │
│  │  │  (1-5)   │  │  (1-5)   │  │  (1-5)    │          │  │
│  │  └──────────┘  └──────────┘  └───────────┘          │  │
│  │                                                        │  │
│  │  - Auto-consume from all provider queues              │  │
│  │  - Dynamic provider selection via strategies          │  │
│  │  - Independent scaling per provider                   │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Queue Architecture

```
RabbitMQ Queues:
├─ openai-analysis-queue      → OpenAI GPT-4o-mini requests
├─ gemini-analysis-queue      → Google Gemini Flash 2.0 requests
├─ anthropic-analysis-queue   → Claude 3.5 Sonnet requests
├─ raw-analysis-queue         → Legacy/unprocessed (optional)
├─ analysis-results-queue     → Completed analysis results
└─ analysis-dlq               → Failed/dead-letter messages
```

**Worker Behavior**:
- Each worker instance consumes from **ALL** provider-specific queues
- Provider selection strategy determines which AI provider processes each message
- Horizontal scaling: Add more workers for higher throughput

---

## 🔧 Railway Service Configuration

### Service 1: Analysis Worker (Multi-Provider)

**Service Name**: `ziraai-analysis-worker-staging`

**GitHub Repository**: Connect your repository containing the worker code

**Root Directory**: `/` (repository root)

**Build Configuration**:
- **Builder**: Dockerfile
- **Dockerfile Path**: `workers/analysis-worker/Dockerfile`
- **Docker Context**: Repository root
- **Build Command**: Not required (handled by Dockerfile)

**Deployment Files**:
1. **Dockerfile** (`workers/analysis-worker/Dockerfile`) - Multi-stage build
2. **.dockerignore** (`workers/analysis-worker/.dockerignore`) - Build optimization
3. **railway.json** (`workers/analysis-worker/railway.json`) - Railway configuration

**Railway Configuration** (`workers/analysis-worker/railway.json`):
```json
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "DOCKERFILE",
    "dockerfilePath": "workers/analysis-worker/Dockerfile",
    "buildCommand": null
  },
  "deploy": {
    "startCommand": null,
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10,
    "healthcheckPath": null,
    "healthcheckTimeout": 300,
    "numReplicas": 1
  }
}
```

**Dockerfile** (`workers/analysis-worker/Dockerfile`):
```dockerfile
# ============================================
# Stage 1: Build
# ============================================
FROM node:18-alpine AS builder

WORKDIR /app

# Copy package files
COPY package*.json ./
COPY tsconfig.json ./

# Install ALL dependencies (including devDependencies for build)
RUN npm ci

# Copy source code
COPY src ./src

# Build TypeScript to JavaScript
RUN npm run build

# ============================================
# Stage 2: Production
# ============================================
FROM node:18-alpine

WORKDIR /app

# Install production dependencies only
COPY package*.json ./
RUN npm ci --omit=dev && npm cache clean --force

# Copy compiled JavaScript from builder
COPY --from=builder /app/dist ./dist

# Create non-root user for security
RUN addgroup -g 1001 -S nodejs && \
    adduser -S nodejs -u 1001 && \
    chown -R nodejs:nodejs /app

USER nodejs

# Expose port (if needed for health checks)
EXPOSE 3000

# Health check (optional - can be customized)
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD node -e "console.log('healthy')" || exit 1

# Start the worker
CMD ["node", "dist/index.js"]
```

**.dockerignore** (`workers/analysis-worker/.dockerignore`):
```
# Node.js & Dependencies
node_modules/
npm-debug.log*

# Build Artifacts
dist/
*.tsbuildinfo

# Development Files
.env
.env.*
!.env.example

# Git & Version Control
.git/
.gitignore

# Documentation
*.md
!README.md
claudedocs/

# Testing
test/
tests/
*.test.ts
*.test.js
coverage/

# IDE & Editor
.vscode/
.idea/
*.swp
.DS_Store

# CI/CD
.github/
.gitlab-ci.yml

# Logs & Temporary Files
logs/
*.log
tmp/

# Docker
Dockerfile*
docker-compose*.yml
.dockerignore
```

**Environment Variables** (Railway Web UI):

#### Core Configuration
```bash
# Worker Identification
WORKER_ID=worker-staging-1
NODE_ENV=staging
LOG_LEVEL=info
CONCURRENCY=60

# Health Checks
HEALTH_CHECK_INTERVAL=30000
```

#### Provider API Keys (REQUIRED - At least one)
```bash
OPENAI_API_KEY=sk-proj-...
GEMINI_API_KEY=...
ANTHROPIC_API_KEY=sk-ant-...
```

#### Provider Selection Strategy
```bash
# Strategy Options: FIXED | ROUND_ROBIN | COST_OPTIMIZED | QUALITY_FIRST | MESSAGE_BASED | WEIGHTED
PROVIDER_SELECTION_STRATEGY=ROUND_ROBIN

# For FIXED strategy (single provider only):
# PROVIDER_SELECTION_STRATEGY=FIXED
# PROVIDER_FIXED=gemini

# For WEIGHTED strategy (custom distribution):
# PROVIDER_SELECTION_STRATEGY=WEIGHTED
# PROVIDER_WEIGHTS=[{"provider":"gemini","weight":70},{"provider":"openai","weight":20},{"provider":"anthropic","weight":10}]
```

#### Provider Metadata (Optional - Dynamic Costs/Quality)
```bash
# Override default cost and quality scores
PROVIDER_METADATA={"gemini":{"costPerMillion":1.0,"qualityScore":7},"openai":{"costPerMillion":5.0,"qualityScore":8}}
```

#### RabbitMQ Configuration
```bash
RABBITMQ_URL=${{RabbitMQ.CLOUDAMQP_URL}}
RESULT_QUEUE=analysis-results-queue
DLQ_QUEUE=analysis-dlq
PREFETCH_COUNT=10
```

**Note**: Provider-specific queues (openai-analysis-queue, gemini-analysis-queue, anthropic-analysis-queue) are hardcoded in config.ts.

#### Redis Configuration
```bash
REDIS_URL=${{Redis.REDIS_URL}}
REDIS_KEY_PREFIX=ziraai:staging:ratelimit:
REDIS_TTL=120
```

#### Provider-Specific Settings
```bash
# OpenAI
PROVIDER_MODEL=gpt-4o-mini
RATE_LIMIT=350
TIMEOUT=60000

# Gemini (uses defaults in code)
# GEMINI_MODEL=gemini-2.0-flash-exp (default)

# Anthropic (uses defaults in code)
# ANTHROPIC_MODEL=claude-3-5-sonnet-20241022 (default)
```

---

## 🚀 Deployment Scenarios

### Scenario 1: Single Provider Testing (FIXED Strategy)

**Use Case**: Test one provider in isolation before enabling multi-provider.

**Configuration**:
```bash
PROVIDER_SELECTION_STRATEGY=FIXED
PROVIDER_FIXED=gemini
GEMINI_API_KEY=...
```

**Expected Behavior**:
- Worker consumes from all queues (openai-analysis-queue, gemini-analysis-queue, anthropic-analysis-queue)
- ALL messages processed by Gemini only
- Cost: ~$0.108/1K analyses (cheapest option)

**Deployment**:
```bash
# Railway CLI
railway up --service ziraai-analysis-worker-staging

# Or via Railway Web UI
# 1. Connect GitHub repo
# 2. Select workers/analysis-worker directory
# 3. Set environment variables
# 4. Deploy
```

---

### Scenario 2: Cost-Optimized Multi-Provider (COST_OPTIMIZED Strategy)

**Use Case**: Production-ready with automatic cost optimization.

**Configuration**:
```bash
PROVIDER_SELECTION_STRATEGY=COST_OPTIMIZED
OPENAI_API_KEY=sk-proj-...
GEMINI_API_KEY=...
ANTHROPIC_API_KEY=sk-ant-...
```

**Expected Behavior**:
- Worker tries Gemini first (cheapest: $0.108/1K)
- Fallback to OpenAI ($0.513/1K) if Gemini fails
- Fallback to Anthropic ($4.80/1K) if both fail
- Automatic circuit breaker and failover

**Cost Impact** (1M analyses/day):
```
Gemini (95%):    950K × $0.000108 = $102.60
OpenAI (4%):      40K × $0.000513 = $20.52
Anthropic (1%):   10K × $0.004800 = $48.00
────────────────────────────────────────────
Total Daily:                       $171.12
Monthly:                          ~$5,134
```

---

### Scenario 3: Quality-First (QUALITY_FIRST Strategy)

**Use Case**: Maximum accuracy for critical analyses, cost secondary.

**Configuration**:
```bash
PROVIDER_SELECTION_STRATEGY=QUALITY_FIRST
OPENAI_API_KEY=sk-proj-...
GEMINI_API_KEY=...
ANTHROPIC_API_KEY=sk-ant-...
```

**Expected Behavior**:
- Worker tries Anthropic first (quality: 10/10)
- Fallback to OpenAI (quality: 8/10) if Anthropic fails
- Fallback to Gemini (quality: 7/10) if both fail

**Cost Impact** (1M analyses/day):
```
Anthropic (95%):  950K × $0.004800 = $4,560
OpenAI (4%):       40K × $0.000513 = $20.52
Gemini (1%):       10K × $0.000108 = $1.08
────────────────────────────────────────────
Total Daily:                      $4,581.60
Monthly:                         ~$137,448
```

---

### Scenario 4: Weighted Distribution (WEIGHTED Strategy)

**Use Case**: Custom load balancing based on business requirements.

**Configuration**:
```bash
PROVIDER_SELECTION_STRATEGY=WEIGHTED
PROVIDER_WEIGHTS=[{"provider":"gemini","weight":70},{"provider":"openai","weight":20},{"provider":"anthropic","weight":10}]
OPENAI_API_KEY=sk-proj-...
GEMINI_API_KEY=...
ANTHROPIC_API_KEY=sk-ant-...
```

**Expected Behavior**:
- 70% of messages → Gemini
- 20% of messages → OpenAI
- 10% of messages → Anthropic

**Cost Impact** (1M analyses/day):
```
Gemini (70%):     700K × $0.000108 = $75.60
OpenAI (20%):     200K × $0.000513 = $102.60
Anthropic (10%):  100K × $0.004800 = $480.00
────────────────────────────────────────────
Total Daily:                        $658.20
Monthly:                          ~$19,746
```

---

### Scenario 5: Round-Robin (ROUND_ROBIN Strategy)

**Use Case**: Balanced testing across all providers.

**Configuration**:
```bash
PROVIDER_SELECTION_STRATEGY=ROUND_ROBIN
OPENAI_API_KEY=sk-proj-...
GEMINI_API_KEY=...
ANTHROPIC_API_KEY=sk-ant-...
```

**Expected Behavior**:
- Messages distributed evenly: OpenAI → Gemini → Anthropic → repeat

**Cost Impact** (1M analyses/day):
```
Each provider (33.33%):
Gemini:           333K × $0.000108 = $35.96
OpenAI:           333K × $0.000513 = $170.83
Anthropic:        333K × $0.004800 = $1,598.40
────────────────────────────────────────────
Total Daily:                      $1,805.19
Monthly:                         ~$54,156
```

---

## 📊 Horizontal Scaling Strategy

### Scaling Configuration

**Railway Autoscaling** (Recommended):
```json
{
  "replicas": {
    "min": 3,
    "max": 15
  },
  "resources": {
    "cpu": "1000m",
    "memory": "512Mi"
  },
  "autoscaling": {
    "enabled": true,
    "targetCPU": 70,
    "targetMemory": 80
  }
}
```

### Instance Distribution Examples

#### Light Load (3 instances - 3,600 analyses/day):
```
Worker 1: ROUND_ROBIN (OpenAI, Gemini, Anthropic)
Worker 2: ROUND_ROBIN (OpenAI, Gemini, Anthropic)
Worker 3: ROUND_ROBIN (OpenAI, Gemini, Anthropic)

Throughput: ~3 analyses/min × 3 = ~9 analyses/min
Daily: ~12,960 analyses
Cost: ~$23.40/day (with COST_OPTIMIZED)
```

#### Medium Load (8 instances - 691,200 analyses/day):
```
Worker 1-8: COST_OPTIMIZED (Gemini → OpenAI → Anthropic)

Throughput: ~60 analyses/min × 8 = ~480 analyses/min
Daily: ~691,200 analyses
Cost: ~$74.69/day (95% Gemini success rate)
```

#### Heavy Load (15 instances - 1,296,000 analyses/day):
```
Worker 1-15: COST_OPTIMIZED (Gemini → OpenAI → Anthropic)

Throughput: ~60 analyses/min × 15 = ~900 analyses/min
Daily: ~1,296,000 analyses
Cost: ~$140.05/day (95% Gemini success rate)
```

### Cost Breakdown by Strategy (15 instances, 1.3M/day)

| Strategy | Daily Cost | Monthly Cost | Notes |
|----------|-----------|--------------|-------|
| **COST_OPTIMIZED** | $140.05 | $4,202 | Recommended for production |
| **FIXED (Gemini)** | $140.05 | $4,202 | Single provider, lowest cost |
| **ROUND_ROBIN** | $2,341 | $70,230 | Even distribution (not cost-efficient) |
| **QUALITY_FIRST** | $5,943 | $178,290 | Anthropic-first (highest quality) |
| **WEIGHTED (70/20/10)** | $854 | $25,620 | Custom balance |

---

## 🔍 Monitoring & Health Checks

### Health Check Endpoint

Each worker exposes a health endpoint (if implemented):

```bash
GET http://worker-instance/health

Response:
{
  "status": "healthy",
  "uptime": 3600,
  "providers": ["openai", "gemini", "anthropic"],
  "rabbitmq": "connected",
  "redis": "connected",
  "queues": {
    "openai": { "depth": 12, "consuming": true },
    "gemini": { "depth": 5, "consuming": true },
    "anthropic": { "depth": 2, "consuming": true }
  },
  "rateLimit": {
    "openai": { "current": 45, "limit": 350, "allowed": true },
    "gemini": { "current": 120, "limit": 2000, "allowed": true },
    "anthropic": { "current": 8, "limit": 50, "allowed": true }
  }
}
```

### Railway Logs Monitoring

**View Worker Logs**:
```bash
railway logs --service ziraai-analysis-worker-staging --tail
```

**Key Log Patterns**:
```
✅ INFO: Worker started successfully and consuming from all provider queues
✅ INFO: Provider API keys validated: providers=["OpenAI","Gemini","Anthropic"]
✅ INFO: Started consuming from queue: queueName="openai-analysis-queue"
✅ INFO: Started consuming from queue: queueName="gemini-analysis-queue"
✅ INFO: Started consuming from queue: queueName="anthropic-analysis-queue"
✅ DEBUG: Cost-optimized provider selected: provider="gemini" cost=1.087
✅ DEBUG: Message acknowledged: analysisId="abc-123" queueName="gemini-analysis-queue"

❌ ERROR: Message processing failed: error="Rate limit exceeded" queueName="openai-analysis-queue"
❌ WARN: Message sent to DLQ: analysisId="xyz-789" attemptCount=3
```

### Metrics to Monitor

1. **Queue Depth** (RabbitMQ Management):
   - Target: < 100 messages per queue
   - Alert: > 500 messages (scaling needed)

2. **Message Processing Rate**:
   - Target: ~60 messages/min per worker
   - Alert: < 30 messages/min (performance issue)

3. **Error Rate**:
   - Target: < 5% (provider failures)
   - Alert: > 15% (investigate provider issues)

4. **DLQ Messages**:
   - Target: < 1% of total messages
   - Alert: > 50 messages/hour (systematic failures)

5. **Provider Distribution** (in logs):
   - Verify strategy is working (e.g., COST_OPTIMIZED uses Gemini 95%+)

---

## 🛠️ Deployment Steps

### Step 1: Prepare Railway Environment

1. **Create Railway Account**: https://railway.app
2. **Create New Project**: `ziraai-staging`
3. **Add Required Services**:
   - PostgreSQL (already exists from WebAPI)
   - Redis (already exists from WebAPI)
   - RabbitMQ (CloudAMQP plugin)

### Step 2: Configure RabbitMQ (CloudAMQP)

1. **Add CloudAMQP Plugin**:
   ```bash
   railway add cloudamqp
   ```

2. **Configure Queues** (via CloudAMQP Management UI):
   - Access: `${{RabbitMQ.CLOUDAMQP_URL}}` → Management UI
   - Create queues:
     - `openai-analysis-queue` (Durable: Yes, TTL: 24h)
     - `gemini-analysis-queue` (Durable: Yes, TTL: 24h)
     - `anthropic-analysis-queue` (Durable: Yes, TTL: 24h)
     - `analysis-results-queue` (Durable: Yes)
     - `analysis-dlq` (Durable: Yes)

### Step 3: Deploy Analysis Worker

1. **Create New Service**:
   ```bash
   railway service create ziraai-analysis-worker-staging
   ```

2. **Link GitHub Repository**:
   - Repository: `ziraai`
   - Root Directory: `workers/analysis-worker`
   - Build Command: `npm run build`
   - Start Command: `node dist/index.js`

3. **Set Environment Variables** (copy from section above)

4. **Deploy**:
   ```bash
   railway up
   ```

### Step 4: Verify Deployment

1. **Check Logs**:
   ```bash
   railway logs --tail
   ```

2. **Expected Output**:
   ```
   INFO: Starting multi-provider analysis worker
   INFO: Provider API keys validated: providers=["OpenAI","Gemini","Anthropic"]
   INFO: RabbitMQ connected successfully
   INFO: Started consuming from queue: queueName="openai-analysis-queue"
   INFO: Started consuming from queue: queueName="gemini-analysis-queue"
   INFO: Started consuming from queue: queueName="anthropic-analysis-queue"
   INFO: Worker started successfully and consuming from all provider queues
   ```

3. **Test Message Flow**:
   - Send test message to `gemini-analysis-queue`
   - Verify worker processes it
   - Check result in `analysis-results-queue`

### Step 5: Scale Horizontally

1. **Add More Instances** (Railway UI):
   - Settings → Replicas → Min: 3, Max: 15

2. **Or via CLI**:
   ```bash
   railway service scale --replicas 3
   ```

3. **Verify Load Distribution**:
   - Each instance should consume from all 3 queues
   - Check RabbitMQ consumer count (should be 3x number of instances)

---

## 🚨 Troubleshooting

### Issue 1: Worker Not Consuming Messages

**Symptoms**:
- Logs show "Worker started" but no "Message received" logs
- Queue depth increasing in RabbitMQ

**Diagnosis**:
```bash
railway logs | grep -i "consuming\|queue"
```

**Solutions**:
1. Verify RabbitMQ connection:
   ```bash
   # Check RABBITMQ_URL is correct
   railway variables
   ```

2. Check queue names match:
   ```bash
   # Queues are hardcoded: openai-analysis-queue, gemini-analysis-queue, anthropic-analysis-queue
   # Verify they exist in RabbitMQ Management UI
   ```

3. Verify at least one API key is configured:
   ```bash
   railway variables | grep -i "api_key"
   ```

---

### Issue 2: High Error Rate in Logs

**Symptoms**:
- Many "Message processing failed" errors
- DLQ filling up

**Diagnosis**:
```bash
railway logs | grep -i "error\|failed"
```

**Solutions**:
1. **Rate Limit Exceeded**:
   ```bash
   # Increase rate limits or add more workers
   RATE_LIMIT=500  # Up from 350

   # Or scale horizontally
   railway service scale --replicas 5
   ```

2. **Provider API Issues**:
   ```bash
   # Check provider status
   # OpenAI: https://status.openai.com
   # Gemini: https://status.cloud.google.com
   # Anthropic: https://status.anthropic.com

   # Switch to different provider temporarily
   PROVIDER_SELECTION_STRATEGY=FIXED
   PROVIDER_FIXED=gemini  # If OpenAI is down
   ```

3. **Invalid Messages**:
   ```bash
   # Check DLQ for message patterns
   # RabbitMQ Management UI → analysis-dlq → Get Messages
   ```

---

### Issue 3: Cost Exceeding Budget

**Symptoms**:
- Daily AI API costs higher than expected

**Diagnosis**:
```bash
railway logs | grep -i "provider selected"
```

**Solutions**:
1. **Switch to COST_OPTIMIZED**:
   ```bash
   PROVIDER_SELECTION_STRATEGY=COST_OPTIMIZED
   ```

2. **Use FIXED strategy with Gemini only**:
   ```bash
   PROVIDER_SELECTION_STRATEGY=FIXED
   PROVIDER_FIXED=gemini
   ```

3. **Adjust WEIGHTED distribution**:
   ```bash
   # More Gemini, less Anthropic
   PROVIDER_WEIGHTS=[{"provider":"gemini","weight":90},{"provider":"openai","weight":9},{"provider":"anthropic","weight":1}]
   ```

---

### Issue 4: Provider Not Being Used

**Symptoms**:
- Logs show only one provider being selected (e.g., always Gemini)
- Other providers not appearing in logs

**Diagnosis**:
```bash
railway logs | grep -i "provider selected"
```

**Solutions**:
1. **Check API Keys**:
   ```bash
   railway variables | grep -i "api_key"
   # Ensure all desired providers have keys set
   ```

2. **Verify Strategy**:
   ```bash
   # If using FIXED, only one provider will be used
   railway variables | grep PROVIDER_SELECTION_STRATEGY

   # Change to ROUND_ROBIN to test all providers
   PROVIDER_SELECTION_STRATEGY=ROUND_ROBIN
   ```

3. **Check initializeProviders() Logic**:
   - Provider only initialized if API key exists
   - Verify logs: "Provider API keys validated"

---

---

## 🚀 GitHub-Based Deployment to Railway

### Prerequisites

1. **GitHub Repository**: Code pushed to GitHub (public or private)
2. **Railway Account**: Connected to GitHub
3. **Railway CLI** (optional): `npm install -g @railway/cli`

### Deployment Steps

#### Step 1: Prepare Repository

Ensure these files exist in your repository:

```
your-repo/
├── workers/
│   └── analysis-worker/
│       ├── Dockerfile          ✅ Created
│       ├── .dockerignore       ✅ Created
│       ├── railway.json        ✅ Created
│       ├── package.json
│       ├── tsconfig.json
│       └── src/
│           └── index.ts
```

**Verify Files**:
```bash
cd workers/analysis-worker
ls -la Dockerfile .dockerignore railway.json
# All 3 files should exist
```

#### Step 2: Push to GitHub

```bash
# Add deployment files
git add workers/analysis-worker/Dockerfile
git add workers/analysis-worker/.dockerignore
git add workers/analysis-worker/railway.json

# Commit
git commit -m "feat: Add Docker deployment configuration for analysis-worker

- Multi-stage Dockerfile for TypeScript compilation
- .dockerignore for build optimization
- railway.json for Railway deployment config"

# Push to GitHub
git push origin feature/production-storage-service
# Or your current branch
```

#### Step 3: Create Railway Project

**Option A: Railway Dashboard (Recommended)**

1. Go to [Railway Dashboard](https://railway.app/dashboard)
2. Click **"New Project"**
3. Select **"Deploy from GitHub repo"**
4. Authorize Railway to access your repository
5. Select your repository: `ziraai`
6. Railway will detect the `railway.json` configuration

**Option B: Railway CLI**

```bash
# Login to Railway
railway login

# Link to existing project or create new
railway init

# Deploy
railway up
```

#### Step 4: Configure Build Settings

In Railway Dashboard → Service Settings:

**Build Settings**:
- **Builder**: Dockerfile
- **Dockerfile Path**: `workers/analysis-worker/Dockerfile`
- **Docker Build Context**: `/` (repository root)

**Deploy Settings**:
- **Start Command**: (leave empty - handled by Dockerfile CMD)
- **Restart Policy**: On Failure
- **Max Retries**: 10

#### Step 5: Configure Environment Variables

Go to Railway Dashboard → Variables tab and add:

**Required Variables**:
```bash
# Worker Configuration
WORKER_ID=worker-staging-1
NODE_ENV=staging

# RabbitMQ (CloudAMQP)
RABBITMQ_URL=amqps://username:password@host.cloudamqp.com/vhost

# Redis
REDIS_URL=redis://default:password@redis.railway.internal:6379

# AI Provider API Keys (at least one required)
OPENAI_API_KEY=sk-proj-...
GEMINI_API_KEY=...
ANTHROPIC_API_KEY=sk-ant-...

# Provider Selection Strategy
PROVIDER_SELECTION_STRATEGY=COST_OPTIMIZED

# Queue Configuration
RESULT_QUEUE=analysis-results-queue
DLQ_QUEUE=analysis-dlq
PREFETCH_COUNT=10

# Rate Limiting
RATE_LIMIT=350

# Logging
LOG_LEVEL=info
```

**Copy from Existing Services** (if available):
```bash
# RabbitMQ, Redis URLs can be copied from WebAPI service
# Just update WORKER_ID to be unique
```

#### Step 6: Deploy

**Automatic Deployment** (when connected to GitHub):
- Every push to the connected branch triggers automatic deployment
- Railway builds Docker image using your Dockerfile
- Deploys new version with zero downtime

**Manual Deployment** (Railway CLI):
```bash
railway up
```

**Monitor Deployment**:
```bash
# View build logs
railway logs --build

# View runtime logs
railway logs
```

#### Step 7: Verify Deployment

**Check Service Health**:
```bash
# Railway Dashboard → Deployments → Latest Deployment
# Should show "Active" status

# CLI verification
railway logs | grep -i "worker started successfully"
```

**Expected Log Output**:
```
{"level":"info","workerId":"worker-staging-1","availableProviders":["openai","gemini","anthropic"],"msg":"Starting multi-provider analysis worker"}
{"level":"info","queueName":"openai-analysis-queue","msg":"Started consuming from queue"}
{"level":"info","queueName":"gemini-analysis-queue","msg":"Started consuming from queue"}
{"level":"info","queueName":"anthropic-analysis-queue","msg":"Started consuming from queue"}
{"level":"info","queues":["openai-analysis-queue","gemini-analysis-queue","anthropic-analysis-queue"],"providerCount":3,"msg":"Worker started successfully"}
```

### Deployment Validation Checklist

- [ ] GitHub repository connected to Railway
- [ ] Dockerfile, .dockerignore, railway.json exist in repo
- [ ] Railway project created and linked to repo
- [ ] Build settings configured (Dockerfile path)
- [ ] Environment variables configured (all required vars)
- [ ] Deployment succeeded (check Railway dashboard)
- [ ] Service is Active (not crashed/stopped)
- [ ] Logs show "Worker started successfully"
- [ ] All provider queues being consumed
- [ ] RabbitMQ connection established
- [ ] Redis connection established

### Troubleshooting Deployment

#### Build Failures

**Error: Cannot find Dockerfile**
```bash
# Solution: Verify Dockerfile path in Railway settings
# Should be: workers/analysis-worker/Dockerfile
# Docker context should be: / (root)
```

**Error: npm install fails**
```bash
# Solution: Check package.json and package-lock.json are committed
git add workers/analysis-worker/package*.json
git commit -m "fix: Add package files for Docker build"
git push
```

**Error: TypeScript compilation fails**
```bash
# Solution: Verify tsconfig.json exists and is valid
# Check build logs for specific TypeScript errors
railway logs --build
```

#### Runtime Failures

**Error: Worker crashes immediately**
```bash
# Check environment variables
railway variables

# Check logs for missing config
railway logs | grep -i "error\|missing"
```

**Error: Cannot connect to RabbitMQ**
```bash
# Verify RABBITMQ_URL format
# Should be: amqps://user:pass@host/vhost
railway logs | grep -i "rabbitmq"
```

**Error: No provider API keys**
```bash
# Verify at least one API key is set
railway variables | grep -i "api_key"

# Set missing keys
railway variables set OPENAI_API_KEY=sk-proj-...
```

### Scaling the Deployment

**Horizontal Scaling**:
```bash
# Railway Dashboard → Service → Settings → Scaling
# Or via CLI:
railway service scale --replicas 3
```

**Update Environment Variables**:
```bash
# For multiple replicas, ensure unique WORKER_ID per instance
# Railway automatically handles this with $RAILWAY_REPLICA_ID

# Update WORKER_ID to use replica ID
WORKER_ID=worker-staging-$RAILWAY_REPLICA_ID
```

### Continuous Deployment

**Automatic Deployment on Git Push**:
1. Make code changes locally
2. Commit and push to GitHub
3. Railway automatically detects changes
4. Builds new Docker image
5. Deploys with zero downtime

**Deployment Branches**:
- **Staging**: Deploy from `develop` or `staging` branch
- **Production**: Deploy from `main` or `master` branch

**Configure in Railway**:
- Railway Dashboard → Service → Settings → Source
- Set **Branch**: `develop` (for staging) or `main` (for production)

### Rollback Strategy

**Rollback to Previous Deployment**:
```bash
# Railway Dashboard → Deployments → Select previous deployment → Redeploy
# Or via CLI:
railway rollback
```

**Emergency Stop**:
```bash
# Stop the service temporarily
railway service stop

# Restart after fixing issues
railway service start
```

---

## 📈 Performance Benchmarks

### Expected Throughput (per worker instance)

| Concurrency | Throughput | Daily Capacity | Notes |
|-------------|-----------|----------------|-------|
| 10 | ~10 analyses/min | ~14,400/day | Conservative |
| 30 | ~30 analyses/min | ~43,200/day | Moderate |
| 60 | ~60 analyses/min | ~86,400/day | **Recommended** |
| 100 | ~80 analyses/min | ~115,200/day | Diminishing returns |

**Formula**:
```
Daily Capacity = Throughput × 60 min × 24 hours
Cost per Analysis = (Provider Cost per 1M tokens × Avg Tokens) / 1M
```

### Scaling Recommendations

| Target Daily Volume | Workers Needed | Strategy | Estimated Cost/Day |
|---------------------|----------------|----------|-------------------|
| 10,000 | 1 | COST_OPTIMIZED | $1.08 |
| 100,000 | 2 | COST_OPTIMIZED | $10.80 |
| 500,000 | 6 | COST_OPTIMIZED | $54.00 |
| 1,000,000 | 12 | COST_OPTIMIZED | $108.00 |

**Note**: Costs assume 95% Gemini success rate with COST_OPTIMIZED strategy.

---

## 🎯 Success Criteria

### Deployment Validation Checklist

- [ ] RabbitMQ connection established
- [ ] Redis connection established
- [ ] At least one provider API key configured
- [ ] All 3 provider queues created in RabbitMQ
- [ ] Worker consuming from all provider queues
- [ ] Test message processed successfully
- [ ] Result published to analysis-results-queue
- [ ] Health check passing (if implemented)
- [ ] Logs showing correct provider selection strategy
- [ ] No errors in DLQ after 100 test messages

### Production Readiness

- [ ] COST_OPTIMIZED strategy enabled (or justified alternative)
- [ ] Horizontal scaling configured (min 3, max 15)
- [ ] Monitoring alerts set up (queue depth, error rate)
- [ ] Cost tracking enabled (provider distribution logs)
- [ ] Failover tested (simulate provider outage)
- [ ] Load testing completed (target throughput achieved)
- [ ] Documentation reviewed by team

---

## 🔗 Related Documentation

- [Phase 1 Day 1: TypeScript Worker Implementation](./PHASE1_DAY1_TYPESCRIPT_WORKER_IMPLEMENTATION.md)
- [Phase 1 Day 2: Multi-Provider Implementation](./PHASE1_DAY2_MULTI_PROVIDER_IMPLEMENTATION.md)
- [Provider Selection Strategies](./PROVIDER_SELECTION_STRATEGIES.md)
- [Dynamic Provider Metadata](./DYNAMIC_PROVIDER_METADATA.md)
- [Platform Modernization README](./README.md)

---

## 📝 Next Steps

After successful Railway Staging deployment:

1. **Phase 1, Day 5-7**: WebAPI Modifications
   - Update PlantAnalysisAsyncService to route to provider-specific queues
   - Add provider selection logic in DTOs
   - Implement feature flags for gradual rollout

2. **Phase 1, Day 8-10**: Production Validation
   - Load testing with 10K analyses
   - Cost validation against projections
   - Performance benchmarking

3. **Phase 2**: Dispatcher Implementation (Week 3-4)
   - Intelligent routing based on analysis type
   - Dynamic provider selection optimization
   - Advanced circuit breaker patterns

---

**Last Updated**: 30 Kasım 2025
**Status**: Ready for Railway Staging Deployment
**Next Action**: Deploy to Railway and validate with test messages
