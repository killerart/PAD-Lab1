using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Tasks;
using MessageBroker.MessageShipper;
using MessageBroker.MessageShipper.Abstractions;
using MessageBroker.QueueManager.Abstractions;

namespace MessageBroker.QueueManager {
    public class MemoryQueueManager<T> : IQueueManager<T> where T : class {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<T, T>> _topics;

        private readonly ConcurrentDictionary<T, ConcurrentDictionary<string, string>> _users;

        // private readonly ConcurrentDictionary<string, ConcurrentQueue<string>>  _topics;

        private readonly IMessageShipper<T> _messageShipper;

        public MemoryQueueManager(IMessageShipper<T> messageShipper) {
            _messageShipper = messageShipper;
            _topics         = new ConcurrentDictionary<string, ConcurrentDictionary<T, T>>();
            _users          = new ConcurrentDictionary<T, ConcurrentDictionary<string, string>>();
            // _topics          = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
        }

        public void Publish(string topic, string message) {
            if (!_topics.TryGetValue(topic, out var clients)) {
                Console.WriteLine($"No listeners on topic '{topic}'");
                return;
            }

            Task.Run(() => _messageShipper.Deliver(clients.Keys, topic, message));
            Console.WriteLine($"Published message on topic '{topic}'");
        }

        public void Subscribe(string topic, T client) {
            var clients = _topics.GetOrAdd(topic, _ => new ConcurrentDictionary<T, T>());
            clients.TryAdd(client, client);
            var userTopics = _users.GetOrAdd(client, _ => new ConcurrentDictionary<string, string>());
            userTopics.TryAdd(topic, topic);
            Console.WriteLine($"Client subscribed to topic '{topic}'");
        }

        public void Unsubscribe(string topic, T client) {
            if (_topics.TryGetValue(topic, out var clients)) {
                clients.TryRemove(client, out _);
            }

            if (_users.TryGetValue(client, out var userTopics)) {
                userTopics.TryRemove(topic, out _);
            }

            Console.WriteLine($"Client unsubscribed from topic '{topic}'");
        }

        public void UnsubscribeFromAll(T client) {
            if (!_users.TryGetValue(client, out var userTopics))
                return;
            foreach (var userTopic in userTopics) {
                if (_topics.TryGetValue(userTopic.Key, out var topic)) {
                    topic.TryRemove(client, out _);
                }
            }

            _users.TryRemove(client, out _);

            Console.WriteLine("Client unsubscribed from all topics");
        }
    }
}
