# ZiraAI WhatsApp Sponsor Request System - Implementation Plan

## 🎯 Project Overview

**Goal**: Implement WhatsApp-based sponsor request system where farmers can request sponsorship from companies via WhatsApp messages with deeplinks, and sponsors can approve requests through mobile dashboard.

**Key Requirements**:
- Farmer sends WhatsApp message with deeplink to sponsor
- Deeplink opens ZiraAI app for request processing  
- Sponsor manages requests via mobile dashboard
- Auto-sponsorship activation upon approval
- No WhatsApp webhook processing (deeplink-based approach)

## 📋 Requirements Summary

### Farmer Workflow
1. Farmer composes WhatsApp message: "Yapay destekli ZiraAI kullanarak bitkilerimi analiz yapmak istiyorum. Bunun için ZiraAI uygulamasında sponsor olmanızı istiyorum"
2. Message includes deeplink with hashed data: `https://ziraai.com/sponsor-request/{hashedToken}`
3. Hash contains: farmer phone, customer number, sponsor phone
4. Farmer sends to sponsor's WhatsApp
5. Sponsor clicks link → ZiraAI app opens → Request processing

### Sponsor Workflow  
1. Sponsor receives WhatsApp message with deeplink
2. Clicks link → ZiraAI mobile app opens
3. Request appears in sponsor dashboard
4. Sponsor can approve single/bulk requests
5. Upon approval → Auto-sponsorship link sent to farmer
6. Sponsor manages contact list (manual + WhatsApp API import)

## 🏗️ Technical Architecture

### Backend Components

#### 1. New Entities

**SponsorRequest Entity**
```csharp
public class SponsorRequest 
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public int SponsorId { get; set; }
    public string FarmerPhone { get; set; }    // +905551234567
    public string SponsorPhone { get; set; }   // +905557654321
    public string RequestMessage { get; set; }
    public string RequestToken { get; set; }   // Hashed verification token
    public DateTime RequestDate { get; set; }
    public string Status { get; set; }         // Pending, Approved, Rejected, Expired
    public DateTime? ApprovalDate { get; set; }
    public int? ApprovedSubscriptionTierId { get; set; }
    public string? ApprovalNotes { get; set; }
    public string? GeneratedSponsorshipCode { get; set; }
}
```

**SponsorContact Entity**
```csharp
public class SponsorContact
{
    public int Id { get; set; }
    public int SponsorId { get; set; }
    public string ContactName { get; set; }
    public string PhoneNumber { get; set; }
    public string Source { get; set; }         // Manual, WhatsAppAPI
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}
```

#### 2. New API Endpoints

**Farmer Request APIs**
```csharp
// Create sponsor request (from mobile app)
POST /api/sponsor-requests/create
{
    "sponsorPhone": "+905557654321",
    "requestMessage": "Yapay destekli ZiraAI...",
    "requestedTierId": 2
}

// Process deeplink when sponsor clicks
GET /api/sponsor-requests/process/{hashedToken}

// Generate WhatsApp message with deeplink
GET /api/sponsor-requests/generate-message/{requestId}
```

**Sponsor Management APIs**
```csharp
// Get pending requests for sponsor
GET /api/sponsor-requests/pending
Authorization: Bearer {token}, Role: Sponsor

// Approve single/bulk requests
POST /api/sponsor-requests/approve
{
    "requestIds": [1, 2, 3],
    "subscriptionTierId": 2,
    "approvalNotes": "Onaylandı"
}

// Reject requests
POST /api/sponsor-requests/reject
{
    "requestIds": [1, 2],
    "rejectionReason": "Budget exceeded"
}

// Contact management
GET /api/sponsor-contacts
POST /api/sponsor-contacts/bulk-add
PUT /api/sponsor-contacts/{id}
DELETE /api/sponsor-contacts/{id}
```

#### 3. CQRS Implementation

**Commands**
- `CreateSponsorRequestCommand`: Create request + generate deeplink
- `ProcessDeeplinkCommand`: Validate token + create request record
- `ApproveSponsorRequestCommand`: Approve + auto-generate sponsorship code
- `ManageSponsorContactsCommand`: Contact CRUD operations

