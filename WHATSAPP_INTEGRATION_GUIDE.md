# 📱 WhatsApp Entegrasyon Rehberi - ZiraAI

## 📊 Genel Bakış

Bu dokümantasyon, ZiraAI projesi içerisindeki mevcut WhatsApp entegrasyon durumunu, eksiklikleri ve implementation planını detaylandırmaktadır. Sponsorship sistemi içerisinde WhatsApp üzerinden link dağıtımı için gerekli bilgiler ve adımlar bu dokümanda bulunmaktadır.

**Mevcut Durum**: %70 hazır altyapı, production için kritik eksiklikler mevcut
**Hedef**: Tam fonksiyonel WhatsApp Business API entegrasyonu

---

## 🎯 Mevcut WhatsApp Endpoint'leri

### 1. Ana WhatsApp Endpoint: `/api/v1/sponsorships/send-link`

**Dosya Konumu**: `WebAPI/Controllers/SponsorshipController.cs:335`
**Handler**: `Business/Handlers/Sponsorship/Commands/SendSponsorshipLinkCommand.cs`

#### 📋 Endpoint Detayları
- **Method**: POST
- **Authentication**: Bearer Token (Sponsor, Admin rolleri gerekli)
- **Rate Limiting**: Henüz implementasyonu yok
- **Current Status**: MOCK IMPLEMENTATION

#### 📥 Request Format
```json
{
  "recipients": [
    {
      "code": "AGRI-2025-ABC123",
      "phone": "+905551234567", 
      "name": "Ahmet Çiftçi"
    },
    {
      "code": "AGRI-2025-DEF456",
      "phone": "+905559876543",
      "name": "Fatma Çiftçi"
    }
  ],
  "channel": "WhatsApp",
  "customMessage": "Merhaba {name}, tarım sponsorluğu için linkiniz: {link}"
}
```

#### 📤 Response Format
```json
{
  "success": true,
  "data": {
    "totalSent": 2,
    "successCount": 2,
    "failureCount": 0,
    "results": [
      {
        "code": "AGRI-2025-ABC123",
        "phone": "+905551234567",
        "success": true,
        "errorMessage": null,
        "deliveryStatus": "Mock Delivered"
      },
      {
        "code": "AGRI-2025-DEF456",
        "phone": "+905559876543",
        "success": true,
        "errorMessage": null,
        "deliveryStatus": "Mock Delivered"
      }
    ]
  },
  "message": "📱 MOCK: 2 link başarıyla gönderildi via WhatsApp"
}
```

#### ⚠️ Mevcut Limitasyonlar
```csharp
// SendSponsorshipLinkCommand.cs:60-62
// MOCK IMPLEMENTATION - Skip database validation for now
_logger.LogInformation("📋 MOCK: Skipping database validation for codes");

// Mock successful bulk send result - Gerçek API çağrısı yok
```

### 2. Toplu WhatsApp Endpoint: `/api/v1/bulk-operations/send-links`

**Dosya Konumu**: `WebAPI/Controllers/BulkOperationsController.cs:27`
**Purpose**: Yüksek hacimli WhatsApp mesajlaşması (25+ mesaj)

#### 📋 Endpoint Detayları
- **Method**: POST
- **Authentication**: Bearer Token (Sponsor, Admin rolleri)
- **Queue Support**: Background job processing ile
- **Batch Processing**: Configurable batch sizes

#### 📥 Advanced Request Format
```json
{
  "recipients": [
    {
      "code": "AGRI-2025-001",
      "phone": "+905551234567",
      "name": "Çiftçi 1"
    }
    // ... 100+ recipients
  ],
  "channel": "WhatsApp",
  "customMessage": "Merhaba {name}! Tarım sponsorluğu programımıza katılmak için: {link}",
  "processingOptions": {
    "batchSize": 25,          // WhatsApp rate limit uyumluluğu
    "maxConcurrency": 3,      // Paralel işlem sayısı  
    "retryAttempts": 2        // Başarısız mesajlar için yeniden deneme
  }
}
```

