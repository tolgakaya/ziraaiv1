# 📱 Deep Links API Dokümantasyonu

## 🎯 Genel Bakış

Deep Links API, ZiraAI mobil uygulaması için dinamik bağlantı oluşturma, izleme ve yönetim sistemidir. Sponsorlar ve adminler, çiftçileri mobil uygulamaya yönlendirmek, kod kullandırmak veya analiz sonuçlarını göstermek için özel linkler oluşturabilir.

### 🔗 Deep Link Nedir?
Deep link, kullanıcıyı doğrudan mobil uygulamanın belirli bir ekranına götüren özel URL'lerdir.

**Örnek Karşılaştırma:**
- **Normal Link**: `https://ziraai.com` → Ana sayfayı açar
- **Deep Link**: `ziraai://redemption?code=ABC123` → Direkt kod kullanma ekranını açar
- **Universal Link**: `https://ziraai.com/redeem/ABC123` → iOS/Android uyumlu

## 🚀 API Endpoints

### 1. Deep Link Oluşturma

**Endpoint**: `POST /api/v1/deeplinks/generate`  
**Authorization**: Bearer Token (Sponsor, Admin)  
**Content-Type**: `application/json`

#### Request Payload
```json
{
  \"type\": \"redemption\",  // Link tipi: redemption, analysis, dashboard, profile
  \"primaryParameter\": \"AGRI-2025-44273F45\",  // Ana parametre (kod, analiz ID)
  \"additionalParameters\": {
    \"campaign\": \"harvest2025\",
    \"source\": \"whatsapp\",
    \"region\": \"ankara\"
  },
  \"generateQrCode\": true,  // QR kod oluştur (opsiyonel)
  \"campaignSource\": \"whatsapp\",  // Kampanya kaynağı
  \"sponsorId\": \"123\",  // Sponsor ID (otomatik set edilir)
  \"fallbackUrl\": \"https://web.ziraai.com/redeem\"  // Web fallback URL
}
```

#### Response
```json
{
  \"success\": true,
  \"data\": {
    \"linkId\": \"Abc123Xyz456\",  // Unique link ID
    \"deepLinkUrl\": \"ziraai://redemption?code=AGRI-2025-44273F45&linkId=Abc123Xyz456\",
    \"universalLinkUrl\": \"https://ziraai.com/redeem/AGRI-2025-44273F45\",
    \"webFallbackUrl\": \"https://web.ziraai.com/redeem/AGRI-2025-44273F45\",
    \"shortUrl\": \"https://ziraai.com/s/4B2C1F9\",
    \"qrCodeUrl\": \"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAA...\",
    \"createdDate\": \"2025-01-19T10:30:00\",
    \"expiryDate\": \"2025-07-19T10:30:00\",
    \"isActive\": true
  }
}
```

### 2. Link Analytics

**Endpoint**: `GET /api/v1/deeplinks/analytics/{linkId}`  
**Authorization**: Bearer Token (Sponsor, Admin)

#### Response
```json
{
  \"success\": true,
  \"data\": {
    \"linkId\": \"Abc123Xyz456\",
    \"totalClicks\": 145,
    \"mobileAppOpens\": 98,
    \"webFallbackOpens\": 47,
    \"uniqueDevices\": 89,
    \"conversionRate\": 67.6,
    \"platformBreakdown\": {
      \"iOS\": 65,
      \"Android\": 33,
      \"Web\": 47
    },
    \"sourceBreakdown\": {
      \"whatsapp\": 78,
      \"sms\": 34,
      \"email\": 23,
      \"direct\": 10
    },
    \"geographicData\": {
      \"Turkey\": 98,
      \"Germany\": 25,
      \"Netherlands\": 22
    },
    \"recentClicks\": [
      {
        \"clickDate\": \"2025-01-19T15:45:00\",
        \"platform\": \"iOS\",
        \"country\": \"Turkey\",
        \"source\": \"whatsapp\",
        \"didOpenApp\": true,
        \"didCompleteAction\": true
      }
    ]
  }
}
```

