# RabbitMQ Queue Deletion & Redeployment Guide

**Date**: 2025-12-07
**Commit**: 2909a02d - Standardize RabbitMQ queue TTL configuration across all services
**Branch**: staging → master → production
**Objective**: Delete all queues and redeploy services with standardized configuration

---

## ⚠️ CRITICAL: Pre-Deployment Requirements

### 1. Code Changes Deployed
- ✅ Commit 2909a02d pushed to staging
- ⏳ Merge staging → master (after staging verification)
- ⏳ Deploy to production (after master merge)

### 2. Backup Plan
- **No data loss risk**: Deleting queues only affects in-flight messages
- **Recovery**: Services will recreate queues automatically on startup
- **Timing**: Perform during low-traffic period to minimize impact

### 3. Downtime Window
- **Estimated**: 5-10 minutes per environment
- **Impact**: Analysis requests will be queued by dispatcher during worker restart
- **Mitigation**: Deploy during off-peak hours

---

## 🗑️ Queue Deletion Commands

### Method 1: RabbitMQ Management UI (RECOMMENDED)

#### Staging (Railway or your staging RabbitMQ):
1. Open RabbitMQ Management UI: `http://[STAGING_RABBITMQ_HOST]:15672`
2. Login with admin credentials
3. Go to **Queues** tab
4. For each queue, click queue name → **Delete** button at bottom
5. Confirm deletion

#### Production (Railway or your production RabbitMQ):
1. Open RabbitMQ Management UI: `http://[PRODUCTION_RABBITMQ_HOST]:15672`
2. Login with admin credentials
3. Go to **Queues** tab
4. For each queue, click queue name → **Delete** button at bottom
5. Confirm deletion

### Method 2: rabbitmqadmin CLI (ALTERNATIVE)

#### Staging:
```bash
# Set environment variables for staging RabbitMQ
export RABBITMQ_HOST="[STAGING_RABBITMQ_HOST]"
export RABBITMQ_USER="[ADMIN_USERNAME]"
export RABBITMQ_PASS="[ADMIN_PASSWORD]"

# Delete all queues
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=raw-analysis-queue
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=openai-analysis-queue
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=gemini-analysis-queue
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=anthropic-analysis-queue
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=analysis-dlq
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=plant-analysis-results
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=plant-analysis-multi-image-results
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=dealer-invitation-requests
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=farmer-code-distribution-requests
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=farmer-subscription-assignment-requests

echo "✅ All queues deleted in Staging"
```

#### Production (AFTER staging verification):
```bash
# Set environment variables for production RabbitMQ
export RABBITMQ_HOST="[PRODUCTION_RABBITMQ_HOST]"
export RABBITMQ_USER="[ADMIN_USERNAME]"
export RABBITMQ_PASS="[ADMIN_PASSWORD]"

# Delete all queues (same commands as staging)
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=raw-analysis-queue
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=openai-analysis-queue
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=gemini-analysis-queue
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=anthropic-analysis-queue
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=analysis-dlq
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=plant-analysis-results
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=plant-analysis-multi-image-results
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=dealer-invitation-requests
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=farmer-code-distribution-requests
rabbitmqadmin -H $RABBITMQ_HOST -u $RABBITMQ_USER -p $RABBITMQ_PASS delete queue name=farmer-subscription-assignment-requests

echo "✅ All queues deleted in Production"
```

### Method 3: RabbitMQ HTTP API (ALTERNATIVE)

