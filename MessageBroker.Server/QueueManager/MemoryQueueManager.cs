using System;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using MessageBroker.Server.Models;
using MessageBroker.Server.QueueManager.Abstractions;

namespace MessageBroker.Server.QueueManager {
    using Client = ActionBlock<MessageEvent>;

    public class MemoryQueueManager : IQueueManager {
        private readonly ConcurrentDictionary<string, ConcurrentSet<Client>> _topics;

        private readonly ConcurrentDictionary<Client, ConcurrentSet<string>> _users;

        public MemoryQueueManager() {
            _topics = new ConcurrentDictionary<string, ConcurrentSet<Client>>();
            _users  = new ConcurrentDictionary<Client, ConcurrentSet<string>>();
        }

        public void Publish(MessageEvent messageEvent) {
            if (!_topics.TryGetValue(messageEvent.Topic, out var clients)) {
                // Console.WriteLine($"No listeners on topic '{messageEvent.Topic}'");
                return;
            }

            foreach (var (client, _) in clients) {
                client.Post(messageEvent);
            }

            // Console.WriteLine($"Published message on topic '{messageEvent.Topic}'");
        }

        public void Subscribe(string topic, Client client) {
            var clients = _topics.GetOrAdd(topic, _ => new ConcurrentSet<Client>());
            clients.TryAdd(client, client);
            var userTopics = _users.GetOrAdd(client, _ => new ConcurrentSet<string>());
            userTopics.TryAdd(topic, topic);
            // Console.WriteLine($"Client subscribed to topic '{topic}'");
        }

        public void Unsubscribe(string topic, Client client) {
            if (_topics.TryGetValue(topic, out var clients)) {
                clients.TryRemove(client, out _);
            }

            if (_users.TryGetValue(client, out var userTopics)) {
                userTopics.TryRemove(topic, out _);
            }

            // Console.WriteLine($"Client unsubscribed from topic '{topic}'");
        }

        public void UnsubscribeFromAll(Client client) {
            if (!_users.TryGetValue(client, out var userTopics))
                return;
            foreach (var userTopic in userTopics) {
                if (_topics.TryGetValue(userTopic.Key, out var topic)) {
                    topic.TryRemove(client, out _);
                }
            }

            _users.TryRemove(client, out _);

            client.Complete();
            // Console.WriteLine("Client unsubscribed from all topics");
        }
    }
}