### 3. Click Tracking

**Endpoint**: `POST /api/v1/deeplinks/track-click/{linkId}`  
**Authorization**: Public (Anonymous)  
**Content-Type**: `application/json`

Bu endpoint mobil uygulama ve web tarafından otomatik olarak çağrılır.

#### Headers
```
User-Agent: Mobile app veya browser bilgisi
X-Device-Id: Cihaz ID (mobil uygulamalardan)
X-Real-IP: Gerçek IP adresi
Referer: Kaynak URL
```

### 4. Universal Link Configuration

**Endpoint**: `GET /api/v1/deeplinks/universal-link-config`  
**Authorization**: Public (Anonymous)

iOS ve Android için uygulama yapılandırma bilgilerini döner.

#### Response
```json
{
  \"success\": true,
  \"data\": {
    \"appId\": \"com.ziraai.app\",
    \"teamId\": \"ABC123DEF4\",
    \"pathPatterns\": {
      \"/redeem/*\": \"redemption\",
      \"/analysis/*\": \"analysis\",
      \"/dashboard\": \"dashboard\",
      \"/profile/*\": \"profile\"
    },
    \"appleAppStoreUrl\": \"https://apps.apple.com/app/ziraai/id123456789\",
    \"googlePlayStoreUrl\": \"https://play.google.com/store/apps/details?id=com.ziraai.app\",
    \"webFallbackDomain\": \"https://web.ziraai.com\",
    \"isConfigured\": true
  }
}
```

### 5. Smart Redirect

**Endpoint**: `GET /r/{linkId}`  
**Authorization**: Public (Anonymous)

Platform-aware yönlendirme sistemi. Kullanıcının platformunu tespit eder ve uygun yönlendirme yapar.

#### Davranış:
- **iOS**: Universal link ile uygulamayı açar, uygulama yoksa App Store'a yönlendirir
- **Android**: App link ile uygulamayı açar, uygulama yoksa Play Store'a yönlendirir  
- **Desktop**: Web fallback URL'e yönlendirir

### 6. Apple App Site Association

**Endpoint**: `GET /.well-known/apple-app-site-association`  
**Authorization**: Public (Anonymous)  
**Content-Type**: `application/json`

iOS Universal Links için gerekli Apple konfigürasyon dosyası.

### 7. Android Asset Links

**Endpoint**: `GET /.well-known/assetlinks.json`  
**Authorization**: Public (Anonymous)  
**Content-Type**: `application/json`

Android App Links için gerekli Google konfigürasyon dosyası.

## 🎯 Link Tipleri ve Kullanım Senaryoları

### 1️⃣ Sponsorship Code Redemption (`type: \"redemption\"`)

**Amaç**: Sponsorların çiftçilere kod dağıtımı

#### Kullanım Senaryosu:
1. Sponsor, toplu kod satın alır
2. Deep link oluşturur: kod + kampanya bilgileri
3. WhatsApp/SMS ile çiftçilere gönderir
4. Çiftçi linke tıklar → Mobil uygulamada kod otomatik kullanılır

#### Örnek Request:
```json
{
  \"type\": \"redemption\",
  \"primaryParameter\": \"AGRI-2025-44273F45\",
  \"additionalParameters\": {
    \"campaign\": \"hasat2025\",
    \"sponsorName\": \"AgroTech Ltd\"
  },
  \"campaignSource\": \"whatsapp\",
  \"generateQrCode\": true
}
```

#### Generated URLs:
- **Deep Link**: `ziraai://redemption?code=AGRI-2025-44273F45&linkId=Abc123`
- **Universal Link**: `https://ziraai.com/redeem/AGRI-2025-44273F45`
- **Short URL**: `https://ziraai.com/s/4B2C1F9`

