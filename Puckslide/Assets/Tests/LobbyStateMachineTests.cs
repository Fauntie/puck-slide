using NUnit.Framework;

public class LobbyStateMachineTests
{
    [SetUp]
    public void SetUp()
    {
        LoopbackTransport.ClearEndpoints();
    }

    [Test]
    public void HostFlow_ReachesReadyAndStarting()
    {
        var transport = new LoopbackTransport();
        var lobby = new LobbyStateMachine(transport);

        lobby.Host("Host", "ABCD");
        Assert.AreEqual(LobbyState.Hosting, lobby.State);
        Assert.IsTrue(transport.IsHosting);
        Assert.AreEqual(0, transport.LocalPeerId);

        lobby.MarkReady();
        Assert.AreEqual(LobbyState.Ready, lobby.State);

        lobby.BeginStart();
        Assert.AreEqual(LobbyState.Starting, lobby.State);
    }

    [Test]
    public void JoinFlow_NotifiesHostAndTransitions()
    {
        var hostTransport = new LoopbackTransport();
        var clientTransport = new LoopbackTransport();
        var hostLobby = new LobbyStateMachine(hostTransport);
        var clientLobby = new LobbyStateMachine(clientTransport);

        hostLobby.Host("Host", "EFGH");
        Assert.AreEqual(LobbyState.Hosting, hostLobby.State);

        clientLobby.Join("Client", "EFGH");
        Assert.AreEqual(LobbyState.Joining, clientLobby.State);
        CollectionAssert.Contains(hostTransport.Peers, clientTransport.LocalPeerId);

        clientLobby.MarkReady();
        Assert.AreEqual(LobbyState.Ready, clientLobby.State);
    }

    [Test]
    public void SessionCodeToPort_IsStable()
    {
        int first = LobbySessionCodeUtility.GetPort("ROOM");
        int second = LobbySessionCodeUtility.GetPort("ROOM");
        Assert.AreEqual(first, second);
    }

    [Test]
    public void CannotStartUnlessReady()
    {
        var transport = new LoopbackTransport();
        var lobby = new LobbyStateMachine(transport);
        lobby.Host("Host", "XYZ");

        Assert.Throws<System.InvalidOperationException>(() => lobby.BeginStart());
    }
}
