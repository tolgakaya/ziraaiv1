# RabbitMQ Queue TTL Configuration - Complete Reference

**Date**: 2025-12-07
**Issue**: PRECONDITION_FAILED errors due to TTL mismatch between queue declarations
**Resolution**: Standardize TTL configuration across all queue declarations

---

## 🔴 Problem

Production logs showed RabbitMQ connection failures:

```
PRECONDITION_FAILED - inequivalent arg 'x-message-ttl' for queue 'plant-analysis-results'
in vhost '/': received none but current is the value '86400000' of type 'signedint'
```

**Root Cause**: Queue was created with TTL by Dispatcher/TypeScript Worker, but C# Worker Service tried to declare it without TTL.

**Impact**: PlantAnalysisWorkerService couldn't start, preventing analysis results from being processed.

---

## ✅ Solution: Standardized TTL Configuration

### Queue TTL Matrix

| Queue Name | TTL | Created By | Consumed By | Purpose |
|-----------|-----|------------|-------------|---------|
| `plant-analysis-results` | **24h** | Dispatcher/Worker | RabbitMQConsumerWorker | Single-image analysis results |
| `plant-analysis-multi-image-results` | **24h** | Dispatcher/Worker | RabbitMQMultiImageConsumerWorker | Multi-image analysis results |
| `raw-analysis-queue` | **24h** | WebAPI/Dispatcher | Dispatcher | NEW system entry point |
| `openai-analysis-queue` | **24h** | Dispatcher | TypeScript Worker | OpenAI provider queue |
| `gemini-analysis-queue` | **24h** | Dispatcher | TypeScript Worker | Gemini provider queue |
| `anthropic-analysis-queue` | **24h** | Dispatcher | TypeScript Worker | Anthropic provider queue |
| `analysis-dlq` | **24h** | Dispatcher | N/A | Dead letter queue |
| `plant-analysis-requests` | **None** | WebAPI | (OLD system) | Legacy single-image requests |
| `plant-analysis-multi-image-requests` | **None** | WebAPI | (OLD system) | Legacy multi-image requests |
| `dealer-invitation-requests` | **None** | WebAPI | DealerInvitationConsumerWorker | Admin bulk operations |
| `farmer-code-distribution-requests` | **None** | WebAPI | FarmerCodeDistributionConsumerWorker | Admin bulk operations |
| `farmer-subscription-assignment-requests` | **None** | WebAPI | FarmerSubscriptionAssignmentConsumerWorker | Admin bulk operations |

---

## 📝 Code Changes

### 1. PlantAnalysisWorkerService - Single Image Worker

**File**: `PlantAnalysisWorkerService/Services/RabbitMQConsumerWorker.cs:103-114`

```csharp
// Queue arguments - must match existing queue configuration
var queueArguments = new Dictionary<string, object>
{
    { "x-message-ttl", 86400000 } // 24 hours TTL
};

await _channel.QueueDeclareAsync(
    queue: _rabbitMQOptions.Queues.PlantAnalysisResult,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: queueArguments);
```

---

### 2. PlantAnalysisWorkerService - Multi-Image Worker

**File**: `PlantAnalysisWorkerService/Services/RabbitMQMultiImageConsumerWorker.cs:103-114`

```csharp
// Queue arguments - match plant-analysis-results for consistency
var queueArguments = new Dictionary<string, object>
{
    { "x-message-ttl", 86400000 } // 24 hours TTL (same as plant-analysis-results)
};

await _channel.QueueDeclareAsync(
    queue: _rabbitMQOptions.Queues.PlantAnalysisMultiImageResult,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: queueArguments);
```

---

### 3. Business Layer - Message Queue Service

**File**: `Business/Services/MessageQueue/SimpleRabbitMQService.cs:61-80`

```csharp
// Declare queue with appropriate arguments based on queue type
// Analysis result queues need TTL to match Worker Service configuration
Dictionary<string, object> queueArguments = null;

// Queues that need TTL (must match Worker Service declarations)
if (queueName == "raw-analysis-queue" ||
    queueName == "plant-analysis-results" ||
    queueName == "plant-analysis-multi-image-results")
{
    // TTL parameter (matches Dispatcher and Worker Service configuration)
    queueArguments = new Dictionary<string, object>
    {
        { "x-message-ttl", 86400000 } // 24 hours TTL
    };
    Console.WriteLine($"[SimpleRabbitMQService.PublishAsync] Using TTL for queue: {queueName}");
}
else
{
    Console.WriteLine($"[SimpleRabbitMQService.PublishAsync] No TTL for queue: {queueName}");
}

await _channel.QueueDeclareAsync(
    queue: queueName,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: queueArguments);
```

