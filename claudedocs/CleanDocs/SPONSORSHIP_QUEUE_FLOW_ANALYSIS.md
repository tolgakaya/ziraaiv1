# Sponsorship Queue Flow - Uçtan Uca Detaylı Analiz

**Tarih**: 2025-11-23
**Senaryo**: Mevcut aktif subscription'ı olan bir farmer'ın yeni sponsorship code kullanması

## 📋 Özet

Mevcut aktif subscription'ı olan bir farmer yeni bir sponsorship code kullandığında:
1. ✅ **Code hemen "IsUsed = true" olarak işaretlenir**
2. ✅ **Yeni subscription "Pending" (beklemede) statüsünde oluşturulur**
3. ✅ **Eski subscription normal şekilde kullanılmaya devam eder**
4. ✅ **Eski subscription süresi dolduğunda otomatik olarak bekleyen subscription aktif olur**

## 🔍 Uçtan Uca Akış Analizi

### 1️⃣ Redeem İsteği (API Endpoint)

**Endpoint**: `POST /api/sponsorships/redeem`

```csharp
// WebAPI/Controllers/SponsorshipsController.cs
[HttpPost("redeem")]
public async Task<IActionResult> RedeemCode([FromBody] RedeemSponsorshipCodeCommand command)
{
    command.UserId = userId;
    command.UserEmail = email;
    command.UserFullName = fullName;

    var result = await Mediator.Send(command);
    return Ok(result);
}
```

### 2️⃣ MediatR Command Handler

**Handler**: `RedeemSponsorshipCodeCommandHandler`
**Dosya**: `Business/Handlers/Sponsorship/Commands/RedeemSponsorshipCodeCommand.cs`

```csharp
public async Task<IDataResult<UserSubscription>> Handle(
    RedeemSponsorshipCodeCommand request,
    CancellationToken cancellationToken)
{
    // Log redemption attempt
    Console.WriteLine($"[SponsorshipRedeem] User {request.UserEmail} attempting to redeem code: {request.Code}");

    // Delegate to SponsorshipService
    var result = await _sponsorshipService.RedeemSponsorshipCodeAsync(request.Code, request.UserId);

    if (result.Success)
        Console.WriteLine($"[SponsorshipRedeem] ✅ Code {request.Code} successfully redeemed");
    else
        Console.WriteLine($"[SponsorshipRedeem] ❌ Failed: {result.Message}");

    return result;
}
```

### 3️⃣ Sponsorship Service - Ana Karar Mantığı

**Servis**: `SponsorshipService`
**Metod**: `RedeemSponsorshipCodeAsync`
**Dosya**: `Business/Services/Sponsorship/SponsorshipService.cs:231-260`

```csharp
public async Task<IDataResult<UserSubscription>> RedeemSponsorshipCodeAsync(string code, int userId)
{
    try
    {
        // 1. Validate code
        var sponsorshipCode = await _sponsorshipCodeRepository.GetUnusedCodeAsync(code);
        if (sponsorshipCode == null)
            return new ErrorDataResult<UserSubscription>("Invalid or expired sponsorship code");

        // 2. Check for active sponsored subscription
        var existingSubscription = await _userSubscriptionRepository
            .GetActiveSubscriptionByUserIdAsync(userId);

        // ⚠️ KRİTİK KARAR NOKTASI
        bool hasActiveSponsorshipOrPaid = existingSubscription != null &&
                                           existingSubscription.IsSponsoredSubscription &&
                                           existingSubscription.QueueStatus == SubscriptionQueueStatus.Active;

        if (hasActiveSponsorshipOrPaid)
        {
            // 🔄 QUEUE PATH: Existing active sponsorship - queue the new one
            return await QueueSponsorship(code, userId, sponsorshipCode, existingSubscription.Id);
        }

        // ✅ DIRECT ACTIVATION PATH: Trial or no active subscription
        return await ActivateSponsorship(code, userId, sponsorshipCode, existingSubscription);
    }
    catch (Exception ex)
    {
        return new ErrorDataResult<UserSubscription>($"Error redeeming sponsorship code: {ex.Message}");
    }
}
```

