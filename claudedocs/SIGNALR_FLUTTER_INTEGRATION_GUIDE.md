# SignalR Flutter Integration Guide - ZiraAI Mobile

## Genel Bakış

ZiraAI backend'inde **real-time plant analysis notification** sistemi SignalR kullanılarak implement edildi. Bu dokümanda Flutter mobile uygulamasına SignalR entegrasyonunun nasıl yapılacağı detaylı olarak açıklanmaktadır.

---

## Backend API Architecture

### SignalR Hub Endpoint
```
wss://ziraai-api-sit.up.railway.app/hubs/plantanalysis
```

**Authentication**: JWT Bearer token required
**Protocol**: WebSocket (SignalR Core)

### Hub Methods

#### 1. Client → Server Methods

**Ping Test**
```dart
await hubConnection.invoke('Ping');
// Response: Pong event with server timestamp
```

**Subscribe to Specific Analysis**
```dart
await hubConnection.invoke('SubscribeToAnalysis', args: [analysisId]);
```

**Unsubscribe from Analysis**
```dart
await hubConnection.invoke('UnsubscribeFromAnalysis', args: [analysisId]);
```

#### 2. Server → Client Events

**ReceiveAnalysisCompleted** (Main Event)
```json
{
  "analysisId": 123,
  "userId": 456,
  "status": "Completed",
  "completedAt": "2025-09-30T10:30:00Z",
  "cropType": "Tomato",
  "primaryConcern": "Leaf Blight",
  "overallHealthScore": 75,
  "imageUrl": "https://storage.com/image.jpg",
  "deepLink": "app://analysis/123",
  "sponsorId": "SPONSOR123",
  "message": "Analysis completed successfully"
}
```

**ReceiveAnalysisFailed**
```json
{
  "analysisId": 123,
  "userId": 456,
  "errorMessage": "Image processing failed"
}
```

---

## Flutter Implementation Steps

### 1. Dependencies

`pubspec.yaml` dosyasına ekleyin:

```yaml
dependencies:
  signalr_netcore: ^1.3.7  # SignalR client for Flutter
  # Mevcut dependencies
  dio: ^5.0.0  # HTTP client (zaten var olabilir)
```

### 2. SignalR Service Implementation

**`lib/services/signalr_service.dart`** oluşturun:

```dart
import 'package:signalr_netcore/signalr_client.dart';
import 'dart:developer' as developer;

class SignalRService {
  late HubConnection _hubConnection;
  bool _isConnected = false;

  // Event callbacks
  Function(Map<String, dynamic>)? onAnalysisCompleted;
  Function(int analysisId, String error)? onAnalysisFailed;

  // Singleton pattern
  static final SignalRService _instance = SignalRService._internal();
  factory SignalRService() => _instance;
  SignalRService._internal();

  /// Initialize SignalR connection
  Future<void> initialize(String jwtToken) async {
    if (_isConnected) {
      developer.log('SignalR already connected', name: 'SignalRService');
      return;
    }

    try {
      // Hub connection builder
      _hubConnection = HubConnectionBuilder()
          .withUrl(
            'https://ziraai-api-sit.up.railway.app/hubs/plantanalysis',
            options: HttpConnectionOptions(
              accessTokenFactory: () async => jwtToken,
              logMessageContent: true,
              skipNegotiation: false,
              transport: HttpTransportType.WebSockets,
            ),
          )
          .withAutomaticReconnect(
            retryDelays: [0, 2000, 5000, 10000, 30000], // Reconnect intervals
          )
          .configureLogging(LogLevel.information)
          .build();

      // Register event handlers
      _registerEventHandlers();

      // Connection lifecycle handlers
      _hubConnection.onclose((error) {
        _isConnected = false;
        developer.log(
          'SignalR connection closed: $error',
          name: 'SignalRService',
          error: error,
        );
      });

      _hubConnection.onreconnecting((error) {
        developer.log(
          'SignalR reconnecting...',
          name: 'SignalRService',
        );
      });

      _hubConnection.onreconnected((connectionId) {
        _isConnected = true;
        developer.log(
          'SignalR reconnected: $connectionId',
          name: 'SignalRService',
        );
      });

      // Start connection
      await _hubConnection.start();
      _isConnected = true;

      developer.log(
        '✅ SignalR connected successfully',
        name: 'SignalRService',
      );

      // Test ping
      await ping();

    } catch (e) {
      developer.log(
        '❌ SignalR connection failed: $e',
        name: 'SignalRService',
        error: e,
      );
      rethrow;
    }
  }

  /// Register server → client event handlers
  void _registerEventHandlers() {
    // Analysis completed event
    _hubConnection.on('ReceiveAnalysisCompleted', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final notification = arguments[0] as Map<String, dynamic>;
        developer.log(
          '📨 Analysis completed notification received: ${notification['analysisId']}',
          name: 'SignalRService',
        );

        onAnalysisCompleted?.call(notification);
      }
    });

    // Analysis failed event
    _hubConnection.on('ReceiveAnalysisFailed', (arguments) {
      if (arguments != null && arguments.length >= 2) {
        final analysisId = arguments[0] as int;
        final errorMessage = arguments[1] as String;

        developer.log(
          '❌ Analysis failed notification: $analysisId - $errorMessage',
          name: 'SignalRService',
        );

        onAnalysisFailed?.call(analysisId, errorMessage);
      }
    });

    // Pong response
    _hubConnection.on('Pong', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final timestamp = arguments[0];
        developer.log('🏓 Pong received: $timestamp', name: 'SignalRService');
      }
    });
  }

  /// Test connection with ping
  Future<void> ping() async {
    if (!_isConnected) {
      throw Exception('SignalR not connected');
    }

    await _hubConnection.invoke('Ping');
  }

  /// Subscribe to specific analysis updates
  Future<void> subscribeToAnalysis(int analysisId) async {
    if (!_isConnected) {
      throw Exception('SignalR not connected');
    }

    await _hubConnection.invoke('SubscribeToAnalysis', args: [analysisId]);
    developer.log(
      'Subscribed to analysis: $analysisId',
      name: 'SignalRService',
    );
  }

  /// Unsubscribe from analysis
  Future<void> unsubscribeFromAnalysis(int analysisId) async {
    if (!_isConnected) {
      throw Exception('SignalR not connected');
    }

    await _hubConnection.invoke('UnsubscribeFromAnalysis', args: [analysisId]);
    developer.log(
      'Unsubscribed from analysis: $analysisId',
      name: 'SignalRService',
    );
  }

  /// Disconnect from SignalR
  Future<void> disconnect() async {
    if (!_isConnected) return;

    await _hubConnection.stop();
    _isConnected = false;

    developer.log('SignalR disconnected', name: 'SignalRService');
  }

  /// Connection status
  bool get isConnected => _isConnected;
}
```

### 3. Authentication Integration

JWT token'ı SignalR'a sağlamanız gerekiyor. Mevcut auth service'inizle entegre edin:

```dart
// Example: After login success
final authToken = await authService.login(email, password);

// Initialize SignalR with JWT
final signalR = SignalRService();
await signalR.initialize(authToken.accessToken);
```

### 4. Notification Model

**`lib/models/plant_analysis_notification.dart`** oluşturun:

```dart
class PlantAnalysisNotification {
  final int analysisId;
  final int userId;
  final String status;
  final DateTime completedAt;
  final String? cropType;
  final String? primaryConcern;
  final int? overallHealthScore;
  final String? imageUrl;
  final String? deepLink;
  final String? sponsorId;
  final String? message;

  PlantAnalysisNotification({
    required this.analysisId,
    required this.userId,
    required this.status,
    required this.completedAt,
    this.cropType,
    this.primaryConcern,
    this.overallHealthScore,
    this.imageUrl,
    this.deepLink,
    this.sponsorId,
    this.message,
  });

  factory PlantAnalysisNotification.fromJson(Map<String, dynamic> json) {
    return PlantAnalysisNotification(
      analysisId: json['analysisId'] as int,
      userId: json['userId'] as int,
      status: json['status'] as String,
      completedAt: DateTime.parse(json['completedAt'] as String),
      cropType: json['cropType'] as String?,
      primaryConcern: json['primaryConcern'] as String?,
      overallHealthScore: json['overallHealthScore'] as int?,
      imageUrl: json['imageUrl'] as String?,
      deepLink: json['deepLink'] as String?,
      sponsorId: json['sponsorId'] as String?,
      message: json['message'] as String?,
    );
  }
}
```

