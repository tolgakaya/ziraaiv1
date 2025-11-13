# Competitive Benchmarking Analytics - API Documentation

**Feature**: Sponsor Advanced Analytics
**Endpoint**: `/api/v1/sponsorship/competitive-benchmarking`
**Date**: 2025-11-12
**Version**: 1.0

---

## Overview

The Competitive Benchmarking Analytics endpoint provides sponsors with anonymous, privacy-preserving performance comparisons against industry averages. This enables data-driven decision making and competitive positioning insights.

### Key Features

- **Industry Benchmarks**: Compare performance against anonymized aggregate metrics
- **Percentile Rankings**: See where sponsor ranks (0-100 scale, higher = better)
- **Gap Analysis**: Identify performance gaps vs industry average and top performers
- **Actionable Recommendations**: Get specific suggestions for improvement
- **Privacy Protection**: Requires minimum 3 sponsors for anonymization
- **Flexible Time Periods**: Analyze 30, 60, 90, or custom day windows

---

## Endpoint Details

### HTTP Request

```
GET /api/v1/sponsorship/competitive-benchmarking?timePeriodDays={days}
```

### Authorization

**Roles**: `Sponsor`, `Admin`
**SecuredOperation**: `GetCompetitiveBenchmarkingQuery`
**Authentication**: Bearer token (JWT)

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `timePeriodDays` | integer | No | 90 | Number of days for analysis window (e.g., 30, 60, 90, 180) |

### Headers

```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

---

## Request Examples

### Example 1: Sponsor - Default 90-Day Period

```bash
GET /api/v1/sponsorship/competitive-benchmarking
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Example 2: Sponsor - Custom 30-Day Period

