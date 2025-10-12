# 🎯 Sponsor Persona - Complete Journey & Actions Report

**Document Version:** 1.0
**Last Updated:** 2025-10-10
**Prepared For:** Development & Product Team

---

## 📋 Executive Summary

This document provides a comprehensive persona-based analysis of sponsor capabilities within the ZiraAI platform. It details every action a sponsor can perform, constraints they operate under, and the complete user journey from initial registration to advanced feature usage.

### Key Insights
- **4 Tier Levels**: S, M, L, XL with progressive feature unlocking
- **Purchase-Based Model**: Sponsors buy packages and distribute codes to farmers
- **Tier-Based Access**: Data visibility increases from 30% (S) → 60% (M) → 100% (L/XL)
- **Communication**: Messaging available only in L and XL tiers
- **Smart Links**: Exclusive to XL tier (AI-powered product recommendations)
- **Multi-Channel Distribution**: SMS and WhatsApp code distribution with tracking

---

## 👤 Sponsor Persona Definition

### Primary Persona: "Agricultural Enterprise Sponsor"
**Name:** Mehmet Yılmaz
**Role:** Marketing Director at AgriTech Solutions
**Company Type:** Agricultural input supplier (seeds, fertilizers, pesticides)
**Business Model:** B2B2C (Sell through dealers, market to farmers)
**Goals:**
- Increase brand awareness among farmers
- Gather market intelligence on crop health and disease patterns
- Provide value to farmers while building loyalty
- Track ROI on marketing investments

### Pain Points Addressed by ZiraAI Sponsorship
- ❌ No direct channel to end-user farmers (dealers control relationship)
- ❌ Limited data on actual crop problems farmers face
- ❌ High cost of traditional marketing (radio, print ads)
- ❌ Difficulty measuring marketing campaign effectiveness
- ✅ ZiraAI provides: Direct farmer engagement, data insights, measurable ROI

---

## 🚀 Complete Sponsor Journey

### Phase 1: Registration & Onboarding

#### 1.1 Initial Registration (Any User Can Become Sponsor)

**Starting Point:** User has registered via:
- Phone number + SMS OTP (most common for Turkish market)
- Email + password (traditional)
- Google/Apple social login

**Initial Role:** `Farmer` (default role for all new users)

**Account Details:**
```json
{
  "userId": 1001,
  "fullName": "Mehmet Yılmaz",
  "email": "mehmet@agritech.com" or "05321234567@phone.ziraai.com",
  "mobilePhone": "+905321234567",
  "roles": ["Farmer"],
  "authenticationProviderType": "Phone" or "Email" or "Google"
}
```

#### 1.2 Create Sponsor Profile (Required First Step)

**Endpoint:** `POST /api/v1/sponsorship/create-profile`

**Authorization:**
- ✅ Any authenticated user (Farmer can upgrade to Sponsor)
- ❌ No role requirement (this creates the Sponsor role)

**Required Information:**
```json
{
  "companyName": "AgriTech Solutions A.Ş.",
  "contactEmail": "support@agritech.com",
  "password": "SecurePass123",  // REQUIRED for phone-registered users
  "companyDescription": "Leading agricultural input supplier in Turkey",
  "sponsorLogoUrl": "https://cdn.ziraai.com/logos/agritech.png",
  "websiteUrl": "https://agritech.com.tr",
  "contactPhone": "+905321234567",
  "contactPerson": "Mehmet Yılmaz",
  "companyType": "Manufacturer",
  "businessModel": "B2B2C"
}
```

**What Happens:**
1. ✅ System checks if user already has sponsor profile (prevents duplicates)
2. ✅ Creates/updates `SponsorProfile` entity
3. ✅ Adds `Sponsor` role to user (can still use Farmer features)
4. ✅ For phone-registered users: Sets password for traditional login
5. ✅ Updates email if provided (phone users get real email)
6. ✅ Initializes statistics: `TotalPurchases=0`, `TotalCodesGenerated=0`

**Outcome:**
```json
{
  "success": true,
  "message": "Sponsor profile created successfully",
  "data": {
    "id": 501,
    "sponsorId": 1001,
    "companyName": "AgriTech Solutions A.Ş.",
    "isActive": true,
    "isVerified": false,  // Admin verification pending
    "createdDate": "2025-10-10T10:00:00Z"
  }
}
```

**User State After Profile Creation:**
- ✅ Roles: `["Farmer", "Sponsor"]` (dual-role capability)
- ✅ Can login with: Phone+OTP OR Email+Password (if password set)
- ✅ Access to: All Farmer features + Sponsor dashboard
- ⚠️ Cannot purchase packages until admin verification (optional requirement)

**Constraints:**
- ❌ Cannot create multiple sponsor profiles (one per user)
- ❌ Company name must be unique (business rule)
- ✅ Profile can be updated anytime with PUT request

---

### Phase 2: Package Purchase & Code Generation

#### 2.1 View Available Subscription Tiers

**Endpoint:** `GET /api/v1/subscription-tiers` (public endpoint)

**Available Tiers:**
```javascript
[
  {
    "id": 1,
    "tierName": "S",
    "displayName": "Small",
    "description": "Basic visibility package",
    "monthlyPrice": 50.00,
    "yearlyPrice": 500.00,
    "currency": "TRY",
    "dailyRequestLimit": 10,
    "monthlyRequestLimit": 300,
    "features": {
      "dataVisibility": "30%",
      "logoDisplay": ["Start Screen"],
      "messaging": false,
      "smartLinks": false,
      "profileVisibility": "Anonymous"
    }
  },
  {
    "id": 2,
    "tierName": "M",
    "displayName": "Medium",
    "monthlyPrice": 100.00,
    "yearlyPrice": 1000.00,
    "features": {
      "dataVisibility": "60%",
      "logoDisplay": ["Start Screen", "Result Screen"],
      "messaging": false,
      "smartLinks": false,
      "profileVisibility": "Anonymous"
    }
  },
  {
    "id": 3,
    "tierName": "L",
    "displayName": "Large",
    "monthlyPrice": 200.00,
    "yearlyPrice": 2000.00,
    "features": {
      "dataVisibility": "100%",
      "logoDisplay": ["Start Screen", "Result Screen", "Analysis Details", "Profile"],
      "messaging": true,
      "smartLinks": false,
      "profileVisibility": "Visible"
    }
  },
  {
    "id": 4,
    "tierName": "XL",
    "displayName": "Extra Large",
    "monthlyPrice": 500.00,
    "yearlyPrice": 5000.00,
    "features": {
      "dataVisibility": "100%",
      "logoDisplay": ["Start Screen", "Result Screen", "Analysis Details", "Profile"],
      "messaging": true,
      "smartLinks": true,  // EXCLUSIVE FEATURE
      "smartLinkQuota": 50,
      "profileVisibility": "Visible"
    }
  }
]
```

