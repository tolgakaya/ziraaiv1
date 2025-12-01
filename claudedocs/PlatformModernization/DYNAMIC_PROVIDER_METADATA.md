# Dynamic Provider Metadata Configuration

**Date**: 30 Kasım 2025  
**Version**: 1.0  
**Feature**: Dynamic cost and quality metadata

---

## Overview

Provider selection artık hardcoded cost/quality rankings yerine **dynamic metadata** kullanıyor. Bu sayede:

✅ Provider fiyatları güncellendiğinde kod değişikliği gerekmez  
✅ A/B test sonuçlarına göre quality scores güncellenebilir  
✅ Domain-specific quality metrics kullanılabilir  
✅ Runtime'da metadata güncellemeleri yapılabilir

---

## Default Metadata

### Built-in Defaults

```typescript
{
  'gemini': {
    name: 'gemini',
    inputCostPerMillion: 0.075,
    outputCostPerMillion: 0.30,
    costPerMillion: 1.087,  // Estimated average for typical analysis
    qualityScore: 7,        // Good quality (1-10 scale)
  },
  'openai': {
    name: 'openai',
    inputCostPerMillion: 0.250,
    outputCostPerMillion: 2.00,
    costPerMillion: 5.125,  // Estimated average
    qualityScore: 8,        // Very good quality
  },
  'anthropic': {
    name: 'anthropic',
    inputCostPerMillion: 3.00,
    outputCostPerMillion: 15.00,
    costPerMillion: 48.0,   // Estimated average
    qualityScore: 10,       // Excellent quality
  }
}
```

### How It Works

**COST_OPTIMIZED Strategy**:
```typescript
// Old (hardcoded):
COST_RANKING = ['gemini', 'openai', 'anthropic']

// New (dynamic):
sortedByCost = availableProviders
  .map(provider => ({
    provider,
    cost: metadata.get(provider).costPerMillion
  }))
  .sort((a, b) => a.cost - b.cost)
```

**QUALITY_FIRST Strategy**:
```typescript
// Old (hardcoded):
QUALITY_RANKING = ['anthropic', 'openai', 'gemini']

// New (dynamic):
sortedByQuality = availableProviders
  .map(provider => ({
    provider,
    quality: metadata.get(provider).qualityScore
  }))
  .sort((a, b) => b.quality - a.quality)
```

---

## Configuration Methods

### 1. Environment Variable (Preferred)

**Full Override**:
```env
PROVIDER_METADATA={"gemini":{"costPerMillion":1.0,"qualityScore":7},"openai":{"costPerMillion":5.0,"qualityScore":8},"anthropic":{"costPerMillion":48.0,"qualityScore":10}}
```

**Partial Update** (only specify changed values):
```env
# Just update Gemini quality after A/B testing
PROVIDER_METADATA={"gemini":{"qualityScore":8.5}}
```

**Cost Update Example** (provider price change):
```env
# OpenAI reduced prices by 10%
PROVIDER_METADATA={"openai":{"inputCostPerMillion":0.225,"outputCostPerMillion":1.80,"costPerMillion":4.6}}
```

### 2. Runtime API (Future Admin Panel)

```typescript
// Update single provider
providerSelector.updateProviderMetadata('gemini', {
  qualityScore: 8.5,
  costPerMillion: 0.95,
});

// Load from external config
providerSelector.loadProviderMetadataFromConfig({
  gemini: { qualityScore: 8.5 },
  openai: { costPerMillion: 4.5 },
});
```

### 3. Config File (Future Enhancement)

```json
// providers-metadata.json
{
  "gemini": {
    "inputCostPerMillion": 0.075,
    "outputCostPerMillion": 0.30,
    "costPerMillion": 1.087,
    "qualityScore": 7,
    "notes": "Cost-effective, good for bulk processing"
  },
  "openai": {
    "inputCostPerMillion": 0.250,
    "outputCostPerMillion": 2.00,
    "costPerMillion": 5.125,
    "qualityScore": 8,
    "notes": "Balanced quality and cost"
  },
  "anthropic": {
    "inputCostPerMillion": 3.00,
    "outputCostPerMillion": 15.00,
    "costPerMillion": 48.0,
    "qualityScore": 10,
    "notes": "Highest quality, premium pricing"
  }
}
```

