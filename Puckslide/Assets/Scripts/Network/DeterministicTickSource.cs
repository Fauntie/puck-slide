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

    public double TickDurationSeconds { get; }
    public uint CurrentTick { get; private set; }

    public DeterministicTickSource(double tickRate, ITimeProvider timeProvider)
    {
        if (tickRate <= 0)
        {
            throw new ArgumentException("Tick rate must be positive.");
        }

        m_TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        TickDurationSeconds = 1.0 / tickRate;
        CurrentTick = 0;
    }

    public bool Update()
    {
        uint tickForTime = GetTickForTime(m_TimeProvider.CurrentTimeSeconds);
        if (tickForTime > CurrentTick)
        {
            CurrentTick = tickForTime;
            return true;
        }

        return false;
    }

    public uint GetTickForTime(double timeSeconds)
    {
        if (timeSeconds < 0)
        {
            return 0;
        }

        return (uint)Math.Floor(timeSeconds / TickDurationSeconds);
    }
}

public class TickSynchronizer
{
    private readonly DeterministicTickSource m_LocalTickSource;
    private double m_RemoteToLocalOffsetSeconds;

    public TickSynchronizer(DeterministicTickSource localTickSource)
    {
        m_LocalTickSource = localTickSource ?? throw new ArgumentNullException(nameof(localTickSource));
    }

    public void UpdateOffset(uint remoteTick, double localArrivalTimeSeconds)
    {
        double remoteTimeSeconds = remoteTick * m_LocalTickSource.TickDurationSeconds;
        m_RemoteToLocalOffsetSeconds = localArrivalTimeSeconds - remoteTimeSeconds;
    }

    public uint GetLocalTickForRemote(uint remoteTick)
    {
        double remoteTimeSeconds = remoteTick * m_LocalTickSource.TickDurationSeconds;
        double localEquivalentTime = remoteTimeSeconds + m_RemoteToLocalOffsetSeconds;
        return m_LocalTickSource.GetTickForTime(localEquivalentTime);
    }
}