```bash
# Staging - Delete all queues via HTTP API
RABBITMQ_HOST="[STAGING_RABBITMQ_HOST]:15672"
RABBITMQ_USER="[ADMIN_USERNAME]"
RABBITMQ_PASS="[ADMIN_PASSWORD]"

curl -u $RABBITMQ_USER:$RABBITMQ_PASS -X DELETE http://$RABBITMQ_HOST/api/queues/%2F/raw-analysis-queue
curl -u $RABBITMQ_USER:$RABBITMQ_PASS -X DELETE http://$RABBITMQ_HOST/api/queues/%2F/openai-analysis-queue
curl -u $RABBITMQ_USER:$RABBITMQ_PASS -X DELETE http://$RABBITMQ_HOST/api/queues/%2F/gemini-analysis-queue
curl -u $RABBITMQ_USER:$RABBITMQ_PASS -X DELETE http://$RABBITMQ_HOST/api/queues/%2F/anthropic-analysis-queue
curl -u $RABBITMQ_USER:$RABBITMQ_PASS -X DELETE http://$RABBITMQ_HOST/api/queues/%2F/analysis-dlq
curl -u $RABBITMQ_USER:$RABBITMQ_PASS -X DELETE http://$RABBITMQ_HOST/api/queues/%2F/plant-analysis-results
curl -u $RABBITMQ_USER:$RABBITMQ_PASS -X DELETE http://$RABBITMQ_HOST/api/queues/%2F/plant-analysis-multi-image-results
curl -u $RABBITMQ_USER:$RABBITMQ_PASS -X DELETE http://$RABBITMQ_HOST/api/queues/%2F/dealer-invitation-requests
curl -u $RABBITMQ_USER:$RABBITMQ_PASS -X DELETE http://$RABBITMQ_HOST/api/queues/%2F/farmer-code-distribution-requests
curl -u $RABBITMQ_USER:$RABBITMQ_PASS -X DELETE http://$RABBITMQ_HOST/api/queues/%2F/farmer-subscription-assignment-requests
```

---

## 🚀 Deployment Steps

### Phase 1: Staging Environment

#### Step 1.1: Delete All Queues (Staging)
```bash
# Use Method 1 (UI) or Method 2 (CLI) above
# Verify deletion in RabbitMQ Management UI - Queues tab should be empty
```

#### Step 1.2: Redeploy Services (Staging)

**Order of Deployment** (recommended):

1. **TypeScript Dispatcher** (creates analysis queues)
   ```bash
   # If using Railway:
   # - Go to Railway dashboard
   # - Select Dispatcher service
   # - Click "Redeploy" or push to trigger deployment

   # If using Docker:
   cd workers/dispatcher
   docker build -t ziraai-dispatcher:latest .
   docker restart ziraai-dispatcher
   ```

2. **TypeScript Analysis Worker** (creates result queues)
   ```bash
   # If using Railway:
   # - Go to Railway dashboard
   # - Select Analysis Worker service
   # - Click "Redeploy"

   # If using Docker:
   cd workers/analysis-worker
   docker build -t ziraai-analysis-worker:latest .
   docker restart ziraai-analysis-worker
   ```

3. **C# PlantAnalysisWorkerService** (consumes result queues, creates admin queues)
   ```bash
   # If using Railway:
   # - Code changes already deployed (commit 2909a02d)
   # - Click "Redeploy" on Worker Service

   # If using Docker:
   docker build -t ziraai-worker-service:latest .
   docker restart ziraai-worker-service
   ```

4. **C# WebAPI** (publishes to all queues)
   ```bash
   # If using Railway:
   # - Code changes already deployed (commit 2909a02d)
   # - Click "Redeploy" on WebAPI service

   # If using Docker:
   docker build -t ziraai-webapi:latest .
   docker restart ziraai-webapi
   ```

#### Step 1.3: Verify Staging Deployment

**Check RabbitMQ Management UI:**
```
Expected Queues (10 total):

Analysis Queues WITH TTL (7 queues):
✅ raw-analysis-queue - arguments: { "x-message-ttl": 86400000 }
✅ openai-analysis-queue - arguments: { "x-message-ttl": 86400000 }
✅ gemini-analysis-queue - arguments: { "x-message-ttl": 86400000 }
✅ anthropic-analysis-queue - arguments: { "x-message-ttl": 86400000 }
✅ analysis-dlq - arguments: { "x-message-ttl": 86400000 }
✅ plant-analysis-results - arguments: { "x-message-ttl": 86400000 }
✅ plant-analysis-multi-image-results - arguments: { "x-message-ttl": 86400000 }

Admin Queues WITHOUT TTL (3 queues):
✅ dealer-invitation-requests - arguments: {}
✅ farmer-code-distribution-requests - arguments: {}
✅ farmer-subscription-assignment-requests - arguments: {}
```

