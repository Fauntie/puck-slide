using TMPro;
using UnityEngine;

public class TurnIndicator : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro m_Text;

    [SerializeField]
    private Color m_CheckColor = Color.red;
    private Color m_BaseColor;

    [SerializeField]
    private Vector3 m_Offset = new Vector3(0f, 0f, -0.1f);

    private bool m_SkipFlip = true;

    private void Awake()
    {
        if (m_Text == null)
        {
            m_Text = GetComponent<TextMeshPro>();
        }

        if (m_Text != null)
        {
            m_BaseColor = m_Text.color;
        }
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
        EventsManager.OnTurnChanged.AddListener(OnTurnChanged, true);
        EventsManager.OnCheck.AddListener(OnCheck, true);
    }

    private void OnDisable()
    {
        EventsManager.OnTurnChanged.RemoveListener(OnTurnChanged);
        EventsManager.OnCheck.RemoveListener(OnCheck);
    }

    private void OnTurnChanged(bool isWhiteTurn)
    {
        if (m_Text != null)
        {
            m_Text.text = isWhiteTurn ? "White's turn" : "Black's turn";
            m_Text.color = m_BaseColor;
        }

        if (m_SkipFlip)
        {
            m_SkipFlip = false;
            return;
        }

        if (Phase2Manager.IsPhase2Active)
        {
            BoardFlipper.FlipCamera();
        }
        else
        {
            BoardFlipper.Flip();
        }
    }

    private void OnCheck(int checkState)
    {
        if (m_Text == null)
        {
            return;
        }

        bool whiteTurn = EventsManager.IsWhiteTurn;
        if ((checkState == 1 && whiteTurn) || (checkState == -1 && !whiteTurn))
        {
            m_Text.color = m_CheckColor;
        }
        else
        {
            m_Text.color = m_BaseColor;
        }
    }
}
