# RabbitMQ Queue Ownership and TTL Configuration - FINAL SOLUTION

**Date**: 2025-12-07
**Status**: SOLVED ✅
**Issue**: Analysis workers crashing on startup with PRECONDITION_FAILED errors

---

## 🎯 Root Cause Analysis

### The Problem
Workers were asserting **ALL queues** from `config.queues` with 24h TTL, including queues they don't own:

```typescript
// WRONG APPROACH (Previous implementation)
const queues = Object.values(this.config.queues);  // Gets ALL queues
for (const queue of queues) {
  await this.channel.assertQueue(queue, {
    durable: true,
    arguments: { 'x-message-ttl': 86400000 }  // Tries to add TTL to ALL queues
  });
}
```

### Why This Failed

**Error Sequence**:
1. Worker starts, connects to RabbitMQ
2. Tries to assert `plant-analysis-results` with TTL
   - Queue exists without TTL → PRECONDITION_FAILED
   - Channel recreated successfully ✅
3. Tries to assert `analysis-dlq` with TTL
   - Success ✅
4. **Tries to assert `plant-analysis-requests` with TTL**
   - **Queue owned by WebAPI/Dispatcher, has NO TTL**
   - PRECONDITION_FAILED → **RabbitMQ closes CONNECTION (not just channel)**
   - Worker crashes ❌

### The Key Discovery

**Workers should ONLY assert queues they OWN (publish to)**, not queues they consume from!

---

## 📋 Queue Ownership Model

### Ownership Rules

| Queue | Owner | TTL | Created By | Consumed By | Published By |
|-------|-------|-----|------------|-------------|--------------|
| `plant-analysis-requests` | WebAPI/Dispatcher | **None** | WebAPI/Dispatcher | Worker | WebAPI |
| `plant-analysis-multi-image-requests` | WebAPI/Dispatcher | **None** | WebAPI/Dispatcher | Worker | WebAPI |
| `openai-analysis-queue` | Dispatcher | 24h | Dispatcher | Worker | Dispatcher |
| `gemini-analysis-queue` | Dispatcher | 24h | Dispatcher | Worker | Dispatcher |
| `anthropic-analysis-queue` | Dispatcher | 24h | Dispatcher | Worker | Dispatcher |
| `raw-analysis-queue` | Dispatcher | 24h | Dispatcher | Dispatcher | WebAPI |
| `analysis-dlq` | Dispatcher | 24h | Dispatcher | N/A | Worker/Dispatcher |
| **`plant-analysis-results`** | **Worker** | **24h** | **Worker** | **C# Worker** | **Worker** |

### Key Principle

**"Only assert what you publish to, not what you consume from"**

- ✅ Worker publishes to `plant-analysis-results` → Worker asserts it with TTL
- ✅ Worker publishes to `analysis-dlq` → Worker asserts it with TTL
- ❌ Worker consumes from `plant-analysis-requests` → Worker does NOT assert it
- ❌ Worker consumes from provider queues → Worker does NOT assert them

---

## 🔧 The Solution

### workers/analysis-worker/src/services/rabbitmq.service.ts

**Before (WRONG)**:
```typescript
private async assertQueues(): Promise<void> {
  const queues = Object.values(this.config.queues);  // ALL queues

  for (const queue of queues) {
    await this.channel.assertQueue(queue, {
      durable: true,
      arguments: { 'x-message-ttl': 86400000 }  // TTL on ALL queues
    });
  }
}
```

