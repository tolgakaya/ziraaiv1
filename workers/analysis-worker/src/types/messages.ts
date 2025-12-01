/**
 * Message types for RabbitMQ communication
 * CRITICAL: These types MUST match the n8n flow exactly for business compatibility
 */

/**
 * Raw analysis message from WebAPI
 * Contains all context fields from n8n ZiraaiV3Async_MultiImage flow
 */
export interface RawAnalysisMessage {
  // Core analysis fields
  analysis_id: string;
  timestamp: string;

  // Multi-image support (up to 5 images)
  image: string; // Main image (required) - URL or base64
  leaf_top_image?: string; // Yaprağın üst yüzeyi (optional)
  leaf_bottom_image?: string; // Yaprağın alt yüzeyi (optional)
  plant_overview_image?: string; // Bitkinin genel görünümü (optional)
  root_image?: string; // Kök resmi (optional)

  // User identification
  user_id?: string | number;
  farmer_id?: string | number;
  sponsor_id?: string | number;

  // Location data
  location?: string;
  gps_coordinates?: string | { lat: number; lng: number };
  altitude?: number;

  // Field and crop information
  field_id?: string | number;
  crop_type?: string;
  planting_date?: string;
  expected_harvest_date?: string;

  // Agricultural practices
  last_fertilization?: string;
  last_irrigation?: string;
  previous_treatments?: string[];

  // Environmental conditions
  weather_conditions?: string;
  temperature?: number;
  humidity?: number;
  soil_type?: string;

  // Additional information
  urgency_level?: 'low' | 'normal' | 'high' | 'critical';
  notes?: string;
  contact_info?: {
    phone?: string;
    email?: string;
  };
  additional_info?: {
    irrigation_method?: string;
    greenhouse?: boolean;
    organic_certified?: boolean;
  };

  // Image metadata
  image_metadata?: {
    format?: string;
    size_bytes?: number;
    size_kb?: number;
    size_mb?: number;
    base64_length?: number;
    upload_timestamp?: string;
    total_images?: number;
    images_provided?: string[];
    has_leaf_top?: boolean;
    has_leaf_bottom?: boolean;
    has_plant_overview?: boolean;
    has_root?: boolean;
  };

  // RabbitMQ metadata
  rabbitmq_metadata?: {
    correlation_id?: string;
    response_queue?: string;
    callback_url?: string;
    priority?: 'low' | 'normal' | 'high';
    retry_count?: number;
    received_at?: string;
    message_id?: string;
    routing_key?: string;
  };
}

/**
 * Analysis result message - matches n8n output structure exactly
 */
export interface AnalysisResultMessage {
  // All original input fields preserved
  analysis_id: string;
  timestamp: string;
  farmer_id?: string | number;
  sponsor_id?: string | number;
  user_id?: string | number;
  location?: string;
  gps_coordinates?: { lat: number; lng: number };
  altitude?: number;
  field_id?: string | number;
  crop_type?: string;
  planting_date?: string;
  expected_harvest_date?: string;
  last_fertilization?: string;
  last_irrigation?: string;
  previous_treatments?: string[];
  weather_conditions?: string;
  temperature?: number;
  humidity?: number;
  soil_type?: string;
  urgency_level?: string;
  notes?: string;
  contact_info?: any;
  additional_info?: any;
  image_url?: string;
  image_metadata?: any;
  request_metadata?: any;
  rabbitmq_metadata?: any;

  // AI Analysis results
  plant_identification: {
    species: string;
    variety: string;
    growth_stage: 'fide' | 'vejetatif' | 'çiçeklenme' | 'meyve' | 'unknown';
    confidence: number;
    identifying_features: string[];
    visible_parts: string[];
  };

  health_assessment: {
    vigor_score: number; // 1-10
    leaf_color: string;
    leaf_texture: string;
    growth_pattern: string;
    structural_integrity: string;
    stress_indicators: string[];
    disease_symptoms: string[];
    severity: 'yok' | 'düşük' | 'orta' | 'yüksek' | 'kritik' | 'unknown';
  };