#### 📊 Bulk Operation Status Tracking
```bash
# İşlem durumu takibi
GET {{baseUrl}}/api/v1/bulk-operations/status/{{operationId}}

# İşlem geçmişi
GET {{baseUrl}}/api/v1/bulk-operations/history?pageSize=50

# Başarısız mesajları yeniden gönder  
POST {{baseUrl}}/api/v1/bulk-operations/retry/{{operationId}}
```

---

## 🔧 Teknik Implementasyon Durumu

### ✅ Tamamlanmış Bileşenler

#### 1. WhatsApp Business Service Altyapısı
**Dosya**: `Business/Services/Messaging/WhatsAppBusinessService.cs`

**Mevcut Özellikler**:
- Facebook Graph API v18.0 entegrasyonu hazır
- Template mesajları desteği
- Bulk messaging kapasitesi
- Rate limiting koruması
- Phone number normalization (Türkiye +90 formatı)

```csharp
// Örnek kullanım (henüz aktif değil)
public async Task<IResult> SendMessageAsync(string phoneNumber, string message)
{
    var normalizedPhone = NormalizePhoneForWhatsApp(phoneNumber);
    // Facebook Graph API call
    var result = await _httpClient.PostAsync($"{_baseUrl}/{_businessPhoneNumberId}/messages", content);
}
```

#### 2. DTO Yapıları
**Dosya**: `Entities/Dtos/MessagingDtos.cs`

**Mevcut Sınıflar**:
- `BulkWhatsAppRequest`
- `WhatsAppRecipient`
- `WhatsAppDeliveryStatus`
- `WhatsAppAccountInfo`
- `WhatsAppTemplate`

#### 3. Phone Number Formatting
```csharp
// WhatsAppBusinessService.cs:420-440
private string NormalizePhoneForWhatsApp(string phone)
{
    // +905551234567 → 905551234567 (WhatsApp formatı)
    // Türk telefon numaralarını otomatik formatlar
}
```

#### 4. Error Handling Framework
- API rate limit koruması
- Network timeout handling  
- Invalid phone number validation
- WhatsApp Business API error codes mapping

### 🚫 Eksik/Mock Implementasyonlar

#### 1. **KRİTİK**: Gerçek API Entegrasyonu Eksik
```csharp
// SendSponsorshipLinkCommand.cs:55-87
_logger.LogInformation("📤 MOCK: Sponsor {SponsorId} sending {Count} sponsorship links via {Channel}");

// YAPILMASI GEREKEN:
var result = await _whatsAppService.SendBulkMessageAsync(bulkRequest);
```

#### 2. **KRİTİK**: Konfigürasyon Eksiklikleri
```json
// appsettings.json içinde EKSİK
{
  "WhatsApp": {
    "BaseUrl": "https://graph.facebook.com/v18.0",
    "AccessToken": "REQUIRED_BUT_MISSING",           // 🚨 EKSİK
    "BusinessPhoneNumberId": "REQUIRED_BUT_MISSING", // 🚨 EKSİK
    "WebhookVerifyToken": "REQUIRED_BUT_MISSING"     // 🚨 EKSİK
  }
}
```

#### 3. Database Integration Eksiklikleri
- Sponsorship code validation atlanıyor
- Message delivery tracking kaydedilmiyor
- WhatsApp usage analytics toplanmıyor
- Cost tracking implementasyonu yok

---

## 📋 Production Hazırlık Checklist

### 🔴 Kritik Öncelik (1-2 hafta)

#### ✅ 1. WhatsApp Business API Setup
**Gerekli Adımlar**:
- [ ] Meta Business hesabı oluşturma
- [ ] WhatsApp Business API erişimi başvurusu
- [ ] Business Phone Number verification
- [ ] Access Token alma
- [ ] Webhook URL konfigürasyonu

**Tahmini Süre**: 3-5 iş günü (Meta approval süreci)