#### 2.2 Purchase Bulk Package

**Endpoint:** `POST /api/v1/sponsorship/purchase-package`

**Authorization:** `Sponsor` or `Admin` role required

**Request Example:**
```json
{
  "subscriptionTierId": 3,  // L tier
  "quantity": 100,  // 100 sponsorship codes
  "totalAmount": 20000.00,  // 100 × 200 TRY
  "paymentMethod": "CreditCard",
  "paymentReference": "IYZICO-TXN-789456123",
  "companyName": "AgriTech Solutions A.Ş.",
  "invoiceAddress": "Atatürk Cad. No:123 Ankara",
  "taxNumber": "1234567890",
  "codePrefix": "AGRI",  // Optional: custom prefix
  "validityDays": 365,  // Optional: default 365 days
  "notes": "Q4 2025 farmer outreach campaign"
}
```

**System Processing:**
1. ✅ Validates sponsor has active profile
2. ✅ Validates subscription tier exists and is active
3. ✅ Creates `SponsorshipPurchase` record
4. ✅ Sets payment status to "Completed" (payment gateway integration assumed)
5. ✅ Generates exactly `quantity` unique codes
6. ✅ Updates sponsor profile statistics
7. ✅ Returns purchase details with all generated codes

**Code Generation Logic:**
```csharp
// Format: {PREFIX}-{YEAR}-{RANDOM}
// Example: AGRI-2025-X3K9, AGRI-2025-P7M2, AGRI-2025-J5N8

for (int i = 0; i < quantity; i++)
{
    var code = new SponsorshipCode
    {
        Code = $"{codePrefix}-{DateTime.Now.Year}-{GenerateRandomString(4)}",
        SponsorId = sponsorId,
        SubscriptionTierId = tierId,
        SponsorshipPurchaseId = purchaseId,
        CreatedDate = DateTime.Now,
        ExpiryDate = DateTime.Now.AddDays(validityDays),
        IsActive = true,
        IsUsed = false
    };
    // Save to database
}
```

**Response:**
```json
{
  "success": true,
  "message": "100 sponsorship codes generated successfully",
  "data": {
    "id": 2001,
    "sponsorId": 1001,
    "subscriptionTierId": 3,
    "tierName": "L",
    "quantity": 100,
    "unitPrice": 200.00,
    "totalAmount": 20000.00,
    "currency": "TRY",
    "purchaseDate": "2025-10-10T10:30:00Z",
    "paymentStatus": "Completed",
    "codesGenerated": 100,
    "codesUsed": 0,
    "validityDays": 365,
    "generatedCodes": [
      {
        "id": 10001,
        "code": "AGRI-2025-X3K9",
        "tierName": "L",
        "isUsed": false,
        "isActive": true,
        "expiryDate": "2026-10-10T10:30:00Z",
        "redemptionLink": "https://ziraai.com/redeem/AGRI-2025-X3K9"
      },
      // ... 99 more codes
    ]
  }
}
```

**Sponsor Profile Statistics Updated:**
```json
{
  "totalPurchases": 1,
  "totalCodesGenerated": 100,
  "totalCodesRedeemed": 0,
  "totalInvestment": 20000.00,
  "lastPurchaseDate": "2025-10-10T10:30:00Z"
}
```

**Constraints:**
- ❌ Cannot purchase negative or zero quantity
- ❌ Payment must be completed (integration with payment gateway)
- ✅ Can purchase multiple packages over time
- ✅ Different tiers can be mixed (e.g., 50 M-tier + 100 L-tier)
- ⚠️ Codes expire after `validityDays` (default: 365 days)

---

### Phase 3: Code Distribution

#### 3.1 View Generated Codes

**Endpoint:** `GET /api/v1/sponsorship/codes?onlyUnused=true`

**Authorization:** `Sponsor` or `Admin` role

**Query Parameters:**
- `onlyUnused`: `true` (available codes) or `false` (all codes)

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 10001,
      "code": "AGRI-2025-X3K9",
      "sponsorId": 1001,
      "subscriptionTierId": 3,
      "tierName": "L",
      "isUsed": false,
      "isActive": true,
      "expiryDate": "2026-10-10T10:30:00Z",
      "redemptionLink": "https://ziraai.com/redeem/AGRI-2025-X3K9",
      "recipientPhone": null,
      "recipientName": null,
      "linkSentDate": null,
      "linkClickCount": 0
    }
    // ... more codes
  ]
}
```

#### 3.2 Send Codes via SMS/WhatsApp (Bulk Distribution)

**Endpoint:** `POST /api/v1/sponsorship/send-link`

**Authorization:** `Sponsor` or `Admin` role

**Request Example:**
```json
{
  "recipients": [
    {
      "code": "AGRI-2025-X3K9",
      "phone": "+905321111111",
      "name": "Ali Kaya"
    },
    {
      "code": "AGRI-2025-P7M2",
      "phone": "+905322222222",
      "name": "Ayşe Demir"
    }
    // ... up to 100 recipients per request
  ],
  "channel": "SMS",  // or "WhatsApp"
  "customMessage": "AgriTech Solutions olarak size özel Premium üyelik hediye ediyoruz!"
}
```

**System Processing:**
1. ✅ Validates all codes belong to sponsor
2. ✅ Validates codes are unused and not expired
3. ✅ Formats phone numbers to international format (+90...)
4. ✅ Generates redemption links for each code
5. ✅ Sends via NetGSM (SMS) or WhatsApp Business API
6. ✅ Tracks delivery status for each recipient
7. ✅ Updates code records with distribution details

**SMS Message Template:**
```
Merhaba Ali Kaya! 🌱

