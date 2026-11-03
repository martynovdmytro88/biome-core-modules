# com.biome.playerdata

## Purpose

The one home of the player's persistent facts: state that grows with development (tutorial, battle mode, player id once
the backend exists). v0 carries a single fact — whether the player has opened the game before. Main thread only.

This package carries the contract. It knows nothing about the host, DI or the Unity scene.

## Layers

- `Biome.PlayerData.Abstract` (`Abstract/`, engine-free) — `IPlayerDataService`: `IsFirstLaunch { get; }` ·
  `MarkLaunched()`. `IsFirstLaunch` is a session snapshot: read once at startup, unchanged for the whole session.
- `Biome.PlayerData.Data` (`Data/`, engine-free) — `PlayerDataRecord`, the one growing record (v0 field: has-launched
  flag).
- `Biome.PlayerData.Fakes` (`Fakes/`, Editor-only, not auto-referenced) — `InMemoryPlayerDataService`, no store, same
  snapshot semantics as production.

The host owns `Runtime`, `Installers`, `InstallersMock`, `Events` and `Editor`. `Templates~/` holds empty skeletons of the
host layers.

## Tests

`Tests/Editor` (`Biome.PlayerData.Editor.Tests`) holds the abstract, store-free `PlayerDataContractTests` (the
first-launch snapshot), which runs here on `InMemoryPlayerDataService`. `Tests/Editor/Consumer` proves that a consumer
needs only `Abstract` and `Fakes`.

A host must prove its own implementation: reference `Biome.PlayerData.Editor.Tests` from its test assembly, derive from
the suite and override `CreateFresh`. The store-backed behaviour (reading on start, writing through, a missing, corrupt
or version-mismatched record) is proven only by the host's own cases beside it.

Requires `com.unity.test-framework` in the project (deliberately not a package dependency). A package outside
`Packages/` also needs an entry in `testables`.

## Decisions kept in code

- **One growing record.** New facts widen `PlayerDataRecord`; there is no record per fact group.
- **A widening is a version bump.** A shape change raises the record's version; the previous shape stays frozen as
  `PlayerDataRecordV<n>` under the same key, and the migration is owned by PlayerData.
