# Farmer Journey - Frontend Integration Guide

## Admin'in Farmer Journey'ye Erişimi - Açıklama

### ✅ Doğru Anlayış

**Endpoint**: `GET /api/sponsorship/farmer-journey?farmerId={id}`

**Admin Erişim Modeli**:
- ❌ Admin **toplu/liste halinde** tüm farmer journey'lere bakamaz
- ✅ Admin **tek tek** her farmer için `farmerId` parametresi ile journey'yi görüntüler
- ✅ Backend admin için otomatik olarak TÜM verileri döner (sponsor kısıtlaması olmadan)

**Sponsor Erişim Modeli**:
- ✅ Sponsor sadece **kendi çiftçilerini** görebilir (kodunu kullanan çiftçiler)
- ✅ Backend otomatik olarak `RequestingSponsorId` ile filtreler
- ❌ Sponsor başka sponsor'un çiftçilerini **göremez** (403 Forbidden)

---

## Admin Panelinde "View Farmer Journey" Butonu Eklenmesi Gereken Sayfalar

### 1. Admin Users - User List Page

**Endpoint**: `GET /api/admin/users`

**Query Parameters**:
```typescript
{
  page?: number;        // Default: 1
  pageSize?: number;    // Default: 50
  isActive?: boolean;   // Optional filter
  status?: string;      // Optional filter
}
```

**Response Example**:
```json
{
  "success": true,
  "data": {
    "users": [
      {
        "id": 123,
        "fullName": "John Doe",
        "email": "john@example.com",
        "mobilePhone": "+905551234567",
        "isActive": true,
        "roles": ["Farmer"],
        "createdDate": "2025-01-15T10:30:00Z"
      },
      // ... more users
    ],
    "totalCount": 150,
    "page": 1,
    "pageSize": 50
  }
}
```

**UI Action**:
- Tabloda her **Farmer rolüne sahip** kullanıcı için "View Journey" butonu ekleyin
- Buton sadece `roles` içinde `"Farmer"` olanlar için görünsün
- Tıklandığında: `farmerId = user.id` ile Farmer Journey endpoint'ine istek atın

**Implementation Example**:
```typescript
function UserListTable({ users }) {
  const handleViewJourney = async (userId: number) => {
    try {
      const response = await fetch(
        `${API_BASE_URL}/api/sponsorship/farmer-journey?farmerId=${userId}`,
        {
          headers: {
            'Authorization': `Bearer ${authToken}`,
            'Content-Type': 'application/json'
          }
        }
      );

      if (response.ok) {
        const result = await response.json();
        // Navigate to Farmer Journey detail page
        router.push(`/admin/farmers/${userId}/journey`, { state: result.data });
      } else {
        showError('Failed to load farmer journey');
      }
    } catch (error) {
      console.error('Error loading farmer journey:', error);
    }
  };

  return (
    <table>
      {users.map(user => (
        <tr key={user.id}>
          <td>{user.fullName}</td>
          <td>{user.email}</td>
          <td>{user.roles.join(', ')}</td>
          <td>
            {user.roles.includes('Farmer') && (
              <button onClick={() => handleViewJourney(user.id)}>
                View Journey
              </button>
            )}
          </td>
        </tr>
      ))}
    </table>
  );
}
```

---

### 2. Admin Users - Search Page

**Endpoint**: `GET /api/admin/users/search`

**Query Parameters**:
```typescript
{
  searchTerm: string;   // Required (email, name, or mobile phone)
  page?: number;        // Default: 1
  pageSize?: number;    // Default: 50
}
```

**Response Example**: (Same structure as User List)

**UI Action**:
- Search results tablosunda her **Farmer** için "View Journey" butonu
- Implementation aynı User List ile aynı

---

### 3. Admin Sponsorship - Sponsor's Analyses Page

**Endpoint**: `GET /api/admin/sponsorship/sponsors/{sponsorId}/analyses`

**Query Parameters**:
```typescript
{
  sponsorId: number;          // Required (path parameter)
  page?: number;              // Default: 1
  pageSize?: number;          // Default: 20
  sortBy?: string;            // Default: "date"
  sortOrder?: string;         // Default: "desc"
  filterByTier?: string;      // Optional
  filterByCropType?: string;  // Optional
  startDate?: string;         // Optional (ISO format)
  endDate?: string;           // Optional (ISO format)
  dealerId?: number;          // Optional
  filterByMessageStatus?: string; // Optional
  hasUnreadMessages?: boolean;    // Optional
}
```

