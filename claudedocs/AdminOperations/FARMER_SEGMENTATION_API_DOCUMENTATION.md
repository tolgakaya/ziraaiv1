# Farmer Segmentation Analytics API - Complete Documentation

**Version:** 1.0
**Date:** 2025-11-12
**Branch:** `feature/sponsor-advanced-analytics`
**Target Audience:** Frontend & Mobile Development Teams

---

## Overview

Farmer Segmentation API provides behavioral analysis of farmers, categorizing them into 4 actionable segments:
- **Heavy Users** - High engagement, frequent analyses
- **Regular Users** - Consistent usage patterns
- **At-Risk Users** - Declining engagement, may churn
- **Dormant Users** - Inactive, need re-engagement

---

## Endpoint Details

### Get Farmer Segmentation

Retrieve behavioral segmentation analytics for the current sponsor's farmers.

#### HTTP Request

```http
GET /api/v1/sponsorship/farmer-segmentation
```

#### Authorization

**Required Roles:** `Sponsor` OR `Admin`

**Header:**
```http
Authorization: Bearer {access_token}
```

#### Request Parameters

**No parameters required.**

- For **Sponsor users**: Automatically filters to their farmers only
- For **Admin users**: Returns segmentation for ALL farmers across all sponsors

#### Response Status Codes

| Code | Description |
|------|-------------|
| `200 OK` | Segmentation data retrieved successfully |
| `401 Unauthorized` | Missing or invalid authentication token |
| `403 Forbidden` | User lacks required role (not Sponsor or Admin) |
| `500 Internal Server Error` | Server-side error occurred |

---

## Response Structure

### Success Response (200 OK)

