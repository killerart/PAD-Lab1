using System.Collections.Generic;
using System.Threading.Tasks;

namespace MessageBroker.Server.MessageShipper.Abstractions {
    public interface IMessageShipper<in T> {
        Task Deliver(IEnumerable<T> clients, string topic, string message);
        Task Deliver(T              client,  string topic, string message);
    }
}
