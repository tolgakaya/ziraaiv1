# Sponsorship System Architecture Patterns

## Overview
ZiraAI sponsorship system enables sponsors to purchase subscription packages and distribute codes to farmers. This document captures architectural patterns and design decisions.

## Core Entities

### SponsorProfile
**Purpose**: Stores company information for sponsors
**Location**: `Entities/Concrete/SponsorProfile.cs`
**Key Fields**:
- `CompanyName`: Legal company name for invoices
- `TaxNumber`: Tax identification number
- `Address`, `City`, `Country`, `PostalCode`: Invoice address components
- `SponsorId`: Foreign key to User table

**Usage Pattern**: Acts as default invoice data source for all purchases by sponsor.

### SponsorshipPurchase
**Purpose**: Records package purchases by sponsors
**Location**: `Entities/Concrete/SponsorshipPurchase.cs`
**Key Fields**:
- `InvoiceNumber`: Unique invoice identifier
- `CompanyName`, `InvoiceAddress`, `TaxNumber`: Invoice data per purchase
- `PaymentStatus`: "Pending", "Completed", "Failed", "Refunded"
- `PaymentMethod`: "CreditCard", "BankTransfer", etc.
- `PaymentReference`: External payment system reference
- `PaymentCompletedDate`: When payment was confirmed

**Pattern**: Stores historical invoice data per purchase, allowing overrides from SponsorProfile defaults.

### SponsorshipCode
**Purpose**: Individual subscription codes generated from purchases
**Location**: `Entities/Concrete/SponsorshipCode.cs`
**Key Fields**:
- `Code`: Unique redemption code
- `PurchaseId`: Links to SponsorshipPurchase
- `Status`: "Available", "Sent", "Used", "Expired", "Deactivated"
- `ExpiryDate`: Code validity deadline
- `RedeemedBy`: UserId who redeemed (nullable)
- `RedeemedAt`: Redemption timestamp (nullable)

**Pattern**: One-to-many from Purchase to Codes. Codes generated immediately after purchase (currently) or after payment confirmation (future).

## Service Layer Architecture

### SponsorshipService
**Location**: `Business/Services/Sponsorship/SponsorshipService.cs`
**Responsibilities**:
- Purchase processing with invoice integration
- Code generation and management
- Payment status tracking
- Analytics and reporting

**Dependencies**:
- `ISponsorshipPurchaseRepository`
- `ISponsorshipCodeRepository`
- `ISubscriptionTierRepository`
- `IUserRepository`
- `ISponsorProfileRepository` (added 2025-10-12)

**Key Pattern**: All business logic centralized in service, handlers act as thin command processors.

### SponsorshipTierMappingService
**Location**: `Business/Services/Sponsorship/SponsorshipTierMappingService.cs`
**Purpose**: Maps subscription tier names (S, M, L, XL) to sponsorship feature sets
**Created**: 2025-10-12
**Pattern**: Strategy pattern for tier-specific feature configuration

## Invoice Data Flow Pattern

### Three-Tier Fallback System
Implemented in `SponsorshipService.PurchaseBulkSubscriptionsAsync`:

```csharp
// Priority 1: Provided parameters (from API request)
var finalCompanyName = companyName ?? 
    // Priority 2: SponsorProfile data
    sponsorProfile?.CompanyName ?? 
    // Priority 3: User fallback
    sponsor.FullName;

var finalInvoiceAddress = invoiceAddress ?? sponsorProfile?.Address;
var finalTaxNumber = taxNumber ?? sponsorProfile?.TaxNumber;
```

**Design Rationale**:
- Flexibility: Sponsors can override profile defaults per purchase
- Convenience: Pre-configured profile data auto-fills
- Failsafe: User.FullName ensures company name always exists

**Validation**: Only company name is strictly required, returns error if missing from all sources.

## Payment Flow Architecture

### Current Implementation (Mock)
1. Create `SponsorshipPurchase` with `PaymentStatus = "Pending"`
2. Immediately update to `PaymentStatus = "Completed"` with timestamp
3. Generate codes automatically
4. Return codes in response

**Purpose**: Enables development and testing without payment gateway dependency.