**Check Service Logs:**
```bash
# C# Worker Service - Should see successful initialization
[RABBITMQ_INIT_SUCCESS] RabbitMQ initialized successfully - QueueName: plant-analysis-results
[RABBITMQ_MULTI_IMAGE_INIT_SUCCESS] Multi-Image RabbitMQ initialized successfully - QueueName: plant-analysis-multi-image-results

# TypeScript Dispatcher
[Dispatcher 1] Connected to RabbitMQ
[Dispatcher 1] Consuming from raw-analysis-queue

# TypeScript Analysis Worker
[RabbitMQService] Successfully connected to RabbitMQ
[RabbitMQService] Result queue asserted: plant-analysis-results
[RabbitMQService] Result queue asserted: plant-analysis-multi-image-results

# NO PRECONDITION_FAILED errors!
```

**Test Functionality:**
1. Submit a plant analysis request via API
2. Verify message flows through dispatcher
3. Verify TypeScript worker processes request
4. Verify C# worker receives result
5. Check analysis appears in database

---

### Phase 2: Production Environment (AFTER Staging Success)

#### Step 2.1: Merge to Master
```bash
# Checkout master branch
git checkout master

# Merge staging (with standardized queue configuration)
git merge staging

# Push to master
git push origin master
```

#### Step 2.2: Delete All Queues (Production)
```bash
# Use Method 1 (UI) or Method 2 (CLI) above for PRODUCTION RabbitMQ
# Verify deletion in RabbitMQ Management UI - Queues tab should be empty
```

#### Step 2.3: Redeploy Services (Production)

**Same order as staging:**
1. TypeScript Dispatcher
2. TypeScript Analysis Worker
3. C# PlantAnalysisWorkerService
4. C# WebAPI

**Railway Deployment:**
```bash
# Railway automatically deploys on master branch push
# OR manually trigger redeploy in Railway dashboard for each service
```

#### Step 2.4: Verify Production Deployment

**Same verification steps as Staging**:
1. ✅ Check RabbitMQ Management UI for correct queue arguments
2. ✅ Check service logs for successful initialization
3. ✅ Test end-to-end plant analysis flow
4. ✅ Monitor for 15-30 minutes for any errors

---

## 🔍 Verification Checklist

### RabbitMQ Queue Configuration

```bash
# Check via HTTP API
curl -u [USER]:[PASS] http://[RABBITMQ_HOST]:15672/api/queues/%2F

# Expected response for analysis queues:
{
  "name": "plant-analysis-results",
  "durable": true,
  "auto_delete": false,
  "arguments": {
    "x-message-ttl": 86400000
  }
}

# Expected response for admin queues:
{
  "name": "dealer-invitation-requests",
  "durable": true,
  "auto_delete": false,
  "arguments": {}
}
```

### Service Health Checks

**C# Worker Service:**
```bash
# Check logs for initialization success
grep "RABBITMQ_INIT_SUCCESS" application.log
grep "RABBITMQ_MULTI_IMAGE_INIT_SUCCESS" application.log

# Should NOT see:
grep "PRECONDITION_FAILED" application.log  # Should be empty
```

**TypeScript Dispatcher:**
```bash
# Check logs
docker logs ziraai-dispatcher | grep "Connected to RabbitMQ"
docker logs ziraai-dispatcher | grep "Consuming from raw-analysis-queue"
```

**TypeScript Analysis Worker:**
```bash
# Check logs
docker logs ziraai-analysis-worker | grep "Successfully connected to RabbitMQ"
docker logs ziraai-analysis-worker | grep "Result queue asserted"
```

### End-to-End Test

**Test Single-Image Analysis:**
```bash
# POST /api/PlantAnalysis/analyze-async
# Body: { "ImageBase64": "...", "LanguageCode": "en" }

# Expected flow:
# 1. WebAPI → raw-analysis-queue (TTL: 24h)
# 2. Dispatcher → openai-analysis-queue (TTL: 24h)
# 3. TypeScript Worker → plant-analysis-results (TTL: 24h)
# 4. C# Worker → Database

# Verify in RabbitMQ UI:
# - Messages flow through queues
# - No dead letters in analysis-dlq
```

