# com.biome.eventbus

## Purpose

The communication channel between modules: a producer publishes a **readonly, past-tense fact struct**, any number of
independent listeners (UI · controllers · VFX · audio · gameplay) receive it, and the producer knows none of them. The
module never names a payload type — each producer owns its facts. Main thread only.

This package carries the contract. It knows nothing about the host, DI or the Unity scene.

## Layers

Every namespace equals its asmdef `name` (`rootNamespace` equals `name` too).

- `Biome.EventBus.Abstract` (`Abstract/`, refs none, engine-free) — the contract: `IEvent<TData>` (typed channel, the
  hot path) · `IEventPublisher` / `IEventSubscriber` / `IEventBus` · `EventListener<TData>(in TData)` ·
  `IEventErrorSink` · `IEventTrace`.
- `Biome.EventBus.Data` (`Data/`, refs none, engine-free) — `ExceptionPolicy` (`Isolate` · `Rethrow`).
- `Biome.EventBus.Config` (`Config/`) — `EventBusConfig` (policy · max dispatch depth · initial listener capacity · tracing).
- `Biome.EventBus.Fakes` (`Fakes/`, Editor-only, not auto-referenced; refs Abstract) — `RecordingEventBus`, the
  recording fake of `IEventBus`; it never ships in a player.

The host owns `Runtime` (the implementation and its factory), `Installers` and `InstallersMock`. `Templates~/` holds
empty skeletons of the host layers.

## Tests

`Tests/Editor` (`Biome.EventBus.Editor.Tests`) holds the abstract `EventBusContractTests` (delivery · fail-fast ·
re-entrancy · (un)subscribe during dispatch · lifetime), which runs here on `RecordingEventBus`.
`Tests/Editor/Consumer` proves that a consumer needs only `Abstract` and `Fakes`.

A host must prove its own implementation: reference `Biome.EventBus.Editor.Tests` from its test assembly, derive from
the suite and override `CreateBus` and `CreateRethrowBus`.

Requires `com.unity.test-framework` in the project (deliberately not a package dependency). A package outside
`Packages/` also needs an entry in `testables`.

## Decisions kept in code

- **Delivery.** Synchronous, no queue · `Isolate` by default (the fake is fail-fast) · Publish from a listener is
  depth-first, bounded by the loop guard · a listener added mid-dispatch waits for the next Publish, a disposed one is
  never called again · no ordering guarantee · one bus per DI scope, `Dispose()` drops everything · tracing and the
  thread guard are dev-only.
- **Namespaces.** Strict: one namespace per asmdef, equal to its `name`.
