# ZiraAI API Documentation - Tier-Based Access Control System

## Overview
ZiraAI API provides comprehensive plant analysis services with a sophisticated tier-based sponsorship system. This documentation covers all endpoints, authentication requirements, and tier-specific access controls.

**Version**: 2.0  
**Last Updated**: August 2025  
**Base URL**: `https://api.ziraai.com/api/v1`

## Authentication

### JWT Bearer Authentication
All protected endpoints require JWT Bearer token authentication:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Role-Based Access Control
The system supports three primary roles:
- **Farmer**: Plant analysis and subscription management
- **Sponsor**: Company profile, package purchasing, tier-based features
- **Admin**: Full system administration

## Tier-Based Access Matrix

| Endpoint Category | S Tier | M Tier | L Tier | XL Tier | Farmer | Admin |
|-------------------|--------|--------|--------|---------|--------|-------|
| **Basic Analytics** | ✅ 30% | ✅ 30% | ✅ 60% | ✅ 100% | ❌ | ✅ |
| **Farmer Messaging** | ❌ | ❌ | ✅ | ✅ | ✅ (Receive) | ✅ |
| **Full Farmer Profiles** | ❌ | ❌ | ✅ | ✅ | ✅ (Own) | ✅ |
| **Anonymous Profiles** | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ |
| **Smart Linking** | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |
| **Logo Visibility** | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |

## Authentication Endpoints

### User Registration
```http
POST /auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Farmer" // "Farmer", "Sponsor", "Admin"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "userId": 123,
    "email": "user@example.com",
    "role": "Farmer",
    "isEmailVerified": false
  }
}
```

### User Login
```http
POST /auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here",
    "expiration": "2025-08-17T10:30:00Z",
    "user": {
      "id": 123,
      "email": "user@example.com",
      "role": "Farmer"
    }
  }
}
```

## Plant Analysis Endpoints

### Synchronous Plant Analysis
```http
POST /plantanalyses/analyze
Authorization: Bearer {token}
Content-Type: application/json
Roles: Farmer, Admin

{
  "image": "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQ...",
  "farmerId": "F001",
  "cropType": "tomato",
  "location": {
    "latitude": 39.9334,
    "longitude": 32.8597
  },
  "weatherConditions": {
    "temperature": 25.5,
    "humidity": 60.0,
    "rainfall": 0.0
  }
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 456,
    "analysisDate": "2025-08-16T14:30:00Z",
    "overallHealthScore": 8,
    "healthStatus": "Good",
    "recommendations": [
      "Increase nitrogen fertilization",
      "Monitor for early blight symptoms"
    ],
    "diseases": [
      {
        "name": "Early Blight",
        "confidence": 0.75,
        "severity": "Mild"
      }
    ],
    "nutrientStatus": {
      "nitrogen": "Deficient",
      "phosphorus": "Adequate",
      "potassium": "Adequate"
    },
    "sponsorInfo": {
      "companyName": "AgriTech Solutions",
      "logoUrl": "https://api.ziraai.com/sponsor-logos/agritech.png",
      "tier": "L"
    }
  }
}
```

### Asynchronous Plant Analysis
```http
POST /plantanalyses/analyze-async
Authorization: Bearer {token}
Content-Type: application/json
Roles: Farmer, Admin

{
  "image": "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQ...",
  "farmerId": "F001",
  "cropType": "tomato"
}
```

**Response:**
```json
{
  "success": true,
  "data": "async_analysis_20250816_143022_abc123",
  "message": "Analysis queued successfully. You will be notified when complete."
}
```

### Get Plant Analysis List (Mobile Optimized)
```http
GET /plantanalyses/list?page=1&pageSize=20&status=Completed&cropType=tomato
Authorization: Bearer {token}
Roles: Farmer, Admin
```

**Response:**
```json
{
  "success": true,
  "data": {
    "analyses": [
      {
        "id": 123,
        "imagePath": "https://api.ziraai.com/uploads/plant-images/image.jpg",
        "status": "Completed",
        "statusIcon": "✅",
        "cropType": "tomato",
        "farmerId": "F045",
        "overallHealthScore": 8,
        "primaryConcern": "Mild nutrient deficiency",
        "formattedDate": "16/08/2025 14:30",
        "isSponsored": true,
        "hasResults": true,
        "healthScoreText": "8/10"
      }
    ],
    "totalCount": 45,
    "page": 1,
    "totalPages": 3,
    "hasNextPage": true,
    "completedCount": 42,
    "sponsoredCount": 15
  }
}
```

