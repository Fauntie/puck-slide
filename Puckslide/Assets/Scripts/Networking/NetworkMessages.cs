using System;
using UnityEngine;

namespace Puckslide.Networking
{
    [Serializable]
    public class NetworkLobbySnapshot
    {
        public string LobbyId;
        public LobbySnapshot Snapshot;
        public bool HostIsAuthoritative;
        public ulong HostPeerId;
        public uint SnapshotVersion;
        public double ServerTime;
    }

    [Serializable]
    public class PieceSetupMessage
    {
        public string LobbyId;
        public PieceSetupData[] Setup;
        public bool HostIsWhite;
        public uint Version;
        public double ServerTime;
    }

    [Serializable]
    public class TurnChangeMessage
    {
        public string LobbyId;
        public bool IsWhiteTurn;
        public uint TurnNumber;
        public string Reason;
        public double ServerTime;
    }

    [Serializable]
    public class PuckSpawnMessage
    {
        public string LobbyId;
        public int NetworkInstanceId;
        public bool IsWhitePiece;
        public Vector2 Position;
        public Vector2 Velocity;
        public string SpawnReason;
        public double ServerTime;
    }

    [Serializable]
    public class PuckDespawnMessage
    {
        public string LobbyId;
        public int NetworkInstanceId;
        public string Reason;
        public double ServerTime;
    }

    [Serializable]
    public class ShotLaunchMessage
    {
        public string LobbyId;
        public int TargetInstanceId;
        public Vector2 Direction;
        public float Force;
        public int PlayerId;
        public double ClientTime;
        public double ServerTime;
    }

    [Serializable]
    public class PlayerCommandMessage
    {
        public string LobbyId;
        public PlayerCommand Command;
        public double ClientTime;
        public double ServerTime;
    }

    [Serializable]
    public class PuckStateSnapshotMessage
    {
        public string LobbyId;
        public bool IsWhiteTurn;
        public uint TurnNumber;
        public bool IsPhase2Active;
        public PuckState[] Pucks;
        public double ServerTime;
    }

    [Serializable]
    public class TurnDeterminismMessage
    {
        public string LobbyId;
        public uint TurnNumber;
        public int RandomSeed;
        public GameStateSnapshot Snapshot;
        public double ServerTime;
    }
}
