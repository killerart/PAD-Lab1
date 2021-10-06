using System;

namespace MessageBroker.Tests.Common {
    public class Message {
        public int      Id   { get; set; }
        public string?  Text { get; set; }
        public DateTime Time { get; set; }
    }
}
