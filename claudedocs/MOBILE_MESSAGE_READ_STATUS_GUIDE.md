# Mobile App - Mesaj Okundu İşaretleme Entegrasyonu

**Tarih:** 2025-10-24  
**Durum:** Backend hazır, mobile entegrasyonu gerekli  
**Sorun:** Mesajlar karşılıklı okunuyor ama okundu statüsü işaretlenmiyor

---

## Problem

Farmer ve Sponsor arasında mesajlaşma yapılıyor ancak:
- ❌ Mesajlar okunduğunda backend'e bildirim gönderilmiyor
- ❌ `IsRead` flag'i güncellenmıyor
- ❌ `UnreadMessageCount` doğru hesaplanmıyor
- ❌ Kullanıcılar gereksiz bildirimler alıyor

**Sonuç:** Her iki tarafta da okunmamış mesaj sayıları yanlış görünüyor.

---

## Çözüm - Backend Endpoint'leri Kullanımı

Backend'de **2 endpoint** zaten mevcut ve hazır:

### 1️⃣ Tek Mesajı Okundu İşaretle

**Endpoint:**
```http
PATCH /api/sponsorship/messages/{messageId}/read
Authorization: Bearer {token}
```

**Ne Zaman Kullanılır:**
- Mesaj detay ekranında mesaj görüntülendiğinde
- Chat bubble'a tıklandığında
- Bildirimden mesaja gidildiğinde

**Örnek Kullanım:**
```dart
Future<void> markMessageAsRead(int messageId) async {
  try {
    final response = await dio.patch(
      '/api/sponsorship/messages/$messageId/read',
      options: Options(headers: {
        'Authorization': 'Bearer $token',
      }),
    );
    
    if (response.data['success'] == true) {
      print('Message marked as read');
      // UI'ı güncelle: unread count'u azalt
      _decrementUnreadCount();
    }
  } catch (e) {
    print('Error marking message as read: $e');
  }
}
```

### 2️⃣ Toplu Mesajları Okundu İşaretle (Bulk)

**Endpoint:**
```http
PATCH /api/sponsorship/messages/read
Authorization: Bearer {token}
Content-Type: application/json

Body: [123, 124, 125, 126]  // Message ID'leri
```

**Ne Zaman Kullanılır:**
- Konuşma ekranı açıldığında (conversation view)
- Birden fazla mesaj scroll edildiğinde
- "Tümünü okundu işaretle" aksiyonunda

**Örnek Kullanım:**
```dart
Future<void> markMessagesAsRead(List<int> messageIds) async {
  if (messageIds.isEmpty) return;
  
  try {
    final response = await dio.patch(
      '/api/sponsorship/messages/read',
      data: messageIds,
      options: Options(headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
      }),
    );
    
    if (response.data['success'] == true) {
      final markedCount = response.data['data'];
      print('$markedCount messages marked as read');
      // UI'ı güncelle
      _refreshUnreadCounts();
    }
  } catch (e) {
    print('Error marking messages as read: $e');
  }
}
```

---

## Mobile Entegrasyon Stratejisi

### 1. **Mesaj Listesi Ekranı (Chat List)**

```dart
class AnalysisListScreen extends StatefulWidget {
  @override
  Widget build(BuildContext context) {
    return ListView.builder(
      itemBuilder: (context, index) {
        final analysis = analyses[index];
        
        return ListTile(
          title: Text(analysis.cropType),
          subtitle: Text(analysis.lastMessagePreview),
          trailing: analysis.unreadMessageCount > 0
              ? Badge(
                  label: Text('${analysis.unreadMessageCount}'),
                  backgroundColor: Colors.red,
                )
              : null,
          onTap: () {
            // Konuşma ekranına git
            Navigator.push(
              context,
              MaterialPageRoute(
                builder: (_) => ConversationScreen(
                  analysisId: analysis.id,
                  onMessagesRead: () {
                    // ✅ Callback ile unread count'u sıfırla
                    setState(() {
                      analysis.unreadMessageCount = 0;
                    });
                  },
                ),
              ),
            );
          },
        );
      },
    );
  }
}
```

### 2. **Konuşma Ekranı (Conversation Screen)**

