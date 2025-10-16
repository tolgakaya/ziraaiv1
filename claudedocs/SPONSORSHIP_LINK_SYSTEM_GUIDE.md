# Sponsorship Link Distribution System - Endpoint Usage Guide

## Overview
The Sponsorship Link Distribution System allows sponsors to create redemption codes and distribute them to farmers via SMS/WhatsApp links. Farmers can redeem these codes through public links that automatically create accounts and provide access to the platform.

## System Architecture

```
Sponsor → Creates Codes → Sends Links → Farmer Clicks → Auto Account Creation → Platform Access
```

## API Endpoints

### Base URL
- **Development**: `https://localhost:5001`
- **API Version**: `v1`
- **Public Links**: `https://localhost:5001/redeem/{code}`

## 1. Sponsor Code Creation (Prerequisite)

Before distributing links, sponsors must create sponsorship codes.

### Endpoint
```http
POST /api/v1/sponsorship/codes
Authorization: Bearer {sponsor_jwt_token}
Content-Type: application/json
```

### Request Body
```json
{
  "farmerName": "Ahmet Yılmaz",
  "farmerPhone": "5551234567", 
  "amount": 100.00,
  "description": "Domates tohumu desteği",
  "expiryDate": "2025-12-31T23:59:59"
}
```

### Response
```json
{
  "success": true,
  "message": "Sponsorship code created successfully",
  "data": {
    "id": 123,
    "code": "SPONSOR-2025-ABC123",
    "farmerName": "Ahmet Yılmaz",
    "farmerPhone": "5551234567",
    "amount": 100.00,
    "description": "Domates tohumu desteği",
    "expiryDate": "2025-12-31T23:59:59",
    "isRedeemed": false,
    "createdDate": "2025-08-14T10:00:00"
  }
}
```

## 2. Link Distribution (SMS/WhatsApp)

Send redemption links to farmers via SMS or WhatsApp.

### Endpoint
```http
POST /api/v1/sponsorship/send-link
Authorization: Bearer {sponsor_jwt_token}
Content-Type: application/json
```

### Request Body
```json
{
  "codes": [
    {
      "code": "SPONSOR-2025-ABC123",
      "recipientName": "Ahmet Yılmaz",
      "recipientPhone": "5551234567"
    },
    {
      "code": "SPONSOR-2025-XYZ789", 
      "recipientName": "Fatma Demir",
      "recipientPhone": "5559876543"
    }
  ],
  "sendVia": "WhatsApp",
  "customMessage": "Size özel tarımsal destek kodunuz hazır! Hemen kullanın:"
}
```

### Response
```json
{
  "success": true,
  "message": "Links sent successfully to 2 recipients",
  "data": {
    "sentCount": 2,
    "failedCount": 0,
    "results": [
      {
        "code": "SPONSOR-2025-ABC123",
        "recipientPhone": "5551234567",
        "redemptionLink": "https://localhost:5001/redeem/SPONSOR-2025-ABC123",
        "sentStatus": "Success",
        "sentDate": "2025-08-14T10:30:00",
        "deliveryStatus": "Delivered"
      },
      {
        "code": "SPONSOR-2025-XYZ789",
        "recipientPhone": "5559876543", 
        "redemptionLink": "https://localhost:5001/redeem/SPONSOR-2025-XYZ789",
        "sentStatus": "Success",
        "sentDate": "2025-08-14T10:30:01",
        "deliveryStatus": "Delivered"
      }
    ]
  }
}
```

### What Happens Internally:
1. **Link Generation**: Creates unique redemption URLs for each code
2. **Message Formatting**: Combines custom message with redemption link
3. **SMS/WhatsApp Sending**: Delivers messages via configured provider
4. **Database Updates**: Records send date, delivery status, and recipient info
5. **Analytics Tracking**: Enables click and usage statistics

## 3. Link Statistics and Analytics

Monitor sent links performance and usage statistics.