**Karar Kriterleri**:
```
hasActiveSponsorshipOrPaid = existingSubscription != null
                            AND existingSubscription.IsSponsoredSubscription == true
                            AND existingSubscription.QueueStatus == Active

TRUE → QueueSponsorship() çağrılır (SIRAYLA GİRİŞ)
FALSE → ActivateSponsorship() çağrılır (DOĞRUDAN AKTİVASYON)
```

### 4️⃣ Queue Sponsorship - Sıraya Alma İşlemi

**Metod**: `QueueSponsorship`
**Dosya**: `Business/Services/Sponsorship/SponsorshipService.cs:265-317`

```csharp
private async Task<IDataResult<UserSubscription>> QueueSponsorship(
    string code,
    int userId,
    SponsorshipCode sponsorshipCode,
    int previousSponsorshipId)
{
    try
    {
        var tier = await _subscriptionTierRepository.GetAsync(t => t.Id == sponsorshipCode.SubscriptionTierId);
        if (tier == null)
            return new ErrorDataResult<UserSubscription>("Subscription tier not found");

        // ✨ YENİ SUBSCRIPTION OLUŞTUR - PENDING DURUMUNDA
        var queuedSubscription = new UserSubscription
        {
            UserId = userId,
            SubscriptionTierId = sponsorshipCode.SubscriptionTierId,

            // ⚠️ QUEUE STATÜSÜ - PENDING
            QueueStatus = SubscriptionQueueStatus.Pending,  // 🔴 BEKLEME DURUMU
            QueuedDate = DateTime.Now,
            PreviousSponsorshipId = previousSponsorshipId,  // 🔗 Önceki subscription referansı

            // ⚠️ AKTIF DEĞİL (henüz kullanılamaz)
            IsActive = false,
            Status = "Pending",

            AutoRenew = false,
            PaymentMethod = "Sponsorship",
            PaymentReference = code,
            PaidAmount = 0,
            Currency = tier.Currency,
            CurrentDailyUsage = 0,
            CurrentMonthlyUsage = 0,
            IsTrialSubscription = false,
            IsSponsoredSubscription = true,
            SponsorshipCodeId = sponsorshipCode.Id,
            SponsorId = sponsorshipCode.SponsorId,
            SponsorshipNotes = $"Queued - Redeemed code: {code}",
            CreatedDate = DateTime.Now
        };

        _userSubscriptionRepository.Add(queuedSubscription);
        await _userSubscriptionRepository.SaveChangesAsync();

        // ⚠️ CODE HEMEN "USED" OLARAK İŞARETLENİR
        await _sponsorshipCodeRepository.MarkAsUsedAsync(code, userId, queuedSubscription.Id);

        Console.WriteLine($"[SponsorshipQueue] ✅ Sponsorship queued for user {userId}. " +
                         $"Will activate when subscription {previousSponsorshipId} expires.");

        return new SuccessDataResult<UserSubscription>(queuedSubscription,
            "Sponsorluk kodunuz sıraya alındı. Mevcut sponsorluk bittiğinde otomatik aktif olacak.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SponsorshipQueue] ❌ Error queueing sponsorship: {ex.Message}");
        return new ErrorDataResult<UserSubscription>($"Error queueing sponsorship: {ex.Message}");
    }
}
```

**Oluşturulan Subscription Özellikleri**:
| Alan | Değer | Açıklama |
|------|-------|----------|
| `QueueStatus` | `Pending` (0) | ⚠️ Bekleme durumunda |
| `IsActive` | `false` | ⚠️ Henüz kullanılamaz |
| `Status` | `"Pending"` | ⚠️ String representation |
| `PreviousSponsorshipId` | `existingSubscription.Id` | 🔗 Hangi subscription'ın bitmesini beklediği |
| `QueuedDate` | `DateTime.Now` | 📅 Sıraya alınma tarihi |
| `StartDate` | `null` veya `DateTime.MinValue` | ⏰ Henüz başlamadı |
| `EndDate` | `null` veya `DateTime.MinValue` | ⏰ Henüz belirlenmedi |

### 5️⃣ Mark Code As Used - Code İşaretleme

**Metod**: `MarkAsUsedAsync`
**Dosya**: `DataAccess/Concrete/EntityFramework/SponsorshipCodeRepository.cs:106-132`

