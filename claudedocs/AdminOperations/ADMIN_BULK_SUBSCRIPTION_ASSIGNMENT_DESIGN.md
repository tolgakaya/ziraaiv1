# Admin Bulk Subscription Assignment - Complete System Design

## Overview

Admin'in çiftçilere toplu olarak subscription ataması için tam kapsamlı sistem tasarımı. Bu sistem, mevcut sponsor kod dağıtım sisteminin aynı mimarisi ile çalışır.

**Date**: 2025-11-10
**Branch**: `feature/admin-bulk-subscription-assignment`

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Database Design](#database-design)
3. [Backend Components](#backend-components)
4. [API Endpoints](#api-endpoints)
5. [Worker Service](#worker-service)
6. [Operation Claims](#operation-claims)
7. [Implementation Roadmap](#implementation-roadmap)

---

## Architecture Overview

### System Flow

```
Admin Upload Excel → Validate → Create Job → Publish to RabbitMQ → Worker Process → Update Job Status
                                                                     ↓
                                                        Create/Update Subscription
                                                                     ↓
                                                         Send Notification (SMS/Email)
```

### Key Patterns

1. **Asynchronous Processing**: RabbitMQ queue for scalability (same as sponsor code distribution)
2. **Job Tracking**: BulkSubscriptionAssignmentJob entity for status monitoring
3. **Atomic Operations**: Prevent duplicate subscriptions and race conditions
4. **Excel-Based Input**: Header-based parsing (Email, Phone, TierName, Duration, etc.)
5. **Result File Generation**: Excel file with success/error status per row

### Differences from Sponsor Code Distribution

| Aspect | Sponsor Code Distribution | Admin Subscription Assignment |
|--------|---------------------------|-------------------------------|
| **Input** | Excel with Email, Phone, FarmerName | Excel with Email, Phone, TierName, Duration |
| **Resource** | Pre-purchased sponsorship codes | Direct subscription creation (no codes) |
| **Availability Check** | Check available codes in SponsorshipPurchase | No check needed (admin can create any subscription) |
| **Assignment** | Allocate code → Send link → User redeems | Create UserSubscription directly |
| **Notification** | SMS with redemption link | SMS/Email with subscription activation info |
| **Sponsor Info** | SponsorId, PurchaseId required | CreatedByAdminId (admin user performing action) |

---

## Database Design

### New Entity: BulkSubscriptionAssignmentJob

**Table**: `BulkSubscriptionAssignmentJobs`

**Purpose**: Track bulk subscription assignment jobs processed through RabbitMQ

```csharp
public class BulkSubscriptionAssignmentJob : IEntity
{
    public int Id { get; set; }

    // Admin Information
    public int AdminId { get; set; } // Admin user who initiated the job

    // Configuration
    /// <summary>
    /// Default subscription tier if not specified per farmer in Excel
    /// </summary>
    public int? DefaultTierId { get; set; }

    /// <summary>
    /// Default duration in days if not specified per farmer in Excel
    /// </summary>
    public int? DefaultDurationDays { get; set; }

    /// <summary>
    /// Whether to send notification (SMS or Email) to farmers
    /// </summary>
    public bool SendNotification { get; set; }

    /// <summary>
    /// Notification method: "SMS", "Email", "Both"
    /// </summary>
    public string NotificationMethod { get; set; }

    /// <summary>
    /// Whether to auto-activate subscriptions or create as Pending
    /// </summary>
    public bool AutoActivate { get; set; } = true;

    // Progress Tracking
    public int TotalFarmers { get; set; }
    public int ProcessedFarmers { get; set; }
    public int SuccessfulAssignments { get; set; }
    public int FailedAssignments { get; set; }
    public int NewSubscriptionsCreated { get; set; }
    public int ExistingSubscriptionsUpdated { get; set; }

    // Status: Pending, Processing, Completed, PartialSuccess, Failed
    public string Status { get; set; } = "Pending";

    // Timestamps
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    // File Information
    public string OriginalFileName { get; set; }
    public int FileSize { get; set; } // in bytes

    // Results
    /// <summary>
    /// URL to download result file (Excel with success/error status per row)
    /// Generated after job completion
    /// </summary>
    public string ResultFileUrl { get; set; }

    /// <summary>
    /// JSON array of error details per failed farmer
    /// Format: [{"rowNumber": 12, "email": "user@example.com", "error": "message", "timestamp": "2025-11-10T15:30:00Z"}]
    /// </summary>
    public string ErrorSummary { get; set; }

    // Statistics
    public int TotalNotificationsSent { get; set; }
    public int TotalNotificationsFailed { get; set; }

    // Notes
    public string AdminNotes { get; set; } // Optional notes from admin
}
```

### Database Migration

**Migration Name**: `AddBulkSubscriptionAssignmentJobTable`

```sql
CREATE TABLE "BulkSubscriptionAssignmentJobs" (
    "Id" SERIAL PRIMARY KEY,
    "AdminId" INTEGER NOT NULL,
    "DefaultTierId" INTEGER,
    "DefaultDurationDays" INTEGER,
    "SendNotification" BOOLEAN NOT NULL DEFAULT FALSE,
    "NotificationMethod" VARCHAR(20),
    "AutoActivate" BOOLEAN NOT NULL DEFAULT TRUE,
    "TotalFarmers" INTEGER NOT NULL DEFAULT 0,
    "ProcessedFarmers" INTEGER NOT NULL DEFAULT 0,
    "SuccessfulAssignments" INTEGER NOT NULL DEFAULT 0,
    "FailedAssignments" INTEGER NOT NULL DEFAULT 0,
    "NewSubscriptionsCreated" INTEGER NOT NULL DEFAULT 0,
    "ExistingSubscriptionsUpdated" INTEGER NOT NULL DEFAULT 0,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "CreatedDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "StartedDate" TIMESTAMP,
    "CompletedDate" TIMESTAMP,
    "OriginalFileName" VARCHAR(500),
    "FileSize" INTEGER NOT NULL,
    "ResultFileUrl" VARCHAR(2000),
    "ErrorSummary" TEXT,
    "TotalNotificationsSent" INTEGER NOT NULL DEFAULT 0,
    "TotalNotificationsFailed" INTEGER NOT NULL DEFAULT 0,
    "AdminNotes" TEXT,

    CONSTRAINT "FK_BulkSubscriptionAssignmentJobs_Users_AdminId"
        FOREIGN KEY ("AdminId") REFERENCES "Users"("Id"),
    CONSTRAINT "FK_BulkSubscriptionAssignmentJobs_SubscriptionTiers_DefaultTierId"
        FOREIGN KEY ("DefaultTierId") REFERENCES "SubscriptionTiers"("Id")
);

-- Indexes
CREATE INDEX "IX_BulkSubscriptionAssignmentJobs_AdminId" ON "BulkSubscriptionAssignmentJobs" ("AdminId");
CREATE INDEX "IX_BulkSubscriptionAssignmentJobs_Status" ON "BulkSubscriptionAssignmentJobs" ("Status");
CREATE INDEX "IX_BulkSubscriptionAssignmentJobs_CreatedDate" ON "BulkSubscriptionAssignmentJobs" ("CreatedDate");
```

### Entity Configuration

**File**: `DataAccess/Concrete/Configurations/BulkSubscriptionAssignmentJobEntityConfiguration.cs`

```csharp
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Concrete.Configurations
{
    public class BulkSubscriptionAssignmentJobEntityConfiguration : IEntityTypeConfiguration<BulkSubscriptionAssignmentJob>
    {
        public void Configure(EntityTypeBuilder<BulkSubscriptionAssignmentJob> builder)
        {
            builder.ToTable("BulkSubscriptionAssignmentJobs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
            builder.Property(x => x.NotificationMethod).HasMaxLength(20);
            builder.Property(x => x.OriginalFileName).HasMaxLength(500);
            builder.Property(x => x.ResultFileUrl).HasMaxLength(2000);
            builder.Property(x => x.ErrorSummary).HasColumnType("TEXT");
            builder.Property(x => x.AdminNotes).HasColumnType("TEXT");

            builder.HasIndex(x => x.AdminId).HasDatabaseName("IX_BulkSubscriptionAssignmentJobs_AdminId");
            builder.HasIndex(x => x.Status).HasDatabaseName("IX_BulkSubscriptionAssignmentJobs_Status");
            builder.HasIndex(x => x.CreatedDate).HasDatabaseName("IX_BulkSubscriptionAssignmentJobs_CreatedDate");

            // Relationships
            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<SubscriptionTier>()
                .WithMany()
                .HasForeignKey(x => x.DefaultTierId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
```

### Repository Interface & Implementation

**Interface**: `DataAccess/Abstract/IBulkSubscriptionAssignmentJobRepository.cs`

```csharp
using Core.DataAccess;
using Entities.Concrete;
using System.Threading.Tasks;

namespace DataAccess.Abstract
{
    public interface IBulkSubscriptionAssignmentJobRepository : IRepository<BulkSubscriptionAssignmentJob>
    {
        /// <summary>
        /// Atomically increment progress counters
        /// Prevents race conditions when multiple workers process same job
        /// </summary>
        Task<BulkSubscriptionAssignmentJob> IncrementProgressAsync(
            int jobId,
            bool success,
            bool isNewSubscription,
            bool notificationSent);

        /// <summary>
        /// Mark job as completed and update final statistics
        /// </summary>
        Task<BulkSubscriptionAssignmentJob> CompleteJobAsync(
            int jobId,
            string resultFileUrl,
            string errorSummary);
    }
}
```

**Implementation**: `DataAccess/Concrete/EntityFramework/BulkSubscriptionAssignmentJobRepository.cs`

```csharp
using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Concrete.EntityFramework
{
    public class BulkSubscriptionAssignmentJobRepository : EfRepositoryBase<BulkSubscriptionAssignmentJob, ProjectDbContext>, IBulkSubscriptionAssignmentJobRepository
    {
        public BulkSubscriptionAssignmentJobRepository(ProjectDbContext context) : base(context)
        {
        }

        public async Task<BulkSubscriptionAssignmentJob> IncrementProgressAsync(
            int jobId,
            bool success,
            bool isNewSubscription,
            bool notificationSent)
        {
            var job = await Context.BulkSubscriptionAssignmentJobs.FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
                return null;

            job.ProcessedFarmers++;

            if (success)
            {
                job.SuccessfulAssignments++;

                if (isNewSubscription)
                    job.NewSubscriptionsCreated++;
                else
                    job.ExistingSubscriptionsUpdated++;
            }
            else
            {
                job.FailedAssignments++;
            }

            if (notificationSent)
                job.TotalNotificationsSent++;
            else if (success) // Success but notification failed
                job.TotalNotificationsFailed++;

            // Check if all farmers processed
            if (job.ProcessedFarmers >= job.TotalFarmers)
            {
                job.Status = job.FailedAssignments == 0 ? "Completed" :
                            job.SuccessfulAssignments > 0 ? "PartialSuccess" : "Failed";
                job.CompletedDate = DateTime.Now;
            }

            await Context.SaveChangesAsync();
            return job;
        }

        public async Task<BulkSubscriptionAssignmentJob> CompleteJobAsync(
            int jobId,
            string resultFileUrl,
            string errorSummary)
        {
            var job = await Context.BulkSubscriptionAssignmentJobs.FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
                return null;

            job.ResultFileUrl = resultFileUrl;
            job.ErrorSummary = errorSummary;
            job.CompletedDate = DateTime.Now;

            if (job.Status == "Processing")
            {
                job.Status = job.FailedAssignments == 0 ? "Completed" :
                            job.SuccessfulAssignments > 0 ? "PartialSuccess" : "Failed";
            }

            await Context.SaveChangesAsync();
            return job;
        }
    }
}
```

---

## Backend Components

### 1. DTOs

#### BulkSubscriptionAssignmentJobDto.cs

**File**: `Entities/Dtos/BulkSubscriptionAssignmentJobDto.cs`

```csharp
using System;

namespace Entities.Dtos
{
    public class BulkSubscriptionAssignmentJobDto
    {
        public int JobId { get; set; }
        public int TotalFarmers { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
        public string StatusCheckUrl { get; set; }
    }
}
```

#### BulkSubscriptionAssignmentProgressDto.cs

**File**: `Entities/Dtos/BulkSubscriptionAssignmentProgressDto.cs`

```csharp
using System;

namespace Entities.Dtos
{
    public class BulkSubscriptionAssignmentProgressDto
    {
        public int JobId { get; set; }
        public string Status { get; set; }
        public int TotalFarmers { get; set; }
        public int ProcessedFarmers { get; set; }
        public int SuccessfulAssignments { get; set; }
        public int FailedAssignments { get; set; }
        public int NewSubscriptionsCreated { get; set; }
        public int ExistingSubscriptionsUpdated { get; set; }
        public int TotalNotificationsSent { get; set; }
        public int ProgressPercentage { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? StartedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string ResultFileUrl { get; set; }
        public string ErrorSummary { get; set; }
        public string AdminNotes { get; set; }
    }
}
```

#### BulkSubscriptionAssignmentFormDto.cs

**File**: `Entities/Dtos/BulkSubscriptionAssignmentFormDto.cs`

```csharp
using Microsoft.AspNetCore.Http;

namespace Entities.Dtos
{
    /// <summary>
    /// Form data for bulk subscription assignment request
    /// </summary>
    public class BulkSubscriptionAssignmentFormDto
    {
        public IFormFile ExcelFile { get; set; }
        public int? DefaultTierId { get; set; }
        public int? DefaultDurationDays { get; set; }
        public bool SendNotification { get; set; } = false;
        public string NotificationMethod { get; set; } = "Email"; // Email, SMS, Both
        public bool AutoActivate { get; set; } = true;
        public string AdminNotes { get; set; }
    }
}
```

### 2. Service: BulkSubscriptionAssignmentService

**File**: `Business/Services/Subscription/BulkSubscriptionAssignmentService.cs`

**Purpose**: Parse Excel, validate, create job, publish to RabbitMQ

**Key Methods**:
- `QueueBulkSubscriptionAssignmentAsync()`: Main entry point
- `ValidateFile()`: Check file size, extension
- `ParseExcelAsync()`: Header-based parsing (Email, Phone, TierName, Duration)
- `ValidateRowsAsync()`: Email/phone format, tier validation, duplicates
- `PublishToRabbitMQ()`: Publish messages per farmer

**Excel Columns** (Header-based):
- **Email** (required): Farmer's email
- **Phone** (optional): For SMS notifications
- **TierName** (optional): S, M, L, XL (uses DefaultTierId if not specified)
- **Duration** (optional): Duration in days (uses DefaultDurationDays if not specified)
- **FarmerName** (optional): Display name
- **Notes** (optional): Per-farmer notes

### 3. Queue Message Structure

```csharp
public class FarmerSubscriptionAssignmentQueueMessage
{
    public string CorrelationId { get; set; }
    public int RowNumber { get; set; }
    public int BulkJobId { get; set; }
    public int AdminId { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string FarmerName { get; set; }
    public int TierId { get; set; }
    public int DurationDays { get; set; }
    public bool SendNotification { get; set; }
    public string NotificationMethod { get; set; }
    public bool AutoActivate { get; set; }
    public string Notes { get; set; }
    public DateTime QueuedAt { get; set; }
}
```

### 4. RabbitMQ Queue Configuration

**Config Key**: `RabbitMQOptions.Queues.FarmerSubscriptionAssignmentRequest`

**Queue Name**: `farmer-subscription-assignment-request`

**Exchange**: `ziraai-admin-exchange`

**Routing Key**: `subscription.assignment.farmer`

---

## API Endpoints

### Controller: AdminSubscriptionController

**File**: `WebAPI/Controllers/AdminSubscriptionController.cs`

**Base Route**: `/api/admin/subscriptions`

### 1. Upload Bulk Subscription Assignment (POST)

**Endpoint**: `POST /api/admin/subscriptions/bulk-assignment`

**Operation Claim**: `BulkSubscriptionAssignmentCommand` (ID: 159)

**Request**: `multipart/form-data`
- `excelFile`: Excel file
- `defaultTierId`: Default tier if not in Excel
- `defaultDurationDays`: Default duration if not in Excel
- `sendNotification`: true/false
- `notificationMethod`: Email, SMS, Both
- `autoActivate`: true/false
- `adminNotes`: Optional notes

**Response** (200 OK):
```json
{
  "data": {
    "jobId": 123,
    "totalFarmers": 150,
    "status": "Processing",
    "createdDate": "2025-11-10T10:00:00Z",
    "estimatedCompletionTime": "2025-11-10T10:15:00Z",
    "statusCheckUrl": "/api/admin/subscriptions/bulk-assignment/status/123"
  },
  "success": true,
  "message": "Toplu subscription ataması başlatıldı. 150 farmer kuyruğa eklendi."
}
```

### 2. Get Job Status (GET)

**Endpoint**: `GET /api/admin/subscriptions/bulk-assignment/status/{jobId}`

**Alias**: `GET /api/admin/subscriptions/bulk-assignment/{jobId}`

**Operation Claim**: `GetBulkSubscriptionAssignmentStatusQuery` (ID: 160)

**Response** (200 OK):
```json
{
  "data": {
    "jobId": 123,
    "status": "Processing",
    "totalFarmers": 150,
    "processedFarmers": 75,
    "successfulAssignments": 70,
    "failedAssignments": 5,
    "newSubscriptionsCreated": 50,
    "existingSubscriptionsUpdated": 20,
    "totalNotificationsSent": 65,
    "progressPercentage": 50,
    "createdDate": "2025-11-10T10:00:00Z",
    "startedDate": "2025-11-10T10:00:05Z",
    "completedDate": null,
    "resultFileUrl": null,
    "errorSummary": null,
    "adminNotes": "Q4 farmer onboarding"
  },
  "success": true
}
```

### 3. Get Job History (GET)

**Endpoint**: `GET /api/admin/subscriptions/bulk-assignment/history`

**Operation Claim**: `GetBulkSubscriptionAssignmentHistoryQuery` (ID: 161)

**Query Parameters**:
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 20, max: 100)
- `adminId`: Filter by admin user
- `status`: Filter by status (Pending, Processing, Completed, PartialSuccess, Failed)
- `startDate`: Filter from date
- `endDate`: Filter to date

**Response** (200 OK):
```json
{
  "data": {
    "items": [...],
    "currentPage": 1,
    "totalPages": 5,
    "totalItems": 100,
    "hasNextPage": true,
    "hasPreviousPage": false
  },
  "success": true
}
```

### 4. Download Result File (GET)

**Endpoint**: `GET /api/admin/subscriptions/bulk-assignment/{jobId}/result`

**Operation Claim**: `GetBulkSubscriptionAssignmentResultQuery` (ID: 162)

**Response**: Excel file download (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet)

---

## Worker Service

### FarmerSubscriptionAssignmentJobService

**File**: `PlantAnalysisWorkerService/Jobs/FarmerSubscriptionAssignmentJobService.cs`

**Queue**: `farmer-subscription-assignment-request`

**Process Flow**:

1. **Find or Create User**
   - Search by email
   - If not found, create new user with email/phone
   - Set user role as Farmer

2. **Check Existing Subscription**
   - Find active subscription for user
   - Decide: Create new or update existing

3. **Create/Update Subscription**
   ```csharp
   var subscription = new UserSubscription
   {
       UserId = user.Id,
       SubscriptionTierId = message.TierId,
       StartDate = DateTime.Now,
       EndDate = DateTime.Now.AddDays(message.DurationDays),
       IsActive = message.AutoActivate,
       Status = message.AutoActivate ? "Active" : "Pending",
       PaymentMethod = "AdminAssignment",
       PaymentReference = $"BulkJob-{message.BulkJobId}",
       PaidAmount = 0,
       Currency = "TRY",
       CreatedUserId = message.AdminId,
       CreatedDate = DateTime.Now,
       IsSponsoredSubscription = false,
       IsTrialSubscription = false
   };
   ```

4. **Send Notification**
   - **SMS**: "Merhaba {FarmerName}, ZiraAI {TierName} subscription'ınız aktif edildi. {Duration} gün boyunca günlük {DailyLimit} analiz yapabilirsiniz."
   - **Email**: HTML email with subscription details

5. **Update Job Progress**
   - Call `IncrementProgressAsync()` atomically
   - Update success/failure counts

6. **Error Handling**
   - Log errors to ErrorSummary JSON
   - Continue processing (don't fail entire job)

### RabbitMQ Consumer Registration

**File**: `PlantAnalysisWorkerService/Program.cs`

```csharp
// Register consumer
services.AddHostedService(provider => new RabbitMQConsumerService(
    provider,
    rabbitMQOptions.Queues.FarmerSubscriptionAssignmentRequest,
    async (message, correlationId, serviceProvider) =>
    {
        var jobService = serviceProvider.GetRequiredService<IFarmerSubscriptionAssignmentJobService>();
        await jobService.ProcessFarmerSubscriptionAssignmentAsync(message, correlationId);
    }
));
```

---

## Operation Claims

### New Claims

| Claim ID | Claim Name | Alias | Description |
|----------|------------|-------|-------------|
| 159 | BulkSubscriptionAssignmentCommand | Admin Bulk Subscription Assignment | Admin olarak toplu subscription ataması |
| 160 | GetBulkSubscriptionAssignmentStatusQuery | Admin Bulk Subscription Assignment Status | Bulk subscription job durumu görüntüleme |
| 161 | GetBulkSubscriptionAssignmentHistoryQuery | Admin Bulk Subscription Assignment History | Bulk subscription job geçmişi görüntüleme |
| 162 | GetBulkSubscriptionAssignmentResultQuery | Admin Bulk Subscription Assignment Result | Bulk subscription job sonuç dosyası indirme |

### SQL Script

**File**: `claudedocs/AdminOperations/ADD_ADMIN_BULK_SUBSCRIPTION_ASSIGNMENT_CLAIMS.sql`

```sql
-- =============================================
-- Admin Bulk Subscription Assignment - Operation Claims
-- =============================================
-- Date: 2025-11-10
-- Claims: 159-162
-- =============================================

DO $$
BEGIN
    -- Claim 159: Bulk Subscription Assignment
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Id" = 159) THEN
        INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
        VALUES (159, 'BulkSubscriptionAssignmentCommand', 'Admin Bulk Subscription Assignment', 'Admin olarak toplu subscription ataması');
        RAISE NOTICE 'Claim 159 added successfully';
    END IF;

    -- Claim 160: Get Status
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Id" = 160) THEN
        INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
        VALUES (160, 'GetBulkSubscriptionAssignmentStatusQuery', 'Admin Bulk Subscription Assignment Status', 'Bulk subscription job durumu görüntüleme');
        RAISE NOTICE 'Claim 160 added successfully';
    END IF;

    -- Claim 161: Get History
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Id" = 161) THEN
        INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
        VALUES (161, 'GetBulkSubscriptionAssignmentHistoryQuery', 'Admin Bulk Subscription Assignment History', 'Bulk subscription job geçmişi görüntüleme');
        RAISE NOTICE 'Claim 161 added successfully';
    END IF;

    -- Claim 162: Download Result
    IF NOT EXISTS (SELECT 1 FROM "OperationClaims" WHERE "Id" = 162) THEN
        INSERT INTO "OperationClaims" ("Id", "Name", "Alias", "Description")
        VALUES (162, 'GetBulkSubscriptionAssignmentResultQuery', 'Admin Bulk Subscription Assignment Result', 'Bulk subscription job sonuç dosyası indirme');
        RAISE NOTICE 'Claim 162 added successfully';
    END IF;
END $$;

-- Grant all claims to Administrators (GroupId = 1)
DO $$
BEGIN
    -- Grant Claim 159
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 159) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId") VALUES (1, 159);
        RAISE NOTICE 'Claim 159 granted to Administrators';
    END IF;

    -- Grant Claim 160
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 160) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId") VALUES (1, 160);
        RAISE NOTICE 'Claim 160 granted to Administrators';
    END IF;

    -- Grant Claim 161
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 161) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId") VALUES (1, 161);
        RAISE NOTICE 'Claim 161 granted to Administrators';
    END IF;

    -- Grant Claim 162
    IF NOT EXISTS (SELECT 1 FROM "GroupClaims" WHERE "GroupId" = 1 AND "ClaimId" = 162) THEN
        INSERT INTO "GroupClaims" ("GroupId", "ClaimId") VALUES (1, 162);
        RAISE NOTICE 'Claim 162 granted to Administrators';
    END IF;
END $$;

-- Verification Query
SELECT oc."Id", oc."Name", oc."Alias",
       CASE WHEN gc."GroupId" IS NOT NULL THEN 'YES - GroupId: ' || gc."GroupId"
            ELSE 'NO - MISSING' END as "HasGroupClaim"
FROM "OperationClaims" oc
LEFT JOIN "GroupClaims" gc ON oc."Id" = gc."ClaimId" AND gc."GroupId" = 1
WHERE oc."Id" BETWEEN 159 AND 162
ORDER BY oc."Id";
```

---

## Implementation Roadmap

### Phase 1: Database & Core Entities (Priority: HIGH)

**Tasks**:
1. Create `BulkSubscriptionAssignmentJob` entity
2. Create entity configuration
3. Create migration
4. Create repository interface
5. Implement repository with atomic methods
6. Run migration on dev database

**Files**:
- `Entities/Concrete/BulkSubscriptionAssignmentJob.cs`
- `DataAccess/Concrete/Configurations/BulkSubscriptionAssignmentJobEntityConfiguration.cs`
- `DataAccess/Abstract/IBulkSubscriptionAssignmentJobRepository.cs`
- `DataAccess/Concrete/EntityFramework/BulkSubscriptionAssignmentJobRepository.cs`
- `DataAccess/Migrations/Pg/YYYYMMDDHHMMSS_AddBulkSubscriptionAssignmentJobTable.cs`

### Phase 2: DTOs & Queue Messages (Priority: HIGH)

**Tasks**:
1. Create `BulkSubscriptionAssignmentJobDto`
2. Create `BulkSubscriptionAssignmentProgressDto`
3. Create `BulkSubscriptionAssignmentFormDto`
4. Create `FarmerSubscriptionAssignmentQueueMessage`
5. Create validators for DTOs

**Files**:
- `Entities/Dtos/BulkSubscriptionAssignmentJobDto.cs`
- `Entities/Dtos/BulkSubscriptionAssignmentProgressDto.cs`
- `Entities/Dtos/BulkSubscriptionAssignmentFormDto.cs`
- `Business/Services/Subscription/BulkSubscriptionAssignmentService.cs` (queue message class)
- `Business/Validators/BulkSubscriptionAssignmentFormDtoValidator.cs`

### Phase 3: Business Service (Priority: HIGH)

**Tasks**:
1. Create `IBulkSubscriptionAssignmentService` interface
2. Implement `BulkSubscriptionAssignmentService`
   - File validation
   - Excel parsing (header-based)
   - Row validation
   - Job creation
   - RabbitMQ publishing
3. Add RabbitMQ queue configuration
4. Register service in DI container

**Files**:
- `Business/Services/Subscription/BulkSubscriptionAssignmentService.cs`
- `Core/Configuration/RabbitMQOptions.cs` (add new queue)
- `Business/DependencyResolvers/AutofacBusinessModule.cs`

### Phase 4: API Endpoints (Priority: HIGH)

**Tasks**:
1. Create `AdminSubscriptionController`
2. Implement POST `/bulk-assignment` endpoint
3. Implement GET `/bulk-assignment/status/{jobId}` endpoint
4. Implement GET `/bulk-assignment/history` endpoint
5. Implement GET `/bulk-assignment/{jobId}/result` endpoint
6. Create command/query handlers

**Files**:
- `WebAPI/Controllers/AdminSubscriptionController.cs`
- `Business/Handlers/AdminSubscription/Commands/BulkSubscriptionAssignmentCommand.cs`
- `Business/Handlers/AdminSubscription/Queries/GetBulkSubscriptionAssignmentStatusQuery.cs`
- `Business/Handlers/AdminSubscription/Queries/GetBulkSubscriptionAssignmentHistoryQuery.cs`
- `Business/Handlers/AdminSubscription/Queries/GetBulkSubscriptionAssignmentResultQuery.cs`

### Phase 5: Worker Service (Priority: HIGH)

**Tasks**:
1. Create `IFarmerSubscriptionAssignmentJobService`
2. Implement `FarmerSubscriptionAssignmentJobService`
   - Find/create user logic
   - Subscription creation/update logic
   - Notification sending (SMS/Email)
   - Job progress update
3. Register RabbitMQ consumer in Program.cs
4. Test queue processing

**Files**:
- `PlantAnalysisWorkerService/Jobs/FarmerSubscriptionAssignmentJobService.cs`
- `PlantAnalysisWorkerService/Program.cs`

### Phase 6: Authorization & Claims (Priority: MEDIUM)

**Tasks**:
1. Create SQL script for claims 159-162
2. Execute on dev/staging databases
3. Update API documentation
4. Test authorization on all endpoints

**Files**:
- `claudedocs/AdminOperations/ADD_ADMIN_BULK_SUBSCRIPTION_ASSIGNMENT_CLAIMS.sql`

### Phase 7: Notification Templates (Priority: MEDIUM)

**Tasks**:
1. Create SMS template for subscription activation
2. Create email template for subscription activation
3. Update `IMessagingServiceFactory` if needed

**Files**:
- `Business/Services/Messaging/Templates/SubscriptionActivationSmsTemplate.cs`
- `Business/Services/Messaging/Templates/SubscriptionActivationEmailTemplate.cs`

### Phase 8: Result File Generation (Priority: LOW)

**Tasks**:
1. Create service to generate result Excel file
2. Include success/error status per row
3. Upload to file storage
4. Update job with ResultFileUrl

**Files**:
- `Business/Services/Subscription/BulkSubscriptionResultFileService.cs`

### Phase 9: Documentation (Priority: LOW)

**Tasks**:
1. Create comprehensive API documentation
2. Create deployment checklist
3. Create testing guide
4. Update main admin API documentation

**Files**:
- `claudedocs/AdminOperations/ADMIN_BULK_SUBSCRIPTION_ASSIGNMENT_API.md`
- `claudedocs/AdminOperations/DEPLOYMENT_BULK_SUBSCRIPTION_ASSIGNMENT.md`
- `claudedocs/ADMIN_SPONSOR_VIEW_API_DOCUMENTATION.md` (update)

### Phase 10: Testing & Validation (Priority: HIGH)

**Tasks**:
1. Unit tests for service methods
2. Integration tests for API endpoints
3. End-to-end test with sample Excel file
4. Load testing with 1000+ farmers
5. Error handling validation

---

## Excel Template Example

**File**: `BulkSubscriptionAssignment_Template.xlsx`

| Email | Phone | TierName | Duration | FarmerName | Notes |
|-------|-------|----------|----------|------------|-------|
| farmer1@example.com | 905551234567 | S | 30 | Ali Yılmaz | Q4 campaign |
| farmer2@example.com | 905559876543 | M | 60 | Ayşe Kaya | Premium tier |
| farmer3@example.com | | L | 90 | Mehmet Demir | No phone |
| farmer4@example.com | 905551111111 | | | | Use default tier |

---

## Success Criteria

✅ Excel file parsed correctly with header-based columns
✅ Job created and tracked in database
✅ Messages published to RabbitMQ successfully
✅ Worker processes messages and creates/updates subscriptions
✅ Notifications sent via SMS/Email
✅ Job status updated atomically
✅ Result file generated with success/error per row
✅ Admin can monitor job progress via API
✅ All 4 operation claims (159-162) created and assigned
✅ Error handling prevents job failure on individual farmer errors
✅ Load test: 1000 farmers processed within 15 minutes

---

## Notes

- **No Code Allocation**: Unlike sponsor distribution, this directly creates subscriptions (no intermediate codes)
- **User Creation**: Auto-create users if email not found (set role as Farmer)
- **Subscription Conflicts**: If user has active subscription, extend EndDate or create new (configurable)
- **Notification Flexibility**: Support SMS, Email, or Both
- **Admin Audit**: Track which admin performed the bulk assignment
- **Atomic Progress**: Use database-level increment to prevent race conditions
- **Result File**: Excel file with same structure + Status and ErrorMessage columns

---

**End of Design Document**
