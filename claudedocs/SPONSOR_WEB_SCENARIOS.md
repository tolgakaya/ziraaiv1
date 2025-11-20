# Sponsor Web Senaryoları - Endpoint Listesi

**Doküman Versiyonu:** 1.0
**Tarih:** 2025-11-02
**Branch:** `feature/sponsor-web-scenarios`
**Amaç:** Sponsor web arayüzü için backend endpoint referansı

---

## 📋 İçindekiler

1. [Kimlik Doğrulama & Hesap](#1-kimlik-doğrulama--hesap)
2. [Sponsor Profil Yönetimi](#2-sponsor-profil-yönetimi)
3. [Paket Satın Alma](#3-paket-satın-alma)
4. [Kod Yönetimi](#4-kod-yönetimi)
5. [Dealer Yönetimi](#5-dealer-yönetimi)
6. [Farmer İlişkileri](#6-farmer-i̇lişkileri)
7. [Messaging (İletişim)](#7-messaging-i̇letişim)
8. [Analytics & Raporlama](#8-analytics--raporlama)
9. [Smart Links (XL Tier)](#9-smart-links-xl-tier)
10. [Logo & Görünürlük](#10-logo--görünürlük)

---

## 1. Kimlik Doğrulama & Hesap

### 1.1 Login (Giriş Yapma)
**Endpoint:** `POST {{base_url}}/api/v{{version}}/Auth/login`

**Body:**
```json
{
  "email": "sponsor@company.com",
  "password": "SecurePass123"
}
```

**Kullanım:** Email + password ile giriş (sponsor profile oluştururken şifre belirtilmişse)

---

### 1.2 Phone Login (Telefon ile Giriş)
**Endpoint:** `POST {{base_url}}/api/v{{version}}/Auth/login-phone`

**Body:**
```json
{
  "phoneNumber": "+905321234567"
}
```

**Kullanım:** OTP ile giriş başlatma (mobile users)

---

### 1.3 OTP Doğrulama
**Endpoint:** `POST {{base_url}}/api/v{{version}}/Auth/verify-phone-otp`

**Body:**
```json
{
  "phoneNumber": "+905321234567",
  "otpCode": "123456"
}
```

**Kullanım:** Phone login OTP doğrulama

---

### 1.4 Token Yenileme
**Endpoint:** `POST {{base_url}}/api/v{{version}}/Auth/refresh-token`

**Body:**
```json
{
  "refreshToken": "eyJhbGc..."
}
```

**Kullanım:** JWT token yenileme (token expiry: 60 min)

---

### 1.5 Şifre Değiştirme
**Endpoint:** `PUT {{base_url}}/api/v{{version}}/Auth/user-password`

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "currentPassword": "OldPass123",
  "newPassword": "NewPass456"
}
```

**Kullanım:** Oturum açmış sponsor şifresini değiştirir

---

### 1.6 Şifremi Unuttum
**Endpoint:** `PUT {{base_url}}/api/v{{version}}/Auth/forgot-password`

**Body:**
```json
{
  "email": "sponsor@company.com"
}
```

**Kullanım:** Şifre sıfırlama linki gönderme

---

### 1.7 Kullanıcı Bilgileri
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/debug/user-info`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Oturum açmış kullanıcı bilgilerini görüntüleme (debug)

---

## 2. Sponsor Profil Yönetimi

### 2.1 Profil Oluşturma
**Endpoint:** `POST {{base_url}}/api/v{{version}}/sponsorship/create-profile`

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "companyName": "AgriTech Solutions A.Ş.",
  "contactEmail": "support@agritech.com",
  "password": "SecurePass123",
  "companyDescription": "Lider tarım girdileri sağlayıcısı",
  "sponsorLogoUrl": "https://cdn.ziraai.com/logos/agritech.png",
  "websiteUrl": "https://agritech.com.tr",
  "contactPhone": "+905321234567",
  "contactPerson": "Mehmet Yılmaz",
  "companyType": "Manufacturer",
  "businessModel": "B2B2C",
  "taxNumber": "1234567890",
  "tradeRegistryNumber": "TR123456",
  "address": "Atatürk Cad. No:123",
  "city": "Ankara",
  "country": "Türkiye",
  "postalCode": "06100",
  "linkedInUrl": "https://linkedin.com/company/agritech",
  "twitterUrl": "https://twitter.com/agritech",
  "facebookUrl": "https://facebook.com/agritech",
  "instagramUrl": "https://instagram.com/agritech"
}
```

**Kullanım:** İlk kez sponsor profili oluşturma (one-time setup)

**Notlar:**
- `password` ZORUNLU (phone-registered users için)
- Email update yapar (phone users get real email)
- User'a `Sponsor` rolü eklenir
- Duplicate profile kontrolü var

---

### 2.2 Profil Güncelleme
**Endpoint:** `PUT {{base_url}}/api/v{{version}}/sponsorship/update-profile`

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "companyName": "AgriTech Solutions A.Ş.",
  "contactEmail": "support@agritech.com",
  "password": "NewPassword123",
  "companyDescription": "Güncel açıklama",
  "sponsorLogoUrl": "https://new-cdn.com/logo.png",
  "websiteUrl": "https://agritech.com.tr",
  "linkedInUrl": "https://linkedin.com/company/agritech",
  "twitterUrl": "https://twitter.com/agritech"
}
```

**Kullanım:** Sponsor profili güncelleme (partial update supported)

**Notlar:**
- Sadece gönderilen alanlar güncellenir
- Email duplicate check yapılır
- Password update secure hashing ile
- Audit trail (UpdatedDate, UpdatedByUserId)

---

### 2.3 Profil Görüntüleme
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/profile`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Mevcut sponsor profilini görüntüleme

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 501,
    "sponsorId": 1001,
    "companyName": "AgriTech Solutions A.Ş.",
    "contactEmail": "support@agritech.com",
    "companyDescription": "...",
    "sponsorLogoUrl": "...",
    "websiteUrl": "...",
    "contactPhone": "...",
    "linkedInUrl": "...",
    "twitterUrl": "...",
    "facebookUrl": "...",
    "instagramUrl": "...",
    "taxNumber": "...",
    "address": "...",
    "city": "...",
    "country": "...",
    "isActive": true,
    "isVerified": false,
    "createdDate": "2025-10-10T10:00:00Z"
  }
}
```

---

## 3. Paket Satın Alma

### 3.1 Tier Listesi (Paket Seçenekleri)
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/tiers-for-purchase`

**Kullanım:** Satın alınabilir tier'ları listeleme (Trial hariç)

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "tierName": "S",
      "displayName": "Small - Temel Görünürlük",
      "description": "Başlangıç paketi",
      "monthlyPrice": 50.00,
      "yearlyPrice": 500.00,
      "currency": "TRY",
      "features": [
        "Logo: Sadece başlangıç ekranı",
        "Farmer verisi: %30 (anonim)",
        "Mesajlaşma: Yok",
        "Smart Links: Yok"
      ]
    },
    {
      "id": 2,
      "tierName": "M",
      "displayName": "Medium - Gelişmiş Görünürlük",
      "monthlyPrice": 100.00,
      "yearlyPrice": 1000.00,
      "features": [
        "Logo: Başlangıç + Sonuç ekranları",
        "Farmer verisi: %60 (anonim)",
        "Mesajlaşma: Yok",
        "Smart Links: Yok"
      ]
    },
    {
      "id": 3,
      "tierName": "L",
      "displayName": "Large - Tam Veri Erişimi",
      "monthlyPrice": 200.00,
      "yearlyPrice": 2000.00,
      "features": [
        "Logo: Tüm ekranlar",
        "Farmer verisi: %100 (tam detay)",
        "Mesajlaşma: Aktif",
        "Smart Links: Yok"
      ]
    },
    {
      "id": 4,
      "tierName": "XL",
      "displayName": "Extra Large - Premium",
      "monthlyPrice": 500.00,
      "yearlyPrice": 5000.00,
      "features": [
        "Logo: Tüm ekranlar",
        "Farmer verisi: %100",
        "Mesajlaşma: Aktif",
        "Smart Links: 50 adet (AI-powered)"
      ]
    }
  ]
}
```

---

### 3.2 Paket Satın Alma
**Endpoint:** `POST {{base_url}}/api/v{{version}}/sponsorship/purchase-package`

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "subscriptionTierId": 3,
  "quantity": 100,
  "totalAmount": 20000.00,
  "paymentMethod": "CreditCard",
  "paymentReference": "IYZICO-TXN-789456123",
  "companyName": "AgriTech Solutions A.Ş.",
  "invoiceAddress": "Atatürk Cad. No:123 Ankara",
  "taxNumber": "1234567890",
  "codePrefix": "AGRI",
  "validityDays": 365,
  "notes": "Q4 2025 farmer kampanyası"
}
```

**Kullanım:** Bulk subscription package satın alma ve kod oluşturma

**Notlar:**
- `companyName`, `invoiceAddress`, `taxNumber` opsiyonel (profile'dan fallback)
- `codePrefix` opsiyonel (default: "ZIRA")
- `validityDays` opsiyonel (default: 365)
- Codes otomatik oluşturulur (format: `{PREFIX}-{YEAR}-{RANDOM}`)

**Response:**
```json
{
  "success": true,
  "message": "100 sponsorship kodu başarıyla oluşturuldu",
  "data": {
    "id": 2001,
    "sponsorId": 1001,
    "tierName": "L",
    "quantity": 100,
    "totalAmount": 20000.00,
    "purchaseDate": "2025-10-10T10:30:00Z",
    "paymentStatus": "Completed",
    "generatedCodes": [
      {
        "id": 10001,
        "code": "AGRI-2025-X3K9",
        "tierName": "L",
        "isUsed": false,
        "expiryDate": "2026-10-10T10:30:00Z"
      }
      // ... 99 more codes
    ]
  }
}
```

---

### 3.3 Satın Alma Geçmişi
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/purchases`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Tüm paket satın alma geçmişini görüntüleme

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 2001,
      "tierName": "L",
      "quantity": 100,
      "totalAmount": 20000.00,
      "purchaseDate": "2025-10-10T10:30:00Z",
      "paymentStatus": "Completed",
      "codesGenerated": 100,
      "codesUsed": 50,
      "codesActive": 35,
      "codesExpired": 15
    }
  ]
}
```

---

## 4. Kod Yönetimi

### 4.1 Kod Listesi (Filtrelenebilir)
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/codes`

**Headers:** `Authorization: Bearer {token}`

**Query Parameters:**
- `onlyUnused` (bool): Sadece kullanılmamış kodlar
- `onlyUnsent` (bool): Hiç gönderilmemiş kodlar (dealer'a transfer edilmemiş)
- `sentDaysAgo` (int): X gün önce gönderilmiş ama kullanılmamış kodlar
- `onlySentExpired` (bool): Gönderilmiş ancak süresi dolmuş kodlar
- `excludeDealerTransferred` (bool): Dealer'a transfer edilen kodları hariç tut
- `page` (int): Sayfa numarası (1-∞)
- `pageSize` (int): Sayfa başına kayıt (1-200)

**Kullanım Örnekleri:**

```
# Tüm kodlar (sayfalı)
GET {{base_url}}/api/v{{version}}/sponsorship/codes?page=1&pageSize=50

# Sadece kullanılmamış kodlar
GET {{base_url}}/api/v{{version}}/sponsorship/codes?onlyUnused=true

# Hiç dağıtılmamış kodlar (yeni kampanya için)
GET {{base_url}}/api/v{{version}}/sponsorship/codes?onlyUnsent=true&excludeDealerTransferred=true

# 7 gün önce gönderilmiş ama kullanılmamış (reminder için)
GET {{base_url}}/api/v{{version}}/sponsorship/codes?sentDaysAgo=7

# Gönderilmiş ama süresi dolmuş (analiz için)
GET {{base_url}}/api/v{{version}}/sponsorship/codes?onlySentExpired=true
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 10001,
      "code": "AGRI-2025-X3K9",
      "tierName": "L",
      "isUsed": false,
      "isActive": true,
      "expiryDate": "2026-10-10T10:30:00Z",
      "recipientPhone": "+905321111111",
      "recipientName": "Ali Kaya",
      "linkSentDate": "2025-10-10T11:00:00Z",
      "distributionChannel": "SMS",
      "dealerId": null,
      "dealerTransferDate": null
    }
  ]
}
```

---

### 4.2 Kod Doğrulama
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/validate/{code}`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Kodu kullanmadan geçerliliğini kontrol etme

**Response:**
```json
{
  "success": true,
  "data": {
    "code": "AGRI-2025-X3K9",
    "isValid": true,
    "tierName": "L",
    "expiryDate": "2026-10-10T10:30:00Z",
    "isUsed": false,
    "message": "Kod geçerli ve kullanılabilir"
  }
}
```

---

### 4.3 Manuel Kod Oluşturma
**Endpoint:** `POST {{base_url}}/api/v{{version}}/sponsorship/codes`

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "subscriptionTierId": 3,
  "quantity": 1,
  "codePrefix": "SPECIAL",
  "validityDays": 180
}
```

**Kullanım:** Tek tek kod oluşturma (özel kampanyalar için)

---

## 5. Dealer Yönetimi

### 5.1 Dealer Davet Gönderme
**Endpoint:** `POST {{base_url}}/api/v{{version}}/sponsorship/dealer/invite`

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "dealerName": "Çankaya Tarım Bayi",
  "dealerEmail": "info@cankatari.com",
  "dealerPhone": "+905329999999",
  "initialCodeCount": 20,
  "purchaseId": 2001,
  "notes": "Ankara bölge bayisi"
}
```

**Kullanım:** Dealer'a davet gönderme ve otomatik kod transferi

---

### 5.2 Dealer Davet Listesi
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/dealer/invitations`

**Headers:** `Authorization: Bearer {token}`

**Query Parameters:**
- `status`: "Pending", "Accepted", "Rejected", "Expired"
- `page`, `pageSize`

**Kullanım:** Gönderilmiş davetleri listeleme

---

### 5.3 Dealer'a Kod Transferi
**Endpoint:** `POST {{base_url}}/api/v{{version}}/sponsorship/dealer/transfer-codes`

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "dealerId": 158,
  "purchaseId": 2001,
  "codeCount": 50
}
```

**Kullanım:** Mevcut dealer'a ek kod transferi

---

### 5.4 Dealer'dan Kod Geri Alma
**Endpoint:** `POST {{base_url}}/api/v{{version}}/sponsorship/dealer/reclaim-codes`

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "dealerId": 158,
  "codeCount": 10
}
```

**Kullanım:** Dealer'dan kullanılmamış kod geri alma

---

### 5.5 Dealer Dashboard Özeti
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/dealer/my-dashboard`

**Headers:** `Authorization: Bearer {token}` (Dealer rolü)

**Kullanım:** Dealer'ın kendi kod durumunu görüntüleme (self-service)

---

### 5.6 Dealer Performans Raporu
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/dealer/performance/{dealerId}`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Belirli dealer'ın performans metriklerini görme

---

### 5.7 Dealer Listesi
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/dealer/summary`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Sponsor'un tüm dealer'larını listeleme

---

### 5.8 Dealer Invitation Detayları
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/dealer/invitation-details/{token}`

**Kullanım:** Token ile davet detayları (public, dealer accept için)

---

## 6. Farmer İlişkileri

### 6.1 Sponsored Farmers Listesi
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/farmers`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Sponsor'un kodlarını kullanan farmer'ları listeleme

**Response (Tier-Based):**
```json
{
  "success": true,
  "data": [
    {
      "farmerId": 5001,
      "farmerName": "Ali Kaya",
      "farmerEmail": "ali@example.com",
      "farmerPhone": "+905321111111",
      "location": {
        "city": "Ankara",
        "district": "Çankaya"
      },
      "redeemedCode": "AGRI-2025-X3K9",
      "redeemedDate": "2025-10-10T12:30:00Z",
      "subscriptionStatus": "Active",
      "totalAnalysisCount": 15
    }
  ]
}
```

**Notlar:**
- S Tier: farmerName = "Anonymous", email/phone = null
- M Tier: farmerName = "Anonymous", ama location detaylı
- L/XL Tier: Tam detay

---

### 6.2 Sponsored Analyses Listesi
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/analyses`

**Headers:** `Authorization: Bearer {token}`

**Query Parameters:**
- `farmerId` (int): Belirli farmer'ın analizleri
- `startDate`, `endDate`: Tarih filtresi
- `page`, `pageSize`

**Kullanım:** Sponsored farmer'ların analizlerini listeleme

---

### 6.3 Analiz Detayı
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/analysis/{id}`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Belirli analiz detayını görme (tier-based)

---

## 7. Messaging (İletişim)

**Not:** L ve XL tier'ler için aktif

### 7.1 Mesajlaşma Özellik Kontrolü
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/messaging/features`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Sponsor'un messaging özelliğinin aktif olup olmadığını kontrol

---

### 7.2 Mesaj Gönderme (Text)
**Endpoint:** `POST {{base_url}}/api/v{{version}}/sponsorship/messages`

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "toUserId": 5001,
  "message": "Merhaba, analizinizde buğday pası tespit ettik...",
  "messageType": "ProductRecommendation"
}
```

**Kullanım:** Farmer'a text mesaj gönderme

---

### 7.3 Görsel Mesaj Gönderme
**Endpoint:** `POST {{base_url}}/api/v{{version}}/sponsorship/messages/image`

**Headers:** `Authorization: Bearer {token}`, `Content-Type: multipart/form-data`

**Body (Form Data):**
- `toUserId`: 5001
- `message`: "Ürün resmi ekledim"
- `imageFile`: [binary file]

**Kullanım:** Görsel ile mesaj gönderme (M+ tier)

---

### 7.4 Sesli Mesaj Gönderme
**Endpoint:** `POST {{base_url}}/api/v{{version}}/sponsorship/messages/voice`

**Headers:** `Authorization: Bearer {token}`, `Content-Type: multipart/form-data`

**Body (Form Data):**
- `toUserId`: 5001
- `voiceFile`: [binary audio]

**Kullanım:** Sesli mesaj gönderme (XL tier exclusive)

---

### 7.5 Konuşma Geçmişi
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/messages/conversation`

**Headers:** `Authorization: Bearer {token}`

**Query Parameters:**
- `farmerId`: 5001
- `plantAnalysisId`: 123 (opsiyonel)

**Kullanım:** Belirli farmer ile konuşma geçmişini görme

---

### 7.6 Mesaj Düzenleme
**Endpoint:** `PUT {{base_url}}/api/v{{version}}/sponsorship/messages/{messageId}`

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "message": "Güncellenmiş mesaj içeriği"
}
```

**Kullanım:** Gönderilmiş mesajı düzenleme (1 saat içinde)

---

### 7.7 Mesaj Silme
**Endpoint:** `DELETE {{base_url}}/api/v{{version}}/sponsorship/messages/{messageId}`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Gönderilmiş mesajı silme

---

### 7.8 Okunmamış Mesaj Sayısı
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/messages/unread-count`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Okunmamış mesaj sayısını görme

---

## 8. Analytics & Raporlama

### 8.1 Dashboard Özeti
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/dashboard-summary`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Sponsor dashboard ana metrikler (cached 15 min)

**Response:**
```json
{
  "success": true,
  "data": {
    "totalInvestment": 20000.00,
    "totalCodesPurchased": 100,
    "totalCodesDistributed": 85,
    "totalCodesRedeemed": 50,
    "redemptionRate": 0.588,
    "activeSponsoredFarmers": 50,
    "totalAnalyses": 750,
    "expiringSubscriptions": 12
  }
}
```

---

### 8.2 ROI Analytics
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/analytics/roi`

**Headers:** `Authorization: Bearer {token}`

**Query Parameters:**
- `startDate`, `endDate`

**Kullanım:** Yatırım getirisi analizi (cached 12 hours)

---

### 8.3 Temporal Analytics
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/analytics/temporal`

**Headers:** `Authorization: Bearer {token}`

**Query Parameters:**
- `startDate`, `endDate`
- `groupBy`: "day", "week", "month"

**Kullanım:** Zaman bazlı trend analizi (cached 1 hour)

---

### 8.4 Messaging Analytics
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/analytics/messaging`

**Headers:** `Authorization: Bearer {token}`

**Query Parameters:**
- `startDate`, `endDate`

**Kullanım:** Mesajlaşma istatistikleri (cached 15 min)

---

### 8.5 Impact Analytics
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/analytics/impact`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Sponsorluk etkisi analizi (cached 6 hours)

---

### 8.6 Temporal Metrics (Dealer)
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/analytics/dealer/temporal-metrics`

**Headers:** `Authorization: Bearer {token}`

**Query Parameters:**
- `dealerId`
- `startDate`, `endDate`

**Kullanım:** Dealer bazlı zaman serisi metrikleri

---

### 8.7 Temporal Metrics (Sponsor)
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/analytics/sponsor/temporal-metrics`

**Headers:** `Authorization: Bearer {token}`

**Query Parameters:**
- `startDate`, `endDate`

**Kullanım:** Sponsor bazlı zaman serisi metrikleri

---

### 8.8 Engagement Metrics
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/analytics/engagement-metrics`

**Headers:** `Authorization: Bearer {token}`

**Query Parameters:**
- `startDate`, `endDate`

**Kullanım:** Farmer engagement metrikleri

---

### 8.9 Conversion Metrics
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/analytics/conversion-metrics`

**Headers:** `Authorization: Bearer {token}`

**Query Parameters:**
- `startDate`, `endDate`

**Kullanım:** Dönüşüm oranları analizi

---

## 9. Smart Links (XL Tier)

**Not:** Sadece XL tier sponsors için

### 9.1 Smart Link Oluşturma
**Endpoint:** `POST {{base_url}}/api/v{{version}}/sponsorship/smart-links`

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "linkUrl": "https://agritech.com.tr/products/fungicide-xyz",
  "linkText": "XYZ Fungisit - Buğday Pası İçin",
  "linkDescription": "Etkili ve hızlı sonuç veren fungisit çözümü",
  "keywords": ["buğday pası", "fungal hastalık", "wheat rust"],
  "targetCropTypes": ["Buğday", "Arpa"],
  "targetDiseases": ["Wheat Rust"],
  "priority": 80,
  "productPrice": 250.00,
  "discountPercentage": 15.0
}
```

**Kullanım:** AI-powered contextual product link oluşturma (max 50)

---

### 9.2 Smart Link Listesi
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/smart-links`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Oluşturulmuş smart link'leri listeleme

---

### 9.3 Smart Link Güncelleme
**Endpoint:** `PUT {{base_url}}/api/v{{version}}/sponsorship/smart-links/{id}`

**Headers:** `Authorization: Bearer {token}`

**Body:** (Smart link create ile aynı format)

**Kullanım:** Mevcut smart link'i güncelleme

---

### 9.4 Smart Link Silme
**Endpoint:** `DELETE {{base_url}}/api/v{{version}}/sponsorship/smart-links/{id}`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Smart link'i deaktif etme

---

### 9.5 Smart Link Performans
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/smart-links/performance`

**Headers:** `Authorization: Bearer {token}`

**Kullanım:** Smart link'lerin CTR, click, impression metrikleri

---

## 10. Logo & Görünürlük

### 10.1 Logo Display Permissions
**Endpoint:** `GET {{base_url}}/api/v{{version}}/sponsorship/logo-permissions`

**Headers:** `Authorization: Bearer {token}`

**Query Parameters:**
- `plantAnalysisId`: 123
- `screen`: "start", "result", "analysis", "profile"

**Kullanım:** Belirli ekranda logo gösterilip gösterilmeyeceğini kontrol

---

### 10.2 Logo Display Toggle
**Endpoint:** `POST {{base_url}}/api/v{{version}}/sponsorship/toggle-logo-display`

**Headers:** `Authorization: Bearer {token}`

**Body:**
```json
{
  "featureName": "sponsor_visibility_result",
  "isEnabled": true
}
```

**Kullanım:** Database-driven logo visibility toggle (Admin only)

---

## 📊 Özet Tablo

| Kategori | Endpoint Sayısı | Tier Kısıtı |
|----------|----------------|-------------|
| **Kimlik Doğrulama** | 7 | Yok |
| **Profil Yönetimi** | 3 | Yok |
| **Paket Satın Alma** | 3 | Yok |
| **Kod Yönetimi** | 4 | Yok |
| **Dealer Yönetimi** | 8 | Yok |
| **Farmer İlişkileri** | 3 | Tier-based data |
| **Messaging** | 8 | L, XL only |
| **Analytics** | 9 | Yok |
| **Smart Links** | 5 | XL only |
| **Logo & Görünürlük** | 2 | Tier-based |
| **TOPLAM** | **52** | - |

---

## 🔒 Tier-Based Özellik Matrisi

| Özellik | S Tier | M Tier | L Tier | XL Tier |
|---------|--------|--------|--------|---------|
| **Farmer Data** | %30 (anonim) | %60 (anonim) | %100 (tam) | %100 (tam) |
| **Logo Display** | Start only | Start + Result | All screens | All screens |
| **Messaging** | ❌ | ❌ | ✅ | ✅ |
| **Voice Messages** | ❌ | ❌ | ❌ | ✅ |
| **Smart Links** | ❌ | ❌ | ❌ | ✅ (50 max) |
| **Analytics** | Basic | Basic | Advanced | Advanced |

---

## 🚨 Önemli Notlar

### Authentication
- JWT token expiry: 60 dakika
- Refresh token ile yenileme yapılmalı
- Phone-based auth için OTP gerekli

### Pagination
- Default page size: 10
- Max page size: 200 (codes), 100 (analyses)
- Zero-based veya 1-based indexing: **1-based** (page=1 ilk sayfa)

### Caching
- Dashboard summary: 15 min
- Analytics: 1-12 hours (endpoint'e göre)
- Cache invalidation: Purchase/transfer sonrası otomatik

### Rate Limiting
- Messaging: 10 mesaj/gün per farmer
- SMS distribution: 100 recipient/request

### Dealer Distribution
- `excludeDealerTransferred=true` kullanarak sadece sponsor'un doğrudan kontrolündeki kodları filtrele
- Dealer'a transfer edilen kodlar sponsor tarafından tekrar dağıtılamaz

---

## 📚 İlgili Dokümanlar

- [SPONSOR_PERSONA_COMPLETE_JOURNEY_REPORT.md](./SPONSOR_PERSONA_COMPLETE_JOURNEY_REPORT.md)
- [Sponsor_API_Endpoints_Analysis.md](./Sponsor_API_Endpoints_Analysis.md)
- [SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md](./SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md)
- [MOBILE_TEAM_SPONSOR_PROFILE_API_DOCUMENTATION.md](./MOBILE_TEAM_SPONSOR_PROFILE_API_DOCUMENTATION.md)

---

**Son Güncelleme:** 2025-11-02
**Branch:** `feature/sponsor-web-scenarios`
**Durum:** ✅ Backend endpoints hazır, web UI geliştirmesi için referans
