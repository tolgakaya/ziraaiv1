# Test HTTPS API
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

try {
    Write-Host "Testing HTTPS API on port 5001..." -ForegroundColor Cyan
    
    # Test Swagger UI accessibility
    $response = Invoke-WebRequest -Uri "https://localhost:5001/swagger" -Method GET -TimeoutSec 10
    Write-Host "✅ API is running on https://localhost:5001" -ForegroundColor Green
    Write-Host "   Swagger UI status: $($response.StatusCode)" -ForegroundColor Gray
    
    # Test versioned endpoint
    $apiResponse = Invoke-RestMethod -Uri "https://localhost:5001/api/v1/subscriptions/tiers" -Method GET -TimeoutSec 5
    Write-Host "✅ API versioning works correctly" -ForegroundColor Green
    Write-Host "   Found $($apiResponse.data.Count) subscription tiers" -ForegroundColor Gray
    
    # Test public redemption endpoint
    $redemptionUrl = "https://localhost:5001/redeem/TEST-CODE-123"
    $redemptionResponse = Invoke-WebRequest -Uri $redemptionUrl -Method GET -TimeoutSec 5
    Write-Host "✅ Public redemption endpoint accessible" -ForegroundColor Green
    Write-Host "   URL: $redemptionUrl" -ForegroundColor Gray
    
} catch {
    Write-Host "❌ API test failed: $($_.Exception.Message)" -ForegroundColor Red
}