# Queue System Fix - Implementation Plan

**Tarih**: 2025-11-24
**Seçilen Business Rule**: Seçenek 1 - Queue'ya Al
**Branch**: feature/staging-testing

---

## 🎯 Implementation Goal

**Objective**: Fix queue system to handle ALL paid subscriptions (not just sponsorships) by queuing new sponsorship redemptions when user has ANY active paid subscription.

**Expected Behavior After Fix**:
```
User State                          | Action                    | Result
----------------------------------- | ------------------------- | ---------------------------------
Trial subscription active           | Redeem sponsorship code   | Deactivate trial, activate sponsorship ✅ (works)
Sponsorship subscription active     | Redeem sponsorship code   | Queue new sponsorship (Pending) ✅ (works)
CreditCard subscription active      | Redeem sponsorship code   | Queue new sponsorship (Pending) ❌ (broken - will fix)
BankTransfer subscription active    | Redeem sponsorship code   | Queue new sponsorship (Pending) ❌ (broken - will fix)
No active subscription              | Redeem sponsorship code   | Activate sponsorship immediately ✅ (works)
Multiple active subscriptions       | Redeem sponsorship code   | Prevent or queue ❌ (broken - will fix)
```

---

## 📋 Implementation Checklist

### Phase 1: Repository Layer Enhancement ✅
- [ ] Create new method `GetActiveNonTrialSubscriptionAsync` in `IUserSubscriptionRepository`
- [ ] Implement method in `UserSubscriptionRepository` with proper ordering
- [ ] Add method `GetAllActiveSubscriptionsAsync` for validation/debugging
- [ ] Add unit tests for new repository methods

### Phase 2: Service Layer Logic Fix ✅
- [ ] Update decision condition in `RedeemSponsorshipCodeAsync`
- [ ] Fix `hasActiveSponsorshipOrPaid` to check ANY paid subscription
- [ ] Update logging to show subscription type in decision
- [ ] Add validation to prevent multiple active paid subscriptions

### Phase 3: Queue Logic Enhancement ✅
- [ ] Update `QueueSponsorship` to handle CreditCard subscriptions
- [ ] Add proper messaging for queued sponsorships
- [ ] Update `PreviousSponsorshipId` to reference ANY subscription type
- [ ] Test queue activation works for CreditCard → Sponsorship

### Phase 4: Validation & Error Handling ✅
- [ ] Add validation for multiple active subscription detection
- [ ] Add meaningful error messages for users
- [ ] Add detailed logging for debugging
- [ ] Handle edge cases (expired but not processed, etc.)

### Phase 5: Testing ✅
- [ ] Manual test: CreditCard → Sponsorship (should queue)
- [ ] Manual test: Sponsorship → Sponsorship (should still work)
- [ ] Manual test: Trial → Sponsorship (should still work)
- [ ] Manual test: No subscription → Sponsorship (should activate)
- [ ] Test queue activation when CreditCard expires
- [ ] Test UserId=189 scenario

### Phase 6: Data Cleanup (Optional) ✅
- [ ] Create SQL script to identify affected users
- [ ] Create cleanup script for UserId=189 and similar cases
- [ ] Document manual cleanup process

---

## 🔧 Detailed Implementation Steps

### Step 1: Repository Layer Enhancement

**File**: `DataAccess/Abstract/IUserSubscriptionRepository.cs`

**Action**: Add new interface methods

```csharp
/// <summary>
/// Gets the user's active non-trial subscription (paid subscriptions only)
/// Returns CreditCard, BankTransfer, or Sponsorship subscriptions (NOT Trial)
/// Orders by priority: CreditCard > BankTransfer > Sponsorship > Others
/// </summary>
Task<UserSubscription> GetActiveNonTrialSubscriptionAsync(int userId);

/// <summary>
/// Gets ALL active subscriptions for a user (for validation and debugging)
/// </summary>
Task<List<UserSubscription>> GetAllActiveSubscriptionsAsync(int userId);
```

**Why**: 
- `GetActiveNonTrialSubscriptionAsync`: Replaces current logic, explicitly excludes trials
- Priority ordering ensures deterministic behavior when multiple active subscriptions exist
- `GetAllActiveSubscriptionsAsync`: Helps detect conflicts and multiple active subscriptions

---

**File**: `DataAccess/Concrete/EntityFramework/UserSubscriptionRepository.cs`

**Action**: Implement new methods

