# Complete test: Create sponsorship code then send link
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$apiUrl = "https://localhost:5001"
$jwtToken = "eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjM5IiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6IlRlc3QgU3BvbnNvciIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IlNwb25zb3IiLCJuYmYiOjE3NTUyMDE1NjAsImV4cCI6MTc1NTIwNTE2MCwiaXNzIjoid3d3LnppcmFhaS5jb20iLCJhdWQiOiJ3d3cuemlyYWFpLmNvbSJ9.pZlzw5BjZDLOg3MzFze8ss-P4cEvDO7LmDp90JPTrms"

$headers = @{
    "Authorization" = "Bearer $jwtToken"
    "Content-Type" = "application/json"
}

Write-Host "STEP 1: Creating a sponsorship code..." -ForegroundColor Yellow

# Create a sponsorship code first
$codeData = @{
    farmerName = "Test Farmer"
    farmerPhone = "+905551234567"
    description = "Test sponsorship code for link testing"
    subscriptionTierId = 2  # Medium tier
}

try {
    $codeResponse = Invoke-RestMethod -Uri "$apiUrl/api/v1/sponsorship/codes" -Method POST -Body ($codeData | ConvertTo-Json) -Headers $headers
    
    if ($codeResponse.success) {
        $sponsorshipCode = $codeResponse.data.code
        Write-Host "SUCCESS: Sponsorship code created: $sponsorshipCode" -ForegroundColor Green
        
        Write-Host "`nSTEP 2: Sending link for the created code..." -ForegroundColor Yellow
        
        # Now send the link for this real code
        $linkData = @{
            recipients = @(
                @{
                    code = $sponsorshipCode
                    name = "Test Farmer"
                    phone = "+905551234567"
                }
            )
            channel = "WhatsApp"
            customMessage = "Your sponsorship code is ready! Click the link to activate your subscription."
        }
        
        $linkResponse = Invoke-RestMethod -Uri "$apiUrl/api/v1/sponsorship/send-link" -Method POST -Body ($linkData | ConvertTo-Json -Depth 3) -Headers $headers
        
        Write-Host "SUCCESS: Link sent successfully!" -ForegroundColor Green
        Write-Host "Response:" -ForegroundColor Cyan
        Write-Host ($linkResponse | ConvertTo-Json -Depth 3) -ForegroundColor Green
        
    } else {
        Write-Host "FAILED: Could not create sponsorship code" -ForegroundColor Red
        Write-Host ($codeResponse | ConvertTo-Json) -ForegroundColor Red
    }
    
} catch {
    $errorMessage = $_.Exception.Message
    $statusCode = $null
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.Value__
    }
    
    Write-Host "ERROR at step 1 or 2:" -ForegroundColor Red
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