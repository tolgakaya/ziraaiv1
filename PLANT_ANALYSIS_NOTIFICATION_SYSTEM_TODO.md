# 📱 Plant Analysis Notification System - Todo Dokümantasyonu

## 🎯 Proje Genel Bakış

Bu doküman, ZiraAI uygulamasında plant analysis tamamlandığında web ve mobil uygulamalar için **in-app notification sistemi** geliştirme planını içermektedir.

### 📋 Mevcut Durum
- ✅ Mock notification endpoint (`/api/v1/notification/plant-analysis-completed`) mevcut
- ✅ Worker service'te Hangfire job scheduling hazır
- ✅ Basic DTO structure tanımlı
- ❌ Gerçek notification infrastructure eksik
- ❌ Database schema notification desteği yok
- ❌ Frontend/mobile integration yok

### 🎯 Hedef
Sadece **web ve mobil uygulama içinde gösterilecek** push notification sistemi (email/SMS değil)

---

## 📊 Database Schema Tasarımı

### 1. Notifications Table
```sql
CREATE TABLE Notifications (
    Id SERIAL PRIMARY KEY,
    UserId INT NOT NULL REFERENCES Users(UserId),
    Title VARCHAR(200) NOT NULL,
    Message TEXT NOT NULL,
    Type VARCHAR(50) NOT NULL, -- 'analysis_completed', 'analysis_failed', 'subscription_expiry' 
    RelatedEntityId VARCHAR(100), -- PlantAnalysis.AnalysisId
    RelatedEntityType VARCHAR(50), -- 'plant_analysis', 'subscription', etc.
    IsRead BOOLEAN DEFAULT FALSE,
    Priority VARCHAR(20) DEFAULT 'normal', -- 'low', 'normal', 'high', 'urgent'
    ActionUrl VARCHAR(500), -- Deep link to specific page
    IconType VARCHAR(50), -- For frontend icon selection
    CreatedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ReadDate TIMESTAMP NULL,
    ExpiryDate TIMESTAMP NULL -- Auto-cleanup old notifications
);

CREATE INDEX IX_Notifications_UserId_IsRead ON Notifications(UserId, IsRead);
CREATE INDEX IX_Notifications_CreatedDate ON Notifications(CreatedDate);
CREATE INDEX IX_Notifications_Type ON Notifications(Type);
```

