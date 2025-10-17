# Sponsorship Queue System Design

## Implementation Approach

**✅ Event-Driven Queue Activation (Selected)**
- Queue activates automatically during subscription validation
- No scheduled jobs or additional infrastructure needed
- Immediate activation when previous sponsorship expires
- Integrated into `SubscriptionValidationService.ProcessExpiredSubscriptionsAsync()`

**❌ Scheduled Job Approach (Not Used)**
- Would require Hangfire recurring job
- Delayed activation (up to 1 hour)
- Additional infrastructure overhead
- Unnecessary for this use case

---

## Business Requirements

### Core Rules
1. **NO simultaneous active sponsorships** - Only ONE active sponsor subscription at a time
2. **Trial → Sponsor**: Immediate activation (any tier S/M/L/XL)
3. **Active Sponsor → New Sponsor**: Queue the new sponsorship
4. **Auto-activation**: Queued sponsorship activates when current expires
5. **Multi-sponsor support**: Each sponsorship can be from different sponsor companies
6. **Sponsor attribution**: Track which sponsor was active for each PlantAnalysis

---

## Database Schema Changes

### 1. UserSubscription Entity (Existing - Modifications)

**New Fields:**
```csharp
public enum SubscriptionStatus
{
    Pending = 0,    // Redeemed but queued, waiting for activation
    Active = 1,     // Currently active and usable
    Expired = 2,    // Past end date
    Cancelled = 3   // Manually cancelled by user or admin
}

// New properties to add:
public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
public DateTime? QueuedDate { get; set; }        // When code was redeemed (if queued)
public DateTime? ActivatedDate { get; set; }     // When subscription actually activated
public int? PreviousSponsorshipId { get; set; }  // FK to the sponsorship this is waiting for
public virtual UserSubscription PreviousSponsorship { get; set; }  // Navigation property
```

**Migration Impact:**
- Existing records: Default `Status = Active`, `ActivatedDate = StartDate`
- Indexes: Add index on `Status` for queue queries
- Add FK constraint: `PreviousSponsorshipId` → `UserSubscription.Id`

---

### 2. PlantAnalysis Entity (Existing - Modifications)

**New Fields:**
```csharp
// Sponsor attribution tracking
public int? ActiveSponsorshipId { get; set; }    // FK to UserSubscription that was active
public int? SponsorCompanyId { get; set; }       // Denormalized for performance
public virtual UserSubscription ActiveSponsorship { get; set; }
public virtual Sponsor SponsorCompany { get; set; }
```

**Purpose:**
- `ActiveSponsorshipId`: Links analysis to exact sponsorship package used
- `SponsorCompanyId`: Denormalized for quick logo/access queries without joins
- Both set at analysis creation time, immutable afterward

**Migration Impact:**
- Existing records: Can be NULL (historical data)
- New analyses: MUST populate both fields
- Indexes: Add composite index on `(UserId, SponsorCompanyId)` for filtering

---

## Queue Logic Flow

### Scenario 1: Trial User Redeems Sponsorship Code
```
Current State: UserSubscription (Trial, Active)
Action: Redeem sponsorship code SPONSOR-XL-ABC123

Logic:
1. Validate code (unused, not expired, correct sponsor)
2. Create UserSubscription:
   - Status = Active (immediate activation)
   - ActivatedDate = DateTime.Now
   - StartDate = DateTime.Now
   - EndDate = DateTime.Now.AddMonths(tier.DurationMonths)
   - QueuedDate = NULL
   - PreviousSponsorshipId = NULL
3. Update Trial subscription:
   - EndDate = DateTime.Now (terminate immediately)
   - Status = Expired
4. Return success: "Sponsorluk aktivasyonu tamamlandı!"
```

---

### Scenario 2: Sponsored User Redeems Second Sponsorship
```
Current State: UserSubscription (Sponsor L, Active, Expires 2025-12-31)
Action: Redeem second code SPONSOR-XL-DEF456

Logic:
1. Validate code
2. Check for active sponsorship:
   - Found: UserSubscription ID=42 (Sponsor L, Active)
3. Create UserSubscription:
   - Status = Pending (queued, NOT active)
   - QueuedDate = DateTime.Now
   - ActivatedDate = NULL
   - StartDate = NULL (will be set on activation)
   - EndDate = NULL (will be set on activation)
   - PreviousSponsorshipId = 42 (waiting for this to expire)
4. Return success: "Sponsorluk kodunuz sıraya alındı. Mevcut sponsorluk bittiğinde otomatik aktif olacak."
```

