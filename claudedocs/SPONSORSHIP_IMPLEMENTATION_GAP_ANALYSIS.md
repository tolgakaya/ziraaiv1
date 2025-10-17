# Sponsorship System - Implementation Gap Analysis & Action Plan

**Date:** 2025-10-07
**Status:** Ready for Mobile Integration + Backend Gaps Identified

---

## 📊 Executive Summary

| Component | Status | Completeness | Action Needed |
|-----------|--------|--------------|---------------|
| **Backend Core** | ✅ Implemented | 95% | Minor fixes |
| **SMS Templates** | ✅ Ready | 100% | None - Ready for mobile |
| **WhatsApp Templates** | ✅ Ready | 100% | None - Ready for mobile |
| **Mock Services** | ✅ Implemented | 100% | None - Working |
| **Real SMS Provider** | ⚠️ Mock Only | 0% | Configure NetGSM/Twilio |
| **Real WhatsApp** | ⚠️ Mock Only | 50% | Configure credentials |
| **Mobile Deep Linking** | ❌ Not Started | 0% | **CRITICAL - Mobile team** |
| **SMS Auto-Fill** | ❌ Not Started | 0% | **CRITICAL - Mobile team** |
| **Multiple Sponsorship** | ⚠️ Needs Decision | 80% | Business logic decision |
| **Payment Gateway** | ❌ Not Started | 0% | Backend integration |

---

## 🔍 Detailed Gap Analysis

### 1️⃣ SMS Templates & Content ✅ READY FOR MOBILE

#### **Current Implementation:**

**File:** `RedemptionService.cs:579-597`

```csharp
private string BuildSponsorshipMessage(
    string recipientName,
    string sponsorName,
    string tierName,
    string redemptionLink,
    DateTime expiryDate)
{
    return $@"🎁 Merhaba {recipientName}!

{sponsorName} size {tierName} abonelik paketi hediye etti!

📱 Hemen aktivasyon yapın:
{redemptionLink}

⏰ Son kullanım: {expiryDate:dd.MM.yyyy}
🌱 ZiraAI ile tarımınızı dijitalleştirin!

ZiraAI - Akıllı Tarım Çözümleri";
}
```

**SMS Example Output:**
```
🎁 Merhaba Ahmet Yılmaz!

Tarım A.Ş. size Medium abonelik paketi hediye etti!

📱 Hemen aktivasyon yapın:
https://ziraai.com/redeem/AGRI-2025-X3K9

⏰ Son kullanım: 07.10.2026
🌱 ZiraAI ile tarımınızı dijitalleştirin!

ZiraAI - Akıllı Tarım Çözümleri
```

**Key Elements for Mobile:**
- ✅ Redemption link: `https://ziraai.com/redeem/{CODE}`
- ✅ Code format: `AGRI-2025-X3K9` (pattern: `AGRI-YYYY-XXXX`)
- ✅ Message clearly states code will auto-fill (from ReferralLinkService.cs:239)
- ✅ Expiry date included

---

### 2️⃣ WhatsApp Templates ✅ READY FOR MOBILE

#### **Current Implementation:**

**File:** `NotificationService.cs:294-327`

```csharp
public async Task<IDataResult<NotificationResultDto>> SendSponsorshipLinkNotificationAsync(
    string phoneNumber,
    string farmerName,
    string sponsorCompany,
    string subscriptionTier,
    string redemptionLink,
    string expiryDate,
    NotificationChannel channel)
{
    var parameters = new Dictionary<string, object>
    {
        ["farmer_name"] = farmerName,
        ["sponsor_company"] = sponsorCompany,
        ["subscription_tier"] = subscriptionTier,
        ["redemption_link"] = redemptionLink,
        ["expiry_date"] = expiryDate
    };

    var templateName = GetTemplateNameFromConfig("SponsorshipInvitation");

    switch (channel)
    {
        case NotificationChannel.WhatsApp:
            return await SendWhatsAppTemplateNotificationAsync(phoneNumber, templateName, parameters);
        case NotificationChannel.SMS:
            return await SendSmsNotificationAsync(phoneNumber, templateName, parameters);
    }
}
```

**Template Name:** `sponsorship_invitation` (from config)

