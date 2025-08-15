# 🏢 ZiraAI Sponsorluk Sistemi - Kapsamlı Dokümantasyon

## 📋 İçindekiler
1. [Sistem Genel Bakış](#sistem-genel-bakış)
2. [Sponsorluk Paketleri](#sponsorluk-paketleri)
3. [Database Yapısı](#database-yapısı)
4. [API Entegrasyonu](#api-entegrasyonu)
5. [Frontend Entegrasyonu](#frontend-entegrasyonu)
6. [Kod Örnekleri](#kod-örnekleri)
7. [Test Senaryoları](#test-senaryoları)
8. [Deployment Kılavuzu](#deployment-kılavuzu)

---

## 🎯 Sistem Genel Bakış

ZiraAI Sponsorluk Sistemi, tarımsal analiz platformunda sponsor firmalar için 4 aşamalı (S, M, L, XL) hizmet paketi sunan kapsamlı bir çözümdür.

### ✨ Temel Özellikler
- **4 Tier Sponsorluk Sistemi**: S, M, L, XL paketleri
- **Dinamik Logo Görünürlüğü**: Ekran bazında logo kontrolü
- **Veri Erişim Kontrol**: %30, %60, %100 veri erişim seviyeleri
- **Mesajlaşma Sistemi**: Sponsor-çiftçi iletişimi
- **Akıllı Link Sistemi**: AI destekli ürün önerisi
- **Analitik ve Raporlama**: Detaylı performans takibi

### 🔧 Teknik Mimari
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Frontend      │────│   WebAPI         │────│   Business      │
│   (React/Vue)   │    │   Controllers    │    │   Services      │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │                        │
                        ┌──────────────────┐    ┌─────────────────┐
                        │   Data Access    │────│   Database      │
                        │   Repositories   │    │   PostgreSQL    │
                        └──────────────────┘    └─────────────────┘
```

---

## 📦 Sponsorluk Paketleri

### 🟢 S Paketi (Small)
**💰 Fiyat**: ₺299/ay
```json
{
  "visibilityLevel": "ResultOnly",
  "dataAccessLevel": "Basic30", 
  "dataAccessPercentage": 30,
  "hasMessaging": false,
  "hasSmartLinking": false
}
```

**📋 Özellikler**:
- ✅ Logo sadece sonuç ekranlarında görüntülenir
- ✅ Temel kullanım istatistikleri
- ✅ %30 çiftçi analiz verilerine erişim
- ✅ Temel analitik raporlar
- ❌ Mesajlaşma yok
- ❌ Akıllı linkler yok

**🎯 Erişilebilir Veri Alanları**:
- OverallHealthScore, PlantSpecies, PlantVariety
- GrowthStage, ImagePath, CropType
- AnalysisDate, PlantType (Legacy)

### 🟡 M Paketi (Medium)
**💰 Fiyat**: ₺599/ay
```json
{
  "visibilityLevel": "StartAndResult",
  "dataAccessLevel": "Basic30",
  "dataAccessPercentage": 30,
  "hasMessaging": true,
  "hasSmartLinking": false
}
```

**📋 Özellikler**:
- ✅ Logo başlangıç ve sonuç ekranlarında
- ✅ %30 veri erişimi + çiftçi profilleri
- ✅ Temel mesajlaşma sistemi
- ✅ Gelişmiş analitik raporlar
- ✅ Çiftçi iletişim bilgileri (sınırlı)
- ❌ Akıllı linkler yok

### 🟠 L Paketi (Large) 
**💰 Fiyat**: ₺1.199/ay
```json
{
  "visibilityLevel": "AllScreens",
  "dataAccessLevel": "Medium60",
  "dataAccessPercentage": 60,
  "hasMessaging": true,
  "hasSmartLinking": false
}
```

**📋 Özellikler**:
- ✅ Logo tüm uygulama ekranlarında
- ✅ %60 detaylı analiz verilerine erişim
- ✅ Tam mesajlaşma sistemi (threading)
- ✅ Lokasyon ve çevre koşulları bilgisi
- ✅ Hastalık ve zararlı analizleri
- ✅ Gelişmiş raporlama

**🎯 Ek Erişilebilir Veri Alanları**:
- VigorScore, HealthSeverity, StressIndicators
- DiseaseSymptoms, NutrientStatus, Recommendations
- Location, WeatherConditions, SoilType
- Diseases, Pests, ElementDeficiencies

### 🔴 XL Paketi (Extra Large)
**💰 Fiyat**: ₺2.399/ay
```json
{
  "visibilityLevel": "AllScreens", 
  "dataAccessLevel": "Full100",
  "dataAccessPercentage": 100,
  "hasMessaging": true,
  "hasSmartLinking": true
}
```

**📋 Özellikler**:
- ✅ Tüm veri alanlarına %100 erişim
- ✅ AI destekli akıllı link sistemi
- ✅ Ürün önerisi ve pazarlama
- ✅ A/B testing yetenekleri
- ✅ Detaylı çiftçi profilleri
- ✅ Tam analitik suite

**🎯 Ek Özellikler**:
- ContactPhone, ContactEmail, AdditionalInfo
- DetailedAnalysisData, CrossFactorInsights
- EstimatedYieldImpact, ConfidenceLevel
- Smart product recommendations

---

## 🗄️ Database Yapısı

### 📊 SponsorProfile Entity
```sql
CREATE TABLE "SponsorProfiles" (
    "Id" SERIAL PRIMARY KEY,
    "SponsorId" INTEGER NOT NULL,
    "CompanyName" VARCHAR(200) NOT NULL,
    "CompanyDescription" TEXT,
    "SponsorLogoUrl" VARCHAR(500),
    "WebsiteUrl" VARCHAR(500),
    "ContactEmail" VARCHAR(100),
    "ContactPhone" VARCHAR(20),
    "ContactPerson" VARCHAR(100),
    "CurrentSubscriptionTierId" INTEGER NOT NULL,
    "VisibilityLevel" VARCHAR(50) NOT NULL, -- ResultOnly, StartAndResult, AllScreens
    "DataAccessLevel" VARCHAR(50) NOT NULL, -- Basic30, Medium60, Full100
    "HasMessaging" BOOLEAN NOT NULL DEFAULT FALSE,
    "HasSmartLinking" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "TotalSponsored" INTEGER DEFAULT 0,
    "ActiveSponsored" INTEGER DEFAULT 0,
    "TotalInvestment" DECIMAL(18,2) DEFAULT 0,
    "CreatedDate" TIMESTAMP NOT NULL,
    "UpdatedDate" TIMESTAMP
);
```

### 📈 SponsorAnalysisAccess Entity
```sql
CREATE TABLE "SponsorAnalysisAccesses" (
    "Id" SERIAL PRIMARY KEY,
    "SponsorId" INTEGER NOT NULL,
    "PlantAnalysisId" INTEGER NOT NULL,
    "FarmerId" INTEGER NOT NULL,
    "AccessLevel" VARCHAR(50) NOT NULL,
    "AccessPercentage" INTEGER NOT NULL,
    "FirstViewedDate" TIMESTAMP NOT NULL,
    "LastViewedDate" TIMESTAMP NOT NULL,
    "ViewCount" INTEGER DEFAULT 0,
    "AccessedFields" TEXT, -- JSON array
    "RestrictedFields" TEXT, -- JSON array
    "CanViewHealthScore" BOOLEAN DEFAULT TRUE,
    "CanViewDiseases" BOOLEAN DEFAULT FALSE,
    "CanViewPests" BOOLEAN DEFAULT FALSE,
    "CanViewNutrients" BOOLEAN DEFAULT FALSE,
    "CanViewRecommendations" BOOLEAN DEFAULT FALSE,
    "CanViewFarmerContact" BOOLEAN DEFAULT FALSE,
    "CanViewLocation" BOOLEAN DEFAULT FALSE,
    "CanViewImages" BOOLEAN DEFAULT TRUE
);
```

### 💬 AnalysisMessage Entity
```sql
CREATE TABLE "AnalysisMessages" (
    "Id" SERIAL PRIMARY KEY,
    "PlantAnalysisId" INTEGER NOT NULL,
    "FromUserId" INTEGER NOT NULL,
    "ToUserId" INTEGER NOT NULL,
    "ParentMessageId" INTEGER NULL,
    "Message" TEXT NOT NULL,
    "Subject" VARCHAR(200),
    "MessageType" VARCHAR(50), -- Information, Question, Answer, Warning
    "SentDate" TIMESTAMP NOT NULL,
    "IsRead" BOOLEAN DEFAULT FALSE,
    "IsApproved" BOOLEAN DEFAULT TRUE,
    "IsDeleted" BOOLEAN DEFAULT FALSE,
    "IsFlagged" BOOLEAN DEFAULT FALSE,
    "FlagReason" VARCHAR(200),
    "SenderRole" VARCHAR(20), -- Farmer, Sponsor
    "SenderName" VARCHAR(100),
    "SenderCompany" VARCHAR(200),
    "Priority" VARCHAR(20) DEFAULT 'Normal', -- Low, Normal, High, Urgent
    "Category" VARCHAR(50) DEFAULT 'General'
);
```

### 🔗 SmartLink Entity
```sql
CREATE TABLE "SmartLinks" (
    "Id" SERIAL PRIMARY KEY,
    "SponsorId" INTEGER NOT NULL,
    "SponsorName" VARCHAR(100),
    "LinkUrl" VARCHAR(1000) NOT NULL,
    "LinkText" VARCHAR(200) NOT NULL,
    "LinkDescription" TEXT,
    "LinkType" VARCHAR(50) DEFAULT 'Product', -- Product, Service, Information, Promotion
    "Keywords" TEXT, -- JSON array
    "ProductCategory" VARCHAR(100),
    "TargetCropTypes" TEXT, -- JSON array
    "TargetDiseases" TEXT, -- JSON array
    "TargetPests" TEXT, -- JSON array
    "TargetRegions" TEXT, -- JSON array
    "Priority" INTEGER DEFAULT 50,
    "DisplayPosition" VARCHAR(20) DEFAULT 'Inline', -- Top, Bottom, Inline, Sidebar
    "DisplayStyle" VARCHAR(20) DEFAULT 'Button', -- Button, Text, Card, Banner
    "ProductName" VARCHAR(200),
    "ProductPrice" DECIMAL(10,2),
    "ProductCurrency" VARCHAR(3) DEFAULT 'TRY',
    "IsPromotional" BOOLEAN DEFAULT FALSE,
    "DiscountPercentage" DECIMAL(5,2),
    "ClickCount" INTEGER DEFAULT 0,
    "UniqueClickCount" INTEGER DEFAULT 0,
    "DisplayCount" INTEGER DEFAULT 0,
    "ClickThroughRate" DECIMAL(5,2) DEFAULT 0,
    "ConversionCount" INTEGER DEFAULT 0,
    "ConversionRate" DECIMAL(5,2) DEFAULT 0,
    "IsActive" BOOLEAN DEFAULT TRUE,
    "IsApproved" BOOLEAN DEFAULT FALSE,
    "RelevanceScore" DECIMAL(5,2)
);
```

---

## 🔌 API Entegrasyonu

### 🏢 Sponsor Profile Management

#### POST `/api/sponsorships/create-profile`
Yeni sponsor profili oluşturur.

**Request:**
```json
{
  "sponsorId": 123,
  "companyName": "Tarım Tech Ltd.",
  "companyDescription": "Akıllı tarım çözümleri",
  "sponsorLogoUrl": "https://example.com/logo.png",
  "websiteUrl": "https://tarimtech.com",
  "contactEmail": "info@tarimtech.com",
  "contactPhone": "+90 555 123 45 67",
  "contactPerson": "Ahmet Yılmaz",
  "currentSubscriptionTierId": 2
}
```

**Response:**
```json
{
  "success": true,
  "message": "Sponsor profili başarıyla oluşturuldu"
}
```

#### GET `/api/sponsorships/profile/{sponsorId}`
Sponsor profil bilgilerini getirir.

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "sponsorId": 123,
    "companyName": "Tarım Tech Ltd.",
    "visibilityLevel": "StartAndResult",
    "dataAccessLevel": "Basic30",
    "hasMessaging": true,
    "hasSmartLinking": false,
    "isVerified": true,
    "isActive": true
  }
}
```

### 📊 Data Access Control

#### GET `/api/sponsorships/filtered-analysis/{sponsorId}/{plantAnalysisId}`
Sponsor tier'ına göre filtrelenmiş analiz verilerini getirir.

**Response (S/M Paketi - %30 Data):**
```json
{
  "success": true,
  "data": {
    "id": 456,
    "overallHealthScore": 8,
    "plantSpecies": "Solanum lycopersicum",
    "plantVariety": "Cherry Tomato",
    "growthStage": "Flowering",
    "cropType": "tomato",
    "analysisDate": "2025-01-15T10:30:00Z",
    "imagePath": "/uploads/plant_analysis_456.jpg",
    // Diğer alanlar erişim seviyesine göre kısıtlı
    "accessLevel": "Basic30",
    "accessPercentage": 30
  }
}
```

**Response (XL Paketi - %100 Data):**
```json
{
  "success": true, 
  "data": {
    // Tüm analiz verileri dahil
    "id": 456,
    "overallHealthScore": 8,
    "vigorScore": 7,
    "healthSeverity": "Mild",
    "stressIndicators": ["water_stress", "nutrient_deficiency"],
    "diseaseSymptoms": ["leaf_spots", "yellowing"],
    "nutrientStatus": {
      "nitrogen": "adequate",
      "phosphorus": "deficient",
      "potassium": "excess"
    },
    "recommendations": "Azot gübrelemesi öneriliyor...",
    "location": "Antalya, Türkiye",
    "latitude": 36.8841,
    "longitude": 30.7056,
    "weatherConditions": "Sunny, 28°C",
    "contactPhone": "+90 555 987 65 43",
    "contactEmail": "farmer@example.com",
    "accessLevel": "Full100", 
    "accessPercentage": 100
  }
}
```

### 💬 Messaging System

#### POST `/api/sponsorships/send-message`
Sponsor-çiftçi arası mesaj gönderir. (L/XL paketleri)

**Request:**
```json
{
  "fromUserId": 123,
  "toUserId": 456, 
  "plantAnalysisId": 789,
  "message": "Analizinize göre gübre önerilerimiz var.",
  "messageType": "Information",
  "priority": "Normal"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "fromUserId": 123,
    "toUserId": 456,
    "message": "Analizinize göre gübre önerilerimiz var.", 
    "senderRole": "Sponsor",
    "senderName": "Tarım Tech Ltd.",
    "sentDate": "2025-01-15T10:30:00Z",
    "isRead": false
  },
  "message": "Mesaj başarıyla gönderildi"
}
```

#### GET `/api/sponsorships/conversation/{fromUserId}/{toUserId}/{plantAnalysisId}`
İki kullanıcı arasındaki mesaj geçmişini getirir.

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "message": "Analizinize göre gübre önerilerimiz var.",
      "senderRole": "Sponsor",
      "senderName": "Tarım Tech Ltd.",
      "sentDate": "2025-01-15T10:30:00Z",
      "isRead": true
    },
    {
      "id": 2,
      "message": "Detaylı bilgi alabilir miyim?",
      "senderRole": "Farmer", 
      "senderName": "Mehmet Kaya",
      "sentDate": "2025-01-15T11:00:00Z",
      "isRead": false,
      "parentMessageId": 1
    }
  ]
}
```

### 🔗 Smart Link System (XL Paketi)

#### POST `/api/sponsorships/create-smart-link`
AI destekli akıllı link oluşturur.

**Request:**
```json
{
  "sponsorId": 123,
  "linkUrl": "https://tarimtech.com/products/nitrogen-fertilizer",
  "linkText": "Azot Gübresi - %20 İndirim",
  "linkDescription": "Domates için özel formül azot gübresi",
  "keywords": ["azot", "gübre", "domates", "beslenme"],
  "targetCropTypes": ["tomato", "pepper", "cucumber"],
  "targetDiseases": ["nutrient_deficiency"],
  "productName": "TarimTech Azot Plus",
  "productPrice": 149.99,
  "isPromotional": true,
  "discountPercentage": 20,
  "priority": 80
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "sponsorId": 123,
    "linkText": "Azot Gübresi - %20 İndirim",
    "productName": "TarimTech Azot Plus",
    "productPrice": 149.99,
    "discountPercentage": 20,
    "isActive": true,
    "isApproved": false,
    "relevanceScore": 0
  },
  "message": "Akıllı link oluşturuldu, onay bekliyor"
}
```

#### GET `/api/sponsorships/matching-links/{plantAnalysisId}`
Analiz sonucuna göre eşleşen akıllı linkleri getirir.

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "sponsorName": "Tarım Tech Ltd.", 
      "linkUrl": "https://tarimtech.com/products/nitrogen-fertilizer",
      "linkText": "Azot Gübresi - %20 İndirim",
      "productName": "TarimTech Azot Plus",
      "productPrice": 149.99,
      "discountPercentage": 20,
      "relevanceScore": 85.5,
      "displayStyle": "Card"
    },
    {
      "id": 2,
      "sponsorName": "BioGübre A.Ş.",
      "linkUrl": "https://biogubre.com/organic-solutions", 
      "linkText": "Organik Çözüm Seti",
      "productName": "Bio Beslenme Paketi",
      "productPrice": 199.99,
      "relevanceScore": 72.3,
      "displayStyle": "Button"
    }
  ]
}
```

### 📊 Analytics & Performance

#### GET `/api/sponsorships/analytics/{sponsorId}`
Sponsor performans analitikleri.

**Response:**
```json
{
  "success": true,
  "data": {
    "sponsorId": 123,
    "currentMonth": {
      "totalViews": 1250,
      "uniqueFarmers": 380,
      "messagesSent": 45,
      "messagesReceived": 67,
      "smartLinkClicks": 89,
      "conversionRate": 12.5
    },
    "topPerformingLinks": [
      {
        "linkText": "Azot Gübresi - %20 İndirim",
        "clickCount": 34,
        "conversionCount": 8,
        "conversionRate": 23.5
      }
    ],
    "accessedAnalyses": {
      "totalAccessed": 890,
      "byAccessLevel": {
        "Basic30": 567,
        "Medium60": 234,
        "Full100": 89
      }
    }
  }
}
```

---

## 🖥️ Frontend Entegrasyonu

### ⚛️ React Component Örnekleri

#### SponsorLogo Component
```jsx
import React, { useState, useEffect } from 'react';

const SponsorLogo = ({ plantAnalysisId, screenType = 'result' }) => {
  const [sponsorInfo, setSponsorInfo] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchSponsorInfo();
  }, [plantAnalysisId, screenType]);

  const fetchSponsorInfo = async () => {
    try {
      const response = await fetch(`/api/sponsorships/display-info/${plantAnalysisId}?screen=${screenType}`);
      const result = await response.json();
      
      if (result.success) {
        setSponsorInfo(result.data);
      }
    } catch (error) {
      console.error('Sponsor bilgisi alınamadı:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleLogoClick = () => {
    if (sponsorInfo?.websiteUrl) {
      window.open(sponsorInfo.websiteUrl, '_blank');
      
      // Tıklama analitiklerini kaydet
      fetch('/api/sponsorships/track-click', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          sponsorId: sponsorInfo.sponsorId,
          clickType: 'logo',
          plantAnalysisId: plantAnalysisId
        })
      });
    }
  };

  if (loading || !sponsorInfo) {
    return null;
  }

  return (
    <div className="sponsor-logo-container">
      <img 
        src={sponsorInfo.sponsorLogoUrl}
        alt={sponsorInfo.companyName}
        onClick={handleLogoClick}
        className="sponsor-logo clickable"
        title={`${sponsorInfo.companyName} tarafından desteklenmektedir`}
      />
      <span className="sponsor-text">
        {sponsorInfo.companyName} sponsorluğunda
      </span>
    </div>
  );
};
```

#### SmartLinks Component
```jsx
const SmartLinks = ({ plantAnalysisId }) => {
  const [smartLinks, setSmartLinks] = useState([]);

  useEffect(() => {
    fetchSmartLinks();
  }, [plantAnalysisId]);

  const fetchSmartLinks = async () => {
    try {
      const response = await fetch(`/api/sponsorships/matching-links/${plantAnalysisId}`);
      const result = await response.json();
      
      if (result.success) {
        setSmartLinks(result.data);
      }
    } catch (error) {
      console.error('Akıllı linkler alınamadı:', error);
    }
  };

  const handleLinkClick = (link) => {
    // Tıklama kaydı
    fetch('/api/sponsorships/increment-click', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ smartLinkId: link.id })
    });

    window.open(link.linkUrl, '_blank');
  };

  return (
    <div className="smart-links-container">
      <h3>🌱 Analizinize Özel Öneriler</h3>
      {smartLinks.map(link => (
        <div key={link.id} className={`smart-link ${link.displayStyle.toLowerCase()}`}>
          {link.displayStyle === 'Card' ? (
            <div className="product-card" onClick={() => handleLinkClick(link)}>
              <div className="product-info">
                <h4>{link.productName}</h4>
                <p>{link.linkText}</p>
                <div className="price-info">
                  <span className="price">₺{link.productPrice}</span>
                  {link.discountPercentage && (
                    <span className="discount">%{link.discountPercentage} İndirim</span>
                  )}
                </div>
              </div>
              <div className="relevance-badge">
                Uygunluk: {link.relevanceScore.toFixed(1)}%
              </div>
            </div>
          ) : (
            <button 
              className="smart-link-button"
              onClick={() => handleLinkClick(link)}
            >
              {link.linkText}
              {link.discountPercentage && (
                <span className="discount-badge">%{link.discountPercentage}</span>
              )}
            </button>
          )}
        </div>
      ))}
    </div>
  );
};
```

#### SponsorMessaging Component
```jsx
const SponsorMessaging = ({ plantAnalysisId, currentUserId, targetUserId }) => {
  const [messages, setMessages] = useState([]);
  const [newMessage, setNewMessage] = useState('');
  const [canMessage, setCanMessage] = useState(false);

  useEffect(() => {
    checkMessagingPermission();
    fetchMessages();
  }, []);

  const checkMessagingPermission = async () => {
    try {
      const response = await fetch(`/api/sponsorships/messaging-permission/${currentUserId}`);
      const result = await response.json();
      setCanMessage(result.success && result.data);
    } catch (error) {
      console.error('Mesajlaşma izni kontrol edilemedi:', error);
    }
  };

  const fetchMessages = async () => {
    try {
      const response = await fetch(
        `/api/sponsorships/conversation/${currentUserId}/${targetUserId}/${plantAnalysisId}`
      );
      const result = await response.json();
      
      if (result.success) {
        setMessages(result.data);
      }
    } catch (error) {
      console.error('Mesajlar alınamadı:', error);
    }
  };

  const sendMessage = async () => {
    if (!newMessage.trim() || !canMessage) return;

    try {
      const response = await fetch('/api/sponsorships/send-message', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          fromUserId: currentUserId,
          toUserId: targetUserId,
          plantAnalysisId: plantAnalysisId,
          message: newMessage,
          messageType: 'Information'
        })
      });

      const result = await response.json();
      
      if (result.success) {
        setNewMessage('');
        fetchMessages(); // Mesajları yenile
      }
    } catch (error) {
      console.error('Mesaj gönderilemedi:', error);
    }
  };

  if (!canMessage) {
    return (
      <div className="messaging-upgrade">
        <p>💬 Çiftçilerle mesajlaşmak için paketinizi yükseltin</p>
        <button className="upgrade-button">Paketi Yükselt</button>
      </div>
    );
  }

  return (
    <div className="sponsor-messaging">
      <div className="messages-list">
        {messages.map(message => (
          <div key={message.id} className={`message ${message.senderRole.toLowerCase()}`}>
            <div className="message-header">
              <span className="sender">{message.senderName}</span>
              <span className="role-badge">{message.senderRole}</span>
              <span className="date">{new Date(message.sentDate).toLocaleString()}</span>
            </div>
            <div className="message-content">
              {message.message}
            </div>
          </div>
        ))}
      </div>
      
      <div className="message-input">
        <textarea
          value={newMessage}
          onChange={(e) => setNewMessage(e.target.value)}
          placeholder="Mesajınızı yazın..."
          rows={3}
        />
        <button onClick={sendMessage} disabled={!newMessage.trim()}>
          Gönder
        </button>
      </div>
    </div>
  );
};
```

### 🎨 CSS Styles
```css
/* Sponsor Logo Styles */
.sponsor-logo-container {
  display: flex;
  align-items: center;
  gap: 10px;
  margin: 15px 0;
  padding: 10px;
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
  border-radius: 8px;
  border-left: 4px solid #4CAF50;
}

.sponsor-logo {
  max-height: 40px;
  max-width: 120px;
  cursor: pointer;
  transition: transform 0.3s ease;
}

.sponsor-logo:hover {
  transform: scale(1.05);
}

.sponsor-text {
  font-size: 12px;
  color: #666;
  font-weight: 500;
}

/* Smart Links Styles */
.smart-links-container {
  margin: 20px 0;
  padding: 20px;
  background: #f8f9fa;
  border-radius: 12px;
  border: 1px solid #e9ecef;
}

.smart-links-container h3 {
  margin-bottom: 15px;
  color: #2c3e50;
  font-weight: 600;
}

.product-card {
  background: white;
  border-radius: 8px;
  padding: 15px;
  margin: 10px 0;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
  cursor: pointer;
  transition: all 0.3s ease;
  position: relative;
  border: 1px solid #e9ecef;
}

.product-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 15px rgba(0,0,0,0.15);
}

.product-info h4 {
  margin: 0 0 8px 0;
  color: #2c3e50;
  font-size: 16px;
  font-weight: 600;
}

.product-info p {
  margin: 0 0 12px 0;
  color: #666;
  font-size: 14px;
}

.price-info {
  display: flex;
  align-items: center;
  gap: 10px;
}

.price {
  font-size: 18px;
  font-weight: bold;
  color: #e74c3c;
}

.discount {
  background: #e74c3c;
  color: white;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 600;
}

.relevance-badge {
  position: absolute;
  top: 10px;
  right: 10px;
  background: #4CAF50;
  color: white;
  padding: 4px 8px;
  border-radius: 12px;
  font-size: 11px;
  font-weight: 600;
}

.smart-link-button {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  border: none;
  padding: 12px 20px;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 600;
  margin: 5px 0;
  display: flex;
  align-items: center;
  gap: 10px;
  transition: all 0.3s ease;
}

.smart-link-button:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
}

.discount-badge {
  background: #ff6b6b;
  padding: 2px 6px;
  border-radius: 10px;
  font-size: 10px;
}

/* Messaging Styles */
.sponsor-messaging {
  max-width: 600px;
  margin: 20px 0;
  border: 1px solid #e9ecef;
  border-radius: 12px;
  overflow: hidden;
}

.messages-list {
  max-height: 400px;
  overflow-y: auto;
  padding: 15px;
  background: #f8f9fa;
}

.message {
  background: white;
  margin: 10px 0;
  padding: 12px;
  border-radius: 8px;
  border-left: 3px solid #dee2e6;
}

.message.sponsor {
  border-left-color: #007bff;
}

.message.farmer {
  border-left-color: #28a745;
}

.message-header {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 8px;
  font-size: 12px;
}

.sender {
  font-weight: 600;
  color: #2c3e50;
}

.role-badge {
  background: #6c757d;
  color: white;
  padding: 2px 6px;
  border-radius: 10px;
  font-size: 10px;
}

.role-badge.sponsor {
  background: #007bff;
}

.role-badge.farmer {
  background: #28a745;
}

.date {
  color: #6c757d;
  margin-left: auto;
}

.message-content {
  color: #495057;
  line-height: 1.5;
}

.message-input {
  padding: 15px;
  background: white;
  border-top: 1px solid #e9ecef;
}

.message-input textarea {
  width: 100%;
  border: 1px solid #ced4da;
  border-radius: 6px;
  padding: 10px;
  resize: vertical;
  font-family: inherit;
}

.message-input button {
  background: #007bff;
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 4px;
  cursor: pointer;
  margin-top: 10px;
  font-weight: 500;
}

.message-input button:disabled {
  background: #6c757d;
  cursor: not-allowed;
}

.messaging-upgrade {
  text-align: center;
  padding: 40px 20px;
  background: #f8f9fa;
  border: 2px dashed #dee2e6;
  border-radius: 12px;
  margin: 20px 0;
}

.upgrade-button {
  background: linear-gradient(135deg, #ff6b6b 0%, #feca57 100%);
  color: white;
  border: none;
  padding: 12px 24px;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  margin-top: 15px;
  transition: all 0.3s ease;
}

.upgrade-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(255, 107, 107, 0.3);
}

/* Responsive Design */
@media (max-width: 768px) {
  .product-card {
    padding: 12px;
  }
  
  .smart-links-container {
    padding: 15px;
  }
  
  .sponsor-messaging {
    margin: 10px 0;
  }
  
  .messages-list {
    max-height: 300px;
    padding: 10px;
  }
}
```

---

## 🧪 Test Senaryoları

### ✅ Unit Tests

#### Sponsor Visibility Service Tests
```csharp
[Test]
public async Task CanShowLogoOnResultScreen_WithSPackage_ShouldReturnTrue()
{
    // Arrange
    var sponsorProfile = new SponsorProfile 
    { 
        SponsorId = 123,
        VisibilityLevel = "ResultOnly",
        IsActive = true,
        IsVerified = true
    };
    
    _sponsorProfileRepository.Setup(x => x.GetBySponsorIdAsync(123))
        .ReturnsAsync(sponsorProfile);
    
    // Act
    var result = await _sponsorVisibilityService.CanShowLogoOnResultScreenAsync(123);
    
    // Assert
    Assert.IsTrue(result);
}

[Test] 
public async Task CanShowLogoOnStartScreen_WithSPackage_ShouldReturnFalse()
{
    // Arrange
    var sponsorProfile = new SponsorProfile 
    { 
        SponsorId = 123,
        VisibilityLevel = "ResultOnly", // S paketi sadece result'ta gösterir
        IsActive = true,
        IsVerified = true
    };
    
    _sponsorProfileRepository.Setup(x => x.GetBySponsorIdAsync(123))
        .ReturnsAsync(sponsorProfile);
    
    // Act
    var result = await _sponsorVisibilityService.CanShowLogoOnStartScreenAsync(123);
    
    // Assert
    Assert.IsFalse(result);
}
```

#### Data Access Service Tests
```csharp
[Test]
public async Task GetFilteredAnalysisData_WithMPackage_ShouldReturn30PercentData()
{
    // Arrange
    var sponsorProfile = new SponsorProfile 
    { 
        SponsorId = 123,
        DataAccessLevel = "Basic30",
        IsActive = true,
        IsVerified = true
    };
    
    var plantAnalysis = new PlantAnalysis
    {
        Id = 456,
        OverallHealthScore = 8,
        PlantSpecies = "Solanum lycopersicum",
        VigorScore = 7, // Bu alan %60 erişimde görünür
        NutrientStatus = "N:low, P:high", // Bu alan %60 erişimde görünür
        ContactEmail = "farmer@test.com" // Bu alan %100 erişimde görünür
    };
    
    _sponsorProfileRepository.Setup(x => x.GetBySponsorIdAsync(123))
        .ReturnsAsync(sponsorProfile);
    _plantAnalysisRepository.Setup(x => x.GetAsync(It.IsAny<Expression<Func<PlantAnalysis, bool>>>()))
        .ReturnsAsync(plantAnalysis);
    
    // Act
    var result = await _sponsorDataAccessService.GetFilteredAnalysisDataAsync(123, 456);
    
    // Assert
    Assert.IsNotNull(result);
    Assert.AreEqual(8, result.OverallHealthScore); // %30 erişimde var
    Assert.AreEqual("Solanum lycopersicum", result.PlantSpecies); // %30 erişimde var
    Assert.IsNull(result.VigorScore); // %30 erişimde yok, sadece %60+'da var
    Assert.IsNull(result.NutrientStatus); // %30 erişimde yok
    Assert.IsNull(result.ContactEmail); // %30 erişimde yok, sadece %100'de var
}
```

#### Smart Link Service Tests
```csharp
[Test]
public async Task CalculateRelevanceScore_WithMatchingKeywords_ShouldReturnHighScore()
{
    // Arrange
    var smartLink = new SmartLink
    {
        Keywords = "[\"azot\", \"gübre\", \"domates\"]",
        TargetCropTypes = "[\"tomato\"]",
        TargetDiseases = "[\"nutrient_deficiency\"]"
    };
    
    var plantAnalysis = new PlantAnalysis
    {
        CropType = "tomato",
        PrimaryConcern = "azot eksikliği gübre gerekiyor",
        PrimaryDeficiency = "nitrogen",
        Diseases = "nutrient_deficiency"
    };
    
    // Act
    var relevanceScore = await _smartLinkService.CalculateRelevanceScoreAsync(smartLink, plantAnalysis);
    
    // Assert
    Assert.IsTrue(relevanceScore >= 70); // High relevance expected
}
```

### 🔄 Integration Tests

#### API Integration Test
```csharp
[Test]
public async Task CreateSponsorProfile_WithValidData_ShouldReturnSuccess()
{
    // Arrange
    var request = new CreateSponsorProfileCommand
    {
        SponsorId = 123,
        CompanyName = "Test Company",
        CurrentSubscriptionTierId = 2, // M Package
        ContactEmail = "test@company.com"
    };
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/sponsorships/create-profile", request);
    var result = await response.Content.ReadAsStringAsync();
    
    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    Assert.IsTrue(result.Contains("success\":true"));
}

[Test]
public async Task GetFilteredAnalysis_WithInvalidSponsor_ShouldReturnUnauthorized()
{
    // Arrange
    var invalidSponsorId = 999;
    var plantAnalysisId = 456;
    
    // Act
    var response = await _client.GetAsync($"/api/sponsorships/filtered-analysis/{invalidSponsorId}/{plantAnalysisId}");
    
    // Assert
    Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

### 📱 E2E Tests (Cypress)

```javascript
describe('Sponsor System E2E Tests', () => {
  beforeEach(() => {
    cy.login('sponsor@test.com', 'password');
  });

  it('Should display sponsor logo on result screen for S package', () => {
    cy.visit('/plant-analysis/results/123');
    cy.get('[data-cy="sponsor-logo"]').should('be.visible');
    cy.get('[data-cy="sponsor-logo"] img').should('have.attr', 'src').and('include', 'logo');
    cy.get('[data-cy="sponsor-text"]').should('contain', 'sponsorluğunda');
  });

  it('Should show filtered data for M package sponsor', () => {
    cy.visit('/sponsor/filtered-analysis/456');
    
    // %30 erişim kontrolü
    cy.get('[data-cy="health-score"]').should('be.visible');
    cy.get('[data-cy="plant-species"]').should('be.visible');
    
    // %60 erişim - görünmemeli
    cy.get('[data-cy="nutrient-details"]').should('not.exist');
    cy.get('[data-cy="contact-info"]').should('not.exist');
  });

  it('Should allow messaging for L package sponsors', () => {
    cy.visit('/sponsor/messaging/789');
    
    cy.get('[data-cy="message-input"]').should('be.visible');
    cy.get('[data-cy="message-input"] textarea').type('Test mesajı gönderiyorum');
    cy.get('[data-cy="send-button"]').click();
    
    cy.get('[data-cy="success-message"]').should('contain', 'başarıyla gönderildi');
    cy.get('[data-cy="messages-list"]').should('contain', 'Test mesajı gönderiyorum');
  });

  it('Should create and display smart links for XL package', () => {
    cy.visit('/sponsor/smart-links');
    
    cy.get('[data-cy="create-link-button"]').click();
    cy.get('[data-cy="link-url"]').type('https://test.com/product');
    cy.get('[data-cy="link-text"]').type('Test Ürün - %20 İndirim');
    cy.get('[data-cy="keywords"]').type('test,ürün,gübre');
    cy.get('[data-cy="target-crops"]').select('tomato');
    cy.get('[data-cy="product-price"]').type('199.99');
    cy.get('[data-cy="discount"]').type('20');
    
    cy.get('[data-cy="create-button"]').click();
    cy.get('[data-cy="success-alert"]').should('contain', 'Akıllı link oluşturuldu');
    
    // Link listesinde görünmeli
    cy.get('[data-cy="smart-links-list"]').should('contain', 'Test Ürün - %20 İndirim');
  });

  it('Should upgrade package restrictions', () => {
    // S paketli sponsor mesajlaşma erişimi yok
    cy.loginAsSponsor('s-package-sponsor@test.com');
    cy.visit('/plant-analysis/results/123');
    
    cy.get('[data-cy="messaging-section"]').should('contain', 'paketinizi yükseltin');
    cy.get('[data-cy="upgrade-button"]').should('be.visible');
  });
});
```

---

## 🚀 Deployment Kılavuzu

### 📋 Deployment Checklist

#### 1. Database Migration
```bash
# Migration çalıştır
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext

# Seed data kontrolü
dotnet script check_sponsor_tables.csx
```

#### 2. Environment Configuration
```json
// appsettings.Production.json
{
  "SponsorshipSettings": {
    "DefaultLogoDisplayDuration": 5000,
    "MaxSmartLinksPerSponsor": 50,
    "MessageModeration": true,
    "AutoApproveVerifiedSponsors": false,
    "SmartLinkApprovalRequired": true,
    "AnalyticsRetentionDays": 365
  },
  "CacheSettings": {
    "SponsorProfileCacheDurationMinutes": 60,
    "SmartLinksCacheDurationMinutes": 30,
    "AnalyticsCacheDurationMinutes": 15
  }
}
```

#### 3. Service Registration
`Business/DependencyResolvers/AutofacBusinessModule.cs` dosyasına ekle:
```csharp
public class AutofacBusinessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Existing registrations...
        
        // Sponsorship Services
        builder.RegisterType<SponsorVisibilityService>().As<ISponsorVisibilityService>().InstancePerLifetimeScope();
        builder.RegisterType<SponsorDataAccessService>().As<ISponsorDataAccessService>().InstancePerLifetimeScope();
        builder.RegisterType<AnalysisMessagingService>().As<IAnalysisMessagingService>().InstancePerLifetimeScope();
        builder.RegisterType<SmartLinkService>().As<ISmartLinkService>().InstancePerLifetimeScope();
    }
}
```

#### 4. Security Configuration
```csharp
// WebAPI/Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Existing configurations...
    
    services.AddAuthorization(options =>
    {
        options.AddPolicy("SponsorOnly", policy => 
            policy.RequireRole("Sponsor"));
        options.AddPolicy("SponsorOrAdmin", policy => 
            policy.RequireRole("Sponsor", "Admin"));
        options.AddPolicy("VerifiedSponsor", policy =>
            policy.RequireRole("Sponsor")
                  .RequireClaim("SponsorVerified", "true"));
    });
}
```

#### 5. Performance Optimizations

**Caching Strategy**:
```csharp
// Business/Services/Sponsorship/CachedSponsorService.cs
public class CachedSponsorVisibilityService : ISponsorVisibilityService
{
    private readonly ISponsorVisibilityService _baseService;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);

    public async Task<bool> CanShowLogoOnResultScreenAsync(int sponsorId)
    {
        var cacheKey = $"sponsor_visibility_{sponsorId}";
        
        if (_cache.TryGetValue(cacheKey, out SponsorProfile cachedProfile))
        {
            return cachedProfile.VisibilityLevel != "None";
        }

        var result = await _baseService.CanShowLogoOnResultScreenAsync(sponsorId);
        _cache.Set(cacheKey, result, _cacheDuration);
        
        return result;
    }
}
```

**Database Indexes**:
```sql
-- Performance için önemli indexler
CREATE INDEX IX_SponsorProfiles_SponsorId ON "SponsorProfiles" ("SponsorId");
CREATE INDEX IX_SponsorProfiles_IsActive_IsVerified ON "SponsorProfiles" ("IsActive", "IsVerified");