---

### Scenario 3: Event-Driven Queue Activation (Automatic)
```
Trigger: ProcessExpiredSubscriptionsAsync (called during subscription validation)

Logic:
1. Find expired Active sponsorships:
   - IsActive = true AND EndDate < DateTime.Now
2. For each expired:
   - IsActive = false
   - QueueStatus = Expired
   - Status = "Expired"
3. Find queued sponsorships waiting for this:
   - QueueStatus = Pending AND PreviousSponsorshipId = expiredId
4. For each queued (auto-activate immediately):
   - QueueStatus = Active
   - ActivatedDate = DateTime.Now
   - StartDate = DateTime.Now
   - EndDate = DateTime.Now.AddDays(30)
   - IsActive = true
   - Status = "Active"
   - PreviousSponsorshipId = NULL (clear queue reference)
5. Future: Send notification to farmer
   - "🎉 Sıradaki sponsorluğunuz aktif oldu!"
```

---

## Implementation Components

### 1. RedeemSponsorshipCodeCommand Updates

**Current Behavior:** Direct activation
**New Behavior:** Check for active sponsorship, queue if needed

```csharp
// Pseudo-code changes
public async Task<IResult> Handle(RedeemSponsorshipCodeCommand request)
{
    // ... existing validation ...

    // NEW: Check for active sponsorship
    var activeSponsorshipQuery = await _subscriptionRepository.GetAsync(s => 
        s.UserId == request.UserId && 
        s.SubscriptionType == SubscriptionType.Sponsorship &&
        s.Status == SubscriptionStatus.Active &&
        s.EndDate > DateTime.Now);

    if (activeSponsorshipQuery != null)
    {
        // Queue the new sponsorship
        return await QueueSponsorship(request, activeSponsorshipQuery.Id);
    }

    // Immediate activation (Trial or no active sponsorship)
    return await ActivateSponsorship(request);
}

private async Task<IResult> QueueSponsorship(request, previousId)
{
    var subscription = new UserSubscription
    {
        UserId = request.UserId,
        SubscriptionType = SubscriptionType.Sponsorship,
        Tier = code.Tier,
        Status = SubscriptionStatus.Pending,
        QueuedDate = DateTime.Now,
        PreviousSponsorshipId = previousId,
        SponsorshipCodeId = code.Id,
        // StartDate, EndDate, ActivatedDate all NULL
    };

    await _subscriptionRepository.AddAsync(subscription);
    code.IsUsed = true;
    code.RedeemedDate = DateTime.Now;
    await _subscriptionRepository.SaveChangesAsync();

    return new SuccessResult("Sponsorluk kodunuz sıraya alındı!");
}
```

---

### 2. Event-Driven Queue Activation (Integrated into SubscriptionValidationService)

**Trigger:** Automatically during `ProcessExpiredSubscriptionsAsync()` call
**Responsibility:** Activate queued sponsorships when previous expires
**Approach:** Event-driven (not scheduled job), activates immediately when subscription expires

```csharp
// In SubscriptionValidationService.cs
public async Task ProcessExpiredSubscriptionsAsync()
{
    var now = DateTime.Now;
    
    var expiredSubscriptions = await _userSubscriptionRepository.GetListAsync(
        s => s.IsActive && s.EndDate <= now);

    var expiredList = expiredSubscriptions.ToList();

    foreach (var subscription in expiredList)
    {
        subscription.IsActive = false;
        subscription.QueueStatus = SubscriptionQueueStatus.Expired;
        subscription.Status = "Expired";
        subscription.UpdatedDate = now;
        
        _userSubscriptionRepository.Update(subscription);
    }

    await _userSubscriptionRepository.SaveChangesAsync();

    // Event-driven queue activation
    await ActivateQueuedSponsorshipsAsync(expiredList);
}

private async Task ActivateQueuedSponsorshipsAsync(List<UserSubscription> expiredSubscriptions)
{
    foreach (var expired in expiredSubscriptions)
    {
        if (!expired.IsSponsoredSubscription) continue;

        var queued = await _userSubscriptionRepository.GetAsync(s =>
            s.QueueStatus == SubscriptionQueueStatus.Pending &&
            s.PreviousSponsorshipId == expired.Id);

        if (queued != null)
        {
            queued.QueueStatus = SubscriptionQueueStatus.Active;
            queued.ActivatedDate = DateTime.Now;
            queued.StartDate = DateTime.Now;
            queued.EndDate = DateTime.Now.AddDays(30);
            queued.IsActive = true;
            queued.Status = "Active";
            queued.PreviousSponsorshipId = null;
            queued.UpdatedDate = DateTime.Now;

            _userSubscriptionRepository.Update(queued);
            
            _logger.LogInformation("✅ Activated sponsorship {Id} for user {UserId}",
                queued.Id, queued.UserId);
        }
    }

    await _userSubscriptionRepository.SaveChangesAsync();
}
```

