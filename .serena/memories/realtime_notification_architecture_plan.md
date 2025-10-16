# Realtime Plant Analysis Notification System - Architecture & Implementation Plan

## Project Overview
**Feature**: Realtime notification system for async plant analysis completion  
**Branch**: `feature/realtime-analyses-notification-on-app`  
**Base Branch**: `development`  
**Target Platforms**: Flutter Mobile App + Web App (Angular/React)  
**Date Created**: 2025-09-30  
**Status**: Planning Phase - Not Started

---

## Business Requirements

### Primary Goal
Notify users in real-time when their asynchronous plant analysis is completed, providing instant feedback and improving user experience.

### Use Cases

#### Use Case 1: Active User (App Foreground)
- User uploads plant image for analysis
- User waits on analysis detail/loading screen
- Analysis completes (2-5 minutes)
- **Expected**: Instant in-app notification + UI update
- **Solution**: SignalR realtime push

#### Use Case 2: Background User (App Backgrounded)
- User uploads plant image
- User switches to another app
- Analysis completes
- **Expected**: Local notification banner
- **Solution**: SignalR + Flutter local notifications

#### Use Case 3: Web User (Browser Active)
- User uploads analysis via web app
- User browses other pages/tabs
- Analysis completes
- **Expected**: Toast notification + badge update
- **Solution**: SignalR + Web Notifications API

#### Use Case 4: Offline User (Future Enhancement - Phase 2)
- User uploads analysis
- User closes app completely
- Analysis completes
- **Expected**: System push notification
- **Solution**: FCM (Firebase Cloud Messaging)

---

## Technical Solution: SignalR (Phase 1 MVP)

### Why SignalR?

**Advantages:**
✅ **Real-time**: Instant notification via WebSocket (no polling)  
✅ **Bidirectional**: Two-way communication for future features  
✅ **Native Support**: ASP.NET Core built-in integration  
✅ **Cross-Platform**: Flutter (`signalr_netcore`) + Web (`@microsoft/signalr`)  
✅ **Battery Efficient**: Persistent connection, 90% less battery than polling  
✅ **Scalable**: Redis backplane for multi-instance deployment  
✅ **Secure**: JWT authentication integration  
✅ **Connection Management**: Auto-reconnection, heartbeat, lifecycle events  

**Disadvantages:**
❌ App closed = No notification (mitigated by Phase 2 FCM)  
⚠️ Flutter package community-maintained (but stable: 50K+ downloads, actively maintained)

**Decision**: SignalR optimal for MVP because:
1. Users typically wait on app during 2-5 minute analysis
2. Professional, production-grade solution
3. Aligns with existing tech stack (RabbitMQ, Redis, PostgreSQL)
4. Future-proof for bidirectional features (chat, live dashboards)

---

