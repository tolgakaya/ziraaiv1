# Toplu Dealer Davet Sistemi - Tasarım Dokümanı

**Doküman Versiyonu:** 1.0
**Tarih:** 2025-11-03
**Tasarımcı:** Claude Code
**Amaç:** Excel dosyası ile toplu dealer davet gönderimi sistemi

---

## 📋 İçindekiler

1. [Mevcut Sistem Analizi](#1-mevcut-sistem-analizi)
2. [Toplu Davet Gereksinimleri](#2-toplu-davet-gereksinimleri)
3. [Teknik Tasarım](#3-teknik-tasarım)
4. [API Endpoint Spesifikasyonu](#4-api-endpoint-spesifikasyonu)
5. [Excel Dosya Formatı](#5-excel-dosya-formatı)
6. [Validasyon Kuralları](#6-validasyon-kuralları)
7. [Hata Yönetimi](#7-hata-yönetimi)
8. [İmplementasyon Planı](#8-i̇mplementasyon-planı)

---

## 1. Mevcut Sistem Analizi

### 1.1 Tekli Davet Endpoint

**Mevcut Endpoint:** `POST /api/v1/sponsorship/dealer/invite`

**Command:** `CreateDealerInvitationCommand`

**Anahtar Özellikler:**
- Email, Phone, DealerName ile davet oluşturma
- İki tip: **Invite** (manuel kayıt) veya **AutoCreate** (otomatik hesap)
- Tier-based kod seçimi (S, M, L, XL)
- Kod rezervasyonu ve transfer
- SMS gönderimi ve SignalR bildirimi
- 7 günlük davet süresi

**Kod Seçim Algoritması:**
```
1. SponsorId'ye göre filtrele
2. IsUsed = false, DealerId = null, Reserved = null
3. ExpiryDate > Now (aktif kodlar)
4. PackageTier filtresi (opsiyonel)
5. PurchaseId filtresi (opsiyonel, deprecated)
6. Expiry date'e göre sırala (FIFO - önce süresi dolacaklar)
7. CodeCount kadar al
```

**İş Akışı:**
```
1. Request validation (email required for Invite)
2. Tier validation (S, M, L, XL)
3. Kod mevcudiyeti kontrolü
4. Invitation token oluşturma (GUID)
5. DealerInvitation entity oluşturma
6. AutoCreate ise:
   - Dealer hesabı oluştur
   - Sponsor rolü ata
   - Kodları direkt transfer et
   - Status = "Accepted"
7. Invite ise:
   - Kodları rezerve et (ReservedForInvitationId)
   - Status = "Pending"
8. SignalR bildirimi gönder
9. SMS gönder (opsiyonel)
10. Response dön
```

### 1.2 Mevcut Entity: DealerInvitation

**Kritik Alanlar:**
- `SponsorId` - Davet gönderen sponsor
- `Email`, `Phone`, `DealerName` - Dealer bilgileri
- `InvitationType` - "Invite" veya "AutoCreate"
- `Status` - "Pending", "Accepted", "Expired", "Cancelled"
- `PackageTier` - Tier filtresi (S, M, L, XL)
- `CodeCount` - Transfer edilecek kod sayısı
- `InvitationToken` - Unique token
- `ExpiryDate` - Davet süresi (default 7 gün)

---

## 2. Toplu Davet Gereksinimleri

### 2.1 Fonksiyonel Gereksinimler

1. **Excel Upload**
   - Sponsor Excel dosyası yükleyebilmeli (.xlsx, .xls)
   - Dosya boyutu limiti: 5 MB
   - Maximum satır sayısı: 2000 dealer

2. **Validasyon**
   - Email format kontrolü
   - Telefon format kontrolü (Türkiye: +90 veya 0 ile başlayan)
   - Dealer name zorunlu
   - Duplicate email/phone kontrolü (aynı dosyada)
   - Mevcut dealer kontrolü (database'de zaten var mı?)

3. **İşlem Türü**
   - Tek tip seçim: Tüm davetler "Invite" VEYA "AutoCreate"
   - Tier seçimi: Tüm davetler için aynı tier (opsiyonel)
   - Kod sayısı: Her dealer için aynı miktar VEYA farklı (Excel'den)

4. **Batch Processing**
   - Asenkron işleme (background job)
   - Progress tracking (kaç tane başarılı/başarısız)
   - Partial success destekleme (bazıları başarılı, bazıları hatalı)

5. **Reporting**
   - İşlem sonucu raporu (Excel veya JSON)
   - Başarılı davetler listesi
   - Hatalı kayıtlar ve hata nedenleri
   - İşlem özeti (toplam, başarılı, başarısız)

6. **Notification**
   - İşlem başladığında bildirim
   - İşlem tamamlandığında bildirim
   - Email ile rapor gönderimi (opsiyonel)

### 2.2 Teknik Gereksinimler

1. **Performance**
   - 2000 dealer için işlem süresi: < 5 dakika
   - Database transaction yönetimi (batch insert)
   - Memory-efficient Excel parsing

2. **Reliability**
   - Transaction rollback on critical errors
   - Retry logic for SMS failures
   - Database connection pooling

3. **Security**
   - File type validation (only Excel)
   - Virus scanning (optional, for production)
   - Rate limiting (prevent abuse)
   - Authorization: Sponsor role only

4. **Scalability**
   - Queue-based processing (RabbitMQ or Hangfire)
   - Horizontal scaling support
   - Database indexing for bulk queries

---

## 3. Teknik Tasarım

### 3.1 Mimari Akış

```
┌─────────────────┐
│  Sponsor Web UI │
└────────┬────────┘
         │ Upload Excel
         ▼
┌──────────────────────────────────────────┐
│  API: POST /api/v1/sponsorship/          │
│       dealer/invite-bulk                 │
└────────┬─────────────────────────────────┘
         │ 1. Validate File
         │ 2. Parse Excel
         │ 3. Validate Data
         ▼
┌──────────────────────────────────────────┐
│  BulkDealerInvitationCommand             │
│  - SponsorId                             │
│  - ExcelFile (IFormFile)                 │
│  - InvitationType                        │
│  - DefaultTier                           │
│  - DefaultCodeCount                      │
│  - SendSms (bool)                        │
└────────┬─────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────┐
│  BulkDealerInvitationCommandHandler      │
│  1. Parse Excel → List<DealerRow>        │
│  2. Validate Rows (sync)                 │
│  3. Check Code Availability              │
│  4. Create Background Job                │
│  5. Return JobId                         │
└────────┬─────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────┐
│  Background Job (Hangfire)               │
│  - Process each dealer sequentially      │
│  - Create DealerInvitation               │
│  - Reserve/Transfer Codes                │
│  - Send SMS (optional)                   │
│  - Send SignalR notification             │
│  - Track progress                        │
└────────┬─────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────┐
│  Result Entity: BulkInvitationResult     │
│  - JobId                                 │
│  - SponsorId                             │
│  - TotalCount                            │
│  - SuccessCount                          │
│  - FailedCount                           │
│  - Status (Processing, Completed, Failed)│
│  - ResultDetails (JSON)                  │
└──────────────────────────────────────────┘
```

### 3.2 Yeni Entity: BulkInvitationJob

```csharp
public class BulkInvitationJob : IEntity
{
    public int Id { get; set; }
    public string JobId { get; set; } // Hangfire job ID
    public int SponsorId { get; set; }

    // Job Configuration
    public string InvitationType { get; set; } // "Invite" or "AutoCreate"
    public string DefaultTier { get; set; } // S, M, L, XL
    public int DefaultCodeCount { get; set; }
    public bool SendSms { get; set; }

    // Progress Tracking
    public int TotalDealers { get; set; }
    public int ProcessedDealers { get; set; }
    public int SuccessfulInvitations { get; set; }
    public int FailedInvitations { get; set; }

    // Status
    public string Status { get; set; } // "Pending", "Processing", "Completed", "Failed", "PartialSuccess"
    public DateTime CreatedDate { get; set; }
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    // Results
    public string ResultFileUrl { get; set; } // URL to downloadable result Excel
    public string ErrorSummary { get; set; } // JSON array of errors

    // File Info
    public string OriginalFileName { get; set; }
    public int FileSize { get; set; }
}
```

### 3.3 DTO: BulkInvitationRow

```csharp
public class BulkInvitationRow
{
    [Required]
    public int RowNumber { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; }

    [Required, Phone]
    public string Phone { get; set; }

    [Required, MaxLength(200)]
    public string DealerName { get; set; }

    public int? CodeCount { get; set; } // Null = use default
    public string PackageTier { get; set; } // Null = use default

    // Validation Results
    public bool IsValid { get; set; }
    public string ValidationError { get; set; }

    // Processing Results
    public bool IsProcessed { get; set; }
    public string ProcessingError { get; set; }
    public int? InvitationId { get; set; }
    public string InvitationToken { get; set; }
}
```

---

## 4. API Endpoint Spesifikasyonu

### 4.1 Toplu Davet Oluşturma

**Endpoint:** `POST /api/v1/sponsorship/dealer/invite-bulk`

**Authorization:** Sponsor role

**Content-Type:** `multipart/form-data`

**Request:**
```json
{
  "excelFile": [binary file],
  "invitationType": "Invite",
  "defaultTier": "L",
  "defaultCodeCount": 20,
  "sendSms": true,
  "useRowSpecificCounts": false
}
```

**Parameters:**
- `excelFile` (IFormFile, required): Excel dosyası (.xlsx, .xls)
- `invitationType` (string, required): "Invite" veya "AutoCreate"
- `defaultTier` (string, optional): "S", "M", "L", "XL" - tüm davetler için
- `defaultCodeCount` (int, required): Her dealer için kod sayısı (Excel'de belirtilmemişse)
- `sendSms` (bool, optional, default: true): SMS gönderilsin mi?
- `useRowSpecificCounts` (bool, optional, default: false): Excel'deki CodeCount sütununu kullan

**Response (202 Accepted):**
```json
{
  "success": true,
  "message": "Toplu davet işlemi başlatıldı",
  "data": {
    "jobId": "hangfire-job-123",
    "totalDealers": 150,
    "estimatedCompletionTime": "2025-11-03T15:30:00Z",
    "statusCheckUrl": "/api/v1/sponsorship/dealer/bulk-status/hangfire-job-123"
  }
}
```

**Error Responses:**

**400 Bad Request - Invalid File:**
```json
{
  "success": false,
  "message": "Geçersiz dosya formatı. Sadece .xlsx ve .xls desteklenir."
}
```

**400 Bad Request - Too Many Rows:**
```json
{
  "success": false,
  "message": "Maksimum 2000 dealer kaydı yüklenebilir. Dosyanızda 2500 kayıt var."
}
```

**400 Bad Request - Insufficient Codes:**
```json
{
  "success": false,
  "message": "Yetersiz kod. Gerekli: 3000, Mevcut: 1500 (L tier)"
}
```

---

### 4.2 İşlem Durumu Sorgulama

**Endpoint:** `GET /api/v1/sponsorship/dealer/bulk-status/{jobId}`

**Authorization:** Sponsor role (kendi jobları)

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "jobId": "hangfire-job-123",
    "status": "Processing",
    "totalDealers": 150,
    "processedDealers": 75,
    "successfulInvitations": 70,
    "failedInvitations": 5,
    "progressPercentage": 50.0,
    "startedDate": "2025-11-03T15:00:00Z",
    "estimatedCompletionTime": "2025-11-03T15:30:00Z"
  }
}
```

**Status Values:**
- `Pending` - İşlem kuyruğunda bekliyor
- `Processing` - İşlem devam ediyor
- `Completed` - Başarıyla tamamlandı
- `PartialSuccess` - Bazı kayıtlar başarısız
- `Failed` - İşlem tamamen başarısız

---

### 4.3 İşlem Sonucu İndirme

**Endpoint:** `GET /api/v1/sponsorship/dealer/bulk-result/{jobId}`

**Authorization:** Sponsor role

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "jobId": "hangfire-job-123",
    "status": "Completed",
    "totalDealers": 150,
    "successfulInvitations": 145,
    "failedInvitations": 5,
    "completedDate": "2025-11-03T15:25:00Z",
    "resultFileUrl": "https://cdn.ziraai.com/bulk-results/hangfire-job-123.xlsx",
    "summary": {
      "totalCodes": 3000,
      "totalSmsSent": 145,
      "errors": [
        {
          "rowNumber": 12,
          "email": "invalid@email",
          "error": "Geçersiz email formatı"
        },
        {
          "rowNumber": 45,
          "email": "existing@dealer.com",
          "error": "Bu email ile zaten bir dealer mevcut"
        }
      ]
    }
  }
}
```

---

### 4.4 İşlem Geçmişi

**Endpoint:** `GET /api/v1/sponsorship/dealer/bulk-history`

**Authorization:** Sponsor role

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 10, max: 100)
- `status` (string, optional): Filter by status

**Response (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "jobId": "hangfire-job-123",
      "originalFileName": "dealers_november.xlsx",
      "status": "Completed",
      "totalDealers": 150,
      "successfulInvitations": 145,
      "failedInvitations": 5,
      "createdDate": "2025-11-03T15:00:00Z",
      "completedDate": "2025-11-03T15:25:00Z"
    }
  ],
  "totalCount": 15,
  "page": 1,
  "pageSize": 10
}
```

---

## 5. Excel Dosya Formatı

### 5.1 Gerekli Sütunlar

| Sütun Adı | Zorunlu | Format | Açıklama |
|-----------|---------|--------|----------|
| Email | ✅ | email@example.com | Geçerli email formatı |
| Phone | ✅ | +905321234567 veya 05321234567 | Türkiye telefon numarası |
| DealerName | ✅ | Text (max 200 char) | Dealer firma/isim |
| CodeCount | ❌ | Integer (1-1000) | Özel kod sayısı (opsiyonel) |
| PackageTier | ❌ | S, M, L, XL | Özel tier (opsiyonel) |

### 5.2 Örnek Excel

**Sheet Name:** "Dealers" veya "Sheet1"

| Email | Phone | DealerName | CodeCount | PackageTier |
|-------|-------|------------|-----------|-------------|
| dealer1@example.com | +905321234567 | Ankara Tarım Bayi | 20 | L |
| dealer2@example.com | 05331234567 | İstanbul Tarım | 50 | XL |
| dealer3@example.com | +905551234567 | İzmir Tarım Merkezi |  |  |

**Notlar:**
- İlk satır başlık satırı olmalı (header row)
- Boş satırlar atlanır
- CodeCount boş ise `defaultCodeCount` kullanılır
- PackageTier boş ise `defaultTier` kullanılır
- Email ve Phone unique olmalı (aynı dosyada tekrar yok)

### 5.3 Örnek Template Dosyası

Sistem otomatik template oluşturmalı:

**Endpoint:** `GET /api/v1/sponsorship/dealer/bulk-template`

**Response:** Excel dosyası (Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet)

---

## 6. Validasyon Kuralları

### 6.1 Dosya Validasyonu

```csharp
public class FileValidationRules
{
    public const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    public const int MaxRowCount = 2000;
    public static readonly string[] AllowedExtensions = { ".xlsx", ".xls" };
    public static readonly string[] AllowedMimeTypes = {
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel"
    };
}
```

**Kontroller:**
1. Dosya boyutu ≤ 5 MB
2. Dosya uzantısı .xlsx veya .xls
3. MIME type kontrolü
4. Excel parse edilebiliyor mu?
5. Gerekli sütunlar mevcut mu?
6. En az 1 geçerli satır var mı?
7. Maksimum 2000 satır kontrolü

### 6.2 Satır Validasyonu

```csharp
public class RowValidationRules
{
    // Email Rules
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch { return false; }
    }

    // Phone Rules
    public static bool IsValidTurkishPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;

        // Normalize: remove spaces, dashes, parentheses
        var normalized = phone.Replace(" ", "").Replace("-", "")
                              .Replace("(", "").Replace(")", "");

        // Turkish formats:
        // +905321234567 (13 chars)
        // 905321234567 (12 chars)
        // 05321234567 (11 chars)

        if (normalized.StartsWith("+90") && normalized.Length == 13)
            return true;

        if (normalized.StartsWith("90") && normalized.Length == 12)
            return true;

        if (normalized.StartsWith("0") && normalized.Length == 11)
            return true;

        return false;
    }

    // DealerName Rules
    public static bool IsValidDealerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Length > 200) return false;
        return true;
    }

    // CodeCount Rules
    public static bool IsValidCodeCount(int? count)
    {
        if (!count.HasValue) return true; // Optional
        return count.Value >= 1 && count.Value <= 1000;
    }

    // Tier Rules
    public static bool IsValidTier(string tier)
    {
        if (string.IsNullOrWhiteSpace(tier)) return true; // Optional
        var validTiers = new[] { "S", "M", "L", "XL" };
        return validTiers.Contains(tier.ToUpper());
    }
}
```

### 6.3 İş Kuralları Validasyonu

**Pre-processing Checks:**

1. **Duplicate Email Check (Same File):**
```sql
SELECT Email, COUNT(*) as Count
FROM ParsedRows
GROUP BY Email
HAVING COUNT(*) > 1
```

2. **Duplicate Phone Check (Same File):**
```sql
SELECT Phone, COUNT(*) as Count
FROM ParsedRows
GROUP BY Phone
HAVING COUNT(*) > 1
```

3. **Existing Dealer Check (Database):**
```sql
SELECT u.Email, u.FullName
FROM Users u
INNER JOIN UserGroups ug ON u.UserId = ug.UserId
INNER JOIN Groups g ON ug.GroupId = g.Id
WHERE g.GroupName = 'Sponsor'
AND u.Email IN (@EmailList)
```

4. **Code Availability Check:**
```csharp
// Total codes needed
int totalCodesNeeded = rows.Sum(r => r.CodeCount ?? defaultCodeCount);

// Available codes per tier
var availableCodes = await _codeRepository.GetListAsync(c =>
    c.SponsorId == sponsorId &&
    !c.IsUsed &&
    c.DealerId == null &&
    c.ReservedForInvitationId == null &&
    c.ExpiryDate > DateTime.Now);

// Group by tier and check
var codesByTier = availableCodes.GroupBy(c => c.TierName)
    .ToDictionary(g => g.Key, g => g.Count());

// Validate sufficient codes
foreach (var row in rows)
{
    var tier = row.PackageTier ?? defaultTier;
    var count = row.CodeCount ?? defaultCodeCount;

    if (!codesByTier.ContainsKey(tier) || codesByTier[tier] < count)
    {
        throw new InsufficientCodesException(tier, count, codesByTier.GetValueOrDefault(tier, 0));
    }

    codesByTier[tier] -= count; // Reserve for next row
}
```

---

## 7. Hata Yönetimi

### 7.1 Hata Tipleri

**Critical Errors (İşlem Durur):**
- Dosya parse edilemiyor
- Gerekli sütunlar eksik
- Maksimum satır sayısı aşıldı
- Toplam kod sayısı yetersiz
- Database connection hatası

**Row-Level Errors (Satır Atlanır):**
- Geçersiz email formatı
- Geçersiz telefon formatı
- Dealer name eksik/uzun
- Duplicate email/phone (dosya içi)
- Mevcut dealer (database'de var)
- Geçersiz tier/code count

**Warning-Level Errors (İşlem Devam Eder):**
- SMS gönderim hatası
- SignalR bildirim hatası
- Log yazma hatası

### 7.2 Hata Mesajları

**Türkçe Kullanıcı Mesajları:**

```csharp
public static class BulkInvitationErrorMessages
{
    // File Errors
    public const string InvalidFileType = "Geçersiz dosya formatı. Sadece .xlsx ve .xls desteklenir.";
    public const string FileTooLarge = "Dosya boyutu çok büyük. Maksimum: 5 MB";
    public const string TooManyRows = "Maksimum 2000 dealer kaydı yüklenebilir. Dosyanızda {0} kayıt var.";
    public const string ParseError = "Excel dosyası okunamadı. Lütfen dosya formatını kontrol edin.";
    public const string MissingColumns = "Gerekli sütunlar eksik: {0}";
    public const string NoValidRows = "Dosyada geçerli satır bulunamadı.";

    // Row Errors
    public const string InvalidEmail = "Satır {0}: Geçersiz email formatı: {1}";
    public const string InvalidPhone = "Satır {0}: Geçersiz telefon numarası: {1}";
    public const string InvalidDealerName = "Satır {0}: Dealer ismi geçersiz veya çok uzun (max 200 karakter)";
    public const string DuplicateEmail = "Satır {0}: Bu email dosyada birden fazla kez kullanılmış: {1}";
    public const string DuplicatePhone = "Satır {0}: Bu telefon dosyada birden fazla kez kullanılmış: {1}";
    public const string ExistingDealer = "Satır {0}: Bu email ile zaten bir dealer mevcut: {1}";
    public const string InvalidTier = "Satır {0}: Geçersiz tier: {1}. Geçerli değerler: S, M, L, XL";
    public const string InvalidCodeCount = "Satır {0}: Geçersiz kod sayısı: {1}. Aralık: 1-1000";

    // Business Logic Errors
    public const string InsufficientCodes = "Yetersiz kod. Gerekli: {0}, Mevcut: {1} ({2} tier)";
    public const string DatabaseError = "Veritabanı hatası. Lütfen tekrar deneyin.";

    // Processing Errors
    public const string SmsSendFailed = "Satır {0}: SMS gönderilemedi: {1}";
    public const string InvitationCreationFailed = "Satır {0}: Davet oluşturulamadı: {1}";
}
```

### 7.3 Rollback Stratejisi

**Transaction Scope:**

```csharp
// Option 1: Per-Row Transaction (Recommended)
foreach (var row in validRows)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Create invitation
        var invitation = CreateInvitation(row);
        await _invitationRepository.SaveChangesAsync();

        // Reserve codes
        await ReserveCodes(invitation.Id, row.CodeCount);

        await transaction.CommitAsync();
        successCount++;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        failedRows.Add(new FailedRow { RowNumber = row.RowNumber, Error = ex.Message });
    }
}

