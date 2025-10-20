# ZiraAI Abonelik Sistemi Kullanım Kılavuzu

## İçindekiler
1. [Genel Bakış](#genel-bakış)
2. [Kullanıcı Kayıt ve Rol Sistemi](#kullanıcı-kayıt-ve-rol-sistemi)
3. [Abonelik Paketleri](#abonelik-paketleri)
4. [API Referansı](#api-referansı)
5. [Frontend Entegrasyonu](#frontend-entegrasyonu)
6. [Ödeme Gateway Entegrasyonu](#ödeme-gateway-entegrasyonu)
7. [Hata Yönetimi](#hata-yönetimi)
8. [Test Senaryoları](#test-senaryoları)

## Genel Bakış

ZiraAI abonelik sistemi, kullanıcılara farklı seviyelerde bitki analiz hizmetleri sunan kurumsal düzeyde bir çözümdür. Sistem, kullanım bazlı faturalandırma, otomatik kota yönetimi ve çoklu rol desteği ile birlikte gelir.

### Temel Özellikler
- 🔐 **Rol Tabanlı Erişim Kontrolü**: Farmer, Sponsor, Admin rolleri
- 📊 **Kademeli Abonelik Sistemi**: Trial, S, M, L, XL paketleri
- 🔄 **Otomatik Kota Yönetimi**: Günlük ve aylık limitler
- 💳 **Esnek Ödeme Entegrasyonu**: Stripe, Iyzico, PayTR desteği
- 📈 **Detaylı Kullanım Analitikleri**: Her istek için audit trail
- 🚀 **Gerçek Zamanlı Doğrulama**: Anlık kota kontrolü

## Kullanıcı Kayıt ve Rol Sistemi

### 1. Kullanıcı Rolleri

#### Farmer (Çiftçi)
- Temel kullanıcı rolü
- Bitki analizi yapabilir
- Kendi analizlerini görüntüleyebilir
- Abonelik satın alabilir

#### Sponsor
- Kurumsal kullanıcı rolü
- Sponsorluk yaptığı çiftçilerin analizlerini görüntüleyebilir
- Toplu raporlama özelliklerine erişebilir
- Özel API anahtarları alabilir

#### Admin
- Sistem yöneticisi rolü
- Tüm kullanıcıları ve abonelikleri yönetebilir
- Sistem konfigürasyonlarını değiştirebilir
- Detaylı analitiklere erişebilir

### 2. Kayıt API'si

#### Endpoint
```
POST /api/users/register
```

#### Farmer Kaydı
```json
{
  "email": "mehmet.ciftci@gmail.com",
  "password": "CiftciMehmet123!",
  "fullName": "Mehmet Çiftçi",
  "userRole": "Farmer"
}
```

#### Sponsor Kaydı
```json
{
  "email": "iletisim@tarimfirma.com.tr",
  "password": "TarimFirma2025!",
  "fullName": "Yeşil Tarım Teknolojileri A.Ş.",
  "userRole": "Sponsor"
}
```

#### Admin Kaydı
```json
{
  "email": "admin@ziraai.com",
  "password": "Admin@ZiraAI2025!",
  "fullName": "Sistem Yöneticisi",
  "userRole": "Admin"
}
```

#### Başarılı Kayıt Yanıtı
```json
{
  "success": true,
  "message": "Kayıt başarılı",
  "data": {
    "userId": 123,
    "email": "mehmet.ciftci@gmail.com",
    "fullName": "Mehmet Çiftçi",
    "role": "Farmer",
    "hasTrialSubscription": true,
    "trialEndDate": "2025-09-12T23:59:59"
  }
}
```

### 3. Giriş ve Token Yönetimi

#### Login Endpoint
```
POST /api/users/login
```

#### Request
```json
{
  "email": "mehmet.ciftci@gmail.com",
  "password": "CiftciMehmet123!"
}
```

#### Response
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "def50200...",
    "expiration": "2025-08-13T18:30:00",
    "user": {
      "userId": 123,
      "email": "mehmet.ciftci@gmail.com",
      "fullName": "Mehmet Çiftçi",
      "roles": ["Farmer"]
    }
  }
}
```

## Abonelik Paketleri

### Paket Özellikleri

| Paket | Günlük Limit | Aylık Limit | Aylık Ücret | Yıllık Ücret | Özellikler |
|-------|-------------|-------------|-------------|--------------|------------|
| **Trial** | 1 | 30 | ₺0 | - | 30 günlük deneme |
| **S (Small)** | 5 | 50 | ₺99.99 | ₺999.99 | Temel analiz |
| **M (Medium)** | 20 | 200 | ₺299.99 | ₺2,999.99 | Öncelikli destek |
| **L (Large)** | 50 | 500 | ₺599.99 | ₺5,999.99 | Gelişmiş analitik |
| **XL (Extra Large)** | 200 | 2000 | ₺1,499.99 | ₺14,999.99 | API erişimi + Tüm özellikler |

### Paket Detayları

#### Trial (Deneme)
- Yeni kullanıcılara otomatik atanır
- 30 gün süreyle geçerli
- Günde 1, ayda 30 analiz hakkı
- Temel analiz özellikleri

#### S (Small)
- Küçük ölçekli çiftçiler için
- Günde 5 analiz
- Email desteği
- 48 saat yanıt süresi

#### M (Medium)
- Orta ölçekli işletmeler için
- Günde 20 analiz
- Öncelikli destek
- 24 saat yanıt süresi

#### L (Large)
- Büyük işletmeler için
- Günde 50 analiz
- Gelişmiş analitik raporlar
- 12 saat yanıt süresi

#### XL (Extra Large)
- Kurumsal çözüm
- Günde 200 analiz
- API erişimi
- Özel entegrasyon desteği
- 2 saat yanıt süresi

## API Referansı

### 1. Abonelik Paketlerini Listeleme

#### Request
```http
GET /api/subscriptions/tiers
```

#### cURL Örneği
```bash
curl -X GET "https://api.ziraai.com/api/subscriptions/tiers" \
  -H "Accept: application/json"
```

#### Response
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "tierName": "Trial",
      "displayName": "Deneme",
      "description": "30 günlük ücretsiz deneme paketi",
      "dailyRequestLimit": 1,
      "monthlyRequestLimit": 30,
      "monthlyPrice": 0,
      "yearlyPrice": 0,
      "currency": "TRY",
      "features": {
        "prioritySupport": false,
        "advancedAnalytics": false,
        "apiAccess": false,
        "customIntegration": false
      },
      "responseTimeHours": 48,
      "isActive": true
    },
    {
      "id": 2,
      "tierName": "S",
      "displayName": "Small",
      "description": "Küçük ölçekli çiftçiler için başlangıç paketi",
      "dailyRequestLimit": 5,
      "monthlyRequestLimit": 50,
      "monthlyPrice": 99.99,
      "yearlyPrice": 999.99,
      "currency": "TRY",
      "features": {
        "prioritySupport": false,
        "advancedAnalytics": false,
        "apiAccess": false,
        "customIntegration": false
      },
      "responseTimeHours": 48,
      "isActive": true
    }
    // ... diğer paketler
  ]
}
```

### 2. Mevcut Abonelik Bilgisi

#### Request
```http
GET /api/subscriptions/my-subscription
Authorization: Bearer {token}
```

#### cURL Örneği
```bash
curl -X GET "https://api.ziraai.com/api/subscriptions/my-subscription" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  -H "Accept: application/json"
```

#### Response (Aktif Abonelik)
```json
{
  "success": true,
  "data": {
    "id": 456,
    "userId": 123,
    "subscriptionTier": {
      "id": 2,
      "tierName": "M",
      "displayName": "Medium",
      "dailyRequestLimit": 20,
      "monthlyRequestLimit": 200,
      "monthlyPrice": 299.99
    },
    "startDate": "2025-08-01T00:00:00",
    "endDate": "2025-09-01T00:00:00",
    "isActive": true,
    "autoRenew": true,
    "currentDailyUsage": 5,
    "currentMonthlyUsage": 45,
    "lastUsageResetDate": "2025-08-13T00:00:00",
    "monthlyUsageResetDate": "2025-08-01T00:00:00",
    "paymentMethod": "CreditCard",
    "status": "Active"
  }
}
```

### 3. Kullanım Durumu Kontrolü

#### Request
```http
GET /api/subscriptions/usage-status
Authorization: Bearer {token}
```

#### Response
```json
{
  "success": true,
  "data": {
    "hasActiveSubscription": true,
    "canMakeRequest": true,
    "subscription": {
      "tierName": "M",
      "displayName": "Medium"
    },
    "usage": {
      "daily": {
        "used": 5,
        "limit": 20,
        "remaining": 15,
        "percentageUsed": 25,
        "nextReset": "2025-08-14T00:00:00"
      },
      "monthly": {
        "used": 45,
        "limit": 200,
        "remaining": 155,
        "percentageUsed": 22.5,
        "nextReset": "2025-09-01T00:00:00"
      }
    },
    "subscriptionEndDate": "2025-09-01T00:00:00",
    "daysRemaining": 18
  }
}
```

### 4. Abonelik Satın Alma

#### Request
```http
POST /api/subscriptions/subscribe
Authorization: Bearer {token}
Content-Type: application/json
```

#### Request Body
```json
{
  "subscriptionTierId": 2,
  "durationMonths": 1,
  "autoRenew": true,
  "paymentMethod": "CreditCard",
  "paymentReference": "STRIPE-ch_1234567890abcdef",
  "isTrialSubscription": false
}
```

#### cURL Örneği
```bash
curl -X POST "https://api.ziraai.com/api/subscriptions/subscribe" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  -H "Content-Type: application/json" \
  -d '{
    "subscriptionTierId": 2,
    "durationMonths": 1,
    "autoRenew": true,
    "paymentMethod": "CreditCard",
    "paymentReference": "STRIPE-ch_1234567890abcdef",
    "isTrialSubscription": false
  }'
```

#### Response
```json
{
  "success": true,
  "message": "Abonelik başarıyla oluşturuldu",
  "data": {
    "subscriptionId": 789,
    "userId": 123,
    "tier": {
      "name": "M",
      "displayName": "Medium"
    },
    "period": {
      "startDate": "2025-08-13T15:30:00",
      "endDate": "2025-09-13T15:30:00",
      "durationDays": 31
    },
    "limits": {
      "dailyLimit": 20,
      "monthlyLimit": 200
    },
    "payment": {
      "amount": 299.99,
      "currency": "TRY",
      "method": "CreditCard",
      "reference": "STRIPE-ch_1234567890abcdef"
    },
    "autoRenew": true,
    "status": "Active"
  }
}
```

### 5. Abonelik İptali

#### Request
```http
POST /api/subscriptions/cancel
Authorization: Bearer {token}
Content-Type: application/json
```

#### Request Body
```json
{
  "userSubscriptionId": 789,
  "cancellationReason": "Artık ihtiyacım kalmadı",
  "immediateCancellation": false
}
```

#### Response
```json
{
  "success": true,
  "message": "Abonelik dönem sonunda iptal edilecek",
  "data": {
    "subscriptionId": 789,
    "cancellationDate": "2025-09-13T15:30:00",
    "refundAmount": 0,
    "remainingDays": 18
  }
}
```

### 6. Abonelik Geçmişi

#### Request
```http
GET /api/subscriptions/history
Authorization: Bearer {token}
```

#### Response
```json
{
  "success": true,
  "data": [
    {
      "id": 789,
      "tierName": "M",
      "startDate": "2025-08-13T15:30:00",
      "endDate": "2025-09-13T15:30:00",
      "status": "Active",
      "paidAmount": 299.99,
      "usage": {
        "totalRequests": 45,
        "successfulRequests": 43,
        "failedRequests": 2
      }
    },
    {
      "id": 456,
      "tierName": "S",
      "startDate": "2025-07-01T00:00:00",
      "endDate": "2025-08-01T00:00:00",
      "status": "Expired",
      "paidAmount": 99.99,
      "usage": {
        "totalRequests": 48,
        "successfulRequests": 48,
        "failedRequests": 0
      }
    }
  ]
}
```

### 7. Sponsor Özellikleri

#### Sponsor Analizlerini Görüntüleme
```http
GET /api/subscriptions/sponsored-analyses
Authorization: Bearer {sponsor_token}
```

#### Response
```json
{
  "success": true,
  "data": {
    "sponsorInfo": {
      "companyName": "Yeşil Tarım Teknolojileri A.Ş.",
      "sponsorId": 456
    },
    "sponsoredFarmers": [
      {
        "farmerId": "F001",
        "farmerName": "Mehmet Çiftçi",
        "totalAnalyses": 125,
        "lastAnalysisDate": "2025-08-13T10:30:00"
      },
      {
        "farmerId": "F002",
        "farmerName": "Ali Yılmaz",
        "totalAnalyses": 89,
        "lastAnalysisDate": "2025-08-12T14:20:00"
      }
    ],
    "analyses": [
      {
        "id": 1001,
        "farmerId": "F001",
        "farmerName": "Mehmet Çiftçi",
        "cropType": "tomato",
        "analysisDate": "2025-08-13T10:30:00",
        "results": {
          "healthStatus": "Healthy",
          "diseases": [],
          "recommendations": ["Normal sulama programına devam edin"]
        }
      }
      // ... diğer analizler
    ],
    "statistics": {
      "totalAnalyses": 214,
      "thisMonth": 45,
      "healthyPercentage": 78.5,
      "mostCommonCrop": "tomato",
      "mostCommonDisease": "Early Blight"
    }
  }
}
```

## Frontend Entegrasyonu

### 1. React Entegrasyonu

#### Servis Katmanı (services/subscriptionService.js)
```javascript
import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://api.ziraai.com';

class SubscriptionService {
  constructor() {
    this.token = localStorage.getItem('authToken');
  }

  // Auth header helper
  getAuthHeaders() {
    return {
      'Authorization': `Bearer ${this.token}`,
      'Content-Type': 'application/json'
    };
  }

  // Paketleri listele
  async getSubscriptionTiers() {
    try {
      const response = await axios.get(`${API_BASE_URL}/api/subscriptions/tiers`);
      return response.data;
    } catch (error) {
      console.error('Error fetching subscription tiers:', error);
      throw error;
    }
  }

  // Mevcut abonelik bilgisi
  async getMySubscription() {
    try {
      const response = await axios.get(
        `${API_BASE_URL}/api/subscriptions/my-subscription`,
        { headers: this.getAuthHeaders() }
      );
      return response.data;
    } catch (error) {
      if (error.response?.status === 404) {
        return { success: false, data: null };
      }
      throw error;
    }
  }

  // Kullanım durumu
  async getUsageStatus() {
    try {
      const response = await axios.get(
        `${API_BASE_URL}/api/subscriptions/usage-status`,
        { headers: this.getAuthHeaders() }
      );
      return response.data;
    } catch (error) {
      console.error('Error fetching usage status:', error);
      throw error;
    }
  }

  // Abonelik satın al
  async subscribe(subscriptionData) {
    try {
      const response = await axios.post(
        `${API_BASE_URL}/api/subscriptions/subscribe`,
        subscriptionData,
        { headers: this.getAuthHeaders() }
      );
      return response.data;
    } catch (error) {
      console.error('Error subscribing:', error);
      throw error;
    }
  }

  // Abonelik iptal et
  async cancelSubscription(cancellationData) {
    try {
      const response = await axios.post(
        `${API_BASE_URL}/api/subscriptions/cancel`,
        cancellationData,
        { headers: this.getAuthHeaders() }
      );
      return response.data;
    } catch (error) {
      console.error('Error cancelling subscription:', error);
      throw error;
    }
  }
}

export default new SubscriptionService();
```

#### React Component Örneği (SubscriptionManager.jsx)
```jsx
import React, { useState, useEffect } from 'react';
import subscriptionService from '../services/subscriptionService';
import PaymentModal from './PaymentModal';

const SubscriptionManager = () => {
  const [tiers, setTiers] = useState([]);
  const [currentSubscription, setCurrentSubscription] = useState(null);
  const [usageStatus, setUsageStatus] = useState(null);
  const [loading, setLoading] = useState(true);
  const [selectedTier, setSelectedTier] = useState(null);
  const [showPaymentModal, setShowPaymentModal] = useState(false);

  useEffect(() => {
    loadSubscriptionData();
  }, []);

  const loadSubscriptionData = async () => {
    try {
      setLoading(true);
      
      // Paralel API çağrıları
      const [tiersResponse, subscriptionResponse, usageResponse] = await Promise.all([
        subscriptionService.getSubscriptionTiers(),
        subscriptionService.getMySubscription(),
        subscriptionService.getUsageStatus()
      ]);

      setTiers(tiersResponse.data);
      setCurrentSubscription(subscriptionResponse.data);
      setUsageStatus(usageResponse.data);
    } catch (error) {
      console.error('Error loading subscription data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSubscribe = (tier) => {
    setSelectedTier(tier);
    setShowPaymentModal(true);
  };

  const handlePaymentSuccess = async (paymentData) => {
    try {
      const subscriptionData = {
        subscriptionTierId: selectedTier.id,
        durationMonths: 1,
        autoRenew: true,
        paymentMethod: paymentData.paymentMethod,
        paymentReference: paymentData.transactionId,
        isTrialSubscription: false
      };

      const result = await subscriptionService.subscribe(subscriptionData);
      
      if (result.success) {
        alert('Abonelik başarıyla oluşturuldu!');
        await loadSubscriptionData(); // Verileri yenile
        setShowPaymentModal(false);
      }
    } catch (error) {
      alert('Abonelik oluşturulurken hata oluştu: ' + error.message);
    }
  };

  const handleCancelSubscription = async () => {
    if (!window.confirm('Aboneliğinizi iptal etmek istediğinizden emin misiniz?')) {
      return;
    }

    try {
      const result = await subscriptionService.cancelSubscription({
        userSubscriptionId: currentSubscription.id,
        cancellationReason: 'Kullanıcı talebi',
        immediateCancellation: false
      });

      if (result.success) {
        alert('Abonelik dönem sonunda iptal edilecek.');
        await loadSubscriptionData();
      }
    } catch (error) {
      alert('İptal işlemi başarısız: ' + error.message);
    }
  };

  if (loading) {
    return <div className="loading">Yükleniyor...</div>;
  }

  return (
    <div className="subscription-manager">
      {/* Mevcut Abonelik Bilgisi */}
      {currentSubscription && (
        <div className="current-subscription">
          <h2>Mevcut Aboneliğiniz</h2>
          <div className="subscription-card">
            <h3>{currentSubscription.subscriptionTier.displayName}</h3>
            <p>Bitiş Tarihi: {new Date(currentSubscription.endDate).toLocaleDateString('tr-TR')}</p>
            <div className="usage-stats">
              <div className="usage-item">
                <span>Günlük Kullanım:</span>
                <div className="progress-bar">
                  <div 
                    className="progress-fill" 
                    style={{width: `${(usageStatus?.usage?.daily?.percentageUsed || 0)}%`}}
                  />
                </div>
                <span>{usageStatus?.usage?.daily?.used} / {usageStatus?.usage?.daily?.limit}</span>
              </div>
              <div className="usage-item">
                <span>Aylık Kullanım:</span>
                <div className="progress-bar">
                  <div 
                    className="progress-fill" 
                    style={{width: `${(usageStatus?.usage?.monthly?.percentageUsed || 0)}%`}}
                  />
                </div>
                <span>{usageStatus?.usage?.monthly?.used} / {usageStatus?.usage?.monthly?.limit}</span>
              </div>
            </div>
            {currentSubscription.autoRenew && (
              <p className="auto-renew">✓ Otomatik yenileme aktif</p>
            )}
            <button 
              className="btn-cancel" 
              onClick={handleCancelSubscription}
            >
              Aboneliği İptal Et
            </button>
          </div>
        </div>
      )}

      {/* Abonelik Paketleri */}
      <div className="subscription-tiers">
        <h2>Abonelik Paketleri</h2>
        <div className="tiers-grid">
          {tiers.map(tier => (
            <div key={tier.id} className={`tier-card ${tier.tierName}`}>
              <h3>{tier.displayName}</h3>
              <p className="description">{tier.description}</p>
              
              <div className="price">
                <span className="amount">₺{tier.monthlyPrice}</span>
                <span className="period">/ay</span>
              </div>

              <ul className="features">
                <li>✓ Günlük {tier.dailyRequestLimit} analiz</li>
                <li>✓ Aylık {tier.monthlyRequestLimit} analiz</li>
                {tier.features.prioritySupport && <li>✓ Öncelikli destek</li>}
                {tier.features.advancedAnalytics && <li>✓ Gelişmiş analitik</li>}
                {tier.features.apiAccess && <li>✓ API erişimi</li>}
                <li>✓ {tier.responseTimeHours} saat yanıt süresi</li>
              </ul>

              <button 
                className="btn-subscribe"
                onClick={() => handleSubscribe(tier)}
                disabled={currentSubscription?.subscriptionTier?.id === tier.id}
              >
                {currentSubscription?.subscriptionTier?.id === tier.id 
                  ? 'Mevcut Plan' 
                  : 'Satın Al'}
              </button>
            </div>
          ))}
        </div>
      </div>

      {/* Ödeme Modal */}
      {showPaymentModal && (
        <PaymentModal
          tier={selectedTier}
          onSuccess={handlePaymentSuccess}
          onClose={() => setShowPaymentModal(false)}
        />
      )}
    </div>
  );
};

export default SubscriptionManager;
```

#### Kullanım Durumu Widget'ı (UsageWidget.jsx)
```jsx
import React, { useState, useEffect } from 'react';
import subscriptionService from '../services/subscriptionService';

const UsageWidget = () => {
  const [usage, setUsage] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadUsageStatus();
    // Her 30 saniyede bir güncelle
    const interval = setInterval(loadUsageStatus, 30000);
    return () => clearInterval(interval);
  }, []);

  const loadUsageStatus = async () => {
    try {
      const response = await subscriptionService.getUsageStatus();
      setUsage(response.data);
    } catch (error) {
      console.error('Error loading usage status:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div>Yükleniyor...</div>;
  if (!usage?.hasActiveSubscription) {
    return (
      <div className="usage-widget no-subscription">
        <p>Aktif aboneliğiniz bulunmuyor</p>
        <a href="/subscription">Abonelik Satın Al</a>
      </div>
    );
  }

  const canMakeRequest = usage.canMakeRequest;
  const dailyPercentage = usage.usage.daily.percentageUsed;
  const monthlyPercentage = usage.usage.monthly.percentageUsed;

  return (
    <div className="usage-widget">
      <div className="usage-header">
        <h4>{usage.subscription.displayName} Plan</h4>
        <span className={`status ${canMakeRequest ? 'active' : 'limit-reached'}`}>
          {canMakeRequest ? '✓ Analiz Yapabilirsiniz' : '⚠ Limit Aşıldı'}
        </span>
      </div>

      <div className="usage-stats">
        <div className="stat-item">
          <label>Günlük:</label>
          <div className="mini-progress">
            <div 
              className="fill" 
              style={{
                width: `${dailyPercentage}%`,
                backgroundColor: dailyPercentage > 80 ? '#ff6b6b' : '#4ecdc4'
              }}
            />
          </div>
          <span>{usage.usage.daily.used}/{usage.usage.daily.limit}</span>
        </div>

        <div className="stat-item">
          <label>Aylık:</label>
          <div className="mini-progress">
            <div 
              className="fill" 
              style={{
                width: `${monthlyPercentage}%`,
                backgroundColor: monthlyPercentage > 80 ? '#ff6b6b' : '#4ecdc4'
              }}
            />
          </div>
          <span>{usage.usage.monthly.used}/{usage.usage.monthly.limit}</span>
        </div>
      </div>

      {!canMakeRequest && (
        <div className="limit-message">
          {usage.usage.daily.used >= usage.usage.daily.limit 
            ? `Günlük limitiniz doldu. Yarın saat 00:00'da sıfırlanacak.`
            : `Aylık limitiniz doldu. ${new Date(usage.usage.monthly.nextReset).toLocaleDateString('tr-TR')} tarihinde sıfırlanacak.`
          }
        </div>
      )}
    </div>
  );
};

export default UsageWidget;
```

### 2. Angular Entegrasyonu

#### Servis (subscription.service.ts)
```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface SubscriptionTier {
  id: number;
  tierName: string;
  displayName: string;
  description: string;
  dailyRequestLimit: number;
  monthlyRequestLimit: number;
  monthlyPrice: number;
  yearlyPrice: number;
  currency: string;
  features: {
    prioritySupport: boolean;
    advancedAnalytics: boolean;
    apiAccess: boolean;
  };
  responseTimeHours: number;
  isActive: boolean;
}

export interface UserSubscription {
  id: number;
  userId: number;
  subscriptionTier: SubscriptionTier;
  startDate: string;
  endDate: string;
  isActive: boolean;
  autoRenew: boolean;
  currentDailyUsage: number;
  currentMonthlyUsage: number;
  status: string;
}

export interface UsageStatus {
  hasActiveSubscription: boolean;
  canMakeRequest: boolean;
  subscription: {
    tierName: string;
    displayName: string;
  };
  usage: {
    daily: {
      used: number;
      limit: number;
      remaining: number;
      percentageUsed: number;
      nextReset: string;
    };
    monthly: {
      used: number;
      limit: number;
      remaining: number;
      percentageUsed: number;
      nextReset: string;
    };
  };
}

@Injectable({
  providedIn: 'root'
})
export class SubscriptionService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('authToken');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });
  }

  getSubscriptionTiers(): Observable<{ success: boolean; data: SubscriptionTier[] }> {
    return this.http.get<{ success: boolean; data: SubscriptionTier[] }>(
      `${this.apiUrl}/api/subscriptions/tiers`
    );
  }

  getMySubscription(): Observable<{ success: boolean; data: UserSubscription }> {
    return this.http.get<{ success: boolean; data: UserSubscription }>(
      `${this.apiUrl}/api/subscriptions/my-subscription`,
      { headers: this.getAuthHeaders() }
    );
  }

  getUsageStatus(): Observable<{ success: boolean; data: UsageStatus }> {
    return this.http.get<{ success: boolean; data: UsageStatus }>(
      `${this.apiUrl}/api/subscriptions/usage-status`,
      { headers: this.getAuthHeaders() }
    );
  }

  subscribe(subscriptionData: any): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/api/subscriptions/subscribe`,
      subscriptionData,
      { headers: this.getAuthHeaders() }
    );
  }

  cancelSubscription(cancellationData: any): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/api/subscriptions/cancel`,
      cancellationData,
      { headers: this.getAuthHeaders() }
    );
  }

  getSubscriptionHistory(): Observable<any> {
    return this.http.get(
      `${this.apiUrl}/api/subscriptions/history`,
      { headers: this.getAuthHeaders() }
    );
  }
}
```

#### Component (subscription-manager.component.ts)
```typescript
import { Component, OnInit } from '@angular/core';
import { SubscriptionService, SubscriptionTier, UserSubscription, UsageStatus } from '../services/subscription.service';
import { PaymentService } from '../services/payment.service';

