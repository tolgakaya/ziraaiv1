# Test sponsorship link sending with valid JWT token
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$apiUrl = "https://localhost:5001"
$jwtToken = "eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjM5IiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6IlRlc3QgU3BvbnNvciIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IlNwb25zb3IiLCJuYmYiOjE3NTUyMDA3NDYsImV4cCI6MTc1NTIwNDM0NiwiaXNzIjoid3d3LnppcmFhaS5jb20iLCJhdWQiOiJ3d3cuemlyYWFpLmNvbSJ9.p7bMoWWtkWlBJjojbcldE-Gs3eUeaviX6_f7mNp8gb8"

Write-Host "Testing sponsorship link sending with valid token..." -ForegroundColor Yellow

# Test data for link sending
$linkData = @{
    recipients = @(
        @{
            code = "TEST-CODE-123"
            name = "Test Farmer"
            phone = "+905551234567"
        }
    )
    channel = "WhatsApp"
    customMessage = "Test message for sponsorship link"
}

# Set up headers with JWT token
$headers = @{
    "Authorization" = "Bearer $jwtToken"
    "Content-Type" = "application/json"
}

Write-Host "Request data:" -ForegroundColor Cyan
Write-Host ($linkData | ConvertTo-Json -Depth 3) -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri "$apiUrl/api/v1/sponsorship/send-link" -Method POST -Body ($linkData | ConvertTo-Json -Depth 3) -Headers $headers
    
    Write-Host "SUCCESS: Sponsorship link endpoint works!" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Cyan
    Write-Host ($response | ConvertTo-Json -Depth 3) -ForegroundColor Green
    
} catch {
    $errorMessage = $_.Exception.Message
    $statusCode = $null
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.Value__
    }
    
    Write-Host "Status Code: $statusCode" -ForegroundColor Yellow
    Write-Host "Error: $errorMessage" -ForegroundColor Red
    
    # Try to get detailed error response
    if ($_.Exception.Response) {
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $errorBody = $reader.ReadToEnd()
            Write-Host "Error body: $errorBody" -ForegroundColor Red
        } catch {
            Write-Host "Could not read error response body" -ForegroundColor Yellow
        }
    }
}