**WhatsApp Message (Plain Text Fallback):**
```
🎁 Merhaba Ahmet Yılmaz!

Tarım A.Ş. size Medium abonelik paketi hediye etti!

📱 Hemen aktivasyon yapın:
https://ziraai.com/redeem/AGRI-2025-X3K9

⏰ Son kullanım: 07.10.2026
🌱 ZiraAI ile tarımınızı dijitalleştirin!

ZiraAI - Akıllı Tarım Çözümleri
```

---

### 3️⃣ Mock Services ✅ WORKING

#### **SMS Mock Service:**

**File:** `MockSmsService.cs`

**Features:**
- ✅ Console logging for development
- ✅ Fixed OTP code support (for testing)
- ✅ Message ID generation
- ✅ Delivery status simulation
- ✅ Turkish phone number normalization

**Example Console Output:**
```
📱 MOCK SMS
   To: 05551234567
   Message: 🎁 Merhaba Ahmet Yılmaz!...
   MessageId: MOCK-SMS-20251007143022-A3F5C8D1

📱 MOCK SMS sent. To=05551234567, MessageId=MOCK-SMS-20251007143022-A3F5C8D1
```

**Configuration:**
```json
{
  "SmsService": {
    "MockSettings": {
      "UseFixedCode": true,
      "FixedCode": "123456",
      "LogToConsole": true
    }
  }
}
```

#### **WhatsApp Mock Service:**

**File:** `WhatsAppBusinessService.cs`

**Features:**
- ✅ WhatsApp Business API integration ready
- ✅ Template message support
- ✅ Phone normalization (Turkish format)
- ✅ Rate limiting (1 msg/sec)
- ✅ Bulk message support

**Current Status:** Mock responds with success but doesn't send real messages

---

### 4️⃣ CRITICAL GAPS - Mobile Integration

#### **Gap 4A: Deep Link Handling** ❌ NOT IMPLEMENTED

**What's Missing:**
```dart
// Flutter - Universal Links / App Links
// iOS: .well-known/apple-app-site-association
// Android: .well-known/assetlinks.json

// Deep link handler
void _handleDeepLink(Uri uri) {
  if (uri.pathSegments.contains('redeem')) {
    String code = uri.pathSegments.last;
    _navigateToRedemption(code);
  }
}
```

**Required Files (Mobile Team):**
1. `ios/.well-known/apple-app-site-association`
2. `android/.well-known/assetlinks.json`
3. Deep link configuration in `AndroidManifest.xml`
4. Deep link configuration in `Info.plist`

**Backend Support:** ✅ Ready
- Redemption link format: `https://ziraai.com/redeem/{CODE}`
- Already implemented in `SendSponsorshipLinkCommand.cs:98`

---

#### **Gap 4B: SMS Auto-Fill (Deferred Deep Linking)** ❌ NOT IMPLEMENTED

**What's Missing:**

```dart
// Flutter - SMS Permission & Reading
import 'package:telephony/telephony.dart';

class SmsAutoFillService {
  final Telephony telephony = Telephony.instance;

  Future<String?> extractSponsorshipCode() async {
    // Request SMS permission
    bool? permissionsGranted = await telephony.requestPhoneAndSmsPermissions;

    if (permissionsGranted != true) return null;

    // Read recent SMS (last 24 hours)
    List<SmsMessage> messages = await telephony.getInboxSms(
      filter: SmsFilter.where(SmsColumn.DATE)
        .greaterThan(DateTime.now().subtract(Duration(days: 1)).millisecondsSinceEpoch.toString())
    );

    // Extract sponsorship code pattern: AGRI-2025-XXXX
    RegExp codePattern = RegExp(r'AGRI-\d{4}-[A-Z0-9]{4}');

    for (var message in messages) {
      var match = codePattern.firstMatch(message.body ?? '');
      if (match != null) {
        return match.group(0); // Return: AGRI-2025-X3K9
      }
    }

    return null;
  }
}
```

**Code Pattern to Match:** `AGRI-YYYY-XXXX`
- Example: `AGRI-2025-X3K9`
- Regex: `AGRI-\d{4}-[A-Z0-9]{4}`

**Backend Support:** ✅ Ready
- Code format implemented in `SponsorshipCodeRepository.cs`
- SMS messages include code in plain text
- Message says: "Uygulama açıldığında kod otomatik gelecek!"

---

### 5️⃣ Business Logic Gap - Multiple Sponsorship

