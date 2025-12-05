# Cache Implementation Master Plan

**Branch**: `feature/production-readiness`
**Deployment**: Auto-deploy to Staging (Railway)
**Started**: 2025-12-05
**Last Updated**: 2025-12-05

---

## 📋 Critical Development Rules (MUST FOLLOW)

### 1. Branch & Deployment
- ✅ Work ONLY in `feature/production-readiness` branch
- ✅ Auto-deploy to Staging after each push
- ✅ Never merge to master without explicit approval

### 2. Build Validation
- ✅ Run `dotnet build` after EVERY meaningful change
- ✅ Fix compilation errors immediately
- ✅ No commits with build errors

### 3. Database Migrations
- ✅ NO Entity Framework migrations
- ✅ ALL database changes via SQL scripts only
- ✅ Place SQL scripts in `claudedocs/cache/migrations/`

### 4. Documentation
- ✅ All cache documentation in `claudedocs/cache/`
- ✅ All cache-related files (plans, API docs, migrations) in this directory

### 5. SecuredOperation Guidelines
- ✅ Study `SponsorAnalyticsController.cs` for reference
- ✅ Study `SECUREDOPERATION_GUIDE.md` for details
- ✅ Use claims from `claudedocs/AdminOperations/operation_claims.csv`
- ✅ Create SQL scripts for new claims
- ✅ Assign claims to appropriate groups (Admin=1, Sponsor=3)

### 6. Backwards Compatibility
- ✅ Never break existing features
- ✅ Test existing functionality after changes
- ✅ Verify dual-role users (e.g., Sponsor+Dealer) work correctly

### 7. Backend Focus
- ✅ Backend development ONLY
- ✅ No UI/Frontend work
- ✅ Document all endpoints for Frontend/Mobile teams

### 8. API Documentation
- ✅ Create API docs for each completed endpoint
- ✅ Include: endpoint URL, method, parameters, payload, response structure
- ✅ Explain business purpose and use case

### 9. API Versioning
- ✅ Use `/api/v1/` for farmer endpoints
- ✅ Use `/api/admin/` for admin endpoints (no version)
- ✅ Follow existing patterns

### 10. Configuration Management
- ✅ Study existing config implementation before creating new configs
- ✅ Use Railway environment variables
- ✅ Reference: Storage service configuration implementation

### 11. Session Continuity
- ✅ Update this document after each phase completion
- ✅ Use this document for context recovery
- ✅ Track progress in detailed task lists below

### 12. Existing Cache Infrastructure
- ✅ Redis already configured (RedisCacheManager)
- ✅ ICacheManager interface available
- ✅ Example: SponsorDealerAnalyticsCacheService (reference implementation)

---

## 🎯 Implementation Strategy

### Phase Organization

**Total Phases**: 5 phases
**Approach**: Incremental implementation with validation gates
**Priority**: High-impact, low-risk queries first

### Success Metrics
- Response time improvement: 60-80% reduction
- Cache hit ratio: 80-95%
- Zero breaking changes to existing functionality
- Build succeeds after each phase

---

## 📊 Phase 1: Cache Infrastructure Setup

**Status**: ✅ COMPLETED
**Priority**: 🔴 CRITICAL
**Completion Date**: 2025-12-05

### Objectives
1. Create base cache service infrastructure
2. Implement cache invalidation service with Redis Pub/Sub
3. Add monitoring and logging utilities
4. Create configuration management

### Tasks

#### Task 1.1: Create ICacheInvalidationService Interface
- [ ] Create `Business/Services/Cache/ICacheInvalidationService.cs`
- [ ] Define methods for all invalidation patterns:
  - [ ] `InvalidateDealerDashboardAsync(int dealerId)`
  - [ ] `InvalidateSponsorDashboardAsync(int sponsorId)`
  - [ ] `InvalidateAdminStatisticsAsync()`
  - [ ] `InvalidateSubscriptionTierAsync(int? tierId = null)`
  - [ ] `InvalidateConfigurationAsync(string key)`
  - [ ] `InvalidateAnalyticsAsync(int sponsorId)`
- [ ] Add Redis Pub/Sub broadcast methods
- [ ] Build and verify (no errors)

#### Task 1.2: Implement CacheInvalidationService
- [ ] Create `Business/Services/Cache/CacheInvalidationService.cs`
- [ ] Inject `ICacheManager` and `ILogger`
- [ ] Implement all invalidation methods
- [ ] Add Redis Pub/Sub for distributed invalidation
- [ ] Add comprehensive logging
- [ ] Build and verify (no errors)

