﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using MessageBroker.Grpc;
using MessageBroker.MessageShipper.Abstractions;

namespace MessageBroker.MessageShipper {
    public class GrpcMessageShipper : IMessageShipper<IServerStreamWriter<Response>> {
        public async Task Deliver(IEnumerable<IServerStreamWriter<Response>> clients, string topic, string message) {
            await Task.WhenAll(clients.Select(client => Deliver(client, topic, message)));
        }

        public async Task Deliver(IServerStreamWriter<Response> client, string topic, string message) {
            await client.WriteAsync(new Response { Topic = topic, Message = message });
        }
    }
}
