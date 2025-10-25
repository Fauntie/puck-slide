using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    private static bool s_ResetQueuedBeforeInit;

    [SerializeField]
    private bool m_IsWhiteTurn = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple TurnManager instances detected. Destroying the newest instance.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        EventsManager.OnPuckStopped.AddListener(OnPuckStopped);

        if (s_ResetQueuedBeforeInit)
        {
            s_ResetQueuedBeforeInit = false;
            InternalResetTurnOrder();
            return;
        }

        BroadcastTurn();
    }

    private void OnDisable()
    {
        EventsManager.OnPuckStopped.RemoveListener(OnPuckStopped);
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnPuckStopped(PuckStoppedEvent puckEvent)
    {
        if (Phase2Manager.IsPhase2Active && !puckEvent.HasReachedBoard)
        {
            return;
        }

        m_IsWhiteTurn = !m_IsWhiteTurn;

        if (Phase2Manager.IsPhase2Active)
        {
            BoardFlipper.FlipCamera();
        }
        else if (puckEvent.HasReachedBoard)
        {
            BoardFlipper.Flip();
        }

        BroadcastTurn();
    }

    public static void ResetTurnOrder()
    {
        if (Instance == null)
        {
            s_ResetQueuedBeforeInit = true;
            return;
        }

        Instance.InternalResetTurnOrder();
    }

    private void InternalResetTurnOrder()
    {
        m_IsWhiteTurn = true;
        BroadcastTurn();
    }

    private void BroadcastTurn()
    {
        EventsManager.OnTurnChanged.Invoke(m_IsWhiteTurn);
    }
}
