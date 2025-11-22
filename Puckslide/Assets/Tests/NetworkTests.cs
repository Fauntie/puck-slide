using NUnit.Framework;

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
