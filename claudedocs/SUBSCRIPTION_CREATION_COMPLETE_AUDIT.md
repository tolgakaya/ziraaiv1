# Subscription Creation Complete Audit

**Date**: 2025-12-07
**Issue**: Trial subscriptions not being cancelled when paid/sponsored subscriptions are assigned
**Root Cause**: Multiple independent subscription creation handlers - each must implement Trial logic separately

---

## 🎯 Executive Summary

The system has **4 different handlers** that create subscriptions. All have been audited and fixed to ensure consistent Trial subscription cancellation logic.

### Audit Results

| Handler | Location | Trial Logic | Status |
|---------|----------|-------------|--------|
| **Admin Assignment** | `AssignSubscriptionCommand.cs` | ✅ FIXED | Commit 4b8df199 |
| **Payment Callback** | `IyzicoPaymentService.cs` | ✅ CORRECT | Lines 802-857 |
| **Code Redemption** | `SponsorshipService.cs` | ✅ CORRECT | Lines 392-410 |
| **Bulk Assignment Worker** | `FarmerSubscriptionAssignmentJobService.cs` | ✅ FIXED | This commit |

---

## 📋 Subscription Creation Handlers

### 1. Admin Manual Assignment
**File**: `Business/Handlers/AdminSubscriptions/Commands/AssignSubscriptionCommand.cs`
**Endpoint**: `POST /api/admin/subscriptions/assign`
**Fixed**: Commit 4b8df199 (2025-12-07)

**Behavior**:
- Trial → Any: Auto-cancel Trial, activate new immediately
- Sponsored → Sponsored: Queue by default, or force activation with `ForceActivation=true`
- Paid → Any: Require `ForceActivation=true` or return error

**Code Snippet** (lines 57-90):
```csharp
var existingActiveSubscription = await _subscriptionRepository.GetAsync(s =>
    s.UserId == request.UserId &&
    s.IsActive &&
    s.Status == "Active" &&
    s.EndDate > now);

if (existingActiveSubscription != null)
{
    if (existingActiveSubscription.IsTrialSubscription)
    {
        // Cancel Trial and activate new immediately
        existingActiveSubscription.IsActive = false;
        existingActiveSubscription.Status = "Cancelled";
        existingActiveSubscription.QueueStatus = SubscriptionQueueStatus.Cancelled;
        existingActiveSubscription.EndDate = now;
        existingActiveSubscription.UpdatedDate = now;
        _subscriptionRepository.Update(existingActiveSubscription);

        // Create new active subscription (trial replaced)
        var subscription = new UserSubscription { ... };
        _subscriptionRepository.Add(subscription);
    }
}
```

---

### 2. Payment Gateway Callback
**File**: `Business/Services/Payment/IyzicoPaymentService.cs`
**Method**: `ProcessFarmerSubscriptionAsync()`
**Status**: ✅ CORRECT (already has proper Trial handling)

**Behavior**:
- Trial → Paid: Cancel Trial, create new paid subscription
- Paid → Paid: Extend existing subscription duration

**Code Snippet** (lines 802-857):
```csharp
var existingSubscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(transaction.UserId);

if (existingSubscription != null)
{
    // If upgrading from trial to paid, cancel trial and create new subscription
    if (existingSubscription.IsTrialSubscription)
    {
        _logger.LogInformation($"[iyzico] Upgrading from trial subscription. TrialSubId: {existingSubscription.Id}");

        // Cancel trial subscription
        existingSubscription.IsActive = false;
        existingSubscription.Status = "Upgraded";
        existingSubscription.CancellationDate = DateTime.Now;
        existingSubscription.CancellationReason = $"Upgraded to paid subscription via payment transaction {transaction.Id}";
        existingSubscription.UpdatedDate = DateTime.Now;

        _userSubscriptionRepository.Update(existingSubscription);

        // Create new paid subscription
        var subscription = new UserSubscription { ... };
        _userSubscriptionRepository.Add(subscription);
    }
    else
    {
        // Extend existing paid subscription
        var extensionDays = flowData.DurationMonths * 30;
        existingSubscription.EndDate = existingSubscription.EndDate.AddDays(extensionDays);
    }
}
```

---

### 3. Sponsorship Code Redemption
**File**: `Business/Services/Sponsorship/SponsorshipService.cs`
**Method**: `ActivateSponsorship()`
**Status**: ✅ CORRECT (already has proper Trial handling)

