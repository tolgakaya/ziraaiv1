# 🧪 ZiraAI Sponsorluk Sistemi Test Kılavuzu

## 🎯 Test Stratejisi: S Paketinden XL Paketine Adım Adım

### **Adım 1: S Paketi (Temel Seviye) Test Senaryoları**

#### A. Ortam Hazırlığı
```bash
# Database migration kontrolü
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext

# API'yi başlat
dotnet run --project WebAPI
```

#### B. Authentication ve Sponsor Profili Oluşturma
```bash
# 1. Admin olarak giriş yap
POST https://localhost:5001/api/v1/auth/login
{
  "email": "admin@ziraai.com", 
  "password": "Admin123!"
}
# Token'ı kaydet: eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9...

# 2. Sponsor hesabı oluştur veya mevcut sponsor ile giriş yap
POST https://localhost:5001/api/v1/auth/login
{
  "email": "sponsor@company.com",
  "password": "SponsorPassword123!"
}
```

#### C. S Paketi Sponsor Profili Oluşturma
```bash
POST https://localhost:5001/api/sponsorships/create-profile
Authorization: Bearer {token}
{
  "sponsorId": 101,
  "companyName": "Tarım S Şirketi",
  "companyDescription": "Temel tarım destek hizmetleri",
  "sponsorLogoUrl": "https://example.com/s-logo.png",
  "websiteUrl": "https://tarims.com.tr",
  "contactEmail": "info@tarims.com.tr", 
  "contactPhone": "+90 212 555 01 01",
  "contactPerson": "S Paket Sponsor",
  "currentSubscriptionTierId": 1
}

# Beklenen Response:
{
  "success": true,
  "data": {
    "id": 1,
    "dataAccessLevel": "Basic", // %30 erişim
    "logoVisibilityLevel": "ResultOnly", // Sadece sonuç ekranı
    "messagingEnabled": false, // Mesajlaşma YOK
    "smartLinksEnabled": false // Smart link YOK
  }
}
```

#### D. S Paketi Logo Görünürlük Testi
```bash
# Logo yetkilerini kontrol et
GET https://localhost:5001/api/sponsorships/logo-permissions/101
Authorization: Bearer {token}

# Beklenen Response (S Paketi):
{
  "success": true,
  "data": {
    "sponsorId": 101,
    "canShowOnStart": false, // ❌ Başlangıç ekranında gösterilmez
    "canShowOnResult": true, // ✅ Sadece sonuç ekranında
    "canShowOnAnalysis": false, // ❌ Analiz detayında gösterilmez
    "canShowOnProfile": false, // ❌ Profil sayfasında gösterilmez
    "tierLevel": "S"
  }
}

# Result screen için display info testi
GET https://localhost:5001/api/sponsorships/display-info/456?screen=result
Authorization: Bearer {token}

# Beklenen Response:
{
  "success": true,
  "data": {
    "sponsorLogoUrl": "https://example.com/s-logo.png",
    "companyName": "Tarım S Şirketi",
    "websiteUrl": "https://tarims.com.tr"
  }
}

# Start screen test (başarısız olmalı)
GET https://localhost:5001/api/sponsorships/display-info/456?screen=start
# Beklenen: 403 Forbidden veya null data
```

#### E. S Paketi Veri Erişim Testi (%30 Erişim)
```bash
# Filtrelenmiş analiz verisi al
GET https://localhost:5001/api/sponsorships/filtered-analysis/101/456
Authorization: Bearer {token}

# Beklenen Response (Sadece %30 veri):
{
  "success": true,
  "data": {
    "id": 456,
    "plantType": "domates", // ✅ Temel bilgi
    "overallHealthScore": 7, // ✅ Sağlık skoru
    "cropType": "tomato", // ✅ Temel tür
    "analysisDate": "2025-08-15T10:30:00",
    // ❌ Missing: GPS coordinates, disease details, farmer contact
    // ❌ Missing: Detailed recommendations, stress indicators
    // ❌ Missing: Environmental data, advanced metrics
    "accessLevel": "Basic",
    "dataPercentage": 30
  }
}
```

