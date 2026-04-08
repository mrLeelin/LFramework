# HotfixMinimal Sample

Goal: show the smallest hotfix-enabled integration path.

## What this sample demonstrates

- loading into a hotfix entry
- registering a hotfix procedure
- activating state-bound providers/worlds from hotfix code

## Consumer exercise

1. Create a hotfix entry class following your HybridCLR load path
2. Add one `HotfixProcedureBase` implementation
3. Give it a `ProcedureState`
4. Add one provider/world annotated with the same state

## Validate

- hotfix assembly is discovered
- hotfix entry runs
- provider registration occurs for the procedure state
- world setup/cleanup follows the procedure lifecycle
