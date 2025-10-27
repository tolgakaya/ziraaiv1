# Tier-Based Features Guide

**Date:** 2025-10-26  
**Purpose:** Subscription tier'larına göre feature kısıtlamalarının yönetimi

---

## Tier Yapısı (Veritabanı)

### SubscriptionTiers Tablosu

**Location:** `DataAccess/Concrete/Configurations/SubscriptionTierEntityConfiguration.cs`

| ID | TierName | DisplayName | DailyLimit | MonthlyLimit | MonthlyPrice | Features |
|----|----------|-------------|------------|--------------|--------------|----------|
| 1  | Trial    | Trial       | 1          | 30           | 0 TRY        | Basic    |
| 2  | S        | Small       | 5          | 50           | 99.99 TRY    | Basic    |
| 3  | M        | Medium      | 20         | 200          | 299.99 TRY   | Advanced Analytics |
| 4  | L        | Large       | 50         | 500          | 599.99 TRY   | **Messaging** + API |
| 5  | XL       | Extra Large | 200        | 2000         | 1499.99 TRY  | **Messaging** + Enterprise |

---

## Feature Kısıtlamaları

### 1. Mesajlaşma (Messaging)

**Kısıt:** Sadece **L (ID=4)** ve **XL (ID=5)** tier'lar mesajlaşabilir

**Konum:** `Business/Services/Sponsorship/AnalysisMessagingService.cs:67-68`

**Kod:**
```csharp
// Sadece L, XL tier'larında mesajlaşma var (L=3, XL=4)
// M tier'da mesajlaşma yok çünkü çiftçi profili anonim
if (purchase.SubscriptionTierId >= 3) // L=3, XL=4
{
    return true;
}
```

**❗ DİKKAT:** Kod yorumunda "L=3, XL=4" yazıyor ama gerçekte:
- **L = ID 4** (veritabanında)
- **XL = ID 5** (veritabanında)

**Kontrol edilen yerler:**
1. `CanSendMessageAsync(int sponsorId)` - Genel mesajlaşma yetkisi
2. `CanSendMessageForAnalysisAsync(...)` - Analiz bazlı mesajlaşma
3. `SendMessageAsync(...)` - Mesaj gönderme
4. `SendVoiceMessageCommand` - Sesli mesaj gönderme
5. `SendMessageWithAttachmentCommand` - Eklentili mesaj gönderme

---

### 2. Sesli Mesaj (Voice Messages)

**Kısıt:** Sadece **XL (ID=5)** tier - Premium feature

**Konum:** `Business/Handlers/AnalysisMessages/Commands/SendVoiceMessageCommand.cs:18-21`

**Kod:**
```csharp
/// <summary>
/// Send voice message (XL tier only - premium feature)
/// </summary>
public class SendVoiceMessageCommand : IRequest<IDataResult<AnalysisMessageDto>>
```

**Kontrol:** `IMessagingFeatureService.ValidateFeatureAccessAsync("VoiceMessages", ...)`

---

### 3. Eklentiler (Attachments)

**Kısıt:** Tier bazlı validasyon

**Konum:** `Business/Handlers/AnalysisMessages/Commands/SendMessageWithAttachmentCommand.cs:85-90`

**Kod:**
```csharp
// Validate attachments based on ANALYSIS tier
var validationResult = await _attachmentValidation.ValidateAttachmentsAsync(
    request.Attachments,
    request.PlantAnalysisId);
```

**Service:** `IAttachmentValidationService.ValidateAttachmentsAsync(...)`

---

### 4. Logo Görünürlüğü (Logo Visibility)

**Kısıt:** M tier'da farmer anonim (logo gösterilmez)

**Konum:** `Business/Services/Messaging/MessagingFeatureService.cs`

**Tier Mapping:**
- **Trial, S:** Logo yok
- **M:** Logo yok (anonim çiftçi)
- **L, XL:** Logo var

---