```csharp
public async Task<bool> MarkAsUsedAsync(string code, int userId, int subscriptionId)
{
    var sponsorshipCode = await GetUnusedCodeAsync(code);
    if (sponsorshipCode == null)
        return false;

    // ⚠️ CODE HEMEN "USED" OLARAK İŞARETLENİR
    sponsorshipCode.IsUsed = true;
    sponsorshipCode.UsedByUserId = userId;
    sponsorshipCode.UsedDate = DateTime.Now;
    sponsorshipCode.CreatedSubscriptionId = subscriptionId;  // Queued subscription ID

    Context.SponsorshipCodes.Update(sponsorshipCode);
    await Context.SaveChangesAsync();

    // Update the purchase's used count
    var purchase = await Context.SponsorshipPurchases
        .FirstOrDefaultAsync(p => p.Id == sponsorshipCode.SponsorshipPurchaseId);
    if (purchase != null)
    {
        purchase.CodesUsed = await GetUsedCountByPurchaseAsync(purchase.Id);
        purchase.UpdatedDate = DateTime.Now;
        Context.SponsorshipPurchases.Update(purchase);
        await Context.SaveChangesAsync();
    }

    return true;
}
```

**⚠️ ÖNEMLİ NOT**:
- Code hemen `IsUsed = true` olur
- Subscription henüz aktif olmasa bile code kullanılmış sayılır
- `CreatedSubscriptionId` queued (pending) subscription'ın ID'sini gösterir
- Sponsor dashboard'da bu code "Used" olarak görünür

### 6️⃣ Queue Activation - Otomatik Aktivasyon

#### 6.1. Event-Driven Activation Trigger

**Servis**: `SubscriptionValidationService`
**Metod**: `ValidateAndLogUsageAsync`
**Dosya**: `Business/Services/Subscription/SubscriptionValidationService.cs:282-328`

```csharp
public async Task<IResult> ValidateAndLogUsageAsync(int userId, string endpoint, string method)
{
    var correlationId = Guid.NewGuid().ToString();

    _logger.LogInformation("[USAGE_VALIDATION_START] UserId: {UserId}, CorrelationId: {CorrelationId}, Endpoint: {Endpoint}, Method: {Method}",
        userId, correlationId, endpoint, method);

    try
    {
        // ✨ EVENT-DRIVEN QUEUE ACTIVATION
        // Her API call'da otomatik olarak expired subscription'ları kontrol eder
        await ProcessExpiredSubscriptionsAsync();

        var statusResult = await CheckSubscriptionStatusAsync(userId);

        // ... rest of validation logic
    }
    catch (Exception ex)
    {
        // error handling
    }
}
```

**Tetiklenme Zamanları**:
- ✅ Her plant analysis request öncesi
- ✅ Her subscription validation sırasında
- ✅ Farmer herhangi bir API endpoint'i çağırdığında
- ❌ Scheduled job DEĞİL (event-driven)

#### 6.2. Process Expired Subscriptions

**Metod**: `ProcessExpiredSubscriptionsAsync`
**Dosya**: `Business/Services/Subscription/SubscriptionValidationService.cs:489-513`

```csharp
public async Task ProcessExpiredSubscriptionsAsync()
{
    // Use DateTime.Now instead of DateTime.UtcNow (PostgreSQL compatibility)
    var now = DateTime.Now;

    // 1️⃣ Find expired active subscriptions
    var expiredSubscriptions = await _userSubscriptionRepository.GetListAsync(
        s => s.IsActive && s.EndDate <= now);

    var expiredList = expiredSubscriptions.ToList();

    // 2️⃣ Mark them as expired
    foreach (var subscription in expiredList)
    {
        subscription.IsActive = false;
        subscription.QueueStatus = SubscriptionQueueStatus.Expired;
        subscription.Status = "Expired";
        subscription.UpdatedDate = now;

        _userSubscriptionRepository.Update(subscription);
    }

    await _userSubscriptionRepository.SaveChangesAsync();

    // 3️⃣ Event-driven queue activation: activate queued sponsorships
    await ActivateQueuedSponsorshipsAsync(expiredList);
}
```

#### 6.3. Activate Queued Sponsorships

**Metod**: `ActivateQueuedSponsorshipsAsync`
**Dosya**: `Business/Services/Subscription/SubscriptionValidationService.cs:518-553`