#### ✅ 2. Database Schema Ekleme
```sql
-- WhatsApp message tracking için
CREATE TABLE WhatsAppMessages (
    Id SERIAL PRIMARY KEY,
    SponsorshipCodeId INT REFERENCES SponsorshipCodes(Id),
    SponsorId INT REFERENCES Users(UserId),
    RecipientPhone VARCHAR(20) NOT NULL,
    RecipientName VARCHAR(100),
    MessageContent TEXT,
    WhatsAppMessageId VARCHAR(100), -- Meta'dan dönen message ID
    Status VARCHAR(50) DEFAULT 'sent', -- sent, delivered, read, failed
    SentAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    DeliveredAt TIMESTAMP NULL,
    ReadAt TIMESTAMP NULL,
    FailedAt TIMESTAMP NULL,
    ErrorCode VARCHAR(50) NULL,
    ErrorMessage TEXT NULL,
    Cost DECIMAL(10,4) NULL, -- Message cost tracking
    Channel VARCHAR(20) DEFAULT 'WhatsApp'
);

CREATE INDEX IX_WhatsAppMessages_SponsorId ON WhatsAppMessages(SponsorId);
CREATE INDEX IX_WhatsAppMessages_Status ON WhatsAppMessages(Status);
CREATE INDEX IX_WhatsAppMessages_SentAt ON WhatsAppMessages(SentAt);
```

#### ✅ 3. Mock Implementation Kaldırma
**Değiştirilecek Dosyalar**:
- `SendSponsorshipLinkCommand.cs:55-87` - Mock kaldır, gerçek service çağır
- `BulkOperationService.cs` - WhatsApp service entegrasyonu ekle
- `RedemptionService.cs:52-64` - Mock implementation kaldır

```csharp
// DEĞİŞTİRİLECEK (SendSponsorshipLinkCommand.cs)
// BEFORE (Mock):
var mockResult = new BulkSendResult { /* mock data */ };

// AFTER (Real):
var whatsAppRequest = new BulkWhatsAppRequest
{
    Recipients = request.Recipients.Select(r => new WhatsAppRecipient
    {
        PhoneNumber = r.Phone,
        Name = r.Name,
        PersonalizedMessage = GeneratePersonalizedMessage(r, request.CustomMessage)
    }).ToArray()
};

var result = await _whatsAppService.SendBulkMessageAsync(whatsAppRequest);
```

### 🟡 Orta Öncelik (2-3 hafta)

#### ✅ 4. Webhook Integration
**Yeni Endpoint Ekleme**:
```csharp
// WebAPI/Controllers/WhatsAppWebhookController.cs
[HttpPost("webhook")]
public async Task<IActionResult> HandleWhatsAppWebhook([FromBody] WhatsAppWebhookRequest request)
{
    // Delivery status updates
    // Message read receipts  
    // Error notifications
    // Cost tracking updates
}
```

#### ✅ 5. Template Management
**WhatsApp Business Templates**:
```json
{
  "name": "sponsorship_invitation",
  "language": "tr", 
  "components": [
    {
      "type": "BODY",
      "text": "Merhaba {{1}}, tarım sponsorluğu programımıza katılmak için: {{2}}"
    },
    {
      "type": "FOOTER", 
      "text": "ZiraAI - Tarımda Yapay Zeka"
    }
  ]
}
```

#### ✅ 6. Analytics & Monitoring
**Yeni Service**: `WhatsAppAnalyticsService.cs`
- Delivery rate tracking
- Click-through rate analytics  
- Cost per message tracking
- Daily/monthly usage reports

### 🟢 Düşük Öncelik (1 ay+)

#### ✅ 7. Advanced Features
- Media message support (images, documents)
- Interactive buttons/quick replies
- WhatsApp Business catalog integration
- Multi-language template support

---

## 💡 Kullanım Senaryoları

### Senaryo 1: Tek Sponsor - Az Mesaj (1-10 mesaj)
```bash
POST {{baseUrl}}/api/v1/sponsorships/send-link
Authorization: Bearer {{sponsorToken}}
Content-Type: application/json

{
  "recipients": [
    {"code": "AGRI-001", "phone": "+905551234567", "name": "Ali"}
  ],
  "channel": "WhatsApp", 
  "customMessage": "Merhaba {name}, ücretsiz tarım analizi için: {link}"
}
```

