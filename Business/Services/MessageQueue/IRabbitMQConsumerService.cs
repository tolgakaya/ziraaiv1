using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Services.MessageQueue
{
    public interface IRabbitMQConsumerService
    {
        Task StartConsumingAsync<T>(string queueName, Func<T, string, Task> messageHandler, CancellationToken cancellationToken) where T : class;
        void StopConsuming();
        Task<bool> IsHealthyAsync();
    }
}