```csharp
public async Task<UserSubscription> GetActiveNonTrialSubscriptionAsync(int userId)
{
    return await Context.UserSubscriptions
        .Include(x => x.SubscriptionTier)
        .Where(x => x.UserId == userId 
            && x.IsActive 
            && x.Status == "Active" 
            && x.EndDate > DateTime.Now
            && !x.IsTrialSubscription)  // ⚠️ KEY: Exclude trials
        .OrderBy(x => 
            // Priority: CreditCard (0) > BankTransfer (1) > Sponsorship (2) > Others (3)
            x.PaymentMethod == "CreditCard" ? 0 :
            x.PaymentMethod == "BankTransfer" ? 1 :
            x.IsSponsoredSubscription ? 2 : 3)
        .ThenByDescending(x => x.CreatedDate)  // If same priority, newest first
        .FirstOrDefaultAsync();
}

public async Task<List<UserSubscription>> GetAllActiveSubscriptionsAsync(int userId)
{
    return await Context.UserSubscriptions
        .Include(x => x.SubscriptionTier)
        .Where(x => x.UserId == userId 
            && x.IsActive 
            && x.Status == "Active" 
            && x.EndDate > DateTime.Now)
        .OrderByDescending(x => x.CreatedDate)
        .ToListAsync();
}
```

**Why**:
- **Trial exclusion**: Trials should always be replaced, not queued
- **Priority ordering**: Deterministic behavior when conflicts exist
- **Comprehensive filtering**: Active + Valid EndDate + Active Status

**Testing**:
```sql
-- Test query: Should return CreditCard subscription first for UserId=189
SELECT Id, UserId, PaymentMethod, IsSponsoredSubscription, IsTrialSubscription, QueueStatus, IsActive
FROM UserSubscriptions
WHERE UserId = 189 
  AND IsActive = true 
  AND Status = 'Active'
  AND EndDate > NOW()
  AND IsTrialSubscription = false
ORDER BY 
  CASE 
    WHEN PaymentMethod = 'CreditCard' THEN 0
    WHEN PaymentMethod = 'BankTransfer' THEN 1
    WHEN IsSponsoredSubscription = true THEN 2
    ELSE 3
  END,
  CreatedDate DESC;
```

---

### Step 2: Service Layer Logic Fix

**File**: `Business/Services/Sponsorship/SponsorshipService.cs`

**Location**: Method `RedeemSponsorshipCodeAsync` (lines 231-260)

**Current Code** (BROKEN):
```csharp
public async Task<IDataResult<UserSubscription>> RedeemSponsorshipCodeAsync(string code, int userId)
{
    try
    {
        // 1. Validate code
        var sponsorshipCode = await _sponsorshipCodeRepository.GetUnusedCodeAsync(code);
        if (sponsorshipCode == null)
            return new ErrorDataResult<UserSubscription>("Invalid or expired sponsorship code");

        // 2. Check for active sponsored subscription
        var existingSubscription = await _userSubscriptionRepository
            .GetActiveSubscriptionByUserIdAsync(userId);  // ❌ OLD METHOD

        // ⚠️ BROKEN CONDITION
        bool hasActiveSponsorshipOrPaid = existingSubscription != null &&
                                           existingSubscription.IsSponsoredSubscription &&  // ❌ ONLY sponsorships
                                           existingSubscription.QueueStatus == SubscriptionQueueStatus.Active;

        if (hasActiveSponsorshipOrPaid)
        {
            return await QueueSponsorship(code, userId, sponsorshipCode, existingSubscription.Id);
        }

        return await ActivateSponsorship(code, userId, sponsorshipCode, existingSubscription);
    }
    catch (Exception ex)
    {
        return new ErrorDataResult<UserSubscription>($"Error redeeming sponsorship code: {ex.Message}");
    }
}
```

