# Mobile Backend API Integration Guide

**Last Updated**: 2025-10-19
**Backend Branch**: `feature/sponsor-farmer-chat-enhancements`
**API Base URL**:
- Development: `https://localhost:5001`
- Staging: `https://ziraai-api-sit.up.railway.app`
- Production: `https://api.ziraai.com`

---

## 📋 Table of Contents

1. [Authentication](#authentication)
2. [Messaging Endpoints](#messaging-endpoints)
3. [Avatar Management](#avatar-management)
4. [Feature Flags](#feature-flags)
5. [SignalR Real-time Events](#signalr-real-time-events)
6. [Response Models](#response-models)
7. [Error Handling](#error-handling)
8. [Testing Guide](#testing-guide)

---

## 🔐 Authentication

All endpoints require JWT Bearer authentication.

### Headers Required
```http
Authorization: Bearer {access_token}
Content-Type: application/json
```

### Getting Access Token

**Endpoint**: `POST /api/auth/verify-phone-otp`

**Request**:
```json
{
  "mobilePhone": "+905551234567",
  "code": "123456"
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here",
    "userId": 165,
    "fullName": "User 1113",
    "mobilePhone": "+905551234567",
    "tokenExpiration": "2025-10-19T12:00:00Z"
  },
  "message": "Login successful"
}
```

---

## 💬 Messaging Endpoints

### 1. Get Conversation Messages

**Purpose**: Retrieve all messages in a conversation between two users for a specific plant analysis

**Endpoint**: `GET /api/sponsorship/messages/conversation`

**Query Parameters**:
- `fromUserId` (int, required): Current user's ID
- `toUserId` (int, required): Other participant's user ID
- `plantAnalysisId` (int, required): Plant analysis ID for this conversation

**Example Request**:
```http
GET /api/sponsorship/messages/conversation?fromUserId=165&toUserId=159&plantAnalysisId=60
Authorization: Bearer {token}
```

**Response Structure**:
```json
{
  "success": true,
  "data": [
    {
      "id": 16,
      "plantAnalysisId": 60,
      "fromUserId": 165,
      "toUserId": 159,
      "message": "Merhaba, bitkiniz hakkında bilgi almak istiyorum",
      "messageType": "Information",
      "subject": null,

      // Message Status (Phase 1B)
      "messageStatus": "delivered",
      "isRead": false,
      "sentDate": "2025-10-19T10:50:49Z",
      "deliveredDate": "2025-10-19T10:50:50Z",
      "readDate": null,

      // Sender Information
      "senderRole": "Farmer",
      "senderName": "User 1113",
      "senderCompany": "",

      // Avatar Support (Phase 1A)
      "senderAvatarUrl": "https://api.ziraai.com/avatars/user_165.jpg",
      "senderAvatarThumbnailUrl": "https://api.ziraai.com/avatars/thumbs/user_165_thumb.jpg",

      // Message Classification
      "priority": "Normal",
      "category": "General",

      // Attachment Support (Phase 2A)
      "hasAttachments": false,
      "attachmentCount": 0,
      "attachmentUrls": null,
      "attachmentTypes": null,
      "attachmentSizes": null,
      "attachmentNames": null,

      // Voice Message Support (Phase 2B)
      "isVoiceMessage": false,
      "voiceMessageUrl": null,
      "voiceMessageDuration": null,
      "voiceMessageWaveform": null,

      // Edit/Delete/Forward Support (Phase 4)
      "isEdited": false,
      "editedDate": null,
      "isForwarded": false,
      "forwardedFromMessageId": null,
      "isActive": true
    },
    {
      "id": 17,
      "plantAnalysisId": 60,
      "fromUserId": 159,
      "toUserId": 165,
      "message": "Tabii, size yardımcı olmaktan mutluluk duyarım",
      "messageType": "Information",
      "subject": null,
      "messageStatus": "read",
      "isRead": true,
      "sentDate": "2025-10-19T11:00:00Z",
      "deliveredDate": "2025-10-19T11:00:01Z",
      "readDate": "2025-10-19T11:05:00Z",
      "senderRole": "Sponsor",
      "senderName": "Sponsor User",
      "senderCompany": "AgriTech Inc",
      "senderAvatarUrl": "https://api.ziraai.com/avatars/user_159.jpg",
      "senderAvatarThumbnailUrl": "https://api.ziraai.com/avatars/thumbs/user_159_thumb.jpg",
      "priority": "Normal",
      "category": "General",
      "hasAttachments": false,
      "attachmentCount": 0,
      "attachmentUrls": null,
      "attachmentTypes": null,
      "attachmentSizes": null,
      "attachmentNames": null,
      "isVoiceMessage": false,
      "voiceMessageUrl": null,
      "voiceMessageDuration": null,
      "voiceMessageWaveform": null,
      "isEdited": false,
      "editedDate": null,
      "isForwarded": false,
      "forwardedFromMessageId": null,
      "isActive": true
    }
  ],
  "message": null
}
```

**UI Usage**:
- Display messages in chat bubbles
- Show sender avatar using `senderAvatarThumbnailUrl`
- Show message status icons based on `messageStatus`:
  - `"sent"` → Single checkmark ✓
  - `"delivered"` → Double checkmark ✓✓
  - `"read"` → Blue double checkmark ✓✓
- Render attachments if `hasAttachments: true`
- Render voice player if `isVoiceMessage: true`
- Show "edited" badge if `isEdited: true`
- Hide message if `isActive: false` (deleted message)

---

### 2. Send Text Message

**Purpose**: Send a simple text message to another user

**Endpoint**: `POST /api/sponsorship/messages/send`

**Request Body**:
```json
{
  "fromUserId": 165,
  "toUserId": 159,
  "plantAnalysisId": 60,
  "message": "Teşekkürler, çok yardımcı oldunuz!",
  "messageType": "Information",
  "priority": "Normal",
  "category": "General"
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "id": 18,
    "plantAnalysisId": 60,
    "fromUserId": 165,
    "toUserId": 159,
    "message": "Teşekkürler, çok yardımcı oldunuz!",
    "messageType": "Information",
    "subject": null,
    "messageStatus": "sent",
    "isRead": false,
    "sentDate": "2025-10-19T11:30:00Z",
    "deliveredDate": null,
    "readDate": null,
    "senderRole": "Farmer",
    "senderName": "User 1113",
    "senderCompany": "",
    "senderAvatarUrl": "https://api.ziraai.com/avatars/user_165.jpg",
    "senderAvatarThumbnailUrl": "https://api.ziraai.com/avatars/thumbs/user_165_thumb.jpg",
    "priority": "Normal",
    "category": "General",
    "hasAttachments": false,
    "attachmentCount": 0,
    "attachmentUrls": null,
    "attachmentTypes": null,
    "attachmentSizes": null,
    "attachmentNames": null,
    "isVoiceMessage": false,
    "voiceMessageUrl": null,
    "voiceMessageDuration": null,
    "voiceMessageWaveform": null,
    "isEdited": false,
    "editedDate": null,
    "isForwarded": false,
    "forwardedFromMessageId": null,
    "isActive": true
  },
  "message": "Message sent successfully"
}
```

**UI Usage**:
- Immediately append message to chat UI
- Show "sending" indicator until success
- Update to delivered status when SignalR confirms

---

### 3. Send Message with Attachments

**Purpose**: Send a message with one or multiple file attachments (images, documents)

**Endpoint**: `POST /api/sponsorship/messages/attachments`

**Content-Type**: `multipart/form-data`

**Form Data**:
```
toUserId: 159
plantAnalysisId: 60
message: "Fotoğrafları inceleyebilir misiniz?"
messageType: "Information"
attachments[0]: [binary file - photo1.jpg]
attachments[1]: [binary file - photo2.jpg]
attachments[2]: [binary file - report.pdf]
```

**Example Request (cURL)**:
```bash
curl -X POST "https://api.ziraai.com/api/sponsorship/messages/attachments" \
  -H "Authorization: Bearer {token}" \
  -F "toUserId=159" \
  -F "plantAnalysisId=60" \
  -F "message=Fotoğrafları inceleyebilir misiniz?" \
  -F "messageType=Information" \
  -F "attachments=@photo1.jpg" \
  -F "attachments=@photo2.jpg" \
  -F "attachments=@report.pdf"
```

**Response**:
```json
{
  "success": true,
  "data": {
    "id": 19,
    "plantAnalysisId": 60,
    "fromUserId": 165,
    "toUserId": 159,
    "message": "Fotoğrafları inceleyebilir misiniz?",
    "messageType": "Information",
    "subject": null,
    "messageStatus": "sent",
    "isRead": false,
    "sentDate": "2025-10-19T12:00:00Z",
    "deliveredDate": null,
    "readDate": null,
    "senderRole": "Farmer",
    "senderName": "User 1113",
    "senderCompany": "",
    "senderAvatarUrl": "https://api.ziraai.com/avatars/user_165.jpg",
    "senderAvatarThumbnailUrl": "https://api.ziraai.com/avatars/thumbs/user_165_thumb.jpg",
    "priority": "Normal",
    "category": "General",

    // Attachment metadata
    "hasAttachments": true,
    "attachmentCount": 3,
    "attachmentUrls": [
      "https://i.freeimage.host/abc123.jpg",
      "https://i.freeimage.host/def456.jpg",
      "https://localhost:5001/attachments/report_165_638123456789.pdf"
    ],
    "attachmentTypes": [
      "image/jpeg",
      "image/jpeg",
      "application/pdf"
    ],
    "attachmentSizes": [
      1024567,
      2048123,
      345678
    ],
    "attachmentNames": [
      "photo1.jpg",
      "photo2.jpg",
      "report.pdf"
    ],

    "isVoiceMessage": false,
    "voiceMessageUrl": null,
    "voiceMessageDuration": null,
    "voiceMessageWaveform": null,
    "isEdited": false,
    "editedDate": null,
    "isForwarded": false,
    "forwardedFromMessageId": null,
    "isActive": true
  },
  "message": "Message sent with 3 attachment(s)"
}
```

**UI Usage**:
```dart
// Render attachments
if (message.hasAttachments) {
  for (int i = 0; i < message.attachmentCount; i++) {
    final url = message.attachmentUrls[i];
    final type = message.attachmentTypes[i];
    final name = message.attachmentNames[i];
    final size = message.attachmentSizes[i];

    if (type.startsWith('image/')) {
      // Show image thumbnail
      Image.network(url);
    } else if (type == 'application/pdf') {
      // Show PDF icon with download button
      FileAttachment(name: name, size: size, url: url);
    }
  }
}
```

**Storage Notes**:
- **Images** (`image/*`): Stored on FreeImageHost → `https://i.freeimage.host/...`
- **Documents** (PDFs, etc.): Stored locally → `https://api.ziraai.com/attachments/...`
- Maximum file size: 10MB (images), 5MB (documents)
- Maximum attachments per message: No hard limit, but validate on UI

---

### 4. Send Voice Message

**Purpose**: Send a voice recording message (XL tier required)

**Endpoint**: `POST /api/sponsorship/messages/voice`

**Content-Type**: `multipart/form-data`

**Form Data**:
```
toUserId: 159
plantAnalysisId: 60
voiceFile: [binary audio file - .m4a, .aac, or .mp3]
duration: 45
waveform: "[0.1, 0.5, 0.8, 0.6, 0.3, ...]"
```

**Example Request (cURL)**:
```bash
curl -X POST "https://api.ziraai.com/api/sponsorship/messages/voice" \
  -H "Authorization: Bearer {token}" \
  -F "toUserId=159" \
  -F "plantAnalysisId=60" \
  -F "voiceFile=@voice_note.m4a" \
  -F "duration=45" \
  -F 'waveform=[0.1,0.5,0.8,0.6,0.3,0.9,0.4,0.7]'
```

**Response**:
```json
{
  "success": true,
  "data": {
    "id": 20,
    "plantAnalysisId": 60,
    "fromUserId": 165,
    "toUserId": 159,
    "message": "[Voice Message]",
    "messageType": "VoiceMessage",
    "subject": null,
    "messageStatus": "sent",
    "isRead": false,
    "sentDate": "2025-10-19T12:15:00Z",
    "deliveredDate": null,
    "readDate": null,
    "senderRole": "Farmer",
    "senderName": "User 1113",
    "senderCompany": "",
    "senderAvatarUrl": "https://api.ziraai.com/avatars/user_165.jpg",
    "senderAvatarThumbnailUrl": "https://api.ziraai.com/avatars/thumbs/user_165_thumb.jpg",
    "priority": "Normal",
    "category": "General",
    "hasAttachments": false,
    "attachmentCount": 0,
    "attachmentUrls": null,
    "attachmentTypes": null,
    "attachmentSizes": null,
    "attachmentNames": null,

    // Voice message data
    "isVoiceMessage": true,
    "voiceMessageUrl": "https://localhost:5001/voice-messages/voice_msg_165_638123456789.m4a",
    "voiceMessageDuration": 45,
    "voiceMessageWaveform": "[0.1,0.5,0.8,0.6,0.3,0.9,0.4,0.7]",

    "isEdited": false,
    "editedDate": null,
    "isForwarded": false,
    "forwardedFromMessageId": null,
    "isActive": true
  },
  "message": "Voice message sent (45s)"
}
```

**UI Usage**:
```dart
if (message.isVoiceMessage) {
  VoiceMessagePlayer(
    url: message.voiceMessageUrl,
    duration: message.voiceMessageDuration,
    waveform: jsonDecode(message.voiceMessageWaveform),
    onPlay: () { /* Play audio */ },
    onPause: () { /* Pause audio */ },
  );
}
```

**Validation**:
- ✅ User must have **XL tier** subscription
- ✅ Maximum duration: **60 seconds**
- ✅ Maximum file size: **5 MB**
- ✅ Allowed formats: `.m4a`, `.aac`, `.mp3`

**Error Response** (Tier Restriction):
```json
{
  "success": false,
  "message": "VoiceMessages requires XL tier. Your tier: L",
  "errors": ["Voice message feature requires XL tier subscription"]
}
```

---

### 5. Mark Message as Read

**Purpose**: Mark a single message as read and send read receipt to sender

**Endpoint**: `PATCH /api/sponsorship/messages/{messageId}/read`

**Path Parameters**:
- `messageId` (int): Message ID to mark as read

**Request Body**: Empty

**Example Request**:
```http
PATCH /api/sponsorship/messages/16/read
Authorization: Bearer {token}
```

**Response**:
```json
{
  "success": true,
  "message": "Message marked as read"
}
```

**Side Effect**:
- Message `isRead` → `true`
- Message `readDate` → current timestamp
- Message `messageStatus` → `"read"`
- **SignalR event sent** to sender: `MessageRead`

**SignalR Event Payload** (sent to sender):
```json
{
  "messageId": 16,
  "readByUserId": 159,
  "readAt": "2025-10-19T12:30:00Z"
}
```

**UI Usage**:
```dart
// When user opens message thread, mark all unread messages as read
for (var message in unreadMessages) {
  await api.markMessageAsRead(message.id);
}

// Listen for read receipts via SignalR
hubConnection.on('MessageRead', (data) {
  final messageId = data['messageId'];
  final readAt = data['readAt'];

  // Update UI to show blue checkmarks
  updateMessageStatus(messageId, 'read', readAt);
});
```

---

### 6. Bulk Mark Messages as Read

**Purpose**: Mark multiple messages as read in one request

**Endpoint**: `PATCH /api/sponsorship/messages/bulk-read`

**Request Body**:
```json
{
  "messageIds": [16, 17, 18, 19, 20]
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "updatedCount": 5
  },
  "message": "5 messages marked as read"
}
```

**UI Usage**:
```dart
// Mark entire conversation as read when user opens it
final unreadIds = messages
    .where((m) => !m.isRead && m.toUserId == currentUserId)
    .map((m) => m.id)
    .toList();

if (unreadIds.isNotEmpty) {
  await api.bulkMarkAsRead(unreadIds);
}
```

---

### 7. Edit Message

**Purpose**: Edit the content of a previously sent message (L tier required)

**Endpoint**: `PUT /api/sponsorship/messages/{messageId}`

**Path Parameters**:
- `messageId` (int): Message ID to edit

**Request Body**:
```json
{
  "newContent": "Düzeltilmiş mesaj içeriği (typo düzeltildi)"
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "id": 16,
    "message": "Düzeltilmiş mesaj içeriği (typo düzeltildi)",
    "isEdited": true,
    "editedDate": "2025-10-19T12:45:00Z"
  },
  "message": "Message edited successfully"
}
```

**Validation**:
- ✅ User must be the sender (`fromUserId == currentUserId`)
- ✅ User must have **L tier or higher**
- ✅ Message type must be `"Information"` (text only, no voice/attachments)
- ✅ Message sent within last **1 hour** (`sentDate + 1h > now`)
- ✅ Message not deleted (`isActive == true`)

**Error Response** (Time Limit Exceeded):
```json
{
  "success": false,
  "message": "Message can only be edited within 1 hour of sending",
  "errors": ["Edit time limit (3600 seconds) exceeded"]
}
```

**UI Usage**:
```dart
// Show "Edit" button only if:
// 1. Message is from current user
// 2. Message is text-only (not voice/attachment)
// 3. Sent within last hour
// 4. User has L tier or higher
bool canEdit(Message msg) {
  final sentTime = msg.sentDate;
  final oneHourAgo = DateTime.now().subtract(Duration(hours: 1));

  return msg.fromUserId == currentUserId &&
         !msg.isVoiceMessage &&
         !msg.hasAttachments &&
         sentTime.isAfter(oneHourAgo) &&
         userTier >= 'L';
}

// Show "edited" badge
if (message.isEdited) {
  Text('Edited ${timeAgo(message.editedDate)}');
}
```

---

### 8. Delete Message

**Purpose**: Soft delete a message (marks as deleted, doesn't remove from DB)

**Endpoint**: `DELETE /api/sponsorship/messages/{messageId}`

**Path Parameters**:
- `messageId` (int): Message ID to delete

**Request Body**: Empty

**Response**:
```json
{
  "success": true,
  "message": "Message deleted successfully"
}
```

**Validation**:
- ✅ User must be the sender (`fromUserId == currentUserId`)
- ✅ Message sent within last **24 hours** (`sentDate + 24h > now`)
- ✅ Message not already deleted (`isActive == true`)

**Side Effect**:
- Message `isActive` → `false` (soft delete)
- Message content preserved in database
- Message appears as "deleted" in UI

**Error Response** (Time Limit Exceeded):
```json
{
  "success": false,
  "message": "Message can only be deleted within 24 hours of sending",
  "errors": ["Delete time limit (86400 seconds) exceeded"]
}
```

**UI Usage**:
```dart
// Show "Delete" button only if:
// 1. Message is from current user
// 2. Sent within last 24 hours
bool canDelete(Message msg) {
  final sentTime = msg.sentDate;
  final twentyFourHoursAgo = DateTime.now().subtract(Duration(hours: 24));

  return msg.fromUserId == currentUserId &&
         sentTime.isAfter(twentyFourHoursAgo);
}

// Render deleted message
if (!message.isActive) {
  return Container(
    child: Text(
      'Bu mesaj silindi',
      style: TextStyle(fontStyle: FontStyle.italic, color: Colors.grey),
    ),
  );
}
```

---

### 9. Forward Message

**Purpose**: Forward an existing message to another conversation (L tier required)

**Endpoint**: `POST /api/sponsorship/messages/{messageId}/forward`

**Path Parameters**:
- `messageId` (int): Original message ID to forward

**Request Body**:
```json
{
  "toUserId": 170,
  "plantAnalysisId": 75
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "id": 21,
    "plantAnalysisId": 75,
    "fromUserId": 165,
    "toUserId": 170,
    "message": "İletilen mesaj: Orijinal mesaj içeriği...",
    "messageType": "Information",
    "messageStatus": "sent",
    "isRead": false,
    "sentDate": "2025-10-19T13:00:00Z",
    "senderAvatarUrl": "https://api.ziraai.com/avatars/user_165.jpg",
    "isForwarded": true,
    "forwardedFromMessageId": 16,
    "isActive": true
  },
  "message": "Message forwarded successfully"
}
```

**Validation**:
- ✅ User must have **L tier or higher**
- ✅ Original message must exist and be active
- ✅ User must have permission to message the recipient

**UI Usage**:
```dart
// Show "Forward" button
// When forwarding, show conversation selector

// Display forwarded message badge
if (message.isForwarded) {
  Row(
    children: [
      Icon(Icons.forward, size: 12),
      Text('Forwarded', style: TextStyle(fontSize: 10)),
    ],
  );
}
```

---

## 👤 Avatar Management

### 1. Upload Avatar

**Purpose**: Upload a new profile picture for the current user

**Endpoint**: `POST /api/users/avatar`

**Content-Type**: `multipart/form-data`

**Form Data**:
```
file: [binary image file - .jpg, .png, .heic]
```

**Example Request (cURL)**:
```bash
curl -X POST "https://api.ziraai.com/api/users/avatar" \
  -H "Authorization: Bearer {token}" \
  -F "file=@profile_photo.jpg"
```

**Response**:
```json
{
  "success": true,
  "data": {
    "avatarUrl": "https://i.freeimage.host/user_165_638123456789.jpg",
    "avatarThumbnailUrl": "https://i.freeimage.host/user_165_638123456789_thumb.jpg"
  },
  "message": "Avatar uploaded successfully"
}
```

**Processing**:
- Original image resized to **512x512px**
- Thumbnail created at **128x128px**
- Uploaded to FreeImageHost (dev/staging) or S3 (production)
- User record updated with new URLs

**Validation**:
- ✅ Maximum file size: **5 MB**
- ✅ Allowed formats: `.jpg`, `.jpeg`, `.png`, `.heic`, `.webp`
- ✅ Image must be square or will be center-cropped

**UI Usage**:
```dart
// Select image from gallery/camera
final image = await ImagePicker().pickImage(source: ImageSource.gallery);

// Upload
final response = await api.uploadAvatar(image);

// Update UI
setState(() {
  currentUser.avatarUrl = response.data.avatarUrl;
  currentUser.avatarThumbnailUrl = response.data.avatarThumbnailUrl;
});
```

---

### 2. Get User Avatar

**Purpose**: Retrieve avatar URLs for a specific user

**Endpoint**: `GET /api/users/{userId}/avatar`

**Path Parameters**:
- `userId` (int): User ID

**Response**:
```json
{
  "success": true,
  "data": {
    "userId": 165,
    "avatarUrl": "https://i.freeimage.host/user_165_638123456789.jpg",
    "avatarThumbnailUrl": "https://i.freeimage.host/user_165_638123456789_thumb.jpg",
    "updatedDate": "2025-10-19T10:00:00Z"
  }
}
```

**UI Usage**:
```dart
// Show avatar in user profile or message list
CircleAvatar(
  backgroundImage: NetworkImage(user.avatarThumbnailUrl ?? defaultAvatar),
  radius: 20,
);
```

---

### 3. Delete Avatar

**Purpose**: Remove current user's profile picture

**Endpoint**: `DELETE /api/users/avatar`

**Request Body**: Empty

**Response**:
```json
{
  "success": true,
  "message": "Avatar deleted successfully"
}
```

**Side Effect**:
- User `avatarUrl` → `null`
- User `avatarThumbnailUrl` → `null`
- Image files deleted from storage

**UI Usage**:
```dart
// Show default avatar after deletion
setState(() {
  currentUser.avatarUrl = null;
  currentUser.avatarThumbnailUrl = null;
});
```

---

## 🎛️ Feature Flags

### Get User Features

**Purpose**: Retrieve feature availability for current user based on subscription tier

**Endpoint**: `GET /api/sponsorship/messaging/features`

**Query Parameters**: None (user identified from JWT token)

**Response**:
```json
{
  "success": true,
  "data": {
    "voiceMessages": {
      "enabled": true,
      "available": false,
      "requiredTier": "XL",
      "maxFileSize": 5242880,
      "maxDuration": 60,
      "allowedTypes": ["audio/m4a", "audio/aac", "audio/mp3", "audio/mpeg"],
      "unavailableReason": "Requires XL tier (your tier: L)"
    },
    "imageAttachments": {
      "enabled": true,
      "available": true,
      "requiredTier": "L",
      "maxFileSize": 10485760,
      "maxDuration": null,
      "allowedTypes": ["image/jpeg", "image/png", "image/webp", "image/heic"],
      "unavailableReason": null
    },
    "videoAttachments": {
      "enabled": true,
      "available": false,
      "requiredTier": "XL",
      "maxFileSize": 52428800,
      "maxDuration": 60,
      "allowedTypes": ["video/mp4", "video/mov", "video/avi"],
      "unavailableReason": "Requires XL tier (your tier: L)"
    },
    "fileAttachments": {
      "enabled": true,
      "available": true,
      "requiredTier": "L",
      "maxFileSize": 5242880,
      "maxDuration": null,
      "allowedTypes": ["application/pdf", "application/msword", "text/plain"],
      "unavailableReason": null
    },
    "messageEdit": {
      "enabled": false,
      "available": false,
      "requiredTier": "L",
      "maxFileSize": null,
      "maxDuration": null,
      "timeLimit": 3600,
      "unavailableReason": "MessageEdit feature is currently disabled"
    },
    "messageDelete": {
      "enabled": true,
      "available": true,
      "requiredTier": "None",
      "maxFileSize": null,
      "maxDuration": null,
      "timeLimit": 86400,
      "unavailableReason": null
    },
    "messageForward": {
      "enabled": false,
      "available": false,
      "requiredTier": "L",
      "unavailableReason": "MessageForward feature is currently disabled"
    },
    "typingIndicator": {
      "enabled": true,
      "available": true,
      "requiredTier": "None",
      "unavailableReason": null
    },
    "linkPreview": {
      "enabled": false,
      "available": false,
      "requiredTier": "None",
      "unavailableReason": "LinkPreview feature is currently disabled"
    }
  }
}
```

**UI Usage**:
```dart
// Fetch features on app start
final features = await api.getMessagingFeatures();

// Show/hide UI elements based on availability
Widget buildAttachmentButton() {
  if (features.imageAttachments.available) {
    return IconButton(
      icon: Icon(Icons.attach_file),
      onPressed: () => selectImage(),
    );
  } else {
    return IconButton(
      icon: Icon(Icons.lock),
      onPressed: () => showUpgradeDialog(features.imageAttachments.unavailableReason),
    );
  }
}

// Validate file before upload
bool canUploadVoice(File audioFile) {
  final feature = features.voiceMessages;

  if (!feature.available) {
    showError(feature.unavailableReason);
    return false;
  }

  if (audioFile.lengthSync() > feature.maxFileSize) {
    showError('File too large. Max: ${feature.maxFileSize / 1024 / 1024}MB');
    return false;
  }

  return true;
}
```

**Feature Status Meanings**:
- `enabled: false` → Feature globally disabled by admin
- `available: false` → User doesn't have required tier
- `unavailableReason` → Human-readable explanation

---

## 📡 SignalR Real-time Events

### Connection Setup

**Hub URL**: `/hubs/plantanalysis`

**Connection Code (Dart)**:
```dart
import 'package:signalr_netcore/signalr_netcore.dart';

class SignalRService {
  HubConnection? _hubConnection;

  Future<void> connect(String accessToken) async {
    _hubConnection = HubConnectionBuilder()
        .withUrl(
          'https://api.ziraai.com/hubs/plantanalysis',
          HttpConnectionOptions(
            accessTokenFactory: () => Future.value(accessToken),
          ),
        )
        .build();

    // Register event listeners
    _hubConnection!.on('UserTyping', _handleUserTyping);
    _hubConnection!.on('NewMessage', _handleNewMessage);
    _hubConnection!.on('MessageRead', _handleMessageRead);

    await _hubConnection!.start();
    print('✅ SignalR connected');
  }

  void _handleUserTyping(List<Object?>? arguments) {
    final data = arguments![0] as Map<String, dynamic>;
    print('User ${data['userId']} is typing: ${data['isTyping']}');

    // Update UI to show/hide typing indicator
    onUserTyping?.call(
      userId: data['userId'],
      plantAnalysisId: data['plantAnalysisId'],
      isTyping: data['isTyping'],
    );
  }

  void _handleNewMessage(List<Object?>? arguments) {
    final data = arguments![0] as Map<String, dynamic>;
    print('New message received: ${data['messageId']}');

    // Fetch message details and update UI
    onNewMessage?.call(messageId: data['messageId']);
  }

  void _handleMessageRead(List<Object?>? arguments) {
    final data = arguments![0] as Map<String, dynamic>;
    print('Message ${data['messageId']} was read');

    // Update message status to "read"
    onMessageRead?.call(
      messageId: data['messageId'],
      readByUserId: data['readByUserId'],
      readAt: DateTime.parse(data['readAt']),
    );
  }
}
```

---

### Event 1: UserTyping

**Purpose**: Show/hide "User is typing..." indicator in real-time

**Client → Server** (Send typing status):
```dart
// User starts typing
await _hubConnection!.invoke(
  'StartTyping',
  args: [conversationUserId, plantAnalysisId],
);

// User stops typing (after 3 seconds of inactivity or sends message)
await _hubConnection!.invoke(
  'StopTyping',
  args: [conversationUserId, plantAnalysisId],
);
```

**Server → Client** (Receive typing status):
```json
{
  "userId": "165",
  "plantAnalysisId": 60,
  "isTyping": true,
  "timestamp": "2025-10-19T13:30:00Z"
}
```

**UI Implementation**:
```dart
class ChatScreen extends StatefulWidget {
  @override
  _ChatScreenState createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  bool otherUserTyping = false;
  Timer? _typingTimer;

  void _onTextChanged(String text) {
    // Send StartTyping when user types
    signalR.startTyping(widget.otherUserId, widget.plantAnalysisId);

    // Reset timer
    _typingTimer?.cancel();
    _typingTimer = Timer(Duration(seconds: 3), () {
      // Stop typing after 3 seconds of inactivity
      signalR.stopTyping(widget.otherUserId, widget.plantAnalysisId);
    });
  }

  void _onTypingReceived(int userId, bool isTyping) {
    if (userId == widget.otherUserId) {
      setState(() {
        otherUserTyping = isTyping;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        // Messages list
        Expanded(child: MessagesList()),

        // Typing indicator
        if (otherUserTyping)
          Padding(
            padding: EdgeInsets.all(8),
            child: Text(
              '${widget.otherUserName} is typing...',
              style: TextStyle(fontStyle: FontStyle.italic, color: Colors.grey),
            ),
          ),

        // Input field
        TextField(
          onChanged: _onTextChanged,
          onSubmitted: (text) {
            _typingTimer?.cancel();
            signalR.stopTyping(widget.otherUserId, widget.plantAnalysisId);
          },
        ),
      ],
    );
  }
}
```

---

### Event 2: NewMessage

**Purpose**: Receive instant notification when a new message arrives

**Server → Client** (New message notification):
```json
{
  "messageId": 22,
  "senderId": "165",
  "plantAnalysisId": 60,
  "timestamp": "2025-10-19T13:35:00Z"
}
```

**UI Implementation**:
```dart
void _onNewMessage(int messageId) async {
  // Fetch full message details
  final message = await api.getMessage(messageId);

  // Add to current conversation if it matches
  if (message.plantAnalysisId == currentPlantAnalysisId) {
    setState(() {
      messages.add(message);
    });

    // Scroll to bottom
    _scrollController.animateTo(
      _scrollController.position.maxScrollExtent,
      duration: Duration(milliseconds: 300),
      curve: Curves.easeOut,
    );

    // Mark as read if conversation is open
    if (isConversationVisible) {
      await api.markMessageAsRead(messageId);
    }
  } else {
    // Update unread count in conversation list
    updateUnreadCount(message.plantAnalysisId);
  }

  // Play notification sound
  audioPlayer.play('assets/sounds/message.mp3');

  // Show notification if app is in background
  if (!isAppInForeground) {
    showLocalNotification(
      title: message.senderName,
      body: message.isVoiceMessage ? '🎤 Voice message' : message.message,
    );
  }
}
```

---

### Event 3: MessageRead

**Purpose**: Receive instant notification when recipient reads your message

**Server → Client** (Read receipt):
```json
{
  "messageId": 18,
  "readByUserId": "159",
  "readAt": "2025-10-19T13:40:00Z"
}
```

**UI Implementation**:
```dart
void _onMessageRead(int messageId, int readByUserId, DateTime readAt) {
  setState(() {
    // Find message and update status
    final index = messages.indexWhere((m) => m.id == messageId);
    if (index != -1) {
      messages[index] = messages[index].copyWith(
        isRead: true,
        readDate: readAt,
        messageStatus: 'read',
      );
    }
  });

  print('✓✓ Message $messageId read at $readAt');
}

// Render message with status indicator
Widget buildMessageBubble(Message message) {
  return Row(
    children: [
      Text(message.message),
      SizedBox(width: 4),
      _buildStatusIcon(message.messageStatus),
    ],
  );
}

Widget _buildStatusIcon(String status) {
  switch (status) {
    case 'sent':
      return Icon(Icons.check, size: 14, color: Colors.grey);
    case 'delivered':
      return Icon(Icons.done_all, size: 14, color: Colors.grey);
    case 'read':
      return Icon(Icons.done_all, size: 14, color: Colors.blue);
    default:
      return Icon(Icons.access_time, size: 14, color: Colors.grey);
  }
}
```

---

## 📦 Response Models

### MessageModel (Complete)

```dart
class MessageModel {
  final int id;
  final int plantAnalysisId;
  final int fromUserId;
  final int toUserId;
  final String message;
  final String messageType;
  final String? subject;

  // Status
  final String messageStatus; // "sent", "delivered", "read"
  final bool isRead;
  final DateTime sentDate;
  final DateTime? deliveredDate;
  final DateTime? readDate;

  // Sender
  final String senderRole;
  final String senderName;
  final String senderCompany;
  final String? senderAvatarUrl;
  final String? senderAvatarThumbnailUrl;

  // Classification
  final String priority;
  final String category;

  // Attachments
  final bool hasAttachments;
  final int attachmentCount;
  final List<String>? attachmentUrls;
  final List<String>? attachmentTypes;
  final List<int>? attachmentSizes;
  final List<String>? attachmentNames;

  // Voice
  final bool isVoiceMessage;
  final String? voiceMessageUrl;
  final int? voiceMessageDuration;
  final String? voiceMessageWaveform;

  // Edit/Delete/Forward
  final bool isEdited;
  final DateTime? editedDate;
  final bool isForwarded;
  final int? forwardedFromMessageId;
  final bool isActive;

  MessageModel.fromJson(Map<String, dynamic> json)
      : id = json['id'],
        plantAnalysisId = json['plantAnalysisId'],
        fromUserId = json['fromUserId'],
        toUserId = json['toUserId'],
        message = json['message'] ?? '',
        messageType = json['messageType'] ?? 'Information',
        subject = json['subject'],
        messageStatus = json['messageStatus'] ?? 'sent',
        isRead = json['isRead'] ?? false,
        sentDate = DateTime.parse(json['sentDate']),
        deliveredDate = json['deliveredDate'] != null
            ? DateTime.parse(json['deliveredDate'])
            : null,
        readDate = json['readDate'] != null
            ? DateTime.parse(json['readDate'])
            : null,
        senderRole = json['senderRole'] ?? '',
        senderName = json['senderName'] ?? '',
        senderCompany = json['senderCompany'] ?? '',
        senderAvatarUrl = json['senderAvatarUrl'],
        senderAvatarThumbnailUrl = json['senderAvatarThumbnailUrl'],
        priority = json['priority'] ?? 'Normal',
        category = json['category'] ?? 'General',
        hasAttachments = json['hasAttachments'] ?? false,
        attachmentCount = json['attachmentCount'] ?? 0,
        attachmentUrls = json['attachmentUrls'] != null
            ? List<String>.from(json['attachmentUrls'])
            : null,
        attachmentTypes = json['attachmentTypes'] != null
            ? List<String>.from(json['attachmentTypes'])
            : null,
        attachmentSizes = json['attachmentSizes'] != null
            ? List<int>.from(json['attachmentSizes'])
            : null,
        attachmentNames = json['attachmentNames'] != null
            ? List<String>.from(json['attachmentNames'])
            : null,
        isVoiceMessage = json['isVoiceMessage'] ?? false,
        voiceMessageUrl = json['voiceMessageUrl'],
        voiceMessageDuration = json['voiceMessageDuration'],
        voiceMessageWaveform = json['voiceMessageWaveform'],
        isEdited = json['isEdited'] ?? false,
        editedDate = json['editedDate'] != null
            ? DateTime.parse(json['editedDate'])
            : null,
        isForwarded = json['isForwarded'] ?? false,
        forwardedFromMessageId = json['forwardedFromMessageId'],
        isActive = json['isActive'] ?? true;
}
```

---

### MessagingFeaturesModel

```dart
class MessagingFeaturesModel {
  final FeatureDetail voiceMessages;
  final FeatureDetail imageAttachments;
  final FeatureDetail videoAttachments;
  final FeatureDetail fileAttachments;
  final FeatureDetail messageEdit;
  final FeatureDetail messageDelete;
  final FeatureDetail messageForward;
  final FeatureDetail typingIndicator;
  final FeatureDetail linkPreview;

  MessagingFeaturesModel.fromJson(Map<String, dynamic> json)
      : voiceMessages = FeatureDetail.fromJson(json['voiceMessages']),
        imageAttachments = FeatureDetail.fromJson(json['imageAttachments']),
        videoAttachments = FeatureDetail.fromJson(json['videoAttachments']),
        fileAttachments = FeatureDetail.fromJson(json['fileAttachments']),
        messageEdit = FeatureDetail.fromJson(json['messageEdit']),
        messageDelete = FeatureDetail.fromJson(json['messageDelete']),
        messageForward = FeatureDetail.fromJson(json['messageForward']),
        typingIndicator = FeatureDetail.fromJson(json['typingIndicator']),
        linkPreview = FeatureDetail.fromJson(json['linkPreview']);
}

class FeatureDetail {
  final bool enabled;
  final bool available;
  final String requiredTier;
  final int? maxFileSize;
  final int? maxDuration;
  final int? timeLimit;
  final List<String>? allowedTypes;
  final String? unavailableReason;

  FeatureDetail.fromJson(Map<String, dynamic> json)
      : enabled = json['enabled'] ?? false,
        available = json['available'] ?? false,
        requiredTier = json['requiredTier'] ?? 'None',
        maxFileSize = json['maxFileSize'],
        maxDuration = json['maxDuration'],
        timeLimit = json['timeLimit'],
        allowedTypes = json['allowedTypes'] != null
            ? List<String>.from(json['allowedTypes'])
            : null,
        unavailableReason = json['unavailableReason'];
}
```

---

## ⚠️ Error Handling

### Standard Error Response

All endpoints return errors in this format:

```json
{
  "success": false,
  "message": "Human-readable error message",
  "errors": [
    "Detailed error 1",
    "Detailed error 2"
  ]
}
```

### Common HTTP Status Codes

| Code | Meaning | Example |
|------|---------|---------|
| 200 | Success | Request completed successfully |
| 400 | Bad Request | Invalid request payload or parameters |
| 401 | Unauthorized | Missing or invalid authentication token |
| 403 | Forbidden | User doesn't have required tier or permission |
| 404 | Not Found | Message, user, or resource not found |
| 413 | Payload Too Large | File exceeds maximum size limit |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server-side error (contact support) |

### Error Handling Example

```dart
Future<MessageModel> sendMessage(SendMessageRequest request) async {
  try {
    final response = await http.post(
      Uri.parse('$baseUrl/api/sponsorship/messages/send'),
      headers: {
        'Authorization': 'Bearer $accessToken',
        'Content-Type': 'application/json',
      },
      body: jsonEncode(request.toJson()),
    );

    final data = jsonDecode(response.body);

    if (response.statusCode == 200 && data['success']) {
      return MessageModel.fromJson(data['data']);
    } else {
      // Handle error
      final errorMessage = data['message'] ?? 'Unknown error';
      final errors = data['errors'] as List<dynamic>? ?? [];

      throw ApiException(
        statusCode: response.statusCode,
        message: errorMessage,
        errors: errors.map((e) => e.toString()).toList(),
      );
    }
  } on SocketException {
    throw NetworkException('No internet connection');
  } catch (e) {
    throw Exception('Failed to send message: $e');
  }
}

// Usage with user-friendly error messages
try {
  await api.sendMessage(request);
} on ApiException catch (e) {
  if (e.statusCode == 403) {
    showUpgradeDialog('Upgrade to ${e.message}');
  } else if (e.statusCode == 413) {
    showError('File too large');
  } else {
    showError(e.message);
  }
} on NetworkException catch (e) {
  showError('Check your internet connection');
} catch (e) {
  showError('Something went wrong');
}
```

---

## 🧪 Testing Guide

### 1. Manual Testing with Postman

**Import Collection**:
- File: `ZiraAI_Chat_Enhancements_Postman_Collection.json`
- Import into Postman
- Set environment variables:
  - `baseUrl`: `https://ziraai-api-sit.up.railway.app`
  - `token`: Your JWT token (auto-filled after login)

**Test Sequence**:
```
1. Auth/Login with Phone → Saves token
2. Auth/Verify Phone OTP → Auto-extracts token
3. Avatar/Upload Avatar → Test multipart upload
4. Messaging/Get Features → Check tier permissions
5. Messaging/Send Message → Test basic message
6. Messaging/Get Conversation → Verify response structure
7. Messaging/Send Attachment → Test file upload
8. Messaging/Send Voice → Test audio upload
9. Messaging/Mark as Read → Test read receipt
10. Messaging/Edit Message → Test edit (within 1h)
11. Messaging/Delete Message → Test delete (within 24h)
```

---

### 2. Automated Testing (Flutter)

**Integration Test Example**:
```dart
void main() {
  group('Messaging API Tests', () {
    late ApiService api;
    late int testMessageId;

    setUpAll(() async {
      api = ApiService(baseUrl: 'https://ziraai-api-sit.up.railway.app');
      await api.login(phone: '+905551234567', otp: '123456');
    });

    test('Should send text message', () async {
      final message = await api.sendMessage(
        toUserId: 159,
        plantAnalysisId: 60,
        message: 'Test message',
      );

      expect(message.id, isNotNull);
      expect(message.message, equals('Test message'));
      expect(message.messageStatus, equals('sent'));
      expect(message.senderAvatarUrl, isNotNull);

      testMessageId = message.id;
    });

    test('Should mark message as read', () async {
      await api.markMessageAsRead(testMessageId);

      final messages = await api.getConversation(
        fromUserId: 159,
        toUserId: 165,
        plantAnalysisId: 60,
      );

      final readMessage = messages.firstWhere((m) => m.id == testMessageId);
      expect(readMessage.isRead, isTrue);
      expect(readMessage.messageStatus, equals('read'));
      expect(readMessage.readDate, isNotNull);
    });

    test('Should upload voice message (XL tier only)', () async {
      final audioFile = File('test_assets/voice.m4a');

      try {
        final message = await api.sendVoiceMessage(
          toUserId: 159,
          plantAnalysisId: 60,
          voiceFile: audioFile,
          duration: 45,
          waveform: '[0.1,0.5,0.8]',
        );

        expect(message.isVoiceMessage, isTrue);
        expect(message.voiceMessageUrl, isNotNull);
        expect(message.voiceMessageDuration, equals(45));
      } on ApiException catch (e) {
        if (e.statusCode == 403) {
          print('✓ Correctly rejected (user tier insufficient)');
        } else {
          rethrow;
        }
      }
    });

    test('Should fetch messaging features', () async {
      final features = await api.getMessagingFeatures();

      expect(features.imageAttachments.enabled, isTrue);
      expect(features.voiceMessages.requiredTier, equals('XL'));
      expect(features.messageDelete.timeLimit, equals(86400));
    });
  });
}
```

---

### 3. SignalR Testing

**Connection Test**:
```dart
void main() async {
  final signalR = SignalRService();

  // Connect
  await signalR.connect('your_access_token_here');

  // Test typing event
  signalR.onUserTyping = (userId, plantAnalysisId, isTyping) {
    print('✓ Typing event received: User $userId, typing: $isTyping');
  };

  await signalR.startTyping(159, 60);
  await Future.delayed(Duration(seconds: 2));
  await signalR.stopTyping(159, 60);

  // Test new message event
  signalR.onNewMessage = (messageId) {
    print('✓ New message event received: $messageId');
  };

  // Test read receipt event
  signalR.onMessageRead = (messageId, readByUserId, readAt) {
    print('✓ Read receipt received: Message $messageId read by $readByUserId');
  };

  // Keep connection alive
  await Future.delayed(Duration(minutes: 5));
}
```

---

## 📝 Summary

### What Changed from Previous Version

| Feature | Before | After |
|---------|--------|-------|
| **Avatar in Messages** | ❌ Not included | ✅ `senderAvatarUrl`, `senderAvatarThumbnailUrl` |
| **Message Status** | ❌ Only `isRead` | ✅ `messageStatus`, `deliveredDate`, `readDate` |
| **Attachments** | ❌ No metadata | ✅ Full metadata arrays (URLs, types, sizes, names) |
| **Voice Messages** | ❌ Not supported | ✅ Full support with URL, duration, waveform |
| **Edit/Delete** | ❌ No tracking | ✅ `isEdited`, `editedDate`, `isActive` flags |
| **Forward** | ❌ No tracking | ✅ `isForwarded`, `forwardedFromMessageId` |
| **SignalR Events** | ⚠️ Basic only | ✅ UserTyping, NewMessage, MessageRead |

### Mobile Team Action Items

1. ✅ **Update Data Models**: Use complete MessageModel with all new fields
2. ✅ **UI Components**: Add avatar, status indicators, attachment renderers
3. ✅ **SignalR Integration**: Implement typing indicators and read receipts
4. ✅ **Feature Flags**: Check user tier before showing premium features
5. ✅ **Testing**: Run integration tests with staging API
6. ✅ **Production**: Ready for production deployment

---

**Last Updated**: 2025-10-19
**Questions?**: Contact backend team or refer to `BACKEND_EKSIKLIKLER_VE_TALEPLER.md`