#### Task 1.3: Register Services in DI Container
- [ ] Open `Business/DependencyResolvers/AutofacBusinessModule.cs`
- [ ] Register `ICacheInvalidationService` → `CacheInvalidationService` (Singleton)
- [ ] Build and verify (no errors)

#### Task 1.4: Create Cache Constants
- [ ] Create `Entities/Constants/CacheKeys.cs`
- [ ] Define all cache key patterns:
  ```csharp
  public static class CacheKeys
  {
      // Dashboard caches
      public const string DealerDashboard = "dealer:dashboard:{0}";
      public const string SponsorDashboard = "sponsor:dashboard:{0}";
      public const string AdminUserStats = "admin:stats:users";

      // Reference data caches
      public const string SubscriptionTiers = "subscription:tiers";
      public const string Configuration = "config:{0}";

      // Analytics caches
      public const string SponsorAnalytics = "sponsor:analytics:{0}";
  }
  ```
- [ ] Build and verify (no errors)

#### Task 1.5: Create Cache Duration Configuration
- [ ] Add to `Entities/Constants/ConfigurationKeys.cs`:
  ```csharp
  public static class Cache
  {
      public const string DashboardCacheDuration = "CACHE_DASHBOARD_DURATION_MINUTES";
      public const string StatisticsCacheDuration = "CACHE_STATISTICS_DURATION_MINUTES";
      public const string ReferenceDataCacheDuration = "CACHE_REFERENCE_DATA_DURATION_MINUTES";
  }
  ```
- [ ] Build and verify (no errors)

#### Task 1.6: Create SQL Migration for Cache Configuration
- [ ] Create `claudedocs/cache/migrations/001_cache_configuration.sql`
- [ ] Add cache duration configurations:
  ```sql
  INSERT INTO "Configurations" ("Key", "Value", "Category", "Description")
  SELECT 'CACHE_DASHBOARD_DURATION_MINUTES', '15', 'Cache', 'Dashboard cache TTL in minutes'
  WHERE NOT EXISTS (SELECT 1 FROM "Configurations" WHERE "Key" = 'CACHE_DASHBOARD_DURATION_MINUTES');

  INSERT INTO "Configurations" ("Key", "Value", "Category", "Description")
  SELECT 'CACHE_STATISTICS_DURATION_MINUTES', '60', 'Cache', 'Statistics cache TTL in minutes'
  WHERE NOT EXISTS (SELECT 1 FROM "Configurations" WHERE "Key" = 'CACHE_STATISTICS_DURATION_MINUTES');

  INSERT INTO "Configurations" ("Key", "Value", "Category", "Description")
  SELECT 'CACHE_REFERENCE_DATA_DURATION_MINUTES', '1440', 'Cache', 'Reference data cache TTL (1 day)'
  WHERE NOT EXISTS (SELECT 1 FROM "Configurations" WHERE "Key" = 'CACHE_REFERENCE_DATA_DURATION_MINUTES');
  ```
- [ ] Add verification queries
- [ ] Document migration in this file

#### Task 1.7: Build and Commit Phase 1
- [ ] Run `dotnet build` (verify no errors)
- [ ] Commit: "feat: Add cache infrastructure - invalidation service, constants, configuration"
- [ ] Push to `feature/production-readiness`
- [ ] Verify Staging deployment

### Completion Criteria
- ✅ All cache infrastructure services created
- ✅ DI registration complete
- ✅ Cache keys and durations defined
- ✅ Build succeeds with no errors
- ✅ Code committed and pushed

---

## 📊 Phase 2: Dealer Dashboard Cache Implementation

**Status**: ⏳ PENDING (Blocked by Phase 1)
**Priority**: 🔴 HIGH
**Estimated Time**: 3-4 hours

### Objectives
1. Implement caching for `GetDealerDashboardSummaryQuery`
2. Add cache invalidation triggers
3. Create cache warming strategy
4. Add monitoring and metrics

### Reference Documents
- Source: `claudedocs/cache/CACHE_PERFORMANCE_OPTIMIZATION_ANALYSIS.md` (Section 2.1)
- Invalidation: `claudedocs/cache/CACHE_INVALIDATION_STRATEGY.md` (Section 2.1)

