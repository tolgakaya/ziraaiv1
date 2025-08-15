# 🚀 ZiraAI Sponsorluk Sistemi - Hızlı Başlangıç Kılavuzu

## 📋 İçindekiler
- [Hızlı Kurulum](#hızlı-kurulum)
- [API Testleri](#api-testleri)
- [Frontend Entegrasyonu](#frontend-entegrasyonu)
- [Sık Sorulan Sorular](#sik-sorulan-sorular)

---

## 🚀 Hızlı Kurulum

### 1. Database Migration
```bash
# Sponsorluk tablolarını oluştur
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext

# Seed data kontrolü
dotnet script check_sponsor_tables.csx
```

### 2. Servis Kayıtları
`Business/DependencyResolvers/AutofacBusinessModule.cs` dosyasına ekleyin:

```csharp
// Sponsorship Services
builder.RegisterType<SponsorVisibilityService>().As<ISponsorVisibilityService>().InstancePerLifetimeScope();
builder.RegisterType<SponsorDataAccessService>().As<ISponsorDataAccessService>().InstancePerLifetimeScope();
builder.RegisterType<AnalysisMessagingService>().As<IAnalysisMessagingService>().InstancePerLifetimeScope();
builder.RegisterType<SmartLinkService>().As<ISmartLinkService>().InstancePerLifetimeScope();
```

### 3. Postman Koleksiyonu
- [ZiraAI_Postman_Collection_v1.4.0.json](./ZiraAI_Postman_Collection_v1.4.0.json) dosyasını Postman'e import edin
- Environment variables ayarlayın:
  - `baseUrl`: https://localhost:5001
  - `accessToken`: (login sonrası otomatik dolar)
  - `sponsorId`: (sponsor login sonrası otomatik dolar)

---

## 🧪 API Testleri

### Adım 1: Authentication
```bash
# Admin olarak giriş
POST /api/v1/auth/login
{
  "email": "admin@ziraai.com",
  "password": "Admin123!"
}

# Sponsor olarak giriş  
POST /api/v1/auth/login
{
  "email": "sponsor@company.com",
  "password": "SponsorPassword123!"
}
```

### Adım 2: Sponsor Profile Oluşturma
```bash
# Sponsor profili oluştur (M paketi)
POST /api/sponsorships/create-profile
{
  "sponsorId": 123,
  "companyName": "Tarım Tech Ltd.",
  "companyDescription": "Akıllı tarım çözümleri",
  "sponsorLogoUrl": "https://example.com/logo.png",
  "websiteUrl": "https://tarimtech.com.tr",
  "contactEmail": "info@tarimtech.com.tr",
  "contactPhone": "+90 212 555 12 34",
  "contactPerson": "Ahmet Yılmaz",
  "currentSubscriptionTierId": 2
}
```

### Adım 3: Logo Görünürlük Testi
```bash
# Logo görünürlük kontrolü
GET /api/sponsorships/logo-permissions/123

# Analiz için logo bilgisi
GET /api/sponsorships/display-info/456?screen=result
```

### Adım 4: Veri Erişim Testi
```bash
# Filtrelenmiş analiz verisi (%30 erişim)
GET /api/sponsorships/filtered-analysis/123/456

# Erişim istatistikleri
GET /api/sponsorships/access-statistics/123
```

### Adım 5: Mesajlaşma Testi (L/XL Paketi)
```bash
# Mesaj gönder
POST /api/sponsorships/send-message
{
  "fromUserId": 123,
  "toUserId": 456,
  "plantAnalysisId": 789,
  "message": "Analizinize göre önerilerimiz var.",
  "messageType": "Information"
}

# Mesaj geçmişi
GET /api/sponsorships/conversation/123/456/789
```

### Adım 6: Smart Link Testi (XL Paketi)
```bash
# Smart link oluştur
POST /api/sponsorships/create-smart-link
{
  "sponsorId": 123,
  "linkUrl": "https://tarimtech.com.tr/azot-gubresi",
  "linkText": "Azot Gübresi - %25 İndirim!",
  "keywords": ["azot", "gübre", "domates"],
  "targetCropTypes": ["tomato"],
  "productName": "TarımTech Azot Plus",
  "productPrice": 149.99,
  "discountPercentage": 25
}

# Eşleşen linkler
GET /api/sponsorships/matching-links/789
```

---

## 💻 Frontend Entegrasyonu

### React Component Örnekleri

#### 1. Sponsor Logo Gösterimi
```jsx
const SponsorLogo = ({ plantAnalysisId, screenType }) => {
  const [sponsorInfo, setSponsorInfo] = useState(null);

  useEffect(() => {
    fetch(`/api/sponsorships/display-info/${plantAnalysisId}?screen=${screenType}`)
      .then(res => res.json())
      .then(data => {
        if (data.success) setSponsorInfo(data.data);
      });
  }, [plantAnalysisId, screenType]);

  if (!sponsorInfo) return null;

  return (
    <div className="sponsor-logo-container">
      <img 
        src={sponsorInfo.sponsorLogoUrl}
        alt={sponsorInfo.companyName}
        onClick={() => window.open(sponsorInfo.websiteUrl)}
      />
      <span>{sponsorInfo.companyName} sponsorluğunda</span>
    </div>
  );
};
```

#### 2. Smart Links
```jsx
const SmartLinks = ({ plantAnalysisId }) => {
  const [links, setLinks] = useState([]);

  useEffect(() => {
    fetch(`/api/sponsorships/matching-links/${plantAnalysisId}`)
      .then(res => res.json())
      .then(data => {
        if (data.success) setLinks(data.data);
      });
  }, [plantAnalysisId]);

  const handleClick = (link) => {
    // Tıklama kaydı
    fetch('/api/sponsorships/increment-click', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ smartLinkId: link.id })
    });
    
    window.open(link.linkUrl);
  };

  return (
    <div className="smart-links">
      <h3>🌱 Size Özel Öneriler</h3>
      {links.map(link => (
        <div key={link.id} className="product-card" onClick={() => handleClick(link)}>
          <h4>{link.productName}</h4>
          <p>{link.linkText}</p>
          <div className="price">
            ₺{link.productPrice}
            {link.discountPercentage && (
              <span className="discount">%{link.discountPercentage} İndirim</span>
            )}
          </div>
        </div>
      ))}
    </div>
  );
};
```

#### 3. Mesajlaşma Sistemi
```jsx
const SponsorMessaging = ({ plantAnalysisId, currentUserId, targetUserId }) => {
  const [messages, setMessages] = useState([]);
  const [newMessage, setNewMessage] = useState('');

  const sendMessage = async () => {
    const response = await fetch('/api/sponsorships/send-message', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        fromUserId: currentUserId,
        toUserId: targetUserId,
        plantAnalysisId: plantAnalysisId,
        message: newMessage
      })
    });
    
    if (response.ok) {
      setNewMessage('');
      loadMessages(); // Mesajları yenile
    }
  };

  return (
    <div className="sponsor-messaging">
      <div className="messages">
        {messages.map(msg => (
          <div key={msg.id} className={`message ${msg.senderRole.toLowerCase()}`}>
            <strong>{msg.senderName}</strong>
            <p>{msg.message}</p>
            <small>{new Date(msg.sentDate).toLocaleString()}</small>
          </div>
        ))}
      </div>
      <div className="message-input">
        <textarea
          value={newMessage}
          onChange={(e) => setNewMessage(e.target.value)}
          placeholder="Mesajınızı yazın..."
        />
        <button onClick={sendMessage}>Gönder</button>
      </div>
    </div>
  );
};
```

### CSS Stilleri
```css
.sponsor-logo-container {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px;
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
  border-radius: 8px;
}

.sponsor-logo-container img {
  max-height: 40px;
  cursor: pointer;
  transition: transform 0.3s ease;
}

.sponsor-logo-container img:hover {
  transform: scale(1.05);
}

.smart-links .product-card {
  background: white;
  border-radius: 8px;
  padding: 15px;
  margin: 10px 0;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
  cursor: pointer;
  transition: transform 0.3s ease;
}

.smart-links .product-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 15px rgba(0,0,0,0.15);
}

.sponsor-messaging .message {
  background: white;
  padding: 12px;
  margin: 10px 0;
  border-radius: 8px;
  border-left: 3px solid #dee2e6;
}

.sponsor-messaging .message.sponsor {
  border-left-color: #007bff;
}

.sponsor-messaging .message.farmer {
  border-left-color: #28a745;
}
```

---

## ❓ Sık Sorulan Sorular

### Q: Sponsorluk paketleri arasındaki farklar nelerdir?
**A:** 4 farklı paket var:
- **S Paketi**: Sadece sonuç ekranında logo, %30 veri erişimi
- **M Paketi**: Başlangıç + sonuç ekranında logo, %30 veri + mesajlaşma
- **L Paketi**: Tüm ekranlarda logo, %60 veri + tam mesajlaşma
- **XL Paketi**: Tüm özellikler + %100 veri + AI akıllı linkler

### Q: Logo hangi ekranlarda görünür?
**A:** Paketlere göre:
- S: Sadece result screen
- M: Start + result screens
- L/XL: Tüm ekranlar (start, result, analysis, profile)

### Q: Mesajlaşma sistemi nasıl çalışır?
**A:** 
- L ve XL paketlerinde aktif
- Sponsor ↔ Çiftçi doğrudan mesajlaşma
- Analiz bazında konuşma geçmişi
- Okundu/okunmadı durumu takibi

### Q: Smart Link sistemi nedir?
**A:**
- Sadece XL pakette mevcut
- AI analize dayalı ürün önerisi
- Anahtar kelime eşleştirmesi
- Performans analitikleri (CTR, conversion)

### Q: Veri erişim yüzdeleri nasıl hesaplanır?
**A:**
- %30: Temel sağlık skorları, tür bilgisi
- %60: + Hastalık/zararlı analizi, lokasyon, öneriler
- %100: + İletişim bilgileri, detaylı veriler

### Q: API rate limit var mı?
**A:** Standart API rate limit kuralları geçerli. Sponsorluk endpoint'leri için özel limit yok.

### Q: Cache sistemi nasıl çalışır?
**A:**
- Sponsor profilleri: 1 saat cache
- Smart linkler: 30 dakika cache
- Analitikler: 15 dakika cache

### Q: Test ortamında nasıl test edilir?
**A:**
1. Postman koleksiyonunu import edin
2. Admin/Sponsor hesabı ile login olun
3. Sponsor profili oluşturun
4. Tier-based endpoint'leri test edin
5. Analytics kontrolü yapın

### Q: Production deployment öncesi checklist?
**A:**
- [ ] Database migration tamamlandı
- [ ] Seed data yüklendi
- [ ] Servis kayıtları yapıldı
- [ ] Cache ayarları yapılandırıldı
- [ ] Environment config güncellendi
- [ ] Health check endpoint'leri test edildi

### Q: Hata durumları nasıl handle ediliyor?
**A:**
- Yetkisiz erişim: HTTP 401/403
- Paket yetersizliği: User-friendly mesaj
- Veri erişim kısıtı: Filtrelenmiş response
- Service hatalar: Graceful fallback

---

## 📞 Destek

- **Dokümantasyon**: [SPONSORSHIP_SYSTEM_DOCUMENTATION.md](./SPONSORSHIP_SYSTEM_DOCUMENTATION.md)
- **API Koleksiyonu**: [ZiraAI_Postman_Collection_v1.4.0.json](./ZiraAI_Postman_Collection_v1.4.0.json)
- **GitHub Issues**: Teknik sorunlar için issue açın

---

**🎉 Sponsorluk sistemi başarıyla entegre edildi! Tier-based özelliklerinin keyfini çıkarın.**