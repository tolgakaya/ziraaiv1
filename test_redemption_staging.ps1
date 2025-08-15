# Test redemption endpoint in Staging environment with HTTPS
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$apiUrl = "https://localhost:5001"
$testCode = "SPONSOR-20250814-533815"

Write-Host "Testing redemption endpoint in STAGING environment..." -ForegroundColor Yellow
Write-Host "URL: $apiUrl/redeem/$testCode" -ForegroundColor Gray
Write-Host "Environment: STAGING" -ForegroundColor Cyan
Write-Host "Protocol: HTTPS" -ForegroundColor Cyan

try {
    # Test JSON API response
    $apiHeaders = @{
        'Accept' = 'application/json'
        'Content-Type' = 'application/json'
        'User-Agent' = 'ZiraAI-Test-Client/1.0'
    }
    
    Write-Host "`nTesting JSON API response..." -ForegroundColor Cyan
    $apiResponse = Invoke-RestMethod -Uri "$apiUrl/redeem/$testCode" -Method GET -Headers $apiHeaders
    
    Write-Host "[SUCCESS] JSON response received!" -ForegroundColor Green
    Write-Host ($apiResponse | ConvertTo-Json -Depth 3) -ForegroundColor Green
    
} catch {
    Write-Host "[ERROR] JSON API test failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
        
        # Try to get response body
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response body: $responseBody" -ForegroundColor Red
        } catch {
            Write-Host "Could not read response body" -ForegroundColor Yellow
        }
    }
}

Write-Host "`nTest completed." -ForegroundColor Cyan