**Test Multi-Image Analysis:**
```bash
# POST /api/PlantAnalysis/analyze-multi-image-async
# Body: { "ImageUrls": ["...", "..."], "LanguageCode": "en" }

# Expected flow:
# 1. WebAPI → raw-analysis-queue (TTL: 24h)
# 2. Dispatcher → openai-analysis-queue (TTL: 24h)
# 3. TypeScript Worker → plant-analysis-multi-image-results (TTL: 24h)
# 4. C# Worker → Database

# Verify multi-image queue has TTL now!
```

**Test Admin Operations:**
```bash
# POST /api/BulkOperations/distribute-codes
# Verify dealer-invitation-requests queue has NO TTL

# POST /api/BulkOperations/assign-subscriptions
# Verify farmer-subscription-assignment-requests queue has NO TTL
```

---

## 📊 Expected Results

### Before Deployment (OLD Configuration)
```
Production:
- plant-analysis-results: TTL = 24h ✅
- plant-analysis-multi-image-results: NO TTL ❌

Staging:
- plant-analysis-results: NO TTL ❌
- plant-analysis-multi-image-results: NO TTL ❌

Result: Environment-specific code, PRECONDITION_FAILED errors
```

### After Deployment (NEW Configuration)
```
Production:
- plant-analysis-results: TTL = 24h ✅
- plant-analysis-multi-image-results: TTL = 24h ✅
- All other analysis queues: TTL = 24h ✅
- All admin queues: NO TTL ✅

Staging:
- plant-analysis-results: TTL = 24h ✅
- plant-analysis-multi-image-results: TTL = 24h ✅
- All other analysis queues: TTL = 24h ✅
- All admin queues: NO TTL ✅

Result: Identical configuration, NO errors, consistent behavior
```

---

## 🚨 Troubleshooting

### Issue: PRECONDITION_FAILED After Deployment

**Symptom:**
```
PRECONDITION_FAILED - inequivalent arg 'x-message-ttl' for queue 'xxx'
```

**Cause**: Queue was not deleted before deployment, old configuration still exists

**Fix:**
```bash
# 1. Stop all services
# 2. Delete the problematic queue via RabbitMQ Management UI
# 3. Restart services (they will recreate queue with correct TTL)
```

### Issue: Queue Not Created After Deployment

**Symptom:**
```
[ERROR] Queue 'xxx' not found
```

**Cause**: Service didn't start properly or RabbitMQ connection failed

**Fix:**
```bash
# 1. Check RabbitMQ is running and accessible
# 2. Verify RabbitMQ connection string in service configuration
# 3. Check service logs for connection errors
# 4. Restart service
```

### Issue: Messages Not Flowing

**Symptom:** Messages stuck in queues, not being consumed

**Cause:** Consumer service not started or crashed

**Fix:**
```bash
# 1. Check consumer service logs
# 2. Verify consumer service is running: docker ps | grep ziraai
# 3. Restart consumer service
# 4. Check RabbitMQ Management UI for active consumers
```

---

## 📝 Rollback Plan

### If Issues Occur in Production:

1. **Immediate**: Revert master to previous commit
   ```bash
   git revert 2909a02d
   git push origin master
   ```

2. **Delete Queues Again**: Clear all queues in production

3. **Redeploy Previous Version**: Trigger redeployment on Railway

4. **Queues Will Match Old Code**: No PRECONDITION_FAILED errors

5. **Investigate**: Review logs, test in staging again

---

## ✅ Success Criteria

- [ ] All 10 queues recreated in Staging with correct TTL
- [ ] All 10 queues recreated in Production with correct TTL
- [ ] No PRECONDITION_FAILED errors in any service logs
- [ ] Single-image analysis end-to-end test passes
- [ ] Multi-image analysis end-to-end test passes
- [ ] Admin operations (bulk invitations, code distribution) work
- [ ] Service logs show successful RabbitMQ initialization
- [ ] RabbitMQ Management UI shows correct queue arguments
- [ ] No increase in error rate after 30 minutes of monitoring

---

**Commit**: 2909a02d
**Documentation**: RABBITMQ_QUEUE_STANDARDIZATION_PLAN.md
**Related**: RABBITMQ_QUEUE_CONFIGURATION_COMPLETE_AUDIT.md
**Deployment**: Staging ✅ → Master ⏳ → Production ⏳
