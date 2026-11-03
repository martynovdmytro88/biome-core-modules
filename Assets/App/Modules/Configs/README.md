# Configs

## Purpose

The single entry point to the project's configs: an environment names where each config type is read from, loaders
owned by the config's module turn the raw asset into the config, and a consumer reads it through `IConfigsReader` or
the `IConfig<T>` provider. Configs are immutable after load; there is no re-init within a session. Main thread only.

## Layers

Every namespace equals its asmdef `name` (`rootNamespace` equals `name` too).

- `Biome.Configs.Abstract` (`Abstract/`, refs UniTask, engine-free) — the contract: `IConfigsService` · `IConfigsReader` ·
  `IConfig<T>` · `IConfigLoader` · `IConfigsEnvironment` · `IConfigSourceReader` · `ConfigSource` · `ConfigSourceKind` ·
  `ConfigNotInitializedException` · `ConfigLoadException`. UniTask is a deliberate exposure in this contract.
- `Biome.Configs.Data` (`Data/`, refs none, engine-free) — module state.
- `Biome.Configs.Config` (`Config/`) — the environment config.
- `Biome.Configs` (`Runtime/`) — the service, the provider and the source reader.
- `Biome.Configs.Installers` (`Installers/`, not auto-referenced) — the VContainer installer.
- `Biome.Configs.Fakes` (`Fakes/`, Editor-only, not auto-referenced) — fakes of the contract.
- `Biome.Configs.Installers.Mock` (`InstallersMock/`, Editor-only, not auto-referenced) — the mock-profile installer.
- `Biome.Configs.Tests` (`Tests/`, EditMode) — the module's tests.

There is no `Events/` layer.

## Installer contract

`ConfigsInstaller` is a plain class: it takes the environment by reference in its constructor (null throws
`ArgumentNullException`). `Install` registers that instance as `IConfigsEnvironment` and hands the service no base
configs. The boot resolves `IConfigsService` and `IConfigsEnvironment` from the container and calls
`InitializeAsync`. No environment is loaded by name here.
