using System;
using System;
using System.Collections;
using UnityEngine;

#if MIRROR
using Mirror;
#if STEAMWORKSNET
using Mirror.FizzySteam;
#endif
#endif

namespace Puckslide.Networking
{
    [DefaultExecutionOrder(-200)]
    [AddComponentMenu("Networking/Network Session Manager")]
    public class NetworkSessionManager : MonoBehaviour
    {
        [SerializeField]
        private string m_DefaultLobbyId = "local";
        [SerializeField]
        private float m_SnapshotIntervalSeconds = 3f;
        [SerializeField]
        private bool m_StartAsHost = true;

#if MIRROR
        [SerializeField]
        private NetworkManager m_NetworkManager;
#if STEAMWORKSNET
        [SerializeField]
        private FizzySteamyMirror m_SteamTransport;
#endif
#endif

        private static NetworkSessionManager s_Instance;
        private Coroutine m_SnapshotRoutine;
        private uint m_SnapshotVersion;
        private ulong m_LocalPeerId;
        private bool m_IsHost;
        private string m_CurrentLobbyId;

        public static NetworkSessionManager Instance => s_Instance;
        public bool IsHost => m_IsHost;
        public string LobbyId => m_CurrentLobbyId;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            m_CurrentLobbyId = string.IsNullOrEmpty(m_DefaultLobbyId) ? Guid.NewGuid().ToString("N") : m_DefaultLobbyId;
            m_LocalPeerId = (ulong)UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            LobbyState.SetLocalPeerId(m_LocalPeerId);
        }

        private void OnEnable()
        {
            if (m_StartAsHost)
            {
                CreateLobby();
            }
        }

        private void OnDisable()
        {
            if (m_SnapshotRoutine != null)
            {
                StopCoroutine(m_SnapshotRoutine);
                m_SnapshotRoutine = null;
            }
        }

        public void CreateLobby(string lobbyId = null)
        {
            m_IsHost = true;
            m_CurrentLobbyId = string.IsNullOrEmpty(lobbyId) ? m_CurrentLobbyId : lobbyId;
            LobbyState.SetLocalHost(true);
            m_SnapshotVersion = 0;
            StartSnapshotLoop();
            PublishLobbySnapshot("host-created");

#if MIRROR
            if (m_NetworkManager != null)
            {
                m_NetworkManager.StartHost();
            }
#endif
        }

        public void JoinLobby(string lobbyId)
        {
            m_IsHost = false;
            m_CurrentLobbyId = lobbyId;
            LobbyState.SetLocalHost(false);
            StopSnapshotLoop();

#if MIRROR
            if (m_NetworkManager != null)
            {
                m_NetworkManager.networkAddress = lobbyId;
                m_NetworkManager.StartClient();
            }
#endif
        }

        public void PromoteNewHost(ulong hostPeerId)
        {
            bool localIsNewHost = hostPeerId == m_LocalPeerId;
            m_IsHost = localIsNewHost;
            LobbyState.SetLocalHost(localIsNewHost);
            if (localIsNewHost)
            {
                PublishLobbySnapshot("host-migrated");
                StartSnapshotLoop();
            }
            else
            {
                StopSnapshotLoop();
            }
        }

        public void PublishLobbySnapshot(string reason)
        {
            if (!m_IsHost)
            {
                Debug.LogWarning($"Ignoring snapshot publish ({reason}) because local peer is not host.");
                return;
            }

            LobbySnapshot latest = LobbyState.LatestLobbySnapshot ?? new LobbySnapshot
            {
                HostIsWhite = LobbyState.LocalIsWhitePlayer,
                PieceSetup = Array.Empty<PieceSetupData>()
            };

            NetworkLobbySnapshot snapshot = new NetworkLobbySnapshot
            {
                LobbyId = m_CurrentLobbyId,
                Snapshot = LobbySnapshot.Create(latest.PieceSetup, latest.HostIsWhite),
                HostIsAuthoritative = m_IsHost,
                HostPeerId = m_LocalPeerId,
                SnapshotVersion = ++m_SnapshotVersion,
                ServerTime = Time.unscaledTimeAsDouble
            };

            LobbyState.ApplySnapshot(snapshot);
        }

