# Bulk Farmer Code Distribution - Detailed Design

**Feature**: Excel-based bulk code distribution to farmers
**Pattern**: Same as Bulk Dealer Invitation (RabbitMQ + Worker + SignalR)
**Date**: 2025-11-05
**Status**: Design Phase

---

## 📋 Feature Overview

### Purpose
Allow sponsors to distribute sponsorship codes to multiple farmers at once via Excel upload.

### Key Features
- Excel file upload with farmer details (phone, name, tier, code count)
- Asynchronous processing via RabbitMQ worker
- Real-time progress updates via SignalR
- Result file generation (success/failure per farmer)
- SMS delivery optional (like dealer invitations)

---

## 🏗️ Architecture (Based on Bulk Dealer Invitation Pattern)

```
┌─────────────┐
│   API       │
│  (Upload)   │
└──────┬──────┘
       │ 1. Validate Excel
       │ 2. Create Job Record
       │ 3. Publish to RabbitMQ
       ├─────────────────────────┐
       │                         │
       ▼                         ▼
┌──────────────┐         ┌──────────────┐
│  RabbitMQ    │         │  Database    │
│    Queue     │         │  (Job Info)  │
└──────┬───────┘         └──────────────┘
       │
       │ 4. Consumer picks message
       ▼
┌──────────────┐
│    Worker    │
│   Service    │
└──────┬───────┘
       │ 5. Process each farmer:
       │    - Validate farmer
       │    - Assign codes
       │    - Send SMS (optional)
       │    - Update progress
       │
       │ 6. HTTP callback to API
       ▼
┌──────────────┐
│     API      │
│  (Callback)  │
└──────┬───────┘
       │
       │ 7. SignalR notification
       ▼
┌──────────────┐
│   Frontend   │
│  (Real-time) │
└──────────────┘
```

---

## 📊 Database Schema

### New Entity: `BulkCodeDistributionJob`

```csharp
public class BulkCodeDistributionJob : IEntity
{
    public int Id { get; set; }

    // Sponsor Information
    public int SponsorId { get; set; }

    // Configuration
    public int PurchaseId { get; set; }          // Which package to use
    public bool SendSms { get; set; }            // SMS delivery option
    public string DeliveryMethod { get; set; }   // "Direct", "SMS", "Both"

    // Progress Tracking
    public int TotalFarmers { get; set; }
    public int ProcessedFarmers { get; set; }
    public int SuccessfulDistributions { get; set; }
    public int FailedDistributions { get; set; }

    // Status: Pending, Processing, Completed, PartialSuccess, Failed
    public string Status { get; set; } = "Pending";

    // Timestamps
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    // File Information
    public string OriginalFileName { get; set; }
    public int FileSize { get; set; }

    // Results
    public string ResultFileUrl { get; set; }     // Download result Excel
    public string ErrorSummary { get; set; }      // JSON array of errors

    // Statistics
    public int TotalCodesDistributed { get; set; }
    public int TotalSmsSent { get; set; }
}
```

---

## 📄 Excel Format

### Required Columns (Header-based like dealer invitations)

| Column Name      | Type   | Required | Example              | Description                    |
|------------------|--------|----------|----------------------|--------------------------------|
| Phone            | string | ✅       | 905551234567         | Farmer phone (normalized)      |
| Name             | string | ✅       | Ahmet Yılmaz         | Farmer name                    |
| CodeCount        | int    | ✅       | 10                   | Number of codes to distribute  |
| Notes            | string | ❌       | Kırklareli çiftçisi  | Optional notes                 |

### Example Excel

```
Phone           | Name          | CodeCount | Notes
----------------|---------------|-----------|---------------------
905551234567    | Ahmet Yılmaz  | 10        | Kırklareli
905559876543    | Mehmet Kaya   | 5         | Edirne
905551112233    | Ayşe Demir    | 15        | Tekirdağ
```

