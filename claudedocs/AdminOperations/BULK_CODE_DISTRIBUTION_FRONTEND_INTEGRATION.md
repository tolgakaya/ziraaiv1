# Bulk Code Distribution - Frontend Entegrasyon Kılavuzu

## İçindekiler
1. [Genel Bakış](#genel-bakış)
2. [Endpoint Listesi](#endpoint-listesi)
3. [Kullanım Senaryoları](#kullanım-senaryoları)
4. [Request/Response Detayları](#requestresponse-detayları)
5. [Örnek Kod (TypeScript/JavaScript)](#örnek-kod-typescriptjavascript)
6. [Hata Yönetimi](#hata-yönetimi)
7. [UI/UX Önerileri](#uiux-önerileri)

---

## Genel Bakış

### Sistem Akışı
```
┌─────────────────┐
│ 1. Excel Yükle  │ → POST /api/v1/sponsorship/bulk-code-distribution
└────────┬────────┘
         │ Response: { jobId: 123, status: "Pending", ... }
         ↓
┌─────────────────┐
│ 2. Durum Takibi │ → GET /api/v1/sponsorship/bulk-code-distribution/status/{jobId}
└────────┬────────┘   (Polling: Her 3-5 saniyede bir)
         │ Response: { status: "Processing", progressPercentage: 45, ... }
         ↓
┌─────────────────┐
│ 3. Tamamlandı   │ → Status: "Completed" veya "PartialSuccess"
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│ 4. Sonuç İndir  │ → GET /api/v1/sponsorship/bulk-code-distribution/{jobId}/result
└─────────────────┘   (Excel dosyası)

┌─────────────────┐
│ 5. Geçmiş Görüntüle │ → GET /api/admin/sponsorship/bulk-code-distribution/history
└─────────────────┘      (Admin için tüm işler)
```

### Roller ve Yetkiler

| Endpoint | Sponsor | Admin | Açıklama |
|----------|---------|-------|----------|
| POST .../bulk-code-distribution | ✅ | ✅ | Sponsor kendi adına, Admin başkası adına |
| GET .../status/{jobId} | ✅ (kendi) | ✅ (tümü) | Sponsor sadece kendi işlerini görür |
| GET .../history | ✅ (kendi) | ✅ (tümü) | Admin tüm sponsor'ları filtreleyebilir |
| GET .../result | ✅ (kendi) | ✅ (tümü) | Sonuç dosyasını indir |
| **GET /api/admin/.../history** | ❌ | ✅ | **YENİ**: Admin dashboard için |

---

## Endpoint Listesi

### 1. Excel Yükleme (Job Oluşturma)
```
POST /api/v1/sponsorship/bulk-code-distribution
```
- **Amaç**: Toplu kod dağıtımı için Excel dosyası yükle
- **Yetki**: Sponsor (kendi adına), Admin (başkası adına)
- **Content-Type**: `multipart/form-data`

### 2. Job Durumu Sorgulama (Polling)
```
GET /api/v1/sponsorship/bulk-code-distribution/status/{jobId}
```
- **Amaç**: İşlemin anlık durumunu öğren
- **Kullanım**: Polling için (her 3-5 saniye)
- **Yetki**: Sponsor (kendi işleri), Admin (tüm işler)

### 3. Job Geçmişi Listeleme (Sponsor)
```
GET /api/v1/sponsorship/bulk-code-distribution/history
```
- **Amaç**: Sponsor'un kendi geçmiş işlerini listele
- **Yetki**: Sponsor (kendi), Admin (sponsorId parametresi gerekli)

### 4. Job Geçmişi Listeleme (Admin Dashboard) ⭐ YENİ
```
GET /api/admin/sponsorship/bulk-code-distribution/history
```
- **Amaç**: Admin dashboard için gelişmiş filtreleme ve raporlama
- **Yetki**: Sadece Admin
- **Özellikler**: Sponsor bilgileri dahil, detaylı filtreleme

### 5. Sonuç Dosyası İndirme
```
GET /api/v1/sponsorship/bulk-code-distribution/{jobId}/result
```
- **Amaç**: İşlem sonuç Excel dosyasını indir
- **Format**: Excel (.xlsx)

---

## Kullanım Senaryoları

### Senaryo 1: Sponsor - Excel Yükleme ve Takip

#### Adım 1: Excel Yükleme
```typescript
// 1. Excel dosyasını FormData ile hazırla
const formData = new FormData();
formData.append('ExcelFile', selectedFile);
formData.append('SendSms', 'true'); // veya 'false'

// 2. API isteği gönder
const response = await fetch('/api/v1/sponsorship/bulk-code-distribution', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`
  },
  body: formData
});

const result = await response.json();
// result.data.jobId -> Polling için sakla
// result.data.statusCheckUrl -> Polling URL'i
```

**Beklenen Response:**
```json
{
  "data": {
    "jobId": 123,
    "totalFarmers": 150,
    "totalCodesRequired": 150,
    "availableCodes": 200,
    "status": "Pending",
    "createdDate": "2025-11-09T10:30:00Z",
    "estimatedCompletionTime": "2025-11-09T10:35:00Z",
    "statusCheckUrl": "/api/v1/sponsorship/bulk-code-distribution/status/123"
  },
  "success": true,
  "message": "Job başarıyla oluşturuldu. 150 çiftçiye kod dağıtımı başlatıldı."
}
```

#### Adım 2: Durum Takibi (Polling)
```typescript
// Her 3-5 saniyede bir çalışacak polling fonksiyonu
async function pollJobStatus(jobId: number) {
  const response = await fetch(
    `/api/v1/sponsorship/bulk-code-distribution/status/${jobId}`,
    {
      headers: { 'Authorization': `Bearer ${token}` }
    }
  );

  const result = await response.json();
  return result.data;
}

// Polling döngüsü
const intervalId = setInterval(async () => {
  const status = await pollJobStatus(123);

  // UI güncelleme
  updateProgressBar(status.progressPercentage);
  updateStatusText(status.status);

  // Tamamlandıysa polling'i durdur
  if (['Completed', 'PartialSuccess', 'Failed'].includes(status.status)) {
    clearInterval(intervalId);
    onJobComplete(status);
  }
}, 3000); // 3 saniye
```

**Polling Response Örneği:**
```json
{
  "data": {
    "jobId": 123,
    "status": "Processing",
    "totalFarmers": 150,
    "processedFarmers": 75,
    "successfulDistributions": 70,
    "failedDistributions": 5,
    "progressPercentage": 50,
    "totalCodesDistributed": 70,
    "totalSmsSent": 70,
    "createdDate": "2025-11-09T10:30:00Z",
    "startedDate": "2025-11-09T10:30:05Z",
    "completedDate": null,
    "estimatedTimeRemaining": "00:02:30"
  },
  "success": true,
  "message": "Job durumu başarıyla alındı."
}
```

#### Adım 3: Sonuç İndirme
```typescript
async function downloadResult(jobId: number) {
  const response = await fetch(
    `/api/v1/sponsorship/bulk-code-distribution/${jobId}/result`,
    {
      headers: { 'Authorization': `Bearer ${token}` }
    }
  );

  if (!response.ok) {
    throw new Error('Sonuç dosyası henüz hazır değil');
  }

  // Excel dosyasını indir
  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `bulk_distribution_result_${jobId}.xlsx`;
  a.click();
}
```

---

### Senaryo 2: Admin - Başka Sponsor Adına İşlem

```typescript
// Admin, sponsorId=456 olan sponsor adına işlem başlatıyor
const formData = new FormData();
formData.append('ExcelFile', selectedFile);
formData.append('SendSms', 'true');

const response = await fetch(
  '/api/v1/sponsorship/bulk-code-distribution?onBehalfOfSponsorId=456',
  {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${adminToken}`
    },
    body: formData
  }
);
```

**⚠️ ÖNEMLİ**: Admin role sahip kullanıcı bu endpoint'i kullanırken **mutlaka** `onBehalfOfSponsorId` parametresi göndermeli. Aksi halde 400 Bad Request hatası alır.

---

### Senaryo 3: Admin Dashboard - Gelişmiş Job Geçmişi ⭐ YENİ

#### Use Case 1: Tüm İşleri Listele
```typescript
async function getAllJobs(page = 1, pageSize = 50) {
  const response = await fetch(
    `/api/admin/sponsorship/bulk-code-distribution/history?page=${page}&pageSize=${pageSize}`,
    {
      headers: { 'Authorization': `Bearer ${adminToken}` }
    }
  );

  return await response.json();
}
```

**Response:**
```json
{
  "data": {
    "totalCount": 245,
    "page": 1,
    "pageSize": 50,
    "totalPages": 5,
    "jobs": [
      {
        "jobId": 150,
        "sponsorId": 456,
        "sponsorName": "Ahmet Yılmaz",
        "sponsorEmail": "ahmet@example.com",
        "purchaseId": 789,
        "deliveryMethod": "Both",
        "totalFarmers": 200,
        "processedFarmers": 200,
        "successfulDistributions": 195,
        "failedDistributions": 5,
        "status": "Completed",
        "createdDate": "2025-11-09T08:15:00Z",
        "startedDate": "2025-11-09T08:15:10Z",
        "completedDate": "2025-11-09T08:22:30Z",
        "originalFileName": "ciftciler_kasim.xlsx",
        "fileSize": 87432,
        "resultFileUrl": "https://storage.example.com/results/job_150.xlsx",
        "totalCodesDistributed": 195,
        "totalSmsSent": 195
      }
      // ... 49 more jobs
    ]
  },
  "success": true,
  "message": "Retrieved 50 jobs (Page 1/5, Total: 245)"
}
```

#### Use Case 2: Belirli Sponsor'ın İşlerini Filtrele
```typescript
async function getSponsorJobs(sponsorId: number) {
  const response = await fetch(
    `/api/admin/sponsorship/bulk-code-distribution/history?sponsorId=${sponsorId}&page=1&pageSize=20`,
    {
      headers: { 'Authorization': `Bearer ${adminToken}` }
    }
  );

  return await response.json();
}

// Kullanım
const sponsorJobs = await getSponsorJobs(456);
console.log(`${sponsorJobs.data.sponsorName} için ${sponsorJobs.data.totalCount} iş bulundu`);
```

#### Use Case 3: Tamamlanmış İşleri Listele
```typescript
async function getCompletedJobs() {
  const response = await fetch(
    `/api/admin/sponsorship/bulk-code-distribution/history?status=Completed&page=1&pageSize=100`,
    {
      headers: { 'Authorization': `Bearer ${adminToken}` }
    }
  );

  return await response.json();
}
```

#### Use Case 4: Tarih Aralığına Göre Filtrele
```typescript
async function getJobsByDateRange(startDate: string, endDate: string) {
  const params = new URLSearchParams({
    startDate: startDate, // "2025-11-01T00:00:00Z"
    endDate: endDate,     // "2025-11-09T23:59:59Z"
    page: '1',
    pageSize: '50'
  });

  const response = await fetch(
    `/api/admin/sponsorship/bulk-code-distribution/history?${params}`,
    {
      headers: { 'Authorization': `Bearer ${adminToken}` }
    }
  );

  return await response.json();
}

// Kullanım: Bu ayın işlerini getir
const thisMonth = await getJobsByDateRange(
  '2025-11-01T00:00:00Z',
  '2025-11-30T23:59:59Z'
);
```

#### Use Case 5: Kombine Filtreleme
```typescript
async function getFilteredJobs(filters: {
  sponsorId?: number;
  status?: string;
  startDate?: string;
  endDate?: string;
  page?: number;
  pageSize?: number;
}) {
  const params = new URLSearchParams();

  if (filters.sponsorId) params.append('sponsorId', filters.sponsorId.toString());
  if (filters.status) params.append('status', filters.status);
  if (filters.startDate) params.append('startDate', filters.startDate);
  if (filters.endDate) params.append('endDate', filters.endDate);
  params.append('page', (filters.page || 1).toString());
  params.append('pageSize', (filters.pageSize || 50).toString());

  const response = await fetch(
    `/api/admin/sponsorship/bulk-code-distribution/history?${params}`,
    {
      headers: { 'Authorization': `Bearer ${adminToken}` }
    }
  );

  return await response.json();
}

// Örnek: "Ahmet Yılmaz" sponsor'unun bu haftaki başarılı işleri
const result = await getFilteredJobs({
  sponsorId: 456,
  status: 'Completed',
  startDate: '2025-11-04T00:00:00Z',
  endDate: '2025-11-09T23:59:59Z',
  page: 1,
  pageSize: 20
});
```

---

## Request/Response Detayları

### 1. POST - Excel Yükleme

#### Request
**Headers:**
```
Authorization: Bearer {token}
Content-Type: multipart/form-data
```

**Body (FormData):**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| ExcelFile | File | ✅ | Excel dosyası (.xlsx, .xls) |
| SendSms | boolean | ✅ | SMS gönderilsin mi? (true/false) |

**Query Parameters (Admin için):**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| onBehalfOfSponsorId | int | ✅ (Admin) | Hangi sponsor adına işlem yapılacak |

**Excel Dosya Formatı:**
```
| Email              | Phone          | Name           |
|--------------------|----------------|----------------|
| farmer1@test.com   | 905551234567   | Ali Çiftçi     |
| farmer2@test.com   | 905559876543   | Ayşe Tarımcı   |
```

#### Response (Success - 200 OK)
```json
{
  "data": {
    "jobId": 123,
    "totalFarmers": 150,
    "totalCodesRequired": 150,
    "availableCodes": 200,
    "status": "Pending",
    "createdDate": "2025-11-09T10:30:00Z",
    "estimatedCompletionTime": "2025-11-09T10:35:00Z",
    "statusCheckUrl": "/api/v1/sponsorship/bulk-code-distribution/status/123"
  },
  "success": true,
  "message": "Job başarıyla oluşturuldu. 150 çiftçiye kod dağıtımı başlatıldı."
}
```

#### Response (Error - 400 Bad Request)
```json
{
  "success": false,
  "message": "Yetersiz kod sayısı. Gerekli: 150, Mevcut: 100. Lütfen yeni paket satın alın."
}
```

**Olası Hatalar:**
- Excel dosyası eksik
- Geçersiz Excel formatı
- Yetersiz kod sayısı
- Admin için sponsorId eksik
- Maksimum 2000 çiftçi sınırı aşıldı

---

### 2. GET - Job Durumu (Polling)

#### Request
```
GET /api/v1/sponsorship/bulk-code-distribution/status/{jobId}
```

**Headers:**
```
Authorization: Bearer {token}
```

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| jobId | int | Job ID (Excel yükleme sonucu alınan) |

#### Response (Processing - 200 OK)
```json
{
  "data": {
    "jobId": 123,
    "status": "Processing",
    "totalFarmers": 150,
    "processedFarmers": 75,
    "successfulDistributions": 70,
    "failedDistributions": 5,
    "progressPercentage": 50,
    "totalCodesDistributed": 70,
    "totalSmsSent": 70,
    "createdDate": "2025-11-09T10:30:00Z",
    "startedDate": "2025-11-09T10:30:05Z",
    "completedDate": null,
    "estimatedTimeRemaining": "00:02:30"
  },
  "success": true,
  "message": "Job durumu başarıyla alındı."
}
```

#### Response (Completed - 200 OK)
```json
{
  "data": {
    "jobId": 123,
    "status": "Completed",
    "totalFarmers": 150,
    "processedFarmers": 150,
    "successfulDistributions": 148,
    "failedDistributions": 2,
    "progressPercentage": 100,
    "totalCodesDistributed": 148,
    "totalSmsSent": 148,
    "createdDate": "2025-11-09T10:30:00Z",
    "startedDate": "2025-11-09T10:30:05Z",
    "completedDate": "2025-11-09T10:35:20Z",
    "estimatedTimeRemaining": null,
    "resultFileUrl": "https://storage.example.com/results/job_123.xlsx"
  },
  "success": true,
  "message": "Job başarıyla tamamlandı. 148/150 çiftçiye kod dağıtıldı."
}
```

**Status Değerleri:**
| Status | Açıklama | UI Durumu |
|--------|----------|-----------|
| `Pending` | İşlem sırada bekliyor | Spinner göster |
| `Processing` | İşlem devam ediyor | Progress bar göster |
| `Completed` | Tamamlandı (100% başarılı) | ✅ Yeşil badge |
| `PartialSuccess` | Kısmen başarılı (bazı hatalar var) | ⚠️ Sarı badge |
| `Failed` | Tamamen başarısız | ❌ Kırmızı badge |

---

### 3. GET - Job Geçmişi (Admin Dashboard) ⭐ YENİ

#### Request
```
GET /api/admin/sponsorship/bulk-code-distribution/history
```

**Headers:**
```
Authorization: Bearer {adminToken}
```

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | int | ❌ | 1 | Sayfa numarası |
| pageSize | int | ❌ | 50 | Sayfa başına kayıt |
| status | string | ❌ | null | Filtre: Pending, Processing, Completed, PartialSuccess, Failed |
| sponsorId | int | ❌ | null | Belirli sponsor'ın işleri |
| startDate | DateTime | ❌ | null | Başlangıç tarihi (ISO 8601) |
| endDate | DateTime | ❌ | null | Bitiş tarihi (ISO 8601) |

**Örnek URL'ler:**
```
# Tüm işler (varsayılan)
/api/admin/sponsorship/bulk-code-distribution/history

# Sayfa 2, sayfa başına 20 kayıt
/api/admin/sponsorship/bulk-code-distribution/history?page=2&pageSize=20

# Sadece tamamlanmış işler
/api/admin/sponsorship/bulk-code-distribution/history?status=Completed

# Belirli sponsor (ID: 456)
/api/admin/sponsorship/bulk-code-distribution/history?sponsorId=456

# Tarih aralığı
/api/admin/sponsorship/bulk-code-distribution/history?startDate=2025-11-01T00:00:00Z&endDate=2025-11-09T23:59:59Z

# Kombine filtre
/api/admin/sponsorship/bulk-code-distribution/history?sponsorId=456&status=Completed&page=1&pageSize=10
```

#### Response (Success - 200 OK)
```json
{
  "data": {
    "totalCount": 245,
    "page": 1,
    "pageSize": 50,
    "totalPages": 5,
    "jobs": [
      {
        "jobId": 150,
        "sponsorId": 456,
        "sponsorName": "Ahmet Yılmaz",
        "sponsorEmail": "ahmet@example.com",
        "purchaseId": 789,
        "deliveryMethod": "Both",
        "totalFarmers": 200,
        "processedFarmers": 200,
        "successfulDistributions": 195,
        "failedDistributions": 5,
        "status": "Completed",
        "createdDate": "2025-11-09T08:15:00Z",
        "startedDate": "2025-11-09T08:15:10Z",
        "completedDate": "2025-11-09T08:22:30Z",
        "originalFileName": "ciftciler_kasim.xlsx",
        "fileSize": 87432,
        "resultFileUrl": "https://storage.example.com/results/job_150.xlsx",
        "totalCodesDistributed": 195,
        "totalSmsSent": 195
      },
      {
        "jobId": 149,
        "sponsorId": 789,
        "sponsorName": "Mehmet Demir",
        "sponsorEmail": "mehmet@example.com",
        "purchaseId": 790,
        "deliveryMethod": "SMS",
        "totalFarmers": 100,
        "processedFarmers": 100,
        "successfulDistributions": 98,
        "failedDistributions": 2,
        "status": "PartialSuccess",
        "createdDate": "2025-11-08T15:20:00Z",
        "startedDate": "2025-11-08T15:20:08Z",
        "completedDate": "2025-11-08T15:24:15Z",
        "originalFileName": "toplu_dagitim.xlsx",
        "fileSize": 42150,
        "resultFileUrl": "https://storage.example.com/results/job_149.xlsx",
        "totalCodesDistributed": 98,
        "totalSmsSent": 98
      }
      // ... 48 more jobs
    ]
  },
  "success": true,
  "message": "Retrieved 50 jobs (Page 1/5, Total: 245)"
}
```

**Response Fields Açıklaması:**

**Pagination:**
- `totalCount`: Filtrelere uyan toplam iş sayısı
- `page`: Mevcut sayfa numarası
- `pageSize`: Sayfa başına kayıt sayısı
- `totalPages`: Toplam sayfa sayısı

**Job Fields:**
- `jobId`: İş ID (int, NOT Guid)
- `sponsorId`: Sponsor kullanıcı ID
- `sponsorName`: Sponsor adı (User tablosundan)
- `sponsorEmail`: Sponsor email (User tablosundan)
- `purchaseId`: Kullanılan satın alma paketi ID
- `deliveryMethod`: "Direct", "SMS", "Both"
- `totalFarmers`: Toplam çiftçi sayısı
- `processedFarmers`: İşlenen çiftçi sayısı
- `successfulDistributions`: Başarılı dağıtım sayısı
- `failedDistributions`: Başarısız dağıtım sayısı
- `status`: Job durumu
- `createdDate`: Oluşturulma tarihi
- `startedDate`: Başlama tarihi (nullable)
- `completedDate`: Tamamlanma tarihi (nullable)
- `originalFileName`: Yüklenen Excel dosya adı
- `fileSize`: Dosya boyutu (bytes)
- `resultFileUrl`: Sonuç dosyası URL (nullable)
- `totalCodesDistributed`: Toplam dağıtılan kod
- `totalSmsSent`: Toplam gönderilen SMS

---

## Örnek Kod (TypeScript/JavaScript)

### React Component Örneği - Job Geçmişi Tablosu

```typescript
import React, { useState, useEffect } from 'react';
import { format } from 'date-fns';

interface BulkJob {
  jobId: number;
  sponsorId: number;
  sponsorName: string;
  sponsorEmail: string;
  totalFarmers: number;
  successfulDistributions: number;
  failedDistributions: number;
  status: string;
  createdDate: string;
  completedDate: string | null;
  originalFileName: string;
}

interface JobHistoryResponse {
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  jobs: BulkJob[];
}

const BulkJobHistoryTable: React.FC = () => {
  const [data, setData] = useState<JobHistoryResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState({
    page: 1,
    pageSize: 20,
    status: '',
    sponsorId: null as number | null,
    startDate: '',
    endDate: ''
  });

  useEffect(() => {
    fetchJobs();
  }, [filters]);

  const fetchJobs = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      params.append('page', filters.page.toString());
      params.append('pageSize', filters.pageSize.toString());
      if (filters.status) params.append('status', filters.status);
      if (filters.sponsorId) params.append('sponsorId', filters.sponsorId.toString());
      if (filters.startDate) params.append('startDate', filters.startDate);
      if (filters.endDate) params.append('endDate', filters.endDate);

      const response = await fetch(
        `/api/admin/sponsorship/bulk-code-distribution/history?${params}`,
        {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('adminToken')}`
          }
        }
      );

      const result = await response.json();
      setData(result.data);
    } catch (error) {
      console.error('Job geçmişi yüklenirken hata:', error);
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status: string) => {
    const badges: Record<string, { color: string; text: string }> = {
      'Pending': { color: 'bg-gray-200 text-gray-800', text: 'Bekliyor' },
      'Processing': { color: 'bg-blue-200 text-blue-800', text: 'İşleniyor' },
      'Completed': { color: 'bg-green-200 text-green-800', text: 'Tamamlandı' },
      'PartialSuccess': { color: 'bg-yellow-200 text-yellow-800', text: 'Kısmen Başarılı' },
      'Failed': { color: 'bg-red-200 text-red-800', text: 'Başarısız' }
    };

    const badge = badges[status] || badges['Pending'];
    return (
      <span className={`px-2 py-1 rounded text-xs font-semibold ${badge.color}`}>
        {badge.text}
      </span>
    );
  };

  const calculateSuccessRate = (job: BulkJob) => {
    if (job.totalFarmers === 0) return 0;
    return Math.round((job.successfulDistributions / job.totalFarmers) * 100);
  };

  if (loading) return <div>Yükleniyor...</div>;
  if (!data) return <div>Veri yok</div>;

  return (
    <div className="p-4">
      <h2 className="text-2xl font-bold mb-4">
        Toplu Kod Dağıtım Geçmişi
      </h2>

      {/* Filtreleme */}
      <div className="mb-4 grid grid-cols-4 gap-4">
        <select
          value={filters.status}
          onChange={(e) => setFilters({ ...filters, status: e.target.value, page: 1 })}
          className="border rounded px-3 py-2"
        >
          <option value="">Tüm Durumlar</option>
          <option value="Pending">Bekliyor</option>
          <option value="Processing">İşleniyor</option>
          <option value="Completed">Tamamlandı</option>
          <option value="PartialSuccess">Kısmen Başarılı</option>
          <option value="Failed">Başarısız</option>
        </select>

        <input
          type="date"
          value={filters.startDate}
          onChange={(e) => setFilters({ ...filters, startDate: e.target.value, page: 1 })}
          className="border rounded px-3 py-2"
          placeholder="Başlangıç Tarihi"
        />

        <input
          type="date"
          value={filters.endDate}
          onChange={(e) => setFilters({ ...filters, endDate: e.target.value, page: 1 })}
          className="border rounded px-3 py-2"
          placeholder="Bitiş Tarihi"
        />

        <button
          onClick={() => setFilters({
            page: 1,
            pageSize: 20,
            status: '',
            sponsorId: null,
            startDate: '',
            endDate: ''
          })}
          className="bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600"
        >
          Filtreleri Temizle
        </button>
      </div>

      {/* Tablo */}
      <div className="overflow-x-auto">
        <table className="min-w-full bg-white border">
          <thead className="bg-gray-100">
            <tr>
              <th className="px-4 py-2 border">Job ID</th>
              <th className="px-4 py-2 border">Sponsor</th>
              <th className="px-4 py-2 border">Dosya</th>
              <th className="px-4 py-2 border">Çiftçi</th>
              <th className="px-4 py-2 border">Başarı</th>
              <th className="px-4 py-2 border">Hata</th>
              <th className="px-4 py-2 border">Başarı Oranı</th>
              <th className="px-4 py-2 border">Durum</th>
              <th className="px-4 py-2 border">Tarih</th>
              <th className="px-4 py-2 border">İşlemler</th>
            </tr>
          </thead>
          <tbody>
            {data.jobs.map((job) => (
              <tr key={job.jobId} className="hover:bg-gray-50">
                <td className="px-4 py-2 border text-center">{job.jobId}</td>
                <td className="px-4 py-2 border">
                  <div className="text-sm font-semibold">{job.sponsorName}</div>
                  <div className="text-xs text-gray-500">{job.sponsorEmail}</div>
                </td>
                <td className="px-4 py-2 border text-sm">{job.originalFileName}</td>
                <td className="px-4 py-2 border text-center">{job.totalFarmers}</td>
                <td className="px-4 py-2 border text-center text-green-600">
                  {job.successfulDistributions}
                </td>
                <td className="px-4 py-2 border text-center text-red-600">
                  {job.failedDistributions}
                </td>
                <td className="px-4 py-2 border text-center">
                  <div className="flex items-center justify-center">
                    <div className="w-16 bg-gray-200 rounded-full h-2 mr-2">
                      <div
                        className="bg-green-500 h-2 rounded-full"
                        style={{ width: `${calculateSuccessRate(job)}%` }}
                      ></div>
                    </div>
                    <span className="text-sm">{calculateSuccessRate(job)}%</span>
                  </div>
                </td>
                <td className="px-4 py-2 border text-center">
                  {getStatusBadge(job.status)}
                </td>
                <td className="px-4 py-2 border text-sm">
                  {format(new Date(job.createdDate), 'dd.MM.yyyy HH:mm')}
                </td>
                <td className="px-4 py-2 border text-center">
                  <button
                    onClick={() => window.location.href = `/api/v1/sponsorship/bulk-code-distribution/${job.jobId}/result`}
                    disabled={!job.completedDate}
                    className="text-blue-600 hover:underline disabled:text-gray-400 disabled:cursor-not-allowed"
                  >
                    Sonuç İndir
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      <div className="mt-4 flex items-center justify-between">
        <div className="text-sm text-gray-600">
          Toplam {data.totalCount} kayıt bulundu.
          Sayfa {data.page} / {data.totalPages}
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setFilters({ ...filters, page: filters.page - 1 })}
            disabled={filters.page === 1}
            className="px-4 py-2 bg-blue-500 text-white rounded disabled:bg-gray-300"
          >
            Önceki
          </button>
          <button
            onClick={() => setFilters({ ...filters, page: filters.page + 1 })}
            disabled={filters.page >= data.totalPages}
            className="px-4 py-2 bg-blue-500 text-white rounded disabled:bg-gray-300"
          >
            Sonraki
          </button>
        </div>
      </div>
    </div>
  );
};

export default BulkJobHistoryTable;
```

### Polling Hook Örneği

```typescript
import { useState, useEffect, useCallback, useRef } from 'react';

interface JobStatus {
  jobId: number;
  status: string;
  progressPercentage: number;
  successfulDistributions: number;
  failedDistributions: number;
  totalFarmers: number;
}

export function useJobPolling(jobId: number | null, interval = 3000) {
  const [jobStatus, setJobStatus] = useState<JobStatus | null>(null);
  const [isPolling, setIsPolling] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const fetchStatus = useCallback(async () => {
    if (!jobId) return;

    try {
      const response = await fetch(
        `/api/v1/sponsorship/bulk-code-distribution/status/${jobId}`,
        {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
          }
        }
      );

      if (!response.ok) {
        throw new Error('Job durumu alınamadı');
      }

      const result = await response.json();
      setJobStatus(result.data);

      // İşlem tamamlandıysa polling'i durdur
      if (['Completed', 'PartialSuccess', 'Failed'].includes(result.data.status)) {
        stopPolling();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Bilinmeyen hata');
      stopPolling();
    }
  }, [jobId]);

  const startPolling = useCallback(() => {
    if (intervalRef.current) return; // Zaten polling yapılıyorsa

    setIsPolling(true);
    fetchStatus(); // İlk fetch

    intervalRef.current = setInterval(() => {
      fetchStatus();
    }, interval);
  }, [fetchStatus, interval]);

  const stopPolling = useCallback(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
    setIsPolling(false);
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      stopPolling();
    };
  }, [stopPolling]);

  // Auto-start polling when jobId changes
  useEffect(() => {
    if (jobId) {
      startPolling();
    } else {
      stopPolling();
    }
  }, [jobId, startPolling, stopPolling]);

  return {
    jobStatus,
    isPolling,
    error,
    startPolling,
    stopPolling,
    refetch: fetchStatus
  };
}

// Kullanım:
function JobMonitor({ jobId }: { jobId: number }) {
  const { jobStatus, isPolling, error } = useJobPolling(jobId);

  if (error) return <div className="text-red-600">Hata: {error}</div>;
  if (!jobStatus) return <div>Yükleniyor...</div>;

  return (
    <div>
      <h3>Job #{jobStatus.jobId}</h3>
      <div>Durum: {jobStatus.status}</div>
      <div>İlerleme: %{jobStatus.progressPercentage}</div>
      <div>Başarılı: {jobStatus.successfulDistributions}</div>
      <div>Başarısız: {jobStatus.failedDistributions}</div>
      {isPolling && <div className="text-blue-600">Güncelleniyor...</div>}
    </div>
  );
}
```

---

## Hata Yönetimi

### HTTP Status Kodları

| Status | Açıklama | Kullanıcıya Gösterilecek Mesaj |
|--------|----------|-------------------------------|
| 200 | Başarılı | İşlem başarılı |
| 400 | Hatalı İstek | "Lütfen tüm alanları doğru doldurun" |
| 401 | Yetkisiz | "Oturum süreniz doldu, lütfen tekrar giriş yapın" |
| 403 | Erişim Engellendi | "Bu işlem için yetkiniz bulunmuyor" |
| 404 | Bulunamadı | "İşlem bulunamadı" |
| 500 | Sunucu Hatası | "Bir hata oluştu, lütfen daha sonra tekrar deneyin" |

### Error Response Formatı

```json
{
  "success": false,
  "message": "Hata mesajı burada"
}
```

### Hata Yakalama Örneği

```typescript
async function uploadExcel(file: File, sendSms: boolean) {
  try {
    const formData = new FormData();
    formData.append('ExcelFile', file);
    formData.append('SendSms', sendSms.toString());

    const response = await fetch('/api/v1/sponsorship/bulk-code-distribution', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`
      },
      body: formData
    });

    const result = await response.json();

    if (!response.ok) {
      throw new Error(result.message || 'Bilinmeyen hata');
    }

    if (!result.success) {
      throw new Error(result.message);
    }

    return result.data;

  } catch (error) {
    if (error instanceof Error) {
      // Kullanıcıya göster
      showErrorNotification(error.message);
    }
    throw error;
  }
}
```

### Yaygın Hatalar ve Çözümleri

#### 1. "Yetersiz kod sayısı"
```json
{
  "success": false,
  "message": "Yetersiz kod sayısı. Gerekli: 150, Mevcut: 100. Lütfen yeni paket satın alın."
}
```
**Çözüm**: Kullanıcıyı paket satın alma sayfasına yönlendir.

#### 2. "Admin users must specify sponsorId"
```json
{
  "success": false,
  "message": "Admin users must specify onBehalfOfSponsorId query parameter"
}
```
**Çözüm**: Admin kullanıcı için sponsor seçimi zorunlu yap.

#### 3. "Job bulunamadı"
```json
{
  "success": false,
  "message": "Job bulunamadı veya erişim yetkiniz yok."
}
```
**Çözüm**: JobId kontrolü yap, geçersiz ID'leri yakala.

#### 4. "Sonuç dosyası henüz hazır değil"
```json
{
  "success": false,
  "message": "Sonuç dosyası henüz hazır değil. Lütfen işlem tamamlanana kadar bekleyin."
}
```
**Çözüm**: "İndir" butonunu sadece `completedDate` doluysa aktif et.

---

## UI/UX Önerileri

### 1. Excel Yükleme Sayfası

**Tasarım Önerileri:**
- Drag & drop alanı
- Excel dosya formatı bilgilendirmesi
- Örnek Excel dosyası indirme linki
- SMS gönderimi checkbox'ı
- Mevcut kod sayısı göstergesi
- "Yükle ve Başlat" butonu

**Örnek UI:**
```
┌──────────────────────────────────────┐
│  📊 Toplu Kod Dağıtımı               │
├──────────────────────────────────────┤
│                                       │
│  ┌────────────────────────────────┐  │
│  │  📁 Excel Dosyasını Sürükleyin │  │
│  │     veya Tıklayarak Seçin      │  │
│  └────────────────────────────────┘  │
│                                       │
│  ☑️ SMS ile gönder                   │
│                                       │
│  ℹ️ Mevcut Kod: 500                  │
│  ℹ️ Maksimum: 2000 çiftçi           │
│                                       │
│  📄 Örnek Excel İndir                │
│                                       │
│  [Yükle ve Başlat]                   │
└──────────────────────────────────────┘
```

### 2. Polling (İlerleme) Sayfası

**Tasarım Önerileri:**
- Animasyonlu progress bar
- Anlık istatistikler (başarılı/hatalı)
- Tahmini tamamlanma süresi
- "İptal" veya "Arka planda çalıştır" seçeneği

**Örnek UI:**
```
┌──────────────────────────────────────┐
│  ⏳ İşlem Devam Ediyor...            │
├──────────────────────────────────────┤
│                                       │
│  ████████████░░░░░░░░ 60%           │
│                                       │
│  📊 İstatistikler:                   │
│  • İşlenen: 90 / 150                 │
│  • Başarılı: 85                      │
│  • Hatalı: 5                         │
│                                       │
│  ⏱️ Tahmini Süre: 2 dakika          │
│                                       │
│  [Arka Planda Çalıştır]              │
└──────────────────────────────────────┘
```

### 3. Sonuç Sayfası

**Tasarım Önerileri:**
- Başarı/başarısızlık oranı grafiği
- Detaylı istatistikler
- Sonuç Excel indirme butonu
- Hatalı kayıtlar için filtre

**Örnek UI:**
```
┌──────────────────────────────────────┐
│  ✅ İşlem Tamamlandı                 │
├──────────────────────────────────────┤
│                                       │
│  📊 Sonuç Özeti:                     │
│  • Toplam: 150 çiftçi                │
│  • Başarılı: 145 (%96.7)             │
│  • Başarısız: 5 (%3.3)               │
│                                       │
│  [📥 Sonuç Excel İndir]              │
│                                       │
│  ❌ Hatalı Kayıtlar:                 │
│  1. Ali Çiftçi - Geçersiz telefon    │
│  2. Veli Tarım - Kod tükendi         │
│  ...                                  │
│                                       │
│  [Yeni İşlem Başlat]                 │
└──────────────────────────────────────┘
```

### 4. Geçmiş Listesi (Dashboard)

**Tasarım Önerileri:**
- Tablo formatı
- Durum filtreleme
- Tarih aralığı seçici
- Sponsor filtreleme (admin için)
- Pagination
- Sıralama (tarih, durum, başarı oranı)

**Örnek UI (Yukarıdaki React component'i kullan)**

### 5. Responsive Tasarım

**Mobil için:**
- Kart görünümü (tablo yerine)
- Swipe to refresh
- Compact progress bar
- Bottom sheet modals

---

## Best Practices

### 1. Polling Stratejisi

```typescript
// ❌ Kötü: Her saniye poll
setInterval(pollStatus, 1000); // Çok agresif

// ✅ İyi: 3-5 saniye aralıklarla
setInterval(pollStatus, 3000); // Optimum

// ✅ Daha İyi: Exponential backoff
let delay = 2000;
const poll = async () => {
  await pollStatus();
  if (notComplete) {
    delay = Math.min(delay * 1.2, 10000); // Max 10 saniye
    setTimeout(poll, delay);
  }
};
```

### 2. Hata Gösterimi

```typescript
// ✅ Kullanıcı dostu mesajlar
const userFriendlyErrors: Record<string, string> = {
  'Insufficient codes': 'Kodlarınız tükendi. Yeni paket satın almanız gerekiyor.',
  'File too large': 'Dosya çok büyük. Maksimum 2000 çiftçi yükleyebilirsiniz.',
  'Invalid format': 'Excel dosyası geçersiz. Lütfen örnek dosyayı indirin.'
};

function showError(error: string) {
  const message = userFriendlyErrors[error] || error;
  toast.error(message);
}
```

### 3. Caching

```typescript
// Job geçmişini cache'le (5 dakika)
const CACHE_DURATION = 5 * 60 * 1000;
let cachedData: JobHistoryResponse | null = null;
let cacheTime = 0;

async function getJobHistory(forceRefresh = false) {
  const now = Date.now();

  if (!forceRefresh && cachedData && (now - cacheTime) < CACHE_DURATION) {
    return cachedData;
  }

  const data = await fetchJobHistory();
  cachedData = data;
  cacheTime = now;
  return data;
}
```

### 4. Loading States

```typescript
// ✅ Tüm durumlarda loading göster
function BulkJobPage() {
  const [loading, setLoading] = useState({
    initial: true,      // İlk yükleme
    polling: false,     // Polling
    download: false,    // Dosya indirme
    filter: false       // Filtreleme
  });

  // Her işlem için ayrı loading state
}
```

---

## Özet Checklist

### Frontend Developer İçin Checklist

- [ ] Excel yükleme formu oluşturuldu
- [ ] Polling mekanizması implement edildi
- [ ] Progress bar ve istatistikler gösteriliyor
- [ ] Sonuç dosyası indirme çalışıyor
- [ ] Job geçmişi tablosu oluşturuldu
- [ ] Filtreleme ve pagination çalışıyor
- [ ] Hata durumları handle ediliyor
- [ ] Loading states eklendi
- [ ] Responsive tasarım yapıldı
- [ ] Admin/Sponsor role bazlı UI ayrımı yapıldı
- [ ] Caching mekanizması eklendi
- [ ] Toast/notification sistemi entegre edildi

### Test Checklist

- [ ] Excel yükleme testi
- [ ] Polling cancel testi
- [ ] Pagination testi
- [ ] Filtreleme testi
- [ ] Admin "on behalf of" testi
- [ ] Hata senaryoları testi
- [ ] Responsive tasarım testi
- [ ] Performance testi (büyük veri setleri)

---

## Sorular?

Entegrasyon sırasında sorun yaşarsanız:

1. Backend developer ile iletişime geçin
2. API dokümantasyonunu kontrol edin: `BULK_CODE_DISTRIBUTION_HISTORY_ENDPOINT.md`
3. Postman collection'ı kullanarak endpoint'leri test edin
4. Tarayıcı console ve Network tab'ını kontrol edin

**Not**: `jobId` alanı **int** tipindedir, Guid değil!
