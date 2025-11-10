# Admin Sponsor API Reference

Comprehensive API documentation for admin sponsor reports, analytics, and management operations.

**Base URL:** `{{base_url}}/api`
**API Version:** `v1`
**Authentication:** JWT Bearer Token (Admin role required)
**Header:** `x-dev-arch-version: 1`

---

## Table of Contents

1. [Analytics & Statistics](#1-analytics--statistics)
2. [Sponsor Management](#2-sponsor-management)
3. [Purchase Management](#3-purchase-management)
4. [Code Management](#4-code-management)
5. [Analysis Management](#5-analysis-management)
6. [Comparison Analytics](#6-comparison-analytics)
7. [Bulk Operations](#7-bulk-operations)

---

## 1. Analytics & Statistics

### 1.1 Get User Statistics

**Endpoint:** `GET /api/admin/analytics/user-statistics`

**Description:** Get comprehensive user statistics and metrics

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | DateTime | No | Start date for filtering (ISO 8601) |
| endDate | DateTime | No | End date for filtering (ISO 8601) |

**Request Example:**
```http
GET /api/admin/analytics/user-statistics?startDate=2025-01-01&endDate=2025-12-31
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "totalUsers": 1250,
    "activeUsers": 980,
    "inactiveUsers": 270,
    "farmerUsers": 850,
    "sponsorUsers": 150,
    "adminUsers": 5,
    "usersRegisteredToday": 12,
    "usersRegisteredThisWeek": 85,
    "usersRegisteredThisMonth": 340,
    "startDate": "2025-01-01T00:00:00Z",
    "endDate": "2025-12-31T23:59:59Z",
    "generatedAt": "2025-11-10T14:30:00Z"
  },
  "success": true,
  "message": "User statistics retrieved successfully"
}
```

---

### 1.2 Get Subscription Statistics

**Endpoint:** `GET /api/admin/analytics/subscription-statistics`

**Description:** Get subscription system metrics including revenue and tier distribution

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | DateTime | No | Start date for filtering |
| endDate | DateTime | No | End date for filtering |

**Request Example:**
```http
GET /api/admin/analytics/subscription-statistics?startDate=2025-01-01
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "totalSubscriptions": 2500,
    "activeSubscriptions": 1850,
    "expiredSubscriptions": 650,
    "trialSubscriptions": 450,
    "sponsoredSubscriptions": 890,
    "paidSubscriptions": 1160,
    "subscriptionsByTier": {
      "Trial": 450,
      "S": 620,
      "M": 890,
      "L": 420,
      "XL": 120
    },
    "totalRevenue": 45680.50,
    "averageSubscriptionDuration": 45.8,
    "startDate": "2025-01-01T00:00:00Z",
    "endDate": null,
    "generatedAt": "2025-11-10T14:30:00Z"
  },
  "success": true,
  "message": "Subscription statistics retrieved successfully"
}
```

---

### 1.3 Get Sponsorship Statistics

**Endpoint:** `GET /api/admin/analytics/sponsorship`

**Description:** Get comprehensive sponsorship system statistics including purchases, codes, and redemption rates

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | DateTime | No | Start date for filtering |
| endDate | DateTime | No | End date for filtering |

**Request Example:**
```http
GET /api/admin/analytics/sponsorship?startDate=2025-01-01&endDate=2025-12-31
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "totalPurchases": 245,
    "completedPurchases": 210,
    "pendingPurchases": 15,
    "refundedPurchases": 20,
    "totalRevenue": 125680.75,
    "totalCodesGenerated": 12450,
    "totalCodesUsed": 8920,
    "totalCodesActive": 3150,
    "totalCodesExpired": 380,
    "codeRedemptionRate": 71.65,
    "averagePurchaseAmount": 512.57,
    "totalQuantityPurchased": 12450,
    "uniqueSponsorCount": 87,
    "startDate": "2025-01-01T00:00:00Z",
    "endDate": "2025-12-31T23:59:59Z",
    "generatedAt": "2025-11-10T14:30:00Z"
  },
  "success": true,
  "message": "Sponsorship statistics retrieved successfully"
}
```

---

### 1.4 Get Dashboard Overview

**Endpoint:** `GET /api/admin/analytics/dashboard-overview`

**Description:** Get all key metrics in a single request for dashboard display

**Query Parameters:** None

**Request Example:**
```http
GET /api/admin/analytics/dashboard-overview
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "userStatistics": {
      "totalUsers": 1250,
      "activeUsers": 980,
      "farmerUsers": 850,
      "sponsorUsers": 150,
      "adminUsers": 5
    },
    "subscriptionStatistics": {
      "totalSubscriptions": 2500,
      "activeSubscriptions": 1850,
      "totalRevenue": 45680.50
    },
    "sponsorshipStatistics": {
      "totalPurchases": 245,
      "totalRevenue": 125680.75,
      "codeRedemptionRate": 71.65
    },
    "generatedAt": "2025-11-10T14:30:00Z"
  },
  "success": true,
  "message": "Dashboard data retrieved successfully"
}
```

---

### 1.5 Get Activity Logs

**Endpoint:** `GET /api/admin/analytics/activity-logs`

**Description:** Get system activity logs with filtering and pagination

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | int | No | 1 | Page number |
| pageSize | int | No | 10 | Page size |
| userId | int | No | null | Filter by admin or target user ID |
| actionType | string | No | null | Filter by action type |
| startDate | DateTime | No | null | Start date for filtering |
| endDate | DateTime | No | null | End date for filtering |

**Request Example:**
```http
GET /api/admin/analytics/activity-logs?page=1&pageSize=10&actionType=CreatePurchase
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "logs": [
      {
        "id": 1234,
        "adminUserId": 5,
        "targetUserId": 150,
        "action": "CreatePurchase",
        "details": "Created purchase for sponsor John Doe - 100 S tier codes",
        "timestamp": "2025-11-10T14:25:00Z",
        "ipAddress": "192.168.1.100"
      }
    ],
    "page": 1,
    "pageSize": 10,
    "totalCount": 1450
  },
  "success": true,
  "message": "Activity logs retrieved successfully"
}
```

---

### 1.6 Export Statistics

**Endpoint:** `GET /api/admin/analytics/export`

**Description:** Export statistics as CSV file

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | DateTime | No | Start date for filtering |
| endDate | DateTime | No | End date for filtering |

**Request Example:**
```http
GET /api/admin/analytics/export?startDate=2025-01-01&endDate=2025-12-31
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```
Content-Type: text/csv
Content-Disposition: attachment; filename="ziraai-statistics-2025-11-10-143000.csv"

Date,Users,Subscriptions,Purchases,Revenue
2025-01-01,1200,2400,240,120000
...
```

---

## 2. Sponsor Management

### 2.1 Get All Sponsors

**Endpoint:** `GET /api/admin/sponsorship/sponsors`

**Description:** Get list of all users with Sponsor role (GroupId = 3)

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | int | No | 1 | Page number |
| pageSize | int | No | 50 | Page size |
| isActive | bool | No | null | Filter by active status |
| status | string | No | null | Filter by status |
| searchTerm | string | No | null | Search by email, name, or phone |

**Request Example:**
```http
GET /api/admin/sponsorship/sponsors?page=1&pageSize=50&isActive=true&searchTerm=john
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": [
    {
      "userId": 150,
      "fullName": "John Doe",
      "email": "john.doe@example.com",
      "mobilePhones": "05321234567",
      "address": "Istanbul, Turkey",
      "notes": "Premium sponsor",
      "gender": 1,
      "status": true,
      "isActive": true
    }
  ],
  "success": true,
  "message": "Retrieved 1 sponsors successfully"
}
```

---

### 2.2 Get Sponsor Detailed Report

**Endpoint:** `GET /api/admin/sponsorship/sponsors/{sponsorId}/detailed-report`

**Description:** Get comprehensive report for a specific sponsor including purchases, codes, and usage statistics

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sponsorId | int | Yes | Sponsor user ID |

**Request Example:**
```http
GET /api/admin/sponsorship/sponsors/150/detailed-report
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "sponsorId": 150,
    "sponsorName": "John Doe",
    "sponsorEmail": "john.doe@example.com",
    "totalPurchases": 12,
    "activePurchases": 8,
    "pendingPurchases": 1,
    "cancelledPurchases": 1,
    "completedPurchases": 10,
    "totalSpent": 15680.50,
    "totalCodesGenerated": 1200,
    "totalCodesSent": 950,
    "totalCodesUsed": 680,
    "totalCodesActive": 520,
    "totalCodesExpired": 0,
    "codeRedemptionRate": 71.57,
    "purchases": [
      {
        "purchaseId": 450,
        "purchaseDate": "2025-10-15T10:30:00Z",
        "tierName": "M",
        "quantity": 100,
        "totalAmount": 1500.00,
        "status": "Active",
        "paymentStatus": "Completed",
        "codesGenerated": 100,
        "codesUsed": 68
      }
    ],
    "codeDistribution": {
      "byTier": {
        "S": 400,
        "M": 600,
        "L": 200
      },
      "byStatus": {
        "Active": 520,
        "Used": 680,
        "Expired": 0
      }
    }
  },
  "success": true,
  "message": "Sponsor detailed report retrieved successfully"
}
```

---

## 3. Purchase Management

### 3.1 Get All Purchases

**Endpoint:** `GET /api/admin/sponsorship/purchases`

**Description:** Get all sponsorship purchases with pagination and filtering

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | int | No | 1 | Page number |
| pageSize | int | No | 50 | Page size |
| status | string | No | null | Filter by status (Active, Pending, Cancelled) |
| paymentStatus | string | No | null | Filter by payment status (Completed, Pending, Failed) |
| sponsorId | int | No | null | Filter by sponsor ID |

**Request Example:**
```http
GET /api/admin/sponsorship/purchases?page=1&pageSize=50&status=Active&sponsorId=150
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": [
    {
      "id": 450,
      "sponsorId": 150,
      "sponsor": {
        "userId": 150,
        "fullName": "John Doe",
        "email": "john.doe@example.com"
      },
      "subscriptionTierId": 2,
      "subscriptionTier": {
        "id": 2,
        "tierName": "M",
        "displayName": "Medium"
      },
      "quantity": 100,
      "unitPrice": 15.00,
      "totalAmount": 1500.00,
      "status": "Active",
      "paymentStatus": "Completed",
      "paymentMethod": "CreditCard",
      "purchaseDate": "2025-10-15T10:30:00Z",
      "validFrom": "2025-10-15T10:30:00Z",
      "validUntil": "2026-10-15T10:30:00Z"
    }
  ],
  "success": true,
  "message": "Purchases retrieved successfully"
}
```

---

### 3.2 Get Purchase By ID

**Endpoint:** `GET /api/admin/sponsorship/purchases/{purchaseId}`

**Description:** Get detailed information for a specific purchase

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| purchaseId | int | Yes | Purchase ID |

**Request Example:**
```http
GET /api/admin/sponsorship/purchases/450
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "id": 450,
    "sponsorId": 150,
    "subscriptionTierId": 2,
    "quantity": 100,
    "unitPrice": 15.00,
    "totalAmount": 1500.00,
    "status": "Active",
    "paymentStatus": "Completed",
    "paymentMethod": "CreditCard",
    "paymentReference": "PAY-20251015-450",
    "purchaseDate": "2025-10-15T10:30:00Z",
    "validFrom": "2025-10-15T10:30:00Z",
    "validUntil": "2026-10-15T10:30:00Z",
    "companyName": "ABC Tarım Ltd.",
    "taxNumber": "1234567890",
    "invoiceAddress": "Istanbul, Turkey",
    "notes": "Annual purchase"
  },
  "success": true,
  "message": "Purchase retrieved successfully"
}
```

---

### 3.3 Get Sponsorship Statistics

**Endpoint:** `GET /api/admin/sponsorship/statistics`

**Description:** Get sponsorship statistics (same as Analytics endpoint)

See [1.3 Get Sponsorship Statistics](#13-get-sponsorship-statistics) for details.

---

### 3.4 Approve Purchase

**Endpoint:** `POST /api/admin/sponsorship/purchases/{purchaseId}/approve`

**Description:** Approve a pending sponsorship purchase (generates codes)

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| purchaseId | int | Yes | Purchase ID to approve |

**Request Body:**
```json
{
  "notes": "Approved after payment verification"
}
```

**Request Example:**
```http
POST /api/admin/sponsorship/purchases/450/approve
Authorization: Bearer {token}
x-dev-arch-version: 1
Content-Type: application/json