### 5. Gelişmiş Analitik (Advanced Analytics)

**Kısıt:** M, L, XL tier'lar

**Veritabanı Field:** `AdvancedAnalytics` (boolean)

| Tier | AdvancedAnalytics |
|------|-------------------|
| Trial | false |
| S | false |
| M | **true** |
| L | **true** |
| XL | **true** |

---

### 6. API Erişimi (API Access)

**Kısıt:** L, XL tier'lar

**Veritabanı Field:** `ApiAccess` (boolean)

| Tier | ApiAccess |
|------|-----------|
| Trial | false |
| S | false |
| M | false |
| L | **true** |
| XL | **true** |

---

### 7. Öncelikli Destek (Priority Support)

**Kısıt:** L, XL tier'lar

**Veritabanı Field:** `PrioritySupport` (boolean)

| Tier | PrioritySupport | ResponseTimeHours |
|------|----------------|-------------------|
| Trial | false | 72 |
| S | false | 48 |
| M | false | 24 |
| L | **true** | 12 |
| XL | **true** | 6 |

---

## Feature Kontrolü Nasıl Yapılır?

### Yöntem 1: Tier ID Kontrolü (Messaging için)

**Kullanım:**
```csharp
// Sadece L (4) ve XL (5) tier'lar için
if (purchase.SubscriptionTierId >= 4) // L=4, XL=5
{
    // Messaging allowed
}
```

**Örnek:** `AnalysisMessagingService.CanSendMessageAsync`

---

### Yöntem 2: Boolean Field Kontrolü

**Kullanım:**
```csharp
var tier = await _tierRepository.GetAsync(t => t.Id == tierID);

if (tier.AdvancedAnalytics)
{
    // Advanced analytics allowed
}

if (tier.ApiAccess)
{
    // API access allowed
}

if (tier.PrioritySupport)
{
    // Priority support allowed
}
```

---

### Yöntem 3: AdditionalFeatures JSON Kontrolü

**Veritabanı:** `AdditionalFeatures` field (JSON string)

**Örnek Data (L Tier):**
```json
[
    "Premium plant analysis with AI insights",
    "All notification channels",
    "Custom reports",
    "Full historical data",
    "Full API access",
    "Priority support",
    "Export capabilities"
]
```

**Kullanım:**
```csharp
var features = JsonConvert.DeserializeObject<List<string>>(tier.AdditionalFeatures);

if (features.Contains("Export capabilities"))
{
    // Export allowed
}
```

---

### Yöntem 4: MessagingFeatureService (Analysis-Based)

**Kullanım:**
```csharp
var featureValidation = await _messagingFeatureService.ValidateFeatureAccessAsync(
    "VoiceMessages",      // Feature name
    plantAnalysisId,      // Analysis to check
    fileSize,             // Optional: file size for validation
    duration              // Optional: duration for voice messages
);

if (!featureValidation.Success)
{
    return new ErrorDataResult<T>(featureValidation.Message);
}
```

**Service Location:** `Business/Services/Messaging/MessagingFeatureService.cs`

**Feature Types:**
- `"VoiceMessages"` - XL tier only
- `"Attachments"` - Tier-based size limits
- `"Messaging"` - L, XL tiers

---

## Tier Kısıtlarını Değiştirme

### 1. Mesajlaşma Tier Değiştirme

**Senaryo:** M tier'a da mesajlaşma hakkı vermek

**Adımlar:**

**A) Kod Değişikliği:**

`Business/Services/Sponsorship/AnalysisMessagingService.cs:67`
```csharp
// ÖNCE (Sadece L, XL):
if (purchase.SubscriptionTierId >= 4) // L=4, XL=5

// SONRA (M, L, XL):
if (purchase.SubscriptionTierId >= 3) // M=3, L=4, XL=5
```