AgriTech Solutions olarak size özel Premium üyelik hediye ediyoruz!

Kodunuz: AGRI-2025-X3K9

ZiraAI uygulamasını indirin ve kodu kullanarak 30 gün boyunca sınırsız bitki analizi yapın.

İndirmek için: https://ziraai.com/redeem/AGRI-2025-X3K9

Bu bağlantıya tıklayarak uygulamayı açın ve kod otomatik olarak girilecektir. ✨

İyi günler dileriz! 🚜
AgriTech Solutions
```

**Deferred Deep Linking Flow:**
1. Farmer clicks link: `https://ziraai.com/redeem/AGRI-2025-X3K9`
2. If app installed: Opens app → auto-fills code → shows redemption screen
3. If app not installed: Redirects to App Store/Play Store → installs → opens app → auto-fills code

**Response:**
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
        "code": "AGRI-2025-X3K9",
        "phone": "+905321111111",
        "success": true,
        "deliveryStatus": "Sent",
        "errorMessage": null
      },
      {
        "code": "AGRI-2025-P7M2",
        "phone": "+905322222222",
        "success": true,
        "deliveryStatus": "Sent",
        "errorMessage": null
      }
    ]
  }
}
```

**Code Records Updated:**
```json
{
  "id": 10001,
  "code": "AGRI-2025-X3K9",
  "recipientPhone": "+905321111111",
  "recipientName": "Ali Kaya",
  "linkSentDate": "2025-10-10T11:00:00Z",
  "linkSentVia": "SMS",
  "linkDelivered": true,
  "distributionChannel": "SMS",
  "distributionDate": "2025-10-10T11:00:00Z",
  "distributedTo": "Ali Kaya (+905321111111)"
}
```

**Constraints:**
- ✅ Maximum 100 recipients per request (prevent spam)
- ❌ Cannot send same code to multiple recipients
- ❌ Cannot send if code already used or expired
- ⚠️ SMS delivery depends on NetGSM service availability
- ⚠️ WhatsApp requires pre-approved message template

#### 3.3 Track Link Performance

**Endpoint:** `GET /api/v1/sponsorship/link-statistics?startDate=2025-10-01&endDate=2025-10-31`

**Authorization:** `Sponsor` or `Admin` role

**Response:**
```json
{
  "success": true,
  "data": {
    "totalCodesSent": 100,
    "totalLinksClicked": 75,
    "totalCodesRedeemed": 50,
    "clickThroughRate": 0.75,  // 75%
    "redemptionRate": 0.50,  // 50%
    "averageTimeToRedeem": "2.5 days",
    "byChannel": {
      "SMS": {
        "sent": 60,
        "clicked": 45,
        "redeemed": 30,
        "redemptionRate": 0.50
      },
      "WhatsApp": {
        "sent": 40,
        "clicked": 30,
        "redeemed": 20,
        "redemptionRate": 0.50
      }
    },
    "topPerformingCodes": [
      {
        "code": "AGRI-2025-X3K9",
        "recipientName": "Ali Kaya",
        "sentDate": "2025-10-10T11:00:00Z",
        "clickCount": 3,
        "redeemedDate": "2025-10-10T12:30:00Z",
        "timeToRedeem": "1.5 hours"
      }
    ]
  }
}
```

---

### Phase 4: Farmer Data Access (Tier-Based)

#### 4.1 View Sponsored Farmers

**Endpoint:** `GET /api/v1/sponsorship/farmers`

**Authorization:** `Sponsor` or `Admin` role

**What Data Is Visible?**

Depends on the **tier of the redeemed code**, not the sponsor's current purchase tier.

**S Tier (30% Data Visibility):**
```json
{
  "success": true,
  "data": [
    {
      "farmerId": 5001,
      "farmerName": "Anonymous User",  // ❌ Hidden
      "farmerEmail": null,  // ❌ Hidden
      "farmerPhone": null,  // ❌ Hidden
      "location": {
        "city": "Ankara",  // ✅ Visible
        "district": null  // ❌ Hidden
      },
      "redeemedCode": "AGRI-2025-X3K9",
      "redeemedDate": "2025-10-10T12:30:00Z",
      "subscriptionStatus": "Active",
      "subscriptionEndDate": "2025-11-09T12:30:00Z",
      "totalAnalysisCount": 15,  // ✅ Visible
      "lastAnalysisDate": "2025-10-15T14:00:00Z"
    }
  ]
}
```

**M Tier (60% Data Visibility):**
```json
{
  "farmerId": 5001,
  "farmerName": "Anonymous User",  // ❌ Still hidden
  "farmerEmail": null,  // ❌ Hidden
  "farmerPhone": null,  // ❌ Hidden
  "location": {
    "city": "Ankara",  // ✅ Visible
    "district": "Çankaya"  // ✅ NOW VISIBLE
  },
  "cropTypes": ["Buğday", "Arpa", "Mısır"],  // ✅ NOW VISIBLE
  "totalAnalysisCount": 15,
  "diseaseCategories": [  // ✅ NOW VISIBLE
    {
      "category": "Fungal",
      "count": 8
    },
    {
      "category": "Pest",
      "count": 5
    },
    {
      "category": "Nutrient Deficiency",
      "count": 2
    }
  ]
}
```

**L Tier (100% Data Visibility):**
```json
{
  "farmerId": 5001,
  "farmerName": "Ali Kaya",  // ✅ NOW VISIBLE
  "farmerEmail": "ali.kaya@example.com",  // ✅ NOW VISIBLE
  "farmerPhone": "+905321111111",  // ✅ NOW VISIBLE
  "location": {
    "city": "Ankara",
    "district": "Çankaya",
    "coordinates": {  // ✅ NOW VISIBLE
      "latitude": 39.9208,
      "longitude": 32.8541
    }
  },
  "farmDetails": {  // ✅ NOW VISIBLE
    "farmSize": "50 dekar",
    "mainCrops": ["Buğday", "Arpa"]
  },
  "cropTypes": ["Buğday", "Arpa", "Mısır"],
  "totalAnalysisCount": 15,
  "diseaseCategories": [...],
  "analyses": [  // ✅ Full analysis details now accessible
    {
      "id": 123,
      "cropType": "Buğday",
      "disease": "Wheat Rust",
      "severity": "Moderate",
      "date": "2025-10-15T14:00:00Z",
      "recommendations": [...]
    }
  ]
}
```

**XL Tier (100% Data Visibility + Smart Links):**
- Same as L tier data access
- **Additional Capability:** Can create Smart Links (see Phase 6)

#### 4.2 View Sponsored Farmer's Analysis

**Endpoint:** `GET /api/v1/sponsorship/analysis/{plantAnalysisId}`

**Authorization:** `Sponsor` or `Admin` role

**Access Control:**
- ✅ Can view if farmer used sponsor's code for active subscription
- ❌ Cannot view if different sponsor or no sponsorship
- Filtered based on redeemed code tier (30%/60%/100%)

**S Tier Analysis Response (30% Visibility):**
```json
{
  "success": true,
  "data": {
    "id": 123,
    "farmerName": "Anonymous",  // ❌ Hidden
    "analysisDate": "2025-10-15T14:00:00Z",
    "cropType": "Buğday",  // ✅ Visible
    "location": {
      "city": "Ankara"  // ✅ Visible
    },
    // ❌ No detailed analysis data
    // ❌ No images
    // ❌ No recommendations
  }
}
```

**M Tier Analysis Response (60% Visibility):**
```json
{
  "id": 123,
  "farmerName": "Anonymous",  // ❌ Still hidden
  "cropType": "Buğday",
  "location": {
    "city": "Ankara",
    "district": "Çankaya"  // ✅ NOW VISIBLE
  },
  "diseaseCategory": "Fungal",  // ✅ NOW VISIBLE
  "severity": "Moderate",  // ✅ NOW VISIBLE
  // ❌ Still no images
  // ❌ Still no detailed recommendations
}
```

**L/XL Tier Analysis Response (100% Visibility):**
```json
{
  "id": 123,
  "farmerName": "Ali Kaya",  // ✅ NOW VISIBLE
  "farmerPhone": "+905321111111",  // ✅ NOW VISIBLE
  "cropType": "Buğday",
  "location": {
    "city": "Ankara",
    "district": "Çankaya",
    "coordinates": {
      "latitude": 39.9208,
      "longitude": 32.8541
    }
  },
  "diseaseCategory": "Fungal",
  "diseaseName": "Wheat Rust (Puccinia triticina)",  // ✅ Full details
  "severity": "Moderate",
  "confidence": 0.92,
  "images": [  // ✅ NOW VISIBLE
    "https://cdn.ziraai.com/analysis/123/image1.jpg"
  ],
  "aiAnalysis": {  // ✅ Full AI response
    "diagnosis": "Wheat rust fungal infection detected...",
    "symptoms": [...],
    "causes": [...],
    "recommendations": [...]
  },
  "recommendations": [
    {
      "type": "Chemical",
      "product": "Fungicide XYZ",
      "dosage": "100ml/10L water",
      "timing": "Apply immediately"
    }
  ]
}
```

**Constraints:**
- ❌ Cannot access analyses of farmers who didn't use sponsor's codes
- ❌ Cannot access if farmer's sponsored subscription expired
- ✅ Access remains even if sponsor doesn't renew (already-redeemed codes honored)
- ⚠️ Data visibility is per-code, not per-sponsor (sponsor can have mixed tiers)

---

### Phase 5: Farmer Communication (L & XL Tiers Only)

#### 5.1 Check Messaging Permission

**Logic:**
```csharp
public async Task<bool> CanSendMessageAsync(int sponsorId)
{
    var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);

    if (profile == null || !profile.IsActive)
        return false;

    // Check if sponsor has any L or XL tier purchases
    foreach (var purchase in profile.SponsorshipPurchases)
    {
        if (purchase.SubscriptionTierId >= 3)  // L=3, XL=4
            return true;
    }

    return false;
}
```

**Tier Permissions:**
- ❌ S Tier: No messaging
- ❌ M Tier: No messaging (farmer profile anonymous)
- ✅ L Tier: Can send messages
- ✅ XL Tier: Can send messages

#### 5.2 Send Message to Farmer

**Endpoint:** `POST /api/v1/sponsorship/messages`

**Authorization:** `Sponsor` or `Admin` role + L/XL tier purchase required

**Request:**
```json
{
  "toUserId": 5001,  // Farmer user ID
  "plantAnalysisId": 123,  // Analysis context
  "message": "Merhaba Ali Bey, analizinizde buğday pası tespit ettik. Önerimiz XYZ Fungisit ürünümüzü kullanmanız. Detaylı bilgi için 0532-123-4567 numaralı hattımızdan bize ulaşabilirsiniz.",
  "messageType": "ProductRecommendation"
}
```

**System Processing:**
1. ✅ Validates sponsor has L or XL tier purchase
2. ✅ Validates farmer used sponsor's code
3. ✅ Creates `AnalysisMessage` entity
4. ✅ Sends in-app notification to farmer
5. ✅ Optionally sends SMS/email notification

**Message Entity:**
```json
{
  "id": 9001,
  "plantAnalysisId": 123,
  "fromUserId": 1001,  // Sponsor
  "toUserId": 5001,  // Farmer
  "message": "Merhaba Ali Bey...",
  "messageType": "ProductRecommendation",
  "sentDate": "2025-10-15T15:00:00Z",
  "isRead": false,
  "senderRole": "Sponsor",
  "senderName": "AgriTech Solutions",
  "senderCompany": "AgriTech Solutions A.Ş.",
  "priority": "Normal",
  "isApproved": true  // Auto-approved for verified sponsors
}
```

**Farmer Sees In App:**
```
📨 Yeni Mesaj (AgriTech Solutions)

