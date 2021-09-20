using System;
using System.Text.Json;
using System.Threading.Tasks;
using MessageBroker.Client.Grpc;
using MessageBroker.Client.Socket;

namespace MessageBroker.TestClient {
    class Program {
        static async Task Main() {
            // using var messageBrokerClient = new SocketMessageBrokerClient("127.0.0.1", 9876);
            using var messageBrokerClient = new GrpcMessageBrokerClient("127.0.0.1", 9876);

            await messageBrokerClient.Subscribe<Message>(HandleMessageEvent);

            messageBrokerClient.StartListening();

            await messageBrokerClient.Publish(new Message {
                Id   = 5,
                Text = "Hello",
                Time = DateTime.Now
            });

            Console.ReadKey(true);

            await messageBrokerClient.Unsubscribe<Message>(HandleMessageEvent);

            await messageBrokerClient.Disconnect();
        }

        private static void HandleMessageEvent(object message) {
            Console.WriteLine(JsonSerializer.Serialize(message,
                                                       new JsonSerializerOptions {
                                                           WriteIndented = true
                                                       }));
        }
    }

    public class Message {
        public int      Id   { get; set; }
        public string?  Text { get; set; }
        public DateTime Time { get; set; }
    }
}
