using System;
using System.Collections.Generic;

public class LocalEventBus : IEventBus
{
    private class Event<T>
    {
        public event Action<T> Handlers = delegate { };
        public T Last;
        public void Publish(T msg)
        {
            Last = msg;
            Handlers.Invoke(msg);
        }
        public void Subscribe(Action<T> handler, bool receiveLast)
        {
            Handlers += handler;
            if (receiveLast)
            {
                handler(Last);
            }
        }
        public void Unsubscribe(Action<T> handler)
        {
            Handlers -= handler;
        }
    }

    private readonly Dictionary<string, object> m_Events = new Dictionary<string, object>();

    private Event<T> Get<T>(string name)
    {
        if (!m_Events.TryGetValue(name, out var evt))
        {
            evt = new Event<T>();
            m_Events[name] = evt;
        }
        return (Event<T>)evt;
    }

    public void Publish<T>(string eventName, T message)
    {
        Get<T>(eventName).Publish(message);
    }

    public void Subscribe<T>(string eventName, Action<T> handler, bool receiveLast = false)
    {
        Get<T>(eventName).Subscribe(handler, receiveLast);
    }

    public void Unsubscribe<T>(string eventName, Action<T> handler)
    {
        if (m_Events.TryGetValue(eventName, out var evt))
        {
            ((Event<T>)evt).Unsubscribe(handler);
        }
    }
}
