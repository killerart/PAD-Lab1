using System.Net.Sockets;

namespace MessageBroker.QueueManager.Abstractions {
    public interface IQueueManager {
        void Publish(string               topic, string    message);
        void Subscribe(string             topic, TcpClient client);
        void Unsubscribe(string           topic, TcpClient client);
        void UnsubscribeFromAll(TcpClient client);
    }
}