### Get Plant Analysis Details
```http
GET /plantanalyses/{id}
Authorization: Bearer {token}
Roles: Farmer, Admin, Sponsor (with data access)
```

## Subscription System Endpoints

### Get Available Subscription Tiers
```http
GET /subscriptions/tiers
Content-Type: application/json
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "tierName": "S",
      "displayName": "Small",
      "dailyRequestLimit": 5,
      "monthlyRequestLimit": 50,
      "monthlyPrice": 99.99,
      "currency": "TRY",
      "features": [
        "Basic plant analysis",
        "Mobile app access",
        "Email support"
      ]
    },
    {
      "id": 3,
      "tierName": "L",
      "displayName": "Large",
      "dailyRequestLimit": 50,
      "monthlyRequestLimit": 500,
      "monthlyPrice": 599.99,
      "currency": "TRY",
      "features": [
        "Advanced plant analysis",
        "Priority support",
        "API access",
        "Advanced analytics"
      ]
    }
  ]
}
```

### Subscribe to Plan
```http
POST /subscriptions/subscribe
Authorization: Bearer {token}
Content-Type: application/json
Roles: Farmer

{
  "subscriptionTierId": 2,
  "durationMonths": 1,
  "autoRenew": true,
  "paymentMethod": "CreditCard",
  "paymentReference": "txn_123456"
}
```

### Redeem Sponsorship Code
```http
POST /subscriptions/redeem-code
Authorization: Bearer {token}
Content-Type: application/json
Roles: Farmer

{
  "sponsorshipCode": "SPT001-ABC123"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "subscriptionId": 789,
    "tierName": "L",
    "dailyLimit": 50,
    "monthlyLimit": 500,
    "validUntil": "2026-08-16T00:00:00Z",
    "sponsorInfo": {
      "companyName": "AgriTech Solutions",
      "contactEmail": "support@agritech.com"
    }
  }
}
```

## Sponsorship System Endpoints

### Company Profile Management

#### Create Sponsor Profile
```http
POST /sponsorships/create-profile
Authorization: Bearer {token}
Content-Type: application/json
Roles: Sponsor, Admin

{
  "companyName": "AgriTech Solutions Ltd",
  "contactEmail": "contact@agritech.com",
  "contactPhone": "+90555123456",
  "website": "https://agritech.com",
  "companyType": "Private",
  "businessModel": "B2B",
  "description": "Leading agricultural technology provider",
  "logoUrl": "https://agritech.com/logo.png"
}
```

#### Get Sponsor Profile
```http
GET /sponsorships/my-profile
Authorization: Bearer {token}
Roles: Sponsor, Admin
```

#### Update Sponsor Profile
```http
PUT /sponsorships/update-profile/{id}
Authorization: Bearer {token}
Content-Type: application/json
Roles: Sponsor, Admin

{
  "companyName": "Updated Company Name",
  "contactEmail": "newemail@company.com",
  "website": "https://newwebsite.com"
}
```

### Package Purchasing & Code Management

#### Purchase Sponsorship Package
```http
POST /sponsorships/purchase-package
Authorization: Bearer {token}
Content-Type: application/json
Roles: Sponsor, Admin

{
  "subscriptionTierId": 3,
  "quantity": 50,
  "amount": 2999.50,
  "paymentMethod": "CreditCard",
  "paymentReference": "txn_abc123",
  "validityDays": 365
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "purchaseId": 123,
    "codePrefix": "SPT001",
    "totalAmount": 2999.50,
    "generatedCodes": [
      {
        "code": "SPT001-ABC123",
        "tierName": "L",
        "expiryDate": "2026-08-16T00:00:00Z"
      }
    ],
    "summary": {
      "totalCodes": 50,
      "tierName": "L",
      "validityDays": 365
    }
  }
}
```

#### Get Purchase History
```http
GET /sponsorships/my-purchases
Authorization: Bearer {token}
Roles: Sponsor, Admin
```

#### Get Generated Codes
```http
GET /sponsorships/my-codes?page=1&pageSize=50&isRedeemed=false
Authorization: Bearer {token}
Roles: Sponsor, Admin
```

