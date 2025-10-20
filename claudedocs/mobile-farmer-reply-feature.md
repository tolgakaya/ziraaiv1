# Farmer Reply Feature - Mobile Integration Guide

## Overview
Farmers can now reply to sponsor messages. This document outlines the API changes and UI implementation requirements.

---

## API Changes

### Endpoint: `GET /api/v1/PlantAnalyses/{id}/detail`

#### New Field in Response
A new `canReply` field has been added to `sponsorshipMetadata`:

```json
{
  "id": 60,
  "analysisDate": "2025-10-18T10:30:00Z",
  // ... other analysis fields ...

  "sponsorshipMetadata": {
    "tierName": "L",
    "accessPercentage": 60,
    "canMessage": true,
    "canReply": true,  // ✅ NEW FIELD
    "canViewLogo": true,
    "sponsorInfo": {
      "sponsorId": 159,
      "companyName": "Dort Tarim",
      "logoUrl": "https://...",
      "websiteUrl": "https://..."
    },
    "accessibleFields": {
      "canViewBasicInfo": true,
      "canViewHealthScore": true,
      // ... other fields
    }
  }
}
```

#### Field Behavior

| Condition | `canReply` Value | Meaning |
|-----------|------------------|---------|
| Sponsor has sent at least one message | `true` | Farmer can reply |
| Sponsor has not sent any message yet | `false` | Farmer cannot send messages |
| Analysis not sponsored | `null` | No sponsorship metadata |

---

## Mobile Implementation

### 1. UI Logic for Reply Button

```dart
// Example Flutter implementation
Widget buildMessageButton(PlantAnalysisDetail analysis) {
  // Check if analysis is sponsored
  if (analysis.sponsorshipMetadata == null) {
    return Container(); // No sponsor, no messaging
  }

  // Check if farmer can reply
  if (analysis.sponsorshipMetadata.canReply == true) {
    return ElevatedButton(
      onPressed: () => openMessageDialog(analysis),
      child: Text('Reply to Sponsor'),
    );
  } else {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(12),
        child: Row(
          children: [
            Icon(Icons.info_outline, color: Colors.blue),
            SizedBox(width: 8),
            Expanded(
              child: Text(
                'The sponsor can send you a message about this analysis',
                style: TextStyle(fontSize: 14),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
```

### 2. Sending a Reply

#### Endpoint: `POST /api/v1/sponsorship/messages`

**Authorization**: Required - Farmer role

**Request Body**:
```json
{
  "fromUserId": 123,              // Farmer's user ID (from JWT)
  "toUserId": 159,                // Sponsor's user ID (from sponsorshipMetadata.sponsorInfo.sponsorId)
  "plantAnalysisId": 60,          // Analysis ID
  "message": "Thank you for your message. I have a question about...",
  "messageType": "Information",
  "priority": "Normal",
  "category": "General"
}
```

**Success Response (200 OK)**:
```json
{
  "data": {
    "id": 456,
    "plantAnalysisId": 60,
    "fromUserId": 123,
    "toUserId": 159,
    "message": "Thank you for your message...",
    "sentDate": "2025-10-18T15:45:00Z",
    "isRead": false,
    "senderRole": "Farmer",
    "senderName": "Ahmet Yılmaz"
  },
  "success": true,
  "message": "Message sent successfully"
}
```

**Error Responses**:

```json
// 400 - Cannot reply (sponsor hasn't sent message yet)
{
  "success": false,
  "message": "You can only reply after the sponsor sends you a message first"
}

// 403 - Forbidden (authorization issue)
{
  "success": false,
  "message": "Access denied"
}

// 400 - Blocked
{
  "success": false,
  "message": "You have blocked this sponsor"
}
```

---

## UI/UX Recommendations

### When `canReply = false`
Show an informative message instead of a button:

**Option 1 - Informational Card**:
```
ℹ️ This analysis is sponsored by [Company Name]
   They can send you a message if they have questions.
```

**Option 2 - Minimalist**:
```
🏢 Sponsored by [Company Name]
```

### When `canReply = true`
Show an actionable reply button:

**Option 1 - Button**:
```
[💬 Reply to Sponsor]
```

