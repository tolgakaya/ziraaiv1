# Test redemption with new code
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$apiUrl = "https://localhost:5001"
$testCode = "SPONSOR-20250814-141747"

Write-Host "Testing redemption with NEW code..." -ForegroundColor Yellow
Write-Host "Code: $testCode" -ForegroundColor Cyan
Write-Host "URL: $apiUrl/redeem/$testCode" -ForegroundColor Gray

try {
    $apiResponse = Invoke-RestMethod -Uri "$apiUrl/redeem/$testCode" -Method GET -Headers @{'Accept' = 'application/json'}
    
    Write-Host "[SUCCESS] Redemption successful!" -ForegroundColor Green
    Write-Host ($apiResponse | ConvertTo-Json -Depth 3) -ForegroundColor Green
    
} catch {
    Write-Host "[ERROR] Redemption failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
        
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response body:" -ForegroundColor Yellow
            Write-Host $responseBody -ForegroundColor Red
        } catch {
            Write-Host "Could not read response body" -ForegroundColor Yellow
        }
    }
}

Write-Host "`nTest completed." -ForegroundColor Cyan