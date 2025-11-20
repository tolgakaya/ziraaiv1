# Admin Operations API - Testing Guide (Code-Verified)

**Version:** 2.0
**Last Updated:** 2025-01-23
**Branch:** feature/step-by-step-admin-operations
**Status:** ✅ All endpoints and payloads verified against actual code

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Environment Setup](#environment-setup)
3. [Authentication](#authentication)
4. [Endpoint Reference](#endpoint-reference)
5. [Test Scenarios](#test-scenarios)
6. [Database Verification](#database-verification)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Tools
- ✅ Postman (v10.0+) or similar API client
- ✅ PostgreSQL client (pgAdmin, DBeaver, or psql)
- ✅ Admin account with proper operation claims

### Required Access
- ✅ Admin JWT token with `Admin` and `admin.*` claims
- ✅ Test database access
- ✅ Development/Staging environment

---

## Environment Setup

### 1. Postman Environment Variables

Create a Postman environment with these variables:

```json
{
  "baseUrl": "https://localhost:5001",
  "adminToken": "",
  "testUserId": "123",
  "testSubscriptionId": "456",
  "testSponsorId": "234",
  "testPurchaseId": "892"
}
```

### 2. Get Admin Token

**Endpoint:** `POST {{baseUrl}}/api/auth/login`

**Request:**
```json
{
  "email": "admin@ziraai.com",
  "password": "YourAdminPassword"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "...",
    "expiration": "2025-01-23T15:00:00Z"
  }
}
```

**Postman Test Script** (Auto-save token):
```javascript
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    if (jsonData.data && jsonData.data.accessToken) {
        pm.environment.set('adminToken', jsonData.data.accessToken);
        console.log('✅ Admin token saved');
    }
}
```

### 3. Set Authorization Header

For all admin requests, add:
```
Authorization: Bearer {{adminToken}}
```

---

## Endpoint Reference

### 1. User Management

#### 1.1 Get All Users
**GET** `/api/admin/users`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 50)
- `isActive` (bool?, optional) - Filter by active status
- `status` (string, optional) - Filter by status

**Example:**
```http
GET {{baseUrl}}/api/admin/users?page=1&pageSize=20&isActive=true
Authorization: Bearer {{adminToken}}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "userId": 123,
      "fullName": "Ahmet Yılmaz",
      "email": "ahmet@example.com",
      "mobilePhones": "+905551234567",
      "isActive": true,
      "status": "Active",
      "createdDate": "2025-01-15T10:30:00"
    }
  ],
  "message": "Users retrieved successfully"
}
```

---

#### 1.2 Get User By ID
**GET** `/api/admin/users/{userId}`

**Example:**
```http
GET {{baseUrl}}/api/admin/users/123
Authorization: Bearer {{adminToken}}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "userId": 123,
    "fullName": "Ahmet Yılmaz",
    "email": "ahmet@example.com",
    "mobilePhones": "+905551234567",
    "isActive": true,
    "status": "Active",
    "createdDate": "2025-01-15T10:30:00",
    "deactivatedDate": null,
    "deactivatedByAdminId": null
  },
  "message": "User retrieved successfully"
}
```

---

#### 1.3 Search Users
**GET** `/api/admin/users/search`

**Query Parameters:**
- `searchTerm` (string, required) - Search in email, name, or mobile
- `page` (int, default: 1)
- `pageSize` (int, default: 50)

**Example:**
```http
GET {{baseUrl}}/api/admin/users/search?searchTerm=ahmet&page=1&pageSize=20
Authorization: Bearer {{adminToken}}
```

**Response:** Same as Get All Users

---

#### 1.4 Deactivate User
**POST** `/api/admin/users/{userId}/deactivate`

**Request Body:**
```json
{
  "reason": "User requested account closure"
}
```

**Example:**
```http
POST {{baseUrl}}/api/admin/users/123/deactivate
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "reason": "User requested account closure"
}
```

**Response:**
```json
{
  "success": true,
  "message": "User deactivated successfully"
}
```

---

#### 1.5 Reactivate User
**POST** `/api/admin/users/{userId}/reactivate`

**Request Body:**
```json
{
  "reason": "Issue resolved, reactivating account"
}
```

**Example:**
```http
POST {{baseUrl}}/api/admin/users/123/reactivate
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "reason": "Issue resolved, reactivating account"
}
```

**Response:**
```json
{
  "success": true,
  "message": "User reactivated successfully"
}
```

---

#### 1.6 Bulk Deactivate Users
**POST** `/api/admin/users/bulk/deactivate`

**Request Body:**
```json
{
  "userIds": [101, 102, 103, 104, 105],
  "reason": "Bulk cleanup operation"
}
```

**Example:**
```http
POST {{baseUrl}}/api/admin/users/bulk/deactivate
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "userIds": [101, 102, 103],
  "reason": "Bulk cleanup operation"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Bulk operation completed: 3 succeeded, 0 failed"
}
```

---

### 2. Subscription Management

#### 2.1 Get All Subscriptions
**GET** `/api/admin/subscriptions`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 50)
- `status` (string, optional) - Filter by status
- `isActive` (bool?, optional) - Filter by active status
- `isSponsoredSubscription` (bool?, optional) - Filter by sponsored status

**Example:**
```http
GET {{baseUrl}}/api/admin/subscriptions?page=1&pageSize=20&isActive=true
Authorization: Bearer {{adminToken}}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 456,
      "userId": 123,
      "subscriptionTierId": 3,
      "tierName": "M",
      "startDate": "2025-01-01T00:00:00",
      "endDate": "2025-02-01T00:00:00",
      "isActive": true,
      "status": "Active",
      "isSponsoredSubscription": false,
      "sponsorId": null,
      "dailyUsage": 5,
      "monthlyUsage": 20,
      "dailyLimit": 50,
      "monthlyLimit": 500
    }
  ],
  "message": "Subscriptions retrieved successfully"
}
```

---

#### 2.2 Get Subscription By ID
**GET** `/api/admin/subscriptions/{subscriptionId}`

**Example:**
```http
GET {{baseUrl}}/api/admin/subscriptions/456
Authorization: Bearer {{adminToken}}
```

**Response:** Same structure as Get All Subscriptions (single object)

---

#### 2.3 Assign Subscription
**POST** `/api/admin/subscriptions/assign`

**Request Body:**
```json
{
  "userId": 123,
  "subscriptionTierId": 3,
  "durationMonths": 1,
  "isSponsoredSubscription": false,
  "sponsorId": null,
  "notes": "Admin assigned premium subscription"
}
```

**Field Details:**
- `userId` (int, required) - Target user ID
- `subscriptionTierId` (int, required) - Tier ID (1=Trial, 2=S, 3=M, 4=L, 5=XL)
- `durationMonths` (int, required) - Duration in MONTHS (not days)
- `isSponsoredSubscription` (bool, required) - Whether this is sponsored
- `sponsorId` (int?, optional) - Sponsor user ID if sponsored
- `notes` (string, optional) - Additional notes

**Example:**
```http
POST {{baseUrl}}/api/admin/subscriptions/assign
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "userId": 123,
  "subscriptionTierId": 3,
  "durationMonths": 1,
  "isSponsoredSubscription": false,
  "sponsorId": null,
  "notes": "Admin assigned premium subscription for testing"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Subscription assigned successfully. Valid until 2025-02-23"
}
```

---

#### 2.4 Extend Subscription
**POST** `/api/admin/subscriptions/{subscriptionId}/extend`

**Request Body:**
```json
{
  "extensionMonths": 1,
  "notes": "Extended as compensation for service disruption"
}
```

**Field Details:**
- `extensionMonths` (int, required) - Months to extend (not days)
- `notes` (string, optional) - Extension reason/notes

**Example:**
```http
POST {{baseUrl}}/api/admin/subscriptions/456/extend
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "extensionMonths": 1,
  "notes": "Extended as compensation for service disruption"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Subscription extended successfully"
}
```

---

#### 2.5 Cancel Subscription
**POST** `/api/admin/subscriptions/{subscriptionId}/cancel`

**Request Body:**
```json
{
  "cancellationReason": "User requested cancellation"
}
```

**Example:**
```http
POST {{baseUrl}}/api/admin/subscriptions/456/cancel
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "cancellationReason": "User requested cancellation"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Subscription cancelled successfully"
}
```

---

#### 2.6 Bulk Cancel Subscriptions
**POST** `/api/admin/subscriptions/bulk/cancel`

**Request Body:**
```json
{
  "subscriptionIds": [456, 457, 458],
  "cancellationReason": "Bulk cancellation for inactive users"
}
```

**Example:**
```http
POST {{baseUrl}}/api/admin/subscriptions/bulk/cancel
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "subscriptionIds": [456, 457],
  "cancellationReason": "Bulk cancellation for inactive users"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Bulk operation completed: 2 succeeded, 0 failed"
}
```

---

### 3. Sponsorship Management

#### 3.1 Get All Purchases
**GET** `/api/admin/sponsorship/purchases`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 50)
- `status` (string, optional) - Filter by status
- `paymentStatus` (string, optional) - Filter by payment status
- `sponsorId` (int?, optional) - Filter by sponsor ID

**Example:**
```http
GET {{baseUrl}}/api/admin/sponsorship/purchases?page=1&pageSize=20&status=Active
Authorization: Bearer {{adminToken}}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 892,
      "sponsorId": 234,
      "subscriptionTierId": 3,
      "quantity": 10,
      "unitPrice": 99.99,
      "totalAmount": 999.90,
      "currency": "TRY",
      "status": "Active",
      "paymentStatus": "Completed",
      "paymentMethod": "BankTransfer",
      "paymentReference": "REF-2025-001",
      "companyName": "Test Company Ltd",
      "taxNumber": "1234567890",
      "invoiceAddress": "Istanbul, Turkey",
      "codePrefix": "TEST",
      "validityDays": 365,
      "codesGenerated": 10,
      "codesSent": 3,
      "codesUsed": 1,
      "createdDate": "2025-01-20T10:00:00"
    }
  ],
  "message": "Purchases retrieved successfully"
}
```

---

#### 3.2 Get Purchase By ID
**GET** `/api/admin/sponsorship/purchases/{purchaseId}`

**Example:**
```http
GET {{baseUrl}}/api/admin/sponsorship/purchases/892
Authorization: Bearer {{adminToken}}
```

**Response:** Same structure as Get All Purchases (single object)

---

#### 3.3 Approve Purchase
**POST** `/api/admin/sponsorship/purchases/{purchaseId}/approve`

**Request Body:**
```json
{
  "notes": "Payment verified via bank transfer"
}
```

**Example:**
```http
POST {{baseUrl}}/api/admin/sponsorship/purchases/892/approve
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "notes": "Payment verified via bank transfer"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Purchase approved successfully. 10 codes generated."
}
```

---

#### 3.4 Refund Purchase
**POST** `/api/admin/sponsorship/purchases/{purchaseId}/refund`

**Request Body:**
```json
{
  "refundReason": "Sponsor requested refund due to payment error"
}
```

**Example:**
```http
POST {{baseUrl}}/api/admin/sponsorship/purchases/892/refund
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "refundReason": "Sponsor requested refund due to payment error"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Purchase refunded successfully"
}
```

---

#### 3.5 Create Purchase On Behalf Of Sponsor (OBO)
**POST** `/api/admin/sponsorship/purchases/create-on-behalf-of`

**Request Body:**
```json
{
  "sponsorId": 234,
  "subscriptionTierId": 3,
  "quantity": 10,
  "unitPrice": 99.99,
  "autoApprove": true,
  "paymentMethod": "BankTransfer",
  "paymentReference": "MANUAL-2025-001",
  "companyName": "Test Sponsorship Company",
  "taxNumber": "1234567890",
  "invoiceAddress": "Kadıköy, Istanbul, Turkey",
  "codePrefix": "TEST",
  "validityDays": 365,
  "notes": "Manual purchase created for offline payment"
}
```

**Field Details:**
- `sponsorId` (int, required) - Sponsor user ID
- `subscriptionTierId` (int, required) - Tier ID (1=Trial, 2=S, 3=M, 4=L, 5=XL)
- `quantity` (int, required) - Number of codes to generate
- `unitPrice` (decimal, required) - Price per code
- `autoApprove` (bool, default: false) - Auto-approve and generate codes
- `paymentMethod` (string, required) - Payment method (Manual, BankTransfer, etc.)
- `paymentReference` (string, optional) - Payment reference number
- `companyName` (string, optional) - Invoice company name
- `taxNumber` (string, optional) - Tax/VAT number
- `invoiceAddress` (string, optional) - Invoice address
- `codePrefix` (string, optional) - Prefix for generated codes
- `validityDays` (int, default: 365) - Code validity in days
- `notes` (string, optional) - Additional notes

**Example:**
```http
POST {{baseUrl}}/api/admin/sponsorship/purchases/create-on-behalf-of
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "sponsorId": 234,
  "subscriptionTierId": 3,
  "quantity": 10,
  "unitPrice": 99.99,
  "autoApprove": true,
  "paymentMethod": "BankTransfer",
  "paymentReference": "MANUAL-2025-001",
  "companyName": "Test Company",
  "taxNumber": "1234567890",
  "invoiceAddress": "Istanbul, Turkey",
  "codePrefix": "TEST",
  "validityDays": 365,
  "notes": "Manual purchase for offline payment"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 892,
    "sponsorId": 234,
    "quantity": 10,
    "totalAmount": 999.90,
    "currency": "TRY",
    "status": "Active",
    "paymentStatus": "Completed",
    "codesGenerated": 10
  },
  "message": "Purchase created successfully with 10 codes generated"
}
```

---

#### 3.6 Get All Codes
**GET** `/api/admin/sponsorship/codes`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 50)
- `isUsed` (bool?, optional) - Filter by usage status
- `isActive` (bool?, optional) - Filter by active status
- `sponsorId` (int?, optional) - Filter by sponsor ID
- `purchaseId` (int?, optional) - Filter by purchase ID

**Example:**
```http
GET {{baseUrl}}/api/admin/sponsorship/codes?purchaseId=892&isUsed=false
Authorization: Bearer {{adminToken}}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1001,
      "code": "TEST-ABC123",
      "sponsorshipPurchaseId": 892,
      "sponsorId": 234,
      "subscriptionTierId": 3,
      "isUsed": false,
      "isActive": true,
      "validFrom": "2025-01-20T00:00:00",
      "validUntil": "2026-01-20T00:00:00",
      "linkSentDate": null,
      "recipientPhone": null,
      "recipientName": null,
      "usedDate": null,
      "usedByUserId": null
    }
  ],
  "message": "Codes retrieved successfully"
}
```

---

#### 3.7 Get Code By ID
**GET** `/api/admin/sponsorship/codes/{codeId}`

**Example:**
```http
GET {{baseUrl}}/api/admin/sponsorship/codes/1001
Authorization: Bearer {{adminToken}}
```

**Response:** Same structure as Get All Codes (single object)

---

#### 3.8 Deactivate Code
**POST** `/api/admin/sponsorship/codes/{codeId}/deactivate`

**Request Body:**
```json
{
  "reason": "Code compromised, deactivating for security"
}
```

**Example:**
```http
POST {{baseUrl}}/api/admin/sponsorship/codes/1001/deactivate
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "reason": "Code compromised, deactivating for security"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Code deactivated successfully"
}
```

---

#### 3.9 Bulk Send Codes
**POST** `/api/admin/sponsorship/codes/bulk-send`

**Request Body:**
```json
{
  "sponsorId": 234,
  "purchaseId": 892,
  "recipients": [
    {
      "phoneNumber": "+905551111111",
      "name": "Farmer 1"
    },
    {
      "phoneNumber": "+905552222222",
      "name": "Farmer 2"
    },
    {
      "phoneNumber": "+905553333333",
      "name": "Farmer 3"
    }
  ],
  "sendVia": "SMS"
}
```

**Field Details:**
- `sponsorId` (int, required) - Sponsor user ID
- `purchaseId` (int, required) - Purchase ID to get codes from
- `recipients` (array, required) - List of recipients
  - `phoneNumber` (string, required) - Phone number with country code
  - `name` (string, optional) - Recipient name
- `sendVia` (string, default: "SMS") - Send method: SMS, WhatsApp, Email

**Example:**
```http
POST {{baseUrl}}/api/admin/sponsorship/codes/bulk-send
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "sponsorId": 234,
  "purchaseId": 892,
  "recipients": [
    {"phoneNumber": "+905551111111", "name": "Ahmet"},
    {"phoneNumber": "+905552222222", "name": "Mehmet"},
    {"phoneNumber": "+905553333333", "name": "Ali"}
  ],
  "sendVia": "SMS"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Successfully sent 3 codes via SMS"
}
```

---

#### 3.10 Get Sponsor Detailed Report
**GET** `/api/admin/sponsorship/sponsor/{sponsorId}/report`

**Example:**
```http
GET {{baseUrl}}/api/admin/sponsorship/sponsor/234/report
Authorization: Bearer {{adminToken}}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "sponsorId": 234,
    "sponsorName": "Sponsor Company",
    "totalPurchases": 5,
    "totalCodesGenerated": 50,
    "totalCodesSent": 30,
    "totalCodesUsed": 15,
    "totalSpent": 4999.50,
    "currency": "TRY",
    "codeDistribution": {
      "unused": 35,
      "sent": 15,
      "used": 15
    },
    "purchases": [
      {
        "id": 892,
        "quantity": 10,
        "totalAmount": 999.90,
        "status": "Active",
        "paymentStatus": "Completed",
        "codesGenerated": 10,
        "codesSent": 3,
        "codesUsed": 1,
        "createdDate": "2025-01-20T10:00:00"
      }
    ]
  },
  "message": "Sponsor report retrieved successfully"
}
```

---

### 4. Analytics & Reporting

#### 4.1 Get User Statistics
**GET** `/api/admin/analytics/users`

**Query Parameters:**
- `startDate` (datetime?, optional) - Start date for filtering
- `endDate` (datetime?, optional) - End date for filtering

**Example:**
```http
GET {{baseUrl}}/api/admin/analytics/users?startDate=2025-01-01&endDate=2025-01-23
Authorization: Bearer {{adminToken}}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalUsers": 1523,
    "activeUsers": 1420,
    "inactiveUsers": 103,
    "newUsersToday": 15,
    "newUsersThisWeek": 87,
    "newUsersThisMonth": 342,
    "registrationTrend": [
      {"date": "2025-01-01", "count": 12},
      {"date": "2025-01-02", "count": 15}
    ]
  },
  "message": "User statistics retrieved successfully"
}
```

---

#### 4.2 Get Subscription Statistics
**GET** `/api/admin/analytics/subscriptions`

**Query Parameters:**
- `startDate` (datetime?, optional)
- `endDate` (datetime?, optional)

**Example:**
```http
GET {{baseUrl}}/api/admin/analytics/subscriptions?startDate=2025-01-01
Authorization: Bearer {{adminToken}}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalSubscriptions": 1200,
    "activeSubscriptions": 980,
    "expiredSubscriptions": 220,
    "subscriptionsByTier": {
      "Trial": 450,
      "S": 320,
      "M": 250,
      "L": 120,
      "XL": 60
    },
    "sponsoredSubscriptions": 180,
    "revenueThisMonth": 45000.00,
    "averageSubscriptionDuration": 3.5
  },
  "message": "Subscription statistics retrieved successfully"
}
```

---

#### 4.3 Get Sponsorship Statistics
**GET** `/api/admin/analytics/sponsorship`

**Query Parameters:**
- `startDate` (datetime?, optional)
- `endDate` (datetime?, optional)

**Example:**
```http
GET {{baseUrl}}/api/admin/analytics/sponsorship
Authorization: Bearer {{adminToken}}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalPurchases": 87,
    "totalCodesGenerated": 1250,
    "totalCodesSent": 890,
    "totalCodesUsed": 567,
    "codeRedemptionRate": 63.7,
    "totalRevenue": 124750.00,
    "currency": "TRY",
    "averagePurchaseSize": 14.37,
    "topSponsors": [
      {"sponsorId": 234, "sponsorName": "Company A", "totalPurchases": 12, "totalSpent": 25000.00}
    ]
  },
  "message": "Sponsorship statistics retrieved successfully"
}
```

---

#### 4.4 Get Dashboard Overview
**GET** `/api/admin/analytics/dashboard`

**Example:**
```http
GET {{baseUrl}}/api/admin/analytics/dashboard
Authorization: Bearer {{adminToken}}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "userStatistics": { /* User stats object */ },
    "subscriptionStatistics": { /* Subscription stats object */ },
    "sponsorshipStatistics": { /* Sponsorship stats object */ },
    "generatedAt": "2025-01-23T14:30:00"
  },
  "message": "Dashboard data retrieved successfully"
}
```

---

#### 4.5 Export Statistics (CSV)
**GET** `/api/admin/analytics/export`

**Query Parameters:**
- `startDate` (datetime?, optional)
- `endDate` (datetime?, optional)

**Example:**
```http
GET {{baseUrl}}/api/admin/analytics/export?startDate=2025-01-01&endDate=2025-01-23
Authorization: Bearer {{adminToken}}
```

**Response:**
- Content-Type: `text/csv`
- File download: `ziraai-statistics-2025-01-23-143000.csv`

**CSV Format:**
```csv
Metric,Value
Total Users,1523
Active Users,1420
Total Subscriptions,1200
Active Subscriptions,980
Total Purchases,87
Codes Generated,1250
```

---

### 5. Audit Logs

#### 5.1 Get All Audit Logs
**GET** `/api/admin/audit`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 50)
- `action` (string, optional) - Filter by action
- `entityType` (string, optional) - Filter by entity type
- `isOnBehalfOf` (bool?, optional) - Filter by OBO status
- `startDate` (datetime?, optional)
- `endDate` (datetime?, optional)

