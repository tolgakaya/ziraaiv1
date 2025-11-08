# Admin Subscription Assignment with Queue Control

**Date:** 2025-12-26
**Feature:** Admin Assign Subscription Queue Control
**Status:** ✅ IMPLEMENTED

---

## Overview

Admin subscription assignment now includes intelligent queue control to prevent multiple active sponsorships, matching the behavior of user-initiated code redemption while providing admin override capabilities.

### Problem Solved

**Before:** Admin could assign sponsorships that created conflicts with existing active sponsorships, resulting in users having multiple active sponsorships simultaneously.

**After:** Admin assignment respects queue rules by default, with optional force override for emergency situations.

---

## Implementation Summary

### Three Execution Paths

#### 1. **Immediate Activation** (No Conflict)
- User has NO active sponsorship
- New sponsorship activates immediately
- Same as before (backward compatible)

#### 2. **Queue Mode** (Default - NEW)
- User has active sponsorship
- `ForceActivation = false` (default)
- New sponsorship queued until current expires
- Auto-activates when previous sponsorship ends

#### 3. **Force Override Mode** (Admin Power - NEW)
- User has active sponsorship
- `ForceActivation = true`
- Current sponsorship cancelled immediately
- New sponsorship activated instantly
- **Use with caution** - terminates existing sponsorship

---

## API Documentation

### Endpoint

```
POST /api/admin/subscriptions/assign
```

### Request Body

```json
{
  "userId": 165,
  "subscriptionTierId": 5,
  "durationMonths": 12,
  "isSponsoredSubscription": true,
  "sponsorId": 159,
  "notes": "2025 sponsorship campaign",
  "forceActivation": false  // NEW PARAMETER (default: false)
}
```

### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| userId | int | Yes | - | User ID to assign subscription to |
| subscriptionTierId | int | Yes | - | Tier ID (1=Trial, 2=S, 3=M, 4=L, 5=XL) |
| durationMonths | int | Yes | - | Subscription duration in months |
| isSponsoredSubscription | bool | Yes | - | Whether this is a sponsored subscription |
| sponsorId | int | No | null | Sponsor user ID (required if sponsored) |
| notes | string | No | null | Admin notes about the assignment |
| **forceActivation** | **bool** | **No** | **false** | **Cancel existing sponsorship and activate immediately** |

---

## Behavior Matrix

### Scenario 1: No Active Sponsorship

**Setup:**
- User ID=165 has NO active sponsorship (or has Trial/Regular subscription)

**Request:**
```json
{
  "userId": 165,
  "subscriptionTierId": 5,
  "durationMonths": 12,
  "isSponsoredSubscription": true,
  "forceActivation": false
}
```

**Result:**
```
✅ Subscription assigned successfully. Valid until 2026-12-26
```

**Database State:**
- New subscription: IsActive=true, Status="Active", QueueStatus=Active
- Activates immediately

---

### Scenario 2: Active Sponsorship + Queue Mode (Default)

**Setup:**
- User ID=165 has active Sponsor L (expires 2026-06-30)

**Request:**
```json
{
  "userId": 165,
  "subscriptionTierId": 5,
  "durationMonths": 12,
  "isSponsoredSubscription": true,
  "forceActivation": false  // DEFAULT
}
```

**Result:**
```
✅ Subscription queued successfully. Will activate automatically on 2026-06-30 when current sponsorship expires.
```

**Database State:**
- Existing: IsActive=true, Status="Active", EndDate=2026-06-30
- **New (Queued):**
  - IsActive=false
  - Status="Pending"
  - QueueStatus=Pending
  - PreviousSponsorshipId=42
  - StartDate=DateTime.MinValue (placeholder)
  - EndDate=DateTime.MinValue (placeholder)
  - QueuedDate=2025-12-26

**Auto-Activation:**
When previous sponsorship expires (2026-06-30), `ProcessExpiredSubscriptionsAsync()` automatically:
1. Sets old subscription: IsActive=false, Status="Expired", QueueStatus=Expired
2. Activates queued: IsActive=true, Status="Active", QueueStatus=Active, StartDate=NOW, EndDate=NOW+12 months

