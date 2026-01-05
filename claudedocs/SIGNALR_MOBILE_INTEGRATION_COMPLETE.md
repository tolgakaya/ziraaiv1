# SignalR Mobile Integration - Complete Reference Guide

**Version**: 1.0
**Last Updated**: 2026-01-04
**Target Platform**: Flutter Mobile Application
**Backend**: ASP.NET Core SignalR

## Table of Contents

1. [Overview](#overview)
2. [Connection Setup](#connection-setup)
3. [Hub Endpoints](#hub-endpoints)
4. [Authentication](#authentication)
5. [Event Reference](#event-reference)
   - [Dealer Invitations](#dealer-invitations)
   - [Farmer Invitations](#farmer-invitations)
   - [Plant Analysis](#plant-analysis)
   - [Messaging System](#messaging-system)
   - [Bulk Operations](#bulk-operations)
6. [Group Management](#group-management)
7. [Error Handling](#error-handling)
8. [Testing Guide](#testing-guide)
9. [Best Practices](#best-practices)

---

## Overview

ZiraAI uses SignalR for real-time bidirectional communication between the backend and mobile applications. This enables instant notifications for:

- **Dealer & Farmer Invitations**: Real-time invitation notifications
- **Plant Analysis**: Completion, failure, and progress updates
- **Messaging**: Chat messages, typing indicators, read receipts
- **Bulk Operations**: Progress and completion updates for bulk invitations and code distribution

### SignalR Hubs

| Hub Name | Endpoint | Purpose |
|----------|----------|---------|
| `NotificationHub` | `/hubs/notification` | Invitations and general notifications |
| `PlantAnalysisHub` | `/hubs/plantanalysis` | Plant analysis updates and messaging |

---

## Connection Setup

### 1. Add SignalR Package

```yaml
# pubspec.yaml
dependencies:
  signalr_netcore: ^1.3.5
```

### 2. Connection Configuration

```dart
import 'package:signalr_netcore/signalr_client.dart';

class SignalRService {
  HubConnection? _notificationHub;
  HubConnection? _plantAnalysisHub;
  final String _baseUrl = 'https://api.ziraai.com'; // Production URL

  // Connection state
  bool _isConnected = false;
  bool get isConnected => _isConnected;

  // Authentication token
  String? _accessToken;

  Future<void> initialize(String accessToken) async {
    _accessToken = accessToken;

    // Initialize both hubs
    await _initializeNotificationHub();
    await _initializePlantAnalysisHub();
  }

  Future<void> _initializeNotificationHub() async {
    final httpOptions = HttpConnectionOptions(
      accessTokenFactory: () async => _accessToken,
      transport: HttpTransportType.WebSockets,
      logging: (level, message) => print('[NotificationHub] $message'),
    );

    _notificationHub = HubConnectionBuilder()
        .withUrl('$_baseUrl/hubs/notification', options: httpOptions)
        .withAutomaticReconnect(retryDelays: [0, 2000, 10000, 30000])
        .build();

    // Setup event handlers for NotificationHub
    _setupNotificationHandlers();

    // Connect
    await _notificationHub?.start();
    _isConnected = true;
    print('✅ NotificationHub connected');
  }

  Future<void> _initializePlantAnalysisHub() async {
    final httpOptions = HttpConnectionOptions(
      accessTokenFactory: () async => _accessToken,
      transport: HttpTransportType.WebSockets,
      logging: (level, message) => print('[PlantAnalysisHub] $message'),
    );

    _plantAnalysisHub = HubConnectionBuilder()
        .withUrl('$_baseUrl/hubs/plantanalysis', options: httpOptions)
        .withAutomaticReconnect(retryDelays: [0, 2000, 10000, 30000])
        .build();

    // Setup event handlers for PlantAnalysisHub
    _setupPlantAnalysisHandlers();

    // Connect
    await _plantAnalysisHub?.start();
    print('✅ PlantAnalysisHub connected');
  }

  void _setupNotificationHandlers() {
    // Register event listeners (see Event Reference section)
  }

  void _setupPlantAnalysisHandlers() {
    // Register event listeners (see Event Reference section)
  }

  Future<void> disconnect() async {
    await _notificationHub?.stop();
    await _plantAnalysisHub?.stop();
    _isConnected = false;
    print('❌ SignalR disconnected');
  }
}
```

---

## Hub Endpoints

### Production URLs
```
NotificationHub: https://api.ziraai.com/hubs/notification
PlantAnalysisHub: https://api.ziraai.com/hubs/plantanalysis
```

### Development URLs
```
NotificationHub: https://localhost:5001/hubs/notification
PlantAnalysisHub: https://localhost:5001/hubs/plantanalysis
```

### Staging URLs
```
NotificationHub: https://ziraai-api-sit.up.railway.app/hubs/notification
PlantAnalysisHub: https://ziraai-api-sit.up.railway.app/hubs/plantanalysis
```

---

## Authentication

### JWT Bearer Token

SignalR connections require JWT authentication. The token must be included in the connection setup.

```dart
// Get access token from your auth service
String accessToken = await authService.getAccessToken();

// Initialize SignalR with token
await signalRService.initialize(accessToken);
```

### Token Refresh

When the access token expires (60 minutes), you must:
1. Disconnect from SignalR
2. Refresh the token using refresh token endpoint
3. Reconnect with new token

```dart
Future<void> refreshConnection() async {
  // Disconnect
  await signalRService.disconnect();

  // Get new token
  String newToken = await authService.refreshToken();

  // Reconnect
  await signalRService.initialize(newToken);
}
```

### User Claims Required

The JWT token must include these claims:
- `userId` (NameIdentifier) - User ID
- `email` - User email address
- `phone` (MobilePhone) - User phone number (for farmer invitations)

---

## Event Reference

### Dealer Invitations

#### Event: `NewDealerInvitation`

**Hub**: NotificationHub
**Trigger**: When a sponsor creates a dealer invitation
**Target**: Email and phone groups

**Payload**:
```json
{
  "invitationId": 123,
  "token": "abc123def456",
  "sponsorName": "TOLGA TARIM",
  "codeCount": 50,
  "packageTier": "M",
  "expiresAt": "2026-01-11T10:30:00Z",
  "remainingDays": 7,
  "status": "Pending",
  "dealerEmail": "dealer@example.com",
  "dealerPhone": "+905551234567",
  "createdAt": "2026-01-04T10:30:00Z"
}
```

**Flutter Handler**:
```dart
void _setupNotificationHandlers() {
  _notificationHub?.on('NewDealerInvitation', (arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;

    // Parse payload
    final invitation = DealerInvitation(
      id: data['invitationId'],
      token: data['token'],
      sponsorName: data['sponsorName'],
      codeCount: data['codeCount'],
      packageTier: data['packageTier'],
      expiresAt: DateTime.parse(data['expiresAt']),
      remainingDays: data['remainingDays'],
      status: data['status'],
      dealerEmail: data['dealerEmail'],
      dealerPhone: data['dealerPhone'],
      createdAt: DateTime.parse(data['createdAt']),
    );

    // Show notification
    _showInvitationNotification(invitation);

    // Update UI
    _dealerInvitationController.add(invitation);

    print('📨 Received dealer invitation: ${invitation.id}');
  });
}
```

**When to Show**:
- User is logged in as a dealer
- User's email or phone matches the invitation
- Invitation is in "Pending" status

**User Action**:
- Navigate to "Dealer Invitations" screen
- Show accept/reject buttons
- Display invitation details

---

### Farmer Invitations

#### Event: `NewFarmerInvitation`

**Hub**: NotificationHub
**Trigger**: When a sponsor creates a farmer invitation
**Target**: Phone groups (farmer invitations are phone-based)

**Payload**:
```json
{
  "invitationId": 456,
  "token": "xyz789abc123",
  "sponsorName": "TOLGA TARIM",
  "codeCount": 1,
  "packageTier": "S",
  "expiresAt": "2026-01-11T10:30:00Z",
  "remainingDays": 7,
  "status": "Pending",
  "farmerPhone": "+905551234567",
  "createdAt": "2026-01-04T10:30:00Z"
}
```

**Flutter Handler**:
```dart
void _setupNotificationHandlers() {
  _notificationHub?.on('NewFarmerInvitation', (arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;

    // Parse payload
    final invitation = FarmerInvitation(
      id: data['invitationId'],
      token: data['token'],
      sponsorName: data['sponsorName'],
      codeCount: data['codeCount'],
      packageTier: data['packageTier'],
      expiresAt: DateTime.parse(data['expiresAt']),
      remainingDays: data['remainingDays'],
      status: data['status'],
      farmerPhone: data['farmerPhone'],
      createdAt: DateTime.parse(data['createdAt']),
    );

    // Show notification
    _showFarmerInvitationNotification(invitation);

    // Update UI
    _farmerInvitationController.add(invitation);

    print('📨 Received farmer invitation: ${invitation.id}');
  });
}
```

**When to Show**:
- User is logged in as a farmer
- User's phone matches the invitation phone (after normalization)
- Invitation is in "Pending" status

**User Action**:
- Navigate to "Farmer Invitations" screen
- Show accept button
- Display sponsor information and code count

---

### Plant Analysis

#### Event: `AnalysisCompleted`

**Hub**: PlantAnalysisHub
**Trigger**: When async plant analysis completes successfully
**Target**: User ID (specific user)

**Payload**:
```json
{
  "analysisId": 789,
  "status": "Completed",
  "plantName": "Domates",
  "disease": "Erken Yanıklık",
  "confidence": 0.92,
  "recommendations": "Fungisit uygulayın...",
  "imageUrl": "https://cdn.ziraai.com/images/analysis_789.jpg",
  "completedAt": "2026-01-04T10:35:00Z",
  "processingTime": 15.3
}
```

**Flutter Handler**:
```dart
void _setupPlantAnalysisHandlers() {
  _plantAnalysisHub?.on('AnalysisCompleted', (arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;

    // Parse payload
    final analysis = PlantAnalysisResult(
      id: data['analysisId'],
      status: data['status'],
      plantName: data['plantName'],
      disease: data['disease'],
      confidence: data['confidence'],
      recommendations: data['recommendations'],
      imageUrl: data['imageUrl'],
      completedAt: DateTime.parse(data['completedAt']),
      processingTime: data['processingTime'],
    );

    // Show success notification
    _showAnalysisCompletedNotification(analysis);

    // Navigate to results screen
    navigationService.navigateTo('/analysis-results', arguments: analysis);

    // Update analysis list
    _analysisResultController.add(analysis);

    print('✅ Analysis completed: ${analysis.id}');
  });
}
```

**When to Show**:
- User submitted an async analysis request
- Analysis processing completed successfully
- User is still in the app or receives push notification

**User Action**:
- Show push notification if app is in background
- Auto-navigate to results screen if app is in foreground
- Update "My Analyses" list with completed status

---

#### Event: `AnalysisFailed`

**Hub**: PlantAnalysisHub
**Trigger**: When async plant analysis fails
**Target**: User ID (specific user)

**Payload**:
```json
{
  "analysisId": 790,
  "status": "Failed",
  "error": "Image quality too low",
  "errorCode": "LOW_QUALITY_IMAGE",
  "failedAt": "2026-01-04T10:35:00Z",
  "retryable": true,
  "supportMessage": "Lütfen daha net bir fotoğraf çekin"
}
```

**Flutter Handler**:
```dart
void _setupPlantAnalysisHandlers() {
  _plantAnalysisHub?.on('AnalysisFailed', (arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;

    // Parse payload
    final failure = AnalysisFailure(
      id: data['analysisId'],
      status: data['status'],
      error: data['error'],
      errorCode: data['errorCode'],
      failedAt: DateTime.parse(data['failedAt']),
      retryable: data['retryable'],
      supportMessage: data['supportMessage'],
    );

    // Show error notification
    _showAnalysisFailedNotification(failure);

    // Update UI with error state
    _analysisFailureController.add(failure);

    print('❌ Analysis failed: ${failure.id} - ${failure.error}');
  });
}
```

**When to Show**:
- Analysis processing encountered an error
- User should be informed and potentially retry

**User Action**:
- Show error message with explanation
- Offer "Retry" button if `retryable: true`
- Update "My Analyses" list with failed status

---

#### Event: `AnalysisProgress`

**Hub**: PlantAnalysisHub
**Trigger**: During long-running analysis operations (optional)
**Target**: User ID (specific user)

**Payload**:
```json
{
  "analysisId": 791,
  "status": "Processing",
  "currentStep": "Analyzing plant structure",
  "progress": 45,
  "estimatedTimeRemaining": 10
}
```

**Flutter Handler**:
```dart
void _setupPlantAnalysisHandlers() {
  _plantAnalysisHub?.on('AnalysisProgress', (arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;

    // Parse payload
    final progress = AnalysisProgress(
      id: data['analysisId'],
      status: data['status'],
      currentStep: data['currentStep'],
      progress: data['progress'],
      estimatedTimeRemaining: data['estimatedTimeRemaining'],
    );

    // Update progress UI
    _analysisProgressController.add(progress);

    print('🔄 Analysis progress: ${progress.progress}% - ${progress.currentStep}');
  });
}
```

**When to Show**:
- During async analysis processing
- To provide visual feedback to users

**User Action**:
- Update progress bar
- Show current processing step
- Display estimated time remaining

---

### Messaging System

#### Event: `NewMessage`

**Hub**: PlantAnalysisHub
**Trigger**: When a new message is sent in a conversation
**Target**: User ID (message recipient)

**Payload**:
```json
{
  "messageId": 123,
  "plantAnalysisId": 789,
  "fromUserId": 456,
  "fromUserName": "Ahmet Yılmaz",
  "fromUserCompany": "TOLGA TARIM",
  "senderRole": "Sponsor",
  "senderAvatarUrl": "https://cdn.ziraai.com/avatars/456.jpg",
  "senderAvatarThumbnailUrl": "https://cdn.ziraai.com/avatars/456_thumb.jpg",
  "message": "Merhaba, bitkinizle ilgili sorularınızı yanıtlayabilirim",
  "messageType": "Text",
  "sentDate": "2026-01-04T10:40:00Z",
  "isApproved": true,
  "requiresApproval": false,
  "hasAttachments": true,
  "attachmentCount": 2,
  "attachmentUrls": ["https://cdn.ziraai.com/attachments/1.jpg", "https://cdn.ziraai.com/attachments/2.jpg"],
  "attachmentThumbnails": ["https://cdn.ziraai.com/attachments/1_thumb.jpg", "https://cdn.ziraai.com/attachments/2_thumb.jpg"],
  "isVoiceMessage": false,
  "voiceMessageUrl": null,
  "voiceMessageDuration": null,
  "voiceMessageWaveform": null
}
```

**Flutter Handler**:
```dart
void _setupPlantAnalysisHandlers() {
  _plantAnalysisHub?.on('NewMessage', (arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;

    // Parse complete payload (no need for additional API call)
    final message = ChatMessage(
      id: data['messageId'],
      plantAnalysisId: data['plantAnalysisId'],
      fromUserId: data['fromUserId'],
      fromUserName: data['fromUserName'],
      fromUserCompany: data['fromUserCompany'],
      senderRole: data['senderRole'],
      senderAvatarUrl: data['senderAvatarUrl'],
      senderAvatarThumbnailUrl: data['senderAvatarThumbnailUrl'],
      message: data['message'],
      messageType: data['messageType'],
      sentDate: DateTime.parse(data['sentDate']),
      isApproved: data['isApproved'],
      requiresApproval: data['requiresApproval'],
      hasAttachments: data['hasAttachments'],
      attachmentCount: data['attachmentCount'],
      attachmentUrls: data['attachmentUrls'] != null
          ? List<String>.from(data['attachmentUrls'])
          : null,
      attachmentThumbnails: data['attachmentThumbnails'] != null
          ? List<String>.from(data['attachmentThumbnails'])
          : null,
      isVoiceMessage: data['isVoiceMessage'],
      voiceMessageUrl: data['voiceMessageUrl'],
      voiceMessageDuration: data['voiceMessageDuration'],
      voiceMessageWaveform: data['voiceMessageWaveform'],
    );

    // Add message to chat UI immediately
    _chatMessagesController.add(message);

    // Play notification sound if not in chat
    if (!_isInChatScreen(message.plantAnalysisId)) {
      _playMessageSound();
      _showMessageNotification(message);
    }

    print('💬 New message: ${message.id} from ${message.fromUserName}');
  });
}
```

**When to Show**:
- User receives a new message in a conversation
- Either from farmer ↔ sponsor or farmer ↔ AI chat

**User Action**:
- Show notification if user is not in the chat screen
- Update chat UI with new message
- Play notification sound
- Update unread message count

---

#### Event: `UserTyping`

**Hub**: PlantAnalysisHub
**Trigger**: When another user starts/stops typing
**Target**: User ID (conversation participant)

**Payload**:
```json
{
  "userId": "456",
  "plantAnalysisId": 789,
  "isTyping": true,
  "timestamp": "2026-01-04T10:40:15Z"
}
```

**Flutter Handler**:
```dart
void _setupPlantAnalysisHandlers() {
  _plantAnalysisHub?.on('UserTyping', (arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;

    final userId = data['userId'];
    final plantAnalysisId = data['plantAnalysisId'];
    final isTyping = data['isTyping'];
    final timestamp = DateTime.parse(data['timestamp']);

    // Update typing indicator in chat UI
    if (_currentChatAnalysisId == plantAnalysisId) {
      _typingIndicatorController.add(isTyping);
    }

    print('⌨️ User $userId typing: $isTyping');
  });
}
```

**When to Show**:
- User is in a chat screen
- Other participant starts or stops typing

**User Action**:
- Show "Typing..." indicator in chat UI
- Hide indicator when `isTyping: false`

**Client Methods to Call**:
```dart
// Start typing
await _plantAnalysisHub.invoke('StartTyping',
  args: [conversationUserId, plantAnalysisId]);

// Stop typing
await _plantAnalysisHub.invoke('StopTyping',
  args: [conversationUserId, plantAnalysisId]);
```

---

#### Event: `MessageRead`

**Hub**: PlantAnalysisHub
**Trigger**: When a message is marked as read
**Target**: User ID (message sender)

**Payload**:
```json
{
  "messageId": 123,
  "readByUserId": "789",
  "readAt": "2026-01-04T10:41:00Z"
}
```

**Flutter Handler**:
```dart
void _setupPlantAnalysisHandlers() {
  _plantAnalysisHub?.on('MessageRead', (arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;

    final messageId = data['messageId'];
    final readByUserId = data['readByUserId'];
    final readAt = DateTime.parse(data['readAt']);

    // Update message read status in chat UI
    _updateMessageReadStatus(messageId, readAt);

    print('✓✓ Message $messageId read by user $readByUserId');
  });
}
```

**When to Show**:
- Message sender is in the chat screen
- Recipient has read their message

**User Action**:
- Show double checkmark (✓✓) next to sent message
- Update message status in chat list

**Client Method to Call**:
```dart
// Mark message as read
await _plantAnalysisHub.invoke('NotifyMessageRead',
  args: [senderUserId, messageId]);
```

---

### Bulk Operations

#### Event: `BulkInvitationProgress`

**Hub**: NotificationHub
**Trigger**: During bulk dealer invitation creation
**Target**: Sponsor group (sponsor_{sponsorId})

**Payload**:
```json
{
  "sponsorId": 123,
  "bulkJobId": 456,
  "totalCount": 100,
  "processedCount": 45,
  "successCount": 43,
  "failedCount": 2,
  "progressPercentage": 45,
  "currentStatus": "Processing",
  "estimatedTimeRemaining": 30
}
```

**Flutter Handler**:
```dart
void _setupNotificationHandlers() {
  _notificationHub?.on('BulkInvitationProgress', (arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;

    // Parse payload
    final progress = BulkInvitationProgress(
      sponsorId: data['sponsorId'],
      bulkJobId: data['bulkJobId'],
      totalCount: data['totalCount'],
      processedCount: data['processedCount'],
      successCount: data['successCount'],
      failedCount: data['failedCount'],
      progressPercentage: data['progressPercentage'],
      currentStatus: data['currentStatus'],
      estimatedTimeRemaining: data['estimatedTimeRemaining'],
    );

    // Update progress UI
    _bulkInvitationProgressController.add(progress);

    print('📊 Bulk invitation progress: ${progress.progressPercentage}%');
  });
}
```

**When to Show**:
- Sponsor initiated bulk dealer invitation creation
- Real-time progress updates during processing

**User Action**:
- Show progress bar with percentage
- Display success/failed counts
- Show estimated time remaining

---

#### Event: `BulkInvitationCompleted`

**Hub**: NotificationHub
**Trigger**: When bulk dealer invitation creation completes
**Target**: Sponsor group (sponsor_{sponsorId})

**Payload**:
```json
{
  "bulkJobId": 456,
  "status": "Completed",
  "successCount": 98,
  "failedCount": 2,
  "completedAt": "2026-01-04T10:45:00Z"
}
```

**Flutter Handler**:
```dart
void _setupNotificationHandlers() {
  _notificationHub?.on('BulkInvitationCompleted', (arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;

    // Parse payload
    final completion = BulkInvitationCompletion(
      bulkJobId: data['bulkJobId'],
      status: data['status'],
      successCount: data['successCount'],
      failedCount: data['failedCount'],
      completedAt: DateTime.parse(data['completedAt']),
    );

    // Show completion notification
    _showBulkInvitationCompletedNotification(completion);

    // Navigate to results screen
    if (completion.failedCount > 0) {
      navigationService.navigateTo('/bulk-invitation-results',
        arguments: completion.bulkJobId);
    }

    print('✅ Bulk invitation completed: ${completion.successCount}/${completion.successCount + completion.failedCount}');
  });
}
```

**When to Show**:
- Bulk invitation processing finished
- Show final success/failure counts

**User Action**:
- Show success notification
- If failures exist, offer to view failed items
- Refresh dealer invitation list

---

#### Event: `BulkCodeDistributionProgress`

**Hub**: NotificationHub
**Trigger**: During bulk code distribution to farmers
**Target**: Sponsor group (sponsor_{sponsorId})

**Payload**:
```json
{
  "sponsorId": 123,
  "jobId": 789,
  "totalCount": 50,
  "processedCount": 25,
  "successCount": 24,
  "failedCount": 1,
  "progressPercentage": 50,
  "currentStatus": "Processing",
  "estimatedTimeRemaining": 15
}
```

**Flutter Handler**:
```dart
void _setupNotificationHandlers() {
  _notificationHub?.on('BulkCodeDistributionProgress', (arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;

    // Parse payload
    final progress = BulkCodeDistributionProgress(
      sponsorId: data['sponsorId'],
      jobId: data['jobId'],
      totalCount: data['totalCount'],
      processedCount: data['processedCount'],
      successCount: data['successCount'],
      failedCount: data['failedCount'],
      progressPercentage: data['progressPercentage'],
      currentStatus: data['currentStatus'],
      estimatedTimeRemaining: data['estimatedTimeRemaining'],
    );

    // Update progress UI
    _bulkCodeDistributionProgressController.add(progress);

    print('📊 Bulk code distribution progress: ${progress.progressPercentage}%');
  });
}
```

**When to Show**:
- Sponsor distributing codes to multiple farmers
- Real-time progress during distribution

**User Action**:
- Show progress bar
- Display distribution statistics
- Show estimated completion time

---

#### Event: `BulkCodeDistributionCompleted`

**Hub**: NotificationHub
**Trigger**: When bulk code distribution completes
**Target**: Sponsor group (sponsor_{sponsorId})

**Payload**:
```json
{
  "jobId": 789,
  "status": "Completed",
  "successCount": 49,
  "failedCount": 1,
  "completedAt": "2026-01-04T10:50:00Z"
}
```

**Flutter Handler**:
```dart
void _setupNotificationHandlers() {
  _notificationHub?.on('BulkCodeDistributionCompleted', (arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final data = arguments[0] as Map<String, dynamic>;

    // Parse payload
    final completion = BulkCodeDistributionCompletion(
      jobId: data['jobId'],
      status: data['status'],
      successCount: data['successCount'],
      failedCount: data['failedCount'],
      completedAt: DateTime.parse(data['completedAt']),
    );

    // Show completion notification
    _showBulkDistributionCompletedNotification(completion);

    // Navigate to results if needed
    if (completion.failedCount > 0) {
      navigationService.navigateTo('/bulk-distribution-results',
        arguments: completion.jobId);
    }

    print('✅ Bulk distribution completed: ${completion.successCount}/${completion.successCount + completion.failedCount}');
  });
}
```

**When to Show**:
- Code distribution finished
- Show final statistics

**User Action**:
- Show success notification
- If failures, offer to retry failed items
- Refresh sponsorship dashboard

---

## Group Management

### Automatic Group Joining

When a user connects to NotificationHub, they are automatically added to groups based on their JWT claims:

| Group Type | Format | Used For |
|------------|--------|----------|
| Email Group | `email_{userEmail}` | Dealer invitations |
| Phone Group | `phone_{normalizedPhone}` | Farmer invitations |
| User Group | `user_{userId}` | User-specific events |
| Sponsor Group | `sponsor_{userId}` | Sponsor-only events |

### Phone Normalization

**Critical**: Phone numbers are normalized before creating group names. The mobile app must use the same normalization logic.

```dart
String normalizePhone(String phone) {
  if (phone.isEmpty) return phone;

  return phone
      .replaceAll(' ', '')
      .replaceAll('-', '')
      .replaceAll('(', '')
      .replaceAll(')', '')
      .replaceAll('+', '');
}

// Example:
// Input: "+90 555 123 45 67"
// Output: "905551234567"
// Group: "phone_905551234567"
```

### Connection Lifecycle

```dart
// On connection
await _notificationHub?.start();
print('✅ Connected to NotificationHub');
// Backend automatically adds user to appropriate groups

// On disconnection
await _notificationHub?.stop();
print('❌ Disconnected from NotificationHub');
// Backend automatically removes user from groups
```

---

## Error Handling

### Connection Errors

```dart
class SignalRService {
  Future<void> initialize(String accessToken) async {
    _accessToken = accessToken;

    try {
      await _initializeNotificationHub();
      await _initializePlantAnalysisHub();
    } catch (e) {
      print('❌ SignalR connection failed: $e');

      // Retry after delay
      await Future.delayed(Duration(seconds: 5));
      await initialize(accessToken);
    }
  }

  void _handleConnectionError(Exception error) {
    if (error.toString().contains('401')) {
      // Token expired - refresh and reconnect
      _refreshTokenAndReconnect();
    } else if (error.toString().contains('Network')) {
      // Network issue - retry with backoff
      _retryWithBackoff();
    } else {
      // Unknown error - log and notify user
      print('⚠️ SignalR error: $error');
      _showErrorNotification(error);
    }
  }
}
```

### Message Handling Errors

```dart
void _setupSafeEventHandler(String eventName, Function(dynamic) handler) {
  _notificationHub?.on(eventName, (arguments) {
    try {
      handler(arguments);
    } catch (e) {
      print('❌ Error handling $eventName: $e');
      // Log to analytics
      _analytics.logError('signalr_handler_error', {
        'event': eventName,
        'error': e.toString(),
      });
    }
  });
}
```

### Automatic Reconnection

SignalR client automatically reconnects with exponential backoff:

```dart
.withAutomaticReconnect(retryDelays: [
  0,      // Immediate retry
  2000,   // 2 seconds
  10000,  // 10 seconds
  30000   // 30 seconds (then keeps trying every 30s)
])
```

**Connection State Events**:
```dart
_notificationHub?.onclose((error) {
  print('❌ Connection closed: $error');
  _isConnected = false;
  _connectionStateController.add(false);
});

_notificationHub?.onreconnecting((error) {
  print('🔄 Reconnecting: $error');
  _showReconnectingIndicator();
});

_notificationHub?.onreconnected((connectionId) {
  print('✅ Reconnected: $connectionId');
  _isConnected = true;
  _connectionStateController.add(true);
  _hideReconnectingIndicator();
});
```

---

## Testing Guide

### 1. Test Connection

```dart
// Test ping-pong for NotificationHub
Future<void> testNotificationHub() async {
  await _notificationHub?.invoke('Ping');
  print('🏓 Ping sent to NotificationHub');
}

// Test ping-pong for PlantAnalysisHub
Future<void> testPlantAnalysisHub() async {
  _plantAnalysisHub?.on('Pong', (arguments) {
    final timestamp = arguments?[0];
    print('🏓 Pong received: $timestamp');
  });

  await _plantAnalysisHub?.invoke('Ping');
}
```

### 2. Test Dealer Invitation Notification

**Backend Test** (via Postman/Swagger):
```http
POST /api/v1/Sponsorship/invite-dealer-sms
Authorization: Bearer {sponsor_jwt}
Content-Type: application/json

{
  "email": "dealer@example.com",
  "phone": "+905551234567",
  "dealerName": "Test Dealer",
  "codeCount": 10,
  "packageTier": "M"
}
```

**Expected Result**:
- Mobile app receives `NewDealerInvitation` event
- Notification appears in app
- Invitation shows in "Dealer Invitations" screen

### 3. Test Farmer Invitation Notification

**Backend Test**:
```http
POST /api/v1/Sponsorship/farmer-invitations
Authorization: Bearer {sponsor_jwt}
Content-Type: application/json

{
  "phone": "+905551234567",
  "farmerName": "Test Farmer",
  "codeCount": 1,
  "packageTier": "S"
}
```

**Expected Result**:
- Mobile app (logged in with matching phone) receives `NewFarmerInvitation` event
- Notification appears
- Invitation shows in "Farmer Invitations" screen

### 4. Test Plant Analysis Events

**Backend Test**:
```http
POST /api/v1/PlantAnalysis/analyze-async
Authorization: Bearer {farmer_jwt}
Content-Type: multipart/form-data

image: [plant_image.jpg]
```

**Expected Results**:
1. Analysis starts processing
2. `AnalysisProgress` event (if long-running)
3. `AnalysisCompleted` event (on success) OR `AnalysisFailed` (on error)
4. Mobile app navigates to results screen

### 5. Test Messaging Events

**Test Typing Indicator**:
```dart
// User A starts typing
await _plantAnalysisHub.invoke('StartTyping',
  args: [userBId, analysisId]);

// User B should see typing indicator

// User A stops typing
await _plantAnalysisHub.invoke('StopTyping',
  args: [userBId, analysisId]);

// User B should see indicator disappear
```

**Test Message Delivery**:
```http
POST /api/v1/AnalysisMessages
Authorization: Bearer {user_jwt}
Content-Type: application/json

{
  "toUserId": 123,
  "plantAnalysisId": 456,
  "message": "Test message"
}
```

**Expected Result**:
- Recipient receives `NewMessage` event
- Message appears in chat UI
- Unread count increments

---

## Best Practices

### 1. Connection Management

✅ **Do**:
- Initialize SignalR after user login
- Disconnect on logout
- Reconnect after token refresh
- Use automatic reconnection
- Monitor connection state

❌ **Don't**:
- Keep connection alive after logout
- Connect without authentication token
- Ignore reconnection failures
- Connect multiple times simultaneously

### 2. Event Handling

✅ **Do**:
- Wrap handlers in try-catch
- Validate payload structure
- Log errors to analytics
- Handle missing data gracefully
- Update UI reactively

❌ **Don't**:
- Assume payload structure is always correct
- Block UI thread with heavy processing
- Ignore null/undefined values
- Trust all incoming data

### 3. Notification Display

✅ **Do**:
- Show in-app notifications for foreground
- Use push notifications for background
- Play sounds for important events
- Respect user notification preferences
- Clear old notifications

❌ **Don't**:
- Spam users with notifications
- Show notifications when user is in relevant screen
- Play sounds for every event
- Ignore notification permissions

### 4. Performance

✅ **Do**:
- Use WebSockets transport (preferred)
- Implement exponential backoff for retries
- Debounce typing indicators (500ms)
- Batch UI updates when possible
- Close connections when app is terminated

❌ **Don't**:
- Send typing events for every keystroke
- Keep connections open indefinitely
- Process events synchronously
- Update UI for every progress event

### 5. Security

✅ **Do**:
- Use HTTPS in production
- Validate JWT tokens
- Verify event data before using
- Log security-related events
- Implement rate limiting on client

❌ **Don't**:
- Store tokens in plain text
- Trust all incoming events
- Expose sensitive data in logs
- Send tokens in query parameters

### 6. User Experience

✅ **Do**:
- Show connection status indicator
- Provide offline mode gracefully
- Queue messages when offline
- Auto-retry failed operations
- Give feedback for all actions

❌ **Don't**:
- Hide connection problems from users
- Lose messages when offline
- Block app when connection fails
- Leave users wondering what happened

---

## Troubleshooting

### Connection Issues

**Problem**: Connection fails with 401 Unauthorized

**Solution**:
```dart
// Verify JWT token is valid
print('Token: ${_accessToken?.substring(0, 20)}...');

// Check token expiry
final tokenExpiry = JwtDecoder.getExpirationDate(_accessToken);
if (tokenExpiry.isBefore(DateTime.now())) {
  print('❌ Token expired - refreshing...');
  await refreshConnection();
}
```

**Problem**: Connection drops frequently

**Solution**:
- Check network stability
- Verify server is running
- Increase automatic reconnect delays
- Implement heartbeat/ping mechanism

### Event Not Received

**Problem**: Event sent from backend but not received in app

**Solution**:
```dart
// 1. Verify handler is registered
print('Registered handlers: ${_notificationHub?.getHandlerNames()}');

// 2. Check connection state
print('Connected: $_isConnected');

// 3. Verify user is in correct group
// Check backend logs for group assignments

// 4. Test with Ping/Pong
await testNotificationHub();
```

### Phone Normalization Mismatch

**Problem**: Farmer invitations not received

**Solution**:
```dart
// Ensure phone normalization matches backend
String testPhone = "+90 555 123 45 67";
String normalized = normalizePhone(testPhone);
print('Normalized: $normalized'); // Should be: 905551234567

// Verify group name in backend logs
// Should see: "phone_905551234567"
```

---

## Example: Complete SignalR Service

```dart
import 'dart:async';
import 'package:signalr_netcore/signalr_client.dart';

class SignalRService {
  // Hubs
  HubConnection? _notificationHub;
  HubConnection? _plantAnalysisHub;

  // State
  bool _isConnected = false;
  bool get isConnected => _isConnected;

  // Configuration
  final String _baseUrl;
  String? _accessToken;

  // Controllers for events
  final _dealerInvitationController = StreamController<DealerInvitation>.broadcast();
  final _farmerInvitationController = StreamController<FarmerInvitation>.broadcast();
  final _analysisCompletedController = StreamController<AnalysisResult>.broadcast();
  final _analysisFailedController = StreamController<AnalysisFailure>.broadcast();
  final _newMessageController = StreamController<MessageNotification>.broadcast();
  final _connectionStateController = StreamController<bool>.broadcast();

  // Public streams
  Stream<DealerInvitation> get dealerInvitations => _dealerInvitationController.stream;
  Stream<FarmerInvitation> get farmerInvitations => _farmerInvitationController.stream;
  Stream<AnalysisResult> get analysisCompleted => _analysisCompletedController.stream;
  Stream<AnalysisFailure> get analysisFailed => _analysisFailedController.stream;
  Stream<MessageNotification> get newMessages => _newMessageController.stream;
  Stream<bool> get connectionState => _connectionStateController.stream;

  SignalRService({required String baseUrl}) : _baseUrl = baseUrl;

  Future<void> initialize(String accessToken) async {
    _accessToken = accessToken;

    try {
      await _initializeNotificationHub();
      await _initializePlantAnalysisHub();

      _isConnected = true;
      _connectionStateController.add(true);
    } catch (e) {
      print('❌ SignalR initialization failed: $e');
      _isConnected = false;
      _connectionStateController.add(false);
      rethrow;
    }
  }

  Future<void> _initializeNotificationHub() async {
    final options = HttpConnectionOptions(
      accessTokenFactory: () async => _accessToken,
      transport: HttpTransportType.WebSockets,
      logging: (level, message) => print('[NotificationHub] $message'),
    );

    _notificationHub = HubConnectionBuilder()
        .withUrl('$_baseUrl/hubs/notification', options: options)
        .withAutomaticReconnect(retryDelays: [0, 2000, 10000, 30000])
        .build();

    _setupNotificationHandlers();
    _setupConnectionHandlers(_notificationHub, 'NotificationHub');

    await _notificationHub?.start();
    print('✅ NotificationHub connected');
  }

  Future<void> _initializePlantAnalysisHub() async {
    final options = HttpConnectionOptions(
      accessTokenFactory: () async => _accessToken,
      transport: HttpTransportType.WebSockets,
      logging: (level, message) => print('[PlantAnalysisHub] $message'),
    );

    _plantAnalysisHub = HubConnectionBuilder()
        .withUrl('$_baseUrl/hubs/plantanalysis', options: options)
        .withAutomaticReconnect(retryDelays: [0, 2000, 10000, 30000])
        .build();

    _setupPlantAnalysisHandlers();
    _setupConnectionHandlers(_plantAnalysisHub, 'PlantAnalysisHub');

    await _plantAnalysisHub?.start();
    print('✅ PlantAnalysisHub connected');
  }

  void _setupNotificationHandlers() {
    // Dealer Invitations
    _notificationHub?.on('NewDealerInvitation', (arguments) {
      try {
        if (arguments == null || arguments.isEmpty) return;
        final data = arguments[0] as Map<String, dynamic>;
        final invitation = DealerInvitation.fromJson(data);
        _dealerInvitationController.add(invitation);
      } catch (e) {
        print('❌ Error handling NewDealerInvitation: $e');
      }
    });

    // Farmer Invitations
    _notificationHub?.on('NewFarmerInvitation', (arguments) {
      try {
        if (arguments == null || arguments.isEmpty) return;
        final data = arguments[0] as Map<String, dynamic>;
        final invitation = FarmerInvitation.fromJson(data);
        _farmerInvitationController.add(invitation);
      } catch (e) {
        print('❌ Error handling NewFarmerInvitation: $e');
      }
    });
  }

  void _setupPlantAnalysisHandlers() {
    // Analysis Completed
    _plantAnalysisHub?.on('AnalysisCompleted', (arguments) {
      try {
        if (arguments == null || arguments.isEmpty) return;
        final data = arguments[0] as Map<String, dynamic>;
        final result = AnalysisResult.fromJson(data);
        _analysisCompletedController.add(result);
      } catch (e) {
        print('❌ Error handling AnalysisCompleted: $e');
      }
    });

    // Analysis Failed
    _plantAnalysisHub?.on('AnalysisFailed', (arguments) {
      try {
        if (arguments == null || arguments.isEmpty) return;
        final data = arguments[0] as Map<String, dynamic>;
        final failure = AnalysisFailure.fromJson(data);
        _analysisFailedController.add(failure);
      } catch (e) {
        print('❌ Error handling AnalysisFailed: $e');
      }
    });

    // New Message
    _plantAnalysisHub?.on('NewMessage', (arguments) {
      try {
        if (arguments == null || arguments.isEmpty) return;
        final data = arguments[0] as Map<String, dynamic>;
        final message = MessageNotification.fromJson(data);
        _newMessageController.add(message);
      } catch (e) {
        print('❌ Error handling NewMessage: $e');
      }
    });
  }

  void _setupConnectionHandlers(HubConnection? hub, String hubName) {
    hub?.onclose((error) {
      print('❌ $hubName closed: $error');
      _isConnected = false;
      _connectionStateController.add(false);
    });

    hub?.onreconnecting((error) {
      print('🔄 $hubName reconnecting: $error');
    });

    hub?.onreconnected((connectionId) {
      print('✅ $hubName reconnected: $connectionId');
      _isConnected = true;
      _connectionStateController.add(true);
    });
  }

  Future<void> disconnect() async {
    await _notificationHub?.stop();
    await _plantAnalysisHub?.stop();
    _isConnected = false;
    _connectionStateController.add(false);
    print('❌ SignalR disconnected');
  }

  Future<void> sendTypingIndicator(int recipientUserId, int analysisId, bool isTyping) async {
    try {
      if (isTyping) {
        await _plantAnalysisHub?.invoke('StartTyping', args: [recipientUserId, analysisId]);
      } else {
        await _plantAnalysisHub?.invoke('StopTyping', args: [recipientUserId, analysisId]);
      }
    } catch (e) {
      print('❌ Error sending typing indicator: $e');
    }
  }

  Future<void> markMessageAsRead(int senderUserId, int messageId) async {
    try {
      await _plantAnalysisHub?.invoke('NotifyMessageRead', args: [senderUserId, messageId]);
    } catch (e) {
      print('❌ Error marking message as read: $e');
    }
  }

  void dispose() {
    _dealerInvitationController.close();
    _farmerInvitationController.close();
    _analysisCompletedController.close();
    _analysisFailedController.close();
    _newMessageController.close();
    _connectionStateController.close();
  }
}
```

---

## Summary

This document provides complete SignalR integration guidance for the ZiraAI mobile application. Key points:

1. **Two Hubs**: NotificationHub (invitations, bulk operations) and PlantAnalysisHub (analysis + messaging)
2. **Authentication**: JWT Bearer tokens required for all connections
3. **Group-Based Targeting**: Automatic group assignment based on email/phone/userId/sponsorId
4. **Phone Normalization**: Critical for farmer invitations - must match backend logic
5. **Event Types**: 12 main events covering invitations, analysis, messaging, and bulk operations
6. **Complete Payloads**: All message payloads include full data - no additional API calls needed
7. **Error Handling**: Automatic reconnection, try-catch wrappers, graceful degradation
8. **Testing**: Step-by-step testing guide for all event types
9. **Best Practices**: Connection management, performance, security, UX guidelines

### All SignalR Events

| Hub | Event Name | Purpose |
|-----|------------|---------|
| NotificationHub | `NewDealerInvitation` | New dealer invitation notification |
| NotificationHub | `NewFarmerInvitation` | New farmer invitation notification |
| NotificationHub | `BulkInvitationProgress` | Bulk dealer invitation progress |
| NotificationHub | `BulkInvitationCompleted` | Bulk dealer invitation completed |
| NotificationHub | `BulkCodeDistributionProgress` | Bulk code distribution progress |
| NotificationHub | `BulkCodeDistributionCompleted` | Bulk code distribution completed |
| PlantAnalysisHub | `AnalysisCompleted` | Plant analysis completed |
| PlantAnalysisHub | `AnalysisFailed` | Plant analysis failed |
| PlantAnalysisHub | `AnalysisProgress` | Plant analysis progress |
| PlantAnalysisHub | `NewMessage` | New chat message |
| PlantAnalysisHub | `UserTyping` | User typing indicator |
| PlantAnalysisHub | `MessageRead` | Message read receipt |

For questions or issues, contact the backend team.
