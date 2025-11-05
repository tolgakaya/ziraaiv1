# Bulk Farmer Code Distribution - Excel Template Guide

## Excel Dosya Formatı

### Gerekli Sütunlar (Header Row)

Excel dosyasının **1. satırı header** olmalıdır ve aşağıdaki sütun isimleri **tam olarak** bu şekilde yazılmalıdır:

| Sütun Adı | Zorunlu | Açıklama |
|-----------|---------|----------|
| Email | ✅ Evet | Farmer'ın email adresi (user kaydı için) |
| Phone | ✅ Evet | Farmer'ın telefon numarası (SMS göndermek için) |
| FarmerName | ❌ Hayır | Farmer'ın adı (SMS'te kullanılır, opsiyonel) |

### Validasyon Kuralları

#### Email Validasyonu
- ✅ Geçerli email formatı olmalı (`user@domain.com`)
- ✅ Excel içinde duplicate email olmamalı
- ⚠️ Sistemde kayıtlı olmayan email olması durumunda user bulunmaz hatası verilir

#### Phone Validasyonu
- ✅ Türk cep telefonu formatı (5XX XXX XX XX)
- ✅ Aşağıdaki formatların hepsi kabul edilir ve normalize edilir:
  - `+905321234567` → `05321234567`
  - `905321234567` → `05321234567`
  - `05321234567` → `05321234567`
  - `5321234567` → `05321234567`
  - `0532 123 45 67` → `05321234567`
  - `(0532) 123-45-67` → `05321234567`
- ✅ Excel içinde duplicate phone olmamalı

#### FarmerName Validasyonu
- ❌ Opsiyonel alan (boş bırakılabilir)
- ✅ Maksimum 200 karakter
- 💡 SMS'te kullanılır: "🎁 {SponsorCompany} size sponsorluk paketi hediye etti!"

### Dosya Kısıtlamaları

- **Maksimum Dosya Boyutu:** 5 MB
- **Maksimum Satır Sayısı:** 2,000 farmer
- **Desteklenen Formatlar:** `.xlsx`, `.xls`
- **Encoding:** UTF-8 (Türkçe karakterler için)

## Excel Template Örneği

### Minimal Örnek (Sadece Zorunlu Alanlar)

```
Email                    | Phone
-------------------------|----------------
farmer1@example.com      | 05321234567
farmer2@example.com      | +905429876543
farmer3@example.com      | 5559876543
```

### Tam Örnek (Tüm Alanlar)

```
Email                    | Phone          | FarmerName
-------------------------|----------------|------------------
ahmet.yilmaz@gmail.com   | 05321234567    | Ahmet Yılmaz
mehmet.demir@hotmail.com | +905429876543  | Mehmet Demir
ayse.kaya@outlook.com    | 5559876543     | Ayşe Kaya
fatma.celik@yahoo.com    | 0542 987 65 43 | Fatma Çelik
ali.ozturk@gmail.com     | (0532) 111-22-33| Ali Öztürk
```

### Telefon Format Örnekleri (Hepsi Geçerli)

```
Email                    | Phone              | Normalize Sonuç
-------------------------|--------------------|------------------
user1@test.com           | +905321234567      | 05321234567
user2@test.com           | 905321234567       | 05321234567
user3@test.com           | 05321234567        | 05321234567
user4@test.com           | 5321234567         | 05321234567
user5@test.com           | 0532 123 45 67     | 05321234567
user6@test.com           | (0532) 123-45-67   | 05321234567
user7@test.com           | 532.123.45.67      | 05321234567
```

## İşlem Akışı

### 1. Excel Yükleme
```
POST /api/v1/sponsorship/bulk-code-distribution
Content-Type: multipart/form-data

{
  "excelFile": [Excel dosyası],
  "purchaseId": 26,
  "sendSms": true
}
```

### 2. Validasyon Adımları

1. **Dosya Validasyonu**
   - Dosya boyutu kontrolü (max 5 MB)
   - Dosya formatı kontrolü (.xlsx, .xls)

2. **Purchase Validasyonu**
   - PurchaseId var mı?
   - Sponsor'a ait mi?
   - Ödeme tamamlanmış mı?

3. **Excel Parse**
   - Header satırı okunur
   - Gerekli sütunlar var mı kontrol edilir
   - Satırlar parse edilir

4. **Satır Validasyonu**
   - Her satır için email, phone, codeCount kontrol edilir
   - Duplicate kontrolü yapılır
   - Format validasyonu yapılır

5. **Kod Yeterlilik Kontrolü**
   - Toplam ihtiyaç hesaplanır
   - Mevcut kodlar yeterli mi kontrol edilir

### 3. Hata Mesajları

#### Dosya Hataları
```json
{
  "success": false,
  "message": "Dosya yüklenmedi."
}
```

```json
{
  "success": false,
  "message": "Dosya boyutu çok büyük. Maksimum: 5 MB"
}
```

```json
{
  "success": false,
  "message": "Geçersiz dosya formatı. Sadece .xlsx ve .xls desteklenir."
}
```

#### Excel Format Hataları
```json
{
  "success": false,
  "message": "Excel'de 'Email' sütunu zorunludur"
}
```

```json
{
  "success": false,
  "message": "Excel'de 'Phone' sütunu zorunludur"
}
```



