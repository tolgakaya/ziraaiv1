# TypeScript Worker ↔ C# Services Compatibility Analysis

**Date**: 2025-12-01
**Purpose**: Identify discrepancies between TypeScript worker message types and C# DTO expectations
**Status**: 🔴 **CRITICAL ISSUES FOUND** - Message format mismatches detected

---

## Executive Summary

The current TypeScript worker message types (`RawAnalysisMessage`, `AnalysisResultMessage`) **DO NOT match** the C# DTOs (`PlantAnalysisAsyncRequestDto`, `PlantAnalysisAsyncResponseDto`) used by WebAPI and PlantAnalysisWorkerService.

### Critical Issues:

1. **Request Format Mismatch**: TypeScript expects `snake_case` fields, but C# sends **PascalCase** fields
2. **Response Format Mismatch**: TypeScript sends snake_case in `processing_metadata`, but C# expects **PascalCase**
3. **Missing Required Fields**: Several C# fields have no TypeScript equivalent
4. **Field Name Differences**: Many fields use different naming conventions
5. **Nested Structure Differences**: Response structures don't align properly

---

## Issue 1: Request Message Format (WebAPI → Worker)

### Problem: Field Naming Convention Mismatch

**C# Sends (PlantAnalysisAsyncRequestDto)**:
```json
{
  "Image": null,
  "ImageUrl": "https://...",
  "UserId": 46,
  "FarmerId": "F046",
  "SponsorId": "S003",
  "SponsorUserId": 12,
  "SponsorshipCodeId": 34,
  "CropType": "Tomato",
  "FieldId": "FIELD_001",
  "UrgencyLevel": "Medium",
  "GpsCoordinates": { "Lat": 36.8969, "Lng": 30.7133 },
  "PlantingDate": "2024-12-01T00:00:00Z",
  "ExpectedHarvestDate": "2025-03-01T00:00:00Z",
  "LastFertilization": "2024-12-15T00:00:00Z",
  "LastIrrigation": "2024-12-30T00:00:00Z",
  "PreviousTreatments": ["NPK 20-20-20"],
  "WeatherConditions": "Sunny",
  "Temperature": 25.5,
  "Humidity": 65.0,
  "SoilType": "Loamy",
  "ContactInfo": { "Phone": "+90555", "Email": "farmer@example.com" },
  "AdditionalInfo": { "IrrigationSystem": "Drip" },
  "ResponseQueue": "plant-analysis-results",
  "CorrelationId": "a1b2c3d4",
  "AnalysisId": "async_analysis_20250101_120000_a1b2c3d4",
  "Altitude": 42
}
```

**TypeScript Expects (RawAnalysisMessage)**:
```typescript
{
  analysis_id: string;           // ❌ C# sends "AnalysisId"
  timestamp: string;             // ❌ NOT in C# DTO
  image: string;                 // ❌ C# sends "Image"
  leaf_top_image?: string;       // ❌ NOT in C# DTO
  leaf_bottom_image?: string;    // ❌ NOT in C# DTO
  plant_overview_image?: string; // ❌ NOT in C# DTO
  root_image?: string;           // ❌ NOT in C# DTO
  user_id?: string | number;     // ❌ C# sends "UserId"
  farmer_id?: string | number;   // ❌ C# sends "FarmerId"
  sponsor_id?: string | number;  // ❌ C# sends "SponsorId"
  location?: string;             // ❌ C# sends "Location"
  gps_coordinates?: ...;         // ❌ C# sends "GpsCoordinates"
  altitude?: number;             // ❌ C# sends "Altitude"
  field_id?: string | number;    // ❌ C# sends "FieldId"
  crop_type?: string;            // ❌ C# sends "CropType"
  planting_date?: string;        // ❌ C# sends "PlantingDate"
  // ... ALL fields use snake_case, but C# sends PascalCase
}
```

### Solution:

**Option 1: Transform PascalCase → snake_case in TypeScript Worker**
- Add deserialization mapper to convert C# PascalCase to snake_case
- Pros: Keeps internal TypeScript types clean
- Cons: Performance overhead, maintenance burden

