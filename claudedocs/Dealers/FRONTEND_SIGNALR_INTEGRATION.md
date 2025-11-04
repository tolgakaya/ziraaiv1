# Frontend SignalR Integration Guide - Bulk Dealer Invitations

**Date:** 2025-11-04
**Feature:** Real-time Bulk Invitation Progress Notifications
**Target Audience:** Frontend Developers

---

## 📋 Overview

This document describes how to integrate SignalR for receiving real-time progress notifications during bulk dealer invitation operations. The system sends live updates as each dealer invitation is processed, allowing the frontend to display progress bars, status updates, and completion messages.

---

## 🔌 SignalR Connection Setup

### Hub URL

```
WebSocket URL: wss://{API_BASE_URL}/hubs/notification
```

**Environments:**
- **Development:** `ws://localhost:5001/hubs/notification`
- **Staging:** `wss://ziraai-api-sit.up.railway.app/hubs/notification`
- **Production:** `wss://ziraai.com/hubs/notification`

### Authentication

SignalR requires JWT token authentication. The token must be passed in the query string during connection (SignalR cannot send custom headers during initial handshake).

**Connection URL Format:**
```
wss://{API_BASE_URL}/hubs/notification?access_token={JWT_TOKEN}
```

### Connection Configuration

```javascript
// Recommended SignalR client configuration
{
  skipNegotiation: false,           // Let SignalR negotiate best transport
  transport: HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling,

  // Timeout settings (matches backend configuration)
  serverTimeoutInMilliseconds: 30000,  // 30 seconds (matches ClientTimeoutInterval)
  keepAliveIntervalInMilliseconds: 15000, // 15 seconds (matches KeepAliveInterval)

  // Automatic reconnection
  withAutomaticReconnect: {
    nextRetryDelayInMilliseconds: (retryContext) => {
      if (retryContext.elapsedMilliseconds < 60000) {
        return Math.random() * 10000; // Retry in 0-10 seconds
      } else {
        return null; // Stop retrying after 1 minute
      }
    }
  }
}
```

---

## 📡 Events to Subscribe

### 1. BulkInvitationProgress

**Event Name:** `"BulkInvitationProgress"`

**When Fired:** After each dealer invitation is processed (success or failure)

**Frequency:** Once per dealer invitation (e.g., 6 times for 6 dealers)

**Message Structure:**
```typescript
interface BulkInvitationProgressDto {
  bulkJobId: number;              // Bulk job identifier
  sponsorId: number;              // Sponsor user ID
  status: string;                 // "Processing", "Completed", "PartialSuccess", "Failed"

  // Overall progress counters
  totalDealers: number;           // Total number of dealers to process
  processedDealers: number;       // Number of dealers processed so far
  successfulInvitations: number;  // Number of successful invitations
  failedInvitations: number;      // Number of failed invitations
  progressPercentage: number;     // Progress as decimal (e.g., 33.33, 66.67, 100.00)

  // Latest dealer info (from this specific update)
  latestDealerEmail: string;      // Email of the dealer just processed
  latestDealerSuccess: boolean;   // Whether this dealer's invitation succeeded
  latestDealerError: string | null; // Error message if failed, null if succeeded

  lastUpdateTime: string;         // ISO 8601 timestamp (e.g., "2025-11-04T20:30:00Z")
}
```

**Example Message:**
```json
{
  "bulkJobId": 42,
  "sponsorId": 159,
  "status": "Processing",
  "totalDealers": 6,
  "processedDealers": 3,
  "successfulInvitations": 3,
  "failedInvitations": 0,
  "progressPercentage": 50.00,
  "latestDealerEmail": "dealer3@example.com",
  "latestDealerSuccess": true,
  "latestDealerError": null,
  "lastUpdateTime": "2025-11-04T20:30:15.123Z"
}
```

---

### 2. BulkInvitationCompleted

**Event Name:** `"BulkInvitationCompleted"`

**When Fired:** Once when all dealers have been processed

**Frequency:** Once per bulk job (final notification)

