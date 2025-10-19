# Backend Requirements: flutter_chat_ui Advanced Features

**Tarih:** 19 Ekim 2025
**Amaç:** flutter_chat_ui 2.9.0 kütüphanesinin gelişmiş özelliklerini destekleyecek backend API değişiklikleri
**Platform:** ZiraAI Mobile - Sponsor-Farmer Messaging System

---

## 📋 MEVCUT DURUM ANALİZİ

### Mevcut API Endpoints

#### 1. **POST** `/api/v1/sponsorship/messages`
**Amaç:** Mesaj gönderme

**Request Body:**
```json
{
  "fromUserId": 123,
  "toUserId": 456,
  "farmerId": 456,
  "plantAnalysisId": 789,
  "message": "Merhaba, bitkiniz hakkında...",
  "messageContent": "Merhaba, bitkiniz hakkında...",
  "messageType": "text",
  "subject": "Analiz Hakkında",
  "priority": "normal",
  "category": "consultation"
}
```

**Eksikler:**
- ❌ Attachment URL desteği yok
- ❌ Message status (sent/delivered/read) yok
- ❌ Reply-to (mesaja yanıt) desteği yok
- ❌ Forward (iletme) desteği yok

#### 2. **GET** `/api/v1/sponsorship/messages/conversation`
**Amaç:** Konuşma mesajlarını getirme

**Query Parameters:**
```
farmerId: int
plantAnalysisId: int
```

**Response (Tahmin Edilen):**
```json
{
  "success": true,
  "data": {
    "messages": [
      {
        "id": 1,
        "plantAnalysisId": 789,
        "fromUserId": 123,
        "toUserId": 456,
        "message": "Mesaj içeriği",
        "senderRole": "Sponsor",
        "senderName": "Ahmet Yılmaz",
        "senderCompany": "Tarım A.Ş.",
        "messageType": "text",
        "isRead": false,
        "sentDate": "2025-10-19T10:30:00Z"
      }
    ],
    "canReply": true
  }
}
```

**Eksikler:**
- ❌ Avatar URL'leri yok
- ❌ Message status detayı yok (delivered/seen timestamps)
- ❌ Attachment metadata yok
- ❌ Typing indicator için WebSocket/SignalR endpoint yok
- ❌ Read receipts için ayrı endpoint yok

### Mevcut Domain Model (Mobile)

```dart
class Message {
  final int id;
  final int plantAnalysisId;
  final int fromUserId;
  final int toUserId;
  final String message;
  final String senderRole;        // "Sponsor" | "Farmer"
  final String? messageType;      // "text" | null
  final String? subject;
  final String? senderName;
  final String? senderCompany;
  final String? priority;
  final String? category;
  final bool isRead;
  final DateTime sentDate;
  final DateTime? readDate;
}
```

---

## 🎯 ÖZELLİK GEREKSİNİMLERİ

### 1️⃣ **AVATAR SUPPORT** ⭐⭐⭐

#### Backend Değişiklikleri

##### A. User Profil Tablosuna Yeni Alanlar
```sql
ALTER TABLE Users ADD COLUMN avatar_url VARCHAR(500);
ALTER TABLE Users ADD COLUMN avatar_thumbnail_url VARCHAR(500);
```

##### B. Message Response'una Avatar Bilgisi Ekleme

**Güncellenmiş Response:**
```json
{
  "id": 1,
  "fromUserId": 123,
  "toUserId": 456,
  "message": "Merhaba",
  "senderRole": "Sponsor",
  "senderName": "Ahmet Yılmaz",
  "senderCompany": "Tarım A.Ş.",
  "senderAvatarUrl": "https://cdn.ziraai.com/avatars/user_123.jpg",        // ✅ YENİ
  "senderAvatarThumbnailUrl": "https://cdn.ziraai.com/avatars/thumb_123.jpg", // ✅ YENİ
  "sentDate": "2025-10-19T10:30:00Z",
  "isRead": false
}
```

##### C. Avatar Upload Endpoint
```
POST /api/v1/users/avatar
Content-Type: multipart/form-data

Body:
- file: <image_file>
- userId: int

Response:
{
  "success": true,
  "data": {
    "avatarUrl": "https://cdn.ziraai.com/avatars/user_123.jpg",
    "thumbnailUrl": "https://cdn.ziraai.com/avatars/thumb_123.jpg"
  }
}
```

