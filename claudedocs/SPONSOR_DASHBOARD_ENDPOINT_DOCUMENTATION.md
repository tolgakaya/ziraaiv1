# Sponsor Dashboard Summary Endpoint - Complete Documentation

**Version:** 1.0.0
**Created:** 2025-10-11
**Endpoint:** `/api/v1/sponsorship/dashboard-summary`
**Status:** ✅ Implemented & Tested

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Endpoint Details](#endpoint-details)
3. [Request/Response Specification](#requestresponse-specification)
4. [Data Models](#data-models)
5. [Field Definitions](#field-definitions)
6. [Usage Examples](#usage-examples)
7. [Mobile Implementation Guide](#mobile-implementation-guide)
8. [Performance Considerations](#performance-considerations)
9. [Troubleshooting](#troubleshooting)

---

## 🎯 Overview

### Purpose

The **Dashboard Summary Endpoint** provides a comprehensive, optimized API response for the sponsor mobile app home screen. It consolidates multiple statistics into a single request, reducing network calls and improving app performance.

### Key Features

- **Single API Call**: All dashboard metrics in one response
- **Tier-Based Breakdown**: Package statistics grouped by subscription tier (S, M, L, XL)
- **Analysis Tracking**: Total analyses performed using sponsored codes
- **Distribution Metrics**: Sent vs unsent codes with percentages
- **Overall Statistics**: SMS/WhatsApp distribution, redemption rates, average times

### Business Context

This endpoint supports the sponsor dashboard UI that displays:
1. **Top Row Cards**: Sent codes, total analyses, purchases count
2. **Active Packages Block**: Tier-based code distribution and usage

---

## 🔗 Endpoint Details

### HTTP Method & Path
```http
GET /api/v1/sponsorship/dashboard-summary
```

### Authorization
```
Required: Yes
Roles: Sponsor, Admin
Header: Authorization: Bearer {access_token}
```

### Query Parameters
None required. Sponsor ID is extracted from JWT token.

### Success Response
- **Status Code**: `200 OK`
- **Content-Type**: `application/json`

### Error Responses
- **401 Unauthorized**: Invalid or missing JWT token
- **403 Forbidden**: User lacks Sponsor/Admin role
- **500 Internal Server Error**: Server-side processing error

---

## 📨 Request/Response Specification

### Request Example

```http
GET /api/v1/sponsorship/dashboard-summary HTTP/1.1
Host: ziraai.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Accept: application/json
```

### Response Example (Success)

```json
{
  "success": true,
  "message": "Dashboard summary retrieved successfully",
  "data": {
    "totalCodesCount": 1000,
    "sentCodesCount": 120,
    "sentCodesPercentage": 12.0,
    "totalAnalysesCount": 45,
    "purchasesCount": 3,
    "totalSpent": 50000.00,
    "currency": "TRY",
    "activePackages": [
      {
        "tierName": "S",
        "tierDisplayName": "Small",
        "totalCodes": 100,
        "sentCodes": 60,
        "unsentCodes": 40,
        "usedCodes": 35,
        "unusedSentCodes": 25,
        "remainingCodes": 40,
        "usagePercentage": 58.33,
        "distributionPercentage": 60.0,
        "uniqueFarmers": 28,
        "analysesCount": 12
      },
      {
        "tierName": "M",
        "tierDisplayName": "Medium",
        "totalCodes": 100,
        "sentCodes": 80,
        "unsentCodes": 20,
        "usedCodes": 65,
        "unusedSentCodes": 15,
        "remainingCodes": 20,
        "usagePercentage": 81.25,
        "distributionPercentage": 80.0,
        "uniqueFarmers": 52,
        "analysesCount": 28
      },
      {
        "tierName": "L",
        "tierDisplayName": "Large",
        "totalCodes": 50,
        "sentCodes": 20,
        "unsentCodes": 30,
        "usedCodes": 15,
        "unusedSentCodes": 5,
        "remainingCodes": 30,
        "usagePercentage": 75.0,
        "distributionPercentage": 40.0,
        "uniqueFarmers": 12,
        "analysesCount": 5
      }
    ],
    "overallStats": {
      "smsDistributions": 45,
      "whatsAppDistributions": 75,
      "overallRedemptionRate": 91.67,
      "averageRedemptionTime": 2.3,
      "totalUniqueFarmers": 92,
      "lastPurchaseDate": "2025-10-05T14:30:00Z",
      "lastDistributionDate": "2025-10-10T09:15:00Z"
    }
  }
}
```

### Response Example (Error)

```json
{
  "success": false,
  "message": "Error fetching dashboard summary: Database connection timeout"
}
```

---

## 📊 Data Models

### SponsorDashboardSummaryDto

Root response object containing all dashboard metrics.

| Field | Type | Description |
|-------|------|-------------|
| `totalCodesCount` | integer | Total codes purchased |
| `sentCodesCount` | integer | Codes distributed to farmers |
| `sentCodesPercentage` | decimal | (sent / total) × 100 |
| `totalAnalysesCount` | integer | Analyses using sponsored subs |
| `purchasesCount` | integer | Number of bulk purchases |
| `totalSpent` | decimal | Total investment amount |
| `currency` | string | Currency code (TRY, USD, EUR) |
| `activePackages` | array | Tier-based package breakdown |
| `overallStats` | object | Overall statistics |

---

### ActivePackageSummary

Package statistics for each subscription tier.

| Field | Type | Description | Calculation |
|-------|------|-------------|-------------|
| `tierName` | string | Tier code (S/M/L/XL) | - |
| `tierDisplayName` | string | Tier full name | - |
| `totalCodes` | integer | Total codes for tier | - |
| `sentCodes` | integer | Distributed codes | `WHERE DistributionDate != null` |
| `unsentCodes` | integer | Not yet distributed | `WHERE DistributionDate == null` |
| `usedCodes` | integer | Redeemed codes | `WHERE IsUsed == true` |
| `unusedSentCodes` | integer | Sent but not redeemed | `sent AND NOT used` |
| `remainingCodes` | integer | Available to send | Same as `unsentCodes` |
| `usagePercentage` | decimal | Redemption rate | `(used / sent) × 100` |
| `distributionPercentage` | decimal | Send rate | `(sent / total) × 100` |
| `uniqueFarmers` | integer | Distinct farmers | `COUNT(DISTINCT UsedByUserId)` |
| `analysesCount` | integer | Analyses with this tier | From `PlantAnalyses` table |

---

### OverallStatistics

Aggregate statistics across all packages.

| Field | Type | Description |
|-------|------|-------------|
| `smsDistributions` | integer | Codes sent via SMS |
| `whatsAppDistributions` | integer | Codes sent via WhatsApp |
| `overallRedemptionRate` | decimal | (total used / total sent) × 100 |
| `averageRedemptionTime` | decimal | Average days from send to use |
| `totalUniqueFarmers` | integer | Distinct farmers across tiers |
| `lastPurchaseDate` | datetime? | Most recent purchase |
| `lastDistributionDate` | datetime? | Most recent distribution |

---

## 📖 Field Definitions

### Code States Explained

```
┌─────────────────────────────────────────────┐
│             CODE LIFECYCLE                   │
├─────────────────────────────────────────────┤
│                                              │
│  Created → Purchased (totalCodes)           │
│     │                                        │
│     ├──→ Sent (sentCodes)                   │
│     │      │                                 │
│     │      ├──→ Used (usedCodes)            │
│     │      │                                 │
│     │      └──→ Unused Sent (unusedSentCodes)│
│     │                                        │
│     └──→ Unsent (unsentCodes/remainingCodes)│
│                                              │
└─────────────────────────────────────────────┘
```

### Key Metrics

**1. Total Codes Count**
- All codes purchased across all packages
- Source: `SponsorshipCodes` table
- Filter: `SponsorId == current_user`

**2. Sent Codes Count**
- Codes distributed to farmers
- Condition: `DistributionDate IS NOT NULL`
- Includes: SMS, WhatsApp, Email distributions

**3. Sent Codes Percentage**
- Formula: `(SentCodes / TotalCodes) × 100`
- Shows distribution progress
- Range: 0-100%

**4. Total Analyses Count**
- Plant analyses using sponsored subscriptions
- Join: `PlantAnalyses.ActiveSponsorshipId → UserSubscriptions.Id → SponsorshipCodes.CreatedSubscriptionId`
- Counts all analyses by farmers with sponsor's codes

**5. Purchases Count**
- Number of bulk subscription purchases
- Source: `SponsorshipPurchases` table
- Each purchase generates multiple codes

### Tier-Specific Calculations

**Usage Percentage** (Per Tier):
```
IF sentCodes > 0 THEN
  (usedCodes / sentCodes) × 100
ELSE
  0
END
```

**Distribution Percentage** (Per Tier):
```
IF totalCodes > 0 THEN
  (sentCodes / totalCodes) × 100
ELSE
  0
END
```

---

## 💻 Usage Examples

### Example 1: Basic Dashboard Load

**Scenario**: Load dashboard when sponsor opens mobile app

```dart
// Flutter Example
Future<DashboardData> loadDashboard() async {
  final response = await http.get(
    Uri.parse('https://ziraai.com/api/v1/sponsorship/dashboard-summary'),
    headers: {
      'Authorization': 'Bearer $accessToken',
      'Accept': 'application/json',
    },
  );

  if (response.statusCode == 200) {
    final json = jsonDecode(response.body);
    return DashboardData.fromJson(json['data']);
  } else {
    throw Exception('Failed to load dashboard');
  }
}
```

### Example 2: Display Top Cards

```dart
// Top row cards from response
Widget buildTopCards(DashboardSummary summary) {
  return Row(
    mainAxisAlignment: MainAxisAlignment.spaceEvenly,
    children: [
      // Card 1: Sent Codes
      DashboardCard(
        title: 'Sent Codes',
        value: '${summary.sentCodesCount}/${summary.totalCodesCount}',
        percentage: summary.sentCodesPercentage,
        color: Colors.blue,
      ),

      // Card 2: Analyses
      DashboardCard(
        title: 'Analyses',
        value: '${summary.totalAnalysesCount}',
        icon: Icons.analytics,
        color: Colors.orange,
      ),

      // Card 3: Purchases
      DashboardCard(
        title: 'Purchases',
        value: '${summary.purchasesCount}',
        icon: Icons.shopping_bag,
        color: Colors.green,
      ),
    ],
  );
}
```

### Example 3: Active Packages List

```dart
// Active packages breakdown
Widget buildActivePackages(List<ActivePackageSummary> packages) {
  return ListView.builder(
    itemCount: packages.length,
    itemBuilder: (context, index) {
      final pkg = packages[index];

      return Card(
        child: Column(
          children: [
            Text('${pkg.tierDisplayName} Package'),

            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('${pkg.sentCodes} sent'),
                Text('${pkg.unusedSentCodes} unused'),
              ],
            ),

            Text('Remaining: ${pkg.remainingCodes}'),

            LinearProgressIndicator(
              value: pkg.distributionPercentage / 100,
            ),
          ],
        ),
      );
    },
  );
}
```

### Example 4: Overall Statistics Display

```dart
Widget buildOverallStats(OverallStatistics stats) {
  return Column(
    children: [
      StatsRow(
        label: 'Total Farmers',
        value: '${stats.totalUniqueFarmers}',
      ),
      StatsRow(
        label: 'Redemption Rate',
        value: '${stats.overallRedemptionRate.toStringAsFixed(1)}%',
      ),
      StatsRow(
        label: 'Avg. Redemption Time',
        value: '${stats.averageRedemptionTime.toStringAsFixed(1)} days',
      ),
      StatsRow(
        label: 'SMS vs WhatsApp',
        value: '${stats.smsDistributions} / ${stats.whatsAppDistributions}',
      ),
    ],
  );
}
```

---

## 📱 Mobile Implementation Guide

### Step 1: Create Data Models

```dart
class SponsorDashboardSummary {
  final int totalCodesCount;
  final int sentCodesCount;
  final double sentCodesPercentage;
  final int totalAnalysesCount;
  final int purchasesCount;
  final double totalSpent;
  final String currency;
  final List<ActivePackageSummary> activePackages;
  final OverallStatistics overallStats;

  SponsorDashboardSummary({
    required this.totalCodesCount,
    required this.sentCodesCount,
    required this.sentCodesPercentage,
    required this.totalAnalysesCount,
    required this.purchasesCount,
    required this.totalSpent,
    required this.currency,
    required this.activePackages,
    required this.overallStats,
  });

  factory SponsorDashboardSummary.fromJson(Map<String, dynamic> json) {
    return SponsorDashboardSummary(
      totalCodesCount: json['totalCodesCount'],
      sentCodesCount: json['sentCodesCount'],
      sentCodesPercentage: json['sentCodesPercentage'].toDouble(),
      totalAnalysesCount: json['totalAnalysesCount'],
      purchasesCount: json['purchasesCount'],
      totalSpent: json['totalSpent'].toDouble(),
      currency: json['currency'],
      activePackages: (json['activePackages'] as List)
          .map((p) => ActivePackageSummary.fromJson(p))
          .toList(),
      overallStats: OverallStatistics.fromJson(json['overallStats']),
    );
  }
}

class ActivePackageSummary {
  final String tierName;
  final String tierDisplayName;
  final int totalCodes;
  final int sentCodes;
  final int unsentCodes;
  final int usedCodes;
  final int unusedSentCodes;
  final int remainingCodes;
  final double usagePercentage;
  final double distributionPercentage;
  final int uniqueFarmers;
  final int analysesCount;

  ActivePackageSummary({
    required this.tierName,
    required this.tierDisplayName,
    required this.totalCodes,
    required this.sentCodes,
    required this.unsentCodes,
    required this.usedCodes,
    required this.unusedSentCodes,
    required this.remainingCodes,
    required this.usagePercentage,
    required this.distributionPercentage,
    required this.uniqueFarmers,
    required this.analysesCount,
  });

  factory ActivePackageSummary.fromJson(Map<String, dynamic> json) {
    return ActivePackageSummary(
      tierName: json['tierName'],
      tierDisplayName: json['tierDisplayName'],
      totalCodes: json['totalCodes'],
      sentCodes: json['sentCodes'],
      unsentCodes: json['unsentCodes'],
      usedCodes: json['usedCodes'],
      unusedSentCodes: json['unusedSentCodes'],
      remainingCodes: json['remainingCodes'],
      usagePercentage: json['usagePercentage'].toDouble(),
      distributionPercentage: json['distributionPercentage'].toDouble(),
      uniqueFarmers: json['uniqueFarmers'],
      analysesCount: json['analysesCount'],
    );
  }
}
```

### Step 2: Create API Service

```dart
class SponsorshipApiService {
  final String baseUrl;
  final String accessToken;

  SponsorshipApiService({
    required this.baseUrl,
    required this.accessToken,
  });

  Future<SponsorDashboardSummary> getDashboardSummary() async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/v1/sponsorship/dashboard-summary'),
      headers: {
        'Authorization': 'Bearer $accessToken',
        'Accept': 'application/json',
      },
    );

    if (response.statusCode == 200) {
      final json = jsonDecode(response.body);

      if (json['success'] == true) {
        return SponsorDashboardSummary.fromJson(json['data']);
      } else {
        throw Exception(json['message'] ?? 'Unknown error');
      }
    } else if (response.statusCode == 401) {
      throw UnauthorizedException('Invalid or expired token');
    } else {
      throw Exception('HTTP ${response.statusCode}: ${response.body}');
    }
  }
}
```

### Step 3: State Management (Provider Example)

```dart
class DashboardProvider extends ChangeNotifier {
  final SponsorshipApiService _apiService;

  SponsorDashboardSummary? _summary;
  bool _isLoading = false;
  String? _error;

  SponsorDashboardSummary? get summary => _summary;
  bool get isLoading => _isLoading;
  String? get error => _error;

  DashboardProvider(this._apiService);

  Future<void> loadDashboard() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _summary = await _apiService.getDashboardSummary();
      _isLoading = false;
      notifyListeners();
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> refresh() async {
    await loadDashboard();
  }
}
```

### Step 4: UI Implementation

```dart
class SponsorDashboardScreen extends StatefulWidget {
  @override
  _SponsorDashboardScreenState createState() => _SponsorDashboardScreenState();
}

class _SponsorDashboardScreenState extends State<SponsorDashboardScreen> {
  @override
  void initState() {
    super.initState();
    // Load dashboard on screen open
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<DashboardProvider>().loadDashboard();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Sponsor Dashboard')),
      body: Consumer<DashboardProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return Center(child: CircularProgressIndicator());
          }

          if (provider.error != null) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text('Error: ${provider.error}'),
                  ElevatedButton(
                    onPressed: () => provider.refresh(),
                    child: Text('Retry'),
                  ),
                ],
              ),
            );
          }

          if (provider.summary == null) {
            return Center(child: Text('No data available'));
          }

          return RefreshIndicator(
            onRefresh: () => provider.refresh(),
            child: SingleChildScrollView(
              child: Column(
                children: [
                  buildTopCards(provider.summary!),
                  SizedBox(height: 20),
                  buildActivePackages(provider.summary!.activePackages),
                  SizedBox(height: 20),
                  buildOverallStats(provider.summary!.overallStats),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}
```

---

## ⚡ Performance Considerations

### Response Time

- **Target**: <500ms average response time
- **Database Queries**: Optimized with indexes on `SponsorId`, `DistributionDate`, `IsUsed`
- **Caching**: Not recommended due to real-time nature of metrics

### Optimization Tips

1. **Database Indexes**:
   ```sql
   CREATE INDEX IX_SponsorshipCode_SponsorId ON SponsorshipCodes(SponsorId);
   CREATE INDEX IX_SponsorshipCode_DistributionDate ON SponsorshipCodes(DistributionDate);
   CREATE INDEX IX_SponsorshipCode_IsUsed ON SponsorshipCodes(IsUsed);
   CREATE INDEX IX_PlantAnalysis_ActiveSponsorshipId ON PlantAnalyses(ActiveSponsorshipId);
   ```

2. **Mobile Caching**:
   - Cache dashboard for 5 minutes
   - Use pull-to-refresh for manual updates
   - Show stale data while refreshing

3. **Lazy Loading**:
   - Load top cards first (priority)
   - Load active packages second
   - Load overall stats last

### Payload Size

- Typical Response: ~2-5 KB (depending on package count)
- Maximum Response: ~10 KB (for sponsors with many packages)
- Compression: Enable gzip/deflate for 60-70% reduction

---

## 🔧 Troubleshooting

### Common Issues

#### 1. 401 Unauthorized

**Problem**: Token missing or invalid

**Solution**:
```dart
// Check token before API call
if (accessToken == null || accessToken.isEmpty) {
  // Redirect to login
  Navigator.pushReplacementNamed(context, '/login');
}
```

#### 2. Empty activePackages Array

**Problem**: Sponsor hasn't purchased any packages

**Solution**:
```dart
if (summary.activePackages.isEmpty) {
  return Center(
    child: Text('No packages purchased yet. Purchase your first package!'),
  );
}
```

#### 3. Zero Analyses Count

**Problem**: No analyses performed using sponsored codes

**Reason**:
- Codes distributed but not redeemed yet
- Codes redeemed but no analyses run
- `ActiveSponsorshipId` not set on analyses

**Check**:
```sql
-- Verify analyses are linked to sponsorships
SELECT COUNT(*)
FROM PlantAnalyses
WHERE ActiveSponsorshipId IN (
  SELECT Id FROM UserSubscriptions WHERE SponsorshipCodeId IS NOT NULL
);
```

#### 4. Slow Response Time (>2s)

**Diagnostics**:
```sql
-- Check table sizes
SELECT
  'SponsorshipCodes' as TableName,
  COUNT(*) as RowCount
FROM SponsorshipCodes
UNION ALL
SELECT 'PlantAnalyses', COUNT(*) FROM PlantAnalyses;

-- Check missing indexes
SELECT * FROM pg_indexes WHERE tablename = 'sponsorshipcodes';
```

**Solution**: Add indexes, optimize queries, consider pagination for large datasets

---

## 📚 Related Documentation

- [Sponsorship System Complete Documentation](./SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md)
- [Sponsorship Analytics Endpoints](./session_2025_10_10_sponsorship_complete.md)
- [Mobile Integration Guide](./mobile-integration-guide.md)
- [API Reference - Postman Collection](../ZiraAI_Complete_API_Collection_v6.1.json)

---

## 📝 Changelog

### Version 1.0.0 (2025-10-11)
- ✅ Initial implementation
- ✅ Complete DTO structure with all fields
- ✅ Optimized query handler with tier breakdowns
- ✅ Controller endpoint with logging
- ✅ Comprehensive documentation with examples
- ✅ Mobile implementation guide (Flutter)

---

## 🤝 Support

**Implementation Location:**
- DTO: `Entities/Dtos/SponsorDashboardSummaryDto.cs`
- Query: `Business/Handlers/Sponsorship/Queries/GetSponsorDashboardSummaryQuery.cs`
- Controller: `WebAPI/Controllers/SponsorshipController.cs:295-338`

**For Issues:**
- Check logs: `[Dashboard]` prefix in application logs
- Verify JWT token validity
- Confirm user has Sponsor role
- Test with Postman collection

---

**End of Documentation**

*Last Updated: 2025-10-11 by Claude Code*
*Document Version: 1.0.0*
*Endpoint Status: ✅ Production Ready*
