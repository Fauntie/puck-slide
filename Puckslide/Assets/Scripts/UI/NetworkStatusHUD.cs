using UnityEngine;
using UnityEngine.UI;

public class NetworkStatusHUD : MonoBehaviour
{
    [SerializeField]
    private Text m_PingLabel;
    [SerializeField]
    private Text m_PacketLossLabel;
    [SerializeField]
    private Text m_RollbackLabel;
    [SerializeField]
    private GameObject m_RollbackIndicator;
    [SerializeField]
    private GameObject m_ReconnectPanel;
    [SerializeField]
    private Text m_ReconnectText;
    [SerializeField]
    private Button m_ReconnectButton;

    private NetworkDiagnostics m_Diagnostics;
    private ResilientSessionManager<string> m_SessionManager;
    private LobbyLocalizationStrings m_Localization;
    private int m_LastRollbackCount;

    public void Bind(NetworkDiagnostics diagnostics, ResilientSessionManager<string> sessionManager, LobbyLocalizationStrings localization)
    {
        m_Diagnostics = diagnostics;
        m_SessionManager = sessionManager;
        m_Localization = localization;

        if (m_ReconnectButton != null)
        {
            m_ReconnectButton.onClick.AddListener(OnReconnectClicked);
        }
    }

    public void Tick()
    {
        if (m_Diagnostics != null)
        {
            NetworkMetricsSnapshot snapshot = m_Diagnostics.GetSnapshot();
            if (m_PingLabel != null)
            {
                m_PingLabel.text = string.Format(m_Localization.PingFormat, Mathf.RoundToInt((float)(snapshot.AverageTickLatencySeconds * 1000.0)));
            }

            if (m_PacketLossLabel != null)
            {
                m_PacketLossLabel.text = string.Format(m_Localization.PacketLossFormat, snapshot.PacketLossCount);
            }

            if (m_RollbackLabel != null)
            {
                m_RollbackLabel.text = string.Format(m_Localization.RollbackFormat, snapshot.RollbackCount);
            }

            if (snapshot.RollbackCount > m_LastRollbackCount && m_RollbackIndicator != null)
            {
                m_RollbackIndicator.SetActive(true);
            }

            m_LastRollbackCount = snapshot.RollbackCount;
        }
    }

    public void UpdateSessionStatus(ResilientSessionStatus status, string message)
    {
        if (m_ReconnectPanel == null)
        {
            return;
        }

        bool show = status == ResilientSessionStatus.Reconnecting || status == ResilientSessionStatus.Failed;
        m_ReconnectPanel.SetActive(show);
        if (show && m_ReconnectText != null)
        {
            string fallback = status == ResilientSessionStatus.Reconnecting ? m_Localization.ReconnectingPrompt : m_Localization.ReconnectFailedPrompt;
            m_ReconnectText.text = string.IsNullOrEmpty(message) ? fallback : message;
        }
    }

    private void OnReconnectClicked()
    {
        m_SessionManager?.Resume();
    }
}
