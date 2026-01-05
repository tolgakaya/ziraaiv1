# Farmer Invitations Bulk Upload Guide

**Last Updated**: 2026-01-05
**Document Version**: 1.0

---

## 📋 Overview

Bu doküman, sponsorların çiftçilere toplu davetiye göndermek için Excel dosyası yükleme işlemini açıklar.

**Kullanım Senaryosu:**
- Sponsor tek seferde birden fazla çiftçiye davetiye göndermek istiyor
- Excel ile çiftçi listesi hazırlanıyor
- Sistem her çiftçi için otomatik olarak:
  - Invitation oluşturuyor
  - Kod rezerve ediyor
  - SMS/WhatsApp gönderiyor
  - SignalR notification gönderiyor (eğer çiftçi uygulamada çevrimiçiyse)

---

## 📊 Excel Şablonu

### Dosya İsmi
`Farmer_Invitations_Bulk_Upload_Template.xlsx`

### Kolon Yapısı

| Kolon | Zorunlu | Tip | Açıklama | Örnek |
|-------|---------|-----|----------|-------|
| `Phone` | **Evet** | string | Çiftçinin telefon numarası | `05551234567` |
| `FarmerName` | Hayır | string | Çiftçinin adı soyadı | `Ahmet Yılmaz` |
| `Email` | Hayır | string | Çiftçinin email adresi | `ahmet@example.com` |
| `PackageTier` | Hayır | string | Paket seviyesi: S, M, L, XL | `M` |
| `Notes` | Hayır | string | Davetiye notu (dahili kullanım) | `İlk davet` |

**NOT:** Her davet için **otomatik olarak 1 kod** gönderilir. `CodeCount` kolonu kaldırılmıştır.

### Örnek Excel İçeriği

```csv
Phone,FarmerName,Email,PackageTier,Notes
05551234567,Ahmet Yılmaz,ahmet@example.com,M,İlk davet
05559876543,Mehmet Demir,,S,Küçük üretici
+905556668899,Ayşe Kaya,ayse@email.com,L,Premium müşteri
905553334455,Fatma Şahin,fatma.sahin@mail.com,,Tier belirtilmedi
0 555 111 2233,Ali Yıldız,,XL,VIP çiftçi
```

---

## 📱 Telefon Formatları

Sistem aşağıdaki tüm telefon formatlarını otomatik olarak normalize eder:

| Girilen Format | Normalize Edilmiş | Açıklama |
|----------------|-------------------|----------|
| `05551234567` | `05551234567` | ✅ Standart format (önerilen) |
| `+90 555 123 4567` | `05551234567` | ✅ Uluslararası format boşluklu |
| `+905551234567` | `05551234567` | ✅ Uluslararası format |
| `905551234567` | `05551234567` | ✅ 90 ile başlayan |
| `0 555 123 45 67` | `05551234567` | ✅ Boşluklu format |
| `(0555) 123-4567` | `05551234567` | ✅ Parantez ve tire içeren |

**Normalization Kuralı:**
- Tüm boşluk, tire, parantez kaldırılır
- `+90` veya `90` → `0` ile değiştirilir
- Sonuç: `0XXXXXXXXXX` formatı (11 haneli)

---

## 🎯 PackageTier Kullanımı

**NOT:** Her davetiye için otomatik olarak **1 kod** gönderilir.

### Tier Belirtildiğinde
```csv
Phone,FarmerName,Email,PackageTier,Notes
05551234567,Ahmet Yılmaz,,M,
```
- Sistem sadece **M tier** kodundan **1 adet** rezerve eder
- Eğer M tier kodu yoksa → **Hata döner, davet oluşturulmaz**

### Tier Belirtilmediğinde
```csv
Phone,FarmerName,Email,PackageTier,Notes
05551234567,Ahmet Yılmaz,,,
```
- Sistem **herhangi bir tier**'dan **1 adet** kod rezerve eder
- Öncelik sırası: Expiry date (en yakın süresi dolanlar önce)
- Daha esnek ama tier kontrolü yok

