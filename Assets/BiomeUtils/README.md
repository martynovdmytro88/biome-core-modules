# BiomeUtils — utilities root (not a module)

Editor-only utilities that serve modules: the module-creation tool (`create_module`) and the two generic doors
(`invoke`, `discover`) exposed through unity-cli. Namespace / asmdef `Biome.Utils.Commands` (`Commands/`, editor-only,
not auto-referenced); tests in `Tests/` (`Biome.Utils.Commands.Tests`, EditMode); base skeleton templates in
`Templates/`. Not a module: no passport, no `/module`, no `CODE-KNOWLEDGE/` entry — the vault describes it through the
emitted catalogue (`unity-catalogue` command) and one line in `PROJECT-CANON/unity-side-local.md`.

This human part is hand-written; the section between the markers is emitted by `discover --mode refresh` and must
never be edited by hand (`discover --mode check` reports drift).

## Config — `biome-utils.json`

```json
{
  "modulesFolder": "<project-name>/Modules",
  "asmdefRoot": "<asmdef-root>",
  "appNamespace": "<asmdef-root>.App",
  "statesRoot": "<project-name>/App/States"
}
```

Read once per domain reload (`BiomeUtilsConfig.Current`). `modulesFolder` and `statesRoot` are relative to `Assets/`;
`asmdefRoot` is the one asmdef / namespace root (`{asmdefRoot}.<Module>[.<Part>]`); `appNamespace` is the namespace of a
generated game state. `create_game_state` needs `appNamespace` and `statesRoot` and fails naming them when they are
absent; the other tools need only the first two. Agreement of `modulesFolder` and `asmdefRoot` with the vault's
`CONFIGS/project-identity.md` (`project-name` → `<project-name>/Modules`, `asmdef-root`) is a `discover --mode check`
item (`identity_path`, default `../CONFIGS/project-identity.md` relative to the Unity project root), never a rule to
remember.

## The callable convention (set here by example)

Every callable is a **static method** carrying, top to bottom:

```csharp
/// <summary>What it does — the one documentation source (missing = no-doc warning).</summary>
/// <param name="name">…</param>
[BiomeCallable(CallableKind.Generate)]                       // kind only: generate · test · mock · cheat · util
[CliCommand("create_module", Description, Tags = new[] { "biome/generate" })]   // the unity-cli surface (optional)
public static BiomeEnvelope CreateModule([CliArg("name", "…", Required = true)] string name) { … }
```

- **Where it runs** is the asmdef the method sits in — never a field of the attribute.
- `[MenuItem]` wrappers (menu `Biome/Utils/…`) call the same static method — convenience for hands, never the contract.
- `[CliCommand]` is optional: a `[BiomeCallable]` without it is reachable through `invoke` only (listed in the README
  and the catalogue as *invoke only*).

## The envelope (every result)

```json
{ "code": "ok | fail | exception", "message": "…", "channel": "<callable>", "warnings": [], "payload": … }
```

`BiomeEnvelope` derives from the pipeline's `BaseResponse`, so `fail` / `exception` also set `error` (+ `errorDetails`
for exceptions). It travels inside the pipeline's own envelope: `unity command … --format json` →
`data.result` is the envelope; `payload` is the method's C# return value serialized by Newtonsoft.

## Exit codes and signals — as verified live (unity-cli 1.0.0-beta.6 · com.unity.pipeline 0.6.0-exp.1 · 2026-09-15)