### 2️⃣ Plant Analysis Result (`type: \"analysis\"`)

**Amaç**: Analiz tamamlandığında sonuç linki gönderme

#### Kullanım Senaryosu:
1. Çiftçi bitki analizi yaptırır
2. N8N webhook analizi tamamlar
3. Otomatik deep link oluşturulur
4. SMS/Push notification ile çiftçiye gönderilir
5. Çiftçi direkt analiz sonuçlarını görür

#### Örnek Request:
```json
{
  \"type\": \"analysis\",
  \"primaryParameter\": \"12345\",  // Analysis ID
  \"additionalParameters\": {
    \"notification\": \"completed\",
    \"cropType\": \"tomato\",
    \"urgency\": \"high\"
  },
  \"campaignSource\": \"notification\"
}
```

### 3️⃣ Dashboard Access (`type: \"dashboard\"`)

**Amaç**: Sponsor dashboard'a hızlı erişim

#### Kullanım Senaryosu:
1. Sponsor, analytics raporu istiyor
2. Deep link ile direkt dashboard'a yönlendirilir
3. Belirli raporlara odaklanır

#### Örnek Request:
```json
{
  \"type\": \"dashboard\",
  \"additionalParameters\": {
    \"section\": \"analytics\",
    \"period\": \"monthly\",
    \"filter\": \"sponsored-farmers\"
  },
  \"campaignSource\": \"email\"
}
```

### 4️⃣ Profile Management (`type: \"profile\"`)

**Amaç**: Kullanıcı profil güncelleme

#### Örnek Request:
```json
{
  \"type\": \"profile\",
  \"primaryParameter\": \"user123\",
  \"additionalParameters\": {
    \"action\": \"update-subscription\",
    \"redirect\": \"payment\"
  }
}
```

## 📱 Platform-Specific Behaviors

### iOS Implementation
```
1. Universal Link Check: https://ziraai.com/redeem/CODE
2. App Installed → Direct Open
3. App Not Installed → App Store Redirect
4. Fallback → Safari with download prompt
```

### Android Implementation
```
1. App Links Check: Intent filter match
2. App Installed → Direct Open  
3. App Not Installed → Play Store Redirect
4. Fallback → Chrome with download prompt
```

### Desktop/Web
```
1. Web Fallback URL redirect
2. QR Code display for mobile scanning
3. Download page with mobile app links
```

## 🎨 QR Code Generation

QR kodlar otomatik olarak şu özelliklerde oluşturulur:

### Özellikler:
- **Format**: PNG (Base64 encoded)
- **Size**: 300x300 pixels (varsayılan)
- **Error Correction**: Medium level
- **Content**: Universal Link URL

### Kullanım Alanları:
- Poster ve broşürler
- Kartvizitler
- Etkinlik banner'ları
- Fiziksel kampanyalar
- Offline marketing materyalleri

### Örnek QR Code Data:
```
data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAS0AAAEtCAYAAABKv...
```

## 📊 Analytics ve Tracking

### Otomatik Takip Edilen Metrikler:

#### Click Metrics:
- **Total Clicks**: Toplam tıklama sayısı
- **Unique Devices**: Benzersiz cihaz sayısı  
- **Mobile App Opens**: Mobil uygulamada açılma
- **Web Fallback Opens**: Web'e yönlendirme
- **Conversion Rate**: Başarılı aksiyon oranı

#### Platform Breakdown:
- iOS kullanımı
- Android kullanımı
- Web browser kullanımı
- Mobile vs Desktop

#### Geographic Data:
- Ülke bazında kullanım
- Şehir bazında dağılım
- Zaman dilimi analizi

#### Source Tracking:
- WhatsApp kampanyaları
- SMS kampanyaları
- Email kampanyaları
- Direct erişim
- QR code taramalar

#### Conversion Tracking:
- Link tıklama → Uygulama açma
- Uygulama açma → Kod kullanma
- Kod kullanma → Analiz başlatma
- Analiz tamamlama → Sonuç görüntüleme

