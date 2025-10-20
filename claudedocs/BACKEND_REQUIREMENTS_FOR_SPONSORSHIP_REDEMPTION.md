# Backend Requirements for Sponsorship Redemption System

**Date**: 2025-10-14
**Mobile App Version**: Ready and Deployed
**Purpose**: Backend changes required to fully support mobile app's sponsorship redemption features

---

## Executive Summary

Mobile app has **three working redemption methods**:
1. ✅ Automatic SMS detection (works with current SMS format)
2. ✅ Manual code entry (works independently)
3. ⚠️ Deep link from SMS (**requires backend changes**)

**Current Status**: Methods 1 and 2 work perfectly. Method 3 requires backend to add deep links to SMS messages.

**Required Backend Changes**: 3 critical changes, estimated 2-4 hours of work

---

## 🔴 CRITICAL REQUIREMENT #1: Add Deep Links to SMS Messages

### Current SMS Format (What backend sends now)
```
🎁 Chimera Tarım A.Ş. size Medium paketi hediye etti!
Sponsorluk Kodunuz: AGRI-2025-3852DE2A
Uygulamayı indirin: https://play.google.com/store/apps/details?id=com.ziraai.app
```

### Required SMS Format (What backend must send)
```
🎁 Chimera Tarım A.Ş. size Medium paketi hediye etti!
Sponsorluk Kodunuz: AGRI-2025-3852DE2A

Hemen kullanmak için tıklayın:
https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-3852DE2A

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app
```

### Implementation

#### C# Code Example
```csharp
// appsettings.json or environment variable
public class SponsorshipConfiguration
{
    public string DeepLinkBaseUrl { get; set; } // "https://ziraai-api-sit.up.railway.app" for staging
    public string PlayStoreUrl { get; set; }     // "https://play.google.com/store/apps/details?id=com.ziraai.app"
}

// SMS Service
public class SponsorshipSmsService
{
    private readonly SponsorshipConfiguration _config;

    public string BuildSmsMessage(string sponsorCompany, string packageName, string code)
    {
        var deepLink = $"{_config.DeepLinkBaseUrl}/redeem/{code}";

        return $@"🎁 {sponsorCompany} size {packageName} paketi hediye etti!
Sponsorluk Kodunuz: {code}

Hemen kullanmak için tıklayın:
{deepLink}

Veya uygulamayı indirin:
{_config.PlayStoreUrl}";
    }
}
```

#### Configuration
**appsettings.Staging.json**:
```json
{
  "Sponsorship": {
    "DeepLinkBaseUrl": "https://ziraai-api-sit.up.railway.app",
    "PlayStoreUrl": "https://play.google.com/store/apps/details?id=com.ziraai.app"
  }
}
```

**appsettings.Production.json**:
```json
{
  "Sponsorship": {
    "DeepLinkBaseUrl": "https://ziraai.com",
    "PlayStoreUrl": "https://play.google.com/store/apps/details?id=com.ziraai.app"
  }
}
```

### URL Format Rules
✅ **Correct formats**:
- `https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-ABC123` (Staging)
- `https://ziraai.com/redeem/SPONSOR-2025-XYZ789` (Production)

❌ **Incorrect formats**:
- `https://ziraai.com/redeem?code=ABC123` (query parameter - will not work)
- `https://ziraai.com/sponsorship/redeem/ABC123` (wrong path - will not work)
- `http://ziraai.com/redeem/ABC123` (HTTP not HTTPS - will not work)

**URL Pattern**: `{baseUrl}/redeem/{code}` - Nothing else!

### Testing
```bash
# After SMS sent, test deep link manually
curl -I https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-TEST123

# Should return 200 or 302 redirect
# Mobile app will handle the /redeem/{code} path automatically
```

### Expected Result
When user taps the link in SMS:
1. Android opens ZiraAI mobile app automatically
2. App extracts code from URL (`AGRI-2025-ABC123`)
3. Redemption screen opens with code pre-filled
4. User taps one button to complete redemption

---

## 🔴 CRITICAL REQUIREMENT #2: Serve Digital Asset Links File

### Purpose
Enables Android Universal Links - when user taps `https://ziraai.com/redeem/CODE`, Android opens the app instead of browser.

### Required File
**Location**: `/.well-known/assetlinks.json`

