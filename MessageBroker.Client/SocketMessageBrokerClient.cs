using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using MessageBroker.Client.Abstractions;
using MessageBroker.Client.Models;

namespace MessageBroker.Client {
    public class SocketMessageBrokerClient : MessageBrokerClientBase, IDisposable {
        private readonly TcpClient _socket;

        public SocketMessageBrokerClient(string host, int port) {
            _socket = new TcpClient(host, port);
        }

        public override void StartListening() {
            Task.Run(Listen);
        }

        private async Task Listen() {
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
                var json = new string(charArray);

                await HandleMessage(topic, json);
            }
        }

        public override async ValueTask Subscribe<T>(IMessageHandler<T> messageHandler) {
            var type   = typeof(T);
            var sub    = new SubscriptionInfo(type, messageHandler);
            var stream = _socket.GetStream();
            var writer = new StreamWriter(stream);

            var topic = type.Name;
            if (!Subscriptions.TryAdd(topic, sub))
                return;

            await writer.WriteLineAsync($"SUB {topic}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }

        public override async ValueTask Unsubscribe<T>() {
            var topic = typeof(T).Name;
            await Unsubscribe(topic);
        }

        private async ValueTask Unsubscribe(string topic) {
            var stream = _socket.GetStream();
            var writer = new StreamWriter(stream);
            if (!Subscriptions.Remove(topic, out _)) {
                return;
            }

            await writer.WriteLineAsync($"UNSUB {topic}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }

        public override async ValueTask Publish<T>(T message) {
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

        public override async ValueTask Disconnect() {
            Subscriptions.Clear();
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