#### F. S Paketi Kısıtlama Testleri
```bash
# Mesajlaşma test (başarısız olmalı)
POST https://localhost:5001/api/sponsorships/send-message
Authorization: Bearer {token}
{
  "fromUserId": 101,
  "toUserId": 456,
  "message": "Test mesajı"
}

# Beklenen: 403 Forbidden
{
  "success": false,
  "message": "Messaging feature is not available for your current subscription tier (S)."
}

# Smart link test (başarısız olmalı)
GET https://localhost:5001/api/sponsorships/matching-links/456
# Beklenen: 403 Forbidden
```

---

### **Adım 2: M Paketi (Orta Seviye) Test Senaryoları**

#### A. M Paketi Sponsor Profili Oluşturma
```bash
POST https://localhost:5001/api/sponsorships/create-profile
Authorization: Bearer {token}
{
  "sponsorId": 102,
  "companyName": "Tarım M Teknoloji",
  "companyDescription": "Orta düzey tarım çözümleri ve iletişim",
  "sponsorLogoUrl": "https://example.com/m-logo.png",
  "websiteUrl": "https://tarimm.com.tr",
  "contactEmail": "info@tarimm.com.tr",
  "contactPhone": "+90 212 555 02 02",
  "contactPerson": "M Paket Sponsor",
  "currentSubscriptionTierId": 2
}

# Beklenen Response:
{
  "success": true,
  "data": {
    "id": 2,
    "dataAccessLevel": "Basic", // Hala %30 erişim
    "logoVisibilityLevel": "StartAndResult", // Başlangıç + Sonuç
    "messagingEnabled": true, // ✅ Mesajlaşma aktif
    "smartLinksEnabled": false // Smart link hala YOK
  }
}
```

#### B. M Paketi Logo Görünürlük Testi (Gelişmiş)
```bash
# Logo yetkilerini kontrol et
GET https://localhost:5001/api/sponsorships/logo-permissions/102
Authorization: Bearer {token}

# Beklenen Response (M Paketi):
{
  "success": true,
  "data": {
    "sponsorId": 102,
    "canShowOnStart": true, // ✅ Başlangıç ekranında göster
    "canShowOnResult": true, // ✅ Sonuç ekranında göster
    "canShowOnAnalysis": false, // ❌ Analiz detayı yok
    "canShowOnProfile": false, // ❌ Profil sayfası yok
    "tierLevel": "M"
  }
}

# Start screen test (başarılı olmalı)
GET https://localhost:5001/api/sponsorships/display-info/789?screen=start
Authorization: Bearer {token}

# Beklenen Response:
{
  "success": true,
  "data": {
    "sponsorLogoUrl": "https://example.com/m-logo.png",
    "companyName": "Tarım M Teknoloji",
    "websiteUrl": "https://tarimm.com.tr"
  }
}

# Result screen test (başarılı olmalı)
GET https://localhost:5001/api/sponsorships/display-info/789?screen=result
# Yine başarılı olmalı
```

#### C. M Paketi Mesajlaşma Sistemi Testi
```bash
# Mesaj gönder (başarılı olmalı)
POST https://localhost:5001/api/sponsorships/send-message
Authorization: Bearer {token}
{
  "fromUserId": 102,
  "toUserId": 789,
  "plantAnalysisId": 456,
  "message": "Analizinizi inceledik. Önerilerimiz var.",
  "messageType": "Information"
}

# Beklenen Response:
{
  "success": true,
  "data": {
    "id": 1,
    "messageId": "MSG_20250815_001",
    "sentDate": "2025-08-15T14:30:00Z",
    "readStatus": false
  }
}

# Mesaj geçmişini görüntüle
GET https://localhost:5001/api/sponsorships/conversation/102/789/456
Authorization: Bearer {token}

# Beklenen Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "senderName": "Tarım M Teknoloji",
      "senderRole": "Sponsor",
      "message": "Analizinizi inceledik. Önerilerimiz var.",
      "sentDate": "2025-08-15T14:30:00Z",
      "isRead": false,
      "messageType": "Information"
    }
  ],
  "conversationStats": {
    "totalMessages": 1,
    "unreadCount": 1
  }
}

# Çiftçi yanıtını simüle et
POST https://localhost:5001/api/sponsorships/send-message
Authorization: Bearer {farmer_token}
{
  "fromUserId": 789,
  "toUserId": 102,
  "plantAnalysisId": 456,
  "message": "Teşekkürler, daha fazla bilgi alabilir miyim?",
  "messageType": "Question"
}
```

