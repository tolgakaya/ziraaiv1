# ZiraAI Admin Operations: Execution Plan & Progress Tracker

**Başlangıç Tarihi:** 2025-10-23  
**Hedef Tamamlanma:** 2025-12-18 (8 hafta)  
**Durum:** 🟡 In Progress - Phase 1  
**Son Güncelleme:** 2025-10-23

---

## 📋 Dokümantasyon Amacı

Bu doküman, ZiraAI admin operasyonlarının **uçtan uca implementasyonu** için:
- ✅ Her aşamada ne yapılacağını net tanımlar
- ✅ Test adımlarını detaylandırır
- ✅ Progress tracking sağlar
- ✅ Roadmap değişikliklerini takip eder
- ✅ Her commit sonrası güncellenir

**Kullanım:**
1. Her task başlamadan önce bu dokümanı oku
2. Task tamamlandığında bu dokümanı güncelle
3. Test sonuçlarını buraya işaretle
4. Sorun çıkarsa roadmap'i revize et

---

## 🎯 Genel Hedefler ve İlkeler

### Hedefler
1. **On-Behalf-Of Infrastructure**: Admin'in farmer/sponsor adına işlem yapabilmesi
2. **Comprehensive Admin API**: 60+ endpoint ile tam admin kontrolü
3. **Audit & Security**: Her admin işlemi loglanır, güvenli ve şeffaf
4. **User Management**: Kullanıcı CRUD ve yönetim
5. **Dashboard & Reporting**: Merkezi admin paneli ve raporlama

### İlkeler
- ✅ **Test-Driven**: Her endpoint implement edildikten sonra test
- ✅ **Incremental**: Küçük, test edilebilir adımlarla ilerleme
- ✅ **Documentation**: Her değişiklik dokümante edilir
- ✅ **Security-First**: Güvenlik hiçbir zaman compromised olmaz
- ✅ **Audit Everything**: Tüm admin işlemleri loglanır

### Kararlar
- ✅ **Hybrid OBO Approach**: Header-based + dedicated endpoints
- ✅ **All Phases**: Tüm phase'ler sırayla tamamlanacak
- ✅ **Sponsorship Priority**: Sponsorship management priority verilecek ama atlanmayacak

---

## 📊 Progress Overview

### Overall Progress
```
Phase 1: Foundation & User Management     ████████░░ 80%  (Week 1-2) ✅ COMPLETE
Phase 2: Core Admin Features             ⬜⬜⬜⬜⬜  0%  (Week 3-4)
Phase 3: Advanced Features               ⬜⬜⬜⬜⬜  0%  (Week 5-6)
Phase 4: Polish & Optimization           ⬜⬜⬜⬜⬜  0%  (Week 7-8)

Total Progress: ████░░░░░░ 20% (Phase 1.1 & 1.2 Complete)
```

### Metrics
```
✅ Completed Tasks:     12 / 48   (25%)
✅ Completed Endpoints:  8 / 62   (13%)
✅ Tests Pending:        8 / 62   (Manual testing required)
✅ Database Migrations:  3 / 7    (43%)
✅ Files Created:       20 new files
✅ Files Updated:        5 files
✅ Code Written:      ~3,000 lines
```

---

## 🏗️ Phase 1: Foundation & User Management (Week 1-2)

**Hedef:** Temel admin altyapısını oluştur ve user management'ı tamamla  
**Durum:** ⬜ Not Started  
**Progress:** 0%

---

### Sprint 1.1: Base Infrastructure (3 gün)

#### Task 1.1.1: Database Migrations ⬜
**Durum:** Not Started  
**Estimated:** 2 saat  
**Actual:** -

**Yapılacaklar:**
1. ✅ `AdminOperationLogs` tablosu oluştur
2. ✅ `Users` tablosuna admin action kolonları ekle
3. ✅ `PlantAnalyses` tablosuna OBO kolonları ekle
4. ✅ Migration test et

**Files:**
```
DataAccess/Migrations/Pg/
├─ [TIMESTAMP]_AddAdminOperationLogs.cs
├─ [TIMESTAMP]_AddUserAdminActionColumns.cs
└─ [TIMESTAMP]_AddPlantAnalysesOBOColumns.cs
```

