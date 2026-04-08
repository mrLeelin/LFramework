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

### Settings and component configuration

- `LFramework.Runtime.Settings.*`
- `ComponentSetting` based configuration assets

### Framework components

Use the public component interfaces and documented runtime components rather than binding to editor-only code paths.

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
