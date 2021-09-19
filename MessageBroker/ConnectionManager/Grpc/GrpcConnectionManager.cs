using System;
using Grpc.Core;
using MessageBroker.ConnectionManager.Abstractions;
using MessageBroker.Grpc;

namespace MessageBroker.ConnectionManager.Grpc {
    public class GrpcConnectionManager : IConnectionManager {
        private readonly string _host;
        private readonly int    _port;

        public GrpcConnectionManager(string host, int port) {
            _host = host;
            _port = port;
        }

        public void Start() {
            var server = new Server {
                Services = { MessageBrokerService.BindService(new MessageBrokerImpl()) },
                Ports    = { new ServerPort(_host, _port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine($"Grpc server listening on {_host}:{_port}");
        }
    }
}
