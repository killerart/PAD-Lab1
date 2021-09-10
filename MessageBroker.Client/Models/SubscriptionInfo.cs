using System;
using MessageBroker.Client.Abstractions;

namespace MessageBroker.Client.Models {
    internal class SubscriptionInfo {
        public SubscriptionInfo(Type messageType, IMessageHandler handler) {
            MessageType = messageType;
            Handler     = handler;
        }

        public Type            MessageType { get; }
        public IMessageHandler Handler     { get; }
    }
}
