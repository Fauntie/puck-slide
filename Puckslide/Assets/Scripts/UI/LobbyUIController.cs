using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField]
    private Toggle m_EnableTelemetryToggle;
    [SerializeField]
    private Toggle m_EnableCrashReportsToggle;
    [SerializeField]
    private NetworkStatusHUD m_NetworkStatusHUD;
    [SerializeField]
    private InputField m_InviteCodeInput;
    [SerializeField]
    private Button m_SendInviteButton;
    [SerializeField]
    private Toggle m_ReadyToggle;
    [SerializeField]
    private Text m_ReadyStateLabel;
    [SerializeField]
    private InputField m_ChatInput;
    [SerializeField]
    private Text m_ChatLog;
    [SerializeField]
    private Dropdown m_QuickEmoteDropdown;

    [Header("Localized Strings")]
    [SerializeField]
    private LobbyLocalizationStrings m_Localization = new LobbyLocalizationStrings();

    private LobbyStateMachine m_Lobby;
    private ManualTimeProvider m_TimeProvider;
    private ResilientSessionManager<string> m_SessionManager;
    private NetTransport m_Transport;
    private bool m_UsingSteam;
    private SteamLobbyManager m_SteamLobbyManager;
    private SteamMatchmakingQuickplay m_Quickplay;
    private bool m_QuickplayInProgress;
    private string m_StatusOverride = string.Empty;
    private NetworkDiagnostics m_Diagnostics;
    private readonly List<string> m_ChatHistory = new List<string>();

    private void Awake()
    {
        m_Diagnostics = new NetworkDiagnostics
        {
            MetricsOptIn = m_EnableTelemetryToggle != null && m_EnableTelemetryToggle.isOn,
            StructuredLogger = LogStructuredNetworkEvent
        };

        CrashReportingService.Initialize(m_Diagnostics, m_EnableCrashReportsToggle != null && m_EnableCrashReportsToggle.isOn);

        if (m_EnableTelemetryToggle != null)
        {
            m_EnableTelemetryToggle.onValueChanged.AddListener(OnTelemetryToggled);
        }

        if (m_EnableCrashReportsToggle != null)
        {
            m_EnableCrashReportsToggle.onValueChanged.AddListener(CrashReportingService.SetEnabled);
        }

        m_TimeProvider = new ManualTimeProvider();
        m_Transport = SteamTransport.IsPlatformSupported ? (NetTransport)new SteamTransport() : new LoopbackTransport();
        m_UsingSteam = m_Transport is SteamTransport;
#if STEAMWORKSNET
        if (m_UsingSteam)
        {
            SteamPlatformService.EnsureInitialized();
        }
#endif
        m_Lobby = new LobbyStateMachine(m_Transport);
        m_SessionManager = new ResilientSessionManager<string>(
            m_Lobby.Transport,
            m_TimeProvider,
            () => $"{m_Lobby.State}:{m_Lobby.PlayerName}:{m_Lobby.SessionCode}",
            ResilientSessionSerializer.SerializeText,
            ResilientSessionSerializer.DeserializeText,
            diagnostics: m_Diagnostics);
        m_SessionManager.OnStatusChanged += OnSessionStatusChanged;

        if (m_NetworkStatusHUD != null)
        {
            m_NetworkStatusHUD.Bind(m_Diagnostics, m_SessionManager, m_Localization);
        }

        if (m_VersionLabel != null)
        {
            m_VersionLabel.text = string.Format(m_Localization.VersionFormat, Application.version);
        }

        if (m_SendInviteButton != null)
        {
            m_SendInviteButton.onClick.AddListener(OnSendInviteClicked);
        }

        if (m_ReadyToggle != null)
        {
            m_ReadyToggle.onValueChanged.AddListener(OnReadyToggled);
            RefreshReadyState(false);
        }

        if (m_QuickEmoteDropdown != null)
        {
            m_QuickEmoteDropdown.onValueChanged.AddListener(OnQuickEmoteSelected);
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

        FocusFirstSelectable();
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
        m_NetworkStatusHUD?.Tick();

        if (m_ChatInput != null && Input.GetKeyDown(KeyCode.Return))
        {
            SubmitChat();
        }
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
        RegisterPrivacyTerms();
        UpdateStatus();
#if STEAMWORKSNET
        if (m_UsingSteam)
        {
            SteamPlatformService.ReportSessionHosted();
        }
#endif
    }

    public void OnJoinClicked()
    {
        ClearStatusOverride();
        m_Lobby.Join(m_PlayerNameInput.text, m_SessionCodeInput.text);
        BeginResilientTracking();
        RegisterPrivacyTerms();
        UpdateStatus();
#if STEAMWORKSNET
        if (m_UsingSteam)
        {
            SteamPlatformService.ReportSessionJoined();
        }
#endif
    }

    public void OnReadyClicked()
    {
        OnReadyToggled(true);
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
        RefreshReadyState(false);
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
            SetStatusOverride(m_Localization.QuickplayRequiresSteam);
            return;
        }

        if (string.IsNullOrWhiteSpace(m_PlayerNameInput.text))
        {
            SetStatusOverride(m_Localization.QuickplayMissingName);
            return;
        }

        m_QuickplayInProgress = true;
        string region = GetSelectedRegion();
        m_Quickplay.BeginQuickplay(2, "quickplay", region, Application.version);
        SetStatusOverride(string.IsNullOrEmpty(region) ? m_Localization.QuickplaySearching : string.Format(m_Localization.QuickplayRegionFormat, region));
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
        m_NetworkStatusHUD?.UpdateSessionStatus(status, message);
    }

    private void OnTelemetryToggled(bool enabled)
    {
        if (m_Diagnostics != null)
        {
            m_Diagnostics.MetricsOptIn = enabled;
            m_Diagnostics.LogEvent("telemetry", enabled ? "Metrics enabled by player." : "Metrics disabled by player.");
        }
    }

    private void LogStructuredNetworkEvent(NetworkLogEvent logEvent)
    {
        string context = logEvent.Context == null || !logEvent.Context.Any()
            ? string.Empty
            : string.Join(", ", logEvent.Context.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        Debug.Log($"[net][{logEvent.EventType}] {logEvent.Message} {context}");
    }

    private void RegisterPrivacyTerms()
    {
        CrashReportingService.AddRedactionTerm(m_PlayerNameInput != null ? m_PlayerNameInput.text : string.Empty);
        CrashReportingService.AddRedactionTerm(m_SessionCodeInput != null ? m_SessionCodeInput.text : string.Empty);
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
        SetStatusOverride(m_Localization.QuickplayHostLobby);
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
        SetStatusOverride(m_Localization.QuickplayJoiningLobby);
    }

    private void OnSteamVersionMismatch(string lobbyVersion)
    {
        m_QuickplayInProgress = false;
        string message = string.Format(m_Localization.VersionMismatchFormat, lobbyVersion, Application.version);
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

    private void OnSendInviteClicked()
    {
        string inviteCode = m_InviteCodeInput != null ? m_InviteCodeInput.text : string.Empty;
        if (string.IsNullOrWhiteSpace(inviteCode))
        {
            SetStatusOverride(m_Localization.InviteMissingCode);
            return;
        }

        SetStatusOverride(string.Format(m_Localization.InviteSentFormat, inviteCode.Trim()));

#if STEAMWORKSNET
        if (m_UsingSteam)
        {
            CSteamID lobbyId = m_SteamLobbyManager != null ? m_SteamLobbyManager.CurrentLobby : CSteamID.Nil;
            SteamPlatformService.OpenInviteOverlay(lobbyId);
        }
#endif
    }

    private void OnReadyToggled(bool ready)
    {
        RefreshReadyState(ready);

        if (ready && (m_Lobby.State == LobbyState.Hosting || m_Lobby.State == LobbyState.Joining))
        {
            m_Lobby.MarkReady();
            UpdateStatus();
        }
    }

    private void RefreshReadyState(bool ready)
    {
        if (m_ReadyStateLabel != null)
        {
            m_ReadyStateLabel.text = ready ? m_Localization.ReadyStateReady : m_Localization.ReadyStateNotReady;
        }

        if (m_ReadyToggle != null && m_ReadyToggle.isOn != ready)
        {
            m_ReadyToggle.isOn = ready;
        }

#if STEAMWORKSNET
        if (ready && m_UsingSteam)
        {
            SteamPlatformService.ReportReadyState();
        }
#endif
    }

    public void SubmitChat()
    {
        if (m_ChatInput == null)
        {
            return;
        }

        string message = m_ChatInput.text;
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string playerName = m_PlayerNameInput != null && !string.IsNullOrWhiteSpace(m_PlayerNameInput.text)
            ? m_PlayerNameInput.text
            : m_Localization.UnknownPlayerName;
        AppendChatLine(string.Format(m_Localization.ChatLineFormat, playerName, message.Trim()));
        m_ChatInput.text = string.Empty;
    }

    private void OnQuickEmoteSelected(int index)
    {
        if (m_QuickEmoteDropdown == null || index < 0 || index >= m_QuickEmoteDropdown.options.Count)
        {
            return;
        }

        string emote = m_QuickEmoteDropdown.options[index].text;
        string playerName = m_PlayerNameInput != null && !string.IsNullOrWhiteSpace(m_PlayerNameInput.text)
            ? m_PlayerNameInput.text
            : m_Localization.UnknownPlayerName;
        AppendChatLine(string.Format(m_Localization.EmoteBroadcastFormat, playerName, emote));
    }

    private void AppendChatLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        m_ChatHistory.Add(line);
        const int maxHistory = 20;
        if (m_ChatHistory.Count > maxHistory)
        {
            m_ChatHistory.RemoveAt(0);
        }

        if (m_ChatLog != null)
        {
            m_ChatLog.text = string.Join("\n", m_ChatHistory);
        }
    }

    private void FocusFirstSelectable()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        if (m_PlayerNameInput != null)
        {
            EventSystem.current.SetSelectedGameObject(m_PlayerNameInput.gameObject);
        }
        else if (m_SessionCodeInput != null)
        {
            EventSystem.current.SetSelectedGameObject(m_SessionCodeInput.gameObject);
        }
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
                    m_StatusLabel.text = m_Localization.IdlePrompt;
                    break;
                case LobbyState.Hosting:
                    m_StatusLabel.text = string.Format(m_Localization.HostingFormat, m_Lobby.SessionCode, m_Lobby.PlayerName);
                    break;
                case LobbyState.Joining:
                    m_StatusLabel.text = string.Format(m_Localization.JoiningFormat, m_Lobby.SessionCode, m_Lobby.PlayerName);
                    break;
                case LobbyState.Ready:
                    m_StatusLabel.text = m_Localization.ReadyPrompt;
                    break;
                case LobbyState.Starting:
                    m_StatusLabel.text = m_Localization.StartingPrompt;
                    break;
            }
        }

        if (m_NetworkStatusLabel != null && m_SessionManager != null)
        {
            m_NetworkStatusLabel.text = string.Format(m_Localization.NetworkStatusFormat, m_SessionManager.Status, m_SessionManager.StatusMessage);
        }
    }
}

