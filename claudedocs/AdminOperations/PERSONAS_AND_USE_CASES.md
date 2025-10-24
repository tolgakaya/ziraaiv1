# Admin Operations - Personas & Use Cases

**Created:** 2025-10-23  
**Purpose:** Frontend Development Planning  
**Status:** Ready for React Implementation

---

## Executive Summary

Bu doküman, Admin Operations API'sini kullanacak React frontend uygulaması için 5 ana persona ve kullanım senaryolarını tanımlar. Her persona'nın ihtiyaç duyduğu endpoint'ler, UI bileşenleri ve iş akışları detaylandırılmıştır.

---

## Persona Grupları

### 🎯 5 Ana Persona

1. **Platform Yöneticisi (Super Admin)** - Tüm sistemi yöneten
2. **Veri Analisti (Data Analyst)** - Raporlama ve analiz yapan
3. **Kullanıcı Destek Uzmanı (User Support)** - Kullanıcı sorunlarını çözen
4. **Sponsorluk Koordinatörü (Sponsorship Manager)** - Sponsorları yöneten
5. **Sistem Denetçisi (Auditor)** - Tüm işlemleri denetleyen

---

## 1. Platform Yöneticisi (Super Admin)

### Profil
- **Ad:** Ayşe Demir
- **Yaş:** 35
- **Rol:** Platform Yöneticisi
- **Deneyim:** 8 yıl tarım teknolojileri
- **Teknik Seviye:** İleri seviye

### Sorumluluklar
- ✅ Tüm platformun genel sağlığını izleme
- ✅ Kullanıcı hesaplarını aktif/pasif etme
- ✅ Sponsorları onaylama ve yönetme
- ✅ Kritik kararlar alma (para iadesi, hesap silme)
- ✅ Sistem genelinde değişiklik yapma

### Kullandığı Endpoint'ler

#### Analytics & Dashboard
```
GET /api/admin/analytics/dashboard-overview
GET /api/admin/analytics/user-statistics
GET /api/admin/analytics/subscription-statistics
GET /api/admin/sponsorship/statistics
```

#### User Management
```
GET /api/admin/users
GET /api/admin/users/{id}
GET /api/admin/users/search
POST /api/admin/users/deactivate/{id}
POST /api/admin/users/reactivate/{id}
POST /api/admin/users/bulk/deactivate
```

#### Sponsorship Management
```
GET /api/admin/sponsorship/purchases
POST /api/admin/sponsorship/purchases/{id}/approve
POST /api/admin/sponsorship/purchases/{id}/refund
```

#### Activity Monitoring
```
GET /api/admin/analytics/activity-logs
GET /api/admin/audit/all
```

### Kullanım Senaryoları

#### Senaryo 1.1: Günlük Platform Kontrolü
**Sıklık:** Günde 2-3 kez (sabah, öğle, akşam)

**İş Akışı:**
1. Login → Dashboard'a geliş
2. Dashboard overview'da key metrics'leri kontrol
   - Toplam kullanıcı sayısı
   - Aktif abonelikler
   - Günlük yeni kayıtlar
   - Son 24 saatteki activity
3. Anomali varsa detay sayfalarına dalış
4. Gerekirse hızlı aksiyon (kullanıcı deaktive etme, vb.)

**Gerekli UI Bileşenleri:**
- 📊 Dashboard kartları (KPI widgets)
- 📈 Trend grafikleri (son 7 gün, 30 gün)
- 🚨 Alert/bildirim paneli
- 🔍 Quick search bar (global)

---

#### Senaryo 1.2: Problem Kullanıcı Yönetimi
**Sıklık:** Haftada 2-3 kez

**İş Akışı:**
1. Şikayet/rapor alır (email, destek talebi)
2. Kullanıcıyı arar (search by name/email/phone)
3. Kullanıcı detaylarını inceler
   - Hesap geçmişi
   - Son aktiviteleri
   - Abonelik durumu
   - Sponsorluk ilişkileri
4. Karar verir:
   - Geçici deaktive (reactivate edilebilir)
   - Kalıcı ban
   - Uyarı notu ekle
5. Aksiyonu gerçekleştirir
6. Audit log'a neden yazar

