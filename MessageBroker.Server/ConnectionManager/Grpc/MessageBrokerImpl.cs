using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Grpc.Core;
using MessageBroker.Grpc;
using MessageBroker.Server.MessageShipper.Abstractions;
using MessageBroker.Server.MessageShipper.Grpc;
using MessageBroker.Server.Models;
using MessageBroker.Server.QueueManager;
using MessageBroker.Server.QueueManager.Abstractions;

namespace MessageBroker.Server.ConnectionManager.Grpc {
    public class MessageBrokerImpl : MessageBrokerService.MessageBrokerServiceBase {
        private readonly IQueueManager _queueManager;

        private readonly IMessageShipper<IServerStreamWriter<Response>> _messageShipper;

        public MessageBrokerImpl() {
            _queueManager   = new MemoryQueueManager();
            _messageShipper = new GrpcMessageShipper();
        }

        public override async Task Connect(IAsyncStreamReader<Request>   requestStream,
                                           IServerStreamWriter<Response> responseStream,
                                           ServerCallContext             context) {
            Console.WriteLine("Client connected");
            using var tokenSource = new CancellationTokenSource();
            var actionBlock = new ActionBlock<MessageEvent>(messageEvent => _messageShipper.Deliver(responseStream, messageEvent),
                                                            new ExecutionDataflowBlockOptions {
                                                                CancellationToken = tokenSource.Token
                                                            });
            await HandleConnection(requestStream, actionBlock);

            _queueManager.UnsubscribeFromAll(actionBlock);
            tokenSource.Cancel();
            tokenSource.Dispose();
            Console.WriteLine("Client disconnected");
        }

        private async Task HandleConnection(IAsyncStreamReader<Request> requestStream, ActionBlock<MessageEvent> actionBlock) {
            while (await requestStream.MoveNext()) {
                var request = requestStream.Current;
                switch (request.Command.ToUpper()) {
                    case "PUB": {
                        var messageEvent = new MessageEvent(request.Topic, request.Message);
                        Task.Run(() => _queueManager.Publish(messageEvent));
                        break;
                    }
                    case "SUB": {
                        _queueManager.Subscribe(request.Topic, actionBlock);
                        break;
                    }
                    case "UNSUB": {
                        _queueManager.Unsubscribe(request.Topic, actionBlock);
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
