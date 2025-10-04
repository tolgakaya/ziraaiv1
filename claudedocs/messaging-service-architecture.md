# Messaging Service Architecture - SMS & WhatsApp

**Document Version**: 1.0
**Created**: 2025-10-02
**Purpose**: Unified, extensible messaging infrastructure for SMS and WhatsApp

---

## Current State Analysis

### Existing Implementation Issues

1. **Two Conflicting ISmsService Interfaces**:
   - `Business/Adapters/SmsService/ISmsService.cs` (Old, basic)
   - `Business/Services/Messaging/ISmsService.cs` (Modern, feature-rich) ✅

2. **Three Different Implementations**:
   - `SmsServiceManager`: Minimal, just sleeps
   - `MockSmsService`: OTP-focused, uses old interface
   - `TurkcellSmsService`: Production-ready, uses modern interface ✅

3. **Inconsistency**:
   - Authentication system uses old interface
   - Referral system needs both SMS and WhatsApp
   - No unified provider selection mechanism

### Goals

✅ **Unify on modern interface** (`Business/Services/Messaging/ISmsService.cs`)
✅ **Add WhatsApp support** with similar interface
✅ **Provider abstraction** for easy swapping (Mock, Twilio, Netgsm, Turkcell)
✅ **Configuration-driven** selection
✅ **Template system** for reusable messages
✅ **Backwards compatible** migration path

---

## New Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────┐
│                Application Layer                         │
│  (Authentication, Referral, Notifications, etc.)        │
└──────────────────────┬──────────────────────────────────┘
                       │
                       │ Uses
                       ▼
┌─────────────────────────────────────────────────────────┐
│            Messaging Service Facades                     │
├─────────────────────────────────────────────────────────┤
│  IMessagingService (Unified facade)                     │
│  - SendSms()                                            │
│  - SendWhatsApp()                                       │
│  - SendBulk()                                           │
│  - GetTemplate()                                        │
└──────────────────────┬──────────────────────────────────┘
                       │
       ┌───────────────┴───────────────┐
       │                               │
       ▼                               ▼
┌──────────────────┐          ┌──────────────────┐
│  ISmsService     │          │ IWhatsAppService │
└──────┬───────────┘          └──────┬───────────┘
       │                              │
       │ Provider Selection           │ Provider Selection
       │ (Configuration)              │ (Configuration)
       │                              │
       ├─────┬──────┬─────┬───────   ├─────┬──────┬─────
       ▼     ▼      ▼     ▼           ▼     ▼      ▼
   ┌────┐ ┌────┐ ┌────┐ ┌────┐   ┌────┐ ┌────┐ ┌────┐
   │Mock│ │Twi-│ │Net-│ │Trkc│   │Mock│ │Twi-│ │Trkc│
   │    │ │lio │ │gsm │ │ell │   │    │ │lio │ │ell │
   └────┘ └────┘ └────┘ └────┘   └────┘ └────┘ └────┘
```

### Provider Selection Flow

```
Application requests SendSms()
    ↓
MessagingServiceFactory checks configuration
    ↓
Provider = "Mock"? → MockSmsService
Provider = "Twilio"? → TwilioSmsService
Provider = "Netgsm"? → NetgsmSmsService
Provider = "Turkcell"? → TurkcellSmsService
    ↓
Selected provider sends message
    ↓
Returns delivery result
```

---

## Interface Definitions

### ISmsService (Modern - Already Exists)

**File**: `Business/Services/Messaging/ISmsService.cs`

```csharp
public interface ISmsService
{
    Task<IResult> SendSmsAsync(string phoneNumber, string message);
    Task<IResult> SendBulkSmsAsync(BulkSmsRequest request);
    Task<IDataResult<SmsDeliveryStatus>> GetDeliveryStatusAsync(string messageId);
    Task<IDataResult<SmsSenderInfo>> GetSenderInfoAsync();
}
```

**Note**: This interface already exists and is well-designed. We'll keep it as-is.

### IWhatsAppService (New)

**File**: `Business/Services/Messaging/IWhatsAppService.cs`

```csharp
public interface IWhatsAppService
{
    Task<IResult> SendMessageAsync(string phoneNumber, string message);
    Task<IResult> SendTemplateMessageAsync(WhatsAppTemplateRequest request);
    Task<IResult> SendBulkMessagesAsync(BulkWhatsAppRequest request);
    Task<IDataResult<WhatsAppDeliveryStatus>> GetDeliveryStatusAsync(string messageId);
    Task<IDataResult<WhatsAppSenderInfo>> GetSenderInfoAsync();
}