**Response Example**:
```json
{
  "success": true,
  "data": {
    "analyses": [
      {
        "id": 456,
        "farmerId": 123,
        "farmerName": "John Doe",
        "cropType": "Wheat",
        "disease": "Leaf Rust",
        "severity": "Moderate",
        "analysisDate": "2025-03-15T08:30:00Z",
        "tier": "M",
        "hasUnreadMessages": false
      },
      // ... more analyses
    ],
    "totalCount": 45,
    "page": 1,
    "pageSize": 20
  }
}
```

**UI Action**:
- Her analysis satırında "View Farmer Journey" butonu ekleyin
- Buton tıklandığında: `farmerId` kullanarak Farmer Journey sayfasına yönlendirin

**Implementation Example**:
```typescript
function SponsorAnalysesTable({ analyses, sponsorId }) {
  return (
    <table>
      {analyses.map(analysis => (
        <tr key={analysis.id}>
          <td>{analysis.farmerName}</td>
          <td>{analysis.cropType}</td>
          <td>{analysis.disease}</td>
          <td>
            <button onClick={() => router.push(`/admin/farmers/${analysis.farmerId}/journey`)}>
              View Farmer Journey
            </button>
          </td>
        </tr>
      ))}
    </table>
  );
}
```

---

### 4. Admin Sponsorship - Non-Sponsored Analyses Page

**Endpoint**: `GET /api/admin/sponsorship/non-sponsored/analyses`

**Query Parameters**:
```typescript
{
  page?: number;              // Default: 1
  pageSize?: number;          // Default: 20
  sortBy?: string;            // Default: "date"
  sortOrder?: string;         // Default: "desc"
  filterByCropType?: string;  // Optional
  startDate?: string;         // Optional
  endDate?: string;           // Optional
  filterByStatus?: string;    // Optional
  userId?: number;            // Optional (specific farmer)
}
```

**Response Example**:
```json
{
  "success": true,
  "data": {
    "analyses": [
      {
        "id": 789,
        "userId": 124,
        "userName": "Jane Smith",
        "cropType": "Corn",
        "analysisDate": "2025-03-20T14:00:00Z",
        "status": "Completed",
        "hasSubscription": true
      },
      // ... more analyses
    ],
    "totalCount": 30,
    "page": 1,
    "pageSize": 20
  }
}
```

**UI Action**:
- Her non-sponsored analysis için "View Farmer Journey" butonu
- `userId` kullanarak journey sayfasına yönlendirin

---

### 5. Admin Sponsorship - Non-Sponsored Farmer Detail Page

**Endpoint**: `GET /api/admin/sponsorship/non-sponsored/farmers/{userId}`

