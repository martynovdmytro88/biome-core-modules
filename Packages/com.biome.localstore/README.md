# com.biome.localstore

## Purpose

Small typed key-value persistence for **short-lived, low-volume, non-critical** data (audio/input settings, tutorial
flags, UI preferences). **Not a save-game system** — no files, no atomic writes, no migrations. Main thread only; no
async, encryption, sync or per-profile partitioning.

This package carries the contract. It knows nothing about the host, DI or the Unity scene.

## Layers

- `Biome.LocalStore.Abstract` (`Abstract/`, engine-free) — the contract consumers bind to: `ILocalStoreService`
  (`TryGet / Set / Has / Remove / Clear / Flush`), `ReadStatus` (`Ok · Missing · Corrupt · VersionMismatch`),
  `ILocalStoreRegistry` + `LocalStoreEntry` (`type → { key, version }`, an `IEquatable` value type), the two
  exceptions, and `ILocalStoreBackend`, the `string → string` backend seam. No serializer type in any signature.
- `Biome.LocalStore.Data` (`Data/`, engine-free) — the record format facts: envelope `{"v":<int>,"d":<payload>}`,
  manifest = JSON array of own prefixed keys.
- `Biome.LocalStore.Config` (`Config/`) — `LocalStoreKeys` (prefix `ls.`, manifest key `ls.__manifest`) and
  `LocalStoreLimits` (key length · per-value UTF-8 bytes · total budget). The default figures are **placeholders**
  marked for the operator, not verified limits.
- `Biome.LocalStore.Fakes` (`Fakes/`, Editor-only, not auto-referenced, engine-free) — `InMemoryLocalStoreBackend`, the
  in-memory implementation of the seam.

The host owns `Runtime` (the core and the production backend), `Installers` and `InstallersMock`. `Templates~/` holds
empty skeletons of the host layers.

## Tests

`Tests/Editor` (`Biome.LocalStore.Editor.Tests`) holds two abstract suites. `LocalStoreBackendContractTests` covers the
backend seam and runs here on `InMemoryLocalStoreBackend`. `LocalStoreContractTests` covers `ILocalStoreService`; **the
package carries no implementation of the service, so this suite does not run until a host binds it.**

A host must prove its own implementation: reference `Biome.LocalStore.Editor.Tests` from its test assembly, derive from
both suites and override `CreateBackend` and `CreateStore`.

Requires `com.unity.test-framework` in the project (deliberately not a package dependency). A package outside
`Packages/` also needs an entry in `testables`.

## Decisions kept in code

Manifest of own keys + common prefix · version per record · one strict read policy (any parse failure → `Corrupt`) ·
size in UTF-8 bytes, a violation throws with the store unchanged · raw-string cache, never the object · an instance
behind the contract, no static · `Flush()` is the caller's, no engine lifecycle in the module.