**New Code** (FIXED):
```csharp
public async Task<IDataResult<UserSubscription>> RedeemSponsorshipCodeAsync(string code, int userId)
{
    try
    {
        Console.WriteLine($"[SponsorshipRedeem] Starting redemption for code: {code}, UserId: {userId}");

        // 1. Validate code
        var sponsorshipCode = await _sponsorshipCodeRepository.GetUnusedCodeAsync(code);
        if (sponsorshipCode == null)
        {
            Console.WriteLine($"[SponsorshipRedeem] ❌ Invalid or expired code: {code}");
            return new ErrorDataResult<UserSubscription>("Invalid or expired sponsorship code");
        }

        Console.WriteLine($"[SponsorshipRedeem] ✅ Valid code found: {code}, Tier: {sponsorshipCode.SubscriptionTierId}");

        // 2. Check for active PAID (non-trial) subscription
        var existingSubscription = await _userSubscriptionRepository
            .GetActiveNonTrialSubscriptionAsync(userId);  // ✅ NEW METHOD

        // 3. Additional validation: Check for multiple active subscriptions
        var allActiveSubscriptions = await _userSubscriptionRepository
            .GetAllActiveSubscriptionsAsync(userId);

        if (allActiveSubscriptions.Count > 1)
        {
            Console.WriteLine($"[SponsorshipRedeem] ⚠️ Multiple active subscriptions detected for UserId: {userId}");
            Console.WriteLine($"[SponsorshipRedeem] Active subscriptions: {string.Join(", ", allActiveSubscriptions.Select(s => $"ID:{s.Id}({s.PaymentMethod})"))}");
            
            // Log warning but continue with the logic
            // The queue system will handle this correctly by queuing the new one
        }

        // ✅ FIXED CONDITION: Queue if ANY paid subscription exists (not just sponsorships)
        bool hasActivePaidSubscription = existingSubscription != null &&
                                          existingSubscription.IsActive &&
                                          existingSubscription.QueueStatus == SubscriptionQueueStatus.Active;

        if (hasActivePaidSubscription)
        {
            Console.WriteLine($"[SponsorshipRedeem] 🔄 Active {existingSubscription.PaymentMethod} subscription found (ID: {existingSubscription.Id})");
            Console.WriteLine($"[SponsorshipRedeem] Queuing new sponsorship code: {code}");
            
            return await QueueSponsorship(code, userId, sponsorshipCode, existingSubscription.Id);
        }

        Console.WriteLine($"[SponsorshipRedeem] ✅ No active paid subscription found, direct activation");
        
        // Check if there's a trial subscription to deactivate
        var trialSubscription = allActiveSubscriptions.FirstOrDefault(s => s.IsTrialSubscription);
        if (trialSubscription != null)
        {
            Console.WriteLine($"[SponsorshipRedeem] Trial subscription found (ID: {trialSubscription.Id}), will be deactivated");
        }

        return await ActivateSponsorship(code, userId, sponsorshipCode, trialSubscription);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SponsorshipRedeem] ❌ Error: {ex.Message}");
        Console.WriteLine($"[SponsorshipRedeem] StackTrace: {ex.StackTrace}");
        return new ErrorDataResult<UserSubscription>($"Error redeeming sponsorship code: {ex.Message}");
    }
}
```

**Key Changes**:
1. ✅ Use `GetActiveNonTrialSubscriptionAsync` instead of `GetActiveSubscriptionByUserIdAsync`
2. ✅ Simplified condition: Check ANY active paid subscription (not just sponsorships)
3. ✅ Added validation to detect multiple active subscriptions
4. ✅ Enhanced logging to show subscription type and decisions
5. ✅ Pass only trial subscription to `ActivateSponsorship` (not any paid subscription)

**Why**:
- **Consistency**: Same behavior for CreditCard, BankTransfer, Sponsorship
- **Clear logic**: If ANY paid subscription exists → Queue
- **Better debugging**: Enhanced logging shows decision path
- **Edge case handling**: Detects and logs multiple active subscriptions

---

### Step 3: Queue Logic Enhancement

**File**: `Business/Services/Sponsorship/SponsorshipService.cs`

**Location**: Method `QueueSponsorship` (lines 265-317)

**Current Code**:
```csharp
private async Task<IDataResult<UserSubscription>> QueueSponsorship(
    string code, int userId, SponsorshipCode sponsorshipCode, int previousSponsorshipId)
{
    var queuedSubscription = new UserSubscription
    {
        UserId = userId,
        SubscriptionTierId = sponsorshipCode.SubscriptionTierId,
        
        QueueStatus = SubscriptionQueueStatus.Pending,
        IsActive = false,
        Status = "Pending",
        PreviousSponsorshipId = previousSponsorshipId,
        QueuedDate = DateTime.Now,
        
        PaymentMethod = "Sponsorship",
        PaymentReference = code,
        IsSponsoredSubscription = true,
        SponsorshipCodeId = sponsorshipCode.Id,
        SponsorId = sponsorshipCode.SponsorId,
        SponsorshipNotes = $"Queued - Redeemed code: {code}",
        CreatedDate = DateTime.Now
    };

    _userSubscriptionRepository.Add(queuedSubscription);
    await _userSubscriptionRepository.SaveChangesAsync();

    await _sponsorshipCodeRepository.MarkAsUsedAsync(code, userId, queuedSubscription.Id);

    return new SuccessDataResult<UserSubscription>(queuedSubscription,
        "Sponsorluk kodunuz sıraya alındı. Mevcut sponsorluk bittiğinde otomatik aktif olacak.");
}
```

