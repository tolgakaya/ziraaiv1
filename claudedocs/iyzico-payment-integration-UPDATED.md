# iyzico Payment Integration - UPDATED Analysis (Both Flows)

**Date:** 2025-11-21  
**Update Reason:** Discovered existing `/api/v1/subscriptions/subscribe` endpoint for Farmer subscriptions

---

## 🔍 Critical Discovery

The codebase has **TWO separate payment flows**, both with mock payment:

### Flow 1: Farmer Individual Subscription
- **Endpoint:** `POST /api/v1/subscriptions/subscribe`
- **Controller:** [SubscriptionsController.cs](../WebAPI/Controllers/SubscriptionsController.cs:151)
- **User:** Farmer role (individual users)
- **Purpose:** Buy subscription for personal use
- **Current Payment:** Mock - just stores `PaymentMethod` and `PaymentReference` strings

### Flow 2: Sponsor Bulk Code Purchase
- **Endpoint:** `POST /api/v1/sponsorship/purchase-package`
- **Controller:** [SponsorshipController.cs](../WebAPI/Controllers/SponsorshipController.cs:316)
- **User:** Sponsor role (companies/organizations)
- **Purpose:** Buy bulk subscription codes to distribute to farmers
- **Current Payment:** Mock - auto-approves all purchases

---

## 🎯 Updated Implementation Strategy

### Option A: Unified Payment Service (⭐ RECOMMENDED)

Create a **single iyzico payment service** that serves BOTH flows:

```
┌─────────────────────────────────┐
│   IyzicoPaymentService          │
│   (Shared across both flows)    │
└───────────┬─────────────────────┘
            │
    ┌───────┴────────┐
    │                │
    ↓                ↓
Farmer Flow    Sponsor Flow
Subscribe      Purchase Bulk
```

**Benefits:**
- ✅ Single authentication logic
- ✅ Shared webhook handler
- ✅ Consistent error handling
- ✅ Easier maintenance
- ✅ Reusable code

### Option B: Separate Payment Services

Create separate services for different business logic needs:

```
FarmerPaymentService → IyzicoPaymentService
SponsorPaymentService → IyzicoPaymentService
```

**When to use:** If business logic differs significantly (installments, invoicing, etc.)

---

## 📊 Comparison: Farmer vs Sponsor Flow

| Aspect | Farmer Subscribe | Sponsor Purchase |
|--------|------------------|------------------|
| **User Type** | Individual (Farmer) | Organization (Sponsor) |
| **Purchase Type** | Personal subscription | Bulk codes for distribution |
| **Payment Amount** | Tier price × months | Tier price × quantity |
| **Invoice Required** | Optional | **Required** (Company name, Tax number) |
| **Trial Support** | ✅ Yes | ❌ No (Trial tier blocked) |
| **Auto-Renew** | ✅ Yes | ❌ No |
| **Output** | User subscription record | Sponsorship codes |
| **Quantity Limits** | N/A | Min/Max per tier |
| **Current Payment** | Mock | Mock |
| **Priority** | 🔴 HIGH | 🔴 HIGH |

---

## 🚀 Recommended Implementation Plan

### Phase 1: Shared iyzico Service (Week 1)

**Create base payment infrastructure:**

```csharp
// New service: Business/Services/Payment/IyzicoPaymentService.cs
public interface IIyzicoPaymentService
{
    // Shared methods
    Task<IDataResult<PaymentInitializeResponse>> InitializePWIAsync(
        PaymentInitializeRequest request);
    
    Task<IDataResult<PaymentVerificationResponse>> VerifyPaymentAsync(
        string token);
    
    Task<bool> ValidateWebhookSignatureAsync(
        string signature, object payload);
}

// Request wrapper for different flows
public class PaymentInitializeRequest
{
    // Common fields
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string ConversationId { get; set; }
    
    // Buyer info
    public string BuyerName { get; set; }
    public string BuyerEmail { get; set; }
    public string BuyerPhone { get; set; }
    public string BuyerAddress { get; set; }
    public string IpAddress { get; set; }
    
    // Basket items
    public List<PaymentBasketItem> Items { get; set; }
    
    // Flow-specific
    public PaymentFlowType FlowType { get; set; }
    public object FlowData { get; set; }  // FarmerSubscriptionData or SponsorPurchaseData
}

public enum PaymentFlowType
{
    FarmerSubscription,
    SponsorBulkPurchase
}
```

