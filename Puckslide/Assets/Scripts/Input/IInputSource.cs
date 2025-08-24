public interface IInputSource
{
    bool GetPointerDown();
    bool GetPointer();
    bool GetPointerUp();
    UnityEngine.Vector3 GetPointerPosition();
}
