# SPONSORSHIP LINK DISTRIBUTION SYSTEM - Implementation Plan

## 📋 PROJECT OVERVIEW

### 🎯 Amaç
Sponsorların satın aldıkları abonelik paketlerini SMS/WhatsApp ile çiftçilere link olarak gönderebilmesi ve çiftçilerin bu linke tıklayarak otomatik abonelik aktivasyonu yapabilmesi.

### 📊 Mevcut Durum
- Sponsorlar kod satın alıyor
- Çiftçiler manuel olarak `/api/sponsorship/redeem` endpoint'ini kullanıyor
- Kod paylaşımı manuel (telefon, email vb.)
- Çiftçi kodu manuel girmek zorunda

### 🎯 Hedef Durum
- Sponsorlar tek tıkla SMS/WhatsApp gönderebilecek
- Çiftçiler link'e tıklayarak otomatik aktivasyon yapabilecek
- Otomatik hesap oluşturma
- Tracking ve analytics

## 🏗️ TECHNICAL ARCHITECTURE

### 1. Link-Based Redemption System

**Current Flow:**
```
POST /api/sponsorship/redeem (requires auth + manual code entry)
```

**New Flow:**
```
GET /redeem/{sponsorshipCode} (public, no auth required)
```

**Implementation Flow:**
1. Public link accessed
2. Code validation
3. Auto account creation (if needed)
4. Subscription activation
5. Redirect to dashboard

### 2. Message Distribution System

**SMS Flow:**
```
Sponsor → SMS Service → Farmer Phone
```

**WhatsApp Flow:**
```
Sponsor → WhatsApp API → Farmer WhatsApp
```

**Message Template:**
```
🎁 {SponsorCompanyName} size {SubscriptionTierName} abonelik paketi hediye etti!

📱 Aktivasyon linki: https://api.ziraai.com/redeem/{CODE}
⏰ Son kullanım: {ExpiryDate}
🌱 ZiraAI ile tarımınızı dijitalleştirin!
```

## 📁 FILE STRUCTURE & IMPLEMENTATION

### Phase 1: Core Link Redemption (Priority: HIGH)

#### New Files:
1. **`WebAPI/Controllers/RedemptionController.cs`**
   - Public redemption endpoint
   - Auto account creation logic
   - Subscription activation

2. **`Business/Handlers/Sponsorship/Commands/RedeemViaLinkCommand.cs`**
   - CQRS command for link-based redemption
   - Validation logic

3. **`Business/Services/Redemption/IRedemptionService.cs`**
   - Interface for redemption operations
   - Account creation methods

4. **`Business/Services/Redemption/RedemptionService.cs`**
   - Core redemption business logic
   - User creation and subscription management

#### Enhanced Files:
1. **`Entities/Concrete/SponsorshipCode.cs`**
   - Add link tracking fields:
     - `RedemptionLink` (VARCHAR 500)
     - `LinkClickDate` (TIMESTAMP)
     - `LinkClickCount` (INTEGER)

2. **`Business/Services/Sponsorship/SponsorshipService.cs`**
   - Add link generation methods
   - Enhanced tracking capabilities

3. **`DataAccess/Abstract/ISponsorshipCodeRepository.cs`**
   - Add link-specific queries
   - Click tracking methods

#### Database Migration:
```sql
-- Add new columns to SponsorshipCodes table
ALTER TABLE "SponsorshipCodes" ADD COLUMN "RedemptionLink" VARCHAR(500);
ALTER TABLE "SponsorshipCodes" ADD COLUMN "LinkClickDate" TIMESTAMP;
ALTER TABLE "SponsorshipCodes" ADD COLUMN "LinkClickCount" INTEGER DEFAULT 0;
ALTER TABLE "SponsorshipCodes" ADD COLUMN "RecipientPhone" VARCHAR(20);
ALTER TABLE "SponsorshipCodes" ADD COLUMN "RecipientName" VARCHAR(100);
```

### Phase 2: SMS/WhatsApp Integration (Priority: MEDIUM)

#### New Files:
1. **`Business/Services/Notification/INotificationService.cs`**
   - Interface for notification operations
   - Multi-channel support

