using System;
using System.Threading.Tasks;
using Grpc.Core;
using MessageBroker.Grpc;
using MessageBroker.Server.MessageShipper.Grpc;
using MessageBroker.Server.QueueManager;
using MessageBroker.Server.QueueManager.Abstractions;

namespace MessageBroker.Server.ConnectionManager.Grpc {
    public class MessageBrokerImpl : MessageBrokerService.MessageBrokerServiceBase {
        private readonly IQueueManager<IServerStreamWriter<Response>> _queueManager;

        public MessageBrokerImpl() {
            _queueManager = new MemoryQueueManager<IServerStreamWriter<Response>>(new GrpcMessageShipper());
        }

        public override async Task Connect(IAsyncStreamReader<Request>   requestStream,
                                           IServerStreamWriter<Response> responseStream,
                                           ServerCallContext             context) {
            Console.WriteLine("Client connected");
            await HandleConnection(requestStream, responseStream);
            
            _queueManager.UnsubscribeFromAll(responseStream);
            Console.WriteLine("Client disconnected");
        }

        private async Task HandleConnection(IAsyncStreamReader<Request> requestStream, IServerStreamWriter<Response> responseStream) {
            while (await requestStream.MoveNext()) {
                var request = requestStream.Current;
                switch (request.Command.ToUpper()) {
                    case "PUB": {
                        _queueManager.Publish(request.Topic, request.Message);
                        break;
                    }
                    case "SUB": {
                        _queueManager.Subscribe(request.Topic, responseStream);
                        break;
                    }
                    case "UNSUB": {
                        _queueManager.Unsubscribe(request.Topic, responseStream);
                        break;
                    }
                    case "DISCONNECT": {
                        return;
                    }
                }
            }
        }
    }
}
