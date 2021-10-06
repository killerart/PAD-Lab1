using System;
using System.Text.Json;
using System.Threading.Tasks;
using MessageBroker.Client.Grpc;
using MessageBroker.Tests.Common;

namespace MessageBroker.TestSubscriber {
    class Program {
        static async Task Main() {
            // using var client = new SocketMessageBrokerClient("127.0.0.1", 9876);
            using var client = new GrpcMessageBrokerClient("127.0.0.1", 9876);

            await client.Subscribe<Message>(HandleMessageEvent);
            client.StartListening();

            Console.ReadKey(true);

            await client.Unsubscribe<Message>(HandleMessageEvent);
            await client.Disconnect();
        }

        private static void HandleMessageEvent(object message) {
            Console.WriteLine(JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
