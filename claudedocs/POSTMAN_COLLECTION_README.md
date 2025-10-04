# ZiraAI Complete Postman Collection

Comprehensive Postman Collection v2.1 for testing all ZiraAI API endpoints.

## Overview

- **Total Controllers**: 23
- **Total Endpoints**: 162
- **Version**: Generated from Swagger on 2025-10-03
- **Collection File**: `ZiraAI_Complete_Postman_Collection.json`

## Features

### 1. Complete Endpoint Coverage

All API endpoints from the Swagger documentation are included:

#### Authentication (13 endpoints)
- Login as Farmer/Sponsor/Admin (duplicate scenarios)
- Standard email/password login
- Phone-based OTP authentication
- Token refresh
- User registration
- Password management

#### Core Features
- **PlantAnalyses** (9 endpoints): Plant disease detection and analysis
- **Subscriptions** (8 endpoints): Subscription tier management
- **Sponsorship** (23 endpoints): Complete sponsor/farmer relationship management
- **Referral** (8 endpoints): Referral code generation and tracking
- **Notifications** (11 endpoints): Real-time notification system
- **BulkOperations** (8 endpoints): Bulk code generation and link sending

#### Smart Links & Deep Links
- **DeepLinks** (7 endpoints): Mobile app deep linking
- **SmartLinks**: Product recommendations and analytics

#### User & Permission Management
- **Users** (6 endpoints): User CRUD operations
- **Groups** (6 endpoints): User group management
- **GroupClaims** (6 endpoints): Group permission management
- **UserClaims** (6 endpoints): User permission management
- **UserGroups** (8 endpoints): User-group relationships
- **OperationClaims** (5 endpoints): Operation permission management

#### Configuration & Localization
- **Languages** (7 endpoints): Language management
- **Translates** (7 endpoints): Translation key management

#### System & Testing
- **Health** (2 endpoints): System health checks
- **Test** (3 endpoints): Test endpoints
- **TestDatabase** (4 endpoints): Database testing
- **Logs** (1 endpoint): System logging

## Environment Variables

The collection includes 10 pre-configured variables:

| Variable | Default Value | Purpose |
|----------|--------------|---------|
| `base_url` | `https://localhost:5001` | API base URL |
| `version` | `1` | API version number |
| `access_token` | _empty_ | JWT access token (auto-filled after login) |
| `refresh_token` | _empty_ | JWT refresh token (auto-filled after login) |
| `user_id` | _empty_ | Current user ID (auto-filled after login) |
| `referral_code` | _empty_ | Referral code for testing |
| `plant_analysis_id` | _empty_ | Plant analysis ID for testing |
| `subscription_id` | _empty_ | Subscription ID for testing |
| `sponsor_id` | _empty_ | Sponsor ID for testing |
| `smart_link_id` | _empty_ | Smart link ID for testing |

## Quick Start

### 1. Import Collection

1. Open Postman
2. Click **Import** button
3. Select `ZiraAI_Complete_Postman_Collection.json`
4. Collection will appear in your workspace

### 2. Configure Environment

Update collection variables for your environment:

```
base_url: https://localhost:5001  (for local development)
         OR
         https://api.ziraai.com     (for production)

version: 1                         (API version)
```

### 3. Authentication Flow

Execute requests in this order:

#### Option A: Email/Password Authentication
```
1. Auth → Login as Farmer (or Sponsor/Admin)
   - Automatically saves access_token and refresh_token
   - Automatically extracts user_id

2. Other endpoints will now work with the saved token
```

#### Option B: Phone-based Authentication
```
1. Auth → POST /api/v1/Auth/login-phone
   - Enter phone number
   - OTP will be sent (check your implementation)

2. Auth → POST /api/v1/Auth/verify-phone-otp
   - Enter phone number and OTP code
   - Automatically saves tokens
```

### 4. Test Other Endpoints

All authenticated endpoints automatically use the saved `access_token`.

## Automated Features

### Automatic Token Management

All login endpoints include test scripts that automatically:

1. **Extract tokens**: Access and refresh tokens are saved to collection variables
2. **Save user info**: User ID is extracted and saved
3. **Validate responses**: Check for successful login and token presence
4. **Log results**: Console output for debugging

Example test script (automatically included):

