# Message Engagement Analytics API Documentation

## 1. Endpoint Metadata

| Property | Value |
|----------|-------|
| **Endpoint** | `GET /api/v1/sponsorship/message-engagement` |
| **Method** | GET |
| **Authorization** | Required (JWT Bearer) |
| **Roles** | Sponsor, Admin |
| **Cache TTL** | 6 hours (360 minutes) |
| **Operation Claim** | `GetMessageEngagementQuery` (ID: 167) |
| **Alias** | `sponsorship.analytics.message-engagement` |
| **Rate Limiting** | Standard API rate limits apply |
| **Version** | API v1 |

## 2. Purpose & Use Cases

### Purpose
Provides comprehensive message engagement analytics that help sponsors optimize their farmer communication strategy. The endpoint analyzes sponsor-farmer messaging patterns to reveal:
- Response rates and average response times
- Engagement score (0-10 scale) based on communication effectiveness
- Message breakdown by category (product recommendations, general queries, follow-ups)
- Best performing message templates with conversion metrics
- Optimal time-of-day analysis for maximum engagement

### Use Cases

**For Sponsors:**
1. **Communication Optimization**: Understand which messages get the best response rates
2. **Timing Strategy**: Identify optimal hours for messaging farmers
3. **Template Refinement**: See which message types drive highest engagement
4. **Engagement Monitoring**: Track overall communication effectiveness over time
5. **Conversion Analysis**: Understand which message categories lead to actions

**For Admins:**
1. **Platform Analytics**: Monitor aggregate messaging patterns across all sponsors
2. **Best Practices**: Identify communication strategies that work best
3. **Sponsor Support**: Provide data-driven guidance to sponsors on messaging

### Business Value
- **Higher Response Rates**: Optimize messaging to increase farmer engagement
- **Better Timing**: Send messages when farmers are most likely to respond
- **Template Optimization**: Focus on message types that drive results
- **ROI Improvement**: More effective communication leads to better sponsorship outcomes
- **Data-Driven Strategy**: Replace trial-and-error with proven messaging patterns

## 3. Request Structure

### HTTP Request
```http
GET /api/v1/sponsorship/message-engagement HTTP/1.1
Host: api.ziraai.com
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

### Request Headers
| Header | Required | Description |
|--------|----------|-------------|
| `Authorization` | Yes | JWT Bearer token obtained from login |
| `Content-Type` | Yes | Must be `application/json` |

### Query Parameters
**None** - This endpoint does not accept query parameters. Data filtering is automatic based on user role:
- **Sponsors**: Automatically filtered to show only messages sent/received by the sponsor
- **Admins**: Shows aggregate messaging data across all sponsors

### Authentication Context
The endpoint uses the authenticated user's JWT token to determine:
- User role (Sponsor or Admin)
- User ID for filtering messages (for Sponsors)
- Authorization via SecuredOperation aspect

### Request Example
```bash
# Sponsor Request
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/message-engagement" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"

