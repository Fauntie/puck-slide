using UnityEngine;
using UnityEngine.UI;
#if STEAMWORKSNET
using Steamworks;
#endif

public class LobbyUIController : MonoBehaviour
{
    [SerializeField]
    private InputField m_PlayerNameInput;
    [SerializeField]
    private InputField m_SessionCodeInput;
    [SerializeField]
    private Text m_StatusLabel;
    [SerializeField]
    private Text m_NetworkStatusLabel;

    private LobbyStateMachine m_Lobby;
    private ManualTimeProvider m_TimeProvider;
    private ResilientSessionManager<string> m_SessionManager;
    private NetTransport m_Transport;
    private bool m_UsingSteam;

    private void Awake()
    {
        m_TimeProvider = new ManualTimeProvider();
        m_Transport = SteamTransport.IsPlatformSupported ? (NetTransport)new SteamTransport() : new LoopbackTransport();
        m_UsingSteam = m_Transport is SteamTransport;
        m_Lobby = new LobbyStateMachine(m_Transport);
        m_SessionManager = new ResilientSessionManager<string>(
            m_Lobby.Transport,
            m_TimeProvider,
            () => $"{m_Lobby.State}:{m_Lobby.PlayerName}:{m_Lobby.SessionCode}",
            ResilientSessionSerializer.SerializeText,
            ResilientSessionSerializer.DeserializeText);
        m_SessionManager.OnStatusChanged += OnSessionStatusChanged;
        UpdateStatus();
    }

    private void OnDestroy()
    {
        if (m_SessionManager != null)
        {
            m_SessionManager.OnStatusChanged -= OnSessionStatusChanged;
        }
    }

    private void Update()
    {
        if (m_TimeProvider != null)
        {
            m_TimeProvider.Advance(Time.deltaTime);
        }

        m_SessionManager?.Update();
    }

    public void OnHostClicked()
    {
        string sessionCode = m_SessionCodeInput.text;

#if STEAMWORKSNET
        if (m_UsingSteam)
        {
            sessionCode = SteamUser.GetSteamID().m_SteamID.ToString();
        }
#endif

        m_Lobby.Host(m_PlayerNameInput.text, sessionCode);
        BeginResilientTracking();
        UpdateStatus();
    }

    public void OnJoinClicked()
    {
        m_Lobby.Join(m_PlayerNameInput.text, m_SessionCodeInput.text);
        BeginResilientTracking();
        UpdateStatus();
    }

    public void OnReadyClicked()
    {
        m_Lobby.MarkReady();
        UpdateStatus();
    }

    public void OnStartClicked()
    {
        m_Lobby.BeginStart();
        UpdateStatus();
    }

    public void OnResetClicked()
    {
        m_Lobby.Reset();
        m_SessionManager?.Pause();
        UpdateStatus();
    }

    public void OnPauseClicked()
    {
        m_SessionManager?.Pause();
        UpdateStatus();
    }

    public void OnResumeClicked()
    {
        m_SessionManager?.Resume();
        UpdateStatus();
    }

    private void BeginResilientTracking()
    {
        if (m_SessionManager == null)
        {
            return;
        }

        int port = LobbySessionCodeUtility.GetPort(m_Lobby.SessionCode);
        string address = m_UsingSteam ? m_Lobby.SessionCode : "localhost";
        m_SessionManager.Start(address, port, m_Lobby.Transport.LocalPeerId);
        m_SessionManager.ConfirmConnected();
    }

    private void OnSessionStatusChanged(ResilientSessionStatus status, string message)
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (m_StatusLabel == null)
        {
            return;
        }

        switch (m_Lobby.State)
        {
            case LobbyState.Idle:
                m_StatusLabel.text = "Enter a name and session to host or join.";
                break;
            case LobbyState.Hosting:
                m_StatusLabel.text = $"Hosting {m_Lobby.SessionCode} as {m_Lobby.PlayerName}.";
                break;
            case LobbyState.Joining:
                m_StatusLabel.text = $"Joining {m_Lobby.SessionCode} as {m_Lobby.PlayerName}.";
                break;
            case LobbyState.Ready:
                m_StatusLabel.text = "Ready to start.";
                break;
            case LobbyState.Starting:
                m_StatusLabel.text = "Starting match...";
                break;
        }

        if (m_NetworkStatusLabel != null && m_SessionManager != null)
        {
            m_NetworkStatusLabel.text = $"Network: {m_SessionManager.Status} - {m_SessionManager.StatusMessage}";
        }
    }
}
