using Business.Services.Configuration;
using Entities.Constants;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Services.MessageQueue
{
    public class RabbitMQConsumerService : IRabbitMQConsumerService, IDisposable
    {
        private readonly IConfigurationService _configurationService;
        private IConnection _connection;
        private IChannel _channel;
        private bool _disposed = false;

        public RabbitMQConsumerService(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        private async Task EnsureConnectionAsync()
        {
            if (_connection?.IsOpen == true && _channel?.IsOpen == true)
                return;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = "localhost",
                    Port = 5672,
                    UserName = "guest",
                    Password = "guest",
                    VirtualHost = "/",
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to connect to RabbitMQ: {ex.Message}", ex);
            }
        }

        public async Task StartConsumingAsync<T>(string queueName, Func<T, string, Task> messageHandler, CancellationToken cancellationToken) where T : class
        {
            await EnsureConnectionAsync();

            await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var message = JsonConvert.DeserializeObject<T>(json);
                    var correlationId = ea.BasicProperties?.CorrelationId;

                    await messageHandler(message, correlationId);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    // Log error and reject message
                    Console.WriteLine($"Error processing message from queue '{queueName}': {ex.Message}");
                    await _channel.BasicRejectAsync(ea.DeliveryTag, false);
                }
            };

            await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

            // Keep consuming until cancellation is requested
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                StopConsuming();
            }
        }

        public void StopConsuming()
        {
            _channel?.CloseAsync();
            _connection?.CloseAsync();
        }

        public async Task<bool> IsHealthyAsync()
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