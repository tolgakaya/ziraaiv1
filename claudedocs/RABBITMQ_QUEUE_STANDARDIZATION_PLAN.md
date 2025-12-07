# RabbitMQ Queue Configuration Standardization Plan

**Date**: 2025-12-07
**Objective**: Consolidate queue configurations across ALL applications for consistent deployment
**Deployment Strategy**: Delete all queues → Redeploy all services → Staging = Production

---

## 🎯 Standardized Queue Configuration

### Analysis Queues (WITH 24h TTL)

| Queue Name | TTL | Purpose | Created By | Consumed By |
|-----------|-----|---------|------------|-------------|
| `raw-analysis-queue` | **24h** | Entry point for analysis requests | WebAPI/Dispatcher | Dispatcher |
| `openai-analysis-queue` | **24h** | OpenAI provider queue | Dispatcher | TypeScript Worker |
| `gemini-analysis-queue` | **24h** | Gemini provider queue | Dispatcher | TypeScript Worker |
| `anthropic-analysis-queue` | **24h** | Anthropic provider queue | Dispatcher | TypeScript Worker |
| `analysis-dlq` | **24h** | Dead letter queue | Dispatcher | N/A |
| `plant-analysis-results` | **24h** | Single-image results | TypeScript Worker/WebAPI | C# Worker |
| `plant-analysis-multi-image-results` | **24h** | Multi-image results | TypeScript Worker/WebAPI | C# Worker |

### Admin Queues (NO TTL)

| Queue Name | TTL | Purpose | Created By | Consumed By |
|-----------|-----|---------|------------|-------------|
| `dealer-invitation-requests` | **None** | Bulk dealer invitations | WebAPI | C# Worker |
| `farmer-code-distribution-requests` | **None** | Bulk code distribution | WebAPI | C# Worker |
| `farmer-subscription-assignment-requests` | **None** | Bulk subscription assignments | WebAPI | C# Worker |

---

## 📋 Design Principles

### 1. Analysis Queues = 24h TTL
**All** plant analysis related queues use 24h TTL (86400000ms).

**Rationale**:
- ✅ Prevents message buildup during system downtime
- ✅ Analysis results older than 24h are stale
- ✅ Automatic cleanup without manual intervention
- ✅ Consistent behavior across all analysis workflows

### 2. Admin Queues = No TTL
Bulk operation queues have **no TTL**.

**Rationale**:
- ✅ Admin operations must never be lost
- ✅ Can be processed even if delayed
- ✅ Finite volume (not continuous like analysis)
- ✅ Critical business operations

### 3. Consistency Across All Services
**ALL** services declaring the same queue **MUST** use identical parameters.

**Parameters**:
```csharp
// Analysis queues
arguments: new Dictionary<string, object>
{
    { "x-message-ttl", 86400000 }
}

// Admin queues
arguments: null
```

---

## 🔧 Required Code Changes

### 1. C# PlantAnalysisWorkerService

#### RabbitMQConsumerWorker.cs (plant-analysis-results)
**Lines 103-114** - ✅ **ALREADY CORRECT** (has TTL)

```csharp
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

#### RabbitMQMultiImageConsumerWorker.cs (plant-analysis-multi-image-results)
**Lines 103-110** - ⚠️ **NEEDS CHANGE** (currently NO TTL, should have TTL)

**CURRENT (WRONG)**:
```csharp
// No TTL for multi-image queue - matches production configuration
await _channel.QueueDeclareAsync(
    queue: _rabbitMQOptions.Queues.PlantAnalysisMultiImageResult,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);
```

**REQUIRED (CORRECT)**:
```csharp
// Analysis queue - 24h TTL (consistent with all analysis queues)
var queueArguments = new Dictionary<string, object>
{
    { "x-message-ttl", 86400000 } // 24 hours TTL
};

await _channel.QueueDeclareAsync(
    queue: _rabbitMQOptions.Queues.PlantAnalysisMultiImageResult,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: queueArguments);
