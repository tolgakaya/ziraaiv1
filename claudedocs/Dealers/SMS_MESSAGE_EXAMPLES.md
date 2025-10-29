# Dealer Invitation SMS Message Examples

**Purpose:** Exact SMS message examples for mobile team to test SMS parsing
**Date:** 2025-01-25
**Target:** Mobile Development Team

---

## 📱 Complete SMS Message Examples

### Example 1: Development Environment

```
🎁 ABC Tarım A.Ş. Bayilik Daveti!

Davet Kodunuz: DEALER-a1b2c3d4e5f6789012345678abcdef01

Hemen katılmak için tıklayın:
https://localhost:5001/dealer-invitation/DEALER-a1b2c3d4e5f6789012345678abcdef01

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app.dev
```

**Character Breakdown:**
- Total length: ~285 characters
- SMS segments: 2 (160 chars per segment)
- Emoji: 🎁 (counts as 2 chars)
- Token: 32 hex characters (lowercase only)
- Deep link: Full URL with token

---

### Example 2: Staging Environment

```
🎁 Yeşil Vadi Tarım Ltd. Şti. Bayilik Daveti!

Davet Kodunuz: DEALER-f7e8d9c0b1a29384756efedcba098765

Hemen katılmak için tıklayın:
https://ziraai-api-sit.up.railway.app/dealer-invitation/DEALER-f7e8d9c0b1a29384756efedcba098765

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app.staging
```

**Character Breakdown:**
- Total length: ~330 characters
- SMS segments: 3 (160 chars per segment)
- Company name: Can be long (Turkish characters supported)
- Token: 32 hex characters
- Deep link: Full Railway URL

---

### Example 3: Production Environment

```
🎁 Marmara Organik Ürünler San. ve Tic. A.Ş. Bayilik Daveti!

Davet Kodunuz: DEALER-0123456789abcdef0123456789abcdef

Hemen katılmak için tıklayın:
https://ziraai.com/dealer-invitation/DEALER-0123456789abcdef0123456789abcdef

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app
```

**Character Breakdown:**
- Total length: ~300 characters
- SMS segments: 2 (160 chars per segment)
- Company name: Long Turkish company name with special chars
- Token: 32 hex characters
- Deep link: Production domain

---

## 🔍 Token Patterns & Regex

### Token Format Specification

| Component | Details |
|-----------|---------|
| **Prefix** | `DEALER-` (always uppercase) |
| **Token Length** | Exactly 32 characters |
| **Allowed Characters** | `a-f` (lowercase), `0-9` (digits only) |
| **Format** | Hexadecimal (lowercase) |
| **Generation** | `Guid.NewGuid().ToString("N")` in C# |
| **Example** | `a1b2c3d4e5f6789012345678abcdef01` |

### Valid Token Examples

✅ **VALID:**
```
DEALER-0123456789abcdef0123456789abcdef
DEALER-ffffffffffffffffffffffffffffffff
DEALER-00000000000000000000000000000000
DEALER-a1b2c3d4e5f6789012345678abcdef01
DEALER-abcdefabcdefabcdefabcdefabcdefab
```

❌ **INVALID:**
```
DEALER-A1B2C3D4E5F6789012345678ABCDEF01  ❌ Uppercase not allowed
DEALER-g1h2i3j4k5l6789012345678mnopqr01  ❌ Invalid chars (g,h,i,j,k,l,m,n,o,p,q,r)
DEALER-a1b2c3d4e5f6                       ❌ Too short (only 16 chars)
DEALER-a1b2c3d4e5f6789012345678abcdef0123 ❌ Too long (34 chars)
dealer-a1b2c3d4e5f6789012345678abcdef01   ❌ Lowercase prefix
DEALER_a1b2c3d4e5f6789012345678abcdef01   ❌ Underscore instead of dash
```

---

## 📝 Regex Patterns for Parsing

### Pattern 1: Extract Token from SMS (Recommended)

```regex
DEALER-([a-f0-9]{32})
```

**Explanation:**
- `DEALER-` - Literal prefix match
- `([a-f0-9]{32})` - Capture group for exactly 32 hex chars
- `[a-f0-9]` - Lowercase letters a-f and digits 0-9
- `{32}` - Exactly 32 characters

**Flutter Example:**
```dart
String extractTokenFromSms(String smsBody) {
  final regex = RegExp(r'DEALER-([a-f0-9]{32})');
  final match = regex.firstMatch(smsBody);

  if (match != null) {
    // Return ONLY the token part (without DEALER- prefix)
    return match.group(1)!; // Returns: "a1b2c3d4e5f6789012345678abcdef01"
  }

  return null;
}
```

### Pattern 2: Extract Deep Link from SMS