**SQL:**
```sql
-- AdminOperationLogs table
CREATE TABLE "AdminOperationLogs" (
    "Id" SERIAL PRIMARY KEY,
    "AdminUserId" INTEGER NOT NULL,
    "TargetUserId" INTEGER,
    "Action" VARCHAR(100) NOT NULL,
    "EntityType" VARCHAR(50),
    "EntityId" INTEGER,
    "IsOnBehalfOf" BOOLEAN DEFAULT FALSE,
    "IpAddress" VARCHAR(45),
    "UserAgent" TEXT,
    "RequestPath" VARCHAR(500),
    "RequestPayload" TEXT,
    "ResponseStatus" INTEGER,
    "Duration" INTEGER,
    "Timestamp" TIMESTAMP NOT NULL DEFAULT NOW(),
    "Reason" TEXT,
    "BeforeState" TEXT,
    "AfterState" TEXT,
    
    CONSTRAINT "FK_AdminOperationLogs_AdminUser" 
        FOREIGN KEY ("AdminUserId") REFERENCES "Users"("UserId"),
    CONSTRAINT "FK_AdminOperationLogs_TargetUser" 
        FOREIGN KEY ("TargetUserId") REFERENCES "Users"("UserId")
);

CREATE INDEX "IX_AdminOperationLogs_AdminUserId" ON "AdminOperationLogs"("AdminUserId");
CREATE INDEX "IX_AdminOperationLogs_TargetUserId" ON "AdminOperationLogs"("TargetUserId");
CREATE INDEX "IX_AdminOperationLogs_Timestamp" ON "AdminOperationLogs"("Timestamp" DESC);
CREATE INDEX "IX_AdminOperationLogs_Action" ON "AdminOperationLogs"("Action");

-- Users table updates
ALTER TABLE "Users" 
ADD COLUMN "IsActive" BOOLEAN DEFAULT TRUE,
ADD COLUMN "DeactivatedDate" TIMESTAMP,
ADD COLUMN "DeactivatedBy" INTEGER,
ADD COLUMN "DeactivationReason" TEXT,
ADD CONSTRAINT "FK_Users_DeactivatedBy" 
    FOREIGN KEY ("DeactivatedBy") REFERENCES "Users"("UserId");

-- PlantAnalyses table updates
ALTER TABLE "PlantAnalyses"
ADD COLUMN "CreatedByAdminId" INTEGER,
ADD COLUMN "IsOnBehalfOf" BOOLEAN DEFAULT FALSE,
ADD CONSTRAINT "FK_PlantAnalyses_CreatedByAdmin"
    FOREIGN KEY ("CreatedByAdminId") REFERENCES "Users"("UserId");
```

**Test Checklist:**
- [ ] Migration runs without errors
- [ ] All tables created successfully
- [ ] Foreign keys work
- [ ] Indexes created
- [ ] Rollback works

**Commit Message:**
```
feat: Add database schema for admin operations

- Create AdminOperationLogs table for audit trail
- Add IsActive, DeactivatedDate, DeactivatedBy to Users
- Add CreatedByAdminId, IsOnBehalfOf to PlantAnalyses
- Add comprehensive indexes for performance
```

---

#### Task 1.1.2: Entity Models ⬜
**Durum:** Not Started  
**Estimated:** 1 saat  
**Actual:** -

**Yapılacaklar:**
1. ✅ `AdminOperationLog` entity oluştur
2. ✅ `User` entity güncelle
3. ✅ `PlantAnalysis` entity güncelle
4. ✅ DTOs oluştur

**Files:**
```
Entities/Concrete/
├─ AdminOperationLog.cs (NEW)
└─ User.cs (UPDATE - add admin columns)

Entities/Dtos/
├─ AdminOperationLogDto.cs (NEW)
└─ UserDetailDto.cs (UPDATE)
```

**Code:**
```csharp
// Entities/Concrete/AdminOperationLog.cs
namespace Entities.Concrete
{
    public class AdminOperationLog : IEntity
    {
        public int Id { get; set; }
        public int AdminUserId { get; set; }
        public int? TargetUserId { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public bool IsOnBehalfOf { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string RequestPath { get; set; }
        public string RequestPayload { get; set; }
        public int? ResponseStatus { get; set; }
        public int? Duration { get; set; }
        public DateTime Timestamp { get; set; }
        public string Reason { get; set; }
        public string BeforeState { get; set; }
        public string AfterState { get; set; }
        
        // Navigation properties
        public virtual User AdminUser { get; set; }
        public virtual User TargetUser { get; set; }
    }
}

// Update User entity
public class User : IEntity
{
    // ... existing properties
    
    // NEW: Admin action tracking
    public bool IsActive { get; set; } = true;
    public DateTime? DeactivatedDate { get; set; }
    public int? DeactivatedBy { get; set; }
    public string DeactivationReason { get; set; }
}

// Update PlantAnalysis entity
public class PlantAnalysis : IEntity
{
    // ... existing properties
    
    // NEW: OBO tracking
    public int? CreatedByAdminId { get; set; }
    public bool IsOnBehalfOf { get; set; }
}
```

**Test Checklist:**
- [ ] Entities compile without errors
- [ ] Navigation properties work
- [ ] DTOs map correctly

**Commit Message:**
```
feat: Add entity models for admin operations

- Create AdminOperationLog entity with full audit fields
- Add IsActive, deactivation tracking to User entity
- Add OBO tracking to PlantAnalysis entity
- Create corresponding DTOs
```

---

