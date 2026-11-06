# CHANGELOG — Decision & Version Log

> Append-only, via the `changelog` command (the sole writer). Active log = the last 5 entries; older
> entries live in `CHANGELOG/` range files (~20 KB each). Level-specific wording: `CONFIGS/changelog-preamble.md`.

current-version: v0.7

## Archive index

| Range | File |
|-------|------|
| v0.1 – v0.2 | `CHANGELOG/v0.1-v0.2.md` |

## History (active — last 5)

| Version | Date | What changed |
|---------|------|--------------|
| v0.3 | 2026-11-03 23:54 +0200 | **Default utilities source.** Assets/BiomeUtils received from the host as the source of the default utilities, copied into a host rather than pinned as a package: callable layer, create_module, create_game_state, templates, tests; biome-utils.json.txt as the host config form; com.unity.pipeline added to the authoring project. |
| v0.4 | 2026-11-04 00:59 +0200 | **Neutral App template.** Assets/App (composition root, profile, state catalog, Init and Emergency, tests), Assets/Boot and Assets/Builder; the six host layers under Assets/App/Modules, copied from the first host; a state's scene is optional; VContainer and Addressables in the authoring manifest. |
| v0.5 | 2026-11-06 14:48 +0200 | **Host tests in the App copy of the modules.** Assets/App/Modules synced from marauders-unity: Tests/Host fixtures for EventBus, GameStates (with Runtime/AssemblyInfo.cs) and PlayerData that bind the package suites to production; the Assets PlayMode fixtures on Addressables with a temporary group prebuild and cleanup, its Tests/Editor step and Tests/Content; host module READMEs to the package-plus-host layout; README pin example on v0.1.2. |
| v0.6 | 2026-11-06 18:41 +0200 | **Packages 0.1.3: host test templates; App copy with the C2 rest.** Packages 0.1.3: Tests/Host asmdef templates of EventBus, PlayerData and GameStates reference what the host fixtures need; Assets Templates~/Tests lays the Addressables PlayMode, Editor and Content fixture. Assets/App/Modules synced from marauders-unity: Configs host fixtures (environment, Resources reader, provider, installers) and Assets FakeBackendBehavior and CanonicalEntryType with the Tests/Shared probes. App: DefaultStateScenarioHandler sends a failed Init to Emergency and is the one Init handler (EmergencyScenarioHandler removed); the test flow lists Emergency. BiomeUtils Skeleton_Compiles builds the probe as an editor assembly on engine modules. README pin example on v0.1.3. |
| v0.7 | 2026-11-06 19:42 +0200 | **App README: the scaffolder's substitution table moved to its source.** Assets/App/README.md §What the scaffolder substitutes now points at biome-projects TOOLS/SCAFFOLDER/scaffolder.md §6 Unity repo, which holds the table; §Copied as is follows. Text only, no code change. |
