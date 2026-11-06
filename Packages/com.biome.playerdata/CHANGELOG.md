# CHANGELOG — Decision & Version Log

> Append-only, via the `changelog` command (the sole writer). Active log = the last 5 entries; older
> entries live in `CHANGELOG/` range files (~20 KB each). Level-specific wording: `CONFIGS/changelog-preamble.md`.

current-version: v0.4

## Archive index

| Range | File |
|-------|------|
| _(none yet)_ | |

## History (active — last 5)

| Version | Date | What changed |
|---------|------|--------------|
| v0.1 | 2026-11-03 19:00 +0200 | **Initial cut.** Package 0.1.0: Abstract, Data and Fakes cut from the Marauders Unity project as an embedded package; assembly names and namespaces unchanged. |
| v0.2 | 2026-11-03 21:58 +0200 | **Empty archive folder tracked.** Package 0.1.1: CHANGELOG/.gitkeep added so a clean git checkout has the folder its .meta declares; no contract or code change. |
| v0.3 | 2026-11-03 23:39 +0200 | **Host skeleton templates completed.** Package 0.1.2: Templates~ completed for create_module host-only mode: Runtime, Installers, InstallersMock, Tests/Host, Events and Editor skeletons under the real host class names; dependencies on 0.1.2; no contract or code change. |
| v0.4 | 2026-11-06 18:41 +0200 | **Host test templates follow the first host.** Package 0.1.3: the Tests/Host asmdef template references the Data, Events, Fakes and installer layers, LocalStore, EventBus and VContainer the host fixtures need; dependencies on 0.1.3; no contract or code change. |
