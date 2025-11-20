# SecuredOperation Aspect - Geliştirme Rehberi

**Tarih**: 2025-10-26  
**Proje**: ZiraAI Backend  
**Konu**: SecuredOperation attribute kullanarak endpoint authorization

---

## 📋 İçindekiler

1. [SecuredOperation Nedir?](#securedoperation-nedir)
2. [OperationClaim Naming Convention](#operationclaim-naming-convention)
3. [Yeni OperationClaim Ekleme Adımları](#yeni-operationclaim-ekleme-adımları)
4. [Handler'lara SecuredOperation Ekleme](#handlerlara-securedoperation-ekleme)
5. [Claim'leri Group'lara Atama](#claimleri-grouplara-atama)
6. [Troubleshooting](#troubleshooting)

---

## 🎯 SecuredOperation Nedir?

`SecuredOperation` Castle DynamicProxy kullanan bir **AOP (Aspect Oriented Programming)** attribute'dur. Handler metotları çalıştırılmadan önce kullanıcının gerekli yetkiye (OperationClaim) sahip olup olmadığını kontrol eder.

### Çalışma Prensibi

1. **Handler çağrılır** → `TransferCodesToDealerCommandHandler.Handle()`
2. **SecuredOperation intercept eder** → `OnBefore()` metodu çalışır
3. **Claim kontrolü yapılır**:
   - Handler sınıf adını alır: `TransferCodesToDealerCommandHandler`
   - "Handler" suffix'ini kaldırır: `TransferCodesToDealerCommand`
   - Kullanıcının cache'deki claim'lerinde arar
4. **Sonuç**:
   - ✅ Claim varsa → Handler çalışır
   - ❌ Claim yoksa → `SecurityException` fırlatır

### Kod Konumu

**Business/BusinessAspects/SecuredOperation.cs**:line:30-60

```csharp
protected override void OnBefore(IInvocation invocation)
{
    // 1. UserId'yi JWT token'dan al
    var userId = _httpContextAccessor.HttpContext?.User.Claims
        .FirstOrDefault(x => x.Type.EndsWith("nameidentifier"))?.Value;

    if (userId == null)
    {
        throw new SecurityException(Messages.AuthorizationsDenied);
    }

    // 2. Kullanıcının claim'lerini cache'den oku
    var oprClaims = _cacheManager.Get<IEnumerable<string>>($"{CacheKeys.UserIdForClaim}={userId}");

    // 3. Handler sınıf adını al ve "Handler" suffix'ini kaldır
    var operationName = invocation.Method?.DeclaringType?.Name;
    if (string.IsNullOrEmpty(operationName))
    {
        throw new SecurityException(Messages.AuthorizationsDenied);
    }
    
    operationName = operationName.Replace("Handler", "");
    
    // 4. Claim kontrolü yap
    if (oprClaims != null && oprClaims.Contains(operationName))
    {
        return; // İzin ver
    }

    throw new SecurityException(Messages.AuthorizationsDenied);
}
```

---

## 🔍 BEFORE Adding New Endpoint: Verification Checklist

**CRITICAL:** Her yeni admin endpoint eklemeden ÖNCE bu adımları takip et!

### Adım 1: Handler Class Adını Doğrula

Handler class adı **mutlaka** "Handler" suffix'i ile bitmeli:

```csharp
// ✅ DOĞRU
public class GetSubscriptionDetailsQueryHandler : IRequestHandler<...>

// ❌ YANLIŞ - Handler suffix'i yok
public class GetSubscriptionDetailsQuery : IRequestHandler<...>
```

### Adım 2: OperationClaim Adını Hesapla

Claim adı = Handler class adı - "Handler" suffix'i

```
GetSubscriptionDetailsQueryHandler → GetSubscriptionDetailsQuery
AssignSubscriptionCommandHandler → AssignSubscriptionCommand
```

### Adım 3: SQL Migration Script Oluştur

**Template:**
```sql
-- Add [Feature] Operation Claims
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
VALUES
    (ID, 'ClaimNameFromStep2', 'Display Name', 'Description')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
VALUES (1, ID)  -- 1 = Administrators
ON CONFLICT DO NOTHING;
```

### Adım 4: Aspect Attributes Ekle

**Admin handler'lar için ZORUNLU aspect sırası:**

```csharp
[SecuredOperation(Priority = 1)]
[PerformanceAspect(5)]
[LogAspect(typeof(FileLogger))]
public async Task<IDataResult<T>> Handle(...)
```

**Gerekli using statements:**
```csharp
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
```

### Adım 5: Deploy ve Test Checklist

- [ ] SQL migration Railway database'de çalıştırıldı
- [ ] Verification query sonucu: Claim eklendi
- [ ] Verification query sonucu: Admin grubuna atandı
- [ ] Code push edildi
- [ ] Railway deployment tamamlandı
- [ ] **Logout/Login yapıldı** (cache refresh için ZORUNLU)
- [ ] Endpoint test edildi (200 OK aldı)

---

## 📝 OperationClaim Naming Convention

### Kural: Handler sınıf adı - "Handler" suffix'i = OperationClaim adı

| Handler Sınıf Adı | OperationClaim Adı | Açıklama |
|-------------------|-------------------|----------|
| `CreateUserCommand`**Handler** | `CreateUserCommand` | Command handler |
| `GetUsersQuery`**Handler** | `GetUsersQuery` | Query handler |
| `TransferCodesToDealerCommand`**Handler** | `TransferCodesToDealerCommand` | Dealer command |
| `GetDealerSummaryQuery`**Handler** | `GetDealerSummaryQuery` | Dealer query |

### ⚠️ Yaygın Hatalar

❌ **YANLIŞ** - Handler suffix'siz isimler:
```sql
INSERT INTO "OperationClaims" ("Name", "Alias", "Description")
VALUES ('TransferCodesToDealer', 'dealer.transfer', '...'); -- YANLIŞ!
```

❌ **YANLIŞ** - Handler suffix'li isimler:
```sql
INSERT INTO "OperationClaims" ("Name", "Alias", "Description")
VALUES ('TransferCodesToDealerCommandHandler', 'dealer.transfer', '...'); -- YANLIŞ!
```

✅ **DOĞRU** - Command/Query suffix'li, Handler suffix'siz:
```sql
INSERT INTO "OperationClaims" ("Name", "Alias", "Description")
VALUES ('TransferCodesToDealerCommand', 'dealer.transfer', '...'); -- DOĞRU!
```

### Mevcut Claim'leri Görüntüleme

Tüm OperationClaim'ler `claudedocs/Dealers/claims.txt` dosyasında listelenmiştir:
```
1   GetUserLookupQuery
2   GetUserQuery
3   GetUsersQuery
4   CreateUserCommand
5   DeleteUserCommand
...
```

---

## 🆕 Yeni OperationClaim Ekleme Adımları

### Adım 1: Handler Sınıf Adını Belirle

**Örnek**: Dealer'a kod transfer etme handler'ı
```csharp
// Business/Handlers/Sponsorship/Commands/TransferCodesToDealerCommand.cs
public class TransferCodesToDealerCommandHandler : IRequestHandler<...>
{
    // ...
}
```

Handler adı: `TransferCodesToDealerCommandHandler`  
OperationClaim adı: `TransferCodesToDealerCommand` (Handler olmadan)

### Adım 2: SQL Migration Oluştur

**claudedocs/Dealers/migrations/004_dealer_authorization.sql**:

```sql
-- =====================================================
-- PART 1: Create Operation Claims
-- =====================================================

-- TransferCodesToDealerCommand
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'TransferCodesToDealerCommand', 'dealer.transfer', 'Transfer sponsorship codes to dealer'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'TransferCodesToDealerCommand');

-- CreateDealerInvitationCommand
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'CreateDealerInvitationCommand', 'dealer.invite', 'Create dealer invitation'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'CreateDealerInvitationCommand');

-- ReclaimDealerCodesCommand
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'ReclaimDealerCodesCommand', 'dealer.reclaim', 'Reclaim unused codes from dealer'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'ReclaimDealerCodesCommand');

-- GetDealerPerformanceQuery
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'GetDealerPerformanceQuery', 'dealer.analytics', 'View dealer performance analytics'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'GetDealerPerformanceQuery');

-- GetDealerSummaryQuery
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'GetDealerSummaryQuery', 'dealer.summary', 'View all dealers summary'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'GetDealerSummaryQuery');

-- GetDealerInvitationsQuery
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'GetDealerInvitationsQuery', 'dealer.invitations', 'List dealer invitations'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'GetDealerInvitationsQuery');

-- SearchDealerByEmailQuery
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'SearchDealerByEmailQuery', 'dealer.search', 'Search dealer by email'
WHERE NOT EXISTS (SELECT 1 FROM public."OperationClaims" WHERE "Name" = 'SearchDealerByEmailQuery');
```

#### SQL Migration Kuralları

1. **İdempotent Olmalı**: `WHERE NOT EXISTS` kullan (aynı script birden fazla çalıştırılabilmeli)
2. **ON CONFLICT KULLANMA**: `Name` field'ında UNIQUE constraint yok
3. **Alias**: Kısa, okunabilir alias ekle (örn: `dealer.transfer`)
4. **Description**: Açıklayıcı tanım yaz

### Adım 3: Group'lara Atama

Claim'leri Admin ve/veya Sponsor gruplarına ata:

```sql
-- =====================================================
-- PART 2: Assign Claims to Groups
-- =====================================================

-- Sponsor Group (GroupId = 3)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 3, oc."Id"
FROM public."OperationClaims" oc
WHERE oc."Name" IN (
    'TransferCodesToDealerCommand',
    'CreateDealerInvitationCommand',
    'ReclaimDealerCodesCommand',
    'GetDealerPerformanceQuery',
    'GetDealerSummaryQuery',
    'GetDealerInvitationsQuery',
    'SearchDealerByEmailQuery'
)
AND NOT EXISTS (
    SELECT 1 FROM public."GroupClaims" gc 
    WHERE gc."GroupId" = 3 AND gc."ClaimId" = oc."Id"
);

-- Admin Group (GroupId = 1)
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 1, oc."Id"
FROM public."OperationClaims" oc
WHERE oc."Name" IN (
    'TransferCodesToDealerCommand',
    'CreateDealerInvitationCommand',
    'ReclaimDealerCodesCommand',
    'GetDealerPerformanceQuery',
    'GetDealerSummaryQuery',
    'GetDealerInvitationsQuery',
    'SearchDealerByEmailQuery'
)
AND NOT EXISTS (
    SELECT 1 FROM public."GroupClaims" gc 
    WHERE gc."GroupId" = 1 AND gc."ClaimId" = oc."Id"
);
```

#### Group ID'leri

| Group ID | Group Adı | Açıklama |
|----------|-----------|----------|
| 1 | Admin | Sistem yöneticileri |
| 3 | Sponsor | Sponsor kullanıcılar |

### Adım 4: Verification Queries Ekle

Migration'ın başarılı olup olmadığını kontrol et:

```sql
-- =====================================================
-- PART 3: Verification Queries
-- =====================================================

-- Verify OperationClaims were created
SELECT "Id", "Name", "Alias", "Description"
FROM public."OperationClaims"
WHERE "Name" LIKE '%Dealer%'
ORDER BY "Id";

-- Verify GroupClaims assignments for Sponsor group
SELECT 
    g."GroupName",
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias",
    oc."Description"
FROM public."GroupClaims" gc
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."Id" = 3 -- Sponsor group
  AND oc."Name" LIKE '%Dealer%'
ORDER BY oc."Name";

-- Verify GroupClaims assignments for Admin group
SELECT 
    g."GroupName",
    oc."Name" as "ClaimName",
    oc."Alias" as "ClaimAlias",
    oc."Description"
FROM public."GroupClaims" gc
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."Id" = 1 -- Admin group
  AND oc."Name" LIKE '%Dealer%'
ORDER BY oc."Name";
```

### Adım 5: SQL Migration'ı Çalıştır

**Staging Database**:
```bash
psql -h postgres.railway.internal -U postgres -d ziraai_staging \
  -f claudedocs/Dealers/migrations/004_dealer_authorization.sql
```

**Production Database** (sadece test sonrası):
```bash
psql -h prod-host -U postgres -d ziraai_production \
  -f claudedocs/Dealers/migrations/004_dealer_authorization.sql
```

---

## 🔒 Handler'lara SecuredOperation Ekleme

### Adım 1: Using Directive Ekle

```csharp
using Business.BusinessAspects;
```

### Adım 2: Handler Metoda Attribute Ekle

```csharp
public class TransferCodesToDealerCommandHandler 
    : IRequestHandler<TransferCodesToDealerCommand, IDataResult<DealerCodeTransferResponseDto>>
{
    // Dependencies...

    [SecuredOperation(Priority = 1)]
    public async Task<IDataResult<DealerCodeTransferResponseDto>> Handle(
        TransferCodesToDealerCommand request, 
        CancellationToken cancellationToken)
    {
        // Business logic...
    }
}
```

### Priority Nedir?

`Priority = 1` aspect çalışma sırasını belirler:
- **Priority = 1**: İlk çalışacak aspect (authorization check)
- **Priority = 2**: İkinci sırada çalışacak (örn: validation)
- **Priority = 3**: Üçüncü sırada (örn: caching)

Authorization **her zaman** ilk kontrol edilmeli → `Priority = 1`

### Örnek Handler Implementasyonu

**Command Handler**:
```csharp
using Business.BusinessAspects;
using MediatR;
using Core.Utilities.Results;

namespace Business.Handlers.Sponsorship.Commands
{
    public class TransferCodesToDealerCommandHandler 
        : IRequestHandler<TransferCodesToDealerCommand, IDataResult<DealerCodeTransferResponseDto>>
    {
        private readonly ISponsorshipCodeRepository _codeRepository;
        private readonly IUserRepository _userRepository;

        public TransferCodesToDealerCommandHandler(
            ISponsorshipCodeRepository codeRepository,
            IUserRepository userRepository)
        {
            _codeRepository = codeRepository;
            _userRepository = userRepository;
        }

        [SecuredOperation(Priority = 1)]
        public async Task<IDataResult<DealerCodeTransferResponseDto>> Handle(
            TransferCodesToDealerCommand request, 
            CancellationToken cancellationToken)
        {
            // 1. Kullanıcı Sponsor rolünde mi?
            var userGroups = await _userRepository.GetUserGroupsAsync(request.UserId);
            if (!userGroups.Contains("Sponsor"))
            {
                return new ErrorDataResult<DealerCodeTransferResponseDto>(
                    "Only sponsors can transfer codes to dealers.");
            }

            // 2. Transfer işlemi...
            // Business logic burada

            return new SuccessDataResult<DealerCodeTransferResponseDto>(response);
        }
    }
}
```

**Query Handler**:
```csharp
using Business.BusinessAspects;
using MediatR;
using Core.Utilities.Results;

namespace Business.Handlers.Sponsorship.Queries
{
    public class GetDealerSummaryQueryHandler 
        : IRequestHandler<GetDealerSummaryQuery, IDataResult<DealerSummaryResponseDto>>
    {
        private readonly IDealerInvitationRepository _dealerRepository;

        public GetDealerSummaryQueryHandler(IDealerInvitationRepository dealerRepository)
        {
            _dealerRepository = dealerRepository;
        }

        [SecuredOperation(Priority = 1)]
        public async Task<IDataResult<DealerSummaryResponseDto>> Handle(
            GetDealerSummaryQuery request, 
            CancellationToken cancellationToken)
        {
            // Query logic...
            
            return new SuccessDataResult<DealerSummaryResponseDto>(response);
        }
    }
}
```

---

## 👥 Claim'leri Group'lara Atama

### Group Yapısı

ZiraAI'da 3 ana grup vardır:

| Group ID | Group Adı | Roller | Açıklama |
|----------|-----------|--------|----------|
| 1 | Admin | Admin | Sistem yöneticileri, tüm yetkilere sahip |
| 3 | Sponsor | Sponsor | Sponsorluk işlemleri yapabilir |
| - | Farmer | Farmer | Çiftçiler (dealer değil) |

### Claim Atama Stratejisi

**Dealer Endpoint'leri için**:
- ✅ Admin Group (Id = 1) → Tüm dealer claim'leri
- ✅ Sponsor Group (Id = 3) → Tüm dealer claim'leri
- ❌ Farmer Group → Dealer claim'leri YOK

**Neden?**
- Main sponsor (Sponsor group) dealer'lara kod transfer edebilir
- Admin her şeyi yapabilir
- Farmer sadece kendi analizlerini görür, dealer işlemleri yapamaz

### SQL ile Atama

```sql
-- Sponsor grubuna 7 dealer claim'i ata
INSERT INTO public."GroupClaims" ("GroupId", "ClaimId")
SELECT 3, oc."Id"
FROM public."OperationClaims" oc
WHERE oc."Name" IN (
    'TransferCodesToDealerCommand',
    'CreateDealerInvitationCommand',
    'ReclaimDealerCodesCommand',
    'GetDealerPerformanceQuery',
    'GetDealerSummaryQuery',
    'GetDealerInvitationsQuery',
    'SearchDealerByEmailQuery'
)
AND NOT EXISTS (
    SELECT 1 FROM public."GroupClaims" gc 
    WHERE gc."GroupId" = 3 AND gc."ClaimId" = oc."Id"
);
```

### Claim Atamasını Doğrulama

```sql
-- Sponsor grubunun tüm claim'lerini listele
SELECT 
    g."GroupName",
    oc."Name",
    oc."Alias",
    oc."Description"
FROM public."GroupClaims" gc
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE g."Id" = 3
ORDER BY oc."Name";
```

---

## 🐛 Troubleshooting

### Hata 1: "AuthorizationsDenied"

**Semptom**: API endpoint 403 veya "AuthorizationsDenied" hatası döndürüyor

**Olası Sebepler**:

1. **OperationClaim database'de yok**
   ```sql
   -- Kontrol et:
   SELECT * FROM public."OperationClaims" 
   WHERE "Name" = 'TransferCodesToDealerCommand';
   ```
   
   **Çözüm**: SQL migration'ı çalıştır

2. **Claim adı yanlış**
   ```sql
   -- Yanlış: Handler suffix'li
   SELECT * FROM public."OperationClaims" 
   WHERE "Name" = 'TransferCodesToDealerCommandHandler'; -- ❌
   
   -- Doğru: Handler suffix'siz
   SELECT * FROM public."OperationClaims" 
   WHERE "Name" = 'TransferCodesToDealerCommand'; -- ✅
   ```
   
   **Çözüm**: Claim adını düzelt ve migration'ı tekrar çalıştır

3. **User claim'i yok (Group'a atanmamış)**
   ```sql
   -- User'ın group'larını kontrol et:
   SELECT u."UserId", u."FullName", g."GroupName"
   FROM public."Users" u
   INNER JOIN public."UserGroup" ug ON u."UserId" = ug."UserId"
   INNER JOIN public."Group" g ON ug."GroupId" = g."Id"
   WHERE u."UserId" = 159;
   
   -- User'ın group üzerinden sahip olduğu claim'leri kontrol et:
   SELECT oc."Name", oc."Alias"
   FROM public."UserGroup" ug
   INNER JOIN public."GroupClaims" gc ON ug."GroupId" = gc."GroupId"
   INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
   WHERE ug."UserId" = 159;
   ```
   
   **Çözüm**: User'ı doğru group'a ekle veya claim'i group'a ata

4. **Cache güncel değil**
   
   Kullanıcı claim'leri cache'de tutulur (`CacheKeys.UserIdForClaim`). Yeni claim eklendiğinde cache temizlenmeli.
   
   **Çözüm**:
   - User logout/login yapsın (token yenilenir, cache refresh olur)
   - Veya cache'i manuel temizle (Redis/Memory)

### Hata 2: NullReferenceException in SecuredOperation

**Semptom**: 
```
System.NullReferenceException: Object reference not set to an instance of an object.
at Business.BusinessAspects.SecuredOperation.OnBefore(IInvocation invocation)
```

**Olası Sebepler**:

1. **invocation.Method?.DeclaringType?.Name null dönüyor**
   
   **Çözüm**: Null check ekle
   ```csharp
   var operationName = invocation.Method?.DeclaringType?.Name;
   if (string.IsNullOrEmpty(operationName))
   {
       throw new SecurityException(Messages.AuthorizationsDenied);
   }
   ```

2. **oprClaims cache'den null dönüyor**
   
   **Çözüm**: Null check ekle
   ```csharp
   if (oprClaims != null && oprClaims.Contains(operationName))
   {
       return;
   }
   ```

3. **userId null (JWT token geçersiz)**
   
   **Çözüm**: Token kontrolü ekle
   ```csharp
   if (userId == null)
   {
       throw new SecurityException(Messages.AuthorizationsDenied);
   }
   ```

### Hata 3: PerformanceAspect Eksikliği - 401 Unauthorized

**Semptom**: 
- API endpoint 401 Unauthorized hatası döndürüyor
- Database'de claim'ler var ve doğru atanmış
- Diğer admin endpoint'leri çalışıyor
- Cache temiz, user logout/login yaptı

**Sebep**: Handler'da `[PerformanceAspect]` attribute'u eksik

**Açıklama**:
Admin handler'larında AOP aspect pipeline'ı şu sırada olmalı:
```csharp
[SecuredOperation(Priority = 1)]
[PerformanceAspect(5)]
[LogAspect(typeof(FileLogger))]
```

Eğer `[PerformanceAspect(5)]` eksikse, aspect pipeline düzgün çalışmaz ve authorization başarısız olur.

**Kontrol**:
```csharp
// ❌ YANLIŞ - PerformanceAspect eksik
[SecuredOperation(Priority = 1)]
[LogAspect(typeof(FileLogger))]
public async Task<IDataResult<T>> Handle(...)

// ✅ DOĞRU - Tüm aspect'ler mevcut
[SecuredOperation(Priority = 1)]
[PerformanceAspect(5)]
[LogAspect(typeof(FileLogger))]
public async Task<IDataResult<T>> Handle(...)
```

**Çözüm**:
1. Çalışan bir admin handler'ı referans al (örn: `GetAllUsersQuery.cs`)
2. Aynı using directive'leri ekle:
   ```csharp
   using Core.Aspects.Autofac.Logging;
   using Core.Aspects.Autofac.Performance;  // ← Bunu ekle
   using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
   ```
3. Handler method'una `[PerformanceAspect(5)]` ekle
4. Build ve test et

**Örnek**:
`Business/Handlers/AdminSubscriptions/Queries/GetSubscriptionDetailsQuery.cs` bu hatadan etkilenmişti.

### Hata 3: SQL Migration Hatası - ON CONFLICT

**Semptom**:
```
ERROR [42P10]: ERROR: there is no unique or exclusion constraint matching 
the ON CONFLICT specification
```

**Sebep**: `OperationClaims.Name` field'ında UNIQUE constraint yok

**Çözüm**: `ON CONFLICT` yerine `WHERE NOT EXISTS` kullan

❌ **YANLIŞ**:
```sql
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
VALUES ('TransferCodesToDealerCommand', 'dealer.transfer', 'Transfer codes')
ON CONFLICT ("Name") DO NOTHING; -- ❌ ÇALIŞMAZ
```

✅ **DOĞRU**:
```sql
INSERT INTO public."OperationClaims" ("Name", "Alias", "Description")
SELECT 'TransferCodesToDealerCommand', 'dealer.transfer', 'Transfer codes'
WHERE NOT EXISTS (
    SELECT 1 FROM public."OperationClaims" 
    WHERE "Name" = 'TransferCodesToDealerCommand'
); -- ✅ ÇALIŞIR
```

### Debug Yöntemleri

#### 1. Railway Logs İnceleme

Railway dashboard → Deployments → Logs

**Aranacak Kelimeler**:
- `AuthorizationsDenied`
- `SecuredOperation`
- `NullReferenceException`
- Endpoint path (örn: `/api/v1/sponsorship/dealer/transfer-codes`)

#### 2. Database'i Kontrol Etme

```sql
-- Claim var mı?
SELECT * FROM public."OperationClaims" 
WHERE "Name" LIKE '%Dealer%';

-- Group'a atanmış mı?
SELECT 
    g."GroupName",
    oc."Name"
FROM public."GroupClaims" gc
INNER JOIN public."Group" g ON gc."GroupId" = g."Id"
INNER JOIN public."OperationClaims" oc ON gc."ClaimId" = oc."Id"
WHERE oc."Name" LIKE '%Dealer%';

-- User bu group'ta mı?
SELECT 
    u."UserId",
    u."FullName",
    g."GroupName"
FROM public."UserGroup" ug
INNER JOIN public."Users" u ON ug."UserId" = u."UserId"
INNER JOIN public."Group" g ON ug."GroupId" = g."Id"
WHERE u."UserId" = 159;
```

#### 3. Postman/Curl ile Test

```bash
# Token al
TOKEN="eyJhbGci..."

# Endpoint test et
curl -X POST 'https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/transfer-codes' \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -H 'x-dev-arch-version: 1.0' \
  -d '{
    "purchaseId": 26,
    "dealerId": 158,
    "codeCount": 5
  }'
```

**Beklenen Başarı Yanıtı**:
```json
{
  "success": true,
  "message": "5 codes transferred successfully",
  "data": { ... }
}
```

**Authorization Hatası**:
```json
{
  "success": false,
  "message": "Code transfer failed: AuthorizationsDenied"
}
```

---

## 📚 Real Example: AdminSubscriptions Handlers

Bu gerçek bir örnektir - AdminSubscriptionsController için eklenen tüm handler'lar ve claim'ler:

### Handler → Claim Mapping

| Handler Class | Claim Name (SQL) | ID | Endpoint |
|---------------|------------------|----|----|
| `GetAllSubscriptionsQueryHandler` | `GetAllSubscriptionsQuery` | 100 | GET /api/admin/subscriptions |
| `GetSubscriptionDetailsQueryHandler` | `GetSubscriptionDetailsQuery` | 101 | GET /api/admin/subscriptions/details |
| `GetSubscriptionByIdQueryHandler` | `GetSubscriptionByIdQuery` | 102 | GET /api/admin/subscriptions/{id} |
| `AssignSubscriptionCommandHandler` | `AssignSubscriptionCommand` | 103 | POST /api/admin/subscriptions/assign |
| `ExtendSubscriptionCommandHandler` | `ExtendSubscriptionCommand` | 104 | PUT /api/admin/subscriptions/{id}/extend |
| `CancelSubscriptionCommandHandler` | `CancelSubscriptionCommand` | 105 | DELETE /api/admin/subscriptions/{id}/cancel |
| `BulkCancelSubscriptionsCommandHandler` | `BulkCancelSubscriptionsCommand` | 106 | POST /api/admin/subscriptions/bulk-cancel |

### SQL Migration Script (add_admin_claims.sql)

```sql
-- Insert operation claims
INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
VALUES
    (100, 'GetAllSubscriptionsQuery', 'Get All Subscriptions', 'Query all subscriptions'),
    (101, 'GetSubscriptionDetailsQuery', 'Get Subscription Details', 'Query detailed subscription information'),
    (102, 'GetSubscriptionByIdQuery', 'Get Subscription By ID', 'Query subscription by ID'),
    (103, 'AssignSubscriptionCommand', 'Assign Subscription', 'Assign subscription to user'),
    (104, 'ExtendSubscriptionCommand', 'Extend Subscription', 'Extend user subscription'),
    (105, 'CancelSubscriptionCommand', 'Cancel Subscription', 'Cancel user subscription'),
    (106, 'BulkCancelSubscriptionsCommand', 'Bulk Cancel Subscriptions', 'Cancel multiple subscriptions')
ON CONFLICT ("Id") DO NOTHING;

-- Assign to Administrators group
INSERT INTO "GroupClaims" ("GroupId", "ClaimId")
VALUES
    (1, 100), (1, 101), (1, 102), (1, 103), (1, 104), (1, 105), (1, 106)
ON CONFLICT DO NOTHING;
```

### Handler Implementation Example

```csharp
// File: Business/Handlers/AdminSubscriptions/Queries/GetSubscriptionDetailsQuery.cs
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;

public class GetSubscriptionDetailsQueryHandler : IRequestHandler<...>
{
    [SecuredOperation(Priority = 1)]
    [PerformanceAspect(5)]
    [LogAspect(typeof(FileLogger))]
    public async Task<IDataResult<...>> Handle(...)
    {
        // Implementation
    }
}
```

### Verification Queries

```sql
-- Check all admin subscription claims exist
SELECT "Id", "Name", "Alias"
FROM "OperationClaims"
WHERE "Id" BETWEEN 100 AND 106
ORDER BY "Id";

-- Check all assigned to Administrators
SELECT gc."ClaimId", oc."Name", g."GroupName"
FROM "GroupClaims" gc
JOIN "OperationClaims" oc ON gc."ClaimId" = oc."Id"
JOIN "Groups" g ON gc."GroupId" = g."Id"
WHERE gc."ClaimId" BETWEEN 100 AND 106;
```

### Common Mistakes (❌ vs ✅)

**Mistake 1: Handler suffix eksik**
```csharp
❌ public class GetSubscriptionDetailsQuery : IRequestHandler<...>
✅ public class GetSubscriptionDetailsQueryHandler : IRequestHandler<...>
```

**Mistake 2: SQL claim adı yanlış**
```sql
❌ INSERT INTO "OperationClaims" ("Name") VALUES ('GetSubscriptionDetails')
✅ INSERT INTO "OperationClaims" ("Name") VALUES ('GetSubscriptionDetailsQuery')
```

**Mistake 3: PerformanceAspect eksik**
```csharp
❌ [SecuredOperation(Priority = 1)]
   [LogAspect(typeof(FileLogger))]
   
✅ [SecuredOperation(Priority = 1)]
   [PerformanceAspect(5)]
   [LogAspect(typeof(FileLogger))]
```

**Mistake 4: GroupClaims ataması unutulmuş**
```sql
❌ INSERT INTO "OperationClaims" ... (claim eklendi ama gruba atanmadı)

✅ INSERT INTO "OperationClaims" ...
   INSERT INTO "GroupClaims" ("GroupId", "ClaimId") VALUES (1, 101)
```

---

---

## ✅ Checklist: Yeni Endpoint Authorization Ekleme

- [ ] Handler sınıf adını belirle (örn: `TransferCodesToDealerCommandHandler`)
- [ ] OperationClaim adını hesapla (Handler suffix'siz: `TransferCodesToDealerCommand`)
- [ ] SQL migration dosyası oluştur
  - [ ] INSERT INTO OperationClaims (WHERE NOT EXISTS kullan)
  - [ ] INSERT INTO GroupClaims (Sponsor ve/veya Admin)
  - [ ] Verification queries ekle
- [ ] SQL migration'ı staging database'de çalıştır
- [ ] Handler'a `[SecuredOperation(Priority = 1)]` ekle
- [ ] Handler'da `using Business.BusinessAspects;` ekle
- [ ] Admin handler ise: Çalışan admin handler'dan aspect pattern'i kopyala
  - [ ] `using Core.Aspects.Autofac.Performance;` ekle
  - [ ] `[PerformanceAspect(5)]` attribute'unu ekle (SecuredOperation ile LogAspect arasına)
- [ ] Build ve deploy
- [ ] Test et (Postman veya curl)
- [ ] Database'de claim'leri verify et
- [ ] Railway logs'da hata yok mu kontrol et
- [ ] E2E test yap (farklı roller ile)

---

## 📚 Referanslar

- **OperationClaims Listesi**: `claudedocs/Dealers/claims.txt`
- **SQL Migration Örneği**: `claudedocs/Dealers/migrations/004_dealer_authorization.sql`
- **SecuredOperation Kodu**: `Business/BusinessAspects/SecuredOperation.cs`
- **DDL (Database Schema)**: `DDL.txt` (OperationClaims ve GroupClaims table yapısı)

---

**Hazırlayan**: Claude Code  
**Son Güncelleme**: 2025-10-26  
**Versiyon**: 1.0
