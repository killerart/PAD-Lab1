namespace MessageBroker.QueueManager.Abstractions {
    public interface IQueueManager<in T> {
        void Publish(string       topic, string message);
        void Subscribe(string     topic, T      client);
        void Unsubscribe(string   topic, T      client);
        void UnsubscribeFromAll(T client);
    }
}
