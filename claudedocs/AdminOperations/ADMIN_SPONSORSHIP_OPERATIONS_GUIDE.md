# Admin Sponsorship Operations - Complete Guide

**Created:** 2025-11-08
**Version:** 1.0
**Controller:** `AdminSponsorshipController`
**Base Route:** `/api/admin/sponsorship`
**Authorization:** Admin role required (Administrators GroupId = 1)

---

## Table of Contents

1. [Overview](#overview)
2. [Purchase Management Operations](#purchase-management-operations)
3. [Code Management Operations](#code-management-operations)
4. [User Management Operations](#user-management-operations)
5. [On-Behalf-Of (OBO) Operations](#on-behalf-of-obo-operations)
6. [Quick Reference Summary](#quick-reference-summary)

---

## Overview

This guide documents all admin operations available for managing sponsorships, purchases, codes, and sponsor users. Admin users can perform critical operations including creating purchases on behalf of sponsors, bulk code distribution, and comprehensive reporting.

### Key Capabilities

✅ **Purchase Management** - Create, approve, refund, and monitor sponsorship purchases
✅ **Code Management** - View, deactivate, and bulk-send sponsorship codes
✅ **User Management** - Search and manage sponsor users
✅ **On-Behalf-Of Operations** - Create purchases for sponsors (manual/offline payments)
✅ **Bulk Operations** - Send codes to multiple farmers simultaneously
✅ **Reporting & Analytics** - Detailed sponsor reports and statistics

### Authentication

All endpoints require:
```http
Authorization: Bearer {admin_jwt_token}
x-dev-arch-version: 1.0
```

Admin user must be in **Administrators group** (GroupId = 1) with appropriate operation claims.

---

## Purchase Management Operations

### 1. Get All Purchases

**Purpose:** Retrieve paginated list of all sponsorship purchases with filtering

**Endpoint:** `GET /api/admin/sponsorship/purchases`

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | int | No | 1 | Page number |
| `pageSize` | int | No | 50 | Items per page |
| `status` | string | No | null | Filter by status (Active, Pending, Cancelled) |
| `paymentStatus` | string | No | null | Filter by payment status (Completed, Pending, Refunded) |
| `sponsorId` | int? | No | null | Filter by sponsor user ID |

**Request Example:**
```http
GET /api/admin/sponsorship/purchases?page=1&pageSize=10&paymentStatus=Completed
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

**Response Example:**
```json
{
  "success": true,
  "message": "Purchases retrieved successfully",
  "data": [
    {
      "id": 26,
      "sponsorId": 159,
      "subscriptionTierId": 3,
      "quantity": 50,
      "unitPrice": 599.99,
      "totalAmount": 29999.50,
      "currency": "TRY",
      "purchaseDate": "2025-10-12T17:40:11.717861",
      "paymentMethod": "CreditCard",
      "paymentReference": "MOCK-1760290809062",
      "paymentStatus": "Completed",
      "paymentCompletedDate": "2025-10-12T17:40:11.745821",
      "invoiceAddress": "Istanbul, Turkey",
      "taxNumber": "1111111111",
      "companyName": "Acme Corp",
      "codesGenerated": 50,
      "codesUsed": 8,
      "codePrefix": "AGRI",
      "validityDays": 30,
      "status": "Active",
      "createdDate": "2025-10-12T17:40:11.717862",
      "updatedDate": "2025-10-15T18:04:06.559065"
    }
  ]
}
```

**Use Cases:**
- Monitor all sponsorship purchases
- Track payment statuses
- Audit purchase history
- Generate financial reports
- Identify pending approvals

---

### 2. Get Purchase By ID

**Purpose:** Retrieve detailed information for a specific purchase

**Endpoint:** `GET /api/admin/sponsorship/purchases/{purchaseId}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `purchaseId` | int | Yes | Purchase ID to retrieve |

**Request Example:**
```http
GET /api/admin/sponsorship/purchases/26
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

**Response Example:**
```json
{
  "success": true,
  "message": "Purchase retrieved successfully",
  "data": {
    "id": 26,
    "sponsorId": 159,
    "subscriptionTierId": 3,
    "quantity": 50,
    "unitPrice": 599.99,
    "totalAmount": 29999.50,
    "currency": "TRY",
    "purchaseDate": "2025-10-12T17:40:11.717861",
    "paymentMethod": "CreditCard",
    "paymentReference": "MOCK-1760290809062",
    "paymentStatus": "Completed",
    "codesGenerated": 50,
    "codesUsed": 8,
    "status": "Active"
  }
}
```

**Use Cases:**
- View purchase details
- Verify payment information
- Check code generation status
- Investigate purchase issues
- Customer support inquiries

---

### 3. Approve Purchase

**Purpose:** Approve a pending purchase and generate codes

**Endpoint:** `POST /api/admin/sponsorship/purchases/{purchaseId}/approve`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `purchaseId` | int | Yes | Purchase ID to approve |

**Request Body:**
```json
{
  "notes": "Payment verified via bank transfer - Reference: TR1234567890"
}
```

**Request Example:**
```http
POST /api/admin/sponsorship/purchases/26/approve
Authorization: Bearer {token}
x-dev-arch-version: 1.0
Content-Type: application/json

{
  "notes": "Payment verified via bank transfer"
}
```

**Response Example:**
```json
{
  "success": true,
  "message": "Purchase approved and codes generated successfully"
}
```

**Process Flow:**
1. Validates purchase exists and is in Pending status
2. Updates payment status to Completed
3. Generates sponsorship codes based on quantity
4. Updates purchase status to Active
5. Creates audit log entry with admin user ID
6. Returns success message

**Use Cases:**
- Manual payment verification
- Bank transfer confirmations
- Offline payment processing
- Corporate purchase approvals
- Payment gateway issue resolution

---

### 4. Refund Purchase

**Purpose:** Process refund and deactivate unused codes

**Endpoint:** `POST /api/admin/sponsorship/purchases/{purchaseId}/refund`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `purchaseId` | int | Yes | Purchase ID to refund |

**Request Body:**
```json
{
  "refundReason": "Customer requested refund - duplicate payment error"
}
```

**Request Example:**
```http
POST /api/admin/sponsorship/purchases/26/refund
Authorization: Bearer {token}
x-dev-arch-version: 1.0
Content-Type: application/json

{
  "refundReason": "Duplicate payment - customer error"
}
```

**Response Example:**
```json
{
  "success": true,
  "message": "Purchase refunded successfully. 10 unused codes deactivated."
}
```

**Process Flow:**
1. Validates purchase exists
2. Updates payment status to Refunded
3. Updates purchase status to Cancelled
4. Deactivates all unused codes from the purchase
5. Creates audit log entry with refund reason
6. Returns count of deactivated codes

**Use Cases:**
- Customer refund requests
- Payment disputes
- Duplicate payments
- Service cancellations
- Compensation processing

---

### 5. Get Sponsorship Statistics

**Purpose:** Retrieve overall sponsorship metrics and statistics

**Endpoint:** `GET /api/admin/sponsorship/statistics`

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `startDate` | DateTime? | No | null | Start date for filtering |
| `endDate` | DateTime? | No | null | End date for filtering |

**Request Example:**
```http
GET /api/admin/sponsorship/statistics?startDate=2025-01-01&endDate=2025-12-31
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

**Response Example:**
```json
{
  "success": true,
  "message": "Statistics retrieved successfully",
  "data": {
    "totalPurchases": 150,
    "totalRevenue": 450000.00,
    "totalCodesGenerated": 7500,
    "totalCodesUsed": 5200,
    "totalCodesActive": 2300,
    "averageCodeUsageRate": 69.33,
    "topSponsors": [
      {
        "sponsorId": 159,
        "sponsorName": "Acme Corp",
        "totalPurchases": 15,
        "totalRevenue": 45000.00
      }
    ]
  }
}
```

**Use Cases:**
- Executive dashboards
- Performance tracking
- Revenue reporting
- Business intelligence
- Trend analysis

---

## Code Management Operations

### 6. Get All Codes

**Purpose:** Retrieve paginated list of all sponsorship codes with filtering

**Endpoint:** `GET /api/admin/sponsorship/codes`

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | int | No | 1 | Page number |
| `pageSize` | int | No | 50 | Items per page |
| `isUsed` | bool? | No | null | Filter by usage status |
| `isActive` | bool? | No | null | Filter by active status |
| `sponsorId` | int? | No | null | Filter by sponsor user ID |
| `purchaseId` | int? | No | null | Filter by purchase ID |

**Request Example:**
```http
GET /api/admin/sponsorship/codes?page=1&pageSize=3&isUsed=true
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

**Response Example:**
```json
{
  "success": true,
  "message": "Codes retrieved successfully",
  "data": [
    {
      "id": 981,
      "code": "AGRI-2025-52834B45",
      "sponsorId": 159,
      "subscriptionTierId": 3,
      "sponsorshipPurchaseId": 26,
      "isUsed": true,
      "usedByUserId": 160,
      "usedDate": "2025-10-14T11:26:56.123637",
      "createdSubscriptionId": 146,
      "createdDate": "2025-10-12T17:40:11.764955",
      "expiryDate": "2025-11-11T17:40:11.764955",
      "isActive": true,
      "linkClickCount": 0,
      "linkDelivered": false
    }
  ]
}
```

**Use Cases:**
- Monitor code usage
- Track code distribution
- Identify unused codes
- Audit code lifecycle
- Generate code reports

---

### 7. Get Code By ID

**Purpose:** Retrieve detailed information for a specific code

**Endpoint:** `GET /api/admin/sponsorship/codes/{codeId}`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `codeId` | int | Yes | Code ID to retrieve |

**Request Example:**
```http
GET /api/admin/sponsorship/codes/981
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

**Response Example:**
```json
{
  "success": true,
  "message": "Code retrieved successfully",
  "data": {
    "id": 981,
    "code": "AGRI-2025-52834B45",
    "sponsorId": 159,
    "subscriptionTierId": 3,
    "sponsorshipPurchaseId": 26,
    "isUsed": true,
    "usedByUserId": 160,
    "usedDate": "2025-10-14T11:26:56.123637",
    "createdSubscriptionId": 146,
    "expiryDate": "2025-11-11T17:40:11.764955",
    "isActive": true
  }
}
```

**Use Cases:**
- Code verification
- Usage tracking
- Troubleshooting issues
- Customer support
- Fraud investigation

---

### 8. Deactivate Code

**Purpose:** Deactivate a sponsorship code (prevent future use)

**Endpoint:** `POST /api/admin/sponsorship/codes/{codeId}/deactivate`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `codeId` | int | Yes | Code ID to deactivate |

**Request Body:**
```json
{
  "reason": "Code reported as compromised - security issue"
}
```

**Request Example:**
```http
POST /api/admin/sponsorship/codes/981/deactivate
Authorization: Bearer {token}
x-dev-arch-version: 1.0
Content-Type: application/json

{
  "reason": "Code reported as compromised"
}
```

**Response Example:**
```json
{
  "success": true,
  "message": "Code deactivated successfully"
}
```

**Use Cases:**
- Security incidents
- Code compromise
- Fraud prevention
- Policy violations
- Manual code management

---

### 9. Bulk Send Codes

**Purpose:** Send sponsorship codes to multiple farmers on behalf of sponsor

**Endpoint:** `POST /api/admin/sponsorship/codes/bulk-send`

**Request Body:**
```json
{
  "sponsorId": 159,
  "purchaseId": 26,
  "recipients": [
    {
      "phoneNumber": "+905551234567",
      "name": "Mehmet Yılmaz"
    },
    {
      "phoneNumber": "+905559876543",
      "name": "Ayşe Demir"
    }
  ],
  "sendVia": "SMS"
}
```

**Field Descriptions:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sponsorId` | int | Yes | Sponsor user ID |
| `purchaseId` | int | Yes | Purchase ID to get codes from |
| `recipients` | array | Yes | List of recipients with phone/name |
| `sendVia` | string | No | Send method: SMS, WhatsApp, Email (default: SMS) |

**Request Example:**
```http
POST /api/admin/sponsorship/codes/bulk-send
Authorization: Bearer {token}
x-dev-arch-version: 1.0
Content-Type: application/json

{
  "sponsorId": 159,
  "purchaseId": 26,
  "recipients": [
    {
      "phoneNumber": "+905551234567",
      "name": "Mehmet Yılmaz"
    },
    {
      "phoneNumber": "+905559876543",
      "name": "Ayşe Demir"
    }
  ],
  "sendVia": "SMS"
}
```

**Response Example:**
```json
{
  "success": true,
  "message": "2 codes sent successfully via SMS. 0 failed."
}
```

**Process Flow:**
1. Validates sponsor and purchase exist
2. Retrieves unused codes from purchase
3. Assigns one code per recipient
4. Sends codes via specified method (SMS/WhatsApp/Email)
5. Marks codes as distributed
6. Creates AdminOperationLog with isOnBehalfOf=true
7. Returns success/failure counts

**Use Cases:**
- Mass code distribution
- Marketing campaigns
- Event sponsorships
- Partner distributions
- Dealer programs

---

### 10. Get Sponsor Detailed Report

**Purpose:** Get comprehensive report for a specific sponsor

**Endpoint:** `GET /api/admin/sponsorship/sponsors/{sponsorId}/detailed-report`

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `sponsorId` | int | Yes | Sponsor user ID |

**Request Example:**
```http
GET /api/admin/sponsorship/sponsors/159/detailed-report
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

**Response Example:**
```json
{
  "success": true,
  "message": "Sponsor report retrieved successfully",
  "data": {
    "sponsorId": 159,
    "sponsorName": "Acme Corp",
    "totalPurchases": 15,
    "totalRevenue": 45000.00,
    "totalCodesGenerated": 750,
    "totalCodesUsed": 520,
    "totalCodesActive": 230,
    "codeUsageRate": 69.33,
    "purchases": [
      {
        "purchaseId": 26,
        "purchaseDate": "2025-10-12T17:40:11",
        "quantity": 50,
        "totalAmount": 29999.50,
        "codesUsed": 8,
        "status": "Active"
      }
    ]
  }
}
```

**Use Cases:**
- Sponsor performance analysis
- Account review
- Business development
- Relationship management
- Retention strategies

---

## User Management Operations

### 11. Get All Sponsors

**Purpose:** Retrieve paginated list of all users with Sponsor role (GroupId = 3)

**Endpoint:** `GET /api/admin/sponsorship/sponsors`

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | int | No | 1 | Page number |
| `pageSize` | int | No | 50 | Items per page |
| `isActive` | bool? | No | null | Filter by active status |
| `status` | string | No | null | Filter by user status |
| `searchTerm` | string | No | null | Search by email, name, or mobile phone |

**Request Example:**
```http
GET /api/admin/sponsorship/sponsors?searchTerm=acme&isActive=true&pageSize=10
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

**Response Example:**
```json
{
  "success": true,
  "message": "Retrieved 5 sponsors successfully",
  "data": [
    {
      "userId": 159,
      "fullName": "John Sponsor",
      "email": "john@acmecorp.com",
      "mobilePhones": "+905551234567",
      "address": "Istanbul, Turkey",
      "notes": "Premium sponsor account",
      "gender": 1,
      "status": true,
      "isActive": true
    }
  ]
}
```

**Search Features:**
- 🔍 Multi-field search across email, full name, mobile phones
- 🔤 Case-insensitive partial matching
- 🔗 Combinable with isActive and status filters
- ⚡ Optimized database queries

**Use Cases:**
- Sponsor directory management
- Quick sponsor lookup
- Support ticket resolution
- Marketing campaigns
- Account management

---

## On-Behalf-Of (OBO) Operations

### 🌟 Create Purchase On Behalf Of Sponsor (OBO)

**Purpose:** Admin creates sponsorship purchase for a sponsor, bypassing online payment flow

**Endpoint:** `POST /api/admin/sponsorship/purchases/create-on-behalf-of`

**⭐ CRITICAL FEATURE:** This is the primary OBO operation that enables admins to create purchases for sponsors for manual/offline payments, corporate partnerships, and special arrangements.

**Request Body:**
```json
{
  "sponsorId": 159,
  "subscriptionTierId": 2,
  "quantity": 5,
  "unitPrice": 99.99,
  "autoApprove": true,
  "paymentMethod": "BankTransfer",
  "paymentReference": "TR1234567890-2025-001",
  "companyName": "Acme Corporation",
  "taxNumber": "1234567890",
  "invoiceAddress": "Levent, Istanbul, Turkey",
  "codePrefix": "ACME",
  "validityDays": 365,
  "notes": "Annual corporate partnership - 2025"
}
```

**Field Descriptions:**
| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `sponsorId` | int | ✅ Yes | - | Sponsor user ID |
| `subscriptionTierId` | int | ✅ Yes | - | Tier: 1=Trial, 2=S, 3=M, 4=L, 5=XL |
| `quantity` | int | ✅ Yes | - | Number of codes to generate |
| `unitPrice` | decimal | ✅ Yes | - | Price per unit |
| `autoApprove` | bool | No | false | Auto-approve without payment verification |
| `paymentMethod` | string | No | "Manual" | Payment method identifier |
| `paymentReference` | string | No | null | Transaction/reference ID for tracking |
| `companyName` | string | No | null | Invoice company name |
| `taxNumber` | string | No | null | Invoice tax number |
| `invoiceAddress` | string | No | null | Invoice address |
| `codePrefix` | string | No | null | Custom prefix for generated codes |
| `validityDays` | int | No | 365 | Validity period for codes (days) |
| `notes` | string | No | null | Additional notes |

**Request Example:**
```http
POST /api/admin/sponsorship/purchases/create-on-behalf-of
Authorization: Bearer {token}
x-dev-arch-version: 1.0
Content-Type: application/json

{
  "sponsorId": 159,
  "subscriptionTierId": 2,
  "quantity": 5,
  "unitPrice": 99.99,
  "autoApprove": true,
  "paymentMethod": "BankTransfer",
  "paymentReference": "TR1234567890",
  "companyName": "Acme Corp",
  "taxNumber": "1234567890",
  "invoiceAddress": "Istanbul, Turkey",
  "codePrefix": "ACME",
  "validityDays": 365,
  "notes": "Annual partnership 2025"
}
```

**Response Example:**
```json
{
  "success": true,
  "message": "Purchase created and auto-approved for User 159. Total: ₺499,95 TRY",
  "data": {
    "id": 27,
    "sponsorId": 159,
    "subscriptionTierId": 2,
    "quantity": 5,
    "unitPrice": 99.99,
    "totalAmount": 499.95,
    "currency": "TRY",
    "purchaseDate": "2025-11-08T19:01:47",
    "paymentMethod": "BankTransfer",
    "paymentReference": "TR1234567890",
    "paymentStatus": "Completed",
    "paymentCompletedDate": "2025-11-08T19:01:47",
    "companyName": "Acme Corp",
    "taxNumber": "1234567890",
    "invoiceAddress": "Istanbul, Turkey",
    "codesGenerated": 5,
    "codesUsed": 0,
    "codePrefix": "ACME",
    "validityDays": 365,
    "status": "Active",
    "notes": "[Created by Admin on behalf of sponsor]\nAnnual partnership 2025",
    "createdDate": "2025-11-08T19:01:47",
    "approvedByUserId": 155,
    "approvalDate": "2025-11-08T19:01:47"
  }
}
```

**Process Flow:**
1. **Validation**
   - Validates sponsor exists and has Sponsor role (GroupId = 3)
   - Validates subscription tier exists and is valid
   - Validates quantity and unit price are positive

2. **Purchase Creation**
   - Calculates total amount: `quantity × unitPrice`
   - Creates purchase record with admin metadata
   - Sets currency to "TRY"
   - Records admin user ID as creator

3. **Note Annotation**
   - Automatically prefixes notes with: `[Created by Admin on behalf of sponsor]`
   - Appends custom notes if provided

4. **Auto-Approval Flow** (if `autoApprove: true`)
   - Sets `paymentStatus` to "Completed"
   - Sets `approvedByUserId` to current admin user ID
   - Sets `approvalDate` to current timestamp
   - Sets `paymentCompletedDate` to current timestamp
   - Generates sponsorship codes immediately
   - Updates `codesGenerated` count

5. **Audit Logging**
   - Creates `AdminOperationLog` entry
   - Sets `isOnBehalfOf: true`
   - Records admin user ID, IP address, user agent
   - Logs operation type: "CreatePurchaseOBO"

6. **Response**
   - Returns created purchase with full details
   - Includes generated code count if auto-approved

**Use Cases:**

### 1. Manual/Offline Payments
**Scenario:** Sponsor pays via bank transfer

```json
{
  "sponsorId": 159,
  "subscriptionTierId": 3,
  "quantity": 50,
  "unitPrice": 599.99,
  "autoApprove": true,
  "paymentMethod": "BankTransfer",
  "paymentReference": "TR-2025-BANK-001234",
  "companyName": "Acme Corp",
  "taxNumber": "1234567890",
  "invoiceAddress": "Istanbul, Turkey",
  "notes": "Bank transfer verified - Reference: TR-2025-BANK-001234"
}
```

### 2. Corporate Partnerships
**Scenario:** Annual contract with fixed pricing

```json
{
  "sponsorId": 159,
  "subscriptionTierId": 5,
  "quantity": 1000,
  "unitPrice": 299.99,
  "autoApprove": true,
  "paymentMethod": "CorporateInvoice",
  "paymentReference": "CONTRACT-2025-ACME",
  "companyName": "Acme Corporation International",
  "taxNumber": "1234567890",
  "invoiceAddress": "Headquarters, Istanbul, Turkey",
  "codePrefix": "ACME2025",
  "validityDays": 365,
  "notes": "Annual corporate partnership - 2025 contract"
}
```

### 3. Special Pricing Arrangements
**Scenario:** Volume discount for preferred partner

```json
{
  "sponsorId": 159,
  "subscriptionTierId": 4,
  "quantity": 200,
  "unitPrice": 399.99,
  "autoApprove": true,
  "paymentMethod": "CorporateAgreement",
  "paymentReference": "PARTNER-2025-Q1",
  "companyName": "Premium Partner Ltd",
  "taxNumber": "9876543210",
  "invoiceAddress": "Ankara, Turkey",
  "codePrefix": "PARTNER",
  "validityDays": 180,
  "notes": "Q1 2025 partnership - 20% volume discount applied"
}
```

### 4. Test/Demo Accounts
**Scenario:** QA testing or demo environment

```json
{
  "sponsorId": 159,
  "subscriptionTierId": 2,
  "quantity": 10,
  "unitPrice": 0.01,
  "autoApprove": true,
  "paymentMethod": "Test",
  "paymentReference": "TEST-QA-2025-001",
  "companyName": "Test Account",
  "codePrefix": "TEST",
  "validityDays": 30,
  "notes": "QA Testing - Staging Environment"
}
```

### 5. Customer Service Recovery
**Scenario:** Compensation for service issues

```json
{
  "sponsorId": 159,
  "subscriptionTierId": 3,
  "quantity": 25,
  "unitPrice": 0.00,
  "autoApprove": true,
  "paymentMethod": "Compensation",
  "paymentReference": "COMP-TICKET-12345",
  "companyName": "Valued Customer",
  "codePrefix": "COMP",
  "validityDays": 90,
  "notes": "Compensation for service disruption - Ticket #12345"
}
```

**Important Notes:**

⚠️ **Auto-Approve Behavior:**
- When `autoApprove: true`, purchase is immediately completed and codes are generated
- When `autoApprove: false`, purchase remains in Pending status and requires manual approval via separate endpoint
- Use `autoApprove: true` for verified offline payments
- Use `autoApprove: false` when waiting for payment confirmation

⚠️ **Payment Method:**
- Freeform string field for tracking payment type
- Common values: "BankTransfer", "Manual", "Offline", "CorporateInvoice", "Cash", "Check"
- Stored for reporting and audit purposes

⚠️ **Audit Trail:**
- All OBO operations are logged with `isOnBehalfOf: true`
- Admin user ID is recorded as creator
- Notes are automatically prefixed to distinguish OBO purchases
- IP address and user agent are captured for security

⚠️ **Code Generation:**
- Only occurs when `autoApprove: true`
- Codes are generated with specified prefix (or default)
- Validity period starts from creation date
- All codes are initially marked as unused and active

---

## Quick Reference Summary

### Purchase Operations
1. ✅ **GET** `/api/admin/sponsorship/purchases` - List all purchases
2. ✅ **GET** `/api/admin/sponsorship/purchases/{purchaseId}` - Get purchase details
3. ✅ **POST** `/api/admin/sponsorship/purchases/{purchaseId}/approve` - Approve purchase
4. ✅ **POST** `/api/admin/sponsorship/purchases/{purchaseId}/refund` - Refund purchase
5. 🌟 **POST** `/api/admin/sponsorship/purchases/create-on-behalf-of` - Create purchase OBO
6. ✅ **GET** `/api/admin/sponsorship/statistics` - Get sponsorship statistics

### Code Operations
7. ✅ **GET** `/api/admin/sponsorship/codes` - List all codes
8. ✅ **GET** `/api/admin/sponsorship/codes/{codeId}` - Get code details
9. ✅ **POST** `/api/admin/sponsorship/codes/{codeId}/deactivate` - Deactivate code
10. 🌟 **POST** `/api/admin/sponsorship/codes/bulk-send` - Bulk send codes (OBO)
11. ✅ **GET** `/api/admin/sponsorship/sponsors/{sponsorId}/detailed-report` - Sponsor report

### User Operations
12. ✅ **GET** `/api/admin/sponsorship/sponsors` - List all sponsors (with search)

### On-Behalf-Of (OBO) Operations
- 🌟 **Create Purchase OBO** - Primary OBO operation for manual/offline payments
- 🌟 **Bulk Send Codes** - Send codes to farmers on behalf of sponsor

### Legend
- ✅ Standard Operation
- 🌟 On-Behalf-Of (OBO) Operation
- ⚠️ Important Note
- 🔍 Search Capability
- 📊 Analytics/Reporting

---

## Operation Categories Summary

### Purchase Management (6 Endpoints)
- List purchases with filtering
- Get purchase details by ID
- Approve pending purchases
- Process refunds
- **Create purchases on behalf of sponsors (OBO)**
- View sponsorship statistics

### Code Management (5 Endpoints)
- List codes with filtering
- Get code details by ID
- Deactivate individual codes
- **Bulk send codes to farmers (OBO)**
- Get detailed sponsor reports

### User Management (1 Endpoint)
- List and search sponsor users

### OBO Operations (2 Endpoints)
- **Create Purchase On Behalf Of Sponsor** - Manual/offline payment processing
- **Bulk Send Codes** - Mass code distribution on behalf of sponsor

---

**Document Version:** 1.0
**Last Updated:** 2025-11-08
**Total Endpoints:** 12
**OBO Endpoints:** 2