---

### 3. PlantAnalysis Creation - Sponsor Attribution

**Update:** `CreatePlantAnalysisCommand` handler

```csharp
// When creating PlantAnalysis
var activeSponsorship = await _subscriptionRepository.GetAsync(s =>
    s.UserId == userId &&
    s.Status == SubscriptionStatus.Active &&
    s.SubscriptionType == SubscriptionType.Sponsorship &&
    s.EndDate > DateTime.Now);

if (activeSponsorship != null)
{
    var code = await _codeRepository.GetAsync(c => c.Id == activeSponsorship.SponsorshipCodeId);
    
    analysis.ActiveSponsorshipId = activeSponsorship.Id;
    analysis.SponsorCompanyId = code?.SponsorId; // Denormalize for performance
}
```

---

### 4. Sponsor Logo Display Logic

**Update:** `GetPlantAnalysisResultQuery` / Frontend

```csharp
// Return sponsor info with analysis
var result = new PlantAnalysisResultDto
{
    // ... existing fields ...
    
    SponsorInfo = analysis.SponsorCompanyId.HasValue ? new SponsorInfoDto
    {
        SponsorId = analysis.SponsorCompanyId.Value,
        LogoUrl = sponsor.LogoUrl,
        CompanyName = sponsor.CompanyName,
        // Only if farmer has permission based on sponsor tier visibility rules
    } : null
};
```

---

## Migration Script

### Step 1: Add UserSubscription Fields
```sql
-- Add new columns
ALTER TABLE "UserSubscriptions" 
ADD COLUMN "Status" INTEGER NOT NULL DEFAULT 1,  -- Active
ADD COLUMN "QueuedDate" TIMESTAMP NULL,
ADD COLUMN "ActivatedDate" TIMESTAMP NULL,
ADD COLUMN "PreviousSponsorshipId" INTEGER NULL;

-- Set ActivatedDate for existing records
UPDATE "UserSubscriptions" 
SET "ActivatedDate" = "StartDate", 
    "Status" = CASE 
        WHEN "EndDate" < NOW() THEN 2  -- Expired
        ELSE 1  -- Active
    END
WHERE "SubscriptionType" = 1;  -- Sponsorship

-- Add FK constraint
ALTER TABLE "UserSubscriptions"
ADD CONSTRAINT "FK_UserSubscriptions_PreviousSponsorship"
FOREIGN KEY ("PreviousSponsorshipId") 
REFERENCES "UserSubscriptions"("Id");

-- Add index for queue queries
CREATE INDEX "IX_UserSubscriptions_Status" ON "UserSubscriptions"("Status");
CREATE INDEX "IX_UserSubscriptions_Queue" ON "UserSubscriptions"("Status", "PreviousSponsorshipId");
```

### Step 2: Add PlantAnalysis Fields
```sql
-- Add sponsor attribution columns
ALTER TABLE "PlantAnalyses"
ADD COLUMN "ActiveSponsorshipId" INTEGER NULL,
ADD COLUMN "SponsorCompanyId" INTEGER NULL;

-- Add FK constraints
ALTER TABLE "PlantAnalyses"
ADD CONSTRAINT "FK_PlantAnalyses_ActiveSponsorship"
FOREIGN KEY ("ActiveSponsorshipId")
REFERENCES "UserSubscriptions"("Id");

ALTER TABLE "PlantAnalyses"
ADD CONSTRAINT "FK_PlantAnalyses_SponsorCompany"
FOREIGN KEY ("SponsorCompanyId")
REFERENCES "Sponsors"("Id");

-- Add composite index for sponsor filtering
CREATE INDEX "IX_PlantAnalyses_UserSponsor" 
ON "PlantAnalyses"("UserId", "SponsorCompanyId");
```

---

## API Changes

### RedeemSponsorshipCode Response