### Current Implementation
**File**: `Business/Handlers/Sponsorship/Queries/GetDealerDashboardSummaryQuery.cs`
**Lines**: 20-129
**Current Performance**: 500-1200ms (7 DB queries)
**Target Performance**: 10-30ms (cache hit)

### Tasks

#### Task 2.1: Create Cached Dealer Dashboard Service
- [ ] Create `Business/Services/Sponsorship/IDealerDashboardCacheService.cs`
- [ ] Define methods:
  ```csharp
  Task<DealerDashboardSummaryDto> GetDashboardSummaryAsync(int dealerId);
  Task InvalidateDashboardAsync(int dealerId);
  Task WarmCacheAsync(int dealerId);
  ```
- [ ] Build and verify

#### Task 2.2: Implement Dealer Dashboard Cache Service
- [ ] Create `Business/Services/Sponsorship/DealerDashboardCacheService.cs`
- [ ] Inject dependencies: `ICacheManager`, `ISponsorshipCodeRepository`, `IDealerInvitationRepository`, `IConfigurationService`
- [ ] Implement `GetDashboardSummaryAsync`:
  - [ ] Check cache first (`dealer:dashboard:{dealerId}`)
  - [ ] If miss: query database, cache result with TTL from config
  - [ ] Return cached data
- [ ] Implement `InvalidateDashboardAsync`:
  - [ ] Remove cache key `dealer:dashboard:{dealerId}`
  - [ ] Log invalidation
- [ ] Implement `WarmCacheAsync`:
  - [ ] Pre-load dashboard data
  - [ ] Cache with TTL
- [ ] Add comprehensive logging
- [ ] Build and verify

#### Task 2.3: Register Service in DI
- [ ] Update `Business/DependencyResolvers/AutofacBusinessModule.cs`
- [ ] Register `IDealerDashboardCacheService` → `DealerDashboardCacheService` (Scoped)
- [ ] Build and verify

#### Task 2.4: Update GetDealerDashboardSummaryQuery Handler
- [ ] Inject `IDealerDashboardCacheService`
- [ ] Replace direct repository queries with cache service call
- [ ] Keep all existing validation logic
- [ ] Build and verify

#### Task 2.5: Add Cache Invalidation Triggers

**Command Handlers to Update**:

##### TransferCodesToDealerCommand
- [ ] Open `Business/Handlers/Sponsorship/Commands/TransferCodesToDealerCommand.cs`
- [ ] Inject `ICacheInvalidationService`
- [ ] After successful transfer, call:
  ```csharp
  await _cacheInvalidationService.InvalidateDealerDashboardAsync(request.DealerId);
  ```
- [ ] Build and verify

##### AcceptDealerInvitationCommand
- [ ] Open `Business/Handlers/Sponsorship/Commands/AcceptDealerInvitationCommand.cs`
- [ ] Inject `ICacheInvalidationService`
- [ ] After invitation acceptance, invalidate dealer dashboard
- [ ] Build and verify

##### ReclaimDealerCodesCommand
- [ ] Open handler file
- [ ] Inject `ICacheInvalidationService`
- [ ] After code reclaim, invalidate dealer dashboard
- [ ] Build and verify

##### DistributeSponsorshipCodeCommand (Dealer distributes to farmer)
- [ ] Locate handler
- [ ] Inject `ICacheInvalidationService`
- [ ] After code distribution, invalidate dealer dashboard
- [ ] Build and verify

#### Task 2.6: Build and Test Phase 2
- [ ] Run `dotnet build` (verify no errors)
- [ ] Manual test checklist:
  - [ ] Verify dashboard loads (cache miss → DB query)
  - [ ] Verify second load is fast (cache hit)
  - [ ] Transfer codes to dealer → verify cache invalidated
  - [ ] Verify dashboard reloads fresh data
- [ ] Document test results

#### Task 2.7: Commit Phase 2
- [ ] Commit: "feat: Implement dealer dashboard caching with invalidation triggers"
- [ ] Push to `feature/production-readiness`
- [ ] Verify Staging deployment
- [ ] Update this document with completion status

### Completion Criteria
- ✅ Cache service implemented and registered
- ✅ Query handler uses cache service
- ✅ All invalidation triggers added
- ✅ Build succeeds
- ✅ Manual tests pass
- ✅ Code committed and deployed

---

## 📊 Phase 3: Admin Statistics Cache Implementation

**Status**: ⏳ PENDING (Blocked by Phase 2)
**Priority**: 🔴 HIGH
**Estimated Time**: 4-5 hours

