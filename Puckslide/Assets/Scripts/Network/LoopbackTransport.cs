using System;
using System.Collections.Generic;

public class LoopbackTransport : NetTransport
{
    private class LoopbackEndpoint
    {
        public LoopbackTransport Host;
        public readonly Dictionary<int, LoopbackTransport> Clients = new Dictionary<int, LoopbackTransport>();
        public int NextPeerId = 1;
        public readonly Dictionary<int, DateTime> GracefulDisconnectExpiry = new Dictionary<int, DateTime>();
    }

    private static readonly Dictionary<int, LoopbackEndpoint> s_Endpoints = new Dictionary<int, LoopbackEndpoint>();

    private readonly Queue<NetReceivedMessage> m_Inbox = new Queue<NetReceivedMessage>();
    private readonly HashSet<int> m_Peers = new HashSet<int>();
    private LoopbackEndpoint m_Endpoint;

    public bool IsHosting { get; private set; }
    public int LocalPeerId { get; private set; } = -1;
    public IReadOnlyCollection<int> Peers => m_Peers;

    public static void ClearEndpoints()
    {
        s_Endpoints.Clear();
    }

    public void Host(int port)
    {
        if (m_Endpoint != null)
        {
            throw new InvalidOperationException("Transport already connected or hosting.");
        }

        if (s_Endpoints.ContainsKey(port))
        {
            throw new InvalidOperationException($"A loopback host already exists on port {port}.");
        }

        var endpoint = new LoopbackEndpoint
        {
            Host = this
        };

        s_Endpoints[port] = endpoint;
        m_Endpoint = endpoint;
        IsHosting = true;
        LocalPeerId = 0;
    }

    public void Connect(string address, int port)
    {
        if (m_Endpoint != null)
        {
            throw new InvalidOperationException("Transport already connected or hosting.");
        }

        if (!s_Endpoints.TryGetValue(port, out LoopbackEndpoint endpoint) || endpoint.Host == null)
        {
            throw new InvalidOperationException("No loopback host is listening on the requested port.");
        }

        LocalPeerId = endpoint.NextPeerId++;
        endpoint.Clients[LocalPeerId] = this;
        m_Endpoint = endpoint;
        m_Peers.Add(0);
        endpoint.Host.m_Peers.Add(LocalPeerId);
        Enqueue(endpoint.Host, LocalPeerId, new NetMessage { Tick = 0, Payload = Array.Empty<byte>() });
    }

    public void Disconnect(int peerId, bool allowGracefulReconnect = false, TimeSpan? graceWindow = null)
    {
        if (m_Endpoint == null)
        {
            return;
        }

        TimeSpan effectiveWindow = graceWindow ?? TimeSpan.FromSeconds(5);
        DateTime expiry = DateTime.UtcNow + effectiveWindow;

        if (IsHosting)
        {
            if (m_Endpoint.Clients.TryGetValue(peerId, out LoopbackTransport client))
            {
                m_Endpoint.Clients.Remove(peerId);
                m_Peers.Remove(peerId);
                client.m_Peers.Clear();
                client.m_Endpoint = null;
                if (allowGracefulReconnect)
                {
                    m_Endpoint.GracefulDisconnectExpiry[peerId] = expiry;
                }
                else
                {
                    m_Endpoint.GracefulDisconnectExpiry.Remove(peerId);
                }
            }
        }
        else if (peerId == 0)
        {
            m_Endpoint.Clients.Remove(LocalPeerId);
            m_Endpoint.Host.m_Peers.Remove(LocalPeerId);
            m_Peers.Clear();
            if (allowGracefulReconnect)
            {
                m_Endpoint.GracefulDisconnectExpiry[LocalPeerId] = expiry;
            }
            else
            {
                m_Endpoint.GracefulDisconnectExpiry.Remove(LocalPeerId);
            }
            m_Endpoint = null;
        }
    }

    public bool TryReconnect(string address, int port, int peerId)
    {
        if (m_Endpoint != null)
        {
            return false;
        }

        if (!s_Endpoints.TryGetValue(port, out LoopbackEndpoint endpoint) || endpoint.Host == null)
        {
            return false;
        }

        if (!endpoint.GracefulDisconnectExpiry.TryGetValue(peerId, out DateTime expiry) || DateTime.UtcNow > expiry)
        {
            return false;
        }

        if (endpoint.Clients.ContainsKey(peerId))
        {
            return false;
        }

        LocalPeerId = peerId;
        endpoint.Clients[peerId] = this;
        m_Endpoint = endpoint;
        m_Peers.Add(0);
        endpoint.Host.m_Peers.Add(peerId);
        Enqueue(endpoint.Host, peerId, new NetMessage { Tick = 0, Payload = Array.Empty<byte>() });
        return true;
    }

    public void Send(int peerId, NetMessage message)
    {
        if (m_Endpoint == null)
        {
            throw new InvalidOperationException("Transport is not connected.");
        }

        if (!m_Peers.Contains(peerId))
        {
            throw new ArgumentException($"Unknown peer id {peerId}");
        }

        if (IsHosting)
        {
            if (m_Endpoint.Clients.TryGetValue(peerId, out LoopbackTransport client))
            {
                Enqueue(client, LocalPeerId, message);
            }
            else
            {
                throw new ArgumentException("Target peer is not connected.");
            }
        }
        else
        {
            Enqueue(m_Endpoint.Host, LocalPeerId, message);
        }
    }

    public bool TryReceive(out NetReceivedMessage message)
    {
        if (m_Inbox.Count > 0)
        {
            message = m_Inbox.Dequeue();
            return true;
        }

        message = default;
        return false;
    }

    private static void Enqueue(LoopbackTransport target, int fromPeerId, NetMessage message)
    {
        target.m_Inbox.Enqueue(new NetReceivedMessage
        {
            FromPeerId = fromPeerId,
            Message = message
        });
    }
}
