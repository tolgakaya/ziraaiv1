# Farmer Journey Analytics API Documentation

## Overview
The Farmer Journey Analytics endpoint provides comprehensive lifecycle analytics for individual farmers, tracking their complete journey from code redemption through ongoing engagement. This endpoint is designed for both sponsors (to view their farmers) and admins (to view all farmers).

**Feature**: Sponsor Advanced Analytics - Priority 4
**Implementation Date**: 2025-11-12
**Operation Claim ID**: 165
**Operation Claim Name**: `GetFarmerJourneyQuery`

---

## Endpoint Details

### GET /api/sponsorship/farmer-journey

**Purpose**: Retrieve complete journey analytics for a specific farmer including timeline events, behavioral patterns, and AI-driven recommendations.

**Authorization**: `[Authorize(Roles = "Sponsor,Admin")]`

**Caching**: 1 hour (60 minutes) TTL via Redis

---

## Request

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `farmerId` | int | Yes | The user ID of the farmer to analyze |

### Example Request

```http
GET /api/sponsorship/farmer-journey?farmerId=123
Authorization: Bearer {jwt_token}
```

### Authorization Rules

**Sponsor Access**:
- Sponsors can only view farmers they have relationships with (farmers who redeemed their codes)
- Backend automatically filters to `RequestingSponsorId = {sponsorUserId}`

**Admin Access**:
- Admins can view ALL farmers system-wide
- Backend sets `RequestingSponsorId = null` for admin requests

---

## Response

### Success Response (200 OK)

```json
{
  "success": true,
  "message": "Farmer journey analytics retrieved successfully",
  "data": {
    "farmerId": 123,
    "farmerName": "John Doe",
    "journeySummary": {
      "firstCodeRedemption": "2025-01-15T10:30:00Z",
      "totalDaysAsCustomer": 90,
      "totalAnalyses": 45,
      "totalSpent": 0.00,
      "totalValueGenerated": 2250.00,
      "currentTier": "M",
      "lifecycleStage": "Active",
      "nextRenewalDate": "2025-04-15T10:30:00Z",
      "daysUntilRenewal": 15
    },
    "timeline": [
      {
        "date": "2025-01-15T10:30:00Z",
        "eventType": "Code Redeemed",
        "details": "Code AGRI-2025-X3K9 activated",
        "cropType": null,
        "disease": null,
        "severity": null,
        "tier": "M",
        "channel": null,
        "trigger": "User Action",
        "alertLevel": null,
        "metadata": null
      },
      {
        "date": "2025-01-16T08:15:00Z",
        "eventType": "First Analysis",
        "details": "Wheat - Leaf Rust",
        "cropType": "Wheat",
        "disease": "Leaf Rust",
        "severity": "Moderate",
        "tier": null,
        "channel": null,
        "trigger": "User Action",
        "alertLevel": null,
        "metadata": null
      },
      {
        "date": "2025-01-17T14:20:00Z",
        "eventType": "Message Sent",
        "details": "Sponsor sent follow-up message",
        "cropType": null,
        "disease": null,
        "severity": null,
        "tier": null,
        "channel": "In-app",
        "trigger": "Sponsor Action",
        "alertLevel": null,
        "metadata": null
      },
      {
        "date": "2025-01-22T12:00:00Z",
        "eventType": "High Activity Period",
        "details": "12 analyses in 7 days",
        "cropType": null,
        "disease": null,
        "severity": null,
        "tier": null,
        "channel": null,
        "trigger": "Activity Pattern",
        "alertLevel": "Info",
        "metadata": null
      },
      {
        "date": "2025-02-10T12:00:00Z",
        "eventType": "Decreased Activity",
        "details": "No analyses in 25 days",
        "cropType": null,
        "disease": null,
        "severity": null,
        "tier": null,
        "channel": null,
        "trigger": "Inactivity Detected",
        "alertLevel": "Warning",
        "metadata": null
      },
      {
        "date": "2025-03-01T09:30:00Z",
        "eventType": "Subscription Created",
        "details": "M tier subscription activated",
        "cropType": null,
        "disease": null,
        "severity": null,
        "tier": "M",
        "channel": null,
        "trigger": "System Action",
        "alertLevel": null,
        "metadata": null
      }
    ],
    "behavioralPatterns": {
      "preferredContactTime": "06:00-09:00",
      "averageDaysBetweenAnalyses": 2.5,
      "mostActiveSeason": "Spring",
      "preferredCrops": ["Wheat", "Corn", "Barley"],
      "commonIssues": ["Leaf Rust", "Powdery Mildew", "Aphids"],
      "messageResponseRate": 75.0,
      "averageMessageResponseTimeHours": 4.5,
      "mostActiveWeekday": "Monday",
      "engagementTrend": "Increasing",
      "churnRiskScore": 25.0
    },
    "recommendedActions": [
      "Send personalized message about Wheat care (most common crop)",
      "Offer early renewal discount (expires in 15 days)",
      "Schedule follow-up in 3 days (typical cycle)",
      "Recommend products for Leaf Rust",
      "Share seasonal tips for Wheat, Corn, Barley"
    ]
  }
}
```