public class WhatsAppTemplateRequest
{
    public string PhoneNumber { get; set; }
    public string TemplateName { get; set; }
    public string LanguageCode { get; set; } = "tr";
    public Dictionary<string, string> Parameters { get; set; }
}

public class BulkWhatsAppRequest
{
    public WhatsAppRecipient[] Recipients { get; set; }
    public string Message { get; set; }
    public string TemplateName { get; set; }
    public bool UseTemplate { get; set; } = false;
    public DateTime? ScheduledDate { get; set; }
}

public class WhatsAppRecipient
{
    public string PhoneNumber { get; set; }
    public string Name { get; set; }
    public string PersonalizedMessage { get; set; }
    public Dictionary<string, string> TemplateParameters { get; set; }
}

public class WhatsAppDeliveryStatus
{
    public string MessageId { get; set; }
    public string PhoneNumber { get; set; }
    public string Status { get; set; } // Sent, Delivered, Read, Failed, Pending
    public DateTime SentDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public DateTime? ReadDate { get; set; }
    public string ErrorMessage { get; set; }
    public decimal Cost { get; set; }
    public string Provider { get; set; }
}

public class WhatsAppSenderInfo
{
    public string PhoneNumber { get; set; }
    public string DisplayName { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; }
    public int MonthlyQuota { get; set; }
    public int UsedQuota { get; set; }
    public string Provider { get; set; }
    public bool IsActive { get; set; }
    public List<string> ApprovedTemplates { get; set; }
}
```

### IMessagingService (Unified Facade - New)

**File**: `Business/Services/Messaging/IMessagingService.cs`

```csharp
public interface IMessagingService
{
    // SMS methods
    Task<IResult> SendSmsAsync(string phoneNumber, string message);
    Task<IResult> SendBulkSmsAsync(BulkSmsRequest request);

    // WhatsApp methods
    Task<IResult> SendWhatsAppAsync(string phoneNumber, string message);
    Task<IResult> SendBulkWhatsAppAsync(BulkWhatsAppRequest request);

    // Template methods (works for both SMS and WhatsApp)
    Task<IResult> SendFromTemplateAsync(MessageTemplateRequest request);

    // Delivery status
    Task<IDataResult<MessageDeliveryStatus>> GetDeliveryStatusAsync(
        string messageId,
        MessageChannel channel);

    // Sender info
    Task<IDataResult<MessageSenderInfo>> GetSenderInfoAsync(MessageChannel channel);
}

public enum MessageChannel
{
    SMS,
    WhatsApp
}

public class MessageTemplateRequest
{
    public MessageChannel Channel { get; set; }
    public string TemplateName { get; set; }
    public string[] PhoneNumbers { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
}

public class MessageDeliveryStatus
{
    public string MessageId { get; set; }
    public string PhoneNumber { get; set; }
    public MessageChannel Channel { get; set; }
    public string Status { get; set; }
    public DateTime SentDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string ErrorMessage { get; set; }
    public decimal Cost { get; set; }
    public string Provider { get; set; }
}

public class MessageSenderInfo
{
    public MessageChannel Channel { get; set; }
    public string Identifier { get; set; } // Phone number or Sender ID
    public decimal Balance { get; set; }
    public string Currency { get; set; }
    public int MonthlyQuota { get; set; }
    public int UsedQuota { get; set; }
    public string Provider { get; set; }
    public bool IsActive { get; set; }
}
```

---

## Provider Implementations

### Mock Implementations

#### MockSmsService (Enhanced)

**File**: `Business/Services/Messaging/Fakes/MockSmsService.cs`

```csharp
public class MockSmsService : ISmsService
{
    private readonly ILogger<MockSmsService> _logger;
    private readonly IConfiguration _configuration;
    private readonly bool _useFixedCode;
    private readonly string _fixedCode;
    private readonly bool _logToConsole;
    private readonly Dictionary<string, SmsDeliveryStatus> _deliveryStatusCache;

