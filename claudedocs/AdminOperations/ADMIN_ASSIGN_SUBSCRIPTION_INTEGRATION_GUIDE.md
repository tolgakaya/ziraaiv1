# Admin Assign Subscription - Complete Integration Guide

**Endpoint:** `POST /api/admin/subscriptions/assign`
**Authentication:** Required (Admin role)
**Version:** 1.0
**Feature:** Queue Control with Force Override
**Status:** ✅ Production Ready

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Endpoint Details](#endpoint-details)
3. [Request Specification](#request-specification)
4. [Response Specification](#response-specification)
5. [Execution Flow](#execution-flow)
6. [Complete Examples](#complete-examples)
7. [Error Handling](#error-handling)
8. [Database Impact](#database-impact)
9. [Frontend Integration](#frontend-integration)
10. [Testing Guide](#testing-guide)
11. [Troubleshooting](#troubleshooting)

---

## Quick Start

### Minimum Required Request
```bash
curl -X POST "https://api.ziraai.com/api/admin/subscriptions/assign" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "userId": 165,
    "subscriptionTierId": 5,
    "durationMonths": 12,
    "isSponsoredSubscription": true
  }'
```

### Response (Immediate Activation)
```json
{
  "success": true,
  "message": "Subscription assigned successfully. Valid until 2026-01-15"
}
```

---

## Endpoint Details

### HTTP Method and URL

```
POST /api/admin/subscriptions/assign
```

### Headers

| Header | Required | Description | Example |
|--------|----------|-------------|---------|
| `Authorization` | ✅ Yes | Admin JWT token | `Bearer eyJhbGci...` |
| `Content-Type` | ✅ Yes | Must be application/json | `application/json` |
| `x-dev-arch-version` | ✅ Yes | API version | `1.0` |

### Authentication & Authorization

- **Role Required:** `Admin`
- **Token Type:** JWT Bearer token
- **Claims Used:**
  - `ClaimTypes.NameIdentifier` → Admin User ID (auto-captured)
  - `ClaimTypes.Name` → Admin User Name
  - `ClaimTypes.Email` → Admin User Email

### Security Features

1. **Automatic Admin Context Capture**
   - AdminUserId, IpAddress, UserAgent, RequestPath automatically extracted from request
   - No need to send these in request body

2. **Audit Logging**
   - All operations logged to AdminAuditLog table
   - Three distinct action types for different scenarios

3. **Authorization Check**
   - `[Authorize(Roles = "Admin")]` enforced
   - `[SecuredOperation(Priority = 1)]` aspect applied

---

## Request Specification

### Request Body Schema

```typescript
interface AssignSubscriptionRequest {
  // Required Fields
  userId: number;                     // User ID to assign subscription to
  subscriptionTierId: number;         // Tier ID (1=Trial, 2=S, 3=M, 4=L, 5=XL)
  durationMonths: number;             // Duration in months (1-120)
  isSponsoredSubscription: boolean;   // Whether this is sponsored

  // Conditional Fields
  sponsorId?: number | null;          // Required if isSponsoredSubscription=true

  // Optional Fields
  notes?: string | null;              // Admin notes about assignment
  forceActivation?: boolean;          // Default: false (queue control)
}
```

### Field Details

#### userId (required)
- **Type:** `integer`
- **Description:** Target user ID to receive subscription
- **Validation:** Must exist in Users table
- **Example:** `165`

#### subscriptionTierId (required)
- **Type:** `integer`
- **Description:** Subscription tier identifier
- **Valid Values:**
  - `1` = Trial
  - `2` = S (Small)
  - `3` = M (Medium)
  - `4` = L (Large)
  - `5` = XL (Extra Large)
- **Validation:** Must exist in SubscriptionTiers table
- **Example:** `5`

#### durationMonths (required)
- **Type:** `integer`
- **Description:** Subscription duration in months
- **Range:** `1` to `120` months (1-10 years)
- **Common Values:** `1`, `3`, `6`, `12`, `24`
- **Example:** `12`

#### isSponsoredSubscription (required)
- **Type:** `boolean`
- **Description:** Whether subscription is sponsored by a company
- **Impact:**
  - `true` → Queue control applies if active sponsorship exists
  - `false` → Always activates immediately (no queue control)
- **Example:** `true`

#### sponsorId (conditional)
- **Type:** `integer | null`
- **Required When:** `isSponsoredSubscription = true`
- **Description:** Sponsor company user ID
- **Validation:** Must be valid User ID with Sponsor role
- **Example:** `159`

#### notes (optional)
- **Type:** `string | null`
- **Description:** Admin notes about the assignment
- **Max Length:** 2000 characters
- **Use Cases:**
  - Campaign tracking: "2025 Spring Campaign"
  - Reason: "Compensation for service outage"
  - Reference: "Support ticket #12345"
- **Example:** `"2025 sponsorship campaign - agricultural development"`

#### forceActivation (optional)
- **Type:** `boolean`
- **Default:** `false`
- **Description:** Control queue behavior when active sponsorship exists
- **Values:**
  - `false` (default) → Queue new sponsorship if conflict exists
  - `true` → Cancel existing sponsorship and activate new immediately
- **⚠️ Important:** Only applies to sponsored subscriptions
- **Example:** `false`

### Request Validation Rules

```typescript
// Validation pseudocode
if (isSponsoredSubscription === true && !sponsorId) {
  return "Sponsor ID is required for sponsored subscriptions";
}

if (subscriptionTierId < 1 || subscriptionTierId > 5) {
  return "Invalid subscription tier ID";
}

if (durationMonths < 1 || durationMonths > 120) {
  return "Duration must be between 1 and 120 months";
}

if (!tierExists(subscriptionTierId)) {
  return "Subscription tier not found";
}
```

---

## Response Specification

### Success Response Structure

```typescript
interface SuccessResponse {
  success: true;
  message: string;  // Human-readable success message
}
```

### HTTP Status Codes

| Status | Scenario | Response Type |
|--------|----------|---------------|
| `200 OK` | Success (any path) | SuccessResponse |
| `400 Bad Request` | Validation error or tier not found | ErrorResponse |
| `401 Unauthorized` | Missing or invalid token | Error |
| `403 Forbidden` | Not admin role | Error |

### Success Message Patterns

The `message` field varies based on execution path:

#### 1. Immediate Activation (No Conflict)
```json
{
  "success": true,
  "message": "Subscription assigned successfully. Valid until 2026-01-15"
}
```
- **Pattern:** `"Subscription assigned successfully. Valid until {EndDate:yyyy-MM-dd}"`
- **Triggers:**
  - User has NO active sponsorship
  - OR `isSponsoredSubscription = false`

#### 2. Queue Mode (Default Behavior)
```json
{
  "success": true,
  "message": "Subscription queued successfully. Will activate automatically on 2025-06-30 when current sponsorship expires."
}
```
- **Pattern:** `"Subscription queued successfully. Will activate automatically on {ExistingEndDate:yyyy-MM-dd} when current sponsorship expires."`
- **Triggers:**
  - User has active sponsorship
  - `forceActivation = false` (default)

#### 3. Force Override
```json
{
  "success": true,
  "message": "Previous sponsorship cancelled. New XL subscription activated. Valid until 2026-01-15"
}
```
- **Pattern:** `"Previous sponsorship cancelled. New {TierName} subscription activated. Valid until {NewEndDate:yyyy-MM-dd}"`
- **Triggers:**
  - User has active sponsorship
  - `forceActivation = true`

### Error Response Structure

```typescript
interface ErrorResponse {
  success: false;
  message: string;  // Human-readable error message
}
```

### Error Messages

| Message | HTTP Status | Cause | Solution |
|---------|-------------|-------|----------|
| `"Subscription tier not found"` | 400 | Invalid `subscriptionTierId` | Use valid tier ID (1-5) |
| `"Unauthorized"` | 401 | Missing/invalid token | Provide valid admin JWT token |
| `"Forbidden"` | 403 | User not admin | Use account with Admin role |

---

## Execution Flow

### Decision Tree

```
POST /api/admin/subscriptions/assign
│
├─ Validate: subscriptionTierId exists?
│  ├─ NO → Return 400 "Subscription tier not found"
│  └─ YES → Continue
│
├─ Check: isSponsoredSubscription = true?
│  ├─ NO → [Path 1] Immediate Activation
│  └─ YES → Check for active sponsorship
│     │
│     ├─ NO active sponsorship → [Path 1] Immediate Activation
│     └─ Active sponsorship exists
│        │
│        ├─ forceActivation = false (default)
│        │  └─ [Path 2] Queue Mode
│        │
│        └─ forceActivation = true
│           └─ [Path 3] Force Override
```

### Path 1: Immediate Activation

**Trigger Conditions:**
- User has NO active sponsorship
- OR `isSponsoredSubscription = false`

**Operations:**
1. Create new UserSubscription record
2. Set `IsActive = true`, `Status = "Active"`, `QueueStatus = Active`
3. Set `StartDate = NOW`, `EndDate = NOW + durationMonths`
4. Set `ActivatedDate = NOW`
5. Save to database
6. Create audit log: `"AssignSubscription"`
7. Return success message

**Database Changes:**
```sql
-- New record inserted
INSERT INTO "UserSubscriptions" (
  UserId, SubscriptionTierId, StartDate, EndDate,
  IsActive, Status, QueueStatus, ActivatedDate,
  IsSponsoredSubscription, SponsorId, SponsorshipNotes,
  CreatedDate, CreatedUserId
) VALUES (
  165, 5, '2025-01-15', '2026-01-15',
  true, 'Active', 1, '2025-01-15',
  true, 159, 'Campaign notes',
  '2025-01-15', 42
);
```

**Response:**
```json
{
  "success": true,
  "message": "Subscription assigned successfully. Valid until 2026-01-15"
}
```

---

### Path 2: Queue Mode (Default)

**Trigger Conditions:**
- User has active sponsorship
- `isSponsoredSubscription = true`
- `forceActivation = false` (default)

**Operations:**
1. Find active sponsorship (IsActive=true, Status="Active", QueueStatus=Active)
2. Create new UserSubscription record in PENDING state
3. Set `IsActive = false`, `Status = "Pending"`, `QueueStatus = Pending`
4. Set `StartDate = DateTime.MinValue`, `EndDate = DateTime.MinValue` (placeholders)
5. Set `QueuedDate = NOW`, `PreviousSponsorshipId = {activeId}`
6. Set `ActivatedDate = null` (will be set on activation)
7. Save to database
8. Create audit log: `"AssignSubscription_Queued"`
9. Return success message with activation date

**Database Changes:**
```sql
-- Existing active subscription (UNCHANGED)
SELECT * FROM "UserSubscriptions"
WHERE UserId = 165 AND QueueStatus = 1 AND IsActive = true;
-- Result: Id=42, EndDate='2025-06-30', Status='Active'

-- New queued subscription (INSERTED)
INSERT INTO "UserSubscriptions" (
  UserId, SubscriptionTierId, StartDate, EndDate,
  IsActive, Status, QueueStatus, QueuedDate,
  PreviousSponsorshipId, ActivatedDate,
  IsSponsoredSubscription, SponsorId, SponsorshipNotes,
  CreatedDate, CreatedUserId
) VALUES (
  165, 5, '0001-01-01', '0001-01-01',  -- Placeholders
  false, 'Pending', 0, '2025-01-15',
  42, NULL,
  true, 159, 'Campaign notes',
  '2025-01-15', 42
);
```

**Auto-Activation Logic:**
- When subscription ID=42 expires (2025-06-30)
- System automatically activates queued subscription
- Sets `StartDate = NOW`, `EndDate = NOW + 12 months`
- Sets `IsActive = true`, `Status = "Active"`, `QueueStatus = Active`
- Sets `ActivatedDate = NOW`

**Response:**
```json
{
  "success": true,
  "message": "Subscription queued successfully. Will activate automatically on 2025-06-30 when current sponsorship expires."
}
```

---

### Path 3: Force Override

**Trigger Conditions:**
- User has active sponsorship
- `isSponsoredSubscription = true`
- `forceActivation = true`

**Operations:**
1. Find active sponsorship
2. **Cancel existing subscription:**
   - Set `IsActive = false`
   - Set `Status = "Cancelled"`
   - Set `QueueStatus = Cancelled`
   - Set `EndDate = NOW` (terminate immediately)
   - Set `UpdatedDate = NOW`
3. **Create new active subscription:**
   - Set `IsActive = true`, `Status = "Active"`, `QueueStatus = Active`
   - Set `StartDate = NOW`, `EndDate = NOW + durationMonths`
   - Set `ActivatedDate = NOW`
4. Save both changes to database
5. Create audit log: `"AssignSubscription_ForceActivation"`
6. Return success message

**Database Changes:**
```sql
-- Existing subscription (CANCELLED)
UPDATE "UserSubscriptions"
SET
  IsActive = false,
  Status = 'Cancelled',
  QueueStatus = 3,
  EndDate = '2025-01-15',  -- NOW (terminated immediately)
  UpdatedDate = '2025-01-15'
WHERE Id = 42;

-- New subscription (ACTIVATED)
INSERT INTO "UserSubscriptions" (
  UserId, SubscriptionTierId, StartDate, EndDate,
  IsActive, Status, QueueStatus, ActivatedDate,
  IsSponsoredSubscription, SponsorId, SponsorshipNotes,
  CreatedDate, CreatedUserId
) VALUES (
  165, 5, '2025-01-15', '2026-01-15',
  true, 'Active', 1, '2025-01-15',
  true, 159, 'Campaign notes',
  '2025-01-15', 42
);
```

**Response:**
```json
{
  "success": true,
  "message": "Previous sponsorship cancelled. New XL subscription activated. Valid until 2026-01-15"
}
```

---

## Complete Examples

### Example 1: Simple Assignment (No Conflict)

**Scenario:** User 170 has NO active sponsorship. Assign XL tier for 12 months.

**Request:**
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/admin/subscriptions/assign" \
  -H "Authorization: Bearer eyJhbGci..." \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "userId": 170,
    "subscriptionTierId": 5,
    "durationMonths": 12,
    "isSponsoredSubscription": true,
    "sponsorId": 159,
    "notes": "2025 Q1 Campaign"
  }'
```

**Response:**
```json
{
  "success": true,
  "message": "Subscription assigned successfully. Valid until 2026-01-15"
}
```

**Database Result:**
```sql
-- Query to verify
SELECT Id, UserId, SubscriptionTierId, Status, QueueStatus,
       IsActive, StartDate, EndDate, ActivatedDate
FROM "UserSubscriptions"
WHERE UserId = 170
ORDER BY CreatedDate DESC
LIMIT 1;

-- Result:
-- Id=250, UserId=170, SubscriptionTierId=5, Status='Active'
-- QueueStatus=1 (Active), IsActive=true
-- StartDate='2025-01-15', EndDate='2026-01-15'
-- ActivatedDate='2025-01-15'
```

---

### Example 2: Queue Mode (Default Behavior)

**Scenario:** User 165 has active L tier until 2025-06-30. Assign XL tier for 12 months.

**Existing Subscription:**
```sql
SELECT * FROM "UserSubscriptions" WHERE UserId = 165 AND IsActive = true;
-- Id=42, SubscriptionTierId=4, EndDate='2025-06-30', Status='Active'
```

**Request:**
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/admin/subscriptions/assign" \
  -H "Authorization: Bearer eyJhbGci..." \
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

**Response:**
```json
{
  "success": true,
  "message": "Subscription queued successfully. Will activate automatically on 2025-06-30 when current sponsorship expires."
}
```

**Database Result:**
```sql
-- Existing subscription (unchanged)
SELECT * FROM "UserSubscriptions" WHERE Id = 42;
-- Status='Active', IsActive=true, QueueStatus=1, EndDate='2025-06-30'

-- New queued subscription
SELECT * FROM "UserSubscriptions" WHERE UserId = 165 AND QueueStatus = 0;
-- Id=251, Status='Pending', IsActive=false, QueueStatus=0 (Pending)
-- PreviousSponsorshipId=42, QueuedDate='2025-01-15'
-- StartDate='0001-01-01', EndDate='0001-01-01' (placeholders)
```

**Auto-Activation on 2025-06-30:**
```sql
-- System automatically executes:
UPDATE "UserSubscriptions" SET
  IsActive = false, Status = 'Expired', QueueStatus = 2
WHERE Id = 42;

UPDATE "UserSubscriptions" SET
  IsActive = true, Status = 'Active', QueueStatus = 1,
  StartDate = '2025-06-30', EndDate = '2026-06-30',
  ActivatedDate = '2025-06-30'
WHERE Id = 251;
```

---

### Example 3: Force Override

**Scenario:** User 165 has active L tier. Admin needs to replace with XL immediately.

**Request:**
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/admin/subscriptions/assign" \
  -H "Authorization: Bearer eyJhbGci..." \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "userId": 165,
    "subscriptionTierId": 5,
    "durationMonths": 12,
    "isSponsoredSubscription": true,
    "sponsorId": 159,
    "notes": "Emergency upgrade - support ticket #12345",
    "forceActivation": true
  }'
```

**Response:**
```json
{
  "success": true,
  "message": "Previous sponsorship cancelled. New XL subscription activated. Valid until 2026-01-15"
}
```

**Database Result:**
```sql
-- Old subscription (cancelled)
SELECT * FROM "UserSubscriptions" WHERE Id = 42;
-- Status='Cancelled', IsActive=false, QueueStatus=3 (Cancelled)
-- EndDate='2025-01-15' (terminated immediately)

-- New subscription (active)
SELECT * FROM "UserSubscriptions" WHERE UserId = 165 AND IsActive = true;
-- Id=252, SubscriptionTierId=5, Status='Active', IsActive=true
-- StartDate='2025-01-15', EndDate='2026-01-15'
-- ActivatedDate='2025-01-15'
```

---

### Example 4: Non-Sponsored Subscription

**Scenario:** Assign regular subscription (no queue control applies).

**Request:**
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/admin/subscriptions/assign" \
  -H "Authorization: Bearer eyJhbGci..." \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "userId": 165,
    "subscriptionTierId": 3,
    "durationMonths": 6,
    "isSponsoredSubscription": false,
    "notes": "Manual payment - invoice #2025-001"
  }'
```

**Response:**
```json
{
  "success": true,
  "message": "Subscription assigned successfully. Valid until 2025-07-15"
}
```

**Note:** Queue control does NOT apply because `isSponsoredSubscription = false`. Always activates immediately.

---

## Error Handling

### Error Scenario 1: Invalid Tier ID

**Request:**
```json
{
  "userId": 165,
  "subscriptionTierId": 99,
  "durationMonths": 12,
  "isSponsoredSubscription": true,
  "sponsorId": 159
}
```

**Response:**
```json
{
  "success": false,
  "message": "Subscription tier not found"
}
```

**HTTP Status:** `400 Bad Request`

---

### Error Scenario 2: Missing Authorization

**Request:**
```bash
curl -X POST "https://api.ziraai.com/api/admin/subscriptions/assign" \
  -H "Content-Type: application/json" \
  -d '{...}'
```

**Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

**HTTP Status:** `401 Unauthorized`

---

### Error Scenario 3: Non-Admin User

**Request:** Valid token but user role is "Farmer" or "Sponsor" (not "Admin")

**Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```

**HTTP Status:** `403 Forbidden`

---

## Database Impact

### Tables Affected

#### 1. UserSubscriptions (INSERT or UPDATE)

**Columns Modified:**

| Column | Path 1 | Path 2 | Path 3 (New) | Path 3 (Old) |
|--------|--------|--------|--------------|--------------|
| UserId | ✅ Set | ✅ Set | ✅ Set | - |
| SubscriptionTierId | ✅ Set | ✅ Set | ✅ Set | - |
| StartDate | NOW | MinValue | NOW | - |
| EndDate | NOW+X | MinValue | NOW+X | NOW |
| IsActive | true | false | true | false |
| Status | "Active" | "Pending" | "Active" | "Cancelled" |
| QueueStatus | Active(1) | Pending(0) | Active(1) | Cancelled(3) |
| IsSponsoredSubscription | ✅ Set | ✅ Set | ✅ Set | - |
| SponsorId | ✅ Set | ✅ Set | ✅ Set | - |
| SponsorshipNotes | ✅ Set | ✅ Set | ✅ Set | - |
| CreatedDate | NOW | NOW | NOW | - |
| CreatedUserId | AdminId | AdminId | AdminId | - |
| QueuedDate | null | NOW | null | - |
| ActivatedDate | NOW | null | NOW | - |
| PreviousSponsorshipId | null | OldId | null | - |
| UpdatedDate | null | null | null | NOW |

#### 2. AdminAuditLogs (INSERT)

**Logged Data:**

```typescript
interface AuditLog {
  action: "AssignSubscription" | "AssignSubscription_Queued" | "AssignSubscription_ForceActivation";
  adminUserId: number;           // From JWT token
  targetUserId: number;          // From request body
  entityType: "UserSubscription";
  entityId: number;              // New subscription ID
  isOnBehalfOf: false;           // Always false for direct assignment
  ipAddress: string;             // From X-Forwarded-For or RemoteIpAddress
  userAgent: string;             // From User-Agent header
  requestPath: "/api/admin/subscriptions/assign";
  reason: string;                // Generated message
  afterState: object;            // JSON with subscription details
  createdDate: DateTime;         // NOW
}
```

**Example Audit Log (Path 3 - Force Override):**
```json
{
  "action": "AssignSubscription_ForceActivation",
  "adminUserId": 42,
  "targetUserId": 165,
  "entityType": "UserSubscription",
  "entityId": 252,
  "reason": "Force activated XL subscription for 12 months (cancelled subscription 42)",
  "afterState": {
    "newSubscription": {
      "id": 252,
      "subscriptionTierId": 5,
      "startDate": "2025-01-15T10:30:00",
      "endDate": "2026-01-15T10:30:00"
    },
    "cancelledSubscription": {
      "id": 42,
      "endDate": "2025-01-15T10:30:00"
    }
  },
  "ipAddress": "203.0.113.45",
  "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)...",
  "requestPath": "/api/admin/subscriptions/assign",
  "isOnBehalfOf": false,
  "createdDate": "2025-01-15T10:30:00"
}
```

---

## Frontend Integration

### TypeScript Client

```typescript
// types.ts
export interface AssignSubscriptionRequest {
  userId: number;
  subscriptionTierId: number;
  durationMonths: number;
  isSponsoredSubscription: boolean;
  sponsorId?: number | null;
  notes?: string | null;
  forceActivation?: boolean;
}

export interface SubscriptionResponse {
  success: boolean;
  message: string;
}

export enum SubscriptionTier {
  Trial = 1,
  Small = 2,
  Medium = 3,
  Large = 4,
  ExtraLarge = 5
}

// api-client.ts
export class AdminSubscriptionService {
  private baseUrl: string;
  private token: string;

  constructor(baseUrl: string, token: string) {
    this.baseUrl = baseUrl;
    this.token = token;
  }

  async assignSubscription(
    request: AssignSubscriptionRequest
  ): Promise<SubscriptionResponse> {
    const response = await fetch(`${this.baseUrl}/api/admin/subscriptions/assign`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'Content-Type': 'application/json',
        'x-dev-arch-version': '1.0'
      },
      body: JSON.stringify(request)
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Assignment failed');
    }

    return await response.json();
  }

  // Helper: Check if user has active sponsorship
  async checkActiveSponsorship(userId: number): Promise<boolean> {
    const response = await fetch(
      `${this.baseUrl}/api/admin/subscriptions?userId=${userId}&isActive=true&isSponsoredSubscription=true`,
      {
        headers: {
          'Authorization': `Bearer ${this.token}`,
          'x-dev-arch-version': '1.0'
        }
      }
    );

    const data = await response.json();
    return data.data && data.data.length > 0;
  }
}
```

### React Component Example

```typescript
import React, { useState } from 'react';
import { AdminSubscriptionService, SubscriptionTier } from './api-client';

interface AssignFormProps {
  adminToken: string;
  apiBaseUrl: string;
}

export const AssignSubscriptionForm: React.FC<AssignFormProps> = ({
  adminToken,
  apiBaseUrl
}) => {
  const [userId, setUserId] = useState<number>(0);
  const [tierId, setTierId] = useState<SubscriptionTier>(SubscriptionTier.Medium);
  const [durationMonths, setDurationMonths] = useState<number>(12);
  const [sponsorId, setSponsorId] = useState<number>(0);
  const [notes, setNotes] = useState<string>('');
  const [forceActivation, setForceActivation] = useState<boolean>(false);
  const [loading, setLoading] = useState<boolean>(false);
  const [result, setResult] = useState<string | null>(null);

  const service = new AdminSubscriptionService(apiBaseUrl, adminToken);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setResult(null);

    try {
      // Check for active sponsorship
      const hasActive = await service.checkActiveSponsorship(userId);

      if (hasActive && !forceActivation) {
        const confirmQueue = window.confirm(
          'User has an active sponsorship. New subscription will be queued. Continue?'
        );
        if (!confirmQueue) {
          setLoading(false);
          return;
        }
      }

      if (hasActive && forceActivation) {
        const confirmForce = window.confirm(
          '⚠️ WARNING: This will cancel the user\'s current active sponsorship immediately. Are you sure?'
        );
        if (!confirmForce) {
          setLoading(false);
          return;
        }
      }

      const response = await service.assignSubscription({
        userId,
        subscriptionTierId: tierId,
        durationMonths,
        isSponsoredSubscription: true,
        sponsorId,
        notes: notes || null,
        forceActivation
      });

      setResult(response.message);
    } catch (error: any) {
      setResult(`Error: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <h2>Assign Subscription</h2>

      <div>
        <label>User ID:</label>
        <input
          type="number"
          value={userId}
          onChange={(e) => setUserId(Number(e.target.value))}
          required
        />
      </div>

      <div>
        <label>Tier:</label>
        <select
          value={tierId}
          onChange={(e) => setTierId(Number(e.target.value) as SubscriptionTier)}
        >
          <option value={SubscriptionTier.Small}>Small (S)</option>
          <option value={SubscriptionTier.Medium}>Medium (M)</option>
          <option value={SubscriptionTier.Large}>Large (L)</option>
          <option value={SubscriptionTier.ExtraLarge}>Extra Large (XL)</option>
        </select>
      </div>

      <div>
        <label>Duration (months):</label>
        <input
          type="number"
          value={durationMonths}
          onChange={(e) => setDurationMonths(Number(e.target.value))}
          min={1}
          max={120}
          required
        />
      </div>

      <div>
        <label>Sponsor ID:</label>
        <input
          type="number"
          value={sponsorId}
          onChange={(e) => setSponsorId(Number(e.target.value))}
          required
        />
      </div>

      <div>
        <label>Notes:</label>
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="Optional notes..."
        />
      </div>

      <div>
        <label>
          <input
            type="checkbox"
            checked={forceActivation}
            onChange={(e) => setForceActivation(e.target.checked)}
          />
          Force Activation (Cancel existing sponsorship)
        </label>
      </div>

      <button type="submit" disabled={loading}>
        {loading ? 'Assigning...' : 'Assign Subscription'}
      </button>

      {result && (
        <div style={{
          padding: '10px',
          marginTop: '10px',
          background: result.includes('Error') ? '#ffebee' : '#e8f5e9'
        }}>
          {result}
        </div>
      )}
    </form>
  );
};
```

---

## Testing Guide

### Test Scenario 1: Immediate Activation

**Setup:**
```sql
-- Ensure user 170 has NO active sponsorship
DELETE FROM "UserSubscriptions"
WHERE UserId = 170 AND IsActive = true AND IsSponsoredSubscription = true;
```

**Execute:**
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/admin/subscriptions/assign" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
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

**Expected Response:**
```json
{
  "success": true,
  "message": "Subscription assigned successfully. Valid until 2025-07-15"
}
```

**Verify:**
```sql
SELECT * FROM "UserSubscriptions"
WHERE UserId = 170
ORDER BY CreatedDate DESC
LIMIT 1;

-- Expected:
-- IsActive=true, Status='Active', QueueStatus=1
-- StartDate ~ NOW, EndDate ~ NOW + 6 months
```

---

### Test Scenario 2: Queue Mode

**Setup:**
```sql
-- Give user 165 an active L tier subscription
INSERT INTO "UserSubscriptions" (
  UserId, SubscriptionTierId, StartDate, EndDate,
  IsActive, Status, IsSponsoredSubscription, QueueStatus,
  CreatedDate
) VALUES (
  165, 4, NOW(), NOW() + INTERVAL '6 months',
  true, 'Active', true, 1,
  NOW()
);
```

**Execute:**
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/admin/subscriptions/assign" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
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

**Expected Response:**
```json
{
  "success": true,
  "message": "Subscription queued successfully. Will activate automatically on 2025-07-15 when current sponsorship expires."
}
```

**Verify:**
```sql
-- Active subscription (unchanged)
SELECT * FROM "UserSubscriptions"
WHERE UserId = 165 AND IsActive = true;
-- Expected: Status='Active', QueueStatus=1

-- Queued subscription
SELECT * FROM "UserSubscriptions"
WHERE UserId = 165 AND QueueStatus = 0;
-- Expected: Status='Pending', QueueStatus=0, IsActive=false
-- PreviousSponsorshipId should point to active subscription ID
```

---

### Test Scenario 3: Force Override

**Setup:** Same as Test Scenario 2 (user has active subscription)

**Execute:**
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/admin/subscriptions/assign" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "userId": 165,
    "subscriptionTierId": 5,
    "durationMonths": 12,
    "isSponsoredSubscription": true,
    "sponsorId": 159,
    "forceActivation": true,
    "notes": "Emergency upgrade"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Previous sponsorship cancelled. New XL subscription activated. Valid until 2026-01-15"
}
```

**Verify:**
```sql
-- Old subscription (should be cancelled)
SELECT * FROM "UserSubscriptions"
WHERE UserId = 165 AND SubscriptionTierId = 4;
-- Expected: Status='Cancelled', QueueStatus=3, IsActive=false
-- EndDate should be ~ NOW (terminated immediately)

-- New subscription (should be active)
SELECT * FROM "UserSubscriptions"
WHERE UserId = 165 AND SubscriptionTierId = 5 AND IsActive = true;
-- Expected: Status='Active', QueueStatus=1, IsActive=true
-- StartDate ~ NOW, EndDate ~ NOW + 12 months

-- Audit log check
SELECT * FROM "AdminAuditLogs"
WHERE Action = 'AssignSubscription_ForceActivation'
ORDER BY CreatedDate DESC
LIMIT 1;
-- Should exist with correct admin and target user IDs
```

---

### Test Scenario 4: Error - Invalid Tier

**Execute:**
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/admin/subscriptions/assign" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "userId": 165,
    "subscriptionTierId": 99,
    "durationMonths": 12,
    "isSponsoredSubscription": true,
    "sponsorId": 159
  }'
```

**Expected Response:**
```json
{
  "success": false,
  "message": "Subscription tier not found"
}
```

**HTTP Status:** `400 Bad Request`

---

## Troubleshooting

### Issue 1: Queue Not Working

**Symptom:** Subscription activates immediately despite active sponsorship

**Possible Causes:**
1. `isSponsoredSubscription = false` → Queue control only applies to sponsored subscriptions
2. Existing subscription has `Status != "Active"` or `QueueStatus != Active`
3. Existing subscription `EndDate` is in the past

**Debug Query:**
```sql
-- Check for active sponsorships
SELECT * FROM "UserSubscriptions"
WHERE UserId = 165
  AND IsSponsoredSubscription = true
  AND IsActive = true
  AND Status = 'Active'
  AND QueueStatus = 1
  AND EndDate > NOW();
```

**Solution:** Ensure existing subscription meets ALL queue detection criteria

---

### Issue 2: Force Override Not Cancelling

**Symptom:** Old subscription still active after force override

**Possible Causes:**
1. Database transaction failed
2. SaveChangesAsync() not awaited properly

**Debug Query:**
```sql
-- Check transaction consistency
SELECT Id, UserId, Status, QueueStatus, IsActive, EndDate
FROM "UserSubscriptions"
WHERE UserId = 165
ORDER BY CreatedDate DESC;
```

**Solution:** Check AdminAuditLogs for `AssignSubscription_ForceActivation` entry. If missing, operation failed.

---

### Issue 3: DateTime MinValue Issues

**Symptom:** Queued subscriptions have dates in year 0001

**Expected Behavior:** This is CORRECT for queued subscriptions. `StartDate` and `EndDate` are set to `DateTime.MinValue` as placeholders.

**Auto-Activation:** When previous subscription expires, system sets real dates:
- `StartDate = NOW`
- `EndDate = NOW + durationMonths`

**Verification:** Check `QueueStatus = 0 (Pending)` and `ActivatedDate = NULL`

---

### Issue 4: Audit Logs Missing

**Symptom:** No audit log entry after successful assignment

**Possible Causes:**
1. AdminAuditService dependency not registered
2. Audit logging failed silently (error suppressed)

**Debug:**
```sql
SELECT * FROM "AdminAuditLogs"
WHERE TargetUserId = 165
ORDER BY CreatedDate DESC
LIMIT 5;
```

**Check Logs:**
```bash
# Search application logs for audit errors
grep "AdminAuditService" /var/log/ziraai/app.log
```

---

## Key Differences: Admin Assign vs Code Redeem

| Aspect | Code Redeem (User) | Admin Assign (Before) | Admin Assign (Now) |
|--------|-------------------|----------------------|-------------------|
| **Queue Control** | ✅ Yes (automatic) | ❌ No | ✅ Yes (default) |
| **Force Override** | ❌ Not available | N/A | ✅ Yes (optional) |
| **Active Check** | ✅ Prevents conflict | ❌ Could create conflict | ✅ Prevents conflict |
| **Default Behavior** | Queue if conflict | Immediate activation | Queue if conflict |
| **Admin Power** | Limited | Full (risky) | Controlled override |
| **Audit Trail** | Standard | Standard | Enhanced (3 types) |

---

## Security & Compliance

### Authorization
- **Endpoint Protection:** `[Authorize(Roles = "Admin")]`
- **Operation Security:** `[SecuredOperation(Priority = 1)]`
- **Claims Required:** Admin role in JWT token

### Audit Trail
- **Action Types:**
  - `AssignSubscription` - Immediate activation
  - `AssignSubscription_Queued` - Queue mode
  - `AssignSubscription_ForceActivation` - Force override
- **Logged Data:**
  - Admin user ID, IP address, user agent
  - Target user ID
  - Subscription details (tier, duration, notes)
  - Before/after states for force override
  - Timestamp with millisecond precision

### Data Integrity
- **Queue System:** Prevents multiple active sponsorships
- **Foreign Keys:** `PreviousSponsorshipId` maintains queue dependencies
- **Transaction Safety:** All database operations in single transaction
- **Cancellation Preservation:** Cancelled subscriptions retained for audit

### Best Practices

1. **Default to Queue Mode:**
   - Use `forceActivation = false` (default) unless emergency
   - Let subscriptions activate automatically when previous expires

2. **Force Override Guidelines:**
   - Require explicit confirmation in UI
   - Document reason in `notes` field
   - Reserve for genuine emergencies or special cases

3. **Notes Field Usage:**
   - Always provide context: campaign, reason, ticket reference
   - Examples: "2025 Q1 Campaign", "Support ticket #12345", "Emergency upgrade"

4. **Monitoring:**
   - Review `AssignSubscription_ForceActivation` audit logs regularly
   - Alert on excessive force override usage
   - Track queue → activation success rate

---

## Related Documentation

- [Admin Assign Queue Control](./ADMIN_ASSIGN_QUEUE_CONTROL.md) - Feature overview
- [Admin Subscriptions API Filtering](./ADMIN_SUBSCRIPTIONS_API_FILTERING.md) - Query filtering guide
- [Sponsorship Queue System Design](../SPONSORSHIP_QUEUE_SYSTEM_DESIGN.md) - Queue architecture

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-01-15 | Initial release with queue control and force override |

---

## Support

**Backend Team:** backend@ziraai.com
**API Documentation:** https://api.ziraai.com/swagger
**Feature Branch:** `feature/admin-assign-queue-control`
**Status:** ✅ Production Ready

---

**Generated:** 2025-01-15
**Last Verified:** 2025-01-15
**Code Version:** Commit 7e32be1
