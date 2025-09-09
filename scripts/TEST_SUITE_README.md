# ZiraAI Authentication Test Suite

Comprehensive authentication testing framework for all environments and user roles.

## 🚀 Quick Start

### Run tests for current environment (development)
```powershell
.\Run-Tests.ps1
```

### Run tests for specific environment
```powershell
.\Run-Tests.ps1 -Environment staging
.\Run-Tests.ps1 -Environment production
```

### Run tests for ALL environments
```powershell
.\Run-Tests.ps1 -Environment all
```

### Quick test mode (essential tests only)
```powershell
.\Run-Tests.ps1 -Quick
```

### Run and open report
```powershell
.\Run-Tests.ps1 -OpenReport
```

## 📋 Test Coverage

### Authentication Flows
- ✅ User Registration
- ✅ Login (email/password)
- ✅ Token Refresh
- ✅ Password Recovery
- ✅ Password Change
- ✅ Mobile Verification

### Security Tests
- ✅ SQL Injection Prevention
- ✅ XSS Attack Prevention
- ✅ Invalid Token Handling
- ✅ Rate Limiting Check
- ✅ Unauthorized Access Prevention

### Role-Based Testing
- ✅ Admin Role Access
- ✅ Farmer Role Access
- ✅ Sponsor Role Access
- ✅ Cross-role Access Prevention

### Validation Tests
- ✅ Email Format Validation
- ✅ Password Strength Requirements
- ✅ Required Field Validation
- ✅ Duplicate User Prevention

## 📊 Test Reports

Reports are automatically generated in multiple formats:

- **HTML Report**: Visual report with charts and tables
- **JSON Report**: Machine-readable detailed results
- **CSV Report**: Excel-compatible data export

Reports are saved to: `./test-results/`

### Report Structure
```
test-results/
├── TestReport_development_20250109_143022.html
├── TestReport_development_20250109_143022.json
└── TestReport_development_20250109_143022.csv
```

## 🔧 Configuration

Edit `test-config.json` to customize:
- Environment URLs
- Test user credentials
- Test scenarios to run
- Report output settings

### Environment Configuration
```json
{
  "environments": {
    "development": {
      "baseUrl": "https://localhost:5001/api/v1"
    },
    "staging": {
      "baseUrl": "https://ziraai-staging.up.railway.app/api/v1"
    },
    "production": {
      "baseUrl": "https://ziraai.up.railway.app/api/v1"
    }
  }
}
```

## 📈 Test Metrics

The test suite tracks:
- **Total Tests Run**: Complete count of all test scenarios
- **Pass/Fail Rate**: Percentage of successful tests
- **Execution Time**: Duration of each test and total suite
- **Response Times**: API response performance metrics
- **Error Details**: Comprehensive error logging

## 🎯 Usage Examples

### Development Testing
```powershell
# Full test suite for development
.\Test-Authentication.ps1 -Environment development -Verbose

# Quick smoke test
.\Run-Tests.ps1 -Environment development -Quick
```

### Staging Validation
```powershell
# Run before deploying to production
.\Test-Authentication.ps1 -Environment staging
```

### Production Monitoring
```powershell
# Light touch production validation
.\Test-Authentication.ps1 -Environment production -QuickTest
```

### CI/CD Integration
```powershell
# For automated pipelines
powershell -ExecutionPolicy Bypass -File Test-Authentication.ps1 -Environment staging
```

## 🔍 Interpreting Results

### Success Indicators
- ✅ Green checkmarks indicate passed tests
- Response times under 5 seconds
- No security vulnerabilities detected

### Failure Analysis
- ❌ Red X marks indicate failed tests
- Check error details in verbose mode
- Review JSON report for full error context

### Common Issues
1. **Connection Refused**: Check if API is running
2. **401 Unauthorized**: Verify test user credentials
3. **Timeout**: Network or performance issue
4. **Rate Limiting**: Too many requests (expected for security test)

## 📝 Test Scenarios

### Essential Tests (Quick Mode)
1. Basic Registration
2. Standard Login
3. Token Refresh
4. Password Recovery
5. Authorized Access

### Full Test Suite
Includes all essential tests plus:
- Complete validation matrix
- All role combinations
- Security attack simulations
- Performance benchmarks
- Edge case scenarios

## 🛠️ Troubleshooting

### PowerShell Execution Policy
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### SSL Certificate Issues (Development)
```powershell
# Bypass SSL for local testing
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
```

### Missing Dependencies
The test suite is self-contained and requires only:
- PowerShell 5.1 or higher
- Internet connection for staging/production tests

## 📧 Contact

For issues or questions about the test suite, please check:
- Test reports in `./test-results/`
- Error logs in verbose mode (`-Verbose`)
- Configuration in `test-config.json`

## 🔄 Updates

Last Updated: January 2025
Version: 1.0.0
Compatible with: ZiraAI API v1