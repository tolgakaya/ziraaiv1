# Conversation Endpoint Usage Guide

**Endpoint**: `GET /api/v1/sponsorship/conversations`
**Authentication**: Required (JWT Bearer Token)
**Purpose**: Load chat messages with proper pagination for messaging UI

---

## 🎯 Quick Start

### Initial Load (Most Recent Messages)
```http
GET /api/v1/sponsorship/conversations?fromUserId={userId}&toUserId={otherUserId}&plantAnalysisId={analysisId}&page=1&pageSize=20
```

### Load Older Messages (Scroll Up)
```http
GET /api/v1/sponsorship/conversations?fromUserId={userId}&toUserId={otherUserId}&plantAnalysisId={analysisId}&page=2&pageSize=20
```

---

## 📝 Request Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `fromUserId` | int | ✅ Yes | - | Current user's ID |
| `toUserId` | int | ✅ Yes | - | Other participant's ID |
| `plantAnalysisId` | int | ✅ Yes | - | Plant analysis context ID |
| `page` | int | ⚠️ Optional | 1 | Page number (1 = most recent) |
| `pageSize` | int | ⚠️ Optional | 20 | Messages per page |

**Note**: For three-way conversations (farmer-sponsor-dealer), all messages in the analysis are visible to all participants.

---

## 📦 Response Structure

```json
{
  "data": [
    {
      "id": 123,
      "plantAnalysisId": 456,
      "fromUserId": 10,
      "toUserId": 20,
      "message": "Hello, how can I help?",
      "messageType": "Information",
      "subject": null,

      // Message Status
      "messageStatus": "Read",
      "isRead": true,
      "sentDate": "2024-01-15T10:30:00",
      "deliveredDate": "2024-01-15T10:30:05",
      "readDate": "2024-01-15T10:32:00",

      // Sender Info
      "senderRole": "Sponsor",
      "senderName": "John Doe",
      "senderCompany": "AgriTech Solutions",
      "senderAvatarUrl": "https://api.ziraai.com/api/v1/files/avatars/10",
      "senderAvatarThumbnailUrl": "https://api.ziraai.com/api/v1/files/avatars/10/thumbnail",

      // Receiver Info
      "receiverRole": "Farmer",
      "receiverName": "Jane Smith",
      "receiverCompany": "",
      "receiverAvatarUrl": "https://api.ziraai.com/api/v1/files/avatars/20",
      "receiverAvatarThumbnailUrl": "https://api.ziraai.com/api/v1/files/avatars/20/thumbnail",

      // Classification
      "priority": "Normal",
      "category": "General",

      // Attachments
      "hasAttachments": true,
      "attachmentCount": 2,
      "attachmentUrls": [
        "https://api.ziraai.com/api/v1/files/attachments/123/0",
        "https://api.ziraai.com/api/v1/files/attachments/123/1"
      ],
      "attachmentThumbnails": [
        "https://api.ziraai.com/api/v1/files/attachments/123/0",
        "https://api.ziraai.com/api/v1/files/attachments/123/1"
      ],
      "attachmentTypes": ["image/jpeg", "application/pdf"],
      "attachmentSizes": [245678, 102400],
      "attachmentNames": ["plant-photo.jpg", "analysis-report.pdf"],

      // Voice Messages
      "isVoiceMessage": false,
      "voiceMessageUrl": null,
      "voiceMessageDuration": null,
      "voiceMessageWaveform": null,

      // Edit/Delete/Forward
      "isEdited": false,
      "editedDate": null,
      "isForwarded": false,
      "forwardedFromMessageId": null,
      "isActive": true
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalRecords": 87,
  "totalPages": 5,
  "success": true,
  "message": null
}
```

---

## 🚀 Implementation Guide

### ✅ React/TypeScript Example