### Endpoint
```http
GET /api/v1/sponsorship/link-statistics?sponsorUserId=123
Authorization: Bearer {sponsor_jwt_token}
```

### Query Parameters
- `sponsorUserId` (required): ID of the sponsor user
- `startDate` (optional): Filter from date (ISO 8601)
- `endDate` (optional): Filter to date (ISO 8601)
- `sendVia` (optional): Filter by sending method (SMS, WhatsApp)

### Response
```json
{
  "success": true,
  "data": {
    "totalLinks": 10,
    "sentLinks": 8,
    "deliveredLinks": 7,
    "clickedLinks": 5,
    "redeemedLinks": 3,
    "statistics": [
      {
        "code": "SPONSOR-2025-ABC123",
        "recipientName": "Ahmet Yılmaz",
        "recipientPhone": "5551234567",
        "redemptionLink": "https://localhost:5001/redeem/SPONSOR-2025-ABC123",
        "linkSentDate": "2025-08-14T10:30:00",
        "linkSentVia": "WhatsApp",
        "linkDelivered": true,
        "linkClickCount": 3,
        "linkClickDate": "2025-08-14T11:15:22",
        "isRedeemed": true,
        "redemptionDate": "2025-08-14T11:15:30",
        "lastClickIpAddress": "192.168.1.100",
        "farmerAccountCreated": true
      }
    ]
  }
}
```

### Analytics Insights:
- **Delivery Rate**: `deliveredLinks / sentLinks * 100`
- **Click-Through Rate**: `clickedLinks / deliveredLinks * 100`  
- **Conversion Rate**: `redeemedLinks / clickedLinks * 100`
- **Peak Usage Times**: Click timestamps analysis
- **Geographic Distribution**: IP address analysis

## 4. Farmer Redemption Process

### A) Mobile-First Deep Linking (Primary Method)

**For Mobile App Users:**
When farmers click WhatsApp/SMS link on mobile device:

```
Flow: WhatsApp Link → Browser → Deep Link → ZiraAI Mobile App
```

**Smart Redirect Logic:**
1. **User-Agent Detection**: System detects mobile device
2. **Deep Link Attempt**: Redirects to `ziraai://redeem?code=XXX&token=YYY`
3. **App Opens**: If ZiraAI app installed, opens directly with auto-login
4. **Fallback**: If app not installed, redirects to App Store/Google Play

**Technical Implementation:**
```http
GET /redeem/SPONSOR-2025-ABC123
User-Agent: Mozilla/5.0 (iPhone; CPU iPhone OS 14_0...)

Response: Smart HTML page with:
- Immediate deep link attempt
- App Store fallback after 3 seconds
- Auto-login token injection
```

### B) Web Browser Access (Fallback Method)

**For Desktop/Web Users:**
When farmers click the link from desktop browsers:

```http
GET /redeem/SPONSOR-2025-ABC123
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64)...

Response: Traditional web page with dashboard redirect
```

### C) API Response Format (Programmatic Access)

**JSON Response for Apps/APIs:**
```http
GET /api/v1/redeem/SPONSOR-2025-ABC123
Accept: application/json
```

### HTML Response Examples