// Option 2: All-or-Nothing Transaction (Strict)
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    foreach (var row in validRows)
    {
        // Create invitation
        var invitation = CreateInvitation(row);
        await _invitationRepository.SaveChangesAsync();

        // Reserve codes
        await ReserveCodes(invitation.Id, row.CodeCount);
    }

    await transaction.CommitAsync();
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    throw new BulkInvitationException("Toplu davet işlemi başarısız: " + ex.Message);
}
```

**Öneri:** **Option 1 (Per-Row Transaction)** kullanılmalı - partial success destekler.

---

## 8. İmplementasyon Planı

### 8.1 Aşama 1: Core Components (2-3 gün)

**8.1.1 Entity ve Repository**
- [ ] `BulkInvitationJob` entity oluşturulması
- [ ] `IBulkInvitationJobRepository` interface
- [ ] Entity Framework configuration
- [ ] Migration oluşturulması

**Dosyalar:**
- `Entities/Concrete/BulkInvitationJob.cs`
- `DataAccess/Abstract/IBulkInvitationJobRepository.cs`
- `DataAccess/Concrete/EntityFramework/BulkInvitationJobRepository.cs`
- `DataAccess/Concrete/Configurations/BulkInvitationJobEntityConfiguration.cs`
- `DataAccess/Migrations/Pg/AddBulkInvitationJob.cs`

**8.1.2 DTOs**
- [ ] `BulkInvitationRow` DTO
- [ ] `BulkInvitationRequest` DTO
- [ ] `BulkInvitationResponse` DTO
- [ ] `BulkInvitationStatusDto` DTO
- [ ] `BulkInvitationResultDto` DTO

**Dosyalar:**
- `Entities/Dtos/BulkInvitationDtos.cs`

---

### 8.2 Aşama 2: Excel Processing Service (1-2 gün)

**8.2.1 Excel Parser Service**
- [ ] `IExcelParserService` interface
- [ ] `ExcelParserService` implementation
- [ ] NuGet paketi: EPPlus veya NPOI
- [ ] Phone normalization utility
- [ ] Email validation utility

**Dosyalar:**
- `Business/Services/Excel/IExcelParserService.cs`
- `Business/Services/Excel/ExcelParserService.cs`
- `Core/Utilities/Validation/PhoneValidator.cs`
- `Core/Utilities/Validation/EmailValidator.cs`

**8.2.2 Validation Service**
- [ ] `IBulkInvitationValidationService` interface
- [ ] File validation (size, type, format)
- [ ] Row validation (email, phone, dealer name)
- [ ] Business rules validation (duplicates, existing dealers)
- [ ] Code availability check

**Dosyalar:**
- `Business/Services/Validation/IBulkInvitationValidationService.cs`
- `Business/Services/Validation/BulkInvitationValidationService.cs`

---

### 8.3 Aşama 3: CQRS Handlers (2-3 gün)

**8.3.1 Upload Command**
- [ ] `BulkDealerInvitationCommand`
- [ ] `BulkDealerInvitationCommandHandler`
- [ ] File upload handling
- [ ] Immediate validation
- [ ] Hangfire job creation
- [ ] Initial response

**Dosyalar:**
- `Business/Handlers/Sponsorship/Commands/BulkDealerInvitationCommand.cs`
- `Business/Handlers/Sponsorship/Commands/BulkDealerInvitationCommandHandler.cs`

**8.3.2 Background Processing**
- [ ] `ProcessBulkInvitationJob` (Hangfire job)
- [ ] Row-by-row processing
- [ ] Progress tracking
- [ ] Error collection
- [ ] SMS/SignalR notifications
- [ ] Result file generation

**Dosyalar:**
- `Business/BackgroundJobs/ProcessBulkInvitationJob.cs`

**8.3.3 Status Query**
- [ ] `GetBulkInvitationStatusQuery`
- [ ] `GetBulkInvitationStatusQueryHandler`
- [ ] Real-time progress tracking

**Dosyalar:**
- `Business/Handlers/Sponsorship/Queries/GetBulkInvitationStatusQuery.cs`
- `Business/Handlers/Sponsorship/Queries/GetBulkInvitationStatusQueryHandler.cs`

**8.3.4 Result Query**
- [ ] `GetBulkInvitationResultQuery`
- [ ] `GetBulkInvitationResultQueryHandler`
- [ ] Result Excel generation
- [ ] Error summary formatting

**Dosyalar:**
- `Business/Handlers/Sponsorship/Queries/GetBulkInvitationResultQuery.cs`
- `Business/Handlers/Sponsorship/Queries/GetBulkInvitationResultQueryHandler.cs`

**8.3.5 History Query**
- [ ] `GetBulkInvitationHistoryQuery`
- [ ] `GetBulkInvitationHistoryQueryHandler`
- [ ] Pagination support

**Dosyalar:**
- `Business/Handlers/Sponsorship/Queries/GetBulkInvitationHistoryQuery.cs`
- `Business/Handlers/Sponsorship/Queries/GetBulkInvitationHistoryQueryHandler.cs`

---

### 8.4 Aşama 4: API Controller (1 gün)

**8.4.1 Endpoints**
- [ ] POST `/api/v1/sponsorship/dealer/invite-bulk` - Upload
- [ ] GET `/api/v1/sponsorship/dealer/bulk-status/{jobId}` - Status
- [ ] GET `/api/v1/sponsorship/dealer/bulk-result/{jobId}` - Result
- [ ] GET `/api/v1/sponsorship/dealer/bulk-history` - History
- [ ] GET `/api/v1/sponsorship/dealer/bulk-template` - Template

**Dosyalar:**
- `WebAPI/Controllers/SponsorshipController.cs` (update existing)

---

### 8.5 Aşama 5: Testing (2-3 gün)

**8.5.1 Unit Tests**
- [ ] Excel parser tests
- [ ] Validation logic tests
- [ ] Phone/email normalization tests
- [ ] Business rules tests

**Dosyalar:**
- `Tests/Business/Services/ExcelParserServiceTests.cs`
- `Tests/Business/Services/BulkInvitationValidationServiceTests.cs`

**8.5.2 Integration Tests**
- [ ] Full upload flow test
- [ ] Error handling tests
- [ ] Progress tracking tests
- [ ] Result generation tests

**Dosyalar:**
- `Tests/Integration/BulkInvitationIntegrationTests.cs`

**8.5.3 Performance Tests**
- [ ] 100 dealers - < 30 seconds
- [ ] 500 dealers - < 2 minutes
- [ ] 2000 dealers - < 5 minutes
- [ ] Memory profiling

---

### 8.6 Aşama 6: Documentation (1 gün)

**8.6.1 API Documentation**
- [ ] Swagger annotations
- [ ] Postman collection update
- [ ] Example Excel files

**8.6.2 User Guide**
- [ ] Excel format guide
- [ ] Error handling guide
- [ ] FAQ document

**Dosyalar:**
- `claudedocs/BULK_DEALER_INVITATION_USER_GUIDE.md`
- `claudedocs/Dealers/template.xlsx`
- `claudedocs/Dealers/example_success.xlsx`
- `claudedocs/Dealers/example_errors.xlsx`

---

## 9. Teknik Detaylar

### 9.1 NuGet Dependencies

```xml
<!-- Excel Processing -->
<PackageReference Include="EPPlus" Version="7.0.0" />
<!-- Alternative: NPOI for .xls and .xlsx support -->

