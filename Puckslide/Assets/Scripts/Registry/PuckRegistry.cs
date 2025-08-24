using System.Collections.Generic;
using UnityEngine;

public class PuckRegistry : MonoBehaviour
{
    public static PuckRegistry Instance { get; private set; }

    private readonly List<PuckController> m_Pucks = new List<PuckController>();
    private IEventBus m_EventBus;

    public PuckController[] GetPucks()
    {
        return m_Pucks.ToArray();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        m_EventBus = FindObjectOfType<EventBusBootstrap>()?.Bus;
    }

    private void OnEnable()
    {
        m_EventBus?.Subscribe<Rigidbody2D>(EventBusEvents.PuckSpawned, OnPuckSpawned);
        m_EventBus?.Subscribe<Rigidbody2D>(EventBusEvents.PuckDespawned, OnPuckDespawned);
    }

    private void OnDisable()
    {
        m_EventBus?.Unsubscribe<Rigidbody2D>(EventBusEvents.PuckSpawned, OnPuckSpawned);
        m_EventBus?.Unsubscribe<Rigidbody2D>(EventBusEvents.PuckDespawned, OnPuckDespawned);
        m_Pucks.Clear();
    }

    private void OnPuckSpawned(Rigidbody2D rb)
    {
        var puck = rb != null ? rb.GetComponent<PuckController>() : null;
        if (puck != null && !m_Pucks.Contains(puck))
        {
            m_Pucks.Add(puck);
        }
    }

    private void OnPuckDespawned(Rigidbody2D rb)
    {
        var puck = rb != null ? rb.GetComponent<PuckController>() : null;
        if (puck != null)
        {
            m_Pucks.Remove(puck);
        }
    }
}