## Architecture Design

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────┐              ┌──────────────────┐        │
│  │  Flutter Mobile  │              │    Web App       │        │
│  │  (iOS/Android)   │              │  (Angular/React) │        │
│  │                  │              │                  │        │
│  │  signalr_netcore │              │ @microsoft/      │        │
│  │  package         │              │ signalr          │        │
│  └────────┬─────────┘              └────────┬─────────┘        │
│           │                                 │                   │
└───────────┼─────────────────────────────────┼───────────────────┘
            │                                 │
            │    WebSocket Connection (wss://)│
            │                                 │
┌───────────┼─────────────────────────────────┼───────────────────┐
│           │         API LAYER               │                   │
├───────────┴─────────────────────────────────┴───────────────────┤
│                                                                  │
│           ┌──────────────────────────────────┐                 │
│           │   SignalR Hub                    │                 │
│           │   /hubs/plantanalysis            │                 │
│           │                                  │                 │
│           │   - PlantAnalysisHub.cs          │                 │
│           │   - JWT Authentication           │                 │
│           │   - User-specific broadcasting   │                 │
│           └─────────────┬────────────────────┘                 │
│                         │                                       │
└─────────────────────────┼───────────────────────────────────────┘
                          │
┌─────────────────────────┼───────────────────────────────────────┐
│                         │    BUSINESS LAYER                     │
├─────────────────────────┴───────────────────────────────────────┤
│                                                                  │
│  ┌───────────────────────────────────────────────────────┐     │
│  │   PlantAnalysisWorkerService (RabbitMQ Consumer)      │     │
│  │                                                        │     │
│  │   Flow:                                                │     │
│  │   1. Consume message from RabbitMQ                     │     │
│  │   2. Send to N8N for AI analysis                       │     │
│  │   3. Receive N8N response (2-5 min)                    │     │
│  │   4. Update database (Status: Completed)               │     │
│  │   5. 🆕 Trigger SignalR notification via IHubContext   │     │
│  │                                                        │     │
│  │   _hubContext.Clients.User(userId.ToString())          │     │
│  │     .SendAsync("AnalysisCompleted", notificationDto);  │     │
│  └────────────────────────────────────────────────────────┘     │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
                          │
┌─────────────────────────┼───────────────────────────────────────┐
│                         │    DATA LAYER                         │
├─────────────────────────┴───────────────────────────────────────┤
│                                                                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐     │
│  │  PostgreSQL  │    │   RabbitMQ   │    │    Redis     │     │
│  │              │    │              │    │              │     │
│  │  Analysis    │    │   Async      │    │  Connection  │     │
│  │  Storage     │    │   Queue      │    │  Mapping     │     │
│  └──────────────┘    └──────────────┘    └──────────────┘     │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Message Flow Sequence

```
1. User Action: Upload Plant Image
   └─> POST /api/v1/plantanalyses/async
       └─> Return: { analysisId: 123, status: "Processing" }
       └─> Publish message to RabbitMQ

2. Background Processing
   └─> PlantAnalysisWorkerService consumes message
       └─> Call N8N webhook with image
           └─> N8N AI Analysis (2-5 minutes)
               └─> Return analysis results

3. Database Update
   └─> Update PlantAnalysis table
       └─> Status: "Completed"
       └─> Store analysis results (JSONB)

4. 🆕 SignalR Notification Trigger
   └─> Worker injects IHubContext<PlantAnalysisHub>
       └─> _hubContext.Clients.User(userId).SendAsync("AnalysisCompleted", data)
           └─> SignalR Hub broadcasts to connected clients

5. Client Notification
   ├─> Flutter Client
   │   └─> Event listener receives "AnalysisCompleted"
   │       └─> Update UI state (Provider/Bloc)
   │       └─> Show local notification (if backgrounded)
   │       └─> Auto-navigate to results screen
   │
   └─> Web Client
       └─> Event listener receives "AnalysisCompleted"
           └─> Show toast notification
           └─> Update analysis list
           └─> Badge count update
```

---

## Backend Implementation

### 1. SignalR Hub

**File**: `WebAPI/Hubs/PlantAnalysisHub.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace WebAPI.Hubs
{
    [Authorize] // JWT authentication required
    public class PlantAnalysisHub : Hub
    {
        private readonly ILogger<PlantAnalysisHub> _logger;

        public PlantAnalysisHub(ILogger<PlantAnalysisHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("userId")?.Value;
            _logger.LogInformation($"User {userId} connected with ConnectionId: {Context.ConnectionId}");
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst("userId")?.Value;
            _logger.LogInformation($"User {userId} disconnected from ConnectionId: {Context.ConnectionId}");
            
            await base.OnDisconnectedAsync(exception);
        }

        // Client can call this to test connection
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
        }
    }
}
```

**Events Broadcast by Server:**
- `AnalysisCompleted`: Analysis finished successfully
- `AnalysisFailed`: Analysis failed with error
- `AnalysisProgress`: Progress updates (future enhancement)

### 2. Notification Service

**File**: `Business/Services/Notification/PlantAnalysisNotificationService.cs`

```csharp
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;
using Entities.Dtos;

namespace Business.Services.Notification
{
    public interface IPlantAnalysisNotificationService
    {
        Task NotifyAnalysisCompleted(int userId, PlantAnalysisNotificationDto notification);
        Task NotifyAnalysisFailed(int userId, string analysisId, string errorMessage);
    }

    public class PlantAnalysisNotificationService : IPlantAnalysisNotificationService
    {
        private readonly IHubContext<PlantAnalysisHub> _hubContext;
        private readonly ILogger<PlantAnalysisNotificationService> _logger;

        public PlantAnalysisNotificationService(
            IHubContext<PlantAnalysisHub> hubContext,
            ILogger<PlantAnalysisNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyAnalysisCompleted(int userId, PlantAnalysisNotificationDto notification)
        {
            try
            {
                // Send to specific user (all their connected devices)
                await _hubContext.Clients
                    .User(userId.ToString())
                    .SendAsync("AnalysisCompleted", notification);

                _logger.LogInformation(
                    $"Sent AnalysisCompleted notification to user {userId} for analysis {notification.AnalysisId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    $"Failed to send notification to user {userId} for analysis {notification.AnalysisId}");
                // Don't throw - notification failure shouldn't break analysis flow
            }
        }

        public async Task NotifyAnalysisFailed(int userId, string analysisId, string errorMessage)
        {
            try
            {
                await _hubContext.Clients
                    .User(userId.ToString())
                    .SendAsync("AnalysisFailed", new { analysisId, errorMessage, timestamp = DateTime.UtcNow });

                _logger.LogInformation(
                    $"Sent AnalysisFailed notification to user {userId} for analysis {analysisId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    $"Failed to send failure notification to user {userId} for analysis {analysisId}");
            }
        }
    }
}
```

### 3. Notification DTO

**File**: `Entities/Dtos/PlantAnalysisNotificationDto.cs`

```csharp
using System;

namespace Entities.Dtos
{
    public class PlantAnalysisNotificationDto
    {
        public int AnalysisId { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; } // "Completed"
        public DateTime CompletedAt { get; set; }
        
        // Preview data for notification
        public string CropType { get; set; }
        public string PrimaryConcern { get; set; }
        public int? OverallHealthScore { get; set; }
        public string ImageUrl { get; set; }
        
        // Deep link for navigation
        public string DeepLink { get; set; } // "app://analysis/123"
    }
}
```

### 4. Worker Service Integration

**File**: `PlantAnalysisWorkerService/Worker.cs`

```csharp
// Add to existing Worker.cs

private readonly IPlantAnalysisNotificationService _notificationService;

// Inject in constructor
public Worker(
    ILogger<Worker> logger,
    // ... existing dependencies
    IPlantAnalysisNotificationService notificationService)
{
    _notificationService = notificationService;
    // ...
}

// In ProcessAnalysisAsync method, after saving to database:
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    // ... existing RabbitMQ consumer code
    
    // After analysis completed and saved to DB:
    if (analysis.AnalysisStatus == "Completed")
    {
        var notificationDto = new PlantAnalysisNotificationDto
        {
            AnalysisId = analysis.Id,
            UserId = analysis.UserId.Value,
            Status = "Completed",
            CompletedAt = DateTime.UtcNow,
            CropType = analysis.CropType,
            PrimaryConcern = analysis.PrimaryConcern,
            OverallHealthScore = analysis.OverallHealthScore,
            ImageUrl = analysis.ImageUrl,
            DeepLink = $"app://analysis/{analysis.Id}"
        };

        // 🆕 Send SignalR notification
        await _notificationService.NotifyAnalysisCompleted(
            analysis.UserId.Value, 
            notificationDto);
    }
}
```

### 5. Startup Configuration

**File**: `WebAPI/Startup.cs`

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ... existing services
    
    // 🆕 Add SignalR with Redis backplane (for scaled deployment)
    services.AddSignalR()
        .AddStackExchangeRedis(Configuration.GetConnectionString("Redis"), options =>
        {
            options.Configuration.ChannelPrefix = "ZiraAI:SignalR:";
        });
    
    // Register notification service
    services.AddScoped<IPlantAnalysisNotificationService, PlantAnalysisNotificationService>();
    
    // Update CORS for SignalR
    services.AddCors(options =>
    {
        options.AddPolicy("AllowSignalR", builder =>
        {
            builder
                .WithOrigins(
                    "http://localhost:3000", // Web dev
                    "https://app.ziraai.com" // Web prod
                    // Add Flutter app origins if needed
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // Important for SignalR
        });
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ... existing middleware
    
    app.UseCors("AllowSignalR");
    
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        
        // 🆕 Map SignalR hub
        endpoints.MapHub<PlantAnalysisHub>("/hubs/plantanalysis");
    });
}
```

### 6. Configuration Settings

**File**: `WebAPI/appsettings.json`

```json
{
  "SignalR": {
    "UseRedisBackplane": true,
    "MaxConnectionsPerUser": 5,
    "ConnectionTimeout": 30,
    "KeepAliveInterval": 15
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://app.ziraai.com"
    ]
  }
}
```

---

## Flutter Implementation

### 1. Package Dependencies

**File**: `pubspec.yaml`

```yaml
dependencies:
  signalr_netcore: ^1.3.4
  flutter_local_notifications: ^17.0.0
  provider: ^6.1.1  # Or Bloc/Riverpod for state management
