import OpenAI from 'openai';
import { ProviderConfig } from '../types/config';
import { ProviderAnalysisMessage, AnalysisResultMessage } from '../types/messages';
import { Logger } from 'pino';

/**
 * OpenAI provider for plant analysis
 * CRITICAL: This implementation MUST match the n8n ZiraaiV3Async_MultiImage flow exactly
 * Model: gpt-5-mini
 * Prompt: Exact copy from n8n (not a single letter different)
 * Output: Same JSON structure as n8n validation
 */
export class OpenAIProvider {
  private client: OpenAI;
  private config: ProviderConfig;
  private logger: Logger;

  constructor(config: ProviderConfig, logger: Logger) {
    this.config = config;
    this.logger = logger;
    this.client = new OpenAI({
      apiKey: config.apiKey,
      timeout: config.timeout,
      maxRetries: config.retryAttempts,
    });
  }

  /**
   * Analyze plant images using OpenAI Vision API
   * Matches n8n flow exactly - multi-image support with comprehensive Turkish analysis
   */
  async analyzeImages(message: ProviderAnalysisMessage): Promise<AnalysisResultMessage> {
    const startTime = Date.now();

    try {
      this.logger.info({
        analysisId: message.analysis_id,
        farmerId: message.farmer_id,
        sponsorId: message.sponsor_id,
        imageCount: message.image_metadata?.total_images || 1,
        provider: 'openai',
      }, 'Starting OpenAI analysis (n8n flow compatible)');

      // Build system prompt - EXACT copy from n8n flow
      const systemPrompt = this.buildSystemPrompt(message);

      // Build image content array for multi-image support
      const imageContent = this.buildImageContent(message);

      // Call OpenAI API with gpt-5-mini
      const response = await this.client.chat.completions.create({
        model: 'gpt-5-mini', // CRITICAL: Must be gpt-5-mini as per n8n flow
        messages: [
          {
            role: 'system',
            content: systemPrompt,
          },
          {
            role: 'user',
            content: imageContent,
          },
        ],
        temperature: 0.7,
        max_tokens: 2000,
        response_format: { type: 'json_object' }, // Ensure JSON response
      });

      const analysisText = response.choices[0]?.message?.content || '';
      const processingTimeMs = Date.now() - startTime;

      // Parse the AI response - match n8n validation exactly
      const analysisResult = this.parseAnalysisResponse(analysisText, message);

      // Calculate token usage and cost - match n8n pricing
      const tokenUsage = this.calculateTokenUsage(response, message);

      this.logger.info({
        analysisId: message.analysis_id,
        processingTimeMs,
        totalTokens: tokenUsage.summary.total_tokens,
        costUsd: tokenUsage.summary.total_cost_usd,
        provider: 'openai',
      }, 'OpenAI analysis completed successfully');

      // Return result matching n8n output structure exactly
      return {
        ...analysisResult,
        processing_metadata: {
          parse_success: true,
          processing_timestamp: new Date().toISOString(),
          processing_time_ms: processingTimeMs,
          ai_model: 'gpt-5-mini',
          workflow_version: '2.0-typescript-worker',
          image_source: message.image.startsWith('http') ? 'url' : 'base64',
        },
        token_usage: tokenUsage,
      };
    } catch (error) {
      const processingTimeMs = Date.now() - startTime;
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';

      this.logger.error({
        analysisId: message.analysis_id,
        farmerId: message.farmer_id,
        error: errorMessage,
        processingTimeMs,
        provider: 'openai',
      }, 'OpenAI analysis failed');

      // Return error response matching n8n error structure
      return this.buildErrorResponse(message, errorMessage, processingTimeMs);
    }
  }

