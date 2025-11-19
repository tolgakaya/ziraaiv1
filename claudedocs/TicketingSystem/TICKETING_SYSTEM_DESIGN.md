# Ticketing System Design Document

## Overview

Basit bir müşteri destek ticketing sistemi. Farmer ve Sponsor rolleri ticket açabilir, Admin yanıt verebilir. Her kullanıcı sadece kendi ticketlarını görebilir.

## Entity Definitions

### Ticket Entity

```csharp
// Entities/Concrete/Ticket.cs
using Core.Entities;
using Core.Entities.Concrete;
using System;
using System.Collections.Generic;

namespace Entities.Concrete
{
    public class Ticket : IEntity
    {
        public int Id { get; set; }

        // Ticket Owner
        public int UserId { get; set; }                    // Farmer or Sponsor who created
        public string UserRole { get; set; }               // "Farmer" or "Sponsor"

        // Ticket Content
        public string Subject { get; set; }                // Short description
        public string Description { get; set; }            // Initial detailed message
        public string Category { get; set; }               // Technical, Billing, Account, General
        public string Priority { get; set; }               // Low, Normal, High

        // Ticket Status
        public string Status { get; set; }                 // Open, InProgress, Resolved, Closed
        public int? AssignedToUserId { get; set; }         // Admin assigned to ticket

        // Resolution
        public DateTime? ResolvedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string ResolutionNotes { get; set; }

        // Satisfaction
        public int? SatisfactionRating { get; set; }       // 1-5 stars after resolution
        public string SatisfactionFeedback { get; set; }

        // Timestamps
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime? LastResponseDate { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual User AssignedToUser { get; set; }
        public virtual ICollection<TicketMessage> Messages { get; set; }
    }
}
```

### TicketMessage Entity

```csharp
// Entities/Concrete/TicketMessage.cs
using Core.Entities;
using Core.Entities.Concrete;
using System;

namespace Entities.Concrete
{
    public class TicketMessage : IEntity
    {
        public int Id { get; set; }

        // Message Context
        public int TicketId { get; set; }
        public int FromUserId { get; set; }

        // Message Content
        public string Message { get; set; }
        public bool IsAdminResponse { get; set; }          // True if from admin
        public bool IsInternal { get; set; }               // Admin internal note (not visible to user)

        // Message Status
        public bool IsRead { get; set; }
        public DateTime? ReadDate { get; set; }

        // Timestamps
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        public virtual Ticket Ticket { get; set; }
        public virtual User FromUser { get; set; }
    }
}
```

## Status Workflow

```
┌─────────┐    Admin assigns    ┌────────────┐    Admin resolves    ┌──────────┐
│  Open   │ ─────────────────► │ InProgress │ ─────────────────► │ Resolved │
└─────────┘                     └────────────┘                     └──────────┘
     │                                │                                  │
     │                                │                                  │
     │                                │                                  ▼
     │                                │                            ┌──────────┐
     └────────────────────────────────┴───────────────────────────►│  Closed  │
                                                                   └──────────┘
```

### Status Definitions:
- **Open**: Yeni açılmış, henüz admin tarafından alınmamış
- **InProgress**: Admin tarafından alınmış, üzerinde çalışılıyor
- **Resolved**: Çözülmüş, kullanıcı onayı bekliyor
- **Closed**: Kapatılmış (kullanıcı onayladı veya timeout)

### Category Definitions:
- **Technical**: Uygulama kullanımı, hata bildirimleri
- **Billing**: Ödeme, abonelik sorunları
- **Account**: Hesap ayarları, profil sorunları
- **General**: Genel sorular, öneriler

## DTOs

### Request DTOs

```csharp
// Entities/Dtos/CreateTicketDto.cs
public class CreateTicketDto
{
    public string Subject { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }     // Technical, Billing, Account, General
    public string Priority { get; set; }     // Low, Normal, High (default: Normal)
}

// Entities/Dtos/AddTicketMessageDto.cs
public class AddTicketMessageDto
{
    public int TicketId { get; set; }
    public string Message { get; set; }
}

// Entities/Dtos/AdminRespondTicketDto.cs
public class AdminRespondTicketDto
{
    public int TicketId { get; set; }
    public string Message { get; set; }
    public bool IsInternal { get; set; }     // Internal note flag
}

// Entities/Dtos/UpdateTicketStatusDto.cs
public class UpdateTicketStatusDto
{
    public int TicketId { get; set; }
    public string Status { get; set; }
    public string ResolutionNotes { get; set; }
}

// Entities/Dtos/RateTicketResolutionDto.cs
public class RateTicketResolutionDto
{
    public int TicketId { get; set; }
    public int Rating { get; set; }          // 1-5
    public string Feedback { get; set; }
}
```

