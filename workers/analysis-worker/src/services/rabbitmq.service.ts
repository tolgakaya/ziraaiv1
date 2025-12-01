import amqp, { Channel, Connection } from 'amqplib';
import { RabbitMQConfig } from '../types/config';
import { ProviderAnalysisMessage, AnalysisResultMessage, DeadLetterMessage } from '../types/messages';
import { Logger } from 'pino';

/**
 * RabbitMQ service for consuming and publishing messages
 * Handles queue operations with automatic reconnection
 */
export class RabbitMQService {
  private connection: Connection | null = null;
  private channel: Channel | null = null;
  private config: RabbitMQConfig;
  private logger: Logger;
  private reconnecting: boolean = false;

  constructor(config: RabbitMQConfig, logger: Logger) {
    this.config = config;
    this.logger = logger;
  }

  /**
   * Connect to RabbitMQ and create channel
   */
  async connect(): Promise<void> {
    try {
      this.logger.info({ url: this.maskUrl(this.config.url) }, 'Connecting to RabbitMQ');

      const conn = await amqp.connect(this.config.url) as unknown as Connection;
      this.connection = conn;

      conn.on('error', (error) => {
        this.logger.error({ error }, 'RabbitMQ connection error');
        this.reconnect();
      });

      conn.on('close', () => {
        this.logger.warn('RabbitMQ connection closed');
        this.reconnect();
      });

      const ch = await (conn as any).createChannel();
      this.channel = ch;

      ch.on('error', (error: Error) => {
        this.logger.error({ error }, 'RabbitMQ channel error');
      });

      ch.on('close', () => {
        this.logger.warn('RabbitMQ channel closed');
      });

      // Set prefetch count for fair distribution
      await ch.prefetch(this.config.prefetchCount);

      // Assert queues exist
      await this.assertQueues();

      this.logger.info('RabbitMQ connected successfully');
    } catch (error) {
      this.logger.error({ error }, 'Failed to connect to RabbitMQ');
      throw error;
    }
  }

  /**
   * Assert that all required queues exist
   */
  private async assertQueues(): Promise<void> {
    if (!this.channel) {
      throw new Error('Channel not initialized');
    }

    const queues = Object.values(this.config.queues);

    for (const queue of queues) {
      await this.channel.assertQueue(queue, {
        durable: true, // Persist queue to disk
        arguments: {
          'x-message-ttl': 86400000, // 24 hours message TTL
        },
      });

      this.logger.debug({ queue }, 'Queue asserted');
    }
  }

  /**
   * Consume messages from a provider-specific queue
   *
   * @param queueName - Queue to consume from (e.g., 'openai-analysis-queue')
   * @param handler - Message handler function
   */
  async consumeProviderQueue(
    queueName: string,
    handler: (message: ProviderAnalysisMessage) => Promise<void>
  ): Promise<void> {
    if (!this.channel) {
      throw new Error('Channel not initialized');
    }

    this.logger.info({ queueName }, 'Starting consumer');

    await this.channel.consume(
      queueName,
      async (msg) => {
        if (!msg) {
          return;
        }

        try {
          const message: ProviderAnalysisMessage = JSON.parse(msg.content.toString());

          this.logger.debug({
            analysisId: message.analysis_id,
            provider: message.provider,
            queueName,
          }, 'Message received');

          // Process the message
          await handler(message);

          // Acknowledge message
          this.channel?.ack(msg);

          this.logger.debug({
            analysisId: message.analysis_id,
            queueName,
          }, 'Message acknowledged');
        } catch (error) {
          this.logger.error({
            error,
            queueName,
            messageId: msg.properties.messageId,
          }, 'Message processing failed');

          // Reject message and send to DLQ
          this.channel?.nack(msg, false, false);
        }
      },
      {
        noAck: false, // Manual acknowledgment
      }
    );
  }

