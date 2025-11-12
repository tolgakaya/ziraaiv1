# Bulk Subscription Assignment - Excel Template Guide

**Document Version:** 1.0
**Last Updated:** 2025-01-10

---

## 📋 Quick Start

### Option 1: CSV Template (Provided)
CSV dosyası zaten hazır: `BULK_SUBSCRIPTION_TEMPLATE.csv`

### Option 2: Convert to Excel (Recommended)
CSV'yi Excel formatına dönüştürmek için:

1. **Microsoft Excel ile:**
   - `BULK_SUBSCRIPTION_TEMPLATE.csv` dosyasını açın
   - **File → Save As** → Format: `Excel Workbook (*.xlsx)`
   - Save as: `BULK_SUBSCRIPTION_TEMPLATE.xlsx`

2. **Google Sheets ile:**
   - Google Drive'a `BULK_SUBSCRIPTION_TEMPLATE.csv` upload edin
   - Dosyayı açın
   - **File → Download → Microsoft Excel (.xlsx)**

3. **LibreOffice Calc ile:**
   - CSV'yi açın
   - **File → Save As** → File type: `Microsoft Excel 2007-365 (.xlsx)`

---

## 📊 Excel Şablonu Yapısı

### Gerekli Kolonlar

| Kolon | Açıklama | Zorunlu mu? | Örnek |
|-------|----------|-------------|-------|
| **Email** | Farmer email adresi | Evet (veya Phone) | `ahmet@example.com` |
| **Phone** | Farmer telefon numarası | Evet (veya Email) | `+905551234567` |
| **FirstName** | Farmer adı | Hayır (opsiyonel) | `Ahmet` |
| **LastName** | Farmer soyadı | Hayır (opsiyonel) | `Yılmaz` |
| **TierName** | Subscription tier adı | Hayır (default kullanılır) | `S`, `M`, `L`, `XL`, `Trial` |
| **DurationDays** | Subscription süresi (gün) | Hayır (default kullanılır) | `30`, `60`, `90`, `365` |
| **Notes** | Notlar (işlenmez) | Hayır | `Promo kampanyası` |

---

### Önemli Kurallar

#### 1. Email veya Phone Zorunlu
```
✅ DOĞRU: Email var, Phone boş
✅ DOĞRU: Email boş, Phone var
✅ DOĞRU: Hem Email hem Phone var
❌ YANLIŞ: Hem Email hem Phone boş
```

**Sistem Davranışı:**
- Önce Email ile kullanıcı aranır
- Email'de bulunamazsa Phone ile aranır
- Hiçbiri bulunamazsa YENİ kullanıcı oluşturulur

---

#### 2. TierName Değerleri

| TierName | Açıklama | Günlük Limit | Aylık Limit |
|----------|----------|--------------|-------------|
| **Trial** | Deneme sürümü | 5 analiz | 150 analiz |
| **S** | Small (Küçük) | 10 analiz | 300 analiz |
| **M** | Medium (Orta) | 20 analiz | 600 analiz |
| **L** | Large (Büyük) | 50 analiz | 1500 analiz |
| **XL** | Extra Large | 100 analiz | 3000 analiz |

**Büyük/küçük harf duyarsız:** `s`, `S`, `m`, `M` hepsi geçerli

---

#### 3. DurationDays Örnekleri

| Değer | Açıklama | Kullanım Senaryosu |
|-------|----------|---------------------|
| **7** | 1 hafta | Trial extension |
| **14** | 2 hafta | Kısa dönem test |
| **30** | 1 ay | Standart aylık paket |
| **60** | 2 ay | 2 aylık kampanya |
| **90** | 3 ay | Üç aylık paket |
| **180** | 6 ay | Altı aylık paket |
| **365** | 1 yıl | Yıllık paket |

---

#### 4. Phone Format

**Kabul Edilen Formatlar:**
```
✅ +905551234567
✅ 905551234567
✅ 05551234567
✅ 5551234567
```

**Sistem Otomatik Formatlar:**
- Tüm formatlar `+90` ile normalize edilir
- Örnek: `05551234567` → `+905551234567`

---

## 📝 Örnek Excel Satırları