@Component({
  selector: 'app-subscription-manager',
  templateUrl: './subscription-manager.component.html',
  styleUrls: ['./subscription-manager.component.css']
})
export class SubscriptionManagerComponent implements OnInit {
  tiers: SubscriptionTier[] = [];
  currentSubscription: UserSubscription | null = null;
  usageStatus: UsageStatus | null = null;
  loading = true;
  selectedTier: SubscriptionTier | null = null;

  constructor(
    private subscriptionService: SubscriptionService,
    private paymentService: PaymentService
  ) {}

  ngOnInit(): void {
    this.loadSubscriptionData();
  }

  async loadSubscriptionData(): Promise<void> {
    this.loading = true;
    
    try {
      // Paralel API çağrıları
      const [tiers, subscription, usage] = await Promise.all([
        this.subscriptionService.getSubscriptionTiers().toPromise(),
        this.subscriptionService.getMySubscription().toPromise(),
        this.subscriptionService.getUsageStatus().toPromise()
      ]);

      this.tiers = tiers?.data || [];
      this.currentSubscription = subscription?.data || null;
      this.usageStatus = usage?.data || null;
    } catch (error) {
      console.error('Error loading subscription data:', error);
    } finally {
      this.loading = false;
    }
  }

  async subscribeToPlan(tier: SubscriptionTier): Promise<void> {
    this.selectedTier = tier;
    
    try {
      // Önce ödeme işlemini gerçekleştir
      const paymentResult = await this.paymentService.processPayment({
        amount: tier.monthlyPrice,
        currency: tier.currency,
        description: `ZiraAI ${tier.displayName} Plan - 1 Ay`
      });

      if (paymentResult.success) {
        // Ödeme başarılıysa abonelik oluştur
        const subscriptionData = {
          subscriptionTierId: tier.id,
          durationMonths: 1,
          autoRenew: true,
          paymentMethod: paymentResult.paymentMethod,
          paymentReference: paymentResult.transactionId,
          isTrialSubscription: false
        };

        const result = await this.subscriptionService.subscribe(subscriptionData).toPromise();
        
        if (result?.success) {
          alert('Abonelik başarıyla oluşturuldu!');
          await this.loadSubscriptionData();
        }
      }
    } catch (error) {
      console.error('Subscription error:', error);
      alert('Abonelik oluşturulurken hata oluştu.');
    }
  }

