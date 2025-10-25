# Mobile App - hasUnreadForCurrentUser Filter Integration

**Date:** 2025-10-24  
**Status:** ✅ Backend Ready  
**Endpoints:** Farmer & Sponsor Analysis List

---

## Problem Çözüldü

Mobile ekip isteği:
> Backend'e yeni filter parametreleri eklenebilir:
> 1. Farmer için: `hasUnreadFromSponsor=true` 
> 2. Sponsor için: `hasUnreadFromFarmer=true`  
> Ya da tek bir parametre: `hasUnreadForCurrentUser=true`

✅ **Çözüm:** `hasUnreadForCurrentUser=true` parametresi **her iki endpoint'e de eklendi**.

---

## Backend Endpoints

### 1️⃣ Farmer Endpoint

**URL:**
```http
GET /api/v1/PlantAnalyses/list?hasUnreadForCurrentUser=true
```

**Ne Yapar:**
- Sadece **sponsor'dan gelen okunmamış mesajları** filtreler
- `LastMessageBy == "sponsor" && UnreadCount > 0`

**Örnek:**
```dart
// Farmer için: Sadece sponsor'dan gelen okunmamış mesajlar
final response = await dio.get(
  '/api/v1/PlantAnalyses/list',
  queryParameters: {
    'page': 1,
    'pageSize': 20,
    'hasUnreadForCurrentUser': true,  // ✅ Yeni parametre
  },
);
```

### 2️⃣ Sponsor Endpoint

**URL:**
```http
GET /api/sponsorship/analyses?hasUnreadForCurrentUser=true
```

**Ne Yapar:**
- Sadece **farmer'dan gelen okunmamış mesajları** filtreler
- `LastMessageBy == "farmer" && UnreadCount > 0`

**Örnek:**
```dart
// Sponsor için: Sadece farmer'dan gelen okunmamış mesajlar
final response = await dio.get(
  '/api/sponsorship/analyses',
  queryParameters: {
    'page': 1,
    'pageSize': 20,
    'hasUnreadForCurrentUser': true,  // ✅ Yeni parametre
  },
);
```

---

## Farklar: hasUnreadMessages vs hasUnreadForCurrentUser

| Parametre | Farmer Endpoint | Sponsor Endpoint |
|-----------|-----------------|------------------|
| `hasUnreadMessages=true` | **Tüm** okunmamış mesajlar (farmer + sponsor'dan) | **Tüm** okunmamış mesajlar (farmer + sponsor'dan) |
| `hasUnreadForCurrentUser=true` | Sadece **sponsor'dan** okunmamış | Sadece **farmer'dan** okunmamış |

---

## Mobile Kullanım Örnekleri

### Senaryo 1: Farmer - "Sponsor'dan Yeni Mesajlarım"

```dart
class FarmerAnalysisScreen extends StatelessWidget {
  Future<void> loadUnreadFromSponsor() async {
    final response = await api.get(
      '/api/v1/PlantAnalyses/list',
      queryParameters: {
        'page': 1,
        'pageSize': 20,
        'hasUnreadForCurrentUser': true,  // ✅ Sadece sponsor'dan okunmamış
        'sortBy': 'lastMessageDate',
        'sortOrder': 'desc',
      },
    );
    
    // Response:
    // - Sadece sponsor'dan yeni mesaj gelen analizler
    // - hasUnreadFromSponsor: true olan kayıtlar
  }
}
```

### Senaryo 2: Sponsor - "Farmer'dan Yanıt Bekleyenler"

```dart
class SponsorDashboardScreen extends StatelessWidget {
  Future<void> loadPendingResponses() async {
    final response = await api.get(
      '/api/sponsorship/analyses',
      queryParameters: {
        'page': 1,
        'pageSize': 20,
        'hasUnreadForCurrentUser': true,  // ✅ Sadece farmer'dan okunmamış
        'sortBy': 'lastMessageDate',
        'sortOrder': 'desc',
      },
    );
    
    // Response:
    // - Sadece farmer'dan yeni mesaj gelen analizler
    // - hasUnreadFromFarmer: true olan kayıtlar
  }
}
```

### Senaryo 3: Kombinasyon - Aktif + Okunmamış

```dart
// Farmer: Aktif konuşmalarda sponsor'dan yeni mesaj
final activeUnread = await api.get(
  '/api/v1/PlantAnalyses/list',
  queryParameters: {
    'filterByMessageStatus': 'active',
    'hasUnreadForCurrentUser': true,
    'sortBy': 'unreadCount',
    'sortOrder': 'desc',
  },
);

// Sponsor: Aktif konuşmalarda farmer'dan yanıt bekleyenler
final pendingActive = await api.get(
  '/api/sponsorship/analyses',
  queryParameters: {
    'filterByMessageStatus': 'active',
    'hasUnreadForCurrentUser': true,
    'sortBy': 'lastMessageDate',
    'sortOrder': 'desc',
  },
);
```

---

## Response Fields