### Phase 2: Update Farmer Subscribe Endpoint (Week 1-2)

**Current Code:** [SubscriptionsController.cs:151-210](../WebAPI/Controllers/SubscriptionsController.cs:151)

**Changes Needed:**

**Step 1: Initialize Payment Instead of Direct Subscribe**
```csharp
[HttpPost("subscribe")]
[Authorize(Roles = "Farmer,Admin")]
public async Task<IActionResult> Subscribe([FromBody] CreateUserSubscriptionDto request)
{
    var userId = GetUserId();
    if (!userId.HasValue)
        return Unauthorized();

    // Validate tier
    var tier = await _tierRepository.GetAsync(t => t.Id == request.SubscriptionTierId && t.IsActive);
    if (tier == null)
        return BadRequest(new ErrorResult("Invalid subscription tier"));

    // Calculate amount
    var durationMonths = request.DurationMonths ?? 1;
    var amount = tier.MonthlyPrice * durationMonths;

    // Get user details
    var user = await _userRepository.GetAsync(u => u.UserId == userId.Value);
    
    // Initialize payment with iyzico
    var paymentRequest = new PaymentInitializeRequest
    {
        UserId = userId.Value,
        Amount = amount,
        Currency = tier.Currency,
        ConversationId = Guid.NewGuid().ToString(),
        BuyerName = user.FullName,
        BuyerEmail = user.Email,
        BuyerPhone = user.MobilePhones,
        BuyerAddress = user.Address,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        FlowType = PaymentFlowType.FarmerSubscription,
        FlowData = new FarmerSubscriptionData
        {
            SubscriptionTierId = request.SubscriptionTierId,
            DurationMonths = durationMonths,
            IsTrialSubscription = request.IsTrialSubscription,
            AutoRenew = request.AutoRenew
        },
        Items = new List<PaymentBasketItem>
        {
            new PaymentBasketItem
            {
                Id = $"SUB-{tier.Id}",
                Name = $"{tier.DisplayName} Subscription ({durationMonths} month)",
                Category = "Subscription",
                ItemType = "VIRTUAL",
                Price = amount
            }
        }
    };

    var result = await _iyzicoPaymentService.InitializePWIAsync(paymentRequest);
    
    if (!result.Success)
        return BadRequest(result);

    // Store pending payment
    var paymentTransaction = new PaymentTransaction
    {
        UserId = userId.Value,
        IyzicoToken = result.Data.Token,
        ConversationId = paymentRequest.ConversationId,
        Amount = amount,
        Currency = tier.Currency,
        Status = "Initialized",
        InitializedAt = DateTime.Now,
        ExpiresAt = result.Data.ExpiresAt,
        FlowType = PaymentFlowType.FarmerSubscription,
        FlowDataJson = JsonConvert.SerializeObject(paymentRequest.FlowData),
        InitializeResponse = result.Data.RawResponse
    };

    _paymentTransactionRepository.Add(paymentTransaction);
    await _paymentTransactionRepository.SaveChangesAsync();

    return Ok(new SuccessDataResult<PaymentInitializeDto>(new PaymentInitializeDto
    {
        PaymentTransactionId = paymentTransaction.Id,
        PaymentToken = result.Data.Token,
        PaymentUrl = result.Data.PaymentUrl,
        ExpiresAt = result.Data.ExpiresAt,
        Amount = amount,
        Currency = tier.Currency
    }));
}
```

