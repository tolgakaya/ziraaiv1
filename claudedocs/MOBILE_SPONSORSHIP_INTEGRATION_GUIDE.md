# ZiraAI Sponsorship System - Mobile Integration Guide

**Version**: 1.0
**Date**: 2025-10-08
**For**: Mobile Development Team (iOS & Android/Flutter)

---

## 📋 Table of Contents

1. [System Overview](#system-overview)
2. [Authentication & Roles](#authentication--roles)
3. [Farmer Flow](#farmer-flow)
4. [Sponsor Flow](#sponsor-flow)
5. [Queue System](#queue-system)
6. [Tier-Based Features](#tier-based-features)
7. [Error Handling](#error-handling)
8. [Testing Checklist](#testing-checklist)

---

## 🎯 System Overview

### What is Sponsorship?

Sponsors (agricultural companies) can purchase subscription packages in bulk and distribute them to farmers for free via SMS/WhatsApp links or codes.

### Key Concepts

- **Sponsor**: Company/user with "Sponsor" role who buys packages
- **Farmer**: End user who redeems codes to get free subscriptions
- **Sponsorship Code**: Unique code (format: `AGRI-XXXXX`) for redemption
- **Queue System**: If farmer has active sponsorship, new codes queue and auto-activate on expiry
- **Tiers**: S, M, L, XL - higher tiers unlock more benefits

### Base URL

```
Development: https://localhost:5001
Staging:     https://ziraai-api-sit.up.railway.app
Production:  https://api.ziraai.com
```

All endpoints use: `{baseUrl}/api/v1/sponsorship/*`

---

## 🔐 Authentication & Roles

### Required Headers

```http
Authorization: Bearer {jwt_token}
Content-Type: application/json
x-dev-arch-version: 1.0
```

### Role Requirements

| Role | Endpoints Access |
|------|-----------------|
| **Farmer** | Redeem codes, view my-sponsor, validate codes |
| **Sponsor** | Purchase packages, send links, view statistics, messaging (tier-based), smart links (XL only) |
| **Admin** | All endpoints |

---

## 👨‍🌾 Farmer Flow

### 1. Check Current Subscription Status

**Purpose**: See active subscription + queued sponsorships

**Endpoint**: `GET /api/v1/subscriptions/my-subscription`

**Authorization**: `Farmer` role required

**Request**:
```http
GET {{baseUrl}}/api/v1/subscriptions/my-subscription
Authorization: Bearer {farmer_token}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": 120,
    "userId": 131,
    "subscriptionTierId": 2,
    "tierName": "M",
    "tierDisplayName": "Medium",
    "startDate": "2025-10-08T08:56:45",
    "endDate": "2025-11-07T08:56:45",
    "isActive": true,
    "status": "Active",
    "queueStatus": 1,
    "queuedDate": null,
    "previousSponsorshipId": null,
    "currentDailyUsage": 5,
    "dailyRequestLimit": 20,
    "currentMonthlyUsage": 15,
    "monthlyRequestLimit": 200,
    "isTrialSubscription": false,
    "queuedSubscriptions": [
      {
        "id": 121,
        "subscriptionTierId": 3,
        "tierName": "L",
        "tierDisplayName": "Large",
        "queueStatus": 0,
        "queuedDate": "2025-10-08T10:30:00",
        "previousSponsorshipId": 120,
        "status": "Pending",
        "isSponsoredSubscription": true
      }
    ]
  }
}
```

**UI Guidelines**:
- Show active subscription prominently
- Display queue count if `queuedSubscriptions.length > 0`
- Show "X sponsorships waiting to activate" badge
- Add auto-activation explanation tooltip

---

### 2. Validate Sponsorship Code (Before Redeeming)

**Purpose**: Check if code is valid without using it

**Endpoint**: `GET /api/v1/sponsorship/validate/{code}`

**Authorization**: `Farmer` role required

**Request**:
```http
GET {{baseUrl}}/api/v1/sponsorship/validate/AGRI-ABC123
Authorization: Bearer {farmer_token}
```

**Response - Valid Code** (200 OK):
```json
{
  "success": true,
  "message": "Code is valid",
  "data": {
    "code": "AGRI-ABC123",
    "subscriptionTier": "Premium",
    "expiryDate": "2026-01-08T00:00:00",
    "isValid": true
  }
}
```

**Response - Invalid Code** (200 OK):
```json
{
  "success": false,
  "message": "Kod bulunamadı veya geçersiz",
  "data": {
    "code": "AGRI-INVALID",
    "isValid": false
  }
}
```

**UI Guidelines**:
- Show real-time validation as user types code
- Display tier name and expiry date for valid codes
- Show clear error message for invalid codes

---

### 3. Redeem Sponsorship Code

**Purpose**: Use code to get free subscription (or queue if active exists)

**Endpoint**: `POST /api/v1/sponsorship/redeem`

**Authorization**: `Farmer` role required

**Request**:
```http
POST {{baseUrl}}/api/v1/sponsorship/redeem
Authorization: Bearer {farmer_token}
Content-Type: application/json

{
  "code": "AGRI-ABC123"
}
```

**Response - Immediate Activation** (Trial/No Active) (200 OK):
```json
{
  "success": true,
  "message": "Sponsorluk aktivasyonu tamamlandı!",
  "data": {
    "id": 125,
    "userId": 131,
    "subscriptionTierId": 2,
    "isActive": true,
    "isSponsoredSubscription": true,
    "queueStatus": 1,
    "startDate": "2025-10-08T11:00:00",
    "endDate": "2025-11-07T11:00:00",
    "status": "Active"
  }
}
```

**Response - Queued** (Active Sponsorship Exists) (200 OK):
```json
{
  "success": true,
  "message": "Sponsorluk kodunuz sıraya alındı. Mevcut sponsorluk bittiğinde otomatik aktif olacak.",
  "data": {
    "id": 126,
    "userId": 131,
    "subscriptionTierId": 3,
    "isActive": false,
    "isSponsoredSubscription": true,
    "queueStatus": 0,
    "previousSponsorshipId": 125,
    "queuedDate": "2025-10-08T11:15:00",
    "status": "Pending"
  }
}
```

**Response - Error** (400 Bad Request):
```json
{
  "success": false,
  "message": "Kod bulunamadı veya kullanılmış",
  "data": null
}
```

**UI Guidelines**:
- Show success screen with tier benefits
- For queued codes: explain auto-activation process
- Show estimated activation date (current endDate)
- Add "View my subscriptions" CTA
- For errors: clear message + retry option

---

## 🏢 Sponsor Flow

### 1. Create Sponsor Profile (One-Time Setup)

**Purpose**: Set up company profile for branding

**Endpoint**: `POST /api/v1/sponsorship/profile`

**Authorization**: `Sponsor` role required

**Request**:
```http
POST {{baseUrl}}/api/v1/sponsorship/profile
Authorization: Bearer {sponsor_token}
Content-Type: application/json

{
  "companyName": "AgriTech Solutions",
  "companyDescription": "Leading agricultural technology provider",
  "sponsorLogoUrl": "https://cdn.example.com/logo.png",
  "websiteUrl": "https://agritech.com",
  "contactEmail": "support@agritech.com",
  "contactPhone": "+905551234567",
  "contactPerson": "John Doe",
  "companyType": "Technology",
  "businessModel": "B2B"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "message": "Sponsor profili başarıyla oluşturuldu",
  "data": {
    "id": 10,
    "sponsorId": 50,
    "companyName": "AgriTech Solutions",
    "companyDescription": "Leading agricultural technology provider",
    "sponsorLogoUrl": "https://cdn.example.com/logo.png",
    "websiteUrl": "https://agritech.com",
    "contactEmail": "support@agritech.com",
    "contactPhone": "+905551234567",
    "contactPerson": "John Doe",
    "companyType": "Technology",
    "businessModel": "B2B",
    "createdDate": "2025-10-08T12:00:00"
  }
}
```

**UI Guidelines**:
- Required fields: companyName, contactEmail
- Optional: logo, website, description
- Show preview of farmer-facing branding
- Allow logo upload with image picker

---

### 2. Get Sponsor Profile

**Endpoint**: `GET /api/v1/sponsorship/profile`

**Authorization**: `Sponsor` role required

**Request**:
```http
GET {{baseUrl}}/api/v1/sponsorship/profile
Authorization: Bearer {sponsor_token}
```

**Response** (200 OK): Same as create response

**Response - No Profile** (400 Bad Request):
```json
{
  "success": false,
  "message": "Sponsor profili bulunamadı"
}
```

---

### 3. Purchase Sponsorship Package

**Purpose**: Buy subscription codes in bulk

**Endpoint**: `POST /api/v1/sponsorship/purchase-package`

**Authorization**: `Sponsor` role required

**Request**:
```http
POST {{baseUrl}}/api/v1/sponsorship/purchase-package
Authorization: Bearer {sponsor_token}
Content-Type: application/json

{
  "subscriptionTierId": 2,
  "quantity": 10,
  "totalAmount": 499.90,
  "paymentReference": "STRIPE-ch_abc123"
}
```

**Field Details**:
- `subscriptionTierId`: Tier ID (2=S, 3=M, 4=L, 5=XL)
- `quantity`: Number of codes to generate
- `totalAmount`: Total payment amount (decimal)
- `paymentReference`: Payment gateway transaction ID

**Response** (200 OK):
```json
{
  "success": true,
  "message": "10 sponsorship codes generated successfully",
  "data": {
    "id": 15,
    "sponsorId": 50,
    "subscriptionTierId": 2,
    "quantity": 10,
    "unitPrice": 49.99,
    "totalAmount": 499.90,
    "currency": "USD",
    "purchaseDate": "2025-10-08T12:30:00",
    "paymentMethod": "CreditCard",
    "paymentReference": "STRIPE-ch_abc123",
    "paymentStatus": "Completed",
    "codesGenerated": 10,
    "codesUsed": 0,
    "codePrefix": "AGRI",
    "validityDays": 365,
    "status": "Active",
    "generatedCodes": [
      {
        "id": 100,
        "code": "AGRI-A1B2C",
        "tierName": "S",
        "isUsed": false,
        "isActive": true,
        "expiryDate": "2026-10-08T00:00:00",
        "usedDate": null,
        "usedByUserId": null
      }
      // ... 9 more codes
    ]
  }
}
```

**UI Guidelines**:
- Show tier selection with pricing
- Quantity picker (min: 1, max: 1000)
- Display total price calculation
- After purchase: show generated codes list
- Allow export/share codes

---

### 4. Get My Sponsorship Codes

**Purpose**: View all generated codes

**Endpoint**: `GET /api/v1/sponsorship/codes?onlyUnused={true|false}`

**Authorization**: `Sponsor` role required

**Request**:
```http
GET {{baseUrl}}/api/v1/sponsorship/codes?onlyUnused=true
Authorization: Bearer {sponsor_token}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": 100,
      "code": "AGRI-A1B2C",
      "tierName": "S",
      "isUsed": false,
      "isActive": true,
      "expiryDate": "2026-10-08T00:00:00",
      "usedDate": null,
      "usedByUserId": null,
      "usedByUserName": null
    },
    {
      "id": 101,
      "code": "AGRI-D3E4F",
      "tierName": "S",
      "isUsed": true,
      "isActive": true,
      "expiryDate": "2026-10-08T00:00:00",
      "usedDate": "2025-10-08T13:00:00",
      "usedByUserId": 131,
      "usedByUserName": "Farmer Ali"
    }
  ]
}
```

**UI Guidelines**:
- Filter: All / Unused / Used
- Search by code
- Show usage status with badges
- Tap code to copy
- Share button for unused codes

---

### 5. Send Sponsorship Links (SMS/WhatsApp)

**Purpose**: Bulk send redemption links to farmers

**Endpoint**: `POST /api/v1/sponsorship/send-link`

**Authorization**: `Sponsor` role required

**Request**:
```http
POST {{baseUrl}}/api/v1/sponsorship/send-link
Authorization: Bearer {sponsor_token}
Content-Type: application/json

{
  "channel": "SMS",
  "customMessage": "Tarımda dijitalleşme için özel hediye!",
  "recipients": [
    {
      "code": "AGRI-A1B2C",
      "phone": "+905551234567",
      "name": "Ahmet Kaya"
    },
    {
      "code": "AGRI-D3E4F",
      "phone": "5559876543",
      "name": "Mehmet Demir"
    }
  ]
}
```

**Field Details**:
- `channel`: "SMS" or "WhatsApp"
- `customMessage`: Optional custom text (max 160 chars)
- `recipients`: Array of recipient objects
  - `code`: Sponsorship code to send
  - `phone`: Farmer's phone (with or without +90)
  - `name`: Farmer's name for personalization

**Response** (200 OK):
```json
{
  "success": true,
  "message": "📱 2 link başarıyla gönderildi via SMS",
  "data": {
    "totalSent": 2,
    "successCount": 2,
    "failureCount": 0,
    "results": [
      {
        "code": "AGRI-A1B2C",
        "phone": "+905551234567",
        "success": true,
        "errorMessage": null,
        "deliveryStatus": "Sent"
      },
      {
        "code": "AGRI-D3E4F",
        "phone": "+905559876543",
        "success": true,
        "errorMessage": null,
        "deliveryStatus": "Sent"
      }
    ]
  }
}
```

**Response - Partial Failure**:
```json
{
  "success": true,
  "message": "📱 1 link başarıyla gönderildi via SMS",
  "data": {
    "totalSent": 2,
    "successCount": 1,
    "failureCount": 1,
    "results": [
      {
        "code": "AGRI-A1B2C",
        "phone": "+905551234567",
        "success": true,
        "errorMessage": null,
        "deliveryStatus": "Sent"
      },
      {
        "code": "AGRI-INVALID",
        "phone": "+905559876543",
        "success": false,
        "errorMessage": "Kod bulunamadı veya kullanılamaz durumda",
        "deliveryStatus": "Failed - Invalid Code"
      }
    ]
  }
}
```

**UI Guidelines**:
- Phone field: auto-format to +90XXXXXXXXXX
- Name field: required for personalization
- Channel selector: SMS / WhatsApp
- Custom message: character counter (160 max)
- After send: show success/failure summary
- Allow retry for failed sends

---

### 6. Get Sponsored Farmers

**Purpose**: View farmers who redeemed codes

**Endpoint**: `GET /api/v1/sponsorship/farmers`

**Authorization**: `Sponsor` role required

**Request**:
```http
GET {{baseUrl}}/api/v1/sponsorship/farmers
Authorization: Bearer {sponsor_token}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "userId": 131,
      "fullName": "Ahmet Kaya",
      "email": "ahmet@example.com",
      "phone": "+905551234567",
      "redeemedCode": "AGRI-A1B2C",
      "subscriptionTierId": 2,
      "tierName": "S",
      "activationDate": "2025-10-08T13:00:00",
      "expiryDate": "2025-11-07T13:00:00",
      "isActive": true,
      "totalAnalyses": 15
    }
  ]
}
```

**UI Guidelines**:
- List view with farmer cards
- Filter by tier / active status
- Sort by activation date
- Show analysis count badge
- Tap farmer to view details

---

### 7. Get Sponsorship Statistics

**Purpose**: Dashboard analytics

**Endpoint**: `GET /api/v1/sponsorship/statistics`

**Authorization**: `Sponsor` role required

**Request**:
```http
GET {{baseUrl}}/api/v1/sponsorship/statistics
Authorization: Bearer {sponsor_token}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "totalCodesGenerated": 100,
    "totalCodesUsed": 45,
    "totalCodesActive": 55,
    "totalFarmersSponsored": 45,
    "totalActiveFarmers": 38,
    "totalAnalysesSponsored": 520,
    "redemptionRate": 45.0,
    "averageAnalysesPerFarmer": 11.5,
    "topTier": {
      "tierId": 2,
      "tierName": "S",
      "count": 60
    },
    "recentRedemptions": [
      {
        "farmerId": 131,
        "farmerName": "Ahmet Kaya",
        "code": "AGRI-A1B2C",
        "redemptionDate": "2025-10-08T13:00:00"
      }
    ]
  }
}
```

**UI Guidelines**:
- Dashboard with key metrics
- Chart: codes generated vs used
- Redemption rate progress bar
- Recent redemptions timeline
- Export statistics option

---

## 🔄 Queue System

### How It Works

1. **Scenario**: Farmer has **active sponsored subscription** (Sponsorship A)
2. **Action**: Farmer redeems **new code** (Sponsorship B)
3. **Result**: Sponsorship B goes to **queue** (status: Pending)
4. **Auto-Activation**: When A expires, B automatically activates

### Event-Driven Activation

- Queue processes on **every API request** (not scheduled jobs)
- When farmer makes plant analysis:
  1. System checks for expired subscriptions
  2. Marks expired ones as `QueueStatus.Expired`
  3. Finds queued subscription linked to expired one
  4. Auto-activates queued subscription
  5. Request proceeds with newly active subscription

### Queue States

| QueueStatus | Value | Meaning |
|-------------|-------|---------|
| Pending | 0 | Waiting to activate |
| Active | 1 | Currently active |
| Expired | 2 | Ended (can trigger queue) |
| Cancelled | 3 | User cancelled |

### Mobile Implementation

**Check Queue Status**:
```http
GET /api/v1/subscriptions/my-subscription
```

Response includes:
```json
{
  "queuedSubscriptions": [
    {
      "id": 126,
      "tierName": "L",
      "queueStatus": 0,
      "queuedDate": "2025-10-08T14:00:00",
      "previousSponsorshipId": 125
    }
  ]
}
```

**UI Guidelines**:
- Badge on subscription card: "1 queued"
- Tap badge to see queue details
- Show estimated activation: `activeSubscription.endDate`
- Explain: "Will activate automatically when current expires"
- No manual activation needed

---

## 🎁 Tier-Based Features

### Tier Comparison

| Feature | S | M | L | XL |
|---------|---|---|---|-----|
| **Analysis Quota** | 10/day, 100/month | 20/day, 200/month | 50/day, 500/month | Unlimited |
| **Logo Display** | ❌ | Start screen | Start + Result | All screens |
| **Messaging** | ❌ | ✅ | ✅ | ✅ |
| **Smart Links** | ❌ | ❌ | ❌ | ✅ |
| **Profile Visibility** | ❌ | Basic | Enhanced | Premium |

### 1. Logo Display Permissions

**Purpose**: Check if sponsor logo can be shown on analysis screen

**Endpoint**: `GET /api/v1/sponsorship/logo-permissions/analysis/{plantAnalysisId}?screen={screen}`

**Authorization**: Any authenticated user

**Request**:
```http
GET {{baseUrl}}/api/v1/sponsorship/logo-permissions/analysis/500?screen=result
Authorization: Bearer {token}
```

**Query Parameters**:
- `screen`: "start" | "result" | "analysis" | "profile"

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "canDisplayLogo": true,
    "tierName": "L",
    "screen": "result",
    "logoUrl": "https://cdn.example.com/sponsor-logo.png",
    "companyName": "AgriTech Solutions"
  }
}
```

**UI Guidelines**:
- Call this endpoint before rendering analysis screen
- If `canDisplayLogo: true`, show logo at appropriate position
- Cache response per analysis ID
- Fallback: no logo if endpoint fails

---

### 2. Get Sponsor Display Info

**Purpose**: Get full sponsor branding for display

**Endpoint**: `GET /api/v1/sponsorship/display-info/analysis/{plantAnalysisId}?screen={screen}`

**Authorization**: Any authenticated user

**Request**:
```http
GET {{baseUrl}}/api/v1/sponsorship/display-info/analysis/500?screen=result
Authorization: Bearer {token}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "sponsorId": 50,
    "companyName": "AgriTech Solutions",
    "logoUrl": "https://cdn.example.com/logo.png",
    "websiteUrl": "https://agritech.com",
    "canDisplayLogo": true,
    "tierName": "L",
    "tierDisplayName": "Large"
  }
}
```

---

### 3. Messaging (M, L, XL Tiers Only)

**Send Message to Farmer**:

**Endpoint**: `POST /api/v1/sponsorship/messages`

**Authorization**: `Sponsor` role required

**Request**:
```http
POST {{baseUrl}}/api/v1/sponsorship/messages
Authorization: Bearer {sponsor_token}
Content-Type: application/json

{
  "toUserId": 131,
  "plantAnalysisId": 500,
  "messageText": "Analiziniz için teşekkürler! Daha fazla destek için bizimle iletişime geçin.",
  "messageType": "FollowUp"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "message": "Mesaj başarıyla gönderildi",
  "data": {
    "id": 50,
    "fromUserId": 50,
    "toUserId": 131,
    "plantAnalysisId": 500,
    "messageText": "Analiziniz için teşekkürler!...",
    "messageType": "FollowUp",
    "sentDate": "2025-10-08T15:00:00",
    "isRead": false
  }
}
```

**Response - Tier Not Allowed** (403 Forbidden):
```json
{
  "success": false,
  "message": "Mesajlaşma özelliği sadece M, L ve XL tier'larda kullanılabilir"
}
```

**Get Conversation**:

**Endpoint**: `GET /api/v1/sponsorship/messages/conversation?farmerId={farmerId}&plantAnalysisId={plantAnalysisId}`

**Request**:
```http
GET {{baseUrl}}/api/v1/sponsorship/messages/conversation?farmerId=131&plantAnalysisId=500
Authorization: Bearer {sponsor_token}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": 50,
      "fromUserId": 50,
      "fromUserName": "AgriTech Support",
      "toUserId": 131,
      "toUserName": "Ahmet Kaya",
      "messageText": "Analiziniz için teşekkürler!",
      "sentDate": "2025-10-08T15:00:00",
      "isRead": true,
      "readDate": "2025-10-08T15:05:00"
    }
  ]
}
```

---

### 4. Smart Links (XL Tier Only)

**Create Smart Link**:

**Endpoint**: `POST /api/v1/sponsorship/smart-links`

**Authorization**: `Sponsor` role (XL tier only)

**Request**:
```http
POST {{baseUrl}}/api/v1/sponsorship/smart-links
Authorization: Bearer {sponsor_token}
Content-Type: application/json

{
  "linkName": "Bahar Kampanyası 2025",
  "codes": ["AGRI-A1B2C", "AGRI-D3E4F"],
  "expiryDate": "2025-12-31T23:59:59",
  "maxRedemptions": 100,
  "isActive": true
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "message": "Smart link created successfully",
  "data": {
    "id": 10,
    "sponsorId": 50,
    "linkName": "Bahar Kampanyası 2025",
    "shortUrl": "https://ziraai.com/sl/abc123",
    "fullUrl": "https://ziraai.com/smart-link/abc123",
    "codes": ["AGRI-A1B2C", "AGRI-D3E4F"],
    "totalCodes": 2,
    "expiryDate": "2025-12-31T23:59:59",
    "maxRedemptions": 100,
    "currentRedemptions": 0,
    "isActive": true,
    "createdDate": "2025-10-08T15:30:00"
  }
}
```

**Get Smart Links**:

**Endpoint**: `GET /api/v1/sponsorship/smart-links`

**Request**:
```http
GET {{baseUrl}}/api/v1/sponsorship/smart-links
Authorization: Bearer {sponsor_token}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": 10,
      "linkName": "Bahar Kampanyası 2025",
      "shortUrl": "https://ziraai.com/sl/abc123",
      "totalCodes": 2,
      "currentRedemptions": 45,
      "maxRedemptions": 100,
      "isActive": true,
      "clickCount": 120,
      "conversionRate": 37.5
    }
  ]
}
```

**Get Smart Link Performance**:

**Endpoint**: `GET /api/v1/sponsorship/smart-links/performance`

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "totalLinks": 5,
    "totalClicks": 500,
    "totalRedemptions": 200,
    "averageConversionRate": 40.0,
    "topPerformingLink": {
      "linkName": "Bahar Kampanyası 2025",
      "clicks": 250,
      "redemptions": 120,
      "conversionRate": 48.0
    }
  }
}
```