**New Code** (ENHANCED):
```csharp
private async Task<IDataResult<UserSubscription>> QueueSponsorship(
    string code, int userId, SponsorshipCode sponsorshipCode, int previousSubscriptionId)  // ✅ Renamed parameter
{
    // Get the previous subscription details for better messaging
    var previousSubscription = await _userSubscriptionRepository.GetAsync(s => s.Id == previousSubscriptionId);
    
    Console.WriteLine($"[SponsorshipQueue] Queueing sponsorship for UserId: {userId}");
    Console.WriteLine($"[SponsorshipQueue] Previous subscription: ID {previousSubscriptionId}, Type: {previousSubscription?.PaymentMethod}");

    var queuedSubscription = new UserSubscription
    {
        UserId = userId,
        SubscriptionTierId = sponsorshipCode.SubscriptionTierId,
        
        // Queue status
        QueueStatus = SubscriptionQueueStatus.Pending,
        IsActive = false,
        Status = "Pending",
        PreviousSponsorshipId = previousSubscriptionId,  // ✅ Can now reference ANY subscription type
        QueuedDate = DateTime.Now,
        
        // Payment info
        PaymentMethod = "Sponsorship",
        PaymentReference = code,
        IsSponsoredSubscription = true,
        SponsorshipCodeId = sponsorshipCode.Id,
        SponsorId = sponsorshipCode.SponsorId,
        
        // Enhanced notes with previous subscription info
        SponsorshipNotes = $"Queued - Redeemed code: {code}. Waiting for {previousSubscription?.PaymentMethod} subscription (ID: {previousSubscriptionId}) to expire.",
        
        CreatedDate = DateTime.Now
    };

    _userSubscriptionRepository.Add(queuedSubscription);
    await _userSubscriptionRepository.SaveChangesAsync();

    Console.WriteLine($"[SponsorshipQueue] ✅ Created queued subscription ID: {queuedSubscription.Id}");

    // Mark code as used
    await _sponsorshipCodeRepository.MarkAsUsedAsync(code, userId, queuedSubscription.Id);
    
    Console.WriteLine($"[SponsorshipQueue] ✅ Code {code} marked as used");

    // ✅ Enhanced user message based on subscription type
    string subscriptionTypeMessage = previousSubscription?.PaymentMethod switch
    {
        "CreditCard" => "kredi kartı aboneliğiniz",
        "BankTransfer" => "banka transferi aboneliğiniz",
        "Sponsorship" => "sponsorluk aboneliğiniz",
        _ => "mevcut aboneliğiniz"
    };

    return new SuccessDataResult<UserSubscription>(queuedSubscription,
        $"Sponsorluk kodunuz sıraya alındı. {subscriptionTypeMessage} bittiğinde otomatik olarak aktif olacak.");
}
```

**Key Changes**:
1. ✅ Renamed `previousSponsorshipId` → `previousSubscriptionId` (more accurate naming)
2. ✅ Fetch previous subscription details for context
3. ✅ Enhanced `SponsorshipNotes` to show subscription type being waited for
4. ✅ User-friendly message based on subscription type
5. ✅ Better logging to track queue operations

**Why**:
- **Clarity**: Parameter name reflects it can reference ANY subscription type
- **User experience**: Clear message about what they're waiting for
- **Debugging**: Enhanced logging and notes for troubleshooting
- **Transparency**: Users know exactly what's happening

---

### Step 4: Validation & Error Handling

**File**: `Business/Services/Sponsorship/SponsorshipService.cs`

**Action**: Add validation method (insert after `RedeemSponsorshipCodeAsync`)

```csharp
/// <summary>
/// Validates user's subscription state before redeeming code
/// Detects and reports multiple active subscription conflicts
/// </summary>
private async Task<IResult> ValidateSubscriptionStateAsync(int userId)
{
    var activeSubscriptions = await _userSubscriptionRepository
        .GetAllActiveSubscriptionsAsync(userId);

    // Check for multiple active paid subscriptions (data integrity issue)
    var activePaidSubscriptions = activeSubscriptions
        .Where(s => !s.IsTrialSubscription && s.IsActive)
        .ToList();

    if (activePaidSubscriptions.Count > 1)
    {
        Console.WriteLine($"[SponsorshipValidation] ⚠️ WARNING: User {userId} has {activePaidSubscriptions.Count} active paid subscriptions");
        
        foreach (var sub in activePaidSubscriptions)
        {
            Console.WriteLine($"[SponsorshipValidation]   - ID: {sub.Id}, Type: {sub.PaymentMethod}, " +
                            $"IsSponsored: {sub.IsSponsoredSubscription}, QueueStatus: {sub.QueueStatus}, " +
                            $"EndDate: {sub.EndDate:yyyy-MM-dd}");
        }

        // Log warning but allow operation to continue
        // The queue system will handle it correctly
        return new ErrorResult(
            $"Veri tutarlılığı hatası: {activePaidSubscriptions.Count} aktif abonelik tespit edildi. " +
            "Lütfen destek ekibi ile iletişime geçin.");
    }

    return new SuccessResult();
}
```