**Option 2: Update TypeScript Types to Match C# Exactly (RECOMMENDED)**
- Change TypeScript interfaces to use PascalCase
- Pros: Direct mapping, no transformation needed, better performance
- Cons: Unconventional for TypeScript (but necessary for C# interop)

---

## Issue 2: Response Message Format (Worker → PlantAnalysisWorkerService)

### Problem: ProcessingMetadata Field Names

**TypeScript Sends (AnalysisResultMessage.processing_metadata)**:
```typescript
processing_metadata: {
  parse_success: boolean,           // ❌ C# expects "ParseSuccess"
  processing_timestamp: string,     // ❌ C# expects "ProcessingTimestamp"
  processing_time_ms: number,       // ❌ C# expects "ProcessingTimeMs"
  ai_model: string,                 // ❌ C# expects "AiModel"
  workflow_version: string,         // ❌ C# expects "WorkflowVersion"
  image_source: 'url' | 'base64',   // ❌ NOT in C# DTO
  error_details?: string            // ❌ NOT in C# DTO
}
```

**C# Expects (ProcessingMetadata)**:
```csharp
public class ProcessingMetadata
{
    public bool ParseSuccess { get; set; }          // PascalCase
    public DateTime ProcessingTimestamp { get; set; } // PascalCase
    public string AiModel { get; set; }             // PascalCase
    public string WorkflowVersion { get; set; }     // PascalCase
    public DateTime ReceivedAt { get; set; }        // Missing in TypeScript
    public int ProcessingTimeMs { get; set; }       // PascalCase
    public int RetryCount { get; set; }             // Missing in TypeScript
    public string Priority { get; set; }            // Missing in TypeScript
}
```

### Solution:

**Update TypeScript to use PascalCase for ProcessingMetadata**:
```typescript
processing_metadata: {
  ParseSuccess: boolean;
  ProcessingTimestamp: string;       // ISO 8601
  AiModel: string;
  WorkflowVersion: string;
  ReceivedAt: string;                // NEW - Add this field
  ProcessingTimeMs: number;
  RetryCount: number;                // NEW - Add this field
  Priority?: string;                 // NEW - Add this field (optional)
}
```

---

## Issue 3: Missing Required Fields

### Request Fields Missing from TypeScript:

| C# Field | TypeScript Equivalent | Status |
|----------|----------------------|--------|
| `SponsorUserId` | None | ❌ Missing |
| `SponsorshipCodeId` | None | ❌ Missing |
| `ResponseQueue` | `rabbitmq_metadata.response_queue` | ⚠️ Wrong location |
| `CorrelationId` | `rabbitmq_metadata.correlation_id` | ⚠️ Wrong location |
| `AnalysisId` | `analysis_id` | ⚠️ Wrong case |

### Response Fields Missing from TypeScript:

| C# Field | TypeScript Equivalent | Status |
|----------|----------------------|--------|
| `SponsorUserId` (PascalCase!) | None | ❌ Missing |
| `SponsorshipCodeId` (PascalCase!) | None | ❌ Missing |
| `success` | None | ❌ Missing |
| `error` (boolean) | `error?: boolean` | ⚠️ Optional (should be required) |
| `ImageMetadata.URL` | `image_metadata.url` | ⚠️ Wrong case (critical!) |

---

## Issue 4: Token Usage Structure Mismatch

### TypeScript Sends (Complex Nested Structure):
```typescript
token_usage: {
  summary: {
    model: string;
    analysis_id: string;
    timestamp: string;
    total_tokens: number;
    total_cost_usd: number;
    total_cost_try: number;
    image_source: string;
  };
  token_breakdown: {
    input: {
      system_prompt: number;
      context_data: number;
      image: number;
      image_url_text: number;
      cached_input_tokens: number;
      regular_input_tokens: number;
      total: number;
    };
    output: {
      response: number;
      total: number;
    };
    grand_total: number;
  };
  cost_breakdown: {
    input_cost_usd: number;
    cached_input_cost_usd: number;
    output_cost_usd: number;
    total_cost_usd: number;
    total_cost_try: number;
    exchange_rate: number;
  };
}
```

### C# Expects (Simple Flat Structure):
```csharp
public class TokenUsage
{
    [JsonProperty("total_tokens")]
    public int TotalTokens { get; set; }

    [JsonProperty("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonProperty("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonProperty("cost_usd")]
    public decimal CostUsd { get; set; }

    [JsonProperty("cost_try")]
    public decimal CostTry { get; set; }
}
```

### Solution:

**Update TypeScript to match C# flat structure**:
```typescript
token_usage: {
  total_tokens: number;           // From token_breakdown.grand_total
  prompt_tokens: number;          // From token_breakdown.input.total
  completion_tokens: number;      // From token_breakdown.output.total
  cost_usd: number;               // From cost_breakdown.total_cost_usd
  cost_try: number;               // From cost_breakdown.total_cost_try
}
```

---

## Issue 5: Special Cases - PascalCase in Response

**CRITICAL**: C# DTO has **inconsistent casing** for some fields:

```csharp
// Most fields use snake_case with JsonProperty attribute:
[JsonProperty("analysis_id")]
public string AnalysisId { get; set; }

// BUT these fields have NO JsonProperty attribute (use PascalCase in JSON):
public int? SponsorUserId { get; set; }        // ❌ NOT snake_case!
public int? SponsorshipCodeId { get; set; }    // ❌ NOT snake_case!
```

**TypeScript MUST send**:
```json
{
  "analysis_id": "async_analysis_...",    // snake_case (has JsonProperty)
  "SponsorUserId": 12,                    // PascalCase (NO JsonProperty!)
  "SponsorshipCodeId": 34                 // PascalCase (NO JsonProperty!)
}
```

---

## Complete Field Mapping Table

### Request Message (WebAPI → Worker)

| C# Field | Expected JSON Name | TypeScript Current | Required Fix |
|----------|-------------------|-------------------|--------------|
| `Image` | `Image` | `image` | ✅ Rename to `Image` |
| `ImageUrl` | `ImageUrl` | None | ✅ Add `ImageUrl` |
| `UserId` | `UserId` | `user_id` | ✅ Rename to `UserId` |
| `FarmerId` | `FarmerId` | `farmer_id` | ✅ Rename to `FarmerId` |
| `SponsorId` | `SponsorId` | `sponsor_id` | ✅ Rename to `SponsorId` |
| `SponsorUserId` | `SponsorUserId` | None | ✅ Add `SponsorUserId` |
| `SponsorshipCodeId` | `SponsorshipCodeId` | None | ✅ Add `SponsorshipCodeId` |
| `Location` | `Location` | `location` | ✅ Rename |
| `GpsCoordinates` | `GpsCoordinates` | `gps_coordinates` | ✅ Rename |
| `CropType` | `CropType` | `crop_type` | ✅ Rename |
| `FieldId` | `FieldId` | `field_id` | ✅ Rename |
| `UrgencyLevel` | `UrgencyLevel` | `urgency_level` | ✅ Rename |
| `Notes` | `Notes` | `notes` | ✅ Rename |
| `ResponseQueue` | `ResponseQueue` | `rabbitmq_metadata.response_queue` | ✅ Move to top level |
| `CorrelationId` | `CorrelationId` | `rabbitmq_metadata.correlation_id` | ✅ Move to top level |
| `AnalysisId` | `AnalysisId` | `analysis_id` | ✅ Rename |
| `Altitude` | `Altitude` | `altitude` | ✅ Rename |
| `PlantingDate` | `PlantingDate` | `planting_date` | ✅ Rename |
| `ExpectedHarvestDate` | `ExpectedHarvestDate` | `expected_harvest_date` | ✅ Rename |
| `LastFertilization` | `LastFertilization` | `last_fertilization` | ✅ Rename |
| `LastIrrigation` | `LastIrrigation` | `last_irrigation` | ✅ Rename |
| `PreviousTreatments` | `PreviousTreatments` | `previous_treatments` | ✅ Rename |
| `WeatherConditions` | `WeatherConditions` | `weather_conditions` | ✅ Rename |
| `Temperature` | `Temperature` | `temperature` | ✅ Rename |
| `Humidity` | `Humidity` | `humidity` | ✅ Rename |
| `SoilType` | `SoilType` | `soil_type` | ✅ Rename |
| `ContactInfo` | `ContactInfo` | `contact_info` | ✅ Rename |
| `AdditionalInfo` | `AdditionalInfo` | `additional_info` | ✅ Rename |

### Response Message (Worker → PlantAnalysisWorkerService)

**Analysis Results** (All use snake_case with `JsonProperty`):
- ✅ `plant_identification` - Correct
- ✅ `health_assessment` - Correct
- ✅ `nutrient_status` - Correct
- ✅ `pest_disease` - Correct
- ✅ `environmental_stress` - Correct
- ✅ `cross_factor_insights` - Correct
- ✅ `recommendations` - Correct
- ✅ `summary` - Correct
- ✅ `risk_assessment` - Correct
- ✅ `confidence_notes` - Correct
- ✅ `farmer_friendly_summary` - Correct

**Metadata Fields** (Mixed casing):
| C# Field | Expected JSON Name | TypeScript Current | Required Fix |
|----------|-------------------|-------------------|--------------|
| `AnalysisId` | `analysis_id` | ✅ Correct | None |
| `Timestamp` | `timestamp` | ✅ Correct | None |
| `UserId` | `user_id` | ✅ Correct | None |
| `FarmerId` | `farmer_id` | ✅ Correct | None |
| `SponsorId` | `sponsor_id` | ✅ Correct | None |
| `SponsorUserId` | **`SponsorUserId`** (PascalCase!) | None | ✅ Add (PascalCase) |
| `SponsorshipCodeId` | **`SponsorshipCodeId`** (PascalCase!) | None | ✅ Add (PascalCase) |
| `Location` | `location` | ✅ Correct | None |
| `ProcessingMetadata.AiModel` | **`AiModel`** (PascalCase!) | `ai_model` | ✅ Fix casing |
| `ProcessingMetadata.WorkflowVersion` | **`WorkflowVersion`** (PascalCase!) | `workflow_version` | ✅ Fix casing |
| `ProcessingMetadata.ProcessingTimestamp` | **`ProcessingTimestamp`** (PascalCase!) | `processing_timestamp` | ✅ Fix casing |
| `ProcessingMetadata.ParseSuccess` | **`ParseSuccess`** (PascalCase!) | `parse_success` | ✅ Fix casing |
| `ProcessingMetadata.ReceivedAt` | **`ReceivedAt`** (PascalCase!) | None | ✅ Add |
| `ProcessingMetadata.ProcessingTimeMs` | **`ProcessingTimeMs`** (PascalCase!) | `processing_time_ms` | ✅ Fix casing |
| `ProcessingMetadata.RetryCount` | **`RetryCount`** (PascalCase!) | None | ✅ Add |
| `ProcessingMetadata.Priority` | **`Priority`** (PascalCase!) | None | ✅ Add |
| `ImageMetadata.URL` | **`URL`** (PascalCase!) | `url` | ✅ Fix casing (CRITICAL) |
| `ImageMetadata.Format` | **`Format`** (PascalCase!) | `format` | ✅ Fix casing |
| `ImageMetadata.SizeBytes` | **`SizeBytes`** (PascalCase!) | `size_bytes` | ✅ Fix casing |
| `ImageMetadata.SizeKb` | **`SizeKb`** (PascalCase!) | `size_kb` | ✅ Fix casing |
| `ImageMetadata.SizeMb` | **`SizeMb`** (PascalCase!) | `size_mb` | ✅ Fix casing |
| `ImageMetadata.Base64Length` | **`Base64Length`** (PascalCase!) | `base64_length` | ✅ Fix casing |
| `ImageMetadata.UploadTimestamp` | **`UploadTimestamp`** (PascalCase!) | `upload_timestamp` | ✅ Fix casing |

**Status Fields**:
| C# Field | Expected JSON Name | TypeScript Current | Required Fix |
|----------|-------------------|-------------------|--------------|
| `Success` | `success` | None | ✅ Add (required, boolean) |
| `Message` | `message` | None | ✅ Add (optional) |
| `Error` | `error` | `error?` | ✅ Make required (boolean) |
| `ErrorMessage` | `error_message` | ✅ Correct | None |
| `ErrorType` | `error_type` | ✅ Correct | None |

---

## Action Plan

### Phase 1: Update TypeScript Request Interface (HIGH PRIORITY)

**File**: [workers/analysis-worker/src/types/messages.ts](../../../analysis-worker/src/types/messages.ts)

**Changes Required**:
1. Create new `PlantAnalysisAsyncRequestDto` interface matching C# exactly
2. Use **PascalCase** for all fields to match C# JSON serialization
3. Add missing fields: `SponsorUserId`, `SponsorshipCodeId`
4. Move `ResponseQueue`, `CorrelationId`, `AnalysisId` to top level
5. Remove `RawAnalysisMessage` (not compatible with WebAPI)

### Phase 2: Update TypeScript Response Interface (CRITICAL)

**File**: [workers/analysis-worker/src/types/messages.ts](../../../analysis-worker/src/types/messages.ts)

**Changes Required**:
1. Create new `PlantAnalysisAsyncResponseDto` interface matching C# exactly
2. Fix `ProcessingMetadata` to use **PascalCase** fields
3. Fix `ImageMetadata` to use **PascalCase** fields (especially `URL`!)
4. Add `SponsorUserId`, `SponsorshipCodeId` (PascalCase, top-level)
5. Simplify `token_usage` to flat structure (match C# `TokenUsage` class)
6. Add required `success: boolean` and `error: boolean` fields
7. Remove `AnalysisResultMessage` (not compatible with PlantAnalysisWorkerService)

### Phase 3: Update Worker Implementation

**File**: [workers/analysis-worker/src/index.ts](../../../analysis-worker/src/index.ts)

**Changes Required**:
1. Update message deserialization to expect PascalCase from WebAPI
2. Update response serialization to produce correct casing (mixed: snake_case for analysis results, PascalCase for metadata)
3. Set `success: true`, `error: false` for successful analyses
4. Populate `ImageMetadata.URL` (PascalCase) with image URL
5. Echo all request fields back in response (maintain exact values)

### Phase 4: Integration Testing

1. **Request Deserialization Test**:
   - Load actual WebAPI message from RabbitMQ
   - Verify all fields deserialize correctly
   - Check PascalCase handling

2. **Response Serialization Test**:
   - Generate response in TypeScript
   - Verify JSON matches C# expectations
   - Test with PlantAnalysisJobService deserialization

3. **End-to-End Test**:
   - WebAPI → RabbitMQ → TypeScript Worker → AI → RabbitMQ → PlantAnalysisWorkerService → Database
   - Verify database record matches expected values

---

## Risk Assessment

### 🔴 High Risk Issues:

1. **ImageMetadata.URL casing** - Wrong case prevents database from storing image URL (line 231 in PlantAnalysisJobService.cs)
2. **SponsorUserId/SponsorshipCodeId missing** - Breaks sponsor attribution system
3. **ProcessingMetadata PascalCase** - Prevents AI model tracking and analytics

### 🟡 Medium Risk Issues:

1. **Token usage structure** - Prevents cost tracking and billing
2. **Request field casing** - Worker may fail to deserialize WebAPI messages
3. **Success/error flags** - May cause incorrect status in database

### 🟢 Low Risk Issues:

1. **Optional metadata fields** - Can be added incrementally
2. **Additional nested fields** - Won't break core functionality

---

## Summary

**Critical Finding**: Current TypeScript worker types are **NOT compatible** with C# WebAPI and PlantAnalysisWorkerService.

**Root Cause**: TypeScript types were designed based on N8N snake_case conventions, but C# uses **PascalCase** for top-level DTO fields with mixed casing for nested objects.

**Required Action**: Complete rewrite of TypeScript message types to match C# DTOs exactly.

**Estimated Effort**: 4-6 hours for type updates + implementation changes + testing

**Priority**: 🔴 **CRITICAL** - Must be fixed before deploying TypeScript worker to production

---

## Next Steps

1. ✅ Review this analysis with team
2. ⏳ Create corrected TypeScript interfaces (Phase 1 & 2)
3. ⏳ Update worker implementation (Phase 3)
4. ⏳ Integration testing (Phase 4)
5. ⏳ Deploy to staging environment
6. ⏳ Verify with actual WebAPI traffic

**Last Updated**: 2025-12-01
**Reviewer**: Pending
**Status**: Analysis Complete, Implementation Pending