```

### 2. SignalR Service

**File**: `lib/services/signalr_service.dart`

```dart
import 'package:signalr_netcore/signalr_client.dart';
import 'package:flutter/foundation.dart';

class SignalRService {
  HubConnection? _hubConnection;
  final String _hubUrl = 'https://api.ziraai.com/hubs/plantanalysis';
  final String _accessToken;
  
  // Event callbacks
  Function(Map<String, dynamic>)? onAnalysisCompleted;
  Function(Map<String, dynamic>)? onAnalysisFailed;
  
  SignalRService(this._accessToken);
  
  Future<void> connect() async {
    if (_hubConnection?.state == HubConnectionState.Connected) {
      debugPrint('SignalR already connected');
      return;
    }
    
    _hubConnection = HubConnectionBuilder()
        .withUrl(
          _hubUrl,
          options: HttpConnectionOptions(
            accessTokenFactory: () => Future.value(_accessToken),
            transport: HttpTransportType.WebSockets,
            skipNegotiation: true,
          ),
        )
        .withAutomaticReconnect(
          retryDelays: [0, 2000, 5000, 10000, 30000], // Retry intervals in ms
        )
        .build();
    
    // Register event handlers
    _hubConnection!.on('AnalysisCompleted', _handleAnalysisCompleted);
    _hubConnection!.on('AnalysisFailed', _handleAnalysisFailed);
    _hubConnection!.on('Pong', (arguments) {
      debugPrint('Pong received: ${arguments?[0]}');
    });
    
    // Connection lifecycle events
    _hubConnection!.onclose((error) {
      debugPrint('SignalR connection closed: $error');
    });
    
    _hubConnection!.onreconnecting((error) {
      debugPrint('SignalR reconnecting: $error');
    });
    
    _hubConnection!.onreconnected((connectionId) {
      debugPrint('SignalR reconnected: $connectionId');
    });
    
    try {
      await _hubConnection!.start();
      debugPrint('SignalR connected successfully');
      
      // Send ping to test connection
      await ping();
    } catch (e) {
      debugPrint('SignalR connection failed: $e');
      rethrow;
    }
  }
  