**Example:**
```http
GET {{baseUrl}}/api/admin/audit?action=DeactivateUser&page=1&pageSize=20
Authorization: Bearer {{adminToken}}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 5001,
      "action": "DeactivateUser",
      "adminUserId": 1,
      "adminUserEmail": "admin@ziraai.com",
      "targetUserId": 123,
      "entityType": "User",
      "entityId": 123,
      "isOnBehalfOf": false,
      "reason": "User requested account closure",
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0...",
      "requestPath": "/api/admin/users/123/deactivate",
      "beforeState": "{\"isActive\":true}",
      "afterState": "{\"isActive\":false}",
      "createdDate": "2025-01-23T10:30:00"
    }
  ],
  "message": "Audit logs retrieved successfully"
}
```

---

#### 5.2 Get Audit Logs By Admin
**GET** `/api/admin/audit/admin/{adminUserId}`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 50)
- `startDate` (datetime?, optional)
- `endDate` (datetime?, optional)

**Example:**
```http
GET {{baseUrl}}/api/admin/audit/admin/1?page=1&pageSize=20
Authorization: Bearer {{adminToken}}
```

**Response:** Same structure as Get All Audit Logs

---

#### 5.3 Get Audit Logs By Target User
**GET** `/api/admin/audit/target/{targetUserId}`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 50)
- `startDate` (datetime?, optional)
- `endDate` (datetime?, optional)