### Objectives
1. Cache `GetUserStatisticsQuery`
2. Cache `GetSubscriptionStatisticsQuery`
3. Cache `GetSponsorshipStatisticsQuery`
4. Implement invalidation triggers
5. Add admin cache rebuild endpoint

### Reference Documents
- Source: `claudedocs/cache/CACHE_PERFORMANCE_OPTIMIZATION_ANALYSIS.md` (Section 2.2)
- Invalidation: `claudedocs/cache/CACHE_INVALIDATION_STRATEGY.md` (Section 2.2)

### Current Performance
- **User Statistics**: 800-2000ms → Target: 20-50ms
- **Subscription Statistics**: 600-1500ms → Target: 15-40ms
- **Sponsorship Statistics**: 500-1200ms → Target: 10-30ms

### Tasks

#### Task 3.1: Create Admin Statistics Cache Service
- [ ] Create `Business/Services/AdminAnalytics/IAdminStatisticsCacheService.cs`
- [ ] Define methods:
  ```csharp
  Task<UserStatisticsDto> GetUserStatisticsAsync(DateTime? startDate, DateTime? endDate);
  Task<SubscriptionStatisticsDto> GetSubscriptionStatisticsAsync(DateTime? startDate, DateTime? endDate);
  Task<object> GetSponsorshipStatisticsAsync(int sponsorId);
  Task InvalidateAllStatisticsAsync();
  Task RebuildAllCachesAsync();
  ```
- [ ] Build and verify

#### Task 3.2: Implement Admin Statistics Cache Service
- [ ] Create `Business/Services/AdminAnalytics/AdminStatisticsCacheService.cs`
- [ ] Inject: `ICacheManager`, repositories, `IConfigurationService`
- [ ] Implement caching logic for each statistics type
- [ ] Use cache keys: `admin:stats:users:{hash}`, `admin:stats:subscriptions:{hash}`, `admin:stats:sponsorship:{sponsorId}`
- [ ] Hash date ranges for cache keys
- [ ] Build and verify

#### Task 3.3: Register Service in DI
- [ ] Update `AutofacBusinessModule.cs`
- [ ] Register `IAdminStatisticsCacheService` → `AdminStatisticsCacheService`
- [ ] Build and verify

#### Task 3.4: Update Statistics Query Handlers

##### GetUserStatisticsQuery
- [ ] Open handler file
- [ ] Inject `IAdminStatisticsCacheService`
- [ ] Replace direct queries with cache service
- [ ] Keep SecuredOperation and aspects
- [ ] Build and verify

##### GetSubscriptionStatisticsQuery
- [ ] Open `Business/Handlers/AdminAnalytics/Queries/GetSubscriptionStatisticsQuery.cs`
- [ ] Inject cache service
- [ ] Use cache for statistics
- [ ] Build and verify

##### GetSponsorshipStatisticsQuery
- [ ] Open `Business/Handlers/Sponsorship/Queries/GetSponsorshipStatisticsQuery.cs`
- [ ] Inject cache service
- [ ] Use cache for statistics
- [ ] Build and verify

#### Task 3.5: Add Cache Invalidation Triggers

**Critical Commands** (Invalidate ALL statistics):

##### RegisterUserCommand
- [ ] Inject `ICacheInvalidationService`
- [ ] After user registration: `await _cacheInvalidationService.InvalidateAdminStatisticsAsync();`
- [ ] Build and verify

##### AssignSubscriptionCommand
- [ ] Inject `ICacheInvalidationService`
- [ ] After subscription assignment: invalidate statistics
- [ ] Build and verify

##### PurchaseBulkSponsorshipCommand
- [ ] Inject `ICacheInvalidationService`
- [ ] After purchase: invalidate statistics
- [ ] Build and verify

##### CancelSubscriptionCommand
- [ ] Inject invalidation service
- [ ] After cancellation: invalidate statistics
- [ ] Build and verify

#### Task 3.6: Create Admin Cache Management Endpoint

##### Create Operation Claim
- [ ] Create SQL migration: `002_admin_cache_management_claims.sql`
  ```sql
  INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
  VALUES (200, 'RebuildAdminCacheCommand', 'admin.cache.rebuild', 'Rebuild admin statistics cache')
  ON CONFLICT ("Id") DO NOTHING;

  INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
  VALUES (1, 200)
  ON CONFLICT DO NOTHING;
  ```
