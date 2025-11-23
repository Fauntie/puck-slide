#if MIRROR
using Mirror;
using Puckslide.Networking;

namespace Puckslide.Networking
{
    public struct MirrorLobbySnapshotMessage : NetworkMessage
    {
        public NetworkLobbySnapshot Snapshot;
    }

    public struct MirrorPuckSnapshotMessage : NetworkMessage
    {
        public PuckStateSnapshotMessage Snapshot;
    }

    public struct MirrorPlayerCommandMessage : NetworkMessage
    {
        public PlayerCommandMessage Command;
    }

    public struct MirrorPuckSpawnMessage : NetworkMessage
    {
        public PuckSpawnMessage Puck;
    }

    public struct MirrorPuckDespawnMessage : NetworkMessage
    {
        public PuckDespawnMessage Puck;
    }

    public struct MirrorTurnChangeMessage : NetworkMessage
    {
        public TurnChangeMessage Turn;
    }

    public struct MirrorTurnDeterminismMessage : NetworkMessage
    {
        public TurnDeterminismMessage Turn;
    }
}
#endif
