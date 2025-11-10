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
    /// Background service for consuming farmer subscription assignment requests from RabbitMQ
    /// Pattern: Same as FarmerCodeDistributionConsumerWorker
    /// </summary>
    public class FarmerSubscriptionAssignmentConsumerWorker : BackgroundService
    {
        private readonly ILogger<FarmerSubscriptionAssignmentConsumerWorker> _logger;
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IChannel? _channel;

        public FarmerSubscriptionAssignmentConsumerWorker(
            ILogger<FarmerSubscriptionAssignmentConsumerWorker> logger,
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
                "[FARMER_SUBSCRIPTION_ASSIGNMENT_WORKER_START] Farmer Subscription Assignment Consumer Worker starting - QueueName: {QueueName}",
                _rabbitMQOptions.Queues.FarmerSubscriptionAssignmentRequest);

            try
            {
                await InitializeRabbitMQAsync();
                startupStopwatch.Stop();

                _logger.LogInformation(
                    "[FARMER_SUBSCRIPTION_ASSIGNMENT_WORKER_INITIALIZED] Worker initialized - InitTime: {InitTime}ms",
                    startupStopwatch.ElapsedMilliseconds);

                await StartConsumingAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[FARMER_SUBSCRIPTION_ASSIGNMENT_WORKER_CANCELLED] Worker cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FARMER_SUBSCRIPTION_ASSIGNMENT_WORKER_FATAL_ERROR] Fatal error");
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
                "[FARMER_SUBSCRIPTION_ASSIGNMENT_INIT_START] Initializing RabbitMQ connection");

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
                    queue: _rabbitMQOptions.Queues.FarmerSubscriptionAssignmentRequest,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _logger.LogInformation(
                    "[FARMER_SUBSCRIPTION_ASSIGNMENT_INIT_SUCCESS] RabbitMQ initialized - Queue: {QueueName}",
                    _rabbitMQOptions.Queues.FarmerSubscriptionAssignmentRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FARMER_SUBSCRIPTION_ASSIGNMENT_INIT_ERROR] Failed to initialize RabbitMQ");
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
                    "[FARMER_SUBSCRIPTION_ASSIGNMENT_MESSAGE_RECEIVED] Message received - Size: {Size}B, CorrelationId: {CorrelationId}",
                    body.Length, correlationId);

                try
                {
                    // Deserialize message
                    var assignmentMessage = JsonConvert.DeserializeObject<FarmerSubscriptionAssignmentQueueMessage>(message);

                    if (assignmentMessage == null)
                    {
                        _logger.LogWarning(
                            "[FARMER_SUBSCRIPTION_ASSIGNMENT_DESERIALIZATION_FAILED] Null message - CorrelationId: {CorrelationId}",
                            correlationId);

                        await _channel.BasicNackAsync(deliveryTag, false, false);
                        return;
                    }

                    _logger.LogInformation(
                        "[FARMER_SUBSCRIPTION_ASSIGNMENT_DESERIALIZATION_SUCCESS] Message parsed - BulkJobId: {BulkJobId}, Email: {Email}, Phone: {Phone}, Row: {RowNumber}",
                        assignmentMessage.BulkJobId, assignmentMessage.Email, assignmentMessage.Phone, assignmentMessage.RowNumber);

                    // Enqueue Hangfire job for processing
                    var jobId = BackgroundJob.Enqueue<IFarmerSubscriptionAssignmentJobService>(
                        service => service.ProcessFarmerSubscriptionAssignmentAsync(assignmentMessage, correlationId));

                    messageStopwatch.Stop();

                    _logger.LogInformation(
                        "[FARMER_SUBSCRIPTION_ASSIGNMENT_HANGFIRE_ENQUEUED] Job enqueued - HangfireJobId: {HangfireJobId}, Duration: {Duration}ms",
                        jobId, messageStopwatch.ElapsedMilliseconds);

                    // Acknowledge message after successful Hangfire enqueue
                    await _channel.BasicAckAsync(deliveryTag, false);

                    _logger.LogInformation(
                        "[FARMER_SUBSCRIPTION_ASSIGNMENT_MESSAGE_ACKED] Message acknowledged - DeliveryTag: {DeliveryTag}",
                        deliveryTag);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx,
                        "[FARMER_SUBSCRIPTION_ASSIGNMENT_DESERIALIZATION_ERROR] JSON deserialization error - CorrelationId: {CorrelationId}, RawMessage: {Message}",
                        correlationId, message);

                    // Reject message without requeue (invalid format)
                    await _channel.BasicNackAsync(deliveryTag, false, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[FARMER_SUBSCRIPTION_ASSIGNMENT_MESSAGE_ERROR] Error processing message - CorrelationId: {CorrelationId}",
                        correlationId);

                    // Reject with requeue (transient error)
                    await _channel.BasicNackAsync(deliveryTag, false, true);
                }
            };

            // Start consuming with prefetch count
            await _channel.BasicQosAsync(0, 5, false);  // Process 5 messages at a time
            await _channel.BasicConsumeAsync(
                queue: _rabbitMQOptions.Queues.FarmerSubscriptionAssignmentRequest,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation(
                "[FARMER_SUBSCRIPTION_ASSIGNMENT_CONSUMING] Started consuming messages - PrefetchCount: {PrefetchCount}",
                5);

            // Keep worker alive
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task CleanupAsync()
        {
            _logger.LogInformation("[FARMER_SUBSCRIPTION_ASSIGNMENT_CLEANUP] Cleaning up resources");

            try
            {
                if (_channel != null)
                {
                    await _channel.CloseAsync();
                    _channel.Dispose();
                    _logger.LogInformation("[FARMER_SUBSCRIPTION_ASSIGNMENT_CHANNEL_CLOSED] Channel closed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FARMER_SUBSCRIPTION_ASSIGNMENT_CHANNEL_CLOSE_ERROR] Error closing channel");
            }

            try
            {
                if (_connection != null)
                {
                    await _connection.CloseAsync();
                    _connection.Dispose();
                    _logger.LogInformation("[FARMER_SUBSCRIPTION_ASSIGNMENT_CONNECTION_CLOSED] Connection closed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FARMER_SUBSCRIPTION_ASSIGNMENT_CONNECTION_CLOSE_ERROR] Error closing connection");
            }
        }

        public override void Dispose()
        {
            CleanupAsync().GetAwaiter().GetResult();
            base.Dispose();
        }
    }
}