**Gerekli UI Bileşenleri:**
- 🔍 Gelişmiş kullanıcı arama
- 👤 Kullanıcı profil detay sayfası
- 📝 Timeline view (kullanıcı aktiviteleri)
- ⚠️ Aksiyon modal'ları (confirm dialogları)
- 📄 Not/neden girme alanları

---

#### Senaryo 1.3: Sponsorluk Paket Onaylama
**Sıklık:** Günde 1-2 kez (ortalama)

**İş Akışı:**
1. "Bekleyen Onaylar" bölümünü açar
2. Yeni satın almaları listeler
3. Her satın alma için:
   - Sponsor detaylarını kontrol
   - Ödeme bilgilerini doğrular
   - Geçmiş sponsorluk performansına bakar
4. Onaylar veya reddeder
5. Onaylanırsa → Kod üretimi otomatik başlar
6. Sponsor'a bildirim gönderilir

**Gerekli UI Bileşenleri:**
- 📋 Pending approvals listesi
- 💳 Ödeme detay kartları
- ✅ Bulk approve özelliği
- 📊 Sponsor performance özet
- 📧 Bildirim önizleme

---

## 2. Veri Analisti (Data Analyst)

### Profil
- **Ad:** Mehmet Kaya
- **Yaş:** 28
- **Rol:** Veri Analisti
- **Deneyim:** 4 yıl veri analizi
- **Teknik Seviye:** İleri seviye (SQL, Excel, BI tools)

### Sorumluluklar
- ✅ Haftalık/aylık raporlar hazırlama
- ✅ Kullanıcı davranışlarını analiz etme
- ✅ Sponsorluk ROI hesaplama
- ✅ Trend analizi yapma
- ✅ Yönetim için insight'lar üretme

### Kullandığı Endpoint'ler

#### Analytics (Yoğun Kullanım)
```
GET /api/admin/analytics/user-statistics?startDate=X&endDate=Y
GET /api/admin/analytics/subscription-statistics?startDate=X&endDate=Y
GET /api/admin/sponsorship/statistics?startDate=X&endDate=Y
GET /api/admin/sponsorship/sponsors/{id}/detailed-report
```

#### Data Export
```
GET /api/admin/analytics/export?type=csv&startDate=X&endDate=Y
```

#### Activity Logs (Behavior Analysis)
```
GET /api/admin/analytics/activity-logs?page=X&pageSize=100
```

### Kullanım Senaryoları

#### Senaryo 2.1: Haftalık Performans Raporu
**Sıklık:** Her Pazartesi sabahı

**İş Akışı:**
1. Dashboard'a giriş
2. Tarih aralığı seç (geçen hafta)
3. User statistics'i çek
   - Yeni kayıtlar
   - Aktif kullanıcı oranı
   - Churn rate
4. Subscription statistics'i çek
   - Yeni abonelikler
   - İptal oranı
   - Tier dağılımı
5. Sponsorship statistics'i çek
   - Toplam satın alma
   - Kod kullanım oranı
   - ROI metrikleri
6. Karşılaştırma yap (week-over-week)
7. CSV export al
8. Excel'de pivot table ve grafikler hazırla
9. Yönetime sunum yap