**Example:**
```http
GET {{baseUrl}}/api/admin/audit/target/123?page=1&pageSize=20
Authorization: Bearer {{adminToken}}
```

**Response:** Same structure as Get All Audit Logs

---

#### 5.4 Get On-Behalf-Of Logs
**GET** `/api/admin/audit/on-behalf-of`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 50)
- `startDate` (datetime?, optional)
- `endDate` (datetime?, optional)

**Example:**
```http
GET {{baseUrl}}/api/admin/audit/on-behalf-of?page=1&pageSize=20
Authorization: Bearer {{adminToken}}
```

**Response:** Same structure as Get All Audit Logs (filtered for `isOnBehalfOf = true`)

---

### 6. Plant Analysis Management

#### 6.1 Create Analysis On Behalf Of User
**POST** `/api/admin/plant-analysis/on-behalf-of`

**Request Body:**
```json
{
  "targetUserId": 123,
  "imageUrl": "https://storage.example.com/images/plant-123.jpg",
  "analysisResult": "Plant appears healthy with minor leaf discoloration...",
  "notes": "Manual analysis created for customer support case #456"
}
```

**Example:**
```http
POST {{baseUrl}}/api/admin/plant-analysis/on-behalf-of
Authorization: Bearer {{adminToken}}
Content-Type: application/json

{
  "targetUserId": 123,
  "imageUrl": "https://storage.example.com/images/plant-123.jpg",
  "analysisResult": "Plant appears healthy...",
  "notes": "Manual analysis for support case"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 7890,
    "userId": 123,
    "imageUrl": "https://storage.example.com/images/plant-123.jpg",
    "analysisResult": "Plant appears healthy...",
    "createdDate": "2025-01-23T14:30:00"
  },
  "message": "Plant analysis created successfully on behalf of user"
}
```

