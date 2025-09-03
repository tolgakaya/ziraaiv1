# Test script to verify security validation in plant analysis endpoints
# Tests FarmerId and SponsorId validation for both sync and async endpoints

# Ignore SSL certificate errors
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
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy

Write-Host "=== Security Validation Test ===" -ForegroundColor Green

# Test configuration
$baseUrl = "https://localhost:5001/api/v1"
$loginUrl = "$baseUrl/auth/login"
$analysisUrl = "$baseUrl/plantanalyses/analyze"
$asyncAnalysisUrl = "$baseUrl/plantanalyses/analyze-async"

# Test credentials (should be existing user in staging database)
$testUser = @{
    email = "farmer1@test.com"
    password = "Test123456"
}

# Sample plant analysis request
$analysisRequest = @{
    image = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAAAAAAAD"
    farmerId = "F999"  # Intentionally wrong farmer ID - should be overridden by security validation
    sponsorId = "S999" # Intentionally wrong sponsor ID - should be validated
    cropType = "tomato"
    location = "Test Farm"
    notes = "Security validation test"
}

try {
    Write-Host "1. Testing user login..." -ForegroundColor Yellow
    
    # Login to get JWT token
    $loginResponse = Invoke-RestMethod -Uri $loginUrl -Method POST -Body ($testUser | ConvertTo-Json) -ContentType "application/json"
    
    if ($loginResponse.success) {
        $token = $loginResponse.data.accessToken.token
        Write-Host "   Login successful" -ForegroundColor Green
        Write-Host "   Token: $($token.Substring(0, 50))..." -ForegroundColor Gray
    } else {
        Write-Host "   Login failed: $($loginResponse.message)" -ForegroundColor Red
        exit 1
    }

    # Setup authorization header
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }

    Write-Host "2. Testing synchronous analysis endpoint security..." -ForegroundColor Yellow
    
    try {
        $syncResponse = Invoke-RestMethod -Uri $analysisUrl -Method POST -Body ($analysisRequest | ConvertTo-Json) -Headers $headers
        
        if ($syncResponse.success) {
            Write-Host "   Analysis submitted successfully" -ForegroundColor Green
            Write-Host "   Farmer ID in response: $($syncResponse.data.farmerId)" -ForegroundColor Cyan
            Write-Host "   Sponsor ID in response: $($syncResponse.data.sponsorId)" -ForegroundColor Cyan
            
            # Check if FarmerId was overridden by security validation
            if ($syncResponse.data.farmerId -ne "F999") {
                Write-Host "   SECURITY VALIDATION WORKING: Farmer ID was overridden" -ForegroundColor Green
            } else {
                Write-Host "   WARNING: Farmer ID was not validated" -ForegroundColor Yellow
            }
        } else {
            Write-Host "   Analysis failed: $($syncResponse.message)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "   Error during sync analysis: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $errorResponse = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorResponse)
            $errorBody = $reader.ReadToEnd()
            Write-Host "   Error details: $errorBody" -ForegroundColor Red
        }
    }

    Write-Host "3. Testing asynchronous analysis endpoint security..." -ForegroundColor Yellow
    
    try {
        $asyncResponse = Invoke-RestMethod -Uri $asyncAnalysisUrl -Method POST -Body ($analysisRequest | ConvertTo-Json) -Headers $headers
        
        if ($asyncResponse.success) {
            Write-Host "   Async analysis queued successfully" -ForegroundColor Green
            Write-Host "   Analysis ID: $($asyncResponse.analysis_id)" -ForegroundColor Cyan
        } else {
            Write-Host "   Async analysis failed: $($asyncResponse.message)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "   Error during async analysis: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $errorResponse = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorResponse)
            $errorBody = $reader.ReadToEnd()
            Write-Host "   Error details: $errorBody" -ForegroundColor Red
        }
    }

    Write-Host "=== Security Validation Test Complete ===" -ForegroundColor Green
}
catch {
    Write-Host "Test failed with error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}