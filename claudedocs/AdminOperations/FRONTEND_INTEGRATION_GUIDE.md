# Frontend Integration Guide - Bulk Code Distribution

**Feature:** Admin & Sponsor Bulk Farmer Code Distribution
**Pattern:** Unified Excel Upload with Real-time Progress Tracking
**Date:** 2025-11-09
**Status:** Implementation Complete - Ready for Frontend Integration

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Sponsor vs Admin Usage](#sponsor-vs-admin-usage)
3. [API Endpoints](#api-endpoints)
4. [Excel File Format](#excel-file-format)
5. [SignalR Integration](#signalr-integration)
6. [Implementation Examples](#implementation-examples)
7. [UI/UX Recommendations](#uiux-recommendations)
8. [Error Handling](#error-handling)
9. [Testing Checklist](#testing-checklist)

---

## 📊 Overview

Bu sistem hem sponsor'ların hem de admin'lerin Excel tabanlı bulk code distribution yapmasını sağlar.

### Key Features

| Feature | Sponsor | Admin (OBO) |
|---------|---------|-------------|
| Excel Upload | ✅ | ✅ |
| Max Farmers | 2000+ | 2000+ |
| Real-time Progress | ✅ | ✅ |
| Result File Download | ✅ | ✅ |
| SMS Integration | ✅ | ✅ |
| Audit Logging | ❌ | ✅ |

### Architecture Flow

```
┌────────────────────────────────────────────────────────────┐
│ 1. Frontend: Excel Upload + SignalR Connection Setup      │
└────────────────┬───────────────────────────────────────────┘
                 │
                 ▼
┌────────────────────────────────────────────────────────────┐
│ 2. API: Validate Excel → Create Job → Queue to RabbitMQ   │
└────────────────┬───────────────────────────────────────────┘
                 │
                 ▼
┌────────────────────────────────────────────────────────────┐
│ 3. Worker: Process Each Farmer (Code Allocation + SMS)    │
└────────────────┬───────────────────────────────────────────┘
                 │
                 ▼
┌────────────────────────────────────────────────────────────┐
│ 4. SignalR: Real-time Progress Updates to Frontend        │
└────────────────┬───────────────────────────────────────────┘
                 │
                 ▼
┌────────────────────────────────────────────────────────────┐
│ 5. Frontend: Display Progress → Download Result File      │
└────────────────────────────────────────────────────────────┘
```

---

## 🎭 Sponsor vs Admin Usage

### Sponsor Flow (Self-Service)

```javascript
// Sponsor uploads Excel for their own farmers
const uploadBulkCodes = async (excelFile, sendSms) => {
  const formData = new FormData();
  formData.append('excelFile', excelFile);
  formData.append('sendSms', sendSms);

  const response = await fetch('/api/v1/sponsorship/bulk-code-distribution', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${sponsorToken}`
      // No onBehalfOfSponsorId needed!
    },
    body: formData
  });

  return await response.json();
};
```

**Key Points:**
- ✅ Sponsor automatically acts for themselves
- ✅ No additional parameters needed
- ✅ Progress notifications sent to `sponsor_{userId}` SignalR group

### Admin Flow (On Behalf Of)

```javascript
// Admin uploads Excel on behalf of a sponsor
const uploadBulkCodesAsAdmin = async (excelFile, sendSms, targetSponsorId) => {
  const formData = new FormData();
  formData.append('excelFile', excelFile);
  formData.append('sendSms', sendSms);

  // CRITICAL: Query parameter is REQUIRED for admin
  const url = `/api/v1/sponsorship/bulk-code-distribution?onBehalfOfSponsorId=${targetSponsorId}`;

  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${adminToken}`
    },
    body: formData
  });

  return await response.json();
};
```

**Key Points:**
- ⚠️ `onBehalfOfSponsorId` query parameter is **REQUIRED** for admin
- ✅ Admin action logged in audit trail
- ✅ Progress notifications sent to `sponsor_{targetSponsorId}` SignalR group
- ❌ Admin does NOT receive SignalR notifications (must poll or enhance system)

---

## 🔌 API Endpoints

### 1. Upload Excel for Bulk Distribution

#### Request

**Endpoint:** `POST /api/v1/sponsorship/bulk-code-distribution`

**Authorization:** `Bearer {token}` (Sponsor or Admin role)

**Content-Type:** `multipart/form-data`

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `excelFile` | File | Form Data | ✅ | Excel file (.xlsx) with farmer data |
| `sendSms` | Boolean | Form Data | ✅ | Whether to send SMS with codes |
| `onBehalfOfSponsorId` | Integer | Query String | ⚠️ **Required for Admin** | Target sponsor ID (Admin only) |

**Example - Sponsor:**
```http
POST /api/v1/sponsorship/bulk-code-distribution
Authorization: Bearer {sponsor_token}
Content-Type: multipart/form-data

--boundary
Content-Disposition: form-data; name="excelFile"; filename="farmers.xlsx"
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet

[binary data]
--boundary
Content-Disposition: form-data; name="sendSms"

true
--boundary--
```

**Example - Admin:**
```http
POST /api/v1/sponsorship/bulk-code-distribution?onBehalfOfSponsorId=159
Authorization: Bearer {admin_token}
Content-Type: multipart/form-data

--boundary
Content-Disposition: form-data; name="excelFile"; filename="farmers.xlsx"
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet

[binary data]
--boundary
Content-Disposition: form-data; name="sendSms"

true
--boundary--
```

#### Response

**Success (200 OK):**
```json
{
  "success": true,
  "data": {
    "jobId": 123,
    "totalFarmers": 150,
    "totalCodesRequired": 150,
    "availableCodes": 2000,
    "status": "Pending",
    "createdDate": "2025-11-09T10:00:00Z",
    "estimatedCompletionTime": "2025-11-09T10:15:00Z",
    "statusCheckUrl": "/api/v1/sponsorship/bulk-code-distribution/status/123"
  },
  "message": "Toplu kod dağıtım işlemi başlatıldı. 150 farmer kuyruğa eklendi."
}
```

**Error (400 Bad Request - Admin without sponsor ID):**
```json
{
  "success": false,
  "message": "Admin users must specify onBehalfOfSponsorId query parameter"
}
```

**Error (400 Bad Request - Invalid file):**
```json
{
  "success": false,
  "message": "Geçersiz Excel formatı veya eksik sütunlar"
}
```

**Error (400 Bad Request - Insufficient codes):**
```json
{
  "success": false,
  "message": "Yetersiz kod. Gerekli: 150, Mevcut: 100"
}
```

---

### 2. Get Job Status

#### Request

**Endpoint:** `GET /api/v1/sponsorship/bulk-code-distribution/{jobId}`

**Authorization:** `Bearer {token}` (Sponsor or Admin role)

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `jobId` | Integer | Path | ✅ | Job ID from upload response |

**Example:**
```http
GET /api/v1/sponsorship/bulk-code-distribution/123
Authorization: Bearer {token}
```

#### Response

**Success (200 OK):**
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
    "totalCodesDistributed": 70,
    "totalSmsSent": 70,
    "startedDate": "2025-11-09T10:00:00Z",
    "estimatedTimeRemaining": "PT7M30S",
    "resultFileUrl": null
  }
}
```

**Success (200 OK - Completed):**
```json
{
  "success": true,
  "data": {
    "jobId": 123,
    "status": "Completed",
    "totalFarmers": 150,
    "processedFarmers": 150,
    "successfulDistributions": 145,
    "failedDistributions": 5,
    "progressPercentage": 100,
    "totalCodesDistributed": 145,
    "totalSmsSent": 145,
    "startedDate": "2025-11-09T10:00:00Z",
    "completedDate": "2025-11-09T10:15:00Z",
    "resultFileUrl": "/api/v1/sponsorship/bulk-code-distribution/123/result"
  }
}
```

**Error (404 Not Found):**
```json
{
  "success": false,
  "message": "Job bulunamadı veya erişim yetkiniz yok."
}
```

**Authorization Rules:**
- **Sponsor:** Can only view their own jobs (`job.SponsorId == userId`)
- **Admin:** Can view ANY job (no ownership restriction)

---

### 3. Get Job History

#### Request

**Endpoint:** `GET /api/v1/sponsorship/bulk-code-distribution/history`

**Authorization:** `Bearer {token}` (Sponsor or Admin role)

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `sponsorId` | Integer | Query String | ⚠️ **Required for Admin** | Target sponsor ID (Admin only) |
| `page` | Integer | Query String | ❌ | Page number (default: 1) |
| `pageSize` | Integer | Query String | ❌ | Page size (default: 20) |
| `status` | String | Query String | ❌ | Filter by status (Pending, Processing, Completed, etc.) |

**Example - Sponsor:**
```http
GET /api/v1/sponsorship/bulk-code-distribution/history?page=1&pageSize=20
Authorization: Bearer {sponsor_token}
```

**Example - Admin:**
```http
GET /api/v1/sponsorship/bulk-code-distribution/history?sponsorId=159&page=1&pageSize=20
Authorization: Bearer {admin_token}
```

#### Response

**Success (200 OK):**
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
        "createdDate": "2025-11-09T10:00:00Z",
        "completedDate": "2025-11-09T10:15:00Z",
        "resultFileUrl": "/api/v1/sponsorship/bulk-code-distribution/123/result"
      },
      {
        "jobId": 122,
        "fileName": "farmers_october.xlsx",
        "totalFarmers": 200,
        "successfulDistributions": 200,
        "failedDistributions": 0,
        "status": "Completed",
        "createdDate": "2025-10-15T14:30:00Z",
        "completedDate": "2025-10-15T14:50:00Z",
        "resultFileUrl": "/api/v1/sponsorship/bulk-code-distribution/122/result"
      }
    ],
    "totalCount": 10,
    "page": 1,
    "pageSize": 20
  }
}
```

**Error (400 Bad Request - Admin without sponsor ID):**
```json
{
  "success": false,
  "message": "Admin users must specify sponsorId query parameter"
}
```

---

### 4. Download Result File

#### Request

**Endpoint:** `GET /api/v1/sponsorship/bulk-code-distribution/{jobId}/result`

**Authorization:** `Bearer {token}` (Sponsor or Admin role)

**Parameters:**

| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `jobId` | Integer | Path | ✅ | Job ID |

**Example:**
```http
GET /api/v1/sponsorship/bulk-code-distribution/123/result
Authorization: Bearer {token}
```

#### Response

**Success (200 OK):**
- **Content-Type:** `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **Content-Disposition:** `attachment; filename="bulk_distribution_result_123.xlsx"`
- **Body:** Binary Excel file

**Excel File Columns:**
| Column | Description |
|--------|-------------|
| Row Number | Original row number from upload |
| Phone | Farmer phone number |
| Name | Farmer name |
| Code Count | Requested code count (always 1) |
| Status | Success or Failed |
| Allocated Code | Sponsorship code (if successful) |
| Error Message | Error details (if failed) |

**Error (404 Not Found):**
```json
{
  "success": false,
  "message": "Result file not available yet or job not found"
}
```

---

## 📄 Excel File Format

### Required Columns (Header-based Detection)

| Column Name | Type | Required | Example | Validation |
|-------------|------|----------|---------|------------|
| `Phone` | String | ✅ | `905551234567` | 12 digits, starts with `90` |
| `Name` | String | ✅ | `Ahmet Yılmaz` | 2-100 characters |
| `Email` | String | ❌ | `ahmet@example.com` | Valid email format (optional) |
| `Notes` | String | ❌ | `Kırklareli çiftçisi` | Any text (optional) |

### Excel Template Example

```
Phone           | Name          | Email                 | Notes
----------------|---------------|-----------------------|---------------------
905551234567    | Ahmet Yılmaz  | ahmet@example.com     | Kırklareli
905559876543    | Mehmet Kaya   | mehmet@example.com    | Edirne
905551112233    | Ayşe Demir    | ayse@example.com      | Tekirdağ
```

### Validation Rules

#### 1. File Validation
- **Max File Size:** 5 MB
- **Supported Formats:** `.xlsx` only
- **Max Row Count:** 2000 farmers

#### 2. Phone Number Validation
- **Format:** 12 digits starting with `90` (Turkey)
- **Auto-normalization:** `5551234567` → `905551234567`
- **Uniqueness:** No duplicate phones in same Excel

#### 3. Name Validation
- **Min Length:** 2 characters
- **Max Length:** 100 characters

#### 4. Email Validation (Optional)
- **Format:** Valid email pattern
- **Max Length:** 100 characters

#### 5. Code Availability
- **Total codes needed:** Each farmer gets exactly 1 code
- **Check:** Total farmers ≤ Available codes in sponsor's purchase

### Download Template

Provide users with a downloadable template:

```javascript
const downloadTemplate = () => {
  const templateUrl = '/api/v1/sponsorship/bulk-code-distribution/template';
  window.open(templateUrl, '_blank');
};
```

---

## 🔔 SignalR Integration

### Connection Setup

#### 1. Install SignalR Client

```bash
npm install @microsoft/signalr
```

#### 2. Create SignalR Connection

```javascript
import * as signalR from '@microsoft/signalr';

class BulkCodeDistributionService {
  constructor(apiBaseUrl, authToken) {
    this.apiBaseUrl = apiBaseUrl;
    this.authToken = authToken;
    this.connection = null;
  }

  /**
   * Initialize SignalR connection
   * CRITICAL: Connection must be established BEFORE uploading Excel
   */
  async initializeSignalR() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiBaseUrl}/hubs/notifications`, {
        accessTokenFactory: () => this.authToken
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000]) // Retry delays
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Register event handlers BEFORE starting connection
    this.connection.on('BulkCodeDistributionProgress', this.onProgressUpdate.bind(this));
    this.connection.on('BulkCodeDistributionCompleted', this.onCompleted.bind(this));

    try {
      await this.connection.start();
      console.log('✅ SignalR connected');
    } catch (err) {
      console.error('❌ SignalR connection failed:', err);
      throw err;
    }
  }

  /**
   * Handle progress updates
   */
  onProgressUpdate(data) {
    console.log('📊 Progress update:', data);
    /*
    data = {
      jobId: 123,
      sponsorId: 159,
      status: "Processing",
      totalFarmers: 150,
      processedFarmers: 75,
      successfulDistributions: 70,
      failedDistributions: 5,
      progressPercentage: 50,
      totalCodesDistributed: 70,
      totalSmsSent: 70
    }
    */

    // Update UI
    this.updateProgressBar(data.progressPercentage);
    this.updateStats(data);
  }

  /**
   * Handle completion notification
   */
  onCompleted(data) {
    console.log('✅ Job completed:', data);
    /*
    data = {
      jobId: 123,
      status: "Completed",
      successCount: 145,
      failedCount: 5,
      completedAt: "2025-11-09T10:15:00Z"
    }
    */

    // Update UI
    this.showCompletionNotification(data);
    this.enableDownloadButton(data.jobId);
  }

  /**
   * Disconnect SignalR
   */
  async disconnect() {
    if (this.connection) {
      await this.connection.stop();
      console.log('🔌 SignalR disconnected');
    }
  }
}
```

### Event Handlers

#### Progress Update Event

**Event Name:** `BulkCodeDistributionProgress`

**Payload:**
```json
{
  "jobId": 123,
  "sponsorId": 159,
  "status": "Processing",
  "totalFarmers": 150,
  "processedFarmers": 75,
  "successfulDistributions": 70,
  "failedDistributions": 5,
  "progressPercentage": 50,
  "totalCodesDistributed": 70,
  "totalSmsSent": 70
}
```

#### Completion Event

**Event Name:** `BulkCodeDistributionCompleted`

**Payload:**
```json
{
  "jobId": 123,
  "status": "Completed",
  "successCount": 145,
  "failedCount": 5,
  "completedAt": "2025-11-09T10:15:00Z"
}
```

### SignalR Groups (CRITICAL!)

#### How Groups Work

SignalR notifications are sent to **sponsor groups** only:

```csharp
// Backend: BulkCodeDistributionNotificationService.cs
var sponsorGroup = $"sponsor_{progress.SponsorId}"; // e.g., "sponsor_159"

await _hubContext.Clients
    .Group(sponsorGroup)
    .SendAsync("BulkCodeDistributionProgress", progress);
```

#### Who Receives Notifications?

| Scenario | Sponsor Receives? | Admin Receives? |
|----------|-------------------|-----------------|
| **Sponsor uploads for self** | ✅ Yes (`sponsor_{sponsorId}`) | ❌ No |
| **Admin uploads for Sponsor 159** | ✅ Yes (`sponsor_159`) | ❌ No |

**Why Admin doesn't receive:**
- Admin is in `sponsor_{adminId}` group, NOT `sponsor_{targetSponsorId}`
- System sends notifications to target sponsor's group
- Admin must use **polling** to track progress

#### Admin Progress Tracking Options

**Option 1: Polling (Current Implementation)**
```javascript
class AdminBulkCodeService {
  async uploadAndTrack(excelFile, sendSms, targetSponsorId) {
    // 1. Upload Excel
    const uploadResponse = await this.uploadBulkCodesAsAdmin(
      excelFile,
      sendSms,
      targetSponsorId
    );
    const jobId = uploadResponse.data.jobId;

    // 2. Poll for progress every 5 seconds
    const pollInterval = setInterval(async () => {
      const status = await this.getJobStatus(jobId);

      // Update UI
      this.updateProgressBar(status.data.progressPercentage);
      this.updateStats(status.data);

      // Stop polling when completed
      if (status.data.status === 'Completed' ||
          status.data.status === 'Failed') {
        clearInterval(pollInterval);
        this.onCompleted(status.data);
      }
    }, 5000); // Poll every 5 seconds

    return jobId;
  }

  async getJobStatus(jobId) {
    const response = await fetch(
      `/api/v1/sponsorship/bulk-code-distribution/${jobId}`,
      {
        headers: {
          'Authorization': `Bearer ${this.adminToken}`
        }
      }
    );
    return await response.json();
  }
}
```

**Option 2: Future Enhancement (Admin SignalR Group)**

Backend enhancement needed:
1. Add `InitiatedByAdminId` field to `BulkCodeDistributionJob`
2. Modify notification service to send to both sponsor AND admin groups
3. Admin UI receives real-time notifications

```csharp
// Future enhancement
if (progress.InitiatedByAdminId.HasValue)
{
    var adminGroup = $"admin_{progress.InitiatedByAdminId.Value}";
    await _hubContext.Clients.Group(adminGroup)
        .SendAsync("BulkCodeDistributionProgress", progress);
}
```

---

## 💻 Implementation Examples

### Complete React Example (Sponsor)

```jsx
import React, { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';

const BulkCodeDistribution = () => {
  const [connection, setConnection] = useState(null);
  const [file, setFile] = useState(null);
  const [sendSms, setSendSms] = useState(true);
  const [currentJob, setCurrentJob] = useState(null);
  const [progress, setProgress] = useState(null);
  const [isUploading, setIsUploading] = useState(false);

  // 1. Initialize SignalR on component mount
  useEffect(() => {
    const initSignalR = async () => {
      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${process.env.REACT_APP_API_URL}/hubs/notifications`, {
          accessTokenFactory: () => localStorage.getItem('authToken')
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Register event handlers
      newConnection.on('BulkCodeDistributionProgress', (data) => {
        console.log('📊 Progress:', data);
        setProgress(data);
      });

      newConnection.on('BulkCodeDistributionCompleted', (data) => {
        console.log('✅ Completed:', data);
        setProgress({ ...progress, status: 'Completed' });
        alert(`Bulk distribution completed!\nSuccess: ${data.successCount}\nFailed: ${data.failedCount}`);
      });

      try {
        await newConnection.start();
        console.log('✅ SignalR connected');
        setConnection(newConnection);
      } catch (err) {
        console.error('❌ SignalR error:', err);
      }
    };

    initSignalR();

    // Cleanup on unmount
    return () => {
      if (connection) {
        connection.stop();
      }
    };
  }, []);

  // 2. Handle file upload
  const handleUpload = async () => {
    if (!file) {
      alert('Please select an Excel file');
      return;
    }

    setIsUploading(true);

    try {
      const formData = new FormData();
      formData.append('excelFile', file);
      formData.append('sendSms', sendSms);

      const response = await fetch(
        `${process.env.REACT_APP_API_URL}/api/v1/sponsorship/bulk-code-distribution`,
        {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('authToken')}`
          },
          body: formData
        }
      );

      const result = await response.json();

      if (result.success) {
        setCurrentJob(result.data);
        setProgress({
          jobId: result.data.jobId,
          progressPercentage: 0,
          status: 'Pending'
        });
        alert('Upload successful! Processing started.');
      } else {
        alert(`Error: ${result.message}`);
      }
    } catch (error) {
      console.error('Upload error:', error);
      alert('Upload failed. Please try again.');
    } finally {
      setIsUploading(false);
    }
  };

  // 3. Download result file
  const handleDownloadResult = async () => {
    if (!currentJob) return;

    const url = `${process.env.REACT_APP_API_URL}/api/v1/sponsorship/bulk-code-distribution/${currentJob.jobId}/result`;
    window.open(url, '_blank');
  };

  // 4. Render UI
  return (
    <div className="bulk-code-distribution">
      <h2>Bulk Code Distribution</h2>

      {/* Connection Status */}
      <div className="connection-status">
        {connection?.state === 'Connected' ? (
          <span className="connected">🟢 Real-time updates active</span>
        ) : (
          <span className="disconnected">🔴 Connecting...</span>
        )}
      </div>

      {/* File Upload */}
      <div className="upload-section">
        <input
          type="file"
          accept=".xlsx"
          onChange={(e) => setFile(e.target.files[0])}
          disabled={isUploading || (progress && progress.status === 'Processing')}
        />
        <label>
          <input
            type="checkbox"
            checked={sendSms}
            onChange={(e) => setSendSms(e.target.checked)}
            disabled={isUploading || (progress && progress.status === 'Processing')}
          />
          Send SMS to farmers
        </label>
        <button
          onClick={handleUpload}
          disabled={!file || isUploading || (progress && progress.status === 'Processing')}
        >
          {isUploading ? 'Uploading...' : 'Upload Excel'}
        </button>
      </div>

      {/* Progress Display */}
      {progress && (
        <div className="progress-section">
          <h3>Job Status: {progress.status}</h3>

          {/* Progress Bar */}
          <div className="progress-bar">
            <div
              className="progress-fill"
              style={{ width: `${progress.progressPercentage || 0}%` }}
            >
              {progress.progressPercentage || 0}%
            </div>
          </div>

          {/* Stats */}
          <div className="stats">
            <div className="stat">
              <span>Total Farmers:</span>
              <strong>{progress.totalFarmers || 0}</strong>
            </div>
            <div className="stat">
              <span>Processed:</span>
              <strong>{progress.processedFarmers || 0}</strong>
            </div>
            <div className="stat success">
              <span>Success:</span>
              <strong>{progress.successfulDistributions || 0}</strong>
            </div>
            <div className="stat failed">
              <span>Failed:</span>
              <strong>{progress.failedDistributions || 0}</strong>
            </div>
          </div>

          {/* Download Result Button */}
          {progress.status === 'Completed' && (
            <button onClick={handleDownloadResult} className="download-btn">
              📥 Download Result File
            </button>
          )}
        </div>
      )}
    </div>
  );
};

export default BulkCodeDistribution;
```

### Complete React Example (Admin - On Behalf Of)

```jsx
import React, { useState } from 'react';

const AdminBulkCodeDistribution = () => {
  const [file, setFile] = useState(null);
  const [sendSms, setSendSms] = useState(true);
  const [targetSponsorId, setTargetSponsorId] = useState('');
  const [currentJob, setCurrentJob] = useState(null);
  const [progress, setProgress] = useState(null);
  const [isUploading, setIsUploading] = useState(false);
  const [pollInterval, setPollInterval] = useState(null);

  // 1. Handle file upload
  const handleUpload = async () => {
    if (!file) {
      alert('Please select an Excel file');
      return;
    }

    if (!targetSponsorId) {
      alert('Please enter target sponsor ID');
      return;
    }

    setIsUploading(true);

    try {
      const formData = new FormData();
      formData.append('excelFile', file);
      formData.append('sendSms', sendSms);

      // CRITICAL: Query parameter for admin
      const url = `${process.env.REACT_APP_API_URL}/api/v1/sponsorship/bulk-code-distribution?onBehalfOfSponsorId=${targetSponsorId}`;

      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('adminToken')}`
        },
        body: formData
      });

      const result = await response.json();

      if (result.success) {
        setCurrentJob(result.data);
        setProgress({
          jobId: result.data.jobId,
          progressPercentage: 0,
          status: 'Pending'
        });

        // Start polling for progress (Admin doesn't get SignalR notifications)
        startPolling(result.data.jobId);

        alert('Upload successful! Processing started.');
      } else {
        alert(`Error: ${result.message}`);
      }
    } catch (error) {
      console.error('Upload error:', error);
      alert('Upload failed. Please try again.');
    } finally {
      setIsUploading(false);
    }
  };

  // 2. Polling for progress (Admin-specific)
  const startPolling = (jobId) => {
    const interval = setInterval(async () => {
      try {
        const response = await fetch(
          `${process.env.REACT_APP_API_URL}/api/v1/sponsorship/bulk-code-distribution/${jobId}`,
          {
            headers: {
              'Authorization': `Bearer ${localStorage.getItem('adminToken')}`
            }
          }
        );

        const result = await response.json();

        if (result.success) {
          setProgress(result.data);

          // Stop polling when completed
          if (result.data.status === 'Completed' ||
              result.data.status === 'Failed') {
            clearInterval(interval);
            setPollInterval(null);
            alert(`Job ${result.data.status}!\nSuccess: ${result.data.successfulDistributions}\nFailed: ${result.data.failedDistributions}`);
          }
        }
      } catch (error) {
        console.error('Polling error:', error);
      }
    }, 5000); // Poll every 5 seconds

    setPollInterval(interval);
  };

  // 3. Stop polling on unmount
  React.useEffect(() => {
    return () => {
      if (pollInterval) {
        clearInterval(pollInterval);
      }
    };
  }, [pollInterval]);

  // 4. Download result file
  const handleDownloadResult = async () => {
    if (!currentJob) return;

    const url = `${process.env.REACT_APP_API_URL}/api/v1/sponsorship/bulk-code-distribution/${currentJob.jobId}/result`;
    window.open(url, '_blank');
  };

  // 5. Render UI
  return (
    <div className="admin-bulk-code-distribution">
      <h2>Admin: Bulk Code Distribution (On Behalf Of)</h2>

      {/* Admin Notice */}
      <div className="admin-notice">
        ⚠️ You are acting on behalf of a sponsor. Progress updates use polling.
      </div>

      {/* Upload Section */}
      <div className="upload-section">
        <div className="form-group">
          <label>Target Sponsor ID *</label>
          <input
            type="number"
            value={targetSponsorId}
            onChange={(e) => setTargetSponsorId(e.target.value)}
            placeholder="Enter sponsor ID (e.g., 159)"
            disabled={isUploading || pollInterval !== null}
          />
        </div>

        <div className="form-group">
          <label>Excel File *</label>
          <input
            type="file"
            accept=".xlsx"
            onChange={(e) => setFile(e.target.files[0])}
            disabled={isUploading || pollInterval !== null}
          />
        </div>

        <div className="form-group">
          <label>
            <input
              type="checkbox"
              checked={sendSms}
              onChange={(e) => setSendSms(e.target.checked)}
              disabled={isUploading || pollInterval !== null}
            />
            Send SMS to farmers
          </label>
        </div>

        <button
          onClick={handleUpload}
          disabled={!file || !targetSponsorId || isUploading || pollInterval !== null}
        >
          {isUploading ? 'Uploading...' : 'Upload Excel (Admin)'}
        </button>
      </div>

      {/* Progress Display */}
      {progress && (
        <div className="progress-section">
          <h3>Job Status: {progress.status}</h3>

          {pollInterval && (
            <div className="polling-indicator">
              🔄 Polling for updates every 5 seconds...
            </div>
          )}

          {/* Progress Bar */}
          <div className="progress-bar">
            <div
              className="progress-fill"
              style={{ width: `${progress.progressPercentage || 0}%` }}
            >
              {progress.progressPercentage || 0}%
            </div>
          </div>

          {/* Stats */}
          <div className="stats">
            <div className="stat">
              <span>Job ID:</span>
              <strong>{progress.jobId}</strong>
            </div>
            <div className="stat">
              <span>Total Farmers:</span>
              <strong>{progress.totalFarmers || 0}</strong>
            </div>
            <div className="stat">
              <span>Processed:</span>
              <strong>{progress.processedFarmers || 0}</strong>
            </div>
            <div className="stat success">
              <span>Success:</span>
              <strong>{progress.successfulDistributions || 0}</strong>
            </div>
            <div className="stat failed">
              <span>Failed:</span>
              <strong>{progress.failedDistributions || 0}</strong>
            </div>
          </div>

          {/* Download Result Button */}
          {progress.status === 'Completed' && (
            <button onClick={handleDownloadResult} className="download-btn">
              📥 Download Result File
            </button>
          )}
        </div>
      )}
    </div>
  );
};

