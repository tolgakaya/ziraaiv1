# Farmer Profil Yönetimi - Kod Analiz Raporu

**Analiz Tarihi**: 2025-11-17
**Analiz Kapsamı**: Farmer rolündeki kullanıcıların profil bilgileri ve yönetim endpoint'leri

---

## Özet

✅ **Farmer kullanıcıları için genel User endpoint'leri mevcut**
❌ **Farmer'a özel profil endpoint'i YOK**
⚠️ **Güvenlik açıkları ve eksik validasyonlar tespit edildi**

---

## 1. Farmer Kullanıcı Bilgileri (User Entity)

### Tüm User Alanları

**Dosya**: [Core/Entities/Concrete/User.cs](../Core/Entities/Concrete/User.cs)

```csharp
public class User : IEntity
{
    // Temel Kimlik Bilgileri
    public int UserId { get; set; }
    public long CitizenId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string MobilePhones { get; set; }

    // Kişisel Bilgiler
    public DateTime? BirthDate { get; set; }
    public int? Gender { get; set; }
    public string Address { get; set; }
    public string Notes { get; set; }

    // Güvenlik
    public byte[] PasswordSalt { get; set; }
    public byte[] PasswordHash { get; set; }
    public string RefreshToken { get; set; }

    // Avatar/Profil Resmi
    public string AvatarUrl { get; set; }
    public string AvatarThumbnailUrl { get; set; }
    public DateTime? AvatarUpdatedDate { get; set; }

    // Sistem Bilgileri
    public bool Status { get; set; }
    public DateTime RecordDate { get; set; }
    public DateTime UpdateContactDate { get; set; }

    // Referral System
    public string RegistrationReferralCode { get; set; }

    // Admin İşlemleri
    public bool IsActive { get; set; } = true;
    public DateTime? DeactivatedDate { get; set; }
    public int? DeactivatedBy { get; set; }
    public string DeactivationReason { get; set; }

    // Not Mapped
    [NotMapped]
    public string AuthenticationProviderType { get; set; } = "Person";
}
```

---

## 2. Mevcut Endpoint'ler

### 2.1. GET /api/users/{id} - Kullanıcı Detayı

