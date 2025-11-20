# Analytics API - Frontend Integration Guide

## 📋 Overview

Bu doküman, sponsor-dealer performans analitiği için yeni geliştirilen Redis tabanlı cache API'lerinin frontend entegrasyonu için detaylı bilgiler içerir.

**Performans**: 5-15ms yanıt süresi (SQL sorguları: 150-300ms)
**Güncelleme**: Event-driven, gerçek zamanlı
**Cache Süresi**: 24 saat

---

## 🔐 Authentication

Tüm endpoints **Sponsor** rolü gerektirir.

```javascript
// Authorization header
{
  "Authorization": "Bearer {jwt_token}",
  "x-dev-arch-version": "1.0"
}
```

**Sponsor ID**: JWT token'dan otomatik olarak alınır (NameIdentifier claim)

---

## 📡 API Endpoints

### 1. Get Dealer Performance Analytics

**Tüm bayilerin veya belirli bir bayinin performans metriklerini getirir.**

#### Endpoint
```
GET /api/v1/sponsorship/analytics/dealer-performance
```

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `dealerId` | int | ❌ No | Belirli bir bayi için filtreleme. Boş ise tüm bayiler döner. |

#### Request Examples

**Tüm Bayilerin Analitiği:**
```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/analytics/dealer-performance" \
  -H "Authorization: Bearer eyJhbGc..." \
  -H "x-dev-arch-version: 1.0"
```

**Tek Bayinin Analitiği:**
```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/analytics/dealer-performance?dealerId=158" \
  -H "Authorization: Bearer eyJhbGc..." \
  -H "x-dev-arch-version: 1.0"
```

#### JavaScript/TypeScript Example
```typescript
// API Service
async getDealerPerformance(dealerId?: number): Promise<DealerSummaryDto> {
  const url = dealerId
    ? `/api/v1/sponsorship/analytics/dealer-performance?dealerId=${dealerId}`
    : `/api/v1/sponsorship/analytics/dealer-performance`;

  const response = await fetch(url, {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'x-dev-arch-version': '1.0'
    }
  });

  const result = await response.json();
  return result.data;
}
```

#### Response Structure

**Success Response (200 OK):**
```json
{
  "data": {
    "totalDealers": 3,
    "totalCodesDistributed": 150,
    "totalCodesUsed": 95,
    "totalCodesAvailable": 45,
    "totalCodesReclaimed": 10,
    "overallUsageRate": 63.33,
    "dealers": [
      {
        "dealerId": 158,
        "dealerName": "Ahmet Yılmaz",
        "dealerEmail": "ahmet.yilmaz@example.com",
        "totalCodesReceived": 50,
        "codesSent": 48,
        "codesUsed": 40,
        "codesAvailable": 2,
        "codesReclaimed": 0,
        "usageRate": 83.33,
        "uniqueFarmersReached": 35,
        "totalAnalyses": 120,
        "firstTransferDate": "2025-01-01T10:00:00",
        "lastTransferDate": "2025-01-04T15:30:00"
      },
      {
        "dealerId": 159,
        "dealerName": "Mehmet Demir",
        "dealerEmail": "mehmet.demir@example.com",
        "totalCodesReceived": 60,
        "codesSent": 55,
        "codesUsed": 35,
        "codesAvailable": 5,
        "codesReclaimed": 0,
        "usageRate": 63.64,
        "uniqueFarmersReached": 28,
        "totalAnalyses": 95,
        "firstTransferDate": "2025-01-02T08:15:00",
        "lastTransferDate": "2025-01-04T12:00:00"
      },
      {
        "dealerId": 160,
        "dealerName": "Ayşe Kaya",
        "dealerEmail": "ayse.kaya@example.com",
        "totalCodesReceived": 40,
        "codesSent": 37,
        "codesUsed": 20,
        "codesAvailable": 3,
        "codesReclaimed": 10,
        "usageRate": 54.05,
        "uniqueFarmersReached": 18,
        "totalAnalyses": 60,
        "firstTransferDate": "2024-12-28T14:20:00",
        "lastTransferDate": "2025-01-03T09:45:00"
      }
    ]
  },
  "success": true,
  "message": "Analytics retrieved successfully"
}
```