**Action**: Call validation in `RedeemSponsorshipCodeAsync` (add after code validation)

```csharp
// After: var sponsorshipCode = await _sponsorshipCodeRepository.GetUnusedCodeAsync(code);

// Validate subscription state
var validationResult = await ValidateSubscriptionStateAsync(userId);
if (!validationResult.Success)
{
    Console.WriteLine($"[SponsorshipRedeem] ❌ Validation failed: {validationResult.Message}");
    // Continue anyway but log the issue
    // return new ErrorDataResult<UserSubscription>(validationResult.Message);
}
```

**Why**:
- **Data integrity**: Detect existing conflicts before creating new subscriptions
- **User protection**: Prevent worsening existing data issues
- **Debugging**: Clear logging of subscription state conflicts
- **Graceful handling**: Can log warning and continue, or block operation

---

### Step 5: Update Queue Activation Logic

**File**: `Business/Services/Subscription/SubscriptionValidationService.cs`

**Location**: Method `ActivateQueuedSponsorshipsAsync` (lines 519-553)

**Current Code**:
```csharp
private async Task ActivateQueuedSponsorshipsAsync(List<UserSubscription> expiredSubscriptions)
{
    foreach (var expired in expiredSubscriptions)
    {
        if (!expired.IsSponsoredSubscription) continue;  // ❌ ONLY handles expired sponsorships

        var queued = await _userSubscriptionRepository.GetAsync(s =>
            s.QueueStatus == SubscriptionQueueStatus.Pending &&
            s.PreviousSponsorshipId == expired.Id);

        if (queued != null)
        {
            // Activate logic...
        }
    }
}
```

**New Code** (FIXED):
```csharp
private async Task ActivateQueuedSponsorshipsAsync(List<UserSubscription> expiredSubscriptions)
{
    foreach (var expired in expiredSubscriptions)
    {
        // ✅ FIXED: Check for queued subscriptions waiting for ANY expired subscription
        // Not just sponsorships - CreditCard, BankTransfer, etc. can also have queued subscriptions
        
        Console.WriteLine($"[QueueActivation] Checking for queued subscriptions waiting for ID: {expired.Id} ({expired.PaymentMethod})");

        var queued = await _userSubscriptionRepository.GetAsync(s =>
            s.QueueStatus == SubscriptionQueueStatus.Pending &&
            s.PreviousSponsorshipId == expired.Id);  // ✅ Now references ANY subscription type

        if (queued != null)
        {
            Console.WriteLine($"[QueueActivation] 🔄 Found queued subscription ID: {queued.Id}");
            Console.WriteLine($"[QueueActivation] Activating queued subscription for UserId: {queued.UserId}");

            // Activate the queued subscription
            queued.QueueStatus = SubscriptionQueueStatus.Active;
            queued.ActivatedDate = DateTime.Now;
            queued.StartDate = DateTime.Now;
            queued.EndDate = DateTime.Now.AddDays(30);  // 30 days for sponsorships
            queued.IsActive = true;
            queued.Status = "Active";
            queued.PreviousSponsorshipId = null;  // Clear reference
            queued.UpdatedDate = DateTime.Now;
            queued.SponsorshipNotes = $"{queued.SponsorshipNotes} | Activated on {DateTime.Now:yyyy-MM-dd HH:mm:ss} after {expired.PaymentMethod} subscription expired";

            _userSubscriptionRepository.Update(queued);
            
            Console.WriteLine($"[QueueActivation] ✅ Activated subscription ID: {queued.Id} for UserId: {queued.UserId}");
        }
        else
        {
            Console.WriteLine($"[QueueActivation] No queued subscriptions found for expired ID: {expired.Id}");
        }
    }

    await _userSubscriptionRepository.SaveChangesAsync();
    Console.WriteLine($"[QueueActivation] Queue activation complete");
}
```

**Key Changes**:
1. ✅ Removed `if (!expired.IsSponsoredSubscription) continue;` check
2. ✅ Now processes ALL expired subscriptions (CreditCard, BankTransfer, Sponsorship)
3. ✅ Enhanced logging to show subscription types
4. ✅ Updated notes to show what subscription was waited for

**Why**:
- **Consistency**: Queue activation works for ANY subscription type expiring
- **Fix critical gap**: CreditCard → Sponsorship queue can now activate properly
- **Better tracking**: Enhanced notes show complete activation history

---

## 🧪 Testing Plan

### Test Case 1: CreditCard → Sponsorship (PRIMARY FIX)

