#if MIRROR
using Mirror;
using UnityEngine;

namespace Puckslide.Networking
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Networking/Mirror Network Bridge")]
    public class MirrorNetworkBridge : MonoBehaviour
    {
        public static MirrorNetworkBridge Instance { get; private set; }

        private NetworkSessionManager Session => NetworkSessionManager.Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            RegisterClientHandlers();
            RegisterServerHandlers();
        }

        private void OnEnable()
        {
            SubscribeToNetworkEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromNetworkEvents();
            UnregisterClientHandlers();
            UnregisterServerHandlers();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private bool IsHost => Session != null && Session.IsHost;

        /// <summary>
        /// Called by non-host clients to send a PlayerCommandMessage to the host.
        /// </summary>
        public void SendPlayerCommand(PlayerCommandMessage message)
        {
            // Clients only
            if (!NetworkClient.active || NetworkServer.active)
            {
                // Either not connected or we are the host; do nothing here.
                return;
            }

            NetworkClient.Send(new MirrorPlayerCommandMessage
            {
                Command = message
            });
        }

        private void RegisterClientHandlers()
        {
            NetworkClient.RegisterHandler<MirrorLobbySnapshotMessage>(OnMirrorLobbySnapshotReceived);
            NetworkClient.RegisterHandler<MirrorGameStartMessage>(OnMirrorGameStartReceived);
            NetworkClient.RegisterHandler<MirrorPuckSnapshotMessage>(OnMirrorPuckSnapshotReceived);
            NetworkClient.RegisterHandler<MirrorPuckSpawnMessage>(OnMirrorPuckSpawnReceived);
            NetworkClient.RegisterHandler<MirrorPuckDespawnMessage>(OnMirrorPuckDespawnReceived);
            NetworkClient.RegisterHandler<MirrorTurnChangeMessage>(OnMirrorTurnChangeReceived);
            NetworkClient.RegisterHandler<MirrorTurnDeterminismMessage>(OnMirrorTurnDeterminismReceived);
        }

        private void UnregisterClientHandlers()
        {
            NetworkClient.UnregisterHandler<MirrorLobbySnapshotMessage>();
            NetworkClient.UnregisterHandler<MirrorGameStartMessage>();
            NetworkClient.UnregisterHandler<MirrorPuckSnapshotMessage>();
            NetworkClient.UnregisterHandler<MirrorPuckSpawnMessage>();
            NetworkClient.UnregisterHandler<MirrorPuckDespawnMessage>();
            NetworkClient.UnregisterHandler<MirrorTurnChangeMessage>();
            NetworkClient.UnregisterHandler<MirrorTurnDeterminismMessage>();
        }

        private void RegisterServerHandlers()
        {
            NetworkServer.RegisterHandler<MirrorLobbySnapshotMessage>(OnMirrorLobbySnapshotReceived);
            NetworkServer.RegisterHandler<MirrorGameStartMessage>(OnMirrorGameStartReceived);
            NetworkServer.RegisterHandler<MirrorPuckSnapshotMessage>(OnMirrorPuckSnapshotReceived);
            NetworkServer.RegisterHandler<MirrorPuckSpawnMessage>(OnMirrorPuckSpawnReceived);
            NetworkServer.RegisterHandler<MirrorPuckDespawnMessage>(OnMirrorPuckDespawnReceived);
            NetworkServer.RegisterHandler<MirrorTurnChangeMessage>(OnMirrorTurnChangeReceived);
            NetworkServer.RegisterHandler<MirrorTurnDeterminismMessage>(OnMirrorTurnDeterminismReceived);
            NetworkServer.RegisterHandler<MirrorPlayerCommandMessage>(OnMirrorPlayerCommandReceived);
        }

        private void UnregisterServerHandlers()
        {
            NetworkServer.UnregisterHandler<MirrorLobbySnapshotMessage>();
            NetworkServer.UnregisterHandler<MirrorGameStartMessage>();
            NetworkServer.UnregisterHandler<MirrorPuckSnapshotMessage>();
            NetworkServer.UnregisterHandler<MirrorPuckSpawnMessage>();
            NetworkServer.UnregisterHandler<MirrorPuckDespawnMessage>();
            NetworkServer.UnregisterHandler<MirrorTurnChangeMessage>();
            NetworkServer.UnregisterHandler<MirrorTurnDeterminismMessage>();
            NetworkServer.UnregisterHandler<MirrorPlayerCommandMessage>();
        }

        private void SubscribeToNetworkEvents()
        {
            NetworkEvents.OnLobbySnapshot.AddListener(OnLobbySnapshotBroadcasted);
            NetworkEvents.OnGameStart.AddListener(OnLocalGameStart);
            NetworkEvents.OnPuckSnapshot.AddListener(OnPuckSnapshotBroadcasted);
            NetworkEvents.OnNetworkPuckSpawned.AddListener(OnPuckSpawnBroadcasted);
            NetworkEvents.OnNetworkPuckDespawned.AddListener(OnPuckDespawnBroadcasted);
            NetworkEvents.OnTurnChanged.AddListener(OnTurnChangedBroadcasted);
            NetworkEvents.OnTurnDeterminism.AddListener(OnTurnDeterminismBroadcasted);
            NetworkEvents.OnPlayerCommandSubmitted.AddListener(OnPlayerCommandSubmitted);
        }

        private void UnsubscribeFromNetworkEvents()
        {
            NetworkEvents.OnLobbySnapshot.RemoveListener(OnLobbySnapshotBroadcasted);
            NetworkEvents.OnGameStart.RemoveListener(OnLocalGameStart);
            NetworkEvents.OnPuckSnapshot.RemoveListener(OnPuckSnapshotBroadcasted);
            NetworkEvents.OnNetworkPuckSpawned.RemoveListener(OnPuckSpawnBroadcasted);
            NetworkEvents.OnNetworkPuckDespawned.RemoveListener(OnPuckDespawnBroadcasted);
            NetworkEvents.OnTurnChanged.RemoveListener(OnTurnChangedBroadcasted);
            NetworkEvents.OnTurnDeterminism.RemoveListener(OnTurnDeterminismBroadcasted);
            NetworkEvents.OnPlayerCommandSubmitted.RemoveListener(OnPlayerCommandSubmitted);
        }

        private void OnLobbySnapshotBroadcasted(NetworkLobbySnapshot snapshot)
        {
            if (!IsHost || snapshot == null)
            {
                return;
            }

            if (!NetworkServer.active)
            {
                return;
            }

            NetworkServer.SendToAll(new MirrorLobbySnapshotMessage { Snapshot = snapshot });
        }

        private void OnPuckSnapshotBroadcasted(PuckStateSnapshotMessage snapshot)
        {
            if (!IsHost || snapshot == null)
            {
                return;
            }

            if (!NetworkServer.active)
            {
                return;
            }

            NetworkServer.SendToAll(new MirrorPuckSnapshotMessage { Snapshot = snapshot });
        }

        private void OnPuckSpawnBroadcasted(PuckSpawnMessage puck)
        {
            if (!IsHost || puck == null)
            {
                return;
            }

            NetworkServer.SendToAll(new MirrorPuckSpawnMessage { Puck = puck });
        }

        private void OnPuckDespawnBroadcasted(PuckDespawnMessage puck)
        {
            if (!IsHost || puck == null)
            {
                return;
            }

            NetworkServer.SendToAll(new MirrorPuckDespawnMessage { Puck = puck });
        }

        private void OnTurnChangedBroadcasted(TurnChangeMessage turn)
        {
            if (!IsHost || turn == null)
            {
                return;
            }

            NetworkServer.SendToAll(new MirrorTurnChangeMessage { Turn = turn });
        }

        private void OnTurnDeterminismBroadcasted(TurnDeterminismMessage turn)
        {
            if (!IsHost || turn == null)
            {
                return;
            }

            NetworkServer.SendToAll(new MirrorTurnDeterminismMessage { Turn = turn });
        }

        private void OnLocalGameStart(GameStartMessage message)
        {
            if (!IsHost)
            {
                return;
            }

            if (!NetworkServer.active)
            {
                return;
            }

            MirrorGameStartMessage mirrorMsg = new MirrorGameStartMessage
            {
                Message = message
            };

            NetworkServer.SendToAll(mirrorMsg);
        }

        private void OnPlayerCommandSubmitted(PlayerCommandMessage command)
        {
            if (command == null || IsHost)
            {
                return;
            }

            NetworkClient.Send(new MirrorPlayerCommandMessage { Command = command });
        }

        private void OnMirrorLobbySnapshotReceived(NetworkConnection conn, MirrorLobbySnapshotMessage msg)
        {
            LobbyState.ApplySnapshot(msg.Snapshot);
        }

        private void OnMirrorGameStartReceived(NetworkConnection conn, MirrorGameStartMessage msg)
        {
            GameStartMessage message = msg.Message;

            NetworkSessionManager manager = NetworkSessionManager.Instance;
            if (manager != null && !string.IsNullOrEmpty(manager.LobbyId))
            {
                if (message.LobbyId != manager.LobbyId)
                {
                    return;
                }
            }

            NetworkEvents.OnGameStart.Invoke(message);
        }

        private void OnMirrorPuckSnapshotReceived(NetworkConnection conn, MirrorPuckSnapshotMessage msg)
        {
            NetworkEvents.OnPuckSnapshot.Invoke(msg.Snapshot);
        }

        private void OnMirrorPuckSpawnReceived(NetworkConnection conn, MirrorPuckSpawnMessage msg)
        {
            NetworkEvents.OnNetworkPuckSpawned.Invoke(msg.Puck);
        }

        private void OnMirrorPuckDespawnReceived(NetworkConnection conn, MirrorPuckDespawnMessage msg)
        {
            NetworkEvents.OnNetworkPuckDespawned.Invoke(msg.Puck);
        }

        private void OnMirrorTurnChangeReceived(NetworkConnection conn, MirrorTurnChangeMessage msg)
        {
            NetworkEvents.OnTurnChanged.Invoke(msg.Turn);
        }

        private void OnMirrorTurnDeterminismReceived(NetworkConnection conn, MirrorTurnDeterminismMessage msg)
        {
            NetworkEvents.OnTurnDeterminism.Invoke(msg.Turn);
        }

        private void OnMirrorPlayerCommandReceived(NetworkConnectionToClient conn, MirrorPlayerCommandMessage msg)
        {
            // Server-only: this should only run on host.
            if (!NetworkServer.active)
            {
                return;
            }

            PlayerCommandMessage commandMessage = msg.Command;

            // Optional validation against current lobby.
            NetworkSessionManager manager = NetworkSessionManager.Instance;
            if (manager != null && !string.IsNullOrEmpty(manager.LobbyId))
            {
                if (commandMessage.LobbyId != manager.LobbyId)
                {
                    return;
                }
            }

            NetworkEvents.OnPlayerCommandSubmitted.Invoke(commandMessage);
        }
    }
}
#endif
