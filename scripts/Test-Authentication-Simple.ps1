# ZiraAI Simple Authentication Test
# A simpler version without special characters

param(
    [string]$Environment = 'development'
)

# Environment Configuration
$environments = @{
    development = @{
        baseUrl = 'https://localhost:5001/api/v1'
        name = 'Development'
    }
    staging = @{
        baseUrl = 'https://ziraai-staging.up.railway.app/api/v1'
        name = 'Staging'
    }
    production = @{
        baseUrl = 'https://ziraai.up.railway.app/api/v1'
        name = 'Production'
    }
}

$script:currentEnv = $environments[$Environment]
$script:testResults = @()

Write-Host '=====================================================================' -ForegroundColor Cyan
Write-Host "   ZiraAI Authentication Test - $($script:currentEnv.name)" -ForegroundColor White
Write-Host '=====================================================================' -ForegroundColor Cyan

function Invoke-ApiRequest {
    param(
        [string]$Endpoint,
        [string]$Method = 'GET',
        [object]$Body = $null,
        [string]$Token = $null
    )
    
    $uri = "$($script:currentEnv.baseUrl)/$Endpoint"
    
    $headers = @{
        'Content-Type' = 'application/json'
        'Accept' = 'application/json'
    }
    
    if ($Token) {
        $headers['Authorization'] = "Bearer $Token"
    }
    
    # Allow self-signed certificates for development
    if ($Environment -eq 'development') {
        add-type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(
        ServicePoint srvPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) {
        return true;
    }
}
"@ -ErrorAction SilentlyContinue
        [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    }
    
    $params = @{
        Uri = $uri
        Method = $Method
        Headers = $headers
        UseBasicParsing = $true
        ErrorAction = 'Stop'
    }
    
    if ($Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
    }
    
    try {
        $response = Invoke-RestMethod @params
        return @{
            Success = $true
            Data = $response
            StatusCode = 200
        }
    }
    catch {
        $statusCode = 0
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode.value__
        }
        
        return @{
            Success = $false
            Error = $_.Exception.Message
            StatusCode = $statusCode
        }
    }
}

Write-Host ''
Write-Host 'Running Authentication Tests...' -ForegroundColor Yellow
Write-Host ''

# Test 1: Registration
Write-Host '[TEST] User Registration' -ForegroundColor Cyan

$randomNum = Get-Random -Maximum 99999
$newUser = @{
    email = "test_$randomNum@ziraai.com"
    password = 'Test123!@#'
    fullName = 'Test User'
    mobilePhones = '+905551234567'
    citizenId = '12345678901'
}

$result = Invoke-ApiRequest -Endpoint 'auth/register' -Method 'POST' -Body $newUser

if ($result.Success) {
    Write-Host '  [PASS] User registered successfully' -ForegroundColor Green
    $script:testResults += @{Test='Registration'; Result='PASS'}
}
else {
    Write-Host "  [FAIL] Registration failed: $($result.Error)" -ForegroundColor Red
    $script:testResults += @{Test='Registration'; Result='FAIL'}
}

# Test 2: Login
Write-Host '[TEST] User Login' -ForegroundColor Cyan

$loginData = @{
    email = $newUser.email
    password = $newUser.password
}

$result = Invoke-ApiRequest -Endpoint 'auth/login' -Method 'POST' -Body $loginData

if ($result.Success -and $result.Data.data.token) {
    Write-Host '  [PASS] Login successful' -ForegroundColor Green
    $script:testResults += @{Test='Login'; Result='PASS'}
    $token = $result.Data.data.token
    $refreshToken = $result.Data.data.refreshToken
}
else {
    Write-Host "  [FAIL] Login failed: $($result.Error)" -ForegroundColor Red
    $script:testResults += @{Test='Login'; Result='FAIL'}
}

# Test 3: Invalid Login
Write-Host '[TEST] Invalid Password Login' -ForegroundColor Cyan

$invalidLogin = @{
    email = $newUser.email
    password = 'WrongPassword'
}

$result = Invoke-ApiRequest -Endpoint 'auth/login' -Method 'POST' -Body $invalidLogin

if (-not $result.Success) {
    Write-Host '  [PASS] Invalid login correctly rejected' -ForegroundColor Green
    $script:testResults += @{Test='Invalid Login'; Result='PASS'}
}
else {
    Write-Host '  [FAIL] Invalid login was not rejected' -ForegroundColor Red
    $script:testResults += @{Test='Invalid Login'; Result='FAIL'}
}