  Future<void> disconnect() async {
    if (_hubConnection != null) {
      await _hubConnection!.stop();
      debugPrint('SignalR disconnected');
    }
  }
  
  Future<void> ping() async {
    if (_hubConnection?.state == HubConnectionState.Connected) {
      await _hubConnection!.invoke('Ping');
    }
  }
  
  void _handleAnalysisCompleted(List<Object?>? arguments) {
    if (arguments != null && arguments.isNotEmpty) {
      final data = arguments[0] as Map<String, dynamic>;
      debugPrint('Analysis completed: ${data['analysisId']}');
      
      onAnalysisCompleted?.call(data);
    }
  }
  
  void _handleAnalysisFailed(List<Object?>? arguments) {
    if (arguments != null && arguments.isNotEmpty) {
      final data = arguments[0] as Map<String, dynamic>;
      debugPrint('Analysis failed: ${data['analysisId']}');
      
      onAnalysisFailed?.call(data);
    }
  }
  
  bool get isConnected => 
      _hubConnection?.state == HubConnectionState.Connected;
}
```

### 3. Notification Handler

**File**: `lib/services/notification_handler.dart`

```dart
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:flutter/material.dart';

class NotificationHandler {
  static final FlutterLocalNotificationsPlugin _notifications = 
      FlutterLocalNotificationsPlugin();
  
  static Future<void> initialize() async {
    const androidSettings = AndroidInitializationSettings('@mipmap/ic_launcher');
    const iosSettings = DarwinInitializationSettings(
      requestAlertPermission: true,
      requestBadgePermission: true,
      requestSoundPermission: true,
    );
    
    const settings = InitializationSettings(
      android: androidSettings,
      iOS: iosSettings,
    );
    
    await _notifications.initialize(
      settings,
      onDidReceiveNotificationResponse: _onNotificationTapped,
    );
  }
  
  static Future<void> showAnalysisCompletedNotification(
    Map<String, dynamic> data,
  ) async {
    const androidDetails = AndroidNotificationDetails(
      'plant_analysis_channel',
      'Plant Analysis',
      channelDescription: 'Notifications for plant analysis completion',
      importance: Importance.high,
      priority: Priority.high,
      icon: '@mipmap/ic_launcher',
    );
    
    const iosDetails = DarwinNotificationDetails(
      presentAlert: true,
      presentBadge: true,
      presentSound: true,
    );
    
    const details = NotificationDetails(
      android: androidDetails,
      iOS: iosDetails,
    );
    
    await _notifications.show(
      data['analysisId'],
      '🌱 Analysis Complete!',
      'Your ${data['cropType']} analysis is ready. Health Score: ${data['overallHealthScore']}/100',
      details,
      payload: data['deepLink'], // For navigation
    );
  }
  
