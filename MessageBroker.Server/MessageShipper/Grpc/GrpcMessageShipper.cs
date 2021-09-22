using System;
using System.Threading.Tasks;
using Grpc.Core;
using MessageBroker.Grpc;
using MessageBroker.Server.MessageShipper.Abstractions;
using MessageBroker.Server.Models;

namespace MessageBroker.Server.MessageShipper.Grpc {
    public class GrpcMessageShipper : IMessageShipper<IServerStreamWriter<Response>> {
        public async Task Deliver(IServerStreamWriter<Response> client, MessageEvent messageEvent) {
            try {
                await client.WriteAsync(new Response { Topic = messageEvent.Topic, Message = messageEvent.Message });
                // Console.WriteLine($"Message sent to topic '{messageEvent.Topic}'");
            } catch (Exception) {
                // ignored
            }
        }
    }
}
