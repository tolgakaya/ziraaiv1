# Realtime Plant Analysis Notification System - Architecture & Implementation Plan

## Project Overview
**Feature**: Realtime notification system for async plant analysis completion
**Branch**: `feature/realtime-analyses-notification-on-app`
**Base Branch**: `development`
**Target Platforms**: Flutter Mobile App + Web App (Angular/React)
**Date Created**: 2025-09-30
**Status**: Planning Phase - Not Started

---

## Business Requirements

### Primary Goal
Notify users in real-time when their asynchronous plant analysis is completed, providing instant feedback and improving user experience.

### Use Cases

#### Use Case 1: Active User (App Foreground)
- User uploads plant image for analysis
- User waits on analysis detail/loading screen
- Analysis completes (2-5 minutes)
- **Expected**: Instant in-app notification + UI update
- **Solution**: SignalR realtime push

#### Use Case 2: Background User (App Backgrounded)
- User uploads plant image
- User switches to another app
- Analysis completes
- **Expected**: Local notification banner
- **Solution**: SignalR + Flutter local notifications

#### Use Case 3: Web User (Browser Active)
- User uploads analysis via web app
- User browses other pages/tabs
- Analysis completes
- **Expected**: Toast notification + badge update
- **Solution**: SignalR + Web Notifications API

#### Use Case 4: Offline User (Future Enhancement - Phase 2)
- User uploads analysis
- User closes app completely
- Analysis completes
- **Expected**: System push notification
- **Solution**: FCM (Firebase Cloud Messaging)

---

## Technical Solution: SignalR (Phase 1 MVP)

### Why SignalR?

**Advantages:**
✅ **Real-time**: Instant notification via WebSocket (no polling)
✅ **Bidirectional**: Two-way communication for future features
✅ **Native Support**: ASP.NET Core built-in integration
✅ **Cross-Platform**: Flutter (`signalr_netcore`) + Web (`@microsoft/signalr`)
✅ **Battery Efficient**: Persistent connection, 90% less battery than polling
✅ **Scalable**: Redis backplane for multi-instance deployment
✅ **Secure**: JWT authentication integration
✅ **Connection Management**: Auto-reconnection, heartbeat, lifecycle events

**Disadvantages:**
❌ App closed = No notification (mitigated by Phase 2 FCM)
⚠️ Flutter package community-maintained (but stable: 50K+ downloads, actively maintained)

**Decision**: SignalR optimal for MVP because:
1. Users typically wait on app during 2-5 minute analysis
2. Professional, production-grade solution
3. Aligns with existing tech stack (RabbitMQ, Redis, PostgreSQL)
4. Future-proof for bidirectional features (chat, live dashboards)

---

