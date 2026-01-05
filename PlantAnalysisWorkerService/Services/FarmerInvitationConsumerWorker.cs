using Business.Services.MessageQueue;
using Core.Configuration;
using Entities.Dtos;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PlantAnalysisWorkerService.Jobs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlantAnalysisWorkerService.Services
{
    /// <summary>
    /// Background service for consuming farmer invitation requests from RabbitMQ
    /// Pattern: Same as DealerInvitationConsumerWorker
    /// </summary>
    public class FarmerInvitationConsumerWorker : BackgroundService
    {
        private readonly ILogger<FarmerInvitationConsumerWorker> _logger;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IChannel? _channel;

        public FarmerInvitationConsumerWorker(
            ILogger<FarmerInvitationConsumerWorker> logger,
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
            _logger.LogInformation(
                "[FARMER_INVITATION_WORKER_START] Farmer Invitation Consumer Worker starting - QueueName: {QueueName}",
                _rabbitMQOptions.Queues.FarmerInvitationRequest);

            try
            {
                await InitializeRabbitMQAsync();
                startupStopwatch.Stop();

                _logger.LogInformation(
                    "[FARMER_INVITATION_WORKER_INITIALIZED] Worker initialized - InitTime: {InitTime}ms",
                    startupStopwatch.ElapsedMilliseconds);

                await StartConsumingAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[FARMER_INVITATION_WORKER_CANCELLED] Worker cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FARMER_INVITATION_WORKER_FATAL_ERROR] Fatal error");
                throw;
            }
            finally
            {
                await CleanupAsync();
            }
        }

        private async Task InitializeRabbitMQAsync()
        {
            _logger.LogInformation(
                "[FARMER_INVITATION_INIT_START] Initializing RabbitMQ connection");

            try
            {
                var factory = new ConnectionFactory();
                factory.Uri = new Uri(_rabbitMQOptions.ConnectionString);
                factory.AutomaticRecoveryEnabled = true;
                factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(
                    _rabbitMQOptions.ConnectionSettings.NetworkRecoveryInterval);
                factory.RequestedHeartbeat = TimeSpan.FromSeconds(
                    _rabbitMQOptions.ConnectionSettings.RequestedHeartbeat);

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                // Declare queue (durable, not auto-delete)
                await _channel.QueueDeclareAsync(
                    queue: _rabbitMQOptions.Queues.FarmerInvitationRequest,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _logger.LogInformation(
                    "[FARMER_INVITATION_INIT_SUCCESS] RabbitMQ initialized - Queue: {QueueName}",
                    _rabbitMQOptions.Queues.FarmerInvitationRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FARMER_INVITATION_INIT_ERROR] Failed to initialize RabbitMQ");
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

                _logger.LogInformation(
                    "[FARMER_INVITATION_MESSAGE_RECEIVED] Message received - Size: {Size}B, CorrelationId: {CorrelationId}",
                    body.Length, correlationId);

                try
                {
                    // Deserialize message
                    var invitationMessage = JsonConvert.DeserializeObject<FarmerInvitationQueueMessage>(message);

                    if (invitationMessage == null)
                    {
                        _logger.LogWarning(
                            "[FARMER_INVITATION_DESERIALIZATION_FAILED] Null message - CorrelationId: {CorrelationId}",
                            correlationId);

                        await _channel.BasicNackAsync(deliveryTag, false, false);
                        return;
                    }

                    _logger.LogInformation(
                        "[FARMER_INVITATION_DESERIALIZATION_SUCCESS] Message parsed - BulkJobId: {BulkJobId}, Phone: {Phone}, Row: {RowNumber}",
                        invitationMessage.BulkJobId, invitationMessage.Phone, invitationMessage.RowNumber);

                    // Enqueue Hangfire job for processing
                    var jobId = BackgroundJob.Enqueue<IFarmerInvitationJobService>(
                        service => service.ProcessFarmerInvitationAsync(invitationMessage, correlationId));

                    _logger.LogInformation(
                        "[FARMER_INVITATION_JOB_ENQUEUED] Hangfire job enqueued - JobId: {JobId}, BulkJobId: {BulkJobId}",
                        jobId, invitationMessage.BulkJobId);

                    // Acknowledge message
                    await _channel.BasicAckAsync(deliveryTag, false);

                    messageStopwatch.Stop();
                    _logger.LogInformation(
                        "[FARMER_INVITATION_MESSAGE_PROCESSED] Message processed - TotalTime: {TotalTime}ms",
                        messageStopwatch.ElapsedMilliseconds);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex,
                        "[FARMER_INVITATION_JSON_ERROR] JSON deserialization error - CorrelationId: {CorrelationId}",
                        correlationId);

                    await _channel.BasicNackAsync(deliveryTag, false, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[FARMER_INVITATION_PROCESSING_ERROR] Error processing message - CorrelationId: {CorrelationId}",
                        correlationId);

                    // Retry logic
                    var shouldRequeue = ShouldRetryMessage(ex);
                    await _channel.BasicNackAsync(deliveryTag, false, shouldRequeue);
                }
            };

            // Set prefetch count for controlled parallelism
            await _channel.BasicQosAsync(0, 5, false);  // Process 5 messages at a time

            await _channel.BasicConsumeAsync(
                queue: _rabbitMQOptions.Queues.FarmerInvitationRequest,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("[FARMER_INVITATION_CONSUMER_STARTED] Started consuming messages");

            // Keep alive
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private bool ShouldRetryMessage(Exception ex)
        {
            // Don't retry JSON errors (malformed message)
            if (ex is JsonException) return false;

            // Retry for transient errors (database, network, etc.)
            return true;
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