#### Mobile Redirect Page (Smart Deep Linking)
```html
<!DOCTYPE html>
<html>
<head>
    <title>Sponsorluk Kodu Başarıyla Kullanıldı</title>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }
        .container { background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .success { color: #28a745; }
        .info { background: #e3f2fd; padding: 15px; border-radius: 4px; margin: 20px 0; }
        .credentials { background: #fff3cd; padding: 15px; border-radius: 4px; margin: 20px 0; border-left: 4px solid #ffc107; }
        .button { background: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block; margin: 10px 0; }
    </style>
</head>
<body>
    <div class="container">
        <h2 class="success">🎉 Tebrikler Ahmet Yılmaz!</h2>
        <p>100 TL değerindeki sponsorluk kodunuz başarıyla hesabınıza eklendi.</p>
        
        <div class="info">
            <h4>Sponsorluk Detayları:</h4>
            <p><strong>Kod:</strong> SPONSOR-2025-ABC123</p>
            <p><strong>Tutar:</strong> 100 TL</p>
            <p><strong>Açıklama:</strong> Domates tohumu desteği</p>
            <p><strong>Kullanım Tarihi:</strong> 14 Ağustos 2025, 11:15</p>
        </div>

        <div class="credentials">
            <h4>🔐 Hesap Bilgileriniz:</h4>
            <p><strong>Email:</strong> ahmet.yilmaz.generated@ziraai.com</p>
            <p><strong>Telefon:</strong> 905551234567</p>
            <p><strong>Geçici Şifre:</strong> TempPass123!</p>
            <p><em>⚠️ Güvenliğiniz için lütfen ilk girişinizde şifrenizi değiştirin.</em></p>
        </div>

        <a href="/dashboard" class="button">Hesabınıza Giriş Yapın</a>
        <a href="/profile/change-password" class="button" style="background: #ffc107; color: #000;">Şifreyi Değiştir</a>
    </div>

    <script>
        // Auto-login: JWT token'ı localStorage'a kaydet
        localStorage.setItem('ziraai_token', 'eyJ0eXAiOiJKV1QiOiJIUzI1NiIsImFsZyI6IkhTMjU2In0...');
        localStorage.setItem('ziraai_user', JSON.stringify({
            id: 456,
            fullName: 'Ahmet Yılmaz',
            email: 'ahmet.yilmaz.generated@ziraai.com',
            role: 'Farmer'
        }));
        
        // Redirect after 3 seconds
        setTimeout(() => {
            if(confirm('Dashboard\'a yönlendirileceksiniz. Devam etmek istiyor musunuz?')) {
                window.location.href = '/dashboard';
            }
        }, 3000);
    </script>
</body>
</html>
```

### B) API Endpoint Access (Programmatic)

For programmatic access or mobile apps:

```http
GET /api/v1/redeem/SPONSOR-2025-ABC123
Accept: application/json
```

### JSON Response
```json
{
  "success": true,
  "message": "Sponsorship code redeemed successfully",
  "data": {
    "redemption": {
      "code": "SPONSOR-2025-ABC123",
      "amount": 100.00,
      "description": "Domates tohumu desteği",
      "redemptionDate": "2025-08-14T11:15:30"
    },
    "user": {
      "id": 456,
      "fullName": "Ahmet Yılmaz",
      "email": "ahmet.yilmaz.generated@ziraai.com",
      "mobilePhones": "905551234567",
      "role": "Farmer",
      "status": true,
      "wasAccountCreated": true
    },
    "authentication": {
      "token": "eyJ0eXAiOiJKV1QiOiJIUzI1NiIsImFsZyI6IkhTMjU2In0...",
      "refreshToken": "eyJ0eXAiOiJKV1QiOiJIUzI1NiIsImFsZyI6IkhTMjU2In1...",
      "tokenExpiry": "2025-08-14T12:15:30",
      "refreshTokenExpiry": "2025-08-17T11:15:30"
    },
    "credentials": {
      "temporaryPassword": "TempPass123!",
      "passwordChangeRequired": true,
      "passwordChangeUrl": "/api/v1/auth/change-password"
    }
  }
}
```

## 5. Automatic Account Creation Process

When a farmer redeems a code and doesn't have an account:

### Account Generation Logic
```json
{
  "fullName": "Ahmet Yılmaz (SMS'ten)", // From recipient name + source
  "email": "ahmet.yilmaz.generated@ziraai.com", // Auto-generated from name
  "mobilePhones": "905551234567", // Normalized phone (Turkey format)
  "password": "TempPass123!", // Secure temporary password
  "role": "Farmer", // Default role
  "status": true, // Active account
  "emailConfirmed": false, // Requires email verification
  "phoneConfirmed": true, // Phone from SMS is considered verified
  "createdFrom": "SponsorshipRedemption", // Account source tracking
  "initialSponsorshipAmount": 100.00 // First redemption amount
}
```

