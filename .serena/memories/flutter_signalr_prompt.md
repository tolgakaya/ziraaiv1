# Flutter SignalR Integration - AI Agent Prompt

## Context
Backend SignalR real-time notification system has been implemented and deployed to staging. Flutter mobile app needs SignalR client integration for receiving plant analysis completion notifications.

## AI Agent Prompt

```
ZiraAI Flutter mobile uygulamasına SignalR real-time notification sistemi entegre etmeni istiyorum.

Backend'de SignalR implementasyonu tamamlandı ve staging'de deploy edildi. Şimdi Flutter tarafında entegrasyonu yapman gerekiyor.

Detaylı teknik dokümantasyon: 
claudedocs/SIGNALR_FLUTTER_INTEGRATION_GUIDE.md dosyasını oku ve adım adım implement et.

Backend API bilgileri:
- Staging Hub URL: wss://ziraai-api-sit.up.railway.app/hubs/plantanalysis
- Authentication: JWT Bearer token (query string: access_token)
- Main Event: ReceiveAnalysisCompleted (plant analysis tamamlandığında real-time bildirim)
- Failed Event: ReceiveAnalysisFailed (analiz başarısız olursa)

Yapılacaklar:
1. signalr_netcore package ekle (version: ^1.3.7)
2. SignalRService singleton class oluştur
3. JWT token ile authentication entegrasyonu
4. ReceiveAnalysisCompleted event'ini dinle ve UI'da göster
5. App lifecycle management (background/foreground)
6. Error handling ve auto-reconnect
7. Test senaryoları

Dokümandaki tüm kod örnekleri ve best practice'ler mevcut. Mevcut authentication ve navigation yapısına uygun şekilde entegre et.

Test için manuel notification gönderme komutu da dokümanda mevcut.
```

## Implementation Checklist

### Phase 1: Setup
- [ ] Add `signalr_netcore: ^1.3.7` to pubspec.yaml
- [ ] Create `lib/services/signalr_service.dart`
- [ ] Create `lib/models/plant_analysis_notification.dart`

### Phase 2: Core Implementation
- [ ] Implement SignalRService singleton with connection management
- [ ] Register event handlers (ReceiveAnalysisCompleted, ReceiveAnalysisFailed)
- [ ] Integrate JWT authentication from existing auth service
- [ ] Add auto-reconnect with exponential backoff

### Phase 3: UI Integration
- [ ] Add notification dialog/snackbar on ReceiveAnalysisCompleted
- [ ] Implement deep link navigation to analysis detail
- [ ] Refresh analysis list on notification received
- [ ] Handle ReceiveAnalysisFailed with error display

### Phase 4: Lifecycle Management
- [ ] Initialize SignalR on user login
- [ ] Disconnect SignalR on user logout
- [ ] Handle app background/foreground state changes
- [ ] Add WidgetsBindingObserver in main app

### Phase 5: Testing
- [ ] Unit test: Connection establishment and ping
- [ ] Manual test: Send test notification via curl, verify Flutter receives it
- [ ] E2E test: Create async plant analysis, verify notification flow
- [ ] Test auto-reconnect on network interruption

### Phase 6: Error Handling
- [ ] Handle 401 Unauthorized (token refresh)
- [ ] Handle WebSocket connection failures
- [ ] Handle network connectivity issues
- [ ] Add debug logging for troubleshooting

## Test Commands

### Manual Notification Test
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

### Health Check
```bash
curl https://ziraai-api-sit.up.railway.app/api/internal/signalr/health
```

## Key Technical Details

### SignalR Hub Configuration
- **URL**: `https://ziraai-api-sit.up.railway.app/hubs/plantanalysis`
- **Authentication**: JWT via `accessTokenFactory`
- **Transport**: WebSockets (automatic negotiation)
- **Reconnect**: [0, 2000, 5000, 10000, 30000] ms intervals

### Event Payload Structure
```dart
{
  "analysisId": int,
  "userId": int,
  "status": string,
  "completedAt": DateTime,
  "cropType": string?,
  "primaryConcern": string?,
  "overallHealthScore": int?,
  "imageUrl": string?,
  "deepLink": string?,
  "sponsorId": string?,
  "message": string?
}
```

### Deep Link Format
```
app://analysis/[analysisId]
```

## Success Criteria

1. ✅ SignalR connection established on user login
2. ✅ Real-time notifications displayed when analysis completes
3. ✅ Navigation to analysis detail works via deep link
4. ✅ Auto-reconnect works on network interruption
5. ✅ Connection closed on user logout
6. ✅ No memory leaks or background battery drain
7. ✅ Error handling for all failure scenarios

## Documentation Reference

All code examples, best practices, and troubleshooting guides are in:
`claudedocs/SIGNALR_FLUTTER_INTEGRATION_GUIDE.md`

Read this file first before implementation.