using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PieceSetupCounter : MonoBehaviour
{
    [SerializeField]
    private GameSetupManager m_GameSetupManager;
    [SerializeField]
    private ChessPieceType m_ChessPieceType;

    [SerializeField]
    private Button m_MinusButton;
    [SerializeField]
    private Button m_PlusButton;

    [SerializeField]
    private TextMeshProUGUI m_TextMeshProUGUI;

    [SerializeField]
    private bool m_IsWhiteCounter;

    private int m_CurrentCount = 0;

    private void OnEnable()
    {
        m_CurrentCount = m_GameSetupManager.GetCount(m_ChessPieceType, m_IsWhiteCounter);
        m_TextMeshProUGUI.text = $"{m_CurrentCount}";
        m_MinusButton.onClick.AddListener(MinusPressed);
        m_PlusButton.onClick.AddListener(PlusPressed);
    }

    private void OnDisable()
    {
        m_MinusButton.onClick.RemoveListener(MinusPressed);
        m_PlusButton.onClick.RemoveListener(PlusPressed);
    }

    private void MinusPressed()
    {
        if (m_CurrentCount == 0)
        {
            return;
        }
        
        m_GameSetupManager.DecreaseCount(m_ChessPieceType, m_IsWhiteCounter);
        m_CurrentCount--;
        m_TextMeshProUGUI.text = $"{m_CurrentCount}";
    }
    
    private void PlusPressed()
    {
        if (m_IsWhiteCounter ? m_GameSetupManager.WithinWhiteCount() : m_GameSetupManager.WithinBlackCount())
        {
            m_GameSetupManager.IncreaseCount(m_ChessPieceType, m_IsWhiteCounter);
            m_CurrentCount++;
            m_TextMeshProUGUI.text = $"{m_CurrentCount}";
        }
    }
}