**İş Kuralları:**
- Avatar boyutu max 5MB
- Desteklenen formatlar: JPG, PNG, WebP
- Otomatik thumbnail oluşturma (150x150px)
- CDN entegrasyonu (Azure Blob Storage / AWS S3)

---

### 2️⃣ **MESSAGE STATUS & READ RECEIPTS** ⭐⭐⭐

#### Backend Değişiklikleri

##### A. Message Tablosuna Yeni Alanlar
```sql
ALTER TABLE Messages ADD COLUMN status VARCHAR(20) DEFAULT 'sent';
ALTER TABLE Messages ADD COLUMN delivered_date DATETIME NULL;
ALTER TABLE Messages ADD COLUMN read_date DATETIME NULL;
```

**Status Enum:**
- `sending` - Gönderiliyor (client tarafında)
- `sent` - Sunucuya ulaştı
- `delivered` - Alıcıya iletildi
- `seen` - Alıcı tarafından görüldü
- `error` - Gönderim hatası

##### B. Güncellenmiş Message Response
```json
{
  "id": 1,
  "message": "Merhaba",
  "status": "seen",                           // ✅ YENİ
  "sentDate": "2025-10-19T10:30:00Z",
  "deliveredDate": "2025-10-19T10:30:05Z",    // ✅ YENİ
  "readDate": "2025-10-19T10:35:00Z",         // ✅ YENİ
  "isRead": true
}
```

##### C. Mark as Read Endpoint
```
PATCH /api/v1/sponsorship/messages/{messageId}/read
Authorization: Bearer {token}

Response:
{
  "success": true,
  "data": {
    "messageId": 1,
    "readDate": "2025-10-19T10:35:00Z",
    "status": "seen"
  }
}
```

##### D. Bulk Mark as Read
```
POST /api/v1/sponsorship/messages/mark-read
Authorization: Bearer {token}

Body:
{
  "messageIds": [1, 2, 3, 4, 5],
  "plantAnalysisId": 789
}

Response:
{
  "success": true,
  "data": {
    "markedCount": 5
  }
}
```

---

### 3️⃣ **ATTACHMENT SUPPORT** ⭐⭐⭐

#### Backend Değişiklikleri

##### A. Message Tablosuna Attachment Desteği
```sql
ALTER TABLE Messages ADD COLUMN has_attachment BOOLEAN DEFAULT FALSE;
ALTER TABLE Messages ADD COLUMN attachment_url VARCHAR(500);
ALTER TABLE Messages ADD COLUMN attachment_type VARCHAR(50);  -- 'image', 'video', 'file', 'voice'
ALTER TABLE Messages ADD COLUMN attachment_size BIGINT;        -- bytes
ALTER TABLE Messages ADD COLUMN attachment_filename VARCHAR(255);
ALTER TABLE Messages ADD COLUMN attachment_mime_type VARCHAR(100);
ALTER TABLE Messages ADD COLUMN attachment_thumbnail_url VARCHAR(500);
```

##### B. Send Message with Attachment
```
POST /api/v1/sponsorship/messages/with-attachment
Authorization: Bearer {token}
Content-Type: multipart/form-data

Body:
- fromUserId: int
- toUserId: int
- plantAnalysisId: int
- message: string (optional)
- file: <file>
- messageType: string ('image' | 'file' | 'video' | 'voice')

Response:
{
  "success": true,
  "data": {
    "id": 123,
    "message": "Bitki görseli",
    "hasAttachment": true,
    "attachmentUrl": "https://cdn.ziraai.com/messages/plant_123.jpg",
    "attachmentType": "image",
    "attachmentSize": 245678,
    "attachmentFilename": "plant_photo.jpg",
    "attachmentMimeType": "image/jpeg",
    "attachmentThumbnailUrl": "https://cdn.ziraai.com/messages/thumb_plant_123.jpg",
    "sentDate": "2025-10-19T10:40:00Z"
  }
}
```