  /**
   * Publish analysis result to results queue
   */
  async publishResult(result: AnalysisResultMessage): Promise<void> {
    if (!this.channel) {
      throw new Error('Channel not initialized');
    }

    try {
      const message = JSON.stringify(result);

      const published = this.channel.publish(
        '', // Default exchange
        this.config.queues.results,
        Buffer.from(message),
        {
          persistent: true, // Persist message to disk
          contentType: 'application/json',
          timestamp: Date.now(),
          messageId: `${result.analysis_id}-${Date.now()}`,
        }
      );

      if (!published) {
        this.logger.warn({
          analysisId: result.analysis_id,
          queue: this.config.queues.results,
        }, 'Message buffered (channel full)');
      } else {
        this.logger.debug({
          analysisId: result.analysis_id,
          queue: this.config.queues.results,
        }, 'Result published');
      }
    } catch (error) {
      this.logger.error({
        error,
        analysisId: result.analysis_id,
      }, 'Failed to publish result');
      throw error;
    }
  }

  /**
   * Publish failed message to dead letter queue
   */
  async publishToDeadLetterQueue(
    originalMessage: ProviderAnalysisMessage,
    error: string,
    attemptCount: number
  ): Promise<void> {
    if (!this.channel) {
      throw new Error('Channel not initialized');
    }

    try {
      const dlqMessage: DeadLetterMessage = {
        originalMessage,
        error,
        failureTimestamp: new Date().toISOString(),
        attemptCount,
        lastProvider: originalMessage.provider,
      };

      const message = JSON.stringify(dlqMessage);

      this.channel.publish(
        '',
        this.config.queues.dlq,
        Buffer.from(message),
        {
          persistent: true,
          contentType: 'application/json',
          timestamp: Date.now(),
          messageId: `dlq-${originalMessage.analysis_id}-${Date.now()}`,
        }
      );

      this.logger.warn({
        analysisId: originalMessage.analysis_id,
        provider: originalMessage.provider,
        attemptCount,
        error,
      }, 'Message sent to DLQ');
    } catch (error) {
      this.logger.error({
        error,
        analysisId: originalMessage.analysis_id,
      }, 'Failed to publish to DLQ');
    }
  }

  /**
   * Get queue depth (number of messages waiting)
   */
  async getQueueDepth(queueName: string): Promise<number> {
    if (!this.channel) {
      throw new Error('Channel not initialized');
    }

    try {
      const queueInfo = await this.channel.checkQueue(queueName);
      return queueInfo.messageCount;
    } catch (error) {
      this.logger.error({ error, queueName }, 'Failed to get queue depth');
      return 0;
    }
  }

  /**
   * Reconnect to RabbitMQ after connection loss
   */
  private async reconnect(): Promise<void> {
    if (this.reconnecting) {
      return;
    }

    this.reconnecting = true;

    this.logger.info('Attempting to reconnect to RabbitMQ');

    await new Promise(resolve => setTimeout(resolve, this.config.reconnectDelay));

    try {
      await this.connect();
      this.reconnecting = false;
    } catch (error) {
      this.logger.error({ error }, 'Reconnection failed, will retry');
      this.reconnecting = false;
      this.reconnect();
    }
  }

  /**
   * Close connection gracefully
   */
  async close(): Promise<void> {
    try {
      if (this.channel) {
        await this.channel.close();
      }

      if (this.connection) {
        await (this.connection as any).close();
      }

      this.logger.info('RabbitMQ connection closed gracefully');
    } catch (error) {
      this.logger.error({ error }, 'Error closing RabbitMQ connection');
    }
  }

  /**
   * Health check
   */
  async healthCheck(): Promise<boolean> {
    try {
      if (!this.channel) {
        return false;
      }

      // Try to check a queue
      await this.channel.checkQueue(this.config.queues.results);
      return true;
    } catch (error) {
      this.logger.error({ error }, 'RabbitMQ health check failed');
      return false;
    }
  }

  /**
   * Mask sensitive URL information for logging
   */
  private maskUrl(url: string): string {
    try {
      const parsed = new URL(url);
      if (parsed.password) {
        parsed.password = '***';
      }
      return parsed.toString();
    } catch {
      return '***';
    }
  }
}