### Password Generation Rules
- **Length**: 12 characters minimum
- **Complexity**: Uppercase, lowercase, numbers, special chars
- **Pattern**: `TempPass` + 3 digits + 1 special char
- **Expiry**: 7 days (forces password change)
- **Uniqueness**: Each account gets different password

### Email Generation Algorithm
1. **Normalize Name**: Remove Turkish chars, convert to lowercase
2. **Add Dots**: Replace spaces with dots
3. **Add Suffix**: `.generated@ziraai.com`
4. **Conflict Resolution**: Add numbers if exists (e.g., `ahmet.yilmaz.generated2@ziraai.com`)

## 6. Click Tracking and Analytics

### Automatic Tracking
Every link access triggers:

```json
{
  "linkClickCount": "increment by 1",
  "linkClickDate": "2025-08-14T11:15:22", // Latest click timestamp
  "lastClickIpAddress": "192.168.1.100", // User's IP
  "userAgent": "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0...", // Device info
  "referrer": "whatsapp://", // Traffic source
  "clickTimestamp": "2025-08-14T11:15:22.543Z" // Precise timing
}
```

### Analytics Data Points
- **First Click Time**: Time from send to first click
- **Multiple Clicks**: Indicates user interest/confusion
- **Device Types**: Mobile vs desktop usage
- **Geographic Data**: IP-based location analysis
- **Time Patterns**: Peak usage hours/days
- **Conversion Funnel**: Click → View → Redeem rates

## 7. Error Handling

### Invalid Code
```http
GET /redeem/INVALID-CODE-123
```

```json
{
  "success": false,
  "message": "Sponsorluk kodu bulunamadı veya geçersiz",
  "errorCode": "INVALID_CODE",
  "details": {
    "code": "INVALID-CODE-123",
    "timestamp": "2025-08-14T11:20:00"
  }
}
```

### Expired Code
```json
{
  "success": false,
  "message": "Bu sponsorluk kodunun süresi dolmuş (Son geçerlilik: 31 Aralık 2024)",
  "errorCode": "EXPIRED_CODE",
  "details": {
    "code": "SPONSOR-2024-OLD123",
    "expiryDate": "2024-12-31T23:59:59",
    "daysExpired": 45
  }
}
```

### Already Redeemed
```json
{
  "success": false,
  "message": "Bu sponsorluk kodu daha önce kullanılmış (Kullanım tarihi: 10 Ağustos 2025)",
  "errorCode": "ALREADY_REDEEMED", 
  "details": {
    "code": "SPONSOR-2025-USED456",
    "redemptionDate": "2025-08-10T14:30:00",
    "redeemedBy": "Mehmet Demir",
    "redeemedAmount": 150.00
  }
}
```

### Account Creation Failure
```json
{
  "success": false,
  "message": "Hesap oluşturulurken bir hata oluştu. Lütfen tekrar deneyin.",
  "errorCode": "ACCOUNT_CREATION_FAILED",
  "details": {
    "reason": "Email already exists",
    "suggestedEmail": "ahmet.yilmaz.generated2@ziraai.com",
    "supportContact": "support@ziraai.com"
  }
}
```

### Rate Limiting
```json
{
  "success": false,
  "message": "Çok fazla deneme yapıldı. Lütfen 5 dakika sonra tekrar deneyin.",
  "errorCode": "RATE_LIMIT_EXCEEDED",
  "details": {
    "retryAfter": 300,
    "maxAttempts": 10,
    "windowMinutes": 60
  }
}
```

## 8. Security Considerations

