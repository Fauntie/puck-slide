using System;
using System.Collections.Generic;
using System.Linq;

public interface IDeterministicStateHasher<TState>
{
    int ComputeHash(TState state);
}

public class DeterministicRandom
{
    private readonly Random m_Random;

    public DeterministicRandom(int seed)
    {
        m_Random = new Random(seed);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        return m_Random.Next(minInclusive, maxExclusive);
    }

    public float NextFloat()
    {
        return (float)m_Random.NextDouble();
    }
}

public class LockstepInputBuffer
{
    private struct BufferedInput
    {
        public byte[] Data;
        public bool Predicted;
    }

    private const int k_MaxInputBytes = 4096;

    private readonly HashSet<int> m_Peers;
    private readonly Dictionary<uint, Dictionary<int, BufferedInput>> m_InputByTick = new Dictionary<uint, Dictionary<int, BufferedInput>>();
    private readonly SortedSet<uint> m_PredictedTicks = new SortedSet<uint>();
    private readonly Queue<uint> m_ReadyTicks = new Queue<uint>();
    private readonly Action<string> m_Telemetry;
    private readonly NetworkDiagnostics m_Diagnostics;

    public LockstepInputBuffer(IEnumerable<int> peers, Action<string> telemetry = null, NetworkDiagnostics diagnostics = null)
    {
        if (peers == null)
        {
            throw new ArgumentNullException(nameof(peers));
        }

        m_Peers = new HashSet<int>(peers);
        m_Telemetry = telemetry;
        m_Diagnostics = diagnostics;
    }

    public void BufferLocalInput(uint tick, byte[] input, int localPeerId)
    {
        BufferInput(tick, localPeerId, input, predicted: false);
    }

    public void BufferRemoteInput(uint tick, byte[] input, int fromPeerId)
    {
        BufferInput(tick, fromPeerId, input, predicted: false);
    }

    public void PredictMissingInputs(uint tick, Func<int, byte[]> predictionGenerator)
    {
        if (predictionGenerator == null)
        {
            throw new ArgumentNullException(nameof(predictionGenerator));
        }

        EnsureTickMap(tick);
        foreach (int peerId in m_Peers)
        {
            if (!m_InputByTick[tick].ContainsKey(peerId))
            {
                byte[] prediction = predictionGenerator(peerId) ?? Array.Empty<byte>();
                BufferInput(tick, peerId, prediction, predicted: true);
                m_Diagnostics?.RecordPacketLoss(tick, peerId);
            }
        }
    }

    public bool TryDequeueReady(out uint tick, out Dictionary<int, byte[]> inputs, out bool usedPrediction)
    {
        while (m_ReadyTicks.Count > 0)
        {
            uint candidateTick = m_ReadyTicks.Peek();
            if (!IsComplete(candidateTick))
            {
                break;
            }

            m_ReadyTicks.Dequeue();
            inputs = m_InputByTick[candidateTick].ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Data);
            usedPrediction = m_InputByTick[candidateTick].Any(kvp => kvp.Value.Predicted);
            tick = candidateTick;
            return true;
        }

        tick = default;
        inputs = default;
        usedPrediction = false;
        return false;
    }

    public IEnumerable<uint> GetPredictedTicks()
    {
        return m_PredictedTicks;
    }

    public void Reconcile(uint tick, int peerId, byte[] authoritativeInput)
    {
        EnsureTickMap(tick);
        byte[] sanitized = SanitizeInput(authoritativeInput, tick, peerId);
        m_InputByTick[tick][peerId] = new BufferedInput { Data = sanitized, Predicted = false };
        if (!m_InputByTick[tick].Values.Any(input => input.Predicted))
        {
            m_PredictedTicks.Remove(tick);
        }
    }

    public bool HasAllInputs(uint tick)
    {
        return IsComplete(tick);
    }

    private void BufferInput(uint tick, int peerId, byte[] input, bool predicted)
    {
        byte[] sanitized = SanitizeInput(input, tick, peerId);
        EnsureTickMap(tick);
        m_InputByTick[tick][peerId] = new BufferedInput { Data = sanitized, Predicted = predicted };
        if (predicted)
        {
            m_PredictedTicks.Add(tick);
        }

        if (IsComplete(tick) && !m_ReadyTicks.Contains(tick))
        {
            m_ReadyTicks.Enqueue(tick);
        }
    }

    private void EnsureTickMap(uint tick)
    {
        if (!m_InputByTick.ContainsKey(tick))
        {
            m_InputByTick[tick] = new Dictionary<int, BufferedInput>();
        }
    }

    private byte[] SanitizeInput(byte[] input, uint tick, int peerId)
    {
        if (input == null)
        {
            m_Telemetry?.Invoke($"Input from peer {peerId} at tick {tick} was null; substituting empty payload.");
            return Array.Empty<byte>();
        }

        if (input.Length <= k_MaxInputBytes)
        {
            return input;
        }

        byte[] trimmed = new byte[k_MaxInputBytes];
        Array.Copy(input, trimmed, k_MaxInputBytes);
        m_Telemetry?.Invoke($"Input from peer {peerId} at tick {tick} exceeded {k_MaxInputBytes} bytes and was truncated.");
        return trimmed;
    }

    private bool IsComplete(uint tick)
    {
        return m_InputByTick.TryGetValue(tick, out Dictionary<int, BufferedInput> inputs) && inputs.Count == m_Peers.Count;
    }
}

