using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using MessageBroker.Client.Abstractions;
using MessageBroker.Client.Models;
using MessageBroker.Grpc;

namespace MessageBroker.Client {
    public class GrpcMessageBrokerClient : MessageBrokerClientBase, IDisposable {
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
                await HandleMessage(response.Topic, response.Message);
            }
        }

        public override async ValueTask Subscribe<T>(IMessageHandler<T> messageHandler) {
            var type  = typeof(T);
            var sub   = new SubscriptionInfo(type, messageHandler);
            var topic = type.Name;

            if (!Subscriptions.TryAdd(topic, sub))
                return;

            await _stream.RequestStream.WriteAsync(new Request { Command = "SUB", Topic = topic });
        }

        public override async ValueTask Unsubscribe<T>() {
            var topic = typeof(T).Name;
            await Unsubscribe(topic);
        }

        private async ValueTask Unsubscribe(string topic) {
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
            Subscriptions.Clear();

            await _stream.RequestStream.WriteAsync(new Request { Command = "DISCONNECT", Topic = "" });
            await _stream.RequestStream.CompleteAsync();
        }

        public void Dispose() {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _stream.Dispose();
        }
    }
}