**Message Structure:**
```typescript
interface BulkInvitationCompletedDto {
  bulkJobId: number;       // Bulk job identifier
  status: string;          // "Completed", "PartialSuccess", "Failed"
  successCount: number;    // Total successful invitations
  failedCount: number;     // Total failed invitations
  completedAt: string;     // ISO 8601 timestamp (e.g., "2025-11-04T20:30:30Z")
}
```

**Status Values:**
- `"Completed"`: All invitations succeeded (failedCount = 0)
- `"PartialSuccess"`: Some succeeded, some failed (successCount > 0 AND failedCount > 0)
- `"Failed"`: All invitations failed (successCount = 0)

**Example Message:**
```json
{
  "bulkJobId": 42,
  "status": "Completed",
  "successCount": 6,
  "failedCount": 0,
  "completedAt": "2025-11-04T20:30:30.456Z"
}
```

---

## 💻 Frontend Implementation Examples

### React + @microsoft/signalr

```typescript
import * as signalR from '@microsoft/signalr';

class BulkInvitationNotificationService {
  private connection: signalR.HubConnection | null = null;
  private token: string;
  private apiBaseUrl: string;

  constructor(token: string, apiBaseUrl: string) {
    this.token = token;
    this.apiBaseUrl = apiBaseUrl;
  }

  async connect() {
    // Create SignalR connection with JWT token
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiBaseUrl}/hubs/notification?access_token=${this.token}`, {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets |
                  signalR.HttpTransportType.ServerSentEvents |
                  signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.elapsedMilliseconds < 60000) {
            return Math.random() * 10000; // 0-10 seconds
          }
          return null; // Stop after 1 minute
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Subscribe to progress events
    this.connection.on('BulkInvitationProgress', (data: BulkInvitationProgressDto) => {
      console.log('📊 Progress update:', data);
      this.handleProgress(data);
    });

    // Subscribe to completion events
    this.connection.on('BulkInvitationCompleted', (data: BulkInvitationCompletedDto) => {
      console.log('✅ Bulk invitation completed:', data);
      this.handleCompleted(data);
    });

    // Connection lifecycle events
    this.connection.onreconnecting((error) => {
      console.warn('⚠️ SignalR reconnecting...', error);
      // Show "Reconnecting..." UI message
    });

    this.connection.onreconnected((connectionId) => {
      console.log('✅ SignalR reconnected:', connectionId);
      // Hide "Reconnecting..." message
    });

    this.connection.onclose((error) => {
      console.error('❌ SignalR connection closed:', error);
      // Show "Disconnected" message, offer manual reconnect
    });

    // Start connection
    try {
      await this.connection.start();
      console.log('✅ SignalR connected to NotificationHub');
    } catch (error) {
      console.error('❌ SignalR connection failed:', error);
      throw error;
    }
  }

  private handleProgress(data: BulkInvitationProgressDto) {
    // Update progress bar
    const progressBar = document.getElementById('bulk-progress-bar');
    if (progressBar) {
      progressBar.style.width = `${data.progressPercentage}%`;
      progressBar.textContent = `${data.processedDealers}/${data.totalDealers} (${data.progressPercentage.toFixed(1)}%)`;
    }

    // Update status text
    const statusText = document.getElementById('bulk-status-text');
    if (statusText) {
      if (data.latestDealerSuccess) {
        statusText.textContent = `✅ ${data.latestDealerEmail} - Başarılı`;
      } else {
        statusText.textContent = `❌ ${data.latestDealerEmail} - ${data.latestDealerError}`;
      }
    }

    // Update counters
    document.getElementById('success-count')!.textContent = data.successfulInvitations.toString();
    document.getElementById('failed-count')!.textContent = data.failedInvitations.toString();
  }

  private handleCompleted(data: BulkInvitationCompletedDto) {
    // Show completion notification
    let message = '';
    let type = '';

    if (data.status === 'Completed') {
      message = `🎉 Tüm davetiyeler başarıyla gönderildi! (${data.successCount} başarılı)`;
      type = 'success';
    } else if (data.status === 'PartialSuccess') {
      message = `⚠️ Kısmi başarı: ${data.successCount} başarılı, ${data.failedCount} başarısız`;
      type = 'warning';
    } else {
      message = `❌ Tüm davetiyeler başarısız oldu! (${data.failedCount} başarısız)`;
      type = 'error';
    }

    // Show toast/snackbar notification
    this.showNotification(message, type);

    // Refresh dealer list or navigate to results page
    this.refreshDealerList();
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.stop();
      console.log('🔌 SignalR disconnected');
    }
  }
}

