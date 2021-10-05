using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;

namespace MessageBroker.Server.Models {
    using Client = ActionBlock<MessageEvent>;

    public class Topic {
        public ConcurrentSet<Client>         Clients  { get; }
        public ConcurrentQueue<MessageEvent> Messages { get; }

        public Topic() {
            Clients  = new ConcurrentSet<Client>();
            Messages = new ConcurrentQueue<MessageEvent>();
        }
    }
}