```json
{
  "data": {
    "sponsorId": 159,
    "totalFarmers": 127,
    "segments": [
      {
        "segmentName": "Heavy Users",
        "farmerCount": 13,
        "percentage": 10.24,
        "characteristics": {
          "avgAnalysesPerMonth": 8.2,
          "avgDaysSinceLastAnalysis": 3,
          "medianDaysSinceLastAnalysis": 2,
          "mostCommonTier": "L",
          "activeSubscriptionRate": 100.0,
          "avgEngagementScore": 95.3,
          "topCrop": "Domates",
          "topDisease": "Mildiyö"
        },
        "farmerAvatar": {
          "profile": "Aktif çiftçi, ayda 8 kez analiz yapıyor, öncelikle Domates yetiştiriyor",
          "behaviorPattern": "Sık analiz yapar, genellikle son kontrolden bir hafta içinde tekrar gelir. Yüksek platform etkileşimi.",
          "painPoints": "Mildiyö ile tekrarlayan sorunlar yaşıyor. Proaktif önleme stratejilerine ihtiyacı var.",
          "engagementStyle": "Tüm mesajları okur, hızlı yanıt verir, önerileri aktif kullanır."
        },
        "farmerIds": [1234, 1235, 1236, 1237, 1238, 1239, 1240, 1241, 1242, 1243, 1244, 1245, 1246],
        "recommendedActions": [
          "Sadakati özel ipuçları veya öncelikli destek ile ödüllendirin",
          "Premium abonelik yükseltmesi sunun",
          "Referans ve vaka çalışmaları isteyin",
          "Yeni özellikleri beta test etmeye davet edin"
        ]
      },
      {
        "segmentName": "Regular Users",
        "farmerCount": 54,
        "percentage": 42.52,
        "characteristics": {
          "avgAnalysesPerMonth": 3.8,
          "avgDaysSinceLastAnalysis": 12,
          "medianDaysSinceLastAnalysis": 10,
          "mostCommonTier": "M",
          "activeSubscriptionRate": 87.0,
          "avgEngagementScore": 68.5,
          "topCrop": "Biber",
          "topDisease": "Bakteriyel Solgunluk"
        },
        "farmerAvatar": {
          "profile": "Tutarlı çiftçi, ayda 4 kez analiz yapıyor, Biber yetiştiriyor",
          "behaviorPattern": "İstikrarlı analiz paterni, yetiştirme sezonunda düzenli kontrol eder.",
          "painPoints": "Ara sıra Bakteriyel Solgunluk ile karşılaşır. Mevsimsel tavsiyelere ihtiyaç duyabilir.",
          "engagementStyle": "Çoğu mesajı okur, ürün linklerine tıklar, bazen takip soruları sorar."
        },
        "farmerIds": [1300, 1301, 1302, ...],
        "recommendedActions": [
          "Mevsimsel tarım ipuçları ve en iyi uygulamalar gönderin",
          "Ürün/hastalık paternlerine göre ilgili ürünleri tanıtın",
          "Platformu diğer çiftçilerle paylaşmayı teşvik edin",
          "Tier yükseltme teşvikleri sunun"
        ]
      },
      {
        "segmentName": "At-Risk Users",
        "farmerCount": 38,
        "percentage": 29.92,
        "characteristics": {
          "avgAnalysesPerMonth": 1.5,
          "avgDaysSinceLastAnalysis": 42,
          "medianDaysSinceLastAnalysis": 38,
          "mostCommonTier": "S",
          "activeSubscriptionRate": 45.0,
          "avgEngagementScore": 32.1,
          "topCrop": "Patlıcan",
          "topDisease": "Gri Küf"
        },
        "farmerAvatar": {
          "profile": "Azalan etkileşim, son analizden 42 gün geçti, Patlıcan yetiştiriyor",
          "behaviorPattern": "Kullanım düşüyor, sorun yaşıyor veya alternatif çözümler bulmuş olabilir.",
          "painPoints": "Platform yeterli değer sağlamıyor hissedebilir veya kullanılabilirlik zorlukları yaşıyor.",
          "engagementStyle": "Nadiren mesajları açar, düşük tıklama oranı, minimal etkileşim."
        },
        "farmerIds": [1400, 1401, 1402, ...],
        "recommendedActions": [
          "Değer teklifi ile yeniden etkileşim mesajı gönderin",
          "Abonelik yenileme için sınırlı süreli indirim sunun",
          "Ürün geçmişine göre kişiselleştirilmiş ipuçları sağlayın",
          "Sürekli kullanımdaki engelleri anlamak için anket yapın"
        ]
      },
      {
        "segmentName": "Dormant Users",
        "farmerCount": 22,
        "percentage": 17.32,
        "characteristics": {
          "avgAnalysesPerMonth": 0.3,
          "avgDaysSinceLastAnalysis": 87,
          "medianDaysSinceLastAnalysis": 82,
          "mostCommonTier": "Trial",
          "activeSubscriptionRate": 0.0,
          "avgEngagementScore": 8.2,
          "topCrop": "Salatalık",
          "topDisease": "Külleme"
        },
        "farmerAvatar": {
          "profile": "Aktif olmayan çiftçi, son analizden 87 gün geçti. Önceki ürün: Salatalık",
          "behaviorPattern": "Son zamanlarda aktivite yok. Platformu terk etmiş veya teknik sorunlar yaşıyor olabilir.",
          "painPoints": "İlgiyi kaybetti, alternatif çözüm buldu veya aboneliği yenileme olmadan sona erdi.",
          "engagementStyle": "Mesajlar veya içerikle etkileşim yok. Muhtemelen yeni özelliklerden habersiz."
        },
        "farmerIds": [1500, 1501, 1502, ...],
        "recommendedActions": [
          "Özel teklifle geri kazanma kampanyası başlatın",
          "Son kullanımdan bu yana yeni özellikleri ve iyileştirmeleri vurgulayın",
          "Churn nedenlerini anlamak için anket yapın",
          "Platform faydalarını SMS hatırlatıcısı ile bildirin"
        ]
      }
    ],
    "generatedAt": "2025-11-12T14:30:00Z"
  },
  "success": true,
  "message": "Farmer segmentation computed successfully"
}
```

### Error Response (401 Unauthorized)

```json
{
  "success": false,
  "message": "Unauthorized access"
}
```

### Error Response (403 Forbidden)

```json
{
  "success": false,
  "message": "User lacks required permissions"
}
```

### Error Response (500 Internal Server Error)

```json
{
  "success": false,
  "message": "Error computing farmer segmentation: [error details]"
}
```

---

## Response Field Descriptions

