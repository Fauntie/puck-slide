using System;
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
    [SerializeField]
    private Dropdown m_RegionDropdown;
    [SerializeField]
    private Text m_VersionLabel;
    [SerializeField]
    private GameObject m_UpdatePromptPanel;
    [SerializeField]
    private Text m_UpdatePromptText;

    private LobbyStateMachine m_Lobby;
    private ManualTimeProvider m_TimeProvider;
    private ResilientSessionManager<string> m_SessionManager;
    private NetTransport m_Transport;
    private bool m_UsingSteam;
    private SteamLobbyManager m_SteamLobbyManager;
    private SteamMatchmakingQuickplay m_Quickplay;
    private bool m_QuickplayInProgress;
    private string m_StatusOverride = string.Empty;

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

        if (m_VersionLabel != null)
        {
            m_VersionLabel.text = $"Version {Application.version}";
        }

        if (SteamLobbyUtility.IsAvailable)
        {
            m_SteamLobbyManager = new SteamLobbyManager();
            m_SteamLobbyManager.OnLobbyReady += OnSteamLobbyReady;
            m_SteamLobbyManager.OnLobbyJoin += OnSteamLobbyJoin;
            m_SteamLobbyManager.OnVersionMismatch += OnSteamVersionMismatch;

            m_Quickplay = new SteamMatchmakingQuickplay(m_SteamLobbyManager);
            m_Quickplay.OnStatusChanged += SetStatusOverride;
        }

        UpdateStatus();
    }

    private void OnDestroy()
    {
        if (m_SessionManager != null)
        {
            m_SessionManager.OnStatusChanged -= OnSessionStatusChanged;
        }

        if (m_SteamLobbyManager != null)
        {
            m_SteamLobbyManager.OnLobbyReady -= OnSteamLobbyReady;
            m_SteamLobbyManager.OnLobbyJoin -= OnSteamLobbyJoin;
            m_SteamLobbyManager.OnVersionMismatch -= OnSteamVersionMismatch;
        }

        if (m_Quickplay != null)
        {
            m_Quickplay.OnStatusChanged -= SetStatusOverride;
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

        ClearStatusOverride();
        m_Lobby.Host(m_PlayerNameInput.text, sessionCode);
        BeginResilientTracking();
        UpdateStatus();
    }

    public void OnJoinClicked()
    {
        ClearStatusOverride();
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
        m_QuickplayInProgress = false;
        ClearStatusOverride();
        if (m_UpdatePromptPanel != null)
        {
            m_UpdatePromptPanel.SetActive(false);
        }
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

    public void OnQuickplayClicked()
    {
        if (!m_UsingSteam || m_SteamLobbyManager == null || m_Quickplay == null)
        {
            SetStatusOverride("Quickplay requires Steam matchmaking.");
            return;
        }

        if (string.IsNullOrWhiteSpace(m_PlayerNameInput.text))
        {
            SetStatusOverride("Enter a player name before quickplay.");
            return;
        }

        m_QuickplayInProgress = true;
        string region = GetSelectedRegion();
        m_Quickplay.BeginQuickplay(2, "quickplay", region, Application.version);
        SetStatusOverride(string.IsNullOrEmpty(region) ? "Searching for a quickplay match..." : $"Searching in {region} region...");
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

#if STEAMWORKSNET
    private void OnSteamLobbyReady(CSteamID lobbyId)
    {
        if (!m_QuickplayInProgress)
        {
            return;
        }

        string sessionCode = SteamUser.GetSteamID().m_SteamID.ToString();
        m_Lobby.Host(m_PlayerNameInput.text, sessionCode);
        BeginResilientTracking();
        m_QuickplayInProgress = false;
        SetStatusOverride("Hosting a new quickplay lobby...");
    }

    private void OnSteamLobbyJoin(CSteamID lobbyId)
    {
        if (!m_QuickplayInProgress)
        {
            return;
        }

        CSteamID owner = SteamMatchmaking.GetLobbyOwner(lobbyId);
        if (owner == SteamUser.GetSteamID())
        {
            return;
        }

        string sessionCode = owner.m_SteamID.ToString();
        if (m_SessionCodeInput != null)
        {
            m_SessionCodeInput.text = sessionCode;
        }

        m_Lobby.Join(m_PlayerNameInput.text, sessionCode);
        BeginResilientTracking();
        m_QuickplayInProgress = false;
        SetStatusOverride("Joining quickplay lobby...");
    }

    private void OnSteamVersionMismatch(string lobbyVersion)
    {
        m_QuickplayInProgress = false;
        string message = $"Version mismatch. Lobby is on {lobbyVersion}, you are on {Application.version}. Please update to play.";
        SetStatusOverride(message);

        if (m_UpdatePromptPanel != null)
        {
            m_UpdatePromptPanel.SetActive(true);
        }

        if (m_UpdatePromptText != null)
        {
            m_UpdatePromptText.text = message;
        }
    }
#endif

    private string GetSelectedRegion()
    {
        if (m_RegionDropdown == null || m_RegionDropdown.options.Count == 0)
        {
            return string.Empty;
        }

        string selected = m_RegionDropdown.options[m_RegionDropdown.value].text;
        return string.Equals(selected, "Any", StringComparison.OrdinalIgnoreCase) ? string.Empty : selected;
    }

    private void SetStatusOverride(string message)
    {
        m_StatusOverride = message;
        UpdateStatus();
    }

    private void ClearStatusOverride()
    {
        m_StatusOverride = string.Empty;
    }

    private void UpdateStatus()
    {
        if (m_StatusLabel == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(m_StatusOverride))
        {
            m_StatusLabel.text = m_StatusOverride;
        }
        else
        {
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
        }

        if (m_NetworkStatusLabel != null && m_SessionManager != null)
        {
            m_NetworkStatusLabel.text = $"Network: {m_SessionManager.Status} - {m_SessionManager.StatusMessage}";
        }
    }
}