**Expected Response Time**: <2 saniye
**Cost**: ~$0.01-0.05 per message

### Senaryo 2: Kurumsal Sponsor - Toplu Kampanya (100+ mesaj)
```bash
POST {{baseUrl}}/api/v1/bulk-operations/send-links
Authorization: Bearer {{corporateSponsorToken}}
Content-Type: application/json

{
  "recipients": [...], // 100+ recipients
  "channel": "WhatsApp",
  "customMessage": "Sayın {name}, tarım kooperatifimiz size sponsorluk imkanı sunuyor: {link}",
  "processingOptions": {
    "batchSize": 25, // WhatsApp rate limit compliance  
    "maxConcurrency": 3,
    "retryAttempts": 2
  }
}
```

**Expected Response Time**: Background processing, 5-15 dakika
**Cost**: ~$1-5 (100 mesaj için)

### Senaryo 3: İşlem Durumu ve Analytics
```bash
# 1. İşlem durumu takibi
GET {{baseUrl}}/api/v1/bulk-operations/status/{{operationId}}

# 2. Başarısız mesajları yeniden gönder
POST {{baseUrl}}/api/v1/bulk-operations/retry/{{operationId}}

# 3. WhatsApp istatistikleri
GET {{baseUrl}}/api/v1/sponsorships/statistics
```

---

## 🚨 Önemli Kısıtlamalar ve Uyumluluk

### WhatsApp Business API Technical Limits

#### Rate Limits
- **Yeni Hesaplar**: 250 mesaj/gün (ilk 7 gün)
- **Verified Business**: 1,000 mesaj/saniye (tier-based)
- **Template Messages**: 50 mesaj/saniye (initial limit)

#### Message Types
- **Template Messages**: Meta onayı gerekli, ücretli
- **Session Messages**: 24 saat window, ücretsiz (template sonrası)
- **Media Support**: Images, documents, audio (2MB limit)

### Cost Structure (2024 pricing)
- **Marketing Messages**: $0.01-0.05 per message (country-based)
- **Utility Messages**: $0.001-0.01 per message
- **Authentication Messages**: Ücretsiz (OTP, verification)

### Yasal Uyumluluk Gereksinimleri

#### KVKV (GDPR) Compliance
```csharp
// Gerekli kontroller
public async Task<bool> HasWhatsAppConsent(int userId)
{
    // Açık rıza kontrolü - database'de kaydedilmeli
    // "WhatsApp üzerinden bilgilendirme mesajları almayı kabul ediyorum"
}
```

#### Opt-out Mechanism
```csharp
// Her mesajda bulunması gereken
string optOutMessage = "Bu mesajları almak istemiyorsanız 'DURDUR' yazın.";
```

#### Business Hours Compliance
```csharp
public bool IsWithinBusinessHours()
{
    var now = DateTime.Now.TimeOfDay;
    return now >= TimeSpan.FromHours(8) && now <= TimeSpan.FromHours(22);
}
```

---

## 🗓️ Implementation Roadmap

### Sprint 1: Foundation (1 hafta)
- [ ] WhatsApp Business API credentials alma
- [ ] Database schema oluşturma  
- [ ] Basic configuration setup
- [ ] Mock implementation'ları gerçek service çağrılarıyla değiştirme

### Sprint 2: Core Integration (1 hafta)  
- [ ] WhatsAppBusinessService'yi aktif etme
- [ ] Message tracking implementation
- [ ] Error handling ve retry logic
- [ ] Phone number validation enhancement

### Sprint 3: Webhook & Analytics (1 hafta)
- [ ] Webhook endpoint oluşturma
- [ ] Delivery status tracking
- [ ] Basic analytics dashboard
- [ ] Cost tracking implementation