public class RollbackManager<TState>
{
    private readonly Func<TState, TState> m_Cloner;
    private readonly Func<TState, bool> m_StateValidator;
    private readonly Action<string> m_Logger;
    private readonly NetworkDiagnostics m_Diagnostics;
    private readonly SortedDictionary<uint, TState> m_Checkpoints = new SortedDictionary<uint, TState>();

    public RollbackManager(Func<TState, TState> cloner, Func<TState, bool> stateValidator = null, Action<string> logger = null, NetworkDiagnostics diagnostics = null)
    {
        m_Cloner = cloner ?? throw new ArgumentNullException(nameof(cloner));
        m_StateValidator = stateValidator;
        m_Logger = logger ?? Console.WriteLine;
        m_Diagnostics = diagnostics;
    }

    public void SaveCheckpoint(uint tick, TState state)
    {
        ValidateState(state, tick, "checkpoint save");
        m_Checkpoints[tick] = m_Cloner(state);
    }

    public bool TryGetCheckpoint(uint tick, out TState state)
    {
        bool found = m_Checkpoints.TryGetValue(tick, out state);
        if (found)
        {
            ValidateState(state, tick, "checkpoint load");
            m_Diagnostics?.RecordRollback(tick);
        }

        return found;
    }

    public IEnumerable<uint> GetCheckpointTicks()
    {
        return m_Checkpoints.Keys;
    }

    public void PruneBefore(uint tick)
    {
        List<uint> toRemove = new List<uint>();
        foreach (uint checkpointTick in m_Checkpoints.Keys)
        {
            if (checkpointTick < tick)
            {
                toRemove.Add(checkpointTick);
            }
        }

        foreach (uint key in toRemove)
        {
            m_Checkpoints.Remove(key);
        }
    }

    private void ValidateState(TState state, uint tick, string context)
    {
        if (m_StateValidator != null && !m_StateValidator(state))
        {
            m_Logger?.Invoke($"State validation failed during {context} at tick {tick}.");
            throw new InvalidOperationException($"Invalid state encountered during {context}.");
        }
    }
}

public class DesyncDetector<TState>
{
    private readonly IDeterministicStateHasher<TState> m_Hasher;
    private readonly Action<string> m_Logger;
    private readonly Action<uint, int, int> m_AnomalyReporter;
    private readonly Dictionary<uint, int> m_Hashes = new Dictionary<uint, int>();
    private readonly NetworkDiagnostics m_Diagnostics;

    public DesyncDetector(IDeterministicStateHasher<TState> hasher, Action<string> logger = null, Action<uint, int, int> anomalyReporter = null, NetworkDiagnostics diagnostics = null)
    {
        m_Hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        m_Logger = logger ?? Console.WriteLine;
        m_AnomalyReporter = anomalyReporter;
        m_Diagnostics = diagnostics;
    }

    public void Record(uint tick, TState state)
    {
        int hash = m_Hasher.ComputeHash(state);
        m_Hashes[tick] = hash;
    }

    public void Validate(uint tick, TState state)
    {
        int hash = m_Hasher.ComputeHash(state);
        if (m_Hashes.TryGetValue(tick, out int expected) && expected != hash)
        {
            m_Logger?.Invoke($"Desync detected at tick {tick}: expected hash {expected} but got {hash}.");
            m_AnomalyReporter?.Invoke(tick, expected, hash);
            m_Diagnostics?.LogEvent(
                "desync",
                "Deterministic hash mismatch.",
                new Dictionary<string, string>
                {
                    {"tick", tick.ToString()},
                    {"expectedHash", expected.ToString()},
                    {"actualHash", hash.ToString()}
                });
        }
    }
}

