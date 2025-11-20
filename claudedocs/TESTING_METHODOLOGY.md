# Testing Methodology for ZiraAI Staging Environment

**Environment**: Windows 11  
**Platform**: Railway Staging  
**Date Created**: 2025-10-26

## Common Issues and Solutions

### Issue 1: Curl Commands on Windows
**Problem**: Curl commands with complex JSON bodies fail on Windows bash  
**Symptoms**: 
- Syntax errors with `$()` 
- Quote escaping issues
- Empty responses

**Solution**: Use simple curl without variable assignments
```bash
# ❌ WRONG - Don't use variable assignment
MAIN_TOKEN=$(curl -s ...)

# ✅ RIGHT - Direct curl with escaped quotes
curl -s -X POST "https://url.com/api/endpoint" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d "{\"field\":\"value\"}"
```

### Issue 2: PowerShell Script Execution
**Problem**: PowerShell scripts blocked or syntax errors  
**Symptoms**: 
- `Empty pipe element` errors
- Execution policy restrictions

**Solution**: Use inline PowerShell commands instead of scripts
```bash
# ❌ WRONG - Script file execution
powershell -ExecutionPolicy Bypass -File test.ps1

# ✅ RIGHT - Inline PowerShell command
powershell -Command "Invoke-RestMethod -Uri 'https://url.com' -Method Get"
```

### Issue 3: JSON Response Parsing
**Problem**: Cannot parse JSON responses in bash  
**Solution**: Save to file first, then read
```bash
# Save response
curl -s ... > /tmp/response.json

# Read response
cat /tmp/response.json
```

### Issue 4: Token Extraction
**Problem**: Complex token extraction fails  
**Solution**: Use full response, copy token manually
```bash
# Don't try to extract with grep/sed on Windows
# Just output full response and copy token manually
curl -s -X POST ".../verify-phone-otp" -d "..." | cat
```

## ✅ WORKING METHOD (2025-10-26)

### **USE THIS**: Curl with -i flag (shows status codes)
```bash
# Always use -i to see HTTP status codes
curl -i -X GET "https://url.com/endpoint" \
  -H "Authorization: Bearer TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

**Why this works**:
- `-i` shows HTTP headers and status code
- Can see 200 OK, 400 Bad Request, 401 Unauthorized, etc.
- Helps debug auth vs validation vs logic errors

**Common Mistakes**:
- ❌ Using `-s` alone (silent, hides status codes)
- ❌ Not checking HTTP status code
- ❌ Assuming empty response = failure (could be 200 OK with empty data)

## Recommended Testing Approach for Windows

### Method 1: Direct Curl with -i flag (BEST)
Use for simple GET/POST requests:
```bash
# GET request
curl -s -X GET "https://ziraai-api-sit.up.railway.app/api/v1/endpoint" \
  -H "Authorization: Bearer TOKEN_HERE" \
  -H "x-dev-arch-version: 1.0"

# POST request  
curl -s -X POST "https://ziraai-api-sit.up.railway.app/api/v1/endpoint" \
  -H "Authorization: Bearer TOKEN_HERE" \
  -H "x-dev-arch-version: 1.0" \
  -H "Content-Type: application/json" \
  -d "{\"field\":\"value\"}"
```

### Method 2: Postman Collection (Recommended)
- Import existing Postman collection
- Set environment variables for tokens
- Run tests through Postman UI
- Export results

### Method 3: Manual REST Client
Use VS Code REST Client extension:
```http
### Test 1
GET https://ziraai-api-sit.up.railway.app/api/v1/endpoint
Authorization: Bearer {{token}}
x-dev-arch-version: 1.0

### Test 2  
POST https://ziraai-api-sit.up.railway.app/api/v1/endpoint
Authorization: Bearer {{token}}
x-dev-arch-version: 1.0
Content-Type: application/json

{
  "field": "value"
}
```

## Authentication Flow (Works!)

### Step 1: Request OTP
```bash
curl -s -X POST "https://ziraai-api-sit.up.railway.app/api/v1/Auth/login-phone" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d "{\"mobilePhone\":\"05411111114\"}"
```

**Response**:
```json
{"data":{"status":"Ok","message":"SendMobileCode175375"},"success":true}
```

### Step 2: Verify OTP
```bash
curl -s -X POST "https://ziraai-api-sit.up.railway.app/api/v1/Auth/verify-phone-otp" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d "{\"mobilePhone\":\"05411111114\",\"code\":\"175375\"}"
```

**Response**: Full token response (copy token manually)

### Step 3: Use Token
```bash
TOKEN="paste_token_here"

curl -s -X GET "https://ziraai-api-sit.up.railway.app/api/v1/endpoint" \
  -H "Authorization: Bearer $TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

## Test Data

### Test Users (Always Available)
- **Main Sponsor**: 05411111114 (UserId: 159)
- **Dealer**: 05411111113 (UserId: 158)

### Required Headers
```
Authorization: Bearer {token}
x-dev-arch-version: 1.0
Content-Type: application/json (for POST/PUT)
```

## Troubleshooting Checklist

When a test fails:

1. ✅ **Check Token Validity**
   - Tokens expire in 1 hour
   - Get fresh token if needed

2. ✅ **Check Endpoint URL**
   - Base URL: `https://ziraai-api-sit.up.railway.app/api/v1`
   - Correct version in URL

3. ✅ **Check Headers**
   - Authorization header present
   - x-dev-arch-version header present
   - Content-Type for POST requests

4. ✅ **Check JSON Syntax**
   - Properly escaped quotes in bash: `\"`
   - Valid JSON structure
   - No trailing commas

5. ✅ **Check Response**
   - Empty response = endpoint issue or auth failure
   - Error response = check error message
   - Success response = test passed

## Best Practices

1. **Keep It Simple**: Use direct curl commands, avoid complex scripting
2. **Save Responses**: Always save to file for debugging
3. **Manual Token Management**: Copy/paste tokens, don't automate extraction
4. **Test One at a Time**: Don't batch tests, run individually
5. **Document Results**: Save request/response pairs manually

## Current Testing Session

**Last Successful Test**: 2025-10-26
- Login with phone OTP ✅
- Token retrieval ✅
- Token usage: PENDING (testing dealer endpoints)

**Current Issue**: 
- Curl responses appearing empty
- Need to verify if endpoint is working or auth issue
