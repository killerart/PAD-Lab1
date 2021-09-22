using System.Collections.Concurrent;

namespace MessageBroker.Server.Models {
    public class ConcurrentSet<T> : ConcurrentDictionary<T, T> where T : notnull { }
}