**Current Behavior:**
```csharp
// RedeemSponsorshipCodeCommand.cs:182-206
if (existingSubscription != null)
{
    bool canUpgrade = existingTier != null &&
                     (existingTier.TierName == "Trial" ||
                      existingTier.MonthlyPrice == 0 ||
                      existingSubscription.IsTrialSubscription);

    if (!canUpgrade)
    {
        return new ErrorDataResult<UserSubscription>(
            "Zaten aktif bir aboneliğiniz var. " +
            "Sponsorship codes can only be used to upgrade from Trial subscriptions."
        );
    }
}
```

**Scenarios:**

| Current State | New Code | Current Behavior | Recommendation |
|---------------|----------|------------------|----------------|
| Trial | Sponsor S | ✅ Allowed (upgrade) | Keep |
| Trial | Sponsor M | ✅ Allowed (upgrade) | Keep |
| Trial | Sponsor L | ✅ Allowed (upgrade) | Keep |
| Sponsor S (active) | Sponsor M | ❌ **BLOCKED** | ❓ Allow upgrade? |
| Sponsor M (active) | Sponsor L | ❌ **BLOCKED** | ❓ Allow upgrade? |
| Sponsor M (active) | Sponsor S | ❌ **BLOCKED** | ✅ Keep blocked (downgrade) |
| Sponsor M (active) | Sponsor M | ❌ **BLOCKED** | ❓ Stack 30 days? |
| Paid subscription | Any sponsor code | ❌ **BLOCKED** | ✅ Keep blocked |

**Recommended Logic:**
```csharp
// Option 1: Allow Upgrade Only (RECOMMENDED)
bool canUpgrade = existingTier != null &&
                 (existingTier.TierName == "Trial" ||
                  existingTier.MonthlyPrice == 0 ||
                  existingSubscription.IsSponsoredSubscription &&
                  newTierId > existingSubscription.SubscriptionTierId);

// Option 2: Allow Stacking (Alternative)
if (existingSubscription.IsSponsoredSubscription &&
    newTierId == existingSubscription.SubscriptionTierId)
{
    // Extend end date by 30 days
    existingSubscription.EndDate = existingSubscription.EndDate.AddDays(30);
}
```

**⚠️ NEEDS BUSINESS DECISION:** Choose Option 1 or Option 2

---

### 6️⃣ Provider Integration Gaps

#### **Gap 6A: Real SMS Provider** ⚠️ MOCK ONLY

**What's Missing:**
- NetGSM / Twilio / AWS SNS integration
- Real API credentials
- Production configuration

**Files to Update:**
- `appsettings.Production.json`:
  ```json
  {
    "SmsService": {
      "Provider": "NetGSM",  // or "Twilio"
      "NetGSM": {
        "Username": "${NETGSM_USERNAME}",
        "Password": "${NETGSM_PASSWORD}",
        "ApiUrl": "https://api.netgsm.com.tr/sms/send/get"
      }
    }
  }
  ```

**Implementation:** `TurkcellSmsService.cs` exists but needs configuration

---

#### **Gap 6B: Real WhatsApp Credentials** ⚠️ PARTIAL

**What's Missing:**
- Facebook Business Account setup
- WhatsApp Business API Phone Number ID
- Access Token
- Template approval from Meta

**Files to Update:**
- `appsettings.Production.json`:
  ```json
  {
    "WhatsApp": {
      "BaseUrl": "https://graph.facebook.com/v18.0",
      "AccessToken": "${WHATSAPP_ACCESS_TOKEN}",
      "BusinessPhoneNumberId": "${WHATSAPP_PHONE_NUMBER_ID}",
      "WebhookVerifyToken": "${WHATSAPP_WEBHOOK_TOKEN}"
    }
  }
  ```

**Implementation:** ✅ `WhatsAppBusinessService.cs` ready, just needs credentials

---

### 7️⃣ Configuration Gaps

#### **Gap 7A: Hardcoded Redemption Link** ⚠️ NEEDS FIX

**Current Issue:**

**File:** `SendSponsorshipLinkCommand.cs:98`
```csharp
// HARDCODED ❌
var redemptionLink = $"https://ziraai.com/redeem/{recipient.Code}";
```

**Should Use:**
```csharp
// Use configuration ✅
var baseUrl = _configuration["Sponsorship:RedemptionBaseUrl"]
    ?? throw new InvalidOperationException("Sponsorship:RedemptionBaseUrl not configured");
var redemptionLink = $"{baseUrl}/redeem/{recipient.Code}";
```