### Root Level

| Field | Type | Description |
|-------|------|-------------|
| `data` | Object | Main response data container |
| `success` | Boolean | Indicates if request was successful |
| `message` | String | Human-readable status message |

### Data Object

| Field | Type | Description |
|-------|------|-------------|
| `sponsorId` | Integer (nullable) | Sponsor ID for filtered data (null for admin all-farmers view) |
| `totalFarmers` | Integer | Total number of farmers analyzed |
| `segments` | Array | List of behavioral segments |
| `generatedAt` | DateTime (ISO 8601) | Timestamp when segmentation was computed |

### Segment Object

| Field | Type | Description |
|-------|------|-------------|
| `segmentName` | String | Segment name: "Heavy Users", "Regular Users", "At-Risk Users", "Dormant Users" |
| `farmerCount` | Integer | Number of farmers in this segment |
| `percentage` | Decimal | Percentage of total farmers (0-100) |
| `characteristics` | Object | Statistical characteristics of segment |
| `farmerAvatar` | Object | Typical farmer profile for this segment |
| `farmerIds` | Array[Integer] | List of farmer user IDs in this segment (for targeted campaigns) |
| `recommendedActions` | Array[String] | Actionable recommendations for engaging this segment |

### Characteristics Object

| Field | Type | Description |
|-------|------|-------------|
| `avgAnalysesPerMonth` | Decimal | Average number of analyses per month |
| `avgDaysSinceLastAnalysis` | Integer | Average days since last analysis |
| `medianDaysSinceLastAnalysis` | Integer | Median days since last analysis |
| `mostCommonTier` | String | Most common subscription tier: "Trial", "S", "M", "L", "XL" |
| `activeSubscriptionRate` | Decimal | Percentage with active subscriptions (0-100) |
| `avgEngagementScore` | Decimal | Average engagement score (0-100 scale) |
| `topCrop` | String | Most common crop analyzed by this segment |
| `topDisease` | String | Most common disease encountered by this segment |

### FarmerAvatar Object

| Field | Type | Description |
|-------|------|-------------|
| `profile` | String | Descriptive profile of typical farmer in segment |
| `behaviorPattern` | String | Typical behavioral patterns observed |
| `painPoints` | String | Common pain points or challenges faced |
| `engagementStyle` | String | How they typically engage with platform content |

---

## Segmentation Algorithm

The API uses behavioral analysis to categorize farmers:

### Heavy Users
**Criteria:**
- `avgAnalysesPerMonth >= 6`
- `daysSinceLastAnalysis <= 7`

**Expected Distribution:** 10-15% of farmers

### Regular Users
**Criteria:**
- `avgAnalysesPerMonth >= 2`
- `daysSinceLastAnalysis <= 30`
- NOT classified as Heavy User

**Expected Distribution:** 40-50% of farmers

### At-Risk Users
**Criteria:**
- `avgAnalysesPerMonth >= 1`
- `daysSinceLastAnalysis BETWEEN 31-60 days`

**Expected Distribution:** 10-20% of farmers

### Dormant Users
**Criteria:**
- `daysSinceLastAnalysis > 60 days`
- OR `subscriptionExpired = true`

**Expected Distribution:** 5-10% of farmers

---

## Engagement Score Calculation

The engagement score (0-100) is calculated from three weighted factors:

### Frequency Component (40 points max)
```
score += min(40, analysesPerMonth * 4)
```

### Recency Component (30 points max)
```
if daysSinceLastAnalysis <= 7:  score += 30
if daysSinceLastAnalysis <= 14: score += 25
if daysSinceLastAnalysis <= 30: score += 15
if daysSinceLastAnalysis <= 60: score += 5
```

### Subscription Component (30 points max)
```
if hasActiveSubscription:      score += 30
elif !subscriptionExpired:     score += 15
```

---

## Caching Behavior

**Cache Duration:** 6 hours (360 minutes)

**Cache Strategy:**
- First request: Computes segmentation (200-500ms response time)
- Subsequent requests within 6 hours: Returns cached data (<10ms response time)
- After 6 hours: Cache expires, next request recomputes

**Cache Invalidation:**
- Automatic after 6 hours
- No manual invalidation endpoint currently

