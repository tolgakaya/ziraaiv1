# Admin Bulk Subscription Assignment - Integration Guide

**Version:** 1.0
**Date:** 2025-01-10
**Author:** ZiraAI Development Team

---

## Table of Contents
1. [Overview](#overview)
2. [System Architecture](#system-architecture)
3. [Authentication & Authorization](#authentication--authorization)
4. [API Endpoints](#api-endpoints)
5. [Excel File Format](#excel-file-format)
6. [Integration Flow](#integration-flow)
7. [Response Codes](#response-codes)
8. [Error Handling](#error-handling)
9. [Testing Guide](#testing-guide)
10. [Frontend Implementation Examples](#frontend-implementation-examples)

---

## Overview

### Purpose
Admin Bulk Subscription Assignment allows administrators to upload an Excel file containing farmer information and automatically assign subscriptions to multiple farmers in a single operation.

### Key Features
- ✅ Excel-based batch subscription assignment
- ✅ Auto-create user accounts if they don't exist
- ✅ Update existing subscriptions or create new ones
- ✅ Real-time progress tracking
- ✅ SMS/Email notifications support
- ✅ Job history and result file download
- ✅ Async processing with RabbitMQ + Hangfire

### Use Cases
1. **Onboarding Campaign**: Assign trial subscriptions to 1000+ new farmers
2. **Subscription Renewal**: Bulk renew expiring subscriptions
3. **Promotional Campaign**: Assign premium tiers to specific farmer segments
4. **Partner Integration**: Import subscriptions from external systems

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                          ADMIN FRONTEND                             │
│  (React/Angular/Vue - Web Dashboard OR Flutter - Mobile Admin App) │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             │ 1. POST Excel File
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     WEBAPI (AdminBulkSubscriptionController)        │
│  • Validate JWT Token                                               │
│  • Check Admin Role                                                 │
│  • Forward to QueueBulkSubscriptionAssignmentCommand                │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             │ 2. CQRS Command Handler
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│              BUSINESS LAYER (BulkSubscriptionAssignmentService)     │
│  • Validate Excel (size, format, columns)                           │
│  • Parse rows with header-based mapping                             │
│  • Validate subscription tiers and durations                         │
│  • Create BulkSubscriptionAssignmentJob entity                       │
│  • Publish N messages to RabbitMQ (1 per farmer)                    │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             │ 3. Publish to Queue
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    RABBITMQ (Message Queue)                          │
│  Queue: farmer-subscription-assignment-requests                      │
│  • Message per farmer with subscription details                      │
│  • Correlation ID = BulkJobId                                        │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             │ 4. Consume Messages
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│     WORKER SERVICE (FarmerSubscriptionAssignmentConsumerWorker)     │
│  • Listen to RabbitMQ queue                                          │
│  • Enqueue Hangfire background jobs                                  │
│  • Process 5 messages concurrently (prefetch limit)                  │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             │ 5. Background Processing
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│      HANGFIRE JOB (FarmerSubscriptionAssignmentJobService)          │
│  • Lookup user by email/phone                                        │
│  • Create user if doesn't exist                                      │
│  • Create/Update subscription with atomic operations                 │
│  • Send SMS/Email notifications (optional)                           │
│  • Update job progress atomically                                    │
│  • Send HTTP callback to WebAPI for SignalR broadcasting             │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             │ 6. Real-time Updates (Optional)
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       SIGNALR HUB (WebAPI)                           │
│  • Broadcast progress updates to connected admin clients             │
│  • Push completion notifications                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Authentication & Authorization

### Required Headers
```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: multipart/form-data
```

### JWT Token Requirements
- **Role:** `Admin` (required)
- **Claims:** Must have valid `NameIdentifier` (UserId)

### Operation Claims (Database-Level)
The following operation claims must be assigned to the admin's group (typically GroupId = 1 for Administrators):

| Claim ID | Claim Name | Description |
|----------|------------|-------------|
| 159 | `QueueBulkSubscriptionAssignmentCommand` | Upload Excel and queue bulk job |
| 160 | `GetBulkSubscriptionAssignmentStatusQuery` | Check job status/progress |
| 161 | `GetBulkSubscriptionAssignmentHistoryQuery` | View job history |
| 162 | `GetBulkSubscriptionAssignmentResultQuery` | Download result file |

**⚠️ IMPORTANT:** Admins must **logout and login** after operation claims are added to refresh the claims cache.

### How to Execute Operation Claims SQL
```sql
-- Execute on staging/production database
-- File: claudedocs/AdminOperations/002_admin_bulk_subscription_operation_claims.sql

-- This script creates claims 159-162 and assigns them to Administrators group
-- Run verification queries at the end to confirm success
```

---

## API Endpoints

### Base URL
- **Development:** `https://localhost:5001/api/v1/admin/subscriptions`
- **Staging:** `https://ziraai-api-sit.up.railway.app/api/v1/admin/subscriptions`
- **Production:** `https://api.ziraai.com/api/v1/admin/subscriptions`

---

### 1. Queue Bulk Subscription Assignment

**Endpoint:** `POST /bulk-assignment`

**Description:** Upload Excel file and queue bulk subscription assignment job.

#### Request

**Headers:**
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: multipart/form-data
```

**Form Data:**
```
excelFile: <FILE> (required)
  - Type: IFormFile
  - Max Size: 5 MB
  - Formats: .xlsx, .xls
  - Max Rows: 2000

defaultTierId: <INTEGER> (optional)
  - Subscription tier ID to use when Excel doesn't specify TierName
  - Example: 2 (for "S" tier)

defaultDurationDays: <INTEGER> (optional)
  - Duration in days to use when Excel doesn't specify DurationDays
  - Example: 30

sendNotification: <BOOLEAN> (optional, default: true)
  - Whether to send notifications to farmers

notificationMethod: <STRING> (optional, default: "Email")
  - Values: "SMS" | "Email"

autoActivate: <BOOLEAN> (optional, default: true)
  - Whether to auto-activate subscriptions (set IsActive=true)
```

**Example (cURL):**
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/admin/subscriptions/bulk-assignment" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "excelFile=@farmers_subscription_bulk.xlsx" \
  -F "defaultTierId=2" \
  -F "defaultDurationDays=30" \
  -F "sendNotification=true" \
  -F "notificationMethod=SMS" \
  -F "autoActivate=true"
```

**Example (JavaScript Fetch):**
```javascript
const formData = new FormData();
formData.append('excelFile', fileInput.files[0]);
formData.append('defaultTierId', '2');
formData.append('defaultDurationDays', '30');
formData.append('sendNotification', 'true');
formData.append('notificationMethod', 'SMS');
formData.append('autoActivate', 'true');

const response = await fetch('https://ziraai-api-sit.up.railway.app/api/v1/admin/subscriptions/bulk-assignment', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${jwtToken}`
  },
  body: formData
});

const result = await response.json();
```

#### Response

**Success (200 OK):**
```json
{
  "data": {
    "jobId": 42,
    "totalFarmers": 150,
    "status": "Processing",
    "createdDate": "2025-01-10T14:30:00Z",
    "estimatedCompletionTime": "2025-01-10T15:45:00Z",
    "statusCheckUrl": "/api/v1/admin/subscriptions/bulk-assignment/status/42"
  },
  "success": true,
  "message": "Toplu subscription atama işlemi başlatıldı. 150 farmer kuyruğa eklendi."
}
```

**Error (400 Bad Request):**
```json
{
  "data": null,
  "success": false,
  "message": "Dosya boyutu çok büyük. Maksimum: 5 MB"
}
```

**Error (401 Unauthorized):**
```json
{
  "message": "Unauthorized"
}
```

**Error (403 Forbidden):**
```json
{
  "message": "You are not authorized to perform this operation"
}
```

---

### 2. Get Job Status

**Endpoint:** `GET /bulk-assignment/status/{jobId}`

**Description:** Get real-time job status and progress.

#### Request

**Path Parameters:**
```
jobId: <INTEGER> (required)
  - The job ID returned from the queue endpoint
```

**Example (cURL):**
```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/admin/subscriptions/bulk-assignment/status/42" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Response

**Success (200 OK):**
```json
{
  "data": {
    "jobId": 42,
    "status": "Processing",
    "totalFarmers": 150,
    "processedFarmers": 87,
    "successfulAssignments": 82,
    "failedAssignments": 5,
    "newSubscriptionsCreated": 60,
    "existingSubscriptionsUpdated": 22,
    "totalNotificationsSent": 75,
    "createdDate": "2025-01-10T14:30:00Z",
    "startedDate": "2025-01-10T14:30:15Z",
    "completedDate": null,
    "resultFileUrl": null
  },
  "success": true,
  "message": "Job status retrieved successfully"
}
```

**Status Values:**
- `Pending`: Job created, waiting to start
- `Processing`: Currently processing farmers
- `Completed`: All farmers processed successfully
- `CompletedWithErrors`: Processing finished but some farmers failed
- `Failed`: Job failed completely

**Progress Calculation:**
```
Progress Percentage = (processedFarmers / totalFarmers) * 100
```

**Error (404 Not Found):**
```json
{
  "data": null,
  "success": false,
  "message": "Job not found or you don't have permission to view it"
}
```

---

### 3. Get Job History

**Endpoint:** `GET /bulk-assignment/history`

**Description:** Get paginated job history for the current admin.

#### Request

**Query Parameters:**
```
pageNumber: <INTEGER> (optional, default: 1)
  - Page number for pagination

pageSize: <INTEGER> (optional, default: 20, max: 100)
  - Number of jobs per page
```

**Example (cURL):**
```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/admin/subscriptions/bulk-assignment/history?pageNumber=1&pageSize=20" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Response

**Success (200 OK):**
```json
{
  "data": [
    {
      "jobId": 42,
      "originalFileName": "farmers_subscription_bulk.xlsx",
      "fileSize": 45678,
      "totalFarmers": 150,
      "processedFarmers": 150,
      "successfulAssignments": 145,
      "failedAssignments": 5,
      "status": "CompletedWithErrors",
      "createdDate": "2025-01-10T14:30:00Z",
      "completedDate": "2025-01-10T15:42:33Z"
    },
    {
      "jobId": 41,
      "originalFileName": "trial_farmers.xlsx",
      "fileSize": 12345,
      "totalFarmers": 50,
      "processedFarmers": 50,
      "successfulAssignments": 50,
      "failedAssignments": 0,
      "status": "Completed",
      "createdDate": "2025-01-09T10:15:00Z",
      "completedDate": "2025-01-09T10:32:11Z"
    }
  ],
  "success": true,
  "message": "Job history retrieved successfully"
}
```

---

### 4. Get Job Result File

**Endpoint:** `GET /bulk-assignment/result/{jobId}`

**Description:** Get the result file URL for downloading detailed success/failure information.

#### Request

**Path Parameters:**
```
jobId: <INTEGER> (required)
  - The job ID to get result file for
```

**Example (cURL):**
```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/admin/subscriptions/bulk-assignment/result/42" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Response

**Success (200 OK):**
```json
{
  "data": "https://storage.ziraai.com/bulk-jobs/results/42_result_20250110_154233.xlsx",
  "success": true,
  "message": "Result file URL retrieved successfully"
}
```

**Error (404 Not Found - Job Not Found):**
```json
{
  "data": null,
  "success": false,
  "message": "Job not found or you don't have permission to view it"
}
```

**Error (404 Not Found - Result Not Ready):**
```json
{
  "data": null,
  "success": false,
  "message": "Result file is not available yet. Job may still be processing."
}
```

---

## Excel File Format

### Required Columns
At least **one** of the following is required for each farmer:
- `Email` (string)
- `Phone` (string)

### Optional Columns
- `FirstName` (string)
- `LastName` (string)
- `TierName` (string) - Required if `defaultTierId` not provided in request
- `DurationDays` (integer) - Required if `defaultDurationDays` not provided in request
- `Notes` (string)

### Column Mapping Rules
- **Case-Insensitive:** Headers like "email", "Email", "EMAIL" are all valid
- **Header-Based:** Column order doesn't matter, system maps by column name
- **Flexible:** Extra columns are ignored

### TierName Values
| TierName | SubscriptionTierId | Description |
|----------|-------------------|-------------|
| Trial | 1 | Trial tier |
| S | 2 | Small tier |
| M | 3 | Medium tier |
| L | 4 | Large tier |
| XL | 5 | Extra Large tier |

### Excel Template Example

| Email | Phone | FirstName | LastName | TierName | DurationDays | Notes |
|-------|-------|-----------|----------|----------|--------------|-------|
| ahmet@example.com | +905551234567 | Ahmet | Yılmaz | S | 30 | New customer |
| mehmet@example.com | | Mehmet | Demir | M | 60 | Renewal |
| | +905559876543 | Ayşe | Kaya | L | 90 | Partner referral |

### Validation Rules
1. **Email Format:** Must be valid email if provided
2. **Phone Format:** Will be normalized to `+90XXXXXXXXXX` format
3. **TierName:** Must match one of: Trial, S, M, L, XL (case-insensitive)
4. **DurationDays:** Must be positive integer (1-365)
5. **Max Rows:** 2000 farmers per file
6. **Max File Size:** 5 MB

---

## Integration Flow

### Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│ STEP 1: Upload Excel                                                │
│ Admin uploads Excel file via frontend                               │
└─────────────────────────────┬───────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│ STEP 2: Validate & Queue                                            │
│ • Validate file (size, format, columns)                             │
│ • Parse Excel rows                                                   │
│ • Validate subscription tiers and durations                          │
│ • Create BulkSubscriptionAssignmentJob (status: Pending)             │
│ • Publish 150 messages to RabbitMQ                                   │
│ • Update job status to Processing                                    │
│ • Return jobId = 42 to frontend                                      │
└─────────────────────────────┬───────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│ STEP 3: Poll Job Status                                             │
│ Frontend polls GET /status/42 every 2-3 seconds                     │
│                                                                      │
│ Poll 1: { processedFarmers: 0,  status: "Processing" }              │
│ Poll 2: { processedFarmers: 15, status: "Processing" }              │
│ Poll 3: { processedFarmers: 32, status: "Processing" }              │
│ ...                                                                  │
│ Poll N: { processedFarmers: 150, status: "Completed" }              │
└─────────────────────────────┬───────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│ STEP 4: Background Processing (Async - Worker Service)              │
│                                                                      │
│ For each farmer (150 total):                                        │
│   1. Lookup user by email/phone                                     │
│   2. If user doesn't exist:                                         │
│      → Create new user account                                      │
│   3. Check if user has existing active subscription                 │
│   4. If subscription exists:                                        │
│      → Update: tier, dates, duration, reset usage counters          │
│      → existingSubscriptionsUpdated++                               │
│   5. If no subscription:                                            │
│      → Create new subscription                                      │
│      → newSubscriptionsCreated++                                    │
│   6. If sendNotification = true:                                    │
│      → Send SMS/Email to farmer                                     │
│      → totalNotificationsSent++                                     │
│   7. Atomic progress update:                                        │
│      → processedFarmers++                                           │
│      → successfulAssignments++ OR failedAssignments++               │
│   8. HTTP callback to WebAPI for SignalR broadcast                  │
│                                                                      │
│ Processing Rate: ~2-3 farmers/second (30-45 seconds per farmer)     │
└─────────────────────────────┬───────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│ STEP 5: Completion                                                   │
│ • Job status changes to "Completed" or "CompletedWithErrors"         │
│ • Result file URL becomes available                                  │
│ • Frontend shows success summary                                     │
│ • Admin can download result file with details                        │
└─────────────────────────────────────────────────────────────────────┘
```

### Detailed Step-by-Step Flow

#### Step 1: Admin Uploads Excel File
```javascript
// Frontend code
const handleUpload = async (file) => {
  const formData = new FormData();
  formData.append('excelFile', file);
  formData.append('defaultTierId', '2'); // S tier
  formData.append('defaultDurationDays', '30');
  formData.append('sendNotification', 'true');
  formData.append('notificationMethod', 'SMS');
  formData.append('autoActivate', 'true');

  const response = await fetch('/api/v1/admin/subscriptions/bulk-assignment', {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: formData
  });

  const result = await response.json();
  if (result.success) {
    const jobId = result.data.jobId; // 42
    startPolling(jobId);
  }
};
```

#### Step 2: Backend Validates & Queues
```csharp
// Backend processing
1. Validate file size (< 5 MB)
2. Validate file format (.xlsx or .xls)
3. Parse Excel with EPPlus:
   - Read headers from row 1
   - Map columns by name (case-insensitive)
   - Parse rows 2-N
4. Validate each row:
   - Email format
   - Phone format
   - Tier ID exists and is active
   - Duration is positive integer
5. Create BulkSubscriptionAssignmentJob:
   - AdminId = current admin
   - TotalFarmers = 150
   - Status = "Pending"
6. Publish 150 messages to RabbitMQ:
   - Queue: farmer-subscription-assignment-requests
   - Correlation ID = JobId (42)
7. Update job status to "Processing"
8. Return jobId to frontend
```

#### Step 3: Frontend Polls for Progress
```javascript
// Frontend polling
const startPolling = (jobId) => {
  const interval = setInterval(async () => {
    const response = await fetch(`/api/v1/admin/subscriptions/bulk-assignment/status/${jobId}`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });

    const status = await response.json();

    // Update progress bar
    const progress = (status.data.processedFarmers / status.data.totalFarmers) * 100;
    updateProgressBar(progress);

    // Check if complete
    if (status.data.status === 'Completed' || status.data.status === 'CompletedWithErrors') {
      clearInterval(interval);
      showCompletionSummary(status.data);
    }
  }, 2000); // Poll every 2 seconds
};
```

#### Step 4: Worker Service Processes Farmers
```csharp
// Worker service background processing
For each message in RabbitMQ:
{
  1. Dequeue message with farmer details
  2. Enqueue Hangfire background job
  3. Hangfire executes FarmerSubscriptionAssignmentJobService:

     a. User Lookup:
        - Try find by email
        - If not found, try find by phone
        - If still not found, create new user

     b. Subscription Management:
        - Check for existing active subscription
        - If exists: Update tier, dates, reset usage counters
        - If not exists: Create new subscription with tier and dates

     c. Notification (if enabled):
        - Get subscription tier display name
        - Build SMS message with farmer name and tier
        - Send SMS via messaging service
        - Log SMS to database

     d. Atomic Progress Update:
        - Execute raw SQL to increment counters
        - Prevents race conditions in concurrent processing
        - Update: processedFarmers, successfulAssignments, etc.

     e. Check Completion:
        - If all farmers processed, mark job as "Completed"
        - Generate result file with success/failure details

     f. HTTP Callback (optional):
        - Send progress to WebAPI for SignalR broadcasting
        - Real-time updates to connected admin clients
}
```

#### Step 5: Admin Views Results
```javascript
// Frontend completion handling
const showCompletionSummary = (data) => {
  console.log(`
    Job Completed!
    - Total Farmers: ${data.totalFarmers}
    - Successful: ${data.successfulAssignments}
    - Failed: ${data.failedAssignments}
    - New Subscriptions: ${data.newSubscriptionsCreated}
    - Updated Subscriptions: ${data.existingSubscriptionsUpdated}
    - Notifications Sent: ${data.totalNotificationsSent}
  `);

  // Download result file if needed
  if (data.resultFileUrl) {
    downloadResultFile(data.resultFileUrl);
  }
};
```

---

## Response Codes

### HTTP Status Codes

| Code | Description | When It Occurs |
|------|-------------|----------------|
| 200 | OK | Successful operation |
| 400 | Bad Request | Invalid file, invalid tier, validation errors |
| 401 | Unauthorized | Missing or invalid JWT token |
| 403 | Forbidden | User doesn't have Admin role or required operation claims |
| 404 | Not Found | Job not found or no permission to view |
| 500 | Internal Server Error | Unexpected server error |

### Business Error Messages

| Message | Cause | Solution |
|---------|-------|----------|
| "Dosya yüklenmedi." | No file in request | Provide Excel file in form data |
| "Dosya boyutu çok büyük. Maksimum: 5 MB" | File > 5 MB | Reduce file size or split into multiple files |
| "Geçersiz dosya formatı. Sadece .xlsx ve .xls desteklenir." | Wrong file format | Use Excel format (.xlsx or .xls) |
| "Excel'de 'Email' veya 'Phone' sütunlarından en az biri zorunludur" | Missing required columns | Add Email or Phone column to Excel |
| "Maksimum 2000 farmer kaydı yüklenebilir." | Too many rows | Split Excel into multiple files (max 2000 each) |
| "Geçersiz veya pasif subscription tier ID: X" | Invalid tier | Use valid tier ID (1=Trial, 2=S, 3=M, 4=L, 5=XL) |
| "Job not found or you don't have permission to view it" | Wrong job ID or not your job | Check job ID and ensure you created the job |

---

## Error Handling

### Client-Side Error Handling

```javascript
const uploadBulkSubscription = async (file) => {
  try {
    const formData = new FormData();
    formData.append('excelFile', file);
    formData.append('defaultTierId', '2');
    formData.append('defaultDurationDays', '30');

    const response = await fetch('/api/v1/admin/subscriptions/bulk-assignment', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${token}` },
      body: formData
    });

    // Check HTTP status
    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Oturum süreniz doldu. Lütfen tekrar giriş yapın.');
      }
      if (response.status === 403) {
        throw new Error('Bu işlem için yetkiniz yok.');
      }
    }

    const result = await response.json();

    // Check business logic success
    if (!result.success) {
      throw new Error(result.message);
    }

    return result.data;

  } catch (error) {
    console.error('Bulk subscription upload failed:', error);

    // Show user-friendly error message
    if (error.message.includes('NetworkError')) {
      alert('Bağlantı hatası. Lütfen internet bağlantınızı kontrol edin.');
    } else {
      alert(error.message);
    }

    throw error;
  }
};
```

### Retry Strategy for Polling

```javascript
const pollJobStatus = async (jobId, maxRetries = 3) => {
  let retries = 0;

  const poll = async () => {
    try {
      const response = await fetch(`/api/v1/admin/subscriptions/bulk-assignment/status/${jobId}`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const status = await response.json();
      retries = 0; // Reset retry counter on success
      return status.data;

    } catch (error) {
      retries++;

      if (retries >= maxRetries) {
        console.error('Max retries reached:', error);
        throw new Error('Durum sorgulanamadı. Lütfen sayfayı yenileyin.');
      }

      console.warn(`Retry ${retries}/${maxRetries}:`, error);
      await new Promise(resolve => setTimeout(resolve, 5000)); // Wait 5s before retry
      return poll(); // Retry
    }
  };

  return poll();
};
```

---

## Testing Guide

### Manual Testing Steps

#### 1. Prepare Test Data
Create an Excel file (`test_farmers.xlsx`) with the following structure:

| Email | Phone | FirstName | LastName | TierName | DurationDays | Notes |
|-------|-------|-----------|----------|----------|--------------|-------|
| test1@example.com | +905551111111 | Test | User1 | S | 30 | Test case 1 |
| test2@example.com | +905552222222 | Test | User2 | M | 60 | Test case 2 |
| test3@example.com | +905553333333 | Test | User3 | L | 90 | Test case 3 |

#### 2. Obtain Admin JWT Token
```bash
# Login as admin
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@ziraai.com",
    "password": "AdminPassword123!"
  }'

# Copy the "accessToken" from response
```

#### 3. Upload Excel File
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/admin/subscriptions/bulk-assignment" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "excelFile=@test_farmers.xlsx" \
  -F "defaultTierId=2" \
  -F "defaultDurationDays=30" \
  -F "sendNotification=true" \
  -F "notificationMethod=SMS" \
  -F "autoActivate=true"

# Expected response: { "data": { "jobId": 1, ... }, "success": true }
```

#### 4. Monitor Job Progress
```bash
# Poll every 2-3 seconds
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/admin/subscriptions/bulk-assignment/status/1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Check processedFarmers increasing
```

#### 5. Verify Completion
```bash
# Final status check
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/admin/subscriptions/bulk-assignment/status/1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Expected: status = "Completed", processedFarmers = totalFarmers
```

#### 6. Download Result File
```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/admin/subscriptions/bulk-assignment/result/1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Expected: { "data": "https://storage.ziraai.com/...", "success": true }
```

### Test Cases

#### Test Case 1: Successful Bulk Assignment
**Input:**
- 50 valid farmers with email and phone
- Valid tier names (S, M, L)
- Valid durations (30-90 days)

**Expected Output:**
- Job status: "Completed"
- successfulAssignments: 50
- failedAssignments: 0

#### Test Case 2: Mixed Success/Failure
**Input:**
- 100 farmers
- 10 with invalid email format
- 5 with invalid tier name

**Expected Output:**
- Job status: "CompletedWithErrors"
- successfulAssignments: 85
- failedAssignments: 15
- Result file contains error details

#### Test Case 3: Create New Users
**Input:**
- 20 farmers with emails not in database

**Expected Output:**
- 20 new user accounts created
- 20 new subscriptions created
- newSubscriptionsCreated: 20

#### Test Case 4: Update Existing Subscriptions
**Input:**
- 30 farmers with existing active subscriptions

**Expected Output:**
- 30 subscriptions updated (tier, dates, usage reset)
- existingSubscriptionsUpdated: 30

#### Test Case 5: File Validation Errors
**Input:**
- File > 5 MB

**Expected Output:**
- HTTP 400 Bad Request
- Message: "Dosya boyutu çok büyük. Maksimum: 5 MB"

#### Test Case 6: Authorization Errors
**Input:**
- Request without JWT token

**Expected Output:**
- HTTP 401 Unauthorized

**Input:**
- JWT token from non-admin user

**Expected Output:**
- HTTP 403 Forbidden

---

## Frontend Implementation Examples

### React Example

```jsx
import React, { useState, useEffect } from 'react';
import axios from 'axios';

const BulkSubscriptionUpload = () => {
  const [file, setFile] = useState(null);
  const [jobId, setJobId] = useState(null);
  const [progress, setProgress] = useState(null);
  const [loading, setLoading] = useState(false);

  const handleFileChange = (e) => {
    setFile(e.target.files[0]);
  };

  const handleUpload = async () => {
    if (!file) {
      alert('Lütfen bir dosya seçin');
      return;
    }

    setLoading(true);

    const formData = new FormData();
    formData.append('excelFile', file);
    formData.append('defaultTierId', '2');
    formData.append('defaultDurationDays', '30');
    formData.append('sendNotification', 'true');
    formData.append('notificationMethod', 'SMS');
    formData.append('autoActivate', 'true');

    try {
      const response = await axios.post(
        '/api/v1/admin/subscriptions/bulk-assignment',
        formData,
        {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`,
            'Content-Type': 'multipart/form-data'
          }
        }
      );

      if (response.data.success) {
        setJobId(response.data.data.jobId);
        alert('Yükleme başarılı! İşlem başlatıldı.');
      }
    } catch (error) {
      alert(error.response?.data?.message || 'Yükleme başarısız');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!jobId) return;

    const pollInterval = setInterval(async () => {
      try {
        const response = await axios.get(
          `/api/v1/admin/subscriptions/bulk-assignment/status/${jobId}`,
          {
            headers: {
              'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
          }
        );

        const data = response.data.data;
        setProgress(data);

        if (data.status === 'Completed' || data.status === 'CompletedWithErrors') {
          clearInterval(pollInterval);
        }
      } catch (error) {
        console.error('Polling error:', error);
      }
    }, 2000);

    return () => clearInterval(pollInterval);
  }, [jobId]);

  return (
    <div>
      <h2>Toplu Subscription Yükleme</h2>

      <input type="file" accept=".xlsx,.xls" onChange={handleFileChange} />

      <button onClick={handleUpload} disabled={loading || !file}>
        {loading ? 'Yükleniyor...' : 'Yükle'}
      </button>

      {progress && (
        <div>
          <h3>İşlem Durumu</h3>
          <p>Durum: {progress.status}</p>
          <p>İşlenen: {progress.processedFarmers} / {progress.totalFarmers}</p>
          <p>Başarılı: {progress.successfulAssignments}</p>
          <p>Başarısız: {progress.failedAssignments}</p>

          <div style={{ width: '100%', backgroundColor: '#ddd' }}>
            <div
              style={{
                width: `${(progress.processedFarmers / progress.totalFarmers) * 100}%`,
                height: '30px',
                backgroundColor: '#4caf50',
                transition: 'width 0.3s'
              }}
            />
          </div>
        </div>
      )}
    </div>
  );
};

export default BulkSubscriptionUpload;
```

### Angular Example

```typescript
import { Component } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { interval, Subscription } from 'rxjs';
import { switchMap, takeWhile } from 'rxjs/operators';

interface JobProgress {
  jobId: number;
  status: string;
  totalFarmers: number;
  processedFarmers: number;
  successfulAssignments: number;
  failedAssignments: number;
}

@Component({
  selector: 'app-bulk-subscription-upload',
  templateUrl: './bulk-subscription-upload.component.html'
})
export class BulkSubscriptionUploadComponent {
  selectedFile: File | null = null;
  jobId: number | null = null;
  progress: JobProgress | null = null;
  loading = false;
  pollSubscription: Subscription | null = null;

  constructor(private http: HttpClient) {}

  onFileSelected(event: any): void {
    this.selectedFile = event.target.files[0];
  }

  async uploadFile(): Promise<void> {
    if (!this.selectedFile) {
      alert('Lütfen bir dosya seçin');
      return;
    }

    this.loading = true;

    const formData = new FormData();
    formData.append('excelFile', this.selectedFile);
    formData.append('defaultTierId', '2');
    formData.append('defaultDurationDays', '30');
    formData.append('sendNotification', 'true');
    formData.append('notificationMethod', 'SMS');
    formData.append('autoActivate', 'true');

    const headers = new HttpHeaders({
      'Authorization': `Bearer ${localStorage.getItem('token')}`
    });

    try {
      const response: any = await this.http.post(
        '/api/v1/admin/subscriptions/bulk-assignment',
        formData,
        { headers }
      ).toPromise();

      if (response.success) {
        this.jobId = response.data.jobId;
        this.startPolling();
        alert('Yükleme başarılı! İşlem başlatıldı.');
      }
    } catch (error: any) {
      alert(error.error?.message || 'Yükleme başarısız');
    } finally {
      this.loading = false;
    }
  }

  startPolling(): void {
    if (!this.jobId) return;

    const headers = new HttpHeaders({
      'Authorization': `Bearer ${localStorage.getItem('token')}`
    });

    this.pollSubscription = interval(2000)
      .pipe(
        switchMap(() =>
          this.http.get<any>(
            `/api/v1/admin/subscriptions/bulk-assignment/status/${this.jobId}`,
            { headers }
          )
        ),
        takeWhile(
          (response) =>
            response.data.status !== 'Completed' &&
            response.data.status !== 'CompletedWithErrors',
          true
        )
      )
      .subscribe((response) => {
        this.progress = response.data;
      });
  }

  ngOnDestroy(): void {
    this.pollSubscription?.unsubscribe();
  }
}
```

### Vue.js Example

```vue
<template>
  <div class="bulk-subscription-upload">
    <h2>Toplu Subscription Yükleme</h2>

    <input type="file" accept=".xlsx,.xls" @change="handleFileChange" />

    <button @click="handleUpload" :disabled="loading || !file">
      {{ loading ? 'Yükleniyor...' : 'Yükle' }}
    </button>

    <div v-if="progress" class="progress-section">
      <h3>İşlem Durumu</h3>
      <p>Durum: {{ progress.status }}</p>
      <p>İşlenen: {{ progress.processedFarmers }} / {{ progress.totalFarmers }}</p>
      <p>Başarılı: {{ progress.successfulAssignments }}</p>
      <p>Başarısız: {{ progress.failedAssignments }}</p>

      <div class="progress-bar">
        <div
          class="progress-fill"
          :style="{ width: progressPercentage + '%' }"
        ></div>
      </div>
    </div>
  </div>
</template>

<script>
import axios from 'axios';

export default {
  name: 'BulkSubscriptionUpload',
  data() {
    return {
      file: null,
      jobId: null,
      progress: null,
      loading: false,
      pollInterval: null
    };
  },
  computed: {
    progressPercentage() {
      if (!this.progress) return 0;
      return (this.progress.processedFarmers / this.progress.totalFarmers) * 100;
    }
  },
  methods: {
    handleFileChange(event) {
      this.file = event.target.files[0];
    },
    async handleUpload() {
      if (!this.file) {
        alert('Lütfen bir dosya seçin');
        return;
      }

      this.loading = true;

      const formData = new FormData();
      formData.append('excelFile', this.file);
      formData.append('defaultTierId', '2');
      formData.append('defaultDurationDays', '30');
      formData.append('sendNotification', 'true');
      formData.append('notificationMethod', 'SMS');
      formData.append('autoActivate', 'true');

      try {
        const response = await axios.post(
          '/api/v1/admin/subscriptions/bulk-assignment',
          formData,
          {
            headers: {
              'Authorization': `Bearer ${localStorage.getItem('token')}`,
              'Content-Type': 'multipart/form-data'
            }
          }
        );

        if (response.data.success) {
          this.jobId = response.data.data.jobId;
          this.startPolling();
          alert('Yükleme başarılı! İşlem başlatıldı.');
        }
      } catch (error) {
        alert(error.response?.data?.message || 'Yükleme başarısız');
      } finally {
        this.loading = false;
      }
    },
    startPolling() {
      if (!this.jobId) return;

      this.pollInterval = setInterval(async () => {
        try {
          const response = await axios.get(
            `/api/v1/admin/subscriptions/bulk-assignment/status/${this.jobId}`,
            {
              headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
              }
            }
          );

          const data = response.data.data;
          this.progress = data;

          if (data.status === 'Completed' || data.status === 'CompletedWithErrors') {
            clearInterval(this.pollInterval);
          }
        } catch (error) {
          console.error('Polling error:', error);
        }
      }, 2000);
    }
  },
  beforeUnmount() {
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
    }
  }
};
</script>

<style scoped>
.progress-bar {
  width: 100%;
  height: 30px;
  background-color: #ddd;
  margin-top: 10px;
}

.progress-fill {
  height: 100%;
  background-color: #4caf50;
  transition: width 0.3s;
}
</style>
```

---

## Additional Notes

### Performance Considerations
- **Processing Rate:** ~2-3 farmers per second
- **Max Concurrent:** 5 messages processed simultaneously (prefetch limit)
- **Estimated Time:** 50 farmers ≈ 1 minute, 1000 farmers ≈ 20 minutes

### Database Transactions
- **Atomic Operations:** Progress updates use raw SQL to prevent race conditions
- **Subscription Creation:** Each farmer processed in separate transaction
- **User Creation:** Separate transaction to avoid conflicts

### SMS Rate Limiting
- If SMS provider has rate limits, processing may slow down
- Worker service handles SMS failures gracefully (logs but doesn't fail job)

### SignalR Integration (Optional)
For real-time updates without polling, implement SignalR:
1. Connect to SignalR hub on frontend
2. Worker service sends HTTP callbacks to WebAPI
3. WebAPI broadcasts to connected admin clients via SignalR

---

## Support & Resources

### Documentation
- **API Endpoints:** `/swagger` (when running locally)
- **Postman Collection:** `ZiraAI_Complete_API_Collection_v6.1.json`
- **SQL Scripts:** `claudedocs/AdminOperations/`

### Contact
- **Email:** support@ziraai.com
- **Slack:** #ziraai-api-support

---

**Document Version:** 1.0
**Last Updated:** 2025-01-10
**Status:** ✅ Ready for Production