**Path Parameter**: `userId` (farmer's user ID)

**Response Example**:
```json
{
  "success": true,
  "data": {
    "userId": 124,
    "userName": "Jane Smith",
    "email": "jane@example.com",
    "mobilePhone": "+905559876543",
    "totalAnalyses": 15,
    "subscriptionTier": "Trial",
    "registrationDate": "2025-02-01T10:00:00Z",
    "lastAnalysisDate": "2025-03-20T14:00:00Z",
    "analyses": [
      // ... list of analyses
    ]
  }
}
```

**UI Action**:
- Farmer detail header'ında prominent bir "View Farmer Journey" butonu
- Buton tıklandığında: Journey detail page'e yönlendirin

**Implementation Example**:
```typescript
function NonSponsoredFarmerDetail({ userId, farmerData }) {
  return (
    <div className="farmer-detail-header">
      <h1>{farmerData.userName}</h1>
      <div className="actions">
        <button
          className="btn-primary"
          onClick={() => router.push(`/admin/farmers/${userId}/journey`)}
        >
          View Complete Journey
        </button>
      </div>
      {/* ... rest of farmer details */}
    </div>
  );
}
```

---

## Farmer Journey Detail Page (Yeni Sayfa)

### Route
- **Admin**: `/admin/farmers/{farmerId}/journey`
- **Sponsor**: `/sponsor/farmers/{farmerId}/journey`

### Layout Önerisi

```typescript
function FarmerJourneyPage({ farmerId }) {
  const [journeyData, setJourneyData] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadFarmerJourney();
  }, [farmerId]);

  async function loadFarmerJourney() {
    try {
      setLoading(true);
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
          router.back();
          return;
        }
        throw new Error('Failed to load farmer journey');
      }

      const result = await response.json();
      setJourneyData(result.data);
    } catch (error) {
      console.error('Error loading farmer journey:', error);
      showError('Failed to load farmer journey analytics');
    } finally {
      setLoading(false);
    }
  }

  if (loading) return <LoadingSpinner />;
  if (!journeyData) return <ErrorMessage />;

  return (
    <div className="farmer-journey-page">
      {/* Header with farmer name and back button */}
      <PageHeader
        title={`${journeyData.farmerName}'s Journey`}
        backButton="/admin/users"
      />

      {/* Journey Summary Cards */}
      <JourneySummarySection summary={journeyData.journeySummary} />

      {/* Timeline Visualization */}
      <TimelineSection events={journeyData.timeline} />

      {/* Behavioral Patterns Dashboard */}
      <BehavioralPatternsSection patterns={journeyData.behavioralPatterns} />

      {/* Recommended Actions Panel */}
      <RecommendedActionsSection actions={journeyData.recommendedActions} />
    </div>
  );
}
```

---

## UI Component Recommendations

### 1. Journey Summary Cards

```typescript
function JourneySummarySection({ summary }) {
  const lifecycleColor = {
    'Active': 'green',
    'At-Risk': 'yellow',
    'Dormant': 'orange',
    'Churned': 'red'
  }[summary.lifecycleStage];

  const riskLevel = summary.churnRiskScore <= 25 ? 'Low' :
                    summary.churnRiskScore <= 50 ? 'Medium' :
                    summary.churnRiskScore <= 75 ? 'High' : 'Critical';

  return (
    <div className="summary-cards">
      <MetricCard
        title="Customer Days"
        value={summary.totalDaysAsCustomer}
        subtitle={`Since ${new Date(summary.firstCodeRedemption).toLocaleDateString()}`}
      />
      <MetricCard
        title="Total Analyses"
        value={summary.totalAnalyses}
        subtitle={`$${summary.totalValueGenerated.toFixed(2)} value`}
      />
      <MetricCard
        title="Lifecycle Stage"
        value={summary.lifecycleStage}
        color={lifecycleColor}
        subtitle={`Tier: ${summary.currentTier}`}
      />
      <MetricCard
        title="Churn Risk"
        value={`${summary.churnRiskScore}%`}
        subtitle={riskLevel}
        progressBar={summary.churnRiskScore}
      />
      {summary.nextRenewalDate && (
        <MetricCard
          title="Next Renewal"
          value={`${summary.daysUntilRenewal} days`}
          subtitle={new Date(summary.nextRenewalDate).toLocaleDateString()}
        />
      )}
    </div>
  );
}
```

### 2. Timeline Visualization

```typescript
function TimelineSection({ events }) {
  const [filter, setFilter] = useState('all');

  const eventIcons = {
    'Code Redeemed': '🎟️',
    'First Analysis': '🌱',
    'Analysis': '🔍',
    'Message Sent': '💬',
    'High Activity Period': '⚡',
    'Decreased Activity': '⚠️',
    'Subscription Created': '📦',
    'Reengagement': '🔄'
  };

  const filteredEvents = filter === 'all'
    ? events
    : events.filter(e => e.eventType === filter);

  return (
    <div className="timeline-section">
      <div className="timeline-header">
        <h2>Timeline ({events.length} events)</h2>
        <select value={filter} onChange={e => setFilter(e.target.value)}>
          <option value="all">All Events</option>
          <option value="Analysis">Analyses Only</option>
          <option value="Message Sent">Messages Only</option>
          <option value="Code Redeemed">Code Events</option>
        </select>
      </div>

      <div className="timeline-list">
        {filteredEvents.map((event, index) => (
          <TimelineEvent
            key={index}
            event={event}
            icon={eventIcons[event.eventType]}
          />
        ))}
      </div>
    </div>
  );
}

