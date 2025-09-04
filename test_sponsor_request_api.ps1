# WhatsApp Sponsor Request System Test Script
# Tests the newly implemented sponsor request API endpoints

$baseUrl = "https://localhost:5001"  # Adjust based on your API URL
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

Write-Host "🧪 Testing WhatsApp Sponsor Request System" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

# Test 1: Process deeplink (no auth required)
Write-Host "`n1. Testing deeplink processing..." -ForegroundColor Yellow
$testToken = "sample-test-token-12345"
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/sponsor-request/process/$testToken" -Method GET -Headers $headers
    Write-Host "✅ Deeplink endpoint accessible" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Deeplink test failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Create sponsor request (requires Farmer auth)
Write-Host "`n2. Testing sponsor request creation..." -ForegroundColor Yellow
$createRequest = @{
    "sponsorPhone" = "+905557654321"
    "requestMessage" = "Yapay destekli ZiraAI kullanarak bitkilerimi analiz yapmak istiyorum. Bunun için ZiraAI uygulamasında sponsor olmanızı istiyorum."
    "requestedTierId" = 2
}

try {
    # This will fail without proper authentication, but tests endpoint exists
    $response = Invoke-RestMethod -Uri "$baseUrl/api/sponsor-request/create" -Method POST -Headers $headers -Body ($createRequest | ConvertTo-Json)
    Write-Host "✅ Create endpoint accessible" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor Cyan
} catch {
    if ($_.Exception.Message -like "*Unauthorized*" -or $_.Exception.Message -like "*401*") {
        Write-Host "✅ Create endpoint requires authentication (as expected)" -ForegroundColor Green
    } else {
        Write-Host "❌ Create test failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test 3: Get pending requests (requires Sponsor auth)
Write-Host "`n3. Testing pending requests retrieval..." -ForegroundColor Yellow
try {
    # This will fail without proper authentication, but tests endpoint exists
    $response = Invoke-RestMethod -Uri "$baseUrl/api/sponsor-request/pending" -Method GET -Headers $headers
    Write-Host "✅ Pending requests endpoint accessible" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor Cyan
} catch {
    if ($_.Exception.Message -like "*Unauthorized*" -or $_.Exception.Message -like "*401*") {
        Write-Host "✅ Pending requests endpoint requires authentication (as expected)" -ForegroundColor Green
    } else {
        Write-Host "❌ Pending requests test failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test 4: Approve requests (requires Sponsor auth)
Write-Host "`n4. Testing request approval..." -ForegroundColor Yellow
$approveRequest = @{
    "requestIds" = @(1, 2, 3)
    "subscriptionTierId" = 2
    "approvalNotes" = "Approved for testing"
}

try {
    # This will fail without proper authentication, but tests endpoint exists
    $response = Invoke-RestMethod -Uri "$baseUrl/api/sponsor-request/approve" -Method POST -Headers $headers -Body ($approveRequest | ConvertTo-Json)
    Write-Host "✅ Approve endpoint accessible" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor Cyan
} catch {
    if ($_.Exception.Message -like "*Unauthorized*" -or $_.Exception.Message -like "*401*") {
        Write-Host "✅ Approve endpoint requires authentication (as expected)" -ForegroundColor Green
    } else {
        Write-Host "❌ Approve test failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n🎉 WhatsApp Sponsor Request API Test Complete!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Apply database migration: dotnet ef database update" -ForegroundColor White
Write-Host "2. Test with authenticated users (Farmer and Sponsor roles)" -ForegroundColor White
Write-Host "3. Test complete workflow: Create → Process Deeplink → Approve" -ForegroundColor White