```javascript
pm.test("Login successful", function () {
    pm.response.to.have.status(200);
    var jsonData = pm.response.json();

    if (jsonData.success && jsonData.data && jsonData.data.token) {
        pm.collectionVariables.set("access_token", jsonData.data.token);
        pm.collectionVariables.set("refresh_token", jsonData.data.refreshToken);
        console.log("Access token saved");

        if (jsonData.data.user) {
            pm.collectionVariables.set("user_id", jsonData.data.user.userId);
            console.log("User ID:", jsonData.data.user.userId);
        }
    }
});
```

### Pre-Request Scripts

Global pre-request script checks for access token and logs warnings if missing.

### Request Examples

All POST/PUT requests include example request bodies:

#### Register User
```json
{
  "email": "newuser@example.com",
  "password": "Test123!",
  "fullName": "Test User",
  "mobilePhones": "+905551234567",
  "referralCode": "",
  "role": "Farmer"
}
```

#### Create Smart Link (for XL tier sponsors)
```json
{
  "sponsorId": "{{sponsor_id}}",
  "linkUrl": "https://example.com/product",
  "linkText": "Check out our product",
  "linkDescription": "Best agricultural solution",
  "linkType": "Product",
  "keywords": ["agriculture", "fertilizer", "pesticide"],
  "productCategory": "Fertilizers",
  "targetCropTypes": ["Tomato", "Wheat"],
  "targetDiseases": ["Blight", "Rust"],
  "targetPests": ["Aphids"],
  "priority": 1,
  "displayPosition": "TopBanner",
  "displayStyle": "Card",
  "productName": "Super Fertilizer X",
  "productPrice": 99.99,
  "productCurrency": "TRY",
  "isPromotional": true,
  "discountPercentage": 15.0
}
```

## Folder Organization

Requests are organized into 23 logical folders by controller:

### Authentication & Authorization
- **Auth**: Login, registration, password management
- **Users**: User management
- **Groups**: User group management
- **UserGroups**: User-group relationships
- **GroupClaims**: Group permissions
- **UserClaims**: User permissions
- **OperationClaims**: Operation permissions

### Core Business Logic
- **PlantAnalyses**: AI-powered plant disease detection
- **Subscriptions**: Tier-based subscription management
- **Sponsorship**: Sponsor-farmer relationships and analytics
- **Referral**: Referral code system
- **Redemption**: Code redemption

### Communication & Notifications
- **Notification**: Real-time notifications
- **SignalRNotification**: SignalR-based real-time messaging

### Integration & Mobile
- **DeepLinks**: Mobile app deep linking
- **BulkOperations**: Bulk operations for sponsors

### Configuration
- **Languages**: Supported languages
- **Translates**: Translation keys

### System
- **Health**: System health checks
- **Test**: Test endpoints
- **TestDatabase**: Database testing
- **Logs**: System logs

## Common Workflows

### Workflow 1: Complete Farmer Journey
```
1. Auth → Login as Farmer
2. PlantAnalyses → POST /api/v1/PlantAnalyses/analyze
   - Upload plant image
   - Receive AI analysis
3. Notification → GET /api/v1/Notification/user-notifications
   - Check for sponsor recommendations
4. Referral → GET /api/v1/Referral/rewards
   - Check referral rewards
```

### Workflow 2: Sponsor Operations
```
1. Auth → Login as Sponsor
2. Sponsorship → POST /api/v1/sponsorship/purchase-package
   - Purchase analysis codes
3. Sponsorship → POST /api/v1/sponsorship/codes
   - Generate referral codes
4. BulkOperations → POST /api/v1/BulkOperations/send-links
   - Send codes to farmers
5. Sponsorship → GET /api/v1/sponsorship/statistics
   - View analytics
```

### Workflow 3: Admin Management
```
1. Auth → Login as Admin
2. Users → GET /api/v1/users
   - View all users
3. Subscriptions → GET /api/v1/Subscriptions
   - Manage subscriptions
4. TestDatabase → POST /api/v1/TestDatabase/connection
   - Test database connection
```

## Testing Tips

### 1. Use Collection Runner

Run entire folders or the complete collection:

1. Right-click folder → **Run folder**
2. Configure iterations and delays
3. View aggregated test results

### 2. Use Environments

