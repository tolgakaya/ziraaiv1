# Sponsor Attribution Enhanced Logging - October 17, 2025

## Issue
User reported that `ActiveSponsorshipId` is `null` for most analyses in database, only 1 analysis has it populated.

## Investigation
The `CaptureActiveSponsorAsync` method in `CreatePlantAnalysisCommand.cs` is called but may be failing silently due to:

1. **No active sponsored subscription** - Subscription might be:
   - Not marked as `IsSponsoredSubscription = true`
   - `QueueStatus != Active`
   - `IsActive = false`
   - `EndDate` expired

2. **Timing issues** - Subscription might not be activated yet when analysis runs

3. **Code not linked** - `SponsorshipCodeId` might be null

## Solution
Enhanced logging in `CaptureActiveSponsorAsync` to identify root cause:

### Added Logging Points
1. **Entry Check**: Log when userId is missing
2. **Subscription Lookup**: Log search for active sponsorship
3. **Not Found Details**: When no active subscription:
   - Check if user has ANY subscription
   - Log subscription properties (IsSponsoredSubscription, QueueStatus, IsActive, EndDate)
4. **Success Path**: Log subscription ID and code ID
5. **Code Lookup**: Log if sponsorship code is found
6. **Attribution Success**: Log both IDs being set
7. **Error Handling**: Full exception logging with stack trace

### Log Format
```
[SponsorAttribution] 🔍 Looking for active sponsorship for user {userId}
[SponsorAttribution] ❌ No active sponsored subscription found
[SponsorAttribution] ℹ️ User has subscription but not active/sponsored:
   - IsSponsoredSubscription: false/true
   - QueueStatus: Pending/Active/Expired
   - IsActive: true/false
   - EndDate: {date}
[SponsorAttribution] ✅ Analysis attributed to sponsor {sponsorId}
```

## Testing Instructions
1. Build and deploy
2. Create new analysis with a farmer who has an active sponsored subscription
3. Check logs for `[SponsorAttribution]` messages
4. Identify which condition is failing

## Common Failure Scenarios
- **QueueStatus = Pending**: Subscription not yet activated
- **IsActive = false**: Subscription deactivated
- **EndDate expired**: Subscription already ended
- **IsSponsoredSubscription = false**: Regular subscription, not sponsored

## Related Code
- **File**: `Business/Handlers/PlantAnalyses/Commands/CreatePlantAnalysisCommand.cs`
- **Method**: `CaptureActiveSponsorAsync(PlantAnalysis analysis, int? userId)`
- **Called from**: `CreatePlantAnalysisCommandHandler.Handle()` (line 114)

## Next Steps
Run analysis and check logs to determine why sponsor capture is failing for most users.
