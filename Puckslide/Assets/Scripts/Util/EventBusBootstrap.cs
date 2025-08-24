using UnityEngine;

public class EventBusBootstrap : MonoBehaviour
{
    public IEventBus Bus { get; private set; }

    private void Awake()
    {
        Bus = new LocalEventBus();
    }
}
