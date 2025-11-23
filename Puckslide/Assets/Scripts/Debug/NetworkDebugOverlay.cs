using UnityEngine;
using TMPro;
using Puckslide;
using Puckslide.Networking;

public class NetworkDebugOverlay : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_DebugPanel;
    [SerializeField] private TMP_Text m_Text;
    [SerializeField] private KeyCode m_ToggleKey = KeyCode.F1;

    [SerializeField] private bool m_StartVisibleInEditor = true;

    private bool m_Visible;
    private bool m_OverlayAllowed = true;

    private void Start()
    {
#if UNITY_EDITOR
        m_Visible = m_StartVisibleInEditor;
#else
        BuildConfig config = Resources.Load<BuildConfig>("BuildConfig");
        m_OverlayAllowed = config == null || config.EnableDebugOverlayInRelease;
        m_Visible = false;
#endif
        ApplyVisibility();
    }

    private void Update()
    {
        if (!m_OverlayAllowed)
        {
            return;
        }

        if (Input.GetKeyDown(m_ToggleKey))
        {
            m_Visible = !m_Visible;
            ApplyVisibility();
        }

        if (!m_Visible || m_Text == null)
            return;

        var nsm = NetworkSessionManager.Instance;
        if (nsm == null)
        {
            m_Text.text = "No NetworkSessionManager.";
            return;
        }

        string phaseLabel = Phase2Manager.IsPhase2Active ? "Phase 2" : "Phase 1";

        m_Text.text =
            $"LobbyId: {nsm.LobbyId}\n" +
            $"IsHost: {nsm.IsHost}\n" +
            $"Phase: {phaseLabel}\n" +
            $"Turn: {PuckController.TurnNumber}\n" +
            $"LocalIsWhite: {LobbyState.LocalIsWhitePlayer}\n" +
            $"WhiteTurn: {PuckController.IsWhiteTurn}\n" +
            $"OfflineMode: {nsm.OfflineMode}";

        NetworkDiagnostics diagnostics = nsm.Diagnostics;
        if (diagnostics != null)
        {
            m_Text.text += $"\nPing: {diagnostics.LatencyEstimateMs:0} ms";
            m_Text.text += $"\nLoss: {diagnostics.PacketLossEstimatePercent:0.0}%";
        }
    }

    private void ApplyVisibility()
    {
        if (m_DebugPanel == null) return;

        m_DebugPanel.alpha = m_Visible ? 1f : 0f;
        m_DebugPanel.interactable = m_Visible;
        m_DebugPanel.blocksRaycasts = m_Visible;
    }
}