  /**
   * Build system prompt - EXACT copy from n8n flow (lines 43-44 in JSON)
   * Not a single letter different for business compatibility
   */
  private buildSystemPrompt(message: ProviderAnalysisMessage): string {
    // Extract context values for prompt
    const totalImages = message.image_metadata?.total_images || 1;
    const imagesProvided = message.image_metadata?.images_provided?.join(', ') || 'main';

    // CRITICAL: This is the EXACT prompt from n8n - DO NOT MODIFY
    let prompt = `You are an expert agricultural analyst with deep knowledge in plant pathology, nutrition (macro and micro elements), pest management, physiological disorders, soil science, and environmental stress factors.

Your task is to analyze the provided plant image(s) comprehensively and return a structured JSON report.

============================================
MULTI-IMAGE ANALYSIS (if additional images provided)
============================================

You may receive UP TO 4 DIFFERENT IMAGES of the same plant. Analyze all provided images together for a more comprehensive diagnosis:

**MAIN IMAGE:** Main plant image
This is the primary image for analysis.
`;

    // Add multi-image instructions if additional images are provided
    if (message.leaf_top_image) {
      prompt += `
**LEAF TOP IMAGE (Yaprağın Üst Yüzeyi):** Leaf top image provided
Focus on: Upper leaf surface symptoms, color variations, spots, lesions, powdery mildew, rust, insect feeding damage, nutrient deficiency patterns (interveinal chlorosis, etc.)
`;
    }

    if (message.leaf_bottom_image) {
      prompt += `
**LEAF BOTTOM IMAGE (Yaprağın Alt Yüzeyi):** Leaf bottom image provided
Focus on: Aphid colonies, whiteflies and eggs, spider mites and webs, downy mildew spores, rust pustules, scale insects, stomatal abnormalities
`;
    }

    if (message.plant_overview_image) {
      prompt += `
**PLANT OVERVIEW IMAGE (Bitkinin Genel Görünümü):** Plant overview image provided
Focus on: Overall plant vigor, stunting, wilting patterns, vascular wilt symptoms (one-sided wilting), stem structure, branching pattern, fruit/flower status
`;
    }

    if (message.root_image) {
      prompt += `
**ROOT IMAGE (Kök Resmi):** Root image provided
Focus on: Root color (healthy white vs brown/black rotted), root-knot nematode galling, root rot lesions, root development, fibrous root density, soil-borne disease symptoms
`;
    }

    prompt += `
**MULTI-IMAGE ANALYSIS INSTRUCTIONS:**
- Analyze ALL provided images together for comprehensive diagnosis
- Cross-reference findings between images to confirm or rule out issues
- If symptoms appear in multiple images, this increases diagnostic confidence
- Note any contradictions between different image observations
- If only the main image is provided, base your analysis solely on it
- Total images provided: ${totalImages}
- Available images: ${imagesProvided}

============================================
IMPORTANT INSTRUCTIONS
============================================

All JSON keys must remain in English exactly as provided.

All values must be written in Turkish (e.g., species name, disease description, nutrient status, stress factors, recommendations, summaries, etc.).

Do not mix languages: keys stay in English, values are always Turkish.

Always:

Cross-check visible symptoms with provided environmental, soil, and treatment data.

Distinguish between biotic (pests, diseases) and abiotic (nutrient, environmental, physiological) stress.

Provide confidence scores (0–100) for each major detection.

If information is insufficient or ambiguous, explicitly state uncertainty and suggest what extra farmer input is needed (in Turkish).

Adapt recommendations to regional conditions if location data is available.

Include both scientific explanations and a plain farmer-friendly summary in Turkish.

Provide organic and chemical management options where relevant.

CONTEXT INFORMATION PROVIDED:

Analysis ID: ${message.analysis_id}

Farmer ID: ${message.farmer_id || 'Not provided'}

Location: ${message.location || 'Not provided'}

GPS Coordinates: ${message.gps_coordinates ? JSON.stringify(message.gps_coordinates) : 'Not provided'}

Altitude: ${message.altitude || 'Not provided'} meters

Field ID: ${message.field_id || 'Not provided'}

Crop Type: ${message.crop_type || 'Not provided'}

Planting Date: ${message.planting_date || 'Not provided'}

Expected Harvest: ${message.expected_harvest_date || 'Not provided'}

Soil Type: ${message.soil_type || 'Not provided'}

Last Fertilization: ${message.last_fertilization || 'Not provided'}

Last Irrigation: ${message.last_irrigation || 'Not provided'}

Weather Conditions: ${message.weather_conditions || 'Not provided'}

Temperature: ${message.temperature || 'Not provided'}°C

Humidity: ${message.humidity || 'Not provided'}%

Previous Treatments: ${message.previous_treatments && message.previous_treatments.length > 0 ? JSON.stringify(message.previous_treatments) : 'None'}

Urgency Level: ${message.urgency_level || 'normal'}

Notes from Farmer: ${message.notes || 'None'}

Perform a complete analysis covering ALL of the following aspects:

Return ONLY a valid JSON object with this EXACT structure (no additional text):

{
  "plant_identification": {
    "species": "Türkçe değer",
    "variety": "Türkçe değer veya bilinmiyor",
    "growth_stage": "fide|vejetatif|çiçeklenme|meyve",
    "confidence": 0-100,
    "identifying_features": ["özellik1", "özellik2"],
    "visible_parts": ["yapraklar", "gövde", "çiçekler", "meyveler", "kökler"]
  },
  "health_assessment": {
    "vigor_score": 1-10,
    "leaf_color": "Türkçe açıklama",
    "leaf_texture": "Türkçe açıklama",
    "growth_pattern": "normal|anormal - detay",
    "structural_integrity": "sağlam|orta|zayıf - detay",
    "stress_indicators": ["belirti1", "belirti2"],
    "disease_symptoms": ["belirti1", "belirti2"],
    "severity": "yok|düşük|orta|yüksek|kritik"
  },
  "nutrient_status": {
    "nitrogen": "normal|eksik|fazla",
    "phosphorus": "normal|eksik|fazla",
    "potassium": "normal|eksik|fazla",
    "calcium": "normal|eksik|fazla",
    "magnesium": "normal|eksik|fazla",
    "sulfur": "normal|eksik|fazla",
    "iron": "normal|eksik|fazla",
    "zinc": "normal|eksik|fazla",
    "manganese": "normal|eksik|fazla",
    "boron": "normal|eksik|fazla",
    "copper": "normal|eksik|fazla",
    "molybdenum": "normal|eksik|fazla",
    "chlorine": "normal|eksik|fazla",
    "nickel": "normal|eksik|fazla",
    "primary_deficiency": "ana eksiklik veya yok",
    "secondary_deficiencies": ["eksiklik1", "eksiklik2"],
    "severity": "yok|düşük|orta|yüksek|kritik"
  },
  "pest_disease": {
    "pests_detected": [
      {"type": "zararlı adı", "group": "böcek|akar|nematod|kemirgen|diğer", "severity": "düşük|orta|yüksek", "confidence": 0-100, "location": "bitkinin bölgesi"}
    ],
    "diseases_detected": [
      {"type": "hastalık adı", "category": "fungal|bakteriyel|viral|fizyolojik", "severity": "düşük|orta|yüksek", "affected_parts": ["etkilenen kısımlar"], "confidence": 0-100}
    ],
    "damage_pattern": "zarar deseni açıklaması",
    "affected_area_percentage": 0-100,
    "spread_risk": "yok|düşük|orta|yüksek",
    "primary_issue": "ana sorun veya yok"
  },
  "environmental_stress": {
    "water_status": "optimal|hafif kurak|kurak|hafif fazla|su baskını",
    "temperature_stress": "yok|hafif sıcak|aşırı sıcak|hafif soğuk|aşırı soğuk",
    "light_stress": "yok|yetersiz|aşırı",
    "physical_damage": "yok|rüzgar|dolu|mekanik|hayvan",
    "chemical_damage": "yok|şüpheli|kesin - detay",
    "physiological_disorders": [
      {"type": "güneş yanığı|tuz zararı|don zararı|herbisit zararı|besin toksisitesi", "severity": "düşük|orta|yüksek", "notes": "detaylar"}
    ],
    "soil_health_indicators": {
      "salinity": "yok|hafif|orta|şiddetli",
      "pH_issue": "asidik|alkali|optimal",
      "organic_matter": "düşük|orta|yüksek"
    },
    "primary_stressor": "ana stres faktörü veya yok"
  },
  "cross_factor_insights": [
    {
      "insight": "faktörler arası ilişki açıklaması",
      "confidence": 0.0-1.0,
      "affected_aspects": ["alan1", "alan2"],
      "impact_level": "düşük|orta|yüksek"
    }
  ],
  "risk_assessment": {
    "yield_loss_probability": "düşük|orta|yüksek",
    "timeline_to_worsen": "gün|hafta",
    "spread_potential": "yok|lokal|tarlanın geneli"
  },
  "recommendations": {
    "immediate": [
      {"action": "ne yapılmalı", "details": "özel talimat", "timeline": "X saat içinde", "priority": "kritik|yüksek|orta"}
    ],
    "short_term": [
      {"action": "ne yapılmalı", "details": "özel talimat", "timeline": "X-Y gün", "priority": "yüksek|orta|düşük"}
    ],
    "preventive": [
      {"action": "önlem", "details": "özel talimat", "timeline": "sürekli", "priority": "orta|düşük"}
    ],
    "monitoring": [
      {"parameter": "izlenecek parametre", "frequency": "sıklık", "threshold": "tetikleyici eşik"}
    ],
    "resource_estimation": {
      "water_required_liters": "litre cinsinden",
      "fertilizer_cost_estimate_usd": "maliyet $",
      "labor_hours_estimate": "saat"
    },
    "localized_recommendations": {
      "region": "bölge adı",
      "preferred_practices": ["uygulama1", "uygulama2"],
      "restricted_methods": ["yasaklı yöntem1", "yasaklı yöntem2"]
    }
  },
  "summary": {
    "overall_health_score": 1-10,
    "primary_concern": "en kritik sorun",
    "secondary_concerns": ["diğer önemli sorunlar"],
    "critical_issues_count": 0-N,
    "confidence_level": 0-100,
    "prognosis": "mükemmel|iyi|orta|zayıf|kritik",
    "estimated_yield_impact": "yok|minimal|orta|önemli|çok ciddi"
  },
  "confidence_notes": [
    {"aspect": "nutrient_status", "confidence": 0.85, "reason": "Türkçe açıklama"}
  ],
  "farmer_friendly_summary": "Çiftçi için sade Türkçe açıklama."
}`;

    return prompt;
  }