2. **`Business/Services/Notification/SmsNotificationService.cs`**
   - SMS-specific implementation
   - Message templating

3. **`Business/Services/Notification/WhatsAppNotificationService.cs`**
   - WhatsApp API integration
   - Rich message support

4. **`Business/Handlers/Sponsorship/Commands/SendSponsorshipLinkCommand.cs`**
   - CQRS command for sending links
   - Bulk sending support

5. **`Entities/DTOs/SendLinkRequestDto.cs`**
   - Request model for link sending
   - Recipient information

#### Enhanced Files:
1. **`WebAPI/Controllers/SponsorshipController.cs`**
   - Add `POST /api/sponsorship/send-link` endpoint
   - Bulk sending capabilities

2. **`Business/Adapters/SmsService/ISmsService.cs`**
   - Enhance if needed for templating

### Phase 3: Sponsor Dashboard Enhancement (Priority: LOW)

#### New Files:
1. **`Business/Handlers/Sponsorship/Queries/GetLinkStatisticsQuery.cs`**
   - Analytics and reporting
   - Click tracking statistics

2. **`Entities/DTOs/LinkStatisticsDto.cs`**
   - Statistics data transfer object
   - Performance metrics

3. **`Business/Services/Analytics/ILinkAnalyticsService.cs`**
   - Analytics service interface
   - Reporting capabilities

## 🔧 DETAILED IMPLEMENTATION STEPS

### Step 1: Public Redemption Controller

```csharp
[Route("redeem")]
[AllowAnonymous]
public class RedemptionController : ControllerBase
{
    private readonly IRedemptionService _redemptionService;
    private readonly ILogger<RedemptionController> _logger;

    [HttpGet("{code}")]
    public async Task<IActionResult> RedeemSponsorshipCode(string code)
    {
        try
        {
            // 1. Track link click
            await _redemptionService.TrackLinkClickAsync(code, HttpContext.Connection.RemoteIpAddress?.ToString());
            
            // 2. Validate code
            var validationResult = await _redemptionService.ValidateCodeAsync(code);
            if (!validationResult.Success)
            {
                return RedirectToAction("Error", new { message = validationResult.Message });
            }

            // 3. Check if user exists (via phone number in code)
            var existingUser = await _redemptionService.FindUserByCodeAsync(code);
            
            // 4. Create account if needed
            if (existingUser == null)
            {
                var accountResult = await _redemptionService.CreateAccountFromCodeAsync(code);
                if (!accountResult.Success)
                {
                    return RedirectToAction("Error", new { message = accountResult.Message });
                }
                existingUser = accountResult.Data;
            }

            // 5. Activate subscription
            var subscriptionResult = await _redemptionService.ActivateSubscriptionAsync(code, existingUser.UserId);
            if (!subscriptionResult.Success)
            {
                return RedirectToAction("Error", new { message = subscriptionResult.Message });
            }

            // 6. Auto-login user (generate JWT token)
            var loginToken = await _redemptionService.GenerateAutoLoginTokenAsync(existingUser.UserId);

            // 7. Redirect to success page with token
            return RedirectToAction("Success", new { token = loginToken });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redeeming sponsorship code: {Code}", code);
            return RedirectToAction("Error", new { message = "Beklenmeyen bir hata oluştu." });
        }
    }

    [HttpGet("success")]
    public IActionResult Success(string token)
    {
        // Return success page with auto-login capability
        return View(new { Token = token });
    }

    [HttpGet("error")]
    public IActionResult Error(string message)
    {
        return View(new { ErrorMessage = message });
    }
}
```

### Step 2: Auto Account Creation Logic

