# Sponsorship Queue System - Test Rehberi

**Branch**: `feature/sponsorship-improvements`
**Migration**: `AddSponsorshipQueueSystem.sql` ✅ Applied
**Tarih**: 2025-10-08

---

## 🎯 Test Edilen Özellikler

1. **Queue Management**: Aktif sponsorluk varken yeni kod kullanımı
2. **Auto-Activation**: Sponsorluk bitince sıradaki otomatik aktif olur
3. **Sponsor Attribution**: Plant analysis'de aktif sponsor yakalanır
4. **Environment URLs**: Dinamik redemption link'leri

---

## ⚙️ Ön Hazırlık

### 1. Migration Kontrolü

```sql
-- UserSubscriptions yeni kolonlar
SELECT column_name FROM information_schema.columns
WHERE table_name = 'UserSubscriptions'
AND column_name IN ('QueueStatus', 'QueuedDate', 'ActivatedDate', 'PreviousSponsorshipId');

-- PlantAnalyses yeni kolonlar
SELECT column_name FROM information_schema.columns
WHERE table_name = 'PlantAnalyses'
AND column_name IN ('ActiveSponsorshipId', 'SponsorCompanyId');
```

**Beklenen**: 6 satır (her sorgudan 2+4)

### 2. Environment Setup

```bash
# Baseurl ayarı
Development: https://localhost:5001
Staging:     https://ziraai-api-sit.up.railway.app
Production:  https://api.ziraai.com
```

---

## 📝 Test Kullanıcıları Hazırlama

### Sponsor User

```http
POST {{baseUrl}}/api/v1/auth/register
Content-Type: application/json

{
  "firstName": "Sponsor",
  "lastName": "Test",
  "email": "sponsor@test.com",
  "password": "123456",
  "phoneNumber": "+905559876543"
}
```

**Role ekle (SQL)**:
```sql
INSERT INTO UserGroups (UserId, GroupId)
SELECT u.UserId, g.Id
FROM Users u, Groups g
WHERE u.Email = 'sponsor@test.com' AND g.GroupName = 'Sponsor';
```

**Login**:
```http
POST {{baseUrl}}/api/v1/auth/login
Content-Type: application/json

{
  "email": "sponsor@test.com",
  "password": "123456"
}
```

💾 **Token'ı kaydet**: `SPONSOR_TOKEN`

### Farmer User

```http
POST {{baseUrl}}/api/v1/auth/register
Content-Type: application/json

{
  "firstName": "Farmer",
  "lastName": "Test",
  "email": "farmer@test.com",
  "password": "123456",
  "phoneNumber": "+905551234567"
}
```

⚠️ **Otomatik Trial subscription oluşur**

**Login**:
```http
POST {{baseUrl}}/api/v1/auth/login
Content-Type: application/json

{
  "email": "farmer@test.com",
  "password": "123456"
}
```

💾 **Token'ı kaydet**: `FARMER_TOKEN`

---

## 🧪 Test Scenario 1: Trial → Immediate Activation

### 1.1 Trial Subscription Kontrolü

```http
GET {{baseUrl}}/api/v1/subscriptions/my-subscription
Authorization: Bearer {{FARMER_TOKEN}}
```

**Beklenen Response**:
```json
{
  "success": true,
  "data": {
    "id": 1,
    "tierName": "Trial",
    "isActive": true,
    "isTrialSubscription": true,
    "status": "Active"
  }
}
```

### 1.2 Sponsorluk Paketi Satın Al (Sponsor)

```http
POST {{baseUrl}}/api/v1/sponsorship/purchase-package
Authorization: Bearer {{SPONSOR_TOKEN}}
Content-Type: application/json

{
  "subscriptionTierId": 2,
  "quantity": 2,
  "totalAmount": 199.98,
  "paymentReference": "TEST-001",
  "validityDays": 90
}
```

**Beklenen Response**:
```json
{
  "success": true,
  "data": {
    "purchaseId": 1,
    "codesGenerated": 2,
    "codes": [
      {
        "code": "AGRI-XXXXX1",
        "subscriptionTierId": 2
      },
      {
        "code": "AGRI-XXXXX2",
        "subscriptionTierId": 2
      }
    ]
  }
}
```

💾 **İlk kodu kaydet**: `CODE_1 = AGRI-XXXXX1`
💾 **İkinci kodu kaydet**: `CODE_2 = AGRI-XXXXX2`

### 1.3 İlk Kodu Redeem Et (Farmer - Trial)

```http
POST {{baseUrl}}/api/v1/sponsorship/redeem
Authorization: Bearer {{FARMER_TOKEN}}
Content-Type: application/json

{
  "code": "{{CODE_1}}"
}
```

