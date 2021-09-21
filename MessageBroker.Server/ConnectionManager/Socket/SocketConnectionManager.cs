using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Server.ConnectionManager.Abstractions;
using MessageBroker.Server.MessageShipper.Socket;
using MessageBroker.Server.QueueManager;
using MessageBroker.Server.QueueManager.Abstractions;

namespace MessageBroker.Server.ConnectionManager.Socket {
    public class SocketConnectionManager : IConnectionManager {
        private readonly TcpListener              _socket;
        private readonly int                      _port;
        private readonly IQueueManager<TcpClient> _queueManager;

        public SocketConnectionManager(int port) {
            _port         = port;
            _socket       = new TcpListener(IPAddress.Any, port);
            _queueManager = new MemoryQueueManager<TcpClient>(new TcpMessageShipper());
        }

        public void Start() {
            _socket.Start();
            Console.WriteLine($"Socket listening on port {_port}");
            Task.Run(async () => {
                while (true) {
                    var client = await _socket.AcceptTcpClientAsync();
                    Task.Run(async () => {
                        // Console.WriteLine("Client connected");
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
                                    case "DISCONNECT": {
                                        await reader.ReadLineAsync();
                                        throw new Exception();
                                    }
                                    case "PUB": {
                                        var topic               = args[1];
                                        var contentLengthHeader = (await reader.ReadLineAsync())!.ToLower();
                                        await reader.ReadLineAsync();
                                        var length = int.Parse(contentLengthHeader.Split("content-length: ", StringSplitOptions.RemoveEmptyEntries)[0]);
                                        var charArray = new char[length];
                                        await reader.ReadAsync(charArray);
                                        await reader.ReadLineAsync();
                                        await reader.ReadLineAsync();
                                        var message = new string(charArray);
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
                            // Console.WriteLine("Client disconnected");
                            client.Dispose();
                        }
                    });
                }
            });
        }
    }
}
