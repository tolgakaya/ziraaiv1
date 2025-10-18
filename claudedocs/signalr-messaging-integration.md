# SignalR Messaging Integration - Mobile Team

## Quick Overview
SignalR real-time notifications are now enabled for sponsor-farmer messaging. You already have SignalR implemented for PlantAnalysis notifications - just add one more event handler.

---

## What's New

### Event: `NewMessage`

**Triggered when**: Sponsor sends a message to farmer (or farmer replies to sponsor)

**Hub**: Same hub you're already using (`/hubs/plantanalysis`)

**Event Payload**:
```json
{
  "messageId": 456,
  "plantAnalysisId": 60,
  "fromUserId": 159,
  "fromUserName": "Ahmet Yılmaz",
  "fromUserCompany": "Dort Tarim",
  "senderRole": "Sponsor",  // "Sponsor" or "Farmer"
  "message": "We can help with your crop's nutrient deficiency.",
  "messageType": "Information",
  "sentDate": "2025-10-18T15:45:00Z",
  "isApproved": true,
  "requiresApproval": false
}
```

---

## Implementation (3 Steps)

### Step 1: Add Event Listener to Existing SignalR Service

You already have SignalR connected for `AnalysisCompleted` event. Just add one more listener:

```dart
// In your existing SignalRService
Future<void> connect(String accessToken) async {
  // ... existing code ...

  // Existing listener
  _hubConnection!.on('AnalysisCompleted', _handleAnalysisCompleted);

  // ✅ NEW: Add message listener
  _hubConnection!.on('NewMessage', _handleNewMessage);

  await _hubConnection!.start();
}

// ✅ NEW: Message handler
void _handleNewMessage(List<Object>? arguments) {
  if (arguments == null || arguments.isEmpty) return;

  final data = arguments[0] as Map<String, dynamic>;

  // Notify listeners
  _messageStreamController.add(MessageNotification.fromJson(data));

  // Show notification
  _showNotification(
    title: data['senderRole'] == 'Sponsor'
        ? '${data['fromUserCompany']} sent you a message'
        : 'New message from farmer',
    body: data['message'],
    payload: jsonEncode(data),
  );
}
```

### Step 2: Create Message Notification Model

```dart
class MessageNotification {
  final int messageId;
  final int plantAnalysisId;
  final int fromUserId;
  final String fromUserName;
  final String? fromUserCompany;
  final String senderRole;  // "Sponsor" or "Farmer"
  final String message;
  final DateTime sentDate;
  final bool isApproved;

  MessageNotification.fromJson(Map<String, dynamic> json)
      : messageId = json['messageId'],
        plantAnalysisId = json['plantAnalysisId'],
        fromUserId = json['fromUserId'],
        fromUserName = json['fromUserName'],
        fromUserCompany = json['fromUserCompany'],
        senderRole = json['senderRole'],
        message = json['message'],
        sentDate = DateTime.parse(json['sentDate']),
        isApproved = json['isApproved'];
}
```

### Step 3: Handle in UI

```dart
// In your messages screen or app-wide listener
class MessagesScreen extends StatefulWidget {
  @override
  _MessagesScreenState createState() => _MessagesScreenState();
}

class _MessagesScreenState extends State<MessagesScreen> {
  StreamSubscription? _messageSubscription;

  @override
  void initState() {
    super.initState();

    // Listen to message notifications
    _messageSubscription = SignalRService()
        .messageStream
        .listen(_onNewMessage);
  }

  void _onNewMessage(MessageNotification notification) {
    // If user is on this analysis's message screen, refresh
    if (notification.plantAnalysisId == currentAnalysisId) {
      _refreshMessages();
    }

    // Update unread count badge
    _updateUnreadCount();
  }

  @override
  void dispose() {
    _messageSubscription?.cancel();
    super.dispose();
  }
}
```

---

## Testing

1. **Connect SignalR** (you're already doing this for PlantAnalysis)
2. **Sponsor sends message** via Postman:
   ```bash
   POST /api/v1/sponsorship/messages
   {
     "fromUserId": 159,
     "toUserId": 123,
     "plantAnalysisId": 60,
     "message": "Test message"
   }
   ```
3. **Check logs** - Should see: `NewMessage event received`
4. **Verify notification** appears on farmer's device

---

## Notes

- Same SignalR connection handles both `AnalysisCompleted` and `NewMessage` events
- No additional connection needed
- Event is sent to `toUserId` (the recipient)
- Works for both sponsor→farmer and farmer→sponsor messages
- Falls back gracefully if SignalR fails (message still saved in DB, can poll)

---

## Quick Checklist

- [ ] Add `NewMessage` event listener to existing SignalR service
- [ ] Create `MessageNotification` model
- [ ] Handle event in messages UI screen
- [ ] Test with Postman
- [ ] Verify notification appears

That's it! 🚀