<!-- Background Jobs -->
<PackageReference Include="Hangfire.Core" Version="1.8.6" />
<PackageReference Include="Hangfire.PostgreSql" Version="1.20.0" />

<!-- File Validation -->
<PackageReference Include="FluentValidation" Version="11.8.0" />
```

### 9.2 Configuration (appsettings.json)

```json
{
  "BulkInvitation": {
    "MaxFileSizeBytes": 5242880,
    "MaxRowCount": 2000,
    "AllowedExtensions": [".xlsx", ".xls"],
    "DefaultInvitationExpiryDays": 7,
    "EnableSmsNotifications": true,
    "EnableSignalRNotifications": true,
    "ResultFileRetentionDays": 30,
    "ProcessingTimeout": 300000,
    "BatchSize": 50
  },
  "Hangfire": {
    "DashboardEnabled": true,
    "DashboardPath": "/hangfire",
    "WorkerCount": 5
  }
}
```

### 9.3 Database Schema

**Table: BulkInvitationJobs**

```sql
CREATE TABLE "BulkInvitationJobs" (
    "Id" SERIAL PRIMARY KEY,
    "JobId" VARCHAR(100) NOT NULL UNIQUE,
    "SponsorId" INTEGER NOT NULL,
    "InvitationType" VARCHAR(20) NOT NULL,
    "DefaultTier" VARCHAR(10),
    "DefaultCodeCount" INTEGER NOT NULL,
    "SendSms" BOOLEAN NOT NULL DEFAULT true,
    "TotalDealers" INTEGER NOT NULL,
    "ProcessedDealers" INTEGER NOT NULL DEFAULT 0,
    "SuccessfulInvitations" INTEGER NOT NULL DEFAULT 0,
    "FailedInvitations" INTEGER NOT NULL DEFAULT 0,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "CreatedDate" TIMESTAMP NOT NULL,
    "StartedDate" TIMESTAMP,
    "CompletedDate" TIMESTAMP,
    "ResultFileUrl" VARCHAR(500),
    "ErrorSummary" TEXT,
    "OriginalFileName" VARCHAR(255),
    "FileSize" INTEGER,
    FOREIGN KEY ("SponsorId") REFERENCES "Users"("UserId")
);

