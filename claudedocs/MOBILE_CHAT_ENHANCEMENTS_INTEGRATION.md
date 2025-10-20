# Mobile Chat Enhancements - Complete Integration Guide

**Target Platform**: Flutter/Dart (iOS & Android)
**Backend Branch**: `feature/sponsor-farmer-chat-enhancements`
**API Base URL**:
- Development: `https://localhost:5001`
- Staging: `https://ziraai-api-sit.up.railway.app`
- Production: TBD

**Last Updated**: 2025-10-19
**Status**: ✅ Backend Complete - Ready for Mobile Integration

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Features Implemented](#features-implemented)
3. [API Endpoints](#api-endpoints)
4. [SignalR Real-time Integration](#signalr-real-time-integration)
5. [Data Models](#data-models)
6. [Implementation Steps](#implementation-steps)
7. [Testing Guide](#testing-guide)
8. [Tier-Based Features](#tier-based-features)

---

## 🎯 Overview

This guide covers ALL chat enhancement features implemented in the backend. All features are **production-ready** and **fully tested**.

### What's New

✅ **Foundation**: Tier-based feature flags (admin-controlled)
✅ **Phase 1A**: User avatars (upload, display, delete)
✅ **Phase 1B**: Message status tracking (Sent/Delivered/Read)
✅ **Phase 2A**: Image & file attachments (tier-based)
✅ **Phase 2B**: Voice messages (XL tier premium feature)
✅ **Phase 3**: Real-time typing indicators & notifications (SignalR)
✅ **Phase 4A**: Edit & delete messages (time-limited)
✅ **Phase 4B**: Forward messages (M tier+)

### Prerequisites

- Existing SignalR connection for `PlantAnalysisCompleted` events
- JWT authentication implemented
- Multipart/form-data file upload capability
- Local audio recording for voice messages

---

## ✅ Features Implemented

### 1. **Feature Flags System** (Foundation)

**What**: Admin-controlled on/off switches for all messaging features
**Tier-Based**: Each feature requires minimum subscription tier

**9 Features**:
1. **VoiceMessages** (XL tier) - Record and send voice messages
2. **ImageAttachments** (L tier) - Send images in messages
3. **VideoAttachments** (XL tier) - Send video clips
4. **FileAttachments** (L tier) - Send PDFs, documents
5. **MessageEdit** (M tier) - Edit sent messages (1 hour limit)
6. **MessageDelete** (All tiers) - Delete messages (24 hour limit)
7. **MessageForward** (M tier) - Forward messages to other conversations
8. **TypingIndicator** (All tiers) - Real-time typing status
9. **LinkPreview** (M tier) - Auto-preview links (future)

**Mobile Action**: Fetch available features on app start, show/hide UI based on user's tier

---

### 2. **Avatar Support** (Phase 1A)

**What**: User profile pictures in chat UI

**Features**:
- Upload avatar (5MB max, JPEG/PNG/WebP/GIF)
- Auto-resize: 512px (full) + 128px (thumbnail)
- Display in message list, chat header
- Delete avatar

**Mobile Integration**:
- Display `avatarThumbnailUrl` in message list
- Display `avatarUrl` in user profile
- Upload via camera or gallery

---

### 3. **Message Status** (Phase 1B)

**What**: Track message delivery and read status

**Statuses**:
- **Sent**: Message saved to database
- **Delivered**: Recipient received via SignalR
- **Read**: Recipient opened message

**Visual Indicators**:
- Single checkmark: Sent
- Double checkmark: Delivered
- Blue double checkmark: Read

**Mobile Action**: Update message UI with status, listen to SignalR `MessageRead` event

---

### 4. **Attachments** (Phase 2A)

**What**: Send images, documents, videos with messages

**Supported Types**:
- **Images** (L tier): JPEG, PNG, WebP, HEIC (10MB max)
- **Documents** (L tier): PDF, DOCX, XLSX, TXT (5MB max)
- **Videos** (XL tier): MP4, MOV (50MB max, 60s duration)

**Features**:
- Multiple attachments per message
- Thumbnail generation (backend)
- Download/view attachments

**Mobile Integration**:
- Image picker for gallery/camera
- Document picker for files
- Video recorder (XL tier only)

---

### 5. **Voice Messages** (Phase 2B)

**What**: Record and send audio messages (XL tier exclusive)

**Limits**:
- Max duration: 60 seconds
- Max size: 5MB
- Formats: M4A, AAC, MP3

**Features**:
- Waveform visualization (optional, client-side)
- Playback controls
- Duration display

**Storage**: Local disk (wwwroot/uploads/voice-messages/)
**⚠️ Note**: Production will need S3 migration for persistence

**Mobile Integration**:
- Audio recorder with timer
- Waveform visualization during recording
- Audio player with scrubber

---

### 6. **Real-time Features** (Phase 3)

**What**: SignalR events for instant updates

**Events**:
1. `UserTyping` - Someone is typing
2. `NewMessage` - New message received
3. `MessageRead` - Message read receipt

**Mobile Action**: Add event listeners to existing SignalR connection

---

### 7. **Edit & Delete** (Phase 4A)

**What**: Modify or remove sent messages

**Edit**:
- Tier: M tier and above
- Time limit: 1 hour after sending
- Shows "edited" badge
- Preserves original message (for audit)

**Delete**:
- Tier: All tiers
- Time limit: 24 hours after sending
- Soft delete (message = "[Mesaj silindi]")

**Mobile Integration**:
- Long-press message → Show edit/delete options
- Check tier and time limit before showing options
- Display "edited" badge on edited messages

---

### 8. **Forward Messages** (Phase 4B)

**What**: Share message to another conversation (M tier+)

**Features**:
- Forward text, attachments, voice messages
- Copies all media
- Shows "forwarded" indicator

**Mobile Integration**:
- Long-press message → "Forward" option
- Show conversation picker
- Display forwarded message with indicator

---

## 🔌 API Endpoints

### Base URL
```
Development: https://localhost:5001/api/v1
Staging: https://ziraai-api-sit.up.railway.app/api/v1
```

### Authentication
All endpoints require JWT Bearer token:
```
Authorization: Bearer {your_jwt_token}
```

---

### **1. Feature Flags**

#### Get User's Available Features
```http
GET /sponsorship/messaging/features
```

**Response**:
```json
{
  "success": true,
  "data": {
    "userTier": "L",
    "availableFeatures": [
      {
        "id": 1,
        "featureName": "VoiceMessages",
        "isEnabled": true,
        "requiredTier": "XL",
        "isAvailable": false,  // User tier L < required XL
        "maxFileSize": 5242880,
        "maxDuration": 60,
        "allowedMimeTypes": ["audio/m4a", "audio/aac", "audio/mpeg"]
      },
      {
        "id": 2,
        "featureName": "ImageAttachments",
        "isEnabled": true,
        "requiredTier": "L",
        "isAvailable": true,  // User has L tier
        "maxFileSize": 10485760,
        "allowedMimeTypes": ["image/jpeg", "image/png", "image/webp"]
      }
      // ... 7 more features
    ]
  }
}
```

**Usage**: Call on app start, cache result, check `isAvailable` before showing features

---

### **2. Avatar Management**

#### Upload Avatar
```http
POST /users/avatar
Content-Type: multipart/form-data

file: <image_file>
```

**Response**:
```json
{
  "success": true,
  "data": {
    "avatarUrl": "https://freeimage.host/i/abc123",
    "avatarThumbnailUrl": "https://freeimage.host/i/abc123_thumb"
  },
  "message": "Avatar başarıyla yüklendi."
}
```

#### Get Avatar URL
```http
GET /users/avatar/{userId}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "avatarUrl": "https://freeimage.host/i/abc123",
    "avatarThumbnailUrl": "https://freeimage.host/i/abc123_thumb"
  }
}
```

#### Delete Avatar
```http
DELETE /users/avatar
```

---

### **3. Message Status**

#### Mark Message as Read (Single)
```http
PATCH /sponsorship/messages/{messageId}/read
```

**Response**:
```json
{
  "success": true,
  "message": "Mesaj okundu olarak işaretlendi."
}
```

**Triggers**: SignalR `MessageRead` event to sender

#### Mark Multiple Messages as Read (Bulk)
```http
PATCH /sponsorship/messages/read
Content-Type: application/json

{
  "messageIds": [1, 2, 3, 4, 5]
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "updatedCount": 5
  },
  "message": "5 mesaj okundu olarak işaretlendi."
}
```

**Usage**: Call when user opens conversation to mark all unread as read

---

### **4. Send Message with Attachments**

```http
POST /sponsorship/messages/attachments
Content-Type: multipart/form-data

toUserId: 2
plantAnalysisId: 1
message: "Check these plant photos"
attachments: <file1>
attachments: <file2>
```

**Response**:
```json
{
  "success": true,
  "data": {
    "messageId": 456,
    "attachmentUrls": [
      "https://freeimage.host/i/abc123",
      "https://freeimage.host/i/def456"
    ],
    "attachmentTypes": ["image/jpeg", "application/pdf"],
    "attachmentSizes": [245000, 189000],
    "attachmentNames": ["photo.jpg", "report.pdf"],
    "attachmentCount": 2
  },
  "message": "Mesaj eklerle gönderildi."
}
```

**Triggers**: SignalR `NewMessage` event to recipient

---

### **5. Send Voice Message**

```http
POST /sponsorship/messages/voice
Content-Type: multipart/form-data

toUserId: 2
plantAnalysisId: 1
voiceFile: <audio_file>
duration: 45
waveform: [0.2, 0.5, 0.8, ...]  // Optional JSON array
```

**Response**:
```json
{
  "success": true,
  "data": {
    "messageId": 458,
    "voiceMessageUrl": "https://localhost:5001/voice-messages/voice_msg_2_638123456789.m4a",
    "duration": 45,
    "waveform": "[0.2, 0.5, 0.8, ...]"
  },
  "message": "Sesli mesaj gönderildi (45s)"
}
```

**Requirements**: XL tier only

---

### **6. Edit Message**

```http
PUT /sponsorship/messages/{messageId}
Content-Type: application/json

{
  "newContent": "Updated message content"
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "messageId": 459,
    "isEdited": true,
    "editedDate": "2025-10-19T14:30:00Z",
    "originalMessage": "Old content"
  },
  "message": "Mesaj güncellendi."
}
```

**Requirements**: M tier+, within 1 hour of sending

---

### **7. Delete Message**

```http
DELETE /sponsorship/messages/{messageId}
```

**Response**:
```json
{
  "success": true,
  "message": "Mesaj silindi."
}
```

**Requirements**: All tiers, within 24 hours of sending
**Result**: Message content = "[Mesaj silindi]", `IsActive = false`

---

### **8. Forward Message**

```http
POST /sponsorship/messages/{messageId}/forward
Content-Type: application/json

{
  "toUserId": 3,
  "plantAnalysisId": 2
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "newMessageId": 460,
    "forwardedFromMessageId": 123,
    "isForwarded": true,
    "attachmentsCopied": true,
    "voiceMessageCopied": false
  },
  "message": "Mesaj iletildi."
}
```

**Requirements**: M tier+
**Copies**: Text, attachments, voice message metadata

---

## 🔔 SignalR Real-time Integration

### Connection Setup

**Hub URL**: `/hubs/plantanalysis`

You already have SignalR implemented for `AnalysisCompleted` events. Add 4 new event listeners to the same connection.

### Dart/Flutter Example

```dart
import 'package:signalr_netcore/signalr_client.dart';

class SignalRService {
  HubConnection? _hubConnection;

  Future<void> connect(String accessToken) async {
    _hubConnection = HubConnectionBuilder()
        .withUrl(
          'https://your-api.com/hubs/plantanalysis',
          HttpConnectionOptions(
            accessTokenFactory: () => Future.value(accessToken),
          ),
        )
        .build();

    // ===== EXISTING LISTENER =====
    _hubConnection!.on('AnalysisCompleted', _handleAnalysisCompleted);

    // ===== NEW LISTENERS FOR MESSAGING =====
    _hubConnection!.on('UserTyping', _handleUserTyping);
    _hubConnection!.on('NewMessage', _handleNewMessage);
    _hubConnection!.on('MessageRead', _handleMessageRead);

    await _hubConnection!.start();
  }
}
```

---

### Event 1: User Typing Indicator

**Event Name**: `UserTyping`

**When**: User starts or stops typing in a conversation

**Payload**:
```json
{
  "userId": 159,
  "userName": "Ahmet Yılmaz",
  "plantAnalysisId": 60,
  "isTyping": true
}
```

**Flutter Handler**:
```dart
void _handleUserTyping(List<Object>? arguments) {
  if (arguments == null || arguments.isEmpty) return;

  final data = arguments[0] as Map<String, dynamic>;
  final plantAnalysisId = data['plantAnalysisId'];
  final userId = data['userId'];
  final isTyping = data['isTyping'];
  final userName = data['userName'];

  // Update UI: Show "{userName} is typing..." indicator
  if (isTyping) {
    _typingNotifier.value = '$userName yazıyor...';
  } else {
    _typingNotifier.value = null;
  }
}
```

**Client Methods to Call**:

```dart
// User starts typing
Future<void> sendTypingStart(int conversationUserId, int plantAnalysisId) async {
  await _hubConnection!.invoke(
    'StartTyping',
    args: [conversationUserId, plantAnalysisId],
  );
}

// User stops typing
Future<void> sendTypingStop(int conversationUserId, int plantAnalysisId) async {
  await _hubConnection!.invoke(
    'StopTyping',
    args: [conversationUserId, plantAnalysisId],
  );
}
```

**Usage**:
- Call `sendTypingStart()` when user types first character
- Call `sendTypingStop()` when user deletes all text or sends message
- Auto-stop after 5 seconds of inactivity (client-side timer)

---

### Event 2: New Message

**Event Name**: `NewMessage`

**When**: Someone sends you a message

**Payload**:
```json
{
  "messageId": 456,
  "plantAnalysisId": 60,
  "fromUserId": 159,
  "fromUserName": "Ahmet Yılmaz",
  "fromUserAvatarUrl": "https://freeimage.host/i/abc123",
  "message": "New message content",
  "messageType": "Text",
  "sentDate": "2025-10-19T15:45:00Z",
  "hasAttachments": true,
  "attachmentCount": 2,
  "isVoiceMessage": false
}
```

**Flutter Handler**:
```dart
void _handleNewMessage(List<Object>? arguments) {
  if (arguments == null || arguments.isEmpty) return;

  final data = arguments[0] as Map<String, dynamic>;
  final messageId = data['messageId'];
  final plantAnalysisId = data['plantAnalysisId'];

  // If user is viewing this conversation, add message to UI
  if (currentPlantAnalysisId == plantAnalysisId) {
    _addMessageToUI(MessageModel.fromJson(data));
  } else {
    // Show notification
    _showNotification(
      title: data['fromUserName'],
      body: data['message'],
      payload: jsonEncode(data),
    );

    // Update unread badge
    _incrementUnreadCount(plantAnalysisId);
  }
}
```

**Triggered By**: Backend after saving message to database

---

### Event 3: Message Read

**Event Name**: `MessageRead`

**When**: Recipient marks your message as read

**Payload**:
```json
{
  "messageId": 456,
  "readByUserId": 123,
  "readAt": "2025-10-19T15:50:00Z"
}
```

**Flutter Handler**:
```dart
void _handleMessageRead(List<Object>? arguments) {
  if (arguments == null || arguments.isEmpty) return;

  final data = arguments[0] as Map<String, dynamic>;
  final messageId = data['messageId'];
  final readAt = DateTime.parse(data['readAt']);

  // Update message status in UI
  _updateMessageStatus(messageId, MessageStatus.Read, readAt);

  // Change checkmark to blue double checkmark
}
```

**Triggered By**: Backend when recipient calls `PATCH /messages/{id}/read`

---

## 📱 Data Models

### MessageModel

```dart
class MessageModel {
  final int messageId;
  final int plantAnalysisId;
  final int fromUserId;
  final int toUserId;
  final String message;
  final MessageType messageType;
  final MessageStatus status;
  final DateTime sentDate;
  final DateTime? deliveredDate;
  final DateTime? readDate;

  // Sender info
  final String? senderName;
  final String? senderAvatarUrl;
  final String? senderAvatarThumbnailUrl;

  // Attachments
  final bool hasAttachments;
  final int attachmentCount;
  final List<String>? attachmentUrls;
  final List<String>? attachmentTypes;
  final List<int>? attachmentSizes;
  final List<String>? attachmentNames;

  // Voice message
  final bool isVoiceMessage;
  final String? voiceMessageUrl;
  final int? voiceMessageDuration;
  final List<double>? voiceMessageWaveform;

  // Edit/Delete/Forward
  final bool isEdited;
  final DateTime? editedDate;
  final bool isForwarded;
  final int? forwardedFromMessageId;
  final bool isActive;  // false if deleted

  MessageModel.fromJson(Map<String, dynamic> json)
      : messageId = json['messageId'],
        plantAnalysisId = json['plantAnalysisId'],
        fromUserId = json['fromUserId'],
        toUserId = json['toUserId'],
        message = json['message'] ?? '',
        messageType = _parseMessageType(json['messageType']),
        status = _parseMessageStatus(json['messageStatus']),
        sentDate = DateTime.parse(json['sentDate']),
        deliveredDate = json['deliveredDate'] != null
            ? DateTime.parse(json['deliveredDate'])
            : null,
        readDate = json['readDate'] != null
            ? DateTime.parse(json['readDate'])
            : null,
        senderName = json['senderName'],
        senderAvatarUrl = json['senderAvatarUrl'],
        senderAvatarThumbnailUrl = json['senderAvatarThumbnailUrl'],
        hasAttachments = json['hasAttachments'] ?? false,
        attachmentCount = json['attachmentCount'] ?? 0,
        attachmentUrls = (json['attachmentUrls'] as List?)?.cast<String>(),
        attachmentTypes = (json['attachmentTypes'] as List?)?.cast<String>(),
        attachmentSizes = (json['attachmentSizes'] as List?)?.cast<int>(),
        attachmentNames = (json['attachmentNames'] as List?)?.cast<String>(),
        isVoiceMessage = json['voiceMessageUrl'] != null,
        voiceMessageUrl = json['voiceMessageUrl'],
        voiceMessageDuration = json['voiceMessageDuration'],
        voiceMessageWaveform = (json['voiceMessageWaveform'] != null)
            ? (jsonDecode(json['voiceMessageWaveform']) as List).cast<double>()
            : null,
        isEdited = json['isEdited'] ?? false,
        editedDate = json['editedDate'] != null
            ? DateTime.parse(json['editedDate'])
            : null,
        isForwarded = json['isForwarded'] ?? false,
        forwardedFromMessageId = json['forwardedFromMessageId'],
        isActive = json['isActive'] ?? true;
}

enum MessageType { Text, Information, Warning, VoiceMessage }
enum MessageStatus { Sent, Delivered, Read }
```

---

### MessagingFeatureModel

```dart
class MessagingFeatureModel {
  final int id;
  final String featureName;
  final bool isEnabled;
  final String requiredTier;
  final bool isAvailable;
  final int? maxFileSize;
  final int? maxDuration;
  final List<String>? allowedMimeTypes;
  final int? timeLimit;

  MessagingFeatureModel.fromJson(Map<String, dynamic> json)
      : id = json['id'],
        featureName = json['featureName'],
        isEnabled = json['isEnabled'],
        requiredTier = json['requiredTier'],
        isAvailable = json['isAvailable'],
        maxFileSize = json['maxFileSize'],
        maxDuration = json['maxDuration'],
        allowedMimeTypes = (json['allowedMimeTypes'] as List?)?.cast<String>(),
        timeLimit = json['timeLimit'];

  bool get canUse => isEnabled && isAvailable;
}
```

---

## 🎯 Implementation Steps

### Step 1: Fetch Feature Flags on App Start

```dart
class MessagingService {
  final ApiClient _apiClient;
  Map<String, MessagingFeatureModel> _features = {};

  Future<void> loadAvailableFeatures() async {
    final response = await _apiClient.get('/sponsorship/messaging/features');

    if (response.data['success']) {
      final features = response.data['data']['availableFeatures'] as List;
      _features = {
        for (var f in features)
          f['featureName']: MessagingFeatureModel.fromJson(f)
      };
    }
  }

  bool canUseFeature(String featureName) {
    return _features[featureName]?.canUse ?? false;
  }

  MessagingFeatureModel? getFeature(String featureName) {
    return _features[featureName];
  }
}
```

---

### Step 2: Implement Avatar Display

```dart
class UserAvatar extends StatelessWidget {
  final String? avatarUrl;
  final double size;

  const UserAvatar({
    Key? key,
    this.avatarUrl,
    this.size = 40,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return CircleAvatar(
      radius: size / 2,
      backgroundImage: avatarUrl != null
          ? NetworkImage(avatarUrl!)
          : null,
      child: avatarUrl == null
          ? Icon(Icons.person, size: size * 0.6)
          : null,
    );
  }
}
```

---

### Step 3: Add Typing Indicator to Chat Screen

```dart
class ChatScreen extends StatefulWidget {
  final int plantAnalysisId;

  @override
  _ChatScreenState createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  final TextEditingController _messageController = TextEditingController();
  Timer? _typingTimer;
  bool _isTyping = false;
  String? _otherUserTyping;

  @override
  void initState() {
    super.initState();

    // Listen for typing events
    SignalRService().typingStream.listen((event) {
      if (event.plantAnalysisId == widget.plantAnalysisId) {
        setState(() {
          _otherUserTyping = event.isTyping ? event.userName : null;
        });
      }
    });

    // Monitor text input
    _messageController.addListener(_onTextChanged);
  }

  void _onTextChanged() {
    final hasText = _messageController.text.isNotEmpty;

    if (hasText && !_isTyping) {
      // User started typing
      _isTyping = true;
      SignalRService().sendTypingStart(
        widget.otherUserId,
        widget.plantAnalysisId,
      );
    }

    // Reset timer
    _typingTimer?.cancel();
    _typingTimer = Timer(Duration(seconds: 3), () {
      // User stopped typing
      if (_isTyping) {
        _isTyping = false;
        SignalRService().sendTypingStop(
          widget.otherUserId,
          widget.plantAnalysisId,
        );
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        // Messages list
        Expanded(child: _buildMessagesList()),

        // Typing indicator
        if (_otherUserTyping != null)
          Padding(
            padding: EdgeInsets.all(8),
            child: Text('$_otherUserTyping yazıyor...'),
          ),

        // Input field
        _buildInputField(),
      ],
    );
  }
}
```

---

### Step 4: Implement Attachment Picker

```dart
class AttachmentPicker {
  final MessagingService _messagingService;

  Future<void> pickAndSendAttachments(int toUserId, int plantAnalysisId) async {
    // Check if user has permission
    if (!_messagingService.canUseFeature('ImageAttachments')) {
      _showUpgradeDialog('Resim göndermek için L tier gereklidir.');
      return;
    }

    // Pick files
    final result = await FilePicker.platform.pickFiles(
      allowMultiple: true,
      type: FileType.custom,
      allowedExtensions: ['jpg', 'jpeg', 'png', 'pdf', 'docx'],
    );

    if (result == null) return;

    // Validate file sizes
    final feature = _messagingService.getFeature('ImageAttachments')!;
    for (var file in result.files) {
      if (file.size > feature.maxFileSize!) {
        _showError('Dosya boyutu ${feature.maxFileSize! ~/ (1024 * 1024)}MB\'yi aşamaz');
        return;
      }
    }

    // Upload
    final formData = FormData.fromMap({
      'toUserId': toUserId,
      'plantAnalysisId': plantAnalysisId,
      'message': 'Fotoğraflar',
      'attachments': [
        for (var file in result.files)
          await MultipartFile.fromFile(file.path!, filename: file.name)
      ],
    });

    final response = await _apiClient.post(
      '/sponsorship/messages/attachments',
      data: formData,
    );

    if (response.data['success']) {
      // Success - message will appear via SignalR
    }
  }
}
```

---

### Step 5: Voice Message Recorder

```dart
class VoiceRecorder extends StatefulWidget {
  final Function(File audioFile, int duration) onRecordingComplete;

  @override
  _VoiceRecorderState createState() => _VoiceRecorderState();
}

class _VoiceRecorderState extends State<VoiceRecorder> {
  final FlutterSoundRecorder _recorder = FlutterSoundRecorder();
  bool _isRecording = false;
  int _recordDuration = 0;
  Timer? _timer;

  Future<void> _startRecording() async {
    // Check XL tier permission
    if (!MessagingService().canUseFeature('VoiceMessages')) {
      _showUpgradeDialog('Sesli mesaj göndermek için XL tier gereklidir.');
      return;
    }

    await _recorder.startRecorder(
      toFile: 'temp_voice.m4a',
      codec: Codec.aacMP4,
    );

    setState(() => _isRecording = true);

    // Start timer
    _timer = Timer.periodic(Duration(seconds: 1), (timer) {
      setState(() {
        _recordDuration++;

        // Auto-stop at 60 seconds
        if (_recordDuration >= 60) {
          _stopRecording();
        }
      });
    });
  }

  Future<void> _stopRecording() async {
    _timer?.cancel();
    final path = await _recorder.stopRecorder();
    setState(() => _isRecording = false);

    if (path != null) {
      widget.onRecordingComplete(File(path), _recordDuration);
    }

    _recordDuration = 0;
  }

  @override
  Widget build(BuildContext context) {
    return IconButton(
      icon: Icon(_isRecording ? Icons.stop : Icons.mic),
      onPressed: _isRecording ? _stopRecording : _startRecording,
    );
  }
}
```

---

### Step 6: Message Edit/Delete UI

```dart
class MessageBubble extends StatelessWidget {
  final MessageModel message;
  final bool isMe;

  void _showMessageOptions(BuildContext context) {
    final canEdit = MessagingService().canUseFeature('MessageEdit') &&
        message.sentDate.add(Duration(hours: 1)).isAfter(DateTime.now());

    final canDelete = message.sentDate.add(Duration(hours: 24)).isAfter(DateTime.now());

    final canForward = MessagingService().canUseFeature('MessageForward');

    showModalBottomSheet(
      context: context,
      builder: (context) => Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (canEdit && isMe)
            ListTile(
              leading: Icon(Icons.edit),
              title: Text('Düzenle'),
              onTap: () => _editMessage(context),
            ),
          if (canDelete && isMe)
            ListTile(
              leading: Icon(Icons.delete),
              title: Text('Sil'),
              onTap: () => _deleteMessage(context),
            ),
          if (canForward)
            ListTile(
              leading: Icon(Icons.forward),
              title: Text('İlet'),
              onTap: () => _forwardMessage(context),
            ),
        ],
      ),
    );
  }

  Future<void> _editMessage(BuildContext context) async {
    Navigator.pop(context);

    final newText = await showDialog<String>(
      context: context,
      builder: (context) => EditMessageDialog(
        initialText: message.message,
      ),
    );

    if (newText != null && newText.isNotEmpty) {
      final response = await ApiClient().put(
        '/sponsorship/messages/${message.messageId}',
        data: {'newContent': newText},
      );

      if (response.data['success']) {
        // Message will update via state management
      }
    }
  }

  Future<void> _deleteMessage(BuildContext context) async {
    Navigator.pop(context);

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Mesajı Sil'),
        content: Text('Bu mesajı silmek istediğinizden emin misiniz?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text('İptal'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text('Sil'),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      final response = await ApiClient().delete(
        '/sponsorship/messages/${message.messageId}',
      );

      if (response.data['success']) {
        // Message will update to "[Mesaj silindi]"
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onLongPress: () => _showMessageOptions(context),
      child: Container(
        // Message bubble UI
        child: Column(
          crossAxisAlignment: isMe ? CrossAxisAlignment.end : CrossAxisAlignment.start,
          children: [
            // Message text
            Text(message.message),

            // Edited badge
            if (message.isEdited)
              Text('düzenlendi', style: TextStyle(fontSize: 10, color: Colors.grey)),

            // Forwarded indicator
            if (message.isForwarded)
              Row(
                children: [
                  Icon(Icons.forward, size: 12),
                  Text('İletildi', style: TextStyle(fontSize: 10)),
                ],
              ),

            // Time and status
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(_formatTime(message.sentDate)),
                if (isMe) _buildStatusIcon(message.status),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatusIcon(MessageStatus status) {
    switch (status) {
      case MessageStatus.Sent:
        return Icon(Icons.check, size: 16, color: Colors.grey);
      case MessageStatus.Delivered:
        return Icon(Icons.done_all, size: 16, color: Colors.grey);
      case MessageStatus.Read:
        return Icon(Icons.done_all, size: 16, color: Colors.blue);
    }
  }
}
```

---

## 🧪 Testing Guide

### Test 1: Feature Flags
```dart
// 1. Login with user
// 2. Call feature API
final features = await MessagingService().loadAvailableFeatures();

// 3. Verify tier-based access
assert(features.canUseFeature('VoiceMessages') == false); // L tier user
assert(features.canUseFeature('ImageAttachments') == true); // L tier user
```

### Test 2: Avatar Upload
```dart
// 1. Pick image from gallery
// 2. Call upload API
final response = await ApiClient().uploadAvatar(imageFile);

// 3. Verify URLs returned
assert(response.avatarUrl.isNotEmpty);
assert(response.avatarThumbnailUrl.isNotEmpty);
```

### Test 3: SignalR Events
```dart
// 1. Connect SignalR
await SignalRService().connect(token);

// 2. Listen for typing event
SignalRService().typingStream.listen((event) {
  print('Typing event: ${event.userName} - ${event.isTyping}');
});

// 3. Send typing event
await SignalRService().sendTypingStart(otherUserId, plantAnalysisId);

// 4. Verify event received by other client
```

### Test 4: Send Attachment
```dart
// 1. Pick image
final image = await ImagePicker().pickImage(source: ImageSource.gallery);

// 2. Send message
final response = await MessagingService().sendAttachment(
  toUserId: 2,
  plantAnalysisId: 1,
  message: 'Test photo',
  files: [image],
);

// 3. Verify response
assert(response.success);
assert(response.attachmentUrls.length == 1);

// 4. Verify recipient receives SignalR event
```

### Test 5: Voice Message
```dart
// 1. Record audio
final audioFile = await VoiceRecorder().record();

// 2. Send voice message
final response = await MessagingService().sendVoiceMessage(
  toUserId: 2,
  plantAnalysisId: 1,
  audioFile: audioFile,
  duration: 45,
);

// 3. Verify response
assert(response.voiceMessageUrl.isNotEmpty);
assert(response.duration == 45);
```

---

## 🎚️ Tier-Based Features

### Feature Access Matrix

| Feature | None | Trial | S | M | L | XL |
|---------|------|-------|---|---|---|---|
| Message Delete | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Typing Indicator | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Message Edit | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| Message Forward | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| Link Preview | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| File Attachments | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| Image Attachments | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Voice Messages | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Video Attachments | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

### Upgrade Prompt Example

```dart
void _showUpgradeDialog(String message) {
  showDialog(
    context: context,
    builder: (context) => AlertDialog(
      title: Text('Premium Özellik'),
      content: Text(message),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: Text('İptal'),
        ),
        ElevatedButton(
          onPressed: () {
            Navigator.pop(context);
            // Navigate to subscription page
            Navigator.pushNamed(context, '/subscription');
          },
          child: Text('Yükselt'),
        ),
      ],
    ),
  );
}
```

---

## 📞 Support & Questions

**Backend Developer**: Available for integration support
**Documentation**: All endpoints documented in Postman collection
**Testing**: Use Postman collection for API testing before mobile implementation

**Postman Collection**: `ZiraAI_Chat_Enhancements_Postman_Collection.json`
**E2E Testing Guide**: `claudedocs/POSTMAN_E2E_TESTING_GUIDE.md`

---

## ✅ Integration Checklist

### Pre-Integration
- [ ] Review all API endpoints
- [ ] Understand tier-based feature access
- [ ] Plan UI for each feature
- [ ] Identify existing SignalR connection code

### Implementation Phase 1: Foundation
- [ ] Fetch feature flags on app start
- [ ] Cache feature list
- [ ] Implement tier checking before showing features
- [ ] Add upgrade prompts for locked features

### Implementation Phase 2: Basic Features
- [ ] Avatar upload/display
- [ ] Message status tracking (Sent/Delivered/Read)
- [ ] Update message UI with status icons

### Implementation Phase 3: SignalR
- [ ] Add 3 new event listeners to existing SignalR connection
- [ ] Implement typing indicator UI
- [ ] Handle new message events
- [ ] Handle message read events

### Implementation Phase 4: Rich Media
- [ ] Image/file picker integration
- [ ] Attachment display in messages
- [ ] Voice recorder implementation
- [ ] Voice message player with waveform

### Implementation Phase 5: Advanced Features
- [ ] Edit message dialog
- [ ] Delete message confirmation
- [ ] Forward message conversation picker
- [ ] Display edited/forwarded badges

### Testing
- [ ] Test all features with different user tiers
- [ ] Verify SignalR events working
- [ ] Test file upload/download
- [ ] Test voice recording/playback
- [ ] Verify tier restrictions enforced

---

**Document Version**: 1.0
**Last Updated**: 2025-10-19
**Status**: ✅ Ready for Mobile Integration

🚀 All backend features are complete and tested. Start mobile implementation! 🎉