{
  "notes": "Approved after payment verification"
}
```

**Response 200:**
```json
{
  "success": true,
  "message": "Purchase approved successfully. 100 codes generated."
}
```

---

### 3.5 Refund Purchase

**Endpoint:** `POST /api/admin/sponsorship/purchases/{purchaseId}/refund`

**Description:** Refund a sponsorship purchase (deactivates all codes)

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| purchaseId | int | Yes | Purchase ID to refund |

**Request Body:**
```json
{
  "refundReason": "Customer requested cancellation"
}
```

**Request Example:**
```http
POST /api/admin/sponsorship/purchases/450/refund
Authorization: Bearer {token}
x-dev-arch-version: 1
Content-Type: application/json

{
  "refundReason": "Customer requested cancellation"
}
```

**Response 200:**
```json
{
  "success": true,
  "message": "Purchase refunded successfully. All codes deactivated."
}
```

---

### 3.6 Create Purchase On Behalf Of Sponsor

**Endpoint:** `POST /api/admin/sponsorship/purchases/create-on-behalf-of`

**Description:** Create sponsorship purchase on behalf of a sponsor (supports manual/offline payments)

**Request Body:**
```json
{
  "sponsorId": 150,
  "subscriptionTierId": 2,
  "quantity": 100,
  "unitPrice": 15.00,
  "autoApprove": true,
  "paymentMethod": "BankTransfer",
  "paymentReference": "TRF-20251110-001",
  "companyName": "ABC Tarım Ltd.",
  "taxNumber": "1234567890",
  "invoiceAddress": "Ataşehir, Istanbul, Turkey",
  "codePrefix": "ABC",
  "validityDays": 365,
  "notes": "Offline payment - bank transfer received"
}
```

**Request Schema:**
| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| sponsorId | int | Yes | - | Sponsor user ID |
| subscriptionTierId | int | Yes | - | Tier ID (1=Trial, 2=S, 3=M, 4=L, 5=XL) |
| quantity | int | Yes | - | Number of codes to generate |
| unitPrice | decimal | Yes | - | Price per code |
| autoApprove | bool | No | false | Auto-approve without payment gateway |
| paymentMethod | string | No | "Manual" | Payment method |
| paymentReference | string | No | null | Payment reference/transaction ID |
| companyName | string | No | null | Invoice company name |
| taxNumber | string | No | null | Invoice tax number |
| invoiceAddress | string | No | null | Invoice address |
| codePrefix | string | No | null | Custom prefix for codes |
| validityDays | int | No | 365 | Code validity period |
| notes | string | No | null | Additional notes |

**Request Example:**
```http
POST /api/admin/sponsorship/purchases/create-on-behalf-of
Authorization: Bearer {token}
x-dev-arch-version: 1
Content-Type: application/json

