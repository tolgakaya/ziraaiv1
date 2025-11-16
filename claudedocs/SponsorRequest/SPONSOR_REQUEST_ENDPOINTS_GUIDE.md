# SponsorRequest Endpoint Sistemi - Kapsamlı Kılavuz

**Oluşturulma Tarihi**: 2025-11-16  
**Durum**: ✅ Aktif Olarak Kullanımda  
**Amaç**: Çiftçi-Ziraat Firması Sponsorluk Talep Sistemi

---

## 📋 İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [Sistem Mimarisi](#sistem-mimarisi)
3. [Endpoint Detayları](#endpoint-detayları)
4. [Güvenlik Mekanizması](#güvenlik-mekanizması)
5. [WhatsApp Entegrasyonu](#whatsapp-entegrasyonu)
6. [İş Akışı](#iş-akışı)
7. [Kod Referansları](#kod-referansları)
8. [Konfigürasyon](#konfigürasyon)

---

## Genel Bakış

### 🎯 Ana Amaç

**SponsorRequest** sistemi, **çiftçilerin Ziraat Firmalarından sponsorluk kodu talep etmesini sağlayan WhatsApp tabanlı bir iş akışı**dır. 

**Nasıl Çalışır?**
1. Çiftçi, bir Ziraat Firmasının telefon numarasını girerek sponsorluk talep eder
2. Sistem otomatik olarak güvenli bir deeplink oluşturur
3. Çiftçi, WhatsApp üzerinden Ziraat Firmasına mesaj gönderir
4. Ziraat Firması linke tıklayarak talebi görür ve onaylar
5. Sistem otomatik olarak sponsorluk kodu üretir

**Avantajları:**
- ✅ Manuel telefon görüşmesi gerektirmez
- ✅ WhatsApp üzerinden hızlı iletişim
- ✅ Token tabanlı güvenli doğrulama
- ✅ Otomatik kod üretimi
- ✅ 24 saat geçerlilik süresi ile güvenlik

---

## Sistem Mimarisi

### Entity Yapısı

**Entity**: [Entities/Concrete/SponsorRequest.cs](../../Entities/Concrete/SponsorRequest.cs)

```csharp
public class SponsorRequest : IEntity
{
    public int Id { get; set; }
    public int FarmerId { get; set; }              // Talep eden çiftçi
    public int SponsorId { get; set; }             // Ziraat Firması
    public string FarmerPhone { get; set; }        // +905551234567
    public string SponsorPhone { get; set; }       // +905557654321
    public string RequestMessage { get; set; }     // Çiftçinin mesajı
    public string RequestToken { get; set; }       // HMACSHA256 hashed token
    public DateTime RequestDate { get; set; }      // Talep tarihi
    public string Status { get; set; }             // Pending, Approved, Rejected, Expired
    public DateTime? ApprovalDate { get; set; }    // Onay tarihi
    public int? ApprovedSubscriptionTierId { get; set; }  // Tier seviyesi
    public string? ApprovalNotes { get; set; }     // Sponsor notu
    public string? GeneratedSponsorshipCode { get; set; }  // Üretilen kod
    public DateTime CreatedDate { get; set; }      // Kayıt tarihi
    public DateTime UpdatedDate { get; set; }      // Güncelleme tarihi
    
    // Navigation properties
    public virtual User Farmer { get; set; }
    public virtual User Sponsor { get; set; }
    public virtual SubscriptionTier ApprovedSubscriptionTier { get; set; }
}
```

### Status Durumları

| Status | Açıklama | Geçiş Yolu |
|--------|----------|-----------|
| **Pending** | Oluşturuldu, sponsor onayı bekleniyor | İlk durum |
| **Approved** | Sponsor onayladı, kod üretildi | Pending → Approved |
| **Rejected** | Sponsor reddetti | Pending → Rejected ⚠️ (henüz yok) |
| **Expired** | 24 saat geçti, token artık geçersiz | Pending → Expired (otomatik) |

---

## Endpoint Detayları

### 1. POST /api/sponsorrequest/create (Talep Oluşturma)

**Controller**: [WebAPI/Controllers/SponsorRequestController.cs:22-35](../../WebAPI/Controllers/SponsorRequestController.cs#L22-L35)  
**Handler**: [Business/Handlers/SponsorRequest/Commands/CreateSponsorRequestCommand.cs](../../Business/Handlers/SponsorRequest/Commands/CreateSponsorRequestCommand.cs)  
**Service**: [Business/Services/SponsorRequest/SponsorRequestService.cs:38-98](../../Business/Services/SponsorRequest/SponsorRequestService.cs#L38-L98)

**Yetki**: `[Authorize(Roles = "Farmer,Admin")]`

#### Request

```http
POST /api/sponsorrequest/create HTTP/1.1
Host: api.ziraai.com
Authorization: Bearer {farmer_jwt_token}
Content-Type: application/json

{
  "sponsorId": 456,
  "requestMessage": "Merhaba, bitki analizi için sponsorluk kodu talep ediyorum."
}
```

**DTO**: `CreateSponsorRequestDto`
- `SponsorId` (int, required): Ziraat Firması ID
- `RequestMessage` (string, optional): Özel mesaj

#### Response

```json
{
  "success": true,
  "data": {
    "requestId": 123,
    "whatsappUrl": "https://wa.me/+905551234567?text=...",
    "deeplinkUrl": "https://ziraai.com/sponsor-request/abc123xyz",
    "status": "Pending",
    "expiresAt": "2025-11-17T15:30:00Z"
  },
  "message": "Sponsorluk talebi oluşturuldu"
}
```

#### İşleyiş Adımları

```csharp
// 1. JWT'den farmer bilgisi çekme
var farmerId = User.GetUserId();
var farmer = await _userRepository.GetAsync(u => u.UserId == farmerId);

// 2. Sponsor kontrolü
var sponsor = await _userRepository.GetAsync(u => u.UserId == dto.SponsorId);
if (sponsor == null) return Error("Sponsor bulunamadı");

// 3. Duplicate talep kontrolü
var existingRequest = await _sponsorRequestRepository.GetAsync(
    sr => sr.FarmerId == farmerId && 
          sr.SponsorId == dto.SponsorId && 
          sr.Status == "Pending"
);
if (existingRequest != null) return Error("Bu sponsora zaten bekleyen talebiniz var");

// 4. Güvenli token oluşturma (HMACSHA256)
var token = GenerateRequestToken(farmer.MobilePhones, sponsor.MobilePhones, farmerId);

// 5. Request entity oluşturma
var request = new SponsorRequest
{
    FarmerId = farmerId,
    SponsorId = dto.SponsorId,
    FarmerPhone = farmer.MobilePhones,
    SponsorPhone = sponsor.MobilePhones,
    RequestMessage = dto.RequestMessage ?? "Sponsorluk kodu talep ediyorum",
    RequestToken = token,
    RequestDate = DateTime.Now,
    Status = "Pending",
    CreatedDate = DateTime.Now,
    UpdatedDate = DateTime.Now
};

// 6. Veritabanına kaydetme
await _sponsorRequestRepository.AddAsync(request);
await _sponsorRequestRepository.SaveChangesAsync();

// 7. WhatsApp URL oluşturma
var whatsappUrl = GenerateWhatsAppMessage(request);

// 8. Response dönme
return new SuccessDataResult<SponsorRequestDto>(new SponsorRequestDto
{
    RequestId = request.Id,
    WhatsappUrl = whatsappUrl,
    DeeplinkUrl = $"{baseUrl}{token}",
    Status = request.Status,
    ExpiresAt = request.RequestDate.AddHours(24)
});
```

#### Önemli Kurallar

- ✅ Aynı çiftçi-sponsor çifti için aynı anda sadece **1 adet Pending** talep olabilir
- ✅ Token **24 saat** geçerlidir
- ✅ WhatsApp URL frontend tarafından kullanılır (çiftçi "Mesaj Gönder" butonuna tıklar)
- ⚠️ Farmer rolü zorunludur

---

### 2. GET /api/sponsorrequest/process/{hashedToken} (Deeplink İşleme)

**Controller**: [WebAPI/Controllers/SponsorRequestController.cs:42-53](../../WebAPI/Controllers/SponsorRequestController.cs#L42-L53)  
**Handler**: [Business/Handlers/SponsorRequest/Queries/ProcessDeeplinkQuery.cs](../../Business/Handlers/SponsorRequest/Queries/ProcessDeeplinkQuery.cs)  
**Service**: [Business/Services/SponsorRequest/SponsorRequestService.cs:101-135](../../Business/Services/SponsorRequest/SponsorRequestService.cs#L101-L135)

**Yetki**: Public (token tabanlı güvenlik)

#### Request

```http
GET /api/sponsorrequest/process/abc123xyz HTTP/1.1
Host: api.ziraai.com
```

**URL Parametresi**:
- `hashedToken` (string, required): HMACSHA256 ile oluşturulmuş token

#### Response (Başarılı)

```json
{
  "success": true,
  "data": {
    "requestId": 123,
    "farmerId": 789,
    "farmerName": "Ahmet Yılmaz",
    "farmerPhone": "+905551234567",
    "requestMessage": "Bitki analizi için kod talep ediyorum",
    "requestDate": "2025-11-16T15:30:00Z",
    "status": "Pending",
    "tier": {
      "tierId": 3,
      "tierName": "M - Orta Paket",
      "analysisLimit": 100
    }
  }
}
```

#### Response (Expired)

```json
{
  "success": false,
  "message": "Bu talep süresi dolmuştur. Lütfen yeni bir talep oluşturun.",
  "data": {
    "status": "Expired",
    "expirationDate": "2025-11-17T15:30:00Z"
  }
}
```

#### İşleyiş Adımları

```csharp
// 1. Token ile request bulma
var request = await _sponsorRequestRepository.GetAsync(
    sr => sr.RequestToken == hashedToken,
    include: q => q.Include(sr => sr.Farmer)
                   .Include(sr => sr.Sponsor)
                   .Include(sr => sr.ApprovedSubscriptionTier)
);

if (request == null) return Error("Geçersiz veya bulunamayan token");

// 2. Status kontrolü
if (request.Status != "Pending")
{
    return Error($"Bu talep zaten işlenmiş. Durum: {request.Status}");
}

// 3. Expiry kontrolü (24 saat)
var expirationTime = request.RequestDate.AddHours(24);
if (DateTime.Now > expirationTime)
{
    request.Status = "Expired";
    request.UpdatedDate = DateTime.Now;
    await _sponsorRequestRepository.UpdateAsync(request);
    await _sponsorRequestRepository.SaveChangesAsync();
    
    return Error("Bu talep süresi dolmuştur");
}

// 4. Geçerli talep - detayları döndür
return new SuccessDataResult<ProcessDeeplinkDto>(new ProcessDeeplinkDto
{
    RequestId = request.Id,
    FarmerId = request.FarmerId,
    FarmerName = request.Farmer.FullName,
    FarmerPhone = request.FarmerPhone,
    RequestMessage = request.RequestMessage,
    RequestDate = request.RequestDate,
    Status = request.Status,
    Tier = request.ApprovedSubscriptionTier
});
```

#### Kullanım Senaryosu

```
1. Çiftçi WhatsApp'tan mesaj gönderir
   ↓
2. Sponsor WhatsApp mesajındaki linke tıklar:
   https://ziraai.com/sponsor-request/abc123xyz
   ↓
3. Frontend, sayfayı yükler ve backend'e istek atar:
   GET /api/sponsorrequest/process/abc123xyz
   ↓
4. Backend token'ı doğrular:
   - Token geçerli mi? ✅
   - Status Pending mi? ✅
   - 24 saat içinde mi? ✅
   ↓
5. Frontend sponsor'a talep detaylarını gösterir:
   - Çiftçi bilgileri
   - Talep mesajı
   - "Onayla" / "Reddet" butonları
```

#### Önemli Kurallar

- ✅ Token **one-time use** değil, ancak status Pending'den çıktığında artık kullanılamaz
- ✅ 24 saat sonra otomatik **Expired** olur
- ✅ Public endpoint (authorization yok, token yeterli)
- ⚠️ Frontend bu endpoint'i sayfa yüklendiğinde çağırmalı

---

### 3. GET /api/sponsorrequest/pending (Bekleyen Talepler)

**Controller**: [WebAPI/Controllers/SponsorRequestController.cs:60-68](../../WebAPI/Controllers/SponsorRequestController.cs#L60-L68)  
**Handler**: `GetPendingSponsorRequestsQuery`

**Yetki**: `[Authorize(Roles = "Sponsor,Admin")]`

#### Request

```http
GET /api/sponsorrequest/pending HTTP/1.1
Host: api.ziraai.com
Authorization: Bearer {sponsor_jwt_token}
```

#### Response

```json
{
  "success": true,
  "data": [
    {
      "requestId": 123,
      "farmer": {
        "farmerId": 789,
        "fullName": "Ahmet Yılmaz",
        "phone": "+905551234567"
      },
      "requestMessage": "Bitki analizi için kod talep ediyorum",
      "requestDate": "2025-11-16T15:30:00Z",
      "tier": {
        "tierId": 3,
        "tierName": "M - Orta Paket"
      },
      "expiresAt": "2025-11-17T15:30:00Z"
    },
    {
      "requestId": 124,
      "farmer": {
        "farmerId": 790,
        "fullName": "Mehmet Kaya",
        "phone": "+905559876543"
      },
      "requestMessage": "Sponsorluk kodu istiyorum",
      "requestDate": "2025-11-16T16:00:00Z",
      "tier": null,
      "expiresAt": "2025-11-17T16:00:00Z"
    }
  ]
}
```

#### İşleyiş

```csharp
// 1. JWT'den sponsor ID çekme
var sponsorId = User.GetUserId();

// 2. Pending talepleri getirme
var requests = await _sponsorRequestRepository.GetListAsync(
    predicate: sr => sr.SponsorId == sponsorId && sr.Status == "Pending",
    include: q => q.Include(sr => sr.Farmer)
                   .Include(sr => sr.ApprovedSubscriptionTier),
    orderBy: q => q.OrderByDescending(sr => sr.RequestDate)
);

// 3. DTO'ya mapping
var dtos = requests.Select(r => new PendingSponsorRequestDto
{
    RequestId = r.Id,
    Farmer = new FarmerInfoDto
    {
        FarmerId = r.FarmerId,
        FullName = r.Farmer.FullName,
        Phone = r.FarmerPhone
    },
    RequestMessage = r.RequestMessage,
    RequestDate = r.RequestDate,
    Tier = r.ApprovedSubscriptionTier != null ? new TierDto
    {
        TierId = r.ApprovedSubscriptionTier.Id,
        TierName = r.ApprovedSubscriptionTier.Name
    } : null,
    ExpiresAt = r.RequestDate.AddHours(24)
}).ToList();

return new SuccessDataResult<List<PendingSponsorRequestDto>>(dtos);
```

#### Kullanım Senaryosu

```
1. Sponsor panel'e giriş yapar
   ↓
2. "Bekleyen Talepler" sekmesine tıklar
   ↓
3. Frontend: GET /api/sponsorrequest/pending
   ↓
4. Backend sponsor'a gelen tüm Pending talepleri listeler
   ↓
5. Frontend her talep için:
   - Çiftçi adı
   - Talep mesajı
   - Kalan süre (24 saatten geri sayım)
   - "Onayla" butonu
```

---

### 4. POST /api/sponsorrequest/approve (Talep Onaylama)

**Controller**: [WebAPI/Controllers/SponsorRequestController.cs:76-89](../../WebAPI/Controllers/SponsorRequestController.cs#L76-L89)  
**Handler**: [Business/Handlers/SponsorRequest/Commands/ApproveSponsorRequestCommand.cs](../../Business/Handlers/SponsorRequest/Commands/ApproveSponsorRequestCommand.cs)  
**Service**: [Business/Services/SponsorRequest/SponsorRequestService.cs:138-174](../../Business/Services/SponsorRequest/SponsorRequestService.cs#L138-L174)

**Yetki**: `[Authorize(Roles = "Sponsor,Admin")]`

#### Request

```http
POST /api/sponsorrequest/approve HTTP/1.1
Host: api.ziraai.com
Authorization: Bearer {sponsor_jwt_token}
Content-Type: application/json

{
  "requestIds": [123, 124, 125],
  "approvalNotes": "Hoş geldiniz! Kodunuzu aşağıda bulabilirsiniz."
}
```

**DTO**: `ApproveSponsorRequestDto`
- `RequestIds` (List<int>, required): Onaylanacak talep ID'leri
- `ApprovalNotes` (string, optional): Sponsor notu

#### Response

```json
{
  "success": true,
  "data": {
    "approvedCount": 3,
    "approvedRequests": [
      {
        "requestId": 123,
        "farmerId": 789,
        "farmerName": "Ahmet Yılmaz",
        "generatedCode": "ZIRA-ABC123XYZ",
        "tier": "M - Orta Paket",
        "approvalDate": "2025-11-16T17:00:00Z"
      },
      {
        "requestId": 124,
        "farmerId": 790,
        "farmerName": "Mehmet Kaya",
        "generatedCode": "ZIRA-DEF456UVW",
        "tier": "S - Küçük Paket",
        "approvalDate": "2025-11-16T17:00:00Z"
      },
      {
        "requestId": 125,
        "farmerId": 791,
        "farmerName": "Ali Demir",
        "generatedCode": "ZIRA-GHI789RST",
        "tier": "L - Büyük Paket",
        "approvalDate": "2025-11-16T17:00:00Z"
      }
    ]
  },
  "message": "3 talep başarıyla onaylandı ve kodlar üretildi"
}
```

#### İşleyiş Adımları

```csharp
// 1. JWT'den sponsor ID çekme
var sponsorId = User.GetUserId();

// 2. Her request için döngü
var approvedRequests = new List<ApprovedRequestDto>();

foreach (var requestId in dto.RequestIds)
{
    // 3. Request bulma ve doğrulama
    var request = await _sponsorRequestRepository.GetAsync(
        sr => sr.Id == requestId && sr.SponsorId == sponsorId,
        include: q => q.Include(sr => sr.Farmer)
                       .Include(sr => sr.ApprovedSubscriptionTier)
    );
    
    if (request == null)
    {
        _logger.LogWarning($"Request {requestId} bulunamadı veya bu sponsor'a ait değil");
        continue;
    }
    
    if (request.Status != "Pending")
    {
        _logger.LogWarning($"Request {requestId} zaten işlenmiş. Status: {request.Status}");
        continue;
    }
    
    // 4. Sponsorluk kodu üretme
    var codeDto = new GenerateSponsorshipCodeDto
    {
        SponsorId = sponsorId,
        SubscriptionTierId = request.ApprovedSubscriptionTierId ?? 2, // Default: S tier
        Quantity = 1,
        ExpirationDate = DateTime.Now.AddMonths(6),
        Notes = $"Talep ID: {requestId} için otomatik üretildi"
    };
    
    var generatedCode = await _sponsorshipCodeService.GenerateSponsorshipCodeAsync(codeDto);
    
    // 5. Request güncelleme
    request.Status = "Approved";
    request.ApprovalDate = DateTime.Now;
    request.GeneratedSponsorshipCode = generatedCode.Code;
    request.ApprovalNotes = dto.ApprovalNotes;
    request.UpdatedDate = DateTime.Now;
    
    await _sponsorRequestRepository.UpdateAsync(request);
    
    // 6. Response listesine ekleme
    approvedRequests.Add(new ApprovedRequestDto
    {
        RequestId = request.Id,
        FarmerId = request.FarmerId,
        FarmerName = request.Farmer.FullName,
        GeneratedCode = generatedCode.Code,
        Tier = request.ApprovedSubscriptionTier?.Name,
        ApprovalDate = request.ApprovalDate.Value
    });
}

// 7. Veritabanına kaydetme
await _sponsorRequestRepository.SaveChangesAsync();

// 8. Response dönme
return new SuccessDataResult<ApprovalResponseDto>(new ApprovalResponseDto
{
    ApprovedCount = approvedRequests.Count,
    ApprovedRequests = approvedRequests
});
```

#### Otomatik Kod Üretimi

**SponsorshipCodeService Integration**:

```csharp
public async Task<GeneratedCodeDto> GenerateSponsorshipCodeAsync(GenerateSponsorshipCodeDto dto)
{
    // 1. Unique kod oluşturma
    var code = GenerateUniqueCode();  // Örnek: ZIRA-ABC123XYZ
    
    // 2. SponsorshipCode entity oluşturma
    var sponsorshipCode = new SponsorshipCode
    {
        Code = code,
        SponsorId = dto.SponsorId,
        SubscriptionTierId = dto.SubscriptionTierId,
        ExpirationDate = dto.ExpirationDate,
        IsActive = true,
        UsageLimit = 1,
        UsedCount = 0,
        CreatedDate = DateTime.Now
    };
    
    // 3. Veritabanına kaydetme
    await _sponsorshipCodeRepository.AddAsync(sponsorshipCode);
    await _sponsorshipCodeRepository.SaveChangesAsync();
    
    return new GeneratedCodeDto
    {
        Code = code,
        ExpirationDate = dto.ExpirationDate,
        Tier = await _subscriptionTierRepository.GetAsync(t => t.Id == dto.SubscriptionTierId)
    };
}
```

#### Önemli Kurallar

- ✅ Sadece **kendi taleplerine** onay verebilir (SponsorId kontrolü)
- ✅ Sadece **Pending** talepleri onaylanabilir
- ✅ Her talep için **otomatik olarak 1 adet kod** üretilir
- ✅ Kod varsayılan olarak **6 ay geçerlidir**
- ✅ Kod **tek kullanımlıktır** (UsageLimit: 1)
- ✅ Birden fazla talebi **toplu onaylama** desteklenir
- ⚠️ Geçersiz talepler atlanır, hata fırlatmaz (logging yapılır)

---

### 5. POST /api/sponsorrequest/reject (Talep Reddetme)

**Controller**: [WebAPI/Controllers/SponsorRequestController.cs:98-104](../../WebAPI/Controllers/SponsorRequestController.cs#L98-L104)  
**Handler**: ⚠️ **Henüz implement edilmemiş**

**Yetki**: `[Authorize(Roles = "Sponsor,Admin")]`

#### Mevcut Durum

```csharp
[HttpPost("reject")]
public async Task<IActionResult> RejectRequests([FromBody] RejectSponsorRequestDto dto)
{
    // TODO: Implement reject functionality
    return Ok(new { message = "Reject functionality not yet implemented" });
}
```

#### Planlanan İşleyiş

```csharp
// Planlanan implementasyon
public async Task<IResult> RejectRequestsAsync(List<int> requestIds, string rejectionReason)
{
    foreach (var requestId in requestIds)
    {
        var request = await _sponsorRequestRepository.GetAsync(sr => sr.Id == requestId);
        
        if (request != null && request.Status == "Pending")
        {
            request.Status = "Rejected";
            request.ApprovalNotes = rejectionReason;
            request.UpdatedDate = DateTime.Now;
            
            await _sponsorRequestRepository.UpdateAsync(request);
        }
    }
    
    await _sponsorRequestRepository.SaveChangesAsync();
    return new SuccessResult("Talepler reddedildi");
}
```

---

## Güvenlik Mekanizması

### Token Oluşturma (HMACSHA256)

**Service Method**: [SponsorRequestService.cs:210-225](../../Business/Services/SponsorRequest/SponsorRequestService.cs#L210-L225)

```csharp
public string GenerateRequestToken(string farmerPhone, string sponsorPhone, int farmerId)
{
    // 1. Payload oluşturma - timestamp ile unique
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var payload = $"{farmerId}:{farmerPhone}:{sponsorPhone}:{timestamp}";
    
    // 2. Secret key alma (appsettings.json)
    var secret = _configuration["Security:RequestTokenSecret"] ?? "DefaultSecretKey123!@#";
    
    // 3. HMACSHA256 ile hash
    using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
    {
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        
        // 4. URL-safe base64 encoding
        return Convert.ToBase64String(hash)
            .Replace('+', '-')    // URL-safe karakter
            .Replace('/', '_')    // URL-safe karakter
            .Replace("=", "");    // Padding kaldırma
    }
}
```

### Güvenlik Özellikleri

#### 1. Kriptografik Hash (HMACSHA256)
- ✅ Tek yönlü hash (reverse edilemez)
- ✅ Secret key ile imzalanır
- ✅ Değiştirme/tahmin edilemez

#### 2. Timestamp-based Uniqueness
```csharp
var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
// Örnek: 1700235890
```
- ✅ Her token benzersizdir
- ✅ Aynı parametreler bile farklı token üretir

#### 3. 24 Saat Otomatik Expiry
```csharp
var expirationTime = request.RequestDate.AddHours(24);
if (DateTime.Now > expirationTime)
{
    request.Status = "Expired";
}
```
- ✅ Eski linkler otomatik expire olur
- ✅ Güvenlik penceresi sınırlı

#### 4. One-Time Processing Logic
```csharp
if (request.Status != "Pending")
{
    return Error("Bu talep zaten işlenmiş");
}
```
- ✅ Status Pending'den çıktığında link artık geçersiz
- ✅ Tekrar onaylama/reddetme engellenmiş

#### 5. URL-Safe Encoding
```csharp
.Replace('+', '-')
.Replace('/', '_')
.Replace("=", "")
```
- ✅ URL'de problem çıkarmaz
- ✅ WhatsApp mesajlarında güvenle kullanılabilir

### Güvenlik En İyi Uygulamaları

**appsettings.json**:
```json
{
  "Security": {
    "RequestTokenSecret": "YourVerySecureRandomKey123!@#$%^&*()"
  }
}
```

**⚠️ ÖNEMLİ**:
- Production'da **güçlü bir secret key** kullanın (minimum 32 karakter)
- Secret key'i **asla git'e commit etmeyin**
- Environment variable olarak yönetin
- Her environment için farklı secret kullanın

---

## WhatsApp Entegrasyonu

### WhatsApp URL Oluşturma

**Service Method**: [SponsorRequestService.cs:197-208](../../Business/Services/SponsorRequest/SponsorRequestService.cs#L197-L208)

```csharp
public string GenerateWhatsAppMessage(SponsorRequest request)
{
    // 1. Deeplink base URL (konfigürasyondan)
    var baseUrl = _configuration["SponsorRequest:DeepLinkBaseUrl"] ?? 
                  "https://ziraai.com/sponsor-request/";
    
    // 2. Tam deeplink URL
    var deeplinkUrl = $"{baseUrl}{request.RequestToken}";
    
    // 3. Mesaj içeriği (custom veya default)
    var message = request.RequestMessage ?? 
                  _configuration["SponsorRequest:DefaultRequestMessage"] ??
                  "Merhaba, ZiraAI üzerinden sponsorluk kodu talep ediyorum.";
    
    // 4. Mesajı deeplink ile birleştirme
    var fullMessage = $"{message}\n\nOnaylamak için tıklayın: {deeplinkUrl}";
    
    // 5. URL encoding
    var encodedMessage = Uri.EscapeDataString(fullMessage);
    
    // 6. WhatsApp URL formatı
    return $"https://wa.me/{request.SponsorPhone}?text={encodedMessage}";
}
```

### Örnek WhatsApp URL

**Input**:
```csharp
SponsorPhone = "+905551234567"
RequestMessage = "Merhaba, bitki analizi için sponsorluk kodu talep ediyorum."
RequestToken = "abc123xyz"
```

**Output**:
```
https://wa.me/+905551234567?text=Merhaba%2C%20bitki%20analizi%20i%C3%A7in%20sponsorluk%20kodu%20talep%20ediyorum.%0A%0AOnaylamak%20i%C3%A7in%20t%C4%B1klay%C4%B1n%3A%20https%3A%2F%2Fziraai.com%2Fsponsor-request%2Fabc123xyz
```

**Decoded Mesaj**:
```
Merhaba, bitki analizi için sponsorluk kodu talep ediyorum.

Onaylamak için tıklayın: https://ziraai.com/sponsor-request/abc123xyz
```

### WhatsApp Akışı

```
1. Frontend "Sponsor Talep Et" butonu
   ↓
2. POST /api/sponsorrequest/create
   ↓
3. Backend WhatsApp URL döner
   ↓
4. Frontend WhatsApp URL'sini kullanarak:
   <a href="{whatsappUrl}" target="_blank">
     WhatsApp'tan Mesaj Gönder
   </a>
   ↓
5. Kullanıcı butona tıklar
   ↓
6. WhatsApp açılır, mesaj otomatik dolu
   ↓
7. Kullanıcı "Gönder" tuşuna basar
   ↓
8. Sponsor WhatsApp'tan mesajı alır
   ↓
9. Sponsor linke tıklar
   ↓
10. Frontend deeplink'i yakalar ve backend'e gönderir
```

### Mobil Uygulama Entegrasyonu

**Android Deep Link Handling**:

```xml
<!-- AndroidManifest.xml -->
<activity android:name=".SponsorRequestActivity">
    <intent-filter>
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        
        <data
            android:scheme="https"
            android:host="ziraai.com"
            android:pathPrefix="/sponsor-request/" />
    </intent-filter>
</activity>
```

**Flutter Deep Link Handling**:

```dart
// main.dart
void main() {
  runApp(MyApp());
  _handleDeepLinks();
}

void _handleDeepLinks() async {
  // Listen to incoming links
  _sub = uriLinkStream.listen((Uri? uri) {
    if (uri != null && uri.path.startsWith('/sponsor-request/')) {
      String token = uri.pathSegments.last;
      _processDeeplink(token);
    }
  });
}

void _processDeeplink(String token) async {
  final response = await http.get(
    Uri.parse('https://api.ziraai.com/api/sponsorrequest/process/$token')
  );
  
  if (response.statusCode == 200) {
    // Show request details to sponsor
    Navigator.push(context, SponsorRequestDetailsPage(data));
  }
}
```

---

## İş Akışı

### Tam Kullanıcı Akışı (End-to-End)

```
┌─────────────────────────────────────────────────────────────────┐
│                    1. ÇIFTÇI: TALEP OLUŞTURMA                    │
└─────────────────────────────────────────────────────────────────┘
                                ↓
    Çiftçi mobile app'i açar → "Sponsor Talep Et" sekmesi
                                ↓
    Sponsor listesi görür (veya telefon numarası girer)
                                ↓
    Sponsor seçer + mesaj yazar (opsiyonel)
                                ↓
    Frontend: POST /api/sponsorrequest/create
    {
      "sponsorId": 456,
      "requestMessage": "Bitki analizi için kod istiyorum"
    }
                                ↓
    Backend: Token oluşturur + WhatsApp URL döner
                                ↓
    Frontend: "WhatsApp'tan Gönder" butonu gösterir

┌─────────────────────────────────────────────────────────────────┐
│                   2. ÇIFTÇI: WHATSAPP MESAJI                     │
└─────────────────────────────────────────────────────────────────┘
                                ↓
    Çiftçi "WhatsApp'tan Gönder" butonuna tıklar
                                ↓
    WhatsApp açılır, mesaj hazır:
    "Merhaba, bitki analizi için kod talep ediyorum.
     
     Onaylamak için tıklayın: https://ziraai.com/sponsor-request/abc123"
                                ↓
    Çiftçi "Gönder" tuşuna basar
                                ↓
    Mesaj sponsor'a iletilir

┌─────────────────────────────────────────────────────────────────┐
│                  3. SPONSOR: LİNKE TIKLAMA                       │
└─────────────────────────────────────────────────────────────────┘
                                ↓
    Sponsor WhatsApp'tan mesajı alır
                                ↓
    Linke tıklar: https://ziraai.com/sponsor-request/abc123
                                ↓
    Mobile app açılır (deep link handling)
    VEYA
    Web browser açılır → app'e redirect
                                ↓
    Frontend: GET /api/sponsorrequest/process/abc123
                                ↓
    Backend: Token doğrular + talep detaylarını döner
    {
      "farmerId": 789,
      "farmerName": "Ahmet Yılmaz",
      "farmerPhone": "+905551234567",
      "requestMessage": "...",
      "requestDate": "2025-11-16T15:30:00Z",
      "status": "Pending"
    }
                                ↓
    Frontend: Talep detay sayfası gösterir
    - Çiftçi adı
    - Telefon
    - Mesaj
    - Kalan süre (24 saatten geri sayım)
    - "Onayla" ve "Reddet" butonları

┌─────────────────────────────────────────────────────────────────┐
│                 4. SPONSOR: BEKLEYEN TALEPLER                    │
└─────────────────────────────────────────────────────────────────┘
                                ↓
    Sponsor app'te "Bekleyen Talepler" sekmesine gider
                                ↓
    Frontend: GET /api/sponsorrequest/pending
                                ↓
    Backend: Tüm Pending talepleri listeler
                                ↓
    Frontend: Liste gösterir
    - Her talep için: Çiftçi adı, mesaj, kalan süre
    - Çoklu seçim checkbox'ları
    - "Toplu Onayla" butonu

┌─────────────────────────────────────────────────────────────────┐
│                   5. SPONSOR: ONAYLAMA                           │
└─────────────────────────────────────────────────────────────────┘
                                ↓
    Sponsor talepleri seçer (tekli veya çoklu)
                                ↓
    "Onayla" butonuna tıklar
                                ↓
    Opsiyonel: Not ekler
    "Hoş geldiniz! Kodunuzu aşağıda bulabilirsiniz."
                                ↓
    Frontend: POST /api/sponsorrequest/approve
    {
      "requestIds": [123, 124, 125],
      "approvalNotes": "Hoş geldiniz..."
    }
                                ↓
    Backend: Her talep için:
    1. Status kontrolü (Pending mi?)
    2. Sponsorluk kodu üretimi (ZIRA-ABC123)
    3. Status → "Approved"
    4. GeneratedSponsorshipCode → "ZIRA-ABC123"
    5. ApprovalDate → şimdi
    6. ApprovalNotes → sponsor'un notu
                                ↓
    Backend: Response döner
    {
      "approvedCount": 3,
      "approvedRequests": [
        {
          "requestId": 123,
          "farmerName": "Ahmet Yılmaz",
          "generatedCode": "ZIRA-ABC123",
          "tier": "M - Orta Paket"
        },
        ...
      ]
    }
                                ↓
    Frontend: Başarı mesajı gösterir
    "3 talep onaylandı ve kodlar üretildi!"

┌─────────────────────────────────────────────────────────────────┐
│                   6. ÇIFTÇI: KOD KULLANIMI                       │
└─────────────────────────────────────────────────────────────────┘
                                ↓
    Çiftçi app'te "Kodlarım" sekmesine gider
    VEYA
    Push notification alır: "Kodunuz onaylandı!"
                                ↓
    Frontend: GET /api/farmer/sponsorship-codes
    (veya sponsor bildirim gönderir)
                                ↓
    Çiftçi kodu görür: "ZIRA-ABC123"
                                ↓
    Bitki analizi yaparken kodu kullanır
                                ↓
    Backend: Kod doğrulama + subscription aktif etme
                                ↓
    Çiftçi artık analiz yapabilir!
```

### Durum Geçişleri

```
                    CREATE REQUEST
                          ↓
                    ┌──────────┐
                    │ Pending  │ ←─ İlk durum
                    └──────────┘
                          │
         ┌────────────────┼────────────────┐
         │                │                │
    APPROVE          REJECT           24 SAAT
         │                │                │
         ↓                ↓                ↓
   ┌──────────┐    ┌──────────┐    ┌──────────┐
   │ Approved │    │ Rejected │    │ Expired  │
   └──────────┘    └──────────┘    └──────────┘
         │                │                │
         │                │                │
    Kod Üretildi    Kod Yok         Kod Yok
    Status: Final   Status: Final   Status: Final
```

### Hata Senaryoları

#### 1. Duplicate Talep
```
Çiftçi → POST /create (sponsorId: 456)
Backend → Kontrol: Pending talep var mı?
Backend → Error: "Bu sponsora zaten bekleyen talebiniz var"
```

#### 2. Expired Token
```
Sponsor → Link'e tıklar (26 saat sonra)
Backend → GET /process/{token}
Backend → Kontrol: requestDate + 24h < now?
Backend → Status: "Expired"
Backend → Error: "Bu talep süresi dolmuş"
```

#### 3. Geçersiz Token
```
Hacker → GET /process/fake-token
Backend → Kontrol: RequestToken == "fake-token"?
Backend → Result: null
Backend → Error: "Geçersiz veya bulunamayan token"
```

#### 4. Zaten İşlenmiş Talep
```
Sponsor → Link'e tıklar (daha önce onaylanmış)
Backend → GET /process/{token}
Backend → Kontrol: status == "Pending"?
Backend → status: "Approved"
Backend → Error: "Bu talep zaten işlenmiş"
```

---

## Kod Referansları

### Controller
- **Ana Controller**: [WebAPI/Controllers/SponsorRequestController.cs](../../WebAPI/Controllers/SponsorRequestController.cs)
  - Line 22-35: Create endpoint
  - Line 42-53: Process deeplink endpoint
  - Line 60-68: Get pending requests endpoint
  - Line 76-89: Approve requests endpoint
  - Line 98-104: Reject requests endpoint (placeholder)

### Entity & DTOs
- **Entity**: [Entities/Concrete/SponsorRequest.cs](../../Entities/Concrete/SponsorRequest.cs)
- **DTOs**: [Entities/Dtos/SponsorRequestDto.cs](../../Entities/Dtos/SponsorRequestDto.cs)
  - `CreateSponsorRequestDto`: Create request için
  - `ProcessDeeplinkDto`: Deeplink response
  - `PendingSponsorRequestDto`: Pending list response
  - `ApproveSponsorRequestDto`: Approve request için
  - `ApprovalResponseDto`: Approve response

### Business Logic
- **Commands**:
  - [Business/Handlers/SponsorRequest/Commands/CreateSponsorRequestCommand.cs](../../Business/Handlers/SponsorRequest/Commands/CreateSponsorRequestCommand.cs)
  - [Business/Handlers/SponsorRequest/Commands/ApproveSponsorRequestCommand.cs](../../Business/Handlers/SponsorRequest/Commands/ApproveSponsorRequestCommand.cs)
- **Queries**:
  - [Business/Handlers/SponsorRequest/Queries/ProcessDeeplinkQuery.cs](../../Business/Handlers/SponsorRequest/Queries/ProcessDeeplinkQuery.cs)
  - `GetPendingSponsorRequestsQuery` (handler dosyası)

### Service
- **Ana Service**: [Business/Services/SponsorRequest/SponsorRequestService.cs](../../Business/Services/SponsorRequest/SponsorRequestService.cs)
  - Line 38-98: `CreateRequestAsync`
  - Line 101-135: `ProcessDeeplinkAsync`
  - Line 138-174: `ApproveRequestsAsync`
  - Line 197-208: `GenerateWhatsAppMessage`
  - Line 210-225: `GenerateRequestToken`

### Repository
- **Interface**: `DataAccess/Abstract/ISponsorRequestRepository.cs`
- **Implementation**: `DataAccess/Concrete/EntityFramework/SponsorRequestRepository.cs`

### Database Configuration
- **EF Configuration**: `DataAccess/Concrete/Configurations/SponsorRequestEntityConfiguration.cs`
- **DbContext**: `DataAccess/Concrete/EntityFramework/Contexts/ProjectDbContext.cs`

---

## Konfigürasyon

### appsettings.json

**Development**:
```json
{
  "Security": {
    "RequestTokenSecret": "DevSecretKey123!@#ForDevelopmentOnly"
  },
  "SponsorRequest": {
    "DeepLinkBaseUrl": "http://localhost:5001/sponsor-request/",
    "DefaultRequestMessage": "Merhaba, ZiraAI üzerinden sponsorluk kodu talep ediyorum.",
    "TokenExpirationHours": 24
  },
  "MobileApp": {
    "AndroidPackageName": "com.ziraai.app.dev",
    "iOSBundleId": "com.ziraai.app.dev"
  }
}
```

**Staging**:
```json
{
  "Security": {
    "RequestTokenSecret": "${SPONSOR_REQUEST_TOKEN_SECRET}"  // Environment variable
  },
  "SponsorRequest": {
    "DeepLinkBaseUrl": "https://ziraai-staging.com/sponsor-request/",
    "DefaultRequestMessage": "Merhaba, ZiraAI üzerinden sponsorluk kodu talep ediyorum.",
    "TokenExpirationHours": 24
  },
  "MobileApp": {
    "AndroidPackageName": "com.ziraai.app.staging",
    "iOSBundleId": "com.ziraai.app.staging"
  }
}
```

**Production**:
```json
{
  "Security": {
    "RequestTokenSecret": "${SPONSOR_REQUEST_TOKEN_SECRET}"  // Environment variable
  },
  "SponsorRequest": {
    "DeepLinkBaseUrl": "https://ziraai.com/sponsor-request/",
    "DefaultRequestMessage": "Merhaba, ZiraAI üzerinden sponsorluk kodu talep ediyorum.",
    "TokenExpirationHours": 24
  },
  "MobileApp": {
    "AndroidPackageName": "com.ziraai.app",
    "iOSBundleId": "com.ziraai.app"
  }
}
```

### Environment Variables (Production)

**Railway/Deployment Platform**:
```bash
# Güvenlik
SPONSOR_REQUEST_TOKEN_SECRET=YourVerySecureRandomKeyMinimum32CharactersLong!@#$%^&*()

# Base URLs
SPONSOR_REQUEST_DEEPLINK_BASE_URL=https://ziraai.com/sponsor-request/

# Mobile App
ANDROID_PACKAGE_NAME=com.ziraai.app
IOS_BUNDLE_ID=com.ziraai.app
```

### Database Migration

**Tablo Oluşturma**:
```bash
dotnet ef migrations add AddSponsorRequestTable \
  --project DataAccess \
  --startup-project WebAPI \
  --context ProjectDbContext \
  --output-dir Migrations/Pg
```

**Migration Uygulama**:
```bash
dotnet ef database update \
  --project DataAccess \
  --startup-project WebAPI \
  --context ProjectDbContext
```

### Tablo Şeması

```sql
CREATE TABLE "SponsorRequests" (
    "Id" SERIAL PRIMARY KEY,
    "FarmerId" INTEGER NOT NULL,
    "SponsorId" INTEGER NOT NULL,
    "FarmerPhone" VARCHAR(20) NOT NULL,
    "SponsorPhone" VARCHAR(20) NOT NULL,
    "RequestMessage" TEXT,
    "RequestToken" VARCHAR(255) NOT NULL UNIQUE,
    "RequestDate" TIMESTAMP NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "ApprovalDate" TIMESTAMP NULL,
    "ApprovedSubscriptionTierId" INTEGER NULL,
    "ApprovalNotes" TEXT NULL,
    "GeneratedSponsorshipCode" VARCHAR(50) NULL,
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_SponsorRequests_Users_FarmerId" 
        FOREIGN KEY ("FarmerId") REFERENCES "Users"("UserId"),
    CONSTRAINT "FK_SponsorRequests_Users_SponsorId" 
        FOREIGN KEY ("SponsorId") REFERENCES "Users"("UserId"),
    CONSTRAINT "FK_SponsorRequests_SubscriptionTiers" 
        FOREIGN KEY ("ApprovedSubscriptionTierId") REFERENCES "SubscriptionTiers"("Id")
);

-- Indexes
CREATE INDEX "IX_SponsorRequests_FarmerId" ON "SponsorRequests"("FarmerId");
CREATE INDEX "IX_SponsorRequests_SponsorId" ON "SponsorRequests"("SponsorId");
CREATE INDEX "IX_SponsorRequests_Status" ON "SponsorRequests"("Status");
CREATE INDEX "IX_SponsorRequests_RequestToken" ON "SponsorRequests"("RequestToken");
CREATE INDEX "IX_SponsorRequests_RequestDate" ON "SponsorRequests"("RequestDate");
```

---

## Özet

### Temel Özellikler

✅ **WhatsApp Tabanlı İletişim**: Manuel telefon görüşmesi gerektirmez  
✅ **Güvenli Token Sistemi**: HMACSHA256 ile kriptografik güvenlik  
✅ **24 Saat Geçerlilik**: Otomatik expiry ile güvenlik  
✅ **Otomatik Kod Üretimi**: Onaylamada sponsorluk kodu otomatik oluşur  
✅ **Toplu İşlem Desteği**: Birden fazla talebi tek seferde onaylama  
✅ **Duplicate Prevention**: Aynı sponsora birden fazla Pending talep engellenir  
✅ **Deep Link Integration**: Mobil ve web uygulamalarda sorunsuz çalışır

### Kullanım Akışı (Özet)

1. **Çiftçi** → Sponsor seçer + talep oluşturur
2. **Sistem** → Token + WhatsApp URL üretir
3. **Çiftçi** → WhatsApp'tan mesaj gönderir
4. **Sponsor** → Linke tıklar + talep detaylarını görür
5. **Sponsor** → Talebi onaylar
6. **Sistem** → Otomatik kod üretir
7. **Çiftçi** → Kodu kullanarak analiz yapar

### Teknik Stack

- **Backend**: .NET 9.0, CQRS (MediatR), Entity Framework Core
- **Database**: PostgreSQL
- **Security**: HMACSHA256, JWT Authentication, Role-based Authorization
- **Integration**: WhatsApp API (wa.me), Deep Links
- **Architecture**: Clean Architecture, Repository Pattern, Service Layer

---

**Doküman Versiyonu**: 1.0  
**Son Güncelleme**: 2025-11-16  
**Hazırlayan**: ZiraAI Development Team