Analiziniz hakkında tavsiye:
"Merhaba Ali Bey, analizinizde buğday pası tespit ettik..."

[Cevapla] [Mesajları Gör]
```

**Constraints:**
- ❌ S Tier: Cannot send messages (tier too low)
- ❌ M Tier: Cannot send messages (farmer anonymous)
- ❌ Cannot send if farmer blocked sponsor
- ❌ Cannot send spam (rate limit: 10 messages/day per farmer)
- ✅ Farmer can reply (creates conversation thread)
- ⚠️ Admin moderation may apply for first messages (anti-spam)

#### 5.3 View Conversation

**Endpoint:** `GET /api/v1/sponsorship/messages/conversation?farmerId=5001&plantAnalysisId=123`

**Authorization:** `Sponsor` or `Admin` or `Farmer` (participants only)

**Response:**
```json
{
  "success": true,
  "data": {
    "plantAnalysisId": 123,
    "participants": [
      {
        "userId": 1001,
        "role": "Sponsor",
        "name": "AgriTech Solutions",
        "companyName": "AgriTech Solutions A.Ş."
      },
      {
        "userId": 5001,
        "role": "Farmer",
        "name": "Ali Kaya"
      }
    ],
    "messages": [
      {
        "id": 9001,
        "fromUserId": 1001,
        "message": "Merhaba Ali Bey...",
        "sentDate": "2025-10-15T15:00:00Z",
        "isRead": true,
        "readDate": "2025-10-15T15:05:00Z"
      },
      {
        "id": 9002,
        "fromUserId": 5001,
        "message": "Teşekkür ederim, ürününüzü nereden alabilirim?",
        "sentDate": "2025-10-15T15:10:00Z",
        "isRead": true
      },
      {
        "id": 9003,
        "fromUserId": 1001,
        "message": "Size en yakın bayimiz: Çankaya Tarım - 0312-XXX-XXXX",
        "sentDate": "2025-10-15T15:15:00Z",
        "isRead": false
      }
    ]
  }
}
```

---

### Phase 6: Smart Links (XL Tier Exclusive)

#### 6.1 Check Smart Link Permission

**Logic:**
```csharp
public async Task<bool> CanCreateSmartLinksAsync(int sponsorId)
{
    var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);

    if (profile == null || !profile.IsActive)
        return false;

    // Only XL tier (ID=4) can create smart links
    foreach (var purchase in profile.SponsorshipPurchases)
    {
        if (purchase.SubscriptionTierId == 4)  // XL tier
            return true;
    }

    return false;
}
```

**Tier Permissions:**
- ❌ S Tier: No smart links
- ❌ M Tier: No smart links
- ❌ L Tier: No smart links
- ✅ XL Tier: Can create up to 50 smart links

#### 6.2 Create Smart Link

**Endpoint:** `POST /api/v1/sponsorship/smart-links`

**Authorization:** `Sponsor` or `Admin` role + XL tier purchase required

**What Are Smart Links?**
AI-powered contextual product recommendations that appear in farmer analysis results based on:
- Crop type match
- Disease/pest match
- Keyword match in AI analysis
- Geographic relevance
- Seasonal relevance

**Request:**
```json
{
  "linkUrl": "https://agritech.com.tr/products/fungicide-xyz",
  "linkText": "XYZ Fungisit - Buğday Pası İçin",
  "linkDescription": "Etkili ve hızlı sonuç veren fungisit çözümü",
  "linkType": "Product",
  "keywords": ["buğday pası", "fungal hastalık", "wheat rust", "fungicide"],
  "productCategory": "Fungicide",
  "targetCropTypes": ["Buğday", "Arpa"],
  "targetDiseases": ["Wheat Rust", "Leaf Rust"],
  "targetPests": [],
  "priority": 80,  // 0-100, higher = more likely to show
  "displayPosition": "Inline",  // "Inline", "Sidebar", "Bottom"
  "displayStyle": "Button",  // "Button", "Card", "Banner"
  "productName": "AgriTech XYZ Fungisit",
  "productPrice": 250.00,
  "productCurrency": "TRY",
  "isPromotional": true,
  "discountPercentage": 15.0
}
```

**System Processing:**
1. ✅ Validates sponsor has XL tier purchase
2. ✅ Checks quota: max 50 active smart links per sponsor
3. ✅ Creates `SmartLink` entity (IsApproved=false by default)
4. ✅ Admin reviews and approves link (anti-spam protection)
5. ✅ Once approved, link starts appearing in relevant analyses

**Smart Link Entity:**
```json
{
  "id": 7001,
  "sponsorId": 1001,
  "sponsorName": "AgriTech Solutions",
  "linkUrl": "https://agritech.com.tr/products/fungicide-xyz",
  "linkText": "XYZ Fungisit - Buğday Pası İçin",
  "keywords": ["buğday pası", "fungal hastalık", "wheat rust", "fungicide"],
  "targetCropTypes": ["Buğday", "Arpa"],
  "targetDiseases": ["Wheat Rust", "Leaf Rust"],
  "priority": 80,
  "isActive": true,
  "isApproved": false,  // Pending admin approval
  "displayCount": 0,
  "clickCount": 0,
  "uniqueClickCount": 0,
  "clickThroughRate": 0.0,
  "createdDate": "2025-10-15T16:00:00Z"
}
```

**Matching Algorithm:**
When farmer gets analysis result for "Buğday + Wheat Rust":
```csharp
public async Task<List<SmartLink>> GetMatchingLinksAsync(PlantAnalysis analysis)
{
    var keywords = ExtractKeywordsFromAnalysis(analysis);
    // ["buğday", "fungal hastalık", "yaprak hastalığı"]

    var matchingLinks = await _repository.GetMatchingLinksAsync(
        keywords,
        analysis.CropType,  // "Buğday"
        analysis.Disease,   // "Wheat Rust"
        analysis.Pest       // null
    );

    // Calculate relevance score for each link
    foreach (var link in matchingLinks)
    {
        link.RelevanceScore = CalculateRelevanceScore(link, analysis);
        // Factors: keyword match, crop match, disease match, priority
    }

    return matchingLinks
        .OrderByDescending(l => l.RelevanceScore)
        .ThenByDescending(l => l.Priority)
        .Take(5)  // Max 5 links per analysis
        .ToList();
}
```

**Farmer Sees In Analysis Result:**
```
📊 Analiz Sonucu: Buğday Pası Tespit Edildi