# Admin Request (identical, role determined by JWT)
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/message-engagement" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"
```

## 4. Response Structure

### Success Response (200 OK)
```json
{
  "success": true,
  "message": "Message engagement analytics retrieved successfully",
  "data": {
    "totalMessagesSent": 342,
    "totalMessagesReceived": 289,
    "responseRate": 84.5,
    "averageResponseTime": 4.2,
    "engagementScore": 7.8,
    "messageBreakdown": {
      "productRecommendations": {
        "sent": 156,
        "responded": 142,
        "conversionRate": 91.0
      },
      "generalQueries": {
        "sent": 98,
        "responded": 76,
        "conversionRate": 77.6
      },
      "followUps": {
        "sent": 88,
        "responded": 71,
        "conversionRate": 80.7
      }
    },
    "bestPerformingMessages": [
      {
        "messageType": "Recommendation",
        "template": "Product solution for detected disease",
        "responseRate": 94.2,
        "avgResponseTime": 2.8,
        "conversionRate": 87.5,
        "usageCount": 112
      },
      {
        "messageType": "Question",
        "template": "Follow-up on treatment effectiveness",
        "responseRate": 88.6,
        "avgResponseTime": 3.5,
        "conversionRate": 82.3,
        "usageCount": 67
      }
    ],
    "timeOfDayAnalysis": {
      "06:00-09:00": {
        "messagesSent": 45,
        "responseRate": 78.2,
        "bestFor": "General queries - farmers check app in morning"
      },
      "09:00-12:00": {
        "messagesSent": 89,
        "responseRate": 88.5,
        "bestFor": "Product recommendations - highest engagement"
      },
      "12:00-15:00": {
        "messagesSent": 67,
        "responseRate": 72.4,
        "bestFor": "Low engagement - farmers in field"
      },
      "15:00-18:00": {
        "messagesSent": 78,
        "responseRate": 82.1,
        "bestFor": "Follow-ups - farmers back from field"
      },
      "18:00-21:00": {
        "messagesSent": 52,
        "responseRate": 85.3,
        "bestFor": "High engagement - farmers reviewing day"
      },
      "21:00-06:00": {
        "messagesSent": 11,
        "responseRate": 45.5,
        "bestFor": "Avoid - very low engagement"
      }
    },
    "sponsorId": 42,
    "generatedAt": "2025-11-12T14:35:22"
  }
}
```

### Error Response (400 Bad Request)
```json
{
  "success": false,
  "message": "Failed to retrieve message engagement analytics"
}
```

### Error Response (401 Unauthorized)
```json
{
  "success": false,
  "message": "User ID not found in claims"
}
```

### Error Response (500 Internal Server Error)
```json
{
  "success": false,
  "message": "Message engagement retrieval failed: {error_details}"
}
```

## 5. Data Models (DTOs)

### MessageEngagementDto
Main response container with comprehensive messaging analytics.

| Field | Type | Description |
|-------|------|-------------|
| `totalMessagesSent` | int | Total messages sent by sponsor to farmers |
| `totalMessagesReceived` | int | Total messages received from farmers |
| `responseRate` | decimal | Percentage of messages that received responses (0-100) |
| `averageResponseTime` | decimal | Average time in hours for farmers to respond |
| `engagementScore` | decimal | Overall engagement quality score (0-10) calculated as: 70% response rate + 30% speed factor |
| `messageBreakdown` | MessageBreakdownDto | Breakdown by message category with conversion metrics |
| `bestPerformingMessages` | List<MessageTemplatePerformanceDto> | Top 5 message templates by effectiveness |
| `timeOfDayAnalysis` | Dictionary<string, TimeSlotAnalysisDto> | Engagement metrics by time slot |
| `sponsorId` | int? | Sponsor ID (null for Admin aggregate view) |
| `generatedAt` | DateTime | Timestamp when analytics were generated |

### MessageBreakdownDto
Categorized message statistics.

| Field | Type | Description |
|-------|------|-------------|
| `productRecommendations` | MessageCategoryStatsDto | Messages recommending specific products |
| `generalQueries` | MessageCategoryStatsDto | General information/help requests |
| `followUps` | MessageCategoryStatsDto | Follow-up messages on previous conversations |

### MessageCategoryStatsDto
Statistics for a specific message category.

| Field | Type | Description |
|-------|------|-------------|
| `sent` | int | Number of messages sent in this category |
| `responded` | int | Number that received responses |
| `conversionRate` | decimal | Response rate percentage for this category |

### MessageTemplatePerformanceDto
Performance metrics for message templates.

| Field | Type | Description |
|-------|------|-------------|
| `messageType` | string | Type of message (Recommendation, Question, Information, Answer) |
| `template` | string | Description of message template/pattern |
| `responseRate` | decimal | Percentage of messages using this template that got responses |
| `avgResponseTime` | decimal | Average response time in hours for this template |
| `conversionRate` | decimal | Percentage of responses that led to meaningful engagement |
| `usageCount` | int | Number of times this template was used |

### TimeSlotAnalysisDto
Engagement metrics for specific time periods.

| Field | Type | Description |
|-------|------|-------------|
| `messagesSent` | int | Number of messages sent during this time slot |
| `responseRate` | decimal | Response rate percentage for this time slot |
| `bestFor` | string | Recommendation for what type of messages work best in this time slot |

## 6. Frontend Integration Notes

### Component Structure Recommendations

**Dashboard Widget:**
```typescript
interface MessageEngagementWidget {
  // Key Metrics Cards (Top Row)
  totalMessagesSent: number;
  totalMessagesReceived: number;
  responseRate: number;        // Display with % symbol and color coding
  engagementScore: number;      // Display with /10 and progress bar

