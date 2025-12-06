# Payment Implementation Complete Guide - iyzico Integration

**Date:** 2025-11-22  
**Flow:** Sponsor Bulk Purchase (Reference Implementation)  
**Payment Provider:** iyzico (Turkish Payment Gateway)  
**Purpose:** End-to-end payment implementation guide for integrating iyzico payment flow

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Database Schema](#database-schema)
4. [Backend Implementation](#backend-implementation)
5. [Mobile App Implementation](#mobile-app-implementation)
6. [Payment Flow Step-by-Step](#payment-flow-step-by-step)
7. [Error Handling](#error-handling)
8. [Security Considerations](#security-considerations)
9. [Testing Guide](#testing-guide)
10. [Deployment Checklist](#deployment-checklist)
11. [Troubleshooting](#troubleshooting)

---

## Overview

### What is This Flow?

The **Sponsor Bulk Purchase** flow allows sponsors to purchase sponsorship packages using credit cards via iyzico payment gateway. The flow includes:

- Payment initialization with user data
- 3D Secure authentication via WebView
- Callback processing and verification
- Database record creation
- Cache invalidation
- Mobile app deep link redirection

### Key Components

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Payment Gateway | iyzico Sandbox/Production | Turkish payment processing |
| Backend API | ASP.NET Core 9.0 | Payment orchestration |
| Mobile App | Flutter | User interface & WebView |
| Database | PostgreSQL | Transaction storage |
| Cache | Redis | Dashboard performance |
| Authentication | JWT Bearer | API security |

### Flow Types Supported

This guide uses **Sponsor Bulk Purchase** as reference, but the pattern applies to:

- ✅ Sponsor Bulk Purchase (implemented)
- 🔄 Farmer Subscription (similar pattern)
- 🔄 Dealer Package Purchase (future)
- 🔄 Premium Feature Unlock (future)

---

## Architecture

### High-Level Architecture

```
┌─────────────────┐
│   Mobile App    │
│   (Flutter)     │
└────────┬────────┘
         │ 1. Initialize Payment
         │ POST /api/v1/payments/initialize
         ▼
┌─────────────────────────────────────────┐
│         Backend API (.NET 9)             │
│  ┌────────────────────────────────┐     │
│  │  PaymentController             │     │
│  └───────┬────────────────────────┘     │
│          │                               │
│  ┌───────▼────────────────────────┐     │
│  │  IyzicoPaymentService          │     │
│  │  - CreatePaymentTransaction()  │     │
│  │  - GenerateHMACSignature()     │     │
│  │  - CallIyzicoAPI()             │     │
│  └───────┬────────────────────────┘     │
│          │                               │
│  ┌───────▼────────────────────────┐     │
│  │  PaymentTransactionRepository  │     │
│  └────────────────────────────────┘     │
└───────────┬─────────────────────────────┘
            │ 2. iyzico API Call
            ▼
┌─────────────────────────────────────────┐
│         iyzico API                       │
│  - Validate request signature            │
│  - Create payment session                │
│  - Return payment page URL & token       │
└───────────┬─────────────────────────────┘
            │ 3. Return payment URL
            ▼
┌─────────────────┐
│   Mobile App    │ 4. Open WebView
│   WebView       │ Load: https://sandbox-cpp.iyzipay.com?token=xxx
└────────┬────────┘
         │ 5. User fills card details
         │ 6. User clicks "Pay" button
         │ 7. 3D Secure authentication
         ▼
┌─────────────────────────────────────────┐
│      iyzico Payment Page                 │
│  - Collect card details                  │
│  - 3D Secure authentication              │
│  - Process payment                       │
└───────────┬─────────────────────────────┘
            │ 8. POST callback
            │ POST /api/v1/payments/callback
            ▼
┌─────────────────────────────────────────┐
│         Backend API (.NET 9)             │
│  ┌────────────────────────────────┐     │
│  │  PaymentController.Callback()  │     │
│  └───────┬────────────────────────┘     │
│          │                               │
│  ┌───────▼────────────────────────┐     │
│  │  IyzicoPaymentService          │     │
│  │  - VerifyPayment()             │     │
│  │  - ProcessSuccessfulPayment()  │     │
│  │  - CreatePurchaseRecord()      │     │
│  │  - GenerateCodes()             │     │
│  │  - InvalidateCache()           │     │
│  └────────────────────────────────┘     │
└───────────┬─────────────────────────────┘
            │ 9. HTTP 302 Redirect
            │ Location: ziraai://payment-callback?token=xxx&status=success
            ▼
┌─────────────────┐
│   Mobile App    │ 10. Deep link opens app
│   - Verify      │ 11. GET /api/v1/payments/verify
│   - Show result │ 12. Display success screen
└─────────────────┘
```

### Component Responsibilities

#### 1. Mobile App (Flutter)
- Collect purchase details (tier, quantity, invoice info)
- Initialize payment request
- Display payment WebView
- Handle deep link callbacks
- Verify payment completion
- Show success/error screens

#### 2. Backend API (.NET Core)
- Validate user authentication
- Create payment transactions
- Generate HMAC signatures for iyzico
- Process payment callbacks
- Verify payment results
- Create business records (purchases, codes)
- Invalidate caches
- Return deep link redirects

#### 3. iyzico Payment Gateway
- Validate API requests
- Host payment forms
- Process credit card payments
- Handle 3D Secure authentication
- Send payment callbacks
- Provide verification API

#### 4. Database (PostgreSQL)
- Store payment transactions
- Store business records (purchases, codes)
- Maintain audit trail
- Enable analytics and reporting

#### 5. Cache (Redis)
- Cache dashboard data (24h TTL)
- Improve API performance
- Require invalidation after purchases

---

## Database Schema

### 1. PaymentTransaction Table

**Purpose:** Track all payment attempts and their lifecycle

**File:** `Entities/Concrete/PaymentTransaction.cs`

```csharp
public class PaymentTransaction : IEntity
{
    public int Id { get; set; }
    
    // User & Flow
    public int UserId { get; set; }
    public string FlowType { get; set; }           // "SponsorBulkPurchase", "FarmerSubscription"
    public string FlowDataJson { get; set; }       // Serialized flow-specific data
    
    // Payment Details
    public decimal Amount { get; set; }
    public string Currency { get; set; }           // "TRY", "USD"
    public string Status { get; set; }             // "Pending", "Success", "Failed", "Cancelled"
    
    // iyzico Integration
    public string IyzicoToken { get; set; }        // Payment session token
    public string IyzicoPaymentId { get; set; }    // iyzico payment ID (after success)
    public string IyzicoConversationId { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    
    // Error Tracking
    public string ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
    
    // Business Links
    public int? SponsorshipPurchaseId { get; set; }
    public int? UserSubscriptionId { get; set; }
    
    // Navigation Properties
    public virtual User User { get; set; }
    public virtual SponsorshipPurchase SponsorshipPurchase { get; set; }
    public virtual UserSubscription UserSubscription { get; set; }
}
```

**Key Fields Explanation:**

| Field | Type | Purpose | Example |
|-------|------|---------|---------|
| `FlowType` | string | Identifies payment purpose | "SponsorBulkPurchase" |
| `FlowDataJson` | string | Serialized flow-specific data | `{"SubscriptionTierId":1,"Quantity":50}` |
| `IyzicoToken` | string | Payment session token from iyzico | "2b602b20-8952-4c9b-9a55-f1e5164538c9" |
| `Status` | string | Current payment state | "Pending" → "Success" |
| `SponsorshipPurchaseId` | int? | Link to business record (if sponsor flow) | 39 |

**Status Lifecycle:**

```
Pending ──┬──> Success ──> CompletedAt set
          │
          ├──> Failed ──> ErrorCode & ErrorMessage set
          │
          └──> Cancelled ──> CancelledAt set
```

### 2. SponsorshipPurchase Table

**Purpose:** Store sponsor package purchase records

**File:** `Entities/Concrete/SponsorshipPurchase.cs`

```csharp
public class SponsorshipPurchase : IEntity
{
    public int Id { get; set; }
    
    // Sponsor & Tier
    public int SponsorId { get; set; }
    public int SubscriptionTierId { get; set; }
    public int Quantity { get; set; }              // Number of codes purchased
    
    // Pricing
    public decimal UnitPrice { get; set; }         // Price per code
    public decimal TotalAmount { get; set; }       // Total payment amount
    public string Currency { get; set; }           // "TRY"
    
    // Invoice Information (NEW - Added 2025-11-22)
    public string CompanyName { get; set; }        // Optional: For corporate invoices
    public string TaxNumber { get; set; }          // Optional: Tax ID
    public string InvoiceAddress { get; set; }     // Optional: Invoice address
    public string InvoiceNumber { get; set; }      // Generated invoice number
    
    // Payment Details
    public DateTime PurchaseDate { get; set; }
    public string PaymentMethod { get; set; }      // "CreditCard", "BankTransfer"
    public string PaymentReference { get; set; }   // iyzico payment ID
    public string PaymentStatus { get; set; }      // "Completed", "Pending", "Failed"
    public DateTime? PaymentCompletedDate { get; set; }
    
    // Code Management
    public string CodePrefix { get; set; }         // "AGRI"
    public int ValidityDays { get; set; }          // 30, 60, 90
    public string Status { get; set; }             // "Active", "Expired", "Cancelled"
    public int CodesGenerated { get; set; }        // Actual number of codes created
    public int CodesUsed { get; set; }             // Number of codes redeemed
    
    // Audit
    public DateTime CreatedDate { get; set; }
    public int? PaymentTransactionId { get; set; }
    
    // Navigation Properties
    public virtual User Sponsor { get; set; }
    public virtual SubscriptionTier SubscriptionTier { get; set; }
    public virtual PaymentTransaction PaymentTransaction { get; set; }
    public virtual ICollection<SponsorshipCode> SponsorshipCodes { get; set; }
}
```

**Invoice Fields (Personal vs Corporate):**

| Scenario | CompanyName | TaxNumber | InvoiceAddress | InvoiceNumber |
|----------|-------------|-----------|----------------|---------------|
| Personal Purchase | NULL | NULL | NULL | Auto-generated |
| Corporate Purchase | "ABC Ltd." | "1234567890" | "Istanbul, Turkey" | Auto-generated |

### 3. Flow Data DTOs

**Purpose:** Define structure for `FlowDataJson` in PaymentTransaction

**File:** `Entities/Dtos/Payment/PaymentInitializeRequestDto.cs`

```csharp
/// <summary>
/// Flow data for sponsor bulk purchase
/// Serialized to JSON and stored in PaymentTransaction.FlowDataJson
/// </summary>
public class SponsorBulkPurchaseFlowData
{
    [Required]
    public int SubscriptionTierId { get; set; }

    [Required]
    [Range(1, 10000)]
    public int Quantity { get; set; }

    /// <summary>
    /// Company name for invoice (optional for personal purchases)
    /// </summary>
    public string CompanyName { get; set; }

    /// <summary>
    /// Tax number for invoice (optional for personal purchases)
    /// </summary>
    public string TaxNumber { get; set; }

    /// <summary>
    /// Invoice address (optional for personal purchases)
    /// </summary>
    public string InvoiceAddress { get; set; }
}
```

**Example Serialized Data:**

```json
{
  "SubscriptionTierId": 1,
  "Quantity": 50,
  "CompanyName": "ZiraAI Tech Ltd.",
  "TaxNumber": "1234567890",
  "InvoiceAddress": "Istanbul, Turkey"
}
```

### Database Relationships

```
User ────┬────< PaymentTransaction
         │
         └────< SponsorshipPurchase ────< SponsorshipCode
                      │
                      └────> SubscriptionTier

PaymentTransaction ──────> SponsorshipPurchase (1:1 link after success)
```

---

## Backend Implementation

### 1. Payment Controller

**File:** `WebAPI/Controllers/PaymentController.cs`

**Purpose:** HTTP endpoints for payment operations

#### Endpoint 1: Initialize Payment

```csharp
[HttpPost("initialize")]
[Authorize]
public async Task<IActionResult> InitializePayment([FromBody] PaymentInitializeRequestDto request)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    
    _logger.LogInformation("[Payment] Initialize payment request. UserId: {UserId}, FlowType: {FlowType}",
        userId, request.FlowType);

    try
    {
        var result = await _paymentService.InitializePaymentAsync(userId, request);

        if (!result.Success)
        {
            _logger.LogWarning("[Payment] Payment initialization failed. UserId: {UserId}, Error: {Error}",
                userId, result.Message);
            return BadRequest(new { error = result.Message });
        }

        _logger.LogInformation("[Payment] Payment initialized successfully. UserId: {UserId}, TransactionId: {TransactionId}",
            userId, result.Data.TransactionId);

        return Ok(result.Data);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[Payment] Error initializing payment. UserId: {UserId}", userId);
        return StatusCode(500, new { error = "Payment initialization failed" });
    }
}
```

**Request DTO:**

```csharp
public class PaymentInitializeRequestDto
{
    [Required]
    public string FlowType { get; set; }  // "SponsorBulkPurchase", "FarmerSubscription"

    [Required]
    public object FlowData { get; set; }  // Flow-specific data (will be serialized to JSON)
}
```

**Response DTO:**

```csharp
public class PaymentInitializeResponseDto
{
    public int TransactionId { get; set; }
    public string PaymentPageUrl { get; set; }     // https://sandbox-cpp.iyzipay.com?token=xxx
    public string PaymentToken { get; set; }        // xxx
    public string CallbackUrl { get; set; }         // https://api.ziraai.com/api/v1/payments/callback
    public decimal Amount { get; set; }
    public string Currency { get; set; }
}
```

#### Endpoint 2: Payment Callback (From iyzico)

```csharp
[HttpPost("callback")]
[AllowAnonymous]  // iyzico calls this, not authenticated user
public async Task<IActionResult> PaymentCallback([FromForm] IyzicoCallbackRequest request)
{
    _logger.LogInformation("[Payment] Callback received from iyzico. Token: {Token}, Status: {Status}",
        request.Token, request.Status);

    try
    {
        // Verify payment with iyzico
        var verifyResult = await _paymentService.VerifyPaymentAsync(request.Token);

        if (!verifyResult.Success)
        {
            _logger.LogError("[Payment] Payment verification failed. Token: {Token}, Error: {Error}",
                request.Token, verifyResult.Message);

            // Redirect to mobile with error
            var errorDeepLink = $"ziraai://payment-callback?token={request.Token}&status=failed&error={Uri.EscapeDataString(verifyResult.Message)}";
            return Redirect(errorDeepLink);
        }

        // Process successful payment
        await _paymentService.ProcessPaymentCallbackAsync(request.Token);

        // Redirect to mobile app deep link
        var deepLinkUrl = $"ziraai://payment-callback?token={request.Token}&status=success";

        _logger.LogInformation("[Payment] Redirecting to mobile app: {DeepLink}", deepLinkUrl);

        return Redirect(deepLinkUrl);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[Payment] Callback processing failed for token: {Token}", request.Token);

        var errorDeepLink = $"ziraai://payment-callback?token={request.Token}&status=failed&error={Uri.EscapeDataString(ex.Message)}";
        return Redirect(errorDeepLink);
    }
}
```

**Callback Request DTO:**

```csharp
public class IyzicoCallbackRequest
{
    public string Token { get; set; }
    public string Status { get; set; }
    public string PaymentId { get; set; }
    public string ConversationId { get; set; }
}
```

#### Endpoint 3: Verify Payment (From Mobile)

```csharp
[HttpGet("verify")]
[Authorize]
public async Task<IActionResult> VerifyPayment([FromQuery] string token)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

    _logger.LogInformation("[Payment] Verify payment request. UserId: {UserId}, Token: {Token}",
        userId, token);

    try
    {
        var result = await _paymentService.VerifyPaymentAsync(token);

        if (!result.Success)
        {
            _logger.LogWarning("[Payment] Payment verification failed. Token: {Token}, Error: {Error}",
                token, result.Message);
            return BadRequest(new { error = result.Message });
        }

        _logger.LogInformation("[Payment] Payment verified successfully. UserId: {UserId}, Status: {Status}",
            userId, result.Data.Status);

        return Ok(result.Data);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[Payment] Error verifying payment. Token: {Token}", token);
        return StatusCode(500, new { error = "Payment verification failed" });
    }
}
```

**Verify Response DTO:**

```csharp
public class PaymentVerifyResponseDto
{
    public string Status { get; set; }              // "Success", "Failed", "Pending"
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string PaymentMethod { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ErrorMessage { get; set; }
    public object FlowData { get; set; }            // Deserialized flow data
}
```

### 2. Payment Service

**File:** `Business/Services/Payment/IyzicoPaymentService.cs`

**Purpose:** Core payment business logic and iyzico API integration

#### Method 1: Initialize Payment

```csharp
public async Task<IDataResult<PaymentInitializeResponseDto>> InitializePaymentAsync(
    int userId, 
    PaymentInitializeRequestDto request)
{
    _logger.LogInformation("[iyzico] Initializing payment for User {UserId}, FlowType: {FlowType}",
        userId, request.FlowType);

    try
    {
        // 1. Get user details
        var user = await _userRepository.GetAsync(u => u.Id == userId);
        if (user == null)
            return new ErrorDataResult<PaymentInitializeResponseDto>("User not found");

        // 2. Calculate amount based on flow type
        decimal amount;
        string flowDataJson;

        switch (request.FlowType)
        {
            case "SponsorBulkPurchase":
                var sponsorFlowData = JsonSerializer.Deserialize<SponsorBulkPurchaseFlowData>(
                    request.FlowData.ToString());
                
                var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == sponsorFlowData.SubscriptionTierId);
                if (tier == null)
                    return new ErrorDataResult<PaymentInitializeResponseDto>("Subscription tier not found");

                amount = tier.MonthlyPrice * sponsorFlowData.Quantity;
                flowDataJson = JsonSerializer.Serialize(sponsorFlowData);
                
                _logger.LogInformation("[iyzico] SponsorBulkPurchase - TierId: {TierId}, Quantity: {Quantity}",
                    sponsorFlowData.SubscriptionTierId, sponsorFlowData.Quantity);
                break;

            case "FarmerSubscription":
                var farmerFlowData = JsonSerializer.Deserialize<FarmerSubscriptionFlowData>(
                    request.FlowData.ToString());
                
                var subTier = await _subscriptionTierRepository.GetAsync(t => t.Id == farmerFlowData.SubscriptionTierId);
                if (subTier == null)
                    return new ErrorDataResult<PaymentInitializeResponseDto>("Subscription tier not found");

                amount = subTier.MonthlyPrice;
                flowDataJson = JsonSerializer.Serialize(farmerFlowData);
                break;

            default:
                return new ErrorDataResult<PaymentInitializeResponseDto>($"Unsupported flow type: {request.FlowType}");
        }

        // 3. Create payment transaction record
        var transaction = new PaymentTransaction
        {
            UserId = userId,
            FlowType = request.FlowType,
            FlowDataJson = flowDataJson,
            Amount = amount,
            Currency = "TRY",
            Status = "Pending",
            CreatedAt = DateTime.Now,
            IyzicoConversationId = $"{request.FlowType}_{userId}_{DateTime.Now.Ticks}"
        };

        _paymentTransactionRepository.Add(transaction);
        await _paymentTransactionRepository.SaveChangesAsync();

        // 4. Prepare iyzico request
        var iyzicoRequest = new
        {
            locale = "tr",
            conversationId = transaction.IyzicoConversationId,
            price = amount,
            paidPrice = amount,
            currency = transaction.Currency,
            basketId = transaction.IyzicoConversationId,
            paymentChannel = "MOBILE",
            paymentGroup = "SUBSCRIPTION",
            callbackUrl = $"{_configuration["WebAPI:BaseUrl"]}/api/v1/payments/callback",
            enabledInstallments = new[] { 1 },
            buyer = new
            {
                id = user.Id.ToString(),
                name = user.FirstName ?? "User",
                surname = user.LastName ?? user.Id.ToString(),
                email = user.Email,
                gsmNumber = FormatPhoneNumber(user.PhoneNumber),
                identityNumber = "11111111111",  // Test value for sandbox
                registrationDate = user.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                registrationAddress = user.Address ?? "Istanbul, Turkey",
                city = user.City ?? "Istanbul",
                country = "Turkey",
                zipCode = "34732",
                ip = "127.0.0.1"
            },
            shippingAddress = new
            {
                address = user.Address ?? "Istanbul, Turkey",
                zipCode = "34742",
                contactName = $"User {user.Id}",
                city = user.City ?? "Istanbul",
                country = "Turkey"
            },
            billingAddress = new
            {
                address = user.Address ?? "Istanbul, Turkey",
                zipCode = "34742",
                contactName = $"User {user.Id}",
                city = user.City ?? "Istanbul",
                country = "Turkey"
            },
            basketItems = new[]
            {
                new
                {
                    id = "1",
                    price = amount,
                    name = request.FlowType == "SponsorBulkPurchase" ? "Sponsorship Package" : "Subscription",
                    category1 = "Subscription",
                    category2 = "Service",
                    itemType = "VIRTUAL"
                }
            }
        };

        // 5. Generate HMAC signature
        var signature = GenerateHMACSignature(iyzicoRequest);

        // 6. Call iyzico API
        var response = await CallIyzicoInitializeAsync(iyzicoRequest, signature);

        if (response.status != "success")
        {
            transaction.Status = "Failed";
            transaction.ErrorMessage = response.errorMessage;
            _paymentTransactionRepository.Update(transaction);
            await _paymentTransactionRepository.SaveChangesAsync();

            return new ErrorDataResult<PaymentInitializeResponseDto>($"iyzico error: {response.errorMessage}");
        }

        // 7. Update transaction with iyzico token
        transaction.IyzicoToken = response.token;
        _paymentTransactionRepository.Update(transaction);
        await _paymentTransactionRepository.SaveChangesAsync();

        _logger.LogInformation("[iyzico] Payment initialized successfully. TransactionId: {TransactionId}, Token: {Token}",
            transaction.Id, response.token);

        // 8. Return response
        return new SuccessDataResult<PaymentInitializeResponseDto>(new PaymentInitializeResponseDto
        {
            TransactionId = transaction.Id,
            PaymentPageUrl = response.paymentPageUrl,
            PaymentToken = response.token,
            CallbackUrl = iyzicoRequest.callbackUrl,
            Amount = amount,
            Currency = transaction.Currency
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[iyzico] Error initializing payment");
        return new ErrorDataResult<PaymentInitializeResponseDto>("Payment initialization failed");
    }
}
```

#### Method 2: Generate HMAC Signature

**iyzico Security:** All API requests must include HMACSHA256 signature

```csharp
private string GenerateHMACSignature(object requestData)
{
    // 1. Generate random key
    var randomKey = Guid.NewGuid().ToString();
    
    _logger.LogDebug("[iyzico] Random Key: {RandomKey}", randomKey);

    // 2. Get URI path based on endpoint
    string uriPath = "/payment/iyzipos/checkoutform/initialize/auth/ecom";

    _logger.LogDebug("[iyzico] URI Path: {UriPath}", uriPath);

    // 3. Serialize request to JSON (no whitespace)
    var requestJson = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    });

    // 4. Build data to hash: randomKey + uriPath + requestJson
    var dataToHash = $"{randomKey}{uriPath}{requestJson}";

    _logger.LogDebug("[iyzico] Data to hash length: {Length}", dataToHash.Length);
    _logger.LogDebug("[iyzico] Data to hash (first 150 chars): {Data}",
        dataToHash.Length > 150 ? dataToHash.Substring(0, 150) : dataToHash);

    // 5. Generate HMACSHA256 hash
    var secretKey = _configuration["Iyzico:SecretKey"];
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
    var signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

    _logger.LogDebug("[iyzico] Signature (hex): {Signature}", signature);

    // 6. Build authorization string
    var apiKey = _configuration["Iyzico:ApiKey"];
    var authString = $"apiKey:{apiKey}&randomKey:{randomKey}&signature:{signature}";

    _logger.LogDebug("[iyzico] Auth string (before base64): {AuthString}", authString);

    // 7. Base64 encode
    var authBytes = Encoding.UTF8.GetBytes(authString);
    var authHeaderValue = Convert.ToBase64String(authBytes);

    // 8. Final authorization header
    var authHeader = $"IYZWSv2 {authHeaderValue}";

    _logger.LogDebug("[iyzico] Final Authorization Header: {AuthHeader}", authHeader);

    return authHeader;
}
```

**HMAC Signature Steps:**

```
1. randomKey = Guid.NewGuid()  // "17f35945-46a3-4010-be7d-0856c86c81b6"

2. uriPath = "/payment/iyzipos/checkoutform/initialize/auth/ecom"

3. requestJson = JsonSerializer.Serialize(request)
   // {"locale":"tr","conversationId":"...","price":4999.50,...}

4. dataToHash = randomKey + uriPath + requestJson
   // "17f35945-46a3-4010-be7d-0856c86c81b6/payment/iyzipos/checkoutform/initialize/auth/ecom{"locale":"tr",...}"

5. signature = HMACSHA256(dataToHash, secretKey)
   // "233daead2940dade72cb52deddd2fc979858bdc18b95c21e5b0d01c4752dc39c"

6. authString = "apiKey:{apiKey}&randomKey:{randomKey}&signature:{signature}"
   // "apiKey:sandbox-oLzYimS7gk78wdOspOXjSS7AjgtH9SjU&randomKey:17f35945-46a3-4010-be7d-0856c86c81b6&signature:233daead..."

7. authHeader = "IYZWSv2 " + Base64(authString)
   // "IYZWSv2 YXBpS2V5OnNhbmRib3gtb0x6WWltUzdnazc4d2RPc3BPWGpTUzdBamd0SDlTalUmcmFuZG9tS2V5OjE3ZjM1OTQ1LTQ2YTMtNDAxMC1iZTdkLTA4NTZjODZjODFiNiZzaWduYXR1cmU6MjMzZGFlYWQyOTQwZGFkZTcyY2I1MmRlZGRkMmZjOTc5ODU4YmRjMThiOTVjMjFlNWIwZDAxYzQ3NTJkYzM5Yw=="
```

#### Method 3: Call iyzico Initialize API

```csharp
private async Task<dynamic> CallIyzicoInitializeAsync(object request, string authHeader)
{
    var baseUrl = _configuration["Iyzico:BaseUrl"];  // https://sandbox-api.iyzipay.com
    var endpoint = "/payment/iyzipos/checkoutform/initialize/auth/ecom";

    _logger.LogDebug("[iyzico] Calling {Endpoint}", endpoint);

    var requestJson = JsonSerializer.Serialize(request);
    
    _logger.LogInformation("[iyzico] FULL Request Body: {RequestBody}", requestJson);

    var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}{endpoint}")
    {
        Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
    };

    httpRequest.Headers.Add("Authorization", authHeader);
    httpRequest.Headers.Add("x-iyzi-client-version", "iyzipay-dotnet-2.1.39");

    var httpClient = _httpClientFactory.CreateClient();
    var httpResponse = await httpClient.SendAsync(httpRequest);

    var responseJson = await httpResponse.Content.ReadAsStringAsync();

    _logger.LogDebug("[iyzico] Raw response: {Response}", responseJson);

    return JsonSerializer.Deserialize<dynamic>(responseJson);
}
```

#### Method 4: Verify Payment

```csharp
public async Task<IDataResult<PaymentVerifyResponseDto>> VerifyPaymentAsync(string token)
{
    _logger.LogInformation("[iyzico] Verifying payment token: {Token}", token);

    try
    {
        // 1. Get transaction by token
        var transaction = await _paymentTransactionRepository.GetAsync(t => t.IyzicoToken == token);
        if (transaction == null)
            return new ErrorDataResult<PaymentVerifyResponseDto>("Transaction not found");

        // 2. If already verified, return cached result
        if (transaction.Status == "Success" && transaction.CompletedAt.HasValue)
        {
            _logger.LogInformation("[iyzico] Payment already verified. TransactionId: {TransactionId}",
                transaction.Id);

            return new SuccessDataResult<PaymentVerifyResponseDto>(new PaymentVerifyResponseDto
            {
                Status = "Success",
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                PaymentMethod = "CreditCard",
                CompletedAt = transaction.CompletedAt,
                FlowData = JsonSerializer.Deserialize<object>(transaction.FlowDataJson)
            });
        }

        // 3. Call iyzico verify API
        var iyzicoRequest = new
        {
            locale = "tr",
            conversationId = transaction.IyzicoConversationId,
            token = token
        };

        var signature = GenerateVerifySignature(iyzicoRequest);
        var response = await CallIyzicoVerifyAsync(iyzicoRequest, signature);

        // 4. Verify signature
        var expectedSignature = GenerateResponseSignature(response);
        if (expectedSignature != response.signature)
        {
            _logger.LogError("[iyzico] Response signature verification failed");
            return new ErrorDataResult<PaymentVerifyResponseDto>("Payment verification failed: Invalid signature");
        }

        _logger.LogInformation("[iyzico] Response signature verified successfully");

        // 5. Check payment status
        if (response.paymentStatus != "SUCCESS")
        {
            transaction.Status = "Failed";
            transaction.ErrorMessage = $"Payment status: {response.paymentStatus}";
            _paymentTransactionRepository.Update(transaction);
            await _paymentTransactionRepository.SaveChangesAsync();

            return new ErrorDataResult<PaymentVerifyResponseDto>($"Payment failed: {response.paymentStatus}");
        }

        // 6. Update transaction as successful
        transaction.Status = "Success";
        transaction.IyzicoPaymentId = response.paymentId;
        transaction.CompletedAt = DateTime.Now;
        _paymentTransactionRepository.Update(transaction);
        await _paymentTransactionRepository.SaveChangesAsync();

        _logger.LogInformation("[iyzico] Payment verified successfully. TransactionId: {TransactionId}, PaymentId: {PaymentId}",
            transaction.Id, response.paymentId);

        return new SuccessDataResult<PaymentVerifyResponseDto>(new PaymentVerifyResponseDto
        {
            Status = "Success",
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            PaymentMethod = "CreditCard",
            CompletedAt = transaction.CompletedAt,
            FlowData = JsonSerializer.Deserialize<object>(transaction.FlowDataJson)
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[iyzico] Error verifying payment");
        return new ErrorDataResult<PaymentVerifyResponseDto>("Payment verification failed");
    }
}
```

#### Method 5: Process Payment Callback

```csharp
public async Task ProcessPaymentCallbackAsync(string token)
{
    _logger.LogInformation("[iyzico] Processing payment callback. Token: {Token}", token);

    // 1. Get and verify transaction
    var transaction = await _paymentTransactionRepository.GetAsync(t => t.IyzicoToken == token);
    if (transaction == null)
        throw new Exception("Transaction not found");

    if (transaction.Status != "Success")
        throw new Exception("Payment not successful");

    // 2. Process based on flow type
    switch (transaction.FlowType)
    {
        case "SponsorBulkPurchase":
            await ProcessSponsorBulkPurchaseAsync(transaction);
            break;

        case "FarmerSubscription":
            await ProcessFarmerSubscriptionAsync(transaction);
            break;

        default:
            throw new Exception($"Unsupported flow type: {transaction.FlowType}");
    }

    _logger.LogInformation("[iyzico] Payment callback processed successfully. TransactionId: {TransactionId}",
        transaction.Id);
}
```

#### Method 6: Process Sponsor Bulk Purchase

**CRITICAL:** This is where business logic happens after successful payment

```csharp
private async Task ProcessSponsorBulkPurchaseAsync(PaymentTransaction transaction)
{
    _logger.LogInformation("[iyzico] Processing sponsor bulk purchase. TransactionId: {TransactionId}",
        transaction.Id);

    // 1. Deserialize flow data
    var flowData = JsonSerializer.Deserialize<SponsorBulkPurchaseFlowData>(transaction.FlowDataJson);

    // 2. Get subscription tier
    var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == flowData.SubscriptionTierId);
    if (tier == null)
        throw new Exception("Subscription tier not found");

    // 3. Create sponsorship purchase record
    var purchase = new SponsorshipPurchase
    {
        SponsorId = transaction.UserId,
        SubscriptionTierId = flowData.SubscriptionTierId,
        Quantity = flowData.Quantity,
        UnitPrice = tier.MonthlyPrice,
        TotalAmount = transaction.Amount,
        Currency = transaction.Currency,
        PurchaseDate = DateTime.Now,
        PaymentMethod = "CreditCard",
        PaymentReference = transaction.IyzicoPaymentId,
        PaymentStatus = "Completed",
        PaymentCompletedDate = transaction.CompletedAt,
        PaymentTransactionId = transaction.Id,
        
        // Invoice fields (NEW - from flowData)
        CompanyName = flowData.CompanyName,
        TaxNumber = flowData.TaxNumber,
        InvoiceAddress = flowData.InvoiceAddress,
        
        CodePrefix = "AGRI",
        ValidityDays = 30,
        Status = "Active",
        CreatedDate = DateTime.Now,
        CodesGenerated = 0,
        CodesUsed = 0
    };

    _sponsorshipPurchaseRepository.Add(purchase);
    await _sponsorshipPurchaseRepository.SaveChangesAsync();

    _logger.LogInformation("[iyzico] Sponsorship purchase created. PurchaseId: {PurchaseId}", purchase.Id);

    // 4. Generate sponsorship codes
    var codes = await _sponsorshipCodeRepository.GenerateCodesAsync(
        purchase.Id,
        transaction.UserId,
        flowData.SubscriptionTierId,
        flowData.Quantity,
        purchase.CodePrefix,
        purchase.ValidityDays);

    // 5. Update codes generated count
    purchase.CodesGenerated = codes.Count();
    _sponsorshipPurchaseRepository.Update(purchase);
    await _sponsorshipPurchaseRepository.SaveChangesAsync();

    // 6. Link purchase to transaction
    transaction.SponsorshipPurchaseId = purchase.Id;
    _paymentTransactionRepository.Update(transaction);
    await _paymentTransactionRepository.SaveChangesAsync();

    _logger.LogInformation("[iyzico] Generated {Count} sponsorship codes. PurchaseId: {PurchaseId}",
        codes.Count(), purchase.Id);

    // 7. CRITICAL: Invalidate sponsor dashboard cache
    var cacheKey = $"SponsorDashboard:{transaction.UserId}";
    _cacheManager.Remove(cacheKey);
    
    _logger.LogInformation("[DashboardCache] 🗑️ Invalidated cache for sponsor {SponsorId} after purchase creation",
        transaction.UserId);
}
```

**Cache Invalidation Pattern:**

```csharp
// Cache key format
var cacheKey = $"SponsorDashboard:{sponsorId}";

// Invalidation points
_cacheManager.Remove(cacheKey);  // After purchase creation
_cacheManager.Remove(cacheKey);  // After code distribution
_cacheManager.Remove(cacheKey);  // After purchase approval
```

### 3. Repository Layer

**File:** `DataAccess/Concrete/EntityFramework/PaymentTransactionRepository.cs`

```csharp
public class PaymentTransactionRepository : EfRepositoryBase<PaymentTransaction, ProjectDbContext>, IPaymentTransactionRepository
{
    public PaymentTransactionRepository(ProjectDbContext context) : base(context)
    {
    }

    public async Task<PaymentTransaction> GetByTokenAsync(string token)
    {
        return await Context.PaymentTransactions
            .Include(t => t.User)
            .Include(t => t.SponsorshipPurchase)
            .Include(t => t.UserSubscription)
            .FirstOrDefaultAsync(t => t.IyzicoToken == token);
    }

    public async Task<List<PaymentTransaction>> GetByUserIdAsync(int userId)
    {
        return await Context.PaymentTransactions
            .Include(t => t.SponsorshipPurchase)
            .Include(t => t.UserSubscription)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PaymentTransaction>> GetPendingTransactionsAsync()
    {
        return await Context.PaymentTransactions
            .Where(t => t.Status == "Pending" && t.CreatedAt > DateTime.Now.AddHours(-1))
            .ToListAsync();
    }
}
```

### 4. Configuration

**File:** `appsettings.Staging.json`

```json
{
  "Iyzico": {
    "BaseUrl": "https://sandbox-api.iyzipay.com",
    "ApiKey": "sandbox-xxxxxxxxxxxxxxxxxxxxxxx",
    "SecretKey": "sandbox-yyyyyyyyyyyyyyyyyyyyyyyy",
    "Currency": "TRY",
    "PaymentChannel": "MOBILE",
    "PaymentGroup": "SUBSCRIPTION",
    "TokenExpirationMinutes": 30,
    "Callback": {
      "DeepLinkScheme": "ziraai://payment-callback",
      "FallbackUrl": "https://ziraai-api-sit.up.railway.app/api/v1/payments/callback"
    },
    "Timeout": {
      "InitializeTimeoutSeconds": 30,
      "VerifyTimeoutSeconds": 30,
      "WebhookTimeoutSeconds": 15
    },
    "Retry": {
      "MaxRetryAttempts": 3,
      "RetryDelayMilliseconds": 1000,
      "UseExponentialBackoff": true
    }
  },
  "WebAPI": {
    "BaseUrl": "https://ziraai-api-sit.up.railway.app",
    "InternalSecret": "ZiraAI_Internal_Secret_Staging_2025"
  }
}
```

**Environment-Specific Values:**

| Environment | BaseUrl | ApiKey | SecretKey | Callback URL |
|-------------|---------|--------|-----------|--------------|
| Development | sandbox-api.iyzipay.com | sandbox-xxx | sandbox-yyy | http://localhost:5001/api/v1/payments/callback |
| Staging | sandbox-api.iyzipay.com | sandbox-xxx | sandbox-yyy | https://ziraai-api-sit.up.railway.app/api/v1/payments/callback |
| Production | api.iyzipay.com | production-xxx | production-yyy | https://api.ziraai.com/api/v1/payments/callback |

---

## Mobile App Implementation

### 1. Payment Models

**File:** `lib/features/payment/data/models/payment_models.dart`

```dart
class PaymentInitializeRequest {
  final String flowType;
  final Map<String, dynamic> flowData;

  PaymentInitializeRequest({
    required this.flowType,
    required this.flowData,
  });

  Map<String, dynamic> toJson() => {
        'flowType': flowType,
        'flowData': flowData,
      };
}

class SponsorBulkPurchaseFlowData {
  final int subscriptionTierId;
  final int quantity;
  final String? companyName;
  final String? taxNumber;
  final String? invoiceAddress;

  SponsorBulkPurchaseFlowData({
    required this.subscriptionTierId,
    required this.quantity,
    this.companyName,
    this.taxNumber,
    this.invoiceAddress,
  });

  Map<String, dynamic> toJson() => {
        'subscriptionTierId': subscriptionTierId,
        'quantity': quantity,
        if (companyName != null) 'companyName': companyName,
        if (taxNumber != null) 'taxNumber': taxNumber,
        if (invoiceAddress != null) 'invoiceAddress': invoiceAddress,
      };
}

class PaymentInitializeResponse {
  final int transactionId;
  final String paymentPageUrl;
  final String paymentToken;
  final String callbackUrl;
  final double amount;
  final String currency;

  PaymentInitializeResponse({
    required this.transactionId,
    required this.paymentPageUrl,
    required this.paymentToken,
    required this.callbackUrl,
    required this.amount,
    required this.currency,
  });

  factory PaymentInitializeResponse.fromJson(Map<String, dynamic> json) {
    return PaymentInitializeResponse(
      transactionId: json['transactionId'] as int,
      paymentPageUrl: json['paymentPageUrl'] as String,
      paymentToken: json['paymentToken'] as String,
      callbackUrl: json['callbackUrl'] as String,
      amount: (json['amount'] as num).toDouble(),
      currency: json['currency'] as String,
    );
  }
}

class PaymentVerifyResponse {
  final String status;
  final double amount;
  final String currency;
  final String? paymentMethod;
  final DateTime? completedAt;
  final String? errorMessage;
  final Map<String, dynamic>? flowData;

  PaymentVerifyResponse({
    required this.status,
    required this.amount,
    required this.currency,
    this.paymentMethod,
    this.completedAt,
    this.errorMessage,
    this.flowData,
  });

  factory PaymentVerifyResponse.fromJson(Map<String, dynamic> json) {
    return PaymentVerifyResponse(
      status: json['status'] as String,
      amount: (json['amount'] as num).toDouble(),
      currency: json['currency'] as String,
      paymentMethod: json['paymentMethod'] as String?,
      completedAt: json['completedAt'] != null
          ? DateTime.parse(json['completedAt'] as String)
          : null,
      errorMessage: json['errorMessage'] as String?,
      flowData: json['flowData'] as Map<String, dynamic>?,
    );
  }

  bool get isSuccess => status == 'Success';
  bool get isFailed => status == 'Failed';
  bool get isPending => status == 'Pending';
}
```

### 2. Payment Service

**File:** `lib/features/payment/services/payment_service.dart`

```dart
class PaymentService {
  final Dio _dio;

  PaymentService(this._dio);

  /// Initialize payment for sponsor bulk purchase
  Future<PaymentInitializeResponse> initializeSponsorPurchase({
    required int tierId,
    required int quantity,
    String? companyName,
    String? taxNumber,
    String? invoiceAddress,
  }) async {
    print('💳 Payment: Initializing sponsor purchase...');
    print('💳 Payment: TierId=$tierId, Quantity=$quantity');
    if (companyName != null) {
      print('💳 Payment: Corporate invoice - Company: $companyName');
    }

    try {
      final flowData = SponsorBulkPurchaseFlowData(
        subscriptionTierId: tierId,
        quantity: quantity,
        companyName: companyName,
        taxNumber: taxNumber,
        invoiceAddress: invoiceAddress,
      );

      final request = PaymentInitializeRequest(
        flowType: 'SponsorBulkPurchase',
        flowData: flowData.toJson(),
      );

      final response = await _dio.post(
        '/payments/initialize',
        data: request.toJson(),
      );

      print('💳 Payment: Initialization successful');
      print('💳 Payment: TransactionId=${response.data['transactionId']}');
      print('💳 Payment: Token=${response.data['paymentToken']}');

      return PaymentInitializeResponse.fromJson(response.data);
    } catch (e) {
      print('❌ Payment: Initialization failed - $e');
      rethrow;
    }
  }

  /// Verify payment after callback
  Future<PaymentVerifyResponse> verifyPayment(String token) async {
    print('💳 Payment: Verifying payment...');
    print('💳 Payment: Token=$token');

    try {
      final response = await _dio.get(
        '/payments/verify',
        queryParameters: {'token': token},
      );

      print('💳 Payment: Verification response - Status=${response.data['status']}');

      return PaymentVerifyResponse.fromJson(response.data);
    } catch (e) {
      print('❌ Payment: Verification failed - $e');
      rethrow;
    }
  }
}
```

### 3. Payment WebView Screen

**File:** `lib/features/payment/presentation/screens/payment_webview_screen.dart`

```dart
class PaymentWebViewScreen extends StatefulWidget {
  final String paymentPageUrl;
  final String paymentToken;
  final String callbackUrl;

  const PaymentWebViewScreen({
    Key? key,
    required this.paymentPageUrl,
    required this.paymentToken,
    required this.callbackUrl,
  }) : super(key: key);

  @override
  State<PaymentWebViewScreen> createState() => _PaymentWebViewScreenState();
}

class _PaymentWebViewScreenState extends State<PaymentWebViewScreen> {
  late final WebViewController _controller;
  bool _isLoading = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _initializeWebView();
  }

  void _initializeWebView() {
    print('💳 WebView: Initializing...');
    print('💳 WebView: URL=${widget.paymentPageUrl}');
    print('💳 WebView: Token=${widget.paymentToken}');

    _controller = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      ..setBackgroundColor(Colors.white)
      ..setNavigationDelegate(
        NavigationDelegate(
          onProgress: (int progress) {
            print('💳 WebView: Loading progress: $progress%');
          },
          onPageStarted: (String url) {
            print('💳 WebView: Page started - $url');
            
            // Check if this is our callback URL
            if (url.startsWith(widget.callbackUrl) || 
                url.startsWith('ziraai://payment-callback')) {
              _handleCallback(url);
            }
          },
          onPageFinished: (String url) {
            print('💳 WebView: Page finished - $url');
            setState(() {
              _isLoading = false;
            });

            // Double-check for callback URLs
            if (url.startsWith(widget.callbackUrl) || 
                url.startsWith('ziraai://payment-callback')) {
              _handleCallback(url);
            }
          },
          onWebResourceError: (WebResourceError error) {
            print('❌ WebView: Error - ${error.description}');
            setState(() {
              _isLoading = false;
              _errorMessage = error.description;
            });
          },
          onNavigationRequest: (NavigationRequest request) {
            print('💳 WebView: Navigation request - ${request.url}');

            // Handle deep link callbacks
            if (request.url.startsWith('ziraai://payment-callback')) {
              print('💳 WebView: Deep link callback detected');
              _handleCallback(request.url);
              return NavigationDecision.prevent;
            }

            // Handle HTTPS callback
            if (request.url.startsWith(widget.callbackUrl)) {
              print('💳 WebView: HTTPS callback detected');
              _handleCallback(request.url);
              return NavigationDecision.prevent;
            }

            return NavigationDecision.navigate;
          },
        ),
      )
      ..loadRequest(Uri.parse(widget.paymentPageUrl));
  }

  void _handleCallback(String url) {
    print('💳 WebView: Processing callback - $url');

    // Parse callback URL
    final uri = Uri.parse(url);
    final token = uri.queryParameters['token'];
    final status = uri.queryParameters['status'];
    final error = uri.queryParameters['error'];

    print('💳 WebView: Callback params - token=$token, status=$status, error=$error');

    if (token == null) {
      print('❌ WebView: No token in callback');
      return;
    }

    // Close WebView and return to previous screen with result
    Navigator.of(context).pop({
      'token': token,
      'status': status ?? 'unknown',
      'error': error,
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Payment'),
        leading: IconButton(
          icon: const Icon(Icons.close),
          onPressed: () {
            Navigator.of(context).pop({'status': 'cancelled'});
          },
        ),
      ),
      body: Stack(
        children: [
          WebViewWidget(controller: _controller),
          if (_isLoading)
            const Center(
              child: CircularProgressIndicator(),
            ),
          if (_errorMessage != null)
            Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.error, size: 64, color: Colors.red),
                  const SizedBox(height: 16),
                  Text(
                    'Payment Error',
                    style: Theme.of(context).textTheme.headline6,
                  ),
                  const SizedBox(height: 8),
                  Text(_errorMessage!),
                  const SizedBox(height: 24),
                  ElevatedButton(
                    onPressed: () {
                      Navigator.of(context).pop({'status': 'failed'});
                    },
                    child: const Text('Close'),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
```

### 4. Sponsor Payment Screen (Complete Flow)

**File:** `lib/features/payment/presentation/screens/sponsor_payment_screen.dart`

```dart
class SponsorPaymentScreen extends StatefulWidget {
  final int tierId;
  final int quantity;

  const SponsorPaymentScreen({
    Key? key,
    required this.tierId,
    required this.quantity,
  }) : super(key: key);

  @override
  State<SponsorPaymentScreen> createState() => _SponsorPaymentScreenState();
}

class _SponsorPaymentScreenState extends State<SponsorPaymentScreen> {
  final _formKey = GlobalKey<FormState>();
  final _paymentService = GetIt.instance<PaymentService>();

  bool _isLoading = false;
  bool _wantsCorporateInvoice = false;

  // Invoice form controllers
  final _companyNameController = TextEditingController();
  final _taxNumberController = TextEditingController();
  final _invoiceAddressController = TextEditingController();

  @override
  void dispose() {
    _companyNameController.dispose();
    _taxNumberController.dispose();
    _invoiceAddressController.dispose();
    super.dispose();
  }

  Future<void> _startPayment() async {
    if (_wantsCorporateInvoice && !_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _isLoading = true;
    });

    try {
      print('💳 Payment: Starting payment flow...');

      // 1. Initialize payment
      final response = await _paymentService.initializeSponsorPurchase(
        tierId: widget.tierId,
        quantity: widget.quantity,
        companyName: _wantsCorporateInvoice ? _companyNameController.text : null,
        taxNumber: _wantsCorporateInvoice ? _taxNumberController.text : null,
        invoiceAddress: _wantsCorporateInvoice ? _invoiceAddressController.text : null,
      );

      print('💳 Payment: Opening WebView...');

      // 2. Open WebView
      final result = await Navigator.push(
        context,
        MaterialPageRoute(
          builder: (context) => PaymentWebViewScreen(
            paymentPageUrl: response.paymentPageUrl,
            paymentToken: response.paymentToken,
            callbackUrl: response.callbackUrl,
          ),
        ),
      );

      if (result == null || result['status'] == 'cancelled') {
        print('💳 Payment: User cancelled');
        _showError('Payment cancelled');
        return;
      }

      final token = result['token'] as String;
      final status = result['status'] as String;

      if (status == 'failed') {
        print('❌ Payment: Failed - ${result['error']}');
        _showError(result['error'] ?? 'Payment failed');
        return;
      }

      print('💳 Payment: Verifying payment...');

      // 3. Verify payment
      final verifyResponse = await _paymentService.verifyPayment(token);

      if (verifyResponse.isSuccess) {
        print('✅ Payment: Verification successful');
        _showSuccess();
      } else {
        print('❌ Payment: Verification failed');
        _showError(verifyResponse.errorMessage ?? 'Payment verification failed');
      }
    } catch (e) {
      print('❌ Payment: Error - $e');
      _showError('Payment error: $e');
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  void _showSuccess() {
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => AlertDialog(
        title: const Text('Payment Successful'),
        content: const Text('Your purchase has been completed successfully.'),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.of(context).pop(); // Close dialog
              Navigator.of(context).pop(); // Close payment screen
            },
            child: const Text('OK'),
          ),
        ],
      ),
    );
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: Colors.red,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Complete Purchase'),
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            // Purchase summary
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Purchase Summary',
                      style: Theme.of(context).textTheme.headline6,
                    ),
                    const SizedBox(height: 16),
                    _buildSummaryRow('Tier', 'S Tier'),
                    _buildSummaryRow('Quantity', '${widget.quantity} codes'),
                    const Divider(),
                    _buildSummaryRow('Total', '₺4,999.50', bold: true),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),

            // Invoice option toggle
            SwitchListTile(
              title: const Text('Corporate Invoice'),
              subtitle: const Text('I want a corporate invoice'),
              value: _wantsCorporateInvoice,
              onChanged: (value) {
                setState(() {
                  _wantsCorporateInvoice = value;
                });
              },
            ),

            // Corporate invoice form (conditional)
            if (_wantsCorporateInvoice) ...[
              const SizedBox(height: 16),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Invoice Information',
                        style: Theme.of(context).textTheme.subtitle1,
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _companyNameController,
                        decoration: const InputDecoration(
                          labelText: 'Company Name',
                          border: OutlineInputBorder(),
                        ),
                        validator: (value) {
                          if (_wantsCorporateInvoice && (value == null || value.isEmpty)) {
                            return 'Please enter company name';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _taxNumberController,
                        decoration: const InputDecoration(
                          labelText: 'Tax Number',
                          border: OutlineInputBorder(),
                        ),
                        keyboardType: TextInputType.number,
                        validator: (value) {
                          if (_wantsCorporateInvoice && (value == null || value.isEmpty)) {
                            return 'Please enter tax number';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _invoiceAddressController,
                        decoration: const InputDecoration(
                          labelText: 'Invoice Address',
                          border: OutlineInputBorder(),
                        ),
                        maxLines: 3,
                        validator: (value) {
                          if (_wantsCorporateInvoice && (value == null || value.isEmpty)) {
                            return 'Please enter invoice address';
                          }
                          return null;
                        },
                      ),
                    ],
                  ),
                ),
              ),
            ],

            const SizedBox(height: 24),

            // Payment button
            ElevatedButton(
              onPressed: _isLoading ? null : _startPayment,
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.all(16),
              ),
              child: _isLoading
                  ? const CircularProgressIndicator()
                  : const Text(
                      'Proceed to Payment',
                      style: TextStyle(fontSize: 18),
                    ),
            ),

            const SizedBox(height: 16),

            // Security info
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: const [
                Icon(Icons.lock, size: 16, color: Colors.grey),
                SizedBox(width: 8),
                Text(
                  'Secure payment powered by iyzico',
                  style: TextStyle(color: Colors.grey, fontSize: 12),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSummaryRow(String label, String value, {bool bold = false}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: TextStyle(
              fontWeight: bold ? FontWeight.bold : FontWeight.normal,
            ),
          ),
          Text(
            value,
            style: TextStyle(
              fontWeight: bold ? FontWeight.bold : FontWeight.normal,
            ),
          ),
        ],
      ),
    );
  }
}
```

### 5. Deep Link Configuration

**File:** `android/app/src/main/AndroidManifest.xml`

```xml
<activity
    android:name=".MainActivity"
    android:exported="true">
    
    <!-- ... existing intent filters ... -->

    <!-- Payment callback deep link -->
    <intent-filter android:autoVerify="true">
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        
        <data
            android:scheme="ziraai"
            android:host="payment-callback" />
    </intent-filter>
</activity>
```

**File:** `ios/Runner/Info.plist`

```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleTypeRole</key>
        <string>Editor</string>
        <key>CFBundleURLName</key>
        <string>com.ziraai.app</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>ziraai</string>
        </array>
    </dict>
</array>
```

---

## Payment Flow Step-by-Step

### Complete Flow Timeline

```
[Mobile] User selects tier & quantity
    ↓
[Mobile] User clicks "Proceed to Payment"
    ↓ (Optional: Fill invoice form)
    ↓
[Mobile] POST /api/v1/payments/initialize
    {
      "flowType": "SponsorBulkPurchase",
      "flowData": {
        "subscriptionTierId": 1,
        "quantity": 50,
        "companyName": "ZiraAI Ltd.",  // Optional
        "taxNumber": "1234567890",     // Optional
        "invoiceAddress": "Istanbul"   // Optional
      }
    }
    ↓
[Backend] Create PaymentTransaction (Status: Pending)
    ↓
[Backend] Generate HMACSHA256 signature
    ↓
[Backend] POST https://sandbox-api.iyzipay.com/payment/iyzipos/checkoutform/initialize/auth/ecom
    ↓
[iyzico] Validate signature & create payment session
    ↓
[iyzico] Return payment page URL & token
    ↓
[Backend] Update PaymentTransaction with token
    ↓
[Backend] Return response to mobile
    {
      "transactionId": 19,
      "paymentPageUrl": "https://sandbox-cpp.iyzipay.com?token=xxx",
      "paymentToken": "xxx",
      "callbackUrl": "https://ziraai-api-sit.up.railway.app/api/v1/payments/callback",
      "amount": 4999.50,
      "currency": "TRY"
    }
    ↓
[Mobile] Open WebView with paymentPageUrl
    ↓
[User] Fill card details in WebView
    - Card number: 5528790000000008
    - Expiry: 12/2030
    - CVV: 123
    - Name: Test User
    ↓
[User] Click "Ödemeyi Tamamla" (Complete Payment)
    ↓
[iyzico] Show 3D Secure page
    ↓
[User] Enter SMS code: 123456
    ↓
[iyzico] Process payment with bank
    ↓
[iyzico] POST /api/v1/payments/callback
    token=xxx
    ↓
[Backend] VerifyPaymentAsync(token)
    ↓
[Backend] POST https://sandbox-api.iyzipay.com/payment/iyzipos/checkoutform/auth/ecom/detail
    ↓
[iyzico] Return payment result
    {
      "paymentStatus": "SUCCESS",
      "paymentId": "27659972",
      "price": 4999.50,
      ...
    }
    ↓
[Backend] Verify response signature
    ↓
[Backend] Update PaymentTransaction (Status: Success, CompletedAt: now)
    ↓
[Backend] ProcessPaymentCallbackAsync(token)
    ↓
[Backend] Create SponsorshipPurchase record
    - SponsorId: 189
    - TierId: 1
    - Quantity: 50
    - TotalAmount: 4999.50
    - CompanyName: "ZiraAI Ltd." (if provided)
    - TaxNumber: "1234567890" (if provided)
    - InvoiceAddress: "Istanbul" (if provided)
    ↓
[Backend] GenerateCodesAsync(50 codes)
    ↓
[Backend] Update SponsorshipPurchase (CodesGenerated: 50)
    ↓
[Backend] Link PaymentTransaction to SponsorshipPurchase
    ↓
[Backend] CRITICAL: Invalidate cache
    _cacheManager.Remove($"SponsorDashboard:{sponsorId}")
    ↓
[Backend] HTTP 302 Redirect
    Location: ziraai://payment-callback?token=xxx&status=success
    ↓
[Mobile] Deep link opens app
    ↓
[Mobile] GET /api/v1/payments/verify?token=xxx
    ↓
[Backend] Return cached verification result
    {
      "status": "Success",
      "amount": 4999.50,
      "currency": "TRY",
      "paymentMethod": "CreditCard",
      "completedAt": "2025-11-22T10:36:28Z",
      "flowData": {
        "subscriptionTierId": 1,
        "quantity": 50,
        "companyName": "ZiraAI Ltd."
      }
    }
    ↓
[Mobile] Show success screen
    ↓
[User] Navigate to dashboard
    ↓
[Mobile] GET /api/v1/sponsorship/dashboard-summary
    ↓
[Backend] Cache MISS (invalidated after purchase)
    ↓
[Backend] Fetch fresh data from database
    - Total packages: 1
    - Active codes: 50
    - Available codes: 50
    ↓
[Backend] Cache result (TTL: 24h)
    ↓
[Backend] Return dashboard with NEW purchase visible ✅
```

### Timeline with Actual Timestamps (From Logs)

```
10:35:43.666 - [Mobile] POST /api/v1/payments/initialize
10:35:43.674 - [Backend] IyzicoPaymentService.InitializePaymentAsync()
10:35:43.730 - [Backend] POST to iyzico initialize API
10:35:44.188 - [iyzico] Response 200 (452ms)
10:35:44.193 - [Backend] PaymentTransaction created, Token saved
10:35:44.438 - [Backend] Return response to mobile

10:36:27.739 - [iyzico] POST /api/v1/payments/callback
10:36:27.742 - [Backend] IyzicoPaymentService.VerifyPaymentAsync()
10:36:27.777 - [Backend] POST to iyzico verify API
10:36:27.868 - [iyzico] Response 200 (90ms)
10:36:27.876 - [Backend] Signature verified ✅
10:36:27.917 - [Backend] ProcessSponsorBulkPurchaseAsync()
10:36:28.058 - [Backend] SponsorshipPurchase created (PurchaseId: 39)
10:36:28.289 - [Backend] 50 codes generated
10:36:28.xxx - [Backend] Cache invalidated (MISSING IN OLD LOGS - NOW FIXED)
10:36:28.379 - [Backend] Redirect to ziraai://payment-callback?token=xxx&status=success

10:36:28.839 - [Mobile] GET /api/v1/payments/verify?token=xxx
10:36:28.856 - [Backend] Return cached verification result

10:36:37.475 - [Mobile] GET /api/v1/sponsorship/dashboard-summary
10:36:37.xxx - [Backend] Cache MISS (invalidated)
10:36:37.556 - [Backend] Fresh data from database with NEW purchase ✅
```

**Total Flow Time:** ~54 seconds (from initialize to dashboard refresh)

---

## Error Handling

### 1. Payment Initialization Errors

**Scenario:** User not found

```csharp
// Backend
if (user == null)
    return new ErrorDataResult<PaymentInitializeResponseDto>("User not found");

// Mobile
catch (DioError e) {
  if (e.response?.statusCode == 400) {
    _showError(e.response?.data['error'] ?? 'Payment initialization failed');
  }
}
```

**Scenario:** Tier not found

```csharp
// Backend
var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == flowData.SubscriptionTierId);
if (tier == null)
    return new ErrorDataResult<PaymentInitializeResponseDto>("Subscription tier not found");
```

**Scenario:** iyzico API error

```csharp
// Backend
if (response.status != "success")
{
    transaction.Status = "Failed";
    transaction.ErrorMessage = response.errorMessage;
    _paymentTransactionRepository.Update(transaction);
    await _paymentTransactionRepository.SaveChangesAsync();

    return new ErrorDataResult<PaymentInitializeResponseDto>($"iyzico error: {response.errorMessage}");
}
```

### 2. Payment Callback Errors

**Scenario:** Signature verification fails

```csharp
// Backend
var expectedSignature = GenerateResponseSignature(response);
if (expectedSignature != response.signature)
{
    _logger.LogError("[iyzico] Response signature verification failed");
    
    var errorDeepLink = $"ziraai://payment-callback?token={token}&status=failed&error=Invalid+signature";
    return Redirect(errorDeepLink);
}
```

**Scenario:** Payment not successful

```csharp
// Backend
if (response.paymentStatus != "SUCCESS")
{
    transaction.Status = "Failed";
    transaction.ErrorMessage = $"Payment status: {response.paymentStatus}";
    
    var errorDeepLink = $"ziraai://payment-callback?token={token}&status=failed&error={Uri.EscapeDataString(transaction.ErrorMessage)}";
    return Redirect(errorDeepLink);
}
```

### 3. Mobile WebView Errors

**Scenario:** Page load error

```dart
// Mobile
onWebResourceError: (WebResourceError error) {
  print('❌ WebView: Error - ${error.description}');
  setState(() {
    _isLoading = false;
    _errorMessage = error.description;
  });
}
```

**Scenario:** User cancels payment

```dart
// Mobile
leading: IconButton(
  icon: const Icon(Icons.close),
  onPressed: () {
    Navigator.of(context).pop({'status': 'cancelled'});
  },
)
```

### 4. Error Response Format

**Backend Error Response:**

```json
{
  "error": "Subscription tier not found",
  "errorCode": "TIER_NOT_FOUND",
  "timestamp": "2025-11-22T10:36:27Z"
}
```

**Mobile Error Handling:**

```dart
try {
  final response = await _paymentService.initializeSponsorPurchase(...);
} on DioError catch (e) {
  if (e.response != null) {
    final error = e.response?.data['error'] ?? 'Unknown error';
    _showError(error);
  } else {
    _showError('Network error');
  }
}
```

---

## Security Considerations

### 1. HMAC Signature Validation

**Purpose:** Prevent tampering with API requests

```csharp
// Backend generates signature
var signature = HMACSHA256(randomKey + uriPath + requestJson, secretKey);

// iyzico validates signature
if (!ValidateSignature(request, signature)) {
    return Unauthorized();
}
```

**Security Rules:**

- ✅ Generate new random key for each request
- ✅ Use HMACSHA256 (SHA256 only is insecure)
- ✅ Validate both request and response signatures
- ❌ Never expose secret key to mobile app
- ❌ Never log secret keys or full signatures

### 2. Deep Link Security

**Risk:** Malicious apps could intercept deep links

**Mitigation:**

```dart
// Mobile: Always verify payment after deep link
final verifyResponse = await _paymentService.verifyPayment(token);

if (!verifyResponse.isSuccess) {
  // Don't trust the deep link status parameter
  _showError('Payment verification failed');
  return;
}
```

### 3. Token Expiration

**iyzico tokens expire after 30 minutes**

```csharp
// Backend configuration
"Iyzico": {
  "TokenExpirationMinutes": 30
}

// Mobile: Handle expired tokens
if (error.contains('token expired')) {
  _showError('Payment session expired. Please try again.');
}
```

### 4. SSL/TLS Requirements

**All communications must use HTTPS:**

- ✅ API endpoints: `https://api.ziraai.com`
- ✅ iyzico API: `https://api.iyzipay.com`
- ✅ Callback URL: `https://api.ziraai.com/api/v1/payments/callback`
- ❌ Never use HTTP in production

### 5. Sensitive Data Logging

**DO NOT log:**

- ❌ Full card numbers
- ❌ CVV codes
- ❌ Secret keys
- ❌ Full HMAC signatures

**Safe to log:**

- ✅ Transaction IDs
- ✅ User IDs
- ✅ Payment tokens (they expire)
- ✅ Payment status
- ✅ Amounts and currencies

```csharp
// GOOD
_logger.LogInformation("[Payment] TransactionId: {TransactionId}, Amount: {Amount}",
    transaction.Id, transaction.Amount);

// BAD
_logger.LogInformation("[Payment] SecretKey: {SecretKey}", _secretKey);  // ❌ NEVER
```

---

## Testing Guide

### 1. Test Credentials (iyzico Sandbox)

**Test Credit Cards:**

| Card Number | Bank | 3D Secure | Result |
|-------------|------|-----------|--------|
| 5528790000000008 | Vakıfbank | Yes | Success |
| 5451030000000000 | Garanti | Yes | Success |
| 4766620000000001 | Denizbank | Yes | Success |

**Test Data:**

- Expiry: Any future date (e.g., `12/2030`)
- CVV: Any 3 digits (e.g., `123`)
- 3D Secure SMS Code: `123456` (sandbox always accepts this)

### 2. Test Scenarios

#### Scenario 1: Personal Purchase (Success)

**Steps:**

1. Mobile: Select S Tier, Quantity: 50
2. Mobile: Do NOT toggle "Corporate Invoice"
3. Mobile: Click "Proceed to Payment"
4. WebView: Fill test card `5528790000000008`
5. WebView: Click "Ödemeyi Tamamla"
6. 3D Secure: Enter SMS code `123456`
7. Mobile: App opens with success message
8. Mobile: Navigate to dashboard
9. **Expected:** New purchase visible with 50 available codes

**Database Verification:**

```sql
SELECT * FROM "SponsorshipPurchases" WHERE "Id" = 39;
-- CompanyName: NULL
-- TaxNumber: NULL
-- InvoiceAddress: NULL
-- Status: 'Active'
-- CodesGenerated: 50
```

#### Scenario 2: Corporate Purchase (Success)

**Steps:**

1. Mobile: Select S Tier, Quantity: 50
2. Mobile: Toggle "Corporate Invoice" ON
3. Mobile: Fill invoice form:
   - Company: "ZiraAI Tech Ltd."
   - Tax Number: "1234567890"
   - Address: "Istanbul, Turkey"
4. Mobile: Click "Proceed to Payment"
5-9. (Same as Scenario 1)

**Database Verification:**

```sql
SELECT * FROM "SponsorshipPurchases" WHERE "Id" = 39;
-- CompanyName: 'ZiraAI Tech Ltd.'
-- TaxNumber: '1234567890'
-- InvoiceAddress: 'Istanbul, Turkey'
-- Status: 'Active'
-- CodesGenerated: 50
```

#### Scenario 3: User Cancels Payment

**Steps:**

1-4. (Same as Scenario 1)
5. WebView: Click back/close button
6. **Expected:** Return to purchase screen with "Payment cancelled" message
7. **Expected:** No database records created

#### Scenario 4: Payment Fails (Card Declined)

**Steps:**

1-4. (Same as Scenario 1)
5. WebView: Fill invalid card number
6. **Expected:** iyzico shows error message
7. **Expected:** PaymentTransaction.Status = "Failed"

#### Scenario 5: Network Error During Callback

**Steps:**

1-5. (Same as Scenario 1)
6. Backend: Simulate network error (disconnect Railway)
7. **Expected:** Mobile shows "Payment verification failed"
8. **Recovery:** User can retry verification with same token

### 3. Backend Testing (Postman)

**Collection:** `ZiraAI_Complete_API_Collection_v6.1.json`

**Test 1: Initialize Payment**

```
POST {{baseUrl}}/api/v1/payments/initialize
Authorization: Bearer {{token}}

{
  "flowType": "SponsorBulkPurchase",
  "flowData": {
    "subscriptionTierId": 1,
    "quantity": 50,
    "companyName": "Test Company",
    "taxNumber": "1234567890",
    "invoiceAddress": "Istanbul, Turkey"
  }
}

Expected Response:
{
  "transactionId": 19,
  "paymentPageUrl": "https://sandbox-cpp.iyzipay.com?token=xxx",
  "paymentToken": "xxx",
  "callbackUrl": "https://ziraai-api-sit.up.railway.app/api/v1/payments/callback",
  "amount": 4999.50,
  "currency": "TRY"
}
```

**Test 2: Verify Payment**

```
GET {{baseUrl}}/api/v1/payments/verify?token=xxx
Authorization: Bearer {{token}}

Expected Response (Success):
{
  "status": "Success",
  "amount": 4999.50,
  "currency": "TRY",
  "paymentMethod": "CreditCard",
  "completedAt": "2025-11-22T10:36:28Z",
  "flowData": {
    "subscriptionTierId": 1,
    "quantity": 50,
    "companyName": "Test Company"
  }
}
```

### 4. Cache Invalidation Testing

**Verify cache is invalidated after purchase:**

```bash
# 1. Get dashboard (will be cached)
GET /api/v1/sponsorship/dashboard-summary
# Response: activePackages = []
# Log: [DashboardCache] ✅ Cache HIT

# 2. Complete payment
POST /api/v1/payments/initialize
# ... complete payment flow ...

# 3. Get dashboard again (should be fresh)
GET /api/v1/sponsorship/dashboard-summary
# Response: activePackages = [{ purchaseId: 39, ... }]
# Log: [DashboardCache] ❌ Cache MISS
# Log: [DashboardCache] 🗑️ Invalidated cache for sponsor 189
```

**Manual Redis Check:**

```bash
# Connect to Railway Redis
railway connect redis

# Check cache key
GET SponsorDashboard:189

# Should return (nil) after purchase
```

---

## Deployment Checklist

### 1. Environment Configuration

**Staging:**

```bash
# Railway Environment Variables
Iyzico__BaseUrl=https://sandbox-api.iyzipay.com
Iyzico__ApiKey=sandbox-xxxxxxxxxxxxxxxxxxxxxxx
Iyzico__SecretKey=sandbox-yyyyyyyyyyyyyyyyyyyyyyyy
Iyzico__Callback__FallbackUrl=https://ziraai-api-sit.up.railway.app/api/v1/payments/callback
WebAPI__BaseUrl=https://ziraai-api-sit.up.railway.app
```

**Production:**

```bash
# Railway Environment Variables
Iyzico__BaseUrl=https://api.iyzipay.com
Iyzico__ApiKey=production-xxxxxxxxxxxxxxxxxxxxxxx
Iyzico__SecretKey=production-yyyyyyyyyyyyyyyyyyyyyyyy
Iyzico__Callback__FallbackUrl=https://api.ziraai.com/api/v1/payments/callback
WebAPI__BaseUrl=https://api.ziraai.com
```

### 2. Database Migration

```bash
# 1. Add migration for PaymentTransaction and SponsorshipPurchase tables
dotnet ef migrations add AddPaymentTables --project DataAccess --startup-project WebAPI --context ProjectDbContext --output-dir Migrations/Pg

# 2. Review migration SQL
dotnet ef migrations script --project DataAccess --startup-project WebAPI --context ProjectDbContext

# 3. Apply to staging
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext --connection "Host=postgres.railway.internal;Port=5432;Database=ziraai_staging;..."

# 4. Verify tables created
psql $DATABASE_URL -c "SELECT table_name FROM information_schema.tables WHERE table_name IN ('PaymentTransactions', 'SponsorshipPurchases');"

# 5. Apply to production (after testing)
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext --connection "Host=production-host;..."
```

### 3. Backend Deployment

```bash
# 1. Checkout feature branch
git checkout feature/payment-integration

# 2. Pull latest changes
git pull origin feature/payment-integration

# 3. Push to Railway
git push origin feature/payment-integration

# 4. Railway auto-deploys (wait ~2 minutes)

# 5. Verify deployment
curl https://ziraai-api-sit.up.railway.app/health

# 6. Check logs
railway logs --tail 100
```

**Deployment Verification:**

```bash
# 1. Check cache invalidation code exists
grep -r "Invalidated cache for sponsor" Business/Services/Payment/IyzicoPaymentService.cs
# Expected output: Line 768

# 2. Check invoice fields exist
grep -r "CompanyName = flowData.CompanyName" Business/Services/Payment/IyzicoPaymentService.cs
# Expected output: Line 726-728

# 3. Check callback URL
grep "FallbackUrl" WebAPI/appsettings.Staging.json
# Expected: "https://ziraai-api-sit.up.railway.app/api/v1/payments/callback"
```

### 4. Mobile Deployment

**Flutter Build:**

```bash
# 1. Update version in pubspec.yaml
version: 1.2.0+12

# 2. Build staging APK
flutter build apk --release --flavor staging -t lib/main_staging.dart

# 3. Build production APK
flutter build apk --release --flavor production -t lib/main_production.dart

# 4. Upload to internal testing (Google Play Console)
```

**Deep Link Verification (Android):**

```bash
# 1. Install APK on device
adb install build/app/outputs/flutter-apk/app-staging-release.apk

# 2. Test deep link
adb shell am start -a android.intent.action.VIEW \
  -d "ziraai://payment-callback?token=test&status=success"

# 3. Expected: App opens and handles callback
```

### 5. Production Cutover

**Pre-Deployment:**

- [ ] All tests passing (unit, integration, E2E)
- [ ] Database migrations reviewed and tested
- [ ] iyzico production credentials obtained
- [ ] Cache invalidation verified in staging
- [ ] Invoice fields tested with both personal and corporate scenarios
- [ ] Mobile app approved by internal testers
- [ ] Rollback plan documented

**Deployment Steps:**

1. **Database Migration** (30 minutes before)
   ```bash
   dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext
   ```

2. **Backend Deployment** (Railway)
   ```bash
   git checkout main
   git merge feature/payment-integration
   git push origin main
   # Railway auto-deploys to production
   ```

3. **Mobile Deployment** (Google Play)
   - Upload production APK to Google Play Console
   - Release to staged rollout (10% → 50% → 100%)

4. **Verification** (immediately after)
   - [ ] Test payment with production card
   - [ ] Verify dashboard shows new purchase
   - [ ] Check Redis cache invalidation
   - [ ] Monitor error logs for 1 hour

**Post-Deployment:**

- [ ] Monitor payment success rate
- [ ] Check for failed transactions in database
- [ ] Review user feedback
- [ ] Update documentation with production URLs

---

## Troubleshooting

### Problem 1: "Webpage not available" in WebView

**Symptoms:**

- WebView loads payment form successfully
- User fills card details
- Click "Pay" button
- Error: "Webpage not available"
- 3D Secure never loads

**Root Cause:**

Callback URL is a deep link (`ziraai://payment-callback`) instead of HTTPS URL.

**Solution:**

```bash
# 1. Check callback URL in configuration
grep "FallbackUrl" WebAPI/appsettings.Staging.json

# Expected: "https://ziraai-api-sit.up.railway.app/api/v1/payments/callback"
# Wrong: "ziraai://payment-callback"

# 2. Update Railway environment variable
railway variables set Iyzico__Callback__FallbackUrl="https://ziraai-api-sit.up.railway.app/api/v1/payments/callback"

# 3. Redeploy
railway up
```

**Reference:** See [CALLBACK_URL_MUST_BE_HTTPS.md](./CALLBACK_URL_MUST_BE_HTTPS.md)

### Problem 2: Dashboard not showing new purchase

**Symptoms:**

- Payment completes successfully
- Database has purchase record
- Mobile dashboard shows old data (empty packages)
- Logging out and logging in doesn't help
- Only manual Redis cache deletion fixes it

**Root Cause:**

Cache not invalidated after purchase creation.

**Solution:**

```bash
# 1. Verify cache invalidation code exists
grep -A5 "Invalidate sponsor dashboard cache" Business/Services/Payment/IyzicoPaymentService.cs

# Expected output:
# var cacheKey = $"SponsorDashboard:{transaction.UserId}";
# _cacheManager.Remove(cacheKey);
# _logger.LogInformation($"[DashboardCache] 🗑️ Invalidated cache...");

# 2. Check deployment logs for invalidation message
railway logs | grep "DashboardCache.*Invalidated"

# Expected: "[DashboardCache] 🗑️ Invalidated cache for sponsor 189"
# If missing: Redeploy latest code

# 3. Verify commit includes cache fix
git log --oneline --grep="cache invalidation"
# Expected: e98e09b fix: Add dashboard cache invalidation after sponsor purchase creation
```

**Manual Fix (Emergency):**

```bash
# Connect to Railway Redis
railway connect redis

# Delete cache manually
DEL SponsorDashboard:189

# User refreshes dashboard → fresh data ✅
```

### Problem 3: Invoice fields NULL in database

**Symptoms:**

- Corporate invoice form filled correctly
- Payment completes successfully
- Database: `CompanyName`, `TaxNumber`, `InvoiceAddress` are NULL

**Root Cause:**

Backend not extracting invoice fields from `FlowDataJson`.

**Solution:**

```bash
# 1. Verify flowData includes invoice fields
grep -A10 "class SponsorBulkPurchaseFlowData" Entities/Dtos/Payment/PaymentInitializeRequestDto.cs

# Expected:
# public string CompanyName { get; set; }
# public string TaxNumber { get; set; }
# public string InvoiceAddress { get; set; }

# 2. Verify backend uses invoice fields
grep -A3 "CompanyName = flowData.CompanyName" Business/Services/Payment/IyzicoPaymentService.cs

# Expected output (lines 726-728):
# CompanyName = flowData.CompanyName,
# TaxNumber = flowData.TaxNumber,
# InvoiceAddress = flowData.InvoiceAddress,

# 3. Check mobile sends invoice fields
grep -A10 "initializeSponsorPurchase" lib/features/payment/services/payment_service.dart

# Expected: companyName, taxNumber, invoiceAddress parameters
```

**Reference:** See [INVOICE_FIELDS_IMPLEMENTATION.md](./INVOICE_FIELDS_IMPLEMENTATION.md)

### Problem 4: Payment stuck in "Pending"

**Symptoms:**

- Payment initialized successfully
- User fills card and completes 3D Secure
- But transaction stays "Pending"
- No callback received

**Diagnosis:**

```sql
-- Check transaction status
SELECT 
    "Id",
    "UserId",
    "Status",
    "IyzicoToken",
    "IyzicoPaymentId",
    "CreatedAt",
    "CompletedAt"
FROM "PaymentTransactions"
WHERE "Id" = 19;

-- Check backend logs for callback
-- Expected: "[Payment] Callback received from iyzico"
-- If missing: Callback never reached backend
```

**Possible Causes:**

1. **iyzico cannot reach callback URL** (firewall/network)
   ```bash
   # Test callback URL is accessible
   curl -X POST https://ziraai-api-sit.up.railway.app/api/v1/payments/callback \
        -d "token=test-token-123"
   
   # Expected: HTTP 302 redirect (not 404 or 500)
   ```

2. **Callback URL misconfigured**
   ```bash
   # Check Railway environment
   railway variables | grep Callback
   
   # Should match appsettings.Staging.json
   ```

3. **SSL certificate issue**
   ```bash
   # Verify SSL certificate valid
   curl -v https://ziraai-api-sit.up.railway.app/health
   
   # Should NOT show SSL errors
   ```

**Recovery:**

```csharp
// Manual verification endpoint (for stuck transactions)
[HttpPost("manual-verify/{transactionId}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> ManualVerify(int transactionId)
{
    var transaction = await _paymentTransactionRepository.GetAsync(t => t.Id == transactionId);
    if (transaction == null)
        return NotFound();

    var result = await _paymentService.VerifyPaymentAsync(transaction.IyzicoToken);
    
    if (result.Success && result.Data.Status == "Success")
    {
        await _paymentService.ProcessPaymentCallbackAsync(transaction.IyzicoToken);
        return Ok("Payment processed successfully");
    }

    return BadRequest(result.Message);
}
```

### Problem 5: HMAC Signature Mismatch

**Symptoms:**

- Payment initialization fails
- iyzico returns: "Invalid signature"
- Backend logs show signature generation

**Diagnosis:**

```csharp
// Enable detailed logging
_logger.LogDebug("[iyzico] Random Key: {RandomKey}", randomKey);
_logger.LogDebug("[iyzico] URI Path: {UriPath}", uriPath);
_logger.LogDebug("[iyzico] Data to hash: {Data}", dataToHash);
_logger.LogDebug("[iyzico] Signature: {Signature}", signature);
```

**Common Mistakes:**

1. **Whitespace in JSON**
   ```csharp
   // WRONG
   var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
   
   // CORRECT
   var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = false });
   ```

2. **Wrong URI path**
   ```csharp
   // WRONG
   var uriPath = "/payment/initialize";
   
   // CORRECT
   var uriPath = "/payment/iyzipos/checkoutform/initialize/auth/ecom";
   ```

3. **Wrong secret key**
   ```bash
   # Verify secret key matches iyzico dashboard
   echo $Iyzico__SecretKey
   
   # Should start with "sandbox-" for staging
   ```

---

## Appendix

### A. iyzico API Reference

**Base URLs:**

- Sandbox: `https://sandbox-api.iyzipay.com`
- Production: `https://api.iyzipay.com`

**Key Endpoints:**

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/payment/iyzipos/checkoutform/initialize/auth/ecom` | POST | Initialize payment |
| `/payment/iyzipos/checkoutform/auth/ecom/detail` | POST | Verify payment |

**Request Headers:**

```
Authorization: IYZWSv2 {base64(apiKey:xxx&randomKey:yyy&signature:zzz)}
Content-Type: application/json
x-iyzi-client-version: iyzipay-dotnet-2.1.39
```

**Initialize Request Schema:**

```json
{
  "locale": "tr",
  "conversationId": "unique-id",
  "price": 4999.50,
  "paidPrice": 4999.50,
  "currency": "TRY",
  "basketId": "unique-id",
  "paymentChannel": "MOBILE",
  "paymentGroup": "SUBSCRIPTION",
  "callbackUrl": "https://api.ziraai.com/api/v1/payments/callback",
  "enabledInstallments": [1],
  "buyer": { ... },
  "shippingAddress": { ... },
  "billingAddress": { ... },
  "basketItems": [ ... ]
}
```

**Initialize Response Schema:**

```json
{
  "status": "success",
  "locale": "tr",
  "systemTime": 1763807744153,
  "conversationId": "unique-id",
  "token": "payment-token",
  "paymentPageUrl": "https://sandbox-cpp.iyzipay.com?token=xxx",
  "signature": "response-signature"
}
```

### B. Database Schema SQL

```sql
-- PaymentTransaction table
CREATE TABLE "PaymentTransactions" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "FlowType" VARCHAR(50) NOT NULL,
    "FlowDataJson" TEXT NOT NULL,
    "Amount" DECIMAL(18,2) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL,
    "Status" VARCHAR(20) NOT NULL,
    "IyzicoToken" VARCHAR(255),
    "IyzicoPaymentId" VARCHAR(255),
    "IyzicoConversationId" VARCHAR(255),
    "CreatedAt" TIMESTAMP NOT NULL,
    "CompletedAt" TIMESTAMP,
    "CancelledAt" TIMESTAMP,
    "ErrorCode" VARCHAR(50),
    "ErrorMessage" TEXT,
    "SponsorshipPurchaseId" INTEGER,
    "UserSubscriptionId" INTEGER,
    
    CONSTRAINT "FK_PaymentTransactions_Users" FOREIGN KEY ("UserId") REFERENCES "Users"("Id"),
    CONSTRAINT "FK_PaymentTransactions_SponsorshipPurchases" FOREIGN KEY ("SponsorshipPurchaseId") REFERENCES "SponsorshipPurchases"("Id"),
    CONSTRAINT "FK_PaymentTransactions_UserSubscriptions" FOREIGN KEY ("UserSubscriptionId") REFERENCES "UserSubscriptions"("Id")
);

CREATE INDEX "IX_PaymentTransactions_UserId" ON "PaymentTransactions"("UserId");
CREATE INDEX "IX_PaymentTransactions_IyzicoToken" ON "PaymentTransactions"("IyzicoToken");
CREATE INDEX "IX_PaymentTransactions_Status" ON "PaymentTransactions"("Status");

-- SponsorshipPurchase table (updated with invoice fields)
CREATE TABLE "SponsorshipPurchases" (
    "Id" SERIAL PRIMARY KEY,
    "SponsorId" INTEGER NOT NULL,
    "SubscriptionTierId" INTEGER NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "UnitPrice" DECIMAL(18,2) NOT NULL,
    "TotalAmount" DECIMAL(18,2) NOT NULL,
    "Currency" VARCHAR(3) NOT NULL,
    
    -- Invoice fields (NEW)
    "CompanyName" VARCHAR(255),
    "TaxNumber" VARCHAR(50),
    "InvoiceAddress" TEXT,
    "InvoiceNumber" VARCHAR(50),
    
    "PurchaseDate" TIMESTAMP NOT NULL,
    "PaymentMethod" VARCHAR(50),
    "PaymentReference" VARCHAR(255),
    "PaymentStatus" VARCHAR(20),
    "PaymentCompletedDate" TIMESTAMP,
    "CodePrefix" VARCHAR(10),
    "ValidityDays" INTEGER,
    "Status" VARCHAR(20),
    "CodesGenerated" INTEGER,
    "CodesUsed" INTEGER,
    "CreatedDate" TIMESTAMP NOT NULL,
    "PaymentTransactionId" INTEGER,
    
    CONSTRAINT "FK_SponsorshipPurchases_Users" FOREIGN KEY ("SponsorId") REFERENCES "Users"("Id"),
    CONSTRAINT "FK_SponsorshipPurchases_SubscriptionTiers" FOREIGN KEY ("SubscriptionTierId") REFERENCES "SubscriptionTiers"("Id"),
    CONSTRAINT "FK_SponsorshipPurchases_PaymentTransactions" FOREIGN KEY ("PaymentTransactionId") REFERENCES "PaymentTransactions"("Id")
);

CREATE INDEX "IX_SponsorshipPurchases_SponsorId" ON "SponsorshipPurchases"("SponsorId");
CREATE INDEX "IX_SponsorshipPurchases_Status" ON "SponsorshipPurchases"("Status");
```

### C. Environment Variables Complete Reference

**Development:**

```bash
ASPNETCORE_ENVIRONMENT=Development
Iyzico__BaseUrl=https://sandbox-api.iyzipay.com
Iyzico__ApiKey=sandbox-xxx
Iyzico__SecretKey=sandbox-yyy
Iyzico__Callback__FallbackUrl=http://localhost:5001/api/v1/payments/callback
WebAPI__BaseUrl=http://localhost:5001
```

**Staging:**

```bash
ASPNETCORE_ENVIRONMENT=Staging
Iyzico__BaseUrl=https://sandbox-api.iyzipay.com
Iyzico__ApiKey=sandbox-xxx
Iyzico__SecretKey=sandbox-yyy
Iyzico__Callback__FallbackUrl=https://ziraai-api-sit.up.railway.app/api/v1/payments/callback
WebAPI__BaseUrl=https://ziraai-api-sit.up.railway.app
```

**Production:**

```bash
ASPNETCORE_ENVIRONMENT=Production
Iyzico__BaseUrl=https://api.iyzipay.com
Iyzico__ApiKey=production-xxx
Iyzico__SecretKey=production-yyy
Iyzico__Callback__FallbackUrl=https://api.ziraai.com/api/v1/payments/callback
WebAPI__BaseUrl=https://api.ziraai.com
```

### D. Monitoring Queries

**Payment Success Rate (Last 24h):**

```sql
SELECT 
    "Status",
    COUNT(*) as "Count",
    ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 2) as "Percentage"
FROM "PaymentTransactions"
WHERE "CreatedAt" > NOW() - INTERVAL '24 hours'
GROUP BY "Status"
ORDER BY "Count" DESC;
```

**Average Payment Processing Time:**

```sql
SELECT 
    AVG(EXTRACT(EPOCH FROM ("CompletedAt" - "CreatedAt"))) as "AvgSeconds",
    MIN(EXTRACT(EPOCH FROM ("CompletedAt" - "CreatedAt"))) as "MinSeconds",
    MAX(EXTRACT(EPOCH FROM ("CompletedAt" - "CreatedAt"))) as "MaxSeconds"
FROM "PaymentTransactions"
WHERE "Status" = 'Success' 
  AND "CompletedAt" IS NOT NULL
  AND "CreatedAt" > NOW() - INTERVAL '7 days';
```

**Failed Payments Analysis:**

```sql
SELECT 
    "ErrorCode",
    "ErrorMessage",
    COUNT(*) as "Count"
FROM "PaymentTransactions"
WHERE "Status" = 'Failed'
  AND "CreatedAt" > NOW() - INTERVAL '7 days'
GROUP BY "ErrorCode", "ErrorMessage"
ORDER BY "Count" DESC
LIMIT 10;
```

**Revenue by Flow Type:**

```sql
SELECT 
    "FlowType",
    COUNT(*) as "Transactions",
    SUM("Amount") as "TotalRevenue",
    AVG("Amount") as "AvgAmount"
FROM "PaymentTransactions"
WHERE "Status" = 'Success'
  AND "CreatedAt" > NOW() - INTERVAL '30 days'
GROUP BY "FlowType"
ORDER BY "TotalRevenue" DESC;
```

---

## Document Changelog

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-11-22 | 1.0.0 | Initial comprehensive guide | Claude |

**Next Update:** Add production metrics and optimization recommendations after 1 month in production.

---

**END OF DOCUMENT**

This guide provides complete end-to-end payment implementation details for integrating iyzico payment gateway into any flow in the ZiraAI system. Use this as a reference when implementing new payment flows (Dealer Purchase, Premium Features, etc.).