  /**
   * Build image content array for OpenAI Vision API
   * Supports up to 5 images (main + 4 optional)
   */
  private buildImageContent(message: ProviderAnalysisMessage): any[] {
    const content: any[] = [
      {
        type: 'text',
        text: `Analyze this image: Main plant image`,
      },
    ];

    // Add main image
    content.push({
      type: 'image_url',
      image_url: {
        url: message.image,
        detail: 'high', // High detail for better analysis
      },
    });

    // Add leaf top image if provided
    if (message.leaf_top_image) {
      content.push({
        type: 'image_url',
        image_url: {
          url: message.leaf_top_image,
          detail: 'high',
        },
      });
    }

    // Add leaf bottom image if provided
    if (message.leaf_bottom_image) {
      content.push({
        type: 'image_url',
        image_url: {
          url: message.leaf_bottom_image,
          detail: 'high',
        },
      });
    }

    // Add plant overview image if provided
    if (message.plant_overview_image) {
      content.push({
        type: 'image_url',
        image_url: {
          url: message.plant_overview_image,
          detail: 'high',
        },
      });
    }

    // Add root image if provided
    if (message.root_image) {
      content.push({
        type: 'image_url',
        image_url: {
          url: message.root_image,
          detail: 'high',
        },
      });
    }

    return content;
  }