**Option 2 - Card with Action**:
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
💬 Message from Dort Tarim
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[View Messages]  [Reply]
```

### Message Input Dialog
When farmer clicks reply button:

```
┌─────────────────────────────────┐
│ Reply to Dort Tarim             │
├─────────────────────────────────┤
│                                 │
│ [Text input area]               │
│                                 │
│                                 │
├─────────────────────────────────┤
│         [Cancel]  [Send Reply]  │
└─────────────────────────────────┘
```

---

## Example User Flow

### Scenario 1: Farmer views sponsored analysis (no messages yet)

1. Farmer opens analysis detail screen
2. API returns `canReply: false`
3. UI shows: "Sponsored by Dort Tarim" (informational only)
4. No reply button visible
5. Farmer can view sponsor logo and company info

### Scenario 2: Sponsor sends first message

1. Sponsor sends message: "We noticed your crop has nutrient deficiency. We can help."
2. Farmer receives notification (if implemented)
3. Backend creates message record

### Scenario 3: Farmer replies

1. Farmer opens same analysis detail screen
2. API now returns `canReply: true`
3. UI shows "Reply to Sponsor" button
4. Farmer clicks button → message dialog opens
5. Farmer types reply: "Thank you! What products do you recommend?"
6. Farmer sends → `POST /api/v1/sponsorship/messages`
7. Success → Show success message
8. Navigate to conversation view (optional)

---

## Testing Checklist

- [ ] Analysis without sponsorship: No metadata shown
- [ ] Sponsored analysis, no messages: `canReply: false`, info card shown
- [ ] Sponsored analysis, sponsor sent message: `canReply: true`, reply button shown
- [ ] Reply button click: Opens message dialog
- [ ] Send reply: Success message shown
- [ ] Error handling: Network errors, validation errors displayed
- [ ] UI states: Loading, success, error states handled
- [ ] Authorization: JWT token included in request headers

---

## Error Handling

### Network Errors
```dart
try {
  await sendMessage(messageData);
  showSuccessSnackbar('Message sent successfully');
} catch (e) {
  if (e is NetworkException) {
    showErrorDialog('Network error. Please check your connection.');
  } else if (e is ApiException) {
    showErrorDialog(e.message); // Display backend error message
  }
}
```

### Validation Before Sending
```dart
bool canSendMessage() {
  if (messageText.isEmpty) {
    showError('Message cannot be empty');
    return false;
  }

  if (messageText.length > 1000) {
    showError('Message too long (max 1000 characters)');
    return false;
  }

  if (sponsorshipMetadata?.canReply != true) {
    showError('You cannot send messages yet');
    return false;
  }

  return true;
}
```

---

## Data Models (Example)

```dart
class SponsorshipMetadata {
  final String tierName;
  final int accessPercentage;
  final bool canMessage;
  final bool canReply;  // ✅ NEW FIELD
  final bool canViewLogo;
  final SponsorInfo? sponsorInfo;
  final AccessibleFieldsInfo accessibleFields;

  SponsorshipMetadata({
    required this.tierName,
    required this.accessPercentage,
    required this.canMessage,
    required this.canReply,  // ✅ ADD TO CONSTRUCTOR
    required this.canViewLogo,
    this.sponsorInfo,
    required this.accessibleFields,
  });

  factory SponsorshipMetadata.fromJson(Map<String, dynamic> json) {
    return SponsorshipMetadata(
      tierName: json['tierName'],
      accessPercentage: json['accessPercentage'],
      canMessage: json['canMessage'],
      canReply: json['canReply'] ?? false,  // ✅ ADD WITH DEFAULT
      canViewLogo: json['canViewLogo'],
      sponsorInfo: json['sponsorInfo'] != null
          ? SponsorInfo.fromJson(json['sponsorInfo'])
          : null,
      accessibleFields: AccessibleFieldsInfo.fromJson(json['accessibleFields']),
    );
  }
}

class SponsorInfo {
  final int sponsorId;
  final String companyName;
  final String? logoUrl;
  final String? websiteUrl;

  SponsorInfo({
    required this.sponsorId,
    required this.companyName,
    this.logoUrl,
    this.websiteUrl,
  });

