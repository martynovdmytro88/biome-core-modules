# Authoring project assets

This Unity project exists so the packages under `Packages/` and their test suites compile and run without a host.

- `BiomeUtils/` — the source of the default Biome utilities, compiled and tested here. A host receives a copy, see the root
  `README.md`. `biome-utils.json` is this project's working config; `biome-utils.json.txt` is the form a host's config is
  filled from.

- `App/` — the neutral App: this project's composition root and the template a host copies, see `App/README.md`. Its
  `Modules/` holds the host layers of the six core modules.
- `Boot/Boot.unity` — the boot scene, build index 0.
- `Builder/` — the build entry point, a stub.
