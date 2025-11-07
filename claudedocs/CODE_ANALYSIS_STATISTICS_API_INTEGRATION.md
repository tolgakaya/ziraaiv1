# Code Analysis Statistics API - Frontend Integration Guide

## Overview
The Code Analysis Statistics endpoint provides sponsors with detailed analytics about their distributed sponsorship codes and the plant analyses performed by farmers using those codes. This guide covers the updated paginated version of the API.

---

## Endpoint Information

### Base Endpoint
```
GET /api/Sponsorship/code-analysis-statistics
```

### Authentication
- **Required:** Yes
- **Authorization:** Bearer Token (JWT)
- **Roles:** Sponsor, Admin
- **Header:** `Authorization: Bearer {token}`

---

## Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | integer | No | 1 | Current page number (minimum: 1) |
| `pageSize` | integer | No | 50 | Number of codes per page (minimum: 1, maximum: 100) |
| `includeAnalysisDetails` | boolean | No | true | Include full analysis list for each code |
| `topCodesCount` | integer | No | 10 | Number of top performing codes to show |
| `startDate` | datetime | No | null | Filter codes redeemed after this date (ISO 8601 format) |
| `endDate` | datetime | No | null | Filter codes redeemed before this date (ISO 8601 format) |

### Parameter Notes:
- **pageSize** is automatically capped at 100 to prevent performance issues
- **startDate/endDate** filter by code redemption date (`UsedDate`), not analysis date
- **includeAnalysisDetails=false** significantly improves performance (recommended for summary views)

---

## Request Examples

### Basic Request (First Page)
```http
GET /api/Sponsorship/code-analysis-statistics?page=1&pageSize=50
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
x-dev-arch-version: 1.0
```

### Summary View (No Analysis Details)
```http
GET /api/Sponsorship/code-analysis-statistics?includeAnalysisDetails=false&pageSize=20
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
x-dev-arch-version: 1.0
```

### Date Range Filter
```http
GET /api/Sponsorship/code-analysis-statistics?startDate=2025-01-01&endDate=2025-03-31&page=1&pageSize=50
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
x-dev-arch-version: 1.0
```

### Next Page with Large Page Size
```http
GET /api/Sponsorship/code-analysis-statistics?page=2&pageSize=100
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
x-dev-arch-version: 1.0
```

---

## Response Structure

### Success Response (200 OK)

```json
{
  "data": {
    "totalRedeemedCodes": 245,
    "totalAnalysesPerformed": 1823,
    "averageAnalysesPerCode": 7.44,
    "totalActiveFarmers": 187,
    "page": 1,
    "pageSize": 50,
    "totalPages": 5,
    "codeBreakdowns": [
      {
        "code": "AGRI-2025-ABC123",
        "tierName": "XL",
        "farmerId": 1234,
        "farmerName": "Ahmet Yılmaz",
        "farmerEmail": "ahmet@example.com",
        "farmerPhone": "+905551234567",
        "location": "Ankara, Çankaya",
        "redeemedDate": "2025-01-15T10:30:00Z",
        "subscriptionStatus": "Active",
        "subscriptionEndDate": "2025-07-15T10:30:00Z",
        "totalAnalyses": 23,
        "lastAnalysisDate": "2025-03-20T14:22:00Z",
        "daysSinceLastAnalysis": 5,
        "analyses": [
          {
            "analysisId": 5678,
            "analysisDate": "2025-03-20T14:22:00Z",
            "cropType": "Tomato",
            "disease": "Late Blight",
            "diseaseCategory": "Fungal",
            "severity": "Moderate",
            "location": "Ankara, Çankaya",
            "status": "Completed",
            "sponsorLogoDisplayed": true,
            "analysisDetailsUrl": "https://ziraai.com/api/v1/sponsorship/analysis/5678"
          }
        ]
      }
    ],
    "topPerformingCodes": [
      {
        "code": "AGRI-2025-TOP001",
        "totalAnalyses": 45,
        "farmerName": "Mehmet Demir",
        "tierName": "L"
      }
    ],
    "cropTypeDistribution": [
      {
        "cropType": "Tomato",
        "analysisCount": 456,
        "percentage": 25.02,
        "uniqueFarmers": 89
      },
      {
        "cropType": "Pepper",
        "analysisCount": 342,
        "percentage": 18.76,
        "uniqueFarmers": 67
      }
    ],
    "diseaseDistribution": [
      {
        "disease": "Late Blight",
        "category": "Fungal",
        "occurrenceCount": 234,
        "percentage": 12.84,
        "affectedCrops": ["Tomato", "Potato"],
        "geographicDistribution": ["Ankara", "İzmir", "Antalya"]
      }
    ]
  },
  "success": true,
  "message": "Code analysis statistics retrieved successfully"
}
```

