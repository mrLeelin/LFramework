# UIBasic Sample

Goal: show the minimal integration path between the framework flow layer and UI systems.

## What this sample demonstrates

- opening an initial UI screen from procedure flow
- keeping UI startup work outside of bootstrap internals
- separating app flow from concrete view implementation

## Suggested consumer exercise

1. Create a `ProcedureHome`
2. Inject the framework UI-facing service/component your project standardizes on
3. Open a simple home screen on `OnEnter`
4. Close it or replace it when the procedure changes

## Recommended boundaries

- procedures decide **when** a screen is needed
- UI layer decides **how** it is loaded and presented
- avoid placing long-lived UI state directly in procedures
