using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Puckslide;

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
        [SerializeField]
        private float m_ConnectionTimeoutSeconds = 10f;
        [SerializeField]
        private bool m_OfflineMode;
        [SerializeField]
        private BuildConfig m_BuildConfig;
        [SerializeField]
        private NetworkDiagnostics m_Diagnostics;

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
        private string m_LocalPeerId;
        private bool m_IsHost;
        private string m_CurrentLobbyId;
        private LobbySnapshot m_PersistedLobbySnapshot;
        private readonly List<TurnDeterminismMessage> m_TurnHistory = new List<TurnDeterminismMessage>();
        private PlayerCommandDispatcher m_Dispatcher;
        private GridManager m_GridManager;
        private double m_LastSnapshotReceivedTime;
        private bool m_ConnectionTimedOut;

        public static NetworkSessionManager Instance => s_Instance;
        public bool IsHost => m_IsHost;
        public string LobbyId => m_CurrentLobbyId;
        public bool OfflineMode => m_OfflineMode;
        public NetworkDiagnostics Diagnostics => m_Diagnostics;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            m_CurrentLobbyId = string.IsNullOrEmpty(m_DefaultLobbyId) ? Guid.NewGuid().ToString("N") : m_DefaultLobbyId;
            m_LocalPeerId = Guid.NewGuid().ToString("N");
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
            NetworkEvents.OnPuckSnapshot.AddListener(OnAnyPuckSnapshot);
        }

        private bool ShouldAutoStartHost()
        {
#if STEAMWORKSNET
            if (SteamworksBootstrap.Instance != null && SteamworksBootstrap.Instance.DrivesNetworkSession)
            {
                return false;
            }
#endif

            if (m_OfflineMode)
            {
                return false;
            }

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
            NetworkEvents.OnPuckSnapshot.RemoveListener(OnAnyPuckSnapshot);
        }

        private void Update()
        {
            if (m_OfflineMode)
            {
                return;
            }

            if (m_ConnectionTimedOut)
            {
                return;
            }

            if (m_ConnectionTimeoutSeconds > 0f && m_LastSnapshotReceivedTime > 0d)
            {
                double now = Time.unscaledTimeAsDouble;
                if (now - m_LastSnapshotReceivedTime > m_ConnectionTimeoutSeconds)
                {
                    m_ConnectionTimedOut = true;
                    Debug.LogWarning("[Networking] Connection timed out (no snapshots received).");
                    NetworkEvents.OnDisconnected.Invoke(NetworkDisconnectReason.Timeout);
                }
            }
        }

        public void CreateLobby(string lobbyId = null)
        {
            if (m_OfflineMode)
            {
                m_IsHost = true;
                LobbyState.SetLocalHost(true);
                m_CurrentLobbyId = string.IsNullOrEmpty(lobbyId) ? m_CurrentLobbyId : lobbyId;
                return;
            }

            m_IsHost = true;
            m_CurrentLobbyId = string.IsNullOrEmpty(lobbyId) ? m_CurrentLobbyId : lobbyId;
            m_LastSnapshotReceivedTime = Time.unscaledTimeAsDouble;
            m_ConnectionTimedOut = false;
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
            if (m_NetworkManager != null && IsMirrorEnabled())
            {
                m_NetworkManager.StartHost();
            }
#endif
        }

        public void JoinLobby(string lobbyId)
        {
            if (m_OfflineMode)
            {
                Debug.Log("[Networking] Offline mode is enabled; ignoring JoinLobby call.");
                return;
            }

            m_IsHost = false;
            m_CurrentLobbyId = lobbyId;
            m_LastSnapshotReceivedTime = Time.unscaledTimeAsDouble;
            m_ConnectionTimedOut = false;
            LobbyState.SetLocalHost(false);
            StopSnapshotLoop();
            StopPuckSnapshotLoop();

#if MIRROR
            if (m_NetworkManager != null && IsMirrorEnabled())
            {
                m_NetworkManager.networkAddress = lobbyId;
                m_NetworkManager.StartClient();
            }
#endif
        }

        public void PromoteNewHost(string hostPeerId)
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

        public void StartMatchAsHost()
        {
            if (!m_IsHost)
            {
                Debug.LogWarning("Only host can start the match.");
                return;
            }

            GameStartMessage message = new GameStartMessage
            {
                LobbyId = m_CurrentLobbyId,
                ServerTime = Time.unscaledTimeAsDouble
            };

            NetworkEvents.OnGameStart.Invoke(message);
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

        public void UpdateHostPieceSetup(PieceSetupData[] setupData, bool? hostIsWhiteOverride = null)
        {
            if (!m_IsHost)
            {
                Debug.LogWarning("Only host should call UpdateHostPieceSetup.");
                return;
            }

            LobbySnapshot latest = m_PersistedLobbySnapshot ?? LobbyState.LatestLobbySnapshot ?? new LobbySnapshot
            {
                HostIsWhite = LobbyState.LocalIsWhitePlayer,
                PieceSetup = Array.Empty<PieceSetupData>()
            };

            bool hostIsWhite = hostIsWhiteOverride ?? latest.HostIsWhite;
            m_PersistedLobbySnapshot = LobbySnapshot.Create(setupData, hostIsWhite);

            PublishLobbySnapshot("piece-setup-updated");
        }

        public void BroadcastPieceSetup(PieceSetupData[] setup, bool hostIsWhite)
        {
            if (!m_IsHost)
            {
                Debug.LogWarning("Non-host attempted to broadcast piece setup.");
                return;
            }

            UpdateHostPieceSetup(setup, hostIsWhite);

            m_SnapshotVersion = Math.Max(m_SnapshotVersion, LobbyState.LatestSnapshotVersion);
            PieceSetupMessage message = new PieceSetupMessage
            {
                LobbyId = m_CurrentLobbyId,
                Setup = LobbySnapshot.ClonePieceSetup(setup),
                HostIsWhite = hostIsWhite,
                Version = m_SnapshotVersion,
                ServerTime = Time.unscaledTimeAsDouble
            };

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

        // Submit a player command according to session authority and transport availability.
        public void SubmitPlayerCommand(PlayerCommand command)
        {
            if (m_OfflineMode)
            {
                TryApplyCommandAsHost(command);
                return;
            }

            // Host: apply command directly into the simulation.
            if (m_IsHost)
            {
                TryApplyCommandAsHost(command);
                return;
            }

            // Client: wrap into a PlayerCommandMessage
            PlayerCommandMessage message = new PlayerCommandMessage
            {
                LobbyId = m_CurrentLobbyId,
                Command = command,
                ClientTime = Time.unscaledTimeAsDouble,
                ServerTime = 0d
            };

#if MIRROR
            // If we have a Mirror bridge and a live client connection,
            // send the command to the host over the network.
            if (MirrorNetworkBridge.Instance != null && IsMirrorEnabled())
            {
                MirrorNetworkBridge.Instance.SendPlayerCommand(message);
                return;
            }
#endif

            // No transport available: drop the command to preserve host authority.
            Debug.LogWarning("Dropping player command — no active network connection and not in offline mode.");
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
            if (m_OfflineMode)
            {
                return;
            }

            StopSnapshotLoop();
            if (m_SnapshotIntervalSeconds > 0f)
            {
                m_SnapshotRoutine = StartCoroutine(SendSnapshots());
            }
        }

        private void StartPuckSnapshotLoop()
        {
            if (m_OfflineMode)
            {
                return;
            }

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
            if (!m_IsHost)
            {
                Debug.LogWarning("Rejected command application — local peer is not the host.");
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

        public void HandleDisconnect(NetworkDisconnectReason reason)
        {
            StopSnapshotLoop();
            StopPuckSnapshotLoop();
            m_ConnectionTimedOut = false;
            m_LastSnapshotReceivedTime = 0d;
            Debug.Log($"[Networking] Handling disconnect ({reason}).");

#if MIRROR
            // Ensure Mirror connections are torn down when the session ends, even without a dedicated handler.
            try
            {
                NetworkManager networkManager = m_NetworkManager != null ? m_NetworkManager : FindObjectOfType<NetworkManager>();
                if (networkManager != null && networkManager.isNetworkActive)
                {
                    if (m_IsHost)
                    {
                        networkManager.StopHost();
                    }
                    else
                    {
                        networkManager.StopClient();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Networking] Failed to shut down Mirror transport: {ex.Message}");
            }
#endif
        }

        private void OnAnyPuckSnapshot(PuckStateSnapshotMessage snapshot)
        {
            m_LastSnapshotReceivedTime = Time.unscaledTimeAsDouble;
            m_ConnectionTimedOut = false;
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

        public void SetOfflineMode(bool offline)
        {
            m_OfflineMode = offline;
            if (offline)
            {
                m_IsHost = true;
                LobbyState.SetLocalHost(true);
                StopSnapshotLoop();
                StopPuckSnapshotLoop();
            }
        }

        private bool IsMirrorEnabled()
        {
            BuildConfig config = ResolveBuildConfig();
            return config == null || config.EnableMirror;
        }

        private BuildConfig ResolveBuildConfig()
        {
            if (m_BuildConfig != null)
            {
                return m_BuildConfig;
            }

            m_BuildConfig = Resources.Load<BuildConfig>("BuildConfig");
            return m_BuildConfig;
        }
    }

    [Serializable]
    public class NetworkDiagnostics
    {
        public float LatencyEstimateMs;
        public float PacketLossEstimatePercent;
    }
}