### 5. UI Integration Example

**Notification Handler in Main Screen:**

```dart
class PlantAnalysisScreen extends StatefulWidget {
  @override
  _PlantAnalysisScreenState createState() => _PlantAnalysisScreenState();
}

class _PlantAnalysisScreenState extends State<PlantAnalysisScreen> {
  final signalR = SignalRService();

  @override
  void initState() {
    super.initState();
    _setupSignalRHandlers();
  }

  void _setupSignalRHandlers() {
    // Handle analysis completed
    signalR.onAnalysisCompleted = (notificationData) {
      final notification = PlantAnalysisNotification.fromJson(notificationData);

      // Show notification to user
      _showAnalysisCompletedDialog(notification);

      // Refresh analysis list
      _refreshAnalysisList();

      // Navigate to detail if needed
      if (notification.deepLink != null) {
        _handleDeepLink(notification.deepLink!);
      }
    };

    // Handle analysis failed
    signalR.onAnalysisFailed = (analysisId, errorMessage) {
      _showAnalysisFailedDialog(analysisId, errorMessage);
    };
  }

  void _showAnalysisCompletedDialog(PlantAnalysisNotification notification) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Analysis Completed'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Crop: ${notification.cropType ?? "Unknown"}'),
            Text('Health Score: ${notification.overallHealthScore ?? "N/A"}'),
            if (notification.primaryConcern != null)
              Text('Concern: ${notification.primaryConcern}'),
            if (notification.message != null)
              Text(notification.message!),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.pop(context);
              // Navigate to detail
              Navigator.pushNamed(
                context,
                '/analysis-detail',
                arguments: notification.analysisId,
              );
            },
            child: Text('View Details'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: Text('Close'),
          ),
        ],
      ),
    );
  }

  void _showAnalysisFailedDialog(int analysisId, String error) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Analysis Failed'),
        content: Text('Analysis #$analysisId failed: $error'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: Text('OK'),
          ),
        ],
      ),
    );
  }

  void _handleDeepLink(String deepLink) {
    // Parse deep link: app://analysis/123
    final uri = Uri.parse(deepLink);
    if (uri.scheme == 'app' && uri.host == 'analysis') {
      final analysisId = int.tryParse(uri.pathSegments.first);
      if (analysisId != null) {
        Navigator.pushNamed(
          context,
          '/analysis-detail',
          arguments: analysisId,
        );
      }
    }
  }

  void _refreshAnalysisList() {
    // Trigger state refresh
    setState(() {
      // Reload analysis list from API
    });
  }

  @override
  void dispose() {
    // Don't disconnect here - keep connection alive across screens
    super.dispose();
  }
}
```

### 6. App Lifecycle Management

**`lib/main.dart`** - Connection lifecycle:

```dart
class MyApp extends StatefulWidget {
  @override
  _MyAppState createState() => _MyAppState();
}

class _MyAppState extends State<MyApp> with WidgetsBindingObserver {
  final signalR = SignalRService();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    signalR.disconnect();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // Handle app background/foreground
    if (state == AppLifecycleState.paused) {
      // App went to background - SignalR will auto-reconnect
      developer.log('App paused', name: 'Lifecycle');
    } else if (state == AppLifecycleState.resumed) {
      // App came to foreground
      developer.log('App resumed', name: 'Lifecycle');

      // SignalR auto-reconnects, but you can manually check:
      if (!signalR.isConnected) {
        // Re-initialize if needed
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      // ... your app config
    );
  }
}
```

---

## Testing Steps

### 1. Unit Test - Connection Test

