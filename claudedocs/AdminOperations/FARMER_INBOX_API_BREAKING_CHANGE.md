# ⚠️ BREAKING CHANGE: Farmer Sponsorship Inbox API

## Değişiklik Özeti
Farmer Sponsorship Inbox endpoint'inde **güvenlik düzeltmesi** yapıldı. API artık **JWT authentication** gerektiriyor ve telefon numarası parametresi kaldırıldı.

## Tarih
2025-01-24

## Değişen Endpoint
```
GET /api/v1/sponsorship/farmer-inbox
```

## ❌ ESKİ KULLANIM (Artık Çalışmıyor)
```dart
// ❌ YANLIŞ - Bu artık 401 Unauthorized döner
final response = await http.get(
  Uri.parse('$apiBase/sponsorship/farmer-inbox?phone=05551234567'),
);
```

**Sorun:** Herhangi bir kullanıcı, başka kullanıcıların sponsorluk kodlarını görebiliyordu (IDOR güvenlik açığı).

## ✅ YENİ KULLANIM (Zorunlu)
```dart
// ✅ DOĞRU - JWT token ile authenticate edilmiş istek
final token = await getAuthToken(); // Kullanıcının JWT token'ı

final response = await http.get(
  Uri.parse('$apiBase/sponsorship/farmer-inbox?includeUsed=false&includeExpired=false'),
  headers: {
    'Authorization': 'Bearer $token',
    'Content-Type': 'application/json',
  },
);
```

## Değişiklikler

### 1. Authentication Zorunlu
- **Eski:** `[AllowAnonymous]` - Herkes erişebiliyordu
- **Yeni:** `[Authorize(Roles = "Farmer")]` - Sadece authenticate farmer'lar erişebilir

### 2. Parametreler
| Parametre | Eski | Yeni | Açıklama |
|-----------|------|------|----------|
| `phone` | ✅ Zorunlu | ❌ Kaldırıldı | Telefon numarası JWT token'dan otomatik alınıyor |
| `includeUsed` | ✅ Opsiyonel | ✅ Opsiyonel | Aynı (kullanılmış kodları göster) |
| `includeExpired` | ✅ Opsiyonel | ✅ Opsiyonel | Aynı (süresi dolmuş kodları göster) |

### 3. Response Format
Response formatı **değişmedi** - aynı DTO yapısı kullanılıyor.

### 4. Yeni Error Responses
```dart
// 401 Unauthorized - Token yok veya geçersiz
{
  "success": false,
  "message": "Kullanıcı kimliği doğrulanamadı"
}

// 400 Bad Request - Kullanıcının telefon numarası yok
{
  "success": false,
  "message": "Kullanıcı telefon numarası bulunamadı"
}
```

## Güvenlik İyileştirmesi
- ✅ **IDOR Açığı Kapatıldı:** Farmer artık sadece kendi kodlarını görebilir
- ✅ **Token-Based Security:** JWT ile kimlik doğrulama
- ✅ **Role-Based Authorization:** Sadece Farmer rolü erişebilir
- ✅ **Data Isolation:** UserId → Phone lookup yapılarak sadece kullanıcının kodları döner

## Gerekli Değişiklikler (Mobile Tarafında)

### 1. Token Ekleme
```dart
// Kullanıcının JWT token'ını alın
final token = await AuthService.getToken(); // veya SharedPreferences'dan

// Her istekte Authorization header'ı ekleyin
final headers = {
  'Authorization': 'Bearer $token',
  'Content-Type': 'application/json',
};
```

### 2. Phone Parametresini Kaldırma
```dart
// ❌ KALDIR
final url = '$apiBase/sponsorship/farmer-inbox?phone=$userPhone';

// ✅ YENİ
final url = '$apiBase/sponsorship/farmer-inbox';
// veya filtrelerle:
final url = '$apiBase/sponsorship/farmer-inbox?includeUsed=true';
```

### 3. Error Handling Güncelleme
```dart
try {
  final response = await http.get(url, headers: headers);

  if (response.statusCode == 401) {
    // Token geçersiz veya expired
    // Kullanıcıyı login ekranına yönlendir
    await AuthService.logout();
    Navigator.pushReplacement(context, LoginPage());
    return;
  }

  if (response.statusCode == 400) {
    // Kullanıcının telefon numarası yok
    showError('Telefon numaranız kayıtlı değil');
    return;
  }

  // Normal response işleme...
  final data = jsonDecode(response.body);

} catch (e) {
  // Network error handling
}
```

## Tam Örnek (Flutter)

```dart
class SponsorshipInboxService {
  final String baseUrl = 'https://api.ziraai.com';

  Future<List<SponsorshipCode>> getFarmerInbox({
    bool includeUsed = false,
    bool includeExpired = false,
  }) async {
    try {
      // 1. Token'ı al
      final token = await AuthService.getToken();
      if (token == null) {
        throw Exception('User not authenticated');
      }

      // 2. URL'i oluştur (phone parametresi YOK!)
      final queryParams = {
        if (includeUsed) 'includeUsed': 'true',
        if (includeExpired) 'includeExpired': 'true',
      };

      final uri = Uri.parse('$baseUrl/api/v1/sponsorship/farmer-inbox')
          .replace(queryParameters: queryParams);

      // 3. Authorization header ile istek gönder
      final response = await http.get(
        uri,
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
        },
      );

      // 4. Response'u işle
      if (response.statusCode == 401) {
        // Token invalid - logout yap
        await AuthService.logout();
        throw Exception('Authentication failed');
      }

      if (response.statusCode == 400) {
        throw Exception('User has no phone number');
      }

      if (response.statusCode != 200) {
        throw Exception('Failed to load inbox');
      }

      // 5. JSON parse
      final jsonData = jsonDecode(response.body);
      final codes = (jsonData['data'] as List)
          .map((json) => SponsorshipCode.fromJson(json))
          .toList();

      return codes;

    } catch (e) {
      print('Error fetching inbox: $e');
      rethrow;
    }
  }
}
```

## Test Checklist

- [ ] Login yapılmış kullanıcı ile inbox açılıyor mu?
- [ ] Token expire olduğunda 401 alınıp login'e yönlendiriliyor mu?
- [ ] Telefon numarası olmayan kullanıcı için hata mesajı gösteriliyor mu?
- [ ] `includeUsed` ve `includeExpired` parametreleri çalışıyor mu?
- [ ] Response data formatı beklenen gibi mi? (değişmedi olmalı)

## Yayın Bilgisi
- **Branch:** feature/staging-testing
- **Commit:** c369c19
- **Deploy:** Staging environment'a deploy edildi
- **Production:** Henüz production'a deploy edilmedi

## İletişim
Sorularınız için: Backend Team

## Ek Dokümantasyon
Detaylı API dokümantasyonu için:
- `claudedocs/AdminOperations/API_SponsorshipInbox.md`
