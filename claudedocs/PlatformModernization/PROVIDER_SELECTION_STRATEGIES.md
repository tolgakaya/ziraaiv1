# Provider Selection Strategies

**Date**: 30 Kasım 2025  
**Version**: 1.0  
**Status**: Production Ready

---

## Overview

Analysis Worker artık 6 farklı provider selection stratejisi destekliyor. Bu, yük dağılımı, maliyet optimizasyonu ve kalite kontrolü için tam esneklik sağlıyor.

### Supported Strategies

1. **FIXED** - Belirli bir provider'ı zorunlu kullan
2. **ROUND_ROBIN** - Yükü eşit dağıt (default)
3. **COST_OPTIMIZED** - En ucuz provider'ı tercih et
4. **QUALITY_FIRST** - En kaliteli provider'ı tercih et
5. **MESSAGE_BASED** - message.provider field'ına göre seç (legacy n8n)
6. **WEIGHTED** - Özel ağırlıklı dağıtım

---

## Strategy Details

### 1. FIXED Strategy

**Use Case**: Sadece belirli bir provider kullanmak istediğinizde

**Configuration**:
```env
PROVIDER_SELECTION_STRATEGY=FIXED
PROVIDER_FIXED=gemini
GEMINI_API_KEY=AIza...
```

**Behavior**:
- Her zaman sadece `PROVIDER_FIXED` değerinde belirtilen provider kullanılır
- Diğer provider'lar ignore edilir (API keyleri bile gerekli değil)
- En basit ve öngörülebilir strateji

**Example Scenarios**:

**Scenario 1: Cost Savings - Gemini Only**
```env
PROVIDER_SELECTION_STRATEGY=FIXED
PROVIDER_FIXED=gemini
GEMINI_API_KEY=AIza...
```
- Result: %100 Gemini kullanımı
- Cost: $1,087/1M analysis
- Quality: Good (Flash 2.0 model)

**Scenario 2: Maximum Quality - Anthropic Only**
```env
PROVIDER_SELECTION_STRATEGY=FIXED
PROVIDER_FIXED=anthropic
ANTHROPIC_API_KEY=sk-ant-...
```
- Result: %100 Anthropic kullanımı
- Cost: $48,000/1M analysis
- Quality: Excellent (Claude 3.5 Sonnet)

**Scenario 3: Balanced - OpenAI Only**
```env
PROVIDER_SELECTION_STRATEGY=FIXED
PROVIDER_FIXED=openai
OPENAI_API_KEY=sk-...
```
- Result: %100 OpenAI kullanımı
- Cost: $5,125/1M analysis
- Quality: Very Good (GPT-4o-mini)

---

### 2. ROUND_ROBIN Strategy (DEFAULT)

**Use Case**: Yükü eşit dağıtmak ve tüm provider'ları kullanmak

**Configuration**:
```env
PROVIDER_SELECTION_STRATEGY=ROUND_ROBIN
OPENAI_API_KEY=sk-...
GEMINI_API_KEY=AIza...
ANTHROPIC_API_KEY=sk-ant-...
```

**Behavior**:
- Her mesaj sırayla farklı provider'a gönderilir
- 3 provider varsa: 1st → OpenAI, 2nd → Gemini, 3rd → Anthropic, 4th → OpenAI...
- En adil dağıtım stratejisi

**Distribution Examples**:

**3 Providers (OpenAI, Gemini, Anthropic)**:
```
Request 1 → OpenAI
Request 2 → Gemini
Request 3 → Anthropic
Request 4 → OpenAI
Request 5 → Gemini
Request 6 → Anthropic
...
```
- Result: 33% her provider
- Average Cost: ($5,125 + $1,087 + $48,000) / 3 = $18,071/1M

**2 Providers (OpenAI, Gemini)**:
```
Request 1 → OpenAI
Request 2 → Gemini
Request 3 → OpenAI
Request 4 → Gemini
...
```
- Result: 50% her provider
- Average Cost: ($5,125 + $1,087) / 2 = $3,106/1M

**1 Provider (Gemini)**:
```
Request 1 → Gemini
Request 2 → Gemini
Request 3 → Gemini
...
```
- Result: 100% Gemini
- Average Cost: $1,087/1M

---

### 3. COST_OPTIMIZED Strategy

**Use Case**: Maliyet minimizasyonu, en ucuz provider öncelikli

