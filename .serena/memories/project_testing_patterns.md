# ZiraAI Testing Patterns and Standards

## Authentication Testing Framework

### Test Organization
```
/scripts/
├── Test-Authentication-Simple.ps1  # Main test suite
├── Run-Tests.ps1                  # Test orchestrator
├── test-config.json               # Configuration
├── TEST_SUITE_README.md          # Documentation
└── test-results/                 # Output directory
```

### Environment Management
- Use parameter-based environment switching
- Store environment configs in hashtables
- Support for development, staging, production
- SSL certificate handling for local development

### API Request Pattern
```powershell
function Invoke-ApiRequest {
    param(
        [string]$Endpoint,
        [string]$Method,
        [object]$Body,
        [string]$Token
    )
    # Standard headers
    # Token authentication
    # Error handling with status codes
    # Return success/failure with data
}
```

### Test Result Tracking
- Store results in script-scoped array
- Track test name, result (PASS/FAIL), details
- Calculate metrics: total, passed, failed, pass rate
- Generate timestamped reports

### Report Generation
- JSON for programmatic access
- HTML for visual review
- CSV for Excel analysis
- Automatic directory creation
- Timestamped filenames

### PowerShell Best Practices
- Use single quotes to avoid encoding issues
- Avoid backticks in strings
- Handle self-signed certificates for dev
- Use -ErrorAction for error control
- Implement proper parameter validation

### Test Categories
1. **Registration Tests**
   - Valid registration
   - Duplicate prevention
   - Email validation
   - Password strength

2. **Login Tests**
   - Valid credentials
   - Invalid password
   - Non-existent user
   - Empty credentials

3. **Token Management**
   - Token refresh
   - Token expiry
   - Invalid token
   - Token decoding

4. **Security Tests**
   - SQL injection
   - XSS prevention
   - Rate limiting
   - Unauthorized access

5. **Role-Based Tests**
   - Admin access
   - Farmer access
   - Sponsor access
   - Cross-role prevention

### Known Issues
- UTF-8 encoding in PowerShell scripts
- Special characters in strings
- Railway staging timeout (30s limit)
- Development SSL certificates

### Testing Commands
```powershell
# Development
.\scripts\Run-Tests.ps1

# Staging
.\scripts\Run-Tests.ps1 -Environment staging

# All environments
.\scripts\Run-Tests.ps1 -Environment all

# Quick smoke test
.\scripts\Run-Tests.ps1 -Quick
```