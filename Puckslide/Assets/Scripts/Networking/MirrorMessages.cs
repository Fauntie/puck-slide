#if MIRROR
using Mirror;

namespace Puckslide.Networking
{
    public struct MirrorLobbySnapshotMessage : NetworkMessage
    {
        public NetworkLobbySnapshot Payload;
    }

    public struct MirrorPieceSetupMessage : NetworkMessage
    {
        public PieceSetupMessage Payload;
    }

    public struct MirrorTurnChangeMessage : NetworkMessage
    {
        public TurnChangeMessage Payload;
    }

    public struct MirrorPuckSpawnMessage : NetworkMessage
    {
        public PuckSpawnMessage Payload;
    }

    public struct MirrorPuckDespawnMessage : NetworkMessage
    {
        public PuckDespawnMessage Payload;
    }

    public struct MirrorShotLaunchMessage : NetworkMessage
    {
        public ShotLaunchMessage Payload;
    }

    public struct MirrorPlayerCommandMessage : NetworkMessage
    {
        public PlayerCommandMessage Payload;
    }

    public struct MirrorPuckStateSnapshotMessage : NetworkMessage
    {
        public PuckStateSnapshotMessage Payload;
    }

    public struct MirrorTurnDeterminismMessage : NetworkMessage
    {
        public TurnDeterminismMessage Payload;
    }
}
#endif