**Configuration**:
```env
PROVIDER_SELECTION_STRATEGY=COST_OPTIMIZED
OPENAI_API_KEY=sk-...
GEMINI_API_KEY=AIza...
ANTHROPIC_API_KEY=sk-ant-...
```

**Behavior**:
- Provider ranking: Gemini > OpenAI > Anthropic
- Önce Gemini kullanılır
- Gemini yoksa OpenAI
- OpenAI yoksa Anthropic

**Cost Ranking (Cheapest to Most Expensive)**:
1. **Gemini Flash 2.0**: $0.075/M input, $0.30/M output → $1,087/1M analyses
2. **OpenAI GPT-4o-mini**: $0.250/M input, $2.00/M output → $5,125/1M analyses
3. **Anthropic Claude 3.5**: $3.00/M input, $15.00/M output → $48,000/1M analyses

**Example Scenarios**:

**All 3 Providers Available**:
```
Request 1 → Gemini (cheapest)
Request 2 → Gemini
Request 3 → Gemini
Request 4 → Gemini
...
```
- Result: 100% Gemini
- Cost: $1,087/1M

**Only OpenAI and Anthropic Available**:
```
Request 1 → OpenAI (cheaper of the two)
Request 2 → OpenAI
Request 3 → OpenAI
...
```
- Result: 100% OpenAI
- Cost: $5,125/1M

**Only Anthropic Available**:
```
Request 1 → Anthropic (only option)
Request 2 → Anthropic
...
```
- Result: 100% Anthropic
- Cost: $48,000/1M

---

### 4. QUALITY_FIRST Strategy

**Use Case**: Kalite öncelikli, maliyet ikinci planda

**Configuration**:
```env
PROVIDER_SELECTION_STRATEGY=QUALITY_FIRST
OPENAI_API_KEY=sk-...
GEMINI_API_KEY=AIza...
ANTHROPIC_API_KEY=sk-ant-...
```

**Behavior**:
- Provider ranking: Anthropic > OpenAI > Gemini
- Önce Anthropic kullanılır
- Anthropic yoksa OpenAI
- OpenAI yoksa Gemini

**Quality Ranking (Best to Good)**:
1. **Anthropic Claude 3.5 Sonnet**: Excellent (most accurate, best reasoning)
2. **OpenAI GPT-4o-mini**: Very Good (balanced quality)
3. **Gemini Flash 2.0**: Good (fast and affordable)

**Example Scenarios**:

**All 3 Providers Available**:
```
Request 1 → Anthropic (best quality)
Request 2 → Anthropic
Request 3 → Anthropic
...
```
- Result: 100% Anthropic
- Cost: $48,000/1M
- Quality: Maximum

**Only OpenAI and Gemini Available**:
```
Request 1 → OpenAI (better of the two)
Request 2 → OpenAI
...
```
- Result: 100% OpenAI
- Cost: $5,125/1M
- Quality: Very Good

---

### 5. MESSAGE_BASED Strategy

**Use Case**: Legacy n8n compatibility, message field'ına göre provider seçimi

**Configuration**:
```env
PROVIDER_SELECTION_STRATEGY=MESSAGE_BASED
OPENAI_API_KEY=sk-...
GEMINI_API_KEY=AIza...
ANTHROPIC_API_KEY=sk-ant-...
```

**Behavior**:
- `message.provider` field'ındaki değer kullanılır
- Eğer belirtilen provider mevcut değilse, ilk available provider kullanılır
- n8n flow'dan gelen mesajlar için backward compatibility

**Example Messages**:

**Message 1** (provider: openai):
```json
{
  "analysis_id": "abc123",
  "provider": "openai",
  "image": "https://...",
  ...
}
```
- Result: OpenAI kullanılır

**Message 2** (provider: gemini):
```json
{
  "analysis_id": "def456",
  "provider": "gemini",
  "image": "https://...",
  ...
}
```
- Result: Gemini kullanılır

**Message 3** (provider: anthropic):
```json
{
  "analysis_id": "ghi789",
  "provider": "anthropic",
  "image": "https://...",
  ...
}
```
- Result: Anthropic kullanılır

**Message 4** (provider field yok veya invalid):
```json
{
  "analysis_id": "jkl012",
  "provider": "unknown",
  "image": "https://...",
  ...
}
```
- Result: First available provider (fallback)

---

### 6. WEIGHTED Strategy

**Use Case**: Özel ağırlıklı dağıtım, fine-grained control

