# LocalStore

Small typed key-value persistence over PlayerPrefs for **short-lived, low-volume, non-critical** data (audio/input
settings, tutorial flags, UI preferences). **Not a save-game system** — no files, no atomic writes, no migrations.
Main thread only; no async, encryption, sync or per-profile partitioning.

## Package and host

The contract and the engine-free layers come from the package `com.biome.localstore` 0.1.2; its README describes them.
This folder holds the host layers.

From the package:

- `Biome.LocalStore.Abstract` (`Abstract/`, engine-free) — the contract consumers bind to: `ILocalStoreService`
  (`TryGet / Set / Has / Remove / Clear / Flush`), `ReadStatus` (`Ok · Missing · Corrupt · VersionMismatch`),
  `ILocalStoreRegistry` + `LocalStoreEntry` (`type → { key, version }`, an `IEquatable` value type), the seam
  `ILocalStoreBackend` (string → string), the two exceptions. No serializer type in any signature — a reference-graph
  fact, not an agreement.
- `Biome.LocalStore.Data` (`Data/`, engine-free) — the record format facts: envelope `{"v":<int>,"d":<payload>}`,
  manifest = JSON array of own prefixed keys.
- `Biome.LocalStore.Config` (`Config/`) — `LocalStoreKeys` (prefix `ls.`, manifest key `ls.__manifest`) and
  `LocalStoreLimits` (key length · per-value UTF-8 bytes · total budget). The default figures are **placeholders**
  marked for the operator, not verified limits.
- `Biome.LocalStore.Fakes` (`Fakes/`, Editor-only, not auto-referenced, engine-free) — `InMemoryLocalStoreBackend`, the
  in-memory fake of the seam.
- `Biome.LocalStore.Editor.Tests` (`Tests/Editor/`) — the contract suites of the seam and of the service.

Here:

- `Biome.LocalStore` (`Runtime/`) — `LocalStoreFactory.CreatePlayerPrefs(registry, limits)` (the public entry) ·
  `Core/` (the `internal` `LocalStore` implementation + the single module-owned `JsonSerializerSettings`,
  `TypeNameHandling` off) · `PlayerPrefsBackend/` (the production seam implementation, the only PlayerPrefs code) ·
  `Registry/` (`DictionaryLocalStoreRegistry`, a registry built in code — the composer builds the app's one).
- `Biome.LocalStore.Installers` (`Installers/`, not auto-referenced) — `LocalStoreInstaller` (VContainer
  `IInstaller`): one `ILocalStoreService` per scope over PlayerPrefs. The module's only VContainer reference in
  shipping code.
- `Biome.LocalStore.Installers.Mock` (`InstallersMock/`, Editor-only, not auto-referenced) — `InMemoryLocalStore.Create(registry)`
  (namespace `Biome.LocalStore.Fakes`): the real core over the in-memory backend; and `LocalStoreMockInstaller`, the
  mock-profile twin of the installer: the same `ILocalStoreService`, backed by that store instead of PlayerPrefs. Takes
  only the registry.
- `Biome.LocalStore.Tests.Host` (`Tests/Host/`, EditMode) — the package's suites on production:
  `PlayerPrefsLocalStoreBackendContractTests` (the seam on PlayerPrefs), `InMemoryLocalStoreContractTests` (the core over
  the in-memory backend), `PlayerPrefsLocalStoreContractTests` (the core over PlayerPrefs; cleans only the keys it
  touched, never `DeleteAll`).

## Decisions kept in code

Manifest of own keys + common prefix (Clear() walks the manifest, never `DeleteAll`; a dangling entry is pruned on
read) · version per record · one strict read policy (any parse failure → `Corrupt`) · size in UTF-8 bytes, a violation
throws with the store unchanged · raw-string cache, never the object · an instance behind the contract, no static ·
`Flush()` is the caller's, no engine lifecycle in the module · the core is `internal`; the public entries are the
factory and `DictionaryLocalStoreRegistry` — the registry stays public because type registration exists only on the
concrete class, and widening `ILocalStoreRegistry` would change the contract · `InternalsVisibleTo` only for the
module's own `.Installers.Mock` / `.Tests.Host` · the fake is the real core over an in-memory seam, not a re-implementation, so a
consumer's tests exercise production logic · the mock installer takes only the registry (the fake's limits are
placeholders).

## Not in this slice

Concrete limit values · registry authoring · a non-throwing probe · the AOT device build.

The section between the markers is emitted by `discover --mode refresh` (unity-cli) from the module's callables
(`[BiomeCallable]` + `///`); never edit it by hand — `discover --mode check` reports drift.

<!-- biome:generated:start -->
<!-- biome:generated:end -->
