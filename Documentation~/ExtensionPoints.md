# Extension Points

Use this guide when extending the framework in a consumer project.

## Add a runtime procedure

1. Create a class deriving from `RuntimeBaseProcedure`
2. Keep the procedure focused on flow orchestration
3. Resolve dependencies via Zenject injection
4. Trigger flow transitions through the procedure system rather than unrelated globals

Recommended responsibilities:

- enter/leave flow state
- request UI/resource setup
- coordinate providers/worlds

Avoid:

- long-lived business state stored directly in the procedure
- direct editor-only dependencies

## Add a hotfix procedure

1. Create a class deriving from `HotfixProcedureBase`
2. Implement `ProcedureState`
3. Mark it for discovery with the package hotfix attribute flow used by your project

Hotfix procedures automatically participate in provider/world registration for their state.

## Add a provider

1. Derive from `SystemProviderBase`
2. Annotate with `[BelongTo(procedureState, lifecycle, sort)]`
3. Override:
   - `AwakeComponent()`
   - `SetUp()`
   - `Subscribe(...)`
   - `UnSubscribe(...)`
   - `OnStop()`

Use providers for:

- procedure-scoped services
- event wiring
- lightweight orchestration support

## Add a world

1. Derive from `WorldBase`
2. Annotate for the owning procedure state
3. Register helpers explicitly or via auto-registration attributes

Use worlds for:

- stateful gameplay domains
- helper aggregation
- update-driven gameplay/runtime coordination

## Add a framework component

1. Create a class deriving from `GameFrameworkComponent`
2. Add or create a matching `ComponentSetting` if configuration is needed
3. Register the component type in `LSystemApplicationBehaviour`

Use components for long-lived, app-wide systems rather than per-procedure state.

## Add settings

1. Create a new setting asset type under the package/project conventions
2. Ensure it is discoverable by `ProjectSettingSelector`
3. Bind and consume it through DI instead of singleton lookups where possible

## Good extension hygiene

- Prefer interfaces at app boundaries
- Keep game-specific code in consumer assemblies
- Treat package helpers as optional conveniences, not contracts
- Log failures during registration/initialization with enough context to diagnose broken setup