**Queries**
- `GetPendingSponsorRequestsQuery`: Sponsor dashboard data
- `GetSponsorContactsQuery`: Contact list for mobile
- `GetSponsorRequestDetailsQuery`: Request detail view

#### 4. Service Enhancements

**New: ISponsorRequestService**
```csharp
public interface ISponsorRequestService
{
    Task<IDataResult<string>> CreateRequestAsync(int farmerId, string sponsorPhone, string message, int tierId);
    Task<IDataResult<SponsorRequest>> ProcessDeeplinkAsync(string hashedToken);
    Task<IResult> ApproveRequestsAsync(List<int> requestIds, int sponsorId, int tierId, string notes);
    Task<IDataResult<List<SponsorRequest>>> GetPendingRequestsAsync(int sponsorId);
    string GenerateWhatsAppMessage(SponsorRequest request);
}
```

**Enhancement: INotificationService**
```csharp
// Add sponsor request notification
Task<IDataResult<NotificationResultDto>> SendSponsorRequestNotificationAsync(
    int sponsorId, 
    string farmerName, 
    string farmerPhone, 
    string requestMessage);
```

### Backend Database Changes

#### 1. New Tables
```sql
CREATE TABLE "SponsorRequests" (
    "Id" SERIAL PRIMARY KEY,
    "FarmerId" integer NOT NULL,
    "SponsorId" integer NOT NULL, 
    "FarmerPhone" varchar(20) NOT NULL,
    "SponsorPhone" varchar(20) NOT NULL,
    "RequestMessage" text,
    "RequestToken" varchar(255) NOT NULL UNIQUE,
    "RequestDate" timestamp NOT NULL DEFAULT NOW(),
    "Status" varchar(20) NOT NULL DEFAULT 'Pending',
    "ApprovalDate" timestamp,
    "ApprovedSubscriptionTierId" integer,
    "ApprovalNotes" text,
    "GeneratedSponsorshipCode" varchar(50),
    FOREIGN KEY ("FarmerId") REFERENCES "Users"("UserId"),
    FOREIGN KEY ("SponsorId") REFERENCES "Users"("UserId"),
    FOREIGN KEY ("ApprovedSubscriptionTierId") REFERENCES "SubscriptionTiers"("Id")
);

CREATE TABLE "SponsorContacts" (
    "Id" SERIAL PRIMARY KEY,
    "SponsorId" integer NOT NULL,
    "ContactName" varchar(100) NOT NULL,
    "PhoneNumber" varchar(20) NOT NULL,
    "Source" varchar(20) NOT NULL DEFAULT 'Manual',
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedDate" timestamp NOT NULL DEFAULT NOW(),
    FOREIGN KEY ("SponsorId") REFERENCES "Users"("UserId")
);
```

## 📱 Mobile App Implementation

### Flutter Components Required

#### Farmer Side
1. **SponsorRequestScreen**: Request creation form
2. **WhatsAppIntegration**: Deep link generation + WhatsApp launch
3. **RequestStatusScreen**: Track pending/approved requests

#### Sponsor Side  
1. **SponsorDashboardScreen**: Pending requests overview
2. **ContactManagementScreen**: Contact list CRUD
3. **BulkApprovalScreen**: Multi-select + approve interface
4. **WhatsAppContactSync**: Optional WhatsApp Business API contact import

### Key Flutter Packages
```yaml
dependencies:
  url_launcher: ^6.0.0        # WhatsApp deep linking
  contacts_service: ^0.6.0    # Contact access (optional)
  crypto: ^3.0.0              # Token hashing
  provider: ^6.0.0            # State management
```

## 🔐 Security Implementation

### Token Security
```csharp
// Request token generation
public string GenerateRequestToken(string farmerPhone, string sponsorPhone, int farmerId)
{
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var payload = $"{farmerId}:{farmerPhone}:{sponsorPhone}:{timestamp}";
    var secret = _configuration["Security:RequestTokenSecret"];
    
    using (var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret)))
    {
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}

// Token validation  
public async Task<SponsorRequest> ValidateRequestTokenAsync(string token)
{
    // Decode token → Extract farmer/sponsor info → Verify signature
    // Return request data if valid, null if invalid/expired
}
```