### Authentication & Authorization
- **Sponsor Endpoints**: Require valid JWT token with Sponsor role
- **Public Redemption**: No authentication (security through code complexity)
- **Admin Endpoints**: Require Admin role for statistics access
- **Rate Limiting**: Prevent brute force attacks on redemption

### Data Protection
- **Code Complexity**: 16+ character unique codes
- **Temporary Passwords**: Expire after 7 days
- **Personal Data**: Minimal data collection, GDPR compliant
- **Audit Trail**: Complete tracking for compliance

### Validation Rules
- **Phone Numbers**: Turkish mobile format validation
- **Expiry Dates**: Must be future dates, max 1 year
- **Amount Limits**: Min: 1 TL, Max: 10,000 TL per code
- **Name Validation**: Turkish character support, length limits

## 9. Integration Examples

### Frontend Integration
```javascript
// Send redemption links
async function sendSponsorshipLinks(codes, sendVia = 'WhatsApp') {
    const response = await fetch('/api/v1/sponsorship/send-link', {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${getToken()}`,
            'Content-Type': 'application/json',
            'x-api-version': '1.0'
        },
        body: JSON.stringify({
            codes: codes,
            sendVia: sendVia,
            customMessage: 'ZiraAI tarımsal destek kodunuz hazır!'
        })
    });
    
    return await response.json();
}

// Check link statistics
async function getLinkStatistics(sponsorUserId) {
    const response = await fetch(`/api/v1/sponsorship/link-statistics?sponsorUserId=${sponsorUserId}`, {
        headers: {
            'Authorization': `Bearer ${getToken()}`,
            'x-api-version': '1.0'
        }
    });
    
    return await response.json();
}
```

### Mobile App Integration
```dart
// Flutter/Dart example
Future<Map<String, dynamic>> redeemCode(String code) async {
  final response = await http.get(
    Uri.parse('$baseUrl/api/v1/redeem/$code'),
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
    },
  );
  
  if (response.statusCode == 200) {
    final data = json.decode(response.body);
    
    // Store authentication tokens
    await storage.write(key: 'token', value: data['data']['authentication']['token']);
    await storage.write(key: 'refreshToken', value: data['data']['authentication']['refreshToken']);
    
    return data;
  } else {
    throw Exception('Redemption failed');
  }
}
```

### SMS Provider Integration
```csharp
// C# SMS service integration
public async Task<bool> SendSmsAsync(string phone, string message, string redemptionLink)
{
    var fullMessage = $"{message}\n\nKodunuzu kullanmak için: {redemptionLink}\n\nZiraAI Destek";
    
    using var httpClient = new HttpClient();
    var smsRequest = new
    {
        to = NormalizePhoneNumber(phone),
        text = fullMessage,
        from = "ZiraAI"
    };
    
    var response = await httpClient.PostAsJsonAsync("https://api.sms-provider.com/send", smsRequest);
    return response.IsSuccessStatusCode;
}

private string NormalizePhoneNumber(string phone)
{
    // Turkish mobile number normalization
    phone = phone.TrimStart('+').TrimStart('9').TrimStart('0');
    return $"+90{phone}";
}
```

## 10. Mobile App Integration

### Deep Link Setup

#### iOS Configuration (Info.plist)
```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLName</key>
        <string>com.ziraai.app</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>ziraai</string>
        </array>
    </dict>
</array>

<!-- Universal Links (Optional) -->
<key>com.apple.developer.associated-domains</key>
<array>
    <string>applinks:ziraai.com</string>
    <string>applinks:*.ziraai.com</string>
</array>
```

#### Android Configuration (AndroidManifest.xml)
```xml
<activity 
    android:name=".activities.RedemptionActivity"
    android:exported="true">
    <intent-filter>
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        <data android:scheme="ziraai" android:host="redeem" />
    </intent-filter>
    
    <!-- App Links (Optional) -->
    <intent-filter android:autoVerify="true">
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        <data android:scheme="https" android:host="ziraai.com" />
    </intent-filter>
