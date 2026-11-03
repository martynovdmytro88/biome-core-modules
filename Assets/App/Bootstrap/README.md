# App/Bootstrap

The composition root: selection only.

- `AppCompositionRoot` — the `Resources/` asset. Its serialized state is the profile id only (`_profileId`, the `AppProfile` file name, now `Game`). `SelectProfile()` loads that `AppProfile` from `Resources/` by name and throws, naming the id, when it is blank or unknown; there is no environment name. In the Editor only, a one-shot `SessionState` key (`Biome.App.ProfileIdOverride`, set by C# run through unity-cli) replaces the id for the launch that reads it and is erased by that read; an unknown override id throws like any other, and a player build ignores the key.
  `Raise(AppProfile)` validates the profile and raises its scope as VContainer's root scope, handing the scope the
  profile before the container builds. `Shutdown()` destroys it.
- The environment and the flow come from the profile, never from the root. The root runs no config init: the
  Init state does, with the environment the profile registered (`States/Init`); the root only starts the state flow
  when the game booted from the Boot scene (build index 0, `Assets/Boot/Boot.unity`).
