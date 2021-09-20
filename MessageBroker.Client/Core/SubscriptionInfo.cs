using System;

namespace MessageBroker.Client.Core {
    public delegate void MessageBrokerEventHandler(object message);

    public sealed class SubscriptionInfo {
        public SubscriptionInfo(Type messageType) {
            MessageType = messageType;
        }

        public Type MessageType { get; }

        private event MessageBrokerEventHandler? MessagePublished;

        public void AddEventHandler(MessageBrokerEventHandler messageHandler) {
            MessagePublished += messageHandler;
        }

        public void RemoveEventHandler(MessageBrokerEventHandler messageHandler) {
            MessagePublished -= messageHandler;
        }

        public bool HasEventHandlers() {
            return MessagePublished != null;
        }

        public void OnMessagePublished(object message) {
            MessagePublished?.Invoke(message);
        }

        public void ClearEventHandlers() {
            MessagePublished = null;
        }
    }
}
