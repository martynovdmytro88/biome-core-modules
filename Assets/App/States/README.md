# App/States

The base states of the application and their installers, wired into the profile's scope by `AppStatesInstaller`, which takes the one flow the scope runs.
One folder per state (`Init`, `Emergency`): the state and its installer. A host adds its own with `create_game_state`, and one entry per state in `AppStateCatalog`.

- `Init` is entered once on Boot, loads no scene, and its scope lives for the whole application; it holds the scenario handler of the machine. Its first act is the configs init: it takes the configs service and the profile's environment and awaits the service's environment init, then loads the configs when they are not loaded, so nothing proceeds before both complete. A failure of either takes the machine's error route (log, the Init scope's scenario handler, the error event): in the app flow the handler (`EmergencyScenarioHandler`) queues a switch to `Emergency`; the test flow keeps the no-op `DefaultStateScenarioHandler`, so the failure is only logged and published. It requests no next state: a host writes that request at the marked line.
- `Emergency` is a shell with no scene. A state's scene is optional: a blank scene in the flow loads none.
- `StateFlowStarter` has no configs dependency; when an entry point calls `RunAsync` it switches the machine to the first state the flow names (`AppFlow` names `InitState`). The scope does not start it on build, the boot does when the game starts from the Boot scene.
- `ConfigFlowDescriptorProvider` gives the scene keys and the first state from the scope's flow copy.
- `AppStateCatalog` maps each base state type to its declaration; `AppStatesInstaller` declares exactly the states the flow lists and throws, naming flow and state, when a listed state has no entry. A scope variant adds its own states with `AddCatalogEntry`.

States reference only the abstract, event and config layers of `Biome.GameStates`; the composition files here also reference its installers and runtime entry points.

- `Scenario/` holds `StateCompletedEvent` and `IFlowScenario`; the app registers no implementor. The test flow's state lives in `App/Tests/TestFlow/`, never here. Its Init state registers the in-memory local store, the in-memory player data and the fake configs service in its own scope (`TestInitStateInstaller.RegisterFakes`, called from `TestScope`), so a `Test` boot creates none of the real ones and writes no PlayerPrefs; the bus, the assets and the machine stay the root's.
