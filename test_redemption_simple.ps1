# Simple redemption endpoint test
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$apiUrl = "https://localhost:5001"
$testCode = "SPONSOR-20250814-533815"

Write-Host "Testing redemption endpoint..." -ForegroundColor Yellow
Write-Host "URL: $apiUrl/redeem/$testCode" -ForegroundColor Gray

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

Write-Host "`nTrying HTML response..." -ForegroundColor Cyan
try {
    # Test HTML response  
    $htmlHeaders = @{
        'Accept' = 'text/html,application/xhtml+xml'
        'User-Agent' = 'Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X)'
    }
    
    $htmlResponse = Invoke-WebRequest -Uri "$apiUrl/redeem/$testCode" -Headers $htmlHeaders -Method GET
    
    Write-Host "[SUCCESS] HTML response received!" -ForegroundColor Green
    Write-Host "Status: $($htmlResponse.StatusCode)" -ForegroundColor Gray
    Write-Host "Content-Type: $($htmlResponse.Headers['Content-Type'])" -ForegroundColor Gray
    Write-Host "Content Length: $($htmlResponse.Content.Length) chars" -ForegroundColor Gray
    
} catch {
    Write-Host "[ERROR] HTML test failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nTest completed." -ForegroundColor Cyan