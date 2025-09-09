# ZiraAI Authentication Test Suite
# Multi-environment, multi-role comprehensive testing
# Version: 1.0.0
# Usage: .\Test-Authentication.ps1 -Environment development|staging|production

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("development", "staging", "production")]
    [string]$Environment = "development",
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose,
    
    [Parameter(Mandatory=$false)]
    [switch]$QuickTest,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".\test-results"
)

# Environment Configuration
$environments = @{
    development = @{
        baseUrl = "https://localhost:5001/api/v1"
        name = "Development"
        color = "Yellow"
    }
    staging = @{
        baseUrl = "https://ziraai-staging.up.railway.app/api/v1"
        name = "Staging"
        color = "Cyan"
    }
    production = @{
        baseUrl = "https://ziraai.up.railway.app/api/v1"
        name = "Production"
        color = "Green"
    }
}

# Test Data Configuration
$testUsers = @{
    newUser = @{
        email = "test_$(Get-Random -Maximum 99999)@ziraai.com"
        password = "Test123!@#"
        fullName = "Test User"
        mobilePhones = "+905551234567"
        citizenId = "12345678901"
    }
    farmer = @{
        email = "farmer_test@ziraai.com"
        password = "Farmer123!@#"
        role = "Farmer"
    }
    sponsor = @{
        email = "sponsor_test@ziraai.com"
        password = "Sponsor123!@#"
        role = "Sponsor"
    }
    admin = @{
        email = "admin@ziraai.com"
        password = "Admin123!@#"
        role = "Admin"
    }
}

# Global variables
$script:testResults = @()
$script:tokens = @{}
$script:testStartTime = Get-Date
$script:currentEnv = $environments[$Environment]

# Helper Functions
function Write-TestHeader {
    param([string]$TestName)
    
    Write-Host "`n" -NoNewline
    Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor DarkGray
    Write-Host "  $TestName" -ForegroundColor White
    Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor DarkGray
}

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Success,
        [string]$Message,
        [object]$Details = $null
    )
    
    $icon = if ($Success) { "✅" } else { "❌" }
    $color = if ($Success) { "Green" } else { "Red" }
    
    Write-Host "$icon " -NoNewline -ForegroundColor $color
    Write-Host "$TestName" -NoNewline -ForegroundColor White
    Write-Host " - " -NoNewline -ForegroundColor DarkGray
    Write-Host $Message -ForegroundColor $color
    
    if ($Verbose -and $Details) {
        Write-Host "   Details: " -NoNewline -ForegroundColor DarkGray
        Write-Host ($Details | ConvertTo-Json -Compress) -ForegroundColor DarkGray
    }
    
    # Record result
    $script:testResults += [PSCustomObject]@{
        Timestamp = Get-Date
        Environment = $script:currentEnv.name
        TestName = $TestName
        Success = $Success
        Message = $Message
        Details = $Details
        Duration = ((Get-Date) - $script:testStartTime).TotalSeconds
    }
}

function Invoke-ApiRequest {
    param(
        [string]$Endpoint,
        [string]$Method = "GET",
        [object]$Body = $null,
        [string]$Token = $null
    )
    
    $uri = "$($script:currentEnv.baseUrl)/$Endpoint"
    
    $headers = @{
        "Content-Type" = "application/json"
        "Accept" = "application/json"
    }
    
    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }
    
    $params = @{
        Uri = $uri
        Method = $Method
        Headers = $headers
        UseBasicParsing = $true
        ErrorAction = "Stop"
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
        $statusCode = $_.Exception.Response.StatusCode.value__
        $errorBody = $_.ErrorDetails.Message
        
        return @{
            Success = $false
            Error = $_.Exception.Message
            StatusCode = $statusCode
            ErrorBody = $errorBody
        }
    }
}