---

## Use Cases

### Use Case 1: Provider Price Update

**Scenario**: Google Gemini announces 20% price reduction

**Before** (hardcoded):
```typescript
// Need code change and deployment
costPerMillion: 1.087 → 0.870
```

**After** (dynamic):
```env
# Just update environment variable, no deployment
PROVIDER_METADATA={"gemini":{"inputCostPerMillion":0.06,"outputCostPerMillion":0.24,"costPerMillion":0.870}}
```

**Benefit**: Zero downtime, no code deployment needed

---

### Use Case 2: A/B Testing Results

**Scenario**: Internal testing shows Gemini performs better than expected for specific crop types

**Configuration**:
```env
# Increase Gemini quality score from 7 to 8.5
PROVIDER_METADATA={"gemini":{"qualityScore":8.5}}
```

**Impact on QUALITY_FIRST Strategy**:
- Before: Anthropic (10) > OpenAI (8) > Gemini (7)
- After: Anthropic (10) > Gemini (8.5) > OpenAI (8)

**Result**: QUALITY_FIRST now prefers Gemini over OpenAI (cost savings!)

---

### Use Case 3: Domain-Specific Quality Metrics

**Scenario**: You track actual farmer satisfaction scores per provider

**Measured Results** (over 1 month):
- Gemini: 4.2/5 stars (84% satisfaction)
- OpenAI: 4.3/5 stars (86% satisfaction)
- Anthropic: 4.7/5 stars (94% satisfaction)

**Map to Quality Scores**:
```env
PROVIDER_METADATA={"gemini":{"qualityScore":8.4},"openai":{"qualityScore":8.6},"anthropic":{"qualityScore":9.4}}
```

**Benefit**: Quality scores reflect real-world performance, not theoretical capabilities

---

### Use Case 4: Cost Optimization During High Load

**Scenario**: Running low on monthly budget, need to reduce costs immediately

**Configuration**:
```env
# Temporarily boost Gemini quality score to encourage WEIGHTED strategy to use it more
PROVIDER_METADATA={"gemini":{"qualityScore":9}}
```

**Impact on WEIGHTED Strategy** (70% Gemini, 20% OpenAI, 10% Anthropic):
- Maintains quality perception
- Actual cost: $6,586/1M → Even lower with Gemini preference

---

### Use Case 5: Regional Pricing Differences

**Scenario**: Different costs in different regions

**EU Region**:
```env
PROVIDER_METADATA={"gemini":{"costPerMillion":1.2},"openai":{"costPerMillion":5.5},"anthropic":{"costPerMillion":52.0}}
```

**US Region**:
```env
PROVIDER_METADATA={"gemini":{"costPerMillion":1.0},"openai":{"costPerMillion":5.0},"anthropic":{"costPerMillion":48.0}}
```

**Benefit**: Accurate cost optimization per region

---

## Metadata Fields

### Required Fields
- `name: ProviderName` - Provider identifier (gemini, openai, anthropic)

### Cost Fields
- `inputCostPerMillion: number` - Cost per 1M input tokens (USD)
- `outputCostPerMillion: number` - Cost per 1M output tokens (USD)
- `costPerMillion: number` - Estimated average cost per 1M tokens (weighted)

### Quality Fields
- `qualityScore: number` - Quality score 1-10 (10 = best)

### How to Calculate `costPerMillion`

**Formula** (based on typical analysis token distribution):
```typescript
// Typical analysis: ~8,500 input tokens, ~1,500 output tokens
const typicalInputTokens = 8500;
const typicalOutputTokens = 1500;

const costPerMillion = (
  (typicalInputTokens / 1_000_000) * inputCostPerMillion +
  (typicalOutputTokens / 1_000_000) * outputCostPerMillion
) * (1_000_000 / (typicalInputTokens + typicalOutputTokens));
```

**Example for Gemini**:
```typescript
inputCost = (8500 / 1_000_000) * 0.075 = $0.0006375
outputCost = (1500 / 1_000_000) * 0.30 = $0.00045
totalCost = $0.0010875

costPerMillion = $0.0010875 * (1_000_000 / 10_000) = $1.0875
```

