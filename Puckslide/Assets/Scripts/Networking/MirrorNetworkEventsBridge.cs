#if MIRROR
using Mirror;
using UnityEngine;

namespace Puckslide.Networking
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Networking/Mirror Network Events Bridge")]
    public class MirrorNetworkEventsBridge : MonoBehaviour
    {
        private NetworkSessionManager m_Session;

        private NetworkSessionManager Session => m_Session ?? (m_Session = NetworkSessionManager.Instance ?? FindObjectOfType<NetworkSessionManager>());

        private void Awake()
        {
            m_Session = Session;
        }

        private void OnEnable()
        {
            RegisterClientHandlers();
            RegisterServerHandlers();
            SubscribeToNetworkEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromNetworkEvents();
            UnregisterClientHandlers();
            UnregisterServerHandlers();
        }

        private void RegisterClientHandlers()
        {
            NetworkClient.RegisterHandler<MirrorLobbySnapshotMessage>(OnLobbySnapshotReceived);
            NetworkClient.RegisterHandler<MirrorPieceSetupMessage>(OnPieceSetupReceived);
            NetworkClient.RegisterHandler<MirrorTurnChangeMessage>(OnTurnChangedReceived);
            NetworkClient.RegisterHandler<MirrorPuckSpawnMessage>(OnPuckSpawnedReceived);
            NetworkClient.RegisterHandler<MirrorPuckDespawnMessage>(OnPuckDespawnedReceived);
            NetworkClient.RegisterHandler<MirrorShotLaunchMessage>(OnShotLaunchedReceived);
            NetworkClient.RegisterHandler<MirrorPuckStateSnapshotMessage>(OnPuckSnapshotReceived);
            NetworkClient.RegisterHandler<MirrorTurnDeterminismMessage>(OnTurnDeterminismReceived);
        }

        private void UnregisterClientHandlers()
        {
            NetworkClient.UnregisterHandler<MirrorLobbySnapshotMessage>();
            NetworkClient.UnregisterHandler<MirrorPieceSetupMessage>();
            NetworkClient.UnregisterHandler<MirrorTurnChangeMessage>();
            NetworkClient.UnregisterHandler<MirrorPuckSpawnMessage>();
            NetworkClient.UnregisterHandler<MirrorPuckDespawnMessage>();
            NetworkClient.UnregisterHandler<MirrorShotLaunchMessage>();
            NetworkClient.UnregisterHandler<MirrorPuckStateSnapshotMessage>();
            NetworkClient.UnregisterHandler<MirrorTurnDeterminismMessage>();
        }

        private void RegisterServerHandlers()
        {
            NetworkServer.RegisterHandler<MirrorPlayerCommandMessage>(OnPlayerCommandReceived);
        }

        private void UnregisterServerHandlers()
        {
            NetworkServer.UnregisterHandler<MirrorPlayerCommandMessage>();
        }

        private void SubscribeToNetworkEvents()
        {
            NetworkEvents.OnLobbySnapshot.AddListener(OnLobbySnapshotBroadcasted);
            NetworkEvents.OnPieceSetupData.AddListener(OnPieceSetupBroadcasted);
            NetworkEvents.OnTurnChanged.AddListener(OnTurnChangedBroadcasted);
            NetworkEvents.OnNetworkPuckSpawned.AddListener(OnPuckSpawnedBroadcasted);
            NetworkEvents.OnNetworkPuckDespawned.AddListener(OnPuckDespawnedBroadcasted);
            NetworkEvents.OnShotLaunched.AddListener(OnShotLaunchedBroadcasted);
            NetworkEvents.OnPuckSnapshot.AddListener(OnPuckSnapshotBroadcasted);
            NetworkEvents.OnTurnDeterminism.AddListener(OnTurnDeterminismBroadcasted);
            NetworkEvents.OnPlayerCommandSubmitted.AddListener(OnPlayerCommandSubmitted);
        }

        private void UnsubscribeFromNetworkEvents()
        {
            NetworkEvents.OnLobbySnapshot.RemoveListener(OnLobbySnapshotBroadcasted);
            NetworkEvents.OnPieceSetupData.RemoveListener(OnPieceSetupBroadcasted);
            NetworkEvents.OnTurnChanged.RemoveListener(OnTurnChangedBroadcasted);
            NetworkEvents.OnNetworkPuckSpawned.RemoveListener(OnPuckSpawnedBroadcasted);
            NetworkEvents.OnNetworkPuckDespawned.RemoveListener(OnPuckDespawnedBroadcasted);
            NetworkEvents.OnShotLaunched.RemoveListener(OnShotLaunchedBroadcasted);
            NetworkEvents.OnPuckSnapshot.RemoveListener(OnPuckSnapshotBroadcasted);
            NetworkEvents.OnTurnDeterminism.RemoveListener(OnTurnDeterminismBroadcasted);
            NetworkEvents.OnPlayerCommandSubmitted.RemoveListener(OnPlayerCommandSubmitted);
        }

        private bool IsHost() => Session != null && Session.IsHost;

        private bool ShouldSendToClients() => IsHost() && NetworkServer.active;

        private bool ShouldSendToServer() => !IsHost() && NetworkClient.active;

        private static void SendToRemoteClients<T>(T message) where T : struct, NetworkMessage
        {
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection == null)
                {
                    continue;
                }

                if (NetworkServer.localConnection != null && connection.connectionId == NetworkServer.localConnection.connectionId)
                {
                    continue;
                }

                connection.Send(message);
            }
        }

        private void OnLobbySnapshotBroadcasted(NetworkLobbySnapshot snapshot)
        {
            if (snapshot == null || !ShouldSendToClients())
            {
                return;
            }

            SendToRemoteClients(new MirrorLobbySnapshotMessage { Payload = snapshot });
        }

        private void OnPieceSetupBroadcasted(PieceSetupMessage message)
        {
            if (message == null || !ShouldSendToClients())
            {
                return;
            }

            SendToRemoteClients(new MirrorPieceSetupMessage { Payload = message });
        }

        private void OnTurnChangedBroadcasted(TurnChangeMessage message)
        {
            if (message == null || !ShouldSendToClients())
            {
                return;
            }

            SendToRemoteClients(new MirrorTurnChangeMessage { Payload = message });
        }

        private void OnPuckSpawnedBroadcasted(PuckSpawnMessage message)
        {
            if (message == null || !ShouldSendToClients())
            {
                return;
            }

            SendToRemoteClients(new MirrorPuckSpawnMessage { Payload = message });
        }

        private void OnPuckDespawnedBroadcasted(PuckDespawnMessage message)
        {
            if (message == null || !ShouldSendToClients())
            {
                return;
            }

            SendToRemoteClients(new MirrorPuckDespawnMessage { Payload = message });
        }

        private void OnShotLaunchedBroadcasted(ShotLaunchMessage message)
        {
            if (message == null || !ShouldSendToClients())
            {
                return;
            }

            SendToRemoteClients(new MirrorShotLaunchMessage { Payload = message });
        }

        private void OnPuckSnapshotBroadcasted(PuckStateSnapshotMessage message)
        {
            if (message == null || !ShouldSendToClients())
            {
                return;
            }

            SendToRemoteClients(new MirrorPuckStateSnapshotMessage { Payload = message });
        }

        private void OnTurnDeterminismBroadcasted(TurnDeterminismMessage message)
        {
            if (message == null || !ShouldSendToClients())
            {
                return;
            }

            SendToRemoteClients(new MirrorTurnDeterminismMessage { Payload = message });
        }

        private void OnPlayerCommandSubmitted(PlayerCommandMessage message)
        {
            if (message == null || !ShouldSendToServer())
            {
                return;
            }

            NetworkClient.Send(new MirrorPlayerCommandMessage { Payload = message });
        }

        private void OnLobbySnapshotReceived(MirrorLobbySnapshotMessage message)
        {
            if (IsHost() || message.Payload == null)
            {
                return;
            }

            NetworkEvents.OnLobbySnapshot.Invoke(message.Payload);
        }

        private void OnPieceSetupReceived(MirrorPieceSetupMessage message)
        {
            if (IsHost() || message.Payload == null)
            {
                return;
            }

            NetworkEvents.OnPieceSetupData.Invoke(message.Payload);
        }

        private void OnTurnChangedReceived(MirrorTurnChangeMessage message)
        {
            if (IsHost() || message.Payload == null)
            {
                return;
            }

            NetworkEvents.OnTurnChanged.Invoke(message.Payload);
        }

        private void OnPuckSpawnedReceived(MirrorPuckSpawnMessage message)
        {
            if (IsHost() || message.Payload == null)
            {
                return;
            }

            NetworkEvents.OnNetworkPuckSpawned.Invoke(message.Payload);
        }

        private void OnPuckDespawnedReceived(MirrorPuckDespawnMessage message)
        {
            if (IsHost() || message.Payload == null)
            {
                return;
            }

            NetworkEvents.OnNetworkPuckDespawned.Invoke(message.Payload);
        }

        private void OnShotLaunchedReceived(MirrorShotLaunchMessage message)
        {
            if (IsHost() || message.Payload == null)
            {
                return;
            }

            NetworkEvents.OnShotLaunched.Invoke(message.Payload);
        }

        private void OnPuckSnapshotReceived(MirrorPuckStateSnapshotMessage message)
        {
            if (IsHost() || message.Payload == null)
            {
                return;
            }

            NetworkEvents.OnPuckSnapshot.Invoke(message.Payload);
        }

        private void OnTurnDeterminismReceived(MirrorTurnDeterminismMessage message)
        {
            if (IsHost() || message.Payload == null)
            {
                return;
            }

            NetworkEvents.OnTurnDeterminism.Invoke(message.Payload);
        }

        private void OnPlayerCommandReceived(NetworkConnectionToClient conn, MirrorPlayerCommandMessage message)
        {
            if (message.Payload == null)
            {
                return;
            }

            NetworkEvents.OnPlayerCommandSubmitted.Invoke(message.Payload);
        }
    }
}
#endif
