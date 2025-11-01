# `/sponsorship/dealer/invite` Endpoint Analizi

Bu endpoint'in **iki farklı kullanım amacı** var ve **InvitationType** parametresine göre farklı davranıyor:

---

## 🎯 Genel Amaç

Sponsorun, dealer network'ünü genişletmek için **yeni dealer eklemesi** (onboarding). Üç method'dan **Method B (Invite)** ve **Method C (AutoCreate)** için kullanılıyor.

---

## 📊 İki Farklı Mod

### **Method B: "Invite" (Davetiye Linki ile)**

**Ne zaman kullanılır:**
- Dealer'ın hesabı **var veya yok** (her ikisi için de kullanılabilir)
- **Formal onboarding** süreci isteniyor
- Dealer'ın **manuel olarak kabul etmesi** bekleniyor

**Nasıl çalışır:**

1. **Sponsor istek gönderir:**
```json
{
  "invitationType": "Invite",
  "email": "newdealer@example.com",
  "phone": "+905551234567",
  "dealerName": "New Dealer Company",
  "purchaseId": 26,
  "codeCount": 15
}
```

2. **Sistem yapar:**
   - `DealerInvitation` kaydı oluşturur (Status: "Pending")
   - Unique invitation token üretir
   - Davetiye linki oluşturur: `https://ziraai.com/dealer-invitation?token=abc123`
   - **7 gün** geçerlilik süresi
   - **Kodlar henüz transfer edilmez** ❌

3. **Response döner:**
```json
{
  "invitationId": 5,
  "invitationToken": "abc123def456",
  "invitationLink": "https://ziraai.com/dealer-invitation?token=abc123",
  "email": "newdealer@example.com",
  "dealerName": "New Dealer Company",
  "codeCount": 15,
  "status": "Pending",
  "invitationType": "Invite",
  "autoCreatedPassword": null,
  "createdDealerId": null
}
```

4. **Link Nasıl Gönderiliyor:**
   - ⚠️ **ŞU ANDA OTOMATIK GÖNDERIM YOK**
   - Sponsor, response'daki `invitationLink`'i **manuel olarak** dealer'a iletir
   - Gönderim yöntemleri:
     - Email (manuel)
     - WhatsApp (manuel)
     - SMS (manuel)
     - Telefon görüşmesi ile paylaşım
   
   **Gelecek Özellik (TODO):**
   - Otomatik email gönderimi
   - Otomatik SMS gönderimi
   - In-app notification

5. **Dealer Linki Tıklar:**
   
   **Link Formatı:**
   ```
   https://ziraai.com/dealer-invitation?token=abc123def456
   ```
   
   **Ne Oluyor:**
   - Link web browser veya mobil uygulamada açılıyor
   - Frontend (Angular Web veya Flutter Mobile) token'ı alıyor
   - Dealer'a davetiye detayları gösteriliyor:
     - Sponsor adı
     - Kaç kod transfer edilecek
     - Davetiye süresi (7 gün)
   - Dealer iki seçenek görüyor:
     - ✅ **Kabul Et** (Accept)
     - ❌ **Reddet** (Reject)

6. **Dealer Kabul Ederse:**
   
   **⚠️ ÖNEMLİ NOT:** 
   Şu anda backend'de **invitation kabul endpoint'i HENÜZ IMPLEMENT EDİLMEMİŞ**.
   
   **Gelecekte Olması Gereken Akış:**
   
   **Frontend Request (Gelecek):**
   ```http
   POST /api/v1/sponsorship/dealer/accept-invitation
   Authorization: Bearer {dealer_token}
   Content-Type: application/json
   
   {
     "invitationToken": "abc123def456"
   }
   ```
   
   **Backend İşlemleri (Gelecek Implementation):**
   ```csharp
   // 1. Token'ı validate et
   var invitation = await _repository.GetAsync(i => i.InvitationToken == token);
   if (invitation == null || invitation.Status != "Pending") {
       return Error("Invalid or expired invitation");
   }
   
   // 2. Dealer'ın token sahibi olduğunu kontrol et
   if (invitation.Email != dealerEmail) {
       return Error("Unauthorized");
   }
   
   // 3. Süresi dolmuş mu kontrol et
   if (invitation.ExpiryDate < DateTime.Now) {
       invitation.Status = "Expired";
       return Error("Invitation expired");
   }
   
   // 4. Invitation'ı "Accepted" olarak güncelle
   invitation.Status = "Accepted";
   invitation.AcceptedDate = DateTime.Now;
   invitation.CreatedDealerId = currentUserId; // Dealer'ın UserId'si
   
   // 5. Kodları transfer et
   await TransferCodesToDealer(
       invitation.PurchaseId, 
       invitation.SponsorId, 
       currentUserId, 
       invitation.CodeCount
   );
   
   // 6. Response
   return Success("Invitation accepted. Codes transferred.");
   ```
   
   **Şu Anki Çözüm (Workaround):**
   Dealer kabul etmek isterse, sponsor **Method A (Manual Transfer)** kullanmalı:
   ```
   1. Dealer hesap oluşturur (normal kayıt)
   2. Sponsor → GET /dealer/search?email=dealer@example.com
   3. Sponsor → POST /dealer/transfer-codes
   ```

