/**
 * Anthropic AI Provider Implementation
 * 
 * NEW implementation for multi-provider failover strategy
 * Uses same Turkish prompt and output structure as OpenAI for consistency
 * 
 * Model: claude-3-5-sonnet-20241022
 * Pricing: Input $3/M, Output $15/M
 */

import Anthropic from '@anthropic-ai/sdk';
import { ProviderAnalysisMessage, AnalysisResultMessage } from '../types/messages';
import pino from 'pino';
import * as defaults from './defaults';

export class AnthropicProvider {
  private client: Anthropic;
  private logger: pino.Logger;

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

    // String format
    if (typeof gpsInput === 'string') {
      const match = gpsInput.match(/(-?\d+\.?\d*)[,\s]+(-?\d+\.?\d*)/);
      if (match) {
        return {
          lat: parseFloat(match[1]),
          lng: parseFloat(match[2]),
        };
      }
    }

    return undefined;
  }

  constructor(apiKey: string, logger: pino.Logger) {
    this.client = new Anthropic({ apiKey });
    this.logger = logger;
  }

  /**
   * Main analysis method - calls Anthropic API with multi-image support
   */
  async analyzeImages(message: ProviderAnalysisMessage): Promise<AnalysisResultMessage> {
    const startTime = Date.now();

    try {
      const systemPrompt = this.buildSystemPrompt(message);
      const imageContent = await this.buildImageContent(message);

      this.logger.debug({
        analysisId: message.analysis_id,
        imageCount: imageContent.length,
        model: 'claude-3-5-sonnet-20241022',
      }, 'Calling Anthropic API');

      const response = await this.client.messages.create({
        model: 'claude-3-5-sonnet-20241022',
        max_tokens: 2000,
        temperature: 0.7,
        system: systemPrompt,
        messages: [
          {
            role: 'user',
            content: imageContent,
          },
        ],
      });

      const analysisText = response.content[0]?.type === 'text' ? response.content[0].text : '';
      const processingTimeMs = Date.now() - startTime;

      this.logger.debug({
        analysisId: message.analysis_id,
        responseLength: analysisText.length,
        processingTimeMs,
      }, 'Anthropic API response received');

      const analysisResult = this.parseAnalysisResponse(analysisText, message);
      const tokenUsage = this.calculateTokenUsage(response, message);

      return {
        ...analysisResult,
        processing_metadata: {
          parse_success: true,
          processing_timestamp: new Date().toISOString(),
          processing_time_ms: processingTimeMs,
          ai_model: 'claude-3-5-sonnet-20241022',
          workflow_version: '2.0-typescript-worker',
          image_source: message.image.startsWith('http') ? 'url' : 'base64',
        },
        token_usage: tokenUsage,
      };
    } catch (error: any) {
      const processingTimeMs = Date.now() - startTime;
      const errorMessage = error?.message || 'Unknown Anthropic API error';

      this.logger.error({
        analysisId: message.analysis_id,
        error: errorMessage,
        processingTimeMs,
      }, 'Anthropic analysis failed');

      return this.buildErrorResponse(message, errorMessage, processingTimeMs);
    }
  }

  /**
   * Build system prompt - SAME as OpenAI for consistency
   * Uses exact Turkish prompt from n8n flow
   */
  private buildSystemPrompt(message: ProviderAnalysisMessage): string {
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

    // Conditional multi-image instructions based on what's provided
    if (message.leaf_top_image) {
      prompt += `\n**LEAF TOP IMAGE (Yaprağın Üst Yüzeyi):** Leaf top image provided\nFocus on: Upper leaf surface symptoms, color variations, spots, lesions, powdery mildew, rust, insect feeding damage, nutrient deficiency patterns (interveinal chlorosis, etc.)\n`;
    }

    if (message.leaf_bottom_image) {
      prompt += `\n**LEAF BOTTOM IMAGE (Yaprağın Alt Yüzeyi):** Leaf bottom image provided\nFocus on: Lower leaf surface, stomata conditions, fungal growth (downy mildew, rust pustules), aphids, whiteflies, spider mite webbing, nutrient deficiency symptoms on underside\n`;
    }

    if (message.plant_overview_image) {
      prompt += `\n**PLANT OVERVIEW IMAGE (Bitkinin Genel Görünümü):** Plant overview image provided\nFocus on: Overall plant structure, growth habit, canopy health, wilting patterns, stem condition, distribution of symptoms across the plant, environmental stress indicators\n`;
    }

    if (message.root_image) {
      prompt += `\n**ROOT IMAGE (Kök Resmi):** Root image provided\nFocus on: Root system health, root rot, discoloration, root hair development, soil-borne disease symptoms, waterlogging damage, root-knot nematode galls\n`;
    }

    prompt += `\n**Number of images provided:** ${totalImages}\n**Images included:** ${imagesProvided}\n\n`;

    prompt += `============================================
CRITICAL INSTRUCTIONS
============================================

1. **COMPREHENSIVE MULTI-IMAGE ANALYSIS:**
   - If multiple images are provided, analyze them TOGETHER
   - Cross-reference symptoms across different images
   - Provide a unified diagnosis based on all available visual information

2. **ACCURATE SPECIES IDENTIFICATION:**
   - Carefully identify the plant species and variety
   - Use botanical characteristics visible in the images
   - If uncertain, provide your best assessment with confidence score

3. **DETAILED NUTRIENT DEFICIENCY ANALYSIS:**
   - Assess EACH of the 14 key nutrients individually
   - Use specific visual symptoms (chlorosis patterns, necrosis, stunting, etc.)
   - Identify primary and secondary deficiencies
   - Rate severity level

4. **PEST AND DISEASE DIAGNOSIS:**
   - Look for visible pests (insects, mites, etc.)
   - Identify disease symptoms (fungal, bacterial, viral)
   - Describe damage patterns and affected areas
   - Estimate spread risk

5. **ENVIRONMENTAL STRESS ASSESSMENT:**
   - Identify water stress (drought or waterlogging)
   - Detect temperature stress (heat or cold damage)
   - Recognize light-related issues
   - Note physical damage

6. **ACTIONABLE RECOMMENDATIONS:**
   - Provide SPECIFIC, PRACTICAL recommendations
   - Prioritize immediate actions
   - Include preventive measures
   - Estimate urgency and timeframe

`;

    // Add context information if provided
    prompt += `\n============================================\nCONTEXT INFORMATION PROVIDED\n============================================\n\n`;
    prompt += `Analysis ID: ${message.analysis_id}\n`;
    prompt += `Farmer ID: ${message.farmer_id || 'Not provided'}\n`;
    prompt += `Location: ${message.location || 'Not provided'}\n`;

    if (message.gps_coordinates) {
      const coords =
        typeof message.gps_coordinates === 'string'
          ? message.gps_coordinates
          : `${message.gps_coordinates.lat}, ${message.gps_coordinates.lng}`;
      prompt += `GPS Coordinates: ${coords}\n`;
    }

    if (message.altitude) prompt += `Altitude: ${message.altitude}m\n`;
    if (message.crop_type) prompt += `Crop Type: ${message.crop_type}\n`;
    if (message.soil_type) prompt += `Soil Type: ${message.soil_type}\n`;
    if (message.weather_conditions) prompt += `Weather: ${message.weather_conditions}\n`;
    if (message.temperature) prompt += `Temperature: ${message.temperature}°C\n`;
    if (message.humidity) prompt += `Humidity: ${message.humidity}%\n`;
    if (message.last_fertilization) prompt += `Last Fertilization: ${message.last_fertilization}\n`;
    if (message.last_irrigation) prompt += `Last Irrigation: ${message.last_irrigation}\n`;

    if (message.previous_treatments && message.previous_treatments.length > 0) {
      prompt += `Previous Treatments:\n`;
      message.previous_treatments.forEach((treatment, index) => {
        prompt += `  ${index + 1}. ${treatment}\n`;
      });
    }

    if (message.urgency_level) prompt += `Urgency Level: ${message.urgency_level}\n`;
    if (message.notes) prompt += `Additional Notes: ${message.notes}\n`;

    // Add JSON schema
    prompt += `\n============================================\nOUTPUT FORMAT\n============================================\n\n`;
    prompt += `Return ONLY a valid JSON object with this EXACT structure (no additional text):\n\n`;

    prompt += `{
  "plant_identification": {
    "species": "string (bitki türü, örn: Domates, Buğday)",
    "variety": "string (çeşit, örn: Rio Grande, emin değilseniz 'Bilinmiyor')",
    "growth_stage": "fide | vejetatif | çiçeklenme | meyve | unknown",
    "confidence": 0.85,
    "identifying_features": ["array of strings", "belirgin özellikler"],
    "visible_parts": ["array of strings", "görünen bitki kısımları"]
  },
  "nutrient_status": {
    "nitrogen": "normal | eksik | fazla | unknown",
    "phosphorus": "normal | eksik | fazla | unknown",
    "potassium": "normal | eksik | fazla | unknown",
    "calcium": "normal | eksik | fazla | unknown",
    "magnesium": "normal | eksik | fazla | unknown",
    "sulfur": "normal | eksik | fazla | unknown",
    "iron": "normal | eksik | fazla | unknown",
    "manganese": "normal | eksik | fazla | unknown",
    "zinc": "normal | eksik | fazla | unknown",
    "copper": "normal | eksik | fazla | unknown",
    "boron": "normal | eksik | fazla | unknown",
    "molybdenum": "normal | eksik | fazla | unknown",
    "chlorine": "normal | eksik | fazla | unknown",
    "nickel": "normal | eksik | fazla | unknown",
    "primary_deficiency": "string (en önemli eksiklik veya 'Yok')",
    "secondary_deficiencies": ["array", "diğer eksiklikler"],
    "severity": "yok | düşük | orta | yüksek | kritik | unknown",
    "visual_symptoms": "string (görsel belirtiler detaylı açıklama)"
  },
  "health_assessment": {
    "overall_health": "sağlıklı | hafif sorunlu | orta sorunlu | ciddi sorunlu | kritik | unknown",
    "vigor": "zayıf | orta | iyi | mükemmel | unknown",
    "leaf_condition": "string (yaprak durumu açıklaması)",
    "stem_condition": "string (gövde durumu açıklaması)",
    "root_condition": "string (kök durumu, görünüyorsa)",
    "color_assessment": "string (renk değerlendirmesi)",
    "abnormalities": ["array", "anormallikler listesi"]
  },
  "pest_disease": {
    "pests_detected": [
      {
        "pest_name": "string (zararlı adı)",
        "scientific_name": "string (bilimsel adı)",
        "severity": "düşük | orta | yüksek | kritik",
        "visible_damage": "string (görünen hasar açıklaması)",
        "lifecycle_stage": "string (yaşam döngüsü evresi)"
      }
    ],
    "diseases_detected": [
      {
        "disease_name": "string (hastalık adı)",
        "pathogen_type": "fungal | bacterial | viral | unknown",
        "severity": "düşük | orta | yüksek | kritik",
        "symptoms": "string (semptom açıklaması)",
        "affected_parts": ["array", "etkilenen kısımlar"]
      }
    ],
    "damage_pattern": "string (hasar pattern açıklaması)",
    "affected_area_percentage": 25,
    "spread_risk": "yok | düşük | orta | yüksek",
    "primary_issue": "string (ana sorun)"
  },
  "environmental_stress": {
    "water_stress": {
      "type": "none | drought | waterlogging | unknown",
      "severity": "yok | düşük | orta | yüksek | unknown",
      "indicators": ["array", "göstergeler"]
    },
    "temperature_stress": {
      "type": "none | heat | cold | unknown",
      "severity": "yok | düşük | orta | yüksek | unknown",
      "indicators": ["array", "göstergeler"]
    },
    "light_stress": {
      "type": "none | insufficient | excessive | unknown",
      "severity": "yok | düşük | orta | yüksek | unknown",
      "indicators": ["array", "göstergeler"]
    },
    "physical_damage": {
      "present": true,
      "type": "string (hasar tipi)",
      "severity": "yok | düşük | orta | yüksek | unknown"
    }
  },
  "recommendations": {
    "immediate_actions": [
      {
        "action": "string (yapılacak iş)",
        "priority": "düşük | orta | yüksek | kritik",
        "timeframe": "string (zaman dilimi)",
        "expected_outcome": "string (beklenen sonuç)"
      }
    ],
    "fertilization": {
      "needed": true,
      "nutrients_to_apply": ["array", "uygulanacak besinler"],
      "application_method": "string (uygulama yöntemi)",
      "timing": "string (zamanlama)"
    },
    "pest_disease_management": {
      "treatment_needed": true,
      "recommended_products": ["array", "önerilen ürünler (genel kategoriler)"],
      "application_timing": "string (uygulama zamanı)",
      "precautions": ["array", "önlemler"]
    },
    "irrigation": {
      "adjustment_needed": true,
      "recommendation": "string (sulama önerisi)",
      "frequency": "string (sıklık)"
    },
    "preventive_measures": ["array", "önleyici tedbirler"],
    "monitoring": "string (izleme önerileri)"
  },
  "summary": {
    "main_findings": "string (ana bulgular özeti - 2-3 cümle)",
    "diagnosis": "string (teşhis - 2-3 cümle)",
    "action_summary": "string (yapılacaklar özeti - 2-3 cümle)",
    "urgency": "düşük | orta | yüksek | kritik",
    "prognosis": "mükemmel | iyi | orta | kötü | unknown",
    "confidence_level": 0.85
  },
  "analysis_metadata": {
    "analysis_timestamp": "${new Date().toISOString()}",
    "analysis_id": "${message.analysis_id}",
    "image_quality": "düşük | orta | yüksek",
    "visibility_conditions": "string (görüş koşulları)",
    "limitations": ["array", "analiz kısıtlamaları"]
  }
}`;

    prompt += `\n\n============================================\nIMPORTANT NOTES\n============================================\n\n`;
    prompt += `- Return ONLY the JSON object, no additional text\n`;
    prompt += `- All string values should be in Turkish\n`;
    prompt += `- Use "unknown" when uncertain, but provide best assessment when possible\n`;
    prompt += `- Be specific and actionable in recommendations\n`;
    prompt += `- If multiple images provided, integrate findings from all images\n`;
    prompt += `- Base confidence scores on image quality and symptom clarity\n\n`;

    return prompt;
  }

  /**
   * Build image content for Anthropic API
   * Anthropic uses different format than OpenAI
   */
  private async buildImageContent(message: ProviderAnalysisMessage): Promise<any[]> {
    const content: any[] = [];

    // Helper function to add image
    const addImage = async (imageUrl: string, label: string) => {
      if (!imageUrl) return;

      try {
        // If URL, fetch and convert to base64
        if (imageUrl.startsWith('http')) {
          const response = await fetch(imageUrl);
          const buffer = await response.arrayBuffer();
          const base64 = Buffer.from(buffer).toString('base64');
          const mimeType = response.headers.get('content-type') || 'image/jpeg';

          content.push({
            type: 'image',
            source: {
              type: 'base64',
              media_type: mimeType,
              data: base64,
            },
          });
        } else {
          // Already base64
          const base64Data = imageUrl.replace(/^data:image\/\w+;base64,/, '');
          const mimeType = imageUrl.match(/^data:(image\/\w+);base64,/)
            ? imageUrl.match(/^data:(image\/\w+);base64,/)![1]
            : 'image/jpeg';

          content.push({
            type: 'image',
            source: {
              type: 'base64',
              media_type: mimeType,
              data: base64Data,
            },
          });
        }

        this.logger.debug({ label }, 'Image added to analysis');
      } catch (error: any) {
        this.logger.warn({ label, error: error.message }, 'Failed to process image');
      }
    };

    // Add all provided images
    await addImage(message.image, 'main');
    if (message.leaf_top_image) await addImage(message.leaf_top_image, 'leaf_top');
    if (message.leaf_bottom_image) await addImage(message.leaf_bottom_image, 'leaf_bottom');
    if (message.plant_overview_image) await addImage(message.plant_overview_image, 'plant_overview');
    if (message.root_image) await addImage(message.root_image, 'root');

    // Add text prompt at the end
    content.push({
      type: 'text',
      text: 'Please analyze the provided plant images and return a comprehensive JSON report following the exact structure specified in the system prompt.',
    });

    return content;
  }

  /**
   * Parse AI response - SAME logic as OpenAI for consistency
   */
  private parseAnalysisResponse(
    analysisText: string,
    originalMessage: ProviderAnalysisMessage
  ): AnalysisResultMessage {
    try {
      // Claude sometimes wraps JSON in markdown code blocks
      let cleanedText = analysisText.trim();
      if (cleanedText.startsWith('```json')) {
        cleanedText = cleanedText.replace(/^```json\s*/, '').replace(/\s*```$/, '');
      } else if (cleanedText.startsWith('```')) {
        cleanedText = cleanedText.replace(/^```\s*/, '').replace(/\s*```$/, '');
      }

      const parsed = JSON.parse(cleanedText);

      // Preserve ALL input fields in the result
      return {
        // Original input fields (ALL preserved)
        analysis_id: originalMessage.analysis_id,
        timestamp: originalMessage.timestamp,
        image_url: originalMessage.image,
        user_id: originalMessage.user_id,
        farmer_id: originalMessage.farmer_id,
        sponsor_id: originalMessage.sponsor_id,
        location: originalMessage.location,
        gps_coordinates: this.parseGpsCoordinates(originalMessage.gps_coordinates),
        altitude: originalMessage.altitude,
        field_id: originalMessage.field_id,
        crop_type: originalMessage.crop_type,
        planting_date: originalMessage.planting_date,
        expected_harvest_date: originalMessage.expected_harvest_date,
        last_fertilization: originalMessage.last_fertilization,
        last_irrigation: originalMessage.last_irrigation,
        previous_treatments: originalMessage.previous_treatments,
        weather_conditions: originalMessage.weather_conditions,
        temperature: originalMessage.temperature,
        humidity: originalMessage.humidity,
        soil_type: originalMessage.soil_type,
        urgency_level: originalMessage.urgency_level,
        notes: originalMessage.notes,
        contact_info: originalMessage.contact_info,
        additional_info: originalMessage.additional_info,
        image_metadata: originalMessage.image_metadata,
        rabbitmq_metadata: originalMessage.rabbitmq_metadata,

        // AI Analysis results
        plant_identification: parsed.plant_identification || defaults.getDefaultPlantIdentification(),
        nutrient_status: parsed.nutrient_status || defaults.getDefaultNutrientStatus(),
        health_assessment: parsed.health_assessment || defaults.getDefaultHealthAssessment(),
        pest_disease: parsed.pest_disease || defaults.getDefaultPestDisease(),
        environmental_stress: parsed.environmental_stress || defaults.getDefaultEnvironmentalStress(),
        recommendations: parsed.recommendations || defaults.getDefaultRecommendations(),
        summary: parsed.summary || defaults.getDefaultSummary(),

        // Processing metadata
        processing_metadata: {
          parse_success: true,
          processing_timestamp: new Date().toISOString(),
          processing_time_ms: 0, // Will be set by caller
          ai_model: 'claude-3-5-sonnet-20241022',
          workflow_version: '2.0-typescript-worker',
          image_source: originalMessage.image.startsWith('http') ? 'url' : 'base64',
        },
      };
    } catch (error: any) {
      this.logger.error({ error: error.message, analysisText }, 'Failed to parse Anthropic response');
      return this.buildParseErrorResponse(originalMessage, analysisText);
    }
  }

  /**
   * Calculate token usage and cost - Anthropic specific pricing
   */
  private calculateTokenUsage(response: any, message: ProviderAnalysisMessage): any {
    const usage = response.usage || {};
    const inputTokens = usage.input_tokens || 0;
    const outputTokens = usage.output_tokens || 0;
    const totalTokens = inputTokens + outputTokens;

    // Claude 3.5 Sonnet pricing (December 2024)
    const pricing = {
      input_per_million: 3.0, // $3 per 1M input tokens
      output_per_million: 15.0, // $15 per 1M output tokens
    };

    // Calculate costs
    const inputCostUsd = (inputTokens / 1_000_000) * pricing.input_per_million;
    const outputCostUsd = (outputTokens / 1_000_000) * pricing.output_per_million;
    const totalCostUsd = inputCostUsd + outputCostUsd;

    const usdToTry = 50; // Exchange rate
    const totalCostTry = totalCostUsd * usdToTry;

    // Estimate breakdown
    const estimatedSystemPromptTokens = 4000;
    const estimatedContextTokens = 150;
    const estimatedImageTokens = (message.image_metadata?.total_images || 1) * 765;
    const estimatedImageUrlTokens = (message.image_metadata?.total_images || 1) * 85;

    return {
      summary: {
        model: 'claude-3-5-sonnet-20241022',
        analysis_id: message.analysis_id,
        timestamp: new Date().toISOString(),
        total_tokens: totalTokens,
        total_cost_usd: parseFloat(totalCostUsd.toFixed(6)),
        total_cost_try: parseFloat(totalCostTry.toFixed(4)),
        image_source: message.image.startsWith('http') ? 'url' : 'base64',
      },
      detailed: {
        input_tokens: inputTokens,
        output_tokens: outputTokens,
        input_cost_usd: parseFloat(inputCostUsd.toFixed(6)),
        output_cost_usd: parseFloat(outputCostUsd.toFixed(6)),
      },
      breakdown: {
        system_prompt_tokens: estimatedSystemPromptTokens,
        context_tokens: estimatedContextTokens,
        image_tokens: estimatedImageTokens,
        image_url_tokens: estimatedImageUrlTokens,
        output_tokens: outputTokens,
      },
      pricing: {
        input_per_million: pricing.input_per_million,
        output_per_million: pricing.output_per_million,
        currency: 'USD',
        usd_to_try_rate: usdToTry,
      },
    };
  }

  /**
   * Build error response when analysis fails
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
      image_url: message.image,
      user_id: message.user_id,
      farmer_id: message.farmer_id,
      sponsor_id: message.sponsor_id,
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
      image_metadata: message.image_metadata,
      rabbitmq_metadata: message.rabbitmq_metadata,

      // Default analysis values
      plant_identification: defaults.getDefaultPlantIdentification(),
      nutrient_status: defaults.getDefaultNutrientStatus(),
      health_assessment: defaults.getDefaultHealthAssessment(),
      pest_disease: defaults.getDefaultPestDisease(),
      environmental_stress: defaults.getDefaultEnvironmentalStress(),
      recommendations: defaults.getDefaultRecommendations(),
      summary: {
        overall_health_score: 0,
        primary_concern: `Anthropic API hatası: ${errorMessage}`,
        secondary_concerns: ['Analiz başarısız oldu'],
        critical_issues_count: 0,
        confidence_level: 0,
        prognosis: 'unknown' as const,
        estimated_yield_impact: 'Bilinmiyor',
      },

      processing_metadata: {
        parse_success: false,
        processing_timestamp: new Date().toISOString(),
        processing_time_ms: processingTimeMs,
        ai_model: 'claude-3-5-sonnet-20241022',
        workflow_version: '2.0-typescript-worker',
        image_source: message.image.startsWith('http') ? 'url' : 'base64',
        error_details: errorMessage,
      },
    };
  }

  /**
   * Build response when JSON parsing fails
   */
  private buildParseErrorResponse(
    message: ProviderAnalysisMessage,
    analysisText: string
  ): AnalysisResultMessage {
    return this.buildErrorResponse(
      message,
      `Failed to parse Anthropic response as JSON. Response length: ${analysisText.length}`,
      0
    );
  }


}