```csharp
private async Task ActivateQueuedSponsorshipsAsync(List<UserSubscription> expiredSubscriptions)
{
    foreach (var expired in expiredSubscriptions)
    {
        // Only process sponsored subscriptions
        if (!expired.IsSponsoredSubscription) continue;

        // ⚠️ Find queued sponsorship waiting for this one
        var queued = await _userSubscriptionRepository.GetAsync(s =>
            s.QueueStatus == SubscriptionQueueStatus.Pending &&
            s.PreviousSponsorshipId == expired.Id);

        if (queued != null)
        {
            _logger.LogInformation("🔄 [SponsorshipQueue] Activating queued sponsorship {QueuedId} for user {UserId} (previous: {ExpiredId})",
                queued.Id, queued.UserId, expired.Id);

            // ✨ ACTIVATE THE QUEUED SUBSCRIPTION
            queued.QueueStatus = SubscriptionQueueStatus.Active;  // 🟢 PENDING → ACTIVE
            queued.ActivatedDate = DateTime.Now;
            queued.StartDate = DateTime.Now;
            queued.EndDate = DateTime.Now.AddDays(30);  // Default 30 days (tier'e göre değişebilir)
            queued.IsActive = true;  // 🟢 Artık kullanılabilir
            queued.Status = "Active";
            queued.PreviousSponsorshipId = null;  // Clear queue reference
            queued.UpdatedDate = DateTime.Now;

            _userSubscriptionRepository.Update(queued);

            _logger.LogInformation("✅ [SponsorshipQueue] Activated sponsorship {Id} for user {UserId}",
                queued.Id, queued.UserId);
        }
    }

    await _userSubscriptionRepository.SaveChangesAsync();
}
```

## 📊 Subscription Status Akış Diyagramı

```
┌──────────────────────────────────────────────────────────────────┐
│ FARMER: Mevcut Aktif Sponsorship'i Var                           │
│ - UserSubscription #1: IsActive=true, QueueStatus=Active         │
│ - SponsorshipCode: AGRI-2024-ABC (kullanılmış)                   │
└──────────────────────────────────────────────────────────────────┘
                              │
                              │ Farmer yeni code kullanıyor
                              │ POST /api/sponsorships/redeem
                              │ Code: AGRI-2024-XYZ
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│ REDEEM SPONSORSHIP CODE                                          │
│ RedeemSponsorshipCodeAsync(AGRI-2024-XYZ, userId)               │
│                                                                  │
│ Karar: hasActiveSponsorshipOrPaid?                              │
│   existingSubscription != null                                  │
│   AND existingSubscription.IsSponsoredSubscription == true      │
│   AND existingSubscription.QueueStatus == Active                │
│                                                                  │
│ ✅ TRUE → QueueSponsorship() çağrılır                            │
└──────────────────────────────────────────────────────────────────┘
                              │
                              │ QueueSponsorship()
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│ YENİ SUBSCRIPTION OLUŞTURULDU (PENDING)                          │
│                                                                  │
│ UserSubscription #2:                                             │
│   - QueueStatus: Pending (0) ⏳                                  │
│   - IsActive: false ❌                                           │
│   - Status: "Pending"                                            │
│   - PreviousSponsorshipId: 1 (Subscription #1'in ID'si)         │
│   - QueuedDate: 2025-11-23 10:30:00                             │
│   - StartDate: null                                              │
│   - EndDate: null                                                │
│   - SponsorshipCodeId: [AGRI-2024-XYZ'nin ID'si]               │
│                                                                  │
│ SponsorshipCode: AGRI-2024-XYZ                                   │
│   - IsUsed: true ✅ (HEMEN İŞARETLENDİ!)                         │
│   - UsedByUserId: [farmer user ID]                              │
│   - UsedDate: 2025-11-23 10:30:00                               │
│   - CreatedSubscriptionId: 2 (Subscription #2'nin ID'si)        │
└──────────────────────────────────────────────────────────────────┘
                              │
                              │ Response to Farmer:
                              │ "Sponsorluk kodunuz sıraya alındı.
                              │  Mevcut sponsorluk bittiğinde otomatik aktif olacak."
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│ FARMER MEVCUT SUBSCRIPTION'I KULLANMAYA DEVAM EDER              │
│                                                                  │
│ UserSubscription #1: (HALA ACTIVE)                              │
│   - QueueStatus: Active (1) 🟢                                   │
│   - IsActive: true ✅                                            │
│   - Status: "Active"                                             │
│   - EndDate: 2025-11-30 23:59:59                                │
│                                                                  │
│ Farmer her API call yaptığında:                                  │
│   ✅ Subscription #1 kontrol edilir                              │
│   ✅ Quota'dan düşülür (daily/monthly usage)                     │
│   ✅ PlantAnalysis request'leri başarılı                         │
└──────────────────────────────────────────────────────────────────┘
                              │
                              │ Zaman geçiyor...
                              │ 2025-11-30 23:59:59 → geçti
                              │
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│ EVENT-DRIVEN ACTIVATION                                          │
│                                                                  │
│ Farmer yeni bir API call yaptı (örn: PlantAnalysis)             │
│ ValidateAndLogUsageAsync() tetiklendi                           │
│   ↓                                                              │
│ ProcessExpiredSubscriptionsAsync() otomatik çalıştı             │
│   ↓                                                              │
│ Query: IsActive=true AND EndDate <= NOW                         │
│   ✅ Subscription #1 bulundu (expired)                           │
│   ↓                                                              │
│ Subscription #1 güncellendi:                                     │
│   - IsActive: false ❌                                           │
│   - QueueStatus: Expired (2) 🔴                                  │
│   - Status: "Expired"                                            │
│   ↓                                                              │
│ ActivateQueuedSponsorshipsAsync([Subscription #1])              │
│   ↓                                                              │
│ Query: QueueStatus=Pending AND PreviousSponsorshipId=1          │
│   ✅ Subscription #2 bulundu (queued)                            │
└──────────────────────────────────────────────────────────────────┘
                              │
                              │ Subscription #2 aktive ediliyor
                              ▼
┌──────────────────────────────────────────────────────────────────┐
│ QUEUED SUBSCRIPTION ACTIVATED!                                   │
│                                                                  │
│ UserSubscription #2:                                             │
│   - QueueStatus: Active (1) 🟢 (PENDING → ACTIVE)               │
│   - IsActive: true ✅ (false → true)                             │
│   - Status: "Active" ("Pending" → "Active")                     │
│   - ActivatedDate: 2025-12-01 00:00:05                          │
│   - StartDate: 2025-12-01 00:00:05                              │
│   - EndDate: 2025-12-31 00:00:05 (30 gün eklendi)               │
│   - PreviousSponsorshipId: null (referans temizlendi)           │
│                                                                  │
│ SponsorshipCode: AGRI-2024-XYZ                                   │
│   - IsUsed: true ✅ (zaten true'ydu, değişmedi)                  │
│                                                                  │
│ Farmer'ın API request'i artık Subscription #2 ile devam eder    │
└──────────────────────────────────────────────────────────────────┘
```

