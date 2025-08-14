# Test bulk purchase endpoint with detailed logging

# API Base URL
$baseUrl = "http://localhost:5001"

# First, register and login a sponsor user
$registerUrl = "$baseUrl/api/auth/register"
$loginUrl = "$baseUrl/api/auth/login"
$purchaseUrl = "$baseUrl/api/sponsorship/purchase-bulk"

Write-Host "=== Testing Sponsorship Purchase Bulk Endpoint ===" -ForegroundColor Yellow

# 1. Register a sponsor user
Write-Host "1. Registering sponsor user..." -ForegroundColor Green
$registerBody = @{
    firstName = "Test"
    lastName = "Sponsor Company"
    email = "sponsor@test.com"
    password = "TestPass123!"
    userRole = "Sponsor"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri $registerUrl -Method POST -Body $registerBody -ContentType "application/json"
    Write-Host "✅ Registration successful: $($registerResponse.message)" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Registration failed (might already exist): $($_.Exception.Message)" -ForegroundColor Yellow
}

# 2. Login to get token
Write-Host "2. Logging in..." -ForegroundColor Green
$loginBody = @{
    email = "sponsor@test.com"
    password = "TestPass123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri $loginUrl -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.data.accessToken.token
    Write-Host "✅ Login successful, token received" -ForegroundColor Green
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 3. Test purchase bulk endpoint
Write-Host "3. Testing purchase bulk endpoint..." -ForegroundColor Green
$purchaseBody = @{
    subscriptionTierId = 2  # Medium tier
    quantity = 10
    totalAmount = 2999.90
    paymentMethod = "CreditCard"
    paymentReference = "TEST_PAYMENT_123"
    companyName = "Test Sponsor Company"
    invoiceAddress = "Test Address 123"
    taxNumber = "1234567890"
    codePrefix = "TEST"
    validityDays = 365
    notes = "Test bulk purchase"
} | ConvertTo-Json

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "Request URL: $purchaseUrl" -ForegroundColor Cyan
Write-Host "Request Body: $purchaseBody" -ForegroundColor Cyan

try {
    $purchaseResponse = Invoke-RestMethod -Uri $purchaseUrl -Method POST -Body $purchaseBody -Headers $headers
    Write-Host "✅ Purchase bulk successful!" -ForegroundColor Green
    Write-Host "Response: $($purchaseResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Green
} catch {
    Write-Host "❌ Purchase bulk failed:" -ForegroundColor Red
    Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    # Try to get detailed error response
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errorDetails = $reader.ReadToEnd()
            Write-Host "Error Details: $errorDetails" -ForegroundColor Red
        } catch {
            Write-Host "Could not read error details" -ForegroundColor Red
        }
    }
}