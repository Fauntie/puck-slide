using UnityEngine;

/// <summary>
/// Drives the NetworkInputSource each frame so that stale pointer
/// states are cleared when no new network input is received.
/// </summary>
public class NetworkInputDriver : MonoBehaviour
{
    private void Update()
    {
        if (InputSourceBootstrapper.Current is NetworkInputSource networkInput)
        {
            networkInput.Update();
        }
    }
}
