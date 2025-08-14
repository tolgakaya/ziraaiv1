# Test Sponsorship Link Distribution System
# PowerShell script for testing on port 5001

$baseUrl = "https://localhost:5001"
$apiVersion = "v1"

Write-Host "Testing Sponsorship Link Distribution System" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

# Step 1: Test if API is running
Write-Host ""
Write-Host "Step 1: Testing API Health" -ForegroundColor Yellow

try {
    # Test with Swagger endpoint which should always be available
    $healthResponse = Invoke-WebRequest -Uri "$baseUrl/swagger" -Method GET -TimeoutSec 5
    Write-Host "API is running on port 5001" -ForegroundColor Green
    Write-Host "   Swagger UI is accessible" -ForegroundColor Gray
}
catch {
    Write-Host "API is not responding on port 5001. Please ensure it's running." -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Test public redemption endpoint
Write-Host ""
Write-Host "Step 2: Testing Public Redemption Endpoint" -ForegroundColor Yellow

$testCode = "TEST-CODE-123"
$redemptionUrl = "$baseUrl/redeem/$testCode"

try {
    $response = Invoke-WebRequest -Uri $redemptionUrl -Method GET
    if ($response.StatusCode -eq 200) {
        Write-Host "Redemption endpoint is accessible" -ForegroundColor Green
        Write-Host "URL: $redemptionUrl" -ForegroundColor Gray
    }
}
catch {
    # This is expected if code doesn't exist
    Write-Host "Redemption endpoint returned error (expected for invalid code)" -ForegroundColor Yellow
    Write-Host "URL: $redemptionUrl" -ForegroundColor Gray
}

# Step 3: Test versioned API endpoints
Write-Host ""
Write-Host "Step 3: Testing API Versioning" -ForegroundColor Yellow

# Test with existing subscription tiers endpoint
$versionedUrl = "$baseUrl/api/$apiVersion/subscriptions/tiers"
Write-Host "Testing: $versionedUrl" -ForegroundColor Gray

try {
    $tiersResponse = Invoke-RestMethod -Uri $versionedUrl -Method GET
    Write-Host "API versioning is working correctly" -ForegroundColor Green
    if ($tiersResponse.data) {
        Write-Host "Found $($tiersResponse.data.Count) subscription tiers" -ForegroundColor Gray
    }
}
catch {
    Write-Host "API versioning test failed" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Test completed!" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Cyan