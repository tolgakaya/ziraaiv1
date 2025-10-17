# Sponsor-Farmer Messaging System - Complete Documentation

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [System Architecture](#system-architecture)
3. [Business Rules & Constraints](#business-rules--constraints)
4. [Technical Implementation](#technical-implementation)
5. [Security & Privacy](#security--privacy)
6. [API Reference](#api-reference)
7. [Database Schema](#database-schema)
8. [Error Handling](#error-handling)

---

## Executive Summary

### Purpose
The Sponsor-Farmer Messaging System enables **tier-based, analysis-scoped communication** between agricultural sponsors (L/XL tiers) and farmers whose plant analyses were performed using sponsorship codes. This system ensures controlled, contextual communication while protecting farmer privacy and preventing spam.

### Key Characteristics
- **Tier-Restricted**: Only L and XL tier sponsors can message farmers
- **Analysis-Scoped**: Messages are contextual to specific plant analyses
- **Ownership-Based**: Sponsors can only message farmers for analyses done with their codes
- **Rate-Limited**: Maximum 10 messages per day per farmer
- **Farmer-Controlled**: Farmers can block/mute unwanted sponsors
- **In-App Only**: No external SMS/WhatsApp integration

### Business Value
- **For Sponsors**: Direct technical support channel for farmers using their products
- **For Farmers**: Free expert advice from sponsors who funded their analyses
- **For Platform**: Increased engagement and value proposition for higher tier packages

---

## System Architecture

### High-Level Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                     SPONSOR-FARMER MESSAGING FLOW                    │
└─────────────────────────────────────────────────────────────────────┘

1. FARMER REDEEMS CODE
   Farmer → Redemption → Analysis → SponsorAnalysisAccess Created

2. ANALYSIS COMPLETED
   Analysis → Database → Sponsor Notified (has access to view)

3. SPONSOR VIEWS ANALYSIS
   Sponsor → GET /sponsorship/analysis/{id} → Filtered Data (tier-based)

4. SPONSOR SENDS MESSAGE (L/XL Only)
   ┌──────────────────────────────────────────────────┐
   │ Validation Layers (Sequential):                  │
   │ 1. Tier Check (L=3 or XL=4 only)                │
   │ 2. Analysis Ownership (SponsorUserId match)      │
   │ 3. Access Record Verification                    │
   │ 4. Block Check (Farmer hasn't blocked sponsor)  │
   │ 5. Rate Limit (< 10 messages today)             │
   │ 6. First Message Approval (IsApproved = false)  │
   └──────────────────────────────────────────────────┘

5. MESSAGE STORED
   Message → Database → AnalysisMessage Table

6. FARMER VIEWS MESSAGE
   Farmer → GET /sponsorship/messages/conversation → Message List

7. FARMER CAN REPLY
   Farmer → POST /sponsorship/messages → Reply (no restrictions)

8. FARMER CAN BLOCK
   Farmer → POST /sponsorship/messages/block → FarmerSponsorBlock Record
```

---

## Business Rules & Constraints

### 1. Tier-Based Messaging Rules

#### S Tier (₺99.99/month)
- **Data Access**: 30%
- **Messaging**: ❌ **NOT ALLOWED**
- **Farmer Visibility**: Anonymous (no contact info)
- **Why No Messaging**: Farmer identity is completely hidden for privacy

#### M Tier (₺299.99/month)
- **Data Access**: 30%
- **Messaging**: ❌ **NOT ALLOWED**
- **Farmer Visibility**: Anonymous profile only (demographics, no identity)
- **Why No Messaging**: Farmer remains anonymous, no direct contact possible

#### L Tier (₺599.99/month)
- **Data Access**: 60%
- **Messaging**: ✅ **ENABLED**
- **Farmer Visibility**: Full profile with contact information
- **Message Limit**: 10 messages/day/farmer
- **Features**: Direct messaging, technical support, product recommendations

#### XL Tier (₺1,499.99/month)
- **Data Access**: 100%
- **Messaging**: ✅ **ENABLED**
- **Farmer Visibility**: Full profile with contact information
- **Message Limit**: 10 messages/day/farmer
- **Features**: All L tier features + Smart Links + Priority support

### 2. Analysis-Scoped Messaging

**Critical Constraint**: Messaging is NOT open - it's contextual to specific analyses.

#### Ownership Requirements
```csharp
// Sponsor can ONLY message farmer if:
1. Analysis.SponsorUserId == Sponsor.UserId
   → Analysis was performed using sponsor's code

2. SponsorAnalysisAccess record exists
   → Sponsor has been granted access to this analysis

3. Analysis.ActiveSponsorshipId is valid
   → Analysis is within sponsored subscription period
```

#### Example Scenarios

**✅ ALLOWED**:
```
Scenario: Sponsor A purchased XL package, farmer redeemed code, did analysis
- Sponsor A can message farmer about THIS analysis
- Message appears in analysis context
- Farmer sees sponsor logo + message
```

**❌ NOT ALLOWED**:
```
Scenario: Sponsor A tries to message farmer about analysis done with Sponsor B's code
- Validation fails at ownership check
- Error: "You can only message farmers for analyses done using your sponsorship codes"
```

**❌ NOT ALLOWED**:
```
Scenario: Sponsor A (S tier) tries to message farmer
- Validation fails at tier check
- Error: "Messaging is only available for L and XL tier sponsors"
```

### 3. Rate Limiting Rules

#### Daily Limit: 10 Messages Per Farmer
```csharp
// Rate limit calculation
Today = DateTime.Now.Date
MessageCount = Count(AnalysisMessage WHERE
    FromUserId = SponsorId AND
    ToUserId = FarmerId AND
    SentDate >= Today AND
    SentDate < Today + 1 day AND
    SenderRole = "Sponsor")

if (MessageCount >= 10) {
    return Error("Daily message limit reached (10 messages per day per farmer)")
}
```

#### Limit Applies Per Farmer (Not Per Analysis)
- Sponsor can send max 10 messages to Farmer X across ALL analyses
- Not 10 per analysis - 10 per farmer total per day
- Resets at midnight (00:00 local time)

#### Rate Limit Bypass
- **Farmers**: No rate limit (can reply unlimited times)
- **Admins**: No rate limit (platform moderation)

### 4. Block & Mute System

#### Farmer Protection Rights
```
Farmers can:
1. Block sponsors completely (IsBlocked = true)
   → Sponsor CANNOT send messages
   → Sponsor CANNOT view farmer profile

2. Mute sponsors (IsMuted = true)
   → Sponsor CAN send messages
   → Farmer doesn't receive notifications
```

#### Block Workflow
```sql
-- Farmer blocks Sponsor
INSERT INTO FarmerSponsorBlocks (FarmerId, SponsorId, IsBlocked, IsMuted, CreatedDate, Reason)
VALUES (123, 456, true, false, NOW(), 'Spam messages');

-- Validation check before sending message
SELECT IsBlocked FROM FarmerSponsorBlocks
WHERE FarmerId = 123 AND SponsorId = 456;

-- If IsBlocked = true → Error: "This farmer has blocked messages from you"
```

#### Unblock Workflow
```sql
-- Farmer unblocks Sponsor
UPDATE FarmerSponsorBlocks
SET IsBlocked = false
WHERE FarmerId = 123 AND SponsorId = 456;
```

### 5. First Message Approval

#### Admin Moderation for New Conversations
```csharp
// Check if first message between sponsor and farmer for this analysis
IsFirstMessage = !EXISTS(AnalysisMessage WHERE
    PlantAnalysisId = X AND
    ((FromUserId = SponsorId AND ToUserId = FarmerId) OR
     (FromUserId = FarmerId AND ToUserId = SponsorId)))

// First messages require approval
if (IsFirstMessage) {
    Message.IsApproved = false; // Requires admin review
    Message.ApprovedDate = null;
} else {
    Message.IsApproved = true; // Auto-approve subsequent messages
    Message.ApprovedDate = DateTime.Now;
}
```

#### Approval Workflow (Future)
1. First message sent by sponsor → `IsApproved = false`
2. Admin reviews message in moderation panel
3. Admin approves → `IsApproved = true`, `ApprovedDate = NOW()`
4. Farmer can now see the message
5. All subsequent messages in this conversation auto-approved

---

## Technical Implementation

### 1. Service Layer Architecture

#### AnalysisMessagingService.cs

**Core Methods**:

```csharp
public class AnalysisMessagingService : IAnalysisMessagingService
{
    // Dependencies
    private readonly IAnalysisMessageRepository _messageRepository;
    private readonly ISponsorProfileRepository _sponsorProfileRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISponsorAnalysisAccessRepository _analysisAccessRepository;
    private readonly IPlantAnalysisRepository _plantAnalysisRepository;
    private readonly IFarmerSponsorBlockRepository _blockRepository;
    private readonly IMessageRateLimitService _rateLimitService;

    // 1. TIER-BASED PERMISSION CHECK (L/XL only)
    public async Task<bool> CanSendMessageAsync(int sponsorId)
    {
        var profile = await _sponsorProfileRepository.GetBySponsorIdAsync(sponsorId);

        // Farmers can always message
        if (profile == null) return true;

        // Inactive sponsors cannot message
        if (!profile.IsActive) return false;

        // Check for L (tier 3) or XL (tier 4) purchases
        foreach (var purchase in profile.SponsorshipPurchases)
        {
            if (purchase.SubscriptionTierId >= 3)
                return true;
        }

        return false;
    }

    // 2. COMPREHENSIVE VALIDATION (All 4 checks)
    public async Task<(bool canSend, string errorMessage)>
        CanSendMessageForAnalysisAsync(int sponsorId, int farmerId, int plantAnalysisId)
    {
        // CHECK 1: Tier Permission
        if (!await CanSendMessageAsync(sponsorId))
            return (false, "Messaging is only available for L and XL tier sponsors");

        // CHECK 2: Analysis Ownership
        var analysis = await _plantAnalysisRepository.GetAsync(a => a.Id == plantAnalysisId);
        if (analysis == null)
            return (false, "Analysis not found");

        if (analysis.SponsorUserId != sponsorId)
            return (false, "You can only message farmers for analyses done using your sponsorship codes");

        // CHECK 3: Access Record Verification
        var hasAccess = await _analysisAccessRepository.GetAsync(
            a => a.SponsorId == sponsorId && a.PlantAnalysisId == plantAnalysisId);

        if (hasAccess == null)
            return (false, "No access record found for this analysis");

        // CHECK 4: Block Check
        var isBlocked = await _blockRepository.IsBlockedAsync(farmerId, sponsorId);
        if (isBlocked)
            return (false, "This farmer has blocked messages from you");

        // CHECK 5: Rate Limit
        var canSendByRate = await _rateLimitService.CanSendMessageToFarmerAsync(sponsorId, farmerId);
        if (!canSendByRate)
        {
            var remaining = await _rateLimitService.GetRemainingMessagesAsync(sponsorId, farmerId);
            return (false, $"Daily message limit reached (10 messages per day per farmer). Remaining: {remaining}");
        }

        return (true, string.Empty);
    }

    // 3. SEND MESSAGE WITH VALIDATION
    public async Task<AnalysisMessage> SendMessageAsync(
        int fromUserId, int toUserId, int plantAnalysisId,
        string message, string messageType = "Information")
    {
        // Get users
        var fromUser = await _userRepository.GetAsync(u => u.UserId == fromUserId);
        var toUser = await _userRepository.GetAsync(u => u.UserId == toUserId);

        if (fromUser == null || toUser == null)
            return null;

        // Validate if sender is sponsor
        var sponsorProfile = await _sponsorProfileRepository.GetBySponsorIdAsync(fromUserId);

        if (sponsorProfile != null)
        {
            // Comprehensive validation for sponsors
            var (canSend, errorMessage) = await CanSendMessageForAnalysisAsync(
                fromUserId, toUserId, plantAnalysisId);

            if (!canSend)
                return null; // Validation failed
        }

        // Check if first message (requires approval)
        var isFirstMessage = await IsFirstMessageAsync(fromUserId, toUserId, plantAnalysisId);

        // Create message
        var newMessage = new AnalysisMessage
        {
            PlantAnalysisId = plantAnalysisId,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Message = message,
            MessageType = messageType,
            SentDate = DateTime.Now,
            IsRead = false,
            IsApproved = !isFirstMessage, // First message needs approval
            ApprovedDate = !isFirstMessage ? DateTime.Now : null,
            SenderRole = sponsorProfile != null ? "Sponsor" : "Farmer",
            SenderName = fromUser.FullName,
            SenderCompany = sponsorProfile?.CompanyName,
            Priority = "Normal",
            Category = "General",
            CreatedDate = DateTime.Now
        };

        _messageRepository.Add(newMessage);
        await _messageRepository.SaveChangesAsync();

        return newMessage;
    }
}
```

#### MessageRateLimitService.cs

```csharp
public class MessageRateLimitService : IMessageRateLimitService
{
    private readonly IAnalysisMessageRepository _messageRepository;
    private const int DAILY_MESSAGE_LIMIT = 10;

    public async Task<bool> CanSendMessageToFarmerAsync(int sponsorId, int farmerId)
    {
        var todayCount = await GetTodayMessageCountAsync(sponsorId, farmerId);
        return todayCount < DAILY_MESSAGE_LIMIT;
    }

    public async Task<int> GetTodayMessageCountAsync(int sponsorId, int farmerId)
    {
        var today = DateTime.Now.Date;
        var tomorrow = today.AddDays(1);

        var messages = await _messageRepository.GetListAsync(m =>
            m.FromUserId == sponsorId &&
            m.ToUserId == farmerId &&
            m.SentDate >= today &&
            m.SentDate < tomorrow &&
            m.SenderRole == "Sponsor"
        );

        return messages?.Count() ?? 0;
    }

    public async Task<int> GetRemainingMessagesAsync(int sponsorId, int farmerId)
    {
        var todayCount = await GetTodayMessageCountAsync(sponsorId, farmerId);
        var remaining = DAILY_MESSAGE_LIMIT - todayCount;
        return remaining > 0 ? remaining : 0;
    }
}
```

### 2. Command Handler

#### SendMessageCommand.cs

```csharp
public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, IDataResult<AnalysisMessageDto>>
{
    private readonly IAnalysisMessagingService _messagingService;

    public async Task<IDataResult<AnalysisMessageDto>> Handle(
        SendMessageCommand request, CancellationToken cancellationToken)
    {
        // Normalize field names
        var toUserId = request.ToUserId ?? request.FarmerId ?? 0;
        var messageContent = !string.IsNullOrEmpty(request.Message)
            ? request.Message : request.MessageContent;

        // COMPREHENSIVE VALIDATION
        var (canSend, errorMessage) = await _messagingService.CanSendMessageForAnalysisAsync(
            request.FromUserId,
            toUserId,
            request.PlantAnalysisId);

        if (!canSend)
        {
            return new ErrorDataResult<AnalysisMessageDto>(errorMessage);
        }

        // Send message
        var message = await _messagingService.SendMessageAsync(
            request.FromUserId,
            toUserId,
            request.PlantAnalysisId,
            messageContent,
            request.MessageType
        );

        if (message == null)
        {
            return new ErrorDataResult<AnalysisMessageDto>("Message send failed");
        }

        // Map to DTO and return
        var messageDto = new AnalysisMessageDto { /* mapping */ };
        return new SuccessDataResult<AnalysisMessageDto>(messageDto, "Message sent successfully");
    }
}
```

---

## Security & Privacy

### 1. Privacy Protection by Tier

```
┌─────────────────────────────────────────────────────────────┐
│              FARMER DATA VISIBILITY BY TIER                  │
├──────────┬──────────────┬──────────────┬────────────────────┤
│ Tier     │ Farmer Data  │ Contact Info │ Messaging          │
├──────────┼──────────────┼──────────────┼────────────────────┤
│ S        │ 30% (Anon)   │ ❌           │ ❌                 │
│ M        │ 30% (Anon)   │ ❌           │ ❌                 │
│ L        │ 60% (Full)   │ ✅           │ ✅ (10 msg/day)    │
│ XL       │ 100% (Full)  │ ✅           │ ✅ (10 msg/day)    │
└──────────┴──────────────┴──────────────┴────────────────────┘
```

### 2. Authorization Checks

```csharp
// LAYERED SECURITY MODEL

// Layer 1: API Authorization
[Authorize(Roles = "Sponsor,Admin")]
[HttpPost("messages")]
public async Task<IActionResult> SendMessage([FromBody] SendMessageCommand command)

// Layer 2: Tier-Based Authorization
if (!await CanSendMessageAsync(sponsorId))
    return Error("L/XL tier required");

// Layer 3: Ownership Authorization
if (analysis.SponsorUserId != sponsorId)
    return Error("Not your analysis");

// Layer 4: Access Record Authorization
if (hasAccess == null)
    return Error("No access record");

// Layer 5: Privacy Authorization (Block Check)
if (await IsBlockedAsync(farmerId, sponsorId))
    return Error("Farmer blocked you");

// Layer 6: Rate Limit Authorization
if (!await CanSendByRateLimit(sponsorId, farmerId))
    return Error("Daily limit exceeded");
```

### 3. Data Protection

#### Message Content
- All messages encrypted in transit (HTTPS)
- Messages stored in database with referential integrity
- Soft delete support (IsDeleted flag)
- Message history preserved for audit trail

#### Farmer Protection
- Farmers control who can message them (block/mute)
- First message approval prevents unsolicited spam
- Rate limiting prevents message flooding
- No external data sharing (in-app only)

---

## API Reference

### 1. Send Message (Sponsor to Farmer)

```http
POST /api/v1/sponsorship/messages
Authorization: Bearer {jwt_token}
Roles: Sponsor, Admin

Request Body:
{
  "toUserId": 789,              // Farmer user ID
  "plantAnalysisId": 123,        // Analysis context
  "message": "Hello! I reviewed your tomato analysis...",
  "messageType": "Information",  // Question, Answer, Recommendation, Information
  "priority": "Normal"           // Low, Normal, High, Urgent
}

Success Response (200 OK):
{
  "success": true,
  "message": "Message sent successfully",
  "data": {
    "id": 456,
    "plantAnalysisId": 123,
    "fromUserId": 456,           // Sponsor ID
    "toUserId": 789,             // Farmer ID
    "message": "Hello! I reviewed your tomato analysis...",
    "messageType": "Information",
    "isRead": false,
    "sentDate": "2025-10-17T10:30:00Z",
    "senderRole": "Sponsor",
    "senderName": "TarimTech Solutions",
    "senderCompany": "TarimTech",
    "priority": "Normal"
  }
}

Error Responses:

400 Bad Request - Tier Restriction:
{
  "success": false,
  "message": "Messaging is only available for L and XL tier sponsors"
}

400 Bad Request - Ownership Check:
{
  "success": false,
  "message": "You can only message farmers for analyses done using your sponsorship codes"
}

400 Bad Request - Rate Limit:
{
  "success": false,
  "message": "Daily message limit reached (10 messages per day per farmer). Remaining: 0"
}

400 Bad Request - Blocked:
{
  "success": false,
  "message": "This farmer has blocked messages from you"
}
```

### 2. Get Conversation

```http
GET /api/v1/sponsorship/messages/conversation?farmerId=789&plantAnalysisId=123
Authorization: Bearer {jwt_token}
Roles: Sponsor, Farmer, Admin

Success Response (200 OK):
{
  "success": true,
  "data": [
    {
      "id": 1,
      "plantAnalysisId": 123,
      "fromUserId": 456,
      "toUserId": 789,
      "message": "Hello! I reviewed your tomato analysis...",
      "messageType": "Information",
      "isRead": true,
      "sentDate": "2025-10-17T10:30:00Z",
      "readDate": "2025-10-17T11:00:00Z",
      "senderRole": "Sponsor",
      "senderName": "TarimTech Solutions",
      "senderCompany": "TarimTech"
    },
    {
      "id": 2,
      "plantAnalysisId": 123,
      "fromUserId": 789,
      "toUserId": 456,
      "message": "Thank you for the advice!",
      "messageType": "Answer",
      "isRead": false,
      "sentDate": "2025-10-17T12:00:00Z",
      "senderRole": "Farmer",
      "senderName": "Mehmet Yılmaz"
    }
  ]
}
```

### 3. Block Sponsor (Farmer Only)

```http
POST /api/v1/sponsorship/messages/block
Authorization: Bearer {jwt_token}
Roles: Farmer, Admin

Request Body:
{
  "sponsorId": 456,
  "reason": "Spam messages"
}

Success Response (200 OK):
{
  "success": true,
  "message": "Sponsor has been blocked successfully"
}
```

### 4. Unblock Sponsor (Farmer Only)

```http
DELETE /api/v1/sponsorship/messages/block/456
Authorization: Bearer {jwt_token}
Roles: Farmer, Admin

Success Response (200 OK):
{
  "success": true,
  "message": "Sponsor has been unblocked successfully"
}
```

### 5. Get Blocked Sponsors (Farmer Only)

```http
GET /api/v1/sponsorship/messages/blocked
Authorization: Bearer {jwt_token}
Roles: Farmer, Admin

Success Response (200 OK):
{
  "success": true,
  "data": [
    {
      "sponsorId": 456,
      "sponsorName": "TarimTech Solutions",
      "isBlocked": true,
      "isMuted": false,
      "blockedDate": "2025-10-17T14:00:00Z",
      "reason": "Spam messages"
    }
  ]
}
```

---

## Database Schema

### AnalysisMessage Table

```sql
CREATE TABLE AnalysisMessages (
    -- Primary Key
    Id SERIAL PRIMARY KEY,

    -- Message Context (CRITICAL: Analysis-scoped)
    PlantAnalysisId INTEGER NOT NULL REFERENCES PlantAnalyses(Id),
    FromUserId INTEGER NOT NULL REFERENCES Users(UserId),
    ToUserId INTEGER NOT NULL REFERENCES Users(UserId),

    -- Message Content
    Message TEXT NOT NULL,
    MessageType VARCHAR(50) DEFAULT 'Information', -- Question, Answer, Recommendation, Information
    Subject VARCHAR(200),
    ParentMessageId INTEGER REFERENCES AnalysisMessages(Id), -- For threaded replies

    -- Message Status
    IsRead BOOLEAN DEFAULT FALSE,
    SentDate TIMESTAMP NOT NULL DEFAULT NOW(),
    ReadDate TIMESTAMP,

    -- Sender Information
    SenderRole VARCHAR(20) NOT NULL, -- Farmer, Sponsor, Admin
    SenderName VARCHAR(100),
    SenderCompany VARCHAR(100),

    -- Priority & Classification
    Priority VARCHAR(20) DEFAULT 'Normal', -- Low, Normal, High, Urgent
    Category VARCHAR(50) DEFAULT 'General',

    -- Moderation
    IsApproved BOOLEAN DEFAULT TRUE,
    ApprovedDate TIMESTAMP,
    ApprovedBy INTEGER REFERENCES Users(UserId),

    -- Soft Delete
    IsDeleted BOOLEAN DEFAULT FALSE,
    DeletedDate TIMESTAMP,

    -- Notification Tracking
    EmailNotificationSent BOOLEAN DEFAULT FALSE,
    SmsNotificationSent BOOLEAN DEFAULT FALSE,
    PushNotificationSent BOOLEAN DEFAULT FALSE,

    -- Audit
    CreatedDate TIMESTAMP DEFAULT NOW(),
    UpdatedDate TIMESTAMP,

    -- Indexes for Performance
    INDEX idx_analysis_messages_plant_analysis (PlantAnalysisId),
    INDEX idx_analysis_messages_from_user (FromUserId),
    INDEX idx_analysis_messages_to_user (ToUserId),
    INDEX idx_analysis_messages_conversation (PlantAnalysisId, FromUserId, ToUserId),
    INDEX idx_analysis_messages_sent_date (SentDate),
    INDEX idx_analysis_messages_unread (ToUserId, IsRead) WHERE IsRead = FALSE
);
```

### FarmerSponsorBlock Table

```sql
CREATE TABLE FarmerSponsorBlocks (
    -- Primary Key
    Id SERIAL PRIMARY KEY,

    -- Block Relationship
    FarmerId INTEGER NOT NULL REFERENCES Users(UserId),
    SponsorId INTEGER NOT NULL REFERENCES Users(UserId),

    -- Block Status
    IsBlocked BOOLEAN DEFAULT FALSE,  -- Cannot send messages
    IsMuted BOOLEAN DEFAULT FALSE,    -- Can send but no notifications

    -- Metadata
    CreatedDate TIMESTAMP DEFAULT NOW(),
    Reason VARCHAR(500),

    -- Foreign Keys
    FOREIGN KEY (FarmerId) REFERENCES Users(UserId) ON DELETE RESTRICT,
    FOREIGN KEY (SponsorId) REFERENCES Users(UserId) ON DELETE RESTRICT,

    -- Unique Constraint (One block record per farmer-sponsor pair)
    UNIQUE (FarmerId, SponsorId),

    -- Indexes
    INDEX idx_farmer_sponsor_blocks_farmer (FarmerId),
    INDEX idx_farmer_sponsor_blocks_sponsor (SponsorId),
    INDEX idx_farmer_sponsor_blocks_blocked (FarmerId, SponsorId) WHERE IsBlocked = TRUE
);
```

---

## Error Handling

### Error Codes & Messages

| Error Code | Message | Solution |
|------------|---------|----------|
| `MSG_001` | Messaging is only available for L and XL tier sponsors | Upgrade to L or XL tier |
| `MSG_002` | You can only message farmers for analyses done using your sponsorship codes | Only message farmers who used your codes |
| `MSG_003` | No access record found for this analysis | Contact support - access record missing |
| `MSG_004` | This farmer has blocked messages from you | Respect farmer's decision - cannot message |
| `MSG_005` | Daily message limit reached (10 messages per day per farmer) | Wait until tomorrow or message different farmer |
| `MSG_006` | Analysis not found | Verify analysis ID is correct |
| `MSG_007` | Message send failed | Retry or contact support |
| `MSG_008` | User not found | Verify user IDs are correct |

### Validation Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│         SEND MESSAGE VALIDATION FLOW CHART                   │
└─────────────────────────────────────────────────────────────┘

START
  │
  ├─→ Is Sender a Sponsor?
  │   ├─ NO → Allow (Farmers have no restrictions)
  │   └─ YES → Continue to validations
  │
  ├─→ [VALIDATION 1] Tier Check
  │   ├─ Has L or XL tier purchase?
  │   ├─ NO → Error: MSG_001 (L/XL tier required)
  │   └─ YES → Continue
  │
  ├─→ [VALIDATION 2] Analysis Ownership
  │   ├─ Analysis.SponsorUserId == Sponsor.UserId?
  │   ├─ NO → Error: MSG_002 (Not your analysis)
  │   └─ YES → Continue
  │
  ├─→ [VALIDATION 3] Access Record
  │   ├─ SponsorAnalysisAccess record exists?
  │   ├─ NO → Error: MSG_003 (No access record)
  │   └─ YES → Continue
  │
  ├─→ [VALIDATION 4] Block Check
  │   ├─ Farmer blocked sponsor?
  │   ├─ YES → Error: MSG_004 (You are blocked)
  │   └─ NO → Continue
  │
  ├─→ [VALIDATION 5] Rate Limit
  │   ├─ Today's message count < 10?
  │   ├─ NO → Error: MSG_005 (Daily limit reached)
  │   └─ YES → Continue
  │
  ├─→ [CHECK] First Message?
  │   ├─ YES → Set IsApproved = FALSE (needs admin review)
  │   └─ NO → Set IsApproved = TRUE (auto-approved)
  │
  ├─→ CREATE MESSAGE
  │   └─ Save to database
  │
  └─→ SUCCESS
      └─ Return message DTO
```

---

## Performance Considerations

### Database Indexes
```sql
-- Critical indexes for messaging system performance
CREATE INDEX idx_analysis_messages_conversation
ON AnalysisMessages(PlantAnalysisId, FromUserId, ToUserId);

CREATE INDEX idx_analysis_messages_unread
ON AnalysisMessages(ToUserId, IsRead)
WHERE IsRead = FALSE;

CREATE INDEX idx_analysis_messages_rate_limit
ON AnalysisMessages(FromUserId, ToUserId, SentDate)
WHERE SenderRole = 'Sponsor';

CREATE INDEX idx_farmer_sponsor_blocks_active
ON FarmerSponsorBlocks(FarmerId, SponsorId)
WHERE IsBlocked = TRUE;
```

### Caching Strategy
```csharp
// Cache sponsor tier information (15 min TTL)
Cache.Set($"sponsor_tier_{sponsorId}", tierInfo, TimeSpan.FromMinutes(15));

// Cache rate limit count (1 min TTL - frequently updated)
Cache.Set($"rate_limit_{sponsorId}_{farmerId}", messageCount, TimeSpan.FromMinutes(1));

// Cache block status (5 min TTL)
Cache.Set($"block_{farmerId}_{sponsorId}", isBlocked, TimeSpan.FromMinutes(5));
```

---

## Monitoring & Analytics

### Key Metrics to Track

1. **Message Volume**
   - Messages sent per day/week/month
   - Messages by tier (L vs XL)
   - Average messages per sponsor
   - Average messages per farmer

2. **Engagement Rates**
   - % of analyses with messages
   - Farmer response rate
   - Average response time
   - Conversation length (message count per thread)

3. **Quality Metrics**
   - % of first messages approved
   - Block rate (% of farmers who block sponsors)
   - Rate limit hit rate (% of sponsors hitting 10 msg limit)
   - Mute rate vs block rate

4. **Business Intelligence**
   - Conversion rate: messages → product sales
   - Farmer satisfaction by tier
   - ROI of messaging feature for sponsors

---

## Future Enhancements

### Phase 2 Features
1. **Rich Media Support**: Image attachments in messages
2. **Message Templates**: Pre-defined templates for common scenarios
3. **Auto-translation**: Multi-language support
4. **Voice Messages**: Audio message support
5. **Read Receipts**: Real-time read status updates (SignalR)
6. **Typing Indicators**: Show when other party is typing
7. **Message Reactions**: Like/emoji reactions to messages
8. **Scheduled Messages**: Send messages at specific times
9. **Bulk Messaging**: Send same message to multiple farmers (admin feature)
10. **AI Moderation**: Automatic spam/inappropriate content detection

### Phase 3 Features
1. **Video Call Integration**: Direct video support calls
2. **Screen Sharing**: For technical troubleshooting
3. **Group Conversations**: Multi-party conversations
4. **Chatbot Integration**: AI-powered first response
5. **Analytics Dashboard**: Detailed messaging insights for sponsors

---

## Conclusion

The Sponsor-Farmer Messaging System is a **tier-based, analysis-scoped, rate-limited communication platform** that enables L/XL tier sponsors to provide technical support to farmers while protecting farmer privacy and preventing spam.

### Key Takeaways
✅ **Tier-Restricted**: Only L/XL sponsors can message
✅ **Contextual**: Messages are analysis-scoped (not open messaging)
✅ **Protected**: Farmers can block/mute unwanted sponsors
✅ **Rate-Limited**: 10 messages/day/farmer prevents spam
✅ **Moderated**: First messages require approval
✅ **Secure**: 6-layer validation ensures compliance
✅ **In-App Only**: No external SMS/WhatsApp integration

### Support & Questions
For technical implementation questions, contact the development team.
For business rule clarifications, refer to `SPONSORSHIP_BUSINESS_LOGIC.md`.
