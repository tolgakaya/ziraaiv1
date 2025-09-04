# WhatsApp Sponsor Request System - Technical Architecture

## Overview
The WhatsApp Sponsor Request System enables farmers to request sponsorship from verified sponsors through WhatsApp deeplinks, creating a seamless mobile-first sponsorship workflow with enterprise-grade security and validation.

## Architecture Components

### 1. Core Entities

#### SponsorRequest Entity
```csharp
public class SponsorRequest : IEntity
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public int SponsorId { get; set; }
    public string FarmerPhone { get; set; }    // +905551234567
    public string SponsorPhone { get; set; }   // +905557654321
    public string RequestMessage { get; set; }
    public string RequestToken { get; set; }   // HMAC-SHA256 hashed token
    public DateTime RequestDate { get; set; }
    public string Status { get; set; }         // Pending, Approved, Rejected, Expired
    public DateTime? ApprovalDate { get; set; }
    public int? ApprovedSubscriptionTierId { get; set; }
    public string? ApprovalNotes { get; set; }
    public string? GeneratedSponsorshipCode { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    
    // Navigation properties
    public virtual User Farmer { get; set; }
    public virtual User Sponsor { get; set; }
    public virtual SubscriptionTier ApprovedSubscriptionTier { get; set; }
}
```

#### SponsorContact Entity
```csharp
public class SponsorContact : IEntity
{
    public int Id { get; set; }
    public int SponsorId { get; set; }
    public string ContactName { get; set; }
    public string PhoneNumber { get; set; }
    public string ContactType { get; set; }      // WhatsApp, SMS, Phone
    public bool IsActive { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    
    public virtual User Sponsor { get; set; }
}
```

### 2. Service Layer Architecture

#### ISponsorRequestService Interface
```csharp
public interface ISponsorRequestService
{
    Task<IDataResult<string>> CreateRequestAsync(int farmerId, string sponsorPhone, string message, int tierId);
    Task<IDataResult<SponsorRequest>> ProcessDeeplinkAsync(string hashedToken);
    Task<IResult> ApproveRequestsAsync(List<int> requestIds, int sponsorId, int tierId, string notes);
    Task<IDataResult<List<SponsorRequest>>> GetPendingRequestsAsync(int sponsorId);
    string GenerateWhatsAppMessage(SponsorRequest request);
    string GenerateRequestToken(string farmerPhone, string sponsorPhone, int farmerId);
    Task<SponsorRequest> ValidateRequestTokenAsync(string token);
}
```

### 3. CQRS Implementation

#### Command Handlers
- **CreateSponsorRequestCommand**: Creates new sponsor request with token generation
- **ApproveSponsorRequestCommand**: Processes bulk approval with sponsorship code generation
- **RejectSponsorRequestCommand**: Updates status to rejected (placeholder implementation)

#### Query Handlers
- **GetPendingSponsorRequestsQuery**: Retrieves pending requests for sponsor dashboard
- **ProcessDeeplinkQuery**: Validates and processes WhatsApp deeplink tokens

### 4. Security Architecture

#### Token Generation (HMAC-SHA256)
```
Payload: farmerId:farmerPhone:sponsorPhone:timestamp
Secret: Configurable HMAC key
Hash: HMAC-SHA256(payload, secret)
Encoding: URL-safe Base64 (replace +/= with -_)
Expiry: 24 hours (configurable)
```

#### Authorization Matrix
| Endpoint | Farmer | Sponsor | Admin | Public |
|----------|--------|---------|-------|--------|
| create | ✅ | ❌ | ✅ | ❌ |
| process/{token} | ❌ | ❌ | ❌ | ✅ |
| pending | ❌ | ✅ | ✅ | ❌ |
| approve | ❌ | ✅ | ✅ | ❌ |
| reject | ❌ | ✅ | ✅ | ❌ |
| whatsapp-message | ✅ | ❌ | ✅ | ❌ |