#### D. M Paketi Veri Erişim Testi (Hala %30)
```bash
# Veri erişimi S paketi ile aynı olmalı (%30)
GET https://localhost:5001/api/sponsorships/filtered-analysis/102/456
Authorization: Bearer {token}

# M paketi için aynı %30 veri kısıtlaması
{
  "success": true,
  "data": {
    "accessLevel": "Basic",
    "dataPercentage": 30,
    // Sadece temel veriler
  }
}

# Erişim istatistikleri
GET https://localhost:5001/api/sponsorships/access-statistics/102
Authorization: Bearer {token}

# Beklenen:
{
  "success": true,
  "data": {
    "totalAccess": 5,
    "thisMonthAccess": 3,
    "averageDataAccessed": 30.0,
    "accessTrends": [...],
    "messagingSummary": {
      "totalMessagesSent": 1,
      "totalMessagesReceived": 1,
      "activeConversations": 1
    }
  }
}
```

---

### **Adım 3: L Paketi (İleri Seviye) Test Senaryoları**

#### A. L Paketi Sponsor Profili Oluşturma
```bash
POST https://localhost:5001/api/sponsorships/create-profile
Authorization: Bearer {token}
{
  "sponsorId": 103,
  "companyName": "Tarım L Premium",
  "companyDescription": "İleri düzey tarım analizi ve tam mesajlaşma",
  "sponsorLogoUrl": "https://example.com/l-logo.png",
  "websiteUrl": "https://tariml.com.tr",
  "contactEmail": "info@tariml.com.tr",
  "contactPhone": "+90 212 555 03 03",
  "contactPerson": "L Paket Sponsor",
  "currentSubscriptionTierId": 3
}

# Beklenen Response:
{
  "success": true,
  "data": {
    "id": 3,
    "dataAccessLevel": "Intermediate", // %60 erişim
    "logoVisibilityLevel": "AllScreens", // Tüm ekranlar
    "messagingEnabled": true, // ✅ Tam mesajlaşma
    "smartLinksEnabled": false // Smart link hala YOK
  }
}
```

#### B. L Paketi Logo Görünürlük Testi (Tam Erişim)
```bash
# Logo yetkilerini kontrol et
GET https://localhost:5001/api/sponsorships/logo-permissions/103
Authorization: Bearer {token}

# Beklenen Response (L Paketi):
{
  "success": true,
  "data": {
    "sponsorId": 103,
    "canShowOnStart": true, // ✅ Başlangıç
    "canShowOnResult": true, // ✅ Sonuç
    "canShowOnAnalysis": true, // ✅ Analiz detayı
    "canShowOnProfile": true, // ✅ Profil sayfası
    "tierLevel": "L"
  }
}

# Tüm ekranları test et
GET https://localhost:5001/api/sponsorships/display-info/999?screen=start
GET https://localhost:5001/api/sponsorships/display-info/999?screen=result
GET https://localhost:5001/api/sponsorships/display-info/999?screen=analysis
GET https://localhost:5001/api/sponsorships/display-info/999?screen=profile

# Hepsi başarılı olmalı ve aynı sponsor bilgilerini döndürmeli
```

#### C. L Paketi Veri Erişim Testi (%60 Erişim)
```bash
# Daha kapsamlı analiz verisi al
GET https://localhost:5001/api/sponsorships/filtered-analysis/103/999
Authorization: Bearer {token}

# Beklenen Response (%60 veri):
{
  "success": true,
  "data": {
    "id": 999,
    "plantType": "domates", // ✅ Temel bilgi
    "overallHealthScore": 8, // ✅ Sağlık skoru
    "cropType": "tomato", // ✅ Temel tür
    "analysisDate": "2025-08-15T10:30:00",
    
    // ✅ L Paketi ile eklenenler (%60 seviye):
    "diseaseSymptoms": ["leaf_spot", "yellowing"], // ✅ Hastalık detayları
    "pestInfo": ["aphids"], // ✅ Zararlı bilgisi
    "elementDeficiencies": ["nitrogen", "potassium"], // ✅ Besin eksiklikleri
    "weatherConditions": "sunny, 25°C", // ✅ Hava durumu
    "soilConditions": "pH: 6.5, moisture: 70%", // ✅ Toprak durumu
    "recommendations": [ // ✅ Öneriler
      "Apply nitrogen fertilizer",
      "Monitor for pest activity"
    ],
    
    // ❌ Hala eksik olanlar (%40):
    // GPS coordinates, farmer contact info
    // Advanced soil analysis, detailed environmental data
    
    "accessLevel": "Intermediate",
    "dataPercentage": 60
  }
}
```

