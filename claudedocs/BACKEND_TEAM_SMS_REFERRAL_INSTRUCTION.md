# Backend Team - SMS Referral Implementation Guide

**Project**: ZiraAI Mobile - Deferred Deep Linking
**Date**: 2025-10-06
**Target Audience**: Backend Development Team
**Implementation Time**: 30 minutes

---

## 📋 Quick Summary

### What You Need to Do

**ONE SIMPLE CHANGE**: Update SMS message format to include ZIRA referral code explicitly.

**Why**: Mobile app will read SMS on first launch to auto-fill referral code (deferred deep linking).

**Impact**:
- ✅ No API changes
- ✅ No database changes
- ✅ No business logic changes
- ✅ Only SMS message text changes

---

## 🎯 The Task

### Current SMS Format (Before)

```csharp
// BEFORE - Old format
var message = $@"ZiraAI'ya davet edildiniz!
Tıklayın: {deepLink}";
```

**Example Output**:
```
ZiraAI'ya davet edildiniz!
Tıklayın: https://ziraai-api-sit.up.railway.app/ref/ZIRA-K5ZYZX
```

### ✅ New SMS Format (After)

```csharp
// AFTER - New format (REQUIRED)
var message = $@"🌱 ZiraAI'ya davet edildiniz!

Referans Kodunuz: {referralCode}

Uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app

Uygulama açıldığında kod otomatik gelecek!";
```

**Example Output**:
```
🌱 ZiraAI'ya davet edildiniz!

Referans Kodunuz: ZIRA-K5ZYZX

Uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app

Uygulama açıldığında kod otomatik gelecek!
```

### 🔑 Critical Requirements

**MUST HAVE**:
1. ✅ Referral code in format: `ZIRA-XXXXX` (with hyphen!)
2. ✅ Code visible as plain text in message body
3. ✅ Play Store link included

**Example Valid Codes**:
- ✅ `ZIRA-K5ZYZX`
- ✅ `ZIRA-ABC123`
- ✅ `ZIRA-XYZ789`

**Example Invalid Codes** (Will NOT work):
- ❌ `ZIRATEST123` (no hyphen)
- ❌ `zira-k5zyzx` (lowercase)
- ❌ `K5ZYZX` (no prefix)

---

## 💻 Implementation Steps

### Step 1: Locate SMS Service Code

**Find the file where SMS messages are generated**:

Common locations:
- `Services/SmsService.cs`
- `Services/ReferralService.cs`
- `Controllers/ReferralController.cs`
- `Business/Messaging/SmsProvider.cs`

**Look for code like this**:
```csharp
// Find where SMS message is created
var message = "ZiraAI'ya davet edildiniz...";
await _smsService.SendAsync(phoneNumber, message);
```

### Step 2: Update Message Format

#### Option A: Direct String (Simple)

```csharp
// In ReferralService.cs or SmsService.cs

public async Task<DeliveryStatus> SendReferralSms(
    string phoneNumber,
    string referralCode)
{
    // ✅ NEW FORMAT
    var message = $@"🌱 ZiraAI'ya davet edildiniz!

Referans Kodunuz: {referralCode}

Uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app

Uygulama açıldığında kod otomatik gelecek!";

    // Send SMS (existing code - no changes)
    var result = await _smsProvider.SendAsync(phoneNumber, message);

    return new DeliveryStatus
    {
        PhoneNumber = phoneNumber,
        Method = "SMS",
        Status = result.Success ? "Sent" : "Failed"
    };
}
```

#### Option B: Message Template (Enterprise)

```csharp
// appsettings.json or database
{
  "SmsTemplates": {
    "ReferralInvite": "🌱 ZiraAI'ya davet edildiniz!\n\nReferans Kodunuz: {referralCode}\n\nUygulamayı indirin:\nhttps://play.google.com/store/apps/details?id=com.ziraai.app\n\nUygulama açıldığında kod otomatik gelecek!"
  }
}

// In service
var template = _configuration["SmsTemplates:ReferralInvite"];
var message = template.Replace("{referralCode}", referralCode);
```