### Tier-Based Features

#### Farmer Messaging (L/XL Tiers Only)

##### Send Message to Farmer
```http
POST /sponsorship/messages
Authorization: Bearer {token}
Content-Type: application/json
Roles: Sponsor (L/XL Tiers Only)

{
  "farmerId": 123,
  "subject": "Plant Analysis Follow-up",
  "message": "Your tomato plants show excellent growth patterns. We recommend organic fertilizers for optimal yield."
}
```

**Tier Restriction Response (S/M Tiers):**
```json
{
  "success": false,
  "message": "Messaging is not allowed for your subscription tier. Upgrade to Large or Extra Large package to access farmer messaging.",
  "tierInfo": {
    "currentTier": "S",
    "messagingTiers": ["L", "XL"],
    "upgradeRequired": true
  }
}
```

##### Get Message Conversation
```http
GET /sponsorship/messages/conversation/{farmerId}
Authorization: Bearer {token}
Roles: Sponsor (L/XL Tiers Only)
```

##### List All Messages
```http
GET /sponsorship/messages?page=1&pageSize=20
Authorization: Bearer {token}
Roles: Sponsor (L/XL Tiers Only)
```

#### Farmer Profile Access (Tier-Dependent)

##### Get Farmer Profile
```http
GET /sponsorship/farmer-profile/{farmerId}
Authorization: Bearer {token}
Roles: Sponsor
```

**S Tier Response:**
```json
{
  "success": false,
  "message": "Farmer profile access is not available for your tier.",
  "tierInfo": {
    "currentTier": "S",
    "profileAccessTiers": ["M", "L", "XL"]
  }
}
```

**M Tier Response (Anonymous):**
```json
{
  "success": true,
  "data": {
    "farmerId": "F***",
    "region": "Central Anatolia",
    "cropTypes": ["tomato", "pepper"],
    "farmSize": "5-10 hectares",
    "experienceLevel": "Intermediate",
    "analysisCount": 25
  }
}
```

**L/XL Tier Response (Full Profile):**
```json
{
  "success": true,
  "data": {
    "farmerId": "F045",
    "firstName": "John",
    "lastName": "Doe",
    "email": "farmer@example.com",
    "phone": "+90555987654",
    "location": {
      "region": "Central Anatolia",
      "city": "Ankara"
    },
    "farmDetails": {
      "farmSize": "8 hectares",
      "cropTypes": ["tomato", "pepper", "cucumber"],
      "experienceLevel": "Intermediate"
    },
    "analysisHistory": {
      "totalAnalyses": 25,
      "lastAnalysisDate": "2025-08-15T10:00:00Z",
      "averageHealthScore": 7.8
    }
  }
}
```

#### Analytics & Data Access

##### Get Sponsored Analyses
```http
GET /sponsorships/sponsored-analyses?page=1&pageSize=50
Authorization: Bearer {token}
Roles: Sponsor
```

**Response (Data Limited by Tier):**
```json
{
  "success": true,
  "data": {
    "analyses": [
      {
        "id": 123,
        "farmerId": "F045",
        "cropType": "tomato",
        "healthScore": 8,
        "analysisDate": "2025-08-16T14:30:00Z",
        "location": "Central Anatolia"
      }
    ],
    "totalCount": 150,
    "accessibleCount": 45,
    "tierInfo": {
      "currentTier": "S",
      "dataAccessPercentage": 30,
      "upgradeForFullAccess": ["L", "XL"]
    }
  }
}
```

##### Usage Analytics
```http
GET /sponsorships/usage-analytics
Authorization: Bearer {token}
Roles: Sponsor
```

#### Smart Links (All Tiers)

##### Create Smart Link
```http
POST /sponsorship/smart-links
Authorization: Bearer {token}
Content-Type: application/json
Roles: Sponsor

{
  "title": "AgriTech Plant Analysis",
  "description": "Professional plant health assessment powered by AI",
  "targetUrl": "https://agritech.com/analysis",
  "isActive": true,
  "displayDuration": 30
}
```

##### Get Smart Links
```http
GET /sponsorship/smart-links
Authorization: Bearer {token}
Roles: Sponsor
```

## Error Responses

### Common Error Formats

#### Authentication Error
```json
{
  "success": false,
  "message": "Unauthorized access. Please provide a valid token.",
  "errorCode": "AUTH_001"
}
```