#### D. L Paketi Gelişmiş Mesajlaşma Testi
```bash
# Farklı mesaj türleri test et
POST https://localhost:5001/api/sponsorships/send-message
Authorization: Bearer {token}
{
  "fromUserId": 103,
  "toUserId": 999,
  "plantAnalysisId": 555,
  "message": "Analizinizde azot eksikliği tespit edildi. Özel gübre önerimiz var.",
  "messageType": "Recommendation",
  "priority": "High",
  "attachmentUrls": ["https://example.com/nitrogen-guide.pdf"]
}

# File attachment test
POST https://localhost:5001/api/sponsorships/send-message
{
  "fromUserId": 103,
  "toUserId": 999,
  "plantAnalysisId": 555,
  "message": "Ekteki rehberi inceleyebilirsiniz.",
  "messageType": "Documentation",
  "attachmentUrls": [
    "https://example.com/care-guide.pdf",
    "https://example.com/fertilizer-chart.png"
  ]
}

# Bulk messaging test (L paketi özelliği)
POST https://localhost:5001/api/sponsorships/send-bulk-message
Authorization: Bearer {token}
{
  "sponsorId": 103,
  "targetUserIds": [999, 888, 777],
  "message": "Yeni sezon için özel indirim fırsatımız başladı!",
  "messageType": "Promotion",
  "plantAnalysisIds": [555, 444, 333]
}
```

#### E. L Paketi Analytics Testi
```bash
# Detaylı analytics
GET https://localhost:5001/api/sponsorships/detailed-analytics/103
Authorization: Bearer {token}

# Beklenen Response:
{
  "success": true,
  "data": {
    "dataAccess": {
      "totalAccess": 25,
      "averageAccessPercentage": 60.0,
      "dataCategories": {
        "basicInfo": 100,
        "healthMetrics": 100, 
        "diseaseAnalysis": 80,
        "recommendations": 75,
        "environmentalData": 45
      }
    },
    "messaging": {
      "totalMessagesSent": 15,
      "totalMessagesReceived": 12,
      "activeConversations": 8,
      "messageTypes": {
        "Information": 6,
        "Recommendation": 4,
        "Documentation": 3,
        "Promotion": 2
      },
      "responseRate": 80.0,
      "averageResponseTime": "2.5 hours"
    },
    "logoVisibility": {
      "totalImpressions": 150,
      "screenBreakdown": {
        "start": 50,
        "result": 50,
        "analysis": 30,
        "profile": 20
      },
      "clickThroughRate": 15.5
    }
  }
}
```

---

### **Adım 4: XL Paketi (Premium Seviye) Test Senaryoları**

#### A. XL Paketi Sponsor Profili Oluşturma
```bash
POST https://localhost:5001/api/sponsorships/create-profile
Authorization: Bearer {token}
{
  "sponsorId": 104,
  "companyName": "Tarım XL Enterprise",
  "companyDescription": "Premium tarım teknolojileri ve AI destekli çözümler",
  "sponsorLogoUrl": "https://example.com/xl-logo.png",
  "websiteUrl": "https://tarimxl.com.tr",
  "contactEmail": "info@tarimxl.com.tr",
  "contactPhone": "+90 212 555 04 04",
  "contactPerson": "XL Paket Sponsor",
  "currentSubscriptionTierId": 4
}

# Beklenen Response:
{
  "success": true,
  "data": {
    "id": 4,
    "dataAccessLevel": "Full", // %100 tam erişim
    "logoVisibilityLevel": "AllScreens", // Tüm ekranlar
    "messagingEnabled": true, // ✅ Tam mesajlaşma
    "smartLinksEnabled": true // ✅ AI Smart Links aktif
  }
}
```