CREATE INDEX "IX_BulkInvitationJobs_SponsorId" ON "BulkInvitationJobs"("SponsorId");
CREATE INDEX "IX_BulkInvitationJobs_Status" ON "BulkInvitationJobs"("Status");
CREATE INDEX "IX_BulkInvitationJobs_CreatedDate" ON "BulkInvitationJobs"("CreatedDate");
```

---

## 10. Örnek Kullanım Senaryoları

### 10.1 Senaryo 1: Basit Toplu Davet

**Durum:** Sponsor 50 dealer'a aynı tier ve kod sayısı ile davet göndermek istiyor.

**Excel Dosyası:**
```
Email                    | Phone          | DealerName
dealer1@example.com      | +905321234567  | Ankara Tarım
dealer2@example.com      | 05331234567    | İstanbul Tarım
...
```

**API Request:**
```bash
curl -X POST https://ziraai.com/api/v1/sponsorship/dealer/invite-bulk \
  -H "Authorization: Bearer {token}" \
  -F "excelFile=@dealers.xlsx" \
  -F "invitationType=Invite" \
  -F "defaultTier=L" \
  -F "defaultCodeCount=20" \
  -F "sendSms=true"
```

**Response:**
```json
{
  "success": true,
  "message": "Toplu davet işlemi başlatıldı",
  "data": {
    "jobId": "hangfire-job-123",
    "totalDealers": 50
  }
}
```

---

### 10.2 Senaryo 2: Özelleştirilmiş Kod Sayıları

**Durum:** Her dealer için farklı kod sayısı belirtmek istiyor.

**Excel Dosyası:**
```
Email                    | Phone          | DealerName         | CodeCount
dealer1@example.com      | +905321234567  | Ankara Tarım       | 10
dealer2@example.com      | 05331234567    | İstanbul Tarım     | 50
dealer3@example.com      | 05551234567    | İzmir Tarım        | 25
```

**API Request:**
```bash
curl -X POST https://ziraai.com/api/v1/sponsorship/dealer/invite-bulk \
  -H "Authorization: Bearer {token}" \
  -F "excelFile=@dealers.xlsx" \
  -F "invitationType=AutoCreate" \
  -F "defaultTier=L" \
  -F "defaultCodeCount=20" \
  -F "useRowSpecificCounts=true"