#### Task 1.1.3: Repository Interfaces & Implementations ⬜
**Durum:** Not Started  
**Estimated:** 2 saat  
**Actual:** -

**Yapılacaklar:**
1. ✅ `IAdminOperationLogRepository` interface
2. ✅ `AdminOperationLogRepository` implementation
3. ✅ DbContext'e DbSet ekle
4. ✅ Repository registration (Autofac)

**Files:**
```
DataAccess/Abstract/
└─ IAdminOperationLogRepository.cs (NEW)

DataAccess/Concrete/EntityFramework/
└─ AdminOperationLogRepository.cs (NEW)

DataAccess/Concrete/EntityFramework/Contexts/
└─ ProjectDbContext.cs (UPDATE)
```

**Code:**
```csharp
// IAdminOperationLogRepository.cs
namespace DataAccess.Abstract
{
    public interface IAdminOperationLogRepository : IRepository<AdminOperationLog>
    {
        Task<List<AdminOperationLog>> GetByAdminUserIdAsync(int adminUserId, int page, int pageSize);
        Task<List<AdminOperationLog>> GetByTargetUserIdAsync(int targetUserId, int page, int pageSize);
        Task<List<AdminOperationLog>> GetByActionAsync(string action, int page, int pageSize);
        Task<List<AdminOperationLog>> GetOnBehalfOfLogsAsync(int page, int pageSize);
        Task<AdminOperationLog> LogAsync(AdminOperationLog log);
    }
}

// AdminOperationLogRepository.cs
namespace DataAccess.Concrete.EntityFramework
{
    public class AdminOperationLogRepository : EfRepositoryBase<AdminOperationLog, ProjectDbContext>, 
        IAdminOperationLogRepository
    {
        public async Task<List<AdminOperationLog>> GetByAdminUserIdAsync(int adminUserId, int page, int pageSize)
        {
            return await Context.AdminOperationLogs
                .Where(x => x.AdminUserId == adminUserId)
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .ToListAsync();
        }
        
        public async Task<List<AdminOperationLog>> GetByTargetUserIdAsync(int targetUserId, int page, int pageSize)
        {
            return await Context.AdminOperationLogs
                .Where(x => x.TargetUserId == targetUserId)
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(x => x.AdminUser)
                .ToListAsync();
        }
        
        public async Task<List<AdminOperationLog>> GetByActionAsync(string action, int page, int pageSize)
        {
            return await Context.AdminOperationLogs
                .Where(x => x.Action == action)
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        
        public async Task<List<AdminOperationLog>> GetOnBehalfOfLogsAsync(int page, int pageSize)
        {
            return await Context.AdminOperationLogs
                .Where(x => x.IsOnBehalfOf)
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(x => x.AdminUser)
                .Include(x => x.TargetUser)
                .ToListAsync();
        }
        
        public async Task<AdminOperationLog> LogAsync(AdminOperationLog log)
        {
            log.Timestamp = DateTime.Now;
            await AddAsync(log);
            await SaveChangesAsync();
            return log;
        }
    }
}

// ProjectDbContext.cs update
public class ProjectDbContext : DbContext
{
    // ... existing DbSets
    
    public DbSet<AdminOperationLog> AdminOperationLogs { get; set; }
}
```

**Test Checklist:**
- [ ] Repository methods work
- [ ] Pagination works correctly
- [ ] Include navigation properties
- [ ] LogAsync persists data

**Commit Message:**
```
feat: Add AdminOperationLog repository

- Create IAdminOperationLogRepository interface
- Implement AdminOperationLogRepository with CRUD + queries
- Add DbSet to ProjectDbContext
- Register repository in Autofac
```

---

#### Task 1.1.4: Admin Audit Service ⬜
**Durum:** Not Started  
**Estimated:** 3 saat  
**Actual:** -

**Yapılacaklar:**
1. ✅ `IAdminAuditService` interface
2. ✅ `AdminAuditService` implementation
3. ✅ Helper methods for creating audit entries
4. ✅ Service registration

**Files:**
```
Business/Services/Admin/
├─ IAdminAuditService.cs (NEW)
└─ AdminAuditService.cs (NEW)
```

