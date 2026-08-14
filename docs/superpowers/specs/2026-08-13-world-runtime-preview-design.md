# World Runtime Preview Design

## Goal

Add a `World` page under Game Window > 运行时预览 that shows the active `WorldBase` instance and every registered `IWorldHelper` in enough detail to diagnose interface bindings and frame callbacks.

## Data Boundary

`WorldBase` owns the authoritative helper collections. In Editor builds it exposes an immutable diagnostic snapshot containing the world type, linked procedure, helper instances, concrete registration keys, custom service interface, implemented interfaces, automatic-registration state, and active `IWorldUpdate` / `IWorldLateUpdate` callback membership. The editor does not mutate world state.

The profiled page discovers active worlds from the LServices scope tree, deduplicates aliases that resolve to the same instance, and asks each world for its snapshot. This keeps hotfix world creation unchanged and supports root or child scopes.

## Panel Layout

- Summary row: active worlds, unique helpers, Update callbacks, LateUpdate callbacks.
- Current World section: type, namespace/assembly, linked procedure, registered service aliases.
- World Helpers section: searchable helper cards with implementation type, instance id, registration source, DI binding, concrete keys, all implemented interfaces, and lifecycle/update capability badges.
- Empty and failure states: Play Mode required, no world registered, or a scope inspection error.

## Verification

- Editor tests cover helper deduplication and metadata classification.
- Unity compilation validates runtime/editor assembly boundaries.
- Source checks verify the page is auto-discoverable through `ProfiledBase` and Chinese text is not corrupted.
