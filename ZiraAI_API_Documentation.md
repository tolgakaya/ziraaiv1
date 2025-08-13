# ZiraAI Plant Analysis API - Kapsamlı Dokümantasyon

## 📋 İçindekiler
1. [API Genel Bakış](#api-genel-bakış)
2. [Authentication & Authorization](#authentication--authorization)
3. [Rol ve Yetki Sistemi](#rol-ve-yetki-sistemi)
4. [Subscription Sistemi](#subscription-sistemi)
5. [Plant Analysis API](#plant-analysis-api)
6. [Authentication Endpoints](#authentication-endpoints)
7. [User Management](#user-management)
8. [Subscription Management](#subscription-management)
9. [Test Endpoints](#test-endpoints)
10. [Error Handling](#error-handling)
11. [Postman Collection Kullanımı](#postman-collection-kullanımı)

---

## 🔧 API Genel Bakış

### Base URL
```
Development: https://localhost:5001
Staging: https://staging-api.ziraai.com
Production: https://api.ziraai.com
```

### API Versioning
- **Header**: `x-dev-arch-version: 1.0`
- **Default Version**: 1.0

### Content Types
- **Request**: `application/json`
- **Response**: `application/json`

---

## 🔐 Authentication & Authorization

### JWT Token Sistemi

#### Token Özellikleri
- **Access Token Süresi**: 60 dakika
- **Refresh Token Süresi**: 180 dakika
- **Algorithm**: HS256
- **Claims**: UserId, Email, Roles, OperationClaims

#### Token Usage
```http
Authorization: Bearer {access_token}
```

#### Token Response Format
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
    "expiration": "2025-08-13T15:30:00Z"
  },
  "message": "Login successful"
}
```

---

## 👥 Rol ve Yetki Sistemi

### Temel Roller

#### 🌱 **Farmer (Çiftçi)**
- **Açıklama**: Plant analysis isteklerinde bulunan temel kullanıcı
- **Yetkiler**:
  - Plant analysis oluşturma (sync/async)
  - Kendi analizlerini görüntüleme
  - Profil yönetimi
- **Kısıtlamalar**: Subscription sistemine tabi

#### 💰 **Sponsor**
- **Açıklama**: Plant analysis'leri sponsor eden kullanıcı
- **Yetkiler**:
  - Sponsor olduğu analizleri görüntüleme
  - Sponsor raporları alma
- **Özellikler**: SponsorId ile tracking

#### 👑 **Admin**
- **Açıklama**: Sistem yöneticisi
- **Yetkiler**:
  - Tüm analizleri görüntüleme ve yönetme
  - Kullanıcı yönetimi
  - Subscription tier yönetimi
  - Sistem konfigürasyonu
- **Kısıtlamalar**: Yok

### Operation Claims Sistemi

#### Yetki Kontrolü
- Method-level security with `[SecuredOperation]` attribute
- Claims cache sistemi (Redis)
- Dinamik yetki atama

#### Örnek Operation Claims
```
- PlantAnalysisCreate
- PlantAnalysisRead
- UserManagement
- SubscriptionManagement
- SystemConfiguration
```

---

## 💳 Subscription Sistemi

### Subscription Tiers

| Tier | Adı | Daily Limit | Monthly Limit | Fiyat (TL/ay) |
|------|-----|------------|---------------|---------------|
| **S** | Small | 5 | 50 | ₺99.99 |
| **M** | Medium | 20 | 200 | ₺299.99 |
| **L** | Large | 50 | 500 | ₺599.99 |
| **XL** | Extra Large | 200 | 2000 | ₺1,499.99 |

### Subscription Features

#### Tier Özellikleri
```json
{
  "tierName": "M",
  "displayName": "Medium",
  "dailyRequestLimit": 20,
  "monthlyRequestLimit": 200,
  "monthlyPrice": 299.99,
  "currency": "TRY",
  "prioritySupport": true,
  "advancedAnalytics": false,
  "apiAccess": true,
  "responseTimeHours": 24
}
```

#### Usage Tracking
- Real-time kullanım takibi
- Günlük limit: Her gece 00:00'da reset
- Aylık limit: Her ayın 1'inde reset
- Quota aşım kontrolü her istekte

---

## 🌿 Plant Analysis API

### 1. Synchronous Analysis

#### POST `/api/plantanalyses/analyze`
**🔒 Yetki**: `[Authorize]` - Farmer, Admin  
**📊 Subscription**: Gerekli

```http
POST /api/plantanalyses/analyze
Authorization: Bearer {token}
Content-Type: application/json

{
  "image": "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQ...",
  "farmerId": "FARMER001",
  "sponsorId": "SPONSOR001",
  "cropType": "tomato",
  "location": "Antalya Greenhouse",
  "gpsCoordinates": {
    "latitude": 36.8969,
    "longitude": 30.7133
  },
  "fieldId": "FIELD001",
  "plantSpecies": "Solanum lycopersicum",
  "growthStage": "flowering",
  "environmentalData": {
    "temperature": 24.5,
    "humidity": 65.0,
    "soilMoisture": 45.2,
    "lightIntensity": 850.0
  }
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": 123,
    "analysisId": "PLT_20250813_142030_abc123",
    "farmerId": "FARMER001",
    "sponsorId": "SPONSOR001",
    "analysisDate": "2025-08-13T14:20:30Z",
    "status": "Completed",
    "imageProcessingMethod": "URL",
    "tokenUsage": 1500,
    "originalImageSize": "2.1MB",
    "optimizedImageSize": "98KB",
    "summary": {
      "overallHealthScore": 7.5,
      "primaryConcern": "Slight nutrient deficiency",
      "confidenceLevel": 0.87,
      "plantSpecies": "Solanum lycopersicum",
      "growthStage": "flowering"
    },
    "detailedAnalysis": {
      "diseases": [
        {
          "name": "Early Blight",
          "confidence": 0.23,
          "severity": "Minimal",
          "affectedArea": "5%"
        }
      ],
      "pests": [],
      "nutritionalDeficiencies": [
        {
          "nutrient": "Nitrogen",
          "severity": "Mild",
          "confidence": 0.65,
          "recommendations": "Apply nitrogen-rich fertilizer"
        }
      ],
      "environmentalStress": {
        "waterStress": "Optimal",
        "temperatureStress": "None",
        "lightStress": "None"
      }
    },
    "actionRecommendations": [
      {
        "priority": "Medium",
        "action": "Apply nitrogen-rich fertilizer",
        "timeframe": "Within 1 week",
        "details": "Use 15-15-15 NPK fertilizer at 200g per plant"
      }
    ],
    "processedImageUrl": "https://api.ziraai.com/uploads/plant-images/plt_123_20250813_142030.jpg"
  },
  "message": "Plant analysis completed successfully"
}
```

### 2. Asynchronous Analysis

#### POST `/api/plantanalyses/analyze-async`
**🔒 Yetki**: `[Authorize]` - Farmer, Admin  
**📊 Subscription**: Gerekli

```http
POST /api/plantanalyses/analyze-async
Authorization: Bearer {token}
Content-Type: application/json

{
  "image": "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQ...",
  "farmerId": "FARMER001",
  "sponsorId": "SPONSOR001",
  "cropType": "tomato",
  "location": "Antalya Greenhouse"
}
```

**Response (202 Accepted):**
```json
{
  "success": true,
  "data": "async_analysis_20250813_142030_abc123",
  "message": "Analysis request queued successfully. You will receive results via notification."
}
```

### 3. Get Analysis by ID

#### GET `/api/plantanalyses/{id}`
**🔒 Yetki**: `[Authorize]` - Farmer (own), Admin (all)

```http
GET /api/plantanalyses/123
Authorization: Bearer {token}
```

### 4. Get Analysis Image

#### GET `/api/plantanalyses/{id}/image`
**🔒 Yetki**: `[Authorize]` - Farmer (own), Admin (all)

```http
GET /api/plantanalyses/123/image
Authorization: Bearer {token}
```

**Response**: Binary image data with correct MIME type

### 5. Admin Endpoints

#### GET `/api/plantanalyses`
**🔒 Yetki**: `[Authorize(Roles = "Admin")]`

```http
GET /api/plantanalyses?page=1&size=20&cropType=tomato
Authorization: Bearer {token}
```

#### GET `/api/plantanalyses/sponsored-analyses`
**🔒 Yetki**: `[Authorize(Roles = "Admin")]`

```http
GET /api/plantanalyses/sponsored-analyses
Authorization: Bearer {token}
```

---

## 🔑 Authentication Endpoints

### 1. Login

#### POST `/api/v1/auth/login`
**🔓 Public Endpoint**

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "farmer@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
    "expiration": "2025-08-13T15:30:00Z",
    "user": {
      "userId": 123,
      "fullName": "John Doe",
      "email": "farmer@example.com",
      "roles": ["Farmer"],
      "subscription": {
        "tierName": "M",
        "dailyUsed": 5,
        "dailyLimit": 20,
        "monthlyUsed": 45,
        "monthlyLimit": 200
      }
    }
  },
  "message": "Login successful"
}
```

### 2. Refresh Token

#### POST `/api/v1/auth/refresh-token`
**🔓 Public Endpoint**

```http
POST /api/v1/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 3. Register

#### POST `/api/v1/auth/register`
**🔓 Public Endpoint**

```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "fullName": "John Doe",
  "email": "farmer@example.com",
  "password": "SecurePassword123!",
  "citizenId": 12345678901,
  "mobilePhones": "+90 555 123 4567",
  "address": "Antalya, Turkey"
}
```

### 4. Change Password

#### PUT `/api/v1/auth/user-password`
**🔒 Yetki**: `[Authorize]`

```http
PUT /api/v1/auth/user-password
Authorization: Bearer {token}
Content-Type: application/json

{
  "oldPassword": "OldPassword123!",
  "newPassword": "NewSecurePassword123!"
}
```

---

## 👤 User Management

### 1. Get Users (Admin Only)

#### GET `/api/users`
**🔒 Yetki**: `[Authorize(Roles = "Admin")]`

```http
GET /api/users?page=1&size=20&search=john
Authorization: Bearer {token}
```

### 2. Get User by ID

#### GET `/api/users/{id}`
**🔒 Yetki**: `[Authorize]`

```http
GET /api/users/123
Authorization: Bearer {token}
```

### 3. Update User

#### PUT `/api/users/{id}`
**🔒 Yetki**: `[Authorize]` (own) or Admin

```http
PUT /api/users/123
Authorization: Bearer {token}
Content-Type: application/json

{
  "fullName": "John Updated Doe",
  "email": "john.updated@example.com",
  "mobilePhones": "+90 555 987 6543",
  "address": "Updated Address"
}
```

---

## 💳 Subscription Management

### 1. Get Subscription Tiers

#### GET `/api/subscriptions/tiers`
**🔓 Public Endpoint**

```http
GET /api/subscriptions/tiers
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
      "description": "Perfect for small farms",
      "dailyRequestLimit": 5,
      "monthlyRequestLimit": 50,
      "monthlyPrice": 99.99,
      "yearlyPrice": 1079.99,
      "currency": "TRY",
      "prioritySupport": false,
      "advancedAnalytics": false,
      "apiAccess": true,
      "responseTimeHours": 48
    }
  ]
}
```

### 2. Get My Subscription

#### GET `/api/subscriptions/my-subscription`
**🔒 Yetki**: `[Authorize(Roles = "Farmer")]`

```http
GET /api/subscriptions/my-subscription
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 456,
    "subscriptionTier": {
      "tierName": "M",
      "displayName": "Medium",
      "dailyRequestLimit": 20,
      "monthlyRequestLimit": 200
    },
    "startDate": "2025-08-01T00:00:00Z",
    "endDate": "2025-09-01T00:00:00Z",
    "isActive": true,
    "autoRenew": true,
    "currentDailyUsage": 5,
    "currentMonthlyUsage": 45,
    "status": "Active"
  }
}
```

### 3. Get Usage Status

#### GET `/api/subscriptions/usage-status`
**🔒 Yetki**: `[Authorize(Roles = "Farmer")]`

```http
GET /api/subscriptions/usage-status
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "tierName": "M",
    "dailyUsed": 5,
    "dailyLimit": 20,
    "dailyRemaining": 15,
    "monthlyUsed": 45,
    "monthlyLimit": 200,
    "monthlyRemaining": 155,
    "nextDailyReset": "2025-08-14T00:00:00Z",
    "nextMonthlyReset": "2025-09-01T00:00:00Z",
    "canMakeRequest": true
  }
}
```

### 4. Subscribe to Plan

#### POST `/api/subscriptions/subscribe`
**🔒 Yetki**: `[Authorize(Roles = "Farmer")]`

```http
POST /api/subscriptions/subscribe
Authorization: Bearer {token}
Content-Type: application/json