##### C. Message Response with Attachment
```json
{
  "id": 123,
  "message": "Bitki görseli",
  "messageType": "image",
  "hasAttachment": true,                                                       // ✅ YENİ
  "attachmentUrl": "https://cdn.ziraai.com/messages/plant_123.jpg",          // ✅ YENİ
  "attachmentType": "image",                                                  // ✅ YENİ
  "attachmentSize": 245678,                                                   // ✅ YENİ
  "attachmentFilename": "plant_photo.jpg",                                   // ✅ YENİ
  "attachmentMimeType": "image/jpeg",                                        // ✅ YENİ
  "attachmentThumbnailUrl": "https://cdn.ziraai.com/messages/thumb_plant_123.jpg", // ✅ YENİ
  "sentDate": "2025-10-19T10:40:00Z"
}
```

**İş Kuralları:**
- Maksimum dosya boyutu: 10MB (image), 50MB (video), 5MB (file)
- Desteklenen image formatları: JPG, PNG, WebP, HEIC
- Desteklenen video formatları: MP4, MOV, AVI
- Otomatik thumbnail oluşturma (image ve video için)
- Virus tarama entegrasyonu
- CDN yükleme

---

### 4️⃣ **TYPING INDICATOR (Real-time)** ⭐⭐

#### Backend Değişiklikleri

##### A. SignalR/WebSocket Hub Ekleme

**SignalR Hub:**
```csharp
public class MessagingHub : Hub
{
    // User typing notification
    public async Task UserTyping(int plantAnalysisId, int userId, string userName)
    {
        await Clients.OthersInGroup($"analysis_{plantAnalysisId}")
            .SendAsync("UserTyping", new
            {
                PlantAnalysisId = plantAnalysisId,
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow
            });
    }

    // User stopped typing
    public async Task UserStoppedTyping(int plantAnalysisId, int userId)
    {
        await Clients.OthersInGroup($"analysis_{plantAnalysisId}")
            .SendAsync("UserStoppedTyping", new
            {
                PlantAnalysisId = plantAnalysisId,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });
    }

    // Join conversation group
    public async Task JoinConversation(int plantAnalysisId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"analysis_{plantAnalysisId}");
    }

    // Leave conversation group
    public async Task LeaveConversation(int plantAnalysisId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"analysis_{plantAnalysisId}");
    }
}
```

**SignalR Connection Endpoint:**
```
wss://api.ziraai.com/hubs/messaging
```

**Mobile Client Events:**
```dart
// Connect to SignalR
connection.on('UserTyping', (data) {
  // Show typing indicator
  // data: { plantAnalysisId, userId, userName, timestamp }
});

connection.on('UserStoppedTyping', (data) {
  // Hide typing indicator
});

// Send typing event
connection.invoke('UserTyping', plantAnalysisId, userId, userName);
connection.invoke('UserStoppedTyping', plantAnalysisId, userId);
```

---

### 5️⃣ **MESSAGE ACTIONS (Long Press)** ⭐⭐

#### Backend Değişiklikleri

##### A. Delete Message Endpoint
```
DELETE /api/v1/sponsorship/messages/{messageId}
Authorization: Bearer {token}

Response:
{
  "success": true,
  "data": {
    "messageId": 123,
    "deletedAt": "2025-10-19T11:00:00Z"
  }
}
```

**İş Kuralları:**
- Sadece gönderen silebilir
- Soft delete (mesaj veritabanında kalır, isDeleted flag)
- 24 saat içinde gönderilmiş mesajlar silinebilir

##### B. Edit Message Endpoint
```
PATCH /api/v1/sponsorship/messages/{messageId}
Authorization: Bearer {token}

Body:
{
  "message": "Düzeltilmiş mesaj içeriği"
}

Response:
{
  "success": true,
  "data": {
    "id": 123,
    "message": "Düzeltilmiş mesaj içeriği",
    "isEdited": true,
    "editedAt": "2025-10-19T11:05:00Z"
  }
}
```

**İş Kuralları:**
- Sadece gönderen düzenleyebilir
- 1 saat içinde gönderilmiş mesajlar düzenlenebilir
- "Düzenlendi" flag'i gösterilir