{
  "sponsorId": 150,
  "subscriptionTierId": 2,
  "quantity": 100,
  "unitPrice": 15.00,
  "autoApprove": true,
  "paymentMethod": "BankTransfer"
}
```

**Response 200:**
```json
{
  "data": {
    "purchaseId": 451,
    "codesGenerated": 100,
    "totalAmount": 1500.00,
    "status": "Active"
  },
  "success": true,
  "message": "Purchase created and approved successfully. 100 codes generated."
}
```

---

## 4. Code Management

### 4.1 Get All Codes

**Endpoint:** `GET /api/admin/sponsorship/codes`

**Description:** Get all sponsorship codes with pagination and filtering

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | int | No | 1 | Page number |
| pageSize | int | No | 50 | Page size |
| isUsed | bool | No | null | Filter by usage status |
| isActive | bool | No | null | Filter by active status |
| sponsorId | int | No | null | Filter by sponsor ID |
| purchaseId | int | No | null | Filter by purchase ID |

**Request Example:**
```http
GET /api/admin/sponsorship/codes?page=1&pageSize=50&isUsed=false&isActive=true&sponsorId=150
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": [
    {
      "id": 12450,
      "code": "ABC-M-XY12AB",
      "sponsorId": 150,
      "sponsorshipPurchaseId": 450,
      "subscriptionTierId": 2,
      "isUsed": false,
      "isActive": true,
      "usedBy": null,
      "usedDate": null,
      "expirationDate": "2026-10-15T10:30:00Z",
      "createdDate": "2025-10-15T10:30:00Z",
      "notes": null
    }
  ],
  "success": true,
  "message": "Codes retrieved successfully"
}
```

---

### 4.2 Get Code By ID

**Endpoint:** `GET /api/admin/sponsorship/codes/{codeId}`

**Description:** Get detailed information for a specific code

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| codeId | int | Yes | Code ID |

**Request Example:**
```http
GET /api/admin/sponsorship/codes/12450
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "id": 12450,
    "code": "ABC-M-XY12AB",
    "sponsorId": 150,
    "sponsorshipPurchaseId": 450,
    "subscriptionTierId": 2,
    "isUsed": false,
    "isActive": true,
    "usedBy": null,
    "usedDate": null,
    "expirationDate": "2026-10-15T10:30:00Z",
    "createdDate": "2025-10-15T10:30:00Z",
    "notes": null
  },
  "success": true,
  "message": "Code retrieved successfully"
}
```

---

### 4.3 Deactivate Code

**Endpoint:** `POST /api/admin/sponsorship/codes/{codeId}/deactivate`

**Description:** Deactivate a sponsorship code (cannot be used after deactivation)

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| codeId | int | Yes | Code ID to deactivate |

**Request Body:**
```json
{
  "reason": "Fraud detected"
}
```

**Request Example:**
```http
POST /api/admin/sponsorship/codes/12450/deactivate
Authorization: Bearer {token}
x-dev-arch-version: 1
Content-Type: application/json

