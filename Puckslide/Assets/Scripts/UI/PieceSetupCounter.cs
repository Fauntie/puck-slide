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
        m_MinusButton.onClick.AddListener(MinusPressed);
        m_PlusButton.onClick.AddListener(PlusPressed);
        m_GameSetupManager.CountsChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        m_MinusButton.onClick.RemoveListener(MinusPressed);
        m_PlusButton.onClick.RemoveListener(PlusPressed);
        m_GameSetupManager.CountsChanged -= RefreshUI;
    }

    private void MinusPressed()
    {
        bool changed = m_GameSetupManager.DecreaseCount(m_ChessPieceType, m_IsWhiteCounter);
        if (changed)
        {
            m_CurrentCount--;
        }

        RefreshUI();
    }

    private void PlusPressed()
    {
        bool changed = m_GameSetupManager.IncreaseCount(m_ChessPieceType, m_IsWhiteCounter);
        if (changed)
        {
            m_CurrentCount++;
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        m_CurrentCount = m_GameSetupManager.GetCount(m_ChessPieceType, m_IsWhiteCounter);
        m_TextMeshProUGUI.text = $"{m_CurrentCount}";
        bool canEdit = m_GameSetupManager != null && m_GameSetupManager.IsLocalHost;
        m_MinusButton.interactable = canEdit && m_GameSetupManager.CanDecrease(m_ChessPieceType, m_IsWhiteCounter);
        m_PlusButton.interactable = canEdit && m_GameSetupManager.CanIncrease(m_ChessPieceType, m_IsWhiteCounter);
    }
}
