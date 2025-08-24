using UnityEngine;

public class MouseInputSource : IInputSource
{
    public bool GetPointerDown()
    {
        return Input.GetMouseButtonDown(0);
    }

    public bool GetPointer()
    {
        return Input.GetMouseButton(0);
    }

    public bool GetPointerUp()
    {
        return Input.GetMouseButtonUp(0);
    }

    public Vector3 GetPointerPosition()
    {
        return Input.mousePosition;
    }
}