7. **Dealer Reddederse:**
   
   **Frontend Request (Gelecek):**
   ```http
   POST /api/v1/sponsorship/dealer/reject-invitation
   Authorization: Bearer {dealer_token}
   Content-Type: application/json
   
   {
     "invitationToken": "abc123def456",
     "reason": "İlgilenmiyorum"
   }
   ```
   
   **Backend İşlemleri:**
   ```csharp
   invitation.Status = "Rejected";
   invitation.RejectedDate = DateTime.Now;
   invitation.RejectionReason = request.Reason;
   ```
   
   **Sonuç:**
   - Kodlar transfer edilmez
   - Sponsor yeni davetiye gönderebilir
   - Veya başka dealer'a kod transfer edebilir

**Kullanım Senaryosu:**
> Sponsor: "Bu dealer'ı eklemek istiyorum ama önce onay alayım, belki kabul etmeyebilir."

**Şu Anki Durum:**
- ⚠️ Invitation link oluşturulur ama **otomatik gönderilmez**
- ⚠️ Dealer **kabul/red endpoint'leri henüz yok**
- ✅ Sadece invitation kaydı database'de oluşturulur
- ✅ Sponsor invitation listesini görebilir

**Önerilen Kullanım (Şu An İçin):**
Method B yerine **Method A** veya **Method C** kullanın:
- **Method A**: Dealer hesabı varsa direkt transfer
- **Method C**: Yeni dealer için otomatik hesap oluştur

---

### **Method C: "AutoCreate" (Anında Hesap Oluşturma)**

**Ne zaman kullanılır:**
- **Hızlı** dealer ekleme isteniyor
- Dealer'ın kabulünü **beklemek istemiyoruz**
- Dealer için **yeni hesap** oluşturmak gerekiyor

**Nasıl çalışır:**

1. **Sponsor istek gönderir:**
```json
{
  "invitationType": "AutoCreate",
  "email": "quickdealer@example.com",
  "dealerName": "Quick Dealer LLC",
  "purchaseId": 26,
  "codeCount": 20
}
```

2. **Sistem yapar (HEPSİ ANINDA):**
   - ✅ **Yeni User hesabı oluşturur**
   - ✅ **Random şifre** generate eder (12 karakter)
   - ✅ **Sponsor role** atar (UserGroup)
   - ✅ `DealerInvitation` kaydı oluşturur (Status: "Accepted" - otomatik kabul)
   - ✅ **Kodları hemen transfer eder** (beklemez)
   - ✅ Şifreyi response'da döner

3. **Response döner:**
```json
{
  "invitationId": 6,
  "invitationToken": "xyz789",
  "invitationLink": null,
  "email": "quickdealer@example.com",
  "dealerName": "Quick Dealer LLC",
  "codeCount": 20,
  "status": "Accepted",
  "invitationType": "AutoCreate",
  "autoCreatedPassword": "AbCdEf123456",  // ⚠️ Bu şifreyi dealer'a ilet
  "createdDealerId": 170                   // Yeni oluşturulan dealer ID
}
```

4. **Sonraki adımlar:**
   - Sponsor, email ve şifreyi dealer'a güvenli şekilde iletir
   - Dealer hemen login olabilir
   - Kodlar zaten transfer edilmiş durumda

**Kullanım Senaryosu:**
> Sponsor: "Bu dealer'ı hemen ekle, bekleyemem. Hesabını ben oluşturayım, şifresini kendisine ileteceğim."

---

## 🔄 Method A ile Karşılaştırma

**Method A: Manual Transfer**
```
GET /dealer/search → POST /dealer/transfer-codes
```
- Dealer **zaten hesabı var**
- Email ile bulunur
- Direkt kod transferi yapılır
- **Davetiye sistemi kullanılmaz**