```

---

### 10.3 Senaryo 3: Progress Tracking

**Status Check:**
```bash
curl -X GET https://ziraai.com/api/v1/sponsorship/dealer/bulk-status/hangfire-job-123 \
  -H "Authorization: Bearer {token}"
```

**Response (Processing):**
```json
{
  "success": true,
  "data": {
    "jobId": "hangfire-job-123",
    "status": "Processing",
    "totalDealers": 50,
    "processedDealers": 25,
    "successfulInvitations": 23,
    "failedInvitations": 2,
    "progressPercentage": 50.0
  }
}
```

**Response (Completed):**
```json
{
  "success": true,
  "data": {
    "jobId": "hangfire-job-123",
    "status": "PartialSuccess",
    "totalDealers": 50,
    "processedDealers": 50,
    "successfulInvitations": 48,
    "failedInvitations": 2,
    "progressPercentage": 100.0,
    "resultFileUrl": "https://cdn.ziraai.com/bulk-results/hangfire-job-123.xlsx"
  }
}
```

---

## 11. Güvenlik Önlemleri

### 11.1 File Security

1. **File Type Validation:**
   - MIME type check
   - Extension whitelist
   - Magic number validation (file signature)

2. **Virus Scanning:**
   - Production ortamında antivirus taraması (ClamAV gibi)
   - Şüpheli dosyalar karantinaya alınır

3. **Upload Limits:**
   - Dosya boyutu: 5 MB
   - Satır sayısı: 2000
   - Eşzamanlı upload limiti: 3 per sponsor

### 11.2 Data Security

1. **Email/Phone Privacy:**
   - AutoCreate şifreleri hashlenmiş saklanır
   - Result dosyaları şifreli saklanır
   - 30 gün sonra otomatik silinir

2. **Authorization:**
   - Sponsor sadece kendi job'larını görebilir
   - Admin tüm job'ları görebilir

3. **Rate Limiting:**
   - 5 upload per hour per sponsor
   - 100 status check per minute per sponsor

### 11.3 Injection Protection

1. **SQL Injection:**
   - Parameterized queries kullanılır
   - EF Core ORM kullanımı

2. **Command Injection:**
   - User input sanitization
   - Whitelist-based validation

---

## 12. Performans Optimizasyonları

### 12.1 Database Optimizations

1. **Batch Insert:**
```csharp
// Instead of individual inserts
var invitations = validRows.Select(r => CreateInvitation(r)).ToList();
await _invitationRepository.AddRangeAsync(invitations);
await _invitationRepository.SaveChangesAsync();
```

2. **Connection Pooling:**
```csharp
services.AddDbContext<ProjectDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MinBatchSize(2);
        npgsqlOptions.MaxBatchSize(100);
    });
}, ServiceLifetime.Scoped);
```

3. **Indexed Queries:**
```sql
CREATE INDEX "IX_DealerInvitations_SponsorId_Status" ON "DealerInvitations"("SponsorId", "Status");
CREATE INDEX "IX_SponsorshipCodes_SponsorId_Available" ON "SponsorshipCodes"("SponsorId", "IsUsed", "DealerId");
```

### 12.2 Memory Optimizations

1. **Streaming Excel Read:**
```csharp
// Use streaming read for large files
using var stream = excelFile.OpenReadStream();
using var package = new ExcelPackage(stream);
var worksheet = package.Workbook.Worksheets[0];