```typescript
import { useState, useEffect, useRef } from 'react';
import axios from 'axios';

interface Message {
  id: number;
  fromUserId: number;
  message: string;
  sentDate: string;
  senderName: string;
  senderAvatarUrl?: string;
  hasAttachments: boolean;
  attachmentUrls?: string[];
  isVoiceMessage: boolean;
  voiceMessageUrl?: string;
  // ... other fields
}

interface ConversationResponse {
  data: Message[];
  page: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
}

function ChatComponent({
  currentUserId,
  otherUserId,
  plantAnalysisId
}: {
  currentUserId: number;
  otherUserId: number;
  plantAnalysisId: number;
}) {
  const [messages, setMessages] = useState<Message[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const chatContainerRef = useRef<HTMLDivElement>(null);

  // Initial load - most recent messages
  useEffect(() => {
    loadMessages(1, true);
  }, []);

  const loadMessages = async (pageNum: number, isInitial: boolean = false) => {
    if (loading || (pageNum > totalPages && !isInitial)) return;

    setLoading(true);
    try {
      const response = await axios.get<ConversationResponse>(
        '/api/v1/sponsorship/conversations',
        {
          params: {
            fromUserId: currentUserId,
            toUserId: otherUserId,
            plantAnalysisId: plantAnalysisId,
            page: pageNum,
            pageSize: 20
          },
          headers: {
            Authorization: `Bearer ${localStorage.getItem('token')}`
          }
        }
      );

      const newMessages = response.data.data;

      if (isInitial) {
        // Initial load: Set messages and scroll to bottom
        setMessages(newMessages);
        setTotalPages(response.data.totalPages);
        setTimeout(() => scrollToBottom(), 100);
      } else {
        // Load older messages: Prepend to existing
        setMessages(prev => [...newMessages, ...prev]);
      }

      setPage(pageNum);
    } catch (error) {
      console.error('Failed to load messages:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleScroll = (e: React.UIEvent<HTMLDivElement>) => {
    const element = e.currentTarget;

    // Detect scroll to top (load older messages)
    if (element.scrollTop === 0 && page < totalPages) {
      const previousScrollHeight = element.scrollHeight;

      loadMessages(page + 1).then(() => {
        // Maintain scroll position after prepending messages
        const newScrollHeight = element.scrollHeight;
        element.scrollTop = newScrollHeight - previousScrollHeight;
      });
    }
  };

  const scrollToBottom = () => {
    if (chatContainerRef.current) {
      chatContainerRef.current.scrollTop = chatContainerRef.current.scrollHeight;
    }
  };

  return (
    <div
      ref={chatContainerRef}
      onScroll={handleScroll}
      className="chat-container"
      style={{
        height: '500px',
        overflowY: 'auto',
        display: 'flex',
        flexDirection: 'column'
      }}
    >
      {loading && page > 1 && (
        <div className="loading-indicator">Loading older messages...</div>
      )}

      {messages.map((msg) => (
        <div
          key={msg.id}
          className={msg.fromUserId === currentUserId ? 'message-sent' : 'message-received'}
        >
          <div className="message-bubble">
            <p>{msg.message}</p>

            {/* Attachments */}
            {msg.hasAttachments && msg.attachmentUrls?.map((url, index) => (
              <img key={index} src={url} alt="attachment" />
            ))}

            {/* Voice Message */}
            {msg.isVoiceMessage && (
              <audio src={msg.voiceMessageUrl} controls />
            )}

            <span className="timestamp">
              {new Date(msg.sentDate).toLocaleTimeString()}
            </span>
          </div>
        </div>
      ))}
    </div>
  );
}
```

---

### ✅ Flutter/Dart Example