##### C. Forward Message
```
POST /api/v1/sponsorship/messages/{messageId}/forward
Authorization: Bearer {token}

Body:
{
  "toUserId": 789,
  "plantAnalysisId": 999
}

Response:
{
  "success": true,
  "data": {
    "newMessageId": 456,
    "forwardedFrom": 123
  }
}
```

---

### 6️⃣ **LINK PREVIEW** ⭐

#### Backend Değişiklikleri

##### A. Link Metadata Extraction
```
POST /api/v1/sponsorship/messages/link-preview
Authorization: Bearer {token}

Body:
{
  "url": "https://www.ziraai.com/blog/bitki-bakim-rehberi"
}

Response:
{
  "success": true,
  "data": {
    "url": "https://www.ziraai.com/blog/bitki-bakim-rehberi",
    "title": "Bitki Bakım Rehberi",
    "description": "Bitkilerinizin sağlıklı büyümesi için...",
    "imageUrl": "https://www.ziraai.com/images/og-image.jpg",
    "siteName": "ZiraAI Blog"
  }
}
```

##### B. Message with Link Metadata
```json
{
  "id": 123,
  "message": "Bu rehberi okuyun: https://www.ziraai.com/blog/rehber",
  "hasLinks": true,                                                     // ✅ YENİ
  "linkPreviews": [                                                     // ✅ YENİ
    {
      "url": "https://www.ziraai.com/blog/rehber",
      "title": "Bitki Bakım Rehberi",
      "description": "Sağlıklı büyüme ipuçları",
      "imageUrl": "https://www.ziraai.com/images/og.jpg",
      "siteName": "ZiraAI Blog"
    }
  ]
}
```

---

### 7️⃣ **VOICE MESSAGES** ⭐⭐

#### Backend Değişiklikleri

##### A. Voice Message Upload
```
POST /api/v1/sponsorship/messages/voice
Authorization: Bearer {token}
Content-Type: multipart/form-data

Body:
- fromUserId: int
- toUserId: int
- plantAnalysisId: int
- audioFile: <file>
- duration: int (seconds)

Response:
{
  "success": true,
  "data": {
    "id": 789,
    "messageType": "voice",
    "voiceUrl": "https://cdn.ziraai.com/voice/msg_789.m4a",
    "voiceDuration": 15,  // seconds
    "waveformData": [0.2, 0.5, 0.8, 0.6, ...],  // Audio waveform for visualization
    "sentDate": "2025-10-19T12:00:00Z"
  }
}
```

**İş Kuralları:**
- Maksimum süre: 60 saniye
- Desteklenen formatlar: M4A, AAC, MP3
- Otomatik waveform oluşturma

---

### 8️⃣ **REAL-TIME MESSAGE UPDATES** ⭐⭐⭐

#### Backend Değişiklikleri

##### A. SignalR New Message Event
```csharp
// When new message is sent
public async Task SendMessage(SendMessageCommand message)
{
    // Save to database
    var savedMessage = await _repository.SaveMessage(message);

    // Broadcast to conversation group
    await _hubContext.Clients.Group($"analysis_{message.PlantAnalysisId}")
        .SendAsync("NewMessage", new
        {
            Message = savedMessage,
            Timestamp = DateTime.UtcNow
        });
}
```

**Mobile Client:**
```dart
connection.on('NewMessage', (data) {
  // Add message to chat UI in real-time
  messagingBloc.add(NewMessageReceivedEvent(data['Message']));
});
```

##### B. SignalR Message Status Update
```csharp
public async Task MessageStatusUpdated(int messageId, string status)
{
    await _hubContext.Clients.Group($"message_{messageId}")
        .SendAsync("MessageStatusUpdated", new
        {
            MessageId = messageId,
            Status = status,  // 'delivered' | 'seen'
            Timestamp = DateTime.UtcNow
        });
}
```

---

## 📊 VERİTABANI SCHEMA DEĞİŞİKLİKLERİ

### Migration Script

