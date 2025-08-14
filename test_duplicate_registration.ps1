# Test script to verify duplicate email registration error handling
$apiUrl = "https://localhost:7065/api/auth/register"

# Test data - using a likely existing email
$testData = @{
    email = "test@example.com"
    password = "TestPassword123!"
    fullName = "Test User"
    userRole = "Farmer"
} | ConvertTo-Json

Write-Host "🧪 Testing duplicate email registration..." -ForegroundColor Yellow
Write-Host "📧 Testing with email: test@example.com" -ForegroundColor Cyan

try {
    # First, try to register the user (this might succeed or fail depending on if user exists)
    Write-Host "📝 Attempting first registration..." -ForegroundColor Cyan
    $response1 = Invoke-RestMethod -Uri $apiUrl -Method POST -Body $testData -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✅ First registration response:" -ForegroundColor Green
    Write-Host ($response1 | ConvertTo-Json -Depth 3) -ForegroundColor White
}
catch {
    Write-Host "⚠️ First registration failed (user might already exist):" -ForegroundColor Yellow
    $errorResponse = $_.Exception.Response
    if ($errorResponse) {
        $reader = New-Object System.IO.StreamReader($errorResponse.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host $errorBody -ForegroundColor Red
    }
}

Write-Host "`n🔄 Now testing duplicate registration..." -ForegroundColor Yellow

try {
    # Try to register the same user again (this should fail with meaningful error)
    $response2 = Invoke-RestMethod -Uri $apiUrl -Method POST -Body $testData -ContentType "application/json" -SkipCertificateCheck
    Write-Host "❌ UNEXPECTED: Duplicate registration succeeded!" -ForegroundColor Red
    Write-Host ($response2 | ConvertTo-Json -Depth 3) -ForegroundColor White
}
catch {
    Write-Host "✅ Expected: Duplicate registration failed" -ForegroundColor Green
    $errorResponse = $_.Exception.Response
    
    if ($errorResponse) {
        Write-Host "📊 Status Code: $($errorResponse.StatusCode)" -ForegroundColor Cyan
        
        $reader = New-Object System.IO.StreamReader($errorResponse.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host "📋 Error Response:" -ForegroundColor Cyan
        Write-Host $errorBody -ForegroundColor White
        
        # Check if the error message contains our new meaningful message
        if ($errorBody -like "*EmailAlreadyExists*" -or $errorBody -like "*Bu e-posta adresi ile zaten bir hesap mevcut*" -or $errorBody -like "*An account with this email address already exists*") {
            Write-Host "✅ SUCCESS: Meaningful error message is being returned!" -ForegroundColor Green
        }
        else {
            Write-Host "❌ ERROR: Generic error message detected. Expected meaningful email conflict message." -ForegroundColor Red
        }
    }
}

Write-Host "`n🏁 Test completed!" -ForegroundColor Yellow