// Usage in React component
const BulkInvitationPage = () => {
  const [notificationService, setNotificationService] = useState<BulkInvitationNotificationService | null>(null);

  useEffect(() => {
    const token = localStorage.getItem('jwt_token');
    const apiBaseUrl = process.env.REACT_APP_API_BASE_URL;

    if (token) {
      const service = new BulkInvitationNotificationService(token, apiBaseUrl);
      service.connect().then(() => {
        console.log('✅ Notification service connected');
        setNotificationService(service);
      });
    }

    // Cleanup on unmount
    return () => {
      notificationService?.disconnect();
    };
  }, []);

  // Component UI...
};
```

---

### Angular + @microsoft/signalr

```typescript
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';

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
  latestDealerError: string | null;
  lastUpdateTime: string;
}

export interface BulkInvitationCompletedDto {
  bulkJobId: number;
  status: string;
  successCount: number;
  failedCount: number;
  completedAt: string;
}

@Injectable({ providedIn: 'root' })
export class BulkInvitationNotificationService {
  private connection: signalR.HubConnection | null = null;

  private progressSubject = new BehaviorSubject<BulkInvitationProgressDto | null>(null);
  public progress$: Observable<BulkInvitationProgressDto | null> = this.progressSubject.asObservable();

  private completedSubject = new BehaviorSubject<BulkInvitationCompletedDto | null>(null);
  public completed$: Observable<BulkInvitationCompletedDto | null> = this.completedSubject.asObservable();

  constructor(
    private authService: AuthService,
    private envService: EnvironmentService
  ) {}

  async connect(): Promise<void> {
    const token = this.authService.getToken();
    const apiBaseUrl = this.envService.getApiBaseUrl();

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiBaseUrl}/hubs/notification?access_token=${token}`)
      .withAutomaticReconnect()
      .build();

    this.connection.on('BulkInvitationProgress', (data: BulkInvitationProgressDto) => {
      console.log('📊 Progress:', data);
      this.progressSubject.next(data);
    });

    this.connection.on('BulkInvitationCompleted', (data: BulkInvitationCompletedDto) => {
      console.log('✅ Completed:', data);
      this.completedSubject.next(data);
    });

    await this.connection.start();
    console.log('✅ SignalR connected');
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
    }
  }
}

// Usage in component
@Component({
  selector: 'app-bulk-invitation',
  templateUrl: './bulk-invitation.component.html'
})
export class BulkInvitationComponent implements OnInit, OnDestroy {
  progressPercentage = 0;
  statusMessage = '';
  successCount = 0;
  failedCount = 0;

  constructor(private notificationService: BulkInvitationNotificationService) {}

  ngOnInit() {
    this.notificationService.connect();

    this.notificationService.progress$.subscribe(progress => {
      if (progress) {
        this.progressPercentage = progress.progressPercentage;
        this.successCount = progress.successfulInvitations;
        this.failedCount = progress.failedInvitations;

        this.statusMessage = progress.latestDealerSuccess
          ? `✅ ${progress.latestDealerEmail} - Başarılı`
          : `❌ ${progress.latestDealerEmail} - ${progress.latestDealerError}`;
      }
    });

    this.notificationService.completed$.subscribe(completed => {
      if (completed) {
        // Show completion dialog/toast
        this.showCompletionDialog(completed);
      }
    });
  }

  ngOnDestroy() {
    this.notificationService.disconnect();
  }
}
```

---

### Vue 3 + @microsoft/signalr

```typescript
// composables/useBulkInvitationNotifications.ts
import { ref, onMounted, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '@/stores/auth';

export function useBulkInvitationNotifications() {
  const authStore = useAuthStore();

  const progress = ref<BulkInvitationProgressDto | null>(null);
  const completed = ref<BulkInvitationCompletedDto | null>(null);
  const isConnected = ref(false);

  let connection: signalR.HubConnection | null = null;

  const connect = async () => {
    const token = authStore.token;
    const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;

    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiBaseUrl}/hubs/notification?access_token=${token}`)
      .withAutomaticReconnect()
      .build();

    connection.on('BulkInvitationProgress', (data: BulkInvitationProgressDto) => {
      console.log('📊 Progress:', data);
      progress.value = data;
    });

    connection.on('BulkInvitationCompleted', (data: BulkInvitationCompletedDto) => {
      console.log('✅ Completed:', data);
      completed.value = data;
    });

    await connection.start();
    isConnected.value = true;
    console.log('✅ SignalR connected');
  };

  const disconnect = async () => {
    if (connection) {
      await connection.stop();
      isConnected.value = false;
    }
  };

  onMounted(() => connect());
  onUnmounted(() => disconnect());

  return {
    progress,
    completed,
    isConnected
  };
}