**Before:**
```json
{
  "success": true,
  "message": "Sponsorluk aktivasyonu tamamlandı!",
  "data": {
    "subscriptionId": 42,
    "tier": "XL",
    "startDate": "2025-10-07",
    "endDate": "2026-10-07"
  }
}
```

**After (Immediate Activation):**
```json
{
  "success": true,
  "message": "Sponsorluk aktivasyonu tamamlandı!",
  "data": {
    "subscriptionId": 42,
    "tier": "XL",
    "status": "Active",
    "activatedDate": "2025-10-07T10:30:00Z",
    "startDate": "2025-10-07",
    "endDate": "2026-10-07"
  }
}
```

**After (Queued):**
```json
{
  "success": true,
  "message": "Sponsorluk kodunuz sıraya alındı. Mevcut sponsorluk bittiğinde otomatik aktif olacak.",
  "data": {
    "subscriptionId": 43,
    "tier": "XL",
    "status": "Pending",
    "queuedDate": "2025-10-07T10:30:00Z",
    "previousSponsorshipId": 42,
    "estimatedActivationDate": "2025-12-31"  // Previous EndDate
  }
}
```

---

## Testing Scenarios

### Test 1: Trial → Sponsor XL
- **Setup:** User with Trial subscription
- **Action:** Redeem SPONSOR-XL-ABC123
- **Expected:** Status=Active, immediate activation, Trial ends

### Test 2: Sponsor L → Sponsor XL (Queue)
- **Setup:** User with active Sponsor L (expires 2025-12-31)
- **Action:** Redeem SPONSOR-XL-DEF456
- **Expected:** Status=Pending, queued, waits until 2025-12-31

### Test 3: Background Job Activation
- **Setup:** Sponsor L expired on 2025-10-07, XL queued
- **Action:** Run background job
- **Expected:** L → Expired, XL → Active

### Test 4: PlantAnalysis Sponsor Attribution
- **Setup:** User with active Sponsor L from "GreenTech A.Ş."
- **Action:** Create plant analysis
- **Expected:** `SponsorCompanyId = GreenTech ID`, logo displays correctly

### Test 5: Multiple Queue (Edge Case)
- **Setup:** Active L, queued M, try to redeem XL
- **Action:** Redeem third code
- **Expected:** Reject with "Zaten sırada bekleyen bir sponsorluk var"

---

## Configuration

### Event-Driven Activation
No additional configuration needed. Queue activation happens automatically when:
- Subscription validation runs (every PlantAnalysis request)
- Explicit `ProcessExpiredSubscriptionsAsync()` calls
- Any subscription status check

**Advantages over scheduled jobs:**
- Immediate activation (no waiting for hourly job)
- No additional infrastructure (Hangfire) required
- Happens naturally during existing subscription checks
- Lower system overhead

---

## Future Enhancements

1. **Multiple Queue Support**: Allow 2-3 queued sponsorships (FIFO)
2. **Queue Cancellation**: Allow users to cancel queued (not active) sponsorships
3. **Queue Transfer**: Allow transferring queue position to another user
4. **Analytics**: Track queue wait times, activation rates
5. **Notifications**: Implement farmer notification when queue activates
6. **Scheduled Job (Optional)**: If needed, add to PlantAnalysisWorkerService for guaranteed activation without user requests

---

## Implementation Checklist

- [x] Add `SubscriptionQueueStatus` enum to Entities
- [x] Update `UserSubscription` entity with queue fields (QueueStatus, QueuedDate, ActivatedDate, PreviousSponsorshipId)
- [x] Update `PlantAnalysis` entity with sponsor attribution (ActiveSponsorshipId, SponsorCompanyId)
- [x] Create migration: `AddSponsorshipQueueSystem`
- [x] Update `RedeemSponsorshipCodeCommand` with queue logic (QueueSponsorship, ActivateSponsorship)
- [x] Add event-driven queue activation to `SubscriptionValidationService`
- [x] Update `CreatePlantAnalysisCommand` to capture sponsor (CaptureActiveSponsorAsync)
- [x] Fix environment-based redemption URL in `SendSponsorshipLinkCommand`
- [ ] Update `GetPlantAnalysisResultQuery` to return sponsor info (if needed)
- [ ] Add unit tests for queue scenarios
- [ ] Update API documentation
- [ ] Update mobile team handoff with queue status handling

---

**Created:** 2025-10-07  
**Status:** Design Complete - Ready for Implementation
