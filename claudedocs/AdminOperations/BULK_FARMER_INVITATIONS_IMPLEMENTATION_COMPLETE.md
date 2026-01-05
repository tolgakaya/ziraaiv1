# Bulk Farmer Invitations Implementation - Complete Guide

## Overview

Bulk farmer invitation system implemented using Excel upload with asynchronous RabbitMQ processing, following the exact pattern of dealer bulk invitations.

**Implementation Date**: January 5, 2026
**Status**: ✅ COMPLETED - Build Successful
**Pattern**: Cloned from `DealerInvitation` bulk system

---

## Key Design Decisions

### 1. **Shared Database Table**
- Uses `BulkInvitationJob` table (shared with dealer invitations)
- Distinguished by `InvitationType` column:
  - Farmer: `"FarmerInvite"`
  - Dealer: `"Invite"` or `"AutoCreate"`

### 2. **Phone Number Format**
- **Dealer**: `905XXXXXXXXXX` (12 digits, international format)
- **Farmer**: `0XXXXXXXXXX` (11 digits, Turkish local format)
- Normalization handles: `+90 506 946 86 93`, `0506 946 86 93`, `5069468693`

### 3. **Code Allocation**
- **Always 1 code per farmer** (hardcoded)
- No `CodeCount` field in queue message
- Dealer system has variable `CodeCount` from Excel

### 4. **Channel Support (Limitation)**
- Bulk service accepts `channel` parameter (SMS/WhatsApp)
- **CreateFarmerInvitationCommand does NOT support Channel/CustomMessage**
- Currently defaults to SMS with standard template
- ⚠️ **Future Enhancement Required**: Need to create Channel-aware farmer invitation command

---

## Architecture

### Request Flow

```
Controller (Excel Upload)
    ↓
BulkFarmerInvitationService
    ├─ Validate Excel
    ├─ Parse rows
    ├─ Check code availability
    ├─ Create BulkInvitationJob (InvitationType="FarmerInvite")
    └─ Publish to RabbitMQ (farmer-invitation-requests)
         ↓
FarmerInvitationConsumerWorker (Background Service)
    ↓
Hangfire (Job Scheduling)
    ↓
FarmerInvitationJobService
    ├─ Validate InvitationType ⚠️ CRITICAL
    ├─ Call CreateFarmerInvitationCommand (MediatR)
    ├─ Update BulkInvitationJob progress (atomic)
    ├─ Send SignalR notifications (via HTTP → WebAPI)
    └─ Mark job complete when done
```

---

## Files Created

### 1. **Queue Message DTO**
**File**: `Entities/Dtos/FarmerInvitationQueueMessage.cs`

```csharp
public class FarmerInvitationQueueMessage
{
    public string CorrelationId { get; set; }
    public int RowNumber { get; set; }
    public int BulkJobId { get; set; }
    public int SponsorId { get; set; }

    // Farmer Information
    public string Phone { get; set; }
    public string FarmerName { get; set; }
    public string Email { get; set; }
    public string PackageTier { get; set; }
    public string Notes { get; set; }

    // Messaging Settings
    public string Channel { get; set; }         // SMS or WhatsApp
    public string CustomMessage { get; set; }

    public DateTime QueuedAt { get; set; }
}
```

**Key Differences from Dealer**:
- No `CodeCount` field (always 1)
- Has `Channel` and `CustomMessage` (not yet supported by command)

---

### 2. **Bulk Service**
**Files**:
- `Business/Services/Sponsorship/IBulkFarmerInvitationService.cs`
- `Business/Services/Sponsorship/BulkFarmerInvitationService.cs` (~550 lines)

**Key Method**:
```csharp
Task<IDataResult<BulkInvitationJobDto>> QueueBulkInvitationsAsync(
    IFormFile excelFile,
    int sponsorId,
    string channel,        // SMS or WhatsApp
    string customMessage);
```

**Excel Format**:
- **Required Column**: `Phone`
- **Optional Columns**: `FarmerName`, `Email`, `PackageTier`, `Notes`

**Phone Normalization**:
```csharp
// Input: +90 506 946 86 93, 0506 946 86 93, 5069468693
// Output: 05069468693 (11 digits)

if (cleaned.Length == 12 && cleaned.StartsWith("90"))
    return "0" + cleaned.Substring(2);  // 905069468693 → 05069468693
if (cleaned.Length == 11 && cleaned.StartsWith("0"))
    return cleaned;  // Already normalized
if (cleaned.Length == 10 && cleaned.StartsWith("5"))
    return "0" + cleaned;  // 5069468693 → 05069468693
```

