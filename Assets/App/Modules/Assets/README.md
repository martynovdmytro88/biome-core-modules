# Assets

## Purpose

Loads assets by key or label over Addressables and keeps them in memory for as long as a **context** holds them. A
context is a lifetime scope (global · game state · window); an asset leaves memory when no context holds it, and a key
held by several contexts is loaded once. A leaf service: it publishes no events, persists nothing and references no
other module. Main thread only.

## Layers

Every namespace equals its asmdef `name` (`rootNamespace` equals `name` too).

- `Biome.Assets.Abstract` (`Abstract/`, refs UniTask only, engine-free) — the contract: `IAssetsService`
  (`GlobalContext` · `CreateContext`) · `IAssetsContext` (`CreateChild` · `LoadAsync` by key and by key list ·
  `PreloadByLabelAsync` · `LoadSceneAsync` · `UnloadSceneAsync` · `Release` · `Unload`) · the value types `AssetKey` and
  `AssetLabel` (plain strings, ordinal) and `SceneHandle` (key plus instance id, opaque, no engine type).
- `Biome.Assets.Data` (`Data/`, engine-free) and `Biome.Assets.Config` (`Config/`, refs Data) — reserved: both asmdefs
  exist with no types.
- `Biome.Assets` (`Runtime/`) — `AssetsFactory.CreateAddressables()` (the public entry) · `Contexts/` (`AssetsService`,
  `AssetsContext`, internal) · `RefStore/` (one entry per key with its holding contexts and one shared in-flight load) ·
  `Backends/` (`internal IAssetBackend`, load-by-key · resolve-label · load-scene, no ref counting; `AddressablesBackend`,
  the only Addressables code).
- `Biome.Assets.Fakes` (`Fakes/`, Editor-only, not auto-referenced) — `FakeAssetBackend` and
  `FakeAssets.CreateService(backend)`: the real store and contexts over the fake backend, a behavioural stand-in.
- `Biome.Assets.Installers` (`Installers/`, not auto-referenced) — `AssetsInstaller` (VContainer `IInstaller`): one
  `IAssetsService` per scope, disposed with it. The module's only VContainer reference in shipping code.
- `Biome.Assets.Installers.Mock` (`InstallersMock/`, Editor-only, not auto-referenced) — `MockAssetsInstaller`: the same
  contract over a caller-kept `FakeAssetBackend` (seed assets and scenes, read counters).
- Tests: `Biome.Assets.Tests.Shared` (`Tests/Shared/`) — the contract suite written once over the contract, plus the
  `CanonicalProbe` component double; `Biome.Assets.Tests` (`Tests/`, EditMode) — the suite on the fake and the
  implementation-side cases; `Biome.Assets.Tests.PlayMode` (`Tests/PlayMode/`) — the suite on Addressables;
  `Biome.Assets.Tests.Editor` (`Tests/Editor/`) — the Addressables test content setup.

## Decisions kept in code

- **A (context, key) pair is a set.** Loading a key twice in one context holds one reference; one `Release` drops it.
  `Unload` closes the context, unloads its children first, cancels its pending waiters, then releases its keys.
- **Shared loads.** A load in flight is shared by its waiters; one waiter cancelling does not cancel it — the last one
  leaving does. Loading a held key completes synchronously.
- **One canonical type per key.** Every key is loaded once as `UnityEngine.Object`; the caller's `T` is a view taken
  by a cast — a `GameObject` yields its `T` component (an interface included), otherwise `InvalidCastException`.
- **Closed context.** `CreateChild`, both `LoadAsync` and `PreloadByLabelAsync` throw `ObjectDisposedException`
  synchronously; `Release`, `Unload` and `Dispose` are no-ops.
- **Scenes belong to a context.** `LoadSceneAsync` opens the scene additively and the context owns it; each call is a
  new instance with its own `SceneHandle` (no sharing, no ref counting). `UnloadSceneAsync` closes one; a handle the
  context does not own is a no-op. `Unload` / `Dispose` unload every scene the context and its children loaded, not
  awaited. A context closed while a scene is loading unloads that scene again and the load throws
  `OperationCanceledException`. Scenes are not in the `RefStore`.
- **Creation through a factory.** `AssetsService` is `internal`; `AssetsFactory` (production) and `FakeAssets` (the
  fake) are the only creation points, and the installers own the registration and the lifetime.

## Not in this slice

Named only; nothing here is promised.

- Any type in `Data/` or `Config/` (load budgets, preload manifests).
- Instantiation, pooling and remote catalog updates.

The section between the markers is emitted by `discover --mode refresh` (unity-cli) from the module's callables
(`[BiomeCallable]` + `///`); never edit it by hand — `discover --mode check` reports drift.

<!-- biome:generated:start -->
<!-- biome:generated:end -->