### Response Fields Explained

#### Root Level Statistics
| Field | Type | Description |
|-------|------|-------------|
| `totalRedeemedCodes` | integer | **Total** matching codes across all pages (not just current page) |
| `totalAnalysesPerformed` | integer | Sum of analyses from codes in current page |
| `averageAnalysesPerCode` | decimal | Average analyses per code (based on total codes) |
| `totalActiveFarmers` | integer | Count of farmers with active subscriptions in current page |
| `page` | integer | Current page number |
| `pageSize` | integer | Codes per page |
| `totalPages` | integer | Total number of pages available |

#### Code Breakdown Object
| Field | Type | Visibility Rule | Description |
|-------|------|----------------|-------------|
| `code` | string | All tiers | Sponsorship code |
| `tierName` | string | All tiers | Subscription tier (S, M, L, XL) |
| `farmerId` | integer | All tiers | Farmer user ID |
| `farmerName` | string | **Tier-based** | Farmer full name or "Anonymous" |
| `farmerEmail` | string | **L, XL only** | Farmer email address |
| `farmerPhone` | string | **L, XL only** | Farmer phone number |
| `location` | string | **Tier-based** | Full address (L, XL), City only (M), "Limited" (S) |
| `redeemedDate` | datetime | All tiers | When farmer redeemed the code |
| `subscriptionStatus` | string | All tiers | "Active" or "Expired" |
| `subscriptionEndDate` | datetime | All tiers | Subscription expiration date |
| `totalAnalyses` | integer | All tiers | Total plant analyses performed |
| `lastAnalysisDate` | datetime | All tiers | Most recent analysis date |
| `daysSinceLastAnalysis` | integer | All tiers | Days since last analysis (null if no analyses) |
| `analyses` | array | All tiers | List of analysis summaries (if `includeAnalysisDetails=true`) |

#### Data Visibility by Tier

| Data Point | S (30%) | M (60%) | L (100%) | XL (100%) |
|------------|---------|---------|----------|-----------|
| Farmer Name | Anonymous | Anonymous | Full Name | Full Name |
| Farmer Email | ❌ | ❌ | ✅ | ✅ |
| Farmer Phone | ❌ | ❌ | ✅ | ✅ |
| Location | "Limited" | City only | Full address | Full address |
| Analysis Count | ✅ | ✅ | ✅ | ✅ |
| Crop Types | ✅ | ✅ | ✅ | ✅ |
| Disease Info | ✅ | ✅ | ✅ | ✅ |

---

## Frontend Implementation Guide

### 1. TypeScript Interfaces

```typescript
// Request parameters interface
interface CodeAnalysisStatisticsParams {
  page?: number;
  pageSize?: number;
  includeAnalysisDetails?: boolean;
  topCodesCount?: number;
  startDate?: string; // ISO 8601 format
  endDate?: string;   // ISO 8601 format
}

// Response interfaces
interface CodeAnalysisStatisticsResponse {
  data: {
    totalRedeemedCodes: number;
    totalAnalysesPerformed: number;
    averageAnalysesPerCode: number;
    totalActiveFarmers: number;
    page: number;
    pageSize: number;
    totalPages: number;
    codeBreakdowns: CodeAnalysisBreakdown[];
    topPerformingCodes: CodeAnalysisBreakdown[];
    cropTypeDistribution: CropTypeStatistic[];
    diseaseDistribution: DiseaseStatistic[];
  };
  success: boolean;
  message: string;
}

interface CodeAnalysisBreakdown {
  code: string;
  tierName: string;
  farmerId: number;
  farmerName: string;
  farmerEmail: string;
  farmerPhone: string;
  location: string;
  redeemedDate: string;
  subscriptionStatus: string;
  subscriptionEndDate: string | null;
  totalAnalyses: number;
  lastAnalysisDate: string | null;
  daysSinceLastAnalysis: number | null;
  analyses: SponsoredAnalysisSummary[];
}

interface SponsoredAnalysisSummary {
  analysisId: number;
  analysisDate: string;
  cropType: string;
  disease: string;
  diseaseCategory: string;
  severity: string;
  location: string;
  status: string;
  sponsorLogoDisplayed: boolean;
  analysisDetailsUrl: string;
}

interface CropTypeStatistic {
  cropType: string;
  analysisCount: number;
  percentage: number;
  uniqueFarmers: number;
}

interface DiseaseStatistic {
  disease: string;
  category: string;
  occurrenceCount: number;
  percentage: number;
  affectedCrops: string[];
  geographicDistribution: string[];
}
```

