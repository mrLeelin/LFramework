# ProcedureFlow Sample

Goal: show how runtime flow is modeled with procedures, providers, and worlds.

## What this sample demonstrates

- a generated scene: `Scenes/ProcedureFlow.unity`
- a runtime entrance procedure
- a follow-up procedure transition
- the simplest possible state handoff using `ChangeState<T>()`

## Included scripts

- `Runtime/ProcedureFlowApplicationBehaviour.cs`
- `Runtime/ProcedureFlowLaunchProcedure.cs`
- `Runtime/ProcedureFlowHomeProcedure.cs`
- `Editor/ProcedureFlowSampleInstaller.cs`

## How to use

1. Import the sample from Package Manager.
2. Run `LFramework/Samples/Procedure Flow/Install Sample`.
3. Open `Scenes/ProcedureFlow.unity`.
4. Press Play and watch the Console.

## Key classes to study

- `RuntimeBaseProcedure`
- `ProcedureFlowLaunchProcedure`
- `ProcedureFlowHomeProcedure`
