# Sponsorship Link Distribution System - Testing Workflow

## Test Environment Setup

### Prerequisites
- API running on `https://localhost:5001`
- PostgreSQL database with applied migrations
- Postman collection imported
- Valid sponsor JWT token

### Environment Variables
```bash
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=https://localhost:5001
export ConnectionStrings__DArchPgContext="Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass"
```

## Testing Workflow - Step by Step

### Phase 1: Environment Verification

#### Step 1.1: API Health Check
```bash
# PowerShell
./test_https_api.ps1
```

**Expected Result:**
```
✅ API is running on https://localhost:5001
   Swagger UI status: 200
✅ API versioning works correctly
   Found 5 subscription tiers
✅ Public redemption endpoint accessible
```

#### Step 1.2: Database Connection
```bash
# C# Script
dotnet script check_migration_status.csx
```

**Expected Result:**
```
✅ Connected to staging database
📝 Applied migrations: 11 migrations found
🔗 Link-related columns: All 9 columns present
```

### Phase 2: Authentication Setup

#### Step 2.1: Get Sponsor JWT Token
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "sponsor@test.com",
  "password": "Test123!"
}
```

#### Step 2.2: Verify Token in Postman
1. Open Postman collection
2. Set `{{token}}` variable with JWT token
3. Set `{{baseUrl}}` to `https://localhost:5001`

### Phase 3: Core Functionality Testing

#### Step 3.1: Create Sponsorship Codes
**Postman Request:** `Create Sponsorship Code`

```json
{
  "farmerName": "Test Çiftçi 1",
  "farmerPhone": "5551234567",
  "amount": 100.00,
  "description": "Test desteği",
  "expiryDate": "2025-12-31T23:59:59"
}
```

**Validation Checklist:**
- [ ] Response status: 200 OK
- [ ] Code generated (format: SPONSOR-2025-XXXXXX)
- [ ] Database record created
- [ ] All fields correctly saved

#### Step 3.2: Send Distribution Links
**Postman Request:** `📱 Send Sponsorship Links (SMS/WhatsApp)`

```json
{
  "codes": [
    {
      "code": "SPONSOR-2025-ABC123",
      "recipientName": "Test Çiftçi 1",
      "recipientPhone": "5551234567"
    }
  ],
  "sendVia": "WhatsApp",
  "customMessage": "Test mesajı - kodunuz hazır!"
}
```

**Validation Checklist:**
- [ ] Response status: 200 OK
- [ ] Link generated correctly
- [ ] Database updated with send info
- [ ] Recipient details saved

#### Step 3.3: Test Public Redemption (HTML)
**Manual Browser Test:**
1. Copy redemption link from previous response
2. Open in browser: `https://localhost:5001/redeem/SPONSOR-2025-ABC123`
3. Verify HTML page loads
4. Check auto-login functionality

**Validation Checklist:**
- [ ] HTML page renders correctly
- [ ] User details displayed
- [ ] JWT token stored in localStorage
- [ ] Account created in database
- [ ] Code marked as redeemed

#### Step 3.4: Test API Redemption (JSON)
**Postman Request:** `🔗 Test Versioned Public Redemption`

```http
GET /api/v1/redeem/SPONSOR-2025-XYZ789
Accept: application/json
```

**Validation Checklist:**
- [ ] JSON response returned
- [ ] User object contains correct data
- [ ] JWT tokens provided
- [ ] Temporary password included
- [ ] Account creation confirmed

### Phase 4: Analytics and Statistics

#### Step 4.1: Check Link Statistics
**Postman Request:** `📊 Get Link Distribution Statistics`

```http
GET /api/v1/sponsorship/link-statistics?sponsorUserId=123
Authorization: Bearer {{token}}
```

**Validation Checklist:**
- [ ] Statistics returned correctly
- [ ] Click count updated
- [ ] Redemption status accurate
- [ ] Analytics data complete

### Phase 5: Error Handling Testing

#### Step 5.1: Invalid Code Test
```http
GET /redeem/INVALID-CODE-123
```

**Expected Result:**
```json
{
  "success": false,
  "message": "Sponsorluk kodu bulunamadı veya geçersiz",
  "errorCode": "INVALID_CODE"
}
```

#### Step 5.2: Expired Code Test
1. Create code with past expiry date
2. Attempt redemption
3. Verify error response

#### Step 5.3: Already Redeemed Test
1. Use previously redeemed code
2. Attempt redemption again
3. Verify appropriate error

### Phase 6: Performance Testing

#### Step 6.1: Bulk Link Sending
```json
{
  "codes": [
    {"code": "CODE1", "recipientName": "User 1", "recipientPhone": "5551111111"},
    {"code": "CODE2", "recipientName": "User 2", "recipientPhone": "5552222222"},
    // ... up to 100 codes
  ],
  "sendVia": "SMS"
}
```

**Performance Metrics:**
- [ ] Response time < 5 seconds for 100 codes
- [ ] All links generated successfully
- [ ] Database performance acceptable

#### Step 6.2: Concurrent Redemption Test
1. Create multiple redemption links
2. Access simultaneously from different browsers
3. Verify system stability