```sql
-- 1. User Avatar Support
ALTER TABLE Users ADD COLUMN avatar_url VARCHAR(500);
ALTER TABLE Users ADD COLUMN avatar_thumbnail_url VARCHAR(500);
ALTER TABLE Users ADD COLUMN avatar_uploaded_date DATETIME;

-- 2. Message Status & Receipts
ALTER TABLE Messages ADD COLUMN status VARCHAR(20) DEFAULT 'sent';
ALTER TABLE Messages ADD COLUMN delivered_date DATETIME NULL;
ALTER TABLE Messages ADD COLUMN read_date DATETIME NULL;
ALTER TABLE Messages ADD COLUMN is_edited BOOLEAN DEFAULT FALSE;
ALTER TABLE Messages ADD COLUMN edited_date DATETIME NULL;
ALTER TABLE Messages ADD COLUMN is_deleted BOOLEAN DEFAULT FALSE;
ALTER TABLE Messages ADD COLUMN deleted_date DATETIME NULL;

-- 3. Attachment Support
ALTER TABLE Messages ADD COLUMN has_attachment BOOLEAN DEFAULT FALSE;
ALTER TABLE Messages ADD COLUMN attachment_url VARCHAR(500);
ALTER TABLE Messages ADD COLUMN attachment_type VARCHAR(50);
ALTER TABLE Messages ADD COLUMN attachment_size BIGINT;
ALTER TABLE Messages ADD COLUMN attachment_filename VARCHAR(255);
ALTER TABLE Messages ADD COLUMN attachment_mime_type VARCHAR(100);
ALTER TABLE Messages ADD COLUMN attachment_thumbnail_url VARCHAR(500);
ALTER TABLE Messages ADD COLUMN attachment_duration INT;  -- For voice/video

-- 4. Link Previews
CREATE TABLE MessageLinkPreviews (
    id INT PRIMARY KEY IDENTITY(1,1),
    message_id INT NOT NULL,
    url VARCHAR(1000) NOT NULL,
    title VARCHAR(500),
    description TEXT,
    image_url VARCHAR(500),
    site_name VARCHAR(255),
    created_date DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (message_id) REFERENCES Messages(id)
);

-- 5. Message Reactions (Future Feature)
CREATE TABLE MessageReactions (
    id INT PRIMARY KEY IDENTITY(1,1),
    message_id INT NOT NULL,
    user_id INT NOT NULL,
    reaction VARCHAR(50) NOT NULL,  -- 'like', 'love', 'helpful', etc.
    created_date DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (message_id) REFERENCES Messages(id),
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- 6. Indexes for Performance
CREATE INDEX idx_messages_status ON Messages(status);
CREATE INDEX idx_messages_plant_analysis ON Messages(plant_analysis_id);
CREATE INDEX idx_messages_users ON Messages(from_user_id, to_user_id);
CREATE INDEX idx_messages_delivered_date ON Messages(delivered_date);
CREATE INDEX idx_messages_read_date ON Messages(read_date);
```

---

## 🔌 YENİ API ENDPOINTS ÖZETİ

### Kullanıcı Profil
| Method | Endpoint | Amaç |
|--------|----------|------|
| POST | `/api/v1/users/avatar` | Avatar yükleme |
| GET | `/api/v1/users/{userId}/avatar` | Avatar URL getirme |

### Mesajlaşma - Temel
| Method | Endpoint | Amaç |
|--------|----------|------|
| POST | `/api/v1/sponsorship/messages/with-attachment` | Ek dosyalı mesaj gönderme |
| PATCH | `/api/v1/sponsorship/messages/{id}` | Mesaj düzenleme |
| DELETE | `/api/v1/sponsorship/messages/{id}` | Mesaj silme |
| POST | `/api/v1/sponsorship/messages/{id}/forward` | Mesaj iletme |

### Mesajlaşma - Durum
| Method | Endpoint | Amaç |
|--------|----------|------|
| PATCH | `/api/v1/sponsorship/messages/{id}/read` | Mesajı okundu işaretle |
| POST | `/api/v1/sponsorship/messages/mark-read` | Toplu okundu işareti |

### Mesajlaşma - Özel Tipler
| Method | Endpoint | Amaç |
|--------|----------|------|
| POST | `/api/v1/sponsorship/messages/voice` | Sesli mesaj gönderme |
| POST | `/api/v1/sponsorship/messages/link-preview` | Link önizleme getirme |

