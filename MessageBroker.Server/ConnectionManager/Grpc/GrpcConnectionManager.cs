using Grpc.Core;
using MessageBroker.Grpc;
using MessageBroker.Server.ConnectionManager.Abstractions;
using Serilog;
using Serilog.Events;

namespace MessageBroker.Server.ConnectionManager.Grpc {
    public class GrpcConnectionManager : IConnectionManager {
        private readonly string        _host;
        private readonly ILogger       _logger;
        private readonly LogEventLevel _logLevel;
        private readonly int           _port;

        public GrpcConnectionManager(string host, int port, LogEventLevel logEventLevel = LogEventLevel.Information) {
            _host     = host;
            _port     = port;
            _logLevel = logEventLevel;
            _logger   = new LoggerConfiguration().MinimumLevel.Is(logEventLevel).WriteTo.Console().CreateLogger();
        }

        public void Start() {
            var server = new global::Grpc.Core.Server {
                Services = { MessageBrokerService.BindService(new MessageBrokerImpl(_logLevel)) },
                Ports    = { new ServerPort(_host, _port, ServerCredentials.Insecure) }
            };
            server.Start();
            _logger.Information("Grpc server listening on {Host}:{Port}", _host, _port);
        }
    }
}
