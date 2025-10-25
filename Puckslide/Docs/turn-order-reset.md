# Turn order reset regression

## What went wrong
- `GameSetupManager.OnEnable` resets Phase 1 by invoking `TurnManager.ResetTurnOrder()` as soon as the setup canvas becomes active.
- During the most recent scene changes we now activate the setup UI before the `TurnManager` singleton finishes its `Awake`/`OnEnable` cycle, so `TurnManager.Instance` is still `null` when the reset is requested.
- The guard clause we added in `TurnManager.ResetTurnOrder()` logs the warning that shows up in the console screenshot and exits early, which means the `EventsManager.OnTurnChanged` event keeps broadcasting whichever colour moved last in the previous session.
- Because no new "white to move" broadcast is sent, the launcher logic keeps thinking it is still white's turn, so play never alternates during the launch phase.

## Fix
- `TurnManager.ResetTurnOrder()` now queues the request if the singleton is not initialised yet. When the manager finishes enabling it performs the pending reset before sending the initial `OnTurnChanged` broadcast, restoring the white/black launch alternation.
