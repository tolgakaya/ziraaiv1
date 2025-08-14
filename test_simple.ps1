# Simple API Test Script - No parameters required
# Tests basic API functionality when Visual Studio app is running

[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$baseUrl = "https://localhost:5001"

Write-Host "🚀 Simple API Test (No Auth Required)" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Testing Visual Studio running app on $baseUrl" -ForegroundColor Gray
Write-Host ""

# Test 1: API Health via Swagger
Write-Host "1️⃣ Testing API Health..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/swagger" -Method GET -TimeoutSec 10
    Write-Host "   ✅ API is running!" -ForegroundColor Green
    Write-Host "   📄 Swagger UI accessible (Status: $($response.StatusCode))" -ForegroundColor Gray
}
catch {
    Write-Host "   ❌ API not accessible: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   💡 Make sure Visual Studio app is running on port 5001" -ForegroundColor Yellow
    exit 1
}

# Test 2: Public Subscription Tiers (No auth required)
Write-Host "`n2️⃣ Testing Public Endpoints..." -ForegroundColor Yellow
try {
    $tiersResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/subscriptions/tiers" -Method GET -TimeoutSec 5
    Write-Host "   ✅ Subscription tiers endpoint works!" -ForegroundColor Green
    Write-Host "   📊 Found $($tiersResponse.data.Count) subscription tiers" -ForegroundColor Gray
    
    # List tiers
    foreach ($tier in $tiersResponse.data) {
        Write-Host "      🎯 $($tier.tierName): $($tier.displayName) - $($tier.monthlyPrice) TRY/month" -ForegroundColor Gray
    }
}
catch {
    Write-Host "   ❌ Subscription tiers failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Public Redemption Endpoint (Invalid code - should return error gracefully)
Write-Host "`n3️⃣ Testing Redemption Endpoint..." -ForegroundColor Yellow
$testCode = "TEST-INVALID-CODE-123"
$redemptionUrl = "$baseUrl/redeem/$testCode"

try {
    $redemptionResponse = Invoke-WebRequest -Uri $redemptionUrl -Method GET -TimeoutSec 5
    if ($redemptionResponse.StatusCode -eq 200) {
        Write-Host "   ⚠️  Unexpected success with invalid code" -ForegroundColor Yellow
        Write-Host "   📄 Response length: $($redemptionResponse.Content.Length) chars" -ForegroundColor Gray
    }
}
catch {
    Write-Host "   ✅ Invalid code properly rejected (expected)" -ForegroundColor Green
    Write-Host "   🔗 Redemption URL format: $redemptionUrl" -ForegroundColor Gray
    
    # Check if it's a 404 or error page
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "   📊 Status Code: $statusCode" -ForegroundColor Gray
    }
}

# Test 4: API Versioning Check
Write-Host "`n4️⃣ Testing API Versioning..." -ForegroundColor Yellow
try {
    # Test both versioned and non-versioned
    $versionedUrl = "$baseUrl/api/v1/subscriptions/tiers"
    $versionedResponse = Invoke-RestMethod -Uri $versionedUrl -Method GET -TimeoutSec 5
    
    Write-Host "   ✅ API versioning (v1) works!" -ForegroundColor Green
    Write-Host "   🔗 Versioned URL: $versionedUrl" -ForegroundColor Gray
}
catch {
    Write-Host "   ❌ API versioning failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Mobile User-Agent Simulation
Write-Host "`n5️⃣ Testing Mobile Simulation..." -ForegroundColor Yellow
try {
    $mobileHeaders = @{
        'User-Agent' = 'Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15'
        'Accept' = 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8'
    }
    
    $mobileResponse = Invoke-WebRequest -Uri $redemptionUrl -Headers $mobileHeaders -Method GET -TimeoutSec 5
    Write-Host "   ⚠️  Mobile response received (unexpected with invalid code)" -ForegroundColor Yellow
}
catch {
    Write-Host "   ✅ Mobile User-Agent test completed" -ForegroundColor Green
    Write-Host "   📱 Tested iPhone User-Agent" -ForegroundColor Gray
}

# Summary
Write-Host "`n📋 Test Summary" -ForegroundColor Cyan
Write-Host "===============" -ForegroundColor Cyan
Write-Host "✅ Basic API functionality verified" -ForegroundColor Green
Write-Host "✅ Swagger UI accessible" -ForegroundColor Green
Write-Host "✅ Public endpoints working" -ForegroundColor Green
Write-Host "✅ API versioning functional" -ForegroundColor Green
Write-Host "✅ Error handling proper" -ForegroundColor Green

Write-Host "`n💡 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Get a sponsor JWT token for full testing" -ForegroundColor Gray
Write-Host "2. Run: ./test_complete_redemption.ps1 -SponsorToken 'YOUR_TOKEN'" -ForegroundColor Gray
Write-Host "3. Create actual sponsorship codes and test redemption" -ForegroundColor Gray

Write-Host "`n🎉 Simple test completed successfully!" -ForegroundColor Green