**BulkInvitationJob Creation**:
```csharp
var bulkJob = new BulkInvitationJob
{
    SponsorId = sponsorId,
    InvitationType = "FarmerInvite",  // ⚠️ CRITICAL for filtering
    DefaultTier = null,
    DefaultCodeCount = 1,  // Always 1 for farmers
    SendSms = channel.ToLower() != "whatsapp",
    TotalDealers = rows.Count,  // Reusing dealer field name
    ProcessedDealers = 0,
    SuccessfulInvitations = 0,
    FailedInvitations = 0,
    Status = "Pending",
    CreatedDate = DateTime.Now,
    OriginalFileName = excelFile.FileName,
    FileSize = (int)excelFile.Length
};
```

---

### 3. **Worker Consumer**
**File**: `PlantAnalysisWorkerService/Services/FarmerInvitationConsumerWorker.cs`

**Purpose**: Background service consuming `farmer-invitation-requests` queue

**Key Pattern**:
```csharp
var invitationMessage = JsonConvert.DeserializeObject<FarmerInvitationQueueMessage>(message);

var jobId = BackgroundJob.Enqueue<IFarmerInvitationJobService>(
    service => service.ProcessFarmerInvitationAsync(invitationMessage, correlationId));

await _channel.BasicAckAsync(deliveryTag, false);
```

**Prefetch Count**: 5 messages (controlled parallelism)

---

### 4. **Job Service**
**File**: `PlantAnalysisWorkerService/Jobs/FarmerInvitationJobService.cs`

**Purpose**: Hangfire job processor for individual farmer invitations

**⚠️ CRITICAL: InvitationType Validation**:
```csharp
// 1. Get bulk job
var bulkJob = await _bulkJobRepository.GetAsync(j => j.Id == message.BulkJobId);

// 2. CRITICAL: Validate InvitationType to prevent processing dealer jobs
if (bulkJob.InvitationType != "FarmerInvite")
{
    _logger.LogError(
        "[FARMER_INVITATION_JOB_TYPE_MISMATCH] InvitationType mismatch - BulkJobId: {BulkJobId}, Expected: FarmerInvite, Actual: {ActualType}",
        message.BulkJobId, bulkJob.InvitationType);
    return;
}
```

**Command Execution**:
```csharp
var command = new CreateFarmerInvitationCommand
{
    SponsorId = message.SponsorId,
    Phone = message.Phone,
    FarmerName = message.FarmerName,
    Email = message.Email,
    CodeCount = 1,  // Always 1 for farmer invitations
    PackageTier = message.PackageTier,
    Notes = message.Notes
    // NOTE: Channel and CustomMessage NOT supported by command
};

var result = await _mediator.Send(command);
```

**Progress Tracking (Atomic)**:
```csharp
// Atomically update bulk job progress
bulkJob = await _bulkJobRepository.IncrementProgressAsync(message.BulkJobId, result.Success);

// Check if complete
bool isComplete = await _bulkJobRepository.CheckAndMarkCompleteAsync(message.BulkJobId);
```

**SignalR Notifications (Cross-Process HTTP)**:
```csharp
// Send progress notification via HTTP → WebAPI → SignalR
await SendProgressNotificationViaHttp(progressDto);

// Send completion notification if done
if (isComplete)
    await SendCompletionNotificationViaHttp(...);
```

---

## Files Modified

### 1. **Configuration Files**
Added `FarmerInvitationRequest: "farmer-invitation-requests"` to:
- `Core/Configuration/RabbitMQOptions.cs` (line 33)
- `WebAPI/appsettings.Development.json` (line 155-156)
- `WebAPI/appsettings.Staging.json` (line 75-76)
- `PlantAnalysisWorkerService/appsettings.Development.json` (line 45-46)
- `PlantAnalysisWorkerService/appsettings.Staging.json` (line 12-13)

### 2. **Controller**
**File**: `WebAPI/Controllers/SponsorshipController.cs`

**Endpoint**: `POST /api/v1/sponsorship/farmer/invitations/bulk`