[Analysis details...]

💡 Önerilen Çözümler:
┌─────────────────────────────────────┐
│ 🏷️ AgriTech XYZ Fungisit           │
│                                     │
│ Etkili ve hızlı sonuç veren        │
│ fungisit çözümü                     │
│                                     │
│ 💰 250 TRY → 212.50 TRY (15% İndirim)│
│                                     │
│ [Ürünü İncele] 🔗                   │
└─────────────────────────────────────┘
📍 AgriTech Solutions tarafından önerildi
```

**Click Tracking:**
When farmer clicks "Ürünü İncele":
```csharp
// Increment counters
smartLink.ClickCount++;
smartLink.UniqueClickCount++; // If first click by this farmer
smartLink.DisplayCount++; // Total impressions

// Calculate CTR
smartLink.ClickThroughRate = (double)smartLink.ClickCount / smartLink.DisplayCount;

// Track click event for analytics
CreateClickEvent(smartLinkId, farmerId, analysisId, timestamp);
```

#### 6.3 View Smart Link Performance

**Endpoint:** `GET /api/v1/sponsorship/smart-links/performance`

**Authorization:** `Sponsor` or `Admin` role

**Response:**
```json
{
  "success": true,
  "data": {
    "totalSmartLinks": 12,
    "activeSmartLinks": 10,
    "totalDisplays": 5420,
    "totalClicks": 847,
    "totalUniqueClicks": 623,
    "overallCTR": 0.156,  // 15.6%
    "topPerformingLinks": [
      {
        "id": 7001,
        "linkText": "XYZ Fungisit - Buğday Pası İçin",
        "displayCount": 1250,
        "clickCount": 312,
        "uniqueClickCount": 245,
        "clickThroughRate": 0.249,  // 24.9%
        "estimatedRevenue": 12300.00,  // If tracking conversions
        "roi": 2.45  // Revenue / Investment
      }
    ],
    "performanceByCategory": {
      "Fungicide": {
        "displays": 2100,
        "clicks": 420,
        "ctr": 0.20
      },
      "Fertilizer": {
        "displays": 1800,
        "clicks": 270,
        "ctr": 0.15
      }
    },
    "performanceByCropType": {
      "Buğday": {
        "displays": 3200,
        "clicks": 640,
        "ctr": 0.20
      },
      "Mısır": {
        "displays": 1500,
        "clicks": 180,
        "ctr": 0.12
      }
    }
  }
}
```

**Constraints:**
- ❌ Only XL tier sponsors can create smart links
- ✅ Maximum 50 active smart links per sponsor
- ⚠️ Links require admin approval before going live
- ⚠️ Links must comply with advertising guidelines
- ❌ Cannot create misleading or false claims
- ✅ Can update/deactivate links anytime
- ✅ Full analytics available for all links

---

### Phase 7: Analytics & ROI Tracking

#### 7.1 Sponsorship Statistics Dashboard

**Endpoint:** `GET /api/v1/sponsorship/statistics`

**Authorization:** `Sponsor` or `Admin` role

**Response:**
```json
{
  "success": true,
  "data": {
    "overview": {
      "totalInvestment": 20000.00,
      "totalCodesPurchased": 100,
      "totalCodesDistributed": 85,
      "totalCodesRedeemed": 50,
      "redemptionRate": 0.588,  // 50/85 = 58.8%
      "activeSponsoredfarmers": 50,
      "expiringSubscriptions": 12  // Next 7 days
    },
    "byTier": {
      "L": {
        "codesPurchased": 100,
        "codesRedeemed": 50,
        "investment": 20000.00,
        "avgCostPerFarmer": 400.00,  // 20000/50
        "farmerDataAccess": "100%",
        "messagingEnabled": true
      }
    },
    "farmerEngagement": {
      "totalAnalysesPerformed": 750,  // By sponsored farmers
      "avgAnalysesPerFarmer": 15,
      "topCropTypes": [
        {"cropType": "Buğday", "count": 320},
        {"cropType": "Mısır", "count": 180},
        {"cropType": "Arpa", "count": 150}
      ],
      "topDiseases": [
        {"disease": "Wheat Rust", "count": 120},
        {"disease": "Corn Blight", "count": 85}
      ]
    },
    "geographicDistribution": {
      "byCity": [
        {"city": "Ankara", "farmerCount": 15, "analysisCount": 225},
        {"city": "Konya", "farmerCount": 12, "analysisCount": 180},
        {"city": "İzmir", "farmerCount": 10, "analysisCount": 150}
      ]
    },
    "smartLinksPerformance": {  // Only for XL tier
      "totalLinks": 12,
      "totalDisplays": 5420,
      "totalClicks": 847,
      "overallCTR": 0.156,
      "estimatedReach": 623  // Unique farmers who saw links
    },
    "roi": {
      "costPerAcquisition": 400.00,  // 20000/50
      "avgLifetimeValue": 1200.00,  // Estimated based on engagement
      "projectedROI": 3.0,  // 1200/400
      "breakEvenDate": "2026-02-10"  // Estimated
    },
    "trends": {
      "weeklyNewRedemptions": [12, 8, 15, 10, 5],  // Last 5 weeks
      "weeklyAnalyses": [120, 135, 145, 150, 160]
    }
  }
}
```

#### 7.2 View Purchase History

**Endpoint:** `GET /api/v1/sponsorship/purchases`

**Authorization:** `Sponsor` or `Admin` role

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 2001,
      "tierName": "L",
      "quantity": 100,
      "totalAmount": 20000.00,
      "purchaseDate": "2025-10-10T10:30:00Z",
      "paymentStatus": "Completed",
      "codesGenerated": 100,
      "codesUsed": 50,
      "codesActive": 35,  // Distributed but not redeemed
      "codesExpired": 15,
      "validityDays": 365,
      "expiryDate": "2026-10-10T10:30:00Z"
    }
  ]
}
```