**Setup**:
```sql
-- Create test user with CreditCard subscription
INSERT INTO UserSubscriptions (UserId, SubscriptionTierId, PaymentMethod, IsActive, QueueStatus, StartDate, EndDate, Status, CreatedDate)
VALUES (999, 2, 'CreditCard', true, 1, NOW(), NOW() + INTERVAL '30 days', 'Active', NOW());
```

**Test Steps**:
1. Redeem sponsorship code as UserId=999
2. Verify new subscription created with `QueueStatus = Pending` (0)
3. Verify new subscription has `PreviousSponsorshipId = [CreditCard subscription ID]`
4. Verify code marked as `IsUsed = true`
5. Check logs show: "Active CreditCard subscription found, queuing"

**Expected Result**:
```
✅ Queued subscription created
✅ Status = Pending
✅ IsActive = false
✅ PreviousSponsorshipId points to CreditCard subscription
✅ User message: "kredi kartı aboneliğiniz bittiğinde otomatik olarak aktif olacak"
```

**SQL Validation**:
```sql
SELECT Id, UserId, PaymentMethod, IsSponsoredSubscription, QueueStatus, IsActive, PreviousSponsorshipId, Status
FROM UserSubscriptions
WHERE UserId = 999
ORDER BY CreatedDate DESC;
```

---

### Test Case 2: Queue Activation After CreditCard Expires

**Setup**: Use same test user from Test Case 1

**Test Steps**:
1. Manually expire CreditCard subscription:
```sql
UPDATE UserSubscriptions 
SET EndDate = NOW() - INTERVAL '1 day'
WHERE UserId = 999 AND PaymentMethod = 'CreditCard';
```

2. Trigger validation (make any API call that calls `ValidateAndLogUsageAsync`)
3. Or manually call: `await ProcessExpiredSubscriptionsAsync()`

**Expected Result**:
```
✅ CreditCard subscription marked as Expired (QueueStatus=2)
✅ Queued sponsorship activated (QueueStatus=1)
✅ Queued sponsorship IsActive = true
✅ PreviousSponsorshipId cleared (null)
✅ Logs show queue activation for UserId=999
```

**SQL Validation**:
```sql
-- Should show CreditCard expired, Sponsorship active
SELECT Id, PaymentMethod, QueueStatus, IsActive, Status, PreviousSponsorshipId
FROM UserSubscriptions
WHERE UserId = 999
ORDER BY CreatedDate DESC;
```

---

### Test Case 3: Sponsorship → Sponsorship (REGRESSION TEST)

**Purpose**: Ensure existing functionality still works

**Setup**:
```sql
-- User with active sponsorship
INSERT INTO UserSubscriptions (UserId, SubscriptionTierId, PaymentMethod, IsSponsoredSubscription, IsActive, QueueStatus, StartDate, EndDate, Status, CreatedDate)
VALUES (998, 2, 'Sponsorship', true, true, 1, NOW(), NOW() + INTERVAL '30 days', 'Active', NOW());
```

**Test Steps**:
1. Redeem another sponsorship code as UserId=998
2. Verify queued subscription created

**Expected Result**:
```
✅ Still works as before
✅ New subscription queued (Pending)
✅ References previous sponsorship
```

---

### Test Case 4: Trial → Sponsorship (REGRESSION TEST)

**Purpose**: Ensure trial replacement still works

**Setup**:
```sql
-- User with trial subscription
INSERT INTO UserSubscriptions (UserId, SubscriptionTierId, IsTrialSubscription, IsActive, QueueStatus, StartDate, EndDate, Status, CreatedDate)
VALUES (997, 1, true, true, 1, NOW(), NOW() + INTERVAL '7 days', 'Active', NOW());
```

**Test Steps**:
1. Redeem sponsorship code as UserId=997
2. Verify trial deactivated and sponsorship activated immediately

**Expected Result**:
```
✅ Trial marked as Expired
✅ Sponsorship immediately active (NOT queued)
✅ QueueStatus = Active for sponsorship
```

---

### Test Case 5: UserId=189 Scenario (REAL DATA TEST)

**Current State**:
```
ID 187: CreditCard (Active)
ID 188: Sponsorship (Active) ❌ Should be Pending
ID 189: Sponsorship (Active) ❌ Should be Pending
ID 190: Sponsorship (Active) ❌ Should be Pending
```

**Test Approach**:
1. **Manual cleanup first** (see Data Cleanup section)
2. After cleanup, test redeeming new code
3. Verify proper queueing behavior

**Expected After Fix**:
```
ID 187: CreditCard (Active) ✅
New redemption: Sponsorship (Pending) ✅
```

---

## 🧹 Data Cleanup Plan

