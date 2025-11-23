using Puckslide.Networking;
using TMPro;
using UnityEngine;

public class NetworkErrorHUD : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_Panel;
    [SerializeField] private TMP_Text m_MessageLabel;
    [SerializeField] private float m_AutoReturnDelay = 3f;
    [SerializeField] private GameObject m_LobbyUIRoot;
    [SerializeField] private GameObject m_GameUIRoot;

    private void OnEnable()
    {
        NetworkEvents.OnDisconnected.AddListener(OnDisconnected);
    }

    private void OnDisable()
    {
        NetworkEvents.OnDisconnected.RemoveListener(OnDisconnected);
    }

    private void Start()
    {
        if (m_Panel != null)
        {
            m_Panel.alpha = 0f;
            m_Panel.interactable = false;
            m_Panel.blocksRaycasts = false;
        }
    }

    private void OnDisconnected(NetworkDisconnectReason reason)
    {
        if (m_Panel != null)
        {
            m_Panel.alpha = 1f;
            m_Panel.interactable = true;
            m_Panel.blocksRaycasts = true;
        }

        if (m_MessageLabel != null)
        {
            m_MessageLabel.text = reason switch
            {
                NetworkDisconnectReason.Timeout => "Connection lost (timeout).",
                NetworkDisconnectReason.RemoteClosed => "Connection closed.",
                NetworkDisconnectReason.TransportError => "Network error occurred.",
                _ => "Disconnected from match."
            };
        }

        Invoke(nameof(ReturnToLobby), m_AutoReturnDelay);
    }

    private void ReturnToLobby()
    {
        if (m_LobbyUIRoot != null)
        {
            m_LobbyUIRoot.SetActive(true);
        }

        if (m_GameUIRoot != null)
        {
            m_GameUIRoot.SetActive(false);
        }

        if (m_Panel != null)
        {
            m_Panel.alpha = 0f;
            m_Panel.interactable = false;
            m_Panel.blocksRaycasts = false;
        }
    }
}