- [ ] Document claim ID (200) in this plan

##### Create Handler
- [ ] Create `Business/Handlers/AdminAnalytics/Commands/RebuildAdminCacheCommand.cs`
- [ ] Inject `IAdminStatisticsCacheService`
- [ ] Add `[SecuredOperation(Priority = 1)]`
- [ ] Add `[PerformanceAspect(5)]`
- [ ] Add `[LogAspect(typeof(FileLogger))]`
- [ ] Implement: call `RebuildAllCachesAsync()`
- [ ] Build and verify

##### Create Controller Endpoint
- [ ] Open or create `WebAPI/Controllers/AdminAnalyticsController.cs`
- [ ] Add endpoint: `POST /api/admin/analytics/rebuild-cache`
- [ ] No API versioning (admin endpoint)
- [ ] Build and verify

#### Task 3.7: Build and Test Phase 3
- [ ] Run `dotnet build` (verify no errors)
- [ ] Manual test:
  - [ ] Load user statistics (cache miss)
  - [ ] Load again (cache hit, fast)
  - [ ] Register new user → verify cache invalidated
  - [ ] Rebuild cache endpoint works
- [ ] Document test results

#### Task 3.8: Create API Documentation
- [ ] Create `claudedocs/cache/api/admin-statistics-cache-api.md`
- [ ] Document all endpoints:
  - [ ] GET `/api/admin/analytics/user-statistics`
  - [ ] GET `/api/admin/analytics/subscription-statistics`
  - [ ] GET `/api/admin/analytics/sponsorship-statistics`
  - [ ] POST `/api/admin/analytics/rebuild-cache`
- [ ] Include request/response examples
- [ ] Explain business purpose

#### Task 3.9: Commit Phase 3
- [ ] Commit: "feat: Implement admin statistics caching with auto-invalidation"
- [ ] Push to `feature/production-readiness`
- [ ] Verify Staging deployment
- [ ] Update this document

### Completion Criteria
- ✅ All statistics queries cached
- ✅ Invalidation triggers working
- ✅ Admin rebuild endpoint functional
- ✅ Build succeeds
- ✅ Tests pass
- ✅ API documentation complete

---

## 📊 Phase 4: Sponsor Dashboard & Analytics Cache

**Status**: ⏳ PENDING (Blocked by Phase 3)
**Priority**: 🟡 MEDIUM
**Estimated Time**: 3-4 hours

### Objectives
1. Cache sponsor dashboard data
2. Cache sponsor analytics (already partially implemented in `SponsorDealerAnalyticsCacheService`)
3. Enhance existing analytics cache
4. Implement invalidation triggers

### Reference Documents
- Source: `claudedocs/cache/CACHE_PERFORMANCE_OPTIMIZATION_ANALYSIS.md` (Section 2.3)
- Invalidation: `claudedocs/cache/CACHE_INVALIDATION_STRATEGY.md` (Section 2.3)

### Tasks

#### Task 4.1: Enhance Sponsor Analytics Cache
- [ ] Review existing `SponsorDealerAnalyticsCacheService`
- [ ] Add missing invalidation triggers
- [ ] Integrate with `ICacheInvalidationService`
- [ ] Build and verify

#### Task 4.2: Add Invalidation Triggers

##### PurchaseBulkSponsorshipCommand
- [ ] Inject `ICacheInvalidationService`
- [ ] After purchase: `InvalidateSponsorDashboardAsync(sponsorId)`
- [ ] Build and verify

##### RedeemSponsorshipCodeCommand
- [ ] Inject invalidation service
- [ ] After redemption: invalidate sponsor analytics
- [ ] Build and verify

##### SendMessageToFarmerCommand
- [ ] Inject invalidation service
- [ ] After message sent: invalidate sponsor messaging analytics
- [ ] Build and verify

#### Task 4.3: Build and Test Phase 4
- [ ] Run `dotnet build`
- [ ] Test sponsor analytics caching
- [ ] Verify invalidation works
- [ ] Document results

#### Task 4.4: Commit Phase 4
- [ ] Commit: "feat: Enhance sponsor analytics caching with comprehensive invalidation"
- [ ] Push and deploy
- [ ] Update this document

### Completion Criteria
- ✅ Sponsor analytics enhanced
- ✅ Invalidation triggers added
- ✅ Build succeeds
- ✅ Tests pass

---