#### B. XL Paketi Full Veri Erişim Testi (%100)
```bash
# Tam analiz verisi al
GET https://localhost:5001/api/sponsorships/filtered-analysis/104/123
Authorization: Bearer {token}

# Beklenen Response (%100 tam veri):
{
  "success": true,
  "data": {
    "id": 123,
    "plantType": "domates", // ✅ Temel bilgi
    "overallHealthScore": 9, // ✅ Sağlık skoru
    "cropType": "tomato", // ✅ Temel tür
    "analysisDate": "2025-08-15T10:30:00",
    
    // ✅ Tüm detaylar (%100 erişim):
    "diseaseSymptoms": ["leaf_spot", "yellowing"],
    "pestInfo": ["aphids", "whiteflies"],
    "elementDeficiencies": ["nitrogen", "potassium", "phosphorus"],
    "weatherConditions": "sunny, 25°C, humidity 65%",
    "soilConditions": "pH: 6.5, moisture: 70%, organic matter: 3.2%",
    "recommendations": [
      "Apply nitrogen fertilizer (20-10-10) at 150kg/ha",
      "Monitor for pest activity every 3 days",
      "Adjust irrigation schedule"
    ],
    
    // ✅ XL Paketi özel veriler:
    "farmerContactInfo": { // ✅ Çiftçi iletişim bilgileri
      "name": "Ahmet Çiftçi",
      "phone": "+90 532 555 12 34",
      "email": "ahmet@example.com"
    },
    "gpsCoordinates": { // ✅ GPS koordinatları
      "latitude": 39.9208,
      "longitude": 32.8541,
      "altitude": 850
    },
    "fieldMetadata": { // ✅ Arazi detayları
      "fieldSize": "2.5 hectares",
      "plantingDate": "2025-06-15",
      "expectedHarvestDate": "2025-09-30",
      "previousCrops": ["wheat", "barley"]
    },
    "advancedAnalysis": { // ✅ İleri analizler
      "leafAreaIndex": 3.2,
      "chlorophyllContent": 45.6,
      "stomatalConductance": 0.25,
      "waterStressIndex": 0.15
    },
    
    "accessLevel": "Full",
    "dataPercentage": 100
  }
}
```

#### C. XL Paketi Smart Links Sistemi Testi
```bash
# Smart link oluştur
POST https://localhost:5001/api/sponsorships/create-smart-link
Authorization: Bearer {token}
{
  "sponsorId": 104,
  "linkUrl": "https://tarimxl.com.tr/azot-gubresi-special",
  "linkText": "TarımXL Azot Plus Gübresi - %30 İndirimli!",
  "keywords": ["azot", "gübre", "domates", "nitrogen", "fertilizer"],
  "targetCropTypes": ["tomato", "pepper", "eggplant"],
  "targetDiseases": ["leaf_yellowing", "nutrient_deficiency"],
  "productName": "TarımXL Azot Plus Premium",
  "productCategory": "Fertilizer",
  "productPrice": 299.99,
  "discountPercentage": 30,
  "priority": 1,
  "isActive": true
}

# Beklenen Response:
{
  "success": true,
  "data": {
    "id": 1,
    "linkId": "SL_20250815_001",
    "sponsorId": 104,
    "linkUrl": "https://tarimxl.com.tr/azot-gubresi-special",
    "trackingUrl": "https://localhost:5001/api/sponsorships/track-click/1",
    "isActive": true,
    "createdDate": "2025-08-15T15:30:00Z"
  }
}

# Daha fazla smart link oluştur
POST https://localhost:5001/api/sponsorships/create-smart-link
{
  "sponsorId": 104,
  "linkUrl": "https://tarimxl.com.tr/pestisit-organik",
  "linkText": "Organik Pestisit Çözümü - Zararsız ve Etkili",
  "keywords": ["aphid", "pest", "organic", "zararlı", "doğal"],
  "targetCropTypes": ["tomato"],
  "targetPests": ["aphids", "whiteflies"],
  "productName": "TarımXL BioPest Organik",
  "productPrice": 159.99,
  "priority": 2
}

# AI eşleştirme test et - Azot eksikliği olan analiz için
GET https://localhost:5001/api/sponsorships/matching-links/123
Authorization: Bearer {token}

# Beklenen Response (AI relevance score ile sıralı):
{
  "success": true,
  "data": [
    {
      "id": 1,
      "linkText": "TarımXL Azot Plus Gübresi - %30 İndirimli!",
      "productName": "TarımXL Azot Plus Premium",
      "linkUrl": "https://tarimxl.com.tr/azot-gubresi-special",
      "productPrice": 299.99,
      "discountPercentage": 30,
      "relevanceScore": 95.5, // AI calculated
      "matchingReasons": [
        "Nitrogen deficiency detected in analysis",
        "Crop type match: tomato",
        "High-priority sponsored product"
      ]
    },
    {
      "id": 2,
      "linkText": "Organik Pestisit Çözümü - Zararsız ve Etkili",
      "productName": "TarımXL BioPest Organik", 
      "linkUrl": "https://tarimxl.com.tr/pestisit-organik",
      "productPrice": 159.99,
      "relevanceScore": 78.2,
      "matchingReasons": [
        "Aphid infestation detected",
        "Organic solution preferred"
      ]
    }
  ]
}

# Smart link tıklama kaydı
POST https://localhost:5001/api/sponsorships/track-click
{
  "smartLinkId": 1,
  "userId": 123,
  "plantAnalysisId": 123
}

# Smart link conversion kaydı
POST https://localhost:5001/api/sponsorships/track-conversion
{
  "smartLinkId": 1,
  "userId": 123,
  "purchaseAmount": 209.99 // İndirimli fiyat
}
```

