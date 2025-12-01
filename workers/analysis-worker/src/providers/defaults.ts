/**
 * Shared default values for all AI providers
 * Ensures consistency across OpenAI, Gemini, and Anthropic implementations
 */

export function getDefaultPlantIdentification() {
  return {
    species: 'Belirlenemedi',
    variety: 'bilinmiyor',
    growth_stage: 'unknown' as const,
    confidence: 0,
    identifying_features: [],
    visible_parts: [],
  };
}

export function getDefaultHealthAssessment() {
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

export function getDefaultNutrientStatus() {
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

export function getDefaultPestDisease() {
  return {
    pests_detected: [],
    diseases_detected: [],
    damage_pattern: 'Analiz edilemedi',
    affected_area_percentage: 0,
    spread_risk: 'yok' as const,
    primary_issue: 'yok',
  };
}

export function getDefaultEnvironmentalStress() {
  return {
    water_status: 'unknown',
    temperature_stress: 'unknown',
    light_stress: 'unknown',
    physical_damage: 'unknown',
    chemical_damage: 'unknown',
    primary_stressor: 'yok',
  };
}

export function getDefaultRecommendations() {
  return {
    immediate: [],
    short_term: [],
    preventive: [],
    monitoring: [],
  };
}

export function getDefaultSummary() {
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

export function getDefaultAnalysisMetadata(analysisId: string) {
  return {
    analysis_timestamp: new Date().toISOString(),
    analysis_id: analysisId,
    image_quality: 'orta' as const,
    visibility_conditions: 'Normal',
    limitations: ['Tam analiz yapılamadı'],
  };
}