**Behavior**:
- Trial → Sponsored: Cancel Trial, activate sponsored immediately
- Paid → Sponsored: Queue sponsored subscription
- Sponsored → Sponsored: Queue new sponsored subscription

**Code Snippet** (lines 392-410):
```csharp
if (existingSubscription != null)
{
    var existingTier = await _subscriptionTierRepository.GetAsync(t => t.Id == existingSubscription.SubscriptionTierId);
    bool isTrial = existingTier != null &&
                  (existingTier.TierName == "Trial" ||
                   existingTier.MonthlyPrice == 0 ||
                   existingSubscription.IsTrialSubscription);

    if (isTrial)
    {
        Console.WriteLine($"[SponsorshipRedeem] Deactivating existing {existingTier?.TierName} subscription (ID: {existingSubscription.Id})");
        existingSubscription.IsActive = false;
        existingSubscription.QueueStatus = SubscriptionQueueStatus.Expired;
        existingSubscription.Status = "Upgraded";
        existingSubscription.EndDate = DateTime.Now;
        existingSubscription.UpdatedDate = DateTime.Now;
        _userSubscriptionRepository.Update(existingSubscription);
        await _userSubscriptionRepository.SaveChangesAsync();
    }
}
```

**Queue Logic** (lines 250-270):
```csharp
bool hasActivePaidSubscription = existingSubscription != null &&
                                  existingSubscription.IsActive &&
                                  existingSubscription.QueueStatus == SubscriptionQueueStatus.Active;

if (hasActivePaidSubscription)
{
    Console.WriteLine($"[SponsorshipRedeem] Active {existingSubscription.PaymentMethod} subscription found");
    Console.WriteLine($"[SponsorshipRedeem] Queuing new sponsorship code: {code}");

    return await QueueSponsorship(code, userId, sponsorshipCode, existingSubscription.Id);
}

// Check if there's a trial subscription to deactivate
var trialSubscription = allActiveSubscriptions.FirstOrDefault(s => s.IsTrialSubscription);
if (trialSubscription != null)
{
    Console.WriteLine($"[SponsorshipRedeem] Trial subscription found, will be deactivated");
}

return await ActivateSponsorship(code, userId, sponsorshipCode, trialSubscription);
```

---

### 4. Bulk Subscription Assignment Worker
**File**: `PlantAnalysisWorkerService/Jobs/FarmerSubscriptionAssignmentJobService.cs`
**Method**: `ProcessFarmerSubscriptionAssignmentAsync()`
**Fixed**: This commit (2025-12-07)

**Problem**: Only checked for `IsActive`, didn't differentiate between Trial and paid subscriptions. Both Trial and newly assigned subscription would be active simultaneously.

**Fix**: Added explicit Trial detection and cancellation logic.

**Code Snippet** (lines 145-261):
```csharp
var existingSubscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(user.UserId);

if (existingSubscription != null)
{
    // ✅ FIX: If existing subscription is Trial, cancel it and create new subscription
    if (existingSubscription.IsTrialSubscription)
    {
        _logger.LogInformation(
            "[FARMER_SUBSCRIPTION_TRIAL_CANCEL] Cancelling Trial subscription - UserId: {UserId}, TrialSubId: {TrialSubId}",
            user.UserId, existingSubscription.Id);

        // Cancel Trial subscription
        existingSubscription.IsActive = false;
        existingSubscription.Status = "Upgraded";
        existingSubscription.QueueStatus = SubscriptionQueueStatus.Expired;
        existingSubscription.CancellationDate = DateTime.Now;
        existingSubscription.CancellationReason = $"Upgraded to {message.SubscriptionTierId} subscription via bulk assignment (Job: {message.BulkJobId})";
        existingSubscription.EndDate = DateTime.Now;
        existingSubscription.UpdatedDate = DateTime.Now;

        _userSubscriptionRepository.Update(existingSubscription);
        await _userSubscriptionRepository.SaveChangesAsync();

        // Create new subscription (Trial replaced)
        subscription = new UserSubscription
        {
            UserId = user.UserId,
            SubscriptionTierId = message.SubscriptionTierId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddDays(message.DurationDays),
            Status = message.AutoActivate ? "Active" : "Pending",
            IsActive = message.AutoActivate,
            AutoRenew = false,
            PaymentMethod = "AdminAssignment",
            IsTrialSubscription = false,
            IsSponsoredSubscription = false,
            QueueStatus = message.AutoActivate ? SubscriptionQueueStatus.Active : SubscriptionQueueStatus.Pending,
            ActivatedDate = message.AutoActivate ? DateTime.Now : (DateTime?)null,
            CreatedDate = DateTime.Now,
            CurrentDailyUsage = 0,
            CurrentMonthlyUsage = 0,
            LastUsageResetDate = DateTime.Now,
            MonthlyUsageResetDate = DateTime.Now
        };

        _userSubscriptionRepository.Add(subscription);
        await _userSubscriptionRepository.SaveChangesAsync();

        _logger.LogInformation(
            "[FARMER_SUBSCRIPTION_TRIAL_REPLACED] Created new subscription after Trial cancellation");
    }
    else
    {
        // Update existing non-Trial subscription
        existingSubscription.SubscriptionTierId = message.SubscriptionTierId;
        existingSubscription.StartDate = DateTime.Now;
        existingSubscription.EndDate = DateTime.Now.AddDays(message.DurationDays);
        // ... update fields ...
    }
}
```