---

## Use Cases & Integration Examples

### 1. Display Segment Overview Dashboard

**Frontend Component: Segment Cards**

```typescript
// TypeScript/React Example
interface SegmentCardProps {
  segment: {
    segmentName: string;
    farmerCount: number;
    percentage: number;
    characteristics: {
      avgEngagementScore: number;
      topCrop: string;
    };
  };
}

const SegmentCard: React.FC<SegmentCardProps> = ({ segment }) => {
  const getSegmentColor = (name: string) => {
    switch (name) {
      case 'Heavy Users': return '#10b981'; // Green
      case 'Regular Users': return '#3b82f6'; // Blue
      case 'At-Risk Users': return '#f59e0b'; // Orange
      case 'Dormant Users': return '#ef4444'; // Red
    }
  };

  return (
    <div style={{ borderLeft: `4px solid ${getSegmentColor(segment.segmentName)}` }}>
      <h3>{segment.segmentName}</h3>
      <p>{segment.farmerCount} çiftçi ({segment.percentage.toFixed(1)}%)</p>
      <p>Engagement: {segment.characteristics.avgEngagementScore.toFixed(0)}/100</p>
      <p>Top Crop: {segment.characteristics.topCrop}</p>
    </div>
  );
};
```

### 2. Targeted Messaging Campaign

**Mobile App: Send Segment-Specific Messages**

```dart
// Flutter/Dart Example
Future<void> sendSegmentMessage(String segmentName, List<int> farmerIds) async {
  final messages = {
    'Heavy Users': 'Sadık müşterimizsiniz! Premium özelliklerimizi keşfedin.',
    'Regular Users': 'Yeni sezon ipuçlarımızı kaçırmayın!',
    'At-Risk Users': 'Size özel %20 indirim! Geri dönün.',
    'Dormant Users': 'Sizi özledik! Yeni özelliklerimize göz atın.'
  };

  final message = messages[segmentName] ?? 'Merhaba!';

  await apiService.sendBulkSMS(
    farmerIds: farmerIds,
    message: message,
    channel: 'SMS'
  );
}
```

### 3. Segment Trend Visualization

**Web Dashboard: Chart.js Integration**

```javascript
// JavaScript/Chart.js Example
async function renderSegmentChart() {
  const response = await fetch('/api/v1/sponsorship/farmer-segmentation', {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const data = await response.json();
  const segments = data.data.segments;

  new Chart(document.getElementById('segmentChart'), {
    type: 'doughnut',
    data: {
      labels: segments.map(s => s.segmentName),
      datasets: [{
        data: segments.map(s => s.farmerCount),
        backgroundColor: ['#10b981', '#3b82f6', '#f59e0b', '#ef4444']
      }]
    },
    options: {
      plugins: {
        legend: { position: 'bottom' },
        title: { display: true, text: 'Farmer Segmentation Distribution' }
      }
    }
  });
}
```

### 4. Export Farmer List for CRM

**Backend Integration: Export to CSV**

```javascript
// Node.js/Express Example
app.get('/api/export-segment/:segmentName', async (req, res) => {
  const response = await axios.get('https://api.ziraai.com/api/v1/sponsorship/farmer-segmentation', {
    headers: { 'Authorization': req.headers.authorization }
  });

  const segment = response.data.data.segments.find(
    s => s.segmentName === req.params.segmentName
  );

  if (!segment) {
    return res.status(404).json({ error: 'Segment not found' });
  }

  // Export farmer IDs to CSV for CRM integration
  const csv = segment.farmerIds.map(id => `${id}\n`).join('');

  res.setHeader('Content-Type', 'text/csv');
  res.setHeader('Content-Disposition', `attachment; filename="${req.params.segmentName}.csv"`);
  res.send(csv);
});
```

---

## Performance Considerations

### Response Time
- **Cached:** <10ms
- **Uncached (small dataset <100 farmers):** 200-300ms
- **Uncached (medium dataset 100-500 farmers):** 300-500ms
- **Uncached (large dataset 500+ farmers):** 500-1000ms

### Rate Limiting
- No specific rate limit for this endpoint
- Standard API rate limits apply (100 requests/minute per user)

