# Bulk Dealer Invitation - Frontend Integration Guide

## Table of Contents
1. [Overview](#overview)
2. [Architecture & Flow](#architecture--flow)
3. [API Endpoints](#api-endpoints)
4. [SignalR Integration](#signalr-integration)
5. [Excel File Format](#excel-file-format)
6. [Implementation Steps](#implementation-steps)
7. [Error Handling](#error-handling)
8. [UI/UX Recommendations](#uiux-recommendations)

---

## Overview

The Bulk Dealer Invitation system allows sponsors to upload Excel files containing dealer information and process invitations asynchronously with real-time progress updates via SignalR.

### Key Features
- **Excel Upload**: Support for .xlsx and .xls files (max 2000 dealers, max 5MB)
- **Async Processing**: RabbitMQ-based background processing
- **Real-time Updates**: SignalR notifications for progress and completion
- **Two Invitation Types**: 
  - **Invite**: Send invitation, dealer creates account
  - **AutoCreate**: Automatically create dealer account with credentials
- **Job History**: Track all bulk invitation jobs with status and results

### User Roles
- **Sponsor**: Can upload Excel files and track bulk invitation jobs
- **Admin**: Can view and manage all bulk invitation jobs

---

## Architecture & Flow

```
┌─────────────────┐
│   Frontend      │
│   (Angular)     │
└────────┬────────┘
         │ 1. Upload Excel
         ▼
┌─────────────────────────────────┐
│   POST /api/v1/sponsorship/     │
│   dealer/invite-bulk            │
└────────┬────────────────────────┘
         │ 2. Validate & Parse
         ▼
┌─────────────────────────────────┐
│   BulkDealerInvitationService   │
│   - Validate File               │
│   - Parse Excel                 │
│   - Check Code Availability     │
│   - Create BulkInvitationJob    │
└────────┬────────────────────────┘
         │ 3. Publish Messages
         ▼
┌─────────────────────────────────┐
│   RabbitMQ Queue                │
│   dealer-invitation-requests    │
│   (One message per dealer)      │
└────────┬────────────────────────┘
         │ 4. Consume Messages
         ▼
┌─────────────────────────────────┐
│   DealerInvitationConsumer      │
│   WorkerService                 │
└────────┬────────────────────────┘
         │ 5. Process via Hangfire
         ▼
┌─────────────────────────────────┐
│   DealerInvitationJobService    │
│   - Process Each Dealer         │
│   - Update BulkInvitationJob    │
│   - Send SignalR Notifications  │
└────────┬────────────────────────┘
         │ 6. Real-time Updates
         ▼
┌─────────────────────────────────┐
│   SignalR Hub                   │
│   /hubs/notifications           │
└────────┬────────────────────────┘
         │ 7. Push to Frontend
         ▼
┌─────────────────┐
│   Frontend      │
│   - Progress    │
│   - Completion  │
└─────────────────┘
```

---

## API Endpoints

### Base URL
- **Development**: `https://localhost:5001`
- **Staging**: `https://ziraai-api-sit.up.railway.app`
- **Production**: `https://ziraai.com`

### Required Headers
```http
Authorization: Bearer {JWT_TOKEN}
x-dev-arch-version: 1.0
```

---

### 1. Upload Excel for Bulk Invitation

**Endpoint**: `POST /api/v1/sponsorship/dealer/invite-bulk`

**Authorization**: Sponsor, Admin roles

**Content-Type**: `multipart/form-data`

#### Request Parameters

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `file` | File | ✅ Yes | Excel file (.xlsx or .xls) |
| `invitationType` | string | ✅ Yes | "Invite" or "AutoCreate" |
| `defaultTier` | string | ❌ No | Default package tier (S/M/L/XL) if not specified in Excel |
| `defaultCodeCount` | int | ❌ No | Default code count if not specified in Excel |
| `sendSms` | bool | ❌ No | Send SMS notifications (default: true) |

#### Request Example (JavaScript/Fetch)

```javascript
const formData = new FormData();
formData.append('file', fileInput.files[0]);
formData.append('invitationType', 'Invite'); // or 'AutoCreate'
formData.append('defaultTier', 'M');
formData.append('defaultCodeCount', '50');
formData.append('sendSms', 'true');

const response = await fetch('https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invite-bulk', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'x-dev-arch-version': '1.0'
  },
  body: formData
});

const result = await response.json();
```

#### Request Example (Angular/HttpClient)

```typescript
uploadBulkInvitation(file: File, invitationType: string, defaultTier?: string, defaultCodeCount?: number, sendSms: boolean = true) {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('invitationType', invitationType);
  
  if (defaultTier) {
    formData.append('defaultTier', defaultTier);
  }
  
  if (defaultCodeCount) {
    formData.append('defaultCodeCount', defaultCodeCount.toString());
  }
  
  formData.append('sendSms', sendSms.toString());

  return this.http.post<ApiResponse<BulkInvitationJobDto>>(
    `${this.baseUrl}/api/v1/sponsorship/dealer/invite-bulk`,
    formData,
    { headers: this.getHeaders() }
  );
}

private getHeaders() {
  return new HttpHeaders({
    'Authorization': `Bearer ${this.authService.getToken()}`,
    'x-dev-arch-version': '1.0'
  });
}
```

#### Success Response (200 OK)

```json
{
  "success": true,
  "message": "Toplu davetiye işlemi başlatıldı. 150 bayi için davetiyeler kuyruğa alındı.",
  "data": {
    "id": 42,
    "sponsorId": 159,
    "invitationType": "Invite",
    "defaultTier": "M",
    "defaultCodeCount": 50,
    "sendSms": true,
    "totalDealers": 150,
    "processedDealers": 0,
    "successfulInvitations": 0,
    "failedInvitations": 0,
    "status": "Pending",
    "createdDate": "2025-11-03T14:30:00Z",
    "startedDate": null,
    "completedDate": null,
    "originalFileName": "dealers_list.xlsx",
    "fileSize": 245760,
    "resultFileUrl": null,
    "errorSummary": null
  }
}
```

#### Error Responses

**400 Bad Request - File Validation Error**
```json
{
  "success": false,
  "message": "Dosya yüklenemedi",
  "errors": [
    "Dosya boyutu 5MB'dan küçük olmalıdır",
    "Sadece .xlsx ve .xls uzantılı dosyalar desteklenir"
  ]
}
```

**400 Bad Request - Excel Validation Error**
```json
{
  "success": false,
  "message": "Excel dosyası geçersiz",
  "errors": [
    "Satır 5: Email formatı geçersiz (invalid@)",
    "Satır 12: Telefon numarası formatı geçersiz (123456)",
    "Satır 8: Email zaten kullanımda (existing@dealer.com)",
    "Satır 15: Telefon numarası zaten kullanımda (905551234567)"
  ]
}
```

**400 Bad Request - Code Availability Error**
```json
{
  "success": false,
  "message": "Yetersiz kod. Sponsor'ün 200 kodu var ancak 250 kod gerekiyor. Eksik: 50 kod",
  "errors": []
}
```

**401 Unauthorized**
```json
{
  "success": false,
  "message": "Yetkilendirme başarısız",
  "errors": []
}
```

**403 Forbidden**
```json
{
  "success": false,
  "message": "Bu işlem için yetkiniz yok",
  "errors": []
}
```

---

### 2. Get Bulk Invitation Job Status

**Endpoint**: `GET /api/v1/sponsorship/dealer/bulk-status/{jobId}`

**Authorization**: Sponsor, Admin roles

#### Request Example

```javascript
const response = await fetch(`https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/bulk-status/42`, {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'x-dev-arch-version': '1.0'
  }
});

const result = await response.json();
```

#### Angular Example

```typescript
getBulkInvitationJobStatus(jobId: number) {
  return this.http.get<ApiResponse<BulkInvitationJob>>(
    `${this.baseUrl}/api/v1/sponsorship/dealer/bulk-status/${jobId}`,
    { headers: this.getHeaders() }
  );
}
```

#### Success Response (200 OK)

```json
{
  "success": true,
  "message": "İşlem detayı başarıyla alındı",
  "data": {
    "id": 42,
    "sponsorId": 159,
    "invitationType": "Invite",
    "defaultTier": "M",
    "defaultCodeCount": 50,
    "sendSms": true,
    "totalDealers": 150,
    "processedDealers": 87,
    "successfulInvitations": 85,
    "failedInvitations": 2,
    "status": "Processing",
    "createdDate": "2025-11-03T14:30:00Z",
    "startedDate": "2025-11-03T14:30:05Z",
    "completedDate": null,
    "originalFileName": "dealers_list.xlsx",
    "fileSize": 245760,
    "resultFileUrl": null,
    "errorSummary": null
  }
}
```

#### Error Responses

**404 Not Found**
```json
{
  "success": false,
  "message": "İşlem bulunamadı",
  "errors": []
}
```

**403 Forbidden** (Trying to access another sponsor's job)
```json
{
  "success": false,
  "message": "Bu işlemi görüntüleme yetkiniz yok",
  "errors": []
}
```

---

### 3. Get Bulk Invitation Job History

**Endpoint**: `GET /api/v1/sponsorship/dealer/bulk-history`

**Authorization**: Sponsor, Admin roles

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | int | ❌ No | 1 | Page number |
| `pageSize` | int | ❌ No | 20 | Items per page |
| `status` | string | ❌ No | null | Filter by status (Pending, Processing, Completed, PartialSuccess, Failed) |

#### Request Example

```javascript
const response = await fetch('https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/bulk-history?page=1&pageSize=20&status=Completed', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'x-dev-arch-version': '1.0'
  }
});

const result = await response.json();
```

#### Angular Example

```typescript
getBulkInvitationJobHistory(page: number = 1, pageSize: number = 20, status?: string) {
  let params = new HttpParams()
    .set('page', page.toString())
    .set('pageSize', pageSize.toString());
  
  if (status) {
    params = params.set('status', status);
  }

  return this.http.get<ApiResponse<BulkInvitationJob[]>>(
    `${this.baseUrl}/api/v1/sponsorship/dealer/bulk-history`,
    { headers: this.getHeaders(), params }
  );
}
```

#### Success Response (200 OK)

```json
{
  "success": true,
  "message": "Toplu davetiye geçmişi başarıyla alındı",
  "data": [
    {
      "id": 42,
      "sponsorId": 159,
      "invitationType": "Invite",
      "defaultTier": "M",
      "defaultCodeCount": 50,
      "sendSms": true,
      "totalDealers": 150,
      "processedDealers": 150,
      "successfulInvitations": 148,
      "failedInvitations": 2,
      "status": "PartialSuccess",
      "createdDate": "2025-11-03T14:30:00Z",
      "startedDate": "2025-11-03T14:30:05Z",
      "completedDate": "2025-11-03T14:45:30Z",
      "originalFileName": "dealers_list.xlsx",
      "fileSize": 245760,
      "resultFileUrl": null,
      "errorSummary": "2 davetiye gönderilemedi: Email zaten kullanımda"
    },
    {
      "id": 41,
      "sponsorId": 159,
      "invitationType": "AutoCreate",
      "defaultTier": "S",
      "defaultCodeCount": 25,
      "sendSms": false,
      "totalDealers": 50,
      "processedDealers": 50,
      "successfulInvitations": 50,
      "failedInvitations": 0,
      "status": "Completed",
      "createdDate": "2025-11-02T10:15:00Z",
      "startedDate": "2025-11-02T10:15:03Z",
      "completedDate": "2025-11-02T10:20:45Z",
      "originalFileName": "dealers_batch_2.xlsx",
      "fileSize": 98304,
      "resultFileUrl": null,
      "errorSummary": null
    }
  ]
}
```

---

## SignalR Integration

### SignalR Hub URL

**Hub Endpoint**: `/hubs/notifications`

**Full URLs**:
- **Development**: `https://localhost:5001/hubs/notifications`
- **Staging**: `https://ziraai-api-sit.up.railway.app/hubs/notifications`
- **Production**: `https://ziraai.com/hubs/notifications`

### Connection Setup

#### JavaScript/SignalR Client

```javascript
import * as signalR from '@microsoft/signalr';

class NotificationService {
  private connection: signalR.HubConnection;

  constructor(private baseUrl: string, private getToken: () => string) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/notifications`, {
        accessTokenFactory: () => this.getToken()
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();
  }

  async start() {
    try {
      await this.connection.start();
      console.log('SignalR Connected');
      
      // Join sponsor group
      await this.connection.invoke('JoinSponsorGroup');
    } catch (err) {
      console.error('SignalR Connection Error:', err);
      setTimeout(() => this.start(), 5000);
    }
  }

  onBulkInvitationProgress(callback: (data: BulkInvitationProgressDto) => void) {
    this.connection.on('BulkInvitationProgress', callback);
  }

  onBulkInvitationCompleted(callback: (data: BulkInvitationCompletedDto) => void) {
    this.connection.on('BulkInvitationCompleted', callback);
  }

  async stop() {
    await this.connection.stop();
  }
}
```

#### Angular Service Example

```typescript
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';

export interface BulkInvitationProgressDto {
  bulkJobId: number;
  sponsorId: number;
  status: string;
  totalDealers: number;
  processedDealers: number;
  successfulInvitations: number;
  failedInvitations: number;
  progressPercentage: number;
  latestDealerEmail: string;
  latestDealerSuccess: boolean;
  latestDealerError?: string;
  lastUpdateTime: Date;
}

export interface BulkInvitationCompletedDto {
  bulkJobId: number;
  status: string;
  successCount: number;
  failedCount: number;
  completedAt: Date;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection;
  
  public bulkInvitationProgress$ = new Subject<BulkInvitationProgressDto>();
  public bulkInvitationCompleted$ = new Subject<BulkInvitationCompletedDto>();

  constructor(private authService: AuthService) {}

  public startConnection(baseUrl: string): Promise<void> {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/notifications`, {
        accessTokenFactory: () => this.authService.getToken()
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    return this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR Connection Started');
        this.registerEventHandlers();
        return this.joinSponsorGroup();
      })
      .catch(err => {
        console.error('Error starting SignalR connection:', err);
        // Retry after 5 seconds
        setTimeout(() => this.startConnection(baseUrl), 5000);
      });
  }

  private registerEventHandlers(): void {
    this.hubConnection.on('BulkInvitationProgress', (data: BulkInvitationProgressDto) => {
      console.log('Progress Update:', data);
      this.bulkInvitationProgress$.next(data);
    });

    this.hubConnection.on('BulkInvitationCompleted', (data: BulkInvitationCompletedDto) => {
      console.log('Job Completed:', data);
      this.bulkInvitationCompleted$.next(data);
    });
  }

  private joinSponsorGroup(): Promise<void> {
    return this.hubConnection.invoke('JoinSponsorGroup');
  }

  public stopConnection(): Promise<void> {
    if (this.hubConnection) {
      return this.hubConnection.stop();
    }
    return Promise.resolve();
  }
}
```

### SignalR Events

#### 1. BulkInvitationProgress

**Event Name**: `BulkInvitationProgress`

**Fired When**: After each dealer invitation is processed

**Payload**:
```typescript
interface BulkInvitationProgressDto {
  bulkJobId: number;
  sponsorId: number;
  status: string; // "Processing"
  totalDealers: number;
  processedDealers: number;
  successfulInvitations: number;
  failedInvitations: number;
  progressPercentage: number; // 0-100
  latestDealerEmail: string;
  latestDealerSuccess: boolean;
  latestDealerError?: string;
  lastUpdateTime: Date;
}
```

**Example Payload**:
```json
{
  "bulkJobId": 42,
  "sponsorId": 159,
  "status": "Processing",
  "totalDealers": 150,
  "processedDealers": 87,
  "successfulInvitations": 85,
  "failedInvitations": 2,
  "progressPercentage": 58.0,
  "latestDealerEmail": "dealer87@example.com",
  "latestDealerSuccess": true,
  "latestDealerError": null,
  "lastUpdateTime": "2025-11-03T14:42:15Z"
}
```

**Usage Example**:
```typescript
ngOnInit() {
  this.signalRService.bulkInvitationProgress$.subscribe((progress) => {
    // Update progress bar
    this.progressPercentage = progress.progressPercentage;
    
    // Update counters
    this.processedCount = progress.processedDealers;
    this.successCount = progress.successfulInvitations;
    this.failedCount = progress.failedInvitations;
    
    // Show latest dealer status
    if (progress.latestDealerSuccess) {
      this.showSuccessToast(`✅ ${progress.latestDealerEmail} başarıyla eklendi`);
    } else {
      this.showErrorToast(`❌ ${progress.latestDealerEmail}: ${progress.latestDealerError}`);
    }
  });
}
```

---

#### 2. BulkInvitationCompleted

**Event Name**: `BulkInvitationCompleted`

**Fired When**: All dealers have been processed

**Payload**:
```typescript
interface BulkInvitationCompletedDto {
  bulkJobId: number;
  status: string; // "Completed", "PartialSuccess", or "Failed"
  successCount: number;
  failedCount: number;
  completedAt: Date;
}
```

**Example Payload**:
```json
{
  "bulkJobId": 42,
  "status": "PartialSuccess",
  "successCount": 148,
  "failedCount": 2,
  "completedAt": "2025-11-03T14:45:30Z"
}
```

**Usage Example**:
```typescript
ngOnInit() {
  this.signalRService.bulkInvitationCompleted$.subscribe((completed) => {
    // Hide progress bar
    this.isProcessing = false;
    
    // Show completion notification
    if (completed.status === 'Completed') {
      this.showSuccessNotification(
        `🎉 Tüm davetiyeler başarıyla gönderildi! (${completed.successCount})`
      );
    } else if (completed.status === 'PartialSuccess') {
      this.showWarningNotification(
        `⚠️ ${completed.successCount} davetiye başarılı, ${completed.failedCount} başarısız`
      );
    } else {
      this.showErrorNotification(
        `❌ Tüm davetiyeler başarısız oldu (${completed.failedCount})`
      );
    }
    
    // Refresh job history
    this.loadJobHistory();
  });
}
```

---

## Excel File Format

### Required Columns

| Column Name | Type | Required | Description | Example |
|-------------|------|----------|-------------|---------|
| `Email` | string | ✅ Yes | Dealer email address | dealer@example.com |
| `Phone` | string | ✅ Yes | Turkish phone number | +905551234567 or 05551234567 |
| `DealerName` | string | ❌ No | Dealer full name | Ahmet Yılmaz |
| `PackageTier` | string | ❌ No | Package tier (S/M/L/XL) | M |
| `CodeCount` | int | ❌ No | Number of codes | 50 |

### Phone Number Formats (All Valid)

```
+905551234567
905551234567
05551234567
5551234567
```

### Excel Template Example

| Email | Phone | DealerName | PackageTier | CodeCount |
|-------|-------|------------|-------------|-----------|
| dealer1@example.com | +905551234567 | Ahmet Yılmaz | M | 50 |
| dealer2@example.com | 05552345678 | Mehmet Kaya | L | 100 |
| dealer3@example.com | 5553456789 | Ayşe Demir | S | 25 |
| dealer4@example.com | 905554567890 | | | |

**Notes**:
- First row must be headers
- `Email` and `Phone` are mandatory
- If `DealerName` is empty, email prefix will be used
- If `PackageTier` is empty, `defaultTier` from API request will be used
- If `CodeCount` is empty, `defaultCodeCount` from API request will be used
- Maximum 2000 rows (excluding header)
- File size maximum 5MB

### Download Template

Provide a template Excel file for download with proper headers and example rows:

```typescript
downloadTemplate() {
  const templateUrl = '/assets/templates/bulk_dealer_invitation_template.xlsx';
  window.open(templateUrl, '_blank');
}
```

---

## Implementation Steps

### Step 1: Setup SignalR Connection

```typescript
// app.component.ts or root component
export class AppComponent implements OnInit, OnDestroy {
  constructor(
    private signalRService: SignalRService,
    private authService: AuthService,
    private environment: EnvironmentService
  ) {}

  ngOnInit() {
    if (this.authService.isAuthenticated()) {
      this.signalRService.startConnection(this.environment.apiBaseUrl);
    }
  }

  ngOnDestroy() {
    this.signalRService.stopConnection();
  }
}
```

### Step 2: Create Upload Component

```typescript
// bulk-dealer-invitation.component.ts
export class BulkDealerInvitationComponent implements OnInit, OnDestroy {
  selectedFile: File | null = null;
  invitationType: string = 'Invite';
  defaultTier: string = 'M';
  defaultCodeCount: number = 50;
  sendSms: boolean = true;
  
  isUploading: boolean = false;
  isProcessing: boolean = false;
  currentJobId: number | null = null;
  
  progressData: BulkInvitationProgressDto | null = null;
  
  private progressSubscription?: Subscription;
  private completedSubscription?: Subscription;

  constructor(
    private dealerService: DealerService,
    private signalRService: SignalRService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.subscribeToSignalR();
  }

  ngOnDestroy() {
    this.progressSubscription?.unsubscribe();
    this.completedSubscription?.unsubscribe();
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    
    if (file) {
      // Validate file
      if (!file.name.endsWith('.xlsx') && !file.name.endsWith('.xls')) {
        this.toastService.error('Sadece .xlsx ve .xls dosyaları desteklenir');
        return;
      }
      
      if (file.size > 5 * 1024 * 1024) {
        this.toastService.error('Dosya boyutu 5MB\'dan küçük olmalıdır');
        return;
      }
      
      this.selectedFile = file;
    }
  }

  async uploadFile() {
    if (!this.selectedFile) {
      this.toastService.error('Lütfen bir dosya seçin');
      return;
    }

    this.isUploading = true;

    try {
      const response = await this.dealerService.uploadBulkInvitation(
        this.selectedFile,
        this.invitationType,
        this.defaultTier,
        this.defaultCodeCount,
        this.sendSms
      ).toPromise();

      if (response.success) {
        this.currentJobId = response.data.id;
        this.isProcessing = true;
        this.toastService.success(response.message);
      }
    } catch (error: any) {
      this.toastService.error(error.error?.message || 'Yükleme başarısız');
      
      if (error.error?.errors?.length > 0) {
        error.error.errors.forEach((err: string) => {
          this.toastService.error(err);
        });
      }
    } finally {
      this.isUploading = false;
    }
  }

  private subscribeToSignalR() {
    this.progressSubscription = this.signalRService.bulkInvitationProgress$
      .subscribe((progress) => {
        if (this.currentJobId === progress.bulkJobId) {
          this.progressData = progress;
        }
      });

    this.completedSubscription = this.signalRService.bulkInvitationCompleted$
      .subscribe((completed) => {
        if (this.currentJobId === completed.bulkJobId) {
          this.isProcessing = false;
          this.handleCompletion(completed);
        }
      });
  }

  private handleCompletion(completed: BulkInvitationCompletedDto) {
    if (completed.status === 'Completed') {
      this.toastService.success(
        `🎉 Tüm davetiyeler başarıyla gönderildi! (${completed.successCount})`
      );
    } else if (completed.status === 'PartialSuccess') {
      this.toastService.warning(
        `⚠️ ${completed.successCount} davetiye başarılı, ${completed.failedCount} başarısız`
      );
    } else {
      this.toastService.error(
        `❌ Tüm davetiyeler başarısız oldu (${completed.failedCount})`
      );
    }
    
    // Reset form
    this.selectedFile = null;
    this.currentJobId = null;
    this.progressData = null;
  }
}
```

### Step 3: Create Component Template

```html
<!-- bulk-dealer-invitation.component.html -->
<div class="bulk-invitation-container">
  <h2>Toplu Bayi Davetiyesi</h2>
  
  <!-- Upload Form -->
  <div class="upload-section" *ngIf="!isProcessing">
    <div class="form-group">
      <label>Davetiye Tipi</label>
      <select [(ngModel)]="invitationType" class="form-control">
        <option value="Invite">Davetiye Gönder (Bayi hesap oluşturur)</option>
        <option value="AutoCreate">Otomatik Oluştur (Sistem hesap oluşturur)</option>
      </select>
    </div>

    <div class="form-group">
      <label>Varsayılan Paket Seviyesi</label>
      <select [(ngModel)]="defaultTier" class="form-control">
        <option value="S">Small (S)</option>
        <option value="M">Medium (M)</option>
        <option value="L">Large (L)</option>
        <option value="XL">Extra Large (XL)</option>
      </select>
    </div>

    <div class="form-group">
      <label>Varsayılan Kod Sayısı</label>
      <input type="number" [(ngModel)]="defaultCodeCount" class="form-control" min="1" />
    </div>

    <div class="form-group">
      <label>
        <input type="checkbox" [(ngModel)]="sendSms" />
        SMS Bildirimi Gönder
      </label>
    </div>

    <div class="file-upload">
      <input type="file" (change)="onFileSelected($event)" accept=".xlsx,.xls" />
      <span *ngIf="selectedFile">{{ selectedFile.name }}</span>
    </div>

    <button 
      (click)="uploadFile()" 
      [disabled]="!selectedFile || isUploading"
      class="btn btn-primary">
      {{ isUploading ? 'Yükleniyor...' : 'Yükle ve İşleme Başla' }}
    </button>

    <a href="/assets/templates/bulk_dealer_invitation_template.xlsx" download class="btn btn-link">
      📥 Excel Şablonunu İndir
    </a>
  </div>

  <!-- Progress Section -->
  <div class="progress-section" *ngIf="isProcessing && progressData">
    <h3>İşlem Devam Ediyor...</h3>
    
    <div class="progress-bar-container">
      <div class="progress-bar" [style.width.%]="progressData.progressPercentage"></div>
    </div>
    
    <div class="progress-stats">
      <div class="stat">
        <span class="label">Toplam:</span>
        <span class="value">{{ progressData.totalDealers }}</span>
      </div>
      <div class="stat">
        <span class="label">İşlenen:</span>
        <span class="value">{{ progressData.processedDealers }}</span>
      </div>
      <div class="stat success">
        <span class="label">Başarılı:</span>
        <span class="value">{{ progressData.successfulInvitations }}</span>
      </div>
      <div class="stat error">
        <span class="label">Başarısız:</span>
        <span class="value">{{ progressData.failedInvitations }}</span>
      </div>
    </div>

    <div class="latest-update" *ngIf="progressData.latestDealerEmail">
      <span [class.success]="progressData.latestDealerSuccess" [class.error]="!progressData.latestDealerSuccess">
        {{ progressData.latestDealerSuccess ? '✅' : '❌' }}
        {{ progressData.latestDealerEmail }}
      </span>
      <small *ngIf="progressData.latestDealerError">{{ progressData.latestDealerError }}</small>
    </div>

    <p class="progress-percentage">{{ progressData.progressPercentage.toFixed(1) }}% Tamamlandı</p>
  </div>
</div>
```

### Step 4: Add Job History Component

```typescript
// bulk-invitation-history.component.ts
export class BulkInvitationHistoryComponent implements OnInit {
  jobs: BulkInvitationJob[] = [];
  isLoading: boolean = false;
  currentPage: number = 1;
  pageSize: number = 20;
  selectedStatus: string | null = null;

  constructor(private dealerService: DealerService) {}

  ngOnInit() {
    this.loadHistory();
  }

  async loadHistory() {
    this.isLoading = true;

    try {
      const response = await this.dealerService.getBulkInvitationJobHistory(
        this.currentPage,
        this.pageSize,
        this.selectedStatus
      ).toPromise();

      if (response.success) {
        this.jobs = response.data;
      }
    } catch (error) {
      console.error('Error loading history:', error);
    } finally {
      this.isLoading = false;
    }
  }

  filterByStatus(status: string | null) {
    this.selectedStatus = status;
    this.currentPage = 1;
    this.loadHistory();
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Completed': return 'badge-success';
      case 'PartialSuccess': return 'badge-warning';
      case 'Failed': return 'badge-danger';
      case 'Processing': return 'badge-info';
      case 'Pending': return 'badge-secondary';
      default: return 'badge-light';
    }
  }

  getStatusText(status: string): string {
    switch (status) {
      case 'Completed': return 'Tamamlandı';
      case 'PartialSuccess': return 'Kısmen Başarılı';
      case 'Failed': return 'Başarısız';
      case 'Processing': return 'İşleniyor';
      case 'Pending': return 'Bekliyor';
      default: return status;
    }
  }
}
```

```html
<!-- bulk-invitation-history.component.html -->
<div class="history-container">
  <h2>Toplu Davetiye Geçmişi</h2>

  <div class="filters">
    <button (click)="filterByStatus(null)" [class.active]="selectedStatus === null">
      Tümü
    </button>
    <button (click)="filterByStatus('Completed')" [class.active]="selectedStatus === 'Completed'">
      Tamamlandı
    </button>
    <button (click)="filterByStatus('PartialSuccess')" [class.active]="selectedStatus === 'PartialSuccess'">
      Kısmen Başarılı
    </button>
    <button (click)="filterByStatus('Processing')" [class.active]="selectedStatus === 'Processing'">
      İşleniyor
    </button>
    <button (click)="filterByStatus('Failed')" [class.active]="selectedStatus === 'Failed'">
      Başarısız
    </button>
  </div>

  <div class="jobs-list" *ngIf="!isLoading">
    <div class="job-card" *ngFor="let job of jobs">
      <div class="job-header">
        <span class="job-id">#{{ job.id }}</span>
        <span [class]="'badge ' + getStatusBadgeClass(job.status)">
          {{ getStatusText(job.status) }}
        </span>
      </div>

      <div class="job-details">
        <div class="detail-row">
          <span class="label">Dosya:</span>
          <span class="value">{{ job.originalFileName }} ({{ (job.fileSize / 1024).toFixed(0) }} KB)</span>
        </div>
        <div class="detail-row">
          <span class="label">Davetiye Tipi:</span>
          <span class="value">{{ job.invitationType === 'Invite' ? 'Davetiye Gönder' : 'Otomatik Oluştur' }}</span>
        </div>
        <div class="detail-row">
          <span class="label">Toplam Bayi:</span>
          <span class="value">{{ job.totalDealers }}</span>
        </div>
        <div class="detail-row">
          <span class="label">Başarılı:</span>
          <span class="value success">{{ job.successfulInvitations }}</span>
        </div>
        <div class="detail-row">
          <span class="label">Başarısız:</span>
          <span class="value error">{{ job.failedInvitations }}</span>
        </div>
        <div class="detail-row">
          <span class="label">Oluşturulma:</span>
          <span class="value">{{ job.createdDate | date:'dd.MM.yyyy HH:mm' }}</span>
        </div>
        <div class="detail-row" *ngIf="job.completedDate">
          <span class="label">Tamamlanma:</span>
          <span class="value">{{ job.completedDate | date:'dd.MM.yyyy HH:mm' }}</span>
        </div>
        <div class="detail-row" *ngIf="job.errorSummary">
          <span class="label">Hata Özeti:</span>
          <span class="value error">{{ job.errorSummary }}</span>
        </div>
      </div>
    </div>
  </div>

  <div class="loading" *ngIf="isLoading">
    Yükleniyor...
  </div>
</div>
```

---

## Error Handling

### Client-Side Validation

```typescript
validateFile(file: File): string[] {
  const errors: string[] = [];

  // Size validation
  if (file.size > 5 * 1024 * 1024) {
    errors.push('Dosya boyutu 5MB\'dan küçük olmalıdır');
  }

  // Extension validation
  if (!file.name.endsWith('.xlsx') && !file.name.endsWith('.xls')) {
    errors.push('Sadece .xlsx ve .xls dosyaları desteklenir');
  }

  return errors;
}
```

### Server Error Handling

```typescript
async uploadFile() {
  try {
    const response = await this.dealerService.uploadBulkInvitation(...).toPromise();
    
    if (response.success) {
      this.toastService.success(response.message);
    }
  } catch (error: any) {
    // HTTP error
    if (error.status === 400) {
      // Validation errors
      if (error.error?.errors?.length > 0) {
        error.error.errors.forEach((err: string) => {
          this.toastService.error(err);
        });
      } else {
        this.toastService.error(error.error?.message || 'Geçersiz istek');
      }
    } else if (error.status === 401) {
      this.toastService.error('Oturum süreniz doldu. Lütfen tekrar giriş yapın.');
      this.router.navigate(['/login']);
    } else if (error.status === 403) {
      this.toastService.error('Bu işlem için yetkiniz yok');
    } else if (error.status === 500) {
      this.toastService.error('Sunucu hatası. Lütfen daha sonra tekrar deneyin.');
    } else {
      this.toastService.error('Beklenmeyen bir hata oluştu');
    }
  }
}
```

### SignalR Reconnection Handling

```typescript
constructor() {
  this.hubConnection = new signalR.HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/notifications`, {
      accessTokenFactory: () => this.authService.getToken()
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (retryContext) => {
        // Exponential backoff: 0s, 2s, 5s, 10s, 30s
        const delays = [0, 2000, 5000, 10000, 30000];
        return delays[Math.min(retryContext.previousRetryCount, delays.length - 1)];
      }
    })
    .build();

  this.hubConnection.onreconnecting((error) => {
    console.warn('SignalR reconnecting...', error);
    this.toastService.warning('Bağlantı yeniden kuruluyor...');
  });

  this.hubConnection.onreconnected((connectionId) => {
    console.log('SignalR reconnected:', connectionId);
    this.toastService.success('Bağlantı yeniden kuruldu');
    
    // Re-join sponsor group
    this.joinSponsorGroup();
  });

  this.hubConnection.onclose((error) => {
    console.error('SignalR connection closed:', error);
    this.toastService.error('Bağlantı koptu. Sayfa yenilenecek.');
    
    // Retry connection after 5 seconds
    setTimeout(() => this.startConnection(baseUrl), 5000);
  });
}
```

---

## UI/UX Recommendations

### 1. File Upload UX

- **Drag & Drop Support**: Allow users to drag and drop Excel files
- **File Preview**: Show file name, size, and row count before upload
- **Validation Feedback**: Show real-time validation errors before upload
- **Template Download**: Provide easy access to Excel template

### 2. Progress Display

- **Visual Progress Bar**: Show percentage completion
- **Live Updates**: Update counters in real-time as dealers are processed
- **Latest Status**: Show the last processed dealer and their status
- **Estimated Time**: Calculate and display estimated completion time

### 3. Notifications

- **Toast Notifications**: Show success/error toasts for individual dealers
- **Sound Alerts**: Optional sound on completion
- **Browser Notifications**: Request permission for desktop notifications
- **Email Summary**: Send email summary on completion

### 4. Job History

- **Filtering**: Filter by status, date range, invitation type
- **Sorting**: Sort by date, success rate, total dealers
- **Details Modal**: Show detailed error logs in a modal
- **Export Results**: Allow downloading result files
- **Retry Failed**: Allow retrying failed invitations

### 5. Error Handling UX

- **Inline Validation**: Validate fields before submission
- **Error Summary**: Show all validation errors in a collapsible section
- **Helpful Messages**: Provide actionable error messages
- **Support Link**: Link to support/help page for complex errors

### 6. Mobile Responsiveness

- **Responsive Design**: Ensure UI works on tablets and phones
- **Touch-Friendly**: Large buttons and touch targets
- **Simplified Layout**: Show essential info on mobile, details in modal

### 7. Accessibility

- **ARIA Labels**: Proper labels for screen readers
- **Keyboard Navigation**: Full keyboard support
- **Color Contrast**: Ensure sufficient color contrast
- **Focus Management**: Proper focus management in modals

---

## Testing Checklist

### Unit Tests
- [ ] File validation (size, extension)
- [ ] Phone number format validation
- [ ] Email format validation
- [ ] SignalR event handlers
- [ ] Error handling logic

### Integration Tests
- [ ] API endpoint calls with valid data
- [ ] API endpoint calls with invalid data
- [ ] SignalR connection establishment
- [ ] SignalR event reception
- [ ] Authentication token handling

### E2E Tests
- [ ] Upload valid Excel file
- [ ] Upload invalid Excel file (size, format)
- [ ] Upload Excel with invalid data (email, phone)
- [ ] Receive progress updates via SignalR
- [ ] Receive completion notification
- [ ] View job history
- [ ] Filter job history by status
- [ ] Handle network disconnection
- [ ] Handle authentication expiration

### Performance Tests
- [ ] Upload file with 2000 dealers
- [ ] Measure average processing time per dealer
- [ ] Test SignalR connection with multiple users
- [ ] Test concurrent job processing

---

## Common Issues & Solutions

### Issue 1: SignalR Connection Fails

**Symptoms**: No real-time updates, connection errors in console

**Solutions**:
1. Check JWT token validity
2. Verify hub URL is correct
3. Check CORS configuration on server
4. Verify firewall/proxy settings
5. Check browser console for detailed errors

### Issue 2: Excel Upload Fails

**Symptoms**: 400 Bad Request, validation errors

**Solutions**:
1. Verify file format (.xlsx or .xls)
2. Check file size (<5MB)
3. Validate Excel columns match required format
4. Ensure no duplicate emails/phones in Excel
5. Check sponsor has sufficient codes

### Issue 3: Progress Updates Not Received

**Symptoms**: Upload succeeds but no SignalR updates

**Solutions**:
1. Verify SignalR connection is established
2. Check sponsor group join was successful
3. Verify `bulkJobId` matches between upload and SignalR
4. Check PlantAnalysisWorkerService is running
5. Verify RabbitMQ queue is being consumed

### Issue 4: Job Status Shows "Pending" Forever

**Symptoms**: Job created but never starts processing

**Solutions**:
1. Check PlantAnalysisWorkerService is running
2. Verify RabbitMQ connection is active
3. Check RabbitMQ queue has messages
4. Review worker service logs for errors
5. Verify database connection is healthy

---

## Summary

This integration guide provides everything needed to implement the Bulk Dealer Invitation feature on the frontend:

✅ **API Endpoints**: 3 endpoints with detailed request/response examples  
✅ **SignalR Integration**: Real-time progress and completion events  
✅ **Excel Format**: Clear specification with examples  
✅ **Implementation**: Step-by-step Angular examples  
✅ **Error Handling**: Comprehensive client and server error handling  
✅ **UI/UX Guidelines**: Best practices for user experience  
✅ **Testing**: Complete testing checklist  
✅ **Troubleshooting**: Common issues and solutions

**Next Steps**:
1. Implement SignalR service in Angular
2. Create upload component with progress tracking
3. Create job history component
4. Add comprehensive error handling
5. Test with sample Excel files
6. Deploy and monitor production usage

**Support**:
- Backend Developer: [Your Name]
- API Documentation: `/swagger`
- Test Environment: `https://ziraai-api-sit.up.railway.app`
