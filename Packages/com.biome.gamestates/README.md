# com.biome.gamestates

## Purpose

The state flow of the application: base states switched by a queued state machine, sub-states switched by a service owned by
their base state, pluggable transitions and scenario handlers for failures. It never holds a state's own logic or content;
states live in the app, not here.

This package carries the contract. It knows nothing about the host, DI or the Unity scene.

## Layers

- `Biome.GameStates.Abstract` (`Abstract/`, engine-free) - the contract: one set of interfaces for base states and sub-states
  (`IGameState`, `IGameStateMachine`, `ISubStateService`, `IStateTransition`, `IStateScenarioHandler`, `IStateInstaller`) and
  `StateFailure`.
- `Biome.GameStates.Data` (`Data/`, engine-behaviour-free) - `StateLevel`, `StatePhase`.
- `Biome.GameStates.Fakes` (`Fakes/`, Editor-only) - `FakeGameState`, `MockGameState`, `MockGameStateMachine`,
  `RecordingStateTransition`.

The host owns `Runtime`, `Config`, `Events`, `Installers` and `InstallersMock`. `Templates~/` holds empty skeletons of the
host layers and the state templates `State.cs.txt` and `StateInstaller.cs.txt`, read by the `create_game_state` generator.

## Tests

`Tests/Editor` (`Biome.GameStates.Editor.Tests`) holds two abstract suites. `GameStateMachineContractTests` runs here on
`MockGameStateMachine`; `SubStateServiceContractTests` runs on a test-local double, because the package ships no fake of
the service. **The mock only records requests, so both suites cover the current state and the request order only;
queueing, cancellation, scopes, transitions and failure routing are proven only by a host's own tests.**
`Tests/Editor/Consumer` proves that a consumer needs only `Abstract` and `Fakes`.

A host must prove its own implementation: reference `Biome.GameStates.Editor.Tests` and `Biome.GameStates.Fakes` from its
test assembly, derive from the suites and override `CreateMachine` and `CreateService`. The suites switch A, then B, then
C, so a machine that needs an Init state first maps `TestStateA` to it.

Requires `com.unity.test-framework` in the project (deliberately not a package dependency). A package outside
`Packages/` also needs an entry in `testables`.

## Decisions kept in code

- `Abstract` references `Data` (and UniTask) because `StateFailure` carries `StateLevel` and `StatePhase`.