**Configuration**:
```env
PROVIDER_SELECTION_STRATEGY=WEIGHTED
PROVIDER_WEIGHTS=[{"provider":"gemini","weight":50},{"provider":"openai","weight":30},{"provider":"anthropic","weight":20}]
OPENAI_API_KEY=sk-...
GEMINI_API_KEY=AIza...
ANTHROPIC_API_KEY=sk-ant-...
```

**Behavior**:
- Provider weights normalize edilir (toplam 100)
- Random selection ağırlıklara göre yapılır
- Daha flexible cost/quality balance

**Example Configurations**:

**Scenario 1: Cost-Focused with Quality Sampling**
```json
[
  {"provider":"gemini","weight":70},
  {"provider":"openai","weight":20},
  {"provider":"anthropic","weight":10}
]
```
- Result: 70% Gemini, 20% OpenAI, 10% Anthropic
- Average Cost: (0.7 × $1,087) + (0.2 × $5,125) + (0.1 × $48,000) = $6,586/1M
- Strategy: Mostly cheap, some quality checks

**Scenario 2: Balanced Mix**
```json
[
  {"provider":"gemini","weight":50},
  {"provider":"openai","weight":30},
  {"provider":"anthropic","weight":20}
]
```
- Result: 50% Gemini, 30% OpenAI, 20% Anthropic
- Average Cost: (0.5 × $1,087) + (0.3 × $5,125) + (0.2 × $48,000) = $11,681/1M
- Strategy: Cost savings with quality validation

**Scenario 3: Quality-Focused with Cost Sampling**
```json
[
  {"provider":"anthropic","weight":60},
  {"provider":"openai","weight":30},
  {"provider":"gemini","weight":10}
]
```
- Result: 60% Anthropic, 30% OpenAI, 10% Anthropic
- Average Cost: (0.6 × $48,000) + (0.3 × $5,125) + (0.1 × $1,087) = $30,446/1M
- Strategy: High quality with some cost optimization

**Scenario 4: Two-Provider Mix**
```json
[
  {"provider":"gemini","weight":80},
  {"provider":"openai","weight":20}
]
```
- Result: 80% Gemini, 20% OpenAI
- Average Cost: (0.8 × $1,087) + (0.2 × $5,125) = $1,895/1M
- Strategy: Mostly cheap, OpenAI for validation

---

## Strategy Comparison

| Strategy | Cost Control | Quality Control | Flexibility | Use Case |
|----------|--------------|-----------------|-------------|----------|
| FIXED | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐ | Single provider, predictable |
| ROUND_ROBIN | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | Even distribution, failover |
| COST_OPTIMIZED | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐ | Cost minimization |
| QUALITY_FIRST | ⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ | Quality maximization |
| MESSAGE_BASED | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | n8n compatibility |
| WEIGHTED | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | Custom balance |

---

## Cost Analysis by Strategy

### For 1,000,000 Daily Analyses

**FIXED - Gemini Only**:
- Daily Cost: $1,087
- Monthly Cost: $32,610
- Best For: Maximum cost savings

**FIXED - OpenAI Only**:
- Daily Cost: $5,125
- Monthly Cost: $153,750
- Best For: Balanced quality/cost

**FIXED - Anthropic Only**:
- Daily Cost: $48,000
- Monthly Cost: $1,440,000
- Best For: Maximum quality (expensive!)

**ROUND_ROBIN - All 3 Providers**:
- Daily Cost: $18,071 (average)
- Monthly Cost: $542,130
- Best For: Provider diversity, failover

**COST_OPTIMIZED - All 3 Available**:
- Daily Cost: $1,087 (100% Gemini)
- Monthly Cost: $32,610
- Best For: Cost minimization with failover option

**QUALITY_FIRST - All 3 Available**:
- Daily Cost: $48,000 (100% Anthropic)
- Monthly Cost: $1,440,000
- Best For: Quality maximization (very expensive)

**WEIGHTED - 70% Gemini, 20% OpenAI, 10% Anthropic**:
- Daily Cost: $6,586
- Monthly Cost: $197,580
- Best For: Cost savings with quality checks

---

## Implementation Details

### ProviderSelectorService

**Location**: `workers/analysis-worker/src/services/provider-selector.service.ts`

**Key Methods**:
```typescript
selectProvider(messageProvider?: string): ProviderName
updateStrategy(newConfig: Partial<ProviderSelectorConfig>): void
getStats(): object
```