**Gerekli UI Bileşenleri:**
- 📅 Gelişmiş tarih aralığı seçici (preset'lerle: "Last Week", "Last Month")
- 📊 Karşılaştırmalı grafikler (week-over-week, month-over-month)
- 📥 Export butonu (CSV, Excel, PDF)
- 📈 Trend indicators (↑↓ arrows with %)
- 🔄 Refresh data butonu

---

#### Senaryo 2.2: Sponsorluk ROI Analizi
**Sıklık:** Ayda 1 kez (her ayın 5'i)

**İş Akışı:**
1. Tüm sponsor listesini çeker
2. Her sponsor için detailed report alır
3. Karşılaştırmalı analiz yapar:
   - Satın alma tutarı vs dağıtılan kod sayısı
   - Kod kullanım oranı
   - Farmer reach (kaç çiftçiye ulaştı)
   - Tier bazlı performans
4. En iyi performans gösteren sponsorları belirler
5. Düşük performanslılar için öneriler hazırlar
6. Toplam ROI hesaplar
7. Rapor hazırlar (grafik + özet)

**Gerekli UI Bileşenleri:**
- 📋 Sponsor comparison table (sortable, filterable)
- 📊 ROI calculator widget
- 📈 Performance scatter plot (investment vs reach)
- 🏆 Top performers highlight
- 📉 Underperformers warning cards
- 📄 Report builder (drag-drop widgets)

---

## 3. Kullanıcı Destek Uzmanı (User Support)

### Profil
- **Ad:** Zeynep Arslan
- **Yaş:** 25
- **Rol:** Kullanıcı Destek Uzmanı
- **Deneyim:** 2 yıl müşteri hizmetleri
- **Teknik Seviye:** Orta seviye

### Sorumluluklar
- ✅ Kullanıcı şikayetlerini çözme
- ✅ Hesap sorunlarını giderme
- ✅ Abonelik yönetimi (uzatma, iptal, değişim)
- ✅ Kullanıcı adına işlem yapma (OBO)
- ✅ Teknik olmayan sorunları çözme

### Kullandığı Endpoint'ler

#### User Search & Details
```
GET /api/admin/users/search?searchTerm=X
GET /api/admin/users/{id}
GET /api/admin/users/{id}/analyses
```

#### Subscription Management
```
GET /api/admin/subscriptions
GET /api/admin/subscriptions/{id}
POST /api/admin/subscriptions/assign
POST /api/admin/subscriptions/extend
POST /api/admin/subscriptions/cancel
```

#### On-Behalf-Of Operations
```
POST /api/admin/plant-analysis/on-behalf-of
POST /api/admin/users/register
```

#### Activity Monitoring (User-Specific)
```
GET /api/admin/analytics/activity-logs?userId=X
GET /api/admin/audit/by-target?targetUserId=X
```

### Kullanım Senaryoları

#### Senaryo 3.1: Abonelik Sorunu Çözme
**Sıklık:** Günde 5-10 kez

**İş Akışı:**
1. Kullanıcıdan şikayet gelir: "Aboneliğim bitmiş ama yenilemeyi unuttum"
2. Kullanıcıyı ara (isim, email veya telefon)
3. Kullanıcı profilini aç
4. Abonelik geçmişini kontrol et
   - Son abonelik ne zaman bitti?
   - Daha önce kaç kez yeniledi?
   - Ödeme geçmişi nasıl?
5. Karar ver:
   - Goodwill olarak 7 gün uzat
   - Veya yeni abonelik ata
6. Aksiyonu gerçekleştir
7. Kullanıcıya bilgi ver
8. Ticket'ı kapat

**Gerekli UI Bileşenleri:**
- 🔍 Quick user search (sidebar'da her zaman erişilebilir)
- 👤 User profile quick view (modal)
- 📅 Subscription timeline
- ⚡ Quick actions panel (extend, assign, cancel)
- 📝 Notes section (destek notu ekleme)
- ✅ Case/ticket linking

---

#### Senaryo 3.2: Kullanıcı Adına Bitki Analizi (OBO)
**Sıklık:** Günde 2-3 kez

**İş Akışı:**
1. Kullanıcıdan gelen istek: "Uygulama çalışmıyor, analiz yapar mısınız?"
2. Kullanıcı profilini aç
3. Kullanıcıdan fotoğraf al (email/whatsapp)
4. "Create Analysis On Behalf Of" modalını aç
5. Kullanıcıyı seç
6. Fotoğrafı upload et
7. Not ekle: "Kullanıcı adına destek talebi için oluşturuldu"
8. Submit
9. Analiz sonucunu kullanıcıya ilet
10. Kullanıcıya uygulamadan nasıl yapacağını öğret

**Gerekli UI Bileşenleri:**
- 📤 Image upload component (drag-drop)
- 📝 Analysis form (on-behalf-of modal)
- 👤 User selector (typeahead search)
- 📄 Notes/reason textarea
- 📊 Analysis result preview
- 📧 Send result to user button

---

#### Senaryo 3.3: Sponsorluk Kodu Sorunu
**Sıklık:** Günde 1-2 kez

**İş Akışı:**
1. Çiftçiden şikayet: "Kod çalışmıyor"
2. Çiftçiyi ara ve profilini aç
3. Kodu kontrol et (kod detayına git)
4. Kod durumunu kontrol et:
   - Kullanılmış mı?
   - Süresi dolmuş mu?
   - Geçerli mi?
5. Problemi belirle:
   - Kod zaten kullanılmış → Sponsor'a yeni kod istenmeli
   - Kod süresi dolmuş → Sponsor'a bilgi ver
   - Kod geçersiz → Yeni kod oluştur
6. Çözümü uygula
7. Çiftçiye yeni kod ver veya durumu açıkla

**Gerekli UI Bileşenleri:**
- 🔍 Code lookup/search
- 📋 Code details card (status, expiry, usage)
- 👤 Code owner (sponsor) info
- ⚙️ Code actions (deactivate, regenerate)
- 📧 Notification sender

---

## 4. Sponsorluk Koordinatörü (Sponsorship Manager)

### Profil
- **Ad:** Burak Yıldız
- **Yaş:** 32
- **Rol:** Sponsorluk Koordinatörü
- **Deneyim:** 6 yıl kurumsal satış & ortaklıklar
- **Teknik Seviye:** Orta seviye

### Sorumluluklar
- ✅ Sponsorları onaylama
- ✅ Sponsor ilişkilerini yönetme
- ✅ Sponsorluk paketleri tasarlama
- ✅ Kod dağıtım stratejisi belirleme
- ✅ Sponsor performansını izleme

### Kullandığı Endpoint'ler

#### Sponsorship Operations
```
GET /api/admin/sponsorship/purchases
GET /api/admin/sponsorship/purchases/{id}
POST /api/admin/sponsorship/purchases/{id}/approve
POST /api/admin/sponsorship/purchases/{id}/refund
POST /api/admin/sponsorship/purchases/create-on-behalf-of
```

#### Sponsor Analytics
```
GET /api/admin/sponsorship/statistics
GET /api/admin/sponsorship/sponsors/{id}/detailed-report
```

#### Code Management
```
GET /api/admin/sponsorship/codes
GET /api/admin/sponsorship/codes/{id}
POST /api/admin/sponsorship/codes/bulk-send
POST /api/admin/sponsorship/codes/{id}/deactivate
```

### Kullanım Senaryoları

#### Senaryo 4.1: Kurumsal Sponsor Onboarding
**Sıklık:** Ayda 2-3 kez (yeni sponsor)

**İş Akışı:**
1. Yeni kurumsal sponsor ile anlaşma imzalandı
2. Sponsor detaylarını sisteme gir
3. Özel paket oluştur (custom tier)
   - Kod sayısı: 500
   - Geçerlilik: 12 ay
   - Özel fiyat
4. Satın alma kaydı oluştur (on-behalf-of)
5. Otomatik kod üretimi başlar
6. Sponsor dashboard'una erişim ver
7. Kod dağıtım stratejisini planla
8. İlk batch kodları sponsor'a gönder
9. Onboarding dokümanı paylaş

**Gerekli UI Bileşenleri:**
- 📝 Sponsor registration form
- 💳 Custom package builder
- 🎫 Bulk code generator
- 📧 Email template selector
- 📊 Onboarding checklist
- 📄 Document repository

---

#### Senaryo 4.2: Sponsor Performans İncelemesi
**Sıklık:** Ayda 1 kez (her sponsor için)

**İş Akışı:**
1. Sponsor listesini aç
2. Bir sponsor seç
3. Detailed report'u görüntüle:
   - Toplam satın alma
   - Toplam kod üretimi
   - Kod kullanım oranı (% kaç kod kullanıldı)
   - Farmer reach (kaç çiftçi kullandı)
   - Tier breakdown
   - Aylık trend
4. Performans değerlendirmesi yap:
   - İyi performans → Teşekkür emaili + premium benefits öner
   - Orta performans → Kod kullanımını artırma önerileri
   - Düşük performans → Görüşme planlama
5. Action plan belirle
6. Notes ekle
7. Follow-up reminder kur

**Gerekli UI Bileşenleri:**
- 📊 Sponsor performance dashboard
- 📈 Usage trend charts
- 🎯 KPI cards (kod kullanım %, farmer reach)
- 📋 Action items checklist
- 🔔 Reminder/notification scheduler
- 📝 Internal notes section

---

## 5. Sistem Denetçisi (Auditor)

### Profil
- **Ad:** Dr. Selma Öztürk
- **Yaş:** 40
- **Rol:** Sistem Denetçisi (Compliance & Security)
- **Deneyim:** 12 yıl IT audit & compliance
- **Teknik Seviye:** İleri seviye

### Sorumluluklar
- ✅ Tüm admin işlemlerini denetleme
- ✅ Compliance kurallarına uygunluk kontrolü
- ✅ Şüpheli aktiviteleri tespit etme
- ✅ Güvenlik açıklarını raporlama
- ✅ Audit raporları hazırlama

### Kullandığı Endpoint'ler

#### Audit Logs (Yoğun Kullanım)
```
GET /api/admin/analytics/activity-logs
GET /api/admin/audit/all
GET /api/admin/audit/by-admin?adminUserId=X
GET /api/admin/audit/by-target?targetUserId=X
GET /api/admin/audit/on-behalf-of
```

#### User & Operation Analysis
```
GET /api/admin/users
GET /api/admin/plant-analysis/on-behalf-of
GET /api/admin/sponsorship/purchases
```

### Kullanım Senaryoları

#### Senaryo 5.1: Aylık Compliance Raporu
**Sıklık:** Her ayın ilk haftası

**İş Akışı:**
1. Activity logs sayfasına git
2. Tarih aralığı: Geçen ay
3. Tüm admin işlemlerini listele (page by page)
4. Kategorilere ayır:
   - User deactivations (kaç, kim tarafından, neden)
   - Subscription changes (kaç, tip)
   - On-behalf-of operations (kaç, kim tarafından)
   - Refunds (kaç, toplam tutar)
   - Bulk operations (kaç, nedenler)
5. Her kategori için:
   - İstatistik çıkar
   - Anomali tespiti (örn: 1 admin çok fazla deactivation yapmış)
   - Policy uygunluğu kontrol et
6. Findings document et
7. Risky operations highlight et
8. Yönetime rapor sun

**Gerekli UI Bileşenleri:**
- 📋 Activity log table (advanced filtering)
- 🔍 Advanced search/filter panel
- 📊 Activity breakdown charts (by type, by admin, by date)
- ⚠️ Anomaly detection alerts
- 📥 Bulk export (all logs as CSV)
- 📄 Report generator
- 🎯 Compliance checklist

---

#### Senaryo 5.2: Şüpheli Aktivite İncelemesi
**Sıklık:** Ad-hoc (şüphe uyandığında)

**İş Akışı:**
1. Anomaly alert gelir: "Admin user #166, 1 saatte 50 kullanıcı deaktive etti"
2. Activity logs'u filtrele:
   - AdminUserId = 166
   - Action = "DeactivateUser"
   - Last 24 hours
3. Tüm deactivation kayıtlarını incele:
   - Her birinin nedeni var mı?
   - IP adresi aynı mı?
   - Kullanıcılar birbiriyle ilişkili mi?
4. Admin'in diğer aktivitelerini kontrol et (son 7 gün)
5. Bulgular:
   - Meşru toplu işlem mi? (örn: spam hesapları temizleme)
   - Şüpheli davranış mı? (örn: yetkisiz işlem)
6. Karar:
   - Meşru → Document et, kapat
   - Şüpheli → Admin hesabını kısıtla, derinlemesine araştır
7. Incident report hazırla
8. Gerekirse admin'le görüşme yap

**Gerekli UI Bileşenleri:**
- 🚨 Anomaly detection dashboard
- 🔍 Advanced log filtering (multi-criteria)
- 👤 Admin activity timeline
- 🌐 IP/location tracking
- 📊 Pattern analysis tools
- 🔒 Admin action restrictor
- 📝 Incident report form

---

## Endpoint-Persona Mapping Matrix

### Kullanım Yoğunluğu Matrisi

| Endpoint Group | Super Admin | Analyst | Support | Sponsorship | Auditor |
|---------------|-------------|---------|---------|-------------|---------|
| **Analytics & Dashboard** | 🔥🔥🔥 | 🔥🔥🔥🔥🔥 | 🔥 | 🔥🔥 | 🔥🔥 |
| **User Management** | 🔥🔥🔥 | 🔥 | 🔥🔥🔥🔥🔥 | - | 🔥🔥 |
| **Subscription Management** | 🔥🔥 | 🔥 | 🔥🔥🔥🔥 | - | 🔥 |
| **Sponsorship Management** | 🔥🔥🔥 | 🔥🔥 | 🔥 | 🔥🔥🔥🔥🔥 | 🔥🔥 |
| **Plant Analysis (OBO)** | 🔥 | - | 🔥🔥🔥 | - | 🔥 |
| **Activity Logs** | 🔥🔥🔥 | 🔥🔥 | 🔥🔥 | 🔥 | 🔥🔥🔥🔥🔥 |
| **Audit Logs** | 🔥🔥 | 🔥 | - | - | 🔥🔥🔥🔥🔥 |

**Açıklama:**
- 🔥 = Düşük kullanım (ayda birkaç kez)
- 🔥🔥 = Orta kullanım (haftada birkaç kez)
- 🔥🔥🔥 = Yüksek kullanım (günde birkaç kez)
- 🔥🔥🔥🔥 = Çok yüksek kullanım (günde 10+ kez)
- 🔥🔥🔥🔥🔥 = Kritik/Sürekli (her gün, çok sık)

---

## Frontend Geliştirme Öncelikleri

### Phase 1: Temel İşlevsellik (MVP)
**Hedef:** Super Admin ve Support için temel özellikler

**Öncelik Sırası:**
1. **Authentication & Authorization** (1 hafta)
   - Login page
   - JWT token management
   - Role-based access control
   - Protected routes

2. **Dashboard (Super Admin)** (1.5 hafta)
   - KPI cards (user stats, subscription stats, sponsorship stats)
   - Activity feed (son 10 aktivite)
   - Quick actions panel
   - Responsive layout

3. **User Management (Support)** (2 hafta)
   - User search
   - User list (paginated, filterable)
   - User detail view
   - User actions (activate, deactivate)
   - Activity log (user-specific)

4. **Subscription Management (Support)** (1.5 hafta)
   - Subscription list
   - Assign subscription
   - Extend subscription
   - Cancel subscription
   - Subscription timeline

**Toplam:** ~6 hafta

---

### Phase 2: Analitik & Raporlama
**Hedef:** Data Analyst için gelişmiş analiz araçları

**Öncelik Sırası:**
1. **Analytics Dashboard** (2 hafta)
   - Date range picker (presets)
   - User statistics (charts)
   - Subscription statistics (charts)
   - Sponsorship statistics (charts)
   - Week-over-week comparison
   - Export to CSV

2. **Sponsor Reports** (1.5 hafta)
   - Sponsor list
   - Sponsor detailed report
   - ROI calculator
   - Performance comparison table
   - Report builder

**Toplam:** ~3.5 hafta

---

### Phase 3: Sponsorluk Yönetimi
**Hedef:** Sponsorship Manager için tam özellik seti

**Öncelik Sırası:**
1. **Sponsor Operations** (2 hafta)
   - Purchase approval workflow
   - Bulk approve
   - Refund flow
   - Create purchase (on-behalf-of)
   - Code management

2. **Sponsor Dashboard** (1 hafta)
   - Sponsor performance overview
   - Code usage tracking
   - Farmer reach metrics
   - Action planner

**Toplam:** ~3 hafta

---

### Phase 4: Audit & Compliance
**Hedef:** Auditor için kapsamlı denetim araçları

**Öncelik Sırası:**
1. **Activity Logs** (2 hafta)
   - Advanced filtering
   - Log detail view
   - Pattern detection
   - Export capabilities

2. **Audit Dashboard** (1.5 hafta)
   - Compliance checklist
   - Anomaly detection
   - Incident reporting
   - Admin activity tracking

**Toplam:** ~3.5 hafta

---

## React Component Önerileri

### Shared Components

```
components/
├── Layout/
│   ├── AdminLayout.tsx          # Ana layout (sidebar, header, content)
│   ├── Sidebar.tsx               # Sol menü (persona bazlı items)
│   ├── Header.tsx                # Üst bar (user info, notifications)
│   └── Footer.tsx
│
├── Common/
│   ├── KPICard.tsx               # Dashboard KPI widget
│   ├── DataTable.tsx             # Reusable table (pagination, sort, filter)
│   ├── SearchBar.tsx             # Global search
│   ├── DateRangePicker.tsx       # Tarih aralığı seçici
│   ├── ExportButton.tsx          # CSV/Excel export
│   ├── ConfirmDialog.tsx         # Onay modal'ı
│   └── LoadingSpinner.tsx
│
├── Charts/
│   ├── LineChart.tsx             # Trend grafiği
│   ├── BarChart.tsx              # Bar grafik
│   ├── PieChart.tsx              # Pasta grafik
│   └── ChartCard.tsx             # Grafik container
│
└── Forms/
    ├── UserSearchForm.tsx
    ├── SubscriptionForm.tsx
    ├── SponsorApprovalForm.tsx
    └── OBOAnalysisForm.tsx
```

### Page Components

```
pages/
├── Dashboard/
│   ├── SuperAdminDashboard.tsx
│   ├── AnalystDashboard.tsx
│   └── components/
│       ├── ActivityFeed.tsx
│       ├── QuickStats.tsx
│       └── TrendChart.tsx
│
├── Users/
│   ├── UserList.tsx
│   ├── UserDetail.tsx
│   ├── UserSearch.tsx
│   └── components/
│       ├── UserCard.tsx
│       ├── UserActions.tsx
│       └── UserTimeline.tsx
│
├── Subscriptions/
│   ├── SubscriptionList.tsx
│   ├── SubscriptionDetail.tsx
│   └── components/
│       ├── AssignModal.tsx
│       ├── ExtendModal.tsx
│       └── SubscriptionTimeline.tsx
│
├── Sponsorship/
│   ├── PurchaseList.tsx
│   ├── SponsorDetail.tsx
│   ├── CodeManagement.tsx
│   └── components/
│       ├── ApprovalCard.tsx
│       ├── RefundModal.tsx
│       └── PerformanceChart.tsx
│
├── Analytics/
│   ├── UserAnalytics.tsx
│   ├── SubscriptionAnalytics.tsx
│   ├── SponsorshipAnalytics.tsx
│   └── components/
│       ├── StatCard.tsx
│       ├── ComparisonTable.tsx
│       └── ExportPanel.tsx
│
└── Audit/
    ├── ActivityLogs.tsx
    ├── AuditDashboard.tsx
    └── components/
        ├── LogTable.tsx
        ├── FilterPanel.tsx
        └── AnomalyAlert.tsx
```

---

## API Service Layer Önerisi

```typescript
// services/api/index.ts
import axios from 'axios';

const API_BASE = process.env.REACT_APP_API_BASE_URL;

const apiClient = axios.create({
  baseURL: API_BASE,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor (JWT token ekle)
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('adminToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor (error handling)
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token expired, redirect to login
      localStorage.removeItem('adminToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default apiClient;
```

```typescript
// services/api/analytics.ts
import apiClient from './index';

export const analyticsService = {
  getDashboardOverview: () => 
    apiClient.get('/api/admin/analytics/dashboard-overview'),

  getUserStatistics: (params?: { startDate?: string; endDate?: string }) => 
    apiClient.get('/api/admin/analytics/user-statistics', { params }),

  getSubscriptionStatistics: (params?: { startDate?: string; endDate?: string }) => 
    apiClient.get('/api/admin/analytics/subscription-statistics', { params }),

  getSponsorshipStatistics: (params?: { startDate?: string; endDate?: string }) => 
    apiClient.get('/api/admin/sponsorship/statistics', { params }),

  getActivityLogs: (params: {
    page: number;
    pageSize: number;
    userId?: number;
    actionType?: string;
    startDate?: string;
    endDate?: string;
  }) => apiClient.get('/api/admin/analytics/activity-logs', { params }),
};
```

```typescript
// services/api/users.ts
import apiClient from './index';

export const userService = {
  getAll: (params: { page: number; pageSize: number; isActive?: boolean }) => 
    apiClient.get('/api/admin/users', { params }),

  getById: (userId: number) => 
    apiClient.get(`/api/admin/users/${userId}`),

  search: (searchTerm: string, page = 1, pageSize = 20) => 
    apiClient.get('/api/admin/users/search', { 
      params: { searchTerm, page, pageSize } 
    }),

  deactivate: (userId: number, reason: string) => 
    apiClient.post(`/api/admin/users/deactivate/${userId}`, { reason }),

  reactivate: (userId: number, reason: string) => 
    apiClient.post(`/api/admin/users/reactivate/${userId}`, { reason }),

  bulkDeactivate: (userIds: number[], reason: string) => 
    apiClient.post('/api/admin/users/bulk/deactivate', { userIds, reason }),
};
```

---

## State Management Önerisi

### Option 1: Redux Toolkit (Önerilen)
**Avantajları:**
- ✅ Büyük ölçekli uygulamalar için ideal
- ✅ Güçlü developer tools
- ✅ RTK Query ile API entegrasyonu kolay
- ✅ TypeScript desteği mükemmel

**Kullanım:**
```typescript
// store/slices/dashboardSlice.ts
import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { analyticsService } from '../../services/api/analytics';

export const fetchDashboardData = createAsyncThunk(
  'dashboard/fetchData',
  async () => {
    const response = await analyticsService.getDashboardOverview();
    return response.data;
  }
);

const dashboardSlice = createSlice({
  name: 'dashboard',
  initialState: {
    data: null,
    loading: false,
    error: null,
  },
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchDashboardData.pending, (state) => {
        state.loading = true;
      })
      .addCase(fetchDashboardData.fulfilled, (state, action) => {
        state.loading = false;
        state.data = action.payload;
      })
      .addCase(fetchDashboardData.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message;
      });
  },
});

export default dashboardSlice.reducer;
```

### Option 2: React Query (Alternatif)
**Avantajları:**
- ✅ API state management için optimize
- ✅ Otomatik caching
- ✅ Background refetching
- ✅ Daha az boilerplate

**Kullanım:**
```typescript
// hooks/useUserStatistics.ts
import { useQuery } from '@tanstack/react-query';
import { analyticsService } from '../services/api/analytics';

export const useUserStatistics = (startDate?: string, endDate?: string) => {
  return useQuery({
    queryKey: ['userStatistics', startDate, endDate],
    queryFn: () => analyticsService.getUserStatistics({ startDate, endDate }),
    staleTime: 5 * 60 * 1000, // 5 dakika
  });
};
```

---

## UI/UX Design Guidelines

### Color Palette (Persona Bazlı)

**Super Admin (Primary Blue)**
- Primary: `#1976d2` (Material Blue)
- Secondary: `#424242` (Dark Gray)
- Accent: `#ff9800` (Orange - warning)

**Data Analyst (Analytics Purple)**
- Primary: `#6a1b9a` (Deep Purple)
- Charts: Multi-color palette (10 renk)

**User Support (Friendly Green)**
- Primary: `#43a047` (Green)
- Success: `#66bb6a`
- Error: `#f44336`

**Sponsorship Manager (Gold)**
- Primary: `#f57c00` (Orange)
- Secondary: `#ffc107` (Amber)

**Auditor (Professional Gray)**
- Primary: `#37474f` (Blue Gray)
- Warning: `#ff5722` (Deep Orange)

### Typography
- **Headings:** Inter / Roboto (500-700 weight)
- **Body:** Inter / Roboto (400 weight)
- **Monospace (logs):** Fira Code / Consolas

### Layout
- **Sidebar Width:** 240px (collapsed: 60px)
- **Content Max Width:** 1440px
- **Card Padding:** 24px
- **Grid Gutter:** 16px

---

## Sonraki Adımlar

### 1. Frontend Proje Kurulumu
- [ ] React + TypeScript + Vite setup
- [ ] UI library seçimi (Material-UI, Ant Design, veya Tailwind)
- [ ] Router setup (React Router v6)
- [ ] State management setup (Redux Toolkit veya React Query)
- [ ] API service layer
- [ ] Authentication flow

### 2. Tasarım Sistemi
- [ ] Figma/Adobe XD mockup'ları
- [ ] Component library (Storybook)
- [ ] Theme configuration
- [ ] Icon set selection

### 3. İlk Sprint (MVP)
- [ ] Login page
- [ ] Dashboard (Super Admin)
- [ ] User list + search
- [ ] User detail view

---

**Hazırlayan:** Claude Code  
**Tarih:** 2025-10-23  
**Durum:** Frontend geliştirmeye hazır  
**Sonraki Seans:** React component implementation