**Changes**:
```csharp
// Constructor (line 53, 63, 72)
private readonly IBulkFarmerInvitationService _bulkFarmerInvitationService;

public SponsorshipController(..., IBulkFarmerInvitationService bulkFarmerInvitationService)
{
    _bulkFarmerInvitationService = bulkFarmerInvitationService;
}

// Endpoint (line 2968-3028)
[Authorize(Roles = "Sponsor,Admin")]
[HttpPost("farmer/invitations/bulk")]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IDataResult<BulkInvitationJobDto>))]
public async Task<IActionResult> BulkCreateFarmerInvitations(
    [FromForm] IFormFile excelFile,
    [FromForm] string channel = "SMS",
    [FromForm] string? customMessage = null)
{
    var result = await _bulkFarmerInvitationService.QueueBulkInvitationsAsync(
        excelFile,
        userId.Value,
        channel,
        customMessage);

    if (result.Success)
    {
        _logger.LogInformation("✅ Farmer invitations queued - JobId: {JobId}, Count: {Count}",
            result.Data?.JobId, result.Data?.TotalDealers);
        return Ok(result);
    }
}
```

### 3. **Dependency Injection**

**Business Layer** (`Business/DependencyResolvers/AutofacBusinessModule.cs` line 236-237):
```csharp
builder.RegisterType<BulkFarmerInvitationService>().As<IBulkFarmerInvitationService>()
    .InstancePerLifetimeScope();
```

**Worker Service** (`PlantAnalysisWorkerService/Program.cs`):
```csharp
// Repository (line 216)
builder.Services.AddScoped<DataAccess.Abstract.IFarmerInvitationRepository,
    DataAccess.Concrete.EntityFramework.FarmerInvitationRepository>();

// Notification Service (line 313-314)
builder.Services.AddScoped<Business.Services.Notification.IFarmerInvitationNotificationService,
    Business.Services.Notification.FarmerInvitationNotificationService>();

// Worker and Job Service (line 328-330)
builder.Services.AddHostedService<FarmerInvitationConsumerWorker>();
builder.Services.AddScoped<IFarmerInvitationJobService, FarmerInvitationJobService>();
```

### 4. **InvitationType Filtering (CRITICAL FIX)**

**Problem**: Shared `BulkInvitationJob` table could allow dealer jobs to be processed by farmer worker and vice versa.

**Solution**: Added InvitationType validation in BOTH job services

**FarmerInvitationJobService.cs** (line 61-68):
```csharp
if (bulkJob.InvitationType != "FarmerInvite")
{
    _logger.LogError(
        "[FARMER_INVITATION_JOB_TYPE_MISMATCH] InvitationType mismatch - BulkJobId: {BulkJobId}, Expected: FarmerInvite, Actual: {ActualType}",
        message.BulkJobId, bulkJob.InvitationType);
    return;
}
```

**DealerInvitationJobService.cs** (line 65-72):
```csharp
if (bulkJob.InvitationType == "FarmerInvite")
{
    _logger.LogError(
        "[DEALER_INVITATION_JOB_TYPE_MISMATCH] InvitationType mismatch - BulkJobId: {BulkJobId}, Expected: Invite/AutoCreate, Actual: {ActualType}",
        message.BulkJobId, bulkJob.InvitationType);
    return;
}
```

---

## Build Results

✅ **Build Succeeded**
- **Errors**: 0
- **Warnings**: 16 (pre-existing, unrelated to farmer invitations)
- **Solution**: All projects compiled successfully

---

## Testing Checklist

### Prerequisites
- ✅ RabbitMQ running on localhost:5672
- ✅ PostgreSQL database running
- ✅ Worker Service running (`dotnet run --project PlantAnalysisWorkerService`)
- ✅ WebAPI running (`dotnet run --project WebAPI`)

### Test Flow

#### 1. **Excel Preparation**
Create `farmers_test.xlsx` with columns:
```
Phone             | FarmerName      | Email                 | PackageTier | Notes
+90 506 946 86 93 | Ali Yılmaz     | ali@email.com        | M           | Test farmer 1
0507 123 45 67    | Ayşe Demir     | ayse@email.com       | L           | Test farmer 2
5551234567        | Mehmet Kaya    | mehmet@email.com     |             | Auto-tier
```

