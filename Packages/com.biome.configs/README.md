# com.biome.configs

## Purpose

The single entry point to the project's configs: an environment names where each config type is read from, loaders
owned by the config's module turn the raw asset into the config, and a consumer reads it through `IConfigsReader` or
the `IConfig<T>` provider. Configs are immutable after load; there is no re-init within a session. Main thread only.

This package carries the contract. It knows nothing about the host, DI or the Unity scene.

## Layers

Every namespace equals its asmdef `name` (`rootNamespace` equals `name` too).

- `Biome.Configs.Abstract` (`Abstract/`, refs UniTask, engine-free) — the contract: `IConfigsService` · `IConfigsReader` ·
  `IConfig<T>` · `IConfigLoader` · `IConfigsEnvironment` · `IConfigsEnvironmentGuard` · `IConfigSourceReader` ·
  `ConfigSource` · `ConfigSourceKind` · `ConfigNotInitializedException` · `ConfigLoadException`. UniTask is a deliberate
  exposure in this contract.
- `Biome.Configs.Data` (`Data/`, refs none, engine-free) — module state.
- `Biome.Configs.Fakes` (`Fakes/`, Editor-only, not auto-referenced) — fakes of the contract.

The host owns `Runtime` (the service, the provider and the source reader), `Config` (the environment config),
`Installers` and `InstallersMock`. `Templates~/` holds empty skeletons of the host layers.

## Tests

`Tests/Editor` (`Biome.Configs.Editor.Tests`) holds four abstract suites: `ConfigsServiceContractTests`,
`ConfigsEnvironmentContractTests`, `ConfigProviderContractTests` and `ConfigSourceReaderContractTests<TAsset>`. The first
three run here on the package's fakes; the reader suite runs on a test-local double, because the package ships no
reader. `Tests/Editor/Consumer` proves that a consumer needs only `Abstract` and `Fakes`.

A host must prove its own implementation: reference `Biome.Configs.Editor.Tests` from its test assembly, derive from the
suites and override `CreateService`, `CreateEnvironment`, `CreateProvider` and `CreateReader` (with the reader's sources
and asset check).

Requires `com.unity.test-framework` in the project (deliberately not a package dependency). A package outside
`Packages/` also needs an entry in `testables`.

## Decisions kept in code

- **One source per config type.** An environment that can hold a duplicate exposes `IConfigsEnvironmentGuard`; the
  service refuses to load when the guard reports one.
- **No re-init.** A second initialization within a session throws.
