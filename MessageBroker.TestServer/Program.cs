using System.Threading.Tasks;
using MessageBroker.ConnectionManager;
using MessageBroker.ConnectionManager.Abstractions;

namespace MessageBroker.TestServer {
    class Program {
        static async Task Main() {
            IConnectionManager connectionManager = new SocketConnectionManager(9876);

            connectionManager.Start();
            var waitForStop = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            await waitForStop.Task;
        }
    }
}