### Step 1: Identify Affected Users

**SQL Query**:
```sql
-- Find users with multiple active paid subscriptions
WITH ActivePaidSubs AS (
  SELECT 
    UserId,
    COUNT(*) as ActiveCount,
    STRING_AGG(CONCAT('ID:', Id, '(', PaymentMethod, ')'), ', ') as SubscriptionList
  FROM UserSubscriptions
  WHERE IsActive = true
    AND Status = 'Active'
    AND EndDate > NOW()
    AND IsTrialSubscription = false
  GROUP BY UserId
  HAVING COUNT(*) > 1
)
SELECT * FROM ActivePaidSubs
ORDER BY ActiveCount DESC;
```

**Expected Output**:
```
UserId | ActiveCount | SubscriptionList
-------+-------------+--------------------------------------------------
189    | 4           | ID:187(CreditCard), ID:188(Sponsorship), ...
...
```

---

### Step 2: Cleanup Script for UserId=189

**SQL Script**: `claudedocs/cleanup_userid_189.sql`

```sql
-- Cleanup Script for UserId=189
-- Keeps only ONE active subscription, queues the rest

BEGIN;

-- Step 1: Identify subscriptions to keep and queue
-- Keep: CreditCard subscription (ID 187) - legitimate paid subscription
-- Queue: Sponsorship subscriptions (ID 188, 189, 190)

-- Step 2: Update sponsorship subscriptions to Pending status
UPDATE UserSubscriptions
SET 
  QueueStatus = 0,  -- Pending
  IsActive = false,
  Status = 'Pending',
  UpdatedDate = NOW(),
  SponsorshipNotes = CONCAT(
    COALESCE(SponsorshipNotes, ''), 
    ' | Queued on ', NOW()::text, 
    ' (data cleanup - was incorrectly activated)'
  )
WHERE UserId = 189
  AND Id IN (188, 189, 190)
  AND IsSponsoredSubscription = true;

-- Step 3: Set up queue chain
-- ID 188 waits for ID 187 (CreditCard)
UPDATE UserSubscriptions
SET PreviousSponsorshipId = 187
WHERE Id = 188;

-- ID 189 waits for ID 188
UPDATE UserSubscriptions
SET PreviousSponsorshipId = 188
WHERE Id = 189;

-- ID 190 waits for ID 189
UPDATE UserSubscriptions
SET PreviousSponsorshipId = 189
WHERE Id = 190;

-- Step 4: Verify cleanup
SELECT 
  Id,
  PaymentMethod,
  IsSponsoredSubscription,
  QueueStatus,
  IsActive,
  Status,
  PreviousSponsorshipId,
  EndDate
FROM UserSubscriptions
WHERE UserId = 189
ORDER BY CreatedDate;

-- Expected result:
-- ID 187: QueueStatus=1 (Active), IsActive=true, PreviousSponsorshipId=NULL
-- ID 188: QueueStatus=0 (Pending), IsActive=false, PreviousSponsorshipId=187
-- ID 189: QueueStatus=0 (Pending), IsActive=false, PreviousSponsorshipId=188
-- ID 190: QueueStatus=0 (Pending), IsActive=false, PreviousSponsorshipId=189

-- If everything looks good, commit
COMMIT;
-- If not, rollback: ROLLBACK;
```

---

### Step 3: Generic Cleanup Script

**SQL Script**: `claudedocs/cleanup_multiple_active_subscriptions.sql`

```sql
-- Generic cleanup for all users with multiple active subscriptions

BEGIN;

-- Create temporary table with cleanup plan
CREATE TEMP TABLE SubscriptionCleanupPlan AS
WITH RankedSubscriptions AS (
  SELECT 
    Id,
    UserId,
    PaymentMethod,
    IsSponsoredSubscription,
    IsTrialSubscription,
    CreatedDate,
    -- Priority: CreditCard (1) > BankTransfer (2) > Sponsorship (3)
    ROW_NUMBER() OVER (
      PARTITION BY UserId 
      ORDER BY 
        CASE 
          WHEN PaymentMethod = 'CreditCard' THEN 1
          WHEN PaymentMethod = 'BankTransfer' THEN 2
          WHEN IsSponsoredSubscription = true THEN 3
          ELSE 4
        END,
        CreatedDate DESC
    ) as Priority
  FROM UserSubscriptions
  WHERE IsActive = true
    AND Status = 'Active'
    AND EndDate > NOW()
    AND IsTrialSubscription = false
)
SELECT 
  Id,
  UserId,
  PaymentMethod,
  CASE 
    WHEN Priority = 1 THEN 'KEEP_ACTIVE'
    ELSE 'QUEUE'
  END as Action,
  LAG(Id) OVER (PARTITION BY UserId ORDER BY Priority) as WaitForSubscriptionId
FROM RankedSubscriptions;

-- Show cleanup plan
SELECT * FROM SubscriptionCleanupPlan WHERE Action = 'QUEUE';

-- Execute cleanup (uncomment to run)
/*
UPDATE UserSubscriptions us
SET 
  QueueStatus = 0,  -- Pending
  IsActive = false,
  Status = 'Pending',
  PreviousSponsorshipId = cp.WaitForSubscriptionId,
  UpdatedDate = NOW(),
  SponsorshipNotes = CONCAT(
    COALESCE(us.SponsorshipNotes, ''), 
    ' | Queued on ', NOW()::text, 
    ' (auto-cleanup - was incorrectly activated)'
  )
FROM SubscriptionCleanupPlan cp
WHERE us.Id = cp.Id
  AND cp.Action = 'QUEUE';
*/

-- Verify results
SELECT 
  UserId,
  COUNT(*) as ActiveCount
FROM UserSubscriptions
WHERE IsActive = true
  AND Status = 'Active'
  AND EndDate > NOW()
  AND IsTrialSubscription = false
GROUP BY UserId
HAVING COUNT(*) > 1;

COMMIT;
```