**Configuration Required:**
```json
{
  "Sponsorship": {
    "RedemptionBaseUrl": "https://ziraai.com"  // Production
    // "RedemptionBaseUrl": "https://ziraai-api-sit.up.railway.app"  // Staging
    // "RedemptionBaseUrl": "https://localhost:5001"  // Development
  }
}
```

---

#### **Gap 7B: Payment Gateway** ❌ NOT IMPLEMENTED

**What's Missing:**
- Payment provider integration (Stripe, iyzico, etc.)
- Webhook handling for payment confirmation
- Purchase status update workflow

**Current Behavior:**
```csharp
// PurchaseBulkSponsorshipCommand.cs
// Manual payment confirmation - no gateway
var purchase = new SponsorshipPurchase
{
    PaymentStatus = "Completed",  // ❌ Manually set
    PaymentReference = request.PaymentReference  // ❌ No validation
};
```

**Needed:**
```csharp
// Stripe example
var paymentIntent = await _stripeService.CreatePaymentIntentAsync(totalAmount);
purchase.PaymentReference = paymentIntent.Id;
purchase.PaymentStatus = "Pending";  // Update via webhook

// Webhook endpoint
POST /api/webhooks/stripe/payment-confirmed
{
  "payment_intent_id": "pi_xxx",
  "status": "succeeded"
}
```

---

## 🎯 Implementation Priority Plan

### **Phase 1: Mobile Deep Linking (CRITICAL)** 🚨
**Owner:** Mobile Team
**Timeline:** 2-3 days
**Blockers:** None - Backend ready

**Tasks:**
1. Configure deep links (iOS: Universal Links, Android: App Links)
2. Test redemption flow: SMS link → App opens → Code auto-fills
3. Handle app-not-installed scenario (Play Store → Install → Open)

**Mobile Team Deliverables:**
- [ ] Deep link configuration files
- [ ] `RedemptionScreen` with auto-fill logic
- [ ] Integration with `/api/sponsorship/redeem` endpoint

---

### **Phase 2: SMS Auto-Fill (CRITICAL)** 🚨
**Owner:** Mobile Team
**Timeline:** 1-2 days
**Blockers:** None - Backend templates ready

**Tasks:**
1. Request SMS read permission
2. Scan SMS inbox for `AGRI-YYYY-XXXX` pattern
3. Auto-fill redemption screen on app launch
4. Test with mock SMS messages

**Mobile Team Deliverables:**
- [ ] SMS permission handling
- [ ] Pattern extraction: `AGRI-\d{4}-[A-Z0-9]{4}`
- [ ] Auto-redemption workflow

---

### **Phase 3: Backend Configuration Fixes** ⚙️
**Owner:** Backend Team
**Timeline:** 1 day
**Blockers:** None

**Tasks:**
1. Fix hardcoded redemption URL → Use config
2. Add environment-specific configs
3. Test with staging/production URLs

**Backend Team Deliverables:**
- [ ] `Sponsorship:RedemptionBaseUrl` configuration
- [ ] Update `SendSponsorshipLinkCommand.cs:98`
- [ ] Environment variable validation

---

### **Phase 4: Business Logic Decision** 📋
**Owner:** Product/Business Team
**Timeline:** 1 day
**Blockers:** None

**Tasks:**
1. Decide: Allow upgrade-only OR allow stacking?
2. Document edge case behaviors
3. Update redemption logic

**Decisions Needed:**
- [ ] Sponsor M → Sponsor L: Upgrade allowed?
- [ ] Sponsor M → Sponsor M: Stack 30 days?
- [ ] Sponsor L → Sponsor S: Downgrade blocked?

---

### **Phase 5: Provider Integration (Optional)** 🔌
**Owner:** Backend Team
**Timeline:** 2-3 days
**Blockers:** Credentials, Business accounts

**Tasks:**
1. Setup NetGSM/Twilio account
2. Configure WhatsApp Business API
3. Get templates approved by Meta
4. Production testing

**Backend Team Deliverables:**
- [ ] Real SMS provider integration
- [ ] WhatsApp template approval
- [ ] Production environment variables

---

### **Phase 6: Payment Gateway (Future)** 💳
**Owner:** Backend Team
**Timeline:** 1 week
**Blockers:** Payment provider selection

**Tasks:**
1. Select payment gateway (Stripe, iyzico, etc.)
2. Implement webhook handlers
3. Update purchase workflow
4. Add refund logic