### Error Responses

#### 400 Bad Request
```json
{
  "success": false,
  "message": "FarmerId is required"
}
```

#### 401 Unauthorized
```json
{
  "success": false,
  "message": "User ID not found in token"
}
```

#### 403 Forbidden
```json
{
  "success": false,
  "message": "You do not have permission to view this farmer's journey"
}
```

**Note**: Sponsors attempting to view farmers they don't have relationships with will receive this error.

#### 404 Not Found
```json
{
  "success": false,
  "message": "Farmer not found or no journey data available",
  "data": null
}
```

#### 500 Internal Server Error
```json
{
  "success": false,
  "message": "Farmer journey retrieval failed: {error_details}"
}
```

---

## Response Schema Details

### FarmerJourneyDto

| Field | Type | Description |
|-------|------|-------------|
| `farmerId` | int | Farmer's user ID |
| `farmerName` | string | Farmer's full name |
| `journeySummary` | JourneySummaryDto | High-level journey metrics |
| `timeline` | TimelineEventDto[] | Chronological list of all events |
| `behavioralPatterns` | BehavioralPatternsDto | Discovered behavioral insights |
| `recommendedActions` | string[] | AI-driven recommended actions (max 5) |

### JourneySummaryDto

| Field | Type | Description |
|-------|------|-------------|
| `firstCodeRedemption` | DateTime? | Date of first sponsorship code redemption |
| `totalDaysAsCustomer` | int | Days since first code redemption |
| `totalAnalyses` | int | Total plant analyses performed |
| `totalSpent` | decimal | Total amount spent (always 0 for sponsored farmers) |
| `totalValueGenerated` | decimal | Value generated for sponsor (analyses × $50) |
| `currentTier` | string | Current subscription tier (S/M/L/XL/Trial/None) |
| `lifecycleStage` | string | Active/At-Risk/Dormant/Churned |
| `nextRenewalDate` | DateTime? | Next subscription renewal date |
| `daysUntilRenewal` | int? | Days remaining until renewal |

#### Lifecycle Stage Definitions

- **Active**: Last analysis within 7 days
- **At-Risk**: Last analysis 8-30 days ago
- **Dormant**: Last analysis 31-60 days ago
- **Churned**: Last analysis >60 days ago

### TimelineEventDto

| Field | Type | Description |
|-------|------|-------------|
| `date` | DateTime | Event timestamp |
| `eventType` | string | Event category (see Event Types below) |
| `details` | string | Human-readable event description |
| `cropType` | string? | Crop type (for analysis events) |
| `disease` | string? | Disease/issue detected (for analysis events) |
| `severity` | string? | Health severity (for analysis events) |
| `tier` | string? | Subscription tier (for code/subscription events) |
| `channel` | string? | Communication channel (for message events) |
| `trigger` | string? | What triggered the event |
| `alertLevel` | string? | Info/Warning/Critical (for pattern events) |
| `metadata` | string? | Additional contextual information |