#### Option C: WhatsApp Template (If using WhatsApp)

```csharp
// For WhatsApp Business API

public async Task<DeliveryStatus> SendReferralWhatsApp(
    string phoneNumber,
    string referralCode)
{
    var message = $@"🌱 *ZiraAI'ya davet edildiniz!*

*Referans Kodunuz:* {referralCode}

Uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app

_Uygulama açıldığında kod otomatik gelecek!_";

    var result = await _whatsAppService.SendAsync(phoneNumber, message);

    return new DeliveryStatus
    {
        PhoneNumber = phoneNumber,
        Method = "WhatsApp",
        Status = result.Success ? "Sent" : "Failed"
    };
}
```

### Step 3: Validate Referral Code Format

**Add validation to ensure codes are in correct format**:

```csharp
// In ReferralService.cs or validation layer

public string GenerateReferralCode(string userId)
{
    // Your existing code generation logic
    var code = GenerateUniqueCode();  // e.g., "K5ZYZX"

    // ✅ ENSURE ZIRA- prefix
    var referralCode = $"ZIRA-{code}";

    // Validate format
    if (!IsValidReferralCodeFormat(referralCode))
    {
        throw new InvalidOperationException($"Generated invalid code: {referralCode}");
    }

    return referralCode;
}

private bool IsValidReferralCodeFormat(string code)
{
    // Regex: ZIRA-[A-Z0-9]+
    var regex = new Regex(@"^ZIRA-[A-Z0-9]+$");
    return regex.IsMatch(code);
}
```

### Step 4: Update ReferralController (If needed)

**Ensure response includes referral code**:

```csharp
// Controllers/ReferralController.cs

[HttpPost("generate")]
[Authorize]
public async Task<IActionResult> GenerateReferralLink([FromBody] ReferralGenerateRequest request)
{
    try
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Generate referral code (ensure ZIRA- format)
        var referralCode = await _referralService.GenerateReferralCode(userId);
        // Output example: "ZIRA-K5ZYZX"

        // Send SMS/WhatsApp with NEW format
        var deliveryStatuses = new List<DeliveryStatus>();
        foreach (var phone in request.RecipientPhones)
        {
            DeliveryStatus status;

            if (request.DeliveryMethod == 1 || request.DeliveryMethod == 3)  // SMS
            {
                status = await _smsService.SendReferralSms(phone, referralCode);
                deliveryStatuses.Add(status);
            }

            if (request.DeliveryMethod == 2 || request.DeliveryMethod == 3)  // WhatsApp
            {
                status = await _whatsAppService.SendReferralWhatsApp(phone, referralCode);
                deliveryStatuses.Add(status);
            }
        }

        var response = new ReferralLinkResponse
        {
            ReferralCode = referralCode,
            DeepLink = $"https://ziraai-api-sit.up.railway.app/ref/{referralCode}",
            PlayStoreLink = "https://play.google.com/store/apps/details?id=com.ziraai.app",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            DeliveryStatuses = deliveryStatuses
        };

        return Ok(new { success = true, data = response });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error generating referral link");
        return StatusCode(500, new { success = false, message = "Error generating link" });
    }
}
```

---

## 🧪 Testing

### Test 1: Local Development

**Using Postman/curl**:

```bash
# Generate referral link
curl -X POST "http://localhost:5000/api/v1/Referral/generate" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "recipientPhones": ["+905551234567"],
    "deliveryMethod": 1
  }'
```

**Expected Response**:
```json
{
  "success": true,
  "data": {
    "referralCode": "ZIRA-K5ZYZX",
    "deepLink": "https://ziraai-api-sit.up.railway.app/ref/ZIRA-K5ZYZX",
    "playStoreLink": "https://play.google.com/store/apps/details?id=com.ziraai.app",
    "deliveryStatuses": [
      {
        "phoneNumber": "+905551234567",
        "method": "SMS",
        "status": "Sent"
      }
    ]
  }
}
```

### Test 2: Check SMS Content