    public MockSmsService(
        ILogger<MockSmsService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        _useFixedCode = bool.Parse(_configuration["SmsService:MockSettings:UseFixedCode"] ?? "true");
        _fixedCode = _configuration["SmsService:MockSettings:FixedCode"] ?? "123456";
        _logToConsole = bool.Parse(_configuration["SmsService:MockSettings:LogToConsole"] ?? "true");
        _deliveryStatusCache = new Dictionary<string, SmsDeliveryStatus>();
    }

    public async Task<IResult> SendSmsAsync(string phoneNumber, string message)
    {
        await Task.Delay(100); // Simulate network delay

        var messageId = GenerateMessageId();
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        // Extract OTP if present
        var otpCode = ExtractOtpCode(message);
        var displayMessage = _useFixedCode && !string.IsNullOrEmpty(otpCode)
            ? $"Fixed OTP: {_fixedCode} (Original: {otpCode})"
            : message;

        if (_logToConsole)
        {
            Console.WriteLine($"📱 MOCK SMS");
            Console.WriteLine($"   To: {normalizedPhone}");
            Console.WriteLine($"   Message: {displayMessage}");
            Console.WriteLine($"   MessageId: {messageId}");
        }

        _logger.LogInformation(
            "📱 MOCK SMS sent. To={Phone}, MessageId={MessageId}",
            normalizedPhone, messageId);

        // Store delivery status
        _deliveryStatusCache[messageId] = new SmsDeliveryStatus
        {
            MessageId = messageId,
            PhoneNumber = normalizedPhone,
            Status = "Delivered",
            SentDate = DateTime.Now,
            DeliveredDate = DateTime.Now.AddSeconds(2),
            Cost = 0.05m,
            Provider = "Mock"
        };

        return new SuccessResult($"SMS sent successfully. MessageId: {messageId}");
    }

    public async Task<IResult> SendBulkSmsAsync(BulkSmsRequest request)
    {
        _logger.LogInformation("📱 MOCK Bulk SMS to {Count} recipients", request.Recipients.Length);

        var successCount = 0;
        var failedCount = 0;

        foreach (var recipient in request.Recipients)
        {
            try
            {
                var message = !string.IsNullOrEmpty(recipient.PersonalizedMessage)
                    ? recipient.PersonalizedMessage
                    : request.Message.Replace("{name}", recipient.Name ?? "Valued User");

                var result = await SendSmsAsync(recipient.PhoneNumber, message);

                if (result.Success)
                    successCount++;
                else
                    failedCount++;
            }
            catch
            {
                failedCount++;
            }

            await Task.Delay(50); // Simulate processing time
        }

        if (_logToConsole)
        {
            Console.WriteLine($"📱 MOCK Bulk SMS Complete");
            Console.WriteLine($"   Success: {successCount}");
            Console.WriteLine($"   Failed: {failedCount}");
        }

        return new SuccessResult(
            $"Bulk SMS sent. Success: {successCount}, Failed: {failedCount}");
    }

    public async Task<IDataResult<SmsDeliveryStatus>> GetDeliveryStatusAsync(string messageId)
    {
        await Task.Delay(50);

        if (_deliveryStatusCache.TryGetValue(messageId, out var status))
        {
            return new SuccessDataResult<SmsDeliveryStatus>(status);
        }

        return new ErrorDataResult<SmsDeliveryStatus>("Message not found");
    }

    public async Task<IDataResult<SmsSenderInfo>> GetSenderInfoAsync()
    {
        await Task.Delay(50);

        var info = new SmsSenderInfo
        {
            SenderId = "MOCK-ZIRAAI",
            Balance = 999.99m,
            Currency = "TL",
            MonthlyQuota = 100000,
            UsedQuota = 1234,
            Provider = "Mock",
            IsActive = true
        };

        return new SuccessDataResult<SmsSenderInfo>(info);
    }

