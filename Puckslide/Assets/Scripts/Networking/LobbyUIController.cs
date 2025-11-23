using Puckslide.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text m_RoleLabel;
    [SerializeField] private TMP_Text m_ColorLabel;
    [SerializeField] private Button m_StartMatchButton;
    [SerializeField] private Toggle m_HostIsWhiteToggle;
    [SerializeField] private CanvasGroup m_HostOnlyControls;

    private void OnEnable()
    {
        NetworkEvents.OnLobbySnapshot.AddListener(OnLobbySnapshotReceived);
    }

    private void OnDisable()
    {
        NetworkEvents.OnLobbySnapshot.RemoveListener(OnLobbySnapshotReceived);
    }

    private void Start()
    {
        if (m_StartMatchButton != null)
        {
            m_StartMatchButton.onClick.AddListener(OnStartMatchClicked);
        }
    }

    private void OnLobbySnapshotReceived(NetworkLobbySnapshot snapshot)
    {
        if (m_RoleLabel != null)
        {
            m_RoleLabel.text = LobbyState.LocalIsHost ? "Host" : "Guest";
        }

        if (m_ColorLabel != null)
        {
            m_ColorLabel.text = LobbyState.LocalIsWhitePlayer ? "White" : "Black";
        }

        bool isHost = LobbyState.LocalIsHost;

        if (m_StartMatchButton != null)
        {
            m_StartMatchButton.interactable = isHost;
            m_StartMatchButton.gameObject.SetActive(isHost);
        }

        if (m_HostIsWhiteToggle != null)
        {
            m_HostIsWhiteToggle.interactable = isHost;
        }

        if (m_HostOnlyControls != null)
        {
            m_HostOnlyControls.interactable = isHost;
            m_HostOnlyControls.alpha = isHost ? 1f : 0.5f;
            m_HostOnlyControls.blocksRaycasts = isHost;
        }
    }

    private void OnStartMatchClicked()
    {
        if (NetworkSessionManager.Instance != null)
        {
            NetworkSessionManager.Instance.StartMatchAsHost();
        }
    }
}
