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

Provider execution uses `BelongTo.Sort` descending (larger values first). Equal
priorities retain registration order, including across procedure entries. Set
different priorities when one provider depends on another; discovery order is
not a dependency declaration.

- Each new registration batch uses this order separately for injection,
  `AwakeComponent`, event subscription, and `SetUp`. Existing `Forever` providers
  are not initialized again when another procedure starts.
- `UpdateComponent` and server-data synchronization use the same global active
  provider order. Synchronization can interleave different payload types; null
  properties and empty lists still do not invoke synchronization callbacks.
- Normal procedure exit and application disposal use the same priority order.
  Failed registration rolls back the current batch in reverse initialization
  order. Unregistered providers no longer receive update or synchronization calls.
  If normal cleanup throws, the failed provider remains tracked so cleanup can be retried;
  successfully released providers are removed immediately.

These rules do not change hotfix-component or world scheduling, network login
event timing, or account-state reset behavior.

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
