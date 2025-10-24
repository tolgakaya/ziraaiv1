# Missing Endpoints - Implementation Plan

**Created:** 2025-10-23
**Status:** Ready for Implementation

---

## Analysis Summary

### Current Status
After analyzing the codebase, here's what exists:

✅ **Controllers:** All controller methods already exist
✅ **Query Handlers:** Most query handlers exist but are incomplete
❌ **Route Mapping:** Some routes don't match test expectations

### Implementation Status by Endpoint

| # | Endpoint | Controller | Handler | Status | Issue |
|---|----------|------------|---------|--------|-------|
| 1 | GET /api/admin/analytics/user-statistics | ❌ | ✅ | Route mismatch | Expects `/user-statistics`, has `/users` |
| 2 | GET /api/admin/analytics/subscription-statistics | ❌ | ✅ | Route mismatch | Expects `/subscription-statistics`, has `/subscriptions` |
| 3 | GET /api/admin/analytics/dashboard-overview | ❌ | ✅ | Route mismatch | Expects `/dashboard-overview`, has `/dashboard` |
| 4 | GET /api/admin/analytics/activity-logs | ❌ | ❌ | Not implemented | No handler exists |
| 5 | GET /api/admin/sponsorship/statistics | ❌ | ✅ | Not in controller | Handler in Analytics controller |
| 6 | GET /api/admin/sponsorship/sponsors/{id}/detailed-report | ✅ | ✅ | Route mismatch | Expects `/detailed-report`, has `/report` |
| 7 | GET /api/admin/plant-analysis/on-behalf-of | ⚠️ | ❌ | Placeholder | Returns redirect message |

---

## Implementation Tasks

### Phase 1: Route Fixes (15 minutes)

#### Task 1.1: Fix Analytics Routes
**File:** `WebAPI/Controllers/AdminAnalyticsController.cs`

**Changes:**
```csharp
// BEFORE
[HttpGet("users")]
public async Task<IActionResult> GetUserStatistics(...)

[HttpGet("subscriptions")]
public async Task<IActionResult> GetSubscriptionStatistics(...)

[HttpGet("dashboard")]
public async Task<IActionResult> GetDashboardOverview()

// AFTER
[HttpGet("user-statistics")]
public async Task<IActionResult> GetUserStatistics(...)

[HttpGet("subscription-statistics")]
public async Task<IActionResult> GetSubscriptionStatistics(...)

[HttpGet("dashboard-overview")]
public async Task<IActionResult> GetDashboardOverview()
```

#### Task 1.2: Fix Sponsor Report Route
**File:** `WebAPI/Controllers/AdminSponsorshipController.cs`

**Changes:**
```csharp
// BEFORE
[HttpGet("sponsor/{sponsorId}/report")]
public async Task<IActionResult> GetSponsorDetailedReport(int sponsorId)

// AFTER
[HttpGet("sponsors/{sponsorId}/detailed-report")]
public async Task<IActionResult> GetSponsorDetailedReport(int sponsorId)
```

#### Task 1.3: Add Sponsorship Statistics Endpoint
**File:** `WebAPI/Controllers/AdminSponsorshipController.cs`

**Add after GetPurchaseById:**
```csharp
/// <summary>
/// Get sponsorship statistics and metrics
/// </summary>
[HttpGet("statistics")]
public async Task<IActionResult> GetStatistics(
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null)
{
    var query = new GetSponsorshipStatisticsQuery
    {
        StartDate = startDate,
        EndDate = endDate
    };

    var result = await Mediator.Send(query);
    return GetResponse(result);
}
```

---

### Phase 2: Complete Handler Implementations (2-3 hours)

#### Task 2.1: Complete GetUserStatisticsQuery
**File:** `Business/Handlers/AdminAnalytics/Queries/GetUserStatisticsQuery.cs`

**Issue:** Role-based counts are set to 0 with TODO comments

**Solution:** Add UserOperationClaim repository and implement role counts

```csharp
private readonly IUserRepository _userRepository;
private readonly IUserOperationClaimRepository _userOperationClaimRepository;

public GetUserStatisticsQueryHandler(
    IUserRepository userRepository,
    IUserOperationClaimRepository userOperationClaimRepository)
{
    _userRepository = userRepository;
    _userOperationClaimRepository = userOperationClaimRepository;
}

// In Handle method:
var operationClaims = _userOperationClaimRepository.Query().ToList();
var farmerClaim = operationClaims.FirstOrDefault(c => c.ClaimId == /* Farmer claim ID */);
var sponsorClaim = operationClaims.FirstOrDefault(c => c.ClaimId == /* Sponsor claim ID */);
var adminClaim = operationClaims.FirstOrDefault(c => c.ClaimId == /* Admin claim ID */);

stats.FarmerUsers = operationClaims.Count(oc => oc.ClaimId == farmerClaim?.ClaimId);
stats.SponsorUsers = operationClaims.Count(oc => oc.ClaimId == sponsorClaim?.ClaimId);
stats.AdminUsers = operationClaims.Count(oc => oc.ClaimId == adminClaim?.ClaimId);
```