# Test Functions
function Test-Registration {
    Write-TestHeader "User Registration Tests"
    
    # Test 1: Valid Registration
    $newUser = $testUsers.newUser
    $result = Invoke-ApiRequest -Endpoint "auth/register" -Method "POST" -Body $newUser
    
    if ($result.Success) {
        Write-TestResult -TestName "New User Registration" -Success $true -Message "User registered successfully" -Details $newUser.email
        $script:testUsers.newUser.registered = $true
    }
    else {
        Write-TestResult -TestName "New User Registration" -Success $false -Message "Registration failed: $($result.Error)" -Details $result.ErrorBody
    }
    
    # Test 2: Duplicate Registration
    $result = Invoke-ApiRequest -Endpoint "auth/register" -Method "POST" -Body $newUser
    $expectedFail = -not $result.Success
    Write-TestResult -TestName "Duplicate Registration Prevention" -Success $expectedFail -Message "Duplicate registration correctly blocked"
    
    # Test 3: Invalid Email Format
    $invalidUser = $newUser.Clone()
    $invalidUser.email = "invalid-email"
    $result = Invoke-ApiRequest -Endpoint "auth/register" -Method "POST" -Body $invalidUser
    $expectedFail = -not $result.Success
    Write-TestResult -TestName "Invalid Email Validation" -Success $expectedFail -Message "Invalid email correctly rejected"
    
    # Test 4: Weak Password
    $weakPasswordUser = $newUser.Clone()
    $weakPasswordUser.email = "weak_$(Get-Random)@test.com"
    $weakPasswordUser.password = "123"
    $result = Invoke-ApiRequest -Endpoint "auth/register" -Method "POST" -Body $weakPasswordUser
    $expectedFail = -not $result.Success
    Write-TestResult -TestName "Weak Password Validation" -Success $expectedFail -Message "Weak password correctly rejected"
}

function Test-Login {
    Write-TestHeader "User Login Tests"
    
    # Test 1: Valid Login with registered user
    if ($script:testUsers.newUser.registered) {
        $loginData = @{
            email = $script:testUsers.newUser.email
            password = $script:testUsers.newUser.password
        }
        
        $result = Invoke-ApiRequest -Endpoint "auth/login" -Method "POST" -Body $loginData
        
        if ($result.Success -and $result.Data.data.token) {
            Write-TestResult -TestName "Valid User Login" -Success $true -Message "Login successful"
            $script:tokens.newUser = $result.Data.data.token
            $script:tokens.refreshToken = $result.Data.data.refreshToken
        }
        else {
            Write-TestResult -TestName "Valid User Login" -Success $false -Message "Login failed: $($result.Error)"
        }
    }
    
    # Test 2: Invalid Password
    $invalidLogin = @{
        email = $script:testUsers.newUser.email
        password = "WrongPassword123!"
    }
    $result = Invoke-ApiRequest -Endpoint "auth/login" -Method "POST" -Body $invalidLogin
    $expectedFail = -not $result.Success
    Write-TestResult -TestName "Invalid Password Login" -Success $expectedFail -Message "Invalid password correctly rejected"
    
    # Test 3: Non-existent User
    $nonExistentLogin = @{
        email = "nonexistent@ziraai.com"
        password = "Test123!@#"
    }
    $result = Invoke-ApiRequest -Endpoint "auth/login" -Method "POST" -Body $nonExistentLogin
    $expectedFail = -not $result.Success
    Write-TestResult -TestName "Non-existent User Login" -Success $expectedFail -Message "Non-existent user correctly rejected"
    
    # Test 4: Empty Credentials
    $emptyLogin = @{
        email = ""
        password = ""
    }
    $result = Invoke-ApiRequest -Endpoint "auth/login" -Method "POST" -Body $emptyLogin
    $expectedFail = -not $result.Success
    Write-TestResult -TestName "Empty Credentials Login" -Success $expectedFail -Message "Empty credentials correctly rejected"
}

function Test-TokenRefresh {
    Write-TestHeader "Token Refresh Tests"
    
    if ($script:tokens.refreshToken) {
        # Test 1: Valid Token Refresh
        $refreshData = @{
            refreshToken = $script:tokens.refreshToken
        }
        
        $result = Invoke-ApiRequest -Endpoint "auth/refresh-token" -Method "POST" -Body $refreshData
        
        if ($result.Success -and $result.Data.data.token) {
            Write-TestResult -TestName "Valid Token Refresh" -Success $true -Message "Token refreshed successfully"
            $script:tokens.newUserRefreshed = $result.Data.data.token
        }
        else {
            Write-TestResult -TestName "Valid Token Refresh" -Success $false -Message "Token refresh failed: $($result.Error)"
        }
    }
    
    # Test 2: Invalid Refresh Token
    $invalidRefresh = @{
        refreshToken = "invalid.refresh.token"
    }
    $result = Invoke-ApiRequest -Endpoint "auth/refresh-token" -Method "POST" -Body $invalidRefresh
    $expectedFail = -not $result.Success
    Write-TestResult -TestName "Invalid Refresh Token" -Success $expectedFail -Message "Invalid refresh token correctly rejected"
}