**Method B: Invite**
```
POST /dealer/invite (Invite) → Dealer kabul eder → Kodlar transfer
```
- Dealer hesabı **var veya yok**
- Formal onboarding
- **İki adımlı** süreç

**Method C: AutoCreate**
```
POST /dealer/invite (AutoCreate) → Hesap + Kodlar anında hazır
```
- Dealer hesabı **kesinlikle yok**
- Hızlı onboarding
- **Tek adımlı** süreç

---

## 💡 Kod İncelemesi - Önemli Noktalar

### **Kod Transfer Fonksiyonu (Her İki Method'da Farklı)**

```csharp
// Method B (Invite): Kod transferi YOK, sadece davetiye
if (request.InvitationType == "Invite") {
    // Invitation oluşturulur
    // Link gönderilir
    // Status: "Pending"
    // Codes are NOT transferred yet ❌
}

// Method C (AutoCreate): Kod transferi ANINDA
if (request.InvitationType == "AutoCreate") {
    // 1. Dealer account oluştur
    var newDealer = new User { ... };
    
    // 2. Random password generate et
    var autoPassword = GenerateRandomPassword();
    
    // 3. Sponsor role ata
    var userGroup = new UserGroup { UserId = createdDealer.UserId, GroupId = sponsorGroup.Id };
    
    // 4. Invitation'ı "Accepted" olarak kaydet
    invitation.Status = "Accepted";
    invitation.CreatedDealerId = createdDealer.UserId;
    invitation.AutoCreatedPassword = autoPassword;
    
    // 5. Kodları hemen transfer et ✅
    await TransferCodesToDealer(request.PurchaseId, request.SponsorId, createdDealer.UserId, request.CodeCount);
}
```

### **Transfer Fonksiyonu Detayı**

```csharp
private async Task TransferCodesToDealer(int purchaseId, int sponsorId, int dealerId, int codeCount)
{
    var codes = await _sponsorshipCodeRepository.GetByPurchaseIdAsync(purchaseId);
    var codesToTransfer = codes
        .Where(c => c.DealerId == null)  // Sadece dealera verilmemiş kodlar
        .Take(codeCount)                 // İstenen sayı kadar
        .ToList();

    foreach (var code in codesToTransfer)
    {
        code.DealerId = dealerId;              // Dealer'a ata
        code.TransferredAt = DateTime.Now;     // Timestamp
        code.TransferredByUserId = sponsorId;  // Kimin transfer ettiği
        _sponsorshipCodeRepository.Update(code);
    }
    await _sponsorshipCodeRepository.SaveChangesAsync();
}
```

---

## 📋 Özet Tablo

| Özellik | Method B (Invite) | Method C (AutoCreate) |
|---------|-------------------|----------------------|
| **Dealer hesabı** | Var veya yok | Kesinlikle yok (yeni oluşturulur) |
| **Süreç** | İki adımlı (davet → kabul) | Tek adımlı (anında) |
| **Kod transferi** | Kabul sonrası | Hemen |
| **Şifre** | Dealer kendi belirler | Random generate |
| **Link** | Davetiye linki var (manuel gönderim) | Link yok |
| **Link Gönderimi** | ❌ Manuel (sponsor kendisi gönderir) | N/A |
| **Accept/Reject** | ❌ Endpoint'ler yok (TODO) | N/A |
| **Status** | Pending → Accepted/Rejected | Direkt Accepted |
| **Response'da şifre** | Yok | Var (güvenli paylaş) |
| **Geçerlilik süresi** | 7 gün | N/A (anında tamamlanır) |

---

## 🎯 Kullanım Kararı Ağacı

```
Dealer eklenmeli mi?
├─ Dealer hesabı var mı?
│  ├─ Evet → Method A (Manual Transfer) kullan
│  └─ Hayır → Devam et
│
├─ Hızlı mı olmalı?
│  ├─ Evet, bekleyemem
│  │  └─ Method C (AutoCreate) kullan
│  │     - Hesap anında oluştur
│  │     - Kodlar anında transfer
│  │     - Şifreyi güvenli paylaş
│  │
│  └─ Hayır, formal süreç olsun
│     └─ Method B (Invite) kullan
│        - Davetiye gönder
│        - Dealer kabulünü bekle
│        - Kabul sonrası kodlar transfer
```

---

## ⚠️ Güvenlik Notu

