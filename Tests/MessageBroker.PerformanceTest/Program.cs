using System;
using System.Threading.Tasks;
using MessageBroker.Client.Grpc;
using MessageBroker.Client.Socket;
using MessageBroker.Tests.Common;
using NBomber.Contracts;
using NBomber.CSharp;

namespace MessageBroker.PerformanceTest {
    class Program {
        private const int ClientCount = 16;

        static void Main(string[] args) {
            // TestSocket();
            TestGrpc();
        }

        private static void TestSocket() {
            var factory = ClientFactory.Create(name: "socket_factory",
                                               clientCount: ClientCount + 2,
                                               initClient: (_, _) => Task.FromResult(new SocketMessageBrokerClient("localhost", 9876)));

            var message = new Message {
                Id   = 5,
                Text = "Hello",
                Time = DateTime.Now
            };

            var step = Step.Create("step",
                                   clientFactory: factory,
                                   timeout: TimeSpan.FromSeconds(20),
                                   execute: async context => {
                                       var messageBrokerClient = context.Client;
                                       await messageBrokerClient.Publish(message);

                                       return Response.Ok();
                                   });

            var scenario = ScenarioBuilder.CreateScenario("socket_test", step)
                                          .WithWarmUpDuration(TimeSpan.FromSeconds(5))
                                          .WithLoadSimulations(Simulation.KeepConstant(ClientCount, TimeSpan.FromSeconds(60)));

            NBomberRunner.RegisterScenarios(scenario).Run();
        }

        private static void TestGrpc() {
            var factory = ClientFactory.Create(name: "grpc_factory",
                                               clientCount: ClientCount + 2,
                                               initClient: (_, _) => Task.FromResult(new GrpcMessageBrokerClient("localhost", 9876)));
            var message = new Message {
                Id   = 5,
                Text = "Hello",
                Time = DateTime.Now
            };

            var step = Step.Create("step",
                                   clientFactory: factory,
                                   timeout: TimeSpan.FromSeconds(20),
                                   execute: async context => {
                                       var messageBrokerClient = context.Client;
                                       await messageBrokerClient.Publish(message);

                                       return Response.Ok();
                                   });

            var scenario = ScenarioBuilder.CreateScenario("grpc_test", step)
                                          .WithWarmUpDuration(TimeSpan.FromSeconds(5))
                                          .WithLoadSimulations(Simulation.KeepConstant(ClientCount, TimeSpan.FromSeconds(60)));

            NBomberRunner.RegisterScenarios(scenario).Run();
        }
    }
}