```

#### DealerInvitationConsumerWorker.cs
**Lines 94-99** - ✅ **ALREADY CORRECT** (no TTL for admin queue)

#### FarmerCodeDistributionConsumerWorker.cs
**Lines 95-100** - ✅ **ALREADY CORRECT** (no TTL for admin queue)

#### FarmerSubscriptionAssignmentConsumerWorker.cs
**Lines 93-98** - ✅ **ALREADY CORRECT** (no TTL for admin queue)

---

### 2. C# Business Layer

#### SimpleRabbitMQService.cs
**Lines 67-80** - ⚠️ **NEEDS CHANGE** (add plant-analysis-multi-image-results back to TTL list)

**CURRENT (WRONG)**:
```csharp
if (queueName == "raw-analysis-queue" ||
    queueName == "plant-analysis-results")
{
    queueArguments = new Dictionary<string, object>
    {
        { "x-message-ttl", 86400000 }
    };
}
```

**REQUIRED (CORRECT)**:
```csharp
// ALL analysis queues have 24h TTL
if (queueName == "raw-analysis-queue" ||
    queueName == "plant-analysis-results" ||
    queueName == "plant-analysis-multi-image-results")
{
    queueArguments = new Dictionary<string, object>
    {
        { "x-message-ttl", 86400000 } // 24 hours TTL
    };
    Console.WriteLine($"[SimpleRabbitMQService.PublishAsync] Using TTL for queue: {queueName}");
}
```

---

### 3. TypeScript Dispatcher

#### workers/dispatcher/src/dispatcher.ts
**Lines 47-57** - ✅ **ALREADY CORRECT** (all queues have 24h TTL)

```typescript
const queueOptions = {
  durable: true,
  arguments: { 'x-message-ttl': 86400000 } // 24 hours message TTL
};

await ch.assertQueue(this.config.rabbitmq.queues.rawAnalysis, queueOptions);
await ch.assertQueue(this.config.rabbitmq.queues.openai, queueOptions);
await ch.assertQueue(this.config.rabbitmq.queues.gemini, queueOptions);
await ch.assertQueue(this.config.rabbitmq.queues.anthropic, queueOptions);
await ch.assertQueue(this.config.rabbitmq.queues.dlq, queueOptions);
```

---

### 4. TypeScript Analysis Worker

#### workers/analysis-worker/src/services/rabbitmq.service.ts
**Lines 104-109** - ✅ **ALREADY CORRECT** (result queues have 24h TTL)

```typescript
await this.channel.assertQueue(queue, {
  durable: true,
  arguments: {
    'x-message-ttl': 86400000, // 24 hours
  },
});
```

---

## ✅ Summary of Required Changes

### Changes Needed: 2 Files

1. **PlantAnalysisWorkerService/Services/RabbitMQMultiImageConsumerWorker.cs**
   - Add TTL argument back (24h)
   - Change `arguments: null` → `arguments: queueArguments` with TTL

2. **Business/Services/MessageQueue/SimpleRabbitMQService.cs**
   - Add `plant-analysis-multi-image-results` back to TTL queue list

### Already Correct: 6 Files

1. ✅ RabbitMQConsumerWorker.cs (has TTL)
2. ✅ DealerInvitationConsumerWorker.cs (no TTL)
3. ✅ FarmerCodeDistributionConsumerWorker.cs (no TTL)
4. ✅ FarmerSubscriptionAssignmentConsumerWorker.cs (no TTL)
5. ✅ workers/dispatcher/src/dispatcher.ts (all queues have TTL)
6. ✅ workers/analysis-worker/src/services/rabbitmq.service.ts (result queues have TTL)

---

## 🚀 Deployment Steps

### Pre-Deployment: Delete All Queues

**RabbitMQ Management UI** (http://localhost:15672 or Railway):

```bash
# Option 1: Via RabbitMQ Management UI
# Go to Queues tab → Select each queue → Delete