**Method C (AutoCreate) kullanırken:**
```json
{
  "autoCreatedPassword": "AbCdEf123456"  // ⚠️ TEK SEFERLIK GÖRÜNTÜLEME
}
```

- Bu şifre **sadece response'da bir kez** dönüyor
- Database'de **plain text** olarak tutuluyor (invitation tablosunda)
- Sponsor'un bu şifreyi **güvenli kanal** ile dealer'a iletmesi gerekiyor
- Dealer ilk login'de **şifre değiştirmeli**

---

## 📍 API Endpoint Detayları

### **Endpoint**
```
POST /api/v1/sponsorship/dealer/invite
```

### **Authorization**
```
Sponsor, Admin
```

### **Headers**
```http
Authorization: Bearer {JWT_TOKEN}
x-dev-arch-version: 1.0
Content-Type: application/json
```

### **Request Body (Invite Type)**
```json
{
  "invitationType": "Invite",
  "email": "newdealer@example.com",
  "phone": "+905551234567",
  "dealerName": "New Dealer Company",
  "purchaseId": 26,
  "codeCount": 15
}
```

### **Request Body (AutoCreate Type)**
```json
{
  "invitationType": "AutoCreate",
  "email": "quickdealer@example.com",
  "dealerName": "Quick Dealer LLC",
  "purchaseId": 26,
  "codeCount": 20
}
```

### **Response - Invite Type** (200 OK)
```json
{
  "data": {
    "invitationId": 5,
    "invitationToken": "abc123def456",
    "invitationLink": "https://ziraai.com/dealer-invitation?token=abc123def456",
    "email": "newdealer@example.com",
    "phone": "+905551234567",
    "dealerName": "New Dealer Company",
    "codeCount": 15,
    "status": "Pending",
    "invitationType": "Invite",
    "autoCreatedPassword": null,
    "createdDealerId": null,
    "createdAt": "2025-10-26T10:30:00"
  },
  "success": true,
  "message": "Invitation sent to newdealer@example.com"
}
```

### **Response - AutoCreate Type** (200 OK)
```json
{
  "data": {
    "invitationId": 6,
    "invitationToken": "xyz789uvw456",
    "invitationLink": null,
    "email": "quickdealer@example.com",
    "dealerName": "Quick Dealer LLC",
    "codeCount": 20,
    "status": "Accepted",
    "invitationType": "AutoCreate",
    "autoCreatedPassword": "AbCdEf123456",
    "createdDealerId": 170,
    "createdAt": "2025-10-26T10:30:00"
  },
  "success": true,
  "message": "Dealer account created successfully. Login: quickdealer@example.com, Password: AbCdEf123456"
}
```

### **Error Responses**

**400 Bad Request - Email Required**
```json
{
  "success": false,
  "message": "Email is required for Invite type."
}
```

**400 Bad Request - Not Enough Codes**
```json
{
  "success": false,
  "message": "Not enough available codes. Requested: 15, Available: 5"
}
```

**404 Not Found - Purchase Not Found**
```json
{
  "success": false,
  "message": "Purchase not found"
}
```

---

## 🔗 İlgili Endpoint'ler

### **Davetiye Listesi**
```http
GET /api/v1/sponsorship/dealer/invitations?status=Pending
```
Oluşturulan davetiyeleri görüntüle

### **Kod Transferi (Method A)**
```http
POST /api/v1/sponsorship/dealer/transfer-codes
```
Mevcut dealer'a direkt kod transferi

### **Dealer Arama**
```http
GET /api/v1/sponsorship/dealer/search?email=dealer@example.com
```
Dealer hesabının var olup olmadığını kontrol et

---

## 📚 İlgili Dokümanlar

- **API Documentation**: `API_DOCUMENTATION.md`
- **Flow Guide**: `SPONSOR_DEALER_FARMER_FLOW_GUIDE.md`
- **Testing Checklist**: `TESTING_CHECKLIST.md`
- **Postman Collection**: `ZiraAI_Dealer_Distribution_Complete_E2E.postman_collection.json`

---

## 🔍 Handler Dosyası

**Konum**: `Business/Handlers/Sponsorship/Commands/CreateDealerInvitationCommandHandler.cs`

**Sorumluluklar**:
- Invitation oluşturma
- AutoCreate için User account oluşturma
- Random password generation
- Sponsor role atama
- Kod transferi (AutoCreate için)
- Email validation
- Kod sayısı validasyonu

---

---

## 🚧 Implementation Status

### ✅ Tamamlanmış Özellikler

