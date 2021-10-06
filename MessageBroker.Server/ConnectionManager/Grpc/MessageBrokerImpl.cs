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
using Serilog;
using Serilog.Events;

namespace MessageBroker.Server.ConnectionManager.Grpc {
    public class MessageBrokerImpl : MessageBrokerService.MessageBrokerServiceBase {
        private readonly ILogger                                        _logger;
        private readonly IMessageShipper<IServerStreamWriter<Response>> _messageShipper;
        private readonly IQueueManager                                  _queueManager;

        public MessageBrokerImpl(LogEventLevel logEventLevel = LogEventLevel.Information) {
            _queueManager   = new MemoryQueueManager(logEventLevel);
            _messageShipper = new GrpcMessageShipper();
            _logger         = new LoggerConfiguration().MinimumLevel.Is(logEventLevel).WriteTo.Console().CreateLogger();
        }

        public override async Task Connect(IAsyncStreamReader<Request>   requestStream,
                                           IServerStreamWriter<Response> responseStream,
                                           ServerCallContext             context) {
            _logger.Debug("Client connected");
            using var tokenSource = new CancellationTokenSource();
            var actionBlock = new ActionBlock<MessageEvent>(messageEvent => _messageShipper.Deliver(responseStream, messageEvent),
                                                            new ExecutionDataflowBlockOptions {
                                                                CancellationToken = tokenSource.Token
                                                            });
            await HandleConnection(requestStream, actionBlock);

            _queueManager.UnsubscribeFromAll(actionBlock);
            tokenSource.Cancel();
            tokenSource.Dispose();
            _logger.Debug("Client disconnected");
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