function TimelineEvent({ event, icon }) {
  const alertColors = {
    'Info': 'blue',
    'Warning': 'yellow',
    'Critical': 'red'
  };

  return (
    <div className={`timeline-event alert-${event.alertLevel?.toLowerCase()}`}>
      <div className="event-icon">{icon}</div>
      <div className="event-content">
        <div className="event-header">
          <span className="event-type">{event.eventType}</span>
          <span className="event-date">
            {new Date(event.date).toLocaleString()}
          </span>
        </div>
        <div className="event-details">{event.details}</div>
        {event.cropType && (
          <div className="event-meta">
            <span>🌾 {event.cropType}</span>
            {event.disease && <span>🦠 {event.disease}</span>}
            {event.severity && <span>⚠️ {event.severity}</span>}
          </div>
        )}
        {event.alertLevel && (
          <div className={`alert-badge ${alertColors[event.alertLevel]}`}>
            {event.alertLevel}
          </div>
        )}
      </div>
    </div>
  );
}
```

### 3. Behavioral Patterns Dashboard

```typescript
function BehavioralPatternsSection({ patterns }) {
  return (
    <div className="behavioral-patterns">
      <h2>Behavioral Insights</h2>

      <div className="patterns-grid">
        {/* Preferred Contact Time */}
        <PatternCard title="Best Contact Time">
          <ClockDiagram time={patterns.preferredContactTime} />
          <p>{patterns.preferredContactTime}</p>
        </PatternCard>

        {/* Analysis Frequency */}
        <PatternCard title="Analysis Frequency">
          <MetricValue>
            Every {patterns.averageDaysBetweenAnalyses.toFixed(1)} days
          </MetricValue>
          <TrendIndicator trend={patterns.engagementTrend} />
        </PatternCard>

        {/* Preferred Crops */}
        <PatternCard title="Preferred Crops">
          <PieChart
            data={patterns.preferredCrops.map(crop => ({ label: crop, value: 1 }))}
          />
          <ul>
            {patterns.preferredCrops.map(crop => (
              <li key={crop}>🌾 {crop}</li>
            ))}
          </ul>
        </PatternCard>

        {/* Common Issues */}
        <PatternCard title="Common Issues">
          <ul>
            {patterns.commonIssues.map(issue => (
              <li key={issue}>🦠 {issue}</li>
            ))}
          </ul>
        </PatternCard>

        {/* Activity by Weekday */}
        <PatternCard title="Activity Pattern">
          <BarChart
            data={[
              { day: patterns.mostActiveWeekday, active: true },
              // ... other days
            ]}
          />
          <p>Most active on <strong>{patterns.mostActiveWeekday}</strong></p>
        </PatternCard>

        {/* Message Engagement */}
        <PatternCard title="Message Engagement">
          <ProgressCircle value={patterns.messageResponseRate} />
          <p>{patterns.messageResponseRate.toFixed(0)}% response rate</p>
          <small>
            Avg. {patterns.averageMessageResponseTimeHours.toFixed(1)} hours to respond
          </small>
        </PatternCard>
      </div>
    </div>
  );
}
```

### 4. Recommended Actions Panel

```typescript
function RecommendedActionsSection({ actions }) {
  const [completedActions, setCompletedActions] = useState([]);

  const markAsComplete = (actionIndex) => {
    setCompletedActions([...completedActions, actionIndex]);
    // Optionally: Save to backend for tracking
  };

  return (
    <div className="recommended-actions">
      <h2>Recommended Actions</h2>
      <div className="actions-list">
        {actions.map((action, index) => (
          <ActionCard
            key={index}
            action={action}
            isCompleted={completedActions.includes(index)}
            onComplete={() => markAsComplete(index)}
          />
        ))}
      </div>
    </div>
  );
}

function ActionCard({ action, isCompleted, onComplete }) {
  return (
    <div className={`action-card ${isCompleted ? 'completed' : ''}`}>
      <div className="action-content">
        <div className="action-icon">💡</div>
        <p>{action}</p>
      </div>
      <div className="action-buttons">
        {!isCompleted && (
          <>
            <button className="btn-primary" onClick={onComplete}>
              Mark as Done
            </button>
            {action.includes('message') && (
              <button className="btn-secondary" onClick={() => openMessageModal()}>
                Send Message
              </button>
            )}
            {action.includes('renewal') && (
              <button className="btn-secondary" onClick={() => openRenewalModal()}>
                Offer Discount
              </button>
            )}
          </>
        )}
        {isCompleted && <span className="completed-badge">✓ Done</span>}
      </div>
    </div>
  );
}
```

---

## Error Handling

### Common Scenarios

**1. Sponsor Trying to View Other's Farmer (403)**:
```typescript
if (response.status === 403) {
  showNotification({
    type: 'error',
    title: 'Access Denied',
    message: 'You do not have permission to view this farmer\'s journey'
  });
  router.back(); // Go back to previous page
}
```

**2. Farmer Not Found (404)**:
```typescript
if (response.status === 404) {
  showNotification({
    type: 'warning',
    title: 'No Data Available',
    message: 'Journey data not available for this farmer'
  });
  // Show empty state with helpful message
}
```

**3. Unauthorized (401)**:
```typescript
if (response.status === 401) {
  // Token expired or invalid
  logout();
  router.push('/login');
}
```

---

## Performance Optimization

### 1. Use Caching

```typescript
// Cache journey data for 5 minutes on frontend
const CACHE_TTL = 5 * 60 * 1000; // 5 minutes