**Verify SMS received**:
```
🌱 ZiraAI'ya davet edildiniz!

Referans Kodunuz: ZIRA-K5ZYZX    ← CHECK: Code is visible

Uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app

Uygulama açıldığında kod otomatik gelecek!
```

**Validation Checklist**:
- [ ] SMS received successfully
- [ ] Referral code starts with "ZIRA-"
- [ ] Code is in uppercase
- [ ] Play Store link is correct
- [ ] Message is readable (no encoding issues)

### Test 3: Code Format Validation

**Test with your validation function**:

```csharp
// Unit test
[Fact]
public void ReferralCode_ShouldHaveCorrectFormat()
{
    var code = "ZIRA-K5ZYZX";
    var regex = new Regex(@"^ZIRA-[A-Z0-9]+$");

    Assert.True(regex.IsMatch(code));
}

[Fact]
public void ReferralCode_InvalidFormat_ShouldFail()
{
    var invalidCodes = new[] { "ZIRATEST", "zira-k5zyzx", "K5ZYZX", "ZIRA-" };

    foreach (var code in invalidCodes)
    {
        var regex = new Regex(@"^ZIRA-[A-Z0-9]+$");
        Assert.False(regex.IsMatch(code));
    }
}
```

---

## 📋 Pre-Deployment Checklist

### Code Changes
- [ ] SMS message updated to include `ZIRA-XXXXX` format
- [ ] Referral code generation ensures `ZIRA-` prefix
- [ ] Validation added for code format
- [ ] Unit tests added/updated

### Testing
- [ ] Local testing completed (Postman/curl)
- [ ] Test SMS sent and verified (correct format)
- [ ] Code format validation passing
- [ ] WhatsApp message tested (if applicable)

### Documentation
- [ ] Code changes documented
- [ ] SMS template updated in documentation
- [ ] Changelog updated

### Staging Deployment
- [ ] Deploy to staging environment
- [ ] Test with real phone numbers
- [ ] Verify SMS delivery
- [ ] Check message format on different carriers
- [ ] Monitor error logs

### Production Deployment
- [ ] Production deployment planned
- [ ] Rollback plan ready
- [ ] Monitoring alerts configured
- [ ] Team notified of changes

---

## 🔍 Troubleshooting

### Issue 1: SMS Not Received

**Possible Causes**:
- SMS provider rate limiting
- Invalid phone number format
- Network issues

**Solution**:
```csharp
// Add retry logic
var maxRetries = 3;
for (int i = 0; i < maxRetries; i++)
{
    var result = await _smsProvider.SendAsync(phone, message);
    if (result.Success) break;

    await Task.Delay(TimeSpan.FromSeconds(2));
}
```

### Issue 2: Code Format Invalid

**Possible Causes**:
- Code generation doesn't include `ZIRA-` prefix
- Lowercase letters generated

**Solution**:
```csharp
// Ensure uppercase and prefix
var code = GenerateUniqueCode().ToUpper();  // ← ToUpper()
var referralCode = $"ZIRA-{code}";         // ← Prefix
```

### Issue 3: Special Characters in SMS

**Possible Causes**:
- Encoding issues (emojis not supported by carrier)

**Solution**:
```csharp
// Option 1: Remove emoji for problematic carriers
var message = isInternational
    ? $"ZiraAI'ya davet edildiniz!\n\nReferans Kodunuz: {referralCode}..."  // No emoji
    : $"🌱 ZiraAI'ya davet edildiniz!\n\nReferans Kodunuz: {referralCode}...";  // With emoji

// Option 2: Use plain text
var message = $@"ZiraAI'ya davet edildiniz!

Referans Kodunuz: {referralCode}

Uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app";
```

### Issue 4: Message Too Long (>160 characters)

**Possible Causes**:
- SMS message exceeds single message limit
- Will be sent as multiple messages (may cost more)

**Current Message Length**: ~180 characters (with emoji) = 2 SMS