---

#### 6.2 Get User Analyses
**GET** `/api/admin/plant-analysis/user/{userId}`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 50)
- `status` (string, optional) - Filter by status
- `isOnBehalfOf` (bool?, optional) - Filter by OBO status

**Example:**
```http
GET {{baseUrl}}/api/admin/plant-analysis/user/123?page=1&pageSize=20
Authorization: Bearer {{adminToken}}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 7890,
      "userId": 123,
      "imageUrl": "https://storage.example.com/images/plant-123.jpg",
      "analysisResult": "Plant appears healthy...",
      "status": "Completed",
      "createdDate": "2025-01-23T14:30:00",
      "isOnBehalfOf": true
    }
  ],
  "message": "User analyses retrieved successfully"
}
```

---

#### 6.3 Get All On-Behalf-Of Analyses
**GET** `/api/admin/plant-analysis/on-behalf-of`

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 50)

**Example:**
```http
GET {{baseUrl}}/api/admin/plant-analysis/on-behalf-of?page=1
Authorization: Bearer {{adminToken}}
```

**Response:**
```json
{
  "success": true,
  "message": "Use audit logs to view all OBO operations: GET /api/admin/audit/on-behalf-of"
}
```

*(This endpoint redirects to audit logs for comprehensive OBO tracking)*

