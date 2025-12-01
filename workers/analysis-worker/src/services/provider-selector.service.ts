/**
 * Provider Selection Service
 * 
 * Implements multiple strategies for selecting AI providers:
 * 1. FIXED: Always use specific provider (manual override)
 * 2. ROUND_ROBIN: Distribute load evenly across all providers
 * 3. COST_OPTIMIZED: Prefer cheaper providers (Gemini > OpenAI > Anthropic)
 * 4. QUALITY_FIRST: Prefer higher quality (Anthropic > OpenAI > Gemini)
 * 5. MESSAGE_BASED: Use provider specified in message (legacy n8n behavior)
 * 6. WEIGHTED: Custom weight distribution (e.g., 50% Gemini, 30% OpenAI, 20% Anthropic)
 */

import pino from 'pino';

export type ProviderName = 'openai' | 'gemini' | 'anthropic';

export type SelectionStrategy =
  | 'FIXED'           // Always use one provider
  | 'ROUND_ROBIN'     // Even distribution
  | 'COST_OPTIMIZED'  // Cheapest first
  | 'QUALITY_FIRST'   // Best quality first
  | 'MESSAGE_BASED'   // Use message.provider field
  | 'WEIGHTED';       // Custom weight distribution

export interface ProviderWeight {
  provider: ProviderName;
  weight: number; // 0-100
}

export interface ProviderSelectorConfig {
  strategy: SelectionStrategy;
  
  // For FIXED strategy
  fixedProvider?: ProviderName;
  
  // For WEIGHTED strategy
  weights?: ProviderWeight[];
  
  // Available providers
  availableProviders: ProviderName[];
}

/**
 * Provider metadata for cost and quality rankings
 */
interface ProviderMetadata {
  name: ProviderName;
  costPerMillion: number; // Average cost per 1M tokens (input + output weighted)
  qualityScore: number;   // Quality score 1-10 (10 = best)
  inputCostPerMillion: number;
  outputCostPerMillion: number;
}

export class ProviderSelectorService {
  private logger: pino.Logger;
  private config: ProviderSelectorConfig;
  private roundRobinIndex: number = 0;
  
  // Provider metadata - can be updated dynamically
  private providerMetadata: Map<ProviderName, ProviderMetadata> = new Map([
    ['gemini', {
      name: 'gemini',
      inputCostPerMillion: 0.075,
      outputCostPerMillion: 0.30,
      costPerMillion: 1.087,  // Estimated average for typical analysis
      qualityScore: 7,        // Good quality
    }],
    ['openai', {
      name: 'openai',
      inputCostPerMillion: 0.250,
      outputCostPerMillion: 2.00,
      costPerMillion: 5.125,  // Estimated average
      qualityScore: 8,        // Very good quality
    }],
    ['anthropic', {
      name: 'anthropic',
      inputCostPerMillion: 3.00,
      outputCostPerMillion: 15.00,
      costPerMillion: 48.0,   // Estimated average
      qualityScore: 10,       // Excellent quality
    }],
  ]);

  constructor(config: ProviderSelectorConfig, logger: pino.Logger) {
    this.config = config;
    this.logger = logger;
    
    this.validateConfiguration();
    
    this.logger.info({
      strategy: config.strategy,
      availableProviders: config.availableProviders,
      fixedProvider: config.fixedProvider,
      weights: config.weights,
    }, 'Provider selector initialized');
  }

  /**
   * Select provider based on configured strategy
   */
  selectProvider(messageProvider?: string): ProviderName {
    switch (this.config.strategy) {
      case 'FIXED':
        return this.selectFixed();
      
      case 'ROUND_ROBIN':
        return this.selectRoundRobin();
      
      case 'COST_OPTIMIZED':
        return this.selectCostOptimized();
      
      case 'QUALITY_FIRST':
        return this.selectQualityFirst();
      
      case 'MESSAGE_BASED':
        return this.selectMessageBased(messageProvider);
      
      case 'WEIGHTED':
        return this.selectWeighted();
      
      default:
        this.logger.warn({ strategy: this.config.strategy }, 'Unknown strategy, falling back to ROUND_ROBIN');
        return this.selectRoundRobin();
    }
  }

