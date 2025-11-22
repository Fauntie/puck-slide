using System;

#if STEAMWORKSNET
using Steamworks;
#endif

public static class SteamLobbyUtility
{
    public const string ModeKey = "mode";
    public const string RegionKey = "region";
    public const string VersionKey = "version";

#if STEAMWORKSNET
    private static Callback<GameLobbyJoinRequested_t> s_InviteRequested;
    private static Callback<LobbyEnter_t> s_LobbyEntered;

    public static bool IsAvailable => SteamTransport.IsPlatformSupported;

    public static void InitializeCallbacks(Action<CSteamID> onJoinRequested, Action<CSteamID> onEntered)
    {
        s_InviteRequested = Callback<GameLobbyJoinRequested_t>.Create(data => onJoinRequested?.Invoke(data.m_steamIDLobby));
        s_LobbyEntered = Callback<LobbyEnter_t>.Create(data => onEntered?.Invoke(data.m_ulSteamIDLobby));
    }

    public static void SetMetadata(CSteamID lobbyId, string mode, string region, string version)
    {
        SteamMatchmaking.SetLobbyData(lobbyId, ModeKey, mode ?? string.Empty);
        SteamMatchmaking.SetLobbyData(lobbyId, RegionKey, region ?? string.Empty);
        SteamMatchmaking.SetLobbyData(lobbyId, VersionKey, version ?? string.Empty);
    }

    public static string BuildDeeplink(CSteamID lobbyId, CSteamID host)
    {
        return $"steam://joinlobby/{SteamUtils.GetAppID()}/{lobbyId.m_SteamID}/{host.m_SteamID}";
    }
#else
    public static bool IsAvailable => false;

    public static void InitializeCallbacks(Action<object> onJoinRequested, Action<object> onEntered)
    {
    }

    public static void SetMetadata(object lobbyId, string mode, string region, string version)
    {
    }

    public static string BuildDeeplink(object lobbyId, object host)
    {
        return string.Empty;
    }
#endif
}