### 2. API Service (React/Angular/Vue)

```typescript
import axios from 'axios';

class SponsorshipAnalyticsService {
  private baseUrl = 'https://ziraai.com/api/Sponsorship';

  async getCodeAnalysisStatistics(
    params: CodeAnalysisStatisticsParams = {}
  ): Promise<CodeAnalysisStatisticsResponse> {
    const {
      page = 1,
      pageSize = 50,
      includeAnalysisDetails = true,
      topCodesCount = 10,
      startDate,
      endDate
    } = params;

    try {
      const response = await axios.get<CodeAnalysisStatisticsResponse>(
        `${this.baseUrl}/code-analysis-statistics`,
        {
          params: {
            page,
            pageSize,
            includeAnalysisDetails,
            topCodesCount,
            startDate,
            endDate
          },
          headers: {
            'Authorization': `Bearer ${this.getAuthToken()}`,
            'x-dev-arch-version': '1.0'
          }
        }
      );

      return response.data;
    } catch (error) {
      console.error('Failed to fetch code analysis statistics:', error);
      throw error;
    }
  }

  private getAuthToken(): string {
    // Retrieve token from storage (localStorage, sessionStorage, etc.)
    return localStorage.getItem('authToken') || '';
  }
}

export const analyticsService = new SponsorshipAnalyticsService();
```

### 3. React Hook Example

```typescript
import { useState, useEffect } from 'react';
import { analyticsService } from './services/analyticsService';

export function useCodeAnalysisStatistics(
  params: CodeAnalysisStatisticsParams = {}
) {
  const [data, setData] = useState<CodeAnalysisStatisticsResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  const fetchData = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await analyticsService.getCodeAnalysisStatistics(params);
      setData(response);
    } catch (err) {
      setError(err as Error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [JSON.stringify(params)]); // Re-fetch when params change

  return { data, loading, error, refetch: fetchData };
}

// Usage in component
function CodeAnalyticsPage() {
  const [page, setPage] = useState(1);
  const [dateRange, setDateRange] = useState<{start?: string, end?: string}>({});

  const { data, loading, error } = useCodeAnalysisStatistics({
    page,
    pageSize: 50,
    includeAnalysisDetails: false, // Optimize for summary view
    startDate: dateRange.start,
    endDate: dateRange.end
  });

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error.message}</div>;
  if (!data) return null;

  return (
    <div>
      <h1>Code Analysis Statistics</h1>

      {/* Summary Cards */}
      <div className="summary-cards">
        <StatCard
          title="Total Codes Redeemed"
          value={data.data.totalRedeemedCodes}
        />
        <StatCard
          title="Total Analyses"
          value={data.data.totalAnalysesPerformed}
        />
        <StatCard
          title="Average per Code"
          value={data.data.averageAnalysesPerCode.toFixed(2)}
        />
        <StatCard
          title="Active Farmers"
          value={data.data.totalActiveFarmers}
        />
      </div>

      {/* Code Breakdown Table */}
      <CodeBreakdownTable codes={data.data.codeBreakdowns} />

      {/* Pagination */}
      <Pagination
        currentPage={data.data.page}
        totalPages={data.data.totalPages}
        onPageChange={setPage}
      />
    </div>
  );
}
```

### 4. Pagination Component Example