  // Charts (Middle Section)
  timeOfDayChart: {            // Line or bar chart
    labels: string[];          // ["06:00-09:00", "09:00-12:00", ...]
    data: number[];            // Response rates
    insights: string[];        // Best practices for each slot
  };

  messageBreakdownChart: {     // Pie or donut chart
    categories: string[];
    values: number[];
    conversionRates: number[];
  };

  // Top Templates Table (Bottom Section)
  bestPerformingMessages: Array<{
    template: string;
    responseRate: number;
    avgResponseTime: number;
    usageCount: number;
  }>;
}
```

### Display Recommendations

1. **Key Metrics Cards:**
   - Response Rate: Color code (>80% = green, 60-80% = yellow, <60% = red)
   - Engagement Score: Show as circular progress (0-10 scale)
   - Average Response Time: Format as "X hours" with context (faster = better)

2. **Time-of-Day Heatmap:**
   - Use color gradient to show best/worst times
   - Add tooltips with "bestFor" recommendations
   - Highlight top 3 time slots

3. **Message Breakdown:**
   - Donut chart showing distribution
   - Each segment clickable to show details
   - Display conversion rates prominently

4. **Best Templates Table:**
   - Sortable by response rate, usage count
   - Add "Use Template" button for quick access
   - Show trend indicators (↑ ↓ →)

### Refresh Strategy
```typescript
// Cache-aware refresh
const CACHE_DURATION = 6 * 60 * 60 * 1000; // 6 hours

async function fetchMessageEngagement() {
  const lastFetch = localStorage.getItem('lastMessageEngagementFetch');
  const now = Date.now();

  // Respect cache to reduce server load
  if (lastFetch && (now - parseInt(lastFetch)) < CACHE_DURATION) {
    // Use cached data or fetch anyway with indication
    showCacheIndicator();
  }

  const response = await fetch('/api/v1/sponsorship/message-engagement', {
    headers: { Authorization: `Bearer ${token}` }
  });

  localStorage.setItem('lastMessageEngagementFetch', now.toString());
  return response.json();
}
```

### Error Handling
```typescript
try {
  const result = await fetchMessageEngagement();

  if (!result.success) {
    showError(result.message);
    return;
  }

  // Check for empty state
  if (result.data.totalMessagesSent === 0) {
    showEmptyState("No messaging data available yet");
    return;
  }

  renderEngagementDashboard(result.data);
} catch (error) {
  showError("Failed to load engagement analytics");
  logError(error);
}
```

### State Management (React Example)
```typescript
interface MessageEngagementState {
  loading: boolean;
  error: string | null;
  data: MessageEngagementDto | null;
  lastUpdated: Date | null;
}

