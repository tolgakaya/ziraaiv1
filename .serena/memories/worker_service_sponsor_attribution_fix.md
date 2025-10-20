# Worker Service Sponsor Attribution Fix - October 17, 2025

## Critical Issue Discovered
Worker Service (async analysis flow) was NOT capturing `ActiveSponsorshipId` and `SponsorCompanyId` fields when completing analyses. These fields remained NULL for all async analyses, causing:
- Dashboard to show 0 analysis counts
- Sponsor access control failures
- Missing sponsor attribution in analytics

## Root Cause
The `CaptureActiveSponsorAsync` method existed in `CreatePlantAnalysisCommand` (sync flow) but was NOT called in `PlantAnalysisJobService.ProcessPlantAnalysisResultAsync` (async flow via RabbitMQ).

## Solution Implemented

### Changes to PlantAnalysisWorkerService/Jobs/PlantAnalysisJobService.cs

1. **Added Dependencies:**
   - `IUserSubscriptionRepository _userSubscriptionRepository`
   - `ISponsorshipCodeRepository _sponsorshipCodeRepository`
   - Added to constructor parameters

2. **Added Using Statement:**
   - `using Entities.Concrete.Enums;` for `SubscriptionQueueStatus`

3. **Implemented CaptureActiveSponsorAsync Method:**
   ```csharp
   private async Task CaptureActiveSponsorAsync(PlantAnalysis analysis, int? userId)
   {
       // Looks up active sponsored subscription
       // Sets ActiveSponsorshipId and SponsorCompanyId
       // Enhanced logging with [SponsorAttribution] prefix
   }
   ```

4. **Added Method Call in ProcessPlantAnalysisResultAsync:**
   - Before: `_plantAnalysisRepository.Update(existingAnalysis);`
   - After: 
     ```csharp
     await CaptureActiveSponsorAsync(existingAnalysis, existingAnalysis.UserId);
     _plantAnalysisRepository.Update(existingAnalysis);
     ```
   - Location: Line 303 (after legacy field updates, before Update call)

## Testing Instructions

### 1. Deploy to Staging
- Build and deploy Worker Service
- Ensure repositories are registered in Program.cs (already done)

### 2. Create Async Analysis
```bash
POST /api/v1/plant-analyses/async
Authorization: Bearer {farmer_token_with_sponsored_subscription}
```

### 3. Check Logs
Look for `[SponsorAttribution]` messages:
```
[SponsorAttribution] 🔍 Looking for active sponsorship for user 165
[SponsorAttribution] ✅ Found active sponsorship: ID=456, CodeId=975
[SponsorAttribution] ✅ Found sponsorship code: AGRI-ABC123, SponsorId=159
[SponsorAttribution] ✅ Analysis 56 attributed to sponsor 159 (subscription 456)
```

### 4. Verify Database
```sql
SELECT 
    Id,
    AnalysisId,
    UserId,
    SponsorUserId,
    SponsorshipCodeId,
    ActiveSponsorshipId,    -- Should be populated now ✅
    SponsorCompanyId,       -- Should be populated now ✅
    CreatedDate
FROM PlantAnalyses
WHERE AnalysisStatus = 'Completed'
ORDER BY CreatedDate DESC
LIMIT 10;
```

### 5. Check Dashboard
```bash
GET /api/v1/sponsorship/dashboard-summary
Authorization: Bearer {sponsor_token}
```

Expected: `totalAnalysesCount` and tier-specific `analysesCount` should now show correct values.

## Commits
1. `d0f23ed` - Dashboard fix + API enhanced logging
2. `eaaba59` - Worker Service sponsor capture (THIS FIX)

## Related Files
- `PlantAnalysisWorkerService/Jobs/PlantAnalysisJobService.cs` (modified)
- `Business/Handlers/PlantAnalyses/Commands/CreatePlantAnalysisCommand.cs` (reference)
- `Business/Handlers/Sponsorship/Queries/GetSponsorDashboardSummaryQuery.cs` (uses these fields)

## Impact
- ✅ Async analyses now capture sponsor attribution correctly
- ✅ Dashboard analytics will show accurate counts
- ✅ Sponsor access control will work
- ✅ Matches synchronous analysis flow behavior