    private string GenerateMessageId()
    {
        return $"MOCK-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
    }

    private string NormalizePhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone))
            return phone;

        var digitsOnly = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");

        // Turkish format normalization
        if (digitsOnly.StartsWith("90") && digitsOnly.Length == 12)
            return "0" + digitsOnly.Substring(2);

        if (!digitsOnly.StartsWith("0") && digitsOnly.Length == 10)
            return "0" + digitsOnly;

        return digitsOnly;
    }

    private string ExtractOtpCode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        var patterns = new[]
        {
            @"PAROLANIZ\s*:?\s*(\d{4,6})",
            @"CODE\s*:?\s*(\d{4,6})",
            @"OTP\s*:?\s*(\d{4,6})",
            @"KOD\s*:?\s*(\d{4,6})",
            @"\b(\d{4,6})\b"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                text,
                pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
                return match.Groups[1].Value;
        }

        return null;
    }
}
```

#### MockWhatsAppService (New)

**File**: `Business/Services/Messaging/Fakes/MockWhatsAppService.cs`

```csharp
public class MockWhatsAppService : IWhatsAppService
{
    private readonly ILogger<MockWhatsAppService> _logger;
    private readonly IConfiguration _configuration;
    private readonly bool _logToConsole;
    private readonly Dictionary<string, WhatsAppDeliveryStatus> _deliveryStatusCache;

    public MockWhatsAppService(
        ILogger<MockWhatsAppService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        _logToConsole = bool.Parse(_configuration["WhatsAppService:MockSettings:LogToConsole"] ?? "true");
        _deliveryStatusCache = new Dictionary<string, WhatsAppDeliveryStatus>();
    }

    public async Task<IResult> SendMessageAsync(string phoneNumber, string message)
    {
        await Task.Delay(150); // WhatsApp typically slower than SMS

        var messageId = GenerateMessageId();
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);

        if (_logToConsole)
        {
            Console.WriteLine($"💬 MOCK WhatsApp");
            Console.WriteLine($"   To: {normalizedPhone}");
            Console.WriteLine($"   Message: {message}");
            Console.WriteLine($"   MessageId: {messageId}");
        }

        _logger.LogInformation(
            "💬 MOCK WhatsApp sent. To={Phone}, MessageId={MessageId}",
            normalizedPhone, messageId);

        // Store delivery status
        _deliveryStatusCache[messageId] = new WhatsAppDeliveryStatus
        {
            MessageId = messageId,
            PhoneNumber = normalizedPhone,
            Status = "Read",
            SentDate = DateTime.Now,
            DeliveredDate = DateTime.Now.AddSeconds(3),
            ReadDate = DateTime.Now.AddSeconds(5),
            Cost = 0.08m,
            Provider = "Mock"
        };