### Future Implementation (Real Payment Gateway)
1. Create `SponsorshipPurchase` with `PaymentStatus = "Pending"`
2. Call payment gateway API (Iyzico/PayTR)
3. Return `paymentUrl` to mobile app
4. Mobile opens WebView for payment
5. Payment gateway sends webhook callback
6. Verify payment with gateway
7. Update `PaymentStatus = "Completed"`
8. Generate codes
9. Notify sponsor

**Architecture Ready**: Service method signature and data model support future integration without breaking changes.

## CQRS Command Pattern

### PurchaseBulkSponsorshipCommand
**Location**: `Business/Handlers/Sponsorship/Commands/PurchaseBulkSponsorshipCommand.cs`
**Pattern**: MediatR command with embedded handler

**Structure**:
```csharp
public class PurchaseBulkSponsorshipCommand : IRequest<IDataResult<...>>
{
    // Command properties (input)
    public int SponsorId { get; set; }
    public string CompanyName { get; set; }
    // ...
    
    public class PurchaseBulkSponsorshipCommandHandler : IRequestHandler<...>
    {
        // Handler implementation delegates to service
    }
}
```

**Responsibility Split**:
- **Command**: Data transfer and validation
- **Handler**: Orchestration and cache management
- **Service**: Business logic execution

**Cache Invalidation Pattern**: Handler invalidates sponsor dashboard cache after successful purchase:
```csharp
if (result.Success)
{
    var cacheKey = $"SponsorDashboard:{request.SponsorId}";
    _cacheManager.Remove(cacheKey);
}
```

## API Controller Pattern

### SponsorshipController
**Location**: `WebAPI/Controllers/SponsorshipController.cs`
**Pattern**: Thin controller delegating to MediatR commands/queries

**Key Endpoints**:
- `GET /tiers-for-purchase`: Returns S, M, L, XL tier comparison (Trial excluded)
- `POST /purchase-package`: Initiates bulk purchase
- `GET /codes`: Paginated code retrieval with filtering
- `GET /statistics`: Sponsor analytics dashboard

**Authorization**: JWT-based with role claims (Sponsor role required)

## Dependency Injection Pattern

### Autofac Registration
**Location**: `Business/DependencyResolvers/AutofacBusinessModule.cs`

**Service Lifetime**: `InstancePerLifetimeScope` (scoped per HTTP request)

**Pattern**:
```csharp
builder.RegisterType<SponsorshipService>()
    .As<ISponsorshipService>()
    .InstancePerLifetimeScope();
```

## Data Access Patterns

### Repository Pattern
All database access through repository abstractions:
- Interface: `DataAccess/Abstract/I{Entity}Repository.cs`
- Implementation: `DataAccess/Concrete/EntityFramework/{Entity}Repository.cs`
- Base: `IRepository<T>` provides standard CRUD operations

### Entity Framework Configuration
- Design-time factory for migrations
- PostgreSQL-specific optimizations
- Timezone handling with legacy timestamp behavior

## Testing Considerations

### Integration Testing
- Use in-memory database for repository tests
- Mock payment gateway responses
- Test invoice data fallback scenarios

### Unit Testing
- Mock repository dependencies
- Test tier mapping logic
- Validate payment status transitions

## Common Pitfalls & Solutions

### DateTime with PostgreSQL
**Issue**: Infinity values cause errors
**Solution**: Global switches in Program.cs:
```csharp
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
```

### Trial Tier Handling
**Issue**: Trial tier caused 500 errors in tier selection endpoint
**Solution**: Filter Trial tier at controller level before mapping:
```csharp
var purchasableTiers = tiers.Where(t => t.TierName != "Trial").ToList();
```

### Nullable vs Non-Nullable Decimals
**Issue**: Compilation errors with null-coalescing operator on non-nullable types
**Solution**: Check entity property types, use direct assignment if non-nullable:
```csharp
// Wrong: YearlyPrice = tier.YearlyPrice ?? 0m
// Right: YearlyPrice = tier.YearlyPrice
```

## Documentation References
- `claudedocs/SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md`: Feature specifications
- `claudedocs/PURCHASE_FLOW_ANALYSIS.md`: Issue analysis and recommendations
- `claudedocs/MOBILE_TIER_SELECTION_INTEGRATION.md`: Mobile integration guide
