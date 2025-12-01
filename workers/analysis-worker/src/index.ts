import dotenv from 'dotenv';
import pino from 'pino';
import { OpenAIProvider } from './providers/openai.provider';
import { GeminiProvider } from './providers/gemini.provider';
import { AnthropicProvider } from './providers/anthropic.provider';
import { RabbitMQService } from './services/rabbitmq.service';
import { RateLimiterService } from './services/rate-limiter.service';
import { ProviderSelectorService, ProviderName } from './services/provider-selector.service';
import { WorkerConfig, ProviderConfig, RabbitMQConfig, RedisConfig, EnvironmentVariables, ProviderSelectionConfig } from './types/config';
import { ProviderAnalysisMessage, AnalysisResultMessage } from './types/messages';

// Provider interface for abstraction
interface AIProvider {
  analyzeImages(message: ProviderAnalysisMessage): Promise<AnalysisResultMessage>;
}

// Load environment variables
dotenv.config();

/**
 * Main worker entry point
 * Processes plant analysis messages from RabbitMQ using AI providers
 */
class AnalysisWorker {
  private config: WorkerConfig;
  private logger: pino.Logger;
  private providers: Map<string, AIProvider>;
  private providerSelector: ProviderSelectorService;
  private rabbitmq: RabbitMQService;
  private rateLimiter: RateLimiterService;
  private isShuttingDown: boolean = false;
  private processedCount: number = 0;
  private errorCount: number = 0;

  constructor() {
    // Initialize logger
    this.logger = pino({
      level: process.env.LOG_LEVEL || 'info',
      transport: process.env.NODE_ENV !== 'production' ? {
        target: 'pino-pretty',
        options: {
          colorize: true,
          translateTime: 'SYS:standard',
          ignore: 'pid,hostname',
        },
      } : undefined,
    });

    // Load configuration from environment
    this.config = this.loadConfiguration();

    // Initialize AI providers (multi-provider support)
    this.providers = this.initializeProviders();

    // Initialize provider selector
    this.providerSelector = new ProviderSelectorService(
      {
        strategy: this.config.providerSelection.strategy,
        fixedProvider: this.config.providerSelection.fixedProvider as ProviderName | undefined,
        weights: this.config.providerSelection.weights?.map(w => ({
          provider: w.provider as ProviderName,
          weight: w.weight,
        })),
        availableProviders: Array.from(this.providers.keys()) as ProviderName[],
      },
      this.logger
    );

    // Load provider metadata from environment if configured
    if (process.env.PROVIDER_METADATA) {
      try {
        const metadata = JSON.parse(process.env.PROVIDER_METADATA);
        this.providerSelector.loadProviderMetadataFromConfig(metadata);
        this.logger.info({ metadata }, 'Provider metadata loaded from environment');
      } catch (error: any) {
        this.logger.warn({ error: error.message }, 'Failed to parse PROVIDER_METADATA from environment');
      }
    }

    // Initialize services
    this.rabbitmq = new RabbitMQService(this.config.rabbitmq, this.logger);
    this.rateLimiter = new RateLimiterService(this.config.redis, this.logger);

    // Setup graceful shutdown handlers
    this.setupShutdownHandlers();
  }

  /**
   * Initialize all AI providers based on configuration
   */
  private initializeProviders(): Map<string, AIProvider> {
    const providers = new Map<string, AIProvider>();

    // OpenAI provider (always available)
    if (process.env.OPENAI_API_KEY) {
      providers.set('openai', new OpenAIProvider(this.config.provider, this.logger));
      this.logger.info('OpenAI provider initialized');
    }

    // Gemini provider (optional)
    if (process.env.GEMINI_API_KEY) {
      providers.set('gemini', new GeminiProvider(process.env.GEMINI_API_KEY, this.logger));
      this.logger.info('Gemini provider initialized');
    }

    // Anthropic provider (optional)
    if (process.env.ANTHROPIC_API_KEY) {
      providers.set('anthropic', new AnthropicProvider(process.env.ANTHROPIC_API_KEY, this.logger));
      this.logger.info('Anthropic provider initialized');
    }

    if (providers.size === 0) {
      throw new Error('No AI providers configured. Please set at least one API key.');
    }

    this.logger.info({ providers: Array.from(providers.keys()) }, 'AI providers initialized');
    return providers;
  }

