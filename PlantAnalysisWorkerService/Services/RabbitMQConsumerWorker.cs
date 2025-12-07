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
    public class RabbitMQConsumerWorker : BackgroundService
    {
        private readonly ILogger<RabbitMQConsumerWorker> _logger;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMQConsumerWorker(
            ILogger<RabbitMQConsumerWorker> logger,
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
            _logger.LogInformation("[RABBITMQ_WORKER_START] Plant Analysis RabbitMQ Consumer Worker starting - QueueName: {QueueName}, ConnectionString: {ConnectionString}", 
                _rabbitMQOptions.Queues.PlantAnalysisResult, _rabbitMQOptions.ConnectionString);

            try
            {
                await InitializeRabbitMQAsync();
                startupStopwatch.Stop();
                
                _logger.LogInformation("[RABBITMQ_WORKER_INITIALIZED] Worker initialized successfully - InitializationTime: {InitializationTime}ms", 
                    startupStopwatch.ElapsedMilliseconds);
                
                await StartConsumingAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                startupStopwatch.Stop();
                _logger.LogInformation("[RABBITMQ_WORKER_CANCELLED] Plant Analysis RabbitMQ Consumer Worker cancellation requested - UpTime: {UpTime}ms", 
                    startupStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                startupStopwatch.Stop();
                _logger.LogError(ex, "[RABBITMQ_WORKER_FATAL_ERROR] Fatal error in Plant Analysis RabbitMQ Consumer Worker - UpTime: {UpTime}ms, ExceptionType: {ExceptionType}", 
                    startupStopwatch.ElapsedMilliseconds, ex.GetType().Name);
                throw;
            }
            finally
            {
                startupStopwatch.Stop();
                await CleanupAsync();
                _logger.LogInformation("[RABBITMQ_WORKER_STOP] Plant Analysis RabbitMQ Consumer Worker stopping - TotalUpTime: {TotalUpTime}ms", 
                    startupStopwatch.ElapsedMilliseconds);
            }
        }

        private async Task InitializeRabbitMQAsync()
        {
            var initStopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("[RABBITMQ_INIT_START] Initializing RabbitMQ connection - ConnectionString: {ConnectionString}, Heartbeat: {Heartbeat}s, RecoveryInterval: {RecoveryInterval}s", 
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
                
                _logger.LogInformation("[RABBITMQ_CONNECTION_SUCCESS] RabbitMQ connection established - ConnectionTime: {ConnectionTime}ms", 
                    connectionStart.ElapsedMilliseconds);

                var channelStart = Stopwatch.StartNew();
                _channel = await _connection.CreateChannelAsync();
                channelStart.Stop();

                _logger.LogInformation("[RABBITMQ_CHANNEL_SUCCESS] RabbitMQ channel created - ChannelTime: {ChannelTime}ms", 
                    channelStart.ElapsedMilliseconds);

                // Declare queue (make sure it exists)
                // Simple and robust approach: try to create with desired params, gracefully handle if exists with different params
                var queueDeclareStart = Stopwatch.StartNew();

                try
                {
                    // Queue arguments - 24h TTL for all analysis queues
                    var queueArguments = new Dictionary<string, object>
                    {
                        { "x-message-ttl", 86400000 } // 24 hours TTL
                    };

                    await _channel.QueueDeclareAsync(
                        queue: _rabbitMQOptions.Queues.PlantAnalysisResult,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: queueArguments);

                    queueDeclareStart.Stop();
                    _logger.LogInformation("[RABBITMQ_QUEUE_READY] Queue ready (created or already exists with matching config) - QueueName: {QueueName}, QueueDeclareTime: {QueueDeclareTime}ms",
                        _rabbitMQOptions.Queues.PlantAnalysisResult, queueDeclareStart.ElapsedMilliseconds);
                }
                catch (Exception queueEx) when (queueEx.Message.Contains("PRECONDITION_FAILED"))
                {
                    // Queue exists with different parameters - use it anyway (graceful degradation)
                    queueDeclareStart.Stop();
                    _logger.LogWarning("[RABBITMQ_QUEUE_EXISTS] Queue exists with different configuration - using existing queue - QueueName: {QueueName}, Error: {ErrorMessage}",
                        _rabbitMQOptions.Queues.PlantAnalysisResult, queueEx.Message);
                }

                initStopwatch.Stop();
                _logger.LogInformation("[RABBITMQ_INIT_SUCCESS] RabbitMQ initialized successfully - QueueName: {QueueName}, TotalInitTime: {TotalInitTime}ms",
                    _rabbitMQOptions.Queues.PlantAnalysisResult, initStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                initStopwatch.Stop();
                _logger.LogError(ex, "[RABBITMQ_INIT_ERROR] Failed to initialize RabbitMQ connection - InitAttemptTime: {InitAttemptTime}ms, ExceptionType: {ExceptionType}, Message: {ErrorMessage}", 
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

                _logger.LogInformation("[RABBITMQ_MESSAGE_RECEIVED] RabbitMQ message received - Size: {MessageSize} bytes, CorrelationId: {CorrelationId}, DeliveryTag: {DeliveryTag}", 
                    body.Length, correlationId, deliveryTag);
                
                _logger.LogDebug("[RABBITMQ_MESSAGE_CONTENT] Message content preview - CorrelationId: {CorrelationId}, Content: {MessageContent}", 
                    correlationId, message.Substring(0, Math.Min(500, message.Length)) + (message.Length > 500 ? "..." : ""));

                try
                {
                    var deserializationStart = Stopwatch.StartNew();
                    // Deserialize message
                    var analysisResult = JsonConvert.DeserializeObject<PlantAnalysisAsyncResponseDto>(message);
                    deserializationStart.Stop();
                    
                    if (analysisResult == null)
                    {
                        messageStopwatch.Stop();
                        _logger.LogWarning("[RABBITMQ_DESERIALIZATION_FAILED] Failed to deserialize message - CorrelationId: {CorrelationId}, DeliveryTag: {DeliveryTag}, ProcessingTime: {ProcessingTime}ms, MessageLength: {MessageLength}", 
                            correlationId, deliveryTag, messageStopwatch.ElapsedMilliseconds, message.Length);
                        
                        await _channel.BasicNackAsync(deliveryTag, false, false);
                        return;
                    }

                    _logger.LogInformation("[RABBITMQ_DESERIALIZATION_SUCCESS] Message deserialized successfully - CorrelationId: {CorrelationId}, DeserializationTime: {DeserializationTime}ms, AnalysisId: {AnalysisId}", 
                        correlationId, deserializationStart.ElapsedMilliseconds, analysisResult.AnalysisId);

                    // Enqueue Hangfire job for processing
                    var jobEnqueueStart = Stopwatch.StartNew();
                    var jobId = BackgroundJob.Enqueue<IPlantAnalysisJobService>(
                        service => service.ProcessPlantAnalysisResultAsync(analysisResult, correlationId));
                    jobEnqueueStart.Stop();

                    _logger.LogInformation("[RABBITMQ_JOB_ENQUEUED] Hangfire job enqueued successfully - CorrelationId: {CorrelationId}, JobId: {JobId}, AnalysisId: {AnalysisId}, EnqueueTime: {EnqueueTime}ms", 
                        correlationId, jobId, analysisResult.AnalysisId, jobEnqueueStart.ElapsedMilliseconds);

                    // Acknowledge message
                    await _channel.BasicAckAsync(deliveryTag, false);
                    
                    messageStopwatch.Stop();
                    _logger.LogInformation("[RABBITMQ_MESSAGE_PROCESSED] Message processed successfully - CorrelationId: {CorrelationId}, TotalProcessingTime: {TotalProcessingTime}ms, DeliveryTag: {DeliveryTag}", 
                        correlationId, messageStopwatch.ElapsedMilliseconds, deliveryTag);
                }
                catch (JsonException ex)
                {
                    messageStopwatch.Stop();
                    _logger.LogError(ex, "[RABBITMQ_JSON_ERROR] JSON deserialization error - CorrelationId: {CorrelationId}, DeliveryTag: {DeliveryTag}, ProcessingTime: {ProcessingTime}ms, MessageLength: {MessageLength}, ExceptionType: {ExceptionType}", 
                        correlationId, deliveryTag, messageStopwatch.ElapsedMilliseconds, message.Length, ex.GetType().Name);
                    
                    await _channel.BasicNackAsync(deliveryTag, false, false);
                }
                catch (Exception ex)
                {
                    messageStopwatch.Stop();
                    _logger.LogError(ex, "[RABBITMQ_PROCESSING_ERROR] Error processing message - CorrelationId: {CorrelationId}, DeliveryTag: {DeliveryTag}, ProcessingTime: {ProcessingTime}ms, ExceptionType: {ExceptionType}, Message: {ErrorMessage}", 
                        correlationId, deliveryTag, messageStopwatch.ElapsedMilliseconds, ex.GetType().Name, ex.Message);
                    
                    // Retry logic - nack with requeue for transient errors
                    var shouldRequeue = ShouldRetryMessage(ea, ex);
                    
                    _logger.LogInformation("[RABBITMQ_RETRY_DECISION] Message retry decision - CorrelationId: {CorrelationId}, ShouldRequeue: {ShouldRequeue}, ExceptionType: {ExceptionType}", 
                        correlationId, shouldRequeue, ex.GetType().Name);
                    
                    await _channel.BasicNackAsync(deliveryTag, false, shouldRequeue);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _rabbitMQOptions.Queues.PlantAnalysisResult,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("Started consuming messages from RabbitMQ");

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
                _logger.LogWarning(ex, "Error during cleanup");
            }
        }

        public override void Dispose()
        {
            CleanupAsync().GetAwaiter().GetResult();
            base.Dispose();
        }
    }
}