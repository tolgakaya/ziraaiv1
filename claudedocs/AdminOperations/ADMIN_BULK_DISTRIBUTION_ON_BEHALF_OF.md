# Admin Bulk Code Distribution "On Behalf Of" - Implementation Plan

**Feature:** Admin'in sponsor adına bulk code distribution yapabilmesi
**Pattern:** Mevcut modern bulk distribution sistemine "on behalf of" özelliği ekleme
**Date:** 2025-11-09
**Status:** Design Complete - Ready for Implementation

---

## 📊 Executive Summary

### Problem
- **Eski sistem:** `/api/admin/sponsorship/codes/bulk-send` basit, senkron, sınırlı (max ~100 recipient)
- **Yeni sistem:** `/api/v1/sponsorship/bulk-code-distribution` modern, asenkron, güçlü (max 2000+ farmer)
- **Eksik:** Admin, sponsor adına yeni sistemin gücünden faydalanamıyor

### Çözüm
Mevcut modern bulk distribution sistemine **minimal dokunuşla** admin authorization eklemek.

### Impact
- ✅ Admin, Excel ile 2000+ farmer'a kod dağıtabilir
- ✅ Real-time progress tracking
- ✅ Result file download
- ✅ Audit logging
- ✅ Kod tekrarı YOK
- ✅ Mevcut worker service değişmez

---

## 🏗️ Architecture Comparison

### Current State: Two Separate Systems

```
┌─────────────────────────────────────────────────────────────┐
│ SPONSOR'S BULK DISTRIBUTION (Modern - Asynchronous)         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Sponsor → Excel Upload → RabbitMQ → Worker Service →      │
│            SignalR Progress → Result File Download          │
│                                                             │
│  Features:                                                  │
│  ✅ Excel upload (2000+ farmers)                            │
│  ✅ Asynchronous processing                                 │
│  ✅ Real-time progress (SignalR)                            │
│  ✅ Result file generation                                  │
│  ✅ SMS integration                                         │
│  ✅ Comprehensive error handling                            │
│                                                             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ ADMIN'S BULK SEND (Legacy - Synchronous)                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Admin → JSON Request → Direct DB Update → Response        │
│                                                             │
│  Features:                                                  │
│  ❌ JSON body only (manual recipient list)                 │
│  ❌ Synchronous (timeout risk)                             │
│  ❌ No progress tracking                                   │
│  ❌ No result file                                         │
│  ❌ Limited scalability                                    │
│  ✅ Audit logging                                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Target State: Unified System with "On Behalf Of"

```
┌─────────────────────────────────────────────────────────────┐
│ UNIFIED BULK DISTRIBUTION SYSTEM                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Sponsor ──┐                                                │
│            ├─→ Excel Upload → RabbitMQ → Worker →          │
│  Admin ────┘   (on behalf of sponsor)                       │
│                                                             │
│            ↓                                                │
│         SignalR Progress                                    │
│            ↓                                                │
│         Result File Download                                │
│            ↓                                                │
│         Audit Log (if admin)                                │
│                                                             │
│  Features:                                                  │
│  ✅ Same powerful features for both roles                   │
│  ✅ Single codebase (DRY principle)                         │
│  ✅ Audit trail for admin actions                           │
│  ✅ Authorization check (admin can override sponsor)        │
│  ✅ No code duplication                                     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎯 Recommended Approach: **Option 1 - Minimal Touch**

### Why This Approach?

1. **Zero Code Duplication** - Reuse existing robust implementation
2. **Consistent UX** - Admin and Sponsor use same powerful features
3. **Maintainability** - Single code path, single worker, single logic
4. **Scalability** - RabbitMQ + Worker already production-ready
5. **Audit Trail** - Easy to integrate with existing AdminAuditService

### Implementation Strategy

**Modify existing endpoint** to accept optional `onBehalfOfSponsorId` parameter:

```
Current Endpoint:
POST /api/v1/sponsorship/bulk-code-distribution
[Authorize(Roles = "Sponsor,Admin")] ✅ Already allows Admin!

New Behavior:
- If caller is Sponsor → use their own userId as sponsorId
- If caller is Admin → use onBehalfOfSponsorId (if provided)
- Add audit log for admin actions
```

