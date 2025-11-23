using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Puckslide;
using TMPro;

#if STEAMWORKSNET
using Steamworks;
#endif

namespace Puckslide.Networking
{
    [DefaultExecutionOrder(-400)]
    [AddComponentMenu("Networking/Steamworks Bootstrap")]
    public class SteamworksBootstrap : MonoBehaviour
    {
        public static SteamworksBootstrap Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField]
        private uint m_EditorAppId = 480;
        [SerializeField]
        private bool m_WriteAppIdInEditor = true;
        [SerializeField]
        private bool m_EnableRichPresence = true;
        [SerializeField]
        private string m_MenuRichPresence = "In menus";
        [SerializeField]
        private string m_LobbyRichPresence = "In lobby";
        [SerializeField]
        private string m_MatchRichPresence = "In match";
        [SerializeField]
        private bool m_InitializeOnAwake = true;
        [SerializeField]
        private bool m_DriveNetworkSession = true;
        [SerializeField]
        private NetworkSessionManager m_SessionManager;
        [SerializeField]
        private int m_MaxPlayers = 2;
#if STEAMWORKSNET
        [SerializeField]
        private ELobbyType m_DefaultLobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;
#else
        [SerializeField]
        private int m_DefaultLobbyType = 0;
#endif
        [Header("Debug")]
        [SerializeField]
        private TMP_Text m_DebugStatusLabel;
        [SerializeField]
        private BuildConfig m_BuildConfig;
        [Header("Quickplay")]
        [SerializeField]
        private bool m_EnableQuickplay = true;
        [SerializeField]
        private int m_QuickplayMaxPlayers = 2;

        public bool Initialized { get; private set; }
        public bool LoggedOn { get; private set; }
        public ulong SteamId { get; private set; }
        public string PersonaName { get; private set; }
        public bool DrivesNetworkSession => m_DriveNetworkSession;

        public event Action<ulong> OnLobbyJoined;
        public event Action<ulong> OnLobbyInvite;
        public event Action<IReadOnlyList<SteamLobbySummary>> OnLobbyListUpdated;

#if STEAMWORKSNET
        private Callback<GameLobbyJoinRequested_t> m_JoinRequestedCallback;
        private Callback<LobbyInvite_t> m_LobbyInviteCallback;
        private CallResult<LobbyMatchList_t> m_LobbyMatchListResult;
        private CallResult<LobbyCreated_t> m_LobbyCreatedResult;
        private CallResult<LobbyEnter_t> m_LobbyEnterResult;
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (m_InitializeOnAwake)
            {
                Initialize();
            }