// Usage in component
<template>
  <div class="bulk-invitation-progress">
    <div v-if="progress" class="progress-bar">
      <div class="progress-fill" :style="{ width: progress.progressPercentage + '%' }">
        {{ progress.processedDealers }}/{{ progress.totalDealers }}
      </div>
    </div>

    <div v-if="progress" class="status-message">
      <span v-if="progress.latestDealerSuccess" class="success">
        ✅ {{ progress.latestDealerEmail }} - Başarılı
      </span>
      <span v-else class="error">
        ❌ {{ progress.latestDealerEmail }} - {{ progress.latestDealerError }}
      </span>
    </div>

    <div v-if="completed" class="completion-message">
      <h3>{{ completionTitle }}</h3>
      <p>Başarılı: {{ completed.successCount }} | Başarısız: {{ completed.failedCount }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useBulkInvitationNotifications } from '@/composables/useBulkInvitationNotifications';

const { progress, completed } = useBulkInvitationNotifications();

const completionTitle = computed(() => {
  if (!completed.value) return '';

  if (completed.value.status === 'Completed') {
    return '🎉 Tüm davetiyeler başarıyla gönderildi!';
  } else if (completed.value.status === 'PartialSuccess') {
    return '⚠️ Kısmi başarı';
  } else {
    return '❌ İşlem başarısız';
  }
});
</script>
```

---

## 🔐 Authentication & Authorization

### JWT Token Requirements

The JWT token must include:
```json
{
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "159",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "User 1114",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": ["Farmer", "Sponsor"]
}
```

### Automatic Group Subscription

When a sponsor connects to the NotificationHub, they are **automatically** added to the following groups:

1. `user_{userId}` - General user group
2. `sponsor_{userId}` - Sponsor-specific group (**used for bulk invitation notifications**)

**Example:** If userId is 159, the sponsor is added to:
- `user_159`
- `sponsor_159`

Bulk invitation notifications are sent to `sponsor_{sponsorId}`, so you don't need to manually join any groups. Just connect with a valid JWT token.

---

## 🧪 Testing Guide

### 1. Test Connection

```javascript
// Check if connection is established
connection.state === signalR.HubConnectionState.Connected
```

### 2. Test with Ping

```javascript
// Send ping to keep connection alive and test connectivity
await connection.invoke('Ping');
console.log('✅ Ping successful');
```

### 3. Trigger Test Notifications

**Step 1:** Upload a small Excel file (2-3 dealers) via bulk invitation API:
```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invite-bulk" \
  -H "Authorization: Bearer $SPONSOR_TOKEN" \
  -H "x-dev-arch-version: 1.0" \
  -F "SponsorId=159" \
  -F "ExcelFile=@test_dealers.xlsx" \
  -F "InvitationType=Invite" \
  -F "SendSms=false"