```dart
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

class Message {
  final int id;
  final int fromUserId;
  final String message;
  final DateTime sentDate;
  final String senderName;
  final String? senderAvatarUrl;
  final bool hasAttachments;
  final List<String>? attachmentUrls;
  final bool isVoiceMessage;
  final String? voiceMessageUrl;

  Message.fromJson(Map<String, dynamic> json)
    : id = json['id'],
      fromUserId = json['fromUserId'],
      message = json['message'],
      sentDate = DateTime.parse(json['sentDate']),
      senderName = json['senderName'],
      senderAvatarUrl = json['senderAvatarUrl'],
      hasAttachments = json['hasAttachments'],
      attachmentUrls = json['attachmentUrls']?.cast<String>(),
      isVoiceMessage = json['isVoiceMessage'],
      voiceMessageUrl = json['voiceMessageUrl'];
}

class ConversationResponse {
  final List<Message> messages;
  final int page;
  final int pageSize;
  final int totalRecords;
  final int totalPages;

  ConversationResponse.fromJson(Map<String, dynamic> json)
    : messages = (json['data'] as List).map((m) => Message.fromJson(m)).toList(),
      page = json['page'],
      pageSize = json['pageSize'],
      totalRecords = json['totalRecords'],
      totalPages = json['totalPages'];
}

class ChatScreen extends StatefulWidget {
  final int currentUserId;
  final int otherUserId;
  final int plantAnalysisId;

  const ChatScreen({
    required this.currentUserId,
    required this.otherUserId,
    required this.plantAnalysisId,
  });

  @override
  _ChatScreenState createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  final List<Message> _messages = [];
  final ScrollController _scrollController = ScrollController();
  int _currentPage = 1;
  int _totalPages = 1;
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _loadMessages(1, isInitial: true);

    // Detect scroll to top
    _scrollController.addListener(() {
      if (_scrollController.position.pixels == 0 &&
          _currentPage < _totalPages &&
          !_isLoading) {
        _loadOlderMessages();
      }
    });
  }

  Future<void> _loadMessages(int page, {bool isInitial = false}) async {
    if (_isLoading) return;

    setState(() => _isLoading = true);

    try {
      final token = await _getAuthToken(); // Your auth method
      final response = await http.get(
        Uri.parse('https://api.ziraai.com/api/v1/sponsorship/conversations').replace(
          queryParameters: {
            'fromUserId': widget.currentUserId.toString(),
            'toUserId': widget.otherUserId.toString(),
            'plantAnalysisId': widget.plantAnalysisId.toString(),
            'page': page.toString(),
            'pageSize': '20',
          },
        ),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        final data = ConversationResponse.fromJson(json.decode(response.body));

        setState(() {
          if (isInitial) {
            _messages.clear();
            _messages.addAll(data.messages);
            _totalPages = data.totalPages;

            // Scroll to bottom after initial load
            WidgetsBinding.instance.addPostFrameCallback((_) {
              _scrollToBottom();
            });
          } else {
            // Prepend older messages
            _messages.insertAll(0, data.messages);
          }
          _currentPage = page;
        });
      }
    } catch (e) {
      print('Error loading messages: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }

  Future<void> _loadOlderMessages() async {
    await _loadMessages(_currentPage + 1);
  }

  void _scrollToBottom() {
    if (_scrollController.hasClients) {
      _scrollController.animateTo(
        _scrollController.position.maxScrollExtent,
        duration: Duration(milliseconds: 300),
        curve: Curves.easeOut,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Conversation')),
      body: Column(
        children: [
          if (_isLoading && _currentPage > 1)
            LinearProgressIndicator(),

          Expanded(
            child: ListView.builder(
              controller: _scrollController,
              itemCount: _messages.length,
              itemBuilder: (context, index) {
                final message = _messages[index];
                final isSentByMe = message.fromUserId == widget.currentUserId;

                return Align(
                  alignment: isSentByMe ? Alignment.centerRight : Alignment.centerLeft,
                  child: Container(
                    margin: EdgeInsets.symmetric(vertical: 4, horizontal: 8),
                    padding: EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: isSentByMe ? Colors.blue[100] : Colors.grey[200],
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(message.message),

                        // Attachments
                        if (message.hasAttachments && message.attachmentUrls != null)
                          ...message.attachmentUrls!.map((url) =>
                            Image.network(url, height: 200)
                          ),

                        // Voice Message
                        if (message.isVoiceMessage && message.voiceMessageUrl != null)
                          // Add audio player widget
                          Text('Voice Message'),

                        SizedBox(height: 4),
                        Text(
                          _formatTime(message.sentDate),
                          style: TextStyle(fontSize: 10, color: Colors.grey[600]),
                        ),
                      ],
                    ),
                  ),
                );
              },
            ),
          ),

          // Message input field
          // ... your input widget
        ],
      ),
    );
  }

  String _formatTime(DateTime date) {
    return '${date.hour}:${date.minute.toString().padLeft(2, '0')}';
  }

  Future<String> _getAuthToken() async {
    // Your token retrieval logic
    return 'your-jwt-token';
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }
}
```

---

## 📊 Pagination Logic Explained

### How It Works

```
Total Messages: 87
Page Size: 20

Database Order: Newest → Oldest (DESC)
Frontend Display: Oldest → Newest (ASC) per page

┌─────────────────────────────────────────┐
│ Page 1 (Most Recent)                    │
│ DB: Messages 87-68 (DESC)               │
│ Display: Messages 68→87 (ASC)           │ ← Initial Load
│ Shows: Most recent 20 messages          │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Page 2 (Previous)                       │
│ DB: Messages 67-48 (DESC)               │
│ Display: Messages 48→67 (ASC)           │ ← Scroll Up
│ Shows: Previous 20 messages             │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Page 3                                  │
│ DB: Messages 47-28 (DESC)               │
│ Display: Messages 28→47 (ASC)           │ ← Scroll Up
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Page 4                                  │
│ DB: Messages 27-8 (DESC)                │
│ Display: Messages 8→27 (ASC)            │ ← Scroll Up
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Page 5 (Oldest)                         │
│ DB: Messages 7-1 (DESC)                 │
│ Display: Messages 1→7 (ASC)             │ ← Scroll Up
│ Shows: Oldest 7 messages                │
└─────────────────────────────────────────┘
```

