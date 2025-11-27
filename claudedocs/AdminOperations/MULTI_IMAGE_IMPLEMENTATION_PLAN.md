# Multi-Image Plant Analysis Implementation Plan

**Feature**: Multi-image async plant analysis endpoint
**Branch**: `feature/multi-image-analysis`
**Base**: `staging`
**Date Started**: 2025-01-27
**Status**: 🟡 Planning Phase

---

## 📋 Overview

Add multi-image support for async plant analysis. Users can submit up to 5 images (main + 4 specialized views) for more comprehensive AI analysis.

### Key Requirements
- ✅ Async endpoint ONLY (`/analyze-multi-async`)
- ✅ Same flow as single image async
- ✅ Support 5 image types: Main, LeafTop, LeafBottom, PlantOverview, Root
- ✅ Each image: Process → Optimize → Upload → Store URL
- ✅ End-to-end field propagation through entire system
- ✅ Backward compatible (existing single-image flow unchanged)

---

## 🎯 Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Controller: PlantAnalysesController                      │
│    POST /api/v1/plantanalyses/analyze-multi-async           │
│    - Validate 5 base64 images                                │
│    - Check subscription quota                                │
│    - Call PlantAnalysisMultiImageAsyncService                │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Service: PlantAnalysisMultiImageAsyncService             │
│    - Process & optimize each image (5x)                      │
│    - Upload to storage (5x URLs)                             │
│    - Create database record (5 URL fields)                   │
│    - Build PlantAnalysisMultiImageAsyncRequestDto            │
│    - Publish to RabbitMQ queue                               │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. RabbitMQ Queue: "plant-analysis-multi-image-request"     │
│    Message contains 5 image URLs + all metadata             │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. Worker: PlantAnalysisMultiImageJobService                │
│    - Consume message from queue                              │
│    - Call N8N webhook with 5 image URLs                      │
│    - Receive AI analysis results                             │
│    - Update database with results                            │
│    - Capture sponsor attribution                             │
│    - Process referral rewards                                │
│    - Send notification                                       │
└─────────────────────────────────────────────────────────────┘
```

---

## 📦 Phase Breakdown

### ✅ Phase 0: Analysis & Planning (COMPLETED)
- [x] Analyze single image async flow
- [x] Analyze worker service processing
- [x] Understand RabbitMQ message structure
- [x] Review SecuredOperation guide
- [x] Create implementation plan
- [x] Write development rules to memory

### 🔄 Phase 1: DTOs & Request/Response Models (IN PROGRESS)
**Status**: Planning

**Files to Create/Modify**:
1. `Entities/Dtos/PlantAnalysisMultiImageRequestDto.cs` - NEW
2. `Entities/Dtos/PlantAnalysisMultiImageAsyncRequestDto.cs` - NEW (for queue)
3. `Entities/Dtos/PlantAnalysisMultiImageResponseDto.cs` - NEW (if needed)

**Structure**:
```csharp
public class PlantAnalysisMultiImageRequestDto : IDto
{
    // Main image (required)
    [Required] public string Image { get; set; }

    // Additional images (optional)
    public string LeafTopImage { get; set; }
    public string LeafBottomImage { get; set; }
    public string PlantOverviewImage { get; set; }
    public string RootImage { get; set; }

    // All existing fields from PlantAnalysisRequestDto
    public int? UserId { get; set; }
    public string FarmerId { get; set; }
    public string SponsorId { get; set; }
    // ... (all other fields same as single image)
}

public class PlantAnalysisMultiImageAsyncRequestDto
{
    // Image URLs (after upload)
    public string ImageUrl { get; set; }
    public string LeafTopUrl { get; set; }
    public string LeafBottomUrl { get; set; }
    public string PlantOverviewUrl { get; set; }
    public string RootUrl { get; set; }

