# Test sponsor registration after database schema fix

# Wait a moment for API to fully start
Start-Sleep -Seconds 3

# Test sponsor registration
$registerUrl = "http://localhost:5001/api/auth/register"
$registerBody = @{
    firstName = "Test"
    lastName = "Sponsor Company"
    email = "testsponsor@example.com"
    password = "TestPass123!"
    userRole = "Sponsor"
} | ConvertTo-Json

Write-Host "Testing sponsor registration..."
Write-Host "URL: $registerUrl"
Write-Host "Body: $registerBody"

try {
    $response = Invoke-RestMethod -Uri $registerUrl -Method POST -Body $registerBody -ContentType "application/json"
    Write-Host "✅ Registration successful!" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 3)"
} catch {
    Write-Host "❌ Registration failed:" -ForegroundColor Red
    Write-Host "Status: $($_.Exception.Response.StatusCode)"
    Write-Host "Error: $($_.Exception.Message)"
    
    # Try to get detailed error message
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorDetails = $reader.ReadToEnd()
        Write-Host "Details: $errorDetails"
    }
}