// Process in batches
for (int row = 2; row <= worksheet.Dimension.End.Row; row += 100)
{
    var batch = ReadRowBatch(worksheet, row, 100);
    await ProcessBatch(batch);
}
```

2. **Result Streaming:**
```csharp
// Stream result file instead of loading into memory
return File(resultStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    $"result_{jobId}.xlsx");
```

### 12.3 Async Operations

1. **Non-blocking SMS:**
```csharp
// Fire and forget SMS
_ = Task.Run(async () =>
{
    try { await SendSmsAsync(phone, message); }
    catch (Exception ex) { _logger.LogWarning(ex, "SMS failed"); }
});
```

2. **Parallel Processing (dikkatli kullanım):**
```csharp
// Only for independent operations
var tasks = validRows.Select(async row =>
{
    using var scope = _serviceProvider.CreateScope();
    var handler = scope.ServiceProvider.GetRequiredService<IInvitationProcessor>();
    return await handler.ProcessRowAsync(row);
});

var results = await Task.WhenAll(tasks);
```

---

## 13. Monitoring ve Logging

### 13.1 Structured Logging

```csharp
_logger.LogInformation(
    "📨 Bulk invitation started. JobId: {JobId}, SponsorId: {SponsorId}, TotalDealers: {TotalDealers}",
    jobId, sponsorId, totalDealers);