{
  "subscriptionTierId": 2,
  "durationMonths": 1,
  "autoRenew": true,
  "paymentMethod": "CreditCard",
  "paymentReference": "txn_123456789"
}
```

### 5. Cancel Subscription

#### POST `/api/subscriptions/cancel`
**🔒 Yetki**: `[Authorize(Roles = "Farmer")]`

```http
POST /api/subscriptions/cancel
Authorization: Bearer {token}
Content-Type: application/json

{
  "userSubscriptionId": 456,
  "cancellationReason": "Too expensive for my needs",
  "immediateCancellation": false
}
```

---

## 🧪 Test Endpoints

### 1. RabbitMQ Health Check

#### GET `/api/test/rabbitmq-health`
**🔒 Yetki**: `[Authorize]`

```http
GET /api/test/rabbitmq-health
Authorization: Bearer {token}
```

### 2. Mock N8N Response

#### POST `/api/test/mock-n8n-response`
**🔒 Yetki**: `[Authorize]`

```http
POST /api/test/mock-n8n-response
Authorization: Bearer {token}
Content-Type: application/json

{
  "analysisId": "test_analysis_123",
  "farmerId": "FARMER001"
}
```

---

## ❌ Error Handling

### Standard Error Response Format

```json
{
  "success": false,
  "message": "Error description",
  "errors": ["Detailed error 1", "Detailed error 2"],
  "timestamp": "2025-08-13T14:30:00Z",
  "path": "/api/plantanalyses/analyze"
}
```

### Common HTTP Status Codes

| Code | Açıklama | Örnek |
|------|----------|-------|
| **200** | Success | İstek başarılı |
| **201** | Created | Kayıt oluşturuldu |
| **400** | Bad Request | Geçersiz istek formatı |
| **401** | Unauthorized | Geçersiz token |
| **403** | Forbidden | Yetki yok |
| **404** | Not Found | Kaynak bulunamadı |
| **409** | Conflict | Çakışma (duplicate) |
| **422** | Unprocessable Entity | Validation hatası |
| **429** | Too Many Requests | Quota aşıldı |
| **500** | Internal Server Error | Sunucu hatası |

### Subscription Quota Errors

```json
{
  "success": false,
  "message": "Daily request limit reached (20 requests). Resets at midnight.",
  "subscriptionStatus": {
    "tierName": "M",
    "dailyUsed": 20,
    "dailyLimit": 20,
    "monthlyUsed": 150,
    "monthlyLimit": 200,
    "nextDailyReset": "2025-08-14T00:00:00Z",
    "canMakeRequest": false
  }
}
```

### Authentication Errors

```json
{
  "success": false,
  "message": "You need an active subscription to make analysis requests. Please subscribe to one of our plans.",
  "subscriptionStatus": {
    "hasActiveSubscription": false,
    "canMakeRequest": false
  }
}
```

---

## 🚀 Postman Collection Kullanımı

### Collection Yapısı

```
📁 ZiraAI Plant Analysis API
├── 📁 Authentication
│   ├── Login
│   ├── Refresh Token
│   ├── Register
│   └── Change Password
├── 📁 Plant Analysis
│   ├── Synchronous Analysis
│   ├── Asynchronous Analysis
│   ├── Get Analysis by ID
│   ├── Get Analysis Image
│   └── Get All Analyses (Admin)
├── 📁 Subscription Management
│   ├── Get Subscription Tiers
│   ├── Get My Subscription
│   ├── Get Usage Status
│   ├── Subscribe to Plan
│   └── Cancel Subscription
├── 📁 User Management
│   ├── Get Users (Admin)
│   ├── Get User by ID
│   └── Update User
└── 📁 Test & Health
    ├── RabbitMQ Health
    ├── Mock N8N Response
    └── JWT Token Test
