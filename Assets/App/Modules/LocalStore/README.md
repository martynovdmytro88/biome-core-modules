# LocalStore

Small typed key-value persistence over PlayerPrefs for **short-lived, low-volume, non-critical** data (audio/input
settings, tutorial flags, UI preferences). **Not a save-game system** — no files, no atomic writes, no migrations.
Main thread only; no async, encryption, sync or per-profile partitioning.

## Layers (asmdefs)

- `Biome.LocalStore.Abstract` (`Abstract/`, engine-free) — the contract consumers bind to: `ILocalStoreService`
  (`TryGet / Set / Has / Remove / Clear / Flush`), `ReadStatus` (`Ok · Missing · Corrupt · VersionMismatch`),
  `ILocalStoreRegistry` + `LocalStoreEntry` (`type → { key, version }`, an `IEquatable` value type), the two
  exceptions. No serializer type in any signature — a reference-graph fact, not an agreement.
- `Biome.LocalStore.Data` (`Data/`, engine-free) — the record format facts: envelope `{"v":<int>,"d":<payload>}`,
  manifest = JSON array of own prefixed keys.
- `Biome.LocalStore.Config` (`Config/`) — `LocalStoreKeys` (prefix `ls.`, manifest key `ls.__manifest`) and
  `LocalStoreLimits` (key length · per-value UTF-8 bytes · total budget). The default figures are **placeholders**
  marked for the operator, not verified limits.
- `Biome.LocalStore` (`Runtime/`) — `LocalStoreFactory.CreatePlayerPrefs(registry, limits)` (the public entry) ·
  `Core/` (the `internal` `LocalStore` implementation + the single module-owned `JsonSerializerSettings`,
  `TypeNameHandling` off) · `BackendSeam/` (`internal ILocalStoreBackend`, string → string) · `PlayerPrefsBackend/`
  (the production seam implementation, the only PlayerPrefs code) · `Registry/` (`DictionaryLocalStoreRegistry`, a
  registry built in code — the composer builds the app's one).
- `Biome.LocalStore.Installers` (`Installers/`, not auto-referenced) — `LocalStoreInstaller` (VContainer
  `IInstaller`): one `ILocalStoreService` per scope over PlayerPrefs. The module's only VContainer reference in
  shipping code.
- `Biome.LocalStore.Fakes` (`Fakes/`, Editor-only, not auto-referenced, engine-free) — the fake of the seam: the real
  core over an in-memory `ILocalStoreBackend`. Public entry `InMemoryLocalStore.Create(registry)`; the core and the
  fake backend stay internal.
- `Biome.LocalStore.Installers.Mock` (`InstallersMock/`, Editor-only, not auto-referenced) — `LocalStoreMockInstaller`,
  the mock-profile twin of the installer: the same `ILocalStoreService`, backed by the fake instead of PlayerPrefs.
  Takes only the registry. References Abstract + Fakes + VContainer — never Runtime or Config.
- `Biome.LocalStore.Tests` (`Tests/`, EditMode) — the contract tests written once over the seam and run on the
  in-memory fake and on PlayerPrefs (cleans only the keys it touched, never `DeleteAll`).
- `Biome.LocalStore.Tests.Consumer` (`Tests/Consumer/`, EditMode) — the consumer fixture over Abstract + Fakes only:
  what a consumer can rely on, through the fake.

## Decisions kept in code

Manifest of own keys + common prefix (Clear() walks the manifest, never `DeleteAll`; a dangling entry is pruned on
read) · version per record · one strict read policy (any parse failure → `Corrupt`) · size in UTF-8 bytes, a violation
throws with the store unchanged · raw-string cache, never the object · an instance behind the contract, no static ·
`Flush()` is the caller's, no engine lifecycle in the module · the core is `internal`; the public entries are the
factory and `DictionaryLocalStoreRegistry` — the registry stays public because type registration exists only on the
concrete class, and widening `ILocalStoreRegistry` would change the contract · `InternalsVisibleTo` only for the
module's own `.Tests` / `.Fakes` · the fake is the real core over an in-memory seam, not a re-implementation, so a
consumer's tests exercise production logic · the mock installer takes only the registry (the fake's limits are
placeholders) · the consumer fixture proves the contract is reachable without Runtime or Config.

## Not in this slice

Concrete limit values · registry authoring · a non-throwing probe · the AOT device build.

The section between the markers is emitted by `discover --mode refresh` (unity-cli) from the module's callables
(`[BiomeCallable]` + `///`); never edit it by hand — `discover --mode check` reports drift.

<!-- biome:generated:start -->
<!-- biome:generated:end -->