---

## 🚫 Complete Constraint Matrix

### By Tier Level

| Action | S Tier | M Tier | L Tier | XL Tier |
|--------|--------|--------|--------|---------|
| **Data Access** |
| Farmer name/contact | ❌ | ❌ | ✅ | ✅ |
| Location (city) | ✅ | ✅ | ✅ | ✅ |
| Location (district) | ❌ | ✅ | ✅ | ✅ |
| Location (coordinates) | ❌ | ❌ | ✅ | ✅ |
| Crop types | ❌ | ✅ | ✅ | ✅ |
| Disease categories | ❌ | ✅ | ✅ | ✅ |
| Full analysis details | ❌ | ❌ | ✅ | ✅ |
| Analysis images | ❌ | ❌ | ✅ | ✅ |
| AI recommendations | ❌ | ❌ | ✅ | ✅ |
| **Logo Visibility** |
| Start screen | ✅ | ✅ | ✅ | ✅ |
| Result screen | ❌ | ✅ | ✅ | ✅ |
| Analysis details | ❌ | ❌ | ✅ | ✅ |
| Farmer profile | ❌ | ❌ | ✅ | ✅ |
| **Communication** |
| Send messages to farmers | ❌ | ❌ | ✅ | ✅ |
| View message conversations | ❌ | ❌ | ✅ | ✅ |
| **Smart Links** |
| Create smart links | ❌ | ❌ | ❌ | ✅ |
| View smart link analytics | ❌ | ❌ | ❌ | ✅ |
| Smart link quota | 0 | 0 | 0 | 50 |

