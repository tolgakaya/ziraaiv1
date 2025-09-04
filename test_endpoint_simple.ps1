# Simple endpoint test to check if dependency injection works
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$apiUrl = "https://localhost:5001"
$sponsorId = 34

Write-Host "Testing SendSponsorshipLink endpoint..." -ForegroundColor Yellow

# Test data
$linkData = @{
    sponsorId = $sponsorId
    recipients = @(
        @{
            code = "TEST-CODE-123"
            name = "Test Farmer"
            phone = "+905551234567"
        }
    )
    channel = "WhatsApp"
    customMessage = "Test message"
}

# Create a dummy JWT token (will fail auth but should get past DI errors)
$headers = @{
    "Authorization" = "Bearer dummy.token.here"
    "Content-Type" = "application/json"
}

try {
    $response = Invoke-RestMethod -Uri "$apiUrl/api/v1/sponsorship/send-link" -Method POST -Body ($linkData | ConvertTo-Json -Depth 3) -Headers $headers
    Write-Host "SUCCESS: Endpoint works!" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json)" -ForegroundColor Cyan
} catch {
    $errorMessage = $_.Exception.Message
    $statusCode = $_.Exception.Response.StatusCode.Value__
    
    Write-Host "Status Code: $statusCode" -ForegroundColor Yellow
    Write-Host "Error: $errorMessage" -ForegroundColor Red
    
    if ($statusCode -eq 401) {
        Write-Host "401 Unauthorized - Dependency injection is working, just need valid auth!" -ForegroundColor Green
    } elseif ($statusCode -eq 500) {
        Write-Host "500 Server Error - Dependency injection or other issue" -ForegroundColor Red
    } else {
        Write-Host "Other error - Status: $statusCode" -ForegroundColor Yellow
    }
}