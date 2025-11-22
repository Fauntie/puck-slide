using System;

#if STEAMWORKSNET
using Steamworks;
#endif

public class SteamMatchmakingQuickplay
{
#if STEAMWORKSNET
    private readonly SteamLobbyManager m_LobbyManager;
    private readonly CallResult<LobbyMatchList_t> m_LobbyMatchList;

    private string m_Mode = string.Empty;
    private string m_Region = string.Empty;
    private string m_Version = string.Empty;
    private int m_MaxMembers;

    public event Action<string> OnStatusChanged;

    public SteamMatchmakingQuickplay(SteamLobbyManager lobbyManager)
    {
        m_LobbyManager = lobbyManager ?? throw new ArgumentNullException(nameof(lobbyManager));
        m_LobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchListReceived);
    }

    public void BeginQuickplay(int maxMembers, string mode, string region, string version)
    {
        m_MaxMembers = maxMembers;
        m_Mode = mode ?? string.Empty;
        m_Region = region ?? string.Empty;
        m_Version = version ?? string.Empty;

        SteamMatchmaking.AddRequestLobbyListStringFilter(SteamLobbyUtility.ModeKey, m_Mode, ELobbyComparison.k_ELobbyComparisonEqual);
        if (!string.IsNullOrEmpty(m_Region))
        {
            SteamMatchmaking.AddRequestLobbyListStringFilter(SteamLobbyUtility.RegionKey, m_Region, ELobbyComparison.k_ELobbyComparisonEqual);
        }

        if (!string.IsNullOrEmpty(m_Version))
        {
            SteamMatchmaking.AddRequestLobbyListStringFilter(SteamLobbyUtility.VersionKey, m_Version, ELobbyComparison.k_ELobbyComparisonEqual);
        }

        m_LobbyMatchList.Set(SteamMatchmaking.RequestLobbyList());
        OnStatusChanged?.Invoke("Searching for a lobby...");
    }

    private void OnLobbyMatchListReceived(LobbyMatchList_t result, bool ioFailure)
    {
        if (ioFailure)
        {
            OnStatusChanged?.Invoke("Failed to contact Steam matchmaking.");
            return;
        }

        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            if (!IsLobbyCompatible(lobbyId))
            {
                continue;
            }

            SteamMatchmaking.JoinLobby(lobbyId);
            OnStatusChanged?.Invoke("Joining an existing lobby...");
            return;
        }

        m_LobbyManager.HostLobby(m_MaxMembers, m_Mode, m_Region, m_Version);
        OnStatusChanged?.Invoke("No open lobbies found, hosting a new one.");
    }

    private bool IsLobbyCompatible(CSteamID lobbyId)
    {
        int memberLimit = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        if (memberLimit > 0 && memberCount >= memberLimit)
        {
            return false;
        }

        string lobbyVersion = SteamMatchmaking.GetLobbyData(lobbyId, SteamLobbyUtility.VersionKey);
        if (!string.IsNullOrEmpty(m_Version) && !string.Equals(lobbyVersion, m_Version, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string lobbyRegion = SteamMatchmaking.GetLobbyData(lobbyId, SteamLobbyUtility.RegionKey);
        if (!string.IsNullOrEmpty(m_Region) && !string.Equals(lobbyRegion, m_Region, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string lobbyMode = SteamMatchmaking.GetLobbyData(lobbyId, SteamLobbyUtility.ModeKey);
        if (!string.IsNullOrEmpty(m_Mode) && !string.Equals(lobbyMode, m_Mode, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
#else
    public event Action<string> OnStatusChanged;

    public SteamMatchmakingQuickplay(object lobbyManager)
    {
        throw new NotSupportedException("Steamworks.NET is not available in this build.");
    }

    public void BeginQuickplay(int maxMembers, string mode, string region, string version)
    {
        throw new NotSupportedException("Steamworks.NET is not available in this build.");
    }
#endif
}
