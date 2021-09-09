﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MessageBroker.MessageShipper {
    public static class TcpMessageShipper {
        public static async Task Deliver(IEnumerable<TcpClient> clients, string topic, string message) {
            await Task.WhenAll(clients.Select(client => Deliver(client, topic, message)));
        }

        private static async Task Deliver(TcpClient client, string topic, string message) {
            var stream = client.GetStream();
            var writer = new StreamWriter(stream);
            await writer.WriteLineAsync(topic);
            await writer.WriteLineAsync($"Content-Length: {message.Length}\r\n");
            await writer.WriteLineAsync(message);
            await writer.FlushAsync();
        }
    }
}