### Deep Link Security
- Token expiry (24 hours default)
- One-time use tokens
- Rate limiting for request creation
- Phone number validation and formatting

## 🚀 Implementation Phases

### Phase 1: Backend Foundation (2-3 days)
- [ ] Create SponsorRequest & SponsorContact entities
- [ ] Add EF configurations and database migration
- [ ] Implement ISponsorRequestService
- [ ] Create CQRS handlers
- [ ] Add new API endpoints
- [ ] Unit tests for core logic

### Phase 2: Integration & Security (1-2 days)  
- [ ] Token generation/validation system
- [ ] Deeplink processing endpoint
- [ ] WhatsApp message formatting
- [ ] Push notification integration for sponsors
- [ ] Error handling & validation

### Phase 3: Mobile UI - Farmer (2-3 days)
- [ ] Request creation screen
- [ ] WhatsApp integration (launch with pre-filled message)
- [ ] Request status tracking
- [ ] Deeplink handling

### Phase 4: Mobile UI - Sponsor (3-4 days)
- [ ] Sponsor dashboard with pending requests
- [ ] Bulk approval interface  
- [ ] Contact management screens
- [ ] Push notification handling
- [ ] Request detail views

### Phase 5: Enhancement & Polish (1-2 days)
- [ ] WhatsApp Business API contact sync (optional)
- [ ] Analytics dashboard
- [ ] Admin management tools
- [ ] Performance optimization

## 🔧 Configuration Requirements

### appsettings.json additions
```json
{
  "SponsorRequest": {
    "TokenExpiryHours": 24,
    "MaxRequestsPerDay": 10,
    "DeepLinkBaseUrl": "https://ziraai.com/sponsor-request/",
    "DefaultRequestMessage": "Yapay destekli ZiraAI kullanarak bitkilerimi analiz yapmak istiyorum. Bunun için ZiraAI uygulamasında sponsor olmanızı istiyorum."
  },
  "Security": {
    "RequestTokenSecret": "${SPONSOR_REQUEST_TOKEN_SECRET}"
  }
}
```

## 📊 Success Metrics

### KPIs to Track
- **Request Volume**: Daily/monthly sponsor requests
- **Approval Rate**: % of requests approved by sponsors
- **Conversion Rate**: % of approved requests that activate subscriptions  
- **Response Time**: Time from request to sponsor approval
- **User Engagement**: Active sponsors using the feature

### Analytics Dashboard
- Request status distribution (Pending/Approved/Rejected)
- Top sponsors by approval volume
- Popular subscription tiers requested
- Geographic distribution of requests
- Conversion funnel analysis

## 🚧 Potential Challenges & Solutions

### Challenge 1: WhatsApp Rate Limits
**Solution**: Implement request throttling + queue system

### Challenge 2: Token Security
**Solution**: HMAC-based tokens with expiry + one-time use

### Challenge 3: Spam Prevention  
**Solution**: Rate limiting per farmer phone + sponsor approval workflow

### Challenge 4: Mobile App Deep Linking
**Solution**: Universal Links (iOS) + App Links (Android) + fallback web page

## 💡 Future Enhancements (Post-MVP)

1. **Smart Matching**: AI-powered farmer-sponsor matching algorithms
2. **Campaign Tools**: Scheduled request campaigns for sponsors  
3. **Analytics AI**: Predictive analytics for request success rates
4. **Multi-language**: Support for English/Arabic markets
5. **Integration**: CRM integration for sponsor contact management

## ✅ Ready for Implementation

**Technical Feasibility**: 100% - Built on solid existing infrastructure
**Business Value**: High - Streamlines sponsor-farmer connection process
**User Experience**: Intuitive - Leverages familiar WhatsApp + mobile app patterns
**Security**: Enterprise-grade - Token-based with comprehensive validation

Implementation can begin with Phase 1 (Backend Foundation) whenever ready.