function Test-PasswordRecovery {
    Write-TestHeader "Password Recovery Tests"
    
    # Test 1: Valid Forgot Password Request
    $forgotPasswordData = @{
        email = $script:testUsers.newUser.email
        citizenId = $script:testUsers.newUser.citizenId
    }
    
    $result = Invoke-ApiRequest -Endpoint "auth/forgot-password" -Method "PUT" -Body $forgotPasswordData
    
    if ($result.Success) {
        Write-TestResult -TestName "Forgot Password Request" -Success $true -Message "Password reset initiated"
    }
    else {
        Write-TestResult -TestName "Forgot Password Request" -Success $false -Message "Password reset failed: $($result.Error)"
    }
    
    # Test 2: Invalid Email for Password Recovery
    $invalidForgot = @{
        email = "nonexistent@test.com"
        citizenId = "12345678901"
    }
    $result = Invoke-ApiRequest -Endpoint "auth/forgot-password" -Method "PUT" -Body $invalidForgot
    $expectedFail = -not $result.Success
    Write-TestResult -TestName "Invalid Email Password Recovery" -Success $expectedFail -Message "Invalid email correctly handled"
}

function Test-AuthorizedEndpoints {
    Write-TestHeader "Authorization Tests"
    
    if ($script:tokens.newUser) {
        # Test 1: Authorized Access with Valid Token
        $result = Invoke-ApiRequest -Endpoint "auth/test" -Method "POST" -Token $script:tokens.newUser
        
        if ($result.Success) {
            Write-TestResult -TestName "Authorized Access with Token" -Success $true -Message "Token validation successful"
        }
        else {
            Write-TestResult -TestName "Authorized Access with Token" -Success $false -Message "Token validation failed"
        }
        
        # Test 2: Change Password with Valid Token
        $changePasswordData = @{
            oldPassword = $script:testUsers.newUser.password
            newPassword = "NewTest123!@#"
        }
        
        $result = Invoke-ApiRequest -Endpoint "auth/user-password" -Method "PUT" -Body $changePasswordData -Token $script:tokens.newUser
        
        if ($result.Success) {
            Write-TestResult -TestName "Change Password" -Success $true -Message "Password changed successfully"
            $script:testUsers.newUser.password = $changePasswordData.newPassword
        }
        else {
            Write-TestResult -TestName "Change Password" -Success $false -Message "Password change failed: $($result.Error)"
        }
    }
    
    # Test 3: Unauthorized Access without Token
    $result = Invoke-ApiRequest -Endpoint "auth/test" -Method "POST"
    $expectedFail = -not $result.Success
    Write-TestResult -TestName "Unauthorized Access Prevention" -Success $expectedFail -Message "Unauthorized access correctly blocked"
    
    # Test 4: Invalid Token Access
    $result = Invoke-ApiRequest -Endpoint "auth/test" -Method "POST" -Token "invalid.token.here"
    $expectedFail = -not $result.Success
    Write-TestResult -TestName "Invalid Token Access Prevention" -Success $expectedFail -Message "Invalid token correctly rejected"
}

function Test-RoleBasedAccess {
    Write-TestHeader "Role-Based Access Tests"
    
    # Note: These tests assume pre-created test users for each role
    # In production, you would create these users first or use existing test accounts
    
    $roles = @("farmer", "sponsor", "admin")
    
    foreach ($role in $roles) {
        $user = $testUsers.$role
        
        # Try to login with role-specific user
        $loginData = @{
            email = $user.email
            password = $user.password
        }
        
        $result = Invoke-ApiRequest -Endpoint "auth/login" -Method "POST" -Body $loginData
        
        if ($result.Success -and $result.Data.data.token) {
            Write-TestResult -TestName "$($user.role) Role Login" -Success $true -Message "Login successful for $($user.role)"
            $script:tokens.$role = $result.Data.data.token
            
            # Test role-specific endpoints based on role
            Test-RoleSpecificEndpoints -Role $user.role -Token $result.Data.data.token
        }
        else {
            Write-TestResult -TestName "$($user.role) Role Login" -Success $false -Message "Login failed for $($user.role) (user may not exist in this environment)"
        }
    }
}

