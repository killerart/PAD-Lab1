using System;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using MessageBroker.Server.Models;
using MessageBroker.Server.QueueManager.Abstractions;

namespace MessageBroker.Server.QueueManager {
    using Client = ActionBlock<MessageEvent>;

    public class MemoryQueueManager : IQueueManager {
        private readonly ConcurrentDictionary<string, Topic>                 _topics;
        private readonly ConcurrentDictionary<Client, ConcurrentSet<string>> _users;

        public MemoryQueueManager() {
            _topics = new ConcurrentDictionary<string, Topic>();
            _users  = new ConcurrentDictionary<Client, ConcurrentSet<string>>();
        }

        public void Publish(MessageEvent messageEvent) {
            var topic = _topics.GetOrAdd(messageEvent.Topic, _ => new Topic());

            topic.Messages.Enqueue(messageEvent);

            foreach (var client in topic.Clients.Keys) {
                client.Post(messageEvent);
            }

            // Console.WriteLine($"Published message on topic '{messageEvent.Topic}'");
        }

        public void Subscribe(string topicName, Client client) {
            var topic = _topics.GetOrAdd(topicName, _ => new Topic());
            topic.Clients.TryAdd(client, client);
            var userTopics = _users.GetOrAdd(client, _ => new ConcurrentSet<string>());
            userTopics.TryAdd(topicName, topicName);

            foreach (var message in topic.Messages) {
                client.Post(message);
            }
            // Console.WriteLine($"Client subscribed to topic '{topic}'");
        }

        public void Unsubscribe(string topicName, Client client) {
            if (_topics.TryGetValue(topicName, out var topic)) {
                topic.Clients.TryRemove(client, out _);
            }

            if (_users.TryGetValue(client, out var userTopics)) {
                userTopics.TryRemove(topicName, out _);
            }

            // Console.WriteLine($"Client unsubscribed from topic '{topic}'");
        }

        public void UnsubscribeFromAll(Client client) {
            if (!_users.TryGetValue(client, out var userTopics))
                return;
            foreach (var userTopic in userTopics.Keys) {
                if (_topics.TryGetValue(userTopic, out var topic)) {
                    topic.Clients.TryRemove(client, out _);
                }
            }

            _users.TryRemove(client, out _);

            client.Complete();
            // Console.WriteLine("Client unsubscribed from all topics");
        }
    }
}
