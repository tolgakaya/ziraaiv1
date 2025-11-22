# Backend Web Payment Integration Requirements

## Overview

Web frontend için payment entegrasyonu tamamlandı. Ancak backend'in mobile deep link redirect yerine web URL'ine redirect etmesi gerekiyor. Bu doküman backend'de yapılması gereken değişiklikleri detaylı şekilde açıklar.

## Problem

**Mevcut Durum:**
- Backend `/payments/callback` endpoint'i iyzico'dan callback alıyor ✅
- Payment başarılı oluyor, purchase create ediliyor ✅
- Ancak backend mobile deep link'e redirect ediyor: `ziraai://payment-callback?token=xxx` ❌
- Web browser bu deep link'i açamıyor, kullanıcı sonucu göremiyor ❌

**İstenen Durum:**
- Mobile için: `ziraai://payment-callback?token=xxx&status=success` (mevcut)
- **Web için: `https://ziraai-staging.up.railway.app/sponsor/payment-callback?token=xxx&status=success`** (yeni)

---

## Solution 1: Platform Field ile Redirect (Önerilen)

### 1.1. PaymentInitializeRequestDto Değişikliği

**Dosya:** `Entities/Dtos/Payment/PaymentInitializeRequestDto.cs`

```csharp
public class PaymentInitializeRequestDto
{
    [Required]
    public string FlowType { get; set; }  // "SponsorBulkPurchase", "FarmerSubscription"

    [Required]
    public object FlowData { get; set; }

    /// <summary>
    /// Platform from which payment is initiated
    /// Used to determine callback redirect URL
    /// </summary>
    [Required]
    public string Platform { get; set; }  // "iOS", "Android", "Web"
}
```

### 1.2. PaymentTransaction Entity Değişikliği

**Dosya:** `Entities/Concrete/PaymentTransaction.cs`

```csharp
public class PaymentTransaction : IEntity
{
    public int Id { get; set; }

    // User & Flow
    public int UserId { get; set; }
    public string FlowType { get; set; }
    public string FlowDataJson { get; set; }

    // Payment Details
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string Status { get; set; }

    // iyzico Integration
    public string IyzicoToken { get; set; }
    public string IyzicoPaymentId { get; set; }
    public string IyzicoConversationId { get; set; }

    // NEW: Platform information
    /// <summary>
    /// Platform from which payment was initiated: "iOS", "Android", "Web"
    /// </summary>
    public string Platform { get; set; }  // NEW FIELD

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Foreign Keys
    public int? SponsorshipPurchaseId { get; set; }
    public int? UserSubscriptionId { get; set; }
}
```

**Migration:**
```csharp
public partial class AddPlatformToPaymentTransaction : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Platform",
            table: "PaymentTransactions",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "iOS"); // Default for existing records
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Platform",
            table: "PaymentTransactions");
    }
}
```

### 1.3. PaymentService Initialize Değişikliği

**Dosya:** `Business/Concrete/PaymentService.cs`

**Metod:** `InitializePaymentAsync`

