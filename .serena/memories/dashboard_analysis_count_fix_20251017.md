# Dashboard Analysis Count Fix - October 17, 2025

## Problem
Dashboard endpoint returning `totalAnalysesCount: 0` despite analyses being performed.

## Root Cause
Query logic was using indirect relationship:
- `SponsorshipCode.CreatedSubscriptionId` → `PlantAnalysis.ActiveSponsorshipId`
- Complex chain could fail if fields weren't properly set

## Solution
Simplified to use direct `SponsorCompanyId` field:

**Total Count:**
```csharp
var totalAnalyses = await _plantAnalysisRepository.GetCountAsync(
    pa => pa.SponsorCompanyId.HasValue && 
          pa.SponsorCompanyId.Value == request.SponsorId);
```

**Tier Count:**
```csharp
var tierAnalysesCount = await _plantAnalysisRepository.GetCountAsync(
    pa => pa.SponsorCompanyId.HasValue && 
          pa.SponsorCompanyId.Value == request.SponsorId &&
          pa.ActiveSponsorshipId.HasValue &&
          tierSubscriptionIds.Contains(pa.ActiveSponsorshipId.Value));
```

## Changes Made
- **File:** `Business/Handlers/Sponsorship/Queries/GetSponsorDashboardSummaryQuery.cs`
- **Method:** `GetSponsorDashboardSummaryQueryHandler.Handle()`
- **Performance:** Changed from `GetListAsync().Count()` to `GetCountAsync()`
- **Added:** Console logging for debugging analysis counts

## Testing
1. Cache auto-clears on purchase/distribution
2. Look for logs: `[DashboardAnalyses] Total analyses for sponsor {id}: {count}`
3. Verify `SponsorCompanyId` populated in database

## Benefits
- More reliable (direct sponsor-to-analysis relationship)
- Better performance (COUNT vs SELECT * + Count)
- Simpler logic (fewer conditions)
- Added logging for troubleshooting

## Documentation
Created: `claudedocs/DASHBOARD_ANALYSIS_COUNT_FIX.md`