            if (m_DriveNetworkSession && m_SessionManager == null)
            {
                m_SessionManager = FindObjectOfType<NetworkSessionManager>();
                if (m_SessionManager == null)
                {
                    Debug.LogWarning("[Steamworks] m_DriveNetworkSession is true, but no NetworkSessionManager was found in the scene.");
                }
            }
        }

        private void Update()
        {
#if STEAMWORKSNET
            if (Initialized)
            {
                SteamAPI.RunCallbacks();
            }
#endif
        }

        private void OnDestroy()
        {
#if STEAMWORKSNET
            if (Initialized)
            {
                SteamAPI.Shutdown();
            }
#endif

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize()
        {
            if (!IsSteamEnabled())
            {
                Debug.Log("[Steamworks] Steam initialization disabled via BuildConfig.");
                UpdateDebugStatusLabel("Steam: DISABLED (BuildConfig)");
                return;
            }

#if STEAMWORKSNET
            if (Initialized)
            {
                return;
            }

#if UNITY_EDITOR
            if (m_WriteAppIdInEditor)
            {
                WriteSteamAppId();
            }
#endif

            if (!Packsize.Test() || !DllCheck.Test())
            {
                Debug.LogError("[Steamworks] Steamworks binaries appear to be the wrong version for this platform.");
                UpdateDebugStatusLabel("Steam: FAILED (binary mismatch)");
                return;
            }

            Initialized = SteamAPI.Init();
            if (!Initialized)
            {
                Debug.LogError("[Steamworks] Steam API initialization failed.");
                UpdateDebugStatusLabel("Steam: FAILED (running without Steam)");
                return;
            }

            LoggedOn = SteamUser.BLoggedOn();
            SteamId = SteamUser.GetSteamID().m_SteamID;
            PersonaName = SteamFriends.GetPersonaName();

            SteamNetworkingUtils.InitRelayNetworkAccess();

            m_JoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
            m_LobbyInviteCallback = Callback<LobbyInvite_t>.Create(OnLobbyInviteReceived);
            m_LobbyMatchListResult = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
            m_LobbyCreatedResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            m_LobbyEnterResult = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);

            if (m_EnableRichPresence)
            {
                SteamFriends.SetRichPresence("status", m_MenuRichPresence);
            }

            Debug.Log($"[Steamworks] Initialized. Logged in: {LoggedOn} as {PersonaName} ({SteamId}).");
            UpdateDebugStatusLabel($"Steam: OK ({PersonaName})");
#else
            Debug.LogWarning("Steamworks.NET is not enabled; Steam features are disabled.");
            UpdateDebugStatusLabel("Steam: FAILED (Steamworks disabled)");
#endif
        }

        public void HostSteamLobby()
        {
            if (!Initialized)
            {
                Debug.LogWarning("[Steamworks] Cannot host lobby before initialization.");
                return;
            }

            CreateLobby(m_MaxPlayers, m_DefaultLobbyType);
        }

        public void JoinSteamLobbyById(ulong lobbyId)
        {
            if (!Initialized)
            {
                Debug.LogWarning("[Steamworks] Cannot join lobby before initialization.");
                return;
            }

            JoinLobby(lobbyId);
        }

        public void RequestQuickplayLobbies()
        {
            if (!Initialized)
            {
                Debug.LogWarning("[Steamworks] Cannot request quickplay lobbies before initialization.");
                return;
            }

            RequestLobbyList();
        }

        public void Quickplay()
        {
            if (!m_EnableQuickplay)
            {
                Debug.Log("[Steamworks] Quickplay is disabled.");
                return;
            }

            if (!Initialized)
            {
                Debug.LogWarning("[Steamworks] Cannot quickplay before initialization.");
                return;
            }

            RequestLobbyList();
        }

        private void WriteSteamAppId()
        {
            string appIdPath = Path.Combine(Application.dataPath, "..", "steam_appid.txt");
            try
            {
                File.WriteAllText(appIdPath, m_EditorAppId.ToString());
                Debug.Log($"[Steamworks] Wrote steam_appid.txt to {appIdPath}.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Steamworks] Failed to write steam_appid.txt: {ex.Message}");
            }
        }

        public void RequestLobbyList()
        {
#if STEAMWORKSNET
            if (!Initialized)
            {
                Debug.LogWarning("[Steamworks] Cannot request lobby list before initialization.");
                return;
            }

            SteamAPICall_t apiCall = SteamMatchmaking.RequestLobbyList();
            m_LobbyMatchListResult.Set(apiCall);
#else
            Debug.LogWarning("Steamworks.NET is not enabled; cannot request lobby list.");
#endif
        }

#if STEAMWORKSNET
        public void CreateLobby(int maxPlayers, ELobbyType lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly)
        {
            if (!Initialized)
            {
                Debug.LogWarning("[Steamworks] Cannot create lobby before initialization.");
                return;
            }

            SteamAPICall_t apiCall = SteamMatchmaking.CreateLobby(lobbyType, maxPlayers);
            m_LobbyCreatedResult.Set(apiCall);
        }
#else
        public void CreateLobby(int maxPlayers, int lobbyType = 0)
        {
            Debug.LogWarning("Steamworks.NET is not enabled; cannot create lobby.");
        }
#endif

        public void JoinLobby(ulong lobbyId)
        {
#if STEAMWORKSNET
            if (!Initialized)
            {
                Debug.LogWarning("[Steamworks] Cannot join lobby before initialization.");
                return;
            }

            SteamAPICall_t apiCall = SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
            m_LobbyEnterResult.Set(apiCall);
#else
            Debug.LogWarning("Steamworks.NET is not enabled; cannot join lobby.");
#endif
        }

        public void LeaveLobby(ulong lobbyId)
        {
#if STEAMWORKSNET
            if (!Initialized)
            {
                return;
            }

            SteamMatchmaking.LeaveLobby(new CSteamID(lobbyId));
#endif
        }

        public void SetMenuPresence()
        {
            SetRichPresence(m_MenuRichPresence);
        }

        public void SetLobbyPresence()
        {
            SetRichPresence(m_LobbyRichPresence);
        }

        public void SetMatchPresence()
        {
            SetRichPresence(m_MatchRichPresence);
        }

        public void SetRichPresence(string status)
        {
#if STEAMWORKSNET
            if (!Initialized || !m_EnableRichPresence)
            {
                return;
            }

            SteamFriends.SetRichPresence("status", status);
#endif
        }

