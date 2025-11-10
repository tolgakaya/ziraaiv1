# Admin Sponsor View & Analytics API - Complete Integration Documentation

## Overview

This document provides complete integration documentation for the Admin Sponsor View and Analytics feature. This feature enables administrators to:

1. **View Sponsor Perspective**: See what sponsors see (analyses, messages) without logging in as sponsor
2. **Identify Sponsorship Opportunities**: Analyze non-sponsored farmers and their analyses
3. **Measure Sponsorship Impact**: Compare sponsored vs non-sponsored metrics

**Feature Branch:** `feature/advanced-admin-operations`
**Pull Request:** #90
**Base URL:** `https://ziraai-api-sit.up.railway.app` (Staging)
**Authorization:** Required - JWT Bearer token with Administrator role

---

## Table of Contents

1. [Authentication](#authentication)
2. [Phase 1: Admin Sponsor View Endpoints](#phase-1-admin-sponsor-view-endpoints)
   - [Get Sponsor Analyses (Admin View)](#1-get-sponsor-analyses-admin-view)
   - [Get Sponsor Analysis Detail (Admin View)](#2-get-sponsor-analysis-detail-admin-view)
   - [Get Sponsor Messages (Admin View)](#3-get-sponsor-messages-admin-view)
   - [Send Message As Sponsor (Admin)](#4-send-message-as-sponsor-admin)
3. [Phase 2: Non-Sponsored Farmer Analytics](#phase-2-non-sponsored-farmer-analytics)
   - [Get Non-Sponsored Analyses](#5-get-non-sponsored-analyses)
   - [Get Non-Sponsored Farmer Detail](#6-get-non-sponsored-farmer-detail)
4. [Phase 3: Sponsorship Comparison Analytics](#phase-3-sponsorship-comparison-analytics)
   - [Get Sponsorship Comparison Analytics](#7-get-sponsorship-comparison-analytics)
5. [Operation Claims](#operation-claims)
6. [Error Responses](#error-responses)
7. [Usage Examples](#usage-examples)

---

## Authentication

All endpoints require JWT Bearer token authentication with Administrator role (GroupId = 1).

### Request Headers

```http
Authorization: Bearer {your-jwt-token}
Content-Type: application/json
```

### Required Operation Claims

| Claim ID | Claim Name | Description |
|----------|------------|-------------|
| 133 | GetSponsorAnalysesAsAdminQuery | Admin view of sponsor's analyses |
| 134 | GetSponsorAnalysisDetailAsAdminQuery | Admin view of detailed sponsor analysis |
| 135 | GetSponsorMessagesAsAdminQuery | Admin view of sponsor messages |
| 136 | GetNonSponsoredAnalysesQuery | Query non-sponsored analyses |
| 137 | GetNonSponsoredFarmerDetailQuery | Query non-sponsored farmer details |
| 138 | GetSponsorshipComparisonAnalyticsQuery | Compare sponsored vs non-sponsored metrics |
| 139 | SendMessageAsSponsorCommand | Send message on behalf of sponsor |

---

## Phase 1: Admin Sponsor View Endpoints

### 1. Get Sponsor Analyses (Admin View)

**Purpose:** View all analyses associated with a specific sponsor from admin perspective, with full filtering and sorting capabilities.

**Use Cases:**
- Monitor sponsor's analysis activity
- Review analyses by sponsorship tier
- Track unread messages from farmers
- Audit sponsor-farmer interactions

#### Endpoint

```http
GET /api/admin/sponsorship/sponsors/{sponsorId}/analyses
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sponsorId | integer | Yes | Sponsor's user ID |

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | integer | No | 1 | Page number for pagination |
| pageSize | integer | No | 20 | Number of items per page |
| sortBy | string | No | "date" | Sort field: `date`, `healthScore`, `cropType` |
| sortOrder | string | No | "desc" | Sort order: `asc`, `desc` |
| filterByTier | string | No | null | Filter by tier: `S`, `M`, `L`, `XL` |
| filterByCropType | string | No | null | Filter by crop type (partial match) |
| startDate | datetime | No | null | Filter analyses from this date (inclusive) |
| endDate | datetime | No | null | Filter analyses to this date (inclusive) |
| dealerId | integer | No | null | Filter by dealer ID |
| filterByMessageStatus | string | No | null | Filter by message status |
| hasUnreadMessages | boolean | No | null | Filter by unread messages presence |

#### Example Request

```http
GET /api/admin/sponsorship/sponsors/42/analyses?page=1&pageSize=20&sortBy=date&sortOrder=desc&filterByTier=L&hasUnreadMessages=true
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response Structure

```json
{
  "data": [
    {
      "plantAnalysisId": 1234,
      "analysisDate": "2025-11-07T14:30:00",
      "cropType": "Tomato",
      "location": "Antalya, Turkey",
      "overallHealthScore": 78,
      "primaryConcern": "Leaf spot disease detected",
      "imageUrl": "https://storage.example.com/analysis-images/1234.jpg",
      "analysisStatus": "completed",
      "userId": 567,
      "userFullName": "Ahmet Yılmaz",
      "userEmail": "ahmet@example.com",
      "userPhone": "+905551234567",
      "sponsorshipTier": "L",
      "sponsorshipCodeId": 89,
      "sponsorshipCode": "AGRO-L-ABC123",
      "dealerId": 12,
      "dealerName": "Tarım Bayii A.Ş.",
      "unreadMessageCount": 2,
      "totalMessageCount": 5,
      "lastMessageDate": "2025-11-07T16:45:00",
      "lastMessageSender": "farmer",
      "lastMessagePreview": "Önerilen ilacı kullandım, teşekkürler"
    }
  ],
  "success": true,
  "message": "Retrieved 15 analyses for sponsor ID 42",
  "pageNumber": 1,
  "pageSize": 20,
  "totalRecords": 15,
  "totalPages": 1
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| plantAnalysisId | integer | Unique analysis ID |
| analysisDate | datetime | When analysis was performed |
| cropType | string | Type of crop analyzed |
| location | string | Geographic location |
| overallHealthScore | integer | Health score (0-100) |
| primaryConcern | string | Main issue identified |
| imageUrl | string | Analysis image URL |
| analysisStatus | string | Status: `pending`, `completed`, `failed` |
| userId | integer | Farmer's user ID |
| userFullName | string | Farmer's full name |
| userEmail | string | Farmer's email |
| userPhone | string | Farmer's phone number |
| sponsorshipTier | string | Tier: `S`, `M`, `L`, `XL` |
| sponsorshipCodeId | integer | Sponsorship code ID used |
| sponsorshipCode | string | Actual sponsorship code |
| dealerId | integer | Dealer ID (if applicable) |
| dealerName | string | Dealer name |
| unreadMessageCount | integer | Number of unread messages |
| totalMessageCount | integer | Total messages exchanged |
| lastMessageDate | datetime | When last message was sent |
| lastMessageSender | string | Who sent last message: `sponsor`, `farmer` |
| lastMessagePreview | string | Preview of last message (first 100 chars) |

---

### 2. Get Sponsor Analysis Detail (Admin View)

**Purpose:** View complete details of a single analysis including full message history.

**Use Cases:**
- Review detailed analysis results
- Read full conversation between sponsor and farmer
- Audit sponsor's advice quality
- Investigate specific farmer issues

#### Endpoint

```http
GET /api/admin/sponsorship/sponsors/{sponsorId}/analyses/{analysisId}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sponsorId | integer | Yes | Sponsor's user ID |
| analysisId | integer | Yes | Plant analysis ID |

#### Example Request

```http
GET /api/admin/sponsorship/sponsors/42/analyses/1234
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response Structure

```json
{
  "data": {
    "plantAnalysisId": 1234,
    "analysisDate": "2025-11-07T14:30:00",
    "cropType": "Tomato",
    "location": "Antalya, Turkey",
    "overallHealthScore": 78,
    "primaryConcern": "Leaf spot disease detected",
    "diseaseDetected": "Septoria leaf spot",
    "confidenceScore": 92,
    "recommendations": "1. Remove infected leaves\n2. Apply copper-based fungicide\n3. Improve air circulation",
    "imageUrl": "https://storage.example.com/analysis-images/1234.jpg",
    "analysisStatus": "completed",
    "userId": 567,
    "userFullName": "Ahmet Yılmaz",
    "userEmail": "ahmet@example.com",
    "userPhone": "+905551234567",
    "sponsorshipTier": "L",
    "sponsorshipCodeId": 89,
    "sponsorshipCode": "AGRO-L-ABC123",
    "dealerId": 12,
    "dealerName": "Tarım Bayii A.Ş.",
    "messages": [
      {
        "messageId": 101,
        "senderId": 567,
        "senderName": "Ahmet Yılmaz",
        "senderType": "farmer",
        "messageContent": "Bu hastalık için ne önerirsiniz?",
        "sentDate": "2025-11-07T15:00:00",
        "isRead": true,
        "readDate": "2025-11-07T15:30:00"
      },
      {
        "messageId": 102,
        "senderId": 42,
        "senderName": "Agro Sponsor Co.",
        "senderType": "sponsor",
        "messageContent": "Bakırlı fungisit kullanmanızı öneriyorum. Ürünümüz olan AgroFung 500'ü deneyebilirsiniz.",
        "sentDate": "2025-11-07T15:35:00",
        "isRead": true,
        "readDate": "2025-11-07T16:00:00"
      },
      {
        "messageId": 103,
        "senderId": 567,
        "senderName": "Ahmet Yılmaz",
        "senderType": "farmer",
        "messageContent": "Önerilen ilacı kullandım, teşekkürler. Sonuçları bildireceğim.",
        "sentDate": "2025-11-07T16:45:00",
        "isRead": false,
        "readDate": null
      }
    ],
    "messageStatistics": {
      "totalMessages": 3,
      "unreadMessages": 1,
      "sponsorMessages": 1,
      "farmerMessages": 2,
      "firstMessageDate": "2025-11-07T15:00:00",
      "lastMessageDate": "2025-11-07T16:45:00"
    }
  },
  "success": true,
  "message": "Retrieved analysis detail for sponsor ID 42, analysis ID 1234"
}
```

#### Response Fields

**Analysis Details:**
| Field | Type | Description |
|-------|------|-------------|
| diseaseDetected | string | Specific disease name |
| confidenceScore | integer | AI confidence (0-100) |
| recommendations | string | AI-generated recommendations |
| (other fields) | - | Same as list endpoint |

**Message Object:**
| Field | Type | Description |
|-------|------|-------------|
| messageId | integer | Unique message ID |
| senderId | integer | User ID of sender |
| senderName | string | Name of sender |
| senderType | string | `sponsor` or `farmer` |
| messageContent | string | Full message text |
| sentDate | datetime | When message was sent |
| isRead | boolean | Whether message has been read |
| readDate | datetime | When message was read (null if unread) |

**Message Statistics:**
| Field | Type | Description |
|-------|------|-------------|
| totalMessages | integer | Total messages in conversation |
| unreadMessages | integer | Number of unread messages |
| sponsorMessages | integer | Messages from sponsor |
| farmerMessages | integer | Messages from farmer |
| firstMessageDate | datetime | When conversation started |
| lastMessageDate | datetime | Last message timestamp |

---

### 3. Get Sponsor Messages (Admin View)

**Purpose:** View all messages across all analyses for a sponsor with filtering options.

**Use Cases:**
- Monitor sponsor communication quality
- Track response times
- Identify unanswered farmer questions
- Generate sponsor performance reports

#### Endpoint

```http
GET /api/admin/sponsorship/sponsors/{sponsorId}/messages
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sponsorId | integer | Yes | Sponsor's user ID |

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | integer | No | 1 | Page number |
| pageSize | integer | No | 50 | Items per page |
| sortBy | string | No | "date" | Sort by: `date`, `sender` |
| sortOrder | string | No | "desc" | Sort order: `asc`, `desc` |
| filterBySender | string | No | null | Filter by: `sponsor`, `farmer` |
| filterByReadStatus | boolean | No | null | Filter by read status |
| startDate | datetime | No | null | Filter from date |
| endDate | datetime | No | null | Filter to date |
| analysisId | integer | No | null | Filter by specific analysis |

#### Example Request

```http
GET /api/admin/sponsorship/sponsors/42/messages?page=1&pageSize=50&filterBySender=farmer&filterByReadStatus=false
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response Structure

```json
{
  "data": [
    {
      "messageId": 103,
      "plantAnalysisId": 1234,
      "cropType": "Tomato",
      "analysisDate": "2025-11-07T14:30:00",
      "senderId": 567,
      "senderName": "Ahmet Yılmaz",
      "senderType": "farmer",
      "receiverId": 42,
      "receiverName": "Agro Sponsor Co.",
      "messageContent": "Önerilen ilacı kullandım, teşekkürler. Sonuçları bildireceğim.",
      "sentDate": "2025-11-07T16:45:00",
      "isRead": false,
      "readDate": null,
      "sponsorshipTier": "L",
      "userId": 567,
      "userFullName": "Ahmet Yılmaz"
    },
    {
      "messageId": 99,
      "plantAnalysisId": 1200,
      "cropType": "Cucumber",
      "analysisDate": "2025-11-05T10:15:00",
      "senderId": 450,
      "senderName": "Mehmet Demir",
      "senderType": "farmer",
      "receiverId": 42,
      "receiverName": "Agro Sponsor Co.",
      "messageContent": "Bitki hala hasta görünüyor, başka ne yapabilirim?",
      "sentDate": "2025-11-06T08:30:00",
      "isRead": false,
      "readDate": null,
      "sponsorshipTier": "M",
      "userId": 450,
      "userFullName": "Mehmet Demir"
    }
  ],
  "success": true,
  "message": "Retrieved 2 unread farmer messages for sponsor ID 42",
  "pageNumber": 1,
  "pageSize": 50,
  "totalRecords": 2,
  "totalPages": 1
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| messageId | integer | Unique message ID |
| plantAnalysisId | integer | Related analysis ID |
| cropType | string | Crop type for context |
| analysisDate | datetime | When analysis was done |
| senderId | integer | Message sender user ID |
| senderName | string | Sender's name |
| senderType | string | `sponsor` or `farmer` |
| receiverId | integer | Message receiver user ID |
| receiverName | string | Receiver's name |
| messageContent | string | Full message text |
| sentDate | datetime | When sent |
| isRead | boolean | Read status |
| readDate | datetime | When read (null if unread) |
| sponsorshipTier | string | Tier: `S`, `M`, `L`, `XL` |
| userId | integer | Farmer's user ID |
| userFullName | string | Farmer's full name |

---

### 4. Send Message As Sponsor (Admin)

**Purpose:** Send a message on behalf of a sponsor to a farmer.

**Use Cases:**
- Provide expert advice when sponsor is unavailable
- Handle urgent farmer questions
- Quality control for sponsor responses
- Customer service escalation

#### Endpoint

```http
POST /api/admin/sponsorship/sponsors/{sponsorId}/send-message
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sponsorId | integer | Yes | Sponsor's user ID |

#### Request Body

```json
{
  "farmerUserId": 567,
  "plantAnalysisId": 1234,
  "message": "Merhaba Ahmet Bey, tavsiyelerimizi uyguladığınız için teşekkürler. Lütfen 7 gün sonra tekrar kontrol edin ve sonuçları bize bildirin. Gerekirse ek öneriler sunabiliriz.",
  "messageType": "Information",
  "subject": "Takip Mesajı",
  "priority": "Normal",
  "category": "General"
}
```

#### Request Fields

| Field | Type | Required | Max Length | Description |
|-------|------|----------|------------|-------------|
| farmerUserId | integer | Yes | - | Farmer (recipient) user ID |
| plantAnalysisId | integer | Yes | - | Plant analysis ID for the conversation |
| message | string | Yes | 2000 | Message text to send |
| messageType | string | No | 50 | Message type (default: "Information") |
| subject | string | No | 200 | Message subject (optional) |
| priority | string | No | 20 | Priority level (default: "Normal") |
| category | string | No | 50 | Message category (default: "General") |

#### Example Request

```http
POST /api/admin/sponsorship/sponsors/42/send-message
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "farmerUserId": 567,
  "plantAnalysisId": 1234,
  "message": "Merhaba Ahmet Bey, tavsiyelerimizi uyguladığınız için teşekkürler.",
  "messageType": "Information",
  "priority": "Normal"
}
```

#### Response Structure

```json
{
  "success": true,
  "message": "Message sent successfully as sponsor ID 42 to analysis ID 1234",
  "data": {
    "messageId": 104,
    "plantAnalysisId": 1234,
    "senderId": 42,
    "senderName": "Agro Sponsor Co.",
    "receiverId": 567,
    "receiverName": "Ahmet Yılmaz",
    "messageContent": "Merhaba Ahmet Bey, tavsiyelerimizi uyguladığınız için teşekkürler.",
    "sentDate": "2025-11-08T10:15:00",
    "sentByAdmin": true,
    "adminUserId": 1,
    "adminUserName": "System Administrator"
  }
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| messageId | integer | New message ID |
| plantAnalysisId | integer | Analysis ID |
| senderId | integer | Sponsor user ID (on behalf of) |
| senderName | string | Sponsor name |
| receiverId | integer | Farmer user ID |
| receiverName | string | Farmer name |
| messageContent | string | Message text |
| sentDate | datetime | When sent |
| sentByAdmin | boolean | Always `true` for this endpoint |
| adminUserId | integer | Actual admin who sent |
| adminUserName | string | Admin's name |

---

## Phase 2: Non-Sponsored Farmer Analytics

### 5. Get Non-Sponsored Analyses

**Purpose:** Discover analyses from farmers who haven't used any sponsorship codes.

**Use Cases:**
- Identify sponsorship opportunities
- Target farmers for sponsor outreach
- Analyze unsponsored farmer behavior
- Generate leads for sponsors

#### Endpoint

```http
GET /api/admin/sponsorship/non-sponsored/analyses
```

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | integer | No | 1 | Page number |
| pageSize | integer | No | 20 | Items per page |
| sortBy | string | No | "date" | Sort by: `date`, `cropType`, `status` |
| sortOrder | string | No | "desc" | Sort order: `asc`, `desc` |
| filterByCropType | string | No | null | Filter by crop type (partial match) |
| startDate | datetime | No | null | Filter from date |
| endDate | datetime | No | null | Filter to date |
| filterByStatus | string | No | null | Filter by: `pending`, `completed`, `failed` |
| userId | integer | No | null | Filter by specific farmer |

#### Example Request

```http
GET /api/admin/sponsorship/non-sponsored/analyses?page=1&pageSize=20&filterByCropType=tomato&startDate=2025-11-01
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response Structure

```json
{
  "data": [
    {
      "plantAnalysisId": 5678,
      "analysisDate": "2025-11-07T09:20:00",
      "analysisStatus": "completed",
      "cropType": "Tomato",
      "location": "İzmir, Turkey",
      "userId": 789,
      "userFullName": "Fatma Kaya",
      "userEmail": "fatma@example.com",
      "userPhone": "+905559876543",
      "imageUrl": "https://storage.example.com/analysis-images/5678.jpg",
      "overallHealthScore": 65,
      "primaryConcern": "Nutrient deficiency detected",
      "isOnBehalfOf": false,
      "createdByAdminId": null
    },
    {
      "plantAnalysisId": 5680,
      "analysisDate": "2025-11-07T11:45:00",
      "analysisStatus": "completed",
      "cropType": "Tomato",
      "location": "Bursa, Turkey",
      "userId": 820,
      "userFullName": "Ali Çelik",
      "userEmail": "ali@example.com",
      "userPhone": "+905551112233",
      "imageUrl": "https://storage.example.com/analysis-images/5680.jpg",
      "overallHealthScore": 45,
      "primaryConcern": "Severe blight infection",
      "isOnBehalfOf": false,
      "createdByAdminId": null
    }
  ],
  "success": true,
  "message": "Retrieved 2 non-sponsored analyses",
  "pageNumber": 1,
  "pageSize": 20,
  "totalRecords": 2,
  "totalPages": 1
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| plantAnalysisId | integer | Analysis ID |
| analysisDate | datetime | When analysis was performed |
| analysisStatus | string | `pending`, `completed`, `failed` |
| cropType | string | Type of crop |
| location | string | Geographic location |
| userId | integer | Farmer's user ID |
| userFullName | string | Farmer's full name |
| userEmail | string | Farmer's email |
| userPhone | string | Farmer's phone |
| imageUrl | string | Analysis image URL |
| overallHealthScore | integer | Health score (0-100) |
| primaryConcern | string | Main issue identified |
| isOnBehalfOf | boolean | Whether analysis was created by admin on behalf of farmer |
| createdByAdminId | integer | Admin ID if created on behalf (null otherwise) |

---

### 6. Get Non-Sponsored Farmer Detail

**Purpose:** Get comprehensive profile and analysis history for a non-sponsored farmer.

**Use Cases:**
- Evaluate farmer as sponsorship candidate
- Understand farmer's crop patterns
- Assess farmer's engagement level
- Prepare sponsorship pitch

#### Endpoint

```http
GET /api/admin/sponsorship/non-sponsored/farmers/{userId}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userId | integer | Yes | Farmer's user ID |

#### Example Request

```http
GET /api/admin/sponsorship/non-sponsored/farmers/789
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response Structure

```json
{
  "data": {
    "userId": 789,
    "fullName": "Fatma Kaya",
    "email": "fatma@example.com",
    "mobilePhone": "+905559876543",
    "status": true,
    "recordDate": "2025-06-15T10:30:00",
    "totalAnalyses": 12,
    "completedAnalyses": 11,
    "pendingAnalyses": 1,
    "failedAnalyses": 0,
    "firstAnalysisDate": "2025-06-20T08:15:00",
    "lastAnalysisDate": "2025-11-07T09:20:00",
    "averageHealthScore": 68,
    "cropTypes": [
      "Tomato",
      "Cucumber",
      "Pepper"
    ],
    "commonConcerns": [
      "Nutrient deficiency detected",
      "Leaf spot disease detected",
      "Pest infestation - aphids",
      "Water stress symptoms",
      "Fungal infection suspected"
    ],
    "recentAnalyses": [
      {
        "plantAnalysisId": 5678,
        "analysisDate": "2025-11-07T09:20:00",
        "analysisStatus": "completed",
        "cropType": "Tomato",
        "location": "İzmir, Turkey",
        "userId": 789,
        "userFullName": "Fatma Kaya",
        "userEmail": "fatma@example.com",
        "userPhone": "+905559876543",
        "imageUrl": "https://storage.example.com/analysis-images/5678.jpg",
        "overallHealthScore": 65,
        "primaryConcern": "Nutrient deficiency detected",
        "isOnBehalfOf": false,
        "createdByAdminId": null
      },
      {
        "plantAnalysisId": 5650,
        "analysisDate": "2025-11-05T14:30:00",
        "analysisStatus": "completed",
        "cropType": "Cucumber",
        "location": "İzmir, Turkey",
        "userId": 789,
        "userFullName": "Fatma Kaya",
        "userEmail": "fatma@example.com",
        "userPhone": "+905559876543",
        "imageUrl": "https://storage.example.com/analysis-images/5650.jpg",
        "overallHealthScore": 72,
        "primaryConcern": "Leaf spot disease detected",
        "isOnBehalfOf": false,
        "createdByAdminId": null
      }
    ]
  },
  "success": true,
  "message": "Retrieved detail for non-sponsored farmer Fatma Kaya (12 analyses)"
}
```

#### Response Fields

**Farmer Profile:**
| Field | Type | Description |
|-------|------|-------------|
| userId | integer | Farmer's user ID |
| fullName | string | Farmer's full name |
| email | string | Email address |
| mobilePhone | string | Mobile phone number |
| status | boolean | Account active status |
| recordDate | datetime | Registration date |

**Analysis Statistics:**
| Field | Type | Description |
|-------|------|-------------|
| totalAnalyses | integer | Total non-sponsored analyses |
| completedAnalyses | integer | Successfully completed analyses |
| pendingAnalyses | integer | Analyses in progress |
| failedAnalyses | integer | Failed analyses |
| firstAnalysisDate | datetime | First analysis date |
| lastAnalysisDate | datetime | Most recent analysis date |
| averageHealthScore | integer | Average health score across all analyses |

**Agricultural Profile:**
| Field | Type | Description |
|-------|------|-------------|
| cropTypes | string[] | List of unique crop types analyzed |
| commonConcerns | string[] | Top 5 most frequent concerns/issues |
| recentAnalyses | array | Last 5 analyses (same structure as Get Non-Sponsored Analyses) |

---

## Phase 3: Sponsorship Comparison Analytics

### 7. Get Sponsorship Comparison Analytics

**Purpose:** Compare sponsored vs non-sponsored analysis metrics to measure sponsorship impact.

**Use Cases:**
- Demonstrate ROI of sponsorship program
- Identify performance differences
- Generate executive reports
- Support business development pitches

#### Endpoint

```http
GET /api/admin/sponsorship/comparison/analytics
```

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| startDate | datetime | No | null | Compare from this date (inclusive) |
| endDate | datetime | No | null | Compare to this date (inclusive) |

#### Example Request

```http
GET /api/admin/sponsorship/comparison/analytics?startDate=2025-10-01&endDate=2025-11-08
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response Structure

```json
{
  "data": {
    "dateRange": {
      "startDate": "2025-10-01T00:00:00",
      "endDate": "2025-11-08T23:59:59"
    },
    "totalAnalyses": 1250,
    "sponsorshipRate": 68.4,
    "sponsoredAnalytics": {
      "totalAnalyses": 855,
      "completedAnalyses": 820,
      "pendingAnalyses": 30,
      "failedAnalyses": 5,
      "averageHealthScore": 74,
      "uniqueUsers": 245,
      "topCropTypes": {
        "Tomato": 320,
        "Cucumber": 215,
        "Pepper": 180,
        "Eggplant": 95,
        "Lettuce": 45
      }
    },
    "nonSponsoredAnalytics": {
      "totalAnalyses": 395,
      "completedAnalyses": 350,
      "pendingAnalyses": 35,
      "failedAnalyses": 10,
      "averageHealthScore": 62,
      "uniqueUsers": 180,
      "topCropTypes": {
        "Tomato": 150,
        "Cucumber": 90,
        "Pepper": 85,
        "Eggplant": 45,
        "Lettuce": 25
      }
    },
    "comparisonMetrics": {
      "averageHealthScoreDifference": 12,
      "completionRateSponsored": 95.91,
      "completionRateNonSponsored": 88.61,
      "completionRateDifference": 7.3,
      "userEngagementRatio": 1.36
    }
  },
  "success": true,
  "message": "Comparison analytics: 68.4% sponsorship rate (855/1250)"
}
```

#### Response Fields

**Date Range:**
| Field | Type | Description |
|-------|------|-------------|
| startDate | datetime | Filter start date (null = all time) |
| endDate | datetime | Filter end date (null = all time) |

**Overall Metrics:**
| Field | Type | Description |
|-------|------|-------------|
| totalAnalyses | integer | Total analyses in date range |
| sponsorshipRate | decimal | Percentage of sponsored analyses |

**Analytics Segment (Sponsored & Non-Sponsored):**
| Field | Type | Description |
|-------|------|-------------|
| totalAnalyses | integer | Total analyses in segment |
| completedAnalyses | integer | Successfully completed |
| pendingAnalyses | integer | In progress |
| failedAnalyses | integer | Failed analyses |
| averageHealthScore | integer | Average health score (0-100) |
| uniqueUsers | integer | Distinct farmers in segment |
| topCropTypes | object | Top 5 crop types with counts |

**Comparison Metrics:**
| Field | Type | Description |
|-------|------|-------------|
| averageHealthScoreDifference | integer | Sponsored - Non-sponsored health score |
| completionRateSponsored | decimal | % completion rate for sponsored |
| completionRateNonSponsored | decimal | % completion rate for non-sponsored |
| completionRateDifference | decimal | Sponsored - Non-sponsored completion % |
| userEngagementRatio | decimal | Sponsored users / Non-sponsored users |

**Key Insights:**
- **Positive health score difference**: Sponsored crops are healthier
- **Higher completion rate**: Sponsored analyses more likely to complete successfully
- **User engagement ratio > 1**: Sponsored farmers more engaged

---

## Operation Claims

### Database Setup

Before using these endpoints, ensure operation claims are inserted into the database:

```sql
-- Insert Operation Claims
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description") VALUES
(133, 'GetSponsorAnalysesAsAdminQuery', 'Get Sponsor Analyses (Admin)', 'Admin view of sponsor''s analyses with filters and messaging'),
(134, 'GetSponsorAnalysisDetailAsAdminQuery', 'Get Sponsor Analysis Detail (Admin)', 'Admin view of detailed sponsor analysis with messages'),
(135, 'GetSponsorMessagesAsAdminQuery', 'Get Sponsor Messages (Admin)', 'Admin view of all sponsor messages with filters'),
(136, 'GetNonSponsoredAnalysesQuery', 'Get Non-Sponsored Analyses', 'Query analyses without sponsorship for opportunity identification'),
(137, 'GetNonSponsoredFarmerDetailQuery', 'Get Non-Sponsored Farmer Detail', 'Detailed farmer profile for sponsorship targeting'),
(138, 'GetSponsorshipComparisonAnalyticsQuery', 'Get Sponsorship Comparison Analytics', 'Compare sponsored vs non-sponsored analysis metrics'),
(139, 'SendMessageAsSponsorCommand', 'Send Message As Sponsor (Admin)', 'Admin send message on behalf of sponsor');

-- Assign to Administrators Group (GroupId = 1)
INSERT INTO "GroupClaims" ("GroupId", "ClaimId") VALUES
(1, 133), (1, 134), (1, 135), (1, 136), (1, 137), (1, 138), (1, 139);
```

---

## Error Responses

### Common Error Formats

#### 401 Unauthorized

```json
{
  "success": false,
  "message": "Unauthorized access"
}
```

#### 403 Forbidden

```json
{
  "success": false,
  "message": "User does not have the required operation claim: GetSponsorAnalysesAsAdminQuery"
}
```

#### 404 Not Found

```json
{
  "success": false,
  "message": "User not found"
}
```

**Or:**

```json
{
  "success": false,
  "message": "Analysis not found or not associated with sponsor ID 42"
}
```

#### 400 Bad Request

```json
{
  "success": false,
  "message": "Invalid request parameters",
  "errors": {
    "messageContent": ["Message content is required", "Message content cannot exceed 2000 characters"]
  }
}
```

#### 500 Internal Server Error

```json
{
  "success": false,
  "message": "An error occurred while processing your request"
}
```

---

## Usage Examples

### Example 1: Monitor Sponsor's Unread Messages

**Scenario:** Admin wants to see all unread farmer messages for a sponsor to ensure timely responses.

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/sponsors/42/messages?filterBySender=farmer&filterByReadStatus=false" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Result:** List of all unread messages from farmers to sponsor ID 42.

---

### Example 2: Find High-Value Sponsorship Opportunities

**Scenario:** Admin wants to find active farmers with many analyses but no sponsor.

**Step 1:** Get non-sponsored analyses from last 30 days:

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/non-sponsored/analyses?startDate=2025-10-08&pageSize=100" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Step 2:** For high-activity farmers, get detailed profile:

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/non-sponsored/farmers/789" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Result:** Farmer profile showing 12 total analyses, consistent crop types (Tomato, Cucumber, Pepper), and common issues - ideal sponsorship candidate.

---

### Example 3: Generate Monthly Sponsorship Impact Report

**Scenario:** Generate executive report showing sponsorship program effectiveness.

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/comparison/analytics?startDate=2025-10-01&endDate=2025-10-31" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Key Metrics to Report:**
- Sponsorship Rate: 68.4% (855 of 1250 analyses)
- Health Score Improvement: +12 points for sponsored crops
- Completion Rate Improvement: +7.3% for sponsored analyses
- User Engagement: 1.36x higher for sponsored farmers

---

### Example 4: Admin Responds to Urgent Farmer Question

**Scenario:** Farmer has critical plant disease, sponsor hasn't responded in 24 hours.

**Step 1:** View the analysis detail:

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/sponsors/42/analyses/1234" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Step 2:** Send expert response on behalf of sponsor:

```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/sponsors/42/send-message" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "farmerUserId": 170,
    "plantAnalysisId": 1234,
    "message": "Acil durum nedeniyle uzman ekibimiz yanıtlıyor: Tespit edilen yanıklık hastalığı için derhal bakırlı fungisit uygulayın. 3 gün içinde iyileşme görmezseniz lütfen tekrar bilgi verin.",
    "messageType": "Information",
    "priority": "High"
  }'
```

**Result:** Message sent immediately, farmer receives expert help, message tagged as sent by admin.

---

### Example 5: Filter Analyses by Tier and Crop Type

**Scenario:** Admin wants to see all L-tier tomato analyses with unread messages.

```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/admin/sponsorship/sponsors/42/analyses?filterByTier=L&filterByCropType=tomato&hasUnreadMessages=true" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Result:** Targeted list of L-tier tomato analyses requiring attention.

---

## Testing Checklist

### Pre-Deployment Testing

- [ ] Verify JWT authentication works
- [ ] Confirm all operation claims (133-139) are in database
- [ ] Test Administrators group has all claims assigned
- [ ] Verify non-admin users get 403 Forbidden

### Endpoint Testing

**Phase 1:**
- [ ] Get sponsor analyses with various filters
- [ ] Get analysis detail with full message history
- [ ] Get sponsor messages with filters
- [ ] Send message as sponsor successfully

**Phase 2:**
- [ ] Get non-sponsored analyses with pagination
- [ ] Get farmer detail for existing farmer
- [ ] Handle non-existent farmer gracefully

**Phase 3:**
- [ ] Get comparison analytics without date filter
- [ ] Get comparison analytics with date range
- [ ] Verify calculations are accurate

### Integration Testing

- [ ] Test pagination across all list endpoints
- [ ] Verify sorting works correctly
- [ ] Test all filter combinations
- [ ] Confirm date range filtering accuracy
- [ ] Validate response field completeness

---

## Support & Troubleshooting

### Common Issues

**Issue 1: "Operation claim not found"**
- **Cause:** Claims 133-139 not inserted in database
- **Solution:** Run SQL migration script from Operation Claims section

**Issue 2: "User does not have required claim"**
- **Cause:** Admin user not in Administrators group (GroupId=1)
- **Solution:** Verify user's group membership in `UserGroups` table

**Issue 3: "Analysis not found or not associated with sponsor"**
- **Cause:** Analysis doesn't have matching `SponsorId`, `SponsorshipCodeId`, or `SponsorUserId`
- **Solution:** Verify sponsorship relationship in `PlantAnalysis` table

**Issue 4: Empty results for non-sponsored analyses**
- **Cause:** All analyses have sponsorship codes
- **Solution:** Normal behavior if all farmers are using sponsorship codes

---

## Changelog

### Version 1.0.0 - 2025-11-08

**Initial Release:**
- 7 new endpoints across 3 phases
- 7 new operation claims (133-139)
- Complete admin sponsor view functionality
- Non-sponsored farmer analytics
- Sponsorship comparison metrics

**Files Created:**
- 7 Query handlers
- 1 Command handler
- 9 DTOs
- 2 Seed file updates

**Branch:** `feature/advanced-admin-operations`
**Pull Request:** #90

---

## Additional Resources

- **Postman Collection:** Available in project root - `ZiraAI_Complete_API_Collection_v6.1.json`
- **Architecture Docs:** `claudedocs/ADMIN_SPONSOR_VIEW_REQUIREMENTS.md`
- **Environment Config:** `appsettings.Staging.json`
- **Deployment:** Auto-deploys to Railway staging on push to branch

---

**Document Version:** 1.0.0
**Last Updated:** 2025-11-08
**Author:** ZiraAI Development Team
**Status:** Ready for Staging Testing