---

## 📊 Rollout Strategy

### Phase 1: Development & Testing (Current)
1. ✅ Implement all code changes
2. ✅ Run unit tests for new methods
3. ✅ Manual testing with test users
4. ✅ Verify logs and behavior

### Phase 2: Staging Deployment
1. Deploy to staging environment
2. Run Test Cases 1-4 in staging
3. Monitor logs for 24 hours
4. Verify queue activation works correctly

### Phase 3: Data Cleanup (Staging)
1. Run identification query
2. Manually review affected users
3. Run cleanup script for UserId=189
4. Verify queue chain works end-to-end

### Phase 4: Production Deployment
1. Deploy code changes to production
2. Monitor redemption endpoints for 48 hours
3. Watch for any new multiple active subscription cases
4. Be ready to rollback if issues detected

### Phase 5: Production Data Cleanup
1. Take database backup
2. Run identification query
3. Review each affected user manually
4. Run cleanup scripts in transaction with verification
5. Monitor affected users for proper queue activation

---

## 🔄 Rollback Plan

If issues are detected after deployment:

### Code Rollback
```bash
# Revert to previous commit
git revert HEAD
git push origin feature/staging-testing
```

### Data Rollback (if cleanup was executed)
```sql
-- Restore from backup taken before cleanup
-- Or manually restore affected subscriptions:

UPDATE UserSubscriptions
SET 
  QueueStatus = 1,  -- Active
  IsActive = true,
  Status = 'Active',
  PreviousSponsorshipId = NULL,
  UpdatedDate = NOW()
WHERE Id IN (...);  -- IDs that were changed
```

---

## 📝 Success Criteria

### Code Changes Success
- ✅ All repository methods implemented and tested
- ✅ Decision logic correctly identifies ALL paid subscriptions
- ✅ Queue logic handles CreditCard → Sponsorship
- ✅ Queue activation works for ALL subscription types
- ✅ All test cases pass
- ✅ No regression in existing functionality

### Data Cleanup Success
- ✅ UserId=189 has only 1 active subscription
- ✅ Other subscriptions properly queued with correct chain
- ✅ No users have multiple active paid subscriptions
- ✅ Queue activation works for affected users

### Monitoring Success (7 days post-deployment)
- ✅ No new multiple active subscription cases
- ✅ All sponsorship redemptions correctly queued when paid subscription exists
- ✅ Queue activations working automatically on expiry
- ✅ No user complaints about subscriptions not activating

---

## 📚 Documentation Updates Needed

1. Update API documentation for `/api/sponsorships/redeem` endpoint
2. Document new business rule: "Paid subscription + sponsorship = queue"
3. Update subscription management docs
4. Add troubleshooting guide for queue system
5. Document cleanup process for future reference

---

## 🎯 Summary

**Problem**: Queue system only worked for Sponsorship → Sponsorship, ignored CreditCard subscriptions

**Solution**: Check for ANY active paid subscription (not just sponsorships) before redemption

**Impact**: 
- Users with paid subscriptions can no longer accumulate multiple active sponsorships
- Clear user messaging about queue status
- Proper queue activation when any subscription expires

**Risk**: Low - only changes decision logic condition, doesn't affect core subscription functionality

**Timeline**: 
- Implementation: 4-6 hours
- Testing: 2-3 hours
- Data cleanup: 1-2 hours
- Total: 1 day

---

**Let's implement step by step, following this plan exactly! 🚀**