```csharp
public async Task<IDataResult<PaymentInitializeResponseDto>> InitializePaymentAsync(
    int userId,
    PaymentInitializeRequestDto request)
{
    _logger.LogInformation("[iyzico] Initializing payment for User {UserId}, FlowType: {FlowType}, Platform: {Platform}",
        userId, request.FlowType, request.Platform);

    try
    {
        // 1. Validate platform
        if (!new[] { "iOS", "Android", "Web" }.Contains(request.Platform))
        {
            return new ErrorDataResult<PaymentInitializeResponseDto>("Invalid platform. Must be iOS, Android, or Web");
        }

        // 2. Get user details
        var user = await _userRepository.GetAsync(u => u.Id == userId);
        if (user == null)
            return new ErrorDataResult<PaymentInitializeResponseDto>("User not found");

        // 3. Calculate amount based on flow type
        decimal amount;
        string flowDataJson;

        switch (request.FlowType)
        {
            // ... existing flow type logic ...
        }

        // 4. Create payment transaction with platform info
        var transaction = new PaymentTransaction
        {
            UserId = userId,
            FlowType = request.FlowType,
            FlowDataJson = flowDataJson,
            Amount = amount,
            Currency = "TRY",
            Status = "Pending",
            Platform = request.Platform,  // NEW: Store platform
            CreatedAt = DateTime.UtcNow
        };

        await _paymentTransactionRepository.AddAsync(transaction);

        // 5. Initialize iyzico payment
        var iyzicoRequest = new CreateCheckoutFormInitializeRequest
        {
            locale = Locale.TR.ToString(),
            conversationId = transaction.Id.ToString(),
            price = amount.ToString("F2", CultureInfo.InvariantCulture),
            paidPrice = amount.ToString("F2", CultureInfo.InvariantCulture),
            currency = Currency.TRY.ToString(),
            basketId = Guid.NewGuid().ToString(),
            paymentGroup = PaymentGroup.PRODUCT.ToString(),

            // NEW: Platform-specific callback URL
            callbackUrl = GetCallbackUrl(request.Platform),

            buyer = new Buyer
            {
                id = userId.ToString(),
                name = user.Name ?? "N/A",
                surname = user.Surname ?? "N/A",
                email = user.Email,
                identityNumber = "11111111111",
                registrationAddress = "N/A",
                city = "N/A",
                country = "Turkey",
                ip = "127.0.0.1"
            },
            shippingAddress = new Address
            {
                contactName = $"{user.Name} {user.Surname}",
                city = "N/A",
                country = "Turkey",
                address = "N/A"
            },
            billingAddress = new Address
            {
                contactName = $"{user.Name} {user.Surname}",
                city = "N/A",
                country = "Turkey",
                address = "N/A"
            },
            basketItems = new List<BasketItem>
            {
                new BasketItem
                {
                    id = "1",
                    name = GetBasketItemName(request.FlowType),
                    category1 = "Subscription",
                    itemType = BasketItemType.VIRTUAL.ToString(),
                    price = amount.ToString("F2", CultureInfo.InvariantCulture)
                }
            }
        };

        var response = CheckoutFormInitialize.Create(iyzicoRequest, _options);

        if (response.Status != "success")
        {
            _logger.LogError("[iyzico] Payment initialization failed. Error: {Error}", response.ErrorMessage);
            transaction.Status = "Failed";
            transaction.ErrorCode = response.ErrorCode;
            transaction.ErrorMessage = response.ErrorMessage;
            await _paymentTransactionRepository.UpdateAsync(transaction);

            return new ErrorDataResult<PaymentInitializeResponseDto>(response.ErrorMessage);
        }

        // Update transaction with iyzico token
        transaction.IyzicoToken = response.Token;
        transaction.IyzicoConversationId = response.ConversationId;
        await _paymentTransactionRepository.UpdateAsync(transaction);

        _logger.LogInformation("[iyzico] Payment initialized successfully. TransactionId: {TransactionId}, Token: {Token}",
            transaction.Id, response.Token);

        return new SuccessDataResult<PaymentInitializeResponseDto>(new PaymentInitializeResponseDto
        {
            TransactionId = transaction.Id,
            PaymentPageUrl = response.PaymentPageUrl,
            PaymentToken = response.Token,
            CallbackUrl = iyzicoRequest.CallbackUrl,
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

/// <summary>
/// Get platform-specific callback URL
/// </summary>
private string GetCallbackUrl(string platform)
{
    return platform switch
    {
        "iOS" => $"{_configuration["ApiBaseUrl"]}/api/v1/payments/callback",
        "Android" => $"{_configuration["ApiBaseUrl"]}/api/v1/payments/callback",
        "Web" => $"{_configuration["ApiBaseUrl"]}/api/v1/payments/callback",
        _ => $"{_configuration["ApiBaseUrl"]}/api/v1/payments/callback"
    };
}
```