### Best Practices
1. **Cache Result:** Store response in frontend state for duration of user session
2. **Refresh Strategy:** Refresh data once per day or on user manual refresh
3. **Progressive Loading:** Show skeleton while loading, then populate with data
4. **Error Handling:** Gracefully handle 500 errors with retry mechanism

---

## Testing Guide

### Postman Collection

```json
{
  "name": "Farmer Segmentation API",
  "requests": [
    {
      "name": "Get Farmer Segmentation (Sponsor)",
      "method": "GET",
      "url": "{{baseUrl}}/api/v1/sponsorship/farmer-segmentation",
      "headers": [
        {
          "key": "Authorization",
          "value": "Bearer {{sponsor_token}}"
        }
      ],
      "tests": [
        "pm.test('Status is 200', () => pm.response.to.have.status(200));",
        "pm.test('Has segments array', () => pm.expect(pm.response.json().data.segments).to.be.an('array'));"
      ]
    },
    {
      "name": "Get Farmer Segmentation (Admin)",
      "method": "GET",
      "url": "{{baseUrl}}/api/v1/sponsorship/farmer-segmentation",
      "headers": [
        {
          "key": "Authorization",
          "value": "Bearer {{admin_token}}"
        }
      ]
    }
  ]
}
```

### cURL Examples

**Test as Sponsor:**
```bash
curl -X GET "https://ziraai.com/api/v1/sponsorship/farmer-segmentation" \
  -H "Authorization: Bearer YOUR_SPONSOR_TOKEN" \
  -H "Content-Type: application/json"
```

**Test as Admin:**
```bash
curl -X GET "https://ziraai.com/api/v1/sponsorship/farmer-segmentation" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json"
```

---

## Migration Requirements

**⚠️ IMPORTANT:** Before using this endpoint, database migration must be executed.

### SQL Migration File
`claudedocs/AdminOperations/005_farmer_segmentation_operation_claim.sql`

### Migration Steps
1. Run SQL script on staging database
2. Verify OperationClaim created: `GetFarmerSegmentationQuery`
3. Verify GroupClaims assigned to Administrators (GroupId=1) and Sponsor (GroupId=3)
4. Admins and sponsors must logout/login to refresh claims cache

### Verification Query
```sql
SELECT oc."Name", g."GroupName"
FROM public."OperationClaims" oc
JOIN public."GroupClaims" gc ON oc."Id" = gc."ClaimId"
JOIN public."Group" g ON gc."GroupId" = g."Id"
WHERE oc."Name" = 'GetFarmerSegmentationQuery';
```

**Expected Result:**
```
Name                          | GroupName
------------------------------|-------------
GetFarmerSegmentationQuery    | Administrators
GetFarmerSegmentationQuery    | Sponsor
```

---

## FAQ

### Q: Why is sponsorId null in admin response?
**A:** When admin users access the endpoint, they see ALL farmers across all sponsors. The `sponsorId` field is `null` to indicate it's an aggregate view, not filtered to a specific sponsor.

### Q: Can I filter by specific segment?
**A:** No, the endpoint returns all segments. Filter client-side based on `segmentName` field.

### Q: How often should I refresh this data?
**A:** Data is cached for 6 hours. Refresh once per day or on user manual refresh is sufficient.

### Q: What if a farmer has no analyses?
**A:** Farmers with zero analyses are not included in segmentation. Only farmers with at least one analysis appear.

### Q: Can I get historical segmentation data?
**A:** No, this endpoint only provides current snapshot. Historical trending is planned for future release.

### Q: How do I send messages to a specific segment?
**A:** Use the `farmerIds` array from segment response with existing messaging endpoints (`/api/v1/sponsorship/send-link` or messaging system).

---

## Support & Feedback

**Backend Team:** backend@ziraai.com
**API Documentation:** https://ziraai.com/swagger
**Slack Channel:** #backend-support

For integration issues or questions, contact backend team via Slack.

---

## Changelog

### Version 1.0 (2025-11-12)
- Initial release
- 4 segment types: Heavy, Regular, At-Risk, Dormant
- 6-hour caching
- Support for Sponsor and Admin roles
