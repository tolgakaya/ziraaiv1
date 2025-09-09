# ZiraAI Test Runner
# Quick launcher for authentication tests

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('development', 'staging', 'production', 'all')]
    [string]$Environment = 'development',
    
    [switch]$Quick,
    [switch]$OpenReport
)

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$testScript = Join-Path $scriptPath 'Test-Authentication-Simple.ps1'

Write-Host '=====================================================================' -ForegroundColor Cyan
Write-Host '                   ZiraAI Test Runner                              ' -ForegroundColor Cyan
Write-Host '=====================================================================' -ForegroundColor Cyan

if ($Environment -eq 'all') {
    Write-Host ''
    Write-Host 'Running tests for ALL environments...' -ForegroundColor Yellow
    
    $environments = @('development', 'staging', 'production')
    $results = @()
    
    foreach ($env in $environments) {
        Write-Host ''
        Write-Host "Testing $env environment..." -ForegroundColor Cyan
        
        $params = @{
            Environment = $env
        }
        
        if ($Quick) {
            $params.QuickTest = $true
        }
        
        & $testScript @params
        
        $results += [PSCustomObject]@{
            Environment = $env
            Completed = $true
            Timestamp = Get-Date
        }
    }
    
    Write-Host ''
    Write-Host '=====================================================================' -ForegroundColor Green
    Write-Host 'All environment tests completed!' -ForegroundColor Green
    Write-Host '=====================================================================' -ForegroundColor Green
    
    foreach ($result in $results) {
        $timeStr = $result.Timestamp.ToString('HH:mm:ss')
        Write-Host "OK $($result.Environment) - Completed at $timeStr" -ForegroundColor Green
    }
}
else {
    Write-Host ''
    Write-Host "Running tests for $Environment environment..." -ForegroundColor Yellow
    
    $params = @{
        Environment = $Environment
    }
    
    if ($Quick) {
        $params.QuickTest = $true
        Write-Host 'Mode: Quick Test (Essential tests only)' -ForegroundColor Yellow
    }
    else {
        Write-Host 'Mode: Full Test Suite' -ForegroundColor Green
    }
    
    & $testScript @params
}

if ($OpenReport) {
    $reportPath = Join-Path $scriptPath 'test-results'
    $latestReport = Get-ChildItem -Path $reportPath -Filter '*.html' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($latestReport) {
        Write-Host ''
        Write-Host 'Opening latest report in browser...' -ForegroundColor Cyan
        Start-Process $latestReport.FullName
    }
    else {
        Write-Host ''
        Write-Host "No reports found in $reportPath" -ForegroundColor Yellow
    }
}

Write-Host ''
Write-Host 'Test runner completed!' -ForegroundColor Green