{
  "reason": "Fraud detected"
}
```

**Response 200:**
```json
{
  "success": true,
  "message": "Code deactivated successfully"
}
```

---

### 4.4 Bulk Send Codes

**Endpoint:** `POST /api/admin/sponsorship/codes/bulk-send`

**Description:** Send sponsorship codes to multiple recipients via SMS/WhatsApp/Email

**Request Body:**
```json
{
  "sponsorId": 150,
  "purchaseId": 450,
  "recipients": [
    {
      "phoneNumber": "05321234567",
      "name": "Ahmet Yılmaz"
    },
    {
      "phoneNumber": "05439876543",
      "name": "Mehmet Demir"
    }
  ],
  "sendVia": "SMS"
}
```

**Request Schema:**
| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| sponsorId | int | Yes | - | Sponsor user ID |
| purchaseId | int | Yes | - | Purchase ID to get codes from |
| recipients | array | Yes | - | List of recipients |
| recipients[].phoneNumber | string | Yes | - | Phone number (05XXXXXXXXX) |
| recipients[].name | string | No | null | Recipient name |
| sendVia | string | No | "SMS" | Delivery method (SMS, WhatsApp, Email) |

**Request Example:**
```http
POST /api/admin/sponsorship/codes/bulk-send
Authorization: Bearer {token}
x-dev-arch-version: 1
Content-Type: application/json