#### Tier Restriction Error
```json
{
  "success": false,
  "message": "This feature is not available for your current tier.",
  "tierInfo": {
    "currentTier": "S",
    "requiredTiers": ["L", "XL"],
    "featureName": "Farmer Messaging"
  },
  "errorCode": "TIER_001"
}
```

#### Quota Exceeded Error
```json
{
  "success": false,
  "message": "Daily request limit reached (5 requests). Resets at midnight.",
  "subscriptionStatus": {
    "tierName": "S",
    "dailyUsed": 5,
    "dailyLimit": 5,
    "monthlyUsed": 45,
    "monthlyLimit": 50,
    "nextDailyReset": "2025-08-17T00:00:00Z"
  },
  "errorCode": "QUOTA_001"
}
```

#### Validation Error
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    {
      "field": "farmerId",
      "message": "Valid farmer ID is required"
    },
    {
      "field": "message",
      "message": "Message content cannot be empty"
    }
  ],
  "errorCode": "VALIDATION_001"
}
```

## Rate Limiting

### Subscription-Based Limits
Rate limiting is enforced based on subscription tiers:

- **Trial**: 1 request/day, 30 requests/month
- **S Tier**: 5 requests/day, 50 requests/month
- **M Tier**: 20 requests/day, 200 requests/month
- **L Tier**: 50 requests/day, 500 requests/month
- **XL Tier**: 200 requests/day, 2000 requests/month

### Rate Limit Headers
```http
X-RateLimit-Limit: 50
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1692144000
X-RateLimit-Tier: L
```

## Webhooks & Notifications

### Webhook Configuration
```http
POST /webhooks/configure
Authorization: Bearer {token}
Content-Type: application/json
Roles: Sponsor, Admin

{
  "url": "https://your-domain.com/webhook",
  "events": ["analysis_completed", "message_received", "code_redeemed"],
  "secret": "webhook_secret_key"
}
```

### Webhook Payload Examples

#### Analysis Completed
```json
{
  "event": "analysis_completed",
  "timestamp": "2025-08-16T14:30:00Z",
  "data": {
    "analysisId": 123,
    "farmerId": "F045",
    "sponsorInfo": {
      "companyName": "AgriTech Solutions",
      "tier": "L"
    },
    "results": {
      "healthScore": 8,
      "primaryConcerns": ["Nutrient deficiency"]
    }
  }
}
```

#### Code Redeemed
```json
{
  "event": "code_redeemed",
  "timestamp": "2025-08-16T14:30:00Z",
  "data": {
    "code": "SPT001-ABC123",
    "redeemedBy": {
      "farmerId": "F045",
      "email": "farmer@example.com"
    },
    "subscription": {
      "tier": "L",
      "validUntil": "2026-08-16T00:00:00Z"
    }
  }
}
```

## SDK Integration

### JavaScript/Node.js Example
```javascript
const ZiraAI = require('@ziraai/sdk');

const client = new ZiraAI({
  apiKey: 'your_jwt_token',
  baseUrl: 'https://api.ziraai.com/api/v1'
});

// Send message (L/XL tier only)
const message = await client.sponsorship.sendMessage({
  farmerId: 123,
  subject: 'Analysis Follow-up',
  message: 'Your plants look healthy!'
});

// Get farmer profile (tier-dependent visibility)
const profile = await client.sponsorship.getFarmerProfile(123);
```

### Python Example
```python
from ziraai import ZiraAIClient

client = ZiraAIClient(
    api_key='your_jwt_token',
    base_url='https://api.ziraai.com/api/v1'
)

# Check tier capabilities
tier_info = client.sponsorship.get_tier_info()
if tier_info['messaging_enabled']:
    response = client.sponsorship.send_message(
        farmer_id=123,
        subject='Analysis Follow-up',
        message='Your plants look healthy!'
    )
```

## Testing & Development

### Test Environment
- **Base URL**: `https://api-staging.ziraai.com/api/v1`
- **Test Sponsor Account**: Use sandbox payment methods
- **Mock Farmer Accounts**: Available for integration testing

### Postman Collection
Complete Postman collection available with:
- Pre-configured environment variables
- Automated token management
- Tier-based test scenarios
- Error condition testing

This comprehensive API documentation ensures proper implementation of the tier-based sponsorship system with clear guidelines for each access level and feature set.