**Step 2: New Verify Payment Endpoint**
```csharp
[HttpPost("verify-subscription-payment")]
[Authorize(Roles = "Farmer,Admin")]
public async Task<IActionResult> VerifySubscriptionPayment([FromBody] VerifyPaymentDto request)
{
    var userId = GetUserId();
    if (!userId.HasValue)
        return Unauthorized();

    // Get payment transaction
    var transaction = await _paymentTransactionRepository.GetAsync(
        t => t.IyzicoToken == request.PaymentToken && 
             t.UserId == userId.Value &&
             t.FlowType == PaymentFlowType.FarmerSubscription);

    if (transaction == null)
        return NotFound(new ErrorResult("Payment transaction not found"));

    if (transaction.Status == "Success")
        return Ok(new SuccessResult("Payment already verified"));

    // Verify with iyzico
    var verifyResult = await _iyzicoPaymentService.VerifyPaymentAsync(request.PaymentToken);
    
    if (!verifyResult.Success)
        return BadRequest(verifyResult);

    // Update transaction
    transaction.Status = verifyResult.Data.IsSuccess ? "Success" : "Failed";
    transaction.CompletedAt = DateTime.Now;
    transaction.IyzicoPaymentId = verifyResult.Data.PaymentId;
    transaction.VerifyResponse = verifyResult.Data.RawResponse;
    _paymentTransactionRepository.Update(transaction);

    if (verifyResult.Data.IsSuccess)
    {
        // Create subscription
        var flowData = JsonConvert.DeserializeObject<FarmerSubscriptionData>(transaction.FlowDataJson);
        
        // Check and cancel trial if upgrading
        var existingSubscription = await _userSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId.Value);
        if (existingSubscription?.IsTrialSubscription == true && !flowData.IsTrialSubscription)
        {
            existingSubscription.IsActive = false;
            existingSubscription.Status = "Upgraded";
            existingSubscription.CancellationDate = DateTime.Now;
            existingSubscription.CancellationReason = "Upgraded to paid subscription";
            _userSubscriptionRepository.Update(existingSubscription);
        }

        var tier = await _tierRepository.GetAsync(t => t.Id == flowData.SubscriptionTierId);
        
        var subscription = new UserSubscription
        {
            UserId = userId.Value,
            SubscriptionTierId = flowData.SubscriptionTierId,
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddMonths(flowData.DurationMonths),
            IsActive = true,
            AutoRenew = flowData.AutoRenew,
            PaymentMethod = "iyzico",
            PaymentReference = verifyResult.Data.PaymentId,
            PaymentTransactionId = transaction.Id,
            IyzicoPaymentId = verifyResult.Data.PaymentId,
            PaidAmount = transaction.Amount,
            Currency = transaction.Currency,
            LastPaymentDate = DateTime.Now,
            NextPaymentDate = DateTime.Now.AddMonths(flowData.DurationMonths),
            CurrentDailyUsage = 0,
            CurrentMonthlyUsage = 0,
            LastUsageResetDate = DateTime.Now,
            MonthlyUsageResetDate = DateTime.Now,
            Status = "Active",
            IsTrialSubscription = flowData.IsTrialSubscription,
            CreatedDate = DateTime.Now,
            CreatedUserId = userId.Value
        };

        _userSubscriptionRepository.Add(subscription);
        transaction.UserSubscriptionId = subscription.Id;
        _paymentTransactionRepository.Update(transaction);
    }

    await _paymentTransactionRepository.SaveChangesAsync();

    return verifyResult.Data.IsSuccess
        ? Ok(new SuccessResult("Subscription activated successfully"))
        : BadRequest(new ErrorResult($"Payment failed: {verifyResult.Data.ErrorMessage}"));
}
```

### Phase 3: Update Sponsor Purchase Endpoint (Week 2)

**Current Code:** [SponsorshipController.cs:316](../WebAPI/Controllers/SponsorshipController.cs:316)

**Similar changes but with sponsor-specific logic:**
- Invoice requirements (company name, tax number)
- Quantity validation
- Sponsorship code generation after payment success

### Phase 4: Unified Payment Controller (Week 2-3)

```csharp
[Route("api/v{version:apiVersion}/payment")]
[ApiController]
public class PaymentController : BaseApiController
{
    private readonly IIyzicoPaymentService _iyzicoPaymentService;
    private readonly IPaymentTransactionRepository _paymentTransactionRepository;

    // Common endpoints for both flows
    
    [HttpPost("verify")]
    [Authorize]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentDto request)
    {
        // Shared verification logic
        // Routes to appropriate flow handler based on transaction FlowType
    }

    [HttpPost("webhooks/iyzico")]
    [AllowAnonymous]
    public async Task<IActionResult> IyzicoWebhook(
        [FromHeader(Name = "X-IYZ-SIGNATURE-V3")] string signature)
    {
        // Shared webhook handler
        // Routes to appropriate flow based on conversationId lookup
    }

    [HttpGet("status/{token}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentStatus(string token)
    {
        // Check payment status (for polling)
    }
}
```

---

## 🔄 Database Schema Updates