#### D. XL Paketi Performance Analytics
```bash
# Smart link performans analizi
GET https://localhost:5001/api/sponsorships/smart-link-analytics/104
Authorization: Bearer {token}

# Beklenen Response:
{
  "success": true,
  "data": {
    "totalLinks": 2,
    "totalDisplays": 150,
    "totalClicks": 23,
    "totalConversions": 4,
    "overallCTR": 15.33,
    "overallConversionRate": 17.39,
    "totalRevenue": 839.96,
    "averageOrderValue": 209.99,
    "topPerformingLinks": [
      {
        "id": 1,
        "productName": "TarımXL Azot Plus Premium",
        "displayCount": 95,
        "clickCount": 18,
        "conversionCount": 3,
        "ctr": 18.95,
        "conversionRate": 16.67,
        "revenue": 629.97,
        "lastClickDate": "2025-08-15T16:45:00Z"
      }
    ],
    "weeklyTrends": {
      "clicks": [5, 8, 12, 18, 23],
      "conversions": [0, 1, 1, 3, 4]
    }
  }
}

# Premium seviye tam analytics
GET https://localhost:5001/api/sponsorships/premium-analytics/104
Authorization: Bearer {token}

# Comprehensive enterprise metrics
{
  "success": true,
  "data": {
    "overview": {
      "totalInvestment": 5000.00,
      "totalReturn": 12750.50,
      "roi": 155.01,
      "costPerAcquisition": 125.50,
      "customerLifetimeValue": 875.25
    },
    "dataAccessMetrics": {
      "uniqueFarmersReached": 85,
      "averageDataAccess": 100.0,
      "premiumDataUsage": {
        "contactInfoAccess": 65,
        "gpsDataAccess": 42,
        "advancedMetrics": 38
      }
    },
    "engagementMetrics": {
      "logoImpressions": 2850,
      "messageEngagementRate": 85.5,
      "smartLinkEngagementRate": 15.33,
      "brandAwarenessScore": 78.5
    }
  }
}
```

---

### **Adım 5: Error Handling ve Edge Case Test Senaryoları**

#### A. Authorization Test Senaryoları
```bash
# 1. Yetkisiz erişim testi
GET https://localhost:5001/api/sponsorships/logo-permissions/104
# Token olmadan - Beklenen: 401 Unauthorized

# 2. Yanlış tier erişimi testi  
GET https://localhost:5001/api/sponsorships/matching-links/123
Authorization: Bearer {s_tier_token}
# S tier ile smart link erişimi - Beklenen: 403 Forbidden

# 3. Başkasının verilerine erişim testi
GET https://localhost:5001/api/sponsorships/filtered-analysis/999/123
Authorization: Bearer {sponsor_104_token}
# Sponsor 104'ün sponsor 999'un verilerine erişimi - Beklenen: 403 Forbidden
```

#### B. Data Validation Test Senaryoları
```bash
# 1. Geçersiz sponsor ID
POST https://localhost:5001/api/sponsorships/create-profile
{
  "sponsorId": -1, // Geçersiz ID
  "companyName": ""  // Boş company name
}
# Beklenen: 400 Bad Request with validation errors

# 2. Smart link geçersiz URL
POST https://localhost:5001/api/sponsorships/create-smart-link
{
  "sponsorId": 104,
  "linkUrl": "invalid-url", // Geçersiz URL format
  "keywords": [] // Boş keywords array
}
# Beklenen: 400 Bad Request

# 3. Mesajlaşma geçersiz data
POST https://localhost:5001/api/sponsorships/send-message
{
  "fromUserId": 104,
  "toUserId": 104, // Kendisine mesaj
  "message": "" // Boş mesaj
}
# Beklenen: 400 Bad Request
```

