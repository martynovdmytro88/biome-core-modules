# PlayerData

The one home of the player's persistent facts: state that grows with development (tutorial, battle mode, player id once
the backend exists). v0 carries a single fact — whether the player has opened the game before. Persistence goes through
`Biome.LocalStore` only; PlayerData never touches PlayerPrefs directly. Main thread only.

## Package and host

The contract and the engine-free layers come from the package `com.biome.playerdata` 0.1.3; its README describes them.
This folder holds the host layers.

From the package:

- `Biome.PlayerData.Abstract` (`Abstract/`, engine-free) — `IPlayerDataService`: `IsFirstLaunch { get; }` ·
  `MarkLaunched()`. `IsFirstLaunch` is a session snapshot: read once at startup, unchanged for the whole session.
- `Biome.PlayerData.Data` (`Data/`, engine-free) — `PlayerDataRecord`, the one growing record (v0 field: has-launched
  flag).
- `Biome.PlayerData.Fakes` (`Fakes/`, Editor-only) — `InMemoryPlayerDataService`, the store-free fake.
- `Biome.PlayerData.Editor.Tests` (`Tests/Editor/`) — the store-free contract suite with its fixture on the fake, and the
  record tests; `Biome.PlayerData.Editor.Tests.Consumer` (`Tests/Editor/Consumer/`) — the consumer fixture.

Here:

- `Biome.PlayerData.Events` (`Events/`) — `PlayerDataChangedEvent`.
- `Biome.PlayerData` (`Runtime/`) — `PlayerDataService` (over `ILocalStoreService`, publishes the change) and
  `PlayerDataStoreEntry` (the record's LocalStore key and version).
- `Biome.PlayerData.Installers` (`Installers/`, not auto-referenced) — `PlayerDataInstaller`: one service per scope over
  the scope's `ILocalStoreService`.
- `Biome.PlayerData.Installers.Mock` (`InstallersMock/`, Editor-only, not auto-referenced) — `PlayerDataMockInstaller`:
  the fake, optionally already launched.
- `Biome.PlayerData.Editor` (`Editor/`, Editor-only) — `PlayerDataEditorMenu`: `Biome/PlayerData/ResetToDefaults`.
- `Biome.PlayerData.Tests.Host` (`Tests/Host/`, EditMode) — `ProductionPlayerDataContractTests`: the package's suite on the
  production service over LocalStore, plus the store-backed cases (the snapshot read on start, the one-shot write, the
  failed-read policy through a test-local store double); `PlayerDataInstallerTests`: both installers resolved in a
  container.

## Initialization

`AppProfileScope` registers `PlayerDataRecord` in the LocalStore registry and adds `PlayerDataInstaller`; the service
reads the record when the container builds it.

## Extension rule

- **One growing record.** New facts widen `PlayerDataRecord`; there is no record per fact group.
- **A widening is a version bump.** The record is registered with LocalStore as `type → { key, version }`; a shape
  change raises the version, and an old record then reads as `VersionMismatch` (LocalStore has no migration).
- **The previous shape stays frozen** as `PlayerDataRecordV<n>`, registered under the **same key** with its old
  version. On `VersionMismatch` the service reads the older shapes and converts to the current one.
- **The migration is owned by PlayerData**, not by LocalStore or a consumer.
- **v0 = version 1, no migration.** There is no older shape; the first widening lays the chain. Until then a
  `Corrupt` or `VersionMismatch` record reads as a first launch.
