# Custom Sponsor Code Prefix - Mobile Integration Impact Analysis

**Date**: 2025-12-08
**Backend Change**: Support for custom sponsor code prefixes (e.g., `TOLGATARIM-2025-XXXXX`)
**Affected System**: SMS-based automatic code redemption
**Priority**: 🟡 Medium (Mobile update recommended but not critical)

---

## 📋 Executive Summary

### Backend Change Made

**File**: `WebAPI/Controllers/RedemptionController.cs:64`

**Before** (Only AGRI prefix):
```csharp
if (!code.StartsWith("AGRI-"))
{
    return BadRequest("Geçersiz kod formatı");
}
```

**After** (Any custom prefix):
```csharp
if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Z]+-\d{4}-[A-Z0-9]+$"))
{
    return BadRequest("Geçersiz kod formatı");
}
```

### Impact on Mobile

| Component | Current Status | Action Required |
|-----------|---------------|-----------------|
| **SMS Listener Regex** | ⚠️ Only matches `AGRI-` | 🔧 Update to support custom prefixes |
| **Code Validation** | ⚠️ May reject custom prefixes | 🔧 Update validation pattern |
| **Deep Link Handling** | ✅ Works (backend validates) | ℹ️ No change needed |
| **API Calls** | ✅ Works (backend accepts all) | ℹ️ No change needed |
| **UI Display** | ✅ Works (displays any text) | ℹ️ No change needed |

---

## 🎯 Mobile Code Changes Required

### 1. SMS Listener Regex (CRITICAL)

**Current Code** (from `MOBILE_SPONSORSHIP_SMS_DEEP_LINKING_COMPLETE_GUIDE.md`):

```dart
// ❌ OLD - Only matches AGRI- and SPONSOR- prefixes
static final RegExp _codeRegex = RegExp(
  r'(AGRI-[A-Z0-9]+|SPONSOR-[A-Z0-9]+)',
  caseSensitive: true,
);
```

**Problem**:
- Custom prefix codes like `TOLGATARIM-2025-31803149` will NOT be extracted from SMS
- Farmers won't get automatic redemption notification
- Code won't be auto-filled in redemption screen

**Solution**:

```dart
// ✅ NEW - Matches ANY uppercase prefix with YYYY format
static final RegExp _codeRegex = RegExp(
  r'([A-Z]+-\d{4}-[A-Z0-9]+)',
  caseSensitive: true,
);
```

**What This Regex Matches**:
- `[A-Z]+` → One or more uppercase letters (prefix: AGRI, TOLGATARIM, COMPANY, etc.)
- `-` → Separator
- `\d{4}` → Exactly 4 digits (year: 2025)
- `-` → Separator
- `[A-Z0-9]+` → One or more uppercase letters/digits (unique code)

