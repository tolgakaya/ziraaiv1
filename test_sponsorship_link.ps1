# Test Sponsorship Link Distribution System
# PowerShell script for testing on port 5001

$baseUrl = "http://localhost:5001"
$apiVersion = "v1"

Write-Host "🚀 Testing Sponsorship Link Distribution System" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

# Step 1: Login as Sponsor
Write-Host "`n📝 Step 1: Login as Sponsor" -ForegroundColor Yellow

$loginBody = @{
    email = "sponsor@test.com"
    password = "Test123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/$apiVersion/Auth/login" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json"
    
    $token = $loginResponse.data.token
    Write-Host "✅ Login successful. Token received." -ForegroundColor Green
}
catch {
    Write-Host "❌ Login failed. Creating sponsor account..." -ForegroundColor Red
    
    # Register sponsor if doesn't exist
    $registerBody = @{
        email = "sponsor@test.com"
        password = "Test123!"
        fullName = "Test Sponsor Company"
        userRole = "Sponsor"
    } | ConvertTo-Json
    
    try {
        Invoke-RestMethod -Uri "$baseUrl/api/$apiVersion/Auth/register" `
            -Method POST `
            -Body $registerBody `
            -ContentType "application/json"
        
        Write-Host "✅ Sponsor account created. Logging in..." -ForegroundColor Green
        
        # Try login again
        $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/$apiVersion/Auth/login" `
            -Method POST `
            -Body $loginBody `
            -ContentType "application/json"
        
        $token = $loginResponse.data.token
    }
    catch {
        Write-Host "❌ Failed to create sponsor account: $_" -ForegroundColor Red
        exit 1
    }
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Step 2: Purchase bulk sponsorship codes
Write-Host "`n📝 Step 2: Purchase Bulk Sponsorship Codes" -ForegroundColor Yellow

$purchaseBody = @{
    subscriptionTierId = 2  # M tier
    quantity = 5
    paymentMethod = "CreditCard"
    paymentReference = "TEST-" + (Get-Date -Format "yyyyMMddHHmmss")
} | ConvertTo-Json

try {
    $purchaseResponse = Invoke-RestMethod -Uri "$baseUrl/api/$apiVersion/sponsorship/purchase-bulk" `
        -Method POST `
        -Headers $headers `
        -Body $purchaseBody
    
    Write-Host "✅ Purchased $($purchaseResponse.data.quantity) codes successfully" -ForegroundColor Green
    Write-Host "   Purchase ID: $($purchaseResponse.data.id)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Purchase failed: $_" -ForegroundColor Red
}

# Step 3: Get sponsorship codes
Write-Host "`n📝 Step 3: Get Sponsorship Codes" -ForegroundColor Yellow

try {
    $codesResponse = Invoke-RestMethod -Uri "$baseUrl/api/$apiVersion/sponsorship/codes?onlyUnused=true" `
        -Method GET `
        -Headers $headers
    
    $codes = $codesResponse.data
    Write-Host "✅ Retrieved $($codes.Count) unused codes" -ForegroundColor Green
    
    if ($codes.Count -gt 0) {
        $firstCode = $codes[0]
        Write-Host "   First code: $($firstCode.code)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "❌ Failed to get codes: $_" -ForegroundColor Red
}

# Step 4: Send sponsorship link
Write-Host "`n📝 Step 4: Send Sponsorship Links" -ForegroundColor Yellow

if ($codes -and $codes.Count -gt 0) {
    $sendLinkBody = @{
        recipients = @(
            @{
                code = $codes[0].code
                phone = "+905551234567"
                name = "Ahmet Çiftçi"
            },
            @{
                code = $codes[1].code
                phone = "+905559876543"
                name = "Mehmet Tarım"
            }
        )
        channel = "SMS"
    } | ConvertTo-Json -Depth 3
    
    try {
        $sendResponse = Invoke-RestMethod -Uri "$baseUrl/api/$apiVersion/sponsorship/send-link" `
            -Method POST `
            -Headers $headers `
            -Body $sendLinkBody
        
        Write-Host "✅ Links sent successfully" -ForegroundColor Green
        Write-Host "   Success: $($sendResponse.data.successCount)" -ForegroundColor Gray
        Write-Host "   Failed: $($sendResponse.data.failureCount)" -ForegroundColor Gray
    }
    catch {
        Write-Host "❌ Failed to send links: $_" -ForegroundColor Red
    }
}

# Step 5: Test public redemption link
Write-Host "`n📝 Step 5: Test Public Redemption Link" -ForegroundColor Yellow

if ($codes -and $codes.Count -gt 0) {
    $redemptionUrl = "$baseUrl/redeem/$($codes[0].code)"
    Write-Host "📱 Redemption Link: $redemptionUrl" -ForegroundColor Cyan
    
    # Test the redemption endpoint (should return HTML)
    try {
        $response = Invoke-WebRequest -Uri $redemptionUrl -Method GET
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ Redemption endpoint is accessible" -ForegroundColor Green
            Write-Host "   Response contains: $($response.Content.Substring(0, 100))..." -ForegroundColor Gray
        }
    }
    catch {
        Write-Host "❌ Redemption endpoint error: $_" -ForegroundColor Red
    }
}

# Step 6: Get link statistics
Write-Host "`n📝 Step 6: Get Link Statistics" -ForegroundColor Yellow

try {
    $statsResponse = Invoke-RestMethod -Uri "$baseUrl/api/$apiVersion/sponsorship/statistics" `
        -Method GET `
        -Headers $headers
    
    $stats = $statsResponse.data
    Write-Host "✅ Statistics retrieved successfully" -ForegroundColor Green
    Write-Host "   Total Codes: $($stats.totalCodes)" -ForegroundColor Gray
    Write-Host "   Links Generated: $($stats.totalLinksGenerated)" -ForegroundColor Gray
    Write-Host "   Links Sent: $($stats.totalLinksSent)" -ForegroundColor Gray
    Write-Host "   Links Clicked: $($stats.totalLinksClicked)" -ForegroundColor Gray
    Write-Host "   Conversion Rate: $($stats.conversionRate)%" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Failed to get statistics: $_" -ForegroundColor Red
}

Write-Host "`n✨ Test completed!" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Cyan