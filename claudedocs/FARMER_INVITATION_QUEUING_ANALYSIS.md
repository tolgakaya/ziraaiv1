# Farmer Invitation Queuing Analysis

## Executive Summary

**CRITICAL ISSUE IDENTIFIED**: The farmer invitation acceptance flow does NOT implement subscription queuing when the farmer already has an active subscription. This violates the expected behavior where new subscriptions should be queued as "Pending" when an active subscription exists.

## Current Behavior vs Expected Behavior

### Expected Behavior (Regular Code Redemption)
When a farmer redeems a sponsorship code via `RedeemSponsorshipCodeCommand`:

1. **Check for Active Subscription**: Calls `GetActiveNonTrialSubscriptionAsync()` to check if farmer has an active paid subscription
2. **Queue if Active**: If active subscription exists → Create Pending subscription via `QueueSponsorship()`
3. **Direct Activate if None**: If no active subscription → Create Active subscription via `ActivateSponsorship()`

### Current Behavior (Farmer Invitation Acceptance)
When a farmer accepts an invitation via `AcceptFarmerInvitationCommand`:

1. **❌ NO SUBSCRIPTION CHECK**: Does not check for existing active subscriptions
2. **❌ ONLY ASSIGNS CODES**: Only links codes to the farmer invitation (`code.FarmerInvitationId = invitation.Id`)
3. **❌ NO SUBSCRIPTION CREATION**: Does not create any `UserSubscription` record at all
4. **❌ NO QUEUING LOGIC**: Missing the entire queuing/activation flow

## Code-Level Analysis

### AcceptFarmerInvitationCommand.cs (Lines 136-155)

```csharp
// 5. Assign codes to farmer and populate statistics fields
var now = DateTime.Now;
foreach (var code in codesToAssign)
{
    // Link to farmer invitation
    code.FarmerInvitationId = invitation.Id;

    // Clear reservation fields
    code.ReservedForFarmerInvitationId = null;
    code.ReservedForFarmerAt = null;

    // CRITICAL: Populate statistics-required fields
    code.LinkSentDate = invitation.LinkSentDate ?? now;
    code.DistributionDate = now;
    code.DistributionChannel = "FarmerInvitation";
    code.DistributedTo = request.CurrentUserPhone;

    _codeRepository.Update(code);
}

await _codeRepository.SaveChangesAsync();
```

**Problems**:
1. ❌ Only sets `code.FarmerInvitationId` - does NOT create a `UserSubscription`
2. ❌ Does NOT mark code as used (`IsUsed = true`)
3. ❌ Does NOT set `code.CreatedSubscriptionId` (because no subscription is created)
4. ❌ Does NOT check for existing active subscriptions
5. ❌ Does NOT implement queuing logic

### RedeemSponsorshipCodeCommand.cs + SponsorshipService.cs (CORRECT Implementation)

```csharp
// SponsorshipService.RedeemSponsorshipCodeAsync (Lines 231-294)
public async Task<IDataResult<UserSubscription>> RedeemSponsorshipCodeAsync(string code, int userId)
{
    // 1. Validate code
    var sponsorshipCode = await _sponsorshipCodeRepository.GetUnusedCodeAsync(code);

    // 2. ✅ Check for active PAID (non-trial) subscription
    var existingSubscription = await _userSubscriptionRepository
        .GetActiveNonTrialSubscriptionAsync(userId);

    // ✅ CORRECT CONDITION: Queue if ANY paid subscription exists
    bool hasActivePaidSubscription = existingSubscription != null &&
                                      existingSubscription.IsActive &&
                                      existingSubscription.QueueStatus == SubscriptionQueueStatus.Active;

    if (hasActivePaidSubscription)
    {
        Console.WriteLine($"[SponsorshipRedeem] 🔄 Active {existingSubscription.PaymentMethod} subscription found");
        Console.WriteLine($"[SponsorshipRedeem] Queuing new sponsorship code: {code}");

        return await QueueSponsorship(code, userId, sponsorshipCode, existingSubscription.Id);
    }

    Console.WriteLine($"[SponsorshipRedeem] ✅ No active paid subscription found, direct activation");

    // Check if there's a trial subscription to deactivate
    var trialSubscription = allActiveSubscriptions.FirstOrDefault(s => s.IsTrialSubscription);

    return await ActivateSponsorship(code, userId, sponsorshipCode, trialSubscription);
}
```

### QueueSponsorship Method (Lines 299-377)