### 1.4. Callback Endpoint Değişikliği

**Dosya:** `WebAPI/Controllers/PaymentController.cs`

**Metod:** `PaymentCallback`

```csharp
[HttpPost("callback")]
[AllowAnonymous]
public async Task<IActionResult> PaymentCallback([FromForm] IyzicoCallbackRequest request)
{
    _logger.LogInformation("[Payment] Callback received from iyzico. Token: {Token}, Status: {Status}",
        request.Token, request.Status);

    try
    {
        // 1. Get transaction to determine platform
        var transaction = await _paymentTransactionRepository.GetAsync(t => t.IyzicoToken == request.Token);

        if (transaction == null)
        {
            _logger.LogError("[Payment] Transaction not found for token: {Token}", request.Token);
            return BadRequest("Transaction not found");
        }

        // 2. Verify payment with iyzico
        var verifyResult = await _paymentService.VerifyPaymentAsync(request.Token);

        if (!verifyResult.Success)
        {
            _logger.LogError("[Payment] Payment verification failed. Token: {Token}, Error: {Error}",
                request.Token, verifyResult.Message);

            // Redirect based on platform
            var errorRedirectUrl = GetErrorRedirectUrl(
                transaction.Platform,
                request.Token,
                verifyResult.Message
            );

            return Redirect(errorRedirectUrl);
        }

        // 3. Process successful payment
        await _paymentService.ProcessPaymentCallbackAsync(request.Token);

        // 4. Redirect based on platform
        var successRedirectUrl = GetSuccessRedirectUrl(
            transaction.Platform,
            request.Token
        );

        _logger.LogInformation("[Payment] Payment processed successfully. Platform: {Platform}, RedirectUrl: {Url}",
            transaction.Platform, successRedirectUrl);

        return Redirect(successRedirectUrl);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[Payment] Callback processing failed for token: {Token}", request.Token);

        // Try to get transaction for platform-based redirect
        var transaction = await _paymentTransactionRepository.GetAsync(t => t.IyzicoToken == request.Token);
        var platform = transaction?.Platform ?? "iOS"; // Default to iOS for backward compatibility

        var errorRedirectUrl = GetErrorRedirectUrl(platform, request.Token, ex.Message);
        return Redirect(errorRedirectUrl);
    }
}

/// <summary>
/// Get platform-specific success redirect URL
/// </summary>
private string GetSuccessRedirectUrl(string platform, string token)
{
    return platform switch
    {
        "iOS" => $"ziraai://payment-callback?token={token}&status=success",
        "Android" => $"ziraai://payment-callback?token={token}&status=success",
        "Web" => $"{_configuration["WebAppUrl"]}/sponsor/payment-callback?token={token}&status=success",
        _ => $"ziraai://payment-callback?token={token}&status=success" // Default to mobile
    };
}

/// <summary>
/// Get platform-specific error redirect URL
/// </summary>
private string GetErrorRedirectUrl(string platform, string token, string errorMessage)
{
    var encodedError = Uri.EscapeDataString(errorMessage);

    return platform switch
    {
        "iOS" => $"ziraai://payment-callback?token={token}&status=failed&error={encodedError}",
        "Android" => $"ziraai://payment-callback?token={token}&status=failed&error={encodedError}",
        "Web" => $"{_configuration["WebAppUrl"]}/sponsor/payment-callback?token={token}&status=failed&error={encodedError}",
        _ => $"ziraai://payment-callback?token={token}&status=failed&error={encodedError}"
    };
}
```

### 1.5. Configuration Settings

**Dosya:** `appsettings.json`

```json
{
  "ApiBaseUrl": "https://ziraai-api-sit.up.railway.app",
  "WebAppUrl": "https://ziraai-staging.up.railway.app",

  "Iyzico": {
    "ApiKey": "your-api-key",
    "SecretKey": "your-secret-key",
    "BaseUrl": "https://sandbox-api.iyzipay.com"
  }
}
```

