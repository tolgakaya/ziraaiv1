# 🧪 ZiraAI Sponsorluk Sistemi Test Kılavuzu (v2.0 - Corrected Business Model)

## 🎯 Test Stratejisi: Düzeltilmiş Purchase-Based Tier Erişim Modeli

### **🔄 YENİ İŞ MODELİ ÖZETİ**
- **TEK Company Profile**: Sponsor tek profil oluşturur
- **Çoklu Paket Satın Alma**: Aynı sponsor S/M/L/XL paketlerini ayrı ayrı satın alabilir
- **Kod Bazlı Tier Erişimi**: Her kod hangi paketten alındıysa o tier'ın özelliklerini taşır
- **Farmer Normal Analiz**: Farmer kod kullanmaz, subscription üzerinden analiz yapar

### **Adım 1: S Paketi (Temel Seviye) Test Senaryoları**

#### A. Ortam Hazırlığı
```bash
# Database migration kontrolü
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext

# API'yi başlat
dotnet run --project WebAPI
```

#### B. Sponsor Hesabı Oluşturma ve Giriş
```bash
# 1. Yeni sponsor hesabı oluştur
POST https://localhost:5001/api/v1/auth/register
{
  "email": "sponsor@agricompany.com",
  "password": "Sponsor123!",
  "firstName": "Ali",
  "lastName": "Sponsor",
  "role": "Sponsor"
}

# 2. Sponsor olarak giriş yap
POST https://localhost:5001/api/v1/auth/login
{
  "email": "sponsor@agricompany.com",
  "password": "Sponsor123!"
}
# Token'ı kaydet
```

#### C. TEK Company Profile Oluşturma (Tier Bağımsız)
```bash
# Company profile oluştur (tier bilgisi YOK)
POST https://localhost:5001/api/sponsorships/create-profile
Authorization: Bearer {sponsor_token}
{
  "companyName": "Tarım Teknoloji A.Ş.",
  "companyDescription": "Modern tarım çözümleri ve danışmanlık hizmetleri",
  "sponsorLogoUrl": "https://example.com/company-logo.png",
  "websiteUrl": "https://tarimteknoloji.com.tr",
  "contactEmail": "info@tarimteknoloji.com.tr", 
  "contactPhone": "+90 212 555 01 01",
  "contactPerson": "Ali Sponsor",
  "companyType": "Cooperative",
  "businessModel": "B2B"
}

# Beklenen Response:
{
  "success": true,
  "message": "Sponsor profile created successfully",
  "data": {
    "id": 1,
    "userId": 123,
    "companyName": "Tarım Teknoloji A.Ş.",
    "companyType": "Cooperative",
    "businessModel": "B2B",
    "isActive": true,
    "totalPurchases": 0,
    "totalCodesGenerated": 0,
    "totalCodesRedeemed": 0
  }
}
```

#### D. S Paketi Satın Alma ve Kod Üretimi
```bash
# S paketi satın al (5 adet kod üretilecek)
POST https://localhost:5001/api/sponsorships/purchase-package
Authorization: Bearer {sponsor_token}
{
  "subscriptionTierId": 2, // S paketi ID (database'de 2)
  "quantity": 5,
  "paymentMethod": "CreditCard",
  "paymentReference": "PAY_S_20250816_001",
  "validityDays": 365
}

# Beklenen Response:
{
  "success": true,
  "data": {
    "purchaseId": 1,
    "sponsorId": 1,
    "tierName": "S",
    "quantity": 5,
    "amount": 499.95,
    "codePrefix": "SPT001",
    "generatedCodes": [
      {
        "code": "SPT001-ABC123",
        "tierName": "S",
        "expiryDate": "2026-08-16T00:00:00"
      },
      {
        "code": "SPT001-DEF456",
        "tierName": "S",
        "expiryDate": "2026-08-16T00:00:00"
      }
      // ... 5 kod
    ],
    "tierFeatures": {
      "dailyLimit": 5,
      "monthlyLimit": 50,
      "prioritySupport": false,
      "advancedAnalytics": false,
      "apiAccess": false
    }
  }
}
```

