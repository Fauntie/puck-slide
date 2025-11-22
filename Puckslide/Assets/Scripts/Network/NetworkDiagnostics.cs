using System;
using System.Collections.Generic;
using System.Linq;

public struct NetworkLogEvent
{
    public DateTimeOffset Timestamp;
    public string EventType;
    public string Message;
    public IReadOnlyDictionary<string, string> Context;
}

public struct NetworkMetricsSnapshot
{
    public bool OptedIn;
    public double AverageTickLatencySeconds;
    public int PacketLossCount;
    public int RollbackCount;
}

public class NetworkDiagnostics
{
    private readonly List<double> m_TickLatenciesSeconds = new List<double>();
    private readonly int m_MaxSamples;
    private int m_PacketLossCount;
    private int m_RollbackCount;

    public NetworkDiagnostics(int maxSamples = 120)
    {
        m_MaxSamples = Math.Max(1, maxSamples);
    }

    public bool MetricsOptIn { get; set; }

    public Action<NetworkLogEvent> StructuredLogger { get; set; }

    public void LogEvent(string eventType, string message, Dictionary<string, string> context = null)
    {
        StructuredLogger?.Invoke(new NetworkLogEvent
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventType = eventType ?? string.Empty,
            Message = message ?? string.Empty,
            Context = context ?? new Dictionary<string, string>()
        });
    }

    public void RecordTickLatency(double latencySeconds)
    {
        if (!MetricsOptIn || double.IsNaN(latencySeconds) || double.IsInfinity(latencySeconds))
        {
            return;
        }

        double clamped = Math.Max(0, latencySeconds);
        m_TickLatenciesSeconds.Add(clamped);
        if (m_TickLatenciesSeconds.Count > m_MaxSamples)
        {
            m_TickLatenciesSeconds.RemoveAt(0);
        }
    }

    public void RecordPacketLoss(uint tick, int peerId)
    {
        if (!MetricsOptIn)
        {
            return;
        }

        m_PacketLossCount++;
        LogEvent(
            "packet_loss",
            "Missing input replaced with prediction.",
            new Dictionary<string, string>
            {
                {"tick", tick.ToString()},
                {"peerId", peerId.ToString()}
            });
    }

    public void RecordRollback(uint tick)
    {
        if (!MetricsOptIn)
        {
            return;
        }

        m_RollbackCount++;
        LogEvent(
            "rollback",
            "Simulation rollback applied.",
            new Dictionary<string, string>
            {
                {"tick", tick.ToString()}
            });
    }

    public NetworkMetricsSnapshot GetSnapshot()
    {
        return new NetworkMetricsSnapshot
        {
            OptedIn = MetricsOptIn,
            AverageTickLatencySeconds = m_TickLatenciesSeconds.Count == 0 ? 0 : m_TickLatenciesSeconds.Average(),
            PacketLossCount = m_PacketLossCount,
            RollbackCount = m_RollbackCount
        };
    }
}
