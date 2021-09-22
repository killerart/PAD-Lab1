namespace MessageBroker.Server.Models {
    public class MessageEvent {
        public MessageEvent(string topic, string message) {
            Topic   = topic;
            Message = message;
        }

        public string Topic   { get; }
        public string Message { get; }
    }
}
