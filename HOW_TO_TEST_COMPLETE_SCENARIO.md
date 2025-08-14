# How to Test Complete Sponsorship Scenario

## 🎯 What This Test Does

This test simulates the complete real-world flow:
1. **Sponsor** creates a sponsorship code
2. **Sponsor** sends redemption link via SMS/WhatsApp
3. **Farmer** clicks the link and redeems
4. **System** creates farmer account automatically
5. **System** provides JWT token for auto-login

## 📋 Prerequisites

### 1. Visual Studio App Running
```bash
# Make sure WebAPI is running on port 5001
https://localhost:5001/swagger
```

### 2. Database Ready
```bash
# Check database connection
dotnet script check_database.csx
```

### 3. Get Sponsor JWT Token
You need a valid sponsor JWT token for the full test.

## 🔑 How to Get Sponsor JWT Token

### Method 1: Using Postman (Recommended)

1. **Open ZiraAI Postman Collection**
2. **Find "Login" request**
3. **Use sponsor credentials:**
   ```json
   {
     "email": "sponsor@test.com",
     "password": "Test123!"
   }
   ```
4. **Send request**
5. **Copy JWT token from response:**
   ```json
   {
     "success": true,
     "data": {
       "token": "eyJ0eXAiOiJKV1QiOiJIUzI1NiJ9.eyJ1c2VySWQiOjE..."
     }
   }
   ```

### Method 2: Using PowerShell

```powershell
# Login via API
$loginData = @{
    email = "sponsor@test.com"
    password = "Test123!"
}

$response = Invoke-RestMethod -Uri "https://localhost:5001/api/v1/auth/login" -Method POST -Body ($loginData | ConvertTo-Json) -ContentType "application/json"

if ($response.success) {
    $token = $response.data.token
    Write-Host "Sponsor Token: $token"
    # Copy this token for testing
} else {
    Write-Host "Login failed: $($response.message)"
}
```

### Method 3: Create Sponsor User (if needed)

If you don't have a sponsor user, create one first:

```powershell
# Register sponsor user
$registerData = @{
    fullName = "Test Sponsor"
    email = "sponsor@test.com"
    password = "Test123!"
    mobilePhones = "5559999999"
    role = "Sponsor"
}

$regResponse = Invoke-RestMethod -Uri "https://localhost:5001/api/v1/auth/register" -Method POST -Body ($registerData | ConvertTo-Json) -ContentType "application/json"

if ($regResponse.success) {
    Write-Host "Sponsor user created successfully"
    # Now login to get token
} else {
    Write-Host "Registration failed: $($regResponse.message)"
}
```

## 🚀 Running the Complete Test

### Option 1: Full Test (With Sponsor Token)

```powershell
# Run complete scenario with sponsor token
./test_full_scenario.ps1 -SponsorToken "eyJ0eXAiOiJKV1QiOiJIUzI1NiJ9.eyJ1c2VySWQiOjE..."
```

**What this tests:**
- ✅ Creates real sponsorship code
- ✅ Sends actual redemption link
- ✅ Tests farmer redemption (JSON + HTML)
- ✅ Verifies account creation
- ✅ Tests error scenarios (duplicate, invalid codes)
- ✅ Checks analytics data
- ✅ Tests mobile User-Agent detection

### Option 2: Limited Test (No Token)

```powershell
# Run limited test without token
./test_full_scenario.ps1
```

**What this tests:**
- ✅ API connectivity
- ✅ Endpoint format validation
- ✅ Error handling
- ✅ Mobile User-Agent simulation
- ⚠️ Uses mock data (no real code creation)

## 📊 Expected Test Results

### Successful Full Test Output:

```
COMPLETE SPONSORSHIP SCENARIO TEST
==================================
Base URL: https://localhost:5001
Time: 2025-08-14 15:30:22

STEP 1: API HEALTH CHECK
========================
[OK] API is running (Status: 200)

STEP 2: CREATE SPONSORSHIP CODE
===============================
Creating sponsorship code...
Farmer: Test Farmer 153022
Phone: 5557834
Amount: 127 TL
[SUCCESS] Sponsorship code created!
Code: SPONSOR-2025-ABC123
ID: 45

STEP 3: SEND REDEMPTION LINK
============================
Sending redemption link...
Method: WhatsApp
Recipient: Test Farmer 153022 (5557834)
[SUCCESS] Redemption link sent!
Link: https://localhost:5001/redeem/SPONSOR-2025-ABC123
Status: Success

STEP 4: SIMULATE FARMER REDEMPTION
===================================
Simulating farmer clicking link...

Testing API redemption (JSON response)...
[SUCCESS] Farmer redemption completed!
User created: Test Farmer 153022 (SMS'ten)
Email: test.farmer.153022.generated@ziraai.com
Phone: 905557834
Account was new: True
JWT token generated: 245 chars

Testing mobile HTML redemption...
[SUCCESS] Mobile HTML response received!
Content-Type: text/html; charset=utf-8
Content Length: 3847 chars
[WARNING] No deep link found (may be duplicate redemption)

STEP 5: TEST ERROR SCENARIOS
=============================
Testing duplicate redemption...
[OK] Duplicate redemption properly prevented
Error message: Already redeemed

Testing invalid code...
[OK] Invalid code properly rejected

STEP 6: VERIFY ANALYTICS
=========================
Checking link statistics...
[SUCCESS] Analytics data found!
Click count: 1
Is redeemed: True
Last click: 2025-08-14T15:30:25
Last IP: 127.0.0.1

FULL SCENARIO TEST SUMMARY
===========================
[OK] Sponsorship code created successfully
[OK] Redemption link generated
[OK] Farmer account auto-created
[OK] JWT token generated
[OK] Error scenarios handled
[OK] Mobile/desktop responses working

Test Data Summary:
Sponsor Code: SPONSOR-2025-ABC123
Farmer: Test Farmer 153022
Phone: 5557834
Amount: 127 TL
Redemption URL: https://localhost:5001/redeem/SPONSOR-2025-ABC123

Created Account:
User ID: 156
Email: test.farmer.153022.generated@ziraai.com

COMPLETE SCENARIO TEST FINISHED!
=================================

Database Verification:
To verify data was saved correctly, run:
dotnet script verify_redemption.csx SPONSOR-2025-ABC123 5557834
```

## 🔍 Verifying Test Results

### 1. Database Verification
```bash
# Verify the specific test data
dotnet script verify_redemption.csx SPONSOR-2025-ABC123 5557834

# General database check
dotnet script check_database.csx
```

### 2. Check Created User
```sql
-- Connect to database and check
SELECT * FROM "Users" WHERE "Email" LIKE '%generated@ziraai.com%' ORDER BY "RecordDate" DESC LIMIT 5;
```

### 3. Check Sponsorship Code
```sql
-- Verify code status
SELECT "Code", "IsRedeemed", "LinkClickCount", "RedemptionDate" 
FROM "SponsorshipCodes" 
WHERE "Code" = 'SPONSOR-2025-ABC123';
```

## 🐛 Troubleshooting

### Common Issues

#### 1. "Failed to create sponsorship code"
**Possible causes:**
- Invalid/expired sponsor token
- Wrong user role (not a sponsor)
- API endpoint not implemented
- Database connection issues

**Solutions:**
```powershell
# Check token validity
$headers = @{ 'Authorization' = "Bearer YOUR_TOKEN" }
$userInfo = Invoke-RestMethod -Uri "https://localhost:5001/api/v1/auth/me" -Headers $headers

# Check user role
Write-Host "User role: $($userInfo.data.role)"
```

#### 2. "Failed to send redemption link"
**Possible causes:**
- SMS/WhatsApp service not configured
- Invalid phone number format
- API endpoint missing

**Solutions:**
- Test continues with manual link
- Check API implementation
- Verify phone number format (+90 prefix)

#### 3. "Redemption failed"
**Possible causes:**
- Code already redeemed
- Code expired
- Account creation issues
- Database constraints

**Solutions:**
```powershell
# Check code status first
dotnet script verify_redemption.csx YOUR_CODE YOUR_PHONE
```

#### 4. "No analytics data found"
**Possible causes:**
- Wrong sponsor user ID
- Analytics endpoint not implemented
- Foreign key constraints

**Solutions:**
- Check sponsor user ID in database
- Verify analytics API implementation

### Debug Mode

Enable detailed logging:
```powershell
# Add this to script for more details
$VerbosePreference = "Continue"
$DebugPreference = "Continue"
```

## 📱 Testing Mobile Flow

The test includes mobile User-Agent simulation, but for real mobile testing:

### 1. Test on Real Device
```bash
# Get your computer's IP
ipconfig

# Access from mobile browser
https://192.168.1.XXX:5001/redeem/SPONSOR-2025-ABC123
```

### 2. Test Deep Link (if mobile app exists)
```bash
# iOS Simulator
xcrun simctl openurl booted "ziraai://redeem?code=SPONSOR-2025-ABC123&token=JWT_TOKEN"

# Android
adb shell am start -W -a android.intent.action.VIEW -d "ziraai://redeem?code=SPONSOR-2025-ABC123&token=JWT_TOKEN" com.ziraai.app
```

## 🎯 Success Criteria

A successful test should show:
- ✅ All 6 test steps complete successfully
- ✅ Real sponsorship code created in database
- ✅ Farmer account auto-created with generated email
- ✅ JWT token generated for auto-login
- ✅ Error scenarios handled properly
- ✅ Analytics data recorded correctly
- ✅ Mobile and desktop responses working

## 📊 Performance Expectations

- **Code Creation**: < 2 seconds
- **Link Sending**: < 3 seconds  
- **Redemption**: < 1 second
- **Account Creation**: < 2 seconds
- **Total Test Time**: < 30 seconds

This test validates the entire sponsorship link distribution system end-to-end! 🚀