const journeyCache = new Map();

async function loadFarmerJourney(farmerId: number) {
  const cacheKey = `journey_${farmerId}`;
  const cached = journeyCache.get(cacheKey);

  if (cached && Date.now() - cached.timestamp < CACHE_TTL) {
    return cached.data;
  }

  const data = await fetchFarmerJourney(farmerId);
  journeyCache.set(cacheKey, { data, timestamp: Date.now() });
  return data;
}
```

### 2. Lazy Load Timeline Events

```typescript
// For mobile: Load first 20 events, then lazy load more
function TimelineSection({ events }) {
  const [visibleCount, setVisibleCount] = useState(20);

  const loadMore = () => {
    setVisibleCount(prev => Math.min(prev + 20, events.length));
  };

  return (
    <div>
      {events.slice(0, visibleCount).map(event => (
        <TimelineEvent key={event.id} event={event} />
      ))}
      {visibleCount < events.length && (
        <button onClick={loadMore}>
          Load More ({events.length - visibleCount} remaining)
        </button>
      )}
    </div>
  );
}
```

---

## Mobile Responsiveness

### Recommendations

1. **Collapsible Sections**: Timeline and patterns should be collapsible on mobile
2. **Horizontal Scrolling**: Summary cards should scroll horizontally
3. **Bottom Sheet**: Recommended actions as bottom sheet on mobile
4. **Simplified Timeline**: Show compact view on mobile with expand button

---

## Testing Checklist

### Admin Testing
- [ ] Admin can view journey for ANY farmer from user list
- [ ] Admin can view journey from search results
- [ ] Admin can view journey from sponsor's analyses list
- [ ] Admin can view journey from non-sponsored analyses list
- [ ] Admin can view journey from farmer detail page
- [ ] Timeline shows all event types correctly
- [ ] Behavioral patterns display accurate data
- [ ] Recommended actions are relevant
- [ ] Caching works (second load is faster)

### Sponsor Testing
- [ ] Sponsor can view journey for their own farmers
- [ ] Sponsor gets 403 error for other sponsors' farmers
- [ ] Timeline only shows sponsor-specific data
- [ ] All visualizations render correctly

### Error Scenarios
- [ ] 403 error handled gracefully
- [ ] 404 error handled gracefully
- [ ] 401 error redirects to login
- [ ] Network errors show retry option

---

## Migration Checklist for Frontend Team

### Phase 1: UI Components (Week 1)
- [ ] Create FarmerJourneyPage component
- [ ] Implement JourneySummarySection
- [ ] Implement TimelineSection with event filtering
- [ ] Implement BehavioralPatternsSection
- [ ] Implement RecommendedActionsSection

### Phase 2: Integration (Week 2)
- [ ] Add "View Journey" buttons to Admin Users list
- [ ] Add "View Journey" buttons to Search results
- [ ] Add "View Journey" buttons to Sponsor Analyses list
- [ ] Add "View Journey" buttons to Non-Sponsored Analyses list
- [ ] Add "View Journey" button to Farmer Detail page

### Phase 3: Polish (Week 3)
- [ ] Add loading states
- [ ] Add error handling
- [ ] Implement caching strategy
- [ ] Add mobile responsiveness
- [ ] Test all scenarios
- [ ] Performance optimization

---

## Support & Questions

For backend questions or issues:
- **Backend API**: Refer to `004_FarmerJourney_API_Documentation.md`
- **Database Migration**: Refer to `003_FarmerJourney_Migration.sql`
- **Operation Claim**: ID 165 - `GetFarmerJourneyQuery`

For frontend implementation help:
- Contact frontend team lead
- Review this integration guide
- Test with Postman collection first

---

## Changelog

### 2025-11-12 - Initial Release
- Created comprehensive frontend integration guide
- Defined all admin pages requiring "View Journey" button
- Provided UI component recommendations
- Added error handling patterns
- Included performance optimization tips
- Created mobile responsiveness guidelines
