using UnityEngine;

public class NetworkInputSource : IInputSource
{
    private bool m_PointerDown;
    private bool m_Pointer;
    private bool m_PointerUp;
    private Vector3 m_Position;

    public void FeedInput(Vector3 position, bool pointerDown, bool pointer, bool pointerUp)
    {
        m_Position = position;
        m_PointerDown = pointerDown;
        m_Pointer = pointer;
        m_PointerUp = pointerUp;
    }

    public bool GetPointerDown()
    {
        bool down = m_PointerDown;
        m_PointerDown = false;
        return down;
    }

    public bool GetPointer()
    {
        return m_Pointer;
    }

    public bool GetPointerUp()
    {
        bool up = m_PointerUp;
        m_PointerUp = false;
        return up;
    }

    public Vector3 GetPointerPosition()
    {
        return m_Position;
    }
}
