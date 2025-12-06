# Message Format Compatibility - TypeScript Worker & C# Services

**Purpose**: Ensure TypeScript analysis worker produces identical message formats to N8N for seamless integration with existing WebAPI and PlantAnalysisWorkerService.

**Critical Requirement**: ⚠️ **NO CHANGES** to WebAPI request format or PlantAnalysisWorkerService response format. TypeScript worker is a **drop-in replacement** for N8N only.

---

## Architecture Overview

```
┌─────────────┐    publish     ┌──────────────────────┐    consume    ┌────────────────────┐
│   WebAPI    │ ─────────────> │ plant-analysis-      │ ────────────> │ TypeScript Worker │
│ (C# .NET 9) │                │ requests (RabbitMQ)  │               │ (Node.js)          │
└─────────────┘                └──────────────────────┘               └────────────────────┘
                                                                              │
                                                                              │ AI Analysis
                                                                              │ (OpenAI/Gemini/Anthropic)
                                                                              ▼
┌─────────────────────────┐    consume    ┌──────────────────────┐    publish
│ PlantAnalysisWorkerService│ <─────────── │ plant-analysis-      │ <──────────────
│ (C# Background Service)  │              │ results (RabbitMQ)   │
└─────────────────────────┘              └──────────────────────┘
```

**Flow**:
1. WebAPI publishes `PlantAnalysisAsyncRequestDto` to `plant-analysis-requests` queue
2. **TypeScript Worker** (replaces N8N) consumes request, performs AI analysis
3. TypeScript Worker publishes `PlantAnalysisAsyncResponseDto` to `plant-analysis-results` queue
4. PlantAnalysisWorkerService consumes result and saves to database

---

## 1. Request Message Format (WebAPI → Worker)

### C# DTO: `PlantAnalysisAsyncRequestDto`

**Source**: [Entities/Dtos/PlantAnalysisAsyncRequestDto.cs](../../Entities/Dtos/PlantAnalysisAsyncRequestDto.cs)

