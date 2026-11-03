# GameStates

## Purpose

The state flow of the application: base states switched by a queued state machine, sub-states switched by a service owned by
their base state, pluggable transitions, scenario handlers for failures, and two bus events (switched, error). It never holds
a state's own logic or content; states live in the app, not here.

## Layers

- `Biome.GameStates.Abstract` (`Abstract/`, engine-free) - the contract: one set of interfaces for base states and sub-states
  (`IGameState`, `IGameStateMachine`, `ISubStateService`, `IStateTransition`, `IStateScenarioHandler`, `IStateInstaller`) and
  `StateFailure`.
- `Biome.GameStates.Data` (`Data/`, engine-behaviour-free) - `StateLevel`, `StatePhase`.
- `Biome.GameStates.Events` (`Events/`) - `StateSwitchedEvent`, `StateErrorEvent`: readonly struct payloads for the event bus.
- `Biome.GameStates.Config` (`Config/`) - the module's settings (skeleton).
- `Biome.GameStates` (`Runtime/`) - `GameStateMachine` (queue, phases, error routing, tagged logging) and the DI-free seam the
  installers implement: `IStateScopeFactory` / `IStateScope`, `IBaseStateDescriptorProvider`, `IStateLogSink`.
- `Biome.GameStates.Installers` (`Installers/`, not auto-referenced) - `GameStatesInstaller` (machine as `IGameStateMachine`, per-state scopes over
  VContainer: Init scope under the app resolver, one child scope per base state) and `StateInstallerBase` (the per-state load/unload order).
- `Biome.GameStates.Fakes` (`Fakes/`, Editor-only) - `FakeGameState`, `RecordingStateTransition`.
- `Biome.GameStates.Installers.Mock` (`InstallersMock/`, Editor-only) - `GameStatesInstallerMock` registers a caller-supplied fake `IGameStateMachine`.
- `Biome.GameStates.Tests` (`Tests/`, EditMode) - abstraction shape, machine, sub-state, error-routing, installer and installer-order tests.

## State generator

The `create_game_state` callable is not part of this module. It lives under the utilities root (`Assets/BiomeUtils/GameStates/`,
see `Assets/BiomeUtils/README.md`) and lays a state skeleton, `<StateId>State` and `<StateId>StateInstaller`, into
`Assets/App/States/<StateId>/`. The module does not reference it and it does not reference the module.

- Editor menu: `Biome/GameStates/Create State…`. unity-cli: `unity command create_game_state --state_id <StateId>`.

## Decisions kept in code

- `Abstract` references `Data` (and UniTask) because `StateFailure` carries `StateLevel` and `StatePhase`.
- Events are payloads published through `IEventPublisher`, not `IEvent` implementations; `Events` references only `Data`.

## Not in this slice

- Concrete state installers and the flow config: still to come (`Config/` is a skeleton).