function Test-RoleSpecificEndpoints {
    param(
        [string]$Role,
        [string]$Token
    )
    
    switch ($Role) {
        "Farmer" {
            # Test farmer-specific endpoints
            $result = Invoke-ApiRequest -Endpoint "plantanalyses" -Method "GET" -Token $Token
            $success = $result.Success -or $result.StatusCode -eq 200
            Write-TestResult -TestName "Farmer: Access Plant Analyses" -Success $success -Message "Plant analysis access tested"
            
            $result = Invoke-ApiRequest -Endpoint "subscriptions/usage-status" -Method "GET" -Token $Token
            $success = $result.Success -or $result.StatusCode -eq 200
            Write-TestResult -TestName "Farmer: Check Usage Status" -Success $success -Message "Usage status access tested"
        }
        
        "Sponsor" {
            # Test sponsor-specific endpoints
            $result = Invoke-ApiRequest -Endpoint "sponsorships/sponsored-analyses" -Method "GET" -Token $Token
            $success = $result.Success -or $result.StatusCode -eq 200
            Write-TestResult -TestName "Sponsor: Access Sponsored Analyses" -Success $success -Message "Sponsored analyses access tested"
            
            $result = Invoke-ApiRequest -Endpoint "sponsorships/packages" -Method "GET" -Token $Token
            $success = $result.Success -or $result.StatusCode -eq 200
            Write-TestResult -TestName "Sponsor: View Packages" -Success $success -Message "Package viewing tested"
        }
        
        "Admin" {
            # Test admin-specific endpoints
            $result = Invoke-ApiRequest -Endpoint "users" -Method "GET" -Token $Token
            $success = $result.Success -or $result.StatusCode -eq 200
            Write-TestResult -TestName "Admin: Access User Management" -Success $success -Message "User management access tested"
            
            $result = Invoke-ApiRequest -Endpoint "configurations" -Method "GET" -Token $Token
            $success = $result.Success -or $result.StatusCode -eq 200
            Write-TestResult -TestName "Admin: Access Configurations" -Success $success -Message "Configuration access tested"
        }
    }
}

function Test-MobileVerification {
    Write-TestHeader "Mobile Verification Tests"
    
    # Test 1: Valid Mobile Verification Request
    $verifyData = @{
        citizenId = $script:testUsers.newUser.citizenId
        mobilePhones = $script:testUsers.newUser.mobilePhones
    }
    
    $result = Invoke-ApiRequest -Endpoint "auth/verify" -Method "POST" -Body $verifyData
    
    if ($result.Success) {
        Write-TestResult -TestName "Mobile Verification Request" -Success $true -Message "Verification code sent"
    }
    else {
        Write-TestResult -TestName "Mobile Verification Request" -Success $false -Message "Verification failed: $($result.Error)"
    }
    
    # Test 2: Invalid Phone Number
    $invalidVerify = @{
        citizenId = "12345678901"
        mobilePhones = "invalid"
    }
    $result = Invoke-ApiRequest -Endpoint "auth/verify" -Method "POST" -Body $invalidVerify
    $expectedFail = -not $result.Success
    Write-TestResult -TestName "Invalid Phone Verification" -Success $expectedFail -Message "Invalid phone correctly rejected"
}

function Test-SecurityFeatures {
    Write-TestHeader "Security Feature Tests"
    
    # Test 1: SQL Injection Prevention
    $sqlInjection = @{
        email = "test' OR '1'='1"
        password = "'; DROP TABLE users; --"
    }
    $result = Invoke-ApiRequest -Endpoint "auth/login" -Method "POST" -Body $sqlInjection
    $expectedFail = -not $result.Success
    Write-TestResult -TestName "SQL Injection Prevention" -Success $expectedFail -Message "SQL injection attempt blocked"
    
    # Test 2: XSS Prevention
    $xssAttempt = @{
        email = "<script>alert('XSS')</script>@test.com"
        password = "Test123!@#"
        fullName = "<script>alert('XSS')</script>"
    }
    $result = Invoke-ApiRequest -Endpoint "auth/register" -Method "POST" -Body $xssAttempt
    $expectedFail = -not $result.Success
    Write-TestResult -TestName "XSS Prevention" -Success $expectedFail -Message "XSS attempt blocked"
    
    # Test 3: Rate Limiting (if implemented)
    $rateLimitSuccess = $true
    for ($i = 1; $i -le 10; $i++) {
        $result = Invoke-ApiRequest -Endpoint "auth/login" -Method "POST" -Body @{email="test@test.com"; password="wrong"}
        if ($result.StatusCode -eq 429) {
            Write-TestResult -TestName "Rate Limiting" -Success $true -Message "Rate limiting active after $i attempts"
            $rateLimitSuccess = $true
            break
        }
    }
    if ($i -eq 11) {
        Write-TestResult -TestName "Rate Limiting" -Success $false -Message "No rate limiting detected (may not be implemented)"
    }
}

