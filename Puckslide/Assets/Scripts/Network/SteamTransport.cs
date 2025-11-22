using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if STEAMWORKSNET
using Steamworks;
#endif

public class SteamTransport : NetTransport, IDisposable
{
#if STEAMWORKSNET
    private readonly Queue<NetReceivedMessage> m_Inbox = new Queue<NetReceivedMessage>();
    private readonly Dictionary<int, HSteamNetConnection> m_PeerToConnection = new Dictionary<int, HSteamNetConnection>();
    private readonly Dictionary<HSteamNetConnection, int> m_ConnectionToPeer = new Dictionary<HSteamNetConnection, int>();
    private readonly Dictionary<int, SteamNetworkingIdentity> m_PeerIdentities = new Dictionary<int, SteamNetworkingIdentity>();

    private Callback<SteamNetConnectionStatusChangedCallback_t> m_ConnectionStatusChanged;
    private Callback<GSPolicyResponse_t> m_PolicyResponse;

    private HSteamListenSocket m_ListenSocket = HSteamListenSocket.Invalid;
    private HSteamNetPollGroup m_PollGroup = HSteamNetPollGroup.Invalid;

    private bool m_Disposed;
    private int m_NextPeerId = 1;
    private bool m_HasRelay;
    private bool m_LastVacSecure = true;

    public static bool IsPlatformSupported => SteamAPI.IsSteamRunning();
#else
    private readonly LoopbackTransport m_Fallback = new LoopbackTransport();

    public static bool IsPlatformSupported => false;
#endif

    public bool IsHosting
    {
        get
        {
#if STEAMWORKSNET
            return m_ListenSocket != HSteamListenSocket.Invalid;
#else
            return m_Fallback.IsHosting;
#endif
        }
    }

    public int LocalPeerId
    {
        get
        {
#if STEAMWORKSNET
            return IsHosting ? 0 : 1;
#else
            return m_Fallback.LocalPeerId;
#endif
        }
    }

    public IReadOnlyCollection<int> Peers
    {
        get
        {
#if STEAMWORKSNET
            return m_ConnectionToPeer.Values;
#else
            return m_Fallback.Peers;
#endif
        }
    }

    public event Action<bool> OnVacStatusChanged;
    public event Action<string> OnTransportAnomaly;

#if STEAMWORKSNET
    public SteamTransport()
    {
        if (!SteamAPI.Init())
        {
            throw new InvalidOperationException("Failed to initialize Steam API for transport.");
        }

        SteamNetworkingUtils.InitRelayNetworkAccess();
        m_HasRelay = SteamNetworkingUtils.GetRelayNetworkStatus(out _) == ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current;
        m_ConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
        m_PolicyResponse = Callback<GSPolicyResponse_t>.Create(HandlePolicyResponse);
    }

    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        foreach (var connection in m_PeerToConnection.Values)
        {
            SteamNetworkingSockets.CloseConnection(connection, 0, "Transport disposed", false);
        }

        m_PeerToConnection.Clear();
        m_ConnectionToPeer.Clear();
        m_PeerIdentities.Clear();

        if (m_PollGroup != HSteamNetPollGroup.Invalid)
        {
            SteamNetworkingSockets.DestroyPollGroup(m_PollGroup);
            m_PollGroup = HSteamNetPollGroup.Invalid;
        }

        if (m_ListenSocket != HSteamListenSocket.Invalid)
        {
            SteamNetworkingSockets.CloseListenSocket(m_ListenSocket);
            m_ListenSocket = HSteamListenSocket.Invalid;
        }

