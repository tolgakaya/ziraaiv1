# Farmer Link Redemption Testing Guide

## Overview
This guide shows how to simulate a farmer clicking a redemption link in your localhost/staging environment after a sponsor has sent the link.

## Prerequisites
- API running on `https://localhost:5001`
- Valid sponsor JWT token
- At least one sponsorship code created

## Step-by-Step Testing Process

### Phase 1: Sponsor Sends Link (Setup)

#### 1.1 Create Sponsorship Code
```http
POST https://localhost:5001/api/v1/sponsorship/codes
Authorization: Bearer {sponsor_jwt_token}
Content-Type: application/json

{
  "farmerName": "Test Çiftçi",
  "farmerPhone": "5551234567",
  "amount": 100.00,
  "description": "Test desteği",
  "expiryDate": "2025-12-31T23:59:59"
}
```

**Save the generated code** (e.g., `SPONSOR-2025-ABC123`)

#### 1.2 Send Redemption Link
```http
POST https://localhost:5001/api/v1/sponsorship/send-link
Authorization: Bearer {sponsor_jwt_token}
Content-Type: application/json

{
  "codes": [
    {
      "code": "SPONSOR-2025-ABC123",
      "recipientName": "Test Çiftçi",
      "recipientPhone": "5551234567"
    }
  ],
  "sendVia": "WhatsApp",
  "customMessage": "Test mesajı - kodunuz hazır!"
}
```

**Copy the redemption link** from response:
```json
{
  "success": true,
  "data": {
    "results": [
      {
        "redemptionLink": "https://localhost:5001/redeem/SPONSOR-2025-ABC123"
      }
    ]
  }
}
```

### Phase 2: Farmer Clicks Link (Simulation)

#### 2.1 Browser Testing (Primary Method)

**Step 1: Open Clean Browser Session**
```bash
# Chrome incognito mode
chrome --incognito

# Firefox private mode
firefox --private-window

# Edge InPrivate mode
msedge --inprivate
```

**Step 2: Navigate to Redemption Link**

Both endpoints work:
```
# Public endpoint (recommended for SMS/WhatsApp)
https://localhost:5001/redeem/SPONSOR-2025-ABC123

# Versioned API endpoint  
https://localhost:5001/api/v1/redeem/SPONSOR-2025-ABC123
```

**Step 3: Verify HTML Response**
Expected elements:
- ✅ "Tebrikler [Farmer Name]!" header
- ✅ Sponsorship amount and description
- ✅ Account credentials (email, temporary password)
- ✅ Auto-login functionality
- ✅ Redirect buttons to dashboard

**Step 4: Check Browser Storage**
Open DevTools Console (F12):
```javascript
// Check if JWT token is stored
console.log('Token:', localStorage.getItem('ziraai_token'));
console.log('User Data:', localStorage.getItem('ziraai_user'));

// Verify token is valid
const token = localStorage.getItem('ziraai_token');
if (token) {
    console.log('✅ Token found, length:', token.length);
    
    // Parse JWT payload (without verification)
    const payload = JSON.parse(atob(token.split('.')[1]));
    console.log('Token payload:', payload);
    console.log('Expires:', new Date(payload.exp * 1000));
} else {
    console.log('❌ No token found in localStorage');
}
```

#### 2.2 API Testing (JSON Response)

**PowerShell Script:**
```powershell
# test_farmer_redemption.ps1
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$redemptionLink = "https://localhost:5001/redeem/SPONSOR-2025-ABC123"

Write-Host "Testing farmer redemption..." -ForegroundColor Cyan
Write-Host "Link: $redemptionLink" -ForegroundColor Gray

try {
    # Test with JSON Accept header
    $headers = @{
        'Accept' = 'application/json'
        'Content-Type' = 'application/json'
    }
    
    $response = Invoke-RestMethod -Uri $redemptionLink -Method GET -Headers $headers
    
    Write-Host "✅ Redemption successful!" -ForegroundColor Green
    Write-Host "User created: $($response.data.user.fullName)" -ForegroundColor Gray
    Write-Host "Email: $($response.data.user.email)" -ForegroundColor Gray
    Write-Host "Token length: $($response.data.authentication.token.Length)" -ForegroundColor Gray
    Write-Host "Account was created: $($response.data.user.wasAccountCreated)" -ForegroundColor Gray
    
    # Pretty print full response
    $response | ConvertTo-Json -Depth 10 | Write-Host
}
catch {
    Write-Host "❌ Redemption failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
    }
}
```