CREATE INDEX IX_SponsorAnalysisAccesses_SponsorId_PlantAnalysisId ON "SponsorAnalysisAccesses" ("SponsorId", "PlantAnalysisId");
CREATE INDEX IX_SponsorAnalysisAccesses_AccessLevel ON "SponsorAnalysisAccesses" ("AccessLevel");

CREATE INDEX IX_AnalysisMessages_PlantAnalysisId ON "AnalysisMessages" ("PlantAnalysisId");
CREATE INDEX IX_AnalysisMessages_ToUserId_IsRead ON "AnalysisMessages" ("ToUserId", "IsRead");

CREATE INDEX IX_SmartLinks_SponsorId_IsActive ON "SmartLinks" ("SponsorId", "IsActive");
CREATE INDEX IX_SmartLinks_IsApproved_Priority ON "SmartLinks" ("IsApproved", "Priority");
CREATE INDEX IX_SmartLinks_Keywords_GIN ON "SmartLinks" USING GIN ("Keywords");
```

#### 6. Monitoring & Logging

**Application Insights Configuration**:
```csharp
// WebAPI/Controllers/SponsorshipController.cs
[HttpGet("analytics/{sponsorId}")]
public async Task<IActionResult> GetAnalytics(int sponsorId)
{
    using var activity = Activity.Current?.Source.StartActivity("SponsorAnalytics");
    activity?.SetTag("sponsor.id", sponsorId.ToString());
    
    try 
    {
        var result = await _mediator.Send(new GetSponsorAnalyticsQuery { SponsorId = sponsorId });
        
        _logger.LogInformation("Sponsor analytics requested: {SponsorId}, ResultCount: {Count}", 
            sponsorId, result.Data?.TotalViews);
            
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get sponsor analytics: {SponsorId}", sponsorId);
        throw;
    }
}
```

### 🔧 Production Configuration

#### IIS Deployment
```xml
<!-- web.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" 
                arguments=".\WebAPI.dll" 
                stdoutLogEnabled="true" 
                stdoutLogFile=".\logs\stdout">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        <environmentVariable name="ASPNETCORE_HTTPS_PORT" value="443" />
      </environmentVariables>
    </aspNetCore>
    
    <!-- Static file caching for sponsor logos -->
    <staticContent>
      <clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="30.00:00:00" />
    </staticContent>
    
    <!-- Compression for better performance -->
    <urlCompression doStaticCompression="true" doDynamicCompression="true" />
  </system.webServer>