**Published By**: WebAPI ([PlantAnalysisAsyncService.cs:134-168](../../Business/Services/PlantAnalysis/PlantAnalysisAsyncService.cs#L134-L168))

```csharp
public class PlantAnalysisAsyncRequestDto
{
    // Image (URL-based, NOT base64)
    public string Image { get; set; }          // NULL - not used anymore
    public string ImageUrl { get; set; }       // PRIMARY: Full image URL from storage

    // User & Attribution
    public int? UserId { get; set; }
    public string FarmerId { get; set; }       // Format: "F{userId}" (e.g., "F046")
    public string SponsorId { get; set; }
    public int? SponsorUserId { get; set; }
    public int? SponsorshipCodeId { get; set; }

    // Analysis Request
    public string Location { get; set; }
    public GpsCoordinates GpsCoordinates { get; set; }
    public string CropType { get; set; }
    public string FieldId { get; set; }
    public string UrgencyLevel { get; set; }
    public string Notes { get; set; }

    // RabbitMQ Metadata
    public string ResponseQueue { get; set; } = "plant-analysis-results";
    public string CorrelationId { get; set; }
    public string AnalysisId { get; set; }

    // Additional Context
    public int? Altitude { get; set; }
    public DateTime? PlantingDate { get; set; }
    public DateTime? ExpectedHarvestDate { get; set; }
    public DateTime? LastFertilization { get; set; }
    public DateTime? LastIrrigation { get; set; }
    public string[] PreviousTreatments { get; set; }
    public string WeatherConditions { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? Humidity { get; set; }
    public string SoilType { get; set; }
    public ContactInfo ContactInfo { get; set; }
    public AdditionalInfoData AdditionalInfo { get; set; }
}
```

### TypeScript Interface: `PlantAnalysisMessage`

**Current Location**: [workers/analysis-worker/src/types/messages.ts](../../../analysis-worker/src/types/messages.ts)

**Status**: ✅ **Compatible** - Already matches C# DTO structure

```typescript
export interface PlantAnalysisMessage {
  // Image (URL-based)
  Image: string | null;              // NULL - matches C# behavior
  ImageUrl: string;                  // PRIMARY - full storage URL

  // User & Attribution
  UserId?: number | null;
  FarmerId: string;                  // Format: "F{userId}"
  SponsorId?: string | null;
  SponsorUserId?: number | null;
  SponsorshipCodeId?: number | null;

  // Analysis Request
  Location?: string;
  GpsCoordinates?: GpsCoordinates;
  CropType: string;
  FieldId?: string;
  UrgencyLevel?: string;
  Notes?: string;

  // RabbitMQ Metadata
  ResponseQueue: string;             // "plant-analysis-results"
  CorrelationId: string;
  AnalysisId: string;

  // Additional Context
  Altitude?: number | null;
  PlantingDate?: string | null;      // ISO 8601 date string
  ExpectedHarvestDate?: string | null;
  LastFertilization?: string | null;
  LastIrrigation?: string | null;
  PreviousTreatments?: string[] | null;
  WeatherConditions?: string | null;
  Temperature?: number | null;
  Humidity?: number | null;
  SoilType?: string | null;
  ContactInfo?: ContactInfo | null;
  AdditionalInfo?: Record<string, unknown> | null;
}
```

**Mapping Notes**:
- ✅ Field names match exactly (PascalCase preserved)
- ✅ `Image` field is `null` (WebAPI V2 sends only URLs)
- ✅ `ImageUrl` is the primary image source
- ✅ Dates as ISO 8601 strings (compatible with C# `DateTime?`)
- ✅ All optional fields use `| null` for JSON compatibility

---

## 2. Response Message Format (Worker → PlantAnalysisWorkerService)

### C# DTO: `PlantAnalysisAsyncResponseDto`

**Source**: [Entities/Dtos/PlantAnalysisAsyncResponseDto.cs](../../Entities/Dtos/PlantAnalysisAsyncResponseDto.cs)

**Consumed By**: PlantAnalysisWorkerService ([PlantAnalysisJobService.cs:53](../../PlantAnalysisWorkerService/Jobs/PlantAnalysisJobService.cs#L53))

**Critical Fields** (used by PlantAnalysisJobService for database mapping):

```csharp
public class PlantAnalysisAsyncResponseDto
{
    // ============================================
    // ANALYSIS RESULTS (AI-generated)
    // ============================================
    [JsonProperty("plant_identification")]
    public PlantIdentification PlantIdentification { get; set; }  // REQUIRED

    [JsonProperty("health_assessment")]
    public HealthAssessment HealthAssessment { get; set; }        // REQUIRED

    [JsonProperty("nutrient_status")]
    public NutrientStatus NutrientStatus { get; set; }            // REQUIRED

    [JsonProperty("pest_disease")]
    public PestDisease PestDisease { get; set; }                  // REQUIRED

    [JsonProperty("environmental_stress")]
    public EnvironmentalStress EnvironmentalStress { get; set; }  // REQUIRED

    [JsonProperty("cross_factor_insights")]
    public List<CrossFactorInsight> CrossFactorInsights { get; set; }

    [JsonProperty("recommendations")]
    public Recommendations Recommendations { get; set; }          // REQUIRED

    [JsonProperty("summary")]
    public AnalysisSummary Summary { get; set; }                  // REQUIRED

    // ============================================
    // METADATA (Echo from request)
    // ============================================
    [JsonProperty("analysis_id")]
    public string AnalysisId { get; set; }                        // REQUIRED

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }                       // REQUIRED

    [JsonProperty("farmer_id")]
    public string FarmerId { get; set; }                          // REQUIRED

    [JsonProperty("sponsor_id")]
    public string SponsorId { get; set; }

    public int? SponsorUserId { get; set; }
    public int? SponsorshipCodeId { get; set; }

    [JsonProperty("location")]
    public string Location { get; set; }

    [JsonProperty("gps_coordinates")]
    public GpsCoordinates GpsCoordinates { get; set; }

    [JsonProperty("crop_type")]
    public string CropType { get; set; }

    [JsonProperty("field_id")]
    public string FieldId { get; set; }

    // ... (all other request fields echoed back)

    // ============================================
    // PROCESSING METADATA
    // ============================================
    [JsonProperty("image_metadata")]
    public ImageMetadata ImageMetadata { get; set; }              // REQUIRED (contains URL)

    [JsonProperty("processing_metadata")]
    public ProcessingMetadata ProcessingMetadata { get; set; }    // REQUIRED

    [JsonProperty("token_usage")]
    public TokenUsage TokenUsage { get; set; }

    [JsonProperty("request_metadata")]
    public RequestMetadata RequestMetadata { get; set; }

    // ============================================
    // RESPONSE STATUS
    // ============================================
    [JsonProperty("success")]
    public bool Success { get; set; }                             // REQUIRED

    [JsonProperty("error")]
    public bool Error { get; set; }

    [JsonProperty("error_message")]
    public string ErrorMessage { get; set; }
}
```

### TypeScript Interface: `AnalysisResult`

**Current Location**: [workers/analysis-worker/src/types/messages.ts](../../../analysis-worker/src/types/messages.ts)

**Status**: ⚠️ **NEEDS VERIFICATION** - Must match C# exactly

**Required Structure**:

```typescript
export interface AnalysisResult {
  // ============================================
  // ANALYSIS RESULTS (snake_case for JSON)
  // ============================================
  plant_identification: PlantIdentification;
  health_assessment: HealthAssessment;
  nutrient_status: NutrientStatus;
  pest_disease: PestDisease;
  environmental_stress: EnvironmentalStress;
  cross_factor_insights: CrossFactorInsight[];
  recommendations: Recommendations;
  summary: AnalysisSummary;

  // ============================================
  // METADATA (snake_case, echoed from request)
  // ============================================
  analysis_id: string;                    // From request.AnalysisId
  timestamp: string;                      // ISO 8601 datetime
  user_id?: number | null;                // From request.UserId
  farmer_id: string;                      // From request.FarmerId
  sponsor_id?: string | null;
  SponsorUserId?: number | null;          // PascalCase (NOT snake_case!)
  SponsorshipCodeId?: number | null;      // PascalCase (NOT snake_case!)

  location?: string | null;
  gps_coordinates?: GpsCoordinates | null;
  altitude?: number | null;
  field_id?: string | null;
  crop_type: string;
  planting_date?: string | null;
  expected_harvest_date?: string | null;
  last_fertilization?: string | null;
  last_irrigation?: string | null;
  previous_treatments?: string[] | null;
  weather_conditions?: string | null;
  temperature?: number | null;
  humidity?: number | null;
  soil_type?: string | null;
  urgency_level?: string | null;
  notes?: string | null;
  contact_info?: ContactInfo | null;
  additional_info?: Record<string, unknown> | null;

  // ============================================
  // IMAGE URLs
  // ============================================
  image_url?: string | null;
  image_path?: string | null;
  leaf_top_url?: string | null;
  leaf_bottom_url?: string | null;
  plant_overview_url?: string | null;
  root_url?: string | null;

  // ============================================
  // PROCESSING METADATA (snake_case)
  // ============================================
  image_metadata?: {
    Format?: string;
    URL?: string;                         // CRITICAL: Image URL for database
    SizeBytes?: number;
    SizeKb?: number;
    SizeMb?: number;
    Base64Length?: number;
    UploadTimestamp?: string;
  };

  processing_metadata: {
    ParseSuccess: boolean;
    ProcessingTimestamp: string;          // ISO 8601
    AiModel: string;                      // e.g., "gpt-4o-mini", "gemini-2.0-flash-exp"
    WorkflowVersion: string;              // e.g., "2.0.0"
    ReceivedAt: string;
    ProcessingTimeMs: number;
    RetryCount: number;
    Priority?: string;
  };

  token_usage?: {
    total_tokens: number;
    prompt_tokens: number;
    completion_tokens: number;
    cost_usd: number;
    cost_try: number;
  };

  request_metadata?: {
    user_agent?: string;
    ip_address?: string;
    request_timestamp?: string;
    request_id?: string;
    api_version?: string;
  };

  // ============================================
  // ADDITIONAL FIELDS
  // ============================================
  risk_assessment?: RiskAssessment;
  confidence_notes?: ConfidenceNote[];
  farmer_friendly_summary?: string;

  // ============================================
  // RESPONSE STATUS
  // ============================================
  success: boolean;                       // REQUIRED: true for successful analysis
  message?: string;
  error: boolean;                         // REQUIRED: false for successful analysis
  error_message?: string | null;
  error_type?: string | null;
}
```

---

## 3. Critical Compatibility Requirements

### ⚠️ Snake_case vs PascalCase Inconsistency

**Problem**: C# DTO uses `JsonProperty` attributes with snake_case for most fields, BUT some fields are PascalCase without attributes.

**Examples**:
```csharp
// ✅ Snake_case (with JsonProperty attribute)
[JsonProperty("analysis_id")]
public string AnalysisId { get; set; }

// ❌ PascalCase (NO JsonProperty attribute)
public int? SponsorUserId { get; set; }
public int? SponsorshipCodeId { get; set; }
```

**TypeScript Worker MUST Match**:
```typescript
{
  "analysis_id": "async_analysis_...",    // snake_case
  "SponsorUserId": 123,                   // PascalCase (NO underscore!)
  "SponsorshipCodeId": 456                // PascalCase (NO underscore!)
}
```

### 🔴 Critical Fields for Database Mapping

**PlantAnalysisJobService** ([PlantAnalysisJobService.cs:53-337](../../PlantAnalysisWorkerService/Jobs/PlantAnalysisJobService.cs#L53-L337)) maps these fields:

1. **Identification** (Line 60):
   - `AnalysisId` → Find existing record in database

2. **Attribution** (Lines 82-86):
   - `UserId`, `FarmerId`, `SponsorId`, `SponsorUserId`, `SponsorshipCodeId`

3. **Image URLs** (Lines 93, 96-99, 231):
   - `ImageMetadata.URL` (PRIMARY source)
   - `ImageUrl`, `ImagePath` (fallback)
   - `LeafTopUrl`, `LeafBottomUrl`, `PlantOverviewUrl`, `RootUrl`

4. **Processing Metadata** (Lines 246-255):
   - `ProcessingMetadata.AiModel`
   - `ProcessingMetadata.WorkflowVersion`
   - `ProcessingMetadata.ProcessingTimestamp`
   - `TokenUsage` (entire object serialized)
   - `RequestMetadata` (entire object serialized)

5. **Analysis Results** (Lines 258-331):
   - `PlantIdentification.*` → 6 fields
   - `HealthAssessment.*` → 4 fields
   - `NutrientStatus.*` → 16 fields (all 14 elements + primary/severity)
   - `PestDisease.*` → 6 fields
   - `EnvironmentalStress.*` → 2 fields
   - `Summary.*` → 7 fields
   - `Recommendations` (serialized as JSON)

6. **Status** (Line 224):
   - Sets `AnalysisStatus = "Completed"` when processing succeeds

---

## 4. Verification Checklist

### Request Message (`plant-analysis-requests` queue)

- [ ] TypeScript worker correctly deserializes all fields from WebAPI
- [ ] Handles `ImageUrl` (primary) and ignores `Image` (null)
- [ ] Preserves `CorrelationId` and `AnalysisId` for tracking
- [ ] Supports all optional context fields (dates, weather, etc.)

### Response Message (`plant-analysis-results` queue)

- [ ] All snake_case fields use underscores (`analysis_id`, NOT `analysisId`)
- [ ] Exception: `SponsorUserId` and `SponsorshipCodeId` are PascalCase
- [ ] `ProcessingMetadata` fields are PascalCase (e.g., `AiModel`, not `ai_model`)
- [ ] `ImageMetadata.URL` contains full image URL for database storage
- [ ] All analysis result objects match C# DTO nested structures exactly
- [ ] `success: true` and `error: false` for successful analyses
- [ ] `timestamp` is ISO 8601 format compatible with C# `DateTime`

### Field-by-Field Mapping

**PlantIdentification**:
```typescript
{
  "species": string,                    // → PlantSpecies
  "variety": string,                    // → PlantVariety
  "growth_stage": string,               // → GrowthStage
  "confidence": number,                 // → IdentificationConfidence
  "identifying_features": string[],
  "visible_parts": string[]
}
```

**HealthAssessment**:
```typescript
{
  "vigor_score": number,                // → VigorScore
  "leaf_color": string,
  "leaf_texture": string,
  "growth_pattern": string,
  "structural_integrity": string,
  "stress_indicators": string[],        // → StressIndicators (JSON)
  "disease_symptoms": string[],         // → DiseaseSymptoms (JSON)
  "severity": string                    // → HealthSeverity
}
```

**NutrientStatus** (ALL 14 elements):
```typescript
{
  "nitrogen": string,                   // → Nitrogen
  "phosphorus": string,                 // → Phosphorus
  "potassium": string,                  // → Potassium
  "calcium": string,                    // → Calcium
  "magnesium": string,                  // → Magnesium
  "sulfur": string,                     // → Sulfur
  "iron": string,                       // → Iron
  "zinc": string,                       // → Zinc
  "manganese": string,                  // → Manganese
  "boron": string,                      // → Boron
  "copper": string,                     // → Copper
  "molybdenum": string,                 // → Molybdenum
  "chlorine": string,                   // → Chlorine
  "nickel": string,                     // → Nickel
  "primary_deficiency": string,         // → PrimaryDeficiency
  "secondary_deficiencies": string[],
  "severity": string                    // → NutrientSeverity
}
```

**PestDisease**:
```typescript
{
  "pests_detected": PestDetectedDto[],  // → Pests (JSON)
  "diseases_detected": DiseaseDetectedDto[], // → Diseases (JSON)
  "damage_pattern": string,
  "affected_area_percentage": number,   // → AffectedAreaPercentage
  "spread_risk": string,                // → SpreadRisk
  "primary_issue": string               // → PrimaryIssue
}
```

**EnvironmentalStress**:
```typescript
{
  "water_status": string,
  "temperature_stress": string,
  "light_stress": string,
  "physical_damage": string,
  "chemical_damage": string,
  "soil_indicators": string,
  "primary_stressor": string            // → PrimaryStressor
}
```

**Summary**:
```typescript
{
  "overall_health_score": number,       // → OverallHealthScore
  "primary_concern": string,            // → PrimaryConcern
  "secondary_concerns": string[],
  "critical_issues_count": number,      // → CriticalIssuesCount
  "confidence_level": number,           // → ConfidenceLevel
  "prognosis": string,                  // → Prognosis
  "estimated_yield_impact": string      // → EstimatedYieldImpact
}
```

---

## 5. Example Messages

### Example Request (WebAPI → Worker)

```json
{
  "Image": null,
  "ImageUrl": "https://iili.io/FDuqN99.jpg",
  "UserId": 46,
  "FarmerId": "F046",
  "SponsorId": "S003",
  "SponsorUserId": 12,
  "SponsorshipCodeId": 34,
  "Location": "Antalya, Turkey",
  "GpsCoordinates": {
    "Lat": 36.8969,
    "Lng": 30.7133
  },
  "CropType": "Tomato",
  "FieldId": "FIELD_001",
  "UrgencyLevel": "Medium",
  "Notes": "Yellowing leaves observed",
  "ResponseQueue": "plant-analysis-results",
  "CorrelationId": "a1b2c3d4",
  "AnalysisId": "async_analysis_20250101_120000_a1b2c3d4",
  "Altitude": 42,
  "PlantingDate": "2024-12-01T00:00:00Z",
  "ExpectedHarvestDate": "2025-03-01T00:00:00Z",
  "LastFertilization": "2024-12-15T00:00:00Z",
  "LastIrrigation": "2024-12-30T00:00:00Z",
  "PreviousTreatments": ["NPK 20-20-20", "Fungicide spray"],
  "WeatherConditions": "Sunny, 25°C",
  "Temperature": 25.5,
  "Humidity": 65.0,
  "SoilType": "Loamy",
  "ContactInfo": {
    "Phone": "+90555123456",
    "Email": "farmer@example.com"
  },
  "AdditionalInfo": {
    "IrrigationSystem": "Drip",
    "FertilizerType": "Organic"
  }
}
```

### Example Response (Worker → PlantAnalysisWorkerService)

```json
{
  "plant_identification": {
    "species": "Solanum lycopersicum",
    "variety": "Beefsteak",
    "growth_stage": "Vegetative",
    "confidence": 95,
    "identifying_features": ["Compound leaves", "Yellow flowers"],
    "visible_parts": ["Leaves", "Stem"]
  },
  "health_assessment": {
    "vigor_score": 70,
    "leaf_color": "Yellowish-green",
    "leaf_texture": "Smooth",
    "growth_pattern": "Normal",
    "structural_integrity": "Good",
    "stress_indicators": ["Chlorosis", "Leaf curling"],
    "disease_symptoms": ["Yellowing", "Necrotic spots"],
    "severity": "Moderate"
  },
  "nutrient_status": {
    "nitrogen": "Deficient",
    "phosphorus": "Adequate",
    "potassium": "Adequate",
    "calcium": "Adequate",
    "magnesium": "Low",
    "sulfur": "Adequate",
    "iron": "Deficient",
    "zinc": "Adequate",
    "manganese": "Adequate",
    "boron": "Adequate",
    "copper": "Adequate",
    "molybdenum": "Adequate",
    "chlorine": "Adequate",
    "nickel": "Adequate",
    "primary_deficiency": "Nitrogen",
    "secondary_deficiencies": ["Iron", "Magnesium"],
    "severity": "Moderate"
  },
  "pest_disease": {
    "pests_detected": [],
    "diseases_detected": [
      {
        "type": "Early Blight",
        "category": "Fungal",
        "severity": "Moderate",
        "affected_parts": ["Leaves"],
        "confidence": 0.85
      }
    ],
    "damage_pattern": "Circular lesions on lower leaves",
    "affected_area_percentage": 15,
    "spread_risk": "Medium",
    "primary_issue": "Early Blight"
  },
  "environmental_stress": {
    "water_status": "Adequate",
    "temperature_stress": "None",
    "light_stress": "None",
    "physical_damage": "None",
    "chemical_damage": "None",
    "soil_indicators": "Normal",
    "primary_stressor": "None"
  },
  "cross_factor_insights": [
    {
      "insight": "Nitrogen deficiency combined with fungal infection may worsen quickly",
      "confidence": 0.8,
      "affected_aspects": ["Nutrient", "Disease"],
      "impact_level": "High"
    }
  ],
  "recommendations": {
    "immediate": [
      {
        "action": "Apply nitrogen fertilizer",
        "details": "Use urea or ammonium nitrate (20-30 kg/ha)",
        "timeline": "Within 24 hours",
        "priority": "High"
      }
    ],
    "short_term": [
      {
        "action": "Apply fungicide",
        "details": "Copper-based fungicide for early blight control",
        "timeline": "Within 3 days",
        "priority": "Medium"
      }
    ],
    "preventive": [
      {
        "action": "Regular soil testing",
        "details": "Test soil every 2 weeks for nutrient levels",
        "timeline": "Ongoing",
        "priority": "Low"
      }
    ],
    "monitoring": [
      {
        "parameter": "Leaf color",
        "frequency": "Daily",
        "threshold": "Any further yellowing"
      }
    ],
    "resource_estimation": {
      "water_required_liters": "200-300",
      "fertilizer_cost_estimate_usd": "50-75",
      "labor_hours_estimate": "2-3"
    },
    "localized_recommendations": {
      "region": "Mediterranean",
      "preferred_practices": ["Drip irrigation", "Organic fertilizers"],
      "restricted_methods": ["Flood irrigation"]
    }
  },
  "summary": {
    "overall_health_score": 70,
    "primary_concern": "Nitrogen deficiency with early blight infection",
    "secondary_concerns": ["Magnesium deficiency", "Iron deficiency"],
    "critical_issues_count": 1,
    "confidence_level": 85,
    "prognosis": "Good with immediate treatment",
    "estimated_yield_impact": "10-15% reduction if untreated"
  },
  "analysis_id": "async_analysis_20250101_120000_a1b2c3d4",
  "timestamp": "2025-01-01T12:05:00Z",
  "user_id": 46,
  "farmer_id": "F046",
  "sponsor_id": "S003",
  "SponsorUserId": 12,
  "SponsorshipCodeId": 34,
  "location": "Antalya, Turkey",
  "gps_coordinates": {
    "Lat": 36.8969,
    "Lng": 30.7133
  },
  "altitude": 42,
  "field_id": "FIELD_001",
  "crop_type": "Tomato",
  "planting_date": "2024-12-01T00:00:00Z",
  "expected_harvest_date": "2025-03-01T00:00:00Z",
  "last_fertilization": "2024-12-15T00:00:00Z",
  "last_irrigation": "2024-12-30T00:00:00Z",
  "previous_treatments": ["NPK 20-20-20", "Fungicide spray"],
  "weather_conditions": "Sunny, 25°C",
  "temperature": 25.5,
  "humidity": 65.0,
  "soil_type": "Loamy",
  "urgency_level": "Medium",
  "notes": "Yellowing leaves observed",
  "contact_info": {
    "Phone": "+90555123456",
    "Email": "farmer@example.com"
  },
  "additional_info": {
    "IrrigationSystem": "Drip",
    "FertilizerType": "Organic"
  },
  "image_url": "https://iili.io/FDuqN99.jpg",
  "image_path": "https://iili.io/FDuqN99.jpg",
  "image_metadata": {
    "Format": "JPEG",
    "URL": "https://iili.io/FDuqN99.jpg",
    "SizeKb": 245.6,
    "UploadTimestamp": "2025-01-01T12:00:05Z"
  },
  "processing_metadata": {
    "ParseSuccess": true,
    "ProcessingTimestamp": "2025-01-01T12:05:00Z",
    "AiModel": "gpt-4o-mini",
    "WorkflowVersion": "2.0.0",
    "ReceivedAt": "2025-01-01T12:00:10Z",
    "ProcessingTimeMs": 4523,
    "RetryCount": 0,
    "Priority": "medium"
  },
  "token_usage": {
    "total_tokens": 8542,
    "prompt_tokens": 7125,
    "completion_tokens": 1417,
    "cost_usd": 0.004271,
    "cost_try": 0.14
  },
  "risk_assessment": {
    "yield_loss_probability": "Medium",
    "timeline_to_worsen": "7-10 days",
    "spread_potential": "Medium"
  },
  "confidence_notes": [
    {
      "aspect": "Disease Identification",
      "confidence": 0.85,
      "reason": "Clear visual symptoms match early blight pattern"
    }
  ],
  "farmer_friendly_summary": "Your tomato plant shows signs of nitrogen deficiency and early blight disease. Immediate fertilizer application and fungicide treatment recommended.",
  "success": true,
  "error": false,
  "error_message": null,
  "error_type": null
}
```

---

## 6. Testing Strategy

### Integration Test Plan

1. **Request Deserialization Test**:
   - Load actual WebAPI message from `plant-analysis-requests` queue
   - Verify TypeScript worker deserializes all fields correctly
   - Check null handling for optional fields

2. **Response Serialization Test**:
   - Generate response in TypeScript worker
   - Serialize to JSON
   - Parse with C# `PlantAnalysisAsyncResponseDto` deserializer
   - Verify all fields map correctly to database entity

3. **End-to-End Test**:
   - WebAPI publishes test request
   - TypeScript worker processes (mock AI response)
   - PlantAnalysisWorkerService consumes result
   - Verify database record matches expected values

### Validation Checklist

- [ ] All snake_case fields use underscores
- [ ] `SponsorUserId` and `SponsorshipCodeId` are PascalCase
- [ ] `ProcessingMetadata` fields are PascalCase
- [ ] Dates are ISO 8601 format
- [ ] All required nested objects present
- [ ] Image URLs populated correctly
- [ ] Token usage calculated accurately
- [ ] Success/error flags set appropriately

---

## 7. Related Files

**C# Source Files**:
- [PlantAnalysisAsyncRequestDto.cs](../../Entities/Dtos/PlantAnalysisAsyncRequestDto.cs) - Request format
- [PlantAnalysisAsyncResponseDto.cs](../../Entities/Dtos/PlantAnalysisAsyncResponseDto.cs) - Response format
- [PlantAnalysisAsyncService.cs](../../Business/Services/PlantAnalysis/PlantAnalysisAsyncService.cs) - Request publisher
- [PlantAnalysisJobService.cs](../../PlantAnalysisWorkerService/Jobs/PlantAnalysisJobService.cs) - Response consumer
- [RabbitMQConsumerWorker.cs](../../PlantAnalysisWorkerService/Services/RabbitMQConsumerWorker.cs) - Queue consumer

**TypeScript Source Files**:
- [messages.ts](../../../analysis-worker/src/types/messages.ts) - Request/Response interfaces
- [index.ts](../../../analysis-worker/src/index.ts) - Main worker logic

---

## Summary

**Critical Requirements**:

1. ✅ **Request Format**: TypeScript worker MUST correctly deserialize `PlantAnalysisAsyncRequestDto` from WebAPI
2. ✅ **Response Format**: TypeScript worker MUST produce EXACT format matching `PlantAnalysisAsyncResponseDto`
3. ⚠️ **Snake_case Exception**: `SponsorUserId` and `SponsorshipCodeId` are PascalCase (NOT snake_case)
4. ⚠️ **ProcessingMetadata PascalCase**: All `ProcessingMetadata` fields use PascalCase
5. ✅ **Image URL**: Must set `ImageMetadata.URL` for database storage
6. ✅ **Status Flags**: `success: true`, `error: false` for successful analyses

**Last Updated**: 2025-12-01
**Author**: Analysis Worker Modernization Team
**Version**: 1.0.0
