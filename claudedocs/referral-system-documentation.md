# Referral System - Comprehensive Documentation

## Table of Contents
1. [System Overview](#system-overview)
2. [Architecture](#architecture)
3. [Database Schema](#database-schema)
4. [Business Logic Flow](#business-logic-flow)
5. [API Endpoints](#api-endpoints)
6. [Configuration](#configuration)
7. [Integration Points](#integration-points)
8. [Anti-Abuse Mechanisms](#anti-abuse-mechanisms)
9. [Troubleshooting](#troubleshooting)

---

## System Overview

### Purpose
The Referral System incentivizes user acquisition by rewarding existing users (referrers) when they successfully refer new users (referees) who complete the validation criteria.

### Key Features
- ✅ Unique cryptographic referral code generation (`ZIRA-ABC123`)
- ✅ Hybrid delivery (SMS + WhatsApp simultaneously)
- ✅ 4-stage tracking: Clicked → Registered → Validated → Rewarded
- ✅ Configurable rewards (default: 10 credits per successful referral)
- ✅ 30-day link expiry with automatic cleanup
- ✅ Validation gate: 1 analysis required before reward
- ✅ Anti-abuse: self-referral prevention, duplicate detection
- ✅ Unlimited credit accumulation (credits never expire)
- ✅ Priority usage: referral credits → subscription quota

### Business Rules
1. **Code Generation**: Cryptographically secure, format: `ZIRA-{6 alphanumeric chars}`
2. **Allowed Characters**: `ABCDEFGHJKLMNPQRSTUVWXYZ23456789` (excludes confusing chars: 0, O, 1, I, L)
3. **Expiry**: 30 days from code creation
4. **Validation Gate**: Referee must complete 1 plant analysis
5. **Reward Amount**: Configurable via `Referral_CreditsPerReferral` (default: 10)
6. **Self-Referral**: Blocked - users cannot refer themselves
7. **Duplicate Prevention**: Same IP + Device ID within 24h = ignored
8. **Credit Usage**: Referral credits consumed before subscription quota

---

## Architecture

### Layered Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    WebAPI Layer                              │
│  ┌──────────────────┐  ┌─────────────────────────────────┐ │
│  │ ReferralController│  │ Updated Controllers:            │ │
│  │  - 8 Endpoints    │  │  - RegisterUserCommand          │ │
│  └──────────────────┘  │  - PlantAnalysesController      │ │
│                         └─────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                    Business Layer                            │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ CQRS Handlers (MediatR)                              │  │
│  │  Commands: Generate, Disable, TrackClick             │  │
│  │  Queries: GetStats, GetCodes, GetCredits, etc.       │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Services (5 Core Services)                            │  │
│  │  - ReferralCodeService                                │  │
│  │  - ReferralLinkService (Hybrid Delivery)              │  │
│  │  - ReferralTrackingService (4-stage journey)          │  │
│  │  - ReferralRewardService (Credit allocation)          │  │
│  │  - ReferralConfigurationService (Dynamic config)      │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Messaging Integration                                 │  │
│  │  - MessagingServiceFactory → SMS + WhatsApp          │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                   Data Access Layer                          │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ Repositories (EF Core + PostgreSQL)                   │  │
│  │  - ReferralCodeRepository                             │  │
│  │  - ReferralTrackingRepository                         │  │
│  │  - ReferralRewardRepository                           │  │
│  │  - ReferralConfigurationRepository                    │  │
│  │  - UserSubscriptionRepository (updated)               │  │
│  │  - UserRepository (updated)                           │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Component Interaction Flow

```
User Registration with Referral Code:
┌──────┐     ┌────────────┐     ┌──────────────┐     ┌────────┐
│Mobile│────▶│Register    │────▶│Link          │────▶│Tracking│
│ App  │     │UserCommand │     │Registration  │     │Record  │
└──────┘     └────────────┘     │Async()       │     │Created │
                                 └──────────────┘     └────────┘
                                 Status: Clicked → Registered

First Plant Analysis:
┌──────┐     ┌────────────┐     ┌──────────────┐     ┌────────┐
│Mobile│────▶│Analyze     │────▶│Validate      │────▶│Process │
│ App  │     │Plant       │     │ReferralAsync │     │Reward  │
└──────┘     └────────────┘     └──────────────┘     └────────┘
                                 Status: Registered → Validated → Rewarded
                                 Credits: +10 to referrer
```

---

## Database Schema

### Tables Created

#### 1. ReferralCodes
```sql
CREATE TABLE "ReferralCodes" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Code" VARCHAR(20) UNIQUE NOT NULL,
    "IsActive" BOOLEAN DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL,
    "ExpiresAt" TIMESTAMP NOT NULL,
    "Status" INTEGER DEFAULT 0,
    CONSTRAINT "FK_ReferralCodes_Users" 
        FOREIGN KEY ("UserId") REFERENCES "Users"("UserId") ON DELETE CASCADE
);

CREATE INDEX "IX_ReferralCodes_Code" ON "ReferralCodes"("Code");
CREATE INDEX "IX_ReferralCodes_UserId" ON "ReferralCodes"("UserId");
CREATE INDEX "IX_ReferralCodes_Status" ON "ReferralCodes"("Status");
```

**Fields:**
- `Id`: Primary key
- `UserId`: Referrer user ID
- `Code`: Unique referral code (ZIRA-XXXXXX)
- `IsActive`: Manual enable/disable
- `CreatedAt`: Creation timestamp
- `ExpiresAt`: Expiry timestamp (30 days)
- `Status`: 0=Active, 1=Expired, 2=Disabled

---

#### 2. ReferralTracking
```sql
CREATE TABLE "ReferralTracking" (
    "Id" SERIAL PRIMARY KEY,
    "ReferralCodeId" INTEGER NOT NULL,
    "RefereeUserId" INTEGER,
    "IpAddress" VARCHAR(50),
    "DeviceId" VARCHAR(255),
    "ClickedAt" TIMESTAMP NOT NULL,
    "RegisteredAt" TIMESTAMP,
    "ValidatedAt" TIMESTAMP,
    "RewardProcessedAt" TIMESTAMP,
    "Status" INTEGER DEFAULT 0,
    CONSTRAINT "FK_ReferralTracking_ReferralCodes" 
        FOREIGN KEY ("ReferralCodeId") REFERENCES "ReferralCodes"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ReferralTracking_ReferralCodeId" ON "ReferralTracking"("ReferralCodeId");
CREATE INDEX "IX_ReferralTracking_RefereeUserId" ON "ReferralTracking"("RefereeUserId");
CREATE INDEX "IX_ReferralTracking_Status" ON "ReferralTracking"("Status");
```

**Status Enum:**
- `0` = Clicked (link opened, not registered yet)
- `1` = Registered (user signed up)
- `2` = Validated (completed 1 analysis)
- `3` = Rewarded (credits awarded)

**Journey Tracking:**
- `ClickedAt`: Initial link click timestamp
- `RegisteredAt`: Registration completion timestamp
- `ValidatedAt`: First analysis completion timestamp
- `RewardProcessedAt`: Credit award timestamp

---

#### 3. ReferralRewards
```sql
CREATE TABLE "ReferralRewards" (
    "Id" SERIAL PRIMARY KEY,
    "ReferralTrackingId" INTEGER NOT NULL,
    "ReferrerUserId" INTEGER NOT NULL,
    "RefereeUserId" INTEGER NOT NULL,
    "CreditAmount" INTEGER NOT NULL,
    "AwardedAt" TIMESTAMP NOT NULL,
    "SubscriptionId" INTEGER NOT NULL,
    "ExpiresAt" TIMESTAMP,
    CONSTRAINT "FK_ReferralRewards_ReferralTracking" 
        FOREIGN KEY ("ReferralTrackingId") REFERENCES "ReferralTracking"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ReferralRewards_Subscriptions" 
        FOREIGN KEY ("SubscriptionId") REFERENCES "UserSubscriptions"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ReferralRewards_ReferrerUserId" ON "ReferralRewards"("ReferrerUserId");
CREATE INDEX "IX_ReferralRewards_RefereeUserId" ON "ReferralRewards"("RefereeUserId");
```

**Fields:**
- `CreditAmount`: Credits awarded (default: 10)
- `ExpiresAt`: NULL (credits never expire per requirements)
- `SubscriptionId`: Which subscription received credits

---

#### 4. ReferralConfigurations
```sql
CREATE TABLE "ReferralConfigurations" (
    "Id" SERIAL PRIMARY KEY,
    "ConfigKey" VARCHAR(100) UNIQUE NOT NULL,
    "ConfigValue" TEXT NOT NULL,
    "Description" TEXT,
    "UpdatedAt" TIMESTAMP NOT NULL
);

-- Default configurations
INSERT INTO "ReferralConfigurations" ("ConfigKey", "ConfigValue", "Description", "UpdatedAt") VALUES
('Referral_Enabled', 'true', 'Enable/disable entire referral system', NOW()),
('Referral_CreditsPerReferral', '10', 'Credits awarded per successful referral', NOW()),
('Referral_LinkExpiryDays', '30', 'Referral link expiry in days', NOW()),
('Referral_ValidationRequired', 'true', 'Require validation before reward', NOW()),
('Referral_MinAnalysesForValidation', '1', 'Minimum analyses for validation', NOW()),
('Referral_SMS_Enabled', 'true', 'Enable SMS delivery', NOW()),
('Referral_WhatsApp_Enabled', 'true', 'Enable WhatsApp delivery', NOW()),
('Referral_DeepLinkTemplate', 'ziraai://referral?code={CODE}', 'Deep link URL template', NOW());
```

---

#### 5. Modified Tables

**Users Table:**
```sql
ALTER TABLE "Users" 
ADD COLUMN "RegistrationReferralCode" VARCHAR(20);
```

**UserSubscriptions Table:**
```sql
ALTER TABLE "UserSubscriptions" 
ADD COLUMN "ReferralCredits" INTEGER DEFAULT 0;
```

---

## Business Logic Flow

### 1. Referral Link Generation & Sending

**Endpoint:** `POST /api/referral/generate`

**Flow:**
```
1. User requests referral link generation
   ├─ Validate: delivery method (1=SMS, 2=WhatsApp, 3=Both)
   └─ Validate: phone numbers (max 50, Turkish format)

2. ReferralCodeService.GenerateUniqueCodeAsync()
   ├─ Generate cryptographically secure code
   ├─ Format: ZIRA-{6 chars from allowed set}
   ├─ Check uniqueness in database
   └─ Retry if collision (max 10 attempts)

3. ReferralCodeService.CreateCodeAsync()
   ├─ Insert to ReferralCodes table
   ├─ Set UserId = current user
   ├─ Set CreatedAt = now
   ├─ Set ExpiresAt = now + 30 days
   └─ Set Status = 0 (Active)

4. ReferralLinkService.GenerateAndSendLinksAsync()
   ├─ Generate deep link: ziraai://referral?code={CODE}
   ├─ Generate Play Store link with referral parameter
   ├─ Format message with links
   └─ For each phone number:
       ├─ If SMS enabled → MessagingServiceFactory.SendSmsAsync()
       ├─ If WhatsApp enabled → MessagingServiceFactory.SendWhatsAppAsync()
       └─ Track delivery status (Sent/Failed/Pending)

5. Return ReferralLinkResponse
   ├─ ReferralCode: ZIRA-ABC123
   ├─ DeepLink: ziraai://referral?code=ZIRA-ABC123
   ├─ PlayStoreLink: https://play.google.com/store/...
   ├─ ExpiresAt: 2025-11-02T10:30:00
   └─ DeliveryStatuses: [{ Phone, Method, Status, Error }]
```

**Code Example:**
```csharp
// ReferralCodeService.cs
private async Task<string> GenerateUniqueCodeAsync()
{
    const string allowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    const int codeLength = 6;
    
    for (int attempt = 0; attempt < 10; attempt++)
    {
        var code = "ZIRA-" + GenerateRandomString(codeLength, allowedChars);
        
        var exists = await _codeRepository.GetAsync(c => c.Code == code);
        if (exists == null)
            return code;
    }
    
    throw new Exception("Failed to generate unique code");
}

private string GenerateRandomString(int length, string allowedChars)
{
    using var rng = RandomNumberGenerator.Create();
    var result = new char[length];
    var buffer = new byte[length];
    
    rng.GetBytes(buffer);
    
    for (int i = 0; i < length; i++)
    {
        result[i] = allowedChars[buffer[i] % allowedChars.Length];
    }
    
    return new string(result);
}
```

---

### 2. Click Tracking (Public Endpoint)

**Endpoint:** `POST /api/referral/track-click`

**Flow:**
```
1. Mobile app opens referral link
   ├─ Deep link: ziraai://referral?code=ZIRA-ABC123
   └─ Extract code from URL parameter

2. App calls /api/referral/track-click (no auth required)
   ├─ Request: { Code, IpAddress, DeviceId }
   └─ Server extracts real IP from headers (X-Forwarded-For)

3. ReferralTrackingService.TrackClickAsync()
   ├─ Validate code exists and is active
   ├─ Check expiry (now <= ExpiresAt)
   ├─ Anti-abuse check:
   │   ├─ Find recent clicks (same IP + Device within 24h)
   │   └─ If duplicate → return success but don't track
   ├─ Create ReferralTracking record:
   │   ├─ ReferralCodeId = code.Id
   │   ├─ IpAddress = request.IpAddress
   │   ├─ DeviceId = request.DeviceId
   │   ├─ ClickedAt = DateTime.Now
   │   └─ Status = 0 (Clicked)
   └─ Return success

4. App navigates to registration screen
```

**Anti-Abuse Logic:**
```csharp
// ReferralTrackingService.cs - TrackClickAsync()
var cutoffTime = DateTime.Now.AddHours(-24);
var duplicateClick = await _trackingRepository.GetAsync(t =>
    t.ReferralCodeId == referralCode.Id &&
    t.IpAddress == ipAddress &&
    t.DeviceId == deviceId &&
    t.ClickedAt > cutoffTime);

if (duplicateClick != null)
{
    _logger.LogInformation("Duplicate click detected - ignoring");
    return new SuccessResult("Click tracked"); // Silent success
}
```

---

### 3. Registration Linking

**Endpoint:** `POST /api/auth/register` (with optional referralCode field)

**Flow:**
```
1. User completes registration form
   ├─ Email, Password, FullName, MobilePhone
   └─ ReferralCode (optional): ZIRA-ABC123

2. RegisterUserCommand.Handle()
   ├─ Create User record
   ├─ Save RegistrationReferralCode to Users table
   ├─ Assign Farmer role
   ├─ Create Trial subscription
   └─ If ReferralCode provided:
       └─ Call ReferralTrackingService.LinkRegistrationAsync()

3. LinkRegistrationAsync(userId, code)
   ├─ Find tracking record:
   │   ├─ Where ReferralCodeId matches code
   │   ├─ Where Status = Clicked
   │   └─ Where RefereeUserId is NULL (not yet linked)
   ├─ Self-referral check:
   │   ├─ Get referral code owner
   │   └─ If owner.UserId == userId → Error
   ├─ Update tracking record:
   │   ├─ Set RefereeUserId = userId
   │   ├─ Set RegisteredAt = DateTime.Now
   │   └─ Set Status = 1 (Registered)
   └─ Return success
```

**Self-Referral Prevention:**
```csharp
// ReferralTrackingService.cs - LinkRegistrationAsync()
var referralCode = await _codeRepository.GetAsync(c => c.Code == code);
if (referralCode.UserId == userId)
{
    _logger.LogWarning("Self-referral attempt blocked");
    return new ErrorResult("Cannot refer yourself");
}
```

---

### 4. Validation & Reward (Automatic)

**Trigger:** First plant analysis completion

**Endpoint:** `POST /api/v1/plantanalyses/analyze`

**Flow:**
```
1. User completes first plant analysis
   └─ PlantAnalysesController.Analyze() succeeds

2. After IncrementUsageAsync():
   └─ Call ReferralTrackingService.ValidateReferralAsync(userId)

3. ValidateReferralAsync(userId)
   ├─ Find tracking record:
   │   ├─ Where RefereeUserId = userId
   │   └─ Where Status = Registered
   ├─ If not found → return (user not referred)
   ├─ Check if already validated → skip
   ├─ Get configuration: MinAnalysesForValidation (default: 1)
   ├─ Count user's analyses:
   │   └─ If count >= required → proceed
   ├─ Update tracking:
   │   ├─ Set ValidatedAt = DateTime.Now
   │   └─ Set Status = 2 (Validated)
   └─ Trigger reward: ProcessRewardAsync(trackingId)

4. ProcessRewardAsync(trackingId)
   ├─ Get tracking record
   ├─ Validate Status = Validated
   ├─ Check if already rewarded → skip
   ├─ Get referral code to find referrer
   ├─ Get config: CreditsPerReferral (default: 10)
   ├─ Get or create active subscription for referrer
   ├─ Add credits to subscription:
   │   └─ subscription.ReferralCredits += creditAmount
   ├─ Create ReferralReward record
   ├─ Update tracking:
   │   ├─ Set RewardProcessedAt = DateTime.Now
   │   └─ Set Status = 3 (Rewarded)
   └─ Return success
```

**Validation Logic:**
```csharp
// ReferralTrackingService.cs - ValidateReferralAsync()
public async Task<IResult> ValidateReferralAsync(int userId)
{
    // Find referral tracking for this user
    var tracking = await _trackingRepository.GetAsync(t => 
        t.RefereeUserId == userId && 
        t.Status == (int)ReferralTrackingStatus.Registered);
    
    if (tracking == null)
        return new SuccessResult("No active referral"); // Silent success
    
    // Check analysis count
    var minAnalyses = await _configService.GetMinAnalysesForValidationAsync();
    var analysisCount = await _plantAnalysisRepository.GetCountAsync(
        p => p.UserId == userId);
    
    if (analysisCount < minAnalyses)
        return new SuccessResult("Validation criteria not met yet");
    
    // Mark as validated
    tracking.ValidatedAt = DateTime.Now;
    tracking.Status = (int)ReferralTrackingStatus.Validated;
    _trackingRepository.Update(tracking);
    await _trackingRepository.SaveChangesAsync();
    
    // Process reward asynchronously
    await _rewardService.ProcessRewardAsync(tracking.Id);
    
    return new SuccessResult("Referral validated and reward processed");
}
```

---

## API Endpoints

### 1. Generate Referral Link
**POST** `/api/referral/generate`

**Auth:** Required (JWT)

**Request:**
```json
{
  "deliveryMethod": 3,
  "phoneNumbers": ["05321234567", "05339876543"],
  "customMessage": "ZiraAI'yi dene ve bitkilerini analiz et!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Referral links generated and sent successfully",
  "data": {
    "referralCode": "ZIRA-A3B7K9",
    "deepLink": "ziraai://referral?code=ZIRA-A3B7K9",
    "playStoreLink": "https://play.google.com/store/apps/details?id=com.ziraai&referrer=ZIRA-A3B7K9",
    "expiresAt": "2025-11-02T10:30:00",
    "deliveryStatuses": [
      {
        "phoneNumber": "05321234567",
        "method": "SMS",
        "status": "Sent",
        "errorMessage": null
      },
      {
        "phoneNumber": "05321234567",
        "method": "WhatsApp",
        "status": "Sent",
        "errorMessage": null
      }
    ]
  }
}
```

---

### 2. Track Referral Click
**POST** `/api/referral/track-click`

**Auth:** Public (no auth required)

**Request:**
```json
{
  "code": "ZIRA-A3B7K9",
  "ipAddress": "192.168.1.100",
  "deviceId": "unique-device-uuid"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Click tracked successfully"
}
```

---

### 3. Validate Referral Code
**POST** `/api/referral/validate`

**Auth:** Public (no auth required)

**Request:**
```json
{
  "code": "ZIRA-A3B7K9"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Referral code is valid and active"
}
```

**Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Referral code has expired"
}
```

---

### 4. Get Referral Statistics
**GET** `/api/referral/stats`

**Auth:** Required (JWT)

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "totalReferrals": 15,
    "successfulReferrals": 8,
    "pendingReferrals": 3,
    "totalCreditsEarned": 80,
    "referralBreakdown": {
      "clicked": 15,
      "registered": 11,
      "validated": 9,
      "rewarded": 8
    }
  }
}
```

---

### 5. Get User Referral Codes
**GET** `/api/referral/codes`

**Auth:** Required (JWT)

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "code": "ZIRA-A3B7K9",
      "isActive": true,
      "createdAt": "2025-10-03T10:30:00",
      "expiresAt": "2025-11-02T10:30:00",
      "status": 0,
      "statusText": "Active",
      "usageCount": 5
    }
  ]
}
```

---

### 6. Get Credit Breakdown
**GET** `/api/referral/credits`

**Auth:** Required (JWT)

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "totalEarned": 80,
    "totalUsed": 25,
    "currentBalance": 55
  }
}
```

---

### 7. Get Reward History
**GET** `/api/referral/rewards`

**Auth:** Required (JWT)

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "refereeUserName": "Ahmet Yılmaz",
      "creditAmount": 10,
      "awardedAt": "2025-10-03T15:45:00"
    }
  ]
}
```

---

### 8. Disable Referral Code
**DELETE** `/api/referral/disable/{code}`

**Auth:** Required (JWT)

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Referral code disabled successfully"
}
```

---

## Configuration

### Database Configuration (ReferralConfigurations Table)

| Config Key | Default Value | Description |
|-----------|---------------|-------------|
| `Referral_Enabled` | `true` | Enable/disable entire referral system |
| `Referral_CreditsPerReferral` | `10` | Credits awarded per successful referral |
| `Referral_LinkExpiryDays` | `30` | Referral link expiry in days |
| `Referral_ValidationRequired` | `true` | Require validation before reward |
| `Referral_MinAnalysesForValidation` | `1` | Minimum analyses for validation |
| `Referral_SMS_Enabled` | `true` | Enable SMS delivery |
| `Referral_WhatsApp_Enabled` | `true` | Enable WhatsApp delivery |
| `Referral_DeepLinkTemplate` | `ziraai://referral?code={CODE}` | Deep link URL template |

### Updating Configuration

```sql
-- Increase credits per referral
UPDATE "ReferralConfigurations" 
SET "ConfigValue" = '15', "UpdatedAt" = NOW()
WHERE "ConfigKey" = 'Referral_CreditsPerReferral';

-- Extend expiry period
UPDATE "ReferralConfigurations" 
SET "ConfigValue" = '60', "UpdatedAt" = NOW()
WHERE "ConfigKey" = 'Referral_LinkExpiryDays';

-- Require 3 analyses for validation
UPDATE "ReferralConfigurations" 
SET "ConfigValue" = '3', "UpdatedAt" = NOW()
WHERE "ConfigKey" = 'Referral_MinAnalysesForValidation';
```

**Note:** Configuration cache TTL is 15 minutes. Changes take effect within 15 minutes.

---

## Integration Points

### 1. User Registration
**File:** `Business/Handlers/Authorizations/Commands/RegisterUserCommand.cs`

**Integration:**
```csharp
// Add ReferralCode property to command
public string ReferralCode { get; set; }

// Store in user record
user.RegistrationReferralCode = request.ReferralCode;

// Link registration after user creation
if (!string.IsNullOrWhiteSpace(request.ReferralCode))
{
    await _referralTrackingService.LinkRegistrationAsync(
        user.UserId, 
        request.ReferralCode);
}
```

---

### 2. Plant Analysis (Validation Trigger)
**File:** `WebAPI/Controllers/PlantAnalysesController.cs`

**Integration:**
```csharp
// After successful analysis and usage increment
if (result.Success)
{
    await _subscriptionValidationService.IncrementUsageAsync(userId.Value, result.Data?.Id);
    
    // Process referral validation
    try
    {
        var validationResult = await _referralTrackingService.ValidateReferralAsync(userId.Value);
        if (validationResult.Success)
        {
            _logger.LogInformation("Referral validation processed for UserId: {UserId}", userId.Value);
        }
    }
    catch (Exception refEx)
    {
        // Log but don't fail the analysis
        _logger.LogWarning(refEx, "Referral validation failed for UserId: {UserId}", userId.Value);
    }
    
    return Ok(result);
}
```

---

### 3. Credit Usage in Subscription
**File:** `Business/Services/Subscription/SubscriptionValidationService.cs`

**Integration:**
```csharp
// Priority: Referral credits → Subscription quota
var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);

if (subscription.ReferralCredits > 0)
{
    // Use referral credit
    await _referralRewardService.DeductReferralCreditAsync(userId);
    _logger.LogInformation("Used referral credit for user {UserId}", userId);
}
else
{
    // Use subscription quota
    subscription.CurrentDailyUsage++;
    subscription.CurrentMonthlyUsage++;
}
```

---

## Anti-Abuse Mechanisms

### 1. Self-Referral Prevention
**Location:** `ReferralTrackingService.LinkRegistrationAsync()`

```csharp
var referralCode = await _codeRepository.GetAsync(c => c.Code == code);
if (referralCode.UserId == userId)
{
    return new ErrorResult("Cannot refer yourself");
}
```

---

### 2. Duplicate Click Detection
**Location:** `ReferralTrackingService.TrackClickAsync()`

**Logic:**
- Same IP + Device ID within 24 hours = ignored
- Returns success but doesn't create tracking record

```csharp
var cutoffTime = DateTime.Now.AddHours(-24);
var duplicateClick = await _trackingRepository.GetAsync(t =>
    t.ReferralCodeId == referralCode.Id &&
    t.IpAddress == ipAddress &&
    t.DeviceId == deviceId &&
    t.ClickedAt > cutoffTime);

if (duplicateClick != null)
{
    return new SuccessResult("Click tracked"); // Silent success
}
```

---

### 3. Code Expiry
**Location:** `ReferralCodeService.ValidateCodeAsync()`

**Logic:**
- Check ExpiresAt field
- Automatic status update to Expired

```csharp
if (DateTime.Now > referralCode.ExpiresAt)
{
    referralCode.Status = (int)ReferralCodeStatus.Expired;
    _codeRepository.Update(referralCode);
    await _codeRepository.SaveChangesAsync();
    
    return new ErrorResult("Referral code has expired");
}
```

---

### 4. Double Reward Prevention
**Location:** `ReferralRewardService.ProcessRewardAsync()`

**Logic:**
- Check if reward already exists for tracking ID

```csharp
var existingReward = await _rewardRepository.GetByTrackingIdAsync(trackingId);
if (existingReward != null)
{
    return new SuccessResult("Reward already processed");
}
```

---

## Troubleshooting

### Issue: Referral codes not being generated

**Symptoms:**
- 500 error on `/api/referral/generate`
- Logs: "Failed to generate unique code"

**Possible Causes:**
1. Database connection issue
2. All possible codes exhausted (unlikely with 36^6 combinations)
3. Unique constraint violation loop

**Solution:**
```sql
-- Check existing codes count
SELECT COUNT(*) FROM "ReferralCodes";

-- Check for orphaned codes
SELECT * FROM "ReferralCodes" 
WHERE "ExpiresAt" < NOW() AND "Status" = 0;

-- Clean up expired codes
UPDATE "ReferralCodes" 
SET "Status" = 1 
WHERE "ExpiresAt" < NOW() AND "Status" = 0;
```

---

### Issue: SMS/WhatsApp not being sent

**Symptoms:**
- DeliveryStatuses show "Failed"
- Logs: "SMS/WhatsApp service error"

**Possible Causes:**
1. MessagingServiceFactory not configured
2. SMS/WhatsApp service credentials missing
3. Phone number format invalid

**Solution:**
```csharp
// Check configuration
var smsEnabled = await _configService.GetSmsEnabledAsync();
var whatsappEnabled = await _configService.GetWhatsAppEnabledAsync();

// Validate phone format (Turkish)
var phoneRegex = new Regex(@"^0[0-9]{9,10}$");
if (!phoneRegex.IsMatch(phoneNumber))
{
    _logger.LogWarning("Invalid phone format: {Phone}", phoneNumber);
}
```

---

### Issue: Rewards not being processed

**Symptoms:**
- Status stuck at "Validated"
- Credits not added to referrer

**Possible Causes:**
1. ProcessRewardAsync() exception
2. Subscription not found
3. Transaction rollback

**Debugging:**
```sql
-- Check stuck validations
SELECT * FROM "ReferralTracking" 
WHERE "Status" = 2 AND "RewardProcessedAt" IS NULL;

-- Check referrer subscriptions
SELECT u."UserId", u."Email", us."Id" AS SubscriptionId
FROM "ReferralTracking" rt
JOIN "ReferralCodes" rc ON rt."ReferralCodeId" = rc."Id"
JOIN "Users" u ON rc."UserId" = u."UserId"
LEFT JOIN "UserSubscriptions" us ON u."UserId" = us."UserId" AND us."IsActive" = true
WHERE rt."Status" = 2 AND rt."RewardProcessedAt" IS NULL;
```

**Fix:**
```sql
-- Manually trigger reward processing
UPDATE "ReferralTracking" 
SET "Status" = 1 
WHERE "Id" = <tracking_id>;

-- Then re-run validation
```

---

### Issue: Duplicate click not being detected

**Symptoms:**
- Multiple tracking records with same IP + Device

**Possible Causes:**
1. 24-hour window expired
2. IP/Device ID extraction failing
3. Index not being used

**Solution:**
```sql
-- Check for duplicates
SELECT "IpAddress", "DeviceId", COUNT(*) 
FROM "ReferralTracking" 
WHERE "ReferralCodeId" = <code_id>
GROUP BY "IpAddress", "DeviceId" 
HAVING COUNT(*) > 1;

-- Verify index exists
SELECT * FROM pg_indexes 
WHERE tablename = 'ReferralTracking';
```

---

## Performance Considerations

### Database Indexes
All critical queries are indexed:
- `ReferralCodes`: Code, UserId, Status
- `ReferralTracking`: ReferralCodeId, RefereeUserId, Status
- `ReferralRewards`: ReferrerUserId, RefereeUserId

### Caching Strategy
- Configuration: 15-minute memory cache
- Stats queries: 10-second cache (CacheAspect)
- User codes: 5-second cache

### Query Optimization
```csharp
// Good: Use indexed fields
var tracking = await _trackingRepository.GetAsync(t => 
    t.RefereeUserId == userId && 
    t.Status == (int)ReferralTrackingStatus.Registered);

// Bad: Full table scan
var tracking = await _trackingRepository.GetAllAsync();
var filtered = tracking.Where(t => t.RefereeUserId == userId);
```

---

## Security Considerations

1. **Rate Limiting:** Consider adding rate limits to public endpoints (track-click, validate)
2. **IP Validation:** Validate IP address format before storage
3. **Code Format:** Enforce ZIRA-XXXXXX format in validators
4. **Authorization:** All authenticated endpoints use SecuredOperation aspect
5. **SQL Injection:** Using parameterized queries via EF Core

---

## Monitoring & Analytics

### Key Metrics to Track

1. **Conversion Funnel:**
   - Clicks → Registrations: %
   - Registrations → Validations: %
   - Validations → Rewards: %

2. **Code Performance:**
   - Average clicks per code
   - Average conversion rate per code
   - Top performing codes

3. **Credit Economy:**
   - Total credits issued
   - Total credits consumed
   - Average credits per user

### SQL Queries for Analytics

```sql
-- Conversion funnel
SELECT 
    COUNT(CASE WHEN "Status" >= 0 THEN 1 END) AS Clicks,
    COUNT(CASE WHEN "Status" >= 1 THEN 1 END) AS Registrations,
    COUNT(CASE WHEN "Status" >= 2 THEN 1 END) AS Validations,
    COUNT(CASE WHEN "Status" >= 3 THEN 1 END) AS Rewards
FROM "ReferralTracking";

-- Top referrers
SELECT 
    u."Email",
    u."FullName",
    COUNT(rr."Id") AS TotalRewards,
    SUM(rr."CreditAmount") AS TotalCredits
FROM "ReferralRewards" rr
JOIN "Users" u ON rr."ReferrerUserId" = u."UserId"
GROUP BY u."UserId", u."Email", u."FullName"
ORDER BY TotalCredits DESC
LIMIT 10;

-- Active codes performance
SELECT 
    rc."Code",
    u."Email" AS Owner,
    COUNT(rt."Id") AS Clicks,
    COUNT(CASE WHEN rt."Status" >= 1 THEN 1 END) AS Conversions,
    ROUND(COUNT(CASE WHEN rt."Status" >= 1 THEN 1 END)::NUMERIC / NULLIF(COUNT(rt."Id"), 0) * 100, 2) AS ConversionRate
FROM "ReferralCodes" rc
JOIN "Users" u ON rc."UserId" = u."UserId"
LEFT JOIN "ReferralTracking" rt ON rc."Id" = rt."ReferralCodeId"
WHERE rc."IsActive" = true AND rc."ExpiresAt" > NOW()
GROUP BY rc."Id", rc."Code", u."Email"
ORDER BY ConversionRate DESC;
```

---

## Future Enhancements

1. **Tiered Rewards:** Different credit amounts based on referee's subscription tier
2. **Bonus Campaigns:** Limited-time double credits
3. **Leaderboards:** Gamification with top referrer rankings
4. **Email Delivery:** Add email as delivery method
5. **Custom Landing Pages:** Personalized referral landing pages
6. **A/B Testing:** Test different message templates
7. **Referral Challenges:** "Refer 5 friends, get bonus 20 credits"
8. **Social Sharing:** One-click share to social media

---

**Last Updated:** 2025-10-03  
**Version:** 1.0.0  
**Author:** Claude Code + ZiraAI Team
