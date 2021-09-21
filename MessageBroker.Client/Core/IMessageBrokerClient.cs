using System;
using System.Threading.Tasks;

namespace MessageBroker.Client.Core {
    public interface IMessageBrokerClient : IDisposable {
        void      StartListening();
        ValueTask Subscribe<T>(MessageBrokerEventHandler   messageHandler);
        ValueTask Unsubscribe<T>(MessageBrokerEventHandler messageHandler);

        ValueTask Publish<T>(T message);
        ValueTask Disconnect();
    }
}
