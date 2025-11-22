using System;

public interface ITimeProvider
{
    double CurrentTimeSeconds { get; }
}

public class ManualTimeProvider : ITimeProvider
{
    public double CurrentTimeSeconds { get; private set; }

    public void Advance(double seconds)
    {
        if (seconds < 0)
        {
            throw new ArgumentException("Cannot advance time by a negative amount.");
        }

        CurrentTimeSeconds += seconds;
    }
}

public class DeterministicTickSource
{
    private readonly ITimeProvider m_TimeProvider;
    private readonly uint m_MaxTickCatchUp;

    public double TickDurationSeconds { get; }
    public uint CurrentTick { get; private set; }

    public DeterministicTickSource(double tickRate, ITimeProvider timeProvider, uint maxTickCatchUp = 64)
    {
        if (tickRate <= 0)
        {
            throw new ArgumentException("Tick rate must be positive.");
        }

        m_TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        m_MaxTickCatchUp = maxTickCatchUp == 0 ? 1u : maxTickCatchUp;
        TickDurationSeconds = 1.0 / tickRate;
        CurrentTick = 0;
    }

    public bool Update()
    {
        uint tickForTime = GetTickForTime(m_TimeProvider.CurrentTimeSeconds);

        if (tickForTime > CurrentTick + m_MaxTickCatchUp)
        {
            tickForTime = CurrentTick + m_MaxTickCatchUp;
        }

        if (tickForTime < CurrentTick)
        {
            // Reject time regressions to keep the tick path deterministic.
            return false;
        }

        if (tickForTime > CurrentTick)
        {
            CurrentTick = tickForTime;
            return true;
        }

        return false;
    }

    public uint GetTickForTime(double timeSeconds)
    {
        if (double.IsNaN(timeSeconds) || double.IsInfinity(timeSeconds))
        {
            return CurrentTick;
        }

        double clampedTime = Math.Max(0, timeSeconds);
        double ticks = Math.Floor(clampedTime / TickDurationSeconds);

        if (ticks >= uint.MaxValue)
        {
            return uint.MaxValue;
        }

        return (uint)ticks;
    }
}

public class TickSynchronizer
{
    private readonly DeterministicTickSource m_LocalTickSource;
    private readonly NetworkDiagnostics m_Diagnostics;
    private double m_RemoteToLocalOffsetSeconds;

    public TickSynchronizer(DeterministicTickSource localTickSource, NetworkDiagnostics diagnostics = null)
    {
        m_LocalTickSource = localTickSource ?? throw new ArgumentNullException(nameof(localTickSource));
        m_Diagnostics = diagnostics;
    }

    public void UpdateOffset(uint remoteTick, double localArrivalTimeSeconds)
    {
        double remoteTimeSeconds = remoteTick * m_LocalTickSource.TickDurationSeconds;
        m_RemoteToLocalOffsetSeconds = localArrivalTimeSeconds - remoteTimeSeconds;
        m_Diagnostics?.RecordTickLatency(Math.Abs(m_RemoteToLocalOffsetSeconds));
    }

    public uint GetLocalTickForRemote(uint remoteTick)
    {
        double remoteTimeSeconds = remoteTick * m_LocalTickSource.TickDurationSeconds;
        double localEquivalentTime = remoteTimeSeconds + m_RemoteToLocalOffsetSeconds;
        return m_LocalTickSource.GetTickForTime(localEquivalentTime);
    }
}