---

## ⚠️ Error Handling

### HTTP Status Codes

| Code | Meaning | Action |
|------|---------|--------|
| 200 | Success | Process response |
| 400 | Bad Request | Show validation error |
| 401 | Unauthorized | Redirect to login |
| 403 | Forbidden | Show "Insufficient permissions" |
| 404 | Not Found | Show "Resource not found" |
| 500 | Server Error | Show "Try again later" |

### Common Error Responses

**Invalid Code**:
```json
{
  "success": false,
  "message": "Kod bulunamadı veya kullanılmış",
  "data": null
}
```

**Insufficient Tier**:
```json
{
  "success": false,
  "message": "Bu özellik sadece L ve XL tier'larda kullanılabilir"
}
```

**Expired Code**:
```json
{
  "success": false,
  "message": "Kodun süresi dolmuş"
}
```

### Retry Strategy

- **Network errors**: Auto-retry 3 times with exponential backoff
- **400 errors**: Don't retry, show validation message
- **401 errors**: Refresh token, then retry once
- **500 errors**: Retry once after 2 seconds

---

## ✅ Testing Checklist

### Farmer Flow
- [ ] Validate code (valid & invalid)
- [ ] Redeem code (immediate activation)
- [ ] Redeem code (queue when active exists)
- [ ] View my subscription (with queue)
- [ ] View my subscription (no queue)

