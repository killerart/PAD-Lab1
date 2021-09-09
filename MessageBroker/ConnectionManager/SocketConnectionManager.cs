using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using MessageBroker.ConnectionManager.Abstractions;
using MessageBroker.QueueManager;
using MessageBroker.QueueManager.Abstractions;

namespace MessageBroker.ConnectionManager {
    public class SocketConnectionManager : IConnectionManager {
        private readonly TcpListener   _socket;
        private readonly int           _port;
        private readonly IQueueManager _queueManager;

        public SocketConnectionManager(int port) {
            _port         = port;
            _socket       = new TcpListener(IPAddress.Any, port);
            _queueManager = new MemoryQueueManager();
        }

        public void Start() {
            _socket.Start();
            _socket.BeginAcceptTcpClient(HandleConnection, _socket);
            Console.WriteLine($"Socket listening on port {_port}");
        }

        private async void HandleConnection(IAsyncResult result) {
            Console.WriteLine("Connected");
            _socket.BeginAcceptTcpClient(HandleConnection, _socket);

            using var       client = _socket.EndAcceptTcpClient(result);
            await using var stream = client.GetStream();
            using var       reader = new StreamReader(stream);
            await using var writer = new StreamWriter(stream) {
                AutoFlush = true
            };

            try {
                while (true) {
                    var firstLine = await reader.ReadLineAsync();

                    var args = firstLine!.Split();

                    var command = args[0].ToUpper();
                    switch (command) {
                        case "HELP": {
                            await writer.WriteLineAsync("Available commands:\r\n" +
                                                        "HELP: Prints this text\r\n" +
                                                        "PUB <queue>\\r\\nContent-Length: <character-length>\\r\\n<message>\\r\\n: Publish <message> to <queue>\r\n" +
                                                        "SUB <queue>: Subscribe to messages on <queue>\r\n" +
                                                        "UNSUB <queue>: Unsubscribe to messages on <queue>\r\n" +
                                                        "PING: PONG\r\n" +
                                                        "DISCONNECT: Disconnect from the server\r\n");
                            break;
                        }
                        case "PING": {
                            await writer.WriteLineAsync("PONG");
                            break;
                        }
                        case "DISCONNECT": {
                            throw new Exception();
                        }
                        case "PUB": {
                            var topic               = args[1];
                            var contentLengthHeader = (await reader.ReadLineAsync())!.ToLower();
                            await reader.ReadLineAsync();
                            var length    = int.Parse(contentLengthHeader.Split("content-length: ", StringSplitOptions.RemoveEmptyEntries)[0]);
                            var charArray = new char[length];
                            await reader.ReadAsync(charArray);
                            var message = new string(charArray);
                            // var obj     = JsonSerializer.Deserialize<object>(message);
                            // Console.WriteLine(JsonSerializer.Serialize(obj));
                            _queueManager.Publish(topic, message);

                            break;
                        }
                        case "SUB": {
                            var topic = args[1];
                            _queueManager.Subscribe(topic, client);
                            break;
                        }
                        case "UNSUB": {
                            var topic = args[1];
                            _queueManager.Unsubscribe(topic, client);
                            break;
                        }
                    }
                }
            } catch (Exception) {
                _queueManager.UnsubscribeFromAll(client);
            }
        }
    }
}
