# SMS Retriever API - Quick Reference Card

## Environment Hash Codes

| Environment | Hash Code | Package Name |
|-------------|-----------|--------------|
| **Production** | `YmnluTO3ErN` | com.ziraai.app |
| **Staging** | `2YocBG2c6D1` | com.ziraai.app.staging |
| **Development** | `jEcisGBcK6d` | com.ziraai.app.dev |

---

## OTP Message Template

```
ZiraAI dogrulama kodunuz: {{OTP_CODE}}
<#> {{APP_HASH}}
```

**Requirements**:
- Message < 140 characters ✅
- OTP = 4-6 digits ✅
- Hash on same/next line after OTP ✅
- Format: `<#> XXXXXXXXXXX` ✅

---

## Environment Variables

**Production**:
```bash
ASPNETCORE_ENVIRONMENT=Production
```

**Staging**:
```bash
ASPNETCORE_ENVIRONMENT=Staging
```

**Development**:
```bash
ASPNETCORE_ENVIRONMENT=Development
```

---

## Quick Test Commands

### Development Test
```bash
# Set environment
set ASPNETCORE_ENVIRONMENT=Development

# Run API
dotnet run --project WebAPI

# Test endpoint
curl -X POST http://localhost:5000/api/auth/register-phone \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber": "+905321234567"}'

# Expected console output
📱 MOCK OTP SMS (Google SMS Retriever API)
   Environment: Development
   App Hash: jEcisGBcK6d
```

### Check Logs
```bash
# Check environment
grep "Environment:" logs/api.log

# Check hash
grep "App Hash:" logs/api.log

# Check OTP sent
grep "OTP sent successfully" logs/api.log
```

---

## Troubleshooting Quick Checks

### ❌ Wrong hash in SMS
```bash
# Check environment variable
echo $ASPNETCORE_ENVIRONMENT
# Must be: Production, Staging, or Development (exact case)
```

### ❌ OTP not auto-filling
```bash
# Verify SMS format
1. Contains: <#> XXXXXXXXXXX ✓
2. Message < 140 chars ✓
3. OTP = 4-6 digits ✓
4. Hash matches environment ✓
```

### ❌ Message too long
```bash
# Check message length in logs
grep "Message Length:" logs/api.log
# Should show: "Message Length: XX/140 chars"
```

---

## Files Changed

1. ✅ `Business/Services/Messaging/SmsRetrieverHelper.cs` - NEW
2. ✅ `Business/Services/Messaging/NetgsmSmsService.cs` - UPDATED
3. ✅ `Business/Services/Messaging/TurkcellSmsService.cs` - UPDATED
4. ✅ `Business/Services/Messaging/Fakes/MockSmsService.cs` - UPDATED

---

## Key Methods

### SmsRetrieverHelper
```csharp
// Get hash for current environment
string GetAppSignatureHash()

// Build OTP message with hash
string BuildOtpSmsMessage(string otpCode)

// Validate OTP format
bool IsValidOtpCode(string otpCode)
```

---

## Deployment Checklist

- [ ] Set `ASPNETCORE_ENVIRONMENT` in Railway
- [ ] Verify environment variable is exact case
- [ ] Deploy to environment
- [ ] Test OTP endpoint
- [ ] Check SMS logs for hash code
- [ ] Test mobile app auto-fill
- [ ] Monitor error logs

---

## Support Links

- **Backend Guide**: [SMS_RETRIEVER_BACKEND_IMPLEMENTATION.md](./SMS_RETRIEVER_BACKEND_IMPLEMENTATION.md)
- **Mobile Summary**: [BACKEND_SMS_INTEGRATION_SUMMARY.md](./BACKEND_SMS_INTEGRATION_SUMMARY.md)
- **Google Docs**: https://developers.google.com/identity/sms-retriever/overview

---

**Date**: 2025-12-29 | **Status**: ✅ Complete