---

## 📝 Detailed Implementation Plan

### Phase 1: Extend Existing Endpoint (1-2 hours)

#### 1.1 Add Query Parameter to Endpoint

**File:** `WebAPI/Controllers/SponsorshipController.cs`

**Current Code (line 2660):**
```csharp
[Authorize(Roles = "Sponsor,Admin")]
[HttpPost("bulk-code-distribution")]
public async Task<IActionResult> BulkDistributeCodesToFarmers(
    [FromForm] BulkCodeDistributionFormDto formData)
{
    var userId = GetUserId();
    if (!userId.HasValue)
        return Unauthorized();

    var result = await _bulkCodeDistributionService.QueueBulkCodeDistributionAsync(
        formData.ExcelFile,
        userId.Value, // 👈 Always uses caller's userId
        formData.SendSms);

    return Ok(result);
}
```

**Modified Code:**
```csharp
[Authorize(Roles = "Sponsor,Admin")]
[HttpPost("bulk-code-distribution")]
public async Task<IActionResult> BulkDistributeCodesToFarmers(
    [FromForm] BulkCodeDistributionFormDto formData,
    [FromQuery] int? onBehalfOfSponsorId = null) // 👈 NEW PARAMETER
{
    var userId = GetUserId();
    if (!userId.HasValue)
        return Unauthorized();

    var isAdmin = User.IsInRole("Admin");

    // Determine target sponsor
    int targetSponsorId;
    if (isAdmin && onBehalfOfSponsorId.HasValue)
    {
        // Admin is acting on behalf of another sponsor
        targetSponsorId = onBehalfOfSponsorId.Value;

        _logger.LogInformation(
            "🔐 Admin {AdminId} initiating bulk distribution on behalf of sponsor {SponsorId}",
            userId.Value, targetSponsorId);
    }
    else if (isAdmin && !onBehalfOfSponsorId.HasValue)
    {
        // Admin must specify sponsor when using this endpoint
        return BadRequest(new ErrorResult(
            "Admin users must specify onBehalfOfSponsorId parameter"));
    }
    else
    {
        // Regular sponsor using their own account
        targetSponsorId = userId.Value;
    }

    // Optional: Verify sponsor exists and is valid
    // (can be added if needed)

    var result = await _bulkCodeDistributionService.QueueBulkCodeDistributionAsync(
        formData.ExcelFile,
        targetSponsorId, // 👈 Can be overridden by admin
        formData.SendSms);

    // Log admin action for audit
    if (isAdmin && onBehalfOfSponsorId.HasValue && result.Success)
    {
        await _adminAuditService.LogAsync(
            action: "BulkDistributeCodes_OnBehalfOf",
            adminUserId: userId.Value,
            targetUserId: targetSponsorId,
            entityType: "BulkCodeDistributionJob",
            entityId: result.Data.JobId,
            isOnBehalfOf: true,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString(),
            requestPath: Request.Path,
            reason: $"Bulk code distribution initiated on behalf of sponsor {targetSponsorId}",
            afterState: new
            {
                JobId = result.Data.JobId,
                TotalFarmers = result.Data.TotalFarmers,
                SendSms = formData.SendSms,
                FileName = formData.ExcelFile.FileName
            }
        );
    }

    return Ok(result);
}
```

**Changes:**
1. ✅ Added `onBehalfOfSponsorId` query parameter (optional)
2. ✅ Role check: `User.IsInRole("Admin")`
3. ✅ Logic: Admin can override sponsorId
4. ✅ Validation: Admin MUST provide `onBehalfOfSponsorId`
5. ✅ Audit log for admin actions
6. ✅ No changes to service layer

---

#### 1.2 Update Other Related Endpoints (Optional but Recommended)

**Status Endpoint:**
```csharp
[HttpGet("bulk-code-distribution/status/{jobId}")]
public async Task<IActionResult> GetBulkCodeDistributionStatus(int jobId)
{
    var userId = GetUserId();
    var isAdmin = User.IsInRole("Admin");

    var result = await _bulkCodeDistributionService.GetJobStatusAsync(jobId);

    // Ownership check (skip for admin)
    if (!isAdmin && result.Data.SponsorId != userId.Value)
    {
        return Forbidden(new ErrorResult("Access denied"));
    }

    return Ok(result);
}
```