```regex
https?://[^\s]+/dealer-invitation/DEALER-([a-f0-9]{32})
```

**Explanation:**
- `https?://` - Match http or https
- `[^\s]+` - Match any non-whitespace (domain + path)
- `/dealer-invitation/` - Literal path
- `DEALER-([a-f0-9]{32})` - Token pattern with capture group

**Flutter Example:**
```dart
String extractDeepLinkFromSms(String smsBody) {
  final regex = RegExp(
    r'(https?://[^\s]+/dealer-invitation/DEALER-[a-f0-9]{32})',
    caseSensitive: false
  );
  final match = regex.firstMatch(smsBody);

  if (match != null) {
    // Returns full deep link
    return match.group(1)!;
    // Example: "https://ziraai.com/dealer-invitation/DEALER-a1b2c3d4..."
  }

  return null;
}
```

### Pattern 3: Extract Play Store Link

```regex
https://play\.google\.com/store/apps/details\?id=([a-zA-Z0-9._]+)
```

**Flutter Example:**
```dart
String extractPlayStoreLink(String smsBody) {
  final regex = RegExp(
    r'https://play\.google\.com/store/apps/details\?id=([a-zA-Z0-9._]+)'
  );
  final match = regex.firstMatch(smsBody);

  if (match != null) {
    return match.group(0)!; // Full URL
    // Or return match.group(1)! for just the package name
  }

  return null;
}
```

---

## 🧪 Test Cases for Mobile Team

### Test Case 1: Simple Company Name

**Input SMS:**
```
🎁 ABC Ltd. Bayilik Daveti!

Davet Kodunuz: DEALER-abc123def456789012345678fedcba90

Hemen katılmak için tıklayın:
https://ziraai.com/dealer-invitation/DEALER-abc123def456789012345678fedcba90

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app
```

**Expected Extraction:**
- Token: `abc123def456789012345678fedcba90`
- Deep Link: `https://ziraai.com/dealer-invitation/DEALER-abc123def456789012345678fedcba90`
- Play Store: `https://play.google.com/store/apps/details?id=com.ziraai.app`

---

### Test Case 2: Long Turkish Company Name with Special Characters

**Input SMS:**
```
🎁 Anadolu Çiftçi Kooperatifleri Birliği San. ve Tic. A.Ş. Bayilik Daveti!

Davet Kodunuz: DEALER-0000000000000000000000000000000f

Hemen katılmak için tıklayın:
https://ziraai-api-sit.up.railway.app/dealer-invitation/DEALER-0000000000000000000000000000000f

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app.staging
```

**Expected Extraction:**
- Token: `0000000000000000000000000000000f`
- Deep Link: `https://ziraai-api-sit.up.railway.app/dealer-invitation/DEALER-0000000000000000000000000000000f`
- Play Store: `https://play.google.com/store/apps/details?id=com.ziraai.app.staging`

**Notes:**
- Turkish characters (Ş, İ, Ç, ı, ğ, ü) in company name should NOT affect token parsing
- Token is at the end of hexadecimal range (ending with 'f')

---

### Test Case 3: All Zeros Token

**Input SMS:**
```
🎁 Test Tarım Bayilik Daveti!

Davet Kodunuz: DEALER-00000000000000000000000000000000

Hemen katılmak için tıklayın:
https://localhost:5001/dealer-invitation/DEALER-00000000000000000000000000000000

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app.dev
```

**Expected Extraction:**
- Token: `00000000000000000000000000000000`
- Deep Link: `https://localhost:5001/dealer-invitation/DEALER-00000000000000000000000000000000`
- Play Store: `https://play.google.com/store/apps/details?id=com.ziraai.app.dev`

---

### Test Case 4: All F's Token

**Input SMS:**
```
🎁 Demo Sponsor Bayilik Daveti!

Davet Kodunuz: DEALER-ffffffffffffffffffffffffffffffff

Hemen katılmak için tıklayın:
https://ziraai.com/dealer-invitation/DEALER-ffffffffffffffffffffffffffffffff

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app
```

**Expected Extraction:**
- Token: `ffffffffffffffffffffffffffffffff`
- Deep Link: `https://ziraai.com/dealer-invitation/DEALER-ffffffffffffffffffffffffffffffff`
- Play Store: `https://play.google.com/store/apps/details?id=com.ziraai.app`

---

### Test Case 5: Mixed Alphanumeric Token

**Input SMS:**
```
🎁 XYZ Organik Bayilik Daveti!

Davet Kodunuz: DEALER-a1b2c3d4e5f6789012345678abcdef01

Hemen katılmak için tıklayın:
https://ziraai.com/dealer-invitation/DEALER-a1b2c3d4e5f6789012345678abcdef01

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app
```