#### E. Farmer Kod Kullanımı (Subscription Oluşturma)
```bash
# 1. Farmer hesabı oluştur
POST https://localhost:5001/api/v1/auth/register
{
  "email": "farmer1@example.com",
  "password": "Farmer123!",
  "firstName": "Mehmet",
  "lastName": "Çiftçi",
  "role": "Farmer"
}

# 2. Farmer olarak giriş
POST https://localhost:5001/api/v1/auth/login
{
  "email": "farmer1@example.com",
  "password": "Farmer123!"
}

# 3. Sponsorship kodunu kullanarak subscription oluştur
POST https://localhost:5001/api/subscriptions/redeem-code
Authorization: Bearer {farmer_token}
{
  "sponsorshipCode": "SPT001-ABC123"
}

# Beklenen Response:
{
  "success": true,
  "message": "Sponsorship code redeemed successfully. You now have an S tier subscription.",
  "data": {
    "subscriptionId": 1,
    "tierName": "S",
    "dailyLimit": 5,
    "monthlyLimit": 50,
    "startDate": "2025-08-16T00:00:00",
    "endDate": "2026-08-16T00:00:00",
    "sponsorInfo": {
      "companyName": "Tarım Teknoloji A.Ş.",
      "sponsorId": 1
    }
  }
}
```

#### F. Farmer Normal Analiz Yapması (Subscription ile)
```bash
# Farmer artık subscription'ı olduğu için normal analiz yapabilir
POST https://localhost:5001/api/v1/plantanalyses/analyze
Authorization: Bearer {farmer_token}
{
  "image": "data:image/jpeg;base64,/9j/4AAQ...",
  "farmerId": "F001",
  "cropType": "tomato"
  // NOT: sponsorshipCode GEREKMİYOR, subscription üzerinden çalışır
}

# Beklenen Response:
{
  "success": true,
  "data": {
    "id": 456,
    "analysisId": "ANALYSIS_20250816_001",
    "sponsorUserId": 123, // Sponsorun user ID'si otomatik kaydedilir
    "sponsorshipCodeId": 1, // Kullanılan kodun ID'si
    "status": "Completed",
    "overallHealthScore": 8,
    "subscriptionInfo": {
      "tierName": "S",
      "dailyUsed": 1,
      "dailyLimit": 5,
      "monthlyUsed": 1,
      "monthlyLimit": 50
    }
  }
}

# Result screen için display info testi (YENİ ENDPOINT)
GET https://localhost:5001/api/sponsorships/display-info/analysis/456?screen=result
Authorization: Bearer {token}

# Beklenen Response:
{
  "success": true,
  "data": {
    "sponsorLogoUrl": "https://example.com/company-logo.png",
    "companyName": "Tarım Teknoloji A.Ş.",
    "websiteUrl": "https://tarimteknoloji.com.tr",
    "tierName": "S",
    "canDisplay": true
  }
}

# Start screen test (başarısız olmalı)
GET https://localhost:5001/api/sponsorships/display-info/analysis/456?screen=start
# Beklenen: canDisplay = false çünkü S paketi start screen'de logo gösteremez
{
  "success": true,
  "data": {
    "canDisplay": false,
    "tierName": "S",
    "reason": "S tier cannot display logo on start screen"
  }
}
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

#### A. M Paketi Satın Alma (Aynı Sponsor Firması)
```bash
# Aynı sponsor M paketi de satın alabilir (farklı müşteri grubu için)
POST https://localhost:5001/api/sponsorships/purchase-package
Authorization: Bearer {sponsor_token}
{
  "subscriptionTierId": 3, // M paketi ID (database'de 3)
  "quantity": 10,
  "paymentMethod": "BankTransfer",
  "paymentReference": "PAY_M_20250816_002",
  "validityDays": 365
}

