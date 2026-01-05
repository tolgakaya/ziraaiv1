# Farmer Bulk Invitation Implementation Plan

**Project**: ZiraAI Farmer Bulk Invitation (Excel Upload + RabbitMQ Async Processing)
**Pattern**: Same as Dealer Bulk Invitation (already implemented)
**Status**: 🟡 IN PROGRESS
**Branch**: `staging`
**Created**: 2026-01-05
**Last Updated**: 2026-01-05

---

## 🎯 OBJECTIVE

Implement **bulk farmer invitation system** using **RabbitMQ + Worker Service pattern** (same as dealer bulk).

**Key Difference from Single Invitation**:
- **Single**: Direct synchronous processing (already exists)
- **Bulk**: Excel upload → RabbitMQ → Worker Service → Async processing

**Reference Implementation**: Dealer Bulk Invitation System

---

## 📋 IMPLEMENTATION CHECKLIST

### ✅ Phase 1: Configuration (5 min)
- [ ] Add `FarmerInvitationRequest` queue to `appsettings.*.json` (4 files)
- [ ] Verify queue naming convention matches dealer pattern

### ✅ Phase 2: Queue Message DTO (5 min)
- [ ] Create `Entities/Dtos/FarmerInvitationQueueMessage.cs`
- [ ] Clone from `DealerInvitationQueueMessage` with farmer-specific fields
- [ ] CodeCount = always 1 (not in DTO, hardcoded in handler)

### ✅ Phase 3: Bulk Service (25 min)
- [ ] Create `Business/Services/Sponsorship/BulkFarmerInvitationService.cs`
- [ ] Create `Business/Services/Sponsorship/IBulkFarmerInvitationService.cs`
- [ ] Implement Excel parsing (Phone, FarmerName, Email, PackageTier, Notes)
- [ ] Implement validation (phone, tier)
- [ ] Implement code availability check (1 code per farmer)
- [ ] Create `BulkInvitationJob` entity (same table as dealer)
- [ ] Publish messages to RabbitMQ

### ✅ Phase 4: Controller Endpoint (15 min)
- [ ] Modify `POST /api/v1/sponsorship/farmer/invitations/bulk` in `SponsorshipController.cs`
- [ ] Change from MediatR direct call to `IBulkFarmerInvitationService`
- [ ] Add dependency injection
- [ ] Add SecuredOperation with proper claims
- [ ] Test endpoint with Postman

### ✅ Phase 5: Worker Consumer (15 min)
- [ ] Create `PlantAnalysisWorkerService/Services/FarmerInvitationConsumerWorker.cs`
- [ ] Clone from `DealerInvitationConsumerWorker`
- [ ] Listen to `farmer-invitation-requests` queue
- [ ] Deserialize `FarmerInvitationQueueMessage`
- [ ] Enqueue Hangfire job
- [ ] Register in `Program.cs`

### ✅ Phase 6: Job Service (30 min)
- [ ] Create `PlantAnalysisWorkerService/Jobs/FarmerInvitationJobService.cs`
- [ ] Create `PlantAnalysisWorkerService/Jobs/IFarmerInvitationJobService.cs`
- [ ] Call single `CreateFarmerInvitationCommand` (tekli endpoint command)
- [ ] Update bulk job progress (atomic)
- [ ] Send SignalR progress notification (HTTP to WebAPI)
- [ ] Check completion and send SignalR completion notification
- [ ] Register in `Program.cs`

### ✅ Phase 7: SignalR Integration (10 min)
- [ ] Use existing `/api/internal/signalr/bulk-invitation-progress` endpoint
- [ ] Use existing `/api/internal/signalr/bulk-invitation-completed` endpoint
- [ ] No changes needed (dealer and farmer share same endpoints)

### ✅ Phase 8: Dependency Injection (5 min)
- [ ] Register `IBulkFarmerInvitationService` in `AutofacBusinessModule.cs`
- [ ] Register `IFarmerInvitationJobService` in Worker Service `Program.cs`
- [ ] Register `FarmerInvitationConsumerWorker` in Worker Service `Program.cs`

### ✅ Phase 9: Build & Test (15 min)
- [ ] Build solution (dotnet build)
- [ ] Fix any compilation errors
- [ ] Test with Postman (Excel upload)
- [ ] Verify RabbitMQ message publish
- [ ] Verify Worker Service processing
- [ ] Verify SignalR notifications