### Response DTOs

```csharp
// Entities/Dtos/TicketListDto.cs
public class TicketListDto
{
    public int Id { get; set; }
    public string Subject { get; set; }
    public string Category { get; set; }
    public string Priority { get; set; }
    public string Status { get; set; }
    public int MessageCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastResponseDate { get; set; }
}

// Entities/Dtos/TicketDetailDto.cs
public class TicketDetailDto
{
    public int Id { get; set; }
    public string Subject { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string Priority { get; set; }
    public string Status { get; set; }
    public string AssignedToName { get; set; }
    public string ResolutionNotes { get; set; }
    public int? SatisfactionRating { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public List<TicketMessageDto> Messages { get; set; }
}

// Entities/Dtos/TicketMessageDto.cs
public class TicketMessageDto
{
    public int Id { get; set; }
    public string Message { get; set; }
    public string FromUserName { get; set; }
    public bool IsAdminResponse { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
}

// Entities/Dtos/AdminTicketListDto.cs (for admin dashboard)
public class AdminTicketListDto
{
    public int Id { get; set; }
    public string Subject { get; set; }
    public string Category { get; set; }
    public string Priority { get; set; }
    public string Status { get; set; }
    public string UserName { get; set; }
    public string UserRole { get; set; }
    public string AssignedToName { get; set; }
    public int MessageCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastResponseDate { get; set; }
}
```

## API Endpoints

### User Endpoints (Farmer/Sponsor)

| Method | Endpoint | Handler | Description |
|--------|----------|---------|-------------|
| POST | `/api/tickets` | CreateTicketCommand | Yeni ticket oluştur |
| GET | `/api/tickets` | GetMyTicketsQuery | Kendi ticketlarını listele |
| GET | `/api/tickets/{id}` | GetTicketDetailQuery | Ticket detayı ve mesajları |
| POST | `/api/tickets/{id}/messages` | AddTicketMessageCommand | Ticketa mesaj ekle |
| PUT | `/api/tickets/{id}/close` | CloseTicketCommand | Ticketı kapat |
| POST | `/api/tickets/{id}/rate` | RateTicketResolutionCommand | Çözümü puanla |

### Admin Endpoints

| Method | Endpoint | Handler | Description |
|--------|----------|---------|-------------|
| GET | `/api/admin/tickets` | GetAllTicketsAsAdminQuery | Tüm ticketları listele |
| GET | `/api/admin/tickets/{id}` | GetTicketDetailAsAdminQuery | Admin ticket detay (internal notlarla) |
| PUT | `/api/admin/tickets/{id}/assign` | AssignTicketCommand | Ticketı admina ata |
| POST | `/api/admin/tickets/{id}/respond` | AdminRespondTicketCommand | Admin yanıtı gönder |
| PUT | `/api/admin/tickets/{id}/status` | UpdateTicketStatusCommand | Status güncelle |
| GET | `/api/admin/tickets/stats` | GetTicketStatsQuery | Ticket istatistikleri |

## CQRS Handlers

### Commands (Business/Handlers/Tickets/Commands/)

1. **CreateTicketCommand** - Farmer/Sponsor creates new ticket
   - UserId from JWT
   - Validates category, priority
   - Sets initial status to "Open"
   - Operation Claim: `Farmer` OR `Sponsor`

2. **AddTicketMessageCommand** - User adds message to their ticket
   - UserId from JWT
   - Validates ticket ownership
   - Updates LastResponseDate
   - Operation Claim: `Farmer` OR `Sponsor`

3. **CloseTicketCommand** - User closes their ticket
   - UserId from JWT
   - Validates ticket ownership
   - Sets status to "Closed"
   - Operation Claim: `Farmer` OR `Sponsor`

4. **RateTicketResolutionCommand** - User rates resolution
   - UserId from JWT
   - Validates ticket ownership and Resolved status
   - Operation Claim: `Farmer` OR `Sponsor`