**Curl Command:**
```bash
# Test redemption with curl
curl -X GET "https://localhost:5001/redeem/SPONSOR-2025-ABC123" \
     -H "Accept: application/json" \
     -H "Content-Type: application/json" \
     -k \
     -w "Status: %{http_code}\nTime: %{time_total}s\n"
```

#### 2.3 Mobile Device Simulation

**Chrome DevTools Method:**
1. Open Chrome DevTools (F12)
2. Toggle device toolbar (Ctrl+Shift+M)
3. Select device (iPhone 12, Samsung Galaxy, etc.)
4. Navigate to redemption link
5. Test touch interactions

**Responsive Design Test:**
```javascript
// Test different viewport sizes
const viewports = [
    { width: 375, height: 667, name: 'iPhone SE' },
    { width: 414, height: 896, name: 'iPhone 11 Pro Max' },
    { width: 360, height: 640, name: 'Galaxy S5' },
    { width: 768, height: 1024, name: 'iPad' }
];

viewports.forEach(viewport => {
    console.log(`Testing ${viewport.name}: ${viewport.width}x${viewport.height}`);
    window.resizeTo(viewport.width, viewport.height);
    // Manual verification of layout
});
```

### Phase 3: Verification and Analysis

#### 3.1 Database Verification Script

Create `verify_redemption.csx`:
```csharp
#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.4"

using Npgsql;
using System;

var connectionString = "Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass";
var testCode = "SPONSOR-2025-ABC123";
var testPhone = "905551234567";

try
{
    Console.WriteLine($"🔍 Verifying redemption for code: {testCode}");
    
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    // Check sponsorship code status
    var codeCmd = new NpgsqlCommand(@"
        SELECT ""Code"", ""IsRedeemed"", ""RedemptionDate"", ""LinkClickCount"", 
               ""LinkClickDate"", ""LastClickIpAddress"", ""RecipientPhone""
        FROM ""SponsorshipCodes"" 
        WHERE ""Code"" = @code", connection);
    
    codeCmd.Parameters.AddWithValue("@code", testCode);
    
    await using var codeReader = await codeCmd.ExecuteReaderAsync();
    
    if (await codeReader.ReadAsync())
    {
        var isRedeemed = codeReader.GetBoolean("IsRedeemed");
        var redemptionDate = codeReader.IsDBNull("RedemptionDate") ? null : codeReader.GetDateTime("RedemptionDate");
        var clickCount = codeReader.GetInt32("LinkClickCount");
        var clickDate = codeReader.IsDBNull("LinkClickDate") ? null : codeReader.GetDateTime("LinkClickDate");
        var lastIP = codeReader.IsDBNull("LastClickIpAddress") ? "N/A" : codeReader.GetString("LastClickIpAddress");
        
        Console.WriteLine("📋 Sponsorship Code Status:");
        Console.WriteLine($"   ✅ Is Redeemed: {isRedeemed}");
        Console.WriteLine($"   📅 Redemption Date: {redemptionDate}");
        Console.WriteLine($"   🖱️  Click Count: {clickCount}");
        Console.WriteLine($"   🕐 Last Click: {clickDate}");
        Console.WriteLine($"   🌐 Last IP: {lastIP}");
    }
    else
    {
        Console.WriteLine($"❌ Code {testCode} not found!");
        return;
    }
    
    await codeReader.CloseAsync();
    
    // Check if user was created
    var userCmd = new NpgsqlCommand(@"
        SELECT ""Id"", ""FullName"", ""Email"", ""MobilePhones"", ""Status"", ""RecordDate""
        FROM ""Users"" 
        WHERE ""MobilePhones"" = @phone", connection);
    
    userCmd.Parameters.AddWithValue("@phone", testPhone);
    
    await using var userReader = await userCmd.ExecuteReaderAsync();
    
    if (await userReader.ReadAsync())
    {
        var userId = userReader.GetInt32("Id");
        var fullName = userReader.GetString("FullName");
        var email = userReader.GetString("Email");
        var phone = userReader.GetString("MobilePhones");
        var status = userReader.GetBoolean("Status");
        var recordDate = userReader.GetDateTime("RecordDate");
        
        Console.WriteLine("\n👤 User Account:");
        Console.WriteLine($"   🆔 ID: {userId}");
        Console.WriteLine($"   👨 Name: {fullName}");
        Console.WriteLine($"   📧 Email: {email}");
        Console.WriteLine($"   📱 Phone: {phone}");
        Console.WriteLine($"   ✅ Active: {status}");
        Console.WriteLine($"   📅 Created: {recordDate}");
    }
    else
    {
        Console.WriteLine($"\n👤 User with phone {testPhone} not found!");
    }
    
    Console.WriteLine("\n🎉 Verification completed!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}
```