        return new SuccessResult($"WhatsApp message sent. MessageId: {messageId}");
    }

    public async Task<IResult> SendTemplateMessageAsync(WhatsAppTemplateRequest request)
    {
        await Task.Delay(150);

        var messageId = GenerateMessageId();

        if (_logToConsole)
        {
            Console.WriteLine($"💬 MOCK WhatsApp Template");
            Console.WriteLine($"   To: {request.PhoneNumber}");
            Console.WriteLine($"   Template: {request.TemplateName}");
            Console.WriteLine($"   Parameters: {string.Join(", ", request.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
            Console.WriteLine($"   MessageId: {messageId}");
        }

        _logger.LogInformation(
            "💬 MOCK WhatsApp template sent. To={Phone}, Template={Template}, MessageId={MessageId}",
            request.PhoneNumber, request.TemplateName, messageId);

        return new SuccessResult($"WhatsApp template sent. MessageId: {messageId}");
    }

    public async Task<IResult> SendBulkMessagesAsync(BulkWhatsAppRequest request)
    {
        _logger.LogInformation("💬 MOCK Bulk WhatsApp to {Count} recipients", request.Recipients.Length);

        var successCount = 0;
        var failedCount = 0;

        foreach (var recipient in request.Recipients)
        {
            try
            {
                var message = !string.IsNullOrEmpty(recipient.PersonalizedMessage)
                    ? recipient.PersonalizedMessage
                    : request.Message.Replace("{name}", recipient.Name ?? "Valued User");

                var result = await SendMessageAsync(recipient.PhoneNumber, message);

                if (result.Success)
                    successCount++;
                else
                    failedCount++;
            }
            catch
            {
                failedCount++;
            }

            await Task.Delay(100);
        }

        if (_logToConsole)
        {
            Console.WriteLine($"💬 MOCK Bulk WhatsApp Complete");
            Console.WriteLine($"   Success: {successCount}");
            Console.WriteLine($"   Failed: {failedCount}");
        }

        return new SuccessResult(
            $"Bulk WhatsApp sent. Success: {successCount}, Failed: {failedCount}");
    }

    public async Task<IDataResult<WhatsAppDeliveryStatus>> GetDeliveryStatusAsync(string messageId)
    {
        await Task.Delay(50);

        if (_deliveryStatusCache.TryGetValue(messageId, out var status))
        {
            return new SuccessDataResult<WhatsAppDeliveryStatus>(status);
        }

        return new ErrorDataResult<WhatsAppDeliveryStatus>("Message not found");
    }

    public async Task<IDataResult<WhatsAppSenderInfo>> GetSenderInfoAsync()
    {
        await Task.Delay(50);

        var info = new WhatsAppSenderInfo
        {
            PhoneNumber = "+905321234567",
            DisplayName = "ZiraAI Mock",
            Balance = 999.99m,
            Currency = "TL",
            MonthlyQuota = 50000,
            UsedQuota = 567,
            Provider = "Mock",
            IsActive = true,
            ApprovedTemplates = new List<string>
            {
                "referral_invitation",
                "otp_verification",
                "analysis_complete",
                "subscription_renewal"
            }
        };

        return new SuccessDataResult<WhatsAppSenderInfo>(info);
    }

    private string GenerateMessageId()
    {
        return $"MOCK-WA-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
    }

    private string NormalizePhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone))
            return phone;

        var digitsOnly = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");

        // Turkish format for WhatsApp (international format required)
        if (digitsOnly.StartsWith("90") && digitsOnly.Length == 12)
            return "+" + digitsOnly;

        if (digitsOnly.StartsWith("0") && digitsOnly.Length == 11)
            return "+9" + digitsOnly;

        if (!digitsOnly.StartsWith("0") && digitsOnly.Length == 10)
            return "+90" + digitsOnly;

        return phone.StartsWith("+") ? phone : "+" + digitsOnly;
    }
}
```

---

## Configuration System

### appsettings.Development.json

```json
{
  "MessagingService": {
    "SMS": {
      "Provider": "Mock",
      "DefaultProvider": "Mock"
    },
    "WhatsApp": {
      "Provider": "Mock",
      "DefaultProvider": "Mock"
    }
  },

  "SmsService": {
    "Provider": "Mock",
    "MockSettings": {
      "UseFixedCode": true,
      "FixedCode": "123456",
      "LogToConsole": true
    },
    "TwilioSettings": {
      "AccountSid": "",
      "AuthToken": "",
      "FromNumber": ""
    },
    "NetgsmSettings": {
      "UserCode": "",
      "Password": "",
      "SenderId": "ZIRAAI"
    },
    "TurkcellSettings": {
      "ApiUrl": "https://sms.turkcell.com.tr/api",
      "Username": "",
      "Password": "",
      "SenderId": "ZIRAAI"
    }
  },

  "WhatsAppService": {
    "Provider": "Mock",
    "MockSettings": {
      "LogToConsole": true
    },
    "TwilioSettings": {
      "AccountSid": "",
      "AuthToken": "",
      "FromNumber": "whatsapp:+14155238886"
    },
    "TurkcellSettings": {
      "ApiUrl": "https://whatsapp.turkcell.com.tr/api",
      "Username": "",
      "Password": "",
      "PhoneNumber": "+905321234567"
    }
  }
}
```

### appsettings.Staging.json

```json
{
  "MessagingService": {
    "SMS": {
      "Provider": "Mock"
    },
    "WhatsApp": {
      "Provider": "Mock"
    }
  }
}
```

### appsettings.Production.json

```json
{
  "MessagingService": {
    "SMS": {
      "Provider": "Twilio"
    },
    "WhatsApp": {
      "Provider": "Twilio"
    }
  },

  "SmsService": {
    "Provider": "Twilio",
    "TwilioSettings": {
      "AccountSid": "{{secret}}",
      "AuthToken": "{{secret}}",
      "FromNumber": "+15551234567"
    }
  },

  "WhatsAppService": {
    "Provider": "Twilio",
    "TwilioSettings": {
      "AccountSid": "{{secret}}",
      "AuthToken": "{{secret}}",
      "FromNumber": "whatsapp:+15551234567"
    }
  }
}
```

---

## Provider Factory Pattern

### MessagingServiceFactory

**File**: `Business/Services/Messaging/Factories/MessagingServiceFactory.cs`

```csharp
public interface IMessagingServiceFactory
{
    ISmsService GetSmsService();
    IWhatsAppService GetWhatsAppService();
}

