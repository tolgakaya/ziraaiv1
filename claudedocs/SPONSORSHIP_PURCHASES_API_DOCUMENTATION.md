# Sponsorship Purchases API - Complete Documentation

## Overview

This document provides comprehensive documentation for the Sponsorship Purchases endpoint, including correct status values, parameter usage, and common pitfalls.

**Base URL:** `https://ziraai-api-sit.up.railway.app` (Staging)
**Controller:** `AdminSponsorshipController`
**Authorization:** Required - JWT Bearer token with Administrator role

---

## Table of Contents

1. [Get All Purchases Endpoint](#get-all-purchases-endpoint)
2. [Status Fields Explained](#status-fields-explained)
3. [Common Mistakes](#common-mistakes)
4. [Purchase Lifecycle](#purchase-lifecycle)
5. [Query Examples](#query-examples)
6. [Related Endpoints](#related-endpoints)

---

## Get All Purchases Endpoint

### Endpoint

```http
GET /api/admin/sponsorship/purchases
```

### Purpose

Retrieve sponsorship purchases with optional filtering by sponsor, status, and payment status.

**Use Cases:**
- Monitor sponsor purchases and payments
- Track pending payments requiring approval
- Audit completed transactions
- Identify cancelled or refunded purchases

### Query Parameters

| Parameter | Type | Required | Default | Valid Values | Description |
|-----------|------|----------|---------|--------------|-------------|
| page | integer | No | 1 | > 0 | Page number for pagination |
| pageSize | integer | No | 50 | 1-100 | Number of items per page |
| status | string | No | null | `Active`, `Completed`, `Cancelled` | Filter by purchase status |
| paymentStatus | string | No | null | `Pending`, `Completed`, `Failed`, `Refunded` | Filter by payment status |
| sponsorId | integer | No | null | Valid user ID | Filter by sponsor user ID |

### Request Headers

```http
Authorization: Bearer {your-jwt-token}
Content-Type: application/json
```

### Example Request

```http
GET /api/admin/sponsorship/purchases?sponsorId=159&paymentStatus=Completed&page=1&pageSize=50
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Response Structure

```json
{
  "data": [
    {
      "id": 1,
      "sponsorId": 159,
      "subscriptionTierId": 3,
      "quantity": 100,
      "unitPrice": 50.00,
      "totalAmount": 5000.00,
      "currency": "TRY",
      "purchaseDate": "2025-11-01T10:30:00",
      "paymentMethod": "BankTransfer",
      "paymentReference": "TRX-2025-001234",
      "paymentStatus": "Completed",
      "paymentCompletedDate": "2025-11-01T14:45:00",
      "invoiceNumber": "INV-2025-001234",
      "invoiceAddress": "Ankara Caddesi No:123, İzmir",
      "taxNumber": "1234567890",
      "companyName": "Agro Sponsor A.Ş.",
      "codesGenerated": 100,
      "codesUsed": 45,
      "codePrefix": "AGRO",
      "validityDays": 365,
      "status": "Active",
      "notes": "İlk toplu alım",
      "purchaseReason": "Bayi ağı genişletme",
      "createdDate": "2025-11-01T10:30:00",
      "updatedDate": "2025-11-01T14:45:00",
      "approvedByUserId": 1,
      "approvalDate": "2025-11-01T14:00:00",
      "sponsor": {
        "userId": 159,
        "fullName": "Agro Sponsor A.Ş.",
        "email": "info@agrosponsor.com",
        "mobilePhones": "+905551234567"
      },
      "subscriptionTier": {
        "id": 3,
        "tierName": "L",
        "displayName": "Large - Büyük İşletme",
        "requestLimit": 200,
        "messagingEnabled": true,
        "profileVisibility": "Full"
      },
      "approvedByUser": {
        "userId": 1,
        "fullName": "System Administrator",
        "email": "admin@ziraai.com"
      }
    }
  ],
  "success": true,
  "message": "Purchases retrieved successfully"
}
```

### Response Fields

#### Purchase Information
| Field | Type | Description |
|-------|------|-------------|
| id | integer | Unique purchase ID |
| sponsorId | integer | Sponsor company user ID |
| subscriptionTierId | integer | Which tier was purchased (S=1, M=2, L=3, XL=4) |
| quantity | integer | Number of subscriptions purchased |
| unitPrice | decimal | Price per subscription |
| totalAmount | decimal | Total purchase amount |
| currency | string | Currency code (TRY, USD, EUR) |

#### Payment Information
| Field | Type | Description |
|-------|------|-------------|
| purchaseDate | datetime | When purchase was created |
| paymentMethod | string | Payment method: `CreditCard`, `BankTransfer`, `Invoice` |
| paymentReference | string | Transaction ID or invoice number |
| **paymentStatus** | **string** | **`Pending`, `Completed`, `Failed`, `Refunded`** |
| paymentCompletedDate | datetime | When payment was completed (null if pending) |

#### Invoice Information
| Field | Type | Description |
|-------|------|-------------|
| invoiceNumber | string | Invoice number |
| invoiceAddress | string | Billing address |
| taxNumber | string | Tax ID number |
| companyName | string | Company name for invoice |

#### Code Generation
| Field | Type | Description |
|-------|------|-------------|
| codesGenerated | integer | Number of codes actually generated |
| codesUsed | integer | Number of codes redeemed by farmers |
| codePrefix | string | Custom prefix for codes (e.g., "AGRO", "FARM") |
| validityDays | integer | How many days codes are valid |

#### Status and Audit
| Field | Type | Description |
|-------|------|-------------|
| **status** | **string** | **`Active`, `Completed`, `Cancelled`** |
| notes | string | Internal notes |
| purchaseReason | string | Why this purchase was made |
| createdDate | datetime | When purchase was created |
| updatedDate | datetime | Last update timestamp |
| approvedByUserId | integer | Admin who approved the purchase |
| approvalDate | datetime | When purchase was approved |

#### Navigation Properties
| Field | Type | Description |
|-------|------|-------------|
| sponsor | object | Sponsor user details |
| subscriptionTier | object | Tier details (name, limits, features) |
| approvedByUser | object | Admin who approved |

---

## Status Fields Explained

### ⚠️ IMPORTANT: Two Different Status Fields

The `SponsorshipPurchase` entity has **TWO separate status fields** with different meanings:

### 1. `status` - Purchase Status (Overall Purchase State)

**Purpose:** Tracks the overall state of the purchase and code usage lifecycle.

**Location:** `Entities/Concrete/SponsorshipPurchase.cs:43`

```csharp
public string Status { get; set; } // Active, Completed, Cancelled
```

#### Valid Values

| Value | Meaning | When Used | Example Scenario |
|-------|---------|-----------|------------------|
| **Active** | Purchase is active, codes are being used | After payment completed and codes generated | Sponsor purchased 100 codes, 45 used, 55 remaining |
| **Completed** | All codes have been used or expired | When all codes are redeemed or validity expired | All 100 codes have been used by farmers |
| **Cancelled** | Purchase was cancelled | Admin cancelled purchase, refund issued | Sponsor requested cancellation before code generation |

#### Business Rules

- **New Purchase:** Starts as `Active` after approval
- **Active → Completed:** Automatically when `codesUsed == codesGenerated` OR validity period expired
- **Active → Cancelled:** Manually by admin via cancel/refund endpoint
- **Cannot reactivate** a `Completed` or `Cancelled` purchase

---

### 2. `paymentStatus` - Payment Status

**Purpose:** Tracks the payment processing state.

**Location:** `Entities/Concrete/SponsorshipPurchase.cs:24`

```csharp
public string PaymentStatus { get; set; } // Pending, Completed, Failed, Refunded
```

#### Valid Values

| Value | Meaning | When Used | Example Scenario |
|-------|---------|-----------|------------------|
| **Pending** | Payment not yet completed | Awaiting bank transfer confirmation, credit card processing | Purchase created, waiting for payment proof |
| **Completed** | Payment successfully received | Payment verified and confirmed | Bank transfer confirmed by admin |
| **Failed** | Payment attempt failed | Credit card declined, insufficient funds | Credit card payment failed |
| **Refunded** | Payment was refunded | Admin issued refund after cancellation | Sponsor requested and received refund |

#### Business Rules

- **New Purchase:** Usually starts as `Pending` (unless auto-approved)
- **Pending → Completed:** Admin approves via `/purchases/{id}/approve` endpoint
- **Completed → Refunded:** Admin refunds via `/purchases/{id}/refund` endpoint
- **Pending → Failed:** Payment gateway returns failure
- **Cannot change** `Completed` back to `Pending`

---

## Common Mistakes

### ❌ MISTAKE #1: Using "Approved" as a Status

**WRONG:**
```
GET /api/admin/sponsorship/purchases?status=Approved
```

**WHY IT'S WRONG:**
- There is **NO** "Approved" status in the system
- The word "approve" only appears in the **endpoint** `/purchases/{id}/approve`
- Approval changes `paymentStatus` from `Pending` to `Completed`

**CORRECT:**
```
GET /api/admin/sponsorship/purchases?paymentStatus=Completed
```

---

### ❌ MISTAKE #2: Confusing Status and PaymentStatus

**WRONG:**
```
GET /api/admin/sponsorship/purchases?status=Pending
```

**WHY IT'S WRONG:**
- `status` field does NOT have a "Pending" value
- "Pending" belongs to `paymentStatus`
- Valid `status` values: `Active`, `Completed`, `Cancelled`

**CORRECT:**
```
GET /api/admin/sponsorship/purchases?paymentStatus=Pending
```

---

### ❌ MISTAKE #3: Using Both Filters Incorrectly

**WRONG:**
```
GET /api/admin/sponsorship/purchases?status=Completed&paymentStatus=Active
```

**WHY IT'S WRONG:**
- "Active" is a `status` value, not a `paymentStatus` value
- Mixing up the two fields' valid values

**CORRECT:**
```
GET /api/admin/sponsorship/purchases?status=Active&paymentStatus=Completed
```
*Meaning: Active purchases with completed payments*

---

## Purchase Lifecycle

### Typical Purchase Flow

```
1. CREATE PURCHASE
   ↓
   status: "Active"
   paymentStatus: "Pending"

2. ADMIN APPROVES PAYMENT (/purchases/{id}/approve)
   ↓
   status: "Active"
   paymentStatus: "Completed"
   approvedByUserId: {adminId}
   approvalDate: {now}

3. CODES GENERATED
   ↓
   status: "Active"
   paymentStatus: "Completed"
   codesGenerated: 100
   codesUsed: 0

4. FARMERS USE CODES (ongoing)
   ↓
   status: "Active"
   paymentStatus: "Completed"
   codesUsed: 0 → 45 → 78 → 100

5. ALL CODES USED
   ↓
   status: "Completed"  ← Changed automatically
   paymentStatus: "Completed"
   codesUsed: 100
```

### Cancellation Flow

```
1. PURCHASE CREATED
   ↓
   status: "Active"
   paymentStatus: "Pending"

2. SPONSOR REQUESTS CANCELLATION
   ↓
   Admin calls /purchases/{id}/refund

3. PURCHASE CANCELLED
   ↓
   status: "Cancelled"
   paymentStatus: "Refunded" (if payment was completed)
                  OR "Failed" (if payment never completed)
```

### Payment Failure Flow

```
1. PURCHASE CREATED
   ↓
   status: "Active"
   paymentStatus: "Pending"

2. CREDIT CARD PAYMENT ATTEMPTED
   ↓
   Payment gateway returns error

3. PAYMENT FAILED
   ↓
   status: "Active" (purchase still exists)
   paymentStatus: "Failed"

4. OPTIONS:
   - Retry payment → back to "Pending"
   - Cancel purchase → status: "Cancelled"
```

---

## Query Examples

### Example 1: Get All Pending Payments

**Scenario:** Admin needs to review and approve pending bank transfers.

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/purchases?paymentStatus=Pending&page=1&pageSize=50" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Returns:** All purchases waiting for payment approval.

---

### Example 2: Get Active Purchases for a Sponsor

**Scenario:** Check how many active purchases a specific sponsor has.

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/purchases?sponsorId=159&status=Active" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Returns:** All active purchases for sponsor ID 159.

---

### Example 3: Get Completed Payments That Are Still Active

**Scenario:** Find purchases with completed payments where codes are still being used.

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/purchases?status=Active&paymentStatus=Completed" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Returns:** Active purchases with verified payments (normal operational state).

---

### Example 4: Get All Refunded Purchases

**Scenario:** Generate refund report for accounting.

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/purchases?paymentStatus=Refunded" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Returns:** All purchases that have been refunded.

---

### Example 5: Get Cancelled Purchases for a Sponsor

**Scenario:** Review cancellation history for a specific sponsor.

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/purchases?sponsorId=159&status=Cancelled" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Returns:** All cancelled purchases for sponsor ID 159.

---

### Example 6: Get Failed Payments

**Scenario:** Identify payment failures requiring follow-up.

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/purchases?paymentStatus=Failed" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Returns:** All purchases with failed payment attempts.

---

### Example 7: Get All Purchases for a Sponsor (No Filter)

**Scenario:** View complete purchase history for a sponsor.

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/purchases?sponsorId=159&page=1&pageSize=100" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Returns:** All purchases for sponsor ID 159, regardless of status.

---

## Related Endpoints

### Approve Purchase

**Purpose:** Approve a pending payment and mark it as completed.

```http
POST /api/admin/sponsorship/purchases/{purchaseId}/approve
```

**Request Body:**
```json
{
  "notes": "Bank transfer confirmed - Reference: TRX-2025-001234"
}
```

**Effect:**
- Changes `paymentStatus` from `Pending` to `Completed`
- Sets `approvedByUserId` to current admin
- Sets `approvalDate` to current timestamp
- Sets `paymentCompletedDate` to current timestamp

---

### Refund Purchase

**Purpose:** Issue a refund and cancel the purchase.

```http
POST /api/admin/sponsorship/purchases/{purchaseId}/refund
```

**Request Body:**
```json
{
  "refundReason": "Sponsor requested cancellation due to budget constraints"
}
```

**Effect:**
- Changes `status` to `Cancelled`
- Changes `paymentStatus` to `Refunded`
- Deactivates any unused codes
- Records refund reason in notes

---

### Get Purchase By ID

**Purpose:** Get detailed information about a specific purchase.

```http
GET /api/admin/sponsorship/purchases/{purchaseId}
```

**Returns:** Full purchase details including related codes, sponsor info, and usage statistics.

---

## Status Transition Rules

### Valid Transitions for `status`

```
Active ──────────────────────────────────────────────────────────────────┐
  │                                                                       │
  │ (all codes used OR validity expired)                                 │
  ↓                                                                       │
Completed                                                                 │
                                                                          │
Active ──────────────────────────────────────────────────────────────────┤
  │                                                                       │
  │ (admin cancels purchase)                                             │
  ↓                                                                       ↓
Cancelled ←───────────────────────────────────────────────────────── Invalid
```

**Invalid Transitions:**
- ❌ `Completed` → `Active`
- ❌ `Cancelled` → `Active`
- ❌ `Cancelled` → `Completed`

---

### Valid Transitions for `paymentStatus`

```
Pending ──────────────────────────────────────────────────────────────────┐
  │                                                                        │
  │ (admin approves)                                                      │
  ↓                                                                        │
Completed ──────────────────────────────────────────────────────────────┐ │
  │                                                                      │ │
  │ (admin issues refund)                                               │ │
  ↓                                                                      │ │
Refunded                                                                 │ │
                                                                         │ │
Pending ─────────────────────────────────────────────────────────────────┤ │
  │                                                                        │
  │ (payment gateway fails)                                               │
  ↓                                                                        ↓
Failed ←────────────────────────────────────────────────────────────── Invalid
```

**Invalid Transitions:**
- ❌ `Completed` → `Pending`
- ❌ `Refunded` → `Completed`
- ❌ `Failed` → `Completed` (must go through `Pending` again)

---

## Quick Reference

### Status Values Summary

| Field | Valid Values | Description |
|-------|-------------|-------------|
| **status** | `Active`, `Completed`, `Cancelled` | Purchase lifecycle state |
| **paymentStatus** | `Pending`, `Completed`, `Failed`, `Refunded` | Payment processing state |

### Common Query Patterns

| Need | Query Parameter |
|------|----------------|
| Pending approvals | `?paymentStatus=Pending` |
| Paid purchases | `?paymentStatus=Completed` |
| Active codes | `?status=Active` |
| Used up purchases | `?status=Completed` |
| Cancelled purchases | `?status=Cancelled` |
| Refunded payments | `?paymentStatus=Refunded` |
| Failed payments | `?paymentStatus=Failed` |
| Sponsor's purchases | `?sponsorId={id}` |
| Active + Paid | `?status=Active&paymentStatus=Completed` |

---

## Error Responses

### 400 Bad Request - Invalid Status Value

```json
{
  "success": false,
  "message": "Invalid status value",
  "errors": {
    "status": ["Status must be one of: Active, Completed, Cancelled"]
  }
}
```

### 400 Bad Request - Invalid Payment Status Value

```json
{
  "success": false,
  "message": "Invalid payment status value",
  "errors": {
    "paymentStatus": ["Payment status must be one of: Pending, Completed, Failed, Refunded"]
  }
}
```

### 404 Not Found

```json
{
  "success": false,
  "message": "Purchase not found"
}
```

---

## Best Practices

### 1. Always Use Correct Field Names

✅ **DO:**
```javascript
// Filtering by payment state
fetch('/api/admin/sponsorship/purchases?paymentStatus=Completed')

// Filtering by purchase state
fetch('/api/admin/sponsorship/purchases?status=Active')
```

❌ **DON'T:**
```javascript
// "Approved" doesn't exist
fetch('/api/admin/sponsorship/purchases?status=Approved')

// Mixing up fields
fetch('/api/admin/sponsorship/purchases?status=Pending')
```

---

### 2. Combine Filters for Specific Queries

✅ **DO:**
```javascript
// Get active purchases with completed payments
fetch('/api/admin/sponsorship/purchases?status=Active&paymentStatus=Completed')

// Get pending payments for specific sponsor
fetch('/api/admin/sponsorship/purchases?sponsorId=159&paymentStatus=Pending')
```

---

### 3. Handle Pagination

✅ **DO:**
```javascript
// Always specify page and pageSize for large datasets
fetch('/api/admin/sponsorship/purchases?page=1&pageSize=50')

// Check totalRecords to know if more pages exist
if (response.totalRecords > pageSize) {
  // Fetch next page
  fetch('/api/admin/sponsorship/purchases?page=2&pageSize=50')
}
```

---

### 4. Check Both Status Fields

✅ **DO:**
```javascript
// Check purchase state
if (purchase.status === 'Active' && purchase.paymentStatus === 'Completed') {
  // Normal operational state - codes can be used
}

// Check payment state
if (purchase.paymentStatus === 'Pending') {
  // Show "Awaiting Payment Approval" badge
}
```

---

## Troubleshooting

### Issue 1: "No results found" when using status=Approved

**Problem:** Query returns empty results.

**Cause:** "Approved" is not a valid status value.

**Solution:** Use `paymentStatus=Completed` instead.

---

### Issue 2: Getting Active purchases when expecting Pending

**Problem:** Query with `status=Pending` returns empty or wrong results.

**Cause:** Confusion between `status` and `paymentStatus` fields.

**Solution:** Use `paymentStatus=Pending` for pending payments.

---

### Issue 3: Cannot find purchases after approval

**Problem:** Approved purchases don't show up in queries.

**Cause:** Filtering by wrong status value after approval.

**Solution:** After approval, use `paymentStatus=Completed`, not `status=Approved`.

---

## Changelog

### Version 1.0.0 - 2025-11-08

**Initial Documentation:**
- Complete status field definitions
- Lifecycle flow diagrams
- Common mistakes and corrections
- Query examples and best practices

**Status:** Production Ready

---

**Document Version:** 1.0.0
**Last Updated:** 2025-11-08
**Author:** ZiraAI Development Team
**Related Documentation:**
- Main Admin API: `ADMIN_SPONSOR_VIEW_API_DOCUMENTATION.md`
- Architecture: `ADMIN_SPONSOR_VIEW_REQUIREMENTS.md`