### Geçerli Tier Değerleri
- `S` - Small (Küçük paket)
- `M` - Medium (Orta paket)
- `L` - Large (Büyük paket)
- `XL` - Extra Large (En büyük paket)
- Boş - Herhangi bir tier

**NOT:** Tier değerleri case-insensitive (`m`, `M`, `MEDIUM` hepsi geçerli)

---

## 🔄 API Endpoint

### Endpoint
```
POST /api/v1/sponsorship/farmer/invitations/bulk
```

### Request Headers
```http
Authorization: Bearer {sponsor_jwt_token}
x-dev-arch-version: 1.0
Content-Type: application/json
```

### Request Body
```json
{
  "recipients": [
    {
      "phone": "05551234567",
      "farmerName": "Ahmet Yılmaz",
      "email": "ahmet@example.com",
      "packageTier": "M",
      "notes": "İlk davet"
    },
    {
      "phone": "05559876543",
      "farmerName": "Mehmet Demir",
      "email": null,
      "packageTier": "S",
      "notes": "Küçük üretici"
    }
  ],
  "channel": "SMS",
  "customMessage": null
}
```

**NOT:** `codeCount` alanı kaldırılmıştır. Her davetiye için otomatik olarak 1 kod gönderilir.

### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `recipients` | array | **Yes** | Çiftçi listesi |
| `recipients[].phone` | string | **Yes** | Telefon numarası |
| `recipients[].farmerName` | string | No | Çiftçi adı |
| `recipients[].email` | string | No | Email adresi |
| `recipients[].packageTier` | string | No | S, M, L, XL (her davetiye 1 kod) |
| `recipients[].notes` | string | No | Dahili not (max 500 karakter) |
| `channel` | string | No | `SMS` veya `WhatsApp` (default: SMS) |
| `customMessage` | string | No | Özel mesaj (yoksa default template kullanılır) |

### Success Response (200 OK)

