# Puck Slide

## Registry Lifecycle

The project now uses two registries to track active gameplay objects:

- **PuckRegistry** maintains references to all `PuckController` instances.
- **PieceRegistry** maintains references to all chess `Piece` instances.

Each `PuckController` and `Piece` publishes spawn and despawn events through the global `EventBus` in their `OnEnable` and `OnDisable` methods. The registries subscribe to these `PuckSpawned`/`PuckDespawned` and `PieceSpawned`/`PieceDespawned` events and update their internal lists accordingly.

Systems that previously searched the scene with `FindObjectsOfType` now query the registries instead. This event-driven approach avoids expensive scene scans and ensures all peers operating over the network share a consistent view of active objects. Consumers can obtain stable snapshots using `PuckRegistry.Instance.GetPucks()` and `PieceRegistry.Instance.GetPieces()`.

When objects are destroyed or a scene is unloaded, their `OnDisable` method triggers the corresponding despawn event so the registries automatically stay in sync.

Use these registries as the authoritative source of object lists for future network synchronization work.