### Phase 7: Security Testing

#### Step 7.1: Authorization Tests
1. Test endpoints without JWT token
2. Test with invalid/expired token
3. Test with wrong role (Farmer trying Sponsor endpoints)

#### Step 7.2: Rate Limiting Test
1. Make multiple rapid requests to redemption endpoint
2. Verify rate limiting activates
3. Check error response format

#### Step 7.3: Input Validation
1. Test with malformed phone numbers
2. Test with special characters in names
3. Test with extreme amounts (negative, very large)

### Phase 8: Integration Testing

#### Step 8.1: End-to-End Workflow
1. **Sponsor Journey:**
   - Create account
   - Generate codes
   - Send links
   - Monitor statistics

2. **Farmer Journey:**
   - Receive SMS/WhatsApp
   - Click link
   - Account created automatically
   - Access platform features

3. **System Journey:**
   - Track all interactions
   - Update analytics
   - Maintain data integrity

#### Step 8.2: Cross-Platform Testing
- [ ] Test on mobile devices
- [ ] Test on different browsers
- [ ] Test with different SMS providers
- [ ] Test WhatsApp integration

## Automated Testing Scripts

### PowerShell Test Suite
```powershell
# Complete system test
./test_link_system.ps1

# Expected output:
# Step 1: Testing API Health ✅
# Step 2: Testing Public Redemption Endpoint ✅  
# Step 3: Testing API Versioning ✅
# Test completed!
```

### Database Verification Script
```bash
# Check database state
dotnet script check_migration_status.csx

# Verify sponsorship data
dotnet script verify_sponsorship_data.csx
```

## Test Data Management

### Test Data Setup
```sql
-- Create test sponsor user
INSERT INTO "Users" ("FullName", "Email", "MobilePhones", "Status", "RecordDate")
VALUES ('Test Sponsor', 'sponsor@test.com', '5559999999', true, NOW());

-- Create test subscription for sponsor
INSERT INTO "UserSubscriptions" ("UserId", "SubscriptionTierId", "StartDate", "EndDate", "IsActive")
VALUES (1, 2, NOW(), NOW() + INTERVAL '30 days', true);
```

### Test Data Cleanup
```sql
-- Clean test data after testing
DELETE FROM "SponsorshipCodes" WHERE "Code" LIKE 'TEST-%';
DELETE FROM "Users" WHERE "Email" LIKE '%test.com';
DELETE FROM "SubscriptionUsageLogs" WHERE "UserId" IN (SELECT "Id" FROM "Users" WHERE "Email" LIKE '%test.com');
```

## Test Results Documentation

### Test Report Template
```markdown
## Test Execution Report - [Date]

### Environment
- API Version: v1.0
- Database: PostgreSQL
- Test Environment: Development

### Test Results Summary
- Total Tests: 45
- Passed: 43 ✅
- Failed: 2 ❌
- Skipped: 0 ⏭️

### Failed Tests
1. **Bulk SMS Sending (100+ codes)**
   - Issue: Timeout after 30 seconds
   - Resolution: Implement background job processing

2. **WhatsApp Integration**
   - Issue: Provider API key missing
   - Resolution: Configure WhatsApp Business API

### Performance Metrics
- Average Response Time: 245ms
- Peak Memory Usage: 156MB
- Database Query Time: <50ms
- Concurrent Users Supported: 100+

### Recommendations
1. Implement background processing for bulk operations
2. Add more comprehensive error logging
3. Consider caching for frequently accessed data
```

## Continuous Testing

### GitHub Actions Workflow
```yaml
name: Sponsorship Link System Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:13
        env:
          POSTGRES_DB: ziraai_test
          POSTGRES_USER: test
          POSTGRES_PASSWORD: test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 9.0.x
    
    - name: Run Tests
      run: |
        dotnet test ./Tests/
        dotnet script test_sponsorship_system.csx
```

### Local Development Testing
```bash
# Quick smoke test
npm run test:sponsorship

# Full integration test
npm run test:integration

# Performance test
npm run test:performance
```

## Troubleshooting Guide

### Common Issues

#### 1. API Not Starting
**Symptoms:** Connection refused, timeout errors
**Solutions:**
- Check HTTPS certificate
- Verify port 5001 is available
- Check environment variables

#### 2. Database Connection Issues
**Symptoms:** Database operation failures
**Solutions:**
- Verify connection string
- Check PostgreSQL service status
- Run migration sync script

#### 3. JWT Token Issues
**Symptoms:** 401 Unauthorized responses
**Solutions:**
- Check token expiry
- Verify token format
- Refresh authentication

#### 4. SMS/WhatsApp Delivery Issues
**Symptoms:** Links not sent, delivery failures
**Solutions:**
- Check provider API credentials
- Verify phone number formats
- Review rate limiting settings

### Debug Mode Testing
```bash
# Enable debug logging
export Logging__LogLevel__Default=Debug

# Run with debug output
dotnet run --project WebAPI --configuration Debug
```

This comprehensive testing workflow ensures the Sponsorship Link Distribution System works correctly across all scenarios and environments.