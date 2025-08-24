using TMPro;
using UnityEngine;

public class TurnIndicator : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro m_Text;

    [SerializeField]
    private Vector3 m_Offset = new Vector3(0f, 0f, -0.1f);
    private IEventBus m_EventBus;

    private void Awake()
    {
        if (m_Text == null)
        {
            m_Text = GetComponent<TextMeshPro>();
        }
        m_EventBus = FindObjectOfType<EventBusBootstrap>()?.Bus;
    }

    private void Start()
    {
        Transform board = BoardFlipper.GetBoardTransform();
        if (board != null)
        {
            Vector3 center = BoardFlipper.GetBoardCenter();
            transform.position = center + m_Offset;
            transform.SetParent(board, true);
        }
    }

    private void OnEnable()
    {
        m_EventBus?.Subscribe<bool>(EventBusEvents.TurnChanged, OnTurnChanged);
    }

    private void OnDisable()
    {
        m_EventBus?.Unsubscribe<bool>(EventBusEvents.TurnChanged, OnTurnChanged);
    }

    private void OnTurnChanged(bool isWhiteTurn)
    {
        if (m_Text != null)
        {
            m_Text.text = isWhiteTurn ? "White's turn" : "Black's turn";
        }
    }
}