---

### Scenario 3: Active Sponsorship + Force Override

**Setup:**
- User ID=165 has active Sponsor L (expires 2026-06-30)

**Request:**
```json
{
  "userId": 165,
  "subscriptionTierId": 5,
  "durationMonths": 12,
  "isSponsoredSubscription": true,
  "forceActivation": true  // OVERRIDE
}
```

**Result:**
```
✅ Previous sponsorship cancelled. New XL subscription activated. Valid until 2026-12-26
```

**Database State - Existing (Cancelled):**
- IsActive=false
- Status="Cancelled"
- QueueStatus=Cancelled
- EndDate=2025-12-26 (NOW - terminated immediately)
- UpdatedDate=2025-12-26

**Database State - New (Active):**
- IsActive=true
- Status="Active"
- QueueStatus=Active
- StartDate=2025-12-26
- EndDate=2026-12-26
- ActivatedDate=2025-12-26

---

## Frontend Integration

### Basic Usage (Queue Mode)

```typescript
async function assignSubscription(
  userId: number,
  tierId: number,
  durationMonths: number,
  sponsorId?: number,
  notes?: string
) {
  const response = await fetch('/api/admin/subscriptions/assign', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${adminToken}`,
      'Content-Type': 'application/json',
      'x-dev-arch-version': '1.0'
    },
    body: JSON.stringify({
      userId,
      subscriptionTierId: tierId,
      durationMonths,
      isSponsoredSubscription: !!sponsorId,
      sponsorId,
      notes,
      forceActivation: false // Queue by default
    })
  });

  const result = await response.json();

  if (result.success) {
    if (result.message.includes('queued')) {
      showNotification('Subscription queued', 'Will activate when current expires', 'info');
    } else {
      showNotification('Subscription activated', result.message, 'success');
    }
  }
}
```

### Force Override with Confirmation

```typescript
async function forceAssignSubscription(
  userId: number,
  tierId: number,
  durationMonths: number,
  sponsorId?: number
) {
  // IMPORTANT: Show confirmation dialog
  const confirmed = await showConfirmDialog({
    title: 'Force Activation Warning',
    message: 'This will cancel the user\'s current active sponsorship immediately. Are you sure?',
    confirmText: 'Yes, Cancel Current & Activate New',
    cancelText: 'No, Queue Instead',
    severity: 'warning'
  });

  if (!confirmed) return;

  const response = await fetch('/api/admin/subscriptions/assign', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${adminToken}`,
      'Content-Type': 'application/json',
      'x-dev-arch-version': '1.0'
    },
    body: JSON.stringify({
      userId,
      subscriptionTierId: tierId,
      durationMonths,
      isSponsoredSubscription: !!sponsorId,
      sponsorId,
      forceActivation: true // FORCE OVERRIDE
    })
  });

  const result = await response.json();

  if (result.success) {
    showNotification('Forced Activation', 'Previous sponsorship cancelled', 'success');
  }
}
```

### Recommended UI Flow

```typescript
interface AssignmentFormData {
  userId: number;
  tierId: number;
  durationMonths: number;
  sponsorId?: number;
  notes?: string;
}

async function handleAssignSubmit(data: AssignmentFormData) {
  // Step 1: Check if user has active sponsorship
  const activeSponsorship = await checkActiveSponsorship(data.userId);

  if (!activeSponsorship) {
    // No conflict - assign immediately
    await assignSubscription(data);
    return;
  }

  // Step 2: Show conflict resolution dialog
  const choice = await showDialog({
    title: 'Active Sponsorship Detected',
    message: `User has active ${activeSponsorship.tierName} sponsorship until ${activeSponsorship.endDate}`,
    options: [
      { value: 'queue', label: 'Queue for Later', description: 'Activate automatically when current expires', recommended: true },
      { value: 'force', label: 'Cancel & Replace Now', description: 'Terminate current and activate immediately', danger: true },
      { value: 'cancel', label: 'Cancel Operation' }
    ]
  });

  if (choice === 'queue') {
    await assignSubscription(data); // forceActivation=false
  } else if (choice === 'force') {
    await forceAssignSubscription(data); // forceActivation=true
  }
}
```

---

## Audit Logging

### Action Types

Three distinct audit log actions for tracking:

1. **AssignSubscription** - Immediate activation (no conflict)
```json
{
  "action": "AssignSubscription",
  "reason": "Assigned XL subscription for 12 months",
  "afterState": {
    "id": 123,
    "subscriptionTierId": 5,
    "startDate": "2025-12-26",
    "endDate": "2026-12-26"
  }
}
```

2. **AssignSubscription_Queued** - Queue mode
```json
{
  "action": "AssignSubscription_Queued",
  "reason": "Queued XL subscription for 12 months (will activate after subscription 42 expires)",
  "afterState": {
    "id": 124,
    "subscriptionTierId": 5,
    "queueStatus": "Pending",
    "previousSponsorshipId": 42,
    "estimatedActivation": "2026-06-30"
  }
}
```

3. **AssignSubscription_ForceActivation** - Force override
```json
{
  "action": "AssignSubscription_ForceActivation",
  "reason": "Force activated XL subscription for 12 months (cancelled subscription 42)",
  "afterState": {
    "newSubscription": {
      "id": 125,
      "subscriptionTierId": 5,
      "startDate": "2025-12-26",
      "endDate": "2026-12-26"
    },
    "cancelledSubscription": {
      "id": 42,
      "endDate": "2025-12-26"
    }
  }
}
```

---

## Testing Scenarios

### Test 1: Queue Mode (Default Behavior)

**Setup:**
```sql
-- User 165 has active Sponsor L until 2026-06-30
INSERT INTO "UserSubscriptions" (UserId, SubscriptionTierId, StartDate, EndDate, IsActive, Status, IsSponsoredSubscription, QueueStatus)
VALUES (165, 4, '2025-06-30', '2026-06-30', true, 'Active', true, 1);
```

**Request:**
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/admin/subscriptions/assign" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "userId": 165,
    "subscriptionTierId": 5,
    "durationMonths": 12,
    "isSponsoredSubscription": true,
    "sponsorId": 159,
    "forceActivation": false
  }'
```

**Expected:**
```json
{
  "success": true,
  "message": "Subscription queued successfully. Will activate automatically on 2026-06-30 when current sponsorship expires."
}
```

**Database Verification:**
```sql
-- Queued subscription should exist
SELECT * FROM "UserSubscriptions"
WHERE UserId=165 AND QueueStatus=0 -- Pending
AND PreviousSponsorshipId IS NOT NULL;
```

---

### Test 2: Force Override

**Setup:** Same as Test 1

**Request:**
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/admin/subscriptions/assign" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "userId": 165,
    "subscriptionTierId": 5,
    "durationMonths": 12,
    "isSponsoredSubscription": true,
    "sponsorId": 159,
    "forceActivation": true
  }'
```

**Expected:**
```json
{
  "success": true,
  "message": "Previous sponsorship cancelled. New XL subscription activated. Valid until 2026-12-26"
}
```

**Database Verification:**
```sql
-- Old subscription should be cancelled
SELECT * FROM "UserSubscriptions"
WHERE UserId=165 AND SubscriptionTierId=4
AND Status='Cancelled' AND QueueStatus=3; -- Cancelled

-- New subscription should be active
SELECT * FROM "UserSubscriptions"
WHERE UserId=165 AND SubscriptionTierId=5
AND IsActive=true AND Status='Active' AND QueueStatus=1; -- Active
```

---

### Test 3: No Conflict (Backward Compatible)

**Setup:**
```sql
-- User 170 has NO active sponsorship
-- (or has Trial/Regular subscription)
```

**Request:**
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/admin/subscriptions/assign" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "userId": 170,
    "subscriptionTierId": 3,
    "durationMonths": 6,
    "isSponsoredSubscription": true,
    "sponsorId": 159
  }'