### User Experience Flow

1. **Initial Load (Page 1)**:
   - Fetch most recent 20 messages
   - Messages display chronologically (oldest at top, newest at bottom)
   - Scroll position at bottom

2. **Scroll Up to Load Older Messages**:
   - Detect scroll to top
   - Increment page number (page++)
   - Fetch next 20 older messages
   - Prepend to existing messages
   - Maintain scroll position

3. **Continue Until Oldest Messages**:
   - Keep loading until `page === totalPages`
   - Show "No more messages" when all loaded

---

## 🎨 UI/UX Best Practices

### ✅ DO's

1. **Initial Load**: Always scroll to bottom after loading page 1
2. **Infinite Scroll**: Load older messages when scrolled to top
3. **Loading Indicators**: Show spinner when loading older messages
4. **Maintain Position**: Keep scroll position when prepending messages
5. **Error Handling**: Show error message if load fails
6. **Pull to Refresh**: Reload page 1 to get new messages

### ❌ DON'Ts

1. **Don't** reverse message order in frontend (already correct from API)
2. **Don't** use page for newest messages (page=1 is always most recent)
3. **Don't** reset scroll position when loading older messages
4. **Don't** load all messages at once (use pagination)

---

## 🔐 Authorization Notes

- **JWT Token Required**: All requests must include valid Bearer token
- **Participant Check**: Only message participants can access conversation
- **Three-Way Support**: In dealer conversations, all three parties see all messages
- **Admin Override**: Admins can view all conversations

---

## 🎯 Common Use Cases

### Load Most Recent Messages
```http
GET /api/v1/sponsorship/conversations?fromUserId=10&toUserId=20&plantAnalysisId=456&page=1&pageSize=20
```

### Load Next Page (Older Messages)
```http
GET /api/v1/sponsorship/conversations?fromUserId=10&toUserId=20&plantAnalysisId=456&page=2&pageSize=20
```

### Larger Page Size
```http
GET /api/v1/sponsorship/conversations?fromUserId=10&toUserId=20&plantAnalysisId=456&page=1&pageSize=50
```

### Check for New Messages (Polling)
```javascript
// Poll page 1 every 30 seconds for new messages
setInterval(async () => {
  const response = await loadMessages(1);
  if (response.data.length > currentMessageCount) {
    // New messages available, refresh
    refreshMessages();
  }
}, 30000);
```

---

## 🐛 Troubleshooting

### Messages Display in Wrong Order
- ✅ **Solution**: Don't reverse the array in frontend, API already returns correct order

### Can't Load Older Messages
- ✅ **Check**: `page < totalPages` before loading
- ✅ **Check**: Not already loading (`isLoading === false`)

### Scroll Position Jumps
- ✅ **Solution**: Calculate and maintain scroll position when prepending messages
- ✅ **Formula**: `newScrollTop = newScrollHeight - previousScrollHeight`

### Attachments Not Loading (CORS Error)
- ✅ **Solution**: Ensure JWT token is included in request
- ✅ **Note**: Files are proxied through API with proper CORS headers

### Voice Messages Not Playing
- ✅ **Check**: `voiceMessageUrl` is not null
- ✅ **Check**: Audio player supports `.m4a` format
- ✅ **Check**: JWT token included in audio request

---

## 📚 Related Documentation

- [Message Sending API](./message-sending-guide.md)
- [File Upload Guide](./file-upload-guide.md)
- [Voice Message Recording](./voice-message-guide.md)
- [Real-time Updates (SignalR)](./signalr-integration-guide.md)

---

## 🆘 Support

If you encounter issues:
1. Check JWT token validity
2. Verify participant authorization
3. Inspect network requests for error details
4. Review API logs for detailed error messages

**Last Updated**: 2024-01-15
**API Version**: v1
**Endpoint Status**: ✅ Production Ready