  /**
   * Load worker configuration from environment variables
   */
  private loadConfiguration(): WorkerConfig {
    const env = process.env as unknown as EnvironmentVariables;

    // Validate required environment variables
    this.validateEnvironment(env);

    // Legacy provider config (kept for backward compatibility)
    // In multi-provider mode, this is used only for default rate limiting
    const providerConfig: ProviderConfig = {
      name: env.PROVIDER || 'openai', // Default to openai for legacy compatibility
      apiKey: env.OPENAI_API_KEY || '',
      model: env.PROVIDER_MODEL || 'gpt-4o-mini',
      rateLimit: parseInt(env.RATE_LIMIT || '350'),
      timeout: parseInt(env.TIMEOUT || '60000'),
      retryAttempts: 3,
      retryDelayMs: 1000,
    };

    const rabbitmqConfig: RabbitMQConfig = {
      url: env.RABBITMQ_URL,
      queues: {
        raw: 'raw-analysis-queue',
        openai: 'openai-analysis-queue',
        gemini: 'gemini-analysis-queue',
        anthropic: 'anthropic-analysis-queue',
        results: env.RESULT_QUEUE || 'analysis-results-queue',
        dlq: env.DLQ_QUEUE || 'analysis-dlq',
      },
      prefetchCount: parseInt(env.PREFETCH_COUNT || '10'),
      reconnectDelay: 5000,
    };

    const redisConfig: RedisConfig = {
      url: env.REDIS_URL,
      keyPrefix: env.REDIS_KEY_PREFIX || 'ziraai:ratelimit:',
      ttl: parseInt(env.REDIS_TTL || '120'),
    };

    // Provider selection configuration
    const providerSelectionConfig: ProviderSelectionConfig = {
      strategy: (env.PROVIDER_SELECTION_STRATEGY as any) || 'ROUND_ROBIN',
      fixedProvider: env.PROVIDER_FIXED,
      weights: env.PROVIDER_WEIGHTS ? JSON.parse(env.PROVIDER_WEIGHTS) : undefined,
    };

    return {
      workerId: env.WORKER_ID,
      provider: providerConfig,
      rabbitmq: rabbitmqConfig,
      redis: redisConfig,
      concurrency: parseInt(env.CONCURRENCY || '60'),
      healthCheckInterval: parseInt(env.HEALTH_CHECK_INTERVAL || '30000'),
      logLevel: env.LOG_LEVEL,
      providerSelection: providerSelectionConfig,
    };
  }

  /**
   * Validate required environment variables
   */
  private validateEnvironment(env: EnvironmentVariables): void {
    const required = [
      'WORKER_ID',
      'RABBITMQ_URL',
      'REDIS_URL',
    ];

    const missing = required.filter(key => !env[key as keyof EnvironmentVariables]);

    if (missing.length > 0) {
      throw new Error(`Missing required environment variables: ${missing.join(', ')}`);
    }

    // Validate at least one provider API key is configured
    const hasOpenAI = !!env.OPENAI_API_KEY;
    const hasGemini = !!env.GEMINI_API_KEY;
    const hasAnthropic = !!env.ANTHROPIC_API_KEY;

    if (!hasOpenAI && !hasGemini && !hasAnthropic) {
      throw new Error('At least one provider API key must be configured (OPENAI_API_KEY, GEMINI_API_KEY, or ANTHROPIC_API_KEY)');
    }

    // Log which providers are configured
    const configuredProviders = [];
    if (hasOpenAI) configuredProviders.push('OpenAI');
    if (hasGemini) configuredProviders.push('Gemini');
    if (hasAnthropic) configuredProviders.push('Anthropic');

    this.logger.info({ providers: configuredProviders }, 'Provider API keys validated');
  }

  /**
   * Start the worker
   */
  async start(): Promise<void> {
    const availableProviders = Array.from(this.providers.keys());
    
    this.logger.info({
      workerId: this.config.workerId,
      availableProviders,
      selectionStrategy: this.config.providerSelection.strategy,
      concurrency: this.config.concurrency,
    }, 'Starting multi-provider analysis worker');

    try {
      // Connect to RabbitMQ
      await this.rabbitmq.connect();

      // Start consuming from ALL provider-specific queues
      const queuesToConsume: string[] = [];
      
      if (this.providers.has('openai')) {
        queuesToConsume.push(this.config.rabbitmq.queues.openai);
      }
      if (this.providers.has('gemini')) {
        queuesToConsume.push(this.config.rabbitmq.queues.gemini);
      }
      if (this.providers.has('anthropic')) {
        queuesToConsume.push(this.config.rabbitmq.queues.anthropic);
      }

      // Start consuming from each queue
      for (const queueName of queuesToConsume) {
        await this.rabbitmq.consumeProviderQueue(queueName, async (message) => {
          await this.processMessage(message);
        });

        this.logger.info({ queueName }, 'Started consuming from queue');
      }

      // Start health check interval
      this.startHealthCheck();

      this.logger.info({
        workerId: this.config.workerId,
        queues: queuesToConsume,
        providerCount: availableProviders.length,
      }, 'Worker started successfully and consuming from all provider queues');
    } catch (error) {
      this.logger.fatal({ error }, 'Failed to start worker');
      process.exit(1);
    }
  }

  /**
   * Get the appropriate provider using configured selection strategy
   */
  private getProvider(messageProvider?: string): AIProvider {
    // Use provider selector to determine which provider to use
    const selectedProvider = this.providerSelector.selectProvider(messageProvider);
    
    const provider = this.providers.get(selectedProvider);

    if (!provider) {
      // Fallback to first available provider if selection failed
      const fallbackProvider = this.providers.values().next().value as AIProvider;
      this.logger.warn({
        selectedProvider,
        fallbackProvider: Array.from(this.providers.keys())[0],
        strategy: this.config.providerSelection.strategy,
      }, 'Selected provider not available, using fallback');
      return fallbackProvider;
    }

    this.logger.debug({
      selectedProvider,
      strategy: this.config.providerSelection.strategy,
      messageProvider,
    }, 'Provider selected');

    return provider;
  }