---

## 🔄 Queue Behavior Matrix

| Current Subscription | New Subscription | Handler | Behavior |
|---------------------|------------------|---------|----------|
| **None** | Any | All | Create new active subscription |
| **Trial** | Paid | Payment | Cancel Trial → Create paid (active) |
| **Trial** | Sponsored | Code Redemption | Cancel Trial → Create sponsored (active) |
| **Trial** | Any | Admin Assignment | Cancel Trial → Create new (active) |
| **Trial** | Any | Bulk Worker | Cancel Trial → Create new (active/pending based on AutoActivate flag) |
| **Paid** | Paid | Payment | Extend duration of existing subscription |
| **Paid** | Sponsored | Code Redemption | Queue sponsored (pending) |
| **Paid** | Any | Admin Assignment | Error (require ForceActivation=true) |
| **Sponsored** | Sponsored | Code Redemption | Queue new sponsored (pending) |
| **Sponsored** | Sponsored | Admin Assignment | Queue (default) or force activate (with flag) |

---

## ✅ Testing Scenarios

### Scenario 1: Trial User Receives Admin Assignment
**Steps**:
1. User registers → Trial subscription auto-created (`IsTrialSubscription=true`)
2. Admin calls `POST /api/admin/subscriptions/assign` with TierId=2 (Small)
3. Verify Trial subscription: `IsActive=false`, `Status="Cancelled"`, `EndDate=now`
4. Verify new subscription: `IsActive=true`, `Status="Active"`, `IsTrialSubscription=false`
5. User calls `/api/v1/subscriptions/usage-status` → Should show Small subscription

**Expected**: Trial replaced immediately, new subscription active ✅

---

### Scenario 2: Trial User Redeems Sponsorship Code
**Steps**:
1. User has Trial subscription
2. User calls `POST /api/v1/sponsorship/redeem` with valid code
3. Verify Trial subscription: `IsActive=false`, `Status="Upgraded"`, `QueueStatus=Expired`
4. Verify sponsored subscription: `IsActive=true`, `IsSponsoredSubscription=true`
5. User calls `/api/v1/subscriptions/usage-status` → Should show sponsored subscription

**Expected**: Trial replaced immediately, sponsored subscription active ✅

---

### Scenario 3: Trial User Purchases Paid Subscription
**Steps**:
1. User has Trial subscription
2. User completes payment via Iyzico
3. Payment callback executes `ProcessFarmerSubscriptionAsync()`
4. Verify Trial subscription: `IsActive=false`, `Status="Upgraded"`, `CancellationReason` contains transaction ID
5. Verify paid subscription: `IsActive=true`, `PaymentMethod="CreditCard"`
6. User calls `/api/v1/subscriptions/usage-status` → Should show paid subscription

**Expected**: Trial replaced immediately, paid subscription active ✅

---

### Scenario 4: Trial User in Bulk Assignment
**Steps**:
1. User has Trial subscription
2. Admin uploads Excel with user's email/phone and TierId=3
3. Worker processes `FarmerSubscriptionAssignmentQueueMessage`
4. Verify Trial subscription: `IsActive=false`, `Status="Upgraded"`, `QueueStatus=Expired`
5. Verify new subscription: `IsActive=true` (if AutoActivate=true), `PaymentMethod="AdminAssignment"`
6. User calls `/api/v1/subscriptions/usage-status` → Should show new subscription

**Expected**: Trial replaced immediately, new subscription active/pending based on AutoActivate ✅