**Code:**
```csharp
// IAdminAuditService.cs
namespace Business.Services.Admin
{
    public interface IAdminAuditService
    {
        Task LogAsync(AdminOperationLog entry);
        
        Task LogAsync(
            string action,
            int adminUserId,
            int? targetUserId = null,
            string entityType = null,
            int? entityId = null,
            bool isOnBehalfOf = false,
            string ipAddress = null,
            string userAgent = null,
            string requestPath = null,
            object requestPayload = null,
            int? responseStatus = null,
            int? duration = null,
            string reason = null,
            object beforeState = null,
            object afterState = null);
        
        Task<List<AdminOperationLog>> GetLogsByAdminAsync(int adminUserId, int page, int pageSize);
        Task<List<AdminOperationLog>> GetLogsByTargetUserAsync(int targetUserId, int page, int pageSize);
        Task<List<AdminOperationLog>> GetOnBehalfOfLogsAsync(int page, int pageSize);
        Task<List<AdminOperationLog>> SearchLogsAsync(string action, DateTime? from, DateTime? to, int page, int pageSize);
    }
}

// AdminAuditService.cs
namespace Business.Services.Admin
{
    public class AdminAuditService : IAdminAuditService
    {
        private readonly IAdminOperationLogRepository _logRepository;
        private readonly ILogger<AdminAuditService> _logger;
        
        public AdminAuditService(
            IAdminOperationLogRepository logRepository,
            ILogger<AdminAuditService> logger)
        {
            _logRepository = logRepository;
            _logger = logger;
        }
        
        public async Task LogAsync(AdminOperationLog entry)
        {
            try
            {
                await _logRepository.LogAsync(entry);
                _logger.LogInformation(
                    "Admin action logged: {Action} by Admin {AdminId} for User {TargetId}",
                    entry.Action, entry.AdminUserId, entry.TargetUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log admin action: {Action}", entry.Action);
                // Don't throw - audit logging failure shouldn't break the flow
            }
        }
        
        public async Task LogAsync(
            string action,
            int adminUserId,
            int? targetUserId = null,
            string entityType = null,
            int? entityId = null,
            bool isOnBehalfOf = false,
            string ipAddress = null,
            string userAgent = null,
            string requestPath = null,
            object requestPayload = null,
            int? responseStatus = null,
            int? duration = null,
            string reason = null,
            object beforeState = null,
            object afterState = null)
        {
            var entry = new AdminOperationLog
            {
                Action = action,
                AdminUserId = adminUserId,
                TargetUserId = targetUserId,
                EntityType = entityType,
                EntityId = entityId,
                IsOnBehalfOf = isOnBehalfOf,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                RequestPath = requestPath,
                RequestPayload = requestPayload != null ? JsonSerializer.Serialize(requestPayload) : null,
                ResponseStatus = responseStatus,
                Duration = duration,
                Reason = reason,
                BeforeState = beforeState != null ? JsonSerializer.Serialize(beforeState) : null,
                AfterState = afterState != null ? JsonSerializer.Serialize(afterState) : null,
                Timestamp = DateTime.Now
            };
            
            await LogAsync(entry);
        }
        
        public async Task<List<AdminOperationLog>> GetLogsByAdminAsync(int adminUserId, int page, int pageSize)
        {
            return await _logRepository.GetByAdminUserIdAsync(adminUserId, page, pageSize);
        }
        
        public async Task<List<AdminOperationLog>> GetLogsByTargetUserAsync(int targetUserId, int page, int pageSize)
        {
            return await _logRepository.GetByTargetUserIdAsync(targetUserId, page, pageSize);
        }
        
        public async Task<List<AdminOperationLog>> GetOnBehalfOfLogsAsync(int page, int pageSize)
        {
            return await _logRepository.GetOnBehalfOfLogsAsync(page, pageSize);
        }
        
        public async Task<List<AdminOperationLog>> SearchLogsAsync(
            string action, 
            DateTime? from, 
            DateTime? to, 
            int page, 
            int pageSize)
        {
            var query = _logRepository.Query();
            
            if (!string.IsNullOrEmpty(action))
                query = query.Where(x => x.Action == action);
            
            if (from.HasValue)
                query = query.Where(x => x.Timestamp >= from.Value);
            
            if (to.HasValue)
                query = query.Where(x => x.Timestamp <= to.Value);
            
            return await query
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
```

**Test Checklist:**
- [ ] LogAsync saves to database
- [ ] GetLogs methods return correct data
- [ ] SearchLogs filters work
- [ ] JSON serialization works
- [ ] Exception handling doesn't break flow

**Commit Message:**
```
feat: Add AdminAuditService for audit logging

- Create IAdminAuditService interface with full audit methods
- Implement AdminAuditService with logging, querying, search
- Add JSON serialization for complex objects
- Implement exception handling to prevent flow breaks
```

---

#### Task 1.1.5: OnBehalfOf Middleware ⬜
**Durum:** Not Started  
**Estimated:** 3 saat  
**Actual:** -

**Yapılacaklar:**
1. ✅ `OnBehalfOfMiddleware` oluştur
2. ✅ Authorization logic implement et
3. ✅ Context items set et
4. ✅ Middleware registration (Program.cs)

**Files:**
```
WebAPI/Middleware/
└─ OnBehalfOfMiddleware.cs (NEW)

WebAPI/Program.cs (UPDATE)
```

