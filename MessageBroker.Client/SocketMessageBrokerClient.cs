using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using MessageBroker.Client.Abstractions;
using MessageBroker.Client.Models;

namespace MessageBroker.Client {
    public class SocketMessageBrokerClient : IMessageBrokerClient, IDisposable {
        private readonly TcpClient                            _socket;
        private readonly Dictionary<string, SubscriptionInfo> _subscriptions;

        public SocketMessageBrokerClient(string host, int port) {
            _socket        = new TcpClient(host, port);
            _subscriptions = new Dictionary<string, SubscriptionInfo>();
        }

        public void StartListening() {
            Task.Run(async () => {
                var stream = _socket.GetStream();
                var reader = new StreamReader(stream);
                while (true) {
                    var firstLine = (await reader.ReadLineAsync())?.Split();
                    if (firstLine is null) {
                        break;
                    }

                    var command = firstLine[0];
                    if (command != "EVENT") {
                        continue;
                    }

                    var topic               = firstLine[1];
                    var contentLengthHeader = (await reader.ReadLineAsync())!.ToLower();
                    await reader.ReadLineAsync();
                    var length    = int.Parse(contentLengthHeader.Split("content-length: ", StringSplitOptions.RemoveEmptyEntries)[0]);
                    var charArray = new char[length];
                    await reader.ReadAsync(charArray);
                    await reader.ReadLineAsync();

                    if (!_subscriptions.TryGetValue(topic, out var subscription))
                        continue;

                    var json    = new string(charArray);
                    var message = JsonSerializer.Deserialize(json, subscription.MessageType);
                    if (message is null)
                        continue;

                    var handlerType  = typeof(IMessageHandler<>).MakeGenericType(subscription.MessageType);
                    var handleMethod = handlerType.GetMethod(nameof(IMessageHandler<object>.HandleAsync));
                    if (handleMethod is null)
                        continue;

                    await (ValueTask)handleMethod.Invoke(subscription.Handler,
                                                         new[] {
                                                             message
                                                         })!;
                }
            });
        }

        public async ValueTask Subscribe<T>(IMessageHandler<T> messageHandler) {
            var type   = typeof(T);
            var sub    = new SubscriptionInfo(type, messageHandler);
            var stream = _socket.GetStream();
            var writer = new StreamWriter(stream);

            var topic = type.Name;
            if (!_subscriptions.TryAdd(topic, sub))
                return;

            await writer.WriteLineAsync($"SUB {topic}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }

        public async ValueTask Unsubscribe<T>() {
            var topic = typeof(T).Name;
            await Unsubscribe(topic);
        }

        private async ValueTask Unsubscribe(string topic) {
            var stream = _socket.GetStream();
            var writer = new StreamWriter(stream);
            if (!_subscriptions.Remove(topic)) {
                return;
            }

            await writer.WriteLineAsync($"UNSUB {topic}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }

        public async ValueTask Publish<T>(T message) {
            var stream = _socket.GetStream();
            var writer = new StreamWriter(stream);
            var topic  = typeof(T).Name;
            var json   = JsonSerializer.Serialize(message);
            await writer.WriteLineAsync($"PUB {topic}");
            await writer.WriteLineAsync($"Content-Length: {json.Length}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync($"{json}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }

        public async ValueTask Disconnect() {
            var stream = _socket.GetStream();
            var writer = new StreamWriter(stream);
            await writer.WriteLineAsync("DISCONNECT");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }

        public void Dispose() {
            _socket.Dispose();
        }
    }
}