**Error Response (401 Unauthorized):**
```json
{
  "success": false,
  "message": "Invalid authentication token"
}
```

**Error Response (500 Internal Server Error):**
```json
{
  "success": false,
  "message": "Failed to retrieve analytics"
}
```

#### Response Field Descriptions

**DealerSummaryDto (Root Object)**

| Field | Type | Description |
|-------|------|-------------|
| `totalDealers` | int | Toplam aktif bayi sayısı |
| `totalCodesDistributed` | int | Bayilere dağıtılan toplam kod sayısı |
| `totalCodesUsed` | int | Çiftçiler tarafından kullanılan toplam kod sayısı |
| `totalCodesAvailable` | int | Bayilerde henüz dağıtılmamış kod sayısı |
| `totalCodesReclaimed` | int | Geri alınan toplam kod sayısı |
| `overallUsageRate` | decimal | Genel kullanım oranı (%) - (totalCodesUsed / totalCodesSent * 100) |
| `dealers` | array | Bayi detay listesi (DealerPerformanceDto[]) |

**DealerPerformanceDto (Dealer Object)**

| Field | Type | Description | Nullable |
|-------|------|-------------|----------|
| `dealerId` | int | Bayinin UserId'si | ❌ |
| `dealerName` | string | Bayi adı soyadı | ❌ |
| `dealerEmail` | string | Bayi email adresi | ❌ |
| `totalCodesReceived` | int | Bayiye transfer edilen toplam kod sayısı | ❌ |
| `codesSent` | int | Bayinin çiftçilere gönderdiği kod sayısı | ❌ |
| `codesUsed` | int | Çiftçiler tarafından kullanılan kod sayısı | ❌ |
| `codesAvailable` | int | Bayide henüz dağıtılmamış kod sayısı | ❌ |
| `codesReclaimed` | int | Bu bayiden geri alınan kod sayısı | ❌ |
| `usageRate` | decimal | Kullanım oranı (%) - (codesUsed / codesSent * 100) | ❌ |
| `uniqueFarmersReached` | int | Ulaşılan benzersiz çiftçi sayısı | ✅ |
| `totalAnalyses` | int | Bu bayinin kodlarıyla yapılan toplam analiz sayısı | ✅ |
| `firstTransferDate` | DateTime | İlk kod transfer tarihi | ✅ |
| `lastTransferDate` | DateTime | Son kod transfer tarihi | ✅ |

---

### 2. Rebuild Analytics Cache

**Cache'i veritabanından yeniden oluşturur. Cache eski veya yanlış görünüyorsa kullanın.**

#### Endpoint
```
POST /api/v1/sponsorship/analytics/rebuild-cache
```

#### Query Parameters
Yok. Sponsor ID JWT token'dan otomatik alınır.

#### Request Body
Yok. Body göndermek gerekmez.

#### Request Example

```bash
curl -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/analytics/rebuild-cache" \
  -H "Authorization: Bearer eyJhbGc..." \
  -H "x-dev-arch-version: 1.0"
```

#### JavaScript/TypeScript Example
```typescript
// API Service
async rebuildAnalyticsCache(): Promise<void> {
  const response = await fetch('/api/v1/sponsorship/analytics/rebuild-cache', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'x-dev-arch-version': '1.0'
    }
  });

  if (!response.ok) {
    throw new Error('Failed to rebuild cache');
  }
}
```

#### Response Structure

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Analytics cache rebuilt successfully"
}
```

**Error Response (401 Unauthorized):**
```json
{
  "success": false,
  "message": "Invalid authentication token"
}
```

**Error Response (500 Internal Server Error):**
```json
{
  "success": false,
  "message": "Failed to rebuild analytics cache"
}
```

---

## 💾 TypeScript Type Definitions

```typescript
/**
 * Sponsor-level dealer summary analytics
 */