**Beklenen Response**:
```json
{
  "success": true,
  "message": "Sponsorluk aktivasyonu tamamlandı!",
  "data": {
    "id": 2,
    "userId": 10,
    "subscriptionTierId": 2,
    "isActive": true,
    "isSponsoredSubscription": true,
    "startDate": "2025-10-08T10:00:00",
    "endDate": "2025-11-07T10:00:00"
  }
}
```

✅ **Doğrulama 1**: `success: true` ve `message` içinde "aktivasyonu tamamlandı"
✅ **Doğrulama 2**: `isSponsoredSubscription: true`
✅ **Doğrulama 3**: Trial subscription deactive olmalı

### 1.4 Database Kontrolü

```sql
SELECT
  Id,
  SubscriptionTierId,
  IsActive,
  QueueStatus,
  Status,
  IsTrialSubscription,
  IsSponsoredSubscription
FROM UserSubscriptions
WHERE UserId = (SELECT UserId FROM Users WHERE Email = 'farmer@test.com')
ORDER BY CreatedDate DESC;
```

**Beklenen Sonuç**:
| Id | TierId | IsActive | QueueStatus | Status    | IsTrial | IsSponsored |
|----|--------|----------|-------------|-----------|---------|-------------|
| 2  | 2      | true     | 1 (Active)  | Active    | false   | true        |
| 1  | 1      | false    | 2 (Expired) | Upgraded  | true    | false       |

---

## 🧪 Test Scenario 2: Active Sponsorship → Queue

### 2.1 Aktif Sponsorluk Kontrolü

```http
GET {{baseUrl}}/api/v1/subscriptions/my-subscription
Authorization: Bearer {{FARMER_TOKEN}}
```

**Beklenen**: `isSponsoredSubscription: true`, `isActive: true`

### 2.2 İkinci Kodu Redeem Et (Should Queue!)

```http
POST {{baseUrl}}/api/v1/sponsorship/redeem
Authorization: Bearer {{FARMER_TOKEN}}
Content-Type: application/json

{
  "code": "{{CODE_2}}"
}
```

**Beklenen Response**:
```json
{
  "success": true,
  "message": "Sponsorluk kodunuz sıraya alındı. Mevcut sponsorluk bittiğinde otomatik aktif olacak.",
  "data": {
    "id": 3,
    "userId": 10,
    "subscriptionTierId": 2,
    "isActive": false,
    "isSponsoredSubscription": true,
    "status": "Pending"
  }
}
```

✅ **Doğrulama 1**: `message` içinde "sıraya alındı"
✅ **Doğrulama 2**: `isActive: false`
✅ **Doğrulama 3**: `status: "Pending"`

### 2.3 Database Kontrolü - Queue Relationship

```sql
SELECT
  Id,
  SubscriptionTierId,
  QueueStatus,
  IsActive,
  PreviousSponsorshipId,
  QueuedDate,
  ActivatedDate,
  Status
FROM UserSubscriptions
WHERE UserId = (SELECT UserId FROM Users WHERE Email = 'farmer@test.com')
ORDER BY CreatedDate DESC;
```

**Beklenen Sonuç**:
| Id | QueueStatus | IsActive | PreviousSponsorshipId | QueuedDate  | ActivatedDate | Status  |
|----|-------------|----------|-----------------------|-------------|---------------|---------|
| 3  | 0 (Pending) | false    | 2                     | 2025-10-... | null          | Pending |
| 2  | 1 (Active)  | true     | null                  | null        | 2025-10-...   | Active  |

✅ **Kritik**: `PreviousSponsorshipId = 2` (aktif subscription'ın ID'si)

### 2.4 Code Usage Kontrolü

```sql
SELECT Code, IsUsed, UsedByUserId, UserSubscriptionId
FROM SponsorshipCodes
WHERE Code IN ('{{CODE_1}}', '{{CODE_2}}');
```

**Beklenen**: Her iki kod da `IsUsed = true`

---

## 🧪 Test Scenario 3: Auto-Activation

### 3.1 Aktif Sponsorluğu Manuel Expire Et (Test Amaçlı)

```sql
-- ⚠️ SADECE TEST ORTAMINDA!
UPDATE UserSubscriptions
SET EndDate = NOW() - INTERVAL '1 hour',
    QueueStatus = 2  -- Expired
WHERE Id = 2
AND UserId = (SELECT UserId FROM Users WHERE Email = 'farmer@test.com');
```

### 3.2 Otomatik Queue Aktivasyon (Event-Driven)

✅ **YENİ**: `ProcessExpiredSubscriptionsAsync` metodu artık **event-driven olarak otomatik çalışıyor**!

**Nasıl çalışır?**
- Her subscription validation (plant analysis request) sırasında otomatik tetiklenir
- Expired subscription'ları bulur ve marks as expired
- Sıradaki sponsorship'leri otomatik aktive eder
- Hangfire job veya manuel trigger gerekmez

**Test için yapılacak**: Sadece bir plant analysis request yap!

