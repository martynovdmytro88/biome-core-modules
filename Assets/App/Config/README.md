# App/Config

The app-level assets.

- `AppFlow.asset` — the state flow of the `Game` profile: `InitState`, `EmergencyState`, and the transition between them. A
  host lists its own states here.
- `Configs/Resources/EnvironmentConfig.Production.asset`, `EnvironmentConfig.Test.asset` — the configs environments, empty
  until a host adds its config sources.