interface DealerSummaryDto {
  /** Total number of active dealers */
  totalDealers: number;

  /** Total codes distributed to dealers */
  totalCodesDistributed: number;

  /** Total codes used by farmers */
  totalCodesUsed: number;

  /** Total codes available with dealers (not yet sent) */
  totalCodesAvailable: number;

  /** Total codes reclaimed from dealers */
  totalCodesReclaimed: number;

  /** Overall usage rate percentage (codesUsed / codesSent * 100) */
  overallUsageRate: number;

  /** List of dealer performance details */
  dealers: DealerPerformanceDto[];
}

/**
 * Individual dealer performance metrics
 */
interface DealerPerformanceDto {
  /** Dealer's User ID */
  dealerId: number;

  /** Dealer's full name */
  dealerName: string;

  /** Dealer's email address */
  dealerEmail: string;

  /** Total codes received from sponsor */
  totalCodesReceived: number;

  /** Codes sent to farmers */
  codesSent: number;

  /** Codes used/redeemed by farmers */
  codesUsed: number;

  /** Codes available (not yet sent to farmers) */
  codesAvailable: number;

  /** Codes reclaimed by sponsor */
  codesReclaimed: number;

  /** Usage rate percentage (codesUsed / codesSent * 100) */
  usageRate: number;

  /** Number of unique farmers reached (nullable) */
  uniqueFarmersReached?: number;

  /** Total analyses created with this dealer's codes (nullable) */
  totalAnalyses?: number;

  /** First code transfer date (nullable) */
  firstTransferDate?: string; // ISO 8601 format

  /** Last code transfer date (nullable) */
  lastTransferDate?: string; // ISO 8601 format
}

/**
 * Standard API response wrapper
 */
interface ApiResponse<T> {
  data?: T;
  success: boolean;
  message: string;
}
```

---

## 🎨 Frontend Implementation Examples

### React Example

```typescript
// hooks/useAnalytics.ts
import { useState, useEffect } from 'react';

interface AnalyticsHook {
  data: DealerSummaryDto | null;
  loading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
  rebuildCache: () => Promise<void>;
}

export function useAnalytics(dealerId?: number): AnalyticsHook {
  const [data, setData] = useState<DealerSummaryDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchAnalytics = async () => {
    try {
      setLoading(true);
      setError(null);

      const url = dealerId
        ? `/api/v1/sponsorship/analytics/dealer-performance?dealerId=${dealerId}`
        : `/api/v1/sponsorship/analytics/dealer-performance`;

      const response = await fetch(url, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'x-dev-arch-version': '1.0'
        }
      });

      if (!response.ok) {
        throw new Error('Failed to fetch analytics');
      }

      const result: ApiResponse<DealerSummaryDto> = await response.json();
      setData(result.data || null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  const rebuildCache = async () => {
    try {
      setError(null);

      const response = await fetch('/api/v1/sponsorship/analytics/rebuild-cache', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'x-dev-arch-version': '1.0'
        }
      });

      if (!response.ok) {
        throw new Error('Failed to rebuild cache');
      }

      // Refresh data after rebuild
      await fetchAnalytics();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    }
  };

  useEffect(() => {
    fetchAnalytics();
  }, [dealerId]);

  return {
    data,
    loading,
    error,
    refresh: fetchAnalytics,
    rebuildCache
  };
}

// components/DealerAnalytics.tsx
import React from 'react';
import { useAnalytics } from '../hooks/useAnalytics';