**Expected Extraction:**
- Token: `a1b2c3d4e5f6789012345678abcdef01`
- Deep Link: `https://ziraai.com/dealer-invitation/DEALER-a1b2c3d4e5f6789012345678abcdef01`
- Play Store: `https://play.google.com/store/apps/details?id=com.ziraai.app`

---

## 🔒 Security Considerations

### Token Validation

**Client-Side Validation (Before API Call):**
```dart
bool isValidDealerToken(String token) {
  // Must be exactly 32 characters
  if (token.length != 32) return false;

  // Must contain only lowercase hex characters
  final regex = RegExp(r'^[a-f0-9]{32}$');
  return regex.hasMatch(token);
}
```

**Example Usage:**
```dart
String extractedToken = extractTokenFromSms(smsBody);

if (extractedToken != null && isValidDealerToken(extractedToken)) {
  // ✅ Valid token - proceed to API call
  await callApiWithToken(extractedToken);
} else {
  // ❌ Invalid token - show error
  showError("Geçersiz davetiye kodu");
}
```

---

## 📊 SMS Segment Calculation

### Formula
```
SMS Length = Base Message + Company Name Length + URLs
```

### Breakdown Example (Staging)

```
Component                          | Length
-----------------------------------|--------
"🎁 "                              | 3 chars (emoji = 2 + space = 1)
Company Name                       | ~30 chars (variable)
" Bayilik Daveti!\n\n"            | 16 chars
"Davet Kodunuz: DEALER-"          | 23 chars
Token (32 chars)                   | 32 chars
"\n\nHemen katılmak için tıklayın:\n" | 34 chars
Deep Link (staging)                | ~97 chars
"\n\nVeya uygulamayı indirin:\n"  | 30 chars
Play Store Link (staging)          | ~73 chars
-----------------------------------|--------
TOTAL                              | ~338 chars = 3 SMS segments
```

### SMS Segments by Environment

| Environment | Avg Length | SMS Segments | Cost Factor |
|-------------|-----------|--------------|-------------|
| **Development** | 285 chars | 2 segments | 1.0x |
| **Staging** | 338 chars | 3 segments | 1.5x |
| **Production** | 300 chars | 2 segments | 1.0x |

**Note:** Turkish characters count as 2 chars in GSM-7 encoding

---

## 🎯 Complete Flutter Implementation

```dart
class DealerInvitationSmsParser {
  /// Extract dealer invitation token from SMS
  static String? extractToken(String smsBody) {
    final regex = RegExp(r'DEALER-([a-f0-9]{32})');
    final match = regex.firstMatch(smsBody);
    return match?.group(1); // Returns token WITHOUT prefix
  }

  /// Extract deep link from SMS
  static String? extractDeepLink(String smsBody) {
    final regex = RegExp(
      r'(https?://[^\s]+/dealer-invitation/DEALER-[a-f0-9]{32})',
      caseSensitive: false
    );
    final match = regex.firstMatch(smsBody);
    return match?.group(1);
  }

  /// Validate token format
  static bool isValidToken(String token) {
    if (token.length != 32) return false;
    final regex = RegExp(r'^[a-f0-9]{32}$');
    return regex.hasMatch(token);
  }

  /// Parse complete SMS and extract all components
  static DealerInvitationSmsData? parseSms(String smsBody) {
    final token = extractToken(smsBody);
    final deepLink = extractDeepLink(smsBody);

    if (token == null || !isValidToken(token)) {
      return null;
    }

    return DealerInvitationSmsData(
      token: token,
      deepLink: deepLink,
      rawSms: smsBody,
    );
  }
}

class DealerInvitationSmsData {
  final String token;
  final String? deepLink;
  final String rawSms;

  DealerInvitationSmsData({
    required this.token,
    this.deepLink,
    required this.rawSms,
  });
}

// Usage Example:
void handleIncomingSms(String smsBody) {
  final data = DealerInvitationSmsParser.parseSms(smsBody);

  if (data != null) {
    print('✅ Valid dealer invitation SMS detected');
    print('Token: ${data.token}');
    print('Deep Link: ${data.deepLink}');

    // Store token for later use
    await storeInvitationToken(data.token);

    // Show notification or navigate
    showDealerInvitationNotification(data);
  } else {
    print('❌ Not a valid dealer invitation SMS');
  }
}
```

---

## ⚠️ Common Pitfalls & Solutions

### Pitfall 1: Including "DEALER-" in API Call

❌ **WRONG:**
```dart
String extractedToken = "DEALER-abc123def456"; // With prefix
await api.acceptInvitation(extractedToken);
```

