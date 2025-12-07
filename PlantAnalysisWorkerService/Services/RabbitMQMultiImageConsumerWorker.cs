using Business.Services.MessageQueue;
using Core.Configuration;
using Entities.Dtos;
using Hangfire;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PlantAnalysisWorkerService.Jobs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;

namespace PlantAnalysisWorkerService.Services
{
    public class RabbitMQMultiImageConsumerWorker : BackgroundService
    {
        private readonly ILogger<RabbitMQMultiImageConsumerWorker> _logger;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMQMultiImageConsumerWorker(
            ILogger<RabbitMQMultiImageConsumerWorker> logger,
            IOptions<RabbitMQOptions> rabbitMQOptions,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _rabbitMQOptions = rabbitMQOptions.Value;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var startupStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("[RABBITMQ_MULTI_IMAGE_WORKER_START] Multi-Image Plant Analysis RabbitMQ Consumer Worker starting - QueueName: {QueueName}, ConnectionString: {ConnectionString}",
                _rabbitMQOptions.Queues.PlantAnalysisMultiImageResult, _rabbitMQOptions.ConnectionString);

            try
            {
                await InitializeRabbitMQAsync();
                startupStopwatch.Stop();

                _logger.LogInformation("[RABBITMQ_MULTI_IMAGE_WORKER_INITIALIZED] Multi-Image Worker initialized successfully - InitializationTime: {InitializationTime}ms",
                    startupStopwatch.ElapsedMilliseconds);

                await StartConsumingAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                startupStopwatch.Stop();
                _logger.LogInformation("[RABBITMQ_MULTI_IMAGE_WORKER_CANCELLED] Multi-Image Plant Analysis RabbitMQ Consumer Worker cancellation requested - UpTime: {UpTime}ms",
                    startupStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                startupStopwatch.Stop();
                _logger.LogError(ex, "[RABBITMQ_MULTI_IMAGE_WORKER_FATAL_ERROR] Fatal error in Multi-Image Plant Analysis RabbitMQ Consumer Worker - UpTime: {UpTime}ms, ExceptionType: {ExceptionType}",
                    startupStopwatch.ElapsedMilliseconds, ex.GetType().Name);
                throw;
            }
            finally
            {
                startupStopwatch.Stop();
                await CleanupAsync();
                _logger.LogInformation("[RABBITMQ_MULTI_IMAGE_WORKER_STOP] Multi-Image Plant Analysis RabbitMQ Consumer Worker stopping - TotalUpTime: {TotalUpTime}ms",
                    startupStopwatch.ElapsedMilliseconds);
            }
        }

        private async Task InitializeRabbitMQAsync()
        {
            var initStopwatch = Stopwatch.StartNew();

            _logger.LogInformation("[RABBITMQ_MULTI_IMAGE_INIT_START] Initializing Multi-Image RabbitMQ connection - ConnectionString: {ConnectionString}, Heartbeat: {Heartbeat}s, RecoveryInterval: {RecoveryInterval}s",
                _rabbitMQOptions.ConnectionString, _rabbitMQOptions.ConnectionSettings.RequestedHeartbeat, _rabbitMQOptions.ConnectionSettings.NetworkRecoveryInterval);

            try
            {
                var factory = new ConnectionFactory();
                factory.Uri = new Uri(_rabbitMQOptions.ConnectionString);
                factory.AutomaticRecoveryEnabled = true;
                factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(_rabbitMQOptions.ConnectionSettings.NetworkRecoveryInterval);
                factory.RequestedHeartbeat = TimeSpan.FromSeconds(_rabbitMQOptions.ConnectionSettings.RequestedHeartbeat);

                var connectionStart = Stopwatch.StartNew();
                _connection = await factory.CreateConnectionAsync();
                connectionStart.Stop();

                _logger.LogInformation("[RABBITMQ_MULTI_IMAGE_CONNECTION_SUCCESS] Multi-Image RabbitMQ connection established - ConnectionTime: {ConnectionTime}ms",
                    connectionStart.ElapsedMilliseconds);

                var channelStart = Stopwatch.StartNew();
                _channel = await _connection.CreateChannelAsync();
                channelStart.Stop();

                _logger.LogInformation("[RABBITMQ_MULTI_IMAGE_CHANNEL_SUCCESS] Multi-Image RabbitMQ channel created - ChannelTime: {ChannelTime}ms",
                    channelStart.ElapsedMilliseconds);

                // Declare queue (make sure it exists)
                var queueDeclareStart = Stopwatch.StartNew();

                // No TTL for multi-image queue - matches production configuration
                // (Unlike plant-analysis-results which has TTL, this queue was created without TTL)
                await _channel.QueueDeclareAsync(
                    queue: _rabbitMQOptions.Queues.PlantAnalysisMultiImageResult,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                queueDeclareStart.Stop();

                initStopwatch.Stop();
                _logger.LogInformation("[RABBITMQ_MULTI_IMAGE_INIT_SUCCESS] Multi-Image RabbitMQ initialized successfully - QueueName: {QueueName}, QueueDeclareTime: {QueueDeclareTime}ms, TotalInitTime: {TotalInitTime}ms",
                    _rabbitMQOptions.Queues.PlantAnalysisMultiImageResult, queueDeclareStart.ElapsedMilliseconds, initStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                initStopwatch.Stop();
                _logger.LogError(ex, "[RABBITMQ_MULTI_IMAGE_INIT_ERROR] Failed to initialize Multi-Image RabbitMQ connection - InitAttemptTime: {InitAttemptTime}ms, ExceptionType: {ExceptionType}, Message: {ErrorMessage}",
                    initStopwatch.ElapsedMilliseconds, ex.GetType().Name, ex.Message);
                throw;
            }
        }

        private async Task StartConsumingAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var messageStopwatch = Stopwatch.StartNew();
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var correlationId = ea.BasicProperties?.CorrelationId ?? Guid.NewGuid().ToString("N")[..8];
                var deliveryTag = ea.DeliveryTag;

                _logger.LogInformation("[RABBITMQ_MULTI_IMAGE_MESSAGE_RECEIVED] Multi-Image RabbitMQ message received - Size: {MessageSize} bytes, CorrelationId: {CorrelationId}, DeliveryTag: {DeliveryTag}",
                    body.Length, correlationId, deliveryTag);

                _logger.LogDebug("[RABBITMQ_MULTI_IMAGE_MESSAGE_CONTENT] Multi-Image message content preview - CorrelationId: {CorrelationId}, Content: {MessageContent}",
                    correlationId, message.Substring(0, Math.Min(500, message.Length)) + (message.Length > 500 ? "..." : ""));

                try
                {
                    var deserializationStart = Stopwatch.StartNew();
                    // Deserialize message (same DTO as single-image, supports multi-image fields)
                    var analysisResult = JsonConvert.DeserializeObject<PlantAnalysisAsyncResponseDto>(message);
                    deserializationStart.Stop();

                    if (analysisResult == null)
                    {
                        messageStopwatch.Stop();
                        _logger.LogWarning("[RABBITMQ_MULTI_IMAGE_DESERIALIZATION_FAILED] Failed to deserialize multi-image message - CorrelationId: {CorrelationId}, DeliveryTag: {DeliveryTag}, ProcessingTime: {ProcessingTime}ms, MessageLength: {MessageLength}",
                            correlationId, deliveryTag, messageStopwatch.ElapsedMilliseconds, message.Length);

                        await _channel.BasicNackAsync(deliveryTag, false, false);
                        return;
                    }

                    _logger.LogInformation("[RABBITMQ_MULTI_IMAGE_DESERIALIZATION_SUCCESS] Multi-Image message deserialized successfully - CorrelationId: {CorrelationId}, DeserializationTime: {DeserializationTime}ms, AnalysisId: {AnalysisId}",
                        correlationId, deserializationStart.ElapsedMilliseconds, analysisResult.AnalysisId);

                    // Enqueue Hangfire job for processing (same job service handles both single and multi-image)
                    var jobEnqueueStart = Stopwatch.StartNew();
                    var jobId = BackgroundJob.Enqueue<IPlantAnalysisJobService>(
                        service => service.ProcessPlantAnalysisResultAsync(analysisResult, correlationId));
                    jobEnqueueStart.Stop();

                    _logger.LogInformation("[RABBITMQ_MULTI_IMAGE_JOB_ENQUEUED] Multi-Image Hangfire job enqueued successfully - CorrelationId: {CorrelationId}, JobId: {JobId}, AnalysisId: {AnalysisId}, EnqueueTime: {EnqueueTime}ms",
                        correlationId, jobId, analysisResult.AnalysisId, jobEnqueueStart.ElapsedMilliseconds);

                    // Acknowledge message
                    await _channel.BasicAckAsync(deliveryTag, false);

                    messageStopwatch.Stop();
                    _logger.LogInformation("[RABBITMQ_MULTI_IMAGE_MESSAGE_PROCESSED] Multi-Image message processed successfully - CorrelationId: {CorrelationId}, TotalProcessingTime: {TotalProcessingTime}ms, DeliveryTag: {DeliveryTag}",
                        correlationId, messageStopwatch.ElapsedMilliseconds, deliveryTag);
                }
                catch (JsonException ex)
                {
                    messageStopwatch.Stop();
                    _logger.LogError(ex, "[RABBITMQ_MULTI_IMAGE_JSON_ERROR] Multi-Image JSON deserialization error - CorrelationId: {CorrelationId}, DeliveryTag: {DeliveryTag}, ProcessingTime: {ProcessingTime}ms, MessageLength: {MessageLength}, ExceptionType: {ExceptionType}",
                        correlationId, deliveryTag, messageStopwatch.ElapsedMilliseconds, message.Length, ex.GetType().Name);

                    await _channel.BasicNackAsync(deliveryTag, false, false);
                }
                catch (Exception ex)
                {
                    messageStopwatch.Stop();
                    _logger.LogError(ex, "[RABBITMQ_MULTI_IMAGE_PROCESSING_ERROR] Error processing multi-image message - CorrelationId: {CorrelationId}, DeliveryTag: {DeliveryTag}, ProcessingTime: {ProcessingTime}ms, ExceptionType: {ExceptionType}, Message: {ErrorMessage}",
                        correlationId, deliveryTag, messageStopwatch.ElapsedMilliseconds, ex.GetType().Name, ex.Message);

                    // Retry logic - nack with requeue for transient errors
                    var shouldRequeue = ShouldRetryMessage(ea, ex);

                    _logger.LogInformation("[RABBITMQ_MULTI_IMAGE_RETRY_DECISION] Multi-Image message retry decision - CorrelationId: {CorrelationId}, ShouldRequeue: {ShouldRequeue}, ExceptionType: {ExceptionType}",
                        correlationId, shouldRequeue, ex.GetType().Name);

                    await _channel.BasicNackAsync(deliveryTag, false, shouldRequeue);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _rabbitMQOptions.Queues.PlantAnalysisMultiImageResult,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("Started consuming multi-image messages from RabbitMQ");

            // Keep alive until cancellation
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private bool ShouldRetryMessage(BasicDeliverEventArgs ea, Exception ex)
        {
            // Simple retry logic - you can enhance this
            // For now, retry for most exceptions except JSON errors
            return !(ex is JsonException);
        }

        private async Task CleanupAsync()
        {
            try
            {
                if (_channel?.IsOpen == true)
                {
                    await _channel.CloseAsync();
                    _channel?.Dispose();
                }

                if (_connection?.IsOpen == true)
                {
                    await _connection.CloseAsync();
                    _connection?.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during multi-image consumer cleanup");
            }
        }

        public override void Dispose()
        {
            CleanupAsync().GetAwaiter().GetResult();
            base.Dispose();
        }
    }
}