#### Method B (Invite) - Kısmi
- ✅ `POST /dealer/invite` endpoint (invitation oluşturma)
- ✅ `GET /dealer/invitations` endpoint (invitation listesi)
- ✅ Database entity (`DealerInvitation`)
- ✅ Invitation token generation
- ✅ Invitation link formatı
- ✅ 7 günlük expiry logic

#### Method C (AutoCreate) - Tam
- ✅ `POST /dealer/invite` endpoint (AutoCreate modu)
- ✅ Otomatik user account oluşturma
- ✅ Random password generation
- ✅ Sponsor role atama
- ✅ Anında kod transferi
- ✅ Tüm akış çalışıyor

#### Method A (Manual Transfer) - Tam
- ✅ `GET /dealer/search` endpoint
- ✅ `POST /dealer/transfer-codes` endpoint
- ✅ Direkt kod transferi
- ✅ Tüm akış çalışıyor

---

### ❌ Eksik Özellikler (Method B İçin)

#### 1. Invitation Gönderimi
**Durum:** ❌ Implement edilmedi

**Gerekli:**
- Email gönderimi (SMTP entegrasyonu)
- SMS gönderimi (SMS gateway entegrasyonu)
- In-app notification
- WhatsApp Business API entegrasyonu (opsiyonel)

**Kod Örneği (Gelecek):**
```csharp
// EmailService integration
await _emailService.SendDealerInvitationEmail(
    invitation.Email,
    invitation.InvitationLink,
    invitation.DealerName,
    sponsorCompanyName
);

// SMS Service integration  
await _smsService.SendDealerInvitationSms(
    invitation.Phone,
    invitation.InvitationLink
);
```

---

#### 2. Invitation Accept Endpoint
**Durum:** ❌ Implement edilmedi

**Endpoint:** `POST /api/v1/sponsorship/dealer/accept-invitation`

**Gerekli Implementation:**
```csharp
// Handler: AcceptDealerInvitationCommandHandler.cs
public class AcceptDealerInvitationCommand : IRequest<IResult>
{
    public string InvitationToken { get; set; }
}

public class AcceptDealerInvitationCommandHandler : IRequestHandler<AcceptDealerInvitationCommand, IResult>
{
    public async Task<IResult> Handle(...)
    {
        // 1. Token validation
        // 2. Expiry check
        // 3. Email verification (dealer owns this invitation)
        // 4. Status update: "Pending" → "Accepted"
        // 5. Transfer codes to dealer
        // 6. Send confirmation email/SMS
        return new SuccessResult("Invitation accepted");
    }
}
```

**Controller:**
```csharp
[HttpPost("dealer/accept-invitation")]
public async Task<IActionResult> AcceptDealerInvitation([FromBody] AcceptDealerInvitationCommand command)
{
    var result = await Mediator.Send(command);
    return result.Success ? Ok(result) : BadRequest(result);
}
```

---

#### 3. Invitation Reject Endpoint
**Durum:** ❌ Implement edilmedi

**Endpoint:** `POST /api/v1/sponsorship/dealer/reject-invitation`

**Gerekli Implementation:**
```csharp
// Handler: RejectDealerInvitationCommandHandler.cs
public class RejectDealerInvitationCommand : IRequest<IResult>
{
    public string InvitationToken { get; set; }
    public string Reason { get; set; }
}

public class RejectDealerInvitationCommandHandler : IRequestHandler<RejectDealerInvitationCommand, IResult>
{
    public async Task<IResult> Handle(...)
    {
        // 1. Token validation
        // 2. Email verification
        // 3. Status update: "Pending" → "Rejected"
        // 4. Save rejection reason
        // 5. Notify sponsor (optional)
        return new SuccessResult("Invitation rejected");
    }
}
```

---

#### 4. Frontend Integration
**Durum:** ❌ Implement edilmedi

**Gerekli Sayfalar:**

**A. Invitation Acceptance Page (`/dealer-invitation`)**
```typescript
// Angular Component: dealer-invitation.component.ts
export class DealerInvitationComponent implements OnInit {
  invitationToken: string;
  invitationDetails: any;
  
  ngOnInit() {
    // 1. Get token from query params
    this.route.queryParams.subscribe(params => {
      this.invitationToken = params['token'];
    });
    
    // 2. Fetch invitation details (new endpoint needed)
    this.loadInvitationDetails();
  }
  
  acceptInvitation() {
    // Call: POST /dealer/accept-invitation
    this.apiService.acceptDealerInvitation(this.invitationToken)
      .subscribe(response => {
        this.router.navigate(['/dashboard']);
      });
  }
  
  rejectInvitation() {
    // Call: POST /dealer/reject-invitation
    this.apiService.rejectDealerInvitation(this.invitationToken, this.rejectionReason)
      .subscribe(response => {
        this.router.navigate(['/']);
      });
  }
}
```

