using UnityEngine;

public class NetworkInputSource : IInputSource
{
    private bool m_PointerDown;
    private bool m_Pointer;
    private bool m_PointerUp;
    private Vector3 m_Position;
    private bool m_InputThisFrame;

    public void FeedInput(Vector3 position, bool pointerDown, bool pointer, bool pointerUp)
    {
        m_Position = position;
        m_PointerDown = pointerDown;
        m_Pointer = pointer;
        m_PointerUp = pointerUp;
        m_InputThisFrame = true;
    }

    public bool GetPointerDown()
    {
        bool down = m_PointerDown;
        m_PointerDown = false;
        return down;
    }

    public bool GetPointer()
    {
        bool pointer = m_Pointer;
        m_Pointer = false;
        return pointer;
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

    /// <summary>
    /// Should be called once per frame to clear the pointer state when no new input arrives.
    /// </summary>
    public void Update()
    {
        if (!m_InputThisFrame)
        {
            m_Pointer = false;
            m_PointerDown = false;
            m_PointerUp = false;
        }
        m_InputThisFrame = false;
    }
}