```dart
class ConversationScreen extends StatefulWidget {
  final int analysisId;
  final VoidCallback onMessagesRead;
  
  const ConversationScreen({
    required this.analysisId,
    required this.onMessagesRead,
  });
  
  @override
  State<ConversationScreen> createState() => _ConversationScreenState();
}

class _ConversationScreenState extends State<ConversationScreen> {
  List<Message> messages = [];
  List<int> unreadMessageIds = [];
  
  @override
  void initState() {
    super.initState();
    _loadMessages();
  }
  
  Future<void> _loadMessages() async {
    // Mesajları yükle
    final response = await _apiService.getMessages(widget.analysisId);
    
    setState(() {
      messages = response.messages;
      
      // ✅ Okunmamış mesajları topla
      unreadMessageIds = messages
          .where((m) => !m.isRead && m.toUserId == currentUserId)
          .map((m) => m.id)
          .toList();
    });
    
    // ✅ Ekran açıldığında tüm okunmamış mesajları işaretle
    if (unreadMessageIds.isNotEmpty) {
      _markAllAsRead();
    }
  }
  
  Future<void> _markAllAsRead() async {
    try {
      await _apiService.markMessagesAsRead(unreadMessageIds);
      
      // ✅ Local state güncelle
      setState(() {
        for (var msg in messages) {
          if (unreadMessageIds.contains(msg.id)) {
            msg.isRead = true;
            msg.readDate = DateTime.now();
          }
        }
        unreadMessageIds.clear();
      });
      
      // ✅ Parent ekranı güncelle
      widget.onMessagesRead();
      
    } catch (e) {
      print('Failed to mark messages as read: $e');
    }
  }
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Conversation')),
      body: ListView.builder(
        itemCount: messages.length,
        itemBuilder: (context, index) {
          final message = messages[index];
          
          return ChatBubble(
            message: message,
            isMe: message.fromUserId == currentUserId,
            onVisible: () {
              // ✅ Mesaj görünür olduğunda işaretle (lazy loading için)
              if (!message.isRead && message.toUserId == currentUserId) {
                _markSingleMessageAsRead(message.id);
              }
            },
          );
        },
      ),
    );
  }
  
  Future<void> _markSingleMessageAsRead(int messageId) async {
    try {
      await _apiService.markMessageAsRead(messageId);
      
      setState(() {
        final msg = messages.firstWhere((m) => m.id == messageId);
        msg.isRead = true;
        msg.readDate = DateTime.now();
      });
      
    } catch (e) {
      print('Failed to mark message as read: $e');
    }
  }
}
```

### 3. **Push Notification'dan Gelme**

```dart
class NotificationHandler {
  Future<void> handleMessageNotification(Map<String, dynamic> data) async {
    final messageId = data['messageId'];
    final analysisId = data['analysisId'];
    
    // Konuşma ekranına git
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => ConversationScreen(analysisId: analysisId),
      ),
    );
    
    // ✅ Otomatik olarak ConversationScreen açıldığında
    //    initState içinde markAllAsRead çağrılacak
  }
}
```

---

## API Service Katmanı

```dart
class MessageApiService {
  final Dio dio;
  
  MessageApiService(this.dio);
  
  /// Tek mesajı okundu işaretle
  Future<ApiResult> markMessageAsRead(int messageId) async {
    try {
      final response = await dio.patch(
        '/api/sponsorship/messages/$messageId/read',
      );
      return ApiResult.success(response.data);
    } catch (e) {
      return ApiResult.error('Failed to mark message as read: $e');
    }
  }
  
  /// Toplu mesajları okundu işaretle
  Future<ApiResult<int>> markMessagesAsRead(List<int> messageIds) async {
    if (messageIds.isEmpty) {
      return ApiResult.success(0);
    }
    
    try {
      final response = await dio.patch(
        '/api/sponsorship/messages/read',
        data: messageIds,
      );
      
      final markedCount = response.data['data'];
      return ApiResult.success(markedCount);
    } catch (e) {
      return ApiResult.error('Failed to mark messages as read: $e');
    }
  }
  
  /// Konuşma mesajlarını getir
  Future<MessagesResponse> getMessages(int analysisId, {int page = 1}) async {
    final response = await dio.get(
      '/api/sponsorship/messages/$analysisId',
      queryParameters: {'page': page, 'pageSize': 50},
    );
    return MessagesResponse.fromJson(response.data);
  }
}
```