### Validation Rules

1. **Phone**:
   - Format: 12 digits starting with 90 (Turkey)
   - Auto-normalize: `5551234567` → `905551234567`
   - Validate uniqueness in Excel (no duplicates)

2. **CodeCount**:
   - Minimum: 1
   - Maximum: Per farmer limit (e.g., 100)
   - Must not exceed sponsor's available codes

3. **Name**:
   - Minimum 2 characters
   - Maximum 100 characters

---

## 🔄 API Endpoints

### 1. Upload Excel for Bulk Distribution

```http
POST /api/v1/sponsorship/bulk-code-distribution
Content-Type: multipart/form-data
Authorization: Bearer {token}
```

**Request**:
```json
{
  "excelFile": <file>,
  "purchaseId": 26,
  "sendSms": true,
  "deliveryMethod": "Both"  // "Direct", "SMS", "Both"
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "jobId": 123,
    "totalFarmers": 150,
    "totalCodesRequired": 1500,
    "availableCodes": 2000,
    "status": "Pending",
    "createdDate": "2025-11-05T10:00:00Z",
    "estimatedCompletionTime": "2025-11-05T10:15:00Z"
  },
  "message": "Bulk code distribution job queued successfully"
}
```

### 2. Get Job Status

```http
GET /api/v1/sponsorship/bulk-code-distribution/{jobId}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "jobId": 123,
    "status": "Processing",
    "totalFarmers": 150,
    "processedFarmers": 75,
    "successfulDistributions": 70,
    "failedDistributions": 5,
    "progressPercentage": 50,
    "totalCodesDistributed": 700,
    "startedDate": "2025-11-05T10:00:00Z",
    "estimatedTimeRemaining": "PT7M30S"
  }
}
```

### 3. Get Job History

```http
GET /api/v1/sponsorship/bulk-code-distribution/history?page=1&pageSize=20
```

**Response**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "jobId": 123,
        "fileName": "farmers_november.xlsx",
        "totalFarmers": 150,
        "successfulDistributions": 145,
        "failedDistributions": 5,
        "status": "Completed",
        "createdDate": "2025-11-05T10:00:00Z",
        "completedDate": "2025-11-05T10:15:00Z",
        "resultFileUrl": "https://..."
      }
    ],
    "totalCount": 10,
    "page": 1,
    "pageSize": 20
  }
}
```

### 4. Download Result File

```http
GET /api/v1/sponsorship/bulk-code-distribution/{jobId}/result
```

Returns Excel file with results (success/failure per row)

---

## 🐰 RabbitMQ Integration

### Queue Configuration

**Queue Name**: `farmer_code_distribution_queue`

**Message Format**:
```json
{
  "jobId": 123,
  "sponsorId": 159,
  "purchaseId": 26,
  "sendSms": true,
  "deliveryMethod": "Both",
  "farmers": [
    {
      "rowNumber": 1,
      "phone": "905551234567",
      "name": "Ahmet Yılmaz",
      "codeCount": 10,
      "notes": "Kırklareli çiftçisi"
    }
  ],
  "callbackUrl": "https://ziraai-api/api/v1/bulk-operations/code-distribution-callback"
}
```

### RabbitMQ Settings (appsettings.json)

```json
{
  "RabbitMQ": {
    "Queues": {
      "FarmerCodeDistribution": "farmer_code_distribution_queue"
    }
  }
}
```

---

## ⚙️ Worker Service Processing Logic

### Processing Flow

```csharp
// Pseudocode
foreach (var farmer in job.Farmers)
{
    try
    {
        // 1. Validate farmer phone
        if (!IsValidPhone(farmer.Phone))
        {
            RecordError(farmer.RowNumber, "Invalid phone number");
            continue;
        }

        // 2. Get available codes from sponsor's purchase
        var codes = await GetAvailableCodesAsync(
            sponsorId: job.SponsorId,
            purchaseId: job.PurchaseId,
            count: farmer.CodeCount
        );

        if (codes.Count < farmer.CodeCount)
        {
            RecordError(farmer.RowNumber, $"Insufficient codes (need {farmer.CodeCount}, have {codes.Count})");
            continue;
        }

        // 3. Mark codes as distributed
        foreach (var code in codes)
        {
            code.DistributionDate = DateTime.Now;
            code.DistributedToPhone = farmer.Phone;
            code.DistributedToName = farmer.Name;
            code.Notes = farmer.Notes;
            await _codeRepository.UpdateAsync(code);
        }

        // 4. Send SMS if requested
        if (job.SendSms)
        {
            var smsResult = await SendCodesBySmsAsync(farmer.Phone, codes);
            if (!smsResult.Success)
            {
                _logger.LogWarning("SMS failed for farmer {Phone}", farmer.Phone);
            }
        }

        // 5. Record success
        RecordSuccess(farmer.RowNumber, codes.Count);

        // 6. Update progress (every 10 farmers or 5 seconds)
        if (ShouldSendProgressUpdate())
        {
            await SendProgressUpdateAsync(jobId);
        }
    }
    catch (Exception ex)
    {
        RecordError(farmer.RowNumber, ex.Message);
    }
}