Create separate environments for:
- **Local**: `localhost:5001`
- **Staging**: `staging.ziraai.com`
- **Production**: `api.ziraai.com`

### 3. Monitor Console

Use Postman Console (View → Show Postman Console) to see:
- Token extraction logs
- Variable updates
- Request/response details

### 4. Chain Requests

Variables automatically chain between requests:

```
1. Login → saves access_token
2. Create Resource → uses access_token, saves resource_id
3. Get Resource → uses access_token and resource_id
```

## Troubleshooting

### 401 Unauthorized Error

**Cause**: Missing or expired access token

**Solutions**:
1. Run a login request first (Auth → Login as Farmer/Sponsor/Admin)
2. Check that `access_token` variable is set (click collection → Variables tab)
3. Token may have expired - login again

### 400 Bad Request

**Cause**: Invalid request body or parameters

**Solutions**:
1. Check request body matches expected schema
2. Verify required fields are present
3. Check data types (string vs number vs boolean)
4. Review API documentation in Swagger

### 403 Forbidden

**Cause**: Insufficient permissions for the operation

**Solutions**:
1. Verify user role has required permissions
2. Check operation claims for the endpoint
3. Login with appropriate user role (Farmer/Sponsor/Admin)

### Variables Not Saving

**Cause**: Test scripts not executing or response format mismatch

**Solutions**:
1. Check Postman Console for errors
2. Verify response JSON structure matches test script expectations
3. Ensure tests are enabled (not disabled)

## Advanced Usage

### Custom Test Scripts

You can add additional test scripts to any request:

```javascript
// Validate specific response fields
pm.test("Has required fields", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.data).to.have.property('id');
    pm.expect(jsonData.data).to.have.property('createdDate');
});

// Extract and save custom variables
pm.test("Save plant analysis ID", function () {
    var jsonData = pm.response.json();
    if (jsonData.success && jsonData.data && jsonData.data.id) {
        pm.collectionVariables.set("plant_analysis_id", jsonData.data.id);
    }
});

// Performance testing
pm.test("Response time < 200ms", function () {
    pm.expect(pm.response.responseTime).to.be.below(200);
});
```

### Pre-Request Scripts

Add logic before request execution:

```javascript
// Auto-refresh expired tokens
const accessToken = pm.collectionVariables.get("access_token");
const refreshToken = pm.collectionVariables.get("refresh_token");

if (!accessToken && refreshToken) {
    // Trigger refresh token request
    pm.sendRequest({
        url: pm.collectionVariables.get("base_url") + "/api/v1/Auth/refresh-token",
        method: 'POST',
        header: 'Content-Type: application/json',
        body: {
            mode: 'raw',
            raw: JSON.stringify({ refreshToken: refreshToken })
        }
    }, function (err, res) {
        const newToken = res.json().data.token;
        pm.collectionVariables.set("access_token", newToken);
    });
}
```

## API Version Support

The collection supports API versioning via the `version` variable:

- Current default: `v1`
- All endpoints use: `/api/v{{version}}/...`
- Change version in collection variables to test different API versions

## Security Notes

1. **Never commit tokens**: Keep `access_token` and `refresh_token` empty in the committed collection
2. **Use HTTPS**: Always use `https://` in production
3. **Environment separation**: Use different credentials for local/staging/production
4. **Token expiry**: Tokens expire after 60 minutes - re-login if needed
5. **Refresh tokens**: Valid for 180 minutes - use refresh endpoint to extend session

## Support & Documentation

- **Swagger Documentation**: `https://localhost:5001/swagger`
- **Project Documentation**: See `CLAUDE.md` in project root
- **API Guide**: See `claudedocs/messaging-service-architecture.md`
- **Postman Documentation**: [https://www.postman.com/](https://www.postman.com/)

## Collection Maintenance

### Updating the Collection

If the API changes:

1. Export updated `swagger.json` from Swagger UI
2. Re-run the generation script (if available)
3. Or manually add/update endpoints in Postman
4. Export updated collection
5. Commit changes to repository

### Version History

- **v1.0** (2025-10-03): Initial complete collection with 162 endpoints
  - All 23 controllers included
  - Automated token management
  - Comprehensive test scripts
  - Example request bodies

## License

This collection is part of the ZiraAI project and follows the same license terms.
