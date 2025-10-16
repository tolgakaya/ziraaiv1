# 📱 Sponsor Mobile Application - UX Design Specification

**Document Version:** 2.0
**Last Updated:** 2025-10-10
**Prepared For:** Mobile Design & Development Team
**Platform:** Flutter (iOS & Android)

---

## 📋 Table of Contents

1. [Executive Summary](#executive-summary)
2. [Sponsor Persona](#sponsor-persona)
3. [User Journey Map](#user-journey-map)
4. [Screen-by-Screen Flow](#screen-by-screen-flow)
5. [Feature Matrix by Tier](#feature-matrix-by-tier)
6. [Wireframe Requirements](#wireframe-requirements)
7. [API Integration Reference](#api-integration-reference)
8. [Analytics & Tracking](#analytics--tracking)
9. [Design System Guidelines](#design-system-guidelines)

---

## 🎯 Executive Summary

### Purpose
This document provides complete UX/UI specifications for the sponsor section of the ZiraAI mobile application. It combines persona analysis, user flows, technical requirements, and design guidelines to enable the design team to create an intuitive sponsor experience.

### Sponsor Business Model
- **Purchase-Based**: Sponsors buy subscription packages in bulk
- **Code Distribution**: Distribute codes to farmers via SMS/WhatsApp
- **Tier-Based Features**: Progressive unlocking (S → M → L → XL)
- **Data Access**: Analytics on farmer usage and crop insights
- **ROI Tracking**: Measure campaign effectiveness

### Key Statistics
- **4 Subscription Tiers**: S, M, L, XL
- **Multi-Channel Distribution**: SMS, WhatsApp
- **3 Analytics Levels**: Package stats, code usage, analysis tracking
- **Tier-Based Data Visibility**: 30% (S) → 60% (M) → 100% (L/XL)

---

## 👤 Sponsor Persona

### Primary Persona: "Agricultural Enterprise Sponsor"

```
┌─────────────────────────────────────────────────────────────┐
│ 👤 MEHMET YILMAZ                                            │
│ Marketing Director @ AgriTech Solutions                     │
├─────────────────────────────────────────────────────────────┤
│ Age: 35-45                                                   │
│ Location: Istanbul, Turkey                                   │
│ Company Type: Agricultural Input Supplier                    │
│ Business Model: B2B2C                                        │
├─────────────────────────────────────────────────────────────┤
│ 🎯 GOALS                                                     │
│ • Increase brand awareness among farmers                     │
│ • Gather market intelligence on crop diseases               │
│ • Build farmer loyalty and trust                            │
│ • Track ROI on marketing investments                         │
│ • Direct communication with end-users                        │
├─────────────────────────────────────────────────────────────┤
│ 😫 PAIN POINTS                                               │
│ • No direct channel to farmers (dealers control access)     │
│ • Limited data on actual crop problems                       │
│ • High cost of traditional marketing                         │
│ • Cannot measure campaign effectiveness                      │
│ • Lack of actionable market insights                         │
├─────────────────────────────────────────────────────────────┤
│ 💡 MOTIVATIONS                                               │
│ • Data-driven decision making                                │
│ • Competitive advantage through insights                     │
│ • Building long-term farmer relationships                    │
│ • Demonstrating product value                                │
└─────────────────────────────────────────────────────────────┘
```

### Secondary Persona: "Regional Dealer Sponsor"

```
┌─────────────────────────────────────────────────────────────┐
│ 👤 AYŞE DEMİR                                               │
│ Owner @ Demir Tarım Girdileri                               │
├─────────────────────────────────────────────────────────────┤
│ Age: 28-40                                                   │
│ Location: Adana, Turkey                                      │
│ Company Type: Regional Agricultural Dealer                   │
│ Business Model: B2C                                          │
├─────────────────────────────────────────────────────────────┤
│ 🎯 GOALS                                                     │
│ • Increase customer retention                                │
│ • Provide added value to existing customers                  │
│ • Understand local crop problems                             │
│ • Stand out from competitors                                 │
├─────────────────────────────────────────────────────────────┤
│ 😫 PAIN POINTS                                               │
│ • Limited budget for marketing                               │
│ • Cannot compete with large suppliers                        │
│ • Farmers switch to cheaper alternatives                     │
│ • No customer relationship tools                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 🗺️ User Journey Map

### Complete Journey: Registration → Purchase → Distribution → Analytics

```
┌──────────────────────────────────────────────────────────────────────────────────────┐
│                           SPONSOR USER JOURNEY                                        │
└──────────────────────────────────────────────────────────────────────────────────────┘

Phase 1: ONBOARDING (First-Time User)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📱 Initial Registration
   ↓
   ├─ Phone + OTP (most common in Turkey)
   ├─ Email + Password
   └─ Google/Apple Social Login
   ↓
👤 Default Role: Farmer
   ↓
💼 Create Sponsor Profile
   ├─ Company information
   ├─ Contact details
   ├─ Logo upload
   └─ Business model selection
   ↓
✅ Role Upgraded: Farmer + Sponsor
   ↓
📊 Dashboard Unlocked

Timeline: 5-10 minutes
Emotional State: Curious → Engaged → Confident
Touchpoints: Registration screen → Profile setup → Welcome tour


Phase 2: PACKAGE PURCHASE (Core Action)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📦 Browse Subscription Tiers
   ├─ S Tier: 30% data visibility
   ├─ M Tier: 60% data visibility
   ├─ L Tier: 100% data + messaging
   └─ XL Tier: All features + Smart Links
   ↓
🔢 Select Quantity
   ├─ Minimum: 10 codes
   ├─ Recommended: 50-100 codes
   └─ Enterprise: 500+ codes
   ↓
💳 Payment
   ├─ Credit card
   ├─ Bank transfer
   └─ Invoice (for enterprise)
   ↓
🎉 Codes Generated
   ├─ Format: AGRI-2025-XXXX
   ├─ Validity: 365 days
   └─ Status: Ready to distribute

Timeline: 3-5 minutes
Emotional State: Evaluating → Deciding → Satisfied
Touchpoints: Tier comparison → Quantity selector → Payment → Success confirmation


Phase 3: CODE DISTRIBUTION (Key Activity)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📋 View Available Codes
   ├─ Total purchased: 100
   ├─ Distributed: 50
   ├─ Redeemed: 30
   └─ Available: 50
   ↓
👥 Select Recipients
   ├─ Manual entry (name + phone)
   ├─ Import from contacts
   ├─ Import from CSV/Excel
   └─ Select from farmer database
   ↓
✏️ Customize Message (Optional)
   ├─ Company name
   ├─ Custom greeting
   └─ Promotional text
   ↓
📤 Choose Channel
   ├─ SMS (default, most reliable)
   └─ WhatsApp (requires template approval)
   ↓
🚀 Send Codes
   ├─ Bulk send (max 100 per batch)
   ├─ Real-time delivery tracking
   └─ Success/failure notifications
   ↓
📊 View Send Results
   ├─ Successfully sent: 48
   ├─ Failed: 2
   └─ Pending: 0

Timeline: 10-20 minutes (depends on recipient count)
Emotional State: Organized → Efficient → Accomplished
Touchpoints: Code list → Recipient selection → Message editor → Send confirmation → Results


Phase 4: MONITORING & ANALYTICS (Ongoing)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📊 Dashboard Overview
   ├─ Total codes: 100
   ├─ Distributed: 50 (50%)
   ├─ Redeemed: 30 (60% redemption rate)
   ├─ Active farmers: 30
   └─ Total analyses: 750 (avg 25 per farmer)
   ↓
📈 Package Statistics
   ├─ Distribution funnel
   ├─ Redemption trends
   └─ ROI metrics
   ↓
🔍 Code-Level Analytics
   ├─ Which codes were used
   ├─ Who used them
   ├─ How many analyses per code
   └─ Click through to analysis details
   ↓
🌾 Crop & Disease Insights
   ├─ Top analyzed crops (wheat, tomato, corn)
   ├─ Disease distribution
   ├─ Geographic patterns
   └─ Seasonal trends
   ↓
👨‍🌾 Sponsored Farmers View
   ├─ Farmer profiles (tier-based visibility)
   ├─ Analysis history
   ├─ Engagement metrics
   └─ Contact information (L/XL only)

Timeline: Daily/weekly check-ins (5-10 minutes)
Emotional State: Curious → Informed → Strategic
Touchpoints: Dashboard → Statistics → Farmer profiles → Reports


Phase 5: COMMUNICATION (L/XL Tiers Only)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
💬 Message Sponsored Farmers
   ├─ In-app messaging
   ├─ SMS notifications
   └─ WhatsApp integration
   ↓
🎯 Targeted Campaigns
   ├─ Filter by crop type
   ├─ Filter by disease detected
   ├─ Filter by location
   └─ Filter by engagement level
   ↓
📝 Message Templates
   ├─ Product recommendations
   ├─ Educational content
   ├─ Promotional offers
   └─ Support messages

Timeline: 5-15 minutes per campaign
Emotional State: Strategic → Personalized → Connected
Touchpoints: Farmer list → Message composer → Send → Delivery tracking


Phase 6: SMART LINKS (XL Tier Only)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔗 Create Smart Links
   ├─ AI-powered product recommendations
   ├─ Context-aware (based on analysis)
   └─ Trackable engagement
   ↓
📦 Link Product Catalog
   ├─ Upload product database
   ├─ Map to crop/disease
   └─ Set pricing/availability
   ↓
🤖 AI Recommendation Engine
   ├─ Analyzes farmer's crop issues
   ├─ Matches to sponsor products
   └─ Generates personalized link
   ↓
📊 Track Performance
   ├─ Link views
   ├─ Product clicks
   ├─ Conversion rate
   └─ Revenue attribution

Timeline: Initial setup 30 minutes, ongoing management 10 minutes/week
Emotional State: Innovative → Optimized → Data-driven
Touchpoints: Smart link dashboard → Product mapper → Performance analytics
```

---

## 📱 Screen-by-Screen Flow

### 🏠 Screen 1: Sponsor Dashboard (Home)

**Purpose:** Central hub showing key metrics and quick actions

**Layout:**
```
┌────────────────────────────────────────┐
│  ☰  ZiraAI Sponsor            🔔 [3]   │ ← Header
├────────────────────────────────────────┤
│                                         │
│  👋 Merhaba, Mehmet Yılmaz             │ ← Greeting
│  AgriTech Solutions A.Ş.               │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  📊 ÖZET İSTATİSTİKLER            │  │ ← Stats Card
│  ├──────────────────────────────────┤  │
│  │  Satın Alınan Kod    │    100     │  │
│  │  Dağıtılan Kod       │     50     │  │
│  │  Kullanılan Kod      │     30     │  │
│  │  Aktif Çiftçi        │     30     │  │
│  │  Toplam Analiz       │    750     │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  📈 DAĞITIM HUNİSİ               │  │ ← Funnel Chart
│  │                                   │  │
│  │  Satın Alınan  ████████████  100 │  │
│  │  Dağıtılan     ████████       50 │  │
│  │  Kullanılan    █████          30 │  │
│  │                                   │  │
│  │  Dağıtım Oranı: %50              │  │
│  │  Kullanım Oranı: %60             │  │
│  │  Genel Başarı: %30               │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  🎯 HIZLI AKSİYONLAR              │  │ ← Quick Actions
│  ├──────────────────────────────────┤  │
│  │  [📦 Paket Satın Al]              │  │
│  │  [📤 Kod Gönder]                  │  │
│  │  [📊 Raporları Gör]               │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  🌾 ÜRETİM TRENDLERİ              │  │ ← Insights Card
│  ├──────────────────────────────────┤  │
│  │  1. Buğday         45% (340)     │  │
│  │  2. Domates        30% (225)     │  │
│  │  3. Mısır          15% (113)     │  │
│  │  4. Diğer          10% ( 72)     │  │
│  └──────────────────────────────────┘  │
│                                         │
├────────────────────────────────────────┤
│  [🏠] [📦] [📊] [👤]                   │ ← Bottom Nav
└────────────────────────────────────────┘
```

**Components:**
- Header with notification badge
- Personal greeting + company name
- Stats summary card (5 key metrics)
- Distribution funnel visualization
- Quick action buttons (3 CTAs)
- Crop trends mini-chart
- Bottom navigation bar

**User Actions:**
- View statistics at a glance
- Tap "Paket Satın Al" → Navigate to tier selection
- Tap "Kod Gönder" → Navigate to code distribution
- Tap "Raporları Gör" → Navigate to analytics
- Pull down to refresh data
- Tap notification icon → View notifications

**API Calls:**
- `GET /api/v1/sponsorship/dashboard-summary` (on load)
- `GET /api/v1/sponsorship/package-statistics` (for funnel)

---

### 📦 Screen 2: Package Purchase Flow

#### 2.1: Tier Selection

**Purpose:** Compare and select subscription tier

**Layout:**
```
┌────────────────────────────────────────┐
│  ← Paket Satın Al                      │
├────────────────────────────────────────┤
│                                         │
│  Abonelik Seviyesi Seçin               │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │ 🥉 S Tier - Başlangıç             │  │ ← Card (Swipeable)
│  ├──────────────────────────────────┤  │
│  │ ✓ 30% Veri Görünürlüğü            │  │
│  │ ✓ Temel İstatistikler             │  │
│  │ ✗ Çiftçi İletişimi Yok            │  │
│  │ ✗ Smart Link Yok                  │  │
│  ├──────────────────────────────────┤  │
│  │ ₺100 / kod                         │  │
│  │ [Bu Tier'ı Seç]                   │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │ 🥈 M Tier - Orta                  │  │ ← POPULAR Badge
│  ├──────────────────────────────────┤  │
│  │ ✓ 60% Veri Görünürlüğü            │  │
│  │ ✓ Detaylı Analizler               │  │
│  │ ✗ Çiftçi İletişimi Yok            │  │
│  │ ✗ Smart Link Yok                  │  │
│  ├──────────────────────────────────┤  │
│  │ ₺150 / kod                         │  │
│  │ [Bu Tier'ı Seç]                   │  │
│  └──────────────────────────────────┘  │
│                                         │
│  < Swipe for L & XL tiers >           │
│                                         │
└────────────────────────────────────────┘
```

**Interaction:**
- Horizontal swipe to browse tiers
- Tap "Bu Tier'ı Seç" → Next screen (quantity)
- Tap "?" icon on features → Show feature explanation modal

#### 2.2: Quantity Selection

**Layout:**
```
┌────────────────────────────────────────┐
│  ← Miktar Seçin                        │
├────────────────────────────────────────┤
│                                         │
│  Seçilen Tier: M Tier                  │
│  Birim Fiyat: ₺150                     │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Kod Miktarı                      │  │
│  │                                   │  │
│  │       [－]  [  50  ]  [＋]        │  │ ← Stepper
│  │                                   │  │
│  │  Min: 10    Max: 10,000           │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  💡 ÖNERİLEN PAKETLER             │  │
│  ├──────────────────────────────────┤  │
│  │  [ 50 kod  - ₺7,500  ]            │  │ ← Quick Select
│  │  [100 kod  - ₺15,000 ]            │  │
│  │  [500 kod  - ₺75,000 ]            │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  📊 ÖZET                          │  │
│  ├──────────────────────────────────┤  │
│  │  Kod Sayısı:        50            │  │
│  │  Birim Fiyat:    ₺150             │  │
│  │  Ara Toplam:  ₺7,500              │  │
│  │  KDV (%20):   ₺1,500              │  │
│  │  ─────────────────────            │  │
│  │  TOPLAM:      ₺9,000              │  │
│  └──────────────────────────────────┘  │
│                                         │
│  [Ödemeye Geç]                         │ ← Primary CTA
│                                         │
└────────────────────────────────────────┘
```

**Interaction:**
- Tap +/- to adjust quantity
- Tap quick select button to set quantity
- Tap "Ödemeye Geç" → Payment screen

#### 2.3: Payment & Confirmation

**Layout:**
```
┌────────────────────────────────────────┐
│  ← Ödeme                               │
├────────────────────────────────────────┤
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Fatura Bilgileri                 │  │
│  ├──────────────────────────────────┤  │
│  │  Şirket Adı:  [              ]   │  │
│  │  Vergi No:    [              ]   │  │
│  │  Adres:       [              ]   │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Ödeme Yöntemi                    │  │
│  ├──────────────────────────────────┤  │
│  │  ○ Kredi Kartı                    │  │
│  │  ○ Banka Havalesi                 │  │
│  │  ○ Fatura ile Ödeme (Kurumsal)   │  │
│  └──────────────────────────────────┘  │
│                                         │
│  [Ödemeyi Tamamla - ₺9,000]           │
│                                         │
└────────────────────────────────────────┘
```

**Success Screen:**
```
┌────────────────────────────────────────┐
│                                         │
│          ✅                             │
│                                         │
│     Paket Başarıyla Satın Alındı!      │
│                                         │
│  50 adet sponsorluk kodu oluşturuldu   │
│                                         │
│  Sipariş No: #12345                     │
│  Ödeme: ₺9,000                          │
│  Durum: Tamamlandı ✓                    │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  KODLARINIZ HAZIR                 │  │
│  ├──────────────────────────────────┤  │
│  │  AGRI-2025-3456AB7C               │  │
│  │  AGRI-2025-7890CD1E               │  │
│  │  AGRI-2025-4321EF9G               │  │
│  │  ... 47 kod daha                  │  │
│  │                                   │  │
│  │  [Kodları Görüntüle]              │  │
│  │  [Hemen Dağıt]                    │  │
│  └──────────────────────────────────┘  │
│                                         │
│  [Ana Sayfaya Dön]                     │
│                                         │
└────────────────────────────────────────┘
```

**API Calls:**
- `POST /api/v1/sponsorship/purchase-package`

---

### 📤 Screen 3: Code Distribution Flow

#### 3.1: Code List View

**Purpose:** View all codes and their status

**Layout:**
```
┌────────────────────────────────────────┐
│  ← Kodlarım                     🔍     │
├────────────────────────────────────────┤
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Filtrele                         │  │
│  │  [Tümü ▾] [M Tier ▾] [Durumu ▾]  │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  ☑ AGRI-2025-3456AB7C            │  │ ← Selectable
│  │  M Tier • Kullanılmış ✓          │  │
│  │  📤 SMS • 08.10.2025              │  │
│  │  👤 Mehmet Yılmaz                 │  │
│  │  📊 25 analiz yapıldı             │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  ☐ AGRI-2025-7890CD1E            │  │
│  │  M Tier • Gönderildi 📨          │  │
│  │  📤 SMS • 08.10.2025              │  │
│  │  👤 Ayşe Demir                    │  │
│  │  ⏳ Bekliyor                      │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  ☐ AGRI-2025-4321EF9G            │  │
│  │  M Tier • Hazır ⚪                │  │
│  │  📭 Gönderilmedi                  │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ... 47 kod daha                       │
│                                         │
├────────────────────────────────────────┤
│  [Seçilenleri Gönder (0)]             │ ← Sticky Footer
└────────────────────────────────────────┘
```

**Status Badges:**
- ⚪ Hazır (white) - Not sent
- 📨 Gönderildi (blue) - Sent, not redeemed
- ✅ Kullanılmış (green) - Redeemed
- ⏰ Süresi Doldu (red) - Expired

**User Actions:**
- Tap checkbox to select multiple codes
- Tap "Seçilenleri Gönder" → Distribution screen
- Tap individual code → Code details screen
- Pull to refresh
- Search codes
- Filter by tier/status

#### 3.2: Recipient Entry

**Purpose:** Add recipients for code distribution

**Layout:**
```
┌────────────────────────────────────────┐
│  ← Alıcı Ekle              [Bitti]     │
├────────────────────────────────────────┤
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Alıcı Ekleme Yöntemi             │  │
│  ├──────────────────────────────────┤  │
│  │  [📝 Manuel Giriş]                │  │
│  │  [📇 Kişilerden Seç]              │  │
│  │  [📊 Excel/CSV Yükle]             │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Seçilen Kodlar: 3                │  │
│  ├──────────────────────────────────┤  │
│  │  AGRI-2025-3456AB7C               │  │
│  │  AGRI-2025-7890CD1E               │  │
│  │  AGRI-2025-4321EF9G               │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Alıcı 1                          │  │
│  │  ───────────────────              │  │
│  │  Kod: AGRI-2025-3456AB7C         │  │
│  │  Ad Soyad:  [Mehmet Yılmaz]      │  │
│  │  Telefon:   [+90 555 123 4567]   │  │
│  │  [Alıcıyı Kaldır]                 │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Alıcı 2                          │  │
│  │  ───────────────────              │  │
│  │  Kod: AGRI-2025-7890CD1E         │  │
│  │  Ad Soyad:  [Ayşe Demir]         │  │
│  │  Telefon:   [+90 555 987 6543]   │  │
│  │  [Alıcıyı Kaldır]                 │  │
│  └──────────────────────────────────┘  │
│                                         │
│  [+ Alıcı Ekle]                        │
│                                         │
│  [İleri]                               │
│                                         │
└────────────────────────────────────────┘
```

**Validation:**
- Phone: Turkish format (+90 5XX XXX XX XX)
- Name: Required, min 2 chars
- Code-recipient mapping: 1-to-1

#### 3.3: Message Customization

**Layout:**
```
┌────────────────────────────────────────┐
│  ← Mesaj Özelleştir          [İleri]   │
├────────────────────────────────────────┤
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Gönderim Kanalı                  │  │
│  ├──────────────────────────────────┤  │
│  │  ● SMS (Önerilen)                 │  │
│  │  ○ WhatsApp                       │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Özel Mesaj (Opsiyonel)          │  │
│  ├──────────────────────────────────┤  │
│  │  [AgriTech Solutions             │  │
│  │   sponsorluğunda premium         │  │
│  │   üyelik kazandınız!]            │  │
│  │                                   │  │
│  │  150/300 karakter                 │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  📱 MESAJ ÖNİZLEMESİ              │  │
│  ├──────────────────────────────────┤  │
│  │  Merhaba Mehmet Yılmaz,          │  │
│  │                                   │  │
│  │  AgriTech Solutions               │  │
│  │  sponsorluğunda premium           │  │
│  │  üyelik kazandınız!               │  │
│  │                                   │  │
│  │  Kodunuz: AGRI-2025-3456AB7C     │  │
│  │                                   │  │
│  │  Hemen kullanmak için:            │  │
│  │  https://ziraai.com/redeem/...   │  │
│  │                                   │  │
│  │  365 gün geçerlidir.              │  │
│  └──────────────────────────────────┘  │
│                                         │
│  [Gönder (3 alıcı)]                    │
│                                         │
└────────────────────────────────────────┘
```

**Preview:**
- Real-time message preview
- Character counter
- Personalization tokens: {name}, {code}, {link}

#### 3.4: Send Confirmation & Results

**Layout:**
```
┌────────────────────────────────────────┐
│  Gönderiliyor...                       │
├────────────────────────────────────────┤
│                                         │
│  ┌──────────────────────────────────┐  │
│  │       🚀                          │  │
│  │                                   │  │
│  │  Kodlar gönderiliyor...           │  │
│  │                                   │  │
│  │  ████████████░░░░░░  65%          │  │ ← Progress
│  │                                   │  │
│  │  2 / 3 başarılı                   │  │
│  └──────────────────────────────────┘  │
│                                         │
└────────────────────────────────────────┘

           ↓ (After completion)

┌────────────────────────────────────────┐
│  Gönderim Tamamlandı!                  │
├────────────────────────────────────────┤
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  📊 SONUÇLAR                      │  │
│  ├──────────────────────────────────┤  │
│  │  Toplam:      3                   │  │
│  │  Başarılı:    2  ✅               │  │
│  │  Başarısız:   1  ❌               │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  ✅ Mehmet Yılmaz                 │  │
│  │     +90 555 123 4567              │  │
│  │     AGRI-2025-3456AB7C            │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  ✅ Ayşe Demir                    │  │
│  │     +90 555 987 6543              │  │
│  │     AGRI-2025-7890CD1E            │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  ❌ Ali Kaya                      │  │
│  │     +90 555 111 2222              │  │
│  │     AGRI-2025-4321EF9G            │  │
│  │     Hata: Geçersiz telefon        │  │
│  │     [Tekrar Dene]                 │  │
│  └──────────────────────────────────┘  │
│                                         │
│  [Tamamlandı]                          │
│                                         │
└────────────────────────────────────────┘
```

**API Call:**
- `POST /api/v1/sponsorship/send-link`

---

### 📊 Screen 4: Analytics & Reports

#### 4.1: Statistics Dashboard

**Layout:**
```
┌────────────────────────────────────────┐
│  ← İstatistikler            📅 [Filtre] │
├────────────────────────────────────────┤
│                                         │
│  [Paket İstatistikleri] [Kod Analizi]  │ ← Tab Bar
│  [Ürün Trendleri]                      │
│  ─────────────────────                 │
│                                         │
│  📦 PAKET İSTATİSTİKLERİ               │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Genel Bakış                      │  │
│  ├──────────────────────────────────┤  │
│  │  Satın Alınan:    100  ███████   │  │
│  │  Dağıtılan:        50  ████      │  │
│  │  Kullanılan:       30  ██        │  │
│  │                                   │  │
│  │  Dağıtım Oranı:    %50            │  │
│  │  Kullanım Oranı:   %60            │  │
│  │  Genel Başarı:     %30            │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Tier Bazlı Dağılım               │  │
│  ├──────────────────────────────────┤  │
│  │  M Tier                           │  │
│  │  ████████ 50 kod (%60 kullanım)  │  │
│  │                                   │  │
│  │  L Tier                           │  │
│  │  ████████████ 30 kod (%70 kul.)  │  │
│  │                                   │  │
│  │  XL Tier                          │  │
│  │  ████ 20 kod (%80 kullanım)      │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Kanal Performansı                │  │
│  ├──────────────────────────────────┤  │
│  │  SMS:       40 kod (%95 teslimat)│  │
│  │  WhatsApp:  10 kod (%85 teslimat)│  │
│  └──────────────────────────────────┘  │
│                                         │
└────────────────────────────────────────┘
```

**Interaction:**
- Swipe tabs to change view
- Tap "Filtre" → Date range selector
- Tap on tier/channel → Detailed breakdown

#### 4.2: Code-Level Analysis

**Layout:**
```
┌────────────────────────────────────────┐
│  ← Kod Analizi              [Sırala ▾] │
├────────────────────────────────────────┤
│                                         │
│  En Çok Analiz Üreten Kodlar           │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  1. AGRI-2025-3456AB7C           │  │
│  ├──────────────────────────────────┤  │
│  │  👤 Mehmet Yılmaz (L Tier)       │  │
│  │  📍 Adana                         │  │
│  │  📊 45 analiz yapıldı             │  │
│  │  📅 Son analiz: 2 saat önce       │  │
│  │                                   │  │
│  │  🌾 Top Crops:                    │  │
│  │  • Buğday (20)  • Mısır (15)     │  │
│  │                                   │  │
│  │  [Analizleri Görüntüle →]        │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  2. AGRI-2025-7890CD1E           │  │
│  ├──────────────────────────────────┤  │
│  │  👤 Ayşe Demir (M Tier)          │  │
│  │  📍 Kısıtlı bilgi                 │  │
│  │  📊 30 analiz yapıldı             │  │
│  │  📅 Son analiz: 1 gün önce        │  │
│  │                                   │  │
│  │  [Analizleri Görüntüle →]        │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ... 28 kod daha                       │
│                                         │
└────────────────────────────────────────┘
```

**Privacy Indicator:**
- L/XL Tier: Full farmer details shown
- M Tier: Limited info ("Kısıtlı bilgi")
- S Tier: Minimal info ("Anonim")

#### 4.3: Analysis List (Drill-Down)

**Layout:**
```
┌────────────────────────────────────────┐
│  ← AGRI-2025-3456AB7C                  │
├────────────────────────────────────────┤
│                                         │
│  Mehmet Yılmaz - L Tier                │
│  📍 Adana, Ceyhan                      │
│  📊 45 Analiz                           │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Analiz #789                      │  │
│  │  ────────────────────             │  │
│  │  🌾 Ürün: Buğday                  │  │
│  │  🦠 Hastalık: Wheat Rust          │  │
│  │  ⚠️ Şiddet: Orta                  │  │
│  │  📅 Tarih: 10.10.2025 14:30       │  │
│  │  📍 Konum: Adana                  │  │
│  │  🏷️ Sponsorlu: Evet ✓             │  │
│  │                                   │  │
│  │  [Detayları Gör →]                │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Analiz #788                      │  │
│  │  ────────────────────             │  │
│  │  🌾 Ürün: Domates                 │  │
│  │  🦠 Hastalık: Late Blight         │  │
│  │  ⚠️ Şiddet: Yüksek                │  │
│  │  📅 Tarih: 09.10.2025 09:15       │  │
│  │                                   │  │
│  │  [Detayları Gör →]                │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ... 43 analiz daha                    │
│                                         │
└────────────────────────────────────────┘
```

**User Actions:**
- Scroll to view all analyses
- Tap "Detayları Gör" → Full analysis detail (API call)
- Pull to refresh

**API Calls:**
- `GET /api/v1/sponsorship/code-analysis-statistics?includeAnalysisDetails=true`
- `GET /api/v1/sponsorship/analysis/{id}` (on tap)

#### 4.4: Crop & Disease Insights

**Layout:**
```
┌────────────────────────────────────────┐
│  ← Ürün Trendleri          📊 [Rapor]  │
├────────────────────────────────────────┤
│                                         │
│  En Çok Analiz Edilen Ürünler          │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  🌾 Buğday                        │  │
│  │  ████████████████████ 45% (340)  │  │
│  │  68 çiftçi • 12 il                │  │
│  │  [Detaylar →]                     │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  🍅 Domates                       │  │
│  │  ██████████████ 30% (225)        │  │
│  │  45 çiftçi • 8 il                 │  │
│  │  [Detaylar →]                     │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  🌽 Mısır                         │  │
│  │  ████████ 15% (113)              │  │
│  │  28 çiftçi • 6 il                 │  │
│  │  [Detaylar →]                     │  │
│  └──────────────────────────────────┘  │
│                                         │
│  En Yaygın Hastalıklar                 │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  1. Wheat Rust (Pas)              │  │
│  │     • 120 olay                    │  │
│  │     • Buğday, arpa                │  │
│  │     • Adana, Konya, Ankara        │  │
│  │     [Detaylar →]                  │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ... devamı                            │
│                                         │
└────────────────────────────────────────┘
```

**Insights Value:**
- Identify market opportunities
- Product development insights
- Regional targeting for campaigns

---

### 👥 Screen 5: Sponsored Farmers

**Layout:**
```
┌────────────────────────────────────────┐
│  ← Sponsorlu Çiftçiler      🔍 [Filtre] │
├────────────────────────────────────────┤
│                                         │
│  30 aktif çiftçi                       │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  👤 Mehmet Yılmaz                 │  │ ← L Tier (Full visibility)
│  │  L Tier • AGRI-2025-3456AB7C     │  │
│  ├──────────────────────────────────┤  │
│  │  📧 mehmet@example.com            │  │
│  │  📱 +90 555 123 4567              │  │
│  │  📍 Adana, Ceyhan                 │  │
│  │  📊 45 analiz                     │  │
│  │  📅 Son aktif: 2 saat önce        │  │
│  │                                   │  │
│  │  [💬 Mesaj Gönder] [📊 Analizler] │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  👤 Anonim Çiftçi                 │  │ ← M Tier (Limited visibility)
│  │  M Tier • AGRI-2025-7890CD1E     │  │
│  ├──────────────────────────────────┤  │
│  │  📍 Adana                         │  │
│  │  📊 30 analiz                     │  │
│  │  📅 Son aktif: 1 gün önce         │  │
│  │                                   │  │
│  │  [📊 Analizler]                   │  │
│  │  💬 Mesaj özelliği yok (M Tier)  │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  👤 Anonim                        │  │ ← S Tier (Minimal visibility)
│  │  S Tier • AGRI-2025-4321EF9G     │  │
│  ├──────────────────────────────────┤  │
│  │  📍 Kısıtlı                       │  │
│  │  📊 12 analiz                     │  │
│  │  📅 Son aktif: 3 gün önce         │  │
│  │                                   │  │
│  │  [📊 Analizler]                   │  │
│  └──────────────────────────────────┘  │
│                                         │
└────────────────────────────────────────┘
```

**Privacy Tiers:**
- **L/XL**: Full name, email, phone, exact location
- **M**: "Anonim", city only, no contact
- **S**: "Anonim", "Kısıtlı" location, no contact

**API Call:**
- `GET /api/v1/sponsorship/farmers`

---

### 💬 Screen 6: Messaging (L/XL Only)

#### 6.1: Message Composer

**Layout:**
```
┌────────────────────────────────────────┐
│  ← Mesaj Gönder                 [Gönder]│
├────────────────────────────────────────┤
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Alıcılar (3 seçili)              │  │
│  ├──────────────────────────────────┤  │
│  │  ✓ Mehmet Yılmaz                  │  │
│  │  ✓ Ali Kaya                       │  │
│  │  ✓ Zeynep Arslan                  │  │
│  │                                   │  │
│  │  [+ Alıcı Ekle]                   │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Mesaj Tipi                       │  │
│  ├──────────────────────────────────┤  │
│  │  ○ Bilgilendirme                  │  │
│  │  ● Ürün Önerisi                   │  │
│  │  ○ Promosyon                      │  │
│  │  ○ Destek                         │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Mesaj Şablonları                 │  │
│  ├──────────────────────────────────┤  │
│  │  [Wheat Rust için Öneri]          │  │
│  │  [Yeni Ürün Tanıtımı]             │  │
│  │  [Mevsimsel Kampanya]             │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Mesajınız                        │  │
│  │                                   │  │
│  │  [Merhaba {name},                │  │
│  │                                   │  │
│  │  Buğday pasına karşı yeni         │  │
│  │  geliştirdiğimiz AgriGuard Pro    │  │
│  │  ürünümüzü incelemenizi öneririz. │  │
│  │                                   │  │
│  │  %20 indirim kodu: ZIRAAI2025]   │  │
│  │                                   │  │
│  │  450/500 karakter                 │  │
│  └──────────────────────────────────┘  │
│                                         │
│  [İleri]                               │
│                                         │
└────────────────────────────────────────┘
```

**Features:**
- Multi-recipient messaging
- Message templates
- Personalization tokens
- Character counter
- Message type categorization

#### 6.2: Targeted Campaigns

**Layout:**
```
┌────────────────────────────────────────┐
│  ← Hedefli Kampanya              [İleri]│
├────────────────────────────────────────┤
│                                         │
│  Hedef Kitle Seçin                     │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Ürün Bazlı                       │  │
│  ├──────────────────────────────────┤  │
│  │  ☑ Buğday yetiştiren (68 kişi)   │  │
│  │  ☐ Domates yetiştiren (45 kişi)  │  │
│  │  ☐ Mısır yetiştiren (28 kişi)    │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Hastalık Bazlı                   │  │
│  ├──────────────────────────────────┤  │
│  │  ☑ Wheat Rust tespit eden (42)   │  │
│  │  ☐ Late Blight tespit eden (18)  │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Lokasyon Bazlı                   │  │
│  ├──────────────────────────────────┤  │
│  │  ☐ Adana (35 kişi)                │  │
│  │  ☐ Konya (22 kişi)                │  │
│  │  ☐ Ankara (18 kişi)               │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Aktivite Bazlı                   │  │
│  ├──────────────────────────────────┤  │
│  │  ☐ Son 7 günde aktif (55 kişi)   │  │
│  │  ☐ Son 30 günde aktif (78 kişi)  │  │
│  │  ☐ Yüksek aktivite (20+ analiz)  │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  📊 Hedef Kitle Özeti             │  │
│  │  42 kişi bu kriterlere uyuyor     │  │
│  └──────────────────────────────────┘  │
│                                         │
│  [Mesaj Oluştur]                       │
│                                         │
└────────────────────────────────────────┘
```

**Targeting Options:**
- Crop type filter
- Disease detection filter
- Location filter
- Activity level filter
- Combination filters (AND/OR logic)

**API Call:**
- `POST /api/v1/sponsorship/messages/send`

---

### 🔗 Screen 7: Smart Links (XL Only)

#### 7.1: Smart Link Dashboard

**Layout:**
```
┌────────────────────────────────────────┐
│  ← Smart Links              [+ Yeni]    │
├────────────────────────────────────────┤
│                                         │
│  🤖 AI-Powered Product Recommendations  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Toplam Link: 15                  │  │
│  │  Aktif Kampanya: 3                │  │
│  │  Toplam Tıklama: 245              │  │
│  │  Dönüşüm Oranı: %12               │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  🔗 Wheat Rust Campaign           │  │
│  │  ────────────────────             │  │
│  │  🌾 Hedef: Buğday • Wheat Rust    │  │
│  │  📅 Oluşturma: 05.10.2025         │  │
│  │  📊 125 görüntüleme               │  │
│  │  🖱️ 18 tıklama (%14.4)            │  │
│  │  ✅ 3 dönüşüm                      │  │
│  │                                   │  │
│  │  [Düzenle] [Performans]           │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  🔗 Tomato Disease Package        │  │
│  │  ────────────────────             │  │
│  │  🍅 Hedef: Domates • Late Blight  │  │
│  │  📅 Oluşturma: 08.10.2025         │  │
│  │  📊 80 görüntüleme                │  │
│  │  🖱️ 12 tıklama (%15)              │  │
│  │  ✅ 2 dönüşüm                      │  │
│  │                                   │  │
│  │  [Düzenle] [Performans]           │  │
│  └──────────────────────────────────┘  │
│                                         │
└────────────────────────────────────────┘
```

#### 7.2: Create Smart Link

**Layout:**
```
┌────────────────────────────────────────┐
│  ← Yeni Smart Link          [Oluştur]  │
├────────────────────────────────────────┤
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Kampanya Bilgileri               │  │
│  ├──────────────────────────────────┤  │
│  │  Ad:          [Wheat Protection]  │  │
│  │  Açıklama:    [Buğday hastalık   │  │
│  │               koruma paketi]      │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Hedef Tanımlama                  │  │
│  ├──────────────────────────────────┤  │
│  │  Ürün:      [Buğday ▾]            │  │
│  │  Hastalık:  [Wheat Rust ▾]        │  │
│  │  Bölge:     [Tüm Türkiye ▾]       │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  Ürün Eşleştirme                  │  │
│  ├──────────────────────────────────┤  │
│  │  ☑ AgriGuard Pro                  │  │
│  │     Fungusit • ₺450/L             │  │
│  │                                   │  │
│  │  ☑ WheatShield Plus               │  │
│  │     Koruyucu ilaç • ₺280/L        │  │
│  │                                   │  │
│  │  ☐ NutriFert Wheat                │  │
│  │     Besin takviyesi • ₺120/kg     │  │
│  │                                   │  │
│  │  [+ Ürün Ekle]                    │  │
│  └──────────────────────────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │  🤖 AI Önizleme                   │  │
│  │                                   │  │
│  │  Çiftçi analizi: Buğday + Rust   │  │
│  │  ↓                                │  │
│  │  Önerilen:                        │  │
│  │  1. AgriGuard Pro (öncelikli)    │  │
│  │  2. WheatShield Plus (alternatif)│  │
│  └──────────────────────────────────┘  │
│                                         │
│  [Kaydet ve Aktifte Et]                │
│                                         │
└────────────────────────────────────────┘
```

**API Calls:**
- `POST /api/v1/sponsorship/smart-links/create`
- `GET /api/v1/sponsorship/smart-links/performance`

---

## 📊 Feature Matrix by Tier

### Complete Feature Comparison

| Feature | S Tier | M Tier | L Tier | XL Tier |
|---------|--------|--------|--------|---------|
| **Data Access** |
| Farmer Name | ❌ Anonymous | ❌ Anonymous | ✅ Full | ✅ Full |
| Contact Info | ❌ | ❌ | ✅ Email/Phone | ✅ Email/Phone |
| Location | ⚠️ Limited | ⚠️ City only | ✅ Full address | ✅ Full address |
| Analysis Details | ✅ Basic | ✅ Detailed | ✅ Complete | ✅ Complete |
| Data Visibility | 30% | 60% | 100% | 100% |
| **Analytics** |
| Package Statistics | ✅ | ✅ | ✅ | ✅ |
| Code Analytics | ✅ | ✅ | ✅ | ✅ |
| Crop Insights | ⚠️ Limited | ✅ | ✅ | ✅ |
| Disease Trends | ⚠️ Limited | ✅ | ✅ | ✅ |
| Geographic Analysis | ❌ | ⚠️ Basic | ✅ | ✅ |
| **Communication** |
| Code Distribution | ✅ | ✅ | ✅ | ✅ |
| In-App Messaging | ❌ | ❌ | ✅ | ✅ |
| Targeted Campaigns | ❌ | ❌ | ✅ | ✅ |
| Message Templates | ❌ | ❌ | ✅ | ✅ |
| **Advanced Features** |
| Smart Links | ❌ | ❌ | ❌ | ✅ |
| AI Recommendations | ❌ | ❌ | ❌ | ✅ |
| Product Catalog | ❌ | ❌ | ❌ | ✅ |
| Conversion Tracking | ❌ | ❌ | ❌ | ✅ |
| **Support** |
| Email Support | ✅ | ✅ | ✅ | ✅ |
| Priority Support | ❌ | ❌ | ✅ | ✅ |
| Dedicated Manager | ❌ | ❌ | ❌ | ✅ |

---

## 🎨 Wireframe Requirements

### Design System Elements

#### Color Palette

```
Primary Colors:
• Brand Green:    #2ECC71 (CTAs, success states)
• Dark Green:     #27AE60 (headers, emphasis)
• Light Green:    #A8E6CF (backgrounds, highlights)

Secondary Colors:
• Sky Blue:       #3498DB (info, links)
• Warm Orange:    #E67E22 (warnings, alerts)
• Red:            #E74C3C (errors, critical)

Neutral Colors:
• Dark Gray:      #2C3E50 (text, headers)
• Medium Gray:    #7F8C8D (secondary text)
• Light Gray:     #ECF0F1 (backgrounds, borders)
• White:          #FFFFFF (cards, surfaces)

Tier Colors:
• S Tier:         #95A5A6 (Silver)
• M Tier:         #F39C12 (Gold)
• L Tier:         #9B59B6 (Purple)
• XL Tier:        #E74C3C (Red/Premium)
```

#### Typography

```
Headings:
• H1: 24px, Bold, Dark Gray
• H2: 20px, Bold, Dark Gray
• H3: 18px, SemiBold, Dark Gray

Body:
• Body 1: 16px, Regular, Dark Gray
• Body 2: 14px, Regular, Medium Gray
• Caption: 12px, Regular, Medium Gray

Special:
• Button: 16px, SemiBold, White
• Label: 14px, Medium, Dark Gray
```

#### Component Library

**Buttons:**
```
Primary: Green background, white text, rounded corners (8px)
Secondary: White background, green border, green text
Tertiary: No background, green text, underline on hover
Disabled: Light gray background, medium gray text
```

**Cards:**
```
Background: White
Border: 1px solid Light Gray
Border Radius: 12px
Shadow: 0 2px 8px rgba(0,0,0,0.1)
Padding: 16px
```

**Status Badges:**
```
Success: Green background, white text
Warning: Orange background, white text
Error: Red background, white text
Info: Blue background, white text
Neutral: Gray background, dark text
```

**Input Fields:**
```
Border: 1px solid Light Gray
Border Radius: 8px
Padding: 12px
Focus: 2px solid Brand Green
Error: 2px solid Red
```

---

## 🔌 API Integration Reference

### Authentication

**Header:**
```
Authorization: Bearer {jwt_token}
```

**Getting User ID:**
```dart
final userId = await authService.getCurrentUserId();
```

### Core Endpoints

#### 1. Dashboard Summary
```
GET /api/v1/sponsorship/dashboard-summary
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalCodesPurchased": 100,
    "totalCodesDistributed": 50,
    "totalCodesRedeemed": 30,
    "activeFarmers": 30,
    "totalAnalyses": 750
  }
}
```

#### 2. Purchase Package
```
POST /api/v1/sponsorship/purchase-package
```

**Request:**
```json
{
  "subscriptionTierId": 3,
  "quantity": 50,
  "totalAmount": 7500.00,
  "paymentMethod": "CreditCard",
  "paymentReference": "PAY-12345",
  "companyName": "AgriTech Solutions",
  "invoiceAddress": "İstanbul",
  "taxNumber": "1234567890"
}
```

**Response:**
```json
{
  "success": true,
  "message": "50 sponsorship codes generated successfully",
  "data": {
    "id": 15,
    "codesGenerated": 50,
    "generatedCodes": [
      {
        "id": 501,
        "code": "AGRI-2025-3456AB7C",
        "tierName": "L",
        "expiryDate": "2026-10-10T10:30:00Z"
      }
    ]
  }
}
```

#### 3. Send Sponsorship Links
```
POST /api/v1/sponsorship/send-link
```

**Request:**
```json
{
  "channel": "SMS",
  "customMessage": "AgriTech sponsorluğunda premium üyelik!",
  "recipients": [
    {
      "code": "AGRI-2025-3456AB7C",
      "phone": "+905551234567",
      "name": "Mehmet Yılmaz"
    }
  ]
}
```

**Response:**
```json
{
  "success": true,
  "message": "📱 48 link başarıyla gönderildi via SMS",
  "data": {
    "totalSent": 50,
    "successCount": 48,
    "failureCount": 2,
    "results": [
      {
        "code": "AGRI-2025-3456AB7C",
        "phone": "+905551234567",
        "success": true,
        "deliveryStatus": "Sent"
      }
    ]
  }
}
```

#### 4. Get Sponsorship Codes
```
GET /api/v1/sponsorship/codes?onlyUnused=false
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 501,
      "code": "AGRI-2025-3456AB7C",
      "tierName": "L",
      "isUsed": true,
      "linkSentDate": "2025-10-08T10:00:00Z",
      "linkSentVia": "SMS",
      "recipientPhone": "+905551234567",
      "usedDate": "2025-10-10T14:00:00Z",
      "usedByUserName": "Mehmet Yılmaz"
    }
  ]
}
```

#### 5. Package Distribution Statistics
```
GET /api/v1/sponsorship/package-statistics
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalCodesPurchased": 100,
    "totalCodesDistributed": 50,
    "totalCodesRedeemed": 30,
    "distributionRate": 50.0,
    "redemptionRate": 60.0,
    "overallSuccessRate": 30.0,
    "packageBreakdowns": [...],
    "tierBreakdowns": [...],
    "channelBreakdowns": [...]
  }
}
```

#### 6. Code Analysis Statistics
```
GET /api/v1/sponsorship/code-analysis-statistics?includeAnalysisDetails=true&topCodesCount=10
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalRedeemedCodes": 30,
    "totalAnalysesPerformed": 750,
    "averageAnalysesPerCode": 25.0,
    "codeBreakdowns": [
      {
        "code": "AGRI-2025-3456AB7C",
        "tierName": "L",
        "farmerName": "Mehmet Yılmaz",
        "farmerEmail": "mehmet@example.com",
        "farmerPhone": "+905551234567",
        "location": "Adana, Ceyhan",
        "totalAnalyses": 45,
        "analyses": [
          {
            "analysisId": 789,
            "analysisDate": "2025-10-10T14:00:00Z",
            "cropType": "Buğday",
            "disease": "Wheat Rust",
            "severity": "Orta",
            "location": "Adana",
            "analysisDetailsUrl": "https://ziraai.com/api/v1/sponsorship/analysis/789"
          }
        ]
      }
    ],
    "cropTypeDistribution": [...],
    "diseaseDistribution": [...]
  }
}
```

#### 7. Get Sponsored Farmers
```
GET /api/v1/sponsorship/farmers
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "farmerId": 42,
      "farmerName": "Mehmet Yılmaz",
      "email": "mehmet@example.com",
      "phone": "+905551234567",
      "location": "Adana, Ceyhan",
      "code": "AGRI-2025-3456AB7C",
      "tierName": "L",
      "subscriptionStatus": "Active",
      "totalAnalyses": 45,
      "lastAnalysisDate": "2025-10-10T14:00:00Z"
    }
  ]
}
```

---

## 📈 Analytics & Tracking

### Events to Track

**User Actions:**
```
• sponsor_registered
• profile_created
• package_purchased (tier, quantity, amount)
• codes_distributed (channel, count, success_rate)
• farmer_viewed (tier)
• analysis_viewed
• message_sent (tier_requirement, recipient_count)
• smart_link_created (XL_only)
```

**Engagement Metrics:**
```
• time_on_dashboard
• codes_per_session
• distribution_frequency
• analytics_view_frequency
• message_open_rate (if available)
• smart_link_click_rate (XL_only)
```

**Business Metrics:**
```
• revenue_per_sponsor
• tier_distribution (S/M/L/XL counts)
• code_redemption_rate
• farmer_engagement_rate
• roi_per_campaign
```

### Firebase Analytics Integration

```dart
// Example implementation
await FirebaseAnalytics.instance.logEvent(
  name: 'package_purchased',
  parameters: {
    'tier': 'L',
    'quantity': 50,
    'amount': 7500.00,
    'payment_method': 'CreditCard',
  },
);
```

---

## 🎯 User Experience Principles

### 1. Progressive Disclosure
- Show essential info first
- Hide complexity behind expandable sections
- Use tabs for different data views
- Provide drill-down capability

### 2. Feedback & Confirmation
- Loading states for all async operations
- Success/error messages with clear actions
- Real-time progress for bulk operations
- Confirmation dialogs for critical actions

### 3. Data Visualization
- Use charts for trend analysis
- Color-coded status badges
- Progress bars for completion rates
- Comparative views (before/after, tier comparison)

### 4. Mobile-First Design
- Thumb-friendly touch targets (min 44x44pt)
- Bottom-sheet modals for forms
- Swipe gestures for navigation
- Pull-to-refresh for data updates

### 5. Accessibility
- Minimum contrast ratio 4.5:1
- Text size adjustable
- Screen reader support
- Alternative text for images

---

## 🚀 Implementation Priorities

### Phase 1: MVP (Must-Have)
1. ✅ Dashboard with key metrics
2. ✅ Package purchase flow
3. ✅ Code distribution (SMS only)
4. ✅ Code list view
5. ✅ Package statistics

### Phase 2: Core Features
6. ✅ Code analysis statistics
7. ✅ Sponsored farmers list
8. ✅ Crop & disease insights
9. ⏳ WhatsApp distribution
10. ⏳ CSV import for bulk distribution

### Phase 3: Premium Features (L/XL)
11. ⏳ In-app messaging
12. ⏳ Targeted campaigns
13. ⏳ Message templates

### Phase 4: Advanced (XL Only)
14. ⏳ Smart Links dashboard
15. ⏳ AI recommendations
16. ⏳ Product catalog management
17. ⏳ Conversion tracking

---

## 📝 Design Checklist

### For Each Screen:
- [ ] Wireframe created with annotations
- [ ] User flow documented
- [ ] API endpoints identified
- [ ] Loading states designed
- [ ] Error states designed
- [ ] Empty states designed
- [ ] Success confirmations designed
- [ ] Tier restrictions shown
- [ ] Privacy rules applied
- [ ] Mobile & tablet layouts
- [ ] Dark mode variant (optional)
- [ ] Accessibility audit passed

### For Overall App:
- [ ] Design system created
- [ ] Component library built
- [ ] Icon set selected
- [ ] Navigation structure finalized
- [ ] Animation specifications
- [ ] Prototype created
- [ ] User testing conducted
- [ ] Developer handoff package

---

## 📚 Additional Resources

### Reference Documents:
- [SPONSOR_PERSONA_COMPLETE_JOURNEY_REPORT.md](./SPONSOR_PERSONA_COMPLETE_JOURNEY_REPORT.md) - Full persona analysis
- [SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md](./SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md) - Technical specs
- [MOBILE_SPONSORSHIP_INTEGRATION_GUIDE.md](./MOBILE_SPONSORSHIP_INTEGRATION_GUIDE.md) - Flutter integration

### Design Tools:
- Figma for wireframes & prototypes
- Miro for user journey mapping
- Principle/Flinto for animations

### API Documentation:
- Swagger UI: `https://ziraai.com/swagger`
- Postman Collection: `ZiraAI_Complete_API_Collection_v6.1.json`

---

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.0 | 2025-10-10 | Complete UX specification with flows, wireframes, and API references |
| 1.0 | 2025-10-09 | Initial persona and journey documentation |

---

## 👥 Contact & Feedback

**For Design Questions:**
- Design Team Lead: [Contact Info]

**For Technical Questions:**
- Backend Team: ziraai-backend@example.com
- API Documentation: https://ziraai.com/swagger

**For Business Logic:**
- Product Manager: [Contact Info]

---

**Document Status:** ✅ Ready for Design Phase
**Next Steps:** Create high-fidelity mockups in Figma
