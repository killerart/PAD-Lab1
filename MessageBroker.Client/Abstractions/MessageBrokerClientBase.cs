using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using MessageBroker.Client.Models;

namespace MessageBroker.Client.Abstractions {
    public abstract class MessageBrokerClientBase : IMessageBrokerClient {
        protected readonly ConcurrentDictionary<string, SubscriptionInfo> Subscriptions;

        protected MessageBrokerClientBase() {
            Subscriptions = new ConcurrentDictionary<string, SubscriptionInfo>();
        }

        protected async ValueTask HandleMessage(string topic, string json) {
            if (!Subscriptions.TryGetValue(topic, out var subscription))
                return;

            var message = JsonSerializer.Deserialize(json, subscription.MessageType);
            if (message is null)
                return;

            var handlerType  = typeof(IMessageHandler<>).MakeGenericType(subscription.MessageType);
            var handleMethod = handlerType.GetMethod(nameof(IMessageHandler<object>.HandleAsync));
            if (handleMethod is null)
                return;

            await (ValueTask)handleMethod.Invoke(subscription.Handler,
                                                 new[] {
                                                     message
                                                 })!;
        }

        public abstract void      StartListening();
        public abstract ValueTask Subscribe<T>(IMessageHandler<T> messageHandler);
        public abstract ValueTask Unsubscribe<T>();
        public abstract ValueTask Publish<T>(T message);
        public abstract ValueTask Disconnect();
    }
}