{
  "sponsorId": 150,
  "purchaseId": 450,
  "recipients": [
    {"phoneNumber": "05321234567", "name": "Ahmet Yılmaz"}
  ],
  "sendVia": "SMS"
}
```

**Response 200:**
```json
{
  "success": true,
  "message": "2 codes sent successfully via SMS"
}
```

---

## 5. Analysis Management

### 5.1 Get Sponsor Analyses (Admin View)

**Endpoint:** `GET /api/admin/sponsorship/sponsors/{sponsorId}/analyses`

**Description:** View all analyses for a specific sponsor (admin viewing sponsor perspective)

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sponsorId | int | Yes | Sponsor user ID |

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | int | No | 1 | Page number |
| pageSize | int | No | 20 | Page size |
| sortBy | string | No | "date" | Sort field (date, healthScore, cropType) |
| sortOrder | string | No | "desc" | Sort order (asc, desc) |
| filterByTier | string | No | null | Filter by tier (S, M, L, XL) |
| filterByCropType | string | No | null | Filter by crop type |
| startDate | DateTime | No | null | Filter start date |
| endDate | DateTime | No | null | Filter end date |
| dealerId | int | No | null | Filter by dealer ID |
| filterByMessageStatus | string | No | null | Filter by message status |
| hasUnreadMessages | bool | No | null | Filter by unread messages |

**Request Example:**
```http
GET /api/admin/sponsorship/sponsors/150/analyses?page=1&pageSize=20&filterByTier=M&sortBy=date&sortOrder=desc
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "items": [
      {
        "plantAnalysisId": 5678,
        "analysisDate": "2025-11-08T14:30:00Z",
        "cropType": "Tomato",
        "location": "Antalya",
        "overallHealthScore": 85,
        "primaryConcern": "Leaf spots detected",
        "farmerName": "Ahmet Yılmaz",
        "farmerPhone": "05321234567",
        "tierName": "M",
        "hasUnreadMessages": true,
        "unreadMessageCount": 2
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalRecords": 45,
    "totalPages": 3
  },
  "success": true,
  "message": "Admin retrieved 20 analyses for sponsor 150"
}
```

---

### 5.2 Get Sponsor Analysis Detail (Admin View)

**Endpoint:** `GET /api/admin/sponsorship/sponsors/{sponsorId}/analyses/{plantAnalysisId}`

**Description:** View detailed analysis information (admin viewing sponsor perspective)

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sponsorId | int | Yes | Sponsor user ID |
| plantAnalysisId | int | Yes | Plant analysis ID |

**Request Example:**
```http
GET /api/admin/sponsorship/sponsors/150/analyses/5678
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "plantAnalysisId": 5678,
    "analysisDate": "2025-11-08T14:30:00Z",
    "cropType": "Tomato",
    "location": "Antalya",
    "imageUrl": "https://storage.example.com/analysis-5678.jpg",
    "overallHealthScore": 85,
    "primaryConcern": "Leaf spots detected",
    "recommendations": "Apply fungicide treatment...",
    "farmerName": "Ahmet Yılmaz",
    "farmerPhone": "05321234567",
    "farmerEmail": "ahmet@example.com",
    "tierName": "M",
    "sponsorName": "John Doe",
    "sponsorCode": "ABC-M-XY12AB",
    "unreadMessageCount": 2
  },
  "success": true,
  "message": "Admin retrieved analysis 5678 detail for sponsor 150"
}
```

---

### 5.3 Get Sponsor Messages (Admin View)

**Endpoint:** `GET /api/admin/sponsorship/sponsors/{sponsorId}/messages`

**Description:** View message conversation between sponsor and farmer (admin view)

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sponsorId | int | Yes | Sponsor user ID |

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| farmerUserId | int | Yes | Farmer user ID |
| plantAnalysisId | int | Yes | Plant analysis ID |
| page | int | No | Page number (default: 1) |
| pageSize | int | No | Page size (default: 20) |

**Request Example:**
```http
GET /api/admin/sponsorship/sponsors/150/messages?farmerUserId=850&plantAnalysisId=5678&page=1&pageSize=20
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "items": [
    {
      "id": 9876,
      "fromUserId": 150,
      "toUserId": 850,
      "plantAnalysisId": 5678,
      "message": "Please apply the recommended treatment",
      "messageType": "Information",
      "isRead": true,
      "sentDate": "2025-11-08T15:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalRecords": 5,
  "totalPages": 1
}
```

---

### 5.4 Send Message As Sponsor (Admin OBO)

**Endpoint:** `POST /api/admin/sponsorship/sponsors/{sponsorId}/send-message`

**Description:** Send message on behalf of a sponsor (admin sending as sponsor)

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sponsorId | int | Yes | Sponsor user ID |

**Request Body:**
```json
{
  "farmerUserId": 850,
  "plantAnalysisId": 5678,
  "message": "Please monitor the treatment progress and send updates",
  "messageType": "Information",
  "subject": "Treatment Follow-up",
  "priority": "Normal",
  "category": "General"
}
```

**Request Schema:**
| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| farmerUserId | int | Yes | - | Farmer user ID (recipient) |
| plantAnalysisId | int | Yes | - | Plant analysis ID |
| message | string | Yes | - | Message content |
| messageType | string | No | "Information" | Message type |
| subject | string | No | null | Message subject |
| priority | string | No | "Normal" | Priority level |
| category | string | No | "General" | Message category |

**Request Example:**
```http
POST /api/admin/sponsorship/sponsors/150/send-message
Authorization: Bearer {token}
x-dev-arch-version: 1
Content-Type: application/json

{
  "farmerUserId": 850,
  "plantAnalysisId": 5678,
  "message": "Please monitor the treatment progress"
}
```

**Response 200:**
```json
{
  "data": {
    "messageId": 9877
  },
  "success": true,
  "message": "Message sent successfully"
}
```

---

### 5.5 Get Non-Sponsored Analyses

**Endpoint:** `GET /api/admin/sponsorship/non-sponsored/analyses`

**Description:** Get list of analyses without sponsor codes

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | int | No | 1 | Page number |
| pageSize | int | No | 20 | Page size |
| sortBy | string | No | "date" | Sort field (date, cropType, status) |
| sortOrder | string | No | "desc" | Sort order (asc, desc) |
| filterByCropType | string | No | null | Filter by crop type |
| startDate | DateTime | No | null | Filter start date |
| endDate | DateTime | No | null | Filter end date |
| filterByStatus | string | No | null | Filter by analysis status |
| userId | int | No | null | Filter by user ID |

**Request Example:**
```http
GET /api/admin/sponsorship/non-sponsored/analyses?page=1&pageSize=20&sortBy=date&sortOrder=desc
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "items": [
    {
      "plantAnalysisId": 5679,
      "analysisDate": "2025-11-09T10:00:00Z",
      "analysisStatus": "completed",
      "cropType": "Wheat",
      "location": "Konya",
      "userId": 851,
      "tierId": 1,
      "userFullName": "Mehmet Demir",
      "userEmail": "mehmet@example.com",
      "userPhone": "05439876543",
      "imageUrl": "https://storage.example.com/analysis-5679.jpg",
      "overallHealthScore": 78,
      "primaryConcern": "Pest infestation",
      "isOnBehalfOf": false,
      "createdByAdminId": null
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalRecords": 150,
  "totalPages": 8
}
```

---

### 5.6 Get Non-Sponsored Analysis Detail

**Endpoint:** `GET /api/admin/sponsorship/non-sponsored/analyses/{plantAnalysisId}`

**Description:** View detailed information for a non-sponsored analysis (same view as farmer sees)

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| plantAnalysisId | int | Yes | Plant analysis ID |

**Request Example:**
```http
GET /api/admin/sponsorship/non-sponsored/analyses/5679
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "plantAnalysisId": 5679,
    "analysisDate": "2025-11-09T10:00:00Z",
    "cropType": "Wheat",
    "location": "Konya",
    "imageUrl": "https://storage.example.com/analysis-5679.jpg",
    "overallHealthScore": 78,
    "primaryConcern": "Pest infestation",
    "recommendations": "Apply pesticide treatment immediately...",
    "analysisStatus": "completed",
    "farmerName": "Mehmet Demir",
    "farmerPhone": "05439876543"
  },
  "success": true,
  "message": "Non-sponsored analysis not found or Analysis retrieved successfully"
}
```

---

### 5.7 Get Non-Sponsored Farmer Detail

**Endpoint:** `GET /api/admin/sponsorship/non-sponsored/farmers/{userId}`

**Description:** Get detailed profile for a non-sponsored farmer including analysis statistics

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userId | int | Yes | Farmer user ID |

**Request Example:**
```http
GET /api/admin/sponsorship/non-sponsored/farmers/851
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "userId": 851,
    "fullName": "Mehmet Demir",
    "email": "mehmet@example.com",
    "mobilePhone": "05439876543",
    "status": true,
    "recordDate": "2025-08-15T10:00:00Z",
    "totalAnalyses": 15,
    "completedAnalyses": 12,
    "pendingAnalyses": 2,
    "failedAnalyses": 1,
    "firstAnalysisDate": "2025-08-20T10:00:00Z",
    "lastAnalysisDate": "2025-11-09T10:00:00Z",
    "averageHealthScore": 82,
    "cropTypes": ["Wheat", "Barley", "Tomato"],
    "commonConcerns": [
      "Pest infestation",
      "Nutrient deficiency",
      "Water stress"
    ],
    "recentAnalyses": [
      {
        "plantAnalysisId": 5679,
        "analysisDate": "2025-11-09T10:00:00Z",
        "cropType": "Wheat",
        "overallHealthScore": 78
      }
    ]
  },
  "success": true,
  "message": "Retrieved detail for non-sponsored farmer Mehmet Demir (15 analyses)"
}
```

---

## 6. Comparison Analytics

### 6.1 Get Sponsorship Comparison Analytics

**Endpoint:** `GET /api/admin/sponsorship/comparison/analytics`

**Description:** Compare sponsored vs non-sponsored analyses with comprehensive metrics

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | DateTime | No | Start date for filtering |
| endDate | DateTime | No | End date for filtering |

**Request Example:**
```http
GET /api/admin/sponsorship/comparison/analytics?startDate=2025-01-01&endDate=2025-12-31
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "dateRange": {
      "startDate": "2025-01-01T00:00:00Z",
      "endDate": "2025-12-31T23:59:59Z"
    },
    "totalAnalyses": 2500,
    "sponsorshipRate": 42.5,
    "sponsoredAnalytics": {
      "totalAnalyses": 1063,
      "completedAnalyses": 980,
      "pendingAnalyses": 68,
      "failedAnalyses": 15,
      "averageHealthScore": 87,
      "uniqueUsers": 650,
      "topCropTypes": {
        "Tomato": 320,
        "Wheat": 250,
        "Pepper": 180,
        "Cucumber": 150,
        "Barley": 120
      }
    },
    "nonSponsoredAnalytics": {
      "totalAnalyses": 1437,
      "completedAnalyses": 1250,
      "pendingAnalyses": 142,
      "failedAnalyses": 45,
      "averageHealthScore": 79,
      "uniqueUsers": 890,
      "topCropTypes": {
        "Wheat": 450,
        "Barley": 380,
        "Tomato": 250,
        "Corn": 180,
        "Pepper": 150
      }
    },
    "comparisonMetrics": {
      "averageHealthScoreDifference": 8,
      "completionRateSponsored": 92.19,
      "completionRateNonSponsored": 86.97,
      "completionRateDifference": 5.22,
      "userEngagementRatio": 0.73
    }
  },
  "success": true,
  "message": "Comparison analytics: 42.5% sponsorship rate (1063/2500)"
}
```

**Response Schema:**
| Field | Type | Description |
|-------|------|-------------|
| totalAnalyses | int | Total number of analyses in period |
| sponsorshipRate | decimal | Percentage of sponsored analyses |
| sponsoredAnalytics | object | Metrics for sponsored analyses |
| nonSponsoredAnalytics | object | Metrics for non-sponsored analyses |
| comparisonMetrics.averageHealthScoreDifference | int | Health score difference (sponsored - non-sponsored) |
| comparisonMetrics.completionRateSponsored | decimal | Completion rate for sponsored (%) |
| comparisonMetrics.completionRateNonSponsored | decimal | Completion rate for non-sponsored (%) |
| comparisonMetrics.completionRateDifference | decimal | Difference in completion rates |
| comparisonMetrics.userEngagementRatio | decimal | Sponsored users / non-sponsored users |

---

## 7. Bulk Operations

### 7.1 Get Bulk Code Distribution Job History

**Endpoint:** `GET /api/admin/sponsorship/bulk-code-distribution/history`

**Description:** Get comprehensive history of bulk code distribution jobs with filtering

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | int | No | 1 | Page number |
| pageSize | int | No | 50 | Page size |
| status | string | No | null | Filter by status (Pending, Processing, Completed, PartialSuccess, Failed) |
| sponsorId | int | No | null | Filter by sponsor ID |
| startDate | DateTime | No | null | Filter by creation start date |
| endDate | DateTime | No | null | Filter by creation end date |

**Request Example:**
```http
GET /api/admin/sponsorship/bulk-code-distribution/history?page=1&pageSize=50&status=Completed&sponsorId=150
Authorization: Bearer {token}
x-dev-arch-version: 1
```

**Response 200:**
```json
{
  "data": {
    "totalCount": 45,
    "page": 1,
    "pageSize": 50,
    "totalPages": 1,
    "jobs": [
      {
        "jobId": 123,
        "sponsorId": 150,
        "sponsorName": "John Doe",
        "sponsorEmail": "john.doe@example.com",
        "purchaseId": 450,
        "deliveryMethod": "SMS",
        "totalFarmers": 100,
        "processedFarmers": 100,
        "successfulDistributions": 95,
        "failedDistributions": 5,
        "status": "Completed",
        "createdDate": "2025-11-08T10:00:00Z",
        "startedDate": "2025-11-08T10:01:00Z",
        "completedDate": "2025-11-08T10:05:30Z",
        "originalFileName": "farmers-list.xlsx",
        "fileSize": 25680,
        "resultFileUrl": "https://storage.example.com/results/job-123-results.csv",
        "totalCodesDistributed": 95,
        "totalSmsSent": 95
      }
    ]
  },
  "success": true,
  "message": "Retrieved 1 jobs (Page 1/1, Total: 45)"
}
```

**Response Schema:**
| Field | Type | Description |
|-------|------|-------------|
| jobId | int | Bulk job ID |
| sponsorId | int | Sponsor user ID |
| sponsorName | string | Sponsor full name |
| sponsorEmail | string | Sponsor email |
| purchaseId | int | Purchase ID used for codes |
| deliveryMethod | string | Delivery method (SMS, WhatsApp, Email) |
| totalFarmers | int | Total farmers in uploaded file |
| processedFarmers | int | Number of farmers processed |
| successfulDistributions | int | Successful code distributions |
| failedDistributions | int | Failed distributions |
| status | string | Job status (Pending, Processing, Completed, PartialSuccess, Failed) |
| createdDate | DateTime | Job creation timestamp |
| startedDate | DateTime | Processing start timestamp |
| completedDate | DateTime | Completion timestamp |
| originalFileName | string | Uploaded Excel filename |
| fileSize | int | File size in bytes |
| resultFileUrl | string | Download URL for results CSV |
| totalCodesDistributed | int | Total codes assigned |
| totalSmsSent | int | Total SMS messages sent |

---

## Error Responses

All endpoints return consistent error responses:

**400 Bad Request:**
```json
{
  "success": false,
  "message": "Validation error: Purchase ID is required"
}
```

**401 Unauthorized:**
```json
{
  "success": false,
  "message": "Unauthorized: Invalid or expired token"
}
```

**403 Forbidden:**
```json
{
  "success": false,
  "message": "Forbidden: Admin role required"
}
```

**404 Not Found:**
```json
{
  "success": false,
  "message": "Purchase not found with ID: 450"
}
```

**500 Internal Server Error:**
```json
{
  "success": false,
  "message": "An error occurred while processing your request"
}
```

---

## Common Enumerations

### Purchase Status
- `Pending` - Awaiting approval
- `Active` - Approved and active
- `Cancelled` - Cancelled by admin
- `Refunded` - Refunded to sponsor

### Payment Status
- `Pending` - Payment not received
- `Completed` - Payment successful
- `Failed` - Payment failed

### Analysis Status
- `pending` - Analysis in queue
- `processing` - Currently being analyzed
- `completed` - Analysis finished successfully
- `failed` - Analysis failed

### Bulk Job Status
- `Pending` - Job created, not started
- `Processing` - Job in progress
- `Completed` - All distributions successful
- `PartialSuccess` - Some distributions failed
- `Failed` - Job failed completely

### Delivery Methods
- `SMS` - Turkish SMS service
- `WhatsApp` - WhatsApp Business API
- `Email` - Email delivery

### Subscription Tiers
- `1` - Trial
- `2` - S (Small)
- `3` - M (Medium)
- `4` - L (Large)
- `5` - XL (Extra Large)

---

## Authentication

All endpoints require JWT Bearer token authentication with Admin role.

**Request Header:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
x-dev-arch-version: 1
```