function Generate-TestReport {
    Write-Host "`n" -NoNewline
    Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Blue
    Write-Host "                         TEST REPORT SUMMARY                        " -ForegroundColor White
    Write-Host "═══════════════════════════════════════════════════════════════════" -ForegroundColor Blue
    
    $totalTests = $script:testResults.Count
    $passedTests = ($script:testResults | Where-Object { $_.Success }).Count
    $failedTests = $totalTests - $passedTests
    $passRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 2) } else { 0 }
    $totalDuration = ((Get-Date) - $script:testStartTime).TotalSeconds
    
    Write-Host "`nEnvironment: " -NoNewline -ForegroundColor Gray
    Write-Host $script:currentEnv.name -ForegroundColor $script:currentEnv.color
    
    Write-Host "Base URL: " -NoNewline -ForegroundColor Gray
    Write-Host $script:currentEnv.baseUrl -ForegroundColor White
    
    Write-Host "Test Duration: " -NoNewline -ForegroundColor Gray
    Write-Host "$([math]::Round($totalDuration, 2)) seconds" -ForegroundColor White
    
    Write-Host "`nTest Results:" -ForegroundColor Yellow
    Write-Host "  Total Tests: " -NoNewline -ForegroundColor Gray
    Write-Host $totalTests -ForegroundColor White
    
    Write-Host "  Passed: " -NoNewline -ForegroundColor Gray
    Write-Host $passedTests -ForegroundColor Green
    
    Write-Host "  Failed: " -NoNewline -ForegroundColor Gray
    Write-Host $failedTests -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })
    
    Write-Host "  Pass Rate: " -NoNewline -ForegroundColor Gray
    $passRateColor = if ($passRate -ge 80) { "Green" } elseif ($passRate -ge 60) { "Yellow" } else { "Red" }
    Write-Host "$passRate%" -ForegroundColor $passRateColor
    
    # Create output directory if it doesn't exist
    if (-not (Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    }
    
    # Generate detailed report files
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $reportBaseName = "$OutputPath\TestReport_$($Environment)_$timestamp"
    
    # JSON Report
    $jsonReport = @{
        Environment = $script:currentEnv.name
        BaseUrl = $script:currentEnv.baseUrl
        TestDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Duration = $totalDuration
        Summary = @{
            TotalTests = $totalTests
            Passed = $passedTests
            Failed = $failedTests
            PassRate = $passRate
        }
        Results = $script:testResults
    }
    
    $jsonReport | ConvertTo-Json -Depth 10 | Out-File "$reportBaseName.json"
    Write-Host "`nJSON Report saved to: " -NoNewline -ForegroundColor Gray
    Write-Host "$reportBaseName.json" -ForegroundColor Cyan
    
    # HTML Report
    $htmlContent = @"
<!DOCTYPE html>
<html>
<head>
    <title>ZiraAI Authentication Test Report - $($script:currentEnv.name)</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .header { background-color: #333; color: white; padding: 20px; border-radius: 5px; }
        .summary { background-color: white; padding: 20px; margin: 20px 0; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .test-results { background-color: white; padding: 20px; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        table { width: 100%; border-collapse: collapse; }
        th { background-color: #4CAF50; color: white; padding: 12px; text-align: left; }
        td { padding: 10px; border-bottom: 1px solid #ddd; }
        .pass { color: green; font-weight: bold; }
        .fail { color: red; font-weight: bold; }
        .metric { display: inline-block; margin: 10px 20px; }
        .metric-value { font-size: 24px; font-weight: bold; }
        .metric-label { color: #666; }
    </style>
</head>
<body>
    <div class="header">
        <h1>ZiraAI Authentication Test Report</h1>
        <p>Environment: $($script:currentEnv.name) | Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")</p>
    </div>
    
    <div class="summary">
        <h2>Test Summary</h2>
        <div class="metric">
            <div class="metric-value">$totalTests</div>
            <div class="metric-label">Total Tests</div>
        </div>
        <div class="metric">
            <div class="metric-value" style="color: green;">$passedTests</div>
            <div class="metric-label">Passed</div>
        </div>
        <div class="metric">
            <div class="metric-value" style="color: red;">$failedTests</div>
            <div class="metric-label">Failed</div>
        </div>
        <div class="metric">
            <div class="metric-value">$passRate%</div>
            <div class="metric-label">Pass Rate</div>
        </div>
        <div class="metric">
            <div class="metric-value">$([math]::Round($totalDuration, 2))s</div>
            <div class="metric-label">Duration</div>
        </div>
    </div>
    
    <div class="test-results">
        <h2>Detailed Results</h2>
        <table>
            <tr>
                <th>Test Name</th>
                <th>Result</th>
                <th>Message</th>
                <th>Duration</th>
            </tr>
"@
    
    foreach ($result in $script:testResults) {
        $resultClass = if ($result.Success) { "pass" } else { "fail" }
        $resultText = if ($result.Success) { "PASS" } else { "FAIL" }
        $htmlContent += @"
            <tr>
                <td>$($result.TestName)</td>
                <td class="$resultClass">$resultText</td>
                <td>$($result.Message)</td>
                <td>$([math]::Round($result.Duration, 2))s</td>
            </tr>
"@
    }
    
    $htmlContent += @"
        </table>
    </div>
</body>
</html>
"@
    
    $htmlContent | Out-File "$reportBaseName.html"
    Write-Host "HTML Report saved to: " -NoNewline -ForegroundColor Gray
    Write-Host "$reportBaseName.html" -ForegroundColor Cyan
    
    # CSV Report for Excel
    $script:testResults | Export-Csv -Path "$reportBaseName.csv" -NoTypeInformation
    Write-Host "CSV Report saved to: " -NoNewline -ForegroundColor Gray
    Write-Host "$reportBaseName.csv" -ForegroundColor Cyan
    
    # Display failed tests if any
    if ($failedTests -gt 0) {
        Write-Host "`nFailed Tests:" -ForegroundColor Red
        $script:testResults | Where-Object { -not $_.Success } | ForEach-Object {
            Write-Host "  - $($_.TestName): $($_.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host "`n═══════════════════════════════════════════════════════════════════" -ForegroundColor Blue
}

# Main Execution
function Start-TestSuite {
    Clear-Host
    
    Write-Host "╔═══════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║           ZiraAI Authentication Test Suite v1.0.0                 ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    Write-Host "`nStarting tests for: " -NoNewline -ForegroundColor Gray
    Write-Host $script:currentEnv.name -ForegroundColor $script:currentEnv.color -NoNewline
    Write-Host " environment" -ForegroundColor Gray
    
    Write-Host "Base URL: " -NoNewline -ForegroundColor Gray
    Write-Host $script:currentEnv.baseUrl -ForegroundColor White
    
    Write-Host "Test Mode: " -NoNewline -ForegroundColor Gray
    if ($QuickTest) {
        Write-Host "Quick Test (Essential tests only)" -ForegroundColor Yellow
    }
    else {
        Write-Host "Full Test Suite" -ForegroundColor Green
    }
    
    Write-Host "`nPress any key to start testing..." -ForegroundColor DarkGray
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
    
    # Run test suites
    Test-Registration
    Test-Login
    Test-TokenRefresh
    Test-PasswordRecovery
    Test-AuthorizedEndpoints
    Test-MobileVerification
    
    if (-not $QuickTest) {
        Test-RoleBasedAccess
        Test-SecurityFeatures
    }
    
    # Generate report
    Generate-TestReport
    
    Write-Host "`nTest suite completed!" -ForegroundColor Green
    Write-Host "Press any key to exit..." -ForegroundColor DarkGray
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
}

# Run the test suite
Start-TestSuite
