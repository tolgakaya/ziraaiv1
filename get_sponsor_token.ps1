# Get Sponsor JWT Token for Testing
# Ignore SSL certificate errors for localhost testing
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$apiUrl = "https://localhost:5001"

# Register a new sponsor user for testing
$sponsorRegisterData = @{
    firstName = "Test"
    lastName = "Sponsor"
    email = "testsponsor@example.com"
    password = "TestPassword123"
    role = "Sponsor"
}

Write-Host "Registering test sponsor user..." -ForegroundColor Yellow

try {
    $registerResponse = Invoke-RestMethod -Uri "$apiUrl/api/v1/auth/register" -Method POST -Body ($sponsorRegisterData | ConvertTo-Json) -ContentType "application/json"
    Write-Host "Registration successful!" -ForegroundColor Green
    Write-Host "User: $($registerResponse.data.email)" -ForegroundColor Cyan
} catch {
    Write-Host "Registration failed (may already exist): $($_.Exception.Message)" -ForegroundColor Yellow
}

# Login to get JWT token
$loginData = @{
    email = "testsponsor@example.com"
    password = "TestPassword123"
}

Write-Host "Attempting login..." -ForegroundColor Yellow

try {
    $loginResponse = Invoke-RestMethod -Uri "$apiUrl/api/v1/auth/login" -Method POST -Body ($loginData | ConvertTo-Json) -ContentType "application/json"
    
    if ($loginResponse.success) {
        $token = $loginResponse.data.accessToken.token
        Write-Host "Login successful!" -ForegroundColor Green
        Write-Host "JWT Token: $token" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Now run the full test:" -ForegroundColor Yellow
        Write-Host ".\test_full_scenario.ps1 -SponsorToken '$token'" -ForegroundColor Cyan
        
        # Save token to file for easy reuse
        $token | Out-File -FilePath "sponsor_token.txt" -Encoding UTF8
        Write-Host "Token saved to sponsor_token.txt" -ForegroundColor Green
    } else {
        Write-Host "Login failed: $($loginResponse.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "Login error: $($_.Exception.Message)" -ForegroundColor Red
}