  /**
   * FIXED strategy: Always return the configured provider
   */
  private selectFixed(): ProviderName {
    if (!this.config.fixedProvider) {
      throw new Error('FIXED strategy requires fixedProvider configuration');
    }
    
    if (!this.config.availableProviders.includes(this.config.fixedProvider)) {
      this.logger.warn({
        fixedProvider: this.config.fixedProvider,
        availableProviders: this.config.availableProviders,
      }, 'Fixed provider not available, using first available');
      return this.config.availableProviders[0];
    }
    
    return this.config.fixedProvider;
  }

  /**
   * ROUND_ROBIN strategy: Distribute evenly across all providers
   */
  private selectRoundRobin(): ProviderName {
    const provider = this.config.availableProviders[this.roundRobinIndex];
    this.roundRobinIndex = (this.roundRobinIndex + 1) % this.config.availableProviders.length;
    
    this.logger.debug({
      provider,
      nextIndex: this.roundRobinIndex,
    }, 'Round-robin provider selected');
    
    return provider;
  }

  /**
   * COST_OPTIMIZED strategy: Prefer cheaper providers
   * Dynamically ranks based on actual cost metadata
   */
  private selectCostOptimized(): ProviderName {
    // Sort available providers by cost (cheapest first)
    const sortedByCost = this.config.availableProviders
      .map(provider => ({
        provider,
        cost: this.providerMetadata.get(provider)?.costPerMillion || Infinity,
      }))
      .sort((a, b) => a.cost - b.cost);
    
    const selected = sortedByCost[0].provider;
    
    this.logger.debug({
      provider: selected,
      cost: this.providerMetadata.get(selected)?.costPerMillion,
      ranking: sortedByCost.map(p => `${p.provider}:$${p.cost.toFixed(2)}`),
    }, 'Cost-optimized provider selected');
    
    return selected;
  }

  /**
   * QUALITY_FIRST strategy: Prefer higher quality providers
   * Dynamically ranks based on actual quality metadata
   */
  private selectQualityFirst(): ProviderName {
    // Sort available providers by quality (best first)
    const sortedByQuality = this.config.availableProviders
      .map(provider => ({
        provider,
        quality: this.providerMetadata.get(provider)?.qualityScore || 0,
      }))
      .sort((a, b) => b.quality - a.quality); // Descending order
    
    const selected = sortedByQuality[0].provider;
    
    this.logger.debug({
      provider: selected,
      qualityScore: this.providerMetadata.get(selected)?.qualityScore,
      ranking: sortedByQuality.map(p => `${p.provider}:${p.quality}`),
    }, 'Quality-first provider selected');
    
    return selected;
  }

  /**
   * MESSAGE_BASED strategy: Use provider specified in message
   * This is the legacy n8n behavior
   */
  private selectMessageBased(messageProvider?: string): ProviderName {
    if (!messageProvider) {
      this.logger.warn('MESSAGE_BASED strategy but no provider in message, falling back to first available');
      return this.config.availableProviders[0];
    }
    
    const provider = messageProvider.toLowerCase() as ProviderName;
    
    if (!this.config.availableProviders.includes(provider)) {
      this.logger.warn({
        requestedProvider: provider,
        availableProviders: this.config.availableProviders,
      }, 'Requested provider not available, using first available');
      return this.config.availableProviders[0];
    }
    
    return provider;
  }