```json
{
  "data": {
    "totalRequested": 2,
    "successCount": 2,
    "failedCount": 0,
    "results": [
      {
        "phone": "05551234567",
        "farmerName": "Ahmet Yılmaz",
        "codeCount": 1,
        "packageTier": "M",
        "success": true,
        "invitationId": 15,
        "invitationToken": "a3f5e9c1b2d4f6a8e0c2b4d6f8a0c2e4",
        "invitationLink": "https://ziraai.com/farmer-invite/a3f5e9c1b2d4f6a8e0c2b4d6f8a0c2e4",
        "errorMessage": null,
        "deliveryStatus": "Sent"
      },
      {
        "phone": "05559876543",
        "farmerName": "Mehmet Demir",
        "codeCount": 1,
        "packageTier": "S",
        "success": true,
        "invitationId": 16,
        "invitationToken": "b4c6f0d2e3a5c7b9d1e3f5a7c9b1d3e5",
        "invitationLink": "https://ziraai.com/farmer-invite/b4c6f0d2e3a5c7b9d1e3f5a7c9b1d3e5",
        "errorMessage": null,
        "deliveryStatus": "Sent"
      }
    ]
  },
  "success": true,
  "message": "📱 2 davet başarıyla gönderildi via SMS"
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `totalRequested` | integer | Excel'deki toplam çiftçi sayısı |
| `successCount` | integer | Başarıyla gönderilen davetiye sayısı |
| `failedCount` | integer | Başarısız olan davetiye sayısı |
| `results` | array | Her bir çiftçi için detaylı sonuç |
| `results[].success` | boolean | Bu davetiye başarılı mı? |
| `results[].invitationId` | integer | Oluşturulan davetiye ID |
| `results[].invitationToken` | string | Davetiye token |
| `results[].invitationLink` | string | Deep link URL |
| `results[].errorMessage` | string | Hata mesajı (varsa) |
| `results[].deliveryStatus` | string | `Sent`, `Failed - SMS Error`, `Failed - Insufficient Codes` |

---

## ⚠️ Hata Senaryoları

### 1. Yetersiz Kod (Insufficient Codes)

**Excel:**
```csv
Phone,FarmerName,Email,PackageTier,Notes
05551234567,Ahmet Yılmaz,,M,
```

**Sponsor'un Durumu:**
- M tier kodu: 0 adet (tükenmiş)

**Sonuç:**
```json
{
  "phone": "05551234567",
  "farmerName": "Ahmet Yılmaz",
  "codeCount": 1,
  "packageTier": "M",
  "success": false,
  "invitationId": null,
  "errorMessage": "Yetersiz kod (M tier). Mevcut: 0, İstenen: 1",
  "deliveryStatus": "Failed - Insufficient Codes"
}
```

### 2. Geçersiz Tier

**Excel:**
```csv
Phone,FarmerName,Email,PackageTier,Notes
05551234567,Ahmet Yılmaz,,XXL,
```

**Sonuç:**
```json
{
  "phone": "05551234567",
  "success": false,
  "errorMessage": "Geçersiz tier: XXL",
  "deliveryStatus": "Failed - Invalid Tier"
}
```

### 3. SMS Gönderim Hatası

**Senaryo:** Telefon numarası geçersiz veya SMS servisi çalışmıyor

**Sonuç:**
```json
{
  "phone": "05551234567",
  "success": false,
  "invitationId": 15,
  "invitationToken": "a3f5e9c1b2d4f6a8e0c2b4d6f8a0c2e4",
  "errorMessage": "SMS delivery failed: Invalid phone number",
  "deliveryStatus": "Failed - SMS Error"
}
```

**NOT:** Bu durumda davetiye oluşturulur ve kodlar rezerve edilir, ama SMS gönderilemez. Sponsor daha sonra manuel olarak linki gönderebilir.

---

## 🔒 İş Kuralları

### 1. Kod Rezervasyonu
```csharp
// Her davetiye için kodlar rezerve edilir
SponsorshipCode.ReservedForFarmerInvitationId = invitation.Id
SponsorshipCode.ReservedForFarmerAt = DateTime.Now
```

**Özellikler:**
- Kodlar sadece bu davetiye için ayrılır
- Başka çiftçiye gönderilemez
- Davetiye kabul edilene kadar "Reserved" durumunda
- Davetiye iptal edilirse kodlar serbest kalır

### 2. Kod Seçim Stratejisi
```csharp
var selectedCodes = availableCodes
    .OrderBy(c => c.ExpiryDate)      // Önce süresi yaklaşanlar
    .ThenBy(c => c.CreatedDate)      // Sonra en eskiler (FIFO)
    .Take(codeCount)
    .ToList();
```

**Mantık:**
- Expiry date'e yakın kodlar önce kullanılır → Kod kaybı önlenir
- Aynı expiry date'de en eski kodlar önce → FIFO prensibi

### 3. SMS/WhatsApp Template

**Default Template:**
```
{sponsorName} sizi ZiraAI'ya davet etti!
{codeCount} adet sponsorluk kodu sizi bekliyor.

Daveti kabul etmek için tıklayın:
{deepLink}

Uygulamamız:
{playStoreLink}
```

**Değişkenler:**
- `{sponsorName}` - Sponsor şirket adı
- `{farmerName}` - Çiftçi adı (varsa)
- `{codeCount}` - Kod sayısı
- `{deepLink}` - `https://ziraai.com/farmer-invite/{token}`
- `{playStoreLink}` - Google Play Store linki

### 4. Rate Limiting
```csharp
await Task.Delay(50, cancellationToken); // 50ms per SMS
```
- Her SMS arasında 50ms bekleme
- SMS provider'ın rate limit'ini koruma
- 1000 davet = yaklaşık 50 saniye

---

