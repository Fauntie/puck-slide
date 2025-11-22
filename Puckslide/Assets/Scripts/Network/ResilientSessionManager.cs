using System;
using System.Collections.Generic;
using System.Text;

public enum ResilientSessionStatus
{
    Inactive,
    Connecting,
    Connected,
    Paused,
    Reconnecting,
    Failed
}

public class ResilientSessionManager<TState>
{
    private readonly NetTransport m_Transport;
    private readonly ITimeProvider m_TimeProvider;
    private readonly Func<TState> m_StateAccessor;
    private readonly Func<TState, byte[]> m_SerializeState;
    private readonly Func<byte[], TState> m_DeserializeState;
    private readonly double m_HiccupTimeoutSeconds;
    private readonly double m_ReconnectGraceSeconds;
    private readonly double m_RetryIntervalSeconds;
    private readonly int m_MaxRetries;
    private readonly NetworkDiagnostics m_Diagnostics;

    private double m_LastHeartbeatTime;
    private double m_LastRetryTime;
    private int m_RetryCount;
    private string m_Address = string.Empty;
    private int m_Port;
    private int m_PeerId;
    private byte[] m_FrozenState;
    private string m_StatusMessage = string.Empty;

    public ResilientSessionStatus Status { get; private set; } = ResilientSessionStatus.Inactive;
    public string StatusMessage => m_StatusMessage;

    public event Action<ResilientSessionStatus, string> OnStatusChanged;

    public ResilientSessionManager(
        NetTransport transport,
        ITimeProvider timeProvider,
        Func<TState> stateAccessor,
        Func<TState, byte[]> serializeState,
        Func<byte[], TState> deserializeState,
        double hiccupTimeoutSeconds = 2.0,
        double reconnectGraceSeconds = 5.0,
        double retryIntervalSeconds = 0.5,
        int maxRetries = 5,
        NetworkDiagnostics diagnostics = null)
    {
        m_Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        m_TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        m_StateAccessor = stateAccessor ?? throw new ArgumentNullException(nameof(stateAccessor));
        m_SerializeState = serializeState ?? throw new ArgumentNullException(nameof(serializeState));
        m_DeserializeState = deserializeState ?? throw new ArgumentNullException(nameof(deserializeState));
        m_HiccupTimeoutSeconds = hiccupTimeoutSeconds;
        m_ReconnectGraceSeconds = reconnectGraceSeconds;
        m_RetryIntervalSeconds = retryIntervalSeconds;
        m_MaxRetries = maxRetries;
        m_Diagnostics = diagnostics;
    }

    public void Start(string address, int port, int peerId)
    {
        m_Address = address ?? string.Empty;
        m_Port = port;
        m_PeerId = peerId;
        m_LastHeartbeatTime = m_TimeProvider.CurrentTimeSeconds;
        SetStatus(ResilientSessionStatus.Connecting, "Attempting connection...");
        m_Diagnostics?.LogEvent(
            "network_session",
            "Session start requested.",
            new System.Collections.Generic.Dictionary<string, string>
            {
                {"address", m_Address},
                {"port", m_Port.ToString()},
                {"peerId", m_PeerId.ToString()}
            });
    }

    public void ConfirmConnected()
    {
        m_LastHeartbeatTime = m_TimeProvider.CurrentTimeSeconds;
        m_RetryCount = 0;
        SetStatus(ResilientSessionStatus.Connected, "Link stable.");
        m_Diagnostics?.LogEvent("network_session", "Connection confirmed.");
    }

    public void RecordHeartbeat()
    {
        m_LastHeartbeatTime = m_TimeProvider.CurrentTimeSeconds;
        if (Status == ResilientSessionStatus.Connecting || Status == ResilientSessionStatus.Reconnecting)
        {
            ConfirmConnected();
        }
    }

    public void Pause()
    {
        if (Status == ResilientSessionStatus.Paused)
        {
            return;
        }

        m_FrozenState = m_SerializeState(m_StateAccessor());
        SetStatus(ResilientSessionStatus.Paused, "Session paused; state frozen.");
    }

    public void Resume()
    {
        if (Status != ResilientSessionStatus.Paused)
        {
            return;
        }

        SetStatus(ResilientSessionStatus.Connected, "Session resumed.");
        m_LastHeartbeatTime = m_TimeProvider.CurrentTimeSeconds;
    }

    public bool TryGetFrozenState(out byte[] frozenState)
    {
        frozenState = m_FrozenState;
        return frozenState != null;
    }

    public bool TryRestoreState(byte[] payload, out TState restored)
    {
        if (payload == null || payload.Length == 0)
        {
            restored = default;
            return false;
        }

        restored = m_DeserializeState(payload);
        return true;
    }

    public void Update()
    {
        if (Status == ResilientSessionStatus.Inactive || Status == ResilientSessionStatus.Paused || Status == ResilientSessionStatus.Failed)
        {
            return;
        }

        double now = m_TimeProvider.CurrentTimeSeconds;

        if (Status == ResilientSessionStatus.Connected && now - m_LastHeartbeatTime >= m_HiccupTimeoutSeconds)
        {
            EnterReconnection("Network hiccup detected; attempting to reconnect.");
        }

        if (Status == ResilientSessionStatus.Reconnecting && now - m_LastRetryTime >= m_RetryIntervalSeconds)
        {
            m_LastRetryTime = now;
            if (m_Transport.TryReconnect(m_Address, m_Port, m_PeerId))
            {
                ConfirmConnected();
                return;
            }

            m_RetryCount++;
            if (m_RetryCount >= m_MaxRetries || now - m_LastHeartbeatTime > m_ReconnectGraceSeconds)
            {
                SetStatus(ResilientSessionStatus.Failed, "Failed to recover connection.");
            }
            else
            {
                SetStatus(ResilientSessionStatus.Reconnecting, $"Reconnecting... attempt {m_RetryCount}/{m_MaxRetries}");
            }
        }
    }

    public void HandlePeerDrop()
    {
        EnterReconnection("Peer dropped; waiting for grace window to rejoin.");
        m_Diagnostics?.LogEvent(
            "disconnect",
            "Peer disconnected; entering grace window.",
            new System.Collections.Generic.Dictionary<string, string>
            {
                {"peerId", m_PeerId.ToString()},
                {"address", m_Address}
            });
    }

    private void EnterReconnection(string message)
    {
        m_RetryCount = 0;
        m_LastRetryTime = m_TimeProvider.CurrentTimeSeconds;
        SetStatus(ResilientSessionStatus.Reconnecting, message);
        m_Diagnostics?.LogEvent(
            "network_session",
            message,
            new System.Collections.Generic.Dictionary<string, string>
            {
                {"peerId", m_PeerId.ToString()},
                {"retryCount", m_RetryCount.ToString()}
            });
    }

    private void SetStatus(ResilientSessionStatus status, string message)
    {
        Status = status;
        m_StatusMessage = message ?? string.Empty;
        OnStatusChanged?.Invoke(Status, m_StatusMessage);
        m_Diagnostics?.LogEvent(
            "network_status",
            m_StatusMessage,
            new System.Collections.Generic.Dictionary<string, string>
            {
                {"status", Status.ToString()},
                {"peerId", m_PeerId.ToString()}
            });
    }
}

public static class ResilientSessionSerializer
{
    public static byte[] SerializeText(string state)
    {
        return Encoding.UTF8.GetBytes(state ?? string.Empty);
    }

    public static string DeserializeText(byte[] payload)
    {
        return Encoding.UTF8.GetString(payload ?? Array.Empty<byte>());
    }
}