```csharp
public class RedemptionService : IRedemptionService
{
    private async Task<IDataResult<User>> CreateAccountFromCodeAsync(string code)
    {
        try
        {
            // Get code details including recipient info
            var sponsorshipCode = await _codeRepository.GetByCodeAsync(code);
            if (sponsorshipCode == null)
                return new ErrorDataResult<User>("Geçersiz kod");

            // Extract phone from code or use stored recipient phone
            var phone = sponsorshipCode.RecipientPhone;
            var name = sponsorshipCode.RecipientName ?? "Çiftçi";

            // Check if user already exists
            var existingUser = await _userRepository.GetByPhoneAsync(phone);
            if (existingUser != null)
                return new SuccessDataResult<User>(existingUser);

            // Generate unique email and password
            var email = $"{phone.Replace("+", "").Replace(" ", "")}@temp.ziraai.com";
            var tempPassword = GenerateSecurePassword();

            // Create password hash
            HashingHelper.CreatePasswordHash(tempPassword, out var passwordSalt, out var passwordHash);

            // Create new farmer account
            var newUser = new User
            {
                FullName = name,
                PhoneNumber = phone,
                Email = email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Status = true,
                CreatedDate = DateTime.Now
            };

            _userRepository.Add(newUser);
            await _userRepository.SaveChangesAsync();

            // Assign Farmer role
            await _roleService.AssignRoleAsync(newUser.UserId, "Farmer");

            // Log account creation
            _logger.LogInformation("Auto-created account for phone {Phone} via sponsorship code {Code}", 
                phone, code);

            return new SuccessDataResult<User>(newUser, "Hesap otomatik oluşturuldu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account from code {Code}", code);
            return new ErrorDataResult<User>("Hesap oluşturulurken hata oluştu");
        }
    }

    private string GenerateSecurePassword()
    {
        // Generate 12-character secure password
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
```

### Step 3: SMS/WhatsApp Service Implementation

```csharp
public interface INotificationService
{
    Task<bool> SendSponsorshipLinkAsync(
        string phone, 
        string recipientName,
        string sponsorName,
        string tierName,
        string redemptionLink,
        DateTime expiryDate,
        NotificationChannel channel);
        
    Task<IDataResult<List<SendingResult>>> SendBulkSponsorshipLinksAsync(
        List<SponsorshipLinkRequest> requests);
}

public class NotificationService : INotificationService
{
    private readonly ISmsService _smsService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<NotificationService> _logger;

    public async Task<bool> SendSponsorshipLinkAsync(
        string phone, 
        string recipientName,
        string sponsorName,
        string tierName,
        string redemptionLink,
        DateTime expiryDate,
        NotificationChannel channel)
    {
        try
        {
            var message = BuildSponsorshipMessage(recipientName, sponsorName, tierName, redemptionLink, expiryDate);
            
            var result = channel switch
            {
                NotificationChannel.SMS => await _smsService.SendAsync(phone, message),
                NotificationChannel.WhatsApp => await _whatsAppService.SendAsync(phone, message),
                _ => false
            };

            // Log sending attempt
            _logger.LogInformation("Sent sponsorship link via {Channel} to {Phone}: {Success}", 
                channel, phone, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending sponsorship link to {Phone} via {Channel}", phone, channel);
            return false;
        }
    }

    private string BuildSponsorshipMessage(
        string recipientName, 
        string sponsorName, 
        string tierName, 
        string redemptionLink, 
        DateTime expiryDate)
    {
        return $@"🎁 Merhaba {recipientName}!

{sponsorName} size {tierName} abonelik paketi hediye etti!

📱 Hemen aktivasyon yapın: {redemptionLink}

⏰ Son kullanım tarihi: {expiryDate:dd.MM.yyyy}
🌱 ZiraAI ile tarımınızı dijitalleştirin!

Bu mesaj ZiraAI tarafından gönderilmiştir.";
    }
}
```

### Step 4: Enhanced Sponsor Controller

```csharp
[HttpPost("send-link")]
[Authorize(Roles = "Sponsor,Admin")]
public async Task<IActionResult> SendSponsorshipLink([FromBody] SendSponsorshipLinkCommand command)
{
    try
    {
        // Set sponsor ID from current user
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();
            
        command.SponsorId = userId.Value;
        
        var result = await Mediator.Send(command);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending sponsorship links for sponsor {SponsorId}", userId);
        return StatusCode(500, new { error = "Bağlantılar gönderilirken hata oluştu." });
    }
}

// SendSponsorshipLinkCommand.cs
public class SendSponsorshipLinkCommand : IRequest<IDataResult<List<SendingResult>>>
{
    public int SponsorId { get; set; }
    public List<string> Codes { get; set; } = new();
    public List<RecipientInfo> Recipients { get; set; } = new();
    public NotificationChannel Channel { get; set; } = NotificationChannel.SMS;
    public string CustomMessage { get; set; } // Optional custom message
}

public class RecipientInfo
{
    public string Phone { get; set; }
    public string Name { get; set; }
    public string Code { get; set; } // Which code to send to this recipient
}

public enum NotificationChannel
{
    SMS = 1,
    WhatsApp = 2
}
```

