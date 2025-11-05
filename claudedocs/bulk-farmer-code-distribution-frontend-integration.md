# Bulk Farmer Code Distribution - Frontend Integration Guide

**Complete Integration Documentation with SignalR Real-Time Notifications**

> ⚠️ **Pattern Matching**: This document provides a side-by-side comparison with the **Bulk Dealer Invitation** feature, which is already implemented and working in the frontend. Use the dealer invitation implementation as a reference pattern.

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Side-by-Side Comparison](#side-by-side-comparison)
3. [API Endpoints](#api-endpoints)
4. [Request/Response Examples](#requestresponse-examples)
5. [SignalR Integration](#signalr-integration)
6. [Implementation Steps](#implementation-steps)
7. [Error Handling](#error-handling)

---

## Overview

### Feature Comparison

| Feature | Bulk Dealer Invitation | Bulk Farmer Code Distribution |
|---------|----------------------|------------------------------|
| **Purpose** | Invite multiple dealers to join sponsor's network | Distribute sponsorship codes to multiple farmers |
| **Excel Format** | Email, Phone, DealerName | Email, Phone, FarmerName, CodeCount |
| **Max Records** | 1000 dealers | 2000 farmers |
| **Backend Pattern** | CQRS Command/Handler | Direct Service Call |
| **SignalR Events** | `BulkInvitationProgress`<br>`BulkInvitationCompleted` | `BulkCodeDistributionProgress`<br>`BulkCodeDistributionCompleted` |
| **Authorization** | `Sponsor`, `Admin` | `Sponsor`, `Admin` |

---

## Side-by-Side Comparison

### Endpoint Comparison

#### 1. Upload Excel & Start Job

| | Bulk Dealer Invitation | Bulk Farmer Code Distribution |
|---|----------------------|------------------------------|
| **Endpoint** | `POST /api/v1/sponsorship/dealer/invite-bulk` | `POST /api/v1/sponsorship/bulk-code-distribution` |
| **Parameters** | `excelFile` (IFormFile)<br>`invitationType` (string)<br>`sendSms` (bool)<br>`sendWhatsApp` (bool)<br>`message` (string) | `excelFile` (IFormFile)<br>`purchaseId` (int)<br>`sendSms` (bool) |
| **Response DTO** | `BulkInvitationJobDto` | `BulkCodeDistributionJobDto` |

#### 2. Check Job Status

| | Bulk Dealer Invitation | Bulk Farmer Code Distribution |
|---|----------------------|------------------------------|
| **Endpoint** | `GET /api/v1/sponsorship/dealer/bulk-status/{jobId}` | `GET /api/v1/sponsorship/bulk-code-distribution/status/{jobId}` |
| **Response DTO** | `BulkInvitationJob` (entity) | `BulkCodeDistributionProgressDto` |

#### 3. Get Job History

| | Bulk Dealer Invitation | Bulk Farmer Code Distribution |
|---|----------------------|------------------------------|
| **Endpoint** | `GET /api/v1/sponsorship/dealer/bulk-history?page=1&pageSize=20&status=Completed` | `GET /api/v1/sponsorship/bulk-code-distribution/history?page=1&pageSize=20&status=Completed` |
| **Response** | `List<BulkInvitationJob>` | `List<BulkCodeDistributionJob>` |

#### 4. Download Results

| | Bulk Dealer Invitation | Bulk Farmer Code Distribution |
|---|----------------------|------------------------------|
| **Endpoint** | N/A (not implemented) | `GET /api/v1/sponsorship/bulk-code-distribution/{jobId}/result` |
| **Response** | N/A | Result file URL (string) |

---

## API Endpoints

### 1. Upload Excel & Start Bulk Distribution

**Endpoint**: `POST /api/v1/sponsorship/bulk-code-distribution`

**Authorization**: `Bearer {token}` (Sponsor or Admin role required)

**Headers**:
```http
Content-Type: multipart/form-data
Authorization: Bearer eyJhbGci...
x-dev-arch-version: 1.0
```

**Request Body** (FormData):
```typescript
{
  excelFile: File,        // Excel file with farmer list
  purchaseId: number,     // Purchase ID to use for code distribution
  sendSms: boolean        // Send SMS notification to farmers (default: false)
}
```

**Excel File Format**:
| Column | Type | Required | Description |
|--------|------|----------|-------------|
| Email | string | ✅ | Farmer's email address |
| Phone | string | ✅ | Farmer's phone number (format: +905551234567) |
| FarmerName | string | ✅ | Farmer's full name |
| CodeCount | int | ❌ | Number of codes (default: 1, max: 10) |

**Example Excel**:
```
Email                  | Phone           | FarmerName      | CodeCount
-----------------------|-----------------|-----------------|----------
farmer1@example.com    | +905551234567   | Ahmet Yılmaz    | 1
farmer2@example.com    | +905557654321   | Mehmet Demir    | 2
farmer3@example.com    | +905559876543   | Ali Kaya        | 1
```

**Success Response** (200 OK):
```json
{
  "success": true,
  "message": "Bulk code distribution job created successfully",
  "data": {
    "jobId": 123,
    "totalFarmers": 150,
    "totalCodesRequired": 180,
    "availableCodes": 200,
    "status": "Pending",
    "createdDate": "2025-03-15T14:30:00Z",
    "estimatedCompletionTime": "2025-03-15T14:45:00Z",
    "statusCheckUrl": "/api/v1/sponsorship/bulk-code-distribution/status/123"
  }
}
```

**Error Response** (400 Bad Request):
```json
{
  "success": false,
  "message": "Yetersiz kod. Gerekli: 180, Mevcut: 150"
}
```

---

### 2. Check Job Status (Polling)

**Endpoint**: `GET /api/v1/sponsorship/bulk-code-distribution/status/{jobId}`

**Authorization**: `Bearer {token}` (Sponsor or Admin role required)

**Headers**:
```http
Authorization: Bearer eyJhbGci...
x-dev-arch-version: 1.0
```

**Success Response** (200 OK):
```json
{
  "success": true,
  "message": "Job durumu başarıyla alındı.",
  "data": {
    "jobId": 123,
    "sponsorId": 159,
    "status": "Processing",
    "totalFarmers": 150,
    "processedFarmers": 75,
    "successfulDistributions": 72,
    "failedDistributions": 3,
    "progressPercentage": 50,
    "totalCodesDistributed": 90,
    "totalSmsSent": 72,
    "createdDate": "2025-03-15T14:30:00Z",
    "startedDate": "2025-03-15T14:30:05Z",
    "completedDate": null,
    "estimatedTimeRemaining": "37.5 dakika",
    "resultFileUrl": null,
    "errorSummary": null
  }
}
```

**Error Response** (404 Not Found):
```json
{
  "success": false,
  "message": "Job bulunamadı veya erişim yetkiniz yok."
}
```

---

### 3. Get Job History

**Endpoint**: `GET /api/v1/sponsorship/bulk-code-distribution/history`

**Query Parameters**:
- `page` (int, optional): Page number (default: 1)
- `pageSize` (int, optional): Items per page (default: 20)
- `status` (string, optional): Filter by status (`Pending`, `Processing`, `Completed`, `PartialSuccess`, `Failed`)

**Example Request**:
```http
GET /api/v1/sponsorship/bulk-code-distribution/history?page=1&pageSize=10&status=Completed
Authorization: Bearer eyJhbGci...
x-dev-arch-version: 1.0
```

**Success Response** (200 OK):
```json
{
  "success": true,
  "message": "10 job bulundu.",
  "data": [
    {
      "id": 125,
      "sponsorId": 159,
      "purchaseId": 26,
      "status": "Completed",
      "totalFarmers": 200,
      "processedFarmers": 200,
      "successfulDistributions": 195,
      "failedDistributions": 5,
      "totalCodesDistributed": 245,
      "totalSmsSent": 195,
      "createdDate": "2025-03-14T10:00:00Z",
      "startedDate": "2025-03-14T10:00:05Z",
      "completedDate": "2025-03-14T10:25:00Z",
      "resultFileUrl": "https://example.com/results/bulk-125.xlsx"
    },
    {
      "id": 124,
      "sponsorId": 159,
      "purchaseId": 25,
      "status": "Completed",
      "totalFarmers": 100,
      "processedFarmers": 100,
      "successfulDistributions": 100,
      "failedDistributions": 0,
      "totalCodesDistributed": 100,
      "totalSmsSent": 100,
      "createdDate": "2025-03-13T15:30:00Z",
      "startedDate": "2025-03-13T15:30:05Z",
      "completedDate": "2025-03-13T15:42:00Z",
      "resultFileUrl": "https://example.com/results/bulk-124.xlsx"
    }
  ]
}
```

---

### 4. Download Result File

**Endpoint**: `GET /api/v1/sponsorship/bulk-code-distribution/{jobId}/result`

**Success Response** (200 OK):
```json
{
  "success": true,
  "message": "Sonuç dosyası URL'si alındı.",
  "data": "https://example.com/results/bulk-123.xlsx"
}
```

**Error Response** (404 Not Found):
```json
{
  "success": false,
  "message": "Sonuç dosyası henüz hazır değil."
}
```

---

## SignalR Integration

### Overview

Both bulk operations use **identical SignalR patterns** for real-time notifications. The frontend should implement the same SignalR client logic for both features.

### Connection Setup

```typescript
// SignalR Hub URL
const hubUrl = 'https://your-api-domain.com/notificationHub';

// Initialize SignalR connection
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl(hubUrl, {
    accessTokenFactory: () => getAuthToken(), // Your JWT token getter
  })
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Start connection
await connection.start();
console.log('SignalR connected');
```

### Event Registration

#### Side-by-Side Event Comparison

| Event Type | Bulk Dealer Invitation | Bulk Farmer Code Distribution |
|-----------|----------------------|------------------------------|
| **Progress Event** | `BulkInvitationProgress` | `BulkCodeDistributionProgress` |
| **Completion Event** | `BulkInvitationCompleted` | `BulkCodeDistributionCompleted` |

### Progress Notifications

#### Dealer Invitation Progress Event
```typescript
connection.on('BulkInvitationProgress', (progress: BulkInvitationProgressDto) => {
  console.log('📊 Dealer Invitation Progress:', progress);
  
  // Update UI
  updateProgressBar(progress.progressPercentage);
  updateStats({
    processed: progress.processedDealers,
    total: progress.totalDealers,
    success: progress.successfulInvitations,
    failed: progress.failedInvitations
  });
  
  // Show latest dealer info
  if (progress.latestDealerEmail) {
    showLatestUpdate(
      progress.latestDealerEmail,
      progress.latestDealerSuccess,
      progress.latestDealerError
    );
  }
});
```

**BulkInvitationProgressDto**:
```typescript
interface BulkInvitationProgressDto {
  bulkJobId: number;
  sponsorId: number;
  status: 'Processing' | 'Completed' | 'PartialSuccess';
  totalDealers: number;
  processedDealers: number;
  successfulInvitations: number;
  failedInvitations: number;
  progressPercentage: number;
  latestDealerEmail: string;
  latestDealerSuccess: boolean;
  latestDealerError: string;
  lastUpdateTime: string; // ISO 8601 date
}
```

#### Farmer Code Distribution Progress Event
```typescript
connection.on('BulkCodeDistributionProgress', (progress: BulkCodeDistributionProgressDto) => {
  console.log('📊 Code Distribution Progress:', progress);
  
  // Update UI
  updateProgressBar(progress.progressPercentage);
  updateStats({
    processed: progress.processedFarmers,
    total: progress.totalFarmers,
    success: progress.successfulDistributions,
    failed: progress.failedDistributions,
    codesDistributed: progress.totalCodesDistributed,
    smsSent: progress.totalSmsSent
  });
  
  // Update status text
  updateStatusText(progress.status);
});
```

**BulkCodeDistributionProgressDto**:
```typescript
interface BulkCodeDistributionProgressDto {
  jobId: number;
  sponsorId: number;
  status: 'Pending' | 'Processing' | 'Completed' | 'PartialSuccess' | 'Failed';
  totalFarmers: number;
  processedFarmers: number;
  successfulDistributions: number;
  failedDistributions: number;
  progressPercentage: number;
  totalCodesDistributed: number;
  totalSmsSent: number;
  createdDate: string;
  startedDate?: string;
  completedDate?: string;
  estimatedTimeRemaining?: string;
  resultFileUrl?: string;
  errorSummary?: string;
}
```

### Completion Notifications

#### Dealer Invitation Completion Event
```typescript
connection.on('BulkInvitationCompleted', (result: BulkInvitationCompletedDto) => {
  console.log('✅ Dealer Invitation Completed:', result);
  
  // Show completion notification
  showNotification({
    type: result.status === 'Completed' ? 'success' : 'warning',
    title: 'Toplu Davet Tamamlandı',
    message: `Başarılı: ${result.successCount}, Başarısız: ${result.failedCount}`,
  });
  
  // Update job list
  refreshJobList();
  
  // Reset progress UI
  resetProgressUI();
});
```

**BulkInvitationCompletedDto**:
```typescript
interface BulkInvitationCompletedDto {
  jobId: number;
  status: 'Completed' | 'PartialSuccess' | 'Failed';
  successCount: number;
  failedCount: number;
  completedAt: string; // ISO 8601 date
}
```

#### Farmer Code Distribution Completion Event
```typescript
connection.on('BulkCodeDistributionCompleted', (result: BulkCodeDistributionCompletedDto) => {
  console.log('✅ Code Distribution Completed:', result);
  
  // Show completion notification
  showNotification({
    type: result.status === 'Completed' ? 'success' : 'warning',
    title: 'Kod Dağıtımı Tamamlandı',
    message: `Başarılı: ${result.successCount}, Başarısız: ${result.failedCount}`,
  });
  
  // Enable download button
  enableResultDownload(result.jobId);
  
  // Update job list
  refreshJobList();
  
  // Reset progress UI
  resetProgressUI();
});
```

**BulkCodeDistributionCompletedDto**:
```typescript
interface BulkCodeDistributionCompletedDto {
  jobId: number;
  sponsorId: number;
  status: 'Completed' | 'PartialSuccess' | 'Failed';
  successCount: number;
  failedCount: number;
  completedAt: string; // ISO 8601 date
}
```

### Connection Lifecycle Management

```typescript
// Connection state management
connection.onreconnecting((error) => {
  console.warn('⚠️ SignalR reconnecting...', error);
  showConnectionStatus('reconnecting');
});

connection.onreconnected((connectionId) => {
  console.log('✅ SignalR reconnected:', connectionId);
  showConnectionStatus('connected');
  
  // Re-subscribe to groups (if needed)
  // rejoinSponsorGroup();
});

connection.onclose((error) => {
  console.error('❌ SignalR connection closed:', error);
  showConnectionStatus('disconnected');
  
  // Attempt manual reconnection after delay
  setTimeout(() => connection.start(), 5000);
});
```

---

## Implementation Steps

### Step 1: Upload Excel File

```typescript
async function uploadExcelForCodeDistribution(
  file: File,
  sendSms: boolean = false
): Promise<BulkCodeDistributionJobDto> {
  const formData = new FormData();
  formData.append('excelFile', file);
  formData.append('sendSms', sendSms.toString());

  const response = await fetch(
    'https://your-api-domain.com/api/v1/sponsorship/bulk-code-distribution',
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${getAuthToken()}`,
        'x-dev-arch-version': '1.0',
      },
      body: formData,
    }
  );

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Upload failed');
  }

  const result = await response.json();
  return result.data;
}
```

### Step 2: Initialize SignalR Connection

```typescript
async function initializeSignalR() {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('https://your-api-domain.com/notificationHub', {
      accessTokenFactory: () => getAuthToken(),
    })
    .withAutomaticReconnect()
    .build();

  // Register event handlers
  connection.on('BulkCodeDistributionProgress', handleProgressUpdate);
  connection.on('BulkCodeDistributionCompleted', handleCompletion);

  await connection.start();
  console.log('✅ SignalR connected');
  
  return connection;
}
```

### Step 3: Handle Real-Time Updates

```typescript
function handleProgressUpdate(progress: BulkCodeDistributionProgressDto) {
  // Update progress bar
  const progressBar = document.getElementById('progress-bar');
  progressBar.style.width = `${progress.progressPercentage}%`;
  progressBar.textContent = `${progress.progressPercentage}%`;

  // Update stats
  document.getElementById('processed-count').textContent = 
    `${progress.processedFarmers} / ${progress.totalFarmers}`;
  document.getElementById('success-count').textContent = 
    progress.successfulDistributions.toString();
  document.getElementById('failed-count').textContent = 
    progress.failedDistributions.toString();
  document.getElementById('codes-distributed').textContent = 
    progress.totalCodesDistributed.toString();
  document.getElementById('sms-sent').textContent = 
    progress.totalSmsSent.toString();

  // Update status
  updateStatus(progress.status);
}

function handleCompletion(result: BulkCodeDistributionCompletedDto) {
  // Show success notification
  showNotification({
    type: result.status === 'Completed' ? 'success' : 'warning',
    title: 'Kod Dağıtımı Tamamlandı',
    message: `Başarılı: ${result.successCount}, Başarısız: ${result.failedCount}`,
  });

  // Enable download button
  const downloadBtn = document.getElementById('download-results-btn');
  downloadBtn.disabled = false;
  downloadBtn.onclick = () => downloadResults(result.jobId);

  // Refresh job history
  refreshJobHistory();
}
```

### Step 4: Polling Fallback (Optional)

```typescript
async function pollJobStatus(jobId: number, intervalMs: number = 5000) {
  const intervalId = setInterval(async () => {
    try {
      const response = await fetch(
        `https://your-api-domain.com/api/v1/sponsorship/bulk-code-distribution/status/${jobId}`,
        {
          headers: {
            'Authorization': `Bearer ${getAuthToken()}`,
            'x-dev-arch-version': '1.0',
          },
        }
      );

      const result = await response.json();
      const progress = result.data;

      // Update UI
      handleProgressUpdate(progress);

      // Stop polling if completed
      if (['Completed', 'PartialSuccess', 'Failed'].includes(progress.status)) {
        clearInterval(intervalId);
        handleCompletion({
          jobId: progress.jobId,
          sponsorId: progress.sponsorId,
          status: progress.status,
          successCount: progress.successfulDistributions,
          failedCount: progress.failedDistributions,
          completedAt: progress.completedDate,
        });
      }
    } catch (error) {
      console.error('Error polling job status:', error);
    }
  }, intervalMs);

  return intervalId;
}
```

### Step 5: Download Results

```typescript
async function downloadResults(jobId: number) {
  try {
    const response = await fetch(
      `https://your-api-domain.com/api/v1/sponsorship/bulk-code-distribution/${jobId}/result`,
      {
        headers: {
          'Authorization': `Bearer ${getAuthToken()}`,
          'x-dev-arch-version': '1.0',
        },
      }
    );

    const result = await response.json();
    
    if (result.success) {
      // Open result file URL
      window.open(result.data, '_blank');
    } else {
      showNotification({
        type: 'error',
        title: 'Hata',
        message: result.message,
      });
    }
  } catch (error) {
    console.error('Error downloading results:', error);
  }
}
```

---

## Error Handling

### Common Error Scenarios

#### 1. Insufficient Codes
```json
{
  "success": false,
  "message": "Yetersiz kod. Gerekli: 180, Mevcut: 150"
}
```

**UI Action**: Show error message, suggest purchasing more codes

#### 2. Invalid Excel Format
```json
{
  "success": false,
  "message": "Geçersiz Excel formatı. Gerekli sütunlar: Email, Phone, FarmerName"
}
```

**UI Action**: Show error message with Excel template download link

#### 3. Excel File Too Large
```json
{
  "success": false,
  "message": "Maksimum 2000 çiftçi eklenebilir"
}
```

**UI Action**: Show error message, suggest splitting into multiple files

#### 4. Job Not Found
```json
{
  "success": false,
  "message": "Job bulunamadı veya erişim yetkiniz yok."
}
```

**UI Action**: Redirect to job history page

#### 5. SignalR Connection Lost

**Detection**:
```typescript
connection.onclose((error) => {
  console.error('❌ SignalR connection lost:', error);
  // Fallback to polling
  startPollingFallback();
});
```

**UI Action**: Show "Bağlantı kesildi, yeniden bağlanılıyor..." message, use polling fallback

---

## Complete React/Angular Example

### React Component

```typescript
import React, { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';

interface BulkCodeDistributionProps {
  authToken: string;
  sponsorId: number;
}

const BulkCodeDistribution: React.FC<BulkCodeDistributionProps> = ({ 
  authToken, 
  sponsorId 
}) => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [currentJob, setCurrentJob] = useState<BulkCodeDistributionJobDto | null>(null);
  const [progress, setProgress] = useState<BulkCodeDistributionProgressDto | null>(null);

  useEffect(() => {
    // Initialize SignalR
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://your-api-domain.com/notificationHub', {
        accessTokenFactory: () => authToken,
      })
      .withAutomaticReconnect()
      .build();

    newConnection.on('BulkCodeDistributionProgress', (data) => {
      console.log('📊 Progress:', data);
      setProgress(data);
    });

    newConnection.on('BulkCodeDistributionCompleted', (data) => {
      console.log('✅ Completed:', data);
      alert(`Tamamlandı! Başarılı: ${data.successCount}, Başarısız: ${data.failedCount}`);
      setCurrentJob(null);
      setProgress(null);
    });

    newConnection.start()
      .then(() => console.log('✅ SignalR connected'))
      .catch(err => console.error('❌ SignalR error:', err));

    setConnection(newConnection);

    return () => {
      newConnection.stop();
    };
  }, [authToken]);

  const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('excelFile', file);
    formData.append('sendSms', 'true');

    try {
      const response = await fetch(
        'https://your-api-domain.com/api/v1/sponsorship/bulk-code-distribution',
        {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${authToken}`,
            'x-dev-arch-version': '1.0',
          },
          body: formData,
        }
      );

      const result = await response.json();
      
      if (result.success) {
        setCurrentJob(result.data);
        alert(`Job başlatıldı! ID: ${result.data.jobId}`);
      } else {
        alert(`Hata: ${result.message}`);
      }
    } catch (error) {
      console.error('Upload error:', error);
      alert('Yükleme başarısız');
    }
  };

  return (
    <div className="bulk-code-distribution">
      <h2>Toplu Kod Dağıtımı</h2>
      
      <div className="upload-section">
        <input 
          type="file" 
          accept=".xlsx,.xls" 
          onChange={handleFileUpload}
          disabled={!!currentJob}
        />
      </div>

      {progress && (
        <div className="progress-section">
          <h3>İşlem Durumu</h3>
          <div className="progress-bar">
            <div 
              className="progress-fill" 
              style={{ width: `${progress.progressPercentage}%` }}
            >
              {progress.progressPercentage}%
            </div>
          </div>
          <div className="stats">
            <p>İşlenen: {progress.processedFarmers} / {progress.totalFarmers}</p>
            <p>Başarılı: {progress.successfulDistributions}</p>
            <p>Başarısız: {progress.failedDistributions}</p>
            <p>Dağıtılan Kodlar: {progress.totalCodesDistributed}</p>
            <p>Gönderilen SMS: {progress.totalSmsSent}</p>
          </div>
        </div>
      )}
    </div>
  );
};

export default BulkCodeDistribution;
```

---

## Summary

### Key Differences from Dealer Invitation

1. **Request Parameters**:
   - Dealer: `invitationType`, `sendSms`, `sendWhatsApp`, `message`
   - Farmer: `sendSms` only (purchaseId auto-selected)

2. **Response DTOs**:
   - Dealer: `BulkInvitationJobDto` (simpler)
   - Farmer: `BulkCodeDistributionJobDto` (includes `totalCodesRequired`, `availableCodes`, `estimatedCompletionTime`)

3. **Progress DTO Fields**:
   - Dealer: Includes `latestDealerEmail`, `latestDealerSuccess`, `latestDealerError`
   - Farmer: Includes `totalCodesDistributed`, `totalSmsSent`, `estimatedTimeRemaining`, `resultFileUrl`

4. **SignalR Events**:
   - Dealer: `BulkInvitationProgress`, `BulkInvitationCompleted`
   - Farmer: `BulkCodeDistributionProgress`, `BulkCodeDistributionCompleted`

5. **Result Download**:
   - Dealer: Not implemented
   - Farmer: Implemented (`GET /bulk-code-distribution/{jobId}/result`)

### Implementation Recommendation

**Use the existing dealer invitation frontend code as a template** and apply the following changes:

1. Duplicate dealer invitation component
2. Replace endpoint URLs
3. Update request parameters (remove `invitationType`, `purchaseId` is auto-selected)
4. Update SignalR event names
5. Add result download functionality
6. Update UI labels and messages

The SignalR integration pattern is **identical** - only event names and DTO structures differ.