**Run verification:**
```bash
dotnet script verify_redemption.csx
```

#### 3.2 Analytics Verification

**Check Link Statistics:**
```http
GET https://localhost:5001/api/v1/sponsorship/link-statistics?sponsorUserId=123
Authorization: Bearer {sponsor_jwt_token}
```

**Verify These Metrics:**
- ✅ `linkClickCount` increased to 1
- ✅ `linkClickDate` has current timestamp
- ✅ `isRedeemed` changed to true
- ✅ `redemptionDate` is set
- ✅ `lastClickIpAddress` is recorded
- ✅ `farmerAccountCreated` is true

### Phase 4: Error Scenario Testing

#### 4.1 Invalid Code Test
```powershell
# Test with non-existent code
$invalidLink = "https://localhost:5001/redeem/INVALID-CODE-123"

try {
    $response = Invoke-RestMethod -Uri $invalidLink -Method GET
    Write-Host "❌ Should have failed!" -ForegroundColor Red
}
catch {
    Write-Host "✅ Invalid code properly rejected" -ForegroundColor Green
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Gray
}
```

#### 4.2 Duplicate Redemption Test
```powershell
# Try to redeem the same code again
$sameLink = "https://localhost:5001/redeem/SPONSOR-2025-ABC123"

try {
    $response = Invoke-RestMethod -Uri $sameLink -Method GET
    Write-Host "❌ Should have failed - already redeemed!" -ForegroundColor Red
}
catch {
    Write-Host "✅ Duplicate redemption properly prevented" -ForegroundColor Green
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Gray
}
```

#### 4.3 Expired Code Test
```powershell
# Create an expired code first, then test
# (Requires creating a code with past expiry date)
$expiredLink = "https://localhost:5001/redeem/EXPIRED-CODE-123"

try {
    $response = Invoke-RestMethod -Uri $expiredLink -Method GET
    Write-Host "❌ Should have failed - expired!" -ForegroundColor Red
}
catch {
    Write-Host "✅ Expired code properly rejected" -ForegroundColor Green
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Gray
}
```

### Phase 5: Performance and Load Testing

#### 5.1 Response Time Test
```powershell
# Measure redemption response time
Measure-Command {
    $response = Invoke-RestMethod -Uri "https://localhost:5001/redeem/NEW-CODE-123" -Method GET
}

# Expected: < 1 second for first redemption
# Expected: < 500ms for subsequent clicks (already redeemed)
```

#### 5.2 Concurrent Access Test
```powershell
# Test multiple simultaneous requests to same code
$jobs = @()

1..5 | ForEach-Object {
    $jobs += Start-Job -ScriptBlock {
        param($link)
        try {
            Invoke-RestMethod -Uri $link -Method GET
        } catch {
            $_.Exception.Message
        }
    } -ArgumentList "https://localhost:5001/redeem/CONCURRENT-TEST-123"
}

# Wait for all jobs and check results
$results = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job

Write-Host "Concurrent test results:"
$results | ForEach-Object { Write-Host "  $_" }
```

### Phase 6: Network Analysis

#### 6.1 Request/Response Analysis
**Browser DevTools Network Tab:**
1. Open DevTools (F12) → Network tab
2. Click redemption link
3. Analyze the request:

**Expected Request Headers:**
```
GET /redeem/SPONSOR-2025-ABC123 HTTP/1.1
Host: localhost:5001
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8
User-Agent: Mozilla/5.0 (compatible browser string)
```

**Expected Response Headers:**
```
HTTP/1.1 200 OK
Content-Type: text/html; charset=utf-8
Content-Length: [size]
Set-Cookie: [session cookies if any]
```

#### 6.2 Security Headers Check
```bash
# Check security headers
curl -I "https://localhost:5001/redeem/TEST-CODE" -k

# Look for:
# - X-Content-Type-Options: nosniff
# - X-Frame-Options: DENY
# - X-XSS-Protection: 1; mode=block
```

### Phase 7: Frontend Integration Testing

#### 7.1 Auto-Login Verification
```javascript
// Test auto-login functionality
function testAutoLogin() {
    const token = localStorage.getItem('ziraai_token');
    
    if (!token) {
        console.log('❌ No token found');
        return false;
    }
    
    // Test if token is valid by making authenticated request
    fetch('/api/v1/auth/me', {
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    })
    .then(response => {
        if (response.ok) {
            console.log('✅ Auto-login successful');
            return response.json();
        } else {
            console.log('❌ Token invalid');
        }
    })
    .then(user => {
        console.log('Current user:', user);
    })
    .catch(error => {
        console.log('❌ Auto-login failed:', error);
    });
}

// Run after redemption
testAutoLogin();
```

