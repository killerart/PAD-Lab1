using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MessageBroker.Client.Core;
using MessageBroker.Grpc;

namespace MessageBroker.Client.Grpc {
    public class GrpcMessageBrokerClient : MessageBrokerClientBase {
        private readonly AsyncDuplexStreamingCall<Request, Response> _stream;
        private readonly CancellationTokenSource                     _cancellationTokenSource;

        public GrpcMessageBrokerClient(string host, int port) {
            var grpcClient = new MessageBrokerService.MessageBrokerServiceClient(new Channel(host, port, ChannelCredentials.Insecure));
            _cancellationTokenSource = new CancellationTokenSource();
            _stream                  = grpcClient.Connect(cancellationToken: _cancellationTokenSource.Token);
        }

        public override void StartListening() {
            Task.Run(Listen, _cancellationTokenSource.Token);
        }

        private async Task Listen() {
            while (await _stream.ResponseStream.MoveNext()) {
                var response = _stream.ResponseStream.Current;
                HandleMessage(response.Topic, response.Message);
            }
        }

        public override async ValueTask Subscribe<T>(MessageBrokerEventHandler messageHandler) {
            var type  = typeof(T);
            var topic = type.Name;

            var createdSubInfo = false;
            var sub = Subscriptions.GetOrAdd(topic,
                                             _ => {
                                                 createdSubInfo = true;
                                                 return new SubscriptionInfo(type);
                                             });
            sub.AddEventHandler(messageHandler);

            if (createdSubInfo) {
                await _stream.RequestStream.WriteAsync(new Request { Command = "SUB", Topic = topic });
            }
        }

        protected override async ValueTask Unsubscribe(string topic) {
            if (!Subscriptions.TryRemove(topic, out _)) {
                return;
            }

            await _stream.RequestStream.WriteAsync(new Request { Command = "UNSUB", Topic = topic });
        }

        public override async ValueTask Publish<T>(T message) {
            var topic = typeof(T).Name;
            var json  = JsonSerializer.Serialize(message);

            await _stream.RequestStream.WriteAsync(new Request { Command = "PUB", Topic = topic, Message = json });
        }

        public override async ValueTask Disconnect() {
            await _stream.RequestStream.WriteAsync(new Request { Command = "DISCONNECT", Topic = "" });
            await _stream.RequestStream.CompleteAsync();

            ClearSubs();
        }

        protected override void Dispose(bool disposing) {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _stream.Dispose();
        }
    }
}