**Dosya:** `appsettings.Production.json`

```json
{
  "ApiBaseUrl": "https://api.ziraai.com",
  "WebAppUrl": "https://app.ziraai.com",

  "Iyzico": {
    "ApiKey": "production-api-key",
    "SecretKey": "production-secret-key",
    "BaseUrl": "https://api.iyzipay.com"
  }
}
```

---

## Solution 2: User-Agent Detection (Alternative - Not Recommended)

Eğer frontend'den platform göndermek istemiyorsanız, User-Agent header'ından platform tespit edebilirsiniz. Ancak bu yöntem daha az güvenilirdir.

```csharp
private string DetectPlatform(HttpRequest request)
{
    var userAgent = request.Headers["User-Agent"].ToString().ToLower();

    if (userAgent.Contains("android"))
        return "Android";
    if (userAgent.Contains("iphone") || userAgent.Contains("ipad") || userAgent.Contains("ios"))
        return "iOS";

    // Check for common web browsers
    if (userAgent.Contains("chrome") || userAgent.Contains("firefox") ||
        userAgent.Contains("safari") || userAgent.Contains("edge"))
        return "Web";

    return "iOS"; // Default
}
```

**Not:** Bu yöntem önerilmez çünkü:
- iyzico callback'i kendi User-Agent'ını kullanır
- User-Agent spoofing mümkündür
- Platform bilgisi transaction'da saklanamaz

---

## Frontend Changes Required

### PaymentModal.jsx

```javascript
const handlePayment = async () => {
  setLoading(true);
  setError(null);

  try {
    // Initialize payment with platform info
    const initResponse = await paymentService.initializePayment({
      flowType,
      flowData,
      platform: 'Web'  // NEW: Add platform field
    });

    // ... rest of the code
  } catch (err) {
    // ... error handling
  }
};
```

---

## Testing Checklist

### 1. Staging Environment Testing

**iOS/Android Mobile:**
- [ ] Payment initialize ediliyor
- [ ] iyzico sandbox sayfası açılıyor
- [ ] Payment tamamlanıyor
- [ ] Deep link `ziraai://payment-callback?token=xxx&status=success` açılıyor
- [ ] App callback page'e yönleniyor
- [ ] Purchase database'de görünüyor

**Web:**
- [ ] Payment initialize ediliyor
- [ ] iyzico sandbox sayfası yeni tab'da açılıyor
- [ ] Payment tamamlanıyor
- [ ] Web URL `https://ziraai-staging.up.railway.app/sponsor/payment-callback?token=xxx&status=success` açılıyor
- [ ] Callback page success mesajı gösteriyor
- [ ] Purchase database'de görünüyor
- [ ] 3 saniye sonra `/sponsor/purchases` sayfasına yönleniyor

### 2. Error Scenarios

**Payment Failed:**
- [ ] Web: `https://ziraai-staging.up.railway.app/sponsor/payment-callback?token=xxx&status=failed&error=...`
- [ ] Mobile: `ziraai://payment-callback?token=xxx&status=failed&error=...`

**Transaction Not Found:**
- [ ] Proper error message gösteriliyor
- [ ] User friendly redirect yapılıyor

**iyzico Verification Failed:**
- [ ] Error mesajı loglanıyor
- [ ] User'a anlamlı hata gösteriliyor

### 3. Production Testing

- [ ] Production environment variables doğru set edilmiş
- [ ] iyzico production credentials kullanılıyor
- [ ] Web URL production domain'i kullanıyor: `https://app.ziraai.com`
- [ ] SSL certificate geçerli
- [ ] Real payment test edilmiş

---

## Security Considerations

### 1. Platform Validation
```csharp
// Validate platform before processing
if (!new[] { "iOS", "Android", "Web" }.Contains(request.Platform))
{
    return new ErrorDataResult<PaymentInitializeResponseDto>("Invalid platform");
}
```