  async cancelSubscription(): Promise<void> {
    if (!this.currentSubscription) return;
    
    if (!confirm('Aboneliğinizi iptal etmek istediğinizden emin misiniz?')) {
      return;
    }

    try {
      const result = await this.subscriptionService.cancelSubscription({
        userSubscriptionId: this.currentSubscription.id,
        cancellationReason: 'Kullanıcı talebi',
        immediateCancellation: false
      }).toPromise();

      if (result?.success) {
        alert('Abonelik dönem sonunda iptal edilecek.');
        await this.loadSubscriptionData();
      }
    } catch (error) {
      console.error('Cancellation error:', error);
      alert('İptal işlemi başarısız oldu.');
    }
  }

  getUsagePercentage(type: 'daily' | 'monthly'): number {
    if (!this.usageStatus?.usage) return 0;
    return this.usageStatus.usage[type].percentageUsed;
  }

  canSubscribeTo(tier: SubscriptionTier): boolean {
    if (!this.currentSubscription) return true;
    return this.currentSubscription.subscriptionTier.id !== tier.id;
  }
}
```

### 3. Vue.js Entegrasyonu

#### Composable (useSubscription.js)
```javascript
import { ref, computed } from 'vue';
import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'https://api.ziraai.com';

export function useSubscription() {
  const tiers = ref([]);
  const currentSubscription = ref(null);
  const usageStatus = ref(null);
  const loading = ref(false);
  const error = ref(null);

  const getAuthHeaders = () => ({
    'Authorization': `Bearer ${localStorage.getItem('authToken')}`,
    'Content-Type': 'application/json'
  });

  const fetchTiers = async () => {
    try {
      loading.value = true;
      const response = await axios.get(`${API_URL}/api/subscriptions/tiers`);
      tiers.value = response.data.data;
    } catch (err) {
      error.value = err.message;
    } finally {
      loading.value = false;
    }
  };

  const fetchMySubscription = async () => {
    try {
      loading.value = true;
      const response = await axios.get(
        `${API_URL}/api/subscriptions/my-subscription`,
        { headers: getAuthHeaders() }
      );
      currentSubscription.value = response.data.data;
    } catch (err) {
      if (err.response?.status !== 404) {
        error.value = err.message;
      }
    } finally {
      loading.value = false;
    }
  };

  const fetchUsageStatus = async () => {
    try {
      const response = await axios.get(
        `${API_URL}/api/subscriptions/usage-status`,
        { headers: getAuthHeaders() }
      );
      usageStatus.value = response.data.data;
    } catch (err) {
      error.value = err.message;
    }
  };

  const subscribe = async (tierId, paymentData) => {
    try {
      loading.value = true;
      const response = await axios.post(
        `${API_URL}/api/subscriptions/subscribe`,
        {
          subscriptionTierId: tierId,
          durationMonths: 1,
          autoRenew: true,
          paymentMethod: paymentData.method,
          paymentReference: paymentData.reference,
          isTrialSubscription: false
        },
        { headers: getAuthHeaders() }
      );
      
      if (response.data.success) {
        await fetchMySubscription();
        await fetchUsageStatus();
      }
      
      return response.data;
    } catch (err) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const cancelSubscription = async (subscriptionId, reason) => {
    try {
      loading.value = true;
      const response = await axios.post(
        `${API_URL}/api/subscriptions/cancel`,
        {
          userSubscriptionId: subscriptionId,
          cancellationReason: reason,
          immediateCancellation: false
        },
        { headers: getAuthHeaders() }
      );
      
      if (response.data.success) {
        await fetchMySubscription();
      }
      
      return response.data;
    } catch (err) {
      error.value = err.message;
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const canMakeRequest = computed(() => {
    return usageStatus.value?.canMakeRequest || false;
  });

  const dailyUsagePercentage = computed(() => {
    return usageStatus.value?.usage?.daily?.percentageUsed || 0;
  });

  const monthlyUsagePercentage = computed(() => {
    return usageStatus.value?.usage?.monthly?.percentageUsed || 0;
  });

  return {
    tiers,
    currentSubscription,
    usageStatus,
    loading,
    error,
    canMakeRequest,
    dailyUsagePercentage,
    monthlyUsagePercentage,
    fetchTiers,
    fetchMySubscription,
    fetchUsageStatus,
    subscribe,
    cancelSubscription
  };
}
```

#### Vue Component (SubscriptionManager.vue)
```vue
<template>
  <div class="subscription-manager">
    <!-- Loading State -->
    <div v-if="loading" class="loading">
      <div class="spinner"></div>
      <p>Yükleniyor...</p>
    </div>

    <!-- Error State -->
    <div v-if="error" class="error-message">
      {{ error }}
    </div>

    <!-- Current Subscription -->
    <div v-if="currentSubscription" class="current-subscription">
      <h2>Mevcut Aboneliğiniz</h2>
      <div class="subscription-card">
        <div class="card-header">
          <h3>{{ currentSubscription.subscriptionTier.displayName }}</h3>
          <span class="badge" :class="currentSubscription.isActive ? 'active' : 'inactive'">
            {{ currentSubscription.isActive ? 'Aktif' : 'Pasif' }}
          </span>
        </div>
        
        <div class="subscription-details">
          <p>Bitiş Tarihi: {{ formatDate(currentSubscription.endDate) }}</p>
          <p v-if="currentSubscription.autoRenew" class="auto-renew">
            ✓ Otomatik yenileme aktif
          </p>
        </div>

        <!-- Usage Stats -->
        <div class="usage-stats" v-if="usageStatus">
          <div class="usage-item">
            <label>Günlük Kullanım</label>
            <div class="progress-bar">
              <div 
                class="progress-fill" 
                :style="{
                  width: `${dailyUsagePercentage}%`,
                  backgroundColor: dailyUsagePercentage > 80 ? '#ff6b6b' : '#4ecdc4'
                }"
              ></div>
            </div>
            <span class="usage-text">
              {{ usageStatus.usage.daily.used }} / {{ usageStatus.usage.daily.limit }}
            </span>
          </div>

          <div class="usage-item">
            <label>Aylık Kullanım</label>
            <div class="progress-bar">
              <div 
                class="progress-fill" 
                :style="{
                  width: `${monthlyUsagePercentage}%`,
                  backgroundColor: monthlyUsagePercentage > 80 ? '#ff6b6b' : '#4ecdc4'
                }"
              ></div>
            </div>
            <span class="usage-text">
              {{ usageStatus.usage.monthly.used }} / {{ usageStatus.usage.monthly.limit }}
            </span>
          </div>
        </div>

        <button 
          @click="handleCancelSubscription" 
          class="btn btn-danger"
        >
          Aboneliği İptal Et
        </button>
      </div>
    </div>

    <!-- No Subscription Message -->
    <div v-else-if="!loading" class="no-subscription">
      <h2>Aktif Aboneliğiniz Bulunmuyor</h2>
      <p>Bitki analizi yapabilmek için bir abonelik paketi seçin.</p>
    </div>

    <!-- Subscription Tiers -->
    <div class="subscription-tiers">
      <h2>Abonelik Paketleri</h2>
      <div class="tiers-grid">
        <div 
          v-for="tier in tiers" 
          :key="tier.id" 
          class="tier-card"
          :class="{
            'current': currentSubscription?.subscriptionTier?.id === tier.id,
            'popular': tier.tierName === 'M'
          }"
        >
          <div v-if="tier.tierName === 'M'" class="popular-badge">
            En Popüler
          </div>

          <h3>{{ tier.displayName }}</h3>
          <p class="description">{{ tier.description }}</p>
          
          <div class="price">
            <span class="currency">₺</span>
            <span class="amount">{{ tier.monthlyPrice }}</span>
            <span class="period">/ay</span>
          </div>

          <ul class="features">
            <li>
              <svg class="check-icon" viewBox="0 0 20 20">
                <path d="M0 11l2-2 5 5L18 3l2 2L7 18z"/>
              </svg>
              Günlük {{ tier.dailyRequestLimit }} analiz
            </li>
            <li>
              <svg class="check-icon" viewBox="0 0 20 20">
                <path d="M0 11l2-2 5 5L18 3l2 2L7 18z"/>
              </svg>
              Aylık {{ tier.monthlyRequestLimit }} analiz
            </li>
            <li v-if="tier.features.prioritySupport">
              <svg class="check-icon" viewBox="0 0 20 20">
                <path d="M0 11l2-2 5 5L18 3l2 2L7 18z"/>
              </svg>
              Öncelikli destek
            </li>
            <li v-if="tier.features.advancedAnalytics">
              <svg class="check-icon" viewBox="0 0 20 20">
                <path d="M0 11l2-2 5 5L18 3l2 2L7 18z"/>
              </svg>
              Gelişmiş analitik
            </li>
            <li v-if="tier.features.apiAccess">
              <svg class="check-icon" viewBox="0 0 20 20">
                <path d="M0 11l2-2 5 5L18 3l2 2L7 18z"/>
              </svg>
              API erişimi
            </li>
            <li>
              <svg class="check-icon" viewBox="0 0 20 20">
                <path d="M0 11l2-2 5 5L18 3l2 2L7 18z"/>
              </svg>
              {{ tier.responseTimeHours }} saat yanıt süresi
            </li>
          </ul>

          <button 
            @click="handleSubscribe(tier)"
            class="btn btn-primary"
            :disabled="currentSubscription?.subscriptionTier?.id === tier.id"
          >
            {{ currentSubscription?.subscriptionTier?.id === tier.id ? 'Mevcut Plan' : 'Satın Al' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Payment Modal -->
    <PaymentModal 
      v-if="showPaymentModal"
      :tier="selectedTier"
      @success="handlePaymentSuccess"
      @close="showPaymentModal = false"
    />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useSubscription } from '@/composables/useSubscription';
import PaymentModal from '@/components/PaymentModal.vue';

const {
  tiers,
  currentSubscription,
  usageStatus,
  loading,
  error,
  dailyUsagePercentage,
  monthlyUsagePercentage,
  fetchTiers,
  fetchMySubscription,
  fetchUsageStatus,
  subscribe,
  cancelSubscription
} = useSubscription();

const showPaymentModal = ref(false);
const selectedTier = ref(null);

onMounted(async () => {
  await Promise.all([
    fetchTiers(),
    fetchMySubscription(),
    fetchUsageStatus()
  ]);
});

const formatDate = (dateString) => {
  return new Date(dateString).toLocaleDateString('tr-TR');
};

const handleSubscribe = (tier) => {
  selectedTier.value = tier;
  showPaymentModal.value = true;
};

const handlePaymentSuccess = async (paymentData) => {
  try {
    const result = await subscribe(selectedTier.value.id, paymentData);
    if (result.success) {
      alert('Abonelik başarıyla oluşturuldu!');
      showPaymentModal.value = false;
    }
  } catch (error) {
    alert('Abonelik oluşturulurken hata oluştu: ' + error.message);
  }
};

const handleCancelSubscription = async () => {
  if (!confirm('Aboneliğinizi iptal etmek istediğinizden emin misiniz?')) {
    return;
  }

  try {
    const result = await cancelSubscription(
      currentSubscription.value.id,
      'Kullanıcı talebi'
    );
    if (result.success) {
      alert('Abonelik dönem sonunda iptal edilecek.');
    }
  } catch (error) {
    alert('İptal işlemi başarısız: ' + error.message);
  }
};
</script>
```

## Ödeme Gateway Entegrasyonu

### 1. Stripe Entegrasyonu

#### Backend (StripePaymentService.cs)
```csharp
using Stripe;
using Stripe.Checkout;

public class StripePaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    
    public StripePaymentService(IConfiguration configuration)
    {
        _configuration = configuration;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<PaymentResult> CreatePaymentSession(PaymentRequest request)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency.ToLower(),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.ProductName,
                            Description = request.Description
                        },
                        UnitAmount = (long)(request.Amount * 100), // Stripe kuruş cinsinden çalışır
                    },
                    Quantity = 1,
                },
            },
            Mode = "payment",
            SuccessUrl = $"{_configuration["App:Url"]}/subscription/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{_configuration["App:Url"]}/subscription/cancel",
            Metadata = new Dictionary<string, string>
            {
                { "userId", request.UserId.ToString() },
                { "subscriptionTierId", request.SubscriptionTierId.ToString() }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return new PaymentResult
        {
            Success = true,
            SessionId = session.Id,
            PaymentUrl = session.Url,
            TransactionId = session.PaymentIntentId
        };
    }

    public async Task<PaymentVerification> VerifyPayment(string sessionId)
    {
        var service = new SessionService();
        var session = await service.GetAsync(sessionId);

        return new PaymentVerification
        {
            Success = session.PaymentStatus == "paid",
            TransactionId = session.PaymentIntentId,
            Amount = session.AmountTotal / 100m,
            Currency = session.Currency.ToUpper(),
            Metadata = session.Metadata
        };
    }
}
```

#### Frontend Stripe Integration (React)
```javascript
import { loadStripe } from '@stripe/stripe-js';