```bash
GET /api/v1/sponsorship/competitive-benchmarking?timePeriodDays=30
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Example 3: Admin - Industry-Wide View

```bash
GET /api/v1/sponsorship/competitive-benchmarking?timePeriodDays=90
Authorization: Bearer {admin_token}
```

---

## Response Structure

### Success Response (200 OK)

```json
{
  "data": {
    "sponsorId": 159,
    "totalSponsorsInBenchmark": 12,
    "timePeriod": "Last 90 days",
    "generatedAt": "2025-11-12T14:30:00Z",

    "yourPerformance": {
      "totalFarmers": 127,
      "totalAnalyses": 483,
      "totalMessagesSent": 89,
      "avgFarmersPerMonth": 42.3,
      "avgAnalysesPerFarmer": 3.8,
      "messageResponseRate": 68.5,
      "farmerRetentionRate": 72.4,
      "avgEngagementScore": 67.2
    },

    "industryBenchmarks": {
      "industryAvgFarmers": 85.3,
      "industryAvgAnalyses": 3.2,
      "industryAvgResponseRate": 62.8,
      "industryAvgRetentionRate": 68.5,
      "industryAvgEngagementScore": 64.1,
      "topPerformerFarmers": 150.0,
      "topPerformerAnalyses": 5.5,
      "topPerformerResponseRate": 85.0,
      "topPerformerRetentionRate": 88.0,
      "topPerformerEngagementScore": 82.0
    },

    "ranking": {
      "overallPercentile": 68,
      "farmersPercentile": 75,
      "analysesPercentile": 72,
      "responseRatePercentile": 65,
      "retentionRatePercentile": 58,
      "engagementScorePercentile": 64,
      "rankingDescription": "Top 32% - Above Average Performer"
    },

    "gaps": [
      {
        "metricName": "Farmer Count",
        "yourValue": 127.0,
        "industryAvg": 85.3,
        "topPerformer": 150.0,
        "gapVsIndustry": "+48.9%",
        "gapVsTopPerformer": "-15.3%",
        "status": "Above Average",
        "recommendation": "Strong farmer acquisition. Consider strategies to reach top performer level (150 farmers)."
      },
      {
        "metricName": "Analyses Per Farmer",
        "yourValue": 3.8,
        "industryAvg": 3.2,
        "topPerformer": 5.5,
        "gapVsIndustry": "+18.8%",
        "gapVsTopPerformer": "-30.9%",
        "status": "Above Average",
        "recommendation": "Good analysis frequency. Encourage more frequent check-ins to reach top performer level."
      },
      {
        "metricName": "Message Response Rate",
        "yourValue": 68.5,
        "industryAvg": 62.8,
        "topPerformer": 85.0,
        "gapVsIndustry": "+9.1%",
        "gapVsTopPerformer": "-19.4%",
        "status": "Above Average",
        "recommendation": "Response rate above average. Improve message timing and content relevance."
      },
      {
        "metricName": "Farmer Retention Rate",
        "yourValue": 72.4,
        "industryAvg": 68.5,
        "topPerformer": 88.0,
        "gapVsIndustry": "+5.7%",
        "gapVsTopPerformer": "-17.7%",
        "status": "Above Average",
        "recommendation": "Retention slightly above average. Focus on at-risk farmer engagement."
      },
      {
        "metricName": "Overall Engagement Score",
        "yourValue": 67.2,
        "industryAvg": 64.1,
        "topPerformer": 82.0,
        "gapVsIndustry": "+4.8%",
        "gapVsTopPerformer": "-18.0%",
        "status": "Above Average",
        "recommendation": "Solid engagement. Increase activity frequency and response quality."
      }
    ]
  },
  "success": true,
  "message": "Competitive benchmarking computed successfully"
}
```

### Admin Response (Industry-Wide)

When accessed by Admin role, response shows only industry benchmarks (no sponsor-specific data):

```json
{
  "data": {
    "sponsorId": null,
    "totalSponsorsInBenchmark": 12,
    "timePeriod": "Last 90 days",
    "generatedAt": "2025-11-12T14:30:00Z",

    "yourPerformance": null,
    "ranking": null,
    "gaps": [],

    "industryBenchmarks": {
      "industryAvgFarmers": 85.3,
      "industryAvgAnalyses": 3.2,
      "industryAvgResponseRate": 62.8,
      "industryAvgRetentionRate": 68.5,
      "industryAvgEngagementScore": 64.1,
      "topPerformerFarmers": 150.0,
      "topPerformerAnalyses": 5.5,
      "topPerformerResponseRate": 85.0,
      "topPerformerRetentionRate": 88.0,
      "topPerformerEngagementScore": 82.0
    }
  },
  "success": true,
  "message": "Industry benchmarks computed successfully"
}
```

---

## Error Responses

### 400 Bad Request - Insufficient Sponsors

**Scenario**: Less than 3 sponsors in system (anonymization requirement)

```json
{
  "data": null,
  "success": false,
  "message": "Insufficient sponsors for benchmarking. Minimum 3 sponsors required for anonymization (current: 2)"
}
```

### 401 Unauthorized - Missing/Invalid Token

```json
{
  "success": false,
  "message": "User ID not found in claims"
}
```

### 403 Forbidden - Missing Required Claim

**Scenario**: User lacks `GetCompetitiveBenchmarkingQuery` operation claim

```json
{
  "success": false,
  "message": "You are not authorized!"
}
```

### 500 Internal Server Error

```json
{
  "data": null,
  "success": false,
  "message": "Competitive benchmarking retrieval failed: {error_details}"
}
```

---

## Response Field Descriptions

### Root Level

| Field | Type | Description |
|-------|------|-------------|
| `sponsorId` | integer? | Current sponsor's ID (null for admin) |
| `totalSponsorsInBenchmark` | integer | Number of sponsors included in benchmark calculation |
| `timePeriod` | string | Human-readable time period description |
| `generatedAt` | datetime | ISO 8601 timestamp of when data was generated |

### Your Performance (Sponsor Only)

| Field | Type | Description |
|-------|------|-------------|
| `totalFarmers` | integer | Total unique farmers sponsored |
| `totalAnalyses` | integer | Total plant analyses by sponsored farmers |
| `totalMessagesSent` | integer | Total messages sent to farmers |
| `avgFarmersPerMonth` | decimal | Average farmers acquired per month |
| `avgAnalysesPerFarmer` | decimal | Average analyses per farmer |
| `messageResponseRate` | decimal | Percentage of messages with farmer response (0-100) |
| `farmerRetentionRate` | decimal | Percentage of farmers retained from first to second half of period (0-100) |
| `avgEngagementScore` | decimal | Weighted engagement score (0-100): Activity 40% + Response 30% + Retention 30% |

### Industry Benchmarks

| Field | Type | Description |
|-------|------|-------------|
| `industryAvgFarmers` | decimal | Average farmer count across all sponsors |
| `industryAvgAnalyses` | decimal | Average analyses per farmer (industry) |
| `industryAvgResponseRate` | decimal | Average message response rate (industry) |
| `industryAvgRetentionRate` | decimal | Average farmer retention rate (industry) |
| `industryAvgEngagementScore` | decimal | Average engagement score (industry) |
| `topPerformerFarmers` | decimal | 90th percentile farmer count (top 10%) |
| `topPerformerAnalyses` | decimal | 90th percentile analyses per farmer |
| `topPerformerResponseRate` | decimal | 90th percentile response rate |
| `topPerformerRetentionRate` | decimal | 90th percentile retention rate |
| `topPerformerEngagementScore` | decimal | 90th percentile engagement score |

### Ranking (Sponsor Only)

| Field | Type | Description |
|-------|------|-------------|
| `overallPercentile` | integer | Overall percentile ranking (0-100, higher = better) |
| `farmersPercentile` | integer | Percentile for farmer count metric |
| `analysesPercentile` | integer | Percentile for analyses per farmer metric |
| `responseRatePercentile` | integer | Percentile for message response rate |
| `retentionRatePercentile` | integer | Percentile for retention rate |
| `engagementScorePercentile` | integer | Percentile for engagement score |
| `rankingDescription` | string | Human-readable ranking (e.g., "Top 10%", "Top 25%", "Above Average", "Average", "Below Average") |

### Gaps (Sponsor Only)

| Field | Type | Description |
|-------|------|-------------|
| `metricName` | string | Name of the metric being compared |
| `yourValue` | decimal | Sponsor's actual value for this metric |
| `industryAvg` | decimal | Industry average for this metric |
| `topPerformer` | decimal | Top performer (90th percentile) value |
| `gapVsIndustry` | string | Percentage gap vs industry (e.g., "+18.8%", "-12.3%") |
| `gapVsTopPerformer` | string | Percentage gap vs top performers |
| `status` | string | Performance status: "Above Average", "Average", "Below Average" |
| `recommendation` | string | Actionable suggestion for improvement |

---

## Business Logic

### Engagement Score Calculation

Weighted score from three components (0-100 scale):

```
Engagement Score = Activity (40%) + Response (30%) + Retention (30%)