export function DealerAnalytics() {
  const { data, loading, error, refresh, rebuildCache } = useAnalytics();

  if (loading) return <div>Yükleniyor...</div>;
  if (error) return <div>Hata: {error}</div>;
  if (!data) return <div>Veri bulunamadı</div>;

  return (
    <div className="analytics-dashboard">
      <div className="actions">
        <button onClick={refresh}>Yenile</button>
        <button onClick={rebuildCache}>Cache'i Yeniden Oluştur</button>
      </div>

      <div className="summary">
        <h2>Özet</h2>
        <div className="stats">
          <div>Toplam Bayi: {data.totalDealers}</div>
          <div>Dağıtılan Kod: {data.totalCodesDistributed}</div>
          <div>Kullanılan Kod: {data.totalCodesUsed}</div>
          <div>Mevcut Kod: {data.totalCodesAvailable}</div>
          <div>Kullanım Oranı: {data.overallUsageRate.toFixed(2)}%</div>
        </div>
      </div>

      <div className="dealers">
        <h2>Bayi Performansları</h2>
        <table>
          <thead>
            <tr>
              <th>Bayi</th>
              <th>Email</th>
              <th>Aldığı Kod</th>
              <th>Gönderdiği</th>
              <th>Kullanılan</th>
              <th>Mevcut</th>
              <th>Kullanım Oranı</th>
            </tr>
          </thead>
          <tbody>
            {data.dealers.map(dealer => (
              <tr key={dealer.dealerId}>
                <td>{dealer.dealerName}</td>
                <td>{dealer.dealerEmail}</td>
                <td>{dealer.totalCodesReceived}</td>
                <td>{dealer.codesSent}</td>
                <td>{dealer.codesUsed}</td>
                <td>{dealer.codesAvailable}</td>
                <td>{dealer.usageRate.toFixed(2)}%</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
```

### Angular Example

```typescript
// services/analytics.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private baseUrl = '/api/v1/sponsorship/analytics';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  private getHeaders(): HttpHeaders {
    return new HttpHeaders({
      'Authorization': `Bearer ${this.authService.getToken()}`,
      'x-dev-arch-version': '1.0'
    });
  }

  getDealerPerformance(dealerId?: number): Observable<DealerSummaryDto> {
    const url = dealerId
      ? `${this.baseUrl}/dealer-performance?dealerId=${dealerId}`
      : `${this.baseUrl}/dealer-performance`;

    return this.http.get<ApiResponse<DealerSummaryDto>>(url, {
      headers: this.getHeaders()
    }).pipe(
      map(response => response.data!)
    );
  }

  rebuildCache(): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/rebuild-cache`,
      null,
      { headers: this.getHeaders() }
    );
  }
}

// components/dealer-analytics/dealer-analytics.component.ts
import { Component, OnInit } from '@angular/core';
import { AnalyticsService } from '../../services/analytics.service';

@Component({
  selector: 'app-dealer-analytics',
  templateUrl: './dealer-analytics.component.html'
})
export class DealerAnalyticsComponent implements OnInit {
  analytics: DealerSummaryDto | null = null;
  loading = true;
  error: string | null = null;

  constructor(private analyticsService: AnalyticsService) {}

  ngOnInit(): void {
    this.loadAnalytics();
  }

  loadAnalytics(): void {
    this.loading = true;
    this.error = null;

    this.analyticsService.getDealerPerformance().subscribe({
      next: (data) => {
        this.analytics = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message;
        this.loading = false;
      }
    });
  }

  rebuildCache(): void {
    this.analyticsService.rebuildCache().subscribe({
      next: () => {
        this.loadAnalytics();
      },
      error: (err) => {
        this.error = err.message;
      }
    });
  }
}
```

### Vue.js Example

```typescript
// composables/useAnalytics.ts
import { ref, onMounted } from 'vue';
import type { Ref } from 'vue';

export function useAnalytics(dealerId?: Ref<number | undefined>) {
  const data = ref<DealerSummaryDto | null>(null);
  const loading = ref(true);
  const error = ref<string | null>(null);

  const fetchAnalytics = async () => {
    try {
      loading.value = true;
      error.value = null;

      const url = dealerId?.value
        ? `/api/v1/sponsorship/analytics/dealer-performance?dealerId=${dealerId.value}`
        : `/api/v1/sponsorship/analytics/dealer-performance`;

      const response = await fetch(url, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'x-dev-arch-version': '1.0'
        }
      });

      if (!response.ok) throw new Error('Failed to fetch analytics');

      const result = await response.json();
      data.value = result.data;
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Unknown error';
    } finally {
      loading.value = false;
    }
  };

  const rebuildCache = async () => {
    try {
      error.value = null;

      const response = await fetch('/api/v1/sponsorship/analytics/rebuild-cache', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'x-dev-arch-version': '1.0'
        }
      });

      if (!response.ok) throw new Error('Failed to rebuild cache');

      await fetchAnalytics();
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Unknown error';
    }
  };

  onMounted(() => {
    fetchAnalytics();
  });

  return {
    data,
    loading,
    error,
    refresh: fetchAnalytics,
    rebuildCache
  };
}

// components/DealerAnalytics.vue
<script setup lang="ts">
import { useAnalytics } from '@/composables/useAnalytics';

const { data, loading, error, refresh, rebuildCache } = useAnalytics();
</script>

<template>
  <div class="analytics-dashboard">
    <div v-if="loading">Yükleniyor...</div>
    <div v-else-if="error">Hata: {{ error }}</div>
    <div v-else-if="data">
      <div class="actions">
        <button @click="refresh">Yenile</button>
        <button @click="rebuildCache">Cache'i Yeniden Oluştur</button>
      </div>

      <div class="summary">
        <h2>Özet</h2>
        <div class="stats">
          <div>Toplam Bayi: {{ data.totalDealers }}</div>
          <div>Dağıtılan Kod: {{ data.totalCodesDistributed }}</div>
          <div>Kullanılan Kod: {{ data.totalCodesUsed }}</div>
          <div>Mevcut Kod: {{ data.totalCodesAvailable }}</div>
          <div>Kullanım Oranı: {{ data.overallUsageRate.toFixed(2) }}%</div>
        </div>
      </div>

      <div class="dealers">
        <h2>Bayi Performansları</h2>
        <table>
          <thead>
            <tr>
              <th>Bayi</th>
              <th>Email</th>
              <th>Aldığı Kod</th>
              <th>Gönderdiği</th>
              <th>Kullanılan</th>
              <th>Mevcut</th>
              <th>Kullanım Oranı</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="dealer in data.dealers" :key="dealer.dealerId">
              <td>{{ dealer.dealerName }}</td>
              <td>{{ dealer.dealerEmail }}</td>
              <td>{{ dealer.totalCodesReceived }}</td>
              <td>{{ dealer.codesSent }}</td>
              <td>{{ dealer.codesUsed }}</td>
              <td>{{ dealer.codesAvailable }}</td>
              <td>{{ dealer.usageRate.toFixed(2) }}%</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
```

---

## 📊 UI Components Önerileri

### 1. Dashboard Summary Cards

```typescript
// Özet kartlar için önerilen layout
<div className="summary-grid">
  <SummaryCard
    title="Toplam Bayi"
    value={data.totalDealers}
    icon="users"
  />
  <SummaryCard
    title="Dağıtılan Kodlar"
    value={data.totalCodesDistributed}
    icon="package"
    trend={calculateTrend(data.totalCodesDistributed, previousValue)}
  />
  <SummaryCard
    title="Kullanılan Kodlar"
    value={data.totalCodesUsed}
    icon="check-circle"
    color="success"
  />
  <SummaryCard
    title="Mevcut Kodlar"
    value={data.totalCodesAvailable}
    icon="inbox"
  />
  <SummaryCard
    title="Kullanım Oranı"
    value={`${data.overallUsageRate.toFixed(2)}%`}
    icon="trending-up"
    color={data.overallUsageRate > 70 ? 'success' : 'warning'}
  />
</div>
```

### 2. Dealer Performance Table

```typescript
// Sıralanabilir ve filtrelenebilir tablo
interface TableColumn {
  key: keyof DealerPerformanceDto;
  label: string;
  sortable: boolean;
  format?: (value: any) => string;
}

const columns: TableColumn[] = [
  { key: 'dealerName', label: 'Bayi Adı', sortable: true },
  { key: 'dealerEmail', label: 'Email', sortable: true },
  { key: 'totalCodesReceived', label: 'Aldığı Kod', sortable: true },
  { key: 'codesSent', label: 'Gönderdiği', sortable: true },
  { key: 'codesUsed', label: 'Kullanılan', sortable: true },
  { key: 'codesAvailable', label: 'Mevcut', sortable: true },
  {
    key: 'usageRate',
    label: 'Kullanım Oranı',
    sortable: true,
    format: (value) => `${value.toFixed(2)}%`
  }
];
```

### 3. Charts

```typescript
// Performans grafikleri için öneriler

// 1. Kullanım oranı bar chart
<BarChart
  data={data.dealers}
  x="dealerName"
  y="usageRate"
  title="Bayi Kullanım Oranları"
  color={(value) => value > 70 ? 'green' : value > 50 ? 'orange' : 'red'}
/>

// 2. Kod dağılımı pie chart
<PieChart
  data={[
    { label: 'Kullanılan', value: data.totalCodesUsed },
    { label: 'Mevcut', value: data.totalCodesAvailable },
    { label: 'Geri Alınan', value: data.totalCodesReclaimed }
  ]}
  title="Kod Dağılımı"
/>

// 3. Dealer karşılaştırma radar chart
<RadarChart
  data={data.dealers.map(d => ({
    dealer: d.dealerName,
    metrics: {
      'Kod Sayısı': d.totalCodesReceived,
      'Kullanım Oranı': d.usageRate,
      'Ulaşılan Çiftçi': d.uniqueFarmersReached || 0,
      'Analiz Sayısı': d.totalAnalyses || 0
    }
  }))}
/>
```

---

## 🔄 Real-Time Updates

Cache otomatik olarak güncellenir, ancak frontend'de periyodik yenileme yapabilirsiniz:

```typescript
// Auto-refresh every 5 minutes
useEffect(() => {
  const interval = setInterval(() => {
    fetchAnalytics();
  }, 5 * 60 * 1000); // 5 minutes

  return () => clearInterval(interval);
}, []);
```

---

## ⚠️ Error Handling

```typescript
// Kapsamlı error handling örneği
async function fetchAnalyticsWithRetry(retries = 3): Promise<DealerSummaryDto> {
  for (let i = 0; i < retries; i++) {
    try {
      const response = await fetch('/api/v1/sponsorship/analytics/dealer-performance', {
        headers: {
          'Authorization': `Bearer ${getToken()}`,
          'x-dev-arch-version': '1.0'
        }
      });

      if (response.status === 401) {
        // Token expired, redirect to login
        window.location.href = '/login';
        throw new Error('Authentication required');
      }

      if (response.status === 403) {
        throw new Error('Sponsor role required');
      }

      if (response.status === 500) {
        // Retry on server error
        if (i < retries - 1) {
          await new Promise(resolve => setTimeout(resolve, 1000 * (i + 1)));
          continue;
        }
        throw new Error('Server error, please try again later');
      }

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const result = await response.json();
      return result.data;
    } catch (error) {
      if (i === retries - 1) throw error;
    }
  }

  throw new Error('Failed after retries');
}
```

---

## 🎯 Performance Tips

### 1. Caching on Frontend
```typescript
// LocalStorage cache for offline support
const CACHE_KEY = 'dealer_analytics_cache';
const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes

function getCachedAnalytics(): DealerSummaryDto | null {
  const cached = localStorage.getItem(CACHE_KEY);
  if (!cached) return null;

  const { data, timestamp } = JSON.parse(cached);
  if (Date.now() - timestamp > CACHE_DURATION) {
    localStorage.removeItem(CACHE_KEY);
    return null;
  }

  return data;
}

function setCachedAnalytics(data: DealerSummaryDto): void {
  localStorage.setItem(CACHE_KEY, JSON.stringify({
    data,
    timestamp: Date.now()
  }));
}
```

### 2. Lazy Loading
```typescript
// Component'i lazy load edin
const DealerAnalytics = lazy(() => import('./components/DealerAnalytics'));

// Route'da kullanım
<Route
  path="/analytics"
  element={
    <Suspense fallback={<LoadingSpinner />}>
      <DealerAnalytics />
    </Suspense>
  }
/>
```

### 3. Debouncing Search/Filter
```typescript
// Arama için debounce
const debouncedSearch = useMemo(
  () => debounce((searchTerm: string) => {
    setFilteredDealers(
      dealers.filter(d =>
        d.dealerName.toLowerCase().includes(searchTerm.toLowerCase())
      )
    );
  }, 300),
  [dealers]
);
```

---

## 🧪 Testing

### Unit Test Example (Jest)
```typescript
import { render, screen, waitFor } from '@testing-library/react';
import { DealerAnalytics } from './DealerAnalytics';

describe('DealerAnalytics', () => {
  it('should display analytics data', async () => {
    const mockData: DealerSummaryDto = {
      totalDealers: 3,
      totalCodesDistributed: 150,
      totalCodesUsed: 95,
      totalCodesAvailable: 45,
      totalCodesReclaimed: 10,
      overallUsageRate: 63.33,
      dealers: [
        {
          dealerId: 158,
          dealerName: 'Test Dealer',
          dealerEmail: 'test@example.com',
          totalCodesReceived: 50,
          codesSent: 48,
          codesUsed: 40,
          codesAvailable: 2,
          codesReclaimed: 0,
          usageRate: 83.33
        }
      ]
    };

    jest.spyOn(global, 'fetch').mockResolvedValue({
      ok: true,
      json: async () => ({ data: mockData, success: true })
    } as Response);

    render(<DealerAnalytics />);

    await waitFor(() => {
      expect(screen.getByText('3')).toBeInTheDocument();
      expect(screen.getByText('Test Dealer')).toBeInTheDocument();
    });
  });
});
```

---

## 📝 Notes & Best Practices

### Do's ✅
- Cache'den gelen veriyi olduğu gibi kullanın (5-15ms yanıt süresi)
- Loading state'i gösterin (ilk request rebuild yapabilir, ~200ms)
- Error handling yapın (401, 403, 500)
- Auto-refresh için 5+ dakika interval kullanın
- Nullable fieldları kontrol edin (`uniqueFarmersReached`, `totalAnalyses`, vb.)
- Decimal değerleri `.toFixed(2)` ile formatlayın
- Tarihleri kullanıcı timezone'una göre gösterin

### Don'ts ❌
- Her action'dan sonra rebuild çağırmayın (otomatik güncellenir)
- 1 dakikadan kısa interval'lerle polling yapmayın
- Cache rebuild'i sık kullanmayın (sadece sorun varsa)
- Response'u cache'lemeden önce doğrulamayı unutmayın
- Hata mesajlarını kullanıcıya raw göstermeyin

### Performance Checklist
- [ ] Loading spinner/skeleton screen var mı?
- [ ] Error boundary implement edildi mi?
- [ ] Retry logic var mı?
- [ ] Frontend cache implement edildi mi?
- [ ] Table pagination/virtualization var mı? (>50 dealer için)
- [ ] Chart'lar lazy load ediliyor mu?
- [ ] Mobile responsive mı?
- [ ] Accessibility (ARIA labels) eklendi mi?

---

## 🚀 Deployment Checklist

### Before Going Live
- [ ] API endpoints test edildi (Postman/Swagger)
- [ ] Authorization doğru çalışıyor (Sponsor role)
- [ ] Error handling kapsamlı
- [ ] Loading states eklendi
- [ ] Mobile responsive
- [ ] Cross-browser test yapıldı
- [ ] Performance test yapıldı (Network throttling)
- [ ] Analytics tracking eklendi (optional)

### Production Monitoring
- [ ] API response time monitoring (<50ms expected)
- [ ] Error rate monitoring
- [ ] Cache hit rate monitoring
- [ ] User engagement tracking

---

## 📞 Support & Contact

Sorularınız için:
- **API Issues**: Backend team
- **Frontend Integration**: Frontend team lead
- **Documentation**: Bu dokümanı güncel tutun

**Son Güncelleme**: 2025-01-04
**API Version**: 1.0
**Document Version**: 1.0