#if STEAMWORKSNET
        private void OnLobbyMatchList(LobbyMatchList_t result, bool ioFailure)
        {
            if (ioFailure)
            {
                Debug.LogWarning("[Steamworks] Lobby list request failed (I/O).");
                return;
            }

            List<SteamLobbySummary> summaries = new List<SteamLobbySummary>();
            for (int i = 0; i < result.m_nLobbiesMatching; ++i)
            {
                CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
                string lobbyName = SteamMatchmaking.GetLobbyData(lobbyId, "name");
                summaries.Add(new SteamLobbySummary
                {
                    LobbyId = lobbyId.m_SteamID,
                    Name = string.IsNullOrWhiteSpace(lobbyName) ? lobbyId.m_SteamID.ToString() : lobbyName,
                    MemberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId),
                    MaxMembers = SteamMatchmaking.GetLobbyMemberLimit(lobbyId),
                    OwnerId = SteamMatchmaking.GetLobbyOwner(lobbyId).m_SteamID
                });
            }

            OnLobbyListUpdated?.Invoke(summaries);

            if (m_EnableQuickplay)
            {
                HandleQuickplayFromSummaries(summaries);
            }
        }

        private void OnLobbyCreated(LobbyCreated_t result, bool ioFailure)
        {
            if (ioFailure || result.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogWarning($"[Steamworks] Lobby creation failed: {result.m_eResult} (ioFailure={ioFailure}).");
                return;
            }

            ulong lobbyId = result.m_ulSteamIDLobby;
            SteamMatchmaking.SetLobbyJoinable(new CSteamID(lobbyId), true);
            if (!string.IsNullOrWhiteSpace(PersonaName))
            {
                SteamMatchmaking.SetLobbyData(new CSteamID(lobbyId), "name", PersonaName);
            }

            Debug.Log($"[Steamworks] Created lobby {lobbyId}.");
            JoinLobby(lobbyId);
        }

        private void OnLobbyEntered(LobbyEnter_t result, bool ioFailure)
        {
            if (ioFailure)
            {
                Debug.LogWarning("[Steamworks] Lobby enter failed due to I/O error.");
                return;
            }

            ulong lobbyId = result.m_ulSteamIDLobby;
            CSteamID owner = SteamMatchmaking.GetLobbyOwner(new CSteamID(lobbyId));
            bool isHost = owner == SteamUser.GetSteamID();

            Debug.Log($"[Steamworks] Entered lobby {lobbyId} as {(isHost ? "host" : "client")}.");

            if (m_SessionManager != null && m_DriveNetworkSession)
            {
                if (isHost)
                {
                    if (m_SessionManager.LobbyId != lobbyId.ToString())
                    {
                        m_SessionManager.CreateLobby(lobbyId.ToString());
                    }
                }
                else
                {
                    m_SessionManager.JoinLobby(lobbyId.ToString());
                }
            }

            if (m_EnableRichPresence)
            {
                SetLobbyPresence();
            }

            OnLobbyJoined?.Invoke(lobbyId);
        }

        private void OnLobbyJoinRequested(GameLobbyJoinRequested_t request)
        {
            Debug.Log($"[Steamworks] Lobby join requested for lobby {request.m_steamIDLobby.m_SteamID} by {request.m_steamIDFriend.m_SteamID}.");
            JoinLobby(request.m_steamIDLobby.m_SteamID);
        }

        private void OnLobbyInviteReceived(LobbyInvite_t invite)
        {
            ulong lobbyId = invite.m_ulSteamIDLobby;
            OnLobbyInvite?.Invoke(lobbyId);
            Debug.Log($"[Steamworks] Received lobby invite to {lobbyId} from {invite.m_ulSteamIDUser}.");
        }
#endif

        private void HandleQuickplayFromSummaries(IReadOnlyList<SteamLobbySummary> summaries)
        {
            SteamLobbySummary? best = null;
            foreach (SteamLobbySummary summary in summaries)
            {
                if (summary.MemberCount >= summary.MaxMembers)
                {
                    continue;
                }

                if (best == null || summary.MemberCount > best.Value.MemberCount)
                {
                    best = summary;
                }
            }

            if (best.HasValue)
            {
                Debug.Log($"[Steamworks] Quickplay joining lobby {best.Value.LobbyId} ({best.Value.MemberCount}/{best.Value.MaxMembers}).");
                JoinLobby(best.Value.LobbyId);
                return;
            }

            Debug.Log("[Steamworks] Quickplay creating a new lobby (no suitable existing lobbies).");
            CreateLobby(m_QuickplayMaxPlayers, m_DefaultLobbyType);
        }

        private bool IsSteamEnabled()
        {
            BuildConfig config = ResolveBuildConfig();
            return config == null || config.EnableSteam;
        }

        private BuildConfig ResolveBuildConfig()
        {
            if (m_BuildConfig != null)
            {
                return m_BuildConfig;
            }

            m_BuildConfig = Resources.Load<BuildConfig>("BuildConfig");
            return m_BuildConfig;
        }

        private void UpdateDebugStatusLabel(string text)
        {
            if (m_DebugStatusLabel != null)
            {
                m_DebugStatusLabel.text = text;
            }
        }
    }

    public struct SteamLobbySummary
    {
        public ulong LobbyId;
        public string Name;
        public int MemberCount;
        public int MaxMembers;
        public ulong OwnerId;
    }
}