# Beklenen Response:
{
  "success": true,
  "data": {
    "purchaseId": 2,
    "sponsorId": 1,
    "tierName": "M",
    "quantity": 10,
    "amount": 2999.90,
    "codePrefix": "SPT002",
    "generatedCodes": [
      {
        "code": "SPT002-GHI789",
        "tierName": "M",
        "expiryDate": "2026-08-16T00:00:00"
      }
      // ... 10 kod
    ],
    "tierFeatures": {
      "dailyLimit": 20,
      "monthlyLimit": 200,
      "prioritySupport": false,
      "advancedAnalytics": false,
      "apiAccess": true
    }
  }
}
```

#### B. Farmer M Paketi Kodu Kullanımı
```bash
# Yeni farmer M paketi kodunu kullanır
POST https://localhost:5001/api/subscriptions/redeem-code
Authorization: Bearer {farmer2_token}
{
  "sponsorshipCode": "SPT002-GHI789"
}

# Beklenen Response:
{
  "success": true,
  "message": "Sponsorship code redeemed successfully. You now have an M tier subscription.",
  "data": {
    "subscriptionId": 2,
    "tierName": "M",
    "dailyLimit": 20,
    "monthlyLimit": 200,
    "startDate": "2025-08-16T00:00:00",
    "endDate": "2026-08-16T00:00:00"
  }
}
```

#### C. M Paketi Analiz ve Sponsor Bilgisi Testi
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

#### A. L Paketi Satın Alma (Aynı veya Farklı Sponsor)
```bash
# L paketi satın al (premium farmers için)
POST https://localhost:5001/api/sponsorships/purchase-package
Authorization: Bearer {sponsor_token}
{
  "subscriptionTierId": 4, // L paketi ID (database'de 4)
  "quantity": 3,
  "paymentMethod": "CreditCard",
  "paymentReference": "PAY_L_20250816_003",
  "validityDays": 365
}