**Token Payload Must Include:**
```json
{
  "userId": 5,
  "email": "admin@ziraai.com",
  "roles": ["Admin"],
  "exp": 1699632000
}
```

---

## Rate Limiting

- **Analytics Endpoints:** 60 requests/minute
- **Query Endpoints:** 120 requests/minute
- **Command Endpoints:** 30 requests/minute
- **Bulk Operations:** 10 requests/minute

---

## Pagination

All paginated endpoints follow this pattern:

**Request:**
```
?page=1&pageSize=50
```

**Response:**
```json
{
  "data": {
    "items": [...],
    "page": 1,
    "pageSize": 50,
    "totalRecords": 245,
    "totalPages": 5
  }
}
```

---

## Date/Time Format

All dates use ISO 8601 format:
- **DateTime:** `2025-11-10T14:30:00Z` (UTC)
- **Date Only:** `2025-11-10`

Turkish local time (UTC+3) should be converted to UTC before sending.

---

## Notes

1. **Phone Format:** All phone numbers must be in Turkish local format `05XXXXXXXXX` (11 digits)
2. **Admin Logging:** All admin operations are logged to `AdminOperationLog` table
3. **Async Operations:** Bulk operations are processed asynchronously via RabbitMQ worker
4. **File Storage:** Result files are stored for 30 days before automatic deletion
5. **Cache:** Statistics endpoints have 15-minute cache TTL

---

**Last Updated:** 2025-11-10
**API Version:** 1.0
**Document Version:** 1.0