// 7. Generate result Excel file
await GenerateResultFileAsync(jobId);

// 8. Send completion callback to API
await SendCompletionCallbackAsync(jobId);
```

---

## 🔔 SignalR Real-Time Notifications

### Hub: `BulkOperationsHub`

**Events**:

1. **JobStarted**
```json
{
  "jobId": 123,
  "jobType": "FarmerCodeDistribution",
  "totalFarmers": 150,
  "message": "Code distribution started"
}
```

2. **ProgressUpdate**
```json
{
  "jobId": 123,
  "processedFarmers": 75,
  "successfulDistributions": 70,
  "failedDistributions": 5,
  "progressPercentage": 50
}
```

3. **JobCompleted**
```json
{
  "jobId": 123,
  "status": "Completed",
  "totalFarmers": 150,
  "successfulDistributions": 145,
  "failedDistributions": 5,
  "totalCodesDistributed": 1450,
  "resultFileUrl": "https://...",
  "completedDate": "2025-11-05T10:15:00Z"
}
```

### Frontend Connection

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/bulk-operations")
    .build();

connection.on("FarmerCodeDistributionProgress", (data) => {
    updateProgressBar(data.progressPercentage);
    updateStats(data);
});

connection.on("FarmerCodeDistributionCompleted", (data) => {
    showCompletionNotification(data);
    enableDownloadButton(data.resultFileUrl);
});
```

---

## 📁 File Structure

### Backend Files to Create

```
Business/
├── Services/
│   └── Sponsorship/
│       ├── BulkCodeDistributionService.cs          (Main service)
│       └── IBulkCodeDistributionService.cs         (Interface)
│
├── Handlers/
│   └── Sponsorship/
│       ├── Commands/
│       │   └── QueueBulkCodeDistributionCommand.cs
│       └── Queries/
│           ├── GetBulkCodeDistributionJobStatusQuery.cs
│           └── GetBulkCodeDistributionJobHistoryQuery.cs
│
└── DependencyResolvers/
    └── AutofacBusinessModule.cs                     (Register services)

Entities/
├── Concrete/
│   └── BulkCodeDistributionJob.cs
│
└── Dtos/
    ├── BulkCodeDistributionJobDto.cs
    └── BulkCodeDistributionProgressDto.cs

DataAccess/
├── Abstract/
│   └── IBulkCodeDistributionJobRepository.cs
│
├── Concrete/
│   ├── EntityFramework/
│   │   └── BulkCodeDistributionJobRepository.cs
│   └── Configurations/
│       └── BulkCodeDistributionJobEntityConfiguration.cs

WebAPI/
└── Controllers/
    └── BulkOperationsController.cs                  (New endpoints)

PlantAnalysisWorkerService/
├── Services/
│   └── FarmerCodeDistributionConsumerWorker.cs     (RabbitMQ consumer)
│
└── Jobs/
    └── FarmerCodeDistributionJob.cs                (Hangfire job)
```