**B) Yorumları Güncelle:**
```csharp
// Sponsor'un M, L veya XL paketi satın almış olması gerekiyor (mesajlaşma için)
// M, L, XL tier'larında mesajlaşma var (M=3, L=4, XL=5)
if (purchase.SubscriptionTierId >= 3) // M=3, L=4, XL=5
```

**C) Build ve Test:**
```bash
dotnet build
# Test with M tier sponsor
```

---

### 2. Yeni Feature Eklemek

**Senaryo:** "Smart Link" feature'ı sadece XL tier'a vermek

**Adımlar:**

**A) Entity Güncellemesi (Optional):**

`Entities/Concrete/SubscriptionTier.cs`
```csharp
public class SubscriptionTier : IEntity
{
    // Existing fields...
    
    // NEW: Smart Link feature (XL only)
    public bool SmartLinkAccess { get; set; }
}
```

**B) Migration Oluştur:**
```bash
dotnet ef migrations add AddSmartLinkFeature --project DataAccess --startup-project WebAPI
```

**C) Seed Data Güncelle:**

`DataAccess/Concrete/Configurations/SubscriptionTierEntityConfiguration.cs`
```csharp
new SubscriptionTier
{
    Id = 5,
    TierName = "XL",
    // ... existing fields
    SmartLinkAccess = true, // NEW
}
```

**D) Service'de Kontrol Et:**
```csharp
public async Task<bool> CanUseSmartLinkAsync(int sponsorId)
{
    var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);
    
    if (profile?.SponsorshipPurchases != null)
    {
        foreach (var purchase in profile.SponsorshipPurchases)
        {
            var tier = await _tierRepository.GetAsync(t => t.Id == purchase.SubscriptionTierId);
            
            if (tier?.SmartLinkAccess == true)
            {
                return true;
            }
        }
    }
    
    return false;
}
```

---

### 3. AdditionalFeatures JSON'a Feature Eklemek

**Senaryo:** L tier'a "Custom Branding" eklemek

**Adımlar:**

**A) Veritabanında Manuel Güncelleme (SQL):**
```sql
UPDATE "SubscriptionTiers"
SET "AdditionalFeatures" = jsonb_insert(
    "AdditionalFeatures"::jsonb,
    '{7}',
    '"Custom Branding"'
)::text
WHERE "Id" = 4; -- L tier
```

**VEYA**

**B) Seed Data Güncelleme (Yeni Migration):**

`DataAccess/Concrete/Configurations/SubscriptionTierEntityConfiguration.cs`
```csharp
new SubscriptionTier
{
    Id = 4,
    TierName = "L",
    // ...
    AdditionalFeatures = JsonConvert.SerializeObject(new List<string> 
    { 
        "Premium plant analysis with AI insights",
        "All notification channels",
        "Custom reports",
        "Full historical data",
        "Full API access",
        "Priority support",
        "Export capabilities",
        "Custom Branding" // NEW
    }),
}
```

**C) Migration Oluştur ve Uygula:**
```bash
dotnet ef migrations add UpdateLTierFeatures --project DataAccess --startup-project WebAPI
dotnet ef database update --project DataAccess --startup-project WebAPI
```

---

## Feature Kontrol Örnekleri

### Messaging Feature Check

```csharp
// Sponsor mesaj gönderebilir mi?
var canMessage = await _messagingService.CanSendMessageAsync(sponsorId);

if (!canMessage)
{
    return new ErrorResult("Messaging is only available for L and XL tier sponsors");
}
```

### Voice Message Feature Check

```csharp
// XL tier kontrolü
var featureValidation = await _featureService.ValidateFeatureAccessAsync(
    "VoiceMessages",
    plantAnalysisId,
    voiceFile.Length,
    duration);

if (!featureValidation.Success)
{
    return new ErrorDataResult<T>(featureValidation.Message);
}
```

### API Access Check

```csharp
// API erişimi var mı?
var tier = await _tierRepository.GetAsync(t => t.Id == user.SubscriptionTierId);

if (!tier.ApiAccess)
{
    return Unauthorized("API access requires L or XL tier subscription");
}
```

