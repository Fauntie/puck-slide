# Lobby & Countdown UX Expectations

These notes capture the current interaction model so client and host UIs can mirror behavior during networking work.

## Lobby roles
- Two-player lobby with a single host (Player 1) and guest (Player 2).
- Host sees fully interactive controls; guest sees the same controls but disabled/greyed out with an optional tooltip "Only the host can change settings." when they try to interact.
- Status label examples: "You are the HOST/GUEST", "Waiting for guest to join…", "Guest connected."

## Snapshot behavior
- Host is authoritative. Every change the host makes to colors/skins, board layout, or rules increments `LobbySnapshot.version` and is sent to the guest.
- Guests always render the most recent snapshot they have; stale snapshots (lower version) are ignored.
- Late joiners during the lobby immediately receive the latest snapshot and see an informative line such as "Joined lobby – waiting for host to start.".

## Countdown & start
- Host presses **Start Match** to trigger a shared countdown (e.g., 3→2→1→Go!).
- While the countdown runs, all lobby inputs are disabled for both players.
- If the countdown is canceled (disconnect, etc.), controls re-enable and a brief message such as "Countdown canceled – player left the lobby." is shown.
- Late-joining during a countdown is allowed; the guest sees the in-progress number and cannot change any settings.

## Disconnects and re-entry
- No host migration in v1; if the host disconnects, the guest is returned to the main menu with a message like "Host disconnected – match ended.".
- Guest disconnect during match: host can stay to view the board or exit.
- Reconnects during a lobby use the persisted `LobbySnapshot`; in-match reconnects are not supported in v1.
- Late join during an active match is blocked with a simple message (e.g., "This match is already in progress. Please wait for the next game."), possibly redirecting back to matchmaking.

## Visual feedback
- Keep a small, always-on status label updated with lobby state messages such as "Match starting in 3…" or "Host disconnected – returning to menu…".