  static void _onNotificationTapped(NotificationResponse response) {
    if (response.payload != null) {
      // Navigate to analysis detail screen
      // Implementation depends on your navigation setup
      debugPrint('Notification tapped, navigating to: ${response.payload}');
    }
  }
}
```

### 4. State Management (Provider Example)

**File**: `lib/providers/signalr_provider.dart`

```dart
import 'package:flutter/foundation.dart';
import '../services/signalr_service.dart';
import '../services/notification_handler.dart';

class SignalRProvider with ChangeNotifier {
  SignalRService? _signalRService;
  bool _isConnected = false;
  
  bool get isConnected => _isConnected;
  
  Future<void> initialize(String accessToken) async {
    _signalRService = SignalRService(accessToken);
    
    // Register event handlers
    _signalRService!.onAnalysisCompleted = _handleAnalysisCompleted;
    _signalRService!.onAnalysisFailed = _handleAnalysisFailed;
    
    await connect();
  }
  
  Future<void> connect() async {
    try {
      await _signalRService?.connect();
      _isConnected = true;
      notifyListeners();
    } catch (e) {
      debugPrint('Failed to connect SignalR: $e');
      _isConnected = false;
      notifyListeners();
    }
  }
  
  Future<void> disconnect() async {
    await _signalRService?.disconnect();
    _isConnected = false;
    notifyListeners();
  }
  
  void _handleAnalysisCompleted(Map<String, dynamic> data) {
    // Show local notification
    NotificationHandler.showAnalysisCompletedNotification(data);
    
    // Broadcast to other providers/screens
    notifyListeners();
  }
  
  void _handleAnalysisFailed(Map<String, dynamic> data) {
    debugPrint('Analysis failed: ${data['errorMessage']}');
    notifyListeners();
  }
}
```

### 5. UI Integration

**File**: `lib/screens/analysis_detail_screen.dart`

```dart
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/signalr_provider.dart';

class AnalysisDetailScreen extends StatefulWidget {
  final int analysisId;
  
  const AnalysisDetailScreen({required this.analysisId});
  
  @override
  _AnalysisDetailScreenState createState() => _AnalysisDetailScreenState();
}

class _AnalysisDetailScreenState extends State<AnalysisDetailScreen> {
  @override
  void initState() {
    super.initState();
    
    // Listen to SignalR events
    final signalRProvider = context.read<SignalRProvider>();
    signalRProvider.addListener(_onSignalREvent);
  }
  
  @override
  void dispose() {
    context.read<SignalRProvider>().removeListener(_onSignalREvent);
    super.dispose();
  }
  
  void _onSignalREvent() {
    // Refresh analysis data when notification received
    setState(() {
      // Reload analysis from API
    });
  }
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Analysis Details')),
      body: Consumer<SignalRProvider>(
        builder: (context, signalRProvider, child) {
          return Column(
            children: [
              // Connection status indicator
              if (!signalRProvider.isConnected)
                Container(
                  color: Colors.orange,
                  padding: EdgeInsets.all(8),
                  child: Text('Reconnecting to notification service...'),
                ),
              
              // Analysis content
              Expanded(
                child: _buildAnalysisContent(),
              ),
            ],
          );
        },
      ),
    );
  }
  
  Widget _buildAnalysisContent() {
    // Your analysis UI
    return Container();
  }
}
```

---

## Web Implementation (React Example)

### 1. Package Installation

```bash
npm install @microsoft/signalr
```

### 2. SignalR Service

**File**: `src/services/signalRService.ts`

```typescript
import * as signalR from '@microsoft/signalr';

export interface AnalysisNotification {
  analysisId: number;
  userId: number;
  status: string;
  completedAt: string;
  cropType: string;
  primaryConcern: string;
  overallHealthScore: number;
  imageUrl: string;
  deepLink: string;
}

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private hubUrl = 'https://api.ziraai.com/hubs/plantanalysis';
  
  // Event handlers
  onAnalysisCompleted?: (data: AnalysisNotification) => void;
  onAnalysisFailed?: (data: any) => void;
  
  constructor(private accessToken: string) {}
  
  async connect(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      console.log('SignalR already connected');
      return;
    }
    
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => this.accessToken,
        transport: signalR.HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();
    
    // Register event handlers
    this.connection.on('AnalysisCompleted', (data: AnalysisNotification) => {
      console.log('Analysis completed:', data.analysisId);
      this.onAnalysisCompleted?.(data);
    });
    
    this.connection.on('AnalysisFailed', (data: any) => {
      console.log('Analysis failed:', data.analysisId);
      this.onAnalysisFailed?.(data);
    });
    
    this.connection.on('Pong', (timestamp: string) => {
      console.log('Pong received:', timestamp);
    });
    
    // Connection lifecycle
    this.connection.onclose((error) => {
      console.error('SignalR connection closed:', error);
    });
    
    this.connection.onreconnecting((error) => {
      console.warn('SignalR reconnecting:', error);
    });
    
    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected:', connectionId);
    });
    
    try {
      await this.connection.start();
      console.log('SignalR connected successfully');
      await this.ping();
    } catch (error) {
      console.error('SignalR connection failed:', error);
      throw error;
    }
  }
  
  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      console.log('SignalR disconnected');
    }
  }
  
  async ping(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('Ping');
    }
  }
  
  get isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

