using System;
using System.Text.Json;
using System.Threading.Tasks;
using MessageBroker.Client;
using MessageBroker.Client.Abstractions;

namespace MessageBroker.TestClient {
    class Program {
        static async Task Main() {
            // using var messageBrokerClient = new SocketMessageBrokerClient("127.0.0.1", 9876);
            using var messageBrokerClient = new GrpcMessageBrokerClient("127.0.0.1", 9876);
            
            await messageBrokerClient.Subscribe(new MessageHandler());

            messageBrokerClient.StartListening();

            await messageBrokerClient.Publish(new Message {
                Id   = 5,
                Text = "Hello",
                Time = DateTime.Now
            });

            Console.ReadKey(true);

            await messageBrokerClient.Unsubscribe<Message>();

            await messageBrokerClient.Disconnect();
        }
    }

    public class Message {
        public int      Id   { get; set; }
        public string?  Text { get; set; }
        public DateTime Time { get; set; }
    }

    public class MessageHandler : IMessageHandler<Message> {
        public ValueTask HandleAsync(Message message) {
            Console.WriteLine(JsonSerializer.Serialize(message,
                                                       new JsonSerializerOptions {
                                                           WriteIndented = true
                                                       }));
            return ValueTask.CompletedTask;
        }
    }
}