**B. Get Invitation Details Endpoint**
```http
GET /api/v1/sponsorship/dealer/invitation-details?token=abc123
```

Response:
```json
{
  "sponsorName": "ZiraAI Main Sponsor",
  "codeCount": 15,
  "packageTier": "M",
  "expiresAt": "2025-11-02T10:30:00",
  "status": "Pending",
  "message": "You are invited to join the dealer network"
}
```

---

#### 5. Deep Link Handling (Mobile)
**Durum:** ❌ Implement edilmedi

**Flutter Implementation:**
```dart
// deep_link_handler.dart
class DeepLinkHandler {
  void handleDealerInvitation(String token) async {
    // 1. Check if user is logged in
    if (!isLoggedIn) {
      // Redirect to login with return URL
      Navigator.pushNamed(
        context, 
        '/login',
        arguments: {'returnUrl': '/dealer-invitation?token=$token'}
      );
      return;
    }
    
    // 2. Show invitation details
    final invitation = await apiService.getInvitationDetails(token);
    
    // 3. Navigate to acceptance screen
    Navigator.pushNamed(
      context,
      '/dealer-invitation',
      arguments: {'invitation': invitation, 'token': token}
    );
  }
}
```

---

### 🔮 Gelecek Geliştirmeler (Roadmap)

#### Phase 1: Core Functionality (P0 - Kritik)
- [ ] Accept invitation endpoint implementation
- [ ] Reject invitation endpoint implementation  
- [ ] Get invitation details endpoint (public, no auth)
- [ ] Frontend invitation acceptance page (Web)
- [ ] Mobile deep link handling (Flutter)

#### Phase 2: Notification System (P1 - Yüksek Öncelik)
- [ ] Email gönderimi (invitation link)
- [ ] SMS gönderimi (invitation link)
- [ ] Sponsor bildirim (acceptance/rejection)
- [ ] Reminder emails (3 gün sonra)

#### Phase 3: Enhanced Features (P2 - Orta Öncelik)
- [ ] Invitation resend functionality
- [ ] Invitation cancel (sponsor tarafından)
- [ ] Custom invitation message
- [ ] Invitation analytics (açılma oranı)
- [ ] Batch invitation (çoklu dealer)

#### Phase 4: Advanced Features (P3 - Düşük Öncelik)
- [ ] WhatsApp Business API integration
- [ ] In-app push notifications
- [ ] QR code generation for invitation
- [ ] Invitation template customization

---

### 📝 Geliştirici Notları

**Method B'yi Aktif Kullanmak İçin Gerekli Adımlar:**

1. **Backend:**
   - `AcceptDealerInvitationCommand` + Handler
   - `RejectDealerInvitationCommand` + Handler
   - `GetInvitationDetailsQuery` + Handler (public endpoint)
   - Email/SMS service integration
   - Controller endpoint'leri ekle

2. **Frontend (Angular):**
   - `/dealer-invitation` route ekle
   - Invitation detail page component
   - Accept/Reject button handlers
   - Success/Error notifications

3. **Mobile (Flutter):**
   - Deep link configuration (`ziraai://dealer-invitation`)
   - Invitation screen UI
   - API service methods
   - Push notification handling

4. **Testing:**
   - E2E test senaryoları
   - Email/SMS delivery testleri
   - Expiry handling testleri
   - Authorization testleri

---

### ⚠️ Şu An İçin Öneriler

**Production Kullanımı:**
- ✅ **Method C (AutoCreate)** kullanın → Tam çalışıyor
- ✅ **Method A (Manual Transfer)** kullanın → Tam çalışıyor
- ❌ **Method B (Invite)** kullanmayın → Yarım kalmış

**Geçici Çözüm:**
Method B yerine şu akışı kullanın:
1. Dealer'a email/telefon ile iletişim kurun
2. Dealer hesap oluştursun (normal kayıt)
3. Method A ile kod transfer edin

---

**Document Version**: 1.1  
**Created**: 2025-10-28  
**Last Updated**: 2025-10-28  
**Status**: ⚠️ Method B Partially Implemented (Core works, acceptance flow missing)