export default SignalRService;
```

### 3. React Hook

**File**: `src/hooks/useSignalR.ts`

```typescript
import { useEffect, useState, useCallback } from 'react';
import SignalRService, { AnalysisNotification } from '../services/signalRService';
import { useAuth } from '../contexts/AuthContext'; // Your auth context

export const useSignalR = () => {
  const [signalR, setSignalR] = useState<SignalRService | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const { accessToken } = useAuth();
  
  useEffect(() => {
    if (!accessToken) return;
    
    const service = new SignalRService(accessToken);
    
    service.onAnalysisCompleted = handleAnalysisCompleted;
    service.onAnalysisFailed = handleAnalysisFailed;
    
    service.connect()
      .then(() => setIsConnected(true))
      .catch(console.error);
    
    setSignalR(service);
    
    return () => {
      service.disconnect();
    };
  }, [accessToken]);
  
  const handleAnalysisCompleted = useCallback((data: AnalysisNotification) => {
    // Show toast notification
    showToast({
      title: '🌱 Analysis Complete!',
      message: `Your ${data.cropType} analysis is ready. Health Score: ${data.overallHealthScore}/100`,
      type: 'success',
      action: {
        label: 'View',
        onClick: () => window.location.href = `/analysis/${data.analysisId}`
      }
    });
    
    // Trigger global event for other components
    window.dispatchEvent(new CustomEvent('analysisCompleted', { detail: data }));
  }, []);
  
  const handleAnalysisFailed = useCallback((data: any) => {
    showToast({
      title: 'Analysis Failed',
      message: data.errorMessage,
      type: 'error',
    });
  }, []);
  
  return { signalR, isConnected };
};

// Toast notification function (use your preferred toast library)
function showToast(config: any) {
  // Implementation with react-toastify, sonner, etc.
}
```

### 4. Component Integration

**File**: `src/components/AnalysisDetail.tsx`

```typescript
import React, { useEffect, useState } from 'react';
import { useSignalR } from '../hooks/useSignalR';

interface Props {
  analysisId: number;
}

export const AnalysisDetail: React.FC<Props> = ({ analysisId }) => {
  const { isConnected } = useSignalR();
  const [analysis, setAnalysis] = useState(null);
  
  useEffect(() => {
    // Listen for analysis completion event
    const handleAnalysisCompleted = (event: CustomEvent) => {
      if (event.detail.analysisId === analysisId) {
        // Refresh analysis data
        loadAnalysis();
      }
    };
    
    window.addEventListener('analysisCompleted', handleAnalysisCompleted as EventListener);
    
    return () => {
      window.removeEventListener('analysisCompleted', handleAnalysisCompleted as EventListener);
    };
  }, [analysisId]);
  
  const loadAnalysis = async () => {
    // Fetch analysis from API
  };
  
  return (
    <div>
      {/* Connection status */}
      {!isConnected && (
        <div className="alert alert-warning">
          Reconnecting to notification service...
        </div>
      )}
      
      {/* Analysis content */}
      <div className="analysis-content">
        {/* Your analysis UI */}
      </div>
    </div>
  );
};
```

---

## Testing Strategy

### Backend Tests

**File**: `Tests/Business/Services/PlantAnalysisNotificationServiceTests.cs`

```csharp
using Xunit;
using Moq;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;
using Business.Services.Notification;

public class PlantAnalysisNotificationServiceTests
{
    [Fact]
    public async Task NotifyAnalysisCompleted_SendsToCorrectUser()
    {
        // Arrange
        var mockHubContext = new Mock<IHubContext<PlantAnalysisHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        
        mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);
        mockClients.Setup(x => x.User(It.IsAny<string>())).Returns(mockClientProxy.Object);
        
        var service = new PlantAnalysisNotificationService(
            mockHubContext.Object,
            Mock.Of<ILogger<PlantAnalysisNotificationService>>()
        );
        
        var notification = new PlantAnalysisNotificationDto
        {
            AnalysisId = 123,
            UserId = 456,
            Status = "Completed"
        };
        
