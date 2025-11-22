using System.Collections.Generic;
using System;

public interface NetTransport
{
    bool IsHosting { get; }
    int LocalPeerId { get; }
    IReadOnlyCollection<int> Peers { get; }

    void Host(int port);
    void Connect(string address, int port);
    bool TryReconnect(string address, int port, int peerId);
    void Disconnect(int peerId, bool allowGracefulReconnect = false, TimeSpan? graceWindow = null);

    void Send(int peerId, NetMessage message);
    bool TryReceive(out NetReceivedMessage message);
}

public struct NetReceivedMessage
{
    public int FromPeerId;
    public NetMessage Message;
}