**Usage in Worker**:
```typescript
// Initialize
this.providerSelector = new ProviderSelectorService(config, logger);

// Select provider
const selectedProvider = this.providerSelector.selectProvider(message.provider);
const provider = this.providers.get(selectedProvider);

// Use provider
const result = await provider.analyzeImages(message);
```

---

## Runtime Strategy Updates

Strategy'yi runtime'da değiştirebilirsiniz (gelecekte admin panel üzerinden):

```typescript
// Change to FIXED strategy
this.providerSelector.updateStrategy({
  strategy: 'FIXED',
  fixedProvider: 'gemini',
});

// Change to WEIGHTED strategy
this.providerSelector.updateStrategy({
  strategy: 'WEIGHTED',
  weights: [
    { provider: 'gemini', weight: 60 },
    { provider: 'openai', weight: 40 },
  ],
});
```

---

## Monitoring & Stats

**Provider Selection Stats**:
```typescript
const stats = this.providerSelector.getStats();
// {
//   strategy: 'WEIGHTED',
//   availableProviders: ['openai', 'gemini', 'anthropic'],
//   roundRobinIndex: 0,
//   fixedProvider: undefined,
//   weights: [{ provider: 'gemini', weight: 50 }, ...]
// }
```

**Health Check Logging**:
```json
{
  "workerId": "analysis-worker-001",
  "strategy": "WEIGHTED",
  "providerStats": {
    "openai": { "count": 300, "percentage": 30 },
    "gemini": { "count": 500, "percentage": 50 },
    "anthropic": { "count": 200, "percentage": 20 }
  },
  "processedCount": 1000,
  "errorCount": 5,
  "errorRate": "0.5%"
}
```

---

## Railway Deployment Recommendations

### Staging Environment
```env
# Test with cheap provider
PROVIDER_SELECTION_STRATEGY=FIXED
PROVIDER_FIXED=gemini
GEMINI_API_KEY=...
```

### Production Environment (Cost-Optimized)
```env
# Mostly Gemini, some OpenAI for validation
PROVIDER_SELECTION_STRATEGY=WEIGHTED
PROVIDER_WEIGHTS=[{"provider":"gemini","weight":80},{"provider":"openai","weight":20}]
GEMINI_API_KEY=...
OPENAI_API_KEY=...
```

### Production Environment (Quality-Focused)
```env
# Balanced quality with cost awareness
PROVIDER_SELECTION_STRATEGY=WEIGHTED
PROVIDER_WEIGHTS=[{"provider":"openai","weight":60},{"provider":"gemini","weight":30},{"provider":"anthropic","weight":10}]
OPENAI_API_KEY=...
GEMINI_API_KEY=...
ANTHROPIC_API_KEY=...
```

### Production Environment (Failover-Ready)
```env
# Even distribution for reliability
PROVIDER_SELECTION_STRATEGY=ROUND_ROBIN
OPENAI_API_KEY=...
GEMINI_API_KEY=...
ANTHROPIC_API_KEY=...
```

---

## Best Practices

### 1. Start Simple
- Begin with `FIXED` strategy (Gemini for cost or OpenAI for balance)
- Monitor quality and cost
- Adjust as needed

### 2. Use WEIGHTED for Fine Control
- Start with cost-heavy weights
- Gradually increase quality provider weights based on needs
- Monitor analysis quality metrics

### 3. Keep Failover Options
- Even with `FIXED`, keep other API keys configured
- Easy to switch if primary provider has issues
- Just change `PROVIDER_FIXED` value

### 4. Monitor Costs
- Track actual API usage vs strategy expectations
- Adjust weights based on real-world results
- Consider seasonal or time-based strategy changes

### 5. Quality Validation
- Sample analyses from different providers
- Compare results periodically
- Adjust strategy if quality issues detected

---

## Future Enhancements

### Admin Panel Integration (Phase 3)
- Real-time strategy changes via UI
- Provider performance dashboards
- Cost tracking and budgets
- Auto-strategy based on budget limits

### Dynamic Strategy Switching
- Time-based: cheap provider during low-priority hours
- Load-based: distribute more when under heavy load
- Budget-based: switch to cheaper when monthly budget limit approaching

### Provider Health Monitoring
- Automatic failover on provider errors
- Circuit breaker per provider
- Quality scoring per provider
- Auto-disable low-performing providers

---

**Last Updated**: 30 Kasım 2025  
**Version**: 1.0  
**Status**: Production Ready ✅