  /**
   * Process a single analysis message
   */
  private async processMessage(message: ProviderAnalysisMessage): Promise<void> {
    const startTime = Date.now();

    try {
      this.logger.info({
        analysisId: message.analysis_id,
        farmerId: message.farmer_id,
        messageProvider: message.provider,
        attemptNumber: message.attemptNumber,
      }, 'Processing analysis message');

      // Get the appropriate provider using configured strategy
      const provider = this.getProvider(message.provider);
      const selectedProvider = this.providerSelector.selectProvider(message.provider);

      // Check rate limit before processing (use selected provider for rate limiting)
      const rateLimitAllowed = await this.rateLimiter.waitForRateLimit(
        selectedProvider,
        this.config.provider.rateLimit,
        5000 // Wait up to 5 seconds
      );

      if (!rateLimitAllowed) {
        this.logger.warn({
          analysisId: message.analysis_id,
          selectedProvider,
        }, 'Rate limit exceeded, sending to DLQ');

        await this.rabbitmq.publishToDeadLetterQueue(
          message,
          'Rate limit exceeded after waiting',
          message.attemptNumber
        );

        this.errorCount++;
        return;
      }

      // Process with AI provider
      const result = await provider.analyzeImages(message);

      // Publish result to results queue
      await this.rabbitmq.publishResult(result);

      const processingTime = Date.now() - startTime;

      this.logger.info({
        analysisId: message.analysis_id,
        processingTimeMs: processingTime,
        totalTokens: result.token_usage?.summary.total_tokens,
        costUsd: result.token_usage?.summary.total_cost_usd,
      }, 'Analysis completed and result published');

      this.processedCount++;
    } catch (error) {
      const processingTime = Date.now() - startTime;
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';

      this.logger.error({
        error,
        analysisId: message.analysis_id,
        processingTimeMs: processingTime,
      }, 'Failed to process analysis message');

      // Send to DLQ if max retries exceeded
      if (message.attemptNumber >= 3) {
        await this.rabbitmq.publishToDeadLetterQueue(
          message,
          errorMessage,
          message.attemptNumber
        );
      }

      this.errorCount++;
    }
  }

  /**
   * Start periodic health check
   */
  private startHealthCheck(): void {
    setInterval(async () => {
      try {
        // Check first available provider for health
        const firstProvider = this.providers.values().next().value as AIProvider & { healthCheck?: () => Promise<boolean> };
        const providerHealthy = firstProvider.healthCheck ? await firstProvider.healthCheck() : true;
        const rabbitmqHealthy = await this.rabbitmq.healthCheck();
        const redisHealthy = await this.rateLimiter.healthCheck();

        const overall = providerHealthy && rabbitmqHealthy && redisHealthy;

        this.logger.info({
          workerId: this.config.workerId,
          provider: providerHealthy ? 'healthy' : 'unhealthy',
          rabbitmq: rabbitmqHealthy ? 'healthy' : 'unhealthy',
          redis: redisHealthy ? 'healthy' : 'unhealthy',
          overall: overall ? 'healthy' : 'degraded',
          processedCount: this.processedCount,
          errorCount: this.errorCount,
          errorRate: this.processedCount > 0 ? ((this.errorCount / this.processedCount) * 100).toFixed(2) + '%' : '0%',
        }, 'Health check');

        // Reset counters every hour
        if (this.processedCount > 10000) {
          this.processedCount = 0;
          this.errorCount = 0;
        }
      } catch (error) {
        this.logger.error({ error }, 'Health check failed');
      }
    }, this.config.healthCheckInterval);
  }

  /**
   * Setup graceful shutdown handlers
   */
  private setupShutdownHandlers(): void {
    const shutdown = async (signal: string) => {
      if (this.isShuttingDown) {
        return;
      }

      this.isShuttingDown = true;

      this.logger.info({ signal }, 'Shutdown signal received, closing gracefully');

      try {
        // Close connections
        await this.rabbitmq.close();
        await this.rateLimiter.close();

        this.logger.info({
          processedCount: this.processedCount,
          errorCount: this.errorCount,
        }, 'Worker shutdown complete');

        process.exit(0);
      } catch (error) {
        this.logger.error({ error }, 'Error during shutdown');
        process.exit(1);
      }
    };

    process.on('SIGTERM', () => shutdown('SIGTERM'));
    process.on('SIGINT', () => shutdown('SIGINT'));

    process.on('unhandledRejection', (reason, promise) => {
      this.logger.error({
        reason,
        promise,
      }, 'Unhandled Promise Rejection');
    });

    process.on('uncaughtException', (error) => {
      this.logger.fatal({ error }, 'Uncaught Exception');
      process.exit(1);
    });
  }
}

// Start the worker
const worker = new AnalysisWorker();
worker.start().catch((error) => {
  console.error('Fatal error starting worker:', error);
  process.exit(1);
});