#### 2. **API Request (Postman)**
```http
POST https://localhost:5001/api/v1/sponsorship/farmer/invitations/bulk
Authorization: Bearer {{sponsorToken}}
Content-Type: multipart/form-data

excelFile: @farmers_test.xlsx
channel: SMS
customMessage: (optional custom message)
```

#### 3. **Expected Response**
```json
{
  "success": true,
  "message": "Toplu davet işlemi başlatıldı. JobId: 123 ile ilerlemeyi takip edebilirsiniz.",
  "data": {
    "jobId": 123,
    "totalDealers": 3,
    "status": "Pending"
  }
}
```

#### 4. **Verify RabbitMQ**
- Open RabbitMQ Management: `http://localhost:15672`
- Check `farmer-invitation-requests` queue
- Should see 3 messages published

#### 5. **Verify Worker Logs**
```
[FARMER_INVITATION_WORKER_INITIALIZED] Worker initialized - InitTime: 245ms
[FARMER_INVITATION_MESSAGE_RECEIVED] Message received - Size: 512B
[FARMER_INVITATION_JOB_START] Processing farmer invitation - Phone: 05069468693
[FARMER_INVITATION_SENDING] Sending invitation - Phone: 05069468693
[FARMER_INVITATION_JOB_SUCCESS] Invitation successful - InvitationToken: abc123...
[FARMER_INVITATION_JOB_COMPLETED] Processing completed - Duration: 1234ms
```

#### 6. **Verify Database**
```sql
-- Check BulkInvitationJob
SELECT * FROM "BulkInvitationJobs" WHERE "InvitationType" = 'FarmerInvite' ORDER BY "Id" DESC LIMIT 1;

-- Check FarmerInvitations
SELECT * FROM "FarmerInvitations" WHERE "SponsorId" = {sponsorId} ORDER BY "CreatedDate" DESC LIMIT 3;

-- Check SponsorshipCodes (should be reserved)
SELECT * FROM "SponsorshipCodes" WHERE "ReservedForFarmerInvitationId" IS NOT NULL ORDER BY "ReservedForFarmerAt" DESC LIMIT 3;
```

#### 7. **Verify SignalR Notifications**
- Check WebAPI logs for SignalR notification receipts:
```
✅ Progress notification sent successfully to WebAPI
✅ Completion notification sent successfully to WebAPI
```

---

## Known Limitations

### 1. **Channel/CustomMessage Not Supported**
- Bulk service accepts `channel` and `customMessage` parameters
- `CreateFarmerInvitationCommand` **does NOT** support these fields
- Currently defaults to SMS with standard template
- **Future Fix Required**: Create Channel-aware command or modify existing

### 2. **DTO Field Name Reuse**
- `BulkInvitationJobDto` has `TotalDealers` and `ProcessedDealers` fields
- Used for farmers despite naming (backward compatibility)
- Documentation notes added in code

### 3. **SignalR Cross-Process Communication**
- Worker Service → HTTP → WebAPI → SignalR Hub
- Requires `WebAPI:BaseUrl` configuration in Worker Service
- Requires `WebAPI:InternalSecret` for authentication

---

## Operation Claims

**Required Claims** (to be configured):
- `Sponsorship.CreateFarmerInvitation.Bulk` - Bulk farmer invitation upload
- `Sponsorship.ViewBulkJobs` - View bulk job status

**Assigned To**:
- `Sponsor` group
- `Admin` group

**SQL Script Location**: `claudedocs/AdminOperations/006_bulk_farmer_invitations_claims.sql` (if exists)

---

## API Documentation

### Endpoint

```
POST /api/v1/sponsorship/farmer/invitations/bulk
```

### Authentication
- **Required**: Yes
- **Roles**: `Sponsor`, `Admin`

### Request

**Content-Type**: `multipart/form-data`

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| excelFile | IFormFile | Yes | - | Excel file with farmer list |
| channel | string | No | "SMS" | SMS or WhatsApp |
| customMessage | string | No | null | Custom SMS template (⚠️ not yet supported) |

**Excel Format**:
- **Required Column**: `Phone`
- **Optional Columns**: `FarmerName`, `Email`, `PackageTier` (S/M/L/XL), `Notes`

### Response

**Success (200 OK)**:
```json
{
  "success": true,
  "message": "Toplu davet işlemi başlatıldı. JobId: 123 ile ilerlemeyi takip edebilirsiniz.",
  "data": {
    "jobId": 123,
    "totalDealers": 50,
    "status": "Pending"
  }
}
```

