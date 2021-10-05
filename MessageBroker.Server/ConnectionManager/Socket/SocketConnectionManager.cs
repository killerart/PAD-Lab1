using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MessageBroker.Server.ConnectionManager.Abstractions;
using MessageBroker.Server.MessageShipper.Abstractions;
using MessageBroker.Server.MessageShipper.Socket;
using MessageBroker.Server.Models;
using MessageBroker.Server.QueueManager;
using MessageBroker.Server.QueueManager.Abstractions;

namespace MessageBroker.Server.ConnectionManager.Socket {
    public class SocketConnectionManager : IConnectionManager {
        private readonly TcpListener                   _socket;
        private readonly int                           _port;
        private readonly IQueueManager                 _queueManager;
        private readonly IMessageShipper<StreamWriter> _messageShipper;

        public SocketConnectionManager(int port) {
            _port           = port;
            _socket         = new TcpListener(IPAddress.Any, port);
            _queueManager   = new MemoryQueueManager();
            _messageShipper = new TcpMessageShipper();
        }

        public void Start() {
            _socket.Start();
            _socket.BeginAcceptTcpClient(HandleConnection, _socket);
            Console.WriteLine($"Socket listening on port {_port}");
        }

        private void HandleConnection(IAsyncResult result) {
            _socket.BeginAcceptTcpClient(HandleConnection, _socket);

            using var client = _socket.EndAcceptTcpClient(result);
            Console.WriteLine("Client connected");
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream);

            using var tokenSource = new CancellationTokenSource();

            var actionBlock = new ActionBlock<MessageEvent>(async messageEvent => { await _messageShipper.Deliver(writer, messageEvent); },
                                                            new ExecutionDataflowBlockOptions {
                                                                CancellationToken = tokenSource.Token
                                                            });
            try {
                while (true) {
                    var firstLine = reader.ReadLine();

                    var args = firstLine!.Split();

                    var command = args[0].ToUpper();
                    switch (command) {
                        case "DISCONNECT": {
                            reader.ReadLine();
                            throw new Exception();
                        }
                        case "PUB": {
                            const string contentLengthHeaderName = "content-length: ";

                            var topic               = args[1];
                            var contentLengthHeader = reader.ReadLine()!;

                            if (!contentLengthHeader.StartsWith(contentLengthHeaderName, StringComparison.OrdinalIgnoreCase))
                                continue;

                            reader.ReadLine();
                            var length  = int.Parse(contentLengthHeader.AsSpan()[contentLengthHeaderName.Length..]);
                            var message = string.Create(length, reader, static (span, streamReader) => streamReader.Read(span));
                            reader.ReadLine();
                            reader.ReadLine();
                            var messageEvent = new MessageEvent(topic, message);
                            Task.Run(() => _queueManager.Publish(messageEvent));

                            break;
                        }
                        case "SUB": {
                            var topic = args[1];
                            _queueManager.Subscribe(topic, actionBlock);
                            break;
                        }
                        case "UNSUB": {
                            var topic = args[1];
                            _queueManager.Unsubscribe(topic, actionBlock);
                            break;
                        }
                    }
                }
            } catch (Exception) {
                _queueManager.UnsubscribeFromAll(actionBlock);
                Console.WriteLine("Client disconnected");
            } finally {
                tokenSource.Cancel();
                tokenSource.Dispose();
                actionBlock.Complete();
                writer.Dispose();
                reader.Dispose();
                stream.Dispose();
                client.Dispose();
            }
        }
    }
}