### Senaryo 1: Email ve Telefon İkisi de Var
```
Email: ahmet.yilmaz@example.com
Phone: +905551234567
FirstName: Ahmet
LastName: Yılmaz
TierName: S
DurationDays: 30
Notes: Normal kayıt
```
**Sonuç:** Kullanıcı email ile bulunur, subscription güncellenir

---

### Senaryo 2: Sadece Email
```
Email: ayse.kaya@example.com
Phone: (boş)
FirstName: Ayşe
LastName: Kaya
TierName: M
DurationDays: 60
Notes: Email-only kayıt
```
**Sonuç:** Email ile kullanıcı bulunur veya oluşturulur

---

### Senaryo 3: Sadece Telefon
```
Email: (boş)
Phone: +905559876543
FirstName: Mehmet
LastName: Demir
TierName: S
DurationDays: 30
Notes: Phone-only kayıt
```
**Sonuç:** Telefon ile kullanıcı bulunur veya oluşturulur

---

### Senaryo 4: Default Tier ve Duration Kullanımı
```
Email: fatma@example.com
Phone: +905557654321
FirstName: Fatma
LastName: Çelik
TierName: (boş)
DurationDays: (boş)
Notes: Default değerler kullanılacak
```
**Sonuç:** API çağrısındaki `defaultTierId` ve `defaultDurationDays` kullanılır

---

## 🚀 API ile Kullanım

### Request Örneği (Form Data)

```http
POST /api/v1/admin/subscriptions/bulk-assignment
Authorization: Bearer {admin_token}
Content-Type: multipart/form-data

Form Data:
- excelFile: BULK_SUBSCRIPTION_TEMPLATE.xlsx
- defaultTierId: 2 (S tier - Excel'de boş bırakılanlar için)
- defaultDurationDays: 30 (Excel'de boş bırakılanlar için)
- sendNotification: true
- notificationMethod: "SMS"
- autoActivate: true
```

---

### Default Değerler Mantığı

**Excel'de TierName Boş:**
- API'de `defaultTierId` varsa → Kullanılır
- API'de de yoksa → **ERROR**: TierName required

**Excel'de DurationDays Boş:**
- API'de `defaultDurationDays` varsa → Kullanılır
- API'de de yoksa → **ERROR**: DurationDays required

**Öncelik:**
```
Excel'deki değer > API default değeri > ERROR
```

---

## ✅ Validation Rules

### Email Validation
```
✅ ahmet@example.com
✅ farmer123@gmail.com
❌ ahmet@
❌ @example.com
❌ ahmet.example.com (@ yok)
```

### Phone Validation
```
✅ +905551234567 (11 rakam)
✅ 905551234567 (11 rakam)
✅ 05551234567 (11 rakam)
❌ 555123 (çok kısa)
❌ abc123 (harf var)
```

### TierName Validation
```
✅ Trial, S, M, L, XL (büyük/küçük harf duyarsız)
❌ XXL (geçersiz tier)
❌ Premium (geçersiz tier)
```

### DurationDays Validation
```
✅ 1-365 arası sayılar
❌ 0 (geçersiz)
❌ -30 (negatif)
❌ 500 (çok büyük - max 365)
```

---

## 📊 Örnek Senaryolar

### Senaryo A: Tarım Bakanlığı Projesi (5000 Farmer)

**Excel Yapısı:**
- Email: Tüm farmer'ların email'i var
- Phone: Bazılarında var, bazılarında yok
- TierName: Hepsi `XL` (1 yıl proje)
- DurationDays: Hepsi `365`

**API Parametreleri:**
```javascript
{
  excelFile: bulk_tarim_bakanligi.xlsx,
  defaultTierId: null, // Excel'de hepsinde XL var
  defaultDurationDays: null, // Excel'de hepsinde 365 var
  sendNotification: false, // 5000 SMS maliyetli
  autoActivate: true
}
```

**Sonuç:**
- ✅ 5000 farmer subscription oluşturulur
- ✅ SMS gönderilmez (maliyet tasarrufu)
- ✅ Tüm subscription'lar otomatik aktif

---

### Senaryo B: Yeni Yıl Promo (50K Mevcut Kullanıcı)

**Excel Yapısı:**
- Email: Tüm mevcut kullanıcılar (export edilmiş)
- Phone: (boş bırakılabilir, email yeterli)
- TierName: (boş - API default kullanılacak)
- DurationDays: (boş - API default kullanılacak)

