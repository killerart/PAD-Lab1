using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;

namespace MessageBroker.Client.Core {
    public abstract class MessageBrokerClientBase : IMessageBrokerClient {
        protected readonly ConcurrentDictionary<string, SubscriptionInfo> Subscriptions;

        protected MessageBrokerClientBase() {
            Subscriptions = new ConcurrentDictionary<string, SubscriptionInfo>();
        }

        protected void HandleMessage(string topic, string json) {
            if (!Subscriptions.TryGetValue(topic, out var subscription))
                return;

            var message = JsonSerializer.Deserialize(json, subscription.MessageType);
            if (message is null)
                return;

            subscription.OnMessagePublished(message);

            // var handlerType  = typeof(IMessageHandler<>).MakeGenericType(subscription.MessageType);
            // var handleMethod = handlerType.GetMethod(nameof(IMessageHandler<object>.HandleAsync));
            // if (handleMethod is null)
            //     return;
            //
            // await (ValueTask)handleMethod.Invoke(subscription.Handler,
            //                                      new[] {
            //                                          message
            //                                      })!;
        }

        public abstract void      StartListening();
        public abstract ValueTask Subscribe<T>(MessageBrokerEventHandler messageHandler);

        public virtual async ValueTask Unsubscribe<T>(MessageBrokerEventHandler messageHandler) {
            var topic = typeof(T).Name;
            if (!Subscriptions.TryGetValue(topic, out var sub))
                return;

            sub.RemoveEventHandler(messageHandler);

            if (sub.HasEventHandlers())
                return;

            await Unsubscribe(topic);
        }

        protected abstract ValueTask Unsubscribe(string topic);

        public abstract ValueTask Publish<T>(T message);
        public abstract ValueTask Disconnect();

        protected void ClearSubs() {
            foreach (var sub in Subscriptions) {
                sub.Value.ClearEventHandlers();
            }

            Subscriptions.Clear();
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                ClearSubs();
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MessageBrokerClientBase() {
            Dispose(false);
        }
    }
}
