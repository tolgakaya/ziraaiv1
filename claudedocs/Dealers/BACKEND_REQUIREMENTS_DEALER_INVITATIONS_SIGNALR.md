# Backend Requirements - Dealer Invitations SignalR Solution

**Date**: 2025-10-30
**Feature**: Real-time Dealer Invitation Notifications + Pending Invitations API
**Status**: 📋 Requirements Document - Awaiting Backend Implementation

---

## 🎯 Problem Statement

### Current Issues
1. **SMS Auto-Fill Problem**: Mobile app reads most recent SMS, which may be an already-accepted invitation → 400 error
2. **No Cross-Device Support**: SMS-based invitations lost when user changes phones
3. **Privacy Concerns**: SMS read permission required, GDPR implications
4. **No Real-time Updates**: User must manually check SMS inbox for new invitations
5. **First-Time User Issue**: Users who install app after receiving SMS invitation cannot see their pending invitations

### Proposed Solution
- **Backend Endpoint**: GET user's pending invitations (requires authentication)
- **SignalR Real-time**: Push notification when new invitation created
- **Remove SMS Scanning**: Backend becomes single source of truth
- **Cross-device Support**: Invitations tied to user email, not SMS inbox

---

## 📡 Required Backend Changes

### 1. New API Endpoint

#### **GET /api/v1/dealer/invitations/my-pending**

**Purpose**: Returns authenticated user's pending dealer invitations

**Authentication**: Required (JWT Bearer token)

**Authorization**: Roles: `Dealer`, `Farmer`, or `Sponsor` (future dealers)

**Request**:
```http
GET /api/v1/dealer/invitations/my-pending
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response Structure**:
```json
{
  "success": true,
  "message": "Found 2 pending invitations",
  "data": {
    "invitations": [
      {
        "invitationId": 3,
        "token": "7fc679cd040c44509f961f2b9fb0f7b4",
        "sponsorCompanyName": "dort tarim",
        "codeCount": 11,
        "packageTier": "L",
        "expiresAt": "2025-11-06T09:59:54.535436",
        "remainingDays": 6,
        "status": "Pending",
        "invitationMessage": "🎉 dort tarim sizi bayilik ağına katılmaya davet ediyor!",
        "dealerEmail": "bilgi@bilgitap.com",
        "dealerPhone": "+905556866386",
        "createdAt": "2025-10-30T09:59:54.535416"
      },
      {
        "invitationId": 5,
        "token": "abc123def456...",
        "sponsorCompanyName": "Sponsor B",
        "codeCount": 5,
        "packageTier": "M",
        "expiresAt": "2025-11-10T10:00:00",
        "remainingDays": 10,
        "status": "Pending",
        "invitationMessage": null,
        "dealerEmail": "bilgi@bilgitap.com",
        "dealerPhone": "+905556866386",
        "createdAt": "2025-10-28T12:00:00"
      }
    ],
    "totalCount": 2
  }
}
```

**Business Logic**:
```sql
-- SQL Query (Example)
SELECT
  di.Id as InvitationId,
  di.Token,
  s.CompanyName as SponsorCompanyName,
  di.CodeCount,
  di.PackageTier,
  di.ExpiresAt,
  DATEDIFF(day, GETUTCDATE(), di.ExpiresAt) as RemainingDays,
  di.Status,
  di.InvitationMessage,
  di.DealerEmail,
  di.DealerPhone,
  di.CreatedAt
FROM DealerInvitations di
INNER JOIN Sponsors s ON di.SponsorId = s.Id
WHERE di.DealerEmail = @UserEmail  -- From JWT claims
  AND di.Status = 'Pending'
  AND di.ExpiresAt > GETUTCDATE()  -- Not expired
ORDER BY di.ExpiresAt ASC  -- Expiring soon first
```

**Error Responses**:

```json
// 401 Unauthorized
{
  "success": false,
  "message": "Authentication required"
}

// 404 Not Found
{
  "success": true,
  "message": "No pending invitations found",
  "data": {
    "invitations": [],
    "totalCount": 0
  }
}