### General Constraints

| Action | Constraint |
|--------|-----------|
| **Profile** |
| Create sponsor profile | ✅ Any authenticated user |
| Multiple profiles per user | ❌ Maximum 1 profile per user |
| Update profile | ✅ Anytime by sponsor |
| **Purchase** |
| Minimum quantity | ✅ Must be > 0 |
| Maximum quantity per purchase | ⚠️ No hard limit (business decision) |
| Payment completion | ✅ Required before code generation |
| Multiple tier purchases | ✅ Can buy S, M, L, XL in same account |
| **Code Distribution** |
| Max recipients per SMS/WhatsApp request | ⚠️ 100 recipients |
| Same code to multiple recipients | ❌ Not allowed |
| Send used/expired codes | ❌ Blocked by system |
| SMS delivery guarantee | ⚠️ Depends on NetGSM availability |
| **Farmer Access** |
| View farmer data without redemption | ❌ Only if farmer used sponsor's code |
| Access after subscription expires | ✅ Historical data remains accessible |
| Access different sponsor's farmers | ❌ Only own sponsored farmers |
| **Messaging** |
| Rate limit per farmer | ⚠️ 10 messages/day per farmer |
| Farmer can block sponsor | ✅ Yes |
| Admin moderation | ⚠️ First messages may require approval |
| **Smart Links** |
| Quota per sponsor | ✅ 50 active links (XL tier) |
| Approval requirement | ✅ Admin must approve before live |
| Max links per analysis | ⚠️ 5 links displayed |
| Update/delete links | ✅ Anytime by sponsor |

---

## 📱 Mobile App Integration

### Logo Display Rules (Corrected Architecture)

Logo visibility is determined by **the tier of the redeemed sponsorship code**, not the sponsor's current purchases.

**Logic:**
```csharp
public async Task<SponsorLogoPermissions> GetLogoPermissionsAsync(
    int plantAnalysisId, string screen)
{
    // Get the analysis
    var analysis = await _analysisRepository.GetAsync(a => a.Id == plantAnalysisId);

    // Get user's active subscription
    var subscription = await _subscriptionRepository
        .GetActiveSubscriptionByUserIdAsync(analysis.UserId);

    // Check if it's a sponsored subscription
    if (subscription == null || !subscription.IsSponsoredSubscription)
        return new SponsorLogoPermissions { CanDisplayLogo = false };

    // Get the redeemed code's tier
    var code = await _codeRepository.GetAsync(c =>
        c.CreatedSubscriptionId == subscription.Id);

    if (code == null)
        return new SponsorLogoPermissions { CanDisplayLogo = false };

    var tier = await _tierRepository.GetAsync(t => t.Id == code.SubscriptionTierId);

    // Determine visibility based on tier
    var canDisplay = (tier.TierName, screen) switch
    {
        ("S", "start") => true,
        ("M", "start") => true,
        ("M", "result") => true,
        ("L", _) => true,  // All screens
        ("XL", _) => true,  // All screens
        _ => false
    };

    if (!canDisplay)
        return new SponsorLogoPermissions { CanDisplayLogo = false };

    // Get sponsor profile for logo URL
    var sponsorProfile = await _profileRepository.GetBySponsorIdAsync(code.SponsorId);

    return new SponsorLogoPermissions
    {
        CanDisplayLogo = true,
        SponsorLogoUrl = sponsorProfile.SponsorLogoUrl,
        SponsorCompanyName = sponsorProfile.CompanyName,
        SponsorWebsiteUrl = sponsorProfile.WebsiteUrl,
        TierName = tier.TierName
    };
}
```

**Mobile Implementation:**
```dart
// Flutter example
Widget buildAnalysisScreen(PlantAnalysis analysis) {
  return FutureBuilder<SponsorLogoPermissions>(
    future: getSponsorLogoPermissions(analysis.id, 'result'),
    builder: (context, snapshot) {
      if (snapshot.hasData && snapshot.data!.canDisplayLogo) {
        return Column(
          children: [
            // Show sponsor logo
            SponsorLogoWidget(
              logoUrl: snapshot.data!.sponsorLogoUrl,
              companyName: snapshot.data!.sponsorCompanyName,
              onTap: () => launchUrl(snapshot.data!.sponsorWebsiteUrl),
            ),
            // Analysis content
            AnalysisResultContent(analysis),
          ],
        );
      }

      // No sponsor logo
      return AnalysisResultContent(analysis);
    },
  );
}
```

### Deferred Deep Linking (Code Auto-Fill)

**Implementation:**
```dart
// Flutter example
class SponsorshipCodeHandler {
  Future<void> handleDeepLink(Uri deepLink) async {
    // Parse deep link: https://ziraai.com/redeem/AGRI-2025-X3K9
    if (deepLink.path.startsWith('/redeem/')) {
      final code = deepLink.pathSegments.last;

      // Navigate to redemption screen with auto-filled code
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (context) => RedemptionScreen(
            autoFilledCode: code,
            fromDeepLink: true,
          ),
        ),
      );
    }
  }

  @override
  void initState() {
    super.initState();

    // Listen for deep links
    FirebaseDynamicLinks.instance.onLink.listen((dynamicLinkData) {
      handleDeepLink(dynamicLinkData.link);
    });

    // Check if app was opened from deep link
    FirebaseDynamicLinks.instance.getInitialLink().then((dynamicLinkData) {
      if (dynamicLinkData != null) {
        handleDeepLink(dynamicLinkData.link);
      }
    });
  }
}
```

