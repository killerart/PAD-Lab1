using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using MessageBroker.MessageShipper.Abstractions;

namespace MessageBroker.MessageShipper {
    public class TcpMessageShipper : IMessageShipper<TcpClient> {
        public async Task Deliver(IEnumerable<TcpClient> clients, string topic, string message) {
            await Task.WhenAll(clients.Select(client => Deliver(client, topic, message)));
        }

        public async Task Deliver(TcpClient client, string topic, string message) {
            var stream = client.GetStream();
            var writer = new StreamWriter(stream);
            await writer.WriteLineAsync($"EVENT {topic}");
            await writer.WriteLineAsync($"Content-Length: {message.Length}");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync(message);
            await writer.WriteLineAsync();
            await writer.FlushAsync();

            Console.WriteLine($"Message sent to topic '{topic}'");
        }
    }
}