### New Table: PaymentTransactions
```sql
CREATE TABLE PaymentTransactions (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL,
    
    -- Flow identification
    FlowType VARCHAR(50) NOT NULL,  -- 'FarmerSubscription' or 'SponsorBulkPurchase'
    FlowDataJson NVARCHAR(MAX) NOT NULL,  -- Flow-specific data as JSON
    
    -- Links to result tables
    UserSubscriptionId INT NULL,  -- For Farmer flow
    SponsorshipPurchaseId INT NULL,  -- For Sponsor flow
    
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
    ExpiresAt DATETIME2 NOT NULL,
    
    -- Response data
    InitializeResponse NVARCHAR(MAX) NULL,
    VerifyResponse NVARCHAR(MAX) NULL,
    
    -- Audit
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedDate DATETIME2 NULL,
    
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (UserSubscriptionId) REFERENCES UserSubscriptions(Id),
    FOREIGN KEY (SponsorshipPurchaseId) REFERENCES SponsorshipPurchases(Id),
    INDEX IX_Token (IyzicoToken),
    INDEX IX_Status_Flow (Status, FlowType),
    INDEX IX_Expires (ExpiresAt)
);
```

### Update Existing Tables

**UserSubscriptions:**
```sql
ALTER TABLE UserSubscriptions
ADD PaymentTransactionId INT NULL,
ADD IyzicoPaymentId VARCHAR(255) NULL;

ALTER TABLE UserSubscriptions
ADD FOREIGN KEY (PaymentTransactionId) REFERENCES PaymentTransactions(Id);
```

**SponsorshipPurchases:**
```sql
ALTER TABLE SponsorshipPurchases
ADD PaymentTransactionId INT NULL,
ADD IyzicoPaymentId VARCHAR(255) NULL;

ALTER TABLE SponsorshipPurchases
ADD FOREIGN KEY (PaymentTransactionId) REFERENCES PaymentTransactions(Id);
```

---

## 📱 Mobile App Impact

### For Farmer Flow:

**OLD:**
```dart
// Direct subscribe
final response = await api.subscribe(
  tierId: selectedTier.id,
  durationMonths: 1,
  paymentMethod: "CreditCard",  // Mock!
);
```

**NEW:**
```dart
// Step 1: Initialize payment
final initResponse = await api.initializeSubscription(
  tierId: selectedTier.id,
  durationMonths: 1,
);

// Step 2: Open payment page
await Navigator.push(
  context,
  MaterialPageRoute(
    builder: (_) => PaymentWebView(url: initResponse.paymentUrl),
  ),
);

// Step 3: Verify after deep link callback
final verifyResponse = await api.verifySubscriptionPayment(
  token: callbackToken,
);
```

### For Sponsor Flow:

**Same pattern as Farmer but:**
- Different endpoint: `initializeSponsorPurchase`
- Different verify: `verifySponsorPurchase`
- Additional invoice fields in initialize request

---

## ⚖️ Priority Recommendations

### Option 1: Sequential Implementation (SAFER)

**Week 1-2:** Implement **Sponsor flow only**
- Higher transaction value (bulk purchases)
- Fewer users to test with
- Invoice requirements already clear
- Company payment cards (less 3DS issues)

**Week 3-4:** Implement **Farmer flow**
- Learn from sponsor flow experience
- Handle individual card payments
- Deal with trial upgrade logic

### Option 2: Parallel Implementation (FASTER)

**Week 1-3:** Both flows together
- Shared service ready from start
- Single mobile app release
- Unified testing

**Recommendation:** **Option 1 (Sequential)** - Start with Sponsor flow, learn, then Farmer.

---

## 🔐 Security Differences

### Farmer Flow:
- Individual credit cards
- More 3DS authentication challenges
- Higher fraud risk (smaller amounts)
- Personal data protection (KVKK)

### Sponsor Flow:
- Company credit cards
- Business transactions (less fraud)
- Invoice requirements (official documents)
- Higher transaction amounts (better margins)

Both use **same security**: HMACSHA256, HTTPS, webhook validation.

---

## 💡 Next Steps

1. ❓ Which flow should we implement first? **Sponsor or Farmer?**
2. ❓ Do you want **sequential** (safer) or **parallel** (faster)?
3. ❓ Should we support **installments** (taksit) for either flow?
4. ❓ Do you have **different iyzico accounts** for farmer vs sponsor transactions?
5. ❓ Invoice generation - automatic or manual after payment?

---

**Original Analysis:** [iyzico-payment-integration-analysis.md](./iyzico-payment-integration-analysis.md)  
**Status:** ✅ UPDATED - Both flows identified and analyzed  
**Recommendation:** Implement Sponsor flow first (Week 1-2), then Farmer flow (Week 3-4)
