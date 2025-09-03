# Test script for sponsor display info fix
$baseUrl = "https://localhost:5001/api/v1"
$token = ""  # You'll need to set this with a valid token

Write-Host "🧪 Testing Sponsor Display Info Fix" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

# Test cases for screen parameter normalization
$testCases = @(
    @{ screen = "results"; expected = $true; description = "XL tier with 'results' (plural) should work" },
    @{ screen = "result"; expected = $true; description = "XL tier with 'result' (singular) should work" },
    @{ screen = "analyses"; expected = $true; description = "XL tier with 'analyses' (plural) should work" },
    @{ screen = "analysis"; expected = $true; description = "XL tier with 'analysis' (singular) should work" },
    @{ screen = "profiles"; expected = $true; description = "XL tier with 'profiles' (plural) should work" },
    @{ screen = "profile"; expected = $true; description = "XL tier with 'profile' (singular) should work" },
    @{ screen = "start"; expected = $true; description = "XL tier with 'start' should work" },
    @{ screen = "invalid"; expected = $false; description = "XL tier with invalid screen should fail" }
)

$analysisId = 139  # Analysis ID from the user's example

Write-Host "`n📝 Test Results:" -ForegroundColor Yellow
Write-Host "=================" -ForegroundColor Yellow

foreach ($testCase in $testCases) {
    $url = "$baseUrl/sponsorship/display-info/analysis/$analysisId"
    if ($testCase.screen -ne "") {
        $url += "?screen=$($testCase.screen)"
    }
    
    Write-Host "`n🔍 Testing: $($testCase.description)" -ForegroundColor Cyan
    Write-Host "   URL: $url" -ForegroundColor Gray
    
    if ($token -eq "") {
        Write-Host "   ⚠️  Skipped - No token provided" -ForegroundColor Yellow
        continue
    }
    
    try {
        $headers = @{ 
            "Authorization" = "Bearer $token"
            "Content-Type" = "application/json"
        }
        
        $response = Invoke-RestMethod -Uri $url -Method GET -Headers $headers
        
        $canDisplay = $response.data.canDisplay
        $tierName = $response.data.tierName
        $screen = $response.data.screen
        
        if ($canDisplay -eq $testCase.expected) {
            Write-Host "   ✅ PASS - canDisplay: $canDisplay (expected: $($testCase.expected))" -ForegroundColor Green
        } else {
            Write-Host "   ❌ FAIL - canDisplay: $canDisplay (expected: $($testCase.expected))" -ForegroundColor Red
        }
        
        Write-Host "   📊 Tier: $tierName, Screen: $screen" -ForegroundColor Gray
        
        if (!$canDisplay -and $response.data.reason) {
            Write-Host "   📝 Reason: $($response.data.reason)" -ForegroundColor Gray
        }
        
    } catch {
        Write-Host "   ❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n📋 Manual Test Instructions:" -ForegroundColor Yellow
Write-Host "============================" -ForegroundColor Yellow
Write-Host "1. Set your Bearer token in the `$token variable above"
Write-Host "2. Make sure the WebAPI is running on https://localhost:5001"
Write-Host "3. Run this script again to see actual test results"
Write-Host "4. The primary fix validates that 'screen=results' now works for XL tier"

Write-Host "`n🎯 Expected Behavior After Fix:" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host "• screen=results  → normalized to 'result'  → XL tier shows logo ✅"
Write-Host "• screen=analyses → normalized to 'analysis' → XL tier shows logo ✅"
Write-Host "• screen=profiles → normalized to 'profile'  → XL tier shows logo ✅"
Write-Host "• Invalid screens → canDisplay = false       → Logo hidden ❌"

Write-Host "`n💡 Quick Test Command:" -ForegroundColor Blue
Write-Host "=====================" -ForegroundColor Blue
Write-Host "curl -H `"Authorization: Bearer YOUR_TOKEN`" `"$baseUrl/sponsorship/display-info/analysis/139?screen=results`""