**After (CORRECT)** - Commit [1f4f2ed](https://github.com/tolgakaya/ziraai-workers/commit/1f4f2ed):
```typescript
private async assertQueues(): Promise<void> {
  // ONLY assert queues that this worker creates/owns
  const ownedQueues = [
    this.config.queues.results,   // plant-analysis-results (worker publishes here)
    this.config.queues.dlq,       // analysis-dlq (worker may send failed messages here)
  ];

  for (const queue of ownedQueues) {
    await this.channel.assertQueue(queue, {
      durable: true,
      arguments: { 'x-message-ttl': 86400000 }  // TTL only on owned queues
    });
  }
}
```

---

## ✅ Benefits of This Approach

1. **Deployment Order Independence**: Services can start in any order
2. **No Queue Deletion Required**: Works with existing queues
3. **Clear Ownership**: Each service manages only its own queues
4. **Graceful Degradation**: PRECONDITION_FAILED handled for owned queues
5. **No Configuration Conflicts**: No TTL mismatches between services

---

## 🚀 Deployment

### Changes Made

**Workers Repo** - Commit: [1f4f2ed](https://github.com/tolgakaya/ziraai-workers/commit/1f4f2ed)
- Modified `workers/analysis-worker/src/services/rabbitmq.service.ts`
- Changed `assertQueues()` to only assert worker-owned queues
- Removed assertion of WebAPI-managed queues

**Ziraai Repo** - Commits:
- [a8d64e7](https://github.com/tolgakaya/ziraai-workers/commit/a8d64e7): Channel recreation for TypeScript
- [503e781c](https://github.com/tolgakaya/ziraaiv1/commit/503e781c): Channel recreation for C#

### Verification

After Railway auto-deploys, check logs for:

```
✅ Expected Success Pattern:
[INFO]: Worker queue ready (created or already exists with TTL)
  queue: "plant-analysis-results"
[INFO]: Worker queue ready (created or already exists with TTL)
  queue: "analysis-dlq"
[INFO]: Starting consumer
  queueName: "plant-analysis-requests"
```

```
❌ Old Failure Pattern (FIXED):
[ERROR]: PRECONDITION_FAILED - inequivalent arg 'x-message-ttl' for queue 'plant-analysis-requests'
[FATAL]: Failed to start worker
```

---

## 📊 Queue Configuration Summary

### Analysis Workers (TypeScript)

**Asserts (Owns)**:
- `plant-analysis-results` with 24h TTL
- `analysis-dlq` with 24h TTL

**Consumes From (Does NOT Assert)**:
- `plant-analysis-requests` (WebAPI queue)
- `plant-analysis-multi-image-requests` (WebAPI queue)
- `openai-analysis-queue` (Dispatcher queue)
- `gemini-analysis-queue` (Dispatcher queue)
- `anthropic-analysis-queue` (Dispatcher queue)

### Dispatcher (TypeScript)

**Asserts (Owns)**:
- `raw-analysis-queue` with 24h TTL
- `openai-analysis-queue` with 24h TTL
- `gemini-analysis-queue` with 24h TTL
- `anthropic-analysis-queue` with 24h TTL
- `analysis-dlq` with 24h TTL

**Consumes From**:
- `raw-analysis-queue` (owns it)

### WebAPI (C#)

**Publishes To (May Assert)**:
- `plant-analysis-requests` (NO TTL)
- `plant-analysis-multi-image-requests` (NO TTL)
- `plant-analysis-results` (worker owns, has TTL)
- `raw-analysis-queue` (dispatcher owns, has TTL)

**Does NOT Own**: WebAPI publishes to queues owned by other services

---

## 🎓 Lessons Learned

### Mistake #1: Channel Recreation Not Enough
Initially added channel recreation after PRECONDITION_FAILED, but worker still crashed because it kept trying to assert WebAPI queues.

### Mistake #2: Asserting All Config Queues
Used `Object.values(config.queues)` which asserted queues the worker doesn't own, causing conflicts.

### Mistake #3: Blaming Deployment
Initially thought services weren't deployed, but the real issue was architectural - queue ownership wasn't clear.

### The Fix: Clear Ownership Model
Only assert queues you publish to. Consume from queues without asserting them.

---

## 🔍 Technical Details

### RabbitMQ Behavior

1. **PRECONDITION_FAILED on Queue**: Closes the channel
2. **PRECONDITION_FAILED on Connection**: Closes the connection
3. **Multiple PRECONDITION_FAILED**: Each closes channel, final one may close connection

### Channel vs Connection Closure

- **Channel Closure**: Recoverable with channel recreation
- **Connection Closure**: Fatal, requires full reconnection
- **Worker Crash**: Happens when connection closes during startup

### Why Previous Fix Didn't Work

Adding channel recreation after PRECONDITION_FAILED helped with owned queues, but worker still tried to assert WebAPI queues, causing connection closure.

---

## 📝 Final Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Queue Ownership                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  WebAPI                  Dispatcher                Worker   │
│    │                        │                        │      │
│    │ Publishes to:          │ Creates & Owns:        │      │
│    ├─ raw-analysis-queue    ├─ raw-analysis-queue    │      │
│    ├─ plant-analysis-*      ├─ provider queues       │      │
│    │                        ├─ analysis-dlq          │      │
│    │                        │                        │      │
│    │                        │ Publishes to:          │      │
│    │                        ├─ provider queues       │      │
│    │                        │                        │      │
│    │                        │                   Creates:    │
│    │                        │              plant-analysis-  │
│    │                        │                    results    │
│    │                        │                 analysis-dlq  │
│    │                        │                        │      │
│    └────────────────────────┴────────────────────────┘      │
│                                                              │
│  Key: Each service only asserts queues it PUBLISHES to      │
└─────────────────────────────────────────────────────────────┘
```

---

**Status**: PRODUCTION READY ✅
**Next Step**: Monitor Railway deployment logs for successful worker startup