#### Event Types

| Event Type | Description | Trigger |
|------------|-------------|---------|
| `Code Redeemed` | Sponsorship code activated | User Action |
| `First Analysis` | First plant analysis performed | User Action |
| `Analysis` | Regular plant analysis | User Action |
| `Message Sent` | Sponsor sent message | Sponsor Action |
| `High Activity Period` | 10+ analyses in 7 days detected | Activity Pattern |
| `Decreased Activity` | 21+ days gap detected | Inactivity Detected |
| `Subscription Created` | New subscription activated | System Action |
| `Reengagement` | Farmer returned after dormancy | User Action |

### BehavioralPatternsDto

| Field | Type | Description |
|-------|------|-------------|
| `preferredContactTime` | string | Time range when most active (e.g., "06:00-09:00") |
| `averageDaysBetweenAnalyses` | decimal | Average frequency of analysis requests |
| `mostActiveSeason` | string | Spring/Summer/Fall/Winter |
| `preferredCrops` | string[] | Top 3 most analyzed crops |
| `commonIssues` | string[] | Top 5 most common diseases/issues |
| `messageResponseRate` | decimal | Percentage of messages read/responded to |
| `averageMessageResponseTimeHours` | decimal | Average hours to read messages |
| `mostActiveWeekday` | string | Day of week with most activity |
| `engagementTrend` | string | Increasing/Stable/Decreasing |
| `churnRiskScore` | decimal | 0-100 risk score (higher = more risk) |

#### Churn Risk Score Calculation

The churn risk score is calculated using three weighted factors:

1. **Days Since Last Analysis (40% weight)**:
   - >60 days: +40 points
   - 31-60 days: +30 points
   - 15-30 days: +20 points
   - 8-14 days: +10 points

2. **Engagement Trend (30% weight)**:
   - Decreasing: +30 points
   - Stable: +10 points
   - Increasing: 0 points

3. **Subscription Status (30% weight)**:
   - No active subscription: +30 points
   - Expires ≤7 days: +20 points
   - Expires ≤30 days: +10 points
   - Expires >30 days: 0 points

**Total**: Sum of all factors, capped at 100

#### Risk Interpretation

- **0-25**: Low risk - Highly engaged farmer
- **26-50**: Medium risk - Monitor engagement
- **51-75**: High risk - Intervention recommended
- **76-100**: Critical risk - Immediate action required

### Recommended Actions

The system generates up to 5 AI-driven recommended actions based on:

**Lifecycle Stage Based**:
- Active: Upselling opportunities, tier upgrades
- At-Risk: Reengagement campaigns, personalized outreach
- Dormant/Churned: Win-back campaigns, special offers

**Pattern Based**:
- Renewal proximity: Early renewal discounts
- Activity patterns: Optimal follow-up timing
- Common issues: Product recommendations
- Preferred crops: Seasonal tips and guidance
- Engagement trends: Tier upgrade suggestions

---

## Use Cases

### Frontend/Mobile Implementation

#### Sponsor Dashboard - Farmer Detail View

**Purpose**: Display comprehensive journey analytics when sponsor clicks on a farmer in their dashboard.

**Implementation**:
```typescript
async function loadFarmerJourney(farmerId: number) {
  try {
    const response = await fetch(
      `${API_BASE_URL}/api/sponsorship/farmer-journey?farmerId=${farmerId}`,
      {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        }
      }
    );

    if (!response.ok) {
      if (response.status === 403) {
        showError('You do not have permission to view this farmer');
        return;
      }
      throw new Error('Failed to load farmer journey');
    }

    const result = await response.json();

    // Update UI components
    renderJourneySummary(result.data.journeySummary);
    renderTimeline(result.data.timeline);
    renderBehavioralPatterns(result.data.behavioralPatterns);
    renderRecommendedActions(result.data.recommendedActions);

  } catch (error) {
    console.error('Error loading farmer journey:', error);
    showError('Failed to load farmer journey analytics');
  }
}
```