### Sprint 4: Testing & Optimization (1 hafta)
- [ ] Load testing (100+ mesaj)
- [ ] Rate limit handling test
- [ ] Error scenario testing
- [ ] Performance optimization

### Sprint 5: Production & Monitoring (1 hafta)
- [ ] Production deployment
- [ ] Monitoring setup (alerts, logs)
- [ ] Documentation completion
- [ ] User training materials

---

## 📊 Success Metrics

### Technical KPIs
- **Message Delivery Rate**: >95%
- **API Response Time**: <2 seconds (single), <10 minutes (bulk)
- **Error Rate**: <5%
- **Webhook Processing**: <500ms

### Business KPIs  
- **Cost per Successful Delivery**: <$0.05
- **Click-through Rate**: >10% (link clicks)
- **User Engagement**: >30% (code redemptions)
- **Sponsor Satisfaction**: >4.5/5

### Operational KPIs
- **System Uptime**: >99.5%
- **Queue Processing**: <15 minutes (bulk operations)
- **Support Tickets**: <2% of sent messages

---

## 🔒 Security Considerations

### API Security
- WhatsApp Business API credentials güvenli storage
- Rate limiting (DDoS protection)
- Input validation (phone numbers, messages)
- Audit logging (message history)

### Data Privacy
- Phone number encryption at rest
- GDPR compliance (right to be forgotten)
- Message content retention policies
- Cross-border data transfer compliance

### Access Control
- Role-based WhatsApp access (Sponsor, Admin only)
- IP whitelist for webhook endpoints  
- API key rotation policies
- Monitor suspicious activity

---

## 📞 Support & Troubleshooting

### Common Issues

#### 1. "Message Template Not Approved"
```json
{
  "error": {
    "code": 131051,
    "title": "Message template not approved"
  }
}
```
**Solution**: Meta Business Manager'da template approval bekleyin

#### 2. "Phone Number Not Valid"
```csharp
// Debug phone number formatting
_logger.LogDebug("Original: {Original}, Normalized: {Normalized}", 
    originalPhone, NormalizePhoneForWhatsApp(originalPhone));
```

#### 3. "Rate Limit Exceeded"
**Solution**: Batch size'ı azaltın, retry logic ekleyin

### Emergency Contacts
- **WhatsApp Business Support**: Meta Business Help Center
- **Technical Team**: DevOps team escalation
- **Legal Compliance**: KVKV compliance officer

---

## 📝 Development Notes

### Code References
- **Main Controller**: `WebAPI/Controllers/SponsorshipController.cs:335`
- **Business Logic**: `Business/Handlers/Sponsorship/Commands/SendSponsorshipLinkCommand.cs`
- **WhatsApp Service**: `Business/Services/Messaging/WhatsAppBusinessService.cs`
- **DTO Definitions**: `Entities/Dtos/MessagingDtos.cs`
- **Bulk Operations**: `WebAPI/Controllers/BulkOperationsController.cs:27`

### Testing Checklist
- [ ] Unit tests for phone number normalization
- [ ] Integration tests for WhatsApp API calls
- [ ] Load tests for bulk messaging (100+ messages)
- [ ] Error handling tests (invalid phones, API failures)
- [ ] Webhook endpoint tests (delivery status updates)

### Monitoring Setup
```json
{
  "alerts": [
    {"metric": "whatsapp_delivery_rate", "threshold": 0.95, "operator": "<"},
    {"metric": "whatsapp_api_errors", "threshold": 10, "operator": ">", "window": "5m"},
    {"metric": "whatsapp_cost_daily", "threshold": 100, "operator": ">"}
  ]
}
```

---

**Dokümantasyon Versiyonu**: 1.0  
**Son Güncelleme**: Ağustos 2025  
**Sorumlu Geliştirici**: Claude Code Assistant  
**Review Status**: Pending implementation

---

*Bu dokümantasyon production implementation öncesi referans olarak kullanılmalıdır. WhatsApp Business API politikaları değişebilir, güncel bilgiler için Meta documentation kontrol edilmelidir.*