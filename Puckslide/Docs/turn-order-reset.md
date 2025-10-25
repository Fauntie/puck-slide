# Turn order reset regression

## What went wrong
- `GameSetupManager.OnEnable` resets Phase 1 by invoking `TurnManager.ResetTurnOrder()` as soon as the setup canvas becomes active.
- During the most recent scene changes we now activate the setup UI before the `TurnManager` singleton finishes its `Awake`/`OnEnable` cycle, so `TurnManager.Instance` is still `null` when the reset is requested.
- The guard clause we added in `TurnManager.ResetTurnOrder()` logs the warning that shows up in the console screenshot and exits early, which means the `EventsManager.OnTurnChanged` event keeps broadcasting whichever colour moved last in the previous session.
- Because no new "white to move" broadcast is sent, the launcher logic keeps thinking it is still white's turn, so play never alternates during the launch phase.

## Follow-up task
- Rework the turn reset so it runs after the `TurnManager` singleton has finished initialising (for example by letting `TurnManager` listen for the setup event itself, or by queueing the reset until `Instance` is ready) and verify that the launch phase alternates turns correctly.
