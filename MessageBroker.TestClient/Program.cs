using System;
using System.Text.Json;
using System.Threading.Tasks;
using MessageBroker.Client.Core;
using MessageBroker.Client.Grpc;
using MessageBroker.Client.Socket;

namespace MessageBroker.TestClient {
    class Program {
        static async Task Main() {
            var clients = new IMessageBrokerClient[8];
            for (var i = 0; i < clients.Length; i++) {
                clients[i] = new SocketMessageBrokerClient("127.0.0.1", 9876);
                // clients[i] = new GrpcMessageBrokerClient("127.0.0.1", 9876);
                await clients[i].Subscribe<Message>(HandleMessageEvent);
                clients[i].StartListening();
                await clients[i]
                    .Publish(new Message {
                        Id   = 5,
                        Text = "Hello",
                        Time = DateTime.Now
                    });
            }

            // using var client = new SocketMessageBrokerClient("127.0.0.1", 9876);
            // // using var client = new GrpcMessageBrokerClient("127.0.0.1", 9876);
            //
            // await client.Subscribe<Message>(HandleMessageEvent);
            // client.StartListening();
            //
            // await client.Publish(new Message {
            //     Id   = 5,
            //     Text = "Hello",
            //     Time = DateTime.Now
            // });

            Console.ReadKey(true);
            foreach (var client in clients) {
                await client.Unsubscribe<Message>(HandleMessageEvent);

                await client.Disconnect();
                client.Dispose();
            }
        }

        private static void HandleMessageEvent(object message) {
            // Console.WriteLine(JsonSerializer.Serialize(message,
            //                                            new JsonSerializerOptions {
            //                                                WriteIndented = true
            //                                            }));
        }
    }

    public class Message {
        public int      Id   { get; set; }
        public string?  Text { get; set; }
        public DateTime Time { get; set; }
    }
}