## 🔒 SECURITY CONSIDERATIONS

### Link Security
1. **Expiration**: Links expire with sponsorship code
2. **One-time Use**: Code becomes invalid after redemption
3. **Rate Limiting**: Prevent spam clicks (5 attempts/minute/IP)
4. **HTTPS Only**: All links must be HTTPS
5. **IP Tracking**: Log and monitor suspicious activity

### Phone Security
1. **Format Validation**: Turkey phone format (+90XXXXXXXXXX)
2. **Duplicate Prevention**: Same phone can't receive same code twice
3. **Spam Protection**: Max 10 codes per phone per day
4. **Blacklist Support**: Block known spam numbers

### Data Privacy
1. **KVKK Compliance**: Phone number handling according to Turkish law
2. **Opt-out Mechanism**: Unsubscribe link in messages
3. **Data Retention**: Auto-delete phone numbers after code expiry
4. **Encryption**: Sensitive data encrypted at rest

## 📱 USER EXPERIENCE FLOWS

### Sponsor Flow
```
1. Login to Sponsor Dashboard
2. Navigate to "Kod Dağıtımı" section
3. Select unused codes from list
4. Add recipient details (name, phone)
5. Choose channel (SMS/WhatsApp)
6. Preview message template
7. Send bulk distribution
8. Monitor delivery status & click rates
```

### Farmer Flow
```
1. Receive SMS/WhatsApp message
2. Click redemption link
3. [Auto] Account creation if needed
4. [Auto] Subscription activation
5. Redirect to welcome dashboard
6. Start using plant analysis immediately
```

## 📊 SUCCESS METRICS & TRACKING

### Technical Metrics
- **Link Click Rate**: Percentage of sent links that were clicked
- **Successful Redemption Rate**: Clicks that resulted in subscription activation
- **Account Creation Rate**: New accounts vs existing user activations
- **Average Time**: From click to activation
- **Channel Performance**: SMS vs WhatsApp effectiveness

### Business Metrics
- **Sponsor Adoption Rate**: Percentage of sponsors using link feature
- **Farmer Satisfaction**: Activation process experience
- **Support Ticket Reduction**: Decrease in manual activation requests
- **Channel Preference**: SMS vs WhatsApp usage patterns
- **Code Distribution Efficiency**: Time saved vs manual methods

### Tracking Implementation
```sql
-- Link click tracking
CREATE TABLE "LinkClickLogs" (
    "Id" SERIAL PRIMARY KEY,
    "SponsorshipCodeId" INTEGER REFERENCES "SponsorshipCodes"("Id"),
    "ClickDate" TIMESTAMP NOT NULL,
    "IpAddress" VARCHAR(45),
    "UserAgent" TEXT,
    "Success" BOOLEAN DEFAULT FALSE,
    "ErrorMessage" TEXT
);

-- Message sending logs
CREATE TABLE "MessageSendingLogs" (
    "Id" SERIAL PRIMARY KEY,
    "SponsorshipCodeId" INTEGER REFERENCES "SponsorshipCodes"("Id"),
    "Channel" VARCHAR(20) NOT NULL,
    "Phone" VARCHAR(20) NOT NULL,
    "SentDate" TIMESTAMP NOT NULL,
    "Success" BOOLEAN DEFAULT FALSE,
    "ErrorMessage" TEXT,
    "MessageContent" TEXT
);
```

## 🎯 IMPLEMENTATION TIMELINE

### Week 1-2: Core Development
- [x] Public redemption controller development
- [x] Auto account creation logic
- [x] Link generation system
- [x] Database migrations
- [x] Basic security implementations

### Week 3: SMS Integration
- [ ] Enhance existing SMS service
- [ ] Message templating system
- [ ] Send-link endpoint development
- [ ] Bulk sending capabilities
- [ ] Error handling and logging