// 500 Internal Server Error
{
  "success": false,
  "message": "Failed to retrieve pending invitations",
  "error": "Database connection error"
}
```

**C# Implementation Example**:
```csharp
// Controllers/DealerInvitationController.cs

[HttpGet("my-pending")]
[Authorize(Roles = "Dealer,Farmer,Sponsor")]
public async Task<ActionResult<ApiResponse<PendingInvitationsResponse>>> GetMyPendingInvitations()
{
    try
    {
        // Get user email from JWT claims
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(userEmail))
        {
            return Unauthorized(ApiResponse<PendingInvitationsResponse>.Fail("Email not found in token"));
        }

        // Query pending invitations
        var query = new GetMyPendingInvitationsQuery { DealerEmail = userEmail };
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<PendingInvitationsResponse>.Fail(result.Message));
        }

        return Ok(ApiResponse<PendingInvitationsResponse>.Success(
            result.Data,
            $"Found {result.Data.TotalCount} pending invitations"
        ));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving pending invitations for user");
        return StatusCode(500, ApiResponse<PendingInvitationsResponse>.Fail(
            "Failed to retrieve pending invitations"
        ));
    }
}
```

**Query Handler Example**:
```csharp
// Queries/GetMyPendingInvitationsQuery.cs

public class GetMyPendingInvitationsQueryHandler
    : IRequestHandler<GetMyPendingInvitationsQuery, Result<PendingInvitationsResponse>>
{
    private readonly ApplicationDbContext _context;

    public GetMyPendingInvitationsQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PendingInvitationsResponse>> Handle(
        GetMyPendingInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var invitations = await _context.DealerInvitations
            .Include(di => di.Sponsor)
            .Where(di => di.DealerEmail == request.DealerEmail
                      && di.Status == InvitationStatus.Pending
                      && di.ExpiresAt > DateTime.UtcNow)
            .OrderBy(di => di.ExpiresAt) // Expiring soon first
            .Select(di => new DealerInvitationSummaryDto
            {
                InvitationId = di.Id,
                Token = di.Token,
                SponsorCompanyName = di.Sponsor.CompanyName,
                CodeCount = di.CodeCount,
                PackageTier = di.PackageTier,
                ExpiresAt = di.ExpiresAt,
                RemainingDays = (di.ExpiresAt - DateTime.UtcNow).Days,
                Status = di.Status.ToString(),
                InvitationMessage = di.InvitationMessage,
                DealerEmail = di.DealerEmail,
                DealerPhone = di.DealerPhone,
                CreatedAt = di.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var response = new PendingInvitationsResponse
        {
            Invitations = invitations,
            TotalCount = invitations.Count
        };

        return Result<PendingInvitationsResponse>.Success(response);
    }
}
```

---

### 2. SignalR Real-time Notification

#### **SignalR Hub Setup**

**Hub Path**: `/hubs/notification`

**Event Name**: `NewDealerInvitation`

**Purpose**: Push real-time notification when new dealer invitation is created

**Connection Authentication**: JWT Bearer token

**SignalR Hub Implementation**:
```csharp
// Hubs/NotificationHub.cs

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;

        if (!string.IsNullOrEmpty(userEmail))
        {
            // Add user to their email-based group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userEmail}");
            _logger.LogInformation($"SignalR connected: {userEmail}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value;

        if (!string.IsNullOrEmpty(userEmail))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userEmail}");
            _logger.LogInformation($"SignalR disconnected: {userEmail}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
```

**Startup/Program.cs Configuration**:
```csharp
// Program.cs

builder.Services.AddSignalR();

// ...

app.MapHub<NotificationHub>("/hubs/notification");
```

**Notification Service**:
```csharp
// Services/DealerInvitationNotificationService.cs

public class DealerInvitationNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<DealerInvitationNotificationService> _logger;

    public DealerInvitationNotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<DealerInvitationNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyNewInvitation(DealerInvitation invitation, Sponsor sponsor)
    {
        try
        {
            var notificationData = new
            {
                invitationId = invitation.Id,
                token = invitation.Token,
                sponsorCompanyName = sponsor.CompanyName,
                codeCount = invitation.CodeCount,
                packageTier = invitation.PackageTier,
                expiresAt = invitation.ExpiresAt,
                remainingDays = (invitation.ExpiresAt - DateTime.UtcNow).Days,
                status = invitation.Status.ToString(),
                invitationMessage = invitation.InvitationMessage,
                dealerEmail = invitation.DealerEmail,
                dealerPhone = invitation.DealerPhone,
                createdAt = invitation.CreatedAt
            };

            // Send to user's specific group
            await _hubContext.Clients
                .Group($"user_{invitation.DealerEmail}")
                .SendAsync("NewDealerInvitation", notificationData);

            _logger.LogInformation(
                $"SignalR notification sent to {invitation.DealerEmail} for invitation {invitation.Id}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Failed to send SignalR notification for invitation {invitation.Id}"
            );
        }
    }
}
```

**Integration in Create Invitation Flow**:
```csharp
// Commands/CreateDealerInvitationCommandHandler.cs

public class CreateDealerInvitationCommandHandler
    : IRequestHandler<CreateDealerInvitationCommand, Result<DealerInvitation>>
{
    private readonly ApplicationDbContext _context;
    private readonly ISmsService _smsService;
    private readonly DealerInvitationNotificationService _notificationService; // NEW

    public CreateDealerInvitationCommandHandler(
        ApplicationDbContext context,
        ISmsService smsService,
        DealerInvitationNotificationService notificationService)
    {
        _context = context;
        _smsService = smsService;
        _notificationService = notificationService;
    }

    public async Task<Result<DealerInvitation>> Handle(
        CreateDealerInvitationCommand request,
        CancellationToken cancellationToken)
    {
        // Create invitation
        var invitation = new DealerInvitation
        {
            Token = GenerateToken(),
            SponsorId = request.SponsorId,
            DealerEmail = request.DealerEmail,
            DealerPhone = request.DealerPhone,
            CodeCount = request.CodeCount,
            PackageTier = request.PackageTier,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            InvitationMessage = request.CustomMessage
        };

        await _context.DealerInvitations.AddAsync(invitation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Send SMS (existing)
        await _smsService.SendDealerInvitationAsync(invitation);

        // 🆕 Send SignalR notification (NEW)
        var sponsor = await _context.Sponsors.FindAsync(request.SponsorId);
        await _notificationService.NotifyNewInvitation(invitation, sponsor);

        return Result<DealerInvitation>.Success(invitation);
    }
}
```

---

## 📋 Testing Requirements

### 1. API Endpoint Testing

**Test Case 1: Successful Retrieval**
```http
GET /api/v1/dealer/invitations/my-pending
Authorization: Bearer {valid_token}

Expected: 200 OK with list of pending invitations
```

**Test Case 2: No Pending Invitations**
```http
GET /api/v1/dealer/invitations/my-pending
Authorization: Bearer {valid_token}

Expected: 200 OK with empty list
```

**Test Case 3: Unauthorized**
```http
GET /api/v1/dealer/invitations/my-pending
// No Authorization header

Expected: 401 Unauthorized
```

**Test Case 4: Expired Invitations Filtered**
```
Setup: Create invitation with ExpiresAt in the past
Request: GET /api/v1/dealer/invitations/my-pending

Expected: Expired invitation NOT included in response
```

**Test Case 5: Accepted Invitations Filtered**
```
Setup: Create invitation with Status = "Accepted"
Request: GET /api/v1/dealer/invitations/my-pending

Expected: Accepted invitation NOT included in response
```

### 2. SignalR Testing

**Test Case 1: Connection Established**
```
1. Mobile app connects with valid JWT
2. Check server logs: "SignalR connected: user@email.com"

Expected: Connection successful
```

**Test Case 2: Notification Received**
```
1. Mobile app connected
2. Sponsor creates invitation for user@email.com
3. Mobile app receives "NewDealerInvitation" event

Expected: Event data matches invitation details
```

**Test Case 3: Multiple Users Isolation**
```
1. User A connected
2. User B connected
3. Create invitation for User B
4. User A should NOT receive notification

Expected: Only User B receives notification
```

**Test Case 4: Reconnection After Network Loss**
```
1. Mobile app connected
2. Simulate network loss (disable WiFi)
3. Re-enable WiFi
4. Create invitation

Expected: Mobile app auto-reconnects and receives notification
```

---

## 🔒 Security Considerations

### 1. Authentication & Authorization
- ✅ JWT Bearer token required
- ✅ Email claim extraction from token
- ✅ User can only see their own invitations (email-based filtering)
- ✅ SignalR connection authenticated with same JWT

### 2. Data Privacy
- ✅ No SMS content exposed
- ✅ Invitations filtered by authenticated user's email
- ✅ No cross-user data leakage
- ✅ SignalR uses user-specific groups

### 3. Performance
- ✅ Database query optimized with indexes on `DealerEmail` + `Status` + `ExpiresAt`
- ✅ SignalR uses groups to avoid broadcasting to all users
- ✅ Pagination support (future enhancement)

---

## 📊 Database Recommendations

### Recommended Indexes
```sql
-- For fast pending invitations query
CREATE NONCLUSTERED INDEX IX_DealerInvitations_Email_Status_Expires
ON DealerInvitations (DealerEmail, Status, ExpiresAt)
INCLUDE (SponsorId, Token, CodeCount, PackageTier, InvitationMessage, DealerPhone, CreatedAt);

-- For sponsor lookup
CREATE NONCLUSTERED INDEX IX_DealerInvitations_SponsorId
ON DealerInvitations (SponsorId);
```

---

## ✅ Acceptance Criteria

### Backend Endpoint
- [ ] GET `/api/v1/dealer/invitations/my-pending` implemented
- [ ] Returns only Pending + non-expired invitations
- [ ] Sorted by `ExpiresAt` ASC (expiring soon first)
- [ ] Requires JWT authentication
- [ ] Filters by authenticated user's email
- [ ] Response matches specified JSON structure
- [ ] Proper error handling (401, 404, 500)
- [ ] Unit tests written
- [ ] Integration tests written

### SignalR
- [ ] NotificationHub implemented at `/hubs/notification`
- [ ] JWT authentication enabled
- [ ] `NewDealerInvitation` event defined
- [ ] Notification sent when invitation created
- [ ] User-specific groups used (no broadcast)
- [ ] Connection/disconnection logging
- [ ] Tested with multiple concurrent users
- [ ] Auto-reconnection supported

### Integration
- [ ] Create invitation flow triggers SignalR notification
- [ ] SMS still sent (parallel to SignalR)
- [ ] Notification payload matches API response structure
- [ ] Tested on staging environment
- [ ] Performance tested (100+ concurrent users)

---

## 📝 Notes for Backend Team

1. **Email Field**: Ensure `DealerEmail` field is properly indexed
2. **SignalR Scalability**: For production, consider Redis backplane for multi-server deployments
3. **JWT Claims**: Ensure `ClaimTypes.Email` is included in JWT generation
4. **Timezone**: Use UTC consistently (`DateTime.UtcNow`)
5. **Logging**: Add structured logging for debugging SignalR issues
6. **Rate Limiting**: Consider rate limiting on `/my-pending` endpoint (e.g., 60 requests/minute)

---

## 🚀 Estimated Effort

- **API Endpoint**: 2-3 hours (includes query handler, tests)
- **SignalR Hub**: 3-4 hours (includes hub setup, notification service, integration)
- **Testing**: 2-3 hours (unit + integration tests)
- **Documentation**: 1 hour

**Total**: ~8-11 hours backend development

---

**Document Version**: 1.0
**Created**: 2025-10-30
**Status**: 📋 Requirements - Awaiting Backend Implementation