---

## Tier ID Referansı

| ID | TierName | Messaging | VoiceMsg | Attachments | AdvancedAnalytics | ApiAccess | PrioritySupport |
|----|----------|-----------|----------|-------------|-------------------|-----------|-----------------|
| 1  | Trial    | ❌        | ❌       | ❌          | ❌                | ❌        | ❌              |
| 2  | S        | ❌        | ❌       | ❌          | ❌                | ❌        | ❌              |
| 3  | M        | ❌        | ❌       | ✅          | ✅                | ❌        | ❌              |
| 4  | L        | ✅        | ❌       | ✅          | ✅                | ✅        | ✅              |
| 5  | XL       | ✅        | ✅       | ✅          | ✅                | ✅        | ✅              |

---

## Önemli Notlar

### ⚠️ Kod vs Veritabanı ID Uyumsuzluğu

Kodda yorum satırları **yanlış**:
```csharp
// Sadece L, XL tier'larında mesajlaşma var (L=3, XL=4)  ❌ YANLIŞ
if (purchase.SubscriptionTierId >= 3)
```

**Gerçek Değerler:**
- M = ID 3
- L = ID 4  
- XL = ID 5

**Doğrusu:**
```csharp
// Sadece L, XL tier'larında mesajlaşma var (L=4, XL=5)  ✅ DOĞRU
if (purchase.SubscriptionTierId >= 4)
```

### 📍 Feature Kontrol Konumları

1. **Messaging:** `Business/Services/Sponsorship/AnalysisMessagingService.cs`
2. **Voice Messages:** `Business/Services/Messaging/MessagingFeatureService.cs`
3. **Attachments:** `Business/Services/Messaging/AttachmentValidationService.cs`
4. **API Access:** Controller seviyesinde `[Authorize]` attribute ile
5. **Analytics:** Service metodlarında tier kontrolü ile

### 🔄 Değişiklik Sonrası Checklist

- [ ] Kod değişikliği yapıldı
- [ ] Migration oluşturuldu (varsa)
- [ ] Veritabanı güncellendi
- [ ] Unit testler yazıldı/güncellendi
- [ ] Build başarılı
- [ ] Manuel test yapıldı (her tier için)
- [ ] Dokümantasyon güncellendi
- [ ] Commit ve push yapıldı

---

## Sorgu Örnekleri

### Tier Bilgilerini Görüntüleme

```csharp
var tiers = await _tierRepository.GetListAsync();

foreach (var tier in tiers.OrderBy(t => t.Id))
{
    Console.WriteLine($"{tier.Id}: {tier.TierName} ({tier.DisplayName})");
    Console.WriteLine($"  Messaging: {(tier.Id >= 4 ? "YES" : "NO")}");
    Console.WriteLine($"  Voice: {(tier.Id >= 5 ? "YES" : "NO")}");
    Console.WriteLine($"  API: {tier.ApiAccess}");
    Console.WriteLine($"  Analytics: {tier.AdvancedAnalytics}");
}
```

### Sponsor'un Tier'ını Bulma

```csharp
var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);

if (profile?.SponsorshipPurchases != null)
{
    foreach (var purchase in profile.SponsorshipPurchases)
    {
        var tier = await _tierRepository.GetAsync(t => t.Id == purchase.SubscriptionTierId);
        Console.WriteLine($"Purchase: {purchase.Id}, Tier: {tier.TierName}");
    }
}
```

---

**Son Güncelleme:** 2025-10-26  
**İlgili Dosyalar:**
- `Entities/Concrete/SubscriptionTier.cs`
- `DataAccess/Concrete/Configurations/SubscriptionTierEntityConfiguration.cs`
- `Business/Services/Sponsorship/AnalysisMessagingService.cs`
- `Business/Services/Messaging/MessagingFeatureService.cs`