  /**
   * WEIGHTED strategy: Select based on custom weight distribution
   * Example: 50% Gemini, 30% OpenAI, 20% Anthropic
   */
  private selectWeighted(): ProviderName {
    if (!this.config.weights || this.config.weights.length === 0) {
      throw new Error('WEIGHTED strategy requires weights configuration');
    }
    
    // Normalize weights to sum to 100
    const totalWeight = this.config.weights.reduce((sum, w) => sum + w.weight, 0);
    const normalizedWeights = this.config.weights.map(w => ({
      provider: w.provider,
      weight: (w.weight / totalWeight) * 100,
    }));
    
    // Random selection based on weights
    const random = Math.random() * 100;
    let cumulative = 0;
    
    for (const { provider, weight } of normalizedWeights) {
      cumulative += weight;
      if (random <= cumulative && this.config.availableProviders.includes(provider)) {
        this.logger.debug({ provider, random, cumulative }, 'Weighted provider selected');
        return provider;
      }
    }
    
    // Fallback (shouldn't reach here)
    return this.config.availableProviders[0];
  }

  /**
   * Validate configuration on initialization
   */
  private validateConfiguration(): void {
    if (!this.config.availableProviders || this.config.availableProviders.length === 0) {
      throw new Error('At least one provider must be available');
    }
    
    if (this.config.strategy === 'FIXED' && !this.config.fixedProvider) {
      throw new Error('FIXED strategy requires fixedProvider configuration');
    }
    
    if (this.config.strategy === 'WEIGHTED') {
      if (!this.config.weights || this.config.weights.length === 0) {
        throw new Error('WEIGHTED strategy requires weights configuration');
      }
      
      // Validate weights sum to reasonable range
      const totalWeight = this.config.weights.reduce((sum, w) => sum + w.weight, 0);
      if (totalWeight === 0) {
        throw new Error('Total weight cannot be zero');
      }
    }
  }

  /**
   * Get current strategy statistics
   */
  getStats() {
    return {
      strategy: this.config.strategy,
      availableProviders: this.config.availableProviders,
      roundRobinIndex: this.roundRobinIndex,
      fixedProvider: this.config.fixedProvider,
      weights: this.config.weights,
    };
  }

  /**
   * Update strategy at runtime (for dynamic reconfiguration)
   */
  updateStrategy(newConfig: Partial<ProviderSelectorConfig>): void {
    this.config = { ...this.config, ...newConfig };
    this.validateConfiguration();
    
    this.logger.info({
      newStrategy: this.config.strategy,
      newConfig,
    }, 'Provider selection strategy updated');
  }

  /**
   * Update provider metadata (cost and quality)
   * Useful for dynamic pricing updates or A/B testing results
   */
  updateProviderMetadata(provider: ProviderName, metadata: Partial<ProviderMetadata>): void {
    const current = this.providerMetadata.get(provider);
    if (!current) {
      this.logger.warn({ provider }, 'Attempted to update unknown provider metadata');
      return;
    }

    const updated = { ...current, ...metadata };
    this.providerMetadata.set(provider, updated);

    this.logger.info({
      provider,
      oldMetadata: current,
      newMetadata: updated,
    }, 'Provider metadata updated');
  }

  /**
   * Get current provider metadata
   */
  getProviderMetadata(provider: ProviderName): ProviderMetadata | undefined {
    return this.providerMetadata.get(provider);
  }

  /**
   * Get all provider metadata
   */
  getAllProviderMetadata(): Map<ProviderName, ProviderMetadata> {
    return new Map(this.providerMetadata);
  }

  /**
   * Update provider metadata from environment or config
   * Allows external configuration of costs and quality scores
   */
  loadProviderMetadataFromConfig(config: Record<string, Partial<ProviderMetadata>>): void {
    Object.entries(config).forEach(([providerName, metadata]) => {
      const provider = providerName as ProviderName;
      if (this.providerMetadata.has(provider)) {
        this.updateProviderMetadata(provider, metadata);
      }
    });

    this.logger.info({ config }, 'Provider metadata loaded from config');
  }
}
