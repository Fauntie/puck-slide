using System;

public class Evt<T>
{
    private event Action<T> m_Action = delegate { };
    private T m_LastValue;

    public Evt(T defaultValue = default)
    {
        m_LastValue = defaultValue;
    }

    public void Invoke(T param)
    {
        m_LastValue = param;
        m_Action.Invoke(param);
    }

    public void AddListener(Action<T> listener, bool receiveLastValue = false)
    {
        m_Action += listener;
        if (receiveLastValue)
        {
            listener(m_LastValue);
        }
    }

    public void RemoveListener(Action<T> listener)
    {
        m_Action -= listener;
    }
}