---

## State Management (Provider/Riverpod/Bloc)

### Provider Örneği

```dart
class MessageProvider extends ChangeNotifier {
  final MessageApiService _apiService;
  
  Map<int, int> _unreadCounts = {}; // analysisId -> unreadCount
  
  MessageProvider(this._apiService);
  
  int getUnreadCount(int analysisId) => _unreadCounts[analysisId] ?? 0;
  
  Future<void> markMessagesAsRead(int analysisId, List<int> messageIds) async {
    if (messageIds.isEmpty) return;
    
    final result = await _apiService.markMessagesAsRead(messageIds);
    
    if (result.isSuccess) {
      // ✅ Local state güncelle
      _unreadCounts[analysisId] = 0;
      notifyListeners();
      
      // ✅ Badge count güncelle (app icon)
      _updateAppBadgeCount();
    }
  }
  
  void _updateAppBadgeCount() {
    final totalUnread = _unreadCounts.values.fold(0, (sum, count) => sum + count);
    FlutterAppBadger.updateBadgeCount(totalUnread);
  }
}
```

---

## Test Senaryoları

### ✅ Senaryo 1: Konuşma Ekranı Açıldığında

**Akış:**
1. User, analysis listesinden bir conversation'a tıklar
2. `ConversationScreen.initState()` çağrılır
3. Mesajlar yüklenir
4. Okunmamış mesajların ID'leri toplanır: `[123, 124, 125]`
5. `PATCH /api/sponsorship/messages/read` çağrılır
6. Backend, 3 mesajı okundu işaretler
7. UI'da unread count 0 olarak güncellenir

**Beklenen:**
- ✅ `unreadMessageCount`: 3 → 0
- ✅ Backend'de `IsRead`: false → true
- ✅ `ReadDate`: null → 2025-10-24T14:30:00

### ✅ Senaryo 2: Push Notification'dan Gelme

**Akış:**
1. User, push notification'a tıklar
2. App, conversation screen'e direkt gider
3. `_loadMessages()` otomatik çalışır
4. Okunmamış mesaj varsa işaretlenir

**Beklenen:**
- ✅ Notification badge temizlenir
- ✅ Unread count güncellenir

### ✅ Senaryo 3: Yeni Mesaj Geldiğinde (SignalR)

**Akış:**
1. SignalR'dan yeni mesaj event'i gelir
2. Eğer user aynı conversation'daysa:
   - Mesaj otomatik okundu işaretlenir
3. Eğer user başka ekrandaysa:
   - Unread count artırılır
   - Push notification gönderilir

---

## SignalR Integration (Real-time)

```dart
class SignalRService {
  HubConnection? _connection;
  
  Future<void> connect(String token) async {
    _connection = HubConnectionBuilder()
        .withUrl(
          'https://ziraai-api-sit.up.railway.app/messagingHub',
          HttpConnectionOptions(accessTokenFactory: () async => token),
        )
        .build();
    
    // ✅ Yeni mesaj geldiğinde
    _connection!.on('ReceiveMessage', (args) {
      final message = Message.fromJson(args![0]);
      
      // Eğer user aynı conversation'daysa otomatik işaretle
      if (_isUserInConversation(message.analysisId)) {
        _markMessageAsRead(message.id);
      } else {
        // Unread count artır
        _incrementUnreadCount(message.analysisId);
      }
    });
    
    // ✅ Mesaj okundu event'i (karşı taraf okuduğunda)
    _connection!.on('MessageRead', (args) {
      final data = args![0] as Map<String, dynamic>;
      final messageId = data['messageId'];
      final readByUserId = data['readByUserId'];
      
      // UI'da double check mark göster
      _updateMessageReadStatus(messageId, readByUserId);
    });
    
    await _connection!.start();
  }
}
```

---

## Backend Response Örnekleri

### Tek Mesaj İşaretleme

**Request:**
```http
PATCH /api/sponsorship/messages/123/read
Authorization: Bearer eyJhbGci...
```

**Response:**
```json
{
  "success": true,
  "message": "Message marked as read"
}
```