## 🧪 Test Senaryoları

### Test 1: Başarılı Toplu Gönderim

**Excel:**
```csv
Phone,FarmerName,Email,PackageTier,Notes
05551234567,Ahmet Yılmaz,,M,
05559876543,Mehmet Demir,,S,
```

**Ön Koşullar:**
- Sponsor'da 1+ adet M tier kodu var
- Sponsor'da 1+ adet S tier kodu var

**Beklenen Sonuç:**
```json
{
  "totalRequested": 2,
  "successCount": 2,
  "failedCount": 0
}
```

### Test 2: Kısmi Başarı (Partial Success)

**Excel:**
```csv
Phone,FarmerName,Email,PackageTier,Notes
05551234567,Ahmet Yılmaz,,M,
05559876543,Mehmet Demir,,S,Yetersiz kod
05556668899,Ayşe Kaya,,L,
```

**Ön Koşullar:**
- M tier: 5 kod var
- S tier: 0 kod var (tükenmiş)
- L tier: 3 kod var

**Beklenen Sonuç:**
```json
{
  "totalRequested": 3,
  "successCount": 2,
  "failedCount": 1,
  "results": [
    { "phone": "05551234567", "success": true },
    { "phone": "05559876543", "success": false, "errorMessage": "Yetersiz kod (S tier). Mevcut: 0, İstenen: 1" },
    { "phone": "05556668899", "success": true }
  ]
}
```

### Test 3: Telefon Format Kontrolü

**Excel:**
```csv
Phone,FarmerName,Email,PackageTier,Notes
05551234567,Test 1,,,
+90 555 123 4567,Test 2,,,
905551234567,Test 3,,,
0 555 123 45 67,Test 4,,,
```

**Beklenen:** Tüm formatlar `05551234567` olarak normalize edilir ✅

---

## 📊 Frontend Implementation (Web Admin Panel)

### Excel Upload Bileşeni

```typescript
import { useState } from 'react';
import * as XLSX from 'xlsx';
import axios from 'axios';

interface FarmerInvitationRow {
  Phone: string;
  FarmerName?: string;
  Email?: string;
  PackageTier?: string;
  Notes?: string;
  // CodeCount removed - always 1 per invitation
}

function BulkFarmerInvitationUpload() {
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<any>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0]);
    }
  };

  const parseExcel = async (file: File): Promise<FarmerInvitationRow[]> => {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();

      reader.onload = (e) => {
        try {
          const data = new Uint8Array(e.target?.result as ArrayBuffer);
          const workbook = XLSX.read(data, { type: 'array' });
          const sheetName = workbook.SheetNames[0];
          const worksheet = workbook.Sheets[sheetName];
          const jsonData = XLSX.utils.sheet_to_json<FarmerInvitationRow>(worksheet);

          resolve(jsonData);
        } catch (error) {
          reject(error);
        }
      };

      reader.onerror = () => reject(reader.error);
      reader.readAsArrayBuffer(file);
    });
  };

  const handleUpload = async () => {
    if (!file) {
      alert('Lütfen bir Excel dosyası seçin');
      return;
    }

    setLoading(true);
    try {
      // 1. Parse Excel
      const rows = await parseExcel(file);

      // 2. Transform to API format
      const recipients = rows.map(row => ({
        phone: row.Phone,
        farmerName: row.FarmerName || null,
        email: row.Email || null,
        packageTier: row.PackageTier || null,
        notes: row.Notes || null,
        // CodeCount removed - backend automatically uses 1 per invitation
      }));

      // 3. Send to API
      const response = await axios.post(
        '/api/v1/sponsorship/farmer/invitations/bulk',
        {
          recipients,
          channel: 'SMS', // or 'WhatsApp'
          customMessage: null,
        },
        {
          headers: {
            'Authorization': `Bearer ${jwtToken}`,
            'x-dev-arch-version': '1.0',
          },
        }
      );

      setResult(response.data.data);
      alert(`✅ Başarılı: ${response.data.data.successCount}, ❌ Başarısız: ${response.data.data.failedCount}`);
    } catch (error: any) {
      console.error('Upload error:', error);
      alert('Hata: ' + (error.response?.data?.message || error.message));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="bulk-upload-container">
      <h2>Toplu Çiftçi Davetiyesi Gönder</h2>

      <div className="upload-section">
        <input
          type="file"
          accept=".xlsx, .xls"
          onChange={handleFileChange}
          disabled={loading}
        />
        <button onClick={handleUpload} disabled={!file || loading}>
          {loading ? 'Gönderiliyor...' : 'Excel Yükle ve Gönder'}
        </button>
      </div>

      {result && (
        <div className="result-section">
          <h3>Sonuçlar</h3>
          <p>Toplam: {result.totalRequested}</p>
          <p>Başarılı: {result.successCount} ✅</p>
          <p>Başarısız: {result.failedCount} ❌</p>

          <table>
            <thead>
              <tr>
                <th>Telefon</th>
                <th>Çiftçi Adı</th>
                <th>Tier</th>
                <th>Durum</th>
                <th>Hata</th>
              </tr>
            </thead>
            <tbody>
              {result.results.map((r: any, idx: number) => (
                <tr key={idx} className={r.success ? 'success' : 'error'}>
                  <td>{r.phone}</td>
                  <td>{r.farmerName || '-'}</td>
                  <td>{r.packageTier || 'Any'}</td>
                  <td>{r.deliveryStatus}</td>
                  <td>{r.errorMessage || '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
```