---

## Monitoring & Validation

### Get Current Metadata

```typescript
// Get single provider
const geminiMetadata = providerSelector.getProviderMetadata('gemini');
console.log(geminiMetadata);
// {
//   name: 'gemini',
//   inputCostPerMillion: 0.075,
//   outputCostPerMillion: 0.30,
//   costPerMillion: 1.087,
//   qualityScore: 7
// }

// Get all providers
const allMetadata = providerSelector.getAllProviderMetadata();
```

### Logging

**Provider Selection with Metadata**:
```json
{
  "provider": "gemini",
  "cost": 1.087,
  "ranking": ["gemini:$1.09", "openai:$5.13", "anthropic:$48.00"],
  "strategy": "COST_OPTIMIZED"
}
```

**Quality-First Selection**:
```json
{
  "provider": "anthropic",
  "qualityScore": 10,
  "ranking": ["anthropic:10", "openai:8", "gemini:7"],
  "strategy": "QUALITY_FIRST"
}
```

---

## Best Practices

### 1. Regular Updates
- Review provider pricing monthly
- Update `costPerMillion` when providers announce price changes
- Keep metadata synchronized with actual API pricing

### 2. Quality Scoring
- Base quality scores on measurable metrics (farmer satisfaction, accuracy tests)
- Don't rely solely on marketing claims
- Update quality scores based on A/B testing results

### 3. Documentation
- Document metadata changes in release notes
- Track metadata history for auditing
- Explain quality score calculation methodology

### 4. Testing
- Test metadata updates in staging before production
- Verify cost rankings after updates
- Ensure strategy behavior matches expectations

### 5. Fallback Values
- Always provide default metadata in code
- Handle missing/invalid metadata gracefully
- Log warnings for configuration errors

---

## API Methods

### `updateProviderMetadata(provider, metadata)`

Update metadata for a single provider:

```typescript
providerSelector.updateProviderMetadata('gemini', {
  costPerMillion: 0.95,
  qualityScore: 8.5,
});
```

### `loadProviderMetadataFromConfig(config)`

Bulk update from configuration object:

```typescript
providerSelector.loadProviderMetadataFromConfig({
  gemini: { qualityScore: 8.5 },
  openai: { costPerMillion: 4.5 },
  anthropic: { qualityScore: 9.5 },
});
```

### `getProviderMetadata(provider)`

Get metadata for a single provider:

```typescript
const metadata = providerSelector.getProviderMetadata('gemini');
```

### `getAllProviderMetadata()`

Get all provider metadata:

```typescript
const allMetadata = providerSelector.getAllProviderMetadata();
```

---

## Future Enhancements

### Admin Panel Integration
- UI for updating metadata
- Real-time preview of strategy changes
- Cost/quality visualization
- Historical metadata tracking

### Automatic Updates
- Fetch pricing from provider APIs
- Auto-update on price changes
- Alert on significant pricing differences

### Advanced Metrics
- Track actual costs vs estimates
- Calculate real quality scores from feedback
- Auto-adjust metadata based on performance

### Multi-Dimensional Quality
```typescript
interface AdvancedQualityMetrics {
  accuracy: number;      // 0-10
  speed: number;         // 0-10
  consistency: number;   // 0-10
  farmerSatisfaction: number;  // 0-10
  overall: number;       // Weighted average
}
```

---

## Migration Guide

### From Hardcoded to Dynamic

**Before** (code change required):
```typescript
// src/services/provider-selector.service.ts
private readonly COST_RANKING = ['gemini', 'openai', 'anthropic'];
```

**After** (environment variable):
```env
# No code change needed!
PROVIDER_METADATA={"gemini":{"costPerMillion":0.95}}
```

### Updating Existing Deployments

1. **Add PROVIDER_METADATA to environment**
2. **Restart worker** (picks up new metadata)
3. **Verify logs** (check metadata loaded successfully)
4. **Monitor selection** (ensure correct ranking)

---

**Last Updated**: 30 Kasım 2025  
**Status**: Production Ready ✅  
**Breaking Changes**: None (backward compatible)
