using System;

public interface IEventBus
{
    void Publish<T>(string eventName, T message);
    void Subscribe<T>(string eventName, Action<T> handler, bool receiveLast = false);
    void Unsubscribe<T>(string eventName, Action<T> handler);
}