# Beklenen Response:
{
  "success": true,
  "data": {
    "purchaseId": 3,
    "sponsorId": 1,
    "tierName": "L",
    "quantity": 3,
    "amount": 1799.97,
    "codePrefix": "SPT003",
    "generatedCodes": [
      {
        "code": "SPT003-JKL012",
        "tierName": "L",
        "expiryDate": "2026-08-16T00:00:00"
      }
      // ... 3 kod
    ],
    "tierFeatures": {
      "dailyLimit": 50,
      "monthlyLimit": 500,
      "prioritySupport": true,
      "advancedAnalytics": true,
      "apiAccess": true
    }
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

#### A. XL Paketi Satın Alma (Enterprise Level)
```bash
# XL paketi satın al (enterprise farmers için)
POST https://localhost:5001/api/sponsorships/purchase-package
Authorization: Bearer {sponsor_token}
{
  "subscriptionTierId": 5, // XL paketi ID (database'de 5)
  "quantity": 2,
  "paymentMethod": "CorporateInvoice",
  "paymentReference": "PAY_XL_20250816_004",
  "validityDays": 365
}

# Beklenen Response:
{
  "success": true,
  "data": {
    "purchaseId": 4,
    "sponsorId": 1,
    "tierName": "XL",
    "quantity": 2,
    "amount": 2999.98,
    "codePrefix": "SPT004",
    "generatedCodes": [
      {
        "code": "SPT004-MNO345",
        "tierName": "XL",
        "expiryDate": "2026-08-16T00:00:00"
      },
      {
        "code": "SPT004-PQR678",
        "tierName": "XL",
        "expiryDate": "2026-08-16T00:00:00"
      }
    ],
    "tierFeatures": {
      "dailyLimit": 200,
      "monthlyLimit": 2000,
      "prioritySupport": true,
      "advancedAnalytics": true,
      "apiAccess": true,
      "responseTimeHours": 1
    }
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

## 📊 Beklenen Test Sonuçları Özeti (YENİ ARCHITECTURE)

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

## 🏗️ **YENİ ARCHITECTURE (v2.0 - AUGUST 2025)**

### ✅ **DOĞRU İŞ AKIŞI (Purchase-Based Model):**
1. **Sponsor Registration**: Sponsor rolü ile kayıt
2. **Company Profile**: TEK profil oluşturma (tier bağımsız)
3. **Package Purchase**: S/M/L/XL paketlerini ihtiyaca göre satın alma
4. **Code Generation**: Her satın alma için otomatik kod üretimi
5. **Code Distribution**: Sponsor kodları farmers'a dağıtır (offline)
6. **Code Redemption**: Farmer kodu kullanarak subscription oluşturur
7. **Normal Analysis**: Farmer subscription limitleri ile analiz yapar
8. **Sponsor Tracking**: Analiz otomatik sponsor ile ilişkilendirilir

### ❌ **ESKİ YANLIŞ MODEL (Kaldırıldı):**
- ~~Her tier için ayrı sponsor profile~~
- ~~Profile'a bağlı tier özellikleri~~
- ~~Analiz payload'ında sponsorshipCode~~
- ~~Direct tier-to-profile coupling~~

### 🔧 **YENİ API ENDPOINTS:**

#### Sponsor Management
- `POST /api/sponsorships/create-profile` - Company profile (tier-independent)
- `PUT /api/sponsorships/update-profile/{id}` - Profile güncelleme
- `GET /api/sponsorships/my-profile` - Sponsor profil görüntüleme

#### Package & Code Management
- `POST /api/sponsorships/purchase-package` - Paket satın alma + kod üretimi
- `GET /api/sponsorships/my-purchases` - Satın alma geçmişi
- `GET /api/sponsorships/my-codes` - Üretilen kodları listeleme
- `GET /api/sponsorships/code-status/{code}` - Kod durumu sorgulama
- `PUT /api/sponsorships/deactivate-code/{id}` - Kod deaktivasyonu

#### Analytics & Reporting
- `GET /api/sponsorships/sponsored-analyses` - Sponsor edilen analizler
- `GET /api/sponsorships/usage-analytics` - Kullanım analitiği
- `GET /api/sponsorships/code-redemption-stats` - Kod kullanım istatistikleri

#### Farmer Side
- `POST /api/subscriptions/redeem-code` - Kod ile subscription oluşturma
- `GET /api/subscriptions/my-subscription` - Aktif subscription görüntüleme

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

## 🚀 Kapsamlı Test Senaryoları

### End-to-End Test Scenario
```bash
#!/bin/bash

# 1. SPONSOR SETUP
echo "=== SPONSOR SETUP ==="

# Register sponsor
curl -X POST "https://localhost:5001/api/v1/auth/register" \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test.sponsor@company.com",
       "password": "TestSponsor123!",
       "firstName": "Test",
       "lastName": "Sponsor",
       "role": "Sponsor"
     }'

# Login and get token
TOKEN=$(curl -X POST "https://localhost:5001/api/v1/auth/login" \
     -H "Content-Type: application/json" \
     -d '{"email": "test.sponsor@company.com", "password": "TestSponsor123!"}' \
     | jq -r '.data.accessToken')

# Create company profile
curl -X POST "https://localhost:5001/api/sponsorships/create-profile" \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "companyName": "Test Agriculture Co.",
       "companyDescription": "Test company for sponsorship",
       "companyType": "Cooperative",
       "businessModel": "B2B"
     }'

# 2. PACKAGE PURCHASES
echo "=== PURCHASING PACKAGES ==="

# Purchase S package (5 codes)
curl -X POST "https://localhost:5001/api/sponsorships/purchase-package" \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "subscriptionTierId": 2,
       "quantity": 5,
       "paymentMethod": "Test",
       "paymentReference": "TEST_001"
     }'

# Purchase M package (3 codes)
curl -X POST "https://localhost:5001/api/sponsorships/purchase-package" \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "subscriptionTierId": 3,
       "quantity": 3,
       "paymentMethod": "Test",
       "paymentReference": "TEST_002"
     }'

