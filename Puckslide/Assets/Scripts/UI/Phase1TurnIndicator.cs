using Puckslide.Networking;
using TMPro;
using UnityEngine;

public class Phase1TurnIndicator : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI m_Text;

    private void Awake()
    {
        if (m_Text == null)
        {
            m_Text = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        UpdateText(PuckController.IsWhiteTurn);
        NetworkEvents.OnTurnChanged.AddListener(OnTurnChanged, true);
    }

    private void OnDisable()
    {
        NetworkEvents.OnTurnChanged.RemoveListener(OnTurnChanged);
    }

    private void OnTurnChanged(TurnChangeMessage message)
    {
        UpdateText(message.IsWhiteTurn);
    }

    private void UpdateText(bool isWhiteTurn)
    {
        if (m_Text == null)
        {
            return;
        }

        m_Text.text = isWhiteTurn ? "White's Turn" : "Black's Turn";
    }
}
