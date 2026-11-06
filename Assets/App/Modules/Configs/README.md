# Configs

## Purpose

The single entry point to the project's configs: an environment names where each config type is read from, loaders
owned by the config's module turn the raw asset into the config, and a consumer reads it through `IConfigsReader` or
the `IConfig<T>` provider. Configs are immutable after load; there is no re-init within a session. Main thread only.

## Package and host

The contract and the engine-free layers come from the package `com.biome.configs` 0.1.3; its README describes them. This
folder holds the host layers. Every namespace equals its asmdef `name` (`rootNamespace` equals `name` too).

From the package:

- `Biome.Configs.Abstract` (`Abstract/`, refs UniTask, engine-free) — the contract: `IConfigsService` · `IConfigsReader` ·
  `IConfig<T>` · `IConfigLoader` · `IConfigsEnvironment` · `IConfigsEnvironmentGuard` · `IConfigSourceReader` ·
  `ConfigSource` · `ConfigSourceKind` · `ConfigNotInitializedException` · `ConfigLoadException`. UniTask is a deliberate
  exposure in this contract.
- `Biome.Configs.Data` (`Data/`, refs none, engine-free) — module state (`ConfigStore`).
- `Biome.Configs.Fakes` (`Fakes/`, Editor-only, not auto-referenced) — fakes of the contract (`FakeConfigsService` ·
  `FakeConfigsEnvironment` · `FakeConfigLoader`).
- `Biome.Configs.Editor.Tests` (`Tests/Editor/`) — the contract suite with its fixture on the fake;
  `Biome.Configs.Editor.Tests.Consumer` (`Tests/Editor/Consumer/`) — the consumer fixture.

Here:

- `Biome.Configs.Config` (`Config/`) — the environment config (`EnvironmentConfig`).
- `Biome.Configs` (`Runtime/`) — the service (`ConfigsService`), the provider (`ConfigProvider`) and the source reader
  (`ResourcesConfigSource`).
- `Biome.Configs.Installers` (`Installers/`, not auto-referenced) — the VContainer installer (`ConfigsInstaller`).
- `Biome.Configs.Installers.Mock` (`InstallersMock/`, Editor-only, not auto-referenced) — the mock-profile installer
  (`ConfigsInstallerMock`) and `ConfigsTestEnvironment`.
- `Biome.Configs.Tests.Host` (`Tests/Host/`, EditMode) — the package's suites on production: the service
  (`ProductionConfigsServiceContractTests`), the environment (`EnvironmentConfigContractTests`), the Resources reader
  (`ResourcesConfigSourceContractTests`, over an asset it makes and removes) and the provider
  (`ProductionConfigProviderContractTests`); beside them the host's own cases: `EnvironmentConfigTests` (unresolvable
  names, the duplicate guard), `ConfigProviderServiceTests` (the provider over the real service), `ConfigsInstallerTests`
  and `ConfigsInstallerEnvironmentTests` (the installer against a real container, mock parity).

There is no `Events/` layer.

## Installer contract

`ConfigsInstaller` is a plain class: it takes the environment by reference in its constructor (null throws
`ArgumentNullException`). `Install` registers that instance as `IConfigsEnvironment` and hands the service no base
configs. The boot resolves `IConfigsService` and `IConfigsEnvironment` from the container and calls
`InitializeAsync`. No environment is loaded by name here.
