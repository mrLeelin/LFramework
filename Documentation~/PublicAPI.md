# Public API Guidance

This document defines the intended consumer-facing API surface for `com.lframework.core`.

## Stable entry points for game code

Consumer projects should prefer depending on these concepts:

### Bootstrap

- `LFramework.Runtime.LSystemApplicationBehaviour`
- `LFramework.Runtime.ISystemApplication`
- `LFramework.Runtime.LFrameworkAspect`

### Flow

- `LFramework.Runtime.Procedure.RuntimeBaseProcedure`
- `LFramework.Hotfix.Procedure.HotfixProcedureBase`

### Runtime extension points

- `LFramework.Runtime.SystemProviderBase`
- `LFramework.Runtime.WorldBase`
- `LFramework.Runtime.IWorldHelper`
- `LFramework.Runtime.IWorldUpdate`
- `LFramework.Runtime.IWorldLateUpdate`
- `LFramework.Runtime.WorldResidentBase`
- `LFramework.Runtime.WorldResidentAttribute`
- `LFramework.Runtime.IWorldResidentUpdate`
- `LFramework.Runtime.IWorldResidentLateUpdate`

See [WorldResident usage and lifecycle](WorldResidents.md) for objects that persist across a declared set of Worlds.

### Settings and component configuration

- `LFramework.Runtime.Settings.*`
- `ComponentSetting` based configuration assets

### Framework components

Use the public component interfaces and documented runtime components rather than binding to editor-only code paths.

### Sequence queue scheduling

`SequenceLineComponent.SetSameFrameExecution(groupType, true)` opts one queue type into
processing completed chunks in the same frame. It applies to existing built-in groups
and newly created ordinary or persistent groups. Other queue types keep the original
coroutine scheduling. Disable it with the same method and `false` when the owning
World releases its configuration.

In this mode, `Reset()` runs once, `MoveNext()` is polled once per yielded frame while
it returns `true`, and a `false` result permits the next chunk to start immediately.
Pause continues to gate the start of the next chunk. At most 64 chunks are completed
consecutively before yielding, so self-appending work cannot monopolize a frame.

For deferred events or other operations that require an update before continuing,
set `SequenceLineChunkSetting<T>.WaitForNextFrameAfterCompletion = true`. Custom
settings can implement the optional `ISequenceLineChunkFrameBoundary` interface.
This does not change the existing `ISequenceLineChunkSetting` contract or turn
deferred `EventComponent.Fire` calls into immediate dispatch.

This is a Runtime API addition. HybridCLR consumers must rebuild the Player/AOT
assemblies alongside Hotfix code that calls it; updating Hotfix DLL bytes alone is
insufficient. A Git-package consumer must update its package reference to a version
containing this API before compiling that Hotfix code.

## Treat as implementation detail

Consumer projects should avoid taking hard dependencies on:

- reflection/discovery internals
- editor utility helpers
- startup ordering helpers not explicitly documented as extension points
- package-internal utility methods with narrow implementation purpose

## Recommended dependency rules

Do:

- depend on base classes and interfaces intended for extension
- inject collaborators through Zenject
- isolate game-specific logic in your own assemblies
- keep app code referencing runtime assemblies, not editor assemblies

Do not:

- call editor-only helpers from runtime code
- assume serialized internal type lists are your extension API
- couple game code directly to package-private startup sequencing details
- rely on undocumented helper behavior remaining stable across versions

## Versioning expectation

Until `1.x`, the package should be treated as **stabilizing** rather than frozen. The classes listed above are the safest points to build on, but major refactors may still tighten boundaries.