</activity>
```

### Mobile App Code Examples

#### Flutter Implementation
```dart
// main.dart - Deep link handling
import 'package:uni_links/uni_links.dart';

class MyApp extends StatefulWidget {
  @override
  _MyAppState createState() => _MyAppState();
}

class _MyAppState extends State<MyApp> {
  StreamSubscription _linkSubscription;

  @override
  void initState() {
    super.initState();
    _initDeepLinks();
  }

  void _initDeepLinks() {
    // Handle app launch via deep link
    getInitialLink().then((String initialLink) {
      if (initialLink != null) {
        _handleDeepLink(initialLink);
      }
    });

    // Handle deep links while app is running
    _linkSubscription = linkStream.listen((String link) {
      _handleDeepLink(link);
    });
  }

  void _handleDeepLink(String link) {
    final uri = Uri.parse(link);
    
    if (uri.scheme == 'ziraai' && uri.host == 'redeem') {
      final code = uri.queryParameters['code'];
      final token = uri.queryParameters['token'];
      
      if (code != null && token != null) {
        // Store authentication token
        AuthService.instance.storeToken(token);
        
        // Navigate to redemption success page
        Navigator.pushNamedAndRemoveUntil(
          context,
          '/redemption-success',
          (route) => false,
          arguments: {
            'code': code,
            'token': token,
            'autoRedeemed': true
          }
        );
      }
    }
  }

  @override
  void dispose() {
    _linkSubscription?.cancel();
    super.dispose();
  }
}

// redemption_service.dart
class RedemptionService {
  static Future<void> processAutoRedemption(String code, String token) async {
    try {
      // Validate token with backend
      final response = await ApiService.validateToken(token);
      
      if (response.success) {
        // Update user session
        final user = User.fromJson(response.data['user']);
        UserService.instance.setCurrentUser(user);
        
        // Show success message
        NotificationService.showSuccess(
          'Aboneliğiniz başarıyla aktive edildi!'
        );
        
        // Log redemption analytics
        AnalyticsService.logEvent('sponsorship_redeemed', {
          'code': code,
          'source': 'deep_link',
          'timestamp': DateTime.now().toIso8601String()
        });
      }
    } catch (e) {
      // Handle errors gracefully
      NotificationService.showError(
        'Aktivasyon sırasında bir hata oluştu. Lütfen tekrar deneyin.'
      );
    }
  }
}
```

#### React Native Implementation
```javascript
// DeepLinkHandler.js
import { Linking } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';

class DeepLinkHandler {
  static init() {
    // Handle app launch via deep link
    Linking.getInitialURL().then((url) => {
      if (url) {
        this.handleDeepLink(url);
      }
    });

    // Handle deep links while app is running
    Linking.addEventListener('url', (event) => {
      this.handleDeepLink(event.url);
    });
  }

  static handleDeepLink(url) {
    const parsedUrl = new URL(url);
    
    if (parsedUrl.protocol === 'ziraai:' && parsedUrl.hostname === 'redeem') {
      const code = parsedUrl.searchParams.get('code');
      const token = parsedUrl.searchParams.get('token');
      
      if (code && token) {
        this.processRedemption(code, token);
      }
    }
  }

  static async processRedemption(code, token) {
    try {
      // Store token for authentication
      await AsyncStorage.setItem('authToken', token);
      
      // Navigate to success screen
      NavigationService.navigate('RedemptionSuccess', {
        code,
        token,
        autoRedeemed: true
      });
      
      // Log analytics
      Analytics.track('sponsorship_redeemed', {
        code,
        source: 'deep_link',
        timestamp: new Date().toISOString()
      });
    } catch (error) {
      console.error('Redemption processing failed:', error);
      // Show error to user
      Alert.alert(
        'Hata',
        'Aktivasyon sırasında bir hata oluştu. Lütfen tekrar deneyin.'
      );
    }
  }
}