---

## Test Scenarios

### Scenario 1: Complete User Lifecycle

**Objective:** Test full user management workflow

#### Steps:

**1. Get Active Users**
```http
GET {{baseUrl}}/api/admin/users?page=1&pageSize=20&isActive=true
```
✅ Verify: Status 200, users list returned

**2. Search for User**
```http
GET {{baseUrl}}/api/admin/users/search?searchTerm=ahmet
```
✅ Verify: Results match search term (case-insensitive)

**3. Get User Details**
```http
GET {{baseUrl}}/api/admin/users/123
```
✅ Verify: Full user details returned, `isActive = true`

**4. Deactivate User**
```http
POST {{baseUrl}}/api/admin/users/123/deactivate
Body: {"reason": "TEST: Temporary deactivation"}
```
✅ Verify: Success message, audit log created

**5. Verify Deactivation**
```http
GET {{baseUrl}}/api/admin/users/123
```
✅ Verify: `isActive = false`, `deactivatedDate` populated

**6. Check Audit Log**
```http
GET {{baseUrl}}/api/admin/audit/target/123
```
✅ Verify: DeactivateUser action logged

**7. Reactivate User**
```http
POST {{baseUrl}}/api/admin/users/123/reactivate
Body: {"reason": "TEST: Reactivating after test"}
```
✅ Verify: Success message