```

### Environment Variables

```json
{
  "baseUrl": "https://localhost:5001",
  "accessToken": "{{token_from_login}}",
  "refreshToken": "{{refresh_token_from_login}}",
  "userId": "{{user_id_from_login}}",
  "farmerId": "FARMER001",
  "sponsorId": "SPONSOR001"
}
```

### Pre-request Scripts

**Auto Token Refresh:**
```javascript
// Pre-request script for protected endpoints
const token = pm.environment.get("accessToken");
if (!token) {
    pm.environment.set("skipRequest", true);
    throw new Error("Please login first to get access token");
}

// Check if token is expired (basic check)
const tokenExpiry = pm.environment.get("tokenExpiry");
const now = new Date().getTime();
if (tokenExpiry && now > tokenExpiry) {
    // Auto refresh token logic here
    console.log("Token expired, please refresh");
}
```

### Tests Scripts

**Login Test:**
```javascript
pm.test("Login successful", function () {
    pm.response.to.have.status(200);
    
    const response = pm.response.json();
    pm.expect(response.success).to.be.true;
    
    // Store tokens for other requests
    pm.environment.set("accessToken", response.data.token);
    pm.environment.set("refreshToken", response.data.refreshToken);
    pm.environment.set("userId", response.data.user.userId);
    
    // Calculate expiry time
    const expiry = new Date();
    expiry.setHours(expiry.getHours() + 1); // 1 hour
    pm.environment.set("tokenExpiry", expiry.getTime());
});
```

**Plant Analysis Test:**
```javascript
pm.test("Plant analysis successful", function () {
    pm.response.to.have.status(200);
    
    const response = pm.response.json();
    pm.expect(response.success).to.be.true;
    pm.expect(response.data.analysisId).to.not.be.undefined;
    pm.expect(response.data.summary.overallHealthScore).to.be.a('number');
    
    // Store analysis ID for subsequent requests
    pm.environment.set("lastAnalysisId", response.data.id);
});
```

### Workflow İpuçları

1. **İlk Login**: Authentication -> Login ile başlayın
2. **Token Yenileme**: Token süresi dolduğunda Refresh Token kullanın  
3. **Subscription Check**: Plant analysis öncesi Usage Status kontrolü yapın
4. **Error Monitoring**: Response testlerinde quota ve auth kontrolü ekleyin
5. **Environment Management**: Development/Staging/Production environment'ları ayırın

---

## 📚 Özet API Kullanım Senaryoları

### Senaryo 1: Yeni Farmer Registration & Analysis

```
1. POST /api/v1/auth/register (Farmer kaydı)
2. POST /api/v1/auth/login (Giriş)
3. GET /api/subscriptions/tiers (Planları görüntüle)
4. POST /api/subscriptions/subscribe (Plan satın al)
5. POST /api/plantanalyses/analyze (Analiz yap)
```

### Senaryo 2: Admin Panel Management

```
1. POST /api/v1/auth/login (Admin giriş)
2. GET /api/plantanalyses (Tüm analizler)
3. GET /api/users (Kullanıcı listesi)
4. GET /api/subscriptions/usage-logs (Kullanım raporları)
```

### Senaryo 3: Subscription Management

```
1. GET /api/subscriptions/my-subscription (Mevcut plan)
2. GET /api/subscriptions/usage-status (Kullanım durumu)
3. POST /api/subscriptions/cancel (Plan iptal)
4. POST /api/subscriptions/subscribe (Yeni plan)
```

Bu dokümantasyon ile ZiraAI Plant Analysis API'sini tam olarak kullanabilir, rol ve yetki sistemini anlayabilir, subscription sistemini yönetebilirsiniz.