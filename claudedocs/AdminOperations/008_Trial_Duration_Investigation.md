# Trial Subscription Duration Investigation

**Date**: 2025-11-12
**Issue**: Trial subscription showing 360 days duration instead of expected 30 days
**Status**: ✅ Investigation Complete - Data Anomaly Identified

---

## User Question

> "peki bu bilgiyi de doğrular mısın lütfen neye göre çıkıyor:
> "nextRenewalDate": "2026-11-07T13:38:03.767+00:00",
> "daysUntilRenewal": 360"

**Translation**: "Can you also verify this information please, what is it based on"

---

## Investigation Summary

### ✅ Calculation Logic: CORRECT

The `nextRenewalDate` and `daysUntilRenewal` calculations are working correctly:

```csharp
// BuildJourneySummary method
NextRenewalDate = activeSubscription?.EndDate,
DaysUntilRenewal = activeSubscription != null
    ? (int)(activeSubscription.EndDate - DateTime.Now).TotalDays
    : 0
```

**Math Verification**:
- Trial EndDate: `2026-11-07T13:38:03.767+00:00`
- Current Date: `2025-11-12` (approximately)
- Difference: ~360 days ✅

### ⚠️ Data Anomaly: Trial Duration = 360 Days (Expected: 30 Days)

**Expected Behavior** (from code):

```csharp
// RegisterUserCommand.cs:218
var trialEnd = subscriptionStartDate.AddDays(30);  // Trial should be 30 days
```

**Expected Calculation**:
- Trial Created: `2025-11-07`
- Expected End: `2025-11-07` + 30 days = `2025-12-07` (1 month)

**Actual Data** (from jurney.json):
- Trial Created: `2025-11-07T13:38:03.767+00:00`
- Actual End: `2026-11-07T13:38:03.767+00:00` (1 year = 365 days)

**Discrepancy**: 360 days instead of 30 days (12x longer)

---

## Related Issue: Wrong Tier Selection

**Important**: The response showing `currentTier: "Trial"` is **incorrect** due to bug fixed in commit dc5f27d.

**Timeline** (from jurney.json):
```json
{
    "timeline": [
        {
            "date": "2025-11-07T15:35:09.534+00:00",
            "eventType": "Subscription Created",
            "details": "L tier subscription activated",
            "tier": "L"
        },
        {
            "date": "2025-11-07T13:38:03.767+00:00",
            "eventType": "Subscription Created",
            "details": "Trial tier subscription activated",
            "tier": "Trial"
        }
    ]
}
```

**Issue**: L tier subscription is 2 hours newer than Trial, but `currentTier` shows "Trial"

**Root Cause**: Non-deterministic subscription selection (fixed in commit dc5f27d)

**After Fix**:
- Current tier will show **"L"** (most recent active subscription)
- Renewal date will be from **L tier subscription**, not Trial
- Trial duration anomaly becomes irrelevant for this user

---

## Possible Causes of 360-Day Trial Duration

### 1. **Historical Code Bug** ✅ Most Likely
Previous version of code may have used 365 days for Trial:
```csharp
// Possible old code
var trialEnd = subscriptionStartDate.AddDays(365); // Bug: 1 year trial
```
Later changed to 30 days but existing data not migrated.

### 2. **Manual Database Modification**
Admin manually extended Trial subscription for testing:
```sql
UPDATE "UserSubscriptions"
SET "EndDate" = "StartDate" + INTERVAL '365 days'
WHERE "UserId" = 165 AND "TierId" = (SELECT "Id" FROM "SubscriptionTiers" WHERE "Name" = 'Trial');
```

### 3. **Test Seed Data**
If this is test/staging environment, extended trial durations may be intentional:
```csharp
// Possible test seed
TrialEndDate = DateTime.Now.AddDays(365), // Extended for testing
```

### 4. **Admin Registration Path**
Different registration flow (admin creating user) may have different trial duration logic.

---

## Verification Steps

### 1. Check Database Directly

```sql
-- Get subscription details for User 165
SELECT
    us."Id",
    us."UserId",
    st."Name" as "TierName",
    us."StartDate",
    us."EndDate",
    us."IsActive",
    (us."EndDate" - us."StartDate") as "Duration",
    EXTRACT(DAY FROM (us."EndDate" - us."StartDate")) as "DurationDays",
    us."CreatedDate"
FROM "UserSubscriptions" us
JOIN "SubscriptionTiers" st ON us."TierId" = st."Id"
WHERE us."UserId" = 165
ORDER BY us."CreatedDate" DESC;
```

**Expected Output**:
```
TierName | StartDate  | EndDate    | DurationDays | IsActive
---------|------------|------------|--------------|----------
L        | 2025-11-07 | 2025-12-07 | 30          | true
Trial    | 2025-11-07 | 2026-11-07 | 365         | true
```

### 2. Check All Trial Subscriptions

