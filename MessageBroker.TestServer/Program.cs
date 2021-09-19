﻿using System.Threading.Tasks;
using MessageBroker.ConnectionManager;
using MessageBroker.ConnectionManager.Grpc;

namespace MessageBroker.TestServer {
    class Program {
        static async Task Main() {
            // var connectionManager = new SocketConnectionManager(9876);
            var connectionManager = new GrpcConnectionManager("localhost", 9876);
            connectionManager.Start();
            var waitForStop = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            await waitForStop.Task;
        }
    }
}