#### Admin Panel - Farmer Analytics View

**Purpose**: Allow admins to view journey analytics for any farmer in the system.

**Implementation**: Same as above, but with admin role check on frontend to enable viewing all farmers.

---

## UI Components Recommendations

### 1. Journey Summary Cards

Display key metrics in card format:
- Days as Customer
- Total Analyses
- Current Lifecycle Stage (with color coding)
- Churn Risk Score (with progress bar)

### 2. Timeline Visualization

Vertical timeline showing chronological events:
- Icons for different event types
- Color coding for alert levels
- Expandable details on click
- Filter by event type

### 3. Behavioral Patterns Dashboard

Visualizations for:
- Preferred contact time (clock diagram)
- Crop distribution (pie chart)
- Engagement trend (line chart)
- Activity by weekday (bar chart)

### 4. Recommended Actions Panel

Actionable list with:
- Priority indicators
- "Mark as Done" functionality
- Quick action buttons (send message, schedule call)

---

## Caching Strategy

**Cache Key Format**: `FarmerJourney:{farmerId}:{sponsorId|admin}`

**TTL**: 60 minutes (1 hour)

**Invalidation**: Automatic on expiry (no manual invalidation currently)

**Cache Provider**: Redis

---

## Performance Considerations

- **Average Response Time**: ~200-500ms (first load), ~10-50ms (cached)
- **Data Volume**: Typically 20-100 timeline events per farmer
- **Recommended Page Size**: No pagination needed (single farmer view)
- **Mobile Optimization**: Consider lazy-loading timeline events on mobile

---

## Testing

### Test Scenarios

1. **Sponsor Views Own Farmer**: Should return full journey data
2. **Sponsor Views Other's Farmer**: Should return 403 Forbidden
3. **Admin Views Any Farmer**: Should return full journey data
4. **Farmer Has No Data**: Should return 404 Not Found
5. **Invalid Farmer ID**: Should return 404 Not Found
6. **Cached Data**: Second request should be faster (<50ms)

### Sample Test Data

Use farmer ID 123 with the following characteristics for testing:
- Active lifecycle stage
- 45 total analyses
- Medium (M) tier subscription
- 15 days until renewal
- Preferred contact time: 06:00-09:00
- Churn risk score: 25 (low risk)

---

## Migration Notes

**Database Changes Required**: Yes

**Migration File**: `003_FarmerJourney_Migration.sql`

**Operation Claim**: ID 165 - `GetFarmerJourneyQuery`

**Groups with Access**:
- Administrators (GroupId = 1)
- Sponsors (GroupId = 3)

**Deployment Steps**:
1. Run SQL migration to add operation claim
2. Assign claim to Administrators and Sponsors groups
3. Deploy backend code
4. Update frontend/mobile apps
5. Test with sample farmer data

---

## Troubleshooting

### Common Issues

**Issue**: 403 Forbidden for sponsor viewing their own farmer
**Solution**: Verify farmer has redeemed sponsor's code. Check `SponsorshipCode.UsedByUserId` matches farmer ID.

**Issue**: Empty timeline events
**Solution**: Ensure farmer has performed at least one analysis. Timeline requires data to generate events.

**Issue**: Churn risk score always 100
**Solution**: Check if farmer has any recent analyses. Score calculation requires activity data.

**Issue**: Slow response times (>1s)
**Solution**: Verify Redis caching is enabled and working. Check database query performance.

---

## API Versioning

**Current Version**: v1
**Endpoint Stability**: Stable
**Breaking Changes**: None planned
**Deprecation**: Not applicable

---

## Support

For questions or issues:
- Backend Team: Contact backend development team
- Frontend Team: Refer to this documentation
- Mobile Team: Refer to this documentation
- Admin: Use admin panel analytics tools

---

## Changelog

### 2025-11-12 - Initial Release
- Implemented complete farmer journey analytics
- Added timeline event detection
- Included behavioral pattern analysis
- Generated AI-driven recommendations
- Added 1-hour caching strategy
- Documented full API specification