**History Endpoint:**
```csharp
[HttpGet("bulk-code-distribution/history")]
public async Task<IActionResult> GetBulkCodeDistributionHistory(
    [FromQuery] int? sponsorId = null, // 👈 Admin can specify sponsor
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    var userId = GetUserId();
    var isAdmin = User.IsInRole("Admin");

    // Determine target sponsor
    int targetSponsorId;
    if (isAdmin && sponsorId.HasValue)
    {
        targetSponsorId = sponsorId.Value;
    }
    else if (isAdmin && !sponsorId.HasValue)
    {
        // Admin viewing all jobs (can be filtered later)
        return BadRequest(new ErrorResult(
            "Admin users must specify sponsorId parameter"));
    }
    else
    {
        targetSponsorId = userId.Value;
    }

    var result = await _bulkCodeDistributionService.GetJobHistoryAsync(
        targetSponsorId, page, pageSize);

    return Ok(result);
}
```

---

### Phase 2: Update API Documentation (30 minutes)

#### 2.1 Update Swagger Documentation

**File:** `WebAPI/Controllers/SponsorshipController.cs`

Add XML comments for new parameter:

```csharp
/// <summary>
/// Upload Excel file to distribute sponsorship codes to farmers in bulk
/// Supports both Sponsor (self-service) and Admin (on behalf of) modes
/// </summary>
/// <param name="formData">Excel file and SMS preferences</param>
/// <param name="onBehalfOfSponsorId">
/// (Admin Only) Target sponsor ID when admin is acting on behalf of sponsor.
/// Required for Admin role. Ignored for Sponsor role.
/// </param>
/// <returns>Job information with JobId for tracking progress</returns>
/// <response code="200">Job created successfully</response>
/// <response code="400">Invalid request (missing file, admin without sponsorId, etc.)</response>
/// <response code="401">Unauthorized (no valid JWT token)</response>
/// <response code="403">Forbidden (sponsor has no access to specified purchase)</response>
[Authorize(Roles = "Sponsor,Admin")]
[HttpPost("bulk-code-distribution")]
public async Task<IActionResult> BulkDistributeCodesToFarmers(...)
```

#### 2.2 Update API Documentation File

**File:** `claudedocs/AdminOperations/bulk_send.md`

Update to reference new unified endpoint:

```markdown
# Admin Bulk Code Distribution (On Behalf Of Sponsor)

**Status:** ✅ Unified with Sponsor's Bulk Distribution System

## Overview
Admins can now use the same powerful bulk distribution system that sponsors use,
by acting "on behalf of" a sponsor. This provides Excel upload, asynchronous processing,
real-time progress tracking, and result file download.

## Endpoint

**POST** `/api/v1/sponsorship/bulk-code-distribution?onBehalfOfSponsorId={sponsorId}`

### Authorization
- **Role:** Admin, Sponsor
- **Token:** JWT Bearer token required

### Request

**Headers:**
```
Authorization: Bearer {admin_jwt_token}
Content-Type: multipart/form-data
```

**Query Parameters:**
- `onBehalfOfSponsorId` (required for Admin): Target sponsor ID

**Body (multipart/form-data):**
- `excelFile`: Excel file (.xlsx/.xls) with farmer details
- `sendSms`: `true` or `false` (whether to send SMS notifications)

### Excel Format

| Column Name | Required | Type   | Example              | Description                    |
|-------------|----------|--------|----------------------|--------------------------------|
| Email       | ✅       | string | farmer@example.com   | Farmer email address          |
| Phone       | ✅       | string | 905551234567         | Turkish mobile (normalized)   |
| CodeCount   | ✅       | int    | 5                    | Number of codes (1-10)        |
| FarmerName  | ❌       | string | Ahmet Yılmaz         | Farmer name (optional)        |

### Example Request (cURL)

```bash
curl -X POST "https://ziraai.com/api/v1/sponsorship/bulk-code-distribution?onBehalfOfSponsorId=159" \
  -H "Authorization: Bearer {admin_jwt_token}" \
  -F "excelFile=@farmers.xlsx" \
  -F "sendSms=true"