### ✅ Phase 10: Documentation (15 min)
- [ ] Update `FARMER_INVITATIONS_API_COMPLETE_REFERENCE.md` with bulk endpoint
- [ ] Create API documentation with request/response examples
- [ ] Document RabbitMQ flow
- [ ] Document SignalR events
- [ ] Create migration scripts if needed

### ✅ Phase 11: Operation Claims (10 min)
- [ ] Review `SECUREDOPERATION_GUIDE.md`
- [ ] Check `operation_claims.csv` for existing claims
- [ ] Create SQL script for new claims (if needed)
- [ ] Add claims to appropriate groups (Sponsor, Admin)

---

## 🏗️ ARCHITECTURE

```
┌─────────────────┐
│   Controller    │ POST /api/v1/sponsorship/farmer/invitations/bulk
│  (Excel Upload) │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Bulk Service   │ Parse Excel, Validate, Check Codes
│                 │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   RabbitMQ      │ Queue: farmer-invitation-requests
│                 │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Worker Consumer │ Listen to queue
│                 │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Job Service    │ Process each farmer invitation
│                 │ - Call CreateFarmerInvitationCommand
│                 │ - Update bulk job progress
│                 │ - Send SignalR notifications
└─────────────────┘
```

---

## 📊 REFERENCE: DEALER PATTERN FILES

| Component | Dealer File | Farmer File (To Create) |
|-----------|-------------|-------------------------|
| Queue Message DTO | `DealerInvitationQueueMessage.cs` | `FarmerInvitationQueueMessage.cs` |
| Bulk Service | `BulkDealerInvitationService.cs` | `BulkFarmerInvitationService.cs` |
| Worker Consumer | `DealerInvitationConsumerWorker.cs` | `FarmerInvitationConsumerWorker.cs` |
| Job Service | `DealerInvitationJobService.cs` | `FarmerInvitationJobService.cs` |

---

## 🔑 KEY DIFFERENCES: DEALER VS FARMER

| Aspect | Dealer | Farmer |
|--------|--------|--------|
| CodeCount | Variable (from Excel) | **Always 1** (hardcoded) |
| Phone Format | 905xxxxxxxxx (12 digits) | 0xxxxxxxxxx (11 digits) |
| Single Command | `InviteDealerViaSmsCommand` | `CreateFarmerInvitationCommand` |
| Queue Name | `dealer-invitation-requests` | `farmer-invitation-requests` |

---

## 📝 DEVELOPMENT LOG

### 2026-01-05 10:00 - Plan Created
- ✅ Analyzed dealer bulk pattern
- ✅ Identified all components needed
- ✅ Created implementation plan
- **Next**: Start Phase 1 (Configuration)

---

## ⚠️ CRITICAL REMINDERS

1. **Build After Each Phase**: `dotnet build` to catch errors early
2. **No Dependency Errors**: Check all DI registrations
3. **SecuredOperation**: Review existing patterns before adding claims
4. **No Breaking Changes**: Ensure single invitation still works
5. **Manual Migration**: SQL scripts only (no EF migrations)
6. **Documentation**: API docs for mobile/frontend teams
7. **Branch**: All work in `staging` branch
8. **No UI**: Backend only, document for frontend team

---

## 📚 REFERENCE DOCUMENTS

- ✅ `claudedocs/Farmers/FARMER_INVITATIONS_API_COMPLETE_REFERENCE.md` (Single invitation)
- ✅ `claudedocs/Dealers/BULK_DEALER_INVITATION_RABBITMQ_DESIGN.md` (Dealer pattern)
- ✅ `claudedocs/AdminOperations/SECUREDOPERATION_GUIDE.md` (Claims guide)
- ✅ `claudedocs/AdminOperations/operation_claims.csv` (Existing claims)

---

## 🎯 SUCCESS CRITERIA

- [ ] Excel upload successfully creates bulk job
- [ ] RabbitMQ receives farmer invitation messages
- [ ] Worker service processes messages asynchronously
- [ ] Single farmer invitation command is called for each farmer
- [ ] SMS/WhatsApp sent correctly
- [ ] SignalR notifications sent in real-time
- [ ] Bulk job status updated correctly
- [ ] Build succeeds with no errors
- [ ] Single invitation still works (no breaking changes)
- [ ] API documentation complete