```http
POST {{baseUrl}}/api/v1/plantanalyses/analyze
Authorization: Bearer {{FARMER_TOKEN}}
Content-Type: application/json

{
  "image": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
  "location": "Test Location",
  "notes": "Testing queue activation"
}
```

**Arka planda ne olur?**
1. `ValidateAndLogUsageAsync()` tetiklenir
2. `ProcessExpiredSubscriptionsAsync()` otomatik çalışır
3. ID 2 (expired) → `QueueStatus = 2 (Expired)`
4. ID 3 (queued) → `QueueStatus = 1 (Active)`, `IsActive = true`
5. Request, yeni aktif olan subscription ile devam eder
```

### 3.3 Subscription Kontrolü - Sıradaki Aktif Olmalı

```http
GET {{baseUrl}}/api/v1/subscriptions/my-subscription
Authorization: Bearer {{FARMER_TOKEN}}
```

**Beklenen Response**:
```json
{
  "success": true,
  "data": {
    "id": 3,
    "subscriptionTierId": 2,
    "isActive": true,
    "status": "Active"
  }
}
```

✅ **Doğrulama**: ID 3 (sıradaki) şimdi aktif

### 3.4 Database Kontrolü - Auto-Activation

```sql
SELECT
  Id,
  QueueStatus,
  IsActive,
  ActivatedDate,
  PreviousSponsorshipId,
  StartDate,
  EndDate,
  Status
FROM UserSubscriptions
WHERE UserId = (SELECT UserId FROM Users WHERE Email = 'farmer@test.com')
ORDER BY CreatedDate DESC;
```

**Beklenen Sonuç**:
| Id | QueueStatus | IsActive | ActivatedDate | PreviousSponsorshipId | Status  |
|----|-------------|----------|---------------|-----------------------|---------|
| 3  | 1 (Active)  | true     | 2025-10-...   | null                  | Active  |
| 2  | 2 (Expired) | false    | 2025-10-...   | null                  | Expired |

✅ **Kritik**:
- ID 3: `QueueStatus` changed from `0` → `1`
- ID 3: `ActivatedDate` şimdi dolu
- ID 3: `PreviousSponsorshipId` cleared (null)
- ID 2: `QueueStatus` = `2` (Expired)

### 3.5 Console Log Kontrolü

Application logs'da arayın:
```
🔄 [SponsorshipQueue] Activating queued sponsorship 3 for user 10 (previous: 2)
✅ [SponsorshipQueue] Activated sponsorship 3 for user 10
```

---

## 🧪 Test Scenario 4: Sponsor Attribution

### 4.1 Aktif Sponsored Subscription Kontrolü

```http
GET {{baseUrl}}/api/v1/subscriptions/my-subscription
Authorization: Bearer {{FARMER_TOKEN}}
```

**Beklenen**: `isSponsoredSubscription: true`, `isActive: true`

### 4.2 Plant Analysis Oluştur

```http
POST {{baseUrl}}/api/v1/plantanalyses/analyze
Authorization: Bearer {{FARMER_TOKEN}}
Content-Type: application/json

{
  "image": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
  "location": "Ankara, Turkey",
  "notes": "Test sponsor attribution"
}
```

**Beklenen Response**:
```json
{
  "success": true,
  "data": {
    "id": 100,
    "status": "Completed",
    "imageUrl": "https://..."
  }
}
```

💾 **Analysis ID'yi kaydet**: `ANALYSIS_ID = 100`

### 4.3 Analysis Detail Kontrolü

```http
GET {{baseUrl}}/api/v1/plantanalyses/{{ANALYSIS_ID}}/detail
Authorization: Bearer {{FARMER_TOKEN}}
```

**Beklenen Response**:
```json
{
  "success": true,
  "data": {
    "id": 100,
    "userId": 10,
    "imageUrl": "https://...",
    "sponsorId": "5"
  }
}
```

⚠️ **Not**: `activeSponsorshipId` ve `sponsorCompanyId` backend'de capture ediliyor ama DTO'da yok. Database'de kontrol et.

### 4.4 Database Kontrolü - Attribution

```sql
SELECT
  Id,
  UserId,
  ActiveSponsorshipId,
  SponsorCompanyId,
  SponsorId,
  CreatedDate
FROM PlantAnalyses
WHERE Id = {{ANALYSIS_ID}};
```

**Beklenen**:
- `ActiveSponsorshipId`: Aktif subscription ID'si (3)
- `SponsorCompanyId`: Sponsor user ID'si
- `SponsorId`: Sponsor user ID string (legacy)

### 4.5 Console Log Kontrolü

```
[SponsorAttribution] Analysis 100 attributed to sponsor 5 (subscription 3)
```

---

## 📊 Verification Queries

### Tüm Subscriptions

```sql
SELECT
  Id,
  UserId,
  SubscriptionTierId,
  QueueStatus,
  IsActive,
  IsSponsoredSubscription,
  PreviousSponsorshipId,
  QueuedDate,
  ActivatedDate,
  StartDate,
  EndDate,
  Status