Her iki endpoint de response'da şu alanları döndürür:

```json
{
  "data": {
    "analyses": [
      {
        "id": 123,
        "unreadMessageCount": 5,
        "lastMessageDate": "2025-10-24T14:30:00",
        "lastMessageSenderRole": "sponsor",  // veya "farmer"
        "hasUnreadFromSponsor": true,        // Farmer için
        "hasUnreadFromFarmer": false,        // Sponsor için
        "conversationStatus": "Active"
      }
    ]
  }
}
```

**Fark:**
- Farmer response: `hasUnreadFromSponsor`
- Sponsor response: `hasUnreadFromFarmer`

---

## UI İmplementasyonu

### Farmer App - Unread Badge

```dart
class AnalysisListItem extends StatelessWidget {
  final Analysis analysis;
  
  Widget build(BuildContext context) {
    return ListTile(
      title: Text(analysis.cropType),
      trailing: analysis.hasUnreadFromSponsor
          ? Badge(
              label: Text('${analysis.unreadMessageCount}'),
              backgroundColor: Colors.red,
              child: Icon(Icons.message),
            )
          : null,
    );
  }
}
```

### Sponsor App - Priority Inbox

```dart
class SponsorInboxScreen extends StatelessWidget {
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      child: Column(
        children: [
          TabBar(tabs: [
            Tab(text: 'Tüm Konuşmalar'),
            Tab(text: 'Yanıt Bekleyenler'),  // hasUnreadForCurrentUser=true
          ]),
          TabBarView(children: [
            AllConversationsTab(),
            PendingResponsesTab(useUnreadFilter: true),
          ]),
        ],
      ),
    );
  }
}

class PendingResponsesTab extends StatelessWidget {
  Future<List<Analysis>> loadData() async {
    final response = await api.get(
      '/api/sponsorship/analyses',
      queryParameters: {
        'hasUnreadForCurrentUser': true,  // ✅ Farmer'dan yanıt bekleyenler
      },
    );
    return response.data.analyses;
  }
}
```

---

## Workaround Artık Gerekli Değil

**Eski Yöntem (Client-Side Filtering):**
```dart
// ❌ Artık buna gerek yok
final allAnalyses = await api.getAnalyses();
final unreadFromSponsor = allAnalyses
    .where((a) => a.hasUnreadFromSponsor == true)
    .toList();
```

**Yeni Yöntem (Backend Filtering):**
```dart
// ✅ Backend'de filtreleniyor
final unreadFromSponsor = await api.getAnalyses(
  hasUnreadForCurrentUser: true,
);
```

**Avantajlar:**
- ✅ Doğru pagination (backend'de filtrelendiği için)
- ✅ Performans (daha az veri transfer)
- ✅ Tutarlılık (tüm platformlarda aynı davranış)

---

## Testing

### Test 1: Farmer - Sponsor'dan Okunmamış

```bash
TOKEN="farmer_token"

curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/PlantAnalyses/list?hasUnreadForCurrentUser=true" \
  -H "Authorization: Bearer $TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

**Beklenen:**
- `hasUnreadFromSponsor: true` olan analizler
- `lastMessageSenderRole: "sponsor"`

### Test 2: Sponsor - Farmer'dan Okunmamış

```bash
TOKEN="sponsor_token"

curl -X GET "https://ziraai-api-sit.up.railway.app/api/sponsorship/analyses?hasUnreadForCurrentUser=true" \
  -H "Authorization: Bearer $TOKEN" \
  -H "x-dev-arch-version: 1.0"
```

**Beklenen:**
- `hasUnreadFromFarmer: true` olan analizler
- `lastMessageSenderRole: "farmer"`

---

## Migration Checklist

### Farmer App

- [ ] `hasUnreadForCurrentUser=true` parametresi ekle
- [ ] Client-side filtering kaldır
- [ ] UI'da badge gösterimini güncelle
- [ ] Test: Sadece sponsor'dan okunmamışlar görünmeli

### Sponsor App

- [ ] `hasUnreadForCurrentUser=true` parametresi ekle
- [ ] "Yanıt Bekleyenler" sekmesi ekle
- [ ] Priority inbox implementasyonu
- [ ] Test: Sadece farmer'dan okunmamışlar görünmeli

---

## Summary

✅ **Backend Hazır:**
- Farmer endpoint: `/api/v1/PlantAnalyses/list?hasUnreadForCurrentUser=true`
- Sponsor endpoint: `/api/sponsorship/analyses?hasUnreadForCurrentUser=true`

✅ **Mantık:**
- Farmer için: Sponsor'dan gelen okunmamışlar
- Sponsor için: Farmer'dan gelen okunmamışlar

✅ **Backward Compatible:**
- Mevcut `hasUnreadMessages` parametresi hala çalışıyor
- Yeni parametre **optional**

🎯 **Next Step:**
Mobile ekip bu parametreyi kullanarak client-side filtering'i kaldırabilir ve backend filtering'e geçebilir.