### 2. NotificationPreferences Table
```sql
CREATE TABLE NotificationPreferences (
    Id SERIAL PRIMARY KEY,
    UserId INT NOT NULL REFERENCES Users(UserId) UNIQUE,
    EnableAnalysisCompleted BOOLEAN DEFAULT TRUE,
    EnableAnalysisFailed BOOLEAN DEFAULT TRUE,
    EnableSubscriptionExpiry BOOLEAN DEFAULT TRUE,
    EnableSponsorshipMessages BOOLEAN DEFAULT TRUE,
    EnableSystemAnnouncements BOOLEAN DEFAULT TRUE,
    QuietHoursStart TIME, -- User's quiet hours (e.g., 22:00)
    QuietHoursEnd TIME,   -- (e.g., 08:00)
    Timezone VARCHAR(50) DEFAULT 'Europe/Istanbul',
    CreatedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### 3. NotificationChannels Table (Future-proof)
```sql
CREATE TABLE NotificationChannels (
    Id SERIAL PRIMARY KEY,
    UserId INT NOT NULL REFERENCES Users(UserId),
    ChannelType VARCHAR(50) NOT NULL, -- 'web_push', 'mobile_push', 'websocket'
    IsEnabled BOOLEAN DEFAULT TRUE,
    DeviceToken VARCHAR(500), -- Firebase token for mobile push
    BrowserEndpoint TEXT, -- Web push endpoint
    CreatedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    LastUsedDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

---

## 🏗️ Backend Implementation Plan

### Phase 1: Core Infrastructure

#### 1.1 Entity & DTO Creation
```csharp
// Entities/Concrete/Notification.cs
public class Notification : IEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string Type { get; set; } // NotificationTypes enum
    public string RelatedEntityId { get; set; }
    public string RelatedEntityType { get; set; }
    public bool IsRead { get; set; }
    public string Priority { get; set; } // NotificationPriority enum
    public string ActionUrl { get; set; }
    public string IconType { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ReadDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

// Entities/Dtos/NotificationDtos.cs
public class CreateNotificationDto
{
    public int UserId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string Type { get; set; }
    public string RelatedEntityId { get; set; }
    public string Priority { get; set; }
    public string ActionUrl { get; set; }
}

public class NotificationListDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string Type { get; set; }
    public bool IsRead { get; set; }
    public string Priority { get; set; }
    public string ActionUrl { get; set; }
    public string IconType { get; set; }
    public string TimeAgo { get; set; } // "2 minutes ago"
    public DateTime CreatedDate { get; set; }
}

public class NotificationStatsDto
{
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int HighPriorityCount { get; set; }
    public DateTime? LastReadDate { get; set; }
}
```

#### 1.2 Repository Layer
```csharp
// DataAccess/Abstract/INotificationRepository.cs
public interface INotificationRepository : IRepository<Notification>
{
    Task<List<Notification>> GetUserNotificationsAsync(int userId, int skip = 0, int take = 20);
    Task<List<Notification>> GetUnreadNotificationsAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task<bool> DeleteNotificationAsync(int notificationId, int userId);
    Task<int> CleanupExpiredNotificationsAsync();
}

// DataAccess/Concrete/EntityFramework/NotificationRepository.cs
public class NotificationRepository : EfEntityRepositoryBase<Notification, ProjectDbContext>, INotificationRepository
{
    // Implementation
}
```

#### 1.3 Business Service Layer
```csharp
// Business/Services/Notification/INotificationService.cs
public interface INotificationService
{
    Task<IDataResult<int>> CreateNotificationAsync(CreateNotificationDto dto);
    Task<IDataResult<List<NotificationListDto>>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20);
    Task<IDataResult<NotificationStatsDto>> GetNotificationStatsAsync(int userId);
    Task<IResult> MarkAsReadAsync(int notificationId, int userId);
    Task<IResult> MarkAllAsReadAsync(int userId);
    Task<IResult> DeleteNotificationAsync(int notificationId, int userId);
    Task<IResult> CreateAnalysisCompletedNotificationAsync(PlantAnalysisAsyncResponseDto analysisResult);
    Task<IResult> CreateAnalysisFailedNotificationAsync(string analysisId, int userId, string errorMessage);
}

// Business/Services/Notification/NotificationService.cs
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPlantAnalysisRepository _plantAnalysisRepository;
    private readonly ILogger<NotificationService> _logger;

    // Implementation with detailed business logic
}
```

### Phase 2: API Endpoints

#### 2.1 NotificationController Enhancement
```csharp
// WebAPI/Controllers/NotificationController.cs
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize] // Require authentication
public class NotificationController : BaseApiController
{
    // Existing plant-analysis-completed endpoint enhancement
    [HttpPost("plant-analysis-completed")]
    public async Task<IActionResult> NotifyAnalysisCompleted([FromBody] AnalysisCompletedNotificationDto request)

    // New endpoints for notification management
    [HttpGet("my-notifications")]
    public async Task<IActionResult> GetMyNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)

    [HttpGet("stats")]
    public async Task<IActionResult> GetNotificationStats()

    [HttpPut("{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(int id)

    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)

    [HttpGet("types")]
    public async Task<IActionResult> GetNotificationTypes() // For frontend reference
}
```

#### 2.2 Real-time WebSocket Integration
```csharp
// Business/Services/Notification/INotificationHubService.cs
public interface INotificationHubService
{
    Task SendNotificationToUserAsync(int userId, NotificationListDto notification);
    Task SendBulkNotificationsAsync(Dictionary<int, List<NotificationListDto>> userNotifications);
}

// WebAPI/Hubs/NotificationHub.cs
[Authorize]
public class NotificationHub : Hub
{
    public async Task JoinUserGroup(int userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
    }

    public async Task LeaveUserGroup(int userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
    }
}
```

### Phase 3: Worker Service Integration

#### 3.1 PlantAnalysisJobService Enhancement
```csharp
// PlantAnalysisWorkerService/Jobs/PlantAnalysisJobService.cs
public class PlantAnalysisJobService : IPlantAnalysisJobService
{
    private readonly INotificationService _notificationService;
    private readonly INotificationHubService _hubService;

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 10, 30 })]
    public async Task SendNotificationAsync(PlantAnalysisAsyncResponseDto result)
    {
        try
        {
            // Create notification in database
            var notificationResult = await _notificationService.CreateAnalysisCompletedNotificationAsync(result);
            
            if (notificationResult.Success)
            {
                // Send real-time notification via WebSocket
                var notification = new NotificationListDto
                {
                    Id = notificationResult.Data,
                    Title = "Plant Analysis Completed",
                    Message = $"Your {result.CropType} analysis is ready!",
                    Type = "analysis_completed",
                    Priority = GetPriorityFromHealthScore(result.Summary?.OverallHealthScore),
                    ActionUrl = $"/dashboard/analysis/{result.AnalysisId}",
                    IconType = "plant_analysis",
                    TimeAgo = "Just now",
                    IsRead = false,
                    CreatedDate = DateTime.Now
                };

                // Extract userId from farmerId
                if (ExtractUserIdFromFarmerId(result.FarmerId, out int userId))
                {
                    await _hubService.SendNotificationToUserAsync(userId, notification);
                }
            }

            _logger.LogInformation($"Notification sent for analysis: {result.AnalysisId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send notification for analysis: {result.AnalysisId}");
            throw;
        }
    }

    private string GetPriorityFromHealthScore(int? healthScore)
    {
        return healthScore switch
        {
            <= 3 => "urgent",
            <= 5 => "high", 
            <= 7 => "normal",
            _ => "low"
        };
    }
}
```

---

## 📱 Frontend Implementation Plan

### Phase 4: Web Application (Angular/React)

#### 4.1 Notification Service
```typescript
// services/notification.service.ts
@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private hubConnection: HubConnection;
  private notifications$ = new BehaviorSubject<Notification[]>([]);
  private unreadCount$ = new BehaviorSubject<number>(0);

  constructor(private http: HttpClient, private authService: AuthService) {
    this.initializeSignalR();
  }

  private async initializeSignalR(): Promise<void> {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl('/hubs/notification', {
        accessTokenFactory: () => this.authService.getToken()
      })
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: Notification) => {
      this.addNotification(notification);
      this.showToast(notification);
    });

    await this.hubConnection.start();
    await this.joinUserGroup();
  }

  async getNotifications(page: number = 1): Promise<Notification[]> {
    return this.http.get<Notification[]>(`/api/v1/notification/my-notifications?page=${page}`).toPromise();
  }

  async markAsRead(notificationId: number): Promise<void> {
    await this.http.put(`/api/v1/notification/${notificationId}/mark-read`, {}).toPromise();
    this.updateNotificationReadStatus(notificationId);
  }

  async markAllAsRead(): Promise<void> {
    await this.http.put('/api/v1/notification/mark-all-read', {}).toPromise();
    this.markAllNotificationsAsRead();
  }

  private showToast(notification: Notification): void {
    // Show browser notification if permission granted
    if (Notification.permission === 'granted') {
      new Notification(notification.title, {
        body: notification.message,
        icon: `/assets/icons/${notification.iconType}.png`,
        data: { actionUrl: notification.actionUrl }
      });
    }
  }
}
```

#### 4.2 Notification Components
```typescript
// components/notification-bell/notification-bell.component.ts
@Component({
  selector: 'app-notification-bell',
  template: `
    <div class="notification-bell" (click)="toggleDropdown()">
      <i class="fas fa-bell"></i>
      <span class="badge" *ngIf="unreadCount > 0">{{ unreadCount }}</span>
      
      <div class="notification-dropdown" *ngIf="showDropdown">
        <div class="notification-header">
          <h4>Notifications</h4>
          <button (click)="markAllAsRead()" *ngIf="unreadCount > 0">Mark All Read</button>
        </div>
        
        <div class="notification-list">
          <div class="notification-item" 
               *ngFor="let notification of notifications"
               [class.unread]="!notification.isRead"
               (click)="handleNotificationClick(notification)">
            <div class="notification-icon">
              <i [class]="getIconClass(notification.iconType)"></i>
            </div>
            <div class="notification-content">
              <h5>{{ notification.title }}</h5>
              <p>{{ notification.message }}</p>
              <small>{{ notification.timeAgo }}</small>
            </div>
            <div class="notification-priority" [class]="notification.priority"></div>
          </div>
        </div>
        
        <div class="notification-footer">
          <a routerLink="/notifications">View All Notifications</a>
        </div>
      </div>
    </div>
  `
})
export class NotificationBellComponent implements OnInit {
  notifications: Notification[] = [];
  unreadCount: number = 0;
  showDropdown: boolean = false;

  constructor(private notificationService: NotificationService, private router: Router) {}

  async ngOnInit(): Promise<void> {
    this.notifications = await this.notificationService.getNotifications();
    this.unreadCount = this.notifications.filter(n => !n.isRead).length;
    
    // Subscribe to real-time updates
    this.notificationService.notifications$.subscribe(notifications => {
      this.notifications = notifications;
      this.unreadCount = notifications.filter(n => !n.isRead).length;
    });
  }

  async handleNotificationClick(notification: Notification): Promise<void> {
    if (!notification.isRead) {
      await this.notificationService.markAsRead(notification.id);
    }
    
    if (notification.actionUrl) {
      this.router.navigate([notification.actionUrl]);
    }
    
    this.showDropdown = false;
  }
}
```

### Phase 5: Mobile Application (Flutter/React Native)

#### 5.1 Notification Service (Flutter)
```dart
// services/notification_service.dart
class NotificationService {
  static final NotificationService _instance = NotificationService._internal();
  factory NotificationService() => _instance;
  NotificationService._internal();

  HubConnection? _hubConnection;
  final StreamController<Notification> _notificationController = StreamController.broadcast();
  Stream<Notification> get notificationStream => _notificationController.stream;

  Future<void> initialize() async {
    await _initializeSignalR();
    await _requestNotificationPermissions();
  }

  Future<void> _initializeSignalR() async {
    final token = await AuthService().getToken();
    
    _hubConnection = HubConnectionBuilder()
        .withUrl('${ApiConfig.baseUrl}/hubs/notification',
            options: HttpConnectionOptions(
              accessTokenFactory: () => Future.value(token),
            ))
        .build();

    _hubConnection!.on('ReceiveNotification', (arguments) {
      final notification = Notification.fromJson(arguments![0]);
      _notificationController.add(notification);
      _showLocalNotification(notification);
    });

    await _hubConnection!.start();
  }

  Future<void> _showLocalNotification(Notification notification) async {
    const AndroidNotificationDetails androidPlatformChannelSpecifics =
        AndroidNotificationDetails(
      'plant_analysis_channel',
      'Plant Analysis Notifications',
      channelDescription: 'Notifications for completed plant analyses',
      importance: Importance.high,
      priority: Priority.high,
      ticker: 'Plant Analysis Update',
    );

    const NotificationDetails platformChannelSpecifics =
        NotificationDetails(android: androidPlatformChannelSpecifics);

    await flutterLocalNotificationsPlugin.show(
      notification.id,
      notification.title,
      notification.message,
      platformChannelSpecifics,
      payload: notification.actionUrl,
    );
  }

  Future<List<Notification>> getNotifications({int page = 1}) async {
    final response = await ApiService().get('/notification/my-notifications?page=$page');
    return (response.data as List)
        .map((json) => Notification.fromJson(json))
        .toList();
  }

  Future<void> markAsRead(int notificationId) async {
    await ApiService().put('/notification/$notificationId/mark-read');
  }
}
```

#### 5.2 Notification Widgets (Flutter)
```dart
// widgets/notification_bell.dart
class NotificationBell extends StatefulWidget {
  @override
  _NotificationBellState createState() => _NotificationBellState();
}

class _NotificationBellState extends State<NotificationBell> {
  int unreadCount = 0;
  List<Notification> notifications = [];

  @override
  void initState() {
    super.initState();
    _loadNotifications();
    _listenToNotifications();
  }

  void _loadNotifications() async {
    final notifications = await NotificationService().getNotifications();
    setState(() {
      this.notifications = notifications;
      this.unreadCount = notifications.where((n) => !n.isRead).length;
    });
  }

  void _listenToNotifications() {
    NotificationService().notificationStream.listen((notification) {
      setState(() {
        notifications.insert(0, notification);
        if (!notification.isRead) unreadCount++;
      });
    });
  }

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        IconButton(
          icon: Icon(Icons.notifications),
          onPressed: () => _showNotificationModal(context),
        ),
        if (unreadCount > 0)
          Positioned(
            right: 8,
            top: 8,
            child: Container(
              padding: EdgeInsets.all(2),
              decoration: BoxDecoration(
                color: Colors.red,
                borderRadius: BorderRadius.circular(10),
              ),
              constraints: BoxConstraints(
                minWidth: 16,
                minHeight: 16,
              ),
              child: Text(
                '$unreadCount',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 12,
                ),
                textAlign: TextAlign.center,
              ),
            ),
          ),
      ],
    );
  }

  void _showNotificationModal(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (context) => NotificationModal(
        notifications: notifications,
        onNotificationTap: _handleNotificationTap,
      ),
    );
  }

  void _handleNotificationTap(Notification notification) async {
    if (!notification.isRead) {
      await NotificationService().markAsRead(notification.id);
      setState(() {
        notification.isRead = true;
        unreadCount--;
      });
    }

    if (notification.actionUrl != null) {
      // Navigate to the specified page
      Navigator.pushNamed(context, notification.actionUrl!);
    }
  }
}
```

---

## 🎨 UI/UX Design Specifications

### Notification Types & Icons
```typescript
export const NotificationConfig = {
  types: {
    analysis_completed: {
      icon: 'plant_analysis',
      color: '#10B981', // Green
      defaultTitle: 'Analysis Complete',
      soundFile: 'analysis_complete.mp3'
    },
    analysis_failed: {
      icon: 'error_warning',
      color: '#EF4444', // Red
      defaultTitle: 'Analysis Failed',
      soundFile: 'error_notification.mp3'
    },
    subscription_expiry: {
      icon: 'subscription_warning',
      color: '#F59E0B', // Amber
      defaultTitle: 'Subscription Expiring',
      soundFile: 'warning_notification.mp3'
    },
    sponsorship_message: {
      icon: 'message_sponsor',
      color: '#8B5CF6', // Purple
      defaultTitle: 'Message from Sponsor',
      soundFile: 'message_notification.mp3'
    }
  },
  priorities: {
    low: { color: '#6B7280', badge: false },
    normal: { color: '#3B82F6', badge: false },
    high: { color: '#F59E0B', badge: true },
    urgent: { color: '#EF4444', badge: true, blink: true }
  }
};
```

### Notification Templates
```typescript
export const NotificationTemplates = {
  analysis_completed: {
    title: (cropType: string) => `${cropType} Analysis Complete`,
    message: (healthScore: number, concern: string) => 
      `Health Score: ${healthScore}/10. ${concern ? `Primary concern: ${concern}` : 'Plant looks healthy!'}`,
    actionUrl: (analysisId: string) => `/dashboard/analysis/${analysisId}`
  },
  analysis_failed: {
    title: () => 'Analysis Failed',
    message: (error: string) => `Unable to process your plant image. ${error}`,
    actionUrl: () => '/dashboard/upload'
  }
};
```

---

## 📊 Testing & Quality Assurance

### Unit Tests
```csharp
// Tests/Business/Services/NotificationServiceTests.cs
[TestClass]
public class NotificationServiceTests
{
    [TestMethod]
    public async Task CreateNotification_ValidData_ReturnsSuccess()
    {
        // Arrange
        var notificationDto = new CreateNotificationDto
        {
            UserId = 1,
            Title = "Test Notification",
            Message = "Test message",
            Type = "analysis_completed"
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(notificationDto);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Data > 0);
    }

    [TestMethod]
    public async Task GetUserNotifications_ValidUserId_ReturnsNotifications()
    {
        // Test implementation
    }
}
```

### Integration Tests
```csharp
// Tests/WebAPI/Controllers/NotificationControllerTests.cs
[TestClass]
public class NotificationControllerIntegrationTests : BaseIntegrationTest
{
    [TestMethod]
    public async Task GetMyNotifications_AuthenticatedUser_ReturnsNotifications()
    {
        // Integration test implementation
    }
}
```

### Performance Tests
```csharp
// Tests/Performance/NotificationPerformanceTests.cs
[TestClass]
public class NotificationPerformanceTests
{
    [TestMethod]
    public async Task SendBulkNotifications_1000Users_CompletesWithin5Seconds()
    {
        // Performance test for bulk notifications
    }
}
```

---

## 🚀 Deployment & Monitoring

### Database Migration
```sql
-- V1_Add_Notifications_Tables.sql
-- Create notifications tables with proper indexes
-- Add foreign key constraints
-- Insert default notification preferences for existing users
```

### Configuration
```json
// appsettings.json
{
  "NotificationSettings": {
    "EnableRealTimeNotifications": true,
    "MaxNotificationsPerUser": 1000,
    "NotificationRetentionDays": 30,
    "BatchSize": 100,
    "SignalRConnectionTimeout": 30000
  }
}
```

### Monitoring & Analytics
```csharp
// Business/Services/Notification/NotificationAnalyticsService.cs
public class NotificationAnalyticsService
{
    public async Task<NotificationMetrics> GetMetricsAsync(DateTime from, DateTime to)
    {
        // Track notification delivery rates
        // Monitor user engagement
        // Performance metrics
    }
}
```

---

## 📅 Implementation Timeline

### Sprint 1 (1 hafta) - Core Infrastructure
- [ ] Database schema creation
- [ ] Entity ve DTO'lar
- [ ] Repository layer
- [ ] Basic notification service

### Sprint 2 (1 hafta) - API & Business Logic  
- [ ] NotificationController enhancement
- [ ] Worker service integration
- [ ] SignalR hub setup
- [ ] Unit tests

### Sprint 3 (1 hafta) - Frontend Integration
- [ ] Web notification service
- [ ] Notification bell component
- [ ] Real-time updates
- [ ] Browser notifications

### Sprint 4 (1 hafta) - Mobile Integration
- [ ] Flutter notification service
- [ ] Local notifications setup
- [ ] UI components
- [ ] Deep linking

### Sprint 5 (1 hafta) - Testing & Polish
- [ ] Integration tests
- [ ] Performance optimization
- [ ] UI/UX improvements
- [ ] Documentation

---

## 🎯 Success Metrics

### Technical Metrics
- ✅ Notification delivery time < 2 seconds
- ✅ Real-time connection success rate > 95%
- ✅ Database query performance < 100ms
- ✅ Zero notification loss

### User Experience Metrics
- ✅ Notification click-through rate > 30%
- ✅ User engagement increase > 15%
- ✅ Mobile app retention improvement
- ✅ Reduced support tickets for "missed analysis"

### Business Metrics
- ✅ Increased daily active users
- ✅ Better user satisfaction scores
- ✅ Enhanced product stickiness
- ✅ Improved feature adoption rates

---

## 📝 Notes & Considerations

### Security
- JWT token validation for all notification endpoints
- Rate limiting for notification creation
- XSS protection for notification content
- CSRF protection for state-changing operations

### Scalability
- Horizontal scaling with Redis backplane for SignalR
- Database partitioning for large notification volumes
- CDN integration for notification assets
- Background job processing with Hangfire

### Accessibility
- Screen reader support for notifications
- High contrast mode support
- Keyboard navigation for notification dropdowns
- ARIA labels for all interactive elements

### Internationalization
- Multi-language notification templates
- Timezone-aware notification scheduling
- Cultural considerations for notification timing
- Right-to-left language support

---

*Bu doküman plant analysis notification sistemi için kapsamlı bir implementation rehberidir. Geliştirme sırasında güncellenecek ve detaylandırılacaktır.*