```csharp
private async Task<IDataResult<UserSubscription>> QueueSponsorship(
    string code,
    int userId,
    SponsorshipCode sponsorshipCode,
    int previousSubscriptionId)
{
    // ✅ Creates PENDING subscription
    var queuedSubscription = new UserSubscription
    {
        UserId = userId,
        SubscriptionTierId = sponsorshipCode.SubscriptionTierId,

        // ✅ Queue status
        QueueStatus = SubscriptionQueueStatus.Pending,
        IsActive = false,  // Not usable yet
        Status = "Pending",
        PreviousSponsorshipId = previousSubscriptionId,
        QueuedDate = DateTime.Now,

        // ✅ Payment info
        AutoRenew = false,
        PaymentMethod = "Sponsorship",
        PaymentReference = code,
        PaidAmount = 0,
        Currency = tier.Currency,
        CurrentDailyUsage = 0,
        CurrentMonthlyUsage = 0,

        // ✅ Sponsorship info
        IsTrialSubscription = false,
        IsSponsoredSubscription = true,
        SponsorshipCodeId = sponsorshipCode.Id,
        SponsorId = sponsorshipCode.SponsorId,

        SponsorshipNotes = $"Queued - Redeemed code: {code}. Waiting for {previousSubscription?.PaymentMethod} subscription (ID: {previousSubscriptionId}) to expire.",

        CreatedDate = DateTime.Now
    };

    _userSubscriptionRepository.Add(queuedSubscription);
    await _userSubscriptionRepository.SaveChangesAsync();

    // ✅ Mark code as used
    await _sponsorshipCodeRepository.MarkAsUsedAsync(code, userId, queuedSubscription.Id);

    return new SuccessDataResult<UserSubscription>(queuedSubscription,
        $"Sponsorluk kodunuz sıraya alındı. {subscriptionTypeMessage} bittiğinde otomatik olarak aktif olacak.");
}
```

### ActivateSponsorship Method (Lines 382-459)

```csharp
private async Task<IDataResult<UserSubscription>> ActivateSponsorship(
    string code,
    int userId,
    SponsorshipCode sponsorshipCode,
    UserSubscription existingSubscription)
{
    // ✅ Deactivate existing trial subscription if present
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

    // ✅ Create ACTIVE subscription
    var subscription = new UserSubscription
    {
        UserId = userId,
        SubscriptionTierId = sponsorshipCode.SubscriptionTierId,
        StartDate = DateTime.Now,
        EndDate = DateTime.Now.AddDays(30), // Default 30 days for sponsored subscriptions
        QueueStatus = SubscriptionQueueStatus.Active,
        ActivatedDate = DateTime.Now,
        IsActive = true,
        AutoRenew = false,
        PaymentMethod = "Sponsorship",
        PaymentReference = code,
        PaidAmount = 0,
        Currency = tier.Currency,
        CurrentDailyUsage = 0,
        CurrentMonthlyUsage = 0,
        Status = "Active",
        IsTrialSubscription = false,
        IsSponsoredSubscription = true,
        SponsorshipCodeId = sponsorshipCode.Id,
        SponsorId = sponsorshipCode.SponsorId,
        SponsorshipNotes = $"Redeemed code: {code}",
        CreatedDate = DateTime.Now
    };

    _userSubscriptionRepository.Add(subscription);
    await _userSubscriptionRepository.SaveChangesAsync();

    // ✅ Mark code as used
    await _sponsorshipCodeRepository.MarkAsUsedAsync(code, userId, subscription.Id);

    Console.WriteLine($"[SponsorshipRedeem] ✅ Code {code} successfully activated for user {userId}");

    return new SuccessDataResult<UserSubscription>(subscription,
        "Sponsorluk aktivasyonu tamamlandı!");
}
```

## Critical Differences Table