**8. Verify Reactivation**
```http
GET {{baseUrl}}/api/admin/users/123
```
✅ Verify: `isActive = true`, `deactivatedDate = null`

---

### Scenario 2: Subscription Assignment & Extension

**Objective:** Test subscription lifecycle

#### Steps:

**1. Assign Subscription**
```http
POST {{baseUrl}}/api/admin/subscriptions/assign
Body: {
  "userId": 123,
  "subscriptionTierId": 3,
  "durationMonths": 1,
  "isSponsoredSubscription": false,
  "sponsorId": null,
  "notes": "TEST: Premium subscription for testing"
}
```
✅ Verify: Success message with end date
✅ Save: `subscriptionId` from response

**2. Get Subscription Details**
```http
GET {{baseUrl}}/api/admin/subscriptions/{subscriptionId}
```
✅ Verify: Status = "Active", tierName = "M"

**3. Extend Subscription**
```http
POST {{baseUrl}}/api/admin/subscriptions/{subscriptionId}/extend
Body: {
  "extensionMonths": 1,
  "notes": "TEST: Extension test"
}
```
✅ Verify: End date extended by 1 month

**4. Verify Extension**
```http
GET {{baseUrl}}/api/admin/subscriptions/{subscriptionId}
```
✅ Verify: End date is 2 months from start

**5. Check Analytics**
```http
GET {{baseUrl}}/api/admin/analytics/subscriptions
```
✅ Verify: Active subscriptions count includes test subscription

**6. Cancel Subscription**
```http
POST {{baseUrl}}/api/admin/subscriptions/{subscriptionId}/cancel
Body: {"cancellationReason": "TEST: Cleanup"}
```
✅ Verify: Status changed to "Cancelled"

---

### Scenario 3: Sponsor OBO Operations

**Objective:** Test complete sponsor workflow with OBO features

#### Steps:

**1. Create Purchase On Behalf Of Sponsor**
```http
POST {{baseUrl}}/api/admin/sponsorship/purchases/create-on-behalf-of
Body: {
  "sponsorId": 234,
  "subscriptionTierId": 3,
  "quantity": 10,
  "unitPrice": 99.99,
  "autoApprove": true,
  "paymentMethod": "BankTransfer",
  "paymentReference": "TEST-2025-001",
  "companyName": "Test Company",
  "taxNumber": "1234567890",
  "invoiceAddress": "Istanbul, Turkey",
  "codePrefix": "TEST",
  "validityDays": 365,
  "notes": "TEST: OBO purchase creation"
}
```
✅ Verify: Purchase created, paymentStatus = "Completed"
✅ Save: `purchaseId` from response