**Alternative (Simpler):** Query by role name from UserOperationClaim table
```csharp
var userClaims = _userOperationClaimRepository.GetList().Result;

stats.AdminUsers = userClaims.Count(uc => uc.OperationClaim.Name == "Admin");
stats.FarmerUsers = allUsers.Count() - stats.AdminUsers - stats.SponsorUsers; // Approximation
stats.SponsorUsers = 0; // TODO: Add proper sponsor identification
```

#### Task 2.2: Complete GetSubscriptionStatisticsQuery
**File:** `Business/Handlers/AdminAnalytics/Queries/GetSubscriptionStatisticsQuery.cs`

**Check current implementation and enhance if needed**

#### Task 2.3: Complete GetSponsorshipStatisticsQuery
**File:** `Business/Handlers/AdminAnalytics/Queries/GetSponsorshipStatisticsQuery.cs`

**Check current implementation and enhance if needed**

#### Task 2.4: Complete GetSponsorDetailedReportQuery
**File:** Search for `GetSponsorDetailedReportQuery.cs`

**Verify implementation includes:**
- Sponsor user details
- Purchase history
- Code generation and usage stats
- Farmer reach and impact metrics

---

### Phase 3: Implement Activity Logs Endpoint (1-2 hours)

#### Task 3.1: Create ActivityLog Query
**File:** `Business/Handlers/AdminAnalytics/Queries/GetActivityLogsQuery.cs`

**Create new file:**
```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos;
using MediatR;

namespace Business.Handlers.AdminAnalytics.Queries
{
    public class GetActivityLogsQuery : IRequest<IDataResult<ActivityLogsDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? UserId { get; set; }
        public string ActionType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public class GetActivityLogsQueryHandler : IRequestHandler<GetActivityLogsQuery, IDataResult<ActivityLogsDto>>
        {
            private readonly IAdminOperationLogRepository _logRepository;

            public GetActivityLogsQueryHandler(IAdminOperationLogRepository logRepository)
            {
                _logRepository = logRepository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<ActivityLogsDto>> Handle(GetActivityLogsQuery request, CancellationToken cancellationToken)
            {
                var query = _logRepository.Query();

                // Apply filters
                if (request.UserId.HasValue)
                {
                    query = query.Where(l => l.AdminUserId == request.UserId.Value || l.TargetUserId == request.UserId.Value);
                }

                if (!string.IsNullOrEmpty(request.ActionType))
                {
                    query = query.Where(l => l.Action == request.ActionType);
                }

                if (request.StartDate.HasValue)
                {
                    query = query.Where(l => l.Timestamp >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(l => l.Timestamp <= request.EndDate.Value);
                }

                var totalCount = query.Count();
                var logs = query
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var result = new ActivityLogsDto
                {
                    Logs = logs,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalCount = totalCount
                };

                return new SuccessDataResult<ActivityLogsDto>(result, "Activity logs retrieved successfully");
            }
        }
    }

    public class ActivityLogsDto
    {
        public List<AdminOperationLog> Logs { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
```

#### Task 3.2: Add Controller Method
**File:** `WebAPI/Controllers/AdminAnalyticsController.cs`

**Add method:**
```csharp
/// <summary>
/// Get system activity logs
/// </summary>
[HttpGet("activity-logs")]
public async Task<IActionResult> GetActivityLogs(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] int? userId = null,
    [FromQuery] string actionType = null,
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null)
{
    var query = new GetActivityLogsQuery
    {
        Page = page,
        PageSize = pageSize,
        UserId = userId,
        ActionType = actionType,
        StartDate = startDate,
        EndDate = endDate
    };

    var result = await Mediator.Send(query);
    return GetResponse(result);
}
```

---

### Phase 4: Fix OBO Plant Analysis Endpoint (30 minutes)

#### Task 4.1: Implement Proper OBO List
**File:** `WebAPI/Controllers/AdminPlantAnalysisController.cs`