public class MessagingServiceFactory : IMessagingServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MessagingServiceFactory> _logger;

    public MessagingServiceFactory(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<MessagingServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public ISmsService GetSmsService()
    {
        var provider = _configuration["SmsService:Provider"] ?? "Mock";

        _logger.LogDebug("Creating SMS service with provider: {Provider}", provider);

        return provider.ToLower() switch
        {
            "mock" => _serviceProvider.GetService<MockSmsService>(),
            "twilio" => _serviceProvider.GetService<TwilioSmsService>(),
            "netgsm" => _serviceProvider.GetService<NetgsmSmsService>(),
            "turkcell" => _serviceProvider.GetService<TurkcellSmsService>(),
            _ => throw new InvalidOperationException($"Unknown SMS provider: {provider}")
        };
    }

    public IWhatsAppService GetWhatsAppService()
    {
        var provider = _configuration["WhatsAppService:Provider"] ?? "Mock";

        _logger.LogDebug("Creating WhatsApp service with provider: {Provider}", provider);

        return provider.ToLower() switch
        {
            "mock" => _serviceProvider.GetService<MockWhatsAppService>(),
            "twilio" => _serviceProvider.GetService<TwilioWhatsAppService>(),
            "turkcell" => _serviceProvider.GetService<TurkcellWhatsAppService>(),
            _ => throw new InvalidOperationException($"Unknown WhatsApp provider: {provider}")
        };
    }
}
```

---

## Dependency Injection Setup

### Business/DependencyResolvers/AutofacBusinessModule.cs

```csharp
// Register SMS providers
builder.RegisterType<MockSmsService>()
    .As<ISmsService>()
    .Named<ISmsService>("Mock")
    .InstancePerLifetimeScope();

builder.RegisterType<TwilioSmsService>()
    .Named<ISmsService>("Twilio")
    .InstancePerLifetimeScope();

builder.RegisterType<NetgsmSmsService>()
    .Named<ISmsService>("Netgsm")
    .InstancePerLifetimeScope();

builder.RegisterType<TurkcellSmsService>()
    .Named<ISmsService>("Turkcell")
    .InstancePerLifetimeScope();

// Register WhatsApp providers
builder.RegisterType<MockWhatsAppService>()
    .As<IWhatsAppService>()
    .Named<IWhatsAppService>("Mock")
    .InstancePerLifetimeScope();

builder.RegisterType<TwilioWhatsAppService>()
    .Named<IWhatsAppService>("Twilio")
    .InstancePerLifetimeScope();

builder.RegisterType<TurkcellWhatsAppService>()
    .Named<IWhatsAppService>("Turkcell")
    .InstancePerLifetimeScope();

// Register factory
builder.RegisterType<MessagingServiceFactory>()
    .As<IMessagingServiceFactory>()
    .SingleInstance();

// Register facade
builder.RegisterType<MessagingService>()
    .As<IMessagingService>()
    .InstancePerLifetimeScope();
