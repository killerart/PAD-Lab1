using System.Threading.Tasks;

namespace MessageBroker.Client.Abstractions {
    public interface IMessageHandler { }

    public interface IMessageHandler<in T> : IMessageHandler {
        ValueTask HandleAsync(T message);
    }
}