**Controller**: [WebAPI/Controllers/UsersController.cs:64](../WebAPI/Controllers/UsersController.cs#L64)
**Handler**: [Business/Handlers/Users/Queries/GetUserQuery.cs](../Business/Handlers/Users/Queries/GetUserQuery.cs)
**DTO**: [Core/Entities/Dtos/UserDto.cs](../Core/Entities/Dtos/UserDto.cs)

**Yetki**: `[SecuredOperation]` - Giriş yapmış kullanıcılar

#### Request
```http
GET /api/v1/users/{userId} HTTP/1.1
Authorization: Bearer {token}
```

#### Response DTO
```csharp
public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string MobilePhones { get; set; }
    public string Address { get; set; }
    public string Notes { get; set; }
    public int Gender { get; set; }
    public string Password { get; set; }      // ⚠️ SECURITY ISSUE
    public bool Status { get; set; }
    public bool IsActive { get; set; }
    public string RefreshToken { get; set; } // ⚠️ SECURITY ISSUE
}
```

**❌ Güvenlik Açıkları**:
1. Password field DTO'da (hash değil ama yine de yanlış)
2. RefreshToken DTO'da expose ediliyor
3. Herhangi bir kullanıcı herhangi bir userId ile başkasının bilgilerini görebilir

---

### 2.2. PUT /api/users - Kullanıcı Güncelleme

**Controller**: [WebAPI/Controllers/UsersController.cs:91](../WebAPI/Controllers/UsersController.cs#L91)
**Handler**: [Business/Handlers/Users/Commands/UpdateUserCommand.cs](../Business/Handlers/Users/Commands/UpdateUserCommand.cs)
**DTO**: [Entities/Dtos/UpdateUserDto.cs](../Entities/Dtos/UpdateUserDto.cs)

**Yetki**: `[SecuredOperation]` - Giriş yapmış kullanıcılar

#### Request
```http
PUT /api/v1/users HTTP/1.1
Authorization: Bearer {token}
Content-Type: application/json

{
  "userId": 123,
  "email": "farmer@example.com",
  "fullName": "Ahmet Yılmaz",
  "mobilePhones": "+905551234567",
  "address": "İstanbul",
  "notes": "Organik tarım yapıyorum"
}
```

#### Güncellenebilen Alanlar (UpdateUserDto)
```csharp
public class UpdateUserDto
{
    public int UserId { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string MobilePhones { get; set; }
    public string Address { get; set; }
    public string Notes { get; set; }
}
```

#### Handler Logic
```csharp
public async Task<IResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
{
    var isThereAnyUser = await _userRepository.GetAsync(u => u.UserId == request.UserId);

    isThereAnyUser.FullName = request.FullName;
    isThereAnyUser.Email = request.Email;
    isThereAnyUser.MobilePhones = request.MobilePhones;
    isThereAnyUser.Address = request.Address;
    isThereAnyUser.Notes = request.Notes;

    _userRepository.Update(isThereAnyUser);
    await _userRepository.SaveChangesAsync();
    return new SuccessResult(Messages.Updated);
}
```

**❌ Kritik Güvenlik Açıkları**:
1. **Hiçbir user ownership kontrolü YOK** - Herhangi bir kullanıcı herhangi bir userId ile başkasının profilini güncelleyebilir!
2. **Hiçbir validasyon YOK** - Email, telefon formatı kontrolü yok
3. **JWT'deki userId ile request'teki userId karşılaştırması YOK**

---

### 2.3. Avatar Yönetimi Endpoint'leri

#### POST /api/users/avatar - Avatar Upload

**Controller**: [WebAPI/Controllers/UsersController.cs:122](../WebAPI/Controllers/UsersController.cs#L122)
**Yetki**: `[Authorize]` - JWT'den userId alınıyor ✅

```http
POST /api/v1/users/avatar HTTP/1.1
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [image file]
```

**✅ Güvenlik**: JWT'den userId çekiliyor, user ownership var

#### GET /api/users/avatar/{userId?} - Avatar Bilgisi

**Controller**: [WebAPI/Controllers/UsersController.cs:146](../WebAPI/Controllers/UsersController.cs#L146)

```http
GET /api/v1/users/avatar HTTP/1.1          # Kendi avatar'ı
GET /api/v1/users/avatar/123 HTTP/1.1     # Başkasının avatar'ı
```

**Response**:
```json
{
  "success": true,
  "data": {
    "userId": 123,
    "avatarUrl": "https://...",
    "avatarThumbnailUrl": "https://...",
    "avatarUpdatedDate": "2025-11-17T10:30:00Z"
  }
}
```

#### DELETE /api/users/avatar - Avatar Silme

**Controller**: [WebAPI/Controllers/UsersController.cs:169](../WebAPI/Controllers/UsersController.cs#L169)
**Yetki**: `[Authorize]` - JWT'den userId alınıyor ✅

---

## 3. Güncellenemeyen Alanlar

Farmer kullanıcıları şu bilgilerini **güncelleyemezler** (API'de endpoint yok):

❌ **BirthDate** - Doğum tarihi
❌ **Gender** - Cinsiyet
❌ **AvatarUrl** - Profil resmi (sadece upload endpoint var)
❌ **RegistrationReferralCode** - Kayıt referral kodu (sadece kayıt sırasında)
❌ **CitizenId** - TC Kimlik No (kayıt sonrası değiştirilemez)
❌ **IsActive** - Aktiflik durumu (sadece admin)
❌ **Password** - Şifre (ayrı endpoint gerekli)

---

## 4. Karşılaştırma: Sponsor vs Farmer Profil Endpoint'leri

### Sponsor Profil Endpoint'i

**Controller**: [WebAPI/Controllers/SponsorshipController.cs:1225](../WebAPI/Controllers/SponsorshipController.cs#L1225)

```csharp
[Authorize(Roles = "Sponsor,Admin")]
[HttpGet("profile")]
public async Task<IActionResult> GetSponsorProfile()
{
    var userId = GetCurrentUserId(); // JWT'den alınıyor ✅
    var query = new GetSponsorProfileQuery { SponsorId = userId.Value };
    var result = await Mediator.Send(query);
    return result.Success ? Ok(result) : NotFound(result);
}
```

**✅ Sponsor'lar için özel endpoint VAR**
**✅ JWT'den userId otomatik alınıyor**
**✅ Role-based authorization var**

### Farmer Profil Endpoint'i

**❌ Farmer'lar için özel endpoint YOK**
**❌ Generic /api/users endpoint'leri kullanılıyor**
**❌ User ownership kontrolü yok**
**❌ Validasyon yok**

---

## 5. Güvenlik Açıkları ve Riskler

### 🔴 Kritik Seviye

1. **User Ownership Kontrolü Eksikliği**
   - `PUT /api/users` endpoint'inde JWT'deki userId ile request'teki userId karşılaştırması YOK
   - Herhangi bir farmer başka bir farmer'ın profilini güncelleyebilir
   - **Exploit**: Farmer A, kendi token'ı ile Farmer B'nin userId'sini göndererek B'nin email/telefon/adres bilgilerini değiştirebilir

2. **Sensitive Data Exposure**
   - `UserDto` içinde `RefreshToken` ve `Password` alanları var
   - Bu bilgiler API response'unda dönüyor olabilir

### 🟡 Yüksek Seviye

3. **Validasyon Eksikliği**
   - Email format kontrolü yok
   - Telefon numarası format kontrolü yok
   - FullName uzunluk kontrolü yok

4. **Authorization Kontrolü Gevşek**
   - `GET /api/users/{id}` endpoint'inde herhangi bir kullanıcı başkasının bilgilerini görebilir
   - Role bazlı kontrol yok

---

## 6. Öneriler

### Kısa Vadeli (Acil)

#### 6.1. User Ownership Kontrolü Ekle

**UpdateUserCommand.cs** değişikliği:

```csharp
[SecuredOperation(Priority = 1)]
public async Task<IResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
{
    // JWT'den gelen userId
    var currentUserId = _httpContextAccessor.HttpContext?.User?
        .FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(currentUserId) ||
        int.Parse(currentUserId) != request.UserId)
    {
        return new ErrorResult("You can only update your own profile");
    }

    var user = await _userRepository.GetAsync(u => u.UserId == request.UserId);
    if (user == null)
        return new ErrorResult("User not found");

    user.FullName = request.FullName;
    user.Email = request.Email;
    user.MobilePhones = request.MobilePhones;
    user.Address = request.Address;
    user.Notes = request.Notes;

    _userRepository.Update(user);
    await _userRepository.SaveChangesAsync();
    return new SuccessResult(Messages.Updated);
}
```

#### 6.2. UserDto Temizliği

**UserDto.cs** değişikliği:

```csharp
public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string MobilePhones { get; set; }
    public string Address { get; set; }
    public string Notes { get; set; }
    public int? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public bool Status { get; set; }
    public bool IsActive { get; set; }

    // Avatar
    public string AvatarUrl { get; set; }
    public string AvatarThumbnailUrl { get; set; }

    // ❌ REMOVE THESE:
    // public string Password { get; set; }
    // public string RefreshToken { get; set; }
}
```

### Orta Vadeli

#### 6.3. Farmer Profil Endpoint'i Oluştur

**Yeni Controller Endpoint**:

```csharp
// FarmerController.cs
[Authorize(Roles = "Farmer")]
[HttpGet("profile")]
public async Task<IActionResult> GetMyProfile()
{
    var userId = GetCurrentUserId(); // JWT'den
    var query = new GetFarmerProfileQuery { FarmerId = userId };
    var result = await Mediator.Send(query);
    return result.Success ? Ok(result) : NotFound(result);
}

[Authorize(Roles = "Farmer")]
[HttpPut("profile")]
public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateFarmerProfileDto dto)
{
    var userId = GetCurrentUserId(); // JWT'den
    var command = new UpdateFarmerProfileCommand
    {
        FarmerId = userId,
        FullName = dto.FullName,
        Email = dto.Email,
        MobilePhones = dto.MobilePhones,
        Address = dto.Address,
        BirthDate = dto.BirthDate,
        Gender = dto.Gender
    };
    var result = await Mediator.Send(command);
    return result.Success ? Ok(result) : BadRequest(result);
}
```

#### 6.4. Validasyon Ekle

```csharp
public class UpdateFarmerProfileValidator : AbstractValidator<UpdateFarmerProfileCommand>
{
    public UpdateFarmerProfileValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.MobilePhones)
            .NotEmpty().WithMessage("Mobile phone is required")
            .Matches(@"^\+90\d{10}$").WithMessage("Invalid Turkish phone format");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MinimumLength(2).WithMessage("Name too short")
            .MaximumLength(100).WithMessage("Name too long");

        RuleFor(x => x.BirthDate)
            .Must(BeValidBirthDate).When(x => x.BirthDate.HasValue)
            .WithMessage("Birth date must be between 1900 and today");
    }

    private bool BeValidBirthDate(DateTime? date)
    {
        if (!date.HasValue) return true;
        return date.Value >= new DateTime(1900, 1, 1) &&
               date.Value <= DateTime.Now;
    }
}
```

---

## 7. Kullanım Örnekleri

### Mevcut Kullanım (Güvensiz)

```javascript
// ❌ GÜVENSİZ - Herhangi bir userId gönderilebilir
const response = await fetch('/api/v1/users', {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${myToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    userId: 999,  // Başkasının ID'si!
    fullName: "Hacked Name",
    email: "hacker@example.com"
  })
});
```

### Önerilen Kullanım (Güvenli)

```javascript
// ✅ GÜVENLİ - UserId JWT'den otomatik alınır
const response = await fetch('/api/v1/farmer/profile', {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${myToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    // userId yok - backend JWT'den alıyor
    fullName: "Ahmet Yılmaz",
    email: "ahmet@example.com",
    mobilePhones: "+905551234567",
    address: "İstanbul",
    birthDate: "1990-05-15",
    gender: 1
  })
});
```

---

## 8. Test Senaryoları

### Güvenlik Testi

```bash
# Test 1: Kendi profilimi güncelleyebilir miyim?
curl -X PUT https://api.ziraai.com/api/v1/users \
  -H "Authorization: Bearer MY_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 123,  # Benim ID'm
    "fullName": "Yeni İsim"
  }'
# Beklenen: ✅ Success

# Test 2: Başkasının profilini güncelleyebilir miyim? (AÇIK TESTİ)
curl -X PUT https://api.ziraai.com/api/v1/users \
  -H "Authorization: Bearer MY_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 456,  # Başkasının ID'si!
    "fullName": "Hacked Name"
  }'
# Mevcut Durum: ✅ Success (SORUN!)
# Beklenen Durum: ❌ 403 Forbidden
```

---

## 9. Sonuç

### Mevcut Durum

| Özellik | Durum | Notlar |
|---------|-------|--------|
| Farmer Profil Görüntüleme | ⚠️ Kısmen Var | Generic `/api/users/{id}` endpoint'i |
| Farmer Profil Güncelleme | ⚠️ Kısmen Var | Generic `/api/users` endpoint'i |
| User Ownership Kontrolü | ❌ YOK | Kritik güvenlik açığı |
| Validasyon | ❌ YOK | Email, telefon format kontrolü yok |
| Avatar Yönetimi | ✅ VAR | Upload, get, delete endpoint'leri mevcut |
| Farmer'a Özel Endpoint | ❌ YOK | Sponsor'lar için var ama farmer'lar için yok |

### Acil Aksiyonlar

1. ✅ **User ownership kontrolü ekle** (UpdateUserCommand)
2. ✅ **UserDto'dan Password ve RefreshToken kaldır**
3. ✅ **Validasyon ekle** (Email, telefon formatı)
4. ⏳ **Farmer profil endpoint'i oluştur** (uzun vadeli)

---

**Rapor Hazırlayan**: Claude Code
**Tarih**: 2025-11-17
**Versiyon**: 1.0
