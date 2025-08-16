using TMPro;
using UnityEngine;

public class TurnIndicator : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI m_Text;

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
    }
}
