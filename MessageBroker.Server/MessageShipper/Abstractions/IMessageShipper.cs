using System.Threading.Tasks;
using MessageBroker.Server.Models;

namespace MessageBroker.Server.MessageShipper.Abstractions {
    public interface IMessageShipper<in T> {
        Task Deliver(T client, MessageEvent messageEvent);
    }
}