#### Satır Validasyon Hataları
```json
{
  "success": false,
  "message": "Geçersiz satırlar:\nSatır 3: Geçersiz email - invalid-email\nSatır 5: Geçersiz telefon - 123456"
}
```

#### Kod Yeterliliği Hataları
```json
{
  "success": false,
  "message": "Yetersiz kod. Gerekli: 150, Mevcut: 100"
}
```

### 4. Başarılı Response

```json
{
  "data": {
    "jobId": 42,
    "totalFarmers": 50,
    "totalCodesRequired": 75,
    "availableCodes": 200,
    "status": "Processing",
    "createdDate": "2025-01-15T10:30:00",
    "estimatedCompletionTime": "2025-01-15T10:55:00",
    "statusCheckUrl": "/api/v1/sponsorship/bulk-code-distribution/status/42"
  },
  "success": true,
  "message": "Toplu kod dağıtım işlemi başlatıldı. 50 farmer kuyruğa eklendi."
}
```

## Excel Dosyası Hazırlama İpuçları

### ✅ Yapılması Gerekenler

1. **Header Satırını Doğru Yazın**
   - Büyük/küçük harf fark etmez (`Email` = `email` = `EMAIL`)
   - Boşluk bırakmayın
   - Türkçe karakter kullanmayın header'da

2. **Telefon Numaralarını Temizleyin**
   - Parantez, tire, nokta kullanabilirsiniz (otomatik temizlenir)
   - Ülke kodu (+90) opsiyoneldir
   - Başında 0 olabilir veya olmayabilir

3. **Email Formatını Kontrol Edin**
   - `@` karakteri mutlaka olmalı
   - Domain uzantısı olmalı (`.com`, `.tr`, vs.)

### ❌ Yapılmaması Gerekenler

1. **Boş Satır Bırakmayın**
   - Satır atlamayın
   - Boş satırlar otomatik atlanır ama karışıklığa yol açabilir

2. **Duplicate Veri Girmeyin**
   - Aynı email 2 kez olamaz
   - Aynı telefon 2 kez olamaz

3. **Geçersiz Veri Girmeyin**
   - Email formatına uymayan değerler
   - Türkiye dışı telefon numaraları

## Örnek Kullanım Senaryoları

### Senaryo 1: Küçük Grup (10 Farmer)
```
Email                    | Phone          | FarmerName
-------------------------|----------------|------------------
farmer1@test.com         | 05321111111    | Farmer 1
farmer2@test.com         | 05321111112    | Farmer 2
...
farmer10@test.com        | 05321111120    | Farmer 10

Toplam Kod İhtiyacı: 10 (Her farmer'a 1 kod)
```

### Senaryo 2: Orta Grup (100 Farmer)
```
Email                    | Phone          | FarmerName
-------------------------|----------------|------------------
farmer1@test.com         | 05321111111    | Farmer 1
farmer2@test.com         | 05321111112    | Farmer 2
farmer3@test.com         | 05321111113    | Farmer 3
farmer4@test.com         | 05321111114    | Farmer 4
...

Toplam Kod İhtiyacı: 100 (Her farmer'a 1 kod)
```

### Senaryo 3: Büyük Grup (2000 Farmer - Maksimum)
```
Email                    | Phone          | FarmerName
-------------------------|----------------|------------------
bulk1@test.com           | 05321111111    | Bulk 1
bulk2@test.com           | 05321111112    | Bulk 2
...
bulk2000@test.com        | 05329999999    | Bulk 2000

Toplam Kod İhtiyacı: 2000 (Her farmer'a 1 kod)
```

## SMS Gönderimi

Her farmer'a şu formatta SMS gönderilir:

```
🎁 {Sponsor Firma Adı} size sponsorluk paketi hediye etti!

Sponsorluk Kodunuz: AGRI-2025-XXXXXXXX

Hemen kullanmak için tıklayın:
https://ziraai.com/redeem/AGRI-2025-XXXXXXXX

Veya uygulamayı indirin:
https://play.google.com/store/apps/details?id=com.ziraai.app
```

**Not:**
- `FarmerName` boşsa, user'ın `FullName`'i kullanılır
- `FarmerName` ve `FullName` de boşsa, "Değerli Üyemiz" kullanılır
- Sponsor firma adı `SponsorProfile.CompanyName`'den gelir
- Boşsa "ZiraAI Sponsor" kullanılır

## Frontend Entegrasyon

Excel yükleme için örnek form:

```typescript
const uploadExcel = async (file: File, purchaseId: number, sendSms: boolean) => {
  const formData = new FormData();
  formData.append('excelFile', file);
  formData.append('purchaseId', purchaseId.toString());
  formData.append('sendSms', sendSms.toString());

  const response = await fetch('/api/v1/sponsorship/bulk-code-distribution', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'x-dev-arch-version': '1.0'
    },
    body: formData
  });

  return await response.json();
};
```

## Download Links

### Excel Template Dosyaları
- **Minimal Template:** `bulk-farmer-code-distribution-template-minimal.xlsx`
- **Full Template:** `bulk-farmer-code-distribution-template-full.xlsx`
- **Sample Data:** `bulk-farmer-code-distribution-sample-data.xlsx`

Bu dosyaları frontend'de statik olarak sunabilir veya API endpoint'i oluşturabilirsiniz.
