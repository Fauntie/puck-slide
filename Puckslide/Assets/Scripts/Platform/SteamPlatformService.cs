using System;

#if STEAMWORKSNET
using Steamworks;
#endif

/// <summary>
/// Centralizes Steam-only hooks for achievements, overlay, and simple stat tracking.
/// Calls are guarded by STEAMWORKSNET and runtime checks so non-Steam builds remain unaffected.
/// </summary>
public static class SteamPlatformService
{
#if STEAMWORKSNET
    private const string AchievementHostedLobby = "ACH_SESSION_HOST";
    private const string AchievementJoinedLobby = "ACH_SESSION_JOIN";
    private const string AchievementReadyUp = "ACH_READY_UP";

    private const string StatSessionsHosted = "STAT_SESSIONS_HOSTED";
    private const string StatSessionsJoined = "STAT_SESSIONS_JOINED";
    private const string StatReadyUps = "STAT_READY_UPS";

    private static bool s_StatsRequested;

    public static void EnsureInitialized()
    {
        if (s_StatsRequested || !SteamTransport.IsPlatformSupported)
        {
            return;
        }

        s_StatsRequested = SteamUserStats.RequestCurrentStats();
    }

    public static void ReportSessionHosted()
    {
        if (!EnsureStatsReady())
        {
            return;
        }

        UnlockAchievement(AchievementHostedLobby);
        IncrementStat(StatSessionsHosted);
    }

    public static void ReportSessionJoined()
    {
        if (!EnsureStatsReady())
        {
            return;
        }

        UnlockAchievement(AchievementJoinedLobby);
        IncrementStat(StatSessionsJoined);
    }

    public static void ReportReadyState()
    {
        if (!EnsureStatsReady())
        {
            return;
        }

        UnlockAchievement(AchievementReadyUp);
        IncrementStat(StatReadyUps);
    }

    public static void OpenInviteOverlay(CSteamID lobbyId)
    {
        if (!SteamTransport.IsPlatformSupported)
        {
            return;
        }

        if (lobbyId == CSteamID.Nil)
        {
            SteamFriends.ActivateGameOverlay("Friends");
            return;
        }

        SteamFriends.ActivateGameOverlayInviteDialog(lobbyId);
    }

    public static void OpenAchievementsOverlay()
    {
        if (!SteamTransport.IsPlatformSupported)
        {
            return;
        }

        SteamFriends.ActivateGameOverlay("Achievements");
    }

    private static bool EnsureStatsReady()
    {
        EnsureInitialized();

        if (!SteamTransport.IsPlatformSupported || !s_StatsRequested)
        {
            return false;
        }

        return true;
    }

    private static void UnlockAchievement(string achievementId)
    {
        if (SteamUserStats.SetAchievement(achievementId))
        {
            SteamUserStats.StoreStats();
        }
    }

    private static void IncrementStat(string statName)
    {
        if (SteamUserStats.GetStat(statName, out int current))
        {
            SteamUserStats.SetStat(statName, current + 1);
            SteamUserStats.StoreStats();
        }
    }
#else
    public static void EnsureInitialized()
    {
    }

    public static void ReportSessionHosted()
    {
    }

    public static void ReportSessionJoined()
    {
    }

    public static void ReportReadyState()
    {
    }

    public static void OpenInviteOverlay(object lobbyId)
    {
    }

    public static void OpenAchievementsOverlay()
    {
    }
#endif
}