```

**Expected:**
```json
{
  "success": true,
  "message": "Subscription assigned successfully. Valid until 2026-06-26"
}
```

**Note:** ForceActivation parameter ignored when no conflict exists.

---

## Code Changes Summary

### Files Modified

1. **Business/Handlers/AdminSubscriptions/Commands/AssignSubscriptionCommand.cs**
   - Added `ForceActivation` parameter (default: false)
   - Added active sponsorship detection
   - Split logic into 3 methods:
     - `HandleImmediateActivation()` - No conflict
     - `HandleQueueSponsorship()` - Queue mode
     - `HandleForceActivation()` - Override mode
   - Enhanced audit logging with 3 action types

2. **WebAPI/Controllers/AdminSubscriptionsController.cs**
   - Added `ForceActivation` to `AssignSubscriptionRequest` DTO
   - Added Swagger documentation with queue control remarks
   - Mapped ForceActivation to command

### Build Status

✅ **Build Succeeded** - No compilation errors

---

## Security Considerations

### Force Override Restrictions

1. **Admin-Only Operation**: Only admins can assign subscriptions
2. **Audit Trail**: All force overrides logged with admin context
3. **No Cascade Deletion**: Cancelled subscriptions preserved in database for audit
4. **Explicit Opt-In**: Force override requires `forceActivation=true` parameter

### Recommended Practices

1. **Default to Queue Mode**: Use `forceActivation=false` unless emergency
2. **User Confirmation**: Frontend should show confirmation dialog for force override
3. **Document Reasons**: Use `notes` field to explain why force override was necessary
4. **Monitor Audit Logs**: Review `AssignSubscription_ForceActivation` logs regularly

---

## Comparison: Admin Assign vs Code Redeem

| Aspect | **Code Redeem (User)** | **Admin Assign (Before)** | **Admin Assign (Now)** |
|--------|----------------------|--------------------------|----------------------|
| Queue Control | ✅ Yes | ❌ No | ✅ Yes |
| Force Override | ❌ No | N/A | ✅ Yes (optional) |
| Active Sponsorship Check | ✅ Yes | ❌ No | ✅ Yes |
| Default Behavior | Queue if conflict | Immediate activation | Queue if conflict |
| Admin Power | N/A | Full control | Controlled with override option |

---

## Future Enhancements

### Potential Improvements

1. **Queue Position**: Show queue position if multiple queued sponsorships
2. **Notification**: Notify admin when queued subscription activates
3. **Bulk Assign**: Extend queue control to bulk assignment operations
4. **Queue Transfer**: Allow admin to transfer queue position between users
5. **Queue Cancellation**: Allow admin to cancel queued (not active) sponsorships

---

## Migration Notes

### Backward Compatibility

✅ **Fully Backward Compatible**

- `forceActivation` parameter optional (default: false)
- Existing API calls continue working without changes
- Old behavior (immediate activation for non-sponsored) unchanged
- Only sponsored subscriptions affected by queue control

### No Database Migration Required

All queue fields already exist in `UserSubscription` table from sponsorship queue system implementation.

---

## Troubleshooting

### Issue: Subscription Not Queuing

**Symptom:** Subscription activates immediately despite active sponsorship

**Causes:**
1. `IsSponsoredSubscription = false` - Queue control only applies to sponsored subscriptions
2. Active sponsorship check failed due to Status/QueueStatus mismatch
3. ForceActivation accidentally set to true

**Solution:**
```sql
-- Verify active sponsorship query
SELECT * FROM "UserSubscriptions"
WHERE UserId = 165
AND IsSponsoredSubscription = true
AND IsActive = true
AND Status = 'Active'
AND QueueStatus = 1 -- Active
AND EndDate > NOW();
```

---

### Issue: Force Override Not Working

**Symptom:** Previous sponsorship not cancelled

**Causes:**
1. Database transaction rollback
2. Update operation failed silently

**Solution:**
Check audit logs for `AssignSubscription_ForceActivation` action. If missing, operation failed.

---

## Contact

**Backend Team** - backend@ziraai.com
**Documentation:** claudedocs/AdminOperations/ADMIN_ASSIGN_QUEUE_CONTROL.md

---

**Implementation Date:** 2025-12-26
**Feature Branch:** feature/admin-assign-queue-control
**Target Environment:** Staging → Production

✅ **Status:** Ready for Review and Deployment