const stripePromise = loadStripe(process.env.REACT_APP_STRIPE_PUBLIC_KEY);

const StripePayment = ({ tier, onSuccess, onError }) => {
  const handlePayment = async () => {
    try {
      // Backend'den payment session oluştur
      const response = await fetch('/api/payments/create-session', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('authToken')}`
        },
        body: JSON.stringify({
          amount: tier.monthlyPrice,
          currency: tier.currency,
          productName: `ZiraAI ${tier.displayName} Plan`,
          description: `${tier.monthlyRequestLimit} aylık analiz hakkı`,
          subscriptionTierId: tier.id
        })
      });

      const { sessionId } = await response.json();

      // Stripe checkout'a yönlendir
      const stripe = await stripePromise;
      const { error } = await stripe.redirectToCheckout({ sessionId });

      if (error) {
        onError(error.message);
      }
    } catch (error) {
      onError(error.message);
    }
  };

  return (
    <button onClick={handlePayment} className="stripe-payment-btn">
      Stripe ile Öde
    </button>
  );
};
```

### 2. Iyzico Entegrasyonu

#### Backend (IyzicoPaymentService.cs)
```csharp
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;

public class IyzicoPaymentService : IPaymentService
{
    private readonly Options _options;
    
    public IyzicoPaymentService(IConfiguration configuration)
    {
        _options = new Options
        {
            ApiKey = configuration["Iyzico:ApiKey"],
            SecretKey = configuration["Iyzico:SecretKey"],
            BaseUrl = configuration["Iyzico:BaseUrl"]
        };
    }

    public async Task<PaymentResult> CreatePayment(PaymentRequest request)
    {
        var paymentRequest = new CreatePaymentRequest
        {
            Locale = Locale.TR.ToString(),
            ConversationId = Guid.NewGuid().ToString(),
            Price = request.Amount.ToString(CultureInfo.InvariantCulture),
            PaidPrice = request.Amount.ToString(CultureInfo.InvariantCulture),
            Currency = Currency.TRY.ToString(),
            Installment = 1,
            BasketId = request.SubscriptionTierId.ToString(),
            PaymentChannel = PaymentChannel.WEB.ToString(),
            PaymentGroup = PaymentGroup.SUBSCRIPTION.ToString(),
            CallbackUrl = $"{_configuration["App:Url"]}/api/payments/iyzico-callback"
        };

        // Kart bilgileri
        paymentRequest.PaymentCard = new PaymentCard
        {
            CardHolderName = request.CardHolderName,
            CardNumber = request.CardNumber,
            ExpireMonth = request.ExpireMonth,
            ExpireYear = request.ExpireYear,
            Cvc = request.Cvc,
            RegisterCard = 0
        };

        // Alıcı bilgileri
        paymentRequest.Buyer = new Buyer
        {
            Id = request.UserId.ToString(),
            Name = request.UserName,
            Surname = request.UserSurname,
            Email = request.UserEmail,
            IdentityNumber = request.IdentityNumber,
            RegistrationAddress = request.Address,
            City = request.City,
            Country = "Turkey",
            Ip = request.IpAddress
        };

        // Ürün bilgileri
        var basketItems = new List<BasketItem>();
        basketItems.Add(new BasketItem
        {
            Id = request.SubscriptionTierId.ToString(),
            Name = request.ProductName,
            Category1 = "Subscription",
            ItemType = BasketItemType.VIRTUAL.ToString(),
            Price = request.Amount.ToString(CultureInfo.InvariantCulture)
        });
        paymentRequest.BasketItems = basketItems;

        var payment = Payment.Create(paymentRequest, _options);

        return new PaymentResult
        {
            Success = payment.Status == "success",
            TransactionId = payment.PaymentId,
            ErrorMessage = payment.ErrorMessage
        };
    }
}
```

## Hata Yönetimi

### Yaygın Hatalar ve Çözümleri

#### 1. Abonelik Bulunamadı
```json
{
  "success": false,
  "message": "No active subscription found",
  "errorCode": "SUB_NOT_FOUND",
  "suggestion": "Lütfen bir abonelik paketi satın alın"
}
```

#### 2. Kota Aşımı
```json
{
  "success": false,
  "message": "Daily request limit exceeded",
  "errorCode": "QUOTA_EXCEEDED",
  "details": {
    "limitType": "daily",
    "used": 20,
    "limit": 20,
    "resetTime": "2025-08-14T00:00:00"
  }
}
```

#### 3. Ödeme Hatası
```json
{
  "success": false,
  "message": "Payment failed",
  "errorCode": "PAYMENT_FAILED",
  "details": {
    "provider": "Stripe",
    "reason": "Insufficient funds"
  }
}
```

#### 4. Yetkilendirme Hatası
```json
{
  "success": false,
  "message": "Unauthorized",
  "errorCode": "AUTH_FAILED",
  "details": {
    "reason": "Token expired",
    "expiredAt": "2025-08-13T18:30:00"
  }
}
```

### Frontend Hata Yönetimi Örneği
```javascript
class SubscriptionErrorHandler {
  static handle(error) {
    const errorCode = error.response?.data?.errorCode;
    
    switch (errorCode) {
      case 'SUB_NOT_FOUND':
        return {
          title: 'Abonelik Bulunamadı',
          message: 'Aktif bir aboneliğiniz bulunmuyor. Hizmetlerimizden faydalanmak için abonelik satın alın.',
          action: 'subscribe'
        };
        
      case 'QUOTA_EXCEEDED':
        const details = error.response.data.details;
        const resetTime = new Date(details.resetTime).toLocaleString('tr-TR');
        return {
          title: 'Kota Aşıldı',
          message: `${details.limitType === 'daily' ? 'Günlük' : 'Aylık'} limitiniz doldu. Sıfırlama zamanı: ${resetTime}`,
          action: 'upgrade'
        };
        
      case 'PAYMENT_FAILED':
        return {
          title: 'Ödeme Başarısız',
          message: error.response.data.details.reason || 'Ödeme işlemi başarısız oldu.',
          action: 'retry'
        };
        
      case 'AUTH_FAILED':
        return {
          title: 'Oturum Süresi Doldu',
          message: 'Lütfen tekrar giriş yapın.',
          action: 'login'
        };
        
      default:
        return {
          title: 'Beklenmeyen Hata',
          message: error.message || 'Bir hata oluştu. Lütfen daha sonra tekrar deneyin.',
          action: 'retry'
        };
    }
  }
}

// Kullanım
try {
  const result = await subscriptionService.subscribe(data);
  // ...
} catch (error) {
  const errorInfo = SubscriptionErrorHandler.handle(error);
  
  // Kullanıcıya göster
  showErrorModal({
    title: errorInfo.title,
    message: errorInfo.message,
    buttons: [
      {
        text: errorInfo.action === 'subscribe' ? 'Abonelik Satın Al' : 'Tamam',
        onClick: () => {
          if (errorInfo.action === 'subscribe') {
            router.push('/subscription');
          }
        }
      }
    ]
  });
}
```

## Test Senaryoları

### 1. Kayıt ve Trial Abonelik Testi
```bash
# Test kullanıcısı oluştur
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!",
    "fullName": "Test User",
    "userRole": "Farmer"
  }'

# Login yap
curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!"
  }'

# Token'ı al ve abonelik durumunu kontrol et
curl -X GET http://localhost:5000/api/subscriptions/my-subscription \
  -H "Authorization: Bearer {token}"
```

### 2. Abonelik Satın Alma Testi
```javascript
// test-subscription.js
const testSubscriptionPurchase = async () => {
  const token = 'your-test-token';
  
  // 1. Paketleri listele
  const tiers = await fetch('/api/subscriptions/tiers').then(r => r.json());
  console.log('Available tiers:', tiers);
  
  // 2. M paketi seç
  const mediumTier = tiers.data.find(t => t.tierName === 'M');
  
  // 3. Mock ödeme işlemi
  const paymentResult = {
    success: true,
    transactionId: 'TEST-TXN-' + Date.now()
  };
  
  // 4. Abonelik oluştur
  const subscription = await fetch('/api/subscriptions/subscribe', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      subscriptionTierId: mediumTier.id,
      durationMonths: 1,
      autoRenew: true,
      paymentMethod: 'TEST',
      paymentReference: paymentResult.transactionId,
      isTrialSubscription: false
    })
  }).then(r => r.json());
  
  console.log('Subscription created:', subscription);
  
  // 5. Kullanım durumunu kontrol et
  const usage = await fetch('/api/subscriptions/usage-status', {
    headers: { 'Authorization': `Bearer ${token}` }
  }).then(r => r.json());
  
  console.log('Usage status:', usage);
};

testSubscriptionPurchase();
```

### 3. Kota Kontrolü Testi
```python
# test_quota_limits.py
import requests
import time

class QuotaTest:
    def __init__(self, api_url, token):
        self.api_url = api_url
        self.headers = {
            'Authorization': f'Bearer {token}',
            'Content-Type': 'application/json'
        }
    
    def test_daily_limit(self):
        """Günlük limit testi"""
        usage = self.get_usage_status()
        daily_limit = usage['data']['usage']['daily']['limit']
        
        print(f"Daily limit: {daily_limit}")
        
        # Limite kadar istek gönder
        for i in range(daily_limit + 1):
            response = self.make_analysis_request()
            
            if i < daily_limit:
                assert response['success'] == True, f"Request {i+1} should succeed"
                print(f"✓ Request {i+1}/{daily_limit} successful")
            else:
                assert response['success'] == False, f"Request {i+1} should fail"
                assert 'limit' in response['message'].lower()
                print(f"✓ Request {i+1} correctly rejected (limit exceeded)")
            
            time.sleep(1)  # Rate limiting
    
    def make_analysis_request(self):
        """Mock analiz isteği"""
        return requests.post(
            f"{self.api_url}/api/plantanalyses/analyze",
            headers=self.headers,
            json={
                "image": "data:image/jpeg;base64,/9j/4AAQ...",
                "farmerId": "TEST001",
                "cropType": "test"
            }
        ).json()
    
    def get_usage_status(self):
        """Kullanım durumunu al"""
        return requests.get(
            f"{self.api_url}/api/subscriptions/usage-status",
            headers=self.headers
        ).json()

# Test çalıştır
if __name__ == "__main__":
    tester = QuotaTest('http://localhost:5000', 'your-test-token')
    tester.test_daily_limit()
```

### 4. Sponsor Özellikleri Testi
```bash
# 1. Sponsor olarak kayıt ol
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "sponsor@company.com",
    "password": "Sponsor123!",
    "fullName": "Test Sponsor Company",
    "userRole": "Sponsor"
  }'

# 2. Sponsor token al
SPONSOR_TOKEN=$(curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "sponsor@company.com",
    "password": "Sponsor123!"
  }' | jq -r '.data.token')

# 3. Sponsorlu analizleri görüntüle
curl -X GET http://localhost:5000/api/subscriptions/sponsored-analyses \
  -H "Authorization: Bearer $SPONSOR_TOKEN"
```

## Sorun Giderme

### Sık Karşılaşılan Sorunlar

#### 1. "No active subscription" Hatası
**Sebep**: Kullanıcının aktif aboneliği yok veya süresi dolmuş.
**Çözüm**: 
- Kullanıcının abonelik durumunu kontrol edin
- Trial abonelik oluşturulmuş mı kontrol edin
- Veritabanında `UserSubscriptions` tablosunu kontrol edin

#### 2. Kota Sıfırlanmıyor
**Sebep**: Scheduled job çalışmıyor olabilir.
**Çözüm**:
- Hangfire dashboard'u kontrol edin
- `ResetDailyUsageJob` ve `ResetMonthlyUsageJob` durumlarını kontrol edin
- Manuel sıfırlama için admin endpoint kullanın

#### 3. Ödeme Başarılı Ama Abonelik Oluşturulmuyor
**Sebep**: Webhook konfigürasyonu yanlış olabilir.
**Çözüm**:
- Payment gateway webhook URL'ini kontrol edin
- Webhook secret key'i doğrulayın
- Server loglarını inceleyin

#### 4. Foreign Key Constraint Hatası
**Sebep**: `SubscriptionUsageLogs` tablosunda geçersiz `UserSubscriptionId`.
**Çözüm**:
- Kullanıcının aktif aboneliği olduğundan emin olun
- `ValidateAndLogUsageAsync` metodunda null check yapın

## Performans Optimizasyonu

### 1. Caching Stratejisi
```csharp
// Redis cache kullanımı
public class CachedSubscriptionService
{
    private readonly IMemoryCache _cache;
    private readonly ISubscriptionService _subscriptionService;
    
    public async Task<UsageStatus> GetUsageStatusAsync(int userId)
    {
        var cacheKey = $"usage_status_{userId}";
        
        if (!_cache.TryGetValue(cacheKey, out UsageStatus cachedStatus))
        {
            cachedStatus = await _subscriptionService.GetUsageStatusAsync(userId);
            
            _cache.Set(cacheKey, cachedStatus, TimeSpan.FromMinutes(1));
        }
        
        return cachedStatus;
    }
}
```

### 2. Database Query Optimization
```sql
-- Index oluştur
CREATE INDEX idx_user_subscriptions_active 
ON "UserSubscriptions" ("UserId", "IsActive") 
WHERE "IsActive" = true;

CREATE INDEX idx_usage_logs_date 
ON "SubscriptionUsageLogs" ("UserId", "UsageDate");
```

### 3. Batch Processing
```csharp
// Toplu kullanım güncellemesi
public async Task BatchUpdateUsageAsync(List<int> userIds)
{
    var query = @"
        UPDATE ""UserSubscriptions"" 
        SET ""CurrentDailyUsage"" = 0,
            ""LastUsageResetDate"" = @resetDate
        WHERE ""UserId"" = ANY(@userIds) 
        AND ""IsActive"" = true";
    
    await _context.Database.ExecuteSqlRawAsync(query, 
        new NpgsqlParameter("@userIds", userIds.ToArray()),
        new NpgsqlParameter("@resetDate", DateTime.Now));
}
```

## Güvenlik Önlemleri

### 1. Rate Limiting
```csharp
// Startup.cs
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("subscription-api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
    });
});
```

### 2. Input Validation
```csharp
[ApiController]
public class SubscriptionController : ControllerBase
{
    [HttpPost("subscribe")]
    [ValidateModel]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        if (request.DurationMonths < 1 || request.DurationMonths > 12)
        {
            return BadRequest("Duration must be between 1 and 12 months");
        }
        // ...
    }
}
```

### 3. Audit Logging
```csharp
public async Task LogSubscriptionActivity(SubscriptionActivity activity)
{
    var log = new AuditLog
    {
        UserId = activity.UserId,
        Action = activity.Action,
        Details = JsonSerializer.Serialize(activity.Details),
        IpAddress = activity.IpAddress,
        UserAgent = activity.UserAgent,
        Timestamp = DateTime.UtcNow
    };
    
    await _auditRepository.AddAsync(log);
}
```

## Sonuç

Bu dokümantasyon, ZiraAI abonelik sisteminin tüm yönlerini kapsamaktadır. Sistem, kurumsal düzeyde güvenilir, ölçeklenebilir ve kullanıcı dostu bir abonelik yönetimi sağlar. Frontend entegrasyonları, ödeme gateway'leri ve detaylı test senaryoları ile birlikte production-ready bir çözüm sunulmaktadır.

### Önemli Linkler
- API Dokümantasyonu: `/swagger`
- Hangfire Dashboard: `/hangfire`
- Admin Panel: `/admin/subscriptions`
- Destek: `support@ziraai.com`

### Versiyon Bilgisi
- Dokümantasyon Versiyonu: 1.0.0
- Son Güncelleme: Ağustos 2025
- API Versiyonu: v1
- .NET Versiyonu: 9.0