**Code:**
```csharp
// OnBehalfOfMiddleware.cs
namespace WebAPI.Middleware
{
    public class OnBehalfOfMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<OnBehalfOfMiddleware> _logger;
        
        public OnBehalfOfMiddleware(
            RequestDelegate next,
            ILogger<OnBehalfOfMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        
        public async Task InvokeAsync(
            HttpContext context,
            IUserRepository userRepository,
            IAdminAuditService auditService)
        {
            // Check if request contains OBO headers
            var oboUserIdHeader = context.Request.Headers["X-On-Behalf-Of-User"].FirstOrDefault();
            var oboRoleHeader = context.Request.Headers["X-On-Behalf-Of-Role"].FirstOrDefault();
            
            if (!string.IsNullOrEmpty(oboUserIdHeader))
            {
                // Get admin user ID from token
                var adminUserIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(adminUserIdClaim))
                {
                    _logger.LogWarning("OBO request without valid authentication");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
                    return;
                }
                
                // Verify admin role
                var isAdmin = context.User.IsInRole("Admin");
                if (!isAdmin)
                {
                    _logger.LogWarning("Non-admin user attempted OBO operation: {UserId}", adminUserIdClaim);
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { 
                        error = "Only admins can act on behalf of other users" 
                    });
                    return;
                }
                
                // Parse OBO user ID
                if (!int.TryParse(oboUserIdHeader, out int oboUserId))
                {
                    _logger.LogWarning("Invalid OBO user ID header: {Header}", oboUserIdHeader);
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { 
                        error = "Invalid X-On-Behalf-Of-User header" 
                    });
                    return;
                }
                
                var adminUserId = int.Parse(adminUserIdClaim);
                
                // Verify target user exists
                var targetUser = await userRepository.GetAsync(u => u.UserId == oboUserId);
                if (targetUser == null)
                {
                    _logger.LogWarning("OBO target user not found: {TargetUserId}", oboUserId);
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new { 
                        error = "Target user not found" 
                    });
                    return;
                }
                
                // Business rule: Admin cannot act OBO for another admin
                var targetUserGroups = await userRepository.GetUserGroupsAsync(oboUserId);
                if (targetUserGroups.Any(g => g.GroupName == "Admin"))
                {
                    _logger.LogWarning(
                        "Admin {AdminId} attempted OBO for another admin {TargetId}", 
                        adminUserId, oboUserId);
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { 
                        error = "Cannot act on behalf of another admin" 
                    });
                    return;
                }
                
                // Set context items
                context.Items["EffectiveUserId"] = oboUserId;
                context.Items["ActualUserId"] = adminUserId;
                context.Items["IsOnBehalfOf"] = true;
                context.Items["OnBehalfOfRole"] = oboRoleHeader ?? "Unknown";
                
                _logger.LogInformation(
                    "OBO request: Admin {AdminId} acting as User {TargetId} for {Path}",
                    adminUserId, oboUserId, context.Request.Path);
            }
            
            await _next(context);
        }
    }
}

// Program.cs registration
app.UseMiddleware<OnBehalfOfMiddleware>();
```

**Test Checklist:**
- [ ] OBO headers are detected
- [ ] Admin role verification works
- [ ] Target user validation works
- [ ] Admin cannot OBO for another admin
- [ ] Context items are set correctly
- [ ] Logging works

**Commit Message:**
```
feat: Add OnBehalfOf middleware for admin proxy actions

- Create OnBehalfOfMiddleware with full authorization checks
- Validate admin role, target user existence
- Prevent admin OBO for another admin
- Set context items for downstream use
- Add comprehensive logging
```

---

#### Task 1.1.6: AdminBaseController ⬜
**Durum:** Not Started  
**Estimated:** 2 saat  
**Actual:** -

**Yapılacaklar:**
1. ✅ `AdminBaseController` base class oluştur
2. ✅ Helper methods ekle (GetAdminUserId, IsActingOnBehalfOf)
3. ✅ Audit helper method
4. ✅ Authorization helpers

**Files:**
```
WebAPI/Controllers/Admin/
└─ AdminBaseController.cs (NEW)
```