# Option 2: Via rabbitmqadmin CLI
rabbitmqadmin delete queue name=raw-analysis-queue
rabbitmqadmin delete queue name=openai-analysis-queue
rabbitmqadmin delete queue name=gemini-analysis-queue
rabbitmqadmin delete queue name=anthropic-analysis-queue
rabbitmqadmin delete queue name=analysis-dlq
rabbitmqadmin delete queue name=plant-analysis-results
rabbitmqadmin delete queue name=plant-analysis-multi-image-results
rabbitmqadmin delete queue name=dealer-invitation-requests
rabbitmqadmin delete queue name=farmer-code-distribution-requests
rabbitmqadmin delete queue name=farmer-subscription-assignment-requests
```

### Deployment Order

1. **Apply Code Changes** (2 files)
2. **Build and Test** locally
3. **Delete All Queues** in Staging RabbitMQ
4. **Deploy Staging**:
   - TypeScript Dispatcher
   - TypeScript Analysis Worker
   - C# PlantAnalysisWorkerService
   - C# WebAPI
5. **Verify Staging**: Check queue arguments via RabbitMQ Management UI
6. **Merge to Master**
7. **Delete All Queues** in Production RabbitMQ
8. **Deploy Production** (same order as staging)
9. **Verify Production**: Check queue arguments via RabbitMQ Management UI

---

## 🔍 Verification Checklist

### After Deployment

```bash
# Check queue configuration via RabbitMQ Management API
GET http://localhost:15672/api/queues/%2F

# Expected for analysis queues:
{
  "name": "plant-analysis-results",
  "arguments": {
    "x-message-ttl": 86400000
  }
}

# Expected for admin queues:
{
  "name": "dealer-invitation-requests",
  "arguments": {}
}
```

### Monitor Logs

```bash
# C# Worker Service - Should see successful initialization
[RABBITMQ_INIT_SUCCESS] RabbitMQ initialized successfully - QueueName: plant-analysis-results
[RABBITMQ_MULTI_IMAGE_INIT_SUCCESS] Multi-Image RabbitMQ initialized successfully - QueueName: plant-analysis-multi-image-results

# TypeScript Dispatcher
[Dispatcher] Connected to RabbitMQ
[Dispatcher] Consuming from raw-analysis-queue

# TypeScript Analysis Worker
[RabbitMQService] Successfully connected to RabbitMQ
[RabbitMQService] Result queue asserted: plant-analysis-results
```

---

## 📊 Final Configuration Matrix

| Queue | Analysis | Admin | TTL | C# WebAPI | C# Worker | TS Dispatcher | TS Worker |
|-------|----------|-------|-----|-----------|-----------|---------------|-----------|
| raw-analysis-queue | ✅ | | 24h | Publishes | | Creates | |
| openai-analysis-queue | ✅ | | 24h | | | Creates | Consumes |
| gemini-analysis-queue | ✅ | | 24h | | | Creates | Consumes |
| anthropic-analysis-queue | ✅ | | 24h | | | Creates | Consumes |
| analysis-dlq | ✅ | | 24h | | | Creates | |
| plant-analysis-results | ✅ | | 24h | Publishes | Consumes | | Creates |
| plant-analysis-multi-image-results | ✅ | | 24h | Publishes | Consumes | | Creates |
| dealer-invitation-requests | | ✅ | None | Publishes | Consumes | | |
| farmer-code-distribution-requests | | ✅ | None | Publishes | Consumes | | |
| farmer-subscription-assignment-requests | | ✅ | None | Publishes | Consumes | | |

---

## 🎯 Benefits of Standardization

1. ✅ **Identical Staging/Production**: No environment-specific queue configurations
2. ✅ **Predictable Behavior**: Same TTL rules across all environments
3. ✅ **Easier Debugging**: Consistent configuration reduces variables
4. ✅ **Simplified Deployment**: No environment-specific code branches
5. ✅ **Automatic Cleanup**: TTL prevents queue overflow
6. ✅ **Data Protection**: Admin queues never expire

---

**Next Steps**: Apply the 2 code changes, test build, then proceed with queue deletion and redeployment.
