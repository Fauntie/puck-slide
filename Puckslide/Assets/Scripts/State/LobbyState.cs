using System;
using System.Linq;
using Puckslide.Networking;

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
    private static NetworkLobbySnapshot s_LatestSnapshot;

    public static bool LocalIsHost { get; private set; } = true;
    public static string LocalPeerId { get; private set; }
    public static bool LocalIsWhitePlayer { get; private set; } = true;
    public static NetworkLobbySnapshot LatestSnapshot => s_LatestSnapshot;
    public static NetworkLobbySnapshot LatestNetworkSnapshot => s_LatestSnapshot;
    public static LobbySnapshot LatestLobbySnapshot => s_LatestSnapshot?.Snapshot;
    public static uint LatestSnapshotVersion => s_LatestSnapshot?.SnapshotVersion ?? 0u;

    public static void SetLocalHost(bool isHost)
    {
        LocalIsHost = isHost;
    }

    public static void SetLocalPeerId(string peerId)
    {
        LocalPeerId = peerId;
    }

    public static void ApplySnapshot(NetworkLobbySnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        string currentLobbyId = NetworkSessionManager.Instance?.LobbyId;
        if (!string.IsNullOrEmpty(currentLobbyId) && snapshot.LobbyId != currentLobbyId)
        {
            return;
        }

        if (snapshot.Snapshot == null)
        {
            snapshot.Snapshot = new LobbySnapshot
            {
                HostIsWhite = true,
                PieceSetup = Array.Empty<PieceSetupData>()
            };
        }

        bool isNewLobby = s_LatestSnapshot == null || s_LatestSnapshot.LobbyId != snapshot.LobbyId;
        if (!isNewLobby && snapshot.SnapshotVersion <= s_LatestSnapshot.SnapshotVersion)
        {
            return;
        }

        s_LatestSnapshot = snapshot;
        LocalIsHost = snapshot.HostPeerId == LocalPeerId && snapshot.HostIsAuthoritative;

        bool hostIsWhite = snapshot.Snapshot.HostIsWhite;
        if (LocalIsHost)
        {
            LocalIsWhitePlayer = hostIsWhite;
        }
        else
        {
            LocalIsWhitePlayer = !hostIsWhite;
        }

        NetworkEvents.OnLobbySnapshot.Invoke(snapshot);
    }
}