**Backend Team Deliverables:**
- [ ] Payment intent creation
- [ ] Webhook endpoint
- [ ] Status update workflow

---

## 📱 Mobile Team Handoff Package

### **Ready for Integration** ✅

#### **1. API Endpoints**

**Purchase Codes (Sponsor):**
```http
POST /api/v1/sponsorship/purchase-package
Authorization: Bearer {sponsor_token}

{
  "subscriptionTierId": 2,
  "quantity": 100,
  "totalAmount": 5000.00,
  "paymentReference": "STRIPE-PAY-123"
}

Response:
{
  "success": true,
  "data": {
    "generatedCodes": [
      {
        "code": "AGRI-2025-X3K9",
        "expiryDate": "2026-10-07",
        "redemptionLink": "https://ziraai.com/redeem/AGRI-2025-X3K9"
      }
    ]
  }
}
```

**Send Links (Sponsor):**
```http
POST /api/v1/sponsorship/send-link
Authorization: Bearer {sponsor_token}

{
  "recipients": [
    {
      "code": "AGRI-2025-X3K9",
      "phone": "+905551234567",
      "name": "Ahmet Yılmaz"
    }
  ],
  "channel": "WhatsApp"
}

Response:
{
  "success": true,
  "data": {
    "successCount": 1,
    "failureCount": 0
  }
}
```

**Redeem Code (Farmer):**
```http
POST /api/v1/sponsorship/redeem
Authorization: Bearer {farmer_token}

{
  "code": "AGRI-2025-X3K9"
}

Response:
{
  "success": true,
  "message": "Medium aboneliğiniz başarıyla aktive edildi!",
  "data": {
    "subscriptionId": 567,
    "tierName": "M",
    "startDate": "2025-10-07",
    "endDate": "2025-11-06",
    "sponsorName": "Tarım A.Ş."
  }
}
```

**Validate Code (Before Redemption):**
```http
GET /api/v1/sponsorship/validate/{code}
Authorization: Bearer {farmer_token}

Response:
{
  "success": true,
  "data": {
    "isValid": true,
    "subscriptionTier": "Medium",
    "expiryDate": "2026-10-07"
  }
}
```

---

#### **2. SMS/WhatsApp Message Format**

**SMS Content:**
```
🎁 Merhaba Ahmet Yılmaz!

Tarım A.Ş. size Medium abonelik paketi hediye etti!

📱 Hemen aktivasyon yapın:
https://ziraai.com/redeem/AGRI-2025-X3K9

⏰ Son kullanım: 07.10.2026
🌱 ZiraAI ile tarımınızı dijitalleştirin!

ZiraAI - Akıllı Tarım Çözümleri
```

**Key Information for Mobile:**
- Redemption Link: `https://ziraai.com/redeem/AGRI-2025-X3K9`
- Code Format: `AGRI-2025-X3K9`
- Regex Pattern: `AGRI-\d{4}-[A-Z0-9]{4}`

---

#### **3. Deep Link Configuration**

**iOS Universal Links:**
```json
// .well-known/apple-app-site-association
{
  "applinks": {
    "apps": [],
    "details": [
      {
        "appID": "TEAM_ID.com.ziraai.app",
        "paths": [
          "/redeem/*",
          "/ref/*",
          "/sponsor-request/*"
        ]
      }
    ]
  }
}
```

**Android App Links:**
```json
// .well-known/assetlinks.json
[
  {
    "relation": ["delegate_permission/common.handle_all_urls"],
    "target": {
      "namespace": "android_app",
      "package_name": "com.ziraai.app",
      "sha256_cert_fingerprints": [
        "SHA256_FINGERPRINT_HERE"
      ]
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

#### **4. Flutter Code Examples**

**Deep Link Handler:**
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
    if (uri.pathSegments.isNotEmpty && uri.pathSegments[0] == 'redeem') {
      String code = uri.pathSegments.last;
      Get.toNamed('/redemption', arguments: {'code': code});
    }
  }

  void dispose() {
    _sub?.cancel();
  }
}
```