## 🔑 Kritik Noktalar

### 1. Code Durumu (IsUsed)

**⚠️ ÇOK ÖNEMLİ**: Sponsorship code hemen "used" olarak işaretlenir:

| Zaman | Code Durumu | Subscription Durumu | Açıklama |
|-------|-------------|---------------------|----------|
| Redeem öncesi | `IsUsed = false` | Yok | Code henüz kullanılmamış |
| Redeem anı (queue) | `IsUsed = true` ✅ | `QueueStatus = Pending` ⏳ | Code HEMEN kullanılmış olarak işaretlenir |
| Bekleme süresi | `IsUsed = true` ✅ | `QueueStatus = Pending` ⏳ | Code used, subscription pending |
| Aktivasyon | `IsUsed = true` ✅ | `QueueStatus = Active` 🟢 | Code used, subscription active |

**Neden böyle?**
- ✅ Aynı code'un birden fazla kez kullanılmasını önler
- ✅ Sponsor dashboard'da doğru "used count" gösterir
- ✅ Code reservation system ile uyumlu
- ✅ Farmer code'u kullandığında hemen commit edilir (rollback riski yok)

### 2. Subscription Lifecycle States

```csharp
public enum SubscriptionQueueStatus
{
    Pending = 0,   // Queued, waiting for activation
    Active = 1,    // Currently active and usable
    Expired = 2,   // Past end date
    Cancelled = 3  // Manually cancelled
}
```

**Olası State Transitions**:
```
┌─────────┐   Redeem with active   ┌─────────┐   ProcessExpired   ┌────────┐
│  (New)  │ ───────────────────►  │ Pending │ ─────────────────► │ Active │
└─────────┘                        └─────────┘                    └────────┘
                                                                        │
                                                                        │ EndDate geçti
                                                                        ▼
                                                                   ┌─────────┐
                                                                   │ Expired │
                                                                   └─────────┘
```