**2. Get Purchase Details**
```http
GET {{baseUrl}}/api/admin/sponsorship/purchases/{purchaseId}
```
✅ Verify: 10 codes generated, company info present

**3. Get Unused Codes**
```http
GET {{baseUrl}}/api/admin/sponsorship/codes?purchaseId={purchaseId}&isUsed=false
```
✅ Verify: At least 3 unused codes available

**4. Bulk Send Codes**
```http
POST {{baseUrl}}/api/admin/sponsorship/codes/bulk-send
Body: {
  "sponsorId": 234,
  "purchaseId": {purchaseId},
  "recipients": [
    {"phoneNumber": "+905551111111", "name": "Test Farmer 1"},
    {"phoneNumber": "+905552222222", "name": "Test Farmer 2"},
    {"phoneNumber": "+905553333333", "name": "Test Farmer 3"}
  ],
  "sendVia": "SMS"
}
```
✅ Verify: 3 codes sent successfully

**5. Verify Codes Marked As Sent**
```http
GET {{baseUrl}}/api/admin/sponsorship/codes?purchaseId={purchaseId}
```
✅ Verify: 3 codes have `linkSentDate` populated

**6. Get Sponsor Report**
```http
GET {{baseUrl}}/api/admin/sponsorship/sponsor/234/report
```
✅ Verify: Purchase appears, codesSent = 3

**7. Check OBO Audit Log**
```http
GET {{baseUrl}}/api/admin/audit/on-behalf-of?action=CreatePurchaseOnBehalfOf
```
✅ Verify: OBO action logged, `isOnBehalfOf = true`

---

### Scenario 4: Analytics Export

**Objective:** Test statistics and CSV export

#### Steps:

**1. Get User Statistics**
```http
GET {{baseUrl}}/api/admin/analytics/users?startDate=2025-01-01
```
✅ Verify: totalUsers > 0, activeUsers + inactiveUsers = totalUsers

**2. Get Subscription Statistics**
```http
GET {{baseUrl}}/api/admin/analytics/subscriptions
```
✅ Verify: Tier breakdown includes all tiers

**3. Get Sponsorship Statistics**
```http
GET {{baseUrl}}/api/admin/analytics/sponsorship
```
✅ Verify: Redemption rate between 0-100

**4. Get Dashboard Overview**
```http
GET {{baseUrl}}/api/admin/analytics/dashboard
```
✅ Verify: All three statistics objects present

**5. Export CSV**
```http
GET {{baseUrl}}/api/admin/analytics/export?startDate=2025-01-01&endDate=2025-01-23
```
✅ Verify: Content-Type = text/csv, file downloads
✅ Verify: CSV contains headers and data

---

## Database Verification

### After User Deactivation:
```sql
SELECT "UserId", "IsActive", "DeactivatedDate", "DeactivatedByAdminId"
FROM "Users"
WHERE "UserId" = 123;
```
**Expected:** `IsActive = false`, `DeactivatedDate` not null

### After Subscription Assignment:
```sql
SELECT "Id", "UserId", "SubscriptionTierId", "Status", "StartDate", "EndDate"
FROM "UserSubscriptions"
WHERE "UserId" = 123
ORDER BY "CreatedDate" DESC
LIMIT 1;
```
**Expected:** `Status = 'Active'`, end date = start date + duration months

### After Purchase Creation:
```sql
SELECT p.*,
       (SELECT COUNT(*) FROM "SponsorshipCodes" WHERE "SponsorshipPurchaseId" = p."Id") as "CodeCount"
FROM "SponsorshipPurchases" p
WHERE p."Id" = {purchaseId};
```
**Expected:** `PaymentStatus = 'Completed'`, CodeCount = quantity

### After Bulk Code Send:
```sql
SELECT "Id", "Code", "LinkSentDate", "RecipientPhone", "RecipientName"
FROM "SponsorshipCodes"
WHERE "SponsorshipPurchaseId" = {purchaseId}
  AND "LinkSentDate" IS NOT NULL;
```
**Expected:** 3 records with recipient info populated

### Check Audit Log:
```sql
SELECT "Action", "AdminUserId", "TargetUserId", "EntityType", "IsOnBehalfOf", "Reason"
FROM "AdminOperationLogs"
ORDER BY "CreatedDate" DESC
LIMIT 10;
```
**Expected:** All admin actions logged with proper details

---

## Troubleshooting

### Issue: 401 Unauthorized

**Symptoms:**
```json
{"success": false, "message": "Authorization token is required"}
```

**Solutions:**
1. Verify `{{adminToken}}` environment variable is set
2. Re-login to get fresh token
3. Check token expiration (default: 60 minutes)
4. Ensure header format: `Authorization: Bearer <token>`

---

### Issue: 403 Forbidden

**Symptoms:**
```json
{"success": false, "message": "Insufficient permissions"}
```