```typescript
interface PaginationProps {
  currentPage: number;
  totalPages: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
}

function Pagination({
  currentPage,
  totalPages,
  pageSize,
  onPageChange,
  onPageSizeChange
}: PaginationProps) {
  const pageSizeOptions = [20, 50, 100];

  return (
    <div className="pagination">
      {/* Page Size Selector */}
      <div className="page-size-selector">
        <label>Items per page:</label>
        <select
          value={pageSize}
          onChange={(e) => onPageSizeChange?.(Number(e.target.value))}
        >
          {pageSizeOptions.map(size => (
            <option key={size} value={size}>{size}</option>
          ))}
        </select>
      </div>

      {/* Page Navigation */}
      <div className="page-navigation">
        <button
          disabled={currentPage === 1}
          onClick={() => onPageChange(currentPage - 1)}
        >
          Previous
        </button>

        <span>Page {currentPage} of {totalPages}</span>

        <button
          disabled={currentPage === totalPages}
          onClick={() => onPageChange(currentPage + 1)}
        >
          Next
        </button>
      </div>

      {/* Page Numbers */}
      <div className="page-numbers">
        {Array.from({ length: totalPages }, (_, i) => i + 1).map(pageNum => (
          <button
            key={pageNum}
            className={pageNum === currentPage ? 'active' : ''}
            onClick={() => onPageChange(pageNum)}
          >
            {pageNum}
          </button>
        ))}
      </div>
    </div>
  );
}
```

### 5. Date Range Filter Component

```typescript
interface DateRangeFilterProps {
  onDateRangeChange: (start?: string, end?: string) => void;
}

function DateRangeFilter({ onDateRangeChange }: DateRangeFilterProps) {
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

  const handleApplyFilter = () => {
    onDateRangeChange(
      startDate || undefined,
      endDate || undefined
    );
  };

  const handleClearFilter = () => {
    setStartDate('');
    setEndDate('');
    onDateRangeChange(undefined, undefined);
  };

  return (
    <div className="date-range-filter">
      <div className="date-inputs">
        <label>
          Start Date:
          <input
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
          />
        </label>

        <label>
          End Date:
          <input
            type="date"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
          />
        </label>
      </div>

      <div className="filter-actions">
        <button onClick={handleApplyFilter}>Apply Filter</button>
        <button onClick={handleClearFilter}>Clear</button>
      </div>
    </div>
  );
}
```

---

## Performance Best Practices

### 1. Initial Load
**Use summary view for dashboard:**
```typescript
// Fast loading for dashboard summary
const { data } = useCodeAnalysisStatistics({
  page: 1,
  pageSize: 20,
  includeAnalysisDetails: false // Much faster!
});
```

### 2. Detail View
**Load full details on-demand:**
```typescript
// When user clicks "View Details" on a code
const { data } = useCodeAnalysisStatistics({
  page: selectedPage,
  pageSize: 50,
  includeAnalysisDetails: true // Full details
});
```

### 3. Caching Strategy
```typescript
// Cache results for 5 minutes (matches server cache)
const CACHE_DURATION = 5 * 60 * 1000;

const cachedFetch = async (params: CodeAnalysisStatisticsParams) => {
  const cacheKey = JSON.stringify(params);
  const cached = cache.get(cacheKey);

  if (cached && Date.now() - cached.timestamp < CACHE_DURATION) {
    return cached.data;
  }

  const data = await analyticsService.getCodeAnalysisStatistics(params);
  cache.set(cacheKey, { data, timestamp: Date.now() });

  return data;
};
```

### 4. Optimistic Page Size
```typescript
// Start with smaller page size for faster initial load
const [pageSize, setPageSize] = useState(20);

// User can increase if needed
<select onChange={(e) => setPageSize(Number(e.target.value))}>
  <option value="20">20 (Fastest)</option>
  <option value="50">50 (Balanced)</option>
  <option value="100">100 (Maximum)</option>
</select>
```

---

## Error Handling

### Common Error Scenarios

#### 1. Unauthorized (401)
```typescript
{
  "success": false,
  "message": "Unauthorized access"
}
```
**Action:** Redirect to login, refresh token

#### 2. Invalid Parameters (400)
```typescript
{
  "success": false,
  "message": "Invalid page size. Maximum allowed is 100."
}
```
**Action:** Validate input before sending

#### 3. Server Error (500)
```typescript
{
  "success": false,
  "message": "Error retrieving code analysis statistics"
}
```
**Action:** Show error message, retry with exponential backoff

