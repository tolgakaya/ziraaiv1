# Quick Testing Guide - Fixed Scripts

## 🚀 Problem Solved!

The test scripts now work without mandatory parameters. Here are the fixed versions:

## 📝 Available Test Scripts

### 1. **test_quick.ps1** - Fastest Test (NEW)
**No parameters needed** - Perfect for quick API check
```powershell
./test_quick.ps1
```

**What it tests:**
- ✅ API health (Swagger)
- ✅ Public endpoints (no auth)
- ✅ API versioning
- ✅ Mobile User-Agent simulation
- ✅ Error handling

### 2. **test_simple.ps1** - Basic API Test (NEW)
**No parameters needed** - More detailed than quick test
```powershell
./test_simple.ps1
```

**What it tests:**
- ✅ API connectivity
- ✅ Subscription tiers
- ✅ Redemption endpoint format
- ✅ Mobile simulation with fallback
- ✅ Performance timing

### 3. **test_link_system.ps1** - System Test (FIXED)
**No parameters needed** - Tests core system
```powershell
./test_link_system.ps1
```

**What it tests:**
- ✅ API health via Swagger
- ✅ Public redemption endpoint
- ✅ API versioning
- ✅ Error scenarios

### 4. **test_https_api.ps1** - HTTPS Test (FIXED)
**No parameters needed** - HTTPS connectivity
```powershell
./test_https_api.ps1
```

**What it tests:**
- ✅ HTTPS SSL connection
- ✅ Swagger UI access
- ✅ Versioned endpoints
- ✅ Public redemption URLs

### 5. **check_database.csx** - Database Check (NEW)
**No parameters needed** - Database connectivity
```bash
dotnet script check_database.csx
```

**What it checks:**
- ✅ Database connection
- ✅ Required tables exist
- ✅ Data counts
- ✅ Link distribution fields
- ✅ Migration status

### 6. **test_complete_redemption.ps1** - Full Test (FIXED)
**Optional parameters** - Complete flow test
```powershell
# Without sponsor token (limited test)
./test_complete_redemption.ps1

# With sponsor token (full test)
./test_complete_redemption.ps1 -SponsorToken "eyJ0eXAiOiJKV1Q..."
```

**What it tests:**
- ✅ Full redemption flow (if token provided)
- ✅ Error scenarios
- ✅ Mobile/desktop detection
- ✅ Analytics verification
- ✅ Performance metrics

### 7. **verify_redemption.csx** - Verification (FIXED)
**Optional parameters** - Database verification
```bash
# Use default test values
dotnet script verify_redemption.csx

# Specify code and phone
dotnet script verify_redemption.csx SPONSOR-2025-ABC123 5551234567
```

**What it verifies:**
- ✅ Code redemption status
- ✅ User account creation
- ✅ Click tracking
- ✅ Analytics data

## 🎯 Recommended Testing Flow

### Step 1: Quick Health Check
```powershell
./test_quick.ps1
```
**Expected result:** All green checkmarks

### Step 2: Database Check
```bash
dotnet script check_database.csx
```
**Expected result:** All tables found, field checks pass

### Step 3: Basic System Test
```powershell
./test_simple.ps1
```
**Expected result:** API endpoints working properly

### Step 4: Full System Test (if you have sponsor token)
```powershell
# Get sponsor token first by logging in as sponsor user
./test_complete_redemption.ps1 -SponsorToken "YOUR_TOKEN"
```

## 📋 Getting Sponsor Token

### Method 1: Postman
1. Open ZiraAI Postman Collection
2. Use "Login" request with sponsor credentials
3. Copy JWT token from response
4. Use in test scripts

### Method 2: API Call
```powershell
$loginData = @{
    email = "sponsor@test.com"
    password = "Test123!"
}

$response = Invoke-RestMethod -Uri "https://localhost:5001/api/v1/auth/login" -Method POST -Body ($loginData | ConvertTo-Json) -ContentType "application/json"
$token = $response.data.token
Write-Host "Token: $token"
```

## 🔧 Troubleshooting

### Common Issues

#### 1. "API not responding"
**Solution:**
- Start Visual Studio
- Run the WebAPI project
- Ensure it's running on port 5001
- Check HTTPS is enabled

#### 2. "Database connection failed"  
**Solution:**
- Start PostgreSQL service
- Check connection string in scripts
- Verify database exists: `ziraai_dev`
- Check credentials: `ziraai/devpass`

#### 3. "SSL certificate errors"
**Solution:**
- Scripts include SSL bypass: `[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}`
- For development, this is handled automatically

#### 4. "Subscription tiers not found"
**Solution:**
- Run database migrations
- Check if seed data was applied
- Verify SubscriptionTiers table has data

### Debug Mode
Enable verbose output by modifying scripts:
```powershell
$VerbosePreference = "Continue"
$DebugPreference = "Continue"
```

## 📊 Expected Test Results

### All Green (Success)
```
✅ API is running
✅ Database connection successful  
✅ Public endpoints working
✅ API versioning functional
✅ Error handling proper
✅ Mobile simulation ready
```

### Mixed Results (Partial Issues)
```
✅ API is running
❌ Database connection failed
⚠️  Some endpoints not working
```
**Action:** Check database and authentication

### All Red (Major Issues)  
```
❌ API not running
❌ Database unreachable
❌ Endpoints failing
```
**Action:** Check Visual Studio app, PostgreSQL service

## 🎉 Script Improvements Made

1. **Removed mandatory parameters** from all scripts
2. **Added fallback behavior** when tokens/parameters missing  
3. **Better error messages** with troubleshooting hints
4. **Graceful degradation** - partial tests when full setup unavailable
5. **Clear usage instructions** in script output
6. **Default values** for all optional parameters

Now you can run any script without parameters and get meaningful results! 🚀