### 5. Database Design

#### Table Relationships
```
Users (Farmers & Sponsors)
├── SponsorRequests (FarmerId, SponsorId)
│   ├── FK: Farmer → Users.UserId
│   ├── FK: Sponsor → Users.UserId
│   └── FK: ApprovedSubscriptionTierId → SubscriptionTiers.Id
│
├── SponsorContacts (SponsorId)
│   └── FK: SponsorId → Users.UserId
│
└── SponsorshipCodes (SponsorId, UsedByUserId)
    ├── FK: SponsorId → Users.UserId
    ├── FK: UsedByUserId → Users.UserId
    └── FK: SubscriptionTierId → SubscriptionTiers.Id
```

#### Unique Constraints
- `SponsorRequests`: Unique(FarmerId, SponsorId, Status) for pending requests
- `SponsorContacts`: Unique(SponsorId, PhoneNumber)
- `SponsorshipCodes`: Unique(Code)

### 6. Configuration Management

#### Required Configuration Keys
```json
{
  "SponsorRequest": {
    "TokenExpiryHours": 24,
    "MaxRequestsPerDay": 10,
    "DeepLinkBaseUrl": "https://ziraai.com/sponsor-request/",
    "DefaultRequestMessage": "Yapay destekli ZiraAI kullanarak bitkilerimi analiz yapmak istiyorum. Sponsor olur musunuz?"
  },
  "Security": {
    "RequestTokenSecret": "ZiraAI-SponsorRequest-SecretKey-2025!@#"
  }
}
```

## Workflow Architecture

### 1. Request Creation Flow
```
Farmer (Mobile App)
├── Select Sponsor Contact
├── Choose Subscription Tier
├── Write Custom Message (optional)
└── Tap "Send Request"
    ├── POST /api/sponsor-request/create
    ├── Generate HMAC-SHA256 token
    ├── Create SponsorRequest record
    ├── Generate WhatsApp deeplink URL
    └── Return WhatsApp message URL to farmer
```

### 2. WhatsApp Message Flow
```
System generates URL:
https://wa.me/+905551234567?text=Message%20with%20deeplink
├── Message: "ZiraAI sponsor request..."
├── Deeplink: https://ziraai.com/sponsor-request/{hashedToken}
└── Farmer sends via WhatsApp
```

### 3. Sponsor Processing Flow
```
Sponsor receives WhatsApp
├── Clicks deeplink URL
├── GET /api/sponsor-request/process/{hashedToken}
├── Validate token (24-hour expiry)
├── Return request details
├── Sponsor reviews in mobile app/web
└── Approve/Reject decision
    ├── POST /api/sponsor-request/approve
    ├── Generate sponsorship code
    ├── Create subscription for farmer
    └── Send notification to farmer
```

### 4. Approval Flow
```
Sponsor Approval:
├── Bulk selection support
├── Subscription tier assignment
├── Optional approval notes
├── Automatic sponsorship code generation
├── Database transaction safety
└── Farmer notification trigger
```

## Security Implementation

### 1. Token Security
- **Algorithm**: HMAC-SHA256 with configurable secret
- **Payload**: Includes farmer ID, phones, and timestamp for uniqueness
- **Encoding**: URL-safe Base64 for WhatsApp compatibility
- **Expiry**: 24-hour window prevents token reuse attacks
- **Validation**: Server-side token verification with replay protection

### 2. Authorization Security
- **JWT Bearer**: All endpoints require valid authentication
- **Role-Based Access**: Farmer/Sponsor/Admin role enforcement
- **Resource Ownership**: Users can only access their own requests
- **Public Endpoint**: Only deeplink processing is publicly accessible

### 3. Data Protection
- **Phone Number Validation**: E.164 format enforcement
- **Message Sanitization**: XSS prevention in request messages
- **Rate Limiting**: Configurable daily request limits per farmer
- **Audit Trail**: Complete logging of all operations