### Week 4: WhatsApp Integration
- [ ] WhatsApp API integration research
- [ ] WhatsApp service implementation
- [ ] Multi-channel support
- [ ] Message template optimization
- [ ] Testing & validation

### Week 5: Analytics & Polish
- [ ] Click tracking implementation
- [ ] Statistics dashboard
- [ ] Performance optimization
- [ ] Security audit
- [ ] Documentation completion

## 🧪 TESTING STRATEGY

### Unit Tests
```csharp
// RedemptionService tests
[Test]
public async Task RedeemViaLink_ValidCode_ShouldCreateAccount()
[Test]
public async Task RedeemViaLink_ExistingUser_ShouldActivateSubscription()
[Test]
public async Task RedeemViaLink_ExpiredCode_ShouldReturnError()

// NotificationService tests
[Test]
public async Task SendSponsorshipLink_ValidPhone_ShouldReturnTrue()
[Test]
public async Task BuildMessage_ValidInputs_ShouldFormatCorrectly()
```

### Integration Tests
```csharp
// End-to-end redemption flow
[Test]
public async Task CompleteRedemptionFlow_NewUser_ShouldCreateAccountAndActivateSubscription()

// SMS/WhatsApp delivery
[Test]
public async Task SendBulkLinks_ValidRecipients_ShouldDeliverAllMessages()

// Database transaction integrity
[Test]
public async Task RedemptionWithFailure_ShouldRollbackChanges()
```

### User Acceptance Tests
1. **Sponsor Dashboard Usability**
   - Link sending interface
   - Bulk recipient management
   - Delivery status monitoring

2. **Farmer Activation Experience**
   - Mobile link compatibility
   - Auto-login functionality
   - Error handling user-friendliness

3. **Cross-platform Testing**
   - iOS Safari link handling
   - Android Chrome behavior
   - WhatsApp in-app browser

## 📝 DOCUMENTATION REQUIREMENTS

### 1. API Documentation
- Swagger documentation for new endpoints
- Request/response examples
- Error code definitions
- Rate limiting information

### 2. User Manual
- Sponsor guide for link distribution
- Step-by-step tutorials
- Best practices for message content
- Troubleshooting guide

### 3. Technical Guide
- Developer documentation for future maintenance
- Database schema documentation
- Service configuration guide
- Deployment instructions

### 4. Security Audit
- Security review of public endpoints
- Penetration testing results
- Compliance checklist (KVKV)
- Vulnerability assessment

## 🚀 DEPLOYMENT CONSIDERATIONS

### Environment Setup
```json
// appsettings.json additions
{
  "RedemptionSettings": {
    "BaseUrl": "https://api.ziraai.com",
    "AutoLoginTokenExpiryMinutes": 60,
    "MaxClicksPerIP": 5,
    "RateLimitWindowMinutes": 1
  },
  "NotificationSettings": {
    "SMS": {
      "Provider": "TurkTelekom",
      "MaxDailyPerPhone": 10
    },
    "WhatsApp": {
      "ApiKey": "your-whatsapp-api-key",
      "BaseUrl": "https://api.whatsapp.com"
    }
  }
}
```

### Production Checklist
- [ ] HTTPS certificate configuration
- [ ] Rate limiting setup (Redis/MemoryCache)
- [ ] Error logging and monitoring
- [ ] Database backup strategy
- [ ] Load balancer configuration
- [ ] CDN setup for static redemption pages

---

## 📞 SUPPORT & MAINTENANCE

### Monitoring Alerts
- High failure rate in link redemption
- SMS/WhatsApp delivery failures
- Unusual click patterns (potential spam)
- Database performance issues

### Maintenance Tasks
- Monthly cleanup of expired codes
- Performance optimization reviews
- Security patch applications
- Analytics report generation

---

**Bu dokümantasyon tam implementation için hazır. Hangi phase'den başlamak istediğinizi belirtin ve kodlamaya başlayalım!**

**Oluşturulma Tarihi**: 13 Ağustos 2025  
**Son Güncelleme**: 13 Ağustos 2025  
**Versiyon**: 1.0  
**Statü**: Ready for Implementation