**Full URL**:
- Staging: `https://ziraai-api-sit.up.railway.app/.well-known/assetlinks.json`
- Production: `https://ziraai.com/.well-known/assetlinks.json`

### File Content
```json
[{
  "relation": ["delegate_permission/common.handle_all_urls"],
  "target": {
    "namespace": "android_app",
    "package_name": "com.ziraai.app.staging",
    "sha256_cert_fingerprints": [
      "REPLACE_WITH_STAGING_SHA256_FINGERPRINT"
    ]
  }
},
{
  "relation": ["delegate_permission/common.handle_all_urls"],
  "target": {
    "namespace": "android_app",
    "package_name": "com.ziraai.app",
    "sha256_cert_fingerprints": [
      "REPLACE_WITH_PRODUCTION_SHA256_FINGERPRINT"
    ]
  }
}]
```

### Getting SHA256 Fingerprints

**Mobile team will provide these values**. They need to run:

```bash
# For staging/debug build
keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android | grep SHA256

# For production/release build
keytool -list -v -keystore path/to/release.keystore -alias your_key_alias
```

**Example output**:
```
SHA256: 14:6D:E9:83:C5:73:06:50:D8:EE:B9:95:2F:34:FC:64:16:A0:83:42:E6:1D:BE:A8:8A:04:96:B2:3F:CF:44:E5
```

### Implementation

#### ASP.NET Core Setup
```csharp
// Program.cs or Startup.cs

// 1. Create directory structure
// ProjectRoot/
//   .well-known/
//     assetlinks.json

// 2. Configure static files middleware
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), ".well-known")),
    RequestPath = "/.well-known",
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/json"
});

// IMPORTANT: Add this BEFORE app.UseRouting() and app.UseEndpoints()
```

#### File System Setup
```
Backend/
├── .well-known/
│   └── assetlinks.json    ← Create this file
├── Controllers/
├── Program.cs
└── appsettings.json
```

### Testing
```bash
# Test if file is accessible
curl https://ziraai-api-sit.up.railway.app/.well-known/assetlinks.json

# Expected: JSON response with package names and fingerprints
# Should NOT return 404 or redirect
```

### Common Mistakes to Avoid
❌ Don't redirect `/.well-known/assetlinks.json` to HTTPS (it must be directly accessible)
❌ Don't require authentication for this endpoint
❌ Don't modify the JSON format (Android is strict about it)
❌ Don't forget the leading dot in `.well-known`
✅ File must be publicly accessible without any authentication
✅ Content-Type must be `application/json`
✅ Use exact package names provided by mobile team

---

## 🔴 CRITICAL REQUIREMENT #3: Standardize Sponsorship Code Format

### Current Format (Unknown)
Backend currently generates codes in unknown format. Mobile app supports multiple formats but backend should standardize.

### Required Format
```
Pattern: PREFIX-YEAR-RANDOM
Regex: ^(AGRI|SPONSOR)-[A-Z0-9\-]+$

Examples:
✅ AGRI-2025-52834B45      (Agricultural sponsor)
✅ SPONSOR-2025-A1B2C3D4   (General sponsor)
✅ AGRI-2025-TEST-CODE-1   (Multi-hyphen supported)
✅ SPONSOR-K5ZYZX          (Short codes supported)

❌ agri-2025-abc123        (lowercase not supported)
❌ AGRI_2025_ABC123        (underscores not supported)
❌ 2025-AGRI-ABC123        (prefix must be first)
```

### Implementation

#### Code Generator
```csharp
public class SponsorshipCodeGenerator
{
    public enum CodePrefix
    {
        AGRI,      // Agricultural company sponsor
        SPONSOR    // General sponsor
    }

    public static string GenerateCode(CodePrefix prefix = CodePrefix.AGRI)
    {
        var year = DateTime.UtcNow.Year;
        var random = GenerateRandomString(8); // 8 uppercase alphanumeric characters

        return $"{prefix}-{year}-{random}";
        // Output: AGRI-2025-52834B45
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
```

