# Puckslide P2P Branch Audit (snapshot)

## High-level status by subsystem
- **Core gameplay & turn flow** – Board drag/drop and puck shooting flows are present, with turn changes broadcast from `PuckController` via `NetworkSessionManager` for host authority. Phase 2 activation deletes pucks and sets board flip offsets. However, there is no authoritative enforcement of board layout or turn ownership in the board controller. 
- **Input & command pipeline** – Local input is gathered in `LocalInputRouter`, dispatched through `NetworkSessionManager.SubmitPlayerCommand`, and processed on the host by `SimulationCommandProcessor` into `BoardController` or `PuckController`. The fallback path for non-host clients without Mirror collapses to local delivery, bypassing host authority.
- **Game & puck snapshots** – Host periodically publishes puck-only snapshots via `GameStateSnapshot.Capture` and `PuckStateSnapshotMessage`; clients lerp toward target states in `PuckStateReplicator`. Snapshots lack lobby-id validation on the client side, so late/foreign messages could overwrite local state.
- **Lobby & roles** – Lobby snapshots set `LobbyState.LocalIsHost`/`LocalIsWhitePlayer`, and UI reflects host-only controls. Snapshot version checks exist, but there is no guard against applying snapshots from a different lobby.
- **Network orchestration** – `NetworkSessionManager` owns loops, timeouts, and host/client role, while `MirrorNetworkBridge` relays events over Mirror. Disconnect handling stops snapshot loops but relies on `NetworkDisconnectHandler` for Mirror teardown.
- **Steam/transport** – `SteamworksBootstrap` can drive `NetworkSessionManager` but warns if no session manager is found. Host/guest split follows the Steam lobby owner, but transport start/stop is only wired through `NetworkSessionManager` and the disconnect handler.
- **Error handling & UX** – `NetworkErrorHUD` and `NetworkStatusHUD` react to `NetworkEvents`, and `GameBootstrapper` toggles lobby/game UIs on start. No explicit reconnection or duplicate-disconnect suppression is present.

## Notable issues and recommended fixes
1. **Client fallback bypasses host authority when Mirror is unavailable**
   - Location: `NetworkSessionManager.SubmitPlayerCommand` fallback path.
   - Problem: Non-host clients without Mirror call `NetworkEvents.OnPlayerCommandSubmitted` locally, which the host never receives, so remote input is ignored while still bypassing host-only validation. This also risks offline simulations on clients in what was intended to be an online session.
   - Fix: When not offline and not host, drop commands if Mirror transport is unavailable or queue a clear error instead of invoking the local event. For offline single-player, keep the direct host apply path.
2. **Puck snapshots applied without lobby validation**
   - Location: `PuckStateReplicator.OnSnapshot/OnSpawned`.
   - Problem: Clients accept any `PuckStateSnapshotMessage`/`PuckSpawnMessage` even if the lobby ID does not match the current session, so stale or cross-lobby packets could overwrite local puck state or teleport pucks after leaving a match.
   - Fix: Compare `message.LobbyId` with `NetworkSessionManager.Instance?.LobbyId` before applying updates; ignore mismatches.
3. **Missing guard for lobby snapshots from other sessions**
   - Location: `LobbyState.ApplySnapshot`.
   - Problem: Snapshot versioning is checked, but no lobby-id comparison is done before overwriting local state. A stray snapshot from another lobby could flip `LocalIsHost`/color unexpectedly.
   - Fix: Early-return if `s_LatestSnapshot` exists and `snapshot.LobbyId` differs from the current lobby ID tracked by `NetworkSessionManager`.
4. **Disconnect handling depends on separate component**
   - Location: `NetworkSessionManager.HandleDisconnect` vs. `NetworkDisconnectHandler.OnDisconnected`.
   - Problem: Session manager stops snapshot loops but does not stop Mirror transport; that only happens if a `NetworkDisconnectHandler` is present. If absent (e.g., in a stripped scene or offline mode), Mirror connections may linger.
   - Fix: Add optional transport shutdown inside `NetworkSessionManager.HandleDisconnect` when a Mirror `NetworkManager` is available, or enforce `NetworkDisconnectHandler` presence via `[RequireComponent]` in networked scenes.
5. **Board commands are host-only but lack authority checks**
   - Location: `SimulationCommandProcessor.FixedUpdate` / `NetworkSessionManager.TryApplyCommandAsHost`.
   - Problem: Board commands enqueue and process without verifying `LobbyState.LocalIsHost`, so a misconfigured client marked as host locally could still simulate board changes.
   - Fix: Ensure `TryApplyCommandAsHost` rejects all commands when `m_IsHost` is false (remove the PointerUp-only guard) and gate `SimulationCommandProcessor` execution when not authoritative.

## Quick TODOs
- Harden client command path to require Mirror transport (or explicit offline mode) before applying commands locally.
- Add lobby-id guards to puck snapshot handling and lobby snapshot application.
- Integrate transport shutdown into `NetworkSessionManager.HandleDisconnect` or mandate `NetworkDisconnectHandler` on network scenes.
- Enforce host checks when applying board/puck commands in the simulation loop.