**Solutions:**
1. Verify admin user has `Admin` and `admin.*` claims:
```sql
SELECT oc."Name"
FROM "UserClaims" uc
INNER JOIN "OperationClaims" oc ON uc."ClaimId" = oc."Id"
WHERE uc."UserId" = <your_admin_user_id>;
```
2. Expected claims: `Admin`, `admin.users.manage`, `admin.subscriptions.manage`, etc.

---

### Issue: Subscription Tier Not Found

**Symptoms:**
```json
{"success": false, "message": "Subscription tier not found"}
```

**Solution:**
Check valid tier IDs:
```sql
SELECT "Id", "TierName" FROM "SubscriptionTiers";
```
Use correct tier ID (1=Trial, 2=S, 3=M, 4=L, 5=XL)

---

### Issue: Insufficient Codes for Bulk Send

**Symptoms:**
```json
{"success": false, "message": "Insufficient unused codes"}
```

**Solution:**
Check available codes:
```sql
SELECT COUNT(*)
FROM "SponsorshipCodes"
WHERE "SponsorshipPurchaseId" = {purchaseId}
  AND "IsUsed" = false
  AND "IsActive" = true;
```
Create new purchase or use smaller recipient list.

---

### Issue: CSV Export Empty

**Symptoms:** CSV file downloads but has no data rows

**Solution:**
1. Adjust date range to include data:
```http
GET {{baseUrl}}/api/admin/analytics/export?startDate=2024-01-01&endDate=2025-12-31
```
2. Verify data exists in specified date range

---

## Performance Benchmarks

| Endpoint | Expected | Max Acceptable |
|----------|----------|----------------|
| Get All Users (50 items) | < 200ms | 500ms |
| Get User By ID | < 50ms | 150ms |
| Deactivate User | < 100ms | 300ms |
| Bulk Deactivate (10) | < 500ms | 1000ms |
| Assign Subscription | < 150ms | 400ms |
| Create Purchase OBO | < 500ms | 1500ms |
| Bulk Send Codes (10) | < 1000ms | 3000ms |
| Get Statistics | < 300ms | 800ms |
| Export CSV | < 1000ms | 3000ms |
| Get Dashboard | < 500ms | 1200ms |

---

## Data Cleanup

### After Testing:
```sql
-- Clean test users
DELETE FROM "Users" WHERE "Email" LIKE '%@test.com';

-- Clean test subscriptions
DELETE FROM "UserSubscriptions" WHERE "SponsorshipNotes" LIKE 'TEST:%';

-- Clean test purchases
DELETE FROM "SponsorshipPurchases" WHERE "Notes" LIKE 'TEST:%';

-- Clean test codes
DELETE FROM "SponsorshipCodes" WHERE "CodePrefix" = 'TEST';

-- Clean test audit logs
DELETE FROM "AdminOperationLogs" WHERE "Reason" LIKE 'TEST:%';
```

---

## Quick Reference

### All Endpoints Summary (31 Total)

**User Management (6):**
- GET /api/admin/users
- GET /api/admin/users/{userId}
- GET /api/admin/users/search
- POST /api/admin/users/{userId}/deactivate
- POST /api/admin/users/{userId}/reactivate
- POST /api/admin/users/bulk/deactivate

**Subscription Management (6):**
- GET /api/admin/subscriptions
- GET /api/admin/subscriptions/{id}
- POST /api/admin/subscriptions/assign
- POST /api/admin/subscriptions/{id}/extend
- POST /api/admin/subscriptions/{id}/cancel
- POST /api/admin/subscriptions/bulk/cancel

**Sponsorship Management (10):**
- GET /api/admin/sponsorship/purchases
- GET /api/admin/sponsorship/purchases/{id}
- POST /api/admin/sponsorship/purchases/{id}/approve
- POST /api/admin/sponsorship/purchases/{id}/refund
- POST /api/admin/sponsorship/purchases/create-on-behalf-of
- GET /api/admin/sponsorship/codes
- GET /api/admin/sponsorship/codes/{id}
- POST /api/admin/sponsorship/codes/{id}/deactivate
- POST /api/admin/sponsorship/codes/bulk-send
- GET /api/admin/sponsorship/sponsor/{id}/report

**Analytics (5):**
- GET /api/admin/analytics/users
- GET /api/admin/analytics/subscriptions
- GET /api/admin/analytics/sponsorship
- GET /api/admin/analytics/dashboard
- GET /api/admin/analytics/export

**Audit Logs (4):**
- GET /api/admin/audit
- GET /api/admin/audit/admin/{adminUserId}
- GET /api/admin/audit/target/{targetUserId}
- GET /api/admin/audit/on-behalf-of

**Plant Analysis (3):**
- POST /api/admin/plant-analysis/on-behalf-of
- GET /api/admin/plant-analysis/user/{userId}
- GET /api/admin/plant-analysis/on-behalf-of

---

**End of Testing Guide**

✅ All endpoints verified against actual controller code
✅ All request/response payloads validated
✅ All field types and constraints documented

For issues or updates, refer to the main API documentation.
