# 🚀 ZiraAI Sponsorluk Sistemi - Hızlı Başlangıç Kılavuzu
## ✅ **CORRECTED ARCHITECTURE v2.0**

> **⚠️ ÖNEMLİ GÜNCELLEME:**
> Bu kılavuz, düzeltilmiş sponsorluk sistemi mimarisini yansıtır.
> **Doğru Akış**: Tek şirket profili → Çoklu paket satın alma → Kod dağıtımı → Özellik aktivasyonu

---

## 📋 İçindekiler
- [Hızlı Kurulum](#hızlı-kurulum)
- [Yeni İş Akışı](#yeni-iş-akışı)
- [API Test Örnekleri](#api-test-örnekleri)
- [Frontend Entegrasyonu](#frontend-entegrasyonu)
- [Sık Sorulan Sorular](#sık-sorulan-sorular)

---

## 🚀 Hızlı Kurulum

### 1. Database Migration
```bash
# Yeni sponsorluk tablolarını oluştur
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext

# Migration script'i manuel çalıştır (varsa mevcut data için)
dotnet script migrate_sponsorship_v2.csx
```

### 2. Servis Kayıtları
`Business/DependencyResolvers/AutofacBusinessModule.cs` dosyasına ekleyin:

```csharp
// Corrected Sponsorship Services
builder.RegisterType<SponsorVisibilityService>().As<ISponsorVisibilityService>().InstancePerLifetimeScope();
builder.RegisterType<SponsorDataAccessService>().As<ISponsorDataAccessService>().InstancePerLifetimeScope();
builder.RegisterType<AnalysisMessagingService>().As<IAnalysisMessagingService>().InstancePerLifetimeScope();
builder.RegisterType<SmartLinkService>().As<ISmartLinkService>().InstancePerLifetimeScope();

// Repository registrations
builder.RegisterType<SponsorProfileRepository>().As<ISponsorProfileRepository>().InstancePerLifetimeScope();
builder.RegisterType<SponsorshipPurchaseRepository>().As<ISponsorshipPurchaseRepository>().InstancePerLifetimeScope();
builder.RegisterType<SponsorshipCodeRepository>().As<ISponsorshipCodeRepository>().InstancePerLifetimeScope();
```

### 3. Postman Koleksiyonu
- [ZiraAI_Postman_Collection_v1.5.0.json](./ZiraAI_Postman_Collection_v1.5.0.json) dosyasını import edin
- Environment variables:
  - `baseUrl`: https://localhost:5001
  - `accessToken`: Login sonrası otomatik
  - `sponsorId`: Sponsor ID
  - `plantAnalysisId`: Test için kullanılacak analiz ID

---

## 🔄 Yeni İş Akışı

### Sponsor Tarafı

#### Adım 1: Sponsor Kayıt ve Giriş
```bash
# Sponsor olarak kayıt ol
POST /api/v1/auth/register
{
  "email": "sponsor@company.com",
  "password": "SecurePass123!",
  "fullName": "Sponsor Company",
  "role": "Sponsor"
}

# Giriş yap
POST /api/v1/auth/login
{
  "email": "sponsor@company.com",
  "password": "SecurePass123!"
}
```

#### Adım 2: Şirket Profili Oluştur (Tek Seferlik)
```bash
POST /api/sponsorships/create-profile
Authorization: Bearer {token}
{
  "companyName": "Tarım Teknoloji A.Ş.",
  "companyDescription": "Modern tarım çözümleri sağlayıcısı",
  "sponsorLogoUrl": "https://example.com/logo.png",
  "websiteUrl": "https://tarimtech.com.tr",
  "contactEmail": "info@tarimtech.com.tr",
  "contactPhone": "+90 212 555 0000",
  "contactPerson": "Ahmet Yılmaz",
  "companyType": "Agriculture",
  "businessModel": "B2B"
}
```

#### Adım 3: Paket Satın Al (İhtiyaç Kadar)
```bash
# S Paketi satın al (100 kod)
POST /api/sponsorships/purchase-package
{
  "subscriptionTierId": 1,
  "quantity": 100,
  "unitPrice": 29.99,
  "totalAmount": 2999.00,
  "paymentMethod": "CreditCard"
}

# M Paketi satın al (50 kod)
POST /api/sponsorships/purchase-package
{
  "subscriptionTierId": 2,
  "quantity": 50,
  "unitPrice": 59.99,
  "totalAmount": 2999.50,
  "paymentMethod": "CreditCard"
}

# L Paketi satın al (20 kod)
POST /api/sponsorships/purchase-package
{
  "subscriptionTierId": 3,
  "quantity": 20,
  "unitPrice": 99.99,
  "totalAmount": 1999.80,
  "paymentMethod": "CreditCard"
}

# XL Paketi satın al (10 kod)
POST /api/sponsorships/purchase-package
{
  "subscriptionTierId": 4,
  "quantity": 10,
  "unitPrice": 149.99,
  "totalAmount": 1499.90,
  "paymentMethod": "CreditCard"
}
```

#### Adım 4: Kodları Çiftçilere Dağıt
```bash
# Kullanılmamış kodları listele
GET /api/sponsorships/codes?onlyUnused=true

# Kodları SMS/WhatsApp ile gönder
POST /api/sponsorships/send-codes
{
  "recipients": [
    {
      "phoneNumber": "+90 555 111 2233",
      "farmerName": "Mehmet Çiftçi",
      "code": "AGRI-S-001"
    },
    {
      "phoneNumber": "+90 555 444 5566",
      "farmerName": "Ali Üretici",
      "code": "AGRI-M-001"
    }
  ],
  "channel": "SMS"
}
```

### Farmer (Çiftçi) Tarafı

#### Adım 1: Kodu Doğrula
```bash
GET /api/sponsorships/validate/AGRI-M-001

Response:
{
  "success": true,
  "data": {
    "code": "AGRI-M-001",
    "tierName": "M",
    "tierFeatures": {
      "logoOnStart": true,
      "logoOnResult": true,
      "dataAccessPercentage": 30,
      "messagingEnabled": true
    },
    "expiryDate": "2026-08-15T00:00:00Z"
  }
}
```

#### Adım 2: Kodu Kullanarak Analiz Yap
```bash
POST /api/v1/plantanalyses/analyze
Authorization: Bearer {farmer_token}
{
  "image": "data:image/jpeg;base64,/9j/4AAQ...",
  "sponsorshipCode": "AGRI-M-001",
  "farmerId": "F001",
  "cropType": "tomato"
}

Response:
{
  "success": true,
  "data": {
    "plantAnalysisId": 456,
    "sponsorshipCodeId": 1001,
    "tierName": "M",
    "analysisResult": {...}
  }
}
```

#### Adım 3: Sponsor Logosunu Görüntüle
```bash
# Analiz sonuç ekranında logo kontrolü
GET /api/sponsorships/display-info/analysis/456?screen=result

Response:
{
  "success": true,
  "data": {
    "canDisplay": true,
    "tierName": "M",
    "sponsorInfo": {
      "companyName": "Tarım Teknoloji A.Ş.",
      "sponsorLogoUrl": "https://example.com/logo.png",
      "websiteUrl": "https://tarimtech.com.tr"
    }
  }
}
```

---

## 🧪 API Test Örnekleri

### Paket Özelliklerini Test Etme

#### S Paketi Testi
```javascript
// S paketi kodu ile yapılan analizde
const analysisId = 456; // S kodu ile yapılan analiz

// Logo kontrolü - Result screen (başarılı olmalı)
fetch(`/api/sponsorships/display-info/analysis/${analysisId}?screen=result`)
  .then(res => res.json())
  .then(data => {
    console.assert(data.data.canDisplay === true, "S tier should show logo on result");
  });

// Logo kontrolü - Start screen (başarısız olmalı)
fetch(`/api/sponsorships/display-info/analysis/${analysisId}?screen=start`)
  .then(res => res.json())
  .then(data => {
    console.assert(data.data.canDisplay === false, "S tier should NOT show logo on start");
  });

// Veri erişimi (%30)
fetch(`/api/sponsorships/analysis/${analysisId}/filtered`)
  .then(res => res.json())
  .then(data => {
    console.assert(data.data.accessLevel === "30%", "S tier should have 30% data access");
  });
```

#### M Paketi Testi
```javascript
// M paketi özellikleri
const mAnalysisId = 789;

// Logo hem start hem result'ta görünmeli
// Mesajlaşma aktif olmalı
// Veri erişimi %30

// Mesajlaşma testi
fetch('/api/sponsorships/messages', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${sponsorToken}`
  },
  body: JSON.stringify({
    toUserId: farmerId,
    plantAnalysisId: mAnalysisId,
    message: "Analizinize göre önerilerimiz var."
  })
})
.then(res => {
  console.assert(res.ok, "M tier should allow messaging");
});
```

#### XL Paketi Testi
```javascript
// XL paketi - tüm özellikler aktif
const xlAnalysisId = 999;

// Smart link oluşturma (sadece XL)
fetch('/api/sponsorships/smart-links', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${sponsorToken}`
  },
  body: JSON.stringify({
    linkUrl: "https://tarimtech.com.tr/ozel-urun",
    linkText: "Özel İndirim",
    keywords: ["azot", "gübre"],
    productName: "Premium Gübre",
    productPrice: 299.99
  })
})
.then(res => {
  console.assert(res.ok, "XL tier should allow smart links");
});

// %100 veri erişimi
fetch(`/api/sponsorships/analysis/${xlAnalysisId}/filtered`)
  .then(res => res.json())
  .then(data => {
    console.assert(data.data.accessLevel === "100%", "XL tier should have 100% data access");
    console.assert(data.data.visibleData.gpsCoordinates !== undefined, "Should see GPS data");
    console.assert(data.data.visibleData.farmerContact !== undefined, "Should see farmer contact");
  });
```

---

## 💻 Frontend Entegrasyonu

### React - Sponsor Dashboard
```jsx
import React, { useState, useEffect } from 'react';

const SponsorDashboard = () => {
  const [profile, setProfile] = useState(null);
  const [purchases, setPurchases] = useState([]);
  const [codes, setCodes] = useState([]);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    // Profil bilgisi
    const profileRes = await fetch('/api/sponsorships/profile', {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
    });
    const profileData = await profileRes.json();
    setProfile(profileData.data);

    // Satın almalar
    const purchasesRes = await fetch('/api/sponsorships/purchases', {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
    });
    const purchasesData = await purchasesRes.json();
    setPurchases(purchasesData.data);

    // Kodlar
    const codesRes = await fetch('/api/sponsorships/codes?onlyUnused=true', {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
    });
    const codesData = await codesRes.json();
    setCodes(codesData.data.codes);
  };

  const handlePurchase = async (tierId, tierName, quantity) => {
    const unitPrices = { S: 29.99, M: 59.99, L: 99.99, XL: 149.99 };
    const unitPrice = unitPrices[tierName];
    
    const response = await fetch('/api/sponsorships/purchase-package', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      },
      body: JSON.stringify({
        subscriptionTierId: tierId,
        quantity: quantity,
        unitPrice: unitPrice,
        totalAmount: unitPrice * quantity,
        paymentMethod: 'CreditCard'
      })
    });

    if (response.ok) {
      alert(`${quantity} adet ${tierName} paketi kodu oluşturuldu!`);
      loadDashboardData();
    }
  };

  return (
    <div className="sponsor-dashboard">
      {/* Company Profile Section */}
      <div className="profile-section">
        <h2>{profile?.companyName}</h2>
        <div className="stats-grid">
          <div className="stat-card">
            <span className="stat-value">{profile?.totalPurchases}</span>
            <span className="stat-label">Toplam Satın Alma</span>
          </div>
          <div className="stat-card">
            <span className="stat-value">{profile?.totalCodesGenerated}</span>
            <span className="stat-label">Üretilen Kod</span>
          </div>
          <div className="stat-card">
            <span className="stat-value">{profile?.totalCodesRedeemed}</span>
            <span className="stat-label">Kullanılan Kod</span>
          </div>
          <div className="stat-card">
            <span className="stat-value">₺{profile?.totalInvestment}</span>
            <span className="stat-label">Toplam Yatırım</span>
          </div>
        </div>
      </div>

      {/* Package Purchase Section */}
      <div className="purchase-section">
        <h3>Paket Satın Al</h3>
        <div className="package-grid">
          {['S', 'M', 'L', 'XL'].map((tier, index) => (
            <PackageCard
              key={tier}
              tier={tier}
              tierId={index + 1}
              onPurchase={handlePurchase}
            />
          ))}
        </div>
      </div>

      {/* Purchase History */}
      <div className="history-section">
        <h3>Satın Alma Geçmişi</h3>
        <table className="purchase-table">
          <thead>
            <tr>
              <th>Paket</th>
              <th>Adet</th>
              <th>Kullanılan</th>
              <th>Kalan</th>
              <th>Tarih</th>
            </tr>
          </thead>
          <tbody>
            {purchases.map(p => (
              <tr key={p.purchaseId}>
                <td>{p.tierName}</td>
                <td>{p.quantity}</td>
                <td>{p.codesUsed}</td>
                <td>{p.quantity - p.codesUsed}</td>
                <td>{new Date(p.purchaseDate).toLocaleDateString('tr-TR')}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Available Codes */}
      <div className="codes-section">
        <h3>Kullanılmamış Kodlar</h3>
        <div className="codes-grid">
          {codes.map(code => (
            <div key={code.code} className="code-card">
              <span className="code-text">{code.code}</span>
              <span className="code-tier">{code.tierName} Paketi</span>
              <button onClick={() => copyToClipboard(code.code)}>
                Kopyala
              </button>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

const PackageCard = ({ tier, tierId, onPurchase }) => {
  const [quantity, setQuantity] = useState(10);
  
  const features = {
    S: ['Sonuç ekranında logo', '%30 veri erişimi'],
    M: ['Başlangıç + Sonuç logo', '%30 veri', 'Mesajlaşma'],
    L: ['Tüm ekranlarda logo', '%60 veri', 'Gelişmiş mesajlaşma'],
    XL: ['Tüm özellikler', '%100 veri', 'AI Smart Links', 'Premium analitik']
  };

  const prices = { S: 29.99, M: 59.99, L: 99.99, XL: 149.99 };

  return (
    <div className={`package-card tier-${tier.toLowerCase()}`}>
      <h4>{tier} Paketi</h4>
      <div className="price">₺{prices[tier]}/kod</div>
      <ul className="features">
        {features[tier].map((f, i) => (
          <li key={i}>{f}</li>
        ))}
      </ul>
      <input
        type="number"
        value={quantity}
        onChange={(e) => setQuantity(parseInt(e.target.value))}
        min="1"
        max="1000"
      />
      <div className="total">Toplam: ₺{(prices[tier] * quantity).toFixed(2)}</div>
      <button onClick={() => onPurchase(tierId, tier, quantity)}>
        Satın Al
      </button>
    </div>
  );
};
```

### React - Farmer Analysis Component
```jsx
const FarmerAnalysis = () => {
  const [sponsorshipCode, setSponsorshipCode] = useState('');
  const [codeInfo, setCodeInfo] = useState(null);
  const [image, setImage] = useState(null);

  const validateCode = async () => {
    const response = await fetch(`/api/sponsorships/validate/${sponsorshipCode}`);
    const data = await response.json();
    
    if (data.success) {
      setCodeInfo(data.data);
      toast.success('Kod doğrulandı! Analiz yapabilirsiniz.');
    } else {
      toast.error('Geçersiz veya kullanılmış kod!');
    }
  };

  const startAnalysis = async () => {
    const formData = new FormData();
    formData.append('image', image);
    formData.append('sponsorshipCode', sponsorshipCode);
    formData.append('cropType', 'tomato');

    const response = await fetch('/api/v1/plantanalyses/analyze', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      },
      body: formData
    });

    if (response.ok) {
      const result = await response.json();
      // Analiz sonuç sayfasına yönlendir
      window.location.href = `/analysis/${result.data.plantAnalysisId}`;
    }
  };

  return (
    <div className="farmer-analysis">
      <h2>Sponsorlu Analiz</h2>
      
      <div className="code-section">
        <label>Sponsorluk Kodu</label>
        <input
          type="text"
          value={sponsorshipCode}
          onChange={(e) => setSponsorshipCode(e.target.value)}
          placeholder="AGRI-XXX-000"
        />
        <button onClick={validateCode}>Kodu Doğrula</button>
      </div>

      {codeInfo && (
        <div className="code-info">
          <div className="alert alert-success">
            ✅ Kod geçerli! <strong>{codeInfo.tierName} Paketi</strong>
          </div>
          <ul>
            {codeInfo.tierFeatures.logoOnStart && <li>Başlangıç ekranında sponsor logosu</li>}
            <li>Sonuç ekranında sponsor logosu</li>
            <li>%{codeInfo.tierFeatures.dataAccessPercentage} veri erişimi</li>
            {codeInfo.tierFeatures.messagingEnabled && <li>Sponsor ile mesajlaşma</li>}
            {codeInfo.tierFeatures.smartLinksEnabled && <li>Akıllı ürün önerileri</li>}
          </ul>
          
          <div className="image-upload">
            <label>Analiz için fotoğraf yükleyin</label>
            <input
              type="file"
              accept="image/*"
              onChange={(e) => setImage(e.target.files[0])}
            />
          </div>
          
          {image && (
            <button className="btn-primary" onClick={startAnalysis}>
              Analizi Başlat
            </button>
          )}
        </div>
      )}
    </div>
  );
};
```

---

## ❓ Sık Sorulan Sorular

### Q: Bir sponsor firma kaç farklı paket satın alabilir?
**A:** İstediği kadar! Aynı firma S, M, L ve XL paketlerinden dilediği miktarda satın alabilir. Her satın alma için ayrı kodlar üretilir.

### Q: Paketler arasındaki ana farklar nedir?
**A:** 
- **S**: Temel (sadece sonuç logosu, %30 veri)
- **M**: Orta (başlangıç+sonuç logosu, %30 veri, mesajlaşma)
- **L**: İleri (tüm ekranlar logo, %60 veri, gelişmiş mesajlaşma)
- **XL**: Premium (tüm özellikler, %100 veri, AI smart links)

### Q: Bir çiftçi hangi paketi kullandığını nasıl anlar?
**A:** Kod doğrulama endpoint'i (`GET /api/sponsorships/validate/{code}`) kodun hangi pakete ait olduğunu ve sağladığı özellikleri gösterir.

### Q: Logo görünürlüğü nasıl kontrol ediliyor?
**A:** `GET /api/sponsorships/display-info/analysis/{id}?screen={screenType}` endpoint'i kullanılan kodun tier'ına göre logo gösterilip gösterilmeyeceğini belirler.

### Q: Veri erişim filtreleme nasıl çalışıyor?
**A:** 
- %30: Temel bilgiler (bitki türü, sağlık skoru, tarih)
- %60: + Hastalık/zararlı detayları, öneriler, hava durumu
- %100: + GPS, çiftçi iletişim, detaylı çevresel veriler

### Q: Mevcut sistemden nasıl migrate edilir?
**A:** Migration guide dokümantasyonunda detaylı adımlar mevcut. Temel olarak:
1. SponsorProfile entity'sinden tier bağımlılığı kaldırılır
2. Mevcut tier bilgileri SponsorshipPurchase'a taşınır
3. API endpoint'ler güncellenir
4. Frontend analysis-based endpoint'lere geçirilir

### Q: Test ortamında nasıl test edilir?
**A:** 
1. Sponsor olarak giriş yap
2. Şirket profili oluştur (tek seferlik)
3. Farklı paketlerden satın al
4. Kodları dağıt
5. Farmer olarak kodu kullan ve analiz yap
6. Her tier'ın özelliklerini doğrula

### Q: Smart Links sadece XL pakette mi?
**A:** Evet, AI-powered smart link özelliği sadece XL paketinde mevcut. Bu özellik analiz sonuçlarına göre otomatik ürün önerisi yapar.

### Q: Mesajlaşma hangi paketlerde var?
**A:** M, L ve XL paketlerinde mesajlaşma özelliği aktif. S paketinde mesajlaşma yoktur.

### Q: Production deployment checklist?
**A:** 
- [ ] Database migration tamamlandı
- [ ] Service registrations yapıldı
- [ ] API endpoints güncellendi
- [ ] Frontend analysis-based endpoint'lere geçti
- [ ] Test senaryoları başarılı
- [ ] Health check endpoint'leri çalışıyor

---

## 📞 Destek

- **Dokümantasyon**: [SPONSORSHIP_SYSTEM_DOCUMENTATION.md](./SPONSORSHIP_SYSTEM_DOCUMENTATION.md)
- **Test Guide**: [SPONSORSHIP_TEST_GUIDE.md](./SPONSORSHIP_TEST_GUIDE.md)
- **API Koleksiyonu**: [ZiraAI_Postman_Collection_v1.5.0.json](./ZiraAI_Postman_Collection_v1.5.0.json)

---

**🎉 Corrected sponsorship system başarıyla entegre edildi!**

**Version**: 2.0 (Corrected Architecture)
**Last Updated**: August 2025
**Status**: Production Ready