| Situation | CLI exit | Where the signal actually is |
|---|---|---|
| callable returns `code: fail` (e.g. `discover --mode check` drift) | **0** | `data.result.code` — the server answers 200 for any returned value; a returned `BaseResponse.error` is only telemetry |
| callable returns `code: exception` (caught inside, wrapped) | **0** | `data.result.code` |
| parameter cannot be converted (e.g. `--tags solo` for `string[]`) | **6** | `errors[].message` = `400 Parameter Validation Failed` |
| `run_tests --mode editor` — tests ran and **failed** (live Editor) | **0** | `data.result.Summary.Failed > 0` (`success: true`) — NOT 6 (the Unity repo's `CLAUDE.md`) and NOT 8 (intent note §7); both codes describe headless `unity test`, not the live path |
| `run_tests` green | 0 | `Summary.Failed == 0` |
| no reachable Editor | non-zero (`COMMAND_FAILED`) | `errors[]` |

**GATE-1 deterministic half therefore reads JSON, not exit codes:** `Summary.Failed == 0` for tests and
`data.result.code == "ok"` for `discover --mode check`.

## Arrays and structured parameters — proven at build (intent note §7 open item)

- `string[]` parameters work over `[CliCommand]` **as a JSON-valued flag**: `--tags '["a","b"]'` → `["a","b"]`.
  A bare token (`--tags solo`) is rejected with exit 6 (see above). `unity list` advertises the type as `string[]`.
- Structured DTOs (`IStructuredCommandInput`) work the same way: `--args '{"label":"L","count":2,"items":["i"]}'`
  (nested arrays included); advertised as the DTO type name.
- `invoke` maps a JSON object onto any signature (arrays, `List<int>`, DTOs, defaults) by parameter name — proven by
  the tool tests (`ScannerAndInvokeTests`) and by `unity command biome_probe_params` (a test-kind probe in `Tests/`).
- `ProbeUndocumented` in `Tests/` deliberately has no `///` — it is the fixture that keeps the `no-doc` warning path
  exercised; the one standing warning of `discover --mode check` on this folder is that fixture.

## Usage

```bash
unity command create_module --name Foo --format json
unity command create_module --name Foo --with_events true --format json
unity command create_module --host_only true --from_package com.biome.gamestates --with Events --format json
unity command discover --path Assets/<modulesFolder>/Foo --mode refresh --format json
unity command discover --path Assets/<modulesFolder>/Foo --mode check --format json
unity command invoke --path Assets/<modulesFolder>/Foo --function DoThing --params '{"x":1}' --format json
unity command run_tests --mode editor --filter Biome.Utils.Commands.Tests --filter_type assembly --format json
```

## The skeleton `create_module` lays

The module internal template (`ws-dev/CODE-KNOWLEDGE/ARCHITECTURE/Integration/project-files-structure.md` §Module
internal template), the layer set every built module has:

| Folder | asmdef | Laid file(s) |
|---|---|---|
| `Abstract/` | `{AsmdefRoot}.{Module}.Abstract` (engine-free) | `I{ModuleId}Service` — the empty contract |
| `Config/` | `{AsmdefRoot}.{Module}.Config` (→ Data) | — |
| `Data/` | `{AsmdefRoot}.{Module}.Data` (engine-behaviour-free; laid with `noEngineReferences: true` — the strict start, `false` is the module's decision when it needs an engine value type) | — |
| `Events/` *(only with `with_events`)* | `{AsmdefRoot}.{Module}.Events` (→ Data only; engine-behaviour-free, `noEngineReferences: false`) | — |
| `Installers/` | `{AsmdefRoot}.{Module}.Installers` (not auto-referenced; → Runtime · Abstract · Config · VContainer) | `{ModuleId}Installer` |
| `Runtime/` | `{AsmdefRoot}.{Module}` | internal `{ModuleId}Service` + public `{ModuleId}Factory` |
| `Fakes/` | `{AsmdefRoot}.{Module}.Fakes` (Editor-only, not auto-referenced) | `Fake{ModuleId}Service` |
| `InstallersMock/` | `{AsmdefRoot}.{Module}.Installers.Mock` (Editor-only, not auto-referenced) | `Mock{ModuleId}Installer` |
| `Tests/` | `{AsmdefRoot}.{Module}.Tests` (Editor-only, `UNITY_INCLUDE_TESTS`) | abstract `{ModuleId}ContractTests` + one fixture over production, one over the fake |
| module root | — | `README.md` (A13 sections + the marker frame) |

Never laid: `Components/` · `Editor/` · `Templates/` (config templates belong to the configs process). Every asmdef
sets `rootNamespace` == `name` (S6). `.meta` files come from the `AssetDatabase.Refresh()` after the write.

Generator placeholders (`Templates/*.txt`): `{Module}` (name as typed), `{ModuleId}` (identifier-safe: `.`/`-` → `_`),
`{AsmdefRoot}`, `{Namespace}` = `{AsmdefRoot}.` + the name with its dots kept and every other non-identifier character
→ `_` — equal to the asmdef name for every name made of identifier segments; for any other name the payload carries an
`S6` warning. Plain string replacement — asmdef JSON braces are safe.

## The host-only mode

`create_module --host_only true --from_package com.biome.<m> [--with Events,Editor]` is for a module whose contract lives
in an installed package. It resolves the package through the Package Manager and copies its `Templates~/<layer>/` as they
are (`<path>.txt` → `<path>`, no placeholders) into `Assets/<modulesFolder>/<Module>/`:

- always `Runtime/ Installers/ InstallersMock/ Tests/`; `Events/ Editor/` only on request (`with_events` = `--with Events`);
  a layer the package has no templates for is a `no-template` warning, an unknown name in `--with` a `fail`;
- never `Abstract/ Data/ Fakes/` and no module README;
- `<Module>` comes from `Templates~/Runtime/<Root>.<Module>.asmdef.txt`; `--name` overrides only the folder;
- an existing file is never overwritten and is listed in `skipped[]`; a re-run creates nothing.

The payload is the same `{ modulePath, created[], skipped[], warnings[] }`. `--output_root` replaces the modules home, for
tests and for a probe outside `Assets/`: a skeleton laid under `Assets/` beside the live module of the same package
duplicates its assemblies. The laid `Tests/Host` fixture throws `NotImplementedException` from its hooks: red until the host
binds the package's suite to its implementation.

<!-- biome:generated:start -->
<!-- emitted by discover --mode refresh; do not edit by hand -->
## Callables under `Assets/BiomeUtils` (8)

### `create_module` — generate
<!-- h: create_module 6521c26dbb3c -->
- **method:** `Biome.Utils.Commands.CreateModuleCommand.CreateModule` · **asmdef:** `Biome.Utils.Commands` · **cli:** `unity command create_module`
- **signature:** `BiomeEnvelope Biome.Utils.Commands.CreateModuleCommand.CreateModule(String name = null, Boolean with_events = False, Boolean host_only = False, String from_package = null, String with = null, String output_root = null)`
- **doc:** Lays the canonical module skeleton under the configured modules home (`biome-utils.json`): the layer asmdefs Abstract · Config · Data · Installers · Runtime · Fakes · InstallersMock · Tests (+ Events on request), the empty `I<Module>Service` contract, its internal production implementation behind `<Module>Factory`, the shipped fake, the prod and mock installers, the contract suite with one fixture over production and one over the fake, and the README (A13 sections + marker frame). Folder == name, rule `[A-Za-z0-9][A-Za-z0-9._-]*`. Idempotent — every artefact is reported as `created` or `skipped`. With `host_only` and `from_package` it lays only the host layers Runtime · Installers · InstallersMock · Tests (+ Events · Editor on request) from the package's `Templates~/`, copied as they are, with asmdef references on the package assemblies.
  - `name` — Module name; folder == name. Required unless host_only; there the default is the package's module.
  - `with_events` — Also lay the optional Events/ layer (payload types only, references Data).
  - `host_only` — Lay only the host layers from the templates of `from_package`.
  - `from_package` — host_only: the installed package whose `Templates~/` are laid, e.g. `com.biome.eventbus`.
  - `with` — host_only: optional layers, comma-separated, e.g. `Events,Editor`.
  - `output_root` — Modules home, project-relative; default `Assets/<modulesFolder>`. For tests.
- **returns:** Envelope with payload `{ modulePath, created[], skipped[] }`.

### `discover` — util
<!-- h: discover 9aa68c6bf9fd -->
- **method:** `Biome.Utils.Commands.DiscoverCommand.Discover` · **asmdef:** `Biome.Utils.Commands` · **cli:** `unity command discover`
- **signature:** `BiomeEnvelope Biome.Utils.Commands.DiscoverCommand.Discover(String path, String mode = check, String identity_path = ../CONFIGS/project-identity.md)`
- **doc:** Scans the callables under `path` (attribute + `///`). With `mode=refresh` the generated section of `<path>/README.md` (between the markers) is rewritten — byte-diff first, so an unchanged README is not touched; the human text outside the markers is never modified. With `mode=check` nothing is written: per-callable hash (signature + doc) is compared with the README, `no-doc` callables are listed, and the tool config is compared with the vault's project identity. Codes: clean → `ok`; warnings only → `ok` + `warnings[]`; drift → `fail`.
  - `path` — Project-relative folder to scan (its asmdefs scope the callables).
  - `mode` — `refresh` | `check`.
  - `identity_path` — check only: the vault identity file, relative to the Unity project root.

### `invoke` — util
<!-- h: invoke 0df4e319d4eb -->
- **method:** `Biome.Utils.Commands.InvokeCommand.Invoke` · **asmdef:** `Biome.Utils.Commands` · **cli:** `unity command invoke`
- **signature:** `BiomeEnvelope Biome.Utils.Commands.InvokeCommand.Invoke(String path, String function, String params = null)`
- **doc:** Resolves a callable under `path` (the asmdefs there name the assemblies) by method or `[CliCommand]` name, maps the JSON object in `params` onto the C# parameters (by name; missing optional parameters take their defaults) and returns the method's result in the envelope payload. A thrown exception becomes `code: exception`. A result that is itself an envelope is passed through unchanged.
  - `path` — Project-relative folder holding the asmdef(s), e.g. `Assets/<modulesFolder>/Foo`.
  - `function` — Method name or [CliCommand] name.
  - `params` — JSON object of named parameters, e.g. `{"name":"Foo"}`.

### `biome_probe_params` — test
<!-- h: biome_probe_params 428df9fca446 -->
- **method:** `Biome.Utils.Commands.Tests.ProbeCallables.ProbeParams` · **asmdef:** `Biome.Utils.Commands.Tests` · **cli:** `unity command biome_probe_params`
- **signature:** `BiomeEnvelope Biome.Utils.Commands.Tests.ProbeCallables.ProbeParams(String[] tags = null, ProbeArgs args = null)`
- **doc:** Echoes an array and a structured argument back, so the JSON→C# mapping of both can be read off the payload.
  - `tags` — A string array.
  - `args` — A structured argument.

### `ProbeSum` — test
<!-- h: ProbeSum 8e31c8c06853 -->
- **method:** `Biome.Utils.Commands.Tests.ProbeCallables.ProbeSum` · **asmdef:** `Biome.Utils.Commands.Tests`
- **signature:** `Int32 Biome.Utils.Commands.Tests.ProbeCallables.ProbeSum(List<Int32> values)`
- **doc:** Sums a list — plain return value wrapped by invoke.

### `ProbeThrows` — test
<!-- h: ProbeThrows ad4cf79ca7eb -->
- **method:** `Biome.Utils.Commands.Tests.ProbeCallables.ProbeThrows` · **asmdef:** `Biome.Utils.Commands.Tests`
- **signature:** `String Biome.Utils.Commands.Tests.ProbeCallables.ProbeThrows(String why)`
- **doc:** Throws on purpose — the envelope must report `exception`.

### `ProbeUndocumented` — test
<!-- h: ProbeUndocumented 55fb6a00d7b3 -->
- **method:** `Biome.Utils.Commands.Tests.ProbeCallables.ProbeUndocumented` · **asmdef:** `Biome.Utils.Commands.Tests`
- **signature:** `Int32 Biome.Utils.Commands.Tests.ProbeCallables.ProbeUndocumented(Int32 a, Int32 b = 2)`
- **doc:** _no-doc_ ⚠

### `create_game_state` — generate
<!-- h: create_game_state edf15c6199d0 -->
- **method:** `Biome.Utils.GameStates.GameStateGenerator.CreateGameState` · **asmdef:** `Biome.Utils.GameStates` · **cli:** `unity command create_game_state`
- **signature:** `BiomeEnvelope Biome.Utils.GameStates.GameStateGenerator.CreateGameState(String state_id, String output_root = null)`
- **doc:** Generates the state skeleton: `<StateId>State` (an `IGameState` shell) and `<StateId>StateInstaller` (a `StateInstallerBase` with no dependents) into `Assets/<statesRoot>/<StateId>/` in the `appNamespace` namespace (`biome-utils.json`). An existing folder is refused, nothing is overwritten. The state is not registered anywhere — a flow and a pack stay manual.
  - `state_id` — State id, PascalCase ([A-Z][A-Za-z0-9]*); the types are named after it.
  - `output_root` — Target folder (project-relative); default Assets/<statesRoot>/<StateId>. For tests.
- **returns:** Envelope with payload `{ folder, created[] }`.

<!-- biome:generated:end -->