  factory SponsorInfo.fromJson(Map<String, dynamic> json) {
    return SponsorInfo(
      sponsorId: json['sponsorId'],
      companyName: json['companyName'],
      logoUrl: json['logoUrl'],
      websiteUrl: json['websiteUrl'],
    );
  }
}
```

---

## Important Notes

1. **Reply-Only**: Farmers can ONLY reply to existing sponsor messages, they cannot initiate conversations
2. **Authorization**: Farmer JWT token must be included in API requests
3. **Backward Compatibility**: Existing analysis responses remain unchanged, only `sponsorshipMetadata` is enhanced
4. **toUserId**: Use `sponsorshipMetadata.sponsorInfo.sponsorId` as the `toUserId` when sending messages
5. **Validation**: Backend validates that sponsor has sent at least one message before allowing farmer reply

---

## Real-Time Notifications (SignalR)

### Overview
When a sponsor sends a message, farmers receive **real-time notifications via SignalR** WebSocket connection.

### SignalR Hub Endpoint
```
wss://ziraai-api-sit.up.railway.app/hubs/plantanalysis
```

### Event: `NewMessage`

When a sponsor sends a message, the farmer receives this event:

```json
{
  "messageId": 456,
  "plantAnalysisId": 60,
  "fromUserId": 159,
  "fromUserName": "Ahmet Yılmaz",
  "fromUserCompany": "Dort Tarim",
  "senderRole": "Sponsor",
  "message": "We noticed your crop has nutrient deficiency. We can help.",
  "messageType": "Information",
  "sentDate": "2025-10-18T15:45:00Z",
  "isApproved": true,
  "requiresApproval": false
}
```

### Mobile Implementation

#### 1. Connect to SignalR Hub

```dart
import 'package:signalr_netcore/signalr_client.dart';

class SignalRService {
  HubConnection? _hubConnection;

  Future<void> connect(String accessToken) async {
    final hubUrl = 'https://ziraai-api-sit.up.railway.app/hubs/plantanalysis';

    _hubConnection = HubConnectionBuilder()
      .withUrl(
        hubUrl,
        options: HttpConnectionOptions(
          accessTokenFactory: () => Future.value(accessToken),
        ),
      )
      .build();

    // Listen for new messages
    _hubConnection!.on('NewMessage', _handleNewMessage);

    await _hubConnection!.start();
  }

  void _handleNewMessage(List<Object>? arguments) {
    if (arguments == null || arguments.isEmpty) return;

    final messageData = arguments[0] as Map<String, dynamic>;

    // Show notification
    showNotification(
      title: '${messageData['fromUserCompany']} sent you a message',
      body: messageData['message'],
      data: messageData,
    );

    // Update UI if on messages screen
    // Refresh message list, etc.
  }

  Future<void> disconnect() async {
    await _hubConnection?.stop();
  }
}
```

#### 2. Initialize on Login

```dart
// After successful login
final signalR = SignalRService();
await signalR.connect(accessToken);
```

#### 3. Handle Notification

```dart
void showNotification(String title, String body, Map data) {
  // Use flutter_local_notifications or similar
  LocalNotification.show(
    title: title,
    body: body,
    payload: jsonEncode(data),
  );
}
```

#### 4. Disconnect on Logout

```dart
// On user logout
await signalR.disconnect();
```

### Testing SignalR

1. Connect to SignalR hub with JWT token
2. Have sponsor send a message via API
3. Verify `NewMessage` event is received
4. Check notification is displayed

### Error Handling

```dart
_hubConnection!.onclose((error) {
  print('SignalR connection closed: $error');
  // Attempt reconnection with exponential backoff
});

_hubConnection!.onreconnecting((error) {
  print('SignalR reconnecting: $error');
});

_hubConnection!.onreconnected((connectionId) {
  print('SignalR reconnected: $connectionId');
});
```

---

## Contact

For questions or issues, contact the backend team.

**API Documentation**: https://ziraai-api-sit.up.railway.app/swagger
**SignalR Hub**: wss://ziraai-api-sit.up.railway.app/hubs/plantanalysis
**Backend Team**: backend@ziraai.com