  /**
   * Parse AI response into structured format
   * Matches n8n validation exactly - preserves ALL input fields
   */
  private parseAnalysisResponse(
    analysisText: string,
    message: ProviderAnalysisMessage
  ): AnalysisResultMessage {
    try {
      // Parse JSON from AI response
      const analysis = JSON.parse(analysisText);

      // CRITICAL: Preserve ALL input fields (matching n8n flow)
      return {
        // Original input fields
        analysis_id: message.analysis_id,
        timestamp: message.timestamp,
        farmer_id: message.farmer_id,
        sponsor_id: message.sponsor_id,
        user_id: message.user_id,
        location: message.location,
        gps_coordinates: this.parseGpsCoordinates(message.gps_coordinates),
        altitude: message.altitude,
        field_id: message.field_id,
        crop_type: message.crop_type,
        planting_date: message.planting_date,
        expected_harvest_date: message.expected_harvest_date,
        last_fertilization: message.last_fertilization,
        last_irrigation: message.last_irrigation,
        previous_treatments: message.previous_treatments,
        weather_conditions: message.weather_conditions,
        temperature: message.temperature,
        humidity: message.humidity,
        soil_type: message.soil_type,
        urgency_level: message.urgency_level,
        notes: message.notes,
        contact_info: message.contact_info,
        additional_info: message.additional_info,
        image_url: message.image,
        image_metadata: message.image_metadata,
        rabbitmq_metadata: message.rabbitmq_metadata,

        // AI analysis results (all required sections)
        plant_identification: analysis.plant_identification || this.getDefaultPlantIdentification(),
        health_assessment: analysis.health_assessment || this.getDefaultHealthAssessment(),
        nutrient_status: analysis.nutrient_status || this.getDefaultNutrientStatus(),
        pest_disease: analysis.pest_disease || this.getDefaultPestDisease(),
        environmental_stress: analysis.environmental_stress || this.getDefaultEnvironmentalStress(),
        cross_factor_insights: analysis.cross_factor_insights || [],
        risk_assessment: analysis.risk_assessment,
        recommendations: analysis.recommendations || this.getDefaultRecommendations(),
        summary: analysis.summary || this.getDefaultSummary(),
        confidence_notes: analysis.confidence_notes,
        farmer_friendly_summary: analysis.farmer_friendly_summary,

        // Processing metadata (added by caller)
        processing_metadata: {
          parse_success: true,
          processing_timestamp: new Date().toISOString(),
          processing_time_ms: 0, // Will be set by caller
          ai_model: 'gpt-5-mini',
          workflow_version: '2.0-typescript-worker',
          image_source: message.image.startsWith('http') ? 'url' : 'base64',
        },
      };
    } catch (error) {
      this.logger.warn({ error }, 'Failed to parse JSON response, using fallback');
      throw error;
    }
  }