**API Parametreleri:**
```javascript
{
  excelFile: new_year_promo_50k.xlsx,
  defaultTierId: 1, // Trial tier (hediye)
  defaultDurationDays: 30, // 30 gün deneme
  sendNotification: true,
  notificationMethod: "Email", // SMS'den ucuz
  autoActivate: true
}
```

**Sonuç:**
- ✅ 50K kullanıcıya 30 gün Trial
- ✅ Email bilgilendirme (SMS'den ucuz)
- ✅ Excel basit (sadece email listesi)

---

### Senaryo C: Tarım Kooperatifi (200 Üye - Bazıları Yeni)

**Excel Yapısı:**
- Email: 140 üyede var, 60 üyede yok
- Phone: Hepsinde var
- FirstName/LastName: Hepsinde var (yeni üyeler için)
- TierName: Hepsi `M`
- DurationDays: Hepsi `90`

**API Parametreleri:**
```javascript
{
  excelFile: koop_200_uye.xlsx,
  defaultTierId: null,
  defaultDurationDays: null,
  sendNotification: true,
  notificationMethod: "SMS",
  autoActivate: true
}
```

**Sonuç:**
- ✅ 140 mevcut kullanıcı: Subscription güncellenir
- ✅ 60 yeni kullanıcı: Hesap + Subscription oluşturulur
- ✅ SMS bilgilendirme herkese

---

## 🔧 Troubleshooting

### Problem 1: "Email or Phone required"
**Sebep:** Excel satırında Email ve Phone ikisi de boş

**Çözüm:**
```
❌ YANLIŞ:
Email: (boş)
Phone: (boş)

✅ DOĞRU:
Email: farmer@example.com
Phone: (boş veya +905551234567)
```

---

### Problem 2: "Invalid TierName"
**Sebep:** Excel'de geçersiz tier adı

**Çözüm:**
```
❌ YANLIŞ:
TierName: Premium, XXL, Gold

✅ DOĞRU:
TierName: Trial, S, M, L, XL
```

---

### Problem 3: "DurationDays must be between 1 and 365"
**Sebep:** Geçersiz süre değeri

**Çözüm:**
```
❌ YANLIŞ:
DurationDays: 0, -30, 500

✅ DOĞRU:
DurationDays: 30, 60, 90, 365
```

---

### Problem 4: Duplicate Farmer
**Sebep:** Excel'de aynı farmer birden fazla

**Çözüm:**
```
Sistem son satırı işler (override):
- Satır 1: ahmet@example.com → 30 gün S
- Satır 50: ahmet@example.com → 60 gün M
→ Sonuç: 60 gün M (son satır geçerli)
```

---

## 📈 Performance Tips

### Tip 1: Büyük Excel Dosyaları
```
Maksimum: 2000 satır/dosya
Önerilen: 500-1000 satır/dosya

50.000 farmer için:
- 50 ayrı dosya upload edin (1000'er satır)
- VEYA API'yi 50 kez çağırın
```

---

### Tip 2: SMS Maliyeti
```
Bildirim Maliyeti:
- SMS: ~0.05 TL/farmer
- Email: ~0.001 TL/farmer (çok ucuz)

10.000 farmer:
- SMS: ~500 TL
- Email: ~10 TL

Öneri: Büyük kampanyalarda Email kullanın
```

---

### Tip 3: İşlem Süresi
```
Ortalama İşlem Hızı: 2-3 farmer/saniye

1000 farmer:
- Min: ~5 dakika
- Max: ~8 dakika

5000 farmer:
- Min: ~30 dakika
- Max: ~45 dakika
```

---

## 📚 Related Documentation

- **Integration Guide:** `ADMIN_BULK_SUBSCRIPTION_INTEGRATION_GUIDE.md`
- **System Comparison:** `SUBSCRIPTION_SYSTEMS_COMPARISON.md`
- **API Reference:** Swagger UI at `/swagger`
- **Operation Claims SQL:** `002_admin_bulk_subscription_operation_claims.sql`

---

## 🆘 Support

**Technical Issues:** dev@ziraai.com
**Integration Help:** api-support@ziraai.com
**Excel Template Questions:** Create GitHub issue

---

**Happy Bulk Assigning! 🚀**