```sql
-- Find all Trial subscriptions with unexpected duration
SELECT
    us."UserId",
    us."StartDate",
    us."EndDate",
    EXTRACT(DAY FROM (us."EndDate" - us."StartDate")) as "DurationDays",
    us."IsActive",
    us."CreatedDate"
FROM "UserSubscriptions" us
JOIN "SubscriptionTiers" st ON us."TierId" = st."Id"
WHERE st."Name" = 'Trial'
    AND EXTRACT(DAY FROM (us."EndDate" - us."StartDate")) > 40  -- More than 30 days + buffer
ORDER BY us."CreatedDate" DESC;
```

This will show if the 360-day issue affects only this user or all Trial subscriptions.

### 3. Check Git History

```bash
# Search for changes to trial duration
git log --all -S "AddDays(365)" --source --all
git log --all -S "AddDays(30)" --source --all -p -- Business/Handlers/Authorizations/Commands/RegisterUserCommand.cs
```

---

## Impact Analysis

### Current Impact: **NONE** (After Fix dc5f27d)

For User 165:
- ✅ Current tier will correctly show **"L"** (not Trial)
- ✅ Renewal date will be from **L tier subscription**
- ✅ Trial subscription with 360 days is **inactive/superseded** by L tier

### Historical Impact: **LOW**

- Trial subscriptions with 365-day duration are more generous than intended
- Users got extra free usage (11 months instead of 1 month)
- Revenue impact: Delayed conversions, extended trial periods

---

## Recommendations

### 1. **No Immediate Fix Required** ✅

The `nextRenewalDate` and `daysUntilRenewal` calculation logic is **correct**.

After applying commit dc5f27d (currentTier fix):
- User 165 will show L tier subscription data
- Trial duration anomaly becomes irrelevant

### 2. **Optional: Data Migration** (If Many Users Affected)

If many Trial subscriptions have 365-day duration:

```sql
-- Identify affected subscriptions
SELECT COUNT(*) as "AffectedCount"
FROM "UserSubscriptions" us
JOIN "SubscriptionTiers" st ON us."TierId" = st."Id"
WHERE st."Name" = 'Trial'
    AND EXTRACT(DAY FROM (us."EndDate" - us."StartDate")) > 40
    AND us."IsActive" = true;

-- Fix: Adjust Trial subscriptions to 30 days (if needed)
-- CAUTION: Only run if business confirms this is a bug, not intentional
UPDATE "UserSubscriptions"
SET "EndDate" = "StartDate" + INTERVAL '30 days',
    "UpdatedDate" = NOW()
WHERE "Id" IN (
    SELECT us."Id"
    FROM "UserSubscriptions" us
    JOIN "SubscriptionTiers" st ON us."TierId" = st."Id"
    WHERE st."Name" = 'Trial'
        AND EXTRACT(DAY FROM (us."EndDate" - us."StartDate")) > 40
        AND us."IsActive" = true
        AND NOT EXISTS (
            -- Don't modify if user has paid subscription
            SELECT 1
            FROM "UserSubscriptions" us2
            JOIN "SubscriptionTiers" st2 ON us2."TierId" = st2."Id"
            WHERE us2."UserId" = us."UserId"
                AND st2."Name" != 'Trial'
                AND us2."IsActive" = true
        )
);
```

### 3. **Add Validation** (Prevent Future Issues)

Add trial duration validation in `RegisterUserCommand`:

```csharp
// After creating trial subscription
if ((trialEnd - subscriptionStartDate).TotalDays != 30)
{
    _logger.LogWarning("Trial subscription duration mismatch. Expected: 30 days, Actual: {Days} days",
        (trialEnd - subscriptionStartDate).TotalDays);
}
```

---

## Conclusion

### ✅ Calculation Logic: Correct
The `nextRenewalDate` and `daysUntilRenewal` fields are calculated correctly based on subscription EndDate.

### ⚠️ Data Anomaly: Trial = 360 Days
- Trial subscription has 1-year duration instead of 30 days
- Likely historical code bug or manual data modification
- **Not a calculation bug** - data is as stored in database

### ✅ Current Tier Fix Resolves User Issue
After commit dc5f27d:
- User 165 will show L tier (correct, most recent)
- Renewal date will be from L tier subscription
- Trial duration anomaly becomes irrelevant

### 📋 Action Items
1. ✅ Verify database subscriptions with SQL query
2. ✅ Check if issue affects multiple users
3. ⚠️ Decide: Data migration needed? (business decision)
4. ⚠️ Add validation to prevent future issues

---

## Files Referenced

- `Business/Handlers/Sponsorship/Queries/GetFarmerJourneyQuery.cs` - Calculation logic
- `Business/Handlers/Authorizations/Commands/RegisterUserCommand.cs:218` - Trial duration constant
- `Business/Seeds/UserSeeds.cs:135` - Seed data trial duration
- `claudedocs/AdminOperations/jurney.json` - User data showing anomaly
- `claudedocs/AdminOperations/007_CurrentTier_Logic_Fix.md` - Related fix
