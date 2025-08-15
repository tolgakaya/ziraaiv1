# Test redemption endpoint via HTTP
$apiUrl = "http://localhost:5000"  # Try HTTP port instead
$testCode = "SPONSOR-20250814-533815"

Write-Host "Testing redemption endpoint via HTTP..." -ForegroundColor Yellow
Write-Host "URL: $apiUrl/redeem/$testCode" -ForegroundColor Gray

try {
    Write-Host "`nTesting JSON API response..." -ForegroundColor Cyan
    $apiResponse = Invoke-RestMethod -Uri "$apiUrl/redeem/$testCode" -Method GET -Headers @{'Accept' = 'application/json'}
    
    Write-Host "[SUCCESS] JSON response received!" -ForegroundColor Green
    Write-Host ($apiResponse | ConvertTo-Json -Depth 3) -ForegroundColor Green
    
} catch {
    Write-Host "[ERROR] JSON API test failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
    }
}

Write-Host "`nTest completed." -ForegroundColor Cyan