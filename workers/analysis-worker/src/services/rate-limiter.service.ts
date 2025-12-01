import Redis from 'ioredis';
import { RedisConfig, RateLimitState } from '../types/config';
import { Logger } from 'pino';

/**
 * Redis-based rate limiter with sliding window algorithm
 * Provides centralized rate limiting across multiple worker instances
 */
export class RateLimiterService {
  private redis: Redis;
  private config: RedisConfig;
  private logger: Logger;

  constructor(config: RedisConfig, logger: Logger) {
    this.config = config;
    this.logger = logger;
    this.redis = new Redis(config.url, {
      maxRetriesPerRequest: 3,
      retryStrategy: (times) => {
        const delay = Math.min(times * 50, 2000);
        return delay;
      },
    });

    this.redis.on('error', (error) => {
      this.logger.error({ error }, 'Redis connection error');
    });

    this.redis.on('connect', () => {
      this.logger.info('Redis connected successfully');
    });
  }

  /**
   * Check if a request is allowed under the rate limit
   * Uses sliding window algorithm for accurate rate limiting
   *
   * @param provider - Provider name (e.g., 'openai', 'gemini', 'anthropic')
   * @param limit - Maximum requests per minute
   * @returns true if request is allowed, false if rate limit exceeded
   */
  async checkRateLimit(provider: string, limit: number): Promise<boolean> {
    const key = `${this.config.keyPrefix}${provider}`;
    const now = Date.now();
    const windowStart = now - 60000; // 60 seconds window

    try {
      // Use Redis pipeline for atomic operations
      const pipeline = this.redis.pipeline();

      // Remove old entries outside the sliding window
      pipeline.zremrangebyscore(key, '-inf', windowStart);

      // Count current entries in the window
      pipeline.zcard(key);

      // Add current request timestamp
      pipeline.zadd(key, now, `${now}-${Math.random()}`);

      // Set expiration (cleanup)
      pipeline.expire(key, this.config.ttl);

      const results = await pipeline.exec();

      if (!results) {
        this.logger.error({ provider }, 'Redis pipeline execution failed');
        return false;
      }

      // Get count from zcard result (index 1)
      const currentCount = results[1][1] as number;

      const allowed = currentCount < limit;

      if (!allowed) {
        this.logger.warn({
          provider,
          currentCount,
          limit,
          key,
        }, 'Rate limit exceeded');
      }

      return allowed;
    } catch (error) {
      this.logger.error({
        error,
        provider,
        limit,
      }, 'Rate limit check failed');

      // Fail open: Allow request if Redis is unavailable
      // This prevents Redis outages from blocking all requests
      return true;
    }
  }

  /**
   * Get current rate limit state for a provider
   */
  async getRateLimitState(provider: string, limit: number): Promise<RateLimitState> {
    const key = `${this.config.keyPrefix}${provider}`;
    const now = Date.now();
    const windowStart = now - 60000;

    try {
      // Get current count in the sliding window
      await this.redis.zremrangebyscore(key, '-inf', windowStart);
      const currentCount = await this.redis.zcard(key);

      return {
        provider,
        currentCount,
        limit,
        windowStart,
        windowDuration: 60000,
        allowed: currentCount < limit,
      };
    } catch (error) {
      this.logger.error({ error, provider }, 'Failed to get rate limit state');

      return {
        provider,
        currentCount: 0,
        limit,
        windowStart,
        windowDuration: 60000,
        allowed: true,
      };
    }
  }

  /**
   * Reset rate limit for a provider (admin operation)
   */
  async resetRateLimit(provider: string): Promise<void> {
    const key = `${this.config.keyPrefix}${provider}`;

    try {
      await this.redis.del(key);
      this.logger.info({ provider }, 'Rate limit reset');
    } catch (error) {
      this.logger.error({ error, provider }, 'Failed to reset rate limit');
      throw error;
    }
  }

  /**
   * Get all provider rate limit states (monitoring)
   */
  async getAllRateLimitStates(providers: Array<{ name: string; limit: number }>): Promise<RateLimitState[]> {
    const states: RateLimitState[] = [];

    for (const provider of providers) {
      const state = await this.getRateLimitState(provider.name, provider.limit);
      states.push(state);
    }

    return states;
  }

  /**
   * Wait until rate limit allows a request
   * Uses exponential backoff with jitter
   *
   * @param provider - Provider name
   * @param limit - Rate limit per minute
   * @param maxWaitMs - Maximum wait time (default: 5000ms)
   * @returns true if allowed within maxWaitMs, false if timeout
   */
  async waitForRateLimit(
    provider: string,
    limit: number,
    maxWaitMs: number = 5000
  ): Promise<boolean> {
    const startTime = Date.now();
    let attempt = 0;

    while (Date.now() - startTime < maxWaitMs) {
      const allowed = await this.checkRateLimit(provider, limit);

      if (allowed) {
        return true;
      }

      // Exponential backoff with jitter
      const baseDelay = Math.min(100 * Math.pow(2, attempt), 1000);
      const jitter = Math.random() * 100;
      const delay = baseDelay + jitter;

      this.logger.debug({
        provider,
        attempt,
        delay,
        elapsed: Date.now() - startTime,
      }, 'Rate limit waiting');

      await new Promise(resolve => setTimeout(resolve, delay));
      attempt++;
    }

    this.logger.warn({
      provider,
      maxWaitMs,
      elapsed: Date.now() - startTime,
    }, 'Rate limit wait timeout');

    return false;
  }

  /**
   * Close Redis connection
   */
  async close(): Promise<void> {
    await this.redis.quit();
    this.logger.info('Redis connection closed');
  }

  /**
   * Health check
   */
  async healthCheck(): Promise<boolean> {
    try {
      const result = await this.redis.ping();
      return result === 'PONG';
    } catch (error) {
      this.logger.error({ error }, 'Redis health check failed');
      return false;
    }
  }
}
