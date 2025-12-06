# iyzico Payment Integration - Comprehensive Analysis & Recommendations

**Project:** ZiraAI - Subscription Package Purchase Flow  
**Date:** 2025-11-21  
**Analysis Scope:** Mobile App → API → iyzico Payment Gateway Integration  
**Documentation Sources:** https://docs.iyzico.com/ (Comprehensive review completed)

---

## Executive Summary

This document provides a comprehensive analysis of integrating iyzico payment gateway into ZiraAI's mobile app subscription purchase flow. After extensive review of iyzico documentation and current codebase, I present **3 architectural alternatives** with detailed implementation strategies, security considerations, and recommendations.

**Current State:** The API has a purchase endpoint that accepts payment info as strings (no actual payment processing).  
**Goal:** Enable real payment processing through iyzico gateway with mobile app support.

---

## Table of Contents

1. [Current System Analysis](#1-current-system-analysis)
2. [iyzico Payment Methods Overview](#2-iyzico-payment-methods-overview)
3. [Architecture Alternatives](#3-architecture-alternatives)
4. [Recommended Solution (Alternative 1)](#4-recommended-solution-alternative-1)
5. [Implementation Roadmap](#5-implementation-roadmap)
6. [Security & Compliance](#6-security--compliance)
7. [Testing Strategy](#7-testing-strategy)
8. [Cost Analysis](#8-cost-analysis)

---

## 1. Current System Analysis

### 1.1 Existing Purchase Flow

**Endpoint:** `POST /api/v{version}/sponsorship/purchase-package`  
**Handler:** [PurchaseBulkSponsorshipCommand.cs](../Business/Handlers/Sponsorship/Commands/PurchaseBulkSponsorshipCommand.cs:1)  
**Service:** [SponsorshipService.cs](../Business/Services/Sponsorship/SponsorshipService.cs:38-150)

**Current Flow:**
```
Mobile App → API Endpoint → Service Layer
                                ↓
                          Mock Payment (auto-approve)
                                ↓
                          Generate Sponsorship Codes
                                ↓
                          Return Success
```

**Issues with Current Implementation:**
- ❌ No actual payment processing - just accepts `PaymentMethod` and `PaymentReference` as strings
- ❌ Auto-approves all purchases (mock payment)
- ❌ No payment verification
- ❌ No fraud protection
- ❌ No transaction security

### 1.2 Current Data Model

**SubscriptionTier Entity** - [SubscriptionTier.cs](../Entities/Concrete/SubscriptionTier.cs:5-42)
```csharp
- MonthlyPrice, YearlyPrice: decimal
- Currency: string (TRY, USD, EUR)
- MinPurchaseQuantity, MaxPurchaseQuantity: int
- DailyRequestLimit, MonthlyRequestLimit: int
```

**SponsorshipPurchase Entity:**
```csharp
- PaymentMethod: string (currently just stored)
- PaymentReference: string (currently just stored)
- PaymentStatus: "Pending" → "Completed" (mock)
- TotalAmount: decimal
```

---

## 2. iyzico Payment Methods Overview

### 2.1 Available Payment Methods

iyzico offers multiple payment integration methods. Here's the complete analysis:

| Method | Best For | Mobile Support | Integration Complexity | PCI Compliance | Recommended? |
|--------|----------|----------------|----------------------|----------------|--------------|
| **PWI (Pay With iyzico)** | Mobile Apps | ✅ Excellent | ⭐⭐ Low | ✅ iyzico handles | ✅ **YES** |
| **Checkout Form (CF)** | Web Apps | ⚠️ Limited | ⭐⭐ Low | ✅ iyzico handles | ⚠️ Fallback |
| **Direct Charge (3DS)** | Custom UI | ⚠️ Complex | ⭐⭐⭐⭐ High | ⚠️ You handle | ❌ No |
| **Direct Charge (Non-3DS)** | Low amounts | ⚠️ Complex | ⭐⭐⭐⭐ High | ⚠️ You handle | ❌ No |
| **Tokenization** | Recurring | ⚠️ Complex | ⭐⭐⭐⭐⭐ Very High | ⚠️ You handle | 🔮 Future |
| **Subscription API** | Recurring | ✅ Good | ⭐⭐⭐ Medium | ✅ iyzico handles | 🔮 Future |

### 2.2 PWI (Pay With iyzico) - Detailed Analysis

**Why PWI is Best for Mobile Apps:**
- ✅ **No PCI Compliance Burden:** iyzico hosts the payment form
- ✅ **Native Mobile Support:** Supports MOBILE, MOBILE_IOS, MOBILE_ANDROID channels
- ✅ **Deep Link Integration:** Return to app after payment via callback URL
- ✅ **Secure:** Users enter card details on iyzico's secure page
- ✅ **Simple Integration:** Just 2 API calls (Initialize + Retrieve)
- ✅ **No Card Data Handling:** Backend never sees card numbers

**PWI Implementation Flow:**
```
1. Initialize Payment (Backend → iyzico)
   POST /payment/pay-with-iyzico/initialize
   ↓
   Returns: payWithIyzicoPageUrl + token

2. Open Payment Page (Mobile App)
   WebView or Browser opens iyzico payment page
   User enters card details on iyzico's secure form
   ↓
   iyzico processes payment with 3DS authentication

3. Return to App (Deep Link)
   iyzico redirects to callbackUrl with token
   App receives deep link and extracts token
   ↓
   App sends token to backend

4. Verify Payment (Backend → iyzico)
   POST /payment/iyzipos/checkoutform/auth/ecom/detail
   ↓
   Returns: Complete payment details + status (SUCCESS/FAILURE)
```

### 2.3 iyzico Authentication (HMACSHA256)

**Required for ALL API calls:**

```csharp
// Step 1: Generate random key
string randomKey = DateTime.Now.Ticks.ToString() + "123456789";

// Step 2: Create payload
string payload = randomKey + "/payment/pay-with-iyzico/initialize" + jsonBody;

// Step 3: HMAC-SHA256 signature
using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
string signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

// Step 4: Base64 encode auth string
string authString = $"apiKey:{apiKey}&randomKey:{randomKey}&signature:{signature}";
string authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

// Step 5: Set headers
headers["Authorization"] = "IYZWSv2 " + authorization;
headers["x-iyzi-rnd"] = randomKey;
```

---

## 3. Architecture Alternatives

### Alternative 1: PWI with Deep Link (⭐ RECOMMENDED)

**Best for:** Mobile-first applications requiring secure, PCI-compliant payments with minimal backend complexity.

**Architecture Diagram:**
```
┌─────────────────┐
│  Flutter App    │
└────────┬────────┘
         │ 1. Purchase Request (tierId, quantity)
         ↓
┌─────────────────┐
│   ZiraAI API    │
└────────┬────────┘
         │ 2. Initialize PWI
         ↓
┌─────────────────┐
│  iyzico API     │ POST /payment/pay-with-iyzico/initialize
└────────┬────────┘
         │ 3. Returns payWithIyzicoPageUrl + token
         ↓
┌─────────────────┐
│  Flutter App    │ Opens WebView/Browser
└────────┬────────┘
         │ 4. User completes payment on iyzico
         ↓
┌─────────────────┐
│  iyzico Page    │ Secure payment form with 3DS
└────────┬────────┘
         │ 5. Redirects to ziraai://payment-callback?token=xxx
         ↓
┌─────────────────┐
│  Flutter App    │ Deep link handler catches return
└────────┬────────┘
         │ 6. Send token to backend
         ↓
┌─────────────────┐
│   ZiraAI API    │ Verify payment with token
└────────┬────────┘
         │ 7. Retrieve payment status
         ↓
┌─────────────────┐
│  iyzico API     │ POST /payment/iyzipos/checkoutform/auth/ecom/detail
└────────┬────────┘
         │ 8. Returns payment details + SUCCESS/FAILURE
         ↓
┌─────────────────┐
│   ZiraAI API    │ Generate codes if SUCCESS
└────────┬────────┘
         │ 9. Return purchase result
         ↓
┌─────────────────┐
│  Flutter App    │ Show success/failure to user
└─────────────────┘
```

**Implementation Phases:**

**Phase 1: Backend - PWI Initialize Endpoint**
- New endpoint: `POST /api/v{version}/payment/initialize-purchase`
- Input: `{ tierId, quantity, userId }`
- Business logic:
  - Validate tier exists and is purchasable
  - Validate quantity within limits
  - Calculate total amount
  - Get user details for buyer info
  - Call iyzico PWI Initialize API
  - Store pending payment in database
  - Return `{ paymentToken, paymentUrl, purchaseId }`

**Phase 2: Backend - iyzico Service Layer**
```csharp
public interface IIyzicoPaymentService
{
    Task<IDataResult<PaymentInitializeResponse>> InitializePWIAsync(
        PaymentInitializeRequest request);
    
    Task<IDataResult<PaymentVerificationResponse>> VerifyPaymentAsync(
        string token);
    
    Task<IDataResult<WebhookValidationResponse>> ValidateWebhookAsync(
        string signature, object payload);
}
```

**Phase 3: Mobile App - Deep Link Setup**
```yaml
# android/app/src/main/AndroidManifest.xml
<intent-filter>
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="ziraai"
          android:host="payment-callback" />
</intent-filter>

# ios/Runner/Info.plist
<key>CFBundleURLTypes</key>
<array>
  <dict>
    <key>CFBundleURLSchemes</key>
    <array>
      <string>ziraai</string>
    </array>
  </dict>
</array>
```

**Phase 4: Mobile App - Payment Flow**
```dart
// 1. Initialize payment
final response = await api.initializePurchase(tierId, quantity);
final paymentUrl = response.paymentUrl;
final purchaseId = response.purchaseId;

// 2. Open WebView
await Navigator.push(
  context,
  MaterialPageRoute(
    builder: (_) => PaymentWebView(url: paymentUrl),
  ),
);

// 3. Deep link handler (in main.dart)
@override
void initState() {
  _handleIncomingLinks();
}

void _handleIncomingLinks() {
  _sub = uriLinkStream.listen((Uri? uri) {
    if (uri?.scheme == 'ziraai' && uri?.host == 'payment-callback') {
      final token = uri?.queryParameters['token'];
      _verifyPayment(token);
    }
  });
}

// 4. Verify payment
Future<void> _verifyPayment(String token) async {
  final result = await api.verifyPayment(token);
  if (result.success) {
    // Show success, navigate to codes
  } else {
    // Show failure
  }
}
```

**Pros:**
- ✅ **Simplest backend implementation**
- ✅ **No PCI compliance burden**
- ✅ **Native mobile experience with deep links**
- ✅ **iyzico handles 3DS authentication**
- ✅ **Secure - backend never sees card data**
- ✅ **Fastest time to market (1-2 weeks)**

**Cons:**
- ⚠️ Requires user to leave app briefly (WebView)
- ⚠️ Deep link setup needed per platform
- ⚠️ Network dependency (user must have internet)

**Cost:** Low (~2 weeks development)

---

### Alternative 2: PWI with Webhook Verification

**Enhancement to Alternative 1 with added security and reliability.**

**Architecture Changes:**
- Everything from Alternative 1
- **PLUS:** iyzico sends webhook to our backend when payment completes
- **PLUS:** We verify payment via BOTH token (from app) AND webhook (from iyzico)
- **PLUS:** Codes generated ONLY after webhook confirmation

**Additional Components:**

**Webhook Endpoint:**
```csharp
[HttpPost("webhooks/iyzico")]
[AllowAnonymous]
public async Task<IActionResult> IyzicoWebhook([FromHeader(Name = "X-IYZ-SIGNATURE-V3")] string signature)
{
    var body = await ReadBodyAsync();
    
    // Validate signature
    if (!_iyzicoService.ValidateWebhookSignature(signature, body))
        return Unauthorized();
    
    var webhook = JsonConvert.DeserializeObject<IyzicoWebhookDto>(body);
    
    // Process based on event type
    if (webhook.iyziEventType == "CHECKOUT_FORM_AUTH")
    {
        await _paymentService.ProcessPaymentWebhookAsync(webhook);
    }
    
    return Ok();
}
```

**Payment Flow Enhancement:**
```
User Completes Payment on iyzico
        ↓
  ┌─────┴─────┐
  │           │
  ↓           ↓
Deep Link    Webhook (15sec later)
to App       to Backend
  ↓           ↓
Backend ←─────┘
Verify
  ↓
Generate Codes (only after BOTH confirmations)
```

**Pros:**
- ✅ All benefits from Alternative 1
- ✅ **Double verification** (token + webhook)
- ✅ **Handles edge cases** (user closes app, network issues)
- ✅ **Automatic retry** (webhook retries 3 times)
- ✅ **More reliable** for production

**Cons:**
- ⚠️ Slightly more complex backend
- ⚠️ Need to configure webhook URL in iyzico merchant panel
- ⚠️ Need HTTPS endpoint (already have via Railway)

**Cost:** Medium (~3 weeks development)

---

### Alternative 3: Subscription API (Future-Proof)

**Best for:** Long-term recurring subscriptions with automatic renewals.

**Use Case:** If we want to change from "sponsor buys codes" to "sponsor subscribes monthly/yearly."

**Architecture:**
```
Backend creates iyzico Subscription Product + Pricing Plans
        ↓
Sponsor chooses plan (monthly/yearly)
        ↓
Initialize subscription (iyzico checkout form)
        ↓
iyzico automatically charges every month/year
        ↓
Webhook notifies backend of each charge
        ↓
Backend generates new codes automatically
```

**iyzico Subscription API Endpoints:**
- `POST /v2/subscription/products` - Create subscription product
- `POST /v2/subscription/products/{ref}/pricing-plans` - Create plans
- `POST /v2/subscription/initialize` - Start subscription
- `POST /v2/subscription/subscriptions/{ref}/upgrade` - Change plan
- `POST /v2/subscription/subscriptions/{ref}/cancel` - Cancel subscription

**Pricing Plan Configuration:**
```json
{
  "name": "ZiraAI Medium Tier - Monthly",
  "price": 199.00,
  "currencyCode": "TRY",
  "paymentInterval": "MONTHLY",
  "planPaymentType": "RECURRING",
  "trialPeriodDays": 0,
  "recurrenceCount": null  // Continues until canceled
}
```

**Pros:**
- ✅ **Recurring revenue** - automatic renewals
- ✅ **Better for sponsors** - no need to remember to renew
- ✅ **Predictable income** for ZiraAI
- ✅ **Trial periods** supported (free trial before charging)
- ✅ **Upgrade/downgrade** plans supported

**Cons:**
- ⚠️ More complex implementation
- ⚠️ Changes business model significantly
- ⚠️ Requires subscription management UI
- ⚠️ Need to handle failed payments, cancellations

**Cost:** High (~6-8 weeks development)

**Recommendation:** **NOT NOW** - Use Alternative 1 or 2 first, then migrate to subscriptions later if business model changes.

---

## 4. Recommended Solution (Alternative 1)

### 4.1 Why Alternative 1 (PWI with Deep Link)?

**Decision Factors:**
1. **Time to Market:** Fastest implementation (~2 weeks)
2. **Security:** PCI compliant, no card data in our backend
3. **Mobile-First:** Designed for mobile app integration
4. **Simplicity:** Minimal backend changes
5. **Proven:** Used by many Turkish mobile apps successfully

**When to Upgrade to Alternative 2:**
- After initial launch is stable
- If we see payment verification issues
- Before scaling to high transaction volumes
- When we need bulletproof reliability

### 4.2 Database Schema Changes

**New Table: PaymentTransactions**
```sql
CREATE TABLE PaymentTransactions (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL,
    PurchaseId INT NULL,  -- Links to SponsorshipPurchase after success
    
    -- iyzico data
    IyzicoToken VARCHAR(255) NOT NULL UNIQUE,
    IyzicoPaymentId VARCHAR(255) NULL,
    ConversationId VARCHAR(100) NOT NULL,
    
    -- Payment details
    Amount DECIMAL(18, 2) NOT NULL,
    Currency VARCHAR(3) NOT NULL,
    Status VARCHAR(50) NOT NULL,  -- Initialized, Pending, Success, Failed, Expired
    
    -- Timestamps
    InitializedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CompletedAt DATETIME2 NULL,
    ExpiresAt DATETIME2 NOT NULL,  -- Token expires in 10 minutes
    
    -- Response data (JSON)
    InitializeResponse NVARCHAR(MAX) NULL,
    VerifyResponse NVARCHAR(MAX) NULL,
    
    -- Audit
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedDate DATETIME2 NULL,
    
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (PurchaseId) REFERENCES SponsorshipPurchases(Id),
    INDEX IX_Token (IyzicoToken),
    INDEX IX_Status_Expires (Status, ExpiresAt)
);
```

**Modified Table: SponsorshipPurchase**
```sql
ALTER TABLE SponsorshipPurchases
ADD PaymentTransactionId INT NULL,
ADD IyzicoPaymentId VARCHAR(255) NULL;

ALTER TABLE SponsorshipPurchases
ADD FOREIGN KEY (PaymentTransactionId) REFERENCES PaymentTransactions(Id);
```

### 4.3 Configuration (appsettings.json)

**Development:**
```json
{
  "Iyzico": {
    "BaseUrl": "https://sandbox-api.iyzipay.com",
    "ApiKey": "sandbox-xxx",
    "SecretKey": "sandbox-yyy",
    "PaymentCallbackUrl": "ziraai://payment-callback",
    "WebhookUrl": "https://ziraai-api-dev.railway.app/api/v1/webhooks/iyzico"
  }
}
```

**Production:**
```json
{
  "Iyzico": {
    "BaseUrl": "https://api.iyzipay.com",
    "ApiKey": "${IYZICO_API_KEY}",  // Railway env var
    "SecretKey": "${IYZICO_SECRET_KEY}",  // Railway env var
    "PaymentCallbackUrl": "ziraai://payment-callback",
    "WebhookUrl": "https://api.ziraai.com/api/v1/webhooks/iyzico"
  }
}
```

### 4.4 API Endpoints Design

**New Endpoints:**

**1. Initialize Payment**
```
POST /api/v1/payment/initialize-purchase
Authorization: Bearer {jwt}

Request:
{
  "subscriptionTierId": 2,
  "quantity": 50,
  "companyName": "Örnek Tarım A.Ş.",
  "invoiceAddress": "Kadıköy, İstanbul",
  "taxNumber": "1234567890"
}

Response (Success):
{
  "success": true,
  "data": {
    "purchaseId": 123,
    "paymentToken": "d9d9fc30-8178-4ca9-8f93-1b150f465da6",
    "paymentUrl": "https://sandbox-ode.iyzico.com/sdk?token=...",
    "expiresAt": "2025-11-21T15:35:00Z",
    "amount": 9950.00,
    "currency": "TRY"
  }
}

Response (Error):
{
  "success": false,
  "message": "Quantity must be at least 10 for Medium tier"
}
```

**2. Verify Payment**
```
POST /api/v1/payment/verify-payment
Authorization: Bearer {jwt}

Request:
{
  "paymentToken": "d9d9fc30-8178-4ca9-8f93-1b150f465da6"
}

Response (Success):
{
  "success": true,
  "data": {
    "purchaseId": 123,
    "paymentStatus": "SUCCESS",
    "codes": [
      {
        "code": "AGRI-ABC123",
        "expiresAt": "2025-12-21T00:00:00Z"
      },
      // ... 49 more codes
    ]
  }
}

Response (Failure):
{
  "success": false,
  "message": "Payment failed: Insufficient funds"
}
```

**3. Get Payment Status (Optional - for polling)**
```
GET /api/v1/payment/status/{paymentToken}
Authorization: Bearer {jwt}

Response:
{
  "success": true,
  "data": {
    "status": "Pending",  // Initialized, Pending, Success, Failed, Expired
    "message": "Payment is being processed..."
  }
}
```

### 4.5 Service Layer Implementation

**IyzicoPaymentService.cs** (new)
```csharp
public class IyzicoPaymentService : IIyzicoPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<IyzicoPaymentService> _logger;
    
    private string BaseUrl => _configuration["Iyzico:BaseUrl"];
    private string ApiKey => _configuration["Iyzico:ApiKey"];
    private string SecretKey => _configuration["Iyzico:SecretKey"];
    private string CallbackUrl => _configuration["Iyzico:PaymentCallbackUrl"];
    
    public async Task<IDataResult<PaymentInitializeResponse>> InitializePWIAsync(
        PaymentInitializeRequest request)
    {
        try
        {
            // Build iyzico request
            var iyzicoRequest = new
            {
                locale = "tr",
                conversationId = request.ConversationId,
                price = request.Amount.ToString("F2", CultureInfo.InvariantCulture),
                paidPrice = request.Amount.ToString("F2", CultureInfo.InvariantCulture),
                currency = request.Currency,
                basketId = $"BASKET-{request.PurchaseId}",
                paymentGroup = "SUBSCRIPTION",
                paymentChannel = "MOBILE_ANDROID",  // or MOBILE_IOS based on user agent
                callbackUrl = CallbackUrl,
                enabledInstallments = new[] { 1 },  // No installments for now
                buyer = new
                {
                    id = request.UserId.ToString(),
                    name = request.BuyerName,
                    surname = request.BuyerSurname,
                    email = request.BuyerEmail,
                    gsmNumber = request.BuyerPhone,
                    identityNumber = request.BuyerIdentityNumber ?? "11111111111",
                    registrationAddress = request.BillingAddress,
                    city = request.City,
                    country = "Turkey",
                    ip = request.IpAddress
                },
                billingAddress = new
                {
                    contactName = request.BuyerName + " " + request.BuyerSurname,
                    city = request.City,
                    country = "Turkey",
                    address = request.BillingAddress
                },
                basketItems = new[]
                {
                    new
                    {
                        id = $"TIER-{request.TierId}",
                        name = $"{request.TierName} Subscription ({request.Quantity} codes)",
                        category1 = "Subscription",
                        itemType = "VIRTUAL",
                        price = request.Amount.ToString("F2", CultureInfo.InvariantCulture)
                    }
                }
            };
            
            var jsonBody = JsonConvert.SerializeObject(iyzicoRequest);
            
            // Generate authentication
            var auth = GenerateAuthentication("/payment/pay-with-iyzico/initialize", jsonBody);
            
            // Make request
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, 
                $"{BaseUrl}/payment/pay-with-iyzico/initialize");
            httpRequest.Headers.Add("Authorization", auth.Authorization);
            httpRequest.Headers.Add("x-iyzi-rnd", auth.RandomKey);
            httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("[Iyzico] Initialize response: {Response}", responseBody);
            
            var iyzicoResponse = JsonConvert.DeserializeObject<IyzicoInitializeResponse>(responseBody);
            
            if (iyzicoResponse.status == "success")
            {
                return new SuccessDataResult<PaymentInitializeResponse>(new PaymentInitializeResponse
                {
                    Token = iyzicoResponse.token,
                    PaymentUrl = iyzicoResponse.payWithIyzicoPageUrl,
                    ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(iyzicoResponse.tokenExpireDate).DateTime,
                    RawResponse = responseBody
                });
            }
            
            return new ErrorDataResult<PaymentInitializeResponse>(
                $"iyzico error: {iyzicoResponse.errorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Iyzico] Initialize PWI failed");
            return new ErrorDataResult<PaymentInitializeResponse>($"Payment initialization failed: {ex.Message}");
        }
    }
    
    public async Task<IDataResult<PaymentVerificationResponse>> VerifyPaymentAsync(string token)
    {
        try
        {
            var request = new
            {
                locale = "tr",
                conversationId = Guid.NewGuid().ToString(),
                token = token
            };
            
            var jsonBody = JsonConvert.SerializeObject(request);
            var auth = GenerateAuthentication("/payment/iyzipos/checkoutform/auth/ecom/detail", jsonBody);
            
            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{BaseUrl}/payment/iyzipos/checkoutform/auth/ecom/detail");
            httpRequest.Headers.Add("Authorization", auth.Authorization);
            httpRequest.Headers.Add("x-iyzi-rnd", auth.RandomKey);
            httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("[Iyzico] Verify response: {Response}", responseBody);
            
            var iyzicoResponse = JsonConvert.DeserializeObject<IyzicoPaymentDetailResponse>(responseBody);
            
            if (iyzicoResponse.status == "success" && iyzicoResponse.paymentStatus == "SUCCESS")
            {
                return new SuccessDataResult<PaymentVerificationResponse>(new PaymentVerificationResponse
                {
                    IsSuccess = true,
                    PaymentId = iyzicoResponse.paymentId,
                    Amount = iyzicoResponse.paidPrice,
                    Currency = iyzicoResponse.currency,
                    CardBin = iyzicoResponse.binNumber,
                    CardLastFour = iyzicoResponse.lastFourDigits,
                    FraudStatus = iyzicoResponse.fraudStatus,
                    RawResponse = responseBody
                });
            }
            
            return new SuccessDataResult<PaymentVerificationResponse>(new PaymentVerificationResponse
            {
                IsSuccess = false,
                ErrorMessage = iyzicoResponse.errorMessage ?? "Payment failed",
                RawResponse = responseBody
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Iyzico] Verify payment failed");
            return new ErrorDataResult<PaymentVerificationResponse>($"Payment verification failed: {ex.Message}");
        }
    }
    
    private (string Authorization, string RandomKey) GenerateAuthentication(string uri, string body)
    {
        // Generate random key
        var randomKey = DateTime.Now.Ticks.ToString() + new Random().Next(100000, 999999).ToString();
        
        // Create payload: randomKey + uri + body
        var payload = randomKey + uri + body;
        
        // HMAC-SHA256 signature
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        
        // Create auth string
        var authString = $"apiKey:{ApiKey}&randomKey:{randomKey}&signature:{signature}";
        var authorization = "IYZWSv2 " + Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
        
        return (authorization, randomKey);
    }
}
```

---

## 5. Implementation Roadmap

### Phase 1: Backend Foundation (Week 1)

**Days 1-2: Setup & Configuration**
- [ ] Add Iyzico configuration to appsettings
- [ ] Create PaymentTransactions table migration
- [ ] Add PaymentTransactionId to SponsorshipPurchase
- [ ] Register for iyzico sandbox account
- [ ] Get sandbox API keys

**Days 3-5: Service Layer**
- [ ] Create IyzicoPaymentService interface
- [ ] Implement HMACSHA256 authentication
- [ ] Implement InitializePWI method
- [ ] Implement VerifyPayment method
- [ ] Add comprehensive logging
- [ ] Unit tests for authentication

**Days 6-7: API Endpoints**
- [ ] Create PaymentController
- [ ] Implement InitializePurchase endpoint
- [ ] Implement VerifyPayment endpoint
- [ ] Implement GetPaymentStatus endpoint
- [ ] Add authorization checks
- [ ] Integration tests

### Phase 2: Mobile App Integration (Week 2)

**Days 1-2: Deep Link Setup**
- [ ] Configure Android deep links (AndroidManifest.xml)
- [ ] Configure iOS universal links (Info.plist)
- [ ] Test deep link opening from browser
- [ ] Handle deep link parameters

**Days 3-4: Payment UI**
- [ ] Create payment initiation screen
- [ ] Create WebView for iyzico payment page
- [ ] Handle WebView loading states
- [ ] Handle payment success/failure screens

**Days 5-7: Integration & Testing**
- [ ] Integrate with backend endpoints
- [ ] Test complete flow end-to-end
- [ ] Handle edge cases (network errors, user cancellation)
- [ ] Add analytics tracking
- [ ] User acceptance testing

### Phase 3: Production Deployment (Week 3)

**Days 1-2: Production Setup**
- [ ] Register for iyzico production account
- [ ] Submit required documents for verification
- [ ] Get production API keys
- [ ] Configure production webhook URL
- [ ] Update Railway environment variables

**Days 3-4: Testing & Validation**
- [ ] Test with real credit cards (small amounts)
- [ ] Verify webhook delivery
- [ ] Test 3DS authentication
- [ ] Load testing (simulate concurrent payments)

**Days 5-7: Rollout**
- [ ] Deploy to production
- [ ] Monitor first transactions closely
- [ ] Set up error alerting
- [ ] Update user documentation
- [ ] Train support team

---

## 6. Security & Compliance

### 6.1 PCI DSS Compliance

**Level of Compliance:**
- ✅ **Level 4 SAQ-A:** Because we use iyzico's hosted payment page (PWI)
- ✅ **No card data touches our servers** - iyzico handles everything
- ✅ **No PCI audit required** for our backend

**What We Must Do:**
- ✅ Use HTTPS for all API calls (already done via Railway)
- ✅ Validate iyzico webhook signatures
- ✅ Store only iyzico payment IDs, never card data
- ✅ Log access to payment endpoints

### 6.2 Security Measures

**1. HMAC-SHA256 Authentication**
- Every API call signed with secret key
- Random key prevents replay attacks
- Signature verification prevents tampering

**2. Token Expiration**
- Payment tokens expire in 10 minutes
- Prevents stale payment attempts
- Clean up expired transactions daily

**3. Webhook Signature Validation**
```csharp
public bool ValidateWebhookSignature(string signature, string body, string eventType, string paymentId)
{
    // Format: secretKey + eventType + paymentId + token + conversationId + status
    var payload = $"{SecretKey}{eventType}{paymentId}...";
    
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    var expectedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    
    return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
}
```

**4. Rate Limiting**
- Maximum 5 payment initializations per user per hour
- Prevents abuse and automated attacks

**5. Fraud Detection**
- iyzico provides fraudStatus in response
- We can reject high-risk transactions
- Monitor for suspicious patterns

### 6.3 Data Privacy (KVKK/GDPR)

**Personal Data We Collect:**
- Buyer name, email, phone (required by iyzico)
- IP address (required for fraud detection)
- Card BIN + last 4 digits (for display only, not full card)

**Storage:**
- Store in encrypted database (already done via SQL Server)
- Delete payment transaction details after 90 days (keep only purchase record)
- Provide data export on user request

---

## 7. Testing Strategy

### 7.1 Sandbox Testing

**iyzico Sandbox Environment:**
- Base URL: `https://sandbox-api.iyzipay.com`
- Test credit cards: https://docs.iyzico.com/en/testing

**Test Credit Cards:**
```
Success Card:
  Number: 5890040000000016
  CVV: 123
  Expiry: 12/30
  3DS Password: test

Insufficient Funds:
  Number: 5526080000000006
  CVV: 123
  Expiry: 12/30
```

### 7.2 Test Scenarios

**Happy Path:**
1. ✅ User selects Medium tier, quantity 50
2. ✅ Backend initializes payment successfully
3. ✅ User opens payment page in WebView
4. ✅ User enters card details
5. ✅ 3DS authentication succeeds
6. ✅ iyzico redirects to app with token
7. ✅ Backend verifies payment successfully
8. ✅ 50 sponsorship codes generated
9. ✅ User sees success screen

**Error Cases:**
1. ⚠️ Payment initialization fails (invalid tier)
2. ⚠️ User cancels on payment page
3. ⚠️ Insufficient funds
4. ⚠️ Card declined
5. ⚠️ 3DS authentication fails
6. ⚠️ Network timeout during verification
7. ⚠️ Token expired (>10 minutes)
8. ⚠️ Webhook signature invalid

**Edge Cases:**
1. 🔧 User closes app before payment completes
2. 🔧 User completes payment but token lost
3. 🔧 Duplicate verification attempts
4. 🔧 Concurrent payment attempts

### 7.3 Automated Tests

**Unit Tests:**
```csharp
[Fact]
public void GenerateAuthentication_ValidInput_ReturnsCorrectSignature()
{
    // Arrange
    var service = CreateService();
    var body = "{\"price\":\"100.00\"}";
    
    // Act
    var (auth, randomKey) = service.GenerateAuthentication("/test", body);
    
    // Assert
    Assert.StartsWith("IYZWSv2 ", auth);
    Assert.NotEmpty(randomKey);
}

[Fact]
public async Task InitializePWI_ValidRequest_ReturnsToken()
{
    // Arrange
    var mockHttp = CreateMockHttpClient(successResponse);
    var service = CreateService(mockHttp);
    
    // Act
    var result = await service.InitializePWIAsync(validRequest);
    
    // Assert
    Assert.True(result.Success);
    Assert.NotEmpty(result.Data.Token);
}
```

**Integration Tests:**
```csharp
[Fact]
public async Task CompletePurchaseFlow_ValidUser_GeneratesCodes()
{
    // Arrange
    var user = await CreateTestUser();
    var tier = await CreateTestTier();
    
    // Act - Initialize
    var initResponse = await client.PostAsync("/api/v1/payment/initialize-purchase", 
        new { subscriptionTierId = tier.Id, quantity = 10 });
    var initData = await initResponse.Content.ReadAsAsync<InitializeResponse>();
    
    // Simulate iyzico callback (in real test, would use sandbox)
    var verifyResponse = await client.PostAsync("/api/v1/payment/verify-payment",
        new { paymentToken = initData.PaymentToken });
    
    // Assert
    Assert.True(verifyResponse.IsSuccessStatusCode);
    var codes = await _db.SponsorshipCodes
        .Where(c => c.SponsorId == user.Id)
        .ToListAsync();
    Assert.Equal(10, codes.Count);
}
```

---

## 8. Cost Analysis

### 8.1 Development Costs

| Phase | Duration | Complexity | Risk |
|-------|----------|------------|------|
| Backend Foundation | 1 week | Medium | Low |
| Mobile App Integration | 1 week | Medium | Medium |
| Production Deployment | 1 week | Low | Low |
| **Total** | **3 weeks** | - | - |

### 8.2 iyzico Transaction Fees

**Standard Rates (approximate):**
- Credit Card (Domestic): 2.5% + 0.25 TRY per transaction
- Credit Card (International): 3.5% + 0.25 TRY per transaction
- No monthly fee
- No setup fee

**Example:**
- Purchase: 50 codes × 199 TRY = 9,950 TRY
- Fee: 9,950 × 2.5% + 0.25 = 249.00 TRY
- Net: 9,700.75 TRY

**Note:** Actual rates negotiated based on volume.

### 8.3 Operational Costs

- **API Calls:** Free (no per-call charges)
- **Webhook Bandwidth:** Negligible (<1 KB per webhook)
- **Database Storage:** ~1 KB per transaction (minimal)
- **Monitoring:** CloudWatch logs (existing infrastructure)

---

## 9. Comparison Matrix

| Criteria | Alternative 1 (PWI) | Alternative 2 (PWI+Webhook) | Alternative 3 (Subscription) |
|----------|---------------------|----------------------------|------------------------------|
| **Development Time** | 2 weeks ⭐ | 3 weeks | 8 weeks |
| **Complexity** | Low ⭐ | Medium | High |
| **Security** | High ⭐ | Very High ⭐ | Very High |
| **PCI Compliance** | Easy ⭐ | Easy ⭐ | Easy |
| **Mobile UX** | Good ⭐ | Good ⭐ | Good |
| **Reliability** | Good | Excellent ⭐ | Excellent |
| **Recurring Payments** | No | No | Yes ⭐ |
| **Maintenance** | Low ⭐ | Medium | High |
| **Cost** | Low ⭐ | Medium | High |

---

## 10. Final Recommendation

### ⭐ Go with Alternative 1 (PWI with Deep Link) NOW

**Reasons:**
1. ✅ Fastest time to market (2-3 weeks)
2. ✅ Lowest risk and complexity
3. ✅ Proven solution for mobile apps
4. ✅ PCI compliant out of the box
5. ✅ Can upgrade to Alternative 2 later easily

### 🔮 Upgrade to Alternative 2 after 3 months

**When:**
- After we have 100+ successful transactions
- When we see payment reliability issues
- Before scaling to high volume

### 🚀 Consider Alternative 3 in future

**When:**
- Business model changes to recurring subscriptions
- Sponsors request automatic renewals
- We want predictable recurring revenue

---

## 11. Next Steps

### Immediate Actions:

1. **Get User Approval** on this analysis and recommendation
2. **Register iyzico Sandbox Account** - Get test API keys
3. **Create Feature Branch** - `feature/iyzico-payment-integration`
4. **Start Phase 1** - Backend foundation

### Questions for User:

1. ❓ Do you approve Alternative 1 (PWI with Deep Link)?
2. ❓ Do you have existing iyzico account or should we create new?
3. ❓ What is the planned launch date for payment integration?
4. ❓ Any specific requirements for invoice generation after payment?
5. ❓ Should we support installment payments (taksit)?

---

## 12. References

### iyzico Documentation:
- Main Docs: https://docs.iyzico.com/
- PWI Implementation: https://docs.iyzico.com/en/payment-methods/paywithiyzico/pwi-implementation
- Authentication: https://docs.iyzico.com/en/getting-started/preliminaries/authentication/hmacsha256-auth
- Webhook: https://docs.iyzico.com/en/advanced/webhook
- Subscription API: https://docs.iyzico.com/en/products/subscription

### Flutter Deep Link Resources:
- Flutter Deep Linking Guide: https://docs.flutter.dev/ui/navigation/deep-linking
- app_links Package: https://pub.dev/packages/app_links

### Related Files in Codebase:
- [PurchaseBulkSponsorshipCommand.cs](../Business/Handlers/Sponsorship/Commands/PurchaseBulkSponsorshipCommand.cs)
- [SponsorshipService.cs](../Business/Services/Sponsorship/SponsorshipService.cs)
- [SubscriptionTier.cs](../Entities/Concrete/SubscriptionTier.cs)
- [SponsorshipController.cs](../WebAPI/Controllers/SponsorshipController.cs:316)

---

**Document Version:** 1.0  
**Last Updated:** 2025-11-21  
**Author:** Claude (with comprehensive iyzico documentation review)  
**Status:** ✅ COMPLETE - Ready for user review and approval