        SteamAPI.Shutdown();
        m_Disposed = true;
    }

    public void Host(int port)
    {
        EnsureNotDisposed();
        EnsureRelayReady();

        port = ClampPort(port);

        var listenOptions = BuildRelayConfig();
        m_ListenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, (int)listenOptions.Length, listenOptions);
        m_PollGroup = SteamNetworkingSockets.CreatePollGroup();
        if (m_ListenSocket == HSteamListenSocket.Invalid || m_PollGroup == HSteamNetPollGroup.Invalid)
        {
            throw new InvalidOperationException("Unable to create Steam listen socket.");
        }
    }

    public void Connect(string address, int port)
    {
        EnsureNotDisposed();
        EnsureRelayReady();

        port = ClampPort(port);

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("A SteamID address is required to connect.", nameof(address));
        }

        if (!ulong.TryParse(address, out ulong rawSteamId))
        {
            throw new ArgumentException("Address must be the host SteamID64.", nameof(address));
        }

        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID(new CSteamID(rawSteamId));
        var connectOptions = BuildRelayConfig();
        var connection = SteamNetworkingSockets.ConnectP2P(ref identity, 0, (int)connectOptions.Length, connectOptions);
        if (connection == HSteamNetConnection.Invalid)
        {
            throw new InvalidOperationException("Failed to request connection to host.");
        }

        m_PeerIdentities[1] = identity;
        m_PeerToConnection[1] = connection;
        m_ConnectionToPeer[connection] = 1;
    }

    public bool TryReconnect(string address, int port, int peerId)
    {
        port = ClampPort(port);
        if (!m_PeerIdentities.TryGetValue(peerId, out SteamNetworkingIdentity identity))
        {
            return false;
        }

        var connectOptions = BuildRelayConfig();
        var connection = SteamNetworkingSockets.ConnectP2P(ref identity, 0, (int)connectOptions.Length, connectOptions);
        if (connection == HSteamNetConnection.Invalid)
        {
            return false;
        }

        m_PeerToConnection[peerId] = connection;
        m_ConnectionToPeer[connection] = peerId;
        return true;
    }

    public void Disconnect(int peerId, bool allowGracefulReconnect = false, TimeSpan? graceWindow = null)
    {
        if (!m_PeerToConnection.TryGetValue(peerId, out HSteamNetConnection connection))
        {
            return;
        }

        SteamNetworkingSockets.CloseConnection(connection, 0, "Disconnect", allowGracefulReconnect);
        m_PeerToConnection.Remove(peerId);
        m_ConnectionToPeer.Remove(connection);

        if (!allowGracefulReconnect)
        {
            m_PeerIdentities.Remove(peerId);
        }
    }

    public void Send(int peerId, NetMessage message)
    {
        if (!m_PeerToConnection.TryGetValue(peerId, out HSteamNetConnection connection))
        {
            throw new ArgumentException($"Unknown peer id {peerId}");
        }

        byte[] buffer = NetMessage.Encode(message);
        SteamNetworkingSockets.SendMessageToConnection(connection, buffer, (int)buffer.Length, (int)ESteamNetworkingSend.k_ESteamNetworkingSend_Reliable, out _);
    }

    public bool TryReceive(out NetReceivedMessage message)
    {
        PumpNetwork();

        if (m_Inbox.Count > 0)
        {
            message = m_Inbox.Dequeue();
            return true;
        }

        message = default;
        return false;
    }

    private void PumpNetwork()
    {
        if (m_PollGroup == HSteamNetPollGroup.Invalid)
        {
            return;
        }

        SteamNetworkingMessage_t[] messages = new SteamNetworkingMessage_t[16];
        int messageCount;

        while ((messageCount = SteamNetworkingSockets.ReceiveMessagesOnPollGroup(m_PollGroup, messages, messages.Length)) > 0)
        {
            for (int i = 0; i < messageCount; i++)
            {
                SteamNetworkingMessage_t msg = messages[i];
                if (msg.m_cbSize == 0 || msg.m_conn == HSteamNetConnection.Invalid)
                {
                    msg.Release();
                    continue;
                }

                byte[] payload = new byte[msg.m_cbSize];
                Marshal.Copy(msg.m_pData, payload, 0, msg.m_cbSize);
                msg.Release();

                if (m_ConnectionToPeer.TryGetValue(msg.m_conn, out int fromPeer))
                {
                    m_Inbox.Enqueue(new NetReceivedMessage
                    {
                        FromPeerId = fromPeer,
                        Message = NetMessage.Decode(payload)
                    });
                }
            }
        }

        SteamAPI.RunCallbacks();
    }

    private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t data)
    {
        if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
        {
            if (IsHosting)
            {
                SteamNetworkingSockets.AcceptConnection(data.m_hConn);
                SteamNetworkingSockets.SetConnectionPollGroup(data.m_hConn, m_PollGroup);
                int peerId = m_NextPeerId++;
                m_PeerToConnection[peerId] = data.m_hConn;
                m_ConnectionToPeer[data.m_hConn] = peerId;
                m_PeerIdentities[peerId] = data.m_info.m_identityRemote;
            }
        }
        else if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected)
        {
            if (!IsHosting && m_PollGroup != HSteamNetPollGroup.Invalid)
            {
                SteamNetworkingSockets.SetConnectionPollGroup(data.m_hConn, m_PollGroup);
                m_ConnectionToPeer[data.m_hConn] = 0;
                m_PeerToConnection[0] = data.m_hConn;
                m_PeerIdentities[0] = data.m_info.m_identityRemote;
            }
        }
        else if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer ||
                 data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally)
        {
            if (m_ConnectionToPeer.TryGetValue(data.m_hConn, out int peerId))
            {
                m_PeerToConnection.Remove(peerId);
                m_ConnectionToPeer.Remove(data.m_hConn);
            }
        }
    }

    private void HandlePolicyResponse(GSPolicyResponse_t data)
    {
        bool isSecure = data.m_bSecure == 1;
        if (isSecure != m_LastVacSecure)
        {
            m_LastVacSecure = isSecure;
            OnVacStatusChanged?.Invoke(isSecure);
        }

        if (!isSecure)
        {
            ReportAnomaly("Steam VAC policy reported insecure connection state.");
        }
    }

    private SteamNetworkingConfigValue_t[] BuildRelayConfig()
    {
        return new SteamNetworkingConfigValue_t[]
        {
            new SteamNetworkingConfigValue_t
            {
                m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_ConnectionUserData,
                m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int64,
                m_val = new SteamNetworkingConfigValue_t.OptionValue
                {
                    m_int64 = (long)SteamUser.GetSteamID().m_SteamID
                }
            }
        };
    }

    private void EnsureRelayReady()
    {
        if (!m_HasRelay)
        {
            SteamNetworkingUtils.InitRelayNetworkAccess();
            m_HasRelay = SteamNetworkingUtils.GetRelayNetworkStatus(out _) == ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current;
        }
    }

    private void EnsureNotDisposed()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(SteamTransport));
        }
    }

    private void ReportAnomaly(string message)
    {
        OnTransportAnomaly?.Invoke(message);
    }

    private int ClampPort(int port)
    {
        if (port < 0)
        {
            ReportAnomaly($"Received negative port {port}; clamping to 0.");
            return 0;
        }

        if (port > ushort.MaxValue)
        {
            ReportAnomaly($"Received oversized port {port}; clamping to {ushort.MaxValue}.");
            return ushort.MaxValue;
        }

        return port;
    }
#else
    public void Dispose()
    {
    }

    public void Host(int port)
    {
        port = ClampPort(port);
        m_Fallback.Host(port);
    }

    public void Connect(string address, int port)
    {
        port = ClampPort(port);
        m_Fallback.Connect(address, port);
    }

    public bool TryReconnect(string address, int port, int peerId)
    {
        port = ClampPort(port);
        return m_Fallback.TryReconnect(address, port, peerId);
    }

    public void Disconnect(int peerId, bool allowGracefulReconnect = false, TimeSpan? graceWindow = null)
    {
        m_Fallback.Disconnect(peerId, allowGracefulReconnect, graceWindow);
    }

    public void Send(int peerId, NetMessage message)
    {
        m_Fallback.Send(peerId, message);
    }

    public bool TryReceive(out NetReceivedMessage message)
    {
        return m_Fallback.TryReceive(out message);
    }

    private void ReportAnomaly(string message)
    {
    }

    private int ClampPort(int port)
    {
        return port < 0 ? 0 : Math.Min(port, ushort.MaxValue);
    }
#endif
}