### Error Handling Example
```typescript
async function fetchWithRetry(
  params: CodeAnalysisStatisticsParams,
  maxRetries = 3
) {
  for (let i = 0; i < maxRetries; i++) {
    try {
      return await analyticsService.getCodeAnalysisStatistics(params);
    } catch (error) {
      if (error.response?.status === 401) {
        // Unauthorized - redirect to login
        window.location.href = '/login';
        throw error;
      }

      if (error.response?.status === 400) {
        // Bad request - don't retry
        throw error;
      }

      if (i === maxRetries - 1) {
        // Last retry failed
        throw error;
      }

      // Wait before retry (exponential backoff)
      await new Promise(resolve =>
        setTimeout(resolve, Math.pow(2, i) * 1000)
      );
    }
  }
}
```

---

## UI/UX Recommendations

### 1. Loading States
```typescript
// Skeleton loading for better UX
{loading && (
  <div className="skeleton-loader">
    <SkeletonCard count={4} />
    <SkeletonTable rows={10} />
  </div>
)}
```

### 2. Empty States
```typescript
// No data available
{data?.data.totalRedeemedCodes === 0 && (
  <EmptyState
    icon="📊"
    title="No Codes Redeemed Yet"
    message="Start distributing codes to see analytics here"
    action={<Button>Distribute Codes</Button>}
  />
)}
```

### 3. Tier Badge Display
```typescript
// Visual tier indicators
function TierBadge({ tier }: { tier: string }) {
  const colors = {
    'S': 'bg-gray-200',
    'M': 'bg-blue-200',
    'L': 'bg-purple-200',
    'XL': 'bg-gold-200'
  };

  return (
    <span className={`tier-badge ${colors[tier]}`}>
      {tier}
    </span>
  );
}
```

### 4. Data Privacy Indicators
```typescript
// Show when data is limited by tier
{code.farmerName === "Anonymous" && (
  <Tooltip content="Full farmer details available with L or XL tier">
    <Icon name="lock" />
  </Tooltip>
)}
```

---

## Mobile Considerations

### Responsive Table
```typescript
// Mobile-optimized code breakdown display
<div className="code-cards">
  {data.data.codeBreakdowns.map(code => (
    <div key={code.code} className="code-card">
      <div className="card-header">
        <span className="code">{code.code}</span>
        <TierBadge tier={code.tierName} />
      </div>
      <div className="card-body">
        <div className="stat">
          <label>Farmer:</label>
          <span>{code.farmerName}</span>
        </div>
        <div className="stat">
          <label>Total Analyses:</label>
          <span>{code.totalAnalyses}</span>
        </div>
        <div className="stat">
          <label>Status:</label>
          <StatusBadge status={code.subscriptionStatus} />
        </div>
      </div>
    </div>
  ))}
</div>
```

### Touch-Friendly Pagination
```typescript
// Larger touch targets for mobile
<div className="mobile-pagination">
  <button className="btn-large" onClick={prevPage}>
    ← Previous
  </button>
  <span className="page-info">
    {currentPage} / {totalPages}
  </span>
  <button className="btn-large" onClick={nextPage}>
    Next →
  </button>
</div>
```

---

## Testing Checklist

- [ ] Test with different page sizes (20, 50, 100)
- [ ] Test pagination navigation (first, last, prev, next)
- [ ] Test date range filtering
- [ ] Test with empty results
- [ ] Test with `includeAnalysisDetails=true` and `false`
- [ ] Test loading states
- [ ] Test error handling (401, 400, 500)
- [ ] Test on mobile devices
- [ ] Test with slow network conditions
- [ ] Verify tier-based data visibility rules
- [ ] Test cache invalidation

---

## Support & Resources

### Related Documentation
- [Sponsor Analytics Complete Guide](./SPONSOR_ANALYTICS_API_DOCUMENTATION.md)
- [Authentication Flow](./AUTHENTICATION.md)
- [Tier-Based Features](./SPONSORSHIP_TIERS.md)

### API Versioning
- Current version: `1.0`
- Header: `x-dev-arch-version: 1.0`

### Contact
For API support or questions, contact the development team.

---

**Last Updated:** 2025-03-26
**API Version:** 1.0
**Document Version:** 1.0