    // All existing fields from PlantAnalysisAsyncRequestDto
    public int? UserId { get; set; }
    public string FarmerId { get; set; }
    // ... (all other fields)
}
```

**Tasks**:
- [ ] Create `PlantAnalysisMultiImageRequestDto.cs`
- [ ] Create `PlantAnalysisMultiImageAsyncRequestDto.cs`
- [ ] Add validation attributes
- [ ] Build & verify no errors

---

### ⏳ Phase 2: Database Schema Changes
**Status**: Not Started

**SQL Script Required**: `12_add_multi_image_fields.sql`

**PlantAnalyses Table Modifications**:
```sql
ALTER TABLE "PlantAnalyses"
ADD COLUMN "LeafTopUrl" TEXT,
ADD COLUMN "LeafBottomUrl" TEXT,
ADD COLUMN "PlantOverviewUrl" TEXT,
ADD COLUMN "RootUrl" TEXT;

COMMENT ON COLUMN "PlantAnalyses"."LeafTopUrl" IS 'URL to leaf top view image';
COMMENT ON COLUMN "PlantAnalyses"."LeafBottomUrl" IS 'URL to leaf bottom view image';
COMMENT ON COLUMN "PlantAnalyses"."PlantOverviewUrl" IS 'URL to full plant overview image';
COMMENT ON COLUMN "PlantAnalyses"."RootUrl" IS 'URL to root system image';
```

**Entity Update**:
- Update `Entities/Concrete/PlantAnalysis.cs`
- Add 4 new string properties

**Tasks**:
- [ ] Create SQL migration script
- [ ] Update PlantAnalysis entity
- [ ] Build & verify no errors
- [ ] User executes SQL script in staging

---

### ⏳ Phase 3: Service Layer - Multi-Image Processing
**Status**: Not Started

**File to Create**: `Business/Services/PlantAnalysis/PlantAnalysisMultiImageAsyncService.cs`

**Interface**: `Business/Services/PlantAnalysis/IPlantAnalysisMultiImageAsyncService.cs`

**Key Methods**:
```csharp
public interface IPlantAnalysisMultiImageAsyncService
{
    Task<string> QueueMultiImagePlantAnalysisAsync(PlantAnalysisMultiImageRequestDto request);
    Task<bool> IsQueueHealthyAsync();
}
```

**Implementation Logic**:
```csharp
public async Task<string> QueueMultiImagePlantAnalysisAsync(PlantAnalysisMultiImageRequestDto request)
{
    // 1. Generate IDs
    var correlationId = Guid.NewGuid().ToString("N");
    var analysisId = $"async_multi_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{correlationId[..8]}";

    // 2. Process & upload MAIN image (required)
    var mainImageUrl = await ProcessAndUploadImageAsync(request.Image, analysisId, "main");

    // 3. Process & upload additional images (optional)
    var leafTopUrl = !string.IsNullOrEmpty(request.LeafTopImage)
        ? await ProcessAndUploadImageAsync(request.LeafTopImage, analysisId, "leaf-top")
        : null;

    var leafBottomUrl = !string.IsNullOrEmpty(request.LeafBottomImage)
        ? await ProcessAndUploadImageAsync(request.LeafBottomImage, analysisId, "leaf-bottom")
        : null;

    var plantOverviewUrl = !string.IsNullOrEmpty(request.PlantOverviewImage)
        ? await ProcessAndUploadImageAsync(request.PlantOverviewImage, analysisId, "plant-overview")
        : null;

    var rootUrl = !string.IsNullOrEmpty(request.RootImage)
        ? await ProcessAndUploadImageAsync(request.RootImage, analysisId, "root")
        : null;

    // 4. Create database record
    var plantAnalysis = new PlantAnalysis
    {
        AnalysisId = analysisId,
        ImageUrl = mainImageUrl,
        LeafTopUrl = leafTopUrl,
        LeafBottomUrl = leafBottomUrl,
        PlantOverviewUrl = plantOverviewUrl,
        RootUrl = rootUrl,
        AnalysisStatus = "Processing",
        // ... all other fields
    };

    _plantAnalysisRepository.Add(plantAnalysis);
    await _plantAnalysisRepository.SaveChangesAsync();

    // 5. Build queue message
    var asyncRequest = new PlantAnalysisMultiImageAsyncRequestDto
    {
        ImageUrl = mainImageUrl,
        LeafTopUrl = leafTopUrl,
        LeafBottomUrl = leafBottomUrl,
        PlantOverviewUrl = plantOverviewUrl,
        RootUrl = rootUrl,
        AnalysisId = analysisId,
        CorrelationId = correlationId,
        // ... all other fields
    };

    // 6. Publish to queue
    await _messageQueueService.PublishAsync("plant-analysis-multi-image-request", asyncRequest, correlationId);

    return analysisId;
}

