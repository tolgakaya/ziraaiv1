# SignalR Real-Time Notification - Production Deployment Checklist

**Project**: ZiraAI Plant Analysis Platform
**Feature**: Real-time SignalR Notifications
**Last Updated**: 2025-10-01
**Current Status**: ✅ Staging Verified, ⏳ Awaiting Flutter Client Completion

---

## Overview

This checklist tracks all requirements for deploying SignalR real-time notifications to production. Use this as a step-by-step guide to ensure nothing is missed.

---

## ✅ Phase 1: Backend Implementation (COMPLETED)

### Core Components
- [x] SignalR Hub implementation (`PlantAnalysisHub.cs`)
- [x] Notification Service (`PlantAnalysisNotificationService.cs`)
- [x] Notification DTO (`PlantAnalysisNotificationDto.cs`)
- [x] Internal API Controller (`SignalRNotificationController.cs`)
- [x] Worker Service Integration (`PlantAnalysisJobService.cs`)
- [x] JWT Authentication for SignalR Hub
- [x] CORS Configuration (dev, staging, production domains)

### Configuration
- [x] appsettings.json configuration structure
- [x] appsettings.Development.json
- [x] appsettings.Staging.json
- [x] appsettings.Production.json (template with env vars)
- [x] Environment variable support (ASPNETCORE_ENVIRONMENT)
- [x] Railway double underscore pattern support