5. **AssignTicketCommand** - Admin assigns ticket to themselves
   - AdminId from JWT
   - Sets AssignedToUserId
   - Updates status to "InProgress"
   - Operation Claim: `Admin`

6. **AdminRespondTicketCommand** - Admin sends response
   - AdminId from JWT
   - Creates TicketMessage with IsAdminResponse=true
   - Can be internal note (IsInternal=true)
   - Operation Claim: `Admin`

7. **UpdateTicketStatusCommand** - Admin updates ticket status
   - Sets status, resolution notes
   - Updates appropriate dates
   - Operation Claim: `Admin`

### Queries (Business/Handlers/Tickets/Queries/)

1. **GetMyTicketsQuery** - Get user's tickets
   - UserId from JWT
   - Filter by status, category, date range
   - Pagination support
   - Operation Claim: `Farmer` OR `Sponsor`

2. **GetTicketDetailQuery** - Get ticket detail with messages
   - UserId from JWT
   - Validates ticket ownership
   - Excludes internal admin notes
   - Marks messages as read
   - Operation Claim: `Farmer` OR `Sponsor`

3. **GetAllTicketsAsAdminQuery** - Admin gets all tickets
   - Filter by status, category, priority, userId
   - Pagination support
   - Operation Claim: `Admin`

4. **GetTicketDetailAsAdminQuery** - Admin ticket detail
   - Includes internal notes
   - Shows full ticket history
   - Operation Claim: `Admin`

5. **GetTicketStatsQuery** - Dashboard statistics
   - Total tickets by status
   - Average resolution time
   - Satisfaction ratings
   - Operation Claim: `Admin`

## Repository Interface

```csharp
// DataAccess/Abstract/ITicketRepository.cs
public interface ITicketRepository : IRepository<Ticket>
{
    Task<List<Ticket>> GetUserTicketsAsync(int userId, string status = null, string category = null);
    Task<Ticket> GetTicketWithMessagesAsync(int ticketId);
    Task<List<Ticket>> GetAllTicketsForAdminAsync(string status = null, string category = null, string priority = null);
    Task<int> GetTicketCountByStatusAsync(string status);
}

// DataAccess/Abstract/ITicketMessageRepository.cs
public interface ITicketMessageRepository : IRepository<TicketMessage>
{
    Task<List<TicketMessage>> GetTicketMessagesAsync(int ticketId, bool includeInternal = false);
    Task MarkMessagesAsReadAsync(int ticketId, int userId);
}
```

## EF Configuration

```csharp
// DataAccess/Concrete/Configurations/TicketEntityConfiguration.cs
public class TicketEntityConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Subject).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(2000).IsRequired();
        builder.Property(t => t.Category).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Priority).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Status).HasMaxLength(20).IsRequired();
        builder.Property(t => t.UserRole).HasMaxLength(20).IsRequired();
        builder.Property(t => t.ResolutionNotes).HasMaxLength(1000);
        builder.Property(t => t.SatisfactionFeedback).HasMaxLength(500);

        builder.HasOne(t => t.User)
               .WithMany()
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AssignedToUser)
               .WithMany()
               .HasForeignKey(t => t.AssignedToUserId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.CreatedDate);
    }
}

// DataAccess/Concrete/Configurations/TicketMessageEntityConfiguration.cs
public class TicketMessageEntityConfiguration : IEntityTypeConfiguration<TicketMessage>
{
    public void Configure(EntityTypeBuilder<TicketMessage> builder)
    {
        builder.HasKey(tm => tm.Id);
        builder.Property(tm => tm.Message).HasMaxLength(2000).IsRequired();

        builder.HasOne(tm => tm.Ticket)
               .WithMany(t => t.Messages)
               .HasForeignKey(tm => tm.TicketId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tm => tm.FromUser)
               .WithMany()
               .HasForeignKey(tm => tm.FromUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(tm => tm.TicketId);
        builder.HasIndex(tm => tm.CreatedDate);
    }
}
```

## Operation Claims

New operation claims to add to `OperationClaimSeeds.cs`:

```csharp
// Ticket System Claims (Id range: 140-155)
new OperationClaim { Id = 140, Name = "Ticket.Create", Alias = "Create Ticket", Description = "Create new support ticket" },
new OperationClaim { Id = 141, Name = "Ticket.Read", Alias = "View Own Tickets", Description = "View own tickets" },
new OperationClaim { Id = 142, Name = "Ticket.Message", Alias = "Add Ticket Message", Description = "Add message to own ticket" },
new OperationClaim { Id = 143, Name = "Ticket.Close", Alias = "Close Ticket", Description = "Close own ticket" },
new OperationClaim { Id = 144, Name = "Ticket.Rate", Alias = "Rate Resolution", Description = "Rate ticket resolution" },

// Admin Ticket Claims
new OperationClaim { Id = 150, Name = "GetAllTicketsAsAdminQuery", Alias = "Get All Tickets (Admin)", Description = "Admin view all tickets" },
new OperationClaim { Id = 151, Name = "GetTicketDetailAsAdminQuery", Alias = "Get Ticket Detail (Admin)", Description = "Admin view ticket detail" },
new OperationClaim { Id = 152, Name = "AssignTicketCommand", Alias = "Assign Ticket", Description = "Assign ticket to admin" },
new OperationClaim { Id = 153, Name = "AdminRespondTicketCommand", Alias = "Admin Respond", Description = "Send admin response" },
new OperationClaim { Id = 154, Name = "UpdateTicketStatusCommand", Alias = "Update Ticket Status", Description = "Update ticket status" },
new OperationClaim { Id = 155, Name = "GetTicketStatsQuery", Alias = "Get Ticket Stats", Description = "View ticket statistics" },
```

## Authorization Rules

| Role | Allowed Actions |
|------|-----------------|
| Farmer | Create ticket, view/close own tickets, add messages, rate resolution |
| Sponsor | Create ticket, view/close own tickets, add messages, rate resolution |
| Admin | View all tickets, assign, respond, update status, view stats |

## Validation Rules

### CreateTicketCommand Validation
- Subject: Required, max 200 chars
- Description: Required, max 2000 chars
- Category: Must be one of: Technical, Billing, Account, General
- Priority: Must be one of: Low, Normal, High

### AddTicketMessageCommand Validation
- Message: Required, max 2000 chars
- User must own the ticket
- Ticket must not be Closed

### RateTicketResolutionCommand Validation
- Rating: 1-5
- Ticket must be in Resolved status
- User must own the ticket

## Implementation Phases

### Phase 1: Core Structure
- [ ] Create Ticket and TicketMessage entities
- [ ] Create DTOs
- [ ] Create repository interfaces and implementations
- [ ] Create EF configurations
- [ ] Add DbSet to ProjectDbContext
- [ ] Create migration

### Phase 2: User Operations
- [ ] CreateTicketCommand
- [ ] GetMyTicketsQuery
- [ ] GetTicketDetailQuery
- [ ] AddTicketMessageCommand
- [ ] CloseTicketCommand
- [ ] RateTicketResolutionCommand

### Phase 3: Admin Operations
- [ ] GetAllTicketsAsAdminQuery
- [ ] GetTicketDetailAsAdminQuery
- [ ] AssignTicketCommand
- [ ] AdminRespondTicketCommand
- [ ] UpdateTicketStatusCommand
- [ ] GetTicketStatsQuery

### Phase 4: Controller & Integration
- [ ] TicketsController (user endpoints)
- [ ] Admin endpoints in existing AdminController or new AdminTicketsController
- [ ] Operation claims seed
- [ ] Testing

## Security Considerations

1. **Data Isolation**: Users can only view/modify their own tickets
2. **JWT Validation**: All endpoints require authentication
3. **Input Sanitization**: All text inputs sanitized
4. **Admin Verification**: Admin endpoints verify Admin role via SecuredOperation
5. **Internal Notes**: IsInternal=true messages hidden from users

## Database Migration

```bash
dotnet ef migrations add AddTicketingSystem --project DataAccess --startup-project WebAPI --context ProjectDbContext --output-dir Migrations/Pg
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext
```

## Summary

Bu tasarım, basit ama etkili bir ticketing sistemi sunar:
- Farmer ve Sponsor rolleri ticket açabilir
- Her kullanıcı sadece kendi ticketlarını görebilir
- Admin tüm ticketları görebilir, yanıtlayabilir ve durumu güncelleyebilir
- Mesaj geçmişi takibi
- Çözüm puanlama sistemi
- İstatistik dashboard desteği
