# Subscription Systems Comparison Guide

**Document Version:** 1.0
**Last Updated:** 2025-01-10
**Author:** ZiraAI Development Team

---

## 📋 Table of Contents

1. [Executive Summary](#executive-summary)
2. [System Overview](#system-overview)
3. [Detailed Comparison](#detailed-comparison)
4. [Technical Architecture](#technical-architecture)
5. [User Experience Flows](#user-experience-flows)
6. [Use Cases & Scenarios](#use-cases--scenarios)
7. [API Integration Guide](#api-integration-guide)
8. [Decision Matrix](#decision-matrix)

---

## Executive Summary

ZiraAI platformunda **iki farklı subscription atama sistemi** bulunmaktadır:

### 🎫 Sponsorlu Kod Dağıtımı (Code-Based System)
Sponsorların satın aldığı paketleri farmer'lara **kod bazlı** dağıtım yapan sistem. Farmer'lar kodu manuel olarak girerek (redeem) subscription'larını aktif ederler.

### 🎯 Admin Toplu Subscription Ataması (Direct Assignment System)
Admin'lerin farmer'lara **doğrudan subscription** atadığı sistem. Kod gerektirmez, otomatik aktivasyon sağlar.

---

## System Overview

### 🎫 Sponsorlu Kod Dağıtımı (BulkCodeDistribution)

#### Temel Özellikler
- ✅ **Sponsor Tabanlı:** Sponsor önce paket satın alır
- ✅ **Kod Oluşturma:** Her farmer için 6 haneli benzersiz kod
- ✅ **Manuel Redeem:** Farmer mobil uygulamada kod girer
- ✅ **SMS ile Kod Gönderimi:** Kod + talimat içerir
- ✅ **Kod Takibi:** Her kodun durumu (kullanıldı/kullanılmadı) takip edilir
- ✅ **Sponsor Analytics:** Kod kullanım istatistikleri

#### Workflow
```
Sponsor → Paket Satın Al → Admin Kod Dağıtımı → SMS (KOD) → Farmer Kod Girer → Subscription Aktif
```

#### Veritabanı Akışı
```sql
1. SponsorshipPurchase (sponsor paket satın alır)
2. SponsorshipCode (her farmer için kod oluşturulur)
3. BulkCodeDistributionJob (job tracking)
4. SMS gönderimi (kod içerir)
5. Farmer kod girer (mobile app)
6. RedeemCode endpoint çağrılır
7. UserSubscription oluşturulur (kod doğrulandıktan sonra)
```

---

### 🎯 Admin Toplu Subscription Ataması (BulkSubscriptionAssignment)

#### Temel Özellikler
- ✅ **Admin Tabanlı:** Sponsor gerekmez, admin yetkisi yeterli
- ❌ **Kod Yok:** Kod oluşturulmaz, takip edilmez
- ✅ **Otomatik Aktivasyon:** Subscription direkt veritabanına yazılır
- ✅ **SMS Bilgilendirme (Opsiyonel):** Sadece bilgilendirme amaçlı
- ✅ **Kullanıcı Oluşturma:** Farmer hesabı yoksa otomatik oluşturulur
- ✅ **Subscription Güncelleme:** Mevcut subscription varsa günceller

#### Workflow
```
Admin → Excel Upload → RabbitMQ Queue → Hangfire Job → Subscription Created → SMS (BİLGİLENDİRME)
```

#### Veritabanı Akışı
```sql
1. BulkSubscriptionAssignmentJob (job tracking)
2. User (yoksa oluşturulur, varsa bulunur)
3. UserSubscription (direkt oluşturulur veya güncellenir)
4. SMS gönderimi (SADECE bilgilendirme, kod YOK)
5. Farmer uygulamayı açar → Subscription zaten aktif ✅
```

---

## Detailed Comparison

### Feature Matrix

| **Özellik** | **Sponsorlu Kod Dağıtımı** | **Admin Bulk Subscription** |
|-------------|---------------------------|----------------------------|
| **Kod Oluşturma** | ✅ 6 haneli benzersiz kod | ❌ Kod yok |
| **Redeem Gereksinimi** | ✅ Farmer manuel girmeli | ❌ Otomatik aktivasyon |
| **Sponsor Gereksinimi** | ✅ Sponsor paket satın almalı | ❌ Admin yetkisi yeterli |
| **SMS İçeriği** | Kod + Talimat | Bilgilendirme (kod yok) |
| **Kullanıcı Yoksa** | Kod beklemede kalır | Kullanıcı otomatik oluşturulur |
| **Mevcut Subscription** | Redeem sırasında kontrol | Otomatik güncellenir |
| **Aktivasyon Süresi** | Farmer'ın kod girmesine bağlı | Anlık (job işlendiğinde) |
| **Kod Takibi** | ✅ SponsorshipCode tablosu | ❌ Kod takibi yok |
| **Sponsor Analytics** | ✅ Kod kullanım istatistikleri | ❌ Sadece job istatistikleri |
| **Payment Gateway** | ✅ Sponsor ödeme yapar | ❌ Ödeme yok |
| **Kullanım Senaryosu** | B2C (sponsor → farmer) | B2B, Promo, Admin yönetimli |
| **Frontend Ekranı** | Kod giriş ekranı gerekli | Kod ekranı gerekmez |
| **API Endpoint** | `/api/v1/redemption/redeem-code` | `/api/v1/admin/subscriptions/bulk-assignment` |

---

### SMS Content Comparison

#### 🎫 Sponsorlu Kod Dağıtımı SMS
```
Sayın Ahmet,

Size ZiraAI platformunda kullanabileceğiniz bir subscription kodu gönderildi:

KOD: ABC123

Kodu kullanmak için:
1. ZiraAI mobil uygulamasını açın
2. "Kod Gir" bölümüne gidin
3. ABC123 kodunu girin
4. Subscription'ınız aktif olacaktır

Teşekkürler,
ZiraAI Ekibi
```

**Özellikler:**
- ✅ 6 haneli kod var
- ✅ Kullanım talimatı
- ✅ Redeem işlemi gerekli

---

#### 🎯 Admin Bulk Subscription SMS
```
Sayın Ahmet,

Size Medium (M) paketi tanımlandı. 30 gün boyunca kullanabilirsiniz.

Paketiniz otomatik olarak aktif edildi. Detaylar için uygulamayı ziyaret edin.

Teşekkürler,
ZiraAI Ekibi
```

**Özellikler:**
- ❌ Kod yok
- ✅ Bilgilendirme amaçlı
- ❌ Redeem işlemi gerekmez
- ✅ Direkt kullanıma hazır

---

## Technical Architecture

### 🎫 Sponsorlu Kod Dağıtımı Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    SPONSORLU KOD DAĞITIMI                        │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐
│   Sponsor    │ (1) Paket satın alır
└──────┬───────┘
       │
       v
┌──────────────────────────┐
│ SponsorshipPurchase DB   │ (2) Purchase kaydı
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│  Admin Excel Upload      │ (3) Farmer listesi
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│  BulkCodeDistribution    │ (4) Kod oluşturma servisi
│       Service            │
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│  SponsorshipCode DB      │ (5) Her farmer için KOD
│  (ABC123, DEF456, ...)   │
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│   RabbitMQ Queue         │ (6) Kod dağıtım kuyruğu
│ (farmer-code-dist...)    │
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│  Hangfire Background     │ (7) Her farmer için job
│      Job Service         │
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│   SMS Service            │ (8) SMS (KOD içerir)
│  (Kod: ABC123)           │
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│   Farmer Mobile App      │ (9) Kod giriş ekranı
│   "Kod Gir: ABC123"      │
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│  RedeemCode Endpoint     │ (10) Kod doğrulama
│  POST /redemption/       │
│       redeem-code        │
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│  UserSubscription DB     │ (11) Subscription aktif
│  (Status: Active)        │
└──────────────────────────┘
```

---

### 🎯 Admin Bulk Subscription Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│              ADMIN BULK SUBSCRIPTION ASSIGNMENT                  │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐
│    Admin     │ (1) Excel upload (kod yok!)
└──────┬───────┘
       │
       v
┌──────────────────────────┐
│  AdminBulkSubscription   │ (2) Excel parse + validation
│      Controller          │
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│ BulkSubscriptionAssign   │ (3) Job oluştur (KOD YOK!)
│       Service            │
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│ BulkSubscriptionAssign   │ (4) Job tracking
│      Job DB              │
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│   RabbitMQ Queue         │ (5) Subscription assignment
│ (farmer-subscription-    │     queue
│  assignment-requests)    │
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│  Hangfire Background     │ (6) Her farmer için job
│      Job Service         │
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│   User Lookup/Create     │ (7) Email veya Phone ile ara
│  (User DB)               │     Yoksa oluştur!
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│  UserSubscription DB     │ (8) DİREKT oluştur/güncelle
│  (Status: Active)        │     (KOD KONTROLÜ YOK!)
│  ✅ ANINDA AKTİF         │
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│   SMS Service            │ (9) SMS (BİLGİLENDİRME)
│ (Paket tanımlandı)       │     KOD YOK!
└──────────┬───────────────┘
           │
           v
┌──────────────────────────┐
│   Farmer Mobile App      │ (10) Uygulama açılır
│ ✅ Subscription HAZIR    │      Kod gerekmez!
└──────────────────────────┘
```

---

## User Experience Flows

### 🎫 Sponsorlu Kod Dağıtımı - Farmer Journey

```
┌─────────────────────────────────────────────────────────────────┐
│                      FARMER JOURNEY (KOD SİSTEMİ)                │
└─────────────────────────────────────────────────────────────────┘

ADIM 1: SMS Gelir
┌───────────────────────────────────┐
│ 📱 SMS: "Kodunuz: ABC123"         │
│    Uygulamada kod giriş bölümüne  │
│    gidin ve kodu kullanın.        │
└───────────────────────────────────┘
         │
         v
ADIM 2: Uygulama Açma
┌───────────────────────────────────┐
│ 📱 ZiraAI Mobil App                │
│    → Ana Sayfa                     │
│    → "Kod Gir" butonuna bas        │
└───────────────────────────────────┘
         │
         v
ADIM 3: Kod Giriş Ekranı
┌───────────────────────────────────┐
│ 📱 Kod Giriş Ekranı                │
│                                   │
│    [_A_] [_B_] [_C_] [_1_] [_2_] [_3_] │
│                                   │
│    [Kodu Kullan] butonu           │
└───────────────────────────────────┘
         │
         v
ADIM 4: Kod Doğrulama (Backend)
┌───────────────────────────────────┐
│ 🔄 POST /redemption/redeem-code   │
│    - Kod geçerli mi?              │
│    - Daha önce kullanıldı mı?     │
│    - Sponsor paketi yeterli mi?   │
└───────────────────────────────────┘
         │
         v
ADIM 5: Subscription Oluşturma
┌───────────────────────────────────┐
│ ✅ UserSubscription Created        │
│    Status: Active                 │
│    StartDate: 2025-01-10          │
│    EndDate: 2025-02-09 (30 gün)   │
└───────────────────────────────────┘
         │
         v
ADIM 6: Başarı Mesajı
┌───────────────────────────────────┐
│ 📱 "Tebrikler! Medium (M) paketiniz│
│    başarıyla aktif edildi.        │
│    30 gün boyunca kullanabilirsiniz│
└───────────────────────────────────┘

⏱️ Toplam Süre: 2-5 dakika (farmer hızına bağlı)
👤 Kullanıcı Etkileşimi: YÜKSEK (kod girişi gerekli)
```

---

### 🎯 Admin Bulk Subscription - Farmer Journey

```
┌─────────────────────────────────────────────────────────────────┐
│              FARMER JOURNEY (BULK SUBSCRIPTION)                  │
└─────────────────────────────────────────────────────────────────┘

ADIM 1: SMS Gelir (Opsiyonel)
┌───────────────────────────────────┐
│ 📱 SMS: "Size Medium paketi        │
│    tanımlandı. 30 gün kullanabilir│
│    siniz. Uygulama açın."         │
│    (KOD YOK!)                     │
└───────────────────────────────────┘
         │
         v
ADIM 2: Uygulama Açma
┌───────────────────────────────────┐
│ 📱 ZiraAI Mobil App                │
│    → Ana Sayfa                     │
│    ✅ Subscription ZATEN AKTİF!    │
└───────────────────────────────────┘
         │
         v
ADIM 3: Direkt Kullanım
┌───────────────────────────────────┐
│ 📱 Bitki Analizi Ekranı            │
│    ✅ "Medium (M) Paket Aktif"     │
│    📊 30 gün kaldı                 │
│    🔍 Analiz yapabilir             │
│                                   │
│    [Fotoğraf Çek] butonu          │
└───────────────────────────────────┘

⏱️ Toplam Süre: 0 dakika (otomatik!)
👤 Kullanıcı Etkileşimi: YOK (kod gerekmez)
🎯 Friction: SIFIR
```

---

## Use Cases & Scenarios

### 🎫 Sponsorlu Kod Dağıtımı - Kullanım Senaryoları

#### Senaryo 1: Tarım İlaçları Firması Sponsorluğu
```
Durum:
- Bayer Tarım İlaçları 1000 farmer'a M paketi sponsor olmak istiyor
- Farmer'lar kodu alıp kullanacak
- Bayer kod kullanım istatistiklerini takip edecek

Workflow:
1. Bayer admin panelden 1000 adet M paketi satın alır (ödeme gateway)
2. Admin Bayer için Excel upload yapar (1000 farmer bilgisi)
3. Her farmer için benzersiz kod oluşturulur (ABC123, DEF456, ...)
4. SMS gönderilir: "Kodunuz: ABC123"
5. Farmer uygulamada kod girer
6. Bayer analytics panelinde kod kullanım oranlarını görür:
   - 850 kod kullanıldı (%85)
   - 150 kod henüz kullanılmadı (%15)

Avantajlar:
✅ Sponsor takip edebilir
✅ Kod bazlı raporlama
✅ Farmer manuel onay (engagement)
```

---

#### Senaryo 2: Gübre Firması Mevsimsel Kampanya
```
Durum:
- Gübre firması bahar döneminde 500 farmer'a L paketi veriyor
- 60 günlük sınırlı kampanya
- Kullanılmayan kodların iptali gerekebilir

Workflow:
1. Sponsor 500 adet L paketi satın alır
2. Admin kod dağıtımı yapar
3. Farmer'lar 15 gün içinde kod girmeli (deadline)
4. Kullanılmayan kodlar sponsor'a iade edilebilir
5. Sponsor yeni farmer'lara kod transfer edebilir

Avantajlar:
✅ Kod expiry tarihi kontrolü
✅ Kullanılmayan kod iadesi
✅ Kod transfer esnekliği
```

---

### 🎯 Admin Bulk Subscription - Kullanım Senaryoları

#### Senaryo 1: Tarım Bakanlığı Proje Desteği
```
Durum:
- Tarım Bakanlığı 5000 farmer'a 1 yıl XL paketi vermek istiyor
- B2B anlaşma, ödeme yok
- Farmer'ların kod girmesine gerek yok (hızlı aktivasyon)

Workflow:
1. Bakanlık farmer listesini Excel ile gönderir (email/telefon)
2. Admin Excel'i upload eder (kod oluşturmaz!)
3. Sistem otomatik kullanıcı oluşturur (yoksa)
4. Subscription direkt veritabanına yazılır (Status: Active)
5. SMS bilgilendirme gönderilir (opsiyonel)
6. Farmer uygulamayı açar → Subscription hazır ✅

Avantajlar:
✅ Hızlı toplu aktivasyon (5000 farmer anında)
✅ Kod yönetimi yok
✅ Farmer friction sıfır
✅ Kullanıcı hesabı yoksa otomatik oluşturulur
```

---

#### Senaryo 2: Yeni Yıl Promo Kampanyası
```
Durum:
- ZiraAI tüm mevcut kullanıcılara 30 gün S paketi hediye
- 50.000 aktif kullanıcı
- Hızlı dağıtım gerekli

Workflow:
1. Admin tüm kullanıcı listesini çeker (export)
2. Excel hazırlar (email, 30 gün, S tier)
3. Bulk subscription upload eder
4. RabbitMQ + Hangfire ile 2-3 saat içinde tamamlanır
   (~5-7 farmer/saniye)
5. SMS bilgilendirme (opsiyonel, maliyetli olabilir)
6. Kullanıcılar uygulamayı açar → Hediye paket aktif ✅

Avantajlar:
✅ Çok hızlı dağıtım (50K farmer)
✅ Kod lojistiği yok
✅ Mevcut subscription'lar otomatik güncellenir
✅ SMS opsiyonel (maliyet kontrolü)
```

---

#### Senaryo 3: Tarım Kooperatifi Toplu Üyelik
```
Durum:
- Yerel tarım kooperatifi 200 üyesine subscription verecek
- Üyelerin %30'u henüz ZiraAI hesabı yok
- Hızlı onboarding gerekli

Workflow:
1. Kooperatif üye listesi gönderir (bazılarının hesabı yok)
2. Admin Excel upload eder
3. Sistem otomatik:
   - Hesabı olan: Subscription atar
   - Hesabı olmayan: Kullanıcı oluşturur + Subscription atar
4. Tüm üyeler SMS alır
5. Yeni üyeler ilk giriş → Subscription zaten aktif ✅

Avantajlar:
✅ Kullanıcı yoksa otomatik oluşturur
✅ Pre-activation (hesap açılmadan önce)
✅ Onboarding friction sıfır
```

---

#### Senaryo 4: Beta Tester Grubu
```
Durum:
- ZiraAI yeni AI model test için 100 farmer seçti
- 90 günlük test süresi
- Hızlı aktivasyon + test başlangıcı

Workflow:
1. Product team test farmer listesi hazırlar
2. Admin 100 farmer'a 90 gün XL paketi atar (AI features)
3. Otomatik aktivasyon
4. Test başlar (kod girişi ile zaman kaybı yok)

Avantajlar:
✅ Anında test başlangıcı
✅ Kod karmaşası yok
✅ Tüm tester'lar eşzamanlı başlar
```

---

## API Integration Guide

### 🎫 Sponsorlu Kod Dağıtımı API Flow

#### Step 1: Upload Farmer List for Code Distribution
```http
POST /api/v1/admin/code-distribution/bulk
Authorization: Bearer {admin_token}
Content-Type: multipart/form-data

Form Data:
- excelFile: <FILE> (Excel with farmer data)
- sponsorId: 42 (required)
- sendSms: true (optional, default: true)
```

**Response:**
```json
{
  "data": {
    "jobId": 123,
    "totalFarmers": 150,
    "codesGenerated": 150,
    "status": "Processing",
    "createdDate": "2025-01-10T14:30:00Z"
  },
  "success": true,
  "message": "Kod dağıtımı başlatıldı. 150 farmer için kod oluşturuldu."
}
```

---

#### Step 2: Farmer Redeems Code (Mobile App)
```http
POST /api/v1/redemption/redeem-code
Authorization: Bearer {farmer_token}
Content-Type: application/json

{
  "code": "ABC123"
}
```

**Response:**
```json
{
  "data": {
    "subscriptionId": 456,
    "tierId": 2,
    "tierName": "Medium (M)",
    "startDate": "2025-01-10T15:00:00Z",
    "endDate": "2025-02-09T15:00:00Z",
    "durationDays": 30,
    "status": "Active"
  },
  "success": true,
  "message": "Kod başarıyla kullanıldı. Medium (M) paketiniz aktif edildi."
}
```

---

### 🎯 Admin Bulk Subscription API Flow

#### Step 1: Upload Farmer List for Direct Subscription
```http
POST /api/v1/admin/subscriptions/bulk-assignment
Authorization: Bearer {admin_token}
Content-Type: multipart/form-data

Form Data:
- excelFile: <FILE> (Excel with farmer data)
- defaultTierId: 2 (optional, S tier)
- defaultDurationDays: 30 (optional)
- sendNotification: true (optional, default: true)
- notificationMethod: "SMS" (optional, "SMS" | "Email")
- autoActivate: true (optional, default: true)
```

**Response:**
```json
{
  "data": {
    "jobId": 789,
    "totalFarmers": 150,
    "status": "Processing",
    "createdDate": "2025-01-10T14:30:00Z",
    "estimatedCompletionTime": "2025-01-10T15:45:00Z",
    "statusCheckUrl": "/api/v1/admin/subscriptions/bulk-assignment/status/789"
  },
  "success": true,
  "message": "Toplu subscription atama işlemi başlatıldı. 150 farmer kuyruğa eklendi."
}
```

---

#### Step 2: Check Job Status
```http
GET /api/v1/admin/subscriptions/bulk-assignment/status/789
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "data": {
    "jobId": 789,
    "status": "Completed",
    "totalFarmers": 150,
    "processedFarmers": 150,
    "successfulAssignments": 145,
    "failedAssignments": 5,
    "newSubscriptionsCreated": 120,
    "existingSubscriptionsUpdated": 25,
    "totalNotificationsSent": 140,
    "startedDate": "2025-01-10T14:30:00Z",
    "completedDate": "2025-01-10T15:15:00Z",
    "processingTimeMinutes": 45,
    "resultFileUrl": "https://ziraai.com/files/result_789.xlsx"
  },
  "success": true,
  "message": "Job completed successfully"
}
```

---

#### Step 3: Farmer Opens App (NO ACTION REQUIRED)
```http
GET /api/v1/farmer/subscription/current
Authorization: Bearer {farmer_token}
```

**Response (Subscription Already Active):**
```json
{
  "data": {
    "subscriptionId": 999,
    "userId": 123,
    "tierId": 2,
    "tierName": "Medium (M)",
    "displayName": "Medium",
    "startDate": "2025-01-10T14:35:00Z",
    "endDate": "2025-02-09T14:35:00Z",
    "status": "Active",
    "isActive": true,
    "currentDailyUsage": 0,
    "currentMonthlyUsage": 0,
    "dailyLimit": 50,
    "monthlyLimit": 1500,
    "daysRemaining": 30
  },
  "success": true,
  "message": "Active subscription found"
}
```

**✅ Farmer sees subscription already active - NO CODE ENTRY NEEDED!**

---

## Decision Matrix

### When to Use Each System

```
┌─────────────────────────────────────────────────────────────────┐
│                     DECISION MATRIX                              │
└─────────────────────────────────────────────────────────────────┘

                          🎫 Sponsorlu Kod     🎯 Bulk Subscription
                             Dağıtımı                Assignment
┌────────────────────────┼────────────────────┼──────────────────────┐
│ Sponsor var mı?        │     ✅ EVET        │     ❌ HAYIR         │
├────────────────────────┼────────────────────┼──────────────────────┤
│ Kod takibi gerekli mi? │     ✅ EVET        │     ❌ HAYIR         │
├────────────────────────┼────────────────────┼──────────────────────┤
│ Analytics gerekli mi?  │     ✅ EVET        │     ⚠️ KISITLI       │
├────────────────────────┼────────────────────┼──────────────────────┤
│ Hızlı aktivasyon?      │     ❌ HAYIR       │     ✅ EVET          │
│                        │  (farmer gerekli)  │   (otomatik)         │
├────────────────────────┼────────────────────┼──────────────────────┤
│ Kullanıcı yoksa?       │  ⚠️ Kod bekler     │  ✅ Oluşturulur      │
├────────────────────────┼────────────────────┼──────────────────────┤
│ B2B anlaşmalar         │     ❌ UYGUN DEĞİL │     ✅ İDEAL         │
├────────────────────────┼────────────────────┼──────────────────────┤
│ Promo kampanyalar      │     ⚠️ YAVAŞ       │     ✅ HIZLI         │
├────────────────────────┼────────────────────┼──────────────────────┤
│ Sponsor destekli       │     ✅ İDEAL       │     ❌ UYGUN DEĞİL   │
├────────────────────────┼────────────────────┼──────────────────────┤
│ Ödeme gateway          │     ✅ VAR         │     ❌ YOK           │
├────────────────────────┼────────────────────┼──────────────────────┤
│ Farmer friction        │     ⚠️ YÜKSEK      │     ✅ SIFIR         │
│                        │  (kod girmeli)     │   (otomatik)         │
└────────────────────────┴────────────────────┴──────────────────────┘
```

---

### Use Case Recommendation Table

| **Senaryo** | **Önerilen Sistem** | **Neden?** |
|-------------|---------------------|------------|
| Sponsor firmalar (Bayer, Syngenta) | 🎫 Kod Dağıtımı | Sponsor analytics, kod takibi |
| Tarım Bakanlığı projesi | 🎯 Bulk Subscription | B2B, hızlı aktivasyon, kod gereksiz |
| Yeni yıl promo (50K farmer) | 🎯 Bulk Subscription | Toplu dağıtım, hız, maliyet |
| Beta test grubu | 🎯 Bulk Subscription | Anında aktivasyon, test başlangıcı |
| Tarım kooperatifi üyelik | 🎯 Bulk Subscription | Kullanıcı oluşturma, pre-activation |
| Mevsimsel kampanya (gübre) | 🎫 Kod Dağıtımı | Kod expiry, transfer esnekliği |
| Destek/telafi paketi | 🎯 Bulk Subscription | Hızlı çözüm, friction sıfır |
| Dealer program | 🎫 Kod Dağıtımı | Dealer analytics, kod bazlı tracking |

---

## Summary

### 🎫 Sponsorlu Kod Dağıtımı - Özet
**En İyi Kullanım:** Sponsor destekli, kod bazlı takip gerektiren durumlar
**Avantajlar:** Analytics, kod yönetimi, sponsor ROI takibi
**Dezavantajlar:** Farmer friction (kod girişi), yavaş aktivasyon

### 🎯 Admin Bulk Subscription - Özet
**En İyi Kullanım:** B2B anlaşmalar, toplu dağıtım, hızlı aktivasyon
**Avantajlar:** Hızlı, friction sıfır, otomatik kullanıcı oluşturma
**Dezavantajlar:** Kod takibi yok, sponsor analytics sınırlı

---

## Contact & Support

**Technical Questions:** dev@ziraai.com
**Integration Support:** api-support@ziraai.com
**Documentation:** https://docs.ziraai.com

---

**Document End**
