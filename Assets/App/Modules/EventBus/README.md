# EventBus

## Purpose

The communication channel between modules: a producer publishes a **readonly, past-tense fact struct**, any number of
independent listeners (UI · controllers · VFX · audio · gameplay) receive it, and the producer knows none of them. The
module never names a payload type — each producer owns its facts. Own implementation, no package. Main thread only.

## Layers

Every namespace equals its asmdef `name` (`rootNamespace` equals `name` too).

- `Biome.EventBus.Abstract` (`Abstract/`, refs none, engine-free) — the contract: `IEvent<TData>` (typed channel, the
  hot path) · `IEventPublisher` / `IEventSubscriber` / `IEventBus` · `EventListener<TData>(in TData)` ·
  `IEventErrorSink` · `IEventTrace`.
- `Biome.EventBus.Data` (`Data/`, refs none, engine-free) — `ExceptionPolicy` (`Isolate` · `Rethrow`).
- `Biome.EventBus.Config` (`Config/`) — `EventBusConfig` (policy · max dispatch depth · initial listener capacity · tracing).
- `Biome.EventBus.Fakes` (`Fakes/`, Editor-only, not auto-referenced; refs Abstract) — `RecordingEventBus`, the
  recording fake of `IEventBus`; it never ships in a player.
- `Biome.EventBus` (`Runtime/`) — `EventBusFactory` (the creation point), `EventBus` (the production bus, internal),
  `DebugLogEventErrorSink` (the default sink).
- `Biome.EventBus.Installers` (`Installers/`, not auto-referenced) — VContainer `IInstaller`s: `EventBusInstaller`
  (the production bus), one bus per scope under `IEventBus` · `IEventPublisher` · `IEventSubscriber`, disposed with the
  scope. The module's only VContainer reference.
- `Biome.EventBus.Installers.Mock` (`InstallersMock/`, Editor-only, not auto-referenced) — `RecordingEventBusInstaller`
  (the fake).
- `Biome.EventBus.Tests` (`Tests/`, EditMode) — the contract suite written once over `IEventBus`, run on the fake and
  on the production bus, plus production-only cases (isolation · tracing · thread guard · zero-alloc).
  `Biome.EventBus.Tests.Consumer` (`Tests/Consumer/`) — a test assembly referencing only Abstract, standing in for a consumer.

## Decisions kept in code

- **Delivery.** Synchronous, no queue · `Isolate` by default (the fake is fail-fast) · Publish from a listener is
  depth-first, bounded by the loop guard · a listener added mid-dispatch waits for the next Publish, a disposed one is
  never called again · no ordering guarantee · one bus per DI scope, `Dispose()` drops everything · tracing and the
  thread guard are dev-only · the hot path is a static per-TData slot array indexed by the bus instance — no Type
  lookup, no cast, no allocation after warm-up.
- **Creation through a factory.** `EventBusFactory.Create(EventBusConfig, IEventErrorSink = null, IEventTrace = null)`
  in Runtime is the only creation point outside the Runtime assembly; `EventBus` is `internal sealed`, so nobody
  builds the bus by hand. The factory is pure (no container, no state) and is never injectable. `EventBusInstaller`
  owns the registration — `IEventBus` as a Singleton built through the factory — and the container owns the
  lifetime and the disposal. This deviates from the architecture rule that names the EventBus constructor as the
  public-constructor exception; the deviation is an operator ruling.
- **Namespaces.** Strict: one namespace per asmdef, equal to its `name` (see Layers).
- **Private fields.** `_camelCase` in code and in test fixtures. Renaming the fixture fields is an accepted deviation
  from "tests untouched"; the test bodies and assertions are not changed.

## Not in this slice

Named only; nothing here is promised.

- A seam for `DebugLogEventErrorSink` (it stays public in Runtime; injecting `IEventErrorSink` / `IEventTrace` from the
  container is part of it).
- The EventBus change request: a queue / tick delivery.
- `Events/`.
- The rename of the root `IEventBus` to `I<Module>Service`.
- A service that creates a bus or returns an existing one.
- The ECS event pump / job-side queue · the payload-data home · the IL2CPP player run of the suite.

The section between the markers is emitted by `discover --mode refresh` (unity-cli) from the module's callables
(`[BiomeCallable]` + `///`); never edit it by hand — `discover --mode check` reports drift.

<!-- biome:generated:start -->
<!-- biome:generated:end -->