# Test 4: Token Refresh
if ($refreshToken) {
    Write-Host '[TEST] Token Refresh' -ForegroundColor Cyan
    
    $refreshData = @{
        refreshToken = $refreshToken
    }
    
    $result = Invoke-ApiRequest -Endpoint 'auth/refresh-token' -Method 'POST' -Body $refreshData
    
    if ($result.Success -and $result.Data.data.token) {
        Write-Host '  [PASS] Token refreshed successfully' -ForegroundColor Green
        $script:testResults += @{Test='Token Refresh'; Result='PASS'}
    }
    else {
        Write-Host "  [FAIL] Token refresh failed: $($result.Error)" -ForegroundColor Red
        $script:testResults += @{Test='Token Refresh'; Result='FAIL'}
    }
}

# Test 5: Authorized Access
if ($token) {
    Write-Host '[TEST] Authorized Access' -ForegroundColor Cyan
    
    $result = Invoke-ApiRequest -Endpoint 'auth/test' -Method 'POST' -Token $token
    
    if ($result.Success) {
        Write-Host '  [PASS] Authorized access successful' -ForegroundColor Green
        $script:testResults += @{Test='Authorized Access'; Result='PASS'}
    }
    else {
        Write-Host "  [FAIL] Authorized access failed: $($result.Error)" -ForegroundColor Red
        $script:testResults += @{Test='Authorized Access'; Result='FAIL'}
    }
}

# Test 6: Unauthorized Access
Write-Host '[TEST] Unauthorized Access Prevention' -ForegroundColor Cyan

$result = Invoke-ApiRequest -Endpoint 'auth/test' -Method 'POST'

if (-not $result.Success) {
    Write-Host '  [PASS] Unauthorized access correctly blocked' -ForegroundColor Green
    $script:testResults += @{Test='Unauthorized Access'; Result='PASS'}
}
else {
    Write-Host '  [FAIL] Unauthorized access was not blocked' -ForegroundColor Red
    $script:testResults += @{Test='Unauthorized Access'; Result='FAIL'}
}

# Test 7: Forgot Password
Write-Host '[TEST] Forgot Password' -ForegroundColor Cyan

$forgotData = @{
    email = $newUser.email
    citizenId = $newUser.citizenId
}

$result = Invoke-ApiRequest -Endpoint 'auth/forgot-password' -Method 'PUT' -Body $forgotData

if ($result.Success) {
    Write-Host '  [PASS] Password reset initiated' -ForegroundColor Green
    $script:testResults += @{Test='Forgot Password'; Result='PASS'}
}
else {
    Write-Host "  [FAIL] Password reset failed: $($result.Error)" -ForegroundColor Red
    $script:testResults += @{Test='Forgot Password'; Result='FAIL'}
}

# Generate Summary Report
Write-Host ''
Write-Host '=====================================================================' -ForegroundColor Blue
Write-Host '                         TEST SUMMARY                               ' -ForegroundColor White
Write-Host '=====================================================================' -ForegroundColor Blue

$totalTests = $script:testResults.Count
$passedTests = ($script:testResults | Where-Object { $_.Result -eq 'PASS' }).Count
$failedTests = $totalTests - $passedTests
$passRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 2) } else { 0 }

Write-Host ''
Write-Host "Environment: $($script:currentEnv.name)" -ForegroundColor Cyan
Write-Host "Base URL: $($script:currentEnv.baseUrl)" -ForegroundColor Gray
Write-Host ''
Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { 'Red' } else { 'Green' })
Write-Host "Pass Rate: $passRate%" -ForegroundColor $(if ($passRate -ge 80) { 'Green' } elseif ($passRate -ge 60) { 'Yellow' } else { 'Red' })

if ($failedTests -gt 0) {
    Write-Host ''
    Write-Host 'Failed Tests:' -ForegroundColor Red
    $script:testResults | Where-Object { $_.Result -eq 'FAIL' } | ForEach-Object {
        Write-Host "  - $($_.Test)" -ForegroundColor Red
    }
}

Write-Host ''
Write-Host '=====================================================================' -ForegroundColor Blue

# Save results to file
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$outputDir = Join-Path $PSScriptRoot 'test-results'

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$resultFile = Join-Path $outputDir "TestResults_$($Environment)_$timestamp.json"

$reportData = @{
    Environment = $script:currentEnv.name
    BaseUrl = $script:currentEnv.baseUrl
    TestDate = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    TotalTests = $totalTests
    Passed = $passedTests
    Failed = $failedTests
    PassRate = $passRate
    Results = $script:testResults
}

$reportData | ConvertTo-Json -Depth 10 | Out-File $resultFile

Write-Host ''
Write-Host "Results saved to: $resultFile" -ForegroundColor Cyan
Write-Host ''
Write-Host 'Test completed!' -ForegroundColor Green