## 🔧 Configuration

### appsettings.json
```json
{
  \"DeepLinks\": {
    \"BaseUrl\": \"ziraai://\",
    \"UniversalLinkDomain\": \"https://ziraai.com\",
    \"WebFallbackDomain\": \"https://web.ziraai.com\",
    \"AppId\": \"com.ziraai.app\",
    \"TeamId\": \"ABC123DEF4\",
    \"AppleAppStoreUrl\": \"https://apps.apple.com/app/ziraai/id123456789\",
    \"GooglePlayStoreUrl\": \"https://play.google.com/store/apps/details?id=com.ziraai.app\"
  }
}
```

## 🚨 Error Handling

### Common Error Responses:

#### 404 - Link Not Found
```json
{
  \"success\": false,
  \"message\": \"Deep link bulunamadı\",
  \"errorCode\": \"LINK_NOT_FOUND\"
}
```

#### 400 - Invalid Parameters
```json
{
  \"success\": false,
  \"message\": \"Geçersiz link tipi\",
  \"errorCode\": \"INVALID_TYPE\",
  \"validTypes\": [\"redemption\", \"analysis\", \"dashboard\", \"profile\"]
}
```

#### 410 - Link Expired
```json
{
  \"success\": false,
  \"message\": \"Link süresi dolmuş\",
  \"errorCode\": \"LINK_EXPIRED\",
  \"expiredDate\": \"2025-07-19T10:30:00\"
}
```

## 💡 Best Practices

### 1. Campaign Tracking
```javascript
// Her kampanya için benzersiz source kullanın
const deepLink = await createDeepLink({
  type: \"redemption\",
  primaryParameter: code,
  campaignSource: \"whatsapp_harvest2025\",
  additionalParameters: {
    campaign: \"harvest2025\",
    region: \"ankara\",
    cropType: \"wheat\"
  }
});
```

### 2. QR Code Campaigns
```javascript
// Fiziksel kampanyalar için QR kod oluşturun
const deepLink = await createDeepLink({
  type: \"redemption\",
  primaryParameter: \"FIELD2025\",
  campaignSource: \"qr_poster\",
  generateQrCode: true
});

// Posteri yazdırın
printPoster({
  qrCode: deepLink.qrCodeUrl,
  shortUrl: deepLink.shortUrl
});
```

### 3. Analytics Monitoring
```javascript
// Kampanya performansını düzenli takip edin
const analytics = await getDeepLinkAnalytics(linkId);

if (analytics.conversionRate < 50) {
  // Düşük conversion rate - kampanyayı optimize edin
  optimizeCampaign(linkId);
}
```

### 4. Mobile App Integration
```swift
// iOS Universal Links handling
func application(_ application: UIApplication, 
                continue userActivity: NSUserActivity, 
                restorationHandler: @escaping ([UIUserActivityRestoring]?) -> Void) -> Bool {
    
    guard userActivity.activityType == NSUserActivityTypeBrowsingWeb,
          let url = userActivity.webpageURL else {
        return false
    }
    
    // Parse deep link parameters
    handleDeepLink(url)
    return true
}
```

## 🔒 Security Considerations

### Rate Limiting:
- Sponsor başına günlük 1000 link limiti
- IP bazında saatlik 100 link limiti

### Fraud Detection:
- Anormal tıklama pattern'leri tespit
- Botların engellemesi
- Unique device tracking

### Link Expiry:
- Varsayılan 6 ay geçerlilik
- Kritik linkler için kısa süreli expiry
- Manuel deactivation desteği

### Access Control:
- Sadece Sponsor ve Admin rolleri link oluşturabilir
- Analytics sadece link sahibi görebilir
- Click tracking public (anonymous)

Bu dokümantasyon, ZiraAI Deep Links API'sinin kapsamlı kullanım kılavuzudur. 🚀