**Error (400 Bad Request)**:
```json
{
  "success": false,
  "message": "Yetersiz kod. Mevcut: 20, İstenen: 50"
}
```

### Error Codes
- `400` - Invalid Excel format, insufficient codes, validation errors
- `401` - Unauthorized (missing or invalid token)
- `403` - Forbidden (insufficient permissions)

---

## Monitoring & Debugging

### RabbitMQ Monitoring
```bash
# Check queue depth
rabbitmqadmin list queues name messages

# Purge queue (testing only)
rabbitmqadmin purge queue name=farmer-invitation-requests
```

### Database Queries
```sql
-- Active bulk jobs
SELECT * FROM "BulkInvitationJobs"
WHERE "InvitationType" = 'FarmerInvite'
  AND "Status" IN ('Pending', 'Processing')
ORDER BY "CreatedDate" DESC;

-- Job progress
SELECT
  "Id",
  "Status",
  "TotalDealers",
  "ProcessedDealers",
  "SuccessfulInvitations",
  "FailedInvitations",
  ROUND(("ProcessedDealers"::decimal / "TotalDealers"::decimal * 100), 2) AS "ProgressPercentage"
FROM "BulkInvitationJobs"
WHERE "Id" = {jobId};

-- Failed invitations from error summary
SELECT
  "Id",
  "ErrorSummary"::json
FROM "BulkInvitationJobs"
WHERE "FailedInvitations" > 0
  AND "InvitationType" = 'FarmerInvite';
```

### Logs
**Worker Service**:
```bash
tail -f PlantAnalysisWorkerService/logs/application.log | grep FARMER_INVITATION
```

**WebAPI**:
```bash
tail -f WebAPI/logs/application.log | grep "farmer/invitations/bulk"
```

---

## Deployment Checklist

### Railway Environment Variables
No new environment variables required. Uses existing:
- `RabbitMQ:ConnectionString`
- `RabbitMQ:Queues:FarmerInvitationRequest` (already in appsettings)
- `WebAPI:BaseUrl` (Worker → WebAPI communication)
- `WebAPI:InternalSecret` (Worker → WebAPI authentication)

### Database Migrations
**No manual migrations required** - uses existing tables:
- `BulkInvitationJobs` (shared with dealer)
- `FarmerInvitations` (already exists)
- `SponsorshipCodes` (already exists)

### Build & Deploy
```bash
# Staging
git push origin staging  # Auto-deploys on Railway

# Verify deployment
curl https://ziraai-api-sit.up.railway.app/health
```

---

## Future Enhancements

### 1. **Channel/CustomMessage Support**
Create new command or modify existing:
```csharp
public class CreateFarmerInvitationWithChannelCommand : IRequest<IDataResult<FarmerInvitationResponseDto>>
{
    // ... existing fields
    public string Channel { get; set; }  // SMS or WhatsApp
    public string CustomMessage { get; set; }
}
```

### 2. **WhatsApp Integration**
- Implement `IWhatsAppService` beyond mock
- Update `MessagingServiceFactory` to route based on channel
- Configure WhatsApp Business API credentials

### 3. **Bulk Job Status Endpoint**
```
GET /api/v1/sponsorship/bulk-jobs/{jobId}
```

### 4. **Result File Download**
- Generate Excel with success/failure status per row
- Upload to Cloudflare R2
- Return download URL in `ResultFileUrl` field

---

## References

### Related Files
- Single Farmer Invitation: `claudedocs/Farmers/FARMER_INVITATIONS_API_COMPLETE_REFERENCE.md`
- Dealer Bulk Pattern: `Business/Services/Sponsorship/BulkDealerInvitationService.cs`
- SignalR Integration: `claudedocs/SIGNALR_MOBILE_INTEGRATION_COMPLETE.md`
- Operation Claims: `claudedocs/AdminOperations/SECUREDOPERATION_GUIDE.md`

### Team Contacts
- **Backend**: Implementation complete, ready for integration
- **Frontend/Mobile**: Documented in this guide, follow dealer bulk pattern
- **DevOps**: Auto-deploys from staging branch

---

**Document Version**: 1.0
**Last Updated**: January 5, 2026
**Implementation Status**: ✅ COMPLETE