### 3. PreviousSponsorshipId Referansı

```sql
-- Pending subscription'ın hangi subscription'ın bitmesini beklediğini gösterir
SELECT
    us_pending.Id AS PendingSubscriptionId,
    us_pending.QueueStatus AS PendingStatus,
    us_pending.PreviousSponsorshipId AS WaitingForSubscriptionId,
    us_active.EndDate AS ActiveSubscriptionEndDate,
    us_active.IsActive AS ActiveSubscriptionIsActive
FROM UserSubscriptions us_pending
LEFT JOIN UserSubscriptions us_active ON us_active.Id = us_pending.PreviousSponsorshipId
WHERE us_pending.QueueStatus = 0  -- Pending
```

**Activation query**:
```csharp
var queued = await _userSubscriptionRepository.GetAsync(s =>
    s.QueueStatus == SubscriptionQueueStatus.Pending &&
    s.PreviousSponsorshipId == expired.Id);  // 🔗 Hangi subscription expire oldu?
```

### 4. Event-Driven Activation (Scheduled Job DEĞİL)

**❌ YANLIŞ ANLAŞILABİLİR**:
- "Her gece 00:00'da scheduled job çalışır ve queue'daki subscription'ları aktive eder"
- "Hangfire recurring job ile ProcessExpiredSubscriptionsAsync() çalıştırılır"

**✅ DOĞRU**:
- Her API validation sırasında otomatik çalışır
- Farmer API call yaptığında expired subscription'lar hemen kontrol edilir
- Queue activation event-driven'dır (real-time)
- Scheduled job'a gerek yoktur