```

---

## Migration Strategy

### Phase 1: Add New Implementations (No Breaking Changes)
1. Create `MockSmsService` (new, modern interface)
2. Create `MockWhatsAppService`
3. Create `IWhatsAppService` interface
4. Create `MessagingServiceFactory`
5. Register in DI alongside existing services

### Phase 2: Update Consumers
1. Migrate authentication system to new `ISmsService` (modern)
2. Update referral system to use `IMessagingService`
3. Update notification system

### Phase 3: Deprecate Old Implementations
1. Mark old `ISmsService` (Adapters) as obsolete
2. Mark `SmsServiceManager` as obsolete
3. Remove after confirming no usage

---

## Template System (Future Enhancement)

### Message Templates

**File**: `Business/Services/Messaging/Templates/MessageTemplates.cs`

```csharp
public static class MessageTemplates
{
    public static class OTP
    {
        public const string SMS_TR = "ZiraAI güvenlik kodunuz: {code}. Bu kodu kimseyle paylaşmayın.";
        public const string SMS_EN = "Your ZiraAI security code: {code}. Do not share this code.";

        public const string WHATSAPP_TR = "🌱 *ZiraAI*\n\nGüvenlik kodunuz: *{code}*\n\n_Bu kodu kimseyle paylaşmayın._";
    }

    public static class Referral
    {
        public const string SMS_TR = "🌱 ZiraAI'ye hoş geldin!\n\n{referrerName} seni davet ediyor. Bitki analizi için uygulamayı indir:\n\n{link}\n\nKod: {code}\nSon kullanma: {expiry}";

        public const string WHATSAPP_TR = "🌱 *ZiraAI - Bitki Analizi*\n\nMerhaba! {referrerName} seni ZiraAI'ye davet etti.\n\nYapay zeka ile bitkilerini ücretsiz analiz et:\n{link}\n\n📱 Referans Kodu: *{code}*\n⏰ Son Kullanma: {expiry}\n\n_Kayıt olurken bu kodu kullan!_";
    }
}
```

---

## Testing Strategy

### Unit Tests

```csharp
[TestClass]
public class MockSmsServiceTests
{
    [TestMethod]
    public async Task SendSmsAsync_ShouldReturnSuccess()
    {
        // Arrange
        var service = CreateMockSmsService();

        // Act
        var result = await service.SendSmsAsync("05321234567", "Test message");

        // Assert
        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public async Task SendSmsAsync_WithOTP_ShouldUseFixedCode()
    {
        // Test OTP code replacement
    }
}
```

### Integration Tests

```csharp
[TestClass]
public class MessagingServiceFactoryTests
{
    [TestMethod]
    public void GetSmsService_WithMockProvider_ReturnsM
Service()
    {
        // Test factory provider selection
    }
}
```

---

## Implementation Checklist

### Step 1: Interfaces & Models
- [ ] Create `IWhatsAppService` interface
- [ ] Create WhatsApp-specific models
- [ ] Create `IMessagingService` facade interface
- [ ] Create `IMessagingServiceFactory` interface

### Step 2: Mock Implementations
- [ ] Create `MockSmsService` (modern interface)
- [ ] Create `MockWhatsAppService`
- [ ] Add comprehensive logging
- [ ] Add delivery status simulation

### Step 3: Real Provider Stubs
- [ ] Create `TwilioSmsService` stub
- [ ] Create `TwilioWhatsAppService` stub
- [ ] Create `NetgsmSmsService` stub
- [ ] Document API integration requirements

### Step 4: Factory & DI
- [ ] Implement `MessagingServiceFactory`
- [ ] Update `AutofacBusinessModule`
- [ ] Test provider selection logic

### Step 5: Configuration
- [ ] Update `appsettings.Development.json`
- [ ] Update `appsettings.Staging.json`
- [ ] Prepare `appsettings.Production.json` template

### Step 6: Migration
- [ ] Update authentication system
- [ ] Update referral system
- [ ] Test backwards compatibility
- [ ] Deprecate old implementations

---

**END OF DOCUMENT**