### Real-time (SignalR)
| Event | Amaç |
|-------|------|
| `UserTyping` | Kullanıcı yazıyor bildirimi |
| `UserStoppedTyping` | Yazma durdu |
| `NewMessage` | Yeni mesaj geldi |
| `MessageStatusUpdated` | Mesaj durumu güncellendi |
| `JoinConversation` | Konuşmaya katıl |
| `LeaveConversation` | Konuşmadan ayrıl |

---

## 📱 MOBILE CLIENT DEĞİŞİKLİKLERİ

### Güncellenmiş Message Entity

```dart
class Message {
  final int id;
  final int plantAnalysisId;
  final int fromUserId;
  final int toUserId;
  final String message;
  final String senderRole;
  final String? messageType;  // 'text', 'image', 'voice', 'video', 'file'

  // Avatar Support
  final String? senderAvatarUrl;              // ✅ YENİ
  final String? senderAvatarThumbnailUrl;     // ✅ YENİ

  // Message Status
  final String status;                        // ✅ YENİ: 'sent', 'delivered', 'seen'
  final DateTime sentDate;
  final DateTime? deliveredDate;              // ✅ YENİ
  final DateTime? readDate;                   // ✅ YENİ

  // Attachment Support
  final bool hasAttachment;                   // ✅ YENİ
  final String? attachmentUrl;                // ✅ YENİ
  final String? attachmentType;               // ✅ YENİ
  final int? attachmentSize;                  // ✅ YENİ
  final String? attachmentFilename;           // ✅ YENİ
  final String? attachmentThumbnailUrl;       // ✅ YENİ
  final int? attachmentDuration;              // ✅ YENİ (voice/video)

  // Edit/Delete
  final bool isEdited;                        // ✅ YENİ
  final DateTime? editedDate;                 // ✅ YENİ
  final bool isDeleted;                       // ✅ YENİ

  // Link Previews
  final List<LinkPreview>? linkPreviews;      // ✅ YENİ
}

class LinkPreview {
  final String url;
  final String? title;
  final String? description;
  final String? imageUrl;
  final String? siteName;
}
```

---

## 🎯 IMPLEMENTATION ROADMAP

### Phase 1: Temel Özellikler (2 hafta)
- ✅ Avatar Support
  - User avatar upload endpoint
  - Avatar URL'leri message response'una ekleme
  - CDN entegrasyonu
- ✅ Message Status & Read Receipts
  - Database schema güncellemesi
  - Mark as read endpoint
  - Status tracking

### Phase 2: Multimedya (2 hafta)
- ✅ Attachment Support
  - Image upload ve serving
  - File upload ve serving
  - Thumbnail generation
  - CDN optimization
- ✅ Voice Messages
  - Audio upload
  - Waveform generation

### Phase 3: Real-time (1 hafta)
- ✅ SignalR Hub kurulumu
- ✅ Typing indicator events
- ✅ Real-time message delivery
- ✅ Status update broadcasts

### Phase 4: Gelişmiş Özellikler (1 hafta)
- ✅ Link Preview
- ✅ Message Edit/Delete
- ✅ Message Forward

---

## 🔒 GÜVENLİK KONSİDERASYONLARI

### Dosya Yükleme Güvenliği
1. **Dosya Türü Doğrulama**
   - MIME type kontrolü
   - Magic bytes kontrolü
   - Extension whitelist

2. **Dosya Boyutu Limitleri**
   - Image: 10MB
   - Video: 50MB
   - Voice: 5MB
   - File: 5MB

3. **Virus Tarama**
   - ClamAV entegrasyonu
   - Karantina mekanizması

4. **CDN Güvenliği**
   - Signed URLs
   - Time-limited access
   - Origin verification

### API Güvenliği
1. **Rate Limiting**
   - Mesaj gönderme: 10/dakika
   - Dosya yükleme: 5/dakika
   - Avatar yükleme: 3/saat

2. **Authorization**
   - Sadece ilgili kullanıcılar mesajları görebilir
   - Tier-based messaging restrictions
   - Plant analysis ownership validation

---

## 📝 TEST SENARYOLARI

### Avatar Tests
- ✅ Avatar yükleme başarılı
- ✅ Geçersiz format red