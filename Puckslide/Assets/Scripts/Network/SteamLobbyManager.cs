using System;

#if STEAMWORKSNET
using Steamworks;
#endif

public class SteamLobbyManager
{
#if STEAMWORKSNET
    private Callback<LobbyCreated_t> m_OnLobbyCreated;
    private Callback<LobbyEnter_t> m_OnLobbyEntered;
    private Callback<GameLobbyJoinRequested_t> m_OnGameLobbyJoinRequested;

    private string m_PendingMode = string.Empty;
    private string m_PendingRegion = string.Empty;
    private string m_PendingVersion = string.Empty;

    public CSteamID CurrentLobby { get; private set; }
    public string LastDeeplink { get; private set; } = string.Empty;

    public event Action<CSteamID> OnLobbyReady;
    public event Action<CSteamID> OnLobbyJoin;

    public SteamLobbyManager()
    {
        m_OnLobbyCreated = Callback<LobbyCreated_t>.Create(HandleLobbyCreated);
        m_OnLobbyEntered = Callback<LobbyEnter_t>.Create(HandleLobbyEntered);
        m_OnGameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(HandleInviteRequested);
    }

    public void HostLobby(int maxMembers, string mode, string region, string version)
    {
        m_PendingMode = mode ?? string.Empty;
        m_PendingRegion = region ?? string.Empty;
        m_PendingVersion = version ?? string.Empty;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxMembers);
        LastDeeplink = string.Empty;
        OnLobbyReady?.Invoke(CSteamID.Nil);
    }

    public void JoinLobby(string lobbyId)
    {
        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            throw new ArgumentException("Lobby id required to join.", nameof(lobbyId));
        }

        if (!ulong.TryParse(lobbyId, out ulong rawLobby))
        {
            throw new ArgumentException("Lobby id must be a SteamID64.", nameof(lobbyId));
        }

        SteamMatchmaking.JoinLobby(new CSteamID(rawLobby));
    }

    private void HandleLobbyCreated(LobbyCreated_t data)
    {
        if (data.m_eResult != EResult.k_EResultOK)
        {
            return;
        }

        CurrentLobby = new CSteamID(data.m_ulSteamIDLobby);
        SteamLobbyUtility.SetMetadata(CurrentLobby, m_PendingMode, m_PendingRegion, string.IsNullOrEmpty(m_PendingVersion) ? SteamUtils.GetAppID().ToString() : m_PendingVersion);
        LastDeeplink = SteamLobbyUtility.BuildDeeplink(CurrentLobby, SteamUser.GetSteamID());
        OnLobbyReady?.Invoke(CurrentLobby);
    }

    private void HandleLobbyEntered(LobbyEnter_t data)
    {
        CurrentLobby = new CSteamID(data.m_ulSteamIDLobby);
        OnLobbyJoin?.Invoke(CurrentLobby);
    }

    private void HandleInviteRequested(GameLobbyJoinRequested_t data)
    {
        SteamMatchmaking.JoinLobby(data.m_steamIDLobby);
    }
#else
    public string LastDeeplink => string.Empty;

    public void HostLobby(int maxMembers, string mode, string region, string version)
    {
    }

    public void JoinLobby(string lobbyId)
    {
    }
#endif
}