## 📊 Phase 5: Reference Data Cache (Tiers & Configuration)

**Status**: ⏳ PENDING (Blocked by Phase 4)
**Priority**: 🟢 LOW
**Estimated Time**: 2-3 hours

### Objectives
1. Cache subscription tiers
2. Optimize configuration service caching
3. Implement broadcast invalidation for distributed systems

### Reference Documents
- Source: `claudedocs/cache/CACHE_PERFORMANCE_OPTIMIZATION_ANALYSIS.md` (Section 2.4)
- Invalidation: `claudedocs/cache/CACHE_INVALIDATION_STRATEGY.md` (Section 2.4)

### Tasks

#### Task 5.1: Cache Subscription Tiers
- [ ] Create `ISubscriptionTierCacheService`
- [ ] Implement tier caching
- [ ] Cache for 24 hours (rarely changes)
- [ ] Build and verify

#### Task 5.2: Add Invalidation for UpdateSubscriptionTierCommand
- [ ] Inject `ICacheInvalidationService`
- [ ] Broadcast invalidation (Redis Pub/Sub)
- [ ] Build and verify

#### Task 5.3: Enhance Configuration Service Caching
- [ ] Review existing configuration caching
- [ ] Add broadcast invalidation for UpdateConfigurationCommand
- [ ] Build and verify

#### Task 5.4: Build and Test Phase 5
- [ ] Run `dotnet build`
- [ ] Test tier caching
- [ ] Test configuration invalidation
- [ ] Document results

#### Task 5.5: Create Final API Documentation
- [ ] Document all cache-related endpoints
- [ ] Create performance comparison report
- [ ] Document cache hit ratios

#### Task 5.6: Commit Phase 5
- [ ] Commit: "feat: Implement reference data caching with broadcast invalidation"
- [ ] Push and deploy
- [ ] Update this document

### Completion Criteria
- ✅ All reference data cached
- ✅ Broadcast invalidation working
- ✅ Build succeeds
- ✅ Documentation complete

---

## 📈 Success Metrics & Monitoring

### Performance Targets
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Dealer Dashboard | 500-1200ms | 10-30ms | 95% |
| User Statistics | 800-2000ms | 20-50ms | 97% |
| Subscription Stats | 600-1500ms | 15-40ms | 96% |
| Sponsor Analytics | 400-1000ms | 5-15ms | 98% |

### Cache Hit Ratio Targets
- Dashboard queries: 85-95%
- Statistics queries: 80-90%
- Reference data: 95-99%

### Monitoring Plan
- [ ] Add cache hit/miss logging
- [ ] Track invalidation frequency
- [ ] Monitor cache memory usage
- [ ] Track response times

---

## 🚀 Deployment Strategy

### Staging Deployment
- Auto-deploy after each push to `feature/production-readiness`
- Manual testing required before next phase
- Rollback plan: revert last commit

### Production Deployment
- Only after ALL phases complete
- Requires explicit approval
- Merge to master via PR
- Monitor metrics for 24 hours

---

## 📝 Progress Tracking

### Overall Progress
- [ ] Phase 1: Cache Infrastructure (0%)
- [ ] Phase 2: Dealer Dashboard (0%)
- [ ] Phase 3: Admin Statistics (0%)
- [ ] Phase 4: Sponsor Analytics (0%)
- [ ] Phase 5: Reference Data (0%)

### Next Steps
1. Start Phase 1: Create cache infrastructure
2. Build and test each component
3. Commit and deploy incrementally

---

## 📚 Reference Files

### Code References
- `Core/CrossCuttingConcerns/Caching/Redis/RedisCacheManager.cs` - Redis cache implementation
- `Business/Services/Analytics/SponsorDealerAnalyticsCacheService.cs` - Example cache service
- `WebAPI/Controllers/SponsorAnalyticsController.cs` - Example cached controller
- `claudedocs/SECUREDOPERATION_GUIDE.md` - Authorization guide
- `claudedocs/AdminOperations/operation_claims.csv` - Existing claims

### Documentation References
- `claudedocs/cache/CACHE_PERFORMANCE_OPTIMIZATION_ANALYSIS.md` - Performance analysis
- `claudedocs/cache/CACHE_INVALIDATION_STRATEGY.md` - Invalidation strategy

### SQL Migrations
- Location: `claudedocs/cache/migrations/`
- Naming: `{sequence}_{description}.sql`

---

**END OF MASTER PLAN**
