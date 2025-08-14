# Test Bulk Purchase Endpoint - Simple test
param(
    [Parameter(Mandatory=$true)]
    [string]$SponsorToken
)

[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$baseUrl = "https://localhost:5001"
$timestamp = Get-Date -Format "HHmmss"

Write-Host "Testing Bulk Purchase Endpoint" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan

# Simple bulk purchase request
$bulkData = @{
    subscriptionTierId = 2
    quantity = 1
    totalAmount = 99.99
    paymentMethod = "Test"
    paymentReference = "TEST-$timestamp"
    companyName = "Test Company"
    invoiceAddress = "Test Address"
    taxNumber = "1234567890"
    codePrefix = "TEST"
    validityDays = 30
    notes = "Simple endpoint test"
}

$headers = @{
    'Authorization' = "Bearer $SponsorToken"
    'Content-Type' = 'application/json'
}

try {
    Write-Host "Sending bulk purchase request..." -ForegroundColor Yellow
    Write-Host "Endpoint: $baseUrl/api/v1/sponsorship/purchase-bulk" -ForegroundColor Gray
    Write-Host "Quantity: 1" -ForegroundColor Gray
    Write-Host "Amount: 99.99 TL" -ForegroundColor Gray
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/sponsorship/purchase-bulk" -Method POST -Body ($bulkData | ConvertTo-Json) -Headers $headers
    
    Write-Host "[SUCCESS] Bulk purchase completed!" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Gray
    $response | ConvertTo-Json -Depth 5 | Write-Host
    
    if ($response.data) {
        Write-Host "`nParsed Data:" -ForegroundColor Yellow
        Write-Host "Purchase ID: $($response.data.id)" -ForegroundColor Gray
        
        if ($response.data.generatedCodes) {
            Write-Host "Generated Codes: $($response.data.generatedCodes.Count)" -ForegroundColor Gray
            foreach ($code in $response.data.generatedCodes) {
                Write-Host "  Code: $($code.code)" -ForegroundColor Green
                Write-Host "  ID: $($code.id)" -ForegroundColor Gray
            }
        } else {
            Write-Host "No generated codes in response" -ForegroundColor Yellow
        }
    }
}
catch {
    Write-Host "[ERROR] Bulk purchase failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
        
        try {
            $errorStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorStream)
            $errorBody = $reader.ReadToEnd()
            Write-Host "Error Body: $errorBody" -ForegroundColor Red
        } catch {}
    }
}

Write-Host "`nBulk endpoint test completed!" -ForegroundColor Green