**SMS Auto-Fill:**
```dart
import 'package:telephony/telephony.dart';

class SmsAutoFillService {
  final Telephony telephony = Telephony.instance;

  Future<String?> extractSponsorshipCode() async {
    bool? granted = await telephony.requestPhoneAndSmsPermissions;
    if (granted != true) return null;

    List<SmsMessage> messages = await telephony.getInboxSms(
      filter: SmsFilter.where(SmsColumn.DATE)
        .greaterThan(DateTime.now().subtract(Duration(hours: 24)).millisecondsSinceEpoch.toString())
    );

    RegExp pattern = RegExp(r'AGRI-\d{4}-[A-Z0-9]{4}');

    for (var msg in messages) {
      var match = pattern.firstMatch(msg.body ?? '');
      if (match != null) {
        return match.group(0);
      }
    }

    return null;
  }
}
```

**Redemption Screen:**
```dart
class RedemptionScreen extends StatefulWidget {
  @override
  _RedemptionScreenState createState() => _RedemptionScreenState();
}

class _RedemptionScreenState extends State<RedemptionScreen> {
  final TextEditingController _codeController = TextEditingController();
  final SmsAutoFillService _smsService = SmsAutoFillService();

  @override
  void initState() {
    super.initState();
    _autoFillCode();
  }

  Future<void> _autoFillCode() async {
    // Check if code passed via deep link
    String? code = Get.arguments?['code'];

    if (code != null) {
      _codeController.text = code;
      await _redeemCode(code);
      return;
    }

    // Try SMS auto-fill
    code = await _smsService.extractSponsorshipCode();
    if (code != null) {
      _codeController.text = code;
      // Show confirmation dialog
      bool? shouldRedeem = await showDialog(
        context: context,
        builder: (context) => AlertDialog(
          title: Text('Sponsorluk Kodu Bulundu'),
          content: Text('$code kodunu kullanmak ister misiniz?'),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: Text('İptal')
            ),
            TextButton(
              onPressed: () => Navigator.pop(context, true),
              child: Text('Kullan')
            ),
          ],
        ),
      );

      if (shouldRedeem == true) {
        await _redeemCode(code);
      }
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
          snackPosition: SnackPosition.TOP,
          backgroundColor: Colors.green,
        );
        Get.offNamed('/home');
      } else {
        Get.snackbar(
          'Hata',
          response.message,
          snackPosition: SnackPosition.TOP,
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
                hintText: 'AGRI-2025-X3K9',
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

## ✅ Checklist for Mobile Team

### **Pre-Development**
- [ ] Review API endpoints documentation
- [ ] Understand SMS message format and code pattern
- [ ] Setup deep link configuration (iOS + Android)
- [ ] Request test sponsorship codes from backend

### **Development - Deep Linking**
- [ ] Configure Universal Links (iOS)
- [ ] Configure App Links (Android)
- [ ] Test: Click SMS link → App opens → Correct screen
- [ ] Test: App not installed → Play Store → Install → Open with context
- [ ] Handle invalid/expired codes gracefully

### **Development - SMS Auto-Fill**
- [ ] Request SMS read permission
- [ ] Implement pattern extraction (`AGRI-\d{4}-[A-Z0-9]{4}`)
- [ ] Show confirmation dialog before auto-redemption
- [ ] Test with mock SMS messages
- [ ] Handle permission denied gracefully

### **Development - Redemption Flow**
- [ ] Build redemption screen UI
- [ ] Integrate with `/api/sponsorship/redeem`
- [ ] Handle success/error states
- [ ] Show tier details and sponsor info
- [ ] Navigate to home after successful redemption

### **Testing**
- [ ] Test with mock backend (current setup)
- [ ] Test deep link: `https://ziraai.com/redeem/AGRI-2025-TEST1`
- [ ] Test SMS auto-fill with sample message
- [ ] Test expired code handling
- [ ] Test already-used code handling
- [ ] Test without internet connection

---

## 🚀 Next Steps

1. **Mobile Team:** Start Phase 1 (Deep Linking) immediately - no blockers
2. **Backend Team:** Fix hardcoded URL (Phase 3) - 1 day task
3. **Product Team:** Decide multiple sponsorship logic (Phase 4)
4. **DevOps:** Prepare staging environment configs
5. **QA:** Create test plan for end-to-end sponsorship flow

---

## 📞 Support & Questions

**Backend Questions:** Contact backend team with API integration issues
**Business Logic:** Coordinate with product team on multiple sponsorship rules
**Testing:** Backend has mock services ready for mobile testing

---

**Document Version:** 1.0.0
**Last Updated:** 2025-10-07
**Status:** ✅ Ready for Mobile Integration
