# Dashboard Analysis Count Fix

## Issue
Dashboard endpoint `/api/v1/sponsorship/dashboard-summary` was returning `totalAnalysesCount: 0` and tier-level `analysesCount: 0` even though analyses had been performed using sponsored subscriptions.

## Root Cause
The original query logic had two potential issues:

1. **Complex Query Chain**: 
   - Getting `SponsorshipCode.CreatedSubscriptionId` → Using it to match `PlantAnalysis.ActiveSponsorshipId`
   - This required proper setup of both fields and could fail if either wasn't set correctly

2. **Indirect Relationship**:
   - The query relied on matching subscription IDs between codes and analyses
   - If `ActiveSponsorshipId` wasn't properly captured during analysis creation, counts would be zero

## Solution
Simplified the query to use `SponsorCompanyId` directly:

### Before (Complex):
```csharp
// Get subscription IDs from codes
var sponsoredSubscriptionIds = allCodes
    .Where(c => c.CreatedSubscriptionId.HasValue)
    .Select(c => c.CreatedSubscriptionId.Value)
    .Distinct()
    .ToList();

// Match analyses by subscription ID
var analyses = await _plantAnalysisRepository.GetListAsync(
    pa => pa.ActiveSponsorshipId.HasValue &&
          sponsoredSubscriptionIds.Contains(pa.ActiveSponsorshipId.Value));
```

### After (Direct):
```csharp
// Direct query by sponsor ID
var totalAnalyses = await _plantAnalysisRepository.GetCountAsync(
    pa => pa.SponsorCompanyId.HasValue && 
          pa.SponsorCompanyId.Value == request.SponsorId);
```

### For Tier-Level Counts:
```csharp
// Combine both conditions for tier-specific counts
var tierAnalysesCount = await _plantAnalysisRepository.GetCountAsync(
    pa => pa.SponsorCompanyId.HasValue && 
          pa.SponsorCompanyId.Value == request.SponsorId &&
          pa.ActiveSponsorshipId.HasValue &&
          tierSubscriptionIds.Contains(pa.ActiveSponsorshipId.Value));
```

## Benefits
1. **More Reliable**: Direct relationship between sponsor and analysis via `SponsorCompanyId`
2. **Better Performance**: `GetCountAsync` is more efficient than `GetListAsync().Count()`
3. **Simpler Logic**: Fewer joins and conditions to maintain
4. **Resilient**: Works even if `ActiveSponsorshipId` has issues

## Field Population
Both fields are populated during analysis creation in `CreatePlantAnalysisCommand.cs`:

```csharp
private async Task CaptureActiveSponsorAsync(PlantAnalysis analysis, int? userId)
{
    // Get active sponsored subscription
    var activeSponsorship = await _userSubscriptionRepository.GetAsync(s =>
        s.UserId == userId.Value &&
        s.IsSponsoredSubscription &&
        s.QueueStatus == SubscriptionQueueStatus.Active &&
        s.IsActive &&
        s.EndDate > DateTime.Now);

    if (activeSponsorship == null) return;

    // Get sponsor from code
    var code = await _sponsorshipCodeRepository.GetAsync(c => 
        c.Id == activeSponsorship.SponsorshipCodeId);

    if (code != null)
    {
        analysis.ActiveSponsorshipId = activeSponsorship.Id;  // Subscription ID
        analysis.SponsorCompanyId = code.SponsorId;          // Sponsor user ID (direct)
    }
}
```

## Cache Management
Dashboard data is cached for 24 hours with key: `SponsorDashboard:{sponsorId}`

Cache is automatically invalidated when:
- New purchase is made (`PurchaseBulkSponsorshipCommand`)
- Sponsorship links are sent (`SendSponsorshipLinkCommand`)

## Testing
To verify the fix:

1. **Clear Cache** (if needed):
   - Make any new purchase OR send any sponsorship link
   - Cache will auto-clear

2. **Make API Call**:
   ```bash
   GET /api/v1/sponsorship/dashboard-summary
   Authorization: Bearer {sponsor_token}
   ```

3. **Expected Response**:
   ```json
   {
     "data": {
       "totalAnalysesCount": 10,  // Should now show actual count
       "activePackages": [
         {
           "tierName": "M",
           "analysesCount": 5      // Should show tier-specific count
         }
       ]
     }
   }
   ```

4. **Verification Logs**:
   Look for these console logs:
   ```
   [DashboardAnalyses] Total analyses for sponsor {id}: {count}
   [DashboardAnalyses] Tier M analyses: {count}
   ```

## Database Verification
To verify `SponsorCompanyId` is populated:

```sql
SELECT 
    pa.Id,
    pa.UserId,
    pa.SponsorCompanyId,
    pa.ActiveSponsorshipId,
    pa.CreatedDate
FROM PlantAnalysis pa
WHERE pa.SponsorCompanyId IS NOT NULL
ORDER BY pa.CreatedDate DESC
LIMIT 10;
```

Expected: `SponsorCompanyId` should be populated for all sponsored analyses.

## Related Files
- `Business/Handlers/Sponsorship/Queries/GetSponsorDashboardSummaryQuery.cs` (modified)
- `Business/Handlers/PlantAnalyses/Commands/CreatePlantAnalysisCommand.cs` (analysis attribution)
- `Entities/Concrete/PlantAnalysis.cs` (entity definition)

## Migration Required
No database migration required - fields already exist:
- `PlantAnalysis.SponsorCompanyId` (int?)
- `PlantAnalysis.ActiveSponsorshipId` (int?)

Both fields have been in use since the sponsorship system implementation.