FROM UserSubscriptions
WHERE UserId = (SELECT UserId FROM Users WHERE Email = 'farmer@test.com')
ORDER BY CreatedDate DESC;
```

### Kod Kullanımları

```sql
SELECT
  c.Code,
  c.IsUsed,
  c.UsedDate,
  c.UserSubscriptionId,
  u.Email AS FarmerEmail,
  s.QueueStatus,
  s.IsActive,
  s.Status
FROM SponsorshipCodes c
LEFT JOIN Users u ON c.UsedByUserId = u.UserId
LEFT JOIN UserSubscriptions s ON c.UserSubscriptionId = s.Id
WHERE c.SponsorId = (SELECT UserId FROM Users WHERE Email = 'sponsor@test.com')
ORDER BY c.CreatedDate DESC;
```

### Plant Analysis Attribution

```sql
SELECT
  pa.Id,
  pa.UserId,
  pa.ActiveSponsorshipId,
  pa.SponsorCompanyId,
  us.SponsorId,
  pa.CreatedDate
FROM PlantAnalyses pa
LEFT JOIN UserSubscriptions us ON pa.ActiveSponsorshipId = us.Id
WHERE pa.UserId = (SELECT UserId FROM Users WHERE Email = 'farmer@test.com')
ORDER BY pa.CreatedDate DESC
LIMIT 5;
```

---

## 🔧 Troubleshooting

### Sorun 1: Kod Sıraya Girmiyor (Hemen Aktif Oluyor)

**Belirti**: İkinci kod redeem'de sıraya girmek yerine hemen aktif oluyor

**Olası Sebepler**:
- Aktif subscription sponsored değil (ücretli)
- `IsSponsoredSubscription = false`

**Kontrol**:
```sql
SELECT Id, IsSponsoredSubscription, QueueStatus, IsActive
FROM UserSubscriptions
WHERE UserId = (SELECT UserId FROM Users WHERE Email = 'farmer@test.com')
AND IsActive = true;
```

**Çözüm**: İlk subscription'ın `IsSponsoredSubscription = true` olduğundan emin ol.

---

### Sorun 2: Otomatik Aktivasyon Çalışmıyor

**Belirti**: Sıradaki subscription pending'de kalıyor

**Olası Sebepler**:
- Background job çalışmıyor
- `PreviousSponsorshipId` yanlış

**Kontrol**:
```sql
SELECT
  expired.Id AS ExpiredId,
  queued.Id AS QueuedId,
  queued.PreviousSponsorshipId,
  expired.EndDate
FROM UserSubscriptions expired
INNER JOIN UserSubscriptions queued ON queued.PreviousSponsorshipId = expired.Id
WHERE expired.QueueStatus = 2  -- Expired
AND queued.QueueStatus = 0;    -- Pending
```

**Manuel Aktivasyon** (test only):
```sql
UPDATE UserSubscriptions
SET QueueStatus = 1,
    IsActive = true,
    ActivatedDate = NOW(),
    StartDate = NOW(),
    EndDate = NOW() + INTERVAL '30 days',
    Status = 'Active',
    PreviousSponsorshipId = null
WHERE Id = 3;  -- Queued subscription ID
```

---

### Sorun 3: Sponsor Attribution Null

**Belirti**: `ActiveSponsorshipId` veya `SponsorCompanyId` null

**Olası Sebepler**:
- Aktif sponsored subscription yok
- `CaptureActiveSponsorAsync` hata verdi

**Kontrol**:
```sql
SELECT * FROM UserSubscriptions
WHERE UserId = (SELECT UserId FROM Users WHERE Email = 'farmer@test.com')
AND IsSponsoredSubscription = true
AND QueueStatus = 1
AND IsActive = true
AND EndDate > NOW();
```

**Logları kontrol et**:
```
[SponsorAttribution] Error capturing sponsor for analysis: {error}
```

---

## ✅ Success Checklist

- [ ] **Scenario 1**: Trial user immediate activation ✓
- [ ] **Scenario 2**: Active user code queued ✓
- [ ] **Scenario 3**: Queued auto-activated on expiry ✓
- [ ] **Scenario 4**: Plant analysis captures sponsor ✓
- [ ] Database: Tüm alanlar doğru populate edildi
- [ ] Logs: Beklenen mesajlar görüldü
- [ ] Error yok: Application logs temiz
- [ ] Environment URLs: Dinamik linkler çalışıyor

---

**Test Eden**: _________________
**Tarih**: _________________
**Ortam**: Development / Staging / Production
**Sonuç**: ✅ Pass / ❌ Fail
**Notlar**: _________________
