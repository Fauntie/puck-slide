# Puck Slide Architecture Overview

## Core Phases
- **Phase 1 (Setup):** `GameSetupManager` collects piece counts/sticky flags and raises `EventsManager.OnPieceSetupData` so UI (e.g., `Phase1PieceButton`) can show available pieces. Turn order is reset via `PuckController.ResetTurnOrder()` and the board is cleared by `EventsManager.OnDeletePucks`.
- **Phase 2 (Chess Play):** Triggered from `GridManager.StartPhase2()`. Puck positions are snapped to the grid, the Phase 1 UI/environment are hidden, Phase 2 equivalents are enabled, and `EventsManager.OnTurnChanged` notifies listeners of the active side. `Phase2Manager` marks the phase active and prepares the board transform for flips.

## State & Data Flow
- **Piece configuration:** `PieceSetupState` holds per-piece counts and sticky flags with a max cap (`MaxPiecesPerColor`). `GameSetupManager` wraps the state for UI and publishes snapshots through `EventsManager.OnPieceSetupData` when starting Phase 1 placement.
- **Board layout:** `GridManager` iterates existing `PuckController` instances, snaps them to the grid, records `ChessPiece` occupancy by `Vector2Int`, and broadcasts the map via `EventsManager.OnBoardLayout`.
- **Turn state:** `PuckController` owns the active turn flag and toggles it when a shot fully stops (or immediately in Phase 2). It also triggers camera/board flips through `BoardFlipper` and raises `EventsManager.OnTurnChanged` so UI can react.
- **Events:** `EventsManager` centralizes lightweight event channels (puck lifecycle, board layout, turn changes, board flip state). The generic `Evt<T>` supports replaying the last value to late subscribers when requested.

## Input & Movement
- **Input handling:** `PuckController` handles drag input with on-screen line renderers and trajectory preview. Drag distance is clamped (`m_MaxDragDistance`), and impulse strength scales by pull distance up to `m_MaxShootForce`.
- **Movement rules:** Sticky pieces become `RigidbodyType2D.Static` once their velocity drops below a threshold. Pawns are constrained to their starting half of the board during Phase 1 after crossing the `BoardTrigger`.
- **Grid alignment:** `PuckController.SnapToGrid` and `UpdateGridPosition` translate world positions to tile centers and logical coordinates using the tile size and origin set by `GridManager`.

## Board Presentation
- **Grid generation:** `GridGenerator` builds an 8x8 alternating tile board sized by `m_TileSize`, using `BoardFlipper.SetBoard` in `Awake` to register the board for flips.
- **Board flipping:** `BoardFlipper` animates 180° board rotations (including pucks and chess pieces) and offers a fast camera flip for Phase 2 turns. It tracks the board center to keep visuals aligned and exposes flip state via `EventsManager.OnBoardFlipState`.
- **Physics gating:** `OneWayWall` shifts puck layers in `FixedUpdate` so shots can pass through the entry wall from one side while still bouncing out when returning.

## Module Responsibilities
- **GameSetupManager:** UI-facing wrapper over `PieceSetupState`; coordinates entry into Phase 1 placement and notifies listeners when counts change.
- **PieceSetupState:** Pure logic model for piece counts/sticky flags with cloning utilities for safe snapshots.
- **PuckController:** Player input, shot execution, movement rules, turn progression, and grid alignment per piece.
- **GridManager:** Board snapping, layout generation for Phase 2, and phase transition orchestration.
- **Phase2Manager:** Marks Phase 2 active, aligns the board for flips, and cleans up pieces when ending the game.
- **EventsManager / Evt:** Central hub for cross-system signals with opt-in last-value replay support.
- **BoardFlipper:** Animates/handles board and camera flips while keeping transforms centered.
- **OneWayWall:** Maintains directional pass-through by swapping puck physics layers based on velocity.

## Networking Considerations
The event-driven layout (`EventsManager`), pure piece state (`PieceSetupState`), and deterministic grid snapping (`GridManager`, `PuckController`) form stable seams for networking: serialize `PieceSetupState` snapshots for setup sync, broadcast `OnBoardLayout` changes after snaps, and mirror `OnTurnChanged` / `OnBoardFlipState` for turn order and presentation consistency.