```dart
void main() {
  test('SignalR connection test', () async {
    final signalR = SignalRService();
    final testToken = 'YOUR_TEST_JWT_TOKEN';

    await signalR.initialize(testToken);

    expect(signalR.isConnected, true);

    await signalR.ping();

    await signalR.disconnect();
    expect(signalR.isConnected, false);
  });
}
```

### 2. Manual Test - Simulated Notification

Backend'den test notification göndermek için curl:

```bash
curl -X POST https://ziraai-api-sit.up.railway.app/api/internal/signalr/analysis-completed \
  -H "Content-Type: application/json" \
  -d '{
    "internalSecret": "ZiraAI_Internal_Secret_Staging_2025",
    "userId": YOUR_USER_ID,
    "notification": {
      "analysisId": 999,
      "userId": YOUR_USER_ID,
      "status": "Completed",
      "completedAt": "2025-09-30T10:00:00Z",
      "cropType": "Test Crop",
      "primaryConcern": "Test Issue",
      "overallHealthScore": 85,
      "imageUrl": "https://example.com/test.jpg",
      "message": "Test notification from staging"
    }
  }'
```

Flutter uygulamanızda bu notification'ı görmelisiniz.

### 3. End-to-End Test

1. Flutter app'te login olun
2. SignalR bağlantısı kuruldu mu kontrol edin (logs)
3. Plant analysis request oluşturun (async analysis)
4. Worker Service işleme alıp notification gönderince Flutter app'te pop-up görmeli

---

## Environment Configuration

### Staging
```dart
const SIGNALR_HUB_URL = 'https://ziraai-api-sit.up.railway.app/hubs/plantanalysis';
```

### Production
```dart
const SIGNALR_HUB_URL = 'https://ziraai-api-prod.up.railway.app/hubs/plantanalysis';
```

`.env` dosyası veya `flutter_config` kullanarak environment'a göre URL değiştirin.

---

## Error Handling

### Common Issues

**1. Connection Failed: 401 Unauthorized**
- JWT token eksik veya invalid
- Token expired (refresh token kullanın)

**2. Connection Failed: WebSocket Error**
- Network connectivity issues
- URL yanlış (wss:// değil https:// kullanın, SignalR negotiate eder)

**3. No Notifications Received**
- UserId JWT token'daki claim ile eşleşiyor mu kontrol edin
- Backend logs'da notification gönderildi mi kontrol edin

### Debug Logging

```dart
// Enable detailed SignalR logs
_hubConnection = HubConnectionBuilder()
    .withUrl(...)
    .configureLogging(LogLevel.debug) // Change to debug for troubleshooting
    .build();
```

---

## Performance Considerations

1. **Connection Lifecycle**: SignalR bağlantısını app açıldığında kur, kapandığında kes
2. **Auto-Reconnect**: `withAutomaticReconnect()` kullanıldı, network drops'ta otomatik yeniden bağlanır
3. **Battery Impact**: SignalR WebSocket kullanır, HTTP polling'e göre daha verimli
4. **Notification Queue**: App background'dayken gelen notification'lar için local queue implement edin

---

## Security Notes

1. **JWT Token**: Token'ı secure storage'da saklayın (`flutter_secure_storage`)
2. **HTTPS Only**: Production'da mutlaka HTTPS kullanın
3. **Token Refresh**: Token expire olmadan önce refresh edin
4. **User Validation**: Backend'de JWT claim'lerinden userId alınır, güvenli

---

## Future Enhancements

1. **Push Notifications**: App background'dayken FCM push notification entegrasyonu
2. **Notification History**: Local DB'de notification history saklama
3. **Group Subscriptions**: Sponsor'lar için tüm farmer'larını takip etme
4. **Typing Indicators**: Real-time chat için hazırlık (future feature)

---

## Support & Troubleshooting

Backend API logs: Railway Dashboard → Logs
Flutter logs: `flutter logs` veya Android Studio Logcat

Key log patterns:
- `✅ SignalR connected successfully` - Connection OK
- `📨 Analysis completed notification received` - Event received
- `❌ SignalR connection failed` - Connection issue

---

**Last Updated**: 2025-09-30
**Backend Version**: .NET 9.0 / SignalR Core
**Flutter Package**: signalr_netcore ^1.3.7