```

**Step 2:** Watch browser console for SignalR events:
```
📊 Progress: { processedDealers: 1, totalDealers: 3, progressPercentage: 33.33, ... }
📊 Progress: { processedDealers: 2, totalDealers: 3, progressPercentage: 66.67, ... }
📊 Progress: { processedDealers: 3, totalDealers: 3, progressPercentage: 100.00, ... }
✅ Completed: { status: "Completed", successCount: 3, failedCount: 0 }
```

---

## ⚠️ Common Issues & Troubleshooting

### Issue 1: Connection Fails with 401 Unauthorized

**Cause:** Invalid or expired JWT token

**Solution:**
- Verify token is valid and not expired
- Check token includes `NameIdentifier` and `Role` claims
- Ensure token is passed in query string: `?access_token={TOKEN}`

---

### Issue 2: No Events Received

**Cause:** Not subscribed to correct group or events not registered

**Solution:**
- Verify connection is established: `connection.state === Connected`
- Check event names are **exact** (case-sensitive):
  - ✅ `"BulkInvitationProgress"`
  - ❌ `"bulkinvitationprogress"`
- Ensure sponsor userId matches the one in JWT token

---

### Issue 3: Connection Drops Frequently

**Cause:** Network issues or timeout settings

**Solution:**
- Enable automatic reconnection:
  ```javascript
  .withAutomaticReconnect()
  ```
- Implement ping/keep-alive:
  ```javascript
  setInterval(() => {
    if (connection.state === signalR.HubConnectionState.Connected) {
      connection.invoke('Ping');
    }
  }, 30000); // Every 30 seconds
  ```

---

### Issue 4: Multiple Connections Created

**Cause:** Not cleaning up connections on component unmount

**Solution:**
- Always disconnect in cleanup:
  ```javascript
  // React
  useEffect(() => {
    return () => connection?.stop();
  }, []);

  // Angular
  ngOnDestroy() {
    this.connection?.stop();
  }

  // Vue
  onUnmounted(() => connection?.stop());
  ```

---

## 📊 Example Notification Flow

### Scenario: 6 Dealers Uploaded

```
📤 User uploads Excel with 6 dealers
⏳ Backend creates BulkInvitationJob (status: "Processing")
🔄 Hangfire starts processing invitations in parallel

// Event 1
📊 BulkInvitationProgress {
  processedDealers: 1,
  totalDealers: 6,
  progressPercentage: 16.67,
  latestDealerEmail: "dealer1@test.com",
  latestDealerSuccess: true,
  status: "Processing"
}

// Event 2
📊 BulkInvitationProgress {
  processedDealers: 2,
  totalDealers: 6,
  progressPercentage: 33.33,
  latestDealerEmail: "dealer2@test.com",
  latestDealerSuccess: true,
  status: "Processing"
}

// ... continues for dealers 3-5 ...

// Event 6 (last dealer)
📊 BulkInvitationProgress {
  processedDealers: 6,
  totalDealers: 6,
  progressPercentage: 100.00,
  latestDealerEmail: "dealer6@test.com",
  latestDealerSuccess: true,
  status: "Completed"
}

// Final event
✅ BulkInvitationCompleted {
  bulkJobId: 42,
  status: "Completed",
  successCount: 6,
  failedCount: 0,
  completedAt: "2025-11-04T20:30:30Z"
}
```

---

## 🔗 Related Documentation

- [Backend API - Bulk Invitation Endpoint](./FRONTEND_API_CHANGES.md)
- [Excel Format Guide](./BULK_INVITATION_EXCEL_FORMATS.md)
- [Production Deployment Status](./PRODUCTION_DEPLOYMENT_STATUS.md)

---

## 📞 Support

**Backend Developer:** Claude AI Assistant
**Documentation Date:** 2025-11-04
**SignalR Library:** `@microsoft/signalr` (npm)
**Backend Version:** .NET 9.0 + SignalR Core

---

**Last Updated:** 2025-11-04 21:00 UTC
**Status:** ✅ Production Ready
