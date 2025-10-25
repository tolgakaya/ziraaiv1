# Sponsor Analytics API - Complete Documentation

**Version:** 1.0  
**Last Updated:** 2025-01-25  
**Base URL:** `https://api.ziraai.com` (Production) | `https://ziraai-api-sit.up.railway.app` (Staging)  
**API Version Header:** `x-dev-arch-version: 1.0`

---

## 📚 Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Endpoints Summary](#endpoints-summary)
4. [Dashboard Summary](#1-dashboard-summary)
5. [Package Distribution Statistics](#2-package-distribution-statistics)
6. [Code Analysis Statistics](#3-code-analysis-statistics)
7. [Messaging Analytics](#4-messaging-analytics)
8. [Impact Analytics](#5-impact-analytics)
9. [Temporal Analytics](#6-temporal-analytics)
10. [ROI Analytics](#7-roi-analytics)
11. [Use Cases & Scenarios](#use-cases--scenarios)
12. [Error Handling](#error-handling)
13. [Best Practices](#best-practices)

---

## Overview

The Sponsor Analytics API provides comprehensive insights for sponsors to track their investment performance, farmer engagement, and agricultural impact. All endpoints are designed for real-time dashboards, reporting systems, and mobile applications.

### Key Features

- **Real-time Metrics:** Live data on messaging, analyses, and farmer engagement
- **Caching Strategy:** Intelligent caching (15 min to 12 hours) for optimal performance
- **Tier-based Insights:** Breakdown by subscription tier (S, M, L, XL)
- **Privacy Compliance:** Tier-based privacy rules automatically applied
- **Mobile-Optimized:** Response structures designed for mobile consumption

---

## Authentication

All endpoints require JWT Bearer authentication with **Sponsor** or **Admin** role.

### Headers Required

```http
Authorization: Bearer {your_jwt_token}
x-dev-arch-version: 1.0
Content-Type: application/json
```

### Getting Your Token

```bash
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "sponsor@example.com",
  "password": "your_password"
}

# Response
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "...",
    "userId": 123
  },
  "success": true
}
```

---

## Endpoints Summary

| Endpoint | Method | Cache TTL | Purpose |
|----------|--------|-----------|---------|
| `/api/v1/sponsorship/messaging-analytics` | GET | 15 min | Messaging & conversation metrics |
| `/api/v1/sponsorship/impact-analytics` | GET | 6 hours | Agricultural & geographic impact |
| `/api/v1/sponsorship/temporal-analytics` | GET | 1 hour | Time-series trends & patterns |
| `/api/v1/sponsorship/roi-analytics` | GET | 12 hours | Financial ROI & efficiency metrics |

---

## 1. Dashboard Summary

### Endpoint

```
GET /api/v1/sponsorship/dashboard-summary
```

### Description

Comprehensive mobile dashboard overview including sent codes, total analyses, purchases, and tier-based package breakdowns. Optimized for mobile app home screen with 24-hour cache.

### Query Parameters

None required - returns all-time summary for the authenticated sponsor.

### Request Example

```bash
GET /api/v1/sponsorship/dashboard-summary
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

### Response Schema

```json
{
  "data": {
    "totalCodesCount": 400,
    "sentCodesCount": 320,
    "sentCodesPercentage": 80.00,
    "totalAnalysesCount": 456,
    "purchasesCount": 3,
    "totalSpent": 50000.00,
    "currency": "TRY",
    "activePackages": [
      {
        "tierName": "S",
        "tierDisplayName": "Small",
        "totalCodes": 100,
        "sentCodes": 85,
        "unsentCodes": 15,
        "usedCodes": 72,
        "unusedSentCodes": 13,
        "remainingCodes": 15,
        "usagePercentage": 84.71,
        "distributionPercentage": 85.00,
        "uniqueFarmers": 45,
        "analysesCount": 123
      }
    ],
    "overallStats": {
      "smsDistributions": 245,
      "whatsAppDistributions": 75,
      "overallRedemptionRate": 82.50,
      "averageRedemptionTime": 12.3,
      "totalUniqueFarmers": 127,
      "lastPurchaseDate": "2025-01-20T10:00:00Z",
      "lastDistributionDate": "2025-01-24T15:30:00Z"
    }
  },
  "success": true,
  "message": "Dashboard summary retrieved successfully"
}
```

### Response Fields Explained

#### Top-Level Metrics
- `totalCodesCount`: Total sponsorship codes purchased
- `sentCodesCount`: Codes distributed to farmers
- `sentCodesPercentage`: Distribution rate (%)
- `totalAnalysesCount`: Total plant analyses performed by sponsored farmers
- `purchasesCount`: Number of sponsorship purchases made
- `totalSpent`: Total investment (TRY/USD)
- `currency`: Currency code

#### Active Packages (Per Tier)
- `tierName`: S, M, L, or XL
- `tierDisplayName`: Full tier name
- `totalCodes`: Codes purchased in this tier
- `sentCodes`: Codes distributed
- `unsentCodes`: Codes not yet distributed
- `usedCodes`: Codes redeemed by farmers
- `unusedSentCodes`: Distributed but not redeemed
- `remainingCodes`: Available for distribution
- `usagePercentage`: (usedCodes / sentCodes) × 100
- `distributionPercentage`: (sentCodes / totalCodes) × 100
- `uniqueFarmers`: Distinct farmers sponsored in this tier
- `analysesCount`: Analyses generated by this tier

#### Overall Statistics
- `smsDistributions`: Codes sent via SMS
- `whatsAppDistributions`: Codes sent via WhatsApp
- `overallRedemptionRate`: Overall code usage rate (%)
- `averageRedemptionTime`: Average days from distribution to redemption
- `totalUniqueFarmers`: Total unique farmers across all tiers
- `lastPurchaseDate`: Most recent purchase date
- `lastDistributionDate`: Most recent code distribution date

### Use Cases

**1. Mobile App Home Screen**
```javascript
const { totalAnalysesCount, totalUniqueFarmers, sentCodesPercentage } = data;
// Display: "456 Analyses • 127 Farmers • 80% Distributed"
```

**2. Tier Performance Card**
```javascript
activePackages.forEach(tier => {
  showTierCard(tier.tierName, tier.usagePercentage, tier.analysesCount);
});
```

**3. Distribution Status**
```javascript
const { sentCodesCount, totalCodesCount } = data;
const remaining = totalCodesCount - sentCodesCount;
showAlert(`${remaining} codes ready to distribute`);
```

### Cache Behavior

- **TTL:** 24 hours (1440 minutes)
- **Cache Key:** `SponsorDashboard:{SponsorId}`
- **Why 24 hours?** Dashboard summary changes slowly, suitable for daily review

---

## 2. Package Distribution Statistics

### Endpoint

```
GET /api/v1/sponsorship/package-statistics
```

### Description

Package-level distribution funnel showing purchased → distributed → redeemed conversion rates with breakdowns by package, tier, and distribution channel.

### Query Parameters

None required - returns all-time package statistics for the authenticated sponsor.

### Request Example

```bash
GET /api/v1/sponsorship/package-statistics
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

### Response Schema

```json
{
  "data": {
    "totalCodesPurchased": 400,
    "totalCodesDistributed": 320,
    "totalCodesRedeemed": 264,
    "codesNotDistributed": 80,
    "codesDistributedNotRedeemed": 56,
    "distributionRate": 80.00,
    "redemptionRate": 82.50,
    "overallSuccessRate": 66.00,
    "packageBreakdowns": [
      {
        "purchaseId": 101,
        "purchaseDate": "2025-01-15T10:00:00Z",
        "tierName": "L",
        "codesPurchased": 200,
        "codesDistributed": 170,
        "codesRedeemed": 145,
        "codesNotDistributed": 30,
        "codesDistributedNotRedeemed": 25,
        "distributionRate": 85.00,
        "redemptionRate": 85.29,
        "totalAmount": 25000.00,
        "currency": "TRY"
      }
    ],
    "tierBreakdowns": [
      {
        "tierName": "L",
        "tierDisplayName": "Large",
        "codesPurchased": 200,
        "codesDistributed": 170,
        "codesRedeemed": 145,
        "distributionRate": 85.00,
        "redemptionRate": 85.29
      }
    ],
    "channelBreakdowns": [
      {
        "channel": "SMS",
        "codesDistributed": 245,
        "codesDelivered": 240,
        "codesRedeemed": 202,
        "deliveryRate": 97.96,
        "redemptionRate": 82.45
      }
    ]
  },
  "success": true,
  "message": "Package distribution statistics retrieved successfully"
}
```

### Response Fields Explained

#### Funnel Metrics
- `totalCodesPurchased`: All codes purchased
- `totalCodesDistributed`: Codes sent to farmers
- `totalCodesRedeemed`: Codes used by farmers
- `codesNotDistributed`: Not yet sent
- `codesDistributedNotRedeemed`: Sent but not used
- `distributionRate`: (distributed / purchased) × 100
- `redemptionRate`: (redeemed / distributed) × 100
- `overallSuccessRate`: (redeemed / purchased) × 100

#### Package Breakdowns
- Per-purchase analysis with ROI tracking
- Includes purchase date, tier, costs

#### Tier Breakdowns
- Aggregated metrics per subscription tier (S, M, L, XL)

#### Channel Breakdowns
- Performance by distribution channel (SMS, WhatsApp, Email, Manual)
- Delivery and redemption rates per channel

### Use Cases

**1. Conversion Funnel Chart**
```javascript
const funnel = [
  { stage: 'Purchased', count: totalCodesPurchased },
  { stage: 'Distributed', count: totalCodesDistributed },
  { stage: 'Redeemed', count: totalCodesRedeemed }
];
```

**2. Channel Performance Comparison**
```javascript
const bestChannel = channelBreakdowns.sort((a, b) => 
  b.redemptionRate - a.redemptionRate
)[0];
console.log(`Best channel: ${bestChannel.channel} with ${bestChannel.redemptionRate}%`);
```

**3. Package ROI Tracking**
```javascript
packageBreakdowns.forEach(pkg => {
  const roi = (pkg.codesRedeemed * 50 - pkg.totalAmount) / pkg.totalAmount * 100;
  console.log(`Package ${pkg.purchaseId} ROI: ${roi}%`);
});
```

### Cache Behavior

- **TTL:** 5 minutes
- **Cache Key:** Via CacheAspect attribute
- **Why 5 min?** Distribution metrics update frequently

---

## 3. Code Analysis Statistics

### Endpoint

```
GET /api/v1/sponsorship/code-analysis-statistics
```

### Description

Code-level analysis tracking showing which sponsorship codes generated how many analyses, with farmer details (tier-based privacy), crop distribution, and disease insights.

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `includeAnalysisDetails` | Boolean | No | true | Include full analysis list per code |
| `topCodesCount` | Integer | No | 10 | Number of top performing codes to highlight |

### Request Example

```bash
# Get full details with top 10 codes
GET /api/v1/sponsorship/code-analysis-statistics
Authorization: Bearer {token}
x-dev-arch-version: 1.0

# Get top 20 codes without analysis details
GET /api/v1/sponsorship/code-analysis-statistics?includeAnalysisDetails=false&topCodesCount=20
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

### Response Schema

```json
{
  "data": {
    "totalRedeemedCodes": 264,
    "totalAnalysesPerformed": 456,
    "averageAnalysesPerCode": 1.73,
    "totalActiveFarmers": 103,
    "codeBreakdowns": [
      {
        "code": "ZIRA-L-ABC123",
        "tierName": "L",
        "farmerId": 567,
        "farmerName": "Ahmet Yılmaz",
        "farmerEmail": "ahmet@example.com",
        "farmerPhone": "+905551234567",
        "location": "Antalya, Merkez",
        "redeemedDate": "2025-01-10T14:00:00Z",
        "subscriptionStatus": "Active",
        "subscriptionEndDate": "2025-04-10T14:00:00Z",
        "totalAnalyses": 12,
        "analyses": [
          {
            "analysisId": 1234,
            "analysisDate": "2025-01-24T10:00:00Z",
            "cropType": "Tomato",
            "disease": "Early Blight",
            "diseaseCategory": "Fungal",
            "severity": "High",
            "location": "Antalya, Merkez",
            "status": "Completed",
            "sponsorLogoDisplayed": true,
            "analysisDetailsUrl": "https://ziraai.com/api/v1/sponsorship/analysis/1234"
          }
        ],
        "lastAnalysisDate": "2025-01-24T10:00:00Z",
        "daysSinceLastAnalysis": 1
      }
    ],
    "topPerformingCodes": [],
    "cropTypeDistribution": [
      {
        "cropType": "Tomato",
        "analysisCount": 156,
        "percentage": 34.21,
        "uniqueFarmers": 45
      }
    ],
    "diseaseDistribution": [
      {
        "disease": "Early Blight",
        "category": "Fungal",
        "occurrenceCount": 67,
        "percentage": 14.69,
        "affectedCrops": ["Tomato", "Potato"],
        "geographicDistribution": ["Antalya", "İzmir", "Mersin"]
      }
    ]
  },
  "success": true,
  "message": "Code analysis statistics retrieved successfully"
}
```

### Response Fields Explained

#### Top-Level Metrics
- `totalRedeemedCodes`: Codes used by farmers
- `totalAnalysesPerformed`: Total analyses across all codes
- `averageAnalysesPerCode`: Average analyses per redeemed code
- `totalActiveFarmers`: Farmers with active subscriptions

#### Code Breakdowns
- **Tier-Based Privacy Rules:**
  - **L/XL**: Full farmer details (name, email, phone, location)
  - **M**: Anonymous farmer, city-only location
  - **S**: Anonymous farmer, limited location

- Per-code tracking with farmer engagement metrics
- Subscription status and expiry date
- Analysis history (optional, controlled by `includeAnalysisDetails`)

#### Top Performing Codes
- Codes with highest analysis counts (controlled by `topCodesCount`)

#### Crop & Disease Distribution
- Crop type breakdown with farmer counts
- Disease occurrence with affected crops and geography

### Use Cases

**1. Farmer Engagement Report**
```javascript
codeBreakdowns.forEach(code => {
  if (code.daysSinceLastAnalysis > 30) {
    sendReEngagementEmail(code.farmerEmail);
  }
});
```

**2. Disease Hotspot Detection**
```javascript
diseaseDistribution.forEach(disease => {
  if (disease.category === 'Fungal' && disease.percentage > 15) {
    alertForDiseaseTrend(disease.disease, disease.geographicDistribution);
  }
});
```

**3. Crop Focus Analysis**
```javascript
const topCrop = cropTypeDistribution[0];
console.log(`Focus crop: ${topCrop.cropType} (${topCrop.uniqueFarmers} farmers)`);
```

**4. Code ROI Calculation**
```javascript
codeBreakdowns.forEach(code => {
  const value = code.totalAnalyses * 50; // 50 TL per analysis
  const efficiency = code.totalAnalyses >= 2 ? 'High' : 'Low';
  console.log(`Code ${code.code}: ${code.totalAnalyses} analyses (${efficiency} efficiency)`);
});
```

### Cache Behavior

- **TTL:** 5 minutes
- **Cache Key:** Via CacheAspect attribute
- **Why 5 min?** Analysis data updates frequently

---

## 4. Messaging Analytics

### Endpoint

```
GET /api/v1/sponsorship/messaging-analytics
```

### Description

Provides comprehensive messaging and conversation metrics between sponsors and farmers, including response times, engagement rates, and conversation quality indicators.

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `startDate` | DateTime | No | 30 days ago | Filter messages from this date (ISO 8601) |
| `endDate` | DateTime | No | Now | Filter messages until this date (ISO 8601) |

### Request Example

```bash
# Get all-time messaging analytics
GET /api/v1/sponsorship/messaging-analytics
Authorization: Bearer {token}
x-dev-arch-version: 1.0

# Get last 7 days
GET /api/v1/sponsorship/messaging-analytics?startDate=2025-01-18T00:00:00Z&endDate=2025-01-25T23:59:59Z
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

### Response Schema

```json
{
  "data": {
    "totalMessagesSent": 156,
    "totalMessagesReceived": 203,
    "unreadMessagesCount": 12,
    "averageResponseTimeHours": 2.45,
    "responseRate": 87.50,
    "totalConversations": 45,
    "activeConversations": 23,
    "textMessageCount": 289,
    "voiceMessageCount": 42,
    "attachmentCount": 28,
    "averageMessageRating": 4.35,
    "positiveRatingsCount": 67,
    "mostActiveConversations": [
      {
        "analysisId": 1234,
        "farmerId": 567,
        "farmerName": "Ahmet Yılmaz",
        "messageCount": 45,
        "sponsorMessageCount": 22,
        "farmerMessageCount": 23,
        "lastMessageDate": "2025-01-25T14:30:00Z",
        "hasUnreadMessages": true,
        "cropType": "Tomato",
        "disease": "Early Blight",
        "averageRating": 4.8
      }
    ],
    "dataStartDate": "2024-12-26T00:00:00Z",
    "dataEndDate": "2025-01-25T23:59:59Z"
  },
  "success": true,
  "message": "Messaging analytics retrieved successfully"
}
```

### Response Fields Explained

#### Message Volume
- `totalMessagesSent`: Total messages sent by sponsor
- `totalMessagesReceived`: Total messages received from farmers
- `unreadMessagesCount`: Unread messages pending sponsor response

#### Response Metrics
- `averageResponseTimeHours`: Average time (hours) to respond to farmer messages
- `responseRate`: Percentage of farmer messages that received a response

#### Conversation Metrics
- `totalConversations`: Total unique plant analysis conversations
- `activeConversations`: Conversations with messages in last 7 days

#### Content Types
- `textMessageCount`: Text-only messages
- `voiceMessageCount`: Voice/audio messages
- `attachmentCount`: Messages with file attachments

#### Satisfaction Metrics
- `averageMessageRating`: Average rating (1-5 scale)
- `positiveRatingsCount`: Count of ratings ≥ 4 stars

#### Top Conversations
- `mostActiveConversations`: Top 10 conversations by message count
- Includes farmer name (tier-based privacy: S/M = Anonymous, L/XL = Full name)

### Use Cases

**1. Dashboard Overview Widget**
```javascript
// Display key messaging metrics
const { totalMessagesSent, totalMessagesReceived, unreadMessagesCount, responseRate } = data;
```

**2. Response Time Monitoring**
```javascript
// Alert if response time exceeds SLA
if (averageResponseTimeHours > 24) {
  showAlert('Response time exceeds 24-hour SLA');
}
```

**3. Conversation Priority List**
```javascript
// Sort by unread and last message date
const urgentConversations = mostActiveConversations
  .filter(c => c.hasUnreadMessages)
  .sort((a, b) => new Date(b.lastMessageDate) - new Date(a.lastMessageDate));
```

**4. Engagement Report**
```javascript
// Calculate engagement score
const engagementScore = (responseRate * 0.6) + (averageMessageRating / 5 * 100 * 0.4);
```

### Cache Behavior

- **TTL:** 15 minutes
- **Cache Key:** `MessagingAnalytics:{SponsorId}:{StartDate}-{EndDate}`
- **Invalidation:** Automatic after 15 minutes
- **Why 15 min?** Real-time messaging requires frequent updates

---

## 5. Impact Analytics

### Endpoint

```
GET /api/v1/sponsorship/impact-analytics
```

### Description

Tracks agricultural and geographic impact including farmers reached, crops analyzed, diseases detected, and regional distribution.

### Query Parameters

None required - returns all-time impact metrics for the authenticated sponsor.

### Request Example

```bash
GET /api/v1/sponsorship/impact-analytics
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

### Response Schema

```json
{
  "data": {
    "totalFarmersReached": 127,
    "activeFarmersLast30Days": 78,
    "farmerRetentionRate": 61.42,
    "averageFarmerLifetimeDays": 145.6,
    "totalCropsAnalyzed": 456,
    "uniqueCropTypes": 23,
    "diseasesDetected": 234,
    "criticalIssuesResolved": 45,
    "citiesReached": 34,
    "districtsReached": 89,
    "topCities": [
      {
        "cityName": "Antalya",
        "farmerCount": 23,
        "analysisCount": 89,
        "percentageOfTotal": 19.52
      }
    ],
    "severityDistribution": {
      "low": 123,
      "moderate": 198,
      "high": 89,
      "critical": 46
    },
    "topCrops": [
      {
        "cropType": "Tomato",
        "analysisCount": 156,
        "percentageOfTotal": 34.21
      }
    ],
    "topDiseases": [
      {
        "diseaseName": "Early Blight",
        "occurrenceCount": 67,
        "percentageOfTotal": 14.69
      }
    ],
    "impactSummary": "Sponsorluk programınız 127 çiftçiye ulaşarak 34 şehirde 456 ürün analizi gerçekleştirdi. En çok etki yarattığınız bölge Antalya (23 çiftçi, 89 analiz)."
  },
  "success": true,
  "message": "Impact analytics retrieved successfully"
}
```

### Response Fields Explained

#### Farmer Impact
- `totalFarmersReached`: Unique farmers sponsored
- `activeFarmersLast30Days`: Farmers with analysis in last 30 days
- `farmerRetentionRate`: (Active this month / Active last month) × 100
- `averageFarmerLifetimeDays`: Average days from first to last analysis

#### Agricultural Impact
- `totalCropsAnalyzed`: Total plant analyses performed
- `uniqueCropTypes`: Distinct crop varieties
- `diseasesDetected`: Analyses with identified disease
- `criticalIssuesResolved`: Critical severity cases addressed

#### Geographic Reach
- `citiesReached`: Number of unique cities
- `districtsReached`: Number of unique districts
- `topCities`: Top 10 cities by analysis count

#### Distribution Metrics
- `severityDistribution`: Breakdown by health severity
- `topCrops`: Top 10 crop types by count
- `topDiseases`: Top 10 diseases by occurrence

#### Impact Summary
- `impactSummary`: Turkish language narrative summary

### Use Cases

**1. Impact Dashboard**
```javascript
// Show geographic reach on map
topCities.forEach(city => {
  addMarkerToMap(city.cityName, city.analysisCount);
});
```

**2. Retention Monitoring**
```javascript
// Alert on retention drop
if (farmerRetentionRate < 50) {
  showAlert('Farmer retention below 50% - action required');
}
```

**3. Crop Focus Analysis**
```javascript
// Identify most supported crops
const topCrop = topCrops[0];
console.log(`Primary focus: ${topCrop.cropType} (${topCrop.percentageOfTotal}%)`);
```

**4. Critical Issue Tracking**
```javascript
// Track critical issue resolution rate
const criticalResolutionRate = (criticalIssuesResolved / severityDistribution.critical) * 100;
```

### Cache Behavior

- **TTL:** 6 hours (360 minutes)
- **Cache Key:** `ImpactAnalytics:{SponsorId}`
- **Why 6 hours?** Impact metrics change slowly, suitable for daily reporting

---

## 6. Temporal Analytics

### Endpoint

```
GET /api/v1/sponsorship/temporal-analytics
```

### Description

Time-series analytics showing trends over time with dynamic grouping by day, week, or month. Includes trend analysis and peak performance detection.

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `startDate` | DateTime | No | 90 days ago | Start of analysis period |
| `endDate` | DateTime | No | Now | End of analysis period |
| `groupBy` | String | No | `Day` | Grouping interval: `Day`, `Week`, or `Month` |

### Request Example

```bash
# Get daily trends for last 30 days
GET /api/v1/sponsorship/temporal-analytics?startDate=2024-12-26T00:00:00Z&endDate=2025-01-25T23:59:59Z&groupBy=Day
Authorization: Bearer {token}
x-dev-arch-version: 1.0

# Get monthly trends for last year
GET /api/v1/sponsorship/temporal-analytics?startDate=2024-01-01T00:00:00Z&endDate=2025-01-01T23:59:59Z&groupBy=Month
Authorization: Bearer {token}
x-dev-arch-version: 1.0

# Get weekly trends (default period)
GET /api/v1/sponsorship/temporal-analytics?groupBy=Week
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

### Response Schema

```json
{
  "data": {
    "groupBy": "Day",
    "timeSeries": [
      {
        "periodLabel": "2025-01-20",
        "periodStart": "2025-01-20T00:00:00Z",
        "periodEnd": "2025-01-20T23:59:59Z",
        "codesDistributed": 5,
        "codesRedeemed": 4,
        "analysesPerformed": 12,
        "newFarmers": 2,
        "activeFarmers": 8,
        "messagesSent": 15,
        "messagesReceived": 18
      }
    ],
    "trendAnalysis": {
      "analysesGrowth": 15.50,
      "farmerGrowth": 8.30,
      "engagementGrowth": 12.75,
      "redemptionGrowth": 5.60,
      "overallTrend": "Up"
    },
    "peakMetrics": {
      "peakAnalysisDay": {
        "date": "2025-01-22",
        "analysisCount": 45,
        "activeFarmers": 23
      },
      "peakRedemptionDay": {
        "date": "2025-01-18",
        "redemptionCount": 12,
        "codesRedeemed": 12
      }
    },
    "startDate": "2024-12-26T00:00:00Z",
    "endDate": "2025-01-25T23:59:59Z"
  },
  "success": true,
  "message": "Temporal analytics retrieved successfully"
}
```

### Response Fields Explained

#### Time Series Data
- `periodLabel`: Display label (e.g., "2025-01-20", "2025-W03", "2025-01")
- `periodStart` / `periodEnd`: Period boundaries
- `codesDistributed`: Codes created in period
- `codesRedeemed`: Codes used by farmers
- `analysesPerformed`: Plant analyses completed
- `newFarmers`: First-time farmers in period
- `activeFarmers`: Unique farmers with activity
- `messagesSent` / `messagesReceived`: Message counts

#### Trend Analysis
- `analysesGrowth`: % change in analyses (current vs previous period)
- `farmerGrowth`: % change in active farmers
- `engagementGrowth`: % change in messaging activity
- `redemptionGrowth`: % change in code redemptions
- `overallTrend`: "Up", "Down", or "Stable"

#### Peak Performance
- `peakAnalysisDay`: Day with highest analysis count
- `peakRedemptionDay`: Day with highest redemption count

### Use Cases

**1. Line Chart Visualization**
```javascript
// Create time-series chart
const chartData = timeSeries.map(period => ({
  x: period.periodLabel,
  analyses: period.analysesPerformed,
  farmers: period.activeFarmers
}));
```

**2. Growth Monitoring**
```javascript
// Show trend indicators
if (trendAnalysis.overallTrend === 'Up') {
  showGreenArrow();
} else if (trendAnalysis.overallTrend === 'Down') {
  showRedArrow();
}
```

**3. Peak Day Highlighting**
```javascript
// Highlight best performing day
const { peakAnalysisDay } = peakMetrics;
highlightDate(peakAnalysisDay.date, peakAnalysisDay.analysisCount);
```

**4. Weekly Report Generation**
```javascript
// Generate weekly summary
const weeklyData = await fetch('/api/v1/sponsorship/temporal-analytics?groupBy=Week');
generatePDFReport(weeklyData);
```

### GroupBy Options

**Day:**
- Best for: Last 30-90 days analysis
- Label format: `YYYY-MM-DD`
- Use case: Daily dashboard, detailed trends

**Week:**
- Best for: Last 3-6 months analysis
- Label format: `YYYY-Www` (e.g., 2025-W03)
- Use case: Weekly reports, mid-term trends

**Month:**
- Best for: Last 12 months or year-over-year
- Label format: `YYYY-MM`
- Use case: Monthly reports, long-term trends

### Cache Behavior

- **TTL:** 1 hour (60 minutes)
- **Cache Key:** `TemporalAnalytics:{SponsorId}:{StartDate}-{EndDate}:{GroupBy}`
- **Why 1 hour?** Balances real-time needs with performance

---

## 7. ROI Analytics

### Endpoint

```
GET /api/v1/sponsorship/roi-analytics
```

### Description

Financial return on investment (ROI) analytics including cost breakdown, value generation, efficiency metrics, and tier-based performance.

### Query Parameters

None required - calculates all-time ROI for the authenticated sponsor.

### Request Example

```bash
GET /api/v1/sponsorship/roi-analytics
Authorization: Bearer {token}
x-dev-arch-version: 1.0
```

### Response Schema

```json
{
  "data": {
    "totalInvestment": 50000.00,
    "costPerCode": 125.00,
    "costPerRedemption": 156.25,
    "costPerAnalysis": 109.65,
    "costPerFarmer": 393.70,
    "totalAnalysesValue": 22800.00,
    "averageLifetimeValuePerFarmer": 179.53,
    "averageValuePerCode": 57.00,
    "overallROI": -54.40,
    "roiStatus": "Negative",
    "roiByTier": [
      {
        "tierName": "L",
        "investment": 20000.00,
        "codesPurchased": 100,
        "codesRedeemed": 85,
        "analysesGenerated": 234,
        "totalValue": 11700.00,
        "roi": -41.50,
        "utilizationRate": 85.00
      },
      {
        "tierName": "M",
        "investment": 15000.00,
        "codesPurchased": 150,
        "codesRedeemed": 120,
        "analysesGenerated": 178,
        "totalValue": 8900.00,
        "roi": -40.67,
        "utilizationRate": 80.00
      }
    ],
    "utilizationRate": 82.50,
    "wasteRate": 12.30,
    "breakevenAnalysisCount": 1000,
    "analysesUntilBreakeven": 544,
    "estimatedPaybackDays": 245,
    "analysisUnitValue": 50.00
  },
  "success": true,
  "message": "ROI analytics calculated successfully"
}
```

### Response Fields Explained

#### Cost Breakdown
- `totalInvestment`: Total amount spent on sponsorship purchases (TL)
- `costPerCode`: Investment divided by total codes purchased
- `costPerRedemption`: Investment divided by redeemed codes
- `costPerAnalysis`: Investment divided by analyses generated
- `costPerFarmer`: Investment divided by unique farmers reached

#### Value Analysis
- `totalAnalysesValue`: Analyses count × `analysisUnitValue` (50 TL)
- `averageLifetimeValuePerFarmer`: Average value generated per farmer
- `averageValuePerCode`: Average value per distributed code

#### ROI Metrics
- `overallROI`: ((Total Value - Total Investment) / Total Investment) × 100
- `roiStatus`: "Positive" (>0%), "Negative" (<0%), or "Breakeven" (=0%)
- `roiByTier`: ROI breakdown by subscription tier (S, M, L, XL)

#### Efficiency Metrics
- `utilizationRate`: (Redeemed codes / Purchased codes) × 100
- `wasteRate`: (Expired unused codes / Purchased codes) × 100
- `breakevenAnalysisCount`: Number of analyses needed to breakeven
- `analysesUntilBreakeven`: Remaining analyses to reach breakeven
- `estimatedPaybackDays`: Estimated days to breakeven (null if already profitable)

#### Supporting Data
- `analysisUnitValue`: Standard value per analysis (50 TL) - used for calculations

### Use Cases

**1. ROI Dashboard Gauge**
```javascript
// Display ROI with color coding
const { overallROI, roiStatus } = data;
const color = roiStatus === 'Positive' ? 'green' : 'red';
showGauge(overallROI, color);
```

**2. Tier Performance Comparison**
```javascript
// Compare tier ROI
const bestTier = roiByTier.sort((a, b) => b.roi - a.roi)[0];
console.log(`Best performing tier: ${bestTier.tierName} with ${bestTier.roi}% ROI`);
```

**3. Breakeven Tracking**
```javascript
// Show progress to breakeven
const progress = ((breakevenAnalysisCount - analysesUntilBreakeven) / breakevenAnalysisCount) * 100;
showProgressBar(progress);
```

**4. Efficiency Alert**
```javascript
// Alert on low utilization
if (utilizationRate < 70) {
  showAlert(`Low code utilization: ${utilizationRate}%`);
}
```

**5. Investment Analysis**
```javascript
// Calculate cost per value unit
const costEfficiency = (totalInvestment / totalAnalysesValue) * 100;
console.log(`Spending ${costEfficiency}% of generated value`);
```

### Understanding ROI Calculation

**How It Works:**

1. **Total Investment:** Sum of all sponsorship purchases
   ```
   Purchase 1: 10,000 TL
   Purchase 2: 15,000 TL
   Purchase 3: 25,000 TL
   Total Investment = 50,000 TL
   ```

2. **Total Analyses Value:** Analyses × 50 TL standard value
   ```
   Analyses Performed: 456
   Analysis Unit Value: 50 TL
   Total Value = 456 × 50 = 22,800 TL
   ```

3. **ROI Calculation:**
   ```
   ROI = ((22,800 - 50,000) / 50,000) × 100
   ROI = -54.40% (Negative)
   ```

4. **Breakeven Analysis:**
   ```
   Breakeven Count = 50,000 ÷ 50 = 1,000 analyses
   Current Analyses = 456
   Analyses Until Breakeven = 1,000 - 456 = 544
   ```

**Note:** Each sponsor has different investment and analysis counts → ROI is **completely dynamic per sponsor**.

### Cache Behavior

- **TTL:** 12 hours (720 minutes)
- **Cache Key:** `SponsorROIAnalytics:{SponsorId}`
- **Why 12 hours?** Financial data changes slowly, suitable for daily review

---

## Use Cases & Scenarios

### Scenario 1: Daily Dashboard for Sponsor

**Goal:** Show sponsor a quick overview of their performance

**API Calls:**
```javascript
// 1. Get messaging stats (real-time)
const messaging = await fetch('/api/v1/sponsorship/messaging-analytics');

// 2. Get last 7 days trends
const trends = await fetch('/api/v1/sponsorship/temporal-analytics?groupBy=Day&startDate=' + sevenDaysAgo);

// 3. Get impact summary (cached)
const impact = await fetch('/api/v1/sponsorship/impact-analytics');

// 4. Get ROI status (cached)
const roi = await fetch('/api/v1/sponsorship/roi-analytics');
```

**Dashboard Widgets:**
- Unread messages count (messaging)
- Active conversations (messaging)
- 7-day trend chart (temporal)
- Total farmers reached (impact)
- Current ROI % (roi)
- Analyses until breakeven (roi)

---

### Scenario 2: Weekly Report Generation

**Goal:** Generate PDF report for sponsor to review weekly performance

**API Calls:**
```javascript
// Get weekly temporal data
const weekly = await fetch('/api/v1/sponsorship/temporal-analytics?groupBy=Week&startDate=' + fourWeeksAgo);

// Get impact summary
const impact = await fetch('/api/v1/sponsorship/impact-analytics');

// Get messaging stats for the week
const messaging = await fetch('/api/v1/sponsorship/messaging-analytics?startDate=' + oneWeekAgo);
```

**Report Sections:**
1. **Executive Summary** (impact.impactSummary)
2. **Weekly Trends** (weekly.timeSeries chart)
3. **Messaging Performance** (messaging response metrics)
4. **Geographic Reach** (impact.topCities map)
5. **Crop Distribution** (impact.topCrops pie chart)

---

### Scenario 3: Mobile App - Sponsor Screen

**Goal:** Show sponsor their stats on mobile app

**Screen Layout:**

**Header Card:**
```javascript
const { totalFarmersReached, activeFarmersLast30Days } = impact.data;
// Display: "127 Farmers Reached • 78 Active"
```

**ROI Card:**
```javascript
const { overallROI, roiStatus } = roi.data;
// Display gauge with color coding
```

**Messages Card:**
```javascript
const { unreadMessagesCount, activeConversations } = messaging.data;
// Display: "12 Unread • 23 Active Chats"
```

**Quick Stats:**
```javascript
const { totalCropsAnalyzed, citiesReached } = impact.data;
// Display: "456 Analyses • 34 Cities"
```

**Recent Activity (List):**
```javascript
const { mostActiveConversations } = messaging.data;
// Show top 5 with unread badge
```

---

### Scenario 4: Performance Monitoring Alert System

**Goal:** Alert sponsor on important metrics

**Alert Rules:**

```javascript
// 1. Response Time SLA Alert
if (messaging.averageResponseTimeHours > 24) {
  sendAlert('Response time exceeds 24 hours');
}

// 2. Retention Alert
if (impact.farmerRetentionRate < 50) {
  sendAlert('Farmer retention dropped below 50%');
}

// 3. Utilization Alert
if (roi.utilizationRate < 70) {
  sendAlert('Code utilization is low: ' + roi.utilizationRate + '%');
}

// 4. Trend Alert
if (temporal.trendAnalysis.overallTrend === 'Down') {
  sendAlert('Downward trend detected in activity');
}

// 5. Breakeven Milestone
if (roi.analysesUntilBreakeven <= 100) {
  sendAlert('You are approaching breakeven! Only ' + roi.analysesUntilBreakeven + ' analyses left');
}
```

---

### Scenario 5: Admin Analytics Dashboard

**Goal:** Show all sponsors' performance for admin review

**API Calls (for each sponsor):**
```javascript
const sponsors = await fetch('/api/v1/admin/users?role=Sponsor');

for (const sponsor of sponsors) {
  // Get ROI for comparison
  const roi = await authenticatedFetch(sponsor.id, '/api/v1/sponsorship/roi-analytics');
  
  // Get impact for leaderboard
  const impact = await authenticatedFetch(sponsor.id, '/api/v1/sponsorship/impact-analytics');
}
```

**Admin Dashboard:**
- Leaderboard: Sponsors by total farmers reached
- ROI comparison chart
- Geographic heatmap (all sponsors combined)
- Engagement metrics (response rates comparison)

---

## Error Handling

### Standard Error Response

```json
{
  "data": null,
  "success": false,
  "message": "Error description here"
}
```

### Common Error Scenarios

#### 1. Unauthorized (401)

```json
{
  "success": false,
  "message": "Unauthorized"
}
```

**Cause:** Missing or invalid JWT token  
**Solution:** Refresh token or re-authenticate

#### 2. Forbidden (403)

```json
{
  "success": false,
  "message": "Insufficient permissions"
}
```

**Cause:** User is not Sponsor or Admin  
**Solution:** Verify user role

#### 3. Bad Request (400)

```json
{
  "success": false,
  "message": "Invalid groupBy parameter. Use Day, Week, or Month."
}
```

**Cause:** Invalid query parameter  
**Solution:** Check parameter values

#### 4. No Data Found

```json
{
  "data": {
    "totalMessagesSent": 0,
    "totalMessagesReceived": 0,
    ...
  },
  "success": true,
  "message": "No messages found in specified date range"
}
```

**Cause:** No data exists for the requested period  
**Solution:** Normal behavior - display "No data" message

#### 5. Internal Server Error (500)

```json
{
  "success": false,
  "message": "Error calculating ROI analytics: Database connection timeout"
}
```

**Cause:** Server-side error  
**Solution:** Retry request, contact support if persists

---

## Best Practices

### 1. **Caching Strategy**

**Respect Cache TTLs:**
```javascript
// Cache-aware implementation
const cache = new Map();

async function getMessagingAnalytics() {
  const cacheKey = 'messaging_' + sponsorId;
  const cached = cache.get(cacheKey);
  
  if (cached && Date.now() - cached.timestamp < 15 * 60 * 1000) {
    return cached.data; // Use cached data within 15 min
  }
  
  const data = await fetch('/api/v1/sponsorship/messaging-analytics');
  cache.set(cacheKey, { data, timestamp: Date.now() });
  return data;
}
```

### 2. **Date Range Filtering**

**Use ISO 8601 Format:**
```javascript
// ✅ Correct
const startDate = new Date('2025-01-01').toISOString(); // "2025-01-01T00:00:00.000Z"

// ❌ Incorrect
const startDate = '01/01/2025'; // Won't parse correctly
```

**Time Zone Handling:**
```javascript
// Convert local time to UTC
const localDate = new Date('2025-01-01');
const utcDate = new Date(localDate.getTime() - localDate.getTimezoneOffset() * 60000);
```

### 3. **Pagination for Large Datasets**

**Most Active Conversations (Top 10 Only):**
```javascript
// API returns top 10 automatically
const { mostActiveConversations } = messaging.data;

// For more conversations, use dedicated conversation list endpoint
// (Not part of analytics API)
```

### 4. **Error Handling**

**Retry Logic:**
```javascript
async function fetchWithRetry(url, retries = 3) {
  for (let i = 0; i < retries; i++) {
    try {
      const response = await fetch(url);
      if (!response.ok) throw new Error('HTTP ' + response.status);
      return await response.json();
    } catch (error) {
      if (i === retries - 1) throw error;
      await new Promise(r => setTimeout(r, 1000 * (i + 1))); // Exponential backoff
    }
  }
}
```

### 5. **Performance Optimization**

**Parallel Requests:**
```javascript
// ✅ Fetch all analytics in parallel
const [messaging, impact, temporal, roi] = await Promise.all([
  fetch('/api/v1/sponsorship/messaging-analytics'),
  fetch('/api/v1/sponsorship/impact-analytics'),
  fetch('/api/v1/sponsorship/temporal-analytics'),
  fetch('/api/v1/sponsorship/roi-analytics')
]);

// ❌ Sequential requests (slower)
const messaging = await fetch('/api/v1/sponsorship/messaging-analytics');
const impact = await fetch('/api/v1/sponsorship/impact-analytics');
...
```

### 6. **Mobile App Considerations**

**Minimize Data Transfer:**
```javascript
// Only fetch what you need for current screen
// Dashboard: messaging + impact + roi (3 requests)
// Trends Screen: temporal only (1 request)
// Messages Screen: messaging only (1 request)
```

**Handle Offline Mode:**
```javascript
// Cache last successful response
localStorage.setItem('analytics_cache', JSON.stringify(data));

// On offline, show cached data with timestamp
if (!navigator.onLine) {
  const cached = JSON.parse(localStorage.getItem('analytics_cache'));
  showCachedData(cached, cached.timestamp);
}
```

### 7. **Security Best Practices**

**Token Management:**
```javascript
// Store token securely
// ✅ Mobile: Secure storage (Keychain/Keystore)
// ✅ Web: HttpOnly cookie or secure session storage

// ❌ Never store in localStorage (XSS vulnerable)
```

**Validate User Context:**
```javascript
// Ensure user can only see their own data
// Backend automatically filters by authenticated sponsor ID
// Frontend should not pass sponsorId in query params
```

### 8. **Data Freshness Indicators**

**Show Last Update Time:**
```javascript
const { dataStartDate, dataEndDate } = messaging.data;
displayTimestamp(`Data from ${dataStartDate} to ${dataEndDate}`);
```

**Pull-to-Refresh:**
```javascript
// Mobile apps should implement pull-to-refresh
// This bypasses cache and fetches fresh data
function onPullRefresh() {
  bypassCache = true;
  await fetchAllAnalytics();
}
```

### 9. **Chart/Visualization Best Practices**

**Temporal Chart:**
```javascript
// Use appropriate groupBy for timeframe
const timeframe = getTimeframeDays();
const groupBy = timeframe <= 30 ? 'Day' : timeframe <= 180 ? 'Week' : 'Month';
```

**ROI Gauge:**
```javascript
// Color code ROI status
const color = roi.overallROI >= 0 ? '#10B981' : '#EF4444'; // Green : Red
```

**Map Visualization:**
```javascript
// Show top cities on Turkey map
impact.topCities.forEach(city => {
  addMapMarker(city.cityName, city.analysisCount);
});
```

### 10. **Rate Limiting**

**Respect API Limits:**
```javascript
// Don't spam refresh
let lastFetch = 0;
const MIN_INTERVAL = 60000; // 1 minute

function canFetch() {
  const now = Date.now();
  if (now - lastFetch < MIN_INTERVAL) {
    return false;
  }
  lastFetch = now;
  return true;
}
```

---

## Integration Examples

### React Integration

```jsx
import { useState, useEffect } from 'react';

function SponsorDashboard() {
  const [analytics, setAnalytics] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadAnalytics() {
      try {
        const token = localStorage.getItem('authToken');
        
        const [messaging, impact, roi] = await Promise.all([
          fetch('/api/v1/sponsorship/messaging-analytics', {
            headers: {
              'Authorization': `Bearer ${token}`,
              'x-dev-arch-version': '1.0'
            }
          }).then(r => r.json()),
          
          fetch('/api/v1/sponsorship/impact-analytics', {
            headers: {
              'Authorization': `Bearer ${token}`,
              'x-dev-arch-version': '1.0'
            }
          }).then(r => r.json()),
          
          fetch('/api/v1/sponsorship/roi-analytics', {
            headers: {
              'Authorization': `Bearer ${token}`,
              'x-dev-arch-version': '1.0'
            }
          }).then(r => r.json())
        ]);
        
        setAnalytics({ messaging: messaging.data, impact: impact.data, roi: roi.data });
      } catch (error) {
        console.error('Failed to load analytics:', error);
      } finally {
        setLoading(false);
      }
    }
    
    loadAnalytics();
  }, []);

  if (loading) return <div>Loading...</div>;

  return (
    <div>
      <MetricsCard 
        unreadMessages={analytics.messaging.unreadMessagesCount}
        activeFarmers={analytics.impact.activeFarmersLast30Days}
        roi={analytics.roi.overallROI}
      />
      <ConversationsList 
        conversations={analytics.messaging.mostActiveConversations}
      />
    </div>
  );
}
```

### Flutter Integration

```dart
import 'package:http/http.dart' as http;
import 'dart:convert';

class SponsorAnalyticsService {
  final String baseUrl = 'https://api.ziraai.com';
  final String token;
  
  SponsorAnalyticsService(this.token);
  
  Future<Map<String, dynamic>> getMessagingAnalytics() async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/v1/sponsorship/messaging-analytics'),
      headers: {
        'Authorization': 'Bearer $token',
        'x-dev-arch-version': '1.0',
      },
    );
    
    if (response.statusCode == 200) {
      final json = jsonDecode(response.body);
      return json['data'];
    } else {
      throw Exception('Failed to load messaging analytics');
    }
  }
  
  Future<Map<String, dynamic>> getAllAnalytics() async {
    final results = await Future.wait([
      getMessagingAnalytics(),
      getImpactAnalytics(),
      getROIAnalytics(),
    ]);
    
    return {
      'messaging': results[0],
      'impact': results[1],
      'roi': results[2],
    };
  }
}
```

---

## API Versioning

### Current Version: 1.0

**Header:** `x-dev-arch-version: 1.0`

**Breaking Changes Policy:**
- Minor updates (bug fixes, new optional fields): Same version
- Major updates (breaking changes): New version (2.0)

**Deprecation Notice:**
- Deprecated fields marked with `[DEPRECATED]` in response
- Minimum 90 days notice before removal

---

## Support & Feedback

### Contact

- **Email:** api-support@ziraai.com
- **Documentation:** https://docs.ziraai.com/analytics
- **Status Page:** https://status.ziraai.com

### Reporting Issues

Include:
1. Endpoint URL
2. Request parameters
3. Expected vs actual response
4. Timestamp of request
5. User ID (sponsor ID)

---

## Changelog

### v1.0 (2025-01-25) - Initial Release

**New Endpoints:**
- `/api/v1/sponsorship/messaging-analytics` - Messaging metrics
- `/api/v1/sponsorship/impact-analytics` - Agricultural impact
- `/api/v1/sponsorship/temporal-analytics` - Time-series trends
- `/api/v1/sponsorship/roi-analytics` - Financial ROI

**Features:**
- Real-time messaging metrics with 15-min cache
- Geographic impact tracking with city-level granularity
- Dynamic time-series grouping (Day/Week/Month)
- Tier-based ROI analysis with breakeven tracking
- Privacy-compliant farmer data (tier-based anonymization)

---

**End of Documentation**

For implementation support or questions, please contact the ZiraAI development team.
