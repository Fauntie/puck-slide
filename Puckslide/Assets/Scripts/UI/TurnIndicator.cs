using TMPro;
using UnityEngine;

public class TurnIndicator : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro m_Text;

    [SerializeField]
    private Vector3 m_Offset = new Vector3(0f, 0f, -0.1f);

    private bool m_SkipFlip = true;

    private void Awake()
    {
        if (m_Text == null)
        {
            m_Text = GetComponent<TextMeshPro>();
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
    }

    private void OnDisable()
    {
        EventsManager.OnTurnChanged.RemoveListener(OnTurnChanged);
    }

    private void OnTurnChanged(bool isWhiteTurn)
    {
        if (m_Text != null)
        {
            m_Text.text = isWhiteTurn ? "White's turn" : "Black's turn";
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
}
