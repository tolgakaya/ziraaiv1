# Dealer Code Distribution System - API Documentation

**Version**: 1.0  
**Last Updated**: 2025-10-26  
**Base URL**: `https://ziraai-api-sit.up.railway.app/api/v{version}/sponsorship`

## Table of Contents
1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Onboarding Methods](#onboarding-methods)
4. [API Endpoints](#api-endpoints)
   - [Transfer Codes](#1-transfer-codes-to-dealer)
   - [Create Dealer Invitation](#2-create-dealer-invitation)
   - [Reclaim Dealer Codes](#3-reclaim-dealer-codes)
   - [Get Dealer Performance](#4-get-dealer-performance)
   - [Get Dealer Summary](#5-get-dealer-summary)
   - [Get Dealer Invitations](#6-get-dealer-invitations)
   - [Search Dealer by Email](#7-search-dealer-by-email)
5. [Error Codes](#error-codes)
6. [Testing Guide](#testing-guide)

---

## Overview

The Dealer Code Distribution System enables sponsors to distribute sponsorship codes through a dealer network. Main sponsors (package purchasers) can onboard dealers using three methods and transfer codes to them for redistribution to farmers.

### Key Features
- **Three Onboarding Methods**: Manual transfer, invitation link, or automatic account creation
- **Code Transfer Management**: Transfer, reclaim, and track code distribution
- **Performance Analytics**: Monitor dealer usage, farmer engagement, and messaging activity
- **Backward Compatible**: Existing sponsor functionality remains unchanged

### Business Logic
- Main sponsors purchase packages and receive codes
- Codes can be distributed to dealers (sub-sponsors) 
- Dealers distribute codes to farmers using their own sponsorship profile
- All analyses remain linked to original sponsor for reporting
- Main sponsor maintains read-only visibility into dealer-distributed analyses

---

## Authentication

All endpoints require JWT Bearer authentication with **Sponsor** or **Admin** role.

```http
Authorization: Bearer {JWT_TOKEN}
x-dev-arch-version: 1.0
```

### Required Roles
- **Sponsor**: Can manage their own dealer network
- **Admin**: Full access to all dealer operations

---

## Onboarding Methods

### Method A: Manual Transfer (Existing Sponsor)
Search for existing sponsor by email → Transfer codes directly

**Use Case**: Sponsor already has an account, just needs codes

**Steps**:
1. Search dealer by email: `GET /dealer/search?email={email}`
2. Transfer codes: `POST /dealer/transfer-codes`

### Method B: Invitation Link (New or Existing)
Send invitation link → Dealer accepts → Transfer codes

**Use Case**: Sponsor may or may not have account, invitation provides formal process

**Steps**:
1. Create invitation: `POST /dealer/invite` (type: "Invite")
2. Dealer receives link and accepts invitation
3. Codes transferred upon acceptance

### Method C: AutoCreate (Quick Setup)
Create dealer account with auto-generated password → Transfer codes immediately

**Use Case**: Quick dealer onboarding without waiting for acceptance

**Steps**:
1. Create dealer with AutoCreate: `POST /dealer/invite` (type: "AutoCreate")
2. System creates account with random password
3. Codes transferred immediately
4. Return password to main sponsor for sharing

---

## API Endpoints

### 1. Transfer Codes to Dealer

Transfer available sponsorship codes from main sponsor to dealer.

**Endpoint**: `POST /api/v1/sponsorship/dealer/transfer-codes`

**Authorization**: Sponsor, Admin

**Request Body**:
```json
{
  "dealerId": 165,
  "purchaseId": 26,
  "codeCount": 10,
  "notes": "Transfer 10 codes for Q4 campaign"
}
```

**Response** (200 OK):
```json
{
  "data": {
    "dealerId": 165,
    "dealerEmail": "dealer@example.com",
    "dealerName": "Dealer Company Ltd",
    "transferredCount": 10,
    "transferredCodes": [
      {
        "codeId": 981,
        "code": "ZAI-ABC123",
        "transferredAt": "2025-10-26T10:30:00"
      }
    ],
    "remainingAvailableCodes": 40,
    "transferDate": "2025-10-26T10:30:00",
    "transferredBy": "Main Sponsor Inc"
  },
  "success": true,
  "message": "Successfully transferred 10 codes to dealer@example.com"
}
```

**Error Responses**:
- `400 Bad Request`: Not enough available codes
  ```json
  {
    "success": false,
    "message": "Not enough available codes. Requested: 10, Available: 5"
  }
  ```
- `404 Not Found`: Purchase not found or dealer not found
- `403 Forbidden`: Not authorized to transfer from this purchase

**Business Rules**:
- Only unused, active, non-expired codes can be transferred
- Codes must belong to requesting sponsor's purchase
- Dealer must exist and have Sponsor role
- Transfer is logged with timestamp and transferring user

---

### 2. Create Dealer Invitation

Create dealer invitation (Invite or AutoCreate types).

**Endpoint**: `POST /api/v1/sponsorship/dealer/invite`

**Authorization**: Sponsor, Admin

**Request Body** (Invite Type):
```json
{
  "invitationType": "Invite",
  "email": "newdealer@example.com",
  "phone": "+90555123456",
  "dealerName": "New Dealer Company",
  "purchaseId": 26,
  "codeCount": 15
}
```

**Request Body** (AutoCreate Type):
```json
{
  "invitationType": "AutoCreate",
  "email": "quickdealer@example.com",
  "dealerName": "Quick Dealer LLC",
  "purchaseId": 26,
  "codeCount": 20
}
```

**Response - Invite Type** (200 OK):
```json
{
  "data": {
    "invitationId": 5,
    "invitationToken": "abc123def456",
    "invitationLink": "https://ziraai.com/dealer-invitation?token=abc123def456",
    "email": "newdealer@example.com",
    "phone": "+90555123456",
    "dealerName": "New Dealer Company",
    "codeCount": 15,
    "status": "Pending",
    "invitationType": "Invite",
    "autoCreatedPassword": null,
    "createdDealerId": null,
    "createdAt": "2025-10-26T10:30:00"
  },
  "success": true,
  "message": "Invitation sent to newdealer@example.com"
}
```

**Response - AutoCreate Type** (200 OK):
```json
{
  "data": {
    "invitationId": 6,
    "invitationToken": "xyz789uvw456",
    "invitationLink": null,
    "email": "quickdealer@example.com",
    "dealerName": "Quick Dealer LLC",
    "codeCount": 20,
    "status": "Accepted",
    "invitationType": "AutoCreate",
    "autoCreatedPassword": "AbCdEf123456",
    "createdDealerId": 170,
    "createdAt": "2025-10-26T10:30:00"
  },
  "success": true,
  "message": "Dealer account created successfully. Login: quickdealer@example.com, Password: AbCdEf123456"
}
```

**Error Responses**:
- `400 Bad Request`: Email required for Invite type
- `400 Bad Request`: Not enough available codes
- `404 Not Found`: Purchase not found

**Business Rules**:
- **Invite Type**: Creates invitation with 7-day expiry, sends link to dealer
- **AutoCreate Type**: Creates dealer account immediately with random password, transfers codes
- AutoCreate assigns Sponsor role to new dealer
- Invitation token is unique and single-use
- Codes are transferred only when invitation is accepted (Invite) or immediately (AutoCreate)

---

### 3. Reclaim Dealer Codes

Reclaim unused codes from a dealer back to main sponsor inventory.

**Endpoint**: `POST /api/v1/sponsorship/dealer/reclaim-codes`

**Authorization**: Sponsor, Admin

**Request Body**:
```json
{
  "dealerId": 165,
  "codeCount": 5,
  "reason": "End of campaign - unused codes"
}
```

**Response** (200 OK):
```json
{
  "data": {
    "dealerId": 165,
    "dealerEmail": "dealer@example.com",
    "reclaimedCount": 5,
    "reclaimedCodes": [
      {
        "codeId": 985,
        "code": "ZAI-XYZ789",
        "reclaimedAt": "2025-10-26T11:00:00"
      }
    ],
    "reclaimDate": "2025-10-26T11:00:00",
    "reason": "End of campaign - unused codes"
  },
  "success": true,
  "message": "Successfully reclaimed 5 unused codes from dealer@example.com"
}
```

**Error Responses**:
- `400 Bad Request`: Not enough unused codes available for reclaim
  ```json
  {
    "success": false,
    "message": "Not enough unused codes to reclaim. Requested: 5, Available: 2"
  }
  ```
- `404 Not Found`: Dealer not found

**Business Rules**:
- Only unused, non-expired codes can be reclaimed
- Codes must currently belong to dealer
- Reclaim clears DealerId and transfer metadata
- Reclaim reason is logged for audit trail

---

### 4. Get Dealer Performance

Retrieve detailed performance analytics for a specific dealer.

**Endpoint**: `GET /api/v1/sponsorship/dealer/analytics/{dealerId}`

**Authorization**: Sponsor, Admin

**Query Parameters**: None

**Response** (200 OK):
```json
{
  "data": {
    "dealerId": 165,
    "dealerEmail": "dealer@example.com",
    "dealerName": "User 1113",
    "totalCodesReceived": 50,
    "codesUsed": 32,
    "codesAvailable": 18,
    "usageRate": 64.0,
    "totalFarmersSponsored": 28,
    "averageAnalysesPerFarmer": 2.5,
    "totalAnalyses": 70,
    "messagingStatistics": {
      "totalConversations": 25,
      "activeConversations": 15,
      "responseRate": 88.0,
      "averageResponseTime": "4.2 hours"
    },
    "performanceMetrics": {
      "topCropTypes": [
        {"cropType": "Tomato", "count": 25},
        {"cropType": "Pepper", "count": 18}
      ],
      "averageHealthScore": 78.5,
      "analysisDistribution": {
        "thisWeek": 12,
        "thisMonth": 45,
        "total": 70
      }
    }
  },
  "success": true,
  "message": "Dealer performance retrieved successfully"
}
```

**Error Responses**:
- `404 Not Found`: Dealer not found
- `403 Forbidden`: Not authorized to view this dealer's performance

**Business Rules**:
- Shows only data for analyses distributed by this dealer
- Messaging statistics calculated from dealer's conversations with farmers
- Performance metrics help main sponsor evaluate dealer effectiveness

---

### 5. Get Dealer Summary

Retrieve summary list of all dealers for the requesting sponsor.

**Endpoint**: `GET /api/v1/sponsorship/dealer/summary`

**Authorization**: Sponsor, Admin

**Query Parameters**: None

**Response** (200 OK):
```json
{
  "data": {
    "totalDealers": 3,
    "activeDealers": 2,
    "totalCodesDistributed": 150,
    "totalCodesUsed": 98,
    "totalAnalyses": 210,
    "dealers": [
      {
        "dealerId": 165,
        "dealerEmail": "dealer1@example.com",
        "dealerName": "Dealer One",
        "codesReceived": 50,
        "codesUsed": 32,
        "codesAvailable": 18,
        "usageRate": 64.0,
        "farmersSponsored": 28,
        "totalAnalyses": 70,
        "lastActivity": "2025-10-25T14:30:00"
      },
      {
        "dealerId": 168,
        "dealerEmail": "dealer2@example.com",
        "dealerName": "Dealer Two",
        "codesReceived": 100,
        "codesUsed": 66,
        "codesAvailable": 34,
        "usageRate": 66.0,
        "farmersSponsored": 52,
        "totalAnalyses": 140,
        "lastActivity": "2025-10-26T09:15:00"
      }
    ]
  },
  "success": true,
  "message": "Dealer summary retrieved successfully"
}
```

**Business Rules**:
- Returns all dealers who received codes from requesting sponsor
- Active dealers = dealers with at least one code transfer in last 90 days
- Sorted by last activity (most recent first)

---

### 6. Get Dealer Invitations

Retrieve list of dealer invitations with optional status filter.

**Endpoint**: `GET /api/v1/sponsorship/dealer/invitations?status={status}`

**Authorization**: Sponsor, Admin

**Query Parameters**:
- `status` (optional): Filter by invitation status
  - `Pending`: Awaiting dealer acceptance
  - `Accepted`: Dealer accepted, codes transferred
  - `Expired`: Invitation expired (>7 days)
  - `Cancelled`: Invitation cancelled by sponsor

**Response** (200 OK):
```json
{
  "data": [
    {
      "invitationId": 5,
      "email": "newdealer@example.com",
      "phone": "+90555123456",
      "dealerName": "New Dealer Company",
      "status": "Pending",
      "invitationType": "Invite",
      "codeCount": 15,
      "createdDate": "2025-10-20T10:30:00",
      "expiryDate": "2025-10-27T10:30:00",
      "acceptedDate": null,
      "invitationLink": "https://ziraai.com/dealer-invitation?token=abc123"
    },
    {
      "invitationId": 6,
      "email": "quickdealer@example.com",
      "dealerName": "Quick Dealer LLC",
      "status": "Accepted",
      "invitationType": "AutoCreate",
      "codeCount": 20,
      "createdDate": "2025-10-26T10:30:00",
      "expiryDate": "2025-11-02T10:30:00",
      "acceptedDate": "2025-10-26T10:30:00",
      "createdDealerId": 170
    }
  ],
  "success": true,
  "message": "Retrieved 2 dealer invitations"
}
```

**Business Rules**:
- Returns all invitations created by requesting sponsor
- AutoCreate invitations show `createdDealerId` instead of invitation link
- Expired invitations can be re-sent by creating new invitation

---

### 7. Search Dealer by Email

Search for existing sponsor/dealer by email address (Method A support).

**Endpoint**: `GET /api/v1/sponsorship/dealer/search?email={email}`

**Authorization**: Sponsor, Admin

**Query Parameters**:
- `email` (required): Email address to search

**Response** (200 OK - Found Sponsor):
```json
{
  "data": {
    "userId": 165,
    "email": "dealer@example.com",
    "firstName": "User",
    "lastName": "1113",
    "companyName": "",
    "isSponsor": true
  },
  "success": true,
  "message": "Dealer found successfully."
}
```

**Response** (200 OK - Found Non-Sponsor):
```json
{
  "data": {
    "userId": 168,
    "email": "farmer@example.com",
    "firstName": "John",
    "lastName": "Smith",
    "companyName": "",
    "isSponsor": false
  },
  "success": true,
  "message": "User found but does not have Sponsor role. You can still transfer codes, but they will need Sponsor role to distribute them."
}
```

**Error Responses**:
- `400 Bad Request`: Email parameter is required
- `404 Not Found`: No user found with this email address
  ```json
  {
    "success": false,
    "message": "No user found with this email address."
  }
  ```

**Business Rules**:
- Searches entire User table by email
- Returns user info with Sponsor role status
- Main sponsor can transfer codes to non-sponsors (they get codes but can't redistribute until role assigned)

---

## Error Codes

| HTTP Code | Description | Common Causes |
|-----------|-------------|---------------|
| 200 | Success | Request completed successfully |
| 400 | Bad Request | Invalid input, not enough codes, missing required fields |
| 401 | Unauthorized | Missing or invalid JWT token |
| 403 | Forbidden | Valid token but insufficient permissions |
| 404 | Not Found | Resource (dealer, purchase, invitation) not found |
| 500 | Internal Server Error | Unexpected server error, database issues |

### Common Error Response Format
```json
{
  "success": false,
  "message": "Detailed error description"
}
```

---

## Testing Guide

### Prerequisites
1. Valid JWT token with Sponsor or Admin role
2. Active sponsor profile
3. Purchase with available codes

### Test Scenario 1: Manual Transfer (Method A)

**Step 1**: Search for dealer
```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/search?email=dealer@example.com" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

**Step 2**: Transfer codes
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/transfer-codes" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "x-dev-arch-version: 1.0" \
  -H "Content-Type: application/json" \
  -d '{
    "dealerId": 165,
    "purchaseId": 26,
    "codeCount": 10
  }'
```

### Test Scenario 2: Invitation Link (Method B)

**Step 1**: Create invitation
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invite" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "x-dev-arch-version: 1.0" \
  -H "Content-Type: application/json" \
  -d '{
    "invitationType": "Invite",
    "email": "newdealer@example.com",
    "phone": "+90555123456",
    "dealerName": "New Dealer Company",
    "purchaseId": 26,
    "codeCount": 15
  }'
```

**Step 2**: Check invitation status
```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invitations?status=Pending" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

### Test Scenario 3: AutoCreate (Method C)

**Step 1**: Create dealer with AutoCreate
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invite" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "x-dev-arch-version: 1.0" \
  -H "Content-Type: application/json" \
  -d '{
    "invitationType": "AutoCreate",
    "email": "quickdealer@example.com",
    "dealerName": "Quick Dealer LLC",
    "purchaseId": 26,
    "codeCount": 20
  }'
```

**Step 2**: Verify dealer performance
```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/analytics/170" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

### Test Scenario 4: Reclaim Codes

```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/reclaim-codes" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "x-dev-arch-version: 1.0" \
  -H "Content-Type: application/json" \
  -d '{
    "dealerId": 165,
    "codeCount": 5,
    "reason": "End of campaign"
  }'
```

### Test Scenario 5: Dealer Summary

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/summary" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

---

## Postman Collection

A complete Postman collection with all endpoints and test scenarios is available at:
`/ZiraAI_Dealer_Distribution_API_v1.0.postman_collection.json`

### Collection Features
- Pre-configured environment variables
- Automated token refresh
- Request examples for all endpoints
- Test scripts for response validation
- Error scenario examples

---

## Additional Resources

- **Database Migrations**: See `claudedocs/Dealers/migrations/` for SQL scripts
- **Development Tracker**: `claudedocs/Dealers/DEVELOPMENT_TRACKER.md`
- **Main API Documentation**: Root Postman collection for complete ZiraAI API

---

**Document Version**: 1.0  
**API Version**: 1.0  
**Last Review**: 2025-10-26