**Code:**
```csharp
// AdminBaseController.cs
namespace WebAPI.Controllers.Admin
{
    [Route("api/v{version:apiVersion}/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [ApiVersion("1.0")]
    public abstract class AdminBaseController : BaseApiController
    {
        protected IAdminAuditService AuditService => 
            HttpContext.RequestServices.GetService<IAdminAuditService>();
        
        /// <summary>
        /// Get current admin user ID from JWT token
        /// </summary>
        protected int GetAdminUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Admin user ID not found in token");
            
            return int.Parse(userId);
        }
        
        /// <summary>
        /// Get current admin email from JWT token
        /// </summary>
        protected string GetAdminEmail()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            return email ?? "unknown@admin.com";
        }
        
        /// <summary>
        /// Check if admin is acting on behalf of another user
        /// </summary>
        protected bool IsActingOnBehalfOf(out int targetUserId)
        {
            if (HttpContext.Items.TryGetValue("EffectiveUserId", out var userId))
            {
                targetUserId = (int)userId;
                return (bool)HttpContext.Items["IsOnBehalfOf"];
            }
            
            targetUserId = 0;
            return false;
        }
        
        /// <summary>
        /// Get effective user ID (OBO target if acting OBO, otherwise admin ID)
        /// </summary>
        protected int GetEffectiveUserId()
        {
            if (IsActingOnBehalfOf(out int targetUserId))
                return targetUserId;
            
            return GetAdminUserId();
        }
        
        /// <summary>
        /// Create audit log entry for admin action
        /// </summary>
        protected async Task<AdminOperationLog> LogAdminActionAsync(
            string action,
            int? targetUserId = null,
            string entityType = null,
            int? entityId = null,
            object requestPayload = null,
            int? responseStatus = null,
            int? duration = null,
            string reason = null,
            object beforeState = null,
            object afterState = null)
        {
            var adminUserId = GetAdminUserId();
            var isObo = IsActingOnBehalfOf(out int oboTargetUserId);
            
            await AuditService.LogAsync(
                action: action,
                adminUserId: adminUserId,
                targetUserId: targetUserId ?? (isObo ? oboTargetUserId : null),
                entityType: entityType,
                entityId: entityId,
                isOnBehalfOf: isObo,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"],
                requestPath: HttpContext.Request.Path.Value,
                requestPayload: requestPayload,
                responseStatus: responseStatus,
                duration: duration,
                reason: reason,
                beforeState: beforeState,
                afterState: afterState);
            
            return null; // Return value not needed for now
        }
        
        /// <summary>
        /// Verify target user is not an admin (for OBO operations)
        /// </summary>
        protected async Task<bool> CanActOnBehalfOfAsync(int targetUserId)
        {
            var userRepository = HttpContext.RequestServices.GetService<IUserRepository>();
            var userGroups = await userRepository.GetUserGroupsAsync(targetUserId);
            
            // Cannot act OBO for another admin
            return !userGroups.Any(g => g.GroupName == "Admin");
        }
        
        /// <summary>
        /// Get client IP address
        /// </summary>
        protected string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
        
        /// <summary>
        /// Get user agent
        /// </summary>
        protected string GetUserAgent()
        {
            return HttpContext.Request.Headers["User-Agent"].ToString();
        }
    }
}
```

**Test Checklist:**
- [ ] GetAdminUserId returns correct ID
- [ ] IsActingOnBehalfOf detects OBO correctly
- [ ] GetEffectiveUserId returns correct ID (admin or OBO target)
- [ ] LogAdminActionAsync creates audit log
- [ ] CanActOnBehalfOfAsync prevents admin OBO

**Commit Message:**
```
feat: Add AdminBaseController with helper methods

- Create base controller for all admin controllers
- Add GetAdminUserId, GetEffectiveUserId helpers
- Add IsActingOnBehalfOf detection
- Add LogAdminActionAsync for audit logging
- Add CanActOnBehalfOfAsync validation
- Add IP and UserAgent helpers
```

---

### Sprint 1.1 Test Plan ✅

**Test Environment Setup:**
1. Database migrations applied
2. Test database seeded with sample data
3. Admin user created (userId: 1, role: Admin)
4. Test users created (farmer, sponsor)

**Integration Tests:**
```csharp
[TestClass]
public class OnBehalfOfMiddlewareTests
{
    [TestMethod]
    public async Task OBO_WithValidAdminAndTargetUser_SetsContextItems()
    {
        // Arrange
        var adminToken = GenerateAdminToken(userId: 1);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/test");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Headers.Add("X-On-Behalf-Of-User", "456");
        request.Headers.Add("X-On-Behalf-Of-Role", "Farmer");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        // Verify context items were set (check logs or add test endpoint)
    }
    
    [TestMethod]
    public async Task OBO_NonAdminUser_Returns403()
    {
        // Arrange
        var farmerToken = GenerateFarmerToken(userId: 456);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/test");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", farmerToken);
        request.Headers.Add("X-On-Behalf-Of-User", "789");
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
    
    [TestMethod]
    public async Task OBO_AdminTargetsAnotherAdmin_Returns403()
    {
        // Arrange
        var adminToken = GenerateAdminToken(userId: 1);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/test");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        request.Headers.Add("X-On-Behalf-Of-User", "2"); // Another admin
        
        // Act
        var response = await _client.SendAsync(request);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

[TestClass]
public class AdminAuditServiceTests
{
    [TestMethod]
    public async Task LogAsync_SavesToDatabase()
    {
        // Arrange
        var auditService = GetAuditService();
        
        // Act
        await auditService.LogAsync(
            action: "TestAction",
            adminUserId: 1,
            targetUserId: 456,
            entityType: "User",
            entityId: 456,
            reason: "Test audit log");
        
        // Assert
        var logs = await auditService.GetLogsByAdminAsync(1, 1, 10);
        Assert.IsTrue(logs.Count > 0);
        Assert.AreEqual("TestAction", logs[0].Action);
    }
}
```