</configuration>
```

#### Docker Deployment
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["Business/Business.csproj", "Business/"]
COPY ["DataAccess/DataAccess.csproj", "DataAccess/"]
COPY ["Entities/Entities.csproj", "Entities/"]
COPY ["Core/Core.csproj", "Core/"]

RUN dotnet restore "WebAPI/WebAPI.csproj"
COPY . .
WORKDIR "/src/WebAPI"
RUN dotnet build "WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create uploads directory for sponsor logos
RUN mkdir -p /app/wwwroot/uploads/sponsor-logos

ENTRYPOINT ["dotnet", "WebAPI.dll"]
```

#### Health Checks
```csharp
// WebAPI/Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddHealthChecks()
        .AddNpgSql(Configuration.GetConnectionString("DArchPgContext"))
        .AddCheck<SponsorSystemHealthCheck>("sponsor-system");
}

// Health check implementation
public class SponsorSystemHealthCheck : IHealthCheck
{
    private readonly ISponsorProfileRepository _sponsorRepository;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var activeSponsorsCount = await _sponsorRepository.GetCountAsync(s => s.IsActive);
            
            return activeSponsorsCount >= 0 
                ? HealthCheckResult.Healthy($"Sponsor system operational. Active sponsors: {activeSponsorsCount}")
                : HealthCheckResult.Degraded("No active sponsors found");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Sponsor system check failed", ex);
        }
    }
}
```

---

Bu dokümantasyon, ZiraAI Sponsorluk Sistemi'nin tüm teknik detaylarını, entegrasyon kılavuzlarını ve deployment süreçlerini kapsamaktadır. Sistemi üretime almak ve geliştirmek için ihtiyaç duyulan tüm bilgiler mevcuttur.