✅ **CORRECT:**
```dart
String extractedToken = "abc123def456..."; // WITHOUT prefix
await api.acceptInvitation(extractedToken);
```

### Pitfall 2: Case Sensitivity

❌ **WRONG:**
```dart
// Token should be lowercase only
final regex = RegExp(r'DEALER-([a-fA-F0-9]{32})'); // Allows uppercase
```

✅ **CORRECT:**
```dart
// Only lowercase hex characters
final regex = RegExp(r'DEALER-([a-f0-9]{32})'); // Lowercase only
```

### Pitfall 3: Partial Token Match

❌ **WRONG:**
```dart
// Matches ANY length token
final regex = RegExp(r'DEALER-([a-f0-9]+)');
```

✅ **CORRECT:**
```dart
// Matches EXACTLY 32 characters
final regex = RegExp(r'DEALER-([a-f0-9]{32})');
```

### Pitfall 4: Not Handling Turkish Characters in Company Name

✅ **SOLUTION:**
```dart
// SMS may contain Turkish chars (Ş, İ, Ç, Ğ, Ü, Ö)
// This should NOT affect token extraction
// Token regex is independent of company name

String sms = "🎁 Türk Çiftçi Şirketi Bayilik Daveti!\n\n" +
             "Davet Kodunuz: DEALER-abc123...";

// Token extraction works regardless of Turkish chars
String token = extractToken(sms); // ✅ Works correctly
```

---

## 📱 SMS Permission Handling

### Required Permissions (AndroidManifest.xml)

```xml
<uses-permission android:name="android.permission.RECEIVE_SMS" />
<uses-permission android:name="android.permission.READ_SMS" />
```

### Flutter SMS Reading Example

```dart
import 'package:telephony/telephony.dart';

class SmsReader {
  final Telephony telephony = Telephony.instance;

  Future<void> requestSmsPermission() async {
    bool? permissionsGranted = await telephony.requestPhoneAndSmsPermissions;

    if (permissionsGranted ?? false) {
      print('✅ SMS permissions granted');
      await listenForIncomingSms();
    } else {
      print('❌ SMS permissions denied');
    }
  }

  Future<void> listenForIncomingSms() async {
    telephony.listenIncomingSms(
      onNewMessage: (SmsMessage message) {
        print('📨 New SMS received from: ${message.address}');

        // Parse SMS body
        final data = DealerInvitationSmsParser.parseSms(message.body ?? '');

        if (data != null) {
          handleDealerInvitation(data);
        }
      },
      listenInBackground: false,
    );
  }

  Future<void> scanExistingSms() async {
    List<SmsMessage> messages = await telephony.getInboxSms(
      columns: [SmsColumn.ADDRESS, SmsColumn.BODY, SmsColumn.DATE],
      sortOrder: [
        OrderBy(SmsColumn.DATE, sort: Sort.DESC),
      ],
    );

    for (var message in messages.take(50)) { // Check last 50 messages
      final data = DealerInvitationSmsParser.parseSms(message.body ?? '');

      if (data != null) {
        print('✅ Found dealer invitation in existing SMS');
        handleDealerInvitation(data);
        break; // Stop after first match
      }
    }
  }
}
```

---

## 🔗 Environment URLs Reference

| Environment | Base URL | Package Name | Deep Link Example |
|-------------|----------|--------------|-------------------|
| **Development** | `https://localhost:5001` | `com.ziraai.app.dev` | `https://localhost:5001/dealer-invitation/DEALER-abc...` |
| **Staging** | `https://ziraai-api-sit.up.railway.app` | `com.ziraai.app.staging` | `https://ziraai-api-sit.up.railway.app/dealer-invitation/DEALER-abc...` |
| **Production** | `https://ziraai.com` | `com.ziraai.app` | `https://ziraai.com/dealer-invitation/DEALER-abc...` |

---

## ✅ Final Checklist for Mobile Team

- [ ] Implement SMS reading permission request
- [ ] Add SMS listener for incoming messages
- [ ] Implement token extraction regex (`DEALER-([a-f0-9]{32})`)
- [ ] Validate token format (exactly 32 hex chars)
- [ ] Extract deep link from SMS
- [ ] Store extracted token in local storage
- [ ] Handle deep link click (app already installed)
- [ ] Handle SMS-based token (app just installed)
- [ ] Test with all 5 test cases above
- [ ] Test with Turkish company names
- [ ] Verify API calls use token WITHOUT "DEALER-" prefix
- [ ] Test deep link intent filters (Android)
- [ ] Test universal links (iOS)
- [ ] Handle expired invitations gracefully
- [ ] Show user-friendly error messages

---

**Last Updated:** 2025-01-25
**Contact:** Backend Team for API questions, Mobile Lead for implementation
