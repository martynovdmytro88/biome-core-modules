# PlayerData

The one home of the player's persistent facts: state that grows with development (tutorial, battle mode, player id once
the backend exists). v0 carries a single fact — whether the player has opened the game before. Persistence goes through
`Biome.LocalStore` only; PlayerData never touches PlayerPrefs directly. Main thread only.

## Layers (asmdefs)

- `Biome.PlayerData.Abstract` (`Abstract/`, engine-free) — `IPlayerDataService`: `IsFirstLaunch { get; }` ·
  `MarkLaunched()`. `IsFirstLaunch` is a session snapshot: read once at startup, unchanged for the whole session.
- `Biome.PlayerData.Data` (`Data/`, engine-free) — `PlayerDataRecord`, the one growing record (v0 field: has-launched
  flag).

Further layers arrive with later slices of the task; none is declared here until its folder exists.

## Initialization (future)

The call that creates the service and reads the record from LocalStore will be made by the init state of the game
states module, which does not exist yet. Until it lands, the module is exercised only by its tests and exposes its
public API (`IPlayerDataService`); nothing in the app calls it at startup.

## Extension rule

- **One growing record.** New facts widen `PlayerDataRecord`; there is no record per fact group.
- **A widening is a version bump.** The record is registered with LocalStore as `type → { key, version }`; a shape
  change raises the version, and an old record then reads as `VersionMismatch` (LocalStore has no migration).
- **The previous shape stays frozen** as `PlayerDataRecordV<n>`, registered under the **same key** with its old
  version. On `VersionMismatch` the service reads the older shapes and converts to the current one.
- **The migration is owned by PlayerData**, not by LocalStore or a consumer.
- **v0 = version 1, no migration.** There is no older shape; the first widening lays the chain. Until then a
  `Corrupt` or `VersionMismatch` record reads as a first launch.
