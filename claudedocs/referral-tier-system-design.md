# Referral Tier System - Technical Design & Implementation Plan

**Document Version**: 1.0
**Created**: 2025-10-02
**Project**: ZiraAI Referral System
**Feature Branch**: `feature/referrer-tier-system`

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [System Overview](#2-system-overview)
3. [Architecture Design](#3-architecture-design)
4. [Database Schema](#4-database-schema)
5. [API Specifications](#5-api-specifications)
6. [Deep Linking Architecture](#6-deep-linking-architecture)
7. [Configuration System](#7-configuration-system)
8. [Business Logic](#8-business-logic)
9. [Integration Points](#9-integration-points)
10. [Implementation Roadmap](#10-implementation-roadmap)
11. [Testing Strategy](#11-testing-strategy)
12. [Security Considerations](#12-security-considerations)
13. [Deployment Guide](#13-deployment-guide)

---

## 1. Executive Summary

### Purpose
Implement a viral referral system that rewards farmers for bringing new users to the ZiraAI platform through configurable analysis credit incentives.

### Key Features
- 📱 **Hybrid Delivery**: SMS and WhatsApp link sharing
- 🎁 **Configurable Rewards**: Admin-controlled credit amounts per referral
- 🔗 **Deep Linking**: Play Store → App → Registration with referral code
- ⏰ **Time-Limited Links**: 30-day expiry for referral links
- 🛡️ **Anti-Abuse**: New user must complete 1 analysis for referral to count
- 📊 **Usage Visibility**: Referral-earned credits shown in usage endpoints

### Business Goals
- Viral growth through farmer network effects
- Reduced user acquisition cost
- Increased platform engagement
- Natural market expansion through trusted referrals

---

## 2. System Overview

### User Journey Flow

```
Referrer (Farmer A)
    ↓
[Generate Referral Link]
    ↓
[Send via SMS/WhatsApp]
    ↓
Referee (Farmer B) clicks link
    ↓
[Play Store Opens]
    ↓
[User installs app]
    ↓
[App opens with referral code]
    ↓
[User registers with phone + referral code]
    ↓
[User completes 1st analysis] ← Validation Gate
    ↓
[Referrer receives configurable credits (e.g., +10)]
    ↓
[Credits visible in usage endpoints]
```

### Key Components

1. **Referral Code Service**: Generate unique, secure codes
2. **Link Generator Service**: Create time-limited deep links
3. **Tracking Service**: Monitor clicks, registrations, validations
4. **Reward Distribution Engine**: Credit allocation after validation
5. **Configuration Service**: Admin-controlled reward amounts
6. **SMS/WhatsApp Delivery**: Message sending infrastructure
7. **Deep Link Handler**: Mobile app integration

---

## 3. Architecture Design

### Component Diagram

```
┌─────────────────────────────────────────────────────────┐
│                     WebAPI Layer                         │
├─────────────────────────────────────────────────────────┤
│  ReferralController                                      │
│  - GenerateReferralLink()                               │
│  - GetReferralStats()                                   │
│  - TrackReferralClick()                                 │
└─────────────────┬───────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────┐
│                  Business Layer                          │
├─────────────────────────────────────────────────────────┤
│  Commands:                                               │
│  - GenerateReferralLinkCommand                          │
│  - ValidateReferralCommand                              │
│  - ProcessReferralRewardCommand                         │
│                                                          │
│  Queries:                                                │
│  - GetUserReferralStatsQuery                            │
│  - GetReferralDetailsQuery                              │
│                                                          │
│  Services:                                               │
│  - ReferralCodeService                                  │
│  - ReferralLinkService                                  │
│  - ReferralRewardService                                │
│  - ReferralTrackingService                              │
└─────────────────┬───────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────┐
│                 DataAccess Layer                         │
├─────────────────────────────────────────────────────────┤
│  Repositories:                                           │
│  - IReferralCodeRepository                              │
│  - IReferralTrackingRepository                          │
│  - IReferralRewardRepository                            │
│  - IReferralConfigurationRepository                     │
└─────────────────┬───────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────┐
│                PostgreSQL Database                       │
├─────────────────────────────────────────────────────────┤
│  Tables:                                                 │
│  - ReferralCodes                                        │
│  - ReferralTracking                                     │
│  - ReferralRewards                                      │
│  - ReferralConfigurations                               │
└─────────────────────────────────────────────────────────┘

External Integrations:
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│   SMS Service    │  │  WhatsApp API    │  │   Play Store     │
└──────────────────┘  └──────────────────┘  └──────────────────┘
```

### Integration Points

1. **Phone Authentication System**: Registration with referral code
2. **Subscription System**: Add referral credits to usage quota
3. **Plant Analysis Service**: Track completion for validation
4. **Configuration Service**: Dynamic reward amounts
5. **SMS/WhatsApp Services**: Link delivery

---

## 4. Database Schema

### 4.1 ReferralCodes Table

```sql
CREATE TABLE public."ReferralCodes" (
    "Id" INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "UserId" INTEGER NOT NULL,  -- Referrer (FK to Users)
    "Code" VARCHAR(20) NOT NULL UNIQUE,  -- e.g., "ZIRA-ABC123"
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" TIMESTAMP NOT NULL,  -- CreatedAt + 30 days
    "Status" INTEGER NOT NULL DEFAULT 0,  -- 0=Active, 1=Expired, 2=Disabled

    CONSTRAINT "FK_ReferralCodes_Users"
        FOREIGN KEY ("UserId") REFERENCES public."Users"("UserId") ON DELETE CASCADE
);

CREATE INDEX "IX_ReferralCodes_UserId" ON public."ReferralCodes"("UserId");
CREATE INDEX "IX_ReferralCodes_Code" ON public."ReferralCodes"("Code");
CREATE INDEX "IX_ReferralCodes_ExpiresAt" ON public."ReferralCodes"("ExpiresAt");
CREATE INDEX "IX_ReferralCodes_Status" ON public."ReferralCodes"("Status");
```

**Fields Explanation**:
- `Code`: Unique identifier like "ZIRA-ABC123" (format: ZIRA-{6 alphanumeric})
- `ExpiresAt`: Automatically set to CreatedAt + 30 days
- `Status`: Enum for lifecycle management (Active/Expired/Disabled)

### 4.2 ReferralTracking Table

```sql
CREATE TABLE public."ReferralTracking" (
    "Id" INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "ReferralCodeId" INTEGER NOT NULL,
    "RefereeUserId" INTEGER NULL,  -- New user who registered (FK to Users)
    "ClickedAt" TIMESTAMP NULL,  -- When link was clicked
    "RegisteredAt" TIMESTAMP NULL,  -- When user completed registration
    "FirstAnalysisAt" TIMESTAMP NULL,  -- When user completed 1st analysis
    "RewardProcessedAt" TIMESTAMP NULL,  -- When referrer received credits
    "Status" INTEGER NOT NULL DEFAULT 0,  -- 0=Clicked, 1=Registered, 2=Validated, 3=Rewarded
    "RefereeMobilePhone" VARCHAR(15) NULL,  -- For tracking before registration
    "IpAddress" VARCHAR(45) NULL,  -- Anti-abuse tracking
    "DeviceId" VARCHAR(255) NULL,  -- Mobile device identifier
    "FailureReason" TEXT NULL,  -- If validation failed, why?

    CONSTRAINT "FK_ReferralTracking_ReferralCodes"
        FOREIGN KEY ("ReferralCodeId") REFERENCES public."ReferralCodes"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ReferralTracking_Users"
        FOREIGN KEY ("RefereeUserId") REFERENCES public."Users"("UserId") ON DELETE SET NULL
);

CREATE INDEX "IX_ReferralTracking_ReferralCodeId" ON public."ReferralTracking"("ReferralCodeId");
CREATE INDEX "IX_ReferralTracking_RefereeUserId" ON public."ReferralTracking"("RefereeUserId");
CREATE INDEX "IX_ReferralTracking_Status" ON public."ReferralTracking"("Status");
CREATE INDEX "IX_ReferralTracking_RefereeMobilePhone" ON public."ReferralTracking"("RefereeMobilePhone");
```

**Status Flow**:
0. **Clicked**: User clicked referral link (ClickedAt populated)
1. **Registered**: User completed registration (RegisteredAt populated)
2. **Validated**: User completed 1st analysis (FirstAnalysisAt populated)
3. **Rewarded**: Referrer received credits (RewardProcessedAt populated)

### 4.3 ReferralRewards Table

```sql
CREATE TABLE public."ReferralRewards" (
    "Id" INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "ReferralTrackingId" INTEGER NOT NULL,
    "ReferrerUserId" INTEGER NOT NULL,  -- Who received the reward
    "RefereeUserId" INTEGER NOT NULL,  -- Who triggered the reward
    "CreditAmount" INTEGER NOT NULL,  -- How many analysis credits awarded
    "AwardedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "SubscriptionId" INTEGER NULL,  -- Which subscription received credits
    "ExpiresAt" TIMESTAMP NULL,  -- If credits have expiry

    CONSTRAINT "FK_ReferralRewards_ReferralTracking"
        FOREIGN KEY ("ReferralTrackingId") REFERENCES public."ReferralTracking"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ReferralRewards_ReferrerUsers"
        FOREIGN KEY ("ReferrerUserId") REFERENCES public."Users"("UserId") ON DELETE CASCADE,
    CONSTRAINT "FK_ReferralRewards_RefereeUsers"
        FOREIGN KEY ("RefereeUserId") REFERENCES public."Users"("UserId") ON DELETE CASCADE,
    CONSTRAINT "FK_ReferralRewards_Subscriptions"
        FOREIGN KEY ("SubscriptionId") REFERENCES public."Subscriptions"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_ReferralRewards_ReferrerUserId" ON public."ReferralRewards"("ReferrerUserId");
CREATE INDEX "IX_ReferralRewards_RefereeUserId" ON public."ReferralRewards"("RefereeUserId");
CREATE INDEX "IX_ReferralRewards_AwardedAt" ON public."ReferralRewards"("AwardedAt");
```

**Credit Management**:
- `CreditAmount`: Configurable (default 10, admin can change)
- `SubscriptionId`: Tracks which subscription received credits
- `ExpiresAt`: Future feature for time-limited rewards (NULL = no expiry)

### 4.4 ReferralConfigurations Table

```sql
CREATE TABLE public."ReferralConfigurations" (
    "Id" INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "Key" VARCHAR(100) NOT NULL UNIQUE,
    "Value" TEXT NOT NULL,
    "Description" TEXT NULL,
    "DataType" VARCHAR(20) NOT NULL DEFAULT 'string',  -- string, int, bool, decimal
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedBy" INTEGER NULL,  -- Admin user who made change

    CONSTRAINT "FK_ReferralConfigurations_Users"
        FOREIGN KEY ("UpdatedBy") REFERENCES public."Users"("UserId") ON DELETE SET NULL
);

CREATE UNIQUE INDEX "IX_ReferralConfigurations_Key" ON public."ReferralConfigurations"("Key");

-- Insert default configurations
INSERT INTO public."ReferralConfigurations" ("Key", "Value", "Description", "DataType") VALUES
('Referral.CreditPerReferral', '10', 'Number of analysis credits awarded per successful referral', 'int'),
('Referral.LinkExpiryDays', '30', 'Number of days before referral link expires', 'int'),
('Referral.MinAnalysisForValidation', '1', 'Minimum number of analyses new user must complete', 'int'),
('Referral.MaxReferralsPerUser', '0', 'Maximum referrals per user (0 = unlimited)', 'int'),
('Referral.EnableWhatsApp', 'true', 'Enable WhatsApp link sharing', 'bool'),
('Referral.EnableSMS', 'true', 'Enable SMS link sharing', 'bool'),
('Referral.CodePrefix', 'ZIRA', 'Prefix for referral codes', 'string'),
('Referral.DeepLinkBaseUrl', 'https://ziraai.com/ref/', 'Base URL for referral deep links', 'string');
```

**Configuration Keys**:
- `Referral.CreditPerReferral`: **Configurable reward amount (e.g., 10)**
- `Referral.LinkExpiryDays`: 30 days standard
- `Referral.MinAnalysisForValidation`: 1 analysis required
- Other keys for future flexibility

### 4.5 Schema Modifications to Existing Tables

#### Subscriptions Table
```sql
-- Add field to track referral-earned credits separately
ALTER TABLE public."Subscriptions"
    ADD COLUMN "ReferralCredits" INTEGER NOT NULL DEFAULT 0;

-- Add comment
COMMENT ON COLUMN public."Subscriptions"."ReferralCredits" IS
    'Analysis credits earned through referrals, separate from subscription quota';
```

#### Users Table
```sql
-- Add referral code that was used during registration
ALTER TABLE public."Users"
    ADD COLUMN "RegistrationReferralCode" VARCHAR(20) NULL;

-- Add foreign key to track which referral code was used
CREATE INDEX "IX_Users_RegistrationReferralCode"
    ON public."Users"("RegistrationReferralCode");
```

---

## 5. API Specifications

### 5.1 Generate Referral Link

**Endpoint**: `POST /api/v1/referral/generate-link`
**Authorization**: Required (Bearer token)
**Rate Limit**: 10 requests/hour per user

#### Request
```json
{
  "deliveryMethod": "SMS",  // "SMS" | "WhatsApp" | "Both"
  "recipientPhoneNumbers": [
    "05321234567",
    "05339876543"
  ],
  "customMessage": "ZiraAI ile bitkilerini analiz et!"  // Optional
}
```

#### Response (Success - 200 OK)
```json
{
  "success": true,
  "message": "Referral links generated successfully",
  "data": {
    "referralCode": "ZIRA-ABC123",
    "deepLink": "https://ziraai.com/ref/ZIRA-ABC123",
    "playStoreLink": "https://play.google.com/store/apps/details?id=com.ziraai&referrer=ZIRA-ABC123",
    "expiresAt": "2025-11-01T12:00:00Z",
    "deliveryStatus": [
      {
        "phoneNumber": "05321234567",
        "method": "SMS",
        "status": "Sent",
        "sentAt": "2025-10-02T12:00:00Z"
      },
      {
        "phoneNumber": "05339876543",
        "method": "WhatsApp",
        "status": "Queued"
      }
    ]
  }
}
```

#### Response (Error - 400 Bad Request)
```json
{
  "success": false,
  "message": "Invalid phone number format",
  "errors": [
    "Phone number '1234567890' is not a valid Turkish mobile number"
  ]
}
```

### 5.2 Get Referral Statistics

**Endpoint**: `GET /api/v1/referral/stats`
**Authorization**: Required (Bearer token)

#### Response (200 OK)
```json
{
  "success": true,
  "data": {
    "totalReferrals": 15,
    "successfulReferrals": 12,  // Completed 1st analysis
    "pendingReferrals": 3,  // Registered but not yet validated
    "totalCreditsEarned": 120,
    "referralBreakdown": {
      "clicked": 25,
      "registered": 15,
      "validated": 12,
      "rewarded": 12
    },
    "recentReferrals": [
      {
        "referralCode": "ZIRA-ABC123",
        "recipientPhone": "053********67",  // Masked
        "status": "Rewarded",
        "creditsEarned": 10,
        "registeredAt": "2025-09-15T10:30:00Z",
        "validatedAt": "2025-09-16T14:20:00Z"
      }
    ],
    "activeLinks": [
      {
        "code": "ZIRA-XYZ789",
        "createdAt": "2025-10-01T09:00:00Z",
        "expiresAt": "2025-10-31T09:00:00Z",
        "clickCount": 5,
        "registrationCount": 2
      }
    ]
  }
}
```

### 5.3 Track Referral Click

**Endpoint**: `POST /api/v1/referral/track-click`
**Authorization**: Not required (public endpoint)
**Rate Limit**: 100 requests/hour per IP

#### Request
```json
{
  "referralCode": "ZIRA-ABC123",
  "deviceId": "device-uuid-12345",
  "ipAddress": "192.168.1.100"  // Optional, can be inferred
}
```

#### Response (200 OK)
```json
{
  "success": true,
  "message": "Click tracked successfully",
  "data": {
    "referralCode": "ZIRA-ABC123",
    "playStoreUrl": "https://play.google.com/store/apps/details?id=com.ziraai&referrer=ZIRA-ABC123",
    "isValid": true,
    "expiresAt": "2025-11-01T12:00:00Z"
  }
}
```

### 5.4 Validate Referral Code (Mobile App Registration)

**Endpoint**: `POST /api/v1/referral/validate-code`
**Authorization**: Not required (called during registration)

#### Request
```json
{
  "referralCode": "ZIRA-ABC123",
  "mobilePhone": "05321234567"
}
```

#### Response (200 OK)
```json
{
  "success": true,
  "message": "Referral code is valid",
  "data": {
    "isValid": true,
    "referrerName": "Ahmet Y.",  // First name + last initial
    "expectedReward": 10,  // Credits user will earn for referrer
    "validUntil": "2025-11-01T12:00:00Z"
  }
}
```

#### Response (Error - 400 Bad Request)
```json
{
  "success": false,
  "message": "Referral code expired or invalid",
  "data": {
    "isValid": false,
    "reason": "Code expired on 2025-09-30T12:00:00Z"
  }
}
```

### 5.5 Process Referral Reward (Internal)

**Endpoint**: `POST /api/v1/referral/process-reward`
**Authorization**: Required (Internal service token)
**Called By**: PlantAnalysisWorkerService after 1st analysis completion

#### Request
```json
{
  "refereeUserId": 12345,
  "referralCode": "ZIRA-ABC123"
}
```

#### Response (200 OK)
```json
{
  "success": true,
  "message": "Referral reward processed successfully",
  "data": {
    "referrerUserId": 67890,
    "creditsAwarded": 10,
    "referralTrackingId": 456,
    "rewardId": 789
  }
}
```

### 5.6 Get User Usage with Referral Credits

**Endpoint**: `GET /api/v1/subscription/usage`
**Authorization**: Required (Bearer token)
**Modified**: Add referral credit breakdown

#### Response (200 OK)
```json
{
  "success": true,
  "data": {
    "subscriptionTier": "Trial",
    "subscriptionCredits": 5,  // From paid subscription
    "referralCredits": 30,  // From successful referrals
    "totalAvailableCredits": 35,
    "usedThisMonth": 8,
    "remainingThisMonth": 27,
    "referralCreditBreakdown": {
      "totalEarned": 120,
      "totalUsed": 90,
      "currentBalance": 30
    }
  }
}
```

---

## 6. Deep Linking Architecture

### 6.1 Link Flow Diagram

```
User clicks referral link
    ↓
https://ziraai.com/ref/ZIRA-ABC123
    ↓
[Backend tracks click]
    ↓
Redirect to Play Store with referral parameter
    ↓
https://play.google.com/store/apps/details?id=com.ziraai&referrer=ZIRA-ABC123
    ↓
User installs app
    ↓
App opens and reads "referrer" parameter from Play Store
    ↓
App sends referral code to backend for validation
    ↓
User registers with phone + referral code
    ↓
Backend links new user to referral tracking
```

### 6.2 Web Redirect Handler

**File**: `WebAPI/Controllers/ReferralRedirectController.cs`

```csharp
[ApiController]
[Route("ref")]
public class ReferralRedirectController : ControllerBase
{
    [HttpGet("{code}")]
    public async Task<IActionResult> HandleReferralLink(string code)
    {
        // Track click
        await _trackingService.TrackClickAsync(code, Request.HttpContext);

        // Generate Play Store link
        var playStoreUrl = $"https://play.google.com/store/apps/details?" +
                          $"id=com.ziraai&referrer={code}";

        // Redirect
        return Redirect(playStoreUrl);
    }
}
```

### 6.3 Mobile App Integration Points

#### Flutter App - Referral Code Capture

```dart
// File: lib/services/referral_service.dart
class ReferralService {
  Future<String?> getReferralCode() async {
    // Get install referrer from Play Store
    final referrer = await AndroidPlayInstallReferrer.installReferrer;

    if (referrer != null && referrer.isNotEmpty) {
      // Extract ZIRA-XXXXXX code
      final codeMatch = RegExp(r'ZIRA-[A-Z0-9]{6}').firstMatch(referrer);
      return codeMatch?.group(0);
    }
    return null;
  }

  Future<bool> validateReferralCode(String code) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/v1/referral/validate-code'),
      body: jsonEncode({'referralCode': code}),
    );

    return response.statusCode == 200;
  }
}
```

#### Registration Flow with Referral

```dart
// File: lib/screens/register_screen.dart
class RegisterScreen extends StatefulWidget {
  @override
  _RegisterScreenState createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  String? _referralCode;

  @override
  void initState() {
    super.initState();
    _loadReferralCode();
  }

  Future<void> _loadReferralCode() async {
    final code = await ReferralService().getReferralCode();
    if (code != null) {
      final isValid = await ReferralService().validateReferralCode(code);
      if (isValid) {
        setState(() {
          _referralCode = code;
        });
        _showReferralBanner(code);
      }
    }
  }

  Future<void> _register() async {
    final request = {
      'mobilePhone': _phoneController.text,
      'fullName': _nameController.text,
      'referralCode': _referralCode,  // Include if present
    };

    await AuthService().registerWithPhone(request);
  }
}
```

### 6.4 SMS/WhatsApp Message Templates

#### SMS Template
```
🌱 ZiraAI'ye hoş geldin!

{ReferrerName} seni davet ediyor.
Bitki analizi için uygulamayı indir:

{PlayStoreLink}

Kod: {ReferralCode}
Son kullanma: {ExpiryDate}
```

#### WhatsApp Template
```
🌱 *ZiraAI - Bitki Analizi*

Merhaba! {ReferrerName} seni ZiraAI'ye davet etti.

Yapay zeka ile bitkilerini ücretsiz analiz et:
{PlayStoreLink}

📱 Referans Kodu: *{ReferralCode}*
⏰ Son Kullanma: {ExpiryDate}

_Kayıt olurken bu kodu kullan!_
```

---

## 7. Configuration System

### 7.1 Configuration Service Interface

```csharp
// File: Business/Services/Referral/IReferralConfigurationService.cs
public interface IReferralConfigurationService
{
    Task<int> GetCreditsPerReferralAsync();
    Task<int> GetLinkExpiryDaysAsync();
    Task<int> GetMinAnalysisForValidationAsync();
    Task<int> GetMaxReferralsPerUserAsync();
    Task<bool> IsWhatsAppEnabledAsync();
    Task<bool> IsSmsEnabledAsync();
    Task<string> GetCodePrefixAsync();
    Task<string> GetDeepLinkBaseUrlAsync();

    Task UpdateConfigurationAsync(string key, string value, int updatedBy);
}
```

### 7.2 Configuration Service Implementation

```csharp
// File: Business/Services/Referral/ReferralConfigurationService.cs
public class ReferralConfigurationService : IReferralConfigurationService
{
    private readonly IReferralConfigurationRepository _configRepo;
    private readonly ICacheService _cache;
    private const int CacheTtlMinutes = 15;

    public async Task<int> GetCreditsPerReferralAsync()
    {
        return await GetIntConfigAsync("Referral.CreditPerReferral", defaultValue: 10);
    }

    public async Task<int> GetLinkExpiryDaysAsync()
    {
        return await GetIntConfigAsync("Referral.LinkExpiryDays", defaultValue: 30);
    }

    private async Task<int> GetIntConfigAsync(string key, int defaultValue)
    {
        var cacheKey = $"config:{key}";

        // Try cache first
        if (_cache.TryGet(cacheKey, out int cachedValue))
            return cachedValue;

        // Get from database
        var config = await _configRepo.GetAsync(c => c.Key == key);

        if (config == null || !int.TryParse(config.Value, out int value))
            value = defaultValue;

        // Cache for 15 minutes
        _cache.Set(cacheKey, value, TimeSpan.FromMinutes(CacheTtlMinutes));

        return value;
    }

    public async Task UpdateConfigurationAsync(string key, string value, int updatedBy)
    {
        var config = await _configRepo.GetAsync(c => c.Key == key);

        if (config == null)
            throw new NotFoundException($"Configuration key '{key}' not found");

        config.Value = value;
        config.UpdatedAt = DateTime.Now;
        config.UpdatedBy = updatedBy;

        await _configRepo.UpdateAsync(config);

        // Invalidate cache
        _cache.Remove($"config:{key}");
    }
}
```

### 7.3 Admin Configuration Endpoint

```csharp
// File: WebAPI/Controllers/Admin/ReferralConfigController.cs
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/referral-config")]
public class ReferralConfigController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetConfigurations()
    {
        var query = new GetReferralConfigurationsQuery();
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateConfiguration(
        string key,
        [FromBody] UpdateConfigurationCommand command)
    {
        command.Key = key;
        command.UpdatedBy = UserId;  // From JWT claims

        var result = await Mediator.Send(command);
        return GetResponseOnlyResult(result);
    }
}
```

---

## 8. Business Logic

### 8.1 Referral Code Generation

**Service**: `ReferralCodeService`

```csharp
// File: Business/Services/Referral/ReferralCodeService.cs
public class ReferralCodeService : IReferralCodeService
{
    private const string AllowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 6;

    public string GenerateUniqueCode(string prefix = "ZIRA")
    {
        var random = new Random();
        var attempts = 0;
        const int maxAttempts = 10;

        while (attempts < maxAttempts)
        {
            var code = new StringBuilder(prefix);
            code.Append('-');

            for (int i = 0; i < CodeLength; i++)
            {
                code.Append(AllowedChars[random.Next(AllowedChars.Length)]);
            }

            var generatedCode = code.ToString();

            // Check uniqueness
            if (!_codeRepo.Query().Any(c => c.Code == generatedCode))
                return generatedCode;

            attempts++;
        }

        throw new ApplicationException("Failed to generate unique referral code");
    }

    public async Task<ReferralCode> CreateReferralCodeAsync(int userId)
    {
        var config = await _configService.GetLinkExpiryDaysAsync();
        var prefix = await _configService.GetCodePrefixAsync();

        var code = new ReferralCode
        {
            UserId = userId,
            Code = GenerateUniqueCode(prefix),
            IsActive = true,
            CreatedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddDays(config),
            Status = ReferralCodeStatus.Active
        };

        await _codeRepo.AddAsync(code);

        return code;
    }
}
```

**Code Format**: `ZIRA-ABC123`
- Prefix: "ZIRA" (configurable)
- Separator: "-"
- Suffix: 6 alphanumeric characters (excluding confusing: 0, O, 1, I, L)
- Total length: 11 characters

### 8.2 Referral Link Service

```csharp
// File: Business/Services/Referral/ReferralLinkService.cs
public class ReferralLinkService : IReferralLinkService
{
    public async Task<ReferralLinkResult> GenerateAndSendLinksAsync(
        int userId,
        List<string> phoneNumbers,
        DeliveryMethod method)
    {
        // 1. Generate or reuse active referral code
        var code = await GetOrCreateActiveCodeAsync(userId);

        // 2. Build deep link
        var baseUrl = await _configService.GetDeepLinkBaseUrlAsync();
        var deepLink = $"{baseUrl}{code.Code}";

        // 3. Build Play Store link
        var playStoreLink = BuildPlayStoreLink(code.Code);

        // 4. Send messages
        var deliveryResults = await SendMessagesAsync(
            phoneNumbers,
            code,
            deepLink,
            playStoreLink,
            method);

        return new ReferralLinkResult
        {
            ReferralCode = code.Code,
            DeepLink = deepLink,
            PlayStoreLink = playStoreLink,
            ExpiresAt = code.ExpiresAt,
            DeliveryStatus = deliveryResults
        };
    }

    private string BuildPlayStoreLink(string code)
    {
        return $"https://play.google.com/store/apps/details?" +
               $"id=com.ziraai&referrer={code}";
    }

    private async Task<List<DeliveryResult>> SendMessagesAsync(
        List<string> phones,
        ReferralCode code,
        string deepLink,
        string playStoreLink,
        DeliveryMethod method)
    {
        var results = new List<DeliveryResult>();

        foreach (var phone in phones)
        {
            if (method == DeliveryMethod.SMS || method == DeliveryMethod.Both)
            {
                var smsResult = await SendSmsAsync(phone, code, playStoreLink);
                results.Add(smsResult);
            }

            if (method == DeliveryMethod.WhatsApp || method == DeliveryMethod.Both)
            {
                var whatsappResult = await SendWhatsAppAsync(phone, code, playStoreLink);
                results.Add(whatsappResult);
            }
        }

        return results;
    }

    private async Task<DeliveryResult> SendSmsAsync(
        string phone,
        ReferralCode code,
        string link)
    {
        var message = BuildSmsMessage(code, link);

        try
        {
            var sent = await _smsService.SendAssist(message, phone);

            return new DeliveryResult
            {
                PhoneNumber = phone,
                Method = DeliveryMethod.SMS,
                Status = sent ? DeliveryStatus.Sent : DeliveryStatus.Failed,
                SentAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Phone}", phone);

            return new DeliveryResult
            {
                PhoneNumber = phone,
                Method = DeliveryMethod.SMS,
                Status = DeliveryStatus.Failed,
                ErrorMessage = ex.Message
            };
        }
    }

    private string BuildSmsMessage(ReferralCode code, string link)
    {
        var referrerName = _userRepo.GetAsync(code.UserId).FullName;
        var expiryDate = code.ExpiresAt.ToString("dd MMM yyyy",
            new CultureInfo("tr-TR"));

        return $"🌱 ZiraAI'ye hoş geldin!\n\n" +
               $"{referrerName} seni davet ediyor. " +
               $"Bitki analizi için uygulamayı indir:\n\n" +
               $"{link}\n\n" +
               $"Kod: {code.Code}\n" +
               $"Son kullanma: {expiryDate}";
    }
}
```

### 8.3 Referral Tracking Service

```csharp
// File: Business/Services/Referral/ReferralTrackingService.cs
public class ReferralTrackingService : IReferralTrackingService
{
    public async Task TrackClickAsync(string code, HttpContext context)
    {
        var referralCode = await _codeRepo.GetAsync(c => c.Code == code);

        if (referralCode == null || !referralCode.IsActive)
        {
            _logger.LogWarning("Invalid referral code clicked: {Code}", code);
            return;
        }

        // Check if already tracked from this device
        var deviceId = GetDeviceId(context);
        var existingTracking = await _trackingRepo.Query()
            .Where(t => t.ReferralCodeId == referralCode.Id &&
                       t.DeviceId == deviceId)
            .FirstOrDefaultAsync();

        if (existingTracking != null)
        {
            _logger.LogInformation("Duplicate click from device {DeviceId}", deviceId);
            return;
        }

        // Create tracking record
        var tracking = new ReferralTracking
        {
            ReferralCodeId = referralCode.Id,
            ClickedAt = DateTime.Now,
            Status = ReferralTrackingStatus.Clicked,
            IpAddress = GetIpAddress(context),
            DeviceId = deviceId
        };

        await _trackingRepo.AddAsync(tracking);

        _logger.LogInformation("Referral click tracked: Code={Code}, Device={Device}",
            code, deviceId);
    }

    public async Task LinkRegistrationAsync(int userId, string referralCode)
    {
        var code = await _codeRepo.GetAsync(c => c.Code == referralCode);

        if (code == null || !code.IsActive)
            throw new BusinessException("Invalid or expired referral code");

        // Prevent self-referral
        if (code.UserId == userId)
            throw new BusinessException("Cannot use your own referral code");

        // Find tracking record by user's phone
        var user = await _userRepo.GetAsync(userId);
        var tracking = await _trackingRepo.Query()
            .Where(t => t.ReferralCodeId == code.Id &&
                       t.RefereeMobilePhone == user.MobilePhones)
            .FirstOrDefaultAsync();

        if (tracking == null)
        {
            // Create new tracking if click wasn't tracked
            tracking = new ReferralTracking
            {
                ReferralCodeId = code.Id,
                ClickedAt = DateTime.Now,
                Status = ReferralTrackingStatus.Registered
            };
        }

        tracking.RefereeUserId = userId;
        tracking.RegisteredAt = DateTime.Now;
        tracking.Status = ReferralTrackingStatus.Registered;

        await _trackingRepo.UpdateAsync(tracking);

        // Update user record
        user.RegistrationReferralCode = referralCode;
        await _userRepo.UpdateAsync(user);

        _logger.LogInformation("Registration linked to referral: User={UserId}, Code={Code}",
            userId, referralCode);
    }

    public async Task ValidateReferralAsync(int userId)
    {
        // Called after user completes 1st analysis
        var user = await _userRepo.GetAsync(userId);

        if (string.IsNullOrEmpty(user.RegistrationReferralCode))
            return;  // No referral code used

        var code = await _codeRepo.GetAsync(c => c.Code == user.RegistrationReferralCode);
        var tracking = await _trackingRepo.Query()
            .Where(t => t.ReferralCodeId == code.Id &&
                       t.RefereeUserId == userId)
            .FirstOrDefaultAsync();

        if (tracking == null)
        {
            _logger.LogError("Tracking record not found for user {UserId}", userId);
            return;
        }

        // Check if already validated
        if (tracking.Status >= ReferralTrackingStatus.Validated)
        {
            _logger.LogInformation("Referral already validated for user {UserId}", userId);
            return;
        }

        // Update tracking
        tracking.FirstAnalysisAt = DateTime.Now;
        tracking.Status = ReferralTrackingStatus.Validated;
        await _trackingRepo.UpdateAsync(tracking);

        _logger.LogInformation("Referral validated: User={UserId}, Code={Code}",
            userId, user.RegistrationReferralCode);

        // Trigger reward processing
        await _rewardService.ProcessRewardAsync(code.UserId, userId, tracking.Id);
    }
}
```

### 8.4 Referral Reward Service

```csharp
// File: Business/Services/Referral/ReferralRewardService.cs
public class ReferralRewardService : IReferralRewardService
{
    public async Task ProcessRewardAsync(
        int referrerUserId,
        int refereeUserId,
        int trackingId)
    {
        // 1. Check if already rewarded
        var existingReward = await _rewardRepo.Query()
            .Where(r => r.ReferralTrackingId == trackingId)
            .FirstOrDefaultAsync();

        if (existingReward != null)
        {
            _logger.LogWarning("Reward already processed for tracking {TrackingId}", trackingId);
            return;
        }

        // 2. Get configurable credit amount
        var creditAmount = await _configService.GetCreditsPerReferralAsync();

        // 3. Get referrer's active subscription
        var subscription = await _subscriptionRepo.Query()
            .Where(s => s.UserId == referrerUserId && s.Status == "Active")
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            _logger.LogError("No active subscription found for referrer {UserId}",
                referrerUserId);
            throw new BusinessException("Referrer must have an active subscription");
        }

        // 4. Add credits to subscription
        subscription.ReferralCredits += creditAmount;
        await _subscriptionRepo.UpdateAsync(subscription);

        // 5. Create reward record
        var reward = new ReferralReward
        {
            ReferralTrackingId = trackingId,
            ReferrerUserId = referrerUserId,
            RefereeUserId = refereeUserId,
            CreditAmount = creditAmount,
            AwardedAt = DateTime.Now,
            SubscriptionId = subscription.Id,
            ExpiresAt = null  // No expiry for now
        };

        await _rewardRepo.AddAsync(reward);

        // 6. Update tracking status
        var tracking = await _trackingRepo.GetAsync(trackingId);
        tracking.RewardProcessedAt = DateTime.Now;
        tracking.Status = ReferralTrackingStatus.Rewarded;
        await _trackingRepo.UpdateAsync(tracking);

        _logger.LogInformation(
            "Referral reward processed: Referrer={Referrer}, Referee={Referee}, Credits={Credits}",
            referrerUserId, refereeUserId, creditAmount);

        // 7. Send notification (future enhancement)
        // await _notificationService.SendReferralRewardNotification(referrerUserId, creditAmount);
    }

    public async Task<int> GetTotalReferralCreditsAsync(int userId)
    {
        var subscription = await _subscriptionRepo.Query()
            .Where(s => s.UserId == userId && s.Status == "Active")
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        return subscription?.ReferralCredits ?? 0;
    }

    public async Task DeductReferralCreditAsync(int userId)
    {
        var subscription = await _subscriptionRepo.Query()
            .Where(s => s.UserId == userId && s.Status == "Active")
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (subscription == null || subscription.ReferralCredits <= 0)
            throw new BusinessException("No referral credits available");

        subscription.ReferralCredits--;
        await _subscriptionRepo.UpdateAsync(subscription);
    }
}
```

### 8.5 Credit Usage Priority Logic

**Modified**: `Business/Services/SubscriptionValidationService.cs`

```csharp
public async Task<bool> ValidateAndDecrementUsageAsync(int userId)
{
    var subscription = await GetActiveSubscriptionAsync(userId);

    // Priority: Use referral credits first, then subscription quota
    if (subscription.ReferralCredits > 0)
    {
        await _referralRewardService.DeductReferralCreditAsync(userId);
        _logger.LogInformation("Used referral credit for user {UserId}", userId);
        return true;
    }

    if (subscription.DailyUsage < subscription.DailyRequestLimit)
    {
        // Use subscription quota
        subscription.DailyUsage++;
        await _subscriptionRepo.UpdateAsync(subscription);
        _logger.LogInformation("Used subscription quota for user {UserId}", userId);
        return true;
    }

    throw new BusinessException("No available credits or quota");
}
```

---

## 9. Integration Points

### 9.1 Phone Authentication Integration

**Modified**: `Business/Handlers/Authorizations/Commands/RegisterUserWithPhoneCommand.cs`

```csharp
public class RegisterUserWithPhoneCommandHandler : IRequestHandler<RegisterUserWithPhoneCommand, IResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IReferralTrackingService _referralService;

    public async Task<IResult> Handle(RegisterUserWithPhoneCommand request, CancellationToken cancellationToken)
    {
        // ... existing validation ...

        // Create user
        var user = new User
        {
            MobilePhones = normalizedPhone,
            FullName = request.FullName,
            Status = true,
            AuthenticationProviderType = AuthenticationProviderType.Phone.ToString(),
            RegistrationReferralCode = request.ReferralCode  // NEW: Store referral code
        };

        await _userRepository.AddAsync(user);

        // ... create trial subscription ...

        // NEW: Link referral if provided
        if (!string.IsNullOrEmpty(request.ReferralCode))
        {
            try
            {
                await _referralService.LinkRegistrationAsync(user.UserId, request.ReferralCode);
                _logger.LogInformation("Referral linked for user {UserId}", user.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to link referral for user {UserId}", user.UserId);
                // Don't fail registration if referral linking fails
            }
        }

        return new SuccessResult(Messages.UserRegisteredSuccessfully);
    }
}
```

### 9.2 Plant Analysis Integration

**Modified**: `Business/Handlers/PlantAnalysisRequests/Commands/CreatePlantAnalysisRequestCommand.cs`

```csharp
public class CreatePlantAnalysisRequestCommandHandler : IRequestHandler<CreatePlantAnalysisRequestCommand, IResult>
{
    private readonly IReferralTrackingService _referralService;

    public async Task<IResult> Handle(CreatePlantAnalysisRequestCommand request, CancellationToken cancellationToken)
    {
        // ... existing analysis logic ...

        var analysisRequest = new PlantAnalysisRequest
        {
            UserId = userId,
            ImageUrl = imageUrl,
            // ... other fields ...
        };

        await _analysisRepo.AddAsync(analysisRequest);

        // NEW: Check if this is user's first analysis
        var analysisCount = await _analysisRepo.Query()
            .Where(a => a.UserId == userId)
            .CountAsync(cancellationToken);

        if (analysisCount == 1)  // First analysis
        {
            try
            {
                await _referralService.ValidateReferralAsync(userId);
                _logger.LogInformation("First analysis validation triggered for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate referral for user {UserId}", userId);
                // Don't fail analysis if referral validation fails
            }
        }

        return new SuccessResult(Messages.AnalysisCreated);
    }
}
```

### 9.3 Subscription Usage Integration

**Modified**: `Business/Handlers/Subscriptions/Queries/GetUserSubscriptionUsageQuery.cs`

```csharp
public class GetUserSubscriptionUsageQueryHandler : IRequestHandler<GetUserSubscriptionUsageQuery, IDataResult<SubscriptionUsageDto>>
{
    private readonly IReferralRewardService _referralService;

    public async Task<IDataResult<SubscriptionUsageDto>> Handle(
        GetUserSubscriptionUsageQuery request,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepo.GetActiveSubscriptionAsync(request.UserId);

        // NEW: Get referral credit breakdown
        var referralCredits = await _referralService.GetTotalReferralCreditsAsync(request.UserId);
        var totalReferralEarned = await _referralService.GetTotalEarnedCreditsAsync(request.UserId);
        var totalReferralUsed = totalReferralEarned - referralCredits;

        var dto = new SubscriptionUsageDto
        {
            SubscriptionTier = subscription.Tier,
            SubscriptionCredits = subscription.DailyRequestLimit - subscription.DailyUsage,
            ReferralCredits = referralCredits,  // NEW
            TotalAvailableCredits = (subscription.DailyRequestLimit - subscription.DailyUsage) + referralCredits,
            UsedThisMonth = subscription.MonthlyUsage,
            RemainingThisMonth = subscription.MonthlyRequestLimit - subscription.MonthlyUsage,
            ReferralCreditBreakdown = new ReferralCreditBreakdownDto  // NEW
            {
                TotalEarned = totalReferralEarned,
                TotalUsed = totalReferralUsed,
                CurrentBalance = referralCredits
            }
        };

        return new SuccessDataResult<SubscriptionUsageDto>(dto);
    }
}
```

---

## 10. Implementation Roadmap

### Phase 1: Core Infrastructure (Week 1)
**Goal**: Database schema and core services

#### Tasks:
1. **Database Setup**
   - [ ] Create migration script for 4 new tables
   - [ ] Add ReferralCredits column to Subscriptions
   - [ ] Add RegistrationReferralCode to Users
   - [ ] Run migration on dev database
   - [ ] Verify indexes and constraints

2. **Entity Classes**
   - [ ] Create `ReferralCode.cs` entity
   - [ ] Create `ReferralTracking.cs` entity
   - [ ] Create `ReferralReward.cs` entity
   - [ ] Create `ReferralConfiguration.cs` entity
   - [ ] Add EF configurations for all entities

3. **Repository Layer**
   - [ ] Create `IReferralCodeRepository` interface
   - [ ] Implement `ReferralCodeRepository`
   - [ ] Create `IReferralTrackingRepository` interface
   - [ ] Implement `ReferralTrackingRepository`
   - [ ] Create `IReferralRewardRepository` interface
   - [ ] Implement `ReferralRewardRepository`
   - [ ] Create `IReferralConfigurationRepository` interface
   - [ ] Implement `ReferralConfigurationRepository`
   - [ ] Register repositories in DI container

4. **Configuration Service**
   - [ ] Implement `ReferralConfigurationService`
   - [ ] Add caching layer
   - [ ] Insert default configuration values
   - [ ] Add configuration update logic

**Deliverable**: Database schema ready, repositories functional, configuration system operational

### Phase 2: Business Logic & Services (Week 2)
**Goal**: Core referral services implementation

#### Tasks:
1. **Referral Code Service**
   - [ ] Implement code generation algorithm
   - [ ] Add uniqueness validation
   - [ ] Add expiry logic
   - [ ] Add code status management
   - [ ] Write unit tests (90% coverage)

2. **Referral Link Service**
   - [ ] Implement link generation
   - [ ] Add Play Store URL builder
   - [ ] Integrate with SMS service
   - [ ] Integrate with WhatsApp service
   - [ ] Add delivery status tracking
   - [ ] Write unit tests

3. **Referral Tracking Service**
   - [ ] Implement click tracking
   - [ ] Add registration linking
   - [ ] Add validation logic
   - [ ] Add anti-duplicate logic
   - [ ] Write unit tests

4. **Referral Reward Service**
   - [ ] Implement reward processing
   - [ ] Add credit allocation logic
   - [ ] Add credit deduction logic
   - [ ] Integrate with subscription system
   - [ ] Write unit tests

**Deliverable**: All core services implemented and tested

### Phase 3: API Endpoints (Week 3)
**Goal**: REST API for referral operations

#### Tasks:
1. **ReferralController**
   - [ ] Implement `POST /generate-link`
   - [ ] Implement `GET /stats`
   - [ ] Implement `POST /track-click`
   - [ ] Implement `POST /validate-code`
   - [ ] Add authorization attributes
   - [ ] Add rate limiting

2. **CQRS Handlers**
   - [ ] Create `GenerateReferralLinkCommand`
   - [ ] Create `GenerateReferralLinkCommandHandler`
   - [ ] Create `GetUserReferralStatsQuery`
   - [ ] Create `GetUserReferralStatsQueryHandler`
   - [ ] Create `ValidateReferralCodeQuery`
   - [ ] Create `ValidateReferralCodeQueryHandler`
   - [ ] Add FluentValidation rules

3. **DTOs**
   - [ ] Create `ReferralLinkRequestDto`
   - [ ] Create `ReferralLinkResponseDto`
   - [ ] Create `ReferralStatsDto`
   - [ ] Create `ReferralValidationDto`

4. **Admin Endpoints**
   - [ ] Create `ReferralConfigController`
   - [ ] Implement `GET /config`
   - [ ] Implement `PUT /config/{key}`
   - [ ] Add admin authorization

**Deliverable**: Complete REST API with documentation

### Phase 4: Integration & Testing (Week 4)
**Goal**: System integration and E2E testing

#### Tasks:
1. **Phone Authentication Integration**
   - [ ] Modify `RegisterUserWithPhoneCommand` to accept referral code
   - [ ] Add referral linking on registration
   - [ ] Test registration flow with referral code

2. **Plant Analysis Integration**
   - [ ] Add first-analysis detection
   - [ ] Trigger referral validation
   - [ ] Test reward processing

3. **Subscription Usage Integration**
   - [ ] Modify `GetUserSubscriptionUsageQuery`
   - [ ] Add referral credit display
   - [ ] Modify `ValidateAndDecrementUsageAsync`
   - [ ] Implement credit usage priority
   - [ ] Test credit deduction

4. **Web Redirect Handler**
   - [ ] Create `ReferralRedirectController`
   - [ ] Implement click tracking
   - [ ] Test Play Store redirect

5. **E2E Testing**
   - [ ] Test complete referral flow (generate → send → register → analyze → reward)
   - [ ] Test with SMS delivery
   - [ ] Test with WhatsApp delivery
   - [ ] Test expiry scenarios
   - [ ] Test anti-abuse scenarios (self-referral, duplicate, etc.)
   - [ ] Load testing (1000 referrals)

**Deliverable**: Fully integrated system with passing E2E tests

### Phase 5: Mobile App Integration (Week 5)
**Goal**: Flutter app deep linking and referral code handling

#### Tasks:
1. **Deep Linking Setup**
   - [ ] Configure Android App Links in `AndroidManifest.xml`
   - [ ] Add Play Install Referrer library
   - [ ] Test deep link handling

2. **Referral Service (Flutter)**
   - [ ] Create `ReferralService` class
   - [ ] Implement referral code extraction
   - [ ] Add code validation API call
   - [ ] Add error handling

3. **Registration Screen**
   - [ ] Modify registration to accept referral code
   - [ ] Add referral code input field (optional)
   - [ ] Add referral banner display
   - [ ] Test registration with referral

4. **Testing**
   - [ ] Test Play Store → App flow
   - [ ] Test referral code capture
   - [ ] Test registration with code
   - [ ] Test without code (backward compatibility)

**Deliverable**: Mobile app fully integrated with referral system

### Phase 6: Monitoring & Analytics (Week 6)
**Goal**: Observability and reporting

#### Tasks:
1. **Logging**
   - [ ] Add structured logging to all services
   - [ ] Log referral lifecycle events
   - [ ] Log reward processing events
   - [ ] Log delivery failures

2. **Metrics**
   - [ ] Track referral generation rate
   - [ ] Track click-through rate
   - [ ] Track conversion rate (click → registration)
   - [ ] Track validation rate (registration → 1st analysis)
   - [ ] Track reward distribution

3. **Dashboard (Optional)**
   - [ ] Create admin dashboard for referral overview
   - [ ] Display top referrers
   - [ ] Display conversion funnel
   - [ ] Display reward distribution

4. **Alerts**
   - [ ] Alert on high failure rate
   - [ ] Alert on suspicious activity
   - [ ] Alert on reward processing errors

**Deliverable**: Production-ready monitoring and observability

### Phase 7: Production Deployment (Week 7)
**Goal**: Safe production rollout

#### Tasks:
1. **Staging Deployment**
   - [ ] Deploy to staging environment
   - [ ] Run smoke tests
   - [ ] Run E2E tests
   - [ ] Performance testing

2. **Production Database Migration**
   - [ ] Backup production database
   - [ ] Run migration script
   - [ ] Verify schema changes

3. **Production Deployment**
   - [ ] Deploy backend API
   - [ ] Deploy mobile app update
   - [ ] Monitor error rates
   - [ ] Monitor performance metrics

4. **Documentation**
   - [ ] Update API documentation
   - [ ] Create user guide for referral feature
   - [ ] Create admin guide for configuration
   - [ ] Update Postman collection

**Deliverable**: Live referral system in production

---

## 11. Testing Strategy

### 11.1 Unit Tests

#### ReferralCodeService Tests
```csharp
[TestClass]
public class ReferralCodeServiceTests
{
    [TestMethod]
    public void GenerateUniqueCode_ShouldReturnCorrectFormat()
    {
        // Arrange
        var service = new ReferralCodeService(...);

        // Act
        var code = service.GenerateUniqueCode("ZIRA");

        // Assert
        Assert.IsTrue(Regex.IsMatch(code, @"^ZIRA-[A-Z0-9]{6}$"));
    }

    [TestMethod]
    public async Task CreateReferralCodeAsync_ShouldSetExpiryCorrectly()
    {
        // Arrange
        var service = new ReferralCodeService(...);
        var userId = 123;

        // Act
        var code = await service.CreateReferralCodeAsync(userId);

        // Assert
        var expectedExpiry = DateTime.Now.AddDays(30);
        Assert.AreEqual(expectedExpiry.Date, code.ExpiresAt.Date);
    }
}
```

#### ReferralTrackingService Tests
```csharp
[TestClass]
public class ReferralTrackingServiceTests
{
    [TestMethod]
    public async Task TrackClickAsync_ShouldCreateNewTracking()
    {
        // Test click tracking
    }

    [TestMethod]
    public async Task TrackClickAsync_ShouldPreventDuplicates()
    {
        // Test duplicate prevention
    }

    [TestMethod]
    public async Task LinkRegistrationAsync_ShouldPreventSelfReferral()
    {
        // Test anti-abuse: self-referral
    }
}
```

#### ReferralRewardService Tests
```csharp
[TestClass]
public class ReferralRewardServiceTests
{
    [TestMethod]
    public async Task ProcessRewardAsync_ShouldAddCreditsToSubscription()
    {
        // Test reward processing
    }

    [TestMethod]
    public async Task ProcessRewardAsync_ShouldPreventDuplicateRewards()
    {
        // Test duplicate reward prevention
    }

    [TestMethod]
    public async Task DeductReferralCreditAsync_ShouldDecrementCorrectly()
    {
        // Test credit deduction
    }
}
```

### 11.2 Integration Tests

#### Referral Flow Integration Test
```csharp
[TestClass]
public class ReferralFlowIntegrationTests
{
    [TestMethod]
    public async Task CompleteReferralFlow_ShouldProcessRewardCorrectly()
    {
        // 1. User A generates referral link
        var linkResult = await GenerateReferralLink(userAId);

        // 2. Track click
        await TrackClick(linkResult.ReferralCode);

        // 3. User B registers with referral code
        var userB = await RegisterUser("05321234567", linkResult.ReferralCode);

        // 4. User B completes 1st analysis
        await CreatePlantAnalysis(userB.UserId);

        // 5. Verify reward processed
        var stats = await GetReferralStats(userAId);
        Assert.AreEqual(1, stats.SuccessfulReferrals);
        Assert.AreEqual(10, stats.TotalCreditsEarned);

        // 6. Verify credits added to subscription
        var subscription = await GetSubscription(userAId);
        Assert.AreEqual(10, subscription.ReferralCredits);
    }
}
```

### 11.3 E2E Test Scenarios

#### Scenario 1: Happy Path
```
1. User A logs in
2. User A generates referral link for User B's phone
3. User B receives SMS with link
4. User B clicks link → redirected to Play Store
5. User B installs app
6. App captures referral code
7. User B registers with phone + referral code
8. User B completes 1st plant analysis
9. System processes reward
10. User A receives 10 credits
11. User A checks usage stats → sees 10 referral credits
```

#### Scenario 2: Expired Link
```
1. User A generates referral link
2. Wait 31 days (simulated)
3. User B clicks link
4. System shows "Link expired" message
5. User B registers without referral code
6. User A receives no reward
```

#### Scenario 3: Self-Referral Prevention
```
1. User A generates referral link
2. User A tries to register new account with own referral code
3. System rejects with error: "Cannot use your own referral code"
```

#### Scenario 4: Credit Usage Priority
```
1. User A has:
   - Subscription: 5 credits
   - Referral: 10 credits
2. User A creates plant analysis
3. System uses referral credit first
4. User A now has:
   - Subscription: 5 credits
   - Referral: 9 credits
```

### 11.4 Performance Tests

#### Load Test: 1000 Concurrent Referrals
```csharp
[TestMethod]
public async Task LoadTest_1000ConcurrentReferralGenerations()
{
    var tasks = new List<Task>();

    for (int i = 0; i < 1000; i++)
    {
        tasks.Add(GenerateReferralLinkAsync(userId: i));
    }

    var stopwatch = Stopwatch.StartNew();
    await Task.WhenAll(tasks);
    stopwatch.Stop();

    // Should complete within 10 seconds
    Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000);
}
```

---

## 12. Security Considerations

### 12.1 Anti-Abuse Measures

#### 1. Self-Referral Prevention
```csharp
public async Task LinkRegistrationAsync(int userId, string referralCode)
{
    var code = await _codeRepo.GetAsync(c => c.Code == referralCode);

    // Prevent self-referral
    if (code.UserId == userId)
        throw new BusinessException("Cannot use your own referral code");

    // ... rest of logic ...
}
```

#### 2. Duplicate Prevention
```csharp
public async Task TrackClickAsync(string code, HttpContext context)
{
    var deviceId = GetDeviceId(context);

    // Check if already tracked from this device
    var existingTracking = await _trackingRepo.Query()
        .Where(t => t.ReferralCodeId == referralCode.Id &&
                   t.DeviceId == deviceId)
        .FirstOrDefaultAsync();

    if (existingTracking != null)
    {
        _logger.LogWarning("Duplicate click from device {DeviceId}", deviceId);
        return;  // Don't create duplicate tracking
    }

    // ... create new tracking ...
}
```

#### 3. Rate Limiting
```csharp
[RateLimit(Name = "GenerateReferralLink", Seconds = 3600, Limit = 10)]
[HttpPost("generate-link")]
public async Task<IActionResult> GenerateReferralLink(...)
{
    // Users can only generate 10 links per hour
}
```

#### 4. Phone Number Validation
```csharp
public async Task<ReferralLinkResult> GenerateAndSendLinksAsync(
    int userId,
    List<string> phoneNumbers,
    DeliveryMethod method)
{
    // Validate phone numbers
    foreach (var phone in phoneNumbers)
    {
        if (!IsValidTurkishPhoneNumber(phone))
            throw new ValidationException($"Invalid phone: {phone}");
    }

    // Prevent sending to same phone multiple times
    var distinctPhones = phoneNumbers.Distinct().ToList();

    // ... rest of logic ...
}
```

### 12.2 Code Generation Security

#### Secure Random Generation
```csharp
public string GenerateUniqueCode(string prefix = "ZIRA")
{
    // Use cryptographically secure random
    using var rng = new RNGCryptoServiceProvider();
    var bytes = new byte[6];
    rng.GetBytes(bytes);

    var code = new StringBuilder(prefix);
    code.Append('-');

    for (int i = 0; i < 6; i++)
    {
        code.Append(AllowedChars[bytes[i] % AllowedChars.Length]);
    }

    return code.ToString();
}
```

#### Collision Detection
```csharp
while (attempts < maxAttempts)
{
    var generatedCode = GenerateCode();

    // Check uniqueness in database
    if (!await _codeRepo.Query().AnyAsync(c => c.Code == generatedCode))
        return generatedCode;

    attempts++;
}

throw new ApplicationException("Failed to generate unique code");
```

### 12.3 Data Privacy

#### Phone Number Masking
```csharp
public class ReferralStatsDto
{
    public List<RecentReferralDto> RecentReferrals { get; set; }
}

public class RecentReferralDto
{
    public string ReferralCode { get; set; }

    // Masked: 05321234567 → 053********67
    public string RecipientPhone => MaskPhoneNumber(_recipientPhone);

    private string MaskPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 4)
            return "***";

        return phone.Substring(0, 3) + new string('*', phone.Length - 5) +
               phone.Substring(phone.Length - 2);
    }
}
```

#### Logging Sanitization
```csharp
_logger.LogInformation(
    "Referral generated: User={UserId}, Code={Code}, Recipients={RecipientCount}",
    userId,
    code,
    phoneNumbers.Count);  // Don't log phone numbers

// BAD: Don't do this
_logger.LogInformation("Sending to: {Phones}", string.Join(",", phoneNumbers));
```

### 12.4 Database Security

#### SQL Injection Prevention
- All queries use parameterized EF Core queries
- No raw SQL with string concatenation
- Repository pattern enforces safe data access

#### Constraint-Based Validation
```sql
-- Database-level constraints
ALTER TABLE public."ReferralCodes"
    ADD CONSTRAINT "CK_ReferralCodes_ExpiresAt_Future"
    CHECK ("ExpiresAt" > "CreatedAt");

ALTER TABLE public."ReferralTracking"
    ADD CONSTRAINT "CK_ReferralTracking_StatusProgression"
    CHECK (
        ("ClickedAt" IS NULL OR "RegisteredAt" IS NULL OR "RegisteredAt" >= "ClickedAt") AND
        ("RegisteredAt" IS NULL OR "FirstAnalysisAt" IS NULL OR "FirstAnalysisAt" >= "RegisteredAt")
    );
```

---

## 13. Deployment Guide

### 13.1 Staging Deployment

#### Step 1: Database Migration
```bash
# Backup staging database
pg_dump -h staging-db-host -U ziraai -d ziraai_staging > backup_staging_$(date +%Y%m%d).sql

# Apply migration
psql -h staging-db-host -U ziraai -d ziraai_staging -f add_referral_system.sql

# Verify tables created
psql -h staging-db-host -U ziraai -d ziraai_staging -c "\dt Referral*"
```

#### Step 2: Configuration Setup
```json
// appsettings.Staging.json
{
  "ReferralSystem": {
    "Enabled": true,
    "DeepLinkBaseUrl": "https://staging.ziraai.com/ref/",
    "PlayStoreAppId": "com.ziraai.staging"
  },
  "SmsService": {
    "Provider": "Mock",  // Use mock for staging
    "MockSettings": {
      "UseFixedCode": true,
      "FixedCode": "123456"
    }
  },
  "WhatsApp": {
    "Enabled": false  // Disable for staging
  }
}
```

#### Step 3: Deploy Application
```bash
# Build
dotnet publish -c Release -o ./publish

# Deploy to staging server
scp -r ./publish/* user@staging-server:/var/www/ziraai

# Restart service
ssh user@staging-server "sudo systemctl restart ziraai"
```

#### Step 4: Smoke Tests
```bash
# Test referral link generation
curl -X POST https://staging.ziraai.com/api/v1/referral/generate-link \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "deliveryMethod": "SMS",
    "recipientPhoneNumbers": ["05321234567"]
  }'

# Test referral stats
curl -X GET https://staging.ziraai.com/api/v1/referral/stats \
  -H "Authorization: Bearer {token}"
```

### 13.2 Production Deployment

#### Step 1: Pre-Deployment Checklist
- [ ] All unit tests passing
- [ ] Integration tests passing
- [ ] E2E tests passing
- [ ] Performance tests passing
- [ ] Security audit completed
- [ ] Load testing completed
- [ ] Staging deployment successful for 7+ days
- [ ] Database backup taken
- [ ] Rollback plan documented

#### Step 2: Database Migration
```bash
# Backup production database
pg_dump -h prod-db-host -U ziraai -d ziraai_prod > backup_prod_$(date +%Y%m%d).sql

# Test migration on copy (CRITICAL)
createdb ziraai_prod_copy
pg_restore -d ziraai_prod_copy backup_prod_$(date +%Y%m%d).sql
psql -d ziraai_prod_copy -f add_referral_system.sql

# Verify migration success on copy
psql -d ziraai_prod_copy -c "SELECT COUNT(*) FROM \"ReferralCodes\""

# Apply to production (if copy successful)
psql -h prod-db-host -U ziraai -d ziraai_prod -f add_referral_system.sql
```

#### Step 3: Configuration Setup
```json
// appsettings.Production.json
{
  "ReferralSystem": {
    "Enabled": true,
    "DeepLinkBaseUrl": "https://ziraai.com/ref/",
    "PlayStoreAppId": "com.ziraai"
  },
  "SmsService": {
    "Provider": "Twilio",  // Real SMS provider
    "Twilio": {
      "AccountSid": "{{secret}}",
      "AuthToken": "{{secret}}",
      "FromNumber": "+905321234567"
    }
  },
  "WhatsApp": {
    "Enabled": true,
    "Provider": "Twilio",
    "Twilio": {
      "AccountSid": "{{secret}}",
      "AuthToken": "{{secret}}",
      "FromNumber": "whatsapp:+905321234567"
    }
  }
}
```

#### Step 4: Gradual Rollout Strategy

**Phase 1: Backend Only (Day 1)**
- Deploy backend with referral system enabled
- Mobile app doesn't have referral feature yet
- Monitor error rates and performance
- Only API endpoints accessible

**Phase 2: Limited Release (Day 3-7)**
- Deploy mobile app update to 10% of users (via Play Store staged rollout)
- Monitor usage patterns
- Monitor error rates
- Collect feedback

**Phase 3: Full Rollout (Day 7+)**
- Deploy mobile app to 100% of users
- Announce feature to all users
- Monitor metrics

#### Step 5: Monitoring Setup
```csharp
// Add Application Insights
services.AddApplicationInsightsTelemetry(options =>
{
    options.InstrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];
});

// Custom metrics
_telemetryClient.TrackMetric("Referral.LinkGenerated", 1);
_telemetryClient.TrackMetric("Referral.ClickTracked", 1);
_telemetryClient.TrackMetric("Referral.RewardProcessed", creditAmount);
```

#### Step 6: Alerts Configuration
```yaml
# Azure Monitor Alerts
alerts:
  - name: HighReferralErrorRate
    condition: ErrorRate > 5%
    window: 5m
    severity: High

  - name: ReferralRewardProcessingFailed
    condition: FailedRewards > 10
    window: 10m
    severity: Critical

  - name: SuspiciousReferralActivity
    condition: ReferralsPerUser > 100
    window: 1h
    severity: Warning
```

### 13.3 Rollback Plan

#### If Issues Detected:
```bash
# 1. Disable referral system via configuration (no code deployment needed)
# Update configuration in database
UPDATE public."ReferralConfigurations"
SET "Value" = 'false'
WHERE "Key" = 'Referral.Enabled';

# 2. If database issues:
# Restore database from backup
pg_restore -d ziraai_prod backup_prod_YYYYMMDD.sql

# 3. If application issues:
# Rollback to previous version
ssh user@prod-server "sudo systemctl stop ziraai"
ssh user@prod-server "cp -r /var/www/ziraai_backup_YYYYMMDD /var/www/ziraai"
ssh user@prod-server "sudo systemctl start ziraai"
```

---

## Appendices

### A. Database ERD

```
┌─────────────────────┐
│       Users         │
├─────────────────────┤
│ UserId (PK)         │
│ MobilePhones        │
│ FullName            │
│ RegistrationReferral│────┐
│ Code                │    │
└─────────────────────┘    │
         │                 │
         │ 1:N             │
         ▼                 │
┌─────────────────────┐    │
│  ReferralCodes      │    │
├─────────────────────┤    │
│ Id (PK)             │    │
│ UserId (FK)         │────┘
│ Code (UNIQUE)       │
│ IsActive            │
│ CreatedAt           │
│ ExpiresAt           │
│ Status              │
└─────────────────────┘
         │
         │ 1:N
         ▼
┌─────────────────────┐
│ ReferralTracking    │
├─────────────────────┤
│ Id (PK)             │
│ ReferralCodeId (FK) │
│ RefereeUserId (FK)  │───┐
│ ClickedAt           │   │
│ RegisteredAt        │   │
│ FirstAnalysisAt     │   │
│ RewardProcessedAt   │   │
│ Status              │   │
│ RefereeMobilePhone  │   │
│ IpAddress           │   │
└─────────────────────┘   │
         │                │
         │ 1:1            │
         ▼                │
┌─────────────────────┐   │
│  ReferralRewards    │   │
├─────────────────────┤   │
│ Id (PK)             │   │
│ ReferralTrackingId  │   │
│ ReferrerUserId (FK) │───┤
│ RefereeUserId (FK)  │───┘
│ CreditAmount        │
│ AwardedAt           │
│ SubscriptionId (FK) │
└─────────────────────┘
         │
         ▼
┌─────────────────────┐
│   Subscriptions     │
├─────────────────────┤
│ Id (PK)             │
│ UserId (FK)         │
│ ReferralCredits     │ ← NEW FIELD
│ DailyRequestLimit   │
│ DailyUsage          │
└─────────────────────┘
```

### B. State Transition Diagrams

#### Referral Code Status
```
[Active] ──────────────────────────────────────┐
   │                                           │
   │ ExpiresAt < Now                           │
   ▼                                           │
[Expired]                                      │
   │                                           │
   │ Admin action                Admin action  │
   ▼                                           ▼
[Disabled] ◄──────────────────────────────[Disabled]
```

#### Referral Tracking Status
```
[Clicked] ──User clicks link
   │
   │ User registers with code
   ▼
[Registered]
   │
   │ User completes 1st analysis
   ▼
[Validated]
   │
   │ Reward processing completes
   ▼
[Rewarded] ◄── Final state
```

### C. Configuration Reference

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Referral.CreditPerReferral` | int | 10 | Credits awarded per successful referral |
| `Referral.LinkExpiryDays` | int | 30 | Days before link expires |
| `Referral.MinAnalysisForValidation` | int | 1 | Minimum analyses for validation |
| `Referral.MaxReferralsPerUser` | int | 0 | Max referrals per user (0 = unlimited) |
| `Referral.EnableWhatsApp` | bool | true | Enable WhatsApp delivery |
| `Referral.EnableSMS` | bool | true | Enable SMS delivery |
| `Referral.CodePrefix` | string | "ZIRA" | Prefix for referral codes |
| `Referral.DeepLinkBaseUrl` | string | "https://ziraai.com/ref/" | Base URL for deep links |

### D. API Rate Limits

| Endpoint | Rate Limit | Per |
|----------|------------|-----|
| `POST /generate-link` | 10 | Hour |
| `GET /stats` | 60 | Hour |
| `POST /track-click` | 100 | Hour (per IP) |
| `POST /validate-code` | 30 | Hour |

### E. Error Codes

| Code | Message | HTTP Status |
|------|---------|-------------|
| `REF001` | Referral code expired | 400 |
| `REF002` | Invalid referral code | 400 |
| `REF003` | Cannot use own referral code | 400 |
| `REF004` | Referral code already used | 400 |
| `REF005` | Phone number already registered | 400 |
| `REF006` | Rate limit exceeded | 429 |
| `REF007` | SMS delivery failed | 500 |
| `REF008` | Reward processing failed | 500 |

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-02 | Claude Code | Initial comprehensive design document |

---

**END OF DOCUMENT**