#### 7.2 Redirect Functionality Test
```javascript
// Test automatic redirect after redemption
function testRedirect() {
    console.log('Testing redirect in 3 seconds...');
    
    setTimeout(() => {
        if (window.location.pathname === '/dashboard') {
            console.log('✅ Redirect to dashboard successful');
        } else {
            console.log('❌ Redirect failed, current path:', window.location.pathname);
        }
    }, 3000);
}
```

## Complete Test Script

Save as `test_complete_redemption.ps1`:
```powershell
#!/usr/bin/env pwsh
# Complete Farmer Redemption Test Script

param(
    [Parameter(Mandatory=$true)]
    [string]$SponsorToken,
    
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "https://localhost:5001"
)

[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

Write-Host "🎯 Complete Farmer Redemption Test" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Step 1: Create sponsorship code
Write-Host "`n1️⃣ Creating sponsorship code..." -ForegroundColor Yellow

$codeData = @{
    farmerName = "Test Farmer $(Get-Date -Format 'HHmmss')"
    farmerPhone = "555$(Get-Random -Minimum 1000000 -Maximum 9999999)"
    amount = 100.00
    description = "Test redemption - $(Get-Date)"
    expiryDate = (Get-Date).AddDays(30).ToString("yyyy-MM-ddTHH:mm:ss")
}

$headers = @{
    'Authorization' = "Bearer $SponsorToken"
    'Content-Type' = 'application/json'
}

try {
    $codeResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/sponsorship/codes" -Method POST -Body ($codeData | ConvertTo-Json) -Headers $headers
    $sponsorCode = $codeResponse.data.code
    Write-Host "✅ Code created: $sponsorCode" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to create code: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Send redemption link
Write-Host "`n2️⃣ Sending redemption link..." -ForegroundColor Yellow

$linkData = @{
    codes = @(
        @{
            code = $sponsorCode
            recipientName = $codeData.farmerName
            recipientPhone = $codeData.farmerPhone
        }
    )
    sendVia = "WhatsApp"
    customMessage = "Test redemption link"
}

try {
    $linkResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/sponsorship/send-link" -Method POST -Body ($linkData | ConvertTo-Json) -Headers $headers
    $redemptionLink = $linkResponse.data.results[0].redemptionLink
    Write-Host "✅ Link sent: $redemptionLink" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to send link: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: Test farmer redemption
Write-Host "`n3️⃣ Testing farmer redemption..." -ForegroundColor Yellow

try {
    $redemptionResponse = Invoke-RestMethod -Uri $redemptionLink -Method GET -Headers @{'Accept' = 'application/json'}
    Write-Host "✅ Redemption successful!" -ForegroundColor Green
    Write-Host "   User: $($redemptionResponse.data.user.fullName)" -ForegroundColor Gray
    Write-Host "   Email: $($redemptionResponse.data.user.email)" -ForegroundColor Gray
    Write-Host "   Account created: $($redemptionResponse.data.user.wasAccountCreated)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Redemption failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 4: Test duplicate redemption (should fail)
Write-Host "`n4️⃣ Testing duplicate redemption..." -ForegroundColor Yellow

try {
    $duplicateResponse = Invoke-RestMethod -Uri $redemptionLink -Method GET -Headers @{'Accept' = 'application/json'}
    Write-Host "❌ Duplicate redemption should have failed!" -ForegroundColor Red
}
catch {
    Write-Host "✅ Duplicate redemption properly prevented" -ForegroundColor Green
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
}

# Step 5: Verify analytics
Write-Host "`n5️⃣ Verifying analytics..." -ForegroundColor Yellow

try {
    $statsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/sponsorship/link-statistics?sponsorUserId=1" -Method GET -Headers $headers
    $codeStats = $statsResponse.data.statistics | Where-Object { $_.code -eq $sponsorCode }
    
    if ($codeStats) {
        Write-Host "✅ Analytics updated:" -ForegroundColor Green
        Write-Host "   Click count: $($codeStats.linkClickCount)" -ForegroundColor Gray
        Write-Host "   Redeemed: $($codeStats.isRedeemed)" -ForegroundColor Gray
        Write-Host "   Last IP: $($codeStats.lastClickIpAddress)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "⚠️ Analytics check failed: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n🎉 Complete test finished!" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Cyan
```

**Usage:**
```bash
# Run complete test
./test_complete_redemption.ps1 -SponsorToken "eyJ0eXAiOiJKV1Q..."
```

This comprehensive guide covers all aspects of testing farmer link redemption in your staging environment!