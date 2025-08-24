using UnityEngine;

public class InputSourceBootstrapper : MonoBehaviour
{
    public enum SourceType
    {
        Mouse,
        Network
    }

    [SerializeField]
    private SourceType m_SourceType = SourceType.Mouse;

    public static IInputSource Current { get; private set; }

    private void Awake()
    {
        switch (m_SourceType)
        {
            case SourceType.Network:
                Current = new NetworkInputSource();
                break;
            default:
                Current = new MouseInputSource();
                break;
        }
    }
}