### Toplu Mesaj İşaretleme

**Request:**
```http
PATCH /api/sponsorship/messages/read
Content-Type: application/json
Authorization: Bearer eyJhbGci...

[123, 124, 125, 126]
```

**Response:**
```json
{
  "data": 4,
  "success": true,
  "message": "4 message(s) marked as read"
}
```

---

## Implementasyon Checklist

### Mobile App Tarafı

- [ ] `MessageApiService` sınıfı oluştur
  - [ ] `markMessageAsRead(int messageId)` metodu
  - [ ] `markMessagesAsRead(List<int> messageIds)` metodu

- [ ] `ConversationScreen` güncelle
  - [ ] `initState()` içinde okunmamış mesajları topla
  - [ ] Ekran açıldığında `markMessagesAsRead()` çağır
  - [ ] Yeni mesaj geldiğinde otomatik işaretle

- [ ] `AnalysisListScreen` güncelle
  - [ ] `onMessagesRead` callback ekle
  - [ ] Unread count state güncellemesi

- [ ] State Management
  - [ ] Provider/Riverpod ile unread count takibi
  - [ ] App badge count güncelleme

- [ ] SignalR Integration
  - [ ] `ReceiveMessage` event handler
  - [ ] `MessageRead` event handler
  - [ ] Auto-mark logic

- [ ] Testing
  - [ ] Unit testler (API service)
  - [ ] Widget testler (UI updates)
  - [ ] Integration testler (end-to-end flow)

### Backend Tarafı (Hazır ✅)

- ✅ `MarkMessageAsReadCommand` handler
- ✅ `MarkMessagesAsReadCommand` handler (bulk)
- ✅ API endpoints (`PATCH /api/sponsorship/messages/{id}/read`)
- ✅ API endpoints (`PATCH /api/sponsorship/messages/read`)
- ✅ SignalR `MessageRead` event broadcasting
- ✅ Database güncellemeleri (`IsRead`, `ReadDate`, `MessageStatus`)

---

## Önemli Notlar

### 🔒 Güvenlik

- Backend, sadece **mesajın alıcısının** (ToUserId) mesajı okundu işaretlemesine izin verir
- Başka kullanıcının mesajını işaretleyemezsiniz
- Authorization token zorunlu

### ⚡ Performance

- Toplu işlem endpoint'i kullanın (bulk) - tek tek yerine
- 50+ mesaj varsa sayfalama kullanın
- Debounce/throttle ile gereksiz API çağrılarını önleyin

### 📱 UX İyileştirmeleri

1. **Optimistic UI Update:**
   ```dart
   // API çağrısı öncesi UI'ı güncelle
   setState(() => message.isRead = true);
   
   try {
     await api.markAsRead(message.id);
   } catch (e) {
     // Hata varsa geri al
     setState(() => message.isRead = false);
   }
   ```

2. **Offline Support:**
   ```dart
   // Offline ise local'de işaretle, online olunca sync et
   if (await connectivity.isOnline()) {
     await api.markAsRead(messageId);
   } else {
     await localStorage.queueReadStatus(messageId);
   }
   ```

3. **Read Receipts (Double Check):**
   - WhatsApp tarzı mavi tik gösterimi
   - SignalR `MessageRead` event'i dinle
   - Karşı taraf okuduğunda UI'da göster

---

## Sonuç

**Backend hazır, mobile entegrasyonu gerekli!**

### Yapılması Gerekenler:

1. ✅ `PATCH /api/sponsorship/messages/{id}/read` endpoint'ini kullan
2. ✅ `PATCH /api/sponsorship/messages/read` bulk endpoint'ini kullan
3. ✅ ConversationScreen açıldığında otomatik işaretle
4. ✅ SignalR ile real-time güncelleme
5. ✅ Unread count state management

### Beklenen Sonuç:

- ✅ Mesajlar okunduğunda backend'e bildirilecek
- ✅ Unread count doğru hesaplanacak
- ✅ Gereksiz bildirimler azalacak
- ✅ UX gelişecek (read receipts, accurate counts)

---

**Not:** Bu dokümantasyon Flutter/Dart örneği içeriyor ama aynı mantık React Native, Native iOS/Android için de geçerlidir.
