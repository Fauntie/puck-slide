using UnityEngine;
using TMPro;
using Puckslide.Networking;

public class NetworkStatusHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text m_StatusLabel;
    [SerializeField] private bool m_ShowDetails = true;

    private void Update()
    {
        if (m_StatusLabel == null)
        {
            return;
        }

        NetworkSessionManager manager = NetworkSessionManager.Instance;
        if (manager == null)
        {
            m_StatusLabel.text = "Offline";
            return;
        }

        if (manager.OfflineMode)
        {
            m_StatusLabel.text = "Offline";
            return;
        }

        m_StatusLabel.text = manager.IsHost ? "Hosting game" : "In game";

        NetworkDiagnostics diagnostics = manager.Diagnostics;
        if (m_ShowDetails && diagnostics != null)
        {
            m_StatusLabel.text += $"\nPing: {diagnostics.LatencyEstimateMs:0} ms  |  Loss: {diagnostics.PacketLossEstimatePercent:0.0}%";
        }
    }
}