[Serializable]
public class LobbyLocalizationStrings
{
    public string IdlePrompt = "Enter a name and session to host or join.";
    public string HostingFormat = "Hosting {0} as {1}.";
    public string JoiningFormat = "Joining {0} as {1}.";
    public string ReadyPrompt = "Ready to start.";
    public string StartingPrompt = "Starting match...";
    public string NetworkStatusFormat = "Network: {0} - {1}";
    public string VersionFormat = "Version {0}";
    public string VersionMismatchFormat = "Version mismatch. Lobby is on {0}, you are on {1}. Please update to play.";
    public string QuickplayRequiresSteam = "Quickplay requires Steam matchmaking.";
    public string QuickplayMissingName = "Enter a player name before quickplay.";
    public string QuickplaySearching = "Searching for a quickplay match...";
    public string QuickplayRegionFormat = "Searching in {0} region...";
    public string QuickplayHostLobby = "Hosting a new quickplay lobby...";
    public string QuickplayJoiningLobby = "Joining quickplay lobby...";
    public string InviteMissingCode = "Enter a friend code before inviting.";
    public string InviteSentFormat = "Invite sent to {0}.";
    public string ReadyStateReady = "Ready";
    public string ReadyStateNotReady = "Not ready";
    public string PingFormat = "Ping: {0} ms";
    public string PacketLossFormat = "Packet Loss: {0}";
    public string RollbackFormat = "Rollbacks: {0}";
    public string ReconnectingPrompt = "Connection interrupted. Attempting to reconnect...";
    public string ReconnectFailedPrompt = "Reconnect failed. Please try again.";
    public string ChatLineFormat = "{0}: {1}";
    public string EmoteBroadcastFormat = "{0} sent emote {1}";
    public string UnknownPlayerName = "Player";
}