---

## 🧪 Implementation Phases

### Phase 1: Database & Entities
- [ ] Create `BulkCodeDistributionJob` entity
- [ ] Create DTOs (JobDto, ProgressDto)
- [ ] Create repository interface and implementation
- [ ] Create EF configuration
- [ ] Add migration

### Phase 2: Business Logic (API Side)
- [ ] Create `BulkCodeDistributionService`
- [ ] Implement Excel parsing (header-based)
- [ ] Implement validation logic
- [ ] Implement code availability check
- [ ] Create CQRS command/queries

### Phase 3: API Endpoints
- [ ] POST `/bulk-code-distribution` - Upload Excel
- [ ] GET `/bulk-code-distribution/{jobId}` - Get status
- [ ] GET `/bulk-code-distribution/history` - Get history
- [ ] GET `/bulk-code-distribution/{jobId}/result` - Download result
- [ ] POST `/bulk-operations/code-distribution-callback` - Worker callback

### Phase 4: RabbitMQ Integration
- [ ] Add queue configuration to `RabbitMQOptions`
- [ ] Publish message from API to queue
- [ ] Add queue setup in worker service startup

### Phase 5: Worker Service
- [ ] Create `FarmerCodeDistributionConsumerWorker`
- [ ] Create `FarmerCodeDistributionJob` (Hangfire)
- [ ] Implement farmer processing logic
- [ ] Implement SMS sending (optional)
- [ ] Implement result file generation
- [ ] Implement HTTP callback to API

### Phase 6: SignalR Notifications
- [ ] Extend `BulkOperationsHub`
- [ ] Implement progress update events
- [ ] Implement completion event
- [ ] Test real-time updates

### Phase 7: Testing
- [ ] Unit tests for service logic
- [ ] Integration tests for API endpoints
- [ ] End-to-end test with Excel upload
- [ ] Test SMS delivery
- [ ] Test error scenarios

---

## 🔐 Security & Validation

### Authorization
- Only sponsors can upload farmer distributions
- Sponsors can only view their own jobs
- Validate sponsor has access to specified purchase

### Validation Rules
1. **File Size**: Max 5MB (same as dealer invitations)
2. **Row Count**: Max 2000 farmers per file
3. **Code Availability**: Total codes required must not exceed available codes
4. **Phone Format**: Must be valid Turkish phone (90xxxxxxxxxx)
5. **Duplicate Detection**: No duplicate phones in same Excel

---

## 📊 Success Metrics

### KPIs to Track
- Average processing time per farmer
- Success rate (%)
- SMS delivery rate (%)
- Code distribution count per job
- Job completion rate

### Monitoring
- Log all critical steps
- Track RabbitMQ message processing time
- Monitor worker service health
- Alert on high failure rates (>10%)

---

## 🚀 Deployment Considerations

### Environment Configuration
- **Development**: Local RabbitMQ + Worker
- **Staging**: Railway RabbitMQ + Staging Worker
- **Production**: Railway RabbitMQ + Production Worker

### Rollout Plan
1. Deploy database migration
2. Deploy API changes
3. Deploy worker service
4. Test with small Excel file (10 farmers)
5. Gradual rollout to production

---

## 📝 Next Steps

1. ✅ Create design document (this file)
2. ⏳ Create feature branch: `feature/bulk-farmer-code-distribution`
3. ⏳ Implement Phase 1 (Database & Entities)
4. ⏳ Continue with remaining phases

---

**Document Version**: 1.0
**Last Updated**: 2025-11-05
**Status**: Ready for Implementation