export default DeepLinkHandler;
```

### Testing Mobile Integration

#### 1. Deep Link Testing
```bash
# iOS Simulator
xcrun simctl openurl booted "ziraai://redeem?code=TEST-123&token=eyJ0eXAi..."

# Android Emulator/Device
adb shell am start \
  -W -a android.intent.action.VIEW \
  -d "ziraai://redeem?code=TEST-123&token=eyJ0eXAi..." \
  com.ziraai.app
```

#### 2. Universal Links Testing (iOS)
```bash
# Test with associated domain
xcrun simctl openurl booted "https://ziraai.com/redeem/TEST-123"
```

#### 3. App Links Testing (Android)
```bash
# Test with verified domain
adb shell am start \
  -W -a android.intent.action.VIEW \
  -d "https://ziraai.com/redeem/TEST-123" \
  com.ziraai.app
```

## 11. Testing Guide

### Mobile Testing Workflow

#### Phase 1: Mobile Browser Simulation
```javascript
// Chrome DevTools - Mobile simulation
// 1. Open DevTools (F12)
// 2. Toggle device toolbar (Ctrl+Shift+M)
// 3. Select mobile device (iPhone 12, Galaxy S10, etc.)
// 4. Navigate to redemption link
// 5. Observe deep link attempt and fallback behavior

// Test User-Agent detection
const mobileUserAgents = [
  'Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X)',
  'Mozilla/5.0 (Linux; Android 10; SM-G973F)',
  'Mozilla/5.0 (Linux; Android 11; Pixel 4)'
];

mobileUserAgents.forEach(ua => {
  console.log(`Testing with: ${ua}`);
  // Change user agent in DevTools Network Conditions
});
```

#### Phase 2: Deep Link Testing
```powershell
# PowerShell script for deep link testing
function Test-DeepLink {
    param(
        [string]$Code = "TEST-MOBILE-123",
        [string]$Token = "eyJ0eXAiOiJKV1Q..."
    )
    
    $deepLink = "ziraai://redeem?code=$Code&token=$Token"
    
    Write-Host "Testing deep link: $deepLink" -ForegroundColor Cyan
    
    # On Windows - register custom protocol handler for testing
    $regPath = "HKEY_CLASSES_ROOT\ziraai"
    
    # Test if protocol is registered
    if (Test-Path "Registry::$regPath") {
        Write-Host "✅ Custom protocol registered" -ForegroundColor Green
        
        # Open deep link (will open registered handler or default browser)
        Start-Process $deepLink
    } else {
        Write-Host "⚠️  Custom protocol not registered" -ForegroundColor Yellow
        Write-Host "💡 Install ZiraAI app or register protocol for testing" -ForegroundColor Gray
    }
}

# Usage
Test-DeepLink -Code "SPONSOR-2025-ABC123"
```

### Postman Collection Tests
The updated Postman collection includes:

1. **📱 Send Sponsorship Links (SMS/WhatsApp)**
   - Tests bulk link sending
   - Validates response format
   - Checks delivery status

2. **📊 Get Link Distribution Statistics**
   - Tests analytics endpoint
   - Validates statistics calculations
   - Checks filtering parameters

3. **🔗 Test Public Redemption Link (No Auth)**
   - Tests HTML response
   - Validates auto-login functionality
   - Checks account creation

4. **🔗 Test Versioned Public Redemption**
   - Tests JSON API response
   - Validates token generation
   - Checks user data structure

### Manual Testing Steps
1. Create sponsor account and generate codes
2. Send links via API or Postman
3. Click links from different devices/browsers
4. Verify account creation and auto-login
5. Check analytics and statistics
6. Test error scenarios (invalid/expired codes)

### Automated Testing
```bash
# PowerShell testing scripts
./test_https_api.ps1     # Basic API connectivity
./test_link_system.ps1   # Complete system test
```

This comprehensive guide covers all aspects of the Sponsorship Link Distribution System, from basic usage to advanced integration scenarios.