  /**
   * Parse GPS coordinates from string or object format
   */
  private parseGpsCoordinates(gpsInput: any): { lat: number; lng: number } | undefined {
    if (!gpsInput) return undefined;

    // Already in object format
    if (typeof gpsInput === 'object' && gpsInput !== null) {
      return {
        lat: parseFloat(gpsInput.lat || gpsInput.Lat || 0),
        lng: parseFloat(gpsInput.lng || gpsInput.Lng || gpsInput.lon || gpsInput.Lon || 0),
      };
    }

    // String format: "39.9334,32.8597"
    if (typeof gpsInput === 'string' && gpsInput.includes(',')) {
      const parts = gpsInput.split(',').map((p) => p.trim());
      if (parts.length >= 2) {
        const lat = parseFloat(parts[0]);
        const lng = parseFloat(parts[1]);
        if (!isNaN(lat) && !isNaN(lng)) {
          return { lat, lng };
        }
      }
    }

    return undefined;
  }

  /**
   * Calculate token usage and cost - matches n8n pricing exactly
   * gpt-5-mini pricing:
   * - Input: $0.250/M tokens
   * - Cached input: $0.025/M tokens
   * - Output: $2.000/M tokens
   */
  private calculateTokenUsage(response: any, message: ProviderAnalysisMessage): any {
    const usage = response.usage || {};
    const inputTokens = usage.prompt_tokens || 0;
    const outputTokens = usage.completion_tokens || 0;
    const totalTokens = usage.total_tokens || inputTokens + outputTokens;

    // Pricing per million tokens (from n8n flow line 155)
    const pricing = {
      input_per_million: 0.250,
      cached_input_per_million: 0.025,
      output_per_million: 2.000,
    };

    // Assume no caching for now (can be enhanced later)
    const cachedInputTokens = 0;
    const regularInputTokens = inputTokens;

    // Calculate costs
    const inputCostUsd = (regularInputTokens / 1_000_000) * pricing.input_per_million;
    const cachedInputCostUsd = (cachedInputTokens / 1_000_000) * pricing.cached_input_per_million;
    const outputCostUsd = (outputTokens / 1_000_000) * pricing.output_per_million;
    const totalCostUsd = inputCostUsd + cachedInputCostUsd + outputCostUsd;

    // USD to TRY conversion (can be made configurable)
    const usdToTry = 50;
    const totalCostTry = totalCostUsd * usdToTry;

    // Match n8n token report structure exactly
    return {
      summary: {
        model: 'gpt-5-mini',
        analysis_id: message.analysis_id,
        timestamp: new Date().toISOString(),
        total_tokens: totalTokens,
        total_cost_usd: parseFloat(totalCostUsd.toFixed(6)),
        total_cost_try: parseFloat(totalCostTry.toFixed(4)),
        image_source: message.image.startsWith('http') ? 'url' : 'base64',
      },
      token_breakdown: {
        input: {
          system_prompt: 250, // Estimated
          context_data: 50, // Estimated
          image: (message.image_metadata?.total_images || 1) * 765, // 765 tokens per image
          image_url_text: 10, // Estimated
          cached_input_tokens: cachedInputTokens,
          regular_input_tokens: regularInputTokens,
          total: inputTokens,
        },
        output: {
          response: outputTokens,
          total: outputTokens,
        },
        grand_total: totalTokens,
      },
      cost_breakdown: {
        input_cost_usd: parseFloat(inputCostUsd.toFixed(6)),
        cached_input_cost_usd: parseFloat(cachedInputCostUsd.toFixed(6)),
        output_cost_usd: parseFloat(outputCostUsd.toFixed(6)),
        total_cost_usd: parseFloat(totalCostUsd.toFixed(6)),
        total_cost_try: parseFloat(totalCostTry.toFixed(4)),
        exchange_rate: usdToTry,
      },
    };
  }