# 3. VIEW GENERATED CODES
echo "=== VIEWING CODES ==="
curl -X GET "https://localhost:5001/api/sponsorships/my-codes" \
     -H "Authorization: Bearer $TOKEN"

# 4. FARMER REDEMPTION
echo "=== FARMER CODE REDEMPTION ==="

# Register farmer
curl -X POST "https://localhost:5001/api/v1/auth/register" \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test.farmer@example.com",
       "password": "TestFarmer123!",
       "firstName": "Test",
       "lastName": "Farmer",
       "role": "Farmer"
     }'

# Farmer login
FARMER_TOKEN=$(curl -X POST "https://localhost:5001/api/v1/auth/login" \
     -H "Content-Type: application/json" \
     -d '{"email": "test.farmer@example.com", "password": "TestFarmer123!"}' \
     | jq -r '.data.accessToken')

# Redeem sponsorship code
curl -X POST "https://localhost:5001/api/subscriptions/redeem-code" \
     -H "Authorization: Bearer $FARMER_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"sponsorshipCode": "SPT001-ABC123"}'

# 5. PLANT ANALYSIS
echo "=== PLANT ANALYSIS ==="
curl -X POST "https://localhost:5001/api/v1/plantanalyses/analyze" \
     -H "Authorization: Bearer $FARMER_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "image": "data:image/jpeg;base64,/9j/4AAQ...",
       "farmerId": "F001",
       "cropType": "tomato"
     }'

# 6. SPONSOR ANALYTICS
echo "=== SPONSOR ANALYTICS ==="
curl -X GET "https://localhost:5001/api/sponsorships/sponsored-analyses" \
     -H "Authorization: Bearer $TOKEN"

curl -X GET "https://localhost:5001/api/sponsorships/code-redemption-stats" \
     -H "Authorization: Bearer $TOKEN"

echo "✅ All tests completed successfully!"
```

### Test Validation Checklist

#### 🔍 Database Verification
```sql
-- Check sponsor profiles
SELECT * FROM "SponsorProfiles" WHERE "CompanyName" = 'Test Agriculture Co.';

-- Check purchases
SELECT p.*, t."TierName" 
FROM "SponsorshipPurchases" p
JOIN "SubscriptionTiers" t ON p."SubscriptionTierId" = t."Id"
WHERE p."SponsorId" = 1;

-- Check generated codes
SELECT c.*, t."TierName"
FROM "SponsorshipCodes" c
JOIN "SubscriptionTiers" t ON c."SubscriptionTierId" = t."Id"
WHERE c."SponsorId" = 1;

-- Check farmer subscriptions
SELECT s.*, t."TierName", u."Email"
FROM "UserSubscriptions" s
JOIN "SubscriptionTiers" t ON s."SubscriptionTierId" = t."Id"
JOIN "Users" u ON s."UserId" = u."Id"
WHERE s."SponsorshipCodeId" IS NOT NULL;

-- Check plant analyses with sponsor
SELECT p."Id", p."SponsorUserId", p."SponsorshipCodeId", p."OverallHealthScore"
FROM "PlantAnalyses" p
WHERE p."SponsorUserId" IS NOT NULL;
```

---

## 📋 Özet: Kritik Değişiklikler

### ✅ YAPILMASI GEREKENLER:
1. **TEK Profil**: Sponsor sadece bir company profile oluşturur
2. **Çoklu Paket**: İhtiyaca göre farklı paketler satın alabilir
3. **Kod Dağıtımı**: Üretilen kodları farmers'a dağıtır
4. **Normal Analiz**: Farmer subscription ile analiz yapar

### ❌ YAPILMAMASI GEREKENLER:
1. **Her tier için ayrı profil oluşturmayın**
2. **Analiz payload'ında sponsorshipCode göndermeyın**
3. **Profile'a tier bağlamayın**
4. **Farmer'ın doğrudan sponsor özelliklerine erişmesini beklemeyin**

---

**🎉 Bu güncellenmiş test kılavuzu ile düzeltilmiş sponsorluk sistemini başarıyla test edebilirsiniz!**