```

### Response

**Success (200 OK):**
```json
{
  "success": true,
  "data": {
    "jobId": 123,
    "totalFarmers": 150,
    "status": "Pending",
    "createdDate": "2025-11-09T10:00:00Z",
    "statusCheckUrl": "/api/v1/sponsorship/bulk-code-distribution/status/123"
  },
  "message": "Bulk code distribution job queued successfully"
}
```

**Error (400 Bad Request):**
```json
{
  "success": false,
  "message": "Admin users must specify onBehalfOfSponsorId parameter"
}
```

## Progress Tracking

**GET** `/api/v1/sponsorship/bulk-code-distribution/status/{jobId}`

### Response
```json
{
  "success": true,
  "data": {
    "jobId": 123,
    "sponsorId": 159,
    "status": "Processing",
    "totalFarmers": 150,
    "processedFarmers": 75,
    "successfulDistributions": 70,
    "failedDistributions": 5,
    "progressPercentage": 50,
    "totalCodesDistributed": 350,
    "startedDate": "2025-11-09T10:00:00Z",
    "estimatedTimeRemaining": "PT5M"
  }
}
```

## Job History

**GET** `/api/v1/sponsorship/bulk-code-distribution/history?sponsorId={sponsorId}`

Admin can view all jobs for a specific sponsor.

## Result File Download

**GET** `/api/v1/sponsorship/bulk-code-distribution/{jobId}/result`

Returns Excel file with success/failure status for each farmer.

## Audit Trail

All admin actions are logged with:
- Admin user ID
- Target sponsor ID
- Job ID
- Timestamp
- IP address
- User agent
- Request details

**Query admin actions:**
```sql
SELECT * FROM "AdminAuditLogs"
WHERE "Action" = 'BulkDistributeCodes_OnBehalfOf'
  AND "IsOnBehalfOf" = true
ORDER BY "CreatedDate" DESC;
```

## Migration from Old Endpoint

**Old Endpoint (Deprecated):**
```
POST /api/admin/sponsorship/codes/bulk-send
```

**New Endpoint (Recommended):**
```
POST /api/v1/sponsorship/bulk-code-distribution?onBehalfOfSponsorId={sponsorId}
```

### Migration Benefits
- ✅ Excel upload instead of manual JSON
- ✅ Asynchronous processing (no timeout risk)
- ✅ Real-time progress tracking (SignalR)
- ✅ Result file download
- ✅ Supports 2000+ farmers (vs ~100 limit)
- ✅ Comprehensive error handling
- ✅ Audit logging maintained

### Migration Checklist
- [ ] Update admin UI to use file upload instead of form fields
- [ ] Implement progress tracking UI (SignalR connection)
- [ ] Add result file download button
- [ ] Update API client to use new endpoint
- [ ] Test with small dataset (10 farmers)
- [ ] Test with large dataset (500+ farmers)
- [ ] Verify audit logs are captured correctly
```

---

### Phase 3: Testing (1-2 hours)

#### 3.1 Manual Testing Scenarios

**Test 1: Admin Uploads Excel On Behalf Of Sponsor**
```bash
# Login as Admin
POST /api/v1/auth/login
{
  "email": "admin@ziraai.com",
  "password": "admin123"
}

# Get admin JWT token
# Extract token from response

# Upload Excel on behalf of sponsor
POST /api/v1/sponsorship/bulk-code-distribution?onBehalfOfSponsorId=159
Headers:
  Authorization: Bearer {admin_jwt}
  Content-Type: multipart/form-data
Body:
  excelFile: test_farmers_10.xlsx
  sendSms: true

# Expected: 200 OK with jobId
# Expected: AdminAuditLog created
```

**Test 2: Admin Tries Without onBehalfOfSponsorId**
```bash
POST /api/v1/sponsorship/bulk-code-distribution
Headers:
  Authorization: Bearer {admin_jwt}
Body:
  excelFile: test.xlsx
  sendSms: true

# Expected: 400 Bad Request
# Message: "Admin users must specify onBehalfOfSponsorId parameter"
```

