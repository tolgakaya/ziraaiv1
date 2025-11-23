# SMS Entegrasyonu Geliştirici Rehberi

## 📋 İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [Mevcut Sorun ve Çözümü](#mevcut-sorun-ve-çözümü)
3. [Mimari Yapı](#mimari-yapı)
4. [Yeni Bir Flow'da SMS Entegrasyonu](#yeni-bir-flowda-sms-entegrasyonu)
5. [NetGSM API Kullanımı](#netgsm-api-kullanımı)
6. [Konfigürasyon](#konfigürasyon)
7. [Test ve Doğrulama](#test-ve-doğrulama)
8. [Hata Ayıklama](#hata-ayıklama)
9. [Best Practices](#best-practices)

---

## Genel Bakış

ZiraAI projesinde SMS gönderimi için **NetGSM** entegrasyonu kullanılmaktadır. Sistem, farklı provider'lar (Mock, NetGSM, Turkcell) arasında geçiş yapabilecek esnek bir yapıda tasarlanmıştır.

### Teknoloji Stack
- **SMS Provider**: NetGSM (Türkiye)
- **Dependency Injection**: Autofac
- **Pattern**: Factory Pattern + Strategy Pattern
- **API**: REST v2 (JSON) ve XML (OTP)

### Kullanım Alanları
- ✅ **Login/Register OTP**: Kullanıcı doğrulama kodları
- ✅ **Sponsorship Code Distribution**: Sponsor'dan farmer'a kod gönderimi
- ✅ **Dealer Invitations**: Bayi davet linkleri
- ✅ **Bulk SMS**: Toplu mesaj gönderimi

---

## Mevcut Sorun ve Çözümü

### ❌ Sorun: Factory Pattern Hatası

**Tarih**: 23 Kasım 2025

**Semptom**: Sponsor'ların farmer'lara kod dağıtırken SMS gönderilmiyordu, ancak login/register OTP mesajları çalışıyordu.

**Root Cause**: `MessagingServiceFactory.GetSmsService()` metodu, `ISmsService` interface'i yerine **concrete class**'ları resolve etmeye çalışıyordu:

```csharp
// ❌ YANLIŞ - Eski Kod (Lines 47-54)
return provider.ToLower() switch
{
    "mock" => (ISmsService)_serviceProvider.GetService(typeof(ISmsService)),
    "netgsm" => (ISmsService)_serviceProvider.GetService(typeof(NetgsmSmsService)), // ❌ Concrete class
    "turkcell" => (ISmsService)_serviceProvider.GetService(typeof(TurkcellSmsService)), // ❌ Concrete class
    _ => throw new InvalidOperationException($"Unknown SMS provider: {provider}")
};
```

**Neden Hatalı?**
- `NetgsmSmsService` ve `TurkcellSmsService` concrete class'ları **Microsoft DI container'da kayıtlı değil**
- Sadece `ISmsService` interface'i **Autofac'te kayıtlı** (AutofacBusinessModule.cs:271-295)
- Factory'nin yaptığı switch-case logic **gereksiz ve hatalı** - Autofac zaten provider seçimini yapıyor

**Neden Login/Register Çalışıyordu?**
- Login/Register flow'ları **doğrudan `ISmsService` inject ediyor**:
  ```csharp
  // ✅ DOĞRU - RegisterWithPhoneCommand.cs:30
  private readonly Business.Services.Messaging.ISmsService _smsService;
  ```
- Autofac, `ISmsService` resolve edildiğinde konfigürasyona göre (`SmsService:Provider`) doğru implementasyonu döndürüyor

### ✅ Çözüm: Factory'yi Basitleştirme

Factory'nin tek görevi, `ISmsService` interface'ini resolve etmek olmalı. Provider seçimi Autofac tarafından yapılıyor:

```csharp
// ✅ DOĞRU - Yeni Kod (Lines 41-57)
public ISmsService GetSmsService()
{
    var provider = _configuration["SmsService:Provider"] ?? "Mock";
    _logger.LogDebug("Creating SMS service with provider: {Provider}", provider);

    // ISmsService is already configured in Autofac to resolve the correct provider
    // based on SmsService:Provider configuration, so we just need to resolve the interface
    var smsService = (ISmsService)_serviceProvider.GetService(typeof(ISmsService));

    if (smsService == null)
    {
        throw new InvalidOperationException($"Failed to resolve ISmsService for provider: {provider}. Check Autofac registration.");
    }

    return smsService;
}
```

**Neden Bu Çözüm Doğru?**
1. ✅ Factory sadece interface resolve ediyor
2. ✅ Autofac konfigürasyona göre doğru implementasyonu seçiyor
3. ✅ Tüm flow'lar (Login, Register, Sponsorship) aynı mekanizmayı kullanıyor
4. ✅ Null kontrolü ile güvenli hata yönetimi

**Verification (Production Logs)**:
```
2025-11-23 12:39:25.110 [DBG] Creating SMS service with provider: Netgsm
2025-11-23 12:39:25.111 [INF] Sending SMS to 905866866386 via NetGSM REST v2
2025-11-23 12:39:25.416 [INF] SMS sent successfully to 905866866386. JobId: 17639015653428457917755337
```

---

## Mimari Yapı

### 1. Interface Hierarchy

```
ISmsService (Business/Services/Messaging/ISmsService.cs)
│
├── NetgsmSmsService (NetGSM implementation)
├── TurkcellSmsService (Turkcell implementation - placeholder)
└── MockSmsService (Test/Development mock)
```

### 2. Dependency Injection Flow

```
appsettings.json
  │
  ├─ SmsService:Provider = "Netgsm"
  │
  ↓
AutofacBusinessModule.cs (Lines 271-295)
  │
  ├─ Reads config: "SmsService:Provider"
  ├─ Switch-case: netgsm → NetgsmSmsService
  │                mock → MockSmsService
  │                turkcell → TurkcellSmsService
  │
  ↓
ISmsService (Registered in Autofac)
  │
  ↓
Usage Options:
  │
  ├─ Option 1: Direct Injection ✅ (Recommended)
  │   └─ private readonly ISmsService _smsService;
  │
  └─ Option 2: Factory ✅ (For multi-service scenarios)
      └─ var smsService = _messagingFactory.GetSmsService();
```

### 3. NetGSM Service Architecture

```
NetgsmSmsService
│
├── SendSmsAsync()         → POST /sms/rest/v2/send (Standard SMS)
├── SendOtpAsync()         → POST /sms/send/otp (Fast OTP delivery)
├── SendBulkSmsAsync()     → POST /sms/rest/v2/send (Bulk messages)
├── GetDeliveryStatusAsync() → POST /sms/rest/v2/report
└── GetSenderInfoAsync()   → POST /balance
```

**API Types**:
- **REST v2 (JSON)**: Standard SMS, Bulk SMS, Reports
  - Authentication: Basic Auth
  - Content-Type: application/json
  - Turkish characters: Supported with `encoding: "TR"`

- **XML**: OTP SMS only
  - Faster delivery (max 3 minutes)
  - Turkish characters: **NOT SUPPORTED**
  - Content-Type: application/xml

---

## Yeni Bir Flow'da SMS Entegrasyonu

### Seçenek 1: Direct Injection (✅ Recommended)

**Ne Zaman Kullanılır**: Tek bir SMS servisi kullanılacaksa

**Adımlar**:

#### 1. Command/Handler'da Inject Et

```csharp
// YourCommand.cs
public class YourCommand : IRequest<IResult>
{
    public string PhoneNumber { get; set; }
    public string Message { get; set; }
}

// YourCommandHandler.cs
public class YourCommandHandler : IRequestHandler<YourCommand, IResult>
{
    // ✅ ISmsService'i direkt inject et
    private readonly Business.Services.Messaging.ISmsService _smsService;
    private readonly ILogger<YourCommandHandler> _logger;

    public YourCommandHandler(
        Business.Services.Messaging.ISmsService smsService,
        ILogger<YourCommandHandler> logger)
    {
        _smsService = smsService;
        _logger = logger;
    }

    public async Task<IResult> Handle(YourCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // ✅ SMS gönder
            var result = await _smsService.SendSmsAsync(
                request.PhoneNumber,
                request.Message
            );

            if (!result.Success)
            {
                _logger.LogError("SMS sending failed: {Message}", result.Message);
                return new ErrorResult("SMS gönderilemedi: " + result.Message);
            }

            _logger.LogInformation("SMS sent successfully to {Phone}", request.PhoneNumber);
            return new SuccessResult("SMS başarıyla gönderildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending SMS");
            return new ErrorResult("SMS gönderimi sırasında hata oluştu");
        }
    }
}
```

#### 2. Controller'da Kullan

```csharp
// YourController.cs
[Route("api/v1/[controller]")]
[ApiController]
public class YourController : BaseApiController
{
    [HttpPost("send-notification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendNotification([FromBody] YourCommand command)
    {
        var result = await Mediator.Send(command);

        if (result.Success)
            return Ok(result);

        return BadRequest(result);
    }
}
```

**Örnek**: `RegisterWithPhoneCommand.cs` (Business/Handlers/Authorizations/Commands/)

---

### Seçenek 2: Factory Pattern (✅ For Multi-Service)

**Ne Zaman Kullanılır**: Aynı flow'da hem SMS hem WhatsApp gibi birden fazla messaging servisi kullanılacaksa

**Adımlar**:

#### 1. Factory'yi Inject Et

```csharp
// YourCommandHandler.cs
public class YourCommandHandler : IRequestHandler<YourCommand, IResult>
{
    private readonly IMessagingServiceFactory _messagingFactory;
    private readonly ILogger<YourCommandHandler> _logger;

    public YourCommandHandler(
        IMessagingServiceFactory messagingFactory,
        ILogger<YourCommandHandler> logger)
    {
        _messagingFactory = messagingFactory;
        _logger = logger;
    }

    public async Task<IResult> Handle(YourCommand request, CancellationToken cancellationToken)
    {
        // ✅ Factory'den SMS service al
        var smsService = _messagingFactory.GetSmsService();

        // ✅ SMS gönder
        var smsResult = await smsService.SendSmsAsync(
            request.PhoneNumber,
            request.Message
        );

        // İsteğe bağlı: WhatsApp da gönder
        if (request.SendViaWhatsApp)
        {
            var whatsappService = _messagingFactory.GetWhatsAppService();
            var whatsappResult = await whatsappService.SendMessageAsync(
                request.PhoneNumber,
                request.Message
            );
        }

        return smsResult;
    }
}
```

**Örnek**: `SendSponsorshipLinkCommand.cs` (Business/Handlers/Sponsorship/Commands/)

---

## NetGSM API Kullanımı

### 1. Standard SMS Gönderimi

```csharp
var result = await _smsService.SendSmsAsync(
    phoneNumber: "905551234567",  // 12 digit format: 90 + area code + number
    message: "Kodunuz: ABC123. Bu kod 24 saat geçerlidir."
);

if (result.Success)
{
    Console.WriteLine("SMS gönderildi!");
}
```

**Features**:
- ✅ Turkish characters supported (`ç, ğ, ı, ö, ş, ü`)
- ✅ Automatic encoding detection
- ✅ Phone number normalization (0555 → 905551234567)
- ✅ Response parsing (JobId extraction)

**API Endpoint**: `POST /sms/rest/v2/send`

**Request Format**:
```json
{
  "msgheader": "ZIRAAI",
  "encoding": "TR",
  "messages": [
    {
      "msg": "Kodunuz: ABC123",
      "no": "905551234567"
    }
  ]
}
```

**Response**:
```json
{
  "code": "00",
  "jobid": "17639015653428457917755337"
}
```

---

### 2. OTP SMS Gönderimi (Fast Delivery)

```csharp
var result = await _smsService.SendOtpAsync(
    phoneNumber: "905551234567",
    otpCode: "123456"
);
```

**Features**:
- ✅ Faster delivery (max 3 minutes)
- ⚠️ Turkish characters **NOT SUPPORTED**
- ✅ Automatic message formatting

**Mesaj Formatı**:
```
Dogrulama kodunuz: 123456. Bu kodu kimseyle paylasmayin.
```

**API Endpoint**: `POST /sms/send/otp`

**Request Format** (XML):
```xml
<?xml version="1.0"?>
<mainbody>
   <header>
       <usercode>YOUR_USERNAME</usercode>
       <password>YOUR_PASSWORD</password>
       <msgheader>ZIRAAI</msgheader>
   </header>
   <body>
       <msg><![CDATA[Dogrulama kodunuz: 123456]]></msg>
       <no>905551234567</no>
   </body>
</mainbody>
```

---

### 3. Bulk SMS Gönderimi

```csharp
var bulkRequest = new BulkSmsRequest
{
    Message = "Merhaba {name}, hoş geldiniz!",
    Recipients = new[]
    {
        new SmsRecipient
        {
            Name = "Ahmet",
            PhoneNumber = "905551234567",
            PersonalizedMessage = null  // Use template
        },
        new SmsRecipient
        {
            Name = "Mehmet",
            PhoneNumber = "905559876543",
            PersonalizedMessage = "Özel mesaj"  // Override template
        }
    }
};

var result = await _smsService.SendBulkSmsAsync(bulkRequest);
```

**Features**:
- ✅ Multiple recipients in single API call
- ✅ Template support with `{name}` placeholder
- ✅ Per-recipient message customization
- ✅ Single JobId for tracking

---

### 4. Delivery Status Kontrolü

```csharp
var statusResult = await _smsService.GetDeliveryStatusAsync(
    messageId: "17639015653428457917755337"
);

if (statusResult.Success)
{
    var status = statusResult.Data;
    Console.WriteLine($"Status: {status.Status}");
    Console.WriteLine($"Sent: {status.SentDate}");
}
```

**API Endpoint**: `POST /sms/rest/v2/report`

**Request**:
```json
{
  "jobids": ["17639015653428457917755337"],
  "pagenumber": 0,
  "pagesize": 10
}
```

---

### 5. Bakiye Sorgulama

```csharp
var infoResult = await _smsService.GetSenderInfoAsync();

if (infoResult.Success)
{
    var info = infoResult.Data;
    Console.WriteLine($"Balance: {info.Balance} {info.Currency}");
    Console.WriteLine($"Sender: {info.SenderId}");
}
```

**API Endpoint**: `POST /balance`

**Request**:
```json
{
  "usercode": "YOUR_USERNAME",
  "password": "YOUR_PASSWORD",
  "stip": 2
}
```

**Response**:
```json
{
  "balance": 1250.50
}
```

---

## Konfigürasyon

### 1. appsettings.json

```json
{
  "SmsService": {
    "Provider": "Netgsm"  // Options: Mock, Netgsm, Turkcell
  },
  "SmsProvider": {
    "Netgsm": {
      "ApiUrl": "https://api.netgsm.com.tr",
      "UserCode": "",  // NetGSM username
      "Password": "",  // NetGSM password
      "MsgHeader": "ZIRAAI"  // Approved sender name
    }
  },
  "SmsLogging": {
    "Enabled": true  // Enable/disable SMS logging to database
  }
}
```

### 2. Environment Variables (Production)

**Railway/Production Ortamında**:

```bash
# SMS Provider Selection
SmsService__Provider=Netgsm

# NetGSM Credentials (Recommended for security)
NETGSM_USERCODE=your_username
NETGSM_PASSWORD=your_password
NETGSM_MSGHEADER=ZIRAAI
NETGSM_API_URL=https://api.netgsm.com.tr
```

**Priority Order**:
1. Environment variables (highest)
2. appsettings.{Environment}.json
3. appsettings.json
4. Default values

### 3. Development: Mock SMS

**appsettings.Development.json**:
```json
{
  "SmsService": {
    "Provider": "Mock"  // ✅ Mock for development
  }
}
```

**MockSmsService Behavior**:
- ✅ Logs SMS details without sending
- ✅ Always returns success
- ✅ No external API calls
- ✅ Fast for testing

---

## Test ve Doğrulama

### 1. Unit Testing

```csharp
[Fact]
public async Task SendSms_ValidPhone_ReturnsSuccess()
{
    // Arrange
    var mockSmsService = new Mock<ISmsService>();
    mockSmsService
        .Setup(x => x.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(new SuccessResult("SMS sent"));

    var handler = new YourCommandHandler(mockSmsService.Object, logger);
    var command = new YourCommand
    {
        PhoneNumber = "905551234567",
        Message = "Test"
    };

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.Success);
    mockSmsService.Verify(x => x.SendSmsAsync("905551234567", "Test"), Times.Once);
}
```

### 2. Integration Testing (Postman)

**Endpoint**: `POST /api/v1/sponsorship/send-link`

**Headers**:
```
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

**Request Body**:
```json
{
  "codeId": 123,
  "phoneNumber": "905551234567",
  "customMessage": "Kodunuz: ABC123"
}
```

**Expected Success Response**:
```json
{
  "success": true,
  "message": "SMS başarıyla gönderildi"
}
```

**Check Logs**:
```bash
grep "Sending SMS to 905551234567" application.log
grep "SMS sent successfully" application.log
```

### 3. Production Verification

**Log Pattern**:
```
[DBG] Creating SMS service with provider: Netgsm
[INF] Sending SMS to 905551234567 via NetGSM REST v2
[INF] SMS sent successfully to 905551234567. JobId: 17639015653428457917755337
```

**Database Check** (SmsLogs table):
```sql
SELECT * FROM "SmsLogs"
WHERE "PhoneNumber" = '905551234567'
ORDER BY "SentDate" DESC
LIMIT 10;
```

---

## Hata Ayıklama

### 1. SMS Gönderilmiyor

**Kontrol Listesi**:
- ✅ `SmsService:Provider` = "Netgsm" mi?
- ✅ NetGSM credentials doğru mu? (UserCode, Password)
- ✅ `NETGSM_USERCODE` ve `NETGSM_PASSWORD` env variables set mi?
- ✅ Sender name (MsgHeader) NetGSM'de onaylı mı?
- ✅ Bakiye yeterli mi? (`GetSenderInfoAsync()` ile kontrol)
- ✅ IP kısıtlaması var mı? (NetGSM error code 30)

**Log Kontrolü**:
```bash
# Factory provider seçimi
grep "Creating SMS service with provider" application.log

# Autofac DI registration
grep "[SMS DI]" application.log

# NetGSM API calls
grep "Sending SMS to" application.log
grep "SMS sent successfully" application.log
grep "SMS sending failed" application.log
```

### 2. NetGSM Error Codes

| Code | Anlamı | Çözüm |
|------|--------|-------|
| `00` | ✅ Başarılı | - |
| `20` | Mesaj metni hatalı | Max karakter kontrolü |
| `30` | Geçersiz credentials | UserCode/Password kontrol et |
| `40` | Sender ID tanımlı değil | NetGSM'de MsgHeader onaylat |
| `50` | Yetersiz bakiye | Bakiye yükle |
| `51` | 24 saat içinde aynı mesaj gönderilmiş | Farklı mesaj kullan |
| `70` | Hatalı parametre | JSON format kontrol |
| `80` | Zaman aşımı | Retry mechanism |
| `85` | Yinelenen gönderim | 24 saat bekle |

**Error Log Example**:
```
[ERR] SMS sending failed to 905551234567. Error: 30 - Geçersiz kullanıcı adı/şifre
```

### 3. Phone Number Format Issues

**NetGSM Expects**: `905551234567` (12 digits)

**Auto-normalization**:
- `05551234567` → `905551234567` ✅
- `5551234567` → `905551234567` ✅
- `+905551234567` → `905551234567` ✅

**Log Pattern**:
```
[WRN] Unusual phone number format: +90 555 123 45 67, using as-is: 905551234567
```

### 4. Turkish Character Issues

**OTP SMS**: Turkish characters **NOT SUPPORTED**
- ❌ `Merhaba, şifreniz: 123456`
- ✅ `Dogrulama kodunuz: 123456`

**Standard SMS**: Turkish characters **SUPPORTED**
- ✅ `Hoş geldiniz! Kodunuz: ABC123`

**Auto-detection**: `ContainsTurkishChars()` method sets `encoding: "TR"`

---

## Best Practices

### 1. SMS Gönderimi

✅ **DO**:
- Use `SendOtpAsync()` for OTP codes (faster)
- Use `SendSmsAsync()` for standard messages
- Log all SMS operations
- Handle errors gracefully
- Check balance periodically
- Normalize phone numbers
- Use message templates

❌ **DON'T**:
- Don't send same message twice in 24h (error code 51)
- Don't use Turkish chars in OTP SMS
- Don't hardcode credentials
- Don't ignore error responses
- Don't send SMS without logging

### 2. Error Handling

```csharp
public async Task<IResult> SendSmsWithRetry(string phone, string message)
{
    const int maxRetries = 3;
    int attempt = 0;

    while (attempt < maxRetries)
    {
        attempt++;
        var result = await _smsService.SendSmsAsync(phone, message);

        if (result.Success)
            return result;

        if (attempt < maxRetries)
        {
            _logger.LogWarning("SMS attempt {Attempt} failed, retrying...", attempt);
            await Task.Delay(TimeSpan.FromSeconds(2 * attempt)); // Exponential backoff
        }
    }

    return new ErrorResult("SMS gönderilemedi, tüm denemeler başarısız");
}
```

### 3. Message Templates

```csharp
public static class SmsTemplates
{
    public static string OtpMessage(string code) =>
        $"Dogrulama kodunuz: {code}. Bu kodu kimseyle paylasmayin.";

    public static string SponsorshipCode(string code, string validityDays) =>
        $"Hoş geldiniz! Sponsorluk kodunuz: {code}. {validityDays} gün geçerlidir.";

    public static string DealerInvitation(string sponsorName, string token, string deepLink) =>
        $"🎁 {sponsorName} Bayilik Daveti!\n\n" +
        $"Davet Kodunuz: DEALER-{token}\n\n" +
        $"Hemen katılmak için tıklayın:\n{deepLink}";
}
```

### 4. Configuration Management

```csharp
// ✅ Good: Read from config with fallback
var provider = _configuration["SmsService:Provider"] ?? "Mock";

// ✅ Good: Environment variable priority
var userCode = Environment.GetEnvironmentVariable("NETGSM_USERCODE")
    ?? _configuration["SmsProvider:Netgsm:UserCode"];

// ❌ Bad: Hardcoded credentials
var userCode = "my_username"; // NEVER DO THIS!
```

### 5. Logging Best Practices

```csharp
// ✅ Good: Structured logging
_logger.LogInformation("SMS sent to {Phone}, JobId: {JobId}", phone, jobId);

// ✅ Good: Error details
_logger.LogError("SMS failed: Code={Code}, Message={Message}", errorCode, errorMessage);

// ❌ Bad: String interpolation
_logger.LogInformation($"SMS sent to {phone}"); // Less efficient
```

---

## Checklist: Yeni SMS Flow Ekleme

### 📝 Implementation Checklist

- [ ] **1. Command/Handler Oluştur**
  - [ ] Command class (properties: PhoneNumber, Message)
  - [ ] Handler class (implement IRequestHandler)
  - [ ] Unit tests

- [ ] **2. SMS Service Inject Et**
  - [ ] Option A: Direct injection (`ISmsService _smsService`)
  - [ ] Option B: Factory injection (`IMessagingServiceFactory _factory`)

- [ ] **3. SMS Gönderme Lojiği**
  - [ ] Call `SendSmsAsync()` or `SendOtpAsync()`
  - [ ] Error handling (try-catch)
  - [ ] Success/failure logging
  - [ ] Return appropriate IResult

- [ ] **4. Controller Endpoint**
  - [ ] HTTP method (POST)
  - [ ] Route definition
  - [ ] Authorization (if needed)
  - [ ] Request/response DTOs
  - [ ] Swagger documentation

- [ ] **5. Testing**
  - [ ] Unit tests (mock ISmsService)
  - [ ] Integration tests (Postman)
  - [ ] Development (Mock provider)
  - [ ] Staging (NetGSM test credentials)
  - [ ] Production verification

- [ ] **6. Documentation**
  - [ ] API endpoint docs
  - [ ] Message templates
  - [ ] Error scenarios
  - [ ] Example requests/responses

- [ ] **7. Configuration**
  - [ ] appsettings.Development.json (Mock)
  - [ ] appsettings.Staging.json (NetGSM test)
  - [ ] Environment variables for production
  - [ ] Message templates in config (optional)

---

## Reference Implementation

### ✅ Working Examples

1. **OTP SMS (Login/Register)**
   - File: `Business/Handlers/Authorizations/Commands/RegisterWithPhoneCommand.cs`
   - Pattern: Direct injection
   - Method: `SendOtpAsync()`

2. **Sponsorship Code Distribution**
   - File: `Business/Handlers/Sponsorship/Commands/SendSponsorshipLinkCommand.cs`
   - Pattern: Factory injection
   - Method: `SendSmsAsync()`

3. **Dealer Invitation**
   - File: `Business/Handlers/DealerInvitation/Commands/SendDealerInvitationCommand.cs`
   - Pattern: Direct injection
   - Method: `SendSmsAsync()`

---

## Summary

### ✅ Key Takeaways

1. **Use Direct Injection** for most scenarios:
   ```csharp
   private readonly ISmsService _smsService;
   ```

2. **Factory Pattern** only for multi-service scenarios:
   ```csharp
   var smsService = _messagingFactory.GetSmsService();
   ```

3. **Autofac handles provider selection** via config:
   ```json
   "SmsService": { "Provider": "Netgsm" }
   ```

4. **NetGSM has two API types**:
   - REST v2 (JSON): Standard SMS, Turkish chars ✅
   - XML: OTP only, Turkish chars ❌

5. **Always log** SMS operations for debugging and audit

6. **Environment variables** for production credentials

7. **Error codes** are important - handle them properly

---

## Support & Resources

### NetGSM Documentation
- API Docs: https://www.netgsm.com.tr/dokuman/
- Support: NetGSM customer service

### Internal Resources
- Factory Implementation: `Business/Services/Messaging/Factories/MessagingServiceFactory.cs`
- NetGSM Service: `Business/Services/Messaging/NetgsmSmsService.cs`
- Autofac Registration: `Business/DependencyResolvers/AutofacBusinessModule.cs` (lines 271-295)
- Configuration: `appsettings.Staging.json`, `appsettings.json`

### Contact
For questions or issues:
- Check logs: `claudedocs/application.log`
- Review NetGSM error codes table
- Verify Autofac registration logs: `[SMS DI]`
- Test with Mock provider first

---

**Last Updated**: 23 Kasım 2025
**Version**: 1.0
**Status**: ✅ Production Ready