| Aspect | Regular Code Redemption | Farmer Invitation Acceptance |
|--------|------------------------|------------------------------|
| **Checks for existing subscription** | ✅ Yes (`GetActiveNonTrialSubscriptionAsync`) | ❌ No |
| **Creates UserSubscription** | ✅ Yes (Active or Pending) | ❌ No |
| **Queuing logic** | ✅ Yes (`QueueSponsorship`) | ❌ No |
| **Direct activation logic** | ✅ Yes (`ActivateSponsorship`) | ❌ No |
| **Marks code as used** | ✅ Yes (`MarkAsUsedAsync`) | ❌ No |
| **Sets code.CreatedSubscriptionId** | ✅ Yes | ❌ No |
| **Sets code.IsUsed** | ✅ Yes | ❌ No |
| **Only links code to invitation** | ❌ No | ✅ Yes (that's all it does) |

## Root Cause

The farmer invitation acceptance flow was designed ONLY to **assign codes to the invitation** (`code.FarmerInvitationId`), but **NOT to create subscriptions**. This appears to be an incomplete implementation.

### Evidence from SponsorshipCode.cs (Lines 37-41)

```csharp
// Farmer Invitation System (new fields for farmer invitation acceptance flow)
/// <summary>
/// FarmerInvitation ID when this code was assigned through invitation acceptance.
/// Links the code to the specific farmer invitation that was accepted.
/// </summary>
public int? FarmerInvitationId { get; set; }
```

This field exists ONLY for tracking/analytics purposes (linking codes to invitations), but **does NOT indicate code usage or subscription creation**.

## What Happens After Acceptance?

**Critical Question**: How do farmers actually USE the codes they receive from invitations?

### Hypothesis 1: Farmers manually redeem codes later
- After accepting invitation, farmers receive the actual code strings in the response
- They must then manually enter these codes via the regular redemption flow
- This would trigger the queuing logic properly
- **Problem**: This creates terrible UX and duplicate work

### Hypothesis 2: Incomplete implementation
- The acceptance flow was intended to create subscriptions directly
- But the subscription creation logic was never implemented
- Codes are assigned but never actually "used"
- **This is the most likely scenario based on the code evidence**

## Required Fix

The `AcceptFarmerInvitationCommand` handler needs to be refactored to:

1. **Inject SponsorshipService** (or UserSubscriptionRepository)
2. **Check for existing active subscriptions** using `GetActiveNonTrialSubscriptionAsync()`
3. **For EACH code assigned**, either:
   - Call `QueueSponsorship()` if active subscription exists
   - Call `ActivateSponsorship()` if no active subscription
4. **Handle the case of MULTIPLE codes** from the invitation (e.g., 5 codes → need to queue/activate ALL 5)

### Proposed Implementation Pattern

```csharp
// After assigning codes to invitation (line 155)
await _codeRepository.SaveChangesAsync();

// NEW: Create subscriptions for each code
var existingSubscription = await _userSubscriptionRepository
    .GetActiveNonTrialSubscriptionAsync(request.CurrentUserId);

bool hasActivePaidSubscription = existingSubscription != null &&
                                  existingSubscription.IsActive &&
                                  existingSubscription.QueueStatus == SubscriptionQueueStatus.Active;

var createdSubscriptions = new List<UserSubscription>();

foreach (var code in codesToAssign)
{
    UserSubscription subscription;

    if (hasActivePaidSubscription)
    {
        // Queue the subscription
        subscription = await CreateQueuedSubscription(code, request.CurrentUserId, existingSubscription.Id);
    }
    else
    {
        // Activate directly (and deactivate trial if exists)
        subscription = await CreateActiveSubscription(code, request.CurrentUserId);

        // After first activation, subsequent codes should be queued
        hasActivePaidSubscription = true;
        existingSubscription = subscription;
    }

    // Mark code as used
    await _sponsorshipCodeRepository.MarkAsUsedAsync(code.Code, request.CurrentUserId, subscription.Id);

    createdSubscriptions.Add(subscription);
}

await _userSubscriptionRepository.SaveChangesAsync();
```

### Alternative: Delegate to SponsorshipService

```csharp
// After assigning codes to invitation (line 155)
await _codeRepository.SaveChangesAsync();

// NEW: Use existing SponsorshipService for each code
var createdSubscriptions = new List<UserSubscription>();

foreach (var code in codesToAssign)
{
    var result = await _sponsorshipService.RedeemSponsorshipCodeAsync(code.Code, request.CurrentUserId);

    if (result.Success && result.Data != null)
    {
        createdSubscriptions.Add(result.Data);
    }
    else
    {
        _logger.LogError("Failed to redeem code {Code} for user {UserId}: {Message}",
            code.Code, request.CurrentUserId, result.Message);
    }
}
```

**Recommendation**: Use the delegation approach to avoid code duplication and ensure consistent queuing logic.

## Testing Scenarios

### Scenario 1: Farmer with NO active subscription accepts invitation
- **Expected**: First code creates Active subscription, remaining codes create Pending subscriptions
- **Current**: Codes are assigned but NO subscriptions are created

### Scenario 2: Farmer with Trial subscription accepts invitation
- **Expected**: Trial deactivated, first code creates Active subscription, remaining codes create Pending subscriptions
- **Current**: Codes are assigned but NO subscriptions are created, Trial remains active

### Scenario 3: Farmer with Active Sponsorship subscription accepts invitation
- **Expected**: ALL codes create Pending subscriptions queued after current subscription
- **Current**: Codes are assigned but NO subscriptions are created, existing subscription unaffected

### Scenario 4: Farmer accepts invitation with 5 codes
- **Expected**: 5 separate UserSubscription records (1 Active + 4 Pending, or 5 Pending if active sub exists)
- **Current**: 0 UserSubscription records, just 5 codes linked to invitation

## Conclusion

**CRITICAL BUG CONFIRMED**: The farmer invitation acceptance flow does NOT create subscriptions and does NOT implement queuing logic. This is a fundamental gap in the implementation that violates the expected behavior described by the user.

The codes assigned through farmer invitations are essentially "orphaned" - they are linked to the invitation (`FarmerInvitationId`) but are never marked as used (`IsUsed = false`) and never create subscriptions (`CreatedSubscriptionId = NULL`).

**Immediate Action Required**: Refactor `AcceptFarmerInvitationCommand` to either:
1. Delegate to `SponsorshipService.RedeemSponsorshipCodeAsync()` for each code (RECOMMENDED)
2. Implement the complete queuing/activation logic directly in the handler

This fix will ensure that farmer invitation acceptance behaves identically to regular code redemption in terms of subscription queuing and activation.
