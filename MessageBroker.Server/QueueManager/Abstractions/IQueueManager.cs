using System.Threading.Tasks.Dataflow;
using MessageBroker.Server.Models;

namespace MessageBroker.Server.QueueManager.Abstractions {
    public interface IQueueManager {
        void Publish(MessageEvent messageEvent);

        void Subscribe(string topicName, ActionBlock<MessageEvent> client);

        void Unsubscribe(string topicName, ActionBlock<MessageEvent> client);

        void UnsubscribeFromAll(ActionBlock<MessageEvent> client);
    }
}