**Solution** (if cost is concern):
```csharp
// Shorter version
var message = $@"ZiraAI davet!

Kod: {referralCode}

İndir: https://play.google.com/store/apps/details?id=com.ziraai.app";
// ~100 characters = 1 SMS
```

---

## 📊 Monitoring

### Metrics to Track

**Add logging**:
```csharp
_logger.LogInformation(
    "Referral SMS sent: Code={ReferralCode}, Phone={Phone}, Status={Status}",
    referralCode,
    phoneNumber,
    status
);
```

**Monitor**:
- SMS delivery success rate (target: >95%)
- SMS delivery time (target: <5 seconds)
- Invalid code generation rate (target: 0%)
- User complaints about missing codes

**Dashboard Queries**:
```sql
-- SMS delivery success rate (last 24h)
SELECT
  COUNT(CASE WHEN Status = 'Sent' THEN 1 END) * 100.0 / COUNT(*) as SuccessRate
FROM ReferralDeliveries
WHERE CreatedAt > DATEADD(hour, -24, GETDATE())
  AND Method = 'SMS';

-- Invalid code format count
SELECT COUNT(*)
FROM Referrals
WHERE ReferralCode NOT LIKE 'ZIRA-%'
  OR ReferralCode != UPPER(ReferralCode);
```

---

## 🚀 Deployment Strategy

### Staging Deployment (Day 1)

**Steps**:
1. Deploy code to staging
2. Test with internal team (5-10 test SMS)
3. Verify message format on different devices
4. Check logs for errors
5. Get approval from QA team

### Production Deployment (Day 2-3)

**Staged Rollout**:

**Phase 1: 10% (2 hours)**
```
- Enable for 10% of new referral creations
- Monitor SMS delivery rate
- Check for errors in logs
- Verify user feedback
```

**Phase 2: 50% (24 hours)**
```
- If no issues, increase to 50%
- Continue monitoring
- Collect metrics
```

**Phase 3: 100% (48 hours)**
```
- Full rollout
- Monitor for 1 week
- Document lessons learned
```

### Rollback Plan

**If critical issue detected**:

1. **Immediate**: Revert to old SMS format
   ```csharp
   // Emergency rollback
   var message = $"ZiraAI'ya davet edildiniz!\nTıklayın: {deepLink}";
   ```

2. **Communication**: Notify mobile team
3. **Investigation**: Root cause analysis
4. **Fix**: Deploy fix to staging → test → production

---

## 📞 Contact & Support

### Questions?

**Mobile Team**: [Mobile team lead email/slack]
**Backend Team Lead**: [Backend lead email/slack]
**DevOps**: [DevOps contact]

### Documentation

- Technical Architecture: `DEFERRED_DEEP_LINKING_TECHNICAL_ARCHITECTURE.md`
- Mobile Implementation Plan: `SMS_DEFERRED_DEEP_LINKING_IMPLEMENTATION_PLAN.md`
- API Documentation: [Link to Swagger/Postman]

---

## ✅ Quick Reference

### Valid Code Examples
```
✅ ZIRA-K5ZYZX
✅ ZIRA-ABC123
✅ ZIRA-XYZ789
✅ ZIRA-1A2B3C
```

### Invalid Code Examples
```
❌ ZIRATEST123   (no hyphen)
❌ zira-k5zyzx   (lowercase)
❌ K5ZYZX        (no prefix)
❌ ZIRA-         (no code part)
```

### SMS Template (Copy-Paste Ready)
```
🌱 ZiraAI'ya davet edildiniz!

Referans Kodunuz: {referralCode}

Uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app

Uygulama açıldığında kod otomatik gelecek!
```

---

## 🎯 Summary

**What You Changed**:
- ✅ SMS message format (30 lines of code)

**What You Didn't Change**:
- ✅ API endpoints
- ✅ Database schema
- ✅ Business logic
- ✅ Authentication
- ✅ Everything else!

**Time Required**: 30 minutes

**Impact**: Enables 90-95% automatic referral code delivery for new users

---

**Document Version**: 1.0
**Last Updated**: 2025-10-06
**Status**: Ready for Implementation ✅