  nutrient_status: {
    nitrogen: 'normal' | 'eksik' | 'fazla' | 'unknown';
    phosphorus: 'normal' | 'eksik' | 'fazla' | 'unknown';
    potassium: 'normal' | 'eksik' | 'fazla' | 'unknown';
    calcium: 'normal' | 'eksik' | 'fazla' | 'unknown';
    magnesium: 'normal' | 'eksik' | 'fazla' | 'unknown';
    sulfur: 'normal' | 'eksik' | 'fazla' | 'unknown';
    iron: 'normal' | 'eksik' | 'fazla' | 'unknown';
    zinc: 'normal' | 'eksik' | 'fazla' | 'unknown';
    manganese: 'normal' | 'eksik' | 'fazla' | 'unknown';
    boron: 'normal' | 'eksik' | 'fazla' | 'unknown';
    copper: 'normal' | 'eksik' | 'fazla' | 'unknown';
    molybdenum: 'normal' | 'eksik' | 'fazla' | 'unknown';
    chlorine: 'normal' | 'eksik' | 'fazla' | 'unknown';
    nickel: 'normal' | 'eksik' | 'fazla' | 'unknown';
    primary_deficiency: string;
    secondary_deficiencies: string[];
    severity: 'yok' | 'düşük' | 'orta' | 'yüksek' | 'kritik' | 'unknown';
  };

  pest_disease: {
    pests_detected: Array<{
      type: string;
      group: 'böcek' | 'akar' | 'nematod' | 'kemirgen' | 'diğer';
      severity: 'düşük' | 'orta' | 'yüksek';
      confidence: number;
      location: string;
    }>;
    diseases_detected: Array<{
      type: string;
      category: 'fungal' | 'bakteriyel' | 'viral' | 'fizyolojik';
      severity: 'düşük' | 'orta' | 'yüksek';
      affected_parts: string[];
      confidence: number;
    }>;
    damage_pattern: string;
    affected_area_percentage: number;
    spread_risk: 'yok' | 'düşük' | 'orta' | 'yüksek';
    primary_issue: string;
  };

  environmental_stress: {
    water_status: string;
    temperature_stress: string;
    light_stress: string;
    physical_damage: string;
    chemical_damage: string;
    physiological_disorders?: Array<{
      type: string;
      severity: 'düşük' | 'orta' | 'yüksek';
      notes: string;
    }>;
    soil_health_indicators?: {
      salinity: 'yok' | 'hafif' | 'orta' | 'şiddetli';
      pH_issue: 'asidik' | 'alkali' | 'optimal';
      organic_matter: 'düşük' | 'orta' | 'yüksek';
    };
    primary_stressor: string;
  };

  cross_factor_insights?: Array<{
    insight: string;
    confidence: number;
    affected_aspects: string[];
    impact_level: 'düşük' | 'orta' | 'yüksek';
  }>;

  risk_assessment?: {
    yield_loss_probability: 'düşük' | 'orta' | 'yüksek';
    timeline_to_worsen: string;
    spread_potential: string;
  };

  recommendations: {
    immediate: Array<{
      action: string;
      details: string;
      timeline: string;
      priority: 'kritik' | 'yüksek' | 'orta' | 'düşük';
    }>;
    short_term: Array<{
      action: string;
      details: string;
      timeline: string;
      priority: 'yüksek' | 'orta' | 'düşük';
    }>;
    preventive: Array<{
      action: string;
      details: string;
      timeline: string;
      priority: 'orta' | 'düşük';
    }>;
    monitoring: Array<{
      parameter: string;
      frequency: string;
      threshold: string;
    }>;
    resource_estimation?: {
      water_required_liters: string;
      fertilizer_cost_estimate_usd: string;
      labor_hours_estimate: string;
    };
    localized_recommendations?: {
      region: string;
      preferred_practices: string[];
      restricted_methods: string[];
    };
  };

  summary: {
    overall_health_score: number; // 1-10
    primary_concern: string;
    secondary_concerns: string[];
    critical_issues_count: number;
    confidence_level: number; // 0-100
    prognosis: 'mükemmel' | 'iyi' | 'orta' | 'zayıf' | 'kritik' | 'unknown';
    estimated_yield_impact: string;
  };

  confidence_notes?: Array<{
    aspect: string;
    confidence: number;
    reason: string;
  }>;

  farmer_friendly_summary?: string;

  // Processing metadata
  processing_metadata: {
    parse_success: boolean;
    processing_timestamp: string;
    processing_time_ms: number;
    ai_model: string;
    workflow_version: string;
    image_source: 'url' | 'base64';
    error_details?: string;
  };

  // Token usage
  token_usage?: {
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
  };

  // Error fields (if analysis fails)
  error?: boolean;
  error_message?: string;
  error_type?: string;
  raw_output_sample?: string;
}

/**
 * Provider-specific analysis message (for individual provider queues)
 */
export interface ProviderAnalysisMessage extends RawAnalysisMessage {
  provider: 'openai' | 'gemini' | 'anthropic';
  attemptNumber: number;
}

/**
 * Dead letter queue message
 */
export interface DeadLetterMessage {
  originalMessage: ProviderAnalysisMessage;
  error: string;
  failureTimestamp: string;
  attemptCount: number;
  lastProvider: string;
}