        // Act
        await service.NotifyAnalysisCompleted(456, notification);
        
        // Assert
        mockClients.Verify(x => x.User("456"), Times.Once);
        mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "AnalysisCompleted",
                It.Is<object[]>(o => o.Length == 1),
                default
            ),
            Times.Once
        );
    }
}
```

### Integration Tests

**Manual Testing Checklist:**

1. **Backend SignalR Hub**
   - [ ] Hub accessible at `/hubs/plantanalysis`
   - [ ] JWT authentication required
   - [ ] Unauthorized users rejected
   - [ ] Connection/disconnection logged

2. **Worker Service Integration**
   - [ ] Analysis completes → notification sent
   - [ ] Notification contains correct data
   - [ ] Worker doesn't crash if notification fails
   - [ ] Multiple users can receive notifications

3. **Flutter Client**
   - [ ] Connection established with valid token
   - [ ] AnalysisCompleted event received
   - [ ] UI updates on notification
   - [ ] Local notification shown (background)
   - [ ] Deep link navigation works
   - [ ] Reconnection after network loss

4. **Web Client**
   - [ ] Connection established
   - [ ] Toast notification shown
   - [ ] Analysis list auto-refreshes
   - [ ] Works across multiple tabs
   - [ ] Reconnection handling

5. **Load Testing**
   - [ ] 100+ concurrent connections
   - [ ] 1000+ notifications per minute
   - [ ] Memory usage stable
   - [ ] No connection leaks

---

## Deployment Plan

### Phase 1: Development Environment

**Week 1: Backend Implementation**
- Day 1-2: SignalR Hub + Notification Service
- Day 3: Worker Service integration
- Day 4: Unit tests + Integration tests
- Day 5: Local testing with Postman/SignalR test client

**Week 2: Client Implementation**
- Day 1-2: Flutter SignalR integration
- Day 3: Web SignalR integration
- Day 4-5: End-to-end testing

### Phase 2: Staging Deployment

**Prerequisites:**
- [ ] All unit tests passing
- [ ] Integration tests passing
- [ ] Code review approved
- [ ] Redis available in staging

**Steps:**
1. Deploy backend changes to staging
2. Update staging app configuration
3. Deploy Flutter app to TestFlight/Internal Testing
4. Deploy web app to staging environment
5. Run smoke tests
6. User acceptance testing (1 week)

### Phase 3: Production Rollout

**Strategy**: Gradual rollout with feature flag

**Week 1: 10% Rollout**
- Enable for 10% of users
- Monitor metrics (connection success rate, notification delivery)
- Check error logs
- Gather user feedback

**Week 2: 50% Rollout**
- Expand to 50% if no issues
- Continue monitoring

**Week 3: 100% Rollout**
- Full deployment
- Remove feature flag

**Rollback Plan:**
- Feature flag can disable SignalR instantly
- Fallback: Users manually refresh for updates
- Database/API unchanged, safe to rollback

---

## Monitoring & Observability

### Metrics to Track

**Backend (Application Insights / Grafana):**
- SignalR connection count (current)
- Connection success/failure rate
- Notification delivery rate
- Average notification latency
- Hub method execution time
- Redis backplane performance

**Client (Firebase Analytics / Custom):**
- SignalR connection attempts
- Connection success rate
- Reconnection frequency
- Notification received count
- Time from analysis completion to notification

### Logging

**Backend Log Events:**
```csharp
// Connection
[Info] User {userId} connected with ConnectionId: {connectionId}
[Info] User {userId} disconnected from ConnectionId: {connectionId}

// Notifications
[Info] Sent AnalysisCompleted notification to user {userId} for analysis {analysisId}
[Error] Failed to send notification to user {userId}: {exception}

