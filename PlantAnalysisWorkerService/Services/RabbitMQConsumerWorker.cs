using Business.Services.MessageQueue;
using Core.Configuration;
using Entities.Dtos;
using Hangfire;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PlantAnalysisWorkerService.Jobs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
            _logger.LogInformation("Plant Analysis RabbitMQ Consumer Worker starting...");

            try
            {
                await InitializeRabbitMQAsync();
                await StartConsumingAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Plant Analysis RabbitMQ Consumer Worker cancellation requested");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Plant Analysis RabbitMQ Consumer Worker");
                throw;
            }
            finally
            {
                await CleanupAsync();
                _logger.LogInformation("Plant Analysis RabbitMQ Consumer Worker stopping...");
            }
        }

        private async Task InitializeRabbitMQAsync()
        {
            try
            {
                var factory = new ConnectionFactory();
                factory.Uri = new Uri(_rabbitMQOptions.ConnectionString);
                factory.AutomaticRecoveryEnabled = true;
                factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(_rabbitMQOptions.ConnectionSettings.NetworkRecoveryInterval);
                factory.RequestedHeartbeat = TimeSpan.FromSeconds(_rabbitMQOptions.ConnectionSettings.RequestedHeartbeat);

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                // Declare queue (make sure it exists)
                await _channel.QueueDeclareAsync(
                    queue: _rabbitMQOptions.Queues.PlantAnalysisResult,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _logger.LogInformation($"Connected to RabbitMQ: {_rabbitMQOptions.ConnectionString}");
                _logger.LogInformation($"Listening on queue: {_rabbitMQOptions.Queues.PlantAnalysisResult}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
                throw;
            }
        }

        private async Task StartConsumingAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var correlationId = ea.BasicProperties?.CorrelationId ?? "unknown";

                _logger.LogInformation($"Received message with correlation ID: {correlationId}");

                try
                {
                    // Deserialize message
                    var analysisResult = JsonConvert.DeserializeObject<PlantAnalysisAsyncResponseDto>(message);
                    
                    if (analysisResult == null)
                    {
                        _logger.LogWarning($"Failed to deserialize message: {message}");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                        return;
                    }

                    // Enqueue Hangfire job for processing
                    var jobId = BackgroundJob.Enqueue<IPlantAnalysisJobService>(
                        service => service.ProcessPlantAnalysisResultAsync(analysisResult, correlationId));

                    _logger.LogInformation($"Enqueued Hangfire job {jobId} for analysis: {analysisResult.AnalysisId}");

                    // Acknowledge message
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"JSON deserialization error for message: {message}");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing message with correlation ID: {correlationId}");
                    
                    // Retry logic - nack with requeue for transient errors
                    var shouldRequeue = ShouldRetryMessage(ea, ex);
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, shouldRequeue);
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