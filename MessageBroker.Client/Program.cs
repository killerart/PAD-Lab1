using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace MessageBroker.Client {
    class Program {
        static async Task Main() {
            using var       client = new TcpClient("127.0.0.1", 9876);
            await using var stream = client.GetStream();
            using var       reader = new StreamReader(stream);
            await using var writer = new StreamWriter(stream);

            var obj = new {
                lol      = "kek",
                cheburek = 50
            };
            var json = JsonSerializer.Serialize(obj,
                                                new JsonSerializerOptions {
                                                    WriteIndented = true
                                                });
            await writer.WriteLineAsync("SUB lol");
            await writer.FlushAsync();

            await writer.WriteLineAsync("PUB lol");
            await writer.WriteLineAsync($"Content-Length: {json.Length}");
            await writer.WriteLineAsync();
            await writer.WriteAsync($"{json}");
            await writer.FlushAsync();

            var topic               = await reader.ReadLineAsync();
            var contentLengthHeader = (await reader.ReadLineAsync())!.ToLower();
            await reader.ReadLineAsync();
            var length              = int.Parse(contentLengthHeader.Split("content-length: ", StringSplitOptions.RemoveEmptyEntries)[0]);
            var charArray           = new char[length];
            await reader.ReadAsync(charArray);
            await reader.ReadLineAsync();
            var message = new string(charArray);
            Console.WriteLine(message);
        }
    }
}
