using System.Threading.Tasks;
using MessageBroker.Server.ConnectionManager.Grpc;
using Serilog;
using Serilog.Events;

namespace MessageBroker.TestServer {
    class Program {
        static async Task Main() {
            // var connectionManager = new SocketConnectionManager("127.0.0.1", 9876, LogEventLevel.Debug);
            var connectionManager = new GrpcConnectionManager("127.0.0.1", 9876, LogEventLevel.Debug);
            connectionManager.Start();

            var waitForStop = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            await waitForStop.Task;

            Log.CloseAndFlush();
        }
    }
}
