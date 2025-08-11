using System.Threading.Tasks;

namespace Business.Services.MessageQueue
{
    public interface IMessageQueueService
    {
        Task<bool> PublishAsync<T>(string queueName, T message, string correlationId = null, string routingKey = null) where T : class;
        Task<T> ConsumeAsync<T>(string queueName) where T : class;
        void StartConsuming<T>(string queueName, System.Action<T, string> messageHandler) where T : class;
        void StopConsuming(string queueName);
        Task<bool> IsConnectedAsync();
    }
}