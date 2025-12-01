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
    rateLimit: number;
    timeout: number;
    retryAttempts: number;
    retryDelayMs: number;
}
/**
 * RabbitMQ connection and queue configuration
 */
export interface RabbitMQConfig {
    url: string;
    queues: {
        raw: string;
        openai: string;
        gemini: string;
        anthropic: string;
        results: string;
        dlq: string;
    };
    prefetchCount: number;
    reconnectDelay: number;
}
/**
 * Redis configuration for rate limiting
 */
export interface RedisConfig {
    url: string;
    keyPrefix: string;
    ttl: number;
}
/**
 * Worker configuration
 */
export interface WorkerConfig {
    workerId: string;
    provider: ProviderConfig;
    rabbitmq: RabbitMQConfig;
    redis: RedisConfig;
    concurrency: number;
    healthCheckInterval: number;
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
    failureThreshold: number;
    successThreshold: number;
    timeout: number;
    monitoringWindow: number;
}
/**
 * Rate limiter state
 */
export interface RateLimitState {
    provider: string;
    currentCount: number;
    limit: number;
    windowStart: number;
    windowDuration: number;
    allowed: boolean;
}
/**
 * Environment variable mapping for Railway deployment
 */
export interface EnvironmentVariables {
    WORKER_ID: string;
    PROVIDER: 'openai' | 'gemini' | 'anthropic';
    CONCURRENCY: string;
    OPENAI_API_KEY?: string;
    GEMINI_API_KEY?: string;
    ANTHROPIC_API_KEY?: string;
    PROVIDER_MODEL: string;
    RATE_LIMIT: string;
    TIMEOUT: string;
    RABBITMQ_URL: string;
    QUEUE_NAME: string;
    RESULT_QUEUE: string;
    DLQ_QUEUE: string;
    PREFETCH_COUNT: string;
    REDIS_URL: string;
    REDIS_KEY_PREFIX: string;
    REDIS_TTL: string;
    LOG_LEVEL: 'debug' | 'info' | 'warn' | 'error';
    NODE_ENV: 'development' | 'staging' | 'production';
    HEALTH_CHECK_INTERVAL: string;
}
//# sourceMappingURL=config.d.ts.map