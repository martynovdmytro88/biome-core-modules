# com.biome.assets

## Purpose

Loads assets by key or label and keeps them in memory for as long as a **context** holds them. A context is a lifetime
scope (global · game state · window); an asset leaves memory when no context holds it, and a key held by several
contexts is loaded once. A leaf service: it publishes no events, persists nothing and references no other module. Main
thread only.

This package carries the contract. It knows nothing about the host, DI or the Unity scene.

## Layers

Every namespace equals its asmdef `name` (`rootNamespace` equals `name` too).

- `Biome.Assets.Abstract` (`Abstract/`, refs UniTask only, engine-free) — the contract: `IAssetsService`
  (`GlobalContext` · `CreateContext`) · `IAssetsContext` (`CreateChild` · `LoadAsync` by key and by key list ·
  `PreloadByLabelAsync` · `LoadSceneAsync` · `UnloadSceneAsync` · `Release` · `Unload`) · the value types `AssetKey` and
  `AssetLabel` (plain strings, ordinal) and `SceneHandle` (key plus instance id, opaque, no engine type) · the backend
  seam `IAssetBackend` · `ISceneInstance` · `IAssetHandle` (load-by-key · resolve-label · load-scene, no ref counting).
- `Biome.Assets.Data` (`Data/`, engine-free) — reserved: the asmdef exists with no types.
- `Biome.Assets.Fakes` (`Fakes/`, Editor-only, not auto-referenced) — `FakeAssetBackend`, the fake of the seam.

The host owns `Runtime` (the store, the contexts and the production backend), `Config`, `Installers` and
`InstallersMock`. `Templates~/` holds empty skeletons of the host layers.

## Tests

`Tests/Runtime` (`Biome.Assets.Tests`) holds two abstract suites. `AssetBackendContractTests` covers the loading seam
and runs here on `FakeAssetBackend` (bound in `Tests/Editor`, `Biome.Assets.Editor.Tests`). `AssetsContractTests` covers
`IAssetsService` and `IAssetsContext`; **the package carries no implementation of the service, so this suite does not
run until a host binds it.**

A host must prove its own implementation: reference `Biome.Assets.Tests` from its test assembly (it is not Editor-only,
so PlayMode works), derive from both suites and override `CreateService` / `CreateBackend` plus the key, label and scene
members.

`create_module --host_only` lays that binding on Addressables from `Templates~/Tests`: `Tests/PlayMode` (both suites over
the smoke keys, red until `CreateService` / `CreateBackend` are bound), `Tests/Editor` (the prebuild step that puts the
smoke content in a temporary Addressables group and removes it after the run) and `Tests/Content` (the smoke assets,
with fixed GUIDs the prebuild step looks them up by). The host needs `com.unity.addressables`.

Requires `com.unity.test-framework` in the project (deliberately not a package dependency). A package outside
`Packages/` also needs an entry in `testables`.

## Decisions kept in code

- **A (context, key) pair is a set.** Loading a key twice in one context holds one reference; one `Release` drops it.
  `Unload` closes the context, unloads its children first, cancels its pending waiters, then releases its keys.
- **Shared loads.** A load in flight is shared by its waiters; one waiter cancelling does not cancel it — the last one
  leaving does. Loading a held key completes synchronously.
- **Closed context.** `CreateChild`, both `LoadAsync` and `PreloadByLabelAsync` throw `ObjectDisposedException`
  synchronously; `Release`, `Unload` and `Dispose` are no-ops.
- **Scenes belong to a context.** `LoadSceneAsync` opens the scene additively and the context owns it; each call is a
  new instance with its own `SceneHandle`. `UnloadSceneAsync` closes one; a handle the context does not own is a no-op.