### Sponsor Flow
- [ ] Create profile
- [ ] Get profile
- [ ] Purchase package
- [ ] View codes (all / unused / used)
- [ ] Send links via SMS
- [ ] Send links via WhatsApp
- [ ] View farmers
- [ ] View statistics

### Queue System
- [ ] Redeem 1st code → immediate activation
- [ ] Redeem 2nd code while 1st active → queued
- [ ] Redeem 3rd code → queued after 2nd
- [ ] Make plant analysis → auto-activation
- [ ] Check my-subscription → see queue

### Tier Features
- [ ] S tier: no logo, no messaging, no smart links
- [ ] M tier: start screen logo, messaging allowed
- [ ] L tier: start + result logo, messaging
- [ ] XL tier: all screens logo, messaging, smart links

### Error Handling
- [ ] Invalid code
- [ ] Expired code
- [ ] Used code
- [ ] Insufficient tier
- [ ] Network timeout
- [ ] Server error (500)

---

## 📞 Support

For technical questions or issues:
- Backend Team Lead: [Contact Info]
- API Documentation: `https://api.ziraai.com/swagger`
- Postman Collection: `ZiraAI_Complete_API_Collection_v6.1.json`

---

**Last Updated**: 2025-10-08
**Version**: 1.0
**Reviewed By**: Backend Team
