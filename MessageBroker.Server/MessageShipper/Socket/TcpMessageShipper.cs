using System;
using System.IO;
using System.Threading.Tasks;
using MessageBroker.Server.MessageShipper.Abstractions;
using MessageBroker.Server.Models;

namespace MessageBroker.Server.MessageShipper.Socket {
    public class TcpMessageShipper : IMessageShipper<StreamWriter> {
        public async Task Deliver(StreamWriter client, MessageEvent messageEvent) {
            try {
                await client.WriteLineAsync($"EVENT {messageEvent.Topic}");
                await client.WriteLineAsync($"Content-Length: {messageEvent.Message.Length}");
                await client.WriteLineAsync();
                await client.WriteLineAsync(messageEvent.Message);
                await client.WriteLineAsync();
                await client.FlushAsync();

                // Console.WriteLine($"Message sent to topic '{messageEvent.Topic}'");
            } catch (Exception) {
                // ignored
            }
        }
    }
}
