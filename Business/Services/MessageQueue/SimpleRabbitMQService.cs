using Business.Services.Configuration;
using Core.Configuration;
using Entities.Constants;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.MessageQueue
{
    public class SimpleRabbitMQService : IMessageQueueService, IDisposable
    {
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly IConfigurationService _configurationService;
        private IConnection _connection;
        private IChannel _channel;
        private bool _disposed = false;

        public SimpleRabbitMQService(
            IOptions<RabbitMQOptions> rabbitMQOptions,
            IConfigurationService configurationService)
        {
            _rabbitMQOptions = rabbitMQOptions.Value;
            _configurationService = configurationService;
        }

        private async Task EnsureConnectionAsync()
        {
            if (_connection?.IsOpen == true && _channel?.IsOpen == true)
                return;

            try
            {
                var factory = new ConnectionFactory();
                factory.Uri = new Uri(_rabbitMQOptions.ConnectionString);
                factory.AutomaticRecoveryEnabled = true;
                factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(_rabbitMQOptions.ConnectionSettings.NetworkRecoveryInterval);
                factory.RequestedHeartbeat = TimeSpan.FromSeconds(_rabbitMQOptions.ConnectionSettings.RequestedHeartbeat);

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to connect to RabbitMQ: {ex.Message}", ex);
            }
        }

        public async Task<bool> PublishAsync<T>(string queueName, T message, string correlationId = null, string routingKey = null) where T : class
        {
            try
            {
                await EnsureConnectionAsync();

                // Declare queue if not exists
                await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);

                var json = JsonConvert.SerializeObject(message, Formatting.None);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                };

                if (!string.IsNullOrEmpty(correlationId))
                    properties.CorrelationId = correlationId;

                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: routingKey ?? queueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to publish message to queue '{queueName}': {ex.Message}", ex);
            }
        }

        public async Task<T> ConsumeAsync<T>(string queueName) where T : class
        {
            await EnsureConnectionAsync();

            await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);

            var result = await _channel.BasicGetAsync(queueName, autoAck: true);
            if (result == null)
                return null;

            var json = Encoding.UTF8.GetString(result.Body.ToArray());
            return JsonConvert.DeserializeObject<T>(json);
        }

        public void StartConsuming<T>(string queueName, Action<T, string> messageHandler) where T : class
        {
            // For now, just return - this would need a background service implementation
            // We'll implement this in a separate worker service
        }

        public void StopConsuming(string queueName)
        {
            // For now, just return
        }

        public async Task<bool> IsConnectedAsync()
        {
            try
            {
                await EnsureConnectionAsync();
                return _connection?.IsOpen == true && _channel?.IsOpen == true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _channel?.CloseAsync();
            _channel?.Dispose();
            _connection?.CloseAsync();
            _connection?.Dispose();

            _disposed = true;
        }
    }
}