_logger.LogWarning(
    "⚠️ Row {RowNumber} validation failed. Email: {Email}, Error: {Error}",
    rowNumber, email, error);

_logger.LogError(ex,
    "❌ Bulk invitation job failed. JobId: {JobId}, ProcessedRows: {ProcessedRows}",
    jobId, processedRows);
```

### 13.2 Metrics Collection

```csharp
public class BulkInvitationMetrics
{
    public static readonly Counter ProcessedInvitations = Metrics.CreateCounter(
        "bulk_invitation_processed_total",
        "Total bulk invitations processed");

    public static readonly Histogram ProcessingDuration = Metrics.CreateHistogram(
        "bulk_invitation_duration_seconds",
        "Bulk invitation processing duration");

    public static readonly Gauge ActiveJobs = Metrics.CreateGauge(
        "bulk_invitation_active_jobs",
        "Number of active bulk invitation jobs");
}
```

### 13.3 Hangfire Dashboard

**URL:** `/hangfire`

**Features:**
- Real-time job monitoring
- Failed job retry
- Job history
- Performance statistics

---

## 14. Sonuç

Bu tasarım, toplu dealer davet sistemi için kapsamlı bir çözüm sunmaktadır:

### Temel Özellikler
✅ Excel upload ile toplu davet
✅ Maksimum 2000 dealer limit
✅ Esnek tier ve kod sayısı
✅ Asenkron background processing
✅ Real-time progress tracking
✅ Partial success desteği
✅ Detaylı error reporting
✅ Result file download

### Güvenlik
✅ File type validation
✅ SQL injection protection
✅ Authorization controls
✅ Rate limiting

### Performans
✅ Streaming Excel read
✅ Batch database operations
✅ Connection pooling
✅ Async SMS/SignalR

### Developer Experience
✅ CQRS pattern
✅ Clean architecture
✅ Comprehensive logging
✅ Unit test coverage
✅ Swagger documentation

**Tahmini Geliştirme Süresi:** 10-12 iş günü
**Karmaşıklık:** Orta-Yüksek
**Öncelik:** Yüksek (toplu işlem kritik özellik)

---

**İlgili Dokümanlar:**
- [SPONSOR_WEB_SCENARIOS.md](./SPONSOR_WEB_SCENARIOS.md)
- [Dealer Invitations Architecture](../memories/dealer_invitations_architecture.md)
- [Sponsorship System Documentation](./SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md)