const useMessageEngagement = () => {
  const [state, setState] = useState<MessageEngagementState>({
    loading: true,
    error: null,
    data: null,
    lastUpdated: null
  });

  useEffect(() => {
    fetchMessageEngagement()
      .then(result => {
        if (result.success) {
          setState({
            loading: false,
            error: null,
            data: result.data,
            lastUpdated: new Date(result.data.generatedAt)
          });
        }
      })
      .catch(error => {
        setState({
          loading: false,
          error: error.message,
          data: null,
          lastUpdated: null
        });
      });
  }, []);

  return state;
};
```

## 7. Complete Examples

### Example 1: Sponsor with High Engagement
**Scenario**: Successful sponsor with active farmer communication

**Request:**
```bash
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/message-engagement" \
  -H "Authorization: Bearer sponsor_jwt_token" \
  -H "Content-Type: application/json"
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Message engagement analytics retrieved successfully",
  "data": {
    "totalMessagesSent": 456,
    "totalMessagesReceived": 398,
    "responseRate": 87.3,
    "averageResponseTime": 3.5,
    "engagementScore": 8.4,
    "messageBreakdown": {
      "productRecommendations": {
        "sent": 234,
        "responded": 215,
        "conversionRate": 91.9
      },
      "generalQueries": {
        "sent": 134,
        "responded": 109,
        "conversionRate": 81.3
      },
      "followUps": {
        "sent": 88,
        "responded": 74,
        "conversionRate": 84.1
      }
    },
    "bestPerformingMessages": [
      {
        "messageType": "Recommendation",
        "template": "Fungicide solution for early blight detection",
        "responseRate": 95.7,
        "avgResponseTime": 2.1,
        "conversionRate": 92.5,
        "usageCount": 156
      },
      {
        "messageType": "Question",
        "template": "Did the recommended treatment work?",
        "responseRate": 91.2,
        "avgResponseTime": 4.3,
        "conversionRate": 88.7,
        "usageCount": 78
      },
      {
        "messageType": "Information",
        "template": "Application timing for best results",
        "responseRate": 86.4,
        "avgResponseTime": 5.2,
        "conversionRate": 79.3,
        "usageCount": 65
      }
    ],
    "timeOfDayAnalysis": {
      "06:00-09:00": {
        "messagesSent": 67,
        "responseRate": 82.1,
        "bestFor": "General queries - farmers starting their day"
      },
      "09:00-12:00": {
        "messagesSent": 123,
        "responseRate": 91.9,
        "bestFor": "Product recommendations - peak engagement time"
      },
      "12:00-15:00": {
        "messagesSent": 89,
        "responseRate": 76.4,
        "bestFor": "Moderate engagement - farmers busy"
      },
      "15:00-18:00": {
        "messagesSent": 102,
        "responseRate": 88.2,
        "bestFor": "Follow-ups - farmers checking progress"
      },
      "18:00-21:00": {
        "messagesSent": 65,
        "responseRate": 89.2,
        "bestFor": "High engagement - farmers reviewing day"
      },
      "21:00-06:00": {
        "messagesSent": 10,
        "responseRate": 40.0,
        "bestFor": "Avoid - low engagement overnight"
      }
    },
    "sponsorId": 42,
    "generatedAt": "2025-11-12T14:35:22"
  }
}
```

**Frontend Display:**
- **Engagement Score Badge**: 8.4/10 (Green, "Excellent engagement")
- **Response Rate Trend**: 87.3% with upward arrow
- **Quick Insight**: "Your messages sent 9:00-12:00 get 91.9% response rate - schedule important communications then!"
- **Action Button**: "Use Top Template" → Pre-fills message with best performing template

---

### Example 2: New Sponsor with Limited Data
**Scenario**: Sponsor who just started messaging

**Request:**
```bash
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/message-engagement" \
  -H "Authorization: Bearer new_sponsor_jwt_token" \
  -H "Content-Type: application/json"
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Message engagement analytics retrieved successfully",
  "data": {
    "totalMessagesSent": 23,
    "totalMessagesReceived": 14,
    "responseRate": 60.9,
    "averageResponseTime": 8.7,
    "engagementScore": 5.2,
    "messageBreakdown": {
      "productRecommendations": {
        "sent": 12,
        "responded": 8,
        "conversionRate": 66.7
      },
      "generalQueries": {
        "sent": 8,
        "responded": 4,
        "conversionRate": 50.0
      },
      "followUps": {
        "sent": 3,
        "responded": 2,
        "conversionRate": 66.7
      }
    },
    "bestPerformingMessages": [
      {
        "messageType": "Recommendation",
        "template": "Product solution for pest control",
        "responseRate": 75.0,
        "avgResponseTime": 6.5,
        "conversionRate": 70.0,
        "usageCount": 8
      }
    ],
    "timeOfDayAnalysis": {
      "06:00-09:00": {
        "messagesSent": 3,
        "responseRate": 66.7,
        "bestFor": "Early morning queries"
      },
      "09:00-12:00": {
        "messagesSent": 9,
        "responseRate": 77.8,
        "bestFor": "Best time - continue sending here"
      },
      "12:00-15:00": {
        "messagesSent": 5,
        "responseRate": 40.0,
        "bestFor": "Low response - avoid this slot"
      },
      "15:00-18:00": {
        "messagesSent": 4,
        "responseRate": 50.0,
        "bestFor": "Moderate engagement"
      },
      "18:00-21:00": {
        "messagesSent": 2,
        "responseRate": 50.0,
        "bestFor": "Limited data"
      },
      "21:00-06:00": {
        "messagesSent": 0,
        "responseRate": 0,
        "bestFor": "No data"
      }
    },
    "sponsorId": 87,
    "generatedAt": "2025-11-12T16:22:45"
  }
}
```

**Frontend Display:**
- **Engagement Score Badge**: 5.2/10 (Yellow, "Building engagement")
- **Coaching Message**: "Send more messages during 9:00-12:00 when you get 77.8% response rate"
- **Growth Indicator**: "Send 10 more messages to unlock detailed template insights"

---

### Example 3: Admin View (All Sponsors)
**Scenario**: Admin viewing platform-wide messaging analytics

**Request:**
```bash
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/message-engagement" \
  -H "Authorization: Bearer admin_jwt_token" \
  -H "Content-Type: application/json"
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Message engagement analytics retrieved successfully",
  "data": {
    "totalMessagesSent": 8456,
    "totalMessagesReceived": 6892,
    "responseRate": 81.5,
    "averageResponseTime": 4.8,
    "engagementScore": 7.5,
    "messageBreakdown": {
      "productRecommendations": {
        "sent": 4234,
        "responded": 3678,
        "conversionRate": 86.9
      },
      "generalQueries": {
        "sent": 2345,
        "responded": 1823,
        "conversionRate": 77.7
      },
      "followUps": {
        "sent": 1877,
        "responded": 1391,
        "conversionRate": 74.1
      }
    },
    "bestPerformingMessages": [
      {
        "messageType": "Recommendation",
        "template": "Targeted product for detected disease",
        "responseRate": 92.3,
        "avgResponseTime": 3.2,
        "conversionRate": 89.1,
        "usageCount": 2234
      },
      {
        "messageType": "Question",
        "template": "Treatment effectiveness follow-up",
        "responseRate": 87.6,
        "avgResponseTime": 4.1,
        "conversionRate": 83.4,
        "usageCount": 1456
      },
      {
        "messageType": "Information",
        "template": "Best practices for application",
        "responseRate": 83.2,
        "avgResponseTime": 5.3,
        "conversionRate": 78.9,
        "usageCount": 987
      }
    ],
    "timeOfDayAnalysis": {
      "06:00-09:00": {
        "messagesSent": 1245,
        "responseRate": 79.8,
        "bestFor": "Morning queries and greetings"
      },
      "09:00-12:00": {
        "messagesSent": 2567,
        "responseRate": 89.2,
        "bestFor": "Peak engagement - product recommendations"
      },
      "12:00-15:00": {
        "messagesSent": 1678,
        "responseRate": 73.4,
        "bestFor": "Lower engagement - farmers working"
      },
      "15:00-18:00": {
        "messagesSent": 1789,
        "responseRate": 84.7,
        "bestFor": "Afternoon follow-ups"
      },
      "18:00-21:00": {
        "messagesSent": 998,
        "responseRate": 86.3,
        "bestFor": "Evening engagement - farmers reviewing"
      },
      "21:00-06:00": {
        "messagesSent": 179,
        "responseRate": 48.6,
        "bestFor": "Low engagement overnight"
      }
    },
    "sponsorId": null,
    "generatedAt": "2025-11-12T17:10:33"
  }
}
```

**Frontend Display (Admin Panel):**
- **Platform Health**: "81.5% platform-wide response rate"
- **Best Practices Insight**: "Coaches sponsors: Recommend messaging 9:00-12:00 for 89.2% response rate"
- **Template Library**: "Top 3 templates achieve 87.7% avg response rate"

---

### Example 4: Empty State (No Messages Yet)
**Scenario**: Sponsor who hasn't sent any messages

**Request:**
```bash
curl -X GET "https://api.ziraai.com/api/v1/sponsorship/message-engagement" \
  -H "Authorization: Bearer sponsor_no_messages_jwt_token" \
  -H "Content-Type: application/json"
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "No message data available",
  "data": {
    "totalMessagesSent": 0,
    "totalMessagesReceived": 0,
    "responseRate": 0,
    "averageResponseTime": 0,
    "engagementScore": 0,
    "messageBreakdown": {
      "productRecommendations": {
        "sent": 0,
        "responded": 0,
        "conversionRate": 0
      },
      "generalQueries": {
        "sent": 0,
        "responded": 0,
        "conversionRate": 0
      },
      "followUps": {
        "sent": 0,
        "responded": 0,
        "conversionRate": 0
      }
    },
    "bestPerformingMessages": [],
    "timeOfDayAnalysis": {},
    "sponsorId": 125,
    "generatedAt": "2025-11-12T18:45:12"
  }
}
```

**Frontend Display:**
- **Empty State Illustration**: "Start engaging with farmers"
- **Call-to-Action**: "Send your first message to begin tracking engagement"
- **Quick Start Guide**: Link to messaging best practices

---

## 8. Error Handling

### Common Error Scenarios

#### 1. Unauthorized Access (401)
**Cause**: Invalid or expired JWT token

**Response:**
```json
{
  "success": false,
  "message": "User ID not found in claims"
}
```

**Resolution:**
- Redirect user to login page
- Refresh JWT token if using refresh token flow
- Clear local storage and prompt re-authentication

#### 2. Insufficient Permissions (403)
**Cause**: User role does not have required operation claim

**Response:**
```json
{
  "success": false,
  "message": "Unauthorized access"
}
```

**Resolution:**
- Verify user has Sponsor or Admin role
- Check GroupClaims assignment in database
- Contact admin for permission escalation

#### 3. Server Error (500)
**Cause**: Database connection failure, cache service down, or unhandled exception

**Response:**
```json
{
  "success": false,
  "message": "Message engagement retrieval failed: Connection timeout"
}
```

**Resolution:**
- Retry request after 5 seconds (exponential backoff)
- Show user-friendly error message
- Log error details for monitoring
- Fall back to cached data if available

#### 4. Empty Data (200 with empty data)
**Cause**: Sponsor has no messaging activity yet

**Handling:**
```typescript
if (result.data.totalMessagesSent === 0) {
  showEmptyState({
    title: "No messaging data yet",
    description: "Start engaging with farmers to see analytics",
    action: "Send First Message"
  });
}
```

### Error Handling Best Practices

```typescript
async function loadMessageEngagement() {
  const MAX_RETRIES = 3;
  const RETRY_DELAY = 2000;

  for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
    try {
      const response = await fetch('/api/v1/sponsorship/message-engagement', {
        headers: { Authorization: `Bearer ${token}` }
      });

      if (response.status === 401) {
        // Token expired - attempt refresh
        await refreshToken();
        continue;
      }

      if (response.status === 500 && attempt < MAX_RETRIES) {
        // Server error - retry with backoff
        await delay(RETRY_DELAY * attempt);
        continue;
      }

      const result = await response.json();

      if (!result.success) {
        throw new Error(result.message);
      }

      return result.data;

    } catch (error) {
      if (attempt === MAX_RETRIES) {
        // Final attempt failed
        logError('Message Engagement API', error);
        showErrorNotification('Failed to load engagement analytics');
        return null;
      }
    }
  }
}
```

### Monitoring & Logging

**Key Metrics to Track:**
- API response time (target: <500ms cache hit, <2000ms cache miss)
- Error rate (target: <1%)
- Cache hit rate (target: >80%)
- Empty state frequency (indicates sponsor onboarding issues)

**Log Events:**
```typescript
// Success
console.log('[MessageEngagement] Analytics loaded', {
  sponsorId: data.sponsorId,
  messageCount: data.totalMessagesSent,
  responseTime: Date.now() - startTime
});

// Error
console.error('[MessageEngagement] API error', {
  statusCode: response.status,
  message: error.message,
  attemptNumber: attempt
});
```

---

## Summary

The Message Engagement Analytics endpoint provides comprehensive insights into sponsor-farmer communication effectiveness. Key features:

✅ **Response Rate Analysis**: Track how many messages get responses
✅ **Engagement Scoring**: 0-10 scale quality metric
✅ **Optimal Timing**: Identify best hours for messaging
✅ **Template Performance**: See which message types work best
✅ **Category Breakdown**: Analyze by message type
✅ **6-Hour Caching**: Fast response times with cache-first strategy
✅ **Role-Based Access**: Sponsors see their data, Admins see aggregates

For questions or issues, contact the ZiraAI development team.
