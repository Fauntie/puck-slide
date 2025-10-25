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
        EventsManager.OnTurnChanged.AddListener(OnTurnChanged, true);
    }

    private void OnDisable()
    {
        EventsManager.OnTurnChanged.RemoveListener(OnTurnChanged);
    }

    private void OnTurnChanged(bool isWhiteTurn)
    {
        UpdateText(isWhiteTurn);
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
