# Dealer Code Distribution System - Comprehensive Test Script
# Test Date: 2025-10-26

$baseUrl = "https://ziraai-api-sit.up.railway.app/api/v1"
$version = "1.0"

# Tokens
$mainToken = "eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjE1OSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJVc2VyIDExMTQiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOlsiRmFybWVyIiwiU3BvbnNvciJdLCJuYmYiOjE3NjE0Nzc3MDAsImV4cCI6MTc2MTQ4MTMwMCwiaXNzIjoiWmlyYUFJX1N0YWdpbmciLCJhdWQiOiJaaXJhQUlfU3RhZ2luZ19Vc2VycyJ9.wWFvvRG9AdPspLLkfubXlOxa17pNVXMajriAIjyiVQE"
$dealerToken = "eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjE1OCIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJVc2VyIDExMTMiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOlsiRmFybWVyIiwiU3BvbnNvciJdLCJuYmYiOjE3NjE0Nzc3MTQsImV4cCI6MTc2MTQ4MTMxNCwiaXNzIjoiWmlyYUFJX1N0YWdpbmciLCJhdWQiOiJaaXJhQUlfU3RhZ2luZ19Vc2VycyJ9.mOts8R7Cs1S9vdBhe7EfP4On7GJCM8C2Y0HisBwqzoQ"

$headers = @{
    "Authorization" = "Bearer $mainToken"
    "x-dev-arch-version" = $version
    "Content-Type" = "application/json"
}

$reportFile = "claudedocs/Dealers/TEST_RESULTS.md"

# Initialize report
"# Dealer Endpoints - Test Results`n" | Out-File $reportFile
"**Test Date**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n" | Out-File $reportFile -Append
"**Environment**: Staging`n`n---`n" | Out-File $reportFile -Append

Write-Host "Starting Dealer Endpoints Tests..." -ForegroundColor Green

# Test 1: Search Dealer by Email
Write-Host "`nTest 1: Search Dealer by Email" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/sponsorship/dealer/search?email=test@example.com" -Method Get -Headers $headers
    "`n## Test 1: Search Dealer by Email`n" | Out-File $reportFile -Append
    "**Endpoint**: GET /sponsorship/dealer/search`n" | Out-File $reportFile -Append
    "**Request**:" | Out-File $reportFile -Append
    "``````" | Out-File $reportFile -Append
    "GET $baseUrl/sponsorship/dealer/search?email=test@example.com" | Out-File $reportFile -Append
    "``````" | Out-File $reportFile -Append
    "**Response**:" | Out-File $reportFile -Append
    "``````json" | Out-File $reportFile -Append
    $response | ConvertTo-Json -Depth 10 | Out-File $reportFile -Append
    "``````" | Out-File $reportFile -Append
    Write-Host "✓ Test 1 Passed" -ForegroundColor Green
} catch {
    Write-Host "✗ Test 1 Failed: $_" -ForegroundColor Red
    "**Error**: $($_.Exception.Message)" | Out-File $reportFile -Append
}

# Test 2: Get Dealer Summary
Write-Host "`nTest 2: Get Dealer Summary" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/sponsorship/dealer/summary" -Method Get -Headers $headers
    "`n## Test 2: Get Dealer Summary`n" | Out-File $reportFile -Append
    "**Endpoint**: GET /sponsorship/dealer/summary`n" | Out-File $reportFile -Append
    "**Response**:" | Out-File $reportFile -Append
    "``````json" | Out-File $reportFile -Append
    $response | ConvertTo-Json -Depth 10 | Out-File $reportFile -Append
    "``````" | Out-File $reportFile -Append
    Write-Host "✓ Test 2 Passed" -ForegroundColor Green
} catch {
    Write-Host "✗ Test 2 Failed: $_" -ForegroundColor Red
}

# Test 3: Get Dealer Invitations
Write-Host "`nTest 3: Get Dealer Invitations" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/sponsorship/dealer/invitations?status=Pending" -Method Get -Headers $headers
    "`n## Test 3: Get Dealer Invitations`n" | Out-File $reportFile -Append
    "**Endpoint**: GET /sponsorship/dealer/invitations`n" | Out-File $reportFile -Append
    "**Response**:" | Out-File $reportFile -Append
    "``````json" | Out-File $reportFile -Append
    $response | ConvertTo-Json -Depth 10 | Out-File $reportFile -Append
    "``````" | Out-File $reportFile -Append
    Write-Host "✓ Test 3 Passed" -ForegroundColor Green
} catch {
    Write-Host "✗ Test 3 Failed: $_" -ForegroundColor Red
}

# Test 4: Get Dealer Analytics
Write-Host "`nTest 4: Get Dealer Analytics" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/sponsorship/dealer/analytics/158" -Method Get -Headers $headers
    "`n## Test 4: Get Dealer Analytics`n" | Out-File $reportFile -Append
    "**Endpoint**: GET /sponsorship/dealer/analytics/{dealerId}`n" | Out-File $reportFile -Append
    "**Response**:" | Out-File $reportFile -Append
    "``````json" | Out-File $reportFile -Append
    $response | ConvertTo-Json -Depth 10 | Out-File $reportFile -Append
    "``````" | Out-File $reportFile -Append
    Write-Host "✓ Test 4 Passed" -ForegroundColor Green
} catch {
    Write-Host "✗ Test 4 Failed: $_" -ForegroundColor Red
}

Write-Host "`n`nAll tests completed. Report saved to: $reportFile" -ForegroundColor Green