public struct StateDeltaPacket
{
    public uint BaseTick;
    public uint Tick;
    public byte[] Delta;
}

public interface IStateDeltaCodec<TState>
{
    byte[] EncodeDelta(TState baseline, TState current);
    void ApplyDelta(TState baseline, byte[] delta, out TState output);
}

public class StateDeltaStream<TState>
{
    private readonly IStateDeltaCodec<TState> m_Codec;
    private readonly SortedDictionary<uint, TState> m_Snapshots = new SortedDictionary<uint, TState>();

    public StateDeltaStream(IStateDeltaCodec<TState> codec)
    {
        m_Codec = codec ?? throw new ArgumentNullException(nameof(codec));
    }

    public StateDeltaPacket CreateDelta(uint baselineTick, uint tick, TState baseline, TState current)
    {
        byte[] delta = m_Codec.EncodeDelta(baseline, current);
        m_Snapshots[tick] = current;
        return new StateDeltaPacket
        {
            BaseTick = baselineTick,
            Tick = tick,
            Delta = delta
        };
    }

    public bool TryApplyDelta(StateDeltaPacket packet, out TState resolved)
    {
        if (m_Snapshots.TryGetValue(packet.BaseTick, out TState baseline))
        {
            m_Codec.ApplyDelta(baseline, packet.Delta, out resolved);
            m_Snapshots[packet.Tick] = resolved;
            return true;
        }

        resolved = default;
        return false;
    }

    public void AddSnapshot(uint tick, TState snapshot)
    {
        m_Snapshots[tick] = snapshot;
    }
}

public class StateInterpolator<TState>
{
    private readonly SortedList<uint, TState> m_Buffer = new SortedList<uint, TState>();

    public void AddSnapshot(uint tick, TState snapshot)
    {
        if (!m_Buffer.ContainsKey(tick))
        {
            m_Buffer.Add(tick, snapshot);
        }
    }

    public bool TryInterpolate(uint renderTick, Func<TState, TState, float, TState> lerpFunc, out TState state)
    {
        state = default;
        if (m_Buffer.Count < 2 || lerpFunc == null)
        {
            return false;
        }

        int index = m_Buffer.Keys.ToList().FindLastIndex(k => k <= renderTick);
        if (index < 0 || index + 1 >= m_Buffer.Count)
        {
            return false;
        }

        uint previousTick = m_Buffer.Keys[index];
        uint nextTick = m_Buffer.Keys[index + 1];
        float alpha = (float)(renderTick - previousTick) / (nextTick - previousTick);
        state = lerpFunc(m_Buffer.Values[index], m_Buffer.Values[index + 1], alpha);
        return true;
    }

    public bool TryExtrapolate(uint targetTick, Func<uint, TState> extrapolator, out TState state)
    {
        state = default;
        if (extrapolator == null || m_Buffer.Count == 0)
        {
            return false;
        }

        state = extrapolator(targetTick);
        return true;
    }
}

public class AuthorityReconciler<TState>
{
    private readonly Action<TState> m_ApplyAuthoritativeState;
    private readonly Func<TState, bool> m_StateValidator;
    private readonly Action<uint> m_OnConflictResolved;
    private readonly Action<string> m_Logger;

    public AuthorityReconciler(Action<TState> applyAuthoritativeState, Action<uint> onConflictResolved = null, Action<string> logger = null, Func<TState, bool> stateValidator = null)
    {
        m_ApplyAuthoritativeState = applyAuthoritativeState ?? throw new ArgumentNullException(nameof(applyAuthoritativeState));
        m_OnConflictResolved = onConflictResolved;
        m_Logger = logger ?? Console.WriteLine;
        m_StateValidator = stateValidator;
    }

    public void Resolve(uint tick, TState authoritativeState)
    {
        if (m_StateValidator != null && !m_StateValidator(authoritativeState))
        {
            m_Logger?.Invoke($"Rejected authoritative state at tick {tick} due to validation failure.");
            return;
        }

        m_ApplyAuthoritativeState(authoritativeState);
        m_OnConflictResolved?.Invoke(tick);
        m_Logger?.Invoke($"State-sync reconciliation applied at tick {tick}.");
    }
}