#### Code Validator
```csharp
public class SponsorshipCodeValidator
{
    private static readonly Regex CodeRegex = new Regex(
        @"^(AGRI|SPONSOR)-[A-Z0-9\-]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public static bool IsValid(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        // Must match pattern
        if (!CodeRegex.IsMatch(code))
            return false;

        // Must have minimum length (PREFIX-XXXX = 10 chars minimum)
        if (code.Length < 10)
            return false;

        return true;
    }

    public static ValidationResult Validate(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return ValidationResult.Error("Kod boş olamaz");

        if (code.Length < 10)
            return ValidationResult.Error("Kod çok kısa (minimum 10 karakter)");

        if (!CodeRegex.IsMatch(code))
            return ValidationResult.Error("Geçersiz kod formatı. Format: AGRI-XXXX-XXXXXXXX veya SPONSOR-XXXX-XXXXXXXX");

        return ValidationResult.Success();
    }
}
```

#### Redemption Endpoint with Validation
```csharp
[HttpPost("/api/v1/sponsorships/redeem")]
public async Task<IActionResult> RedeemCode([FromBody] RedeemCodeRequest request)
{
    // Validate format
    var validation = SponsorshipCodeValidator.Validate(request.Code);
    if (!validation.IsSuccess)
    {
        return BadRequest(new {
            success = false,
            message = validation.ErrorMessage
        });
    }

    // Check if code exists
    var sponsorshipCode = await _db.SponsorshipCodes
        .FirstOrDefaultAsync(x => x.Code.ToUpper() == request.Code.ToUpper());

    if (sponsorshipCode == null)
    {
        return NotFound(new {
            success = false,
            message = "Kod bulunamadı"
        });
    }

    // Check expiration (7 days)
    if (sponsorshipCode.CreatedAt.AddDays(7) < DateTime.UtcNow)
    {
        return BadRequest(new {
            success = false,
            message = "Kod süresi dolmuş (7 gün)"
        });
    }

    // Check if already redeemed
    if (sponsorshipCode.RedeemedAt.HasValue)
    {
        return BadRequest(new {
            success = false,
            message = "Kod daha önce kullanılmış"
        });
    }

    // Redeem code...
    sponsorshipCode.RedeemedAt = DateTime.UtcNow;
    sponsorshipCode.RedeemedBy = request.UserId;
    await _db.SaveChangesAsync();

    return Ok(new {
        success = true,
        message = "Sponsorluk başarıyla aktif edildi",
        data = new {
            packageName = sponsorshipCode.PackageName,
            expiresAt = sponsorshipCode.PackageExpiresAt
        }
    });
}
```

### Database Schema
```csharp
public class SponsorshipCode
{
    public int Id { get; set; }
    public string Code { get; set; }                    // AGRI-2025-52834B45
    public string SponsorCompanyId { get; set; }
    public string PackageTierId { get; set; }           // S, M, L, XL
    public string PhoneNumber { get; set; }             // Farmer's phone

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }             // CreatedAt + 7 days

    public DateTime? RedeemedAt { get; set; }
    public string RedeemedBy { get; set; }              // User ID who redeemed

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsRedeemed => RedeemedAt.HasValue;
}

// Index for fast lookup
modelBuilder.Entity<SponsorshipCode>()
    .HasIndex(x => x.Code)
    .IsUnique();
```

---

## 🟡 OPTIONAL REQUIREMENT #4: Code Expiration Policy

### Current Behavior
Mobile app removes codes from local storage after 7 days. Backend should enforce the same.

### Implementation
```csharp
// When creating code
var code = new SponsorshipCode
{
    Code = SponsorshipCodeGenerator.GenerateCode(),
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddDays(7),  // 7 days from creation
    // ... other fields
};

// When redeeming code
if (code.IsExpired)
{
    return BadRequest(new {
        success = false,
        message = "Sponsorluk kodu süresi dolmuş. Yeni kod almak için sponsor ile iletişime geçin."
    });
}
```

### Configuration
```json
{
  "Sponsorship": {
    "CodeExpirationDays": 7
  }
}
```

---

## 🟢 NICE TO HAVE: Analytics and Tracking

### Track Deep Link Opens (Optional but Recommended)

#### Why?
To measure effectiveness of SMS campaigns and understand which redemption method users prefer.

