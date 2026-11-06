# Assets

## Purpose

Loads assets by key or label over Addressables and keeps them in memory for as long as a **context** holds them. A
context is a lifetime scope (global · game state · window); an asset leaves memory when no context holds it, and a key
held by several contexts is loaded once. A leaf service: it publishes no events, persists nothing and references no
other module. Main thread only.

## Package and host

The contract and the engine-free layers come from the package `com.biome.assets` 0.1.3; its README describes them. This
folder holds the host layers. Every namespace equals its asmdef `name` (`rootNamespace` equals `name` too).

From the package:

- `Biome.Assets.Abstract` (`Abstract/`, refs UniTask only, engine-free) — the contract: `IAssetsService`
  (`GlobalContext` · `CreateContext`) · `IAssetsContext` (`CreateChild` · `LoadAsync` by key and by key list ·
  `PreloadByLabelAsync` · `LoadSceneAsync` · `UnloadSceneAsync` · `Release` · `Unload`) · the value types `AssetKey` and
  `AssetLabel` (plain strings, ordinal) and `SceneHandle` (key plus instance id, opaque, no engine type) · the seam
  `IAssetBackend` (load-by-key · resolve-label · load-scene, no ref counting).
- `Biome.Assets.Data` (`Data/`, engine-free) — reserved: the asmdef exists with no types.
- `Biome.Assets.Fakes` (`Fakes/`, Editor-only, not auto-referenced) — `FakeAssetBackend`, the fake of the seam.
- `Biome.Assets.Editor.Tests` (`Tests/Editor/`) — the value-type tests and the seam suite on the fake;
  `Biome.Assets.Tests` (`Tests/Runtime/`) — the contract suites of the service (`AssetsContractTests`) and of the seam
  (`AssetBackendContractTests`), written once.

Here:

- `Biome.Assets.Config` (`Config/`, refs Data) — reserved: the asmdef exists with no types.
- `Biome.Assets` (`Runtime/`) — `AssetsFactory.CreateAddressables()` (the public entry) · `Contexts/` (`AssetsService`,
  `AssetsContext`, internal) · `RefStore/` (one entry per key with its holding contexts and one shared in-flight load) ·
  `Backends/` (`AddressablesBackend`, internal, the only Addressables code).
- `Biome.Assets.Installers` (`Installers/`, not auto-referenced) — `AssetsInstaller` (VContainer `IInstaller`): one
  `IAssetsService` per scope, disposed with it. The module's only VContainer reference in shipping code.
- `Biome.Assets.Installers.Mock` (`InstallersMock/`, Editor-only, not auto-referenced) — `FakeAssets.CreateService(backend)`:
  the real store and contexts over a `FakeAssetBackend`, a behavioural stand-in; `MockAssetsInstaller`: the same
  contract over a caller-kept `FakeAssetBackend` (seed assets and scenes, read counters).
- Tests:
  - `Biome.Assets.Tests.Host` (`Tests/Host/`, EditMode) — `FakeBackendAssetsContractTests`: the service suite on the
    production store and contexts over the fake backend; `FakeBackendBehaviorTests`: what only the fake's counters show
    (reference counting across contexts, the shared in-flight load and its cancellation); `CanonicalEntryTypeTests`: a
    key loads once by the asset's own type and each caller's type is a view of it.
  - `Biome.Assets.Tests.Shared` (`Tests/Shared/`, not Editor-only) — `CanonicalProbe` and `ICanonicalProbe`, the
    component and interface the canonical-type tests put on their prefab; Unity adds no component from an Editor-only
    assembly.
  - `Biome.Assets.Tests.PlayMode` (`Tests/PlayMode/`) — both suites on Addressables, in Use Asset Database mode:
    `AddressablesAssetsContractTests` (the service from `AssetsFactory`) and `AddressablesBackendContractTests` (the
    backend alone).
  - `Biome.Assets.Tests.Editor` (`Tests/Editor/`, Editor-only) — `AddressablesTestContent`, the PlayMode prebuild and
    cleanup: it puts the smoke content of `Tests/Content/` in a temporary Addressables group (`assets-tests/a|b` under
    the label `assets-tests`, `assets-tests/scene`), selects Use Asset Database, and removes all of it after the run.
    It finds the content by GUID and creates the Addressables settings when the project has none, deleting them again
    afterwards.
  - `Tests/Content/` — `smoke-a.txt`, `smoke-b.txt`, `smoke-scene.unity` (empty).

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
