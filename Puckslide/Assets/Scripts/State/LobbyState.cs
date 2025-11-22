using System;
using System.Linq;

[Serializable]
public class LobbySnapshot
{
    public bool HostIsWhite = true;
    public PieceSetupData[] PieceSetup = Array.Empty<PieceSetupData>();

    public static LobbySnapshot Create(PieceSetupData[] setup, bool hostIsWhite)
    {
        return new LobbySnapshot
        {
            HostIsWhite = hostIsWhite,
            PieceSetup = ClonePieceSetup(setup)
        };
    }

    public static PieceSetupData[] ClonePieceSetup(PieceSetupData[] source)
    {
        if (source == null)
        {
            return Array.Empty<PieceSetupData>();
        }

        return source
            .Select(piece => new PieceSetupData
            {
                Type = piece.Type,
                WhiteCount = piece.WhiteCount,
                BlackCount = piece.BlackCount,
                Sticky = piece.Sticky
            })
            .ToArray();
    }
}

public static class LobbyState
{
    private static LobbySnapshot s_LatestSnapshot;

    public static bool LocalIsHost { get; private set; } = true;

    public static bool LocalIsWhitePlayer
    {
        get
        {
            if (s_LatestSnapshot == null)
            {
                return true;
            }

            return LocalIsHost ? s_LatestSnapshot.HostIsWhite : !s_LatestSnapshot.HostIsWhite;
        }
    }

    public static LobbySnapshot LatestSnapshot => s_LatestSnapshot;

    public static void SetLocalHost(bool isHost)
    {
        LocalIsHost = isHost;
    }

    public static void ApplySnapshot(LobbySnapshot snapshot)
    {
        s_LatestSnapshot = snapshot;
        EventsManager.OnLobbySnapshot.Invoke(snapshot);
    }
}