## Performance Characteristics

### 1. Scalability Design
- **Stateless Services**: Horizontal scaling support
- **Repository Pattern**: Database abstraction for optimization
- **Async Operations**: Non-blocking I/O throughout
- **Caching Strategy**: Redis caching for user lookups

### 2. Mobile Optimization
- **Lightweight DTOs**: Minimal data transfer
- **Fast Response Times**: <500ms for all operations
- **WhatsApp Integration**: Direct deeplink generation
- **Offline Resilience**: Graceful degradation support

### 3. Database Optimization
- **Indexed Queries**: Phone numbers and status fields
- **Efficient Joins**: Navigation properties with lazy loading
- **Bulk Operations**: Multiple request approval support
- **Transaction Safety**: ACID compliance for critical operations

## Integration Architecture

### 1. WhatsApp Integration
- **Deeplink Protocol**: Standard WhatsApp URL scheme
- **Message Encoding**: URI-safe encoding for special characters
- **Mobile App**: Flutter integration for seamless UX
- **Web Fallback**: Browser-based approval interface

### 2. Subscription System Integration
- **Automatic Provisioning**: Approved requests create subscriptions
- **Sponsorship Codes**: Generated codes for tracking and analytics
- **Usage Tracking**: Integration with existing quota system
- **Billing Integration**: Sponsor payment tracking

### 3. Notification System Integration
- **Real-time Updates**: Status change notifications
- **Multi-channel**: WhatsApp, email, push notifications
- **Template System**: Configurable message templates
- **Delivery Tracking**: Notification success monitoring

## Error Handling Strategy

### 1. Graceful Degradation
- **Service Failures**: Fallback to basic functionality
- **Network Issues**: Retry mechanisms with exponential backoff
- **Token Expiry**: Clear user messaging and re-request flow
- **Database Errors**: Transaction rollback and user notification

### 2. Monitoring Integration
- **Health Checks**: Service availability monitoring
- **Performance Metrics**: Response time and error rate tracking
- **Business Metrics**: Request volume and approval rate analytics
- **Alert System**: Critical failure notifications

## Quality Assurance

### 1. Testing Strategy
- **Unit Tests**: Service layer and business logic coverage
- **Integration Tests**: Database and external service mocking
- **E2E Tests**: Complete workflow validation
- **Security Tests**: Token validation and authorization checks

### 2. Code Quality
- **Clean Architecture**: SOLID principles adherence
- **Error Handling**: Comprehensive try-catch with logging
- **Performance**: Async/await and efficient database queries
- **Documentation**: Comprehensive code comments and API docs

## Technology Stack

### Backend Technologies
- **.NET 9.0**: Core framework
- **Entity Framework Core**: Database ORM
- **MediatR**: CQRS implementation
- **Autofac**: Dependency injection
- **Serilog**: Structured logging

### Security Technologies
- **HMAC-SHA256**: Token generation and validation
- **JWT Bearer**: API authentication
- **ASP.NET Core Identity**: User management
- **Role-based Authorization**: Access control

### Integration Technologies
- **WhatsApp Business API**: Message integration (future)
- **HTTP Client**: External service communication
- **JSON Serialization**: Data transfer
- **URL Encoding**: Message parameter handling

## Deployment Architecture

### 1. Environment Support
- **Development**: Local SQL Server/PostgreSQL
- **Staging**: Azure/AWS cloud deployment
- **Production**: Containerized microservice deployment
- **Mobile**: Cross-platform Flutter integration

### 2. Infrastructure Requirements
- **Database**: PostgreSQL/SQL Server with connection pooling
- **Caching**: Redis for performance optimization
- **Logging**: Centralized log aggregation
- **Monitoring**: Application performance monitoring

This architecture provides a robust, scalable, and secure foundation for the WhatsApp-based sponsor request workflow, supporting both current requirements and future enhancements.