export default AdminBulkCodeDistribution;
```

### Angular Example (Sponsor)

```typescript
import { Component, OnInit, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../environments/environment';

interface BulkJobResponse {
  success: boolean;
  data: {
    jobId: number;
    totalFarmers: number;
    status: string;
  };
  message: string;
}

interface ProgressUpdate {
  jobId: number;
  sponsorId: number;
  status: string;
  totalFarmers: number;
  processedFarmers: number;
  successfulDistributions: number;
  failedDistributions: number;
  progressPercentage: number;
}

@Component({
  selector: 'app-bulk-code-distribution',
  templateUrl: './bulk-code-distribution.component.html'
})
export class BulkCodeDistributionComponent implements OnInit, OnDestroy {
  private hubConnection: HubConnection;

  file: File | null = null;
  sendSms: boolean = true;
  currentJob: any = null;
  progress: ProgressUpdate | null = null;
  isUploading: boolean = false;
  isConnected: boolean = false;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.initializeSignalR();
  }

  ngOnDestroy(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }

  // Initialize SignalR connection
  async initializeSignalR(): Promise<void> {
    const token = localStorage.getItem('authToken');

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/notifications`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    // Register event handlers
    this.hubConnection.on('BulkCodeDistributionProgress', (data: ProgressUpdate) => {
      console.log('📊 Progress:', data);
      this.progress = data;
    });

    this.hubConnection.on('BulkCodeDistributionCompleted', (data: any) => {
      console.log('✅ Completed:', data);
      if (this.progress) {
        this.progress.status = 'Completed';
      }
      alert(`Completed!\nSuccess: ${data.successCount}\nFailed: ${data.failedCount}`);
    });

    try {
      await this.hubConnection.start();
      this.isConnected = true;
      console.log('✅ SignalR connected');
    } catch (err) {
      console.error('❌ SignalR error:', err);
      this.isConnected = false;
    }
  }

  // Handle file selection
  onFileSelected(event: any): void {
    this.file = event.target.files[0];
  }

  // Upload Excel file
  async uploadExcel(): Promise<void> {
    if (!this.file) {
      alert('Please select an Excel file');
      return;
    }

    this.isUploading = true;

    const formData = new FormData();
    formData.append('excelFile', this.file);
    formData.append('sendSms', this.sendSms.toString());

    const headers = new HttpHeaders({
      'Authorization': `Bearer ${localStorage.getItem('authToken')}`
    });

    try {
      const result = await this.http.post<BulkJobResponse>(
        `${environment.apiUrl}/api/v1/sponsorship/bulk-code-distribution`,
        formData,
        { headers }
      ).toPromise();

      if (result.success) {
        this.currentJob = result.data;
        this.progress = {
          jobId: result.data.jobId,
          sponsorId: 0,
          status: 'Pending',
          totalFarmers: result.data.totalFarmers,
          processedFarmers: 0,
          successfulDistributions: 0,
          failedDistributions: 0,
          progressPercentage: 0
        };
        alert('Upload successful! Processing started.');
      } else {
        alert(`Error: ${result.message}`);
      }
    } catch (error) {
      console.error('Upload error:', error);
      alert('Upload failed. Please try again.');
    } finally {
      this.isUploading = false;
    }
  }

  // Download result file
  downloadResult(): void {
    if (!this.currentJob) return;

    const url = `${environment.apiUrl}/api/v1/sponsorship/bulk-code-distribution/${this.currentJob.jobId}/result`;
    window.open(url, '_blank');
  }
}
```

---

## 🎨 UI/UX Recommendations

### 1. Upload Screen

**Elements:**
- File input (Excel only, .xlsx)
- SMS toggle checkbox
- Sponsor ID input (Admin only, required)
- Upload button
- Template download link
- Validation hints

**Wireframe:**
```
┌────────────────────────────────────────────────────┐
│ Bulk Code Distribution                             │
├────────────────────────────────────────────────────┤
│                                                    │
│ [Admin Only]                                       │
│ Target Sponsor ID: [___________] *                 │
│                                                    │
│ Excel File: [Choose File] [No file chosen]        │
│                                                    │
│ ☑ Send SMS to farmers                             │
│                                                    │
│ [📥 Download Template] [📤 Upload Excel]          │
│                                                    │
│ Max 2000 farmers • Max 5 MB file size             │
└────────────────────────────────────────────────────┘
```

### 2. Progress Screen

**Elements:**
- Job status badge
- Progress bar (animated)
- Real-time stats (total, processed, success, failed)
- Estimated time remaining
- Cancel button (optional)

**Wireframe:**
```
┌────────────────────────────────────────────────────┐
│ Processing... [Status: Processing]                 │
├────────────────────────────────────────────────────┤
│                                                    │
│ Progress: ████████████░░░░░░░░ 75%                │
│                                                    │
│ ┌──────────┬──────────┬──────────┬──────────┐     │
│ │ Total    │ Processed│ Success  │ Failed   │     │
│ │  150     │   113    │   108    │    5     │     │
│ └──────────┴──────────┴──────────┴──────────┘     │
│                                                    │
│ Estimated time remaining: 3 minutes                │
│                                                    │
│ 🔄 Real-time updates active                       │
│                                                    │
└────────────────────────────────────────────────────┘
```

### 3. Completion Screen

**Elements:**
- Success/failure summary
- Download result button
- View history button
- Upload new file button

**Wireframe:**
```
┌────────────────────────────────────────────────────┐
│ ✅ Completed Successfully                          │
├────────────────────────────────────────────────────┤
│                                                    │
│ Summary:                                           │
│ • Total farmers: 150                               │
│ • Successful: 145 (96.7%)                          │
│ • Failed: 5 (3.3%)                                 │
│ • Codes distributed: 145                           │
│ • SMS sent: 145                                    │
│                                                    │
│ Completed at: 2025-11-09 10:15:23                  │
│                                                    │
│ [📥 Download Result File] [📋 View History]       │
│                                                    │
│ [📤 Upload Another File]                          │
│                                                    │
└────────────────────────────────────────────────────┘
```

### 4. History Screen

**Elements:**
- Job list (table or cards)
- Status filter
- Date range filter
- Download result button per job
- Pagination

**Wireframe:**
```
┌────────────────────────────────────────────────────┐
│ Job History                                        │
├────────────────────────────────────────────────────┤
│ Filter: [All Status ▼] [Date Range ▼]             │
├────────────────────────────────────────────────────┤
│ Job ID | File Name        | Status    | Success   │
│ ─────────────────────────────────────────────────  │
│ 123    | farmers_nov.xlsx | Completed | 145/150   │
│        | 2025-11-09       | [📥 Download Result]  │
│ ─────────────────────────────────────────────────  │
│ 122    | farmers_oct.xlsx | Completed | 200/200   │
│        | 2025-10-15       | [📥 Download Result]  │
│ ─────────────────────────────────────────────────  │
│                                                    │
│ « Previous   [1] 2 3 4 5   Next »                 │
└────────────────────────────────────────────────────┘
```

### 5. Responsive Design

**Mobile Considerations:**
- Stack stats vertically
- Simplify progress bar
- Use bottom sheet for file upload
- Optimize table layout (cards on mobile)

---

## ⚠️ Error Handling

### Upload Errors

```javascript
const handleUploadError = (error, response) => {
  // Network error
  if (!response) {
    showError('Network error. Please check your connection and try again.');
    return;
  }

  // API error
  const { message } = response;

  // Common error scenarios
  const errorHandlers = {
    'Admin users must specify onBehalfOfSponsorId': () => {
      showError('Admin error: Please specify target sponsor ID in the form.');
    },
    'Geçersiz Excel formatı': () => {
      showError('Invalid Excel file. Please use the provided template.');
    },
    'Yetersiz kod': () => {
      showError('Insufficient codes. Please purchase more codes or reduce farmer count.');
    },
    'Maksimum .* farmer': () => {
      showError('Excel file exceeds maximum row count (2000 farmers).');
    },
    'Dosya boyutu fazla': () => {
      showError('File size exceeds 5 MB limit. Please reduce file size.');
    },
    'Unauthorized': () => {
      showError('Session expired. Please login again.');
      redirectToLogin();
    }
  };

  // Find matching error handler
  for (const [pattern, handler] of Object.entries(errorHandlers)) {
    if (message.match(new RegExp(pattern, 'i'))) {
      handler();
      return;
    }
  }

  // Generic error
  showError(`Upload failed: ${message}`);
};
```

### SignalR Connection Errors

```javascript
// Connection state monitoring
connection.onclose((error) => {
  console.error('SignalR disconnected:', error);
  showWarning('Real-time updates disconnected. Reconnecting...');
});

connection.onreconnecting((error) => {
  console.warn('SignalR reconnecting:', error);
  showWarning('Connection lost. Reconnecting...');
});

connection.onreconnected((connectionId) => {
  console.log('SignalR reconnected:', connectionId);
  showSuccess('Real-time updates restored.');
});

// Fallback to polling if SignalR fails
const setupFallbackPolling = (jobId) => {
  if (!connection || connection.state !== 'Connected') {
    console.warn('SignalR unavailable. Using polling fallback.');
    startPolling(jobId);
  }
};
```

### Validation Errors (Client-Side)

```javascript
const validateExcelFile = (file) => {
  const errors = [];

  // File type
  if (!file.name.match(/\.xlsx$/i)) {
    errors.push('File must be in .xlsx format');
  }

  // File size (5 MB)
  if (file.size > 5 * 1024 * 1024) {
    errors.push('File size must not exceed 5 MB');
  }

  return errors;
};

const validateAdminForm = (targetSponsorId, file) => {
  const errors = [];

  // Sponsor ID required for admin
  if (!targetSponsorId || targetSponsorId <= 0) {
    errors.push('Target sponsor ID is required');
  }

  // File validation
  errors.push(...validateExcelFile(file));

  return errors;
};
```

---

## ✅ Testing Checklist

### Sponsor Testing

- [ ] **File Upload**
  - [ ] Upload valid .xlsx file with 10 farmers
  - [ ] Upload valid .xlsx file with 2000 farmers
  - [ ] Try uploading invalid file format (.csv, .xls)
  - [ ] Try uploading file > 5 MB
  - [ ] Try uploading Excel with > 2000 rows
  - [ ] Upload Excel with missing columns
  - [ ] Upload Excel with invalid phone numbers

- [ ] **SignalR Connection**
  - [ ] Verify SignalR connects before upload
  - [ ] Receive progress updates in real-time
  - [ ] Receive completion notification
  - [ ] Test reconnection after network drop

- [ ] **Progress Tracking**
  - [ ] Progress bar updates smoothly
  - [ ] Stats update correctly (total, processed, success, failed)
  - [ ] Estimated time remaining is reasonable

- [ ] **Result Download**
  - [ ] Download result file after completion
  - [ ] Verify Excel columns (Row, Phone, Name, Status, Code, Error)
  - [ ] Verify successful allocations have codes
  - [ ] Verify failed allocations have error messages

- [ ] **Edge Cases**
  - [ ] Upload while another job is processing
  - [ ] Cancel/refresh page during processing
  - [ ] Token expiration during upload

### Admin Testing

- [ ] **File Upload (OBO)**
  - [ ] Upload valid Excel with `onBehalfOfSponsorId` parameter
  - [ ] Try upload without `onBehalfOfSponsorId` (expect error)
  - [ ] Try upload with invalid sponsor ID
  - [ ] Verify audit log entry created

- [ ] **Progress Tracking (Polling)**
  - [ ] Polling starts after upload
  - [ ] Progress updates every 5 seconds
  - [ ] Polling stops when job completes
  - [ ] Stats update correctly

- [ ] **Job Status Access**
  - [ ] Admin can view any job status
  - [ ] Sponsor can only view their own jobs

- [ ] **Job History Access**
  - [ ] Admin must specify `sponsorId` parameter
  - [ ] Try accessing history without `sponsorId` (expect error)
  - [ ] View correct sponsor's job history

- [ ] **Result Download**
  - [ ] Admin can download result for any job
  - [ ] Verify result file matches target sponsor's job

### Cross-Role Testing

- [ ] **Authorization**
  - [ ] Sponsor cannot use `onBehalfOfSponsorId` parameter
  - [ ] Admin cannot upload without `onBehalfOfSponsorId`
  - [ ] Invalid token returns 401 Unauthorized

- [ ] **SignalR Groups**
  - [ ] Sponsor receives notifications for their jobs
  - [ ] Sponsor receives notifications when admin uploads OBO
  - [ ] Admin does NOT receive SignalR notifications (polling only)

### Performance Testing

- [ ] **Load Testing**
  - [ ] Upload 2000 farmers (max limit)
  - [ ] Process 10 simultaneous jobs
  - [ ] SignalR connection stability under load

- [ ] **Stress Testing**
  - [ ] Upload files rapidly (10 uploads in 1 minute)
  - [ ] Multiple admins uploading OBO for same sponsor

---

## 📚 Additional Resources

### API Documentation
- Swagger UI: `https://your-api.com/swagger`
- Postman Collection: `ZiraAI_Complete_API_Collection_v6.1.json`

### Backend Documentation
- [Admin Bulk Distribution Implementation Plan](./ADMIN_BULK_DISTRIBUTION_ON_BEHALF_OF.md)
- [Bulk Farmer Code Distribution Design](../BULK_FARMER_CODE_DISTRIBUTION_DESIGN.md)

### SignalR Resources
- [Official SignalR JavaScript Client Docs](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client)
- [SignalR Connection Management](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client#connection-lifetime-events)

---

## 🔧 Configuration

### Environment Variables

```javascript
// React (.env)
REACT_APP_API_URL=https://your-api.com
REACT_APP_SIGNALR_HUB_URL=https://your-api.com/hubs/notifications

// Angular (environment.ts)
export const environment = {
  production: false,
  apiUrl: 'https://your-api.com',
  signalRHubUrl: 'https://your-api.com/hubs/notifications'
};
```

### CORS Configuration

Ensure backend allows SignalR connections:

```csharp
// Program.cs
app.UseCors(builder =>
{
    builder
        .WithOrigins("https://your-frontend.com")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // Required for SignalR
});
```

---

## 🎯 Summary

### Key Differences: Sponsor vs Admin

| Aspect | Sponsor | Admin |
|--------|---------|-------|
| **Query Parameter** | None | `?onBehalfOfSponsorId={id}` (required) |
| **Authorization** | `Bearer {sponsor_token}` | `Bearer {admin_token}` |
| **Progress Tracking** | SignalR real-time | Polling (5s interval) |
| **SignalR Group** | `sponsor_{userId}` | Not subscribed |
| **Job Access** | Own jobs only | Any job |
| **History Access** | Own history | Must specify `sponsorId` |
| **Audit Logging** | No | Yes (AdminAuditService) |

### Best Practices

1. **Always establish SignalR connection BEFORE uploading** (Sponsor)
2. **Always validate file on client-side** before upload
3. **Use polling for Admin** (SignalR doesn't notify admin)
4. **Handle reconnection gracefully** with automatic retry
5. **Provide clear error messages** for validation failures
6. **Show real-time progress** with smooth animations
7. **Enable result download** only after completion
8. **Test thoroughly** with edge cases and network failures

---

**Document Version:** 1.0
**Last Updated:** 2025-11-09
**Status:** ✅ Ready for Frontend Integration