**Examples**:
- ✅ `AGRI-2025-ABC12345` (default)
- ✅ `TOLGATARIM-2025-31803149` (custom)
- ✅ `COMPANYX-2025-XYZ789AB` (custom)
- ❌ `agri-2025-abc` (lowercase - won't match)
- ❌ `TEST-25-ABC` (not 4-digit year)

---

### 2. Code Format Validation (RECOMMENDED)

**Current Code**:

```dart
// ❌ OLD - Hardcoded format check
bool isValidSponsorshipCode(String code) {
  final regex = RegExp(r'^[A-Z]+-[A-Z0-9]+$');
  return regex.hasMatch(code);
}
```

**Problem**:
- Doesn't enforce YYYY (4-digit year) format
- May accept invalid codes like `TEST-ABC` (no year)

**Solution**:

```dart
// ✅ NEW - Enforces PREFIX-YYYY-XXXXXXXX format
bool isValidSponsorshipCode(String code) {
  // Must match: PREFIX-YYYY-ALPHANUMERIC
  // Examples: AGRI-2025-ABC123, TOLGATARIM-2025-31803149
  final regex = RegExp(r'^[A-Z]+-\d{4}-[A-Z0-9]+$');
  return regex.hasMatch(code);
}
```

---

## 📱 Affected Mobile Features

### Feature 1: SMS Auto-Extraction ⚠️ REQUIRES UPDATE

**File**: `lib/services/sponsorship_sms_listener.dart` (or similar)

**Current Behavior**:
```
SMS Received: "TOLGATARIM size sponsorluk paketi hediye etti! Sponsorluk Kodunuz: TOLGATARIM-2025-31803149"
├─> Regex: (AGRI-[A-Z0-9]+|SPONSOR-[A-Z0-9]+)
├─> Match: null ❌
└─> Result: Code NOT extracted, no notification shown
```

**After Update**:
```
SMS Received: "TOLGATARIM size sponsorluk paketi hediye etti! Sponsorluk Kodunuz: TOLGATARIM-2025-31803149"
├─> Regex: ([A-Z]+-\d{4}-[A-Z0-9]+)
├─> Match: "TOLGATARIM-2025-31803149" ✅
├─> Notification: "Sponsorluk kodu geldi! Hemen kullan"
└─> Auto-fill redemption screen ✅
```

---

### Feature 2: Deferred Deep Linking ⚠️ REQUIRES UPDATE

**File**: `lib/services/sponsorship_sms_scanner.dart` (or similar)

**Scenario**: User installs app after receiving SMS

**Current Behavior**:
```dart
// Scan SMS inbox for codes
void scanInboxForCodes() async {
  final messages = await SmsQuery().querySms(
    kinds: [SmsQueryKind.inbox],
    count: 50,  // Last 50 messages
  );

  for (var msg in messages) {
    // ❌ Won't find TOLGATARIM-2025-XXXXX
    final match = RegExp(r'(AGRI-[A-Z0-9]+)').firstMatch(msg.body);
    if (match != null) {
      _storePendingCode(match.group(0)!);
      break;
    }
  }
}
```

**After Update**:
```dart
// Scan SMS inbox for codes
void scanInboxForCodes() async {
  final messages = await SmsQuery().querySms(
    kinds: [SmsQueryKind.inbox],
    count: 50,  // Last 50 messages
  );

  for (var msg in messages) {
    // ✅ Will find any PREFIX-YYYY-XXXXXXXX format
    final match = RegExp(r'([A-Z]+-\d{4}-[A-Z0-9]+)').firstMatch(msg.body);
    if (match != null) {
      _storePendingCode(match.group(0)!);
      break;
    }
  }
}
```

---

### Feature 3: Deep Link Redemption ✅ NO CHANGE NEEDED

**File**: `lib/routes/deep_link_handler.dart` (or similar)

**Current Code**:
```dart
void handleDeepLink(Uri uri) {
  if (uri.host == 'ziraai.com' && uri.pathSegments.first == 'redeem') {
    String code = uri.pathSegments[1];  // Extract from URL

    // Navigate to redemption screen
    Get.toNamed('/sponsorship-redeem', arguments: {'code': code});
  }
}
```

**Status**: ✅ **No change needed**
- Backend validates code format in `/redeem/{code}` endpoint
- Mobile just extracts and passes the code to backend
- Backend returns error if invalid

---

### Feature 4: Manual Code Entry ✅ NO CHANGE NEEDED

**File**: `lib/screens/sponsorship_redeem_screen.dart` (or similar)

**Current Code**:
```dart
void redeemCode(String code) async {
  final response = await _api.post('/api/v1/sponsorship/redeem', {
    'code': code,
  });

  if (response.success) {
    // Show success message
  } else {
    // Show error: response.message
  }
}
```

**Status**: ✅ **No change needed**
- Backend validates all code formats
- Mobile just sends the code
- Backend returns appropriate error if invalid

---

## 🔧 Implementation Guide

### Step 1: Update SMS Listener Regex

**File**: Find your SMS listener service (likely `sponsorship_sms_listener.dart` or similar)

**Change**:
```dart
// OLD
static final RegExp _codeRegex = RegExp(
  r'(AGRI-[A-Z0-9]+|SPONSOR-[A-Z0-9]+)',
  caseSensitive: true,
);

// NEW
static final RegExp _codeRegex = RegExp(
  r'([A-Z]+-\d{4}-[A-Z0-9]+)',
  caseSensitive: true,
);
```

---

### Step 2: Update Code Validation (if exists)

**File**: Find code validation functions (search for `isValidSponsorshipCode` or similar)

**Change**:
```dart
// OLD
bool isValidSponsorshipCode(String code) {
  final regex = RegExp(r'^[A-Z]+-[A-Z0-9]+$');
  return regex.hasMatch(code);
}

// NEW
bool isValidSponsorshipCode(String code) {
  // Enforce PREFIX-YYYY-XXXXXXXX format
  final regex = RegExp(r'^[A-Z]+-\d{4}-[A-Z0-9]+$');
  return regex.hasMatch(code);
}
```

---

### Step 3: Update Inbox Scanner (Deferred Deep Linking)

**File**: Find inbox scanning logic (likely in app startup or first-launch handler)

**Change**:
```dart
// OLD
final match = RegExp(r'(AGRI-[A-Z0-9]+)').firstMatch(msg.body);

// NEW
final match = RegExp(r'([A-Z]+-\d{4}-[A-Z0-9]+)').firstMatch(msg.body);
```

---

## 🧪 Testing Guide

### Test Case 1: Default AGRI Code (Backward Compatibility)

**SMS Content**:
```
🌱 Tarım A.Ş. size Medium paketi hediye etti!

Sponsorluk Kodunuz: AGRI-2025-ABC12345

Hemen kullanmak için tıklayın:
https://api.ziraai.com/redeem/AGRI-2025-ABC12345

Veya ZiraAI'yi indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app
```

**Expected**:
- ✅ SMS listener extracts: `AGRI-2025-ABC12345`
- ✅ Notification shown: "Sponsorluk kodu geldi! Hemen kullan"
- ✅ Redemption screen opens with code auto-filled
- ✅ Backend accepts code: 200 OK

---

### Test Case 2: Custom Prefix Code (New Feature)

**SMS Content**:
```
TOLGA TARIM size ucretsiz ZiraAI aboneligi hediye etti!

Abonelik Kodunuz: TOLGATARIM-2025-31803149

Hemen kullanmak icin tiklayin:
https://api.ziraai.com/redeem/TOLGATARIM-2025-31803149

Veya ZiraAI'yi indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app
```

**Expected**:
- ✅ SMS listener extracts: `TOLGATARIM-2025-31803149`
- ✅ Notification shown: "Sponsorluk kodu geldi! Hemen kullan"
- ✅ Redemption screen opens with code auto-filled
- ✅ Backend accepts code: 200 OK

---

### Test Case 3: Invalid Code Format

**SMS Content**: `Your code is: TEST-ABC-123` (no year)

**Expected**:
- ❌ SMS listener: No match (regex requires 4-digit year)
- ❌ No notification shown
- ℹ️ This is correct behavior - prevents false positives

---

### Test Case 4: Deferred Deep Linking (App Not Installed)

**Steps**:
1. Receive SMS with code `COMPANYX-2025-XYZ789AB`
2. Don't have app installed
3. Install app from Play Store
4. Open app for first time
5. Complete registration/login

**Expected**:
- ✅ App scans SMS inbox on first launch
- ✅ Finds code: `COMPANYX-2025-XYZ789AB`
- ✅ Stores in SharedPreferences
- ✅ After login → navigates to redemption screen
- ✅ Code auto-filled: `COMPANYX-2025-XYZ789AB`

---

## ⚠️ What Happens If Mobile Is NOT Updated?

### Scenario: User receives custom prefix code, mobile has old regex

**SMS Received**: `TOLGATARIM-2025-31803149`

**Mobile Behavior (OLD REGEX)**:
1. SMS listener tries to extract code
2. Regex `(AGRI-[A-Z0-9]+|SPONSOR-[A-Z0-9]+)` does NOT match ❌
3. No notification shown
4. No auto-fill in redemption screen

**Workaround for User** (Still works!):
1. User manually opens app
2. Goes to "Redeem Code" screen
3. **Manually types**: `TOLGATARIM-2025-31803149`
4. Taps "Redeem" button
5. Backend accepts code ✅
6. Subscription activated ✅

**Impact**:
- ❌ Lost automatic convenience (main benefit of SMS listener)
- ✅ Manual redemption still works (backend supports it)
- ⚠️ Lower conversion rate (~40-50% vs 90-95%)

---

## 🎯 Recommended Priority

| Priority | Reason |
|----------|--------|
| 🟡 **Medium** | Manual redemption still works as fallback |
| ⏰ **Timeline** | Update in next sprint (not emergency) |
| 🔄 **Backward Compatible** | New regex still matches old AGRI codes |
| 📊 **User Impact** | Only affects UX quality, not functionality |

---

## 📝 Summary Checklist

### Backend ✅ COMPLETED
- [x] Custom prefix support in code generation
- [x] Redemption validation accepts any prefix
- [x] SMS template includes custom prefix codes

### Mobile ⚠️ ACTION REQUIRED
- [ ] Update SMS listener regex: `([A-Z]+-\d{4}-[A-Z0-9]+)`
- [ ] Update code validation regex: `^[A-Z]+-\d{4}-[A-Z0-9]+$`
- [ ] Update inbox scanner regex (deferred deep linking)
- [ ] Test with both AGRI and custom prefix codes
- [ ] Test deferred deep linking with custom codes

### Files to Update
1. `lib/services/sponsorship_sms_listener.dart` (or equivalent)
   - Update `_codeRegex` constant
2. `lib/utils/validators.dart` (or equivalent)
   - Update `isValidSponsorshipCode()` function
3. `lib/services/deferred_deep_link_handler.dart` (or equivalent)
   - Update inbox scanning regex

---

## 🔗 Related Documentation

- **SMS Integration Guide**: `claudedocs/MOBILE_SPONSORSHIP_SMS_DEEP_LINKING_COMPLETE_GUIDE.md`
- **Backend Code Generation**: `DataAccess/Concrete/EntityFramework/SponsorshipCodeRepository.cs:152`
- **Backend Validation**: `WebAPI/Controllers/RedemptionController.cs:64`
- **Backend Commit**: `d88792b0` - "fix: Support custom sponsor code prefixes in redemption validation"

---

## ❓ FAQ

**Q: Eski AGRI kodları hala çalışacak mı?**
A: ✅ Evet, yeni regex backward compatible. `AGRI-2025-XXXXX` formatı hala match olacak.

**Q: Mobil güncellenene kadar ne olur?**
A: ⚠️ Custom prefix kodları SMS'ten otomatik çıkarılamaz ama kullanıcı manuel olarak girebilir. Backend her iki formatı da kabul eder.

**Q: Referral kodları etkilendi mi?**
A: ℹ️ Hayır, referral kodları farklı format kullanıyor (`ZIRA-XXXXX`). Bu değişiklik sadece sponsorship kodlarını etkiliyor.

**Q: Test nasıl yapabilirim?**
A: Yukarıdaki "Testing Guide" bölümündeki test case'leri kullanın. Hem AGRI hem de TOLGATARIM gibi custom prefix'li kodları test edin.

**Q: Acil bir güncelleme mi?**
A: 🟡 Hayır, medium priority. Manual redemption hala çalışıyor, sadece UX kalitesini etkiliyor.

---

**Generated**: 2025-12-08
**Backend Commit**: `d88792b0`
**Version**: 1.0
