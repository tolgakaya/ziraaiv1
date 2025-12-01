/**
 * Configuration interfaces for worker services
 * These types define the structure of environment-based configuration
 */

/**
 * AI Provider configuration
 */
export interface ProviderConfig {
  name: 'openai' | 'gemini' | 'anthropic';
  apiKey: string;
  model: string;
  rateLimit: number; // requests per minute
  timeout: number; // milliseconds
  retryAttempts: number;
  retryDelayMs: number;
}

/**
 * RabbitMQ connection and queue configuration
 */
export interface RabbitMQConfig {
  url: string;
  queues: {
    raw: string; // raw-analysis-queue
    openai: string; // openai-analysis-queue
    gemini: string; // gemini-analysis-queue
    anthropic: string; // anthropic-analysis-queue
    results: string; // analysis-results-queue
    dlq: string; // analysis-dlq (dead letter queue)
  };
  prefetchCount: number; // concurrent message processing
  reconnectDelay: number; // milliseconds
}

/**
 * Redis configuration for rate limiting
 */
export interface RedisConfig {
  url: string;
  keyPrefix: string; // e.g., "ziraai:ratelimit:"
  ttl: number; // key expiration in seconds
}

/**
 * Worker configuration
 */
export interface WorkerConfig {
  workerId: string; // unique identifier (e.g., "openai-worker-001")
  provider: ProviderConfig;
  rabbitmq: RabbitMQConfig;
  redis: RedisConfig;
  concurrency: number; // max concurrent operations
  healthCheckInterval: number; // milliseconds
  logLevel: 'debug' | 'info' | 'warn' | 'error';
}

/**
 * Dispatcher configuration
 */
export interface DispatcherConfig {
  rabbitmq: RabbitMQConfig;
  redis: RedisConfig;
  providers: ProviderConfig[];
  routingStrategy: 'round-robin' | 'least-loaded' | 'weighted';
  circuitBreaker: CircuitBreakerConfig;
  logLevel: 'debug' | 'info' | 'warn' | 'error';
}

/**
 * Circuit breaker configuration for provider failover
 */
export interface CircuitBreakerConfig {
  failureThreshold: number; // consecutive failures before opening circuit
  successThreshold: number; // consecutive successes to close circuit
  timeout: number; // milliseconds before retry
  monitoringWindow: number; // milliseconds for error rate calculation
}

/**
 * Rate limiter state
 */
export interface RateLimitState {
  provider: string;
  currentCount: number;
  limit: number;
  windowStart: number; // timestamp
  windowDuration: number; // milliseconds
  allowed: boolean;
}

/**
 * Environment variable mapping for Railway deployment
 */
export interface EnvironmentVariables {
  // Worker identification
  WORKER_ID: string;
  PROVIDER: 'openai' | 'gemini' | 'anthropic';
  CONCURRENCY: string;

  // Provider API keys
  OPENAI_API_KEY?: string;
  GEMINI_API_KEY?: string;
  ANTHROPIC_API_KEY?: string;

  // Provider configuration
  PROVIDER_MODEL: string;
  RATE_LIMIT: string; // requests per minute
  TIMEOUT: string; // milliseconds

  // RabbitMQ
  RABBITMQ_URL: string;
  QUEUE_NAME: string; // provider-specific queue
  RESULT_QUEUE: string;
  DLQ_QUEUE: string;
  PREFETCH_COUNT: string;

  // Redis
  REDIS_URL: string;
  REDIS_KEY_PREFIX: string;
  REDIS_TTL: string;

  // Logging
  LOG_LEVEL: 'debug' | 'info' | 'warn' | 'error';
  NODE_ENV: 'development' | 'staging' | 'production';

  // Health checks
  HEALTH_CHECK_INTERVAL: string; // milliseconds
}
