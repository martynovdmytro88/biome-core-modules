# biome-core-modules

The Biome core Unity modules as UPM packages, one repository for all of them. A package holds a module's contract: the
abstractions, contract data, fakes, contract test suites and the skeleton templates of the host layers. The implementation,
DI installers and project-specific tests live in the host project. A package knows nothing about its host, DI or Unity scenes.

## Packages

| Package | Depends on |
|---|---|
| `com.biome.eventbus` | |
| `com.biome.localstore` | |
| `com.biome.assets` | UniTask |
| `com.biome.configs` | UniTask |
| `com.biome.gamestates` | `com.biome.eventbus`, `com.biome.assets`, UniTask |
| `com.biome.playerdata` | `com.biome.localstore`, `com.biome.eventbus` |

Each package's own `README.md` covers its purpose, layers and what a host must prove.

## Pinning from a host

In the host's `Packages/manifest.json`, pin a package on its tag, never on a branch:

```json
{
  "dependencies": {
    "com.biome.eventbus": "https://github.com/martynovdmytro88/biome-core-modules.git?path=/Packages/com.biome.eventbus#eventbus/v0.1.3",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11"
  },
  "testables": ["com.biome.eventbus"]
}
```

- **List every package yourself.** UPM does not resolve transitive git dependencies, so a host lists each `com.biome.*`
  package it needs along with their dependencies, UniTask included. Check versions against each `package.json`.
- **`testables`:** a package's test assemblies stay out of the Test Runner, and out of reach of the host's own test
  assemblies, until the package is listed there.
- **Credentials:** the repository is private, so the machine and CI need a git credential helper holding a GitHub token.
  Unity does not prompt for a login. Never put a token in the URL: the manifest is committed.

## Local development

To edit a package in place from a host, pin it to the local clone for the time being:

```json
"com.biome.eventbus": "file:../../../biome-core-modules/Packages/com.biome.eventbus"
```

The path is relative to the host's `Packages/` folder. Move the pin back to a tag before committing the host.

## Versions and tags

- Each package has its own SemVer version in `package.json` and its own tag: `<module>/vX.Y.Z`, for example `eventbus/v0.1.3`.
- A breaking contract change is a major version.
- A contract change means an edit in the package, an entry in its `CHANGELOG.md` and a new tag. Hosts then move their pins.

## Default utilities

`Assets/BiomeUtils/` is the source of the default utilities: the callable layer, `create_module`, `create_game_state` and their
templates. It is not a package: a host owns its copy under `Assets/BiomeUtils/` and may adapt it, so a later fix here reaches a
host only by copying again. The tag `utils/vX.Y.Z` marks the version a copy was taken from.

- **A host's copy:** the whole of `Commands/`, `Templates/`, `Tests/` with the first module; `GameStates/` with `com.biome.gamestates`.
- **The one host file:** `biome-utils.json`, filled from `biome-utils.json.txt` with the host's `{project-name}` and `{asmdef-root}`.
- **Dependencies:** `com.unity.pipeline` and `com.unity.inputsystem` in the host's manifest. The pipeline package does not
  declare the second one, yet needs it when the project's active input handling includes the Input System.

## Authoring project

The repository root is also a Unity project (`6000.5.4f1`). Open it to compile the packages and the utilities and run their
suites in the Test Runner without a host. The packages are embedded here, so `Packages/manifest.json` has no `com.biome.*`
entries and no `testables`. Its host layers live under `Assets/App/Modules/`.

## App template

`Assets/App/` is this project's own composition root and the neutral template a host copies. It is not a package: a host owns
its copy, and a later change here reaches a host only by copying again. The tag `app/vX.Y.Z` marks the version a copy was
taken from. `Assets/App/README.md` lists what is copied as is, where a host writes and what the scaffolder substitutes.