Activity Score = min(40, analysesPerFarmer * 10)
Response Score = messageResponseRate * 0.3
Retention Score = farmerRetentionRate * 0.3
```

### Retention Rate Calculation

Measures farmer continuation from first to second half of period:

```
Period Midpoint = cutoffDate + (timePeriodDays / 2)

Farmers in First Half = Unique farmers with analyses before midpoint
Farmers in Second Half = Unique farmers with analyses after midpoint
Retained Farmers = Intersection of both sets

Retention Rate = (Retained Farmers / Farmers in First Half) * 100
```

### Percentile Calculation

Statistical ranking showing relative position among all sponsors:

- **90th Percentile**: Top 10% of performers
- **75th Percentile**: Top 25% of performers
- **50th Percentile**: Median performance

**Interpretation**:
- 90-100: "Top 10% - Elite Performer"
- 75-89: "Top 25% - High Performer"
- 60-74: "Top 40% - Above Average Performer"
- 40-59: "Average Performer"
- 0-39: "Below Average - Needs Improvement"

### Gap Analysis

Compares sponsor vs industry with percentage differences:

```
Gap % = ((Your Value - Benchmark Value) / Benchmark Value) * 100

Positive Gap (+X%): Performing above benchmark
Negative Gap (-X%): Performing below benchmark
```

**Status Classification**:
- **Above Average**: Gap vs industry ≥ +5%
- **Average**: Gap vs industry between -5% and +5%
- **Below Average**: Gap vs industry ≤ -5%

---

## Caching Strategy

**Cache Duration**: 24 hours (1440 minutes)
**Cache Key Pattern**: `CompetitiveBenchmarking:{sponsorId}:{timePeriodDays}`
**Rationale**: Benchmark data changes slowly, 24-hour TTL balances freshness with performance

**Cache Behavior**:
- First request: ~500-800ms (cache miss, computation)
- Subsequent requests: <10ms (cache hit)
- Cache invalidation: Automatic after 24 hours

---

## Security & Privacy

### Anonymization Requirements

- **Minimum Sponsors**: 3 sponsors required for benchmark calculation
- **Rationale**: Prevents identification of individual sponsor performance
- **Enforcement**: API returns 400 error if fewer than 3 sponsors exist

### Authorization

- **Sponsors**: See own performance vs anonymized industry benchmarks
- **Admins**: See only industry-wide benchmarks (no individual sponsor data)
- **SecuredOperation AOP**: Validates `GetCompetitiveBenchmarkingQuery` operation claim

### Data Privacy

- No individual sponsor data exposed in industry benchmarks
- Aggregate metrics computed across all sponsors
- Top performer values represent 90th percentile, not specific sponsor

---

## Use Cases

### For Sponsors

1. **Competitive Positioning**: Understand market standing and identify improvement areas
2. **Goal Setting**: Set realistic targets based on industry benchmarks
3. **Performance Monitoring**: Track progress vs industry over time
4. **Investment Decisions**: Justify additional resources to reach top performer levels

### For Admins

1. **Industry Insights**: Monitor overall sponsor ecosystem health
2. **Platform Metrics**: Track industry-wide engagement and retention trends
3. **Benchmarking Data**: Provide context for sponsor performance reviews

### For Mobile/Web Apps

1. **Dashboard Cards**: Display percentile rankings and key gaps
2. **Performance Charts**: Visualize sponsor vs industry over time
3. **Recommendation Widgets**: Show actionable improvement suggestions
4. **Trend Analysis**: Compare multiple time periods (30d, 60d, 90d)

---

## Frontend Integration Examples

### JavaScript/TypeScript Fetch

```typescript
async function getCompetitiveBenchmarking(timePeriodDays = 90) {
  const response = await fetch(
    `${API_BASE_URL}/api/v1/sponsorship/competitive-benchmarking?timePeriodDays=${timePeriodDays}`,
    {
      headers: {
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json'
      }
    }
  );

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
  }

  const result = await response.json();
  return result.data;
}
```

### Flutter/Dart Implementation

```dart
Future<CompetitiveBenchmarkingDto> getCompetitiveBenchmarking({int timePeriodDays = 90}) async {
  final response = await http.get(
    Uri.parse('$apiBaseUrl/api/v1/sponsorship/competitive-benchmarking?timePeriodDays=$timePeriodDays'),
    headers: {
      'Authorization': 'Bearer $accessToken',
      'Content-Type': 'application/json',
    },
  );

  if (response.statusCode == 200) {
    final jsonData = json.decode(response.body);
    return CompetitiveBenchmarkingDto.fromJson(jsonData['data']);
  } else {
    throw Exception('Failed to load competitive benchmarking: ${response.body}');
  }
}
```

### React Component Example

```jsx
function CompetitiveBenchmarkingCard() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function fetchData() {
      try {
        const result = await getCompetitiveBenchmarking(90);
        setData(result);
      } catch (error) {
        console.error('Error fetching benchmarking:', error);
      } finally {
        setLoading(false);
      }
    }
    fetchData();
  }, []);

  if (loading) return <LoadingSpinner />;

  return (
    <Card>
      <h2>Your Performance vs Industry</h2>
      <PercentileRanking ranking={data.ranking} />
      <GapAnalysis gaps={data.gaps} />
      <IndustryComparison
        yourPerformance={data.yourPerformance}
        benchmarks={data.industryBenchmarks}
      />
    </Card>
  );
}
```

---

## Testing Guide

### Test Scenarios

#### Scenario 1: Sponsor with Strong Performance
**Expected**: Above average status, positive gaps, high percentiles

#### Scenario 2: Sponsor with Weak Performance
**Expected**: Below average status, negative gaps, low percentiles

#### Scenario 3: Admin Access
**Expected**: Industry benchmarks only, no sponsor-specific data

#### Scenario 4: Insufficient Sponsors (<3)
**Expected**: 400 error with anonymization message

#### Scenario 5: Different Time Periods
**Expected**: Metrics vary based on 30/60/90/180 day windows

### Postman Collection

```json
{
  "name": "Competitive Benchmarking - 90 Days",
  "request": {
    "method": "GET",
    "header": [
      {
        "key": "Authorization",
        "value": "Bearer {{sponsor_token}}"
      }
    ],
    "url": {
      "raw": "{{base_url}}/api/v1/sponsorship/competitive-benchmarking?timePeriodDays=90",
      "host": ["{{base_url}}"],
      "path": ["api", "v1", "sponsorship", "competitive-benchmarking"],
      "query": [
        {
          "key": "timePeriodDays",
          "value": "90"
        }
      ]
    }
  }
}
```

---

## Performance Characteristics

| Metric | Value | Notes |
|--------|-------|-------|
| **Cache Hit** | <10ms | Typical response from Redis cache |
| **Cache Miss** | 500-800ms | Initial computation with database queries |
| **Database Queries** | 3-5 | Analyses, users, messages, subscriptions |
| **Memory Usage** | ~100KB | Per cached result |
| **Cache TTL** | 24 hours | Automatic invalidation |
| **Scalability** | O(n) | n = number of sponsored analyses |

---

## Change Log

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-11-12 | Initial implementation with industry benchmarks, percentile rankings, gap analysis |

---

## Support & Troubleshooting

### Common Issues

**Issue**: 400 error "Insufficient sponsors"
**Solution**: Ensure at least 3 sponsors exist in system

**Issue**: 403 Forbidden
**Solution**: Verify user has `GetCompetitiveBenchmarkingQuery` operation claim assigned

**Issue**: Slow response times
**Solution**: Check cache hit rate and Redis connection

**Issue**: Unexpected percentile rankings
**Solution**: Verify time period matches expectation (default 90 days)

### Contact

For technical questions or issues, contact:
- Backend Team: backend@ziraai.com
- Documentation: docs@ziraai.com