---

## 🎯 Design Principles

### 1. **Analysis Queues = 24h TTL**
All plant analysis related queues (requests and results) use 24h TTL to prevent message buildup.

**Rationale**:
- Analysis results older than 24h are stale
- Prevents queue overflow during system downtime
- Matches TypeScript Dispatcher configuration

### 2. **Admin Operation Queues = No TTL**
Bulk operation queues (dealer invitations, code distribution, subscription assignment) have no TTL.

**Rationale**:
- Admin operations must not be lost
- Can be processed even if delayed
- Finite volume (not continuous like analysis)

### 3. **Consistency Across Layers**
All components declaring the same queue MUST use identical parameters.

**Components**:
- WebAPI (SimpleRabbitMQService)
- PlantAnalysisWorkerService (Consumer Workers)
- TypeScript Dispatcher
- TypeScript Analysis Workers

---

## 🔍 Verification

### Check Queue Configuration

```sql
-- RabbitMQ Management API
GET http://localhost:15672/api/queues/%2F/plant-analysis-results

-- Expected Response:
{
  "name": "plant-analysis-results",
  "durable": true,
  "auto_delete": false,
  "arguments": {
    "x-message-ttl": 86400000
  }
}
```

### Monitor Production Logs

```bash
# Worker should start successfully
docker logs ziraai-worker-service 2>&1 | grep "RABBITMQ_INIT_SUCCESS"

# Should see:
# [RABBITMQ_INIT_SUCCESS] RabbitMQ initialized successfully - QueueName: plant-analysis-results
# [RABBITMQ_MULTI_IMAGE_INIT_SUCCESS] Multi-Image RabbitMQ initialized successfully - QueueName: plant-analysis-multi-image-results
```

---

## 🚨 Common Mistakes to Avoid

### ❌ Inconsistent TTL Declaration

```csharp
// WebAPI declares with TTL
await _channel.QueueDeclareAsync("plant-analysis-results", true, false, false,
    new Dictionary<string, object> { { "x-message-ttl", 86400000 } });

// Worker declares WITHOUT TTL ❌
await _channel.QueueDeclareAsync("plant-analysis-results", true, false, false, null);
// Result: PRECONDITION_FAILED error
```

### ❌ Wrong TTL Value

```csharp
// Dispatcher uses 86400000 (24h)
await _channel.QueueDeclareAsync("plant-analysis-results", true, false, false,
    new Dictionary<string, object> { { "x-message-ttl", 86400000 } });

// Worker uses different value ❌
await _channel.QueueDeclareAsync("plant-analysis-results", true, false, false,
    new Dictionary<string, object> { { "x-message-ttl", 3600000 } }); // 1h instead of 24h
// Result: PRECONDITION_FAILED error
```

### ❌ Adding TTL to Existing Queue

**Problem**: Production queue exists without TTL, code tries to add TTL.

**Solution**: Must delete and recreate queue, OR ensure all declarations match existing configuration.

```bash
# Check existing queue first
rabbitmqadmin show queue name=plant-analysis-results

# If TTL exists, ALL declarations must include it
# If TTL doesn't exist, NO declarations should include it
```

---

## 📚 Related Documentation

- [RabbitMQ Queue Flow Complete](./PlatformModernization/rabbitmq-queue-flow-complete.md) - Full system architecture
- [TypeScript Worker Update Progress](./PlatformModernization/TYPESCRIPT_WORKER_UPDATE_PROGRESS.md) - Dispatcher configuration
- [Message Format Compatibility](./PlatformModernization/MESSAGE_FORMAT_COMPATIBILITY.md) - DTO schema

---

## ✅ Testing Checklist

Before deploying queue configuration changes:

- [ ] Verify all queue declarations use identical parameters (TTL, durable, exclusive, auto-delete)
- [ ] Check TypeScript Dispatcher queue configuration matches C# Worker Service
- [ ] Test queue creation in development environment first
- [ ] Monitor production logs for PRECONDITION_FAILED errors after deployment
- [ ] Verify worker services start successfully
- [ ] Confirm messages flow through queues without errors
- [ ] Check RabbitMQ Management UI shows correct queue arguments

---

**Commit**: Fix RabbitMQ queue TTL configuration for analysis result queues
**Files Changed**:
- `PlantAnalysisWorkerService/Services/RabbitMQConsumerWorker.cs`
- `PlantAnalysisWorkerService/Services/RabbitMQMultiImageConsumerWorker.cs`
- `Business/Services/MessageQueue/SimpleRabbitMQService.cs`