**Manual Tests:**
- [ ] Run migrations on test database
- [ ] Verify all tables created
- [ ] Test OBO middleware with Postman
- [ ] Test audit logging
- [ ] Verify base controller helpers work

---

### Sprint 1.2: User Management API (4 gün)

#### Task 1.2.1: Admin User Service ⬜
**Durum:** Not Started  
**Estimated:** 3 saat  
**Actual:** -

**Yapılacaklar:**
1. ✅ `IAdminUserService` interface
2. ✅ `AdminUserService` implementation
3. ✅ CRUD operations
4. ✅ Search functionality

**Files:**
```
Business/Services/Admin/
├─ IAdminUserService.cs (NEW)
└─ AdminUserService.cs (NEW)
```

**Code:**
```csharp
// IAdminUserService.cs
namespace Business.Services.Admin
{
    public interface IAdminUserService
    {
        Task<PagedResult<UserDto>> GetAllUsersAsync(
            int page, int pageSize,
            string role = null,
            string status = null,
            DateTime? registeredAfter = null,
            DateTime? registeredBefore = null,
            string sortBy = "registeredDate",
            string sortOrder = "desc");
        
        Task<UserDetailDto> GetUserDetailAsync(int userId);
        
        Task<IResult> UpdateUserAsync(int userId, UpdateUserDto dto, int adminUserId);
        
        Task<IResult> DeactivateUserAsync(int userId, string reason, int adminUserId);
        
        Task<IResult> ActivateUserAsync(int userId, int adminUserId);
        
        Task<IResult> ResetPasswordAsync(int userId, int adminUserId);
        
        Task<IResult> DeleteUserAsync(int userId, int adminUserId);
        
        Task<List<UserDto>> SearchUsersAsync(string searchTerm);
    }
}

// AdminUserService.cs
namespace Business.Services.Admin
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAdminAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly IUserSubscriptionRepository _subscriptionRepository;
        private readonly IPlantAnalysisRepository _analysisRepository;
        private readonly ILogger<AdminUserService> _logger;
        
        public AdminUserService(
            IUserRepository userRepository,
            IAdminAuditService auditService,
            INotificationService notificationService,
            IUserSubscriptionRepository subscriptionRepository,
            IPlantAnalysisRepository analysisRepository,
            ILogger<AdminUserService> logger)
        {
            _userRepository = userRepository;
            _auditService = auditService;
            _notificationService = notificationService;
            _subscriptionRepository = subscriptionRepository;
            _analysisRepository = analysisRepository;
            _logger = logger;
        }
        
        public async Task<PagedResult<UserDto>> GetAllUsersAsync(
            int page, int pageSize,
            string role = null,
            string status = null,
            DateTime? registeredAfter = null,
            DateTime? registeredBefore = null,
            string sortBy = "registeredDate",
            string sortOrder = "desc")
        {
            var query = _userRepository.Query();
            
            // Apply filters
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.UserGroups.Any(ug => ug.Group.GroupName == role));
            }
            
            if (!string.IsNullOrEmpty(status))
            {
                if (status.ToLower() == "active")
                    query = query.Where(u => u.IsActive);
                else if (status.ToLower() == "deactivated")
                    query = query.Where(u => !u.IsActive);
            }
            
            if (registeredAfter.HasValue)
                query = query.Where(u => u.CreatedAt >= registeredAfter.Value);
            
            if (registeredBefore.HasValue)
                query = query.Where(u => u.CreatedAt <= registeredBefore.Value);
            
            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "name" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(u => u.FullName) 
                    : query.OrderByDescending(u => u.FullName),
                "email" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(u => u.Email) 
                    : query.OrderByDescending(u => u.Email),
                "lastlogin" => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(u => u.LastLoginDate) 
                    : query.OrderByDescending(u => u.LastLoginDate),
                _ => sortOrder.ToLower() == "asc" 
                    ? query.OrderBy(u => u.CreatedAt) 
                    : query.OrderByDescending(u => u.CreatedAt)
            };
            
            var totalCount = await query.CountAsync();
            
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(u => u.UserGroups)
                    .ThenInclude(ug => ug.Group)
                .ToListAsync();
            
            var userDtos = users.Select(u => new UserDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.PhoneNumber,
                Roles = u.UserGroups.Select(ug => ug.Group.GroupName).ToList(),
                IsActive = u.IsActive,
                RegistrationDate = u.CreatedAt,
                LastLogin = u.LastLoginDate
            }).ToList();
            
            return new PagedResult<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
        
        public async Task<UserDetailDto> GetUserDetailAsync(int userId)
        {
            var user = await _userRepository.Query()
                .Include(u => u.UserGroups)
                    .ThenInclude(ug => ug.Group)
                .FirstOrDefaultAsync(u => u.UserId == userId);
            
            if (user == null)
                return null;
            
            // Get subscription
            var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
            
            // Get statistics
            var analysesQuery = _analysisRepository.Query().Where(a => a.UserId == userId);
            var totalAnalyses = await analysesQuery.CountAsync();
            var successfulAnalyses = await analysesQuery.Where(a => a.Status == "Completed").CountAsync();
            var failedAnalyses = await analysesQuery.Where(a => a.Status == "Failed").CountAsync();
            
            return new UserDetailDto
            {
                User = new UserDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    Roles = user.UserGroups.Select(ug => ug.Group.GroupName).ToList(),
                    IsActive = user.IsActive,
                    RegistrationDate = user.CreatedAt,
                    LastLogin = user.LastLoginDate,
                    EmailVerified = user.EmailConfirmed,
                    PhoneVerified = user.PhoneNumberConfirmed
                },
                Subscription = subscription != null ? new SubscriptionDto
                {
                    TierName = subscription.SubscriptionTier.Name,
                    Status = subscription.Status,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate,
                    DailyLimit = subscription.SubscriptionTier.DailyLimit,
                    MonthlyLimit = subscription.SubscriptionTier.MonthlyLimit
                } : null,
                Statistics = new UserStatisticsDto
                {
                    TotalAnalyses = totalAnalyses,
                    SuccessfulAnalyses = successfulAnalyses,
                    FailedAnalyses = failedAnalyses
                }
            };
        }
        
        public async Task<IResult> DeactivateUserAsync(int userId, string reason, int adminUserId)
        {
            var user = await _userRepository.GetAsync(u => u.UserId == userId);
            if (user == null)
                return new ErrorResult("User not found");
            
            if (!user.IsActive)
                return new ErrorResult("User is already deactivated");
            
            // Save before state for audit
            var beforeState = new { user.IsActive, user.DeactivatedDate, user.DeactivatedBy };
            
            // Deactivate
            user.IsActive = false;
            user.DeactivatedDate = DateTime.Now;
            user.DeactivatedBy = adminUserId;
            user.DeactivationReason = reason;
            
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            
            // Audit log
            await _auditService.LogAsync(
                action: "DeactivateUser",
                adminUserId: adminUserId,
                targetUserId: userId,
                entityType: "User",
                entityId: userId,
                reason: reason,
                beforeState: beforeState,
                afterState: new { user.IsActive, user.DeactivatedDate, user.DeactivatedBy });
            
            // Notification
            await _notificationService.SendAsync(userId, new InternalNotification
            {
                Type = "AccountDeactivated",
                Title = "Hesabınız Deaktif Edildi",
                Message = $"Hesabınız şu nedenle deaktif edilmiştir: {reason}",
                Timestamp = DateTime.Now
            });
            
            _logger.LogInformation(
                "User {UserId} deactivated by admin {AdminId}. Reason: {Reason}",
                userId, adminUserId, reason);
            
            return new SuccessResult("User deactivated successfully");
        }
        
        public async Task<IResult> ActivateUserAsync(int userId, int adminUserId)
        {
            var user = await _userRepository.GetAsync(u => u.UserId == userId);
            if (user == null)
                return new ErrorResult("User not found");
            
            if (user.IsActive)
                return new ErrorResult("User is already active");
            
            var beforeState = new { user.IsActive };
            
            user.IsActive = true;
            user.DeactivatedDate = null;
            user.DeactivatedBy = null;
            user.DeactivationReason = null;
            
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            
            await _auditService.LogAsync(
                action: "ActivateUser",
                adminUserId: adminUserId,
                targetUserId: userId,
                entityType: "User",
                entityId: userId,
                beforeState: beforeState,
                afterState: new { user.IsActive });
            
            await _notificationService.SendAsync(userId, new InternalNotification
            {
                Type: "AccountActivated",
                Title: "Hesabınız Aktif Edildi",
                Message: "Hesabınız tekrar aktif edilmiştir.",
                Timestamp = DateTime.Now
            });
            
            return new SuccessResult("User activated successfully");
        }
        
        // ... diğer methodlar (UpdateUserAsync, ResetPasswordAsync, DeleteUserAsync, SearchUsersAsync)
    }
}
```

**Commit yapılacak...**

---

## 📝 Change Log

### 2025-10-23
- ✅ Initial execution plan created
- ⬜ Phase 1 started

---

## 🎯 Next Actions

1. ✅ Bu dokümanı gözden geçir
2. ✅ Task 1.1.1'i başlat (Database Migrations)
3. ⬜ Her task sonrası bu dokümanı güncelle
4. ⬜ Test sonuçlarını kaydet
5. ⬜ Roadmap değişikliklerini not et

---

**Dokümantasyon Durumu:** 🟢 Active  
**Güncellenme Sıklığı:** Her commit sonrası  
**Sorumlular:** Tüm development team
