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
    private TextMeshProUGUI m_FeedbackText;

    [SerializeField]
    private bool m_IsWhiteCounter;

    private int m_CurrentCount = 0;
    private DeniedAction m_LastDeniedAction = DeniedAction.None;

    private enum DeniedAction
    {
        None,
        Increase,
        Decrease
    }

    private void OnEnable()
    {
        m_MinusButton.onClick.AddListener(MinusPressed);
        m_PlusButton.onClick.AddListener(PlusPressed);
        m_GameSetupManager.CountsChanged += RefreshUI;
        DisplayFeedback(string.Empty);
        RefreshUI();
    }

    private void OnDisable()
    {
        m_MinusButton.onClick.RemoveListener(MinusPressed);
        m_PlusButton.onClick.RemoveListener(PlusPressed);
        m_GameSetupManager.CountsChanged -= RefreshUI;
        m_LastDeniedAction = DeniedAction.None;
        DisplayFeedback(string.Empty);
    }

    private void MinusPressed()
    {
        bool changed = m_GameSetupManager.DecreaseCount(m_ChessPieceType, m_IsWhiteCounter, out string errorMessage);
        if (changed)
        {
            m_CurrentCount--;
            m_LastDeniedAction = DeniedAction.None;
            DisplayFeedback(string.Empty);
        }
        else
        {
            m_LastDeniedAction = DeniedAction.Decrease;
            DisplayFeedback(errorMessage);
        }

        RefreshUI();
    }

    private void PlusPressed()
    {
        bool changed = m_GameSetupManager.IncreaseCount(m_ChessPieceType, m_IsWhiteCounter, out string errorMessage);
        if (changed)
        {
            m_CurrentCount++;
            m_LastDeniedAction = DeniedAction.None;
            DisplayFeedback(string.Empty);
        }
        else
        {
            m_LastDeniedAction = DeniedAction.Increase;
            DisplayFeedback(errorMessage);
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        m_CurrentCount = m_GameSetupManager.GetCount(m_ChessPieceType, m_IsWhiteCounter);
        m_TextMeshProUGUI.text = $"{m_CurrentCount}";
        m_MinusButton.interactable = m_GameSetupManager.CanDecrease(m_ChessPieceType, m_IsWhiteCounter);
        m_PlusButton.interactable = m_GameSetupManager.CanIncrease(m_ChessPieceType, m_IsWhiteCounter);

        if (m_LastDeniedAction == DeniedAction.Increase && m_PlusButton.interactable)
        {
            m_LastDeniedAction = DeniedAction.None;
            DisplayFeedback(string.Empty);
        }
        else if (m_LastDeniedAction == DeniedAction.Decrease && m_MinusButton.interactable)
        {
            m_LastDeniedAction = DeniedAction.None;
            DisplayFeedback(string.Empty);
        }
    }

    private void DisplayFeedback(string message)
    {
        if (m_FeedbackText == null)
        {
            return;
        }

        bool hasMessage = !string.IsNullOrEmpty(message);
        m_FeedbackText.gameObject.SetActive(hasMessage);
        m_FeedbackText.text = hasMessage ? message : string.Empty;
    }
}