**Avantajları**:
- ⚡ Anında aktivasyon (farmer ilk request'te aktif olur)
- 🔧 Ek infrastructure gerekmez (no Hangfire recurring job)
- 🎯 Her request'te doğru subscription kullanılır
- 🛡️ Race condition riski minimal (ProcessExpiredSubscriptionsAsync atomic)

## 📈 Örnek Senaryo Timeline

```
📅 2025-11-01 10:00:00
Farmer sponsorship code AGRI-2024-ABC ile subscription aldı
→ UserSubscription #1 (Active, EndDate: 2025-11-30 23:59:59)

📅 2025-11-15 14:30:00
Farmer yeni code AGRI-2024-XYZ kullanıyor (mevcut aktif subscription var)
→ UserSubscription #2 (Pending, PreviousSponsorshipId: 1)
→ SponsorshipCode AGRI-2024-XYZ: IsUsed = true ✅ (HEMEN!)
→ Response: "Sponsorluk kodunuz sıraya alındı"

📅 2025-11-20 09:00:00
Farmer plant analysis request yapıyor
→ ValidateAndLogUsageAsync() çağrılır
→ ProcessExpiredSubscriptionsAsync() çalışır
→ Subscription #1 EndDate kontrol: 2025-11-30 > NOW → Henüz expire olmadı
→ Request Subscription #1 ile devam eder ✅

📅 2025-11-30 23:59:59
Subscription #1'in EndDate geçti (ama henüz API call yok)

📅 2025-12-01 08:15:00
Farmer plant analysis request yapıyor
→ ValidateAndLogUsageAsync() çağrılır
→ ProcessExpiredSubscriptionsAsync() çalışır
→ Query: IsActive=true AND EndDate <= NOW
   ✅ Subscription #1 bulundu (expired)
→ Subscription #1: QueueStatus = Expired, IsActive = false
→ ActivateQueuedSponsorshipsAsync([Subscription #1])
→ Query: QueueStatus=Pending AND PreviousSponsorshipId=1
   ✅ Subscription #2 bulundu
→ Subscription #2: QueueStatus = Active, IsActive = true, StartDate = NOW, EndDate = NOW+30 days
→ Request Subscription #2 ile devam eder ✅
→ Log: "🔄 [SponsorshipQueue] Activating queued sponsorship 2 for user 123"
→ Log: "✅ [SponsorshipQueue] Activated sponsorship 2 for user 123"
```

## 🎯 Test Senaryoları

### Senaryo 1: Aktif Sponsorship Varken Redeem

**Test Adımları**:
```sql
-- 1. Aktif subscription oluştur
INSERT INTO UserSubscriptions (UserId, QueueStatus, IsActive, EndDate, IsSponsoredSubscription)
VALUES (123, 1, true, '2025-12-31', true);

-- 2. Yeni code redeem et
POST /api/sponsorships/redeem
{
  "code": "AGRI-TEST-001"
}

-- 3. Kontroller
SELECT * FROM UserSubscriptions WHERE UserId = 123 ORDER BY Id DESC;
-- Beklenilen:
-- Row 1: QueueStatus=1 (Active), IsActive=true, PreviousSponsorshipId=NULL
-- Row 2: QueueStatus=0 (Pending), IsActive=false, PreviousSponsorshipId=[Row 1 ID]

SELECT * FROM SponsorshipCodes WHERE Code = 'AGRI-TEST-001';
-- Beklenilen: IsUsed=true, UsedDate=NOW, UsedByUserId=123
```

### Senaryo 2: Queue Activation

**Test Adımları**:
```sql
-- 1. Aktif subscription'ın EndDate'ini geçmişe çek
UPDATE UserSubscriptions
SET EndDate = '2025-01-01'
WHERE UserId = 123 AND QueueStatus = 1;

-- 2. API call yap (plant analysis)
POST /api/plant-analysis
{
  "image": "base64...",
  "cropType": "Tomato"
}

-- 3. Log'larda kontrol et
-- Beklenilen log:
-- [SponsorshipQueue] 🔄 Activating queued sponsorship {Id} for user 123
-- [SponsorshipQueue] ✅ Activated sponsorship {Id} for user 123

-- 4. Database kontrol
SELECT * FROM UserSubscriptions WHERE UserId = 123 ORDER BY Id DESC;
-- Beklenilen:
-- Row 1: QueueStatus=2 (Expired), IsActive=false
-- Row 2: QueueStatus=1 (Active), IsActive=true, StartDate=NOW, EndDate=NOW+30 days
```

### Senaryo 3: Code Durumu Kontrolü

**Test Adımları**:
```sql
-- 1. Queue'ya alınmış subscription var
SELECT us.Id, us.QueueStatus, sc.Code, sc.IsUsed
FROM UserSubscriptions us
INNER JOIN SponsorshipCodes sc ON sc.Id = us.SponsorshipCodeId
WHERE us.UserId = 123 AND us.QueueStatus = 0;

-- Beklenilen: IsUsed = true (subscription pending olsa bile)

-- 2. Aynı code'u tekrar kullanmayı dene
POST /api/sponsorships/redeem
{
  "code": "AGRI-TEST-001"
}

-- Beklenilen response:
-- { "success": false, "message": "Invalid or expired sponsorship code" }
```

## 🐛 Potansiyel Edge Case'ler

### 1. Multiple Queue (İçiçe Sıra)

**Durum**: Farmer 3 code birden kullanıyor (2 aktif, 1 pending)

**Mevcut Davranış**:
```csharp
// RedeemSponsorshipCodeAsync() sadece 1 aktif sponsorship kontrolü yapar
bool hasActiveSponsorshipOrPaid = existingSubscription != null &&
                                   existingSubscription.IsSponsoredSubscription &&
                                   existingSubscription.QueueStatus == SubscriptionQueueStatus.Active;
```

**Sorun**:
- Farmer zaten 1 pending subscription'ı varsa
- Yeni code kullandığında yine queue'ya alınır
- Ama `PreviousSponsorshipId` aktif olan subscription'ı gösterir
- 2. ve 3. code'lar aynı anda aktive olmaya çalışabilir

**Çözüm**:
```csharp
// En son queued veya active subscription'ı bul
var latestSubscription = await _userSubscriptionRepository.GetAsync(s =>
    s.UserId == userId &&
    (s.QueueStatus == SubscriptionQueueStatus.Active || s.QueueStatus == SubscriptionQueueStatus.Pending),
    orderBy: q => q.OrderByDescending(s => s.CreatedDate));
```

### 2. Activation Race Condition

**Durum**: Farmer aynı anda 2 API call yapıyor (expire zamanı)

**Risk**:
```
Thread 1: ProcessExpiredSubscriptionsAsync() başladı
Thread 2: ProcessExpiredSubscriptionsAsync() başladı
  ↓
Thread 1: Subscription #1 expired, Subscription #2 activate ediliyor
Thread 2: Subscription #1 expired (cached query), Subscription #2 activate etmeye çalışıyor
  ↓
Potential conflict: Subscription #2 duplicate activation?
```

**Mevcut Koruma**:
- `SaveChangesAsync()` atomic operation
- EF Core optimistic concurrency (RowVersion yoksa sorun olabilir)

**Önerilen Çözüm**:
```csharp
// UserSubscription entity'ye RowVersion ekle
[Timestamp]
public byte[] RowVersion { get; set; }
```

### 3. ProcessExpiredSubscriptionsAsync Performance

**Durum**: Her API call'da çalışıyor

**Risk**:
- High traffic: Binlerce farmer aynı anda request yapıyor
- Her request için tüm expired subscription'lar query'leniyor
- Database load artabilir

**Mevcut Query**:
```csharp
var expiredSubscriptions = await _userSubscriptionRepository.GetListAsync(
    s => s.IsActive && s.EndDate <= now);
```

**Optimizasyon Önerileri**:
1. Index: `CREATE INDEX idx_usersubscriptions_expired ON UserSubscriptions(IsActive, EndDate) WHERE IsActive = true;`
2. Cache: Son 1 dakika içinde expire check yapıldıysa skip et (memory cache)
3. Batch: Sadece current user'ın subscription'ını check et (global scan yerine)

## 📝 İlgili Dosyalar

### Core Business Logic
- `Business/Handlers/Sponsorship/Commands/RedeemSponsorshipCodeCommand.cs` - MediatR handler
- `Business/Services/Sponsorship/SponsorshipService.cs:231-317` - Queue logic
- `Business/Services/Subscription/SubscriptionValidationService.cs:489-553` - Activation logic

### Data Access
- `DataAccess/Concrete/EntityFramework/SponsorshipCodeRepository.cs:106-132` - MarkAsUsedAsync
- `DataAccess/Abstract/IUserSubscriptionRepository.cs` - GetActiveSubscriptionByUserIdAsync

### Entities
- `Entities/Concrete/UserSubscription.cs` - Subscription entity with queue fields
- `Entities/Concrete/SponsorshipCode.cs` - Code entity with usage tracking
- `Entities/Concrete/SubscriptionQueueStatus.cs` - Queue status enum

### Controllers
- `WebAPI/Controllers/SponsorshipsController.cs` - Redeem endpoint

### Documentation
- `claudedocs/SPONSORSHIP_QUEUE_SYSTEM_DESIGN.md` - System design
- `claudedocs/SPONSORSHIP_QUEUE_IMPLEMENTATION_SUMMARY.md` - Implementation summary
- `claudedocs/SPONSORSHIP_QUEUE_TESTING_GUIDE.md` - Testing guide

## 🎓 Öğrenilenler

### 1. Event-Driven Queue Pattern
- ✅ Scheduled job'a gerek yok
- ✅ Real-time activation
- ✅ Her validation'da automatic check
- ⚠️ Performance consideration gerekli (caching, indexing)

### 2. Code State Management
- ✅ Code hemen "used" olarak işaretlenir (subscription pending olsa bile)
- ✅ Duplicate usage önlenir
- ✅ Sponsor analytics doğru çalışır

### 3. Subscription Lifecycle
- Pending → Active → Expired state transitions
- PreviousSponsorshipId ile queue chain
- IsActive flag ile runtime control
- QueueStatus ile state tracking

### 4. PostgreSQL DateTime Handling
- ❌ DateTime.UtcNow kullanma
- ✅ DateTime.Now kullan
- System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)

## 🔗 Referanslar

- [Sponsorship Queue System Design](SPONSORSHIP_QUEUE_SYSTEM_DESIGN.md)
- [Sponsorship Queue Implementation Summary](SPONSORSHIP_QUEUE_IMPLEMENTATION_SUMMARY.md)
- [Sponsorship Queue Testing Guide](SPONSORSHIP_QUEUE_TESTING_GUIDE.md)
- [Admin Queue Control Documentation](AdminOperations/ADMIN_ASSIGN_QUEUE_CONTROL.md)

---

**Son Güncelleme**: 2025-11-23
**Analiz Eden**: Claude Code (Sequential Thinking + Serena MCP)
**Branch**: feature/staging-testing