---

### Scenario 5: Paid User Redeems Sponsorship Code
**Steps**:
1. User has active paid subscription (expires in 20 days)
2. User redeems sponsorship code
3. Verify existing subscription: `IsActive=true`, `QueueStatus=Active` (unchanged)
4. Verify sponsored subscription: `IsActive=true`, `QueueStatus=Pending`, `StartDate > existing.EndDate`
5. Verify `QueuedAfterSubscriptionId = existing.Id`

**Expected**: Sponsored subscription queued, will activate when paid expires ✅

---

### Scenario 6: Paid User Receives Admin Assignment Without ForceActivation
**Steps**:
1. User has active paid subscription
2. Admin calls `POST /api/admin/subscriptions/assign` without `ForceActivation=true`
3. Verify response: Error message with subscription details
4. Existing subscription unchanged

**Expected**: Error returned, no changes ✅

---

## 🚨 Why This Bug Keeps Recurring

### Historical Pattern
- **First occurrence**: Fixed in Commit 5eb14dc (Active subscription ordering)
- **Second occurrence**: Fixed in Commit 4b8df199 (Admin assignment Trial cancellation)
- **Third occurrence**: Fixed in this commit (Bulk worker Trial cancellation)

### Root Cause
**Architectural Reality**: 4 independent handlers create subscriptions:
1. `AssignSubscriptionCommand` - MediatR command handler
2. `IyzicoPaymentService` - Payment callback service
3. `SponsorshipService` - Code redemption business logic
4. `FarmerSubscriptionAssignmentJobService` - RabbitMQ worker

**Each handler is independently responsible for Trial cancellation logic.**

### Why Not Centralized?
- Different execution contexts (API vs Worker Service)
- Different transaction scopes
- Different business rules (queue vs immediate, force vs error)
- Different logging and audit requirements

### Prevention Strategy
1. ✅ This documentation serves as single source of truth
2. ✅ All 4 handlers audited and fixed
3. ✅ Memory file updated with architectural explanation
4. 🔄 Future: Add integration tests for all 4 scenarios
5. 🔄 Future: Consider base class or shared validation method

---

## 📝 Code Review Checklist

When adding new subscription creation logic:

- [ ] Does it check for existing active subscription?
- [ ] Does it explicitly check `existingSubscription.IsTrialSubscription`?
- [ ] If Trial exists, does it cancel it before creating new subscription?
- [ ] Are subscription fields set correctly?
  - [ ] `IsActive` properly set
  - [ ] `Status` set to "Active", "Cancelled", "Upgraded", or "Pending"
  - [ ] `QueueStatus` set correctly
  - [ ] `IsTrialSubscription = false` for paid/sponsored subscriptions
  - [ ] `PaymentMethod` set appropriately
  - [ ] `CancellationDate` and `CancellationReason` set when cancelling
- [ ] Does `GetActiveSubscriptionByUserIdAsync()` return correct subscription after changes?
- [ ] Are audit logs created for admin operations?

---

## 🔍 Related Files

### Repository Layer
- `DataAccess/Concrete/EntityFramework/UserSubscriptionRepository.cs`
  - `GetActiveSubscriptionByUserIdAsync()` - Returns first active subscription
  - Used by `/usage-status` endpoint to determine user's current subscription

### Validation Service
- `Business/Services/Subscription/SubscriptionValidationService.cs`
  - `CheckSubscriptionStatusAsync()` - Calls repository to get active subscription
  - Returns DTO shown to user in `/usage-status`

### Controllers
- `WebAPI/Controllers/SubscriptionsController.cs`
  - `/usage-status` endpoint that users call to see current subscription
- `WebAPI/Controllers/AdminSubscriptionsController.cs`
  - `/assign` endpoint for manual admin assignment
- `WebAPI/Controllers/SponsorshipController.cs`
  - `/redeem` endpoint for code redemption

### DTOs
- `Entities/Concrete/UserSubscription.cs` - Main entity
- `Entities/Dtos/SubscriptionUsageStatusDto.cs` - Response DTO for usage-status

---

## 🎯 Success Criteria

✅ All 4 subscription creation handlers have Trial cancellation logic
✅ Build succeeds with no errors
✅ Memory file updated with architectural explanation
✅ Comprehensive documentation created
⏳ End-to-end testing (manual verification required)

---

**Commits**:
- `4b8df199` - Admin assignment Trial cancellation fix
- This commit - Bulk worker Trial cancellation fix
