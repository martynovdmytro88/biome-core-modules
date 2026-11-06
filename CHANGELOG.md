# CHANGELOG — Decision & Version Log

> Append-only, via the `changelog` command (the sole writer). Active log = the last 5 entries; older
> entries live in `CHANGELOG/` range files (~20 KB each). Level-specific wording: `CONFIGS/changelog-preamble.md`.

current-version: v0.5

## Archive index

| Range | File |
|-------|------|
| _(none yet)_ | |

## History (active — last 5)

| Version | Date | What changed |
|---------|------|--------------|
| v0.1 | 2026-11-03 21:28 +0200 | **Monorepo laid.** Six packages 0.1.0 received from marauders-unity 4e79de0: eventbus, localstore, assets, configs, gamestates, playerdata; empty authoring Unity project on 6000.5.4f1. |
| v0.2 | 2026-11-03 23:39 +0200 | **Host skeleton templates in the six packages.** Packages 0.1.2: Templates~ completed for create_module host-only mode under the real host class names; the state templates moved from com.biome.gamestates to the host-side generator. |
| v0.3 | 2026-11-03 23:54 +0200 | **Default utilities source.** Assets/BiomeUtils received from the host as the source of the default utilities, copied into a host rather than pinned as a package: callable layer, create_module, create_game_state, templates, tests; biome-utils.json.txt as the host config form; com.unity.pipeline added to the authoring project. |
| v0.4 | 2026-11-04 00:59 +0200 | **Neutral App template.** Assets/App (composition root, profile, state catalog, Init and Emergency, tests), Assets/Boot and Assets/Builder; the six host layers under Assets/App/Modules, copied from the first host; a state's scene is optional; VContainer and Addressables in the authoring manifest. |
| v0.5 | 2026-11-06 14:48 +0200 | **Host tests in the App copy of the modules.** Assets/App/Modules synced from marauders-unity: Tests/Host fixtures for EventBus, GameStates (with Runtime/AssemblyInfo.cs) and PlayerData that bind the package suites to production; the Assets PlayMode fixtures on Addressables with a temporary group prebuild and cleanup, its Tests/Editor step and Tests/Content; host module READMEs to the package-plus-host layout; README pin example on v0.1.2. |
