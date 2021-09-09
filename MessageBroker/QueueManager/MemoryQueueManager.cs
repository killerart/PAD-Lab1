using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Tasks;
using MessageBroker.MessageShipper;
using MessageBroker.QueueManager.Abstractions;

namespace MessageBroker.QueueManager {
    public class MemoryQueueManager : IQueueManager {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<TcpClient, TcpClient>> _topics;

        private readonly ConcurrentDictionary<TcpClient, ConcurrentDictionary<string, string>> _users;

        // private readonly ConcurrentDictionary<string, ConcurrentQueue<string>>  _topics;

        public MemoryQueueManager() {
            _topics = new ConcurrentDictionary<string, ConcurrentDictionary<TcpClient, TcpClient>>();
            _users  = new ConcurrentDictionary<TcpClient, ConcurrentDictionary<string, string>>();
            // _topics          = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
        }

        public void Publish(string topic, string message) {
            if (!_topics.TryGetValue(topic, out var clients)) {
                return;
            }

            Task.Run(() => TcpMessageShipper.Deliver(clients.Keys, topic, message));
        }

        public void Subscribe(string topic, TcpClient client) {
            var clients = _topics.GetOrAdd(topic, _ => new ConcurrentDictionary<TcpClient, TcpClient>());
            clients.TryAdd(client, client);
            var userTopics = _users.GetOrAdd(client, _ => new ConcurrentDictionary<string, string>());
            userTopics.TryAdd(topic, topic);
        }

        public void Unsubscribe(string topic, TcpClient client) {
            if (_topics.TryGetValue(topic, out var clients)) {
                clients.TryRemove(client, out _);
            }

            if (_users.TryGetValue(client, out var userTopics)) {
                userTopics.TryRemove(topic, out _);
            }
        }

        public void UnsubscribeFromAll(TcpClient client) {
            if (!_users.TryGetValue(client, out var userTopics))
                return;
            foreach (var userTopic in userTopics) {
                if (_topics.TryGetValue(userTopic.Key, out var topic)) {
                    topic.TryRemove(client, out _);
                }
            }

            _users.TryRemove(client, out _);
        }
    }
}
