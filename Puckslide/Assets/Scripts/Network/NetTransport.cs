using System.Collections.Generic;

public interface NetTransport
{
    bool IsHosting { get; }
    int LocalPeerId { get; }
    IReadOnlyCollection<int> Peers { get; }

    void Host(int port);
    void Connect(string address, int port);
    void Disconnect(int peerId);

    void Send(int peerId, NetMessage message);
    bool TryReceive(out NetReceivedMessage message);
}

public struct NetReceivedMessage
{
    public int FromPeerId;
    public NetMessage Message;
}