// Hub invocations
[Info] Hub method {methodName} invoked by user {userId}
```

**Flutter Log Events:**
```dart
debugPrint('SignalR connected successfully');
debugPrint('Analysis completed: ${data['analysisId']}');
debugPrint('SignalR reconnecting after network loss');
```

### Alerts

**Critical Alerts (PagerDuty / Opsgenie):**
- SignalR connection failure rate > 10%
- Notification delivery failure rate > 5%
- Hub response time > 5 seconds
- Redis backplane disconnected

**Warning Alerts (Email / Slack):**
- Connection count spike (> 2x normal)
- Reconnection rate > 20%
- Memory usage > 80%

---

## Security Considerations

### Authentication & Authorization

1. **JWT Token Validation**
   - Hub requires `[Authorize]` attribute
   - Token validated on connection
   - Token refresh handled by client

2. **User Isolation**
   - Users can only receive their own notifications
   - `Clients.User(userId)` ensures targeting
   - No group broadcasts (security risk)

3. **CORS Configuration**
   - Whitelist specific origins only
   - No `AllowAnyOrigin()` with credentials
   - Separate CORS policy for SignalR

### Data Privacy

1. **Notification Content**
   - Only summary data in notifications
   - No sensitive medical information
   - Image URLs pre-signed (S3) or public CDN

2. **Connection Metadata**
   - Log userId only, not personal data
   - ConnectionId rotates on reconnect
   - No IP address logging (GDPR)

### Infrastructure Security

1. **Redis Backplane**
   - Password-protected
   - TLS encryption in production
   - Network isolation (VPC)

2. **WebSocket Security**
   - WSS (TLS) in production
   - Origin validation
   - Rate limiting per connection

---

## Performance Optimization

### Connection Management

**Redis Backplane Benefits:**
- Horizontal scaling across multiple servers
- Shared connection state
- Message distribution to all instances

**Connection Limits:**
- Max 5 connections per user (multiple devices)
- Idle connection timeout: 30 seconds
- KeepAlive interval: 15 seconds

### Message Optimization

**Payload Size:**
- Notification DTO: ~500 bytes
- MessagePack encoding (optional): 30% smaller
- Batch notifications if needed (future)

**Delivery Guarantee:**
- At-least-once delivery
- Client ACK not required (fire-and-forget)
- Retry on network failure (client-side)

### Resource Usage

**Backend:**
- Memory per connection: ~10 KB
- 10,000 connections ≈ 100 MB RAM
- CPU usage minimal (idle connections)

**Client:**
- Battery impact: < 1% per hour
- Data usage: ~1 KB/minute (heartbeat)
- Reconnection backoff to prevent battery drain

---

## Future Enhancements (Phase 2)

### 1. Firebase Cloud Messaging (FCM)

**Purpose**: Notifications when app closed

**Implementation:**
- Backend: Send to FCM after SignalR (redundant)
- Flutter: Handle FCM background messages
- Web: Service Worker for push notifications

**Timeline**: 2-3 weeks after Phase 1 stable

### 2. Progress Updates

**Purpose**: Real-time analysis progress (0% → 100%)

**Implementation:**
- N8N webhook sends progress updates
- Worker forwards to SignalR: `AnalysisProgress` event
- Client shows progress bar

**Timeline**: 1 week

### 3. Bidirectional Features

**Use Cases:**
- In-app chat (Farmer ↔ Sponsor)
- Live Q&A during analysis
- Collaborative analysis review

**Timeline**: 4-6 weeks, separate project

### 4. Analytics Dashboard

**Purpose**: Realtime analytics for admins

**Implementation:**
- SignalR group for admin users
- Broadcast system metrics
- Live charts (Chart.js + SignalR)

**Timeline**: 2 weeks

---

## Troubleshooting Guide

### Common Issues

**Issue**: Client can't connect to SignalR  
**Solutions:**
- Check JWT token validity
- Verify CORS configuration
- Check hub endpoint URL
- Inspect browser/Flutter console logs

**Issue**: Notifications not received  
**Solutions:**
- Verify user is connected (check logs)
- Confirm userId in JWT claims
- Check Worker Service injection
- Verify Redis backplane connectivity

**Issue**: Frequent disconnections  
**Solutions:**
- Check network stability
- Adjust KeepAlive interval
- Review server load
- Check firewall/proxy settings

**Issue**: Memory leak (backend)  
**Solutions:**
- Check for lingering connections
- Verify OnDisconnectedAsync cleanup
- Monitor Redis memory usage
- Review Hub lifecycle

---

## Documentation Links

**SignalR Official Docs:**
- https://learn.microsoft.com/aspnet/core/signalr/introduction
- https://learn.microsoft.com/aspnet/core/signalr/scale

**Flutter Package:**
- https://pub.dev/packages/signalr_netcore

**Web Client:**
- https://www.npmjs.com/package/@microsoft/signalr

**Best Practices:**
- https://learn.microsoft.com/aspnet/core/signalr/security
- https://learn.microsoft.com/aspnet/core/signalr/authn-and-authz

---

## Change Log

| Date | Author | Change | Reason |
|------|--------|--------|--------|
| 2025-09-30 | System | Initial plan created | Feature kickoff |
| | | | |
| | | | |

---

## Sign-Off

**Technical Lead**: _________  
**Product Owner**: _________  
**Date**: _________

---

**Status**: ✅ Plan Approved - Ready for Implementation  
**Next Steps**: Begin Phase 1 backend implementation