**Test 3: Sponsor Uses Endpoint (Existing Behavior)**
```bash
# Login as Sponsor
POST /api/v1/auth/login
{
  "email": "sponsor@example.com",
  "password": "sponsor123"
}

# Upload Excel (own codes)
POST /api/v1/sponsorship/bulk-code-distribution
Headers:
  Authorization: Bearer {sponsor_jwt}
Body:
  excelFile: farmers.xlsx
  sendSms: false

# Expected: 200 OK
# Expected: No audit log (normal sponsor operation)
```

**Test 4: Admin Checks Progress**
```bash
GET /api/v1/sponsorship/bulk-code-distribution/status/123
Headers:
  Authorization: Bearer {admin_jwt}

# Expected: 200 OK (admin can view any job)
```

**Test 5: Sponsor Cannot View Admin's Job**
```bash
# Sponsor tries to view job created by admin for different sponsor
GET /api/v1/sponsorship/bulk-code-distribution/status/123
Headers:
  Authorization: Bearer {sponsor_jwt}

# Expected: 403 Forbidden (if not job owner)
```

#### 3.2 Database Verification

```sql
-- Verify BulkCodeDistributionJob created with correct SponsorId
SELECT * FROM "BulkCodeDistributionJobs"
WHERE "Id" = 123;

-- Expected: SponsorId should be onBehalfOfSponsorId (159)

-- Verify AdminAuditLog created
SELECT * FROM "AdminAuditLogs"
WHERE "Action" = 'BulkDistributeCodes_OnBehalfOf'
  AND "TargetUserId" = 159
ORDER BY "CreatedDate" DESC
LIMIT 1;

-- Expected fields:
-- AdminUserId: {admin_user_id}
-- TargetUserId: 159
-- IsOnBehalfOf: true
-- AfterState: JSON with JobId, TotalFarmers, etc.
```

#### 3.3 Worker Service Verification

```bash
# Worker logs should show processing
# Worker does NOT need to know if admin or sponsor initiated the job
# Worker only cares about SponsorId in the message

# Check worker logs
docker logs ziraai-worker-service -f | grep "Processing farmer code distribution"

# Expected: Processing messages for JobId 123
# Expected: Success/failure counts incrementing
```

---

### Phase 4: Documentation & Deployment (1 hour)

#### 4.1 Update Postman Collection

Add new example request:

```
Collection: ZiraAI Admin Operations
Folder: Sponsorship Management
Request: Bulk Code Distribution (On Behalf Of)

URL: {{baseUrl}}/api/v1/sponsorship/bulk-code-distribution?onBehalfOfSponsorId=159
Method: POST
Auth: Bearer Token ({{adminToken}})
Body: form-data
  - excelFile: [file]
  - sendSms: true

Tests:
  pm.test("Status code is 200", function () {
      pm.response.to.have.status(200);
  });

  pm.test("Response has jobId", function () {
      var jsonData = pm.response.json();
      pm.expect(jsonData.data.jobId).to.be.a('number');
  });
```

#### 4.2 Update OpenAPI/Swagger Spec

Ensure Swagger UI shows:
- New `onBehalfOfSponsorId` parameter
- Parameter constraints (required for Admin)
- Example values
- Response schemas

#### 4.3 Create Admin UI Guide

**File:** `claudedocs/AdminOperations/ADMIN_BULK_DISTRIBUTION_UI_GUIDE.md`

```markdown
# Admin UI Guide: Bulk Code Distribution On Behalf Of Sponsor

## UI Flow

1. **Select Sponsor**
   - Dropdown or search field
   - Show sponsor name, ID, tier
   - Display available codes count

2. **Upload Excel**
   - File picker (accept: .xlsx, .xls)
   - Max size: 5MB
   - Show preview of first 5 rows

3. **Configure Options**
   - Checkbox: "Send SMS to farmers"
   - Display: Estimated SMS cost (if enabled)

4. **Submit**
   - API call with form-data
   - Show loading spinner
   - On success: Redirect to progress page

5. **Progress Tracking**
   - Connect to SignalR hub
   - Real-time progress bar
   - Display counts: Total, Processed, Success, Failed
   - ETA countdown

6. **Completion**
   - Success message
   - Download result file button
   - Link to job history

## Example API Calls

### Upload Request (JavaScript)
```javascript
const formData = new FormData();
formData.append('excelFile', fileInput.files[0]);
formData.append('sendSms', sendSmsCheckbox.checked);

