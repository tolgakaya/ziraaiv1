# Sponsorship Code Distribution - Complete Guide

**Version:** 2.0.0
**Date:** 2025-10-11
**Status:** ✅ Fully Implemented and Production Ready

---

## 📋 Table of Contents

1. [Executive Summary](#executive-summary)
2. [Complete Distribution Flow](#complete-distribution-flow)
3. [Step-by-Step Guide](#step-by-step-guide)
4. [API Reference](#api-reference)
5. [Mobile Integration](#mobile-integration)
6. [Environment Configuration](#environment-configuration)
7. [Testing Guide](#testing-guide)
8. [Troubleshooting](#troubleshooting)

---

## 📊 Executive Summary

### What is Sponsorship Code Distribution?

After a sponsor purchases bulk subscription packages, they need to distribute the generated codes to farmers. The system provides **two channels** for distribution:

1. **SMS** - Text message with redemption link
2. **WhatsApp** - WhatsApp Business message with redemption link

### Key Features

- ✅ **Bulk Distribution**: Send codes to multiple farmers in one request
- ✅ **Multi-Channel**: SMS and WhatsApp support
- ✅ **Deep Linking**: Links open mobile app with auto-filled code
- ✅ **Delivery Tracking**: Track sent, delivered, and clicked status
- ✅ **Click Analytics**: Monitor link engagement
- ✅ **Automatic Updates**: DistributionDate set on successful send
- ✅ **Cache Invalidation**: Dashboard stats auto-update after sending

---

## 🔄 Complete Distribution Flow

### Full Journey: Purchase → Distribution → Redemption

```
┌─────────────────────────────────────────────────────────────────────┐
│                   SPONSORSHIP LIFECYCLE                             │
└─────────────────────────────────────────────────────────────────────┘

╔══════════════════════════════════════════════════════════════════╗
║  PHASE 1: SPONSOR PURCHASES CODES                                ║
╚══════════════════════════════════════════════════════════════════╝

Sponsor (Mobile/Web)
│
├─> POST /api/v1/sponsorship/purchase-package
│   {
│     "subscriptionTierId": 2,        // M tier
│     "quantity": 100,
│     "totalAmount": 9999.00,
│     "paymentReference": "PAY-123"
│   }
│
└─> Response:
    {
      "success": true,
      "data": {
        "purchaseId": 15,
        "codesGenerated": 100,
        "codes": [
          { "code": "AGRI-X3K9", "tier": "M" },
          { "code": "AGRI-P7M4", "tier": "M" },
          ...
        ]
      }
    }

Database State After Purchase:
┌─────────────────────────────────────┐
│ SponsorshipCodes Table              │
├─────────────────────────────────────┤
│ Code: AGRI-X3K9                    │
│ SponsorId: 42                       │
│ SubscriptionTierId: 2               │
│ IsUsed: false                       │
│ IsActive: true                      │
│ DistributionDate: null ← UNSENT    │
│ LinkSentDate: null                  │
│ RecipientPhone: null                │
│ RecipientName: null                 │
└─────────────────────────────────────┘

╔══════════════════════════════════════════════════════════════════╗
║  PHASE 2: SPONSOR DISTRIBUTES CODES                              ║
╚══════════════════════════════════════════════════════════════════╝

Sponsor (Mobile/Web Dashboard)
│
├─> View "Unused Codes" section
│   GET /api/v1/sponsorship/codes?onlyUnused=true
│
│   Shows:
│   • 100 codes available
│   • Filter by tier (S/M/L/XL)
│   • Expiry dates
│
├─> Select codes to distribute
│   • Manual selection OR
│   • Bulk select all unused
│
└─> POST /api/v1/sponsorship/send-link
    {
      "sponsorId": 42,                    ← Auto-set from token
      "recipients": [
        {
          "code": "AGRI-X3K9",
          "phone": "+905551234567",
          "name": "Ahmet Yılmaz"
        },
        {
          "code": "AGRI-P7M4",
          "phone": "+905559876543",
          "name": "Mehmet Kaya"
        }
      ],
      "channel": "WhatsApp",               ← "SMS" or "WhatsApp"
      "customMessage": null                 ← Optional custom text
    }

Backend Processing (SendSponsorshipLinkCommand):
│
├─> 1. Validate Codes
│   │
│   ├─> Check code ownership
│   │   SELECT * FROM SponsorshipCodes
│   │   WHERE Code IN ('AGRI-X3K9', 'AGRI-P7M4')
│   │     AND SponsorId = 42
│   │     AND IsUsed = false
│   │     AND ExpiryDate > NOW()
│   │
│   └─> Result: Both codes valid ✅
│
├─> 2. Generate Redemption Links
│   │
│   │   baseUrl = config["WebAPI:BaseUrl"]
│   │          ?? config["Referral:FallbackDeepLinkBaseUrl"].Replace("/ref", "")
│   │
│   │   For each recipient:
│   │     redemptionLink = $"{baseUrl}/redeem/{code}"
│   │
│   │   Examples by Environment:
│   │   • Development:  https://localhost:5001/redeem/AGRI-X3K9
│   │   • Staging:      https://ziraai-api-sit.up.railway.app/redeem/AGRI-X3K9
│   │   • Production:   https://ziraai.com/redeem/AGRI-X3K9
│   │
│   └─> Links Generated ✅
│
├─> 3. Send Notifications (INotificationService)
│   │
│   ├─> Prepare Bulk Recipients
│   │   recipients = [
│   │     {
│   │       "phoneNumber": "+905551234567",
│   │       "name": "Ahmet Yılmaz",
│   │       "parameters": {
│   │         "farmer_name": "Ahmet Yılmaz",
│   │         "sponsor_code": "AGRI-X3K9",
│   │         "redemption_link": "https://ziraai.com/redeem/AGRI-X3K9",
│   │         "tier_name": "Premium",
│   │         "custom_message": ""
│   │       }
│   │     },
│   │     { ... second recipient }
│   │   ]
│   │
│   ├─> Send via Channel
│   │   │
│   │   ├─> if (channel == "WhatsApp"):
│   │   │     SendBulkTemplateNotificationsAsync(
│   │   │       recipients,
│   │   │       "sponsorship_invitation",
│   │   │       NotificationChannel.WhatsApp
│   │   │     )
│   │   │
│   │   └─> else (SMS):
│   │         SendBulkTemplateNotificationsAsync(
│   │           recipients,
│   │           "sponsorship_invitation_sms",
│   │           NotificationChannel.SMS
│   │         )
│   │
│   └─> Notification Result:
│       {
│         "success": true,
│         "data": [
│           { "success": true, "phoneNumber": "+905551234567" },
│           { "success": true, "phoneNumber": "+905559876543" }
│         ]
│       }
│
├─> 4. Update Database (For Successful Sends)
│   │
│   │   UPDATE SponsorshipCodes
│   │   SET
│   │     RedemptionLink = 'https://ziraai.com/redeem/AGRI-X3K9',
│   │     RecipientPhone = '+905551234567',
│   │     RecipientName = 'Ahmet Yılmaz',
│   │     LinkSentDate = NOW(),
│   │     LinkSentVia = 'WhatsApp',
│   │     LinkDelivered = true,
│   │     DistributionChannel = 'WhatsApp',
│   │     DistributionDate = NOW(),                    ← KEY FIELD!
│   │     DistributedTo = 'Ahmet Yılmaz (+905551234567)'
│   │   WHERE Code = 'AGRI-X3K9'
│   │
│   └─> Database Updated ✅
│
├─> 5. Invalidate Dashboard Cache
│   │
│   │   cacheKey = $"SponsorDashboard:{sponsorId}"
│   │   _cacheManager.Remove(cacheKey)
│   │
│   │   Console: "[DashboardCache] 🗑️ Invalidated cache for sponsor 42"
│   │
│   └─> Cache Cleared ✅
│
└─> 6. Return Response
    {
      "success": true,
      "message": "📱 2 link başarıyla gönderildi via WhatsApp",
      "data": {
        "totalSent": 2,
        "successCount": 2,
        "failureCount": 0,
        "results": [
          {
            "code": "AGRI-X3K9",
            "phone": "+905551234567",
            "success": true,
            "errorMessage": null,
            "deliveryStatus": "Sent"
          },
          {
            "code": "AGRI-P7M4",
            "phone": "+905559876543",
            "success": true,
            "errorMessage": null,
            "deliveryStatus": "Sent"
          }
        ]
      }
    }

Database State After Distribution:
┌─────────────────────────────────────┐
│ SponsorshipCodes Table              │
├─────────────────────────────────────┤
│ Code: AGRI-X3K9                    │
│ SponsorId: 42                       │
│ SubscriptionTierId: 2               │
│ IsUsed: false                       │
│ IsActive: true                      │
│ DistributionDate: 2025-10-11      ← SENT! │
│ LinkSentDate: 2025-10-11           │
│ RecipientPhone: +905551234567       │
│ RecipientName: Ahmet Yılmaz         │
│ RedemptionLink: https://...         │
│ LinkDelivered: true                 │
│ DistributionChannel: WhatsApp       │
│ DistributedTo: Ahmet Y. (+9055...) │
└─────────────────────────────────────┘

Dashboard Statistics Update:
GET /api/v1/sponsorship/dashboard-summary

Before Distribution:
  totalCodes: 100
  sentCodes: 0              ← DistributionDate IS NULL
  sentCodesPercentage: 0%

After Distribution:
  totalCodes: 100
  sentCodes: 2              ← DistributionDate IS NOT NULL
  sentCodesPercentage: 2%

╔══════════════════════════════════════════════════════════════════╗
║  PHASE 3: FARMER RECEIVES AND CLICKS LINK                       ║
╚══════════════════════════════════════════════════════════════════╝

Farmer's Phone
│
├─> WhatsApp Message Received:
│
│   🎁 Merhaba Ahmet Yılmaz!
│
│   Tarım A.Ş. size Medium abonelik paketi hediye etti!
│
│   📱 Hemen aktivasyon yapın:
│   https://ziraai.com/redeem/AGRI-X3K9
│
│   ⏰ Son kullanım: 11.10.2026
│   🌱 ZiraAI ile tarımınızı dijitalleştirin!
│
└─> Farmer Clicks Link
    │
    ├─> Mobile Deep Link Handler
    │   │
    │   ├─> Parse URL: /redeem/AGRI-X3K9
    │   │   Extract code: "AGRI-X3K9"
    │   │
    │   ├─> Check if ZiraAI app installed:
    │   │   │
    │   │   ├─> ✅ App Installed:
    │   │   │     • Launch ZiraAI app
    │   │   │     • Navigate to RedemptionScreen
    │   │   │     • Auto-fill code: "AGRI-X3K9"
    │   │   │
    │   │   └─> ❌ App Not Installed:
    │   │         • Redirect to Play Store/App Store
    │   │         • Store code in deferred deep link
    │   │         • After install → auto-fill code
    │   │
    │   └─> Track Click (Optional)
    │       UPDATE SponsorshipCodes
    │       SET LinkClickCount = LinkClickCount + 1,
    │           LinkClickDate = NOW(),
    │           LastClickIpAddress = '192.168.1.100'
    │       WHERE Code = 'AGRI-X3K9'
    │
    └─> Mobile App: Redemption Screen
        │
        ├─> Code Field: "AGRI-X3K9" (auto-filled)
        │
        └─> User taps "Redeem Code"
            │
            └─> POST /api/v1/sponsorship/redeem
                {
                  "code": "AGRI-X3K9"
                }

╔══════════════════════════════════════════════════════════════════╗
║  PHASE 4: CODE REDEMPTION                                        ║
╚══════════════════════════════════════════════════════════════════╝

Backend (RedeemSponsorshipCodeCommand):
│
├─> 1. Validate Code
│   │
│   ├─> Code exists? ✅
│   ├─> IsUsed = false? ✅
│   ├─> IsActive = true? ✅
│   ├─> ExpiryDate > NOW? ✅
│   │
│   └─> Valid ✅
│
├─> 2. Check Existing Subscription
│   │
│   ├─> User has Trial → Upgrade allowed ✅
│   ├─> User has Active Paid → Block ❌
│   │
│   └─> Can Upgrade ✅
│
├─> 3. Create Subscription
│   │
│   │   INSERT INTO UserSubscriptions (
│   │     UserId, SubscriptionTierId, StartDate, EndDate,
│   │     IsActive, IsSponsoredSubscription, SponsorshipCodeId,
│   │     SponsorId, PaymentMethod, PaymentReference
│   │   ) VALUES (
│   │     237, 2, NOW(), NOW() + 30 days,
│   │     true, true, 1501,
│   │     42, 'Sponsorship', 'AGRI-X3K9'
│   │   )
│   │
│   └─> Subscription Created (ID: 567) ✅
│
├─> 4. Mark Code as Used
│   │
│   │   UPDATE SponsorshipCodes
│   │   SET IsUsed = true,
│   │       UsedByUserId = 237,
│   │       UsedDate = NOW(),
│   │       CreatedSubscriptionId = 567
│   │   WHERE Code = 'AGRI-X3K9'
│   │
│   └─> Code Marked Used ✅
│
├─> 5. Update Statistics
│   │
│   │   UPDATE SponsorshipPurchases
│   │   SET CodesUsed = CodesUsed + 1
│   │   WHERE Id = 15
│   │
│   │   UPDATE SponsorProfiles
│   │   SET TotalCodesRedeemed = TotalCodesRedeemed + 1
│   │   WHERE SponsorId = 42
│   │
│   └─> Stats Updated ✅
│
└─> 6. Return Success
    {
      "success": true,
      "message": "Medium aboneliğiniz başarıyla aktive edildi!",
      "data": {
        "subscriptionId": 567,
        "tierName": "M",
        "startDate": "2025-10-11",
        "endDate": "2025-11-10",
        "sponsorName": "Tarım A.Ş."
      }
    }

Final Database State:
┌─────────────────────────────────────┐
│ SponsorshipCodes Table              │
├─────────────────────────────────────┤
│ Code: AGRI-X3K9                    │
│ SponsorId: 42                       │
│ IsUsed: true             ← REDEEMED!│
│ UsedByUserId: 237                   │
│ UsedDate: 2025-10-11                │
│ CreatedSubscriptionId: 567          │
│ DistributionDate: 2025-10-11        │
│ LinkClickCount: 1                   │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ UserSubscriptions Table             │
├─────────────────────────────────────┤
│ Id: 567                             │
│ UserId: 237 (Ahmet Yılmaz)         │
│ SubscriptionTierId: 2 (M)          │
│ IsActive: true                      │
│ IsSponsoredSubscription: true       │
│ SponsorshipCodeId: 1501             │
│ SponsorId: 42                       │
│ StartDate: 2025-10-11               │
│ EndDate: 2025-11-10                 │
└─────────────────────────────────────┘

Dashboard Final Stats:
GET /api/v1/sponsorship/dashboard-summary

  totalCodes: 100
  sentCodes: 2
  usedCodes: 1              ← IsUsed = true
  unusedSentCodes: 1        ← sent but not used
  sentCodesPercentage: 2%
  usedCodesPercentage: 1%
```

---

## 📖 Step-by-Step Guide

### For Sponsors: How to Distribute Codes

#### Step 1: Purchase Codes (Already Done)

You've already purchased codes via:
```http
POST /api/v1/sponsorship/purchase-bulk
```

#### Step 2: View Available Codes

```http
GET /api/v1/sponsorship/codes?onlyUnused=true
Authorization: Bearer {sponsor_token}
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1501,
      "code": "AGRI-X3K9",
      "tierName": "M",
      "isUsed": false,
      "isActive": true,
      "expiryDate": "2026-10-11",
      "distributionDate": null,     ← Not sent yet
      "recipientPhone": null
    }
  ]
}
```

#### Step 3: Send Links to Farmers

```http
POST /api/v1/sponsorship/send-link
Authorization: Bearer {sponsor_token}
Content-Type: application/json

{
  "recipients": [
    {
      "code": "AGRI-X3K9",
      "phone": "+905551234567",       ← Turkish format: +90XXXXXXXXXX
      "name": "Ahmet Yılmaz"
    },
    {
      "code": "AGRI-P7M4",
      "phone": "05559876543",         ← Auto-converts to +90
      "name": "Mehmet Kaya"
    }
  ],
  "channel": "WhatsApp",              ← "SMS" or "WhatsApp"
  "customMessage": null               ← Optional
}
```

**Response:**
```json
{
  "success": true,
  "message": "📱 2 link başarıyla gönderildi via WhatsApp",
  "data": {
    "totalSent": 2,
    "successCount": 2,
    "failureCount": 0,
    "results": [
      {
        "code": "AGRI-X3K9",
        "phone": "+905551234567",
        "success": true,
        "errorMessage": null,
        "deliveryStatus": "Sent"
      },
      {
        "code": "AGRI-P7M4",
        "phone": "+905559876543",
        "success": true,
        "errorMessage": null,
        "deliveryStatus": "Sent"
      }
    ]
  }
}
```

#### Step 4: Track Distribution

Check dashboard stats:
```http
GET /api/v1/sponsorship/dashboard-summary
Authorization: Bearer {sponsor_token}
```

**Response shows:**
```json
{
  "totalCodes": 100,
  "sentCodes": 2,              ← Codes with DistributionDate != null
  "usedCodes": 0,
  "sentCodesPercentage": 2.0,
  "unusedSentCodes": 2         ← sent = true, used = false
}
```

---

## 📡 API Reference

### Send Sponsorship Links

**Endpoint:** `POST /api/v1/sponsorship/send-link`
**Authorization:** Bearer token (Sponsor or Admin role)
**Handler:** `SendSponsorshipLinkCommand.cs`

#### Request Body

```json
{
  "sponsorId": 42,                    // Auto-set from auth token
  "recipients": [
    {
      "code": "AGRI-X3K9",           // Required
      "phone": "+905551234567",       // Required (Turkish format)
      "name": "Ahmet Yılmaz"          // Required
    }
  ],
  "channel": "WhatsApp",              // Required: "SMS" or "WhatsApp"
  "customMessage": null               // Optional custom text
}
```

#### Phone Number Format

The system automatically normalizes phone numbers:

| Input Format | Normalized Output |
|--------------|-------------------|
| `905551234567` | `+905551234567` |
| `5551234567` | `+905551234567` |
| `+905551234567` | `+905551234567` |
| `05551234567` | `+905551234567` |

**Implementation:** `SendSponsorshipLinkCommand.cs:226-244`

```csharp
private string FormatPhoneNumber(string phone)
{
    // Remove all non-numeric characters
    var cleaned = new string(phone.Where(char.IsDigit).ToArray());

    // Add Turkey country code if not present
    if (!cleaned.StartsWith("90") && cleaned.Length == 10)
    {
        cleaned = "90" + cleaned;
    }

    // Add + prefix
    if (!cleaned.StartsWith("+"))
    {
        cleaned = "+" + cleaned;
    }

    return cleaned;
}
```

#### Response Body

```json
{
  "success": true,
  "message": "📱 {successCount} link başarıyla gönderildi via {channel}",
  "data": {
    "totalSent": 2,
    "successCount": 2,
    "failureCount": 0,
    "results": [
      {
        "code": "AGRI-X3K9",
        "phone": "+905551234567",
        "success": true,
        "errorMessage": null,
        "deliveryStatus": "Sent"
      }
    ]
  }
}
```

#### Error Responses

**Invalid Code:**
```json
{
  "code": "AGRI-INVALID",
  "phone": "+905551234567",
  "success": false,
  "errorMessage": "Kod bulunamadı veya kullanılamaz durumda",
  "deliveryStatus": "Failed - Invalid Code"
}
```

**Notification Failed:**
```json
{
  "code": "AGRI-X3K9",
  "phone": "+905551234567",
  "success": false,
  "errorMessage": "Bildirim gönderimi başarısız",
  "deliveryStatus": "Failed"
}
```

---

## 📱 Mobile Integration

### WhatsApp Message Format

```
🎁 Merhaba {farmer_name}!

{sponsor_company} size {tier_name} abonelik paketi hediye etti!

📱 Hemen aktivasyon yapın:
{redemption_link}

⏰ Son kullanım: {expiry_date}
🌱 ZiraAI ile tarımınızı dijitalleştirin!

ZiraAI - Akıllı Tarım Çözümleri
```

**Example:**
```
🎁 Merhaba Ahmet Yılmaz!

Tarım A.Ş. size Medium abonelik paketi hediye etti!

📱 Hemen aktivasyon yapın:
https://ziraai.com/redeem/AGRI-X3K9

⏰ Son kullanım: 11.10.2026
🌱 ZiraAI ile tarımınızı dijitalleştirin!

ZiraAI - Akıllı Tarım Çözümleri
```

### SMS Message Format

Same as WhatsApp but plain text without emojis.

### Deep Link Handling (Flutter)

```dart
import 'package:uni_links/uni_links.dart';

class DeepLinkService {
  StreamSubscription? _sub;

  void initialize() {
    _sub = uriLinkStream.listen((Uri? uri) {
      if (uri != null) {
        _handleDeepLink(uri);
      }
    });
  }

  void _handleDeepLink(Uri uri) {
    // Parse: https://ziraai.com/redeem/AGRI-X3K9
    if (uri.pathSegments.isNotEmpty && uri.pathSegments[0] == 'redeem') {
      String code = uri.pathSegments.last;  // "AGRI-X3K9"

      // Navigate to redemption screen with auto-filled code
      Get.toNamed('/redemption', arguments: {'code': code});
    }
  }

  void dispose() {
    _sub?.cancel();
  }
}
```

### Redemption Screen (Flutter)

```dart
class RedemptionScreen extends StatefulWidget {
  @override
  _RedemptionScreenState createState() => _RedemptionScreenState();
}

class _RedemptionScreenState extends State<RedemptionScreen> {
  final TextEditingController _codeController = TextEditingController();

  @override
  void initState() {
    super.initState();

    // Auto-fill code from deep link
    String? code = Get.arguments?['code'];
    if (code != null) {
      _codeController.text = code;
    }
  }

  Future<void> _redeemCode(String code) async {
    try {
      final response = await ApiService.post(
        '/api/v1/sponsorship/redeem',
        data: {'code': code},
      );

      if (response.success) {
        Get.snackbar(
          'Başarılı!',
          response.message,
          backgroundColor: Colors.green,
        );
        Get.offNamed('/home');
      } else {
        Get.snackbar(
          'Hata',
          response.message,
          backgroundColor: Colors.red,
        );
      }
    } catch (e) {
      Get.snackbar('Hata', 'Kod kullanılamadı: $e');
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Sponsorluk Kodu Kullan')),
      body: Padding(
        padding: EdgeInsets.all(16.0),
        child: Column(
          children: [
            TextField(
              controller: _codeController,
              decoration: InputDecoration(
                labelText: 'Sponsorluk Kodu',
                hintText: 'AGRI-X3K9',
              ),
            ),
            SizedBox(height: 16),
            ElevatedButton(
              onPressed: () => _redeemCode(_codeController.text),
              child: Text('Kodu Kullan'),
            ),
          ],
        ),
      ),
    );
  }
}
```

---

## ⚙️ Environment Configuration

### Required Settings

**File:** `appsettings.json` / `appsettings.{Environment}.json`

```json
{
  "WebAPI": {
    "BaseUrl": "https://ziraai.com"           // Production
    // "BaseUrl": "https://ziraai-api-sit.up.railway.app"  // Staging
    // "BaseUrl": "https://localhost:5001"    // Development
  },
  "Referral": {
    "FallbackDeepLinkBaseUrl": "https://ziraai.com/ref/"
  }
}
```

### Environment-Specific URLs

| Environment | Base URL | Redemption Link Example |
|-------------|----------|------------------------|
| **Development** | `https://localhost:5001` | `https://localhost:5001/redeem/AGRI-X3K9` |
| **Staging** | `https://ziraai-api-sit.up.railway.app` | `https://ziraai-api-sit.up.railway.app/redeem/AGRI-X3K9` |
| **Production** | `https://ziraai.com` | `https://ziraai.com/redeem/AGRI-X3K9` |

### Link Generation Logic

**File:** `SendSponsorshipLinkCommand.cs:105-109`

```csharp
var baseUrl = _configuration["WebAPI:BaseUrl"]
    ?? _configuration["Referral:FallbackDeepLinkBaseUrl"]?.TrimEnd('/').Replace("/ref", "")
    ?? throw new InvalidOperationException("WebAPI:BaseUrl must be configured");

var redemptionLink = $"{baseUrl.TrimEnd('/')}/redeem/{recipient.Code}";
```

**Priority:**
1. `WebAPI:BaseUrl` (Highest)
2. `Referral:FallbackDeepLinkBaseUrl` (without `/ref`)
3. Throw exception (Config required!)

---

## 🧪 Testing Guide

### Manual Testing Steps

#### Test 1: Send Single Link via SMS

```bash
curl -X POST https://localhost:5001/api/v1/sponsorship/send-link \
  -H "Authorization: Bearer {sponsor_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "recipients": [
      {
        "code": "AGRI-TEST1",
        "phone": "+905551234567",
        "name": "Test Farmer"
      }
    ],
    "channel": "SMS"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "📱 1 link başarıyla gönderildi via SMS",
  "data": {
    "successCount": 1,
    "failureCount": 0
  }
}
```

**Database Verification:**
```sql
SELECT
  Code,
  RecipientPhone,
  RecipientName,
  DistributionDate,
  DistributionChannel,
  LinkSentVia
FROM SponsorshipCodes
WHERE Code = 'AGRI-TEST1';
```

**Expected:**
- `DistributionDate`: NOT NULL (current timestamp)
- `DistributionChannel`: "SMS"
- `RecipientPhone`: "+905551234567"
- `RecipientName`: "Test Farmer"

#### Test 2: Send Bulk via WhatsApp

```http
POST /api/v1/sponsorship/send-link
{
  "recipients": [
    { "code": "AGRI-TEST2", "phone": "+905551111111", "name": "Farmer 1" },
    { "code": "AGRI-TEST3", "phone": "+905552222222", "name": "Farmer 2" },
    { "code": "AGRI-TEST4", "phone": "+905553333333", "name": "Farmer 3" }
  ],
  "channel": "WhatsApp"
}
```

**Expected:**
- `successCount`: 3
- All codes have `DistributionDate` set

#### Test 3: Verify Dashboard Stats

```http
GET /api/v1/sponsorship/dashboard-summary
Authorization: Bearer {sponsor_token}
```

**Before Sending:**
```json
{
  "totalCodes": 10,
  "sentCodes": 0,
  "sentCodesPercentage": 0.0
}
```

**After Sending 3 codes:**
```json
{
  "totalCodes": 10,
  "sentCodes": 3,
  "sentCodesPercentage": 30.0
}
```

#### Test 4: Phone Number Normalization

**Input:**
```json
{
  "recipients": [
    { "code": "T1", "phone": "5551234567", "name": "Test" },
    { "code": "T2", "phone": "05551234567", "name": "Test" },
    { "code": "T3", "phone": "+905551234567", "name": "Test" },
    { "code": "T4", "phone": "905551234567", "name": "Test" }
  ],
  "channel": "SMS"
}
```

**Expected:** All normalized to `+905551234567`

**Verification:**
```sql
SELECT DISTINCT RecipientPhone FROM SponsorshipCodes WHERE Code IN ('T1','T2','T3','T4');
-- Result: +905551234567 (single row)
```

#### Test 5: Invalid Code Handling

```http
POST /api/v1/sponsorship/send-link
{
  "recipients": [
    { "code": "INVALID-CODE", "phone": "+905551234567", "name": "Test" }
  ],
  "channel": "SMS"
}
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "totalSent": 1,
    "successCount": 0,
    "failureCount": 1,
    "results": [
      {
        "code": "INVALID-CODE",
        "phone": "+905551234567",
        "success": false,
        "errorMessage": "Kod bulunamadı veya kullanılamaz durumda",
        "deliveryStatus": "Failed - Invalid Code"
      }
    ]
  }
}
```

---

## 🔧 Troubleshooting

### Issue 1: Links Not Sent

**Symptoms:**
- `successCount`: 0
- `failureCount`: > 0

**Possible Causes:**
1. Invalid codes (already used, expired, or not owned by sponsor)
2. Notification service failure
3. Invalid phone numbers

**Debug Steps:**

**Check Code Status:**
```sql
SELECT
  Code,
  IsUsed,
  IsActive,
  ExpiryDate,
  SponsorId
FROM SponsorshipCodes
WHERE Code = 'YOUR-CODE';
```

**Check Logs:**
```bash
grep "SendSponsorshipLink" logs/dev/*.txt
grep "ERROR" logs/dev/*.txt | grep "notification"
```

**Solution:**
- Verify code ownership: `SponsorId` matches current user
- Check expiry: `ExpiryDate > NOW()`
- Verify not used: `IsUsed = false`

---

### Issue 2: DistributionDate Not Set

**Symptoms:**
- Message sent successfully
- `sentCodes` count doesn't increase
- `DistributionDate` remains NULL

**Possible Causes:**
- Notification failed silently
- Database update not reaching `DistributionDate` field

**Debug Steps:**

**Check Notification Result:**
```csharp
// In SendSponsorshipLinkCommand.cs:147
if (notificationResult.Success && notificationResult.Data != null)
{
    // Process results
}
```

**Check Database Transaction:**
```sql
SELECT
  Code,
  LinkSentDate,           -- Should be set
  DistributionDate,       -- Should be set if sent successfully
  LinkDelivered           -- Should be true
FROM SponsorshipCodes
WHERE Code = 'YOUR-CODE';
```

**Solution:**
- Verify notification service returns `success: true`
- Check `LinkDelivered` flag is set to `true`
- Ensure transaction commits successfully

---

### Issue 3: Dashboard Cache Not Updating

**Symptoms:**
- Links sent successfully
- Database shows `DistributionDate` set
- Dashboard still shows old `sentCodes` count

**Cause:**
Cache invalidation not triggered

**Debug Steps:**

**Check Cache Invalidation:**
```csharp
// In SendSponsorshipLinkCommand.cs:207-214
if (bulkResult.SuccessCount > 0)
{
    var cacheKey = $"SponsorDashboard:{request.SponsorId}";
    _cacheManager.Remove(cacheKey);
    _logger.LogInformation("[DashboardCache] 🗑️ Invalidated cache...");
}
```

**Verify Log:**
```bash
grep "DashboardCache" logs/dev/*.txt | grep "Invalidated"
```

**Solution:**
- Force cache clear:
  ```csharp
  _cacheManager.Remove($"SponsorDashboard:{sponsorId}");
  ```
- Restart application to clear all cache

---

### Issue 4: Deep Link Not Opening App

**Symptoms:**
- Farmer clicks link
- Browser opens instead of mobile app

**Possible Causes:**
1. Deep link configuration missing (iOS/Android)
2. Wrong URL format
3. App not installed

**Solution:**

**iOS Configuration:**
Create `.well-known/apple-app-site-association`:
```json
{
  "applinks": {
    "apps": [],
    "details": [
      {
        "appID": "TEAM_ID.com.ziraai.app",
        "paths": ["/redeem/*", "/ref/*"]
      }
    ]
  }
}
```

**Android Configuration:**
Create `.well-known/assetlinks.json`:
```json
[
  {
    "relation": ["delegate_permission/common.handle_all_urls"],
    "target": {
      "namespace": "android_app",
      "package_name": "com.ziraai.app",
      "sha256_cert_fingerprints": ["SHA256_FINGERPRINT"]
    }
  }
]
```

**AndroidManifest.xml:**
```xml
<intent-filter android:autoVerify="true">
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="https"
          android:host="ziraai.com"
          android:pathPrefix="/redeem" />
</intent-filter>
```

---

## 📊 Key Database Queries

### Query 1: Get All Sent Codes for Sponsor

```sql
SELECT
  Code,
  RecipientName,
  RecipientPhone,
  DistributionDate,
  DistributionChannel,
  IsUsed,
  LinkClickCount
FROM SponsorshipCodes
WHERE SponsorId = 42
  AND DistributionDate IS NOT NULL
ORDER BY DistributionDate DESC;
```

### Query 2: Calculate Sent Codes Percentage

```sql
SELECT
  COUNT(*) as TotalCodes,
  COUNT(CASE WHEN DistributionDate IS NOT NULL THEN 1 END) as SentCodes,
  ROUND(
    COUNT(CASE WHEN DistributionDate IS NOT NULL THEN 1 END)::decimal /
    NULLIF(COUNT(*), 0) * 100,
    2
  ) as SentPercentage
FROM SponsorshipCodes
WHERE SponsorId = 42;
```

### Query 3: Find Sent but Not Used Codes

```sql
SELECT
  Code,
  RecipientName,
  RecipientPhone,
  DistributionDate,
  EXTRACT(DAY FROM AGE(NOW(), DistributionDate)) as DaysSinceSent
FROM SponsorshipCodes
WHERE SponsorId = 42
  AND DistributionDate IS NOT NULL
  AND IsUsed = false
ORDER BY DistributionDate ASC;
```

---

## 📚 Related Documentation

- [SPONSORSHIP_QUANTITY_LIMITS_DOCUMENTATION.md](./SPONSORSHIP_QUANTITY_LIMITS_DOCUMENTATION.md) - Tier purchase limits
- [SPONSOR_DASHBOARD_ENDPOINT_DOCUMENTATION.md](./SPONSOR_DASHBOARD_ENDPOINT_DOCUMENTATION.md) - Dashboard API and cache
- [SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md](./SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md) - Complete system overview
- [SPONSORSHIP_QUEUE_TESTING_GUIDE.md](./SPONSORSHIP_QUEUE_TESTING_GUIDE.md) - Queue system testing
- [ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md](./ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md) - Environment configuration

---

**End of Documentation**

*Last Updated: 2025-10-11 by Claude Code*
*Version: 2.0.0*
*Status: ✅ Production Ready*