#### Implementation
```csharp
// Add tracking table
public class SponsorshipCodeTracking
{
    public int Id { get; set; }
    public string Code { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? DeepLinkOpenedAt { get; set; }
    public DateTime? RedeemedAt { get; set; }
    public string RedemptionMethod { get; set; }  // "SMS_AUTO", "DEEP_LINK", "MANUAL"
}

// Track deep link opens
[HttpGet("/redeem/{code}")]
public async Task<IActionResult> HandleDeepLink(string code)
{
    // Track that user opened deep link
    var tracking = await _db.SponsorshipTracking
        .FirstOrDefaultAsync(x => x.Code == code);

    if (tracking != null && !tracking.DeepLinkOpenedAt.HasValue)
    {
        tracking.DeepLinkOpenedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // Redirect to app (Android will intercept this)
    return Redirect($"ziraai://redeem/{code}");
}

// Update redemption method when code is redeemed
[HttpPost("/api/v1/sponsorships/redeem")]
public async Task<IActionResult> RedeemCode([FromBody] RedeemCodeRequest request)
{
    // ... existing redemption logic ...

    // Track redemption method
    var tracking = await _db.SponsorshipTracking
        .FirstOrDefaultAsync(x => x.Code == request.Code);

    if (tracking != null)
    {
        tracking.RedeemedAt = DateTime.UtcNow;
        tracking.RedemptionMethod = request.RedemptionMethod; // Mobile app sends this
        await _db.SaveChangesAsync();
    }

    return Ok(/* ... */);
}
```

#### Analytics Endpoint
```csharp
[HttpGet("/api/v1/sponsorships/analytics")]
public async Task<SponsorshipAnalytics> GetAnalytics([FromQuery] string sponsorId)
{
    var stats = await _db.SponsorshipTracking
        .Where(x => x.SponsorId == sponsorId)
        .GroupBy(x => 1)
        .Select(g => new SponsorshipAnalytics
        {
            TotalSent = g.Count(),
            TotalOpened = g.Count(x => x.DeepLinkOpenedAt.HasValue),
            TotalRedeemed = g.Count(x => x.RedeemedAt.HasValue),

            OpenRate = (double)g.Count(x => x.DeepLinkOpenedAt.HasValue) / g.Count() * 100,
            RedemptionRate = (double)g.Count(x => x.RedeemedAt.HasValue) / g.Count() * 100,

            ByMethod = g.GroupBy(x => x.RedemptionMethod)
                .ToDictionary(
                    m => m.Key,
                    m => m.Count()
                )
        })
        .FirstOrDefaultAsync();

    return stats ?? new SponsorshipAnalytics();
}
```

---

## Testing Requirements

### Test Case 1: SMS Deep Link Flow
**Steps**:
1. Backend generates code: `AGRI-2025-TEST123`
2. Backend sends SMS with deep link: `https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-TEST123`
3. User receives SMS on mobile phone
4. User taps link
5. App opens automatically (not browser)
6. Redemption screen opens with code pre-filled
7. User taps redeem button
8. Backend accepts redemption
9. User sees success message

**Expected Logs** (Mobile App):
```
📱 DeepLink: Incoming link received: https://ziraai-api-sit.up.railway.app/redeem/AGRI-2025-TEST123
✅ DeepLink: Extracted sponsorship code: AGRI-2025-TEST123
🎯 Navigating to redemption screen with sponsorship code: AGRI-2025-TEST123
[SponsorshipRedeem] Code auto-filled: AGRI-2025-TEST123
```

### Test Case 2: Manual Entry Flow
**Steps**:
1. User opens app
2. User taps "Sponsorluk Kodunu Kullan" button
3. Redemption screen opens with empty field
4. User manually types: `SPONSOR-2025-MANUAL99`
5. User taps redeem button
6. Backend validates format (should pass)
7. Backend accepts redemption
8. User sees success message

### Test Case 3: Invalid Code Formats
Test that backend rejects invalid codes:

```
❌ "agri-2025-abc123"       → "Geçersiz format"
❌ "AGRI_2025_ABC123"       → "Geçersiz format"
❌ "2025-AGRI-ABC"          → "Geçersiz format"
❌ "AGRI-20"                → "Kod çok kısa"
❌ "EXPIRED-2024-OLD123"    → "Kod bulunamadı"
✅ "AGRI-2025-52834B45"     → Should work
✅ "SPONSOR-2025-TEST"      → Should work
```

### Test Case 4: Expiration
**Steps**:
1. Backend creates code with `CreatedAt = 8 days ago`
2. User tries to redeem
3. Backend returns: "Kod süresi dolmuş (7 gün)"

