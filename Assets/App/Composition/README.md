# App/Composition

The profiles of the composition root.

- `AppProfileScope` — the abstract scope: a profile's composition. It serializes `_environment` and `_flow` side by side
  (the environment config and a state flow asset). It receives its `AppProfile` from the root before it builds
  (`Profile`); a scope built without one, or without an environment or a flow, fails loudly naming the prefab.
  `Configure` order: a copy of the flow, `Validate()`, register the copy, the common installers (the configs installer is
  built in code from the environment), then `AppStatesInstaller` over the copy. No parent scope.
- `GameScope` — the game variant: `EnvironmentConfig.Production` and `AppFlow` on `Profiles/Game.prefab`. A variant
  differs by its prefab values and by `DeclareStates` (adds states the app catalogue does not know).
- `AppProfile` — a profile as data: only the scope prefab. `Validate()` fails loudly, naming the profile, on a missing scope.
- `Profiles/Resources/Game.asset` — the game `AppProfile`. The profile sits in `Resources/` (the root loads it by
  file name); the scope prefab stays outside it. The test profile `Test` sits in `App/Tests/TestFlow/Editor/Resources/`
  with its prefab and `TestFlow.asset` outside any `Resources/`, so a player build never carries it.