private async Task<string> ProcessAndUploadImageAsync(string base64Image, string analysisId, string suffix)
{
    // Process (optimize to 100KB)
    var processedImage = await ProcessImageForAIAsync(base64Image);

    // Upload to storage
    var imageUrl = await _fileStorageService.UploadImageFromDataUriAsync(
        processedImage,
        $"{analysisId}_{suffix}",
        "plant-images");

    return imageUrl;
}
```

**Tasks**:
- [ ] Create interface
- [ ] Create service implementation
- [ ] Register in DI container
- [ ] Build & verify no errors

---

### ⏳ Phase 4: Controller Endpoint
**Status**: Not Started

**File**: `WebAPI/Controllers/PlantAnalysesController.cs`

**New Endpoint**:
```csharp
/// <summary>
/// Queue a multi-image plant analysis for async processing
/// Supports up to 5 images: main + 4 specialized views
/// </summary>
[HttpPost("analyze-multi-async")]
[Authorize(Roles = "Farmer,Admin")]
public async Task<IActionResult> AnalyzeMultiImageAsync(
    [FromBody] PlantAnalysisMultiImageRequestDto request)
{
    // 1. Validate model
    if (!ModelState.IsValid) return BadRequest(...);

    // 2. Check queue health
    if (!await _multiImageAsyncService.IsQueueHealthyAsync())
        return StatusCode(503, ...);

    // 3. Get user ID
    var userId = GetUserId();
    if (!userId.HasValue) return Unauthorized();

    // 4. Check subscription quota
    var quotaValidation = await _subscriptionValidationService
        .ValidateAndLogUsageAsync(userId.Value, HttpContext.Request.Path.Value, "POST");
    if (!quotaValidation.Success) return StatusCode(403, ...);

    // 5. Auto-determine FarmerId, SponsorId, etc.
    request.UserId = userId;
    request.FarmerId = $"F{userId.Value:D3}";
    // ... sponsor details

    // 6. Queue analysis
    var analysisId = await _multiImageAsyncService.QueueMultiImagePlantAnalysisAsync(request);

    // 7. Increment usage
    await _subscriptionValidationService.IncrementUsageAsync(userId.Value);

    // 8. Process referral validation
    try
    {
        await _referralTrackingService.ValidateReferralAsync(userId.Value);
    }
    catch { /* log but don't fail */ }

    return Accepted(new
    {
        success = true,
        message = "Multi-image plant analysis queued",
        analysis_id = analysisId,
        estimated_processing_time = "3-7 minutes",
        status_check_endpoint = $"/api/plantanalyses/status/{analysisId}"
    });
}
```

**Tasks**:
- [ ] Add endpoint method
- [ ] Inject `IPlantAnalysisMultiImageAsyncService`
- [ ] Build & verify no errors

---

### ⏳ Phase 5: Worker Service Processing
**Status**: Not Started

**File to Create**: `PlantAnalysisWorkerService/Jobs/PlantAnalysisMultiImageJobService.cs`

**Interface**: `IPlantAnalysisMultiImageJobService`

**Key Method**:
```csharp
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
public async Task ProcessMultiImageAnalysisResultAsync(
    PlantAnalysisAsyncResponseDto result,
    string correlationId)
{
    // 1. Find existing analysis
    var analysis = await _plantAnalysisRepository
        .GetAsync(x => x.AnalysisId == result.AnalysisId);

    // 2. Update with AI results (same as single image)
    analysis.AnalysisStatus = "Completed";
    // ... all AI fields

    // 3. Capture sponsor attribution
    await CaptureActiveSponsorAsync(analysis, analysis.UserId);

    // 4. Save
    _plantAnalysisRepository.Update(analysis);
    await _plantAnalysisRepository.SaveChangesAsync();

    // 5. Referral validation & reward
    // ... (same as single image)

    // 6. Send notification
    BackgroundJob.Enqueue(() => SendNotificationAsync(result));
}
```

**Queue Consumer Setup**:
- Update worker service to consume from `plant-analysis-multi-image-request` queue
- Route multi-image requests to new job service

**Tasks**:
- [ ] Create interface
- [ ] Create job service
- [ ] Add queue consumer
- [ ] Register in DI
- [ ] Build & verify no errors

---

### ⏳ Phase 6: RabbitMQ Configuration
**Status**: Not Started

**Files to Update**:
1. `appsettings.Development.json`
2. `appsettings.Staging.json`
3. `appsettings.Production.json`

**Queue Configuration**:
```json
"RabbitMQ": {
  "Queues": {
    "PlantAnalysisRequest": "plant-analysis-request",
    "PlantAnalysisMultiImageRequest": "plant-analysis-multi-image-request",
    "PlantAnalysisResults": "plant-analysis-results"
  }
}
```

**Tasks**:
- [ ] Add queue name to config files
- [ ] Update RabbitMQOptions class if needed
- [ ] Build & verify no errors

---

### ⏳ Phase 7: SecuredOperation (If Admin Endpoint)
**Status**: Not Started (Skip for Farmer endpoint)

**Note**: Current endpoint is for Farmers, so NO SecuredOperation needed.
If admin version is required later:

**Tasks for Admin Endpoint**:
- [ ] Create handler with "Handler" suffix
- [ ] Add `[SecuredOperation(Priority = 1)]`
- [ ] Calculate claim name (remove "Handler")
- [ ] Create SQL script for OperationClaim
- [ ] Assign to Admin group
- [ ] User executes SQL
- [ ] Logout/Login to refresh cache

---

### ⏳ Phase 8: Testing & Validation
**Status**: Not Started

**Test Scenarios**:
1. **Single image only** (backward compatibility)
   - [ ] Submit with only main image
   - [ ] Verify processes correctly
   - [ ] Check database: Other URL fields are NULL

2. **All 5 images**
   - [ ] Submit with all images
   - [ ] Verify all 5 URLs stored
   - [ ] Check worker processes all images
   - [ ] Verify AI analysis uses all images

3. **Partial images**
   - [ ] Main + LeafTop only
   - [ ] Main + PlantOverview + Root
   - [ ] Verify flexible handling

4. **Subscription quota**
   - [ ] Verify quota check works
   - [ ] Verify usage increment
   - [ ] Test over-quota rejection

5. **Referral validation**
   - [ ] First analysis triggers referral
   - [ ] Reward processed correctly

6. **Notification**
   - [ ] SignalR notification sent
   - [ ] Correct deep link
   - [ ] Mobile app receives notification

**Tasks**:
- [ ] Create Postman collection
- [ ] Test all scenarios
- [ ] Verify database state
- [ ] Check worker logs
- [ ] Validate notification delivery

---

### ⏳ Phase 9: API Documentation
**Status**: Not Started

**Document to Create**: `claudedocs/AdminOperations/API_MultiImageAnalysis.md`

**Contents**:
- Endpoint URL, method
- Authentication requirements
- Request payload structure (JSON examples)
- Response structure (success + error)
- Image size limits
- Processing time estimates
- Status check endpoint
- Mobile integration guide

**Tasks**:
- [ ] Create comprehensive API doc
- [ ] Include cURL examples
- [ ] Add Postman collection
- [ ] Share with mobile/frontend teams

---

### ⏳ Phase 10: Deployment & Monitoring
**Status**: Not Started

**Pre-Deployment Checklist**:
- [ ] All phases completed
- [ ] Build successful (no errors)
- [ ] SQL migration script ready
- [ ] All tests passing
- [ ] API documentation complete

**Deployment Steps**:
1. [ ] Push to `feature/multi-image-analysis`
2. [ ] Railway auto-deploys to staging
3. [ ] Execute SQL migration script
4. [ ] Verify database schema
5. [ ] Test endpoint in staging
6. [ ] Monitor RabbitMQ queues
7. [ ] Monitor worker logs
8. [ ] Create PR to staging (if separate branch)

**Post-Deployment Monitoring**:
- [ ] Check worker service logs
- [ ] Monitor RabbitMQ queue depth
- [ ] Track analysis completion rate
- [ ] Monitor error rates
- [ ] Verify notification delivery

---

## 📊 Progress Tracking

| Phase | Status | Completion | Notes |
|-------|--------|------------|-------|
| Phase 0: Analysis & Planning | ✅ Complete | 100% | Development rules memorized |
| Phase 1: DTOs & Models | 🔄 In Progress | 0% | Starting now |
| Phase 2: Database Schema | ⏳ Not Started | 0% | - |
| Phase 3: Service Layer | ⏳ Not Started | 0% | - |
| Phase 4: Controller | ⏳ Not Started | 0% | - |
| Phase 5: Worker Service | ⏳ Not Started | 0% | - |
| Phase 6: RabbitMQ Config | ⏳ Not Started | 0% | - |
| Phase 7: SecuredOperation | ⏳ Skipped | N/A | Farmer endpoint |
| Phase 8: Testing | ⏳ Not Started | 0% | - |
| Phase 9: API Documentation | ⏳ Not Started | 0% | - |
| Phase 10: Deployment | ⏳ Not Started | 0% | - |

**Overall Progress**: 10% (Phase 0 complete)

---

## 🔧 Technical Decisions

### 1. Image Field Naming
- `ImageUrl` - Main image (existing field, required)
- `LeafTopUrl` - Top view of leaf (optional)
- `LeafBottomUrl` - Bottom view of leaf (optional)
- `PlantOverviewUrl` - Full plant view (optional)
- `RootUrl` - Root system view (optional)

### 2. Queue Strategy
- Separate queue: `plant-analysis-multi-image-request`
- Separate DTO: `PlantAnalysisMultiImageAsyncRequestDto`
- Reuse result queue: `plant-analysis-results`

### 3. Database Strategy
- Add 4 new columns to existing `PlantAnalyses` table
- No separate table (keeps single/multi unified)
- NULL values for single-image analyses (backward compatible)

### 4. Processing Strategy
- Sequential image processing (5x ProcessAndUploadImageAsync)
- Each image independently optimized to 100KB
- Each image gets unique suffix in storage

### 5. Backward Compatibility
- Existing single-image endpoint UNCHANGED
- Multi-image is NEW endpoint
- Worker can handle both message types
- Database schema backward compatible (NULLable columns)

---

## 🚨 Risk Mitigation

### Risk 1: Large Request Size (5 images)
**Mitigation**:
- Optimize each to 100KB (max 500KB total)
- Async processing (no timeout issues)
- Request validation (max file sizes)

### Risk 2: Storage Costs
**Mitigation**:
- Same optimization as single image
- Consider cleanup policy for old images

### Risk 3: Processing Time
**Mitigation**:
- Async flow (no blocking)
- Notification when complete
- Status check endpoint

### Risk 4: Worker Overload
**Mitigation**:
- Retry with backoff
- Queue depth monitoring
- Scale worker instances if needed

---

## 📞 Communication Plan

### Mobile Team
- [ ] Share API documentation
- [ ] Provide test endpoint URLs
- [ ] Share Postman collection
- [ ] Example request/response payloads

### Frontend Team
- [ ] API documentation
- [ ] Deep link format
- [ ] Status check polling guidance

---

## 🔗 References

- Single Image Flow: `PlantAnalysisAsyncService.cs:54-189`
- Worker Processing: `PlantAnalysisJobService.cs:53-433`
- Queue DTO: `PlantAnalysisAsyncRequestDto.cs`
- SecuredOperation Guide: `claudedocs/SECUREDOPERATION_GUIDE.md`
- Development Rules: Memory `multi_image_development_rules`

---

## 📝 Session Notes

### Session 2025-01-27
- ✅ Completed Phase 0 analysis
- ✅ Memorized development rules
- ✅ Created implementation plan
- 🔄 Ready to start Phase 1 (DTOs)

---

**Last Updated**: 2025-01-27
**Next Action**: Begin Phase 1 - Create DTO classes