### 2. Callback URL Validation
```csharp
// Never trust callback URL from frontend
// Always generate server-side based on platform
callbackUrl = GetCallbackUrl(request.Platform);
```

### 3. Token Validation
```csharp
// Verify token exists and belongs to user
var transaction = await _paymentTransactionRepository.GetAsync(
    t => t.IyzicoToken == token && t.UserId == userId
);
```

### 4. HTTPS Enforcement
- All web callback URLs must use HTTPS
- Never expose payment tokens in logs (use masked logging)

---

## Database Migration Steps

1. **Create Migration:**
```bash
dotnet ef migrations add AddPlatformToPaymentTransaction
```

2. **Review Migration:**
```bash
dotnet ef migrations script
```

3. **Apply to Staging:**
```bash
dotnet ef database update --environment Staging
```

4. **Verify Data:**
```sql
-- Check existing transactions have default platform
SELECT Id, Platform, CreatedAt
FROM PaymentTransactions
WHERE Platform IS NULL OR Platform = '';

-- Update if needed
UPDATE PaymentTransactions
SET Platform = 'iOS'
WHERE Platform IS NULL OR Platform = '';
```

5. **Apply to Production:**
```bash
dotnet ef database update --environment Production
```

---

## API Documentation Updates

### POST /api/v1/payments/initialize

**Request Body:**
```json
{
  "flowType": "SponsorBulkPurchase",
  "flowData": {
    "subscriptionTierId": 1,
    "tierName": "Small Package",
    "quantity": 50,
    "unitPrice": 99.99,
    "totalAmount": 4999.50,
    "currency": "TRY",
    "invoiceRequired": false
  },
  "platform": "Web"
}
```

**Response:**
```json
{
  "data": {
    "transactionId": 23,
    "paymentPageUrl": "https://sandbox-cpp.iyzipay.com/payment?token=xxx",
    "paymentToken": "94736064-7881-4cbc-8772-b90ad5ffb0bb",
    "callbackUrl": "https://ziraai-api-sit.up.railway.app/api/v1/payments/callback",
    "amount": 4999.50,
    "currency": "TRY"
  },
  "success": true,
  "message": "Payment initialized successfully"
}
```

---

## Rollback Plan

Eğer bir sorun olursa:

1. **Migration Rollback:**
```bash
dotnet ef database update PreviousMigration
```

2. **Code Rollback:**
```bash
git revert <commit-hash>
```

3. **Emergency Fix:**
- Platform field'ı optional yap
- Default olarak "iOS" kullan
- Tüm callback'leri mobile deep link'e yönlendir

---

## Timeline & Dependencies

### Phase 1: Backend Changes (2-3 hours)
1. Add Platform field to PaymentTransaction entity
2. Create database migration
3. Update PaymentInitializeRequestDto
4. Modify PaymentService.InitializePaymentAsync
5. Update PaymentController.PaymentCallback

### Phase 2: Testing (1-2 hours)
1. Unit tests for platform detection
2. Integration tests for callback redirect
3. Manual testing on staging

### Phase 3: Deployment
1. Deploy to staging
2. Test with real iyzico sandbox
3. Verify all platforms work correctly
4. Deploy to production

---

## Questions to Backend Developer

1. **Configuration:** `WebAppUrl` hangi environment variable'dan gelecek?
2. **Migration:** Database migration'ı siz mi yapacaksınız yoksa otomatik mi?
3. **Testing:** Staging'de test için test kartı bilgileri lazım mı?
4. **Monitoring:** Payment callback'lerinde hangi metrikler loglanmalı?
5. **Error Handling:** İyzico timeout durumunda ne yapmalıyız?

---

## Contact

Frontend implementation tamamlandı ve test edilmeye hazır. Backend değişiklikleri yapıldıktan sonra end-to-end test yapabiliriz.

**Frontend Developer:** [Your name]
**Date:** 2025-01-22
**Status:** Waiting for backend implementation