  /**
   * Build error response matching n8n error structure
   */
  private buildErrorResponse(
    message: ProviderAnalysisMessage,
    errorMessage: string,
    processingTimeMs: number
  ): AnalysisResultMessage {
    return {
      // Preserve all input fields
      analysis_id: message.analysis_id,
      timestamp: message.timestamp,
      farmer_id: message.farmer_id,
      sponsor_id: message.sponsor_id,
      user_id: message.user_id,
      location: message.location,
      gps_coordinates: this.parseGpsCoordinates(message.gps_coordinates),
      altitude: message.altitude,
      field_id: message.field_id,
      crop_type: message.crop_type,
      planting_date: message.planting_date,
      expected_harvest_date: message.expected_harvest_date,
      last_fertilization: message.last_fertilization,
      last_irrigation: message.last_irrigation,
      previous_treatments: message.previous_treatments,
      weather_conditions: message.weather_conditions,
      temperature: message.temperature,
      humidity: message.humidity,
      soil_type: message.soil_type,
      urgency_level: message.urgency_level,
      notes: message.notes,
      contact_info: message.contact_info,
      additional_info: message.additional_info,
      image_url: message.image,
      image_metadata: message.image_metadata,
      rabbitmq_metadata: message.rabbitmq_metadata,

      // Error fields
      error: true,
      error_message: errorMessage,
      error_type: 'analysis_error',

      // Default analysis sections
      plant_identification: this.getDefaultPlantIdentification(),
      health_assessment: this.getDefaultHealthAssessment(),
      nutrient_status: this.getDefaultNutrientStatus(),
      pest_disease: this.getDefaultPestDisease(),
      environmental_stress: this.getDefaultEnvironmentalStress(),
      recommendations: {
        immediate: [
          {
            action: 'Manuel inceleme gerekli',
            details: 'Otomatik analiz hatası nedeniyle uzman değerlendirmesi gerekiyor.',
            timeline: 'En kısa sürede',
            priority: 'kritik' as const,
          },
        ],
        short_term: [],
        preventive: [],
        monitoring: [],
      },
      summary: {
        overall_health_score: 0,
        primary_concern: 'Analiz başarısız - manuel inceleme gerekli',
        secondary_concerns: ['Sistem hatası oluştu'],
        critical_issues_count: 0,
        confidence_level: 0,
        prognosis: 'unknown' as const,
        estimated_yield_impact: 'Bilinmiyor',
      },

      // Processing metadata
      processing_metadata: {
        parse_success: false,
        processing_timestamp: new Date().toISOString(),
        processing_time_ms: processingTimeMs,
        ai_model: 'gpt-5-mini',
        workflow_version: '2.0-typescript-worker',
        image_source: message.image.startsWith('http') ? 'url' : 'base64',
        error_details: errorMessage,
      },
    };
  }

