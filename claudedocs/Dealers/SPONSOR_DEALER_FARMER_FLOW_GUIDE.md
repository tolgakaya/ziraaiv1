# Sponsor → Dealer → Farmer Code Distribution Flow Guide

**Complete API Reference for Multi-Tier Sponsorship System**

---

## Table of Contents

1. [System Overview](#system-overview)
2. [User Roles & Permissions](#user-roles--permissions)
3. [Complete Flow Scenarios](#complete-flow-scenarios)
4. [API Endpoints by Role](#api-endpoints-by-role)
5. [Common Use Cases](#common-use-cases)
6. [Error Handling](#error-handling)

---

## System Overview

### Distribution Hierarchy

```
┌─────────────────────────────────────────────────────────────┐
│                    MAIN SPONSOR                              │
│  • Purchases sponsorship packages                           │
│  • Receives codes (e.g., 100 codes)                         │
│  • Can distribute codes directly to farmers                 │
│  • Can transfer codes to dealers for distribution           │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Transfer Codes
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                    DEALER (Bayi)                             │
│  • Receives codes from main sponsor                         │
│  • Distributes codes to farmers                             │
│  • Tracks own distribution performance                      │
│  • Views analyses from own distributed codes only           │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Distribute Codes
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                    FARMER (Çiftçi)                           │
│  • Receives code via SMS/link from sponsor or dealer        │
│  • Redeems code to activate subscription                    │
│  • Performs plant analyses                                  │
│  • Receives AI-powered recommendations                       │
└─────────────────────────────────────────────────────────────┘
```

### Attribution Chain

Every plant analysis is attributed to:
- **UserId**: Farmer who performed the analysis
- **SponsorCompanyId**: Main sponsor who purchased the package
- **DealerId**: Dealer who distributed the code (if distributed via dealer)
- **ActiveSponsorshipId**: Farmer's active subscription created by code redemption

---

## User Roles & Permissions

### Main Sponsor
**Can:**
- ✅ Purchase sponsorship packages
- ✅ View all own codes (not transferred to dealers)
- ✅ Transfer codes to dealers
- ✅ Invite dealers to network
- ✅ Reclaim codes from dealers (if unsent)
- ✅ Distribute codes directly to farmers
- ✅ View ALL analyses (from own codes + dealer-distributed codes)
- ✅ View dealer performance analytics
- ✅ Message all farmers using own sponsorship

**Cannot:**
- ❌ See codes transferred to dealers in "my codes" list
- ❌ Distribute dealer's codes

### Dealer (Bayi)
**Can:**
- ✅ View codes transferred to them by sponsor
- ✅ Distribute codes to farmers via SMS/link
- ✅ View analyses from farmers who used dealer's codes
- ✅ Message farmers who used dealer's codes
- ✅ Track own distribution performance

**Cannot:**
- ❌ Purchase packages (only sponsors can purchase)
- ❌ Transfer codes to other dealers
- ❌ See sponsor's direct codes
- ❌ See analyses from sponsor's direct distribution

### Hybrid User (Sponsor + Dealer)
**Special Case:** A user who is BOTH sponsor and dealer

**Can:**
- ✅ View ALL analyses where user is sponsor OR dealer
- ✅ Perform both sponsor and dealer operations
- ✅ See combined analytics

**Query Logic:**
```sql
SELECT * FROM PlantAnalyses 
WHERE (SponsorUserId = userId OR DealerId = userId)
```

---

## Complete Flow Scenarios

### Scenario 1: Sponsor → Dealer → Farmer (Standard Flow)

#### Step 1: Sponsor Transfers Codes to Dealer

**Endpoint:** `POST /api/v1/sponsorship/dealer/transfer-codes`  
**User:** Main Sponsor (UserId: 159)  
**Purpose:** Transfer unused codes to dealer for distribution

**Request:**
```http
POST /api/v1/sponsorship/dealer/transfer-codes
Authorization: Bearer {sponsor_token}
Content-Type: application/json
x-dev-arch-version: 1.0

{
  "purchaseId": 26,
  "dealerId": 158,
  "codeCount": 5
}
```

**Response:**
```json
{
  "data": {
    "transferredCodeIds": [932, 933, 934, 935, 936],
    "transferredCount": 5,
    "dealerId": 158,
    "dealerName": "User 1113",
    "transferredAt": "2025-10-26T16:19:26Z"
  },
  "success": true,
  "message": "Successfully transferred 5 codes to dealer."
}
```

**What Happens:**
- 5 codes from Purchase 26 are selected (unsent, active)
- `DealerId` field set to 158
- `TransferredAt` timestamp recorded
- Codes removed from sponsor's "my codes" list
- Codes now visible in dealer's code list

---

#### Step 2: Dealer Views Received Codes

**Endpoint:** `GET /api/v1/sponsorship/codes?page=1&pageSize=10`  
**User:** Dealer (UserId: 158)  
**Purpose:** Check codes received from sponsor

**Request:**
```http
GET /api/v1/sponsorship/codes?page=1&pageSize=10&onlyUnsent=true
Authorization: Bearer {dealer_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
{
  "data": {
    "codes": [
      {
        "id": 932,
        "code": "AGRI-2025-62038F92",
        "sponsorId": 159,
        "dealerId": 158,
        "isUsed": false,
        "isActive": true,
        "distributionDate": null,
        "transferredAt": "2025-10-26T16:19:26Z",
        "expiryDate": "2026-10-26T00:00:00Z",
        "packageTier": "M"
      }
    ],
    "totalCount": 5,
    "currentPage": 1,
    "totalPages": 1
  },
  "success": true
}
```

**Important Fields:**
- `dealerId`: 158 (codes belong to this dealer)
- `sponsorId`: 159 (original sponsor who purchased)
- `distributionDate`: null (not sent to farmer yet)
- `transferredAt`: When sponsor transferred to dealer

---

#### Step 3: Dealer Sends Code to Farmer

**Endpoint:** `POST /api/v1/sponsorship/send-link`  
**User:** Dealer (UserId: 158)  
**Purpose:** Distribute code to farmer via SMS

**Request:**
```http
POST /api/v1/sponsorship/send-link
Authorization: Bearer {dealer_token}
Content-Type: application/json
x-dev-arch-version: 1.0

{
  "code": "AGRI-2025-62038F92",
  "recipientPhone": "05061234567",
  "sendViaSms": true,
  "customMessage": "Merhaba! ZiraAI bitki analizi için kodunuz."
}
```

**Response:**
```json
{
  "data": {
    "code": "AGRI-2025-62038F92",
    "recipientPhone": "05061234567",
    "deepLink": "https://ziraai.com/redeem/AGRI-2025-62038F92",
    "smsSent": true,
    "distributionDate": "2025-10-26T17:30:00Z"
  },
  "success": true,
  "message": "Code sent successfully via SMS"
}
```

**What Happens:**
- SMS sent to farmer's phone: "ZiraAI bitki analizi için kodunuz: https://ziraai.com/redeem/AGRI-2025-62038F92"
- `DistributionDate` field updated in database
- Code marked as "sent" (no longer in "onlyUnsent" list)
- Dealer can track this code as distributed

---

#### Step 4: Farmer Redeems Code

**Endpoint:** `POST /api/v1/sponsorship/redeem`  
**User:** Farmer (UserId: 170)  
**Purpose:** Activate subscription using received code

**Request:**
```http
POST /api/v1/sponsorship/redeem
Authorization: Bearer {farmer_token}
Content-Type: application/json
x-dev-arch-version: 1.0

{
  "code": "AGRI-2025-62038F92"
}
```

**Response:**
```json
{
  "data": {
    "subscriptionId": 170,
    "tier": "M",
    "startDate": "2025-10-26T17:35:00Z",
    "endDate": "2025-11-26T17:35:00Z",
    "dailyLimit": 10,
    "monthlyLimit": 300,
    "isActive": true,
    "sponsorCompanyName": "ZiraAI Sponsor"
  },
  "success": true,
  "message": "Code redeemed successfully. Your subscription is now active."
}
```

**What Happens:**
- New `UserSubscription` created for farmer
- Tier: M (from code's package)
- Duration: 30 days (from package configuration)
- `IsUsed` flag set to true on code
- `RedeemedAt` timestamp recorded
- Farmer can now perform plant analyses

---

#### Step 5: Farmer Performs Plant Analysis

**Endpoint:** `POST /api/v1/PlantAnalyses/async`  
**User:** Farmer (UserId: 170)  
**Purpose:** Analyze plant disease with AI

**Request:**
```http
POST /api/v1/PlantAnalyses/async
Authorization: Bearer {farmer_token}
Content-Type: multipart/form-data
x-dev-arch-version: 1.0

file: [plant_image.jpg]
cropType: "Tomato"
```

**Response:**
```json
{
  "data": {
    "analysisId": 76,
    "status": "Processing",
    "estimatedCompletionTime": "2-5 minutes",
    "message": "Your analysis is being processed. You will receive a notification when complete."
  },
  "success": true
}
```

**What Happens (Background):**
- Analysis request sent to RabbitMQ queue
- Worker service processes analysis (2-5 minutes)
- AI/ML endpoint (N8N) analyzes plant image
- Attribution captured:
  - `UserId`: 170 (farmer)
  - `SponsorCompanyId`: 159 (original sponsor)
  - `DealerId`: 158 (dealer who distributed code)
  - `ActiveSponsorshipId`: 170 (farmer's subscription)
- Analysis status updated to "Completed"
- Farmer receives push notification

---

#### Step 6: Farmer Views Analysis Results

**Endpoint:** `GET /api/v1/PlantAnalyses/{analysisId}`  
**User:** Farmer (UserId: 170)  
**Purpose:** View completed analysis results

**Request:**
```http
GET /api/v1/PlantAnalyses/76
Authorization: Bearer {farmer_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
{
  "data": {
    "id": 76,
    "userId": 170,
    "cropType": "Tomato",
    "imageUrl": "https://freeimage.host/i/abc123",
    "analysisStatus": "Completed",
    "completedAt": "2025-10-26T17:40:00Z",
    "diseaseDetected": "Late Blight",
    "confidence": 92.5,
    "severity": "Moderate",
    "recommendations": [
      "Remove infected leaves immediately",
      "Apply copper-based fungicide",
      "Improve air circulation"
    ],
    "sponsorMetadata": {
      "hasSponsor": true,
      "canMessage": true,
      "canViewLogo": true,
      "sponsorCompanyName": "ZiraAI Sponsor",
      "sponsorLogoUrl": "https://example.com/logo.png"
    }
  },
  "success": true
}
```

**Important Fields:**
- `diseaseDetected`: AI-detected disease name
- `confidence`: AI confidence score (0-100)
- `recommendations`: Actionable treatment steps
- `sponsorMetadata`: Sponsor branding and messaging features

---

#### Step 7: Dealer Views Own Analyses

**Endpoint:** `GET /api/v1/sponsorship/analyses?page=1&pageSize=10`  
**User:** Dealer (UserId: 158)  
**Purpose:** View analyses from farmers using dealer's codes

**Request:**
```http
GET /api/v1/sponsorship/analyses?page=1&pageSize=10
Authorization: Bearer {dealer_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
{
  "data": {
    "analyses": [
      {
        "id": 76,
        "userId": 170,
        "userName": "User 3978",
        "userPhone": "05061234567",
        "cropType": "Tomato",
        "diseaseDetected": "Late Blight",
        "analysisDate": "2025-10-26T17:40:00Z",
        "sponsorCompanyId": 159,
        "dealerId": 158,
        "canMessage": true,
        "canViewLogo": true,
        "unreadMessageCount": 0,
        "lastMessageAt": null
      },
      {
        "id": 75,
        "userId": 170,
        "userName": "User 3978",
        "userPhone": "05061234567",
        "cropType": "Pepper",
        "diseaseDetected": "Bacterial Spot",
        "analysisDate": "2025-10-26T16:50:00Z",
        "sponsorCompanyId": 159,
        "dealerId": 158,
        "canMessage": true,
        "canViewLogo": true,
        "unreadMessageCount": 2,
        "lastMessageAt": "2025-10-26T18:00:00Z"
      }
    ],
    "totalCount": 2,
    "currentPage": 1,
    "totalPages": 1
  },
  "success": true
}
```

**Important:**
- Dealer sees **ONLY** analyses where `dealerId = 158`
- Cannot see sponsor's direct analyses (where `dealerId IS NULL`)
- `canMessage`: true (dealer can message farmers)
- Query uses: `WHERE DealerId = 158`

---

#### Step 8: Sponsor Views ALL Analyses

**Endpoint:** `GET /api/v1/sponsorship/analyses?page=1&pageSize=10`  
**User:** Sponsor (UserId: 159)  
**Purpose:** View all analyses from own sponsorship (direct + dealer-distributed)

**Request:**
```http
GET /api/v1/sponsorship/analyses?page=1&pageSize=10
Authorization: Bearer {sponsor_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
{
  "data": {
    "analyses": [
      {
        "id": 76,
        "userId": 170,
        "userName": "User 3978",
        "cropType": "Tomato",
        "diseaseDetected": "Late Blight",
        "analysisDate": "2025-10-26T17:40:00Z",
        "sponsorCompanyId": 159,
        "dealerId": 158,
        "distributionChannel": "Dealer",
        "canMessage": true
      },
      {
        "id": 75,
        "userId": 170,
        "userName": "User 3978",
        "cropType": "Pepper",
        "diseaseDetected": "Bacterial Spot",
        "analysisDate": "2025-10-26T16:50:00Z",
        "sponsorCompanyId": 159,
        "dealerId": 158,
        "distributionChannel": "Dealer",
        "canMessage": true
      },
      {
        "id": 50,
        "userId": 165,
        "userName": "User 1113",
        "cropType": "Wheat",
        "diseaseDetected": "Rust",
        "analysisDate": "2025-10-25T10:00:00Z",
        "sponsorCompanyId": 159,
        "dealerId": null,
        "distributionChannel": "Direct",
        "canMessage": true
      }
    ],
    "totalCount": 18,
    "currentPage": 1,
    "totalPages": 2
  },
  "success": true
}
```

**Important:**
- Sponsor sees **ALL** analyses: `WHERE (SponsorUserId = 159 OR DealerId = 159)`
- Includes dealer-distributed (dealerId = 158)
- Includes direct distribution (dealerId = null)
- Total: 18 analyses (2 from dealer + 16 direct)

---

### Scenario 2: Sponsor → Farmer (Direct Distribution)

#### Step 1: Sponsor Views Own Codes

**Endpoint:** `GET /api/v1/sponsorship/codes?onlyUnsent=true`  
**User:** Sponsor (UserId: 159)  
**Purpose:** Check available codes for direct distribution

**Request:**
```http
GET /api/v1/sponsorship/codes?page=1&pageSize=10&onlyUnsent=true
Authorization: Bearer {sponsor_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
{
  "data": {
    "codes": [
      {
        "id": 940,
        "code": "AGRI-2025-ABC123",
        "sponsorId": 159,
        "dealerId": null,
        "isUsed": false,
        "distributionDate": null,
        "expiryDate": "2026-10-26T00:00:00Z"
      }
    ],
    "totalCount": 95,
    "currentPage": 1
  },
  "success": true
}
```

**Important:**
- `dealerId: null` (not transferred to dealer)
- Only shows sponsor's direct codes
- Excludes codes transferred to dealers

---

#### Step 2: Sponsor Sends Code Directly to Farmer

**Endpoint:** `POST /api/v1/sponsorship/send-link`  
**User:** Sponsor (UserId: 159)

**Request:**
```http
POST /api/v1/sponsorship/send-link
Authorization: Bearer {sponsor_token}
Content-Type: application/json
x-dev-arch-version: 1.0

{
  "code": "AGRI-2025-ABC123",
  "recipientPhone": "05551234567",
  "sendViaSms": true
}
```

**Response:**
```json
{
  "data": {
    "code": "AGRI-2025-ABC123",
    "recipientPhone": "05551234567",
    "deepLink": "https://ziraai.com/redeem/AGRI-2025-ABC123",
    "smsSent": true,
    "distributionDate": "2025-10-26T18:00:00Z"
  },
  "success": true
}
```

---

#### Step 3: Farmer Redeems & Analyzes

Same as Scenario 1, Steps 4-6

**Attribution Difference:**
- `SponsorCompanyId`: 159 (sponsor)
- `DealerId`: **NULL** (direct distribution)
- `UserId`: 165 (farmer)

---

### Scenario 3: Dealer Management by Sponsor

#### Invite New Dealer

**Endpoint:** `POST /api/v1/sponsorship/dealer/invite`  
**User:** Sponsor (UserId: 159)  
**Purpose:** Invite dealer to join distribution network

**Request:**
```http
POST /api/v1/sponsorship/dealer/invite
Authorization: Bearer {sponsor_token}
Content-Type: application/json
x-dev-arch-version: 1.0

{
  "dealerEmail": "newdealer@example.com",
  "initialCodeCount": 20
}
```

**Response:**
```json
{
  "data": {
    "invitationId": 5,
    "dealerEmail": "newdealer@example.com",
    "status": "Pending",
    "invitedAt": "2025-10-26T19:00:00Z",
    "initialCodeCount": 20
  },
  "success": true,
  "message": "Invitation sent to dealer"
}
```

---

#### View Dealer Invitations

**Endpoint:** `GET /api/v1/sponsorship/dealer/invitations`  
**User:** Sponsor (UserId: 159)

**Request:**
```http
GET /api/v1/sponsorship/dealer/invitations?status=pending&page=1&pageSize=10
Authorization: Bearer {sponsor_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
{
  "data": {
    "invitations": [
      {
        "id": 5,
        "dealerEmail": "newdealer@example.com",
        "dealerName": null,
        "status": "Pending",
        "invitedAt": "2025-10-26T19:00:00Z",
        "acceptedAt": null,
        "initialCodeCount": 20
      },
      {
        "id": 3,
        "dealerEmail": "dealer@example.com",
        "dealerName": "User 1113",
        "dealerId": 158,
        "status": "Accepted",
        "invitedAt": "2025-10-20T10:00:00Z",
        "acceptedAt": "2025-10-21T14:30:00Z",
        "initialCodeCount": 10
      }
    ],
    "totalCount": 2,
    "currentPage": 1
  },
  "success": true
}
```

**Status Values:**
- `Pending`: Invitation sent, awaiting dealer acceptance
- `Accepted`: Dealer accepted invitation
- `Rejected`: Dealer rejected invitation

---

#### Search Dealer by Email

**Endpoint:** `GET /api/v1/sponsorship/dealer/search`  
**User:** Sponsor (UserId: 159)  
**Purpose:** Find dealer for code transfer

**Request:**
```http
GET /api/v1/sponsorship/dealer/search?email=dealer@example.com
Authorization: Bearer {sponsor_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
{
  "data": {
    "userId": 158,
    "email": "dealer@example.com",
    "fullName": "User 1113",
    "phone": "05411111113",
    "isDealer": true,
    "hasReceivedCodes": true,
    "totalCodesReceived": 50,
    "totalCodesDistributed": 45
  },
  "success": true
}
```

---

#### Get Dealer Performance

**Endpoint:** `GET /api/v1/sponsorship/dealer/performance/{dealerId}`  
**User:** Sponsor (UserId: 159)  
**Purpose:** Monitor dealer distribution performance

**Request:**
```http
GET /api/v1/sponsorship/dealer/performance/158
Authorization: Bearer {sponsor_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
{
  "data": {
    "dealerId": 158,
    "dealerName": "User 1113",
    "totalCodesReceived": 50,
    "codesDistributed": 45,
    "codesRemaining": 5,
    "codesRedeemed": 40,
    "redemptionRate": 88.9,
    "activeFarmers": 12,
    "totalAnalyses": 156,
    "distributionVelocity": 3.2,
    "averageTimeToDistribute": "2.5 days",
    "lastDistributionDate": "2025-10-26T17:30:00Z"
  },
  "success": true
}
```

**Key Metrics:**
- `redemptionRate`: % of distributed codes that were redeemed
- `distributionVelocity`: Codes distributed per day
- `activeFarmers`: Unique farmers using dealer's codes
- `totalAnalyses`: Total plant analyses from dealer's farmers

---

#### Get Dealer Summary (All Dealers)

**Endpoint:** `GET /api/v1/sponsorship/dealer/summary`  
**User:** Sponsor (UserId: 159)  
**Purpose:** Overview of entire dealer network

**Request:**
```http
GET /api/v1/sponsorship/dealer/summary
Authorization: Bearer {sponsor_token}
x-dev-arch-version: 1.0
```

**Response:**
```json
{
  "data": {
    "totalDealers": 5,
    "activeDealers": 4,
    "totalCodesTransferred": 250,
    "totalCodesDistributed": 220,
    "totalCodesRedeemed": 200,
    "totalAnalyses": 890,
    "averageRedemptionRate": 87.2,
    "topPerformers": [
      {
        "dealerId": 158,
        "dealerName": "User 1113",
        "codesDistributed": 45,
        "redemptionRate": 88.9,
        "rank": 1
      },
      {
        "dealerId": 160,
        "dealerName": "User 1115",
        "codesDistributed": 60,
        "redemptionRate": 85.0,
        "rank": 2
      }
    ],
    "underperformers": []
  },
  "success": true
}
```

---

#### Reclaim Codes from Dealer

**Endpoint:** `POST /api/v1/sponsorship/dealer/reclaim-codes`  
**User:** Sponsor (UserId: 159)  
**Purpose:** Take back unsent codes from dealer

**Request:**
```http
POST /api/v1/sponsorship/dealer/reclaim-codes
Authorization: Bearer {sponsor_token}
Content-Type: application/json
x-dev-arch-version: 1.0

{
  "dealerId": 158,
  "codeIds": [935, 936]
}
```

**Response:**
```json
{
  "data": {
    "reclaimedCodeIds": [935, 936],
    "reclaimedCount": 2,
    "dealerId": 158,
    "dealerName": "User 1113",
    "reclaimedAt": "2025-10-26T20:00:00Z"
  },
  "success": true,
  "message": "Successfully reclaimed 2 codes from dealer"
}
```

**Validation:**
- Codes must belong to specified dealer (`dealerId = 158`)
- Codes must be unsent (`distributionDate IS NULL`)
- Codes must not be redeemed (`isUsed = false`)

**After Reclaim:**
- `dealerId` set to NULL
- `transferredAt` cleared
- Codes return to sponsor's pool
- Can transfer to another dealer or use directly

---

## API Endpoints by Role

### Sponsor Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/v1/sponsorship/dealer/transfer-codes` | POST | Transfer codes to dealer |
| `/api/v1/sponsorship/dealer/invite` | POST | Invite new dealer |
| `/api/v1/sponsorship/dealer/invitations` | GET | View dealer invitations |
| `/api/v1/sponsorship/dealer/reclaim-codes` | POST | Reclaim codes from dealer |
| `/api/v1/sponsorship/dealer/performance/{id}` | GET | View dealer performance |
| `/api/v1/sponsorship/dealer/summary` | GET | View all dealers summary |
| `/api/v1/sponsorship/dealer/search` | GET | Search dealer by email |
| `/api/v1/sponsorship/codes` | GET | View own codes (not transferred) |
| `/api/v1/sponsorship/send-link` | POST | Send code to farmer directly |
| `/api/v1/sponsorship/analyses` | GET | View ALL analyses (direct + dealer) |

### Dealer Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/v1/sponsorship/codes` | GET | View codes received from sponsor |
| `/api/v1/sponsorship/send-link` | POST | Distribute code to farmer |
| `/api/v1/sponsorship/analyses` | GET | View analyses from own codes only |

### Farmer Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/v1/sponsorship/redeem` | POST | Redeem code to activate subscription |
| `/api/v1/PlantAnalyses/async` | POST | Perform plant analysis |
| `/api/v1/PlantAnalyses/{id}` | GET | View analysis results |
| `/api/v1/PlantAnalyses/list` | GET | View own analysis history |

---

## Common Use Cases

### Use Case 1: Sponsor Monitors Dealer Network

**Goal:** Check which dealers are performing well

**Steps:**
1. `GET /api/v1/sponsorship/dealer/summary` - Overall network health
2. `GET /api/v1/sponsorship/dealer/performance/158` - Specific dealer details
3. Decision: Transfer more codes to high performers OR reclaim from underperformers

---

### Use Case 2: Dealer Daily Distribution

**Goal:** Distribute codes to farmers

**Steps:**
1. `GET /api/v1/sponsorship/codes?onlyUnsent=true` - Check available codes
2. `POST /api/v1/sponsorship/send-link` - Send code to farmer via SMS
3. `GET /api/v1/sponsorship/analyses` - Monitor farmer usage

---

### Use Case 3: Farmer Analysis Journey

**Goal:** Get plant disease diagnosis

**Steps:**
1. Receive SMS with code link
2. `POST /api/v1/sponsorship/redeem` - Activate subscription
3. `POST /api/v1/PlantAnalyses/async` - Upload plant photo
4. Wait 2-5 minutes for AI processing
5. `GET /api/v1/PlantAnalyses/{id}` - View disease diagnosis and recommendations

---

### Use Case 4: Sponsor Redistributes Codes

**Goal:** Move codes from inactive dealer to active dealer

**Steps:**
1. `GET /api/v1/sponsorship/dealer/performance/160` - Check dealer 160 performance (low)
2. `POST /api/v1/sponsorship/dealer/reclaim-codes` - Reclaim 10 codes from dealer 160
3. `POST /api/v1/sponsorship/dealer/transfer-codes` - Transfer 10 codes to dealer 158 (high performer)

---

### Use Case 5: Hybrid User Operations

**Goal:** User who is both sponsor and dealer manages all activities

**Scenario:**
- User 159 purchased package (sponsor role)
- User 159 also receives codes from another sponsor (dealer role)

**Query Behavior:**
```http
GET /api/v1/sponsorship/analyses
Authorization: Bearer {user_159_token}
```

**Returns:**
- Analyses where `SponsorUserId = 159` (as sponsor)
- **AND** analyses where `DealerId = 159` (as dealer)
- Combined total from both roles

---

## Error Handling

### Common Error Responses

#### Insufficient Codes

**Request:**
```json
{
  "purchaseId": 26,
  "dealerId": 158,
  "codeCount": 100
}
```

**Response:**
```json
{
  "data": null,
  "success": false,
  "message": "Insufficient unsent codes. Available: 20, Requested: 100"
}
```

---

#### Invalid Code

**Request:**
```json
{
  "code": "INVALID-CODE"
}
```

**Response:**
```json
{
  "data": null,
  "success": false,
  "message": "Code not found or already used"
}
```

---

#### Unauthorized Access

**Request:**
```http
GET /api/v1/sponsorship/dealer/performance/158
Authorization: Bearer {farmer_token}
```

**Response:**
```json
{
  "data": null,
  "success": false,
  "message": "Authorization denied. Sponsor or Admin role required."
}
```

---

#### Code Already Distributed

**Request:**
```json
{
  "dealerId": 158,
  "codeIds": [932]
}
```

**Response (if code already sent to farmer):**
```json
{
  "data": null,
  "success": false,
  "message": "Cannot reclaim code 932. Already distributed to farmer."
}
```

---

#### Subscription Already Active

**Request:**
```json
{
  "code": "AGRI-2025-ABC123"
}
```

**Response:**
```json
{
  "data": null,
  "success": false,
  "message": "You already have an active subscription. Code redemption not allowed."
}
```

---

## Query Parameters Reference

### Pagination Parameters (All List Endpoints)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | integer | 1 | Page number |
| `pageSize` | integer | 20 | Items per page (max: 100) |

**Example:**
```http
GET /api/v1/sponsorship/analyses?page=2&pageSize=50
```

---

### Code Filtering Parameters

**Endpoint:** `GET /api/v1/sponsorship/codes`

| Parameter | Type | Description |
|-----------|------|-------------|
| `onlyUnsent` | boolean | Show only codes not sent to farmers |
| `onlyActive` | boolean | Show only active (non-expired) codes |
| `status` | string | Filter by status: `available`, `sent`, `redeemed` |

**Example:**
```http
GET /api/v1/sponsorship/codes?onlyUnsent=true&onlyActive=true&page=1&pageSize=10
```

---

### Analysis Filtering Parameters

**Endpoint:** `GET /api/v1/sponsorship/analyses`

| Parameter | Type | Description |
|-----------|------|-------------|
| `sortBy` | string | `date`, `cropType`, `diseaseDetected` |
| `sortOrder` | string | `asc`, `desc` |
| `filterByTier` | string | Filter by subscription tier |
| `filterByCropType` | string | Filter by crop type |
| `startDate` | datetime | Analyses after this date |
| `endDate` | datetime | Analyses before this date |
| `dealerId` | integer | Filter by specific dealer (sponsor only) |
| `hasUnreadMessages` | boolean | Analyses with unread messages |

**Example:**
```http
GET /api/v1/sponsorship/analyses?sortBy=date&sortOrder=desc&filterByCropType=Tomato&hasUnreadMessages=true
```

---

### Invitation Filtering Parameters

**Endpoint:** `GET /api/v1/sponsorship/dealer/invitations`

| Parameter | Type | Description |
|-----------|------|-------------|
| `status` | string | `pending`, `accepted`, `rejected` |

**Example:**
```http
GET /api/v1/sponsorship/dealer/invitations?status=pending
```

---

## Attribution Fields Summary

Every plant analysis contains:

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `userId` | integer | Farmer who performed analysis | 170 |
| `userName` | string | Farmer's name | "User 3978" |
| `sponsorCompanyId` | integer | Main sponsor who purchased package | 159 |
| `dealerId` | integer | Dealer who distributed code (null if direct) | 158 or null |
| `activeSponsorshipId` | integer | Farmer's subscription ID | 170 |

**Distribution Channel Logic:**
```javascript
if (analysis.dealerId === null) {
  distributionChannel = "Direct"; // Sponsor → Farmer
} else {
  distributionChannel = "Dealer"; // Sponsor → Dealer → Farmer
}
```

---

## Important Notes

### Token-Based Role Detection

All endpoints automatically detect user role from JWT token:
- **Sponsor queries:** Use `userId` from token as `SponsorId`
- **Dealer queries:** Use `userId` from token as `DealerId`
- **No manual role parameters needed**

### Query Logic for Analyses

**Sponsor View:**
```sql
WHERE (SponsorUserId = userId OR DealerId = userId)
```

**Dealer View:**
```sql
WHERE DealerId = userId
```

**Farmer View:**
```sql
WHERE UserId = userId
```

### Code States

| State | Description | Actions Allowed |
|-------|-------------|-----------------|
| **Available** | Not sent, not used | Transfer, Distribute, Reclaim |
| **Sent** | Distributed to farmer | Wait for redemption, Track |
| **Redeemed** | Farmer activated subscription | None (final state) |
| **Expired** | Past expiry date | None |

### Messaging Permissions

Users can message farmers if:
- ✅ Sponsor owns the analysis (`sponsorCompanyId = userId`)
- ✅ Dealer distributed the code (`dealerId = userId`)
- ✅ Analysis tier supports messaging (M, L, XL tiers)

---

**Document Version:** 1.0  
**Last Updated:** 2025-10-26  
**Branch:** feature/sponsorship-code-distribution-experiment  
**Status:** ✅ Complete & Tested
