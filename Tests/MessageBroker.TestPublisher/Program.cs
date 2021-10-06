using System;
using System.Threading.Tasks;
using MessageBroker.Client.Grpc;
using MessageBroker.Tests.Common;

namespace MessageBroker.TestPublisher {
    class Program {
        static async Task Main(string[] args) {
            // using var client = new SocketMessageBrokerClient("127.0.0.1", 9876);
            using var client = new GrpcMessageBrokerClient("127.0.0.1", 9876);
            await client.Publish(new Message {
                Id   = 5,
                Text = "Hello",
                Time = DateTime.Now
            });
            await client.Disconnect();
        }
    }
}