---

## 🎯 Best Practices

### 1. Excel Hazırlama
- ✅ Telefon numaralarını `05551234567` formatında girin (önerilen)
- ✅ Tier belirtmeyin eğer sponsor'un çeşitli tier'ları varsa
- ✅ Küçük gruplar halinde test edin (önce 5-10 davet)
- ❌ Aynı telefon numarasını birden fazla satıra yazmayın

### 2. Kod Yönetimi
- ✅ Toplu gönderim öncesi sponsor'un yeterli kodu olduğundan emin olun
- ✅ Tier belirtirseniz o tier için yeterli kod olmalı
- ✅ Her davet için 1 kod rezerve edilir - Excel'deki toplam satır sayısı ≤ Mevcut kodlar

### 3. Test ve Prodüksiyon
- ✅ Test ortamında önce 2-3 davet gönderin
- ✅ Sonuçları kontrol edin (başarılı/başarısız)
- ✅ SMS'lerin geldiğini doğrulayın
- ✅ Çiftçinin daveti kabul edebildiğini test edin

### 4. Hata Yönetimi
- ✅ `results[]` array'ini kontrol edin - her satır için sonuç var
- ✅ Başarısız olanları tekrar göndermek için yeni Excel oluşturun
- ✅ `errorMessage` alanını kullanıcıya gösterin

---

## 🔗 İlgili Dokümanlar

- [Farmer Invitations API Complete Reference](./FARMER_INVITATIONS_API_COMPLETE_REFERENCE.md)
- [Farmer Invitation System Backend Implementation](./FARMER_INVITATION_SYSTEM_BACKEND_IMPLEMENTATION.md)
- [SignalR Mobile Integration Complete Guide](../SIGNALR_MOBILE_INTEGRATION_COMPLETE.md)

---

## 📝 Versiyon Geçmişi

**v1.1 (2026-01-05)**
- CodeCount kolonu kaldırıldı - her davet için otomatik olarak 1 kod gönderilir
- Backend'de `const int codeCount = 1;` ile hardcode edildi
- Frontend mapping ve tablo görünümünden CodeCount referansları kaldırıldı

**v1.0 (2026-01-05)**
- İlk versiyon
- Excel şablonu yapısı
- API endpoint detayları
- Frontend implementation örnekleri
- Test senaryoları
