using System.Threading.Tasks;

namespace MessageBroker.Client.Abstractions {
    public interface IMessageBrokerClient {
        void      StartListening();
        ValueTask Subscribe<T>(IMessageHandler<T> messageHandler);
        ValueTask Unsubscribe<T>();
        ValueTask Publish<T>(T message);
        ValueTask Disconnect();
    }
}
