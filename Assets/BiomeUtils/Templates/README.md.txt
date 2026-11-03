# {Module}

## Purpose

What the module is for and what it never does — written by hand.

## Layers

- `{AsmdefRoot}.{Module}.Abstract` (`Abstract/`, engine-free) — the contract consumers bind to: `I{ModuleId}Service`.
- `{AsmdefRoot}.{Module}.Config` (`Config/`) — the module's settings.
- `{AsmdefRoot}.{Module}.Data` (`Data/`, engine-behaviour-free) — the module's data types.
- `{AsmdefRoot}.{Module}` (`Runtime/`) — the internal implementation behind `{ModuleId}Factory.Create()`.
- `{AsmdefRoot}.{Module}.Installers` (`Installers/`, not auto-referenced) — `{ModuleId}Installer` (VContainer
  `IInstaller`): one `I{ModuleId}Service` per scope.
- `{AsmdefRoot}.{Module}.Fakes` (`Fakes/`, Editor-only, not auto-referenced) — `Fake{ModuleId}Service`, the shipped
  fake.
- `{AsmdefRoot}.{Module}.Installers.Mock` (`InstallersMock/`, Editor-only, not auto-referenced) —
  `Mock{ModuleId}Installer`, the mock-profile twin of the installer.
- `{AsmdefRoot}.{Module}.Tests` (`Tests/`, EditMode) — `{ModuleId}ContractTests`, run on the production
  implementation and on the fake.

## Decisions kept in code

- None yet — record each decision a reader would otherwise have to rediscover from the code.

## Not in this slice

- Everything beyond the empty contract laid by `create_module`.

The section between the markers is emitted by `discover --mode refresh` (unity-cli) from the module's callables
(`[BiomeCallable]` + `///`); never edit it by hand — `discover --mode check` reports drift.

<!-- biome:generated:start -->
<!-- biome:generated:end -->