        public void BroadcastPieceSetup(PieceSetupData[] setup, bool hostIsWhite)
        {
            if (!m_IsHost)
            {
                Debug.LogWarning("Non-host attempted to broadcast piece setup.");
                return;
            }

            PieceSetupMessage message = new PieceSetupMessage
            {
                LobbyId = m_CurrentLobbyId,
                Setup = LobbySnapshot.ClonePieceSetup(setup),
                HostIsWhite = hostIsWhite,
                Version = ++m_SnapshotVersion,
                ServerTime = Time.unscaledTimeAsDouble
            };

            LobbySnapshot snapshot = LobbySnapshot.Create(message.Setup, message.HostIsWhite);
            LobbyState.ApplySnapshot(new NetworkLobbySnapshot
            {
                LobbyId = message.LobbyId,
                Snapshot = snapshot,
                HostIsAuthoritative = m_IsHost,
                HostPeerId = m_LocalPeerId,
                SnapshotVersion = message.Version,
                ServerTime = message.ServerTime
            });

            NetworkEvents.OnPieceSetupData.Invoke(message);
        }

        public void BroadcastTurnChange(bool isWhiteTurn, uint turnNumber, string reason)
        {
            if (!m_IsHost)
            {
                Debug.LogWarning($"Non-host attempted to broadcast turn change ({reason}).");
                return;
            }

            TurnChangeMessage message = new TurnChangeMessage
            {
                LobbyId = m_CurrentLobbyId,
                IsWhiteTurn = isWhiteTurn,
                TurnNumber = turnNumber,
                Reason = reason,
                ServerTime = Time.unscaledTimeAsDouble
            };

            NetworkEvents.OnTurnChanged.Invoke(message);
        }

        public void BroadcastPuckSpawn(PuckController puck, string spawnReason)
        {
            if (puck == null)
            {
                return;
            }

            if (!m_IsHost)
            {
                Debug.LogWarning("Non-host attempted to broadcast puck spawn.");
                return;
            }

            NetworkEvents.OnPuckSpawned.Invoke(puck.Rigidbody);

            PuckSpawnMessage message = new PuckSpawnMessage
            {
                LobbyId = m_CurrentLobbyId,
                NetworkInstanceId = puck.GetInstanceID(),
                IsWhitePiece = puck.IsWhitePiece,
                Position = puck.transform.position,
                Velocity = puck.Velocity,
                SpawnReason = spawnReason,
                ServerTime = Time.unscaledTimeAsDouble
            };

            NetworkEvents.OnNetworkPuckSpawned.Invoke(message);
        }

        public void BroadcastPuckDespawn(Rigidbody2D body, string reason)
        {
            if (!m_IsHost)
            {
                Debug.LogWarning("Non-host attempted to broadcast puck despawn.");
                return;
            }

            NetworkEvents.OnPuckDespawned.Invoke(body);

            NetworkEvents.OnNetworkPuckDespawned.Invoke(new PuckDespawnMessage
            {
                LobbyId = m_CurrentLobbyId,
                NetworkInstanceId = body != null ? body.GetInstanceID() : -1,
                Reason = reason,
                ServerTime = Time.unscaledTimeAsDouble
            });
        }

        public void BroadcastShot(ShotLaunchMessage message)
        {
            if (!m_IsHost)
            {
                Debug.LogWarning("Non-host attempted to relay shot input.");
                return;
            }

            message.LobbyId = m_CurrentLobbyId;
            message.ServerTime = Time.unscaledTimeAsDouble;
            NetworkEvents.OnShotLaunched.Invoke(message);
        }

        public void ReceiveSnapshot(NetworkLobbySnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            m_CurrentLobbyId = snapshot.LobbyId;
            LobbyState.ApplySnapshot(snapshot);
        }

        private void StartSnapshotLoop()
        {
            StopSnapshotLoop();
            if (m_SnapshotIntervalSeconds > 0f)
            {
                m_SnapshotRoutine = StartCoroutine(SendSnapshots());
            }
        }

        private void StopSnapshotLoop()
        {
            if (m_SnapshotRoutine != null)
            {
                StopCoroutine(m_SnapshotRoutine);
                m_SnapshotRoutine = null;
            }
        }

        private IEnumerator SendSnapshots()
        {
            while (true)
            {
                PublishLobbySnapshot("heartbeat");
                yield return new WaitForSeconds(m_SnapshotIntervalSeconds);
            }
        }
    }
}
