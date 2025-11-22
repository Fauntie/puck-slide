# Networking Design

## Model Choice: Snapshot/State-Sync
We evaluated two models for real-time play:

- **Lockstep + Rollback**
  - Pros: deterministic simulation with minimal bandwidth (input only), easy client prediction.
  - Cons: requires deterministic simulation across platforms, complex rollback/replay logic, high sensitivity to latency spikes (all clients wait for inputs), and challenging to reconcile non-deterministic physics.
- **Snapshot/State-Sync (Authoritative Server)**
  - Pros: resilient to latency (clients interpolate/extrapolate), simpler client code, tolerant of non-deterministic physics, and straightforward late-join/rewind support via full state packets.
  - Cons: higher bandwidth than input-only, needs interpolation/rewind to hide jitter.

**Decision:** Use **snapshot/state-sync** with an authoritative server. It best matches the puck physics and avoids deterministic lockstep constraints while keeping implementation simpler for cross-platform clients.

## Authoritative Rules
- **Authoritative Data:**
  - Server is the single source of truth for: puck positions/velocities, player intent resolution, scores/timers, collision outcomes, and RNG-driven events.
  - Clients are authoritative only for local input capture (controller/keyboard state) until acknowledged by the server.
- **Conflict Resolution:**
  - Inputs are timestamped and sequence-numbered; server processes them in arrival order per client using latest known input when packets drop.
  - If a client prediction diverges from server snapshot, the client rewinds to the acknowledged tick and reapplies buffered inputs (client-side reconciliation).
  - In case of simultaneous actions (e.g., double hits), the server resolves by simulation order on a fixed tick; ties break by client ID ordering to ensure determinism.

## Message Schema
All messages use a small header `{msg_type, session_id, seq, sent_at}`.

### Client → Server
- **InputFrame** `{tick, input_seq, dt, move_vec, aim_angle, action_flags}`
- **ReadyUp** `{lobby_id, player_id, build_version, cosmetics}`
- **LobbyCommand** `{lobby_id, action: join|leave|start, payload}`
- **Ping** `{ping_id}`

### Server → Client
- **StateSnapshot** `{tick, authoritative_time, players:[{id, pos, vel, facing, anim_state}], pucks:[{id, pos, vel, spin}], scores, timers, status_flags}`
- **StateDiff** `{base_tick, diff_tick, changed_entities:[...component patches...]}` (sent when bandwidth-friendly)
- **LobbyState** `{lobby_id, players, map, ruleset, countdown, slots}`
- **InputAck** `{input_seq, processed_tick}`
- **Pong** `{ping_id, server_time}`
- **Error** `{code, message}`

### Lobby Metadata
- Lobby IDs are short UUIDs; carries map, ruleset, slots, region, and required build hash.
- Version/gamemode mismatches reject with `Error` before gameplay starts.

## Timing Model
- **Server Tick Rate:** 30 Hz fixed-step simulation.
- **Snapshot Rate:** full snapshot every 5 ticks (6 Hz) with diffs on intermediate ticks if bandwidth allows.
- **Client Prediction:** clients simulate locally using last acknowledged state and their buffered inputs; authority corrections trigger rewind + replay.
- **Interpolation:** render interpolation between last two snapshots; extrapolate for up to 100 ms when packets are late, then snap gently toward authority using damped correction.
- **Input Cadence:** clients send input frames at 60 Hz or when input changes, whichever is greater, with coalescing to reduce spam under quiet input.
- **Clock Sync:** periodic ping/pong adjusts local clock offset; snapshots include `authoritative_time` for time-aligned interpolation.

## Reliability & Transport
- Use UDP with reliability layer: acked input frames, resend on timeout; snapshots/diffs are idempotent and may be dropped.
- Compress payloads (e.g., zstd/delta) and quantize positions/angles for bandwidth.
- Encrypt/authenticate packets (DTLS or ENet-style) to prevent tampering.

## Edge Cases
- **Late Join:** server sends a full snapshot plus lobby state; client loads assets then enters prediction loop after first ack.
- **Pause/Resume:** server halts simulation and broadcasts frozen tick; clients stop prediction until resume packet.
- **Disconnect:** server times out silent clients, clears buffered inputs, and notifies lobby; reconnect uses last player ID with re-auth.

## Observability
- Sequence/latency metrics in each direction; clients surface rollback counts and correction magnitudes for QA tuning.

