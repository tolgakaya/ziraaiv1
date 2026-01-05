# Bulk Farmer Invitations - Frontend API Reference

**Version**: 1.0
**Date**: 2026-01-05
**Target**: Frontend (Web/Mobile) Developers

---

## Table of Contents
1. [Quick Start](#quick-start)
2. [Authentication](#authentication)
3. [API Endpoint](#api-endpoint)
4. [Excel File Format](#excel-file-format)
5. [Request Examples](#request-examples)
6. [Response Format](#response-format)
7. [SignalR Real-time Updates](#signalr-real-time-updates)
8. [Error Handling](#error-handling)
9. [Complete Integration Example](#complete-integration-example)

---

## Quick Start

### What You Need
1. **JWT Token** (Sponsor or Admin role required)
2. **Excel File** (.xlsx format, see [Excel File Format](#excel-file-format))
3. **SignalR Connection** (for real-time progress updates)

### Basic Flow
```
1. User uploads Excel file
2. Frontend sends multipart/form-data POST request
3. Backend validates and queues invitations
4. Backend returns JobId
5. Frontend connects to SignalR hub
6. Backend sends progress updates via SignalR
7. Frontend displays real-time progress
8. Backend sends completion notification
9. Frontend shows final results
```

---

## Authentication

### Required Headers
```http
Authorization: Bearer {jwt_token}
Content-Type: multipart/form-data
```

### Required Roles
- **Sponsor** (GroupId = 3): Can create invitations for themselves
- **Admin** (GroupId = 1): Can create invitations for themselves

**⚠️ Note**: Only sponsors can use this endpoint. Admins creating invitations on behalf of sponsors should use a different endpoint (not implemented yet).

---

## API Endpoint

### POST /api/v1/sponsorship/farmer/invitations/bulk

**Base URL**:
- Development: `https://localhost:5001`
- Staging: `https://ziraai-api-sit.up.railway.app`
- Production: `https://api.ziraai.com`

**Full URL**: `{baseUrl}/api/v1/sponsorship/farmer/invitations/bulk`

### Request Method
`POST`

### Content Type
`multipart/form-data`

### Request Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `excelFile` | File | ✅ Yes | - | Excel file (.xlsx) with farmer data |
| `channel` | String | ❌ No | `"SMS"` | Delivery channel: `"SMS"` or `"WhatsApp"` |
| `customMessage` | String | ❌ No | `null` | Custom message template (not yet supported) |

**⚠️ IMPORTANT**:
- `channel` and `customMessage` are accepted by the API but **NOT YET SUPPORTED** by the underlying command
- All invitations currently use SMS with default template
- This is a known limitation documented for future enhancement

---

## Excel File Format

### Required Columns

| Column Name | Type | Required | Format | Example | Notes |
|-------------|------|----------|--------|---------|-------|
| `Phone` | String | ✅ Yes | 0XXXXXXXXXX or 905XXXXXXXXXX | `05551234567` or `905551234567` | Will be normalized to 11-digit Turkish format |
| `FarmerName` | String | ❌ No | Any text | `Ahmet Yılmaz` | Farmer's full name |
| `Email` | String | ❌ No | Valid email | `ahmet@example.com` | Farmer's email (optional) |
| `PackageTier` | String | ❌ No | S, M, L, XL | `M` | Code tier filter (case-insensitive) |
| `Notes` | String | ❌ No | Any text | `VIP farmer` | Internal notes |

### Excel Template

Download template: `FarmerInvitationsTemplate.xlsx`

**Excel Structure**:
```
Row 1 (Headers):
| Phone         | FarmerName    | Email              | PackageTier | Notes      |

Row 2 (Example):
| 05551234567   | Ahmet Yılmaz  | ahmet@example.com  | M           | VIP farmer |

Row 3 (Example):
| 905559876543  | Ayşe Demir    |                    | L           |            |
```

### Phone Number Formats (All Valid)

The API automatically normalizes phone numbers to 11-digit Turkish format:

| Input Format | Normalized Output | Valid? |
|--------------|-------------------|--------|
| `05551234567` | `05551234567` | ✅ Yes |
| `5551234567` | `05551234567` | ✅ Yes |
| `905551234567` | `05551234567` | ✅ Yes |
| `+905551234567` | `05551234567` | ✅ Yes |
| `0 555 123 45 67` | `05551234567` | ✅ Yes |
| `0555-123-45-67` | `05551234567` | ✅ Yes |

**❌ Invalid Formats**:
- Less than 10 digits
- More than 12 digits
- Non-Turkish numbers (must start with 5 after country code)

### Excel Validation Rules

**Before Upload** (Frontend should validate):
1. File extension must be `.xlsx`
2. File size < 5MB recommended
3. Maximum 1000 rows recommended (no hard limit)
4. First row must be headers
5. Phone column must exist
6. No completely empty rows

**Backend Validation** (Will reject if):
1. File is empty or corrupted
2. No valid rows found (after skipping header)
3. Phone numbers invalid (will skip individual rows)
4. Sponsor has insufficient codes

### Sample Excel File

Create file named `FarmerInvitationsTemplate.xlsx`:

```
Sheet1:
┌───────────────┬──────────────┬───────────────────┬─────────────┬────────────┐
│ Phone         │ FarmerName   │ Email             │ PackageTier │ Notes      │
├───────────────┼──────────────┼───────────────────┼─────────────┼────────────┤
│ 05551234567   │ Ahmet Yılmaz │ ahmet@example.com │ M           │ VIP farmer │
│ 5559876543    │ Ayşe Demir   │                   │ L           │            │
│ 905557777777  │ Mehmet Kaya  │ mehmet@test.com   │ S           │ Test       │
└───────────────┴──────────────┴───────────────────┴─────────────┴────────────┘
```

---

## Request Examples

### 1. JavaScript (Fetch API)

```javascript
// HTML Form
<form id="bulkInviteForm">
  <input type="file" id="excelFile" accept=".xlsx" required />
  <select id="channel">
    <option value="SMS">SMS</option>
    <option value="WhatsApp">WhatsApp (Not yet supported)</option>
  </select>
  <button type="submit">Upload</button>
</form>

// JavaScript
document.getElementById('bulkInviteForm').addEventListener('submit', async (e) => {
  e.preventDefault();

  const fileInput = document.getElementById('excelFile');
  const channel = document.getElementById('channel').value;

  // Validate file
  if (!fileInput.files[0]) {
    alert('Please select an Excel file');
    return;
  }

  if (!fileInput.files[0].name.endsWith('.xlsx')) {
    alert('Please select a valid .xlsx file');
    return;
  }

  // Create FormData
  const formData = new FormData();
  formData.append('excelFile', fileInput.files[0]);
  formData.append('channel', channel);

  try {
    const response = await fetch('https://api.ziraai.com/api/v1/sponsorship/farmer/invitations/bulk', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
        // DO NOT set Content-Type header - browser will set it automatically with boundary
      },
      body: formData
    });

    const result = await response.json();

    if (result.success) {
      const jobId = result.data.jobId;
      console.log('Bulk job created:', jobId);

      // Connect to SignalR for progress updates
      connectToSignalR(jobId);

      // Show initial job info
      alert(`Job created! Total farmers: ${result.data.totalDealers}`);
    } else {
      alert(`Error: ${result.message}`);
    }
  } catch (error) {
    console.error('Upload failed:', error);
    alert('Upload failed. Please try again.');
  }
});
```

### 2. React Example

```typescript
import React, { useState } from 'react';
import axios from 'axios';

interface BulkInviteFormProps {
  onJobCreated: (jobId: number) => void;
}

export const BulkInviteForm: React.FC<BulkInviteFormProps> = ({ onJobCreated }) => {
  const [file, setFile] = useState<File | null>(null);
  const [channel, setChannel] = useState<'SMS' | 'WhatsApp'>('SMS');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!file) {
      setError('Please select an Excel file');
      return;
    }

    if (!file.name.endsWith('.xlsx')) {
      setError('Please select a valid .xlsx file');
      return;
    }

    setLoading(true);
    setError(null);

    const formData = new FormData();
    formData.append('excelFile', file);
    formData.append('channel', channel);

    try {
      const response = await axios.post(
        '/api/v1/sponsorship/farmer/invitations/bulk',
        formData,
        {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('jwt_token')}`
            // axios will automatically set Content-Type with boundary
          }
        }
      );

      if (response.data.success) {
        const jobId = response.data.data.jobId;
        onJobCreated(jobId);
      } else {
        setError(response.data.message);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Upload failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label htmlFor="excelFile">Excel File:</label>
        <input
          type="file"
          id="excelFile"
          accept=".xlsx"
          onChange={(e) => setFile(e.target.files?.[0] || null)}
          disabled={loading}
        />
      </div>

      <div>
        <label htmlFor="channel">Channel:</label>
        <select
          id="channel"
          value={channel}
          onChange={(e) => setChannel(e.target.value as 'SMS' | 'WhatsApp')}
          disabled={loading}
        >
          <option value="SMS">SMS</option>
          <option value="WhatsApp">WhatsApp (Not yet supported)</option>
        </select>
      </div>

      {error && <div className="error">{error}</div>}

      <button type="submit" disabled={loading}>
        {loading ? 'Uploading...' : 'Upload'}
      </button>
    </form>
  );
};
```

### 3. Angular Example

```typescript
import { Component } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Component({
  selector: 'app-bulk-invite',
  templateUrl: './bulk-invite.component.html'
})
export class BulkInviteComponent {
  selectedFile: File | null = null;
  channel: 'SMS' | 'WhatsApp' = 'SMS';
  loading = false;
  error: string | null = null;

  constructor(private http: HttpClient) {}

  onFileSelected(event: any): void {
    this.selectedFile = event.target.files[0];

    if (this.selectedFile && !this.selectedFile.name.endsWith('.xlsx')) {
      this.error = 'Please select a valid .xlsx file';
      this.selectedFile = null;
    }
  }

  onSubmit(): void {
    if (!this.selectedFile) {
      this.error = 'Please select an Excel file';
      return;
    }

    this.loading = true;
    this.error = null;

    const formData = new FormData();
    formData.append('excelFile', this.selectedFile);
    formData.append('channel', this.channel);

    const token = localStorage.getItem('jwt_token');
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });

    this.http.post<any>(
      '/api/v1/sponsorship/farmer/invitations/bulk',
      formData,
      { headers }
    ).subscribe({
      next: (response) => {
        if (response.success) {
          const jobId = response.data.jobId;
          console.log('Job created:', jobId);
          // Connect to SignalR
          this.connectToSignalR(jobId);
        } else {
          this.error = response.message;
        }
        this.loading = false;
      },
      error: (err) => {
        this.error = err.error?.message || 'Upload failed';
        this.loading = false;
      }
    });
  }

  private connectToSignalR(jobId: number): void {
    // See SignalR section below
  }
}
```

### 4. Flutter/Dart Example

```dart
import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';

class BulkFarmerInviteService {
  final Dio _dio = Dio();
  final String baseUrl = 'https://api.ziraai.com';

  Future<Map<String, dynamic>> uploadBulkInvitations({
    required String filePath,
    String channel = 'SMS',
  }) async {
    try {
      // Create FormData
      final formData = FormData.fromMap({
        'excelFile': await MultipartFile.fromFile(
          filePath,
          filename: filePath.split('/').last,
        ),
        'channel': channel,
      });

      // Get token from secure storage
      final token = await getAuthToken();

      // Send request
      final response = await _dio.post(
        '$baseUrl/api/v1/sponsorship/farmer/invitations/bulk',
        data: formData,
        options: Options(
          headers: {
            'Authorization': 'Bearer $token',
          },
        ),
      );

      if (response.data['success'] == true) {
        return response.data['data'];
      } else {
        throw Exception(response.data['message']);
      }
    } catch (e) {
      throw Exception('Upload failed: $e');
    }
  }

  Future<String> getAuthToken() async {
    // Get from secure storage
    return 'your_jwt_token';
  }
}

// Usage in widget
class BulkInviteScreen extends StatefulWidget {
  @override
  _BulkInviteScreenState createState() => _BulkInviteScreenState();
}

class _BulkInviteScreenState extends State<BulkInviteScreen> {
  final BulkFarmerInviteService _service = BulkFarmerInviteService();
  bool _loading = false;
  String? _error;

  Future<void> _pickAndUploadFile() async {
    try {
      // Pick file
      final result = await FilePicker.platform.pickFiles(
        type: FileType.custom,
        allowedExtensions: ['xlsx'],
      );

      if (result == null || result.files.isEmpty) return;

      setState(() {
        _loading = true;
        _error = null;
      });

      // Upload file
      final jobData = await _service.uploadBulkInvitations(
        filePath: result.files.first.path!,
        channel: 'SMS',
      );

      final jobId = jobData['jobId'];
      print('Job created: $jobId');

      // Connect to SignalR
      _connectToSignalR(jobId);

      setState(() => _loading = false);
    } catch (e) {
      setState(() {
        _error = e.toString();
        _loading = false;
      });
    }
  }

  void _connectToSignalR(int jobId) {
    // See SignalR section below
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Bulk Farmer Invitations')),
      body: Column(
        children: [
          ElevatedButton(
            onPressed: _loading ? null : _pickAndUploadFile,
            child: Text(_loading ? 'Uploading...' : 'Select Excel File'),
          ),
          if (_error != null)
            Text(_error!, style: TextStyle(color: Colors.red)),
        ],
      ),
    );
  }
}
```

---

## Response Format

### Success Response (200 OK)

```json
{
  "success": true,
  "message": "📱 3 çiftçi daveti kuyruğa alındı. İlerlemesini SignalR üzerinden takip edebilirsiniz.",
  "data": {
    "jobId": 42,
    "totalDealers": 3,
    "processedDealers": 0,
    "successfulInvitations": 0,
    "failedInvitations": 0,
    "status": "Pending",
    "defaultTier": null,
    "defaultCodeCount": 1,
    "sendSms": true,
    "createdDate": "2026-01-05T14:30:00Z",
    "startedDate": null,
    "completedDate": null,
    "originalFileName": "farmer_invitations.xlsx",
    "fileSize": 8192,
    "resultFileUrl": null,
    "errorSummary": null
  }
}
```

### Error Responses

#### 400 Bad Request - No File
```json
{
  "success": false,
  "message": "Excel dosyası zorunludur"
}
```

#### 400 Bad Request - Invalid Excel
```json
{
  "success": false,
  "message": "Excel dosyası okunamadı veya geçerli satır bulunamadı"
}
```

#### 400 Bad Request - Insufficient Codes
```json
{
  "success": false,
  "message": "Yetersiz kod. Mevcut: 10, İstenen: 50. Lütfen önce yeterli kod satın alın."
}
```

#### 401 Unauthorized
```json
{
  "success": false,
  "message": "Unauthorized"
}
```

#### 403 Forbidden
```json
{
  "success": false,
  "message": "Bu işlem için yetkiniz yok"
}
```

---

## SignalR Real-time Updates

### Hub URL

```
{baseUrl}/bulkInvitationHub
```

**Examples**:
- Development: `https://localhost:5001/bulkInvitationHub`
- Staging: `https://ziraai-api-sit.up.railway.app/bulkInvitationHub`
- Production: `https://api.ziraai.com/bulkInvitationHub`

### Authentication

SignalR connection requires JWT token in query string:

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${baseUrl}/bulkInvitationHub?access_token=${jwtToken}`)
  .build();
```

### Hub Methods to Listen

#### 1. `BulkInvitationProgress`

**Trigger**: After each farmer invitation is processed
**Frequency**: Every single invitation (real-time)

**Payload**:
```typescript
interface BulkInvitationProgressDto {
  bulkJobId: number;              // Job identifier
  sponsorId: number;              // Sponsor who created the job
  status: string;                 // "Pending" | "Processing" | "Completed" | "PartialSuccess" | "Failed"
  totalDealers: number;           // Total farmer count in job
  processedDealers: number;       // Farmers processed so far
  successfulInvitations: number;  // Successful invitation count
  failedInvitations: number;      // Failed invitation count
  progressPercentage: number;     // 0-100
  latestDealerEmail: string;      // Latest processed phone (field name is misleading)
  latestDealerSuccess: boolean;   // Was latest invitation successful?
  latestDealerError: string | null; // Error message if failed
  lastUpdateTime: string;         // ISO 8601 timestamp
}
```

**Example Payload**:
```json
{
  "bulkJobId": 42,
  "sponsorId": 123,
  "status": "Processing",
  "totalDealers": 100,
  "processedDealers": 45,
  "successfulInvitations": 43,
  "failedInvitations": 2,
  "progressPercentage": 45.0,
  "latestDealerEmail": "05551234567",
  "latestDealerSuccess": true,
  "latestDealerError": null,
  "lastUpdateTime": "2026-01-05T14:35:22Z"
}
```

**⚠️ Note**: `latestDealerEmail` field name is misleading - it contains the phone number, not email. This is for backward compatibility with dealer invitation system.

#### 2. `BulkInvitationCompleted`

**Trigger**: When all invitations are processed
**Frequency**: Once per job

**Payload**:
```typescript
interface BulkInvitationCompletedDto {
  bulkJobId: number;
  sponsorId: number;
  status: string;         // "Completed" | "PartialSuccess" | "Failed"
  successCount: number;
  failedCount: number;
  completedAt: string;    // ISO 8601 timestamp
}
```

**Example Payload**:
```json
{
  "bulkJobId": 42,
  "sponsorId": 123,
  "status": "Completed",
  "successCount": 98,
  "failedCount": 2,
  "completedAt": "2026-01-05T14:40:15Z"
}
```

### Complete SignalR Integration Examples

#### JavaScript/TypeScript

```javascript
import * as signalR from '@microsoft/signalr';

class BulkInvitationProgressTracker {
  private connection: signalR.HubConnection;
  private jobId: number;

  constructor(baseUrl: string, jwtToken: string, jobId: number) {
    this.jobId = jobId;

    // Create connection
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/bulkInvitationHub?access_token=${jwtToken}`, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000]) // Retry delays
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupHandlers();
  }

  private setupHandlers(): void {
    // Progress updates
    this.connection.on('BulkInvitationProgress', (progress: BulkInvitationProgressDto) => {
      // Only handle updates for this job
      if (progress.bulkJobId !== this.jobId) return;

      console.log('Progress update:', progress);
      this.handleProgress(progress);
    });

    // Completion notification
    this.connection.on('BulkInvitationCompleted', (completion: BulkInvitationCompletedDto) => {
      // Only handle updates for this job
      if (completion.bulkJobId !== this.jobId) return;

      console.log('Job completed:', completion);
      this.handleCompletion(completion);
    });

    // Connection lifecycle
    this.connection.onreconnecting((error) => {
      console.warn('SignalR reconnecting...', error);
      this.showReconnectingStatus();
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected:', connectionId);
      this.hideReconnectingStatus();
    });

    this.connection.onclose((error) => {
      console.error('SignalR connection closed:', error);
      this.showDisconnectedStatus();
    });
  }

  private handleProgress(progress: BulkInvitationProgressDto): void {
    // Update UI with progress
    const progressBar = document.getElementById('progressBar') as HTMLProgressElement;
    const statusText = document.getElementById('statusText') as HTMLSpanElement;
    const latestFarmer = document.getElementById('latestFarmer') as HTMLSpanElement;

    if (progressBar) {
      progressBar.value = progress.progressPercentage;
    }

    if (statusText) {
      statusText.textContent = `${progress.processedDealers} / ${progress.totalDealers} işlendi (${progress.successfulInvitations} başarılı, ${progress.failedInvitations} başarısız)`;
    }

    if (latestFarmer) {
      const icon = progress.latestDealerSuccess ? '✅' : '❌';
      const error = progress.latestDealerError ? ` - ${progress.latestDealerError}` : '';
      latestFarmer.textContent = `${icon} ${progress.latestDealerEmail}${error}`;
    }
  }

  private handleCompletion(completion: BulkInvitationCompletedDto): void {
    // Show completion modal/notification
    alert(`Toplu davet tamamlandı!\n\nBaşarılı: ${completion.successCount}\nBaşarısız: ${completion.failedCount}\nDurum: ${completion.status}`);

    // Disconnect SignalR
    this.disconnect();
  }

  private showReconnectingStatus(): void {
    const status = document.getElementById('connectionStatus');
    if (status) status.textContent = '🔄 Yeniden bağlanılıyor...';
  }

  private hideReconnectingStatus(): void {
    const status = document.getElementById('connectionStatus');
    if (status) status.textContent = '✅ Bağlı';
  }

  private showDisconnectedStatus(): void {
    const status = document.getElementById('connectionStatus');
    if (status) status.textContent = '❌ Bağlantı kesildi';
  }

  async connect(): Promise<void> {
    try {
      await this.connection.start();
      console.log('✅ SignalR connected');
    } catch (error) {
      console.error('❌ SignalR connection failed:', error);
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    try {
      await this.connection.stop();
      console.log('SignalR disconnected');
    } catch (error) {
      console.error('Error disconnecting SignalR:', error);
    }
  }
}

// Usage
const tracker = new BulkInvitationProgressTracker(
  'https://api.ziraai.com',
  localStorage.getItem('jwt_token')!,
  42 // jobId from upload response
);

await tracker.connect();
// Connection will automatically receive updates and handle completion
```

#### React Hook

```typescript
import { useEffect, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

interface ProgressState {
  totalDealers: number;
  processedDealers: number;
  successfulInvitations: number;
  failedInvitations: number;
  progressPercentage: number;
  status: string;
  latestFarmer?: {
    phone: string;
    success: boolean;
    error?: string;
  };
}

export const useBulkInvitationProgress = (
  baseUrl: string,
  jwtToken: string,
  jobId: number | null
) => {
  const [progress, setProgress] = useState<ProgressState | null>(null);
  const [completed, setCompleted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [connectionStatus, setConnectionStatus] = useState<'disconnected' | 'connecting' | 'connected' | 'reconnecting'>('disconnected');

  useEffect(() => {
    if (!jobId) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/bulkInvitationHub?access_token=${jwtToken}`, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Progress handler
    connection.on('BulkInvitationProgress', (progressDto: any) => {
      if (progressDto.bulkJobId !== jobId) return;

      setProgress({
        totalDealers: progressDto.totalDealers,
        processedDealers: progressDto.processedDealers,
        successfulInvitations: progressDto.successfulInvitations,
        failedInvitations: progressDto.failedInvitations,
        progressPercentage: progressDto.progressPercentage,
        status: progressDto.status,
        latestFarmer: {
          phone: progressDto.latestDealerEmail,
          success: progressDto.latestDealerSuccess,
          error: progressDto.latestDealerError
        }
      });
    });

    // Completion handler
    connection.on('BulkInvitationCompleted', (completionDto: any) => {
      if (completionDto.bulkJobId !== jobId) return;

      setCompleted(true);
      setProgress((prev) => ({
        ...prev!,
        status: completionDto.status
      }));
    });

    // Lifecycle handlers
    connection.onreconnecting(() => setConnectionStatus('reconnecting'));
    connection.onreconnected(() => setConnectionStatus('connected'));
    connection.onclose((err) => {
      setConnectionStatus('disconnected');
      if (err) setError(err.message);
    });

    // Connect
    setConnectionStatus('connecting');
    connection.start()
      .then(() => setConnectionStatus('connected'))
      .catch((err) => {
        setConnectionStatus('disconnected');
        setError(err.message);
      });

    // Cleanup
    return () => {
      connection.stop();
    };
  }, [baseUrl, jwtToken, jobId]);

  return { progress, completed, error, connectionStatus };
};

// Usage in component
const BulkInviteProgress: React.FC<{ jobId: number }> = ({ jobId }) => {
  const { progress, completed, error, connectionStatus } = useBulkInvitationProgress(
    'https://api.ziraai.com',
    localStorage.getItem('jwt_token')!,
    jobId
  );

  if (error) {
    return <div className="error">Connection error: {error}</div>;
  }

  if (!progress) {
    return <div>Connecting to progress updates...</div>;
  }

  return (
    <div>
      <div>Connection: {connectionStatus}</div>
      <progress value={progress.progressPercentage} max={100} />
      <div>
        {progress.processedDealers} / {progress.totalDealers} işlendi
        ({progress.successfulInvitations} başarılı, {progress.failedInvitations} başarısız)
      </div>
      {progress.latestFarmer && (
        <div>
          Son: {progress.latestFarmer.success ? '✅' : '❌'} {progress.latestFarmer.phone}
          {progress.latestFarmer.error && ` - ${progress.latestFarmer.error}`}
        </div>
      )}
      {completed && <div className="success">✅ Tamamlandı!</div>}
    </div>
  );
};
```

#### Flutter/Dart

```dart
import 'package:signalr_netcore/signalr_client.dart';

class BulkInvitationProgressTracker {
  final String baseUrl;
  final String jwtToken;
  final int jobId;

  HubConnection? _connection;

  // Callbacks
  final void Function(BulkInvitationProgressDto)? onProgress;
  final void Function(BulkInvitationCompletedDto)? onCompleted;
  final void Function(String)? onError;

  BulkInvitationProgressTracker({
    required this.baseUrl,
    required this.jwtToken,
    required this.jobId,
    this.onProgress,
    this.onCompleted,
    this.onError,
  });

  Future<void> connect() async {
    try {
      // Create connection
      _connection = HubConnectionBuilder()
        .withUrl(
          '$baseUrl/bulkInvitationHub?access_token=$jwtToken',
          HttpConnectionOptions(
            skipNegotiation: true,
            transport: HttpTransportType.WebSockets,
          ),
        )
        .withAutomaticReconnect(retryDelays: [0, 2000, 5000, 10000])
        .build();

      // Progress handler
      _connection!.on('BulkInvitationProgress', (arguments) {
        final progressData = arguments![0] as Map<String, dynamic>;

        if (progressData['bulkJobId'] != jobId) return;

        final progress = BulkInvitationProgressDto.fromJson(progressData);
        onProgress?.call(progress);
      });

      // Completion handler
      _connection!.on('BulkInvitationCompleted', (arguments) {
        final completionData = arguments![0] as Map<String, dynamic>;

        if (completionData['bulkJobId'] != jobId) return;

        final completion = BulkInvitationCompletedDto.fromJson(completionData);
        onCompleted?.call(completion);
      });

      // Lifecycle handlers
      _connection!.onreconnecting(({error}) {
        print('🔄 SignalR reconnecting...');
      });

      _connection!.onreconnected(({connectionId}) {
        print('✅ SignalR reconnected: $connectionId');
      });

      _connection!.onclose(({error}) {
        if (error != null) {
          print('❌ SignalR connection closed: $error');
          onError?.call(error.toString());
        }
      });

      // Start connection
      await _connection!.start();
      print('✅ SignalR connected');

    } catch (e) {
      print('❌ SignalR connection failed: $e');
      onError?.call(e.toString());
      rethrow;
    }
  }

  Future<void> disconnect() async {
    try {
      await _connection?.stop();
      print('SignalR disconnected');
    } catch (e) {
      print('Error disconnecting SignalR: $e');
    }
  }
}

class BulkInvitationProgressDto {
  final int bulkJobId;
  final int sponsorId;
  final String status;
  final int totalDealers;
  final int processedDealers;
  final int successfulInvitations;
  final int failedInvitations;
  final double progressPercentage;
  final String latestDealerEmail;
  final bool latestDealerSuccess;
  final String? latestDealerError;
  final String lastUpdateTime;

  BulkInvitationProgressDto({
    required this.bulkJobId,
    required this.sponsorId,
    required this.status,
    required this.totalDealers,
    required this.processedDealers,
    required this.successfulInvitations,
    required this.failedInvitations,
    required this.progressPercentage,
    required this.latestDealerEmail,
    required this.latestDealerSuccess,
    this.latestDealerError,
    required this.lastUpdateTime,
  });

  factory BulkInvitationProgressDto.fromJson(Map<String, dynamic> json) {
    return BulkInvitationProgressDto(
      bulkJobId: json['bulkJobId'],
      sponsorId: json['sponsorId'],
      status: json['status'],
      totalDealers: json['totalDealers'],
      processedDealers: json['processedDealers'],
      successfulInvitations: json['successfulInvitations'],
      failedInvitations: json['failedInvitations'],
      progressPercentage: (json['progressPercentage'] as num).toDouble(),
      latestDealerEmail: json['latestDealerEmail'],
      latestDealerSuccess: json['latestDealerSuccess'],
      latestDealerError: json['latestDealerError'],
      lastUpdateTime: json['lastUpdateTime'],
    );
  }
}

class BulkInvitationCompletedDto {
  final int bulkJobId;
  final int sponsorId;
  final String status;
  final int successCount;
  final int failedCount;
  final String completedAt;

  BulkInvitationCompletedDto({
    required this.bulkJobId,
    required this.sponsorId,
    required this.status,
    required this.successCount,
    required this.failedCount,
    required this.completedAt,
  });

  factory BulkInvitationCompletedDto.fromJson(Map<String, dynamic> json) {
    return BulkInvitationCompletedDto(
      bulkJobId: json['bulkJobId'],
      sponsorId: json['sponsorId'],
      status: json['status'],
      successCount: json['successCount'],
      failedCount: json['failedCount'],
      completedAt: json['completedAt'],
    );
  }
}

// Usage in widget
class BulkInviteProgressScreen extends StatefulWidget {
  final int jobId;

  const BulkInviteProgressScreen({required this.jobId});

  @override
  _BulkInviteProgressScreenState createState() => _BulkInviteProgressScreenState();
}

class _BulkInviteProgressScreenState extends State<BulkInviteProgressScreen> {
  BulkInvitationProgressTracker? _tracker;
  BulkInvitationProgressDto? _progress;
  bool _completed = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _initializeSignalR();
  }

  Future<void> _initializeSignalR() async {
    final token = await getAuthToken();

    _tracker = BulkInvitationProgressTracker(
      baseUrl: 'https://api.ziraai.com',
      jwtToken: token,
      jobId: widget.jobId,
      onProgress: (progress) {
        setState(() {
          _progress = progress;
        });
      },
      onCompleted: (completion) {
        setState(() {
          _completed = true;
        });

        showDialog(
          context: context,
          builder: (context) => AlertDialog(
            title: Text('Tamamlandı!'),
            content: Text('Başarılı: ${completion.successCount}\nBaşarısız: ${completion.failedCount}'),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: Text('Tamam'),
              ),
            ],
          ),
        );
      },
      onError: (error) {
        setState(() {
          _error = error;
        });
      },
    );

    await _tracker!.connect();
  }

  @override
  void dispose() {
    _tracker?.disconnect();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_error != null) {
      return Center(child: Text('Hata: $_error'));
    }

    if (_progress == null) {
      return Center(child: CircularProgressIndicator());
    }

    return Column(
      children: [
        LinearProgressIndicator(value: _progress!.progressPercentage / 100),
        SizedBox(height: 16),
        Text('${_progress!.processedDealers} / ${_progress!.totalDealers} işlendi'),
        Text('Başarılı: ${_progress!.successfulInvitations}, Başarısız: ${_progress!.failedInvitations}'),
        SizedBox(height: 16),
        if (_progress!.latestDealerEmail.isNotEmpty)
          Text(
            '${_progress!.latestDealerSuccess ? "✅" : "❌"} ${_progress!.latestDealerEmail}',
            style: TextStyle(
              color: _progress!.latestDealerSuccess ? Colors.green : Colors.red,
            ),
          ),
        if (_completed)
          Padding(
            padding: EdgeInsets.all(16),
            child: Text('✅ Tamamlandı!', style: TextStyle(color: Colors.green, fontSize: 18)),
          ),
      ],
    );
  }

  Future<String> getAuthToken() async {
    // Get from secure storage
    return 'your_jwt_token';
  }
}
```

---

## Error Handling

### Common Errors and Solutions

| Error | HTTP Code | Cause | Solution |
|-------|-----------|-------|----------|
| "Excel dosyası zorunludur" | 400 | No file uploaded | Ensure file is attached to FormData |
| "Excel dosyası okunamadı" | 400 | Corrupt/invalid Excel | Validate file before upload |
| "Yetersiz kod" | 400 | Insufficient codes | Sponsor needs to purchase more codes |
| "Unauthorized" | 401 | Invalid/expired token | Refresh JWT token |
| "Bu işlem için yetkiniz yok" | 403 | Wrong role | User must be Sponsor or Admin |
| SignalR connection failed | N/A | Network/auth issue | Check token, retry with backoff |

### Error Handling Best Practices

```typescript
class BulkInviteErrorHandler {
  static handleUploadError(error: any): string {
    // HTTP errors
    if (error.response) {
      const status = error.response.status;
      const message = error.response.data?.message;

      switch (status) {
        case 400:
          return message || 'Geçersiz istek';
        case 401:
          return 'Oturum süresi dolmuş. Lütfen tekrar giriş yapın.';
        case 403:
          return 'Bu işlem için yetkiniz yok';
        case 500:
          return 'Sunucu hatası. Lütfen daha sonra tekrar deneyin.';
        default:
          return message || 'Bilinmeyen hata';
      }
    }

    // Network errors
    if (error.request) {
      return 'Bağlantı hatası. İnternet bağlantınızı kontrol edin.';
    }

    // Client-side errors
    return error.message || 'Bilinmeyen hata';
  }

  static handleSignalRError(error: any): string {
    if (error?.message?.includes('Unauthorized')) {
      return 'SignalR bağlantısı için oturum süresi dolmuş';
    }

    if (error?.message?.includes('WebSocket')) {
      return 'WebSocket bağlantısı başarısız. Firewall ayarlarınızı kontrol edin.';
    }

    return error?.message || 'SignalR bağlantı hatası';
  }
}

// Usage
try {
  await uploadBulkInvitations(file);
} catch (error) {
  const errorMessage = BulkInviteErrorHandler.handleUploadError(error);
  showErrorNotification(errorMessage);
}
```

---

## Complete Integration Example

### Full Flow Implementation (React + TypeScript)

```typescript
import React, { useState } from 'react';
import { useBulkInvitationProgress } from './hooks/useBulkInvitationProgress';

interface BulkInvitePageProps {
  baseUrl: string;
  jwtToken: string;
}

export const BulkInvitePage: React.FC<BulkInvitePageProps> = ({ baseUrl, jwtToken }) => {
  const [file, setFile] = useState<File | null>(null);
  const [channel, setChannel] = useState<'SMS' | 'WhatsApp'>('SMS');
  const [jobId, setJobId] = useState<number | null>(null);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const { progress, completed, error: signalRError, connectionStatus } = useBulkInvitationProgress(
    baseUrl,
    jwtToken,
    jobId
  );

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];

    if (!selectedFile) {
      setFile(null);
      return;
    }

    // Validate file extension
    if (!selectedFile.name.endsWith('.xlsx')) {
      setUploadError('Lütfen geçerli bir .xlsx dosyası seçin');
      setFile(null);
      return;
    }

    // Validate file size (5MB max recommended)
    if (selectedFile.size > 5 * 1024 * 1024) {
      setUploadError('Dosya boyutu 5MB\'dan küçük olmalıdır');
      setFile(null);
      return;
    }

    setFile(selectedFile);
    setUploadError(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!file) {
      setUploadError('Lütfen bir Excel dosyası seçin');
      return;
    }

    setUploading(true);
    setUploadError(null);

    const formData = new FormData();
    formData.append('excelFile', file);
    formData.append('channel', channel);

    try {
      const response = await fetch(`${baseUrl}/api/v1/sponsorship/farmer/invitations/bulk`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${jwtToken}`
        },
        body: formData
      });

      const result = await response.json();

      if (!response.ok || !result.success) {
        throw new Error(result.message || 'Upload failed');
      }

      // Set job ID to start SignalR connection
      setJobId(result.data.jobId);

    } catch (error: any) {
      setUploadError(error.message);
    } finally {
      setUploading(false);
    }
  };

  const resetForm = () => {
    setFile(null);
    setJobId(null);
    setUploadError(null);
  };

  // Show upload form if no job is active
  if (!jobId) {
    return (
      <div className="bulk-invite-container">
        <h2>Toplu Çiftçi Daveti</h2>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="excelFile">Excel Dosyası (.xlsx):</label>
            <input
              type="file"
              id="excelFile"
              accept=".xlsx"
              onChange={handleFileChange}
              disabled={uploading}
            />
            {file && <div className="file-info">Seçili: {file.name} ({(file.size / 1024).toFixed(2)} KB)</div>}
          </div>

          <div className="form-group">
            <label htmlFor="channel">Kanal:</label>
            <select
              id="channel"
              value={channel}
              onChange={(e) => setChannel(e.target.value as 'SMS' | 'WhatsApp')}
              disabled={uploading}
            >
              <option value="SMS">SMS</option>
              <option value="WhatsApp">WhatsApp (Yakında)</option>
            </select>
          </div>

          {uploadError && <div className="error">{uploadError}</div>}

          <button type="submit" disabled={!file || uploading}>
            {uploading ? 'Yükleniyor...' : 'Yükle'}
          </button>
        </form>

        <div className="help-section">
          <h3>Yardım</h3>
          <ul>
            <li>Excel dosyası .xlsx formatında olmalıdır</li>
            <li>İlk satır başlık satırıdır (Phone, FarmerName, Email, PackageTier, Notes)</li>
            <li>Phone sütunu zorunludur, diğerleri opsiyoneldir</li>
            <li>Telefon formatı: 05551234567 veya 905551234567</li>
            <li><a href="/templates/FarmerInvitationsTemplate.xlsx" download>Örnek şablon indir</a></li>
          </ul>
        </div>
      </div>
    );
  }

  // Show progress if job is active
  return (
    <div className="bulk-invite-progress">
      <h2>Toplu Davet İlerlemesi</h2>

      <div className="connection-status">
        Bağlantı: {
          connectionStatus === 'connected' ? '✅ Bağlı' :
          connectionStatus === 'connecting' ? '🔄 Bağlanıyor...' :
          connectionStatus === 'reconnecting' ? '🔄 Yeniden bağlanıyor...' :
          '❌ Bağlantı kesildi'
        }
      </div>

      {signalRError && <div className="error">SignalR Hatası: {signalRError}</div>}

      {progress && (
        <div className="progress-container">
          <div className="progress-bar-container">
            <progress value={progress.progressPercentage} max={100} />
            <span>{progress.progressPercentage.toFixed(1)}%</span>
          </div>

          <div className="progress-stats">
            <div>Toplam: {progress.totalDealers}</div>
            <div>İşlenen: {progress.processedDealers}</div>
            <div>✅ Başarılı: {progress.successfulInvitations}</div>
            <div>❌ Başarısız: {progress.failedInvitations}</div>
            <div>Durum: {progress.status}</div>
          </div>

          {progress.latestFarmer && (
            <div className="latest-farmer">
              <strong>Son işlenen:</strong>
              <span className={progress.latestFarmer.success ? 'success' : 'error'}>
                {progress.latestFarmer.success ? '✅' : '❌'} {progress.latestFarmer.phone}
                {progress.latestFarmer.error && ` - ${progress.latestFarmer.error}`}
              </span>
            </div>
          )}
        </div>
      )}

      {completed && (
        <div className="completion-message">
          <h3>✅ Toplu Davet Tamamlandı!</h3>
          <p>Başarılı: {progress?.successfulInvitations}</p>
          <p>Başarısız: {progress?.failedInvitations}</p>
          <button onClick={resetForm}>Yeni Davet Oluştur</button>
        </div>
      )}
    </div>
  );
};
```

---

## Testing Checklist

### Before Integration

- [ ] Confirm base URL for environment (dev/staging/prod)
- [ ] Obtain valid JWT token with Sponsor role
- [ ] Download Excel template
- [ ] Prepare test data (3-5 farmers)

### Upload Testing

- [ ] Test with valid .xlsx file
- [ ] Test with invalid file format (.xls, .csv)
- [ ] Test with empty file
- [ ] Test with missing Phone column
- [ ] Test with invalid phone formats
- [ ] Test with insufficient sponsor codes
- [ ] Test without authentication
- [ ] Test with expired token

### SignalR Testing

- [ ] Verify connection establishes
- [ ] Verify progress updates received
- [ ] Verify completion notification received
- [ ] Test reconnection after network interruption
- [ ] Test with multiple concurrent jobs
- [ ] Verify only relevant job updates are handled

### Edge Cases

- [ ] Large file (500+ rows)
- [ ] Duplicate phone numbers in Excel
- [ ] Special characters in farmer names
- [ ] Empty optional fields
- [ ] Mixed phone formats in single file

---

## FAQs

**Q: Can I use .xls format?**
A: No, only .xlsx (Excel 2007+) is supported.

**Q: What happens if a phone number is invalid?**
A: That row is skipped, and processing continues with other rows.

**Q: Can I track multiple bulk jobs simultaneously?**
A: Yes, but ensure your SignalR handler filters by `bulkJobId`.

**Q: Is WhatsApp delivery supported?**
A: Not yet. The API accepts `channel: "WhatsApp"` but currently only SMS is implemented.

**Q: What's the maximum file size?**
A: No hard limit, but 5MB is recommended for performance.

**Q: How long do jobs remain in the system?**
A: Jobs are stored indefinitely for audit purposes.

**Q: Can I cancel a running job?**
A: Not currently. This feature is planned for future releases.

**Q: Why is `latestDealerEmail` field name misleading?**
A: It's for backward compatibility with dealer invitation system. It contains phone number, not email.

---

## Support

For technical issues or questions:
- Backend Team: Check `claudedocs/AdminOperations/BULK_FARMER_INVITATIONS_IMPLEMENTATION_COMPLETE.md`
- API Issues: Contact backend team with error logs
- SignalR Issues: Verify WebSocket support and firewall settings

---

**Document Version**: 1.0
**Last Updated**: 2026-01-05
**Maintained By**: Backend Team