**Replace placeholder:**
```csharp
// BEFORE (placeholder)
[HttpGet("on-behalf-of")]
public IActionResult GetAllOBOAnalyses()
{
    return Ok(new
    {
        Success = true,
        Message = "Use audit logs to view all OBO operations: GET /api/admin/audit/on-behalf-of"
    });
}

// AFTER (proper implementation)
/// <summary>
/// Get all plant analyses created on behalf of users
/// </summary>
[HttpGet("on-behalf-of")]
public async Task<IActionResult> GetAllOBOAnalyses(
    [FromQuery] int? adminUserId = null,
    [FromQuery] int? targetUserId = null,
    [FromQuery] string status = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
{
    var query = new GetAllOBOAnalysesQuery
    {
        AdminUserId = adminUserId,
        TargetUserId = targetUserId,
        Status = status,
        Page = page,
        PageSize = pageSize
    };

    var result = await Mediator.Send(query);
    return GetResponse(result);
}
```

#### Task 4.2: Create Query Handler
**File:** `Business/Handlers/AdminPlantAnalysis/Queries/GetAllOBOAnalysesQuery.cs`

**Create new file:**
```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.AdminPlantAnalysis.Queries
{
    public class GetAllOBOAnalysesQuery : IRequest<IDataResult<OBOAnalysesDto>>
    {
        public int? AdminUserId { get; set; }
        public int? TargetUserId { get; set; }
        public string Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public class GetAllOBOAnalysesQueryHandler : IRequestHandler<GetAllOBOAnalysesQuery, IDataResult<OBOAnalysesDto>>
        {
            private readonly IPlantAnalysisRepository _repository;

            public GetAllOBOAnalysesQueryHandler(IPlantAnalysisRepository repository)
            {
                _repository = repository;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))];
            public async Task<IDataResult<OBOAnalysesDto>> Handle(GetAllOBOAnalysesQuery request, CancellationToken cancellationToken)
            {
                var query = _repository.Query().Where(a => a.IsOnBehalfOf == true);

                // Apply filters
                if (request.AdminUserId.HasValue)
                {
                    query = query.Where(a => a.CreatedByAdminId == request.AdminUserId.Value);
                }

                if (request.TargetUserId.HasValue)
                {
                    query = query.Where(a => a.UserId == request.TargetUserId.Value);
                }

                if (!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(a => a.AnalysisStatus == request.Status);
                }

                var totalCount = query.Count();
                var analyses = query
                    .OrderByDescending(a => a.CreatedDate)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var result = new OBOAnalysesDto
                {
                    Analyses = analyses,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalCount = totalCount
                };

                return new SuccessDataResult<OBOAnalysesDto>(result, "OBO analyses retrieved successfully");
            }
        }
    }

    public class OBOAnalysesDto
    {
        public List<PlantAnalysis> Analyses { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
```

---

## Implementation Order

### Priority 1 (Critical - 15-30 minutes)
1. Fix analytics route names (Task 1.1)
2. Fix sponsor report route (Task 1.2)
3. Add sponsorship statistics endpoint (Task 1.3)

### Priority 2 (High - 1-2 hours)
4. Complete user statistics role counts (Task 2.1)
5. Verify subscription statistics (Task 2.2)
6. Verify sponsorship statistics (Task 2.3)
7. Verify sponsor detailed report (Task 2.4)

### Priority 3 (Medium - 1-2 hours)
8. Implement activity logs query (Task 3.1)
9. Add activity logs endpoint (Task 3.2)

### Priority 4 (Low - 30 minutes)
10. Implement OBO analyses list (Task 4.1)
11. Create OBO query handler (Task 4.2)

---

## Estimated Timeline

- **Route Fixes:** 15 minutes
- **Handler Completion:** 2-3 hours
- **Activity Logs:** 1-2 hours
- **OBO List:** 30 minutes

**Total:** 4-6 hours

---

## Testing Strategy

After implementation:
1. Run all existing tests (should pass)
2. Test newly fixed endpoints:
   - User statistics with role counts
   - Subscription statistics
   - Sponsorship statistics
   - Sponsor detailed report
   - Dashboard overview
   - Activity logs
   - OBO analyses list

3. Update test documentation with actual responses

---

## Success Criteria

✅ All 8 missing endpoints return 200 OK
✅ All data fields populated correctly
✅ Pagination working for list endpoints
✅ Filters working correctly
✅ No TODO comments in handlers
✅ All tests pass

---

**Created By:** Claude Code
**Date:** 2025-10-23
**Status:** Ready for implementation
