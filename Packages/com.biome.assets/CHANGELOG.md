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
| v0.1 | 2026-11-03 18:57 +0200 | **Initial cut.** Package 0.1.0: Abstract, Data and the backend fake cut from the Marauders Unity project as an embedded package; the backend seam is now public in Abstract. |
| v0.2 | 2026-11-03 21:58 +0200 | **Empty archive folder tracked.** Package 0.1.1: CHANGELOG/.gitkeep added so a clean git checkout has the folder its .meta declares; no contract or code change. |
| v0.3 | 2026-11-03 23:39 +0200 | **Host skeleton templates completed.** Package 0.1.2: Templates~ completed for create_module host-only mode: Runtime, Installers, InstallersMock and Tests/Host skeletons under the real host class names; no contract or code change. |
| v0.4 | 2026-11-06 18:41 +0200 | **Addressables test templates.** Package 0.1.3: Templates~/Tests gains PlayMode (both suites over the smoke keys, red until bound), Editor (the temporary-group prebuild and cleanup) and Content (smoke assets with fixed GUIDs), so create_module lays the Addressables fixture; README names it; no contract or code change. |