## Architecture Design

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────┐              ┌──────────────────┐        │
│  │  Flutter Mobile  │              │    Web App       │        │
│  │  (iOS/Android)   │              │  (Angular/React) │        │
│  │                  │              │                  │        │
│  │  signalr_netcore │              │ @microsoft/      │        │
│  │  package         │              │ signalr          │        │
│  └────────┬─────────┘              └────────┬─────────┘        │
│           │                                 │                   │
└───────────┼─────────────────────────────────┼───────────────────┘
            │                                 │
            │    WebSocket Connection (wss://)│
            │                                 │
┌───────────┼─────────────────────────────────┼───────────────────┐
│           │         API LAYER               │                   │
├───────────┴─────────────────────────────────┴───────────────────┤
│                                                                  │
│           ┌──────────────────────────────────┐                 │
│           │   SignalR Hub                    │                 │
│           │   /hubs/plantanalysis            │                 │
│           │                                  │                 │
│           │   - PlantAnalysisHub.cs          │                 │
│           │   - JWT Authentication           │                 │
│           │   - User-specific broadcasting   │                 │
│           └─────────────┬────────────────────┘                 │
│                         │                                       │
└─────────────────────────┼───────────────────────────────────────┘
                          │
┌─────────────────────────┼───────────────────────────────────────┐
│                         │    BUSINESS LAYER                     │
├─────────────────────────┴───────────────────────────────────────┤
│                                                                  │
│  ┌───────────────────────────────────────────────────────┐     │
│  │   PlantAnalysisWorkerService (RabbitMQ Consumer)      │     │
│  │                                                        │     │
│  │   Flow:                                                │     │
│  │   1. Consume message from RabbitMQ                     │     │
│  │   2. Send to N8N for AI analysis                       │     │
│  │   3. Receive N8N response (2-5 min)                    │     │
│  │   4. Update database (Status: Completed)               │     │
│  │   5. 🆕 Trigger SignalR notification via IHubContext   │     │
│  │                                                        │     │
│  │   _hubContext.Clients.User(userId.ToString())          │     │
│  │     .SendAsync("AnalysisCompleted", notificationDto);  │     │
│  └────────────────────────────────────────────────────────┘     │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
                          │
┌─────────────────────────┼───────────────────────────────────────┐
│                         │    DATA LAYER                         │
├─────────────────────────┴───────────────────────────────────────┤
│                                                                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐     │
│  │  PostgreSQL  │    │   RabbitMQ   │    │    Redis     │     │
│  │              │    │              │    │              │     │
│  │  Analysis    │    │   Async      │    │  Connection  │     │
│  │  Storage     │    │   Queue      │    │  Mapping     │     │
│  └──────────────┘    └──────────────┘    └──────────────┘     │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Message Flow Sequence

```
1. User Action: Upload Plant Image
   └─> POST /api/v1/plantanalyses/async
       └─> Return: { analysisId: 123, status: "Processing" }
       └─> Publish message to RabbitMQ

2. Background Processing
   └─> PlantAnalysisWorkerService consumes message
       └─> Call N8N webhook with image
           └─> N8N AI Analysis (2-5 minutes)
               └─> Return analysis results

3. Database Update
   └─> Update PlantAnalysis table
       └─> Status: "Completed"
       └─> Store analysis results (JSONB)

4. 🆕 SignalR Notification Trigger (Cross-Process Communication)
   └─> Worker makes HTTP POST to WebAPI internal endpoint
       └─> POST /api/internal/signalr/analysis-completed
           └─> Internal secret authentication
               └─> WebAPI SignalRNotificationController receives request
                   └─> IPlantAnalysisNotificationService.NotifyAnalysisCompleted()
                       └─> IHubContext<PlantAnalysisHub> broadcasts to connected clients

5. Client Notification
   ├─> Flutter Client
   │   └─> Event listener receives "AnalysisCompleted"
   │       └─> Update UI state (Provider/Bloc)
   │       └─> Show local notification (if backgrounded)
   │       └─> Auto-navigate to results screen
   │
   └─> Web Client
       └─> Event listener receives "AnalysisCompleted"
           └─> Show toast notification
           └─> Update analysis list
           └─> Badge count update
```

---

## Implementation Roadmap

### Phase 1: Backend Implementation (Week 1)

| Day | Task | Deliverable |
|-----|------|-------------|
| 1-2 | SignalR Hub + Notification Service | `PlantAnalysisHub.cs`, `PlantAnalysisNotificationService.cs` |
| 3 | Worker Service Integration | Updated `Worker.cs` with IHubContext injection |
| 4 | Unit Tests + Integration Tests | Test coverage >80% |
| 5 | Local Testing | Postman/SignalR test client validation |

### Phase 2: Client Implementation (Week 2)

| Day | Task | Deliverable |
|-----|------|-------------|
| 1-2 | Flutter SignalR Integration | `signalr_service.dart`, `notification_handler.dart` |
| 3 | Web SignalR Integration | `signalRService.ts`, `useSignalR.ts` |
| 4-5 | End-to-End Testing | All platforms tested |

### Phase 3: Deployment (Week 3)

| Day | Task | Deliverable |
|-----|------|-------------|
| 1-2 | Staging Deployment | Backend + Client deployed to staging |
| 3-5 | User Acceptance Testing | Feedback collection |
| 6-7 | Production Rollout (10% → 50% → 100%) | Gradual release with monitoring |

---

## Backend Implementation Details

### 1. SignalR Hub

**File**: `WebAPI/Hubs/PlantAnalysisHub.cs`

### 2. Notification Service

**File**: `Business/Services/Notification/PlantAnalysisNotificationService.cs`

### 3. Notification DTO

**File**: `Entities/Dtos/PlantAnalysisNotificationDto.cs`

```csharp
public class PlantAnalysisNotificationDto
{
    public int AnalysisId { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } // "Completed"
    public DateTime CompletedAt { get; set; }

    // Preview data
    public string CropType { get; set; }
    public string PrimaryConcern { get; set; }
    public int? OverallHealthScore { get; set; }
    public string ImageUrl { get; set; }

    // Deep link
    public string DeepLink { get; set; } // "app://analysis/123"
}
```

### 4. Startup Configuration

**File**: `WebAPI/Startup.cs`

```csharp
// Add SignalR with Redis backplane
services.AddSignalR()
    .AddStackExchangeRedis(Configuration.GetConnectionString("Redis"), options =>
    {
        options.Configuration.ChannelPrefix = "ZiraAI:SignalR:";
    });

// Register notification service
services.AddScoped<IPlantAnalysisNotificationService, PlantAnalysisNotificationService>();

// Map hub endpoint
endpoints.MapHub<PlantAnalysisHub>("/hubs/plantanalysis");
```

---

## Flutter Implementation Details

### 1. Package Dependencies

```yaml
dependencies:
  signalr_netcore: ^1.3.4
  flutter_local_notifications: ^17.0.0
  provider: ^6.1.1
```

### 2. SignalR Service

**File**: `lib/services/signalr_service.dart`

Key features:
- Connection management with JWT authentication
- Auto-reconnection with exponential backoff
- Event handlers for AnalysisCompleted/Failed
- Ping/Pong for connection health check

### 3. Notification Handler

**File**: `lib/services/notification_handler.dart`

Features:
- Flutter local notifications for background
- Deep link navigation on tap
- Platform-specific notification styles

---

## Web Implementation Details

### 1. SignalR Service (TypeScript)

**File**: `src/services/signalRService.ts`

### 2. React Hook

**File**: `src/hooks/useSignalR.ts`

---

## Configuration

### Backend (appsettings.json)

```json
{
  "SignalR": {
    "UseRedisBackplane": true,
    "MaxConnectionsPerUser": 5,
    "ConnectionTimeout": 30,
    "KeepAliveInterval": 15
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  }
}
```

### Flutter (config.dart)

```dart
class SignalRConfig {
  static const String hubUrl = 'https://api.ziraai.com/hubs/plantanalysis';
  static const bool autoReconnect = true;
  static const int reconnectInterval = 5000; // ms
}
```

---

## Testing Strategy

### Unit Tests
- Hub methods
- Notification service
- DTO serialization

### Integration Tests
- Worker → Hub → Client flow
- Multiple concurrent connections
- Reconnection scenarios

### Manual Tests
- Flutter foreground/background
- Web active/inactive tab
- Multiple devices same user
- Network interruption recovery

---

## Monitoring & Observability

### Metrics
- SignalR connection count
- Notification delivery rate
- Average latency
- Reconnection frequency

### Logging
```
[Info] User {userId} connected with ConnectionId: {connectionId}
[Info] Sent AnalysisCompleted notification to user {userId} for analysis {analysisId}
[Error] Failed to send notification: {exception}
```

### Alerts
- Connection failure rate > 10% → Critical
- Notification delivery failure > 5% → Critical
- Memory usage > 80% → Warning

---

## Security

### Authentication
- JWT Bearer token required for Hub connection
- User isolation: `Clients.User(userId)` targeting
- No group broadcasts
- Internal API secret for Worker→WebAPI communication

### CORS
- Whitelist specific origins only
- `AllowCredentials()` for SignalR

### Data Privacy
- Summary data only in notifications
- No sensitive medical information
- Pre-signed image URLs

### Internal Communication Security
- Shared secret authentication between Worker Service and WebAPI
- Configuration:
```json
"WebAPI": {
  "BaseUrl": "https://localhost:5001",
  "InternalSecret": "ZiraAI_Internal_Secret_2025"
}
```
- Production: Move secret to environment variables or Azure Key Vault

---

## Performance

### Connection Management
- Max 5 connections per user
- Idle timeout: 30 seconds
- KeepAlive interval: 15 seconds

### Resource Usage
- Memory per connection: ~10 KB
- 10,000 connections ≈ 100 MB RAM
- Battery impact: < 1% per hour (client)

---

## Future Enhancements (Phase 2)

### 1. Firebase Cloud Messaging (FCM)
- Push notifications when app closed
- Timeline: 2-3 weeks after Phase 1

### 2. Progress Updates
- Real-time analysis progress (0% → 100%)
- Timeline: 1 week

### 3. Bidirectional Features
- In-app chat (Farmer ↔ Sponsor)
- Timeline: 4-6 weeks

---

## Troubleshooting

### Common Issues

**Issue**: Client can't connect
**Solution**: Check JWT validity, CORS config, hub URL

**Issue**: Notifications not received
**Solution**: Verify connection, check userId in JWT, inspect Worker logs

**Issue**: Frequent disconnections
**Solution**: Network stability, adjust KeepAlive, check firewall

---

## Documentation Links

- SignalR Official: https://learn.microsoft.com/aspnet/core/signalr/introduction
- Flutter Package: https://pub.dev/packages/signalr_netcore
- Web Client: https://www.npmjs.com/package/@microsoft/signalr

---

## Change Log

| Date | Author | Change | Reason |
|------|--------|--------|--------|
| 2025-09-30 | System | Initial plan created | Feature kickoff |
| 2025-09-30 | System | Updated notification flow with HTTP cross-process communication | Worker Service runs separately from WebAPI, cannot directly access IHubContext |
| 2025-09-30 | System | Added configuration examples and security notes | Implementation completed successfully |
| 2025-10-01 | System | Fixed WorkerService environment detection | ASPNETCORE_ENVIRONMENT consistency with WebAPI |
| 2025-10-01 | System | Staging deployment verified and tested | Railway staging environment working correctly |

---

## Implementation Summary

### ✅ Completed Components

1. **SignalR Hub**: `Business/Hubs/PlantAnalysisHub.cs`
   - JWT authentication
   - Connection lifecycle logging
   - Ping/Pong health check

2. **Notification DTO**: `Entities/Dtos/PlantAnalysisNotificationDto.cs`
   - Complete analysis result data structure

3. **Notification Service**: `Business/Services/Notification/PlantAnalysisNotificationService.cs`
   - IHubContext integration
   - User-targeted messaging

4. **Internal API Controller**: `WebAPI/Controllers/SignalRNotificationController.cs`
   - Cross-process communication endpoint
   - Internal secret authentication
   - POST `/api/internal/signalr/analysis-completed`

5. **Worker Service Integration**: `PlantAnalysisWorkerService/Jobs/PlantAnalysisJobService.cs`
   - HTTP client notification delivery
   - Configuration-based WebAPI URL

6. **Configuration Files**: `appsettings.json`, `appsettings.Development.json`
   - WebAPI base URL and internal secret

7. **Test Client**: `claudedocs/test_signalr_client.html`
   - Browser-based SignalR connection testing
   - JWT token authentication
   - Real-time notification display

8. **Startup Configuration**: `WebAPI/Startup.cs`
   - SignalR service registration
   - CORS policy for SignalR
   - JWT query string authentication
   - Hub endpoint mapping

---

**Status**: ✅ Phase 1 Backend Implementation COMPLETED & DEPLOYED TO STAGING
**Test Status**: ✅ Successfully tested on Railway staging environment
**Staging Verification** (2025-10-01):
- ✅ WorkerService environment detection fixed (ASPNETCORE_ENVIRONMENT)
- ✅ Internal secret authentication working (Length: 35 characters)
- ✅ SignalR notifications sent from Worker to WebAPI successfully
- ✅ Real async plant analysis → notification flow verified
- ✅ PR #49 merged to staging branch

**Next Steps**:
1. ✅ ~~Commit changes to feature branch~~ COMPLETED
2. ✅ ~~Test with real async analysis end-to-end~~ COMPLETED ON STAGING
3. ✅ ~~Prepare staging deployment~~ COMPLETED & VERIFIED
4. 🔄 **IN PROGRESS**: Flutter SignalR client integration (separate session)
5. ⏳ **PENDING**: Production deployment after Flutter client complete
6. ⏳ **PENDING**: End-to-end production testing