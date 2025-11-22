using NUnit.Framework;
using System;
using System.Text;
using System.Threading;

public class NetMessageTests
{
    [Test]
    public void EncodeDecode_RoundTripsPayload()
    {
        var message = new NetMessage
        {
            Tick = 42,
            Payload = new byte[] { 1, 2, 3, 4, 5 }
        };

        byte[] encoded = NetMessage.Encode(message);
        NetMessage decoded = NetMessage.Decode(encoded);

        Assert.AreEqual(message.Tick, decoded.Tick);
        Assert.AreEqual(message.Payload.Length, decoded.Payload.Length);
        CollectionAssert.AreEqual(message.Payload, decoded.Payload);
    }
}

public class TickSynchronizationTests
{
    [Test]
    public void RemoteTickAlignsToLocalTimeline()
    {
        var timeProvider = new ManualTimeProvider();
        var tickSource = new DeterministicTickSource(60, timeProvider);
        var synchronizer = new TickSynchronizer(tickSource);

        timeProvider.Advance(2.05);
        tickSource.Update();

        uint remoteTick = 120; // remote time is exactly 2 seconds with 60hz ticks
        synchronizer.UpdateOffset(remoteTick, timeProvider.CurrentTimeSeconds);

        uint alignedTick = synchronizer.GetLocalTickForRemote(remoteTick);
        Assert.AreEqual(tickSource.CurrentTick, alignedTick);
    }
}

public class LoopbackTransportTests
{
    [SetUp]
    public void SetUp()
    {
        LoopbackTransport.ClearEndpoints();
    }

    [Test]
    public void HostAndClient_CanSendMessages()
    {
        var host = new LoopbackTransport();
        var client = new LoopbackTransport();

        host.Host(7777);
        client.Connect("localhost", 7777);

        var outbound = new NetMessage
        {
            Tick = 10,
            Payload = new byte[] { 9, 8, 7 }
        };

        client.Send(0, outbound);

        Assert.IsTrue(host.TryReceive(out NetReceivedMessage received));
        Assert.AreEqual(client.LocalPeerId, received.FromPeerId);
        Assert.AreEqual(outbound.Tick, received.Message.Tick);
        CollectionAssert.AreEqual(outbound.Payload, received.Message.Payload);

        var response = new NetMessage
        {
            Tick = 11,
            Payload = new byte[] { 6, 5, 4 }
        };

        host.Send(client.LocalPeerId, response);
        Assert.IsTrue(client.TryReceive(out NetReceivedMessage clientReceived));
        Assert.AreEqual(0, clientReceived.FromPeerId);
        Assert.AreEqual(response.Tick, clientReceived.Message.Tick);
        CollectionAssert.AreEqual(response.Payload, clientReceived.Message.Payload);
    }
}

public class LoopbackReconnectTests
{
    [SetUp]
    public void SetUp()
    {
        LoopbackTransport.ClearEndpoints();
    }

    [Test]
    public void ClientCanReconnectWithinGraceWindow()
    {
        var host = new LoopbackTransport();
        var client = new LoopbackTransport();

        host.Host(8888);
        client.Connect("localhost", 8888);

        int peerId = client.LocalPeerId;
        host.Disconnect(peerId, allowGracefulReconnect: true, graceWindow: TimeSpan.FromSeconds(2));

        Assert.IsTrue(client.TryReconnect("localhost", 8888, peerId));
        CollectionAssert.Contains(host.Peers, peerId);
    }

    [Test]
    public void ReconnectFailsAfterGraceWindowExpires()
    {
        var host = new LoopbackTransport();
        var client = new LoopbackTransport();

        host.Host(9999);
        client.Connect("localhost", 9999);

        int peerId = client.LocalPeerId;
        host.Disconnect(peerId, allowGracefulReconnect: true, graceWindow: TimeSpan.FromMilliseconds(25));

        Thread.Sleep(40);

        Assert.IsFalse(client.TryReconnect("localhost", 9999, peerId));
    }
}

public class ResilientSessionManagerTests
{
    private class DummyState
    {
        public int Tick { get; set; }
        public string Note { get; set; }
    }

    [Test]
    public void PauseAndResumeFreezesAndRestoresState()
    {
        var transport = new LoopbackTransport();
        var time = new ManualTimeProvider();
        DummyState state = new DummyState { Tick = 4, Note = "live" };

        var manager = new ResilientSessionManager<DummyState>(
            transport,
            time,
            () => state,
            SerializeDummy,
            DeserializeDummy);

        manager.Start("localhost", 0, 0);
        manager.Pause();

        state.Note = "mutated";

        Assert.IsTrue(manager.TryGetFrozenState(out byte[] frozen));
        Assert.IsTrue(manager.TryRestoreState(frozen, out DummyState restored));
        Assert.AreEqual(4, restored.Tick);
        Assert.AreEqual("live", restored.Note);

        manager.Resume();
        Assert.AreEqual(ResilientSessionStatus.Connected, manager.Status);
    }

    [Test]
    public void NetworkHiccupTriggersRetriesAndReconnect()
    {
        LoopbackTransport.ClearEndpoints();

        var host = new LoopbackTransport();
        var client = new LoopbackTransport();
        host.Host(7778);
        client.Connect("localhost", 7778);

        int peerId = client.LocalPeerId;
        var time = new ManualTimeProvider();
        var manager = new ResilientSessionManager<string>(
            client,
            time,
            () => "session",
            ResilientSessionSerializer.SerializeText,
            ResilientSessionSerializer.DeserializeText,
            hiccupTimeoutSeconds: 1.0,
            reconnectGraceSeconds: 3.0,
            retryIntervalSeconds: 0.5,
            maxRetries: 3);

        manager.Start("localhost", 7778, peerId);
        manager.ConfirmConnected();

        time.Advance(1.1);
        manager.Update();
        Assert.AreEqual(ResilientSessionStatus.Reconnecting, manager.Status);

        host.Disconnect(peerId, allowGracefulReconnect: true, graceWindow: TimeSpan.FromSeconds(3));

        time.Advance(0.5);
        manager.Update();

        Assert.AreEqual(ResilientSessionStatus.Connected, manager.Status);
    }

    private static byte[] SerializeDummy(DummyState state)
    {
        return Encoding.UTF8.GetBytes($"{state.Tick}:{state.Note}");
    }

    private static DummyState DeserializeDummy(byte[] payload)
    {
        string data = Encoding.UTF8.GetString(payload ?? Array.Empty<byte>());
        string[] parts = data.Split(':');
        return new DummyState
        {
            Tick = int.Parse(parts[0]),
            Note = parts.Length > 1 ? parts[1] : string.Empty
        };
    }
}