  // Default section generators (matching n8n defaults)
  private getDefaultPlantIdentification() {
    return {
      species: 'Belirlenemedi',
      variety: 'bilinmiyor',
      growth_stage: 'unknown' as const,
      confidence: 0,
      identifying_features: [],
      visible_parts: [],
    };
  }

  private getDefaultHealthAssessment() {
    return {
      vigor_score: 5,
      leaf_color: 'Analiz edilemedi',
      leaf_texture: 'Analiz edilemedi',
      growth_pattern: 'Analiz edilemedi',
      structural_integrity: 'Analiz edilemedi',
      stress_indicators: [],
      disease_symptoms: [],
      severity: 'unknown' as const,
    };
  }

  private getDefaultNutrientStatus() {
    return {
      nitrogen: 'unknown' as const,
      phosphorus: 'unknown' as const,
      potassium: 'unknown' as const,
      calcium: 'unknown' as const,
      magnesium: 'unknown' as const,
      sulfur: 'unknown' as const,
      iron: 'unknown' as const,
      zinc: 'unknown' as const,
      manganese: 'unknown' as const,
      boron: 'unknown' as const,
      copper: 'unknown' as const,
      molybdenum: 'unknown' as const,
      chlorine: 'unknown' as const,
      nickel: 'unknown' as const,
      primary_deficiency: 'yok',
      secondary_deficiencies: [],
      severity: 'unknown' as const,
    };
  }

  private getDefaultPestDisease() {
    return {
      pests_detected: [],
      diseases_detected: [],
      damage_pattern: 'Analiz edilemedi',
      affected_area_percentage: 0,
      spread_risk: 'yok' as const,
      primary_issue: 'yok',
    };
  }

  private getDefaultEnvironmentalStress() {
    return {
      water_status: 'unknown',
      temperature_stress: 'unknown',
      light_stress: 'unknown',
      physical_damage: 'unknown',
      chemical_damage: 'unknown',
      primary_stressor: 'yok',
    };
  }

  private getDefaultRecommendations() {
    return {
      immediate: [],
      short_term: [],
      preventive: [],
      monitoring: [],
    };
  }

  private getDefaultSummary() {
    return {
      overall_health_score: 5,
      primary_concern: 'Analiz tamamlanamadı',
      secondary_concerns: [],
      critical_issues_count: 0,
      confidence_level: 0,
      prognosis: 'unknown' as const,
      estimated_yield_impact: 'Bilinmiyor',
    };
  }

  /**
   * Health check
   */
  async healthCheck(): Promise<boolean> {
    try {
      await this.client.models.list();
      return true;
    } catch (error) {
      this.logger.error({ error }, 'OpenAI health check failed');
      return false;
    }
  }
}
