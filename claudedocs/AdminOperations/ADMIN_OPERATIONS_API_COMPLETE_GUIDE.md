# Admin Operations API - Complete Guide

**Version:** 1.0
**Last Updated:** 2025-01-23
**Branch:** feature/step-by-step-admin-operations

## Table of Contents

1. [Overview](#overview)
2. [Authentication & Authorization](#authentication--authorization)
3. [API Endpoints](#api-endpoints)
4. [Test Scenarios](#test-scenarios)
5. [Error Handling](#error-handling)
6. [Best Practices](#best-practices)

---

## Overview

Admin Operations API provides comprehensive administrative capabilities for managing users, subscriptions, sponsorships, analytics, audit logs, and plant analyses. All operations include full audit trail with On-Behalf-Of (OBO) support.

### Key Features

✅ **Full Audit Trail** - Every admin operation is logged with AdminOperationLog
✅ **On-Behalf-Of Support** - Admin can perform operations as other users
✅ **Comprehensive Reporting** - Statistics, analytics, and detailed reports
✅ **Bulk Operations** - Efficient batch processing for large datasets
✅ **CSV Export** - Export statistics to CSV format
✅ **Pagination** - All list endpoints support pagination

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Admin Operations API                      │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │     User     │  │ Subscription │  │ Sponsorship  │      │
│  │  Management  │  │  Management  │  │  Management  │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  Analytics   │  │  Audit Logs  │  │    Plant     │      │
│  │  & Reports   │  │   & Tracking │  │   Analysis   │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                               │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
                ┌───────────────────────┐
                │  AdminOperationLog    │
                │  (Full Audit Trail)   │
                └───────────────────────┘
```

---

## Authentication & Authorization

### Authentication

All endpoints require JWT Bearer token authentication:

```http
Authorization: Bearer <your-jwt-token>
```

### Authorization

Admin operations require **Admin role** with appropriate operation claims:

- `admin.users.manage` - User management operations
- `admin.subscriptions.manage` - Subscription management
- `admin.sponsorship.manage` - Sponsorship management
- `admin.analytics.view` - View analytics and reports
- `admin.audit.view` - View audit logs
- `admin.plantanalysis.manage` - Plant analysis management

### Getting Admin Token

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@ziraai.com",
  "password": "your-password"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh-token-here",
    "expiration": "2025-01-23T15:00:00Z"
  }
}
```

---

## API Endpoints

### 1. User Management

#### 1.1 Get All Users

**GET** `/api/admin/users`

Returns paginated list of all users with filtering options.

**Query Parameters:**
- `page` (int, default: 1) - Page number
- `pageSize` (int, default: 50) - Items per page (max: 100)
- `isActive` (bool?, optional) - Filter by active status
- `role` (string, optional) - Filter by role (Farmer, Sponsor, Admin)

**Example Request:**
```http
GET /api/admin/users?page=1&pageSize=20&isActive=true&role=Farmer
Authorization: Bearer <token>
```

**Example Response:**
```json
{
  "success": true,
  "message": "Found 150 users",
  "data": [
    {
      "userId": 123,
      "fullName": "Ahmet Yılmaz",
      "email": "ahmet@example.com",
      "mobilePhones": "+905551234567",
      "isActive": true,
      "createdDate": "2024-12-15T10:30:00",
      "lastLoginDate": "2025-01-20T14:25:00"
    }
  ]
}
```

---

#### 1.2 Get User By ID

**GET** `/api/admin/users/{userId}`

Returns detailed information for a specific user.

**Path Parameters:**
- `userId` (int, required) - User ID

**Example Request:**
```http
GET /api/admin/users/123
Authorization: Bearer <token>
```

**Example Response:**
```json
{
  "success": true,
  "data": {
    "userId": 123,
    "fullName": "Ahmet Yılmaz",
    "email": "ahmet@example.com",
    "mobilePhones": "+905551234567",
    "isActive": true,
    "createdDate": "2024-12-15T10:30:00",
    "lastLoginDate": "2025-01-20T14:25:00",
    "deactivatedDate": null,
    "deactivatedByAdminId": null
  }
}
```

---

#### 1.3 Search Users

**GET** `/api/admin/users/search`

Search users by name, email, or phone.

**Query Parameters:**
- `searchTerm` (string, required) - Search term
- `page` (int, default: 1) - Page number
- `pageSize` (int, default: 50) - Items per page

**Example Request:**
```http
GET /api/admin/users/search?searchTerm=ahmet&page=1&pageSize=20
Authorization: Bearer <token>
```

---

#### 1.4 Deactivate User

**POST** `/api/admin/users/{userId}/deactivate`

Deactivate a user account.

**Path Parameters:**
- `userId` (int, required) - User ID to deactivate

**Request Body:**
```json
{
  "reason": "Account violation - spam reports"
}
```

**Example Request:**
```http
POST /api/admin/users/123/deactivate
Authorization: Bearer <token>
Content-Type: application/json

{
  "reason": "Multiple spam reports from other users"
}
```

**Example Response:**
```json
{
  "success": true,
  "message": "User Ahmet Yılmaz deactivated successfully"
}
```

**Audit Log Entry:**
- Action: `DeactivateUser`
- TargetUserId: 123
- IsOnBehalfOf: false
- Reason: "Multiple spam reports from other users"

---

#### 1.5 Reactivate User

**POST** `/api/admin/users/{userId}/reactivate`

Reactivate a previously deactivated user.

**Path Parameters:**
- `userId` (int, required) - User ID to reactivate

**Request Body:**
```json
{
  "reason": "Issue resolved - user verified"
}
```

**Example Response:**
```json
{
  "success": true,
  "message": "User Ahmet Yılmaz reactivated successfully"
}
```

---

#### 1.6 Bulk Deactivate Users

**POST** `/api/admin/users/bulk/deactivate`

Deactivate multiple users at once.

**Request Body:**
```json
{
  "userIds": [123, 456, 789],
  "reason": "Bulk cleanup - inactive accounts over 1 year"
}
```

**Example Response:**
```json
{
  "success": true,
  "message": "Successfully deactivated 3 users",
  "data": {
    "totalRequested": 3,
    "successCount": 3,
    "failedCount": 0,
    "results": [
      {
        "userId": 123,
        "success": true,
        "message": "Deactivated successfully"
      },
      {
        "userId": 456,
        "success": true,
        "message": "Deactivated successfully"
      },
      {
        "userId": 789,
        "success": true,
        "message": "Deactivated successfully"
      }
    ]
  }
}
```

---

### 2. Subscription Management

#### 2.1 Get All Subscriptions

**GET** `/api/admin/subscriptions`

Returns paginated list of all subscriptions.

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 50)
- `status` (string, optional) - Active, Expired, Cancelled
- `tierName` (string, optional) - Trial, S, M, L, XL
- `userId` (int?, optional) - Filter by user ID

**Example Request:**
```http
GET /api/admin/subscriptions?page=1&pageSize=20&status=Active&tierName=M
Authorization: Bearer <token>
```

**Example Response:**
```json
{
  "success": true,
  "message": "Found 85 subscriptions",
  "data": [
    {
      "id": 456,
      "userId": 123,
      "userName": "Ahmet Yılmaz",
      "tierName": "M",
      "status": "Active",
      "startDate": "2025-01-01T00:00:00",
      "endDate": "2025-02-01T00:00:00",
      "dailyLimit": 10,
      "monthlyLimit": 300,
      "dailyUsage": 5,
      "monthlyUsage": 145,
      "autoRenew": true
    }
  ]
}
```

---

#### 2.2 Assign Subscription

**POST** `/api/admin/subscriptions/assign`

Assign a new subscription to a user.

**Request Body:**
```json
{
  "userId": 123,
  "tierName": "M",
  "durationDays": 30,
  "reason": "Premium trial for beta tester"
}
```

**Example Response:**
```json
{
  "success": true,
  "message": "Subscription assigned successfully to Ahmet Yılmaz",
  "data": {
    "id": 789,
    "userId": 123,
    "tierName": "M",
    "startDate": "2025-01-23T00:00:00",
    "endDate": "2025-02-22T00:00:00",
    "status": "Active"
  }
}
```

---

#### 2.3 Extend Subscription

**POST** `/api/admin/subscriptions/{subscriptionId}/extend`

Extend an existing subscription.

**Request Body:**
```json
{
  "additionalDays": 15,
  "reason": "Compensation for service downtime"
}
```

**Example Response:**
```json
{
  "success": true,
  "message": "Subscription extended by 15 days",
  "data": {
    "id": 456,
    "oldEndDate": "2025-02-01T00:00:00",
    "newEndDate": "2025-02-16T00:00:00"
  }
}
```

---

#### 2.4 Cancel Subscription

**POST** `/api/admin/subscriptions/{subscriptionId}/cancel`

Cancel an active subscription.

**Request Body:**
```json
{
  "reason": "User requested cancellation",
  "refundAmount": 50.00
}
```

**Example Response:**
```json
{
  "success": true,
  "message": "Subscription cancelled successfully"
}
```

---

#### 2.5 Bulk Cancel Subscriptions

**POST** `/api/admin/subscriptions/bulk/cancel`

Cancel multiple subscriptions at once.

**Request Body:**
```json
{
  "subscriptionIds": [456, 457, 458],
  "reason": "Service discontinuation",
  "refundAmount": 0
}
```

---

### 3. Sponsorship Management

#### 3.1 Get All Purchases

**GET** `/api/admin/sponsorship/purchases`

Returns paginated list of sponsorship purchases.

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 50)
- `status` (string, optional) - Active, Pending, Cancelled
- `paymentStatus` (string, optional) - Completed, Pending, Refunded
- `sponsorId` (int?, optional) - Filter by sponsor

**Example Request:**
```http
GET /api/admin/sponsorship/purchases?page=1&paymentStatus=Completed
Authorization: Bearer <token>
```

---

#### 3.2 Approve Purchase

**POST** `/api/admin/sponsorship/purchases/{purchaseId}/approve`

Approve a pending sponsorship purchase.

**Request Body:**
```json
{
  "notes": "Payment verified via bank transfer"
}
```

**Example Response:**
```json
{
  "success": true,
  "message": "Purchase approved and codes generated successfully"
}
```

---

#### 3.3 Refund Purchase

**POST** `/api/admin/sponsorship/purchases/{purchaseId}/refund`

Process a refund for a purchase.

**Request Body:**
```json
{
  "refundReason": "Duplicate payment - customer error"
}
```

**Example Response:**
```json
{
  "success": true,
  "message": "Purchase refunded successfully. 10 unused codes deactivated."
}
```

---

#### 3.4 Create Purchase On Behalf Of Sponsor

**POST** `/api/admin/sponsorship/purchases/create-on-behalf-of`

Admin creates a sponsorship purchase on behalf of a sponsor.

**Request Body:**
```json
{
  "sponsorId": 234,
  "subscriptionTierId": 3,
  "quantity": 50,
  "unitPrice": 99.99,
  "autoApprove": true,
  "paymentMethod": "BankTransfer",
  "paymentReference": "TRX-2025-0123-001",
  "companyName": "Tarım Teknolojileri A.Ş.",
  "taxNumber": "1234567890",
  "invoiceAddress": "İstanbul, Turkey",
  "codePrefix": "TARIM",
  "validityDays": 365,
  "notes": "Corporate partnership - annual package"
}
```

**Field Descriptions:**
- `autoApprove` - If true, purchase is immediately approved without payment verification
- `paymentMethod` - Manual, Offline, BankTransfer, etc.
- `paymentReference` - Transaction/reference ID for tracking
- `codePrefix` - Custom prefix for generated codes (optional)
- `validityDays` - Validity period for codes (default: 365)

**Example Response:**
```json
{
  "success": true,
  "message": "Purchase created and auto-approved for Tarım Teknolojileri A.Ş. Total: 4999.50 TRY",
  "data": {
    "id": 892,
    "sponsorId": 234,
    "quantity": 50,
    "totalAmount": 4999.50,
    "currency": "TRY",
    "paymentStatus": "Completed",
    "status": "Active",
    "purchaseDate": "2025-01-23T10:30:00"
  }
}
```

**Use Cases:**
1. **Manual/Offline Payments**: Set `autoApprove: true` to bypass online payment
2. **Corporate Partnerships**: Create packages without payment process
3. **Test/Demo Accounts**: Generate codes for testing purposes
4. **Payment Issues**: Recreate purchase after payment problems

---

#### 3.5 Bulk Send Codes

**POST** `/api/admin/sponsorship/codes/bulk-send`

Send sponsorship codes to multiple farmers via SMS/WhatsApp/Email.

**Request Body:**
```json
{
  "sponsorId": 234,
  "purchaseId": 892,
  "recipients": [
    {
      "phoneNumber": "+905551234567",
      "name": "Mehmet Demir"
    },
    {
      "phoneNumber": "+905557654321",
      "name": "Ayşe Kaya"
    },
    {
      "phoneNumber": "+905559876543",
      "name": "Ali Öztürk"
    }
  ],
  "sendVia": "SMS"
}
```

**Field Descriptions:**
- `sendVia` - SMS, WhatsApp, or Email (default: SMS)
- `recipients.name` - Optional, for better tracking

**Example Response:**
```json
{
  "success": true,
  "message": "Successfully sent 3 codes to farmers"
}
```

**Process:**
1. Validates sponsor and purchase exist
2. Fetches unused codes from the purchase
3. Assigns each code to a recipient
4. Sends link via selected method (SMS/WhatsApp/Email)
5. Updates code records with recipient info and send date
6. Creates audit log entry

**Use Cases:**
1. **Initial Distribution**: Send codes to farmer list after purchase
2. **Redistribution**: Resend codes that weren't received
3. **Targeted Campaigns**: Send to specific farmer segments

---

#### 3.6 Get Sponsor Detailed Report

**GET** `/api/admin/sponsorship/sponsor/{sponsorId}/report`

Get comprehensive report for a specific sponsor.

**Path Parameters:**
- `sponsorId` (int, required) - Sponsor user ID

**Example Request:**
```http
GET /api/admin/sponsorship/sponsor/234/report
Authorization: Bearer <token>
```

**Example Response:**
```json
{
  "success": true,
  "data": {
    "sponsorId": 234,
    "sponsorName": "Tarım Teknolojileri A.Ş.",
    "sponsorEmail": "info@tarimtek.com",

    "totalPurchases": 5,
    "activePurchases": 3,
    "pendingPurchases": 1,
    "cancelledPurchases": 1,
    "completedPurchases": 4,
    "totalSpent": 24997.50,

    "totalCodesGenerated": 250,
    "totalCodesSent": 180,
    "totalCodesUsed": 145,
    "totalCodesActive": 35,
    "totalCodesExpired": 20,
    "codeRedemptionRate": 58.0,

    "purchases": [
      {
        "id": 892,
        "tierName": "M",
        "quantity": 50,
        "totalAmount": 4999.50,
        "currency": "TRY",
        "status": "Active",
        "paymentStatus": "Completed",
        "purchaseDate": "2025-01-23T10:30:00",
        "codesGenerated": 50,
        "codesUsed": 35,
        "codesSent": 40
      }
    ],

    "codeDistribution": {
      "unused": 35,
      "used": 145,
      "expired": 20,
      "deactivated": 0,
      "sent": 180,
      "notSent": 70
    }
  }
}
```

**Report Sections:**
1. **Sponsor Info**: Basic sponsor details
2. **Purchase Statistics**: Total purchases, status breakdown, total spent
3. **Code Statistics**: Generation, usage, and redemption metrics
4. **Detailed Purchases**: Full list with individual stats
5. **Code Distribution**: Status breakdown

---

#### 3.7 Get All Codes

**GET** `/api/admin/sponsorship/codes`

Returns paginated list of sponsorship codes.

**Query Parameters:**
- `page`, `pageSize` - Pagination
- `isUsed` (bool?) - Filter by usage status
- `isActive` (bool?) - Filter by active status
- `sponsorId` (int?) - Filter by sponsor
- `purchaseId` (int?) - Filter by purchase

---

#### 3.8 Deactivate Code

**POST** `/api/admin/sponsorship/codes/{codeId}/deactivate`

Deactivate a sponsorship code.

**Request Body:**
```json
{
  "reason": "Code reported as spam"
}
```

---

### 4. Analytics & Reporting

#### 4.1 Get User Statistics

**GET** `/api/admin/analytics/users`

Get overall user statistics.

**Query Parameters:**
- `startDate` (DateTime?) - Start date for filtering
- `endDate` (DateTime?) - End date for filtering

**Example Response:**
```json
{
  "success": true,
  "data": {
    "totalUsers": 5420,
    "activeUsers": 4850,
    "inactiveUsers": 570,
    "farmerUsers": 0,
    "sponsorUsers": 0,
    "adminUsers": 0,
    "newUsersThisMonth": 245,
    "newUsersToday": 12,
    "startDate": "2024-12-01T00:00:00",
    "endDate": "2025-01-23T00:00:00",
    "generatedAt": "2025-01-23T11:00:00"
  }
}
```

---

#### 4.2 Get Subscription Statistics

**GET** `/api/admin/analytics/subscriptions`

Get subscription-related statistics.

**Example Response:**
```json
{
  "success": true,
  "data": {
    "totalSubscriptions": 3245,
    "activeSubscriptions": 2890,
    "expiredSubscriptions": 355,
    "trialSubscriptions": 1250,
    "paidSubscriptions": 1995,
    "subscriptionsByTier": {
      "Trial": 1250,
      "S": 450,
      "M": 780,
      "L": 550,
      "XL": 215
    },
    "totalRevenue": 125000.00,
    "averageSubscriptionValue": 38.54,
    "renewalRate": 72.5,
    "generatedAt": "2025-01-23T11:00:00"
  }
}
```

---

#### 4.3 Get Sponsorship Statistics

**GET** `/api/admin/analytics/sponsorship`

Get sponsorship-related statistics.

**Example Response:**
```json
{
  "success": true,
  "data": {
    "totalPurchases": 458,
    "completedPurchases": 412,
    "pendingPurchases": 28,
    "refundedPurchases": 18,
    "totalRevenue": 2850000.00,
    "totalCodesGenerated": 45800,
    "totalCodesUsed": 32145,
    "totalCodesActive": 8920,
    "totalCodesExpired": 4735,
    "codeRedemptionRate": 70.2,
    "averagePurchaseAmount": 6222.71,
    "totalQuantityPurchased": 45800,
    "uniqueSponsorCount": 124,
    "generatedAt": "2025-01-23T11:00:00"
  }
}
```

---

#### 4.4 Export Statistics to CSV

**GET** `/api/admin/analytics/export`

Export analytics data to CSV file.

**Query Parameters:**
- `reportType` (string, required) - Users, Subscriptions, or Sponsorship
- `startDate` (DateTime?) - Optional start date
- `endDate` (DateTime?) - Optional end date

**Example Request:**
```http
GET /api/admin/analytics/export?reportType=Sponsorship&startDate=2024-12-01&endDate=2025-01-23
Authorization: Bearer <token>
```

**Response:**
- Content-Type: `text/csv`
- File download with naming: `{ReportType}_Statistics_{Timestamp}.csv`

**CSV Format (Sponsorship Example):**
```csv
Metric,Value
Total Purchases,458
Completed Purchases,412
Pending Purchases,28
Total Revenue,2850000.00
Total Codes Generated,45800
Code Redemption Rate,70.2%
Generated At,2025-01-23 11:00:00
```

---

### 5. Audit Logs

#### 5.1 Get All Audit Logs

**GET** `/api/admin/audit`

Returns paginated list of audit logs.

**Query Parameters:**
- `page`, `pageSize` - Pagination
- `action` (string?) - Filter by action type
- `entityType` (string?) - Filter by entity type
- `isOnBehalfOf` (bool?) - Filter OBO operations
- `startDate`, `endDate` - Date range filter

**Example Request:**
```http
GET /api/admin/audit?page=1&pageSize=50&isOnBehalfOf=true&action=CreatePurchaseOnBehalfOf
Authorization: Bearer <token>
```

**Example Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1523,
      "action": "CreatePurchaseOnBehalfOf",
      "adminUserId": 1,
      "adminUserName": "System Admin",
      "targetUserId": 234,
      "targetUserName": "Tarım Teknolojileri A.Ş.",
      "entityType": "SponsorshipPurchase",
      "entityId": 892,
      "isOnBehalfOf": true,
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0...",
      "requestPath": "/api/admin/sponsorship/purchases/create-on-behalf-of",
      "reason": "Created purchase for sponsor Tarım Teknolojileri A.Ş.: 50 x M",
      "beforeState": null,
      "afterState": "{\"Id\":892,\"SponsorId\":234,\"Quantity\":50,...}",
      "createdDate": "2025-01-23T10:30:00"
    }
  ]
}
```

---

#### 5.2 Get Audit Logs By Admin

**GET** `/api/admin/audit/admin/{adminUserId}`

Get all operations performed by a specific admin.

---

#### 5.3 Get Audit Logs By Target User

**GET** `/api/admin/audit/target/{targetUserId}`

Get all admin operations that affected a specific user.

---

#### 5.4 Get On-Behalf-Of Logs

**GET** `/api/admin/audit/on-behalf-of`

Get all on-behalf-of operations.

**Example Request:**
```http
GET /api/admin/audit/on-behalf-of?page=1&pageSize=20&startDate=2025-01-01
Authorization: Bearer <token>
```

---

### 6. Plant Analysis Management

#### 6.1 Create Analysis On Behalf Of User

**POST** `/api/admin/plant-analysis/on-behalf-of`

Admin creates a plant analysis on behalf of a farmer.

**Request Body:**
```json
{
  "targetUserId": 123,
  "imageUrl": "https://storage.ziraai.com/images/plant-12345.jpg",
  "analysisResult": "Plant: Tomato\nDisease: Early Blight\nSeverity: Moderate\nRecommendation: Apply fungicide treatment",
  "notes": "Sample analysis for demonstration"
}
```

**Example Response:**
```json
{
  "success": true,
  "message": "Plant analysis created successfully for user Ahmet Yılmaz",
  "data": {
    "id": 9876,
    "userId": 123,
    "imageUrl": "https://storage.ziraai.com/images/plant-12345.jpg",
    "analysisStatus": "completed",
    "isOnBehalfOf": true,
    "createdByAdminId": 1,
    "createdDate": "2025-01-23T11:15:00"
  }
}
```

---

#### 6.2 Get User Analyses

**GET** `/api/admin/plant-analysis/user/{userId}`

Get all analyses for a specific user.

**Query Parameters:**
- `page`, `pageSize` - Pagination
- `status` (string?) - Filter by analysis status
- `isOnBehalfOf` (bool?) - Filter OBO analyses

**Example Request:**
```http
GET /api/admin/plant-analysis/user/123?page=1&pageSize=20&isOnBehalfOf=true
Authorization: Bearer <token>
```

---

## Test Scenarios

### Scenario 1: User Management Workflow

**Objective:** Manage user lifecycle from activation to deactivation

**Steps:**

1. **List all active farmers**
```http
GET /api/admin/users?page=1&pageSize=50&isActive=true&role=Farmer
```

2. **Search for specific user**
```http
GET /api/admin/users/search?searchTerm=ahmet
```

3. **Get user details**
```http
GET /api/admin/users/123
```

4. **Deactivate user for policy violation**
```http
POST /api/admin/users/123/deactivate
{
  "reason": "Spam activity detected"
}
```

5. **Verify audit log entry**
```http
GET /api/admin/audit/target/123
```

6. **Reactivate after verification**
```http
POST /api/admin/users/123/reactivate
{
  "reason": "False positive - user verified"
}
```

**Expected Outcome:**
- ✅ User deactivated and audit logged
- ✅ User can be reactivated
- ✅ All operations tracked in audit log

---

### Scenario 2: Subscription Management

**Objective:** Manage user subscriptions end-to-end

**Steps:**

1. **Assign premium trial**
```http
POST /api/admin/subscriptions/assign
{
  "userId": 123,
  "tierName": "M",
  "durationDays": 30,
  "reason": "Beta tester reward"
}
```

2. **Monitor subscription usage**
```http
GET /api/admin/subscriptions?userId=123&status=Active
```

3. **Extend subscription for service downtime**
```http
POST /api/admin/subscriptions/456/extend
{
  "additionalDays": 7,
  "reason": "Compensation for 2-day outage"
}
```

4. **View subscription statistics**
```http
GET /api/admin/analytics/subscriptions?startDate=2025-01-01
```

**Expected Outcome:**
- ✅ Subscription created with correct tier and duration
- ✅ Extension applied correctly
- ✅ Statistics reflect changes
- ✅ Audit trail complete

---

### Scenario 3: Sponsor On-Behalf-Of Operations

**Objective:** Complete sponsor package creation and code distribution

**Steps:**

1. **Create purchase on behalf of sponsor (manual payment)**
```http
POST /api/admin/sponsorship/purchases/create-on-behalf-of
{
  "sponsorId": 234,
  "subscriptionTierId": 3,
  "quantity": 50,
  "unitPrice": 99.99,
  "autoApprove": true,
  "paymentMethod": "BankTransfer",
  "paymentReference": "IBAN-TR123456",
  "companyName": "Tarım A.Ş.",
  "taxNumber": "1234567890",
  "codePrefix": "TARIM",
  "validityDays": 365
}
```

2. **Verify purchase created**
```http
GET /api/admin/sponsorship/purchases?sponsorId=234&paymentStatus=Completed
```

3. **Send codes to farmers**
```http
POST /api/admin/sponsorship/codes/bulk-send
{
  "sponsorId": 234,
  "purchaseId": 892,
  "recipients": [
    {"phoneNumber": "+905551234567", "name": "Mehmet Demir"},
    {"phoneNumber": "+905557654321", "name": "Ayşe Kaya"}
  ],
  "sendVia": "SMS"
}
```

4. **Get sponsor detailed report**
```http
GET /api/admin/sponsorship/sponsor/234/report
```

5. **Check audit trail**
```http
GET /api/admin/audit/on-behalf-of?entityType=SponsorshipPurchase
```

**Expected Outcome:**
- ✅ Purchase created without online payment
- ✅ Codes generated automatically
- ✅ Codes sent to farmers via SMS
- ✅ Sponsor report shows accurate statistics
- ✅ Full audit trail with isOnBehalfOf=true

---

### Scenario 4: Bulk Operations

**Objective:** Efficiently process multiple items

**Steps:**

1. **Identify inactive users**
```http
GET /api/admin/users?isActive=false&page=1&pageSize=100
```

2. **Bulk deactivate spam accounts**
```http
POST /api/admin/users/bulk/deactivate
{
  "userIds": [101, 102, 103, 104, 105],
  "reason": "Automated spam detection"
}
```

3. **Bulk cancel expired subscriptions**
```http
POST /api/admin/subscriptions/bulk/cancel
{
  "subscriptionIds": [501, 502, 503],
  "reason": "Cleanup expired subscriptions"
}
```

4. **Verify operations in audit log**
```http
GET /api/admin/audit?action=BulkDeactivateUsers
```

**Expected Outcome:**
- ✅ All users deactivated in single operation
- ✅ Individual audit log for each user
- ✅ Rollback capability maintained

---

### Scenario 5: Analytics and Reporting

**Objective:** Generate comprehensive reports

**Steps:**

1. **Get user statistics**
```http
GET /api/admin/analytics/users?startDate=2024-12-01&endDate=2025-01-23
```

2. **Get subscription statistics**
```http
GET /api/admin/analytics/subscriptions?startDate=2025-01-01
```

3. **Get sponsorship statistics**
```http
GET /api/admin/analytics/sponsorship
```

4. **Export sponsorship data to CSV**
```http
GET /api/admin/analytics/export?reportType=Sponsorship&startDate=2024-01-01
```

5. **Download and analyze CSV file**

**Expected Outcome:**
- ✅ All statistics accurate and up-to-date
- ✅ CSV export downloads successfully
- ✅ Data properly formatted for analysis

---

## Error Handling

### Common Error Responses

#### 400 Bad Request
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "UserId is required",
    "TierName must be one of: Trial, S, M, L, XL"
  ]
}
```

#### 401 Unauthorized
```json
{
  "success": false,
  "message": "Authorization token is required"
}
```

#### 403 Forbidden
```json
{
  "success": false,
  "message": "Insufficient permissions. Admin role required."
}
```

#### 404 Not Found
```json
{
  "success": false,
  "message": "User with ID 999 not found"
}
```

#### 409 Conflict
```json
{
  "success": false,
  "message": "User already has an active subscription"
}
```

#### 500 Internal Server Error
```json
{
  "success": false,
  "message": "An error occurred while processing your request",
  "details": "Contact support with timestamp: 2025-01-23T11:30:00Z"
}
```

---

## Best Practices

### 1. Always Provide Reason/Notes
```json
// ❌ BAD
{
  "userId": 123
}

// ✅ GOOD
{
  "userId": 123,
  "reason": "Account verification issue - user unable to upload documents"
}
```

### 2. Use Pagination for Large Datasets
```http
// ❌ BAD
GET /api/admin/users

// ✅ GOOD
GET /api/admin/users?page=1&pageSize=50
```

### 3. Filter by Date Range for Analytics
```http
// ❌ BAD
GET /api/admin/analytics/users

// ✅ GOOD
GET /api/admin/analytics/users?startDate=2025-01-01&endDate=2025-01-23
```

### 4. Check Audit Logs After Critical Operations
```http
// After bulk operation
POST /api/admin/users/bulk/deactivate

// Immediately verify
GET /api/admin/audit?action=BulkDeactivateUsers&startDate=2025-01-23
```

### 5. Use On-Behalf-Of for Sponsor Operations
```json
// When acting as sponsor
{
  "autoApprove": true,  // Skip payment for manual processing
  "paymentReference": "OFFLINE-001",  // Track offline payment
  "notes": "Bank transfer confirmed via phone"
}
```

### 6. Export Data for External Analysis
```http
// Regular reporting
GET /api/admin/analytics/export?reportType=Sponsorship&startDate=2025-01-01
```

### 7. Verify State Before and After Operations
```http
// Before
GET /api/admin/users/123

// Operation
POST /api/admin/users/123/deactivate

// After - verify
GET /api/admin/users/123
GET /api/admin/audit/target/123
```

---

## Database Schema Reference

### AdminOperationLog
```sql
CREATE TABLE AdminOperationLog (
    Id SERIAL PRIMARY KEY,
    Action VARCHAR(100) NOT NULL,
    AdminUserId INT NOT NULL,
    TargetUserId INT,
    EntityType VARCHAR(100),
    EntityId INT,
    IsOnBehalfOf BOOLEAN DEFAULT FALSE,
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    RequestPath VARCHAR(500),
    Reason TEXT,
    BeforeState TEXT,
    AfterState TEXT,
    CreatedDate TIMESTAMP NOT NULL
);
```

### User (Admin Fields)
```sql
ALTER TABLE Users ADD COLUMN IsActive BOOLEAN DEFAULT TRUE;
ALTER TABLE Users ADD COLUMN DeactivatedDate TIMESTAMP;
ALTER TABLE Users ADD COLUMN DeactivatedByAdminId INT;
```

### PlantAnalysis (OBO Fields)
```sql
ALTER TABLE PlantAnalyses ADD COLUMN IsOnBehalfOf BOOLEAN DEFAULT FALSE;
ALTER TABLE PlantAnalyses ADD COLUMN CreatedByAdminId INT;
```

---

## Changelog

### Version 1.0 (2025-01-23)
- ✅ Initial release with 6 controller modules
- ✅ Full CRUD operations for users, subscriptions, sponsorships
- ✅ Analytics and reporting with CSV export
- ✅ Comprehensive audit logging
- ✅ On-behalf-of support for sponsor operations
- ✅ Bulk operations for efficiency
- ✅ Plant analysis admin management

---

## Support

For issues or questions:
- GitHub Issues: https://github.com/tolgakaya/ziraaiv1/issues
- Documentation: See `claudedocs/AdminOperations/`
- API Status: https://ziraai.com/api/health

---

**Generated:** 2025-01-23
**Last Updated:** 2025-01-23
**Version:** 1.0
