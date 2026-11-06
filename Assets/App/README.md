# App

The neutral composition root of a Biome host project, and the working App of this authoring project. It is not a package:
a host receives a copy when it is scaffolded and owns it from then on. There is no `com.biome.app`.

Before the first scene loads, the bootstrap loads `Resources/AppCompositionRoot.asset`, picks a profile and raises it as
VContainer's root scope: one instance in DontDestroyOnLoad, the parent of every scene `LifetimeScope` with no setup, disposed
when it is destroyed (quit / leaving play mode). The `VContainerSettings` it needs is created in code, so the project
carries no settings asset.

- `Bootstrap/` — `AppCompositionRoot` (SO): `_profileId` (the `AppProfile` file name in `Resources/`), its only serialized
  field; `Active` (the live root). The mechanism names no module type; the configs init is the first act of `InitState`.
- `Composition/` — `AppProfileScope` (a profile = module installers, explicit and in order, over one environment and one
  state flow) · `GameScope` · `Profiles/Game.prefab`.
- `States/` — the state catalog, the flow starter, `Init` with its scenario handlers, and `Emergency`.
- `Config/` — `AppFlow.asset` and the configs environments.
- `Modules/` — the host layers of the six core modules, under `modulesFolder` of `Assets/BiomeUtils/biome-utils.json`.
- `Editor/` — `PlayModeStartSceneHook`: editor play mode starts from `Assets/Boot/Boot.unity` (build index 0); a PlayMode
  test run keeps the runner's own scene.
- `Tests/` (`Biome.App.Tests`, PlayMode) — the root over the real start path; `Tests/TestFlow/` is the `Test` profile.

## Copied as is

These work in a host unchanged, once the scaffolder's substitutions are applied:

- `Bootstrap/`, `Composition/AppProfile.cs`, `Composition/Profiles/`, `Resources/`, `Editor/`, `AssemblyInfo.cs`.
- `States/AppStatesInstaller.cs`, `States/ConfigFlowDescriptorProvider.cs`, `States/StateFlowStarter.cs`, `States/Scenario/`,
  `States/Init/InitStateInstaller.cs`, the scenario handlers, `States/Emergency/`.
- `Config/`, `Tests/`, `Assets/Boot/Boot.unity`, `Assets/Builder/`, the repository root's `.editorconfig`.

Build Settings must list `Assets/Boot/Boot.unity` at index 0: the flow starts only from that scene.

## A host writes here

- `States/Init/InitState.cs` — the marked line in `EnterAsync`: request the first project state.
- `States/AppStateCatalog.cs` — one entry in `s_app` per project state, created with `create_game_state`.
- `Config/AppFlow.asset` — the project states and transitions. A state's scene is optional; a blank scene loads none.
- `Composition/AppProfileScope.cs` — `Installers()`: the module list, and the types the LocalStore registry holds.
- `States/Emergency/` — what the failure state shows.

## Not updated from the template

A later change here reaches a host only if someone copies it by hand, and nothing reports the difference. The host's copy
is not synchronised back either. The tag `app/vX.Y.Z` marks the version a copy was taken from.

## What the scaffolder substitutes

The files hold real values so that this project compiles; a placeholder is the value's name, not a token in the code.
The table of what the scaffolder substitutes in a host's copy is the scaffolder's: `biome-projects`
`TOOLS/SCAFFOLDER/scaffolder.md` §6 Unity repo.

## Dependencies

`jp.hadashikick.vcontainer`, `com.unity.addressables`, UniTask and every `com.biome.*` package in the host's manifest.

## `Modules/`

Real implementations, copied from the first host. `create_module --host_only` lays skeletons with empty classes, and this App
does not compile over them: it needs `GameStatesInstaller.AddState`, `StateInstallerBase`, the installers' constructors and the
modules' host `Config` layers. A new host needs these layers from somewhere, and the choice between copying them and laying
skeletons is the scaffolder's.