---

## 🎯 Summary: What Can Sponsors Do?

### ✅ Core Actions (All Tiers)
1. **Create sponsor profile** (one-time setup)
2. **Purchase subscription packages** (S/M/L/XL tiers)
3. **Generate redemption codes** (automatic with purchase)
4. **View all generated codes** (used/unused status)
5. **Distribute codes via SMS/WhatsApp** (bulk sending)
6. **Track distribution performance** (delivery, clicks, redemptions)
7. **View sponsored farmers list** (data filtered by tier)
8. **View sponsorship statistics** (engagement, ROI, trends)
9. **View purchase history** (all transactions)
10. **Update sponsor profile** (company info, logo, contact)

### 🔒 Tier-Specific Actions

#### S Tier ($50/month) - Basic Visibility
- ✅ Logo on farmer's **start screen only**
- ✅ Anonymous farmer data (city, analysis count)
- ❌ No messaging
- ❌ No smart links

#### M Tier ($100/month) - Enhanced Visibility
- ✅ Logo on **start + result screens**
- ✅ Partial farmer data (city, district, crop types, disease categories)
- ❌ Farmer identity still anonymous
- ❌ No messaging (farmer profile anonymous)
- ❌ No smart links

#### L Tier ($200/month) - Full Data Access
- ✅ Logo on **all screens** (start, result, analysis, profile)
- ✅ **Full farmer data** (name, email, phone, location, farm details)
- ✅ **Full analysis access** (images, AI diagnosis, recommendations)
- ✅ **Messaging enabled** (can communicate with farmers)
- ❌ No smart links

#### XL Tier ($500/month) - Premium Features
- ✅ Everything in L tier
- ✅ **Smart Links** (AI-powered product recommendations)
- ✅ Create up to **50 smart links**
- ✅ Advanced analytics (CTR, conversions, ROI)
- ✅ Priority support

---

## 🚨 Important Business Rules

### 1. Code Redemption Rules
- ❌ One code per user (cannot redeem multiple times)
- ✅ Code provides **30-day free subscription**
- ✅ After expiry, farmer must purchase or redeem new code
- ✅ If farmer has active sponsored subscription and redeems another code, it **queues** for activation after current expires

### 2. Data Access Rules
- ✅ Sponsor can access farmer data **only if farmer used their code**
- ❌ Cannot access other sponsors' farmers
- ✅ Data access level determined by **redeemed code tier**, not current purchase tier
- ✅ Access remains even if sponsor doesn't make new purchases (honor existing redemptions)

### 3. Logo Display Rules
- ✅ Logo appears based on **redeemed code tier**, not sponsor's current purchases
- ✅ If farmer's sponsored subscription expires, logo disappears
- ✅ If farmer buys own subscription, logo disappears (unless sponsor paid)

### 4. Messaging Rules
- ✅ Only L and XL tier codes enable messaging
- ❌ Rate limit: 10 messages/day per farmer (prevent spam)
- ✅ Farmer can block sponsor anytime
- ⚠️ First messages may require admin approval

### 5. Smart Link Rules
- ✅ Only XL tier sponsors can create smart links
- ✅ Maximum 50 active links per sponsor
- ⚠️ Links require admin approval (anti-spam)
- ✅ Links appear contextually based on AI matching
- ✅ Maximum 5 links per analysis result

---

## 📊 Expected ROI Example

**Scenario:** AgriTech Solutions (M Tier Purchase)

```
Investment:
- 100 M-tier codes × $100 = $10,000
- SMS distribution cost: $0.05 × 100 = $5
- Total Investment: $10,005

Results After 30 Days:
- Codes distributed: 100 (SMS)
- Codes redeemed: 58 (58% redemption rate)
- Active farmers: 58
- Total analyses: 870 (15/farmer avg)
- Smart link clicks: 0 (M tier has no smart links)
- Direct messages: 0 (M tier has no messaging)

Data Gathered:
- Crop type distribution: Wheat 60%, Corn 25%, Barley 15%
- Top diseases: Wheat Rust 35%, Corn Blight 20%, Aphid 15%
- Geographic concentration: Ankara 30%, Konya 25%, İzmir 20%

Business Value:
- Market intelligence: High (identified top diseases, crop preferences)
- Brand awareness: 58 farmers saw logo 870+ times
- Cost per farmer: $172.50 ($10,005 / 58)
- Estimated product sales: $25,000 (based on follow-up dealer reports)
- ROI: 2.5x ($25,000 / $10,005)
```

---

## 🔮 Future Enhancements (Planned)

### 1. WhatsApp Sponsor Request Feature
- Farmers can request sponsorship via WhatsApp
- Sponsors receive requests with farmer profile
- Accept/reject with instant code generation

### 2. Referral Integration
- Farmers can refer other farmers
- Sponsors track referral chains
- Bonus codes for top referrers

### 3. Advanced Analytics
- Conversion tracking (link clicks → purchases)
- Cohort analysis (redemption waves)
- Predictive models (which farmers likely to buy)

### 4. Dynamic Pricing
- Seasonal discounts
- Bulk purchase discounts (>500 codes)
- Early renewal incentives

---

## 📚 Related Documentation

- [Mobile Sponsorship Integration Guide](./MOBILE_SPONSORSHIP_INTEGRATION_GUIDE.md)
- [Mobile Sponsor Registration Integration](./MOBILE_SPONSOR_REGISTRATION_INTEGRATION.md)
- [Sponsorship System Complete Documentation](./SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md)
- [Role Management Complete Guide](./ROLE_MANAGEMENT_COMPLETE_GUIDE.md)
- [Environment Variables Reference](./ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md)

---

**Document Status:** ✅ Complete and Ready for Implementation
**Next Review Date:** 2025-11-10
**Contact:** Development Team - ZiraAI Platform