### Critical Fixes
- [x] WorkerService environment detection fix (PR #49)
- [x] Internal secret .NET Configuration API integration
- [x] Cross-process HTTP notification delivery

### Testing
- [x] Local development testing
- [x] Staging deployment
- [x] End-to-end staging verification (Analysis ID 28)
- [x] Internal secret authentication verified
- [x] SignalR notification flow verified

---

## 🔄 Phase 2: Client Implementation (IN PROGRESS)

### Flutter Mobile App
- [ ] Add `signalr_netcore: ^1.3.7` package to pubspec.yaml
- [ ] Create `SignalRService` singleton
- [ ] Implement JWT authentication integration
- [ ] Register event handlers:
  - [ ] `ReceiveAnalysisCompleted`
  - [ ] `ReceiveAnalysisFailed`
- [ ] UI notification display (dialog/snackbar)
- [ ] Deep link navigation to analysis detail
- [ ] App lifecycle management (background/foreground)
- [ ] Auto-reconnect implementation
- [ ] Error handling and logging
- [ ] Testing:
  - [ ] Connection establishment
  - [ ] Manual notification test (curl)
  - [ ] Real analysis notification
  - [ ] Background notification
  - [ ] Network interruption recovery

**Reference**: `claudedocs/SIGNALR_FLUTTER_INTEGRATION_GUIDE.md`

**Status**: 🔄 In progress (separate session)

### Web App (Optional - Future)
- [ ] Add `@microsoft/signalr` package
- [ ] Create SignalR service
- [ ] React/Angular hook implementation
- [ ] Toast notification display
- [ ] Analysis list auto-refresh

---

## ⏳ Phase 3: Production Preparation (PENDING)

### Railway Production Environment Setup

#### 3.1 WebAPI Service Variables
```bash
# Environment
ASPNETCORE_ENVIRONMENT=Production

# SignalR Notification System
WebAPI__BaseUrl=https://ziraai-api-prod.up.railway.app
WebAPI__InternalSecret=<GENERATE_SECURE_SECRET>

# Redis (if horizontal scaling)
UseRedis=false  # Set to true if using multiple instances
REDIS_URL=<redis-connection-string>  # Only if UseRedis=true
```

**Action Items**:
- [ ] Generate secure production secret (32+ characters)
  ```bash
  # PowerShell
  [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
  ```
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Set `WebAPI__BaseUrl` to production URL
- [ ] Set `WebAPI__InternalSecret` to generated secret
- [ ] Document secret in secure location (1Password/Azure Key Vault)

#### 3.2 Worker Service Variables
```bash
# Environment
ASPNETCORE_ENVIRONMENT=Production

# SignalR Notification System
WebAPI__BaseUrl=https://ziraai-api-prod.up.railway.app
WebAPI__InternalSecret=<SAME_SECRET_AS_WEBAPI>
```

**Action Items**:
- [ ] Use SAME secret as WebAPI
- [ ] Verify Worker Service and WebAPI share same secret
- [ ] Set correct production WebAPI URL

#### 3.3 CORS Verification
**File**: `WebAPI/Startup.cs`

Production domains should already be configured:
```csharp
.WithOrigins(
    "https://app.ziraai.com",
    "https://ziraai.com"
)
```

**Action Items**:
- [ ] Verify production domains in CORS policy
- [ ] Test CORS from production domain
- [ ] Ensure `AllowCredentials()` is present for SignalR

---

## 🟡 Phase 4: Horizontal Scaling (OPTIONAL)

**Required If**:
- Multiple Railway instances
- Auto-scaling enabled
- Load balancing configured

**Skip If**:
- Single Railway instance
- Low traffic expected initially

### Redis Backplane Setup

#### 4.1 Add NuGet Package
```bash
dotnet add WebAPI package Microsoft.AspNetCore.SignalR.StackExchangeRedis
```

#### 4.2 Update Startup.cs
Code already prepared in `PRODUCTION_READINESS.md` Section 4.

#### 4.3 Railway Redis Service
- [ ] Add Redis service in Railway project
- [ ] Link Redis to WebAPI service
- [ ] Set `UseRedis=true` environment variable
- [ ] Verify `REDIS_URL` is automatically provided by Railway

#### 4.4 Testing
- [ ] Deploy 2 WebAPI instances
- [ ] Connect client to instance A
- [ ] Trigger notification via instance B
- [ ] Verify client receives notification

---

## 🟢 Phase 5: Production Deployment

### Pre-Deployment Checklist

#### Code Review
- [ ] All code reviewed and approved
- [ ] PR #49 merged to staging
- [ ] Staging → Production PR created and approved
- [ ] No pending critical bugs
- [ ] All tests passing

#### Configuration Verification
- [ ] All environment variables documented
- [ ] Secrets rotated from staging values
- [ ] Production domains verified in CORS
- [ ] Database connection strings verified
- [ ] Redis connection (if using) verified

#### Documentation
- [x] Production readiness document complete
- [x] Deployment checklist created
- [ ] Runbook for operations team
- [ ] Incident response plan
- [ ] Rollback procedure documented

### Deployment Steps

#### Step 1: Deploy WebAPI to Production
```bash
# Merge to production branch
git checkout master
git merge staging --no-ff
git push origin master

# Railway auto-deploys from master branch
# Monitor deployment in Railway dashboard
```

**Verification**:
- [ ] Deployment successful (no errors)
- [ ] Health check passing: `https://api.ziraai.com/health`
- [ ] SignalR Hub accessible: `wss://api.ziraai.com/hubs/plantanalysis`
- [ ] Logs show correct environment: `Hosting environment: Production`
- [ ] Internal secret loaded: `✅ Internal secret loaded - Length: XX`

#### Step 2: Deploy Worker Service to Production
```bash
# Worker Service should auto-deploy when master branch updates
# Monitor Railway dashboard for Worker deployment
```

**Verification**:
- [ ] Deployment successful
- [ ] Worker connects to RabbitMQ successfully
- [ ] Logs show correct environment: `[WORKER] PlantAnalysisWorkerService starting in Production environment`
- [ ] Internal secret matches: `✅ Internal secret loaded - Length: XX`
- [ ] Worker can reach WebAPI: `✅ WebAPI URL loaded: https://api.ziraai.com`

#### Step 3: Test SignalR Connection
Use test client or Flutter app (when ready):

```html
<!-- Test with claudedocs/test_signalr_client.html -->
<!-- Update URL to: https://api.ziraai.com/hubs/plantanalysis -->
```

**Verification**:
- [ ] Connection establishes successfully
- [ ] Ping/pong working
- [ ] JWT authentication successful
- [ ] No CORS errors in browser console

#### Step 4: End-to-End Production Test
```bash
# Create a real async plant analysis via production API
POST https://api.ziraai.com/api/v1/plantanalyses/analyze-async

# Monitor logs for notification flow:
# 1. Worker processes analysis
# 2. Worker sends HTTP POST to WebAPI
# 3. WebAPI receives notification
# 4. WebAPI broadcasts via SignalR Hub
# 5. Client receives notification
```

**Verification**:
- [ ] Analysis created successfully
- [ ] Worker processes analysis (check logs)
- [ ] Notification sent from Worker → WebAPI (200 OK)
- [ ] SignalR Hub receives notification
- [ ] Client receives `ReceiveAnalysisCompleted` event
- [ ] Notification data correct (analysisId, userId, etc.)

---

## 📊 Phase 6: Post-Deployment Monitoring

### First 24 Hours

#### Metrics to Track
- [ ] SignalR connection count (target: stable growth)
- [ ] Notification delivery success rate (target: >95%)
- [ ] Average notification latency (target: <200ms)
- [ ] Internal API success rate (target: >99%)
- [ ] Error rate (target: <1%)

#### Logs to Monitor
```bash
# Railway logs to watch
railway logs --service webapi --tail
railway logs --service worker --tail

# Look for:
✅ SignalR connections
✅ Notification deliveries
❌ Authentication failures
❌ Connection errors
❌ Internal API failures
```

#### Alerts to Set Up
- [ ] Connection failure rate > 10% → Critical
- [ ] Notification delivery failure > 5% → Critical
- [ ] Internal API failure > 1% → Warning
- [ ] Memory usage > 80% → Warning
- [ ] CPU usage > 80% → Warning

### First Week Review

- [ ] Review connection patterns (peak hours, geographic distribution)
- [ ] Analyze notification delivery latency trends
- [ ] Check for memory leaks or resource exhaustion
- [ ] Review error patterns and fix recurring issues
- [ ] User feedback collection (via support team)
- [ ] Performance optimization opportunities identified

### Performance Benchmarks

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| Concurrent Connections | 1,000+ | TBD | ⏳ |
| Notification Latency | <200ms | TBD | ⏳ |
| Delivery Success Rate | >95% | TBD | ⏳ |
| Internal API Success | >99% | TBD | ⏳ |
| Memory per Connection | <15 KB | TBD | ⏳ |

---

## 🚨 Rollback Plan

### Trigger Conditions
- Critical failure rate > 10%
- System downtime > 5 minutes
- Data corruption detected
- Severe performance degradation

### Rollback Procedure

#### Option 1: Disable SignalR (Keep System Running)
```csharp
// Emergency: Comment out SignalR registration in Startup.cs
// services.AddSignalR();
// endpoints.MapHub<PlantAnalysisHub>("/hubs/plantanalysis");

// Redeploy WebAPI
// System continues working without real-time notifications
// Users can still poll for results
```

#### Option 2: Railway Rollback
```bash
# Rollback to previous deployment
railway rollback --service webapi
railway rollback --service worker
```

#### Option 3: Emergency Configuration
```bash
# Disable SignalR via environment variable
railway variables set ENABLE_SIGNALR=false --service webapi
```

### Post-Rollback Actions
- [ ] Notify mobile/web teams
- [ ] Update status page
- [ ] Document incident details
- [ ] Schedule post-mortem meeting
- [ ] Create follow-up tasks for fixes

---

## ✅ Success Criteria

### Technical Success
- [ ] 95%+ notification delivery rate
- [ ] <200ms average latency
- [ ] <1% error rate
- [ ] Zero critical bugs in first week
- [ ] Memory/CPU usage within acceptable limits
- [ ] No security incidents

### User Experience Success
- [ ] Users receive notifications within 5 seconds of analysis completion
- [ ] No user complaints about missing notifications
- [ ] Positive feedback from beta users
- [ ] Mobile app and web app both working correctly

### Business Success
- [ ] Increased user engagement
- [ ] Reduced support tickets about "where is my result?"
- [ ] Feature adopted by >80% of active users within first month

---

## 📞 Contact & Escalation

### Issue Categories

**Infrastructure Issues** (Railway, Redis, networking)
- First Contact: DevOps team
- Escalation: Railway support

**Code Issues** (bugs, errors, unexpected behavior)
- First Contact: Development team lead
- Escalation: Engineering manager

**Security Issues** (authentication, CORS, secrets)
- First Contact: Security team lead
- Escalation: CISO

**Performance Issues** (latency, memory, CPU)
- First Contact: DevOps team
- Escalation: Performance engineering team

### On-Call Rotation
- [ ] Set up on-call rotation schedule
- [ ] Configure PagerDuty/alerting system
- [ ] Document escalation procedures
- [ ] Conduct dry-run incident response

---

## 📝 Sign-Off

**Development Team Lead**: _________________ Date: _______

**DevOps Lead**: _________________ Date: _______

**Security Review**: _________________ Date: _______

**QA Approval**: _________________ Date: _______

**Product Owner**: _________________ Date: _______

**Approved for Production Deployment**: ☐ Yes  ☐ No  ☐ Conditional

**Conditions/Notes**:
```
Flutter client implementation must be completed and tested
before production deployment proceeds.
```

---

## 📅 Timeline & Milestones

| Milestone | Target Date | Status | Notes |
|-----------|-------------|--------|-------|
| Backend Implementation | 2025-09-30 | ✅ Complete | PR #48 merged |
| Staging Deployment | 2025-10-01 | ✅ Complete | PR #49 merged, verified |
| Flutter Client | 2025-10-XX | 🔄 In Progress | Separate session |
| Flutter Client Testing | 2025-10-XX | ⏳ Pending | After implementation |
| Production Deployment | 2025-10-XX | ⏳ Pending | After Flutter complete |
| Week 1 Review | TBD | ⏳ Pending | 7 days post-deployment |
| Month 1 Review | TBD | ⏳ Pending | 30 days post-deployment |

---

## 🔗 Related Documents

- [REALTIME_NOTIFICATION_PLAN.md](./REALTIME_NOTIFICATION_PLAN.md) - Architecture & implementation plan
- [PRODUCTION_READINESS.md](./PRODUCTION_READINESS.md) - Detailed production requirements
- [SIGNALR_FLUTTER_INTEGRATION_GUIDE.md](./SIGNALR_FLUTTER_INTEGRATION_GUIDE.md) - Flutter client implementation
- [PR #48](https://github.com/tolgakaya/ziraaiv1/pull/48) - Initial SignalR implementation
- [PR #49](https://github.com/tolgakaya/ziraaiv1/pull/49) - Environment detection fix

---

## 📊 Current Status Summary

### ✅ Ready for Production
- Backend implementation complete
- Staging deployment verified
- Configuration framework established
- Documentation complete

### ⏳ Blocking Production Deployment
- Flutter SignalR client implementation (IN PROGRESS)
- End-to-end testing with Flutter client
- Production environment variable configuration

### 🎯 Next Immediate Actions
1. Complete Flutter SignalR client (separate session - IN PROGRESS)
2. Test Flutter client with staging backend
3. Configure production Railway environment variables
4. Execute production deployment checklist
5. Monitor metrics for first 24 hours

---

**Document Version**: 1.0
**Last Updated**: 2025-10-01
**Next Review**: After Flutter client completion