#### C. Rate Limiting ve Performance Test
```bash
# Rapid API calls test (100 calls in 1 minute)
for i in {1..100}; do
  curl -H "Authorization: Bearer {token}" \
       https://localhost:5001/api/sponsorships/logo-permissions/104 &
done

# Rate limiting response beklenen:
{
  "success": false,
  "message": "Rate limit exceeded. Try again later.",
  "retryAfter": 60
}
```

#### D. Database Connection ve Service Failure Tests
```bash
# Database bağlantı kesintisi simülasyonu
# (PostgreSQL servisini geçici durdur)

GET https://localhost:5001/api/sponsorships/filtered-analysis/104/123
# Beklenen: 503 Service Unavailable

{
  "success": false,
  "message": "Service temporarily unavailable. Please try again later.",
  "errorCode": "DATABASE_CONNECTION_ERROR"
}
```

---

## 🎯 Test Sonuçları Doğrulama Checklist

### S Paketi ✅
- [ ] Logo sadece result screen'de görünür
- [ ] %30 veri erişimi (temel bilgiler)
- [ ] Mesajlaşma devre dışı (403 error)
- [ ] Smart link devre dışı (403 error)

### M Paketi ✅  
- [ ] Logo start + result screen'lerde görünür
- [ ] %30 veri erişimi (S ile aynı)
- [ ] Mesajlaşma aktif ve çalışıyor
- [ ] Smart link hala devre dışı

### L Paketi ✅
- [ ] Logo tüm ekranlarda görünür
- [ ] %60 veri erişimi (hastalık, tavsiye detayları)
- [ ] Gelişmiş mesajlaşma (attachment, bulk)
- [ ] Analytics daha detaylı

### XL Paketi ✅
- [ ] Tüm özellikler aktif
- [ ] %100 tam veri erişimi (GPS, iletişim bilgileri)
- [ ] AI Smart Links çalışıyor
- [ ] Premium analytics mevcut
- [ ] ROI ve conversion tracking aktif

---

## 📊 Beklenen Test Sonuçları Özeti

| Özellik | S | M | L | XL |
|---------|---|---|---|----| 
| Logo - Start | ❌ | ✅ | ✅ | ✅ |
| Logo - Result | ✅ | ✅ | ✅ | ✅ |
| Logo - Analysis | ❌ | ❌ | ✅ | ✅ |
| Logo - Profile | ❌ | ❌ | ✅ | ✅ |
| Veri Erişimi | 30% | 30% | 60% | 100% |
| Mesajlaşma | ❌ | ✅ | ✅ | ✅ |
| Smart Links | ❌ | ❌ | ❌ | ✅ |
| Analytics | Temel | Temel | Detaylı | Premium |

---

## 🔧 Postman Kullanımı

### Environment Setup
```json
{
  "baseUrl": "https://localhost:5001",
  "accessToken": "", // Login sonrası otomatik doldurulur
  "sponsorId": "", // Sponsor login sonrası otomatik doldurulur
  "currentTier": "" // Tier bilgisi
}
```

### Test Collection Import
1. [ZiraAI_Postman_Collection_v1.4.0.json](./ZiraAI_Postman_Collection_v1.4.0.json) dosyasını Postman'e import edin
2. Environment variables ayarlayın
3. "Authentication" folder'dan başlayarak testleri sırayla çalıştırın

---

## 🚀 Hızlı Test Script'i

```bash
#!/bin/bash

# Sponsorship System Test Runner
echo "🧪 ZiraAI Sponsorship System Test Starting..."

# Test S Package
echo "Testing S Package..."
curl -X POST "https://localhost:5001/api/sponsorships/create-profile" \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d @s_package_data.json

# Test M Package
echo "Testing M Package..."
curl -X POST "https://localhost:5001/api/sponsorships/create-profile" \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d @m_package_data.json

# ... diğer testler

echo "✅ All tests completed. Check results above."
```

---

**🎉 Bu kapsamlı test kılavuzu ile sponsorluk sisteminin tüm özelliklerini adım adım doğrulayabilirsiniz!**