### Test Case 5: Already Redeemed
**Steps**:
1. User redeems code successfully
2. Same user tries to redeem same code again
3. Backend returns: "Kod daha önce kullanılmış"

---

## Environment Configuration

### Staging Environment
```json
{
  "Sponsorship": {
    "DeepLinkBaseUrl": "https://ziraai-api-sit.up.railway.app",
    "PlayStoreUrl": "https://play.google.com/store/apps/details?id=com.ziraai.app",
    "CodeExpirationDays": 7,
    "CodePrefix": "AGRI"
  }
}
```

### Production Environment
```json
{
  "Sponsorship": {
    "DeepLinkBaseUrl": "https://ziraai.com",
    "PlayStoreUrl": "https://play.google.com/store/apps/details?id=com.ziraai.app",
    "CodeExpirationDays": 7,
    "CodePrefix": "AGRI"
  }
}
```

---

## Deployment Checklist

### Before Deployment
- [ ] Update SMS template to include deep link
- [ ] Create `.well-known/assetlinks.json` file
- [ ] Get SHA256 fingerprints from mobile team
- [ ] Add SHA256 fingerprints to assetlinks.json
- [ ] Configure static files middleware to serve `.well-known` directory
- [ ] Implement code format validation
- [ ] Implement 7-day expiration
- [ ] Test SMS sending with new format
- [ ] Test deep link accessibility (`curl /.well-known/assetlinks.json`)

### Testing in Staging
- [ ] Send test SMS to real phone
- [ ] Verify SMS contains deep link
- [ ] Tap deep link and verify app opens
- [ ] Verify code is auto-filled
- [ ] Test redemption flow end-to-end
- [ ] Test manual entry flow
- [ ] Test invalid code formats
- [ ] Test expired codes
- [ ] Test already redeemed codes

### Production Deployment
- [ ] Deploy backend changes to production
- [ ] Verify `.well-known/assetlinks.json` is accessible at `https://ziraai.com/.well-known/assetlinks.json`
- [ ] Send test SMS to production environment
- [ ] Monitor error logs for first 24 hours
- [ ] Check redemption success rate

---

## API Endpoints Reference

### Existing Endpoints (Mobile App Uses These)
```
POST /api/v1/sponsorships/redeem
Body: { "code": "AGRI-2025-ABC123", "userId": "user_id" }
Response: { "success": true, "message": "...", "data": {...} }
```

### New Endpoint Needed (for Deep Link Tracking - Optional)
```
GET /redeem/{code}
Example: GET /redeem/AGRI-2025-ABC123
Action: Track open, redirect to app
Response: 302 Redirect to ziraai://redeem/AGRI-2025-ABC123
```

---

## Support and Questions

### Mobile Team Contact
If backend team has questions:
- Deep link format questions → Mobile team
- SHA256 fingerprints → Mobile team will provide
- Testing on real devices → Mobile team can help

### Mobile App Capabilities
✅ Handles: `https://ziraai.com/redeem/CODE`
✅ Handles: `https://ziraai-api-sit.up.railway.app/redeem/CODE`
✅ Handles: `ziraai://redeem/CODE`
❌ Cannot handle: Query parameters (`?code=ABC`)
❌ Cannot handle: Wrong paths (`/sponsorship/redeem/CODE`)
❌ Cannot handle: HTTP (must be HTTPS)

---

## Summary

### Must Implement (2-4 hours)
1. **Add deep links to SMS messages** (30 minutes)
   - Update SMS template
   - Add configuration for base URL

2. **Serve assetlinks.json file** (1 hour)
   - Create `.well-known` directory
   - Configure static files middleware
   - Get SHA256 from mobile team
   - Create assetlinks.json

3. **Standardize code format** (1-2 hours)
   - Implement code generator
   - Implement code validator
   - Update redemption endpoint
   - Add database indexes

### Nice to Have (2-4 hours)
4. **Code expiration policy** (30 minutes)
5. **Analytics tracking** (2-3 hours)

### Total Estimated Time
- Critical requirements: **2-4 hours**
- With optional features: **4-8 hours**

---

## Questions?

Contact mobile development team with:
- Code format questions
- Deep link testing issues
- SHA256 fingerprint requests
- Integration testing coordination
