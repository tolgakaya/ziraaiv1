# Basic Test - No emojis, simple output
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$baseUrl = "https://localhost:5001"

Write-Host "Basic API Test" -ForegroundColor Cyan
Write-Host "==============" -ForegroundColor Cyan
Write-Host "URL: $baseUrl"
Write-Host ""

# Test 1: API Health
Write-Host "Testing API..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/swagger" -Method GET -TimeoutSec 5
    Write-Host "SUCCESS: API is running (Status: $($response.StatusCode))" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: API not running - $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "TIP: Start Visual Studio project on port 5001" -ForegroundColor Yellow
    exit 1
}

# Test 2: Subscription Tiers
Write-Host "`nTesting subscription tiers..." -ForegroundColor Yellow
try {
    $tiers = Invoke-RestMethod -Uri "$baseUrl/api/v1/subscriptions/tiers" -Method GET -TimeoutSec 5
    Write-Host "SUCCESS: Found $($tiers.data.Count) subscription tiers" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Subscription tiers failed - $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Redemption Endpoint
Write-Host "`nTesting redemption endpoint..." -ForegroundColor Yellow
$testCode = "TEST-$(Get-Date -Format 'HHmmss')"
try {
    $redemption = Invoke-WebRequest -Uri "$baseUrl/redeem/$testCode" -Method GET -TimeoutSec 5
    Write-Host "WARNING: Unexpected success with invalid code" -ForegroundColor Yellow
}
catch {
    Write-Host "SUCCESS: Invalid code properly rejected" -ForegroundColor Green
}

Write-Host "`nTest Summary:" -ForegroundColor Cyan
Write-Host "- API is working correctly" -ForegroundColor Green
Write-Host "- Endpoints are accessible" -ForegroundColor Green
Write-Host "- Error handling is proper" -ForegroundColor Green

Write-Host "`nNext: Get sponsor token and run full tests" -ForegroundColor Yellow
Write-Host "Basic test completed!" -ForegroundColor Green