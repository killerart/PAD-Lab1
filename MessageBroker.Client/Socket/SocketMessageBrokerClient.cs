using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using MessageBroker.Client.Core;

namespace MessageBroker.Client.Socket {
    public class SocketMessageBrokerClient : MessageBrokerClientBase {
        private readonly TcpClient     _socket;
        private readonly NetworkStream _stream;
        private readonly StreamReader  _reader;
        private readonly StreamWriter  _writer;

        public SocketMessageBrokerClient(string host, int port) {
            _socket = new TcpClient(host, port);
            _stream = _socket.GetStream();
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream);
        }

        public override void StartListening() {
            Task.Run(Listen);
        }

        private async Task Listen() {
            while (true) {
                var firstLine = (await _reader.ReadLineAsync())?.Split();
                if (firstLine is null) {
                    break;
                }

                var command = firstLine[0];
                if (command != "EVENT") {
                    continue;
                }

                var topic               = firstLine[1];
                var contentLengthHeader = (await _reader.ReadLineAsync())!.ToLower();
                await _reader.ReadLineAsync();
                var length    = int.Parse(contentLengthHeader.Split("content-length: ", StringSplitOptions.RemoveEmptyEntries)[0]);
                var charArray = new char[length];
                await _reader.ReadAsync(charArray);
                await _reader.ReadLineAsync();
                var json = new string(charArray);

                HandleMessage(topic, json);
            }
        }

        public override async ValueTask Subscribe<T>(MessageBrokerEventHandler messageHandler) {
            var type = typeof(T);

            var topic = type.Name;

            var createdSubInfo = false;
            var sub = Subscriptions.GetOrAdd(topic,
                                             _ => {
                                                 createdSubInfo = true;
                                                 return new SubscriptionInfo(type);
                                             });
            sub.AddEventHandler(messageHandler);

            if (createdSubInfo) {
                await _writer.WriteLineAsync($"SUB {topic}");
                await _writer.WriteLineAsync();
                await _writer.FlushAsync();
            }
        }

        protected override async ValueTask Unsubscribe(string topic) {
            if (!Subscriptions.Remove(topic, out _)) {
                return;
            }

            await _writer.WriteLineAsync($"UNSUB {topic}");
            await _writer.WriteLineAsync();
            await _writer.FlushAsync();
        }

        public override async ValueTask Publish<T>(T message) {
            var topic = typeof(T).Name;
            var json  = JsonSerializer.Serialize(message);

            await _writer.WriteLineAsync($"PUB {topic}");
            await _writer.WriteLineAsync($"Content-Length: {json.Length}");
            await _writer.WriteLineAsync();
            await _writer.WriteLineAsync($"{json}");
            await _writer.WriteLineAsync();
            await _writer.FlushAsync();
        }

        public override async ValueTask Disconnect() {
            await _writer.WriteLineAsync("DISCONNECT");
            await _writer.WriteLineAsync();
            await _writer.FlushAsync();

            foreach (var sub in Subscriptions.Values) {
                sub.ClearEventHandlers();
            }

            Subscriptions.Clear();
        }

        protected override void Dispose(bool disposing) {
            _reader.Dispose();
            _writer.Dispose();
            _stream.Dispose();
            _socket.Dispose();
            base.Dispose(disposing);
        }
    }
}
