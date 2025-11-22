using System;
using System.Collections;
using System.Collections.Generic;
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
        [SerializeField]
        private float m_PuckSnapshotInterval = 0.25f;
        [SerializeField]
        private float m_AcceptableLatencyMs = 150f;
        [SerializeField]
        private float m_AcceptablePacketLossPercent = 1.5f;
        [SerializeField]
        private int m_MaxTurnHistory = 5;

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
        private Coroutine m_PuckSnapshotRoutine;
        private uint m_SnapshotVersion;
        private ulong m_LocalPeerId;
        private bool m_IsHost;
        private string m_CurrentLobbyId;
        private LobbySnapshot m_PersistedLobbySnapshot;
        private readonly List<TurnDeterminismMessage> m_TurnHistory = new List<TurnDeterminismMessage>();
        private PlayerCommandDispatcher m_Dispatcher;
        private GridManager m_GridManager;

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

            m_Dispatcher = PlayerCommandDispatcher.Instance ?? FindObjectOfType<PlayerCommandDispatcher>();
            m_GridManager = FindObjectOfType<GridManager>();
            Debug.Log($"Networking tolerances — latency: {m_AcceptableLatencyMs}ms, loss: {m_AcceptablePacketLossPercent}%.");
        }

        private void OnEnable()
        {
            if (ShouldAutoStartHost())
            {
                CreateLobby();
            }

            NetworkEvents.OnPlayerCommandSubmitted.AddListener(OnPlayerCommandSubmitted);
            NetworkEvents.OnTurnChanged.AddListener(OnTurnChangedForDeterminism, true);
        }

        private bool ShouldAutoStartHost()
        {
#if STEAMWORKSNET
            if (SteamworksBootstrap.Instance != null && SteamworksBootstrap.Instance.DrivesNetworkSession)
            {
                return false;
            }
#endif

            return m_StartAsHost;
        }

        private void OnDisable()
        {
            if (m_SnapshotRoutine != null)
            {
                StopCoroutine(m_SnapshotRoutine);
                m_SnapshotRoutine = null;
            }

            if (m_PuckSnapshotRoutine != null)
            {
                StopCoroutine(m_PuckSnapshotRoutine);
                m_PuckSnapshotRoutine = null;
            }

            NetworkEvents.OnPlayerCommandSubmitted.RemoveListener(OnPlayerCommandSubmitted);
            NetworkEvents.OnTurnChanged.RemoveListener(OnTurnChangedForDeterminism);
        }

        public void CreateLobby(string lobbyId = null)
        {
            m_IsHost = true;
            m_CurrentLobbyId = string.IsNullOrEmpty(lobbyId) ? m_CurrentLobbyId : lobbyId;
            LobbyState.SetLocalHost(true);
            m_SnapshotVersion = 0;
            m_PersistedLobbySnapshot = LobbyState.LatestLobbySnapshot ?? new LobbySnapshot
            {
                HostIsWhite = LobbyState.LocalIsWhitePlayer,
                PieceSetup = Array.Empty<PieceSetupData>()
            };
            StartSnapshotLoop();
            StartPuckSnapshotLoop();
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
            StopPuckSnapshotLoop();

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
                StartPuckSnapshotLoop();
            }
            else
            {
                StopSnapshotLoop();
                StopPuckSnapshotLoop();
            }
        }

        public void PublishLobbySnapshot(string reason)
        {
            if (!m_IsHost)
            {
                Debug.LogWarning($"Ignoring snapshot publish ({reason}) because local peer is not host.");
                return;
            }

            LobbySnapshot latest = m_PersistedLobbySnapshot ?? LobbyState.LatestLobbySnapshot ?? new LobbySnapshot
            {
                HostIsWhite = LobbyState.LocalIsWhitePlayer,
                PieceSetup = Array.Empty<PieceSetupData>()
            };

            m_PersistedLobbySnapshot = LobbySnapshot.Create(latest.PieceSetup, latest.HostIsWhite);
            m_SnapshotVersion = Math.Max(m_SnapshotVersion, LobbyState.LatestSnapshotVersion);

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

            m_SnapshotVersion = Math.Max(m_SnapshotVersion, LobbyState.LatestSnapshotVersion);
            PieceSetupMessage message = new PieceSetupMessage
            {
                LobbyId = m_CurrentLobbyId,
                Setup = LobbySnapshot.ClonePieceSetup(setup),
                HostIsWhite = hostIsWhite,
                Version = ++m_SnapshotVersion,
                ServerTime = Time.unscaledTimeAsDouble
            };

            LobbySnapshot snapshot = LobbySnapshot.Create(message.Setup, message.HostIsWhite);
            m_PersistedLobbySnapshot = snapshot;
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
            PublishDeterminismSnapshot(turnNumber);
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

        public void SubmitPlayerCommand(PlayerCommand command)
        {
            if (command.CommandType == PlayerCommandType.PointerUp && command.Target == PlayerCommandTarget.Puck)
            {
                TryApplyCommandAsHost(command);
                return;
            }

            if (!m_IsHost)
            {
                PlayerCommandMessage message = new PlayerCommandMessage
                {
                    LobbyId = m_CurrentLobbyId,
                    Command = command,
                    ClientTime = Time.unscaledTimeAsDouble,
                    ServerTime = 0d
                };

                NetworkEvents.OnPlayerCommandSubmitted.Invoke(message);
            }
            else
            {
                TryApplyCommandAsHost(command);
            }
        }

        public void ReceiveSnapshot(NetworkLobbySnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            m_CurrentLobbyId = snapshot.LobbyId;
            LobbyState.ApplySnapshot(snapshot);
            if (snapshot.HostPeerId == m_LocalPeerId)
            {
                m_SnapshotVersion = Math.Max(m_SnapshotVersion, snapshot.SnapshotVersion);
            }
        }

        private void StartSnapshotLoop()
        {
            StopSnapshotLoop();
            if (m_SnapshotIntervalSeconds > 0f)
            {
                m_SnapshotRoutine = StartCoroutine(SendSnapshots());
            }
        }

        private void StartPuckSnapshotLoop()
        {
            StopPuckSnapshotLoop();
            if (m_PuckSnapshotInterval > 0f)
            {
                m_PuckSnapshotRoutine = StartCoroutine(SendPuckSnapshots());
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

        private void StopPuckSnapshotLoop()
        {
            if (m_PuckSnapshotRoutine != null)
            {
                StopCoroutine(m_PuckSnapshotRoutine);
                m_PuckSnapshotRoutine = null;
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

        private IEnumerator SendPuckSnapshots()
        {
            while (true)
            {
                PublishPuckSnapshot();
                yield return new WaitForSeconds(m_PuckSnapshotInterval);
            }
        }

        private void PublishPuckSnapshot()
        {
            if (!m_IsHost)
            {
                return;
            }

            GameStateSnapshot snapshot = GameStateSnapshot.Capture(m_GridManager);
            PuckStateSnapshotMessage message = new PuckStateSnapshotMessage
            {
                LobbyId = m_CurrentLobbyId,
                IsWhiteTurn = snapshot.IsWhiteTurn,
                TurnNumber = PuckController.TurnNumber,
                IsPhase2Active = snapshot.IsPhase2Active,
                Pucks = snapshot.Pucks.ToArray(),
                ServerTime = Time.unscaledTimeAsDouble
            };

            NetworkEvents.OnPuckSnapshot.Invoke(message);
        }

        private void OnPlayerCommandSubmitted(PlayerCommandMessage message)
        {
            if (!m_IsHost || message == null || message.LobbyId != m_CurrentLobbyId)
            {
                return;
            }

            TryApplyCommandAsHost(message.Command);
        }

        private void TryApplyCommandAsHost(PlayerCommand command)
        {
            if (!m_IsHost && command.CommandType == PlayerCommandType.PointerUp)
            {
                return;
            }

            if (command.Target == PlayerCommandTarget.Puck && !ValidatePuckTurn(command.TargetInstanceId))
            {
                Debug.LogWarning($"Rejected command for puck {command.TargetInstanceId} — not active player's turn.");
                return;
            }

            if (m_Dispatcher == null)
            {
                m_Dispatcher = PlayerCommandDispatcher.Instance ?? FindObjectOfType<PlayerCommandDispatcher>();
            }

            if (m_Dispatcher == null)
            {
                Debug.LogWarning("No PlayerCommandDispatcher found to process commands.");
                return;
            }

            m_Dispatcher.Enqueue(command);
        }

        private bool ValidatePuckTurn(int targetInstanceId)
        {
            if (!PuckControllerRouteHub.TryGet(targetInstanceId, out PuckController puck))
            {
                return false;
            }

            return puck.IsWhitePiece == PuckController.IsWhiteTurn;
        }

        private void PublishDeterminismSnapshot(uint turnNumber)
        {
            if (!m_IsHost)
            {
                return;
            }

            GameStateSnapshot snapshot = GameStateSnapshot.Capture(m_GridManager);
            TurnDeterminismMessage message = new TurnDeterminismMessage
            {
                LobbyId = m_CurrentLobbyId,
                TurnNumber = turnNumber,
                RandomSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                Snapshot = snapshot,
                ServerTime = Time.unscaledTimeAsDouble
            };

            m_TurnHistory.Add(message);
            while (m_TurnHistory.Count > m_MaxTurnHistory)
            {
                m_TurnHistory.RemoveAt(0);
            }

            NetworkEvents.OnTurnDeterminism.Invoke(message);
        }

        private void OnTurnChangedForDeterminism(TurnChangeMessage _)
        {
            if (m_IsHost)
            {
                PublishPuckSnapshot();
            }
        }
    }
}