const response = await fetch(
  `/api/v1/sponsorship/bulk-code-distribution?onBehalfOfSponsorId=${sponsorId}`,
  {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${adminToken}`
    },
    body: formData
  }
);

const result = await response.json();
if (result.success) {
  const jobId = result.data.jobId;
  redirectToProgressPage(jobId);
}
```

### Progress Polling (SignalR)
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/bulk-operations")
  .build();

connection.on("FarmerCodeDistributionProgress", (data) => {
  if (data.jobId === currentJobId) {
    updateProgressBar(data.progressPercentage);
    updateStats({
      processed: data.processedFarmers,
      success: data.successfulDistributions,
      failed: data.failedDistributions
    });
  }
});

await connection.start();
```
```

---

## 🚀 Deployment Plan

### Pre-Deployment Checklist

- [ ] **Code Review:** Review modified endpoint logic
- [ ] **Build Test:** Ensure no compilation errors
- [ ] **Unit Tests:** Add tests for new parameter logic
- [ ] **Integration Tests:** Test admin vs sponsor behavior
- [ ] **Database Check:** Verify audit log table exists
- [ ] **Worker Service Check:** Confirm worker is running
- [ ] **RabbitMQ Check:** Confirm queue is healthy

### Deployment Steps

1. **Deploy API Changes**
   ```bash
   # Push code to feature branch
   git checkout -b feature/admin-bulk-distribution-on-behalf-of
   git add .
   git commit -m "feat: Add on-behalf-of support to bulk distribution"
   git push origin feature/admin-bulk-distribution-on-behalf-of

   # Deploy to staging
   # (Railway auto-deploy or manual)

   # Test on staging
   # Run test suite

   # Merge to main
   # Deploy to production
   ```

2. **No Worker Changes Needed** ✅
   - Worker service does not need updates
   - RabbitMQ message format unchanged
   - Worker only cares about `sponsorId` in message

3. **Update Admin UI**
   - Deploy new UI components
   - Add Excel upload widget
   - Add progress tracking page
   - Update API client

4. **Monitor Logs**
   ```bash
   # API logs
   kubectl logs -f deployment/ziraai-api -n production | grep "BulkDistributeCodes_OnBehalfOf"

   # Worker logs
   kubectl logs -f deployment/ziraai-worker -n production | grep "farmer-code-distribution"

   # RabbitMQ queue depth
   # Check management UI: http://rabbitmq:15672
   ```

---

## 📊 Success Metrics

### KPIs to Track

| Metric | Target | How to Measure |
|--------|--------|---------------|
| **Admin Usage** | 10+ jobs/week | Count from `AdminAuditLogs` |
| **Success Rate** | >95% | SuccessfulDistributions / TotalFarmers |
| **Processing Speed** | <30s per 100 farmers | CompletedDate - StartedDate |
| **SMS Delivery** | >90% | TotalSmsSent / (SuccessfulDistributions * CodeCount) |
| **Admin Satisfaction** | Positive feedback | Survey/interviews |

### Monitoring Queries

```sql
-- Admin usage stats (last 30 days)
SELECT
    COUNT(*) as total_jobs,
    SUM(CAST("AfterState"->>'TotalFarmers' AS INT)) as total_farmers,
    COUNT(DISTINCT "AdminUserId") as unique_admins
FROM "AdminAuditLogs"
WHERE "Action" = 'BulkDistributeCodes_OnBehalfOf'
  AND "CreatedDate" >= NOW() - INTERVAL '30 days';

-- Success rate by admin
SELECT
    "AdminUserId",
    COUNT(*) as jobs_initiated,
    AVG(j."SuccessfulDistributions"::FLOAT / NULLIF(j."TotalFarmers", 0) * 100) as avg_success_rate
FROM "AdminAuditLogs" a
JOIN "BulkCodeDistributionJobs" j ON j."Id" = CAST(a."AfterState"->>'JobId' AS INT)
WHERE a."Action" = 'BulkDistributeCodes_OnBehalfOf'
GROUP BY "AdminUserId"
ORDER BY avg_success_rate DESC;
```

---

## 🔐 Security Considerations

### Authorization Matrix

| Role | Can Upload Excel | Can Specify Sponsor | Can View Any Job | Audit Logged |
|------|------------------|---------------------|------------------|--------------|
| **Sponsor** | ✅ (own codes) | ❌ (only self) | ❌ (own jobs only) | ❌ |
| **Admin** | ✅ (on behalf of) | ✅ (required) | ✅ (all jobs) | ✅ |
| **Farmer** | ❌ | ❌ | ❌ | ❌ |

### Potential Risks & Mitigations

1. **Risk:** Admin specifies invalid/non-existent sponsorId
   - **Mitigation:** Add sponsor validation in service layer
   - **Code:**
     ```csharp
     var sponsor = await _userRepository.GetAsync(u =>
         u.Id == targetSponsorId &&
         u.UserType == "Sponsor");
     if (sponsor == null)
         return new ErrorResult("Invalid sponsor ID");
     ```

2. **Risk:** Admin distributes more codes than sponsor has
   - **Mitigation:** Already handled by existing validation in `BulkCodeDistributionService`
   - **Code:** Service checks `availableCodes.Count >= totalCodesRequired`

3. **Risk:** Sponsor tries to use `onBehalfOfSponsorId` to access other sponsor's codes
   - **Mitigation:** Role check ensures only Admin can override sponsorId
   - **Code:** `if (!isAdmin && onBehalfOfSponsorId.HasValue) return Forbidden();`

4. **Risk:** Admin actions not audited
   - **Mitigation:** All admin actions logged to `AdminAuditLogs`
   - **Code:** Audit log created after successful job creation

---

## 📚 References

### Related Files
- `Business/Services/Sponsorship/BulkCodeDistributionService.cs` - Service logic (NO CHANGES)
- `PlantAnalysisWorkerService/Services/FarmerCodeDistributionConsumerWorker.cs` - Worker (NO CHANGES)
- `Entities/Concrete/BulkCodeDistributionJob.cs` - Entity (NO CHANGES)
- `WebAPI/Controllers/SponsorshipController.cs` - Main endpoint (MODIFIED)

### Related Features
- Bulk Dealer Invitation (similar pattern)
- Admin Audit Logging
- RabbitMQ message queue
- SignalR real-time notifications

### Design Documents
- `claudedocs/BULK_FARMER_CODE_DISTRIBUTION_DESIGN.md` - Original design
- `claudedocs/AdminOperations/bulk_send.md` - Old endpoint (to be updated)

---

## ✅ Implementation Checklist

### Code Changes
- [ ] Modify `SponsorshipController.BulkDistributeCodesToFarmers` endpoint
- [ ] Add `onBehalfOfSponsorId` query parameter
- [ ] Add role check (`User.IsInRole("Admin")`)
- [ ] Add sponsor validation logic
- [ ] Add audit logging for admin actions
- [ ] Update XML comments for Swagger

### Optional Enhancements
- [ ] Modify status endpoint to allow admin access
- [ ] Modify history endpoint to accept `sponsorId` parameter
- [ ] Add sponsor validation service method
- [ ] Add unit tests for new logic

### Documentation
- [ ] Update `claudedocs/AdminOperations/bulk_send.md`
- [ ] Create `ADMIN_BULK_DISTRIBUTION_UI_GUIDE.md`
- [ ] Update Swagger/OpenAPI spec
- [ ] Update Postman collection
- [ ] Create admin user guide

### Testing
- [ ] Test: Admin uploads Excel with `onBehalfOfSponsorId`
- [ ] Test: Admin without `onBehalfOfSponsorId` gets error
- [ ] Test: Sponsor uses endpoint normally
- [ ] Test: Admin can view any job status
- [ ] Test: Audit log created correctly
- [ ] Test: Worker processes job successfully
- [ ] Test: Result file downloadable

### Deployment
- [ ] Code review
- [ ] Merge to feature branch
- [ ] Deploy to staging
- [ ] Run integration tests
- [ ] Deploy to production
- [ ] Monitor logs
- [ ] Update admin UI

---

**Document Version:** 1.0
**Last Updated:** 2025-11-09
**Status:** ✅ Ready for Implementation
**Estimated Effort:** 4-6 hours (backend + testing)
